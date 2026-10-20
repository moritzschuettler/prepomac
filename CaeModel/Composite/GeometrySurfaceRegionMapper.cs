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
    // Ply region mapping from geometrical surface: maps a ply's Region to a CAD (BREP) surface.
    // The surface's existing display tessellation (VisualizationData) is reused directly, the
    // structural shell mesh is core sampled against it by drilling from each element's centroid
    // along its normal (same technique as CompositeCaeMapper), and the surface's outer boundary
    // is extracted as the ply's contour.
    public static class GeometrySurfaceRegionMapper
    {
        // Extracts the triangulated tessellation of one geometry face, already computed for
        // display in part.Visualization; any non-triangular cell is fan-triangulated
        public static void ExtractSurfaceTessellation(FeMesh mesh, GeometryPart part, int faceId,
                                                       out double[][] nodes, out int[][] triangles)
        {
            VisualizationData vis = part.Visualization;
            int[] cellIds = vis.CellIdsByFace[faceId];
            //
            Dictionary<int, int> globalToLocal = new Dictionary<int, int>();
            List<double[]> nodeList = new List<double[]>();
            List<int[]> triangleList = new List<int[]>();
            //
            foreach (int cellId in cellIds)
            {
                int[] cell = vis.Cells[cellId];
                int[] localIds = new int[cell.Length];
                for (int i = 0; i < cell.Length; i++)
                {
                    int globalId = cell[i];
                    int localId;
                    if (!globalToLocal.TryGetValue(globalId, out localId))
                    {
                        localId = nodeList.Count;
                        globalToLocal.Add(globalId, localId);
                        nodeList.Add(mesh.Nodes[globalId].Coor);
                    }
                    localIds[i] = localId;
                }
                // Fan triangulation from the first vertex - a no-op for already-triangular cells
                for (int i = 1; i < localIds.Length - 1; i++)
                    triangleList.Add(new int[] { localIds[0], localIds[i], localIds[i + 1] });
            }
            //
            nodes = nodeList.ToArray();
            triangles = triangleList.ToArray();
        }
        // Extracts the outer boundary loop of a geometry face as an ordered world-space polyline;
        // if the face has more than one free-edge loop (e.g. a hole), the longest-perimeter loop
        // is taken as the outer boundary
        public static double[][] ExtractOuterBoundary(FeMesh mesh, GeometryPart part, int faceId)
        {
            VisualizationData vis = part.Visualization;
            HashSet<int>[] loops = vis.GetFreeEdgeLoops(new int[] { faceId });
            if (loops == null || loops.Length == 0) return null;
            //
            List<int> bestLoop = null;
            double bestPerimeter = -1;
            //
            foreach (HashSet<int> loopEdgeIds in loops)
            {
                List<int> loopNodeIds = StitchLoop(vis, loopEdgeIds);
                double perimeter = LoopPerimeter(mesh, loopNodeIds);
                if (perimeter > bestPerimeter)
                {
                    bestPerimeter = perimeter;
                    bestLoop = loopNodeIds;
                }
            }
            //
            if (bestLoop == null || bestLoop.Count == 0) return null;
            //
            double[][] points = new double[bestLoop.Count][];
            for (int i = 0; i < bestLoop.Count; i++) points[i] = mesh.Nodes[bestLoop[i]].Coor;
            return points;
        }
        // Stitches a set of boundary edges (each an ordered node id chain) into one continuous
        // ordered loop by matching shared endpoint nodes
        private static List<int> StitchLoop(VisualizationData vis, HashSet<int> edgeIds)
        {
            List<int[]> segments = new List<int[]>();
            foreach (int edgeId in edgeIds) segments.Add(vis.GetOrderedNodeIdsForEdgeId(edgeId));
            //
            List<int> loop = new List<int>();
            if (segments.Count == 0) return loop;
            //
            bool[] used = new bool[segments.Count];
            loop.AddRange(segments[0]);
            used[0] = true;
            int remaining = segments.Count - 1;
            //
            while (remaining > 0)
            {
                int tail = loop[loop.Count - 1];
                bool found = false;
                //
                for (int i = 0; i < segments.Count; i++)
                {
                    if (used[i]) continue;
                    int[] seg = segments[i];
                    //
                    if (seg[0] == tail)
                    {
                        for (int j = 1; j < seg.Length; j++) loop.Add(seg[j]);
                        used[i] = true; remaining--; found = true; break;
                    }
                    else if (seg[seg.Length - 1] == tail)
                    {
                        for (int j = seg.Length - 2; j >= 0; j--) loop.Add(seg[j]);
                        used[i] = true; remaining--; found = true; break;
                    }
                }
                if (!found) break; // remaining segments are disconnected from the current chain
            }
            //
            return loop;
        }
        private static double LoopPerimeter(FeMesh mesh, List<int> loopNodeIds)
        {
            double perimeter = 0;
            for (int i = 0; i < loopNodeIds.Count - 1; i++)
                perimeter += Distance(mesh.Nodes[loopNodeIds[i]].Coor, mesh.Nodes[loopNodeIds[i + 1]].Coor);
            return perimeter;
        }
        // Core samples the structural shell mesh against a tessellated surface: a drill line is
        // cast from every shell element's centroid along its normal and elements whose drill
        // pierces the surface are selected
        public static int[] MapSurfaceToShellElements(FeMesh mesh, double[][] surfaceNodes, int[][] surfaceTriangles,
                                                       double drillDepth, double tolerance)
        {
            return MapSurfaceToShellElements(mesh, surfaceNodes, surfaceTriangles, drillDepth, tolerance, null);
        }
        // partNames limits the search to the given mesh parts; null or empty searches every shell
        // part. A solid mesh keeps its surface elements in a part of its own (FeMesh names parts by
        // element dimension), so the part type alone cannot tell a structural shell from the skin
        // of a solid and the caller has to be able to name the target part.
        public static int[] MapSurfaceToShellElements(FeMesh mesh, double[][] surfaceNodes, int[][] surfaceTriangles,
                                                       double drillDepth, double tolerance, string[] partNames)
        {
            if (surfaceNodes == null || surfaceNodes.Length == 0 ||
                surfaceTriangles == null || surfaceTriangles.Length == 0)
                return new int[0];
            //
            PlySurfaceSampler sampler = new PlySurfaceSampler(surfaceNodes, surfaceTriangles);
            //
            HashSet<int> shellPartIds = GetShellPartIds(mesh, partNames);
            if (shellPartIds != null && shellPartIds.Count == 0) return new int[0];
            //
            List<int> elementIds = new List<int>();
            List<double[]> origins = new List<double[]>();
            List<double[]> directions = new List<double[]>();
            double edgeLengthSum = 0;
            int edgeLengthCount = 0;
            //
            foreach (var entry in mesh.Elements)
            {
                FeElement element = entry.Value;
                if (!(element is FeElement2D)) continue;
                // A 2D element of a solid or of a mixed part is a surface element of that part and
                // not a structural shell; only the elements of a shell part may be selected
                if (shellPartIds != null && !shellPartIds.Contains(element.PartId)) continue;
                //
                double[] center;
                double[] normal;
                mesh.GetElementFaceCenterAndNormal(element, FeFaceName.S2, out center, out normal, out _);
                if (center == null) center = element.GetCG(mesh.Nodes, out _);
                if (center == null || !Normalize(ref normal)) continue;
                //
                elementIds.Add(entry.Key);
                origins.Add(center);
                directions.Add(normal);
                //
                int[] nodeIds = element.NodeIds;
                if (nodeIds.Length >= 2)
                {
                    edgeLengthSum += Distance(mesh.Nodes[nodeIds[0]].Coor, mesh.Nodes[nodeIds[1]].Coor);
                    edgeLengthCount++;
                }
            }
            //
            if (elementIds.Count == 0) return new int[0];
            //
            if (drillDepth <= 0)
            {
                double avgEdge = edgeLengthCount > 0 ? edgeLengthSum / edgeLengthCount : 0;
                if (avgEdge <= 0)
                {
                    BoundingBox box = new BoundingBox();
                    box.IncludeCoors(surfaceNodes);
                    avgEdge = box.GetDiagonal() * 0.02;
                }
                drillDepth = avgEdge * 5;
                if (drillDepth <= 0) drillDepth = 1;
            }
            if (tolerance < 0) tolerance = drillDepth * 0.05;
            //
            List<int> hits = new List<int>();
            for (int i = 0; i < elementIds.Count; i++)
            {
                double distance;
                if (sampler.Drill(origins[i], directions[i], drillDepth, tolerance, out distance))
                    hits.Add(elementIds[i]);
            }
            return hits.ToArray();
        }
        // Part ids of the shell parts to search; null when the mesh carries no part information, in
        // which case the element type alone has to decide
        private static HashSet<int> GetShellPartIds(FeMesh mesh, string[] partNames)
        {
            if (mesh.Parts == null || mesh.Parts.Count == 0) return null;
            //
            HashSet<string> allowedNames = partNames != null && partNames.Length > 0
                                           ? new HashSet<string>(partNames) : null;
            HashSet<int> partIds = new HashSet<int>();
            foreach (var entry in mesh.Parts)
            {
                if (entry.Value.PartType != PartType.Shell) continue;
                if (allowedNames != null && !allowedNames.Contains(entry.Key)) continue;
                partIds.Add(entry.Value.PartId);
            }
            return partIds;
        }
        // Mesh parts that hold structural shell elements, offered to the user as the search target
        public static string[] GetShellPartNames(FeMesh mesh)
        {
            List<string> names = new List<string>();
            if (mesh?.Parts == null) return names.ToArray();
            //
            foreach (var entry in mesh.Parts)
                if (entry.Value.PartType == PartType.Shell) names.Add(entry.Key);
            return names.ToArray();
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
        private static double Distance(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
