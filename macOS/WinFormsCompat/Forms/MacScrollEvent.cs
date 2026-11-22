// PrePoMax for macOS - the device of a scroll event. A trackpad scrolls continuously while a mouse wheel turns in
// notches, but Avalonia reports both as the same wheel event and drops the flag that tells them apart, so the flag is
// read from the event the application is handling right now. Nothing is created here and nothing is retained - the
// CoreGraphics event of the current NSEvent belongs to the system - so there is no CFRelease on this path.
using System;
using System.Runtime.InteropServices;

namespace System.Windows.Forms
{
    internal static class MacScrollEvent
    {
        // CoreGraphics - CGEventTypes.h, CGEventField: kCGScrollWheelEventIsContinuous = 88, "The value is non-zero
        // when the scrolling data is pixel-based and zero when the scrolling data is line-based."
        // CGEventCreate(NULL) cannot be used to read it: it returns a new event of the default source (CGEvent.h:
        // "Return a new event using the event source source. If source is NULL, the default source is used."), not
        // the event that is being handled, and such a new event carries no scrolling data.
        private const uint ScrollWheelEventIsContinuous = 88;
        // CoreGraphics - CGEventTypes.h, CGEventType: kCGEventScrollWheel = NX_SCROLLWHEELMOVED = 22 (IOLLEvent.h).
        // It guards the field above: a field read of another kind of event would mean nothing
        private const uint ScrollWheelEvent = 22;

        private const string CoreGraphics = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";
        private const string ObjectiveC = "/usr/lib/libobjc.A.dylib";

        [DllImport(CoreGraphics)]
        private static extern long CGEventGetIntegerValueField(IntPtr ev, uint field);
        [DllImport(CoreGraphics)]
        private static extern uint CGEventGetType(IntPtr ev);
        [DllImport(ObjectiveC)]
        private static extern IntPtr objc_getClass(string name);
        [DllImport(ObjectiveC)]
        private static extern IntPtr sel_registerName(string name);
        [DllImport(ObjectiveC, EntryPoint = "objc_msgSend")]
        private static extern IntPtr Send(IntPtr receiver, IntPtr selector);

        // The selectors are registered once instead of on every wheel event
        private static bool _prepared;
        private static bool _warned;
        private static IntPtr _nsApplication;
        private static IntPtr _selSharedApplication;
        private static IntPtr _selCurrentEvent;
        private static IntPtr _selCGEvent;

        private static void Prepare()
        {
            _nsApplication = objc_getClass("NSApplication");
            _selSharedApplication = sel_registerName("sharedApplication");
            _selCurrentEvent = sel_registerName("currentEvent");
            _selCGEvent = sel_registerName("CGEvent");
            _prepared = true;
        }

        // True for the continuous scrolling of a trackpad, false for the notches of a wheel, and null when the event
        // cannot be read, which leaves the decision to the caller
        public static bool? IsContinuous()
        {
            try
            {
                if (!_prepared) Prepare();
                // -[NSApplication currentEvent] is the event AppKit is dispatching, and -[NSEvent CGEvent] the
                // CoreGraphics event of it. NSEvent.h: "-CGEvent returns an autoreleased CGEventRef corresponding to
                // the NSEvent" - it is not created here, so it is not released here either
                IntPtr app = Send(_nsApplication, _selSharedApplication);
                if (app == IntPtr.Zero) return null;
                IntPtr currentEvent = Send(app, _selCurrentEvent);
                if (currentEvent == IntPtr.Zero) return null;
                IntPtr cgEvent = Send(currentEvent, _selCGEvent);
                if (cgEvent == IntPtr.Zero) return null;
                // A wheel event is expected here; anything else means the event was already dispatched and its
                // scrolling data is not the one of the wheel that is being handled
                if (CGEventGetType(cgEvent) != ScrollWheelEvent) return null;
                return CGEventGetIntegerValueField(cgEvent, ScrollWheelEventIsContinuous) != 0;
            }
            catch (Exception ex)
            {
                // A session without the window server (a headless run) has no current event: the wheel keeps working.
                // Anything else is a fault of this probe and is said once, so that it is not searched for in the dark
                if (!_warned)
                {
                    _warned = true;
                    Console.Error.WriteLine("The scroll device cannot be read: " + ex.Message);
                }
                return null;
            }
        }
    }
}
