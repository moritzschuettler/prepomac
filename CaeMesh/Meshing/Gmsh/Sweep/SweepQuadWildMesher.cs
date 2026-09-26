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
using System.Globalization;
using System.IO;
using System.Linq;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// Meshes the source surfaces of a sweep group with QuadWild and carries the mesh into gmsh.
    ///
    /// QuadWild is given the triangulation gmsh made of the surfaces, every edge as a feature line and the nodes of
    /// the fixed edges as prescribed nodes, and it chooses the counts of the free edges itself. Its nodes are then
    /// found on the vertices, the edges and the surfaces they lie on, the edges by their distance to the chain of
    /// nodes they were given and the surfaces by the triangles below the quads. The curve nodes of an edge are
    /// meshed again by gmsh with the count QuadWild chose, before the walls are, and moved to where QuadWild put
    /// them; the surface mesh itself is added only after the walls and the optimisation, since a meshing pass
    /// empties a face whose mesh was added rather than meshed.
    /// </summary>
    public static class SweepQuadWildMesher
    {
        // Variables                                                                                                                
        private const double _vertexTolerance = 1E-6;
        private const double _edgeTolerance = 1E-5;
        private const double _curveTolerance = 0.02;
        private const double _moveTolerance = 1E-10;
        private const int _numOfSurfaceSamples = 15;
        private const double _minQuality = 0.15;
        private const double _sharpCornerQualityFraction = 0.9;


        // Methods                                                                                                                  
        /// <summary>
        /// Meshes the group from the triangulation its surfaces carry now, and the nodes its edges carry now.
        /// </summary>
        public static SweepQuadWildMesh Mesh(SweepQuadWildGroup group, SweepMesh sweepMesh, string runtimeDirectory,
                                             string workDirectory, Action<string> writeOutput)
        {
            string executable = Path.Combine(runtimeDirectory, QuadWildRuntime.MesherFileName);
            string patchExecutable = Path.Combine(runtimeDirectory, QuadWildRuntime.PatchMesherFileName);
            QuadWildRuntime.ValidateRuntime(executable, patchExecutable, runtimeDirectory);
            //
            string directory = Path.Combine(workDirectory, "Group_" + group.Index);
            Directory.CreateDirectory(directory);
            string objFileName = Path.Combine(directory, "input.obj");
            string sharpFileName = Path.Combine(directory, "input.sharp");
            string nodesFileName = Path.Combine(directory, "input.cnodes");
            string prepConfigFileName = Path.Combine(directory, "prep.txt");
            string flowConfigFileName = Path.Combine(directory, "flow.txt");
            string preparedObjFileName = Path.Combine(directory, "input_rem.obj");
            string tracePrefix = Path.Combine(directory, "input_rem_p0");
            string tracedObjFileName = tracePrefix + ".obj";
            string[] tracedFileNames = new string[]
            {
                tracedObjFileName, tracePrefix + ".patch", tracePrefix + ".corners", tracePrefix + ".feature",
                tracePrefix + ".c_feature"
            };
            string smoothObjFileName = tracePrefix + "_1_quadrangulation_smooth.obj";
            string unsmoothedObjFileName = tracePrefix + "_1_quadrangulation.obj";
            //
            SweepQuadWildInput input = SweepQuadWildInput.Read(group);
            input.Write(objFileName, sharpFileName, nodesFileName, group.FixedEdgeIds);
            //
            QuadWildRuntime.WritePrepConfig(runtimeDirectory, prepConfigFileName, false);
            string argument = QuadWildRuntime.Quote(objFileName) + " 2 " + QuadWildRuntime.Quote(prepConfigFileName) +
                              " " + QuadWildRuntime.Quote(sharpFileName);
            QuadWildRuntime.DeleteIfExists(preparedObjFileName);
            QuadWildRuntime.RunStage("preparation and patch tracing of " + group.GetDescription(), executable,
                                     argument, directory, tracedFileNames);
            QuadWildRuntime.EnsureNoPatchesErased(preparedObjFileName, tracedObjFileName, null);
            // The count of a free edge is QuadWild's to choose, not bounded below by the triangulation. The nodes are
            // not moved onto smooth curves through the input nodes: the nodes of the edges are placed on the edges and
            // the nodes inside on the surfaces when the mesh is added, and the move, which takes every node close to an
            // input line for a node of it, pinned the nodes of a folded quad to a boundary and kept it folded. The edge
            // rotations of a later QuadWild are off as well: over the sweep test models they lowered the minimum
            // quality of the swept elements on ten of them and raised it on none
            QuadWildRuntime.WriteFlowConfig(runtimeDirectory, flowConfigFileName, sweepMesh.QuadWildFlowProfile,
                                            sweepMesh.QuadWildAlpha,
                                            new string[] { "input_line_count 0", "smooth_curves 0", "rotate_edges 0" });
            argument = QuadWildRuntime.Quote(tracedObjFileName) + " 1 " + QuadWildRuntime.Quote(flowConfigFileName);
            QuadWildRuntime.RunStage("quad generation of " + group.GetDescription(), patchExecutable, argument,
                                     directory, unsmoothedObjFileName, smoothObjFileName);
            //
            List<double[]> nodeCoors;
            List<int[]> faces;
            ReadObj(sweepMesh.QuadWildSmooth ? smoothObjFileName : unsmoothedObjFileName, out nodeCoors, out faces);
            SweepQuadWildMesh mesh = MapOutput(group, input, nodeCoors, faces);
            //
            int numOfQuads = faces.Count(face => face.Length == 4);
            double minQuality = GetMinQuality(faces, nodeCoors);
            if (!IsAboveQualityLimit(faces, nodeCoors, mesh, input))
                throw new CaeException("The QuadWild mesh of " + group.GetDescription() + " has a minimum quality of " +
                                       Math.Round(minQuality, 3) + ".");
            writeOutput?.Invoke("Sweep: QuadWild meshed " + group.GetDescription() + " with " + numOfQuads +
                                " quadrilaterals" + (numOfQuads < faces.Count ? " and " + (faces.Count - numOfQuads) +
                                " triangles" : "") + ", minimum quality " + Math.Round(minQuality, 3) +
                                ". " + DescribeFreeEdges(group, mesh));
            return mesh;
        }
        /// <summary>
        /// The lowest quality of the elements, which the messages report.
        /// </summary>
        private static double GetMinQuality(List<int[]> faces, List<double[]> nodeCoors)
        {
            double min = 1;
            foreach (var face in faces) min = Math.Min(min, GetQuality(face, nodeCoors));
            return min;
        }
        /// <summary>
        /// Whether every element reaches the quality limit. A corner of a quadrilateral at a vertex where its surface has
        /// a sharp corner - an edge meeting the boundary at a small angle - is held only to a fraction of the sine of
        /// that angle: no quadrilateral filling the corner can do better, so the fixed limit would refuse every mesh of
        /// it. The angle of the surface is the sum of the corners its triangles have there in the triangulation QuadWild
        /// is given. Read from the elements instead, a collapsed element - a quadrilateral with a straight corner at a
        /// vertex on a straight edge - made the corner look as sharp as nothing, and passed. A corner shared by two
        /// elements, each with part of an already sharp angle, is still refused.
        /// </summary>
        private static bool IsAboveQualityLimit(List<int[]> faces, List<double[]> nodeCoors, SweepQuadWildMesh mesh,
                                                SweepQuadWildInput input)
        {
            double[] sines;
            double[] angles;
            List<double[]> faceSines = new List<double[]>();
            for (int f = 0; f < faces.Count; f++)
            {
                if (!GetCorners(faces[f], nodeCoors, out sines, out angles)) return false;
                faceSines.Add(sines);
            }
            Dictionary<int, int> nodeVertexId = input.VertexIdNode.ToDictionary(entry => entry.Value, entry => entry.Key);
            Dictionary<(int, int), double> vertexSurfaceAngle = new Dictionary<(int, int), double>();
            for (int i = 0; i < input.Triangles.Length; i++)
            {
                int[] triangle = input.Triangles[i];
                for (int k = 0; k < 3; k++)
                {
                    int vertexId;
                    if (!nodeVertexId.TryGetValue(triangle[k], out vertexId)) continue;
                    Vec3D corner = new Vec3D(input.Coors[triangle[k]]);
                    Vec3D next = new Vec3D(input.Coors[triangle[(k + 1) % 3]]) - corner;
                    Vec3D previous = new Vec3D(input.Coors[triangle[(k + 2) % 3]]) - corner;
                    double lengths = next.Len * previous.Len;
                    if (lengths == 0) continue;
                    (int, int) key = (vertexId, input.TriangleSurfaceIds[i]);
                    double angle;
                    vertexSurfaceAngle.TryGetValue(key, out angle);
                    vertexSurfaceAngle[key] = angle + Math.Acos(Math.Max(-1, Math.Min(1, Vec3D.DotProduct(next, previous) /
                                                                                             lengths)));
                }
            }
            for (int f = 0; f < faces.Count; f++)
            {
                if (faces[f].Length != 4)
                {
                    if (GetQuality(faces[f], nodeCoors) < _minQuality) return false;
                    continue;
                }
                for (int k = 0; k < 4; k++)
                {
                    double limit = _minQuality;
                    SweepQuadWildNode node = mesh.Elements[f][k];
                    if (node.Dimension == 0)
                    {
                        double angle;
                        if (vertexSurfaceAngle.TryGetValue((node.Tag, mesh.ElementSurfaceIds[f]), out angle) &&
                            angle < Math.PI / 2)
                            limit = Math.Min(limit, _sharpCornerQualityFraction * Math.Sin(angle));
                    }
                    if (faceSines[f][k] < limit) return false;
                }
            }
            return true;
        }
        /// <summary>
        /// The quality of an element, measured against its normal: the scaled Jacobian at the worst corner of a
        /// quadrilateral, and the sine of the largest angle of an obtuse triangle. A triangle is judged by its largest
        /// angle alone, since it is an angle close to 180 degrees that spoils it. A corner turned against the normal is
        /// its negative sine in both.
        /// </summary>
        private static double GetQuality(int[] face, List<double[]> nodeCoors)
        {
            double[] sines;
            double[] angles;
            if (!GetCorners(face, nodeCoors, out sines, out angles)) return -1;
            //
            double min = 1;
            double maxAngle = 0;
            for (int k = 0; k < face.Length; k++)
            {
                if (face.Length == 4 || sines[k] <= 0) min = Math.Min(min, sines[k]);
                else maxAngle = Math.Max(maxAngle, angles[k]);
            }
            if (maxAngle > Math.PI / 2) min = Math.Min(min, Math.Sin(maxAngle));
            return min;
        }
        /// <summary>
        /// The sine and the angle of every corner of an element, signed against its normal; false for an element with
        /// no area or with two nodes in one place.
        /// </summary>
        private static bool GetCorners(int[] face, List<double[]> nodeCoors, out double[] sines, out double[] angles)
        {
            int n = face.Length;
            sines = new double[n];
            angles = new double[n];
            Vec3D normal = GetAreaVector(face, nodeCoors);
            if (normal.Normalize() == 0) return false;
            for (int k = 0; k < n; k++)
            {
                Vec3D corner = new Vec3D(nodeCoors[face[k]]);
                Vec3D next = new Vec3D(nodeCoors[face[(k + 1) % n]]) - corner;
                Vec3D previous = new Vec3D(nodeCoors[face[(k + n - 1) % n]]) - corner;
                double lengths = next.Len * previous.Len;
                if (lengths == 0) return false;
                sines[k] = Vec3D.DotProduct(Vec3D.CrossProduct(next, previous), normal) / lengths;
                angles[k] = Math.Atan2(sines[k], Vec3D.DotProduct(next, previous) / lengths);
            }
            return true;
        }
        private static Vec3D GetAreaVector(int[] face, List<double[]> nodeCoors)
        {
            Vec3D areaVector = new Vec3D();
            for (int k = 0; k < face.Length; k++)
                areaVector = areaVector + Vec3D.CrossProduct(new Vec3D(nodeCoors[face[k]]),
                                                             new Vec3D(nodeCoors[face[(k + 1) % face.Length]]));
            return areaVector;
        }
        private static string DescribeFreeEdges(SweepQuadWildGroup group, SweepQuadWildMesh mesh)
        {
            int[] freeEdgeIds = group.FreeEdgeIds.ToArray();
            if (freeEdgeIds.Length == 0) return "Every edge was fixed.";
            return "Free edges: " + String.Join(", ", freeEdgeIds.Select(id => "Edge " + id + " (" +
                                                                         mesh.GetNumOfElements(id) + ")")) + ".";
        }
        private static void ReadObj(string fileName, out List<double[]> nodeCoors, out List<int[]> faces)
        {
            nodeCoors = new List<double[]>();
            faces = new List<int[]>();
            char[] separator = new char[] { ' ', '\t' };
            foreach (var line in File.ReadLines(fileName))
            {
                if (line.StartsWith("v ", StringComparison.Ordinal))
                {
                    string[] tmp = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    nodeCoors.Add(new double[] { double.Parse(tmp[1], NumberStyles.Float, CultureInfo.InvariantCulture),
                                                 double.Parse(tmp[2], NumberStyles.Float, CultureInfo.InvariantCulture),
                                                 double.Parse(tmp[3], NumberStyles.Float, CultureInfo.InvariantCulture) });
                }
                else if (line.StartsWith("f ", StringComparison.Ordinal))
                {
                    string[] tmp = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length != 4 && tmp.Length != 5)
                        throw new CaeException("The QuadWild output contains a face that is neither a triangle nor a " +
                                               "quadrilateral.");
                    int[] face = new int[tmp.Length - 1];
                    for (int i = 1; i < tmp.Length; i++)
                        face[i - 1] = int.Parse(tmp[i].Split('/')[0], CultureInfo.InvariantCulture) - 1;
                    if (face.Distinct().Count() != face.Length)
                        throw new CaeException("The QuadWild output contains a face with repeated nodes.");
                    faces.Add(face);
                }
            }
            if (faces.Count == 0) throw new CaeException("The QuadWild output contains no elements.");
            foreach (var coor in nodeCoors)
            {
                if (coor.Any(value => double.IsNaN(value) || double.IsInfinity(value)))
                    throw new CaeException("The QuadWild output contains a non-finite node coordinate.");
            }
        }
        /// <summary>
        /// Finds every node of the output on the vertex, the edge or the surface it lies on, checks that the nodes
        /// of every edge follow one another along it as edges of the output, and assigns the elements to the
        /// surfaces by the triangles below them.
        /// </summary>
        private static SweepQuadWildMesh MapOutput(SweepQuadWildGroup group, SweepQuadWildInput input,
                                                   List<double[]> nodeCoors, List<int[]> faces)
        {
            int numOfNodes = nodeCoors.Count;
            double vertexTolerance = _vertexTolerance * input.Diagonal;
            double edgeTolerance = _edgeTolerance * input.Diagonal;
            // The edges of the output and how many elements use each
            Dictionary<(int, int), List<int>> edgeFaces = new Dictionary<(int, int), List<int>>();
            for (int f = 0; f < faces.Count; f++)
            {
                for (int k = 0; k < faces[f].Length; k++)
                {
                    (int, int) key = SweepQuadWildInput.GetEdgeKey(faces[f][k], faces[f][(k + 1) % faces[f].Length]);
                    List<int> list;
                    if (!edgeFaces.TryGetValue(key, out list))
                    {
                        list = new List<int>();
                        edgeFaces.Add(key, list);
                    }
                    list.Add(f);
                }
            }
            if (edgeFaces.Values.Any(list => list.Count > 2))
                throw new CaeException("The QuadWild mesh of " + group.GetDescription() + " is non-manifold.");
            bool[] isOnBoundary = new bool[numOfNodes];
            foreach (var entry in edgeFaces)
            {
                if (entry.Value.Count != 1) continue;
                isOnBoundary[entry.Key.Item1] = true;
                isOnBoundary[entry.Key.Item2] = true;
            }
            // Vertices: QuadWild keeps the prescribed nodes where they were
            int[] nodeVertexId = new int[numOfNodes];
            Dictionary<int, int> vertexIdOutputNode = new Dictionary<int, int>();
            foreach (var entry in input.VertexIdNode)
            {
                double[] vertexCoor = input.Coors[entry.Value];
                int nearest = -1;
                double nearestDistance = double.MaxValue;
                for (int i = 0; i < numOfNodes; i++)
                {
                    double distance = SweepQuadWildInput.GetDistance(vertexCoor, nodeCoors[i]);
                    if (distance < nearestDistance)
                    {
                        nearest = i;
                        nearestDistance = distance;
                    }
                }
                if (nearest < 0 || nearestDistance > vertexTolerance)
                    throw new CaeException("QuadWild placed no node at Point " + entry.Key + " of " +
                                           group.GetDescription() + ".");
                nodeVertexId[nearest] = entry.Key;
                vertexIdOutputNode[entry.Key] = nearest;
            }
            // Edges: a node on an edge lies on the chain of nodes the edge was given, or on the edge itself, to a small
            // fraction of the segment length. A node on the boundary of the output lies on an edge bounding the group,
            // the nearest one whatever the distance, and a node inside the output only on an edge between two of the
            // group's surfaces: a node inside can come close to the boundary where QuadWild folded its mesh
            HashSet<int> groupSurfaceIds = new HashSet<int>(group.SurfaceIds);
            HashSet<int> innerEdgeIds = new HashSet<int>();
            int[] adjacentSurfaceIds;
            foreach (var edgeId in group.EdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out adjacentSurfaceIds, out _);
                if (adjacentSurfaceIds.Count(groupSurfaceIds.Contains) > 1) innerEdgeIds.Add(edgeId);
            }
            int[] nodeEdgeId = new int[numOfNodes];
            double[] nodeEdgeDistance = Enumerable.Repeat(double.MaxValue, numOfNodes).ToArray();
            double[] nodeSegmentLength = new double[numOfNodes];
            foreach (var entry in input.EdgeIdPolyline)
            {
                int[] polyline = entry.Value;
                double maxSegmentLength = 0;
                BoundingBox box = new BoundingBox();
                for (int j = 0; j < polyline.Length; j++)
                {
                    box.IncludeCoor(input.Coors[polyline[j]]);
                    if (j > 0)
                        maxSegmentLength = Math.Max(maxSegmentLength,
                                                    SweepQuadWildInput.GetDistance(input.Coors[polyline[j - 1]],
                                                                                   input.Coors[polyline[j]]));
                }
                box.Inflate(maxSegmentLength + edgeTolerance);
                bool isInnerEdge = innerEdgeIds.Contains(entry.Key);
                bool isFixedEdge = group.FixedEdgeIds.Contains(entry.Key);
                List<int> candidates = new List<int>();
                for (int i = 0; i < numOfNodes; i++)
                {
                    if (nodeVertexId[i] == 0 && isOnBoundary[i] != isInnerEdge &&
                        box.MinOutsideDistance2(nodeCoors[i]) == 0) candidates.Add(i);
                }
                if (candidates.Count == 0) continue;
                //
                double[] closestCoor;
                Gmsh.Model.GetClosestPoint(1, entry.Key, candidates.SelectMany(i => nodeCoors[i]).ToArray(),
                                           out closestCoor, out _);
                if (closestCoor.Length != 3 * candidates.Count)
                    throw new CaeException("The nodes QuadWild placed near Edge " + entry.Key + " could not be projected " +
                                           "onto it.");
                for (int j = 0; j < candidates.Count; j++)
                {
                    int i = candidates[j];
                    double[] closest = new double[] { closestCoor[3 * j], closestCoor[3 * j + 1], closestCoor[3 * j + 2] };
                    double polylineDistance = GetDistanceToPolyline(nodeCoors[i], polyline, input.Coors);
                    double distance = polylineDistance;
                    // The projection of an unbounded curve can land past its ends, which the chain does not
                    if (polylineDistance <= maxSegmentLength)
                        distance = Math.Min(distance, SweepQuadWildInput.GetDistance(nodeCoors[i], closest));
                    // The nodes of a fixed edge are the ones it was given, and a node beside one of them is a node of a
                    // quad QuadWild squeezed against the edge
                    if (isFixedEdge)
                    {
                        distance = double.MaxValue;
                        for (int k = 1; k + 1 < polyline.Length; k++)
                            distance = Math.Min(distance, SweepQuadWildInput.GetDistance(nodeCoors[i], input.Coors[polyline[k]]));
                        if (distance > vertexTolerance) continue;
                    }
                    if (distance >= nodeEdgeDistance[i]) continue;
                    nodeEdgeDistance[i] = distance;
                    nodeEdgeId[i] = entry.Key;
                    nodeSegmentLength[i] = GetNearestSegmentLength(nodeCoors[i], polyline, input.Coors);
                }
            }
            for (int i = 0; i < numOfNodes; i++)
            {
                if (nodeEdgeId[i] == 0 || isOnBoundary[i]) continue;
                if (nodeEdgeDistance[i] > Math.Max(edgeTolerance, _curveTolerance * nodeSegmentLength[i]))
                    nodeEdgeId[i] = 0;
            }
            for (int i = 0; i < numOfNodes; i++)
            {
                if (isOnBoundary[i] && nodeVertexId[i] == 0 && nodeEdgeId[i] == 0)
                    throw new CaeException("A node QuadWild placed on the boundary of " + group.GetDescription() +
                                           " lies on none of its edges.");
            }
            // The nodes of every edge in the order of its parameter, between its two vertices
            Dictionary<int, int[]> edgeIdOutputNodes = new Dictionary<int, int[]>();
            HashSet<(int, int)> segmentKeys = new HashSet<(int, int)>();
            int[] nodeIndexOnEdge = new int[numOfNodes];
            Dictionary<int, double[][]> edgeIdNodeCoors = new Dictionary<int, double[][]>();
            foreach (var edgeId in group.EdgeIds)
            {
                List<int> edgeNodes = new List<int>();
                for (int i = 0; i < numOfNodes; i++) if (nodeEdgeId[i] == edgeId) edgeNodes.Add(i);
                double[] parameters = new double[0];
                if (edgeNodes.Count > 0)
                {
                    double[] coor = new double[3 * edgeNodes.Count];
                    for (int j = 0; j < edgeNodes.Count; j++)
                        for (int k = 0; k < 3; k++) coor[3 * j + k] = nodeCoors[edgeNodes[j]][k];
                    parameters = GetCurveParameters(edgeId, coor, out _);
                }
                int[] ordered = Enumerable.Range(0, edgeNodes.Count).OrderBy(j => parameters[j])
                                          .Select(j => edgeNodes[j]).ToArray();
                List<int> chain = new List<int>();
                chain.Add(vertexIdOutputNode[input.EdgeIdStartVertexId[edgeId]]);
                chain.AddRange(ordered);
                chain.Add(vertexIdOutputNode[input.EdgeIdEndVertexId[edgeId]]);
                for (int j = 0; j + 1 < chain.Count; j++)
                {
                    (int, int) key = SweepQuadWildInput.GetEdgeKey(chain[j], chain[j + 1]);
                    if (!edgeFaces.ContainsKey(key))
                        throw new CaeException("The nodes QuadWild placed on Edge " + edgeId + " of " +
                                               group.GetDescription() + " do not follow one another along it.");
                    segmentKeys.Add(key);
                }
                for (int j = 0; j < ordered.Length; j++) nodeIndexOnEdge[ordered[j]] = j;
                edgeIdOutputNodes[edgeId] = ordered;
                // A fixed edge keeps its nodes exactly; QuadWild stores them in single precision
                int[] polyline = input.EdgeIdPolyline[edgeId];
                if (group.FixedEdgeIds.Contains(edgeId))
                {
                    if (ordered.Length != polyline.Length - 2)
                        throw new CaeException("QuadWild did not keep the " + (polyline.Length - 1) + " elements of " +
                                               "fixed Edge " + edgeId + " of " + group.GetDescription() + ".");
                    double[][] fixedCoors = new double[ordered.Length][];
                    for (int j = 0; j < ordered.Length; j++)
                    {
                        fixedCoors[j] = input.Coors[polyline[j + 1]];
                        if (SweepQuadWildInput.GetDistance(fixedCoors[j], nodeCoors[ordered[j]]) > vertexTolerance)
                            throw new CaeException("QuadWild moved a node of fixed Edge " + edgeId + " of " +
                                                   group.GetDescription() + ".");
                    }
                    edgeIdNodeCoors[edgeId] = fixedCoors;
                }
                else edgeIdNodeCoors[edgeId] = ordered.Select(node => nodeCoors[node]).ToArray();
            }
            foreach (var entry in edgeFaces)
            {
                if (entry.Value.Count == 1 && !segmentKeys.Contains(entry.Key))
                    throw new CaeException("The boundary of the QuadWild mesh of " + group.GetDescription() +
                                           " does not follow its edges.");
            }
            // The elements grouped into the regions the edges divide them into, and each region onto the surface
            // whose triangles lie below it
            int[] faceRegion = GetRegions(faces, edgeFaces, segmentKeys);
            int numOfRegions = faceRegion.Length == 0 ? 0 : faceRegion.Max() + 1;
            int[] regionSurfaceId = new int[numOfRegions];
            for (int r = 0; r < numOfRegions; r++)
            {
                int[] regionFaces = Enumerable.Range(0, faces.Count).Where(f => faceRegion[f] == r).ToArray();
                int step = Math.Max(1, regionFaces.Length / _numOfSurfaceSamples);
                Dictionary<int, int> surfaceIdVotes = new Dictionary<int, int>();
                for (int j = 0; j < regionFaces.Length; j += step)
                {
                    int surfaceId = GetSurfaceIdBelow(GetCentroid(faces[regionFaces[j]], nodeCoors), input);
                    int votes;
                    surfaceIdVotes.TryGetValue(surfaceId, out votes);
                    surfaceIdVotes[surfaceId] = votes + 1;
                }
                regionSurfaceId[r] = surfaceIdVotes.OrderByDescending(entry => entry.Value).First().Key;
            }
            foreach (var surfaceId in group.SurfaceIds)
            {
                if (!regionSurfaceId.Contains(surfaceId))
                    throw new CaeException("The QuadWild mesh of " + group.GetDescription() + " has no elements on " +
                                           "Surface " + surfaceId + ".");
            }
            // The nodes inside the surfaces
            int[] nodeInteriorIndex = Enumerable.Repeat(-1, numOfNodes).ToArray();
            List<double[]> interiorNodeCoors = new List<double[]>();
            List<int> interiorNodeSurfaceIds = new List<int>();
            SweepQuadWildNode[][] elements = new SweepQuadWildNode[faces.Count][];
            int[] elementSurfaceIds = new int[faces.Count];
            for (int f = 0; f < faces.Count; f++)
            {
                int surfaceId = regionSurfaceId[faceRegion[f]];
                elementSurfaceIds[f] = surfaceId;
                elements[f] = new SweepQuadWildNode[faces[f].Length];
                for (int k = 0; k < faces[f].Length; k++)
                {
                    int node = faces[f][k];
                    if (nodeVertexId[node] != 0)
                        elements[f][k] = new SweepQuadWildNode(0, nodeVertexId[node], 0);
                    else if (nodeEdgeId[node] != 0)
                        elements[f][k] = new SweepQuadWildNode(1, nodeEdgeId[node], nodeIndexOnEdge[node]);
                    else
                    {
                        if (nodeInteriorIndex[node] < 0)
                        {
                            nodeInteriorIndex[node] = interiorNodeCoors.Count;
                            interiorNodeCoors.Add(nodeCoors[node]);
                            interiorNodeSurfaceIds.Add(surfaceId);
                        }
                        else if (interiorNodeSurfaceIds[nodeInteriorIndex[node]] != surfaceId)
                            throw new CaeException("A node QuadWild placed inside " + group.GetDescription() +
                                                   " belongs to elements of two surfaces.");
                        elements[f][k] = new SweepQuadWildNode(2, surfaceId, nodeInteriorIndex[node]);
                    }
                }
            }
            return new SweepQuadWildMesh(edgeIdNodeCoors, interiorNodeCoors.ToArray(), interiorNodeSurfaceIds.ToArray(),
                                         elements, elementSurfaceIds);
        }
        private static int[] GetRegions(List<int[]> faces, Dictionary<(int, int), List<int>> edgeFaces,
                                        HashSet<(int, int)> segmentKeys)
        {
            int[] faceRegion = Enumerable.Repeat(-1, faces.Count).ToArray();
            int numOfRegions = 0;
            Queue<int> queue = new Queue<int>();
            for (int first = 0; first < faces.Count; first++)
            {
                if (faceRegion[first] >= 0) continue;
                faceRegion[first] = numOfRegions;
                queue.Enqueue(first);
                while (queue.Count > 0)
                {
                    int f = queue.Dequeue();
                    for (int k = 0; k < faces[f].Length; k++)
                    {
                        (int, int) key = SweepQuadWildInput.GetEdgeKey(faces[f][k], faces[f][(k + 1) % faces[f].Length]);
                        if (segmentKeys.Contains(key)) continue;
                        foreach (var neighbour in edgeFaces[key])
                        {
                            if (faceRegion[neighbour] >= 0) continue;
                            faceRegion[neighbour] = numOfRegions;
                            queue.Enqueue(neighbour);
                        }
                    }
                }
                numOfRegions++;
            }
            return faceRegion;
        }
        private static double[] GetCentroid(int[] face, List<double[]> nodeCoors)
        {
            double[] centroid = new double[3];
            foreach (var node in face)
                for (int k = 0; k < 3; k++) centroid[k] += nodeCoors[node][k] / face.Length;
            return centroid;
        }
        private static int GetSurfaceIdBelow(double[] point, SweepQuadWildInput input)
        {
            int nearest = -1;
            double nearestDistance = double.MaxValue;
            for (int t = 0; t < input.Triangles.Length; t++)
            {
                int[] triangle = input.Triangles[t];
                double distance = GetDistanceToTriangle(point, input.Coors[triangle[0]], input.Coors[triangle[1]],
                                                        input.Coors[triangle[2]]);
                if (distance < nearestDistance)
                {
                    nearest = t;
                    nearestDistance = distance;
                }
            }
            return input.TriangleSurfaceIds[nearest];
        }
        private static double GetDistanceToPolyline(double[] point, int[] polyline, List<double[]> coors)
        {
            double min = double.MaxValue;
            for (int i = 0; i + 1 < polyline.Length; i++)
                min = Math.Min(min, GetDistanceToSegment(point, coors[polyline[i]], coors[polyline[i + 1]]));
            return min;
        }
        private static double GetNearestSegmentLength(double[] point, int[] polyline, List<double[]> coors)
        {
            double min = double.MaxValue;
            double length = 0;
            for (int i = 0; i + 1 < polyline.Length; i++)
            {
                double distance = GetDistanceToSegment(point, coors[polyline[i]], coors[polyline[i + 1]]);
                if (distance < min)
                {
                    min = distance;
                    length = SweepQuadWildInput.GetDistance(coors[polyline[i]], coors[polyline[i + 1]]);
                }
            }
            return length;
        }
        private static double GetDistanceToSegment(double[] p, double[] a, double[] b)
        {
            double[] ab = new double[] { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double length2 = ab[0] * ab[0] + ab[1] * ab[1] + ab[2] * ab[2];
            double t = 0;
            if (length2 > 0)
                t = Math.Max(0, Math.Min(1, ((p[0] - a[0]) * ab[0] + (p[1] - a[1]) * ab[1] + (p[2] - a[2]) * ab[2]) /
                                            length2));
            return SweepQuadWildInput.GetDistance(p, new double[] { a[0] + t * ab[0], a[1] + t * ab[1], a[2] + t * ab[2] });
        }
        private static double GetDistanceToTriangle(double[] p, double[] a, double[] b, double[] c)
        {
            Vec3D pa = new Vec3D(p[0] - a[0], p[1] - a[1], p[2] - a[2]);
            Vec3D ab = new Vec3D(b[0] - a[0], b[1] - a[1], b[2] - a[2]);
            Vec3D ac = new Vec3D(c[0] - a[0], c[1] - a[1], c[2] - a[2]);
            Vec3D normal = Vec3D.CrossProduct(ab, ac);
            double length = normal.Len;
            if (length > 0)
            {
                normal = normal * (1 / length);
                double height = Vec3D.DotProduct(pa, normal);
                Vec3D projected = pa - normal * height;
                // Barycentric coordinates of the projection
                double d00 = Vec3D.DotProduct(ab, ab);
                double d01 = Vec3D.DotProduct(ab, ac);
                double d11 = Vec3D.DotProduct(ac, ac);
                double d20 = Vec3D.DotProduct(projected, ab);
                double d21 = Vec3D.DotProduct(projected, ac);
                double denominator = d00 * d11 - d01 * d01;
                if (denominator > 0)
                {
                    double v = (d11 * d20 - d01 * d21) / denominator;
                    double w = (d00 * d21 - d01 * d20) / denominator;
                    if (v >= 0 && w >= 0 && v + w <= 1) return Math.Abs(height);
                }
            }
            return Math.Min(GetDistanceToSegment(p, a, b), Math.Min(GetDistanceToSegment(p, b, c),
                                                                    GetDistanceToSegment(p, c, a)));
        }
        /// <summary>
        /// Moves the curve nodes of the group's edges to where QuadWild put them. False, with the edges named, when
        /// an edge no longer carries the count of the mesh, which the walls may have changed since the run; nothing
        /// is moved then.
        /// </summary>
        public static bool ApplyEdgeNodes(SweepQuadWildGroup group, out List<int> changedEdgeIds)
        {
            SweepQuadWildMesh mesh = group.Mesh;
            changedEdgeIds = new List<int>();
            Dictionary<int, IntPtr[]> edgeIdTags = new Dictionary<int, IntPtr[]>();
            Dictionary<int, double[]> edgeIdCoor = new Dictionary<int, double[]>();
            IntPtr[] tags;
            double[] coor;
            foreach (var edgeId in group.EdgeIds)
            {
                SweepQuadWildInput.GetCurveNodes(edgeId, out tags, out coor);
                if (tags.Length != mesh.EdgeIdNodeCoors[edgeId].Length) changedEdgeIds.Add(edgeId);
                edgeIdTags[edgeId] = tags;
                edgeIdCoor[edgeId] = coor;
            }
            if (changedEdgeIds.Count > 0) return false;
            //
            double tolerance = _moveTolerance * GetDiagonal(group);
            double[] closestCoor;
            double[] parameters;
            foreach (var edgeId in group.EdgeIds)
            {
                double[][] targetCoors = mesh.EdgeIdNodeCoors[edgeId];
                if (targetCoors.Length == 0) continue;
                parameters = GetCurveParameters(edgeId, targetCoors.SelectMany(c => c).ToArray(), out closestCoor);
                tags = edgeIdTags[edgeId];
                coor = edgeIdCoor[edgeId];
                for (int i = 0; i < tags.Length; i++)
                {
                    double[] point = new double[] { closestCoor[3 * i], closestCoor[3 * i + 1], closestCoor[3 * i + 2] };
                    double[] current = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
                    if (SweepQuadWildInput.GetDistance(point, current) > tolerance)
                        Gmsh.Model.Mesh.SetNode(tags[i], point, new double[] { parameters[i] });
                }
            }
            return true;
        }
        /// <summary>
        /// Replaces whatever gmsh meshed on the group's surfaces with the QuadWild mesh, onto the nodes the vertices
        /// and the edges carry. The nodes inside the surfaces are placed on the geometry, and the elements of each
        /// surface are turned the way gmsh turned its own on it.
        /// </summary>
        public static void AddSurfaceMesh(SweepQuadWildGroup group)
        {
            SweepQuadWildMesh mesh = group.Mesh;
            IntPtr[] tags;
            double[] coor;
            Dictionary<int, IntPtr> vertexIdTag = new Dictionary<int, IntPtr>();
            foreach (var vertexId in group.VertexIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 0, vertexId, false, false);
                vertexIdTag[vertexId] = tags[0];
            }
            Dictionary<int, IntPtr[]> edgeIdTags = new Dictionary<int, IntPtr[]>();
            foreach (var edgeId in group.EdgeIds)
            {
                SweepQuadWildInput.GetCurveNodes(edgeId, out tags, out coor);
                if (tags.Length != mesh.EdgeIdNodeCoors[edgeId].Length)
                    throw new CaeException("Edge " + edgeId + " no longer carries the nodes QuadWild meshed " +
                                           group.GetDescription() + " with.");
                edgeIdTags[edgeId] = tags;
            }
            //
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            IntPtr[] interiorTags = new IntPtr[mesh.InteriorNodeCoors.Length];
            double[][] interiorCoors = new double[mesh.InteriorNodeCoors.Length][];
            foreach (var surfaceId in group.SurfaceIds)
            {
                int gmshOrientation = GetOrientation(surfaceId);
                Gmsh.Model.Mesh.Clear(new Tuple<int, int>[] { new Tuple<int, int>(2, surfaceId) });
                // Nodes
                int[] nodeIndices = Enumerable.Range(0, mesh.InteriorNodeCoors.Length)
                                              .Where(i => mesh.InteriorNodeSurfaceIds[i] == surfaceId).ToArray();
                if (nodeIndices.Length > 0)
                {
                    double[] closestCoor;
                    double[] parametricCoor;
                    Gmsh.Model.GetClosestPoint(2, surfaceId,
                                               nodeIndices.SelectMany(i => mesh.InteriorNodeCoors[i]).ToArray(),
                                               out closestCoor, out _);
                    Gmsh.Model.GetParametrization(2, surfaceId, closestCoor, out parametricCoor);
                    if (closestCoor.Length != 3 * nodeIndices.Length || parametricCoor.Length != 2 * nodeIndices.Length)
                        throw new CaeException("The nodes QuadWild placed inside Surface " + surfaceId +
                                               " could not be placed on the surface.");
                    IntPtr[] newTags = new IntPtr[nodeIndices.Length];
                    for (int j = 0; j < nodeIndices.Length; j++)
                    {
                        newTags[j] = new IntPtr(++nodeTag);
                        interiorTags[nodeIndices[j]] = newTags[j];
                        interiorCoors[nodeIndices[j]] = new double[] { closestCoor[3 * j], closestCoor[3 * j + 1],
                                                                       closestCoor[3 * j + 2] };
                    }
                    Gmsh.Model.Mesh.AddNodes(2, surfaceId, newTags, closestCoor, parametricCoor);
                }
                // Elements
                List<IntPtr[]> surfaceElements = new List<IntPtr[]>();
                for (int e = 0; e < mesh.Elements.Length; e++)
                {
                    if (mesh.ElementSurfaceIds[e] != surfaceId) continue;
                    surfaceElements.Add(mesh.Elements[e].Select(node =>
                        node.Dimension == 0 ? vertexIdTag[node.Tag] :
                        node.Dimension == 1 ? edgeIdTags[node.Tag][node.Index] : interiorTags[node.Index]).ToArray());
                }
                if (GetOrientation(surfaceId, surfaceElements) != gmshOrientation)
                {
                    foreach (var element in surfaceElements) Array.Reverse(element);
                }
                List<int> elementTypes = new List<int>();
                List<IntPtr[]> elementTags = new List<IntPtr[]>();
                List<IntPtr[]> elementNodeTags = new List<IntPtr[]>();
                foreach (var numOfNodes in new int[] { 3, 4 })
                {
                    IntPtr[][] typeElements = surfaceElements.Where(element => element.Length == numOfNodes).ToArray();
                    if (typeElements.Length == 0) continue;
                    elementTypes.Add(numOfNodes == 3 ? (int)GmshElementType.LinearTriangle :
                                                       (int)GmshElementType.LinearQuadrilateral);
                    IntPtr[] typeTags = new IntPtr[typeElements.Length];
                    for (int j = 0; j < typeTags.Length; j++) typeTags[j] = new IntPtr(++elementTag);
                    elementTags.Add(typeTags);
                    elementNodeTags.Add(typeElements.SelectMany(element => element).ToArray());
                }
                Gmsh.Model.Mesh.AddElements(2, surfaceId, elementTypes.ToArray(), elementTags.ToArray(),
                                            elementNodeTags.ToArray());
            }
        }
        /// <summary>
        /// Whether the elements on a surface mostly turn with the normal of the surface (1) or against it (-1): the
        /// elements gmsh meshed on it when none are given.
        /// </summary>
        private static int GetOrientation(int surfaceId, List<IntPtr[]> elements = null)
        {
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
            Dictionary<IntPtr, double[]> tagCoor = new Dictionary<IntPtr, double[]>();
            for (int i = 0; i < tags.Length; i++)
                tagCoor[tags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            if (elements == null)
            {
                elements = new List<IntPtr[]>();
                int[] elementTypes;
                IntPtr[][] elementTags;
                IntPtr[][] nodeTags;
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    int numOfNodes = elementTypes[i] == (int)GmshElementType.LinearTriangle ? 3 :
                                     elementTypes[i] == (int)GmshElementType.LinearQuadrilateral ? 4 : 0;
                    if (numOfNodes == 0) continue;
                    for (int j = 0; j < elementTags[i].Length; j++)
                        elements.Add(nodeTags[i].Skip(j * numOfNodes).Take(numOfNodes).ToArray());
                }
            }
            if (elements.Count == 0) return 1;
            //
            int step = Math.Max(1, elements.Count / _numOfSurfaceSamples);
            List<double[]> centroids = new List<double[]>();
            List<Vec3D> elementNormals = new List<Vec3D>();
            for (int e = 0; e < elements.Count; e += step)
            {
                double[][] points = elements[e].Select(tag => tagCoor[tag]).ToArray();
                double[] centroid = new double[3];
                foreach (var point in points) for (int k = 0; k < 3; k++) centroid[k] += point[k] / points.Length;
                Vec3D normal = new Vec3D();
                for (int k = 0; k < points.Length; k++)
                {
                    Vec3D a = new Vec3D(points[k]) - new Vec3D(centroid);
                    Vec3D b = new Vec3D(points[(k + 1) % points.Length]) - new Vec3D(centroid);
                    normal = normal + Vec3D.CrossProduct(a, b);
                }
                centroids.Add(centroid);
                elementNormals.Add(normal);
            }
            double[] closestCoor;
            double[] parametricCoor;
            double[] surfaceNormals;
            Gmsh.Model.GetClosestPoint(2, surfaceId, centroids.SelectMany(c => c).ToArray(), out closestCoor, out _);
            Gmsh.Model.GetParametrization(2, surfaceId, closestCoor, out parametricCoor);
            Gmsh.Model.GetNormal(surfaceId, parametricCoor, out surfaceNormals);
            int sum = 0;
            for (int i = 0; i < elementNormals.Count; i++)
            {
                Vec3D surfaceNormal = new Vec3D(surfaceNormals[3 * i], surfaceNormals[3 * i + 1], surfaceNormals[3 * i + 2]);
                sum += Vec3D.DotProduct(elementNormals[i], surfaceNormal) >= 0 ? 1 : -1;
            }
            return sum >= 0 ? 1 : -1;
        }
        /// <summary>
        /// The points projected onto the curve and their parameters there. The parameters are those of the projected
        /// points: the ones the projection itself returns for a curve were measured to be meaningless.
        /// </summary>
        private static double[] GetCurveParameters(int edgeId, double[] coor, out double[] closestCoor)
        {
            double[] parameters;
            Gmsh.Model.GetClosestPoint(1, edgeId, coor, out closestCoor, out _);
            Gmsh.Model.GetParametrization(1, edgeId, closestCoor, out parameters);
            if (closestCoor.Length != coor.Length || parameters.Length != coor.Length / 3)
                throw new CaeException("The nodes QuadWild placed on Edge " + edgeId + " could not be placed on the edge.");
            return parameters;
        }
        private static double GetDiagonal(SweepQuadWildGroup group)
        {
            double xMin, yMin, zMin, xMax, yMax, zMax;
            BoundingBox box = new BoundingBox();
            foreach (var surfaceId in group.SurfaceIds)
            {
                Gmsh.Model.GetBoundingBox(2, surfaceId, out xMin, out yMin, out zMin, out xMax, out yMax, out zMax);
                box.IncludeCoor(new double[] { xMin, yMin, zMin });
                box.IncludeCoor(new double[] { xMax, yMax, zMax });
            }
            return box.GetDiagonal();
        }
    }
}
