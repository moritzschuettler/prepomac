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

namespace CaeMesh
{
    /// <summary>
    /// Finds the mirror symmetry of a compound part along the planes it is split on, so that one symmetric piece of
    /// its cells is swept and the mesh mirrored onto the others. A plane is used only when every cell has a mirror
    /// image across it, no cell crosses it, and every edge of an image lies on an edge of the mirrored cell it is
    /// meshed from - a cell may carry more edges than its image, never fewer - and when every mesh setup item that
    /// selects a part of the compound selects the images of what it selects as well.
    /// </summary>
    public class SweepSymmetry
    {
        // Variables
        private readonly FeMesh _geometry;
        private readonly GeometryPart[] _parts;
        private readonly Dictionary<int, GeometryPart> _partIdPart;
        private readonly Dictionary<int, CellShape> _partIdShape;
        private readonly double _vertexTolerance;
        private readonly double _curveTolerance;
        private readonly double _signatureTolerance;
        private Dictionary<string, int[]> _localMeshSizeSelections;


        // Properties
        /// <summary>The chosen planes, a point and a unit normal each: x, y, z, nx, ny, nz.</summary>
        public double[][] Planes { get; private set; }
        /// <summary>One row per mirrored cell: its part id, the part id of its swept cell, the planes as bits.</summary>
        public int[][] PartIds { get; private set; }
        /// <summary>The cells that are swept.</summary>
        public GeometryPart[] MasterParts { get; private set; }
        /// <summary>
        /// Why a mirror plane of the compound is not used, for the output window; null when one is, and when the
        /// compound has no mirror plane at all, which is told by nothing but the cells being swept as they are.
        /// </summary>
        public string Reason { get; private set; }
        /// <summary>
        /// The local mesh sizes that select only some of the mirror images of an entity, by name, with their selection
        /// and every image of it: the size is then the size of every image, so that the mesh stays symmetric.
        /// </summary>
        public Dictionary<string, int[]> LocalMeshSizeSelections { get; private set; }


        // Constructors
        private SweepSymmetry(FeMesh geometry, GeometryPart[] parts)
        {
            _geometry = geometry;
            _parts = parts;
            _partIdPart = parts.ToDictionary(part => part.PartId);
            _partIdShape = new Dictionary<int, CellShape>();
            BoundingBox box = new BoundingBox();
            foreach (var part in parts)
            {
                CellShape shape = new CellShape(part, geometry);
                _partIdShape.Add(part.PartId, shape);
                box.IncludeCoor(new double[] { shape.Box.MinX, shape.Box.MinY, shape.Box.MinZ });
                box.IncludeCoor(new double[] { shape.Box.MaxX, shape.Box.MaxY, shape.Box.MaxZ });
            }
            double diagonal = box.GetDiagonal();
            _vertexTolerance = 1E-5 * diagonal;
            _curveTolerance = 2E-3 * diagonal;
            _signatureTolerance = _curveTolerance;
        }


        // Methods
        /// <summary>
        /// The symmetry of the cells of a compound, or null when no plane can be used. The items are the other mesh
        /// setup items of the model; the own selection is that of the sweep item when it names source surfaces.
        /// </summary>
        public static SweepSymmetry Find(FeMesh geometry, GeometryPart[] parts, IEnumerable<MeshSetupItem> items,
                                         int[] ownSelectedGeometryIds, out string reason)
        {
            reason = null;
            if (parts == null || parts.Length < 2) return null;
            //
            SweepSymmetry symmetry = new SweepSymmetry(geometry, parts);
            if (!symmetry.Detect(items, ownSelectedGeometryIds))
            {
                reason = symmetry.Reason;
                return null;
            }
            return symmetry;
        }
        private bool Detect(IEnumerable<MeshSetupItem> items, int[] ownSelectedGeometryIds)
        {
            // Every plane a planar face shared by two cells lies in, across which every cell has a mirror image
            List<double[]> planes = new List<double[]>();
            List<Dictionary<int, int>> planePartners = new List<Dictionary<int, int>>();
            List<Dictionary<int, int>> planeSides = new List<Dictionary<int, int>>();
            foreach (var plane in GetCandidatePlanes())
            {
                Dictionary<int, int> sides;
                Dictionary<int, int> partners = PairCells(plane, out sides);
                if (partners == null) continue;
                // Mirror planes that are not perpendicular do not commute, and the images they give are not four
                if (planes.Any(other => Math.Abs(Dot(Normal(other), Normal(plane))) > 1E-6)) continue;
                planes.Add(plane);
                planePartners.Add(partners);
                planeSides.Add(sides);
                if (planes.Count == 3) break;
            }
            if (planes.Count == 0) return false;
            // The most planes that can be used together, and for each the side the swept cells lie on: the cells on
            // it have to carry every edge of their images
            string lastReason = null;
            for (int count = planes.Count; count >= 1; count--)
            {
                foreach (var subset in GetSubsets(planes.Count, count))
                {
                    for (int sideBits = 0; sideBits < (1 << count); sideBits++)
                    {
                        if (TryAssign(subset, sideBits, planes, planePartners, planeSides, items, ownSelectedGeometryIds,
                                      out lastReason)) return true;
                    }
                }
            }
            Reason = lastReason;
            return false;
        }
        private bool TryAssign(int[] subset, int sideBits, List<double[]> planes, List<Dictionary<int, int>> planePartners,
                               List<Dictionary<int, int>> planeSides, IEnumerable<MeshSetupItem> items,
                               int[] ownSelectedGeometryIds, out string reason)
        {
            reason = null;
            double[][] usedPlanes = subset.Select(index => planes[index]).ToArray();
            // A cell is a master when it lies on the chosen side of every plane
            HashSet<int> masterIds = new HashSet<int>();
            foreach (var part in _parts)
            {
                bool master = true;
                for (int i = 0; i < subset.Length && master; i++)
                {
                    int wantedSide = (sideBits & (1 << i)) != 0 ? -1 : 1;
                    if (planeSides[subset[i]][part.PartId] != wantedSide) master = false;
                }
                if (master) masterIds.Add(part.PartId);
            }
            if (masterIds.Count == 0 || masterIds.Count == _parts.Length) return false;
            // Every other cell reaches its master by the mirrors across the planes it lies beyond
            List<int[]> rows = new List<int[]>();
            Dictionary<int, Tuple<int, int>> partIdMasterBits = new Dictionary<int, Tuple<int, int>>();
            foreach (var part in _parts)
            {
                if (masterIds.Contains(part.PartId)) continue;
                //
                int bits = 0;
                int masterId = part.PartId;
                for (int i = 0; i < subset.Length; i++)
                {
                    int wantedSide = (sideBits & (1 << i)) != 0 ? -1 : 1;
                    if (planeSides[subset[i]][part.PartId] == wantedSide) continue;
                    bits |= 1 << i;
                    masterId = planePartners[subset[i]][masterId];
                }
                if (!masterIds.Contains(masterId)) return false;
                // The master has to carry every edge of the cell its mesh is mirrored onto
                reason = GetUncoveredEntity(masterId, part.PartId, usedPlanes, bits);
                if (reason != null) return false;
                rows.Add(new int[] { part.PartId, masterId, bits });
                partIdMasterBits.Add(part.PartId, new Tuple<int, int>(masterId, bits));
            }
            // The meshing inputs have to be as symmetric as the geometry
            if (!AreSelectionsSymmetric(items, ownSelectedGeometryIds, usedPlanes, masterIds, partIdMasterBits,
                                        out reason)) return false;
            //
            Planes = usedPlanes;
            PartIds = rows.ToArray();
            LocalMeshSizeSelections = _localMeshSizeSelections;
            MasterParts = _parts.Where(part => masterIds.Contains(part.PartId)).ToArray();
            return true;
        }
        private static IEnumerable<int[]> GetSubsets(int n, int count)
        {
            for (int mask = 0; mask < (1 << n); mask++)
            {
                int[] indices = Enumerable.Range(0, n).Where(i => (mask & (1 << i)) != 0).ToArray();
                if (indices.Length == count) yield return indices;
            }
        }
        // Planes
        /// <summary>
        /// The planes of the planar faces two cells share, each once, through which the bounding box of the compound
        /// is symmetric.
        /// </summary>
        private List<double[]> GetCandidatePlanes()
        {
            List<double[]> planes = new List<double[]>();
            BoundingBox box = new BoundingBox();
            foreach (var shape in _partIdShape.Values)
            {
                box.IncludeCoor(new double[] { shape.Box.MinX, shape.Box.MinY, shape.Box.MinZ });
                box.IncludeCoor(new double[] { shape.Box.MaxX, shape.Box.MaxY, shape.Box.MaxZ });
            }
            foreach (var entry in _geometry.GetSharedSurfaceIds(_parts))
            {
                int partId = FeMesh.GetPartIdFromGmshTopologyId(entry.Key);
                int surfaceId = FeMesh.GetItemIdFromGmshTopologyId(entry.Key);
                GeometryPart part;
                if (!_partIdPart.TryGetValue(partId, out part)) continue;
                VisualizationData vis = part.Visualization;
                if (vis.FaceTypes == null || surfaceId >= vis.FaceTypes.Length ||
                    vis.FaceTypes[surfaceId] != GeomFaceType.Plane) continue;
                //
                double[] plane = _partIdShape[partId].GetFacePlane(surfaceId);
                if (plane == null) continue;
                if (planes.Any(other => IsSamePlane(other, plane))) continue;
                if (!IsBoxSymmetric(box, plane)) continue;
                planes.Add(plane);
            }
            return planes;
        }
        private bool IsSamePlane(double[] plane1, double[] plane2)
        {
            if (Math.Abs(Math.Abs(Dot(Normal(plane1), Normal(plane2))) - 1) > 1E-6) return false;
            return Math.Abs(SignedDistance(plane1, Point(plane2))) <= _vertexTolerance;
        }
        private bool IsBoxSymmetric(BoundingBox box, double[] plane)
        {
            BoundingBox mirrored = MirrorBox(box, plane);
            return IsSameBox(box, mirrored, _signatureTolerance);
        }
        /// <summary>
        /// The mirror image of every cell across the plane, or null when a cell crosses the plane or has no image.
        /// The sides are +1 and -1 for the two sides of the plane.
        /// </summary>
        private Dictionary<int, int> PairCells(double[] plane, out Dictionary<int, int> sides)
        {
            sides = new Dictionary<int, int>();
            foreach (var entry in _partIdShape)
            {
                double min = double.MaxValue;
                double max = -double.MaxValue;
                foreach (var coor in entry.Value.NodeCoors)
                {
                    double distance = SignedDistance(plane, coor);
                    min = Math.Min(min, distance);
                    max = Math.Max(max, distance);
                }
                if (min >= -_vertexTolerance) sides.Add(entry.Key, 1);
                else if (max <= _vertexTolerance) sides.Add(entry.Key, -1);
                else return null;
            }
            Dictionary<int, int> partners = new Dictionary<int, int>();
            foreach (var entry in _partIdShape)
            {
                BoundingBox mirrored = MirrorBox(entry.Value.Box, plane);
                int partnerId = -1;
                double bestError = double.MaxValue;
                foreach (var candidate in _partIdShape)
                {
                    if (sides[candidate.Key] == sides[entry.Key]) continue;
                    // The two triangulate their curved faces apart, so their areas and volumes agree only as closely. A
                    // periodic face closed along a single seam is measured from where its seam lies, which the mirror
                    // does not carry over, and two images of a bore come out a tenth of a percent apart; the box and the
                    // edges decide the pairing, the measurements only rule out cells that are plainly different
                    if (Math.Abs(candidate.Value.Volume - entry.Value.Volume) > 5E-3 * Math.Abs(entry.Value.Volume)) continue;
                    if (Math.Abs(candidate.Value.Area - entry.Value.Area) > 5E-3 * entry.Value.Area) continue;
                    double error = BoxError(mirrored, candidate.Value.Box);
                    if (error > _signatureTolerance || error >= bestError) continue;
                    bestError = error;
                    partnerId = candidate.Key;
                }
                if (partnerId < 0) return null;
                partners.Add(entry.Key, partnerId);
            }
            // The pairing has to be one to one
            foreach (var entry in partners)
            {
                if (partners[entry.Value] != entry.Key) return null;
            }
            return partners;
        }
        // Edges
        /// <summary>
        /// The entity of the cell the master mirrored across the planes of the bits does not cover, named with how far
        /// it is off, or null when the master has the shape of the cell and carries every edge of it: every vertex of
        /// the cell is a mirrored vertex of the master, every edge of the cell runs along mirrored edges of the master,
        /// and every mirrored vertex of the master lies on the cell. The seam of a periodic face closed along a single
        /// seam lies where the modelling kernel put it and not where the mirror does, and a seam wall is meshed around
        /// its seam, so a seam and its vertices only have to lie on the master.
        /// </summary>
        private string GetUncoveredEntity(int masterId, int partId, double[][] planes, int bits)
        {
            CellShape master = _partIdShape[masterId];
            CellShape cell = _partIdShape[partId];
            string masterName = _partIdPart[masterId].Name;
            List<double[]> masterVertices = master.VertexCoors.Select(coor => Transform(coor, planes, bits)).ToList();
            List<double[][][]> masterEdges = master.EdgeSegments.Select(segments => TransformSegments(segments, planes, bits))
                                                   .ToList();
            double distance;
            for (int i = 0; i < cell.VertexCoors.Count; i++)
            {
                double[] coor = cell.VertexCoors[i];
                if (cell.SeamVertexIndices.Contains(i))
                {
                    distance = master.DistanceToSurface(Transform(coor, planes, bits));
                    if (distance > _curveTolerance)
                        return GetVertexName(partId, coor) + " is " + FormatDistance(distance) + " off the mirror " +
                               "image of " + masterName + GetToleranceText(_curveTolerance);
                }
                else
                {
                    distance = masterVertices.Min(vertex => Distance(vertex, coor));
                    if (distance > _vertexTolerance)
                        return GetVertexName(partId, coor) + " is " + FormatDistance(distance) + " from the mirror " +
                               "image of the nearest vertex of " + masterName + GetToleranceText(_vertexTolerance);
                }
            }
            for (int edgeId = 0; edgeId < cell.EdgeSegments.Count; edgeId++)
            {
                bool seam = cell.SeamEdgeIds.Contains(edgeId);
                foreach (var coor in cell.EdgeSegments[edgeId].SelectMany(segment => segment))
                {
                    if (seam) distance = master.DistanceToSurface(Transform(coor, planes, bits));
                    else distance = masterEdges.Min(edge => DistanceToSegments(coor, edge));
                    if (distance > _curveTolerance)
                        return "edge " + (edgeId + 1) + " of " + _partIdPart[partId].Name + " runs " +
                               FormatDistance(distance) + " off the mirror image of " +
                               (seam ? "" : "every edge of ") + masterName + GetToleranceText(_curveTolerance);
                }
            }
            for (int i = 0; i < masterVertices.Count; i++)
            {
                distance = cell.DistanceToSurface(masterVertices[i]);
                if (distance > _curveTolerance)
                    return "the mirror image of " + GetVertexName(masterId, master.VertexCoors[i]) + " is " +
                           FormatDistance(distance) + " off " + _partIdPart[partId].Name +
                           GetToleranceText(_curveTolerance);
            }
            return null;
        }
        /// <summary>
        /// A vertex of a cell named by the edges that meet at it, as the output window names edges.
        /// </summary>
        private string GetVertexName(int partId, double[] coor)
        {
            CellShape shape = _partIdShape[partId];
            List<int> edgeIds = new List<int>();
            for (int edgeId = 0; edgeId < shape.EdgeEnds.Count; edgeId++)
            {
                if (shape.EdgeEnds[edgeId].Any(end => Distance(end, coor) <= _vertexTolerance)) edgeIds.Add(edgeId);
            }
            string partName = _partIdPart[partId].Name;
            if (edgeIds.Count == 0) return "a vertex of " + partName;
            //
            string[] names = edgeIds.Select(id => (id + 1).ToString()).ToArray();
            string list = names.Length == 1 ? names[0] : string.Join(", ", names.Take(names.Length - 1)) + " and " +
                                                          names[names.Length - 1];
            return "the vertex " + (names.Length == 1 ? "of edge " : "where edges ") + list + " of " + partName +
                   (names.Length == 1 ? "" : " meet");
        }
        private static string FormatDistance(double distance)
        {
            return distance.ToString("G3");
        }
        private static string GetToleranceText(double tolerance)
        {
            return ", more than the tolerance of " + tolerance.ToString("G3");
        }
        // Selections
        /// <summary>
        /// Whether every item selecting a part of the compound selects the images of what it selects, and the own
        /// selection of the sweep item as well. A setting on a cell that only one side carries asks for a mesh that
        /// is not symmetric. A local mesh size is different: a count on an edge or a size on a face is a size the
        /// user wants there, and the images of the entity are meshed alike when the size is given to all of them, so
        /// its selection is completed by the images of what it selects. Two local mesh sizes the completion would
        /// put on one entity, from different sides, ask for two sizes there, and the symmetry is not used.
        /// </summary>
        private bool AreSelectionsSymmetric(IEnumerable<MeshSetupItem> items, int[] ownSelectedGeometryIds,
                                            double[][] planes, HashSet<int> masterIds,
                                            Dictionary<int, Tuple<int, int>> partIdMasterBits, out string reason)
        {
            reason = null;
            _localMeshSizeSelections = new Dictionary<string, int[]>();
            List<Tuple<string, int[]>> selections = new List<Tuple<string, int[]>>();
            Dictionary<string, HashSet<int>> completedSelections = new Dictionary<string, HashSet<int>>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (item.CreationIds == null) continue;
                    if (!(item is LocalMeshSize))
                    {
                        selections.Add(new Tuple<string, int[]>(item.Name, item.CreationIds));
                        continue;
                    }
                    HashSet<int> completed = new HashSet<int>(item.CreationIds);
                    foreach (var geometryId in item.CreationIds)
                    {
                        if (!_partIdPart.ContainsKey(FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2])) continue;
                        foreach (var image in GetImages(geometryId, planes, masterIds, partIdMasterBits))
                        {
                            if (image >= 0) completed.Add(image);
                        }
                    }
                    foreach (var entry in completedSelections)
                    {
                        HashSet<int> own = new HashSet<int>(item.CreationIds);
                        HashSet<int> other = new HashSet<int>(items.First(i => i.Name == entry.Key).CreationIds);
                        if (completed.Any(id => entry.Value.Contains(id) && !(own.Contains(id) && other.Contains(id))))
                        {
                            reason = "the local mesh sizes " + entry.Key + " and " + item.Name + " select mirror images " +
                                     "of each other";
                            return false;
                        }
                    }
                    completedSelections.Add(item.Name, completed);
                    if (completed.Count > item.CreationIds.Distinct().Count())
                        _localMeshSizeSelections.Add(item.Name, completed.OrderBy(id => id).ToArray());
                }
            }
            if (ownSelectedGeometryIds != null)
                selections.Add(new Tuple<string, int[]>("the sweep item", ownSelectedGeometryIds));
            //
            foreach (var selection in selections)
            {
                HashSet<int> ids = new HashSet<int>(selection.Item2);
                foreach (var geometryId in selection.Item2)
                {
                    int[] itemTypePart = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
                    if (!_partIdPart.ContainsKey(itemTypePart[2])) continue;
                    //
                    foreach (var image in GetImages(geometryId, planes, masterIds, partIdMasterBits))
                    {
                        if (image < 0 || !ids.Contains(image))
                        {
                            reason = "the selection of " + selection.Item1 + " is not symmetric";
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// The geometry ids of the images of an entity in every other cell of its orbit, -1 where an image has no
        /// entity of its own - an edge of a cell whose image does not carry it.
        /// </summary>
        private IEnumerable<int> GetImages(int geometryId, double[][] planes, HashSet<int> masterIds,
                                           Dictionary<int, Tuple<int, int>> partIdMasterBits)
        {
            int[] itemTypePart = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId);
            int partId = itemTypePart[2];
            // The master of the entity's cell and the bits taking the master to the cell
            int masterId = partId;
            int ownBits = 0;
            Tuple<int, int> masterBits;
            if (partIdMasterBits.TryGetValue(partId, out masterBits))
            {
                masterId = masterBits.Item1;
                ownBits = masterBits.Item2;
            }
            // Every cell of the orbit: the master and the cells mirrored from it
            List<Tuple<int, int>> orbit = new List<Tuple<int, int>> { new Tuple<int, int>(masterId, 0) };
            foreach (var entry in partIdMasterBits)
            {
                if (entry.Value.Item1 == masterId) orbit.Add(new Tuple<int, int>(entry.Key, entry.Value.Item2));
            }
            foreach (var member in orbit)
            {
                if (member.Item1 == partId) continue;
                // From the entity's cell back to the master, then on to the member
                int bits = ownBits ^ member.Item2;
                if (FeMesh.IsPartGeometryId(geometryId))
                {
                    // A bare part id, or one carrying the part type digit
                    yield return FeMesh.GetTypeIdFromGeometryId(geometryId) == 0 ? member.Item1 :
                                 FeMesh.GetGeometryId(0, (int)GeometryType.Part, member.Item1);
                    continue;
                }
                int imageItemId = _partIdShape[member.Item1].FindImage(_partIdShape[partId], itemTypePart[0],
                                                                       (GeometryType)itemTypePart[1], planes, bits,
                                                                       _vertexTolerance, _curveTolerance);
                yield return imageItemId < 0 ? -1 : FeMesh.GetGeometryId(imageItemId, itemTypePart[1], member.Item1);
            }
        }
        // Geometry
        public static double[] Transform(double[] coor, double[][] planes, int bits)
        {
            double[] result = coor.ToArray();
            for (int i = 0; i < planes.Length; i++)
            {
                if ((bits & (1 << i)) != 0) result = Mirror(result, planes[i]);
            }
            return result;
        }
        public static double[] Mirror(double[] coor, double[] plane)
        {
            double distance = SignedDistance(plane, coor);
            return new double[] { coor[0] - 2 * distance * plane[3], coor[1] - 2 * distance * plane[4],
                                  coor[2] - 2 * distance * plane[5] };
        }
        public static double SignedDistance(double[] plane, double[] coor)
        {
            return (coor[0] - plane[0]) * plane[3] + (coor[1] - plane[1]) * plane[4] + (coor[2] - plane[2]) * plane[5];
        }
        private static double[] Point(double[] plane) { return new double[] { plane[0], plane[1], plane[2] }; }
        private static double[] Normal(double[] plane) { return new double[] { plane[3], plane[4], plane[5] }; }
        private static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
        private static double Distance(double[] a, double[] b)
        {
            return Math.Sqrt(Math.Pow(a[0] - b[0], 2) + Math.Pow(a[1] - b[1], 2) + Math.Pow(a[2] - b[2], 2));
        }
        private static BoundingBox MirrorBox(BoundingBox box, double[] plane)
        {
            BoundingBox mirrored = new BoundingBox();
            foreach (var x in new double[] { box.MinX, box.MaxX })
                foreach (var y in new double[] { box.MinY, box.MaxY })
                    foreach (var z in new double[] { box.MinZ, box.MaxZ })
                        mirrored.IncludeCoor(Mirror(new double[] { x, y, z }, plane));
            return mirrored;
        }
        private static double BoxError(BoundingBox box1, BoundingBox box2)
        {
            return new double[] { box1.MinX - box2.MinX, box1.MinY - box2.MinY, box1.MinZ - box2.MinZ,
                                  box1.MaxX - box2.MaxX, box1.MaxY - box2.MaxY, box1.MaxZ - box2.MaxZ }
                   .Max(value => Math.Abs(value));
        }
        private static bool IsSameBox(BoundingBox box1, BoundingBox box2, double tolerance)
        {
            return BoxError(box1, box2) <= tolerance;
        }
        public static double DistanceToSegments(double[] coor, double[][][] segments)
        {
            double min = double.MaxValue;
            foreach (var segment in segments) min = Math.Min(min, DistanceToSegment(coor, segment[0], segment[1]));
            return min;
        }
        private static double[][][] TransformSegments(double[][][] segments, double[][] planes, int bits)
        {
            return segments.Select(segment => new double[][] { Transform(segment[0], planes, bits),
                                                               Transform(segment[1], planes, bits) }).ToArray();
        }
        private static double DistanceToSegment(double[] p, double[] a, double[] b)
        {
            double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double[] ap = { p[0] - a[0], p[1] - a[1], p[2] - a[2] };
            double length2 = Dot(ab, ab);
            double t = length2 <= 0 ? 0 : Math.Max(0, Math.Min(1, Dot(ap, ab) / length2));
            return Distance(p, new double[] { a[0] + t * ab[0], a[1] + t * ab[1], a[2] + t * ab[2] });
        }
        public static double DistanceToTriangle(double[] p, double[] a, double[] b, double[] c)
        {
            double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
            double[] ac = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
            double[] n = { ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0] };
            double area2 = Math.Sqrt(Dot(n, n));
            if (area2 > 0)
            {
                double[] unit = { n[0] / area2, n[1] / area2, n[2] / area2 };
                double height = Dot(new double[] { p[0] - a[0], p[1] - a[1], p[2] - a[2] }, unit);
                double[] q = { p[0] - height * unit[0], p[1] - height * unit[1], p[2] - height * unit[2] };
                // Barycentric test of the projection
                double[] v0 = ab, v1 = ac, v2 = { q[0] - a[0], q[1] - a[1], q[2] - a[2] };
                double d00 = Dot(v0, v0), d01 = Dot(v0, v1), d11 = Dot(v1, v1), d20 = Dot(v2, v0), d21 = Dot(v2, v1);
                double denominator = d00 * d11 - d01 * d01;
                if (denominator > 0)
                {
                    double v = (d11 * d20 - d01 * d21) / denominator;
                    double w = (d00 * d21 - d01 * d20) / denominator;
                    if (v >= 0 && w >= 0 && v + w <= 1) return Math.Abs(height);
                }
            }
            return Math.Min(DistanceToSegment(p, a, b), Math.Min(DistanceToSegment(p, b, c), DistanceToSegment(p, c, a)));
        }


        // Nested
        /// <summary>
        /// The shape of one cell read from its visualization: its vertices, the polylines of its edges, the triangles
        /// of its faces, its bounding box, area and volume.
        /// </summary>
        private class CellShape
        {
            public readonly BoundingBox Box = new BoundingBox();
            public readonly List<double[]> NodeCoors = new List<double[]>();
            public readonly List<double[]> VertexCoors = new List<double[]>();
            public readonly List<double[][][]> EdgeSegments = new List<double[][][]>();
            public readonly List<double[][]> EdgeEnds = new List<double[][]>();
            public readonly List<List<double[][]>> FaceTriangles = new List<List<double[][]>>();
            public readonly HashSet<int> SeamEdgeIds = new HashSet<int>();
            public readonly HashSet<int> SeamVertexIndices = new HashSet<int>();
            public readonly double Area;
            public readonly double Volume;
            //
            public CellShape(GeometryPart part, FeMesh geometry)
            {
                VisualizationData vis = part.Visualization;
                HashSet<int> nodeIds = new HashSet<int>();
                foreach (var cell in vis.Cells) nodeIds.UnionWith(cell);
                foreach (var nodeId in nodeIds)
                {
                    double[] coor = geometry.Nodes[nodeId].Coor;
                    NodeCoors.Add(coor);
                    Box.IncludeCoor(coor);
                }
                // The seams of the periodic faces closed along a single seam, and their vertices
                HashSet<int> seamVertexNodeIds = new HashSet<int>();
                if (vis.FaceEdgeIds != null && vis.EdgeCellIdsByEdge != null)
                {
                    for (int faceId = 0; faceId < vis.FaceEdgeIds.Length; faceId++)
                    {
                        int seamEdgeId;
                        if (!vis.IsSurfaceACylinderLike(faceId, out seamEdgeId)) continue;
                        SeamEdgeIds.Add(seamEdgeId);
                        seamVertexNodeIds.UnionWith(vis.GetVertexNodeIdsForEdgeId(seamEdgeId));
                    }
                }
                if (vis.VertexNodeIds != null)
                {
                    foreach (var nodeId in vis.VertexNodeIds)
                    {
                        if (seamVertexNodeIds.Contains(nodeId)) SeamVertexIndices.Add(VertexCoors.Count);
                        VertexCoors.Add(geometry.Nodes[nodeId].Coor);
                    }
                }
                if (vis.EdgeCellIdsByEdge != null)
                {
                    for (int edgeId = 0; edgeId < vis.EdgeCellIdsByEdge.Length; edgeId++)
                    {
                        List<double[][]> segments = new List<double[][]>();
                        foreach (var edgeCellId in vis.EdgeCellIdsByEdge[edgeId])
                        {
                            int[] edgeCell = vis.EdgeCells[edgeCellId];
                            segments.Add(new double[][] { geometry.Nodes[edgeCell[0]].Coor, geometry.Nodes[edgeCell[1]].Coor });
                        }
                        EdgeSegments.Add(segments.ToArray());
                        EdgeEnds.Add(vis.GetVertexNodeIdsForEdgeId(edgeId).Select(nodeId => geometry.Nodes[nodeId].Coor)
                                        .ToArray());
                    }
                }
                double area = 0;
                double volume = 0;
                for (int faceId = 0; faceId < vis.CellIdsByFace.Length; faceId++)
                {
                    List<double[][]> triangles = new List<double[][]>();
                    foreach (var cellId in vis.CellIdsByFace[faceId])
                    {
                        int[] cell = vis.Cells[cellId];
                        double[] a = geometry.Nodes[cell[0]].Coor;
                        double[] b = geometry.Nodes[cell[1]].Coor;
                        double[] c = geometry.Nodes[cell[2]].Coor;
                        triangles.Add(new double[][] { a, b, c });
                        double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                        double[] ac = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                        double[] n = { ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0] };
                        area += 0.5 * Math.Sqrt(Dot(n, n));
                        volume += Dot(a, new double[] { b[1] * c[2] - b[2] * c[1], b[2] * c[0] - b[0] * c[2],
                                                        b[0] * c[1] - b[1] * c[0] }) / 6;
                    }
                    FaceTriangles.Add(triangles);
                }
                Area = area;
                Volume = Math.Abs(volume);
                // The triangles of a curved face are placed by its tessellation, which two mirror images do not share:
                // they sum to volumes a quarter of a percent apart on a cell whose CAD volumes agree to a hundredth.
                // The measurements of the CAD part are used where the part has them - its inertia is only ever read
                // from the CAD, and is dropped whenever the measurements are taken from the nodes again
                PartMassProperties massProperties = part.MassProperties;
                if (part.IsCADPart && massProperties.InertiaMatrixCG != null && massProperties.Volume > 0 &&
                    massProperties.Area > 0)
                {
                    Area = massProperties.Area;
                    Volume = massProperties.Volume;
                }
            }
            public double[] GetFacePlane(int faceId)
            {
                foreach (var triangle in FaceTriangles[faceId])
                {
                    double[] a = triangle[0], b = triangle[1], c = triangle[2];
                    double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                    double[] ac = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                    double[] n = { ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0] };
                    double length = Math.Sqrt(Dot(n, n));
                    if (length <= 0) continue;
                    n = new double[] { n[0] / length, n[1] / length, n[2] / length };
                    // One orientation for every plane, the largest component positive
                    int k = Math.Abs(n[0]) >= Math.Abs(n[1]) ? (Math.Abs(n[0]) >= Math.Abs(n[2]) ? 0 : 2)
                                                             : (Math.Abs(n[1]) >= Math.Abs(n[2]) ? 1 : 2);
                    if (n[k] < 0) n = new double[] { -n[0], -n[1], -n[2] };
                    return new double[] { a[0], a[1], a[2], n[0], n[1], n[2] };
                }
                return null;
            }
            public double DistanceToSurface(double[] coor)
            {
                double min = double.MaxValue;
                foreach (var triangles in FaceTriangles)
                {
                    foreach (var triangle in triangles)
                        min = Math.Min(min, DistanceToTriangle(coor, triangle[0], triangle[1], triangle[2]));
                }
                return min;
            }
            /// <summary>
            /// The entity of this cell that the given entity of another cell mirrors onto, or -1.
            /// </summary>
            public int FindImage(CellShape source, int itemId, GeometryType type, double[][] planes, int bits,
                                 double vertexTolerance, double curveTolerance)
            {
                if (type == GeometryType.Vertex)
                {
                    // The vertex ids are the node ids of the cell's vertices, in the geometry's own numbering
                    return -1;
                }
                if (type == GeometryType.Edge)
                {
                    if (itemId < 0 || itemId >= source.EdgeSegments.Count) return -1;
                    double[][][] mirrored = TransformSegments(source.EdgeSegments[itemId], planes, bits);
                    double[][] mirroredEnds = source.EdgeEnds[itemId].Select(coor => Transform(coor, planes, bits)).ToArray();
                    for (int edgeId = 0; edgeId < EdgeSegments.Count; edgeId++)
                    {
                        if (EdgeEnds[edgeId].Length != mirroredEnds.Length) continue;
                        if (!EdgeEnds[edgeId].All(end => mirroredEnds.Any(other => Distance(end, other) <= vertexTolerance)))
                            continue;
                        double[][][] segments = EdgeSegments[edgeId];
                        if (mirrored.SelectMany(segment => segment).All(coor => DistanceToSegments(coor, segments) <= curveTolerance) &&
                            segments.SelectMany(segment => segment).All(coor => DistanceToSegments(coor, mirrored) <= curveTolerance))
                            return edgeId;
                    }
                    return -1;
                }
                if (type == GeometryType.SolidSurface)
                {
                    if (itemId < 0 || itemId >= source.FaceTriangles.Count) return -1;
                    double sourceArea = source.FaceTriangles[itemId].Sum(triangle => TriangleArea(triangle));
                    double[][] first = source.FaceTriangles[itemId][0];
                    double[] sample = Transform(new double[] { (first[0][0] + first[1][0] + first[2][0]) / 3,
                                                               (first[0][1] + first[1][1] + first[2][1]) / 3,
                                                               (first[0][2] + first[1][2] + first[2][2]) / 3 }, planes, bits);
                    for (int faceId = 0; faceId < FaceTriangles.Count; faceId++)
                    {
                        double area = FaceTriangles[faceId].Sum(triangle => TriangleArea(triangle));
                        if (Math.Abs(area - sourceArea) > 1E-2 * sourceArea) continue;
                        if (FaceTriangles[faceId].Any(triangle => DistanceToTriangle(sample, triangle[0], triangle[1],
                                                                                     triangle[2]) <= curveTolerance))
                            return faceId;
                    }
                    return -1;
                }
                return -1;
            }
            private static double TriangleArea(double[][] triangle)
            {
                double[] a = triangle[0], b = triangle[1], c = triangle[2];
                double[] ab = { b[0] - a[0], b[1] - a[1], b[2] - a[2] };
                double[] ac = { c[0] - a[0], c[1] - a[1], c[2] - a[2] };
                double[] n = { ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0] };
                return 0.5 * Math.Sqrt(Dot(n, n));
            }
        }
    }
}
