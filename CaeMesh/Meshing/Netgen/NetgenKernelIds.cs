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
    public static class NetgenKernelIds
    {
        // Methods                                                                                                                  
        /// <summary>
        /// The numbers Netgen gives the faces and edges of the parts, by gmsh topology id. Netgen numbers those of a
        /// compound solid by solid, each solid adding the ones no solid before it has, in the order the solid on its
        /// own numbers them, which is the order of the kernel ids of its sub-part. A face or an edge two solids share
        /// is found by its geometry. A sub-part whose kernel ids skip a number holds an item it does not show, which
        /// may or may not be shared, so its numbers cannot be told.
        /// </summary>
        public static bool Get(FeMesh geometry, BasePart[] parts, out Dictionary<int, int> faceKernelIds,
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
