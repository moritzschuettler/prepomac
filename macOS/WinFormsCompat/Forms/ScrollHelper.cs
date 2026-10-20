// PrePoMax for macOS - scroll bars of the controls that draw their content (tree view, list view, data grid view).

using System.Drawing;
using Avalonia;
using AC = Avalonia.Controls;
using Size = System.Drawing.Size;

namespace System.Windows.Forms
{
    internal sealed class ScrollHelper
    {
        private readonly Control _owner;
        private readonly Func<Rectangle> _viewport;      // client area of the content without the scroll bars
        private AC.Primitives.ScrollBar _vertical;
        private AC.Primitives.ScrollBar _horizontal;
        private bool _updating;
        public int OffsetX { get; private set; }
        public int OffsetY { get; private set; }
        public int ExtentWidth { get; private set; }
        public int ExtentHeight { get; private set; }
        public bool VerticalVisible { get { return _vertical != null && _vertical.IsVisible; } }
        public bool HorizontalVisible { get { return _horizontal != null && _horizontal.IsVisible; } }
        public int SmallChangeY { get; set; } = 16;
        public event Action Scrolled;

        public ScrollHelper(Control owner, Func<Rectangle> viewport)
        {
            _owner = owner;
            _viewport = viewport;
        }
        private void Ensure()
        {
            if (_vertical != null) return;
            _vertical = new AC.Primitives.ScrollBar { Orientation = Avalonia.Layout.Orientation.Vertical, AllowAutoHide = false, IsVisible = false };
            _horizontal = new AC.Primitives.ScrollBar { Orientation = Avalonia.Layout.Orientation.Horizontal, AllowAutoHide = false, IsVisible = false };
            _vertical.ValueChanged += (s, e) => { if (!_updating) SetOffset(OffsetX, (int)Math.Round(e.NewValue)); };
            _horizontal.ValueChanged += (s, e) => { if (!_updating) SetOffset((int)Math.Round(e.NewValue), OffsetY); };
            ControlHost host = (ControlHost)_owner.NativeControl;
            host.AddOverlay(_vertical, () =>
            {
                Padding insets = _owner.NonClientInsets;
                Size client = _owner.ClientSize;
                int w = SystemInformation.VerticalScrollBarWidth;
                return new Rect(insets.Left + client.Width - w, insets.Top, w, Math.Max(0, client.Height - (HorizontalVisible ? SystemInformation.HorizontalScrollBarHeight : 0)));
            });
            host.AddOverlay(_horizontal, () =>
            {
                Padding insets = _owner.NonClientInsets;
                Size client = _owner.ClientSize;
                int h = SystemInformation.HorizontalScrollBarHeight;
                return new Rect(insets.Left, insets.Top + client.Height - h, Math.Max(0, client.Width - (VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0)), h);
            });
        }
        // The size of the content; the scroll bars are shown when it does not fit
        public void SetExtent(int width, int height, int fixedTop = 0)
        {
            Ensure();
            ExtentWidth = width;
            ExtentHeight = height;
            Size client = _owner.ClientSize;
            bool needV = height > client.Height - fixedTop;
            bool needH = width > client.Width - (needV ? SystemInformation.VerticalScrollBarWidth : 0);
            if (needH && !needV) needV = height > client.Height - fixedTop - SystemInformation.HorizontalScrollBarHeight;
            int viewW = client.Width - (needV ? SystemInformation.VerticalScrollBarWidth : 0);
            int viewH = client.Height - fixedTop - (needH ? SystemInformation.HorizontalScrollBarHeight : 0);
            _updating = true;
            _vertical.IsVisible = needV;
            _horizontal.IsVisible = needH;
            _vertical.Maximum = Math.Max(0, height - viewH);
            _vertical.ViewportSize = Math.Max(1, viewH);
            _vertical.LargeChange = Math.Max(1, viewH);
            _vertical.SmallChange = SmallChangeY;
            _horizontal.Maximum = Math.Max(0, width - viewW);
            _horizontal.ViewportSize = Math.Max(1, viewW);
            _horizontal.LargeChange = Math.Max(1, viewW);
            _horizontal.SmallChange = 16;
            _updating = false;
            SetOffset(needH ? Math.Min(OffsetX, (int)_horizontal.Maximum) : 0, needV ? Math.Min(OffsetY, (int)_vertical.Maximum) : 0);
            _owner.NativeControl.InvalidateArrange();
        }
        public void SetOffset(int x, int y)
        {
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            if (_vertical != null)
            {
                x = HorizontalVisible ? Math.Min(x, (int)_horizontal.Maximum) : 0;
                y = VerticalVisible ? Math.Min(y, (int)_vertical.Maximum) : 0;
            }
            if (x == OffsetX && y == OffsetY) return;
            OffsetX = x;
            OffsetY = y;
            if (_vertical != null)
            {
                _updating = true;
                _vertical.Value = y;
                _horizontal.Value = x;
                _updating = false;
            }
            _owner.Invalidate();
            Scrolled?.Invoke();
        }
        public bool Wheel(int delta)
        {
            if (!VerticalVisible) return false;
            SetOffset(OffsetX, OffsetY - delta * SystemInformation.MouseWheelScrollLines * SmallChangeY / 120);
            return true;
        }
        // Scrolls so that the rectangle (content coordinates) is visible
        public void EnsureVisible(Rectangle r, int fixedTop = 0)
        {
            Rectangle view = _viewport();
            int viewHeight = view.Height - fixedTop;
            int y = OffsetY;
            if (r.Top < y) y = r.Top;
            else if (r.Bottom > y + viewHeight) y = r.Bottom - viewHeight;
            SetOffset(OffsetX, y);
        }
    }
}
