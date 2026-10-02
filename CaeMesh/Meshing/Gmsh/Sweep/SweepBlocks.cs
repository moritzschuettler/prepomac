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

namespace CaeMesh
{
    /// <summary>
    /// The one-to-one blocks a group of parts is swept in, in gmsh numbering. A group is swept the same way
    /// whichever setup item asks for it, so the blocks are held apart from the item that carries them.
    /// </summary>
    public class SweepBlocks
    {
        // Variables
        private int[][] _sourceSurfaceIds;
        private int[][] _sideSurfaceIds;
        private int[][] _targetSurfaceIds;
        private int[][][] _layerGroupEdgeIds;
        private double[][] _arrowStarts;
        private double[][] _arrowDirections;
        private double[][] _revolveAxes;


        // Properties
        public int[][] SourceSurfaceIds { get { return _sourceSurfaceIds; } }
        public int[][] SideSurfaceIds { get { return _sideSurfaceIds; } }
        public int[][] TargetSurfaceIds { get { return _targetSurfaceIds; } }
        public int[][][] LayerGroupEdgeIds { get { return _layerGroupEdgeIds; } }
        public double[][] ArrowStarts { get { return _arrowStarts; } }
        public double[][] ArrowDirections { get { return _arrowDirections; } }
        /// <summary>
        /// Per block, the axis and the angle a revolved block turns its source through, or null for a block that is
        /// swept: x, y, z, dx, dy, dz, angle in degrees, the point without the offset of an exploded view.
        /// </summary>
        public double[][] RevolveAxes { get { return _revolveAxes; } }
        public bool HasRevolvedBlocks { get { return _revolveAxes.Any(axis => axis != null); } }


        // Constructors
        public SweepBlocks(int[][] sourceSurfaceIds, int[][] sideSurfaceIds, int[][] targetSurfaceIds,
                           int[][][] layerGroupEdgeIds, double[][] arrowStarts, double[][] arrowDirections,
                           double[][] revolveAxes = null)
        {
            _sourceSurfaceIds = sourceSurfaceIds;
            _sideSurfaceIds = sideSurfaceIds;
            _targetSurfaceIds = targetSurfaceIds;
            _layerGroupEdgeIds = layerGroupEdgeIds;
            _arrowStarts = arrowStarts;
            _arrowDirections = arrowDirections;
            _revolveAxes = revolveAxes != null ? revolveAxes : new double[sourceSurfaceIds.Length][];
        }


        // Methods
        public void CopyToSweepMesh(SweepMesh sweepMesh)
        {
            sweepMesh.BlockSourceSurfaceIds = _sourceSurfaceIds;
            sweepMesh.BlockSideSurfaceIds = _sideSurfaceIds;
            sweepMesh.BlockTargetSurfaceIds = _targetSurfaceIds;
            sweepMesh.LayerGroupEdgeIds = _layerGroupEdgeIds;
            sweepMesh.BlockArrowStarts = _arrowStarts;
            sweepMesh.BlockArrowDirections = _arrowDirections;
            sweepMesh.BlockRevolveAxes = HasRevolvedBlocks ? _revolveAxes : null;
        }
        /// <summary>
        /// The blocks of several groups, in the order the groups are given. Every id of a block carries the
        /// part it lies on, so the arrays of the groups are simply concatenated.
        /// </summary>
        public static SweepBlocks Merge(IEnumerable<SweepBlocks> blocks)
        {
            List<int[]> sourceSurfaceIds = new List<int[]>();
            List<int[]> sideSurfaceIds = new List<int[]>();
            List<int[]> targetSurfaceIds = new List<int[]>();
            List<int[][]> layerGroupEdgeIds = new List<int[][]>();
            List<double[]> arrowStarts = new List<double[]>();
            List<double[]> arrowDirections = new List<double[]>();
            List<double[]> revolveAxes = new List<double[]>();
            foreach (var blocksOfGroup in blocks)
            {
                sourceSurfaceIds.AddRange(blocksOfGroup.SourceSurfaceIds);
                sideSurfaceIds.AddRange(blocksOfGroup.SideSurfaceIds);
                targetSurfaceIds.AddRange(blocksOfGroup.TargetSurfaceIds);
                layerGroupEdgeIds.AddRange(blocksOfGroup.LayerGroupEdgeIds);
                arrowStarts.AddRange(blocksOfGroup.ArrowStarts);
                arrowDirections.AddRange(blocksOfGroup.ArrowDirections);
                revolveAxes.AddRange(blocksOfGroup.RevolveAxes);
            }
            return new SweepBlocks(sourceSurfaceIds.ToArray(), sideSurfaceIds.ToArray(), targetSurfaceIds.ToArray(),
                                   layerGroupEdgeIds.ToArray(), arrowStarts.ToArray(), arrowDirections.ToArray(),
                                   revolveAxes.ToArray());
        }
    }
}
