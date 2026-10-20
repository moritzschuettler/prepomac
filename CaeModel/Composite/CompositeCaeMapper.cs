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
using System.Threading.Tasks;
using CaeMesh;

namespace CaeModel
{
    // Maps the plies of a Composite CAE file onto the elements of the structural mesh by core sampling:
    // a drill line is cast from each element centroid along the element normal and every ply surface it
    // pierces contributes that element to the ply region.
    public static class CompositeCaeMapper
    {
        public static Dictionary<int, List<int>> MapPliesToElements(FeMesh mesh, int[] targetElementIds,
                                                                    CompositeCaeData data, double drillDepth,
                                                                    double tolerance, Action<string> log)
        {
            Dictionary<int, List<int>> plyElementIds = new Dictionary<int, List<int>>();
            if (mesh == null || targetElementIds == null || targetElementIds.Length == 0 || data.Plies.Count == 0)
                return plyElementIds;
            //
            if (drillDepth <= 0) drillDepth = data.GetAutoDrillDepth();
            if (tolerance < 0) tolerance = 0;
            //
            double[] stackDirection = data.GetStackDirection();
            //
            List<int> elementIds = new List<int>();
            List<double[]> origins = new List<double[]>();
            List<double[]> directions = new List<double[]>();
            GetDrillLines(mesh, targetElementIds, stackDirection, elementIds, origins, directions);
            //
            if (log != null)
                log("Core sampling " + elementIds.Count + " elements through " + data.Plies.Count +
                    " plies, drill depth " + drillDepth.ToString("G6") + ".");
            //
            if (elementIds.Count == 0) return plyElementIds;
            //
            BoundingBox targetBox = new BoundingBox();
            foreach (double[] origin in origins) targetBox.IncludeCoor(origin);
            targetBox.Inflate(drillDepth + tolerance);
            //
            List<int>[] hits = new List<int>[data.Plies.Count];
            //
            Parallel.For(0, data.Plies.Count, plyIndex =>
            {
                CompositeCaeSurface surface = data.Plies[plyIndex].Surface;
                if (surface == null || surface.TriangleCount == 0) return;
                if (!targetBox.Intersects(surface.Box)) return;
                //
                PlySurfaceSampler sampler = new PlySurfaceSampler(surface.Nodes, surface.Triangles);
                List<int> plyHits = new List<int>();
                //
                for (int i = 0; i < elementIds.Count; i++)
                {
                    double distance;
                    if (sampler.Drill(origins[i], directions[i], drillDepth, tolerance, out distance))
                        plyHits.Add(elementIds[i]);
                }
                //
                hits[plyIndex] = plyHits;
            });
            //
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] != null && hits[i].Count > 0) plyElementIds.Add(i, hits[i]);
            }
            //
            return plyElementIds;
        }
        // The element centroids and the drill directions of the target elements
        private static void GetDrillLines(FeMesh mesh, int[] targetElementIds, double[] stackDirection,
                                          List<int> elementIds, List<double[]> origins, List<double[]> directions)
        {
            foreach (int elementId in targetElementIds)
            {
                FeElement element;
                if (!mesh.Elements.TryGetValue(elementId, out element)) continue;
                //
                double[] center;
                double[] normal = null;
                //
                if (element is FeElement2D)
                {
                    mesh.GetElementFaceCenterAndNormal(element, FeFaceName.S2, out center, out normal, out _);
                    if (center == null) center = element.GetCG(mesh.Nodes, out _);
                }
                else
                {
                    center = element.GetCG(mesh.Nodes, out _);
                }
                //
                if (center == null) continue;
                if (!Normalize(ref normal)) normal = stackDirection;
                //
                elementIds.Add(elementId);
                origins.Add(center);
                directions.Add(normal);
            }
        }
        private static bool Normalize(ref double[] vector)
        {
            if (vector == null) return false;
            //
            double length = Math.Sqrt(vector[0] * vector[0] + vector[1] * vector[1] + vector[2] * vector[2]);
            if (length < 1E-12) return false;
            //
            vector[0] /= length;
            vector[1] /= length;
            vector[2] /= length;
            return true;
        }
        // The element ids of a part or an element set; an empty region name selects all shell elements
        public static int[] GetTargetElementIds(FeMesh mesh, string regionName)
        {
            List<int> elementIds = new List<int>();
            //
            if (string.IsNullOrEmpty(regionName))
            {
                foreach (var entry in mesh.Elements)
                {
                    if (entry.Value is FeElement2D) elementIds.Add(entry.Key);
                }
                // Fall back to the solid elements of the model
                if (elementIds.Count == 0) elementIds.AddRange(mesh.Elements.Keys);
                //
                return elementIds.ToArray();
            }
            //
            BasePart part;
            if (mesh.Parts.TryGetValue(regionName, out part)) return part.Labels.ToArray();
            //
            FeElementSet elementSet;
            if (mesh.ElementSets.TryGetValue(regionName, out elementSet))
            {
                if (elementSet.CreatedFromParts)
                {
                    foreach (int partId in elementSet.Labels)
                    {
                        BasePart setPart = mesh.GetPartFromId(partId);
                        if (setPart != null) elementIds.AddRange(setPart.Labels);
                    }
                    return elementIds.ToArray();
                }
                return elementSet.Labels.ToArray();
            }
            //
            return new int[0];
        }
    }
}
