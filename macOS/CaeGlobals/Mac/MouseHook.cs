// PrePoMax for macOS: replaces CaeGlobals/MouseHook.cs (a global Win32 low level mouse hook used by the WinForms
// autocomplete menu). macOS has no such hook; the Avalonia controls handle their mouse input themselves.
using System;

namespace CaeGlobals
{
    public class MouseHook
    {
        public delegate bool MouseHookCallback(MSLLHOOKSTRUCT mouseStruct);
#pragma warning disable CS0067
        public event MouseHookCallback LeftButtonDown;
        public event MouseHookCallback LeftButtonUp;
        public event MouseHookCallback RightButtonDown;
        public event MouseHookCallback RightButtonUp;
        public event MouseHookCallback MouseMove;
        public event MouseHookCallback MouseWheel;
        public event MouseHookCallback DoubleClick;
        public event MouseHookCallback MiddleButtonDown;
        public event MouseHookCallback MiddleButtonUp;
#pragma warning restore CS0067
        public void Install() { }
        public void Uninstall() { }
        public struct POINT
        {
            public int x;
            public int y;
        }
        public struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
