using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CaeMesh;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using CaeResults;
using CaeGlobals;

namespace FileInOut.Input
{
    static public class MmgFileReader
    {
        private static string[] _spaceSplitter = new string[] { " " };
        public static FeMesh Read(string fileName, ElementsToImport elementsToImport,
                                  MeshRepresentation meshRepresentation,
                                  bool convertToSecondOrder = false,
                                  int firstNodeId = 1,
                                  int firstElementId = 1,
                                  Dictionary<int, FeNode> existingNodes = null,
                                  Dictionary<int[], FeNode> existingMidNodes = null,
                                  double epsilon = 1E-6,
                                  Dictionary<string, Dictionary<int, int>> partNameNewSurfIdOldSurfId = null,
                                  Dictionary<string, Dictionary<int, int>> partNameNewEdgeIdOldEdgeId = null)
        {
            if (File.Exists(fileName))
            {
                Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
                Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
                HashSet<int> surfaceNodeIds;
                HashSet<int> edgeNodeIds;
                Dictionary<int, HashSet<int>> surfaceIdElementIds = null;
                Dictionary<int, HashSet<int>> surfaceIdNodeIds = new Dictionary<int, HashSet<int>>();
                Dictionary<int, HashSet<int>> edgeIdElementIds = null;
                Dictionary<int, HashSet<int>> edgeIdNodeIds = new Dictionary<int, HashSet<int>>();
                Dictionary<int, Dictionary<int, bool>> edgeIdNodeIdCount = null;
                Dictionary<int, int> oldNodeIdNewNodeId = null;
                //
                int nodeId = firstNodeId;
                int elementId = firstElementId;
                int maxPartId = 1;
                string[] lines = CaeGlobals.Tools.ReadAllLines(fileName);
                // Read
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].ToUpper().StartsWith("VERTICES"))
                    {
                        ReadVertices(lines, ref i, ref nodeId, existingNodes, epsilon, ref nodes, out oldNodeIdNewNodeId);
                    }
                    else if (lines[i].ToUpper().StartsWith("EDGES"))
                    {
                        ReadEdges(lines, ref i, ref elementId, oldNodeIdNewNodeId, ref elements,
                                  out edgeIdNodeIdCount, out edgeIdElementIds);
                        //edgeIdNodeIdCount = new Dictionary<int, Dictionary<int, bool>>();
                        //edgeIdElementIds = new Dictionary<int, HashSet<int>>();
                    }
                    else if (lines[i].ToUpper().StartsWith("TRIANGLES"))
                    {
                        ReadTriangles(lines, ref i, ref elementId, ref maxPartId, oldNodeIdNewNodeId, ref elements,
                                      out surfaceIdElementIds);
                    }
                }
                // Count number of edges in an edge node
                Dictionary<int, int> nodeIdEdgeCount = new Dictionary<int, int>();
                foreach (var edgeEntry in edgeIdNodeIdCount)
                {
                    foreach (var nodeEntry in edgeEntry.Value)
                    {
                        if (nodeIdEdgeCount.ContainsKey(nodeEntry.Key)) nodeIdEdgeCount[nodeEntry.Key]++;
                        else nodeIdEdgeCount.Add(nodeEntry.Key, 1);
                    }
                }
                // Get vertices
                HashSet<int> vertexNodeIds = new HashSet<int>();
                foreach (var entry in nodeIdEdgeCount)
                {
                    if (entry.Value > 1) vertexNodeIds.Add(entry.Key);
                }
                //
                if (convertToSecondOrder) FeMesh.LinearToParabolic(ref nodes, ref elements, firstNodeId, existingMidNodes);
                // Surface node ids
                foreach (var entry in surfaceIdElementIds)
                {
                    surfaceNodeIds = new HashSet<int>();
                    foreach (var elementIdEntry in entry.Value) surfaceNodeIds.UnionWith(elements[elementIdEntry].NodeIds);
                    surfaceIdNodeIds.Add(entry.Key, surfaceNodeIds);
                }
                // Edge node ids
                foreach (var entry in edgeIdElementIds)
                {
                    edgeNodeIds = new HashSet<int>();
                    foreach (var elementIdEntry in entry.Value) edgeNodeIds.UnionWith(elements[elementIdEntry].NodeIds);
                    edgeIdNodeIds.Add(entry.Key, edgeNodeIds);
                }
                //
                FeMesh mesh = new FeMesh(nodes, elements, meshRepresentation, null, null, false,
                                         ImportOptions.DetectEdges);
                //
                mesh.ConvertLineFeElementsToEdges(vertexNodeIds);
                // Collect surfaceIdNodeIds for each part
                var allPartsSurfaceIdNodeIds = new Dictionary<string, Dictionary<int, HashSet<int>>>();
                Dictionary<int, HashSet<int>> partSurfaceIdNodeIds;
                HashSet<int> intersect;
                foreach (var partEntry in mesh.Parts)
                {
                    partSurfaceIdNodeIds = new Dictionary<int, HashSet<int>>();
                    if (partEntry.Value.PartType != PartType.Wire)
                    {
                        foreach (var surfaceEntry in surfaceIdNodeIds)
                        {
                            intersect = new HashSet<int>(surfaceEntry.Value.Intersect(partEntry.Value.NodeLabels));
                            partSurfaceIdNodeIds.Add(surfaceEntry.Key, intersect);
                        }
                    }
                    allPartsSurfaceIdNodeIds.Add(partEntry.Key, partSurfaceIdNodeIds);
                }
                // Renumber surfaces                                                                        
                mesh.RenumberVisualizationSurfaces(allPartsSurfaceIdNodeIds, null, partNameNewSurfIdOldSurfId);
                // Collect edgeIdNodeIds for each part
                var allPartsEdgeIdNodeIds = new Dictionary<string, Dictionary<int, HashSet<int>>>();
                Dictionary<int, HashSet<int>> partEdgeIdNodeIds;
                foreach (var partEntry in mesh.Parts)
                {
                    partEdgeIdNodeIds = new Dictionary<int, HashSet<int>>();
                    if (partEntry.Value.PartType != PartType.Wire)
                    {
                        foreach (var edgeEntry in edgeIdNodeIds)
                        {
                            intersect = new HashSet<int>(edgeEntry.Value.Intersect(partEntry.Value.NodeLabels));
                            partEdgeIdNodeIds.Add(edgeEntry.Key, intersect);
                        }
                    }
                    allPartsEdgeIdNodeIds.Add(partEntry.Key, partEdgeIdNodeIds);
                }
                // Renumber edges                                                                        
                mesh.RenumberVisualizationEdges(allPartsEdgeIdNodeIds, partNameNewEdgeIdOldEdgeId);
                //
                if (elementsToImport != ElementsToImport.All)
                {
                    if (!elementsToImport.HasFlag(ElementsToImport.Beam)) mesh.RemoveElementsByType<FeElement1D>();
                    if (!elementsToImport.HasFlag(ElementsToImport.Shell)) mesh.RemoveElementsByType<FeElement2D>();
                    if (!elementsToImport.HasFlag(ElementsToImport.Solid)) mesh.RemoveElementsByType<FeElement3D>();
                }
                //
                return mesh;
            }
            //
            return null;
        }

        public static FeMesh Read3D(string fileName, ElementsToImport elementsToImport,
                                    MeshRepresentation meshRepresentation,
                                    bool convertToSecondOrder = false,
                                    double epsilon = 1E-6)
        {
            if (File.Exists(fileName))
            {
                Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
                Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
                Dictionary<int, int> oldNodeIdNewNodeId = null;
                //
                int nodeId = 1;
                int elementId = 1;
                int maxParId = 1;
                string[] lines = Tools.ReadAllLines(fileName);
                // Read
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].ToUpper().StartsWith("VERTICES"))
                    {
                        ReadVertices(lines, ref i, ref nodeId, null, epsilon, ref nodes, out oldNodeIdNewNodeId);
                    }
                    else if (lines[i].ToUpper().StartsWith("EDGES"))
                    {
                        ReadEdges(lines, ref i, ref elementId, oldNodeIdNewNodeId, ref elements, out _, out _);
                    }
                    else if (lines[i].ToUpper().StartsWith("TRIANGLES"))
                    {
                        ReadTriangles(lines, ref i, ref elementId, ref maxParId, oldNodeIdNewNodeId, ref elements, out _);
                    }
                    else if (lines[i].ToUpper().StartsWith("TETRAHEDRA"))
                    {
                        ReadTetrahedrons(lines, ref i, ref elementId, ref maxParId, oldNodeIdNewNodeId, ref elements, out _);
                    }
                }
                // Get edges between faces
                GetEdgesFromMaterialBoundaries(ref elementId, ref elements);
                // Count number of edges in an edge node
                Dictionary<int, int> nodeIdEdgeCount = new Dictionary<int, int>();
                foreach (var entry in elements)
                {
                    if (entry.Value is LinearBeamElement lbe)
                    {
                        if (nodeIdEdgeCount.ContainsKey(lbe.NodeIds[0])) nodeIdEdgeCount[lbe.NodeIds[0]]++;
                        else nodeIdEdgeCount.Add(lbe.NodeIds[0], 1);
                        if (nodeIdEdgeCount.ContainsKey(lbe.NodeIds[1])) nodeIdEdgeCount[lbe.NodeIds[1]]++;
                        else nodeIdEdgeCount.Add(lbe.NodeIds[1], 1);
                    }
                }
                // Get vertices
                HashSet<int> vertexNodeIds = new HashSet<int>();
                foreach (var entry in nodeIdEdgeCount)
                {
                    if (entry.Value > 2) vertexNodeIds.Add(entry.Key);
                }
                //
                if (convertToSecondOrder) FeMesh.LinearToParabolic(ref nodes, ref elements);
                //
                FeMesh mesh = new FeMesh(nodes, elements, meshRepresentation, null, null, false,
                                         ImportOptions.DetectEdges);
                //
                mesh.ConvertLineFeElementsToEdges(vertexNodeIds);
                //
                if (elementsToImport != ElementsToImport.All)
                {
                    if (!elementsToImport.HasFlag(ElementsToImport.Beam)) mesh.RemoveElementsByType<FeElement1D>();
                    if (!elementsToImport.HasFlag(ElementsToImport.Shell)) mesh.RemoveElementsByType<FeElement2D>();
                    if (!elementsToImport.HasFlag(ElementsToImport.Solid)) mesh.RemoveElementsByType<FeElement3D>();
                }
                //
                return mesh;
            }
            //
            return null;
        }
       
        private static void ReadVertices(string[] lines, ref int currentLine, ref int nodeId,
                                         Dictionary<int, FeNode> existingNodes,
                                         double epsilon, ref Dictionary<int, FeNode> nodes, 
                                         out Dictionary<int, int> oldNodeIdNewNodeId)
        {
            int numOfNodes;
            int possibleNodeId;
            string[] tmp;
            FeNode node;
            //
            oldNodeIdNewNodeId = new Dictionary<int, int>();
            currentLine++;
            //
            if (currentLine < lines.Length)
            {
                numOfNodes = int.Parse(lines[currentLine]);
                currentLine++;
                for (int j = 0; j < numOfNodes && currentLine + j < lines.Length; j++)
                {
                    tmp = lines[currentLine + j].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length >= 3)
                    {
                        node = new FeNode();
                        node.Id = nodeId++;
                        node.X = double.Parse(tmp[0]);
                        node.Y = double.Parse(tmp[1]);
                        node.Z = double.Parse(tmp[2]);
                        possibleNodeId = int.Parse(tmp[3]);
                        // If the node id is not equal to 0 check if the node exists by coordinates
                        if (existingNodes != null && possibleNodeId != 0)
                            node.Id = GetExistingNodeId(node, possibleNodeId, existingNodes, epsilon);
                        nodes.Add(node.Id, node);
                        //
                        oldNodeIdNewNodeId.Add(j + 1, node.Id);
                    }
                }
            }
        }
        private static void ReadEdges(string[] lines, ref int currentLine, ref int elementId,
                                      Dictionary<int, int> oldNodeIdNewNodeId,
                                      ref Dictionary<int, FeElement> elements,
                                      out Dictionary<int, Dictionary<int, bool>> edgeIdNodeIdCount,
                                      out Dictionary<int, HashSet<int>> edgeIdElementIds)
        {
            int numOfEdges;
            int edgeId;
            string[] tmp;
            LinearBeamElement beam;
            Dictionary<int, bool> nodeIdCount;
            HashSet<int> edgeElementIds;
            //
            edgeIdNodeIdCount = new Dictionary<int, Dictionary<int, bool>>();
            edgeIdElementIds = new Dictionary<int, HashSet<int>>();
            currentLine++;
            //
            if (currentLine < lines.Length)
            {
                numOfEdges = int.Parse(lines[currentLine]);
                currentLine++;
                for (int j = 0; j < numOfEdges && currentLine + j < lines.Length; j++)
                {
                    tmp = lines[currentLine + j].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length >= 3)
                    {
                        edgeId = int.Parse(tmp[2]);
                        //
                        if (edgeId > 0)
                        {
                            edgeId--;
                            //
                            if (!edgeIdNodeIdCount.TryGetValue(edgeId, out nodeIdCount))
                            {
                                nodeIdCount = new Dictionary<int, bool>();
                                edgeIdNodeIdCount.Add(edgeId, nodeIdCount);
                            }
                            //
                            beam = new LinearBeamElement(elementId++, new int[2]);
                            beam.NodeIds[0] = oldNodeIdNewNodeId[int.Parse(tmp[0])];
                            beam.NodeIds[1] = oldNodeIdNewNodeId[int.Parse(tmp[1])];
                            // If node already on the edge remove it
                            if (!nodeIdCount.Remove(beam.NodeIds[0])) nodeIdCount.Add(beam.NodeIds[0], true);
                            if (!nodeIdCount.Remove(beam.NodeIds[1])) nodeIdCount.Add(beam.NodeIds[1], true);
                            //
                            elements.Add(beam.Id, beam);
                            //
                            if (edgeIdElementIds.TryGetValue(edgeId, out edgeElementIds))
                                edgeElementIds.Add(beam.Id);
                            else
                                edgeIdElementIds.Add(edgeId, new HashSet<int> { beam.Id });
                        }
                        //else if (System.Diagnostics.Debugger.IsAttached) throw new NotSupportedException();
                    }
                    else if (System.Diagnostics.Debugger.IsAttached) throw new NotSupportedException();
                }
            }
        }
        private static void ReadTriangles(string[] lines, ref int currentLine, ref int elementId, ref int maxPartId,
                                          Dictionary<int, int> oldNodeIdNewNodeId,
                                          ref Dictionary<int, FeElement> elements,
                                          out Dictionary<int, HashSet<int>> surfaceIdElementIds)
        {
            int numOfElements;
            int surfaceId;
            int newMaxPartId = 0;
            string[] tmp;
            HashSet<int> surfaceElementIds;
            LinearTriangleElement triangle;
            //
            surfaceIdElementIds = new Dictionary<int, HashSet<int>>();
            currentLine++;
            //
            if (currentLine < lines.Length)
            {
                numOfElements = int.Parse(lines[currentLine]);
                currentLine++;
                for (int j = 0; j < numOfElements && currentLine + j < lines.Length; j++)
                {
                    tmp = lines[currentLine + j].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length >= 4)
                    {
                        triangle = new LinearTriangleElement(elementId++, new int[3]);
                        triangle.NodeIds[0] = oldNodeIdNewNodeId[int.Parse(tmp[0])];
                        triangle.NodeIds[1] = oldNodeIdNewNodeId[int.Parse(tmp[1])];
                        triangle.NodeIds[2] = oldNodeIdNewNodeId[int.Parse(tmp[2])];
                        surfaceId = int.Parse(tmp[3]) - 1;
                        //
                        triangle.PartId = surfaceId + 1 + maxPartId + 1;
                        if (triangle.PartId > newMaxPartId) newMaxPartId = triangle.PartId;
                        //
                        
                        //
                        elements.Add(triangle.Id, triangle);
                        //
                        if (surfaceIdElementIds.TryGetValue(surfaceId, out surfaceElementIds))
                            surfaceElementIds.Add(triangle.Id);
                        else
                            surfaceIdElementIds.Add(surfaceId, new HashSet<int> { triangle.Id });
                    }
                }
            }
            //
            maxPartId = newMaxPartId;
        }
        private static void ReadTetrahedrons(string[] lines, ref int currentLine, ref int elementId, ref int maxPartId,
                                             Dictionary<int, int> oldNodeIdNewNodeId,
                                             ref Dictionary<int, FeElement> elements,
                                             out Dictionary<int, HashSet<int>> partIdElementIds)
        {
            int numOfElements;
            int newMaxPartId = 0;
            string[] tmp;
            LinearTetraElement tetra;
            HashSet<int> surfaceElementIds;
            //
            partIdElementIds = new Dictionary<int, HashSet<int>>();
            currentLine++;
            //
            if (currentLine < lines.Length)
            {
                numOfElements = int.Parse(lines[currentLine]);
                currentLine++;
                for (int j = 0; j < numOfElements && currentLine + j < lines.Length; j++)
                {
                    tmp = lines[currentLine + j].Split(_spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length >= 5)
                    {
                        tetra = new LinearTetraElement(elementId++, new int[4]);
                        tetra.NodeIds[0] = oldNodeIdNewNodeId[int.Parse(tmp[0])];
                        tetra.NodeIds[1] = oldNodeIdNewNodeId[int.Parse(tmp[1])];
                        tetra.NodeIds[2] = oldNodeIdNewNodeId[int.Parse(tmp[2])];
                        tetra.NodeIds[3] = oldNodeIdNewNodeId[int.Parse(tmp[3])];
                        tetra.PartId = int.Parse(tmp[4]) + maxPartId + 1;
                        if (tetra.PartId > newMaxPartId) newMaxPartId = tetra.PartId;
                        //
                        elements.Add(tetra.Id, tetra);
                        //
                        if (partIdElementIds.TryGetValue(tetra.PartId, out surfaceElementIds))
                            surfaceElementIds.Add(tetra.Id);
                        else
                            partIdElementIds.Add(tetra.PartId, new HashSet<int> { tetra.Id });
                    }
                }
            }
            //
            maxPartId = newMaxPartId;
        }
        //
        private static void GetEdgesFromMaterialBoundaries(ref int elementId, ref Dictionary<int, FeElement> elements)
        {
            int id1;
            int id2;
            int[] key;
            List<int> surfaceIds;
            CompareIntArray comparer = new CompareIntArray();
            HashSet<int[]> existingEdges = new HashSet<int[]>(comparer);
            Dictionary<int[], List<int>> edgeSurfaceId = new Dictionary<int[], List<int>>(comparer);
            //
            foreach (var entry in elements)
            {
                if (entry.Value is LinearBeamElement existingBeam)
                {
                    id1 = existingBeam.NodeIds[0];
                    id2 = existingBeam.NodeIds[1];
                    if (id1 > id2) (id1, id2) = (id2, id1); // sort
                    key = new int[] { id1, id2 };
                    existingEdges.Add(key);
                }
                else if (entry.Value is LinearTriangleElement triangle)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        id1 = triangle.NodeIds[i % 3];
                        id2 = triangle.NodeIds[(i + 1) % 3];
                        if (id1 > id2) (id1, id2) = (id2, id1); // sort
                        key = new int[] { id1, id2 };
                        //
                        if (edgeSurfaceId.TryGetValue(key, out surfaceIds)) surfaceIds.Add(triangle.PartId);
                        else edgeSurfaceId.Add(key, new List<int> { triangle.PartId });
                    }
                }
            }
            HashSet<int> uniqueSurfaceIds;
            LinearBeamElement beam;
            foreach (var entry in edgeSurfaceId)
            {
                if (!existingEdges.Contains(entry.Key))
                {
                    uniqueSurfaceIds = new HashSet<int>(entry.Value);
                    // Free edge
                    if (entry.Value.Count == 1)
                    {
                        beam = new LinearBeamElement(elementId++, entry.Key);
                        elements.Add(beam.Id, beam);
                    }
                    // Internal surface edge
                    else if (entry.Value.Count == 2 && uniqueSurfaceIds.Count == 1)
                    { }
                    // Edge
                    else if (entry.Value.Count > 2 || uniqueSurfaceIds.Count > 1)
                    {
                        beam = new LinearBeamElement(elementId++, entry.Key);
                        elements.Add(beam.Id, beam);
                    }
                }
            }
        }


        private static int GetExistingNodeId(FeNode node, int possibleId, Dictionary<int, FeNode> existingNodes, double epsilon)
        {
            FeNode node2;
            // Check if the node ids represent the same coordinates
            if (existingNodes.TryGetValue(possibleId, out node2))
            {
                if (Math.Abs(node.X - node2.X) < epsilon)
                {
                    if (Math.Abs(node.Y - node2.Y) < epsilon)
                    {
                        if (Math.Abs(node.Z - node2.Z) < epsilon)
                        {
                            return node2.Id;
                        }
                    }
                }
            }
            // Search for the same coordinates
            foreach (var entry in existingNodes)
            {
                if (Math.Abs(node.X - entry.Value.X) < epsilon)
                {
                    if (Math.Abs(node.Y - entry.Value.Y) < epsilon)
                    {
                        if (Math.Abs(node.Z - entry.Value.Z) < epsilon)
                        {
                            return entry.Key;
                        }
                    }
                }
            }
            //
            return node.Id;
        }
    }
}

