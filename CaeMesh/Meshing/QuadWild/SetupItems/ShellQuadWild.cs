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
        private double _scaleFactor;                                     // ISerializable
        private double _alpha;                                           // ISerializable
        private QuadWildFlowProfileEnum _flowProfile;                     // ISerializable
        private bool _keepFilesOnFailure;                                // ISerializable


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
        public double ScaleFactor
        {
            get { return _scaleFactor; }
            set { _scaleFactor = value < 1E-3 ? 1E-3 : value; }
        }
        public double Alpha { get { return _alpha; } set { _alpha = value; } }
        public QuadWildFlowProfileEnum FlowProfile
        {
            get { return _flowProfile; }
            set { _flowProfile = value; }
        }
        public bool KeepFilesOnFailure { get { return _keepFilesOnFailure; } set { _keepFilesOnFailure = value; } }


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
                        _scaleFactor = (double)entry.Value; break;
                    case "_alpha":
                        _alpha = (double)entry.Value; break;
                    case "_flowProfile":
                        _flowProfile = (QuadWildFlowProfileEnum)entry.Value; break;
                    case "_keepFilesOnFailure":
                        _keepFilesOnFailure = (bool)entry.Value; break;
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
            _scaleFactor = 1;
            _alpha = 0.005;
            _flowProfile = QuadWildFlowProfileEnum.Aligned;
            _keepFilesOnFailure = false;
        }
        public void CopyFrom(ShellQuadWild shellQuadWild)
        {
            base.CopyFrom(shellQuadWild);
            //
            _triangulationMesher = shellQuadWild._triangulationMesher;
            _preview = shellQuadWild._preview;
            _smooth = shellQuadWild._smooth;
            _scaleFactor = shellQuadWild._scaleFactor;
            _alpha = shellQuadWild._alpha;
            _flowProfile = shellQuadWild._flowProfile;
            _keepFilesOnFailure = shellQuadWild._keepFilesOnFailure;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_triangulationMesher", _triangulationMesher, typeof(MesherTypeEnum));
            info.AddValue("_preview", _preview, typeof(bool));
            info.AddValue("_smooth", _smooth, typeof(bool));
            info.AddValue("_scaleFactor", _scaleFactor, typeof(double));
            info.AddValue("_alpha", _alpha, typeof(double));
            info.AddValue("_flowProfile", _flowProfile, typeof(QuadWildFlowProfileEnum));
            info.AddValue("_keepFilesOnFailure", _keepFilesOnFailure, typeof(bool));
        }
    }
}
