// PrePoMax for macOS - ControlPaint, TextRenderer, the group box renderer, SendKeys and the binding managers of WinForms.

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms.VisualStyles;

namespace System.Windows.Forms
{
    public enum ButtonBorderStyle { None = 0, Dotted = 1, Dashed = 2, Solid = 3, Inset = 4, Outset = 5 }

    [Flags]
    public enum Border3DSide { Left = 1, Top = 2, Right = 4, Bottom = 8, Middle = 2048, All = 2063 }

    public enum ButtonState_ { Normal = 0 }

    public sealed class ControlPaint
    {
        public static Color ContrastControlDark { get { return SystemColors.ControlDark; } }
        public static Color Dark(Color baseColor) { return Dark(baseColor, 0.5f); }
        public static Color Dark(Color baseColor, float percOfDarkDark)
        {
            float f = 1 - Math.Max(0, Math.Min(1, 0.3f + percOfDarkDark * 0.4f));
            return Color.FromArgb(baseColor.A, (int)(baseColor.R * f), (int)(baseColor.G * f), (int)(baseColor.B * f));
        }
        public static Color DarkDark(Color baseColor) { return Dark(baseColor, 1f); }
        public static Color Light(Color baseColor) { return Light(baseColor, 0.5f); }
        public static Color Light(Color baseColor, float percOfLightLight)
        {
            float f = Math.Max(0, Math.Min(1, 0.3f + percOfLightLight * 0.5f));
            return Color.FromArgb(baseColor.A, (int)(baseColor.R + (255 - baseColor.R) * f), (int)(baseColor.G + (255 - baseColor.G) * f), (int)(baseColor.B + (255 - baseColor.B) * f));
        }
        public static Color LightLight(Color baseColor) { return Light(baseColor, 1f); }
        public static void DrawBorder(Graphics graphics, Rectangle bounds, Color color, ButtonBorderStyle style)
        {
            if (style == ButtonBorderStyle.None) return;
            using (Pen pen = new Pen(color) { DashStyle = style == ButtonBorderStyle.Dotted ? DashStyle.Dot : style == ButtonBorderStyle.Dashed ? DashStyle.Dash : DashStyle.Solid })
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
        public static void DrawBorder(Graphics graphics, Rectangle bounds, Color leftColor, int leftWidth, ButtonBorderStyle leftStyle, Color topColor,
                                      int topWidth, ButtonBorderStyle topStyle, Color rightColor, int rightWidth, ButtonBorderStyle rightStyle,
                                      Color bottomColor, int bottomWidth, ButtonBorderStyle bottomStyle)
        {
            using (SolidBrush b = new SolidBrush(leftColor)) if (leftWidth > 0 && leftStyle != ButtonBorderStyle.None) graphics.FillRectangle(b, bounds.X, bounds.Y, leftWidth, bounds.Height);
            using (SolidBrush b = new SolidBrush(topColor)) if (topWidth > 0 && topStyle != ButtonBorderStyle.None) graphics.FillRectangle(b, bounds.X, bounds.Y, bounds.Width, topWidth);
            using (SolidBrush b = new SolidBrush(rightColor)) if (rightWidth > 0 && rightStyle != ButtonBorderStyle.None) graphics.FillRectangle(b, bounds.Right - rightWidth, bounds.Y, rightWidth, bounds.Height);
            using (SolidBrush b = new SolidBrush(bottomColor)) if (bottomWidth > 0 && bottomStyle != ButtonBorderStyle.None) graphics.FillRectangle(b, bounds.X, bounds.Bottom - bottomWidth, bounds.Width, bottomWidth);
        }
        public static void DrawBorder3D(Graphics graphics, Rectangle rectangle) { DrawBorder3D(graphics, rectangle, Border3DStyle.Etched); }
        public static void DrawBorder3D(Graphics graphics, Rectangle rectangle, Border3DStyle style)
        {
            using (Pen dark = new Pen(SystemColors.ControlDark)) using (Pen light = new Pen(SystemColors.ControlLightLight))
            {
                graphics.DrawRectangle(dark, rectangle.X, rectangle.Y, rectangle.Width - 2, rectangle.Height - 2);
                graphics.DrawRectangle(light, rectangle.X + 1, rectangle.Y + 1, rectangle.Width - 2, rectangle.Height - 2);
            }
        }
        public static void DrawBorder3D(Graphics graphics, Rectangle rectangle, Border3DStyle style, Border3DSide sides) { DrawBorder3D(graphics, rectangle, style); }
        public static void DrawFocusRectangle(Graphics graphics, Rectangle rectangle)
        {
            using (Pen pen = new Pen(Color.Black) { DashStyle = DashStyle.Dot }) graphics.DrawRectangle(pen, rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
        }
        public static void DrawFocusRectangle(Graphics graphics, Rectangle rectangle, Color foreColor, Color backColor) { DrawFocusRectangle(graphics, rectangle); }
        public static void DrawImageDisabled(Graphics graphics, Image image, int x, int y, Color background)
        {
            using (Image disabled = ToolStripRenderer.CreateDisabledImage(image)) graphics.DrawImage(disabled, x, y);
        }
        public static void DrawStringDisabled(Graphics graphics, string s, Font font, Color color, RectangleF layoutRectangle, StringFormat format)
        {
            using (SolidBrush b = new SolidBrush(SystemColors.GrayText)) graphics.DrawString(s, font, b, layoutRectangle, format);
        }
        public static void DrawStringDisabled(IDeviceContext dc, string s, Font font, Color color, Rectangle layoutRectangle, TextFormatFlags format)
        {
            if (dc is Graphics g) TextRenderer.DrawText(g, s, font, layoutRectangle, SystemColors.GrayText, format);
        }
        public static void DrawCheckBox(Graphics graphics, Rectangle rectangle, ButtonState state)
        {
            using (SolidBrush white = new SolidBrush(Color.White)) graphics.FillRectangle(white, rectangle);
            using (Pen border = new Pen(Color.FromArgb(0x33, 0x33, 0x33))) graphics.DrawRectangle(border, rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
            if ((state & ButtonState.Checked) != 0)
            {
                using (Pen mark = new Pen(Color.Black, 1.5f))
                    graphics.DrawLines(mark, new[] { new PointF(rectangle.X + 3, rectangle.Y + rectangle.Height / 2f), new PointF(rectangle.X + rectangle.Width / 2.5f, rectangle.Bottom - 3), new PointF(rectangle.Right - 3, rectangle.Y + 3) });
            }
        }
        public static void DrawCheckBox(Graphics graphics, int x, int y, int width, int height, ButtonState state) { DrawCheckBox(graphics, new Rectangle(x, y, width, height), state); }
        public static void DrawButton(Graphics graphics, Rectangle rectangle, ButtonState state)
        {
            using (SolidBrush face = new SolidBrush(Color.FromArgb(0xE1, 0xE1, 0xE1))) graphics.FillRectangle(face, rectangle);
            using (Pen border = new Pen(Color.FromArgb(0xAD, 0xAD, 0xAD))) graphics.DrawRectangle(border, rectangle.X, rectangle.Y, rectangle.Width - 1, rectangle.Height - 1);
        }
        public static void DrawGrid(Graphics graphics, Rectangle area, Size pixelsBetweenDots, Color backColor) { }
        public static void DrawReversibleFrame(Rectangle rectangle, Color backColor, FrameStyle style) { }
        public static void DrawReversibleLine(Point start, Point end, Color backColor) { }
        public static void FillReversibleRectangle(Rectangle rectangle, Color backColor) { }
    }

    [Flags]
    public enum ButtonState { Normal = 0, Inactive = 256, Pushed = 512, Checked = 1024, Flat = 16384, All = 18176 }

    public enum FrameStyle { Dashed = 0, Thick = 1 }

    /// <summary>Text drawing of GDI (TextRenderer); the padding of GDI is kept for measurements.</summary>
    public sealed class TextRenderer
    {
        public static Size MeasureText(string text, Font font) { return MeasureText(text, font, Size.Empty, TextFormatFlags.Default); }
        public static Size MeasureText(string text, Font font, Size proposedSize) { return MeasureText(text, font, proposedSize, TextFormatFlags.Default); }
        public static Size MeasureText(IDeviceContext dc, string text, Font font) { return MeasureText(text, font); }
        public static Size MeasureText(IDeviceContext dc, string text, Font font, Size proposedSize) { return MeasureText(text, font, proposedSize); }
        public static Size MeasureText(IDeviceContext dc, string text, Font font, Size proposedSize, TextFormatFlags flags) { return MeasureText(text, font, proposedSize, flags); }
        public static Size MeasureText(string text, Font font, Size proposedSize, TextFormatFlags flags)
        {
            if (string.IsNullOrEmpty(text)) return new Size(0, font.Height);
            bool wrap = (flags & TextFormatFlags.WordBreak) != 0 && proposedSize.Width > 0;
            Size size = ContentLayout.MeasureText(text, font, wrap ? proposedSize.Width : 0);
            int padding = (flags & TextFormatFlags.NoPadding) != 0 ? 0 : (int)Math.Round(font.SizeInPixels / 3);
            return new Size(size.Width + padding, size.Height);
        }
        public static void DrawText(IDeviceContext dc, string text, Font font, Point pt, Color foreColor) { DrawText(dc, text, font, pt, foreColor, TextFormatFlags.Default); }
        public static void DrawText(IDeviceContext dc, string text, Font font, Point pt, Color foreColor, Color backColor) { DrawText(dc, text, font, pt, foreColor, TextFormatFlags.Default); }
        public static void DrawText(IDeviceContext dc, string text, Font font, Point pt, Color foreColor, TextFormatFlags flags)
        {
            if (!(dc is Graphics g) || string.IsNullOrEmpty(text)) return;
            using (SolidBrush b = new SolidBrush(foreColor)) g.DrawString(text, font, b, pt.X + ((flags & TextFormatFlags.NoPadding) != 0 ? 0 : font.SizeInPixels / 6) - font.SizeInPixels / 6, pt.Y);
        }
        public static void DrawText(IDeviceContext dc, string text, Font font, Point pt, Color foreColor, Color backColor, TextFormatFlags flags) { DrawText(dc, text, font, pt, foreColor, flags); }
        public static void DrawText(IDeviceContext dc, string text, Font font, Rectangle bounds, Color foreColor) { DrawText(dc, text, font, bounds, foreColor, TextFormatFlags.Default); }
        public static void DrawText(IDeviceContext dc, string text, Font font, Rectangle bounds, Color foreColor, Color backColor) { DrawText(dc, text, font, bounds, foreColor, TextFormatFlags.Default); }
        public static void DrawText(IDeviceContext dc, string text, Font font, Rectangle bounds, Color foreColor, Color backColor, TextFormatFlags flags)
        {
            if (dc is Graphics g && backColor.A > 0) using (SolidBrush b = new SolidBrush(backColor)) g.FillRectangle(b, bounds);
            DrawText(dc, text, font, bounds, foreColor, flags);
        }
        public static void DrawText(IDeviceContext dc, string text, Font font, Rectangle bounds, Color foreColor, TextFormatFlags flags)
        {
            if (!(dc is Graphics g) || string.IsNullOrEmpty(text)) return;
            StringFormat format = new StringFormat();
            format.Alignment = (flags & TextFormatFlags.HorizontalCenter) != 0 ? StringAlignment.Center : (flags & TextFormatFlags.Right) != 0 ? StringAlignment.Far : StringAlignment.Near;
            format.LineAlignment = (flags & TextFormatFlags.VerticalCenter) != 0 ? StringAlignment.Center : (flags & TextFormatFlags.Bottom) != 0 ? StringAlignment.Far : StringAlignment.Near;
            if ((flags & TextFormatFlags.WordBreak) == 0) format.FormatFlags |= StringFormatFlags.NoWrap;
            if ((flags & (TextFormatFlags.EndEllipsis | TextFormatFlags.WordEllipsis | TextFormatFlags.PathEllipsis)) != 0) format.Trimming = StringTrimming.EllipsisCharacter;
            if ((flags & TextFormatFlags.NoClipping) != 0) format.FormatFlags |= StringFormatFlags.NoClip;
            string s = (flags & TextFormatFlags.NoPrefix) != 0 ? text : ContentLayout.StripMnemonic(text, true);
            using (SolidBrush b = new SolidBrush(foreColor)) g.DrawString(s, font, b, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), format);
        }
    }

    public static class SendKeys
    {
        // The keys PrePoMax sends: {TAB} moves into the editor of the selected property or to the next control
        public static void Send(string keys) { Avalonia.Threading.Dispatcher.UIThread.Post(() => Toolkit.Dispatch(() => SendNow(keys))); }
        public static void SendWait(string keys) { SendNow(keys); }
        public static void Flush() { }
        private static void SendNow(string keys)
        {
            if (string.IsNullOrEmpty(keys)) return;
            Control focused = Toolkit.FocusedControl;
            if (focused == null) return;
            if (string.Equals(keys, "{TAB}", StringComparison.OrdinalIgnoreCase))
            {
                for (Control c = focused; c != null; c = c.Parent)
                {
                    if (c is PropertyGrid grid)
                    {
                        PropertyGridEntry entry = grid.SelectedEntry;
                        if (entry != null && entry.GridItemType == GridItemType.Property && !entry.IsReadOnly) grid.GridView.BeginEdit(entry);
                        return;
                    }
                }
                focused.FindForm()?.SelectNextControl(focused, true, true, true, true);
            }
        }
    }

    public abstract class BindingManagerBase
    {
        public abstract int Count { get; }
        public abstract object Current { get; }
        public abstract int Position { get; set; }
        public bool IsBindingSuspended { get; protected set; }
        public event EventHandler CurrentChanged;
        public event EventHandler PositionChanged;
        public abstract void AddNew();
        public abstract void CancelCurrentEdit();
        public abstract void EndCurrentEdit();
        public abstract void RemoveAt(int index);
        public abstract void ResumeBinding();
        public abstract void SuspendBinding();
        public virtual PropertyDescriptorCollection GetItemProperties() { return TypeDescriptor.GetProperties(Current ?? new object()); }
        protected void OnCurrentChanged(EventArgs e) { CurrentChanged?.Invoke(this, e); }
        protected void OnPositionChanged(EventArgs e) { PositionChanged?.Invoke(this, e); }
    }

    public class CurrencyManager : BindingManagerBase
    {
        private readonly IList _list;
        private int _position;
        internal CurrencyManager(IList list) { _list = list ?? new ArrayList(); }
        public IList List { get { return _list; } }
        public override int Count { get { return _list.Count; } }
        public override object Current { get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; } }
        public override int Position
        {
            get { return _position; }
            set
            {
                if (_list is BindingSource bs) { bs.Position = value; _position = bs.Position; }
                else _position = Math.Max(-1, Math.Min(value, _list.Count - 1));
                OnPositionChanged(EventArgs.Empty);
                OnCurrentChanged(EventArgs.Empty);
            }
        }
        public override void AddNew() { if (_list is IBindingList bl) bl.AddNew(); }
        public override void CancelCurrentEdit()
        {
            if (_list is BindingSource bs) bs.CancelEdit();
            else if (Current is IEditableObject e) e.CancelEdit();
        }
        public override void EndCurrentEdit()
        {
            if (_list is BindingSource bs) bs.EndEdit();
            else if (Current is IEditableObject e) e.EndEdit();
        }
        public override void RemoveAt(int index) { _list.RemoveAt(index); }
        public override void ResumeBinding() { IsBindingSuspended = false; }
        public override void SuspendBinding() { IsBindingSuspended = true; }
        public void Refresh() { }
    }

    public class PropertyManager : BindingManagerBase
    {
        private readonly object _source;
        internal PropertyManager(object source) { _source = source; }
        public override int Count { get { return 1; } }
        public override object Current { get { return _source; } }
        public override int Position { get { return 0; } set { } }
        public override void AddNew() { }
        public override void CancelCurrentEdit() { if (_source is IEditableObject e) e.CancelEdit(); }
        public override void EndCurrentEdit() { if (_source is IEditableObject e) e.EndEdit(); }
        public override void RemoveAt(int index) { }
        public override void ResumeBinding() { }
        public override void SuspendBinding() { }
    }
}

namespace System.Windows.Forms.VisualStyles
{
    public enum GroupBoxState { Normal = 1, Disabled = 2 }
    public enum CheckBoxState { UncheckedNormal = 1, UncheckedHot = 2, UncheckedPressed = 3, UncheckedDisabled = 4, CheckedNormal = 5, CheckedHot = 6, CheckedPressed = 7, CheckedDisabled = 8 }
    public enum PushButtonState { Normal = 1, Hot = 2, Pressed = 3, Disabled = 4, Default = 5 }
    public enum RadioButtonState { UncheckedNormal = 1, UncheckedHot = 2, UncheckedPressed = 3, UncheckedDisabled = 4, CheckedNormal = 5, CheckedHot = 6, CheckedPressed = 7, CheckedDisabled = 8 }

    public static class GroupBoxRenderer_ { }
}

namespace System.Windows.Forms
{
    using System.Windows.Forms.VisualStyles;

    public sealed class GroupBoxRenderer
    {
        public static bool RenderMatchingApplicationState { get; set; } = true;
        public static void DrawGroupBox(Graphics g, Rectangle bounds, GroupBoxState state)
        {
            using (Pen pen = new Pen(Color.FromArgb(0xDC, 0xDC, 0xDC))) g.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
        public static void DrawGroupBox(Graphics g, Rectangle bounds, string groupBoxText, Font font, GroupBoxState state)
        {
            DrawGroupBox(g, bounds, groupBoxText, font, Color.Black, state);
        }
        public static void DrawGroupBox(Graphics g, Rectangle bounds, string groupBoxText, Font font, Color textColor, GroupBoxState state)
        {
            int h = font.Height;
            DrawGroupBox(g, new Rectangle(bounds.X, bounds.Y + h / 2, bounds.Width, bounds.Height - h / 2), state);
            Size size = TextRenderer.MeasureText(groupBoxText, font);
            using (SolidBrush back = new SolidBrush(SystemColors.Control)) g.FillRectangle(back, bounds.X + 6, bounds.Y, size.Width, h);
            TextRenderer.DrawText(g, groupBoxText, font, new Point(bounds.X + 8, bounds.Y), state == GroupBoxState.Disabled ? SystemColors.GrayText : textColor);
        }
        public static bool IsBackgroundPartiallyTransparent(GroupBoxState state) { return false; }
        public static void DrawParentBackground(Graphics g, Rectangle bounds, Control childControl) { }
    }

    public sealed class CheckBoxRenderer
    {
        public static void DrawCheckBox(Graphics g, Point glyphLocation, CheckBoxState state)
        {
            bool isChecked = state >= CheckBoxState.CheckedNormal;
            ControlPaint.DrawCheckBox(g, new Rectangle(glyphLocation, new Size(13, 13)), isChecked ? ButtonState.Checked : ButtonState.Normal);
        }
        public static Size GetGlyphSize(Graphics g, CheckBoxState state) { return new Size(13, 13); }
    }
}
