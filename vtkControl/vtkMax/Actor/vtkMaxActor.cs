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
using System.Linq;
using CaeGlobals;
using vtkControl.Native;

namespace vtkControl
{
    // An actor of the view as this control keeps it: the data it was built from, how it is drawn, its transformation,
    // its animation frames and its result extremes. The native view draws it
    public class vtkMaxActor
    {
        // Variables
        protected string _name;
        protected string _parentName;
        protected vtkMaxExtreemeNode _minNode;
        protected vtkMaxExtreemeNode _maxNode;
        //
        protected vtkMaxActorData _data;
        protected bool _nodal;
        protected int _pointCount;
        protected string _nativeName;
        protected double[] _transform;
        protected double[] _explodedOffset;
        protected Dictionary<int, int> _nodeIdToPointId;
        protected float[] _values;
        protected PartExchangeData _modelEdgesToDraw;
        protected string _drawnModelEdgesKey;
        // The whole part mesh comes from the provider - section view, box selection, anchors
        protected Func<string, PartExchangeData> _volumeGridProvider;
        protected Func<string, int, int[], NodesExchangeData> _animationNodesProvider;
        protected string _volumeGridSourceName;
        protected bool _volumeGridAvailable;
        protected bool _hasVolumeCells;
        protected int[] _partNodeIds;
        protected HashSet<int> _partNodeIdSet;
        protected int[] _partElementIds;
        protected bool _geometryHiddenBySectionView;
        protected List<vtkMaxActor> _copies;
        protected vtkMaxActorAnimationData[] _animationData;
        protected int _animationFrameNumber;
        protected ResultFieldExchangeData _geometryResultField;
        protected double[] _resultFieldTransformation;
        //
        protected bool _isAPart;
        protected vtkMaxActorRepresentation _actorRepresentation;
        //
        protected bool _visible;
        protected bool _pickable;
        protected bool _backfaceCulling;
        protected System.Drawing.Color _color;
        protected System.Drawing.Color _backfaceColor;
        protected System.Drawing.Color[] _colorTable;
        protected double _ambient;
        protected double _diffuse;
        protected bool _colorContours;
        protected bool _useGlobalLighting;
        protected bool _useEdgeVisibility;
        protected bool _sectionViewPossible;
        protected bool _drawOnGeometry;
        protected bool _useSecondaryHighlightColor;
        protected bool _keepOwnColor;
        //
        protected bool _canHaveElementEdges;
        protected vtkRendererLayer _layer;
        //
        protected uint _memorySize;


        // Properties
        public string Name { get { return _name; } set { _name = value; } }
        public string ParentName { get { return _parentName; } set { _parentName = value; } }
        public vtkMaxExtreemeNode MinNode { get { return _minNode; } set { _minNode = value; } }
        // Directional result values follow the transformation of the actor
        public bool HasTransformedResultField { get { return _geometryResultField != null; } }
        public vtkMaxExtreemeNode MaxNode { get { return _maxNode; } set { _maxNode = value; } }
        // What the actor was built from and whether it is the points of its geometry
        public vtkMaxActorData Data { get { return _data; } }
        public bool Nodal { get { return _nodal; } }
        public int PointCount { get { return _pointCount; } set { _pointCount = value; } }
        // A transformed copy is known in the native view by a name of its own
        public string NativeName { get { return _nativeName ?? _name; } set { _nativeName = value; } }
        public double[] ExplodedOffset
        {
            get { return _explodedOffset ?? new double[3]; }
            set { _explodedOffset = value; }
        }
        public float[] Values { get { return _values; } }
        // A sub part of a compound part draws only the model edges no other sub part draws
        public PartExchangeData ModelEdgesToDraw { get { return _modelEdgesToDraw; } }
        //
        public Func<string, PartExchangeData> VolumeGridProvider
        {
            get { return _volumeGridProvider; }
            set { _volumeGridProvider = value; }
        }
        public Func<string, int, int[], NodesExchangeData> AnimationNodesProvider
        {
            get { return _animationNodesProvider; }
            set { _animationNodesProvider = value; }
        }
        public string VolumeGridSourceName { get { return _volumeGridSourceName; } }
        public bool VolumeGridAvailable { get { return _volumeGridAvailable; } }
        public bool HasVolumeCells { get { return _hasVolumeCells; } }
        public int[] PartNodeIds { get { return _partNodeIds; } }
        public int[] PartElementIds { get { return _partElementIds; } }
        public bool GeometryHiddenBySectionView
        {
            get { return _geometryHiddenBySectionView; }
            set { _geometryHiddenBySectionView = value; }
        }
        public List<vtkMaxActor> Copies { get { return _copies; } }
        public int AnimationFrameNumber { get { return _animationFrameNumber; } }
        //
        public bool IsAPart { get { return _isAPart; } }
        public vtkMaxActorRepresentation ActorRepresentation
        {
            get { return _actorRepresentation; }
            set { _actorRepresentation = value; }
        }
        public virtual bool VtkMaxActorVisible { get { return _visible; } set { _visible = value; } }
        public bool BackfaceCulling { get { return _backfaceCulling; } set { _backfaceCulling = value; } }
        public bool Pickable { get { return _pickable; } set { _pickable = value; } }
        public System.Drawing.Color Color { get { return _color; } set { _color = value; } }
        public System.Drawing.Color BackfaceColor { get { return _backfaceColor; } set { _backfaceColor = value; } }
        public System.Drawing.Color[] ColorTable { get { return _colorTable; } set { _colorTable = value; } }
        public double Ambient { get { return _ambient; } set { _ambient = value; } }
        public double Diffuse { get { return _diffuse; } set { _diffuse = value; } }
        public bool ColorContours { get { return _colorContours; } set { _colorContours = value; } }
        public bool UseGlobalLighting { get { return _useGlobalLighting; } set { _useGlobalLighting = value; } }
        public bool UseEdgeVisibility { get { return _useEdgeVisibility; } set { _useEdgeVisibility = value; } }
        public bool SectionViewPossible { get { return _sectionViewPossible; } set { _sectionViewPossible = value; } }
        public bool DrawOnGeometry { get { return _drawOnGeometry; } set { _drawOnGeometry = value; } }
        public bool UseSecondaryHighlightColor
        {
            get { return _useSecondaryHighlightColor; }
            set { _useSecondaryHighlightColor = value; }
        }
        public bool KeepOwnColor { get { return _keepOwnColor; } set { _keepOwnColor = value; } }
        public bool CanHaveElementEdges { get { return _canHaveElementEdges; } set { _canHaveElementEdges = value; } }
        public vtkRendererLayer Layer { get { return _layer; } set { _layer = value; } }
        public uint MemorySize { get { return _memorySize; } set { _memorySize = value; } }


        // Constructors
        public vtkMaxActor()
        {
            _name = null;
            _parentName = null;
            _minNode = null;
            _maxNode = null;
            //
            _data = null;
            _nodal = false;
            _pointCount = 0;
            _nativeName = null;
            _transform = null;
            _explodedOffset = null;
            _nodeIdToPointId = null;
            _values = null;
            _volumeGridProvider = null;
            _animationNodesProvider = null;
            _volumeGridSourceName = null;
            _volumeGridAvailable = false;
            _hasVolumeCells = false;
            _partNodeIds = null;
            _partElementIds = null;
            _geometryHiddenBySectionView = false;
            _copies = new List<vtkMaxActor>();
            _animationData = null;
            _animationFrameNumber = -1;
            _geometryResultField = null;
            _resultFieldTransformation = ResultFieldExchangeData.IdentityTransformation();
            //
            _isAPart = false;
            _actorRepresentation = vtkMaxActorRepresentation.Unknown;
            _visible = true;
            _pickable = false;
            _backfaceCulling = true;
            _color = System.Drawing.Color.Empty;
            _backfaceColor = System.Drawing.Color.Empty;
            _colorTable = null;
            _ambient = 0.5;
            _diffuse = 0.5;
            _colorContours = false;
            _useGlobalLighting = true;
            _useEdgeVisibility = true;
            _sectionViewPossible = true;
            _drawOnGeometry = false;
            _useSecondaryHighlightColor = false;
            //
            _canHaveElementEdges = false;
            _layer = vtkRendererLayer.Base;
            //
            _memorySize = 0;
        }
        public vtkMaxActor(vtkMaxActorData data)
            : this(data, false, false)
        {
        }
        public vtkMaxActor(vtkMaxActorData data, bool extractVisualizationSurface, bool createNodalActor)
            : this()
        {
            _data = data;
            _name = data.Name;
            _parentName = data.ParentName;
            _modelEdgesToDraw = data.ModelEdgesToDraw;
            _drawnModelEdgesKey = GetModelEdgesKey(data.ModelEdgesToDraw ?? data.ModelEdges);
            //
            PartExchangeData geometry = data.Geometry;
            _nodal = createNodalActor || (geometry.Cells.CellNodeIds == null && geometry.Nodes.Coor != null);
            _pointCount = GetPointCount(geometry.Nodes);
            _pickable = data.Pickable && !_nodal && geometry.Cells.Ids != null;
            if (!_nodal && geometry.Nodes.Values != null)
            {
                _values = geometry.Nodes.Values;
                if (geometry.ExtremeNodes != null && geometry.ExtremeNodes.Ids != null) SetExtremes(geometry.ExtremeNodes);
            }
            //
            _isAPart = data.IsAPart;
            _volumeGridAvailable = data.Layer == vtkRendererLayer.Base && data.VolumeGridAvailable;
            _hasVolumeCells = data.HasVolumeCells;
            _partNodeIds = data.PartNodeIds;
            _partElementIds = data.PartElementIds;
            CopyStyle(data);
            //
            _geometryResultField = GetCompatibleResultField(geometry.Nodes.ResultField, _pointCount);
        }
        // An actor that draws other points than the nodes of its data: a symbol or a plot of a field. It takes the
        // style of the data, and neither its values nor its volume grid
        public vtkMaxActor(vtkMaxActorData data, int pointCount)
            : this()
        {
            _data = data;
            _name = data.Name;
            _parentName = data.ParentName;
            _pointCount = pointCount;
            _pickable = data.Pickable;
            CopyStyle(data);
        }
        public vtkMaxActor(vtkMaxActor sourceActor)
          : this()
        {
            _name = sourceActor.Name;
            _parentName = sourceActor.ParentName;
            //
            if (sourceActor.MinNode != null) _minNode = new vtkMaxExtreemeNode(sourceActor.MinNode);
            if (sourceActor.MaxNode != null) _maxNode = new vtkMaxExtreemeNode(sourceActor.MaxNode);
            //
            _data = sourceActor._data;
            _modelEdgesToDraw = sourceActor._modelEdgesToDraw;
            _drawnModelEdgesKey = sourceActor._drawnModelEdgesKey;
            _nodal = sourceActor._nodal;
            _pointCount = sourceActor._pointCount;
            _transform = sourceActor._transform == null ? null : (double[])sourceActor._transform.Clone();
            _nodeIdToPointId = sourceActor._nodeIdToPointId;
            _values = sourceActor._values;
            _animationData = sourceActor._animationData;
            _animationFrameNumber = sourceActor._animationFrameNumber;
            _pickable = false;
            // Volume grid - a copy takes the one of the source part
            if (sourceActor.VolumeGridAvailable)
            {
                _volumeGridAvailable = true;
                _hasVolumeCells = sourceActor.HasVolumeCells;
                _partNodeIds = sourceActor.PartNodeIds;
                _partElementIds = sourceActor.PartElementIds;
                _volumeGridProvider = sourceActor.VolumeGridProvider;
                _animationNodesProvider = sourceActor.AnimationNodesProvider;
                _volumeGridSourceName = sourceActor.VolumeGridSourceName ?? sourceActor.Name;
            }
            // Transformed copies
            _copies = new List<vtkMaxActor>(sourceActor.Copies);
            // Result field transformation data is immutable and shared by actor copies
            _geometryResultField = sourceActor._geometryResultField;
            _resultFieldTransformation = (double[])sourceActor._resultFieldTransformation.Clone();
            //
            _isAPart = sourceActor.IsAPart;
            _actorRepresentation = sourceActor.ActorRepresentation;
            _visible = sourceActor.VtkMaxActorVisible;
            _backfaceCulling = sourceActor.BackfaceCulling;
            _color = sourceActor.Color;
            _backfaceColor = sourceActor.BackfaceColor;
            _colorTable = sourceActor.ColorTable;
            _ambient = sourceActor.Ambient;
            _diffuse = sourceActor.Diffuse;
            _colorContours = sourceActor.ColorContours;
            _useGlobalLighting = sourceActor.UseGlobalLighting;
            _useEdgeVisibility = sourceActor.UseEdgeVisibility;
            _sectionViewPossible = sourceActor.SectionViewPossible;
            _drawOnGeometry = sourceActor.DrawOnGeometry;
            _useSecondaryHighlightColor = sourceActor.UseSecondaryHighlightColor;
            _keepOwnColor = sourceActor.KeepOwnColor;
            //
            _canHaveElementEdges = sourceActor.CanHaveElementEdges;
            _layer = sourceActor.Layer;
            //
            _memorySize = sourceActor.MemorySize;
        }


        // Static methods
        public static int GetPointCount(NodesExchangeData nodes)
        {
            if (nodes == null) return 0;
            if (nodes.Coor != null) return nodes.Coor.Length;
            if (nodes.FlatCoor != null) return nodes.FlatCoor.Length / 3;
            return 0;
        }
        // A 4 x 4 matrix of a translation, of a scale about a point and of a rotation about an axis through a point
        public static double[] GetTranslation(double[] displacement)
        {
            double[] matrix = GetIdentity();
            for (int i = 0; i < 3; i++) matrix[4 * i + 3] = displacement[i];
            return matrix;
        }
        public static double[] GetScale(double[] point, double[] scale)
        {
            double[] matrix = GetIdentity();
            for (int i = 0; i < 3; i++)
            {
                matrix[5 * i] = scale[i];
                matrix[4 * i + 3] = point[i] - scale[i] * point[i];
            }
            return matrix;
        }
        // vtkTransform.RotateWXYZ: a rotation by an angle in degrees about an axis
        public static double[] GetRotation(double[] point, double[] axis, double angle)
        {
            double length = Math.Sqrt(axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2]);
            double x = axis[0] / length;
            double y = axis[1] / length;
            double z = axis[2] / length;
            double radians = angle * Math.PI / 180;
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            double t = 1 - c;
            double[] rotation = new double[]
            {
                t * x * x + c,     t * x * y - s * z, t * x * z + s * y, 0,
                t * x * y + s * z, t * y * y + c,     t * y * z - s * x, 0,
                t * x * z - s * y, t * y * z + s * x, t * z * z + c,     0,
                0,                 0,                 0,                 1
            };
            for (int i = 0; i < 3; i++)
            {
                double moved = 0;
                for (int j = 0; j < 3; j++) moved += rotation[4 * i + j] * point[j];
                rotation[4 * i + 3] = point[i] - moved;
            }
            return rotation;
        }
        public static double[] GetIdentity()
        {
            return new double[] { 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1 };
        }
        public static double[] Multiply(double[] a, double[] b)
        {
            double[] product = new double[16];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    double value = 0;
                    for (int k = 0; k < 4; k++) value += a[4 * i + k] * b[4 * k + j];
                    product[4 * i + j] = value;
                }
            }
            return product;
        }
        public static double[] TransformPoint(double[] matrix, double[] point)
        {
            if (matrix == null) return new double[] { point[0], point[1], point[2] };
            double[] transformed = new double[3];
            for (int i = 0; i < 3; i++)
            {
                transformed[i] = matrix[4 * i] * point[0] + matrix[4 * i + 1] * point[1] + matrix[4 * i + 2] * point[2] +
                                 matrix[4 * i + 3];
            }
            return transformed;
        }
        // The rotation part of a transformation, which a directional result component is transformed with
        public static double[] GetResultFieldTransformation(double[] matrix)
        {
            double[] transformation = new double[9];
            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++) transformation[3 * row + column] = matrix[4 * row + column];
            }
            return transformation;
        }
        private static ResultFieldExchangeData GetCompatibleResultField(ResultFieldExchangeData resultField,
                                                                          long numberOfPoints)
        {
            if (resultField == null || !resultField.IsValid() ||
                resultField.GetNumberOfValues() != numberOfPoints) return null;
            return resultField;
        }


        // Methods
        private void CopyStyle(vtkMaxActorData data)
        {
            _actorRepresentation = data.ActorRepresentation;
            _backfaceCulling = data.BackfaceCulling;
            _color = data.Color;
            _backfaceColor = data.BackfaceColor;
            _colorTable = data.ColorTable;
            _ambient = data.Ambient;
            _diffuse = data.Diffuse;
            _colorContours = data.ColorContours;
            _useGlobalLighting = data.UseGlobalLighting;
            _useEdgeVisibility = data.UseEdgeVisibility;
            _sectionViewPossible = data.SectionViewPossible;
            _drawOnGeometry = data.DrawOnGeometry;
            _useSecondaryHighlightColor = data.UseSecondaryHighlightColor;
            _keepOwnColor = data.KeepOwnColor;
            //
            _canHaveElementEdges = data.CanHaveElementEdges;
            _layer = data.Layer;
        }
        private void SetExtremes(NodesExchangeData extremeNodes)
        {
            _minNode = new vtkMaxExtreemeNode(extremeNodes.Ids[0], extremeNodes.Coor[0], extremeNodes.Values[0]);
            _maxNode = new vtkMaxExtreemeNode(extremeNodes.Ids[1], extremeNodes.Coor[1], extremeNodes.Values[1]);
        }
        // The values drawn at the points and their extremes, as a new scalar field gives them
        public void SetValues(float[] values, NodesExchangeData extremeNodes)
        {
            _values = values;
            if (values == null || extremeNodes == null)
            {
                _minNode = null;
                _maxNode = null;
            }
            else SetExtremes(extremeNodes);
        }
        // The style the native view draws the actor with: its data's, with what changed on the actor since
        public VtkNativePartStyle GetNativeStyle()
        {
            VtkNativePartStyle style = _data != null ? VtkNativePartStyle.FromActorData(_data) : new VtkNativePartStyle();
            style.SetColor(_color);
            style.HasBackfaceColor = _backfaceColor.IsEmpty ? 0 : 1;
            if (!_backfaceColor.IsEmpty)
            {
                style.BackfaceColorR = _backfaceColor.R / 255d;
                style.BackfaceColorG = _backfaceColor.G / 255d;
                style.BackfaceColorB = _backfaceColor.B / 255d;
                style.BackfaceColorA = _backfaceColor.A / 255d;
            }
            style.Ambient = _ambient;
            style.Diffuse = _diffuse;
            style.ColorContours = _colorContours ? 1 : 0;
            style.Visible = _visible ? 1 : 0;
            style.Nodal = _nodal ? 1 : 0;
            style.Layer = (int)VtkNativePartStyle.ToNativeLayer(_layer);
            return style;
        }
        // The number of cells the actor is drawn with: its vertices, or the lines and triangles of its cells
        public int GetNumberOfElements()
        {
            if (_data == null) return 0;
            if (_nodal) return _pointCount;
            int[] types = _data.Geometry.Cells.Types;
            if (types == null) return 0;
            int count = 0;
            foreach (int type in types)
            {
                if (type == 21 || type == 9) count += 2;            // VTK_QUADRATIC_EDGE, VTK_QUAD
                else if (type == 22) count += 4;                    // VTK_QUADRATIC_TRIANGLE
                else if (type == 23) count += 6;                    // VTK_QUADRATIC_QUAD
                else count++;
            }
            return count;
        }
        // The point of the given index as it is drawn: of the animation frame shown, transformed with the actor
        public double[] GetPoint(int pointId)
        {
            double[] point = null;
            vtkMaxActorAnimationData frame = GetCurrentFrame();
            if (frame != null && frame.Coordinates != null && 3 * pointId + 2 < frame.Coordinates.Length)
            {
                float[] coordinates = frame.Coordinates;
                point = new double[] { coordinates[3 * pointId], coordinates[3 * pointId + 1], coordinates[3 * pointId + 2] };
            }
            else if (_data != null)
            {
                NodesExchangeData nodes = _data.Geometry.Nodes;
                if (nodes.Coor != null && pointId < nodes.Coor.Length) point = nodes.Coor[pointId];
                else if (nodes.FlatCoor != null && 3 * pointId + 2 < nodes.FlatCoor.Length)
                    point = new double[] { nodes.FlatCoor[3 * pointId], nodes.FlatCoor[3 * pointId + 1],
                                           nodes.FlatCoor[3 * pointId + 2] };
            }
            if (point == null) return null;
            return TransformPoint(_transform, point);
        }
        public bool TryGetPointId(int nodeId, out int pointId)
        {
            pointId = -1;
            if (_data == null || _data.Geometry.Nodes.Ids == null) return false;
            if (_nodeIdToPointId == null)
            {
                int[] ids = _data.Geometry.Nodes.Ids;
                _nodeIdToPointId = new Dictionary<int, int>(ids.Length);
                for (int i = 0; i < ids.Length; i++) _nodeIdToPointId[ids[i]] = i;
            }
            return _nodeIdToPointId.TryGetValue(nodeId, out pointId);
        }
        // The current coordinates of every point, as flat x, y, z values
        public float[] GetCoordinates()
        {
            float[] coordinates = new float[3 * _pointCount];
            for (int i = 0; i < _pointCount; i++)
            {
                double[] point = GetPoint(i);
                if (point == null) continue;
                coordinates[3 * i] = (float)point[0];
                coordinates[3 * i + 1] = (float)point[1];
                coordinates[3 * i + 2] = (float)point[2];
            }
            return coordinates;
        }
        // The model edges a sub part of a compound part draws now; whether they are other edges than it drew
        public bool UpdateModelEdgesToDraw(PartExchangeData modelEdgesToDraw)
        {
            if (_data == null || _data.ModelEdges == null || modelEdgesToDraw == null) return false;
            string modelEdgesKey = GetModelEdgesKey(modelEdgesToDraw);
            if (modelEdgesKey == _drawnModelEdgesKey) return false;
            _modelEdgesToDraw = modelEdgesToDraw;
            _drawnModelEdgesKey = modelEdgesKey;
            return true;
        }
        /// <summary>
        /// The edges given by the nodes of their cells. Two sets of edges with the same number of cells are
        /// still different edges when a sub part of a compound hands a shared edge to another sub part and
        /// takes over a different one in its place
        /// </summary>
        private static string GetModelEdgesKey(PartExchangeData modelEdges)
        {
            if (modelEdges == null || modelEdges.Nodes.Ids == null || modelEdges.Cells.CellNodeIds == null) return null;
            //
            System.Text.StringBuilder key = new System.Text.StringBuilder();
            int[] nodeIds = modelEdges.Nodes.Ids;
            foreach (int[] cellNodeIds in modelEdges.Cells.CellNodeIds)
            {
                foreach (int cellNodeId in cellNodeIds)
                {
                    key.Append(cellNodeId >= 0 && cellNodeId < nodeIds.Length ? nodeIds[cellNodeId] : -1);
                    key.Append(',');
                }
                key.Append(';');
            }
            return key.ToString();
        }
        // Transformation
        public void AppendTransform(double[] matrix)
        {
            _transform = _transform == null ? (double[])matrix.Clone() : Multiply(matrix, _transform);
        }
        // Result fields
        public void UpdateResultFields(ResultFieldExchangeData geometryResultField)
        {
            _geometryResultField = GetCompatibleResultField(geometryResultField, _pointCount);
            // Keep the actor's cumulative transformation and reconstruct the newly selected component
            if (_geometryResultField != null) SetTransformedValues();
        }
        public void ApplyResultFieldTransformation(double[] transformation)
        {
            // Store the geometry transformation even if the currently displayed field is invariant.
            // A later component selection can then reconstruct its directional values correctly
            _resultFieldTransformation = ResultFieldExchangeData.MultiplyTransformations(transformation,
                                                                                         _resultFieldTransformation);
            if (_geometryResultField != null) SetTransformedValues();
        }
        private void SetTransformedValues()
        {
            float[] values = _geometryResultField.GetTransformedValues(_resultFieldTransformation);
            if (values == null || values.Length != _pointCount) return;
            _values = values;
            UpdateResultExtremes(values);
        }
        private void UpdateResultExtremes(float[] values)
        {
            int minIndex = -1;
            int maxIndex = -1;
            float minValue = float.MaxValue;
            float maxValue = -float.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                float value = values[i];
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;
                if (minIndex < 0 || value < minValue)
                {
                    minIndex = i;
                    minValue = value;
                }
                if (maxIndex < 0 || value > maxValue)
                {
                    maxIndex = i;
                    maxValue = value;
                }
            }
            if (minIndex < 0)
            {
                _minNode = null;
                _maxNode = null;
                return;
            }
            //
            int[] ids = _data?.Geometry.Nodes.Ids;
            int minId = ids == null ? -1 : ids[minIndex];
            int maxId = ids == null ? -1 : ids[maxIndex];
            _minNode = new vtkMaxExtreemeNode(minId, GetPoint(minIndex), minValue);
            _maxNode = new vtkMaxExtreemeNode(maxId, GetPoint(maxIndex), maxValue);
        }
        internal bool TryGetAnimationResultRange(IEnumerable<double[]> transformations, out double minimum,
                                                  out double maximum)
        {
            minimum = double.MaxValue;
            maximum = -double.MaxValue;
            if (_animationData == null || transformations == null) return false;
            //
            bool found = false;
            foreach (vtkMaxActorAnimationData animationData in _animationData)
            {
                ResultFieldExchangeData resultField = animationData.ResultField;
                if (resultField == null) continue;
                foreach (double[] transformation in transformations)
                {
                    if (resultField.TryGetTransformedRange(transformation, out float min, out float max))
                    {
                        if (min < minimum) minimum = min;
                        if (max > maximum) maximum = max;
                        found = true;
                    }
                }
            }
            return found;
        }


        // Animation
        public double InitializeAnimation(vtkMaxActorData data)
        {
            _animationFrameNumber = -1;
            if (data.Geometry.NodesAnimation == null)
            {
                _animationData = null;
                return 0;
            }
            //
            int numOfFrames = data.Geometry.NodesAnimation.Length;
            _animationData = new vtkMaxActorAnimationData[numOfFrames];
            double memorySizeKb = 0;
            for (int frameNumber = 0; frameNumber < numOfFrames; frameNumber++)
            {
                NodesExchangeData extremeNodes = null;
                if (data.Geometry.ExtremeNodesAnimation != null)
                    extremeNodes = data.Geometry.ExtremeNodesAnimation[frameNumber];
                //
                vtkMaxActorAnimationData animationData =
                    new vtkMaxActorAnimationData(data.Geometry.NodesAnimation[frameNumber], extremeNodes);
                _animationData[frameNumber] = animationData;
                memorySizeKb += animationData.GetMemorySizeKb();
                ReleaseConvertedAnimationFrame(data, frameNumber);
            }
            SetAnimationFrame(0);
            return memorySizeKb;
        }
        private static void ReleaseConvertedAnimationFrame(vtkMaxActorData data, int frameNumber)
        {
            // Without this every frame is held twice - as the exchange data and as the frame
            data.Geometry.NodesAnimation[frameNumber] = null;
            if (data.ModelEdges != null && data.ModelEdges.NodesAnimation != null)
                data.ModelEdges.NodesAnimation[frameNumber] = null;
        }
        public void SetAnimationFrame(int frameNumber)
        {
            if (_animationData == null || frameNumber < 0 || frameNumber >= _animationData.Length)
                throw new Exception("The animation frame cannot be set.");
            //
            vtkMaxActorAnimationData animationData = _animationData[frameNumber];
            _animationFrameNumber = frameNumber;
            _geometryResultField = animationData.ResultField;
            _resultFieldTransformation = ResultFieldExchangeData.IdentityTransformation();
            if (animationData.Values != null) _values = animationData.Values;
            if (animationData.MinNode != null) _minNode = animationData.MinNode;
            if (animationData.MaxNode != null) _maxNode = animationData.MaxNode;
        }
        private vtkMaxActorAnimationData GetCurrentFrame()
        {
            if (_animationData == null || _animationFrameNumber < 0 || _animationFrameNumber >= _animationData.Length)
                return null;
            return _animationData[_animationFrameNumber];
        }
        internal int GetAnimationFrameCount()
        {
            return _animationData?.Length ?? 0;
        }
        internal bool TryGetAnimationExtremeNodes(int frameNumber, out vtkMaxExtreemeNode minNode,
                                                   out vtkMaxExtreemeNode maxNode)
        {
            minNode = null;
            maxNode = null;
            if (_animationData == null || frameNumber < 0 || frameNumber >= _animationData.Length) return false;
            //
            minNode = _animationData[frameNumber].MinNode;
            maxNode = _animationData[frameNumber].MaxNode;
            return minNode != null && maxNode != null;
        }
        internal bool TryGetAnimationNodeCoordinates(int frameNumber, int nodeId, out double[] coordinates)
        {
            coordinates = null;
            if (_animationData == null || frameNumber < 0 || frameNumber >= _animationData.Length) return false;
            //
            float[] frameCoordinates = _animationData[frameNumber].Coordinates;
            if (frameCoordinates != null && TryGetPointId(nodeId, out int pointId))
            {
                coordinates = new double[] { frameCoordinates[3 * pointId], frameCoordinates[3 * pointId + 1],
                                             frameCoordinates[3 * pointId + 2] };
                return true;
            }
            NodesExchangeData nodes = GetAnimationNodes(frameNumber, new int[] { nodeId });
            if (nodes == null) return false;
            coordinates = new double[] { nodes.FlatCoor[0], nodes.FlatCoor[1], nodes.FlatCoor[2] };
            return true;
        }
        internal bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value)
        {
            coordinates = null;
            value = float.NaN;
            if (TryGetPointId(nodeId, out int pointId))
            {
                coordinates = GetPoint(pointId);
                if (_values != null && pointId < _values.Length) value = _values[pointId];
                return coordinates != null;
            }
            // An interior node - only its point is read at the displayed frame
            if (!_volumeGridAvailable || _partNodeIds == null) return false;
            if (_partNodeIdSet == null) _partNodeIdSet = new HashSet<int>(_partNodeIds);
            if (!_partNodeIdSet.Contains(nodeId)) return false;
            NodesExchangeData nodes = GetAnimationNodes(_animationFrameNumber, new int[] { nodeId });
            if (nodes == null) return false;
            coordinates = new double[] { nodes.FlatCoor[0], nodes.FlatCoor[1], nodes.FlatCoor[2] };
            if (nodes.Values != null && nodes.Values.Length > 0) value = nodes.Values[0];
            return true;
        }
        // The nodes of the part at a frame, read from the results
        internal NodesExchangeData GetAnimationNodes(int frameNumber, int[] nodeIds)
        {
            if (_animationNodesProvider == null || frameNumber < 0) return null;
            NodesExchangeData nodes = _animationNodesProvider(_name, frameNumber, nodeIds);
            if (nodes == null || nodes.FlatCoor == null || nodes.FlatCoor.Length != 3 * nodeIds.Length) return null;
            return nodes;
        }
        // The anchor of an annotation that follows the animation: the cell of the part's volume grid that holds the
        // point, or of its surface when it has no volume grid, with the weights of its points
        internal bool TryCreateAnimationArrowAnchor(double[] anchorPoint, out vtkMaxAnimationArrowAnchor anchor)
        {
            anchor = null;
            PartExchangeData volumeGrid = null;
            if (_volumeGridAvailable && _volumeGridProvider != null)
                volumeGrid = _volumeGridProvider(_volumeGridSourceName ?? _name);
            bool useVolumeGrid = volumeGrid != null && volumeGrid.Cells != null && volumeGrid.Cells.CellNodeIds != null &&
                                 volumeGrid.Nodes.Ids != null && (volumeGrid.Nodes.Coor != null ||
                                                                  volumeGrid.Nodes.FlatCoor != null);
            PartExchangeData mesh;
            if (useVolumeGrid) mesh = volumeGrid;
            else
            {
                if (_data == null || _data.Geometry.Cells.CellNodeIds == null || !_pickable) return false;
                mesh = new PartExchangeData();
                mesh.Nodes.FlatCoor = GetCoordinates();
                mesh.Cells = _data.Geometry.Cells;
            }
            //
            int[] pointIndices;
            double[] weights;
            double[] interpolatedPoint;
            if (!VtkNativeView.LocatePointInMesh(VtkNativeMeshData.FromPartExchangeData(mesh), anchorPoint,
                                                 out pointIndices, out weights, out interpolatedPoint)) return false;
            //
            double[] offset = new double[] { anchorPoint[0] - interpolatedPoint[0],
                                             anchorPoint[1] - interpolatedPoint[1],
                                             anchorPoint[2] - interpolatedPoint[2] };
            int[] nodeIds = null;
            if (useVolumeGrid)
            {
                nodeIds = new int[pointIndices.Length];
                for (int i = 0; i < nodeIds.Length; i++) nodeIds[i] = volumeGrid.Nodes.Ids[pointIndices[i]];
            }
            anchor = new vtkMaxAnimationArrowAnchor(this, pointIndices, nodeIds, weights, offset);
            return true;
        }
        // Memory
        public void RecomputeMemorySize()
        {
            double sizeKb = 0;
            if (_data != null)
            {
                sizeKb += GetMemorySizeKb(_data.Geometry);
                sizeKb += GetMemorySizeKb(_data.ModelEdges);
                sizeKb += GetMemorySizeKb(_data.ElementEdges);
            }
            if (_animationData != null)
            {
                foreach (vtkMaxActorAnimationData animationData in _animationData)
                    sizeKb += animationData.GetMemorySizeKb();
            }
            _memorySize = (uint)Math.Ceiling(sizeKb);
        }
        private static double GetMemorySizeKb(PartExchangeData data)
        {
            if (data == null) return 0;
            double bytes = 0;
            NodesExchangeData nodes = data.Nodes;
            if (nodes != null)
            {
                if (nodes.Coor != null) bytes += nodes.Coor.Length * (3 * sizeof(double) + 24);
                if (nodes.FlatCoor != null) bytes += nodes.FlatCoor.Length * sizeof(float);
                if (nodes.Ids != null) bytes += nodes.Ids.Length * sizeof(int);
                if (nodes.Normals != null) bytes += nodes.Normals.Length * (3 * sizeof(double) + 24);
                if (nodes.Values != null) bytes += nodes.Values.Length * sizeof(float);
            }
            if (data.Cells != null && data.Cells.CellNodeIds != null)
            {
                foreach (int[] cell in data.Cells.CellNodeIds) bytes += cell.Length * sizeof(int) + 24;
                bytes += data.Cells.CellNodeIds.Length * (sizeof(int) + sizeof(float));
            }
            return bytes / 1024;
        }
    }
}
