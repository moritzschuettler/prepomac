// PrePoMax for macOS - matrices, transforms, color tables and math helpers of the VTK compatible library.

using System;
using System.Collections.Generic;

namespace Kitware.VTK
{
    public partial class vtkMatrix4x4
    {
        // Row major: element (i, j) is _e[4 * i + j]
        internal double[] _e = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

        public double GetElement(int i, int j) { return _e[4 * i + j]; }
        public void SetElement(int i, int j, double v) { _e[4 * i + j] = v; Modified(); }
        public void Identity() { _e = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 }; Modified(); }
        public void DeepCopy(vtkMatrix4x4 m) { _e = (double[])m._e.Clone(); Modified(); }
        public void DeepCopy(double[] elements) { _e = (double[])elements.Clone(); Modified(); }
        public void Invert() { _e = Inverse(_e); Modified(); }
        public static void Invert(vtkMatrix4x4 input, vtkMatrix4x4 output) { output._e = Inverse(input._e); output.Modified(); }
        public void Transpose()
        {
            double[] t = new double[16];
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) t[4 * j + i] = _e[4 * i + j];
            _e = t;
            Modified();
        }
        public static void Multiply4x4(vtkMatrix4x4 a, vtkMatrix4x4 b, vtkMatrix4x4 c)
        {
            c._e = Multiply(a._e, b._e);
            c.Modified();
        }
        public double[] MultiplyPoint(double[] p)
        {
            double[] r = new double[4];
            for (int i = 0; i < 4; i++)
                r[i] = _e[4 * i] * p[0] + _e[4 * i + 1] * p[1] + _e[4 * i + 2] * p[2] + _e[4 * i + 3] * (p.Length > 3 ? p[3] : 1);
            return r;
        }
        public double[] MultiplyDoublePoint(double[] p) { return MultiplyPoint(p); }
        public double Determinant() { return Determinant(_e); }
        internal double[] Elements { get { return _e; } set { _e = value; } }

        internal static double[] Multiply(double[] a, double[] b)
        {
            double[] r = new double[16];
            for (int i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                {
                    double v = 0;
                    for (int k = 0; k < 4; k++) v += a[4 * i + k] * b[4 * k + j];
                    r[4 * i + j] = v;
                }
            return r;
        }
        internal static double Determinant(double[] m)
        {
            double[] inv = Cofactors(m, out double det);
            return det;
        }
        internal static double[] Inverse(double[] m)
        {
            double[] inv = Cofactors(m, out double det);
            if (Math.Abs(det) < 1e-300) return new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
            for (int i = 0; i < 16; i++) inv[i] /= det;
            return inv;
        }
        private static double[] Cofactors(double[] m, out double det)
        {
            double[] inv = new double[16];
            inv[0] = m[5] * m[10] * m[15] - m[5] * m[11] * m[14] - m[9] * m[6] * m[15] + m[9] * m[7] * m[14] + m[13] * m[6] * m[11] - m[13] * m[7] * m[10];
            inv[4] = -m[4] * m[10] * m[15] + m[4] * m[11] * m[14] + m[8] * m[6] * m[15] - m[8] * m[7] * m[14] - m[12] * m[6] * m[11] + m[12] * m[7] * m[10];
            inv[8] = m[4] * m[9] * m[15] - m[4] * m[11] * m[13] - m[8] * m[5] * m[15] + m[8] * m[7] * m[13] + m[12] * m[5] * m[11] - m[12] * m[7] * m[9];
            inv[12] = -m[4] * m[9] * m[14] + m[4] * m[10] * m[13] + m[8] * m[5] * m[14] - m[8] * m[6] * m[13] - m[12] * m[5] * m[10] + m[12] * m[6] * m[9];
            inv[1] = -m[1] * m[10] * m[15] + m[1] * m[11] * m[14] + m[9] * m[2] * m[15] - m[9] * m[3] * m[14] - m[13] * m[2] * m[11] + m[13] * m[3] * m[10];
            inv[5] = m[0] * m[10] * m[15] - m[0] * m[11] * m[14] - m[8] * m[2] * m[15] + m[8] * m[3] * m[14] + m[12] * m[2] * m[11] - m[12] * m[3] * m[10];
            inv[9] = -m[0] * m[9] * m[15] + m[0] * m[11] * m[13] + m[8] * m[1] * m[15] - m[8] * m[3] * m[13] - m[12] * m[1] * m[11] + m[12] * m[3] * m[9];
            inv[13] = m[0] * m[9] * m[14] - m[0] * m[10] * m[13] - m[8] * m[1] * m[14] + m[8] * m[2] * m[13] + m[12] * m[1] * m[10] - m[12] * m[2] * m[9];
            inv[2] = m[1] * m[6] * m[15] - m[1] * m[7] * m[14] - m[5] * m[2] * m[15] + m[5] * m[3] * m[14] + m[13] * m[2] * m[7] - m[13] * m[3] * m[6];
            inv[6] = -m[0] * m[6] * m[15] + m[0] * m[7] * m[14] + m[4] * m[2] * m[15] - m[4] * m[3] * m[14] - m[12] * m[2] * m[7] + m[12] * m[3] * m[6];
            inv[10] = m[0] * m[5] * m[15] - m[0] * m[7] * m[13] - m[4] * m[1] * m[15] + m[4] * m[3] * m[13] + m[12] * m[1] * m[7] - m[12] * m[3] * m[5];
            inv[14] = -m[0] * m[5] * m[14] + m[0] * m[6] * m[13] + m[4] * m[1] * m[14] - m[4] * m[2] * m[13] - m[12] * m[1] * m[6] + m[12] * m[2] * m[5];
            inv[3] = -m[1] * m[6] * m[11] + m[1] * m[7] * m[10] + m[5] * m[2] * m[11] - m[5] * m[3] * m[10] - m[9] * m[2] * m[7] + m[9] * m[3] * m[6];
            inv[7] = m[0] * m[6] * m[11] - m[0] * m[7] * m[10] - m[4] * m[2] * m[11] + m[4] * m[3] * m[10] + m[8] * m[2] * m[7] - m[8] * m[3] * m[6];
            inv[11] = -m[0] * m[5] * m[11] + m[0] * m[7] * m[9] + m[4] * m[1] * m[11] - m[4] * m[3] * m[9] - m[8] * m[1] * m[7] + m[8] * m[3] * m[5];
            inv[15] = m[0] * m[5] * m[10] - m[0] * m[6] * m[9] - m[4] * m[1] * m[10] + m[4] * m[2] * m[9] + m[8] * m[1] * m[6] - m[8] * m[2] * m[5];
            det = m[0] * inv[0] + m[1] * inv[4] + m[2] * inv[8] + m[3] * inv[12];
            return inv;
        }
        internal static double[] Translation(double x, double y, double z)
        {
            return new double[] { 1, 0, 0, x, 0, 1, 0, y, 0, 0, 1, z, 0, 0, 0, 1 };
        }
        internal static double[] Scaling(double x, double y, double z)
        {
            return new double[] { x, 0, 0, 0, 0, y, 0, 0, 0, 0, z, 0, 0, 0, 0, 1 };
        }
        internal static double[] Rotation(double angleDeg, double x, double y, double z)
        {
            double l = Math.Sqrt(x * x + y * y + z * z);
            if (l == 0) return Scaling(1, 1, 1);
            x /= l; y /= l; z /= l;
            double a = angleDeg * Math.PI / 180, c = Math.Cos(a), s = Math.Sin(a), t = 1 - c;
            return new double[] {
                t * x * x + c,     t * x * y - s * z, t * x * z + s * y, 0,
                t * x * y + s * z, t * y * y + c,     t * y * z - s * x, 0,
                t * x * z - s * y, t * y * z + s * x, t * z * z + c,     0,
                0, 0, 0, 1 };
        }
        internal static double[] TransformPoint(double[] m, double x, double y, double z)
        {
            double w = m[12] * x + m[13] * y + m[14] * z + m[15];
            if (w == 0) w = 1;
            return new double[] {
                (m[0] * x + m[1] * y + m[2] * z + m[3]) / w,
                (m[4] * x + m[5] * y + m[6] * z + m[7]) / w,
                (m[8] * x + m[9] * y + m[10] * z + m[11]) / w };
        }
        internal static double[] TransformVector(double[] m, double x, double y, double z)
        {
            return new double[] {
                m[0] * x + m[1] * y + m[2] * z,
                m[4] * x + m[5] * y + m[6] * z,
                m[8] * x + m[9] * y + m[10] * z };
        }
    }

    // Transforms
    public partial class vtkAbstractTransform
    {
        public virtual double[] TransformPoint(double x, double y, double z) { return new double[] { x, y, z }; }
        public double[] TransformPoint(double[] p) { return TransformPoint(p[0], p[1], p[2]); }
        public double[] TransformDoublePoint(double x, double y, double z) { return TransformPoint(x, y, z); }
        public double[] TransformDoublePoint(double[] p) { return TransformPoint(p[0], p[1], p[2]); }
        public float[] TransformFloatPoint(float x, float y, float z)
        {
            double[] p = TransformPoint(x, y, z);
            return new float[] { (float)p[0], (float)p[1], (float)p[2] };
        }
        public virtual void TransformPoints(vtkPoints inPts, vtkPoints outPts)
        {
            long n = inPts.GetNumberOfPoints();
            for (long i = 0; i < n; i++) outPts.InsertNextPoint(TransformPoint(inPts.GetPoint(i)));
        }
        public virtual vtkAbstractTransform GetInverse() { return this; }
        public void Update() { }
    }

    public partial class vtkLinearTransform
    {
        internal double[] _m = new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };

        internal double[] Matrix { get { return _m; } }
        public override double[] TransformPoint(double x, double y, double z) { return vtkMatrix4x4.TransformPoint(_m, x, y, z); }
        public double[] TransformVector(double x, double y, double z) { return vtkMatrix4x4.TransformVector(_m, x, y, z); }
        public double[] TransformVector(double[] v) { return TransformVector(v[0], v[1], v[2]); }
        public double[] TransformDoubleVector(double x, double y, double z) { return TransformVector(x, y, z); }
        public double[] TransformDoubleVector(double[] v) { return TransformVector(v[0], v[1], v[2]); }
        public double[] TransformNormal(double x, double y, double z)
        {
            // The inverse transpose
            double[] inv = vtkMatrix4x4.Inverse(_m);
            double[] n = new double[] {
                inv[0] * x + inv[4] * y + inv[8] * z,
                inv[1] * x + inv[5] * y + inv[9] * z,
                inv[2] * x + inv[6] * y + inv[10] * z };
            double l = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            if (l > 0) { n[0] /= l; n[1] /= l; n[2] /= l; }
            return n;
        }
        public double[] TransformNormal(double[] n) { return TransformNormal(n[0], n[1], n[2]); }
        public double[] TransformDoubleNormal(double[] n) { return TransformNormal(n[0], n[1], n[2]); }
        public void TransformNormals(vtkDataArray inNms, vtkDataArray outNms)
        {
            long n = inNms.GetNumberOfTuples();
            for (long i = 0; i < n; i++) outNms.InsertNextTuple(TransformNormal(inNms.GetTuple3(i)));
        }
        public void TransformVectors(vtkDataArray inVrs, vtkDataArray outVrs)
        {
            long n = inVrs.GetNumberOfTuples();
            for (long i = 0; i < n; i++) outVrs.InsertNextTuple(TransformVector(inVrs.GetTuple3(i)));
        }
    }

    public partial class vtkTransform
    {
        private bool _postMultiply;

        public void Identity() { _m = vtkMatrix4x4.Scaling(1, 1, 1); Modified(); }
        public void PostMultiply() { _postMultiply = true; }
        public void PreMultiply() { _postMultiply = false; }
        public int GetPreMultiplyFlag() { return _postMultiply ? 0 : 1; }
        private void Apply(double[] t)
        {
            _m = _postMultiply ? vtkMatrix4x4.Multiply(t, _m) : vtkMatrix4x4.Multiply(_m, t);
            Modified();
        }
        public void Translate(double x, double y, double z) { Apply(vtkMatrix4x4.Translation(x, y, z)); }
        public void Translate(double[] x) { Translate(x[0], x[1], x[2]); }
        public void Scale(double x, double y, double z) { Apply(vtkMatrix4x4.Scaling(x, y, z)); }
        public void Scale(double[] s) { Scale(s[0], s[1], s[2]); }
        public void RotateWXYZ(double angle, double x, double y, double z) { Apply(vtkMatrix4x4.Rotation(angle, x, y, z)); }
        public void RotateWXYZ(double angle, double[] axis) { RotateWXYZ(angle, axis[0], axis[1], axis[2]); }
        public void RotateX(double angle) { RotateWXYZ(angle, 1, 0, 0); }
        public void RotateY(double angle) { RotateWXYZ(angle, 0, 1, 0); }
        public void RotateZ(double angle) { RotateWXYZ(angle, 0, 0, 1); }
        public void Concatenate(vtkMatrix4x4 matrix) { Apply((double[])matrix._e.Clone()); }
        public void Concatenate(double[] elements) { Apply((double[])elements.Clone()); }
        public void Concatenate(vtkLinearTransform transform) { Apply((double[])transform._m.Clone()); }
        public void SetMatrix(vtkMatrix4x4 matrix) { _m = (double[])matrix._e.Clone(); Modified(); }
        public void SetMatrix(double[] elements) { _m = (double[])elements.Clone(); Modified(); }
        public vtkMatrix4x4 GetMatrix()
        {
            vtkMatrix4x4 matrix = new vtkMatrix4x4();
            matrix._e = (double[])_m.Clone();
            return matrix;
        }
        public void GetMatrix(vtkMatrix4x4 matrix) { matrix._e = (double[])_m.Clone(); matrix.Modified(); }
        public void Inverse() { _m = vtkMatrix4x4.Inverse(_m); Modified(); }
        public override vtkAbstractTransform GetInverse()
        {
            vtkTransform inverse = new vtkTransform();
            inverse._m = vtkMatrix4x4.Inverse(_m);
            return inverse;
        }
        public vtkLinearTransform GetLinearInverse() { return (vtkLinearTransform)GetInverse(); }
        public void DeepCopy(vtkAbstractTransform transform)
        {
            if (transform is vtkLinearTransform lt) _m = (double[])lt._m.Clone();
            if (transform is vtkTransform t) _postMultiply = t._postMultiply;
            Modified();
        }
        public double[] GetPosition() { return new double[] { _m[3], _m[7], _m[11] }; }
        public double[] GetScale()
        {
            return new double[] {
                Math.Sqrt(_m[0] * _m[0] + _m[4] * _m[4] + _m[8] * _m[8]),
                Math.Sqrt(_m[1] * _m[1] + _m[5] * _m[5] + _m[9] * _m[9]),
                Math.Sqrt(_m[2] * _m[2] + _m[6] * _m[6] + _m[10] * _m[10]) };
        }
        // The rotation as angle and axis
        public double[] GetOrientationWXYZ()
        {
            double[] s = GetScale();
            double r00 = _m[0] / s[0], r01 = _m[1] / s[1], r02 = _m[2] / s[2];
            double r10 = _m[4] / s[0], r11 = _m[5] / s[1], r12 = _m[6] / s[2];
            double r20 = _m[8] / s[0], r21 = _m[9] / s[1], r22 = _m[10] / s[2];
            double cos = Math.Max(-1, Math.Min(1, (r00 + r11 + r22 - 1) / 2));
            double angle = Math.Acos(cos);
            double x = r21 - r12, y = r02 - r20, z = r10 - r01;
            double l = Math.Sqrt(x * x + y * y + z * z);
            if (l < 1e-12) return new double[] { 0, 0, 0, 1 };
            return new double[] { angle * 180 / Math.PI, x / l, y / l, z / l };
        }
        // The rotations about z, x and y in degrees, the order of vtkProp3D
        public double[] GetOrientation()
        {
            double[] s = GetScale();
            double r00 = _m[0] / s[0], r01 = _m[1] / s[1], r02 = _m[2] / s[2];
            double r10 = _m[4] / s[0], r11 = _m[5] / s[1], r12 = _m[6] / s[2];
            double r20 = _m[8] / s[0], r21 = _m[9] / s[1], r22 = _m[10] / s[2];
            double x = Math.Asin(Math.Max(-1, Math.Min(1, r21)));
            double y, z;
            if (Math.Abs(Math.Cos(x)) > 1e-9)
            {
                y = Math.Atan2(-r20, r22);
                z = Math.Atan2(-r01, r11);
            }
            else
            {
                y = Math.Atan2(r02, r00);
                z = 0;
            }
            double k = 180 / Math.PI;
            return new double[] { x * k, y * k, z * k };
        }
        public void SetInput(vtkLinearTransform input) { if (input != null) _m = (double[])input._m.Clone(); }
    }

    // Color tables
    public partial class vtkScalarsToColors
    {
        protected double[] _range = new double[] { 0, 1 };
        protected double _alpha = 1;
        private int _vectorMode;

        public virtual double[] GetRange() { return (double[])_range.Clone(); }
        public virtual void SetRange(double min, double max) { _range = new double[] { min, max }; Modified(); }
        public void SetRange(double[] range) { SetRange(range[0], range[1]); }
        public virtual double[] GetColor(double v) { return new double[] { 1, 1, 1 }; }
        public virtual void GetColor(double v, double[] rgb)
        {
            double[] c = GetColor(v);
            rgb[0] = c[0]; rgb[1] = c[1]; rgb[2] = c[2];
        }
        public virtual double GetOpacity(double v) { return _alpha; }
        public void SetAlpha(double a) { _alpha = a; }
        public void SetVectorModeToMagnitude() { _vectorMode = 0; }
        public void SetVectorModeToComponent() { _vectorMode = 1; }
        public virtual void Build() { }
        public virtual long GetNumberOfAvailableColors() { return 256; }
        // The color of a value as bytes
        public byte[] MapValue(double v)
        {
            double[] c = GetColor(v);
            return new byte[] { (byte)(c[0] * 255 + 0.5), (byte)(c[1] * 255 + 0.5), (byte)(c[2] * 255 + 0.5),
                                (byte)(GetOpacity(v) * 255 + 0.5) };
        }
    }

    public partial class vtkLookupTable
    {
        private double[][] _table = new double[0][];
        private double[] _hueRange = new double[] { 0, 0.66667 };
        private double[] _saturationRange = new double[] { 1, 1 };
        private double[] _valueRange = new double[] { 1, 1 };
        private double[] _alphaRange = new double[] { 1, 1 };
        private double[] _belowRangeColor = new double[] { 0, 0, 0, 1 };
        private double[] _aboveRangeColor = new double[] { 1, 1, 1, 1 };
        private bool _useBelowRangeColor;
        private bool _useAboveRangeColor;
        private bool _built;
        private int _numberOfColors = 256;
        private int _scale;

        public vtkLookupTable()
        {
            _numberOfColors = 256;
        }
        public double[] GetTableRange() { return (double[])_range.Clone(); }
        public void SetTableRange(double min, double max) { _range = new double[] { min, max }; Modified(); }
        public void SetTableRange(double[] range) { SetTableRange(range[0], range[1]); }
        public override void SetRange(double min, double max) { SetTableRange(min, max); }
        public long GetNumberOfColors() { return _table.Length > 0 ? _table.Length : _numberOfColors; }
        public void SetNumberOfColors(long n)
        {
            _numberOfColors = (int)n;
            if (_table.Length != n) SetNumberOfTableValues(n);
        }
        public long GetNumberOfTableValues() { return GetNumberOfColors(); }
        public void SetNumberOfTableValues(long count)
        {
            Array.Resize(ref _table, (int)count);
            for (int i = 0; i < _table.Length; i++) if (_table[i] == null) _table[i] = new double[] { 0, 0, 0, 1 };
            _numberOfColors = (int)count;
            _built = false;
            Modified();
        }
        public override long GetNumberOfAvailableColors() { return GetNumberOfColors(); }
        public double[] GetTableValue(long index)
        {
            Build();
            return (double[])_table[index].Clone();
        }
        public void GetTableValue(long index, double[] rgba)
        {
            Build();
            Array.Copy(_table[index], rgba, 4);
        }
        public void SetTableValue(long index, double r, double g, double b, double a)
        {
            if (index >= _table.Length) SetNumberOfTableValues(index + 1);
            _table[index] = new double[] { r, g, b, a };
            _built = true;
            Modified();
        }
        public void SetTableValue(long index, double[] rgba) { SetTableValue(index, rgba[0], rgba[1], rgba[2], rgba[3]); }
        public void SetHueRange(double a, double b) { _hueRange = new double[] { a, b }; _built = false; }
        public void SetSaturationRange(double a, double b) { _saturationRange = new double[] { a, b }; _built = false; }
        public void SetValueRange(double a, double b) { _valueRange = new double[] { a, b }; _built = false; }
        public void SetAlphaRange(double a, double b) { _alphaRange = new double[] { a, b }; _built = false; }
        public void SetBelowRangeColor(double r, double g, double b, double a) { _belowRangeColor = new double[] { r, g, b, a }; }
        public void SetAboveRangeColor(double r, double g, double b, double a) { _aboveRangeColor = new double[] { r, g, b, a }; }
        public void UseBelowRangeColorOn() { _useBelowRangeColor = true; }
        public void UseBelowRangeColorOff() { _useBelowRangeColor = false; }
        public void UseAboveRangeColorOn() { _useAboveRangeColor = true; }
        public void UseAboveRangeColorOff() { _useAboveRangeColor = false; }
        public void SetScaleToLinear() { _scale = 0; }
        public void SetScaleToLog10() { _scale = 1; }
        public void SetRampToLinear() { }
        private double[] _nanColor = { 0.5, 0, 0, 1 };
        public void SetNanColor(double r, double g, double b, double a) { _nanColor = new[] { r, g, b, a }; Modified(); }
        public double[] GetNanColor() { return (double[])_nanColor.Clone(); }
        internal double[] NanColor { get { return _nanColor; } }
        public void ForceBuild() { _built = false; Build(); }
        public override void Build()
        {
            if (_built && _table.Length > 0) return;
            if (_table.Length != _numberOfColors) Array.Resize(ref _table, _numberOfColors);
            int n = _table.Length;
            for (int i = 0; i < n; i++)
            {
                double t = n > 1 ? (double)i / (n - 1) : 0;
                double h = _hueRange[0] + t * (_hueRange[1] - _hueRange[0]);
                double s = _saturationRange[0] + t * (_saturationRange[1] - _saturationRange[0]);
                double v = _valueRange[0] + t * (_valueRange[1] - _valueRange[0]);
                double a = _alphaRange[0] + t * (_alphaRange[1] - _alphaRange[0]);
                double[] rgb = vtkMath.HSVToRGB(h, s, v);
                _table[i] = new double[] { rgb[0], rgb[1], rgb[2], a };
            }
            _built = true;
        }
        public long GetIndex(double v)
        {
            Build();
            int n = _table.Length;
            if (n == 0) return -1;
            double min = _range[0], max = _range[1];
            if (_scale == 1 && min > 0 && max > 0 && v > 0)
            {
                v = Math.Log10(v);
                min = Math.Log10(min);
                max = Math.Log10(max);
            }
            if (double.IsNaN(v)) return -1;
            if (max <= min) return v < min ? 0 : n - 1;
            double t = (v - min) / (max - min);
            if (t < 0) return _useBelowRangeColor ? -2 : 0;
            if (t > 1) return _useAboveRangeColor ? -3 : n - 1;
            long index = (long)(t * n);
            if (index >= n) index = n - 1;
            return index;
        }
        public override double[] GetColor(double v)
        {
            double[] c = GetColorRGBA(v);
            return new double[] { c[0], c[1], c[2] };
        }
        internal double[] GetColorRGBA(double v)
        {
            long index = GetIndex(v);
            if (index == -2) return (double[])_belowRangeColor.Clone();
            if (index == -3) return (double[])_aboveRangeColor.Clone();
            if (index < 0) return (double[])_nanColor.Clone();
            return (double[])_table[index].Clone();
        }
        public override double GetOpacity(double v) { return GetColorRGBA(v)[3]; }
        public void DeepCopy(vtkScalarsToColors lut)
        {
            if (lut is vtkLookupTable l)
            {
                _table = new double[l._table.Length][];
                for (int i = 0; i < _table.Length; i++) _table[i] = (double[])l._table[i]?.Clone();
                _range = (double[])l._range.Clone();
                _numberOfColors = l._numberOfColors;
                _built = l._built;
                _scale = l._scale;
                _useAboveRangeColor = l._useAboveRangeColor;
                _useBelowRangeColor = l._useBelowRangeColor;
                _aboveRangeColor = (double[])l._aboveRangeColor.Clone();
                _belowRangeColor = (double[])l._belowRangeColor.Clone();
            }
            Modified();
        }
        internal double[][] Table { get { Build(); return _table; } }
    }

    public partial class vtkColorTransferFunction
    {
        private enum ColorSpace { RGB, HSV, Lab, Diverging }
        private readonly List<double[]> _points = new List<double[]>();     // x, r, g, b
        private ColorSpace _space = ColorSpace.RGB;

        public void SetColorSpaceToRGB() { _space = ColorSpace.RGB; }
        public void SetColorSpaceToHSV() { _space = ColorSpace.HSV; }
        public void SetColorSpaceToLab() { _space = ColorSpace.Lab; }
        public void SetColorSpaceToDiverging() { _space = ColorSpace.Diverging; }
        public int AddRGBPoint(double x, double r, double g, double b)
        {
            _points.RemoveAll(p => p[0] == x);
            _points.Add(new double[] { x, r, g, b });
            _points.Sort((p1, p2) => p1[0].CompareTo(p2[0]));
            Modified();
            return _points.Count - 1;
        }
        public void RemoveAllPoints() { _points.Clear(); }
        public int GetSize() { return _points.Count; }
        public override double[] GetRange()
        {
            if (_points.Count == 0) return new double[] { 0, 0 };
            return new double[] { _points[0][0], _points[_points.Count - 1][0] };
        }
        public override double[] GetColor(double x)
        {
            if (_points.Count == 0) return new double[] { 0, 0, 0 };
            if (x <= _points[0][0]) return new double[] { _points[0][1], _points[0][2], _points[0][3] };
            double[] last = _points[_points.Count - 1];
            if (x >= last[0]) return new double[] { last[1], last[2], last[3] };
            int i = 0;
            while (i < _points.Count - 2 && x > _points[i + 1][0]) i++;
            double[] p1 = _points[i];
            double[] p2 = _points[i + 1];
            double t = (x - p1[0]) / (p2[0] - p1[0]);
            double[] c1 = new double[] { p1[1], p1[2], p1[3] };
            double[] c2 = new double[] { p2[1], p2[2], p2[3] };
            switch (_space)
            {
                case ColorSpace.HSV:
                    {
                        double[] h1 = vtkMath.RGBToHSV(c1[0], c1[1], c1[2]);
                        double[] h2 = vtkMath.RGBToHSV(c2[0], c2[1], c2[2]);
                        // VTK interpolates the hue the short way unless HSVWrap is off; the rainbow goes the long way
                        double h = h1[0] + t * (h2[0] - h1[0]);
                        return vtkMath.HSVToRGB(h, h1[1] + t * (h2[1] - h1[1]), h1[2] + t * (h2[2] - h1[2]));
                    }
                case ColorSpace.Lab:
                    {
                        double[] l1 = vtkMath.RGBToLab(c1);
                        double[] l2 = vtkMath.RGBToLab(c2);
                        return vtkMath.LabToRGB(new double[] { l1[0] + t * (l2[0] - l1[0]), l1[1] + t * (l2[1] - l1[1]),
                                                               l1[2] + t * (l2[2] - l1[2]) });
                    }
                case ColorSpace.Diverging:
                    return vtkMath.InterpolateDiverging(t, c1, c2);
                default:
                    return new double[] { c1[0] + t * (c2[0] - c1[0]), c1[1] + t * (c2[1] - c1[1]), c1[2] + t * (c2[2] - c1[2]) };
            }
        }
        public double GetRedValue(double x) { return GetColor(x)[0]; }
        public double GetGreenValue(double x) { return GetColor(x)[1]; }
        public double GetBlueValue(double x) { return GetColor(x)[2]; }
        public void DeepCopy(vtkColorTransferFunction f)
        {
            _points.Clear();
            foreach (double[] p in f._points) _points.Add((double[])p.Clone());
            _space = f._space;
        }
    }

    // Math
    public partial class vtkMath
    {
        public static double Pi() { return Math.PI; }
        public static double RadiansFromDegrees(double d) { return d * Math.PI / 180; }
        public static double DegreesFromRadians(double r) { return r * 180 / Math.PI; }
        public static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
        public static void Cross(double[] a, double[] b, double[] c)
        {
            double x = a[1] * b[2] - a[2] * b[1], y = a[2] * b[0] - a[0] * b[2], z = a[0] * b[1] - a[1] * b[0];
            c[0] = x; c[1] = y; c[2] = z;
        }
        public static double[] Cross(double[] a, double[] b)
        {
            double[] c = new double[3];
            Cross(a, b, c);
            return c;
        }
        public static double Norm(double[] v) { return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]); }
        public static double Normalize(double[] v)
        {
            double l = Norm(v);
            if (l != 0) { v[0] /= l; v[1] /= l; v[2] /= l; }
            return l;
        }
        public static double Distance2BetweenPoints(double[] a, double[] b)
        {
            double x = a[0] - b[0], y = a[1] - b[1], z = a[2] - b[2];
            return x * x + y * y + z * z;
        }
        public static void Subtract(double[] a, double[] b, double[] c) { c[0] = a[0] - b[0]; c[1] = a[1] - b[1]; c[2] = a[2] - b[2]; }
        public static void Add(double[] a, double[] b, double[] c) { c[0] = a[0] + b[0]; c[1] = a[1] + b[1]; c[2] = a[2] + b[2]; }
        public static void MultiplyScalar(double[] a, double s) { a[0] *= s; a[1] *= s; a[2] *= s; }
        public static void UninitializeBounds(double[] b)
        {
            b[0] = 1; b[1] = -1; b[2] = 1; b[3] = -1; b[4] = 1; b[5] = -1;
        }
        public static bool AreBoundsInitialized(double[] b) { return b[1] - b[0] >= 0; }
        public static double[] HSVToRGB(double h, double s, double v)
        {
            h = h - Math.Floor(h);
            double onethird = 1.0 / 3.0, onesixth = 1.0 / 6.0, twothird = 2.0 / 3.0, fivesixth = 5.0 / 6.0;
            double r, g, b;
            if (h > onesixth && h <= onethird) { g = 1; r = (onethird - h) / onesixth; b = 0; }
            else if (h > onethird && h <= 0.5) { g = 1; b = (h - onethird) / onesixth; r = 0; }
            else if (h > 0.5 && h <= twothird) { b = 1; g = (twothird - h) / onesixth; r = 0; }
            else if (h > twothird && h <= fivesixth) { b = 1; r = (h - twothird) / onesixth; g = 0; }
            else if (h > fivesixth && h <= 1.0) { r = 1; b = (1 - h) / onesixth; g = 0; }
            else { r = 1; g = h / onesixth; b = 0; }
            r = s * r + (1 - s); g = s * g + (1 - s); b = s * b + (1 - s);
            return new double[] { r * v, g * v, b * v };
        }
        public static double[] RGBToHSV(double r, double g, double b)
        {
            double onethird = 1.0 / 3.0, onesixth = 1.0 / 6.0, twothird = 2.0 / 3.0;
            double cmax = Math.Max(r, Math.Max(g, b)), cmin = Math.Min(r, Math.Min(g, b));
            double h = 0, s, v = cmax;
            s = cmax > 0 ? (cmax - cmin) / cmax : 0;
            if (s > 0)
            {
                if (r == cmax) h = onesixth * (g - b) / (cmax - cmin);
                else if (g == cmax) h = onethird + onesixth * (b - r) / (cmax - cmin);
                else h = twothird + onesixth * (r - g) / (cmax - cmin);
                if (h < 0) h += 1;
            }
            return new double[] { h, s, v };
        }
        public static double[] RGBToXYZ(double[] rgb)
        {
            double r = rgb[0], g = rgb[1], b = rgb[2];
            r = r > 0.04045 ? Math.Pow((r + 0.055) / 1.055, 2.4) : r / 12.92;
            g = g > 0.04045 ? Math.Pow((g + 0.055) / 1.055, 2.4) : g / 12.92;
            b = b > 0.04045 ? Math.Pow((b + 0.055) / 1.055, 2.4) : b / 12.92;
            return new double[] { r * 0.4124 + g * 0.3576 + b * 0.1805, r * 0.2126 + g * 0.7152 + b * 0.0722,
                                  r * 0.0193 + g * 0.1192 + b * 0.9505 };
        }
        public static double[] XYZToRGB(double[] xyz)
        {
            double x = xyz[0], y = xyz[1], z = xyz[2];
            double r = x * 3.2406 + y * -1.5372 + z * -0.4986;
            double g = x * -0.9689 + y * 1.8758 + z * 0.0415;
            double b = x * 0.0557 + y * -0.2040 + z * 1.0570;
            r = r > 0.0031308 ? 1.055 * Math.Pow(r, 1 / 2.4) - 0.055 : 12.92 * r;
            g = g > 0.0031308 ? 1.055 * Math.Pow(g, 1 / 2.4) - 0.055 : 12.92 * g;
            b = b > 0.0031308 ? 1.055 * Math.Pow(b, 1 / 2.4) - 0.055 : 12.92 * b;
            return new double[] { Clamp01(r), Clamp01(g), Clamp01(b) };
        }
        private static double Clamp01(double v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
        public static double[] RGBToLab(double[] rgb)
        {
            double[] xyz = RGBToXYZ(rgb);
            double x = xyz[0] / 0.9505, y = xyz[1] / 1.0, z = xyz[2] / 1.089;
            x = x > 0.008856 ? Math.Pow(x, 1.0 / 3) : 7.787 * x + 16.0 / 116;
            y = y > 0.008856 ? Math.Pow(y, 1.0 / 3) : 7.787 * y + 16.0 / 116;
            z = z > 0.008856 ? Math.Pow(z, 1.0 / 3) : 7.787 * z + 16.0 / 116;
            return new double[] { 116 * y - 16, 500 * (x - y), 200 * (y - z) };
        }
        public static double[] LabToRGB(double[] lab)
        {
            double y = (lab[0] + 16) / 116, x = lab[1] / 500 + y, z = y - lab[2] / 200;
            x = Math.Pow(x, 3) > 0.008856 ? Math.Pow(x, 3) : (x - 16.0 / 116) / 7.787;
            y = Math.Pow(y, 3) > 0.008856 ? Math.Pow(y, 3) : (y - 16.0 / 116) / 7.787;
            z = Math.Pow(z, 3) > 0.008856 ? Math.Pow(z, 3) : (z - 16.0 / 116) / 7.787;
            return XYZToRGB(new double[] { x * 0.9505, y * 1.0, z * 1.089 });
        }
        // The diverging color map of Moreland, interpolated in the Msh space as vtkColorTransferFunction does
        internal static double[] InterpolateDiverging(double t, double[] rgb1, double[] rgb2)
        {
            double[] msh1 = LabToMsh(RGBToLab(rgb1));
            double[] msh2 = LabToMsh(RGBToLab(rgb2));
            if (msh1[1] > 0.05 && msh2[1] > 0.05 && AngleDiff(msh1[2], msh2[2]) > 0.33 * Math.PI)
            {
                double mMid = Math.Max(msh1[0], msh2[0]);
                mMid = Math.Max(88.0, mMid);
                if (t < 0.5)
                {
                    msh2[0] = mMid; msh2[1] = 0; msh2[2] = 0;
                    t *= 2;
                }
                else
                {
                    msh1[0] = mMid; msh1[1] = 0; msh1[2] = 0;
                    t = 2 * t - 1;
                }
            }
            if (msh1[1] < 0.05 && msh2[1] > 0.05) msh1[2] = AdjustHue(msh2, msh1[0]);
            else if (msh2[1] < 0.05 && msh1[1] > 0.05) msh2[2] = AdjustHue(msh1, msh2[0]);
            double[] msh = new double[] { (1 - t) * msh1[0] + t * msh2[0], (1 - t) * msh1[1] + t * msh2[1],
                                          (1 - t) * msh1[2] + t * msh2[2] };
            return LabToRGB(MshToLab(msh));
        }
        private static double[] LabToMsh(double[] lab)
        {
            double m = Math.Sqrt(lab[0] * lab[0] + lab[1] * lab[1] + lab[2] * lab[2]);
            double s = m > 0.001 ? Math.Acos(lab[0] / m) : 0;
            double h = s > 0.001 ? Math.Atan2(lab[2], lab[1]) : 0;
            return new double[] { m, s, h };
        }
        private static double[] MshToLab(double[] msh)
        {
            return new double[] { msh[0] * Math.Cos(msh[1]), msh[0] * Math.Sin(msh[1]) * Math.Cos(msh[2]),
                                  msh[0] * Math.Sin(msh[1]) * Math.Sin(msh[2]) };
        }
        private static double AngleDiff(double a1, double a2)
        {
            double d = a1 - a2;
            while (d < -Math.PI) d += 2 * Math.PI;
            while (d > Math.PI) d -= 2 * Math.PI;
            return Math.Abs(d);
        }
        private static double AdjustHue(double[] msh, double unsatM)
        {
            if (msh[0] >= unsatM - 0.1) return msh[2];
            double hueSpin = msh[1] * Math.Sqrt(unsatM * unsatM - msh[0] * msh[0]) / (msh[0] * Math.Sin(msh[1]));
            return msh[2] > -0.3 * Math.PI ? msh[2] + hueSpin : msh[2] - hueSpin;
        }
    }

    // Interpolation of the camera orientation
    public partial class vtkQuaternionInterpolator
    {
        private readonly List<Tuple<double, double[]>> _quaternions = new List<Tuple<double, double[]>>();

        public void SetInterpolationTypeToLinear() { }
        public void SetInterpolationTypeToSpline() { }
        public void AddQuaternion(double t, double[] q)
        {
            _quaternions.Add(Tuple.Create(t, (double[])q.Clone()));
            _quaternions.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        }
        public void Initialize() { _quaternions.Clear(); }
        public void AddQuaternion(double t, IntPtr q)
        {
            double[] v = new double[4];
            System.Runtime.InteropServices.Marshal.Copy(q, v, 0, 4);
            AddQuaternion(t, v);
        }
        public void InterpolateQuaternion(double t, IntPtr q)
        {
            double[] v = new double[4];
            InterpolateQuaternion(t, v);
            System.Runtime.InteropServices.Marshal.Copy(v, 0, q, 4);
        }
        public void InterpolateQuaternion(double t, double[] q)
        {
            if (_quaternions.Count == 0) return;
            if (_quaternions.Count == 1 || t <= _quaternions[0].Item1) { Array.Copy(_quaternions[0].Item2, q, 4); return; }
            var last = _quaternions[_quaternions.Count - 1];
            if (t >= last.Item1) { Array.Copy(last.Item2, q, 4); return; }
            int i = 0;
            while (i < _quaternions.Count - 2 && t > _quaternions[i + 1].Item1) i++;
            double[] a = _quaternions[i].Item2;
            double[] b = _quaternions[i + 1].Item2;
            double u = (t - _quaternions[i].Item1) / (_quaternions[i + 1].Item1 - _quaternions[i].Item1);
            // Spherical linear interpolation
            double dot = a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
            double[] bb = (double[])b.Clone();
            if (dot < 0) { dot = -dot; for (int k = 0; k < 4; k++) bb[k] = -bb[k]; }
            double s0, s1;
            if (dot > 0.9995) { s0 = 1 - u; s1 = u; }
            else
            {
                double theta = Math.Acos(dot);
                s0 = Math.Sin((1 - u) * theta) / Math.Sin(theta);
                s1 = Math.Sin(u * theta) / Math.Sin(theta);
            }
            for (int k = 0; k < 4; k++) q[k] = s0 * a[k] + s1 * bb[k];
        }
    }

    public partial class vtkCameraInterpolator
    {
        private readonly List<Tuple<double, vtkCamera>> _cameras = new List<Tuple<double, vtkCamera>>();

        public void SetInterpolationTypeToSpline() { }
        public void SetInterpolationTypeToLinear() { }
        public void Initialize() { _cameras.Clear(); }
        public void AddCamera(double t, vtkCamera camera)
        {
            vtkCamera copy = new vtkCamera();
            copy.DeepCopy(camera);
            _cameras.Add(Tuple.Create(t, copy));
            _cameras.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        }
        public void InterpolateCamera(double t, vtkCamera camera)
        {
            if (_cameras.Count == 0) return;
            if (_cameras.Count == 1 || t <= _cameras[0].Item1) { camera.DeepCopy(_cameras[0].Item2); return; }
            var last = _cameras[_cameras.Count - 1];
            if (t >= last.Item1) { camera.DeepCopy(last.Item2); return; }
            int i = 0;
            while (i < _cameras.Count - 2 && t > _cameras[i + 1].Item1) i++;
            vtkCamera a = _cameras[i].Item2;
            vtkCamera b = _cameras[i + 1].Item2;
            double u = (t - _cameras[i].Item1) / (_cameras[i + 1].Item1 - _cameras[i].Item1);
            double[] Lerp(double[] p, double[] q) { return new double[] { p[0] + u * (q[0] - p[0]), p[1] + u * (q[1] - p[1]),
                                                                          p[2] + u * (q[2] - p[2]) }; }
            camera.SetPosition(Lerp(a.GetPosition(), b.GetPosition()));
            camera.SetFocalPoint(Lerp(a.GetFocalPoint(), b.GetFocalPoint()));
            camera.SetViewUp(Lerp(a.GetViewUp(), b.GetViewUp()));
            camera.SetParallelScale(a.GetParallelScale() + u * (b.GetParallelScale() - a.GetParallelScale()));
            camera.SetViewAngle(a.GetViewAngle() + u * (b.GetViewAngle() - a.GetViewAngle()));
            camera.OrthogonalizeViewUp();
        }
    }
}
