// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
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
        private List<CalLayupOrientation> _orientationKeywords;
        private List<CalLayupShellSection> _sectionKeywords;
        private List<CalCompositeSolidSection> _solidSectionKeywords;
        private List<CalElementSet> _solidParentElementSets;
        private List<string> _warnings;
        private string _logText;


        // Properties
        public List<FeElementSet> ItaElementSets { get { return _itaElementSets; } }
        public List<CalLayupOrientation> OrientationKeywords { get { return _orientationKeywords; } }
        public List<CalLayupShellSection> SectionKeywords { get { return _sectionKeywords; } }
        public List<CalCompositeSolidSection> SolidSectionKeywords { get { return _solidSectionKeywords; } }
        public List<CalElementSet> SolidParentElementSets { get { return _solidParentElementSets; } }
        public List<string> Warnings { get { return _warnings; } }
        public string LogText { get { return _logText; } }


        // Constructor
        public DiscreteOrientationsWriter(CompositeLayup layup, FeMesh mesh)
        {
            _layup = layup;
            _mesh = mesh;
            _itaElementSets = new List<FeElementSet>();
            _orientationKeywords = new List<CalLayupOrientation>();
            _sectionKeywords = new List<CalLayupShellSection>();
            _solidSectionKeywords = new List<CalCompositeSolidSection>();
            _solidParentElementSets = new List<CalElementSet>();
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
            if (_layup.ElementType == CompositeElementType.ConventionalShell)
            {
                ProcessShell(data);
            }
            else if (_layup.ElementType == CompositeElementType.Solid)
            {
                ProcessSolid(data);
            }
            else
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': discrete orientations are only supported for " +
                    "conventional shell and solid layups; no sections were generated.",
                    _layup.Name));
            }
        }

        private void ProcessShell(DiscreteOrientationsData data)
        {
            string material = _layup.Plies != null && _layup.Plies.Count > 0 ? _layup.Plies[0].Material : null;
            if (string.IsNullOrEmpty(material))
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': discrete orientations require a material on the first ply; " +
                    "no sections were generated.",
                    _layup.Name));
                return;
            }
            if (_layup.Plies.Select(p => p.Material).Distinct().Count() > 1)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': plies use different materials; the first ply material '{1}' " +
                    "is applied to all discrete orientation layers.",
                    _layup.Name, material));
            }

            // Build KD-tree
            KdTree3D tree = new KdTree3D(data.Points);

            // Collect all elements from ply regions
            HashSet<int> allElements = CollectElements();
            if (allElements.Count == 0)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': discrete orientations require at least one ply with a " +
                    "valid element set region; no sections were generated.",
                    _layup.Name));
                return;
            }

            // Map each shell element to nearest stacking point
            Dictionary<int, List<int>> pointToElements = new Dictionary<int, List<int>>();
            int mapped = 0;
            int skipped = 0;

            foreach (int elemId in allElements)
            {
                FeElement element = _mesh.Elements[elemId];
                if (!(element is FeElement2D))
                {
                    skipped++;
                    continue;
                }
                double[] cg = element.GetCG(_mesh.Nodes, out _);
                int nearest = tree.FindNearest(cg);
                if (!pointToElements.ContainsKey(nearest))
                    pointToElements[nearest] = new List<int>();
                pointToElements[nearest].Add(elemId);
                mapped++;
            }
            if (skipped > 0)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': {1} non-shell element(s) in the ply regions were skipped " +
                    "by the discrete orientations mapping.",
                    _layup.Name, skipped));
            }
            if (mapped == 0) return;

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

                // One orientation per distinct layer angle — same base frame, different rotation angle
                List<CompositeShellPlyData> pliesData = new List<CompositeShellPlyData>();
                Dictionary<double, string> angleToOriName = new Dictionary<double, string>();
                foreach (StackingLayer layer in sp.Layers)
                {
                    string oriName;
                    if (!angleToOriName.TryGetValue(layer.Angle, out oriName))
                    {
                        oriName = string.Format("Ori_{0}_{1}", setName, angleToOriName.Count + 1);
                        angleToOriName.Add(layer.Angle, oriName);
                        _orientationKeywords.Add(
                            new CalLayupOrientation(oriName, oriX.Coor, oriY.Coor, layer.Angle, normalAxis));
                    }
                    pliesData.Add(new CompositeShellPlyData(layer.Thickness, material, oriName));
                }

                _sectionKeywords.Add(new CalLayupShellSection(setName, null, pliesData, null));
            }

            _logText = BuildLog(data, allElements.Count, mapped, skipped, pointToElements);
        }

        private void ProcessSolid(DiscreteOrientationsData data)
        {
            if (_layup.Plies == null || _layup.Plies.Count == 0) return;

            // Build KD-tree
            KdTree3D tree = new KdTree3D(data.Points);
            int normalAxis = (int)_layup.NormalDirection + 1;

            int totalElements = 0;
            int totalMapped = 0;
            int totalSkipped = 0;
            HashSet<int> parentElementIds = new HashSet<int>();
            List<string> plyLogLines = new List<string>();

            // Ply k's region contains the elements of physical layer k; each element picks up
            // Layers[k] (the angle) from its nearest stacking point.
            for (int plyIdx = 0; plyIdx < _layup.Plies.Count; plyIdx++)
            {
                Ply ply = _layup.Plies[plyIdx];
                if (string.IsNullOrEmpty(ply.Region) || !_mesh.ElementSets.ContainsKey(ply.Region))
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': element set '{2}' not found in mesh; " +
                        "the ply was skipped by the discrete orientations mapping.",
                        _layup.Name, ply.Name, ply.Region));
                    continue;
                }
                if (string.IsNullOrEmpty(ply.Material))
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': no material assigned; " +
                        "the ply was skipped by the discrete orientations mapping.",
                        _layup.Name, ply.Name));
                    continue;
                }

                // Map each solid element of the ply region to its nearest stacking point
                HashSet<int> elemIds = GetElementIds(ply.Region);
                totalElements += elemIds.Count;
                Dictionary<int, List<int>> pointToElements = new Dictionary<int, List<int>>();
                int skipped = 0;
                foreach (int elemId in elemIds)
                {
                    FeElement element = _mesh.Elements[elemId];
                    if (!(element is FeElement3D))
                    {
                        skipped++;
                        continue;
                    }
                    double[] cg = element.GetCG(_mesh.Nodes, out _);
                    int nearest = tree.FindNearest(cg);
                    if (!pointToElements.ContainsKey(nearest))
                        pointToElements[nearest] = new List<int>();
                    pointToElements[nearest].Add(elemId);
                }
                totalSkipped += skipped;
                if (skipped > 0)
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': {2} non-solid element(s) in the ply region " +
                        "were skipped by the discrete orientations mapping.",
                        _layup.Name, ply.Name, skipped));
                }
                if (pointToElements.Count == 0) continue;

                // One element set + one orientation + one solid section per stacking point group
                int towDropGroups = 0;
                int groupN = 1;
                foreach (var entry in pointToElements)
                {
                    StackingPoint sp = data.Points[entry.Key];
                    string setName = string.Format("{0}_DO_P{1}_{2}", _layup.Name, plyIdx, groupN++);
                    _itaElementSets.Add(new FeElementSet(setName, entry.Value.ToArray()));
                    parentElementIds.UnionWith(entry.Value);
                    totalMapped += entry.Value.Count;

                    // Average through-thickness direction over the group
                    Vec3D normal = ComputeAverageNormal(entry.Value);

                    // Tow-drop: the nearest stacking point has fewer layers than the ply index
                    // requires; fall back to the ply rotation angle with the global rosette
                    bool towDrop = sp.Layers == null || sp.Layers.Count <= plyIdx;
                    if (towDrop) towDropGroups++;
                    double[] mrd = towDrop || sp.MaterialRefDir == null
                        ? data.GlobalRosette : sp.MaterialRefDir;
                    double angle = towDrop ? ply.RotationAngle : sp.Layers[plyIdx].Angle;

                    // Project the reference direction onto the plane perpendicular to the normal
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
                        ComputeLocalRosette(normal, out oriX, out oriY);
                    }

                    string oriName = "Ori_" + setName;
                    _orientationKeywords.Add(
                        new CalLayupOrientation(oriName, oriX.Coor, oriY.Coor, angle, normalAxis));
                    _solidSectionKeywords.Add(
                        new CalCompositeSolidSection(setName, ply.Material, oriName));
                }
                if (towDropGroups > 0)
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': {2} element group(s) mapped to stacking " +
                        "points with fewer than {3} layer(s); the ply rotation angle {4}° with the " +
                        "global rosette was used instead (tow-drop fallback).",
                        _layup.Name, ply.Name, towDropGroups, plyIdx + 1, ply.RotationAngle));
                }
                plyLogLines.Add(string.Format("  Ply {0} '{1}': {2} elements in {3} group(s)",
                    plyIdx + 1, ply.Name, elemIds.Count - skipped, pointToElements.Count));
            }

            if (totalMapped == 0)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': discrete orientations require at least one ply with a " +
                    "valid element set region; no sections were generated.",
                    _layup.Name));
                return;
            }

            // Emit explicit parent elset with direct element IDs (union of all ply sets) so
            // loads applied to the whole solid part are correctly resolved by CalculiX.
            if (!string.IsNullOrEmpty(_layup.SolidPartName) && parentElementIds.Count > 0)
            {
                _solidParentElementSets.Add(
                    new CalElementSet(new FeElementSet(_layup.SolidPartName, parentElementIds.ToArray())));
            }

            _logText = BuildSolidLog(data, totalElements, totalMapped, totalSkipped, plyLogLines);
        }

        private HashSet<int> CollectElements()
        {
            HashSet<int> result = new HashSet<int>();
            // Collect elements from plies that have a region
            if (_layup.Plies != null)
            {
                foreach (Ply ply in _layup.Plies)
                {
                    if (!string.IsNullOrEmpty(ply.Region) && _mesh.ElementSets.ContainsKey(ply.Region))
                        result.UnionWith(GetElementIds(ply.Region));
                }
            }
            return result;
        }

        private HashSet<int> GetElementIds(string regionName)
        {
            FeElementSet elSet = _mesh.ElementSets[regionName];
            if (elSet.CreatedFromParts)
            {
                HashSet<int> elementIds = new HashSet<int>();
                foreach (int partId in elSet.Labels)
                    elementIds.UnionWith(_mesh.GetPartFromId(partId).Labels);
                return elementIds;
            }
            return new HashSet<int>(elSet.Labels);
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

        private string BuildSolidLog(DiscreteOrientationsData data, int total, int mapped, int skipped,
                                     List<string> plyLogLines)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Discrete Orientations Mapping Log (solid)");
            sb.AppendLine("Layup: " + _layup.Name);
            sb.AppendLine("XML file: " + data.XmlFilePath);
            sb.AppendLine("Stacking points loaded: " + data.Points.Count);
            sb.AppendLine("Total elements processed: " + total);
            sb.AppendLine("Elements successfully mapped: " + mapped);
            if (skipped > 0) sb.AppendLine("Elements skipped: " + skipped);
            sb.AppendLine();
            sb.AppendLine("Per-ply group counts:");
            foreach (string line in plyLogLines) sb.AppendLine(line);
            return sb.ToString();
        }

        private Vec3D ComputeAverageNormal(List<int> elementIds)
        {
            double nx = 0, ny = 0, nz = 0;
            foreach (int eid in elementIds)
            {
                Vec3D n = CompositeGeometry.ComputeElementNormal(_mesh.Elements[eid], _mesh.Nodes);
                nx += n.X; ny += n.Y; nz += n.Z;
            }
            Vec3D avg = new Vec3D(nx, ny, nz);
            double len = avg.Normalize();
            if (len < 1e-12) avg = new Vec3D(0, 0, 1);
            return avg;
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
