// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UserControls
{
    // The property grid cannot color the text of a single row. The grid view is painted into a bitmap, the text of the
    // missing values is recolored there and the bitmap is then copied to the screen, so the uncolored text is never
    // shown. The grid view and its row geometry are internal and are reached by reflection
    public class PropertyGridMissingValuePainter : NativeWindow
    {
        // Variables
        public static readonly Color MissingValueColor = Color.Red;
        private const int WM_PAINT = 0x000F;
        private const int RowValueRectangle = 2;
        private static readonly FieldInfo _gridViewField =
            typeof(PropertyGrid).GetField("gridView", BindingFlags.Instance | BindingFlags.NonPublic);
        //
        private readonly PropertyGrid _propertyGrid;
        private readonly Control _gridView;
        private readonly MethodInfo _getGridEntryFromRow;
        private readonly MethodInfo _getRectangle;
        private readonly FieldInfo _visibleRows;


        // Structures
        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT
        {
            public IntPtr Hdc;
            public bool Erase;
            public int PaintLeft;
            public int PaintTop;
            public int PaintRight;
            public int PaintBottom;
            public bool Restore;
            public bool IncUpdate;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] Reserved;
        }
        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT paintStruct);
        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT paintStruct);


        // Constructors
        public PropertyGridMissingValuePainter(PropertyGrid propertyGrid)
        {
            _propertyGrid = propertyGrid;
            _gridView = _gridViewField?.GetValue(propertyGrid) as Control;
            if (_gridView == null) return;
            //
            Type gridViewType = _gridView.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            _getGridEntryFromRow = gridViewType.GetMethod("GetGridEntryFromRow", flags);
            _getRectangle = gridViewType.GetMethod("GetRectangle", flags);
            _visibleRows = gridViewType.GetField("visibleRows", flags);
            if (_getGridEntryFromRow == null || _getRectangle == null || _visibleRows == null) return;
            //
            _gridView.HandleCreated += GridView_HandleCreated;
            _gridView.HandleDestroyed += GridView_HandleDestroyed;
            if (_gridView.IsHandleCreated) AssignHandle(_gridView.Handle);
        }


        // Event handlers
        private void GridView_HandleCreated(object sender, EventArgs e)
        {
            AssignHandle(_gridView.Handle);
        }
        private void GridView_HandleDestroyed(object sender, EventArgs e)
        {
            ReleaseHandle();
        }


        // Overrides
        protected override void WndProc(ref Message m)
        {
            if (m.Msg != WM_PAINT)
            {
                base.WndProc(ref m);
                return;
            }
            //
            IMissingPropertyValues missingPropertyValues = _propertyGrid.SelectedObject as IMissingPropertyValues;
            bool painted = false;
            try
            {
                if (m.WParam == IntPtr.Zero && GetMissingValueRectangles(missingPropertyValues).Count > 0)
                    painted = PaintWithMissingValues(ref m, missingPropertyValues);
            }
            catch { }
            if (!painted) base.WndProc(ref m);
            //
            try { SetEditBoxColor(missingPropertyValues); }
            catch { }
        }


        // Methods
        private bool PaintWithMissingValues(ref Message m, IMissingPropertyValues missingPropertyValues)
        {
            Rectangle clientRectangle = _gridView.ClientRectangle;
            if (clientRectangle.Width <= 0 || clientRectangle.Height <= 0) return false;
            //
            PAINTSTRUCT paintStruct;
            IntPtr screenHdc = BeginPaint(Handle, out paintStruct);
            try
            {
                using (Bitmap bitmap = new Bitmap(clientRectangle.Width, clientRectangle.Height,
                                                  PixelFormat.Format32bppRgb))
                {
                    using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
                    {
                        IntPtr bitmapHdc = bitmapGraphics.GetHdc();
                        try
                        {
                            m.WParam = bitmapHdc;
                            base.WndProc(ref m);
                        }
                        finally
                        {
                            m.WParam = IntPtr.Zero;
                            bitmapGraphics.ReleaseHdc(bitmapHdc);
                        }
                    }
                    //
                    foreach (Rectangle rectangle in GetMissingValueRectangles(missingPropertyValues))
                        RecolorText(bitmap, rectangle);
                    //
                    using (Graphics screenGraphics = Graphics.FromHdc(screenHdc))
                    {
                        screenGraphics.CompositingMode = CompositingMode.SourceCopy;
                        screenGraphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                        screenGraphics.DrawImage(bitmap, clientRectangle, clientRectangle, GraphicsUnit.Pixel);
                    }
                }
            }
            finally
            {
                EndPaint(Handle, ref paintStruct);
            }
            return true;
        }
        private List<Rectangle> GetMissingValueRectangles(IMissingPropertyValues missingPropertyValues)
        {
            List<Rectangle> rectangles = new List<Rectangle>();
            if (missingPropertyValues == null) return rectangles;
            //
            int visibleRows = (int)_visibleRows.GetValue(_gridView);
            for (int row = 0; row <= visibleRows; row++)
            {
                GridItem gridItem = _getGridEntryFromRow.Invoke(_gridView, new object[] { row }) as GridItem;
                if (IsValueMissing(missingPropertyValues, gridItem))
                    rectangles.Add((Rectangle)_getRectangle.Invoke(_gridView, new object[] { row, RowValueRectangle }));
            }
            return rectangles;
        }
        // The text is antialiased against the view back color, so each color channel tells how much of the pixel the
        // text covers. Blending the missing value color by the same amount keeps the glyphs and their position
        private void RecolorText(Bitmap bitmap, Rectangle valueRectangle)
        {
            Rectangle rectangle = Rectangle.Intersect(Rectangle.Inflate(valueRectangle, -1, -1),
                                                      new Rectangle(Point.Empty, bitmap.Size));
            if (rectangle.Width <= 0 || rectangle.Height <= 0) return;
            //
            Color backColor = _gridView.BackColor;
            Color foreColor = _gridView.ForeColor;
            byte[] back = { backColor.B, backColor.G, backColor.R };
            byte[] fore = { foreColor.B, foreColor.G, foreColor.R };
            byte[] target = { MissingValueColor.B, MissingValueColor.G, MissingValueColor.R };
            //
            BitmapData data = bitmap.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format32bppRgb);
            try
            {
                byte[] pixels = new byte[data.Stride * rectangle.Height];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                for (int y = 0; y < rectangle.Height; y++)
                {
                    for (int x = 0; x < rectangle.Width; x++)
                    {
                        int index = y * data.Stride + 4 * x;
                        for (int channel = 0; channel < 3; channel++)
                        {
                            int range = back[channel] - fore[channel];
                            if (range == 0) continue;
                            double coverage = (double)(back[channel] - pixels[index + channel]) / range;
                            if (coverage <= 0) continue;
                            if (coverage > 1) coverage = 1;
                            pixels[index + channel] =
                                (byte)Math.Round(back[channel] + coverage * (target[channel] - back[channel]));
                        }
                    }
                }
                Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
        private void SetEditBoxColor(IMissingPropertyValues missingPropertyValues)
        {
            Color foreColor = IsValueMissing(missingPropertyValues, _propertyGrid.SelectedGridItem) ?
                              MissingValueColor : _gridView.ForeColor;
            //
            foreach (Control control in _gridView.Controls)
            {
                if (control is TextBox textBox && textBox.ForeColor != foreColor) textBox.ForeColor = foreColor;
            }
        }
        private static bool IsValueMissing(IMissingPropertyValues missingPropertyValues, GridItem gridItem)
        {
            return missingPropertyValues != null && gridItem != null &&
                   gridItem.GridItemType == GridItemType.Property && gridItem.PropertyDescriptor != null &&
                   missingPropertyValues.IsPropertyValueMissing(gridItem.PropertyDescriptor.Name);
        }
    }
}
