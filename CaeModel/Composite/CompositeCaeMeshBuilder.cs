// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Linq;
using CaeMesh;

namespace CaeModel
{
    // Builds a PrePoMax finite element mesh from the ply surfaces stored inside a Composite CAE HDF5 file.
    // It is used when the model has no mesh of its own so that a layup can still be imported and mapped.
    public static class CompositeCaeMeshBuilder
    {
        public static FeMesh BuildMesh(CompositeCaeData data, out int layerId, out int plyCount)
        {
            List<CompositeCaePly> layerPlies = GetSourceLayer(data, out layerId);
            plyCount = layerPlies.Count;
            if (plyCount == 0) return null;
            //
            return BuildMesh(layerPlies.Select(x => x.Surface));
        }
        // A single ply surface is only a patch of the part - the plies of one layer are complementary and
        // together they cover the whole part. The layer with the largest total area is used.
        private static List<CompositeCaePly> GetSourceLayer(CompositeCaeData data, out int layerId)
        {
            layerId = 0;
            //
            Dictionary<int, List<CompositeCaePly>> pliesByLayer = new Dictionary<int, List<CompositeCaePly>>();
            foreach (CompositeCaePly ply in data.Plies)
            {
                if (ply.Surface == null || ply.Surface.Elements == null || ply.Surface.Elements.Length == 0) continue;
                //
                List<CompositeCaePly> plies;
                if (!pliesByLayer.TryGetValue(ply.LayerId, out plies))
                {
                    plies = new List<CompositeCaePly>();
                    pliesByLayer.Add(ply.LayerId, plies);
                }
                plies.Add(ply);
            }
            //
            List<CompositeCaePly> best = new List<CompositeCaePly>();
            double bestArea = -1;
            //
            foreach (int layer in pliesByLayer.Keys.OrderBy(x => x))
            {
                double area = pliesByLayer[layer].Sum(x => x.Surface.GetArea());
                if (area > bestArea)
                {
                    bestArea = area;
                    best = pliesByLayer[layer];
                    layerId = layer;
                }
            }
            return best;
        }
        public static FeMesh BuildMesh(IEnumerable<CompositeCaeSurface> surfaces)
        {
            BoundingBox box = new BoundingBox();
            foreach (CompositeCaeSurface surface in surfaces) box.IncludeBox(surface.Box);
            //
            double diagonal = box.GetDiagonal();
            double tolerance = diagonal > 0 ? 1E-6 * diagonal : 1E-9;
            //
            Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
            Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
            Dictionary<long[], int> nodeMap = new Dictionary<long[], int>(new CoorComparer());
            //
            foreach (CompositeCaeSurface surface in surfaces)
            {
                if (surface == null || surface.Nodes == null || surface.Elements == null) continue;
                //
                int[] globalIds = new int[surface.Nodes.Length];
                for (int i = 0; i < surface.Nodes.Length; i++)
                {
                    double[] coor = surface.Nodes[i];
                    long[] key = new long[] { (long)Math.Round(coor[0] / tolerance),
                                              (long)Math.Round(coor[1] / tolerance),
                                              (long)Math.Round(coor[2] / tolerance) };
                    int nodeId;
                    if (!nodeMap.TryGetValue(key, out nodeId))
                    {
                        nodeId = nodes.Count + 1;
                        nodes.Add(nodeId, new FeNode(nodeId, coor[0], coor[1], coor[2]));
                        nodeMap.Add(key, nodeId);
                    }
                    globalIds[i] = nodeId;
                }
                //
                foreach (int[] localIds in surface.Elements)
                {
                    int[] nodeIds = new int[localIds.Length];
                    for (int i = 0; i < localIds.Length; i++) nodeIds[i] = globalIds[localIds[i]];
                    // Skip elements that collapsed while welding the nodes
                    if (nodeIds.Distinct().Count() != nodeIds.Length) continue;
                    //
                    FeElement element = CreateElement(elements.Count + 1, nodeIds, surface.Nodes, localIds);
                    if (element != null) elements.Add(element.Id, element);
                }
            }
            //
            if (elements.Count == 0) return null;
            //
            return new FeMesh(nodes, elements, MeshRepresentation.Mesh, ImportOptions.DetectEdges);
        }
        private static FeElement CreateElement(int id, int[] nodeIds, double[][] surfaceNodes, int[] localIds)
        {
            switch (nodeIds.Length)
            {
                case 3: return new LinearTriangleElement(id, nodeIds);
                case 6: return new ParabolicTriangleElement(id, nodeIds);
                case 4:
                    if (IsPlanar(surfaceNodes, localIds)) return new LinearQuadrilateralElement(id, nodeIds);
                    return new LinearTetraElement(id, nodeIds);
                case 8:
                    if (IsPlanar(surfaceNodes, localIds)) return new ParabolicQuadrilateralElement(id, nodeIds);
                    return new LinearHexaElement(id, nodeIds);
                case 10: return new ParabolicTetraElement(id, nodeIds);
                case 15: return new ParabolicWedgeElement(id, nodeIds);
                case 20: return new ParabolicHexaElement(id, nodeIds);
                default: return null;
            }
        }
        // Distinguishes a surface element from a solid element with the same number of nodes
        private static bool IsPlanar(double[][] surfaceNodes, int[] localIds)
        {
            double[] v0 = surfaceNodes[localIds[0]];
            double[] v1 = surfaceNodes[localIds[1]];
            double[] v2 = surfaceNodes[localIds[2]];
            //
            double e1x = v1[0] - v0[0], e1y = v1[1] - v0[1], e1z = v1[2] - v0[2];
            double e2x = v2[0] - v0[0], e2y = v2[1] - v0[1], e2z = v2[2] - v0[2];
            //
            double nx = e1y * e2z - e1z * e2y;
            double ny = e1z * e2x - e1x * e2z;
            double nz = e1x * e2y - e1y * e2x;
            double length = Math.Sqrt(nx * nx + ny * ny + nz * nz);
            if (length < 1E-12) return true;
            //
            nx /= length; ny /= length; nz /= length;
            //
            double size = Math.Sqrt(Math.Max(e1x * e1x + e1y * e1y + e1z * e1z, e2x * e2x + e2y * e2y + e2z * e2z));
            double limit = 1E-4 * Math.Max(size, 1E-12);
            //
            for (int i = 3; i < localIds.Length; i++)
            {
                double[] v = surfaceNodes[localIds[i]];
                double distance = Math.Abs((v[0] - v0[0]) * nx + (v[1] - v0[1]) * ny + (v[2] - v0[2]) * nz);
                if (distance > limit) return false;
            }
            return true;
        }

        private class CoorComparer : IEqualityComparer<long[]>
        {
            public bool Equals(long[] a, long[] b)
            {
                return a[0] == b[0] && a[1] == b[1] && a[2] == b[2];
            }
            public int GetHashCode(long[] key)
            {
                return key[0].GetHashCode() ^ (key[1].GetHashCode() << 8) ^ (key[2].GetHashCode() << 16);
            }
        }
    }
}
