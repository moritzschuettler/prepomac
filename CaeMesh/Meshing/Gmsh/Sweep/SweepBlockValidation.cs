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
    /// What the validation of one sweep block found for one part swept from one set of source surfaces: the
    /// side and the target surfaces, the side surfaces ring by ring, the edges following the sweep direction
    /// where the walls were found by following the edges, or the reason the block was refused. The result is
    /// a function of the part and the source alone, so one detection keeps it and reads it again for every
    /// way that reaches the same source instead of validating the block again. What the detection derives
    /// from a validated block - the edges of its walls, whether it is a prism, its layer edges and its arrow -
    /// is a function of the same, and is kept here as well, found by the first way and read by the others.
    /// </summary>
    public class SweepBlockValidation
    {
        // Variables                                                                                                                
        private readonly string _error;
        private readonly int[] _sideSurfaceIds;
        private readonly int[] _targetSurfaceIds;
        private readonly List<int[]> _layerSideSurfaceIds;
        private readonly int[] _directionEdgeIds;
        private readonly bool _reachedTarget;
        private HashSet<int> _wallEdgeIds;
        private bool? _prismatic;
        private int[][][] _layerGroupEdgeIds;
        private bool _arrowFound;
        private double[] _arrowStart;
        private double[] _arrowDirection;


        // Properties                                                                                                               
        public string Error { get { return _error; } }
        public int[] TargetSurfaceIds { get { return _targetSurfaceIds; } }
        /// <summary>
        /// Whether the validation found the walls and the target of the block before it refused it, so that the
        /// error names what is wrong with a block that is there rather than that no block could be found.
        /// </summary>
        public bool ReachedTarget { get { return _reachedTarget; } }


        // Constructors                                                                                                             
        public SweepBlockValidation(string error, int[] sideSurfaceIds, int[] targetSurfaceIds,
                                    List<int[]> layerSideSurfaceIds, int[] directionEdgeIds, bool reachedTarget)
        {
            _error = error;
            _reachedTarget = reachedTarget;
            _sideSurfaceIds = sideSurfaceIds;
            _targetSurfaceIds = targetSurfaceIds;
            _layerSideSurfaceIds = layerSideSurfaceIds;
            _directionEdgeIds = directionEdgeIds;
        }


        // Methods                                                                                                                  
        /// <summary>
        /// Copies of what was found, so that a caller may keep or renumber them without touching the kept result.
        /// </summary>
        public void CopyTo(out int[] sideSurfaceIds, out int[] targetSurfaceIds, out List<int[]> layerSideSurfaceIds,
                           out int[] directionEdgeIds)
        {
            sideSurfaceIds = _sideSurfaceIds != null ? _sideSurfaceIds.ToArray() : null;
            targetSurfaceIds = _targetSurfaceIds != null ? _targetSurfaceIds.ToArray() : null;
            layerSideSurfaceIds = _layerSideSurfaceIds != null ? _layerSideSurfaceIds.Select(ids => ids.ToArray()).ToList()
                                                               : null;
            directionEdgeIds = _directionEdgeIds != null ? _directionEdgeIds.ToArray() : null;
        }
        /// <summary>
        /// The edges of the side surfaces, found by the first way and read by every other; the set is kept, so a
        /// caller only reads it.
        /// </summary>
        public HashSet<int> GetWallEdgeIds(Func<HashSet<int>> find)
        {
            if (_wallEdgeIds == null) _wallEdgeIds = find();
            return _wallEdgeIds;
        }
        /// <summary>
        /// Whether the block is a prism, told by the first way and read by every other.
        /// </summary>
        public bool IsPrismatic(Func<bool> tell)
        {
            if (!_prismatic.HasValue) _prismatic = tell();
            return _prismatic.Value;
        }
        /// <summary>
        /// The layer edges of the block, group by group, found by the first way. Every way gets its own outer
        /// arrays, since it replaces the groups by their gmsh numbering, and shares the edge arrays, which no caller
        /// changes.
        /// </summary>
        public int[][][] GetLayerGroupEdgeIds(Func<int[][][]> find)
        {
            if (_layerGroupEdgeIds == null) _layerGroupEdgeIds = find();
            return _layerGroupEdgeIds.Select(groups => groups.ToArray()).ToArray();
        }
        /// <summary>
        /// The arrow of the block, its start and its direction, found by the first way as a pair and copied to
        /// every other; both are null for a block without a layer edge leaving its source.
        /// </summary>
        public void GetArrow(Func<double[][]> find, out double[] arrowStart, out double[] arrowDirection)
        {
            if (!_arrowFound)
            {
                double[][] arrow = find();
                _arrowStart = arrow[0];
                _arrowDirection = arrow[1];
                _arrowFound = true;
            }
            arrowStart = _arrowStart != null ? _arrowStart.ToArray() : null;
            arrowDirection = _arrowDirection != null ? _arrowDirection.ToArray() : null;
        }
    }
}
