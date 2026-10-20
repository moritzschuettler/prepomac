// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
// Source code: https://gitlab.com/MatejB/PrePoMax
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    // The elements of a composite layup that are covered by the same plies with the same layer angles. A zone is solved
    // as a composite shell section, so the solver input, the mass, the model checks and the composite results treat it
    // exactly like a composite shell section of the model.
    public class CompositeLayupZone
    {
        // Variables
        private CompositeLayup _layup;
        private int[] _elementIds;
        private CompositeShellSection _section;


        // Properties
        public CompositeLayup Layup { get { return _layup; } }
        public int[] ElementIds { get { return _elementIds; } }
        public CompositeShellSection Section { get { return _section; } }


        // Constructor
        public CompositeLayupZone(CompositeLayup layup, int[] elementIds, CompositeShellSection section)
        {
            _layup = layup;
            _elementIds = elementIds;
            _section = section;
        }
    }

    // The material orientation of the elements of a composite layup. With discrete orientations or a draping simulation
    // every element has axes of its own, which are computed when they are first needed.
    [Serializable]
    public class CompositeLayupOrientation : MaterialOrientation
    {
        // Variables
        [NonSerialized] private Func<Dictionary<int, double[]>> _referenceDirectionsProvider;
        [NonSerialized] private Dictionary<int, double[]> _referenceDirections;


        // Constructor
        public CompositeLayupOrientation(CompositeLayup layup, Func<Dictionary<int, double[]>> referenceDirectionsProvider)
            : base(layup.Name, null, RegionTypeEnum.None)
        {
            CoordinateSystemName = layup.CoordinateSystemName;
            ReferenceAxis = layup.ReferenceAxis;
            RotationAngle = layup.RotationAngle.DeepClone();
            SymbolDensity = layup.SymbolDensity;
            _referenceDirectionsProvider = referenceDirectionsProvider;
        }


        // Methods
        public override double[][] GetElementAxes(FeMesh mesh, FeElement element, double additionalAngleDeg,
                                                  out bool fallbackUsed)
        {
            if (_referenceDirectionsProvider == null)
                return base.GetElementAxes(mesh, element, additionalAngleDeg, out fallbackUsed);
            //
            if (_referenceDirections == null) _referenceDirections = _referenceDirectionsProvider() ??
                                                                     new Dictionary<int, double[]>();
            fallbackUsed = false;
            double[] point;
            double[] normal;
            if (!MaterialOrientationCalculator.TryGetShellPointAndNormal(mesh, element, out point, out normal)) return null;
            //
            double[] direction;
            if (!_referenceDirections.TryGetValue(element.Id, out direction))
                return base.GetElementAxes(mesh, element, additionalAngleDeg, out fallbackUsed);
            // The reference direction projected onto the shell, as MaterialOrientationCalculator.GetAxes does it
            Vec3D n = new Vec3D(normal);
            n.Normalize();
            Vec3D a = new Vec3D(direction);
            if (a.Normalize() == 0 ||
                Math.Abs(Vec3D.DotProduct(a, n)) > Math.Cos(MaterialOrientationCalculator.ParallelToleranceDeg * Math.PI / 180))
                return base.GetElementAxes(mesh, element, additionalAngleDeg, out fallbackUsed);
            //
            Vec3D e1 = a - Vec3D.DotProduct(a, n) * n;
            e1.Normalize();
            double angle = (RotationAngle.Value + additionalAngleDeg) * Math.PI / 180;
            if (angle != 0)
            {
                e1 = Math.Cos(angle) * e1 + Math.Sin(angle) * Vec3D.CrossProduct(n, e1);
                e1.Normalize();
            }
            Vec3D e2 = Vec3D.CrossProduct(n, e1);
            e2.Normalize();
            return new double[][] { e1.Coor, e2.Coor, n.Coor };
        }
    }

    public static class CompositeLayupZoneBuilder
    {
        // Variables
        private const double AngleRoundingDeg = 0.01;


        // Methods
        // Splits the layup into zones. The orientation is returned for every element of the layup.
        public static List<CompositeLayupZone> Create(FeModel model, CompositeLayup layup,
                                                      out CompositeLayupOrientation orientation, out List<string> warnings)
        {
            warnings = new List<string>();
            orientation = null;
            List<CompositeLayupZone> zones = new List<CompositeLayupZone>();
            FeMesh mesh = model.Mesh;
            if (mesh == null || layup.Plies.Count == 0) return zones;
            //
            if (layup.OrientationType == CompositeLayupOrientationEnum.DiscreteOrientations)
                return CreateDiscreteZones(model, layup, out orientation, warnings);
            //
            Func<Dictionary<int, double[]>> provider = null;
            if (layup.OrientationType == CompositeLayupOrientationEnum.Draping)
                provider = () => ComputeDrapingDirections(mesh, layup, null, out _);
            orientation = new CompositeLayupOrientation(layup, provider);
            // Elements of each ply
            List<HashSet<int>> plyElementIds = new List<HashSet<int>>();
            for (int i = 0; i < layup.Plies.Count; i++)
            {
                int[] ids = CompositeLayup.GetRegionElementIds(mesh, layup.Plies[i].RegionName);
                if (ids == null)
                {
                    warnings.Add("Composite layup " + layup.Name + ": the region of the ply " + layup.Plies[i].Name +
                                 " does not exist.");
                    ids = new int[0];
                }
                plyElementIds.Add(new HashSet<int>(ids));
            }
            // The draping simulation gives the fiber direction of the draped ply, so the layers turn from it
            double angleOffset = 0;
            if (layup.OrientationType == CompositeLayupOrientationEnum.Draping)
            {
                int drapingPly = Math.Min(layup.DrapingPlyIndex, layup.Plies.Count - 1);
                angleOffset = -layup.Plies[drapingPly].Angle.Value;
            }
            // Group the elements by the covering plies and their layer angles
            HashSet<int> allElementIds = new HashSet<int>();
            foreach (var ids in plyElementIds) allElementIds.UnionWith(ids);
            Dictionary<string, List<int>> keyElementIds = new Dictionary<string, List<int>>();
            Dictionary<string, List<Tuple<int, double>>> keyLayers = new Dictionary<string, List<Tuple<int, double>>>();
            List<Tuple<int, double>> layers;
            string key;
            foreach (int elementId in allElementIds.OrderBy(id => id))
            {
                layers = new List<Tuple<int, double>>();
                for (int i = 0; i < layup.Plies.Count; i++)
                {
                    if (!plyElementIds[i].Contains(elementId)) continue;
                    double angle = GetLayerAngle(mesh, mesh.Elements[elementId], layup, layup.Plies[i], orientation) +
                                   angleOffset;
                    layers.Add(new Tuple<int, double>(i, angle));
                }
                key = GetKey(layers);
                if (!keyElementIds.TryGetValue(key, out List<int> ids))
                {
                    ids = new List<int>();
                    keyElementIds.Add(key, ids);
                    keyLayers.Add(key, layers);
                }
                ids.Add(elementId);
            }
            //
            int zoneId = 1;
            foreach (var entry in keyElementIds)
            {
                string name = keyElementIds.Count == 1 ? layup.Name : layup.Name + "_Zone-" + zoneId++;
                List<CompositeLayer> sectionLayers = new List<CompositeLayer>();
                foreach (var layer in keyLayers[entry.Key])
                {
                    Ply ply = layup.Plies[layer.Item1];
                    sectionLayers.Add(new CompositeLayer(ply.MaterialName, ply.Thickness.DeepClone(),
                                                         new EquationContainer(typeof(StringAngleDegConverter),
                                                                               layer.Item2)));
                }
                zones.Add(new CompositeLayupZone(layup, entry.Value.ToArray(), CreateSection(name, layup, sectionLayers)));
            }
            return zones;
        }
        private static CompositeShellSection CreateSection(string name, CompositeLayup layup, List<CompositeLayer> layers)
        {
            CompositeShellSection section = new CompositeShellSection(name, null, RegionTypeEnum.None, layers, false);
            section.Symmetric = layup.Symmetric;
            section.Offset.SetEquationFromValue(layup.GetOffset());
            return section;
        }
        // The angle of a layer about the shell normal from axis 1 of the layup orientation. A ply with a coordinate
        // system of its own gets the angle between its fiber direction and axis 1 of the layup.
        private static double GetLayerAngle(FeMesh mesh, FeElement element, CompositeLayup layup, Ply ply,
                                            MaterialOrientation layupOrientation)
        {
            double angle = ply.Angle.Value;
            if (ply.CoordinateSystemName == null || layup.OrientationType != CompositeLayupOrientationEnum.CoordinateSystem)
                return Round(angle);
            //
            CoordinateSystem coordinateSystem;
            double[] point;
            double[] normal;
            if (!mesh.CoordinateSystems.TryGetValue(ply.CoordinateSystemName, out coordinateSystem) ||
                !MaterialOrientationCalculator.TryGetShellPointAndNormal(mesh, element, out point, out normal))
                return Round(angle);
            //
            double[][] plyAxes = MaterialOrientationCalculator.GetAxes(point, normal, coordinateSystem,
                                                                       layup.ReferenceAxis, angle, out _);
            double[][] layupAxes = layupOrientation.GetElementAxes(mesh, element, 0, out _);
            if (layupAxes == null) return Round(angle);
            // Signed angle about the normal
            Vec3D a = new Vec3D(layupAxes[0]);
            Vec3D b = new Vec3D(plyAxes[0]);
            Vec3D n = new Vec3D(layupAxes[2]);
            double sin = Vec3D.DotProduct(Vec3D.CrossProduct(a, b), n);
            double cos = Vec3D.DotProduct(a, b);
            return Round(Math.Atan2(sin, cos) * 180 / Math.PI);
        }
        private static double Round(double angleDeg)
        {
            return Math.Round(angleDeg / AngleRoundingDeg) * AngleRoundingDeg;
        }
        private static string GetKey(List<Tuple<int, double>> layers)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var layer in layers)
                sb.Append(layer.Item1).Append(':').Append(layer.Item2.ToString("R", CultureInfo.InvariantCulture)).Append(';');
            return sb.ToString();
        }
        // Discrete orientations
        // Every element takes the nearest stacking point: its reference direction and its layers. The layer materials
        // are the materials of the plies with the same index.
        private static List<CompositeLayupZone> CreateDiscreteZones(FeModel model, CompositeLayup layup,
                                                                    out CompositeLayupOrientation orientation,
                                                                    List<string> warnings)
        {
            List<CompositeLayupZone> zones = new List<CompositeLayupZone>();
            FeMesh mesh = model.Mesh;
            DiscreteOrientationsData data = layup.DiscreteOrientationsData;
            Dictionary<int, double[]> directions = new Dictionary<int, double[]>();
            orientation = new CompositeLayupOrientation(layup, () => directions);
            if (data == null || data.Points == null || data.Points.Count == 0)
            {
                warnings.Add("Composite layup " + layup.Name + ": no discrete orientations are imported.");
                return zones;
            }
            //
            HashSet<int> elementIds = layup.GetElementIds(mesh);
            if (elementIds.Count == 0) elementIds = new HashSet<int>(mesh.Elements.Keys);
            //
            StackingPointTree tree = new StackingPointTree(data.Points);
            Dictionary<int, List<int>> pointElementIds = new Dictionary<int, List<int>>();
            foreach (int elementId in elementIds.OrderBy(id => id))
            {
                FeElement element = mesh.Elements[elementId];
                int nearest = tree.FindNearest(element.GetCG(mesh.Nodes, out _));
                if (!pointElementIds.TryGetValue(nearest, out List<int> ids))
                {
                    ids = new List<int>();
                    pointElementIds.Add(nearest, ids);
                }
                ids.Add(elementId);
                directions[elementId] = data.Points[nearest].MaterialRefDir ?? data.GlobalRosette;
            }
            //
            foreach (var entry in pointElementIds.OrderBy(pair => pair.Key))
            {
                StackingPoint point = data.Points[entry.Key];
                List<CompositeLayer> layers = new List<CompositeLayer>();
                for (int i = 0; i < point.Layers.Count; i++)
                {
                    Ply ply = layup.Plies[Math.Min(i, layup.Plies.Count - 1)];
                    layers.Add(new CompositeLayer(ply.MaterialName, point.Layers[i].Thickness, point.Layers[i].Angle));
                }
                if (layers.Count == 0) continue;
                string name = layup.Name + "_Point-" + (point.Id > 0 ? point.Id : entry.Key + 1);
                zones.Add(new CompositeLayupZone(layup, entry.Value.ToArray(), CreateSection(name, layup, layers)));
            }
            return zones;
        }
        // Draping
        // Runs the kinematic draping simulation of the draping ply and returns the draped warp direction of every element
        // of the layup: the direction of the nearest draped cell.
        public static Dictionary<int, double[]> ComputeDrapingDirections(FeMesh mesh, CompositeLayup layup, string logPath,
                                                                         out DrapingResult result)
        {
            result = null;
            Dictionary<int, double[]> directions = new Dictionary<int, double[]>();
            if (layup.Plies.Count == 0) return directions;
            //
            int plyIndex = Math.Min(layup.DrapingPlyIndex, layup.Plies.Count - 1);
            Ply ply = layup.Plies[plyIndex];
            int[] regionElementIds = CompositeLayup.GetRegionElementIds(mesh, ply.RegionName);
            if (regionElementIds == null || regionElementIds.Length == 0) return directions;
            // Surface of the draped ply
            Dictionary<int, int> nodeIdIndex = new Dictionary<int, int>();
            List<double> coordinates = new List<double>();
            List<int> elementNodeCounts = new List<int>();
            List<int> elementNodeIndices = new List<int>();
            foreach (int elementId in regionElementIds)
            {
                if (!mesh.Elements.TryGetValue(elementId, out FeElement element)) continue;
                int[] cornerIds = GetCornerNodeIds(element);
                if (cornerIds.Length < 3) continue;
                elementNodeCounts.Add(cornerIds.Length);
                foreach (int nodeId in cornerIds)
                {
                    if (!nodeIdIndex.TryGetValue(nodeId, out int index))
                    {
                        index = nodeIdIndex.Count;
                        nodeIdIndex.Add(nodeId, index);
                        double[] coor = mesh.Nodes[nodeId].Coor;
                        coordinates.Add(coor[0]);
                        coordinates.Add(coor[1]);
                        coordinates.Add(coor[2]);
                    }
                    elementNodeIndices.Add(index);
                }
            }
            if (elementNodeCounts.Count == 0) return directions;
            //
            result = KinDrape.Run(coordinates.ToArray(), elementNodeCounts.ToArray(), elementNodeIndices.ToArray(),
                                  ply.Angle.Value, layup.DrapingCellSize.Value, 0, logPath);
            if (result == null) return directions;
            // One stacking point per draped cell with the warp direction of the cell
            List<StackingPoint> points = new List<StackingPoint>();
            for (int r = 0; r < result.GridRows - 1; r++)
            {
                for (int c = 0; c < result.GridCols - 1; c++)
                {
                    if (!result.NodeOnMesh[r, c] || !result.NodeOnMesh[r, c + 1] ||
                        !result.NodeOnMesh[r + 1, c] || !result.NodeOnMesh[r + 1, c + 1]) continue;
                    double[] p00 = GetGridNode(result, r, c);
                    double[] p10 = GetGridNode(result, r + 1, c);
                    double[] p01 = GetGridNode(result, r, c + 1);
                    double[] p11 = GetGridNode(result, r + 1, c + 1);
                    if (double.IsNaN(p00[0]) || double.IsNaN(p10[0]) || double.IsNaN(p01[0]) || double.IsNaN(p11[0]))
                        continue;
                    Vec3D warp = new Vec3D(p10[0] - p00[0], p10[1] - p00[1], p10[2] - p00[2]);
                    if (warp.Normalize() < 1e-12) continue;
                    points.Add(new StackingPoint
                    {
                        Id = points.Count + 1,
                        Position = new double[] { (p00[0] + p10[0] + p01[0] + p11[0]) / 4,
                                                  (p00[1] + p10[1] + p01[1] + p11[1]) / 4,
                                                  (p00[2] + p10[2] + p01[2] + p11[2]) / 4 },
                        MaterialRefDir = warp.Coor,
                        Layers = new List<StackingLayer>()
                    });
                }
            }
            if (points.Count == 0) return directions;
            //
            StackingPointTree tree = new StackingPointTree(points);
            foreach (int elementId in layup.GetElementIds(mesh))
            {
                FeElement element = mesh.Elements[elementId];
                directions[elementId] = points[tree.FindNearest(element.GetCG(mesh.Nodes, out _))].MaterialRefDir;
            }
            return directions;
        }
        private static double[] GetGridNode(DrapingResult result, int row, int col)
        {
            return new double[] { result.NodePositions[row, col, 0], result.NodePositions[row, col, 1],
                                  result.NodePositions[row, col, 2] };
        }
        // Corner nodes of a shell element, or of the first face of a solid element
        public static int[] GetCornerNodeIds(FeElement element)
        {
            int n = element.NodeIds.Length;
            if (n <= 4) return element.NodeIds;
            if (n == 6 || n == 10) return new int[] { element.NodeIds[0], element.NodeIds[1], element.NodeIds[2] };
            if (n == 8 || n == 20) return new int[] { element.NodeIds[0], element.NodeIds[1], element.NodeIds[2],
                                                      element.NodeIds[3] };
            return element.NodeIds;
        }
    }

    // Nearest stacking point search (k-d tree)
    internal class StackingPointTree
    {
        // Variables
        private readonly List<StackingPoint> _points;
        private readonly int[] _indices;


        // Constructor
        public StackingPointTree(List<StackingPoint> points)
        {
            _points = points;
            _indices = Enumerable.Range(0, points.Count).ToArray();
            Build(0, _indices.Length - 1, 0);
        }


        // Methods
        private void Build(int lo, int hi, int axis)
        {
            if (lo >= hi) return;
            int mid = (lo + hi) / 2;
            Select(lo, hi, mid, axis);
            Build(lo, mid - 1, (axis + 1) % 3);
            Build(mid + 1, hi, (axis + 1) % 3);
        }
        private void Select(int lo, int hi, int k, int axis)
        {
            while (lo < hi)
            {
                double pivot = _points[_indices[hi]].Position[axis];
                int i = lo;
                for (int j = lo; j < hi; j++)
                {
                    if (_points[_indices[j]].Position[axis] <= pivot)
                    {
                        (_indices[i], _indices[j]) = (_indices[j], _indices[i]);
                        i++;
                    }
                }
                (_indices[i], _indices[hi]) = (_indices[hi], _indices[i]);
                if (i == k) return;
                if (i < k) lo = i + 1;
                else hi = i - 1;
            }
        }
        public int FindNearest(double[] query)
        {
            int best = _indices[_indices.Length / 2];
            double bestDistance = Distance2(query, _points[best].Position);
            Search(0, _indices.Length - 1, 0, query, ref best, ref bestDistance);
            return best;
        }
        private void Search(int lo, int hi, int axis, double[] q, ref int best, ref double bestDistance)
        {
            if (lo > hi) return;
            int mid = (lo + hi) / 2;
            int pointIndex = _indices[mid];
            double d = Distance2(q, _points[pointIndex].Position);
            if (d < bestDistance)
            {
                bestDistance = d;
                best = pointIndex;
            }
            double diff = q[axis] - _points[pointIndex].Position[axis];
            int next = (axis + 1) % 3;
            if (diff <= 0)
            {
                Search(lo, mid - 1, next, q, ref best, ref bestDistance);
                if (diff * diff < bestDistance) Search(mid + 1, hi, next, q, ref best, ref bestDistance);
            }
            else
            {
                Search(mid + 1, hi, next, q, ref best, ref bestDistance);
                if (diff * diff < bestDistance) Search(lo, mid - 1, next, q, ref best, ref bestDistance);
            }
        }
        private static double Distance2(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
