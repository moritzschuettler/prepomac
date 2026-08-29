// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using System.Collections.Generic;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// Directional blocking broad phase followed by a tessellated swept-path check. Bounding-box
    /// projections reject pairs whose silhouettes cannot meet; triangle distances validate the remaining
    /// swept cell pairs. Faces of the mate supplying the tested direction are allowed to slide apart.
    /// </summary>
    internal class DirectionalBlockerEvaluator
    {
        private class SurfaceCell
        {
            public ExplodedViewFace Face;
            public BoundingBox Box;
            public double[][][] Triangles;
        }

        /// <summary>
        /// Static per-cluster bounding-volume hierarchy. A swept cell queries only the static cells whose
        /// boxes cross its swept box instead of scanning the full Cartesian product of both tessellations.
        /// </summary>
        private class SurfaceCellBvh
        {
            private class Node
            {
                public BoundingBox Box;
                public Node Left;
                public Node Right;
                public int[] CellIds;
            }

            private const int _leafSize = 8;
            private SurfaceCell[] _cells;
            private Node _root;

            public SurfaceCellBvh(SurfaceCell[] cells)
            {
                _cells = cells;
                List<int> ids = new List<int>(cells.Length);
                for (int i = 0; i < cells.Length; i++) ids.Add(i);
                _root = Build(ids);
            }

            public void Query(BoundingBox worldBox, double[] staticOffset, List<int> cellIds)
            {
                if (_root == null) return;
                BoundingBox localBox = new BoundingBox(worldBox);
                localBox.RemoveOffset(staticOffset);
                Query(_root, localBox, cellIds);
            }

            private Node Build(List<int> cellIds)
            {
                if (cellIds.Count == 0) return null;
                Node node = new Node();
                node.Box = new BoundingBox();
                foreach (int cellId in cellIds) node.Box.IncludeBox(_cells[cellId].Box);
                if (cellIds.Count <= _leafSize)
                {
                    node.CellIds = cellIds.ToArray();
                    return node;
                }
                // Split cell centers along the longest node-box axis.
                int axis = 0;
                if (node.Box.GetYSize() > node.Box.GetXSize()) axis = 1;
                if (node.Box.GetZSize() > (axis == 0 ? node.Box.GetXSize() : node.Box.GetYSize())) axis = 2;
                cellIds.Sort((first, second) => GetCenter(_cells[first].Box, axis)
                                                   .CompareTo(GetCenter(_cells[second].Box, axis)));
                int middle = cellIds.Count / 2;
                node.Left = Build(cellIds.GetRange(0, middle));
                node.Right = Build(cellIds.GetRange(middle, cellIds.Count - middle));
                return node;
            }

            private static void Query(Node node, BoundingBox box, List<int> cellIds)
            {
                if (node == null || !node.Box.Intersects(box)) return;
                if (node.CellIds != null)
                {
                    foreach (int cellId in node.CellIds) cellIds.Add(cellId);
                }
                else
                {
                    Query(node.Left, box, cellIds);
                    Query(node.Right, box, cellIds);
                }
            }

            private static double GetCenter(BoundingBox box, int axis)
            {
                if (axis == 0) return 0.5 * (box.MinX + box.MaxX);
                else if (axis == 1) return 0.5 * (box.MinY + box.MaxY);
                else return 0.5 * (box.MinZ + box.MaxZ);
            }
        }

        private FeMesh _mesh;
        private DisassemblyContactGraph _graph;
        private SurfaceCell[][] _cellsByCluster;
        private SurfaceCellBvh[] _bvhByCluster;
        private Dictionary<string, bool> _blockingCache;
        private double _collisionTolerance;

        public DirectionalBlockerEvaluator(FeMesh mesh, DisassemblyContactGraph graph)
        {
            _mesh = mesh;
            _graph = graph;
            _cellsByCluster = new SurfaceCell[graph.Clusters.Length][];
            _bvhByCluster = new SurfaceCellBvh[graph.Clusters.Length];
            _blockingCache = new Dictionary<string, bool>();
            double assemblyDiagonal = 0;
            foreach (ExplodedViewCluster cluster in graph.Clusters)
                assemblyDiagonal = Math.Max(assemblyDiagonal, cluster.BoundingBox.GetDiagonal());
            _collisionTolerance = Math.Max(0.1 * graph.Tolerance, 1E-10 * Math.Max(assemblyDiagonal, 1));
            for (int i = 0; i < _cellsByCluster.Length; i++)
            {
                _cellsByCluster[i] = GetSurfaceCells(graph.Clusters[i]);
                _bvhByCluster[i] = new SurfaceCellBvh(_cellsByCluster[i]);
            }
        }

        public List<int> GetBlockers(int[] movingClusterIds, Vec3D direction, HashSet<int> remainingClusterIds)
        {
            HashSet<int> moving = new HashSet<int>(movingClusterIds);
            List<int> blockers = new List<int>();
            foreach (int staticClusterId in remainingClusterIds)
            {
                if (moving.Contains(staticClusterId)) continue;
                bool blocked = false;
                foreach (int movingClusterId in movingClusterIds)
                {
                    if (IsBlocked(movingClusterId, staticClusterId, direction))
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) blockers.Add(staticClusterId);
            }
            return blockers;
        }
        /// <summary>
        /// The planner only needs to know whether a candidate is removable. Stop at the first blocker
        /// instead of materializing the complete set, which is substantially cheaper for interior parts.
        /// </summary>
        public bool HasBlocker(int[] movingClusterIds, Vec3D direction, HashSet<int> remainingClusterIds)
        {
            HashSet<int> moving = new HashSet<int>(movingClusterIds);
            foreach (int staticClusterId in remainingClusterIds)
            {
                if (moving.Contains(staticClusterId)) continue;
                foreach (int movingClusterId in movingClusterIds)
                {
                    if (IsBlocked(movingClusterId, staticClusterId, direction)) return true;
                }
            }
            return false;
        }

        public bool IsBlocked(int movingClusterId, int staticClusterId, Vec3D direction)
        {
            Vec3D unitDirection = new Vec3D(direction);
            unitDirection.Normalize();
            double[] rounded = unitDirection.CoorRounded(4);
            string key = movingClusterId + ":" + staticClusterId + ":" + rounded[0] + ":" +
                         rounded[1] + ":" + rounded[2];
            bool blocked;
            if (_blockingCache.TryGetValue(key, out blocked)) return blocked;
            //
            BoundingBox movingBox = _graph.Clusters[movingClusterId].BoundingBox;
            BoundingBox staticBox = _graph.Clusters[staticClusterId].BoundingBox;
            if (!ProjectedBoxesOverlap(movingBox, staticBox, unitDirection, _graph.Tolerance) ||
                IsEntirelyBehind(movingBox, staticBox, unitDirection)) blocked = false;
            else
            {
                double distance = GetPassDistance(movingBox, staticBox, unitDirection, 2 * _graph.Tolerance);
                blocked = HasSweptConflict(movingClusterId, new double[3], staticClusterId, new double[3],
                                           unitDirection, distance, true);
            }
            _blockingCache.Add(key, blocked);
            return blocked;
        }

        public bool HasSweptConflict(int movingClusterId, double[] movingOffset,
                                     int staticClusterId, double[] staticOffset,
                                     Vec3D direction, double distance, bool allowMatingFaces)
        {
            if (distance <= 0) return false;
            SurfaceCell[] movingCells = _cellsByCluster[movingClusterId];
            SurfaceCell[] staticCells = _cellsByCluster[staticClusterId];
            if (movingCells.Length == 0 || staticCells.Length == 0) return true;
            //
            HashSet<ExplodedViewFace> ignoredMovingFaces = null;
            HashSet<ExplodedViewFace> ignoredStaticFaces = null;
            if (allowMatingFaces)
                GetSlidingFaces(movingClusterId, staticClusterId, direction,
                                out ignoredMovingFaces, out ignoredStaticFaces);
            //
            Vec3D unitDirection = new Vec3D(direction);
            unitDirection.Normalize();
            double startEpsilon = Math.Min(distance, 2 * _collisionTolerance);
            double[] pathOffset = (distance * unitDirection).Coor;
            List<int> candidateStaticCellIds = new List<int>();
            foreach (SurfaceCell movingCell in movingCells)
            {
                if (ignoredMovingFaces != null && ignoredMovingFaces.Contains(movingCell.Face)) continue;
                BoundingBox movingBox = OffsetBox(movingCell.Box, movingOffset);
                BoundingBox endBox = new BoundingBox(movingBox);
                endBox.AddOffset(pathOffset);
                BoundingBox sweptBox = new BoundingBox(movingBox);
                sweptBox.IncludeBox(endBox);
                candidateStaticCellIds.Clear();
                _bvhByCluster[staticClusterId].Query(sweptBox, staticOffset, candidateStaticCellIds);
                foreach (int staticCellId in candidateStaticCellIds)
                {
                    SurfaceCell staticCell = staticCells[staticCellId];
                    if (ignoredStaticFaces != null && ignoredStaticFaces.Contains(staticCell.Face)) continue;
                    BoundingBox staticBox = OffsetBox(staticCell.Box, staticOffset);
                    if (!ProjectedBoxesOverlap(movingBox, staticBox, unitDirection, _collisionTolerance)) continue;
                    // The interval of translations for which the two cell AABBs overlap along the motion.
                    double movingMin;
                    double movingMax;
                    double staticMin;
                    double staticMax;
                    movingBox.GetProjection(unitDirection.Coor, out movingMin, out movingMax);
                    staticBox.GetProjection(unitDirection.Coor, out staticMin, out staticMax);
                    double enter = Math.Max(startEpsilon, staticMin - movingMax - _collisionTolerance);
                    double leave = Math.Min(distance, staticMax - movingMin + _collisionTolerance);
                    if (leave < enter) continue;
                    // Five samples inside the exact AABB overlap interval are a narrow phase over the
                    // tessellation, not a global tiny-step march. Cell-sized intervals keep this bounded.
                    for (int sample = 0; sample < 5; sample++)
                    {
                        double t = enter + (leave - enter) * sample / 4.0;
                        if (TrianglesConflict(movingCell.Triangles, movingOffset, unitDirection, t,
                                              staticCell.Triangles, staticOffset)) return true;
                    }
                }
            }
            return false;
        }

        public bool HasFinalConflict(int firstClusterId, double[] firstOffset,
                                     int secondClusterId, double[] secondOffset)
        {
            BoundingBox firstBox = OffsetBox(_graph.Clusters[firstClusterId].BoundingBox, firstOffset);
            BoundingBox secondBox = OffsetBox(_graph.Clusters[secondClusterId].BoundingBox, secondOffset);
            if (!firstBox.Intersects(secondBox)) return false;
            // Test a very short sweep at the final position without allowing mating-face exclusions.
            Vec3D direction = new Vec3D(1, 0, 0);
            return HasSweptConflict(firstClusterId, firstOffset, secondClusterId, secondOffset,
                                    direction, 4 * _collisionTolerance, false);
        }

        public static double GetPassDistance(BoundingBox movingBox, BoundingBox staticBox,
                                             Vec3D direction, double gap)
        {
            if (!ProjectedBoxesOverlap(movingBox, staticBox, direction, gap)) return 0;
            double movingMin;
            double movingMax;
            double staticMin;
            double staticMax;
            movingBox.GetProjection(direction.Coor, out movingMin, out movingMax);
            staticBox.GetProjection(direction.Coor, out staticMin, out staticMax);
            if (staticMax <= movingMin) return 0;
            return staticMax - movingMin + gap;
        }

        private void GetSlidingFaces(int movingClusterId, int staticClusterId, Vec3D direction,
                                     out HashSet<ExplodedViewFace> movingFaces,
                                     out HashSet<ExplodedViewFace> staticFaces)
        {
            movingFaces = null;
            staticFaces = null;
            ExplodedViewInterface relation = _graph.GetInterface(movingClusterId, staticClusterId);
            if (relation == null || !relation.HasDirection) return;
            Vec3D allowed = relation.GetDirection(movingClusterId);
            double dot = Vec3D.DotProduct(allowed, direction);
            bool followsMate = relation.IsAxialDirection ? Math.Abs(dot) >= Math.Cos(20 * Math.PI / 180) :
                                                          dot >= Math.Cos(20 * Math.PI / 180);
            if (!followsMate) return;
            //
            if (movingClusterId == relation.FirstClusterId)
            {
                movingFaces = new HashSet<ExplodedViewFace>(relation.FirstFaces);
                staticFaces = new HashSet<ExplodedViewFace>(relation.SecondFaces);
            }
            else
            {
                movingFaces = new HashSet<ExplodedViewFace>(relation.SecondFaces);
                staticFaces = new HashSet<ExplodedViewFace>(relation.FirstFaces);
            }
        }

        private SurfaceCell[] GetSurfaceCells(ExplodedViewCluster cluster)
        {
            List<SurfaceCell> cells = new List<SurfaceCell>();
            foreach (ExplodedViewFace face in cluster.Faces)
            {
                for (int i = 0; i < face.CellCount; i++)
                {
                    int[] cell = face.GetCell(i);
                    int cornerCount = cell.Length == 3 || cell.Length == 6 ? 3 :
                                      cell.Length == 4 || cell.Length == 8 ? 4 : 0;
                    if (cornerCount == 0) continue;
                    //
                    BoundingBox box = new BoundingBox();
                    box.IncludeFirstCoor(_mesh.Nodes[cell[0]].Coor);
                    for (int j = 1; j < cornerCount; j++) box.IncludeCoorFast(_mesh.Nodes[cell[j]].Coor);
                    box.Inflate(_collisionTolerance);
                    double[][] first = new double[][] { _mesh.Nodes[cell[0]].Coor,
                                                        _mesh.Nodes[cell[1]].Coor,
                                                        _mesh.Nodes[cell[2]].Coor };
                    double[][][] triangles;
                    if (cornerCount == 3) triangles = new double[][][] { first };
                    else triangles = new double[][][] { first,
                        new double[][] { _mesh.Nodes[cell[0]].Coor, _mesh.Nodes[cell[2]].Coor,
                                         _mesh.Nodes[cell[3]].Coor } };
                    cells.Add(new SurfaceCell() { Face = face, Box = box, Triangles = triangles });
                }
            }
            return cells.ToArray();
        }

        private bool TrianglesConflict(double[][][] movingTriangles, double[] movingOffset,
                                       Vec3D direction, double distance,
                                       double[][][] staticTriangles, double[] staticOffset)
        {
            double[] shift = new double[] { movingOffset[0] + distance * direction.X,
                                            movingOffset[1] + distance * direction.Y,
                                            movingOffset[2] + distance * direction.Z };
            double[] firstPoint = new double[3];
            double[] secondPoint = new double[3];
            foreach (double[][] movingTriangle in movingTriangles)
            {
                double[][] shiftedMoving = OffsetTriangle(movingTriangle, shift);
                foreach (double[][] staticTriangle in staticTriangles)
                {
                    double[][] shiftedStatic = OffsetTriangle(staticTriangle, staticOffset);
                    if (Geometry.TriDist(ref firstPoint, ref secondPoint, shiftedMoving, shiftedStatic, false) <=
                        _collisionTolerance) return true;
                }
            }
            return false;
        }

        private static double[][] OffsetTriangle(double[][] triangle, double[] offset)
        {
            return new double[][] {
                new double[] { triangle[0][0] + offset[0], triangle[0][1] + offset[1], triangle[0][2] + offset[2] },
                new double[] { triangle[1][0] + offset[0], triangle[1][1] + offset[1], triangle[1][2] + offset[2] },
                new double[] { triangle[2][0] + offset[0], triangle[2][1] + offset[1], triangle[2][2] + offset[2] }
            };
        }

        private static BoundingBox OffsetBox(BoundingBox box, double[] offset)
        {
            BoundingBox shifted = new BoundingBox(box);
            shifted.AddOffset(offset);
            return shifted;
        }

        private static bool IsEntirelyBehind(BoundingBox movingBox, BoundingBox staticBox, Vec3D direction)
        {
            double movingMin;
            double movingMax;
            double staticMin;
            double staticMax;
            movingBox.GetProjection(direction.Coor, out movingMin, out movingMax);
            staticBox.GetProjection(direction.Coor, out staticMin, out staticMax);
            return staticMax <= movingMin;
        }

        private static bool ProjectedBoxesOverlap(BoundingBox first, BoundingBox second,
                                                  Vec3D direction, double tolerance)
        {
            Vec3D reference = Math.Abs(direction.X) < 0.8 ? new Vec3D(1, 0, 0) : new Vec3D(0, 1, 0);
            Vec3D firstAxis = Vec3D.CrossProduct(direction, reference);
            if (firstAxis.Len2 <= 1E-24) firstAxis = Vec3D.CrossProduct(direction, new Vec3D(0, 0, 1));
            firstAxis.Normalize();
            Vec3D secondAxis = Vec3D.CrossProduct(direction, firstAxis);
            secondAxis.Normalize();
            return ProjectionIntervalsOverlap(first, second, firstAxis, tolerance) &&
                   ProjectionIntervalsOverlap(first, second, secondAxis, tolerance);
        }

        private static bool ProjectionIntervalsOverlap(BoundingBox first, BoundingBox second,
                                                       Vec3D axis, double tolerance)
        {
            double firstMin;
            double firstMax;
            double secondMin;
            double secondMax;
            first.GetProjection(axis.Coor, out firstMin, out firstMax);
            second.GetProjection(axis.Coor, out secondMin, out secondMax);
            return firstMax + tolerance >= secondMin && secondMax + tolerance >= firstMin;
        }
    }
}
