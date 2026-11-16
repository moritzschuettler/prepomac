// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Meshes a cap face as an O-grid on the nodes of its edges: gmsh supplies the nodes its seam and its rim carry, the
    /// pole nodes, their parameters on the surface and the evaluation of the surface. The face is meshed once gmsh has
    /// meshed the edges and before it meshes the surfaces, which it then leaves as it is (Mesh.MeshOnlyEmpty); any
    /// elements already on it are replaced. The grid is laid in a chart that is regular at a pole: polar coordinates whose
    /// radius is the length along the iso-line of the periodic parameter from the pole and whose angle is the periodic
    /// parameter from the seam. Around each pole a block of a x b quads (CapFace.GetBlock) is laid on the levels of the
    /// seam nodes, the seam its column through the pole, and rounded towards a circle, by each of BlockRoundings in turn,
    /// the grid with the best corner in space kept. The rings between the rim, or the other block, and the block boundary
    /// interpolate the angle and the radius of their nodes between the two by the levels of the seam nodes, so that they
    /// turn from the rim into the rounded block boundary gradually. A face with no rim takes the blocks of both poles and
    /// the rings between them, laid in the chart of the first pole, the block of the second in its own. An edge that is
    /// not a seam of the surface, a seam that does not end in a pole, a rim that does not run once round, edges gmsh
    /// meshed with other counts than given, a seam that leaves no ring, and a grid that folds or has a quad corner below
    /// MinScaledJacobian, measured in space against the normal of the surface (at a pole against the normal of the
    /// quad), leave the face to gmsh.
    /// </summary>
    public static class GmshCapMesher
    {
        // Variables                                                                                                                
        /// <summary>
        /// How far the blocks are rounded from a square towards the disc of the elliptical grid mapping, each tried and then
        /// half a step either side of the best: 0 keeps the square, 1 puts the boundary on the circle, where its corner
        /// angles reach 180 degrees. A sphere is best near 0.5, a sharp cone, whose tangential lengths shrink, near 0.85.
        /// </summary>
        private const double RoundingStep = 0.1;
        private static readonly double[] BlockRoundings = Enumerable.Range(1, 9).Select(k => k * RoundingStep).ToArray();
        /// <summary>
        /// The smallest scaled Jacobian of a quad corner of a cap that is kept, half of that of the other structured
        /// faces: the four quads at the apex of a cone share the angle round it, which shrinks with its half angle (0.26
        /// at 15 degrees, 0.16 at 8), and the free mesh of a face with a pole is rarely better there - gmsh leaves 0.03 to
        /// 0.27 at the pole of the test shapes and fails on a sharp cone.
        /// </summary>
        public const double MinScaledJacobian = 0.1;
        private const int NumOfAngles = 128;
        private const int NumOfSamples = GmshPoleChart.NumOfSamples;


        // Methods                                                                                                                  
        /// <summary>
        /// The face carries gmsh ids: its surface, its vertices and its edges. seamNumOfElements is the number of elements
        /// the seam was given and numOfElementsRound the number round it, the total of its rim, which the nodes gmsh laid
        /// must carry; where gmsh moved the nodes of the seam onto the face they are laid again. Without quads, as for a
        /// surface that is not recombined, every quad is split into two triangles. False, with the reason, when the face
        /// is left to gmsh.
        /// </summary>
        public static bool Mesh(CapFace face, int seamNumOfElements, int numOfElementsRound, bool quads, out string reason)
        {
            reason = null;
            int surfaceId = face.FaceId;
            int p;
            double period;
            if (!GmshQuadMesher.GetSeamPeriod(surfaceId, face.SeamEdgeId, out p, out period))
            {
                reason = "Edge " + face.SeamEdgeId + " is not a seam of its surface";
                return false;
            }
            int a = 1 - p;
            List<IntPtr> seamTags;
            List<double[]> seamXyz;
            int numOfNewSeamNodes;
            List<IntPtr> rimTags = new List<IntPtr>();
            List<double[]> rimXyz = new List<double[]>();
            try
            {
                GmshSeamGridMesher.GetSeamNodes(surfaceId, face.SeamEdgeId, face.SeamStartVertexNodeId,
                                                face.SeamEndVertexNodeId, seamNumOfElements, out seamTags, out seamXyz,
                                                out numOfNewSeamNodes);
                if (face.HasRim)
                    GmshRingMesher.GetLoopNodes(surfaceId, face.RimVertexNodeIds, face.RimEdgeIds, out rimTags, out rimXyz);
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            int n = numOfElementsRound;
            if (n % 2 != 0 || n < CapFace.MinNumberOfElementsRound)
            {
                reason = "it is given " + n + " elements round, " +
                         (n % 2 != 0 ? "an odd number" : "fewer than " + CapFace.MinNumberOfElementsRound);
                return false;
            }
            // gmsh splits the edges of a face whose own mesh it cannot make valid
            if ((face.HasRim && rimTags.Count != n) || seamTags.Count - 1 != seamNumOfElements)
            {
                reason = "gmsh meshed its " + (face.HasRim && rimTags.Count != n ? "rim with " + rimTags.Count : "seam with " +
                         (seamTags.Count - 1)) + " elements, not the " + (face.HasRim && rimTags.Count != n ? n :
                         seamNumOfElements) + " it was given";
                return false;
            }
            int blockA;
            int blockB;
            int depth;
            CapFace.GetBlock(n, out blockA, out blockB, out depth);
            int m = seamTags.Count - 1;
            int numOfRings = m - face.NumberOfPoles * depth;
            if (numOfRings < 1)
            {
                reason = "its seam carries " + m + " elements, which leaves no ring between " +
                         (face.HasRim ? "its rim and the block" : "the blocks") + " of " + blockA + " x " + blockB + " quads";
                return false;
            }
            // The chart: the poles at bounds of the other parameter, the arc length along its iso-lines from the first
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            double[][] seamUv;
            double[][] rimUv;
            try
            {
                seamUv = GmshThreeBlockMesher.GetParametricCoordinates(surfaceId, seamXyz);
                rimUv = face.HasRim ? GmshThreeBlockMesher.GetParametricCoordinates(surfaceId, rimXyz) : new double[0][];
            }
            catch (CaeException ex)
            {
                reason = ToReason(ex.Message);
                return false;
            }
            double seamU = face.HasRim ? rimUv[0][p] : seamUv[1][p];
            double size = GetDistance(seamXyz[0], seamXyz[m]) + GetDistance(seamXyz[0], seamXyz[m / 2]);
            double poleV;
            double farV;
            if (!GmshPoleChart.GetPoleParameter(surfaceId, p, min, max, seamXyz[m], size, out poleV, out farV) ||
                (!face.HasRim && !GmshPoleChart.GetPoleParameter(surfaceId, p, min, max, seamXyz[0], size, out farV,
                                                                 out poleV)))
            {
                reason = "its seam does not end in a pole of its surface";
                return false;
            }
            if (!face.HasRim)
            {
                double swap = poleV;
                poleV = farV;
                farV = swap;
            }
            double[][] tableV;
            double[][] tableR;
            GmshPoleChart.GetArcLengthTables(surfaceId, p, seamU, period, NumOfAngles, NumOfAngles, poleV, farV, out tableV,
                                             out tableR);
            // The seam from the pole: its levels in the chart radius
            double[] seamR = new double[m + 1];
            for (int k = 0; k <= m; k++)
                seamR[k] = GetRadius(tableV, tableR, seamU, seamUv[k][a], seamU, period);
            if (face.HasRim) seamR[m] = 0;
            else
            {
                seamR[0] = 0;
                seamR[m] = GetMeridianLength(tableR, seamU, seamU, period);
            }
            for (int k = 1; k <= m; k++)
            {
                if ((seamR[k] - seamR[k - 1]) * (face.HasRim ? -1 : 1) <= 0)
                {
                    reason = "the nodes of its seam do not run along it from " + (face.HasRim ? "its rim to its pole" :
                                                                                   "pole to pole");
                    return false;
                }
            }
            // The rim in the order of the periodic parameter
            int[] rimOrder = null;
            double[] rimTheta = null;
            double[] rimR = null;
            if (face.HasRim)
            {
                rimTheta = new double[n];
                rimR = new double[n];
                for (int i = 0; i < n; i++)
                {
                    rimTheta[i] = i == 0 ? 0 : 2 * Math.PI * Wrap(rimUv[i][p] - seamU, 0, period) / period;
                    rimR[i] = GetRadius(tableV, tableR, rimUv[i][p], rimUv[i][a], seamU, period);
                }
                rimOrder = new int[] { 0 }.Concat(Enumerable.Range(1, n - 1).OrderBy(i => rimTheta[i])).ToArray();
                if (!GmshSeamGridMesher.RunsOnceRound(rimOrder))
                {
                    reason = "the nodes of its rim do not run once round its seam";
                    return false;
                }
            }
            // The seam nodes gmsh moved onto the face get tags
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long firstNewTag = maxTag.ToInt64();
            List<IntPtr> newSeamTags = new List<IntPtr>();
            List<double[]> newSeamUv = new List<double[]>();
            for (int k = 0; k <= m; k++)
            {
                if (seamTags[k] != IntPtr.Zero) continue;
                seamTags[k] = new IntPtr(++firstNewTag);
                newSeamTags.Add(seamTags[k]);
                newSeamUv.Add(seamUv[k]);
            }
            HashSet<IntPtr> poleTags = new HashSet<IntPtr>(new IntPtr[] { seamTags[m] });
            if (!face.HasRim) poleTags.Add(seamTags[0]);
            double[][] outerLoop = null;
            if (face.HasRim)
            {
                outerLoop = new double[n][];
                for (int i = 0; i < n; i++) outerLoop[i] = new double[] { rimTheta[rimOrder[i]], rimR[rimOrder[i]] };
            }
            // The grid laid with every rounding of the blocks, the one with the best corner in space kept
            IntPtr[] bestQuadNodeTags = null;
            List<IntPtr> bestNewTags = null;
            List<double> bestNewXyz = null;
            List<double> bestNewUv = null;
            Dictionary<IntPtr, double[]> bestTagXyz = null;
            double bestMinScaledJacobian = double.NegativeInfinity;
            int bestNumOfFoldedQuads = int.MaxValue;
            List<double> roundings = new List<double>(BlockRoundings);
            double bestRounding = 0;
            for (int attempt = 0; attempt < roundings.Count; attempt++)
            {
                double rounding = roundings[attempt];
                // The blocks and the rings in the chart, each node as its angle and radius from the first pole
                double[] firstLevels = face.HasRim ? seamR.Reverse().ToArray() : seamR;
                double[][][] firstBlock = GetBlock(blockA, blockB, depth, firstLevels, rounding);
                double[][] firstBoundary = GetBlockBoundary(firstBlock, blockA, blockB);
                double[][][] secondBlock = null;
                double[][] outer = outerLoop;
                if (!face.HasRim)
                {
                    double total = seamR[m];
                    double[] secondLevels = seamR.Reverse().Select(r => total - r).ToArray();
                    secondBlock = GetBlock(blockA, blockB, depth, secondLevels, rounding);
                    outer = GetBlockBoundary(secondBlock, blockA, blockB);
                    for (int i = 0; i < n; i++)
                        outer[i][1] = GetMeridianLength(tableR, ToU(outer[i][0], seamU, period), seamU, period) - outer[i][1];
                }
                // Ring j from the outer loop, the rim or the boundary of the second block, to the first block boundary
                int numOfLines = numOfRings + 1;
                double[][][] rings = new double[numOfLines][][];
                double outerLevel = face.HasRim ? seamR[0] : seamR[m - depth];
                double innerLevel = face.HasRim ? seamR[m - depth] : seamR[depth];
                for (int j = 0; j < numOfLines; j++)
                {
                    double t = (seamR[face.HasRim ? j : m - depth - j] - innerLevel) / (outerLevel - innerLevel);
                    rings[j] = new double[n][];
                    for (int i = 0; i < n; i++)
                    {
                        double outerTheta = outer[i][0];
                        double innerTheta = firstBoundary[i][0];
                        if (i > 0 && outerTheta - innerTheta > Math.PI) innerTheta += 2 * Math.PI;
                        else if (i > 0 && innerTheta - outerTheta > Math.PI) outerTheta += 2 * Math.PI;
                        rings[j][i] = new double[] { innerTheta + t * (outerTheta - innerTheta),
                                                     firstBoundary[i][1] + t * (outer[i][1] - firstBoundary[i][1]) };
                    }
                }
                // The node tags: the rim and the seam are gmsh's, the others new
                long nodeTag = firstNewTag;
                List<IntPtr> newTags = new List<IntPtr>(newSeamTags);
                List<double[]> newUv = new List<double[]>(newSeamUv);
                Dictionary<IntPtr, double[]> tagXyz = new Dictionary<IntPtr, double[]>();
                Dictionary<IntPtr, double[]> tagUv = new Dictionary<IntPtr, double[]>();
                for (int k = 0; k <= m; k++)
                {
                    tagXyz[seamTags[k]] = seamXyz[k];
                    tagUv[seamTags[k]] = seamUv[k];
                }
                for (int i = 0; i < rimTags.Count; i++)
                {
                    tagXyz[rimTags[i]] = rimXyz[i];
                    tagUv[rimTags[i]] = rimUv[i];
                }
                Func<double, double, double[]> firstChartUv = (theta, r) =>
                    GetUv(tableV, tableR, theta, r, seamU, period, min[p], p);
                Func<double, double, double[]> secondChartUv = (theta, r) =>
                    GetUv(tableV, tableR, theta, GetMeridianLength(tableR, ToU(theta, seamU, period), seamU, period) - r,
                          seamU, period, min[p], p);
                IntPtr[][] grid = new IntPtr[numOfLines][];
                for (int j = 0; j < numOfLines; j++)
                {
                    grid[j] = new IntPtr[n];
                    grid[j][0] = seamTags[face.HasRim ? j : m - depth - j];
                    for (int i = 1; i < n; i++)
                    {
                        if (face.HasRim && j == 0) grid[j][i] = rimTags[rimOrder[i]];
                        else grid[j][i] = AddNode(firstChartUv(rings[j][i][0], rings[j][i][1]), ref nodeTag, newTags, newUv);
                    }
                }
                IntPtr[] firstColumn = Enumerable.Range(0, depth + 1)
                                                 .Select(k => seamTags[face.HasRim ? m - depth + k : depth - k]).ToArray();
                IntPtr[][] firstBlockTags = GetBlockTags(firstBlock, grid[numOfLines - 1], blockA, blockB, depth, firstColumn,
                                                         firstChartUv, ref nodeTag, newTags, newUv);
                IntPtr[][] secondBlockTags = null;
                if (!face.HasRim)
                {
                    IntPtr[] secondColumn = Enumerable.Range(0, depth + 1).Select(k => seamTags[m - depth + k]).ToArray();
                    secondBlockTags = GetBlockTags(secondBlock, grid[0], blockA, blockB, depth, secondColumn, secondChartUv,
                                                   ref nodeTag, newTags, newUv);
                }
                // The new nodes on the surface
                List<double> newXyz = new List<double>();
                List<double> flatNewUv = new List<double>();
                if (newTags.Count > 0)
                {
                    double[] coor;
                    Gmsh.Model.GetValue(2, surfaceId, newUv.SelectMany(uv => uv).ToArray(), out coor);
                    newXyz.AddRange(coor);
                    for (int i = 0; i < newTags.Count; i++)
                    {
                        tagXyz[newTags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
                        tagUv[newTags[i]] = newUv[i];
                        flatNewUv.AddRange(newUv[i]);
                    }
                }
                // The quads, each part of the grid turned with the normal of the surface, judged in space
                List<IntPtr[]> parts = new List<IntPtr[]>();
                parts.Add(GmshSeamGridMesher.GetQuadNodeTags(grid));
                parts.Add(GetBlockQuadNodeTags(firstBlockTags));
                if (secondBlockTags != null) parts.Add(GetBlockQuadNodeTags(secondBlockTags));
                List<IntPtr> quadNodeTags = new List<IntPtr>();
                foreach (var part in parts)
                {
                    TurnWithSurface(surfaceId, part, tagXyz, tagUv, poleTags);
                    quadNodeTags.AddRange(part);
                }
                int numOfFoldedQuads;
                double minScaledJacobian = GmshSeamGridMesher.GetMinScaledJacobian(surfaceId, quadNodeTags.ToArray(), tagXyz,
                                                                                   tagUv, poleTags, out numOfFoldedQuads);
                if (numOfFoldedQuads < bestNumOfFoldedQuads ||
                    (numOfFoldedQuads == bestNumOfFoldedQuads && minScaledJacobian > bestMinScaledJacobian))
                {
                    bestQuadNodeTags = quadNodeTags.ToArray();
                    bestNewTags = newTags;
                    bestNewXyz = newXyz;
                    bestNewUv = flatNewUv;
                    bestTagXyz = tagXyz;
                    bestMinScaledJacobian = minScaledJacobian;
                    bestNumOfFoldedQuads = numOfFoldedQuads;
                    bestRounding = rounding;
                }
                // and then half a step either side of the best
                if (attempt == BlockRoundings.Length - 1)
                {
                    roundings.Add(bestRounding - RoundingStep / 2);
                    roundings.Add(bestRounding + RoundingStep / 2);
                }
            }
            int numOfQuads = bestQuadNodeTags.Length / 4;
            if (bestNumOfFoldedQuads > 0)
            {
                reason = bestNumOfFoldedQuads + " of the " + numOfQuads + " quads of its grid fold";
                return false;
            }
            if (bestMinScaledJacobian < MinScaledJacobian)
            {
                reason = "its grid has a quad corner of the scaled Jacobian " + Math.Round(bestMinScaledJacobian, 3) +
                         ", below " + MinScaledJacobian;
                return false;
            }
            GmshSeamGridMesher.ReplaceSurfaceMesh(surfaceId, bestQuadNodeTags, bestNewTags, bestNewXyz, bestNewUv, bestTagXyz,
                                                  quads);
            return true;
        }
        /// <summary>
        /// The number of elements round a cap with no rim: the longest circle of the periodic parameter divided by the
        /// element size the seam was meshed with, even and at least CapFace.MinNumberOfElementsRound.
        /// </summary>
        public static int GetNumOfElementsRound(int surfaceId, int seamEdgeId, int seamNumOfElements)
        {
            int p;
            double period;
            double seamLength;
            Gmsh.Model.OCC.GetMass(1, seamEdgeId, out seamLength);
            if (!GmshQuadMesher.GetSeamPeriod(surfaceId, seamEdgeId, out p, out period) || !(seamLength > 0))
                return CapFace.MinNumberOfElementsRound;
            double[] min;
            double[] max;
            Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
            int numOfLevels = 64;
            int numOfAngles = 64;
            double[] uv = new double[2 * (numOfLevels + 1) * numOfAngles];
            int count = 0;
            for (int j = 0; j <= numOfLevels; j++)
            {
                for (int i = 0; i < numOfAngles; i++)
                {
                    uv[count + p] = min[p] + period * i / numOfAngles;
                    uv[count + 1 - p] = min[1 - p] + (max[1 - p] - min[1 - p]) * j / numOfLevels;
                    count += 2;
                }
            }
            double[] coor;
            Gmsh.Model.GetValue(2, surfaceId, uv, out coor);
            double longest = 0;
            for (int j = 0; j <= numOfLevels; j++)
            {
                double length = 0;
                for (int i = 0; i < numOfAngles; i++)
                {
                    int q0 = 3 * (j * numOfAngles + i);
                    int q1 = 3 * (j * numOfAngles + (i + 1) % numOfAngles);
                    length += Math.Sqrt(Math.Pow(coor[q1] - coor[q0], 2) + Math.Pow(coor[q1 + 1] - coor[q0 + 1], 2) +
                                        Math.Pow(coor[q1 + 2] - coor[q0 + 2], 2));
                }
                longest = Math.Max(longest, length);
            }
            double elementSize = seamLength / Math.Max(1, seamNumOfElements);
            int numOfElementsRound = 2 * (int)Math.Round(longest / elementSize / 2);
            return Math.Max(CapFace.MinNumberOfElementsRound, numOfElementsRound);
        }
        // The angle in the chart, from the seam, of a value of the periodic parameter, and back
        private static double ToU(double theta, double seamU, double period)
        {
            return seamU + period * theta / (2 * Math.PI);
        }
        // The table index below the periodic parameter u and the share of the way to the next one
        private static void GetAngleIndex(double u, double seamU, double period, out int k0, out int k1, out double t)
        {
            double position = Wrap(u - seamU, 0, period) / period * NumOfAngles;
            k0 = Math.Min(NumOfAngles - 1, (int)Math.Floor(position));
            k1 = (k0 + 1) % NumOfAngles;
            t = position - k0;
        }
        /// <summary>
        /// The chart radius of the point (u, v): the arc length from the pole along the iso-line of u up to v.
        /// </summary>
        private static double GetRadius(double[][] tableV, double[][] tableR, double u, double v,
                                        double seamU, double period)
        {
            int k0;
            int k1;
            double t;
            GetAngleIndex(u, seamU, period, out k0, out k1, out t);
            return (1 - t) * GmshPoleChart.Interpolate(tableV[k0], tableR[k0], v) +
                   t * GmshPoleChart.Interpolate(tableV[k1], tableR[k1], v);
        }
        private static double GetMeridianLength(double[][] tableR, double u, double seamU, double period)
        {
            int k0;
            int k1;
            double t;
            GetAngleIndex(u, seamU, period, out k0, out k1, out t);
            return (1 - t) * tableR[k0][NumOfSamples] + t * tableR[k1][NumOfSamples];
        }
        /// <summary>
        /// The parameters of the chart point at the angle theta from the seam and the radius r, the periodic one wrapped
        /// into the bounds from start.
        /// </summary>
        private static double[] GetUv(double[][] tableV, double[][] tableR, double theta, double r,
                                      double seamU, double period, double start, int p)
        {
            double u = ToU(theta, seamU, period);
            int k0;
            int k1;
            double t;
            GetAngleIndex(u, seamU, period, out k0, out k1, out t);
            double[] uv = new double[2];
            uv[p] = Wrap(u, start, period);
            uv[1 - p] = (1 - t) * GmshPoleChart.Interpolate(tableR[k0], tableV[k0], r) +
                        t * GmshPoleChart.Interpolate(tableR[k1], tableV[k1], r);
            return uv;
        }
        /// <summary>
        /// The block about a pole in its Cartesian chart, node [i][k] at (x, y): i from 0 to blockA across the seam, k from
        /// 0 to blockB along it, the seam the column i0 = blockA / 2 from its boundary at k = 0 to the pole at k = depth.
        /// levels are the chart radii of the seam nodes from the pole; the rows and columns lie at those levels, mirrored
        /// beyond the pole, and the block is then rounded towards a circle by the rounding, which keeps the seam.
        /// </summary>
        private static double[][][] GetBlock(int blockA, int blockB, int depth, double[] levels, double rounding)
        {
            int i0 = blockA / 2;
            Func<int, double> level = t => Math.Sign(t) * levels[Math.Abs(t)];
            double[] xs = Enumerable.Range(0, blockB + 1).Select(k => level(depth - k)).ToArray();
            double[] ys = Enumerable.Range(0, blockA + 1).Select(i => level(i - i0)).ToArray();
            double[][][] block = new double[blockA + 1][][];
            for (int i = 0; i <= blockA; i++)
            {
                block[i] = new double[blockB + 1][];
                for (int k = 0; k <= blockB; k++)
                {
                    double x = xs[k] >= 0 ? xs[k] / xs[0] : -xs[k] / xs[blockB];
                    double y = ys[i] >= 0 ? ys[i] / ys[blockA] : -ys[i] / ys[0];
                    double roundX = x * Math.Sqrt(Math.Max(0, 1 - y * y / 2));
                    double roundY = y * Math.Sqrt(Math.Max(0, 1 - x * x / 2));
                    x += rounding * (roundX - x);
                    y += rounding * (roundY - y);
                    block[i][k] = new double[] { x >= 0 ? x * xs[0] : -x * xs[blockB], y >= 0 ? y * ys[blockA] : -y * ys[0] };
                }
            }
            return block;
        }
        /// <summary>
        /// The indices [i, k] of the nodes on the boundary of the block in the order of the angle from the seam: from the
        /// seam along k = 0 to i = blockA, along it to k = blockB, back along k = blockB to i = 0, along it to k = 0 and on
        /// to the seam.
        /// </summary>
        private static List<int[]> GetBlockBoundaryIndices(int blockA, int blockB)
        {
            int i0 = blockA / 2;
            List<int[]> indices = new List<int[]>();
            for (int i = i0; i < blockA; i++) indices.Add(new int[] { i, 0 });
            for (int k = 0; k < blockB; k++) indices.Add(new int[] { blockA, k });
            for (int i = blockA; i > 0; i--) indices.Add(new int[] { i, blockB });
            for (int k = blockB; k > 0; k--) indices.Add(new int[] { 0, k });
            for (int i = 0; i < i0; i++) indices.Add(new int[] { i, 0 });
            return indices;
        }
        /// <summary>
        /// The boundary nodes of the block in the order of the angle from the seam as their angle in [0, 2 pi) and radius.
        /// </summary>
        private static double[][] GetBlockBoundary(double[][][] block, int blockA, int blockB)
        {
            List<int[]> indices = GetBlockBoundaryIndices(blockA, blockB);
            double[][] boundary = new double[indices.Count][];
            for (int i = 0; i < indices.Count; i++)
            {
                double[] point = block[indices[i][0]][indices[i][1]];
                double theta = i == 0 ? 0 : Math.Atan2(point[1], point[0]);
                if (theta < 0) theta += 2 * Math.PI;
                boundary[i] = new double[] { theta, Math.Sqrt(point[0] * point[0] + point[1] * point[1]) };
            }
            return boundary;
        }
        /// <summary>
        /// The node tags of the block: its boundary those of the innermost ring, its seam column those of the seam from the
        /// boundary to the pole, the others new nodes at the parameters chartUv gives for their angle and radius.
        /// </summary>
        private static IntPtr[][] GetBlockTags(double[][][] block, IntPtr[] boundaryTags, int blockA, int blockB, int depth,
                                               IntPtr[] seamColumnTags, Func<double, double, double[]> chartUv,
                                               ref long nodeTag, List<IntPtr> newTags, List<double[]> newUv)
        {
            int i0 = blockA / 2;
            IntPtr[][] tags = new IntPtr[blockA + 1][];
            for (int i = 0; i <= blockA; i++) tags[i] = new IntPtr[blockB + 1];
            List<int[]> indices = GetBlockBoundaryIndices(blockA, blockB);
            for (int i = 0; i < indices.Count; i++) tags[indices[i][0]][indices[i][1]] = boundaryTags[i];
            for (int k = 0; k <= depth; k++) tags[i0][k] = seamColumnTags[k];
            for (int i = 1; i < blockA; i++)
            {
                for (int k = 1; k < blockB; k++)
                {
                    if (tags[i][k] != IntPtr.Zero) continue;
                    double[] point = block[i][k];
                    double theta = Math.Atan2(point[1], point[0]);
                    if (theta < 0) theta += 2 * Math.PI;
                    tags[i][k] = AddNode(chartUv(theta, Math.Sqrt(point[0] * point[0] + point[1] * point[1])), ref nodeTag,
                                         newTags, newUv);
                }
            }
            return tags;
        }
        private static IntPtr AddNode(double[] uv, ref long nodeTag, List<IntPtr> newTags, List<double[]> newUv)
        {
            IntPtr tag = new IntPtr(++nodeTag);
            newTags.Add(tag);
            newUv.Add(uv);
            return tag;
        }
        private static IntPtr[] GetBlockQuadNodeTags(IntPtr[][] tags)
        {
            int blockA = tags.Length - 1;
            int blockB = tags[0].Length - 1;
            IntPtr[] quadNodeTags = new IntPtr[4 * blockA * blockB];
            int count = 0;
            for (int i = 0; i < blockA; i++)
            {
                for (int k = 0; k < blockB; k++)
                {
                    quadNodeTags[count++] = tags[i][k];
                    quadNodeTags[count++] = tags[i + 1][k];
                    quadNodeTags[count++] = tags[i + 1][k + 1];
                    quadNodeTags[count++] = tags[i][k + 1];
                }
            }
            return quadNodeTags;
        }
        /// <summary>
        /// Turns the quads of a part of the grid, which all turn one way, with the normal of the surface, judged by its
        /// first quad without a pole node.
        /// </summary>
        private static void TurnWithSurface(int surfaceId, IntPtr[] quadNodeTags, Dictionary<IntPtr, double[]> tagXyz,
                                            Dictionary<IntPtr, double[]> tagUv, HashSet<IntPtr> poleTags)
        {
            int numOfQuads = quadNodeTags.Length / 4;
            for (int q = 0; q < numOfQuads; q++)
            {
                bool hasPole = false;
                for (int c = 0; c < 4; c++) hasPole |= poleTags.Contains(quadNodeTags[4 * q + c]);
                if (hasPole) continue;
                double[] p0 = tagXyz[quadNodeTags[4 * q]];
                double[] p1 = tagXyz[quadNodeTags[4 * q + 1]];
                double[] p2 = tagXyz[quadNodeTags[4 * q + 2]];
                double[] p3 = tagXyz[quadNodeTags[4 * q + 3]];
                double[] d1 = new double[] { p2[0] - p0[0], p2[1] - p0[1], p2[2] - p0[2] };
                double[] d2 = new double[] { p3[0] - p1[0], p3[1] - p1[1], p3[2] - p1[2] };
                double[] normal = new double[] { d1[1] * d2[2] - d1[2] * d2[1], d1[2] * d2[0] - d1[0] * d2[2],
                                                 d1[0] * d2[1] - d1[1] * d2[0] };
                double[] surfaceNormal = GmshQuadMesher.GetNormals(surfaceId, new double[][] { tagUv[quadNodeTags[4 * q]] })[0];
                double dot = normal[0] * surfaceNormal[0] + normal[1] * surfaceNormal[1] + normal[2] * surfaceNormal[2];
                if (dot >= 0) return;
                for (int r = 0; r < numOfQuads; r++)
                {
                    IntPtr swap = quadNodeTags[4 * r + 1];
                    quadNodeTags[4 * r + 1] = quadNodeTags[4 * r + 3];
                    quadNodeTags[4 * r + 3] = swap;
                }
                return;
            }
        }
        private static double Wrap(double value, double start, double period)
        {
            double wrapped = (value - start) % period;
            if (wrapped < 0) wrapped += period;
            return start + wrapped;
        }
        private static double GetDistance(double[] p, double[] q)
        {
            return Math.Sqrt((p[0] - q[0]) * (p[0] - q[0]) + (p[1] - q[1]) * (p[1] - q[1]) + (p[2] - q[2]) * (p[2] - q[2]));
        }
        private static string ToReason(string message)
        {
            message = message.Trim().TrimEnd('.');
            if (message.StartsWith("The ")) message = "the " + message.Substring(4);
            return message;
        }
    }
}
