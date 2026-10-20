// PrePoMax for macOS - the cell locator of the VTK compatible library: closest points, containing cells and line
// intersections on a bounding volume hierarchy of the cells.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    // A bounding volume hierarchy of boxes
    internal class BoxTree
    {
        private readonly double[] _boxes;       // 6 per item
        private int[] _items;
        private readonly List<Node> _nodes = new List<Node>();
        private const int LeafSize = 8;

        private struct Node
        {
            public double X0, X1, Y0, Y1, Z0, Z1;
            public int Left, Right;     // children; -1 for a leaf
            public int Start, Count;    // items of a leaf
        }

        public BoxTree(double[] boxes, int count)
        {
            _boxes = boxes;
            _items = new int[count];
            for (int i = 0; i < count; i++) _items[i] = i;
            if (count > 0) Build(0, count);
        }
        public int Count { get { return _items.Length; } }

        private int Build(int start, int count)
        {
            Node node = new Node { Left = -1, Right = -1, Start = start, Count = count };
            node.X0 = node.Y0 = node.Z0 = double.MaxValue;
            node.X1 = node.Y1 = node.Z1 = -double.MaxValue;
            for (int i = start; i < start + count; i++)
            {
                int b = 6 * _items[i];
                node.X0 = Math.Min(node.X0, _boxes[b]); node.X1 = Math.Max(node.X1, _boxes[b + 1]);
                node.Y0 = Math.Min(node.Y0, _boxes[b + 2]); node.Y1 = Math.Max(node.Y1, _boxes[b + 3]);
                node.Z0 = Math.Min(node.Z0, _boxes[b + 4]); node.Z1 = Math.Max(node.Z1, _boxes[b + 5]);
            }
            int index = _nodes.Count;
            _nodes.Add(node);
            if (count <= LeafSize) return index;
            // Split along the longest axis at the median of the box centers
            double dx = node.X1 - node.X0, dy = node.Y1 - node.Y0, dz = node.Z1 - node.Z0;
            int axis = dx >= dy && dx >= dz ? 0 : dy >= dz ? 1 : 2;
            Array.Sort(_items, start, count, Comparer<int>.Create((a, b) =>
                (_boxes[6 * a + 2 * axis] + _boxes[6 * a + 2 * axis + 1]).CompareTo(
                 _boxes[6 * b + 2 * axis] + _boxes[6 * b + 2 * axis + 1])));
            int half = count / 2;
            int left = Build(start, half);
            int right = Build(start + half, count - half);
            node.Left = left;
            node.Right = right;
            _nodes[index] = node;
            return index;
        }
        private static double BoxDistance2(Node n, double[] p)
        {
            double d = 0, v;
            v = p[0] < n.X0 ? n.X0 - p[0] : p[0] > n.X1 ? p[0] - n.X1 : 0; d += v * v;
            v = p[1] < n.Y0 ? n.Y0 - p[1] : p[1] > n.Y1 ? p[1] - n.Y1 : 0; d += v * v;
            v = p[2] < n.Z0 ? n.Z0 - p[2] : p[2] > n.Z1 ? p[2] - n.Z1 : 0; d += v * v;
            return d;
        }
        // The item closest to the point by the distance the function computes; the function returns the squared distance
        public int Closest(double[] p, Func<int, double, double> distance2, out double best)
        {
            best = double.MaxValue;
            int bestItem = -1;
            if (_nodes.Count == 0) return -1;
            Stack<int> stack = new Stack<int>();
            stack.Push(0);
            while (stack.Count > 0)
            {
                Node node = _nodes[stack.Pop()];
                if (BoxDistance2(node, p) > best) continue;
                if (node.Left < 0)
                {
                    for (int i = node.Start; i < node.Start + node.Count; i++)
                    {
                        int item = _items[i];
                        double d = distance2(item, best);
                        if (d < best) { best = d; bestItem = item; }
                    }
                }
                else
                {
                    double dl = BoxDistance2(_nodes[node.Left], p);
                    double dr = BoxDistance2(_nodes[node.Right], p);
                    if (dl < dr) { stack.Push(node.Right); stack.Push(node.Left); }
                    else { stack.Push(node.Left); stack.Push(node.Right); }
                }
            }
            return bestItem;
        }
        // The items whose boxes contain the point
        public IEnumerable<int> Containing(double[] p, double tolerance)
        {
            if (_nodes.Count == 0) yield break;
            Stack<int> stack = new Stack<int>();
            stack.Push(0);
            while (stack.Count > 0)
            {
                Node node = _nodes[stack.Pop()];
                if (p[0] < node.X0 - tolerance || p[0] > node.X1 + tolerance || p[1] < node.Y0 - tolerance ||
                    p[1] > node.Y1 + tolerance || p[2] < node.Z0 - tolerance || p[2] > node.Z1 + tolerance) continue;
                if (node.Left < 0)
                {
                    for (int i = node.Start; i < node.Start + node.Count; i++) yield return _items[i];
                }
                else
                {
                    stack.Push(node.Left);
                    stack.Push(node.Right);
                }
            }
        }
        // The items whose boxes the ray p + t d, t in [0, tMax], passes within the tolerance, closest boxes first
        public IEnumerable<int> AlongRay(double[] p, double[] d, double tMax, double tolerance)
        {
            if (_nodes.Count == 0) yield break;
            Stack<int> stack = new Stack<int>();
            stack.Push(0);
            while (stack.Count > 0)
            {
                Node node = _nodes[stack.Pop()];
                if (!RayHitsBox(node, p, d, tMax, tolerance)) continue;
                if (node.Left < 0)
                {
                    for (int i = node.Start; i < node.Start + node.Count; i++) yield return _items[i];
                }
                else
                {
                    stack.Push(node.Left);
                    stack.Push(node.Right);
                }
            }
        }
        // The items whose boxes intersect the box
        public IEnumerable<int> Overlapping(double[] box)
        {
            if (_nodes.Count == 0) yield break;
            Stack<int> stack = new Stack<int>();
            stack.Push(0);
            while (stack.Count > 0)
            {
                Node node = _nodes[stack.Pop()];
                if (node.X1 < box[0] || node.X0 > box[1] || node.Y1 < box[2] || node.Y0 > box[3] ||
                    node.Z1 < box[4] || node.Z0 > box[5]) continue;
                if (node.Left < 0)
                {
                    for (int i = node.Start; i < node.Start + node.Count; i++) yield return _items[i];
                }
                else
                {
                    stack.Push(node.Left);
                    stack.Push(node.Right);
                }
            }
        }
        private static bool RayHitsBox(Node n, double[] p, double[] d, double tMax, double tol)
        {
            double t0 = 0, t1 = tMax;
            double[] lo = { n.X0 - tol, n.Y0 - tol, n.Z0 - tol };
            double[] hi = { n.X1 + tol, n.Y1 + tol, n.Z1 + tol };
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(d[i]) < 1e-300)
                {
                    if (p[i] < lo[i] || p[i] > hi[i]) return false;
                    continue;
                }
                double inv = 1 / d[i];
                double ta = (lo[i] - p[i]) * inv, tb = (hi[i] - p[i]) * inv;
                if (ta > tb) { double t = ta; ta = tb; tb = t; }
                t0 = Math.Max(t0, ta);
                t1 = Math.Min(t1, tb);
                if (t0 > t1) return false;
            }
            return true;
        }
    }

    // The geometry of the cells of a data set prepared for queries: surface cells as triangles, lines as segments,
    // vertices as points and 3D cells as their faces and tetrahedra
    internal class CellQueryData
    {
        public readonly vtkDataSet DataSet;
        public readonly double[] Points;       // 3 per point
        public readonly int[] CellTypes;
        public readonly long[][] CellPoints;
        public readonly BoxTree Tree;
        public readonly ulong Time;

        public CellQueryData(vtkDataSet dataSet)
        {
            DataSet = dataSet;
            Time = dataSet.DataTime;
            long numPoints = dataSet.GetNumberOfPoints();
            if (dataSet is vtkPointSet ps && ps.GetPoints() != null)
            {
                double[] raw = ps.GetPoints().Raw;
                Points = new double[3 * numPoints];
                Array.Copy(raw, Points, 3 * numPoints);
            }
            else
            {
                Points = new double[3 * numPoints];
                for (long i = 0; i < numPoints; i++)
                {
                    double[] p = dataSet.GetPoint(i);
                    Points[3 * i] = p[0]; Points[3 * i + 1] = p[1]; Points[3 * i + 2] = p[2];
                }
            }
            int numCells = (int)dataSet.GetNumberOfCells();
            CellTypes = new int[numCells];
            CellPoints = new long[numCells][];
            double[] boxes = new double[6 * numCells];
            for (int c = 0; c < numCells; c++)
            {
                CellTypes[c] = dataSet.GetCellType(c);
                long[] ids = dataSet.GetCellPointIds(c);
                CellPoints[c] = ids;
                double x0 = double.MaxValue, x1 = -double.MaxValue, y0 = double.MaxValue, y1 = -double.MaxValue,
                       z0 = double.MaxValue, z1 = -double.MaxValue;
                foreach (long id in ids)
                {
                    if (id < 0 || id >= numPoints) continue;
                    double x = Points[3 * id], y = Points[3 * id + 1], z = Points[3 * id + 2];
                    if (x < x0) x0 = x; if (x > x1) x1 = x;
                    if (y < y0) y0 = y; if (y > y1) y1 = y;
                    if (z < z0) z0 = z; if (z > z1) z1 = z;
                }
                if (x0 > x1) { x0 = x1 = y0 = y1 = z0 = z1 = 0; }
                int b = 6 * c;
                boxes[b] = x0; boxes[b + 1] = x1; boxes[b + 2] = y0; boxes[b + 3] = y1; boxes[b + 4] = z0; boxes[b + 5] = z1;
            }
            Tree = new BoxTree(boxes, numCells);
        }
        public double[] Point(long id) { return new[] { Points[3 * id], Points[3 * id + 1], Points[3 * id + 2] }; }
        // The closest point on a cell; 3D cells give the point itself when it is inside
        public double ClosestPointOnCell(int cell, double[] p, out double[] closest)
        {
            int type = CellTypes[cell];
            long[] ids = CellPoints[cell];
            closest = null;
            double best = double.MaxValue;
            int dim = CellType.Dimension(type);
            if (dim == 0)
            {
                foreach (long id in ids)
                {
                    double[] q = Point(id);
                    double d = CellGeometry.Dist2(p, q);
                    if (d < best) { best = d; closest = q; }
                }
            }
            else if (dim == 1)
            {
                long[] chain = type == CellType.QuadraticEdge && ids.Length == 3 ? new[] { ids[0], ids[2], ids[1] } : ids;
                for (int i = 0; i + 1 < chain.Length; i++)
                {
                    double[] q = CellGeometry.ClosestPointOnSegment(p, Point(chain[i]), Point(chain[i + 1]), out _);
                    double d = CellGeometry.Dist2(p, q);
                    if (d < best) { best = d; closest = q; }
                }
            }
            else if (dim == 2)
            {
                foreach (int[] t in CellType.Triangulate(type, ids.Length, null))
                {
                    double[] q = CellGeometry.ClosestPointOnTriangle(p, Point(ids[t[0]]), Point(ids[t[1]]), Point(ids[t[2]]),
                                                                    out _, out _, out _);
                    double d = CellGeometry.Dist2(p, q);
                    if (d < best) { best = d; closest = q; }
                }
            }
            else
            {
                vtkPoints points = CellPointsAsVtk(cell);
                CellGeometry.EvaluatePosition(type, points, p, out closest, out best, out _);
            }
            if (closest == null) { closest = (double[])p.Clone(); best = double.MaxValue; }
            return best;
        }
        public vtkPoints CellPointsAsVtk(int cell)
        {
            vtkPoints points = new vtkPoints();
            points.SetDataTypeToDouble();
            foreach (long id in CellPoints[cell]) points.InsertNextPoint(Point(id));
            return points;
        }
        // The ray parameter of the first intersection of the ray p + t d with the cell within the tolerance, or -1
        public double IntersectRay(int cell, double[] p, double[] d, double tolerance)
        {
            int type = CellTypes[cell];
            long[] ids = CellPoints[cell];
            int dim = CellType.Dimension(type);
            double best = -1;
            if (dim == 2)
            {
                foreach (int[] t in CellType.Triangulate(type, ids.Length, null))
                {
                    double s = RayTriangle(p, d, Point(ids[t[0]]), Point(ids[t[1]]), Point(ids[t[2]]), tolerance);
                    if (s >= 0 && (best < 0 || s < best)) best = s;
                }
            }
            else if (dim == 3)
            {
                foreach (int[] face in CellType.Faces(type))
                {
                    int faceType = CellType.FaceType(face.Length, false);
                    foreach (int[] t in CellType.Triangulate(faceType, face.Length, null))
                    {
                        double s = RayTriangle(p, d, Point(ids[face[t[0]]]), Point(ids[face[t[1]]]), Point(ids[face[t[2]]]),
                                               tolerance);
                        if (s >= 0 && (best < 0 || s < best)) best = s;
                    }
                }
            }
            else if (dim == 1)
            {
                long[] chain = type == CellType.QuadraticEdge && ids.Length == 3 ? new[] { ids[0], ids[2], ids[1] } : ids;
                for (int i = 0; i + 1 < chain.Length; i++)
                {
                    double s = RaySegment(p, d, Point(chain[i]), Point(chain[i + 1]), tolerance);
                    if (s >= 0 && (best < 0 || s < best)) best = s;
                }
            }
            else
            {
                foreach (long id in ids)
                {
                    double s = RayPoint(p, d, Point(id), tolerance);
                    if (s >= 0 && (best < 0 || s < best)) best = s;
                }
            }
            return best;
        }
        // Moeller-Trumbore; a tolerance widens the triangle
        public static double RayTriangle(double[] p, double[] d, double[] a, double[] b, double[] c, double tolerance)
        {
            double[] e1 = CellGeometry.Sub(b, a), e2 = CellGeometry.Sub(c, a);
            double[] h = vtkMath.Cross(d, e2);
            double det = CellGeometry.Dot(e1, h);
            if (Math.Abs(det) > 1e-300)
            {
                double f = 1 / det;
                double[] s = CellGeometry.Sub(p, a);
                double u = f * CellGeometry.Dot(s, h);
                double[] q = vtkMath.Cross(s, e1);
                double v = f * CellGeometry.Dot(d, q);
                double t = f * CellGeometry.Dot(e2, q);
                if (u >= -1e-9 && v >= -1e-9 && u + v <= 1 + 1e-9 && t >= 0) return t;
            }
            if (tolerance <= 0) return -1;
            // Close to the edges
            double best = -1;
            foreach (var edge in new[] { (a, b), (b, c), (c, a) })
            {
                double s = RaySegment(p, d, edge.Item1, edge.Item2, tolerance);
                if (s >= 0 && (best < 0 || s < best)) best = s;
            }
            return best;
        }
        // The ray parameter where the ray passes the segment closer than the tolerance, or -1
        public static double RaySegment(double[] p, double[] d, double[] a, double[] b, double tolerance)
        {
            double[] u = d, v = CellGeometry.Sub(b, a), w = CellGeometry.Sub(p, a);
            double aa = CellGeometry.Dot(u, u), bb = CellGeometry.Dot(u, v), cc = CellGeometry.Dot(v, v);
            double dd = CellGeometry.Dot(u, w), ee = CellGeometry.Dot(v, w);
            double den = aa * cc - bb * bb;
            double sc, tc;
            if (den < 1e-300) { sc = 0; tc = cc > 0 ? ee / cc : 0; }
            else { sc = (bb * ee - cc * dd) / den; tc = (aa * ee - bb * dd) / den; }
            if (tc < 0) { tc = 0; sc = -dd / aa; }
            else if (tc > 1) { tc = 1; sc = (bb - dd) / aa; }
            if (sc < 0) return -1;
            double[] pc = { p[0] + sc * u[0], p[1] + sc * u[1], p[2] + sc * u[2] };
            double[] qc = { a[0] + tc * v[0], a[1] + tc * v[1], a[2] + tc * v[2] };
            return CellGeometry.Dist2(pc, qc) <= tolerance * tolerance ? sc : -1;
        }
        public static double RayPoint(double[] p, double[] d, double[] q, double tolerance)
        {
            double aa = CellGeometry.Dot(d, d);
            double s = CellGeometry.Dot(CellGeometry.Sub(q, p), d) / aa;
            if (s < 0) return -1;
            double[] pc = { p[0] + s * d[0], p[1] + s * d[1], p[2] + s * d[2] };
            return CellGeometry.Dist2(pc, q) <= tolerance * tolerance ? s : -1;
        }
    }

    public partial class vtkLocator
    {
        protected vtkDataSet _dataSet;
        protected bool _lazy;
        protected double _tolerance = 0.001;

        public void SetDataSet(vtkDataSet dataSet) { _dataSet = dataSet; Modified(); }
        public vtkDataSet GetDataSet() { return _dataSet; }
        public void LazyEvaluationOn() { _lazy = true; }
        public void LazyEvaluationOff() { _lazy = false; }
        public void AutomaticOn() { }
        public void AutomaticOff() { }
        public void SetMaxLevel(int level) { }
        public void SetTolerance(double tol) { _tolerance = tol; }
        public virtual void BuildLocator() { }
        public virtual void Update() { BuildLocator(); }
        public virtual void FreeSearchStructure() { }
    }

    public partial class vtkAbstractCellLocator
    {
        public void SetNumberOfCellsPerBucket(int n) { }
        public void CacheCellBoundsOn() { }
        public void UseExistingSearchStructureOn() { }
    }

    public partial class vtkCellLocator
    {
        private CellQueryData _query;

        internal CellQueryData Query
        {
            get
            {
                if (_dataSet == null) return null;
                if (_query == null || _query.DataSet != _dataSet || _query.Time != _dataSet.DataTime)
                    _query = new CellQueryData(_dataSet);
                return _query;
            }
        }
        public override void BuildLocator() { _ = Query; }
        public override void FreeSearchStructure() { _query = null; }

        // The closest point on the cells of the data set
        public void FindClosestPoint(IntPtr x, IntPtr closestPoint, ref long cellId, ref int subId, ref double dist2)
        {
            double[] p = new double[3];
            Marshal.Copy(x, p, 0, 3);
            FindClosestPoint(p, out double[] closest, out cellId, out dist2);
            subId = 0;
            if (closest != null) Marshal.Copy(closest, 0, closestPoint, 3);
        }
        public void FindClosestPoint(IntPtr x, IntPtr closestPoint, vtkGenericCell cell, ref long cellId, ref int subId,
                                     ref double dist2)
        {
            FindClosestPoint(x, closestPoint, ref cellId, ref subId, ref dist2);
            if (cellId >= 0 && cell != null) _dataSet.GetCell(cellId, cell);
        }
        public void FindClosestPoint(double[] x, double[] closestPoint, ref long cellId, ref int subId, ref double dist2)
        {
            FindClosestPoint(x, out double[] closest, out cellId, out dist2);
            subId = 0;
            if (closest != null) Array.Copy(closest, closestPoint, 3);
        }
        internal void FindClosestPoint(double[] p, out double[] closest, out long cellId, out double dist2)
        {
            closest = null;
            cellId = -1;
            dist2 = -1;
            CellQueryData query = Query;
            if (query == null || query.Tree.Count == 0) return;
            double[] bestPoint = null;
            int cell = query.Tree.Closest(p, (c, bound) =>
            {
                double d = query.ClosestPointOnCell(c, p, out double[] q);
                if (d < bound) bestPoint = q;
                return d;
            }, out double best);
            if (cell < 0) return;
            closest = bestPoint;
            cellId = cell;
            dist2 = best;
        }
        // The cell that contains the point
        public long FindCell(IntPtr x)
        {
            double[] p = new double[3];
            Marshal.Copy(x, p, 0, 3);
            return FindCell(p);
        }
        public long FindCell(double[] p)
        {
            CellQueryData query = Query;
            if (query == null) return -1;
            foreach (int c in query.Tree.Containing(p, 1e-9 * Math.Max(1, _dataSet.GetLength())))
            {
                if (CellType.Dimension(query.CellTypes[c]) < 3) continue;
                if (CellGeometry.EvaluatePosition(query.CellTypes[c], query.CellPointsAsVtk(c), p, out _, out _, out _) == 1)
                    return c;
            }
            return -1;
        }
        // The first intersection of the line from p1 to p2 with the cells
        public int IntersectWithLine(double[] p1, double[] p2, double tol, ref double t, double[] x, double[] pcoords,
                                     ref int subId, ref long cellId)
        {
            CellQueryData query = Query;
            cellId = -1;
            if (query == null) return 0;
            double[] d = CellGeometry.Sub(p2, p1);
            double best = double.MaxValue;
            foreach (int c in query.Tree.AlongRay(p1, d, 1, tol))
            {
                double s = query.IntersectRay(c, p1, d, tol);
                if (s >= 0 && s <= 1 && s < best) { best = s; cellId = c; }
            }
            if (cellId < 0) return 0;
            t = best;
            x[0] = p1[0] + best * d[0]; x[1] = p1[1] + best * d[1]; x[2] = p1[2] + best * d[2];
            subId = 0;
            return 1;
        }
        public void FindCellsWithinBounds(double[] bbox, vtkIdList cells)
        {
            cells.Reset();
            CellQueryData query = Query;
            if (query == null) return;
            foreach (int c in query.Tree.Overlapping(bbox)) cells.InsertNextId(c);
        }
    }
}
