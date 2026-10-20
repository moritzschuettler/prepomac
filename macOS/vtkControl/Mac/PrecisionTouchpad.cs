// PrePoMax for macOS - the touchpad of the view (vtkControl/Tools/PrecisionTouchpad.cs). Windows reads the contacts of a
// precision touchpad from raw HID input; macOS turns the trackpad gestures into precise scroll and magnify events, which
// the view receives as wheel messages, so no touchpad device is registered here.
using System;

namespace vtkControl
{
    public struct TouchContact
    {
        public int Id;
        public double X;
        public double Y;
    }

    internal class PrecisionTouchpad
    {
        public bool Found { get { return false; } }

        public bool Register(IntPtr windowHandle) { return false; }
        public TouchContact[] GetContacts(IntPtr rawInputHandle) { return null; }
    }
}
