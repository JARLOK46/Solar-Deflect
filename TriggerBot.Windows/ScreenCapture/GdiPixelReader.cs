using System.ComponentModel;
using System.Runtime.InteropServices;
using TriggerBot.Core.Detection;

namespace TriggerBot.Windows.ScreenCapture;

public sealed class GdiPixelReader : IPixelReader
{
    public RgbColor Read(ScreenPoint point)
    {
        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
            throw new PixelCaptureException("Windows could not provide a screen device context.", new Win32Exception(Marshal.GetLastWin32Error()));

        try
        {
            var color = NativeMethods.GetPixel(screenDc, point.X, point.Y);
            if (color == NativeMethods.ErrorColor)
                throw new PixelCaptureException(
                    $"Windows could not read pixel ({point.X}, {point.Y}).",
                    new Win32Exception(Marshal.GetLastWin32Error()));

            return new RgbColor(
                red: (byte)(color & 0xFF),
                green: (byte)((color >> 8) & 0xFF),
                blue: (byte)((color >> 16) & 0xFF));
        }
        finally
        {
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private static class NativeMethods
    {
        public const uint ErrorColor = 0xFFFFFFFF;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern uint GetPixel(IntPtr hDc, int x, int y);
    }
}

public sealed class PixelCaptureException : Exception
{
    public PixelCaptureException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
