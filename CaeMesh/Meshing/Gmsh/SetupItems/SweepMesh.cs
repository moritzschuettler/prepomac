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
        private int[][] _blockSourceSurfaceIds;                         // ISerializable
        private int[][] _blockSideSurfaceIds;                           // ISerializable
        private int[][] _blockTargetSurfaceIds;                         // ISerializable
        private int[][][] _layerGroupEdgeIds;                           // ISerializable
        private double[][] _blockArrowStarts;                           // ISerializable
        private double[][] _blockArrowDirections;                       // ISerializable
        private SweepSourceMesherEnum _sourceMesher;                    // ISerializable
        private double _quadWildAlpha;                                  // ISerializable
        private QuadWildFlowProfileEnum _quadWildFlowProfile;           // ISerializable
        private bool _quadWildSmooth;                                   // ISerializable
        private bool _quadWildFallbackToGmsh;                           // ISerializable
        private SweepSymmetryEnum _symmetry;                            // ISerializable
        private double[][] _symmetryPlanes;                             // ISerializable
        private int[][] _symmetryPartIds;                               // ISerializable


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
        public int[][] BlockSourceSurfaceIds { get { return _blockSourceSurfaceIds; } set { _blockSourceSurfaceIds = value; } }
        public int[][] BlockSideSurfaceIds { get { return _blockSideSurfaceIds; } set { _blockSideSurfaceIds = value; } }
        public int[][] BlockTargetSurfaceIds { get { return _blockTargetSurfaceIds; } set { _blockTargetSurfaceIds = value; } }
        public int[][][] LayerGroupEdgeIds { get { return _layerGroupEdgeIds; } set { _layerGroupEdgeIds = value; } }
        public double[][] BlockArrowStarts { get { return _blockArrowStarts; } set { _blockArrowStarts = value; } }
        public double[][] BlockArrowDirections { get { return _blockArrowDirections; } set { _blockArrowDirections = value; } }
        public SweepSourceMesherEnum SourceMesher { get { return _sourceMesher; } set { _sourceMesher = value; } }
        /// <summary>
        /// The 2D meshing algorithm as the user chooses it: QuadWild for a sweep whose sources QuadWild meshes, else
        /// the gmsh algorithm. Choosing QuadWild keeps the gmsh algorithm, which still meshes the other surfaces.
        /// </summary>
        public SweepAlgorithmMesh2DEnum SweepAlgorithmMesh2D
        {
            get
            {
                if (_sourceMesher == SweepSourceMesherEnum.QuadWild) return SweepAlgorithmMesh2DEnum.QuadWild;
                return (SweepAlgorithmMesh2DEnum)AlgorithmMesh2D;
            }
            set
            {
                if (value == SweepAlgorithmMesh2DEnum.QuadWild) _sourceMesher = SweepSourceMesherEnum.QuadWild;
                else
                {
                    _sourceMesher = SweepSourceMesherEnum.Gmsh;
                    AlgorithmMesh2D = (GmshAlgorithmMesh2DEnum)value;
                }
            }
        }
        public double QuadWildAlpha
        {
            get { return _quadWildAlpha; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > 1)
                    throw new CaeException("The QuadWild alpha value must be a finite number from 0 to 1.");
                _quadWildAlpha = value;
            }
        }
        public QuadWildFlowProfileEnum QuadWildFlowProfile
        {
            get { return _quadWildFlowProfile; }
            set { _quadWildFlowProfile = value; }
        }
        public bool QuadWildSmooth { get { return _quadWildSmooth; } set { _quadWildSmooth = value; } }
        public bool QuadWildFallbackToGmsh
        {
            get { return _quadWildFallbackToGmsh; }
            set { _quadWildFallbackToGmsh = value; }
        }
        /// <summary>
        /// Whether a compound whose cells are mirror images across the planes it is split on is meshed one symmetric
        /// piece at a time: the cells of one piece are swept and their mesh mirrored onto the cells of the others.
        /// </summary>
        public SweepSymmetryEnum Symmetry { get { return _symmetry; } set { _symmetry = value; } }
        /// <summary>
        /// The mirror planes the detection chose, a point and a unit normal each: x, y, z, nx, ny, nz.
        /// </summary>
        public double[][] SymmetryPlanes { get { return _symmetryPlanes; } set { _symmetryPlanes = value; } }
        /// <summary>
        /// One row per cell that is not swept but mirrored: its part id, the part id of the swept cell it mirrors, and
        /// the planes it is mirrored across as bits of the indices into SymmetryPlanes.
        /// </summary>
        public int[][] SymmetryPartIds { get { return _symmetryPartIds; } set { _symmetryPartIds = value; } }
        public bool HasSymmetry
        {
            get { return _symmetryPlanes != null && _symmetryPlanes.Length > 0 && _symmetryPartIds != null &&
                         _symmetryPartIds.Length > 0; }
        }


        // Constructors                                                                                                             
        public SweepMesh(string name)
            : base(name)
        {
            Reset();
        }
        public SweepMesh(ExtrudeMesh extrudeMesh)
            : base("tmpName")
        {
            SetQuadWildDefaults();
            CopyFrom(extrudeMesh);
        }
        public SweepMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // An item written before the source mesher could be chosen was meshed with gmsh
            SetQuadWildDefaults();
            // An item written before the symmetry could be used meshed every cell, and keeps doing so
            _symmetry = SweepSymmetryEnum.Off;
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
                        break;  // Removed - every block carries an arrow of its own
                    case "_sweepCenter":
                        break;  // Removed - every block carries an arrow of its own
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
                    case "_sourceMesher":
                        _sourceMesher = (SweepSourceMesherEnum)entry.Value; break;
                    case "_quadWildAlpha":
                        _quadWildAlpha = (double)entry.Value; break;
                    case "_quadWildFlowProfile":
                        _quadWildFlowProfile = (QuadWildFlowProfileEnum)entry.Value; break;
                    case "_quadWildSmooth":
                        _quadWildSmooth = (bool)entry.Value; break;
                    case "_quadWildFallbackToGmsh":
                        _quadWildFallbackToGmsh = (bool)entry.Value; break;
                    case "_symmetry":
                        _symmetry = (SweepSymmetryEnum)entry.Value; break;
                    case "_symmetryPlanes":
                        _symmetryPlanes = (double[][])entry.Value; break;
                    case "_symmetryPartIds":
                        _symmetryPartIds = (int[][])entry.Value; break;
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
            // A sweep exists to give a structured solid mesh, and the elements it builds are the elements
            // of the source carried along the sweep lines: a triangle gives a wedge and a quadrangle a
            // hexahedron. The source is therefore recombined, which halves the element count and leaves
            // the minimum scaled Jacobian better on most of the test models and no worse than 0.01 on any.
            // The algorithm has to be the simple one. Every other way of making the source quadrangular -
            // Blossom, either full-quad variant, Frontal-Delaunay for quads, packing of parallelograms and
            // the quasi-structured quad algorithm - rebuilds the source enough to change the node counts
            // along its boundary, and the walls pinned to those counts are then refused by gmsh as
            // transfinite surfaces with non-matching sides. Simple recombination joins triangles where it
            // can and leaves the boundary alone
            AlgorithmRecombine = GmshAlgorithmRecombineEnum.Simple;
            // The layer interior comes out of the morph already smooth; extra relaxation only
            // erodes the grading the source mesh carries, so it is off unless it is asked for
            _numberOfGlobalSmoothSteps = 0;
            // The edges carried across a prism to follow an imprint are scaffolding, dropped from the mesh
            // unless they are asked for
            _keepImprintEdges = false;
            // A new item finds its own source surfaces; an item written before the field existed carries a
            // surface selection and is left at the enum value zero, which is SelectedSurfaces
            _sourceType = SweepSourceTypeEnum.Automatic;
            _blockSourceSurfaceIds = null;
            _blockSideSurfaceIds = null;
            _blockTargetSurfaceIds = null;
            _layerGroupEdgeIds = null;
            _blockArrowStarts = null;
            _blockArrowDirections = null;
            SetQuadWildDefaults();
            _symmetry = SweepSymmetryEnum.Automatic;
            _symmetryPlanes = null;
            _symmetryPartIds = null;
        }
        private void SetQuadWildDefaults()
        {
            _sourceMesher = SweepSourceMesherEnum.Gmsh;
            _quadWildAlpha = 0.005;
            _quadWildFlowProfile = QuadWildFlowProfileEnum.Aligned;
            _quadWildSmooth = true;
            _quadWildFallbackToGmsh = true;
        }
        public void CopyFrom(SweepMesh sweepMesh)
        {
            base.CopyFrom(sweepMesh);
            //
            _sourceType = sweepMesh._sourceType;
            _numberOfGlobalSmoothSteps = sweepMesh._numberOfGlobalSmoothSteps;
            _keepImprintEdges = sweepMesh._keepImprintEdges;
            _blockSourceSurfaceIds = CopyBlockIds(sweepMesh._blockSourceSurfaceIds);
            _blockSideSurfaceIds = CopyBlockIds(sweepMesh._blockSideSurfaceIds);
            _blockTargetSurfaceIds = CopyBlockIds(sweepMesh._blockTargetSurfaceIds);
            _blockArrowStarts = CopyBlockCoors(sweepMesh._blockArrowStarts);
            _blockArrowDirections = CopyBlockCoors(sweepMesh._blockArrowDirections);
            _sourceMesher = sweepMesh._sourceMesher;
            _quadWildAlpha = sweepMesh._quadWildAlpha;
            _quadWildFlowProfile = sweepMesh._quadWildFlowProfile;
            _quadWildSmooth = sweepMesh._quadWildSmooth;
            _quadWildFallbackToGmsh = sweepMesh._quadWildFallbackToGmsh;
            _symmetry = sweepMesh._symmetry;
            _symmetryPlanes = CopyBlockCoors(sweepMesh._symmetryPlanes);
            _symmetryPartIds = CopyBlockIds(sweepMesh._symmetryPartIds);
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
            info.AddValue("_blockSourceSurfaceIds", _blockSourceSurfaceIds, typeof(int[][]));
            info.AddValue("_blockSideSurfaceIds", _blockSideSurfaceIds, typeof(int[][]));
            info.AddValue("_blockTargetSurfaceIds", _blockTargetSurfaceIds, typeof(int[][]));
            info.AddValue("_layerGroupEdgeIds", _layerGroupEdgeIds, typeof(int[][][]));
            info.AddValue("_blockArrowStarts", _blockArrowStarts, typeof(double[][]));
            info.AddValue("_blockArrowDirections", _blockArrowDirections, typeof(double[][]));
            info.AddValue("_sourceMesher", _sourceMesher, typeof(SweepSourceMesherEnum));
            info.AddValue("_quadWildAlpha", _quadWildAlpha, typeof(double));
            info.AddValue("_quadWildFlowProfile", _quadWildFlowProfile, typeof(QuadWildFlowProfileEnum));
            info.AddValue("_quadWildSmooth", _quadWildSmooth, typeof(bool));
            info.AddValue("_quadWildFallbackToGmsh", _quadWildFallbackToGmsh, typeof(bool));
            info.AddValue("_symmetry", _symmetry, typeof(SweepSymmetryEnum));
            info.AddValue("_symmetryPlanes", _symmetryPlanes, typeof(double[][]));
            info.AddValue("_symmetryPartIds", _symmetryPartIds, typeof(int[][]));
        }
    }
}
