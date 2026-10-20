// PrePoMax for macOS - the callbacks of the Windows API compatibility library (Native/Win32Compat.c): the window messages
// and window queries the PrePoMax code sends through user32.dll reach the controls here.

using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;

namespace System.Windows.Forms
{
    internal static unsafe class Win32Bridge
    {
        [DllImport("libWin32Compat", EntryPoint = "Win32Compat_SetCallbacks")]
        private static extern void SetCallbacks(IntPtr sendMessage, IntPtr postMessage, IntPtr getScrollPos, IntPtr setScrollPos,
                                                IntPtr windowFromPoint, IntPtr getWindowText, IntPtr isWindowVisible,
                                                IntPtr enumThreadWindows, IntPtr getKeyboardState);

        public static void Register()
        {
            try
            {
                SetCallbacks((IntPtr)(delegate* unmanaged<IntPtr, int, IntPtr, IntPtr, IntPtr>)&SendMessage,
                             (IntPtr)(delegate* unmanaged<IntPtr, int, IntPtr, IntPtr, IntPtr>)&PostMessage,
                             (IntPtr)(delegate* unmanaged<IntPtr, int, int, int>)&GetScrollPos,
                             (IntPtr)(delegate* unmanaged<IntPtr, int, int, int>)&SetScrollPos,
                             (IntPtr)(delegate* unmanaged<int, int, IntPtr>)&WindowFromPoint,
                             (IntPtr)(delegate* unmanaged<IntPtr, ushort*, int, int>)&GetWindowText,
                             (IntPtr)(delegate* unmanaged<IntPtr, int>)&IsWindowVisible,
                             (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&EnumThreadWindows,
                             (IntPtr)(delegate* unmanaged<byte*, int>)&GetKeyboardState);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("The Windows API compatibility library is not available: " + ex.Message);
            }
        }

        [UnmanagedCallersOnly]
        private static IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                Control control = Control.FromHandle(hwnd);
                if (control == null) return IntPtr.Zero;
                if (msg == 0x000F) { control.Invalidate(true); return IntPtr.Zero; }    // WM_PAINT
                if (Dispatcher.UIThread.CheckAccess()) return control.SendMessageInternal(msg, wParam, lParam);
                return Dispatcher.UIThread.Invoke(() => control.SendMessageInternal(msg, wParam, lParam));
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
        [UnmanagedCallersOnly]
        private static IntPtr PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            Control control = Control.FromHandle(hwnd);
            if (control != null)
                Dispatcher.UIThread.Post(() => Toolkit.Dispatch(() => control.SendMessageInternal(msg, wParam, lParam)));
            return IntPtr.Zero;
        }
        [UnmanagedCallersOnly]
        private static int GetScrollPos(IntPtr hwnd, int bar, int unused)
        {
            Control control = Control.FromHandle(hwnd);
            if (control is IScrollPositionTarget target) return target.GetScrollPosition(bar == 1);
            return 0;
        }
        [UnmanagedCallersOnly]
        private static int SetScrollPos(IntPtr hwnd, int bar, int position)
        {
            Control control = Control.FromHandle(hwnd);
            if (control is IScrollPositionTarget target)
            {
                int old = target.GetScrollPosition(bar == 1);
                target.SetScrollPosition(bar == 1, position);
                return old;
            }
            return 0;
        }
        [UnmanagedCallersOnly]
        private static IntPtr WindowFromPoint(int x, int y)
        {
            // The mouse wheel goes to the control under the pointer without forwarding
            return IntPtr.Zero;
        }
        [UnmanagedCallersOnly]
        private static int GetWindowText(IntPtr hwnd, ushort* buffer, int max)
        {
            Control control = Control.FromHandle(hwnd);
            string text = control?.Text ?? "";
            if (buffer == null || max <= 0) return text.Length;
            int n = Math.Min(text.Length, max - 1);
            for (int i = 0; i < n; i++) buffer[i] = text[i];
            buffer[n] = 0;
            return n;
        }
        [UnmanagedCallersOnly]
        private static int IsWindowVisible(IntPtr hwnd)
        {
            Control control = Control.FromHandle(hwnd);
            return control != null && control.Visible ? 1 : 0;
        }
        [UnmanagedCallersOnly]
        private static int EnumThreadWindows(IntPtr callback, IntPtr lParam)
        {
            delegate* unmanaged<IntPtr, IntPtr, int> enumerate = (delegate* unmanaged<IntPtr, IntPtr, int>)callback;
            foreach (Form form in Application.OpenForms)
            {
                if (enumerate(form.Handle, lParam) == 0) break;
            }
            return 1;
        }
        [UnmanagedCallersOnly]
        private static int GetKeyboardState(byte* state)
        {
            Keys modifiers = Control.ModifierKeys;
            if ((modifiers & Keys.Shift) != 0) state[0x10] = 0x80;
            if ((modifiers & Keys.Control) != 0) state[0x11] = 0x80;
            if ((modifiers & Keys.Alt) != 0) state[0x12] = 0x80;
            return 1;
        }
    }

    // A control with scroll positions that user32 GetScrollPos and SetScrollPos reach
    internal interface IScrollPositionTarget
    {
        int GetScrollPosition(bool vertical);
        void SetScrollPosition(bool vertical, int position);
    }
}
