using System.Runtime.InteropServices;
using Windows.System;

namespace AutoDarkModeApp.Utils;

public partial class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookCallback;
    private readonly Dictionary<VirtualKey, bool> _keyStates = [];

    public event EventHandler<KeyboardHookEventArgs>? KeyEvent;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public int vkCode;
        public int scanCode;
        public int flags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    public void Install()
    {
        if (_hookHandle == IntPtr.Zero)
        {
            _hookCallback = HookCallback;
            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, 0, 0);
        }
        else
        {
            return;
        }
    }

    public void Uninstall()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            if (UnhookWindowsHookEx(_hookHandle))
            {
                _hookHandle = IntPtr.Zero;
            }
        }
        _keyStates.Clear();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            var key = (VirtualKey)kbd.vkCode;
            var isKeyUp = wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP;

            _keyStates[key] = !isKeyUp;

            var args = new KeyboardHookEventArgs
            {
                VirtualKeyCode = kbd.vkCode,
                IsKeyUp = isKeyUp,
                Handled = false
            };

            KeyEvent?.Invoke(this, args);

            if (args.Handled)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    public bool IsKeyDown(VirtualKey key)
    {
        if (_keyStates.TryGetValue(key, out bool state))
        {
            return state;
        }

        return (GetKeyState((int)key) & 0x8000) != 0;
    }

    public void Dispose()
    {
        Uninstall();
        GC.SuppressFinalize(this);
    }

    public KeyboardHook()
    {
        Dispose();
    }
}

public class KeyboardHookEventArgs : EventArgs
{
    public int VirtualKeyCode { get; set; }
    public bool IsKeyUp { get; set; }
    public bool Handled { get; set; }
}
