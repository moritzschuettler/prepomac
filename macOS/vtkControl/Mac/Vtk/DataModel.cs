// PrePoMax for macOS - the data model of the VTK compatible library: data sets, attributes, cells, implicit functions and
// the demand driven pipeline of the algorithms.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kitware.VTK
{
    // The cell types of VTK
    internal static class CellType
    {
        public const int Empty = 0, Vertex = 1, PolyVertex = 2, Line = 3, PolyLine = 4, Triangle = 5, TriangleStrip = 6,
                         Polygon = 7, Pixel = 8, Quad = 9, Tetra = 10, Voxel = 11, Hexahedron = 12, Wedge = 13,
                         Pyramid = 14, QuadraticEdge = 21, QuadraticTriangle = 22, QuadraticQuad = 23,
                         QuadraticTetra = 24, QuadraticHexahedron = 25, QuadraticWedge = 26, QuadraticPyramid = 27;

        public static int Dimension(int type)
        {
            switch (type)
            {
                case Vertex: case PolyVertex: return 0;
                case Line: case PolyLine: case QuadraticEdge: return 1;
                case Triangle: case TriangleStrip: case Polygon: case Pixel: case Quad:
                case QuadraticTriangle: case QuadraticQuad: return 2;
                default: return 3;
            }
        }
        public static bool IsQuadratic(int type) { return type >= 21; }
        // The faces of the 3D cells, as point indices of the cell, ordered so that the face normals point outwards
        public static int[][] Faces(int type)
        {
            switch (type)
            {
                case Tetra: return new[] { new[] { 0, 1, 3 }, new[] { 1, 2, 3 }, new[] { 2, 0, 3 }, new[] { 0, 2, 1 } };
                case QuadraticTetra:
                    return new[] { new[] { 0, 1, 3, 4, 8, 7 }, new[] { 1, 2, 3, 5, 9, 8 }, new[] { 2, 0, 3, 6, 7, 9 },
                                   new[] { 0, 2, 1, 6, 5, 4 } };
                case Wedge:
                    return new[] { new[] { 0, 1, 2 }, new[] { 3, 5, 4 }, new[] { 0, 3, 4, 1 }, new[] { 1, 4, 5, 2 },
                                   new[] { 2, 5, 3, 0 } };
                case QuadraticWedge:
                    return new[] { new[] { 0, 1, 2, 6, 7, 8 }, new[] { 3, 5, 4, 11, 10, 9 }, new[] { 0, 3, 4, 1, 12, 9, 13, 6 },
                                   new[] { 1, 4, 5, 2, 13, 10, 14, 7 }, new[] { 2, 5, 3, 0, 14, 11, 12, 8 } };
                case Hexahedron:
                    return new[] { new[] { 0, 4, 7, 3 }, new[] { 1, 2, 6, 5 }, new[] { 0, 1, 5, 4 }, new[] { 3, 7, 6, 2 },
                                   new[] { 0, 3, 2, 1 }, new[] { 4, 5, 6, 7 } };
                case QuadraticHexahedron:
                    return new[] { new[] { 0, 4, 7, 3, 16, 15, 19, 11 }, new[] { 1, 2, 6, 5, 9, 18, 13, 17 },
                                   new[] { 0, 1, 5, 4, 8, 17, 12, 16 }, new[] { 3, 7, 6, 2, 19, 14, 18, 10 },
                                   new[] { 0, 3, 2, 1, 11, 10, 9, 8 }, new[] { 4, 5, 6, 7, 12, 13, 14, 15 } };
                case Pyramid:
                    return new[] { new[] { 0, 3, 2, 1 }, new[] { 0, 1, 4 }, new[] { 1, 2, 4 }, new[] { 2, 3, 4 },
                                   new[] { 3, 0, 4 } };
                case QuadraticPyramid:
                    return new[] { new[] { 0, 3, 2, 1, 8, 7, 6, 5 }, new[] { 0, 1, 4, 5, 10, 9 }, new[] { 1, 2, 4, 6, 11, 10 },
                                   new[] { 2, 3, 4, 7, 12, 11 }, new[] { 3, 0, 4, 8, 9, 12 } };
                case Voxel:
                    return new[] { new[] { 0, 4, 6, 2 }, new[] { 1, 3, 7, 5 }, new[] { 0, 1, 5, 4 }, new[] { 2, 6, 7, 3 },
                                   new[] { 0, 2, 3, 1 }, new[] { 4, 5, 7, 6 } };
                default: return new int[0][];
            }
        }
        // The edges as point indices of the cell; a quadratic edge has its mid point last
        public static int[][] Edges(int type, int numberOfPoints)
        {
            switch (type)
            {
                case Line: return new[] { new[] { 0, 1 } };
                case QuadraticEdge: return new[] { new[] { 0, 1, 2 } };
                case PolyLine:
                    {
                        int[][] e = new int[Math.Max(0, numberOfPoints - 1)][];
                        for (int i = 0; i < e.Length; i++) e[i] = new[] { i, i + 1 };
                        return e;
                    }
                case Triangle: return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 } };
                case Quad: return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 0 } };
                case Pixel: return new[] { new[] { 0, 1 }, new[] { 1, 3 }, new[] { 3, 2 }, new[] { 2, 0 } };
                case Polygon:
                    {
                        int[][] e = new int[numberOfPoints][];
                        for (int i = 0; i < numberOfPoints; i++) e[i] = new[] { i, (i + 1) % numberOfPoints };
                        return e;
                    }
                case QuadraticTriangle: return new[] { new[] { 0, 1, 3 }, new[] { 1, 2, 4 }, new[] { 2, 0, 5 } };
                case QuadraticQuad: return new[] { new[] { 0, 1, 4 }, new[] { 1, 2, 5 }, new[] { 2, 3, 6 }, new[] { 3, 0, 7 } };
                case Tetra: return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 }, new[] { 0, 3 }, new[] { 1, 3 }, new[] { 2, 3 } };
                case QuadraticTetra:
                    return new[] { new[] { 0, 1, 4 }, new[] { 1, 2, 5 }, new[] { 2, 0, 6 }, new[] { 0, 3, 7 }, new[] { 1, 3, 8 },
                                   new[] { 2, 3, 9 } };
                case Wedge:
                    return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 0 }, new[] { 3, 4 }, new[] { 4, 5 }, new[] { 5, 3 },
                                   new[] { 0, 3 }, new[] { 1, 4 }, new[] { 2, 5 } };
                case QuadraticWedge:
                    return new[] { new[] { 0, 1, 6 }, new[] { 1, 2, 7 }, new[] { 2, 0, 8 }, new[] { 3, 4, 9 }, new[] { 4, 5, 10 },
                                   new[] { 5, 3, 11 }, new[] { 0, 3, 12 }, new[] { 1, 4, 13 }, new[] { 2, 5, 14 } };
                case Hexahedron:
                    return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 3, 2 }, new[] { 0, 3 }, new[] { 4, 5 }, new[] { 5, 6 },
                                   new[] { 7, 6 }, new[] { 4, 7 }, new[] { 0, 4 }, new[] { 1, 5 }, new[] { 3, 7 }, new[] { 2, 6 } };
                case QuadraticHexahedron:
                    return new[] { new[] { 0, 1, 8 }, new[] { 1, 2, 9 }, new[] { 3, 2, 10 }, new[] { 0, 3, 11 }, new[] { 4, 5, 12 },
                                   new[] { 5, 6, 13 }, new[] { 7, 6, 14 }, new[] { 4, 7, 15 }, new[] { 0, 4, 16 },
                                   new[] { 1, 5, 17 }, new[] { 3, 7, 19 }, new[] { 2, 6, 18 } };
                case Pyramid:
                    return new[] { new[] { 0, 1 }, new[] { 1, 2 }, new[] { 2, 3 }, new[] { 3, 0 }, new[] { 0, 4 }, new[] { 1, 4 },
                                   new[] { 2, 4 }, new[] { 3, 4 } };
                case QuadraticPyramid:
                    return new[] { new[] { 0, 1, 5 }, new[] { 1, 2, 6 }, new[] { 2, 3, 7 }, new[] { 3, 0, 8 }, new[] { 0, 4, 9 },
                                   new[] { 1, 4, 10 }, new[] { 2, 4, 11 }, new[] { 3, 4, 12 } };
                case Voxel:
                    return new[] { new[] { 0, 1 }, new[] { 1, 3 }, new[] { 2, 3 }, new[] { 0, 2 }, new[] { 4, 5 }, new[] { 5, 7 },
                                   new[] { 6, 7 }, new[] { 4, 6 }, new[] { 0, 4 }, new[] { 1, 5 }, new[] { 2, 6 }, new[] { 3, 7 } };
                default: return new int[0][];
            }
        }
        // The cell type of a face with the given number of points
        public static int FaceType(int numberOfPoints, bool quadratic)
        {
            switch (numberOfPoints)
            {
                case 3: return Triangle;
                case 4: return Quad;
                case 6: return QuadraticTriangle;
                case 8: return QuadraticQuad;
                default: return Polygon;
            }
        }
        // Triangles covering a linear or quadratic surface cell, as point indices of the cell
        public static int[][] Triangulate(int type, int numberOfPoints, Func<int, double[]> point)
        {
            switch (type)
            {
                case Triangle: return new[] { new[] { 0, 1, 2 } };
                case Quad: return new[] { new[] { 0, 1, 2 }, new[] { 0, 2, 3 } };
                case Pixel: return new[] { new[] { 0, 1, 3 }, new[] { 0, 3, 2 } };
                case QuadraticTriangle: return new[] { new[] { 0, 3, 5 }, new[] { 3, 1, 4 }, new[] { 5, 4, 2 }, new[] { 3, 4, 5 } };
                case QuadraticQuad:
                    return new[] { new[] { 0, 4, 7 }, new[] { 4, 1, 5 }, new[] { 5, 2, 6 }, new[] { 6, 3, 7 }, new[] { 4, 5, 7 },
                                   new[] { 5, 6, 7 } };
                case TriangleStrip:
                    {
                        int[][] t = new int[Math.Max(0, numberOfPoints - 2)][];
                        for (int i = 0; i < t.Length; i++) t[i] = i % 2 == 0 ? new[] { i, i + 1, i + 2 } : new[] { i + 1, i, i + 2 };
                        return t;
                    }
                case Polygon:
                    {
                        int[][] t = new int[Math.Max(0, numberOfPoints - 2)][];
                        for (int i = 0; i < t.Length; i++) t[i] = new[] { 0, i + 1, i + 2 };
                        return t;
                    }
                default: return new int[0][];
            }
        }
    }

    // Field data and attributes
    public partial class vtkFieldData
    {
        protected readonly List<vtkAbstractArray> _arrays = new List<vtkAbstractArray>();

        public int GetNumberOfArrays() { return _arrays.Count; }
        public vtkDataArray GetArray(int i) { return i >= 0 && i < _arrays.Count ? _arrays[i] as vtkDataArray : null; }
        public vtkDataArray GetArray(string name) { return GetAbstractArray(name) as vtkDataArray; }
        public vtkAbstractArray GetAbstractArray(int i) { return i >= 0 && i < _arrays.Count ? _arrays[i] : null; }
        public vtkAbstractArray GetAbstractArray(string name)
        {
            foreach (vtkAbstractArray array in _arrays) if (array.GetName() == name) return array;
            return null;
        }
        public string GetArrayName(int i) { return _arrays[i].GetName(); }
        public virtual int AddArray(vtkAbstractArray array)
        {
            if (array == null) return -1;
            string name = array.GetName();
            if (name != null)
            {
                for (int i = 0; i < _arrays.Count; i++)
                {
                    if (_arrays[i].GetName() == name)
                    {
                        _arrays[i] = array;
                        Modified();
                        return i;
                    }
                }
            }
            _arrays.Add(array);
            Modified();
            return _arrays.Count - 1;
        }
        public virtual void RemoveArray(string name)
        {
            int index = _arrays.FindIndex(a => a.GetName() == name);
            if (index >= 0) RemoveArray(index);
        }
        public virtual void RemoveArray(int index)
        {
            if (index < 0 || index >= _arrays.Count) return;
            _arrays.RemoveAt(index);
            Modified();
        }
        public int HasArray(string name) { return GetAbstractArray(name) != null ? 1 : 0; }
        public virtual void Initialize() { _arrays.Clear(); Modified(); }
        public void Squeeze() { }
        public long GetNumberOfTuples() { return _arrays.Count > 0 ? _arrays[0].GetNumberOfTuples() : 0; }
        public override uint GetMTime()
        {
            uint t = base.GetMTime();
            foreach (vtkAbstractArray array in _arrays) t = Math.Max(t, array.GetMTime());
            return t;
        }
        internal ulong DataTime
        {
            get
            {
                ulong t = MTime;
                foreach (vtkAbstractArray array in _arrays) t = Math.Max(t, array.MTime);
                return t;
            }
        }
        internal IReadOnlyList<vtkAbstractArray> Arrays { get { return _arrays; } }
    }

    public partial class vtkDataSetAttributes
    {
        // The arrays with attribute roles
        protected vtkDataArray _scalars, _vectors, _normals, _tCoords, _tensors, _globalIds, _pedigreeIds;
        private bool _copyScalars = true;

        // The attribute arrays by their index: scalars, vectors, normals, texture coordinates, tensors, global ids
        public vtkDataArray GetAttribute(int index)
        {
            switch (index)
            {
                case 0: return GetScalars();
                case 1: return GetVectors();
                case 2: return GetNormals();
                case 3: return GetTCoords();
                case 4: return GetTensors();
                case 5: return GetGlobalIds();
                default: return null;
            }
        }
        public vtkDataArray GetScalars() { return _scalars != null && _arrays.Contains(_scalars) ? _scalars : null; }
        public vtkDataArray GetScalars(string name) { return GetArray(name) ?? GetScalars(); }
        public int SetScalars(vtkDataArray array) { return SetAttribute(ref _scalars, array); }
        public vtkDataArray GetVectors() { return _vectors != null && _arrays.Contains(_vectors) ? _vectors : null; }
        public vtkDataArray GetVectors(string name) { return GetArray(name) ?? GetVectors(); }
        public int SetVectors(vtkDataArray array) { return SetAttribute(ref _vectors, array); }
        public vtkDataArray GetNormals() { return _normals != null && _arrays.Contains(_normals) ? _normals : null; }
        public int SetNormals(vtkDataArray array) { return SetAttribute(ref _normals, array); }
        public vtkDataArray GetTCoords() { return _tCoords != null && _arrays.Contains(_tCoords) ? _tCoords : null; }
        public int SetTCoords(vtkDataArray array) { return SetAttribute(ref _tCoords, array); }
        public vtkDataArray GetTensors() { return _tensors != null && _arrays.Contains(_tensors) ? _tensors : null; }
        public int SetTensors(vtkDataArray array) { return SetAttribute(ref _tensors, array); }
        public vtkDataArray GetGlobalIds() { return _globalIds != null && _arrays.Contains(_globalIds) ? _globalIds : null; }
        public int SetGlobalIds(vtkDataArray array) { return SetAttribute(ref _globalIds, array); }
        public vtkAbstractArray GetPedigreeIds() { return _pedigreeIds != null && _arrays.Contains(_pedigreeIds) ? _pedigreeIds : null; }
        public int SetPedigreeIds(vtkAbstractArray array) { vtkDataArray a = array as vtkDataArray; return SetAttribute(ref _pedigreeIds, a); }
        public int SetActiveScalars(string name)
        {
            vtkDataArray array = GetArray(name);
            if (array == null) { if (_scalars != null) { _scalars = null; Modified(); } return -1; }
            _scalars = array;
            Modified();
            return _arrays.IndexOf(array);
        }
        public int SetActiveVectors(string name)
        {
            vtkDataArray array = GetArray(name);
            if (array == null) return -1;
            _vectors = array;
            Modified();
            return _arrays.IndexOf(array);
        }
        public int SetActiveNormals(string name)
        {
            vtkDataArray array = GetArray(name);
            if (array == null) return -1;
            _normals = array;
            Modified();
            return _arrays.IndexOf(array);
        }
        public void CopyScalarsOn() { _copyScalars = true; }
        public void CopyScalarsOff() { _copyScalars = false; }
        public void CopyAllOn() { _copyScalars = true; }
        public void CopyAllOff() { }
        public void PassData(vtkFieldData fd) { ShallowCopy(fd); }
        private int SetAttribute(ref vtkDataArray attribute, vtkDataArray array)
        {
            if (attribute != null && attribute != array) _arrays.Remove(attribute);
            attribute = array;
            if (array == null) { Modified(); return -1; }
            if (!_arrays.Contains(array))
            {
                // An array with the same name is replaced
                string name = array.GetName();
                if (name != null) _arrays.RemoveAll(a => a.GetName() == name && a != array);
                _arrays.Add(array);
            }
            Modified();
            return _arrays.IndexOf(array);
        }
        public override void RemoveArray(int index)
        {
            if (index < 0 || index >= _arrays.Count) return;
            vtkAbstractArray array = _arrays[index];
            if (array == _scalars) _scalars = null;
            if (array == _vectors) _vectors = null;
            if (array == _normals) _normals = null;
            if (array == _tCoords) _tCoords = null;
            if (array == _tensors) _tensors = null;
            if (array == _globalIds) _globalIds = null;
            if (array == _pedigreeIds) _pedigreeIds = null;
            base.RemoveArray(index);
        }
        public override void Initialize()
        {
            _scalars = _vectors = _normals = _tCoords = _tensors = _globalIds = _pedigreeIds = null;
            base.Initialize();
        }
        public void ShallowCopy(vtkFieldData fd)
        {
            Initialize();
            if (fd == null) return;
            foreach (vtkAbstractArray array in fd.Arrays) _arrays.Add(array);
            if (fd is vtkDataSetAttributes a)
            {
                _scalars = a._scalars; _vectors = a._vectors; _normals = a._normals; _tCoords = a._tCoords;
                _tensors = a._tensors; _globalIds = a._globalIds; _pedigreeIds = a._pedigreeIds;
            }
            Modified();
        }
        public void DeepCopy(vtkFieldData fd)
        {
            Initialize();
            if (fd == null) return;
            vtkDataSetAttributes a = fd as vtkDataSetAttributes;
            foreach (vtkAbstractArray array in fd.Arrays)
            {
                vtkAbstractArray copy = array;
                if (array is vtkDataArray da)
                {
                    vtkDataArray c = da.NewInstance();
                    c.DeepCopy(da);
                    copy = c;
                    if (a != null)
                    {
                        if (array == a._scalars) _scalars = c;
                        if (array == a._vectors) _vectors = c;
                        if (array == a._normals) _normals = c;
                        if (array == a._tCoords) _tCoords = c;
                        if (array == a._tensors) _tensors = c;
                        if (array == a._globalIds) _globalIds = c;
                        if (array == a._pedigreeIds) _pedigreeIds = c;
                    }
                }
                _arrays.Add(copy);
            }
            Modified();
        }
        // Copying of tuples by the filters: the output gets empty arrays of the same kind with the same roles
        internal void CopyAllocate(vtkDataSetAttributes source)
        {
            Initialize();
            if (source == null) return;
            foreach (vtkAbstractArray array in source._arrays)
            {
                if (!(array is vtkDataArray da)) continue;
                if (!_copyScalars && array == source._scalars) continue;
                vtkDataArray copy = da.CreateEmptyCopy();
                _arrays.Add(copy);
                if (array == source._scalars) _scalars = copy;
                if (array == source._vectors) _vectors = copy;
                if (array == source._normals) _normals = copy;
                if (array == source._tCoords) _tCoords = copy;
                if (array == source._tensors) _tensors = copy;
                if (array == source._globalIds) _globalIds = copy;
                if (array == source._pedigreeIds) _pedigreeIds = copy;
            }
        }
        // Copies tuple fromId of the arrays of the source to tuple toId of the arrays allocated by CopyAllocate
        internal void CopyData(vtkDataSetAttributes source, long fromId, long toId)
        {
            int j = 0;
            foreach (vtkAbstractArray array in source._arrays)
            {
                if (!(array is vtkDataArray da)) continue;
                if (!_copyScalars && array == source._scalars) continue;
                if (j >= _arrays.Count) break;
                ((vtkDataArray)_arrays[j++]).CopyTupleFrom(da, fromId, toId);
            }
        }
        // Interpolates tuple toId from the tuples ids with the weights
        internal void InterpolateTuple(vtkDataSetAttributes source, long toId, long[] ids, double[] weights)
        {
            int j = 0;
            foreach (vtkAbstractArray array in source._arrays)
            {
                if (!(array is vtkDataArray da)) continue;
                if (!_copyScalars && array == source._scalars) continue;
                if (j >= _arrays.Count) break;
                vtkDataArray target = (vtkDataArray)_arrays[j++];
                int nc = da.GetNumberOfComponents();
                double[] tuple = new double[nc];
                bool isId = array == source._globalIds || array == source._pedigreeIds;
                if (isId)
                {
                    // Ids are not interpolated, the closest point gives its id
                    int best = 0;
                    for (int k = 1; k < weights.Length; k++) if (weights[k] > weights[best]) best = k;
                    target.CopyTupleFrom(da, ids[best], toId);
                    continue;
                }
                for (int k = 0; k < ids.Length; k++)
                    for (int c = 0; c < nc; c++) tuple[c] += weights[k] * da.GetComponent(ids[k], c);
                target.InsertTuple(toId, tuple);
            }
        }
    }

    // Data objects
    public partial class vtkDataObject
    {
        internal vtkAlgorithm _producer;
        protected vtkFieldData _fieldData = new vtkFieldData();

        public vtkFieldData GetFieldData() { return _fieldData; }
        public void SetFieldData(vtkFieldData fd) { _fieldData = fd; Modified(); }
        public virtual void Initialize() { Modified(); }
        public virtual void Update() { _producer?.Update(); }
        public virtual void ShallowCopy(vtkDataObject src) { }
        public virtual void DeepCopy(vtkDataObject src) { }
        public virtual uint GetActualMemorySize() { return 1; }
        public virtual int GetDataObjectType() { return 0; }
        public vtkAlgorithm GetProducer() { return _producer; }
        public vtkAlgorithmOutput GetProducerPort()
        {
            vtkTrivialProducer producer = new vtkTrivialProducer(this);
            return producer.GetOutputPort();
        }
        internal virtual ulong DataTime { get { return MTime; } }
        public void ReleaseData() { }
        public void Register(vtkObject o) { }
    }

    internal class vtkTrivialProducer : vtkAlgorithm
    {
        private readonly vtkDataObject _data;
        public vtkTrivialProducer(vtkDataObject data) { _data = data; SetOutputData(0, data, false); }
        protected override void Execute() { }
        internal override void UpdatePipeline() { }
    }

    public partial class vtkDataSet
    {
        protected vtkPointData _pointData = new vtkPointData();
        protected vtkCellData _cellData = new vtkCellData();

        public vtkPointData GetPointData() { return _pointData; }
        public vtkCellData GetCellData() { return _cellData; }
        public virtual long GetNumberOfPoints() { return 0; }
        public virtual long GetNumberOfCells() { return 0; }
        public virtual double[] GetPoint(long i) { return new double[3]; }
        public void GetPoint(long i, double[] x) { double[] p = GetPoint(i); x[0] = p[0]; x[1] = p[1]; x[2] = p[2]; }
        public virtual int GetCellType(long cellId) { return 0; }
        public virtual void GetCellPoints(long cellId, vtkIdList ptIds) { ptIds.Reset(); }
        internal virtual long[] GetCellPointIds(long cellId) { return new long[0]; }
        public virtual vtkCell GetCell(long cellId)
        {
            vtkGenericCell cell = new vtkGenericCell();
            GetCell(cellId, cell);
            return cell;
        }
        public virtual void GetCell(long cellId, vtkGenericCell cell)
        {
            long[] ids = GetCellPointIds(cellId);
            vtkPoints points = new vtkPoints();
            points.SetDataTypeToDouble();
            foreach (long id in ids) points.InsertNextPoint(GetPoint(id));
            cell.Initialize(GetCellType(cellId), ids, points);
        }
        public virtual void GetCellTypes(vtkCellTypes types)
        {
            types.Reset();
            HashSet<int> seen = new HashSet<int>();
            long n = GetNumberOfCells();
            for (long i = 0; i < n; i++)
            {
                int type = GetCellType(i);
                if (seen.Add(type)) types.InsertNextType((byte)type);
            }
        }
        public virtual double[] GetBounds() { return new double[] { 1, -1, 1, -1, 1, -1 }; }
        public void GetBounds(double[] bounds) { Array.Copy(GetBounds(), bounds, 6); }
        public double[] GetCenter()
        {
            double[] b = GetBounds();
            return new double[] { (b[0] + b[1]) / 2, (b[2] + b[3]) / 2, (b[4] + b[5]) / 2 };
        }
        public double GetLength()
        {
            double[] b = GetBounds();
            if (b[1] < b[0]) return 0;
            return Math.Sqrt((b[1] - b[0]) * (b[1] - b[0]) + (b[3] - b[2]) * (b[3] - b[2]) + (b[5] - b[4]) * (b[5] - b[4]));
        }
        public double[] GetScalarRange()
        {
            vtkDataArray scalars = _pointData.GetScalars() ?? _cellData.GetScalars();
            return scalars != null ? scalars.GetRange() : new double[] { 0, 1 };
        }
        public void ComputeBounds() { GetBounds(); }
        public virtual long FindPoint(double x, double y, double z) { return FindPoint(new double[] { x, y, z }); }
        public virtual long FindPoint(double[] x)
        {
            long best = -1;
            double min = double.MaxValue;
            long n = GetNumberOfPoints();
            for (long i = 0; i < n; i++)
            {
                double d = vtkMath.Distance2BetweenPoints(GetPoint(i), x);
                if (d < min) { min = d; best = i; }
            }
            return best;
        }
        public virtual int GetMaxCellSize()
        {
            int max = 0;
            long n = GetNumberOfCells();
            for (long i = 0; i < n; i++) max = Math.Max(max, GetCellPointIds(i).Length);
            return max;
        }
        public virtual void Squeeze() { }
        public void CopyStructure(vtkDataSet ds) { ShallowCopy(ds); }
        public override uint GetMTime()
        {
            return Math.Max(base.GetMTime(), Math.Max(_pointData.GetMTime(), _cellData.GetMTime()));
        }
        internal override ulong DataTime { get { return Math.Max(MTime, Math.Max(_pointData.DataTime, _cellData.DataTime)); } }
        public override uint GetActualMemorySize()
        {
            uint size = 1;
            foreach (vtkAbstractArray array in _pointData.Arrays) size += array.GetActualMemorySize();
            foreach (vtkAbstractArray array in _cellData.Arrays) size += array.GetActualMemorySize();
            return size;
        }
        public static vtkDataSet SafeDownCast(vtkObjectBase o) { return o as vtkDataSet; }
    }

    public partial class vtkPointSet
    {
        protected vtkPoints _points;

        public vtkPoints GetPoints() { return _points; }
        public virtual void SetPoints(vtkPoints points) { _points = points; Modified(); }
        public override long GetNumberOfPoints() { return _points != null ? _points.GetNumberOfPoints() : 0; }
        public override double[] GetPoint(long i) { return _points.GetPoint(i); }
        public override double[] GetBounds()
        {
            if (_points == null || _points.GetNumberOfPoints() == 0) return new double[] { 1, -1, 1, -1, 1, -1 };
            return UsedPointBounds();
        }
        // The bounds of the points the cells use; a data set without cells uses all points
        protected virtual double[] UsedPointBounds() { return _points.GetBounds(); }
        public override uint GetMTime()
        {
            uint t = base.GetMTime();
            if (_points != null) t = Math.Max(t, _points.GetMTime());
            return t;
        }
        internal override ulong DataTime
        {
            get
            {
                ulong t = base.DataTime;
                if (_points != null) t = Math.Max(t, _points.DataTime);
                return t;
            }
        }
        public override uint GetActualMemorySize()
        {
            return base.GetActualMemorySize() + (_points != null ? _points.GetActualMemorySize() : 0);
        }
        public override long FindPoint(double[] x)
        {
            if (_points == null) return -1;
            long best = -1;
            double min = double.MaxValue;
            double[] d = _points.Raw;
            long n = _points.GetNumberOfPoints();
            for (long i = 0; i < n; i++)
            {
                double dx = d[3 * i] - x[0], dy = d[3 * i + 1] - x[1], dz = d[3 * i + 2] - x[2];
                double dist = dx * dx + dy * dy + dz * dz;
                if (dist < min) { min = dist; best = i; }
            }
            return best;
        }
        public static vtkPointSet SafeDownCast(vtkObjectBase o) { return o as vtkPointSet; }
    }

    // Poly data: vertices, lines, polygons and triangle strips; the cell ids follow the order of insertion
    public partial class vtkPolyData
    {
        private vtkCellArray _verts = new vtkCellArray();
        private vtkCellArray _lines = new vtkCellArray();
        private vtkCellArray _polys = new vtkCellArray();
        private vtkCellArray _strips = new vtkCellArray();
        // Cell id -> (kind, index in the cell array, cell type); null while the ids follow the array order
        private List<(byte kind, int index, byte type)> _cellMap;
        private List<int>[] _links;
        private ulong _linksTime;

        public vtkCellArray GetVerts() { return _verts; }
        public vtkCellArray GetLines() { return _lines; }
        public vtkCellArray GetPolys() { return _polys; }
        public vtkCellArray GetStrips() { return _strips; }
        public void SetVerts(vtkCellArray ca) { _verts = ca ?? new vtkCellArray(); _cellMap = null; Modified(); }
        public void SetLines(vtkCellArray ca) { _lines = ca ?? new vtkCellArray(); _cellMap = null; Modified(); }
        public void SetPolys(vtkCellArray ca) { _polys = ca ?? new vtkCellArray(); _cellMap = null; Modified(); }
        public void SetStrips(vtkCellArray ca) { _strips = ca ?? new vtkCellArray(); _cellMap = null; Modified(); }
        public long GetNumberOfVerts() { return _verts.Count; }
        public long GetNumberOfLines() { return _lines.Count; }
        public long GetNumberOfPolys() { return _polys.Count; }
        public long GetNumberOfStrips() { return _strips.Count; }
        public override long GetNumberOfCells() { return _verts.Count + _lines.Count + _polys.Count + _strips.Count; }
        public void Allocate(long numCells, int extSize) { }
        public void Allocate(vtkDataSet inData, long numCells, int extSize) { }
        public void BuildCells() { }
        public void BuildLinks() { BuildLinks(0); }
        public void BuildLinks(int initialSize) { _links = null; EnsureLinks(); }
        public void DeleteCells() { _cellMap = null; }
        public void DeleteLinks() { _links = null; }
        public void RemoveDeletedCells() { }
        public override void Squeeze() { }

        internal vtkCellArray Array(int kind)
        {
            switch (kind)
            {
                case 0: return _verts;
                case 1: return _lines;
                case 2: return _polys;
                default: return _strips;
            }
        }
        private static int KindOfType(int type)
        {
            switch (type)
            {
                case CellType.Vertex: case CellType.PolyVertex: return 0;
                case CellType.Line: case CellType.PolyLine: return 1;
                case CellType.TriangleStrip: return 3;
                default: return 2;
            }
        }
        private static byte DefaultType(int kind, int size)
        {
            switch (kind)
            {
                case 0: return (byte)(size == 1 ? CellType.Vertex : CellType.PolyVertex);
                case 1: return (byte)(size == 2 ? CellType.Line : CellType.PolyLine);
                case 2: return (byte)(size == 3 ? CellType.Triangle : size == 4 ? CellType.Quad : CellType.Polygon);
                default: return (byte)CellType.TriangleStrip;
            }
        }
        private void EnsureCellMap()
        {
            if (_cellMap != null) return;
            _cellMap = new List<(byte, int, byte)>((int)GetNumberOfCells());
            for (int kind = 0; kind < 4; kind++)
            {
                vtkCellArray ca = Array(kind);
                for (int i = 0; i < ca.Count; i++) _cellMap.Add(((byte)kind, i, DefaultType(kind, ca.CellSize(i))));
            }
        }
        // The cells in id order; used by the renderer and the filters
        internal bool HasInsertionOrder { get { return _cellMap != null; } }
        internal void Locate(long cellId, out vtkCellArray array, out int index, out int type)
        {
            if (_cellMap == null)
            {
                long id = cellId;
                for (int kind = 0; kind < 4; kind++)
                {
                    vtkCellArray ca = Array(kind);
                    if (id < ca.Count)
                    {
                        array = ca;
                        index = (int)id;
                        type = DefaultType(kind, ca.CellSize(index));
                        return;
                    }
                    id -= ca.Count;
                }
                throw new IndexOutOfRangeException("Cell " + cellId + " does not exist.");
            }
            var entry = _cellMap[(int)cellId];
            array = Array(entry.kind);
            index = entry.index;
            type = entry.type;
        }
        // The cell id of the cell at index of the cell array of the kind
        internal long[] CellIdsOfKind(int kind)
        {
            vtkCellArray ca = Array(kind);
            long[] ids = new long[ca.Count];
            if (_cellMap == null)
            {
                long offset = 0;
                for (int k = 0; k < kind; k++) offset += Array(k).Count;
                for (int i = 0; i < ids.Length; i++) ids[i] = offset + i;
            }
            else
            {
                for (int c = 0; c < _cellMap.Count; c++) if (_cellMap[c].kind == kind) ids[_cellMap[c].index] = c;
            }
            return ids;
        }
        public long InsertNextCell(int type, vtkIdList pts)
        {
            return InsertNextCell(type, pts.ToArray());
        }
        public long InsertNextCell(int type, long npts, IntPtr pts)
        {
            long[] ids = new long[npts];
            if (npts > 0) Marshal.Copy(pts, ids, 0, (int)npts);
            return InsertNextCell(type, ids);
        }
        public long InsertNextCell(int type, long npts, long[] pts)
        {
            long[] ids = new long[npts];
            System.Array.Copy(pts, ids, npts);
            return InsertNextCell(type, ids);
        }
        internal long InsertNextCell(int type, long[] ids)
        {
            EnsureCellMap();
            int kind = KindOfType(type);
            int index = (int)Array(kind).InsertNextCell(ids);
            _cellMap.Add(((byte)kind, index, (byte)type));
            Modified();
            return _cellMap.Count - 1;
        }
        public override int GetCellType(long cellId)
        {
            Locate(cellId, out _, out _, out int type);
            return type;
        }
        internal override long[] GetCellPointIds(long cellId)
        {
            Locate(cellId, out vtkCellArray array, out int index, out _);
            return array.GetCell(index);
        }
        public override void GetCellPoints(long cellId, vtkIdList ptIds)
        {
            Locate(cellId, out vtkCellArray array, out int index, out _);
            array.GetCell(index, ptIds);
        }
        public void GetPointCells(long ptId, vtkIdList cellIds)
        {
            EnsureLinks();
            cellIds.Reset();
            if (ptId < 0 || ptId >= _links.Length || _links[ptId] == null) return;
            foreach (int c in _links[ptId]) cellIds.InsertNextId(c);
        }
        public void GetCellNeighbors(long cellId, vtkIdList ptIds, vtkIdList cellIds)
        {
            EnsureLinks();
            cellIds.Reset();
            long n = ptIds.GetNumberOfIds();
            if (n == 0) return;
            List<int> candidates = _links[ptIds.GetId(0)] ?? new List<int>();
            foreach (int c in candidates)
            {
                if (c == cellId) continue;
                long[] cellPts = GetCellPointIds(c);
                bool all = true;
                for (long i = 1; i < n && all; i++) all = System.Array.IndexOf(cellPts, ptIds.GetId(i)) >= 0;
                if (all) cellIds.InsertNextId(c);
            }
        }
        private void EnsureLinks()
        {
            if (_links != null && _linksTime == DataTime) return;
            int numPoints = (int)GetNumberOfPoints();
            _links = new List<int>[numPoints];
            long numCells = GetNumberOfCells();
            for (long c = 0; c < numCells; c++)
            {
                foreach (long p in GetCellPointIds(c))
                {
                    if (p < 0 || p >= numPoints) continue;
                    (_links[p] ?? (_links[p] = new List<int>())).Add((int)c);
                }
            }
            _linksTime = DataTime;
        }
        public void ReverseCell(long cellId)
        {
            Locate(cellId, out vtkCellArray array, out int index, out _);
            array.ReverseCell(index);
            Modified();
        }
        public void ReplaceCell(long cellId, int npts, long[] pts)
        {
            Locate(cellId, out vtkCellArray array, out int index, out _);
            long start = array.CellStart(index);
            for (int i = 0; i < npts && i < array.CellSize(index); i++) array.Connectivity[(int)(start + i)] = pts[i];
            Modified();
        }
        // The bounds are asked for by every camera step; they are computed again only after the points or cells changed
        private ulong _boundsTime = ulong.MaxValue;
        private double[] _bounds;
        protected override double[] UsedPointBounds()
        {
            ulong time = DataTime;
            if (_bounds == null || _boundsTime != time)
            {
                _bounds = ComputeUsedPointBounds();
                _boundsTime = time;
            }
            return (double[])_bounds.Clone();
        }
        private double[] ComputeUsedPointBounds()
        {
            if (GetNumberOfCells() == 0) return _points.GetBounds();
            double[] b = new double[] { double.MaxValue, -double.MaxValue, double.MaxValue, -double.MaxValue,
                                        double.MaxValue, -double.MaxValue };
            double[] d = _points.Raw;
            long n = _points.GetNumberOfPoints();
            for (int kind = 0; kind < 4; kind++)
            {
                foreach (long p in Array(kind).Connectivity)
                {
                    if (p < 0 || p >= n) continue;
                    long k = 3 * p;
                    for (int c = 0; c < 3; c++)
                    {
                        double v = d[k + c];
                        if (v < b[2 * c]) b[2 * c] = v;
                        if (v > b[2 * c + 1]) b[2 * c + 1] = v;
                    }
                }
            }
            if (b[0] > b[1]) return new double[] { 1, -1, 1, -1, 1, -1 };
            return b;
        }
        public override uint GetMTime()
        {
            uint t = base.GetMTime();
            t = Math.Max(t, Math.Max(_verts.GetMTime(), _lines.GetMTime()));
            t = Math.Max(t, Math.Max(_polys.GetMTime(), _strips.GetMTime()));
            return t;
        }
        internal override ulong DataTime
        {
            get
            {
                ulong t = base.DataTime;
                t = Math.Max(t, Math.Max(_verts.MTime, _lines.MTime));
                t = Math.Max(t, Math.Max(_polys.MTime, _strips.MTime));
                return t;
            }
        }
        public override void Initialize()
        {
            _points = null;
            _verts = new vtkCellArray();
            _lines = new vtkCellArray();
            _polys = new vtkCellArray();
            _strips = new vtkCellArray();
            _cellMap = null;
            _links = null;
            _pointData.Initialize();
            _cellData.Initialize();
            Modified();
        }
        public override void ShallowCopy(vtkDataObject src)
        {
            if (!(src is vtkDataSet ds)) return;
            if (ds is vtkPointSet ps) _points = ps.GetPoints();
            _pointData.ShallowCopy(ds.GetPointData());
            _cellData.ShallowCopy(ds.GetCellData());
            if (src is vtkPolyData pd)
            {
                _verts = pd._verts; _lines = pd._lines; _polys = pd._polys; _strips = pd._strips;
                _cellMap = pd._cellMap;
            }
            Modified();
        }
        public override void DeepCopy(vtkDataObject src)
        {
            if (!(src is vtkDataSet ds)) return;
            if (ds is vtkPointSet ps && ps.GetPoints() != null)
            {
                _points = new vtkPoints();
                _points.DeepCopy(ps.GetPoints());
            }
            _pointData.DeepCopy(ds.GetPointData());
            _cellData.DeepCopy(ds.GetCellData());
            if (src is vtkPolyData pd)
            {
                _verts = new vtkCellArray(); _verts.DeepCopy(pd._verts);
                _lines = new vtkCellArray(); _lines.DeepCopy(pd._lines);
                _polys = new vtkCellArray(); _polys.DeepCopy(pd._polys);
                _strips = new vtkCellArray(); _strips.DeepCopy(pd._strips);
                _cellMap = pd._cellMap != null ? new List<(byte, int, byte)>(pd._cellMap) : null;
            }
            _links = null;
            Modified();
        }
        public override uint GetActualMemorySize()
        {
            return base.GetActualMemorySize() + _verts.GetActualMemorySize() + _lines.GetActualMemorySize() +
                   _polys.GetActualMemorySize() + _strips.GetActualMemorySize();
        }
        public override int GetDataObjectType() { return 0; }
        public static vtkPolyData SafeDownCast(vtkObjectBase o) { return o as vtkPolyData; }
    }

    // Unstructured grid: cells of any type in the order of insertion
    public partial class vtkUnstructuredGrid
    {
        private List<byte> _types = new List<byte>();
        private vtkCellArray _cells = new vtkCellArray();

        internal List<byte> Types { get { return _types; } }
        internal vtkCellArray Cells { get { return _cells; } }

        public void Allocate(long numCells, int extSize) { }
        public void Allocate(long numCells) { }
        public long InsertNextCell(int type, vtkIdList ptIds)
        {
            _types.Add((byte)type);
            _cells.InsertNextCell(ptIds);
            Modified();
            return _types.Count - 1;
        }
        public long InsertNextCell(int type, long npts, long[] ptIds)
        {
            long[] ids = new long[npts];
            System.Array.Copy(ptIds, ids, npts);
            _types.Add((byte)type);
            _cells.InsertNextCell(ids);
            Modified();
            return _types.Count - 1;
        }
        internal long InsertNextCell(int type, long[] ids)
        {
            _types.Add((byte)type);
            _cells.InsertNextCell(ids);
            return _types.Count - 1;
        }
        public void SetCells(int type, vtkCellArray cells)
        {
            _cells = cells;
            _types = Enumerable.Repeat((byte)type, cells.Count).ToList();
            Modified();
        }
        public void SetCells(vtkUnsignedCharArray types, vtkIdTypeArray locations, vtkCellArray cells)
        {
            _cells = cells;
            _types = new List<byte>();
            for (long i = 0; i < types.GetNumberOfTuples(); i++) _types.Add(types.GetValue(i));
            Modified();
        }
        public void SetCells(int[] types, vtkCellArray cells)
        {
            _cells = cells;
            _types = types.Select(t => (byte)t).ToList();
            Modified();
        }
        public vtkCellArray GetCells() { return _cells; }
        public vtkUnsignedCharArray GetCellTypesArray()
        {
            vtkUnsignedCharArray types = new vtkUnsignedCharArray();
            types.SetNumberOfValues(_types.Count);
            for (int i = 0; i < _types.Count; i++) types.SetValue(i, _types[i]);
            return types;
        }
        public override long GetNumberOfCells() { return _types.Count; }
        public override int GetCellType(long cellId) { return _types[(int)cellId]; }
        internal override long[] GetCellPointIds(long cellId) { return _cells.GetCell((int)cellId); }
        public override void GetCellPoints(long cellId, vtkIdList ptIds) { _cells.GetCell((int)cellId, ptIds); }
        public void GetPointCells(long ptId, vtkIdList cellIds)
        {
            cellIds.Reset();
            for (int c = 0; c < _types.Count; c++)
                if (System.Array.IndexOf(_cells.GetCell(c), ptId) >= 0) cellIds.InsertNextId(c);
        }
        public void BuildLinks() { }
        public void Update() { }
        public override void Squeeze() { }
        public override uint GetMTime() { return Math.Max(base.GetMTime(), _cells.GetMTime()); }
        internal override ulong DataTime { get { return Math.Max(base.DataTime, _cells.MTime); } }
        public override void Initialize()
        {
            _points = null;
            _types = new List<byte>();
            _cells = new vtkCellArray();
            _pointData.Initialize();
            _cellData.Initialize();
            Modified();
        }
        public override void ShallowCopy(vtkDataObject src)
        {
            if (!(src is vtkDataSet ds)) return;
            if (ds is vtkPointSet ps) _points = ps.GetPoints();
            _pointData.ShallowCopy(ds.GetPointData());
            _cellData.ShallowCopy(ds.GetCellData());
            if (src is vtkUnstructuredGrid ug)
            {
                _types = ug._types;
                _cells = ug._cells;
            }
            else
            {
                _types = new List<byte>();
                _cells = new vtkCellArray();
                long n = ds.GetNumberOfCells();
                for (long i = 0; i < n; i++) InsertNextCell(ds.GetCellType(i), ds.GetCellPointIds(i));
            }
            Modified();
        }
        public override void DeepCopy(vtkDataObject src)
        {
            if (!(src is vtkDataSet ds)) return;
            if (ds is vtkPointSet ps && ps.GetPoints() != null)
            {
                _points = new vtkPoints();
                _points.DeepCopy(ps.GetPoints());
            }
            _pointData.DeepCopy(ds.GetPointData());
            _cellData.DeepCopy(ds.GetCellData());
            _types = new List<byte>();
            _cells = new vtkCellArray();
            long n = ds.GetNumberOfCells();
            for (long i = 0; i < n; i++) InsertNextCell(ds.GetCellType(i), ds.GetCellPointIds(i));
            Modified();
        }
        public override uint GetActualMemorySize() { return base.GetActualMemorySize() + _cells.GetActualMemorySize(); }
        public override int GetDataObjectType() { return 4; }
        public static vtkUnstructuredGrid SafeDownCast(vtkObjectBase o) { return o as vtkUnstructuredGrid; }
    }

    // Image data: the pixels of a rendered image
    public partial class vtkImageData
    {
        private int[] _dimensions = new int[] { 0, 0, 1 };

        public void SetDimensions(int x, int y, int z) { _dimensions = new int[] { x, y, z }; }
        public int[] GetDimensions() { return (int[])_dimensions.Clone(); }
        public void SetScalarTypeToUnsignedChar() { }
        public void SetNumberOfScalarComponents(int n) { }
        public void AllocateScalars() { }
        public override long GetNumberOfPoints() { return (long)_dimensions[0] * _dimensions[1] * _dimensions[2]; }
        // The pixels: RGB or RGBA bytes, bottom row first
        internal byte[] Pixels;
        internal int Components;
        public override double[] GetBounds()
        {
            return new double[] { 0, _dimensions[0] - 1, 0, _dimensions[1] - 1, 0, _dimensions[2] - 1 };
        }
    }

    // Selection
    public partial class vtkSelection
    {
        private readonly List<vtkSelectionNode> _nodes = new List<vtkSelectionNode>();
        public uint GetNumberOfNodes() { return (uint)_nodes.Count; }
        public vtkSelectionNode GetNode(uint i) { return _nodes[(int)i]; }
        public void AddNode(vtkSelectionNode node) { _nodes.Add(node); }
    }

    public partial class vtkSelectionNode
    {
        private vtkDataSetAttributes _selectionData = new vtkDataSetAttributes();
        public vtkDataSetAttributes GetSelectionData() { return _selectionData; }
        public vtkAbstractArray GetSelectionList() { return _selectionData.GetAbstractArray(0); }
        public void SetSelectionList(vtkAbstractArray list) { _selectionData.Initialize(); _selectionData.AddArray(list); }
        private int _fieldType, _contentType;
        public void SetFieldType(int type) { _fieldType = type; }
        public int GetFieldType() { return _fieldType; }
        public void SetContentType(int type) { _contentType = type; }
        public int GetContentType() { return _contentType; }
        public bool EqualProperties(vtkSelectionNode other, bool fullCompare)
        {
            return other != null && other._fieldType == _fieldType && other._contentType == _contentType;
        }
    }

    // Cells
    public partial class vtkCell
    {
        protected int _type;
        protected vtkIdList _pointIds = new vtkIdList();
        protected vtkPoints _points = new vtkPoints();

        internal void Initialize(int type, long[] ids, vtkPoints points)
        {
            _type = type;
            _pointIds.SetIds(ids);
            _points = points;
        }
        public virtual int GetCellType() { return _type; }
        public int GetCellDimension() { return CellType.Dimension(_type); }
        public long GetNumberOfPoints() { return _pointIds.GetNumberOfIds(); }
        public vtkIdList GetPointIds() { return _pointIds; }
        public long GetPointId(int i) { return _pointIds.GetId(i); }
        public vtkPoints GetPoints() { return _points; }
        public int IsLinear() { return CellType.IsQuadratic(_type) ? 0 : 1; }
        public double[] GetBounds() { return _points.GetBounds(); }
        public int GetNumberOfEdges() { return CellType.Edges(_type, (int)GetNumberOfPoints()).Length; }
        public int GetNumberOfFaces() { return CellType.Faces(_type).Length; }
        public vtkCell GetEdge(int edgeId)
        {
            int[] e = CellType.Edges(_type, (int)GetNumberOfPoints())[edgeId];
            int type = e.Length == 3 ? CellType.QuadraticEdge : CellType.Line;
            int[] order = e.Length == 3 ? new[] { e[0], e[1], e[2] } : e;
            return SubCell(type, order);
        }
        public vtkCell GetFace(int faceId)
        {
            int[] f = CellType.Faces(_type)[faceId];
            return SubCell(CellType.FaceType(f.Length, CellType.IsQuadratic(_type)), f);
        }
        private vtkCell SubCell(int type, int[] local)
        {
            vtkGenericCell cell = new vtkGenericCell();
            long[] ids = new long[local.Length];
            vtkPoints points = new vtkPoints();
            points.SetDataTypeToDouble();
            for (int i = 0; i < local.Length; i++)
            {
                ids[i] = _pointIds.GetId(local[i]);
                points.InsertNextPoint(_points.GetPoint(local[i]));
            }
            cell.Initialize(type, ids, points);
            return cell;
        }
        // The closest point on the cell and the interpolation weights of the cell points at the point; returns 1 when
        // the point is inside the cell
        public int EvaluatePosition(IntPtr x, IntPtr closestPoint, ref int subId, IntPtr pcoords, ref double dist2,
                                    IntPtr weights)
        {
            double[] p = new double[3];
            Marshal.Copy(x, p, 0, 3);
            int inside = CellGeometry.EvaluatePosition(_type, _points, p, out double[] closest, out double d2,
                                                       out double[] w);
            dist2 = d2;
            subId = 0;
            if (closestPoint != IntPtr.Zero) Marshal.Copy(closest, 0, closestPoint, 3);
            if (pcoords != IntPtr.Zero) Marshal.Copy(new double[3], 0, pcoords, 3);
            if (weights != IntPtr.Zero) Marshal.Copy(w, 0, weights, w.Length);
            return inside;
        }
        public void ShallowCopy(vtkCell cell)
        {
            _type = cell._type;
            _pointIds.DeepCopy(cell._pointIds);
            _points = cell._points;
        }
        public void DeepCopy(vtkCell cell)
        {
            _type = cell._type;
            _pointIds.DeepCopy(cell._pointIds);
            _points = new vtkPoints();
            _points.DeepCopy(cell._points);
        }
    }

    public partial class vtkGenericCell
    {
        public void SetCellType(int type) { _type = type; }
    }
    public partial class vtkTriangle
    {
        public vtkTriangle() { _type = CellType.Triangle; _pointIds.SetNumberOfIds(3); _points.SetNumberOfPoints(3); }
        public static void ComputeNormal(vtkPoints p, int numPts, long[] pts, double[] n)
        {
            double[] a = p.GetPoint(pts[0]), b = p.GetPoint(pts[1]), c = p.GetPoint(pts[2]);
            ComputeNormal(a, b, c, n);
        }
        public static void ComputeNormal(double[] a, double[] b, double[] c, double[] n)
        {
            double[] u = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double[] v = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
            vtkMath.Cross(u, v, n);
            vtkMath.Normalize(n);
        }
        public static double TriangleArea(double[] a, double[] b, double[] c)
        {
            double[] u = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double[] v = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
            return 0.5 * vtkMath.Norm(vtkMath.Cross(u, v));
        }
    }
    public partial class vtkLine
    {
        public vtkLine() { _type = CellType.Line; _pointIds.SetNumberOfIds(2); _points.SetNumberOfPoints(2); }
    }
    public partial class vtkQuad
    {
        public vtkQuad() { _type = CellType.Quad; _pointIds.SetNumberOfIds(4); _points.SetNumberOfPoints(4); }
    }
    public partial class vtkHexahedron
    {
        public vtkHexahedron() { _type = CellType.Hexahedron; _pointIds.SetNumberOfIds(8); _points.SetNumberOfPoints(8); }
    }
    public partial class vtkPolyLine
    {
        public vtkPolyLine() { _type = CellType.PolyLine; }
    }

    public partial class vtkCellTypes
    {
        private readonly List<byte> _types = new List<byte>();
        public int GetNumberOfTypes() { return _types.Count; }
        public byte GetCellType(int i) { return _types[i]; }
        public int IsType(byte type) { return _types.Contains(type) ? 1 : 0; }
        public long InsertNextType(byte type) { _types.Add(type); return _types.Count - 1; }
        public void Reset() { _types.Clear(); }
    }

    // Geometry of the cells: closest points, interpolation weights, inside tests
    internal static class CellGeometry
    {
        public static double[] Sub(double[] a, double[] b) { return new[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
        public static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }

        // The closest point on a triangle with its barycentric coordinates (Ericson, Real-Time Collision Detection)
        public static double[] ClosestPointOnTriangle(double[] p, double[] a, double[] b, double[] c, out double u,
                                                      out double v, out double w)
        {
            double[] ab = Sub(b, a), ac = Sub(c, a), ap = Sub(p, a);
            double d1 = Dot(ab, ap), d2 = Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) { u = 1; v = 0; w = 0; return (double[])a.Clone(); }
            double[] bp = Sub(p, b);
            double d3 = Dot(ab, bp), d4 = Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) { u = 0; v = 1; w = 0; return (double[])b.Clone(); }
            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                double t = d1 / (d1 - d3);
                u = 1 - t; v = t; w = 0;
                return new[] { a[0] + t * ab[0], a[1] + t * ab[1], a[2] + t * ab[2] };
            }
            double[] cp = Sub(p, c);
            double d5 = Dot(ab, cp), d6 = Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) { u = 0; v = 0; w = 1; return (double[])c.Clone(); }
            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                double t = d2 / (d2 - d6);
                u = 1 - t; v = 0; w = t;
                return new[] { a[0] + t * ac[0], a[1] + t * ac[1], a[2] + t * ac[2] };
            }
            double va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                double t = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                u = 0; v = 1 - t; w = t;
                return new[] { b[0] + t * (c[0] - b[0]), b[1] + t * (c[1] - b[1]), b[2] + t * (c[2] - b[2]) };
            }
            double denom = 1 / (va + vb + vc);
            v = vb * denom;
            w = vc * denom;
            u = 1 - v - w;
            return new[] { a[0] + ab[0] * v + ac[0] * w, a[1] + ab[1] * v + ac[1] * w, a[2] + ab[2] * v + ac[2] * w };
        }
        public static double[] ClosestPointOnSegment(double[] p, double[] a, double[] b, out double t)
        {
            double[] ab = Sub(b, a);
            double l2 = Dot(ab, ab);
            t = l2 > 0 ? Dot(Sub(p, a), ab) / l2 : 0;
            if (t < 0) t = 0;
            if (t > 1) t = 1;
            return new[] { a[0] + t * ab[0], a[1] + t * ab[1], a[2] + t * ab[2] };
        }
        public static double Dist2(double[] a, double[] b)
        {
            double x = a[0] - b[0], y = a[1] - b[1], z = a[2] - b[2];
            return x * x + y * y + z * z;
        }
        // Barycentric coordinates of p in the tetrahedron a, b, c, d
        public static double[] TetraWeights(double[] p, double[] a, double[] b, double[] c, double[] d)
        {
            double[] m = { b[0] - a[0], c[0] - a[0], d[0] - a[0],
                           b[1] - a[1], c[1] - a[1], d[1] - a[1],
                           b[2] - a[2], c[2] - a[2], d[2] - a[2] };
            double det = m[0] * (m[4] * m[8] - m[5] * m[7]) - m[1] * (m[3] * m[8] - m[5] * m[6]) + m[2] * (m[3] * m[7] - m[4] * m[6]);
            if (Math.Abs(det) < 1e-300) return null;
            double[] r = Sub(p, a);
            double l1 = (r[0] * (m[4] * m[8] - m[5] * m[7]) - m[1] * (r[1] * m[8] - m[5] * r[2]) + m[2] * (r[1] * m[7] - m[4] * r[2])) / det;
            double l2 = (m[0] * (r[1] * m[8] - m[5] * r[2]) - r[0] * (m[3] * m[8] - m[5] * m[6]) + m[2] * (m[3] * r[2] - r[1] * m[6])) / det;
            double l3 = (m[0] * (m[4] * r[2] - r[1] * m[7]) - m[1] * (m[3] * r[2] - r[1] * m[6]) + r[0] * (m[3] * m[7] - m[4] * m[6])) / det;
            return new[] { 1 - l1 - l2 - l3, l1, l2, l3 };
        }
        // The corner points of the linear cell a cell is based on
        private static int[] Corners(int type)
        {
            switch (type)
            {
                case CellType.QuadraticEdge: return new[] { 0, 1 };
                case CellType.QuadraticTriangle: return new[] { 0, 1, 2 };
                case CellType.QuadraticQuad: return new[] { 0, 1, 2, 3 };
                case CellType.QuadraticTetra: return new[] { 0, 1, 2, 3 };
                case CellType.QuadraticWedge: return new[] { 0, 1, 2, 3, 4, 5 };
                case CellType.QuadraticHexahedron: return new[] { 0, 1, 2, 3, 4, 5, 6, 7 };
                case CellType.QuadraticPyramid: return new[] { 0, 1, 2, 3, 4 };
                default: return null;
            }
        }
        private static int LinearType(int type)
        {
            switch (type)
            {
                case CellType.QuadraticEdge: return CellType.Line;
                case CellType.QuadraticTriangle: return CellType.Triangle;
                case CellType.QuadraticQuad: return CellType.Quad;
                case CellType.QuadraticTetra: return CellType.Tetra;
                case CellType.QuadraticWedge: return CellType.Wedge;
                case CellType.QuadraticHexahedron: return CellType.Hexahedron;
                case CellType.QuadraticPyramid: return CellType.Pyramid;
                default: return type;
            }
        }
        // Decomposition of the linear 3D cells into tetrahedra
        private static int[][] Tetras(int type)
        {
            switch (type)
            {
                case CellType.Tetra: return new[] { new[] { 0, 1, 2, 3 } };
                case CellType.Wedge: return new[] { new[] { 0, 1, 2, 3 }, new[] { 1, 2, 3, 4 }, new[] { 2, 3, 4, 5 } };
                case CellType.Pyramid: return new[] { new[] { 0, 1, 2, 4 }, new[] { 0, 2, 3, 4 } };
                case CellType.Hexahedron:
                    return new[] { new[] { 0, 1, 3, 4 }, new[] { 1, 2, 3, 6 }, new[] { 1, 4, 5, 6 }, new[] { 3, 4, 6, 7 },
                                   new[] { 1, 3, 4, 6 } };
                case CellType.Voxel:
                    return new[] { new[] { 0, 1, 2, 4 }, new[] { 1, 3, 2, 7 }, new[] { 1, 4, 5, 7 }, new[] { 2, 4, 7, 6 },
                                   new[] { 1, 2, 4, 7 } };
                default: return null;
            }
        }
        public static int EvaluatePosition(int type, vtkPoints points, double[] p, out double[] closest, out double dist2,
                                           out double[] weights)
        {
            int n = (int)points.GetNumberOfPoints();
            weights = new double[n];
            int[] corners = Corners(type);
            int linear = LinearType(type);
            Func<int, double[]> P = i => points.GetPoint(corners != null ? corners[i] : i);
            int cornerCount = corners != null ? corners.Length : n;
            double[] w = new double[cornerCount];
            int inside = 0;
            int dim = CellType.Dimension(linear);
            closest = (double[])p.Clone();
            dist2 = 0;
            if (dim == 0)
            {
                closest = P(0);
                dist2 = Dist2(p, closest);
                w[0] = 1;
                inside = dist2 == 0 ? 1 : 0;
            }
            else if (dim == 1)
            {
                double best = double.MaxValue;
                for (int i = 0; i + 1 < cornerCount; i++)
                {
                    double[] c = ClosestPointOnSegment(p, P(i), P(i + 1), out double t);
                    double d = Dist2(p, c);
                    if (d < best)
                    {
                        best = d;
                        closest = c;
                        Array.Clear(w, 0, w.Length);
                        w[i] = 1 - t;
                        w[i + 1] = t;
                    }
                }
                dist2 = best;
                inside = best < 1e-24 ? 1 : 0;
            }
            else if (dim == 2)
            {
                int[][] tris = CellType.Triangulate(linear, cornerCount, P);
                double best = double.MaxValue;
                foreach (int[] t in tris)
                {
                    double[] c = ClosestPointOnTriangle(p, P(t[0]), P(t[1]), P(t[2]), out double u, out double v, out double ww);
                    double d = Dist2(p, c);
                    if (d < best)
                    {
                        best = d;
                        closest = c;
                        Array.Clear(w, 0, w.Length);
                        w[t[0]] += u;
                        w[t[1]] += v;
                        w[t[2]] += ww;
                    }
                }
                dist2 = best;
                inside = best < 1e-24 ? 1 : 0;
            }
            else
            {
                int[][] tets = Tetras(linear);
                if (tets != null)
                {
                    foreach (int[] t in tets)
                    {
                        double[] l = TetraWeights(p, P(t[0]), P(t[1]), P(t[2]), P(t[3]));
                        if (l == null) continue;
                        if (l.All(x => x >= -1e-9))
                        {
                            for (int k = 0; k < 4; k++) w[t[k]] += l[k];
                            inside = 1;
                            break;
                        }
                    }
                }
                if (inside == 0)
                {
                    // Outside: the closest point on the faces
                    double best = double.MaxValue;
                    foreach (int[] face in CellType.Faces(linear))
                    {
                        int[][] tris = CellType.Triangulate(face.Length == 3 ? CellType.Triangle : CellType.Quad, face.Length, null);
                        foreach (int[] t in tris)
                        {
                            double[] c = ClosestPointOnTriangle(p, P(face[t[0]]), P(face[t[1]]), P(face[t[2]]),
                                                                out double u, out double v, out double ww);
                            double d = Dist2(p, c);
                            if (d < best)
                            {
                                best = d;
                                closest = c;
                                Array.Clear(w, 0, w.Length);
                                w[face[t[0]]] += u;
                                w[face[t[1]]] += v;
                                w[face[t[2]]] += ww;
                            }
                        }
                    }
                    dist2 = best;
                }
            }
            for (int i = 0; i < cornerCount; i++) weights[corners != null ? corners[i] : i] = w[i];
            return inside;
        }
    }

    // Implicit functions
    public partial class vtkImplicitFunction
    {
        protected vtkAbstractTransform _transform;

        public void SetTransform(vtkAbstractTransform transform) { _transform = transform; Modified(); }
        public void SetTransform(double[] elements) { vtkTransform t = new vtkTransform(); t.SetMatrix(elements); _transform = t; }
        public vtkAbstractTransform GetTransform() { return _transform; }
        public double FunctionValue(double[] x) { return EvaluateFunction(x); }
        public double FunctionValue(double x, double y, double z) { return EvaluateFunction(new[] { x, y, z }); }
        public double EvaluateFunction(double x, double y, double z) { return EvaluateFunction(new[] { x, y, z }); }
        public double EvaluateFunction(double[] x)
        {
            double[] p = _transform != null ? _transform.TransformPoint(x) : x;
            return Evaluate(p);
        }
        protected virtual double Evaluate(double[] x) { return 0; }
    }

    public partial class vtkPlane
    {
        private double[] _origin = new double[3];
        private double[] _normal = new double[] { 0, 0, 1 };

        public double[] GetOrigin() { return (double[])_origin.Clone(); }
        public double[] GetNormal() { return (double[])_normal.Clone(); }
        public void SetOrigin(double x, double y, double z) { _origin = new double[] { x, y, z }; Modified(); }
        public void SetOrigin(double[] o) { SetOrigin(o[0], o[1], o[2]); }
        public void SetNormal(double x, double y, double z)
        {
            // VTK keeps the normal as given; the evaluation normalizes it
            _normal = new double[] { x, y, z };
            Modified();
        }
        public void SetNormal(double[] n) { SetNormal(n[0], n[1], n[2]); }
        internal double[] UnitNormal
        {
            get
            {
                double[] n = (double[])_normal.Clone();
                vtkMath.Normalize(n);
                return n;
            }
        }
        protected override double Evaluate(double[] x)
        {
            return _normal[0] * (x[0] - _origin[0]) + _normal[1] * (x[1] - _origin[1]) + _normal[2] * (x[2] - _origin[2]);
        }
        public void ProjectPoint(double[] x, double[] xproj)
        {
            double[] n = UnitNormal;
            double d = n[0] * (x[0] - _origin[0]) + n[1] * (x[1] - _origin[1]) + n[2] * (x[2] - _origin[2]);
            xproj[0] = x[0] - d * n[0];
            xproj[1] = x[1] - d * n[1];
            xproj[2] = x[2] - d * n[2];
        }
        public double DistanceToPlane(double[] x)
        {
            double[] n = UnitNormal;
            return Math.Abs(n[0] * (x[0] - _origin[0]) + n[1] * (x[1] - _origin[1]) + n[2] * (x[2] - _origin[2]));
        }
        public void Push(double distance)
        {
            double[] n = UnitNormal;
            for (int i = 0; i < 3; i++) _origin[i] += distance * n[i];
            Modified();
        }
    }

    public partial class vtkPlanes
    {
        private vtkPoints _points;
        private vtkDataArray _normals;

        public void SetPoints(vtkPoints points) { _points = points; Modified(); }
        public vtkPoints GetPoints() { return _points; }
        public void SetNormals(vtkDataArray normals) { _normals = normals; Modified(); }
        public vtkDataArray GetNormals() { return _normals; }
        public int GetNumberOfPlanes() { return _points != null ? (int)_points.GetNumberOfPoints() : 0; }
        public vtkPlane GetPlane(int i)
        {
            vtkPlane plane = new vtkPlane();
            plane.SetOrigin(_points.GetPoint(i));
            plane.SetNormal(_normals.GetTuple3(i));
            return plane;
        }
        public void GetPlane(int i, vtkPlane plane)
        {
            plane.SetOrigin(_points.GetPoint(i));
            plane.SetNormal(_normals.GetTuple3(i));
        }
        // Six planes from the a, b, c, d coefficients of each plane: left, right, bottom, top, near, far
        public void SetFrustumPlanes(double[] planes)
        {
            _points = new vtkPoints();
            _points.SetDataTypeToDouble();
            _normals = new vtkDoubleArray();
            _normals.SetNumberOfComponents(3);
            for (int i = 0; i < 6; i++)
            {
                double a = planes[4 * i], b = planes[4 * i + 1], c = planes[4 * i + 2], d = planes[4 * i + 3];
                double l = Math.Sqrt(a * a + b * b + c * c);
                if (l == 0) l = 1;
                // VTK frustum plane normals point out of the frustum
                double[] n = { -a / l, -b / l, -c / l };
                double[] o = { -a * d / (l * l), -b * d / (l * l), -c * d / (l * l) };
                _points.InsertNextPoint(o);
                _normals.InsertNextTuple3(n[0], n[1], n[2]);
            }
            Modified();
        }
        protected override double Evaluate(double[] x)
        {
            double max = -double.MaxValue;
            for (int i = 0; i < GetNumberOfPlanes(); i++)
            {
                double[] o = _points.GetPoint(i);
                double[] n = _normals.GetTuple3(i);
                double v = n[0] * (x[0] - o[0]) + n[1] * (x[1] - o[1]) + n[2] * (x[2] - o[2]);
                if (v > max) max = v;
            }
            return max;
        }
    }

    public partial class vtkCylinder
    {
        private double _radius = 0.5;
        private double[] _center = new double[3];
        public void SetRadius(double r) { _radius = r; }
        public void SetCenter(double x, double y, double z) { _center = new[] { x, y, z }; }
        // The axis is y
        protected override double Evaluate(double[] x)
        {
            double dx = x[0] - _center[0], dz = x[2] - _center[2];
            return dx * dx + dz * dz - _radius * _radius;
        }
    }

    public partial class vtkBox
    {
        private double[] _bounds = new double[] { 0, 1, 0, 1, 0, 1 };
        public void SetBounds(double x0, double x1, double y0, double y1, double z0, double z1)
        {
            _bounds = new[] { x0, x1, y0, y1, z0, z1 };
        }
        public void SetBounds(double[] b) { _bounds = (double[])b.Clone(); }
        protected override double Evaluate(double[] x)
        {
            double max = -double.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                max = Math.Max(max, _bounds[2 * i] - x[i]);
                max = Math.Max(max, x[i] - _bounds[2 * i + 1]);
            }
            return max;
        }
    }

    public partial class vtkPlaneCollection
    {
        private readonly List<vtkPlane> _planes = new List<vtkPlane>();
        public void AddItem(vtkPlane plane) { _planes.Add(plane); }
        public void RemoveAllItems() { _planes.Clear(); }
        public int GetNumberOfItems() { return _planes.Count; }
        public vtkPlane GetItem(int i) { return _planes[i]; }
        internal List<vtkPlane> Planes { get { return _planes; } }
    }

    // Pipeline
    public partial class vtkAlgorithmOutput
    {
        internal vtkAlgorithm Producer;
        internal int Index;
        public vtkAlgorithm GetProducer() { return Producer; }
        public int GetIndex() { return Index; }
    }

    public partial class vtkAlgorithm
    {
        private readonly List<List<object>> _inputs = new List<List<object>>();
        private readonly List<vtkDataObject> _outputs = new List<vtkDataObject>();
        private ulong _executeTime;
        private bool _executing;
        private bool _abort;
        private double _progress;

        // Inputs: data objects or connections to the outputs of other algorithms
        protected List<object> InputPort(int port)
        {
            while (_inputs.Count <= port) _inputs.Add(new List<object>());
            return _inputs[port];
        }
        public void SetInput(vtkDataObject input) { SetInput(0, input); }
        public void SetInput(int port, vtkDataObject input)
        {
            List<object> list = InputPort(port);
            list.Clear();
            if (input != null) list.Add(input);
            Modified();
        }
        public void AddInput(vtkDataObject input) { AddInput(0, input); }
        public void AddInput(int port, vtkDataObject input) { if (input != null) InputPort(port).Add(input); Modified(); }
        public void SetInputData(vtkDataObject input) { SetInput(0, input); }
        public void SetInputData(int port, vtkDataObject input) { SetInput(port, input); }
        public void AddInputData(vtkDataObject input) { AddInput(0, input); }
        public void SetInputConnection(vtkAlgorithmOutput output) { SetInputConnection(0, output); }
        public void SetInputConnection(int port, vtkAlgorithmOutput output)
        {
            List<object> list = InputPort(port);
            list.Clear();
            if (output != null) list.Add(output);
            Modified();
        }
        public void AddInputConnection(vtkAlgorithmOutput output) { AddInputConnection(0, output); }
        public void AddInputConnection(int port, vtkAlgorithmOutput output) { if (output != null) InputPort(port).Add(output); Modified(); }
        public void RemoveAllInputs() { foreach (var list in _inputs) list.Clear(); Modified(); }
        public void SetSourceConnection(vtkAlgorithmOutput output) { SetInputConnection(1, output); }
        public void SetSource(vtkPolyData source) { SetInput(1, source); }
        public int GetNumberOfInputConnections(int port) { return port < _inputs.Count ? _inputs[port].Count : 0; }
        public vtkDataObject GetInputDataObject(int port, int connection)
        {
            if (port >= _inputs.Count || connection >= _inputs[port].Count) return null;
            object input = _inputs[port][connection];
            if (input is vtkAlgorithmOutput output) return output.Producer.GetOutputDataObject(output.Index);
            return input as vtkDataObject;
        }
        protected IEnumerable<vtkDataObject> InputDataObjects(int port)
        {
            int n = GetNumberOfInputConnections(port);
            for (int i = 0; i < n; i++)
            {
                vtkDataObject data = GetInputDataObject(port, i);
                if (data != null) yield return data;
            }
        }
        public vtkDataObject GetInput() { return GetInputDataObject(0, 0); }
        // Outputs
        protected void SetOutputData(int port, vtkDataObject data, bool produced = true)
        {
            while (_outputs.Count <= port) _outputs.Add(null);
            _outputs[port] = data;
            if (data != null && produced) data._producer = this;
        }
        protected virtual vtkDataObject CreateOutput(int port) { return new vtkPolyData(); }
        public vtkDataObject GetOutputDataObject(int port)
        {
            while (_outputs.Count <= port) _outputs.Add(null);
            if (_outputs[port] == null) SetOutputData(port, CreateOutput(port));
            return _outputs[port];
        }
        public vtkAlgorithmOutput GetOutputPort() { return GetOutputPort(0); }
        public vtkAlgorithmOutput GetOutputPort(int port)
        {
            GetOutputDataObject(port);
            return new vtkAlgorithmOutput { Producer = this, Index = port };
        }
        // Execution
        public virtual void Update() { UpdatePipeline(); }
        public void Update(int port) { UpdatePipeline(); }
        public void UpdateWholeExtent() { UpdatePipeline(); }
        public void UpdateInformation() { }
        internal virtual void UpdatePipeline()
        {
            if (_executing) return;
            ulong inputTime = MTime;
            foreach (List<object> list in _inputs)
            {
                foreach (object input in list)
                {
                    if (input is vtkAlgorithmOutput output)
                    {
                        output.Producer.UpdatePipeline();
                        vtkDataObject data = output.Producer.GetOutputDataObject(output.Index);
                        inputTime = Math.Max(inputTime, Math.Max(output.Producer._executeTime, data.DataTime));
                    }
                    else if (input is vtkDataObject data)
                    {
                        data._producer?.UpdatePipeline();
                        inputTime = Math.Max(inputTime, data.DataTime);
                    }
                }
            }
            inputTime = Math.Max(inputTime, ExtraModificationTime());
            if (_executeTime != 0 && _executeTime >= inputTime) return;
            _executing = true;
            try
            {
                InvokeEvent(vtkCommandIds.StartEvent, IntPtr.Zero);
                Execute();
                _progress = 1;
                InvokeEvent(vtkCommandIds.EndEvent, IntPtr.Zero);
            }
            finally
            {
                _executing = false;
                _executeTime = vtkObject.GlobalTime + 1;
                // The outputs are newer than the inputs from now on
                foreach (vtkDataObject output in _outputs) output?.Modified();
                _executeTime = vtkObject.GlobalTime;
            }
        }
        // The modification time of the objects the algorithm uses besides its inputs (implicit functions, transforms...)
        protected virtual ulong ExtraModificationTime() { return 0; }
        protected virtual void Execute() { }
        public void SetAbortExecute(int value) { _abort = value != 0; }
        public int GetAbortExecute() { return _abort ? 1 : 0; }
        public double GetProgress() { return _progress; }
        public void SetProgress(double p) { _progress = p; }
        public void ReleaseDataFlagOn() { }
        public void ReleaseDataFlagOff() { }
        public void SetReleaseDataFlag(int f) { }
        public int GetNumberOfOutputPorts() { return Math.Max(1, _outputs.Count); }
    }

    public partial class vtkPolyDataAlgorithm
    {
        public vtkPolyData GetOutput() { return (vtkPolyData)GetOutputDataObject(0); }
        public vtkPolyData GetOutput(int port) { return (vtkPolyData)GetOutputDataObject(port); }
        public vtkPolyData GetPolyDataInput(int port) { return GetInputDataObject(port, 0) as vtkPolyData; }
        protected vtkDataSet InputDataSet { get { return GetInputDataObject(0, 0) as vtkDataSet; } }
        protected vtkPolyData Output { get { return GetOutput(); } }
    }

    public partial class vtkUnstructuredGridAlgorithm
    {
        protected override vtkDataObject CreateOutput(int port) { return new vtkUnstructuredGrid(); }
        public vtkUnstructuredGrid GetOutput() { return (vtkUnstructuredGrid)GetOutputDataObject(0); }
        public vtkUnstructuredGrid GetOutput(int port) { return (vtkUnstructuredGrid)GetOutputDataObject(port); }
    }

    public partial class vtkPointSetAlgorithm
    {
        protected override vtkDataObject CreateOutput(int port)
        {
            vtkDataObject input = GetInputDataObject(0, 0);
            return input is vtkUnstructuredGrid ? new vtkUnstructuredGrid() : (vtkDataObject)new vtkPolyData();
        }
        public vtkPointSet GetOutput() { return GetOutputDataObject(0) as vtkPointSet; }
        public vtkPolyData GetPolyDataOutput() { return GetOutputDataObject(0) as vtkPolyData; }
        public vtkUnstructuredGrid GetUnstructuredGridOutput() { return GetOutputDataObject(0) as vtkUnstructuredGrid; }
        // The output type follows the input type
        protected vtkPointSet PrepareOutput()
        {
            vtkDataObject input = GetInputDataObject(0, 0);
            vtkDataObject output = GetOutputDataObject(0);
            bool wantGrid = input is vtkUnstructuredGrid;
            if (wantGrid != (output is vtkUnstructuredGrid))
            {
                output = wantGrid ? new vtkUnstructuredGrid() : (vtkDataObject)new vtkPolyData();
                SetOutputData(0, output);
            }
            return (vtkPointSet)output;
        }
    }

    public partial class vtkDataSetAlgorithm
    {
        protected override vtkDataObject CreateOutput(int port) { return new vtkUnstructuredGrid(); }
        public vtkDataSet GetOutput() { return GetOutputDataObject(0) as vtkDataSet; }
        public vtkDataSet GetOutput(int port) { return GetOutputDataObject(port) as vtkDataSet; }
        public vtkPolyData GetPolyDataOutput() { return GetOutputDataObject(0) as vtkPolyData; }
        public vtkUnstructuredGrid GetUnstructuredGridOutput() { return GetOutputDataObject(0) as vtkUnstructuredGrid; }
    }

    public partial class vtkImageAlgorithm
    {
        protected override vtkDataObject CreateOutput(int port) { return new vtkImageData(); }
        public vtkImageData GetOutput() { return (vtkImageData)GetOutputDataObject(0); }
    }
}
