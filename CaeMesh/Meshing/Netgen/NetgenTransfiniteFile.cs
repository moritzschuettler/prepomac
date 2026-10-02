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
    /// messages use. Netgen then detects no structured faces of its own.
    /// </summary>
    public static class NetgenTransfiniteFile
    {
        // Methods
        /// <summary>
        /// pinnedEdgeCounts holds the number of elements the user set on an edge of the part, by edge id.
        /// </summary>
        public static void Write(string fileName, FeMesh geometry, BasePart part, bool threeSided, bool fourSided,
                                 double transfiniteAngleDeg, Dictionary<int, int> pinnedEdgeCounts)
        {
            TransfiniteFace[] faces = TransfiniteFaceDetector.Detect(geometry, part, threeSided, fourSided,
                                                                     transfiniteAngleDeg);
            VisualizationData vis = part.Visualization;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("faces " + faces.Length);
            HashSet<int> edgeIds = new HashSet<int>();
            foreach (var face in faces)
            {
                sb.AppendLine("face " + GetKernelId(vis.FaceKernelIds, face.FaceId) + " " + face.NumberOfSides +
                              " surface " + (face.FaceId + 1) + " of " + part.Name);
                foreach (var side in face.SideEdgeIds)
                {
                    sb.AppendLine("side " + side.Length + " " +
                                  string.Join(" ", side.Select(edgeId => GetKernelId(vis.EdgeKernelIds, edgeId))));
                    edgeIds.UnionWith(side);
                }
            }
            sb.AppendLine("edges " + edgeIds.Count);
            int count;
            foreach (var edgeId in edgeIds.OrderBy(id => id))
            {
                if (pinnedEdgeCounts == null || !pinnedEdgeCounts.TryGetValue(edgeId, out count)) count = 0;
                sb.AppendLine("edge " + GetKernelId(vis.EdgeKernelIds, edgeId) + " " + count + " edge " + (edgeId + 1) +
                              " of " + part.Name);
            }
            System.IO.File.WriteAllText(fileName, sb.ToString());
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
