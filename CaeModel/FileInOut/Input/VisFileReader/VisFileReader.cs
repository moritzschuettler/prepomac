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
using System.Threading.Tasks;
using System.IO;
using CaeMesh;
using CaeGlobals;
using CaeModel;

namespace FileInOut.Input
{
    static public class VisFileReader
    {
        private static string[] spaceSplitter = new string[] { " " };
        private static string[] lineSplitter = new string[] { "\r", "\n" };
        private static string[] allSplitter = new string[] { " ", "\r", "\n" };

        public static FeMesh Read(string fileName)
        {
            if (File.Exists(fileName))
            {
                string data = File.ReadAllText(fileName);
                GeometryMetadata sourceGeometryMetadata = GeometryMetadataReader.Read(data);
                //
                Dictionary<int, HashSet<int>> surfaceIdNodeIds = new Dictionary<int, HashSet<int>>();
                Dictionary<int, HashSet<int>> edgeIdNodeIds = new Dictionary<int, HashSet<int>>();
                HashSet<int> vertexNodeIds = new HashSet<int>();
                Dictionary<int, FeNode> nodes = new Dictionary<int, FeNode>();
                Dictionary<int, FeElement> elements = new Dictionary<int, FeElement>();
                Dictionary<int, GeomFaceType> faceTypes = new Dictionary<int, GeomFaceType>();
                Dictionary<int, GeomCurveType> edgeTypes = new Dictionary<int, GeomCurveType>();
                //
                BoundingBox bBox = new BoundingBox();
                double epsilon = 1E-9;
                int offsetNodeId = 0;
                int offsetElementId = 0;
                // Read the vertices first - later the merging of nodes is done
                string[] vertexSplitData = data.Split(new string[] { "Number of visualization vertices: " },
                                                      StringSplitOptions.RemoveEmptyEntries);
                if (vertexSplitData.Length >= 2)
                {
                    string vertexData = vertexSplitData[1];
                    int endVertexData = vertexData.IndexOf("****");
                    vertexData = vertexData.Substring(0, endVertexData);
                    offsetNodeId = ReadNodes(vertexData, offsetNodeId, nodes, ref bBox);
                    //
                    vertexNodeIds.UnionWith(nodes.Keys);
                }
                //
                ImportOptions importOptions = ImportOptions.DetectEdges;
                // Import solid geometry
                if (sourceGeometryMetadata != null && sourceGeometryMetadata.GetEntities("solid").Length > 0)
                {
                    importOptions = ImportOptions.ImportOneCADSolidPart;
                }
                // Import shell geometry
                else if (sourceGeometryMetadata != null &&
                         (sourceGeometryMetadata.GetEntities("shell").Length > 0 ||
                          sourceGeometryMetadata.GetEntities("face").Length > 0))
                {
                    importOptions = ImportOptions.ImportCADShellParts;
                }
                //
                if (sourceGeometryMetadata != null)
                {
                    string[] faceData = data.Split(new string[] { "Face mesh number: " },
                                                   StringSplitOptions.RemoveEmptyEntries);
                    //
                    for (int i = 1; i < faceData.Length; i++)
                    {
                        ReadFace(faceData[i], sourceGeometryMetadata, ref offsetNodeId, nodes,
                                 ref offsetElementId, elements, surfaceIdNodeIds, faceTypes,
                                 edgeIdNodeIds, edgeTypes, ref bBox);
                    }
                    //
                    double max = bBox.GetDiagonal();
                    MergeNodes(nodes, elements, surfaceIdNodeIds, edgeIdNodeIds, epsilon * max, out _);
                    MergeEdgeElements(elements);
                    //
                    FeMesh mesh = new FeMesh(nodes, elements, MeshRepresentation.Geometry, importOptions);
                    //
                    mesh.ConvertLineFeElementsToEdges(vertexNodeIds, true);
                    //
                    Dictionary<string, Dictionary<int, int>> partLocalFaceIdSourceFaceId =
                        new Dictionary<string, Dictionary<int, int>>();
                    Dictionary<string, Dictionary<int, int>> partLocalEdgeIdSourceEdgeId =
                        new Dictionary<string, Dictionary<int, int>>();
                    mesh.RenumberVisualizationSurfaces(surfaceIdNodeIds, faceTypes,
                                                       partLocalFaceIdSourceFaceId);
                    mesh.RenumberVisualizationEdges(edgeIdNodeIds, edgeTypes,
                                                    partLocalEdgeIdSourceEdgeId);
                    //
                    mesh.RemoveElementsByType<FeElement1D>();
                    mesh.RemoveElementsByType<FeElement3D>();
                    // The metadata entity IDs are converted from OCC IDs to the same local, 1-based IDs
                    // used by PrePoMax selections. Exact CAD measurements become the canonical values of
                    // geometry parts; later mesh transfer only fills values that the mesh does not have.
                    if (sourceGeometryMetadata != null)
                    {
                        foreach (var entry in mesh.Parts)
                        {
                            Dictionary<int, int> localFaceIdSourceFaceId;
                            Dictionary<int, int> localEdgeIdSourceEdgeId;
                            partLocalFaceIdSourceFaceId.TryGetValue(entry.Key, out localFaceIdSourceFaceId);
                            partLocalEdgeIdSourceEdgeId.TryGetValue(entry.Key, out localEdgeIdSourceEdgeId);
                            entry.Value.GeometryMetadata =
                                GeometryMetadataReader.CreatePartMetadata(sourceGeometryMetadata,
                                                                          localFaceIdSourceFaceId,
                                                                          localEdgeIdSourceEdgeId);
                            GeometryMetadataReader.ApplyCanonicalMeasurements(entry.Value);
                        }
                    }
                    return mesh;
                }
            }
            //
            return null;
        }
        private static void ReadFace(string faceData, GeometryMetadata geometryMetadata,
                                     ref int offsetNodeId, Dictionary<int, FeNode> nodes,
                                     ref int offsetElementId, Dictionary<int, FeElement> elements,
                                     Dictionary<int, HashSet<int>> surfaceIdNodeIds,
                                     Dictionary<int, GeomFaceType> faceTypes,
                                     Dictionary<int, HashSet<int>> edgeIdNodeIds,
                                     Dictionary<int, GeomCurveType> edgeTypes,
                                     ref BoundingBox bBox)
        {
            int numOfNodes = 0;
            int numOfElements = 0;
            string[] data = faceData.Split(new string[] { "*", "Number of " }, StringSplitOptions.RemoveEmptyEntries);
            //
            string[] tmp = data[0].Split(allSplitter, StringSplitOptions.RemoveEmptyEntries);
            int surfaceId = int.Parse(tmp[0]);
            GeometryMetadataEntity face = geometryMetadata.GetEntity("face", surfaceId);
            if (face == null) throw new FormatException("The visualization face " + surfaceId +
                                                        " has no geometry definition.");
            string[] values;
            if (!face.TryGetText("orientation", out values) || values.Length != 1)
                throw new FormatException("The geometry face " + surfaceId + " has no orientation.");
            bool reverse = values[0] == "Reversed";
            if (!face.TryGetCanonicalText("type", out values) || values.Length != 1)
                throw new FormatException("The geometry face " + surfaceId + " has no type.");
            GeomFaceType faceType = (GeomFaceType)Enum.Parse(typeof(GeomFaceType), values[0]);
            //
            if (!faceTypes.ContainsKey(surfaceId)) faceTypes.Add(surfaceId, faceType);
            // Some triangles might be missing - use only trinagle nodes for surface definition - fix geometry problems
            HashSet<int> triangleNodeIds = new HashSet<int>();
            //
            Dictionary<int, FeNode> surfaceNodes = new Dictionary<int, FeNode>();
            CompareIntArray comparer = new CompareIntArray();
            HashSet<int[]> freeEdgeKeys = new HashSet<int[]>(comparer);
            for (int i = 1; i < data.Length; i++)
            {
                if (data[i].StartsWith("nodes"))
                {
                    numOfNodes = ReadNodes(data[i], offsetNodeId, surfaceNodes, ref bBox);
                    nodes.AddRange(surfaceNodes);
                }
                else if (data[i].StartsWith("triangles"))
                {
                    numOfElements = ReadTriangles(data[i], reverse, offsetNodeId, offsetElementId, elements, triangleNodeIds,
                                                  freeEdgeKeys);
                    offsetElementId += numOfElements;
                }
                else if (data[i].StartsWith("edge polylines"))
                {
                    numOfElements = ReadEdges(data[i], geometryMetadata, offsetNodeId, offsetElementId, elements,
                                              edgeIdNodeIds, edgeTypes, freeEdgeKeys);
                    offsetElementId += numOfElements;
                }
            }
            //
            offsetNodeId += numOfNodes;
            // Remove missing nodes from edge node ids - fix geometry problems
            HashSet<int> missingNodes = new HashSet<int>(surfaceNodes.Keys.Except(triangleNodeIds));
            if (missingNodes.Count > 0)
            {
                foreach (var entry in edgeIdNodeIds) entry.Value.ExceptWith(missingNodes);
            }
            // Add surface node ids if it contains more than 1 node
            if (triangleNodeIds.Count > 0)
            {
                HashSet<int> surface;
                if (surfaceIdNodeIds.TryGetValue(surfaceId, out surface)) surface.UnionWith(triangleNodeIds);
                else surfaceIdNodeIds.Add(surfaceId, new HashSet<int>(triangleNodeIds)); // create a copy!!!
            }
        }

        private static int ReadNodes(string nodeData, int offsetNodeId, Dictionary<int, FeNode> nodes, ref BoundingBox bBox)
        {
            string[] data = nodeData.Split(lineSplitter, StringSplitOptions.RemoveEmptyEntries);
            string[] tmp;
            FeNode node;
            //
            for (int i = 1; i < data.Length; i++)   // skip first row: Number of nodes: 14
            {
                tmp = data[i].Split(spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                node = new FeNode();
                node.Id = int.Parse(tmp[0]) + offsetNodeId;
                node.X = double.Parse(tmp[1]);
                node.Y = double.Parse(tmp[2]);
                node.Z = double.Parse(tmp[3]);
                //
                bBox.IncludeNode(node);
                //
                nodes.Add(node.Id, node);
            }
            //
            return data.Length - 1; // return number of read nodes
        }
        private static int ReadTriangles(string elementData, bool reverse, int offsetNodeId, int offsetElementId,
                                         Dictionary<int, FeElement> elements, HashSet<int> triangleNodeIds, HashSet<int[]> freeEdgeKeys)
        {
            int id;
            int[] key;
            int[] count;
            int[] nodeIds;
            string[] data = elementData.Split(lineSplitter, StringSplitOptions.RemoveEmptyEntries);
            string[] tmp;
            LinearTriangleElement element;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], int[]> edgeKeyCount = new Dictionary<int[], int[]>(comparer);
            //
            for (int i = 1; i < data.Length; i++)   // skip first row: Number of elements: 23
            {
                tmp = data[i].Split(spaceSplitter, StringSplitOptions.RemoveEmptyEntries);
                id = int.Parse(tmp[0]) + offsetElementId;
                nodeIds = new int[3];
                nodeIds[0] = int.Parse(tmp[1]) + offsetNodeId;
                if (reverse)
                {
                    nodeIds[1] = int.Parse(tmp[3]) + offsetNodeId;
                    nodeIds[2] = int.Parse(tmp[2]) + offsetNodeId;
                }
                else
                {
                    nodeIds[1] = int.Parse(tmp[2]) + offsetNodeId;
                    nodeIds[2] = int.Parse(tmp[3]) + offsetNodeId;
                }
                triangleNodeIds.UnionWith(nodeIds);
                //
                element = new LinearTriangleElement(id, nodeIds);
                //
                elements.Add(element.Id, element);
                //
                key = nodeIds[0] < nodeIds[1] ? new int[] { nodeIds[0], nodeIds[1] } : new int[] { nodeIds[1], nodeIds[0] };
                if (edgeKeyCount.TryGetValue(key, out count)) count[0]++;
                else edgeKeyCount[key] = new int[] { 1 };
                //
                key = nodeIds[1] < nodeIds[2] ? new int[] { nodeIds[1], nodeIds[2] } : new int[] { nodeIds[2], nodeIds[1] };
                if (edgeKeyCount.TryGetValue(key, out count)) count[0]++;
                else edgeKeyCount[key] = new int[] { 1 };
                //
                key = nodeIds[2] < nodeIds[0] ? new int[] { nodeIds[2], nodeIds[0] } : new int[] { nodeIds[0], nodeIds[2] };
                if (edgeKeyCount.TryGetValue(key, out count)) count[0]++;
                else edgeKeyCount[key] = new int[] { 1 };
            }
            //
            foreach (var entry in edgeKeyCount)
            {
                if (entry.Value[0] == 1) freeEdgeKeys.Add(entry.Key);
            }
            return data.Length - 1; // return number of read elements
        }
        private static int ReadEdges(string elementData, GeometryMetadata geometryMetadata,
                                     int offsetNodeId, int offsetElementId,
                                     Dictionary<int, FeElement> elements, Dictionary<int, HashSet<int>> edgeIdNodeIds,
                                     Dictionary<int, GeomCurveType> edgeTypes, HashSet<int[]> freeEdgeKeys)
        {
            string[] data = elementData.Split(lineSplitter, StringSplitOptions.RemoveEmptyEntries);
            GeomCurveType edgeType;
            GeomCurveType prevEdgeType;
            string[] splitData;
            int id;
            int internalId = 1;
            int[] nodeIds;
            LinearBeamElement element;
            //
            int edgeId;
            int[] key;
            int[] edgeNodeIdsArr;
            HashSet<int> edgeNodeIds = new HashSet<int>();
            HashSet<int> edgeNodeIdsOut;
            //
            for (int i = 1; i < data.Length; i++)   // skip first row: Number of edge polylines: 4
            {
                string line = data[i].Trim();
                const string prefix = "Edge polyline:";
                if (!line.StartsWith(prefix, StringComparison.Ordinal))
                    throw new FormatException("Invalid edge polyline: " + line);
                splitData = line.Substring(prefix.Length).Split(spaceSplitter,
                                                                 StringSplitOptions.RemoveEmptyEntries);
                edgeId = int.Parse(splitData[0]);
                GeometryMetadataEntity edge = geometryMetadata.GetEntity("edge", edgeId);
                string[] values;
                if (edge == null || !edge.TryGetCanonicalText("type", out values) || values.Length != 1)
                    throw new FormatException("The geometry edge " + edgeId + " has no type.");
                edgeType = (GeomCurveType)Enum.Parse(typeof(GeomCurveType), values[0]);
                //
                edgeNodeIds.Clear();
                for (int j = 1; j < splitData.Length - 1; j++)
                {
                    nodeIds = new int[2];
                    nodeIds[0] = int.Parse(splitData[j]) + offsetNodeId;
                    nodeIds[1] = int.Parse(splitData[j + 1]) + offsetNodeId;
                    //
                    edgeNodeIds.Add(nodeIds[0]);
                    edgeNodeIds.Add(nodeIds[1]);
                }
                edgeNodeIdsArr = edgeNodeIds.ToArray();
                for (int j = 0; j < edgeNodeIdsArr.Length; j++)
                {
                    for (int k = j + 1; k < edgeNodeIdsArr.Length; k++)
                    {
                        key = edgeNodeIdsArr[j] < edgeNodeIdsArr[k] ? new int[] { edgeNodeIdsArr[j], edgeNodeIdsArr[k] } :
                              new int[] { edgeNodeIdsArr[k], edgeNodeIdsArr[j] };
                        if (freeEdgeKeys.Contains(key))
                        {
                            id = internalId + offsetElementId;
                            element = new LinearBeamElement(id, new int[] { edgeNodeIdsArr[j] , edgeNodeIdsArr[k] });
                            elements.Add(element.Id, element);
                            internalId++;
                        }
                    }
                }
                //
                if (edgeIdNodeIds.TryGetValue(edgeId, out edgeNodeIdsOut)) edgeNodeIdsOut.UnionWith(edgeNodeIds);
                else edgeIdNodeIds.Add(edgeId, new HashSet<int>(edgeNodeIds));      // create a copy!!!
                //
                if (edgeTypes.TryGetValue(edgeId, out prevEdgeType))
                {
                    if (prevEdgeType != edgeType) throw new NotSupportedException();
                }
                else edgeTypes.Add(edgeId, edgeType);
            }
            //
            return internalId - 1; // return number of read edges
        }
        private static void MergeNodes(Dictionary<int, FeNode> nodes,
                                       Dictionary<int, FeElement> elements,
                                       Dictionary<int, HashSet<int>> surfaceIdNodeIds,
                                       Dictionary<int, HashSet<int>> edgeIdNodeIds,
                                       double epsilon,
                                       out int[] mergedNodeIds)
        {
            int count = 0;
            FeNode[] sortedNodes = new FeNode[nodes.Count];
            // Sort the nodes by x
            foreach (var entry in nodes) sortedNodes[count++] = entry.Value;
            //
            IComparer<FeNode> comparerByX = new CompareFeNodeByX();
            Array.Sort(sortedNodes, comparerByX);
            // Create a map of node ids to be merged to another node id
            Dictionary<int, int> oldIdNewIdMap = new Dictionary<int, int>();
            for (int i = 0; i < sortedNodes.Length - 1; i++)
            {
                if (oldIdNewIdMap.ContainsKey(sortedNodes[i].Id)) continue;       // this node was merged and does not exist anymore
                //
                for (int j = i + 1; j < sortedNodes.Length; j++)
                {
                    if (oldIdNewIdMap.ContainsKey(sortedNodes[j].Id)) continue;   // this node was merged and does not exist anymore
                    //
                    if (Math.Abs(sortedNodes[i].X - sortedNodes[j].X) < epsilon)
                    {
                        if (Math.Abs(sortedNodes[i].Y - sortedNodes[j].Y) < epsilon)
                        {
                            if (Math.Abs(sortedNodes[i].Z - sortedNodes[j].Z) < epsilon)
                            {
                                oldIdNewIdMap.Add(sortedNodes[j].Id, sortedNodes[i].Id);
                            }
                        }
                    }
                    else
                    {
                        break;  // since nodes are sortred by X if dX > epsilon break;
                    }
                }
            }
            // Collect close nodes into bins
            HashSet<int> allNodeIds;
            Dictionary<int, HashSet<int>> newNodeIdAllNodeIds = new Dictionary<int, HashSet<int>>();
            foreach (var entry in oldIdNewIdMap)
            {
                if (newNodeIdAllNodeIds.TryGetValue(entry.Value, out allNodeIds)) allNodeIds.Add(entry.Key);
                else newNodeIdAllNodeIds.Add(entry.Value, new HashSet<int>() { entry.Value, entry.Key });
            }
            // Find the smallest node id
            oldIdNewIdMap.Clear();
            int[] sortedNodeIds;
            foreach (var entry in newNodeIdAllNodeIds)
            {
                sortedNodeIds = entry.Value.ToArray();
                Array.Sort(sortedNodeIds);
                //
                for (int i = 1; i < sortedNodeIds.Length; i++) oldIdNewIdMap.Add(sortedNodeIds[i], sortedNodeIds[0]);
            }
            // Remove unused nodes
            mergedNodeIds = oldIdNewIdMap.Keys.ToArray();
            foreach (int mergedNode in mergedNodeIds) nodes.Remove(mergedNode);
            // Apply the map to the elements
            int newId;
            HashSet<int> nodeIds = new HashSet<int>();
            List<int> elementIdsToRemove = new List<int>();
            foreach (var entry in elements)
            {
                nodeIds.Clear();
                for (int i = 0; i < entry.Value.NodeIds.Length; i++)
                {
                    if (oldIdNewIdMap.TryGetValue(entry.Value.NodeIds[i], out newId)) entry.Value.NodeIds[i] = newId;
                    //
                    nodeIds.Add(entry.Value.NodeIds[i]);
                }
                if (nodeIds.Count != entry.Value.NodeIds.Length) elementIdsToRemove.Add(entry.Key);
            }
            // Remove collapsed elements
            foreach (var elementId in elementIdsToRemove)
            {
                elements.Remove(elementId);
                // Might be also necessary to remove some nodes ?
            }
            // Surface node ids
            HashSet<int> newIds = new HashSet<int>();
            foreach (var entry in surfaceIdNodeIds)
            {
                newIds.Clear();
                foreach (var nodeId in entry.Value)
                {
                    if (oldIdNewIdMap.TryGetValue(nodeId, out newId)) newIds.Add(newId);
                    else newIds.Add(nodeId);
                }
                entry.Value.Clear();
                entry.Value.UnionWith(newIds);
            }
            // Edge node ids
            foreach (var entry in edgeIdNodeIds)
            {
                newIds.Clear();
                foreach (var nodeId in entry.Value)
                {
                    if (oldIdNewIdMap.TryGetValue(nodeId, out newId)) newIds.Add(newId);
                    else newIds.Add(nodeId);
                }
                entry.Value.Clear();
                entry.Value.UnionWith(newIds);
            }
        }
        private static void MergeEdgeElements(Dictionary<int, FeElement> elements)
        {
            int[] key;
            FeElement[] elementsToRemove;
            List<FeElement> elementsToMerge;
            CompareIntArray comparer = new CompareIntArray();
            Dictionary<int[], List<FeElement>> nodeIdsElements = new Dictionary<int[], List<FeElement>>(comparer);
            foreach (var entry in elements)
            {
                if (entry.Value is FeElement1D edgeElement)
                {
                    key = edgeElement.NodeIds;
                    Array.Sort(key);
                    if (nodeIdsElements.TryGetValue(key, out elementsToMerge)) elementsToMerge.Add(edgeElement);
                    else nodeIdsElements.Add(key, new List<FeElement>() { edgeElement });
                }
            }
            //
            foreach (var entry in nodeIdsElements)
            {
                if (entry.Value.Count > 1)
                {
                    elementsToRemove = entry.Value.ToArray();
                    for (int i = 1; i < elementsToRemove.Length; i++)
                    {
                        elements.Remove(elementsToRemove[i].Id);
                    }
                }
            }
        }

    }
}
