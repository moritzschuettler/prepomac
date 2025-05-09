using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CaeGlobals
{
    /// <summary>
    /// Class for intercepting low level Windows mouse hooks.
    /// /// https://github.com/rvknth043/Global-Low-Level-Key-Board-And-Mouse-Hook
    /// </summary>
    public class MouseHook
    {
        /// <summary>
        /// Internal callback processing function
        /// </summary>
        private delegate IntPtr MouseHookHandler(int nCode, IntPtr wParam, IntPtr lParam);
        private MouseHookHandler hookHandler;
        private static long _lastClickTime = 0;
        private static Point _lastClickPosition;
        private static readonly int DoubleClickTime = SystemInformation.DoubleClickTime;
        private static readonly Size DoubleClickSize = SystemInformation.DoubleClickSize;

        /// <summary>
        /// Function to be called when defined even occurs
        /// </summary>
        /// <param name="mouseStruct">MSLLHOOKSTRUCT mouse structure</param>
        public delegate bool MouseHookCallback(MSLLHOOKSTRUCT mouseStruct);

        #region Events
        public event MouseHookCallback LeftButtonDown;
        public event MouseHookCallback LeftButtonUp;
        public event MouseHookCallback RightButtonDown;
        public event MouseHookCallback RightButtonUp;
        public event MouseHookCallback MouseMove;
        public event MouseHookCallback MouseWheel;
        public event MouseHookCallback DoubleClick;
        public event MouseHookCallback MiddleButtonDown;
        public event MouseHookCallback MiddleButtonUp;
        #endregion

        /// <summary>
        /// Low level mouse hook's ID
        /// </summary>
        private IntPtr hookID = IntPtr.Zero;

        /// <summary>
        /// Install low level mouse hook
        /// </summary>
        /// <param name="mouseHookCallbackFunc">Callback function</param>
        public void Install()
        {
            hookHandler = HookFunc;
            hookID = SetHook(hookHandler);
        }

        /// <summary>
        /// Remove low level mouse hook
        /// </summary>
        public void Uninstall()
        {
            if (hookID == IntPtr.Zero)
                return;

            UnhookWindowsHookEx(hookID);
            hookID = IntPtr.Zero;
        }

        /// <summary>
        /// Destructor. Unhook current hook
        /// </summary>
        ~MouseHook()
        {
            Uninstall();
        }

        /// <summary>
        /// Sets hook and assigns its ID for tracking
        /// </summary>
        /// <param name="proc">Internal callback function</param>
        /// <returns>Hook ID</returns>
        private IntPtr SetHook(MouseHookHandler proc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(module.ModuleName), 0);
        }

        /// <summary>
        /// Callback function
        /// </summary>
        private IntPtr HookFunc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            bool callNextHook = true;
            // parse system messages
            if (nCode >= 0)
            {
                if (MouseMessages.WM_LBUTTONDOWN == (MouseMessages)wParam)
                {
                    var mouseStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT));
                    var currentTime = Environment.TickCount;
                    var currentPosition = new Point(mouseStruct.pt.x, mouseStruct.pt.y);
                    //
                    if (currentTime - _lastClickTime <= DoubleClickTime &&
                            Math.Abs(currentPosition.X - _lastClickPosition.X) <= DoubleClickSize.Width &&
                            Math.Abs(currentPosition.Y - _lastClickPosition.Y) <= DoubleClickSize.Height &&
                            DoubleClick != null)
                    {
                        callNextHook = DoubleClick(mouseStruct);
                    }
                    else if (LeftButtonDown != null) callNextHook = LeftButtonDown(mouseStruct);
                    //
                    _lastClickTime = currentTime;
                    _lastClickPosition = currentPosition;
                }
                if (MouseMessages.WM_LBUTTONUP == (MouseMessages)wParam)
                    if (LeftButtonUp != null)
                        callNextHook = LeftButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONDOWN == (MouseMessages)wParam)
                    if (RightButtonDown != null)
                        callNextHook = RightButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_RBUTTONUP == (MouseMessages)wParam)
                    if (RightButtonUp != null)
                        callNextHook = RightButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEMOVE == (MouseMessages)wParam)
                    if (MouseMove != null)
                        callNextHook = MouseMove((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MOUSEWHEEL == (MouseMessages)wParam)
                    if (MouseWheel != null)
                        callNextHook = MouseWheel((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONDOWN == (MouseMessages)wParam)
                    if (MiddleButtonDown != null)
                        callNextHook = MiddleButtonDown((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
                if (MouseMessages.WM_MBUTTONUP == (MouseMessages)wParam)
                    if (MiddleButtonUp != null)
                        callNextHook = MiddleButtonUp((MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT)));
            }
            //
            if (callNextHook) return CallNextHookEx(hookID, nCode, wParam, lParam);
            return (IntPtr)1;
        }

        #region WinAPI
        private const int WH_MOUSE_LL = 14;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP = 0x0202,
            WM_MOUSEMOVE = 0x0200,
            WM_MOUSEWHEEL = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP = 0x0205,
            WM_LBUTTONDBLCLK = 0x0203,
            WM_MBUTTONDOWN = 0x0207,
            WM_MBUTTONUP = 0x0208
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            MouseHookHandler lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        #endregion
    }
}