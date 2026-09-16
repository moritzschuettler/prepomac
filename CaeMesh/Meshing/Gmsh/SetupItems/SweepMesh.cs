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
        private bool _keepImprintEdges;                                 // ISerializable
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
        public bool KeepImprintEdges { get { return _keepImprintEdges; } set { _keepImprintEdges = value; } }
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
                    case "_keepImprintEdges":
                        _keepImprintEdges = (bool)entry.Value; break;
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
            // The edges carried across a prism to follow an imprint are scaffolding, dropped from the mesh
            // unless they are asked for
            _keepImprintEdges = false;
            // A new item finds its own source surfaces; an item written before the field existed carries a
            // surface selection and is left at the enum value zero, which is SelectedSurfaces
            _sourceType = SweepSourceTypeEnum.Automatic;
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
            _keepImprintEdges = sweepMesh._keepImprintEdges;
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
        /// <summary>
        /// A deep clone of the item keeping only the blocks and the layer edge groups that lie on the given
        /// parts. A sweep may span parts that are meshed one at a time, and the mesher must be given the
        /// blocks of the part it is meshing alone. The clone also protects the item from the mesher:
        /// GmshAPI.RenumberGmshDataByCoor replaces the ids of these arrays with gmsh tags in place, which
        /// would corrupt the live item when the mesher runs in the application's own process.
        /// </summary>
        public SweepMesh GetBlocksOnParts(int[] partIds)
        {
            SweepMesh sweepMesh = this.DeepClone();
            if (_blockSourceSurfaceIds == null) return sweepMesh;
            //
            HashSet<int> keptPartIds = new HashSet<int>(partIds);
            List<int[]> blockSourceSurfaceIds = new List<int[]>();
            List<int[]> blockSideSurfaceIds = new List<int[]>();
            List<int[]> blockTargetSurfaceIds = new List<int[]>();
            List<int[][]> layerGroupEdgeIds = new List<int[][]>();
            List<double[]> blockArrowStarts = new List<double[]>();
            List<double[]> blockArrowDirections = new List<double[]>();
            for (int i = 0; i < sweepMesh._blockSourceSurfaceIds.Length; i++)
            {
                if (!IsOnParts(sweepMesh._blockSourceSurfaceIds[i], keptPartIds)) continue;
                //
                blockSourceSurfaceIds.Add(sweepMesh._blockSourceSurfaceIds[i]);
                blockSideSurfaceIds.Add(sweepMesh._blockSideSurfaceIds[i]);
                blockTargetSurfaceIds.Add(sweepMesh._blockTargetSurfaceIds[i]);
                if (sweepMesh._blockArrowStarts != null) blockArrowStarts.Add(sweepMesh._blockArrowStarts[i]);
                if (sweepMesh._blockArrowDirections != null) blockArrowDirections.Add(sweepMesh._blockArrowDirections[i]);
            }
            if (sweepMesh._layerGroupEdgeIds != null)
            {
                foreach (var layerGroups in sweepMesh._layerGroupEdgeIds)
                {
                    foreach (var edgeIds in layerGroups)
                    {
                        if (!IsOnParts(edgeIds, keptPartIds)) continue;
                        //
                        layerGroupEdgeIds.Add(layerGroups);
                        break;
                    }
                }
            }
            //
            sweepMesh._blockSourceSurfaceIds = blockSourceSurfaceIds.ToArray();
            sweepMesh._blockSideSurfaceIds = blockSideSurfaceIds.ToArray();
            sweepMesh._blockTargetSurfaceIds = blockTargetSurfaceIds.ToArray();
            sweepMesh._layerGroupEdgeIds = layerGroupEdgeIds.ToArray();
            if (sweepMesh._blockArrowStarts != null) sweepMesh._blockArrowStarts = blockArrowStarts.ToArray();
            if (sweepMesh._blockArrowDirections != null) sweepMesh._blockArrowDirections = blockArrowDirections.ToArray();
            //
            return sweepMesh;
        }
        private static bool IsOnParts(int[] gmshTopologyIds, HashSet<int> partIds)
        {
            for (int i = 0; i < gmshTopologyIds.Length; i++)
            {
                if (partIds.Contains(FeMesh.GetPartIdFromGmshTopologyId(gmshTopologyIds[i]))) return true;
            }
            return false;
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
            info.AddValue("_keepImprintEdges", _keepImprintEdges, typeof(bool));
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
