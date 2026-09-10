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
using CaeGlobals;

namespace CaeMesh
{
    public class ContactGraph
    {
        // Variables                                                                                                                
        private Graph<NodeData> _graph;


        // Constructors                                                                                                             
        public ContactGraph()
        {
            _graph = new Graph<NodeData>();
        }


        // Methods                                                                                                                  
        public void AddMasterSlaveItems(IEnumerable<MasterSlaveItem> masterSlaveItems, FeMesh mesh)
        {
            AddMasterSlaveItems(masterSlaveItems, mesh, false);
        }
        public void AddMasterSlaveItems(IEnumerable<MasterSlaveItem> masterSlaveItems, FeMesh mesh,
                                        bool mergeNodeOverlaps)
        {
            HashSet<int> mergedItemIds;
            List<HashSet<int>> itemIdsList = new List<HashSet<int>>();
            List<HashSet<int>> itemIdsListToRemove = new List<HashSet<int>>();
            Dictionary<int, int> itemIdGroupIds = null;
            if (mergeNodeOverlaps) itemIdGroupIds = GetItemIdGroupIds(masterSlaveItems, mesh);
            // Collect item ids
            foreach (var masterSlaveItem in masterSlaveItems)
            {
                // Master side                                                          
                itemIdsListToRemove.Clear();
                mergedItemIds = new HashSet<int>(masterSlaveItem.MasterGeometryIds);
                // Find intersecting
                foreach (var node in itemIdsList)
                {
                    if (ItemIdSetsOverlap(node, mergedItemIds, itemIdGroupIds))
                    {
                        mergedItemIds.UnionWith(node);
                        itemIdsListToRemove.Add(node);
                    }
                }
                // Remove merged
                foreach (var node in itemIdsListToRemove) itemIdsList.Remove(node);
                // Add new/merged item
                itemIdsList.Add(mergedItemIds);
                // Slave side                                                           
                itemIdsListToRemove.Clear();
                mergedItemIds = new HashSet<int>(masterSlaveItem.SlaveGeometryIds);
                // Find intersecting
                foreach (var node in itemIdsList)
                {
                    if (ItemIdSetsOverlap(node, mergedItemIds, itemIdGroupIds))
                    {
                        mergedItemIds.UnionWith(node);
                        itemIdsListToRemove.Add(node);
                    }
                }
                // Remove merged
                foreach (var node in itemIdsListToRemove) itemIdsList.Remove(node);
                // Add new/merged item
                itemIdsList.Add(mergedItemIds);
            }
            // Add items to graph
            int id;
            string name;
            List<string> allNames = new List<string>();
            double size;
            NodeData nodeData;
            foreach (var itemIds in itemIdsList)
            {
                id = _graph.Nodes.Count() + 1;
                name = GetNameFromItemIds(itemIds, allNames, mesh);
                size = GetSizeFromItemIds(itemIds, mesh);
                nodeData = new NodeData(id, name, itemIds, size);
                //
                _graph.AddNode(new Node<NodeData>(nodeData));
                allNames.Add(name);
            }
            // Add edges to graph
            Node<NodeData> masterNode;
            Node<NodeData> slaveNode;
            foreach (var masterSlaveItem in masterSlaveItems)
            {
                masterNode = null;
                slaveNode = null;
                foreach (Node<NodeData> node in _graph.Nodes)
                {
                    // Find the nodes
                    if (masterNode == null && node.Value.ItemIds.Intersect(masterSlaveItem.MasterGeometryIds).Count() > 0)
                        masterNode = node;
                    // Use if - master and slave ids can belong to the same graph node
                    if (slaveNode == null && node.Value.ItemIds.Intersect(masterSlaveItem.SlaveGeometryIds).Count() > 0)
                        slaveNode = node;
                    if (masterNode != null && slaveNode != null) break;
                }
                // Surfaces merged into a single graph node are already connected by the mesh
                if (masterNode != slaveNode) _graph.AddUndirectedEdge(masterNode, slaveNode);
            }
        }
        private static bool ItemIdSetsOverlap(HashSet<int> firstItemIds, HashSet<int> secondItemIds,
                                              Dictionary<int, int> itemIdGroupIds)
        {
            if (firstItemIds.Overlaps(secondItemIds)) return true;
            if (itemIdGroupIds == null) return false;
            //
            HashSet<int> firstGroupIds = new HashSet<int>();
            foreach (int itemId in firstItemIds) firstGroupIds.Add(itemIdGroupIds[itemId]);
            foreach (int itemId in secondItemIds)
            {
                if (firstGroupIds.Contains(itemIdGroupIds[itemId])) return true;
            }
            //
            return false;
        }
        private static Dictionary<int, int> GetItemIdGroupIds(IEnumerable<MasterSlaveItem> masterSlaveItems, FeMesh mesh)
        {
            HashSet<int> allItemIds = new HashSet<int>();
            foreach (var masterSlaveItem in masterSlaveItems)
            {
                if (masterSlaveItem.MasterGeometryIds != null) allItemIds.UnionWith(masterSlaveItem.MasterGeometryIds);
                if (masterSlaveItem.SlaveGeometryIds != null) allItemIds.UnionWith(masterSlaveItem.SlaveGeometryIds);
            }
            //
            List<int> sortedItemIds = allItemIds.ToList();
            sortedItemIds.Sort();
            Dictionary<int, int> itemIdParents = new Dictionary<int, int>();
            foreach (int itemId in sortedItemIds) itemIdParents.Add(itemId, itemId);
            // Union all items sharing a mesh node - the union is transitive
            int sharedItemId;
            Dictionary<int, int> nodeIdItemIds = new Dictionary<int, int>();
            foreach (int itemId in sortedItemIds)
            {
                foreach (int nodeId in GetGeometryNodeIds(itemId, mesh))
                {
                    if (nodeIdItemIds.TryGetValue(nodeId, out sharedItemId))
                        UnionItemIds(sharedItemId, itemId, itemIdParents);
                    else nodeIdItemIds.Add(nodeId, itemId);
                }
            }
            //
            Dictionary<int, int> itemIdGroupIds = new Dictionary<int, int>();
            foreach (int itemId in sortedItemIds) itemIdGroupIds.Add(itemId, GetRootItemId(itemId, itemIdParents));
            //
            return itemIdGroupIds;
        }
        private static void UnionItemIds(int firstItemId, int secondItemId, Dictionary<int, int> itemIdParents)
        {
            int firstRootItemId = GetRootItemId(firstItemId, itemIdParents);
            int secondRootItemId = GetRootItemId(secondItemId, itemIdParents);
            //
            if (firstRootItemId < secondRootItemId) itemIdParents[secondRootItemId] = firstRootItemId;
            else if (secondRootItemId < firstRootItemId) itemIdParents[firstRootItemId] = secondRootItemId;
        }
        private static int GetRootItemId(int itemId, Dictionary<int, int> itemIdParents)
        {
            int parentItemId = itemIdParents[itemId];
            while (parentItemId != itemId)
            {
                itemId = parentItemId;
                parentItemId = itemIdParents[itemId];
            }
            return itemId;
        }
        private static HashSet<int> GetGeometryNodeIds(int geometryId, FeMesh mesh)
        {
            HashSet<int> nodeIds;
            int[] itemTypePartIds = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
            GeometryType geometryType = (GeometryType)itemTypePartIds[1];
            VisualizationData visualization = mesh.GetPartFromId(itemTypePartIds[2]).Visualization;
            //
            if (geometryType == GeometryType.ShellEdgeSurface)
                nodeIds = visualization.GetNodeIdsForEdgeId(itemTypePartIds[0]);
            else if (geometryType == GeometryType.SolidSurface ||
                     geometryType == GeometryType.ShellFrontSurface ||
                     geometryType == GeometryType.ShellBackSurface)
                nodeIds = visualization.GetNodeIdsForSurfaceId(itemTypePartIds[0]);
            else throw new NotSupportedException();
            //
            return nodeIds;
        }
        //
        public List<MasterSlaveItem> GetMasterSlaveItems(bool checkUnresolved)
        {
            List<Graph<NodeData>> connectedSubgraphList = _graph.GetConnectedSubgraphs();
            List<MasterSlaveItem> masterSlaveItems = new List<MasterSlaveItem>();
            //
            foreach (var connectedGraph in connectedSubgraphList)
            {
                if (connectedGraph.IsGraphWithoutCycles())
                {
                    masterSlaveItems.AddRange(GetMasterSlaveItemsFromGraphWithoutCycles(connectedGraph));
                }
                else if (connectedGraph.IsGraphWithOneCycle())
                {
                    masterSlaveItems.AddRange(GetMasterSlaveItemsFromGraphWithOneCycle(connectedGraph));
                }
                else
                {
                    masterSlaveItems.AddRange(GetMasterSlaveItemsFromGraphWithMultipleCycles(connectedGraph, checkUnresolved));
                }
            }
            return masterSlaveItems;
        }
        public List<MasterSlaveItem> GetGroupedMasterSlaveItems(FeMesh mesh)
        {
            List<Graph<NodeData>> connectedSubgraphList = _graph.GetConnectedSubgraphs();
            List<MasterSlaveItem> masterSlaveItems = new List<MasterSlaveItem>();
            List<string> allNames = new List<string>();
            HashSet<Node<NodeData>> firstNodes;
            HashSet<Node<NodeData>> secondNodes;
            //
            foreach (var connectedGraph in connectedSubgraphList)
            {
                // A two colorable graph is resolved by a single master slave item
                if (TryGetBipartiteNodeSets(connectedGraph, out firstNodes, out secondNodes))
                {
                    if (firstNodes.Count == 0 || secondNodes.Count == 0) continue;
                    //
                    if (GetNodeSetSize(firstNodes) >= GetNodeSetSize(secondNodes))
                        AddMasterSlaveItem(firstNodes, secondNodes, masterSlaveItems, allNames, mesh);
                    else
                        AddMasterSlaveItem(secondNodes, firstNodes, masterSlaveItems, allNames, mesh);
                }
                else AddMasterSlaveItemsByNodeSize(connectedGraph, masterSlaveItems, allNames, mesh);
            }
            //
            return masterSlaveItems;
        }
        private static void AddMasterSlaveItemsByNodeSize(Graph<NodeData> graph, List<MasterSlaveItem> masterSlaveItems,
                                                          List<string> allNames, FeMesh mesh)
        {
            // Use the largest surfaces as masters
            List<Node<NodeData>> orderedNodes = new List<Node<NodeData>>(graph.Nodes);
            orderedNodes.Sort(CompareNodesBySize);
            //
            int order = 0;
            Dictionary<Node<NodeData>, int> nodeOrders = new Dictionary<Node<NodeData>, int>();
            foreach (var node in orderedNodes) nodeOrders.Add(node, order++);
            // A node is a slave to all the neighbours ordered before it and is thus a slave only once
            int itemId;
            HashSet<Node<NodeData>> masterNodes;
            List<HashSet<Node<NodeData>>> masterNodesList = new List<HashSet<Node<NodeData>>>();
            List<HashSet<Node<NodeData>>> slaveNodesList = new List<HashSet<Node<NodeData>>>();
            foreach (var node in orderedNodes)
            {
                masterNodes = new HashSet<Node<NodeData>>();
                foreach (var neighbour in node.Neighbours)
                {
                    if (nodeOrders[neighbour] < nodeOrders[node]) masterNodes.Add(neighbour);
                }
                if (masterNodes.Count == 0) continue;
                // Collect the slaves sharing the same masters
                itemId = -1;
                for (int i = 0; i < masterNodesList.Count; i++)
                {
                    if (masterNodesList[i].SetEquals(masterNodes)) { itemId = i; break; }
                }
                if (itemId == -1)
                {
                    masterNodesList.Add(masterNodes);
                    slaveNodesList.Add(new HashSet<Node<NodeData>>() { node });
                }
                else slaveNodesList[itemId].Add(node);
            }
            //
            for (int i = 0; i < masterNodesList.Count; i++)
            {
                AddMasterSlaveItem(masterNodesList[i], slaveNodesList[i], masterSlaveItems, allNames, mesh);
            }
        }
        private static void AddMasterSlaveItem(IEnumerable<Node<NodeData>> masterNodes,
                                               IEnumerable<Node<NodeData>> slaveNodes,
                                               List<MasterSlaveItem> masterSlaveItems, List<string> allNames,
                                               FeMesh mesh)
        {
            HashSet<int> masterItemIds = GetItemIds(masterNodes);
            HashSet<int> slaveItemIds = GetItemIds(slaveNodes);
            //
            string masterName = GetNameFromItemIds(masterItemIds, allNames, mesh);
            allNames.Add(masterName);
            string slaveName = GetNameFromItemIds(slaveItemIds, allNames, mesh);
            allNames.Add(slaveName);
            //
            masterSlaveItems.Add(new MasterSlaveItem(masterName, slaveName, masterItemIds, slaveItemIds));
        }
        private static int CompareNodesBySize(Node<NodeData> first, Node<NodeData> second)
        {
            int result = first.Value.CompareTo(second.Value);
            if (result == 0) result = first.Value.Id.CompareTo(second.Value.Id);
            return result;
        }
        private static bool TryGetBipartiteNodeSets(Graph<NodeData> graph, out HashSet<Node<NodeData>> firstNodes,
                                                    out HashSet<Node<NodeData>> secondNodes)
        {
            bool currentColor;
            bool neighbourColor;
            Queue<Node<NodeData>> queue = new Queue<Node<NodeData>>();
            Dictionary<Node<NodeData>, bool> nodeColors = new Dictionary<Node<NodeData>, bool>();
            firstNodes = new HashSet<Node<NodeData>>();
            secondNodes = new HashSet<Node<NodeData>>();
            //
            foreach (var startNode in graph.Nodes)
            {
                if (nodeColors.ContainsKey(startNode)) continue;
                nodeColors.Add(startNode, false);
                queue.Enqueue(startNode);
                //
                while (queue.Count > 0)
                {
                    Node<NodeData> currentNode = queue.Dequeue();
                    currentColor = nodeColors[currentNode];
                    if (currentColor) secondNodes.Add(currentNode);
                    else firstNodes.Add(currentNode);
                    //
                    foreach (var neighbour in currentNode.Neighbours)
                    {
                        if (nodeColors.TryGetValue(neighbour, out neighbourColor))
                        {
                            if (neighbourColor == currentColor) return false;
                        }
                        else
                        {
                            nodeColors.Add(neighbour, !currentColor);
                            queue.Enqueue(neighbour);
                        }
                    }
                }
            }
            //
            return true;
        }
        private static HashSet<int> GetItemIds(IEnumerable<Node<NodeData>> nodes)
        {
            HashSet<int> itemIds = new HashSet<int>();
            foreach (var node in nodes) itemIds.UnionWith(node.Value.ItemIds);
            return itemIds;
        }
        private static double GetNodeSetSize(IEnumerable<Node<NodeData>> nodes)
        {
            double size = 0;
            foreach (var node in nodes) size += node.Value.Value;
            return size;
        }
        //
        public static string GetNameFromItemIds(HashSet<int> itemIds, List<string> allNames, FeMesh mesh)
        {
            string name;
            HashSet<int> partIds = new HashSet<int>();
            foreach (var itemId in itemIds)
            {
                partIds.Add(FeMesh.GetPartIdFromGeometryId(itemId));
            }
            if (partIds.Count == 1) name = mesh.GetPartNamesFromPartIds(partIds.ToArray())[0];
            else name = allNames.GetNextNumberedKey("Merged");
            //
            return name;
        }
        public static double GetSizeFromItemIds(HashSet<int> itemIds, FeMesh mesh)
        {
            double size = 0;
            foreach (var itemId in itemIds) size += GetSize(itemId, mesh);
            return size;
        }
        private static double GetSize(int geometryId, FeMesh mesh)
        {
            int[] itemTypePartIds = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
            GeometryType geomType = (GeometryType)itemTypePartIds[1];
            VisualizationData vis = mesh.GetPartFromId(itemTypePartIds[2]).Visualization;
            // Face
            if (geomType.IsSurface())
            {
                int faceId = itemTypePartIds[0];
                return vis.FaceAreas[faceId] * 1E6;
            }
            // Edge
            else if (geomType.IsEdge())
            {
                int edgeId = itemTypePartIds[0];
                return vis.EdgeLengths[edgeId];
            }
            // Vertex
            else return 0;
        }
        //
        private static List<MasterSlaveItem> GetMasterSlaveItemsFromGraphWithoutCycles(Graph<NodeData> graph)
        {
            Graph<NodeData> reducedGraph;
            return GetMasterSlaveItemsFromGraphWithoutCycles(graph, out reducedGraph);
        }
        private static List<MasterSlaveItem> GetMasterSlaveItemsFromGraphWithoutCycles(Graph<NodeData> graph,
                                                                                       out Graph<NodeData> reducedGraph)
        {
            Node<NodeData> neighbour;
            List<Node<NodeData>> singleConnectedNodes = new List<Node<NodeData>>();
            List<MasterSlaveItem> masterSlaveItems = new List<MasterSlaveItem>();
            reducedGraph = new Graph<NodeData>(graph);
            //
            do
            {
                singleConnectedNodes.Clear();
                //
                foreach (Node<NodeData> node in reducedGraph.Nodes)
                {
                    if (node.Neighbours.Count() == 1)
                    {
                        neighbour = node.Neighbours[0];
                        masterSlaveItems.Add(new MasterSlaveItem(node.Value.Name, neighbour.Value.Name,
                                                                 node.Value.ItemIds, neighbour.Value.ItemIds));
                        //
                        singleConnectedNodes.Add(node);
                        //
                        break;  // this makes master slave parts more organized for the user
                    }
                }
                //
                foreach (var node in singleConnectedNodes)
                {
                    reducedGraph.Remove(node);
                }
            }
            while (singleConnectedNodes.Count > 0);
            //
            return masterSlaveItems;
        }
        private static List<MasterSlaveItem> GetMasterSlaveItemsFromGraphWithOneCycle(Graph<NodeData> graph)
        {
            Graph<NodeData> reducedGraph;
            List<MasterSlaveItem> masterSlaveItems = GetMasterSlaveItemsFromGraphWithoutCycles(graph, out reducedGraph);
            //
            Node<NodeData> currentNode;
            Node<NodeData> parentNode;
            Queue<Node<NodeData>> queue = new Queue<Node<NodeData>>();
            Queue<Node<NodeData>> parentQueue = new Queue<Node<NodeData>>();
            HashSet<Node<NodeData>> visitedNodes = new HashSet<Node<NodeData>>();
            //
            queue.Enqueue(reducedGraph.Nodes.First());
            parentQueue.Enqueue(reducedGraph.Nodes.First());
            //
            while (queue.Count() > 0)
            {
                currentNode = queue.Dequeue();
                parentNode = parentQueue.Dequeue();
                // Check for cycles
                if (visitedNodes.Add(currentNode))
                {
                    // Add all neighbours to the queue
                    foreach (var neighbour in currentNode.Neighbours)
                    {
                        if (neighbour != parentNode)
                        {
                            masterSlaveItems.Add(new MasterSlaveItem(currentNode.Value.Name, neighbour.Value.Name,
                                                                     currentNode.Value.ItemIds, neighbour.Value.ItemIds));
                            //
                            queue.Enqueue(neighbour);
                            parentQueue.Enqueue(currentNode);
                            break;  // add only the first neighbour and continue
                        }
                    }
                }
            }
            //
            return masterSlaveItems;
        }
        private static List<MasterSlaveItem> GetMasterSlaveItemsFromGraphWithMultipleCycles(Graph<NodeData> graph,
                                                                                            bool checkUnresolved)
        {
            string prefix = "";
            HashSet<int> ids = new HashSet<int>();
            List<MasterSlaveItem> masterSlaveItems = new List<MasterSlaveItem>();
            // Create an unresolved master slave item that is shown to the user as a single surface
            if (checkUnresolved)
            {
                MasterSlaveItem masterSlaveItem;
                foreach (var node in graph.Nodes) ids.UnionWith(node.Value.ItemIds);
                masterSlaveItem = new MasterSlaveItem("Unresolved", "", ids, null);
                masterSlaveItem.Unresolved = true;
                masterSlaveItems.Add(masterSlaveItem);
                //
                prefix = "Unresolved_";
            }
            // Create master slave items
            Graph<NodeData> reducedGraph;
            masterSlaveItems.AddRange(GetMasterSlaveItemsFromGraphWithoutCycles(graph, out reducedGraph));
            //
            foreach (var node in reducedGraph.Nodes)
            {
                foreach (var neighbour in node.Neighbours.ToList())
                {
                    masterSlaveItems.Add(new MasterSlaveItem(prefix + node.Value.Name, neighbour.Value.Name,
                                                             node.Value.ItemIds, neighbour.Value.ItemIds));
                    reducedGraph.RemoveDirectedEdge(neighbour, node);
                }
                node.Neighbours.Clear();
            }
            //
            return masterSlaveItems;
        }
    }
}
