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
using CaeGlobals;
using CaeMesh.Sweep;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// A five-faced transfinite volume whose two three-sided caps are meshed as three blocks of quads, like the
    /// three-sided faces of a shell, and filled layer by layer with hexahedra between them. Both caps carry the same
    /// numbers of elements on their sides, which their walls tie together, so their grids join node by node. The walls
    /// are the structured grids gmsh lays over them; a layer of the volume runs through the rows of the walls, and its
    /// inner nodes lie between the caps, moved by what the walls are moved by off the straight lines between the caps.
    /// </summary>
    public class GmshThreeBlockPrism
    {
        // Variables
        private int _volumeId;
        private TransfiniteFace _bottomFace;
        private TransfiniteFace _topFace;
        private int[] _connectingEdgeIds;
        private int[] _wallSurfaceIds;


        // Properties
        public int VolumeId { get { return _volumeId; } }
        public TransfiniteFace BottomFace { get { return _bottomFace; } }
        public TransfiniteFace TopFace { get { return _topFace; } }
        /// <summary>
        /// The edge from corner k of the bottom cap to corner k of the top cap.
        /// </summary>
        public int[] ConnectingEdgeIds { get { return _connectingEdgeIds; } }
        /// <summary>
        /// The wall between side k of the bottom cap and side k of the top cap.
        /// </summary>
        public int[] WallSurfaceIds { get { return _wallSurfaceIds; } }


        // Constructors
        public GmshThreeBlockPrism(int volumeId, TransfiniteFace bottomFace, TransfiniteFace topFace,
                                   int[] connectingEdgeIds, int[] wallSurfaceIds)
        {
            _volumeId = volumeId;
            _bottomFace = bottomFace;
            _topFace = topFace;
            _connectingEdgeIds = connectingEdgeIds;
            _wallSurfaceIds = wallSurfaceIds;
        }


        // Methods
        /// <summary>
        /// The prism of a five-faced volume with the caps and the walls given in gmsh ids, or null when they do not
        /// join as the caps and the walls of a prism. edgeIdVertexIds holds the two vertices of every edge.
        /// </summary>
        public static GmshThreeBlockPrism Create(int volumeId, int bottomSurfaceId, int[] bottomEdgeIds,
                                                 int topSurfaceId, int[] topEdgeIds,
                                                 Dictionary<int, int[]> wallSurfaceIdEdgeIds,
                                                 Dictionary<int, int[]> edgeIdVertexIds)
        {
            int[] bottomCorners;
            int[] bottomSides = GetLoop(bottomEdgeIds, edgeIdVertexIds, out bottomCorners);
            if (bottomSides == null || wallSurfaceIdEdgeIds.Count != 3) return null;
            HashSet<int> capEdgeIds = new HashSet<int>(bottomEdgeIds.Concat(topEdgeIds));
            int[] connectingEdgeIds = wallSurfaceIdEdgeIds.Values.SelectMany(edgeIds => edgeIds)
                                                          .Where(edgeId => !capEdgeIds.Contains(edgeId)).Distinct()
                                                          .ToArray();
            if (connectingEdgeIds.Length != 3) return null;
            // Corner k of the top cap is where the edge from corner k of the bottom cap ends
            int[] orderedConnectingEdgeIds = new int[3];
            int[] topCorners = new int[3];
            for (int k = 0; k < 3; k++)
            {
                int[] edgeIds = connectingEdgeIds.Where(edgeId => edgeIdVertexIds[edgeId].Contains(bottomCorners[k]))
                                                 .ToArray();
                if (edgeIds.Length != 1) return null;
                orderedConnectingEdgeIds[k] = edgeIds[0];
                int[] vertexIds = edgeIdVertexIds[edgeIds[0]];
                topCorners[k] = vertexIds[0] == bottomCorners[k] ? vertexIds[1] : vertexIds[0];
            }
            int[] topSides = new int[3];
            int[] wallSurfaceIds = new int[3];
            for (int k = 0; k < 3; k++)
            {
                int[] edgeIds = topEdgeIds.Where(edgeId => edgeIdVertexIds[edgeId].Contains(topCorners[k]) &&
                                                           edgeIdVertexIds[edgeId].Contains(topCorners[(k + 1) % 3]))
                                          .ToArray();
                if (edgeIds.Length != 1) return null;
                topSides[k] = edgeIds[0];
                int[] wallIds = wallSurfaceIdEdgeIds.Where(entry => entry.Value.Contains(bottomSides[k]) &&
                                                                    entry.Value.Contains(topSides[k]))
                                                    .Select(entry => entry.Key).ToArray();
                if (wallIds.Length != 1) return null;
                wallSurfaceIds[k] = wallIds[0];
            }
            //
            TransfiniteFace bottomFace = new TransfiniteFace(bottomSurfaceId, bottomCorners, new double[3],
                                                             bottomSides.Select(edgeId => new int[] { edgeId }).ToArray());
            TransfiniteFace topFace = new TransfiniteFace(topSurfaceId, topCorners, new double[3],
                                                          topSides.Select(edgeId => new int[] { edgeId }).ToArray());
            return new GmshThreeBlockPrism(volumeId, bottomFace, topFace, orderedConnectingEdgeIds, wallSurfaceIds);
        }
        /// <summary>
        /// The three edges of a cap head to tail, side k running from corner k to corner k + 1, or null when they do
        /// not close one loop of three.
        /// </summary>
        private static int[] GetLoop(int[] edgeIds, Dictionary<int, int[]> edgeIdVertexIds, out int[] cornerIds)
        {
            cornerIds = null;
            if (edgeIds.Length != 3 || edgeIds.Any(edgeId => edgeIdVertexIds[edgeId].Length != 2 ||
                                                           edgeIdVertexIds[edgeId][0] == edgeIdVertexIds[edgeId][1]))
                return null;
            int[] sides = new int[3];
            int[] corners = new int[3];
            sides[0] = edgeIds[0];
            corners[0] = edgeIdVertexIds[sides[0]][0];
            corners[1] = edgeIdVertexIds[sides[0]][1];
            int[] next = edgeIds.Where(edgeId => edgeId != sides[0] && edgeIdVertexIds[edgeId].Contains(corners[1]))
                                .ToArray();
            if (next.Length != 1) return null;
            sides[1] = next[0];
            corners[2] = edgeIdVertexIds[sides[1]][0] == corners[1] ? edgeIdVertexIds[sides[1]][1]
                                                                     : edgeIdVertexIds[sides[1]][0];
            sides[2] = edgeIds.Single(edgeId => edgeId != sides[0] && edgeId != sides[1]);
            if (!edgeIdVertexIds[sides[2]].Contains(corners[2]) || !edgeIdVertexIds[sides[2]].Contains(corners[0]) ||
                corners.Distinct().Count() != 3)
                return null;
            cornerIds = corners;
            return sides;
        }
        /// <summary>
        /// Meshes the caps as three blocks of quads, unless another prism has. capNodeTags and capGrids hold the caps
        /// already meshed, by their surface ids.
        /// </summary>
        public void MeshCaps(Dictionary<int, IntPtr[]> capNodeTags, Dictionary<int, ThreeBlockGrid> capGrids)
        {
            ThreeBlockGrid grid;
            MeshCap(_bottomFace, capNodeTags, capGrids, out grid);
            MeshCap(_topFace, capNodeTags, capGrids, out grid);
        }
        /// <summary>
        /// Meshes the caps, unless another prism has, and fills the volume. capNodeTags and capGrids hold the caps
        /// already meshed, by their surface ids.
        /// </summary>
        public void Build(Dictionary<int, IntPtr[]> capNodeTags, Dictionary<int, ThreeBlockGrid> capGrids)
        {
            ThreeBlockGrid bottomGrid;
            ThreeBlockGrid topGrid;
            IntPtr[] bottomTags = MeshCap(_bottomFace, capNodeTags, capGrids, out bottomGrid);
            IntPtr[] topTags = MeshCap(_topFace, capNodeTags, capGrids, out topGrid);
            int[] s = bottomGrid.SideCounts;
            for (int k = 0; k < 3; k++)
            {
                if (topGrid.SideCounts[k] != s[k])
                    throw new CaeException("The caps Surface " + _bottomFace.FaceId + " and Surface " + _topFace.FaceId +
                                           " carry " + string.Join(", ", s) + " and " +
                                           string.Join(", ", topGrid.SideCounts) + " elements on their sides.");
            }
            // The nodes along the edges between the caps, from the bottom cap
            List<IntPtr>[] columns = new List<IntPtr>[3];
            List<double[]> xyz;
            for (int k = 0; k < 3; k++)
            {
                GmshThreeBlockMesher.GetSideNodes(_wallSurfaceIds[k], _bottomFace.CornerNodeIds[k],
                                                  _topFace.CornerNodeIds[k], new int[] { _connectingEdgeIds[k] },
                                                  out columns[k], out xyz);
            }
            int n = columns[0].Count - 1;
            if (columns.Any(column => column.Count - 1 != n))
                throw new CaeException("The edges " + string.Join(", ", _connectingEdgeIds.Select(id => "Edge " + id)) +
                                       " between the caps of the prism carry different numbers of elements.");
            // The nodes of every wall by row and column
            IntPtr[][,] walls = new IntPtr[3][,];
            for (int k = 0; k < 3; k++)
            {
                IntPtr[] bottomRow = Enumerable.Range(0, s[k] + 1)
                                               .Select(i => bottomTags[bottomGrid.GetBoundaryNodeId(k, i)]).ToArray();
                IntPtr[] topRow = Enumerable.Range(0, s[k] + 1)
                                            .Select(i => topTags[topGrid.GetBoundaryNodeId(k, i)]).ToArray();
                walls[k] = GetWallGrid(_wallSurfaceIds[k], bottomRow, topRow, columns[k].ToArray(),
                                       columns[(k + 1) % 3].ToArray());
            }
            // The coordinates of the nodes on the caps and the walls
            Dictionary<IntPtr, double[]> tagCoor = new Dictionary<IntPtr, double[]>();
            foreach (var surfaceId in _wallSurfaceIds.Concat(new int[] { _bottomFace.FaceId, _topFace.FaceId }))
                AddNodeCoordinates(surfaceId, tagCoor);
            // The tag of grid node g on layer l
            int numOfGridNodes = bottomTags.Length;
            int numOfBoundaryNodes = bottomGrid.NumberOfBoundaryNodes;
            IntPtr[,] tags = new IntPtr[n + 1, numOfGridNodes];
            for (int g = 0; g < numOfGridNodes; g++)
            {
                tags[0, g] = bottomTags[g];
                tags[n, g] = topTags[g];
            }
            int side;
            int index;
            for (int g = 0; g < numOfBoundaryNodes; g++)
            {
                bottomGrid.TryGetBoundaryNode(g, out side, out index);
                for (int l = 1; l < n; l++) tags[l, g] = walls[side][index, l];
            }
            IntPtr maxTag;
            Gmsh.Model.Mesh.GetMaxNodeTag(out maxTag);
            long nodeTag = maxTag.ToInt64();
            int numOfInnerNodes = numOfGridNodes - numOfBoundaryNodes;
            IntPtr[] newTags = new IntPtr[(n - 1) * numOfInnerNodes];
            double[] newCoor = new double[3 * newTags.Length];
            int[][] neighbours = GetNeighbours(numOfGridNodes, bottomGrid.Quads);
            double[] layerFractions = GetLayerFractions(columns, tagCoor);
            double[][] offsets = new double[numOfGridNodes][];
            for (int g = 0; g < numOfGridNodes; g++) offsets[g] = new double[3];
            int count = 0;
            for (int l = 1; l < n; l++)
            {
                double t = layerFractions[l];
                // The walls are moved off the straight lines between the caps by these offsets, which the inner
                // nodes take over smoothly
                for (int g = 0; g < numOfBoundaryNodes; g++)
                {
                    double[] wall = tagCoor[tags[l, g]];
                    double[] bottom = tagCoor[tags[0, g]];
                    double[] top = tagCoor[tags[n, g]];
                    for (int d = 0; d < 3; d++) offsets[g][d] = wall[d] - ((1 - t) * bottom[d] + t * top[d]);
                }
                SolveHarmonicOffsets(offsets, neighbours, numOfBoundaryNodes);
                for (int g = numOfBoundaryNodes; g < numOfGridNodes; g++)
                {
                    tags[l, g] = new IntPtr(++nodeTag);
                    newTags[count] = tags[l, g];
                    double[] bottom = tagCoor[tags[0, g]];
                    double[] top = tagCoor[tags[n, g]];
                    for (int d = 0; d < 3; d++)
                        newCoor[3 * count + d] = (1 - t) * bottom[d] + t * top[d] + offsets[g][d];
                    count++;
                }
            }
            // The hexahedra turn so that the bottom quad looks into the volume
            int[][] quads = bottomGrid.Quads;
            double turning = 0;
            foreach (var quad in quads)
            {
                double[][] bottom = quad.Select(g => tagCoor[tags[0, g]]).ToArray();
                double[] up = new double[3];
                for (int c = 0; c < 4; c++)
                {
                    double[] top = tagCoor[tags[n, quad[c]]];
                    for (int d = 0; d < 3; d++) up[d] += top[d] - bottom[c][d];
                }
                double[] diagonal1 = Subtract(bottom[2], bottom[0]);
                double[] diagonal2 = Subtract(bottom[3], bottom[1]);
                double[] normal = Cross(diagonal1, diagonal2);
                turning += normal[0] * up[0] + normal[1] * up[1] + normal[2] * up[2];
            }
            bool reverse = turning < 0;
            //
            Gmsh.Model.Mesh.GetMaxElementTag(out maxTag);
            long elementTag = maxTag.ToInt64();
            IntPtr[] elementTags = new IntPtr[n * quads.Length];
            IntPtr[] elementNodeTags = new IntPtr[8 * elementTags.Length];
            count = 0;
            for (int l = 0; l < n; l++)
            {
                foreach (var quad in quads)
                {
                    elementTags[count] = new IntPtr(++elementTag);
                    for (int c = 0; c < 4; c++)
                    {
                        int g = quad[reverse ? 3 - c : c];
                        elementNodeTags[8 * count + c] = tags[l, g];
                        elementNodeTags[8 * count + 4 + c] = tags[l + 1, g];
                    }
                    count++;
                }
            }
            if (newTags.Length > 0) Gmsh.Model.Mesh.AddNodes(3, _volumeId, newTags, newCoor);
            Gmsh.Model.Mesh.AddElements(3, _volumeId, new int[] { (int)GmshElementType.LinearHexa },
                                        new IntPtr[][] { elementTags }, new IntPtr[][] { elementNodeTags });
        }
        private static IntPtr[] MeshCap(TransfiniteFace face, Dictionary<int, IntPtr[]> capNodeTags,
                                        Dictionary<int, ThreeBlockGrid> capGrids, out ThreeBlockGrid grid)
        {
            IntPtr[] nodeTags;
            if (capNodeTags.TryGetValue(face.FaceId, out nodeTags))
            {
                grid = capGrids[face.FaceId];
                return nodeTags;
            }
            nodeTags = GmshThreeBlockMesher.Mesh(face, true, out grid);
            capNodeTags[face.FaceId] = nodeTags;
            capGrids[face.FaceId] = grid;
            return nodeTags;
        }
        /// <summary>
        /// The nodes of a wall gmsh meshed as a structured grid of quads, by column i from the first edge between the
        /// caps and row l from the bottom cap: each next node is the corner of the quad across from the three known.
        /// </summary>
        private static IntPtr[,] GetWallGrid(int surfaceId, IntPtr[] bottomRow, IntPtr[] topRow, IntPtr[] firstColumn,
                                             IntPtr[] lastColumn)
        {
            int[] elementTypes;
            IntPtr[][] elementTags;
            IntPtr[][] nodeTags;
            Gmsh.Model.Mesh.GetElements(out elementTypes, out elementTags, out nodeTags, 2, surfaceId);
            Dictionary<Tuple<long, long>, List<IntPtr[]>> edgeQuads = new Dictionary<Tuple<long, long>, List<IntPtr[]>>();
            for (int t = 0; t < elementTypes.Length; t++)
            {
                if (elementTypes[t] != (int)GmshElementType.LinearQuadrilateral)
                    throw new CaeException("Surface " + surfaceId + " is not meshed by a structured grid of quads.");
                for (int e = 0; e < elementTags[t].Length; e++)
                {
                    IntPtr[] quad = new IntPtr[4];
                    Array.Copy(nodeTags[t], 4 * e, quad, 0, 4);
                    for (int c = 0; c < 4; c++)
                    {
                        Tuple<long, long> key = GetEdgeKey(quad[c], quad[(c + 1) % 4]);
                        List<IntPtr[]> list;
                        if (!edgeQuads.TryGetValue(key, out list))
                        {
                            list = new List<IntPtr[]>();
                            edgeQuads.Add(key, list);
                        }
                        list.Add(quad);
                    }
                }
            }
            int numOfColumns = bottomRow.Length;
            int numOfRows = firstColumn.Length;
            IntPtr[,] grid = new IntPtr[numOfColumns, numOfRows];
            for (int i = 0; i < numOfColumns; i++) grid[i, 0] = bottomRow[i];
            for (int l = 0; l < numOfRows; l++) grid[0, l] = firstColumn[l];
            for (int l = 1; l < numOfRows; l++)
            {
                for (int i = 1; i < numOfColumns; i++)
                {
                    IntPtr a = grid[i - 1, l - 1];
                    IntPtr b = grid[i, l - 1];
                    IntPtr c = grid[i - 1, l];
                    List<IntPtr[]> list;
                    IntPtr[] quad = null;
                    if (edgeQuads.TryGetValue(GetEdgeKey(a, b), out list)) quad = list.FirstOrDefault(q => q.Contains(c));
                    if (quad == null)
                        throw new CaeException("Surface " + surfaceId + " is not meshed by a structured grid of " +
                                               "quads between the caps of the prism.");
                    grid[i, l] = quad.First(tag => tag != a && tag != b && tag != c);
                }
            }
            for (int i = 0; i < numOfColumns; i++)
            {
                if (grid[i, numOfRows - 1] != topRow[i])
                    throw new CaeException("The grid of Surface " + surfaceId + " does not end on the top cap.");
            }
            for (int l = 0; l < numOfRows; l++)
            {
                if (grid[numOfColumns - 1, l] != lastColumn[l])
                    throw new CaeException("The grid of Surface " + surfaceId + " does not end on the edge between " +
                                           "the caps.");
            }
            return grid;
        }
        private static Tuple<long, long> GetEdgeKey(IntPtr a, IntPtr b)
        {
            long x = a.ToInt64();
            long y = b.ToInt64();
            return x < y ? new Tuple<long, long>(x, y) : new Tuple<long, long>(y, x);
        }
        private static void AddNodeCoordinates(int surfaceId, Dictionary<IntPtr, double[]> tagCoor)
        {
            IntPtr[] nodeTags;
            double[] coor;
            Gmsh.Model.Mesh.GetNodes(out nodeTags, out coor, 2, surfaceId, true, false);
            for (int i = 0; i < nodeTags.Length; i++)
                tagCoor[nodeTags[i]] = new double[] { coor[3 * i], coor[3 * i + 1], coor[3 * i + 2] };
        }
        /// <summary>
        /// How far along the edges between the caps every layer lies, by length, averaged over the three edges.
        /// </summary>
        private static double[] GetLayerFractions(List<IntPtr>[] columns, Dictionary<IntPtr, double[]> tagCoor)
        {
            int n = columns[0].Count - 1;
            double[] fractions = new double[n + 1];
            foreach (var column in columns)
            {
                double[] lengths = new double[n + 1];
                for (int l = 1; l <= n; l++)
                {
                    double[] segment = Subtract(tagCoor[column[l]], tagCoor[column[l - 1]]);
                    lengths[l] = lengths[l - 1] + Math.Sqrt(segment[0] * segment[0] + segment[1] * segment[1] +
                                                            segment[2] * segment[2]);
                }
                for (int l = 0; l <= n; l++)
                    fractions[l] += (lengths[n] > 0 ? lengths[l] / lengths[n] : (double)l / n) / columns.Length;
            }
            return fractions;
        }
        private static int[][] GetNeighbours(int numOfNodes, int[][] quads)
        {
            SortedSet<int>[] sets = new SortedSet<int>[numOfNodes];
            for (int i = 0; i < numOfNodes; i++) sets[i] = new SortedSet<int>();
            foreach (var quad in quads)
            {
                for (int c = 0; c < 4; c++)
                {
                    sets[quad[c]].Add(quad[(c + 1) % 4]);
                    sets[quad[c]].Add(quad[(c + 3) % 4]);
                }
            }
            return sets.Select(set => set.ToArray()).ToArray();
        }
        /// <summary>
        /// Sets the offsets of the nodes after the boundary ones to the mean of their neighbours, by conjugate
        /// gradients on each coordinate, starting from the offsets they hold.
        /// </summary>
        private static void SolveHarmonicOffsets(double[][] offsets, int[][] neighbours, int numOfBoundaryNodes)
        {
            int n = offsets.Length - numOfBoundaryNodes;
            if (n == 0) return;
            double[] x = new double[n];
            double[] b = new double[n];
            double[] r = new double[n];
            double[] p = new double[n];
            double[] ap = new double[n];
            Action<double[], double[]> multiply = (vector, result) =>
            {
                for (int row = 0; row < n; row++)
                {
                    int[] nodeNeighbours = neighbours[numOfBoundaryNodes + row];
                    double sum = nodeNeighbours.Length * vector[row];
                    foreach (var neighbour in nodeNeighbours)
                    {
                        if (neighbour >= numOfBoundaryNodes) sum -= vector[neighbour - numOfBoundaryNodes];
                    }
                    result[row] = sum;
                }
            };
            for (int d = 0; d < 3; d++)
            {
                double bb = 0;
                for (int row = 0; row < n; row++)
                {
                    x[row] = offsets[numOfBoundaryNodes + row][d];
                    double sum = 0;
                    foreach (var neighbour in neighbours[numOfBoundaryNodes + row])
                    {
                        if (neighbour < numOfBoundaryNodes) sum += offsets[neighbour][d];
                    }
                    b[row] = sum;
                    bb += sum * sum;
                }
                multiply(x, ap);
                double rr = 0;
                for (int row = 0; row < n; row++)
                {
                    r[row] = b[row] - ap[row];
                    p[row] = r[row];
                    rr += r[row] * r[row];
                }
                double limit = 1E-26 * Math.Max(bb, 1E-300);
                for (int iteration = 0; iteration < 10 * n + 100 && rr > limit; iteration++)
                {
                    multiply(p, ap);
                    double pap = 0;
                    for (int row = 0; row < n; row++) pap += p[row] * ap[row];
                    if (!(pap > 0)) break;
                    double alpha = rr / pap;
                    double rrNew = 0;
                    for (int row = 0; row < n; row++)
                    {
                        x[row] += alpha * p[row];
                        r[row] -= alpha * ap[row];
                        rrNew += r[row] * r[row];
                    }
                    double beta = rrNew / rr;
                    for (int row = 0; row < n; row++) p[row] = r[row] + beta * p[row];
                    rr = rrNew;
                }
                for (int row = 0; row < n; row++) offsets[numOfBoundaryNodes + row][d] = x[row];
            }
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        private static double[] Cross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
    }
}
