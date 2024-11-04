using System;
using System.Collections.Generic;
using System.Linq;
using CaeGlobals;
using GmshCommon;

namespace CaeMesh
{
    public static class SweepMethods
    {
        private enum GmshElementTypeEnum
        {
            //Gmsh.Model.Mesh.GetElementProperties(elementType, out dim, out order, out numNodes, out _, out _);
            LinearBeam = 1,
            LinearTriangle = 2,
            LinearQuadrilateral = 3,
            LinearTetra = 4,
            LinearHexa = 5,
            LinearWedge = 6,
            LinearPyramid = 7
        }
        public static void CreateSweepMesh(HashSet<int> sourceSurfaceIds, HashSet<int> sideSurfaceIds,
                                           HashSet<int> targetSurfaceIds, int numLayerSmoothSteps, int numGlobalSmoothSteps,
                                           Dictionary<int, int[]> surfaceIdEdgeIds,
                                           Dictionary<int, int[]> surfaceIdVertexIds)
        {
            // Create sweep lines and their nodes
            Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine;
            Dictionary<IntPtr, double[]> nodeIdCoor;
            HashSet<IntPtr> nodeIdsOfBoundarySweepLines;
            Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours;
            CreateSweepLines(sourceSurfaceIds, sideSurfaceIds, targetSurfaceIds, surfaceIdEdgeIds, surfaceIdVertexIds,
                             out nodeIdSweepLine, out nodeIdsOfBoundarySweepLines, out sweepLineNeighbours, out nodeIdCoor);
            // Smooth sweep lines by z layers
            double avgLayerThickness;
            SmoothSweepLines(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, sweepLineNeighbours,
                             numLayerSmoothSteps, out avgLayerThickness);
            // Project last sweep line node to the target surface
            ProjectSweepLineEndNodesToFaces(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, targetSurfaceIds);
            // Smooth all internal nodes
            SmoothAllGeneratedNodes(nodeIdSweepLine, ref nodeIdCoor, nodeIdsOfBoundarySweepLines, sweepLineNeighbours,
                                    numGlobalSmoothSteps, avgLayerThickness);
            // Project last sweep line node to the target surface
            ProjectSweepLineEndNodesToFaces(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, targetSurfaceIds);
            // Add nodes
            AddNodes(nodeIdSweepLine, nodeIdCoor, nodeIdsOfBoundarySweepLines, targetSurfaceIds);
            // Add elements
            AddElements(nodeIdSweepLine, sourceSurfaceIds, targetSurfaceIds);
        }
        private static void CreateSweepLines(HashSet<int> sourceSurfaceIds, HashSet<int> sideSurfaceIds,
                                             HashSet<int> targetSurfaceIds, Dictionary<int, int[]> surfaceIdEdgeIds,
                                             Dictionary<int, int[]> surfaceIdVertexIds,
                                             out Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                             out HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                             out Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours,
                                             out Dictionary<IntPtr, double[]> nodeIdCoor)
        {
            HashSet<int> sourceEdgeIds = new HashSet<int>();
            HashSet<int> sideEdgeIds = new HashSet<int>();
            HashSet<int> sourceSideBoundaryEdgeIds;
            //
            foreach (var id in sourceSurfaceIds) sourceEdgeIds.UnionWith(surfaceIdEdgeIds[id]);
            foreach (var id in sideSurfaceIds) sideEdgeIds.UnionWith(surfaceIdEdgeIds[id]);
            sourceSideBoundaryEdgeIds = sourceEdgeIds.Intersect(sideEdgeIds).ToHashSet();
            //
            // Side surface sweep lines                                                                     
            //
            // Get side nodes neighbours map
            Dictionary<IntPtr, HashSet<IntPtr>> nodeIdNeighbourIds = GetNodesNeighbours(sideSurfaceIds);
            // Get first nodes of the sweep lines
            IntPtr[] nodeIdsArr;
            nodeIdsOfBoundarySweepLines = new HashSet<IntPtr>();
            foreach (var id in sourceSideBoundaryEdgeIds)
            {
                Gmsh.Model.Mesh.GetNodes(out nodeIdsArr, out _, 1, id, true, false);
                nodeIdsOfBoundarySweepLines.UnionWith(nodeIdsArr);
            }
            // Get sweep lines on side surfaces
            IntPtr firstNodeId;
            IntPtr neighbourId;
            HashSet<IntPtr> currentLayerNodeIds = nodeIdsOfBoundarySweepLines;
            HashSet<IntPtr> prevTwoLayerNodeIds = new HashSet<IntPtr>(nodeIdsOfBoundarySweepLines);
            HashSet<IntPtr> nextLayerNodeIds;
            Dictionary<IntPtr, IntPtr> nodeIdFirstNodeId = new Dictionary<IntPtr, IntPtr>();
            Dictionary<IntPtr, List<IntPtr>> nodeIdSweepLineList = new Dictionary<IntPtr, List<IntPtr>>();
            foreach (var nodeId in nodeIdsOfBoundarySweepLines)
                nodeIdSweepLineList.Add(nodeId, new List<IntPtr>() { nodeId });
            //
            int numOfLayers = 1;
            HashSet<IntPtr> neighbourIds;
            int visitedNodesCount = currentLayerNodeIds.Count;
            //
            while (visitedNodesCount < nodeIdNeighbourIds.Count)
            {
                nextLayerNodeIds = new HashSet<IntPtr>();
                foreach (var nodeId in currentLayerNodeIds)
                {
                    neighbourIds = nodeIdNeighbourIds[nodeId].Except(prevTwoLayerNodeIds).ToHashSet();
                    if (neighbourIds.Count != 1) throw new CaeException("The number of layers in side surfaces is not the same");
                    //
                    neighbourId = neighbourIds.First();
                    // Get first node id of the sweep line
                    if (!nodeIdFirstNodeId.TryGetValue(nodeId, out firstNodeId)) firstNodeId = nodeId;
                    nodeIdFirstNodeId[neighbourId] = firstNodeId;
                    //
                    nodeIdSweepLineList[firstNodeId].Add(neighbourId);
                    //
                    nextLayerNodeIds.Add(neighbourId);
                }
                prevTwoLayerNodeIds = new HashSet<IntPtr>(currentLayerNodeIds);
                prevTwoLayerNodeIds.UnionWith(nextLayerNodeIds);
                //
                currentLayerNodeIds = nextLayerNodeIds;
                //
                numOfLayers++;
                visitedNodesCount += currentLayerNodeIds.Count;
            }
            //
            nodeIdSweepLine = new Dictionary<IntPtr, IntPtr[]>();
            foreach (var entry in nodeIdSweepLineList) nodeIdSweepLine.Add(entry.Key, entry.Value.ToArray());
            //
            // Source surface sweep lines                                                                   
            //
            // Get all node coordinates and max node id
            int currNodeId;
            int maxNodeId = 0;
            double[] coor;
            double[] allCoor;
            nodeIdCoor = new Dictionary<IntPtr, double[]>();
            HashSet<int> allSurfaceIds = sourceSurfaceIds.Union(sideSurfaceIds).ToHashSet();
            foreach (var id in allSurfaceIds)
            {
                Gmsh.Model.Mesh.GetNodes(out nodeIdsArr, out allCoor, 2, id, true, false);
                for (int i = 0; i < nodeIdsArr.Length; i++)
                {
                    coor = new double[3];
                    Array.Copy(allCoor, i * 3, coor, 0, 3);
                    nodeIdCoor[nodeIdsArr[i]] = coor;
                    currNodeId = nodeIdsArr[i].ToInt32();
                    if (currNodeId > maxNodeId) maxNodeId = currNodeId;
                }
            }
            // Get source nodes neighbours map
            nodeIdNeighbourIds = GetNodesNeighbours(sourceSurfaceIds);
            sweepLineNeighbours = nodeIdNeighbourIds;
            //
            int newNodeId = maxNodeId + 1;
            double[] coor1;
            double[] coor2;
            double[] direction;
            double[] avgDirection;
            IntPtr[] sweepLine;
            List<IntPtr[]> sweepLines;
            HashSet<IntPtr> visitedNodeIds = new HashSet<IntPtr>(currentLayerNodeIds);
            //
            currentLayerNodeIds = nodeIdsOfBoundarySweepLines;
            // Go through nodes layer by layer from outside to inside
            while (nodeIdSweepLine.Count != nodeIdNeighbourIds.Count)
            {
                nextLayerNodeIds = new HashSet<IntPtr>();
                foreach (var nodeId in currentLayerNodeIds)
                {
                    nextLayerNodeIds.UnionWith(nodeIdNeighbourIds[nodeId].Except(visitedNodeIds).ToHashSet());
                }
                foreach (var nodeId in nextLayerNodeIds)
                {
                    // If sweep line does not exist
                    if (!nodeIdSweepLine.ContainsKey(nodeId))
                    {
                        // Find neighbouring sweep lines
                        sweepLines = new List<IntPtr[]>();
                        foreach (var neighbourNodeId in nodeIdNeighbourIds[nodeId])
                        {
                            if (nodeIdSweepLine.TryGetValue(neighbourNodeId, out sweepLine)) sweepLines.Add(sweepLine);
                        }
                        // Compute the average positions on the new sweep line
                        if (sweepLines.Count > 0)
                        {
                            sweepLine = new IntPtr[numOfLayers];
                            sweepLine[0] = nodeId; // first node
                            //
                            for (int i = 0; i < numOfLayers - 1; i++)
                            {
                                direction = new double[3];
                                avgDirection = new double[3];
                                for (int j = 0; j < sweepLines.Count; j++)
                                {
                                    coor1 = nodeIdCoor[sweepLines[j][i]];
                                    coor2 = nodeIdCoor[sweepLines[j][i + 1]];
                                    direction[0] = coor2[0] - coor1[0];
                                    direction[1] = coor2[1] - coor1[1];
                                    direction[2] = coor2[2] - coor1[2];
                                    //
                                    avgDirection[0] += direction[0];
                                    avgDirection[1] += direction[1];
                                    avgDirection[2] += direction[2];
                                }
                                avgDirection[0] /= sweepLines.Count;
                                avgDirection[1] /= sweepLines.Count;
                                avgDirection[2] /= sweepLines.Count;
                                //
                                coor1 = nodeIdCoor[sweepLine[i]];
                                coor2 = new double[3];
                                coor2[0] = coor1[0] + avgDirection[0];
                                coor2[1] = coor1[1] + avgDirection[1];
                                coor2[2] = coor1[2] + avgDirection[2];
                                //
                                sweepLine[i + 1] = (IntPtr)newNodeId;
                                nodeIdCoor.Add((IntPtr)newNodeId, coor2);
                                //
                                newNodeId++;
                            }
                            nodeIdSweepLine.Add(nodeId, sweepLine);
                        }
                    }
                }
                //
                visitedNodeIds.UnionWith(nextLayerNodeIds);
                //
                currentLayerNodeIds = nextLayerNodeIds;
            }
        }
        private static void SmoothSweepLines(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine, Dictionary<IntPtr, double[]> nodeIdCoor,
                                             HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                             Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours,
                                             int numSmoothSteps, out double avgLayerThickness)
        {
            double[] coor1;
            double[] coor2;
            double[] direction;
            double[] avgDirection;
            IntPtr[] sweepLine;
            Dictionary<IntPtr, double[]> nodeIdDirection = new Dictionary<IntPtr, double[]>();  // direction to the node
            //
            avgDirection = new double[3];
            foreach (var entry in nodeIdSweepLine)
            {
                sweepLine = entry.Value;
                //
                for (int i = 1; i < sweepLine.Length; i++)
                {
                    coor1 = nodeIdCoor[sweepLine[i - 1]];
                    coor2 = nodeIdCoor[sweepLine[i]];
                    direction = new double[3];
                    direction[0] = coor2[0] - coor1[0];
                    direction[1] = coor2[1] - coor1[1];
                    direction[2] = coor2[2] - coor1[2];
                    nodeIdDirection.Add(sweepLine[i], direction);
                    //
                    avgDirection[0] += direction[0];
                    avgDirection[1] += direction[1];
                    avgDirection[2] += direction[2];
                }
            }
            avgDirection[0] /= nodeIdDirection.Count;
            avgDirection[1] /= nodeIdDirection.Count;
            avgDirection[2] /= nodeIdDirection.Count;
            avgLayerThickness = Math.Sqrt(Math.Pow(avgDirection[0], 2) +
                                          Math.Pow(avgDirection[1], 2) +
                                          Math.Pow(avgDirection[2], 2));
            // Laplacian smoothing of directions
            double delta;
            double maxDelta;
            HashSet<IntPtr> neighbours;
            Dictionary<IntPtr, double[]> smoothNodeIdDirection;
            IntPtr[] internalNodeIds = nodeIdSweepLine.Keys.Except(nodeIdsOfBoundarySweepLines).ToArray();
            //
            for (int i = 0; i < numSmoothSteps; i++) // number of smooth loops
            {
                maxDelta = 0;
                smoothNodeIdDirection = new Dictionary<IntPtr, double[]>();
                foreach (var nodeId in internalNodeIds) // for each sweep line
                {
                    sweepLine = nodeIdSweepLine[nodeId];
                    for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                    {
                        avgDirection = new double[3];
                        neighbours = sweepLineNeighbours[nodeId];
                        //
                        foreach (var neighbourNodeId in neighbours)
                        {
                            direction = nodeIdDirection[nodeIdSweepLine[neighbourNodeId][j]];
                            avgDirection[0] += direction[0];
                            avgDirection[1] += direction[1];
                            avgDirection[2] += direction[2];
                        }
                        avgDirection[0] /= neighbours.Count;
                        avgDirection[1] /= neighbours.Count;
                        avgDirection[2] /= neighbours.Count;
                        //
                        direction = nodeIdDirection[sweepLine[j]];
                        delta = Math.Sqrt(Math.Pow(direction[0] - avgDirection[0], 2) +
                                          Math.Pow(direction[1] - avgDirection[1], 2) +
                                          Math.Pow(direction[2] - avgDirection[2], 2));
                        delta /= avgLayerThickness;
                        if (delta > maxDelta) maxDelta = delta;
                        //
                        smoothNodeIdDirection[sweepLine[j]] = avgDirection;
                    }
                }
                // Copy boundary directions
                foreach (var nodeId in nodeIdsOfBoundarySweepLines)
                {
                    sweepLine = nodeIdSweepLine[nodeId];
                    for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                    {
                        direction = nodeIdDirection[sweepLine[j]];
                        smoothNodeIdDirection[sweepLine[j]] = direction;
                    }
                }
                //
                nodeIdDirection = smoothNodeIdDirection;
                //
                if (maxDelta < 1E-6) break;
            }
            // Apply smoothed directions to coordinates
            foreach (var nodeId in internalNodeIds) // for each sweep line
            {
                sweepLine = nodeIdSweepLine[nodeId];
                for (int j = 1; j < sweepLine.Length; j++)  // for each layer
                {
                    coor1 = nodeIdCoor[sweepLine[j - 1]];
                    direction = nodeIdDirection[sweepLine[j]];
                    coor2 = new double[3];
                    coor2[0] = coor1[0] + direction[0];
                    coor2[1] = coor1[1] + direction[1];
                    coor2[2] = coor1[2] + direction[2];
                    nodeIdCoor[sweepLine[j]] = coor2;
                }
            }
        }
        private static void ProjectSweepLineEndNodesToFaces(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                                            Dictionary<IntPtr, double[]> nodeIdCoor,
                                                            HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                                            HashSet<int> targetSurfaceIds)
        {
            int count;
            int whileCount;
            int numLayers = -1;
            double length = 0;
            double disByDisDer;
            double[] rations;
            double[] t;
            double[][] coor;
            double[] coor1;
            double[] coor2;
            double[] direction = new double[3];
            IntPtr[] sweepLine;
            // Compute the rations of a boundary sweep line
            sweepLine = nodeIdSweepLine[nodeIdsOfBoundarySweepLines.First()];
            numLayers = sweepLine.Length;
            rations = new double[numLayers - 1];
            for (int i = 0; i < numLayers - 1; i++)
            {
                coor1 = nodeIdCoor[sweepLine[i]];
                coor2 = nodeIdCoor[sweepLine[i + 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                rations[i] = Math.Sqrt(Math.Pow(direction[0], 2) + Math.Pow(direction[1], 2) + Math.Pow(direction[2], 2));
                length += rations[i];
            }
            for (int i = 0; i < rations.Length; i++) rations[i] /= length;
            // Use Newtons method to find the surface intersection
            foreach (var entry in nodeIdSweepLine)
            {
                if (nodeIdsOfBoundarySweepLines.Contains(entry.Key)) continue;
                //
                sweepLine = entry.Value;
                coor1 = nodeIdCoor[sweepLine[numLayers - 2]];
                coor2 = nodeIdCoor[sweepLine[numLayers - 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                t = new double[targetSurfaceIds.Count];
                coor = new double[targetSurfaceIds.Count][];
                for (int i = 0; i < t.Length; i++)
                {
                    t[i] = 1;
                    coor[i] = coor2.ToArray();
                }
                //
                whileCount = 0;
                while (whileCount < 10)
                {
                    count = 0;
                    foreach (var targetSurfaceId in targetSurfaceIds)
                    {
                        disByDisDer = DistanceByDistanceDerivative(coor[count], direction, targetSurfaceId);
                        //
                        if (Math.Abs(disByDisDer) < 1E-3)
                        {
                            nodeIdCoor[sweepLine[numLayers - 1]] = coor[count];
                            //
                            //ResetSweepLineLengths(sweepLine, nodeIdCoor, rations);
                            //
                            whileCount = 100;
                            break;
                        }
                        else
                        {
                            t[count] -= disByDisDer;
                            coor[count][0] = coor1[0] + t[count] * direction[0];
                            coor[count][1] = coor1[1] + t[count] * direction[1];
                            coor[count][2] = coor1[2] + t[count] * direction[2];
                        }
                        //
                        count++;
                    }
                    //
                    whileCount++;
                }
            }
        }
        private static void SmoothAllGeneratedNodes(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                                    ref Dictionary<IntPtr, double[]> nodeIdCoor,
                                                    HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                                    Dictionary<IntPtr, HashSet<IntPtr>> sweepLineNeighbours,
                                                    int numSmoothSteps, double avgLayerThickness)
        {
            IntPtr[] sweepLine;
            HashSet<IntPtr> neighbours;
            Dictionary<IntPtr, HashSet<IntPtr>> nodeNeighbours = new Dictionary<IntPtr, HashSet<IntPtr>>();
            foreach (var entry in nodeIdSweepLine)
            {
                if (nodeIdsOfBoundarySweepLines.Contains(entry.Key)) continue;
                //
                sweepLine = entry.Value;
                for (int i = 1; i < sweepLine.Length; i++)  // number of layers
                {
                    neighbours = new HashSet<IntPtr>();
                    foreach (var nodeId in sweepLineNeighbours[entry.Key])
                    {
                        neighbours.Add(nodeIdSweepLine[nodeId][i]);
                    }
                    neighbours.Add(sweepLine[i - 1]);
                    if (i < sweepLine.Length - 1) neighbours.Add(sweepLine[i + 1]);
                    //
                    nodeNeighbours.Add(sweepLine[i], neighbours);
                }
            }
            HashSet<IntPtr> boundaryNodes = nodeIdCoor.Keys.Except(nodeNeighbours.Keys).ToHashSet();
            // Laplacian smoothing of coordinates
            double delta;
            double maxDelta;
            double[] coor;
            double[] avgCoor;
            Dictionary<IntPtr, double[]> smoothNodeIdCoor;
            //
            for (int i = 0; i < numSmoothSteps; i++) // number of smooth loops
            {
                maxDelta = 0;
                smoothNodeIdCoor = new Dictionary<IntPtr, double[]>();
                foreach (var entry in nodeNeighbours)
                {
                    avgCoor = new double[3];
                    neighbours = entry.Value;
                    //
                    foreach (var neighbourNodeId in neighbours)
                    {
                        coor = nodeIdCoor[neighbourNodeId];
                        avgCoor[0] += coor[0];
                        avgCoor[1] += coor[1];
                        avgCoor[2] += coor[2];
                    }
                    avgCoor[0] /= neighbours.Count;
                    avgCoor[1] /= neighbours.Count;
                    avgCoor[2] /= neighbours.Count;
                    //
                    coor = nodeIdCoor[entry.Key];
                    delta = Math.Sqrt(Math.Pow(avgCoor[0] - coor[0], 2) +
                                      Math.Pow(avgCoor[1] - coor[1], 2) +
                                      Math.Pow(avgCoor[2] - coor[2], 2));
                    delta /= avgLayerThickness;
                    if (delta > maxDelta) maxDelta = delta;
                    //
                    smoothNodeIdCoor[entry.Key] = avgCoor;
                }
                // Copy boundary coordinates
                foreach (var nodeId in boundaryNodes)
                {
                    coor = nodeIdCoor[nodeId];
                    smoothNodeIdCoor[nodeId] = coor;
                }
                //
                nodeIdCoor = smoothNodeIdCoor;
                //
                if (maxDelta < 1E-6) break;
            }
        }
        private static void AddNodes(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                     Dictionary<IntPtr, double[]> nodeIdCoor,
                                     HashSet<IntPtr> nodeIdsOfBoundarySweepLines,
                                     HashSet<int> targetSurfaceIds)
        {
            foreach (var entry in nodeIdSweepLine)
            {
                if (!nodeIdsOfBoundarySweepLines.Contains(entry.Key))
                {
                    for (int i = 1; i < entry.Value.Length; i++)
                    {
                        if (i == entry.Value.Length - 1)
                            Gmsh.Model.Mesh.AddNodes(2, targetSurfaceIds.First(), new IntPtr[1] { entry.Value[i] }, nodeIdCoor[entry.Value[i]]);
                        else
                            Gmsh.Model.Mesh.AddNodes(3, 1, new IntPtr[1] { entry.Value[i] }, nodeIdCoor[entry.Value[i]]);
                    }
                }
            }
        }
        private static void AddElements(Dictionary<IntPtr, IntPtr[]> nodeIdSweepLine,
                                        HashSet<int> sourceSurfaceIds,
                                        HashSet<int> targetSurfaceIds)
        {
            // Get max element id
            int currElementId;
            int maxElementId = 0;
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, -1, -1);
            for (int i = 0; i < elementTags.Length; i++)
            {
                for (int j = 0; j < elementTags[i].Length; j++)
                {
                    currElementId = elementTags[i][j].ToInt32();
                    if (currElementId > maxElementId) maxElementId = currElementId;
                }
            }
            //IntPtr maxTag;
            //Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            //
            int numNodes;
            int newElementId = maxElementId + 1;
            int numLayers = nodeIdSweepLine.First().Value.Length - 1;
            IntPtr[] nodeIdsArr;
            IntPtr[] solidNodeIdsArr;
            Dictionary<IntPtr, IntPtr[]> linearTriangles = new Dictionary<IntPtr, IntPtr[]>();
            Dictionary<IntPtr, IntPtr[]> linearQuadrilaterals = new Dictionary<IntPtr, IntPtr[]>();
            Dictionary<IntPtr, IntPtr[]> linearWedges = new Dictionary<IntPtr, IntPtr[]>();
            Dictionary<IntPtr, IntPtr[]> LinearHexas = new Dictionary<IntPtr, IntPtr[]>();
            foreach (var id in sourceSurfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, id);
                //
                for (int i = 0; i < numLayers; i++) // layer id
                {
                    for (int j = 0; j < elementTags.Length; j++) // element type id
                    {
                        if (elementTypes[j] == (int)GmshElementTypeEnum.LinearTriangle) numNodes = 3;
                        else if (elementTypes[j] == (int)GmshElementTypeEnum.LinearQuadrilateral) numNodes = 4;
                        else throw new NotImplementedException();
                        //
                        for (int k = 0; k < elementTags[j].Length; k++) // element id
                        {
                            nodeIdsArr = new IntPtr[numNodes];
                            Array.Copy(nodeTags[j], k * numNodes, nodeIdsArr, 0, numNodes);
                            //
                            if (numNodes == 3)
                            {
                                solidNodeIdsArr = new IntPtr[6];
                                solidNodeIdsArr[0] = nodeIdSweepLine[nodeIdsArr[0]][i + 1];
                                solidNodeIdsArr[1] = nodeIdSweepLine[nodeIdsArr[1]][i + 1];
                                solidNodeIdsArr[2] = nodeIdSweepLine[nodeIdsArr[2]][i + 1];
                                solidNodeIdsArr[3] = nodeIdSweepLine[nodeIdsArr[0]][i];
                                solidNodeIdsArr[4] = nodeIdSweepLine[nodeIdsArr[1]][i];
                                solidNodeIdsArr[5] = nodeIdSweepLine[nodeIdsArr[2]][i];
                                //
                                linearWedges.Add((IntPtr)newElementId++, solidNodeIdsArr);
                                //
                                if (i == numLayers - 1)
                                {
                                    solidNodeIdsArr = new IntPtr[3];
                                    solidNodeIdsArr[0] = nodeIdSweepLine[nodeIdsArr[0]][i + 1];
                                    solidNodeIdsArr[1] = nodeIdSweepLine[nodeIdsArr[2]][i + 1];
                                    solidNodeIdsArr[2] = nodeIdSweepLine[nodeIdsArr[1]][i + 1];
                                    //
                                    linearTriangles.Add((IntPtr)newElementId++, solidNodeIdsArr);
                                }
                            }
                            else if (numNodes == 4)
                            {
                                solidNodeIdsArr = new IntPtr[8];
                                solidNodeIdsArr[0] = nodeIdSweepLine[nodeIdsArr[0]][i + 1];
                                solidNodeIdsArr[1] = nodeIdSweepLine[nodeIdsArr[1]][i + 1];
                                solidNodeIdsArr[2] = nodeIdSweepLine[nodeIdsArr[2]][i + 1];
                                solidNodeIdsArr[3] = nodeIdSweepLine[nodeIdsArr[3]][i + 1];
                                solidNodeIdsArr[4] = nodeIdSweepLine[nodeIdsArr[0]][i];
                                solidNodeIdsArr[5] = nodeIdSweepLine[nodeIdsArr[1]][i];
                                solidNodeIdsArr[6] = nodeIdSweepLine[nodeIdsArr[2]][i];
                                solidNodeIdsArr[7] = nodeIdSweepLine[nodeIdsArr[3]][i];
                                //
                                LinearHexas.Add((IntPtr)newElementId++, solidNodeIdsArr);
                                //
                                if (i == numLayers - 1)
                                {
                                    solidNodeIdsArr = new IntPtr[4];
                                    solidNodeIdsArr[0] = nodeIdSweepLine[nodeIdsArr[0]][i + 1];
                                    solidNodeIdsArr[1] = nodeIdSweepLine[nodeIdsArr[3]][i + 1];
                                    solidNodeIdsArr[2] = nodeIdSweepLine[nodeIdsArr[2]][i + 1];
                                    solidNodeIdsArr[3] = nodeIdSweepLine[nodeIdsArr[1]][i + 1];
                                    //
                                    linearQuadrilaterals.Add((IntPtr)newElementId++, solidNodeIdsArr);
                                }
                            }
                            else throw new NotSupportedException();
                        }
                    }
                }
            }
            // Add elements by single type
            AddElemets(linearTriangles, 3, 2, targetSurfaceIds.First(), (int)GmshElementTypeEnum.LinearTriangle);
            AddElemets(linearQuadrilaterals, 4, 2, targetSurfaceIds.First(), (int)GmshElementTypeEnum.LinearQuadrilateral);
            AddElemets(linearWedges, 6, 3, 1, (int)GmshElementTypeEnum.LinearWedge);
            AddElemets(LinearHexas, 8, 3, 1, (int)GmshElementTypeEnum.LinearHexa);
        }
        private static void AddElemets(Dictionary<IntPtr, IntPtr[]> elements, int n, int dim, int tag, int elementType)
        {
            int count;
            IntPtr[][] elementIds;
            IntPtr[][] nodeIds;
            if (elements.Count > 0)
            {
                count = 0;
                elementIds = new IntPtr[1][];
                elementIds[0] = new IntPtr[elements.Count];
                nodeIds = new IntPtr[1][];
                nodeIds[0] = new IntPtr[n * elements.Count];
                foreach (var entry in elements)
                {
                    elementIds[0][count] = entry.Key;
                    Array.Copy(entry.Value, 0, nodeIds[0], n * count, n);
                    count++;
                }
                Gmsh.Model.Mesh.AddElements(dim, tag, new int[] { elementType }, elementIds, nodeIds);
            }
        }
        private static void ResetSweepLineLengths(IntPtr[] sweepLine, Dictionary<IntPtr, double[]> nodeIdCoor, double[] rations)
        {
            int numLayers = sweepLine.Length;
            double length = 0;
            double[] lengths = new double[numLayers - 1];
            double[] coor1;
            double[] coor2;
            double[] direction = new double[3];
            //
            for (int i = 0; i < numLayers - 1; i++)
            {
                coor1 = nodeIdCoor[sweepLine[i]];
                coor2 = nodeIdCoor[sweepLine[i + 1]];
                direction[0] = coor2[0] - coor1[0];
                direction[1] = coor2[1] - coor1[1];
                direction[2] = coor2[2] - coor1[2];
                //
                lengths[i] = Math.Sqrt(Math.Pow(direction[0], 2) + Math.Pow(direction[1], 2) + Math.Pow(direction[2], 2));
                length += rations[i];
            }
            //
            double t;
            double newLength = 0;
            length = 0;
            for (int i = 0; i < numLayers - 1; i++)
            {
                length += lengths[i];
                newLength += rations[i] * length;

                if (length > newLength)
                {
                    t = 1 - ((length - newLength) / lengths[i]);
                    //
                    coor1 = nodeIdCoor[sweepLine[i]];
                    coor2 = nodeIdCoor[sweepLine[i + 1]];
                    direction[0] = coor2[0] - coor1[0];
                    direction[1] = coor2[1] - coor1[1];
                    direction[2] = coor2[2] - coor1[2];
                }
            }
        }
        private static double DistanceByDistanceDerivative(double[] coor, double[] direction, int surfaceId)
        {
            double epsilon = 1E-6;
            double distance1;
            double distance2;
            //
            int point1Id = Gmsh.Model.OCC.AddPoint(coor[0], coor[1], coor[2]);
            int point2Id = Gmsh.Model.OCC.AddPoint(coor[0] + epsilon * direction[0],
                                                   coor[1] + epsilon * direction[1],
                                                   coor[2] + epsilon * direction[2]);
            //
            Gmsh.Model.OCC.GetDistance(0, point1Id, 2, surfaceId, out distance1);
            Gmsh.Model.OCC.GetDistance(0, point2Id, 2, surfaceId, out distance2);
            //
            double derivative;
            double disByDisDer;
            derivative = (distance2 - distance1) / epsilon;
            disByDisDer = distance1 / derivative;
            //
            return disByDisDer;
        }

        private static Dictionary<IntPtr, HashSet<IntPtr>> GetNodesNeighbours(IEnumerable<int> surfaceIds)
        {
            // Get all elements on the surfaces
            int numNodes;
            IntPtr[] nodeIdsArr;
            Dictionary<IntPtr, IntPtr[]> elementIdNodeIds = new Dictionary<IntPtr, IntPtr[]>();
            foreach (var id in surfaceIds)
            {
                Gmsh.Model.Mesh.GetElements(out int[] elementTypes, out IntPtr[][] elementTags, out IntPtr[][] nodeTags, 2, id);
                //
                for (int i = 0; i < elementTags.Length; i++)
                {
                    Gmsh.Model.Mesh.GetElementProperties(elementTypes[i], out _, out _, out numNodes, out _, out _);
                    //
                    for (int j = 0; j < elementTags[i].Length; j++)
                    {
                        nodeIdsArr = new IntPtr[numNodes];
                        Array.Copy(nodeTags[i], j * numNodes, nodeIdsArr, 0, numNodes);
                        elementIdNodeIds.Add(elementTags[i][j], nodeIdsArr);
                    }
                }
            }
            // Get node neighbours map
            int delta;
            int numOfNodes;
            HashSet<IntPtr> neighbourIds;
            Dictionary<IntPtr, HashSet<IntPtr>> nodeIdNeighbourIds = new Dictionary<IntPtr, HashSet<IntPtr>>();
            foreach (var entry in elementIdNodeIds)
            {
                numOfNodes = entry.Value.Length;
                if (numOfNodes == 3) delta = 1;
                else if (numOfNodes == 4) delta = 2;
                else throw new NotSupportedException();
                //
                for (int i = 0; i < numOfNodes; i++)
                {
                    if (!nodeIdNeighbourIds.TryGetValue(entry.Value[i], out neighbourIds))
                    {
                        neighbourIds = new HashSet<IntPtr>();
                        nodeIdNeighbourIds.Add(entry.Value[i], neighbourIds);
                    }
                    neighbourIds.Add(entry.Value[(i + 1) % numOfNodes]);
                    neighbourIds.Add(entry.Value[(i + 1 + delta) % numOfNodes]);
                }
            }
            //
            return nodeIdNeighbourIds;
        }
    }
}
