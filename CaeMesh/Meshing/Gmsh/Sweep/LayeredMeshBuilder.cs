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
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Turns a filled sweep line table into gmsh nodes and elements.
    ///
    /// The element node order is derived from a signed volume test taken over the whole block
    /// rather than assumed, so a source face that comes back from the geometry kernel with a
    /// reversed winding no longer produces inverted prisms and hexahedra. The winding of the cap
    /// elements on the target surface follows from the same test.
    ///
    /// The builder deliberately knows nothing about how the layer positions were produced. Extrude
    /// and revolve compute their layers analytically and a many-to-one sweep will compute them one
    /// block at a time, but all of them can emit through this class.
    /// </summary>
    public static class LayeredMeshBuilder
    {
        // Methods
        public static void Build(SweepMeshData data, SweepBlock block, TargetProjection projection, bool flip)
        {
            AddNodes(data, block, projection);
            AddElements(data, block, projection, flip);
        }
        private static void AddNodes(SweepMeshData data, SweepBlock block, TargetProjection projection)
        {
            int numLayers = data.NumLayers;
            int[] interior = data.InteriorNodes;
            double[] coords = data.Coords;
            // Interior layers belong to the volume. A pinned line has its intermediate nodes made here too;
            // its source and target nodes exist already, on the carried copy and on the imprint.
            int[] volumeNodes = interior.Concat(data.PinnedNodes).ToArray();
            int count = volumeNodes.Length * Math.Max(0, numLayers - 2);
            if (count > 0)
            {
                IntPtr[] tags = new IntPtr[count];
                double[] positions = new double[3 * count];
                int index = 0;
                for (int layer = 1; layer <= numLayers - 2; layer++)
                {
                    for (int k = 0; k < volumeNodes.Length; k++)
                    {
                        int i = data.Index(layer, volumeNodes[k]);
                        tags[index] = data.NodeTags[i];
                        positions[3 * index] = coords[3 * i];
                        positions[3 * index + 1] = coords[3 * i + 1];
                        positions[3 * index + 2] = coords[3 * i + 2];
                        index++;
                    }
                }
                Gmsh.Model.Mesh.AddNodes(3, block.VolumeId, tags, positions);
            }
            // The target layer belongs to the target surfaces, and carries the parametric
            // coordinates of the projection so that second order midside nodes can be placed on the
            // geometry
            int targetLayer = numLayers - 1;
            Dictionary<int, List<int>> surfaceGroups = new Dictionary<int, List<int>>();
            for (int k = 0; k < interior.Length; k++)
            {
                List<int> group;
                if (!surfaceGroups.TryGetValue(projection.SurfaceIds[k], out group))
                {
                    group = new List<int>();
                    surfaceGroups.Add(projection.SurfaceIds[k], group);
                }
                group.Add(k);
            }
            foreach (var entry in surfaceGroups)
            {
                List<int> group = entry.Value;
                IntPtr[] tags = new IntPtr[group.Count];
                double[] positions = new double[3 * group.Count];
                double[] parametric = new double[2 * group.Count];
                for (int i = 0; i < group.Count; i++)
                {
                    int k = group[i];
                    int index = data.Index(targetLayer, interior[k]);
                    tags[i] = data.NodeTags[index];
                    positions[3 * i] = coords[3 * index];
                    positions[3 * i + 1] = coords[3 * index + 1];
                    positions[3 * i + 2] = coords[3 * index + 2];
                    parametric[2 * i] = projection.ParametricCoords[2 * k];
                    parametric[2 * i + 1] = projection.ParametricCoords[2 * k + 1];
                }
                Gmsh.Model.Mesh.AddNodes(2, entry.Key, tags, positions, parametric);
            }
        }
        private static void AddElements(SweepMeshData data, SweepBlock block, TargetProjection projection, bool flip)
        {
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            int nextElementTag = maxTag.ToInt32() + 1;
            //
            int numLayers = data.NumLayers;
            int[][] sourceElements = data.SourceElements;
            SweepQualityBuffers buffers = new SweepQualityBuffers();
            int[] nodes = buffers.Nodes;
            List<IntPtr> wedgeTags = new List<IntPtr>();
            List<IntPtr> wedgeNodes = new List<IntPtr>();
            List<IntPtr> hexaTags = new List<IntPtr>();
            List<IntPtr> hexaNodes = new List<IntPtr>();
            // Solid elements
            for (int bottom = 0; bottom < numLayers - 1; bottom++)
            {
                for (int e = 0; e < sourceElements.Length; e++)
                {
                    int n = sourceElements[e].Length;
                    SweepQuality.FillOrientedElementNodes(data, e, bottom, buffers);
                    //
                    if (n == 3)
                    {
                        wedgeTags.Add((IntPtr)nextElementTag++);
                        for (int i = 0; i < 6; i++) wedgeNodes.Add(data.NodeTags[nodes[i]]);
                    }
                    else
                    {
                        hexaTags.Add((IntPtr)nextElementTag++);
                        for (int i = 0; i < 8; i++) hexaNodes.Add(data.NodeTags[nodes[i]]);
                    }
                }
            }
            AddElementsOfOneType(3, block.VolumeId, (int)GmshElementType.LinearWedge, wedgeTags, wedgeNodes);
            AddElementsOfOneType(3, block.VolumeId, (int)GmshElementType.LinearHexa, hexaTags, hexaNodes);
            // Cap elements on the target surfaces
            int[] elementSurfaceIds = AssignCapSurfaces(data, block, projection);
            Dictionary<int, List<IntPtr>> triangleTags = new Dictionary<int, List<IntPtr>>();
            Dictionary<int, List<IntPtr>> triangleNodes = new Dictionary<int, List<IntPtr>>();
            Dictionary<int, List<IntPtr>> quadrilateralTags = new Dictionary<int, List<IntPtr>>();
            Dictionary<int, List<IntPtr>> quadrilateralNodes = new Dictionary<int, List<IntPtr>>();
            //
            for (int e = 0; e < sourceElements.Length; e++)
            {
                int n = sourceElements[e].Length;
                int surfaceId = elementSurfaceIds[e];
                bool elementFlipped = SweepQuality.FillOrientedElementNodes(data, e, numLayers - 2, buffers);
                // The face of a positively oriented element formed by its second group of nodes has
                // an outward normal in that node order; the face formed by the first group has an
                // outward normal in the reverse order. The target layer is the second group unless
                // the block was flipped.
                Dictionary<int, List<IntPtr>> tagsBySurface = n == 3 ? triangleTags : quadrilateralTags;
                Dictionary<int, List<IntPtr>> nodesBySurface = n == 3 ? triangleNodes : quadrilateralNodes;
                List<IntPtr> tags;
                List<IntPtr> capNodes;
                if (!tagsBySurface.TryGetValue(surfaceId, out tags))
                {
                    tags = new List<IntPtr>();
                    capNodes = new List<IntPtr>();
                    tagsBySurface.Add(surfaceId, tags);
                    nodesBySurface.Add(surfaceId, capNodes);
                }
                else capNodes = nodesBySurface[surfaceId];
                //
                tags.Add((IntPtr)nextElementTag++);
                if (elementFlipped)
                {
                    for (int i = n - 1; i >= 0; i--) capNodes.Add(data.NodeTags[nodes[i]]);
                }
                else
                {
                    for (int i = 0; i < n; i++) capNodes.Add(data.NodeTags[nodes[n + i]]);
                }
            }
            //
            foreach (var entry in triangleTags)
            {
                AddElementsOfOneType(2, entry.Key, (int)GmshElementType.LinearTriangle, entry.Value,
                                     triangleNodes[entry.Key]);
            }
            foreach (var entry in quadrilateralTags)
            {
                AddElementsOfOneType(2, entry.Key, (int)GmshElementType.LinearQuadrilateral, entry.Value,
                                     quadrilateralNodes[entry.Key]);
            }
        }
        /// <summary>
        /// Decides which target surface each cap element belongs to. With a single target surface
        /// there is nothing to decide. With a split target, each element follows the majority of
        /// its own nodes; elements whose nodes all sit on the target boundary have no vote and are
        /// resolved by projecting their centre, in one batch per surface.
        /// </summary>
        private static int[] AssignCapSurfaces(SweepMeshData data, SweepBlock block, TargetProjection projection)
        {
            int[][] sourceElements = data.SourceElements;
            int[] result = new int[sourceElements.Length];
            //
            if (block.TargetSurfaceIds.Count == 1)
            {
                int only = block.TargetSurfaceIds.First();
                for (int e = 0; e < result.Length; e++) result[e] = only;
                return result;
            }
            // Source node -> index into the projection arrays, or -1 for a boundary node
            int[] projectionIndex = new int[data.NumNodes];
            for (int i = 0; i < projectionIndex.Length; i++) projectionIndex[i] = -1;
            for (int k = 0; k < data.InteriorNodes.Length; k++) projectionIndex[data.InteriorNodes[k]] = k;
            //
            int targetLayer = data.NumLayers - 1;
            double[] coords = data.Coords;
            Dictionary<int, int> votes = new Dictionary<int, int>();
            List<int> undecided = new List<int>();
            //
            for (int e = 0; e < sourceElements.Length; e++)
            {
                votes.Clear();
                int[] element = sourceElements[e];
                for (int i = 0; i < element.Length; i++)
                {
                    int k = projectionIndex[element[i]];
                    if (k < 0) continue;
                    int surfaceId = projection.SurfaceIds[k];
                    int current;
                    votes.TryGetValue(surfaceId, out current);
                    votes[surfaceId] = current + 1;
                }
                //
                int best = -1;
                int bestCount = 0;
                foreach (var entry in votes)
                {
                    if (entry.Value > bestCount) { bestCount = entry.Value; best = entry.Key; }
                }
                result[e] = best;
                if (best < 0) undecided.Add(e);
            }
            //
            if (undecided.Count > 0)
            {
                double[] input = new double[3 * undecided.Count];
                for (int i = 0; i < undecided.Count; i++)
                {
                    int[] element = sourceElements[undecided[i]];
                    double cx = 0, cy = 0, cz = 0;
                    for (int j = 0; j < element.Length; j++)
                    {
                        int index = 3 * data.Index(targetLayer, element[j]);
                        cx += coords[index];
                        cy += coords[index + 1];
                        cz += coords[index + 2];
                    }
                    input[3 * i] = cx / element.Length;
                    input[3 * i + 1] = cy / element.Length;
                    input[3 * i + 2] = cz / element.Length;
                }
                //
                double[] bestDistance = new double[undecided.Count];
                int[] bestSurfaceId = new int[undecided.Count];
                SweepProjector.ProjectOntoNearestSurface(input, block.TargetSurfaceIds, null, null, bestDistance,
                                                         bestSurfaceId);
                //
                for (int i = 0; i < undecided.Count; i++)
                {
                    if (bestSurfaceId[i] < 0)
                        throw new CaeException("A sweep target element could not be assigned to a target surface.");
                    result[undecided[i]] = bestSurfaceId[i];
                }
            }
            //
            return result;
        }
        private static void AddElementsOfOneType(int dim, int tag, int elementType, List<IntPtr> tags,
                                                 List<IntPtr> nodes)
        {
            if (tags == null || tags.Count == 0) return;
            //
            IntPtr[][] elementTags = new IntPtr[1][];
            elementTags[0] = tags.ToArray();
            IntPtr[][] nodeTags = new IntPtr[1][];
            nodeTags[0] = nodes.ToArray();
            Gmsh.Model.Mesh.AddElements(dim, tag, new int[] { elementType }, elementTags, nodeTags);
        }
    }
}
