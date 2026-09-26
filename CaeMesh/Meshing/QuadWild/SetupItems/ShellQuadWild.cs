// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;

namespace CaeMesh
{
    [Serializable]
    public class ShellQuadWild : MeshSetupItem, ISerializable
    {
        // Variables
        private MesherTypeEnum _triangulationMesher;                      // ISerializable
        private bool _preview;                                           // ISerializable
        private bool _smooth;                                            // ISerializable
        private double _alpha;                                           // ISerializable
        private QuadWildFlowProfileEnum _flowProfile;                     // ISerializable
        private QuadWildBoundaryLayerEnum _boundaryLayer;                 // ISerializable
        private bool _keepFilesOnFailure;                                // ISerializable
        private bool _debugVisualization;                                // ISerializable
        private bool _oldAlgorithm;                                      // ISerializable
        private bool _pinEdgeNodes;                                      // ISerializable
        private bool _edgeRotations;                                     // ISerializable
        private double _singularityPosition;                             // ISerializable
        private int _countSearch;                                        // ISerializable


        // Properties
        public override MesherTypeEnum MesherType { get { return MesherTypeEnum.QuadWild; } }
        public MesherTypeEnum TriangulationMesher
        {
            get { return _triangulationMesher; }
            set
            {
                if (value != MesherTypeEnum.Gmsh && value != MesherTypeEnum.Netgen && value != MesherTypeEnum.Mmg)
                    throw new ArgumentException("The QuadWild triangulation mesher must be Gmsh, Netgen, or MMG.");
                _triangulationMesher = value;
            }
        }
        public bool Preview { get { return _preview; } set { _preview = value; } }
        public bool Smooth { get { return _smooth; } set { _smooth = value; } }
        public double Alpha { get { return _alpha; } set { _alpha = value; } }
        public QuadWildFlowProfileEnum FlowProfile
        {
            get { return _flowProfile; }
            set { _flowProfile = value; }
        }
        public QuadWildBoundaryLayerEnum BoundaryLayer { get { return _boundaryLayer; } set { _boundaryLayer = value; } }
        public bool KeepFilesOnFailure { get { return _keepFilesOnFailure; } set { _keepFilesOnFailure = value; } }
        public bool DebugVisualization { get { return _debugVisualization; } set { _debugVisualization = value; } }
        public bool OldAlgorithm { get { return _oldAlgorithm; } set { _oldAlgorithm = value; } }
        public bool PinEdgeNodes { get { return _pinEdgeNodes; } set { _pinEdgeNodes = value; } }
        public bool EdgeRotations { get { return _edgeRotations; } set { _edgeRotations = value; } }
        public double SingularityPosition
        {
            get { return _singularityPosition; }
            set { _singularityPosition = value < 0 ? 0 : value; }
        }
        public int CountSearch { get { return _countSearch; } set { _countSearch = value < 0 ? 0 : value; } }


        // Constructors
        public ShellQuadWild(string name)
            : base(name)
        {
            Reset();
        }
        public ShellQuadWild(ShellQuadWild shellQuadWild)
            : base("tmpName")
        {
            CopyFrom(shellQuadWild);
        }
        public ShellQuadWild(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            Reset();
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_triangulationMesher":
                        _triangulationMesher = (MesherTypeEnum)entry.Value; break;
                    case "_preview":
                        _preview = (bool)entry.Value; break;
                    case "_smooth":
                        _smooth = (bool)entry.Value; break;
                    case "_scaleFactor":
                        break;  // Removed - the quad size follows the triangulation
                    case "_alpha":
                        _alpha = (double)entry.Value; break;
                    case "_flowProfile":
                        _flowProfile = (QuadWildFlowProfileEnum)entry.Value; break;
                    case "_boundaryLayer":
                        _boundaryLayer = (QuadWildBoundaryLayerEnum)entry.Value; break;
                    case "_keepFilesOnFailure":
                        _keepFilesOnFailure = (bool)entry.Value; break;
                    case "_debugVisualization":
                        _debugVisualization = (bool)entry.Value; break;
                    case "_oldAlgorithm":
                        _oldAlgorithm = (bool)entry.Value; break;
                    case "_pinEdgeNodes":
                        _pinEdgeNodes = (bool)entry.Value; break;
                    case "_edgeRotations":
                        _edgeRotations = (bool)entry.Value; break;
                    case "_singularityPosition":
                        _singularityPosition = (double)entry.Value; break;
                    case "_countSearch":
                        _countSearch = (int)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public override void Reset()
        {
            base.Reset();
            //
            _triangulationMesher = MesherTypeEnum.Gmsh;
            _preview = false;
            _smooth = true;
            _alpha = 0.005;
            _flowProfile = QuadWildFlowProfileEnum.Aligned;
            _boundaryLayer = QuadWildBoundaryLayerEnum.None;
            _keepFilesOnFailure = false;
            _debugVisualization = false;
            _oldAlgorithm = false;
            _pinEdgeNodes = false;
            _edgeRotations = false;
            _singularityPosition = 0;
            _countSearch = 0;
        }
        public void CopyFrom(ShellQuadWild shellQuadWild)
        {
            base.CopyFrom(shellQuadWild);
            //
            _triangulationMesher = shellQuadWild._triangulationMesher;
            _preview = shellQuadWild._preview;
            _smooth = shellQuadWild._smooth;
            _alpha = shellQuadWild._alpha;
            _flowProfile = shellQuadWild._flowProfile;
            _boundaryLayer = shellQuadWild._boundaryLayer;
            _keepFilesOnFailure = shellQuadWild._keepFilesOnFailure;
            _debugVisualization = shellQuadWild._debugVisualization;
            _oldAlgorithm = shellQuadWild._oldAlgorithm;
            _pinEdgeNodes = shellQuadWild._pinEdgeNodes;
            _edgeRotations = shellQuadWild._edgeRotations;
            _singularityPosition = shellQuadWild._singularityPosition;
            _countSearch = shellQuadWild._countSearch;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_triangulationMesher", _triangulationMesher, typeof(MesherTypeEnum));
            info.AddValue("_preview", _preview, typeof(bool));
            info.AddValue("_smooth", _smooth, typeof(bool));
            info.AddValue("_alpha", _alpha, typeof(double));
            info.AddValue("_flowProfile", _flowProfile, typeof(QuadWildFlowProfileEnum));
            info.AddValue("_boundaryLayer", _boundaryLayer, typeof(QuadWildBoundaryLayerEnum));
            info.AddValue("_keepFilesOnFailure", _keepFilesOnFailure, typeof(bool));
            info.AddValue("_debugVisualization", _debugVisualization, typeof(bool));
            info.AddValue("_oldAlgorithm", _oldAlgorithm, typeof(bool));
            info.AddValue("_pinEdgeNodes", _pinEdgeNodes, typeof(bool));
            info.AddValue("_edgeRotations", _edgeRotations, typeof(bool));
            info.AddValue("_singularityPosition", _singularityPosition, typeof(double));
            info.AddValue("_countSearch", _countSearch, typeof(int));
        }
    }
}
