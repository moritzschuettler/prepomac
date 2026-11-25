// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// Places the nodes of a meshed model as mirror images across a mirror plane of it. The nodes of a piece of an
    /// edge (GmshMirrorSplits) are the images of those of its image, one by one along the two; an edge that is split
    /// has the node nearest each split as the end of its pieces there. From the sides of the elements that lie along
    /// such pieces the images of the elements are found, carried from element to element across their sides over the
    /// whole surface mesh of the model, whatever surface they lie on: an element has the element of as many nodes at
    /// the image of its side whose centre is nearest the mirror image of its own, and every node one image. So a
    /// seam, and an edge between two surfaces whose image runs inside a surface, get the nodes of a line of the mesh
    /// there as their images. A surface is mirrored when all its elements have images, its nodes are the images of
    /// their images, and none lies farther from the mirror image of its image than MaxMissShare of the shortest
    /// element side at either of the two: the mesh is mirrored already but for the places of its nodes, and placing
    /// them moves none far. On such a surface, and on an edge all of whose surfaces are such, a node and its image
    /// are put half way between where they lie and the mirror images of each other, a node whose image stays where
    /// it is - the node of a vertex, a node of a surface that is not mirrored - at the mirror image of that node, and
    /// a node that is its own image into the plane. The other surfaces and edges are left as they are meshed: a mesh
    /// laid on sizes that are not mirrored is not made so by moving its nodes. A node that lies where it belongs
    /// already, to PlacedShare of the element side at it, is not touched, so a mesh that is mirrored as it is meshed
    /// keeps its coordinates to the last digit.
    /// Before the surfaces are meshed, the nodes of a split edge are moved along it so that it has a node at every
    /// split, with the number of elements the balance of the counts gave each piece (PlaceSplitNodes).
    /// </summary>
    internal static class GmshMirrorNodes
    {
        // Variables                                                                                                    
        private const double MaxMissShare = 0.5;
        private const double MaxSpacingMissShare = 0.1;
        private const double PlacedShare = 1E-9;
        private const int NumOfLengthSamples = 256;


        // Methods                                                                                                      
        /// <summary>
        /// Whether the nodes the edges are meshed with are mirror images across the plane: every node of an edge lies
        /// within MaxSpacingMissShare of the mean element length of the edge from the mirror image of its image. Only
        /// an edge that is not split and has such an edge of as many nodes as its image is compared; the balance of
        /// the counts evens out the others.
        /// </summary>
        public static bool AreEdgeNodesMirrored(GmshMirrorPlane plane, IEnumerable<int> edgeIds)
        {
            Dictionary<long, long> nodeImage;
            Dictionary<long, double[]> nodeCoor;
            Dictionary<int, long[]> pieceIdTags;
            HashSet<int> mirroredEdgeIds;
            if (!GetEdgeNodeImages(plane, out nodeImage, out nodeCoor, out pieceIdTags, out mirroredEdgeIds))
                return true;
            int imageId;
            foreach (var edgeId in edgeIds)
            {
                if (plane.Splits.IsSplit(edgeId) || !mirroredEdgeIds.Contains(edgeId) ||
                    !plane.PieceIdImageId.TryGetValue(edgeId, out imageId) || imageId < 0) continue;
                long[] tags = pieceIdTags[edgeId];
                if (tags.Length < 3) continue;
                double length;
                Gmsh.Model.OCC.GetMass(1, edgeId, out length);
                double tolerance = MaxSpacingMissShare * length / (tags.Length - 1);
                foreach (var tag in tags)
                {
                    if (GetDistance(plane.Mirror(nodeCoor[tag]), nodeCoor[nodeImage[tag]]) > tolerance) return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Moves the nodes of every split edge along it so that it has a node at each split: the pieces carry the
        /// given numbers of elements, and the nodes of a piece keep their shares of its length. An edge that is not
        /// meshed with as many elements as its pieces carry together is left as it is.
        /// </summary>
        public static void PlaceSplitNodes(GmshMirrorSplits splits, Dictionary<int, int> pieceIdCount)
        {
            IntPtr[] tags;
            double[] coor;
            double[] parameters;
            double[] min;
            double[] max;
            double[] points;
            int count;
            foreach (var edgeId in splits.SplitEdgeIds)
            {
                int[] pieceIds = splits.GetPieceIds(edgeId);
                if (!pieceIds.All(pieceIdCount.ContainsKey)) continue;
                Gmsh.Model.Mesh.GetCurveNodes(edgeId, out tags, out coor, out parameters);
                if (parameters.Length != tags.Length ||
                    tags.Length + 1 != pieceIds.Sum(pieceId => pieceIdCount[pieceId])) continue;
                // The length along the edge as a function of its parameter
                Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                double[] sampleParameters = new double[NumOfLengthSamples + 1];
                double[] sampleLengths = new double[NumOfLengthSamples + 1];
                for (int i = 0; i <= NumOfLengthSamples; i++)
                    sampleParameters[i] = min[0] + (max[0] - min[0]) * i / NumOfLengthSamples;
                Gmsh.Model.GetValue(1, edgeId, sampleParameters, out points);
                if (points.Length != 3 * sampleParameters.Length) continue;
                for (int i = 1; i <= NumOfLengthSamples; i++)
                {
                    int p = 3 * i;
                    sampleLengths[i] = sampleLengths[i - 1] +
                                       Math.Sqrt((points[p] - points[p - 3]) * (points[p] - points[p - 3]) +
                                                 (points[p + 1] - points[p - 2]) * (points[p + 1] - points[p - 2]) +
                                                 (points[p + 2] - points[p - 1]) * (points[p + 2] - points[p - 1]));
                }
                int[] order = Enumerable.Range(0, tags.Length).OrderBy(i => parameters[i]).ToArray();
                double tolerance = PlacedShare * sampleLengths[NumOfLengthSamples] / (tags.Length + 1);
                // The lengths the ends of the pieces lie at, as the edge is meshed and as it is split
                double[] splitParameters = splits.GetSplitParameters(edgeId);
                double[] oldEnds = new double[pieceIds.Length + 1];
                double[] newEnds = new double[pieceIds.Length + 1];
                int[] endNodes = new int[pieceIds.Length + 1];
                endNodes[0] = -1;
                oldEnds[pieceIds.Length] = sampleLengths[NumOfLengthSamples];
                newEnds[pieceIds.Length] = sampleLengths[NumOfLengthSamples];
                for (int k = 1; k < pieceIds.Length; k++)
                {
                    count = pieceIdCount[pieceIds[k - 1]];
                    endNodes[k] = endNodes[k - 1] + count;
                    oldEnds[k] = GetLength(sampleParameters, sampleLengths, parameters[order[endNodes[k]]]);
                    newEnds[k] = GetLength(sampleParameters, sampleLengths, splitParameters[k - 1]);
                }
                endNodes[pieceIds.Length] = tags.Length;
                for (int k = 0; k < pieceIds.Length; k++)
                {
                    if (!(oldEnds[k + 1] > oldEnds[k])) continue;
                    for (int i = endNodes[k] + 1; i <= endNodes[k + 1] && i < tags.Length; i++)
                    {
                        double length = GetLength(sampleParameters, sampleLengths, parameters[order[i]]);
                        length = newEnds[k] + (length - oldEnds[k]) / (oldEnds[k + 1] - oldEnds[k]) *
                                              (newEnds[k + 1] - newEnds[k]);
                        double[] parameter = new double[] { i == endNodes[k + 1] ? splitParameters[k] :
                                                            GetParameter(sampleParameters, sampleLengths, length) };
                        Gmsh.Model.GetValue(1, edgeId, parameter, out points);
                        int c = 3 * order[i];
                        if (points.Length == 3 &&
                            GetDistance(points, new double[] { coor[c], coor[c + 1], coor[c + 2] }) > tolerance)
                            Gmsh.Model.Mesh.SetNode(tags[order[i]], points, parameter);
                    }
                }
            }
        }
        /// <summary>
        /// Returns the surfaces whose elements are the mirror images of elements across the plane, and whose nodes
        /// are placed so.
        /// </summary>
        public static List<int> Place(GmshMirrorPlane plane)
        {
            List<int> mirroredSurfaceIds = new List<int>();
            Dictionary<long, long> nodeImage;
            Dictionary<long, double[]> nodeCoor;
            Dictionary<int, long[]> pieceIdTags;
            HashSet<int> mirroredEdgeIds;
            if (!GetEdgeNodeImages(plane, out nodeImage, out nodeCoor, out pieceIdTags, out mirroredEdgeIds))
                return mirroredSurfaceIds;
            // The elements of all surfaces, and the edge or the surface every node that may be moved lies on
            Tuple<int, int>[] dimTags;
            Gmsh.Model.GetEntities(out dimTags, 2);
            int[] surfaceIds = dimTags.Select(dimTag => dimTag.Item2).OrderBy(id => id).ToArray();
            List<long[]> elements = new List<long[]>();
            Dictionary<int, int[]> surfaceIdElementRange = new Dictionary<int, int[]>();
            Dictionary<int, int[]> surfaceIdEdgeIds = new Dictionary<int, int[]>();
            Dictionary<long, int[]> nodeEntity = new Dictionary<long, int[]>();
            foreach (var surfaceId in surfaceIds)
            {
                int[] edgeIds;
                Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out edgeIds);
                surfaceIdEdgeIds.Add(surfaceId, edgeIds.Select(Math.Abs).Distinct().ToArray());
                List<long[]> surfaceElements = GetElements(surfaceId);
                if (surfaceElements == null || surfaceElements.Count == 0) continue;
                surfaceIdElementRange.Add(surfaceId, new int[] { elements.Count, surfaceElements.Count });
                elements.AddRange(surfaceElements);
                foreach (var node in GetNodeCoor(2, surfaceId, true)) nodeCoor[node.Key] = node.Value;
                foreach (var tag in GetNodeCoor(2, surfaceId, false).Keys) nodeEntity[tag] = new int[] { 2, surfaceId };
            }
            if (elements.Any(element => element.Any(tag => !nodeCoor.ContainsKey(tag)))) return mirroredSurfaceIds;
            int[] elementImage = MatchElements(plane, elements, nodeCoor, nodeImage);
            // The mirrored surfaces
            Dictionary<long, double> nodeLength = GetShortestSides(elements, nodeCoor);
            long imageTag;
            long backTag;
            foreach (var entry in surfaceIdElementRange)
            {
                bool mirrored = true;
                for (int e = entry.Value[0]; e < entry.Value[0] + entry.Value[1] && mirrored; e++)
                {
                    mirrored = elementImage[e] >= 0;
                    foreach (var tag in elements[e])
                    {
                        if (!mirrored) break;
                        mirrored = nodeImage.TryGetValue(tag, out imageTag) &&
                                   nodeImage.TryGetValue(imageTag, out backTag) && backTag == tag &&
                                   GetDistance(plane.Mirror(nodeCoor[tag]), nodeCoor[imageTag]) <=
                                   MaxMissShare * Math.Min(nodeLength[tag], nodeLength[imageTag]);
                    }
                }
                if (mirrored) mirroredSurfaceIds.Add(entry.Key);
            }
            // The nodes that are moved: those inside the mirrored surfaces, and those of the edges all of whose
            // surfaces are mirrored
            HashSet<int> mirroredSurfaceIdSet = new HashSet<int>(mirroredSurfaceIds);
            foreach (var tag in nodeEntity.Where(entry => !mirroredSurfaceIdSet.Contains(entry.Value[1]))
                                          .Select(entry => entry.Key).ToArray()) nodeEntity.Remove(tag);
            HashSet<int> placedEdgeIds = new HashSet<int>(
                surfaceIdEdgeIds.Where(entry => mirroredSurfaceIdSet.Contains(entry.Key))
                                .SelectMany(entry => entry.Value));
            foreach (var entry in surfaceIdEdgeIds)
            {
                if (!mirroredSurfaceIdSet.Contains(entry.Key)) placedEdgeIds.ExceptWith(entry.Value);
            }
            foreach (var edgeId in placedEdgeIds.OrderBy(id => id))
            {
                foreach (var tag in GetNodeCoor(1, edgeId, false).Keys) nodeEntity[tag] = new int[] { 1, edgeId };
            }
            //
            foreach (var tag in nodeEntity.Keys.OrderBy(tag => tag).ToArray())
            {
                if (!nodeImage.TryGetValue(tag, out imageTag)) continue;
                int[] entity = nodeEntity[tag];
                int[] imageEntity;
                double[] coor = nodeCoor[tag];
                double tolerance = PlacedShare * nodeLength[tag];
                if (imageTag == tag) SetNode(entity[0], entity[1], tag, coor, tolerance, plane.Project(coor), true);
                else if (!nodeEntity.TryGetValue(imageTag, out imageEntity))
                    SetNode(entity[0], entity[1], tag, coor, tolerance, plane.Mirror(nodeCoor[imageTag]), false);
                else if (tag < imageTag)
                {
                    double[] imageCoor = plane.Mirror(nodeCoor[imageTag]);
                    double[] middle = new double[] { 0.5 * (coor[0] + imageCoor[0]), 0.5 * (coor[1] + imageCoor[1]),
                                                     0.5 * (coor[2] + imageCoor[2]) };
                    middle = SetNode(entity[0], entity[1], tag, coor, tolerance, middle, true);
                    if (middle != null)
                        SetNode(imageEntity[0], imageEntity[1], imageTag, nodeCoor[imageTag],
                                PlacedShare * nodeLength[imageTag], plane.Mirror(middle), false);
                }
            }
            return mirroredSurfaceIds;
        }
        /// <summary>
        /// The image of every element among the elements, -1 for none, and the images of their nodes added to those
        /// the nodes of the edges have. The match starts at the sides both of whose nodes have images and is carried
        /// across the sides: the image of an element is the element of as many nodes at the image of the side whose
        /// centre is nearest the mirror image of its own, when that gives every node one image and no node two.
        /// </summary>
        private static int[] MatchElements(GmshMirrorPlane plane, List<long[]> elements,
                                           Dictionary<long, double[]> nodeCoor, Dictionary<long, long> nodeImage)
        {
            Dictionary<long, long> imageNode = new Dictionary<long, long>();
            foreach (var entry in nodeImage) imageNode[entry.Value] = entry.Key;
            Dictionary<Tuple<long, long>, List<int>> sideElements = GetSideElements(elements);
            double[][] centres = new double[elements.Count][];
            for (int e = 0; e < elements.Count; e++)
            {
                centres[e] = new double[3];
                foreach (var tag in elements[e])
                {
                    for (int c = 0; c < 3; c++) centres[e][c] += nodeCoor[tag][c] / elements[e].Length;
                }
            }
            int[] elementImage = Enumerable.Repeat(-1, elements.Count).ToArray();
            bool[] taken = new bool[elements.Count];
            Queue<Tuple<int, long, long>> queue = new Queue<Tuple<int, long, long>>();
            foreach (var entry in sideElements.OrderBy(entry => entry.Key.Item1).ThenBy(entry => entry.Key.Item2))
            {
                if (!nodeImage.ContainsKey(entry.Key.Item1) || !nodeImage.ContainsKey(entry.Key.Item2)) continue;
                foreach (var element in entry.Value)
                    queue.Enqueue(new Tuple<int, long, long>(element, entry.Key.Item1, entry.Key.Item2));
            }
            List<int> candidates;
            long known;
            while (queue.Count > 0)
            {
                Tuple<int, long, long> item = queue.Dequeue();
                int element = item.Item1;
                if (elementImage[element] >= 0) continue;
                long[] nodes = elements[element];
                int n = nodes.Length;
                long firstImage = nodeImage[item.Item2];
                long secondImage = nodeImage[item.Item3];
                if (!sideElements.TryGetValue(GetSide(firstImage, secondImage), out candidates)) continue;
                double[] centre = plane.Mirror(centres[element]);
                int image = -1;
                foreach (var candidate in candidates)
                {
                    if (taken[candidate] || elements[candidate].Length != n) continue;
                    if (image < 0 || GetDistance(centres[candidate], centre) < GetDistance(centres[image], centre))
                        image = candidate;
                }
                if (image < 0) continue;
                long[] imageNodes = elements[image];
                int start = Array.IndexOf(nodes, item.Item2);
                int imageStart = Array.IndexOf(imageNodes, firstImage);
                int step = nodes[(start + 1) % n] == item.Item3 ? 1 :
                           nodes[(start + n - 1) % n] == item.Item3 ? n - 1 : 0;
                int imageStep = imageNodes[(imageStart + 1) % n] == secondImage ? 1 :
                                imageNodes[(imageStart + n - 1) % n] == secondImage ? n - 1 : 0;
                if (step == 0 || imageStep == 0) continue;
                bool fits = true;
                for (int k = 0; k < n && fits; k++)
                {
                    long node = nodes[(start + k * step) % n];
                    long nodeOfImage = imageNodes[(imageStart + k * imageStep) % n];
                    fits = nodeImage.TryGetValue(node, out known) ? known == nodeOfImage :
                           !imageNode.ContainsKey(nodeOfImage);
                }
                if (!fits) continue;
                for (int k = 0; k < n; k++)
                {
                    long node = nodes[(start + k * step) % n];
                    long nodeOfImage = imageNodes[(imageStart + k * imageStep) % n];
                    nodeImage[node] = nodeOfImage;
                    imageNode[nodeOfImage] = node;
                }
                elementImage[element] = image;
                taken[image] = true;
                for (int k = 0; k < n; k++)
                {
                    long first = nodes[k];
                    long second = nodes[(k + 1) % n];
                    foreach (var next in sideElements[GetSide(first, second)])
                    {
                        if (elementImage[next] < 0) queue.Enqueue(new Tuple<int, long, long>(next, first, second));
                    }
                }
            }
            return elementImage;
        }
        /// <summary>
        /// The length of the shortest element side at every node.
        /// </summary>
        private static Dictionary<long, double> GetShortestSides(List<long[]> elements,
                                                                 Dictionary<long, double[]> nodeCoor)
        {
            Dictionary<long, double> nodeLength = new Dictionary<long, double>();
            double known;
            foreach (var element in elements)
            {
                for (int k = 0; k < element.Length; k++)
                {
                    long first = element[k];
                    long second = element[(k + 1) % element.Length];
                    double length = GetDistance(nodeCoor[first], nodeCoor[second]);
                    if (!nodeLength.TryGetValue(first, out known) || length < known) nodeLength[first] = length;
                    if (!nodeLength.TryGetValue(second, out known) || length < known) nodeLength[second] = length;
                }
            }
            return nodeLength;
        }
        /// <summary>
        /// The images of the nodes of the vertices and of the pieces of the edges that have images, the coordinates of
        /// the nodes of all vertices and edges, the nodes of every piece of an edge from its start to its end along the
        /// parameter, and the edges all of whose pieces have images of as many nodes. An edge that is split has the
        /// node nearest each split as the end of its pieces there. False when a vertex has no node.
        /// </summary>
        private static bool GetEdgeNodeImages(GmshMirrorPlane plane, out Dictionary<long, long> nodeImage,
                                              out Dictionary<long, double[]> nodeCoor,
                                              out Dictionary<int, long[]> pieceIdTags, out HashSet<int> mirroredEdgeIds)
        {
            nodeImage = new Dictionary<long, long>();
            nodeCoor = new Dictionary<long, double[]>();
            pieceIdTags = new Dictionary<int, long[]>();
            mirroredEdgeIds = new HashSet<int>();
            GmshMirrorSplits splits = plane.Splits;
            if (splits == null) return false;
            Dictionary<int, long> vertexIdTag = new Dictionary<int, long>();
            IntPtr[] tags;
            double[] coor;
            int[] vertexIds;
            int[] edgeIds = plane.EdgeIds;
            foreach (var edgeId in edgeIds)
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length == 1) vertexIds = new int[] { vertexIds[0], vertexIds[0] };
                foreach (var vertexId in vertexIds)
                {
                    if (vertexIdTag.ContainsKey(vertexId)) continue;
                    Gmsh.Model.Mesh.GetNodes(out tags, out coor, 0, vertexId, false, false);
                    if (tags.Length != 1) return false;
                    vertexIdTag.Add(vertexId, tags[0].ToInt64());
                    nodeCoor[tags[0].ToInt64()] = coor;
                }
                SweepQuadWildInput.GetCurveNodes(edgeId, out tags, out coor);
                List<long> edgeTags = new List<long>();
                edgeTags.Add(vertexIdTag[vertexIds[0]]);
                for (int i = 0; i < tags.Length; i++)
                {
                    edgeTags.Add(tags[i].ToInt64());
                    nodeCoor[tags[i].ToInt64()] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
                }
                edgeTags.Add(vertexIdTag[vertexIds[1]]);
                // The ends of the pieces: the nodes nearest the splits, one after another
                int[] pieceIds = splits.GetPieceIds(edgeId);
                double[][] splitPoints = splits.GetSplitPoints(edgeId);
                int[] ends = new int[pieceIds.Length + 1];
                ends[pieceIds.Length] = edgeTags.Count - 1;
                bool found = true;
                for (int k = 1; k < pieceIds.Length && found; k++)
                {
                    int nearest = -1;
                    for (int i = ends[k - 1] + 1; i < edgeTags.Count - 1; i++)
                    {
                        if (nearest < 0 || GetDistance(nodeCoor[edgeTags[i]], splitPoints[k - 1]) <
                                           GetDistance(nodeCoor[edgeTags[nearest]], splitPoints[k - 1])) nearest = i;
                    }
                    found = nearest > 0;
                    ends[k] = nearest;
                }
                if (!found) continue;
                for (int k = 0; k < pieceIds.Length; k++)
                    pieceIdTags.Add(pieceIds[k], edgeTags.GetRange(ends[k], ends[k + 1] - ends[k] + 1).ToArray());
            }
            int imageId;
            foreach (var entry in plane.VertexIdImageId)
            {
                if (vertexIdTag.ContainsKey(entry.Key) && vertexIdTag.ContainsKey(entry.Value))
                    nodeImage[vertexIdTag[entry.Key]] = vertexIdTag[entry.Value];
            }
            long[] pieceTags;
            long[] imageTags;
            foreach (var edgeId in edgeIds)
            {
                bool mirrored = true;
                foreach (var pieceId in splits.GetPieceIds(edgeId))
                {
                    if (!plane.PieceIdImageId.TryGetValue(pieceId, out imageId) ||
                        !pieceIdTags.TryGetValue(pieceId, out pieceTags) ||
                        !pieceIdTags.TryGetValue(imageId, out imageTags) || pieceTags.Length != imageTags.Length)
                    {
                        mirrored = false;
                        continue;
                    }
                    bool reversed = plane.IsReversed(pieceId);
                    int n = pieceTags.Length;
                    for (int i = 0; i < n; i++) nodeImage[pieceTags[i]] = imageTags[reversed ? n - 1 - i : i];
                }
                if (mirrored) mirroredEdgeIds.Add(edgeId);
            }
            return true;
        }
        private static double GetLength(double[] sampleParameters, double[] sampleLengths, double parameter)
        {
            int s = 1;
            while (s < sampleParameters.Length - 1 && sampleParameters[s] < parameter) s++;
            double span = sampleParameters[s] - sampleParameters[s - 1];
            double t = span > 0 ? (parameter - sampleParameters[s - 1]) / span : 0;
            return sampleLengths[s - 1] + t * (sampleLengths[s] - sampleLengths[s - 1]);
        }
        private static double GetParameter(double[] sampleParameters, double[] sampleLengths, double length)
        {
            int s = 1;
            while (s < sampleLengths.Length - 1 && sampleLengths[s] < length) s++;
            double span = sampleLengths[s] - sampleLengths[s - 1];
            double t = span > 0 ? (length - sampleLengths[s - 1]) / span : 0;
            return sampleParameters[s - 1] + t * (sampleParameters[s] - sampleParameters[s - 1]);
        }
        /// <summary>
        /// Moves a node of an edge or a surface to the point of the edge or the surface at the given one; a point that
        /// may lie far off it is projected first. The node is put at the point of its parameters, since the projection
        /// of gmsh ends a millionth of a millimetre off a curved surface, and so does the mirror image of a point of
        /// a part the modelling kernel cut from others. A node that lies within the tolerance of that point is not
        /// moved. Returns where the node lies then, null when its point was not found.
        /// </summary>
        private static double[] SetNode(int dim, int id, long tag, double[] present, double tolerance, double[] coor,
                                        bool project)
        {
            double[] parameters;
            if (project)
            {
                double[] closest;
                Gmsh.Model.GetClosestPoint(dim, id, coor, out closest, out _);
                if (closest.Length == 3) coor = closest;
            }
            Gmsh.Model.GetParametrization(dim, id, coor, out parameters);
            if (parameters.Length != dim) return null;
            double[] onEntity;
            Gmsh.Model.GetValue(dim, id, parameters, out onEntity);
            if (onEntity.Length == 3) coor = onEntity;
            if (GetDistance(present, coor) <= tolerance) return present;
            Gmsh.Model.Mesh.SetNode(new IntPtr(tag), coor, parameters);
            return coor;
        }
        private static Dictionary<long, double[]> GetNodeCoor(int dim, int id, bool withBoundary)
        {
            IntPtr[] tags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out tags, out coor, dim, id, withBoundary, false);
            Dictionary<long, double[]> nodeCoor = new Dictionary<long, double[]>();
            for (int i = 0; i < tags.Length; i++)
                nodeCoor[tags[i].ToInt64()] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
            return nodeCoor;
        }
        private static double GetDistance(double[] a, double[] b)
        {
            return Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) +
                             (a[2] - b[2]) * (a[2] - b[2]));
        }
        /// <summary>
        /// The triangles and quads of a surface by the tags of their nodes; null when it holds another element.
        /// </summary>
        private static List<long[]> GetElements(int surfaceId)
        {
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
            List<long[]> elements = new List<long[]>();
            for (int t = 0; t < elementTypes.Length; t++)
            {
                int numOfNodes = elementTypes[t] == (int)GmshElementType.LinearTriangle ? 3 :
                                 elementTypes[t] == (int)GmshElementType.LinearQuadrilateral ? 4 : 0;
                if (numOfNodes == 0) return null;
                for (int e = 0; e < elementTags[t].Length; e++)
                {
                    long[] element = new long[numOfNodes];
                    for (int n = 0; n < numOfNodes; n++) element[n] = nodeTags[t][e * numOfNodes + n].ToInt64();
                    elements.Add(element);
                }
            }
            return elements;
        }
        private static Dictionary<Tuple<long, long>, List<int>> GetSideElements(List<long[]> elements)
        {
            Dictionary<Tuple<long, long>, List<int>> sideElements = new Dictionary<Tuple<long, long>, List<int>>();
            List<int> side;
            for (int e = 0; e < elements.Count; e++)
            {
                long[] nodes = elements[e];
                for (int k = 0; k < nodes.Length; k++)
                {
                    Tuple<long, long> key = GetSide(nodes[k], nodes[(k + 1) % nodes.Length]);
                    if (!sideElements.TryGetValue(key, out side))
                    {
                        side = new List<int>();
                        sideElements.Add(key, side);
                    }
                    side.Add(e);
                }
            }
            return sideElements;
        }
        private static Tuple<long, long> GetSide(long first, long second)
        {
            return new Tuple<long, long>(Math.Min(first, second), Math.Max(first, second));
        }
    }
}
