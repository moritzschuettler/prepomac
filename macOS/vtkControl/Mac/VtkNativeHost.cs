// PrePoMax for macOS - the control a native view draws into: vtkControl/Native/VtkNativeHost.cs for an Avalonia window.
// On Windows VTK makes an OpenGL window of its own inside the control and its interactor reads the mouse and the keyboard
// there. Here the view is an external view (VtkNativeView.CreateExternal) that draws with the OpenGL context of an Avalonia
// OpenGlControlBase shown inside the control, and the control passes the mouse, the keyboard and the timers of the
// interaction style to the view.

using System;
using System.Windows.Forms;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;

namespace vtkControl.Native
{
    public class VtkNativeHost : Control
    {
        // Variables
        private const int SelectionTimerMilliseconds = 200;
        private const int SmoothZoomIntervalMilliseconds = 15;
        //
        private VtkNativeView _view;
        private Surface _surface;
        private bool _paintSuspended;
        private bool _attached;
        private Control _keySource;
        private readonly Timer _selectionTimer;
        private readonly Timer _smoothZoomTimer;


        // Events
        public event EventHandler ViewCreated;


        // Properties
        public VtkNativeView View { get { return _view; } }
        // There is no window of the view: the keyboard comes through this control and its parent
        public IntPtr ViewWindow { get { return IntPtr.Zero; } }
        // While the scene is rebuilt a paint would show it half built; the owner invalidates the host when the scene is
        // complete
        public bool PaintSuspended { get { return _paintSuspended; } set { _paintSuspended = value; } }
        // A trackpad has no middle button: an option click is one, as in the rest of the view
        protected override bool OptionClickIsMiddleButton { get { return true; } }


        // Constructors
        public VtkNativeHost()
        {
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            // The interaction style of Windows runs these timers through the window of VTK
            _selectionTimer = new Timer();
            _selectionTimer.Interval = SelectionTimerMilliseconds;
            _selectionTimer.Tick += selectionTimer_Tick;
            _smoothZoomTimer = new Timer();
            _smoothZoomTimer.Interval = SmoothZoomIntervalMilliseconds;
            _smoothZoomTimer.Tick += smoothZoomTimer_Tick;
            //
            if (!DesignMode) CreateView();
        }


        // Methods
        // The view exists with the control: the compatibility layer raises the Load of the view control, which takes the
        // view, before it creates the handles of its children
        private void CreateView()
        {
            int width = Math.Max(1, ClientSize.Width);
            int height = Math.Max(1, ClientSize.Height);
            // A session without a window has no OpenGL controls: its view draws with a context of its own, for pictures
            try
            {
                if (CompatApplication.IsHeadless) _view = VtkNativeView.CreateOffScreen(width, height);
                else
                {
                    _view = VtkNativeView.CreateExternal(width, height);
                    _view.NativeEvent += view_NativeEvent;
                    _surface = new Surface(this);
                    SetNativeVisual(_surface);
                }
            }
            catch (Exception ex)
            {
                // Without the native library, or on a platform it has no view for, the view stays empty
                Console.Error.WriteLine("The 3D view could not be created: " + ex.Message);
                _view = null;
                return;
            }
            ViewCreated?.Invoke(this, EventArgs.Empty);
        }
        protected override void OnHandleDestroyed(EventArgs e)
        {
            _selectionTimer.Stop();
            _smoothZoomTimer.Stop();
            if (_view != null)
            {
                // The context of the surface is gone or goes with it; its resources go with the context
                _view.NativeEvent -= view_NativeEvent;
                _view.Dispose();
                _view = null;
            }
            _attached = false;
            base.OnHandleDestroyed(e);
        }
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            // The view control keeps the focus, so its keys are the keys of the view
            if (_keySource != null)
            {
                _keySource.KeyDown -= keySource_KeyDown;
                _keySource.KeyUp -= keySource_KeyUp;
            }
            _keySource = Parent;
            if (_keySource != null)
            {
                _keySource.KeyDown += keySource_KeyDown;
                _keySource.KeyUp += keySource_KeyUp;
            }
        }
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_view != null && ClientSize.Width > 0 && ClientSize.Height > 0)
            {
                _view.SetSize(ClientSize.Width, ClientSize.Height);
                Invalidate();
            }
        }
        // An invalidated host draws the view, as a paint of the window of VTK does on Windows
        protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            if (_view == null || _paintSuspended) return;
            if (_attached || _view.OffScreen) _view.Render();
            _surface?.RequestFrame();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
        }
        private void view_NativeEvent(object sender, VtkNativeEventArgs e)
        {
            // A new frame of the view is shown with the next frame of Avalonia
            if (e.Event == VtkNativeEvent.RenderEnd) _surface?.RequestFrame();
        }
        // Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) Input(VtkNativeInput.LeftButtonDown, e);
            else if (e.Button == MouseButtons.Middle) Input(VtkNativeInput.MiddleButtonDown, e);
            else if (e.Button == MouseButtons.Right) Input(VtkNativeInput.RightButtonDown, e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left) Input(VtkNativeInput.LeftButtonUp, e);
            else if (e.Button == MouseButtons.Middle) Input(VtkNativeInput.MiddleButtonUp, e);
            else if (e.Button == MouseButtons.Right) Input(VtkNativeInput.RightButtonUp, e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Input(VtkNativeInput.MouseMove, e);
            // The hover pick of the selection runs once the mouse rests
            _selectionTimer.Stop();
            _selectionTimer.Start();
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Delta == 0) return;
            Input(e.Delta > 0 ? VtkNativeInput.WheelForward : VtkNativeInput.WheelBackward, e);
            if (_view != null && !_smoothZoomTimer.Enabled && SmoothZoomPending()) _smoothZoomTimer.Start();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _selectionTimer.Stop();
            if (_view != null) _view.HandleInput(VtkNativeInput.Leave, 0, 0);
        }
        protected override void OnTouchPadMagnify(int x, int y, double magnification)
        {
            // The view control zooms with the trackpad navigation of the native style
            if (Parent is vtkControl view) view.TouchPadMagnify(x, y, magnification);
        }
        private void Input(VtkNativeInput input, MouseEventArgs e)
        {
            if (_view == null) return;
            Keys modifiers = ModifierKeys;
            // The display coordinates of VTK have the origin bottom left
            _view.HandleInput(input, e.X, ClientSize.Height - 1 - e.Y, (modifiers & Keys.Control) == Keys.Control,
                              (modifiers & Keys.Shift) == Keys.Shift);
        }
        // Keyboard
        private void keySource_KeyDown(object sender, KeyEventArgs e)
        {
            Key(VtkNativeInput.KeyPress, e);
        }
        private void keySource_KeyUp(object sender, KeyEventArgs e)
        {
            Key(VtkNativeInput.KeyRelease, e);
        }
        // The keys the interaction style knows: the arrows turn the view, escape cancels a box selection, z holds a box zoom
        private void Key(VtkNativeInput input, KeyEventArgs e)
        {
            if (_view == null) return;
            string keySym = null;
            int keyCode = 0;
            switch (e.KeyCode)
            {
                case Keys.Left: keySym = "Left"; break;
                case Keys.Up: keySym = "Up"; break;
                case Keys.Right: keySym = "Right"; break;
                case Keys.Down: keySym = "Down"; break;
                case Keys.Escape: keySym = "Escape"; keyCode = 27; break;
                case Keys.Space: keySym = "space"; keyCode = 32; break;
                default:
                    if (e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
                    {
                        char letter = (char)('a' + (e.KeyCode - Keys.A));
                        keySym = letter.ToString();
                        keyCode = e.Shift ? char.ToUpperInvariant(letter) : letter;
                    }
                    break;
            }
            if (keySym == null) return;
            System.Drawing.Point position = PointToClient(Cursor.Position);
            _view.HandleInput(input, position.X, ClientSize.Height - 1 - position.Y, e.Control, e.Shift, keyCode, keySym);
        }
        // Timers
        private void selectionTimer_Tick(object sender, EventArgs e)
        {
            _selectionTimer.Stop();
            // The style ignores the call while no hover pick is pending
            if (_view != null) _view.Navigate(VtkNativeNavigation.FireSelectionTimer);
        }
        private void smoothZoomTimer_Tick(object sender, EventArgs e)
        {
            if (_view == null || !SmoothZoomPending())
            {
                _smoothZoomTimer.Stop();
                return;
            }
            _view.Navigate(VtkNativeNavigation.StepSmoothZoom);
        }
        private bool SmoothZoomPending()
        {
            int[] state = _view.GetInteractionState();
            return state.Length > 3 && state[3] != 0;
        }
        //
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _selectionTimer.Dispose();
                _smoothZoomTimer.Dispose();
            }
            base.Dispose(disposing);
        }


        // The OpenGL control of Avalonia the view draws with
        private sealed class Surface : OpenGlControlBase
        {
            private readonly VtkNativeHost _host;
            private bool _failed;

            public Surface(VtkNativeHost host)
            {
                _host = host;
            }
            public void RequestFrame()
            {
                if (Dispatcher.UIThread.CheckAccess()) RequestNextFrameRendering();
                else Dispatcher.UIThread.Post(RequestNextFrameRendering);
            }
            protected override void OnOpenGlInit(GlInterface gl)
            {
                try
                {
                    if (_host._view == null) return;
                    _host._view.AttachExternalContext();
                    _host._attached = true;
                    _failed = false;
                }
                catch (Exception ex)
                {
                    _failed = true;
                    Console.Error.WriteLine("The 3D view could not be initialized: " + ex);
                }
            }
            protected override void OnOpenGlDeinit(GlInterface gl)
            {
                try
                {
                    if (_host._view != null && _host._attached) _host._view.DetachExternalContext();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("The 3D view could not be released: " + ex);
                }
                _host._attached = false;
            }
            protected override void OnOpenGlRender(GlInterface gl, int fb)
            {
                if (_failed || !_host._attached || _host._view == null || _host._paintSuspended) return;
                double scaling = VisualRoot?.RenderScaling ?? 1;
                int width = Math.Max(1, (int)Math.Round(Bounds.Width * scaling));
                int height = Math.Max(1, (int)Math.Round(Bounds.Height * scaling));
                try
                {
                    _host._view.RenderExternal((uint)fb, width, height);
                }
                catch (Exception ex)
                {
                    _failed = true;
                    Console.Error.WriteLine("The 3D view could not be drawn: " + ex);
                }
            }
        }
    }
}
