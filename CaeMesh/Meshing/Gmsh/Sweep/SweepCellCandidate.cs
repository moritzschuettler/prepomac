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
    /// One way a cell of a compound can be swept: the block of the cell from one source, as the validation of the
    /// sweep sorts its surfaces, with the role it gives every edge of its walls. It tells whether two cells, each
    /// swept one way or left to tetrahedra, agree on a surface they share, which is all the solver of the compound
    /// asks of it; the block itself is what the sweep of the compound is built from.
    /// </summary>
    public class SweepCellCandidate
    {
        // Variables
        private const int _tetrahedra = 0;
        private const int _source = 1;
        private const int _target = 2;
        private const int _wall = 3;
        private const int _none = 4;
        private readonly int _partId;
        private readonly HashSet<int> _sourceSurfaceIds;
        private readonly HashSet<int> _sideSurfaceIds;
        private readonly HashSet<int> _targetSurfaceIds;
        private readonly bool _prismatic;
        private readonly HashSet<int> _wallEdgeIds;
        private readonly int[][] _faceEdgeIds;
        private readonly Dictionary<int, double[][][]> _wallIdSideCentroids;
        private readonly SweepBlocks _blocks;
        private readonly double _score;


        // Properties
        public int PartId { get { return _partId; } }
        public HashSet<int> SourceSurfaceIds { get { return _sourceSurfaceIds; } }
        public HashSet<int> SideSurfaceIds { get { return _sideSurfaceIds; } }
        public HashSet<int> TargetSurfaceIds { get { return _targetSurfaceIds; } }
        /// <summary>
        /// The block of the cell, its ids those of gmsh.
        /// </summary>
        public SweepBlocks Blocks { get { return _blocks; } }
        public double Score { get { return _score; } }


        // Constructors
        /// <summary>
        /// The candidate of the part swept from the source surfaces onto the target surfaces. wallIdSideCentroids
        /// holds for every wall the centroids of the edges of each of its sides: the source side, where it meets the
        /// source or a wall nearer to the source, the target side, and then every chain of edges along the sweep.
        /// </summary>
        public SweepCellCandidate(int partId, int[] sourceSurfaceIds, int[] sideSurfaceIds, int[] targetSurfaceIds,
                                  bool prismatic, HashSet<int> wallEdgeIds, int[][] faceEdgeIds,
                                  Dictionary<int, double[][][]> wallIdSideCentroids, SweepBlocks blocks, double score)
        {
            _partId = partId;
            _sourceSurfaceIds = new HashSet<int>(sourceSurfaceIds);
            _sideSurfaceIds = new HashSet<int>(sideSurfaceIds);
            _targetSurfaceIds = new HashSet<int>(targetSurfaceIds);
            _prismatic = prismatic;
            _wallEdgeIds = wallEdgeIds;
            _faceEdgeIds = faceEdgeIds;
            _wallIdSideCentroids = wallIdSideCentroids;
            _blocks = blocks;
            _score = score;
        }


        // Methods
        /// <summary>
        /// Whether two cells agree on a surface they share, surfaceA of the first and surfaceB of the second, a null
        /// candidate leaving its cell to tetrahedra. Both may sweep away from it; one may end on it and the other be
        /// swept from it, when the one ending on it is split there only along its walls or is a prism, since the mesh
        /// swept onto a target follows no other split; one may be swept from it while it is a wall of the other, whose
        /// structured grid is then the source mesh. Both may have it as a wall when they split its edges into the
        /// same four sides - the source side, the target side and the two chains along the sweep - whichever of the
        /// sides each takes for which, since both then lay the same structured grid on it: the mesher sides a wall
        /// two blocks share once, and the grid is the one both sweep along. A cell left to tetrahedra takes any
        /// surface of its
        /// neighbour, its pyramids meeting the quadrilaterals there, a target on the same condition as a cell swept
        /// from it. firstSplit tells which cell a split target belongs to.
        /// </summary>
        public static SweepCellConflictEnum GetConflict(SweepCellCandidate candidateA, int surfaceA,
                                                        SweepCellCandidate candidateB, int surfaceB, double tolerance,
                                                        out bool firstSplit)
        {
            firstSplit = true;
            int roleA = candidateA == null ? _tetrahedra : candidateA.GetRole(surfaceA);
            int roleB = candidateB == null ? _tetrahedra : candidateB.GetRole(surfaceB);
            if (roleA == _target && (roleB == _source || roleB == _tetrahedra))
                return candidateA.IsSplitOnlyAlongWalls(surfaceA) ? SweepCellConflictEnum.None
                                                                   : SweepCellConflictEnum.TargetSplitInside;
            if (roleB == _target && (roleA == _source || roleA == _tetrahedra))
            {
                firstSplit = false;
                return candidateB.IsSplitOnlyAlongWalls(surfaceB) ? SweepCellConflictEnum.None
                                                                   : SweepCellConflictEnum.TargetSplitInside;
            }
            if (roleA == _tetrahedra || roleB == _tetrahedra) return SweepCellConflictEnum.None;
            if (roleA == _source && roleB == _source) return SweepCellConflictEnum.None;
            if (roleA == _target && roleB == _target) return SweepCellConflictEnum.TargetsMeet;
            if (roleA == _wall && roleB == _wall)
            {
                double[][][] sidesA = candidateA._wallIdSideCentroids[surfaceA];
                double[][][] sidesB = candidateB._wallIdSideCentroids[surfaceB];
                return AreSameWallSides(sidesA, sidesB, tolerance) ? SweepCellConflictEnum.None
                                                                   : SweepCellConflictEnum.WallSidesDiffer;
            }
            if ((roleA == _source && roleB == _wall) || (roleA == _wall && roleB == _source))
                return SweepCellConflictEnum.None;
            if (roleA == _none || roleB == _none) return SweepCellConflictEnum.UnsortedSurface;
            return SweepCellConflictEnum.CapAgainstWall;
        }
        /// <summary>
        /// Whether two swept cells continue one sweep into the other across a surface they share: one is swept from
        /// the target of the other, both are swept away from it, or both are swept alongside each other along it as a
        /// wall, their chains along the sweep being the same edges. Two cells crossing at a shared wall agree, but
        /// the layers of one run across those of the other.
        /// </summary>
        public static bool Continue(SweepCellCandidate candidateA, int surfaceA, SweepCellCandidate candidateB,
                                    int surfaceB, double tolerance)
        {
            if (candidateA == null || candidateB == null) return false;
            int roleA = candidateA.GetRole(surfaceA);
            int roleB = candidateB.GetRole(surfaceB);
            if ((roleA == _source || roleA == _target) && roleB == _source) return true;
            if (roleA == _source && roleB == _target) return true;
            if (roleA == _wall && roleB == _wall)
                return AreSameEdgeGroups(candidateA._wallIdSideCentroids[surfaceA].Skip(2).ToArray(),
                                         candidateB._wallIdSideCentroids[surfaceB].Skip(2).ToArray(), tolerance);
            return false;
        }
        /// <summary>
        /// The first edge of the target surface the block does not have as a wall edge, where a surface shared with
        /// a neighbour ends inside the target, or -1 when there is none or the block is a prism.
        /// </summary>
        public int GetEdgeSplittingTarget(int targetSurfaceId)
        {
            if (_prismatic) return -1;
            foreach (var edgeId in _faceEdgeIds[targetSurfaceId])
            {
                if (!_wallEdgeIds.Contains(edgeId)) return edgeId;
            }
            return -1;
        }
        private bool IsSplitOnlyAlongWalls(int targetSurfaceId)
        {
            return GetEdgeSplittingTarget(targetSurfaceId) < 0;
        }
        private int GetRole(int surfaceId)
        {
            if (_sourceSurfaceIds.Contains(surfaceId)) return _source;
            if (_targetSurfaceIds.Contains(surfaceId)) return _target;
            if (_sideSurfaceIds.Contains(surfaceId)) return _wall;
            return _none;
        }
        /// <summary>
        /// Whether two cells split a wall into the same sides, whichever side each takes for which.
        /// </summary>
        private static bool AreSameWallSides(double[][][] sidesA, double[][][] sidesB, double tolerance)
        {
            return AreSameEdgeGroups(sidesA, sidesB, tolerance);
        }
        private static bool AreSameEdgeGroups(double[][][] groupsA, double[][][] groupsB, double tolerance)
        {
            if (groupsA.Length != groupsB.Length) return false;
            bool[] matched = new bool[groupsB.Length];
            foreach (var groupA in groupsA)
            {
                int match = -1;
                for (int i = 0; i < groupsB.Length && match < 0; i++)
                {
                    if (!matched[i] && AreSameEdges(groupA, groupsB[i], tolerance)) match = i;
                }
                if (match < 0) return false;
                matched[match] = true;
            }
            return true;
        }
        private static bool AreSameEdges(double[][] centroidsA, double[][] centroidsB, double tolerance)
        {
            if (centroidsA.Length != centroidsB.Length) return false;
            foreach (var centroidA in centroidsA)
            {
                if (!centroidsB.Any(centroidB => Math.Abs(centroidA[0] - centroidB[0]) <= tolerance &&
                                                 Math.Abs(centroidA[1] - centroidB[1]) <= tolerance &&
                                                 Math.Abs(centroidA[2] - centroidB[2]) <= tolerance)) return false;
            }
            return true;
        }
    }
}
