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
    /// Finds the planes the gmsh model is its own mirror image across, by its geometry alone, and where its edges are
    /// split for them (GmshMirrorSplits). The model is taken as a shape: its surfaces map onto surfaces, but its edges
    /// need not be split alike on both sides of a plane, so the image of a vertex is a vertex or a point inside an
    /// edge - the seam of a circle, the end of an edge on one side of a straight line only.
    /// Every plane passes through the centre of the area of the model. The candidates map a vertex, or the centre of
    /// a surface, far from that centre onto another as far, or contain it together with the one farthest from the
    /// line through the two, or lie across or along that line with the normal of a surface. A candidate is a mirror
    /// plane when the image of every vertex is a vertex or lies on an edge, the images of three points of every edge
    /// lie on edges or inside surfaces, and every surface has a surface of the same area as its image, at the image
    /// of its centre and through the images of four of its points, or has the images of these points inside
    /// surfaces. An edge whose image runs inside a surface - the seam of a cylinder, the edge between two walls of a
    /// sweep under a vertex its source has on one side only - has no image, and neither has a surface that is split
    /// unlike its image; the mesh shows whether such places are mirrored (GmshMirrorNodes). A plane that holds the
    /// whole model is none.
    /// The planes are taken in the order of how close they are to a coordinate plane, and only those perpendicular
    /// to the ones already taken, three at most: the mirror images across perpendicular planes do not depend on
    /// their order.
    /// </summary>
    public class GmshMirrorDetector
    {
        // Variables                                                                                                    
        private const double SampleTolerance = 1E-5;
        private const double CandidateTolerance = 1E-4;
        private const double AreaTolerance = 1E-4;
        private const double PerpendicularTolerance = 1E-6;
        private const int MaxNumOfStarts = 8;
        private const int MaxNumOfNormalSurfaces = 8;
        private static readonly double[] EdgeSampleShares = new double[] { 0.25, 0.5, 0.75 };
        private static readonly double[][] SurfaceSampleShares = new double[][] { new double[] { 0.3, 0.3 },
                                                                                  new double[] { 0.7, 0.3 },
                                                                                  new double[] { 0.3, 0.7 },
                                                                                  new double[] { 0.7, 0.7 } };
        private readonly double _size;
        private readonly int[] _vertexIds;
        private readonly Dictionary<int, double[]> _vertexIdCoor;
        private readonly Dictionary<Tuple<long, long, long>, List<int>> _cellVertexIds;
        private readonly double _cellSize;
        private readonly int[] _edgeIds;
        private readonly Dictionary<int, int[]> _edgeIdVertexIds;
        private readonly Dictionary<int, double[]> _edgeIdBox;
        private readonly Dictionary<int, double[]> _edgeIdBounds;
        private readonly HashSet<int> _degenerateEdgeIds;
        private Dictionary<int, double[]> _surfaceIdBox;
        private Dictionary<int, double[][]> _edgeIdSamples;
        private readonly int[] _surfaceIds;
        private Dictionary<int, double> _surfaceIdArea;
        private Dictionary<int, double[]> _surfaceIdCentre;
        private Dictionary<int, double[][]> _surfaceIdSamples;


        // Constructors                                                                                                 
        public GmshMirrorDetector()
        {
            Tuple<int, int>[] dimTags;
            Gmsh.Model.GetEntities(out dimTags, 0);
            _vertexIds = dimTags.Select(dimTag => dimTag.Item2).OrderBy(id => id).ToArray();
            Gmsh.Model.GetEntities(out dimTags, 1);
            _edgeIds = dimTags.Select(dimTag => dimTag.Item2).OrderBy(id => id).ToArray();
            Gmsh.Model.GetEntities(out dimTags, 2);
            _surfaceIds = dimTags.Select(dimTag => dimTag.Item2).OrderBy(id => id).ToArray();
            //
            _vertexIdCoor = new Dictionary<int, double[]>();
            foreach (var vertexId in _vertexIds)
            {
                double[] coor;
                Gmsh.Model.GetValue(0, vertexId, new double[0], out coor);
                _vertexIdCoor.Add(vertexId, coor);
            }
            double[] low = new double[3];
            double[] high = new double[3];
            Gmsh.Model.GetBoundingBox(-1, -1, out low[0], out low[1], out low[2], out high[0], out high[1],
                                      out high[2]);
            _size = GetDistance(low, high);
            _cellSize = 1E-3 * _size;
            _cellVertexIds = new Dictionary<Tuple<long, long, long>, List<int>>();
            List<int> cellVertexIds;
            foreach (var vertexId in _vertexIds)
            {
                Tuple<long, long, long> cell = GetCell(_vertexIdCoor[vertexId], 0, 0, 0);
                if (!_cellVertexIds.TryGetValue(cell, out cellVertexIds))
                {
                    cellVertexIds = new List<int>();
                    _cellVertexIds.Add(cell, cellVertexIds);
                }
                cellVertexIds.Add(vertexId);
            }
            // A degenerate edge, the pole of a sphere or the apex of a cone, is a point: gmsh has no curve to project
            // onto and logs an error when asked to, so it is neither searched nor mapped
            _edgeIdVertexIds = new Dictionary<int, int[]>();
            _edgeIdBox = new Dictionary<int, double[]>();
            _edgeIdBounds = new Dictionary<int, double[]>();
            _degenerateEdgeIds = new HashSet<int>();
            double[] min;
            double[] max;
            foreach (var edgeId in _edgeIds)
            {
                int[] vertexIds;
                Gmsh.Model.GetAdjacencies(1, edgeId, out _, out vertexIds);
                if (vertexIds.Length == 1) vertexIds = new int[] { vertexIds[0], vertexIds[0] };
                _edgeIdVertexIds.Add(edgeId, vertexIds);
                double[] box = new double[6];
                Gmsh.Model.GetBoundingBox(1, edgeId, out box[0], out box[1], out box[2], out box[3], out box[4],
                                          out box[5]);
                _edgeIdBox.Add(edgeId, box);
                Gmsh.Model.GetParametrizationBounds(1, edgeId, out min, out max);
                _edgeIdBounds.Add(edgeId, new double[] { min[0], max[0] });
                if (GetDistance(new double[] { box[0], box[1], box[2] }, new double[] { box[3], box[4], box[5] }) <=
                    SampleTolerance * _size) _degenerateEdgeIds.Add(edgeId);
            }
        }


        // Methods                                                                                                      
        /// <summary>
        /// The mirror planes of the gmsh model, perpendicular to each other, with the pieces of its edges mapped
        /// across each (SetPieces); none for a model without surfaces or with an edge that does not run between one
        /// or two vertices.
        /// </summary>
        public List<GmshMirrorPlane> FindPlanes()
        {
            List<GmshMirrorPlane> planes = new List<GmshMirrorPlane>();
            if (_surfaceIds.Length == 0 || !(_size > 0) ||
                _edgeIdVertexIds.Values.Any(vertexIds => vertexIds.Length != 2)) return planes;
            ReadSurfaces();
            double[] centre = new double[3];
            double area = 0;
            foreach (var surfaceId in _surfaceIds)
            {
                for (int c = 0; c < 3; c++) centre[c] += _surfaceIdArea[surfaceId] * _surfaceIdCentre[surfaceId][c];
                area += _surfaceIdArea[surfaceId];
            }
            if (!(area > 0)) return planes;
            for (int c = 0; c < 3; c++) centre[c] /= area;
            foreach (var normal in GetCandidateNormals(centre).OrderByDescending(n => Math.Round(n.Max(Math.Abs), 9))
                                                              .ThenByDescending(n => Math.Round(n[0], 9))
                                                              .ThenByDescending(n => Math.Round(n[1], 9))
                                                              .ThenByDescending(n => Math.Round(n[2], 9)))
            {
                if (planes.Any(plane => Math.Abs(GetDot(plane.Normal, normal)) > PerpendicularTolerance)) continue;
                GmshMirrorPlane mirrorPlane = Verify(centre, normal);
                if (mirrorPlane != null) planes.Add(mirrorPlane);
                if (planes.Count == 3) break;
            }
            return SetPieces(planes);
        }
        /// <summary>
        /// Splits the edges for the given planes together and maps the vertices and the pieces across each: the image
        /// of every vertex that is no vertex splits the edge it lies in, and so does the image of every split across
        /// every plane, until the splits are mirrored across all. Returns the planes, without those across which a
        /// piece has no piece as its image.
        /// </summary>
        public List<GmshMirrorPlane> SetPieces(List<GmshMirrorPlane> planes)
        {
            planes = new List<GmshMirrorPlane>(planes);
            double tolerance = SampleTolerance * _size;
            while (planes.Count > 0)
            {
                GmshMirrorPlane failed = null;
                GmshMirrorSplits splits = new GmshMirrorSplits();
                Queue<double[]> points = new Queue<double[]>(_vertexIds.Select(id => _vertexIdCoor[id]));
                int edgeId;
                double parameter;
                double[] onEdge;
                while (points.Count > 0 && failed == null)
                {
                    double[] point = points.Dequeue();
                    foreach (var plane in planes)
                    {
                        double[] image = plane.Mirror(point);
                        if (FindVertex(image) >= 0) continue;
                        if (!FindPointOnEdge(image, out edgeId, out parameter, out onEdge)) failed = plane;
                        else if (splits.Add(edgeId, parameter, onEdge, tolerance)) points.Enqueue(onEdge);
                    }
                }
                if (failed == null)
                {
                    splits.Number();
                    foreach (var plane in planes)
                    {
                        if (failed == null && !MapPieces(plane, splits)) failed = plane;
                    }
                }
                if (failed == null) return planes;
                planes.Remove(failed);
            }
            return planes;
        }
        /// <summary>
        /// The images of the vertices and of the pieces of the edges across the plane. False when a piece has no
        /// piece as its image.
        /// </summary>
        private bool MapPieces(GmshMirrorPlane plane, GmshMirrorSplits splits)
        {
            Dictionary<int, int> vertexIdImageId = new Dictionary<int, int>();
            Dictionary<int, int> pieceIdImageId = new Dictionary<int, int>();
            HashSet<int> reversedPieceIds = new HashSet<int>();
            double tolerance = SampleTolerance * _size;
            foreach (var vertexId in _vertexIds)
            {
                int imageId = FindVertex(plane.Mirror(_vertexIdCoor[vertexId]));
                if (imageId >= 0) vertexIdImageId.Add(vertexId, imageId);
            }
            int imageEdgeId;
            double parameter;
            double first;
            double last;
            double imageFirst;
            double imageLast;
            foreach (var edgeId in _edgeIds)
            {
                if (_degenerateEdgeIds.Contains(edgeId)) continue;
                foreach (var pieceId in splits.GetPieceIds(edgeId))
                {
                    splits.GetSpan(pieceId, out first, out last);
                    double[] middle = plane.Mirror(GetPoint(edgeId, 0.5 * (first + last)));
                    if (!FindPointOnEdge(middle, out imageEdgeId, out parameter, out _))
                    {
                        // A piece whose image runs inside a surface has no image
                        if (IsInsideSurface(middle)) continue;
                        return false;
                    }
                    int imageId = splits.GetPieceAt(imageEdgeId, parameter);
                    splits.GetSpan(imageId, out imageFirst, out imageLast);
                    double[] start = plane.Mirror(GetPoint(edgeId, first));
                    double[] end = plane.Mirror(GetPoint(edgeId, last));
                    double[] imageStart = GetPoint(imageEdgeId, imageFirst);
                    double[] imageEnd = GetPoint(imageEdgeId, imageLast);
                    bool reversed;
                    if (GetDistance(imageStart, imageEnd) <= tolerance)
                    {
                        // A closed edge: its quarter has the three quarters of its image as its image when reversed
                        double[] quarter = plane.Mirror(GetPoint(edgeId, first + 0.25 * (last - first)));
                        double imageRange = imageLast - imageFirst;
                        reversed = GetDistance(quarter, GetPoint(imageEdgeId, imageFirst + 0.75 * imageRange)) <
                                   GetDistance(quarter, GetPoint(imageEdgeId, imageFirst + 0.25 * imageRange));
                    }
                    else reversed = GetDistance(start, imageEnd) + GetDistance(end, imageStart) <
                                    GetDistance(start, imageStart) + GetDistance(end, imageEnd);
                    if (GetDistance(start, reversed ? imageEnd : imageStart) > tolerance ||
                        GetDistance(end, reversed ? imageStart : imageEnd) > tolerance) return false;
                    pieceIdImageId.Add(pieceId, imageId);
                    if (reversed) reversedPieceIds.Add(pieceId);
                }
            }
            int backId;
            foreach (var entry in pieceIdImageId)
            {
                if (!pieceIdImageId.TryGetValue(entry.Value, out backId) || backId != entry.Key ||
                    reversedPieceIds.Contains(entry.Key) != reversedPieceIds.Contains(entry.Value)) return false;
            }
            plane.SetPieces(splits, _edgeIds, vertexIdImageId, pieceIdImageId, reversedPieceIds);
            return true;
        }
        /// <summary>
        /// The normals of the planes through the centre that may be mirror planes, each once and turned so that its
        /// first component that is not zero is positive.
        /// </summary>
        private List<double[]> GetCandidateNormals(double[] centre)
        {
            List<double[]> normals = new List<double[]>();
            double tolerance = CandidateTolerance * _size;
            List<double[]> surfaceNormals = new List<double[]>();
            double[] min;
            double[] max;
            double[] normal;
            foreach (var surfaceId in _surfaceIds.Take(MaxNumOfNormalSurfaces))
            {
                Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
                Gmsh.Model.GetNormal(surfaceId, new double[] { 0.5 * (min[0] + max[0]), 0.5 * (min[1] + max[1]) },
                                     out normal);
                if (normal.Length == 3) surfaceNormals.Add(normal);
            }
            List<double[]>[] pointSets = new List<double[]>[]
            {
                _vertexIds.Select(id => _vertexIdCoor[id]).ToList(),
                _surfaceIds.Select(id => _surfaceIdCentre[id]).ToList()
            };
            foreach (var points in pointSets)
            {
                double[] distances = points.Select(point => GetDistance(point, centre)).ToArray();
                int[] starts = Enumerable.Range(0, points.Count).Where(p => distances[p] > tolerance)
                                         .OrderByDescending(p => distances[p]).Take(MaxNumOfStarts).ToArray();
                foreach (var start in starts)
                {
                    // The planes that map the start onto a point as far from the centre
                    for (int p = 0; p < points.Count; p++)
                    {
                        if (p != start && Math.Abs(distances[p] - distances[start]) <= tolerance)
                            AddNormal(normals, Subtract(points[p], points[start]), tolerance);
                    }
                    // The planes that hold the start: through the point farthest from the line to it, and those that
                    // map that point onto one as far along and from the line
                    double[] axis = Subtract(points[start], centre);
                    for (int c = 0; c < 3; c++) axis[c] /= distances[start];
                    int off = -1;
                    double offLength = tolerance;
                    for (int p = 0; p < points.Count; p++)
                    {
                        double length = GetLength(GetPerpendicular(points[p], centre, axis, out _));
                        if (length > offLength)
                        {
                            offLength = length;
                            off = p;
                        }
                    }
                    if (off >= 0)
                    {
                        double offAlong;
                        double[] offAcross = GetPerpendicular(points[off], centre, axis, out offAlong);
                        AddNormal(normals, GetCross(axis, offAcross), tolerance);
                        for (int p = 0; p < points.Count; p++)
                        {
                            if (p == off) continue;
                            double along;
                            double[] across = GetPerpendicular(points[p], centre, axis, out along);
                            if (Math.Abs(along - offAlong) > tolerance ||
                                Math.Abs(GetLength(across) - offLength) > tolerance) continue;
                            AddNormal(normals, Subtract(across, offAcross), tolerance);
                        }
                    }
                    // The planes across the line to the start and along it with the normals of the surfaces
                    AddNormal(normals, axis, CandidateTolerance);
                    foreach (var surfaceNormal in surfaceNormals)
                    {
                        AddNormal(normals, GetCross(axis, surfaceNormal), CandidateTolerance);
                        AddNormal(normals, GetPerpendicular(surfaceNormal, new double[3], axis, out _),
                                  CandidateTolerance);
                    }
                }
            }
            return normals;
        }
        /// <summary>
        /// The mirror plane through the point with the normal, with the images of the surfaces, or null when the
        /// model is not its own mirror image across it or lies in it.
        /// </summary>
        private GmshMirrorPlane Verify(double[] point, double[] normal)
        {
            Dictionary<int, int> surfaceIdImageId = new Dictionary<int, int>();
            GmshMirrorPlane plane = new GmshMirrorPlane(point, normal, _size, surfaceIdImageId);
            bool inPlane = true;
            // The candidate comes from centres of surfaces, which are integrated, so the plane is set again from the
            // vertices that have vertices as their images: its normal along the lines between them, and through the
            // middle of these lines
            double[] sum = new double[3];
            double[] middle = new double[3];
            int numOfPairs = 0;
            foreach (var vertexId in _vertexIds)
            {
                double[] coor = _vertexIdCoor[vertexId];
                double[] image = plane.Mirror(coor);
                inPlane &= Math.Abs(plane.GetDistance(coor)) <= SampleTolerance * _size;
                int imageId = FindVertex(image);
                if (imageId < 0 && !FindPointOnEdge(image, out _, out _, out _)) return null;
                if (imageId < 0) continue;
                double[] between = Subtract(_vertexIdCoor[imageId], coor);
                double sign = GetDot(between, normal) < 0 ? -1 : 1;
                for (int c = 0; c < 3; c++)
                {
                    sum[c] += sign * between[c];
                    middle[c] += 0.5 * (coor[c] + _vertexIdCoor[imageId][c]);
                }
                numOfPairs++;
            }
            if (numOfPairs > 0)
            {
                double length = GetLength(sum);
                if (length > SampleTolerance * _size)
                    normal = new double[] { sum[0] / length, sum[1] / length, sum[2] / length };
                double along = GetDot(Subtract(middle.Select(c => c / numOfPairs).ToArray(), point), normal);
                point = new double[] { point[0] + along * normal[0], point[1] + along * normal[1],
                                       point[2] + along * normal[2] };
                plane = new GmshMirrorPlane(point, normal, _size, surfaceIdImageId);
            }
            ReadEdges();
            foreach (var edgeId in _edgeIds)
            {
                if (_degenerateEdgeIds.Contains(edgeId)) continue;
                foreach (var sample in _edgeIdSamples[edgeId])
                {
                    double[] image = plane.Mirror(sample);
                    inPlane &= Math.Abs(plane.GetDistance(sample)) <= SampleTolerance * _size;
                    if (FindVertex(image) < 0 && !FindPointOnEdge(image, out _, out _, out _) &&
                        !IsInsideSurface(image)) return null;
                }
            }
            if (inPlane) return null;
            //
            foreach (var surfaceId in _surfaceIds)
            {
                double area = _surfaceIdArea[surfaceId];
                double[] centre = plane.Mirror(_surfaceIdCentre[surfaceId]);
                double[][] images = _surfaceIdSamples[surfaceId].Select(plane.Mirror).ToArray();
                int imageId = 0;
                foreach (var candidateId in _surfaceIds)
                {
                    double candidateArea = _surfaceIdArea[candidateId];
                    if (Math.Abs(candidateArea - area) > AreaTolerance * Math.Max(area, candidateArea) ||
                        GetDistance(_surfaceIdCentre[candidateId], centre) > CandidateTolerance * _size ||
                        !AreOnEntity(2, candidateId, images)) continue;
                    imageId = candidateId;
                    break;
                }
                if (imageId != 0) surfaceIdImageId.Add(surfaceId, imageId);
                else
                {
                    // A surface split unlike its image: its points that lie inside it have images inside surfaces
                    for (int s = 0; s < images.Length; s++)
                    {
                        if (IsInside(surfaceId, _surfaceIdSamples[surfaceId][s]) && !IsInsideSurface(images[s]))
                            return null;
                    }
                }
            }
            int backId;
            foreach (var entry in surfaceIdImageId)
            {
                if (!surfaceIdImageId.TryGetValue(entry.Value, out backId) || backId != entry.Key) return null;
            }
            return plane;
        }
        private void ReadEdges()
        {
            if (_edgeIdSamples != null) return;
            _edgeIdSamples = new Dictionary<int, double[][]>();
            foreach (var edgeId in _edgeIds)
            {
                if (_degenerateEdgeIds.Contains(edgeId)) continue;
                double[] bounds = _edgeIdBounds[edgeId];
                double[][] samples = new double[EdgeSampleShares.Length][];
                for (int s = 0; s < samples.Length; s++)
                    samples[s] = GetPoint(edgeId, bounds[0] + EdgeSampleShares[s] * (bounds[1] - bounds[0]));
                _edgeIdSamples.Add(edgeId, samples);
            }
        }
        private void ReadSurfaces()
        {
            if (_surfaceIdArea != null) return;
            _surfaceIdArea = new Dictionary<int, double>();
            _surfaceIdCentre = new Dictionary<int, double[]>();
            _surfaceIdSamples = new Dictionary<int, double[][]>();
            double[] min;
            double[] max;
            foreach (var surfaceId in _surfaceIds)
            {
                double area;
                Gmsh.Model.OCC.GetMass(2, surfaceId, out area);
                _surfaceIdArea.Add(surfaceId, area);
                double[] centre = new double[3];
                Gmsh.Model.OCC.GetCenterOfMass(2, surfaceId, out centre[0], out centre[1], out centre[2]);
                _surfaceIdCentre.Add(surfaceId, centre);
                Gmsh.Model.GetParametrizationBounds(2, surfaceId, out min, out max);
                double[][] samples = new double[SurfaceSampleShares.Length][];
                for (int s = 0; s < samples.Length; s++)
                    Gmsh.Model.GetValue(2, surfaceId,
                                        new double[] { min[0] + SurfaceSampleShares[s][0] * (max[0] - min[0]),
                                                       min[1] + SurfaceSampleShares[s][1] * (max[1] - min[1]) },
                                        out samples[s]);
                _surfaceIdSamples.Add(surfaceId, samples);
            }
        }
        private static double[] GetPoint(int edgeId, double parameter)
        {
            double[] point;
            Gmsh.Model.GetValue(1, edgeId, new double[] { parameter }, out point);
            return point;
        }
        /// <summary>
        /// The edge the point lies on, with its parameter there and the point of the edge nearest to it: the first
        /// edge whose box holds the point and that passes within the tolerance of it.
        /// </summary>
        private bool FindPointOnEdge(double[] point, out int edgeId, out double parameter, out double[] onEdge)
        {
            edgeId = 0;
            parameter = 0;
            onEdge = null;
            double tolerance = SampleTolerance * _size;
            double[] closest;
            double[] parameters;
            foreach (var id in _edgeIds)
            {
                if (_degenerateEdgeIds.Contains(id)) continue;
                double[] box = _edgeIdBox[id];
                if (point[0] < box[0] - tolerance || point[0] > box[3] + tolerance || point[1] < box[1] - tolerance ||
                    point[1] > box[4] + tolerance || point[2] < box[2] - tolerance || point[2] > box[5] + tolerance)
                    continue;
                // The parameter gmsh returns with the closest point of an edge is not set, so it is asked for
                try
                {
                    Gmsh.Model.GetClosestPoint(1, id, point, out closest, out _);
                    if (closest.Length != 3 || GetDistance(closest, point) > tolerance) continue;
                    Gmsh.Model.GetParametrization(1, id, closest, out parameters);
                }
                catch (Exception ex) when (!(ex is OutOfMemoryException))
                {
                    continue;
                }
                if (parameters.Length != 1) continue;
                // gmsh projects onto the curve the edge is cut from, which runs on beyond its ends
                double[] bounds = _edgeIdBounds[id];
                double margin = 1E-9 * Math.Max(1, Math.Abs(bounds[1] - bounds[0]));
                if (parameters[0] < bounds[0] - margin || parameters[0] > bounds[1] + margin) continue;
                edgeId = id;
                parameter = parameters[0];
                onEdge = closest;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Whether the point lies inside a surface of the model, within its boundary.
        /// </summary>
        private bool IsInsideSurface(double[] point)
        {
            double tolerance = SampleTolerance * _size;
            if (_surfaceIdBox == null)
            {
                _surfaceIdBox = new Dictionary<int, double[]>();
                foreach (var id in _surfaceIds)
                {
                    double[] box = new double[6];
                    Gmsh.Model.GetBoundingBox(2, id, out box[0], out box[1], out box[2], out box[3], out box[4],
                                              out box[5]);
                    _surfaceIdBox.Add(id, box);
                }
            }
            foreach (var id in _surfaceIds)
            {
                double[] box = _surfaceIdBox[id];
                if (point[0] < box[0] - tolerance || point[0] > box[3] + tolerance || point[1] < box[1] - tolerance ||
                    point[1] > box[4] + tolerance || point[2] < box[2] - tolerance || point[2] > box[5] + tolerance)
                    continue;
                if (IsInside(id, point)) return true;
            }
            return false;
        }
        /// <summary>
        /// Whether the point lies on the surface, within its boundary: gmsh projects onto the surface the face is cut
        /// from, unbounded.
        /// </summary>
        private bool IsInside(int surfaceId, double[] point)
        {
            try
            {
                return AreOnEntity(2, surfaceId, new double[][] { point }) &&
                       Gmsh.Model.IsInside(2, surfaceId, point, false) > 0;
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return false;
            }
        }
        /// <summary>
        /// Whether the points lie on the edge or the surface, by their orthogonal projections onto it.
        /// </summary>
        private bool AreOnEntity(int dim, int id, double[][] points)
        {
            double[] closest;
            try
            {
                Gmsh.Model.GetClosestPoint(dim, id, points.SelectMany(point => point).ToArray(), out closest, out _);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException))
            {
                return false;
            }
            if (closest.Length != 3 * points.Length) return false;
            for (int p = 0; p < points.Length; p++)
            {
                double[] projection = new double[] { closest[3 * p], closest[3 * p + 1], closest[3 * p + 2] };
                if (GetDistance(points[p], projection) > SampleTolerance * _size) return false;
            }
            return true;
        }
        /// <summary>
        /// The vertex at the point, -1 for none. The tolerance is that of a point on an edge, so that the image of a
        /// vertex that misses a vertex by less is that vertex and no split at the end of an edge.
        /// </summary>
        private int FindVertex(double[] coor)
        {
            double tolerance = SampleTolerance * _size;
            List<int> vertexIds;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        if (!_cellVertexIds.TryGetValue(GetCell(coor, i, j, k), out vertexIds)) continue;
                        foreach (var vertexId in vertexIds)
                        {
                            if (GetDistance(_vertexIdCoor[vertexId], coor) <= tolerance) return vertexId;
                        }
                    }
                }
            }
            return -1;
        }
        private Tuple<long, long, long> GetCell(double[] coor, int i, int j, int k)
        {
            return new Tuple<long, long, long>((long)Math.Floor(coor[0] / _cellSize) + i,
                                               (long)Math.Floor(coor[1] / _cellSize) + j,
                                               (long)Math.Floor(coor[2] / _cellSize) + k);
        }
        /// <summary>
        /// Adds the direction as a unit normal, unless it is shorter than the least length or is among the normals,
        /// either way round.
        /// </summary>
        private static void AddNormal(List<double[]> normals, double[] direction, double minLength)
        {
            double length = GetLength(direction);
            if (!(length > minLength)) return;
            double[] normal = new double[] { direction[0] / length, direction[1] / length, direction[2] / length };
            for (int c = 0; c < 3; c++)
            {
                if (Math.Abs(normal[c]) <= 1E-9) continue;
                if (normal[c] < 0) normal = new double[] { -normal[0], -normal[1], -normal[2] };
                break;
            }
            if (normals.Any(other => Math.Abs(GetDot(other, normal)) > 1 - 1E-10)) return;
            normals.Add(normal);
        }
        /// <summary>
        /// The part of the point from the origin across the axis, a unit direction, and how far along it the point
        /// lies.
        /// </summary>
        private static double[] GetPerpendicular(double[] point, double[] origin, double[] axis, out double along)
        {
            double[] across = Subtract(point, origin);
            along = GetDot(across, axis);
            for (int c = 0; c < 3; c++) across[c] -= along * axis[c];
            return across;
        }
        private static double[] Subtract(double[] a, double[] b)
        {
            return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] };
        }
        private static double[] GetCross(double[] a, double[] b)
        {
            return new double[] { a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0] };
        }
        private static double GetDot(double[] a, double[] b)
        {
            return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
        }
        private static double GetLength(double[] a)
        {
            return Math.Sqrt(GetDot(a, a));
        }
        private static double GetDistance(double[] a, double[] b)
        {
            return GetLength(Subtract(a, b));
        }
    }
}
