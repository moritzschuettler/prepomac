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
using CaeMesh.Meshing;
using DynamicTypeDescriptor;
using GmshCommon;

namespace CaeMesh
{
    [Serializable]
    public class SweepMesh : GmshSetupItem, ISerializable
    {
        // Variables                                                                                                                
        private int _numberOfGlobalSmoothSteps;                         // ISerializable
        private double[] _direction;                                    // ISerializable
        private double[] _sweepCenter;                                  // ISerializable
        private int[] _sideSurfaceIds;                                  // ISerializable
        private int[][][] _layerGroupEdgeIds;                           // ISerializable


        // Properties                                                                                                               
        public int NumberOfGlobalSmoothSteps
        {
            get { return _numberOfGlobalSmoothSteps; }
            set
            {
                _numberOfGlobalSmoothSteps = value;
                if (_numberOfGlobalSmoothSteps < 0) _numberOfGlobalSmoothSteps = 0;
            }
        }
        public double[] Direction { get { return _direction; } set { _direction = value; } }
        public double[] SweepCenter { get { return _sweepCenter; } set { _sweepCenter = value; } }
        public int[] SideSurfaceIds { get { return _sideSurfaceIds; } set { _sideSurfaceIds = value; } }
        public int[][][] LayerGroupEdgeIds { get { return _layerGroupEdgeIds; } set { _layerGroupEdgeIds = value; } }


        // Constructors                                                                                                             
        public SweepMesh(string name)
            : base(name)
        {
            Reset();
        }
        public SweepMesh(ExtrudeMesh extrudeMesh)
            : base("tmpName")
        {
            CopyFrom(extrudeMesh);
        }
        public SweepMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_numberOfLayerSmoothSteps":
                        break;  // Removed - the layer interior is no longer built by smoothing
                    case "_numberOfGlobalSmoothSteps":
                        _numberOfGlobalSmoothSteps = (int)entry.Value; break;
                    case "_biasType":
                        break;  // Removed - grading now lives on LocalMeshSize
                    case "_biasFactor":
                        break;  // Removed - grading now lives on LocalMeshSize
                    case "_direction":
                        _direction = (double[])entry.Value; break;
                    case "_sweepCenter":
                        _sweepCenter = (double[])entry.Value; break;
                    case "_sideSurfaceIds":
                        _sideSurfaceIds = (int[])entry.Value; break;
                    case "_layerGroupEdgeIds":
                        _layerGroupEdgeIds = (int[][][])entry.Value; break;
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
            // The layer interior comes out of the morph already smooth; extra relaxation only
            // erodes the grading the source mesh carries, so it is off unless it is asked for
            _numberOfGlobalSmoothSteps = 0;
            _direction = null;
            _sweepCenter = null;
            _sideSurfaceIds = null;
            _layerGroupEdgeIds = null;
        }
        public void CopyFrom(SweepMesh sweepMesh)
        {
            base.CopyFrom(sweepMesh);
            //
            _numberOfGlobalSmoothSteps = sweepMesh._numberOfGlobalSmoothSteps;
            // The guards belong on the source: a null field here is exactly the case that has to be
            // filled in, and a null field there is the case that must not be dereferenced
            _direction = sweepMesh._direction?.ToArray();
            _sweepCenter = sweepMesh._sweepCenter?.ToArray();
            _sideSurfaceIds = sweepMesh._sideSurfaceIds?.ToArray();
            _layerGroupEdgeIds = null;
            if (sweepMesh._layerGroupEdgeIds != null)
            {
                _layerGroupEdgeIds = new int[sweepMesh._layerGroupEdgeIds.Length][][];
                for (int i = 0; i < sweepMesh._layerGroupEdgeIds.Length; i++)
                {
                    _layerGroupEdgeIds[i] = new int[sweepMesh._layerGroupEdgeIds[i].Length][];
                    for (int j = 0; j < sweepMesh._layerGroupEdgeIds[i].Length; j++)
                    {
                        _layerGroupEdgeIds[i][j] = sweepMesh._layerGroupEdgeIds[i][j].ToArray();
                    }
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_numberOfGlobalSmoothSteps", _numberOfGlobalSmoothSteps, typeof(int));
            info.AddValue("_direction", _direction, typeof(double[]));
            info.AddValue("_sweepCenter", _sweepCenter, typeof(double[]));
            info.AddValue("_sideSurfaceIds", _sideSurfaceIds, typeof(int[]));
            info.AddValue("_layerGroupEdgeIds", _layerGroupEdgeIds, typeof(int[][][]));
        }
    }
}
