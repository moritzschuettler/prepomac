// PrePoMax for macOS - the trackpad of the view. A trackpad has no middle button, so a drag with the option key rotates
// (with shift it pans, with command it zooms). The magnify gesture zooms about the pointer as the pinch of a Windows
// precision touchpad does, and the continuous scroll of two fingers feeds the same navigation the contacts of a
// precision touchpad feed on Windows - pan, or rotate when "Two finger drag" is set to Rotate - which leaves the notches
// of a mouse wheel free to zoom. Holding the option key while scrolling pans whatever "Two finger drag" says, so that a
// Mac which keeps the Rotate of an older version still pans when asked to.
namespace vtkControl
{
    public partial class vtkControl
    {
        protected override bool OptionClickIsMiddleButton { get { return true; } }

        protected override void OnTouchPadMagnify(int x, int y, double magnification)
        {
            if (_style == null || _renderer == null || magnification <= -0.9) return;
            // The gestures are off: the pinch is ignored as the contacts of a precision touchpad are
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return;
            // Fingers moving apart make the model larger, which means a smaller parallel scale
            _trackpadZoom /= 1 + magnification;
            _trackpadCenterX = x;
            _trackpadCenterY = ClientRectangle.Height - 1 - y;
            _trackpadShift = false;
            _trackpadPending = true;
            FlushTrackpadGesture();
            _trackpadGestureEndTimer.Stop();
            _trackpadGestureEndTimer.Start();
        }

        protected override bool TouchPadScrollPans { get { return true; } }

        protected override bool OnTouchPadPan(int x, int y, double deltaX, double deltaY, bool panForced)
        {
            // The two finger gestures are off: the scroll is ignored as the pinch is - it is not handed to the wheel,
            // which would zoom - and the wheel itself keeps zooming
            if (_trackpadNavigation == null || !_trackpadNavigation.Enabled) return true;
            if (_style == null || _renderer == null) return false;
            // The scroll delta is the direction the content moves in - macOS has already applied the scroll direction
            // of the user - and the display coordinates of the view grow upwards, so y is mirrored as it is for the
            // mouse and for the contacts of a Windows precision touchpad. The gain is the one of the wheel: 40 pixels
            // a notch
            _trackpadDragX += TrackpadNavigation.WheelDeltaToPixels(deltaX * TrackpadNavigation.WheelDeltaPerNotch);
            _trackpadDragY -= TrackpadNavigation.WheelDeltaToPixels(deltaY * TrackpadNavigation.WheelDeltaPerNotch);
            _trackpadCenterX = x;
            _trackpadCenterY = ClientRectangle.Height - 1 - y;
            // Shift cannot switch between pan and rotate here: AppKit turns the scroll of a held shift key into
            // horizontal deltas, so the setting alone decides - the option key of the scroll overrides the setting
            _trackpadShift = false;
            _trackpadPanForced = panForced;
            _trackpadPending = true;
            FlushTrackpadGesture();
            _trackpadGestureEndTimer.Stop();
            _trackpadGestureEndTimer.Start();
            //
            return true;
        }
    }
}
