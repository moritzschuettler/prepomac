// PrePoMax for macOS - Splitter: a docked bar that resizes the control docked to the same edge before it by dragging.

using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
    public class Splitter : Control
    {
        // Variables
        private int _minSize = 25;
        private int _minExtra = 25;
        private Control _target;
        private int _startPosition;
        private int _startSize;


        // Events
        public event SplitterEventHandler SplitterMoved;


        // Properties
        [DefaultValue(25)] public int MinSize { get { return _minSize; } set { _minSize = Math.Max(0, value); } }
        [DefaultValue(25)] public int MinExtra { get { return _minExtra; } set { _minExtra = Math.Max(0, value); } }
        public int SplitPosition
        {
            get
            {
                Control target = FindTarget();
                if (target == null) return -1;
                return Horizontal ? target.Height : target.Width;
            }
            set
            {
                Control target = FindTarget();
                if (target == null) return;
                SetSize(target, value);
            }
        }
        private bool Horizontal { get { return Dock == DockStyle.Top || Dock == DockStyle.Bottom; } }


        // Constructors
        public Splitter()
        {
            Dock = DockStyle.Left;
            Size = new Size(3, 3);
            TabStop = false;
        }


        // Methods
        protected override void OnDockChanged(EventArgs e)
        {
            base.OnDockChanged(e);
            Cursor = Horizontal ? Cursors.HSplit : Cursors.VSplit;
        }
        // The control docked to the same edge that the docking lays out just before the splitter: the next one in the
        // collection, which WinForms docks first
        private Control FindTarget()
        {
            if (Parent == null || Dock == DockStyle.None || Dock == DockStyle.Fill) return null;
            Control.ControlCollection controls = Parent.Controls;
            int index = controls.GetChildIndex(this, false);
            for (int i = index + 1; i < controls.Count; i++)
            {
                if (controls[i].Visible && controls[i].Dock == Dock) return controls[i];
            }
            for (int i = index - 1; i >= 0; i--)
            {
                if (controls[i].Visible && controls[i].Dock == Dock) return controls[i];
            }
            return null;
        }
        private void SetSize(Control target, int size)
        {
            int available = Horizontal ? Parent.ClientSize.Height : Parent.ClientSize.Width;
            int thickness = Horizontal ? Height : Width;
            size = Math.Max(_minSize, Math.Min(size, available - thickness - _minExtra));
            if (Horizontal) target.Height = size;
            else target.Width = size;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            _target = FindTarget();
            if (_target == null) return;
            Point screen = PointToScreen(e.Location);
            _startPosition = Horizontal ? screen.Y : screen.X;
            _startSize = Horizontal ? _target.Height : _target.Width;
            Capture = true;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_target == null) return;
            Point screen = PointToScreen(e.Location);
            int delta = (Horizontal ? screen.Y : screen.X) - _startPosition;
            // The target grows toward the splitter: away from its edge
            if (Dock == DockStyle.Bottom || Dock == DockStyle.Right) delta = -delta;
            SetSize(_target, _startSize + delta);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_target == null) return;
            _target = null;
            Capture = false;
            SplitterMoved?.Invoke(this, new SplitterEventArgs(e.X, e.Y, Left, Top));
        }
    }
}
