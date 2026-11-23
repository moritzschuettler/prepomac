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
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// A plane the meshed part is its own mirror image across (GmshMirrorDetector): a surface of the gmsh model has a
    /// surface as its image, unless it is split unlike it, and a piece of an edge a piece (GmshMirrorSplits), unless
    /// its image runs inside a surface; an edge that is split like its image is its one piece. A piece is reversed
    /// when its image runs the other way along the parameter of its edge; a piece that is its own image and reversed
    /// crosses the plane, and one that is its own image and not reversed lies in it.
    /// The plane asks the balance of the element counts for what a mirrored mesh needs: the same number of elements on
    /// a piece and on its image, and, where it cuts a surface, a node or the middle of an element at both ends of every
    /// part of the cut, since a structured grid meets the plane along a line of its nodes or along the middle of a row
    /// of its quads.
    /// </summary>
    public class GmshMirrorPlane
    {
        // Variables                                                                                                    
        private const double CollinearTolerance = 1E-6;
        private const int NumOfBisections = 60;
        private readonly double[] _point;
        private readonly double[] _normal;
        private readonly double _size;
        private readonly Dictionary<int, int> _surfaceIdImageId;
        private GmshMirrorSplits _splits;
        private int[] _edgeIds;
        private Dictionary<int, int> _vertexIdImageId;
        private Dictionary<int, int> _pieceIdImageId;
        private HashSet<int> _reversedPieceIds;


        // Properties                                                                                                   
        public double[] Normal { get { return _normal; } }
        public Dictionary<int, int> SurfaceIdImageId { get { return _surfaceIdImageId; } }
        public GmshMirrorSplits Splits { get { return _splits; } }
        /// <summary>The edges of the model.</summary>
        public int[] EdgeIds { get { return _edgeIds; } }
        /// <summary>The vertices that have a vertex as their image.</summary>
        public Dictionary<int, int> VertexIdImageId { get { return _vertexIdImageId; } }
        public Dictionary<int, int> PieceIdImageId { get { return _pieceIdImageId; } }


        // Constructors                                                                                                 
        public GmshMirrorPlane(double[] point, double[] normal, double size, Dictionary<int, int> surfaceIdImageId)
        {
            _point = point;
            _normal = normal;
            _size = size;
            _surfaceIdImageId = surfaceIdImageId;
            _edgeIds = new int[0];
            _vertexIdImageId = new Dictionary<int, int>();
            _pieceIdImageId = new Dictionary<int, int>();
            _reversedPieceIds = new HashSet<int>();
        }


        // Methods                                                                                                      
        /// <summary>
        /// The splits of the edges for all planes of the model, its edges, and the images of its vertices and pieces
        /// across this one; a piece whose image runs inside a surface has none.
        /// </summary>
        public void SetPieces(GmshMirrorSplits splits, int[] edgeIds, Dictionary<int, int> vertexIdImageId,
                              Dictionary<int, int> pieceIdImageId, HashSet<int> reversedPieceIds)
        {
            _splits = splits;
            _edgeIds = edgeIds;
            _vertexIdImageId = vertexIdImageId;
            _pieceIdImageId = pieceIdImageId;
            _reversedPieceIds = reversedPieceIds;
        }
        public double GetDistance(double[] point)
        {
            return (point[0] - _point[0]) * _normal[0] + (point[1] - _point[1]) * _normal[1] +
                   (point[2] - _point[2]) * _normal[2];
        }
        public double[] Mirror(double[] point)
        {
            double distance = 2 * GetDistance(point);
            return new double[] { point[0] - distance * _normal[0], point[1] - distance * _normal[1],
                                  point[2] - distance * _normal[2] };
        }
        public double[] Project(double[] point)
        {
            double distance = GetDistance(point);
            return new double[] { point[0] - distance * _normal[0], point[1] - distance * _normal[1],
                                  point[2] - distance * _normal[2] };
        }
        public bool IsReversed(int pieceId)
        {
            return _reversedPieceIds.Contains(pieceId);
        }
        /// <summary>
        /// The pairs of a piece and its image on the given edges, each pair once, as the two sides of a row of equal
        /// sums.
        /// </summary>
        public void AddEqualCounts(ICollection<int> edgeIds, MirrorCountRows rows)
        {
            int imageId;
            HashSet<long> added = new HashSet<long>();
            foreach (var edgeId in edgeIds.OrderBy(id => id))
            {
                foreach (var pieceId in _splits.GetPieceIds(edgeId))
                {
                    if (!_pieceIdImageId.TryGetValue(pieceId, out imageId) || imageId == pieceId ||
                        !edgeIds.Contains(_splits.GetEdgeId(imageId))) continue;
                    long pair = ((long)Math.Min(pieceId, imageId) << 32) ^ (uint)Math.Max(pieceId, imageId);
                    if (added.Add(pair))
                        rows.EqualSides.Add(new int[][] { new int[] { pieceId }, new int[] { imageId } });
                }
            }
        }
        /// <summary>
        /// The pieces of the edges of a surface that is its own image whose sums must be even for a grid of the
        /// surface to be mirrored. The plane meets the boundary of the surface at its vertices in the plane and where
        /// pieces of its edges cross the plane; these points lie on one line and, sorted along it, bound the parts of
        /// the cut two by two. A part between a vertex and a piece needs a node in the middle of the piece, an even
        /// number of elements; one between two pieces an even sum of their numbers, a node in the middle of both or of
        /// neither. With nodesOnPlane every piece needs a node where it crosses the plane, as on a face whose grid has
        /// inner lines that end at nodes. Nothing is added for a surface with a piece in the plane, with crossings that
        /// are not on one line, or for a piece of an edge that is not among the given ones.
        /// </summary>
        public void AddCrossingCounts(int surfaceId, bool nodesOnPlane, ICollection<int> edgeIds, MirrorCountRows rows)
        {
            List<double[]> points;
            List<int> pointPieceIds;
            if (!GetCrossings(surfaceId, out points, out pointPieceIds)) return;
            if (nodesOnPlane)
            {
                foreach (var pieceId in pointPieceIds.Distinct())
                {
                    if (pieceId != 0 && edgeIds.Contains(_splits.GetEdgeId(pieceId)))
                        rows.MultipleSides.Add(new Tuple<int[], int>(new int[] { pieceId }, 2));
                }
                return;
            }
            if (points.Count < 2 || points.Count % 2 != 0) return;
            // The line through the two crossings farthest apart
            int start = GetFarthest(points, points[0]);
            int end = GetFarthest(points, points[start]);
            double[] direction = new double[3];
            double length = 0;
            for (int c = 0; c < 3; c++)
            {
                direction[c] = points[end][c] - points[start][c];
                length += direction[c] * direction[c];
            }
            length = Math.Sqrt(length);
            if (!(length > CollinearTolerance * _size)) return;
            for (int c = 0; c < 3; c++) direction[c] /= length;
            double[] positions = new double[points.Count];
            for (int p = 0; p < points.Count; p++)
            {
                double offset = 0;
                for (int c = 0; c < 3; c++) positions[p] += (points[p][c] - points[start][c]) * direction[c];
                for (int c = 0; c < 3; c++)
                {
                    double off = points[p][c] - points[start][c] - positions[p] * direction[c];
                    offset += off * off;
                }
                if (Math.Sqrt(offset) > CollinearTolerance * _size) return;
            }
            int[] order = Enumerable.Range(0, points.Count).OrderBy(p => positions[p]).ToArray();
            for (int p = 0; p + 1 < order.Length; p += 2)
            {
                int first = pointPieceIds[order[p]];
                int second = pointPieceIds[order[p + 1]];
                int[] side = new int[] { first, second }.Where(id => id != 0).Distinct().ToArray();
                if (side.Length == 0 || (side.Length == 1 && first == second)) continue;
                if (side.All(pieceId => edgeIds.Contains(_splits.GetEdgeId(pieceId))))
                    rows.MultipleSides.Add(new Tuple<int[], int>(side, 2));
            }
        }
        /// <summary>
        /// The edges of the round holes of a submapped face that is its own image whose sum must be a multiple of
        /// eight. A round hole is a square of a quarter of its elements on each side in the lattice of the face, and
        /// where the plane runs along the lattice and through the hole, the corners of the square are mirrored when
        /// the plane meets the middle of its sides at nodes or at the middle of elements as it meets the circle: at a
        /// vertex or a split with an even quarter of the elements, a multiple of eight in all, and in the middle of a piece with
        /// the sum of its number and of a quarter of all even.
        /// </summary>
        public void AddRoundHoleCounts(SubmapFace face, ICollection<int> edgeIds, MirrorCountRows rows)
        {
            int imageId;
            if (_splits == null || !face.HasHoles || !_surfaceIdImageId.TryGetValue(face.FaceId, out imageId) ||
                imageId != face.FaceId) return;
            // The plane runs along the lattice when an edge of the outer loop and its image run along the same
            // direction of it, and along its diagonal when they run across each other
            bool alongLattice = false;
            for (int k = 0; k < face.EdgeIds.Length; k++)
            {
                if (face.GetEdgeTurn(k) != 0) continue;
                int pieceId = _splits.GetPieceIds(face.EdgeIds[k])[0];
                if (!_pieceIdImageId.TryGetValue(pieceId, out imageId)) continue;
                int imageIndex = Array.IndexOf(face.EdgeIds, _splits.GetEdgeId(imageId));
                if (imageIndex < 0 || face.GetEdgeTurn(imageIndex) != 0) continue;
                if ((face.EdgeDirections[k] - face.EdgeDirections[imageIndex]) % 2 != 0) return;
                alongLattice = true;
                break;
            }
            if (!alongLattice) return;
            int[] vertexIds;
            foreach (var hole in face.Holes)
            {
                if (!hole.IsRound || !hole.EdgeIds.All(edgeIds.Contains)) continue;
                int[] pieceIds = hole.EdgeIds.SelectMany(edgeId => _splits.GetPieceIds(edgeId)).ToArray();
                if (!pieceIds.All(pieceId => _pieceIdImageId.TryGetValue(pieceId, out imageId) &&
                                             hole.EdgeIds.Contains(_splits.GetEdgeId(imageId)))) continue;
                bool atNode = false;
                foreach (var edgeId in hole.EdgeIds)
                {
                    Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                    atNode |= vertexIds.Any(vertexId => _vertexIdImageId.TryGetValue(vertexId, out imageId) &&
                                                        imageId == vertexId) ||
                              GetSplitPointsInPlane(edgeId).Any();
                }
                List<int> side = new List<int>(hole.EdgeIds);
                if (!atNode)
                {
                    int[] crossingIds = pieceIds.Where(pieceId => _pieceIdImageId[pieceId] == pieceId &&
                                                                  _reversedPieceIds.Contains(pieceId)).ToArray();
                    if (crossingIds.Length == 0) continue;
                    for (int i = 0; i < 4; i++) side.Add(crossingIds[0]);
                }
                rows.MultipleSides.Add(new Tuple<int[], int>(side.ToArray(), 8));
            }
        }
        /// <summary>
        /// Where the plane meets the boundary of a surface that is its own image: the points, each with the piece that
        /// crosses the plane there or 0 for a vertex or a split of an edge in the plane. False for a surface that is
        /// not its own image or has a piece in the plane.
        /// </summary>
        private bool GetCrossings(int surfaceId, out List<double[]> points, out List<int> pointPieceIds)
        {
            points = new List<double[]>();
            pointPieceIds = new List<int>();
            int imageId;
            if (_splits == null || !_surfaceIdImageId.TryGetValue(surfaceId, out imageId) || imageId != surfaceId)
                return false;
            int[] surfaceEdgeIds;
            Gmsh.Model.GetAdjacencies(2, surfaceId, out _, out surfaceEdgeIds);
            HashSet<int> vertexIds = new HashSet<int>();
            int[] edgeVertexIds;
            double first;
            double last;
            foreach (var edgeId in surfaceEdgeIds.Select(Math.Abs).Distinct())
            {
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out edgeVertexIds);
                vertexIds.UnionWith(edgeVertexIds);
                bool closed = edgeVertexIds.Distinct().Count() == 1 && !_splits.IsSplit(edgeId);
                foreach (var pieceId in _splits.GetPieceIds(edgeId))
                {
                    if (!_pieceIdImageId.TryGetValue(pieceId, out imageId)) return false;
                    if (imageId != pieceId) continue;
                    if (!_reversedPieceIds.Contains(pieceId)) return false;
                    // A piece between two points crosses the plane once; a closed edge that is not split crosses it
                    // at its vertex and half way round
                    _splits.GetSpan(pieceId, out first, out last);
                    double range = last - first;
                    double[] crossing = closed ? GetCrossing(edgeId, first + 0.25 * range, first + 0.75 * range) :
                                                 GetCrossing(edgeId, first, last);
                    if (crossing == null) return false;
                    points.Add(crossing);
                    pointPieceIds.Add(pieceId);
                }
            }
            foreach (var vertexId in vertexIds.OrderBy(id => id))
            {
                if (!_vertexIdImageId.TryGetValue(vertexId, out imageId) || imageId != vertexId) continue;
                double[] coor;
                Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
                points.Add(coor);
                pointPieceIds.Add(0);
            }
            foreach (var edgeId in surfaceEdgeIds.Select(Math.Abs).Distinct().OrderBy(id => id))
            {
                foreach (var splitPoint in GetSplitPointsInPlane(edgeId))
                {
                    points.Add(splitPoint);
                    pointPieceIds.Add(0);
                }
            }
            return true;
        }
        /// <summary>
        /// The splits of the edge that lie in the plane: the edge has a node there, as at a vertex.
        /// </summary>
        private IEnumerable<double[]> GetSplitPointsInPlane(int edgeId)
        {
            if (!_splits.IsSplit(edgeId)) return new double[0][];
            return _splits.GetSplitPoints(edgeId).Where(
                point => Math.Abs(GetDistance(point)) <= CollinearTolerance * _size);
        }
        /// <summary>
        /// Where the edge crosses the plane between two of its parameters, at which it lies on the two sides of it.
        /// </summary>
        private double[] GetCrossing(int edgeId, double first, double last)
        {
            double[] firstCoor;
            double[] coor;
            Gmsh.Model.GetValue(1, edgeId, new double[] { first }, out firstCoor);
            double firstDistance = GetDistance(firstCoor);
            if (firstDistance == 0) return firstCoor;
            Gmsh.Model.GetValue(1, edgeId, new double[] { last }, out coor);
            double lastDistance = GetDistance(coor);
            if (lastDistance == 0) return coor;
            if (Math.Sign(firstDistance) == Math.Sign(lastDistance)) return null;
            for (int i = 0; i < NumOfBisections; i++)
            {
                double middle = 0.5 * (first + last);
                Gmsh.Model.GetValue(1, edgeId, new double[] { middle }, out coor);
                double distance = GetDistance(coor);
                if (distance == 0) return coor;
                if (Math.Sign(distance) == Math.Sign(firstDistance)) first = middle;
                else last = middle;
            }
            return coor;
        }
        private static int GetFarthest(List<double[]> points, double[] from)
        {
            int farthest = 0;
            double maxDistance = -1;
            for (int p = 0; p < points.Count; p++)
            {
                double distance = 0;
                for (int c = 0; c < 3; c++) distance += (points[p][c] - from[c]) * (points[p][c] - from[c]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    farthest = p;
                }
            }
            return farthest;
        }
    }
}
