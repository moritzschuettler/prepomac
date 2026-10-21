// PrePoMax for macOS - the trackpad of the view. A trackpad has no middle button, so a drag with the option key rotates
// (with shift it pans, with command it zooms), and the magnify gesture zooms about the pointer as the pinch of a
// Windows precision touchpad does.
namespace vtkControl
{
    public partial class vtkControl
    {
        protected override bool OptionClickIsMiddleButton { get { return true; } }

        protected override void OnTouchPadMagnify(int x, int y, double magnification)
        {
            if (_style == null || _renderer == null || magnification <= -0.9) return;
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
    }
}
