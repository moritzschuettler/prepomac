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
using System.Text;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh.Sweep
{
    /// <summary>
    /// The triangulation of the source surfaces of a group as gmsh meshed them, read for QuadWild: the triangles,
    /// oriented alike across the surfaces, and every edge as the chain of its nodes from the vertex at the start of
    /// its parameter to the vertex at its end. The edges are the feature lines QuadWild keeps, and the nodes of a
    /// fixed edge are the nodes it has to reproduce, so the coordinates are written to its files exactly as gmsh
    /// holds them.
    /// </summary>
    public class SweepQuadWildInput
    {
        // Variables                                                                                                                
        private List<double[]> _coors;
        private Dictionary<IntPtr, int> _tagIndex;
        private int[][] _triangles;
        private int[] _triangleSurfaceIds;
        private Dictionary<int, int[]> _edgeIdPolyline;
        private Dictionary<int, int> _edgeIdStartVertexId;
        private Dictionary<int, int> _edgeIdEndVertexId;
        private Dictionary<int, int> _vertexIdNode;
        private double _diagonal;


        // Properties                                                                                                               
        public List<double[]> Coors { get { return _coors; } }
        public int[][] Triangles { get { return _triangles; } }
        public int[] TriangleSurfaceIds { get { return _triangleSurfaceIds; } }
        /// <summary>
        /// The nodes of every edge from its start vertex to its end vertex, both included; the two are the same node
        /// on a closed edge.
        /// </summary>
        public Dictionary<int, int[]> EdgeIdPolyline { get { return _edgeIdPolyline; } }
        public Dictionary<int, int> EdgeIdStartVertexId { get { return _edgeIdStartVertexId; } }
        public Dictionary<int, int> EdgeIdEndVertexId { get { return _edgeIdEndVertexId; } }
        public Dictionary<int, int> VertexIdNode { get { return _vertexIdNode; } }
        public double Diagonal { get { return _diagonal; } }


        // Constructors                                                                                                             
        private SweepQuadWildInput()
        {
            _coors = new List<double[]>();
            _tagIndex = new Dictionary<IntPtr, int>();
            _edgeIdPolyline = new Dictionary<int, int[]>();
            _edgeIdStartVertexId = new Dictionary<int, int>();
            _edgeIdEndVertexId = new Dictionary<int, int>();
            _vertexIdNode = new Dictionary<int, int>();
        }


        // Methods                                                                                                                  
        public static SweepQuadWildInput Read(SweepQuadWildGroup group)
        {
            SweepQuadWildInput input = new SweepQuadWildInput();
            IntPtr[] tags;
            double[] coor;
            // Vertices
            foreach (var vertexId in group.VertexIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 0, vertexId, false, false);
                if (tags.Length != 1)
                    throw new CaeException("Point " + vertexId + " of a sweep source carries no mesh node.");
                input._vertexIdNode[vertexId] = input.AddNode(tags[0], coor, 0);
            }
            // Edges
            int[] vertexIds;
            double[] min;
            double[] max;
            double[] start;
            foreach (var edgeId in group.EdgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length < 1 || vertexIds.Length > 2)
                    throw new CaeException("Edge " + edgeId + " of a sweep source is not bounded by one or two points.");
                int startVertexId = vertexIds[0];
                int endVertexId = vertexIds[vertexIds.Length - 1];
                if (startVertexId != endVertexId)
                {
                    Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                    Gmsh.Model.GetValue(1, edgeId, new double[] { min[0] }, out start);
                    if (GetDistance(start, input._coors[input._vertexIdNode[endVertexId]]) <
                        GetDistance(start, input._coors[input._vertexIdNode[startVertexId]]))
                    {
                        startVertexId = vertexIds[1];
                        endVertexId = vertexIds[0];
                    }
                }
                GetCurveNodes(edgeId, out tags, out coor);
                List<int> polyline = new List<int>();
                polyline.Add(input._vertexIdNode[startVertexId]);
                for (int i = 0; i < tags.Length; i++) polyline.Add(input.AddNode(tags[i], coor, i));
                polyline.Add(input._vertexIdNode[endVertexId]);
                input._edgeIdPolyline[edgeId] = polyline.ToArray();
                input._edgeIdStartVertexId[edgeId] = startVertexId;
                input._edgeIdEndVertexId[edgeId] = endVertexId;
            }
            // Triangles
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            List<int[]> triangles = new List<int[]>();
            List<int> triangleSurfaceIds = new List<int>();
            foreach (var surfaceId in group.SurfaceIds)
            {
                Gmsh.Model.Mesh.GetNodes(out tags, out coor, 2, surfaceId, true, false);
                Dictionary<IntPtr, int> surfaceTagIndex = new Dictionary<IntPtr, int>();
                for (int i = 0; i < tags.Length; i++) surfaceTagIndex[tags[i]] = i;
                //
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
                for (int i = 0; i < elementTypes.Length; i++)
                {
                    if (elementTypes[i] != (int)GmshElementType.LinearTriangle)
                        throw new CaeException("The triangulation of Surface " + surfaceId + " given to QuadWild " +
                                               "contains elements that are not linear triangles.");
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        int[] triangle = new int[3];
                        for (int k = 0; k < 3; k++)
                        {
                            IntPtr tag = nodeTags[i][3 * j + k];
                            int index;
                            if (!input._tagIndex.TryGetValue(tag, out index))
                                index = input.AddNode(tag, coor, surfaceTagIndex[tag]);
                            triangle[k] = index;
                        }
                        triangles.Add(triangle);
                        triangleSurfaceIds.Add(surfaceId);
                    }
                }
                if (elementTypes.Length == 0)
                    throw new CaeException("Surface " + surfaceId + " was not triangulated for QuadWild.");
            }
            input._triangles = triangles.ToArray();
            input._triangleSurfaceIds = triangleSurfaceIds.ToArray();
            input.OrientTriangles();
            //
            BoundingBox box = new BoundingBox();
            foreach (var nodeCoor in input._coors) box.IncludeCoor(nodeCoor);
            input._diagonal = box.GetDiagonal();
            if (input._diagonal <= 0) throw new CaeException("The sweep source " + group.GetDescription() + " is degenerate.");
            return input;
        }
        /// <summary>
        /// The nodes inside a meshed edge, vertices excluded, in the order of the edge's parameter.
        /// </summary>
        public static void GetCurveNodes(int edgeId, out IntPtr[] tags, out double[] coor)
        {
            IntPtr[] allTags;
            double[] allCoor;
            double[] parameters;
            Gmsh.Model.Mesh.GetNodes(out allTags, out allCoor, 1, edgeId, false, false);
            parameters = new double[0];
            if (allTags.Length > 0) Gmsh.Model.GetParametrization(1, edgeId, allCoor, out parameters);
            if (parameters.Length != allTags.Length)
                throw new CaeException("The nodes of Edge " + edgeId + " could not be placed on the edge.");
            int[] order = Enumerable.Range(0, allTags.Length).OrderBy(i => parameters[i]).ToArray();
            tags = new IntPtr[order.Length];
            coor = new double[3 * order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                tags[i] = allTags[order[i]];
                for (int k = 0; k < 3; k++) coor[3 * i + k] = allCoor[3 * order[i] + k];
            }
        }
        private int AddNode(IntPtr tag, double[] coor, int index)
        {
            int nodeIndex;
            if (_tagIndex.TryGetValue(tag, out nodeIndex)) return nodeIndex;
            nodeIndex = _coors.Count;
            _tagIndex.Add(tag, nodeIndex);
            _coors.Add(new double[] { coor[3 * index], coor[3 * index + 1], coor[3 * index + 2] });
            return nodeIndex;
        }
        /// <summary>
        /// Turns the triangles so that every edge two of them share is walked in opposite directions by the two. The
        /// surfaces of one source need not share their orientation, and QuadWild reads a triangulation whose
        /// neighbours disagree as a broken one.
        /// </summary>
        private void OrientTriangles()
        {
            Dictionary<(int, int), List<int>> edgeTriangles = new Dictionary<(int, int), List<int>>();
            for (int t = 0; t < _triangles.Length; t++)
            {
                for (int k = 0; k < 3; k++)
                {
                    (int, int) key = GetEdgeKey(_triangles[t][k], _triangles[t][(k + 1) % 3]);
                    List<int> list;
                    if (!edgeTriangles.TryGetValue(key, out list))
                    {
                        list = new List<int>();
                        edgeTriangles.Add(key, list);
                    }
                    list.Add(t);
                }
            }
            if (edgeTriangles.Values.Any(list => list.Count > 2))
                throw new CaeException("The triangulation of a sweep source given to QuadWild is non-manifold.");
            //
            bool[] visited = new bool[_triangles.Length];
            Queue<int> queue = new Queue<int>();
            for (int first = 0; first < _triangles.Length; first++)
            {
                if (visited[first]) continue;
                visited[first] = true;
                queue.Enqueue(first);
                while (queue.Count > 0)
                {
                    int t = queue.Dequeue();
                    for (int k = 0; k < 3; k++)
                    {
                        int a = _triangles[t][k];
                        int b = _triangles[t][(k + 1) % 3];
                        foreach (var neighbour in edgeTriangles[GetEdgeKey(a, b)])
                        {
                            if (neighbour == t) continue;
                            bool sameDirection = HasDirectedEdge(_triangles[neighbour], a, b);
                            if (!visited[neighbour])
                            {
                                if (sameDirection)
                                {
                                    int tmp = _triangles[neighbour][1];
                                    _triangles[neighbour][1] = _triangles[neighbour][2];
                                    _triangles[neighbour][2] = tmp;
                                }
                                visited[neighbour] = true;
                                queue.Enqueue(neighbour);
                            }
                            else if (sameDirection)
                                throw new CaeException("The triangulation of a sweep source given to QuadWild cannot " +
                                                       "be oriented.");
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Writes the triangulation, its feature lines - every edge of the group, which is also every boundary of
        /// the triangulation - and the nodes QuadWild has to keep: the vertices, and all the nodes of every fixed
        /// edge as one polyline.
        /// </summary>
        public void Write(string objFileName, string sharpFileName, string nodesFileName, HashSet<int> fixedEdgeIds)
        {
            string[] coorTexts = _coors.Select(coor => ToText(coor[0]) + " " + ToText(coor[1]) + " " + ToText(coor[2]))
                                       .ToArray();
            // Triangulation
            StringBuilder obj = new StringBuilder();
            obj.AppendLine("# PrePoMax sweep source triangulation for QuadWild");
            foreach (var coorText in coorTexts) obj.Append("v ").AppendLine(coorText);
            foreach (var triangle in _triangles)
                obj.Append("f ").Append(triangle[0] + 1).Append(" ").Append(triangle[1] + 1).Append(" ")
                   .AppendLine((triangle[2] + 1).ToString(CultureInfo.InvariantCulture));
            // Feature lines
            HashSet<(int, int)> segmentKeys = new HashSet<(int, int)>();
            foreach (var polyline in _edgeIdPolyline.Values)
            {
                for (int i = 0; i + 1 < polyline.Length; i++) segmentKeys.Add(GetEdgeKey(polyline[i], polyline[i + 1]));
            }
            Dictionary<(int, int), List<int[]>> keyReferences = new Dictionary<(int, int), List<int[]>>();
            for (int t = 0; t < _triangles.Length; t++)
            {
                for (int k = 0; k < 3; k++)
                {
                    (int, int) key = GetEdgeKey(_triangles[t][k], _triangles[t][(k + 1) % 3]);
                    List<int[]> references;
                    if (!keyReferences.TryGetValue(key, out references))
                    {
                        references = new List<int[]>();
                        keyReferences.Add(key, references);
                    }
                    references.Add(new int[] { t, k });
                }
            }
            List<string> sharpLines = new List<string>();
            foreach (var entry in keyReferences)
            {
                bool isSegment = segmentKeys.Contains(entry.Key);
                if (entry.Value.Count == 1 && !isSegment)
                    throw new CaeException("The boundary of the triangulation given to QuadWild does not follow the " +
                                           "edges of the sweep source.");
                if (!isSegment) continue;
                int[] reference = entry.Value[0];
                int type = GetSharpEdgeType(reference, entry.Value);
                sharpLines.Add(type + "," + reference[0] + "," + reference[1]);
            }
            if (sharpLines.Count != segmentKeys.Count)
                throw new CaeException("An edge of the sweep source is not an edge of its triangulation.");
            StringBuilder sharp = new StringBuilder();
            sharp.AppendLine(sharpLines.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var line in sharpLines) sharp.AppendLine(line);
            // Prescribed nodes
            StringBuilder nodes = new StringBuilder();
            nodes.AppendLine("# PrePoMax fixed edges of the sweep source, one polyline per edge");
            foreach (var entry in _edgeIdPolyline)
            {
                if (!fixedEdgeIds.Contains(entry.Key)) continue;
                nodes.Append(entry.Value.Length.ToString(CultureInfo.InvariantCulture));
                foreach (var node in entry.Value) nodes.Append(" ").Append(coorTexts[node]);
                nodes.AppendLine();
            }
            nodes.AppendLine("# PrePoMax vertices of the sweep source, one prescribed node per line");
            foreach (var node in _vertexIdNode.Values) nodes.Append("1 ").AppendLine(coorTexts[node]);
            //
            UTF8Encoding utf8WithoutBom = new UTF8Encoding(false);
            File.WriteAllText(objFileName, obj.ToString(), utf8WithoutBom);
            File.WriteAllText(sharpFileName, sharp.ToString(), utf8WithoutBom);
            File.WriteAllText(nodesFileName, nodes.ToString(), utf8WithoutBom);
        }
        /// <summary>
        /// QuadWild uses 0 for a concave feature line and 1 for a convex one; a boundary is convex.
        /// </summary>
        private int GetSharpEdgeType(int[] reference, List<int[]> references)
        {
            if (references.Count == 1) return 1;
            //
            int[] opposite = references[0] == reference ? references[1] : references[0];
            double[] normal0 = GetNormal(_triangles[reference[0]]);
            double[] normal1 = GetNormal(_triangles[opposite[0]]);
            double[] start = _coors[_triangles[reference[0]][reference[1]]];
            double[] end = _coors[_triangles[reference[0]][(reference[1] + 1) % 3]];
            double crossX = normal0[1] * normal1[2] - normal0[2] * normal1[1];
            double crossY = normal0[2] * normal1[0] - normal0[0] * normal1[2];
            double crossZ = normal0[0] * normal1[1] - normal0[1] * normal1[0];
            double dot = crossX * (end[0] - start[0]) + crossY * (end[1] - start[1]) + crossZ * (end[2] - start[2]);
            return dot < 0 ? 0 : 1;
        }
        private double[] GetNormal(int[] triangle)
        {
            double[] p0 = _coors[triangle[0]];
            double[] p1 = _coors[triangle[1]];
            double[] p2 = _coors[triangle[2]];
            double x1 = p1[0] - p0[0];
            double y1 = p1[1] - p0[1];
            double z1 = p1[2] - p0[2];
            double x2 = p2[0] - p0[0];
            double y2 = p2[1] - p0[1];
            double z2 = p2[2] - p0[2];
            return new double[] { y1 * z2 - z1 * y2, z1 * x2 - x1 * z2, x1 * y2 - y1 * x2 };
        }
        private static bool HasDirectedEdge(int[] triangle, int a, int b)
        {
            for (int k = 0; k < 3; k++)
            {
                if (triangle[k] == a && triangle[(k + 1) % 3] == b) return true;
            }
            return false;
        }
        private static string ToText(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }
        public static (int, int) GetEdgeKey(int node1, int node2)
        {
            return node1 < node2 ? (node1, node2) : (node2, node1);
        }
        public static double GetDistance(double[] coor1, double[] coor2)
        {
            return Math.Sqrt(Math.Pow(coor1[0] - coor2[0], 2) + Math.Pow(coor1[1] - coor2[1], 2) +
                             Math.Pow(coor1[2] - coor2[2], 2));
        }
    }
}
