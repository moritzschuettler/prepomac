// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    [Serializable]
    public class BasePart : FeGroup, IComparable<BasePart>, IStoreAsNameInCommands
    {
        // Variables                                                                                                                
        protected int _partId;
        protected PartType _partType;
        protected System.Drawing.Color _color;
        protected Type[] _elementTypes;
        protected int[] _nodeLabels;
        protected bool _smoothShaded;
        protected BoundingBox _boundingBox;
        protected double[] _offset;
        protected double _surfaceOffset;
        protected PartMassProperties _massProperties;
        [OptionalField]
        protected GeometryMetadata _geometryMetadata;
        [OptionalField]
        protected AffineTransform _geometryToMeshTransform;
        //
        [NonSerialized]
        protected VisualizationData _visualization;


        // Properties                                                                                                               
        public int PartId { get { return _partId; } set { _partId = value; } }
        public PartType PartType { get { return _partType; } }
        public System.Drawing.Color Color
        {
            get { return _color; }
            set { _color = value; }
        }
        public Type[] ElementTypes { get { return _elementTypes; } }
        public int[] NodeLabels { get { return _nodeLabels; } set { _nodeLabels = value; } }
        public bool SmoothShaded { get { return _smoothShaded; } set { _smoothShaded = value; } }
        public BoundingBox BoundingBox { get { return _boundingBox; } set { _boundingBox = value; } }
        public double[] Offset { get { return _offset; } set { _offset = value; } }
        public double SurfaceOffset { get { return _surfaceOffset; } set { _surfaceOffset = value; } }
        public PartMassProperties MassProperties { get { return _massProperties; } set { _massProperties = value; } }
        public GeometryMetadata GeometryMetadata { get { return _geometryMetadata; } set { _geometryMetadata = value; } }
        // Where a mesh part was moved to from the geometry it was meshed from; null while it lies on that geometry
        public AffineTransform GeometryToMeshTransform
        {
            get { return _geometryToMeshTransform; }
            set { _geometryToMeshTransform = value; }
        }
        //
        public VisualizationData Visualization { get { return _visualization; } set { _visualization = value; } }


        // Constructors                                                                                                             
        public BasePart(string name, int partId, int[] nodeLabels, int[] elementLabels, Type[] elementTypes)
            : base(name, elementLabels)
        {
            _partId = partId;
            _nodeLabels = nodeLabels;
            _color = System.Drawing.Color.Gray;
            _elementTypes = elementTypes;
            _visualization = new VisualizationData();
            _smoothShaded = false;
            _boundingBox = new BoundingBox();
            _offset = new double[3];
            _massProperties = new PartMassProperties(false);
            _geometryMetadata = null;
            _geometryToMeshTransform = null;
            //
            if (IsSolid()) _partType = PartType.Solid;
            else if (IsShell()) _partType = PartType.Shell;
            else if (IsBeam()) _partType = PartType.Wire;
            else _partType = PartType.Unknown;
        }
        public BasePart(BasePart part)
            : base(part.Name, part.Labels.ToArray())
        {
            _partId = part.PartId;
            //
            _partType = part.PartType;
            //
            _color = part.Color;
            //
            _elementTypes = part.ElementTypes != null ? part.ElementTypes.ToArray() : null;
            //
            _visualization = part.Visualization.DeepCopy();
            //
            _nodeLabels = part.NodeLabels != null ? part.NodeLabels.ToArray() : null;
            //
            _smoothShaded = part.SmoothShaded;
            //
            _boundingBox = part.BoundingBox.DeepClone();
            //
            if (part.Offset != null) _offset = part.Offset.ToArray();
            //
            _surfaceOffset = part.SurfaceOffset;
            //
            _massProperties = part.MassProperties.DeepClone();
            _geometryMetadata = part.GeometryMetadata != null ? part.GeometryMetadata.DeepCopy() : null;
            _geometryToMeshTransform = part.GeometryToMeshTransform;
        }


        // Methods                                                                                                                  
        private bool IsSolid()
        {
            if (_elementTypes == null) return false;
            //
            foreach (var type in _elementTypes)
            {
                if (type != typeof(LinearTetraElement) && type != typeof(LinearPyramidElement) &&
                    type != typeof(LinearWedgeElement) && type != typeof(LinearHexaElement) &&
                    type != typeof(ParabolicTetraElement) && type != typeof(ParabolicPyramidElement) &&
                    type != typeof(ParabolicWedgeElement) && type != typeof(ParabolicHexaElement)) return false;
            }
            return true;
        }
        private bool IsShell()
        {
            if (_elementTypes == null) return false;
            //
            foreach (var type in _elementTypes)
            {
                if (type != typeof(LinearTriangleElement) && type != typeof(LinearQuadrilateralElement) &&
                    type != typeof(ParabolicTriangleElement) && type != typeof(ParabolicQuadrilateralElement)) return false;
            }
            return true;
        }
        private bool IsBeam()
        {
            if (_elementTypes == null) return false;
            //
            foreach (var type in _elementTypes)
            {
                if (type != typeof(LinearBeamElement) && type != typeof(ParabolicBeamElement)) return false;
            }
            return true;
        }
        public void SetPartType(PartType partType)
        {
            if (_partType == PartType.Shell && partType == PartType.SolidAsShell)
            {
                _partType = partType;
            }
            else if (_partType == PartType.Shell && partType == PartType.Shell)
            {
                _partType = partType;
            }
            else throw new NotSupportedException();
        }
        public void SetElementTypes(Type[] elementTypes)
        {
            _elementTypes = elementTypes != null ? elementTypes.ToArray() : null;
        }
        public virtual BasePart DeepCopy()
        {
            return new BasePart(this);
        }
        public virtual PartProperties GetProperties()
        {
            PartProperties properties = new PartProperties();
            properties.Name = Name;
            properties.PartType = _partType;
            properties.IsCADPart = false;
            properties.Color = _color;
            properties.NumberOfNodes = _nodeLabels.Length;
            properties.NumberOfElements = Labels.Length;
            properties.MassProperties = _massProperties;
            return properties;
        }
        public virtual void SetProperties(PartProperties properties)
        {
            Name = properties.Name;
            _color = properties.Color;
        }
        public virtual void RenumberVisualizationNodes(Dictionary<int, int> newIds)
        {
            _visualization.RenumberNodes(newIds);
        }
        public virtual void RenumberVisualizationElements(Dictionary<int, int> newIds)
        {
            _visualization.RenumberElements(newIds);
        }
        public virtual void RenumberVisualizationSurfaces(int[] orderedSurfaceIds)
        {
            _visualization.RenumberSurfaces(orderedSurfaceIds);
        }
        public virtual void RenumberVisualizationEdges(int[] orderedEdgesIds)
        {
            _visualization.RenumberEdges(orderedEdgesIds);
        }
        /// <summary>
        /// Fills what the part is missing from the metadata a geometry part gave, already renumbered to the
        /// faces and edges of this part. Without it the ids are taken to be the same in both parts.
        /// </summary>
        public void FillMissingGeometryDataFrom(BasePart sourcePart, GeometryMetadata matchedMetadata)
        {
            if (matchedMetadata == null)
            {
                FillMissingGeometryDataFrom(sourcePart);
                return;
            }
            if (_geometryMetadata == null)
                _geometryMetadata = new GeometryMetadata(matchedMetadata.FormatVersion,
                                                         matchedMetadata.CoordinateSystem,
                                                         matchedMetadata.LengthUnit);
            _geometryMetadata.FillMissingFrom(matchedMetadata, true, true, true);
            FillMissingMassPropertiesFrom(sourcePart);
        }
        public void FillMissingGeometryDataFrom(BasePart sourcePart)
        {
            GeometryMetadata source = sourcePart?.GeometryMetadata;
            if (source == null) return;
            // Entity IDs only have meaning when both parts still have the same topology counts.
            int sourceFaceCount = sourcePart.Visualization?.FaceCount ?? 0;
            int targetFaceCount = Visualization?.FaceCount ?? 0;
            int sourceEdgeCount = sourcePart.Visualization?.EdgeCount ?? 0;
            int targetEdgeCount = Visualization?.EdgeCount ?? 0;
            bool facesMatch = sourceFaceCount > 0 && sourceFaceCount == targetFaceCount &&
                              source.GetEntities("face").Length == sourceFaceCount;
            bool edgesMatch = sourceEdgeCount > 0 && sourceEdgeCount == targetEdgeCount &&
                              source.GetEntities("edge").Length == sourceEdgeCount;
            if (_geometryMetadata == null)
                _geometryMetadata = new GeometryMetadata(source.FormatVersion, source.CoordinateSystem,
                                                         source.LengthUnit);
            _geometryMetadata.FillMissingFrom(source, facesMatch, edgesMatch, true);
            FillMissingMassPropertiesFrom(sourcePart);
            //
            FillMissingValues(Visualization?.FaceAreas, sourcePart.Visualization?.FaceAreas, facesMatch);
            FillMissingValues(Visualization?.EdgeLengths, sourcePart.Visualization?.EdgeLengths, edgesMatch);
        }
        private void FillMissingMassPropertiesFrom(BasePart sourcePart)
        {
            // Existing mesh/result measurements remain authoritative. CAD values fill only absent values.
            PartMassProperties sourceMass = sourcePart.MassProperties;
            PartMassProperties targetMass = MassProperties;
            bool massChanged = false;
            if (targetMass.Volume <= 0 && sourceMass.Volume > 0)
            {
                targetMass.Volume = sourceMass.Volume;
                massChanged = true;
            }
            if (targetMass.Area <= 0 && sourceMass.Area > 0)
            {
                targetMass.Area = sourceMass.Area;
                massChanged = true;
            }
            if (targetMass.CenterOfMass == null && sourceMass.CenterOfMass != null)
            {
                targetMass.CenterOfMass = sourceMass.CenterOfMass.ToArray();
                massChanged = true;
            }
            if (targetMass.InertiaMatrixCG == null && sourceMass.InertiaMatrixCG != null)
            {
                targetMass.InertiaMatrixCG = sourceMass.InertiaMatrixCG.Select(row => row.ToArray()).ToArray();
                massChanged = true;
            }
            if (targetMass.InertiaMatrixOrigin == null && sourceMass.InertiaMatrixOrigin != null)
            {
                targetMass.InertiaMatrixOrigin = sourceMass.InertiaMatrixOrigin.Select(row => row.ToArray()).ToArray();
                massChanged = true;
            }
            if (massChanged) MassProperties = targetMass;
        }
        private static void FillMissingValues(double[] target, double[] source, bool topologyMatches)
        {
            if (!topologyMatches || target == null || source == null || target.Length != source.Length) return;
            for (int i = 0; i < target.Length; i++)
            {
                if (target[i] <= 0 && source[i] > 0) target[i] = source[i];
            }
        }
        // Added for usage in graphs
        public int CompareTo(BasePart other)
        {
            if (_boundingBox.GetDiagonal() < other._boundingBox.GetDiagonal()) return 1;
            else if (_boundingBox.GetDiagonal() > other._boundingBox.GetDiagonal()) return -1;
            else return 0;
            //if (System.Diagnostics.Debugger.IsAttached) throw new NotImplementedException();
            //else return 0;
        }
    }
}
