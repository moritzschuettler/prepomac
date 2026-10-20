// PrePoMax for macOS - the sources and filters of the VTK compatible library. Each algorithm computes its output from its
// inputs in Execute, which the pipeline calls when an input or a parameter changed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    // Collects the points and cells of an output; the point and cell data are copied or interpolated from the input
    internal class DataBuilder
    {
        public readonly List<double> Points = new List<double>();
        // Point sources: (input ids, weights); a single id with weight 1 is a copy
        public readonly List<(long[] ids, double[] weights)> PointSources = new List<(long[], double[])>();
        public readonly List<(int type, long[] ids)> Cells = new List<(int, long[])>();
        public readonly List<long> CellSources = new List<long>();
        private readonly Dictionary<long, long> _copiedPoints = new Dictionary<long, long>();
        private readonly Dictionary<(long, long, int), long> _edgePoints = new Dictionary<(long, long, int), long>();

        public long AddPoint(double x, double y, double z, long[] ids, double[] weights)
        {
            Points.Add(x); Points.Add(y); Points.Add(z);
            PointSources.Add((ids, weights));
            return PointSources.Count - 1;
        }
        public long AddPoint(double[] p) { return AddPoint(p[0], p[1], p[2], null, null); }
        // A copy of an input point; each input point is copied once
        public long CopyPoint(vtkDataSet input, long id)
        {
            if (_copiedPoints.TryGetValue(id, out long existing)) return existing;
            double[] p = input.GetPoint(id);
            long newId = AddPoint(p[0], p[1], p[2], new[] { id }, new[] { 1.0 });
            _copiedPoints[id] = newId;
            return newId;
        }
        // The point on the edge between two input points at the parameter t from a; shared by the cells of the edge
        public long EdgePoint(vtkDataSet input, long a, long b, double t, int contour)
        {
            if (t <= 0) return CopyPoint(input, a);
            if (t >= 1) return CopyPoint(input, b);
            var key = a < b ? (a, b, contour) : (b, a, contour);
            if (_edgePoints.TryGetValue(key, out long existing)) return existing;
            double[] pa = input.GetPoint(a), pb = input.GetPoint(b);
            long id = AddPoint(pa[0] + t * (pb[0] - pa[0]), pa[1] + t * (pb[1] - pa[1]), pa[2] + t * (pb[2] - pa[2]),
                               new[] { a, b }, new[] { 1 - t, t });
            _edgePoints[key] = id;
            return id;
        }
        public void AddCell(int type, long[] ids, long sourceCell)
        {
            Cells.Add((type, ids));
            CellSources.Add(sourceCell);
        }
        // Creates the output with the point and cell data of the input
        public void Fill(vtkPointSet output, vtkDataSet input, bool copyPointData = true, bool copyCellData = true)
        {
            vtkPoints points = new vtkPoints();
            if (input is vtkPointSet ps && ps.GetPoints() != null && ps.GetPoints().GetDataType() == 11) points.SetDataTypeToDouble();
            points.SetNumberOfPoints(PointSources.Count);
            double[] raw = points.Raw;
            for (int i = 0; i < Points.Count; i++) raw[i] = Points[i];
            output.SetPoints(points);
            vtkPointData pd = output.GetPointData();
            vtkCellData cd = output.GetCellData();
            pd.Initialize();
            cd.Initialize();
            if (input != null && copyPointData)
            {
                pd.CopyAllocate(input.GetPointData());
                for (int i = 0; i < PointSources.Count; i++)
                {
                    var s = PointSources[i];
                    if (s.ids == null) continue;
                    if (s.ids.Length == 1) pd.CopyData(input.GetPointData(), s.ids[0], i);
                    else pd.InterpolateTuple(input.GetPointData(), i, s.ids, s.weights);
                }
                // Points without a source get zero tuples
                foreach (vtkAbstractArray array in pd.Arrays)
                    if (array is vtkDataArray da && da.GetNumberOfTuples() < PointSources.Count) da.SetNumberOfTuples(PointSources.Count);
            }
            if (output is vtkPolyData poly)
            {
                foreach (var c in Cells) poly.InsertNextCell(c.type, c.ids);
            }
            else if (output is vtkUnstructuredGrid grid)
            {
                foreach (var c in Cells) grid.InsertNextCell(c.type, c.ids);
            }
            if (input != null && copyCellData)
            {
                cd.CopyAllocate(input.GetCellData());
                for (int i = 0; i < CellSources.Count; i++)
                    if (CellSources[i] >= 0) cd.CopyData(input.GetCellData(), CellSources[i], i);
                foreach (vtkAbstractArray array in cd.Arrays)
                    if (array is vtkDataArray da && da.GetNumberOfTuples() < CellSources.Count) da.SetNumberOfTuples(CellSources.Count);
            }
            output.Modified();
        }
    }

    internal static class FilterTools
    {
        // The input as the output: points, cells and data are shared
        public static void PassThrough(vtkDataSet input, vtkPolyData output)
        {
            if (input is vtkPolyData pd) output.DeepCopy(pd);
            else output.Initialize();
        }
        public static long EdgeKey(long a, long b) { return a < b ? (a << 32) + b : (b << 32) + a; }
        // The boundary faces of the 3D cells of a data set: the faces used by one cell, with the cell they belong to
        public static List<(int type, long[] ids, long cell)> BoundaryFaces(vtkDataSet input, bool keepQuadratic)
        {
            var faces = new Dictionary<string, (int type, long[] ids, long cell, int count)>();
            var order = new List<string>();
            long n = input.GetNumberOfCells();
            for (long c = 0; c < n; c++)
            {
                int type = input.GetCellType(c);
                if (CellType.Dimension(type) != 3) continue;
                long[] ids = input.GetCellPointIds(c);
                foreach (int[] face in CellType.Faces(type))
                {
                    int corners = CellType.IsQuadratic(type) ? face.Length / 2 : face.Length;
                    long[] faceIds = new long[face.Length];
                    for (int i = 0; i < face.Length; i++) faceIds[i] = ids[face[i]];
                    long[] key = new long[corners];
                    Array.Copy(faceIds, key, corners);
                    Array.Sort(key);
                    string k = string.Join(",", key);
                    if (faces.TryGetValue(k, out var existing)) faces[k] = (existing.type, existing.ids, existing.cell, existing.count + 1);
                    else
                    {
                        int faceType = CellType.FaceType(face.Length, CellType.IsQuadratic(type));
                        if (!keepQuadratic && CellType.IsQuadratic(type))
                        {
                            faceIds = InterleavedPolygon(faceType, faceIds);
                            faceType = CellType.Polygon;
                        }
                        faces[k] = (faceType, faceIds, c, 1);
                        order.Add(k);
                    }
                }
            }
            var result = new List<(int, long[], long)>();
            foreach (string k in order)
            {
                var f = faces[k];
                if (f.count == 1) result.Add((f.type, f.ids, f.cell));
            }
            return result;
        }
        // A quadratic face as a polygon through its corner and mid points
        public static long[] InterleavedPolygon(int type, long[] ids)
        {
            if (type == CellType.QuadraticTriangle && ids.Length == 6) return new[] { ids[0], ids[3], ids[1], ids[4], ids[2], ids[5] };
            if (type == CellType.QuadraticQuad && ids.Length == 8)
                return new[] { ids[0], ids[4], ids[1], ids[5], ids[2], ids[6], ids[3], ids[7] };
            return ids;
        }
        public static double[] PolygonNormal(vtkDataSet data, long[] ids)
        {
            // Newell's method
            double[] n = new double[3];
            for (int i = 0; i < ids.Length; i++)
            {
                double[] a = data.GetPoint(ids[i]), b = data.GetPoint(ids[(i + 1) % ids.Length]);
                n[0] += (a[1] - b[1]) * (a[2] + b[2]);
                n[1] += (a[2] - b[2]) * (a[0] + b[0]);
                n[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            vtkMath.Normalize(n);
            return n;
        }
        public static double PolygonArea(vtkDataSet data, long[] ids)
        {
            double[] n = new double[3];
            for (int i = 0; i < ids.Length; i++)
            {
                double[] a = data.GetPoint(ids[i]), b = data.GetPoint(ids[(i + 1) % ids.Length]);
                n[0] += (a[1] - b[1]) * (a[2] + b[2]);
                n[1] += (a[2] - b[2]) * (a[0] + b[0]);
                n[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return 0.5 * vtkMath.Norm(n);
        }
        // The cells of a poly data in id order with their types
        public static IEnumerable<(long id, int type, long[] ids)> Cells(vtkDataSet data)
        {
            long n = data.GetNumberOfCells();
            for (long c = 0; c < n; c++) yield return (c, data.GetCellType(c), data.GetCellPointIds(c));
        }
    }

    // Sources
    internal static class SourceTools
    {
        public static vtkPolyData Build(List<double[]> points, List<double[]> normals, List<long[]> polys, List<long[]> lines = null)
        {
            vtkPolyData pd = new vtkPolyData();
            vtkPoints pts = new vtkPoints();
            foreach (double[] p in points) pts.InsertNextPoint(p);
            pd.SetPoints(pts);
            if (lines != null && lines.Count > 0)
            {
                vtkCellArray ca = new vtkCellArray();
                foreach (long[] l in lines) ca.InsertNextCell(l);
                pd.SetLines(ca);
            }
            if (polys != null && polys.Count > 0)
            {
                vtkCellArray ca = new vtkCellArray();
                foreach (long[] p in polys) ca.InsertNextCell(p);
                pd.SetPolys(ca);
            }
            if (normals != null)
            {
                vtkFloatArray n = new vtkFloatArray();
                n.SetNumberOfComponents(3);
                n.SetName("Normals");
                foreach (double[] v in normals) n.InsertNextTuple3(v[0], v[1], v[2]);
                pd.GetPointData().SetNormals(n);
            }
            return pd;
        }
        public static void Transform(vtkPolyData pd, double[] m)
        {
            vtkPoints pts = pd.GetPoints();
            if (pts == null) return;
            for (long i = 0; i < pts.GetNumberOfPoints(); i++)
            {
                double[] p = pts.GetPoint(i);
                pts.SetPoint(i, vtkMatrix4x4.TransformPoint(m, p[0], p[1], p[2]));
            }
            vtkDataArray normals = pd.GetPointData().GetNormals();
            if (normals != null)
            {
                for (long i = 0; i < normals.GetNumberOfTuples(); i++)
                {
                    double[] v = normals.GetTuple3(i);
                    double[] r = vtkMatrix4x4.TransformVector(m, v[0], v[1], v[2]);
                    vtkMath.Normalize(r);
                    normals.SetTuple3(i, r[0], r[1], r[2]);
                }
            }
        }
        public static void CopyInto(vtkPolyData source, vtkPolyData output) { output.DeepCopy(source); }
    }

    public partial class vtkSphereSource
    {
        private double _radius = 0.5;
        private double[] _center = new double[3];
        private int _thetaResolution = 8, _phiResolution = 8;
        private double _startTheta, _endTheta = 360, _startPhi, _endPhi = 180;

        public void SetRadius(double r) { _radius = r; Modified(); }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; Modified(); }
        public void SetCenter(double[] c) { SetCenter(c[0], c[1], c[2]); }
        public void SetThetaResolution(int r) { _thetaResolution = Math.Max(3, r); Modified(); }
        public void SetPhiResolution(int r) { _phiResolution = Math.Max(3, r); Modified(); }
        public void SetStartTheta(double a) { _startTheta = a; Modified(); }
        public void SetEndTheta(double a) { _endTheta = a; Modified(); }
        public void SetStartPhi(double a) { _startPhi = a; Modified(); }
        public void SetEndPhi(double a) { _endPhi = a; Modified(); }
        public void SetLatLongTessellation(int v) { }
        protected override void Execute()
        {
            var points = new List<double[]>();
            var normals = new List<double[]>();
            var polys = new List<long[]>();
            double deltaPhi = (_endPhi - _startPhi) / (_phiResolution - 1) * Math.PI / 180;
            bool fullCircle = Math.Abs(_endTheta - _startTheta) >= 360 - 1e-9;
            int numTheta = fullCircle ? _thetaResolution : _thetaResolution + 1;
            double deltaTheta = (_endTheta - _startTheta) / _thetaResolution * Math.PI / 180;
            bool north = _startPhi <= 0, south = _endPhi >= 180;
            long northId = -1, southId = -1;
            if (north)
            {
                points.Add(new[] { _center[0], _center[1], _center[2] + _radius });
                normals.Add(new double[] { 0, 0, 1 });
                northId = 0;
            }
            if (south)
            {
                points.Add(new[] { _center[0], _center[1], _center[2] - _radius });
                normals.Add(new double[] { 0, 0, -1 });
                southId = points.Count - 1;
            }
            int phiStart = north ? 1 : 0, phiEnd = south ? _phiResolution - 1 : _phiResolution;
            int numPhi = phiEnd - phiStart;
            long baseId = points.Count;
            for (int i = 0; i < numTheta; i++)
            {
                double theta = _startTheta * Math.PI / 180 + i * deltaTheta;
                for (int j = phiStart; j < phiEnd; j++)
                {
                    double phi = _startPhi * Math.PI / 180 + j * deltaPhi;
                    double r = _radius * Math.Sin(phi);
                    double[] n = { r * Math.Cos(theta), r * Math.Sin(theta), _radius * Math.Cos(phi) };
                    points.Add(new[] { n[0] + _center[0], n[1] + _center[1], n[2] + _center[2] });
                    double l = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
                    normals.Add(new[] { n[0] / l, n[1] / l, n[2] / l });
                }
            }
            int thetaCells = fullCircle ? numTheta : numTheta - 1;
            for (int i = 0; i < thetaCells; i++)
            {
                long a = baseId + i * numPhi, b = baseId + ((i + 1) % numTheta) * numPhi;
                if (north) polys.Add(new[] { northId, a, b });
                for (int j = 0; j < numPhi - 1; j++)
                {
                    polys.Add(new[] { a + j, a + j + 1, b + j + 1 });
                    polys.Add(new[] { a + j, b + j + 1, b + j });
                }
                if (south) polys.Add(new[] { southId, b + numPhi - 1, a + numPhi - 1 });
            }
            GetOutput().DeepCopy(SourceTools.Build(points, normals, polys));
        }
    }

    public partial class vtkConeSource
    {
        private double _height = 1, _radius = 0.5;
        private int _resolution = 6;
        private double[] _center = new double[3];
        private double[] _direction = { 1, 0, 0 };
        private bool _capping = true;

        public void SetHeight(double h) { _height = h; Modified(); }
        public void SetRadius(double r) { _radius = r; Modified(); }
        public void SetResolution(int r) { _resolution = Math.Max(0, r); Modified(); }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; Modified(); }
        public void SetDirection(double x, double y, double z) { _direction = new[] { x, y, z }; Modified(); }
        public void SetAngle(double a) { _radius = _height * Math.Tan(a * Math.PI / 180); Modified(); }
        public void CappingOn() { _capping = true; Modified(); }
        public void CappingOff() { _capping = false; Modified(); }
        public void SetCapping(int v) { _capping = v != 0; Modified(); }
        protected override void Execute()
        {
            var points = new List<double[]>();
            var polys = new List<long[]>();
            var lines = new List<long[]>();
            points.Add(new[] { _height / 2, 0.0, 0.0 });
            if (_resolution == 0) { points.Add(new[] { -_height / 2, 0.0, 0.0 }); lines.Add(new long[] { 0, 1 }); }
            else if (_resolution < 3)
            {
                points.Add(new[] { -_height / 2, _radius, 0.0 });
                points.Add(new[] { -_height / 2, -_radius, 0.0 });
                polys.Add(new long[] { 0, 1, 2 });
            }
            else
            {
                double angle = 2 * Math.PI / _resolution;
                for (int i = 0; i < _resolution; i++)
                    points.Add(new[] { -_height / 2, _radius * Math.Cos(i * angle), _radius * Math.Sin(i * angle) });
                for (int i = 0; i < _resolution; i++) polys.Add(new long[] { 0, i + 1, (i + 1) % _resolution + 1 });
                if (_capping)
                {
                    long[] cap = new long[_resolution];
                    for (int i = 0; i < _resolution; i++) cap[i] = _resolution - i;
                    polys.Add(cap);
                }
            }
            vtkPolyData pd = SourceTools.Build(points, null, polys, lines);
            // The direction rotates the x axis, then the center translates
            double[] m = vtkMatrix4x4.Translation(_center[0], _center[1], _center[2]);
            double[] d = (double[])_direction.Clone();
            double dl = vtkMath.Normalize(d);
            if (dl > 0 && !(d[0] > 0.999999))
            {
                double[] axis = { (d[0] + 1) / 2, d[1] / 2, d[2] / 2 };
                if (vtkMath.Norm(axis) < 1e-9) axis = new double[] { 0, 0, 1 };
                m = vtkMatrix4x4.Multiply(m, vtkMatrix4x4.Rotation(180, axis[0], axis[1], axis[2]));
            }
            SourceTools.Transform(pd, m);
            GetOutput().DeepCopy(pd);
        }
    }

    public partial class vtkCylinderSource
    {
        private double _height = 1, _radius = 0.5;
        private int _resolution = 6;
        private double[] _center = new double[3];
        private bool _capping = true;

        public void SetHeight(double h) { _height = h; Modified(); }
        public void SetRadius(double r) { _radius = r; Modified(); }
        public void SetResolution(int r) { _resolution = Math.Max(3, r); Modified(); }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; Modified(); }
        public void CappingOn() { _capping = true; Modified(); }
        public void CappingOff() { _capping = false; Modified(); }
        public void SetCapping(int v) { _capping = v != 0; Modified(); }
        protected override void Execute()
        {
            var points = new List<double[]>();
            var normals = new List<double[]>();
            var polys = new List<long[]>();
            double angle = 2 * Math.PI / _resolution;
            // Sides: the axis is y
            for (int i = 0; i < _resolution; i++)
            {
                double x = Math.Cos(i * angle), z = -Math.Sin(i * angle);
                points.Add(new[] { _radius * x + _center[0], _height / 2 + _center[1], _radius * z + _center[2] });
                normals.Add(new[] { x, 0, z });
                points.Add(new[] { _radius * x + _center[0], -_height / 2 + _center[1], _radius * z + _center[2] });
                normals.Add(new[] { x, 0, z });
            }
            for (int i = 0; i < _resolution; i++)
            {
                long a = 2 * i, b = 2 * ((i + 1) % _resolution);
                polys.Add(new[] { a, a + 1, b + 1, b });
            }
            if (_capping)
            {
                long top = points.Count;
                for (int i = 0; i < _resolution; i++)
                {
                    double x = Math.Cos(i * angle), z = -Math.Sin(i * angle);
                    points.Add(new[] { _radius * x + _center[0], _height / 2 + _center[1], _radius * z + _center[2] });
                    normals.Add(new double[] { 0, 1, 0 });
                }
                long bottom = points.Count;
                for (int i = 0; i < _resolution; i++)
                {
                    double x = Math.Cos(i * angle), z = -Math.Sin(i * angle);
                    points.Add(new[] { _radius * x + _center[0], -_height / 2 + _center[1], _radius * z + _center[2] });
                    normals.Add(new double[] { 0, -1, 0 });
                }
                long[] capTop = new long[_resolution], capBottom = new long[_resolution];
                for (int i = 0; i < _resolution; i++)
                {
                    capTop[i] = top + _resolution - 1 - i;
                    capBottom[i] = bottom + i;
                }
                polys.Add(capTop);
                polys.Add(capBottom);
            }
            GetOutput().DeepCopy(SourceTools.Build(points, normals, polys));
        }
    }

    public partial class vtkArrowSource
    {
        private double _tipLength = 0.35, _tipRadius = 0.1, _shaftRadius = 0.03;
        private int _tipResolution = 6, _shaftResolution = 6;

        public void SetTipLength(double v) { _tipLength = v; Modified(); }
        public void SetTipRadius(double v) { _tipRadius = v; Modified(); }
        public void SetShaftRadius(double v) { _shaftRadius = v; Modified(); }
        public void SetTipResolution(int v) { _tipResolution = v; Modified(); }
        public void SetShaftResolution(int v) { _shaftResolution = v; Modified(); }
        public void SetInvert(bool v) { }
        protected override void Execute()
        {
            vtkCylinderSource cylinder = new vtkCylinderSource();
            cylinder.SetResolution(_shaftResolution);
            cylinder.SetRadius(_shaftRadius);
            cylinder.SetHeight(1 - _tipLength);
            cylinder.SetCenter(0, (1 - _tipLength) * 0.5, 0);
            cylinder.Update();
            vtkPolyData shaft = new vtkPolyData();
            shaft.DeepCopy(cylinder.GetOutput());
            SourceTools.Transform(shaft, vtkMatrix4x4.Rotation(-90, 0, 0, 1));
            vtkConeSource cone = new vtkConeSource();
            cone.SetResolution(_tipResolution);
            cone.SetHeight(_tipLength);
            cone.SetRadius(_tipRadius);
            cone.SetCenter(1 - _tipLength * 0.5, 0, 0);
            cone.Update();
            vtkAppendPolyData append = new vtkAppendPolyData();
            append.AddInput(cone.GetOutput());
            append.AddInput(shaft);
            append.Update();
            GetOutput().DeepCopy(append.GetOutput());
        }
    }

    public partial class vtkCubeSource
    {
        private double _xLength = 1, _yLength = 1, _zLength = 1;
        private double[] _center = new double[3];

        public void SetXLength(double v) { _xLength = v; Modified(); }
        public void SetYLength(double v) { _yLength = v; Modified(); }
        public void SetZLength(double v) { _zLength = v; Modified(); }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; Modified(); }
        public void SetBounds(double x0, double x1, double y0, double y1, double z0, double z1)
        {
            _xLength = x1 - x0; _yLength = y1 - y0; _zLength = z1 - z0;
            _center = new[] { (x0 + x1) / 2, (y0 + y1) / 2, (z0 + z1) / 2 };
            Modified();
        }
        protected override void Execute()
        {
            var points = new List<double[]>();
            var normals = new List<double[]>();
            var polys = new List<long[]>();
            double[] h = { _xLength / 2, _yLength / 2, _zLength / 2 };
            // Two faces per axis, each with four points of its own for the face normals
            for (int axis = 0; axis < 3; axis++)
            {
                int u = (axis + 1) % 3, v = (axis + 2) % 3;
                for (int side = -1; side <= 1; side += 2)
                {
                    long start = points.Count;
                    double[][] corners = { new double[] { -1, -1 }, new double[] { 1, -1 }, new double[] { 1, 1 }, new double[] { -1, 1 } };
                    foreach (double[] c in corners)
                    {
                        double[] p = new double[3];
                        p[axis] = side * h[axis];
                        p[u] = c[0] * h[u];
                        p[v] = c[1] * h[v];
                        points.Add(new[] { p[0] + _center[0], p[1] + _center[1], p[2] + _center[2] });
                        double[] n = new double[3];
                        n[axis] = side;
                        normals.Add(n);
                    }
                    polys.Add(side > 0 ? new[] { start, start + 1, start + 2, start + 3 } : new[] { start, start + 3, start + 2, start + 1 });
                }
            }
            GetOutput().DeepCopy(SourceTools.Build(points, normals, polys));
        }
    }

    public partial class vtkDiskSource
    {
        private double _innerRadius = 0.25, _outerRadius = 0.5;
        private int _radialResolution = 1, _circumferentialResolution = 6;

        public void SetInnerRadius(double r) { _innerRadius = r; Modified(); }
        public void SetOuterRadius(double r) { _outerRadius = r; Modified(); }
        public void SetRadialResolution(int r) { _radialResolution = Math.Max(1, r); Modified(); }
        public void SetCircumferentialResolution(int r) { _circumferentialResolution = Math.Max(3, r); Modified(); }
        protected override void Execute()
        {
            var points = new List<double[]>();
            var polys = new List<long[]>();
            double theta = 2 * Math.PI / _circumferentialResolution;
            double dr = (_outerRadius - _innerRadius) / _radialResolution;
            for (int i = 0; i < _circumferentialResolution; i++)
                for (int j = 0; j <= _radialResolution; j++)
                {
                    double r = _innerRadius + j * dr;
                    points.Add(new[] { r * Math.Cos(i * theta), r * Math.Sin(i * theta), 0.0 });
                }
            int n = _radialResolution + 1;
            for (int i = 0; i < _circumferentialResolution; i++)
                for (int j = 0; j < _radialResolution; j++)
                {
                    long a = i * n + j, b = ((i + 1) % _circumferentialResolution) * n + j;
                    polys.Add(new[] { a, a + 1, b + 1, b });
                }
            GetOutput().DeepCopy(SourceTools.Build(points, null, polys));
        }
    }

    public partial class vtkLineSource
    {
        private double[] _p1 = { -0.5, 0, 0 }, _p2 = { 0.5, 0, 0 };
        private int _resolution = 1;

        public void SetPoint1(double x, double y, double z) { _p1 = new[] { x, y, z }; Modified(); }
        public void SetPoint1(double[] p) { SetPoint1(p[0], p[1], p[2]); }
        public void SetPoint2(double x, double y, double z) { _p2 = new[] { x, y, z }; Modified(); }
        public void SetPoint2(double[] p) { SetPoint2(p[0], p[1], p[2]); }
        public void SetResolution(int r) { _resolution = Math.Max(1, r); Modified(); }
        protected override void Execute()
        {
            var points = new List<double[]>();
            long[] line = new long[_resolution + 1];
            for (int i = 0; i <= _resolution; i++)
            {
                double t = (double)i / _resolution;
                points.Add(new[] { _p1[0] + t * (_p2[0] - _p1[0]), _p1[1] + t * (_p2[1] - _p1[1]), _p1[2] + t * (_p2[2] - _p1[2]) });
                line[i] = i;
            }
            GetOutput().DeepCopy(SourceTools.Build(points, null, null, new List<long[]> { line }));
        }
    }

    public partial class vtkRegularPolygonSource
    {
        private int _sides = 6;
        private double[] _center = new double[3], _normal = { 0, 0, 1 };
        private double _radius = 0.5;
        private bool _polygon = true, _polyline = true;

        public void SetNumberOfSides(int n) { _sides = Math.Max(3, n); Modified(); }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; Modified(); }
        public void SetNormal(double x, double y, double z) { _normal = new[] { x, y, z }; Modified(); }
        public void SetRadius(double r) { _radius = r; Modified(); }
        public void GeneratePolygonOn() { _polygon = true; }
        public void GeneratePolygonOff() { _polygon = false; }
        public void GeneratePolylineOn() { _polyline = true; }
        public void GeneratePolylineOff() { _polyline = false; }
        protected override void Execute()
        {
            double[] n = (double[])_normal.Clone();
            vtkMath.Normalize(n);
            double[] px = Math.Abs(n[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double[] ax = vtkMath.Cross(n, px);
            vtkMath.Normalize(ax);
            double[] ay = vtkMath.Cross(n, ax);
            var points = new List<double[]>();
            long[] cell = new long[_sides];
            for (int i = 0; i < _sides; i++)
            {
                double t = 2 * Math.PI * i / _sides;
                points.Add(new[] { _center[0] + _radius * (Math.Cos(t) * ax[0] + Math.Sin(t) * ay[0]),
                                   _center[1] + _radius * (Math.Cos(t) * ax[1] + Math.Sin(t) * ay[1]),
                                   _center[2] + _radius * (Math.Cos(t) * ax[2] + Math.Sin(t) * ay[2]) });
                cell[i] = i;
            }
            var lines = new List<long[]>();
            if (_polyline) lines.Add(cell.Concat(new long[] { 0 }).ToArray());
            GetOutput().DeepCopy(SourceTools.Build(points, null, _polygon ? new List<long[]> { cell } : null, lines));
        }
    }

    // Filters
    public partial class vtkAppendPolyData
    {
        public void RemoveInput(vtkDataObject input) { InputPort(0).Remove(input); Modified(); }
        public void SetParallelStreaming(int v) { }
        protected override void Execute()
        {
            List<vtkPolyData> inputs = InputDataObjects(0).OfType<vtkPolyData>().ToList();
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (inputs.Count == 0) return;
            vtkPoints points = new vtkPoints();
            if (inputs.Any(i => i.GetPoints() != null && i.GetPoints().GetDataType() == 11)) points.SetDataTypeToDouble();
            long total = inputs.Sum(i => i.GetNumberOfPoints());
            points.SetNumberOfPoints(total);
            long[] offsets = new long[inputs.Count];
            long offset = 0;
            for (int k = 0; k < inputs.Count; k++)
            {
                offsets[k] = offset;
                long n = inputs[k].GetNumberOfPoints();
                if (n > 0) Array.Copy(inputs[k].GetPoints().Raw, 0, points.Raw, 3 * offset, 3 * n);
                offset += n;
            }
            output.SetPoints(points);
            // The arrays all inputs have, by name or attribute
            AppendAttributes(output.GetPointData(), inputs.Select(i => (vtkDataSetAttributes)i.GetPointData()).ToList(),
                             inputs.Select(i => i.GetNumberOfPoints()).ToList(), null);
            // Cells: the vertices of all inputs, then the lines, polygons and strips
            List<(int input, long cell)> cellSources = new List<(int, long)>();
            for (int kind = 0; kind < 4; kind++)
            {
                for (int k = 0; k < inputs.Count; k++)
                {
                    vtkCellArray ca = inputs[k].Array(kind);
                    long[] ids = inputs[k].CellIdsOfKind(kind);
                    for (int c = 0; c < ca.Count; c++)
                    {
                        long[] pts = ca.GetCell(c);
                        for (int j = 0; j < pts.Length; j++) pts[j] += offsets[k];
                        output.Array(kind).InsertNextCell(pts);
                        cellSources.Add((k, ids[c]));
                    }
                }
            }
            AppendAttributes(output.GetCellData(), inputs.Select(i => (vtkDataSetAttributes)i.GetCellData()).ToList(), null,
                             cellSources);
            output.Modified();
        }
        private static void AppendAttributes(vtkDataSetAttributes output, List<vtkDataSetAttributes> inputs, List<long> counts,
                                             List<(int input, long cell)> order)
        {
            output.Initialize();
            if (inputs.Count == 0) return;
            vtkDataSetAttributes first = inputs[0];
            foreach (vtkAbstractArray array in first.Arrays)
            {
                if (!(array is vtkDataArray da)) continue;
                string name = da.GetName();
                List<vtkDataArray> sources = new List<vtkDataArray>();
                foreach (vtkDataSetAttributes input in inputs)
                {
                    vtkDataArray match = null;
                    if (array == first.GetScalars()) match = input.GetScalars();
                    else if (array == first.GetNormals()) match = input.GetNormals();
                    else if (array == first.GetGlobalIds()) match = input.GetGlobalIds();
                    else if (name != null) match = input.GetArray(name);
                    if (match == null || match.GetNumberOfComponents() != da.GetNumberOfComponents()) { sources = null; break; }
                    sources.Add(match);
                }
                if (sources == null) continue;
                vtkDataArray result = da.CreateEmptyCopy();
                if (order == null)
                {
                    foreach (vtkDataArray s in sources)
                        for (long t = 0; t < s.GetNumberOfTuples(); t++) result.AppendTupleFrom(s, t);
                }
                else
                {
                    foreach (var o in order) result.AppendTupleFrom(sources[o.input], o.cell);
                }
                if (array == first.GetScalars()) output.SetScalars(result);
                else if (array == first.GetNormals()) output.SetNormals(result);
                else if (array == first.GetGlobalIds()) output.SetGlobalIds(result);
                else if (array == first.GetVectors()) output.SetVectors(result);
                else output.AddArray(result);
            }
        }
    }

    public partial class vtkTriangleFilter
    {
        private bool _passLines = true, _passVerts = true;
        public void PassLinesOn() { _passLines = true; }
        public void PassLinesOff() { _passLines = false; }
        public void PassVertsOn() { _passVerts = true; }
        public void PassVertsOff() { _passVerts = false; }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            DataBuilder b = new DataBuilder();
            long n = input.GetNumberOfPoints();
            for (long i = 0; i < n; i++) b.CopyPoint(input, i);
            foreach (var c in FilterTools.Cells(input))
            {
                int dim = CellType.Dimension(c.type);
                if (dim == 0)
                {
                    if (_passVerts) foreach (long p in c.ids) b.AddCell(CellType.Vertex, new[] { p }, c.id);
                }
                else if (dim == 1)
                {
                    if (_passLines) for (int i = 0; i + 1 < c.ids.Length; i++) b.AddCell(CellType.Line, new[] { c.ids[i], c.ids[i + 1] }, c.id);
                }
                else
                {
                    foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null))
                        b.AddCell(CellType.Triangle, new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] }, c.id);
                }
            }
            b.Fill(output, input);
        }
    }

    public partial class vtkPolyDataNormals
    {
        private double _featureAngle = 30;
        private bool _splitting = true, _consistency = true, _autoOrient, _computePointNormals = true, _computeCellNormals;
        private bool _flipNormals, _nonManifoldTraversal = true;

        public void SetFeatureAngle(double a) { _featureAngle = a; Modified(); }
        public void SplittingOn() { _splitting = true; Modified(); }
        public void SplittingOff() { _splitting = false; Modified(); }
        public void SetSplitting(int v) { _splitting = v != 0; Modified(); }
        public void ConsistencyOn() { _consistency = true; Modified(); }
        public void ConsistencyOff() { _consistency = false; Modified(); }
        public void AutoOrientNormalsOn() { _autoOrient = true; Modified(); }
        public void AutoOrientNormalsOff() { _autoOrient = false; Modified(); }
        public void ComputePointNormalsOn() { _computePointNormals = true; Modified(); }
        public void ComputePointNormalsOff() { _computePointNormals = false; Modified(); }
        public void ComputeCellNormalsOn() { _computeCellNormals = true; Modified(); }
        public void ComputeCellNormalsOff() { _computeCellNormals = false; Modified(); }
        public void FlipNormalsOn() { _flipNormals = true; Modified(); }
        public void FlipNormalsOff() { _flipNormals = false; Modified(); }
        public void NonManifoldTraversalOn() { _nonManifoldTraversal = true; }
        public void NonManifoldTraversalOff() { _nonManifoldTraversal = false; }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            // Polygons in id order; the other cells pass
            List<long[]> polys = new List<long[]>();
            List<long> polyCells = new List<long>();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) != 2) continue;
                if (c.type == CellType.TriangleStrip)
                {
                    foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null))
                    {
                        polys.Add(new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] });
                        polyCells.Add(c.id);
                    }
                }
                else
                {
                    polys.Add((long[])c.ids.Clone());
                    polyCells.Add(c.id);
                }
            }
            if (_consistency) MakeConsistent(polys, input);
            if (_autoOrient) AutoOrient(polys, input);
            if (_flipNormals) foreach (long[] p in polys) Array.Reverse(p);
            // Polygon normals
            double[][] polyNormals = new double[polys.Count][];
            for (int i = 0; i < polys.Count; i++) polyNormals[i] = FilterTools.PolygonNormal(input, polys[i]);
            // Points shared by the polygons; with splitting a point on a sharp edge is duplicated for each smooth region
            long numPoints = input.GetNumberOfPoints();
            List<int>[] pointPolys = new List<int>[numPoints];
            for (int i = 0; i < polys.Count; i++)
                foreach (long p in polys[i]) (pointPolys[p] ?? (pointPolys[p] = new List<int>())).Add(i);
            DataBuilder b = new DataBuilder();
            for (long i = 0; i < numPoints; i++) b.CopyPoint(input, i);
            List<double[]> normals = new List<double[]>();
            for (long i = 0; i < numPoints; i++) normals.Add(new double[3]);
            long[][] newPolys = polys.Select(p => (long[])p.Clone()).ToArray();
            double cosFeature = Math.Cos(_featureAngle * Math.PI / 180);
            for (long p = 0; p < numPoints; p++)
            {
                List<int> around = pointPolys[p];
                if (around == null) continue;
                if (!_splitting || around.Count == 1)
                {
                    double[] n = new double[3];
                    foreach (int k in around) for (int c = 0; c < 3; c++) n[c] += polyNormals[k][c];
                    vtkMath.Normalize(n);
                    normals[(int)p] = n;
                    continue;
                }
                // Groups of polygons connected across smooth edges at the point
                int[] group = Enumerable.Repeat(-1, around.Count).ToArray();
                int groups = 0;
                for (int a = 0; a < around.Count; a++)
                {
                    if (group[a] >= 0) continue;
                    group[a] = groups;
                    Stack<int> stack = new Stack<int>();
                    stack.Push(a);
                    while (stack.Count > 0)
                    {
                        int x = stack.Pop();
                        for (int y = 0; y < around.Count; y++)
                        {
                            if (group[y] >= 0) continue;
                            if (!ShareEdgeAt(polys[around[x]], polys[around[y]], p)) continue;
                            if (vtkMath.Dot(polyNormals[around[x]], polyNormals[around[y]]) < cosFeature) continue;
                            group[y] = groups;
                            stack.Push(y);
                        }
                    }
                    groups++;
                }
                for (int g = 0; g < groups; g++)
                {
                    double[] n = new double[3];
                    for (int a = 0; a < around.Count; a++)
                        if (group[a] == g) for (int c = 0; c < 3; c++) n[c] += polyNormals[around[a]][c];
                    vtkMath.Normalize(n);
                    long id = p;
                    if (g > 0)
                    {
                        double[] pt = input.GetPoint(p);
                        id = b.AddPoint(pt[0], pt[1], pt[2], new[] { p }, new[] { 1.0 });
                        normals.Add(n);
                        for (int a = 0; a < around.Count; a++)
                        {
                            if (group[a] != g) continue;
                            long[] poly = newPolys[around[a]];
                            for (int j = 0; j < poly.Length; j++) if (polys[around[a]][j] == p) poly[j] = id;
                        }
                    }
                    else normals[(int)p] = n;
                }
            }
            // The cells: vertices and lines first as in the input, then the polygons
            foreach (var c in FilterTools.Cells(input))
                if (CellType.Dimension(c.type) < 2) b.AddCell(c.type, c.ids, c.id);
            for (int i = 0; i < newPolys.Length; i++)
            {
                long[] poly = newPolys[i];
                int type = poly.Length == 3 ? CellType.Triangle : poly.Length == 4 ? CellType.Quad : CellType.Polygon;
                b.AddCell(type, poly, polyCells[i]);
            }
            b.Fill(output, input);
            if (_computePointNormals)
            {
                vtkFloatArray n = new vtkFloatArray();
                n.SetNumberOfComponents(3);
                n.SetName("Normals");
                foreach (double[] v in normals) n.InsertNextTuple3(v[0], v[1], v[2]);
                output.GetPointData().SetNormals(n);
            }
            if (_computeCellNormals)
            {
                vtkFloatArray n = new vtkFloatArray();
                n.SetNumberOfComponents(3);
                n.SetName("Normals");
                long numCells = output.GetNumberOfCells();
                int k = 0;
                for (long c = 0; c < numCells; c++)
                {
                    if (CellType.Dimension(output.GetCellType(c)) == 2) { double[] v = polyNormals[k++]; n.InsertNextTuple3(v[0], v[1], v[2]); }
                    else n.InsertNextTuple3(0, 0, 1);
                }
                output.GetCellData().SetNormals(n);
            }
        }
        private static bool ShareEdgeAt(long[] a, long[] b, long p)
        {
            int ia = Array.IndexOf(a, p);
            if (ia < 0 || Array.IndexOf(b, p) < 0) return false;
            long prev = a[(ia + a.Length - 1) % a.Length], next = a[(ia + 1) % a.Length];
            return Array.IndexOf(b, prev) >= 0 || Array.IndexOf(b, next) >= 0;
        }
        // Orders the polygons of each connected region like its first polygon
        private static void MakeConsistent(List<long[]> polys, vtkPolyData input)
        {
            var edgePolys = new Dictionary<long, List<int>>();
            for (int i = 0; i < polys.Count; i++)
            {
                long[] p = polys[i];
                for (int j = 0; j < p.Length; j++)
                {
                    long key = FilterTools.EdgeKey(p[j], p[(j + 1) % p.Length]);
                    if (!edgePolys.TryGetValue(key, out var list)) edgePolys[key] = list = new List<int>();
                    list.Add(i);
                }
            }
            bool[] visited = new bool[polys.Count];
            for (int start = 0; start < polys.Count; start++)
            {
                if (visited[start]) continue;
                visited[start] = true;
                Queue<int> queue = new Queue<int>();
                queue.Enqueue(start);
                while (queue.Count > 0)
                {
                    int i = queue.Dequeue();
                    long[] p = polys[i];
                    for (int j = 0; j < p.Length; j++)
                    {
                        long a = p[j], b = p[(j + 1) % p.Length];
                        foreach (int k in edgePolys[FilterTools.EdgeKey(a, b)])
                        {
                            if (visited[k]) continue;
                            long[] q = polys[k];
                            // A consistent neighbour runs the shared edge the other way
                            int ia = Array.IndexOf(q, a);
                            if (ia >= 0 && q[(ia + 1) % q.Length] == b) Array.Reverse(q);
                            visited[k] = true;
                            queue.Enqueue(k);
                        }
                    }
                }
            }
        }
        // Turns each connected region outwards: the polygon at the extreme point faces away from the inside
        private static void AutoOrient(List<long[]> polys, vtkPolyData input)
        {
            if (polys.Count == 0) return;
            // Regions
            var pointPolys = new Dictionary<long, List<int>>();
            for (int i = 0; i < polys.Count; i++)
                foreach (long p in polys[i]) { if (!pointPolys.TryGetValue(p, out var l)) pointPolys[p] = l = new List<int>(); l.Add(i); }
            int[] region = Enumerable.Repeat(-1, polys.Count).ToArray();
            int regions = 0;
            for (int s = 0; s < polys.Count; s++)
            {
                if (region[s] >= 0) continue;
                Stack<int> stack = new Stack<int>();
                stack.Push(s);
                region[s] = regions;
                while (stack.Count > 0)
                {
                    int i = stack.Pop();
                    foreach (long p in polys[i])
                        foreach (int k in pointPolys[p])
                            if (region[k] < 0) { region[k] = regions; stack.Push(k); }
                }
                regions++;
            }
            for (int r = 0; r < regions; r++)
            {
                // The polygon touching the point of the largest x
                double maxX = -double.MaxValue;
                long maxPoint = -1;
                for (int i = 0; i < polys.Count; i++)
                {
                    if (region[i] != r) continue;
                    foreach (long p in polys[i])
                    {
                        double x = input.GetPoint(p)[0];
                        if (x > maxX) { maxX = x; maxPoint = p; }
                    }
                }
                double bestNx = 0;
                foreach (int k in pointPolys[maxPoint])
                {
                    if (region[k] != r) continue;
                    double nx = FilterTools.PolygonNormal(input, polys[k])[0];
                    if (Math.Abs(nx) > Math.Abs(bestNx)) bestNx = nx;
                }
                if (bestNx < 0)
                    for (int i = 0; i < polys.Count; i++) if (region[i] == r) Array.Reverse(polys[i]);
            }
        }
    }

    public partial class vtkExtractEdges
    {
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            DataBuilder b = new DataBuilder();
            long n = input.GetNumberOfPoints();
            for (long i = 0; i < n; i++) b.CopyPoint(input, i);
            HashSet<long> seen = new HashSet<long>();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) == 0) continue;
                foreach (int[] e in CellType.Edges(c.type, c.ids.Length))
                {
                    long a = c.ids[e[0]], z = c.ids[e[1]];
                    if (e.Length == 3)
                    {
                        long m = c.ids[e[2]];
                        if (seen.Add(FilterTools.EdgeKey(a, m))) b.AddCell(CellType.Line, new[] { a, m }, -1);
                        if (seen.Add(FilterTools.EdgeKey(m, z))) b.AddCell(CellType.Line, new[] { m, z }, -1);
                    }
                    else if (seen.Add(FilterTools.EdgeKey(a, z))) b.AddCell(CellType.Line, new[] { a, z }, -1);
                }
            }
            b.Fill(output, input, true, false);
        }
    }

    // Cuts the cells with the implicit function at the contour values
    public partial class vtkCutter
    {
        private vtkImplicitFunction _cutFunction;
        private double[] _values = { 0 };
        private bool _generateCutScalars;

        public void SetCutFunction(vtkImplicitFunction f) { _cutFunction = f; Modified(); }
        public vtkImplicitFunction GetCutFunction() { return _cutFunction; }
        public void SetValue(int i, double v)
        {
            if (i >= _values.Length) Array.Resize(ref _values, i + 1);
            _values[i] = v;
            Modified();
        }
        public double GetValue(int i) { return _values[i]; }
        public void SetNumberOfContours(int n) { Array.Resize(ref _values, Math.Max(0, n)); Modified(); }
        public int GetNumberOfContours() { return _values.Length; }
        public void GenerateCutScalarsOn() { _generateCutScalars = true; Modified(); }
        public void GenerateCutScalarsOff() { _generateCutScalars = false; Modified(); }
        public void GenerateValues(int n, double min, double max)
        {
            _values = new double[n];
            for (int i = 0; i < n; i++) _values[i] = n > 1 ? min + i * (max - min) / (n - 1) : min;
            Modified();
        }
        protected override ulong ExtraModificationTime() { return _cutFunction != null ? _cutFunction.MTime : 0; }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || _cutFunction == null) return;
            long numPoints = input.GetNumberOfPoints();
            double[] f = new double[numPoints];
            for (long i = 0; i < numPoints; i++) f[i] = _cutFunction.EvaluateFunction(input.GetPoint(i));
            DataBuilder b = new DataBuilder();
            for (int v = 0; v < _values.Length; v++)
            {
                double value = _values[v];
                foreach (var c in FilterTools.Cells(input))
                {
                    int dim = CellType.Dimension(c.type);
                    if (dim == 3) CutVolumeCell(input, c.type, c.ids, c.id, f, value, v, b);
                    else if (dim == 2) CutSurfaceCell(input, c.type, c.ids, c.id, f, value, v, b);
                    else if (dim == 1) CutLineCell(input, c.type, c.ids, c.id, f, value, v, b);
                }
            }
            b.Fill(output, input);
            if (_generateCutScalars)
            {
                vtkFloatArray s = new vtkFloatArray();
                s.SetName("cutFunction");
                for (long i = 0; i < output.GetNumberOfPoints(); i++) s.InsertNextValue((float)_cutFunction.EvaluateFunction(output.GetPoint(i)));
                output.GetPointData().SetScalars(s);
            }
        }
        private static void CutLineCell(vtkDataSet input, int type, long[] ids, long cell, double[] f, double value, int v,
                                        DataBuilder b)
        {
            long[] chain = type == CellType.QuadraticEdge && ids.Length == 3 ? new[] { ids[0], ids[2], ids[1] } : ids;
            for (int i = 0; i + 1 < chain.Length; i++)
            {
                double fa = f[chain[i]] - value, fb = f[chain[i + 1]] - value;
                if ((fa < 0) == (fb < 0) && fa != 0 && fb != 0) continue;
                if (fa == fb) continue;
                double t = fa / (fa - fb);
                b.AddCell(CellType.Vertex, new[] { b.EdgePoint(input, chain[i], chain[i + 1], t, v) }, cell);
            }
        }
        // A face is cut along a line between the crossings of its edges
        private static void CutSurfaceCell(vtkDataSet input, int type, long[] ids, long cell, double[] f, double value, int v,
                                           DataBuilder b)
        {
            foreach (int[] t in CellType.Triangulate(type, ids.Length, null))
            {
                long[] tri = { ids[t[0]], ids[t[1]], ids[t[2]] };
                List<long> crossing = new List<long>();
                for (int i = 0; i < 3; i++)
                {
                    long a = tri[i], z = tri[(i + 1) % 3];
                    double fa = f[a] - value, fz = f[z] - value;
                    if (fa == 0 && fz == 0) continue;
                    if (fa == 0) { AddUnique(crossing, b.CopyPoint(input, a)); continue; }
                    if ((fa < 0) != (fz < 0) && fz != 0) AddUnique(crossing, b.EdgePoint(input, a, z, fa / (fa - fz), v));
                }
                if (crossing.Count == 2) b.AddCell(CellType.Line, crossing.ToArray(), cell);
            }
        }
        private static void AddUnique(List<long> list, long id) { if (!list.Contains(id)) list.Add(id); }
        // A volume cell is cut in a polygon through the crossings of its edges
        private static void CutVolumeCell(vtkDataSet input, int type, long[] ids, long cell, double[] f, double value, int v,
                                          DataBuilder b)
        {
            int[][] edges = CellType.Edges(type, ids.Length);
            List<long> crossing = new List<long>();
            foreach (int[] e in edges)
            {
                long a = ids[e[0]], z = ids[e[1]];
                double fa = f[a] - value, fz = f[z] - value;
                if (fa == 0) { AddUnique(crossing, b.CopyPoint(input, a)); }
                if (fz == 0) { AddUnique(crossing, b.CopyPoint(input, z)); }
                if (fa != 0 && fz != 0 && (fa < 0) != (fz < 0)) AddUnique(crossing, b.EdgePoint(input, a, z, fa / (fa - fz), v));
            }
            if (crossing.Count < 3) return;
            // The crossings lie on a convex polygon; they are ordered by their angle about their center
            double[][] pts = crossing.Select(id => new[] { b.Points[(int)(3 * id)], b.Points[(int)(3 * id + 1)],
                                                            b.Points[(int)(3 * id + 2)] }).ToArray();
            double[] center = new double[3];
            foreach (double[] p in pts) for (int c = 0; c < 3; c++) center[c] += p[c] / pts.Length;
            double[] normal = new double[3];
            for (int i = 0; i < pts.Length; i++)
            {
                double[] a = pts[i], z = pts[(i + 1) % pts.Length];
                normal[0] += (a[1] - z[1]) * (a[2] + z[2]);
                normal[1] += (a[2] - z[2]) * (a[0] + z[0]);
                normal[2] += (a[0] - z[0]) * (a[1] + z[1]);
            }
            if (vtkMath.Normalize(normal) < 1e-300)
            {
                // Use any non degenerate triangle
                for (int i = 1; i + 1 < pts.Length && vtkMath.Norm(normal) < 1e-300; i++)
                {
                    normal = vtkMath.Cross(CellGeometry.Sub(pts[i], pts[0]), CellGeometry.Sub(pts[i + 1], pts[0]));
                    vtkMath.Normalize(normal);
                }
            }
            double[] ax = CellGeometry.Sub(pts[0], center);
            vtkMath.Normalize(ax);
            double[] ay = vtkMath.Cross(normal, ax);
            int[] order = Enumerable.Range(0, pts.Length).OrderBy(i =>
            {
                double[] d = CellGeometry.Sub(pts[i], center);
                return Math.Atan2(vtkMath.Dot(d, ay), vtkMath.Dot(d, ax));
            }).ToArray();
            long[] polygon = order.Select(i => crossing[i]).ToArray();
            int polyType = polygon.Length == 3 ? CellType.Triangle : polygon.Length == 4 ? CellType.Quad : CellType.Polygon;
            b.AddCell(polyType, polygon, cell);
        }
    }

    // Places a copy of the source at each input point, scaled and oriented by the point data
    public partial class vtkGlyph3D
    {
        private bool _scaling = true, _orient = true;
        private int _scaleMode;          // 0 scalar, 1 vector, 2 vector components, 3 data scaling off
        private int _vectorMode;         // 0 vector, 1 normal, 2 off
        private int _colorMode = 0;      // 0 scale, 1 scalar, 2 vector
        private double _scaleFactor = 1;
        private double[] _range = { 0, 1 };
        private bool _clamping;
        private string _scalarsName;

        public void ScalingOn() { _scaling = true; Modified(); }
        public void ScalingOff() { _scaling = false; Modified(); }
        public void SetScaling(int v) { _scaling = v != 0; Modified(); }
        public void OrientOn() { _orient = true; Modified(); }
        public void OrientOff() { _orient = false; Modified(); }
        public void SetOrient(int v) { _orient = v != 0; Modified(); }
        public void SetScaleModeToScaleByScalar() { _scaleMode = 0; Modified(); }
        public void SetScaleModeToScaleByVector() { _scaleMode = 1; Modified(); }
        public void SetScaleModeToScaleByVectorComponents() { _scaleMode = 2; Modified(); }
        public void SetScaleModeToDataScalingOff() { _scaleMode = 3; Modified(); }
        public void SetVectorModeToUseVector() { _vectorMode = 0; Modified(); }
        public void SetVectorModeToUseNormal() { _vectorMode = 1; Modified(); }
        public void SetVectorModeToVectorRotationOff() { _vectorMode = 2; Modified(); }
        public void SetColorModeToColorByScale() { _colorMode = 0; Modified(); }
        public void SetColorModeToColorByScalar() { _colorMode = 1; Modified(); }
        public void SetColorModeToColorByVector() { _colorMode = 2; Modified(); }
        public void SetScaleFactor(double f) { _scaleFactor = f; Modified(); }
        public void SetRange(double a, double b) { _range = new[] { a, b }; Modified(); }
        public void ClampingOn() { _clamping = true; Modified(); }
        public void ClampingOff() { _clamping = false; Modified(); }
        public void SetInputArrayToProcess(int idx, int port, int connection, string association, string name)
        {
            if (idx == 0) _scalarsName = name;
            Modified();
        }
        public void SetInputArrayToProcess(int idx, int port, int connection, int association, string name)
        {
            if (idx == 0) _scalarsName = name;
            Modified();
        }
        public void GeneratePointIdsOn() { }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkPolyData source = GetInputDataObject(1, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            if (source == null)
            {
                // The default glyph is a line along x
                source = SourceTools.Build(new List<double[]> { new double[] { 0, 0, 0 }, new double[] { 1, 0, 0 } }, null, null,
                                           new List<long[]> { new long[] { 0, 1 } });
            }
            vtkDataArray scalars = _scalarsName != null ? input.GetPointData().GetArray(_scalarsName) : input.GetPointData().GetScalars();
            if (scalars == null) scalars = input.GetPointData().GetScalars();
            vtkDataArray vectors = _vectorMode == 1 ? input.GetPointData().GetNormals() : input.GetPointData().GetVectors();
            vtkDataArray sourceNormals = source.GetPointData().GetNormals();
            long numPoints = input.GetNumberOfPoints();
            long numSourcePoints = source.GetNumberOfPoints();
            vtkPoints outPoints = new vtkPoints();
            outPoints.SetNumberOfPoints(numPoints * numSourcePoints);
            double[] raw = outPoints.Raw;
            vtkFloatArray outNormals = null;
            if (sourceNormals != null)
            {
                outNormals = new vtkFloatArray();
                outNormals.SetNumberOfComponents(3);
                outNormals.SetName("Normals");
                outNormals.SetNumberOfTuples(numPoints * numSourcePoints);
            }
            vtkFloatArray outScalars = null;
            vtkDataArray colorScalars = _colorMode == 1 ? input.GetPointData().GetScalars() : null;
            if (scalars != null || colorScalars != null)
            {
                outScalars = new vtkFloatArray();
                outScalars.SetName("GlyphScale");
                outScalars.SetNumberOfTuples(numPoints * numSourcePoints);
            }
            double[] sp = source.GetPoints() != null ? source.GetPoints().Raw : new double[0];
            for (long i = 0; i < numPoints; i++)
            {
                double[] x = input.GetPoint(i);
                double scale = 1;
                double scalarValue = scalars != null ? scalars.GetTuple1(i) : 1;
                double[] v = vectors != null ? vectors.GetTuple3(i) : new double[] { 1, 0, 0 };
                double vMag = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
                double[] scales = { 1, 1, 1 };
                if (_scaling)
                {
                    if (_scaleMode == 0 && scalars != null) scale = scalarValue;
                    else if (_scaleMode == 1) scale = vMag;
                    else if (_scaleMode == 2) scales = new[] { v[0], v[1], v[2] };
                    if (_clamping && _range[1] > _range[0])
                    {
                        scale = Math.Max(_range[0], Math.Min(_range[1], scale));
                        scale = (scale - _range[0]) / (_range[1] - _range[0]);
                    }
                }
                double s = scale * _scaleFactor;
                if (s == 0) s = 1e-10;
                double[] m = vtkMatrix4x4.Translation(x[0], x[1], x[2]);
                if (_orient && vectors != null && vMag > 0 && _vectorMode != 2)
                {
                    if (v[1] == 0 && v[2] == 0)
                    {
                        if (v[0] < 0) m = vtkMatrix4x4.Multiply(m, vtkMatrix4x4.Rotation(180, 0, 1, 0));
                    }
                    else
                    {
                        double[] axis = { (v[0] + vMag) / 2, v[1] / 2, v[2] / 2 };
                        m = vtkMatrix4x4.Multiply(m, vtkMatrix4x4.Rotation(180, axis[0], axis[1], axis[2]));
                    }
                }
                double[] mn = m;     // the rotation part transforms the normals
                if (_scaleMode == 2 && _scaling) m = vtkMatrix4x4.Multiply(m, vtkMatrix4x4.Scaling(scales[0] * _scaleFactor,
                                                                                                    scales[1] * _scaleFactor,
                                                                                                    scales[2] * _scaleFactor));
                else m = vtkMatrix4x4.Multiply(m, vtkMatrix4x4.Scaling(s, s, s));
                long offset = i * numSourcePoints;
                for (long j = 0; j < numSourcePoints; j++)
                {
                    double px = sp[3 * j], py = sp[3 * j + 1], pz = sp[3 * j + 2];
                    long k = 3 * (offset + j);
                    raw[k] = m[0] * px + m[1] * py + m[2] * pz + m[3];
                    raw[k + 1] = m[4] * px + m[5] * py + m[6] * pz + m[7];
                    raw[k + 2] = m[8] * px + m[9] * py + m[10] * pz + m[11];
                    if (outNormals != null)
                    {
                        double[] n = sourceNormals.GetTuple3(j);
                        double[] r = vtkMatrix4x4.TransformVector(mn, n[0], n[1], n[2]);
                        if (s < 0) { r[0] = -r[0]; r[1] = -r[1]; r[2] = -r[2]; }
                        outNormals.SetTuple3(offset + j, r[0], r[1], r[2]);
                    }
                    if (outScalars != null)
                        outScalars.SetValue(offset + j, (float)(colorScalars != null ? colorScalars.GetTuple1(i) : scalarValue));
                }
            }
            output.SetPoints(outPoints);
            // The cells of the source for each point, in the order of insertion
            for (long i = 0; i < numPoints; i++)
            {
                long offset = i * numSourcePoints;
                foreach (var c in FilterTools.Cells(source))
                {
                    long[] ids = new long[c.ids.Length];
                    for (int j = 0; j < ids.Length; j++) ids[j] = c.ids[j] + offset;
                    output.InsertNextCell(c.type, ids);
                }
            }
            if (outNormals != null) output.GetPointData().SetNormals(outNormals);
            if (outScalars != null) output.GetPointData().SetScalars(outScalars);
        }
    }

    public partial class vtkGlyph2D
    {
    }

    // The world size of the given screen size at each point
    public partial class vtkDistanceToCamera
    {
        private vtkRenderer _renderer;
        private double _screenSize = 5;
        private bool _scaling;
        private int[] _lastSize = new int[2];
        private ulong _sizeTime;

        public void SetRenderer(vtkRenderer renderer) { _renderer = renderer; Modified(); }
        public vtkRenderer GetRenderer() { return _renderer; }
        public void SetScreenSize(double s) { _screenSize = s; Modified(); }
        public double GetScreenSize() { return _screenSize; }
        public void ScalingOn() { _scaling = true; }
        public void ScalingOff() { _scaling = false; }
        protected override ulong ExtraModificationTime()
        {
            if (_renderer == null) return 0;
            int[] size = _renderer.GetSize();
            if (size[0] != _lastSize[0] || size[1] != _lastSize[1])
            {
                _lastSize = size;
                _sizeTime = vtkObject.GlobalTime + 1;
            }
            return Math.Max(_renderer.GetActiveCamera().MTime, _sizeTime);
        }
        protected override void Execute()
        {
            vtkPointSet input = GetInputDataObject(0, 0) as vtkPointSet;
            vtkPointSet output = PrepareOutput();
            if (input == null) return;
            output.ShallowCopy(input);
            output.GetPointData().ShallowCopy(input.GetPointData());
            vtkDoubleArray distance = new vtkDoubleArray();
            distance.SetName("DistanceToCamera");
            long n = input.GetNumberOfPoints();
            distance.SetNumberOfValues(n);
            if (_renderer != null)
            {
                vtkCamera camera = _renderer.GetActiveCamera();
                int height = Math.Max(1, _renderer.GetSize()[1]);
                if (camera.GetParallelProjection() != 0)
                {
                    double value = _screenSize * 2 * camera.GetParallelScale() / height;
                    for (long i = 0; i < n; i++) distance.SetValue(i, value);
                }
                else
                {
                    double factor = _screenSize * 2 * Math.Tan(camera.GetViewAngle() * Math.PI / 360) / height;
                    double[] pos = camera.GetPosition();
                    double[] dop = camera.GetDirectionOfProjection();
                    for (long i = 0; i < n; i++)
                    {
                        double[] p = input.GetPoint(i);
                        double depth = (p[0] - pos[0]) * dop[0] + (p[1] - pos[1]) * dop[1] + (p[2] - pos[2]) * dop[2];
                        distance.SetValue(i, factor * depth);
                    }
                }
            }
            output.GetPointData().AddArray(distance);
            output.GetPointData().SetActiveScalars("DistanceToCamera");
        }
    }

    // Transforms the points, normals and vectors; the output has the type of the input
    public partial class vtkTransformFilter
    {
        private vtkAbstractTransform _transform;
        public void SetTransform(vtkAbstractTransform t) { _transform = t; Modified(); }
        public vtkAbstractTransform GetTransform() { return _transform; }
        protected override ulong ExtraModificationTime() { return _transform != null ? _transform.MTime : 0; }
        protected override void Execute()
        {
            vtkPointSet input = GetInputDataObject(0, 0) as vtkPointSet;
            vtkPointSet output = PrepareOutput();
            if (input == null) return;
            TransformTools.Transform(input, output, _transform);
        }
    }

    public partial class vtkTransformPolyDataFilter
    {
        private vtkAbstractTransform _transform;
        public void SetTransform(vtkAbstractTransform t) { _transform = t; Modified(); }
        public vtkAbstractTransform GetTransform() { return _transform; }
        protected override ulong ExtraModificationTime() { return _transform != null ? _transform.MTime : 0; }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            TransformTools.Transform(input, output, _transform);
        }
    }

    internal static class TransformTools
    {
        public static void Transform(vtkPointSet input, vtkPointSet output, vtkAbstractTransform transform)
        {
            output.DeepCopy(input);
            if (transform == null || output.GetPoints() == null) return;
            vtkPoints points = output.GetPoints();
            long n = points.GetNumberOfPoints();
            double[] raw = points.Raw;
            for (long i = 0; i < n; i++)
            {
                double[] p = transform.TransformPoint(raw[3 * i], raw[3 * i + 1], raw[3 * i + 2]);
                raw[3 * i] = p[0]; raw[3 * i + 1] = p[1]; raw[3 * i + 2] = p[2];
            }
            points.Modified();
            if (transform is vtkLinearTransform lt)
            {
                foreach (vtkDataSetAttributes attributes in new vtkDataSetAttributes[] { output.GetPointData(), output.GetCellData() })
                {
                    vtkDataArray normals = attributes.GetNormals();
                    if (normals != null)
                        for (long i = 0; i < normals.GetNumberOfTuples(); i++)
                        {
                            double[] r = lt.TransformNormal(normals.GetTuple3(i));
                            normals.SetTuple3(i, r[0], r[1], r[2]);
                        }
                    vtkDataArray vectors = attributes.GetVectors();
                    if (vectors != null)
                        for (long i = 0; i < vectors.GetNumberOfTuples(); i++)
                        {
                            double[] r = lt.TransformVector(vectors.GetTuple3(i));
                            vectors.SetTuple3(i, r[0], r[1], r[2]);
                        }
                }
            }
            output.Modified();
        }
    }

    // The boundary of an unstructured grid: the faces of the volume cells used by one cell and the lower dimensional cells
    public partial class vtkUnstructuredGridGeometryFilter
    {
        private bool _passPointIds, _passCellIds;
        public void PassThroughPointIdsOn() { _passPointIds = true; Modified(); }
        public void PassThroughPointIdsOff() { _passPointIds = false; Modified(); }
        public void PassThroughCellIdsOn() { _passCellIds = true; Modified(); }
        public void PassThroughCellIdsOff() { _passCellIds = false; Modified(); }
        public void MergingOff() { }
        public void MergingOn() { }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkUnstructuredGrid output = GetOutput();
            output.Initialize();
            if (input == null) return;
            DataBuilder b = new DataBuilder();
            List<(int type, long[] ids, long cell)> cells = new List<(int, long[], long)>();
            foreach (var c in FilterTools.Cells(input))
                if (CellType.Dimension(c.type) < 3) cells.Add((c.type, c.ids, c.id));
            cells.AddRange(FilterTools.BoundaryFaces(input, true));
            cells.Sort((x, y) => x.cell.CompareTo(y.cell));
            List<long> originalPoints = new List<long>();
            foreach (var c in cells)
            {
                long[] ids = new long[c.ids.Length];
                for (int i = 0; i < ids.Length; i++)
                {
                    int before = b.PointSources.Count;
                    ids[i] = b.CopyPoint(input, c.ids[i]);
                    if (b.PointSources.Count > before) originalPoints.Add(c.ids[i]);
                }
                b.AddCell(c.type, ids, c.cell);
            }
            b.Fill(output, input);
            if (_passPointIds)
            {
                vtkIdTypeArray ids = new vtkIdTypeArray();
                ids.SetName("vtkOriginalPointIds");
                foreach (long id in originalPoints) ids.InsertNextValue(id);
                output.GetPointData().AddArray(ids);
            }
            if (_passCellIds)
            {
                vtkIdTypeArray ids = new vtkIdTypeArray();
                ids.SetName("vtkOriginalCellIds");
                foreach (var c in cells) ids.InsertNextValue(c.cell);
                output.GetCellData().AddArray(ids);
            }
        }
    }

    // The surface of a data set as poly data; all input points are kept
    public partial class vtkGeometryFilter
    {
        private bool _merging;
        public void MergingOn() { _merging = true; }
        public void MergingOff() { _merging = false; }
        public void SetMerging(int v) { _merging = v != 0; }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            if (input is vtkPolyData pd)
            {
                output.DeepCopy(pd);
                return;
            }
            DataBuilder b = new DataBuilder();
            long n = input.GetNumberOfPoints();
            for (long i = 0; i < n; i++) b.CopyPoint(input, i);
            List<(int type, long[] ids, long cell)> cells = new List<(int, long[], long)>();
            foreach (var c in FilterTools.Cells(input))
            {
                int dim = CellType.Dimension(c.type);
                if (dim == 0) foreach (long p in c.ids) cells.Add((CellType.Vertex, new[] { p }, c.id));
                else if (dim == 1)
                {
                    if (c.type == CellType.QuadraticEdge && c.ids.Length == 3)
                        cells.Add((CellType.PolyLine, new[] { c.ids[0], c.ids[2], c.ids[1] }, c.id));
                    else cells.Add((c.ids.Length == 2 ? CellType.Line : CellType.PolyLine, c.ids, c.id));
                }
                else if (dim == 2)
                {
                    if (CellType.IsQuadratic(c.type)) cells.Add((CellType.Polygon, FilterTools.InterleavedPolygon(c.type, c.ids), c.id));
                    else if (c.type == CellType.Pixel) cells.Add((CellType.Quad, new[] { c.ids[0], c.ids[1], c.ids[3], c.ids[2] }, c.id));
                    else cells.Add((c.type, c.ids, c.id));
                }
            }
            cells.AddRange(FilterTools.BoundaryFaces(input, false).Select(f =>
                (f.type == CellType.Polygon ? f.type : f.ids.Length == 3 ? CellType.Triangle : f.ids.Length == 4 ? CellType.Quad
                                                                                             : CellType.Polygon, f.ids, f.cell)));
            cells.Sort((x, y) => x.cell.CompareTo(y.cell));
            foreach (var c in cells) b.AddCell(c.type, c.ids, c.cell);
            b.Fill(output, input);
        }
    }

    // Cells by an implicit function
    public partial class vtkExtractGeometry
    {
        private vtkImplicitFunction _function;
        private bool _extractInside = true, _extractBoundaryCells, _extractOnlyBoundaryCells;
        public void SetImplicitFunction(vtkImplicitFunction f) { _function = f; Modified(); }
        public void ExtractInsideOn() { _extractInside = true; Modified(); }
        public void ExtractInsideOff() { _extractInside = false; Modified(); }
        public void ExtractBoundaryCellsOn() { _extractBoundaryCells = true; Modified(); }
        public void ExtractBoundaryCellsOff() { _extractBoundaryCells = false; Modified(); }
        public void ExtractOnlyBoundaryCellsOn() { _extractOnlyBoundaryCells = true; Modified(); }
        public void ExtractOnlyBoundaryCellsOff() { _extractOnlyBoundaryCells = false; Modified(); }
        public void SetCellList(vtkIdList list) { }
        protected override ulong ExtraModificationTime() { return _function != null ? _function.MTime : 0; }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkUnstructuredGrid output = GetOutput();
            output.Initialize();
            if (input == null || _function == null) return;
            long numPoints = input.GetNumberOfPoints();
            bool[] inside = new bool[numPoints];
            double sign = _extractInside ? 1 : -1;
            for (long i = 0; i < numPoints; i++) inside[i] = sign * _function.EvaluateFunction(input.GetPoint(i)) < 0;
            DataBuilder b = new DataBuilder();
            foreach (var c in FilterTools.Cells(input))
            {
                int count = c.ids.Count(p => inside[p]);
                bool all = count == c.ids.Length, none = count == 0;
                bool take;
                if (_extractOnlyBoundaryCells) take = !all && !none && _extractBoundaryCells;
                else if (_extractBoundaryCells) take = !none;
                else take = all;
                if (!take) continue;
                b.AddCell(c.type, c.ids.Select(p => b.CopyPoint(input, p)).ToArray(), c.id);
            }
            b.Fill(output, input);
        }
    }

    public partial class vtkExtractCells
    {
        private long[] _cellList = new long[0];
        public void SetCellList(vtkIdList list) { _cellList = list.ToArray(); Modified(); }
        public void AddCellList(vtkIdList list) { _cellList = _cellList.Concat(list.ToArray()).ToArray(); Modified(); }
        public void SetImplicitFunction(vtkImplicitFunction f) { }
        public void ExtractInsideOff() { }
        public void ExtractBoundaryCellsOn() { }
        public void ExtractBoundaryCellsOff() { }
        public void ExtractOnlyBoundaryCellsOn() { }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkUnstructuredGrid output = GetOutput();
            output.Initialize();
            if (input == null) return;
            DataBuilder b = new DataBuilder();
            foreach (long c in _cellList.Distinct().OrderBy(x => x))
            {
                if (c < 0 || c >= input.GetNumberOfCells()) continue;
                long[] ids = input.GetCellPointIds(c);
                b.AddCell(input.GetCellType(c), ids.Select(p => b.CopyPoint(input, p)).ToArray(), c);
            }
            b.Fill(output, input);
        }
    }

    // The points or cells inside a frustum
    public partial class vtkExtractSelectedFrustum
    {
        private vtkPlanes _frustum;
        private int _fieldType = 0;       // 0 cells, 1 points
        private bool _containingCells;
        public void SetFrustum(vtkPlanes planes) { _frustum = planes; Modified(); }
        public void SetFieldType(int type) { _fieldType = type; Modified(); }
        public void SetContainingCells(int v) { _containingCells = v != 0; Modified(); }
        public void PreserveTopologyOff() { }
        public void InsideOutOff() { }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkUnstructuredGrid output = new vtkUnstructuredGrid();
            SetOutputData(0, output);
            if (input == null || _frustum == null) return;
            int planes = _frustum.GetNumberOfPlanes();
            double[][] origins = new double[planes][], normals = new double[planes][];
            for (int i = 0; i < planes; i++)
            {
                origins[i] = _frustum.GetPoints().GetPoint(i);
                normals[i] = _frustum.GetNormals().GetTuple3(i);
            }
            long numPoints = input.GetNumberOfPoints();
            bool[] inside = new bool[numPoints];
            for (long i = 0; i < numPoints; i++)
            {
                double[] p = input.GetPoint(i);
                bool isIn = true;
                for (int k = 0; k < planes && isIn; k++)
                    isIn = normals[k][0] * (p[0] - origins[k][0]) + normals[k][1] * (p[1] - origins[k][1]) +
                           normals[k][2] * (p[2] - origins[k][2]) <= 0;
                inside[i] = isIn;
            }
            DataBuilder b = new DataBuilder();
            if (_fieldType == 1 && !_containingCells)
            {
                for (long i = 0; i < numPoints; i++)
                    if (inside[i]) b.AddCell(CellType.Vertex, new[] { b.CopyPoint(input, i) }, -1);
                b.Fill(output, input, true, false);
                return;
            }
            foreach (var c in FilterTools.Cells(input))
            {
                bool take = c.ids.Any(p => inside[p]);
                if (!take) take = EdgesCrossFrustum(input, c.type, c.ids, origins, normals);
                if (!take) continue;
                b.AddCell(c.type, c.ids.Select(p => b.CopyPoint(input, p)).ToArray(), c.id);
            }
            b.Fill(output, input);
        }
        private static bool EdgesCrossFrustum(vtkDataSet input, int type, long[] ids, double[][] origins, double[][] normals)
        {
            foreach (int[] e in CellType.Edges(type, ids.Length))
            {
                double[] a = input.GetPoint(ids[e[0]]), z = input.GetPoint(ids[e[1]]);
                double t0 = 0, t1 = 1;
                bool rejected = false;
                for (int k = 0; k < origins.Length && !rejected; k++)
                {
                    double fa = vtkMath.Dot(normals[k], CellGeometry.Sub(a, origins[k]));
                    double fz = vtkMath.Dot(normals[k], CellGeometry.Sub(z, origins[k]));
                    if (fa > 0 && fz > 0) rejected = true;
                    else if (fa > 0) t0 = Math.Max(t0, fa / (fa - fz));
                    else if (fz > 0) t1 = Math.Min(t1, fa / (fa - fz));
                    if (t0 > t1) rejected = true;
                }
                if (!rejected) return true;
            }
            return false;
        }
    }

    // The silhouette of a surface for the camera, the feature edges and the free edges; computed for every camera change
    public partial class vtkPolyDataSilhouette
    {
        private vtkCamera _camera;
        private vtkProp3D _prop;
        private int _direction = 1;       // 0 specified vector, 1 camera vector, 2 camera origin, 3 specified origin
        private bool _enableFeatureAngle = true;
        private double _featureAngle = 60;
        private bool _borderEdges;
        private bool _pieceInvariant = true;

        public void SetCamera(vtkCamera camera) { _camera = camera; Modified(); }
        public void SetProp3D(vtkProp3D prop) { _prop = prop; Modified(); }
        public void SetDirectionToCameraVector() { _direction = 1; Modified(); }
        public void SetDirectionToCameraOrigin() { _direction = 2; Modified(); }
        public void SetEnableFeatureAngle(int v) { _enableFeatureAngle = v != 0; Modified(); }
        public void SetFeatureAngle(double a) { _featureAngle = a; Modified(); }
        public void SetBorderEdges(int v) { _borderEdges = v != 0; Modified(); }
        public void BorderEdgesOn() { _borderEdges = true; Modified(); }
        public void BorderEdgesOff() { _borderEdges = false; Modified(); }
        public void SetPieceInvariant(int v) { _pieceInvariant = v != 0; }
        protected override ulong ExtraModificationTime() { return _camera != null ? _camera.MTime : 0; }

        private vtkPolyData _edgesInput;
        private ulong _edgesTime;
        private List<(long a, long b, int p1, int p2)> _edges;
        private double[][] _normals;
        private double[][] _centers;

        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            if (_edges == null || _edgesInput != input || _edgesTime != input.DataTime) PrepareEdges(input);
            double[] dop = _camera != null ? _camera.GetDirectionOfProjection() : new double[] { 0, 0, -1 };
            double[] pos = _camera != null ? _camera.GetPosition() : new double[3];
            double cosFeature = Math.Cos(_featureAngle * Math.PI / 180);
            vtkCellArray lines = new vtkCellArray();
            foreach (var e in _edges)
            {
                bool draw;
                if (e.p2 < 0) draw = _borderEdges;
                else
                {
                    double[] n1 = _normals[e.p1], n2 = _normals[e.p2];
                    double[] v1 = dop, v2 = dop;
                    if (_direction == 2)
                    {
                        v1 = CellGeometry.Sub(_centers[e.p1], pos);
                        v2 = CellGeometry.Sub(_centers[e.p2], pos);
                    }
                    double d1 = vtkMath.Dot(n1, v1), d2 = vtkMath.Dot(n2, v2);
                    draw = (d1 > 0) != (d2 > 0);
                    if (!draw && _enableFeatureAngle) draw = vtkMath.Dot(n1, n2) < cosFeature;
                }
                if (draw) lines.InsertNextCell(new[] { e.a, e.b });
            }
            output.SetPoints(input.GetPoints());
            output.SetLines(lines);
        }
        private void PrepareEdges(vtkPolyData input)
        {
            _edgesInput = input;
            _edgesTime = input.DataTime;
            var map = new Dictionary<long, (long a, long b, int p1, int p2)>();
            var order = new List<long>();
            List<double[]> normals = new List<double[]>();
            List<double[]> centers = new List<double[]>();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) != 2) continue;
                List<long[]> polys = new List<long[]>();
                if (c.type == CellType.TriangleStrip)
                    foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null)) polys.Add(new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] });
                else polys.Add(c.ids);
                foreach (long[] poly in polys)
                {
                    int index = normals.Count;
                    normals.Add(FilterTools.PolygonNormal(input, poly));
                    double[] center = new double[3];
                    foreach (long p in poly) { double[] x = input.GetPoint(p); for (int k = 0; k < 3; k++) center[k] += x[k] / poly.Length; }
                    centers.Add(center);
                    for (int j = 0; j < poly.Length; j++)
                    {
                        long a = poly[j], b = poly[(j + 1) % poly.Length];
                        long key = FilterTools.EdgeKey(a, b);
                        if (map.TryGetValue(key, out var e))
                        {
                            if (e.p2 < 0) map[key] = (e.a, e.b, e.p1, index);
                        }
                        else
                        {
                            map[key] = (a, b, index, -1);
                            order.Add(key);
                        }
                    }
                }
            }
            _edges = order.Select(k => map[k]).ToList();
            _normals = normals.ToArray();
            _centers = centers.ToArray();
        }
    }

    public partial class vtkFeatureEdges
    {
        private bool _boundary = true, _feature = true, _nonManifold = true, _manifold;
        private double _featureAngle = 30;
        private bool _coloring = true;
        public void BoundaryEdgesOn() { _boundary = true; Modified(); }
        public void BoundaryEdgesOff() { _boundary = false; Modified(); }
        public void FeatureEdgesOn() { _feature = true; Modified(); }
        public void FeatureEdgesOff() { _feature = false; Modified(); }
        public void NonManifoldEdgesOn() { _nonManifold = true; Modified(); }
        public void NonManifoldEdgesOff() { _nonManifold = false; Modified(); }
        public void ManifoldEdgesOn() { _manifold = true; Modified(); }
        public void ManifoldEdgesOff() { _manifold = false; Modified(); }
        public void SetFeatureAngle(double a) { _featureAngle = a; Modified(); }
        public void SetColoring(int v) { _coloring = v != 0; Modified(); }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            var edgeFaces = new Dictionary<long, List<int>>();
            var order = new List<(long a, long b, long key)>();
            List<double[]> normals = new List<double[]>();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) != 2) continue;
                List<long[]> polys = new List<long[]>();
                if (c.type == CellType.TriangleStrip)
                    foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null)) polys.Add(new[] { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] });
                else polys.Add(c.ids);
                foreach (long[] poly in polys)
                {
                    int index = normals.Count;
                    normals.Add(FilterTools.PolygonNormal(input, poly));
                    for (int j = 0; j < poly.Length; j++)
                    {
                        long a = poly[j], b = poly[(j + 1) % poly.Length];
                        if (a == b) continue;
                        long key = FilterTools.EdgeKey(a, b);
                        if (!edgeFaces.TryGetValue(key, out var list))
                        {
                            edgeFaces[key] = list = new List<int>();
                            order.Add((a, b, key));
                        }
                        list.Add(index);
                    }
                }
            }
            double cosFeature = Math.Cos(_featureAngle * Math.PI / 180);
            DataBuilder builder = new DataBuilder();
            foreach (var e in order)
            {
                List<int> faces = edgeFaces[e.key];
                bool take = false;
                if (faces.Count == 1) take = _boundary;
                else if (faces.Count > 2) take = _nonManifold;
                else
                {
                    bool feature = vtkMath.Dot(normals[faces[0]], normals[faces[1]]) < cosFeature;
                    take = (_feature && feature) || (_manifold && !feature);
                }
                if (take) builder.AddCell(CellType.Line, new[] { builder.CopyPoint(input, e.a), builder.CopyPoint(input, e.b) }, -1);
            }
            builder.Fill(output, input, true, false);
        }
    }

    // Clips the polygons with the implicit function; the output keeps the side where the function exceeds the value
    public partial class vtkClipPolyData
    {
        private vtkImplicitFunction _clipFunction;
        private double _value;
        private bool _generateClippedOutput, _generateClipScalars, _insideOut;
        public void SetClipFunction(vtkImplicitFunction f) { _clipFunction = f; Modified(); }
        public void SetValue(double v) { _value = v; Modified(); }
        public void GenerateClippedOutputOn() { _generateClippedOutput = true; Modified(); }
        public void GenerateClippedOutputOff() { _generateClippedOutput = false; Modified(); }
        public void GenerateClipScalarsOn() { _generateClipScalars = true; Modified(); }
        public void GenerateClipScalarsOff() { _generateClipScalars = false; Modified(); }
        public void InsideOutOn() { _insideOut = true; Modified(); }
        public void InsideOutOff() { _insideOut = false; Modified(); }
        public vtkPolyData GetClippedOutput() { Update(); return GetOutput(1); }
        protected override ulong ExtraModificationTime() { return _clipFunction != null ? _clipFunction.MTime : 0; }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData kept = GetOutput(0), clipped = GetOutput(1);
            kept.Initialize();
            clipped.Initialize();
            if (input == null || input.GetPoints() == null || _clipFunction == null) return;
            ClipTools.Clip(input, p => (_insideOut ? -1 : 1) * (_clipFunction.EvaluateFunction(p) - _value), kept, clipped);
        }
    }

    internal static class ClipTools
    {
        // Splits the polygons of the input by the sign of the function: positive values are kept, negative ones clipped
        public static void Clip(vtkPolyData input, Func<double[], double> function, vtkPolyData kept, vtkPolyData clipped)
        {
            long n = input.GetNumberOfPoints();
            double[] f = new double[n];
            for (long i = 0; i < n; i++) f[i] = function(input.GetPoint(i));
            DataBuilder keep = new DataBuilder(), cut = new DataBuilder();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) != 2)
                {
                    bool positive = c.ids.All(p => f[p] >= 0);
                    DataBuilder target = positive ? keep : cut;
                    target.AddCell(c.type, c.ids.Select(p => target.CopyPoint(input, p)).ToArray(), c.id);
                    continue;
                }
                foreach (int[] t in CellType.Triangulate(c.type, c.ids.Length, null))
                {
                    long[] tri = { c.ids[t[0]], c.ids[t[1]], c.ids[t[2]] };
                    ClipPolygon(input, tri, f, keep, true, c.id);
                    if (clipped != null) ClipPolygon(input, tri, f, cut, false, c.id);
                }
            }
            keep.Fill(kept, input);
            if (clipped != null) cut.Fill(clipped, input);
        }
        // Sutherland-Hodgman with the points on the edges shared by the neighbouring polygons
        private static void ClipPolygon(vtkPolyData input, long[] poly, double[] f, DataBuilder b, bool positive, long cell)
        {
            List<long> result = new List<long>();
            for (int i = 0; i < poly.Length; i++)
            {
                long a = poly[i], z = poly[(i + 1) % poly.Length];
                double fa = positive ? f[a] : -f[a], fz = positive ? f[z] : -f[z];
                if (fa >= 0) result.Add(b.CopyPoint(input, a));
                if ((fa >= 0) != (fz >= 0))
                {
                    double t = fa / (fa - fz);
                    result.Add(b.EdgePoint(input, a, z, t, 0));
                }
            }
            result = result.Where((id, i) => i == 0 || id != result[i - 1]).ToList();
            if (result.Count > 1 && result[0] == result[result.Count - 1]) result.RemoveAt(result.Count - 1);
            if (result.Count < 3) return;
            int type = result.Count == 3 ? CellType.Triangle : result.Count == 4 ? CellType.Quad : CellType.Polygon;
            b.AddCell(type, result.ToArray(), cell);
        }
    }

    // Clips a closed surface with planes and closes the cut with faces; the labels mark the cut faces with 1
    public partial class vtkClipClosedSurface
    {
        private readonly List<vtkPlane> _planes = new List<vtkPlane>();
        private bool _generateFaces = true, _generateOutline;
        private int _scalarMode;
        public void SetClippingPlanes(vtkPlaneCollection planes) { _planes.Clear(); _planes.AddRange(planes.Planes); Modified(); }
        public void GenerateFacesOn() { _generateFaces = true; Modified(); }
        public void GenerateFacesOff() { _generateFaces = false; Modified(); }
        public void GenerateOutlineOn() { _generateOutline = true; Modified(); }
        public void GenerateOutlineOff() { _generateOutline = false; Modified(); }
        public void SetScalarModeToLabels() { _scalarMode = 2; Modified(); }
        public void SetScalarModeToNone() { _scalarMode = 0; Modified(); }
        public void SetScalarModeToColors() { _scalarMode = 1; Modified(); }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            vtkPolyData current = input;
            foreach (vtkPlane plane in _planes)
            {
                vtkPolyData kept = new vtkPolyData();
                ClipTools.Clip(current, p => plane.EvaluateFunction(p), kept, null);
                current = kept;
            }
            // The surface triangles and the caps of the cut loops on each plane
            vtkPolyData result = new vtkPolyData();
            vtkPoints points = new vtkPoints();
            points.SetDataTypeToDouble();
            for (long i = 0; i < current.GetNumberOfPoints(); i++) points.InsertNextPoint(current.GetPoint(i));
            result.SetPoints(points);
            List<int> labels = new List<int>();
            foreach (var c in FilterTools.Cells(current))
            {
                if (CellType.Dimension(c.type) != 2) continue;
                result.InsertNextCell(c.type, c.ids);
                labels.Add(0);
            }
            if (_generateFaces)
            {
                foreach (vtkPlane plane in _planes)
                {
                    vtkCutter cutter = new vtkCutter();
                    cutter.SetCutFunction(plane);
                    cutter.SetInput(input);
                    cutter.Update();
                    foreach (long[] tri in CapTools.Cap(cutter.GetOutput(), plane, points))
                    {
                        result.InsertNextCell(CellType.Triangle, tri);
                        labels.Add(1);
                    }
                }
            }
            if (_scalarMode == 2)
            {
                vtkUnsignedCharArray l = new vtkUnsignedCharArray();
                l.SetName("Labels");
                foreach (int label in labels) l.InsertNextValue((byte)label);
                result.GetCellData().SetScalars(l);
            }
            output.DeepCopy(result);
        }
    }

    // Triangulation of the closed loops of a cut on a plane, with holes
    internal static class CapTools
    {
        // The triangles of the area the loops enclose, facing against the plane normal; the points are added to the points
        public static List<long[]> Cap(vtkPolyData cut, vtkPlane plane, vtkPoints points)
        {
            List<long[]> result = new List<long[]>();
            // Merge the points of the cut and chain the segments into loops
            var keyToId = new Dictionary<(long, long, long), int>();
            List<double[]> pts = new List<double[]>();
            int Merge(double[] p)
            {
                double scale = 1e9;
                var key = ((long)Math.Round(p[0] * scale), (long)Math.Round(p[1] * scale), (long)Math.Round(p[2] * scale));
                if (!keyToId.TryGetValue(key, out int id)) { id = pts.Count; pts.Add(p); keyToId[key] = id; }
                return id;
            }
            var adjacency = new Dictionary<int, List<int>>();
            foreach (var c in FilterTools.Cells(cut))
            {
                for (int i = 0; i + 1 < c.ids.Length; i++)
                {
                    int a = Merge(cut.GetPoint(c.ids[i])), b = Merge(cut.GetPoint(c.ids[i + 1]));
                    if (a == b) continue;
                    if (!adjacency.TryGetValue(a, out var la)) adjacency[a] = la = new List<int>();
                    if (!adjacency.TryGetValue(b, out var lb)) adjacency[b] = lb = new List<int>();
                    if (!la.Contains(b)) la.Add(b);
                    if (!lb.Contains(a)) lb.Add(a);
                }
            }
            if (adjacency.Values.Any(l => l.Count != 2)) return result;
            List<List<int>> loops = new List<List<int>>();
            HashSet<int> used = new HashSet<int>();
            foreach (int start in adjacency.Keys)
            {
                if (used.Contains(start)) continue;
                List<int> loop = new List<int> { start };
                used.Add(start);
                int prev = start, cur = adjacency[start][0];
                while (cur != start && !used.Contains(cur))
                {
                    loop.Add(cur);
                    used.Add(cur);
                    int next = adjacency[cur][0] == prev ? adjacency[cur][1] : adjacency[cur][0];
                    prev = cur;
                    cur = next;
                }
                if (loop.Count >= 3) loops.Add(loop);
            }
            if (loops.Count == 0) return result;
            // Plane coordinates
            double[] n = plane.UnitNormal;
            double[] ax = Math.Abs(n[0]) < 0.9 ? new double[] { 1, 0, 0 } : new double[] { 0, 1, 0 };
            double proj = vtkMath.Dot(ax, n);
            ax = new[] { ax[0] - proj * n[0], ax[1] - proj * n[1], ax[2] - proj * n[2] };
            vtkMath.Normalize(ax);
            double[] ay = vtkMath.Cross(n, ax);
            double[] o = plane.GetOrigin();
            double[][] uv = pts.Select(p => new[] { vtkMath.Dot(CellGeometry.Sub(p, o), ax), vtkMath.Dot(CellGeometry.Sub(p, o), ay) }).ToArray();
            List<(int[] outer, List<int[]> holes)> polygons = PolygonTools.NestLoops(loops.Select(l => l.ToArray()).ToList(), uv);
            long baseId = points.GetNumberOfPoints();
            foreach (double[] p in pts) points.InsertNextPoint(p);
            foreach (var polygon in polygons)
            {
                foreach (int[] tri in PolygonTools.Triangulate(polygon.outer, polygon.holes, uv))
                {
                    // Facing against the plane normal: clockwise in the plane coordinates
                    double area = PolygonTools.SignedArea(new[] { tri[0], tri[1], tri[2] }, uv);
                    if (area > 0) result.Add(new[] { baseId + tri[0], baseId + tri[2], baseId + tri[1] });
                    else result.Add(new[] { baseId + tri[0], baseId + tri[1], baseId + tri[2] });
                }
            }
            return result;
        }
    }

    // Polygons with holes in 2D: nesting of loops and ear clipping
    internal static class PolygonTools
    {
        public static double SignedArea(int[] loop, double[][] p)
        {
            double a = 0;
            for (int i = 0; i < loop.Length; i++)
            {
                double[] u = p[loop[i]], v = p[loop[(i + 1) % loop.Length]];
                a += u[0] * v[1] - v[0] * u[1];
            }
            return a / 2;
        }
        public static bool PointInLoop(double[] q, int[] loop, double[][] p)
        {
            bool inside = false;
            for (int i = 0, j = loop.Length - 1; i < loop.Length; j = i++)
            {
                double[] a = p[loop[i]], b = p[loop[j]];
                if ((a[1] > q[1]) != (b[1] > q[1]) && q[0] < (b[0] - a[0]) * (q[1] - a[1]) / (b[1] - a[1]) + a[0]) inside = !inside;
            }
            return inside;
        }
        // Each loop at an even depth is an outer boundary with the loops directly inside it as holes
        public static List<(int[] outer, List<int[]> holes)> NestLoops(List<int[]> loops, double[][] p)
        {
            int n = loops.Count;
            int[] depth = new int[n];
            int[] parent = Enumerable.Repeat(-1, n).ToArray();
            double[] areas = loops.Select(l => Math.Abs(SignedArea(l, p))).ToArray();
            for (int i = 0; i < n; i++)
            {
                double[] q = p[loops[i][0]];
                double best = double.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    if (i == j || areas[j] <= areas[i]) continue;
                    if (PointInLoop(q, loops[j], p))
                    {
                        depth[i]++;
                        if (areas[j] < best) { best = areas[j]; parent[i] = j; }
                    }
                }
            }
            var result = new List<(int[], List<int[]>)>();
            for (int i = 0; i < n; i++)
            {
                if (depth[i] % 2 != 0) continue;
                int[] outer = (int[])loops[i].Clone();
                if (SignedArea(outer, p) < 0) Array.Reverse(outer);
                List<int[]> holes = new List<int[]>();
                for (int j = 0; j < n; j++)
                {
                    if (parent[j] != i || depth[j] != depth[i] + 1) continue;
                    int[] hole = (int[])loops[j].Clone();
                    if (SignedArea(hole, p) > 0) Array.Reverse(hole);
                    holes.Add(hole);
                }
                result.Add((outer, holes));
            }
            return result;
        }
        // Ear clipping of a counter clockwise polygon with clockwise holes joined by bridges
        public static List<int[]> Triangulate(int[] outer, List<int[]> holes, double[][] p)
        {
            List<int> polygon = outer.ToList();
            foreach (int[] hole in holes.OrderByDescending(h => h.Max(i => p[i][0])))
            {
                // The rightmost point of the hole is bridged to a visible point of the polygon
                int hi = 0;
                for (int i = 1; i < hole.Length; i++) if (p[hole[i]][0] > p[hole[hi]][0]) hi = i;
                double[] h = p[hole[hi]];
                int best = -1;
                double bestDist = double.MaxValue;
                for (int i = 0; i < polygon.Count; i++)
                {
                    double[] c = p[polygon[i]];
                    if (c[0] < h[0]) continue;
                    double d = (c[0] - h[0]) * (c[0] - h[0]) + (c[1] - h[1]) * (c[1] - h[1]);
                    if (d >= bestDist) continue;
                    if (!SegmentFree(h, c, polygon, hole, p)) continue;
                    bestDist = d;
                    best = i;
                }
                if (best < 0)
                {
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        double[] c = p[polygon[i]];
                        double d = (c[0] - h[0]) * (c[0] - h[0]) + (c[1] - h[1]) * (c[1] - h[1]);
                        if (d < bestDist && SegmentFree(h, c, polygon, hole, p)) { bestDist = d; best = i; }
                    }
                }
                if (best < 0) continue;
                List<int> merged = new List<int>();
                merged.AddRange(polygon.Take(best + 1));
                for (int k = 0; k <= hole.Length; k++) merged.Add(hole[(hi + k) % hole.Length]);
                merged.AddRange(polygon.Skip(best));
                polygon = merged;
            }
            List<int[]> triangles = new List<int[]>();
            List<int> v = new List<int>(polygon);
            int guard = 0;
            while (v.Count > 3 && guard++ < 100000)
            {
                bool clipped = false;
                for (int i = 0; i < v.Count; i++)
                {
                    int a = v[(i + v.Count - 1) % v.Count], b = v[i], c = v[(i + 1) % v.Count];
                    if (Cross(p[a], p[b], p[c]) <= 1e-30) continue;
                    bool contains = false;
                    for (int k = 0; k < v.Count && !contains; k++)
                    {
                        int q = v[k];
                        if (q == a || q == b || q == c) continue;
                        if (p[q] == p[a] || p[q] == p[b] || p[q] == p[c]) continue;
                        contains = InTriangle(p[q], p[a], p[b], p[c]);
                    }
                    if (contains) continue;
                    triangles.Add(new[] { a, b, c });
                    v.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped)
                {
                    // Degenerate rest: a fan keeps the area
                    for (int i = 1; i + 1 < v.Count; i++) triangles.Add(new[] { v[0], v[i], v[i + 1] });
                    v.Clear();
                }
            }
            if (v.Count == 3) triangles.Add(new[] { v[0], v[1], v[2] });
            return triangles;
        }
        private static double Cross(double[] a, double[] b, double[] c)
        {
            return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
        }
        private static bool InTriangle(double[] q, double[] a, double[] b, double[] c)
        {
            double d1 = Cross(a, b, q), d2 = Cross(b, c, q), d3 = Cross(c, a, q);
            return d1 >= 0 && d2 >= 0 && d3 >= 0;
        }
        private static bool SegmentFree(double[] a, double[] b, List<int> polygon, int[] hole, double[][] p)
        {
            bool Crosses(double[] c, double[] d)
            {
                if (c == a || c == b || d == a || d == b) return false;
                double d1 = Cross(a, b, c), d2 = Cross(a, b, d), d3 = Cross(c, d, a), d4 = Cross(c, d, b);
                return (d1 > 0) != (d2 > 0) && (d3 > 0) != (d4 > 0);
            }
            for (int i = 0; i < polygon.Count; i++)
                if (Crosses(p[polygon[i]], p[polygon[(i + 1) % polygon.Count]])) return false;
            for (int i = 0; i < hole.Length; i++)
                if (Crosses(p[hole[i]], p[hole[(i + 1) % hole.Length]])) return false;
            return true;
        }
    }

    // Joins the lines into poly lines
    public partial class vtkStripper
    {
        private int _maximumLength = 1000;
        public void SetMaximumLength(int n) { _maximumLength = n; }
        public void JoinContiguousSegmentsOn() { }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            List<long[]> segments = new List<long[]>();
            foreach (var c in FilterTools.Cells(input))
                if (CellType.Dimension(c.type) == 1)
                    for (int i = 0; i + 1 < c.ids.Length; i++) segments.Add(new[] { c.ids[i], c.ids[i + 1] });
            var ends = new Dictionary<long, List<int>>();
            for (int i = 0; i < segments.Count; i++)
                foreach (long p in segments[i]) { if (!ends.TryGetValue(p, out var l)) ends[p] = l = new List<int>(); l.Add(i); }
            bool[] used = new bool[segments.Count];
            vtkCellArray lines = new vtkCellArray();
            for (int s = 0; s < segments.Count; s++)
            {
                if (used[s]) continue;
                used[s] = true;
                LinkedList<long> chain = new LinkedList<long>(segments[s]);
                // Extend at both ends
                for (int side = 0; side < 2; side++)
                {
                    while (chain.Count < _maximumLength)
                    {
                        long end = side == 0 ? chain.Last.Value : chain.First.Value;
                        int next = ends[end].FirstOrDefault(k => !used[k], -1);
                        if (next < 0) break;
                        used[next] = true;
                        long other = segments[next][0] == end ? segments[next][1] : segments[next][0];
                        if (side == 0) chain.AddLast(other); else chain.AddFirst(other);
                    }
                }
                lines.InsertNextCell(chain.ToArray());
            }
            output.SetPoints(input.GetPoints());
            output.GetPointData().ShallowCopy(input.GetPointData());
            output.SetLines(lines);
        }
    }

    // Merges coincident points and removes degenerate cells
    public partial class vtkCleanPolyData
    {
        private bool _pointMerging = true, _linesToPoints = true, _polysToLines = true, _stripsToPolys = true;
        private double _tolerance;
        public void PointMergingOn() { _pointMerging = true; Modified(); }
        public void PointMergingOff() { _pointMerging = false; Modified(); }
        public void ConvertLinesToPointsOn() { _linesToPoints = true; Modified(); }
        public void ConvertLinesToPointsOff() { _linesToPoints = false; Modified(); }
        public void ConvertPolysToLinesOn() { _polysToLines = true; Modified(); }
        public void ConvertPolysToLinesOff() { _polysToLines = false; Modified(); }
        public void ConvertStripsToPolysOn() { _stripsToPolys = true; Modified(); }
        public void ConvertStripsToPolysOff() { _stripsToPolys = false; Modified(); }
        public void SetTolerance(double t) { _tolerance = t; Modified(); }
        public void SetAbsoluteTolerance(double t) { _tolerance = t; Modified(); }
        public void ToleranceIsAbsoluteOn() { }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            long n = input.GetNumberOfPoints();
            long[] map = new long[n];
            DataBuilder b = new DataBuilder();
            var keys = new Dictionary<(long, long, long), long>();
            double cell = _tolerance > 0 ? _tolerance * Math.Max(1e-300, input.GetLength()) : 0;
            for (long i = 0; i < n; i++)
            {
                double[] p = input.GetPoint(i);
                if (!_pointMerging) { map[i] = b.CopyPoint(input, i); continue; }
                (long, long, long) key = cell > 0
                    ? ((long)Math.Round(p[0] / cell), (long)Math.Round(p[1] / cell), (long)Math.Round(p[2] / cell))
                    : (BitConverter.DoubleToInt64Bits(p[0]), BitConverter.DoubleToInt64Bits(p[1]), BitConverter.DoubleToInt64Bits(p[2]));
                if (keys.TryGetValue(key, out long existing)) map[i] = existing;
                else keys[key] = map[i] = b.CopyPoint(input, i);
            }
            foreach (var c in FilterTools.Cells(input))
            {
                List<long> ids = new List<long>();
                foreach (long p in c.ids) { long m = map[p]; if (ids.Count == 0 || ids[ids.Count - 1] != m) ids.Add(m); }
                int dim = CellType.Dimension(c.type);
                if (dim == 2 && ids.Count > 1 && ids[0] == ids[ids.Count - 1]) ids.RemoveAt(ids.Count - 1);
                if (dim == 1 && ids.Count < 2)
                {
                    if (_linesToPoints && ids.Count == 1) b.AddCell(CellType.Vertex, ids.ToArray(), c.id);
                    continue;
                }
                if (dim == 2 && ids.Count < 3)
                {
                    if (_polysToLines && ids.Count == 2) b.AddCell(CellType.Line, ids.ToArray(), c.id);
                    continue;
                }
                int type = c.type;
                if (dim == 2 && c.type != CellType.TriangleStrip)
                    type = ids.Count == 3 ? CellType.Triangle : ids.Count == 4 ? CellType.Quad : CellType.Polygon;
                if (dim == 1) type = ids.Count == 2 ? CellType.Line : CellType.PolyLine;
                b.AddCell(type, ids.ToArray(), c.id);
            }
            b.Fill(output, input);
        }
    }

    // A constrained Delaunay triangulation of the points in the xy plane with the edges of the source as constraints
    public partial class vtkDelaunay2D
    {
        private double _tolerance = 0.00001;
        private bool _boundingTriangulation;
        private int _projectionPlaneMode;
        public void SetTolerance(double t) { _tolerance = t; Modified(); }
        public void SetAlpha(double a) { }
        public void SetProjectionPlaneMode(int m) { _projectionPlaneMode = m; Modified(); }
        public void BoundingTriangulationOn() { _boundingTriangulation = true; Modified(); }
        public void BoundingTriangulationOff() { _boundingTriangulation = false; Modified(); }
        protected override void Execute()
        {
            vtkPointSet input = GetInputDataObject(0, 0) as vtkPointSet;
            vtkPolyData source = GetInputDataObject(1, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            int n = (int)input.GetNumberOfPoints();
            double[][] p = new double[n][];
            for (int i = 0; i < n; i++) { double[] x = input.GetPoint(i); p[i] = new[] { x[0], x[1] }; }
            List<(int, int)> constraints = new List<(int, int)>();
            if (source != null)
                foreach (var c in FilterTools.Cells(source))
                    for (int i = 0; i + 1 < c.ids.Length; i++) constraints.Add(((int)c.ids[i], (int)c.ids[i + 1]));
            List<int[]> triangles = Delaunay.Triangulate(p, constraints);
            vtkCellArray polys = new vtkCellArray();
            foreach (int[] t in triangles) polys.InsertNextCell(new long[] { t[0], t[1], t[2] });
            output.SetPoints(input.GetPoints());
            output.SetPolys(polys);
        }
    }

    // Bowyer-Watson triangulation with the constraint edges recovered by edge flips (Sloan)
    internal static class Delaunay
    {
        public static List<int[]> Triangulate(double[][] p, List<(int a, int b)> constraints)
        {
            int n = p.Length;
            List<int[]> result = new List<int[]>();
            if (n < 3) return result;
            double minX = p.Min(q => q[0]), maxX = p.Max(q => q[0]), minY = p.Min(q => q[1]), maxY = p.Max(q => q[1]);
            double d = Math.Max(maxX - minX, maxY - minY);
            if (d <= 0) return result;
            double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            double[][] pts = new double[n + 3][];
            for (int i = 0; i < n; i++) pts[i] = p[i];
            pts[n] = new[] { cx - 20 * d, cy - 20 * d };
            pts[n + 1] = new[] { cx + 20 * d, cy - 20 * d };
            pts[n + 2] = new[] { cx, cy + 20 * d };
            List<int[]> tris = new List<int[]> { new[] { n, n + 1, n + 2 } };
            // Points in a spatial order keep the cavities small
            int[] order = Enumerable.Range(0, n).OrderBy(i => Math.Floor((p[i][0] - minX) / d * 64)).ThenBy(i => p[i][1]).ToArray();
            HashSet<long> seenPoints = new HashSet<long>();
            foreach (int i in order)
            {
                double[] q = pts[i];
                List<int> bad = new List<int>();
                for (int t = 0; t < tris.Count; t++) if (InCircle(pts, tris[t], q)) bad.Add(t);
                if (bad.Count == 0) continue;
                // The boundary of the cavity
                var edgeCount = new Dictionary<long, (int a, int b, int count)>();
                foreach (int t in bad)
                {
                    int[] tri = tris[t];
                    for (int k = 0; k < 3; k++)
                    {
                        int a = tri[k], b = tri[(k + 1) % 3];
                        long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                        if (edgeCount.TryGetValue(key, out var e)) edgeCount[key] = (e.a, e.b, e.count + 1);
                        else edgeCount[key] = (a, b, 1);
                    }
                }
                foreach (int t in bad.OrderByDescending(x => x)) { tris[t] = tris[tris.Count - 1]; tris.RemoveAt(tris.Count - 1); }
                foreach (var e in edgeCount.Values) if (e.count == 1) tris.Add(new[] { e.a, e.b, i });
            }
            foreach (var c in constraints) if (c.a != c.b && c.a < n && c.b < n) RecoverEdge(pts, tris, c.a, c.b);
            foreach (int[] t in tris)
            {
                if (t[0] >= n || t[1] >= n || t[2] >= n) continue;
                result.Add(t);
            }
            return result;
        }
        private static double Orient(double[] a, double[] b, double[] c)
        {
            return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
        }
        private static bool InCircle(double[][] pts, int[] t, double[] d)
        {
            double[] a = pts[t[0]], b = pts[t[1]], c = pts[t[2]];
            if (Orient(a, b, c) < 0) { double[] x = b; b = c; c = x; }
            double ax = a[0] - d[0], ay = a[1] - d[1], bx = b[0] - d[0], by = b[1] - d[1], cx = c[0] - d[0], cy = c[1] - d[1];
            double det = (ax * ax + ay * ay) * (bx * cy - cx * by) - (bx * bx + by * by) * (ax * cy - cx * ay) +
                         (cx * cx + cy * cy) * (ax * by - bx * ay);
            return det > 0;
        }
        private static bool HasEdge(List<int[]> tris, int a, int b)
        {
            foreach (int[] t in tris)
                for (int k = 0; k < 3; k++)
                    if ((t[k] == a && t[(k + 1) % 3] == b) || (t[k] == b && t[(k + 1) % 3] == a)) return true;
            return false;
        }
        private static bool SegmentsCross(double[] a, double[] b, double[] c, double[] d)
        {
            double d1 = Orient(a, b, c), d2 = Orient(a, b, d), d3 = Orient(c, d, a), d4 = Orient(c, d, b);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }
        // Flips the edges that cross the constraint until the constraint is an edge
        private static void RecoverEdge(double[][] pts, List<int[]> tris, int a, int b)
        {
            if (HasEdge(tris, a, b)) return;
            for (int iteration = 0; iteration < 10000; iteration++)
            {
                bool flipped = false;
                for (int t = 0; t < tris.Count && !flipped; t++)
                {
                    int[] tri = tris[t];
                    for (int k = 0; k < 3 && !flipped; k++)
                    {
                        int u = tri[k], v = tri[(k + 1) % 3], w = tri[(k + 2) % 3];
                        if (u == a || u == b || v == a || v == b) continue;
                        if (!SegmentsCross(pts[a], pts[b], pts[u], pts[v])) continue;
                        // The neighbour across u-v
                        for (int s = 0; s < tris.Count; s++)
                        {
                            if (s == t) continue;
                            int[] other = tris[s];
                            int iu = Array.IndexOf(other, u), iv = Array.IndexOf(other, v);
                            if (iu < 0 || iv < 0) continue;
                            int x = other[3 - iu - iv];
                            // The quadrilateral w, u, x, v must be convex to flip
                            if (!SegmentsCross(pts[w], pts[x], pts[u], pts[v])) continue;
                            tris[t] = new[] { w, u, x };
                            tris[s] = new[] { w, x, v };
                            flipped = true;
                            break;
                        }
                    }
                }
                if (HasEdge(tris, a, b) || !flipped) return;
            }
        }
    }

    public partial class vtkTubeFilter
    {
        private double _radius = 0.5;
        private int _numberOfSides = 3;
        private bool _capping;
        public void SetRadius(double r) { _radius = r; Modified(); }
        public void SetNumberOfSides(int n) { _numberOfSides = Math.Max(3, n); Modified(); }
        public void CappingOn() { _capping = true; Modified(); }
        public void CappingOff() { _capping = false; Modified(); }
        public void UseDefaultNormalOff() { }
        public void UseDefaultNormalOn() { }
        public void SetVaryRadiusToVaryRadiusOff() { }
        public void SetCenter(double x, double y, double z) { }
        public void SetHeight(double h) { }
        public void SetResolution(int r) { }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            var points = new List<double[]>();
            var normals = new List<double[]>();
            var polys = new List<long[]>();
            foreach (var c in FilterTools.Cells(input))
            {
                if (CellType.Dimension(c.type) != 1 || c.ids.Length < 2) continue;
                double[][] line = c.ids.Select(id => input.GetPoint(id)).ToArray();
                // Frames along the line by parallel transport
                double[] t0 = CellGeometry.Sub(line[1], line[0]);
                vtkMath.Normalize(t0);
                double[] nrm = Math.Abs(t0[0]) < 0.9 ? vtkMath.Cross(t0, new double[] { 1, 0, 0 }) : vtkMath.Cross(t0, new double[] { 0, 1, 0 });
                vtkMath.Normalize(nrm);
                long start = points.Count;
                for (int i = 0; i < line.Length; i++)
                {
                    double[] t = i == 0 ? CellGeometry.Sub(line[1], line[0])
                               : i == line.Length - 1 ? CellGeometry.Sub(line[i], line[i - 1])
                               : CellGeometry.Sub(line[i + 1], line[i - 1]);
                    vtkMath.Normalize(t);
                    double dot = vtkMath.Dot(nrm, t);
                    nrm = new[] { nrm[0] - dot * t[0], nrm[1] - dot * t[1], nrm[2] - dot * t[2] };
                    vtkMath.Normalize(nrm);
                    double[] bin = vtkMath.Cross(t, nrm);
                    for (int k = 0; k < _numberOfSides; k++)
                    {
                        double a = 2 * Math.PI * k / _numberOfSides;
                        double[] dir = { Math.Cos(a) * nrm[0] + Math.Sin(a) * bin[0], Math.Cos(a) * nrm[1] + Math.Sin(a) * bin[1],
                                         Math.Cos(a) * nrm[2] + Math.Sin(a) * bin[2] };
                        points.Add(new[] { line[i][0] + _radius * dir[0], line[i][1] + _radius * dir[1], line[i][2] + _radius * dir[2] });
                        normals.Add(dir);
                    }
                }
                for (int i = 0; i + 1 < line.Length; i++)
                    for (int k = 0; k < _numberOfSides; k++)
                    {
                        long a = start + i * _numberOfSides + k, b = start + i * _numberOfSides + (k + 1) % _numberOfSides;
                        polys.Add(new[] { a, b, b + _numberOfSides, a + _numberOfSides });
                    }
                if (_capping)
                {
                    long[] first = new long[_numberOfSides], last = new long[_numberOfSides];
                    double[] tf = CellGeometry.Sub(line[0], line[1]); vtkMath.Normalize(tf);
                    double[] tl = CellGeometry.Sub(line[line.Length - 1], line[line.Length - 2]); vtkMath.Normalize(tl);
                    for (int k = 0; k < _numberOfSides; k++)
                    {
                        points.Add(points[(int)(start + k)]);
                        normals.Add(tf);
                        first[_numberOfSides - 1 - k] = points.Count - 1;
                    }
                    for (int k = 0; k < _numberOfSides; k++)
                    {
                        points.Add(points[(int)(start + (line.Length - 1) * _numberOfSides + k)]);
                        normals.Add(tl);
                        last[k] = points.Count - 1;
                    }
                    polys.Add(first);
                    polys.Add(last);
                }
            }
            output.DeepCopy(SourceTools.Build(points, normals, polys));
        }
    }

    public partial class vtkLinearExtrusionFilter
    {
        private double[] _vector = { 0, 0, 1 };
        private double _scaleFactor = 1;
        private bool _capping = true;
        public void SetExtrusionTypeToVectorExtrusion() { }
        public void SetExtrusionTypeToNormalExtrusion() { }
        public void SetVector(double x, double y, double z) { _vector = new[] { x, y, z }; Modified(); }
        public void SetScaleFactor(double f) { _scaleFactor = f; Modified(); }
        public void CappingOn() { _capping = true; Modified(); }
        public void CappingOff() { _capping = false; Modified(); }
        public void SetAngle(double a) { }
        public void SetResolution(int r) { }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            double[] v = { _vector[0] * _scaleFactor, _vector[1] * _scaleFactor, _vector[2] * _scaleFactor };
            ExtrusionTools.Sweep(input, output, 1, (p, step) => new[] { p[0] + step * v[0], p[1] + step * v[1], p[2] + step * v[2] },
                                 _capping, false);
        }
    }

    public partial class vtkRotationalExtrusionFilter
    {
        private double _angle = 360;
        private int _resolution = 12;
        private bool _capping = true;
        public void SetAngle(double a) { _angle = a; Modified(); }
        public void SetResolution(int r) { _resolution = Math.Max(1, r); Modified(); }
        public void CappingOn() { _capping = true; Modified(); }
        public void CappingOff() { _capping = false; Modified(); }
        public void SetExtrusionTypeToVectorExtrusion() { }
        public void SetScaleFactor(double f) { }
        public void SetVector(double x, double y, double z) { }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null || input.GetPoints() == null) return;
            double full = Math.Abs(_angle) >= 360 ? 1 : 0;
            ExtrusionTools.Sweep(input, output, _resolution, (p, step) =>
            {
                double a = _angle * Math.PI / 180 * step / _resolution;
                return new[] { p[0] * Math.Cos(a) - p[1] * Math.Sin(a), p[0] * Math.Sin(a) + p[1] * Math.Cos(a), p[2] };
            }, _capping && full == 0, full > 0);
        }
    }

    internal static class ExtrusionTools
    {
        // Sweeps the boundary edges of the polygons and the lines through the steps; the polygons cap the ends
        public static void Sweep(vtkPolyData input, vtkPolyData output, int steps, Func<double[], int, double[]> position,
                                 bool capping, bool closed)
        {
            int n = (int)input.GetNumberOfPoints();
            List<double[]> points = new List<double[]>();
            int layers = closed ? steps : steps + 1;
            for (int s = 0; s < layers; s++)
                for (int i = 0; i < n; i++) points.Add(position(input.GetPoint(i), s));
            List<long[]> polys = new List<long[]>();
            // Boundary edges of the polygons and the line segments
            var edgeCount = new Dictionary<long, (long a, long b, int count)>();
            var order = new List<long>();
            List<long[]> inputPolys = new List<long[]>();
            foreach (var c in FilterTools.Cells(input))
            {
                int dim = CellType.Dimension(c.type);
                if (dim == 2)
                {
                    inputPolys.Add(c.ids);
                    for (int j = 0; j < c.ids.Length; j++)
                    {
                        long a = c.ids[j], b = c.ids[(j + 1) % c.ids.Length];
                        long key = FilterTools.EdgeKey(a, b);
                        if (edgeCount.TryGetValue(key, out var e)) edgeCount[key] = (e.a, e.b, e.count + 1);
                        else { edgeCount[key] = (a, b, 1); order.Add(key); }
                    }
                }
                else if (dim == 1)
                {
                    for (int j = 0; j + 1 < c.ids.Length; j++)
                    {
                        long key = FilterTools.EdgeKey(c.ids[j], c.ids[j + 1]);
                        edgeCount[key] = (c.ids[j], c.ids[j + 1], 1);
                        order.Add(key);
                    }
                }
            }
            foreach (long key in order)
            {
                var e = edgeCount[key];
                if (e.count != 1) continue;
                for (int s = 0; s < steps; s++)
                {
                    long s0 = s * n, s1 = closed ? ((s + 1) % steps) * n : (s + 1) * n;
                    polys.Add(new[] { e.a + s0, e.b + s0, e.b + s1, e.a + s1 });
                }
            }
            if (capping)
            {
                foreach (long[] poly in inputPolys)
                {
                    polys.Add(poly.Reverse().ToArray());
                    polys.Add(poly.Select(id => id + (long)steps * n).ToArray());
                }
            }
            output.DeepCopy(SourceTools.Build(points, null, polys));
        }
    }

    public partial class vtkReverseSense
    {
        private bool _reverseCells, _reverseNormals;
        public void ReverseCellsOn() { _reverseCells = true; Modified(); }
        public void ReverseCellsOff() { _reverseCells = false; Modified(); }
        public void ReverseNormalsOn() { _reverseNormals = true; Modified(); }
        public void ReverseNormalsOff() { _reverseNormals = false; Modified(); }
        protected override void Execute()
        {
            vtkPolyData input = GetInputDataObject(0, 0) as vtkPolyData;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            output.DeepCopy(input);
            if (_reverseCells) for (long c = 0; c < output.GetNumberOfCells(); c++) output.ReverseCell(c);
            if (_reverseNormals)
            {
                vtkDataArray n = output.GetPointData().GetNormals();
                if (n != null)
                    for (long i = 0; i < n.GetNumberOfTuples(); i++)
                    {
                        double[] v = n.GetTuple3(i);
                        n.SetTuple3(i, -v[0], -v[1], -v[2]);
                    }
            }
        }
    }

    public partial class vtkVertexGlyphFilter
    {
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkPolyData output = GetOutput();
            output.Initialize();
            if (input == null) return;
            DataBuilder b = new DataBuilder();
            for (long i = 0; i < input.GetNumberOfPoints(); i++) b.AddCell(CellType.Vertex, new[] { b.CopyPoint(input, i) }, -1);
            b.Fill(output, input, true, false);
        }
    }

    // Interpolates the point data of the source at the points of the input
    public partial class vtkProbeFilter
    {
        public void SetSource(vtkDataObject source) { SetInput(1, source); }
        public void SetSourceData(vtkDataObject source) { SetInput(1, source); }
        protected override void Execute()
        {
            vtkDataSet input = GetInputDataObject(0, 0) as vtkDataSet;
            vtkDataSet source = GetInputDataObject(1, 0) as vtkDataSet;
            if (input == null) return;
            vtkDataSet output = input is vtkPolyData ? new vtkPolyData() : (vtkDataSet)new vtkUnstructuredGrid();
            output.ShallowCopy(input);
            output.GetPointData().Initialize();
            SetOutputData(0, output);
            if (source == null) return;
            vtkCellLocator locator = new vtkCellLocator();
            locator.SetDataSet(source);
            vtkPointData pd = output.GetPointData();
            pd.CopyAllocate(source.GetPointData());
            vtkCharArrayMask mask = new vtkCharArrayMask();
            long n = input.GetNumberOfPoints();
            for (long i = 0; i < n; i++)
            {
                double[] p = input.GetPoint(i);
                long cell = locator.FindCell(p);
                if (cell < 0)
                {
                    // On a surface source the closest cell within a small distance
                    locator.FindClosestPoint(p, out double[] closest, out cell, out double d2);
                    if (cell >= 0 && d2 > 1e-12 * Math.Max(1, source.GetLength() * source.GetLength())) cell = -1;
                }
                if (cell < 0)
                {
                    foreach (vtkAbstractArray array in pd.Arrays) if (array is vtkDataArray da) da.InsertTuple(i, new double[da.GetNumberOfComponents()]);
                    mask.Values.Add(0);
                    continue;
                }
                long[] ids = source.GetCellPointIds(cell);
                vtkPoints points = new vtkPoints();
                points.SetDataTypeToDouble();
                foreach (long id in ids) points.InsertNextPoint(source.GetPoint(id));
                CellGeometry.EvaluatePosition(source.GetCellType(cell), points, p, out _, out _, out double[] w);
                pd.InterpolateTuple(source.GetPointData(), i, ids, w);
                mask.Values.Add(1);
            }
            vtkCharArray valid = new vtkCharArray();
            valid.SetName("vtkValidPointMask");
            foreach (byte v in mask.Values) valid.InsertNextValue((sbyte)v);
            pd.AddArray(valid);
        }
        private class vtkCharArrayMask { public readonly List<byte> Values = new List<byte>(); }
    }

    public class vtkCharArray : vtkDataArray
    {
        public static new vtkCharArray New() { return new vtkCharArray(); }
        public override int GetDataType() { return 2; }
        public override int GetDataTypeSize() { return 1; }
        public sbyte GetValue(long i) { return (sbyte)_data[i]; }
        public void SetValue(long i, sbyte v) { _data[i] = v; }
        public long InsertNextValue(sbyte v) { long i = _count; SetValueCount(_count + 1); _data[i] = v; return i; }
    }

    // Filters the PrePoMax view uses only in development functions; they pass their input
    public partial class vtkDecimatePro
    {
        public void SetTargetReduction(double r) { }
        public void PreserveTopologyOn() { }
        public void PreserveTopologyOff() { }
        public void SplittingOn() { }
        public void SplittingOff() { }
        public void BoundaryVertexDeletionOn() { }
        public void BoundaryVertexDeletionOff() { }
        protected override void Execute() { FilterTools.PassThrough(GetInputDataObject(0, 0) as vtkDataSet, GetOutput()); }
    }

    public partial class vtkWindowedSincPolyDataFilter
    {
        public void SetNumberOfIterations(int n) { }
        public void SetPassBand(double p) { }
        public void SetFeatureAngle(double a) { }
        public void BoundarySmoothingOn() { }
        public void BoundarySmoothingOff() { }
        public void FeatureEdgeSmoothingOn() { }
        public void FeatureEdgeSmoothingOff() { }
        public void NonManifoldSmoothingOn() { }
        public void NormalizeCoordinatesOn() { }
        protected override void Execute() { FilterTools.PassThrough(GetInputDataObject(0, 0) as vtkDataSet, GetOutput()); }
    }

    public partial class vtkSmoothPolyDataFilter
    {
        public void SetNumberOfIterations(int n) { }
        public void SetRelaxationFactor(double f) { }
        public void FeatureEdgeSmoothingOn() { }
        public void FeatureEdgeSmoothingOff() { }
        public void BoundarySmoothingOn() { }
        public void BoundarySmoothingOff() { }
        protected override void Execute() { FilterTools.PassThrough(GetInputDataObject(0, 0) as vtkDataSet, GetOutput()); }
    }

    public partial class vtkLoopSubdivisionFilter
    {
        public void SetNumberOfSubdivisions(int n) { }
        protected override void Execute() { FilterTools.PassThrough(GetInputDataObject(0, 0) as vtkDataSet, GetOutput()); }
    }

    public partial class vtkThreshold
    {
        public void ThresholdByUpper(double v) { }
        public void ThresholdByLower(double v) { }
        public void ThresholdBetween(double a, double b) { }
    }

    public partial class vtkMultiThreshold
    {
    }
}
