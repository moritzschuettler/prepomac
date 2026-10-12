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
        // Methods
        /// <summary>
        /// The parts are the solids of the brep Netgen meshes, in the order the brep holds them: one part, or the
        /// sub-parts of a compound. pinnedEdgeCounts holds the number of elements the user set on an edge, by gmsh
        /// topology id. Returns false, writing nothing, when the faces and edges of the sub-parts cannot be given the
        /// numbers Netgen gives them in the compound.
        /// </summary>
        public static bool Write(string fileName, FeMesh geometry, BasePart[] parts, TransfiniteThreeSidedEnum threeSided,
                                 bool fourSided, double transfiniteAngleDeg, Dictionary<int, int> pinnedEdgeCounts)
        {
            Dictionary<int, int> faceKernelIds;
            Dictionary<int, int> edgeKernelIds;
            if (!GetCompoundKernelIds(geometry, parts, out faceKernelIds, out edgeKernelIds)) return false;
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
                    faceLines.AppendLine("face " + faceKernelIds[FeMesh.GmshTopologyId(face.FaceId, part.PartId)] +
                                         " " + face.NumberOfSides +
                                         (polarCornerIndex >= 0 ? " polar " + polarCornerIndex : "") +
                                         " surface " + (face.FaceId + 1) + " of " + part.Name);
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
        /// The numbers Netgen gives the faces and edges of the parts, by gmsh topology id. Netgen numbers those of a
        /// compound solid by solid, each solid adding the ones no solid before it has, in the order the solid on its
        /// own numbers them, which is the order of the kernel ids of its sub-part. A face or an edge two solids share
        /// is found by its geometry. A sub-part whose kernel ids skip a number holds an item it does not show, which
        /// may or may not be shared, so its numbers cannot be told.
        /// </summary>
        private static bool GetCompoundKernelIds(FeMesh geometry, BasePart[] parts, out Dictionary<int, int> faceKernelIds,
                                                 out Dictionary<int, int> edgeKernelIds)
        {
            faceKernelIds = new Dictionary<int, int>();
            edgeKernelIds = new Dictionary<int, int>();
            if (parts.Length == 1)
            {
                VisualizationData partVis = parts[0].Visualization;
                for (int i = 0; i < partVis.FaceCount; i++)
                    faceKernelIds.Add(FeMesh.GmshTopologyId(i, parts[0].PartId), GetKernelId(partVis.FaceKernelIds, i));
                for (int i = 0; i < partVis.EdgeCount; i++)
                    edgeKernelIds.Add(FeMesh.GmshTopologyId(i, parts[0].PartId), GetKernelId(partVis.EdgeKernelIds, i));
                return true;
            }
            //
            Dictionary<int, int> earlierFaceIds = geometry.GetSharedSurfaceIds(parts, true);
            Dictionary<int, int> earlierEdgeIds = geometry.GetSharedEdgeIds(parts, true);
            //
            int faceCount = 0;
            int edgeCount = 0;
            foreach (var part in parts)
            {
                VisualizationData vis = part.Visualization;
                if (!AddKernelIds(vis.FaceKernelIds, vis.FaceCount, part.PartId, earlierFaceIds, faceKernelIds,
                                  ref faceCount)) return false;
                if (!AddKernelIds(vis.EdgeKernelIds, vis.EdgeCount, part.PartId, earlierEdgeIds, edgeKernelIds,
                                  ref edgeCount)) return false;
            }
            return true;
        }
        private static bool AddKernelIds(int[] partKernelIds, int numOfItems, int partId,
                                         Dictionary<int, int> earlierItemIds, Dictionary<int, int> kernelIds,
                                         ref int count)
        {
            int[] itemIds = Enumerable.Range(0, numOfItems).ToArray();
            int[] partItemKernelIds = itemIds.Select(itemId => GetKernelId(partKernelIds, itemId)).ToArray();
            Array.Sort(partItemKernelIds, itemIds);
            for (int i = 0; i < itemIds.Length; i++)
            {
                if (partItemKernelIds[i] != i + 1) return false;
            }
            //
            int topologyId;
            int earlierTopologyId;
            foreach (var itemId in itemIds)
            {
                topologyId = FeMesh.GmshTopologyId(itemId, partId);
                if (earlierItemIds.TryGetValue(topologyId, out earlierTopologyId))
                    kernelIds.Add(topologyId, kernelIds[earlierTopologyId]);
                else kernelIds.Add(topologyId, ++count);
            }
            return true;
        }
        // A part from a file written before the kernel ids were kept is ordered by them
        private static int GetKernelId(int[] kernelIds, int itemId)
        {
            if (kernelIds != null && itemId >= 0 && itemId < kernelIds.Length && kernelIds[itemId] > 0)
                return kernelIds[itemId];
            return itemId + 1;
        }
    }
}
