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
using DynamicTypeDescriptor;
using GmshCommon;

namespace CaeMesh
{
    [Serializable]
    public class SweepMesh : GmshSetupItem, ISerializable
    {
        // Variables                                                                                                                
        private SweepSourceTypeEnum _sourceType;                        // ISerializable
        private int _numberOfGlobalSmoothSteps;                         // ISerializable
        private double[] _direction;                                    // ISerializable
        private double[] _sweepCenter;                                  // ISerializable
        private int[][] _blockSourceSurfaceIds;                         // ISerializable
        private int[][] _blockSideSurfaceIds;                           // ISerializable
        private int[][] _blockTargetSurfaceIds;                         // ISerializable
        private int[][][] _layerGroupEdgeIds;                           // ISerializable
        private double[][] _blockArrowStarts;                           // ISerializable
        private double[][] _blockArrowDirections;                       // ISerializable


        // Properties                                                                                                               
        public SweepSourceTypeEnum SourceType { get { return _sourceType; } set { _sourceType = value; } }
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
        public int[][] BlockSourceSurfaceIds { get { return _blockSourceSurfaceIds; } set { _blockSourceSurfaceIds = value; } }
        public int[][] BlockSideSurfaceIds { get { return _blockSideSurfaceIds; } set { _blockSideSurfaceIds = value; } }
        public int[][] BlockTargetSurfaceIds { get { return _blockTargetSurfaceIds; } set { _blockTargetSurfaceIds = value; } }
        public int[][][] LayerGroupEdgeIds { get { return _layerGroupEdgeIds; } set { _layerGroupEdgeIds = value; } }
        public double[][] BlockArrowStarts { get { return _blockArrowStarts; } set { _blockArrowStarts = value; } }
        public double[][] BlockArrowDirections { get { return _blockArrowDirections; } set { _blockArrowDirections = value; } }


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
                    case "_sourceType":
                        _sourceType = (SweepSourceTypeEnum)entry.Value; break;
                    case "_numberOfGlobalSmoothSteps":
                        _numberOfGlobalSmoothSteps = (int)entry.Value; break;
                    case "_direction":
                        _direction = (double[])entry.Value; break;
                    case "_sweepCenter":
                        _sweepCenter = (double[])entry.Value; break;
                    case "_sideSurfaceIds":
                        {
                            // Written before the blocks were recorded: a single block, whose walls these are
                            int[] sideSurfaceIds = (int[])entry.Value;
                            if (sideSurfaceIds != null) _blockSideSurfaceIds = new int[][] { sideSurfaceIds };
                            break;
                        }
                    case "_blockSourceSurfaceIds":
                        _blockSourceSurfaceIds = (int[][])entry.Value; break;
                    case "_blockSideSurfaceIds":
                        _blockSideSurfaceIds = (int[][])entry.Value; break;
                    case "_blockTargetSurfaceIds":
                        _blockTargetSurfaceIds = (int[][])entry.Value; break;
                    case "_layerGroupEdgeIds":
                        _layerGroupEdgeIds = (int[][][])entry.Value; break;
                    case "_blockArrowStarts":
                        _blockArrowStarts = (double[][])entry.Value; break;
                    case "_blockArrowDirections":
                        _blockArrowDirections = (double[][])entry.Value; break;
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
            _sourceType = SweepSourceTypeEnum.SelectedSurfaces;
            _direction = null;
            _sweepCenter = null;
            _blockSourceSurfaceIds = null;
            _blockSideSurfaceIds = null;
            _blockTargetSurfaceIds = null;
            _layerGroupEdgeIds = null;
            _blockArrowStarts = null;
            _blockArrowDirections = null;
        }
        public void CopyFrom(SweepMesh sweepMesh)
        {
            base.CopyFrom(sweepMesh);
            //
            _sourceType = sweepMesh._sourceType;
            _numberOfGlobalSmoothSteps = sweepMesh._numberOfGlobalSmoothSteps;
            // The guards belong on the source: a null field here is exactly the case that has to be
            // filled in, and a null field there is the case that must not be dereferenced
            _direction = sweepMesh._direction?.ToArray();
            _sweepCenter = sweepMesh._sweepCenter?.ToArray();
            _blockSourceSurfaceIds = CopyBlockIds(sweepMesh._blockSourceSurfaceIds);
            _blockSideSurfaceIds = CopyBlockIds(sweepMesh._blockSideSurfaceIds);
            _blockTargetSurfaceIds = CopyBlockIds(sweepMesh._blockTargetSurfaceIds);
            _blockArrowStarts = CopyBlockCoors(sweepMesh._blockArrowStarts);
            _blockArrowDirections = CopyBlockCoors(sweepMesh._blockArrowDirections);
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
        private static int[][] CopyBlockIds(int[][] blockIds)
        {
            if (blockIds == null) return null;
            //
            int[][] copy = new int[blockIds.Length][];
            for (int i = 0; i < blockIds.Length; i++) copy[i] = blockIds[i].ToArray();
            return copy;
        }
        private static double[][] CopyBlockCoors(double[][] blockCoors)
        {
            if (blockCoors == null) return null;
            //
            double[][] copy = new double[blockCoors.Length][];
            for (int i = 0; i < blockCoors.Length; i++) copy[i] = blockCoors[i].ToArray();
            return copy;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_sourceType", _sourceType, typeof(SweepSourceTypeEnum));
            info.AddValue("_numberOfGlobalSmoothSteps", _numberOfGlobalSmoothSteps, typeof(int));
            info.AddValue("_direction", _direction, typeof(double[]));
            info.AddValue("_sweepCenter", _sweepCenter, typeof(double[]));
            info.AddValue("_blockSourceSurfaceIds", _blockSourceSurfaceIds, typeof(int[][]));
            info.AddValue("_blockSideSurfaceIds", _blockSideSurfaceIds, typeof(int[][]));
            info.AddValue("_blockTargetSurfaceIds", _blockTargetSurfaceIds, typeof(int[][]));
            info.AddValue("_layerGroupEdgeIds", _layerGroupEdgeIds, typeof(int[][][]));
            info.AddValue("_blockArrowStarts", _blockArrowStarts, typeof(double[][]));
            info.AddValue("_blockArrowDirections", _blockArrowDirections, typeof(double[][]));
        }
    }
}
