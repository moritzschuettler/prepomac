// PrePoMax for macOS - design time services, the color editor and visual style elements of WinForms.

using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace System.Windows.Forms.Design
{
    public interface IUIService
    {
        IDictionary Styles { get; }
        bool CanShowComponentEditor(object component);
        IWin32Window GetDialogOwnerWindow();
        void SetUIDirty();
        bool ShowComponentEditor(object component, IWin32Window parent);
        DialogResult ShowDialog(Form form);
        void ShowError(string message);
        void ShowError(Exception ex);
        void ShowError(Exception ex, string message);
        void ShowMessage(string message);
        void ShowMessage(string message, string caption);
        DialogResult ShowMessage(string message, string caption, MessageBoxButtons buttons);
        bool ShowToolWindow(Guid toolWindow);
    }
}

namespace System.Drawing.Design
{
    /// <summary>The color editor of the property grid: a drop-down palette of colors.</summary>
    public class ColorEditor : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context) { return UITypeEditorEditStyle.DropDown; }
        public override bool GetPaintValueSupported(ITypeDescriptorContext context) { return true; }
        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is Color c)
                using (SolidBrush brush = new SolidBrush(c)) e.Graphics.FillRectangle(brush, e.Bounds);
        }
        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            IWindowsFormsEditorService service = provider?.GetService(typeof(IWindowsFormsEditorService)) as IWindowsFormsEditorService;
            if (service == null) return value;
            ColorPalette palette = new ColorPalette(service, value is Color c ? c : Color.Empty);
            service.DropDownControl(palette);
            return palette.Picked ? palette.Value : value;
        }
    }

    // A palette of the 48 custom colors of the WinForms color editor
    internal sealed class ColorPalette : Control
    {
        private static readonly Color[] Colors =
        {
            Color.FromArgb(255, 255, 255), Color.FromArgb(255, 192, 192), Color.FromArgb(255, 224, 192), Color.FromArgb(255, 255, 192),
            Color.FromArgb(192, 255, 192), Color.FromArgb(192, 255, 255), Color.FromArgb(192, 192, 255), Color.FromArgb(255, 192, 255),
            Color.FromArgb(224, 224, 224), Color.FromArgb(255, 128, 128), Color.FromArgb(255, 192, 128), Color.FromArgb(255, 255, 128),
            Color.FromArgb(128, 255, 128), Color.FromArgb(128, 255, 255), Color.FromArgb(128, 128, 255), Color.FromArgb(255, 128, 255),
            Color.FromArgb(192, 192, 192), Color.FromArgb(255, 0, 0), Color.FromArgb(255, 128, 0), Color.FromArgb(255, 255, 0),
            Color.FromArgb(0, 255, 0), Color.FromArgb(0, 255, 255), Color.FromArgb(0, 0, 255), Color.FromArgb(255, 0, 255),
            Color.FromArgb(128, 128, 128), Color.FromArgb(192, 0, 0), Color.FromArgb(192, 64, 0), Color.FromArgb(192, 192, 0),
            Color.FromArgb(0, 192, 0), Color.FromArgb(0, 192, 192), Color.FromArgb(0, 0, 192), Color.FromArgb(192, 0, 192),
            Color.FromArgb(64, 64, 64), Color.FromArgb(128, 0, 0), Color.FromArgb(128, 64, 0), Color.FromArgb(128, 128, 0),
            Color.FromArgb(0, 128, 0), Color.FromArgb(0, 128, 128), Color.FromArgb(0, 0, 128), Color.FromArgb(128, 0, 128),
            Color.FromArgb(0, 0, 0), Color.FromArgb(64, 0, 0), Color.FromArgb(128, 64, 64), Color.FromArgb(64, 64, 0),
            Color.FromArgb(0, 64, 0), Color.FromArgb(0, 64, 64), Color.FromArgb(0, 0, 64), Color.FromArgb(64, 0, 64),
        };
        private readonly IWindowsFormsEditorService _service;
        private readonly Color _current;
        public bool Picked;
        public Color Value;
        public ColorPalette(IWindowsFormsEditorService service, Color current)
        {
            _service = service;
            _current = current;
            Size = new Size(8 * 24 + 4, 6 * 24 + 4);
        }
        internal override bool PaintsItself { get { return true; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(SystemColors.Control);
            for (int i = 0; i < Colors.Length; i++)
            {
                Rectangle r = new Rectangle(4 + (i % 8) * 24, 4 + (i / 8) * 24, 20, 20);
                using (SolidBrush b = new SolidBrush(Colors[i])) e.Graphics.FillRectangle(b, r);
                e.Graphics.DrawRectangle(Colors[i].ToArgb() == _current.ToArgb() ? Pens.Black : Pens.Gray, r.X, r.Y, r.Width - 1, r.Height - 1);
            }
            e.Graphics.DrawRectangle(Pens.DarkGray, 0, 0, Width - 1, Height - 1);
            base.OnPaint(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            int column = (e.X - 4) / 24, row = (e.Y - 4) / 24;
            if (column >= 0 && column < 8 && row >= 0 && row < 6)
            {
                Value = Colors[row * 8 + column];
                Picked = true;
                _service.CloseDropDown();
            }
        }
    }
}

namespace System.Windows.Forms.VisualStyles
{
    public class VisualStyleElement
    {
        public string ClassName { get; }
        public int Part { get; }
        public int State { get; }
        private VisualStyleElement(string className, int part, int state) { ClassName = className; Part = part; State = state; }
        public static VisualStyleElement CreateElement(string className, int part, int state) { return new VisualStyleElement(className, part, state); }
        public static class ProgressBar
        {
            public static class Bar { public static VisualStyleElement Normal { get { return CreateElement("PROGRESS", 1, 0); } } }
        }
        public static class TaskbarClock
        {
            public static class Time { public static VisualStyleElement Normal { get { return CreateElement("CLOCK", 1, 1); } } }
        }
        public static class Button
        {
            public static class PushButton { public static VisualStyleElement Normal { get { return CreateElement("BUTTON", 1, 1); } } }
        }
        public static class TextBox
        {
            public static class TextEdit { public static VisualStyleElement Normal { get { return CreateElement("EDIT", 1, 1); } } }
            public static class Caret { public static VisualStyleElement Normal { get { return CreateElement("EDIT", 2, 0); } } }
        }
        public static class TreeView
        {
            public static class Item { public static VisualStyleElement Normal { get { return CreateElement("TREEVIEW", 1, 1); } } }
        }
        public static class ToolTip
        {
            public static class Standard { public static VisualStyleElement Normal { get { return CreateElement("TOOLTIP", 1, 1); } } }
        }
        public static class Window
        {
            public static class Caption { public static VisualStyleElement Active { get { return CreateElement("WINDOW", 1, 1); } } }
        }
    }
}
