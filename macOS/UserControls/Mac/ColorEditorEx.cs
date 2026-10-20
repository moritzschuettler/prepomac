// PrePoMax for macOS - UserControls/ProperyGrid/ColorEditorEx.cs uses the private color picker of the WinForms color
// editor; on macOS the color editor of the compatibility layer is used.
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;

namespace UserControls
{
    public class ColorEditorEx : ColorEditor
    {
        public ColorEditorEx() { }
        public override void PaintValue(PaintValueEventArgs e)
        {
            if (e.Value is Color c)
            {
                using (SolidBrush brush = new SolidBrush(c)) e.Graphics.FillRectangle(brush, e.Bounds);
            }
        }
    }
}
