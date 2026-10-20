// PrePoMax for macOS - the rendering classes of the VTK compatible library: props, properties, mappers, the camera, lights,
// coordinates, renderers and the render window. The OpenGL drawing of a render window is done by Gl/SceneRenderer.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    // Props
    public partial class vtkProp
    {
        protected bool _visibility = true;
        protected bool _pickable = true;
        protected bool _dragable = true;
        protected bool _useBounds = true;
        internal vtkRenderer _renderer;     // the renderer the prop was last added to

        public void SetVisibility(int v) { if (_visibility != (v != 0)) { _visibility = v != 0; Modified(); } }
        public void SetVisibility(bool v) { SetVisibility(v ? 1 : 0); }
        public int GetVisibility() { return _visibility ? 1 : 0; }
        public void VisibilityOn() { SetVisibility(1); }
        public void VisibilityOff() { SetVisibility(0); }
        public void SetPickable(int v) { _pickable = v != 0; Modified(); }
        public int GetPickable() { return _pickable ? 1 : 0; }
        public void PickableOn() { SetPickable(1); }
        public void PickableOff() { SetPickable(0); }
        public void SetDragable(int v) { _dragable = v != 0; }
        public int GetDragable() { return _dragable ? 1 : 0; }
        public void DragableOn() { _dragable = true; }
        public void DragableOff() { _dragable = false; }
        public void SetUseBounds(bool v) { _useBounds = v; }
        public void UseBoundsOn() { _useBounds = true; }
        public void UseBoundsOff() { _useBounds = false; }
        public bool GetUseBounds() { return _useBounds; }
        public virtual double[] GetBounds() { return null; }
        public virtual void ReleaseGraphicsResources(vtkWindow window) { }
        public virtual void ShallowCopy(vtkProp prop)
        {
            _visibility = prop._visibility;
            _pickable = prop._pickable;
            _dragable = prop._dragable;
            _useBounds = prop._useBounds;
        }
        public virtual void GetActors(vtkPropCollection collection) { }
        public virtual double GetEstimatedRenderTime() { return 0; }
        internal bool Visible { get { return _visibility; } }
    }

    public partial class vtkProp3D
    {
        protected double[] _position = new double[3];
        protected double[] _origin = new double[3];
        protected double[] _scale = new double[] { 1, 1, 1 };
        protected double[] _orientation = new double[3];
        protected vtkMatrix4x4 _userMatrix;
        protected vtkLinearTransform _userTransform;

        public void SetPosition(double x, double y, double z) { _position = new[] { x, y, z }; Modified(); }
        public void SetPosition(double[] p) { SetPosition(p[0], p[1], p[2]); }
        public double[] GetPosition() { return (double[])_position.Clone(); }
        public void AddPosition(double x, double y, double z) { SetPosition(_position[0] + x, _position[1] + y, _position[2] + z); }
        public void AddPosition(double[] d) { AddPosition(d[0], d[1], d[2]); }
        public void SetOrigin(double x, double y, double z) { _origin = new[] { x, y, z }; Modified(); }
        public void SetOrigin(double[] o) { SetOrigin(o[0], o[1], o[2]); }
        public double[] GetOrigin() { return (double[])_origin.Clone(); }
        public void SetScale(double x, double y, double z) { _scale = new[] { x, y, z }; Modified(); }
        public void SetScale(double s) { SetScale(s, s, s); }
        public void SetScale(double[] s) { SetScale(s[0], s[1], s[2]); }
        public double[] GetScale() { return (double[])_scale.Clone(); }
        public void SetOrientation(double x, double y, double z) { _orientation = new[] { x, y, z }; Modified(); }
        public void SetOrientation(double[] o) { SetOrientation(o[0], o[1], o[2]); }
        public double[] GetOrientation() { return (double[])_orientation.Clone(); }
        public void AddOrientation(double x, double y, double z)
        {
            SetOrientation(_orientation[0] + x, _orientation[1] + y, _orientation[2] + z);
        }
        public void RotateX(double angle) { RotateWXYZ(angle, 1, 0, 0); }
        public void RotateY(double angle) { RotateWXYZ(angle, 0, 1, 0); }
        public void RotateZ(double angle) { RotateWXYZ(angle, 0, 0, 1); }
        public void RotateWXYZ(double angle, double x, double y, double z)
        {
            // The rotation is concatenated to the current orientation
            vtkTransform t = new vtkTransform();
            t.PostMultiply();
            t.RotateY(_orientation[1]);
            t.RotateX(_orientation[0]);
            t.RotateZ(_orientation[2]);
            t.RotateWXYZ(angle, x, y, z);
            _orientation = t.GetOrientation();
            Modified();
        }
        public void SetUserMatrix(vtkMatrix4x4 matrix) { _userMatrix = matrix; _userTransform = null; Modified(); }
        public vtkMatrix4x4 GetUserMatrix()
        {
            if (_userTransform != null)
            {
                vtkMatrix4x4 m = new vtkMatrix4x4();
                m._e = (double[])_userTransform._m.Clone();
                return m;
            }
            return _userMatrix;
        }
        public void SetUserTransform(vtkLinearTransform transform) { _userTransform = transform; _userMatrix = null; Modified(); }
        public vtkLinearTransform GetUserTransform() { return _userTransform; }
        // The model matrix: user matrix * T(position + origin) * Rz * Rx * Ry * S * T(-origin)
        internal double[] ModelMatrix
        {
            get
            {
                vtkTransform t = new vtkTransform();
                t.PostMultiply();
                t.Translate(-_origin[0], -_origin[1], -_origin[2]);
                t.Scale(_scale[0], _scale[1], _scale[2]);
                t.RotateY(_orientation[1]);
                t.RotateX(_orientation[0]);
                t.RotateZ(_orientation[2]);
                t.Translate(_origin[0] + _position[0], _origin[1] + _position[1], _origin[2] + _position[2]);
                double[] m = t._m;
                if (_userTransform != null) m = vtkMatrix4x4.Multiply(_userTransform._m, m);
                else if (_userMatrix != null) m = vtkMatrix4x4.Multiply(_userMatrix._e, m);
                return m;
            }
        }
        internal bool IsIdentity
        {
            get
            {
                if (_userMatrix != null || _userTransform != null) return false;
                for (int i = 0; i < 3; i++)
                    if (_position[i] != 0 || _orientation[i] != 0 || _scale[i] != 1) return false;
                return true;
            }
        }
        public vtkMatrix4x4 GetMatrix()
        {
            vtkMatrix4x4 m = new vtkMatrix4x4();
            m._e = ModelMatrix;
            return m;
        }
        public void GetMatrix(vtkMatrix4x4 m) { m._e = ModelMatrix; }
        public double[] GetCenter()
        {
            double[] b = GetBounds();
            if (b == null) return new double[3];
            return new[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
        }
        public double GetLength()
        {
            double[] b = GetBounds();
            if (b == null) return 0;
            return Math.Sqrt((b[1] - b[0]) * (b[1] - b[0]) + (b[3] - b[2]) * (b[3] - b[2]) + (b[5] - b[4]) * (b[5] - b[4]));
        }
        internal static double[] TransformBounds(double[] b, double[] m)
        {
            if (b == null) return null;
            double[] r = { double.MaxValue, -double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue,
                           -double.MaxValue };
            for (int i = 0; i < 8; i++)
            {
                double[] p = vtkMatrix4x4.TransformPoint(m, b[i & 1], b[2 + ((i >> 1) & 1)], b[4 + ((i >> 2) & 1)]);
                for (int c = 0; c < 3; c++)
                {
                    if (p[c] < r[2 * c]) r[2 * c] = p[c];
                    if (p[c] > r[2 * c + 1]) r[2 * c + 1] = p[c];
                }
            }
            return r;
        }
        public override void ShallowCopy(vtkProp prop)
        {
            base.ShallowCopy(prop);
            if (prop is vtkProp3D p)
            {
                _position = (double[])p._position.Clone();
                _origin = (double[])p._origin.Clone();
                _scale = (double[])p._scale.Clone();
                _orientation = (double[])p._orientation.Clone();
                _userMatrix = p._userMatrix;
                _userTransform = p._userTransform;
            }
        }
    }

    public partial class vtkActor
    {
        protected vtkMapper _mapper;
        protected vtkProperty _property;
        protected vtkProperty _backfaceProperty;

        public void SetMapper(vtkMapper mapper) { _mapper = mapper; Modified(); }
        public vtkMapper GetMapper() { return _mapper; }
        public vtkProperty GetProperty() { return _property ?? (_property = new vtkProperty()); }
        public void SetProperty(vtkProperty property) { _property = property; Modified(); }
        public vtkProperty GetBackfaceProperty() { return _backfaceProperty; }
        public void SetBackfaceProperty(vtkProperty property) { _backfaceProperty = property; Modified(); }
        public vtkProperty MakeProperty() { return new vtkProperty(); }
        public override double[] GetBounds()
        {
            double[] b = _mapper?.GetBounds();
            if (b == null || b[1] < b[0]) return null;
            return IsIdentity ? b : TransformBounds(b, ModelMatrix);
        }
        public void GetBounds(double[] bounds)
        {
            double[] b = GetBounds() ?? new double[] { 1, -1, 1, -1, 1, -1 };
            Array.Copy(b, bounds, 6);
        }
        public int HasTranslucentPolygonalGeometry() { return GetProperty().GetOpacity() < 1 ? 1 : 0; }
        public override void ShallowCopy(vtkProp prop)
        {
            base.ShallowCopy(prop);
            if (prop is vtkActor a)
            {
                _mapper = a._mapper;
                _property = a._property;
                _backfaceProperty = a._backfaceProperty;
            }
        }
        public override void GetActors(vtkPropCollection collection) { collection.AddItem(this); }
        public static vtkActor SafeDownCast(vtkObjectBase o) { return o as vtkActor; }
    }

    // An actor that always faces the camera
    public partial class vtkFollower
    {
        private vtkCamera _camera;
        public void SetCamera(vtkCamera camera) { _camera = camera; }
        public vtkCamera GetCamera() { return _camera; }
        // The model matrix with the rotation that turns the actor to the camera
        internal double[] FollowerMatrix(vtkCamera camera)
        {
            vtkCamera c = _camera ?? camera;
            if (c == null) return ModelMatrix;
            double[] vup = c.GetViewUp();
            double[] dop = c.GetDirectionOfProjection();
            double[] z = { -dop[0], -dop[1], -dop[2] };
            double[] x = vtkMath.Cross(vup, z);
            vtkMath.Normalize(x);
            double[] y = vtkMath.Cross(z, x);
            double[] r = { x[0], y[0], z[0], 0, x[1], y[1], z[1], 0, x[2], y[2], z[2], 0, 0, 0, 0, 1 };
            double[] m = vtkMatrix4x4.Translation(-_origin[0], -_origin[1], -_origin[2]);
            m = vtkMatrix4x4.Multiply(vtkMatrix4x4.Scaling(_scale[0], _scale[1], _scale[2]), m);
            vtkTransform o = new vtkTransform();
            o.PostMultiply();
            o.RotateY(_orientation[1]);
            o.RotateX(_orientation[0]);
            o.RotateZ(_orientation[2]);
            m = vtkMatrix4x4.Multiply(o._m, m);
            m = vtkMatrix4x4.Multiply(r, m);
            m = vtkMatrix4x4.Multiply(vtkMatrix4x4.Translation(_origin[0] + _position[0], _origin[1] + _position[1],
                                                               _origin[2] + _position[2]), m);
            return m;
        }
    }

    public partial class vtkProperty
    {
        internal double[] _ambientColor = { 1, 1, 1 };
        internal double[] _diffuseColor = { 1, 1, 1 };
        internal double[] _specularColor = { 1, 1, 1 };
        internal double[] _edgeColor = { 0, 0, 0 };
        internal double _ambient, _diffuse = 1, _specular, _specularPower = 1, _opacity = 1;
        internal int _representation = 2, _interpolation = 1;
        internal float _pointSize = 1, _lineWidth = 1;
        internal bool _lighting = true, _backfaceCulling, _frontfaceCulling, _edgeVisibility;
        internal int _lineStipplePattern = 0xFFFF;

        public void SetColor(double r, double g, double b)
        {
            _ambientColor = new[] { r, g, b };
            _diffuseColor = new[] { r, g, b };
            _specularColor = new[] { r, g, b };
            Modified();
        }
        public void SetColor(double[] c) { SetColor(c[0], c[1], c[2]); }
        // The color is the mix of the colors weighted by their coefficients
        public double[] GetColor()
        {
            double total = _ambient + _diffuse + _specular;
            if (total <= 0) return (double[])_diffuseColor.Clone();
            double[] c = new double[3];
            for (int i = 0; i < 3; i++)
                c[i] = (_ambient * _ambientColor[i] + _diffuse * _diffuseColor[i] + _specular * _specularColor[i]) / total;
            return c;
        }
        public void SetAmbientColor(double r, double g, double b) { _ambientColor = new[] { r, g, b }; Modified(); }
        public void SetAmbientColor(double[] c) { SetAmbientColor(c[0], c[1], c[2]); }
        public double[] GetAmbientColor() { return (double[])_ambientColor.Clone(); }
        public void SetDiffuseColor(double r, double g, double b) { _diffuseColor = new[] { r, g, b }; Modified(); }
        public void SetDiffuseColor(double[] c) { SetDiffuseColor(c[0], c[1], c[2]); }
        public double[] GetDiffuseColor() { return (double[])_diffuseColor.Clone(); }
        public void SetSpecularColor(double r, double g, double b) { _specularColor = new[] { r, g, b }; Modified(); }
        public double[] GetSpecularColor() { return (double[])_specularColor.Clone(); }
        public void SetEdgeColor(double r, double g, double b) { _edgeColor = new[] { r, g, b }; Modified(); }
        public double[] GetEdgeColor() { return (double[])_edgeColor.Clone(); }
        public void SetAmbient(double v) { _ambient = v; Modified(); }
        public double GetAmbient() { return _ambient; }
        public void SetDiffuse(double v) { _diffuse = v; Modified(); }
        public double GetDiffuse() { return _diffuse; }
        public void SetSpecular(double v) { _specular = v; Modified(); }
        public double GetSpecular() { return _specular; }
        public void SetSpecularPower(double v) { _specularPower = v; Modified(); }
        public double GetSpecularPower() { return _specularPower; }
        public void SetOpacity(double v) { _opacity = Math.Max(0, Math.Min(1, v)); Modified(); }
        public double GetOpacity() { return _opacity; }
        public void SetPointSize(float v) { _pointSize = v; Modified(); }
        public float GetPointSize() { return _pointSize; }
        public void SetLineWidth(float v) { _lineWidth = v; Modified(); }
        public float GetLineWidth() { return _lineWidth; }
        public void SetLineStipplePattern(int p) { _lineStipplePattern = p; }
        public void SetLineStippleRepeatFactor(int f) { }
        public void SetRepresentationToPoints() { _representation = 0; Modified(); }
        public void SetRepresentationToWireframe() { _representation = 1; Modified(); }
        public void SetRepresentationToSurface() { _representation = 2; Modified(); }
        public void SetRepresentation(int r) { _representation = r; Modified(); }
        public int GetRepresentation() { return _representation; }
        public void SetInterpolationToFlat() { _interpolation = 0; Modified(); }
        public void SetInterpolationToGouraud() { _interpolation = 1; Modified(); }
        public void SetInterpolationToPhong() { _interpolation = 2; Modified(); }
        public void SetInterpolation(int i) { _interpolation = i; Modified(); }
        public int GetInterpolation() { return _interpolation; }
        public void SetLighting(bool v) { _lighting = v; Modified(); }
        public void SetLighting(int v) { _lighting = v != 0; Modified(); }
        public bool GetLighting() { return _lighting; }
        public void LightingOn() { SetLighting(true); }
        public void LightingOff() { SetLighting(false); }
        public void SetBackfaceCulling(int v) { _backfaceCulling = v != 0; Modified(); }
        public int GetBackfaceCulling() { return _backfaceCulling ? 1 : 0; }
        public void BackfaceCullingOn() { SetBackfaceCulling(1); }
        public void BackfaceCullingOff() { SetBackfaceCulling(0); }
        public void SetFrontfaceCulling(int v) { _frontfaceCulling = v != 0; Modified(); }
        public void FrontfaceCullingOn() { SetFrontfaceCulling(1); }
        public void FrontfaceCullingOff() { SetFrontfaceCulling(0); }
        public void SetEdgeVisibility(int v) { _edgeVisibility = v != 0; Modified(); }
        public void EdgeVisibilityOn() { SetEdgeVisibility(1); }
        public void EdgeVisibilityOff() { SetEdgeVisibility(0); }
        public int GetEdgeVisibility() { return _edgeVisibility ? 1 : 0; }
        public void ShadingOn() { }
        public void ShadingOff() { }
        public void DeepCopy(vtkProperty p)
        {
            _ambientColor = (double[])p._ambientColor.Clone();
            _diffuseColor = (double[])p._diffuseColor.Clone();
            _specularColor = (double[])p._specularColor.Clone();
            _edgeColor = (double[])p._edgeColor.Clone();
            _ambient = p._ambient; _diffuse = p._diffuse; _specular = p._specular; _specularPower = p._specularPower;
            _opacity = p._opacity; _representation = p._representation; _interpolation = p._interpolation;
            _pointSize = p._pointSize; _lineWidth = p._lineWidth; _lighting = p._lighting;
            _backfaceCulling = p._backfaceCulling; _frontfaceCulling = p._frontfaceCulling;
            _edgeVisibility = p._edgeVisibility; _lineStipplePattern = p._lineStipplePattern;
            Modified();
        }
    }

    public partial class vtkProperty2D
    {
        internal double[] _color = { 1, 1, 1 };
        internal double _opacity = 1;
        internal float _pointSize = 1, _lineWidth = 1;
        internal int _displayLocation = 1;      // foreground

        public void SetColor(double r, double g, double b) { _color = new[] { r, g, b }; Modified(); }
        public void SetColor(double[] c) { SetColor(c[0], c[1], c[2]); }
        public double[] GetColor() { return (double[])_color.Clone(); }
        public void SetOpacity(double v) { _opacity = v; Modified(); }
        public double GetOpacity() { return _opacity; }
        public void SetPointSize(float v) { _pointSize = v; Modified(); }
        public float GetPointSize() { return _pointSize; }
        public void SetLineWidth(float v) { _lineWidth = v; Modified(); }
        public float GetLineWidth() { return _lineWidth; }
        public void SetLineStipplePattern(int p) { }
        public void SetDisplayLocationToBackground() { _displayLocation = 0; }
        public void SetDisplayLocationToForeground() { _displayLocation = 1; }
        public void SetDisplayLocation(int v) { _displayLocation = v; }
        public void DeepCopy(vtkProperty2D p)
        {
            _color = (double[])p._color.Clone();
            _opacity = p._opacity;
            _pointSize = p._pointSize;
            _lineWidth = p._lineWidth;
            _displayLocation = p._displayLocation;
            Modified();
        }
    }

    public partial class vtkTextProperty
    {
        internal double[] _color = { 1, 1, 1 };
        internal double _opacity = 1;
        internal int _fontSize = 12;
        internal string _fontFamily = "Arial";
        internal bool _bold, _italic, _shadow;
        internal int _justification, _verticalJustification;
        internal double _lineOffset, _lineSpacing = 1.1, _orientation;
        internal double[] _backgroundColor = { 0, 0, 0 };
        internal double _backgroundOpacity;

        public void SetColor(double r, double g, double b) { _color = new[] { r, g, b }; Modified(); }
        public void SetColor(double[] c) { SetColor(c[0], c[1], c[2]); }
        public double[] GetColor() { return (double[])_color.Clone(); }
        public void SetOpacity(double v) { _opacity = v; Modified(); }
        public double GetOpacity() { return _opacity; }
        public void SetFontSize(int s) { _fontSize = s; Modified(); }
        public int GetFontSize() { return _fontSize; }
        public void SetFontFamilyToArial() { _fontFamily = "Arial"; Modified(); }
        public void SetFontFamilyToCourier() { _fontFamily = "Courier"; Modified(); }
        public void SetFontFamilyToTimes() { _fontFamily = "Times"; Modified(); }
        public void SetFontFamilyAsString(string family) { _fontFamily = family; Modified(); }
        public string GetFontFamilyAsString() { return _fontFamily; }
        public void SetFontFamily(int f) { _fontFamily = f == 1 ? "Courier" : f == 2 ? "Times" : "Arial"; Modified(); }
        public void SetBold(int v) { _bold = v != 0; Modified(); }
        public int GetBold() { return _bold ? 1 : 0; }
        public void BoldOn() { SetBold(1); }
        public void BoldOff() { SetBold(0); }
        public void SetItalic(int v) { _italic = v != 0; Modified(); }
        public void ItalicOn() { SetItalic(1); }
        public void ItalicOff() { SetItalic(0); }
        public void SetShadow(int v) { _shadow = v != 0; Modified(); }
        public void ShadowOn() { SetShadow(1); }
        public void ShadowOff() { SetShadow(0); }
        public int GetShadow() { return _shadow ? 1 : 0; }
        public void SetJustificationToLeft() { _justification = 0; Modified(); }
        public void SetJustificationToCentered() { _justification = 1; Modified(); }
        public void SetJustificationToRight() { _justification = 2; Modified(); }
        public void SetJustification(int j) { _justification = j; Modified(); }
        public int GetJustification() { return _justification; }
        public void SetVerticalJustificationToBottom() { _verticalJustification = 0; Modified(); }
        public void SetVerticalJustificationToCentered() { _verticalJustification = 1; Modified(); }
        public void SetVerticalJustificationToTop() { _verticalJustification = 2; Modified(); }
        public void SetVerticalJustification(int j) { _verticalJustification = j; Modified(); }
        public int GetVerticalJustification() { return _verticalJustification; }
        public void SetLineOffset(double v) { _lineOffset = v; Modified(); }
        public double GetLineOffset() { return _lineOffset; }
        public void SetLineSpacing(double v) { _lineSpacing = v; Modified(); }
        public double GetLineSpacing() { return _lineSpacing; }
        public void SetOrientation(double v) { _orientation = v; Modified(); }
        public double GetOrientation() { return _orientation; }
        public void SetBackgroundColor(double r, double g, double b) { _backgroundColor = new[] { r, g, b }; Modified(); }
        public void SetBackgroundOpacity(double v) { _backgroundOpacity = v; Modified(); }
        public void ShallowCopy(vtkTextProperty p) { DeepCopy(p); }
        public void DeepCopy(vtkTextProperty p)
        {
            _color = (double[])p._color.Clone();
            _opacity = p._opacity;
            _fontSize = p._fontSize;
            _fontFamily = p._fontFamily;
            _bold = p._bold; _italic = p._italic; _shadow = p._shadow;
            _justification = p._justification;
            _verticalJustification = p._verticalJustification;
            _lineOffset = p._lineOffset;
            _lineSpacing = p._lineSpacing;
            _orientation = p._orientation;
            _backgroundColor = (double[])p._backgroundColor.Clone();
            _backgroundOpacity = p._backgroundOpacity;
            Modified();
        }
    }

    // Mappers
    public partial class vtkAbstractMapper
    {
        protected readonly List<vtkPlane> _clippingPlanes = new List<vtkPlane>();

        public void AddClippingPlane(vtkPlane plane) { if (!_clippingPlanes.Contains(plane)) _clippingPlanes.Add(plane); Modified(); }
        public void RemoveClippingPlane(vtkPlane plane) { _clippingPlanes.Remove(plane); Modified(); }
        public void RemoveAllClippingPlanes() { _clippingPlanes.Clear(); Modified(); }
        public void SetClippingPlanes(vtkPlaneCollection planes)
        {
            _clippingPlanes.Clear();
            if (planes != null) _clippingPlanes.AddRange(planes.Planes);
            Modified();
        }
        public void SetClippingPlanes(vtkPlanes planes)
        {
            _clippingPlanes.Clear();
            if (planes != null) for (int i = 0; i < planes.GetNumberOfPlanes(); i++) _clippingPlanes.Add(planes.GetPlane(i));
            Modified();
        }
        internal List<vtkPlane> ClippingPlanes { get { return _clippingPlanes; } }
        public virtual void ReleaseGraphicsResources(vtkWindow window) { }
    }

    public partial class vtkMapper
    {
        internal bool _scalarVisibility = true;
        internal int _scalarMode;               // 0 default, 1 point data, 2 cell data, 3 point field, 4 cell field
        internal int _colorMode;                // 0 default, 1 map scalars
        internal vtkScalarsToColors _lookupTable;
        internal double[] _scalarRange = { 0, 1 };
        internal bool _useLookupTableScalarRange;
        internal bool _interpolateScalarsBeforeMapping;
        internal string _arrayName;
        internal int _arrayId = -1;
        internal bool _immediateMode;
        internal bool _static;
        private static int _resolveCoincidentTopology;

        public static void SetResolveCoincidentTopologyToPolygonOffset() { _resolveCoincidentTopology = 1; }
        public static void SetResolveCoincidentTopologyToOff() { _resolveCoincidentTopology = 0; }
        public static void SetResolveCoincidentTopologyToShiftZBuffer() { _resolveCoincidentTopology = 2; }
        public static void SetResolveCoincidentTopologyPolygonOffsetParameters(double factor, double units) { }
        public static void SetResolveCoincidentTopology(int v) { _resolveCoincidentTopology = v; }
        internal static bool PolygonOffset { get { return _resolveCoincidentTopology == 1; } }

        public vtkDataSet GetInput() { return GetInputDataObject(0, 0) as vtkDataSet; }
        public vtkDataSet GetInputAsDataSet() { return GetInput(); }
        public void SetScalarVisibility(int v) { _scalarVisibility = v != 0; Modified(); }
        public void SetScalarVisibility(bool v) { SetScalarVisibility(v ? 1 : 0); }
        public int GetScalarVisibility() { return _scalarVisibility ? 1 : 0; }
        public void ScalarVisibilityOn() { SetScalarVisibility(1); }
        public void ScalarVisibilityOff() { SetScalarVisibility(0); }
        public void SetScalarModeToDefault() { _scalarMode = 0; Modified(); }
        public void SetScalarModeToUsePointData() { _scalarMode = 1; Modified(); }
        public void SetScalarModeToUseCellData() { _scalarMode = 2; Modified(); }
        public void SetScalarModeToUsePointFieldData() { _scalarMode = 3; Modified(); }
        public void SetScalarModeToUseCellFieldData() { _scalarMode = 4; Modified(); }
        public void SetScalarMode(int m) { _scalarMode = m; Modified(); }
        public int GetScalarMode() { return _scalarMode; }
        public void SetColorModeToDefault() { _colorMode = 0; Modified(); }
        public void SetColorModeToMapScalars() { _colorMode = 1; Modified(); }
        public void SetColorMode(int m) { _colorMode = m; Modified(); }
        public void SelectColorArray(string name) { _arrayName = name; _arrayId = -1; Modified(); }
        public void SelectColorArray(int id) { _arrayId = id; _arrayName = null; Modified(); }
        public void SetLookupTable(vtkScalarsToColors lut) { _lookupTable = lut; Modified(); }
        public vtkScalarsToColors GetLookupTable() { return _lookupTable ?? (_lookupTable = DefaultLookupTable()); }
        public void CreateDefaultLookupTable() { _lookupTable = DefaultLookupTable(); }
        private static vtkLookupTable DefaultLookupTable()
        {
            vtkLookupTable lut = new vtkLookupTable();
            lut.Build();
            return lut;
        }
        public void SetScalarRange(double min, double max) { _scalarRange = new[] { min, max }; Modified(); }
        public void SetScalarRange(double[] range) { SetScalarRange(range[0], range[1]); }
        public double[] GetScalarRange() { return (double[])_scalarRange.Clone(); }
        public void SetUseLookupTableScalarRange(int v) { _useLookupTableScalarRange = v != 0; Modified(); }
        public void UseLookupTableScalarRangeOn() { SetUseLookupTableScalarRange(1); }
        public void UseLookupTableScalarRangeOff() { SetUseLookupTableScalarRange(0); }
        public int GetInterpolateScalarsBeforeMapping() { return _interpolateScalarsBeforeMapping ? 1 : 0; }
        public void SetInterpolateScalarsBeforeMapping(int v) { _interpolateScalarsBeforeMapping = v != 0; Modified(); }
        public void InterpolateScalarsBeforeMappingOn() { SetInterpolateScalarsBeforeMapping(1); }
        public void InterpolateScalarsBeforeMappingOff() { SetInterpolateScalarsBeforeMapping(0); }
        public void ImmediateModeRenderingOn() { _immediateMode = true; }
        public void ImmediateModeRenderingOff() { _immediateMode = false; }
        public void SetImmediateModeRendering(int v) { _immediateMode = v != 0; }
        public void StaticOn() { _static = true; }
        public void StaticOff() { _static = false; }
        public void SetResolveCoincidentTopologyToDefault() { }
        public override void Update() { UpdatePipeline(); }
        protected override void Execute() { }
        // The poly data the mapper draws
        internal virtual vtkPolyData GetRenderPolyData()
        {
            UpdatePipeline();
            return GetInput() as vtkPolyData;
        }
        public virtual double[] GetBounds()
        {
            vtkPolyData pd = GetRenderPolyData();
            if (pd == null) { vtkDataSet ds = GetInput(); return ds?.GetBounds(); }
            return pd.GetBounds();
        }
        public double[] GetCenter()
        {
            double[] b = GetBounds();
            return b == null ? new double[3] : new[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
        }
        public double GetLength()
        {
            double[] b = GetBounds();
            if (b == null) return 0;
            return Math.Sqrt((b[1] - b[0]) * (b[1] - b[0]) + (b[3] - b[2]) * (b[3] - b[2]) + (b[5] - b[4]) * (b[5] - b[4]));
        }
        public void ShallowCopy(vtkAbstractMapper mapper)
        {
            if (!(mapper is vtkMapper m)) return;
            _scalarVisibility = m._scalarVisibility;
            _scalarMode = m._scalarMode;
            _colorMode = m._colorMode;
            _lookupTable = m._lookupTable;
            _scalarRange = (double[])m._scalarRange.Clone();
            _useLookupTableScalarRange = m._useLookupTableScalarRange;
            _interpolateScalarsBeforeMapping = m._interpolateScalarsBeforeMapping;
            _arrayName = m._arrayName;
            SetInput(m.GetInput());
        }
        // The scalars that color the cells or points, and whether they are cell scalars
        internal vtkDataArray GetColorScalars(vtkDataSet data, out bool cellScalars)
        {
            cellScalars = false;
            if (!_scalarVisibility || data == null) return null;
            vtkDataArray scalars = null;
            switch (_scalarMode)
            {
                case 1: scalars = data.GetPointData().GetScalars(); break;
                case 2: scalars = data.GetCellData().GetScalars(); cellScalars = true; break;
                case 3:
                    scalars = _arrayName != null ? data.GetPointData().GetArray(_arrayName) : data.GetPointData().GetArray(_arrayId);
                    break;
                case 4:
                    scalars = _arrayName != null ? data.GetCellData().GetArray(_arrayName) : data.GetCellData().GetArray(_arrayId);
                    cellScalars = true;
                    break;
                default:
                    scalars = data.GetPointData().GetScalars();
                    if (scalars == null)
                    {
                        scalars = data.GetCellData().GetScalars();
                        cellScalars = scalars != null;
                    }
                    break;
            }
            return scalars;
        }
        // The range of the colors of the lookup table
        internal double[] ColorRange()
        {
            if (_useLookupTableScalarRange && _lookupTable != null) return _lookupTable.GetRange();
            return (double[])_scalarRange.Clone();
        }
        public static vtkMapper SafeDownCast(vtkObjectBase o) { return o as vtkMapper; }
    }

    public partial class vtkPolyDataMapper
    {
        public new vtkPolyData GetInput() { return GetInputDataObject(0, 0) as vtkPolyData; }
        public void SetNumberOfPieces(int n) { }
        public void SetPiece(int p) { }
    }

    // A mapper of any data set; an unstructured grid is drawn by its visible surface
    public partial class vtkDataSetMapper
    {
        private vtkGeometryFilter _geometryFilter;
        private vtkPolyData _surface;
        private ulong _surfaceTime;
        private vtkDataSet _surfaceInput;

        internal override vtkPolyData GetRenderPolyData()
        {
            UpdatePipeline();
            vtkDataSet input = GetInput();
            if (input is vtkPolyData pd) return pd;
            if (input == null) return null;
            if (_surface == null || _surfaceInput != input || _surfaceTime != input.DataTime)
            {
                if (_geometryFilter == null) _geometryFilter = new vtkGeometryFilter();
                _geometryFilter.SetInput(input);
                _geometryFilter.Update();
                _surface = _geometryFilter.GetOutput();
                _surfaceInput = input;
                _surfaceTime = input.DataTime;
            }
            return _surface;
        }
        public vtkPolyDataMapper GetPolyDataMapper() { return null; }
        public static vtkDataSetMapper SafeDownCast(vtkObjectBase o) { return o as vtkDataSetMapper; }
    }

    // 2D
    public partial class vtkMapper2D
    {
        public virtual double[] GetBounds2D() { return null; }
    }

    public partial class vtkPolyDataMapper2D
    {
        internal vtkCoordinate _transformCoordinate;
        internal bool _scalarVisibility = true;
        internal vtkScalarsToColors _lookupTable;
        internal double[] _scalarRange = { 0, 1 };
        internal int _scalarMode;

        public vtkPolyData GetInput() { return GetInputDataObject(0, 0) as vtkPolyData; }
        public void SetTransformCoordinate(vtkCoordinate c) { _transformCoordinate = c; Modified(); }
        public vtkCoordinate GetTransformCoordinate() { return _transformCoordinate; }
        public void SetTransformCoordinateUseDouble(bool v) { }
        public void SetScalarVisibility(int v) { _scalarVisibility = v != 0; }
        public void ScalarVisibilityOn() { _scalarVisibility = true; }
        public void ScalarVisibilityOff() { _scalarVisibility = false; }
        public void SetLookupTable(vtkScalarsToColors lut) { _lookupTable = lut; }
        public vtkScalarsToColors GetLookupTable() { return _lookupTable; }
        public void SetScalarRange(double a, double b) { _scalarRange = new[] { a, b }; }
        public void SetScalarModeToUseCellData() { _scalarMode = 2; }
        public void SetScalarModeToUsePointData() { _scalarMode = 1; }
        public void SetColorModeToDefault() { }
        public void SetColorModeToMapScalars() { }
        public override void Update() { UpdatePipeline(); }
        internal vtkPolyData RenderPolyData()
        {
            UpdatePipeline();
            return GetInput();
        }
    }

    public partial class vtkTextMapper
    {
        private string _input = "";
        private vtkTextProperty _textProperty = new vtkTextProperty();

        public void SetInput(string text) { _input = text ?? ""; Modified(); }
        public string GetInput() { return _input; }
        public void SetTextProperty(vtkTextProperty p) { _textProperty = p; Modified(); }
        public vtkTextProperty GetTextProperty() { return _textProperty; }
        // The size of the text in display pixels
        public void GetSize(vtkViewport viewport, IntPtr size)
        {
            int[] s = GetSize(viewport);
            Marshal.Copy(s, 0, size, 2);
        }
        public int[] GetSize(vtkViewport viewport) { return TextRenderer.Measure(_input, _textProperty); }
        public void GetSize(vtkViewport viewport, int[] size) { int[] s = GetSize(viewport); size[0] = s[0]; size[1] = s[1]; }
        public int GetWidth(vtkViewport viewport) { return GetSize(viewport)[0]; }
        public int GetHeight(vtkViewport viewport) { return GetSize(viewport)[1]; }
        public int SetConstrainedFontSize(vtkViewport viewport, int width, int height)
        {
            return SetConstrainedFontSize(this, viewport, width, height);
        }
        public static int SetConstrainedFontSize(vtkTextMapper mapper, vtkViewport viewport, int width, int height)
        {
            vtkTextProperty p = mapper._textProperty;
            int size = Math.Max(2, p._fontSize);
            int[] s = mapper.GetSize(viewport);
            while (size > 2 && (s[0] > width || s[1] > height))
            {
                p._fontSize = --size;
                s = mapper.GetSize(viewport);
            }
            while (s[0] < width && s[1] < height && size < 200)
            {
                p._fontSize = ++size;
                s = mapper.GetSize(viewport);
                if (s[0] > width || s[1] > height) { p._fontSize = --size; break; }
            }
            return size;
        }
        public int GetNumberOfLines() { return _input.Split('\n').Length; }
    }

    public partial class vtkLabeledDataMapper
    {
        private vtkTextProperty _labelTextProperty = new vtkTextProperty();
        internal int _labelMode = 1;           // 1 ids, 2 scalars, 5 field data (labels array)
        internal string _fieldDataName;
        internal string _labelFormat;

        public vtkDataSet GetInput() { return GetInputDataObject(0, 0) as vtkDataSet; }
        public vtkTextProperty GetLabelTextProperty() { return _labelTextProperty; }
        public void SetLabelTextProperty(vtkTextProperty p) { _labelTextProperty = p; }
        public void SetLabelModeToLabelIds() { _labelMode = 1; }
        public void SetLabelModeToLabelScalars() { _labelMode = 2; }
        public void SetLabelModeToLabelFieldData() { _labelMode = 5; }
        public void SetLabelModeToLabelVectors() { _labelMode = 3; }
        public void SetLabelMode(int m) { _labelMode = m; }
        public void SetFieldDataName(string name) { _fieldDataName = name; }
        public void CoordinateSystemWorld() { }
        public void CoordinateSystemDisplay() { }
        public void SetLabelFormat(string format) { _labelFormat = format; }
        // The labels and their world positions
        internal IEnumerable<(double[] position, string label)> Labels()
        {
            UpdatePipeline();
            vtkDataSet ds = GetInput();
            if (ds == null) yield break;
            long n = ds.GetNumberOfPoints();
            vtkAbstractArray labels = _fieldDataName != null ? ds.GetPointData().GetAbstractArray(_fieldDataName) : null;
            if (labels == null && _labelMode == 5) labels = ds.GetPointData().GetAbstractArray(0);
            vtkDataArray scalars = ds.GetPointData().GetScalars();
            for (long i = 0; i < n; i++)
            {
                string label;
                if (_labelMode == 5 && labels is vtkStringArray sa) label = sa.GetValue(i);
                else if (_labelMode == 5 && labels is vtkDataArray da) label = FormatNumber(da.GetTuple1(i));
                else if (_labelMode == 2 && scalars != null) label = FormatNumber(scalars.GetTuple1(i));
                else label = i.ToString();
                yield return (ds.GetPoint(i), label);
            }
        }
        private string FormatNumber(double v)
        {
            if (_labelFormat == null) return v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return CFormat.Format(_labelFormat, v);
        }
    }

    // printf style formatting of the label formats of VTK
    internal static class CFormat
    {
        public static string Format(string format, double v)
        {
            var m = System.Text.RegularExpressions.Regex.Match(format, @"%([-+ 0#]*)(\d*)(?:\.(\d+))?([dieEfgGs])");
            if (!m.Success) return v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            int precision = m.Groups[3].Success && m.Groups[3].Value.Length > 0 ? int.Parse(m.Groups[3].Value) : 6;
            string text;
            switch (m.Groups[4].Value)
            {
                case "d": case "i": text = ((long)Math.Round(v)).ToString(); break;
                case "e": text = v.ToString("0." + new string('0', precision) + "e+00", System.Globalization.CultureInfo.InvariantCulture); break;
                case "E": text = v.ToString("0." + new string('0', precision) + "E+00", System.Globalization.CultureInfo.InvariantCulture); break;
                case "f": text = v.ToString("F" + precision, System.Globalization.CultureInfo.InvariantCulture); break;
                default: text = v.ToString("G" + precision, System.Globalization.CultureInfo.InvariantCulture); break;
            }
            return format.Substring(0, m.Index) + text + format.Substring(m.Index + m.Length);
        }
    }

    public partial class vtkActor2D
    {
        protected vtkMapper2D _mapper;
        protected vtkProperty2D _property;
        protected vtkCoordinate _positionCoordinate;
        protected vtkCoordinate _position2Coordinate;
        protected int _layerNumber;

        public vtkActor2D()
        {
            _positionCoordinate = new vtkCoordinate();
            _positionCoordinate.SetCoordinateSystemToViewport();
            _position2Coordinate = new vtkCoordinate();
            _position2Coordinate.SetCoordinateSystemToNormalizedViewport();
            _position2Coordinate.SetValue(0.5, 0.5, 0);
            _position2Coordinate.SetReferenceCoordinate(_positionCoordinate);
        }
        public void SetMapper(vtkMapper2D mapper) { _mapper = mapper; Modified(); }
        public vtkMapper2D GetMapper() { return _mapper; }
        public vtkProperty2D GetProperty() { return _property ?? (_property = new vtkProperty2D()); }
        public void SetProperty(vtkProperty2D p) { _property = p; Modified(); }
        public vtkCoordinate GetPositionCoordinate() { return _positionCoordinate; }
        public vtkCoordinate GetPosition2Coordinate() { return _position2Coordinate; }
        public void SetPosition(double x, double y) { _positionCoordinate.SetValue(x, y, 0); Modified(); }
        public void SetPosition(double[] p) { SetPosition(p[0], p[1]); }
        public double[] GetPosition() { double[] v = _positionCoordinate.GetValue(); return new[] { v[0], v[1] }; }
        public void SetPosition2(double x, double y) { _position2Coordinate.SetValue(x, y, 0); Modified(); }
        public double[] GetPosition2() { double[] v = _position2Coordinate.GetValue(); return new[] { v[0], v[1] }; }
        public void SetDisplayPosition(int x, int y)
        {
            _positionCoordinate.SetCoordinateSystemToDisplay();
            _positionCoordinate.SetValue(x, y, 0);
            Modified();
        }
        public void SetWidth(double w) { double[] v = _position2Coordinate.GetValue(); _position2Coordinate.SetValue(w, v[1], 0); }
        public void SetHeight(double h) { double[] v = _position2Coordinate.GetValue(); _position2Coordinate.SetValue(v[0], h, 0); }
        public double GetWidth() { return _position2Coordinate.GetValue()[0]; }
        public double GetHeight() { return _position2Coordinate.GetValue()[1]; }
        public void SetLayerNumber(int n) { _layerNumber = n; }
        public int GetLayerNumber() { return _layerNumber; }
        public override void ShallowCopy(vtkProp prop)
        {
            base.ShallowCopy(prop);
            if (prop is vtkActor2D a)
            {
                _mapper = a._mapper;
                _property = a._property;
                _positionCoordinate.SetCoordinateSystem(a._positionCoordinate.GetCoordinateSystem());
                _positionCoordinate.SetValue(a._positionCoordinate.GetValue());
            }
        }
        // The display position of the actor; the 2D mappers draw relative to it
        internal double[] DisplayPosition(vtkViewport viewport)
        {
            return _positionCoordinate.GetComputedDoubleDisplayValue(viewport);
        }
        public static vtkActor2D SafeDownCast(vtkObjectBase o) { return o as vtkActor2D; }
    }

    // A text drawn in display coordinates
    public partial class vtkTextActor
    {
        private string _input = "";
        private vtkTextProperty _textProperty = new vtkTextProperty();
        private int _textScaleMode;

        public void SetInput(string text) { _input = text ?? ""; Modified(); }
        public string GetInput() { return _input; }
        public vtkTextProperty GetTextProperty() { return _textProperty; }
        public void SetTextProperty(vtkTextProperty p) { _textProperty = p; Modified(); }
        public void SetTextScaleModeToNone() { _textScaleMode = 0; }
        public void SetTextScaleModeToProp() { _textScaleMode = 1; }
        public void SetTextScaleModeToViewport() { _textScaleMode = 2; }
        public void ScaledTextOff() { _textScaleMode = 0; }
        public void ScaledTextOn() { _textScaleMode = 1; }
        public void GetBoundingBox(vtkViewport viewport, double[] bbox)
        {
            int[] s = TextRenderer.Measure(_input, _textProperty);
            double[] p = DisplayPosition(viewport);
            bbox[0] = p[0]; bbox[1] = p[0] + s[0]; bbox[2] = p[1]; bbox[3] = p[1] + s[1];
        }
        public void GetSize(vtkViewport viewport, double[] size)
        {
            int[] s = TextRenderer.Measure(_input, _textProperty);
            size[0] = s[0];
            size[1] = s[1];
        }
        // The scale of the fonts of VTK with the size of the viewport; the macOS view uses the font sizes as they are
        public static float GetFontScale(vtkViewport viewport) { return 1; }
    }

    // A legend of the scalars; PrePoMax draws its own legend with vtkMaxScalarBarWidget
    public partial class vtkScalarBarActor
    {
        private vtkScalarsToColors _lookupTable;
        private string _title;
        private int _numberOfLabels = 5;
        private string _labelFormat = "%-#6.3g";
        private vtkTextProperty _titleTextProperty = new vtkTextProperty();
        private vtkTextProperty _labelTextProperty = new vtkTextProperty();

        public void SetLookupTable(vtkScalarsToColors lut) { _lookupTable = lut; }
        public vtkScalarsToColors GetLookupTable() { return _lookupTable; }
        public void SetTitle(string title) { _title = title; }
        public void SetNumberOfLabels(int n) { _numberOfLabels = n; }
        public void SetLabelFormat(string f) { _labelFormat = f; }
        public vtkTextProperty GetTitleTextProperty() { return _titleTextProperty; }
        public vtkTextProperty GetLabelTextProperty() { return _labelTextProperty; }
        public void SetTitleTextProperty(vtkTextProperty p) { _titleTextProperty = p; }
        public void SetLabelTextProperty(vtkTextProperty p) { _labelTextProperty = p; }
        public void SetOrientationToVertical() { }
        public void SetOrientationToHorizontal() { }
        public void SetMaximumNumberOfColors(int n) { }
        public void SetMaximumWidthInPixels(int n) { }
        public void SetMaximumHeightInPixels(int n) { }
    }

    // A text with a leader line to a point in the scene
    public partial class vtkCaptionActor2D
    {
        private string _caption = "";
        private vtkTextProperty _captionTextProperty = new vtkTextProperty();
        private double[] _attachmentPoint = new double[3];
        private bool _border = true, _leader = true, _threeDimensionalLeader;
        private double _padding = 3;

        public void SetCaption(string text) { _caption = text ?? ""; Modified(); }
        public string GetCaption() { return _caption; }
        public vtkTextProperty GetCaptionTextProperty() { return _captionTextProperty; }
        public void SetCaptionTextProperty(vtkTextProperty p) { _captionTextProperty = p; Modified(); }
        public vtkTextActor GetTextActor()
        {
            vtkTextActor actor = new vtkTextActor();
            actor.SetInput(_caption);
            actor.SetTextProperty(_captionTextProperty);
            return actor;
        }
        public void SetAttachmentPoint(double x, double y, double z) { _attachmentPoint = new[] { x, y, z }; Modified(); }
        public void SetAttachmentPoint(double[] p) { SetAttachmentPoint(p[0], p[1], p[2]); }
        public double[] GetAttachmentPoint() { return (double[])_attachmentPoint.Clone(); }
        public vtkCoordinate GetAttachmentPointCoordinate()
        {
            vtkCoordinate c = new vtkCoordinate();
            c.SetCoordinateSystemToWorld();
            c.SetValue(_attachmentPoint);
            return c;
        }
        public void SetBorder(int v) { _border = v != 0; }
        public void BorderOn() { _border = true; }
        public void BorderOff() { _border = false; }
        public void SetLeader(int v) { _leader = v != 0; }
        public void LeaderOn() { _leader = true; }
        public void LeaderOff() { _leader = false; }
        public void SetThreeDimensionalLeader(int v) { _threeDimensionalLeader = v != 0; }
        public void ThreeDimensionalLeaderOff() { _threeDimensionalLeader = false; }
        public void ThreeDimensionalLeaderOn() { _threeDimensionalLeader = true; }
        public void SetPadding(int p) { _padding = p; }
        public void SetLeaderGlyph(vtkPolyData glyph) { }
        public void SetLeaderGlyphSize(double s) { }
        public void SetMaximumLeaderGlyphSize(int s) { }
        public void SetDisplayPosition(double x, double y) { GetPositionCoordinate().SetCoordinateSystemToDisplay(); SetPosition(x, y); }
        internal bool BorderVisible { get { return _border; } }
        internal bool LeaderVisible { get { return _leader; } }
        internal double Padding { get { return _padding; } }
        public new double[] GetPosition() { return base.GetPosition(); }
        public void SetPosition(double[] p) { base.SetPosition(p[0], p[1]); }
    }

    // Collections
    public partial class vtkPropCollection
    {
        protected readonly List<vtkProp> _items = new List<vtkProp>();
        private int _traversal;

        public void AddItem(vtkProp prop) { _items.Add(prop); }
        public void RemoveItem(vtkProp prop) { _items.Remove(prop); }
        public void RemoveAllItems() { _items.Clear(); }
        public int GetNumberOfItems() { return _items.Count; }
        public int IsItemPresent(vtkProp prop) { int i = _items.IndexOf(prop); return i + 1; }
        public void InitTraversal() { _traversal = 0; }
        public vtkProp GetNextProp() { return _traversal < _items.Count ? _items[_traversal++] : null; }
        public vtkProp GetLastProp() { return _items.Count > 0 ? _items[_items.Count - 1] : null; }
        public vtkObject GetItemAsObject(int i) { return _items[i]; }
        internal List<vtkProp> Items { get { return _items; } }
    }

    public partial class vtkProp3DCollection
    {
        public void AddItem(vtkProp3D prop) { base.AddItem(prop); }
        public vtkProp3D GetNextProp3D() { return GetNextProp() as vtkProp3D; }
        public vtkProp3D GetLastProp3D() { return GetLastProp() as vtkProp3D; }
    }

    public partial class vtkRendererCollection
    {
        private readonly List<vtkRenderer> _items = new List<vtkRenderer>();
        private int _traversal;

        public void AddItem(vtkRenderer r) { if (!_items.Contains(r)) _items.Add(r); }
        public void RemoveItem(vtkRenderer r) { _items.Remove(r); }
        public int GetNumberOfItems() { return _items.Count; }
        public vtkRenderer GetFirstRenderer() { return _items.Count > 0 ? _items[0] : null; }
        public void InitTraversal() { _traversal = 0; }
        public vtkRenderer GetNextItem() { return _traversal < _items.Count ? _items[_traversal++] : null; }
        public vtkObject GetItemAsObject(int i) { return _items[i]; }
        internal List<vtkRenderer> Items { get { return _items; } }
    }

    // Coordinates
    public partial class vtkCoordinate
    {
        // 0 display, 1 normalized display, 2 viewport, 3 normalized viewport, 4 view, 5 world, 6 user defined
        private int _system = 5;
        private double[] _value = new double[3];
        private vtkCoordinate _reference;
        private vtkViewport _viewport;

        public void SetCoordinateSystemToDisplay() { _system = 0; Modified(); }
        public void SetCoordinateSystemToNormalizedDisplay() { _system = 1; Modified(); }
        public void SetCoordinateSystemToViewport() { _system = 2; Modified(); }
        public void SetCoordinateSystemToNormalizedViewport() { _system = 3; Modified(); }
        public void SetCoordinateSystemToView() { _system = 4; Modified(); }
        public void SetCoordinateSystemToWorld() { _system = 5; Modified(); }
        public void SetCoordinateSystem(int s) { _system = s; Modified(); }
        public int GetCoordinateSystem() { return _system; }
        public void SetValue(double x, double y, double z) { _value = new[] { x, y, z }; Modified(); }
        public void SetValue(double x, double y) { SetValue(x, y, 0); }
        public void SetValue(double[] v) { SetValue(v[0], v[1], v.Length > 2 ? v[2] : 0); }
        public double[] GetValue() { return (double[])_value.Clone(); }
        public void SetReferenceCoordinate(vtkCoordinate c) { _reference = c; Modified(); }
        public vtkCoordinate GetReferenceCoordinate() { return _reference; }
        public void SetViewport(vtkViewport viewport) { _viewport = viewport; }
        public vtkViewport GetViewport() { return _viewport; }
        // The value in display coordinates
        public double[] GetComputedDoubleDisplayValue(vtkViewport viewport)
        {
            vtkViewport vp = viewport ?? _viewport;
            double[] v = (double[])_value.Clone();
            double[] d;
            switch (_system)
            {
                case 0: d = new[] { v[0], v[1] }; break;
                case 1:
                    {
                        int[] size = vp?.GetWindowSize() ?? new[] { 1, 1 };
                        d = new[] { v[0] * size[0], v[1] * size[1] };
                        break;
                    }
                case 2:
                    {
                        double[] origin = vp?.ViewportOriginDisplay() ?? new double[2];
                        d = new[] { v[0] + origin[0], v[1] + origin[1] };
                        break;
                    }
                case 3:
                    {
                        double[] origin = vp?.ViewportOriginDisplay() ?? new double[2];
                        int[] size = vp?.GetSize() ?? new[] { 1, 1 };
                        d = new[] { v[0] * size[0] + origin[0], v[1] * size[1] + origin[1] };
                        break;
                    }
                case 4:
                    {
                        double[] r = vp != null ? vp.ViewToDisplay(v) : v;
                        d = new[] { r[0], r[1] };
                        break;
                    }
                default:
                    {
                        if (vp is vtkRenderer ren)
                        {
                            double[] r = ren.WorldToDisplayPoint(v);
                            d = new[] { r[0], r[1] };
                        }
                        else d = new[] { v[0], v[1] };
                        break;
                    }
            }
            if (_reference != null && _system != 5 && _system != 4)
            {
                double[] r = _reference.GetComputedDoubleDisplayValue(vp);
                if (_system == 0 || _system == 2)
                {
                    d[0] += r[0];
                    d[1] += r[1];
                }
                else
                {
                    // Normalized values are relative to the reference point
                    double[] origin = _system == 3 ? vp?.ViewportOriginDisplay() ?? new double[2] : new double[2];
                    d[0] += r[0] - origin[0];
                    d[1] += r[1] - origin[1];
                }
            }
            return d;
        }
        public int[] GetComputedDisplayValue(vtkViewport viewport)
        {
            double[] d = GetComputedDoubleDisplayValue(viewport);
            return new[] { (int)Math.Floor(d[0] + 0.5), (int)Math.Floor(d[1] + 0.5) };
        }
        public double[] GetComputedDoubleViewportValue(vtkViewport viewport)
        {
            vtkViewport vp = viewport ?? _viewport;
            double[] d = GetComputedDoubleDisplayValue(vp);
            double[] origin = vp?.ViewportOriginDisplay() ?? new double[2];
            return new[] { d[0] - origin[0], d[1] - origin[1] };
        }
        public int[] GetComputedViewportValue(vtkViewport viewport)
        {
            double[] d = GetComputedDoubleViewportValue(viewport);
            return new[] { (int)Math.Floor(d[0] + 0.5), (int)Math.Floor(d[1] + 0.5) };
        }
        public int[] GetComputedLocalDisplayValue(vtkViewport viewport) { return GetComputedDisplayValue(viewport); }
        public double[] GetComputedWorldValue(vtkViewport viewport)
        {
            vtkViewport vp = viewport ?? _viewport;
            if (_system == 5) return (double[])_value.Clone();
            double[] d = GetComputedDoubleDisplayValue(vp);
            if (vp is vtkRenderer ren) return ren.DisplayToWorldPoint(new[] { d[0], d[1], _value[2] });
            return new[] { d[0], d[1], 0 };
        }
        public double[] GetComputedValue(vtkViewport viewport)
        {
            switch (_system)
            {
                case 5: return GetComputedWorldValue(viewport);
                case 0: case 1: { double[] d = GetComputedDoubleDisplayValue(viewport); return new[] { d[0], d[1], 0 }; }
                default: { double[] d = GetComputedDoubleViewportValue(viewport); return new[] { d[0], d[1], 0 }; }
            }
        }
    }

    // Camera
    public partial class vtkCamera
    {
        private double[] _position = { 0, 0, 1 };
        private double[] _focalPoint = { 0, 0, 0 };
        private double[] _viewUp = { 0, 1, 0 };
        private double _viewAngle = 30;
        private double[] _clippingRange = { 0.01, 1000.01 };
        private bool _parallelProjection;
        private double _parallelScale = 1;
        private double _distance = 1;
        private double[] _directionOfProjection = { 0, 0, -1 };
        private double[] _windowCenter = { 0, 0 };
        private bool _useHorizontalViewAngle;
        private double _thickness = 1000;

        public double[] GetPosition() { return (double[])_position.Clone(); }
        public void GetPosition(ref double x, ref double y, ref double z) { x = _position[0]; y = _position[1]; z = _position[2]; }
        public void SetPosition(double x, double y, double z)
        {
            _position = new[] { x, y, z };
            ComputeDistance();
            Modified();
        }
        public void SetPosition(double[] p) { SetPosition(p[0], p[1], p[2]); }
        public double[] GetFocalPoint() { return (double[])_focalPoint.Clone(); }
        public void SetFocalPoint(double x, double y, double z)
        {
            _focalPoint = new[] { x, y, z };
            ComputeDistance();
            Modified();
        }
        public void SetFocalPoint(double[] p) { SetFocalPoint(p[0], p[1], p[2]); }
        public double[] GetViewUp() { return (double[])_viewUp.Clone(); }
        public void SetViewUp(double x, double y, double z)
        {
            double l = Math.Sqrt(x * x + y * y + z * z);
            if (l == 0) { x = 0; y = 1; z = 0; l = 1; }
            _viewUp = new[] { x / l, y / l, z / l };
            Modified();
        }
        public void SetViewUp(double[] v) { SetViewUp(v[0], v[1], v[2]); }
        public double GetViewAngle() { return _viewAngle; }
        public void SetViewAngle(double a) { _viewAngle = Math.Max(0.00000001, Math.Min(179, a)); Modified(); }
        public void SetUseHorizontalViewAngle(int v) { _useHorizontalViewAngle = v != 0; }
        public double[] GetClippingRange() { return (double[])_clippingRange.Clone(); }
        public void GetClippingRange(ref double near, ref double far) { near = _clippingRange[0]; far = _clippingRange[1]; }
        public void SetClippingRange(double near, double far)
        {
            if (near > far) { double t = near; near = far; far = t; }
            if (far - near < 1e-20) far = near + 1e-20;
            _clippingRange = new[] { near, far };
            _thickness = far - near;
            Modified();
        }
        public void SetClippingRange(double[] r) { SetClippingRange(r[0], r[1]); }
        public double GetThickness() { return _thickness; }
        public void SetThickness(double t) { SetClippingRange(_clippingRange[0], _clippingRange[0] + t); }
        public int GetParallelProjection() { return _parallelProjection ? 1 : 0; }
        public void SetParallelProjection(int v) { _parallelProjection = v != 0; Modified(); }
        public void SetParallelProjection(bool v) { SetParallelProjection(v ? 1 : 0); }
        public void ParallelProjectionOn() { SetParallelProjection(1); }
        public void ParallelProjectionOff() { SetParallelProjection(0); }
        public double GetParallelScale() { return _parallelScale; }
        public void SetParallelScale(double s) { _parallelScale = s; Modified(); }
        public double GetDistance() { return _distance; }
        // Moves the focal point to the distance from the position
        public void SetDistance(double d)
        {
            if (d < 1e-20) d = 1e-20;
            _distance = d;
            for (int i = 0; i < 3; i++) _focalPoint[i] = _position[i] + _directionOfProjection[i] * d;
            Modified();
        }
        public double[] GetDirectionOfProjection() { return (double[])_directionOfProjection.Clone(); }
        public double[] GetViewPlaneNormal()
        {
            return new[] { -_directionOfProjection[0], -_directionOfProjection[1], -_directionOfProjection[2] };
        }
        public void SetWindowCenter(double x, double y) { _windowCenter = new[] { x, y }; Modified(); }
        public double[] GetWindowCenter() { return (double[])_windowCenter.Clone(); }
        private void ComputeDistance()
        {
            double[] d = { _focalPoint[0] - _position[0], _focalPoint[1] - _position[1], _focalPoint[2] - _position[2] };
            _distance = Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
            if (_distance < 1e-20)
            {
                _distance = 1e-20;
                // The focal point is moved away from the position
                double[] vt = _directionOfProjection;
                for (int i = 0; i < 3; i++) _focalPoint[i] = _position[i] + vt[i] * _distance;
            }
            else
            {
                _directionOfProjection = new[] { d[0] / _distance, d[1] / _distance, d[2] / _distance };
            }
        }
        // The orientation
        public void OrthogonalizeViewUp()
        {
            double[] m = ViewMatrix();
            SetViewUp(m[4], m[5], m[6]);
        }
        public void Azimuth(double angle)
        {
            // Rotation of the position about the view up vector through the focal point
            double[] r = vtkMatrix4x4.Rotation(angle, _viewUp[0], _viewUp[1], _viewUp[2]);
            SetPosition(RotateAbout(_position, _focalPoint, r));
        }
        public void Elevation(double angle)
        {
            // Rotation of the position about the right axis through the focal point
            double[] m = ViewMatrix();
            double[] axis = { -m[0], -m[1], -m[2] };
            double[] savedViewUp = (double[])_viewUp.Clone();
            double[] r = vtkMatrix4x4.Rotation(angle, axis[0], axis[1], axis[2]);
            _viewUp = vtkMatrix4x4.TransformVector(r, _viewUp[0], _viewUp[1], _viewUp[2]);
            SetPosition(RotateAbout(_position, _focalPoint, r));
            _viewUp = savedViewUp;
            Modified();
        }
        public void Roll(double angle)
        {
            double[] r = vtkMatrix4x4.Rotation(angle, _directionOfProjection[0], _directionOfProjection[1],
                                               _directionOfProjection[2]);
            SetViewUp(vtkMatrix4x4.TransformVector(r, _viewUp[0], _viewUp[1], _viewUp[2]));
        }
        public void SetRoll(double angle)
        {
            Roll(angle - GetRoll());
        }
        public double GetRoll()
        {
            // The angle between the view up vector and the projection of the y axis
            double[] orientation = GetOrientation();
            return orientation[2];
        }
        public void Yaw(double angle)
        {
            double[] r = vtkMatrix4x4.Rotation(angle, _viewUp[0], _viewUp[1], _viewUp[2]);
            SetFocalPoint(RotateAbout(_focalPoint, _position, r));
        }
        public void Pitch(double angle)
        {
            double[] m = ViewMatrix();
            double[] axis = { m[0], m[1], m[2] };
            double[] r = vtkMatrix4x4.Rotation(angle, axis[0], axis[1], axis[2]);
            SetFocalPoint(RotateAbout(_focalPoint, _position, r));
        }
        private static double[] RotateAbout(double[] p, double[] center, double[] r)
        {
            double[] v = vtkMatrix4x4.TransformVector(r, p[0] - center[0], p[1] - center[1], p[2] - center[2]);
            return new[] { center[0] + v[0], center[1] + v[1], center[2] + v[2] };
        }
        public void Dolly(double value)
        {
            if (value <= 0) return;
            double d = _distance / value;
            SetPosition(_focalPoint[0] - d * _directionOfProjection[0], _focalPoint[1] - d * _directionOfProjection[1],
                        _focalPoint[2] - d * _directionOfProjection[2]);
        }
        public void Zoom(double amount)
        {
            if (amount <= 0) return;
            if (_parallelProjection) SetParallelScale(_parallelScale / amount);
            else SetViewAngle(_viewAngle / amount);
        }
        public void ApplyTransform(vtkTransform t)
        {
            double[] pos = t.TransformPoint(_position);
            double[] fp = t.TransformPoint(_focalPoint);
            double[] upPoint = t.TransformPoint(_position[0] + _viewUp[0], _position[1] + _viewUp[1], _position[2] + _viewUp[2]);
            SetPosition(pos);
            SetFocalPoint(fp);
            SetViewUp(upPoint[0] - pos[0], upPoint[1] - pos[1], upPoint[2] - pos[2]);
        }
        // The orientation as rotations about x, y and z in degrees
        public double[] GetOrientation()
        {
            vtkTransform t = new vtkTransform();
            t.SetMatrix(ViewMatrix());
            return t.GetOrientation();
        }
        public double[] GetOrientationWXYZ()
        {
            vtkTransform t = new vtkTransform();
            double[] m = ViewMatrix();
            // The rotation part of the inverse of the view matrix
            double[] r = { m[0], m[4], m[8], 0, m[1], m[5], m[9], 0, m[2], m[6], m[10], 0, 0, 0, 0, 1 };
            t.SetMatrix(r);
            return t.GetOrientationWXYZ();
        }
        public void DeepCopy(vtkCamera c)
        {
            _position = (double[])c._position.Clone();
            _focalPoint = (double[])c._focalPoint.Clone();
            _viewUp = (double[])c._viewUp.Clone();
            _viewAngle = c._viewAngle;
            _clippingRange = (double[])c._clippingRange.Clone();
            _parallelProjection = c._parallelProjection;
            _parallelScale = c._parallelScale;
            _distance = c._distance;
            _directionOfProjection = (double[])c._directionOfProjection.Clone();
            _windowCenter = (double[])c._windowCenter.Clone();
            _useHorizontalViewAngle = c._useHorizontalViewAngle;
            _thickness = c._thickness;
            Modified();
        }
        public void ShallowCopy(vtkCamera c) { DeepCopy(c); }
        // Matrices: the view matrix (world to eye) and the projection matrix (eye to clip, z from nearz to farz)
        internal double[] ViewMatrix()
        {
            double[] f = _directionOfProjection;
            double[] s = vtkMath.Cross(f, _viewUp);
            if (vtkMath.Normalize(s) < 1e-20)
            {
                // The view up is parallel to the direction of projection
                double[] alt = Math.Abs(f[2]) < 0.9 ? new double[] { 0, 0, 1 } : new double[] { 0, 1, 0 };
                s = vtkMath.Cross(f, alt);
                vtkMath.Normalize(s);
            }
            double[] u = vtkMath.Cross(s, f);
            double[] p = _position;
            return new[] {
                s[0], s[1], s[2], -(s[0] * p[0] + s[1] * p[1] + s[2] * p[2]),
                u[0], u[1], u[2], -(u[0] * p[0] + u[1] * p[1] + u[2] * p[2]),
                -f[0], -f[1], -f[2], f[0] * p[0] + f[1] * p[1] + f[2] * p[2],
                0, 0, 0, 1 };
        }
        internal double[] ProjectionMatrix(double aspect, double nearz, double farz)
        {
            double n = _clippingRange[0], f = _clippingRange[1];
            double[] m;
            if (_parallelProjection)
            {
                double width = _parallelScale * aspect, height = _parallelScale;
                double xmin = (_windowCenter[0] - 1) * width, xmax = (_windowCenter[0] + 1) * width;
                double ymin = (_windowCenter[1] - 1) * height, ymax = (_windowCenter[1] + 1) * height;
                m = new[] {
                    2 / (xmax - xmin), 0, 0, -(xmax + xmin) / (xmax - xmin),
                    0, 2 / (ymax - ymin), 0, -(ymax + ymin) / (ymax - ymin),
                    0, 0, -2 / (f - n), -(f + n) / (f - n),
                    0, 0, 0, 1 };
            }
            else
            {
                double tmp = Math.Tan(_viewAngle * Math.PI / 360);
                double width, height;
                if (_useHorizontalViewAngle) { width = n * tmp; height = n * tmp / aspect; }
                else { width = n * tmp * aspect; height = n * tmp; }
                double xmin = (_windowCenter[0] - 1) * width, xmax = (_windowCenter[0] + 1) * width;
                double ymin = (_windowCenter[1] - 1) * height, ymax = (_windowCenter[1] + 1) * height;
                m = new[] {
                    2 * n / (xmax - xmin), 0, (xmax + xmin) / (xmax - xmin), 0,
                    0, 2 * n / (ymax - ymin), (ymax + ymin) / (ymax - ymin), 0,
                    0, 0, -(f + n) / (f - n), -2 * f * n / (f - n),
                    0, 0, -1, 0 };
            }
            // Map the depth from [-1, 1] to [nearz, farz]
            if (nearz != -1 || farz != 1)
            {
                double a = (farz - nearz) / 2, b = (farz + nearz) / 2;
                for (int j = 0; j < 4; j++) m[8 + j] = a * m[8 + j] + b * m[12 + j];
            }
            return m;
        }
        internal double[] CompositeMatrix(double aspect, double nearz, double farz)
        {
            return vtkMatrix4x4.Multiply(ProjectionMatrix(aspect, nearz, farz), ViewMatrix());
        }
        public vtkMatrix4x4 GetViewTransformMatrix()
        {
            vtkMatrix4x4 m = new vtkMatrix4x4();
            m._e = ViewMatrix();
            return m;
        }
        public vtkMatrix4x4 GetProjectionTransformMatrix(double aspect, double nearz, double farz)
        {
            vtkMatrix4x4 m = new vtkMatrix4x4();
            m._e = ProjectionMatrix(aspect, nearz, farz);
            return m;
        }
        public vtkMatrix4x4 GetCompositeProjectionTransformMatrix(double aspect, double nearz, double farz)
        {
            vtkMatrix4x4 m = new vtkMatrix4x4();
            m._e = CompositeMatrix(aspect, nearz, farz);
            return m;
        }
        public vtkTransform GetViewTransformObject()
        {
            vtkTransform t = new vtkTransform();
            t.SetMatrix(ViewMatrix());
            return t;
        }
        // The planes of the view frustum as a, b, c, d of the inward pointing plane equations
        public void GetFrustumPlanes(double aspect, double[] planes)
        {
            double[] m = CompositeMatrix(aspect, -1, 1);
            // left, right, bottom, top, near, far (Gribb-Hartmann)
            for (int i = 0; i < 3; i++)
            {
                for (int k = 0; k < 2; k++)
                {
                    int p = 2 * i + k;
                    double sign = k == 0 ? 1 : -1;
                    for (int j = 0; j < 4; j++) planes[4 * p + j] = m[12 + j] + sign * m[4 * i + j];
                    double l = Math.Sqrt(planes[4 * p] * planes[4 * p] + planes[4 * p + 1] * planes[4 * p + 1] +
                                         planes[4 * p + 2] * planes[4 * p + 2]);
                    if (l > 0) for (int j = 0; j < 4; j++) planes[4 * p + j] /= l;
                }
            }
        }
        public void ComputeViewPlaneNormal() { }
        public void SetViewPlaneNormal(double x, double y, double z) { }
    }

    // Lights
    public partial class vtkLight
    {
        internal double[] _position = { 0, 0, 1 };
        internal double[] _focalPoint = { 0, 0, 0 };
        internal double[] _color = { 1, 1, 1 };
        internal double _intensity = 1;
        internal int _lightType = 2;       // 1 headlight, 2 camera light, 3 scene light
        internal bool _switch = true;
        internal bool _positional;

        public void SetPosition(double x, double y, double z) { _position = new[] { x, y, z }; Modified(); }
        public void SetPosition(double[] p) { SetPosition(p[0], p[1], p[2]); }
        public double[] GetPosition() { return (double[])_position.Clone(); }
        public void SetFocalPoint(double x, double y, double z) { _focalPoint = new[] { x, y, z }; Modified(); }
        public void SetFocalPoint(double[] p) { SetFocalPoint(p[0], p[1], p[2]); }
        public double[] GetFocalPoint() { return (double[])_focalPoint.Clone(); }
        public void SetColor(double r, double g, double b) { _color = new[] { r, g, b }; Modified(); }
        public void SetDiffuseColor(double r, double g, double b) { SetColor(r, g, b); }
        public void SetSpecularColor(double r, double g, double b) { }
        public void SetAmbientColor(double r, double g, double b) { }
        public double[] GetColor() { return (double[])_color.Clone(); }
        public void SetIntensity(double i) { _intensity = i; Modified(); }
        public double GetIntensity() { return _intensity; }
        public void SetLightTypeToHeadlight() { _lightType = 1; Modified(); }
        public void SetLightTypeToCameraLight() { _lightType = 2; Modified(); }
        public void SetLightTypeToSceneLight() { _lightType = 3; Modified(); }
        public int GetLightType() { return _lightType; }
        public void SetSwitch(int v) { _switch = v != 0; Modified(); }
        public int GetSwitch() { return _switch ? 1 : 0; }
        public void SwitchOn() { SetSwitch(1); }
        public void SwitchOff() { SetSwitch(0); }
        public void SetPositional(int v) { _positional = v != 0; }
        public void PositionalOff() { _positional = false; }
        public void PositionalOn() { _positional = true; }
        // The direction to the light in camera (eye) coordinates
        internal double[] EyeDirection(vtkCamera camera)
        {
            if (_lightType == 1) return new double[] { 0, 0, 1 };
            if (_lightType == 2)
            {
                // Camera light coordinates: camera at (0, 0, 1) looking at the origin with up (0, 1, 0)
                double[] d = { _position[0] - _focalPoint[0], _position[1] - _focalPoint[1], _position[2] - _focalPoint[2] };
                vtkMath.Normalize(d);
                return d;
            }
            double[] w = { _position[0] - _focalPoint[0], _position[1] - _focalPoint[1], _position[2] - _focalPoint[2] };
            vtkMath.Normalize(w);
            double[] e = vtkMatrix4x4.TransformVector(camera.ViewMatrix(), w[0], w[1], w[2]);
            vtkMath.Normalize(e);
            return e;
        }
    }

    // Viewports and renderers
    public partial class vtkViewport
    {
        protected double[] _viewport = { 0, 0, 1, 1 };
        protected double[] _background = { 0, 0, 0 };
        protected double[] _background2 = { 0.2, 0.2, 0.2 };
        protected bool _gradientBackground;
        protected readonly List<vtkProp> _props = new List<vtkProp>();
        protected double[] _displayPoint = new double[3];
        protected double[] _viewPoint = new double[3];
        protected double[] _worldPoint = { 0, 0, 0, 1 };
        protected vtkWindow _window;
        protected bool _interactive = true;

        public void SetViewport(double x0, double y0, double x1, double y1) { _viewport = new[] { x0, y0, x1, y1 }; Modified(); }
        public double[] GetViewport() { return (double[])_viewport.Clone(); }
        public void SetBackground(double r, double g, double b) { _background = new[] { r, g, b }; Modified(); }
        public void SetBackground(double[] c) { SetBackground(c[0], c[1], c[2]); }
        public double[] GetBackground() { return (double[])_background.Clone(); }
        public void SetBackground2(double r, double g, double b) { _background2 = new[] { r, g, b }; Modified(); }
        public double[] GetBackground2() { return (double[])_background2.Clone(); }
        public void SetGradientBackground(bool v) { _gradientBackground = v; Modified(); }
        public void SetGradientBackground(int v) { SetGradientBackground(v != 0); }
        public bool GetGradientBackground() { return _gradientBackground; }
        public void GradientBackgroundOn() { SetGradientBackground(true); }
        public void GradientBackgroundOff() { SetGradientBackground(false); }
        public void SetInteractive(int v) { _interactive = v != 0; }
        public int GetInteractive() { return _interactive ? 1 : 0; }
        public void InteractiveOn() { _interactive = true; }
        public void InteractiveOff() { _interactive = false; }
        // Props
        public void AddViewProp(vtkProp prop)
        {
            if (prop == null || _props.Contains(prop)) return;
            _props.Add(prop);
            prop._renderer = this as vtkRenderer;
            Modified();
        }
        public void AddActor2D(vtkProp prop) { AddViewProp(prop); }
        public void RemoveViewProp(vtkProp prop) { if (_props.Remove(prop)) Modified(); }
        public void RemoveActor2D(vtkProp prop) { RemoveViewProp(prop); }
        public void RemoveAllViewProps() { _props.Clear(); Modified(); }
        public int HasViewProp(vtkProp prop) { return _props.Contains(prop) ? 1 : 0; }
        public vtkPropCollection GetViewProps()
        {
            vtkPropCollection c = new vtkPropCollection();
            foreach (vtkProp p in _props) c.AddItem(p);
            return c;
        }
        public vtkPropCollection GetActors2D()
        {
            vtkPropCollection c = new vtkPropCollection();
            foreach (vtkProp p in _props) if (p is vtkActor2D) c.AddItem(p);
            return c;
        }
        internal List<vtkProp> Props { get { return _props; } }
        // Window
        public vtkWindow GetVTKWindow() { return _window; }
        public int[] GetWindowSize() { return _window != null ? _window.GetSize() : new[] { 1, 1 }; }
        public int[] GetSize()
        {
            int[] ws = GetWindowSize();
            return new[] { (int)Math.Round(ws[0] * (_viewport[2] - _viewport[0])),
                           (int)Math.Round(ws[1] * (_viewport[3] - _viewport[1])) };
        }
        public int[] GetOrigin()
        {
            int[] ws = GetWindowSize();
            return new[] { (int)Math.Round(ws[0] * _viewport[0]), (int)Math.Round(ws[1] * _viewport[1]) };
        }
        internal double[] ViewportOriginDisplay()
        {
            int[] ws = GetWindowSize();
            return new[] { ws[0] * _viewport[0], ws[1] * _viewport[1] };
        }
        public double GetAspect() { int[] s = GetSize(); return s[1] > 0 ? (double)s[0] / s[1] : 1; }
        public double GetTiledAspectRatio() { return GetAspect(); }
        public bool IsInViewport(int x, int y)
        {
            int[] ws = GetWindowSize();
            return x >= _viewport[0] * ws[0] && x <= _viewport[2] * ws[0] && y >= _viewport[1] * ws[1] && y <= _viewport[3] * ws[1];
        }
        // Coordinate conversions
        public void SetDisplayPoint(double x, double y, double z) { _displayPoint = new[] { x, y, z }; }
        public void SetDisplayPoint(double[] p) { SetDisplayPoint(p[0], p[1], p[2]); }
        public double[] GetDisplayPoint() { return (double[])_displayPoint.Clone(); }
        public void SetViewPoint(double x, double y, double z) { _viewPoint = new[] { x, y, z }; }
        public double[] GetViewPoint() { return (double[])_viewPoint.Clone(); }
        public void SetWorldPoint(double x, double y, double z, double w) { _worldPoint = new[] { x, y, z, w }; }
        public void SetWorldPoint(double[] p) { SetWorldPoint(p[0], p[1], p[2], p.Length > 3 ? p[3] : 1); }
        public double[] GetWorldPoint() { return (double[])_worldPoint.Clone(); }
        public void DisplayToView() { _viewPoint = DisplayToViewCoordinates(_displayPoint); }
        public void ViewToDisplay() { _displayPoint = ViewToDisplay(_viewPoint); }
        public virtual void WorldToView() { }
        public virtual void ViewToWorld() { }
        public void DisplayToWorld() { DisplayToView(); ViewToWorld(); }
        public void WorldToDisplay() { WorldToView(); ViewToDisplay(); }
        internal double[] DisplayToViewCoordinates(double[] d)
        {
            int[] ws = GetWindowSize();
            double sx = ws[0], sy = ws[1];
            return new[] {
                2.0 * (d[0] - sx * _viewport[0]) / (sx * (_viewport[2] - _viewport[0])) - 1.0,
                2.0 * (d[1] - sy * _viewport[1]) / (sy * (_viewport[3] - _viewport[1])) - 1.0,
                d.Length > 2 ? d[2] : 0 };
        }
        internal double[] ViewToDisplay(double[] v)
        {
            int[] ws = GetWindowSize();
            double sx = ws[0], sy = ws[1];
            return new[] {
                (v[0] + 1.0) * (sx * (_viewport[2] - _viewport[0])) / 2.0 + sx * _viewport[0],
                (v[1] + 1.0) * (sy * (_viewport[3] - _viewport[1])) / 2.0 + sy * _viewport[1],
                v.Length > 2 ? v[2] : 0 };
        }
        public void NormalizedDisplayToDisplay(ref double u, ref double v)
        {
            int[] ws = GetWindowSize();
            u *= ws[0];
            v *= ws[1];
        }
        public void DisplayToNormalizedDisplay(ref double u, ref double v)
        {
            int[] ws = GetWindowSize();
            u /= ws[0];
            v /= ws[1];
        }
    }

    public partial class vtkRenderer
    {
        private vtkCamera _activeCamera;
        private readonly List<vtkLight> _lights = new List<vtkLight>();
        private int _layer;
        private bool _lightFollowCamera = true;
        private bool _twoSidedLighting = true;
        private bool _preserveDepthBuffer;
        private bool _erase = true;
        private double _pickedZ;
        private double[] _ambient = { 1, 1, 1 };
        private bool _automaticLightCreation = true;
        private double _nearClippingPlaneTolerance;

        public vtkCamera GetActiveCamera()
        {
            if (_activeCamera == null)
            {
                _activeCamera = new vtkCamera();
                ResetCamera();
            }
            return _activeCamera;
        }
        public void SetActiveCamera(vtkCamera camera) { _activeCamera = camera; Modified(); }
        public vtkCamera MakeCamera() { return new vtkCamera(); }
        public void SetLayer(int layer) { _layer = layer; Modified(); }
        public int GetLayer() { return _layer; }
        public void AddLight(vtkLight light) { if (!_lights.Contains(light)) _lights.Add(light); Modified(); }
        public void RemoveLight(vtkLight light) { _lights.Remove(light); Modified(); }
        public void RemoveAllLights() { _lights.Clear(); Modified(); }
        internal List<vtkLight> Lights { get { return _lights; } }
        public void SetLightFollowCamera(int v) { _lightFollowCamera = v != 0; }
        public int GetLightFollowCamera() { return _lightFollowCamera ? 1 : 0; }
        public void LightFollowCameraOn() { _lightFollowCamera = true; }
        public void LightFollowCameraOff() { _lightFollowCamera = false; }
        public void SetTwoSidedLighting(int v) { _twoSidedLighting = v != 0; Modified(); }
        public void TwoSidedLightingOn() { SetTwoSidedLighting(1); }
        public void TwoSidedLightingOff() { SetTwoSidedLighting(0); }
        internal bool TwoSidedLighting { get { return _twoSidedLighting; } }
        public void SetAutomaticLightCreation(int v) { _automaticLightCreation = v != 0; }
        internal bool AutomaticLightCreation { get { return _automaticLightCreation; } }
        public void SetAmbient(double r, double g, double b) { _ambient = new[] { r, g, b }; }
        public int UpdateLightsGeometryToFollowCamera() { return 1; }
        public void UpdateLightGeometry() { }
        public void SetPreserveDepthBuffer(int v) { _preserveDepthBuffer = v != 0; }
        public int GetPreserveDepthBuffer() { return _preserveDepthBuffer ? 1 : 0; }
        internal bool PreserveDepthBuffer { get { return _preserveDepthBuffer; } }
        public void SetErase(int v) { _erase = v != 0; }
        public int GetErase() { return _erase ? 1 : 0; }
        public void SetUseDepthPeeling(int v) { }
        public void SetMaximumNumberOfPeels(int v) { }
        public void SetOcclusionRatio(double v) { }
        public void SetNearClippingPlaneTolerance(double v) { _nearClippingPlaneTolerance = v; }
        public void SetRenderWindow(vtkRenderWindow window) { _window = window; }
        public vtkRenderWindow GetRenderWindow() { return _window as vtkRenderWindow; }
        public void Render() { GetRenderWindow()?.RequestRender(); }
        public void Clear() { }
        public double GetPickedZ() { return _pickedZ; }
        internal void SetPickedZ(double z) { _pickedZ = z; }
        public double GetZ(int x, int y) { return 1; }
        // Props
        public void AddActor(vtkProp prop) { AddViewProp(prop); }
        public void RemoveActor(vtkProp prop) { RemoveViewProp(prop); }
        public void AddVolume(vtkProp prop) { AddViewProp(prop); }
        public vtkActorCollection GetActors()
        {
            vtkActorCollection c = new vtkActorCollection();
            foreach (vtkProp p in _props) if (p is vtkActor a) c.AddItem(a);
            return c;
        }
        public vtkLightCollection GetLights()
        {
            vtkLightCollection c = new vtkLightCollection();
            foreach (vtkLight l in _lights) c.AddItem(l);
            return c;
        }
        public int VisibleActorCount() { return _props.Count(p => p.Visible && p is vtkActor); }
        // Bounds
        public double[] ComputeVisiblePropBounds()
        {
            double[] b = { double.MaxValue, -double.MaxValue, double.MaxValue, -double.MaxValue, double.MaxValue,
                           -double.MaxValue };
            bool found = false;
            foreach (vtkProp prop in _props)
            {
                if (!prop.Visible || !prop.GetUseBounds()) continue;
                if (!(prop is vtkProp3D)) continue;
                double[] pb = prop.GetBounds();
                if (pb == null || pb[1] < pb[0] || double.IsNaN(pb[0])) continue;
                found = true;
                for (int i = 0; i < 3; i++)
                {
                    b[2 * i] = Math.Min(b[2 * i], pb[2 * i]);
                    b[2 * i + 1] = Math.Max(b[2 * i + 1], pb[2 * i + 1]);
                }
            }
            if (!found) vtkMath.UninitializeBounds(b);
            return b;
        }
        public void ComputeVisiblePropBounds(double[] bounds) { Array.Copy(ComputeVisiblePropBounds(), bounds, 6); }
        public void ComputeVisiblePropBounds(IntPtr bounds) { Marshal.Copy(ComputeVisiblePropBounds(), 0, bounds, 6); }
        public void ResetCamera(IntPtr bounds)
        {
            double[] b = new double[6];
            Marshal.Copy(bounds, b, 0, 6);
            ResetCamera(b);
        }
        // Camera reset
        public void ResetCamera()
        {
            double[] b = ComputeVisiblePropBounds();
            if (!vtkMath.AreBoundsInitialized(b)) return;
            ResetCamera(b);
        }
        public void ResetCamera(double x0, double x1, double y0, double y1, double z0, double z1)
        {
            ResetCamera(new[] { x0, x1, y0, y1, z0, z1 });
        }
        public void ResetCamera(double[] bounds)
        {
            vtkCamera camera = _activeCamera ?? (_activeCamera = new vtkCamera());
            double[] center = { (bounds[0] + bounds[1]) / 2, (bounds[2] + bounds[3]) / 2, (bounds[4] + bounds[5]) / 2 };
            double w1 = bounds[1] - bounds[0], w2 = bounds[3] - bounds[2], w3 = bounds[5] - bounds[4];
            double radius = 0.5 * Math.Sqrt(w1 * w1 + w2 * w2 + w3 * w3);
            if (radius == 0) radius = 0.5;
            double angle = camera.GetViewAngle() * Math.PI / 180;
            double distance = radius / Math.Sin(angle * 0.5);
            double[] vn = camera.GetViewPlaneNormal();
            double[] vup = camera.GetViewUp();
            if (Math.Abs(vtkMath.Dot(vup, vn)) > 0.999) camera.SetViewUp(-vup[2], vup[0], vup[1]);
            camera.SetFocalPoint(center);
            camera.SetPosition(center[0] + distance * vn[0], center[1] + distance * vn[1], center[2] + distance * vn[2]);
            ResetCameraClippingRange(bounds);
            camera.SetParallelScale(radius);
        }
        public void ResetCameraClippingRange()
        {
            double[] b = ComputeVisiblePropBounds();
            if (!vtkMath.AreBoundsInitialized(b)) return;
            ResetCameraClippingRange(b);
        }
        public void ResetCameraClippingRange(double x0, double x1, double y0, double y1, double z0, double z1)
        {
            ResetCameraClippingRange(new[] { x0, x1, y0, y1, z0, z1 });
        }
        public void ResetCameraClippingRange(double[] bounds)
        {
            vtkCamera camera = GetActiveCamera();
            double[] vn = camera.GetViewPlaneNormal();
            double[] position = camera.GetPosition();
            double a = -vn[0], b = -vn[1], c = -vn[2];
            double d = -(a * position[0] + b * position[1] + c * position[2]);
            double near = double.MaxValue, far = -double.MaxValue;
            for (int k = 0; k < 2; k++)
                for (int j = 0; j < 2; j++)
                    for (int i = 0; i < 2; i++)
                    {
                        double dist = a * bounds[i] + b * bounds[2 + j] + c * bounds[4 + k] + d;
                        near = Math.Min(near, dist);
                        far = Math.Max(far, dist);
                    }
            near = 0.99 * near - (far - near) * 0.5;
            far = 1.01 * far + (far - near) * 0.5;
            double tolerance = _nearClippingPlaneTolerance > 0 ? _nearClippingPlaneTolerance : 0.001;
            if (near < tolerance * far) near = tolerance * far;
            if (camera.GetParallelProjection() == 0 && near <= 0) near = tolerance * far;
            camera.SetClippingRange(near, far);
        }
        // Coordinates
        public override void WorldToView()
        {
            _viewPoint = WorldToViewPoint(_worldPoint);
        }
        public override void ViewToWorld()
        {
            double[] m = vtkMatrix4x4.Inverse(GetActiveCamera().CompositeMatrix(GetTiledAspectRatio(), 0, 1));
            double[] v = _viewPoint;
            double[] r = new double[4];
            for (int i = 0; i < 4; i++) r[i] = m[4 * i] * v[0] + m[4 * i + 1] * v[1] + m[4 * i + 2] * v[2] + m[4 * i + 3];
            if (r[3] != 0) { r[0] /= r[3]; r[1] /= r[3]; r[2] /= r[3]; r[3] = 1; }
            _worldPoint = r;
        }
        internal double[] WorldToViewPoint(double[] w)
        {
            double[] m = GetActiveCamera().CompositeMatrix(GetTiledAspectRatio(), 0, 1);
            double ww = w.Length > 3 ? w[3] : 1;
            double[] r = new double[4];
            for (int i = 0; i < 4; i++) r[i] = m[4 * i] * w[0] + m[4 * i + 1] * w[1] + m[4 * i + 2] * w[2] + m[4 * i + 3] * ww;
            if (r[3] != 0) { r[0] /= r[3]; r[1] /= r[3]; r[2] /= r[3]; }
            return new[] { r[0], r[1], r[2] };
        }
        internal double[] WorldToDisplayPoint(double[] w) { return ViewToDisplay(WorldToViewPoint(w)); }
        internal double[] DisplayToWorldPoint(double[] d)
        {
            double[] v = DisplayToViewCoordinates(d);
            double[] m = vtkMatrix4x4.Inverse(GetActiveCamera().CompositeMatrix(GetTiledAspectRatio(), 0, 1));
            double[] r = new double[4];
            for (int i = 0; i < 4; i++) r[i] = m[4 * i] * v[0] + m[4 * i + 1] * v[1] + m[4 * i + 2] * v[2] + m[4 * i + 3];
            if (r[3] != 0) { r[0] /= r[3]; r[1] /= r[3]; r[2] /= r[3]; }
            return new[] { r[0], r[1], r[2] };
        }
        public bool Transparent() { return _layer > 0; }
        public static vtkRenderer SafeDownCast(vtkObjectBase o) { return o as vtkRenderer; }
    }

    public class vtkActorCollection : vtkPropCollection
    {
        public static new vtkActorCollection New() { return new vtkActorCollection(); }
        public void AddItem(vtkActor a) { base.AddItem(a); }
        public vtkActor GetNextActor() { return GetNextProp() as vtkActor; }
        public vtkActor GetLastActor() { return GetLastProp() as vtkActor; }
    }

    public class vtkLightCollection : vtkObject
    {
        private readonly List<vtkLight> _items = new List<vtkLight>();
        private int _traversal;
        public static new vtkLightCollection New() { return new vtkLightCollection(); }
        public void AddItem(vtkLight l) { _items.Add(l); }
        public int GetNumberOfItems() { return _items.Count; }
        public void InitTraversal() { _traversal = 0; }
        public vtkLight GetNextItem() { return _traversal < _items.Count ? _items[_traversal++] : null; }
    }

    // Windows
    public partial class vtkWindow
    {
        protected int[] _size = { 300, 300 };
        protected int[] _position = { 0, 0 };

        public virtual int[] GetSize() { return (int[])_size.Clone(); }
        public virtual void SetSize(int w, int h)
        {
            if (_size[0] == w && _size[1] == h) return;
            _size = new[] { Math.Max(1, w), Math.Max(1, h) };
            Modified();
        }
        public void SetSize(int[] s) { SetSize(s[0], s[1]); }
        public int[] GetPosition() { return (int[])_position.Clone(); }
        public void SetPosition(int x, int y) { _position = new[] { x, y }; }
        public void SetWindowName(string name) { }
        public void SetDPI(int dpi) { }
        public int GetDPI() { return 72; }
        public void SetOffScreenRendering(int v) { }
        public void OffScreenRenderingOn() { }
        public void OffScreenRenderingOff() { }
        public void SetMapped(int v) { }
        public void SetDisplayId(IntPtr id) { }
        public IntPtr GetGenericWindowId() { return IntPtr.Zero; }
        public IntPtr GetGenericDisplayId() { return IntPtr.Zero; }
        public void SetSwapBuffers(int v) { }
        public void SetErase(int v) { }
        public virtual void Render() { }
    }

    public partial class vtkRenderWindow
    {
        private readonly vtkRendererCollection _renderers = new vtkRendererCollection();
        private int _numberOfLayers = 1;
        private vtkRenderWindowInteractor _interactor;
        private int _multiSamples;
        private bool _pointSmoothing, _lineSmoothing, _polygonSmoothing;
        private bool _alphaBitPlanes;
        private int _cursor;
        private IntPtr _parentId;
        private int _magnification = 1;
        private bool _inRender;

        // The view that draws the window, attached by SetParentId
        internal Gl.RenderSurface Surface;
        internal Gl.SceneRenderer Scene = new Gl.SceneRenderer();

        public void AddRenderer(vtkRenderer renderer)
        {
            _renderers.AddItem(renderer);
            renderer.SetRenderWindow(this);
            Modified();
        }
        public void RemoveRenderer(vtkRenderer renderer) { _renderers.RemoveItem(renderer); renderer.SetRenderWindow(null); }
        public int HasRenderer(vtkRenderer renderer) { return _renderers.Items.Contains(renderer) ? 1 : 0; }
        public vtkRendererCollection GetRenderers() { return _renderers; }
        public void SetNumberOfLayers(int n) { _numberOfLayers = n; }
        public int GetNumberOfLayers() { return _numberOfLayers; }
        public void SetInteractor(vtkRenderWindowInteractor interactor)
        {
            _interactor = interactor;
            if (interactor != null && interactor.GetRenderWindow() != this) interactor.SetRenderWindow(this);
        }
        public vtkRenderWindowInteractor GetInteractor() { return _interactor; }
        public void SetMultiSamples(int n) { _multiSamples = n; }
        public int GetMultiSamples() { return _multiSamples; }
        public void PointSmoothingOn() { _pointSmoothing = true; }
        public void PointSmoothingOff() { _pointSmoothing = false; }
        public void SetPointSmoothing(int v) { _pointSmoothing = v != 0; }
        public void LineSmoothingOn() { _lineSmoothing = true; }
        public void LineSmoothingOff() { _lineSmoothing = false; }
        public void SetLineSmoothing(int v) { _lineSmoothing = v != 0; }
        public void PolygonSmoothingOn() { _polygonSmoothing = true; }
        public void PolygonSmoothingOff() { _polygonSmoothing = false; }
        public void SetPolygonSmoothing(int v) { _polygonSmoothing = v != 0; }
        internal bool PointSmoothing { get { return _pointSmoothing; } }
        public int GetPointSmoothing() { return _pointSmoothing ? 1 : 0; }
        public int GetLineSmoothing() { return _lineSmoothing ? 1 : 0; }
        public int GetPolygonSmoothing() { return _polygonSmoothing ? 1 : 0; }
        private int _swapBuffers = 1;
        public new void SetSwapBuffers(int v) { _swapBuffers = v; }
        public int GetSwapBuffers() { return _swapBuffers; }
        internal bool LineSmoothing { get { return _lineSmoothing; } }
        public void SetAlphaBitPlanes(int v) { _alphaBitPlanes = v != 0; }
        public void AlphaBitPlanesOn() { _alphaBitPlanes = true; }
        public void SetCurrentCursor(int cursor)
        {
            _cursor = cursor;
            Surface?.SetCursor(cursor);
        }
        public int GetCurrentCursor() { return _cursor; }
        public void HideCursor() { }
        public void ShowCursor() { }
        // The window draws into the control with the handle
        public void SetParentId(IntPtr id)
        {
            _parentId = id;
            System.Windows.Forms.Control control = System.Windows.Forms.Control.FromHandle(id);
            if (control != null) Surface = Gl.RenderSurface.Attach(control, this);
        }
        public void SetWindowId(IntPtr id) { SetParentId(id); }
        public IntPtr GetParentId() { return _parentId; }
        public int GetEventPending() { return 0; }
        public override void Render()
        {
            if (_inRender) return;
            _inRender = true;
            try
            {
                InvokeEvent(vtkCommandIds.StartEvent, IntPtr.Zero);
                // The mappers bring their inputs up to date before the frame is drawn
                foreach (vtkRenderer renderer in _renderers.Items)
                {
                    foreach (vtkProp prop in renderer.Props.ToArray())
                    {
                        if (!prop.Visible) continue;
                        if (prop is vtkActor a) a.GetMapper()?.UpdatePipeline();
                    }
                }
                InvokeEvent(vtkCommandIds.RenderEvent, IntPtr.Zero);
                Surface?.RequestFrame();
                InvokeEvent(vtkCommandIds.EndEvent, IntPtr.Zero);
            }
            finally
            {
                _inRender = false;
            }
        }
        internal void RequestRender() { Render(); }
        public void Frame() { }
        public void MakeCurrent() { }
        public void Finalize_() { }
        public void SetStereoRender(int v) { }
        public void SetMagnification(int m) { _magnification = Math.Max(1, m); }
        // The pixels of the window drawn offscreen; RGB or RGBA bytes, bottom row first
        internal byte[] ReadPixels(int magnification, bool alpha, out int width, out int height)
        {
            int[] size = GetSize();
            width = size[0] * magnification;
            height = size[1] * magnification;
            return Gl.OffscreenRenderer.Render(this, width, height, magnification, alpha);
        }
        public static vtkRenderWindow SafeDownCast(vtkObjectBase o) { return o as vtkRenderWindow; }
    }
}
