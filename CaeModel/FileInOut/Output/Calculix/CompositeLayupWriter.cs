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
        private List<CalLayupOrientation> _orientationKeywords;
        private List<CalLayupShellSection> _sectionKeywords;
        private List<CalCompositeSolidSection> _solidSectionKeywords;
        private List<CalElementSet> _solidParentElementSets;
        private List<string> _warnings;
        private Dictionary<string, RosetteEvaluator> _rosetteEvaluators;
        private const double NormalAngleTolDeg = 5.0;
        private const int MaxRosetteGroupWarning = 500;


        // Properties
        public List<FeElementSet> ItaElementSets { get { return _itaElementSets; } }
        public List<CalLayupOrientation> OrientationKeywords { get { return _orientationKeywords; } }
        public List<CalLayupShellSection> SectionKeywords { get { return _sectionKeywords; } }
        public List<CalCompositeSolidSection> SolidSectionKeywords { get { return _solidSectionKeywords; } }
        public List<CalElementSet> SolidParentElementSets { get { return _solidParentElementSets; } }
        public List<string> Warnings { get { return _warnings; } }


        // Constructor
        public CompositeLayupWriter(CompositeLayup layup, FeMesh mesh)
            : this(layup, mesh, null)
        {
        }

        public CompositeLayupWriter(CompositeLayup layup, FeMesh mesh,
                                    CaeGlobals.OrderedDictionary<string, Rosette> rosettes)
        {
            _layup = layup;
            _mesh = mesh;
            _itaElementSets = new List<FeElementSet>();
            _orientationKeywords = new List<CalLayupOrientation>();
            _sectionKeywords = new List<CalLayupShellSection>();
            _solidSectionKeywords = new List<CalCompositeSolidSection>();
            _solidParentElementSets = new List<CalElementSet>();
            _warnings = new List<string>();
            _rosetteEvaluators = RosetteEvaluator.BuildEvaluators(layup, rosettes, _warnings);
        }


        // Methods
        public void Process()
        {
            if (_layup.Plies == null || _layup.Plies.Count == 0)
                return;
            if (_layup.ElementType == CompositeElementType.Solid)
            {
                ProcessSolid();
                return;
            }

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
                plyElements.Add(GetElementIds(ply.Region));
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

                // If all plies share the same region and there is only one normal group,
                // reuse the existing region element set instead of creating a redundant ITA set.
                string commonRegion = null;
                if (groupNormals.Count == 1)
                {
                    string first = _layup.Plies[itaPlyIndices[0]].Region;
                    bool allSame = true;
                    for (int j = 1; j < itaPlyIndices.Count; j++)
                    {
                        if (_layup.Plies[itaPlyIndices[j]].Region != first) { allSame = false; break; }
                    }
                    if (allSame) commonRegion = first;
                }

                // Create element set + orientations + section for each normal group
                for (int g = 0; g < groupNormals.Count; g++)
                {
                    // Plies of this ITA whose reference direction comes from a rosette: the elements
                    // must be split further because the direction varies from element to element
                    List<int> rosettePlyIndices = GetRosettePlyIndices(itaPlyIndices);
                    if (rosettePlyIndices.Count > 0)
                    {
                        List<RosetteSubGroup> subGroups = SplitByRosetteDirections(
                            groupElements[g], groupNormals[g], rosettePlyIndices);
                        if (subGroups.Count > MaxRosetteGroupWarning)
                        {
                            _warnings.Add(string.Format(
                                "Composite layup '{0}': the rosette mapping produced {1} element groups; " +
                                "increase the rosette direction tolerance to reduce the size of the input file.",
                                _layup.Name, subGroups.Count));
                        }
                        int subN = 1;
                        foreach (RosetteSubGroup subGroup in subGroups)
                        {
                            string subSetName = groupNormals.Count == 1
                                ? itaName + "_R" + subN : itaName + "_G" + (g + 1) + "_R" + subN;
                            subN++;
                            _itaElementSets.Add(new FeElementSet(subSetName, subGroup.Elements.ToArray()));
                            EmitShellSection(subSetName, itaPlyIndices, groupNormals[g], subGroup.Directions);
                        }
                        continue;
                    }
                    //
                    string setName;
                    if (commonRegion != null)
                    {
                        // All plies share the same region and there is one normal group.
                        // Prefer the part name over the user element set name: the part name is
                        // auto-created as an element set by the *Element keyword and covers every
                        // element of the part, avoiding gaps caused by incomplete user-defined sets
                        // and conflicts with reserved names (e.g. "ALL").
                        // Only use the part name when the ITA covers ALL elements of that part.
                        // When the ITA is a subset (e.g., one zone of a compound part shared by
                        // multiple plies), use commonRegion so each zone gets its own element set.
                        string[] partNames = _mesh.GetPartNamesFromElementIds(groupElements[g].ToArray());
                        if (partNames != null && partNames.Length == 1 &&
                            _mesh.Parts.ContainsKey(partNames[0]) &&
                            _mesh.Parts[partNames[0]].Labels.Length == groupElements[g].Count)
                        {
                            setName = partNames[0];
                            // No new element set needed.
                        }
                        else
                        {
                            // Elements span multiple parts — fall back to the region element set.
                            FeElementSet commonElSet = _mesh.ElementSets[commonRegion];
                            if (commonElSet.CreatedFromParts)
                            {
                                string[] regionPartNames = _mesh.GetPartNamesFromPartIds(commonElSet.Labels);
                                if (regionPartNames != null && regionPartNames.Length == 1)
                                {
                                    setName = regionPartNames[0];
                                }
                                else
                                {
                                    setName = groupNormals.Count == 1 ? itaName : itaName + "_G" + (g + 1);
                                    _itaElementSets.Add(new FeElementSet(setName, groupElements[g].ToArray()));
                                }
                            }
                            else
                            {
                                setName = commonRegion;
                            }
                        }
                    }
                    else
                    {
                        setName = groupNormals.Count == 1 ? itaName : itaName + "_G" + (g + 1);
                        _itaElementSets.Add(new FeElementSet(setName, groupElements[g].ToArray()));
                    }

                    EmitShellSection(setName, itaPlyIndices, groupNormals[g], null);
                }
            }
        }

        // Creates the orientations and the composite shell section of one element set.
        // rosetteDirections holds the reference direction of the plies that use a rosette; the
        // other plies keep the coordinate system or the layup reference direction.
        private void EmitShellSection(string setName, List<int> itaPlyIndices, Vec3D normal,
                                      Dictionary<int, Vec3D> rosetteDirections)
        {
            int normalAxis = (int)_layup.NormalDirection + 1;   // Axis1=1, Axis2=2, Axis3=3
            double additionalAngle = _layup.AdditionalRotationType == AdditionalRotationType.Angle
                ? _layup.AdditionalRotationAngle : 0.0;
            double refAngleRad = additionalAngle * Math.PI / 180.0;
            Vec3D defaultRefX = new Vec3D(Math.Cos(refAngleRad), Math.Sin(refAngleRad), 0.0);
            //
            List<CompositeShellPlyData> pliesData = new List<CompositeShellPlyData>();
            List<CompositeShellPlyData> forwardPlies = new List<CompositeShellPlyData>();
            foreach (int plyIdx in itaPlyIndices)
            {
                Ply ply = _layup.Plies[plyIdx];
                Vec3D refX;
                double plyRotAngle;
                Vec3D rosetteDirection = null;
                if (rosetteDirections != null) rosetteDirections.TryGetValue(plyIdx, out rosetteDirection);
                //
                Vec3D localX, localY;
                if (rosetteDirection != null)
                {
                    // The rosette only replaces the reference direction; the ply rotation angle and
                    // the additional layup rotation are still applied on top of it
                    RosetteEvaluator evaluator = GetPlyEvaluator(plyIdx);
                    Vec3D axisYHint = evaluator != null ? evaluator.AxisY : null;
                    RosetteEvaluator.ProjectOntoPlane(rosetteDirection, normal, axisYHint, out localX, out localY);
                    plyRotAngle = ply.RotationAngle + additionalAngle;
                }
                else
                {
                    if (!string.IsNullOrEmpty(ply.Csys) && ply.Csys != "<layup>")
                    {
                        CoordinateSystem csys;
                        refX = _mesh.CoordinateSystems.TryGetValue(ply.Csys, out csys)
                            ? new Vec3D(csys.DirectionX().Coor)
                            : new Vec3D(1.0, 0.0, 0.0);
                        plyRotAngle = ply.RotationAngle + additionalAngle;
                    }
                    else
                    {
                        refX = defaultRefX;
                        plyRotAngle = ply.RotationAngle;
                    }
                    ComputeLocalRosette(normal, refX, out localX, out localY);
                }
                string oriName = string.Format("Ori_{0}_P{1}", setName, plyIdx);
                _orientationKeywords.Add(
                    new CalLayupOrientation(oriName, localX.Coor, localY.Coor, plyRotAngle, normalAxis));
                forwardPlies.Add(new CompositeShellPlyData(ply.Thickness, ply.Material, oriName));
            }
            pliesData.AddRange(forwardPlies);
            if (_layup.Symmetric)
            {
                for (int k = forwardPlies.Count - 1; k >= 0; k--)
                    pliesData.Add(forwardPlies[k]);
            }
            _sectionKeywords.Add(new CalLayupShellSection(setName, null, pliesData, ComputeOffset()));
        }

        private void ProcessSolid()
        {
            // For solid composites each ply is a separate element set (one layer thick).
            // No ITA grouping: generate one orientation + one solid section per ply.
            double additionalAngle = _layup.AdditionalRotationType == AdditionalRotationType.Angle
                ? _layup.AdditionalRotationAngle : 0.0;
            double refAngleRad = additionalAngle * Math.PI / 180.0;
            Vec3D defaultRefX = new Vec3D(Math.Cos(refAngleRad), Math.Sin(refAngleRad), 0.0);
            int normalAxis = (int)_layup.NormalDirection + 1;   // Axis1=1, Axis2=2, Axis3=3
            //
            var processedPlySetNames = new List<string>();
            var processedPlyElementIds = new HashSet<int>();
            for (int plyIdx = 0; plyIdx < _layup.Plies.Count; plyIdx++)
            {
                Ply ply = _layup.Plies[plyIdx];
                if (string.IsNullOrEmpty(ply.Region))
                    continue;
                if (!_mesh.ElementSets.ContainsKey(ply.Region))
                {
                    _warnings.Add(string.Format(
                        "Composite layup '{0}', ply '{1}': element set '{2}' not found in mesh.",
                        _layup.Name, ply.Name, ply.Region));
                    continue;
                }
                //
                // Compute representative normal from first element in the ply set
                HashSet<int> elemIds = GetElementIds(ply.Region);
                if (elemIds.Count == 0) continue;
                Vec3D normal = ComputeElementNormal(_mesh.Elements[elemIds.First()], _mesh.Nodes);
                //
                // A rosette gives every element its own reference direction, so the ply region is
                // split into groups of elements that share the same direction
                RosetteEvaluator plyEvaluator = GetPlyEvaluator(plyIdx);
                if (plyEvaluator != null)
                {
                    List<int> rosettePlyIndices = new List<int> { plyIdx };
                    List<RosetteSubGroup> subGroups = SplitByRosetteDirections(
                        elemIds.ToList(), normal, rosettePlyIndices);
                    if (subGroups.Count > MaxRosetteGroupWarning)
                    {
                        _warnings.Add(string.Format(
                            "Composite layup '{0}', ply '{1}': the rosette mapping produced {2} element " +
                            "groups; increase the rosette direction tolerance to reduce the size of the " +
                            "input file.", _layup.Name, ply.Name, subGroups.Count));
                    }
                    int subN = 1;
                    foreach (RosetteSubGroup subGroup in subGroups)
                    {
                        string subSetName = string.Format("{0}_P{1}_R{2}", _layup.Name, plyIdx, subN++);
                        _itaElementSets.Add(new FeElementSet(subSetName, subGroup.Elements.ToArray()));
                        //
                        Vec3D subLocalX, subLocalY;
                        RosetteEvaluator.ProjectOntoPlane(subGroup.Directions[plyIdx], normal,
                                                          plyEvaluator.AxisY, out subLocalX, out subLocalY);
                        string subOriName = "Ori_" + subSetName;
                        _orientationKeywords.Add(new CalLayupOrientation(subOriName, subLocalX.Coor, subLocalY.Coor,
                                                                    ply.RotationAngle + additionalAngle, normalAxis));
                        _solidSectionKeywords.Add(
                            new CalCompositeSolidSection(subSetName, ply.Material, subOriName));
                    }
                    processedPlyElementIds.UnionWith(elemIds);
                    continue;
                }
                //
                // Reference fiber direction
                Vec3D refX;
                double plyRotAngle;
                if (!string.IsNullOrEmpty(ply.Csys) && ply.Csys != "<layup>")
                {
                    CoordinateSystem csys;
                    refX = _mesh.CoordinateSystems.TryGetValue(ply.Csys, out csys)
                        ? new Vec3D(csys.DirectionX().Coor)
                        : new Vec3D(1.0, 0.0, 0.0);
                    plyRotAngle = ply.RotationAngle + additionalAngle;
                }
                else
                {
                    refX = defaultRefX;
                    plyRotAngle = ply.RotationAngle;
                }
                Vec3D localX, localY;
                ComputeLocalRosette(normal, refX, out localX, out localY);
                //
                // Resolve element set name (prefer part name for CreatedFromParts sets)
                string setName = ply.Region;
                FeElementSet elSet = _mesh.ElementSets[ply.Region];
                if (elSet.CreatedFromParts)
                {
                    string[] partNames = _mesh.GetPartNamesFromPartIds(elSet.Labels);
                    if (partNames != null && partNames.Length == 1) setName = partNames[0];
                }
                //
                string oriName = string.Format("Ori_{0}_P{1}", _layup.Name, plyIdx);
                _orientationKeywords.Add(
                    new CalLayupOrientation(oriName, localX.Coor, localY.Coor, plyRotAngle, normalAxis));
                _solidSectionKeywords.Add(
                    new CalCompositeSolidSection(setName, ply.Material, oriName));
                processedPlySetNames.Add(setName);
            }
            // Emit explicit parent elset with direct element IDs (union of all ply sets) so
            // loads applied to the whole solid part are correctly resolved by CalculiX.
            // Using element IDs avoids CalculiX issues with set-name references conflicting
            // with the implicit set already created by *Element, Elset=PartName.
            if (!string.IsNullOrEmpty(_layup.SolidPartName) &&
                (processedPlySetNames.Count > 0 || processedPlyElementIds.Count > 0))
            {
                var allElemIds = new HashSet<int>(processedPlyElementIds);
                foreach (var name in processedPlySetNames)
                    allElemIds.UnionWith(GetElementIds(name));
                if (allElemIds.Count > 0)
                    _solidParentElementSets.Add(
                        new CalElementSet(new FeElementSet(_layup.SolidPartName, allElemIds.ToArray())));
            }
        }

        // A set of elements that share the same quantized rosette direction for every rosette ply
        private class RosetteSubGroup
        {
            public List<int> Elements = new List<int>();
            public Dictionary<int, Vec3D> Directions = new Dictionary<int, Vec3D>();
        }

        private RosetteEvaluator GetPlyEvaluator(int plyIndex)
        {
            string name = _layup.GetEffectivePlyRosette(plyIndex);
            if (string.IsNullOrEmpty(name)) return null;
            RosetteEvaluator evaluator;
            return _rosetteEvaluators != null && _rosetteEvaluators.TryGetValue(name, out evaluator)
                ? evaluator : null;
        }

        private List<int> GetRosettePlyIndices(List<int> plyIndices)
        {
            List<int> result = new List<int>();
            foreach (int plyIdx in plyIndices)
                if (GetPlyEvaluator(plyIdx) != null) result.Add(plyIdx);
            return result;
        }

        private double GetDirectionTolerance(List<int> rosettePlyIndices)
        {
            double tolerance = double.MaxValue;
            foreach (int plyIdx in rosettePlyIndices)
            {
                RosetteEvaluator evaluator = GetPlyEvaluator(plyIdx);
                if (evaluator != null && evaluator.Rosette.DirectionToleranceDeg < tolerance)
                    tolerance = evaluator.Rosette.DirectionToleranceDeg;
            }
            return tolerance == double.MaxValue ? 5.0 : tolerance;
        }

        // Groups the elements so that every group can be written with a single *ORIENTATION per ply
        private List<RosetteSubGroup> SplitByRosetteDirections(List<int> elementIds, Vec3D normal,
                                                              List<int> rosettePlyIndices)
        {
            double tolerance = GetDirectionTolerance(rosettePlyIndices);
            Dictionary<string, RosetteSubGroup> groups = new Dictionary<string, RosetteSubGroup>();
            List<RosetteSubGroup> ordered = new List<RosetteSubGroup>();
            int missing = 0;
            //
            foreach (int elementId in elementIds)
            {
                FeElement element;
                if (!_mesh.Elements.TryGetValue(elementId, out element)) continue;
                double size;
                double[] cg = element.GetCG(_mesh.Nodes, out size);
                //
                StringBuilder keyBuilder = new StringBuilder();
                Dictionary<int, Vec3D> directions = new Dictionary<int, Vec3D>();
                foreach (int plyIdx in rosettePlyIndices)
                {
                    RosetteEvaluator evaluator = GetPlyEvaluator(plyIdx);
                    Vec3D direction;
                    if (evaluator == null || !evaluator.TryGetRefDirection(cg, normal, out direction))
                    {
                        missing++;
                        direction = new Vec3D(1, 0, 0);
                    }
                    // Quantize the direction after the projection so that the key matches the frame
                    // that is actually written
                    Vec3D localX, localY;
                    RosetteEvaluator.ProjectOntoPlane(direction, normal,
                                                      evaluator != null ? evaluator.AxisY : null,
                                                      out localX, out localY);
                    keyBuilder.Append(plyIdx).Append(':')
                              .Append(RosetteEvaluator.GetDirectionKey(localX, tolerance)).Append('|');
                    directions.Add(plyIdx, direction);
                }
                //
                string key = keyBuilder.ToString();
                RosetteSubGroup group;
                if (!groups.TryGetValue(key, out group))
                {
                    group = new RosetteSubGroup();
                    group.Directions = directions;
                    groups.Add(key, group);
                    ordered.Add(group);
                }
                group.Elements.Add(elementId);
            }
            if (missing > 0)
            {
                _warnings.Add(string.Format(
                    "Composite layup '{0}': no rosette direction could be evaluated for {1} element/ply " +
                    "combination(s); the global X direction was used for those.", _layup.Name, missing));
            }
            return ordered;
        }

        // The OFFSET parameter of *SHELL SECTION is the distance from the mid-surface to the
        // reference surface expressed in units of the shell thickness, not a length: +0.5 puts the
        // reference surface on the top face of the laminate, -0.5 on the bottom face, 0 (the
        // default, written by leaving the parameter out) on the mid-surface. It must therefore
        // never be multiplied by the laminate thickness.
        private double? ComputeOffset()
        {
            switch (_layup.ShellOffset)
            {
                case ShellOffsetType.TopSurface: return +0.5;
                case ShellOffsetType.BottomSurface: return -0.5;
                // Already a fraction of the thickness: the dialog limits it to [-1, 1]
                case ShellOffsetType.SpecifyRatio: return _layup.OffsetRatio;
                default: return null;   // MiddleSurface
            }
        }

        private static Vec3D ComputeElementNormal(FeElement element, Dictionary<int, FeNode> nodes)
        {
            return CompositeGeometry.ComputeElementNormal(element, nodes);
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

        private static void ComputeLocalRosette(Vec3D normal, Vec3D refX, out Vec3D localX, out Vec3D localY)
        {
            // Project refX onto the plane perpendicular to normal
            double dot = Vec3D.DotProduct(refX, normal);
            localX = new Vec3D(refX.X - dot * normal.X, refX.Y - dot * normal.Y, refX.Z - dot * normal.Z);
            double len = localX.Normalize();
            if (len < 1e-6)
            {
                // refX parallel to normal; try global Y (0,1,0) as fallback
                double dotY = normal.Y;
                localX = new Vec3D(-dotY * normal.X, 1.0 - dotY * normal.Y, -dotY * normal.Z);
                len = localX.Normalize();
                if (len < 1e-6)
                {
                    // Normal also along global Y; use global X (1,0,0) as last resort
                    double dotX = normal.X;
                    localX = new Vec3D(1.0 - dotX * normal.X, -dotX * normal.Y, -dotX * normal.Z);
                    localX.Normalize();
                }
            }
            localY = Vec3D.CrossProduct(normal, localX);
        }
    }
}
