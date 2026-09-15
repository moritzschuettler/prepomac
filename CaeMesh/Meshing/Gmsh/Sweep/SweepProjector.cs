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
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Where the interior nodes of the target layer end up on the target surfaces, together with
    /// the parametric coordinates of those points.
    /// </summary>
    public class TargetProjection
    {
        public int[] SurfaceIds;                        // per interior node, the surface it landed on
        public double[] ParametricCoords;               // per interior node, u and v on that surface
        public double MaxDistance;                      // largest correction the projection had to make
    }
    /// <summary>
    /// Puts the target layer onto the target surfaces and spreads the correction back along the
    /// sweep lines.
    ///
    /// The projection itself is a single batched orthogonal projection per target surface, done by
    /// gmsh. The first implementation instead ran a hand written Newton iteration on the unsigned
    /// distance to the surface, and evaluated that distance by creating two throwaway OCC points
    /// per iteration per node - points that were never removed. Besides leaking geometry, that
    /// iteration had no damping, used an absolute convergence tolerance that did not scale with
    /// the model, and silently left a node unprojected when it failed to converge.
    ///
    /// Spreading the correction along the whole sweep line, rather than moving only the last node,
    /// is what stops the last element layer from absorbing the entire difference between the swept
    /// shape and the real target surface.
    /// </summary>
    public static class SweepProjector
    {
        // Methods
        public static TargetProjection ProjectTargetLayer(SweepMeshData data, IEnumerable<int> targetSurfaceIds)
        {
            int layer = data.NumLayers - 1;
            int[] interior = data.InteriorNodes;
            int count = interior.Length;
            double[] coords = data.Coords;
            // Positions the morph produced for the target layer
            double[] input = new double[3 * count];
            for (int k = 0; k < count; k++)
            {
                int t = 3 * data.Index(layer, interior[k]);
                input[3 * k] = coords[t];
                input[3 * k + 1] = coords[t + 1];
                input[3 * k + 2] = coords[t + 2];
            }
            //
            double[] bestCoor = new double[3 * count];
            double[] bestParam = new double[2 * count];
            double[] bestDistance = new double[count];
            int[] bestSurfaceId = new int[count];
            // Projecting onto every candidate and taking the nearest is what lets a sweep end on a
            // target that is split into several faces.
            ProjectOntoNearestSurface(input, targetSurfaceIds, bestCoor, bestParam, bestDistance, bestSurfaceId);
            //
            double maxDistance = 0;
            for (int k = 0; k < count; k++)
            {
                if (bestSurfaceId[k] < 0)
                    throw new CaeException("A sweep node could not be projected onto any target surface.");
                maxDistance = Math.Max(maxDistance, Math.Sqrt(bestDistance[k]));
            }
            // Spread the correction along each sweep line
            SpreadCorrection(data, interior, bestCoor);
            //
            return new TargetProjection()
            {
                SurfaceIds = bestSurfaceId,
                ParametricCoords = bestParam,
                MaxDistance = maxDistance
            };
        }
        /// <summary>
        /// Projects every point onto each of the surfaces, one batched projection per surface, and
        /// keeps the nearest hit for each point. gmsh measures the distance to the unbounded underlying
        /// surface, so two faces lying in one plane report the same distance for every point, and the
        /// first of them would take all of it. A surface that contains the projected point therefore
        /// beats one that does not, whatever the distances; only among surfaces that agree on that does
        /// the nearer one win. The containment is asked with the coordinates of the projected point:
        /// asked with its parametric coordinates gmsh answers yes for every face, and the parametric
        /// coordinates the projection returns for a CAD face are all zero anyway. The test is skipped
        /// when there is nothing to tell apart.
        /// </summary>
        public static void ProjectOntoNearestSurface(double[] input, IEnumerable<int> surfaceIds, double[] bestCoor,
                                                     double[] bestParam, double[] bestDistance, int[] bestSurfaceId)
        {
            int count = input.Length / 3;
            List<int> surfaceIdList = new List<int>(surfaceIds);
            bool checkInside = surfaceIdList.Count > 1;
            bool inside;
            bool[] bestInside = new bool[count];
            double[] point = new double[3];
            for (int k = 0; k < count; k++)
            {
                bestDistance[k] = double.MaxValue;
                bestSurfaceId[k] = -1;
            }
            //
            double[] closestCoord;
            double[] parametricCoord;
            foreach (var surfaceId in surfaceIdList)
            {
                Gmsh.Model.GetClosestPoint(2, surfaceId, input, out closestCoord, out parametricCoord);
                if (closestCoord.Length != 3 * count || parametricCoord.Length != 2 * count)
                    throw new CaeException("The projection onto the sweep target surface failed.");
                //
                for (int k = 0; k < count; k++)
                {
                    double dx = closestCoord[3 * k] - input[3 * k];
                    double dy = closestCoord[3 * k + 1] - input[3 * k + 1];
                    double dz = closestCoord[3 * k + 2] - input[3 * k + 2];
                    double distance = dx * dx + dy * dy + dz * dz;
                    //
                    inside = true;
                    if (checkInside)
                    {
                        point[0] = closestCoord[3 * k];
                        point[1] = closestCoord[3 * k + 1];
                        point[2] = closestCoord[3 * k + 2];
                        inside = Gmsh.Model.IsInside(2, surfaceId, point, false) > 0;
                    }
                    if (bestSurfaceId[k] >= 0)
                    {
                        if (bestInside[k] && !inside) continue;
                        if (bestInside[k] == inside && distance >= bestDistance[k]) continue;
                    }
                    //
                    bestInside[k] = inside;
                    bestDistance[k] = distance;
                    bestSurfaceId[k] = surfaceId;
                    if (bestCoor != null)
                    {
                        bestCoor[3 * k] = closestCoord[3 * k];
                        bestCoor[3 * k + 1] = closestCoord[3 * k + 1];
                        bestCoor[3 * k + 2] = closestCoord[3 * k + 2];
                    }
                    if (bestParam != null)
                    {
                        bestParam[2 * k] = parametricCoord[2 * k];
                        bestParam[2 * k + 1] = parametricCoord[2 * k + 1];
                    }
                }
            }
        }
        /// <summary>
        /// Moves the target node of each interior sweep line onto its projected position and
        /// distributes the same correction over the intermediate layers, weighted by how far along
        /// the line each layer sits. The correction vanishes at the source, so the source mesh is
        /// untouched, and it is fully applied at the target, so the target node lands exactly on
        /// the surface. Because the weight follows the arc length of the line, the layer spacing
        /// that the wall grid established is preserved instead of being dumped into the last layer.
        /// </summary>
        private static void SpreadCorrection(SweepMeshData data, int[] interior, double[] targetCoor)
        {
            int numLayers = data.NumLayers;
            double[] coords = data.Coords;
            double[] cumulative = new double[numLayers];
            //
            for (int k = 0; k < interior.Length; k++)
            {
                int node = interior[k];
                // Arc length of the line as the morph left it
                cumulative[0] = 0;
                for (int layer = 1; layer < numLayers; layer++)
                {
                    int a = 3 * data.Index(layer - 1, node);
                    int b = 3 * data.Index(layer, node);
                    cumulative[layer] = cumulative[layer - 1] +
                                        Math.Sqrt(Math.Pow(coords[b] - coords[a], 2) +
                                                  Math.Pow(coords[b + 1] - coords[a + 1], 2) +
                                                  Math.Pow(coords[b + 2] - coords[a + 2], 2));
                }
                //
                int last = 3 * data.Index(numLayers - 1, node);
                double dx = targetCoor[3 * k] - coords[last];
                double dy = targetCoor[3 * k + 1] - coords[last + 1];
                double dz = targetCoor[3 * k + 2] - coords[last + 2];
                //
                double total = cumulative[numLayers - 1];
                if (total <= 0)
                {
                    // Degenerate line - put the whole correction on the target node
                    coords[last] += dx;
                    coords[last + 1] += dy;
                    coords[last + 2] += dz;
                    continue;
                }
                //
                for (int layer = 1; layer < numLayers; layer++)
                {
                    double f = cumulative[layer] / total;
                    int i = 3 * data.Index(layer, node);
                    coords[i] += f * dx;
                    coords[i + 1] += f * dy;
                    coords[i + 2] += f * dz;
                }
            }
        }
        /// <summary>
        /// Pulls a single node back onto the surface it was projected onto. Used by the smoother so
        /// that target layer nodes slide within the target surface instead of leaving it.
        /// </summary>
        public static bool ProjectPoint(int surfaceId, double[] coor, double[] parametricCoor)
        {
            double[] closestCoord;
            double[] parametricCoord;
            Gmsh.Model.GetClosestPoint(2, surfaceId, coor, out closestCoord, out parametricCoord);
            if (closestCoord.Length < 3 || parametricCoord.Length < 2) return false;
            //
            coor[0] = closestCoord[0];
            coor[1] = closestCoord[1];
            coor[2] = closestCoord[2];
            if (parametricCoor != null)
            {
                parametricCoor[0] = parametricCoord[0];
                parametricCoor[1] = parametricCoord[1];
            }
            return true;
        }
    }
}
