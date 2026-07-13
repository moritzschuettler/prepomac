using System;
using System.Drawing;
using System.Windows.Forms;

namespace UserControls
{
    public class MenuStyleButton : Button
    {
        private readonly ProfessionalColorTable _colorTable;
        private bool _mouseOver;
        private bool _mouseDown;
        private bool _ignoreHoverUntilMouseLeave;
        private Color _backcolor = SystemColors.Control;
        public MenuStyleButton()
        {
            _colorTable = new ProfessionalColorTable();
            //
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            //
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            BackColor = _backcolor;
            UseVisualStyleBackColor = false;
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            _mouseOver = true;
            Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            _mouseOver = false;
            _mouseDown = false;
            _ignoreHoverUntilMouseLeave = false;
            Invalidate();
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
                _mouseDown = true;

            Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            _mouseDown = false;
            Invalidate();
        }
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            _mouseDown = false;
            Invalidate();
        }
        protected override void OnClick(EventArgs e)
        {
            try
            {
                base.OnClick(e);
            }
            finally
            {
                _mouseDown = false;
                _ignoreHoverUntilMouseLeave = ClientRectangle.Contains(PointToClient(Cursor.Position));
                Invalidate();
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            if (_ignoreHoverUntilMouseLeave)
                Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e)
        {
            base.OnMouseCaptureChanged(e);

            if (!Capture)
            {
                _mouseDown = false;
                Invalidate();
            }
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);

            _mouseDown = false;
            Invalidate();
        }
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            // The complete surface is painted in OnPaint.
        }
        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Rectangle bounds = ClientRectangle;

            using (SolidBrush brush = new SolidBrush(GetCurrentBackColor()))
                g.FillRectangle(brush, bounds);

            ControlPaint.DrawBorder(g, bounds, GetCurrentBorderColor(), ButtonBorderStyle.Solid);

            Rectangle contentBounds = new Rectangle(1, 1, Math.Max(0, Width - 2), Math.Max(0, Height - 2));
            DrawButtonImage(g, contentBounds);
            DrawButtonText(g, contentBounds);
        }
        private Color GetCurrentBackColor()
        {
            if (!Enabled) return SystemColors.Control;
            if (_mouseDown) return _colorTable.MenuItemPressedGradientBegin;
            if (_mouseOver && !_ignoreHoverUntilMouseLeave) return _colorTable.MenuItemSelected;
            return _backcolor;
        }
        private Color GetCurrentBorderColor()
        {
            if (!Enabled) return SystemColors.ControlDark;
            if (_mouseDown || (_mouseOver && !_ignoreHoverUntilMouseLeave)) return _colorTable.MenuItemBorder;
            return SystemColors.Control;
        }
        private void DrawButtonImage(Graphics g, Rectangle contentBounds)
        {
            if (Image == null) return;

            Rectangle imageBounds = GetAlignedRectangle(contentBounds, Image.Size, ImageAlign);

            if (Enabled)
                g.DrawImage(Image, imageBounds);
            else
                ControlPaint.DrawImageDisabled(g, Image, imageBounds.X, imageBounds.Y, GetCurrentBackColor());
        }
        private void DrawButtonText(Graphics g, Rectangle contentBounds)
        {
            if (string.IsNullOrEmpty(Text)) return;

            Color textColor = Enabled ? ForeColor : SystemColors.GrayText;
            TextRenderer.DrawText(g, Text, Font, contentBounds, textColor, GetTextFormatFlags(TextAlign));
        }
        private Rectangle GetAlignedRectangle(Rectangle bounds, Size size, ContentAlignment alignment)
        {
            int x;
            int y;

            if (alignment == ContentAlignment.TopLeft ||
                alignment == ContentAlignment.MiddleLeft ||
                alignment == ContentAlignment.BottomLeft)
                x = bounds.Left;
            else if (alignment == ContentAlignment.TopRight ||
                     alignment == ContentAlignment.MiddleRight ||
                     alignment == ContentAlignment.BottomRight)
                x = bounds.Right - size.Width;
            else
                x = bounds.Left + (bounds.Width - size.Width) / 2;

            if (alignment == ContentAlignment.TopLeft ||
                alignment == ContentAlignment.TopCenter ||
                alignment == ContentAlignment.TopRight)
                y = bounds.Top;
            else if (alignment == ContentAlignment.BottomLeft ||
                     alignment == ContentAlignment.BottomCenter ||
                     alignment == ContentAlignment.BottomRight)
                y = bounds.Bottom - size.Height;
            else
                y = bounds.Top + (bounds.Height - size.Height) / 2;

            return new Rectangle(x, y, size.Width, size.Height);
        }
        private TextFormatFlags GetTextFormatFlags(ContentAlignment alignment)
        {
            TextFormatFlags flags = TextFormatFlags.EndEllipsis |
                                    TextFormatFlags.SingleLine |
                                    TextFormatFlags.NoPrefix;

            if (alignment == ContentAlignment.TopLeft ||
                alignment == ContentAlignment.MiddleLeft ||
                alignment == ContentAlignment.BottomLeft)
                flags |= TextFormatFlags.Left;
            else if (alignment == ContentAlignment.TopRight ||
                     alignment == ContentAlignment.MiddleRight ||
                     alignment == ContentAlignment.BottomRight)
                flags |= TextFormatFlags.Right;
            else
                flags |= TextFormatFlags.HorizontalCenter;

            if (alignment == ContentAlignment.TopLeft ||
                alignment == ContentAlignment.TopCenter ||
                alignment == ContentAlignment.TopRight)
                flags |= TextFormatFlags.Top;
            else if (alignment == ContentAlignment.BottomLeft ||
                     alignment == ContentAlignment.BottomCenter ||
                     alignment == ContentAlignment.BottomRight)
                flags |= TextFormatFlags.Bottom;
            else
                flags |= TextFormatFlags.VerticalCenter;

            return flags;
        }
    }
}
