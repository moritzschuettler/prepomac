// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace vtkControl.Native
{
    // A widget of a native view besides the scalar bar (vtkn_widget): the probe text box, the status block, the scale
    // bar, the color bar, a text box with an arrow or the triad, with the surface of the vtkMax widget it ports. The
    // texts arrive formatted, with .NET formats; the scale bar asks for its labels through a formatter. It belongs
    // to its view and to that view's thread
    public sealed class VtkNativeWidget : IDisposable
    {
        // Variables
        private const int TargetClickedEvent = 20;
        private const int HoverChangedEvent = 21;
        //
        private IntPtr _handle;
        private readonly VtkNativeView _view;
        private readonly VtkNativeWidgetKind _kind;
        private readonly int _widgetId;
        private string _numberFormat;
        private VtkNativeMethods.NumberFormatter _formatter;


        // Properties
        public VtkNativeWidgetKind Kind { get { return _kind; } }
        public int WidgetId { get { return _widgetId; } }


        // Events
        public event Action<object, MouseEventArgs> MouseDoubleClick;
        public event Action<VtkNativeTriadTarget> TargetClicked;
        public event Action<bool> HoverChanged;


        // Constructors
        public VtkNativeWidget(VtkNativeView view, VtkNativeWidgetKind kind)
        {
            if (view == null) throw new ArgumentNullException("view");
            _view = view;
            _kind = kind;
            IntPtr handle;
            int widgetId;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_create(view.Handle(), (int)kind, out handle,
                                                                       out widgetId));
            _handle = handle;
            _widgetId = widgetId;
            _view.NativeEvent += View_NativeEvent;
        }


        // Event handlers
        private void View_NativeEvent(object sender, VtkNativeEventArgs e)
        {
            if (e.Values.Length < 2 || (int)e.Values[0] != _widgetId) return;
            int eventId = (int)e.Event;
            if (e.Event == VtkNativeEvent.WidgetDoubleClick && e.Values.Length >= 3)
                MouseDoubleClick?.Invoke(this, new MouseEventArgs(MouseButtons.Left, 2, (int)e.Values[1],
                                                                  (int)e.Values[2], 0));
            else if (eventId == TargetClickedEvent) TargetClicked?.Invoke((VtkNativeTriadTarget)(int)e.Values[1]);
            else if (eventId == HoverChangedEvent) HoverChanged?.Invoke(e.Values[1] != 0);
        }
        private int FormatNumber(IntPtr userData, double value, IntPtr buffer, int bufferSize)
        {
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value.ToString(_numberFormat) + "\0");
                if (bytes.Length <= bufferSize) Marshal.Copy(bytes, 0, buffer, bytes.Length);
                return bytes.Length;
            }
            catch
            {
                return 0;
            }
        }


        // Methods
        public void SetTextStyle(VtkNativeTextStyle style)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_text_style(Handle(), ref style));
        }
        public void SetPadding(int padding)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_padding(Handle(), padding));
        }
        public void SetVisibility(bool visibility)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_visibility(Handle(), visibility ? 1 : 0));
        }
        public void SetFrame(bool backgroundVisible, double[] backgroundRgb, bool borderVisible, double[] borderRgb)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_frame(Handle(), backgroundVisible ? 1 : 0,
                                                                           backgroundRgb, borderVisible ? 1 : 0,
                                                                           borderRgb));
        }
        public void SetPosition(VtkNativeWidgetAnchor anchor, double x, double y)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_position(Handle(), (int)anchor, x, y));
        }
        // The position measured from the anchor corner and the size: x, y, width, height
        public double[] GetPositionAndSize(out bool visible)
        {
            double[] values = new double[4];
            int visibility;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_get_geometry(Handle(), values, out visibility));
            visible = visibility != 0;
            return values;
        }
        public Rectangle GetRectangle()
        {
            int[] values = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_get_rectangle(Handle(), values));
            return new Rectangle(values[0], values[1], values[2], values[3]);
        }
        // The corners of the hover highlight, or null when none is shown
        public double[][] GetHighlightCorners()
        {
            double[] values = new double[8];
            int visible;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_get_highlight(Handle(), values, out visible));
            if (visible == 0) return null;
            return new double[][] { new double[] { values[0], values[1] }, new double[] { values[2], values[3] },
                                    new double[] { values[4], values[5] }, new double[] { values[6], values[7] } };
        }
        public void SetText(string text)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_set_text(Handle(), VtkNativeLibrary.ToUtf8(text ?? "")));
        }
        // Status block
        public void SetStatusLines(string line1, string line2, string line3Fixed, string line3Factor, string line4)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_status_block_set_lines(Handle(), VtkNativeLibrary.ToUtf8(line1),
                                                                                 VtkNativeLibrary.ToUtf8(line2),
                                                                                 VtkNativeLibrary.ToUtf8(line3Fixed),
                                                                                 VtkNativeLibrary.ToUtf8(line3Factor),
                                                                                 VtkNativeLibrary.ToUtf8(line4)));
        }
        public bool GetDeformationScaleFactorHit(out Rectangle rectangle)
        {
            int clicked;
            int[] values = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_status_block_get_scale_factor_hit(Handle(), out clicked,
                                                                                            values));
            rectangle = new Rectangle(values[0], values[1], values[2], values[3]);
            return clicked != 0;
        }
        // Scale bar
        public void SetScaleBarWidth(int width)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scale_bar_set_width(Handle(), width));
        }
        public void SetScaleBarUnit(string unit)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scale_bar_set_unit(Handle(), VtkNativeLibrary.ToUtf8(unit ?? "")));
        }
        // The labels are formatted here, with the .NET number format
        public void SetScaleBarNumberFormat(string numberFormat)
        {
            _numberFormat = numberFormat;
            if (_formatter == null) _formatter = FormatNumber;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scale_bar_set_number_formatter(Handle(), _formatter,
                                                                                         IntPtr.Zero));
        }
        public string[] GetScaleBarLabels()
        {
            return VtkNativeLibrary.GetStringList((byte[] buffer, int size, out int required) =>
                VtkNativeMethods.vtkn_scale_bar_get_labels(Handle(), buffer, size, out required));
        }
        // Color bar
        public void SetColorsAndLabels(Color[] colors, string[] labels)
        {
            int count = colors == null || labels == null ? 0 : Math.Min(colors.Length, labels.Length);
            double[] rgb = new double[3 * count];
            string[] names = new string[count];
            for (int i = 0; i < count; i++)
            {
                rgb[3 * i] = colors[i].R / 255.0;
                rgb[3 * i + 1] = colors[i].G / 255.0;
                rgb[3 * i + 2] = colors[i].B / 255.0;
                names[i] = labels[i];
            }
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_color_bar_set_colors_and_labels(Handle(), rgb,
                                                                                          VtkNativeLibrary.ToUtf8List(names),
                                                                                          count));
        }
        public bool GetColorBarTitleRectangle(out Rectangle rectangle)
        {
            int found;
            int[] values = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_color_bar_get_title_rectangle(Handle(), values, out found));
            rectangle = new Rectangle(values[0], values[1], values[2], values[3]);
            return found != 0;
        }
        // Text with arrow
        public void SetAnchorPoint(double x, double y, double z)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_arrow_set_anchor_point(Handle(), x, y, z));
        }
        public void ResetPositionFromAnchor()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_arrow_reset_position_from_anchor(Handle()));
        }
        public void ResetInitialPosition()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_arrow_reset_initial_position(Handle()));
        }
        public void OffsetPosition(double dx, double dy)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_arrow_offset_position(Handle(), dx, dy));
        }
        // The leader from the box to the arrow head: { start, end }, each x, y
        public double[][] GetLeaderLine()
        {
            double[] values = new double[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_arrow_get_leader_line(Handle(), values));
            return new double[][] { new double[] { values[0], values[1] }, new double[] { values[2], values[3] } };
        }
        // Triad
        public VtkNativeTriadTarget GetTriadTarget(int x, int y)
        {
            int target;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_triad_get_target(Handle(), x, y, out target));
            return (VtkNativeTriadTarget)target;
        }
        public void UpdateTriadGeometry()
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_triad_update_geometry(Handle()));
        }
        //
        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;
            _view.NativeEvent -= View_NativeEvent;
            IntPtr handle = _handle;
            _handle = IntPtr.Zero;
            // A view that is gone has destroyed its widgets with it
            if (!_view.IsDisposed)
            {
                _view.Handle();
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_widget_destroy(handle));
            }
            _formatter = null;
        }
        //
        private IntPtr Handle()
        {
            if (_handle == IntPtr.Zero) throw new ObjectDisposedException("VtkNativeWidget");
            _view.Handle();
            return _handle;
        }
    }
}
