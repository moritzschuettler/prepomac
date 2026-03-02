using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;
using CaeModel;
using FileInOut.Output.Calculix;

namespace FileInOut.Output
{
    internal class CompositeLayupWriter
    {
        // Variables
        private CompositeLayup _layup;
        private FeMesh _mesh;
        private List<FeElementSet> _itaElementSets;
        private List<CalOrientation> _orientationKeywords;
        private List<CalCompositeShellSection> _sectionKeywords;
        private List<string> _warnings;
        private const double NormalAngleTolDeg = 5.0;


        // Properties
        public List<FeElementSet> ItaElementSets { get { return _itaElementSets; } }
        public List<CalOrientation> OrientationKeywords { get { return _orientationKeywords; } }
        public List<CalCompositeShellSection> SectionKeywords { get { return _sectionKeywords; } }
        public List<string> Warnings { get { return _warnings; } }


        // Constructor
        public CompositeLayupWriter(CompositeLayup layup, FeMesh mesh)
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
            if (_layup.Plies == null || _layup.Plies.Count == 0)
                return;

            // 1. Build per-ply element hash sets; skip plies with missing/invalid region
            List<int> validPlyIdx = new List<int>();
            List<HashSet<int>> plyElements = new List<HashSet<int>>();
            for (int i = 0; i < _layup.Plies.Count; i++)
            {
                Ply ply = _layup.Plies[i];
                if (string.IsNullOrEmpty(ply.Region))
                    continue;
                if (!_mesh.ElementSets.ContainsKey(ply.Region))
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': element set '{2}' not found in mesh.",
                        _layup.Name, ply.Name, ply.Region));
                    continue;
                }
                validPlyIdx.Add(i);
                plyElements.Add(new HashSet<int>(_mesh.ElementSets[ply.Region].Labels));
            }
            if (validPlyIdx.Count == 0)
                return;

            // 2. Collect all elements and compute ply-coverage signatures
            HashSet<int> allElements = new HashSet<int>();
            foreach (var s in plyElements) allElements.UnionWith(s);

            Dictionary<string, List<int>> sigElements = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> sigPlyIdx = new Dictionary<string, List<int>>();
            foreach (int elemId in allElements)
            {
                List<int> covered = new List<int>();
                for (int j = 0; j < validPlyIdx.Count; j++)
                    if (plyElements[j].Contains(elemId)) covered.Add(validPlyIdx[j]);
                string sig = string.Join(",", covered);
                if (!sigElements.ContainsKey(sig))
                {
                    sigElements[sig] = new List<int>();
                    sigPlyIdx[sig] = covered;
                }
                sigElements[sig].Add(elemId);
            }

            // 3. For each ITA: group elements by normal direction, then generate element sets,
            //    orientations (local rosette), and shell section keywords
            int itaN = 1;
            foreach (var entry in sigElements)
            {
                string itaName = _layup.Name + "_ITA_" + itaN++;
                List<int> itaPlyIndices = sigPlyIdx[entry.Key];

                // Group ITA elements by similar normal direction within angular tolerance
                List<Vec3D> groupNormals = new List<Vec3D>();
                List<List<int>> groupElements = new List<List<int>>();
                foreach (int elemId in entry.Value)
                {
                    Vec3D normal = ComputeElementNormal(_mesh.Elements[elemId], _mesh.Nodes);
                    bool assigned = false;
                    for (int g = 0; g < groupNormals.Count; g++)
                    {
                        // Use abs dot product so anti-parallel normals merge into same group
                        double dot = Math.Abs(normal.X * groupNormals[g].X +
                                              normal.Y * groupNormals[g].Y +
                                              normal.Z * groupNormals[g].Z);
                        double angleDeg = Math.Acos(Math.Min(1.0, dot)) * (180.0 / Math.PI);
                        if (angleDeg <= NormalAngleTolDeg)
                        {
                            groupElements[g].Add(elemId);
                            assigned = true;
                            break;
                        }
                    }
                    if (!assigned)
                    {
                        groupNormals.Add(normal);
                        groupElements.Add(new List<int> { elemId });
                    }
                }

                // Create element set + orientations + section for each normal group
                for (int g = 0; g < groupNormals.Count; g++)
                {
                    string setName = groupNormals.Count == 1 ? itaName : itaName + "_G" + (g + 1);
                    _itaElementSets.Add(new FeElementSet(setName, groupElements[g].ToArray()));

                    Vec3D localX, localY;
                    ComputeLocalRosette(groupNormals[g], out localX, out localY);

                    List<CompositeShellPlyData> pliesData = new List<CompositeShellPlyData>();
                    foreach (int plyIdx in itaPlyIndices)
                    {
                        Ply ply = _layup.Plies[plyIdx];
                        string oriName = string.Format("Ori_{0}_P{1}", setName, plyIdx);
                        _orientationKeywords.Add(
                            new CalOrientation(oriName, localX.Coor, localY.Coor, ply.RotationAngle));
                        pliesData.Add(new CompositeShellPlyData(ply.Thickness, ply.Material, oriName));
                    }
                    _sectionKeywords.Add(new CalCompositeShellSection(setName, null, pliesData));
                }
            }
        }

        private static Vec3D ComputeElementNormal(FeElement element, Dictionary<int, FeNode> nodes)
        {
            if (!(element is FeElement2D))
                return new Vec3D(0, 0, 1);
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
            // Project global X (1,0,0) onto the plane perpendicular to normal
            double dot = normal.X;
            localX = new Vec3D(1.0 - dot * normal.X, -dot * normal.Y, -dot * normal.Z);
            double len = localX.Normalize();
            if (len < 1e-6)
            {
                // Normal is nearly parallel to global X; use global Y (0,1,0) as fallback reference
                dot = normal.Y;
                localX = new Vec3D(-dot * normal.X, 1.0 - dot * normal.Y, -dot * normal.Z);
                localX.Normalize();
            }
            localY = Vec3D.CrossProduct(normal, localX);
        }
    }
}
