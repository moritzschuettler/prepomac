// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using FileInOut.Output;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PrePoMax
{
    // Debug visualization of a QuadWild mesh, added to the generated mesh before it enters the model:
    // - one part per QuadWild patch (the part of the geometry keeps the first patch);
    // - node sets of the irregular interior nodes (valence 3, 5, 6 and more);
    // - the cross field of the prepared triangle mesh and the feature lines QuadWild used, as wire parts;
    // - node sets of the prescribed nodes, the patch corners and the seam nodes of a shell compound, and one element
    //   set per sheet of a shell compound.
    // All names start with the part name, followed by "_QW_" or "_patch_".
    internal static class QuadWildDebugVisualization
    {
        public const string SetPrefix = "_QW_";
        public const string PatchPartPrefix = "_patch_";


        // Methods
        public static void Add(FeMesh mesh, string partName, string tracePrefix, bool triangularPreview,
                               QuadWildInputFileInfo inputInfo, Dictionary<int, string> elementIdMaterialName,
                               Action<string> writeOutput)
        {
            if (!mesh.Parts.TryGetValue(partName, out BasePart part)) return;
            List<string> created = new List<string>();
            int[] elementIds = part.Labels.ToArray();
            double edgeLength = GetMeanEdgeLength(mesh, elementIds);
            // Node and element sets are computed on the whole part, before it is split into patches
            if (!triangularPreview) AddValenceNodeSets(mesh, part, elementIds, created);
            AddPositionNodeSets(mesh, part, tracePrefix, inputInfo, edgeLength, created);
            if (inputInfo != null && inputInfo.SeamEdgeCount > 0 && !triangularPreview)
                AddSheetElementSets(mesh, part, elementIds, tracePrefix, edgeLength, created);
            // Wire parts
            string baseName = tracePrefix.EndsWith("_p0") ? tracePrefix.Substring(0, tracePrefix.Length - 3) : tracePrefix;
            AddCrossFieldWirePart(mesh, partName, baseName + ".obj", baseName + ".rosy", created);
            AddFeatureWirePart(mesh, partName, tracePrefix + ".obj", tracePrefix + ".feature", created);
            // Patches as parts
            int patchCount = AddPatchParts(mesh, partName, elementIds, tracePrefix, triangularPreview,
                                           elementIdMaterialName, created);
            //
            mesh.UpdateMaxNodeAndElementIds();
            writeOutput?.Invoke("QuadWild debug visualization: " + patchCount + " patch/es" +
                                (created.Count > 0 ? "; " + String.Join(", ", created) : "") + ".");
        }
        public static bool IsDebugItemName(string name, string partName)
        {
            return name.StartsWith(partName + SetPrefix, StringComparison.Ordinal) ||
                   name.StartsWith(partName + PatchPartPrefix, StringComparison.Ordinal);
        }
        // Patches
        private static int AddPatchParts(FeMesh mesh, string partName, int[] elementIds, string tracePrefix,
                                         bool triangularPreview, Dictionary<int, string> elementIdMaterialName,
                                         List<string> created)
        {
            // Patch of every element: from the patch file of the traced triangles, or from the OBJ materials of the quads
            Dictionary<int, string> elementIdPatch = new Dictionary<int, string>();
            if (triangularPreview)
            {
                string patchFileName = tracePrefix + ".patch";
                if (!File.Exists(patchFileName)) return 0;
                string[] values = File.ReadAllText(patchFileName).Split(new char[] { ' ', '\r', '\n', '\t' },
                                                                         StringSplitOptions.RemoveEmptyEntries);
                for (int i = 1; i < values.Length; i++)
                    elementIdPatch[i] = values[i].PadLeft(6, '0');     // element i is face i of the OBJ file
            }
            else if (elementIdMaterialName != null)
            {
                foreach (var entry in elementIdMaterialName) elementIdPatch[entry.Key] = entry.Value;
            }
            Dictionary<string, List<int>> patchElementIds = new Dictionary<string, List<int>>();
            foreach (int elementId in elementIds)
            {
                if (!elementIdPatch.TryGetValue(elementId, out string patch)) continue;
                if (!patchElementIds.TryGetValue(patch, out List<int> ids))
                {
                    ids = new List<int>();
                    patchElementIds.Add(patch, ids);
                }
                ids.Add(elementId);
            }
            if (patchElementIds.Count < 2) return patchElementIds.Count;
            // The first patch stays in the part of the geometry, every other one becomes a part
            string[] patches = patchElementIds.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
            int digits = Math.Max(3, patches.Length.ToString(CultureInfo.InvariantCulture).Length);
            List<string> setNames = new List<string>();
            for (int i = 1; i < patches.Length; i++)
            {
                string name = partName + PatchPartPrefix + (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
                mesh.ElementSets.Add(name, new FeElementSet(name, patchElementIds[patches[i]].ToArray()));
                setNames.Add(name);
            }
            mesh.CreatePartsFromElementSets(setNames.ToArray(), out _, out BasePart[] newParts);
            foreach (BasePart newPart in newParts) RenamePart(mesh, newPart, newPart.Name.Substring("From_".Length));
            return patches.Length;
        }
        // Irregular interior nodes
        private static void AddValenceNodeSets(FeMesh mesh, BasePart part, int[] elementIds, List<string> created)
        {
            Dictionary<int, int> valence = new Dictionary<int, int>();
            Dictionary<(int, int), int> edgeUseCount = new Dictionary<(int, int), int>();
            foreach (int elementId in elementIds)
            {
                int[] corners = GetCornerNodeIds(mesh.Elements[elementId]);
                if (corners == null) continue;
                for (int i = 0; i < corners.Length; i++)
                {
                    valence.TryGetValue(corners[i], out int count);
                    valence[corners[i]] = count + 1;
                    (int, int) key = GetEdgeKey(corners[i], corners[(i + 1) % corners.Length]);
                    edgeUseCount.TryGetValue(key, out int useCount);
                    edgeUseCount[key] = useCount + 1;
                }
            }
            // Nodes on the boundary, on seams and on model edges are not interior
            HashSet<int> special = new HashSet<int>();
            foreach (var entry in edgeUseCount)
            {
                if (entry.Value == 2) continue;
                special.Add(entry.Key.Item1);
                special.Add(entry.Key.Item2);
            }
            if (part.Visualization.EdgeCells != null)
            {
                foreach (int[] edgeCell in part.Visualization.EdgeCells)
                {
                    special.Add(edgeCell[0]);
                    special.Add(edgeCell[1]);
                }
            }
            Dictionary<string, List<int>> sets = new Dictionary<string, List<int>>();
            foreach (var entry in valence)
            {
                if (entry.Value == 4 || special.Contains(entry.Key)) continue;
                string name = part.Name + SetPrefix + "valence_" + (entry.Value >= 6 ? "6_or_more" : entry.Value.ToString());
                if (!sets.TryGetValue(name, out List<int> ids))
                {
                    ids = new List<int>();
                    sets.Add(name, ids);
                }
                ids.Add(entry.Key);
            }
            foreach (var entry in sets.OrderBy(item => item.Key, StringComparer.Ordinal))
                AddNodeSet(mesh, entry.Key, entry.Value, created);
        }
        // Prescribed nodes, patch corners and seam nodes by position
        private static void AddPositionNodeSets(FeMesh mesh, BasePart part, string tracePrefix,
                                                QuadWildInputFileInfo inputInfo, double edgeLength, List<string> created)
        {
            NodeLocator locator = new NodeLocator(mesh, part.NodeLabels, edgeLength);
            double exact = 1E-6 * Math.Max(part.BoundingBox.GetDiagonal(), 1E-30);
            // Prescribed nodes (model vertices, seams, constrained curves)
            string nodesFileName = tracePrefix.Substring(0, tracePrefix.Length - "_rem_p0".Length) + ".cnodes";
            List<double[]> prescribed = new List<double[]>();
            if (tracePrefix.EndsWith("_rem_p0") && File.Exists(nodesFileName))
            {
                foreach (string line in File.ReadAllLines(nodesFileName))
                {
                    string[] values = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (values.Length < 4 || values[0].StartsWith("#")) continue;
                    if (!int.TryParse(values[0], out int count)) continue;
                    for (int i = 0; i < count && 3 * i + 3 < values.Length; i++)
                        prescribed.Add(new double[] { ParseDouble(values[3 * i + 1]), ParseDouble(values[3 * i + 2]),
                                                      ParseDouble(values[3 * i + 3]) });
                }
            }
            AddNodeSet(mesh, part.Name + SetPrefix + "prescribed_nodes", locator.Find(prescribed, exact), created);
            // Patch corners of the traced patches; interior corners move while smoothing, so the nearest node is taken
            List<double[]> vertices = ReadObjVertices(tracePrefix + ".obj");
            string cornersFileName = tracePrefix + ".corners";
            List<double[]> corners = new List<double[]>();
            if (vertices != null && File.Exists(cornersFileName))
            {
                int[] values = File.ReadAllText(cornersFileName)
                                   .Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
                HashSet<int> vertexIds = new HashSet<int>();
                int index = 1;
                for (int patch = 0; patch < values[0] && index < values.Length; patch++)
                {
                    int count = values[index++];
                    for (int i = 0; i < count && index < values.Length; i++) vertexIds.Add(values[index++]);
                }
                foreach (int vertexId in vertexIds)
                    if (vertexId >= 0 && vertexId < vertices.Count) corners.Add(vertices[vertexId]);
            }
            AddNodeSet(mesh, part.Name + SetPrefix + "patch_corners", locator.Find(corners, 0.5 * edgeLength), created);
            // Seam nodes of a shell compound
            if (inputInfo != null && inputInfo.SplitNodeCoordinates != null && inputInfo.SplitNodeCoordinates.Length > 0)
                AddNodeSet(mesh, part.Name + SetPrefix + "seam_nodes",
                           locator.Find(inputInfo.SplitNodeCoordinates.ToList(), exact), created);
        }
        // Sheets of a shell compound: the connected components of the split QuadWild input (the tracer merges coincident
        // vertices, so its output can join sheets at single points). Every element goes to the sheet of the input
        // triangle nearest to its centroid.
        private static void AddSheetElementSets(FeMesh mesh, BasePart part, int[] elementIds, string tracePrefix,
                                                double edgeLength, List<string> created)
        {
            if (!tracePrefix.EndsWith("_rem_p0")) return;
            string inputFileName = tracePrefix.Substring(0, tracePrefix.Length - "_rem_p0".Length) + ".obj";
            List<double[]> vertices = ReadObjVertices(inputFileName);
            List<int[]> faces = ReadObjFaces(inputFileName);
            if (vertices == null || faces == null || faces.Count == 0) return;
            // Components of the input triangles
            int[] parent = new int[faces.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            Func<int, int> find = null;
            find = x => parent[x] == x ? x : (parent[x] = find(parent[x]));
            int[] vertexFace = Enumerable.Repeat(-1, vertices.Count).ToArray();
            for (int i = 0; i < faces.Count; i++)
            {
                foreach (int vertex in faces[i])
                {
                    if (vertexFace[vertex] < 0) vertexFace[vertex] = i;
                    else parent[find(i)] = find(vertexFace[vertex]);
                }
            }
            Dictionary<int, int> rootSheet = new Dictionary<int, int>();
            int[] faceSheet = new int[faces.Count];
            for (int i = 0; i < faces.Count; i++)
            {
                int root = find(i);
                if (!rootSheet.TryGetValue(root, out int sheet)) rootSheet.Add(root, sheet = rootSheet.Count);
                faceSheet[i] = sheet;
            }
            if (rootSheet.Count < 2) return;
            // Nearest input triangle to the centroid of every element
            TriangleLocator locator = new TriangleLocator(vertices, faces, 2 * edgeLength);
            Dictionary<int, List<int>> sheetElementIds = new Dictionary<int, List<int>>();
            foreach (int elementId in elementIds)
            {
                int[] corners = GetCornerNodeIds(mesh.Elements[elementId]);
                if (corners == null) continue;
                double[] centroid = new double[3];
                foreach (int nodeId in corners)
                    for (int k = 0; k < 3; k++) centroid[k] += mesh.Nodes[nodeId].Coor[k] / corners.Length;
                int face = locator.Nearest(centroid);
                if (face < 0) continue;
                if (!sheetElementIds.TryGetValue(faceSheet[face], out List<int> ids))
                {
                    ids = new List<int>();
                    sheetElementIds.Add(faceSheet[face], ids);
                }
                ids.Add(elementId);
            }
            var sheets = sheetElementIds.Values.OrderByDescending(ids => ids.Count).ToArray();
            int digits = Math.Max(3, sheets.Length.ToString(CultureInfo.InvariantCulture).Length);
            for (int i = 0; i < sheets.Length; i++)
            {
                string name = part.Name + SetPrefix + "sheet_" + (i + 1).ToString(CultureInfo.InvariantCulture).PadLeft(digits, '0');
                mesh.ElementSets.Add(name, new FeElementSet(name, sheets[i].ToArray()));
                created.Add(name);
            }
        }
        // Wire parts
        private static void AddCrossFieldWirePart(FeMesh mesh, string partName, string objFileName, string rosyFileName,
                                                  List<string> created)
        {
            List<double[]> vertices = ReadObjVertices(objFileName);
            List<int[]> faces = ReadObjFaces(objFileName);
            if (vertices == null || faces == null || !File.Exists(rosyFileName)) return;
            string[] values = File.ReadAllText(rosyFileName).Split(new char[] { ' ', '\r', '\n', '\t' },
                                                                    StringSplitOptions.RemoveEmptyEntries);
            if (values.Length < 2 + 3 * faces.Count) return;
            List<double[][]> segments = new List<double[][]>();
            for (int i = 0; i < faces.Count; i++)
            {
                int[] face = faces[i];
                if (face.Length < 3) continue;
                Vec3D a = new Vec3D(vertices[face[0]]);
                Vec3D b = new Vec3D(vertices[face[1]]);
                Vec3D c = new Vec3D(vertices[face[2]]);
                Vec3D center = (a + b + c) * (1.0 / 3.0);
                Vec3D normal = Vec3D.CrossProduct(b - a, c - a);
                if (normal.Len2 <= 0) continue;
                normal.Normalize();
                Vec3D direction = new Vec3D(ParseDouble(values[2 + 3 * i]), ParseDouble(values[3 + 3 * i]),
                                            ParseDouble(values[4 + 3 * i]));
                direction = direction - normal * Vec3D.DotProduct(direction, normal);
                if (direction.Len2 <= 0 || Double.IsNaN(direction.Len2)) continue;
                direction.Normalize();
                Vec3D other = Vec3D.CrossProduct(normal, direction);
                double half = 0.2 * ((b - a).Len + (c - b).Len + (a - c).Len) / 3.0;
                segments.Add(new double[][] { (center - direction * half).Coor, (center + direction * half).Coor });
                segments.Add(new double[][] { (center - other * half).Coor, (center + other * half).Coor });
            }
            AddWirePart(mesh, partName + SetPrefix + "cross_field", segments, created);
        }
        private static void AddFeatureWirePart(FeMesh mesh, string partName, string objFileName, string featureFileName,
                                               List<string> created)
        {
            List<double[]> vertices = ReadObjVertices(objFileName);
            List<int[]> faces = ReadObjFaces(objFileName);
            if (vertices == null || faces == null || !File.Exists(featureFileName)) return;
            HashSet<(int, int)> keys = new HashSet<(int, int)>();
            List<double[][]> segments = new List<double[][]>();
            foreach (string line in File.ReadAllLines(featureFileName).Skip(1))
            {
                string[] values = line.Split(',');
                if (values.Length < 2 || !int.TryParse(values[0], out int faceIndex) ||
                    !int.TryParse(values[1], out int localEdge)) continue;
                if (faceIndex < 0 || faceIndex >= faces.Count) continue;
                int[] face = faces[faceIndex];
                int v0 = face[localEdge % face.Length];
                int v1 = face[(localEdge + 1) % face.Length];
                if (!keys.Add(GetEdgeKey(v0, v1))) continue;
                segments.Add(new double[][] { vertices[v0], vertices[v1] });
            }
            AddWirePart(mesh, partName + SetPrefix + "features", segments, created);
        }
        private static void AddWirePart(FeMesh mesh, string name, List<double[][]> segments, List<string> created)
        {
            if (segments.Count == 0) return;
            int nodeId = mesh.Nodes.Count > 0 ? mesh.Nodes.Keys.Max() : 0;
            int elementId = mesh.Elements.Count > 0 ? mesh.Elements.Keys.Max() : 0;
            List<int> elementIds = new List<int>(segments.Count);
            foreach (double[][] segment in segments)
            {
                int n0 = ++nodeId;
                mesh.Nodes.Add(n0, new FeNode(n0, segment[0][0], segment[0][1], segment[0][2]));
                int n1 = ++nodeId;
                mesh.Nodes.Add(n1, new FeNode(n1, segment[1][0], segment[1][1], segment[1][2]));
                LinearBeamElement element = new LinearBeamElement(++elementId, new int[] { n0, n1 });
                element.PartId = -1;
                mesh.Elements.Add(element.Id, element);
                elementIds.Add(element.Id);
            }
            mesh.ElementSets.Add(name, new FeElementSet(name, elementIds.ToArray()));
            mesh.CreatePartsFromElementSets(new string[] { name }, out _, out BasePart[] newParts);
            foreach (BasePart newPart in newParts) RenamePart(mesh, newPart, name);
            created.Add(name);
        }
        // Tools
        private static void AddNodeSet(FeMesh mesh, string name, IEnumerable<int> nodeIds, List<string> created)
        {
            int[] ids = nodeIds.Distinct().OrderBy(id => id).ToArray();
            if (ids.Length == 0) return;
            mesh.NodeSets.Add(name, new FeNodeSet(name, ids));
            created.Add(name + " (" + ids.Length + ")");
        }
        private static void RenamePart(FeMesh mesh, BasePart part, string newName)
        {
            if (part.Name == newName) return;
            mesh.Parts.Remove(part.Name);
            part.Name = newName;
            mesh.Parts.Add(newName, part);
        }
        private static int[] GetCornerNodeIds(FeElement element)
        {
            if (element is LinearTriangleElement || element is ParabolicTriangleElement) return element.NodeIds.Take(3).ToArray();
            if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
                return element.NodeIds.Take(4).ToArray();
            return null;
        }
        private static double GetMeanEdgeLength(FeMesh mesh, int[] elementIds)
        {
            double sum = 0;
            int count = 0;
            foreach (int elementId in elementIds)
            {
                int[] corners = GetCornerNodeIds(mesh.Elements[elementId]);
                if (corners == null) continue;
                for (int i = 0; i < corners.Length; i++)
                {
                    sum += (new Vec3D(mesh.Nodes[corners[i]].Coor) -
                            new Vec3D(mesh.Nodes[corners[(i + 1) % corners.Length]].Coor)).Len;
                    count++;
                }
            }
            return count > 0 ? sum / count : 1;
        }
        private static List<double[]> ReadObjVertices(string fileName)
        {
            if (!File.Exists(fileName)) return null;
            List<double[]> vertices = new List<double[]>();
            foreach (string line in File.ReadLines(fileName))
            {
                if (!line.StartsWith("v ")) continue;
                string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                vertices.Add(new double[] { ParseDouble(values[1]), ParseDouble(values[2]), ParseDouble(values[3]) });
            }
            return vertices;
        }
        private static List<int[]> ReadObjFaces(string fileName)
        {
            if (!File.Exists(fileName)) return null;
            List<int[]> faces = new List<int[]>();
            foreach (string line in File.ReadLines(fileName))
            {
                if (!line.StartsWith("f ")) continue;
                string[] values = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                faces.Add(values.Skip(1).Select(value => int.Parse(value.Split('/')[0], CultureInfo.InvariantCulture) - 1)
                                .ToArray());
            }
            return faces;
        }
        private static double ParseDouble(string value)
        {
            return double.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        private static (int, int) GetEdgeKey(int a, int b)
        {
            return a < b ? (a, b) : (b, a);
        }


        // Nearest triangle to a point, by the distance to the triangle itself
        private class TriangleLocator
        {
            private readonly List<double[]> _vertices;
            private readonly List<int[]> _faces;
            private readonly double _cell;
            private readonly Dictionary<(long, long, long), List<int>> _grid = new Dictionary<(long, long, long), List<int>>();

            public TriangleLocator(List<double[]> vertices, List<int[]> faces, double cell)
            {
                _vertices = vertices;
                _faces = faces;
                _cell = Math.Max(cell, 1E-30);
                for (int i = 0; i < faces.Count; i++)
                {
                    double[] min = { double.MaxValue, double.MaxValue, double.MaxValue };
                    double[] max = { -double.MaxValue, -double.MaxValue, -double.MaxValue };
                    foreach (int vertex in faces[i])
                    {
                        for (int k = 0; k < 3; k++)
                        {
                            min[k] = Math.Min(min[k], vertices[vertex][k]);
                            max[k] = Math.Max(max[k], vertices[vertex][k]);
                        }
                    }
                    var a = Key(min);
                    var b = Key(max);
                    for (long x = a.Item1; x <= b.Item1; x++)
                        for (long y = a.Item2; y <= b.Item2; y++)
                            for (long z = a.Item3; z <= b.Item3; z++)
                            {
                                if (!_grid.TryGetValue((x, y, z), out List<int> ids))
                                {
                                    ids = new List<int>();
                                    _grid.Add((x, y, z), ids);
                                }
                                ids.Add(i);
                            }
                }
            }
            public int Nearest(double[] point)
            {
                var key = Key(point);
                int best = -1;
                double bestDistance = double.MaxValue;
                for (int range = 1; range <= 4 && best < 0; range++)
                {
                    for (long dx = -range; dx <= range; dx++)
                        for (long dy = -range; dy <= range; dy++)
                            for (long dz = -range; dz <= range; dz++)
                            {
                                if (!_grid.TryGetValue((key.Item1 + dx, key.Item2 + dy, key.Item3 + dz), out List<int> ids))
                                    continue;
                                foreach (int face in ids)
                                {
                                    double distance = DistanceToTriangle(point, face);
                                    if (distance < bestDistance)
                                    {
                                        bestDistance = distance;
                                        best = face;
                                    }
                                }
                            }
                }
                return best;
            }
            private double DistanceToTriangle(double[] p, int face)
            {
                Vec3D a = new Vec3D(_vertices[_faces[face][0]]);
                Vec3D b = new Vec3D(_vertices[_faces[face][1]]);
                Vec3D c = new Vec3D(_vertices[_faces[face][2]]);
                Vec3D point = new Vec3D(p);
                // Closest point on a triangle (Ericson, Real-Time Collision Detection 5.1.5)
                Vec3D ab = b - a, ac = c - a, ap = point - a;
                double d1 = Vec3D.DotProduct(ab, ap), d2 = Vec3D.DotProduct(ac, ap);
                if (d1 <= 0 && d2 <= 0) return (point - a).Len;
                Vec3D bp = point - b;
                double d3 = Vec3D.DotProduct(ab, bp), d4 = Vec3D.DotProduct(ac, bp);
                if (d3 >= 0 && d4 <= d3) return (point - b).Len;
                double vc = d1 * d4 - d3 * d2;
                if (vc <= 0 && d1 >= 0 && d3 <= 0) return (point - (a + ab * (d1 / (d1 - d3)))).Len;
                Vec3D cp = point - c;
                double d5 = Vec3D.DotProduct(ab, cp), d6 = Vec3D.DotProduct(ac, cp);
                if (d6 >= 0 && d5 <= d6) return (point - c).Len;
                double vb = d5 * d2 - d1 * d6;
                if (vb <= 0 && d2 >= 0 && d6 <= 0) return (point - (a + ac * (d2 / (d2 - d6)))).Len;
                double va = d3 * d6 - d5 * d4;
                if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
                    return (point - (b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6))))).Len;
                double denominator = 1 / (va + vb + vc);
                return (point - (a + ab * (vb * denominator) + ac * (vc * denominator))).Len;
            }
            private (long, long, long) Key(double[] coor)
            {
                return ((long)Math.Floor(coor[0] / _cell), (long)Math.Floor(coor[1] / _cell), (long)Math.Floor(coor[2] / _cell));
            }
        }


        // Nearest node of a part to a position, within a distance
        private class NodeLocator
        {
            private readonly FeMesh _mesh;
            private readonly double _cell;
            private readonly Dictionary<(long, long, long), List<int>> _grid = new Dictionary<(long, long, long), List<int>>();

            public NodeLocator(FeMesh mesh, int[] nodeIds, double cell)
            {
                _mesh = mesh;
                _cell = Math.Max(cell, 1E-30);
                foreach (int nodeId in nodeIds)
                {
                    var key = Key(mesh.Nodes[nodeId].Coor);
                    if (!_grid.TryGetValue(key, out List<int> ids))
                    {
                        ids = new List<int>();
                        _grid.Add(key, ids);
                    }
                    ids.Add(nodeId);
                }
            }
            public List<int> Find(List<double[]> positions, double maxDistance)
            {
                List<int> found = new List<int>();
                int range = (int)Math.Ceiling(maxDistance / _cell);
                foreach (double[] position in positions)
                {
                    var key = Key(position);
                    int best = -1;
                    double bestDistance = maxDistance;
                    for (long dx = -range; dx <= range; dx++)
                        for (long dy = -range; dy <= range; dy++)
                            for (long dz = -range; dz <= range; dz++)
                            {
                                if (!_grid.TryGetValue((key.Item1 + dx, key.Item2 + dy, key.Item3 + dz), out List<int> ids))
                                    continue;
                                foreach (int nodeId in ids)
                                {
                                    double[] coor = _mesh.Nodes[nodeId].Coor;
                                    double dxx = coor[0] - position[0], dyy = coor[1] - position[1], dzz = coor[2] - position[2];
                                    double distance = Math.Sqrt(dxx * dxx + dyy * dyy + dzz * dzz);
                                    if (distance <= bestDistance)
                                    {
                                        bestDistance = distance;
                                        best = nodeId;
                                    }
                                }
                            }
                    if (best >= 0) found.Add(best);
                }
                return found;
            }
            private (long, long, long) Key(double[] coor)
            {
                return ((long)Math.Floor(coor[0] / _cell), (long)Math.Floor(coor[1] / _cell), (long)Math.Floor(coor[2] / _cell));
            }
        }
    }
}
