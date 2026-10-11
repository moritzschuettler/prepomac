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
    /// <summary>
    /// Meshes the selected parts by Netgen - solid parts by tetrahedra and shell parts by triangles or quad-dominated
    /// elements. The mesh size comes from the meshing parameters; the item carries the settings that apply to Netgen
    /// alone. It is what a part is given when the structured mesh its geometry supports is not wanted.
    /// </summary>
    [Serializable]
    public class NetgenMesh : MeshSetupItem, ISerializable
    {
        // Variables
        private int _optimizeSteps2D;               //ISerializable
        private int _optimizeSteps3D;               //ISerializable
        private bool _quadDominated;                //ISerializable
        private TransfiniteThreeSidedEnum _transfiniteThreeSided;   //ISerializable
        private bool _transfiniteFourSided;         //ISerializable
        private double _transfiniteAngleDeg;        //ISerializable


        // Properties
        public override MesherTypeEnum MesherType { get { return MesherTypeEnum.Netgen; } }
        public int OptimizeSteps2D
        {
            get { return _optimizeSteps2D; }
            set
            {
                if (value < 0) throw new Exception("The value must be larger or equal to 0.");
                _optimizeSteps2D = value;
            }
        }
        public int OptimizeSteps3D
        {
            get { return _optimizeSteps3D; }
            set
            {
                if (value < 0) throw new Exception("The value must be larger or equal to 0.");
                _optimizeSteps3D = value;
            }
        }
        public bool QuadDominated { get { return _quadDominated; } set { _quadDominated = value; } }
        public TransfiniteThreeSidedEnum TransfiniteThreeSided
        {
            get { return _transfiniteThreeSided; }
            set { _transfiniteThreeSided = value; }
        }
        public bool IsTransfiniteThreeSided { get { return _transfiniteThreeSided != TransfiniteThreeSidedEnum.No; } }
        public bool TransfiniteFourSided
        {
            get { return _transfiniteFourSided; }
            set { _transfiniteFourSided = value; }
        }
        public double TransfiniteAngleDeg
        {
            get { return _transfiniteAngleDeg; }
            set
            {
                if (value <= 0) throw new Exception("The value must be larger than 0.");
                if (value >= 180) throw new Exception("The value must be smaller than 180.");
                _transfiniteAngleDeg = value;
            }
        }


        // Constructors
        public NetgenMesh(string name)
            : base(name)
        {
            Reset();
        }
        public NetgenMesh(NetgenMesh netgenMesh)
            : base("tmpName")
        {
            CopyFrom(netgenMesh);
        }
        public NetgenMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Compatibility for the Tetrahedral Netgen item, which carried no values
            SetDefaultValues();
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_optimizeSteps2D":
                        _optimizeSteps2D = (int)entry.Value; break;
                    case "_optimizeSteps3D":
                        _optimizeSteps3D = (int)entry.Value; break;
                    case "_quadDominated":
                        _quadDominated = (bool)entry.Value; break;
                    case "_transfiniteFaces":
                        // The 4-sided faces of an earlier version, which had no 3-sided ones
                        _transfiniteFourSided = (NetgenTransfiniteFacesEnum)entry.Value != NetgenTransfiniteFacesEnum.None;
                        break;
                    case "_transfiniteThreeSided":
                        // Compatibility for version v2.6.4: a switch, on for the three blocks of quads made then
                        if (entry.Value is bool threeSided)
                            _transfiniteThreeSided = threeSided ? TransfiniteThreeSidedEnum.Mapped :
                                                                  TransfiniteThreeSidedEnum.No;
                        else _transfiniteThreeSided = (TransfiniteThreeSidedEnum)entry.Value;
                        break;
                    case "_transfiniteFourSided":
                        _transfiniteFourSided = (bool)entry.Value; break;
                    case "_transfiniteAngleDeg":
                        _transfiniteAngleDeg = (double)entry.Value; break;
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
            SetDefaultValues();
        }
        private void SetDefaultValues()
        {
            _optimizeSteps2D = 3;
            _optimizeSteps3D = 3;
            _quadDominated = false;
            _transfiniteThreeSided = TransfiniteThreeSidedEnum.No;
            _transfiniteFourSided = false;
            _transfiniteAngleDeg = 135;
        }
        public void CopyFrom(NetgenMesh netgenMesh)
        {
            base.CopyFrom(netgenMesh);
            //
            _optimizeSteps2D = netgenMesh._optimizeSteps2D;
            _optimizeSteps3D = netgenMesh._optimizeSteps3D;
            _quadDominated = netgenMesh._quadDominated;
            _transfiniteThreeSided = netgenMesh._transfiniteThreeSided;
            _transfiniteFourSided = netgenMesh._transfiniteFourSided;
            _transfiniteAngleDeg = netgenMesh._transfiniteAngleDeg;
        }
        /// <summary>
        /// Takes the Netgen settings the meshing parameters carried before they became the settings of this item.
        /// </summary>
        public void CopyNetgenValuesFrom(MeshingParameters meshingParameters)
        {
            _optimizeSteps2D = meshingParameters.OptimizeSteps2D;
            _optimizeSteps3D = meshingParameters.OptimizeSteps3D;
            _quadDominated = meshingParameters.QuadDominated;
            _transfiniteThreeSided = meshingParameters.TransfiniteThreeSided;
            _transfiniteFourSided = meshingParameters.TransfiniteFourSided;
            _transfiniteAngleDeg = meshingParameters.TransfiniteAngleDeg;
        }
        /// <summary>
        /// Hands the settings to the meshing parameters, which is what the Netgen mesher is driven by.
        /// </summary>
        public void CopyNetgenValuesTo(MeshingParameters meshingParameters)
        {
            meshingParameters.OptimizeSteps2D = _optimizeSteps2D;
            meshingParameters.OptimizeSteps3D = _optimizeSteps3D;
            meshingParameters.QuadDominated = _quadDominated;
            meshingParameters.TransfiniteThreeSided = _transfiniteThreeSided;
            meshingParameters.TransfiniteFourSided = _transfiniteFourSided;
            meshingParameters.TransfiniteAngleDeg = _transfiniteAngleDeg;
        }
        public bool HasSameNetgenValues(NetgenMesh netgenMesh)
        {
            return _optimizeSteps2D == netgenMesh._optimizeSteps2D &&
                   _optimizeSteps3D == netgenMesh._optimizeSteps3D &&
                   _quadDominated == netgenMesh._quadDominated &&
                   _transfiniteThreeSided == netgenMesh._transfiniteThreeSided &&
                   _transfiniteFourSided == netgenMesh._transfiniteFourSided &&
                   _transfiniteAngleDeg == netgenMesh._transfiniteAngleDeg;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_optimizeSteps2D", _optimizeSteps2D, typeof(int));
            info.AddValue("_optimizeSteps3D", _optimizeSteps3D, typeof(int));
            info.AddValue("_quadDominated", _quadDominated, typeof(bool));
            info.AddValue("_transfiniteThreeSided", _transfiniteThreeSided, typeof(TransfiniteThreeSidedEnum));
            info.AddValue("_transfiniteFourSided", _transfiniteFourSided, typeof(bool));
            info.AddValue("_transfiniteAngleDeg", _transfiniteAngleDeg, typeof(double));
        }
    }
}
