using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace TriggerBot.Windows.Input;

public enum TriggerHotkey
{
    Toggle = 1,
    SelectColor = 2,
    FixPosition = 3,
    Spam = 4
}

public sealed class GlobalHotkeyService : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkF5 = 0x74;
    private const uint VkF6 = 0x75;
    private const uint VkF7 = 0x76;
    private const uint VkR = 0x52;
    private readonly Dictionary<int, TriggerHotkey> _registered = new();
    private HwndSource? _source;
    private IntPtr _windowHandle;

    public event EventHandler<TriggerHotkey>? Pressed;

    public void Attach(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (_source is not null) return;

        _windowHandle = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_windowHandle)
            ?? throw new HotkeyRegistrationException("Could not access the application's native window handle.");
        _source.AddHook(WindowProcedure);

        try
        {
            Register(1, TriggerHotkey.Toggle, VkF5);
            Register(2, TriggerHotkey.SelectColor, VkF6);
            Register(3, TriggerHotkey.FixPosition, VkF7);
            Register(4, TriggerHotkey.Spam, VkR);
        }
        catch
        {
            Detach();
            throw;
        }
    }

    public void Detach()
    {
        if (_source is not null)
            _source.RemoveHook(WindowProcedure);

        foreach (var id in _registered.Keys.ToArray())
            NativeMethods.UnregisterHotKey(_windowHandle, id);

        _registered.Clear();
        _source = null;
        _windowHandle = IntPtr.Zero;
    }

    public void Dispose() => Detach();

    private void Register(int id, TriggerHotkey hotkey, uint virtualKey)
    {
        if (!NativeMethods.RegisterHotKey(_windowHandle, id, ModNoRepeat, virtualKey))
            throw new HotkeyRegistrationException($"Could not register {hotkey} (Win32 error {Marshal.GetLastWin32Error()}).");

        _registered.Add(id, hotkey);
    }

    private IntPtr WindowProcedure(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmHotkey && _registered.TryGetValue(wParam.ToInt32(), out var hotkey))
        {
            Pressed?.Invoke(this, hotkey);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}

public sealed class HotkeyRegistrationException : Exception
{
    public HotkeyRegistrationException(string message) : base(message) { }
}
