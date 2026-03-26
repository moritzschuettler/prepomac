using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CaeMesh;
using CaeGlobals;
using FileInOut.Output.Calculix;

namespace CaeModel
{
    internal class DiscreteOrientationsWriter
    {
        // Variables
        private CompositeLayup _layup;
        private FeMesh _mesh;
        private List<FeElementSet> _itaElementSets;
        private List<CalOrientation> _orientationKeywords;
        private List<CalCompositeShellSection> _sectionKeywords;
        private List<string> _warnings;
        private string _logText;


        // Properties
        public List<FeElementSet> ItaElementSets { get { return _itaElementSets; } }
        public List<CalOrientation> OrientationKeywords { get { return _orientationKeywords; } }
        public List<CalCompositeShellSection> SectionKeywords { get { return _sectionKeywords; } }
        public List<string> Warnings { get { return _warnings; } }
        public string LogText { get { return _logText; } }


        // Constructor
        public DiscreteOrientationsWriter(CompositeLayup layup, FeMesh mesh)
        {
            _layup = layup;
            _mesh = mesh;
            _itaElementSets = new List<FeElementSet>();
            _orientationKeywords = new List<CalOrientation>();
            _sectionKeywords = new List<CalCompositeShellSection>();
            _warnings = new List<string>();
        }


        // Methods
        public void Process()
        {
            DiscreteOrientationsData data = _layup.DiscreteOrientationsData;
            if (data == null || data.Points == null || data.Points.Count == 0)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': discrete orientations enabled but no XML data loaded.",
                    _layup.Name));
                return;
            }

            // Build KD-tree
            KdTree3D tree = new KdTree3D(data.Points);

            // Collect all elements from ply regions (or all elements in the mesh if region is empty)
            HashSet<int> allElements = CollectElements();
            if (allElements.Count == 0) return;

            // Map each element to nearest stacking point
            Dictionary<int, List<int>> pointToElements = new Dictionary<int, List<int>>();
            int mapped = 0;
            int skipped = 0;
            List<string> warnings = new List<string>();

            foreach (int elemId in allElements)
            {
                FeElement element = _mesh.Elements[elemId];
                double[] cg = element.GetCG(_mesh.Nodes, out _);
                int nearest = tree.FindNearest(cg);
                if (!pointToElements.ContainsKey(nearest))
                    pointToElements[nearest] = new List<int>();
                pointToElements[nearest].Add(elemId);
                mapped++;
            }

            // Generate keywords: one element set + one shell section per stacking point group.
            // All layers of a stacking share the same base orientation (stacking MaterialRefDir
            // projected onto the group average normal, or GlobalRosette when not specified).
            int groupN = 1;
            foreach (var entry in pointToElements)
            {
                StackingPoint sp = data.Points[entry.Key];

                string setName = string.Format("{0}_DO_{1}", _layup.Name, groupN++);
                _itaElementSets.Add(new FeElementSet(setName, entry.Value.ToArray()));

                // Average normal over all elements in the group
                Vec3D normal = ComputeAverageNormal(entry.Value);
                Vec3D localX, localY;
                ComputeLocalRosette(normal, out localX, out localY);

                // Use stacking MaterialRefDir when defined, otherwise fall back to GlobalRosette
                double[] mrd = sp.MaterialRefDir != null ? sp.MaterialRefDir : data.GlobalRosette;
                double dotN = mrd[0] * normal.X + mrd[1] * normal.Y + mrd[2] * normal.Z;
                Vec3D projRef = new Vec3D(mrd[0] - dotN * normal.X,
                                         mrd[1] - dotN * normal.Y,
                                         mrd[2] - dotN * normal.Z);
                double projLen = projRef.Normalize();

                Vec3D oriX, oriY;
                if (projLen > 1e-6)
                {
                    oriX = projRef;
                    oriY = Vec3D.CrossProduct(normal, oriX);
                }
                else
                {
                    // Ref dir is parallel to normal — fall back to local rosette
                    oriX = localX;
                    oriY = localY;
                }

                int normalAxis = (int)_layup.NormalDirection + 1;

                // One orientation per layer — same base frame, different rotation angle per layer
                List<CompositeShellPlyData> pliesData = new List<CompositeShellPlyData>();
                foreach (StackingLayer layer in sp.Layers)
                {
                    string oriName = string.Format("Ori_{0}_A{1:F1}", setName,
                        layer.Angle).Replace("-", "m").Replace(".", "p");
                    _orientationKeywords.Add(
                        new CalOrientation(oriName, oriX.Coor, oriY.Coor, layer.Angle, normalAxis));
                    string material = (_layup.Plies != null && _layup.Plies.Count > 0)
                        ? _layup.Plies[0].Material : "";
                    pliesData.Add(new CompositeShellPlyData(layer.Thickness, material, oriName));
                }

                _sectionKeywords.Add(new CalCompositeShellSection(setName, null, pliesData, null));
            }

            _logText = BuildLog(data, allElements.Count, mapped, skipped, pointToElements);
        }

        private HashSet<int> CollectElements()
        {
            HashSet<int> result = new HashSet<int>();
            // Collect elements from plies that have a region
            bool anyRegion = false;
            if (_layup.Plies != null)
            {
                foreach (Ply ply in _layup.Plies)
                {
                    if (!string.IsNullOrEmpty(ply.Region) && _mesh.ElementSets.ContainsKey(ply.Region))
                    {
                        anyRegion = true;
                        FeElementSet elSet = _mesh.ElementSets[ply.Region];
                        if (elSet.CreatedFromParts)
                        {
                            foreach (int partId in elSet.Labels)
                                foreach (int eid in _mesh.GetPartFromId(partId).Labels)
                                    result.Add(eid);
                        }
                        else
                        {
                            foreach (int eid in elSet.Labels) result.Add(eid);
                        }
                    }
                }
            }
            // If no ply regions defined, use all elements
            if (!anyRegion)
            {
                foreach (int eid in _mesh.Elements.Keys) result.Add(eid);
            }
            return result;
        }

        private string BuildLog(DiscreteOrientationsData data, int total, int mapped, int skipped,
                                Dictionary<int, List<int>> pointToElements)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Discrete Orientations Mapping Log");
            sb.AppendLine("Layup: " + _layup.Name);
            sb.AppendLine("XML file: " + data.XmlFilePath);
            sb.AppendLine("Stacking points loaded: " + data.Points.Count);
            sb.AppendLine("Total elements processed: " + total);
            sb.AppendLine("Elements successfully mapped: " + mapped);
            if (skipped > 0) sb.AppendLine("Elements skipped: " + skipped);
            sb.AppendLine();
            sb.AppendLine("Stacking point element counts:");
            foreach (var kv in pointToElements)
            {
                double[] pos = data.Points[kv.Key].Position;
                sb.AppendLine(string.Format("  Point {0} ({1:F3}, {2:F3}, {3:F3}) -> {4} elements",
                    kv.Key + 1, pos[0], pos[1], pos[2], kv.Value.Count));
            }
            return sb.ToString();
        }

        private Vec3D ComputeAverageNormal(List<int> elementIds)
        {
            double nx = 0, ny = 0, nz = 0;
            foreach (int eid in elementIds)
            {
                Vec3D n = ComputeElementNormal(_mesh.Elements[eid], _mesh.Nodes);
                nx += n.X; ny += n.Y; nz += n.Z;
            }
            Vec3D avg = new Vec3D(nx, ny, nz);
            double len = avg.Normalize();
            if (len < 1e-12) avg = new Vec3D(0, 0, 1);
            return avg;
        }

        private static Vec3D ComputeElementNormal(FeElement element, Dictionary<int, FeNode> nodes)
        {
            if (!(element is FeElement2D)) return new Vec3D(0, 0, 1);
            FeNode n0 = nodes[element.NodeIds[0]];
            FeNode n1 = nodes[element.NodeIds[1]];
            FeNode n2 = nodes[element.NodeIds[2]];
            Vec3D v1 = new Vec3D(n1.X - n0.X, n1.Y - n0.Y, n1.Z - n0.Z);
            Vec3D v2 = new Vec3D(n2.X - n0.X, n2.Y - n0.Y, n2.Z - n0.Z);
            Vec3D normal = Vec3D.CrossProduct(v1, v2);
            normal.Normalize();
            return normal;
        }

        private static void ComputeLocalRosette(Vec3D normal, out Vec3D localX, out Vec3D localY)
        {
            double dot = normal.X;
            localX = new Vec3D(1.0 - dot * normal.X, -dot * normal.Y, -dot * normal.Z);
            double len = localX.Normalize();
            if (len < 1e-6)
            {
                dot = normal.Y;
                localX = new Vec3D(-dot * normal.X, 1.0 - dot * normal.Y, -dot * normal.Z);
                localX.Normalize();
            }
            localY = Vec3D.CrossProduct(normal, localX);
        }


        // Nested KD-tree for 3-D nearest-neighbor lookup
        private class KdTree3D
        {
            private List<StackingPoint> _points;
            private int[] _indices;

            public KdTree3D(List<StackingPoint> points)
            {
                _points = points;
                _indices = new int[points.Count];
                for (int i = 0; i < points.Count; i++) _indices[i] = i;
                Build(_indices, 0, _indices.Length - 1, 0);
            }

            private void Build(int[] idx, int lo, int hi, int axis)
            {
                if (lo >= hi) return;
                int mid = (lo + hi) / 2;
                // Partial sort: median at mid
                PartialSort(idx, lo, hi, mid, axis);
                Build(idx, lo, mid - 1, (axis + 1) % 3);
                Build(idx, mid + 1, hi, (axis + 1) % 3);
            }

            private void PartialSort(int[] idx, int lo, int hi, int k, int axis)
            {
                while (lo < hi)
                {
                    int pivot = Partition(idx, lo, hi, axis);
                    if (pivot == k) return;
                    if (pivot < k) lo = pivot + 1;
                    else hi = pivot - 1;
                }
            }

            private int Partition(int[] idx, int lo, int hi, int axis)
            {
                double pivot = _points[idx[hi]].Position[axis];
                int i = lo;
                for (int j = lo; j < hi; j++)
                {
                    if (_points[idx[j]].Position[axis] <= pivot)
                    {
                        int tmp = idx[i]; idx[i] = idx[j]; idx[j] = tmp;
                        i++;
                    }
                }
                int t = idx[i]; idx[i] = idx[hi]; idx[hi] = t;
                return i;
            }

            public int FindNearest(double[] query)
            {
                int best = _indices[(_indices.Length) / 2];
                double bestDist = Dist2(query, _points[best].Position);
                Search(_indices, 0, _indices.Length - 1, 0, query, ref best, ref bestDist);
                return best;
            }

            private void Search(int[] idx, int lo, int hi, int axis, double[] q, ref int best, ref double bestDist)
            {
                if (lo > hi) return;
                int mid = (lo + hi) / 2;
                int pointIdx = idx[mid];
                double d = Dist2(q, _points[pointIdx].Position);
                if (d < bestDist) { bestDist = d; best = pointIdx; }

                double diff = q[axis] - _points[pointIdx].Position[axis];
                int near = diff <= 0 ? lo : mid + 1;
                int far = diff <= 0 ? mid + 1 : lo;
                int nearHi = diff <= 0 ? mid - 1 : hi;
                int farHi = diff <= 0 ? hi : mid - 1;

                Search(idx, near, nearHi, (axis + 1) % 3, q, ref best, ref bestDist);
                if (diff * diff < bestDist)
                    Search(idx, far, farHi, (axis + 1) % 3, q, ref best, ref bestDist);
            }

            private static double Dist2(double[] a, double[] b)
            {
                double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
                return dx * dx + dy * dy + dz * dz;
            }
        }
    }
}
