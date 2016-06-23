using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace NexDirect
{
    static class HotkeyManager
    {
        // Import some hotkey stuff from user32.dll - once again have no idea how this stuff works but will try to understand soonTM
        // https://stackoverflow.com/questions/11377977/global-hotkeys-in-wpf-working-from-every-window
        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
            [In] IntPtr hWnd,
            [In] int id,
            [In] uint fsModifiers,
            [In] uint vk);
        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private static HwndSource _source; // hotkey related ???
        private static bool registered;
        private static bool init = false;
        public delegate void HotkeyPressedHandler();
        public static event HotkeyPressedHandler HotkeyPressed;

        public static void Init(IntPtr handle)
        {
            // init stuff
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(HwndHook);
            init = true;
        }

        public static bool Register(IntPtr handle, uint modifiers, uint vk)
        {
            if (registered || !init) return false;

            try
            {
                RegisterHotKey(handle, 9000, modifiers, vk);
                registered = true;
                return true;
            }
            catch { return false; }
        }

        public static bool Unregister(IntPtr handle)
        {
            if (!registered) return false;
            UnregisterHotKey(handle, 0);
            registered = false;
            return true;
        }

        public static IntPtr GetRuntimeHandle(System.Windows.Window wpfInstance)
        {
            return (new WindowInteropHelper(wpfInstance)).Handle;
        }

        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312) // WM_HOTKEY thing
            {
                if (wParam.ToInt32() == 9000) // hotkey ID -- we look for 9000
                {
                    HotkeyPressed();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }
    }
}
