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
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace vtkControl.Native
{
    // The scalar bar of a native view, with the public surface of vtkMaxScalarBarWidget. The lookup table and the
    // labels are made here, because the labels use .NET number formats; the native widget lays them out, draws,
    // hit tests, drags and highlights. It belongs to its view and to that view's thread
    public sealed class VtkNativeScalarBar : IDisposable
    {
        // Variables
        public const int SmoothNumberOfColors = 256;
        //
        private IntPtr _handle;
        private readonly VtkNativeView _view;
        private readonly int _widgetId;
        private bool _visibility;
        private int _numberOfColors;
        private bool _smoothColors;
        private string _labelFormat;
        private bool _addMinColor;
        private bool _addMaxColor;
        private double _minUserValue;
        private double _maxUserValue;
        private bool _minColorVisible;
        private bool _maxColorVisible;
        private Color _minColor;
        private Color _maxColor;
        private string _unitAbbreviation;
        private double _editMinValue;
        private double _editMaxValue;
        private double[] _tableColors;
        private double _tableRangeMin;
        private double _tableRangeMax;
        private string[] _labels;


        // Properties
        public Color MinColor { get { return _minColor; } set { _minColor = value; } }
        public Color MaxColor { get { return _maxColor; } set { _maxColor = value; } }
        public bool MinColorVisible { get { return _minColorVisible; } set { _minColorVisible = value; } }
        public bool MaxColorVisible { get { return _maxColorVisible; } set { _maxColorVisible = value; } }
        public bool AddMinColor { get { return _addMinColor; } }
        public bool AddMaxColor { get { return _addMaxColor; } }
        public string UnitAbbreviation { get { return _unitAbbreviation; } }
        public int WidgetId { get { return _widgetId; } }
        public string[] Labels { get { return _labels == null ? new string[0] : (string[])_labels.Clone(); } }


        // Events
        public event Action<object, MouseEventArgs> MouseDoubleClick;


        // Constructors
        public VtkNativeScalarBar(VtkNativeView view)
        {
            if (view == null) throw new ArgumentNullException("view");
            _view = view;
            IntPtr handle;
            int widgetId;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_create(view.Handle(), out handle, out widgetId));
            _handle = handle;
            _widgetId = widgetId;
            _view.NativeEvent += View_NativeEvent;
            //
            _visibility = true;
            _numberOfColors = 12;
            _smoothColors = false;
            _labelFormat = "G3";
            _addMinColor = false;
            _addMaxColor = false;
            _minColorVisible = true;
            _maxColorVisible = true;
            _unitAbbreviation = "";
            _editMinValue = double.NaN;
            _editMaxValue = double.NaN;
            _tableColors = new double[0];
            _tableRangeMin = -5.5;
            _tableRangeMax = 11;
        }


        // Event handlers
        private void View_NativeEvent(object sender, VtkNativeEventArgs e)
        {
            if (e.Event != VtkNativeEvent.WidgetDoubleClick || e.Values.Length < 3) return;
            if ((int)e.Values[0] != _widgetId) return;
            MouseDoubleClick?.Invoke(this, new MouseEventArgs(MouseButtons.Left, 2, (int)e.Values[1], (int)e.Values[2], 0));
        }


        // Methods
        public void SetTextStyle(VtkNativeTextStyle style)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_text_style(Handle(), ref style));
        }
        public void SetPadding(int padding)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_padding(Handle(), padding));
        }
        public void SetVisibility(bool visibility)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_visibility(Handle(), visibility ? 1 : 0));
            _visibility = visibility;
        }
        public void SetFrame(bool backgroundVisible, double[] backgroundRgb, bool borderVisible, double[] borderRgb)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_frame(Handle(), backgroundVisible ? 1 : 0,
                                                                               backgroundRgb, borderVisible ? 1 : 0,
                                                                               borderRgb));
        }
        public void SetPosition(VtkNativeWidgetAnchor anchor, double x, double y)
        {
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_position(Handle(), (int)anchor, x, y));
        }
        public Rectangle GetRectangle()
        {
            int[] rectangle = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_get_rectangle(Handle(), rectangle));
            return ToRectangle(rectangle);
        }
        public bool GetHighlightRectangle(out Rectangle rectangle)
        {
            int[] values = new int[4];
            int visible;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_get_highlight_rectangle(Handle(), values,
                                                                                             out visible));
            rectangle = ToRectangle(values);
            return visible != 0;
        }
        //
        public void SetText(string fieldName, string componentName, string unitAbbreviation, string complexComponent,
                            string minMaxType)
        {
            fieldName = fieldName.Replace('_', ' ').Replace('-', ' ');
            componentName = componentName.Replace('_', ' ').Replace('-', ' ');
            //
            _unitAbbreviation = unitAbbreviation;
            string complexLabel;
            if (complexComponent == null || complexComponent.Length == 0) complexLabel = "";
            else complexLabel = "Complex: " + complexComponent + Environment.NewLine;
            //
            string text = complexLabel +
                          fieldName + ": " + componentName + Environment.NewLine +
                          "Unit: " + _unitAbbreviation + Environment.NewLine +
                          minMaxType;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_title(Handle(), VtkNativeLibrary.ToUtf8(text)));
        }
        public void SetSmoothColors(bool smoothColors)
        {
            _smoothColors = smoothColors;
        }
        public void SetNumberOfColors(int numOfColors)
        {
            if (_numberOfColors != numOfColors)
            {
                _numberOfColors = numOfColors;
                UpdateColors();
            }
        }
        public void SetLabelFormat(string labelFormat)
        {
            if (_labelFormat != labelFormat)
            {
                _labelFormat = labelFormat;
                UpdateColors();
            }
        }
        public string GetLabelFormat()
        {
            return _labelFormat;
        }
        public void CreateLookupTable(Func<double, double[]> colorAt, double scalarRangeMin, double scalarRangeMax)
        {
            CreateLookupTable(colorAt, scalarRangeMin, scalarRangeMax, double.NaN, double.NaN);
        }
        // colorAt is the color transfer function of the color spectrum, from 0 to 1
        public void CreateLookupTable(Func<double, double[]> colorAt, double scalarRangeMin, double scalarRangeMax,
                                      double minUserValue, double maxUserValue)
        {
            if (colorAt == null) throw new ArgumentNullException("colorAt");
            int numberOfLookupColors = _smoothColors ? SmoothNumberOfColors : _numberOfColors;
            //
            double min = scalarRangeMin;
            double max = scalarRangeMax;
            _minUserValue = minUserValue;
            _maxUserValue = maxUserValue;
            _editMinValue = double.IsNaN(minUserValue) ? scalarRangeMin : minUserValue;
            _editMaxValue = double.IsNaN(maxUserValue) ? scalarRangeMax : maxUserValue;
            _addMinColor = false;
            _addMaxColor = false;
            if (!double.IsNaN(minUserValue) && !double.IsNaN(maxUserValue))
            {
                min = Math.Min(scalarRangeMin, minUserValue);
                max = Math.Max(scalarRangeMax, maxUserValue);
                if (minUserValue > min && max != min) _addMinColor = true;
                if (maxUserValue < max && max != min) _addMaxColor = true;
            }
            _tableRangeMin = min;
            _tableRangeMax = max;
            //
            List<double> colors = new List<double>();
            double delta = 1.0 / (numberOfLookupColors - 1);
            if (_addMinColor) colors.AddRange(new double[] { _minColor.R / 256.0, _minColor.G / 256.0, _minColor.B / 256.0 });
            for (int i = 0; i < numberOfLookupColors; i++)
            {
                double[] color = colorAt(i * delta);
                colors.AddRange(new double[] { color[0], color[1], color[2] });
            }
            if (_addMaxColor) colors.AddRange(new double[] { _maxColor.R / 256.0, _maxColor.G / 256.0, _maxColor.B / 256.0 });
            _tableColors = colors.ToArray();
            //
            UpdateColors();
        }
        public bool TryGetValueEditData(int x, int y, out bool editMinimum, out Rectangle rectangle,
                                        out string valueText, out double minimum, out double maximum)
        {
            editMinimum = false;
            rectangle = Rectangle.Empty;
            valueText = null;
            minimum = _editMinValue;
            maximum = _editMaxValue;
            //
            if (!_visibility || double.IsNaN(minimum) || double.IsNaN(maximum)) return false;
            //
            int label;
            int[] values = new int[4];
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_hit_value_label(Handle(), x, y, out label, values));
            if (label == (int)VtkNativeValueLabel.Minimum)
            {
                editMinimum = true;
                rectangle = ToRectangle(values);
                valueText = _labels[GetMinimumLabelIndex()];
                return true;
            }
            else if (label == (int)VtkNativeValueLabel.Maximum)
            {
                rectangle = ToRectangle(values);
                valueText = _labels[GetMaximumLabelIndex()];
                return true;
            }
            //
            return false;
        }
        public bool TryGetValueEditData(bool editMinimum, out Rectangle rectangle, out string valueText,
                                        out double minimum, out double maximum)
        {
            int[] values = new int[4];
            int found;
            VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_get_value_label_rectangle(Handle(),
                                                                                               editMinimum ? 1 : 0,
                                                                                               values, out found));
            rectangle = ToRectangle(values);
            int labelIndex = editMinimum ? GetMinimumLabelIndex() : GetMaximumLabelIndex();
            valueText = _labels != null && labelIndex >= 0 && labelIndex < _labels.Length ? _labels[labelIndex] : null;
            minimum = _editMinValue;
            maximum = _editMaxValue;
            return found != 0 && valueText != null && !double.IsNaN(minimum) && !double.IsNaN(maximum);
        }
        //
        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;
            _view.NativeEvent -= View_NativeEvent;
            IntPtr handle = _handle;
            _handle = IntPtr.Zero;
            // A view that is gone has destroyed its scalar bar with it
            if (!_view.IsDisposed)
            {
                _view.Handle();
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_destroy(handle));
            }
        }
        //
        private IntPtr Handle()
        {
            if (_handle == IntPtr.Zero) throw new ObjectDisposedException("VtkNativeScalarBar");
            _view.Handle();
            return _handle;
        }
        // GenerateLabels: one label per box border from the top; the added colors are labeled with the table range
        private string[] GenerateLabels()
        {
            int numberOfLabels = _numberOfColors + 1;
            List<string> labels = new List<string>();
            double[] labelsRange = new double[] { _tableRangeMax, _tableRangeMin };
            if (_addMaxColor) labelsRange[0] = _maxUserValue;
            if (_addMinColor) labelsRange[1] = _minUserValue;
            //
            if (_addMaxColor && _maxColorVisible) labels.Add(GetString(_tableRangeMax));
            double delta = (labelsRange[1] - labelsRange[0]) / (numberOfLabels - 1);
            for (int i = 0; i < numberOfLabels; i++) labels.Add(GetString(labelsRange[0] + delta * i));
            if (_addMinColor && _minColorVisible) labels.Add(GetString(_tableRangeMin));
            return labels.ToArray();
        }
        private string GetString(double value)
        {
            string result = value.ToString(_labelFormat);
            if (value >= 0) result = "+" + result;
            if (value < 0) result = result.Insert(1, " ");
            return result;
        }
        private int GetMaximumLabelIndex()
        {
            return _addMaxColor && _maxColorVisible ? 1 : 0;
        }
        private int GetMinimumLabelIndex()
        {
            int count = _labels == null ? 0 : _labels.Length;
            return count - 1 - (_addMinColor && _minColorVisible ? 1 : 0);
        }
        // The native bar takes the table and the labels together; a table made for another number of colors waits
        // for the next CreateLookupTable, as the C# widget drew it only until then
        private void UpdateColors()
        {
            int gradientColors = _smoothColors ? SmoothNumberOfColors : _numberOfColors;
            int tableColors = gradientColors + (_addMinColor ? 1 : 0) + (_addMaxColor ? 1 : 0);
            if (_tableColors.Length != 3 * tableColors) return;
            //
            string[] labels = GenerateLabels();
            byte[] labelBytes = VtkNativeLibrary.ToUtf8List(labels);
            GCHandle colorsHandle = GCHandle.Alloc(_tableColors, GCHandleType.Pinned);
            GCHandle labelsHandle = GCHandle.Alloc(labelBytes, GCHandleType.Pinned);
            try
            {
                VtkNativeScalarBarColors colors = new VtkNativeScalarBarColors();
                colors.TableColors = colorsHandle.AddrOfPinnedObject();
                colors.TableColorCount = tableColors;
                colors.NumberOfColors = _numberOfColors;
                colors.SmoothColors = _smoothColors ? 1 : 0;
                colors.AddMinColor = _addMinColor ? 1 : 0;
                colors.AddMaxColor = _addMaxColor ? 1 : 0;
                colors.MinColorVisible = _minColorVisible ? 1 : 0;
                colors.MaxColorVisible = _maxColorVisible ? 1 : 0;
                colors.Labels = labelsHandle.AddrOfPinnedObject();
                VtkNativeLibrary.Check(VtkNativeMethods.vtkn_scalar_bar_set_colors(Handle(), ref colors));
                _labels = labels;
            }
            finally
            {
                labelsHandle.Free();
                colorsHandle.Free();
            }
        }
        private static Rectangle ToRectangle(int[] values)
        {
            return new Rectangle(values[0], values[1], values[2], values[3]);
        }
    }
}
