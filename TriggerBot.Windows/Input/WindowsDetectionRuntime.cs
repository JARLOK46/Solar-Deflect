using System.ComponentModel;
using System.Runtime.InteropServices;
using TriggerBot.Core.Detection;
using TriggerBot.Windows.ScreenCapture;

namespace TriggerBot.Windows.Input;

public sealed class WindowsDetectionRuntime : IDetectionRuntime
{
    private const int VkE = 0x45;
    private const int VkR = 0x52;
    private readonly GdiPixelReader _pixelReader = new();

    public bool IsActivationKeyPressed => (NativeMethods.GetAsyncKeyState(VkE) & 0x8000) != 0;
    public bool IsSpamKeyPressed => (NativeMethods.GetAsyncKeyState(VkR) & 0x8000) != 0;

    public ScreenPoint GetCursorPosition()
    {
        if (!NativeMethods.GetCursorPos(out var point))
            throw new PixelCaptureException("Windows could not read the cursor position.", new Win32Exception(Marshal.GetLastWin32Error()));

        return new ScreenPoint(point.X, point.Y);
    }

    public RgbColor ReadPixel(ScreenPoint point) => _pixelReader.Read(point);

    public void LeftClick()
    {
        var input = new NativeMethods.Input
        {
            Type = NativeMethods.InputMouse,
            Mouse = new NativeMethods.MouseInput
            {
                Flags = NativeMethods.MouseEventLeftDown
            }
        };
        Send(input);

        input.Mouse.Flags = NativeMethods.MouseEventLeftUp;
        Send(input);
    }

    private static void Send(NativeMethods.Input input)
    {
        if (NativeMethods.SendInput(1, new[] { input }, Marshal.SizeOf<NativeMethods.Input>()) != 1)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows could not send the mouse input.");
    }

    private static class NativeMethods
    {
        public const uint InputMouse = 0;
        public const uint MouseEventLeftDown = 0x0002;
        public const uint MouseEventLeftUp = 0x0004;

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Input
        {
            public uint Type;
            public MouseInput Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseInput
        {
            public int X;
            public int Y;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int virtualKeyCode);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint numberOfInputs, Input[] inputs, int sizeOfInput);
    }
}
