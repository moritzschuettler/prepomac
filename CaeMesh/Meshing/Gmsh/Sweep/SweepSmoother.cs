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
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Optional quality guarded Laplacian smoothing of the generated sweep nodes.
    ///
    /// This is off by default. In the first implementation smoothing was load bearing: it was how
    /// the interior sweep lines got a plausible shape, and how the distortion left by projecting
    /// only the last node was spread around. Both jobs are now done directly - the layer interiors
    /// come from the harmonic morph and the projection correction is spread along each sweep line -
    /// so smoothing is left with one narrow use: a target surface steeply inclined to the sweep
    /// direction, where the orthogonal projection bunches the target layer up and letting those
    /// nodes slide within the surface relieves it.
    ///
    /// It is not free. Relaxation pulls node spacing towards uniform, and the morph deliberately
    /// carries the grading of the source mesh through every layer, so running it trades one against
    /// the other. Judge it by the scaled Jacobian the sweep reports rather than by eye.
    ///
    /// Three things separate this from the plain umbrella Laplacian the first implementation used:
    ///
    ///   - Every trial move is accepted only if the smallest scaled Jacobian of the elements around
    ///     the node does not get worse. Plain Laplacian smoothing loses volume and folds elements
    ///     in concave regions, and nothing in the original pipeline would have noticed: an inverted
    ///     element only surfaced later, as a solver failure.
    ///   - The neighbour weights are edge lengths rather than uniform, so a layer thickness that
    ///     differs from the in-plane element size no longer biases the move along the coarser
    ///     direction.
    ///   - Nodes on the target layer slide within the target surface instead of moving freely and
    ///     then being pulled back by a separate projection pass. The original code smoothed those
    ///     nodes off the surface and re-projected them afterwards, so the smoothing and the
    ///     projection undid each other's work on every run.
    ///
    /// The intermediate layers are relaxed in place, node by node, which converges faster than
    /// rebuilding a full copy of the coordinates on every step. The target layer is relaxed as one
    /// batch so that its projection back onto the geometry is a single call per target surface.
    /// </summary>
    public static class SweepSmoother
    {
        // Methods
        /// <summary>
        /// Relaxes the generated nodes. The stencil is the one ring of the node inside its own
        /// layer plus the previous and next node on its sweep line.
        /// </summary>
        public static void Smooth(SweepMeshData data, TargetProjection projection, bool flip, int numSteps,
                                  double relaxation)
        {
            if (numSteps <= 0) return;
            //
            int numLayers = data.NumLayers;
            int[] interior = data.InteriorNodes;
            SweepQualityBuffers buffers = new SweepQualityBuffers();
            double[] candidate = new double[3];
            // Target layer nodes grouped by the surface they were projected onto
            Dictionary<int, List<int>> surfaceGroups = new Dictionary<int, List<int>>();
            for (int k = 0; k < interior.Length; k++)
            {
                List<int> group;
                int surfaceId = projection.SurfaceIds[k];
                if (!surfaceGroups.TryGetValue(surfaceId, out group))
                {
                    group = new List<int>();
                    surfaceGroups.Add(surfaceId, group);
                }
                group.Add(k);
            }
            //
            for (int step = 0; step < numSteps; step++)
            {
                // Intermediate layers, relaxed in place
                for (int layer = 1; layer <= numLayers - 2; layer++)
                {
                    for (int k = 0; k < interior.Length; k++)
                    {
                        int node = interior[k];
                        if (!GetRelaxedPosition(data, layer, node, relaxation, candidate)) continue;
                        TryMove(data, buffers, layer, node, flip, candidate);
                    }
                }
                // Target layer, relaxed as one batch so it can be projected in one call per surface
                SmoothTargetLayer(data, projection, surfaceGroups, buffers, flip, relaxation);
            }
        }
        private static void SmoothTargetLayer(SweepMeshData data, TargetProjection projection,
                                              Dictionary<int, List<int>> surfaceGroups, SweepQualityBuffers buffers,
                                              bool flip, double relaxation)
        {
            int layer = data.NumLayers - 1;
            int[] interior = data.InteriorNodes;
            double[] candidate = new double[3];
            double[] candidates = new double[3 * interior.Length];
            bool[] moved = new bool[interior.Length];
            // Trial positions from the current coordinates
            for (int k = 0; k < interior.Length; k++)
            {
                if (!GetRelaxedPosition(data, layer, interior[k], relaxation, candidate)) continue;
                candidates[3 * k] = candidate[0];
                candidates[3 * k + 1] = candidate[1];
                candidates[3 * k + 2] = candidate[2];
                moved[k] = true;
            }
            // Back onto the geometry, one batched projection per target surface
            foreach (var entry in surfaceGroups)
            {
                List<int> group = entry.Value;
                int count = 0;
                for (int i = 0; i < group.Count; i++) if (moved[group[i]]) count++;
                if (count == 0) continue;
                //
                int[] map = new int[count];
                double[] input = new double[3 * count];
                int index = 0;
                for (int i = 0; i < group.Count; i++)
                {
                    int k = group[i];
                    if (!moved[k]) continue;
                    map[index] = k;
                    input[3 * index] = candidates[3 * k];
                    input[3 * index + 1] = candidates[3 * k + 1];
                    input[3 * index + 2] = candidates[3 * k + 2];
                    index++;
                }
                //
                double[] closestCoord;
                double[] parametricCoord;
                Gmsh.Model.GetClosestPoint(2, entry.Key, input, out closestCoord, out parametricCoord);
                if (closestCoord.Length != 3 * count || parametricCoord.Length != 2 * count) continue;
                // Accept a move only if it does not make the elements around the node worse
                for (int i = 0; i < count; i++)
                {
                    int k = map[i];
                    candidate[0] = closestCoord[3 * i];
                    candidate[1] = closestCoord[3 * i + 1];
                    candidate[2] = closestCoord[3 * i + 2];
                    if (TryMove(data, buffers, layer, interior[k], flip, candidate))
                    {
                        projection.ParametricCoords[2 * k] = parametricCoord[2 * i];
                        projection.ParametricCoords[2 * k + 1] = parametricCoord[2 * i + 1];
                    }
                }
            }
        }
        /// <summary>
        /// Moves a sweep node part of the way towards the edge length weighted average of its
        /// neighbours. Returns false when the node has no usable neighbours.
        /// </summary>
        private static bool GetRelaxedPosition(SweepMeshData data, int layer, int node, double relaxation,
                                               double[] candidate)
        {
            double[] coords = data.Coords;
            int self = 3 * data.Index(layer, node);
            double sx = 0, sy = 0, sz = 0, sw = 0;
            // Neighbours inside the layer
            for (int k = data.AdjStart[node]; k < data.AdjStart[node + 1]; k++)
            {
                Accumulate(coords, self, 3 * data.Index(layer, data.AdjNodes[k]), ref sx, ref sy, ref sz, ref sw);
            }
            // Neighbours along the sweep line
            Accumulate(coords, self, 3 * data.Index(layer - 1, node), ref sx, ref sy, ref sz, ref sw);
            if (layer + 1 < data.NumLayers)
                Accumulate(coords, self, 3 * data.Index(layer + 1, node), ref sx, ref sy, ref sz, ref sw);
            //
            if (sw <= 0) return false;
            //
            candidate[0] = coords[self] + relaxation * (sx / sw - coords[self]);
            candidate[1] = coords[self + 1] + relaxation * (sy / sw - coords[self + 1]);
            candidate[2] = coords[self + 2] + relaxation * (sz / sw - coords[self + 2]);
            return true;
        }
        private static void Accumulate(double[] coords, int self, int neighbour, ref double sx, ref double sy,
                                       ref double sz, ref double sw)
        {
            double dx = coords[neighbour] - coords[self];
            double dy = coords[neighbour + 1] - coords[self + 1];
            double dz = coords[neighbour + 2] - coords[self + 2];
            double w = Math.Sqrt(dx * dx + dy * dy + dz * dz);
            if (w <= 0) return;
            //
            sx += w * coords[neighbour];
            sy += w * coords[neighbour + 1];
            sz += w * coords[neighbour + 2];
            sw += w;
        }
        /// <summary>
        /// Moves a node to the candidate position and keeps the move only if the smallest scaled
        /// Jacobian around the node does not decrease.
        /// </summary>
        private static bool TryMove(SweepMeshData data, SweepQualityBuffers buffers, int layer, int node, bool flip,
                                    double[] candidate)
        {
            double[] coords = data.Coords;
            int i = 3 * data.Index(layer, node);
            double before = SweepQuality.MinQualityAroundNode(data, buffers, layer, node, flip);
            //
            double x = coords[i];
            double y = coords[i + 1];
            double z = coords[i + 2];
            coords[i] = candidate[0];
            coords[i + 1] = candidate[1];
            coords[i + 2] = candidate[2];
            //
            double after = SweepQuality.MinQualityAroundNode(data, buffers, layer, node, flip);
            if (after < before)
            {
                coords[i] = x;
                coords[i + 1] = y;
                coords[i + 2] = z;
                return false;
            }
            return true;
        }
    }
}
