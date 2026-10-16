// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CaeMesh
{
    /// <summary>
    /// The structured faces Netgen meshes, by the one definition all meshers follow, written for NetGenMesher: every
    /// face and edge by the number the modelling kernel gave it, which is the numbering Netgen meshes with, the sides of
    /// every face as chains of edges in loop order, the numbers of elements the user set on edges and the names its
    /// messages use. A polar three-sided face names the side that starts at its collapsed corner. Netgen then detects
    /// no structured faces of its own.
    /// </summary>
    public static class NetgenTransfiniteFile
    {
        // Variables
        private const int _timeoutSeconds = 20;


        // Methods
        /// <summary>
        /// The parts are the solids of the brep Netgen meshes, in the order the brep holds them: one part, or the
        /// sub-parts of a compound. pinnedEdgeCounts holds the number of elements the user set on an edge and
        /// sizedEdgeCounts the number an element size the user set asks for, both by gmsh topology id. Returns false,
        /// writing nothing, when the faces and edges of the sub-parts cannot be given the numbers Netgen gives them in
        /// the compound.
        /// </summary>
        public static bool Write(string fileName, FeMesh geometry, BasePart[] parts, TransfiniteThreeSidedEnum threeSided,
                                 bool fourSided, double transfiniteAngleDeg, Dictionary<int, int> pinnedEdgeCounts,
                                 Dictionary<int, int> sizedEdgeCounts = null)
        {
            Dictionary<int, int> faceKernelIds;
            Dictionary<int, int> edgeKernelIds;
            if (!NetgenKernelIds.Get(geometry, parts, out faceKernelIds, out edgeKernelIds)) return false;
            //
            int kernelId;
            int topologyId;
            StringBuilder faceLines = new StringBuilder();
            HashSet<int> faceIds = new HashSet<int>();
            Dictionary<int, string> edgeLines = new Dictionary<int, string>();
            Dictionary<int, int> pinnedCounts = new Dictionary<int, int>();
            if (pinnedEdgeCounts != null)
            {
                foreach (var entry in pinnedEdgeCounts)
                {
                    if (edgeKernelIds.TryGetValue(entry.Key, out kernelId)) pinnedCounts[kernelId] = entry.Value;
                }
            }
            Dictionary<int, int> sizedCounts = new Dictionary<int, int>();
            if (sizedEdgeCounts != null)
            {
                foreach (var entry in sizedEdgeCounts)
                {
                    if (edgeKernelIds.TryGetValue(entry.Key, out kernelId) && !pinnedCounts.ContainsKey(kernelId))
                        sizedCounts[kernelId] = entry.Value;
                }
            }
            List<TransfiniteSidePair> pairs = new List<TransfiniteSidePair>();
            List<TransfiniteSideTriple> triples = new List<TransfiniteSideTriple>();
            Dictionary<int, string> faceNames = new Dictionary<int, string>();
            foreach (var part in parts)
            {
                foreach (var face in TransfiniteFaceDetector.Detect(geometry, part, threeSided,
                                                                    fourSided, transfiniteAngleDeg))
                {
                    // A face two solids share is meshed once
                    if (!faceIds.Add(faceKernelIds[FeMesh.GmshTopologyId(face.FaceId, part.PartId)])) continue;
                    //
                    int polarCornerIndex = threeSided == TransfiniteThreeSidedEnum.Polar ?
                                           TransfiniteFaceDetector.GetPolarCornerIndex(geometry, part, face) : -1;
                    int faceKernelId = faceKernelIds[FeMesh.GmshTopologyId(face.FaceId, part.PartId)];
                    faceNames[faceKernelId] = "surface " + (face.FaceId + 1) + " of " + part.Name;
                    faceLines.AppendLine("face " + faceKernelId + " " + face.NumberOfSides +
                                         (polarCornerIndex >= 0 ? " polar " + polarCornerIndex : "") + " " +
                                         faceNames[faceKernelId]);
                    int[][] sides = face.SideEdgeIds.Select(side => side.Select(edgeId =>
                                        edgeKernelIds[FeMesh.GmshTopologyId(edgeId, part.PartId)]).ToArray()).ToArray();
                    if (face.NumberOfSides == 4)
                    {
                        pairs.Add(new TransfiniteSidePair(faceKernelId, sides[0], sides[2]));
                        pairs.Add(new TransfiniteSidePair(faceKernelId, sides[1], sides[3]));
                    }
                    else if (polarCornerIndex >= 0)
                        pairs.Add(new TransfiniteSidePair(faceKernelId, sides[(polarCornerIndex + 2) % 3],
                                                          sides[polarCornerIndex]));
                    else triples.Add(new TransfiniteSideTriple(faceKernelId, sides));
                    foreach (var side in face.SideEdgeIds)
                    {
                        faceLines.AppendLine("side " + side.Length + " " +
                                             string.Join(" ", side.Select(edgeId =>
                                                 edgeKernelIds[FeMesh.GmshTopologyId(edgeId, part.PartId)])));
                        foreach (var edgeId in side)
                        {
                            topologyId = FeMesh.GmshTopologyId(edgeId, part.PartId);
                            kernelId = edgeKernelIds[topologyId];
                            if (!edgeLines.ContainsKey(kernelId))
                                edgeLines.Add(kernelId, "edge " + (edgeId + 1) + " of " + part.Name);
                        }
                    }
                }
            }
            // Netgen divides every edge with a number set into that many elements, on a structured face or not
            foreach (var part in parts)
            {
                for (int edgeId = 0; edgeId < part.Visualization.EdgeCount; edgeId++)
                {
                    topologyId = FeMesh.GmshTopologyId(edgeId, part.PartId);
                    if (pinnedEdgeCounts == null || !pinnedEdgeCounts.ContainsKey(topologyId) ||
                        !edgeKernelIds.TryGetValue(topologyId, out kernelId) || edgeLines.ContainsKey(kernelId)) continue;
                    edgeLines.Add(kernelId, "edge " + (edgeId + 1) + " of " + part.Name);
                }
            }
            // Netgen keeps the numbers of elements set, so the element sizes are settled here among them
            foreach (var entry in KeepElementSizes(pairs, triples, pinnedCounts, sizedCounts, faceNames, edgeLines))
                pinnedCounts[entry.Key] = entry.Value;
            //
            int count;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("faces " + faceIds.Count);
            sb.Append(faceLines);
            sb.AppendLine("edges " + edgeLines.Count);
            foreach (var entry in edgeLines.OrderBy(entry => entry.Key))
            {
                if (!pinnedCounts.TryGetValue(entry.Key, out count)) count = 0;
                sb.AppendLine("edge " + entry.Key + " " + count + " " + entry.Value);
            }
            System.IO.File.WriteAllText(fileName, sb.ToString());
            return true;
        }
        /// <summary>
        /// The numbers of elements of the edges of the structured faces with an element size, each within the range that
        /// keeps its element size and closest to the number it asks for, that the faces can take together with the
        /// numbers of elements set. The other edges are free here, and Netgen balances them around these. When no such
        /// numbers exist, the meshing is refused with the faces and the edges that cannot agree.
        /// </summary>
        private static Dictionary<int, int> KeepElementSizes(List<TransfiniteSidePair> pairs,
                                                             List<TransfiniteSideTriple> triples,
                                                             Dictionary<int, int> pinnedCounts,
                                                             Dictionary<int, int> sizedCounts,
                                                             Dictionary<int, string> faceNames,
                                                             Dictionary<int, string> edgeLines)
        {
            HashSet<int> sideEdgeIds = new HashSet<int>(pairs.SelectMany(pair => pair.SideA.Concat(pair.SideB)));
            sideEdgeIds.UnionWith(triples.SelectMany(triple => triple.Sides.SelectMany(side => side)));
            if (!sideEdgeIds.Any(sizedCounts.ContainsKey)) return new Dictionary<int, int>();
            //
            int count;
            int minCount;
            int maxCount;
            IntervalProblem problem = new IntervalProblem(edgeId => sizedCounts.TryGetValue(edgeId, out count) ? count :
                                                                    pinnedCounts.TryGetValue(edgeId, out count) ? count : 1);
            foreach (var edgeId in sideEdgeIds)
            {
                if (pinnedCounts.TryGetValue(edgeId, out count)) problem.FixEdge(edgeId, count);
                else if (sizedCounts.TryGetValue(edgeId, out count))
                {
                    TransfiniteEdgeCountBalancer.GetElementSizeCountRange(count, out minCount, out maxCount);
                    problem.SetMinimum(edgeId, minCount);
                    problem.SetMaximum(edgeId, maxCount);
                }
                else problem.SetWeight(edgeId, 1E-6);
            }
            foreach (var pair in pairs)
                problem.AddEqualSums(faceNames[pair.FaceId], IntervalRowTierEnum.Hard, pair.SideA, pair.SideB, pair.FaceId);
            foreach (var triple in triples)
                problem.AddThreeSided(faceNames[triple.FaceId], IntervalRowTierEnum.Hard, triple.Sides, triple.FaceId);
            //
            IntervalSolution solution = problem.Solve(false, _timeoutSeconds);
            if (solution.HasCounts)
                return sizedCounts.Where(entry => sideEdgeIds.Contains(entry.Key))
                                  .ToDictionary(entry => entry.Key, entry => solution.EdgeIdNumOfElements[entry.Key]);
            if (solution.Status != LpSolve.Infeasible)
                throw new CaeException("lp_solve found no element counts for the structured faces (" + solution.StatusName +
                                       ").");
            //
            IntervalSolution relaxed = problem.Solve(true, _timeoutSeconds);
            List<IntervalRow> conflicts = relaxed.HasCounts ? relaxed.ViolatedRows : problem.Rows;
            Func<int, string> describe = edgeId =>
            {
                if (pinnedCounts.TryGetValue(edgeId, out count)) return edgeLines[edgeId] + " set to " + count + " elements";
                TransfiniteEdgeCountBalancer.GetElementSizeCountRange(sizedCounts[edgeId], out minCount, out maxCount);
                return edgeLines[edgeId] + " given " + sizedCounts[edgeId] + " elements by its element size, which " +
                       minCount + " to " + maxCount + " elements keep";
            };
            List<string> lines = new List<string>();
            foreach (var row in conflicts)
            {
                int[] edgeIds = row.Sides.SelectMany(side => side).Distinct()
                                   .Where(edgeId => pinnedCounts.ContainsKey(edgeId) || sizedCounts.ContainsKey(edgeId))
                                   .ToArray();
                lines.Add("The sizes set on the edges of " + row.Name + " cannot all be kept by its structured grid (" +
                          string.Join("; ", edgeIds.Select(describe)) + ").");
            }
            lines.Add("Set element sizes and numbers of elements that agree on these edges, or switch off the transfinite " +
                      "meshing of the 3-sided and 4-sided surfaces.");
            throw new CaeException(string.Join(Environment.NewLine, lines));
        }
    }
}
