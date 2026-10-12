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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using System.IO;
using FileInOut.Output.Calculix;
using CaeGlobals;
using Microsoft.SqlServer.Server;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;
using System.Security;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FileInOut.Output
{
    [Serializable]
    public static class CalculixFileWriter
    {
        // Variables                                                                                                                
        private const double SmallestEquationCoefficient = 1E-9;
        private const double AxisymmetricSegmentsPerRevolution = 180;
        private static readonly Regex _nameTokenRegex = new Regex("[^,=\\s\"]+", RegexOptions.Compiled);


        // Methods                                                                                                                  
        static public void Write(string fileName, FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                 bool binaryResultsFile, Dictionary<int, double[]> deformations = null)
        {
            // Copy model
            FeModel copy = model.DeepCopy();
            string[] inactivePartNames = copy.Mesh.GetInactivePartNames();
            copy.Mesh.RemoveParts(inactivePartNames, out _, false);
            //
            List<CalculixKeyword> keywords = GetAllKeywords(copy, convertPyramidsTo, deformations);
            // After the overrides, which are matched against the ASCII keywords shown in the keyword editor
            if (binaryResultsFile) SetBinaryResultsFile(keywords);
            // Write file
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in keywords)
            {
                WriteKeywordRecursively(sb, keyword);
            }
            // Write to file in multiple steps
            int step = 10_000_000;
            int upper;
            using (StreamWriter sw = new StreamWriter(fileName, false))
            {
                for (int i = 0; i < sb.Length; i+=step)
                {
                    upper = Math.Min(sb.Length - i, step);
                    sw.Write(sb.ToString(i, upper));
                }
            }
        }
        static private void SetBinaryResultsFile(List<CalculixKeyword> keywords)
        {
            if (keywords == null) return;
            foreach (var keyword in keywords)
            {
                if (keyword is CalStepHeader stepHeader) SetBinaryResultsFile(stepHeader);
                else SetBinaryResultsFile(keyword.Keywords);
            }
        }
        static private void SetBinaryResultsFile(CalStepHeader stepHeader)
        {
            // CalculiX writes the results of a step in the format of the last field output keyword of the step
            List<CalculixKeyword> fieldOutputs = new List<CalculixKeyword>();
            CollectFieldOutputKeywords(stepHeader.Keywords, fieldOutputs);
            //
            foreach (var fieldOutput in fieldOutputs)
            {
                if (fieldOutput is CalContactFile contactFile && !contactFile.BinarySupported) return;
            }
            //
            foreach (var fieldOutput in fieldOutputs)
            {
                if (fieldOutput is CalNodeFile nodeFile) nodeFile.Binary = true;
                else if (fieldOutput is CalElFile elFile) elFile.Binary = true;
                else if (fieldOutput is CalContactFile contactFile) contactFile.Binary = true;
            }
        }
        static private void CollectFieldOutputKeywords(List<CalculixKeyword> keywords, List<CalculixKeyword> fieldOutputs)
        {
            if (keywords == null) return;
            foreach (var keyword in keywords)
            {
                if (keyword is CalNodeFile || keyword is CalElFile || keyword is CalContactFile) fieldOutputs.Add(keyword);
                CollectFieldOutputKeywords(keyword.Keywords, fieldOutputs);
            }
        }
        static private void Clean(FeModel model)
        {
            foreach (var entry in model.Sections)
            {
                if (entry.Value is MassSection ms)
                {
                    if (ms is PointMassSection pms)
                    {
                        pms.ElementSetName = null;
                    }
                }
            }
        }
        static public void WriteMaterials(string fileName, FeModel model, string[] materialNames)
        {
            List<CalculixKeyword> keywords = new List<CalculixKeyword>();
            // Heading
            CalTitle title = new CalTitle("Heading", "");
            keywords.Add(title);
            AppendHeading(model, title);
            // Materials
            title = new CalTitle("Materials", "");
            keywords.Add(title);
            AppendMaterials(model, title, null, materialNames);
            // Write file
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in keywords)
            {
                WriteKeywordRecursively(sb, keyword);
            }
            File.WriteAllText(fileName, sb.ToString());
        }
        //
        static public List<CalculixKeyword> GetAllKeywords(FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                                           Dictionary<int, double[]> deformations = null)
        {
            List<CalculixKeyword> keywords = GetModelKeywords(model, convertPyramidsTo, deformations);
            // Overrides use indices from the generated tree, before user keywords shift visual positions.
            ApplyKeywordOverrides(keywords, model.CalculixKeywordOverrides);
            // Add user keywords
            if (model.CalculixUserKeywords != null)
            {
                foreach (var entry in model.CalculixUserKeywords)
                {
                    // Deep clone to prevent the changes in user keywords
                    AddUserKeywordByIndices(keywords, entry.Key, entry.Value.DeepClone());
                }
            }
            //
            return keywords;
        }
        static public List<CalculixKeyword> GetModelKeywords(FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                                             Dictionary<int, double[]> deformations = null,
                                                             bool addAllMaterials = false)
        {
            // Clone surfaces
            OrderedDictionary<string, FeSurface> originalSurfaces = model.Mesh.Surfaces;
            OrderedDictionary<string, FeElementSet> originalElementSets = model.Mesh.ElementSets;
            //
            model.Mesh.Surfaces = originalSurfaces.DeepClone();
            model.Mesh.ElementSets = originalElementSets.DeepClone();
            Dictionary<int, FeElement> replacedElements = new Dictionary<int, FeElement>();
            //
            try
            {
                // Only keywords from the model, not user keywords
                // Always add a title keyword to get all possible keyword types to the keyword editor
                int maxNodeId = model.Mesh.MaxNodeId;
                int maxElementId = model.Mesh.MaxElementId;
                List<FeNode> additionalNodes = new List<FeNode>();
                List<FeNodeSet> additionalNodeSets = new List<FeNodeSet>();
                List<CalElement> additionalElementKeywords = new List<CalElement>();
                HashSet<string> additionalElementSetNames = new HashSet<string>();
                Dictionary<string, CalReferencePoint> referencePoints;
                Dictionary<CoordinateSystem, HashSet<string>> coordinateSystemNodeSets =
                    new Dictionary<CoordinateSystem, HashSet<string>>();
                List<SectionData> additionalSectionData = new List<SectionData>();
                List<Material> additionalMaterials = new List<Material>();
                List<BoundaryCondition> additionalBoundaryConditions = new List<BoundaryCondition>();
                OrderedDictionary<string, List<PreTensionLoad>> preTensionLoads;
                List<double[]> equationParameters = new List<double[]>();
                //
                // Collect pre-tension loads to use them in reference point preparation
                GetPretensionLoads(model, out preTensionLoads);
                // Prepare reference points
                GetReferencePoints(model, preTensionLoads, ref maxNodeId, ref additionalNodeSets, out referencePoints);
                // Prepare coordinate system-node set pairs
                GetCoordinateSystemNodeSet(model, referencePoints, ref coordinateSystemNodeSets);
                // Fix pyramid surfaces
                FixPyramidSurfaces(model, convertPyramidsTo, ref replacedElements, ref additionalElementSetNames);
                // Prepare mass sections
                GetMassSections(model, referencePoints, ref maxElementId, ref additionalElementKeywords,
                                ref additionalElementSetNames, ref additionalSectionData);
                // Prepare point springs
                GetPointSprings(model, referencePoints, ref maxElementId, ref additionalElementKeywords,
                                ref additionalElementSetNames, ref additionalSectionData);
                // Collect compression only constraints
                GetCompressionOnlyConstraintData(model, ref maxNodeId, ref maxElementId, ref additionalNodes,
                                                 ref additionalNodeSets, ref additionalElementKeywords,
                                                 ref additionalElementSetNames, ref additionalSectionData,
                                                 ref additionalMaterials, ref additionalBoundaryConditions,
                                                 ref equationParameters);
                // Collect joint constraints
                Dictionary<string, CalReferencePoint[]> jointReferencePoints;
                GetJointData(model, ref maxNodeId, ref maxElementId, ref additionalNodes, ref additionalNodeSets,
                             ref additionalElementKeywords, ref additionalElementSetNames, ref additionalSectionData,
                             ref additionalBoundaryConditions, ref equationParameters, out jointReferencePoints);
                Dictionary<string, string> cyclicSymmetryElementSetNames =
                    PrepareCyclicSymmetryElementSets(model);
                //
                CalTitle title;
                List<CalculixKeyword> keywords = new List<CalculixKeyword>();
                // Heading
                title = new CalTitle("Heading", "");
                keywords.Add(title);
                AppendHeading(model, title);
                // Submodel
                string[] nodeSetNames = GetAllSubmodelNodeSetNames(model);
                if (nodeSetNames.Length > 0)
                {
                    title = new CalTitle("Submodel", "");
                    keywords.Add(title);
                    AppendSubmodel(model, nodeSetNames, title);
                }
                // Nodes
                title = new CalTitle("Nodes", "");
                keywords.Add(title);
                AppendNodes(model, additionalNodes, referencePoints, deformations, title);
                // Elements
                title = new CalTitle("Elements", "");
                keywords.Add(title);
                AppendElements(model, additionalElementKeywords, title, convertPyramidsTo);
                // Node sets
                title = new CalTitle("Node sets", "");
                keywords.Add(title);
                AppendNodeSets(model, additionalNodeSets, title);
                // Element sets
                title = new CalTitle("Element sets", "");
                keywords.Add(title);
                AppendElementSets(model, additionalElementSetNames, title);
                // Surfaces
                title = new CalTitle("Surfaces", "");
                keywords.Add(title);
                AppendSurfaces(model, title);
                // Physical constants
                title = new CalTitle("Physical constants", "");
                keywords.Add(title);
                AppendPhysicalConstants(model, title);
                // Coordinate systems
                title = new CalTitle("Coordinate systems", "");
                keywords.Add(title);
                AppendCoordinateSystems(coordinateSystemNodeSets, title);
                // Materials
                title = new CalTitle("Materials", "");
                keywords.Add(title);
                AppendMaterials(model, title, additionalMaterials, null);
                // Sections
                title = new CalTitle("Sections", "");
                keywords.Add(title);
                AppendSections(model, additionalSectionData, title);
                // Pre-tension sections
                title = new CalTitle("Pre-tension sections", "");
                keywords.Add(title);
                AppendPreTensionSections(preTensionLoads, referencePoints, title);
                // Constraints
                title = new CalTitle("Constraints", "");
                keywords.Add(title);
                AppendConstraints(model, referencePoints, jointReferencePoints, equationParameters,
                                  cyclicSymmetryElementSetNames, title);
                // Surface interactions
                title = new CalTitle("Surface interactions", "");
                keywords.Add(title);
                AppendSurfaceInteractions(model, title);
                // Contact pairs
                title = new CalTitle("Contact pairs", "");
                keywords.Add(title);
                AppendContactPairs(model, title);
                // Amplitudes
                title = new CalTitle("Amplitudes", "");
                keywords.Add(title);
                AppendAmplitudes(model, title);
                // Initial conditions
                title = new CalTitle("Initial conditions", "");
                keywords.Add(title);
                AppendInitialConditions(model, referencePoints, title);
                // Steps
                title = new CalTitle("Steps", "");
                keywords.Add(title);
                AppendSteps(model, additionalBoundaryConditions, referencePoints, title);
                //
                return keywords;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                model.Mesh.ElementSets = originalElementSets;
                model.Mesh.Surfaces = originalSurfaces;
                //
                foreach (var entry in replacedElements) model.Mesh.Elements[entry.Key] = entry.Value;
                //
                Clean(model);
            }
        }
        static private void GetPretensionLoads(FeModel model, out OrderedDictionary<string, List<PreTensionLoad>> preTensionLoads)
        {
            string name;
            List<PreTensionLoad> preTensionLoadsList;
            preTensionLoads =
                new OrderedDictionary<string, List<PreTensionLoad>>("Pretension Loads", StringComparer.OrdinalIgnoreCase);
            foreach (var step in model.StepCollection.StepsList)
            {
                if (step.Active)
                {
                    foreach (var entry in step.Loads)
                    {
                        if (entry.Value is PreTensionLoad ptl)
                        {
                            name = ptl.SurfaceName;
                            if (!ptl.AutoComputeDirection) name += "_" + ptl.X.ToString() + ptl.Y.ToString() + ptl.Z.ToString();
                            //
                            if (preTensionLoads.TryGetValue(name, out preTensionLoadsList)) preTensionLoadsList.Add(ptl);
                            else preTensionLoads.Add(name, new List<PreTensionLoad>() { ptl });
                        }
                    }
                }
            }
        }
        static private void GetReferencePoints(FeModel model, OrderedDictionary<string, List<PreTensionLoad>> preTensionLoads,
                                               ref int maxNodeId, ref List<FeNodeSet> additionalNodeSets,
                                               out Dictionary<string, CalReferencePoint> referencePoints)
        {
            HashSet<string> allNodeSetNames = new HashSet<string>(model.Mesh.NodeSets.Keys);
            foreach (var nodeSet in additionalNodeSets) allNodeSetNames.Add(nodeSet.Name);
            //
            FeNodeSet rpNodeSet;
            referencePoints = new Dictionary<string, CalReferencePoint>();
            if (model.Mesh != null)
            {
                HashSet<string> rigidBodyReferencePointNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                HashSet<string> couplingReferencePointNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in model.Constraints)
                {
                    if (!entry.Value.Active) continue;
                    if (entry.Value is RigidBody rigidBody && rigidBody.ReferencePointName != null)
                        rigidBodyReferencePointNames.Add(rigidBody.ReferencePointName);
                    else if (entry.Value is Coupling coupling && coupling.ReferencePointName != null)
                        couplingReferencePointNames.Add(coupling.ReferencePointName);
                }
                // Fill reference point nodes
                int id = maxNodeId;
                FeReferencePoint rp;
                foreach (var entry in model.Mesh.ReferencePoints)
                {
                    rp = entry.Value;
                    bool usedByRigidBody = rigidBodyReferencePointNames.Contains(rp.Name);
                    bool usedByCoupling = couplingReferencePointNames.Contains(rp.Name);
                    // Check name
                    rp.RefNodeSetName = GetReferencePointNodeSetName(rp.Name, FeReferencePoint.RefName + id + 1);
                    if (allNodeSetNames.Contains(rp.RefNodeSetName))
                        rp.RefNodeSetName = allNodeSetNames.GetNextNumberedKey(rp.RefNodeSetName);
                    rpNodeSet = new FeNodeSet(rp.RefNodeSetName, new int[] { id + 1 });
                    additionalNodeSets.Add(rpNodeSet);
                    allNodeSetNames.Add(rpNodeSet.Name);
                    //
                    CalReferencePoint calReferencePoint = new CalReferencePoint
                    {
                        ReferenceNodeId = id + 1,
                        ReferenceNodeSetName = rp.RefNodeSetName,
                        UsedByRigidBody = usedByRigidBody,
                        UsedByCoupling = usedByCoupling
                    };
                    id++;
                    // Only a rigid body needs the auxiliary CalculiX rotation node.
                    if (usedByRigidBody)
                    {
                        rp.RotNodeSetName = GetReferencePointNodeSetName(rp.Name, FeReferencePoint.RotName + id + 1);
                        if (allNodeSetNames.Contains(rp.RotNodeSetName))
                            rp.RotNodeSetName = allNodeSetNames.GetNextNumberedKey(rp.RotNodeSetName);
                        rpNodeSet = new FeNodeSet(rp.RotNodeSetName, new int[] { id + 1 });
                        additionalNodeSets.Add(rpNodeSet);
                        allNodeSetNames.Add(rpNodeSet.Name);
                        calReferencePoint.RotationNodeId = id + 1;
                        calReferencePoint.RotationNodeSetName = rp.RotNodeSetName;
                        id++;
                    }
                    else rp.RotNodeSetName = null;
                    referencePoints.Add(entry.Key, calReferencePoint);
                }
                //
                foreach (var entry in preTensionLoads)
                {
                    referencePoints.Add(entry.Key, new CalReferencePoint { ReferenceNodeId = id + 1 });
                    id++;
                }
                //
                maxNodeId = id;
            }
        }
        static private string GetReferencePointNodeSetName(string referencePointName, string suffix)
        {
            // Results reading finds the reference point by the suffix, so only the reference point name is shortened
            return CalculixNameShortener.GetShortenedName(referencePointName, CalculixNameShortener.MaxNameLength - suffix.Length) +
                   suffix;
        }
        static private void GetCoordinateSystemNodeSet(FeModel model, Dictionary<string, CalReferencePoint> referencePoints,
                                                       ref Dictionary<CoordinateSystem, HashSet<string>> coordinateSystemNodeSets)
        {
            if (model == null || model.StepCollection == null || model.Mesh.CoordinateSystems == null ||
                model.Mesh.CoordinateSystems.Count() == 0) return;
            //
            string nodeSetName;
            HashSet<string> nodeSetNames;
            FeSurface surface;
            CoordinateSystem coordinateSystem;
            // Boundary conditions
            foreach (var boundaryCondition in model.StepCollection.GetAllBoundaryConditions())
            {
                if (boundaryCondition.Active && 
                    model.Mesh.CoordinateSystems.TryGetValue(boundaryCondition.CoordinateSystemName, out coordinateSystem) &&
                    coordinateSystem != null && boundaryCondition is DisplacementRotation displacementRotation)
                {
                    // Node set
                    if (displacementRotation.RegionType == RegionTypeEnum.NodeSetName)
                    {
                        nodeSetName = displacementRotation.RegionName;
                        if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                            nodeSetNames.Add(nodeSetName);
                        else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                    }
                    // Surface
                    else if (displacementRotation.RegionType == RegionTypeEnum.SurfaceName)
                    {
                        surface = model.Mesh.Surfaces[displacementRotation.RegionName];
                        nodeSetName = surface.NodeSetName;
                        if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                            nodeSetNames.Add(nodeSetName);
                        else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                    }
                    // Reference point - user coordinate systems do not work for reference points
                    else if (displacementRotation.RegionType == RegionTypeEnum.ReferencePointName)
                    {
                        //referencePoint = model.Mesh.ReferencePoints[displacementRotation.RegionName];
                        //nodeSetName = referencePointsNodeSetNames[referencePoint.Name][0];
                        ////
                        //if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                        //    nodeSetNames.Add(nodeSetName);
                        //else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                    }
                }
            }
        }
        static private void FixPyramidSurfaces(FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                               ref Dictionary<int, FeElement> replacedElements,
                                               ref HashSet<string> additionalElementSetNames)
        {
            // Determine if there are any pyramids in the model
            List<BasePart> pyramidContainingParts = new List<BasePart>();
            foreach (var entry in model.Mesh.Parts)
            {
                if (entry.Value.ElementTypes.Contains(typeof(LinearPyramidElement)) ||
                    entry.Value.ElementTypes.Contains(typeof(ParabolicPyramidElement)))
                    pyramidContainingParts.Add(entry.Value);
            }
            //
            if (pyramidContainingParts.Count > 0)
            {
                // Create a mapping from pyramid faces to wedge or hex faces
                Dictionary<FeFaceName, FeFaceName> pyramidToConvertedFaceName =
                    model.Mesh.GetPyramidFaceConversion(convertPyramidsTo);
                //
                Type elementType;
                FeElement element;
                HashSet<int> elementIds = new HashSet<int>();
                HashSet<int> allPyramidElementIds = new HashSet<int>();
                foreach (var part in pyramidContainingParts) elementIds.UnionWith(part.Labels);
                // Collect all pyramid element ids
                foreach (var elementId in elementIds)
                {
                    element = model.Mesh.Elements[elementId];
                    elementType = element.GetType();
                    if (elementType == typeof(LinearPyramidElement) || elementType == typeof(ParabolicPyramidElement))
                    {
                        allPyramidElementIds.Add(elementId);
                        replacedElements.Add(element.Id, element);
                        //
                        if (element is LinearPyramidElement lpe)
                        {
                            if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                                model.Mesh.Elements[elementId] = lpe.ConvertToWedge();
                            else if (convertPyramidsTo == ConvertPyramidsToEnum.Hexahedrons)
                                model.Mesh.Elements[elementId] = lpe.ConvertToHex();
                            else throw new NotSupportedException();
                        }
                        else if (element is ParabolicPyramidElement ppe)
                        {
                            if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                                model.Mesh.Elements[elementId] = ppe.ConvertToWedge();
                            else if (convertPyramidsTo == ConvertPyramidsToEnum.Hexahedrons)
                                model.Mesh.Elements[elementId] = ppe.ConvertToHex();
                            else throw new NotSupportedException();
                        }
                        else throw new NotSupportedException();
                    }
                }
                //
                bool needFixing;
                string name;
                FeFaceName faceName;
                FeSurface surface;
                FeSurface fixedSurface;
                FeElementSet elementSet;
                HashSet<int> normalElementsIds;
                HashSet<int> pyramidElementsIds;
                Dictionary<FeFaceName, HashSet<int>> faceNameElementIds;
                HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
                //
                foreach (var surfaceEntry in model.Mesh.Surfaces)
                {
                    // Determine if the surface contain pyramid elements
                    needFixing = false;
                    surface = surfaceEntry.Value;
                    if (surface.ElementFaces == null)
                    {
                        if (Debugger.IsAttached) Debugger.Break();
                    }
                    else
                    {
                        foreach (var entry in surface.ElementFaces)
                        {
                            elementSet = model.Mesh.ElementSets[entry.Value];
                            if (allPyramidElementIds.Intersect(elementSet.Labels).Count() > 0)
                            {
                                needFixing = true;
                                break;
                            }
                        }
                        // If the surface contains pyramid elements
                        if (needFixing)
                        {
                            // Collect pyramid and normal element ids separately
                            faceNameElementIds = new Dictionary<FeFaceName, HashSet<int>>();
                            foreach (var entry in surface.ElementFaces)
                            {
                                elementSet = model.Mesh.ElementSets[entry.Value];
                                pyramidElementsIds = new HashSet<int>(allPyramidElementIds.Intersect(elementSet.Labels));
                                normalElementsIds = new HashSet<int>(elementSet.Labels.Except(pyramidElementsIds));
                                //
                                if (normalElementsIds.Count > 0)
                                {
                                    faceName = entry.Key;
                                    if (faceNameElementIds.TryGetValue(faceName, out elementIds)) elementIds.UnionWith(normalElementsIds);
                                    else faceNameElementIds.Add(faceName, normalElementsIds);
                                }
                                //
                                if (pyramidElementsIds.Count > 0)
                                {
                                    faceName = pyramidToConvertedFaceName[entry.Key];
                                    if (faceNameElementIds.TryGetValue(faceName, out elementIds)) elementIds.UnionWith(pyramidElementsIds);
                                    else faceNameElementIds.Add(faceName, pyramidElementsIds);
                                }
                            }
                            // Create new surface with the same name
                            fixedSurface = new FeSurface(surface.Name);
                            //
                            foreach (var entry in faceNameElementIds)
                            {
                                // Get element set name
                                name = surface.Name + "_Pyramid_" + entry.Key;
                                if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                                reservedElementSetNames.Add(name);
                                // Create new element set
                                elementSet = new FeElementSet(name, entry.Value.ToArray());
                                model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
                                additionalElementSetNames.Add(elementSet.Name);
                                //
                                fixedSurface.AddElementFace(entry.Key, elementSet.Name);
                                //
                                fixedSurface.NodeSetName = surface.NodeSetName;
                            }
                            // Add fixed surface
                            model.Mesh.Surfaces[fixedSurface.Name] = fixedSurface;
                        }
                    }
                }
            }
        }
        static private bool IsMassSectionRegionPresent(FeModel model, MassSection massSection,
                                                       Dictionary<string, CalReferencePoint> referencePoints)
        {
            FeSurface surface;
            if (massSection is DistributedMassSection)
                return model.Mesh.Surfaces.TryGetValue(massSection.RegionName, out surface) &&
                       model.Mesh.NodeSets.ContainsKey(surface.NodeSetName);
            if (massSection.RegionType == RegionTypeEnum.NodeSetName)
                return model.Mesh.NodeSets.ContainsKey(massSection.RegionName);
            if (massSection.RegionType == RegionTypeEnum.ReferencePointName)
                return model.Mesh.ReferencePoints.ContainsKey(massSection.RegionName) &&
                       referencePoints.ContainsKey(massSection.RegionName);
            return true;
        }
        static private void GetMassSections(FeModel model, Dictionary<string, CalReferencePoint> referencePoints,
                                            ref int maxElementId, ref List<CalElement> additionalElementKeywords,
                                            ref HashSet<string> additionalElementSetNames,
                                            ref List<SectionData> additionalSectionData)
        {
            double aSum;
            double nodalMassFactor;
            double nodalMass;
            string name;
            List<int> elementIds;
            FeElement element;
            FeNodeSet nodeSet;
            FeElementSet elementSet;
            FeSurface surface;
            FeReferencePoint referencePoint;
            MassSectionData massSectionData;
            Dictionary<int, double> nodalValues;
            CalElement calElement;
            HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
            //
            foreach (var entry in model.Sections)
            {
                if (entry.Value is MassSection ms)
                {
                    // A section whose region was removed or renamed is invalid and refused by a run, but the input is
                    // also written to place the user keywords, before the validity is updated
                    if (!IsMassSectionRegionPresent(model, ms, referencePoints)) continue;
                    //
                    if (ms is PointMassSection pms)
                    {
                        if (pms.RegionType == RegionTypeEnum.NodeSetName)
                        {
                            elementIds = new List<int>();
                            nodeSet = model.Mesh.NodeSets[pms.RegionName];
                            nodalMass = pms.Mass.Value;
                            //
                            foreach (var nodeId in nodeSet.Labels)
                            {
                                // Name
                                name = pms.Name + "_NID_" + nodeId;
                                if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                                reservedElementSetNames.Add(name);
                                // Element keyword
                                maxElementId++;
                                element = new MassElement(maxElementId, new int[] { nodeId });
                                calElement = new CalElement(FeElementType0D.Mass.ToString(), name, new List<FeElement> { element });
                                additionalElementKeywords.Add(calElement);
                                elementIds.Add(maxElementId);
                                // Mass section
                                massSectionData = new MassSectionData("NID_" + nodeId, name, nodalMass);
                                additionalSectionData.Add(massSectionData);
                            }
                            // Add elements in sets
                            name = pms.Name + "_All";
                            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                            reservedElementSetNames.Add(name);
                            //
                            elementSet = new FeElementSet(name, elementIds.ToArray());
                            model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
                            additionalElementSetNames.Add(elementSet.Name);
                            //
                            pms.ElementSetName = name;  // temporary storage
                        }
                        else if (pms.RegionType == RegionTypeEnum.ReferencePointName)
                        {
                            elementIds = new List<int>();
                            referencePoint = model.Mesh.ReferencePoints[pms.RegionName];
                            int nodeId = referencePoints[referencePoint.Name].ReferenceNodeId;
                            nodalMass = pms.Mass.Value;

                            // Name
                            name = pms.Name + "_RP_" + referencePoint.Name;
                            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                            reservedElementSetNames.Add(name);
                            // Element keyword
                            maxElementId++;
                            element = new MassElement(maxElementId, new int[] { nodeId });
                            calElement = new CalElement(FeElementType0D.Mass.ToString(), name, new List<FeElement> { element });
                            additionalElementKeywords.Add(calElement);
                            elementIds.Add(maxElementId);
                            // Mass section
                            massSectionData = new MassSectionData("NID_" + nodeId, name, nodalMass);
                            additionalSectionData.Add(massSectionData);
                            // Add elements in sets
                            pms.ElementSetName = name;  // temporary storage
                        }
                        else throw new NotSupportedException();
                    }
                    else if (ms is DistributedMassSection dms)
                    {
                        elementIds = new List<int>();
                        surface = model.Mesh.Surfaces[dms.RegionName];
                        nodeSet = model.Mesh.NodeSets[surface.NodeSetName];
                        model.GetDistributedNodalValuesFromSurface(surface.Name, out nodalValues, out aSum);
                        nodalMassFactor = dms.Mass.Value / aSum;
                        //
                        foreach (var nodeId in nodeSet.Labels)
                        {
                            // Name
                            name = dms.Name + "_NID_" + nodeId;
                            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                            reservedElementSetNames.Add(name);
                            // Element keyword
                            maxElementId++;
                            element = new MassElement(maxElementId, new int[] { nodeId });
                            calElement = new CalElement(FeElementType0D.Mass.ToString(), name, new List<FeElement> { element });
                            additionalElementKeywords.Add(calElement);
                            elementIds.Add(maxElementId);
                            // Mass section
                            nodalMass = nodalValues[nodeId] * nodalMassFactor;
                            massSectionData = new MassSectionData("NID_" + nodeId, name, nodalMass);
                            additionalSectionData.Add(massSectionData);
                        }
                        // Add elements in sets
                        name = dms.Name + "_All";
                        if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                        reservedElementSetNames.Add(name);
                        //
                        elementSet = new FeElementSet(name, elementIds.ToArray());
                        model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
                        additionalElementSetNames.Add(elementSet.Name);
                        //
                        dms.ElementSetName = name;  // temporary storage
                    }
                    else throw new NotSupportedException();
                }
            }
        }
        static private void GetPointSprings(FeModel model, Dictionary<string, CalReferencePoint> referencePoints, ref int maxElementId,
                                            ref List<CalElement> additionalElementKeywords,
                                            ref HashSet<string> additionalElementSetNames,
                                            ref List<SectionData> additionalSectionData)
        {
            if (model.Mesh != null)
            {
                bool twoD = model.Properties.ModelSpace.IsTwoD();
                // CalculiX expands an axisymmetric model into a 2 degree segment and scales the loads given for the full
                // revolution, but not the springs
                double stiffnessFactor = 1;
                if (model.Properties.ModelSpace == ModelSpaceEnum.Axisymmetric)
                    stiffnessFactor = 1 / AxisymmetricSegmentsPerRevolution;
                int count;
                int[] elementIds;
                CalReferencePoint referencePoint;
                int[] directions;
                double[] stiffnesses;
                string name;
                int elementId = maxElementId;
                FeNodeSet nodeSet;
                FeElementSet elementSet;
                List<FeElement> newElements;
                List<FeElement> oneSpringElements;
                // Collect point and surface springs
                Dictionary<string, PointSpringData[]> activeSprings = new Dictionary<string, PointSpringData[]>();
                foreach (var entry in model.Constraints)
                {
                    if (entry.Value is PointSpring ps && ps.Active)
                        activeSprings.Add(ps.Name, new PointSpringData[] { new PointSpringData(ps) });
                    else if (entry.Value is SurfaceSpring ss && ss.Active)
                        activeSprings.Add(ss.Name, model.GetPointSpringsFromSurfaceSpring(ss));
                }
                //
                HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
                //
                foreach (var entry in activeSprings)
                {
                    oneSpringElements = new List<FeElement>();
                    //
                    foreach (PointSpringData psd in entry.Value)
                    {
                        directions = psd.GetSpringDirections();
                        stiffnesses = psd.GetSpringStiffnessValues();
                        //
                        if (directions.Length == 0) continue;
                        //
                        for (int i = 0; i < directions.Length; i++)
                        {
                            // Name
                            name = psd.Name + "_DOF_" + directions[i];
                            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                            reservedElementSetNames.Add(name);
                            //
                            newElements = new List<FeElement>();
                            // Node id
                            if (psd.RegionType == RegionTypeEnum.NodeId)
                            {
                                newElements.Add(new LinearSpringElement(elementId + 1, new int[] { psd.NodeId }));
                                elementId++;
                            }
                            // Node set
                            else if (psd.RegionType == RegionTypeEnum.NodeSetName)
                            {
                                if (model.Mesh.NodeSets.TryGetValue(psd.RegionName, out nodeSet))
                                {
                                    foreach (var label in nodeSet.Labels)
                                    {
                                        newElements.Add(new LinearSpringElement(elementId + 1, new int[] { label }));
                                        elementId++;
                                    }
                                }
                            }
                            // Reference point
                            else if (psd.RegionType == RegionTypeEnum.ReferencePointName)
                            {
                                if (referencePoints.TryGetValue(psd.RegionName, out referencePoint))
                                {
                                    newElements.Add(new LinearSpringElement(elementId + 1,
                                                                           new int[] { referencePoint.ReferenceNodeId }));
                                    elementId++;
                                }
                            }
                            else throw new NotSupportedException();
                            // Get element ids
                            count = 0;
                            elementIds = new int[newElements.Count];
                            foreach (var element in newElements) elementIds[count++] = element.Id;
                            // Add items
                            elementSet = new FeElementSet(name, elementIds);
                            model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
                            additionalElementSetNames.Add(elementSet.Name);
                            additionalSectionData.Add(new LinearSpringSectionData("LinearSpringSection", name, directions[i],
                                                                                  stiffnesses[i] * stiffnessFactor));
                            oneSpringElements.AddRange(newElements);
                        }
                    }
                    // Add elements in sets
                    name = entry.Key + "_All";
                    if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                    reservedElementSetNames.Add(name);
                    additionalElementKeywords.Add(new CalElement(FeElementTypeSpring.SPRING1.ToString(), name, oneSpringElements));
                }
                //
                maxElementId = elementId;
            }
        }
        static private void GetCompressionOnlyConstraintData(FeModel model, ref int maxNodeId, ref int maxElementId,
                                                             ref List<FeNode> additionalNodes,
                                                             ref List<FeNodeSet> additionalNodeSets,
                                                             ref List<CalElement> additionalElementKeywords,
                                                             ref HashSet<string> additionalElementSetNames,
                                                             ref List<SectionData> additionalSectionData,
                                                             ref List<Material> additionalMaterials,
                                                             ref List<BoundaryCondition> additionalBoundaryConditions,
                                                             ref List<double[]> equationParameters)
        {
            HashSet<string> elementIdsHash = new HashSet<string>();
            //
            bool twoD = model.Properties.ModelSpace.IsTwoD();
            bool shellEdgeFace;
            bool shellElement;
            bool nonLinear = false;
            double[] faceNormal;
            FeElement element;
            FeElementSet elementSet;
            FeSurface surface;
            Vec3D normalVec;
            List<Vec3D> nodeNormals;
            Dictionary<int, List<Vec3D>> nodeIdNodeNormals = new Dictionary<int, List<Vec3D>>();
            //
            double[] normal;
            Dictionary<int, double[]> nodeIdNormal = new Dictionary<int, double[]>();
            int count;
            int newNodeId = maxNodeId;
            int newElementId = maxElementId;
            List<int> elementIds;
            string name;
            //
            FeNode node1;
            FeNode node2;
            List<int> bcNodeIds = new List<int>();
            LinearGapElement gapElement;
            List<FeElement> elementsToAdd = new List<FeElement>();
            double clearance;
            double area;
            double nodeStiffness;
            double nodeForce;
            double offset;
            double nodeStiffnessPerArea;
            double nodeForcePerArea;
            Dictionary<int, double> nodeIdNodeWeight;
            HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
            // Get all nodes and their normals
            foreach (var constraintEntry in model.Constraints)
            {
                if (constraintEntry.Value is CompressionOnly co)
                {
                    if (co.Active && co.Valid)
                    {
                        clearance = co.Clearance.Value;
                        //
                        nodeIdNodeNormals.Clear();
                        surface = model.Mesh.Surfaces[co.MasterRegionName];
                        model.GetDistributedNodalValuesFromSurface(surface.Name, out nodeIdNodeWeight, out area);
                        //
                        nodeStiffnessPerArea = co.SpringStiffness.Value;
                        if (double.IsNaN(nodeStiffnessPerArea)) nodeStiffnessPerArea = GapSectionData.InitialSpringStiffness;
                        nodeStiffnessPerArea = nodeStiffnessPerArea / area;
                        //
                        nodeForcePerArea = co.TensileForceAtNegativeInfinity.Value;
                        if (double.IsNaN(nodeForcePerArea)) nodeForcePerArea = GapSectionData.InitialTensileForceAtNegativeInfinity;
                        nodeForcePerArea = nodeForcePerArea / area;
                        //
                        offset = co.Offset.Value;
                        //
                        foreach (var entry in surface.ElementFaces)
                        {
                            elementSet = model.Mesh.ElementSets[entry.Value];
                            foreach (var elementId in elementSet.Labels)
                            {
                                if (elementIdsHash.Contains(entry.Key.ToString() + "_" + elementId))
                                    throw new CaeException("Element id: " + elementId +
                                                           " has more than one compression only constraint defined.");
                                //
                                element = model.Mesh.Elements[elementId];
                                elementIdsHash.Add(entry.Key.ToString() + "_" + elementId);
                                //
                                model.Mesh.GetElementFaceCenterAndNormal(elementId, entry.Key, out _, out faceNormal,
                                                                         out shellElement);
                                // Invert normal
                                shellEdgeFace = shellElement && entry.Key != FeFaceName.S1 && entry.Key != FeFaceName.S2;
                                if (shellElement && !shellEdgeFace)
                                {
                                    faceNormal[0] *= -1;
                                    faceNormal[1] *= -1;
                                    faceNormal[2] *= -1;
                                }
                                normalVec = new Vec3D(faceNormal);
                                //
                                foreach (var nodeId in element.GetNodeIdsFromFaceName(entry.Key))
                                {
                                    if (nodeIdNodeNormals.TryGetValue(nodeId, out nodeNormals)) nodeNormals.Add(normalVec);
                                    else nodeIdNodeNormals.Add(nodeId, new List<Vec3D>() { normalVec });
                                }
                            }
                        }
                        // Get a dictionary of all node ids that have the same normal
                        nodeIdNormal.Clear();
                        foreach (var entry in nodeIdNodeNormals)
                        {
                            normalVec = new Vec3D();
                            foreach (var normalEntry in entry.Value) normalVec += normalEntry;
                            normalVec.Normalize();
                            //
                            nodeIdNormal.Add(entry.Key, normalVec.Coor);
                        }
                        // Get nodes, elements, element sets, gap sections, boundary conditions
                        count = 1;
                        foreach (var entry in nodeIdNormal)
                        {
                            if (nodeIdNodeWeight[entry.Key] != 0)
                            {
                                normal = entry.Value;
                                elementIds = new List<int>();
                                name = co.Name + "_ElementSet-" + count++;
                                if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
                                reservedElementSetNames.Add(name);
                                // Node 1
                                newNodeId++;
                                bcNodeIds.Add(newNodeId);
                                //
                                node1 = new FeNode(newNodeId, model.Mesh.Nodes[entry.Key].Coor);
                                node1.X -= offset * normal[0];
                                node1.Y -= offset * normal[1];
                                node1.Z -= offset * normal[2];
                                additionalNodes.Add(node1);
                                // Node 2
                                newNodeId++;
                                //
                                node2 = new FeNode(newNodeId, model.Mesh.Nodes[entry.Key].Coor);
                                additionalNodes.Add(node2);
                                //
                                newElementId++;
                                gapElement = new LinearGapElement(newElementId, new int[] { node1.Id, node2.Id });
                                elementIds.Add(newElementId);
                                elementsToAdd.Add(gapElement);
                                // Equations
                                equationParameters.Add(new double[] { node2.Id, 1, 1, entry.Key, 1, -1 });
                                equationParameters.Add(new double[] { node2.Id, 2, 1, entry.Key, 2, -1 });
                                equationParameters.Add(new double[] { node2.Id, 3, 1, entry.Key, 3, -1 });
                                //
                                elementSet = new FeElementSet(name, elementIds.ToArray());
                                model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
                                additionalElementSetNames.Add(elementSet.Name);
                                // Scale to nodal area
                                nodeStiffness = nodeStiffnessPerArea * nodeIdNodeWeight[entry.Key];
                                nodeForce = nodeForcePerArea * nodeIdNodeWeight[entry.Key];
                                // Gap section
                                additionalSectionData.Add(new GapSectionData("GapSectionData", name, clearance, normal,
                                                                             nodeStiffness, nodeForce));
                            }
                        }
                        //
                        if (co.NonLinear) nonLinear = true;
                    }
                }
            }
            // Add all elements
            if (elementsToAdd.Count > 0)
                additionalElementKeywords.Add(new CalElement(FeElementTypeGap.GAPUNI.ToString(), null, elementsToAdd));
            // Boundary conditions
            if (bcNodeIds.Count > 0)
            {
                name = "Internal_All_Compression_Only_Constraints_NodeSet";
                additionalNodeSets.Add(new FeNodeSet(name, bcNodeIds.ToArray()));
                DisplacementRotation dr = new DisplacementRotation("Compression_Only_BC", name, RegionTypeEnum.NodeSetName,
                                                                   twoD, false, 0);
                dr.U1.SetEquationFromValue(0);
                dr.U2.SetEquationFromValue(0);
                dr.U3.SetEquationFromValue(0);
                additionalBoundaryConditions.Add(dr);
            }
            // Materials
            if (nonLinear)
            {
                Material material = new Material(model.Materials.GetNextNumberedKey("Internal_compression_only"));
                material.AddProperty(new Density(new double[][] { new double[] { 1, 0 } }));
                material.AddProperty(new Elastic(new double[][] { new double[] { 1, 0, 0 } }));
                material.AddProperty(new Plastic(new double[][] { new double[] { 0, 0, 0 } }));
                additionalMaterials.Add(material);
            }
            //
            maxNodeId = newNodeId;
            maxElementId = newElementId;
        }
        static public string GetCompleteKeywordData(CalculixKeyword keyword)
        {
            return keyword.GetKeywordString() + keyword.GetDataString();
        }
        static public string GetKeywordSignature(CalculixKeyword keyword)
        {
            string signatureData = keyword.GetType().FullName + "\n" + GetKeywordSignatureData(keyword);
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(signatureData));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }
        static private string GetKeywordSignatureData(CalculixKeyword keyword)
        {
            if (keyword is CalHeading heading)
                return heading.GetKeywordString() + heading.GetDataStringWithoutGeneratedValues();
            return GetCompleteKeywordData(keyword);
        }
        static public int ApplyKeywordOverrides(List<CalculixKeyword> keywords,
                                                IList<CalculixKeywordOverride> keywordOverrides)
        {
            if (keywords == null || keywordOverrides == null || keywordOverrides.Count == 0) return 0;
            return ApplyKeywordOverrides(keywords, keywordOverrides, new List<int>());
        }
        static private int ApplyKeywordOverrides(List<CalculixKeyword> keywords,
                                                 IList<CalculixKeywordOverride> keywordOverrides,
                                                 List<int> parentIndices)
        {
            int appliedCount = 0;
            for (int i = 0; i < keywords.Count; i++)
            {
                CalculixKeyword originalKeyword = keywords[i];
                List<int> indices = new List<int>(parentIndices);
                indices.Add(i);
                // Apply child overrides first, then retain those effective children when wrapping this fragment.
                appliedCount += ApplyKeywordOverrides(originalKeyword.Keywords, keywordOverrides, indices);
                // Resolve by generated path and validate against the generated type and complete own text.
                int[] indexPath = indices.ToArray();
                CalculixKeywordOverride keywordOverride = keywordOverrides.FirstOrDefault(
                    item => item != null && item.HasSameIndices(indexPath) &&
                            item.OriginalSignature == GetKeywordSignature(originalKeyword));
                if (keywordOverride != null)
                {
                    string replacementData = keywordOverride.Data;
                    if (originalKeyword is CalHeading heading) replacementData = heading.ApplyGeneratedValues(replacementData);
                    keywords[i] = new CalKeywordOverride(originalKeyword,
                                                         GetCompleteKeywordData(originalKeyword),
                                                         replacementData,
                                                         keywordOverride.Disabled);
                    appliedCount++;
                }
            }
            return appliedCount;
        }
        public enum UserKeywordStatus
        {
            Active,
            Suppressed,
            Conflict
        }
        static public UserKeywordStatus GetUserKeywordStatus(List<CalculixKeyword> keywords, int[] indices)
        {
            if (keywords == null || indices == null || indices.Length == 0) return UserKeywordStatus.Conflict;
            // A root keyword is inserted between two generated keywords.
            if (indices.Length == 1)
            {
                if (indices[0] >= 0 && indices[0] <= keywords.Count) return UserKeywordStatus.Active;
                else return UserKeywordStatus.Conflict;
            }
            // Find and validate the generated parent of the user keyword.
            if (indices[0] < 0 || indices[0] >= keywords.Count) return UserKeywordStatus.Conflict;
            CalculixKeyword keywordParent = keywords[indices[0]];
            if (keywordParent is CalDeactivated) return UserKeywordStatus.Suppressed;
            //
            for (int i = 1; i < indices.Length - 1; i++)
            {
                if (indices[i] < 0 || indices[i] >= keywordParent.Keywords.Count)
                    return UserKeywordStatus.Conflict;
                //
                keywordParent = keywordParent.Keywords[indices[i]];
                if (keywordParent is CalDeactivated) return UserKeywordStatus.Suppressed;
            }
            // The final index is an insertion position, so Count is valid.
            int insertionIndex = indices[indices.Length - 1];
            if (insertionIndex >= 0 && insertionIndex <= keywordParent.Keywords.Count)
                return UserKeywordStatus.Active;
            else
                return UserKeywordStatus.Conflict;
        }
        static public bool AddUserKeywordByIndices(List<CalculixKeyword> keywords, int[] indices, CalculixUserKeyword userKeyword)
        {
            if (GetUserKeywordStatus(keywords, indices) != UserKeywordStatus.Active) return false;
            //
            if (indices.Length == 1)
            {
                keywords.Insert(indices[0], userKeyword);
            }
            else
            {
                CalculixKeyword keywordParent = keywords[indices[0]];
                // Find a parent
                for (int i = 1; i < indices.Length - 1; i++)
                {
                    keywordParent = keywordParent.Keywords[indices[i]];
                }
                // Add the keyword
                keywordParent.Keywords.Insert(indices[indices.Length - 1], userKeyword);
            }
            return true;
        }
        //
        static public void UpdateUserKeywordStates(FeModel model, bool remove, out int suppresedCount, out int unsuppresedCount)
        {
            int removedCount;
            UpdateUserKeywordStates(model, remove, out suppresedCount, out unsuppresedCount, out removedCount);
            // Preserve the original API behavior where removed keywords were returned as suppressed.
            suppresedCount += removedCount;
        }
        static public void UpdateUserKeywordStates(FeModel model, bool remove, out int suppresedCount,
                                                   out int unsuppresedCount, out int removedCount)
        {
            suppresedCount = 0;
            unsuppresedCount = 0;
            removedCount = 0;
            List<CalculixKeyword> keywords;
            try
            {
                keywords = GetModelKeywords(model, ConvertPyramidsToEnum.Wedges, null, true);
            }
            catch (Exception)
            {
                // An item that cannot be written - its region was removed or renamed - must not fail every model change
                // that updates the user keywords. They keep their states until it is fixed, and a run or an export
                // refuses the invalid item meanwhile
                return;
            }
            List<int[]> keywordsToRemove = new List<int[]>();
            // Add user keywords
            if (model.CalculixUserKeywords != null)
            {
                foreach (var entry in model.CalculixUserKeywords)
                {
                    UserKeywordStatus status = GetUserKeywordStatus(keywords, entry.Key);
                    if (status == UserKeywordStatus.Active)
                    {
                        AddUserKeywordByIndices(keywords, entry.Key, entry.Value.DeepClone());
                        if (entry.Value.IsSuppressed) unsuppresedCount++;
                        entry.Value.Unsuppress();
                    }
                    else if (status == UserKeywordStatus.Suppressed)
                    {
                        if (!entry.Value.IsSuppressed) suppresedCount++;
                        entry.Value.Suppress();
                    }
                    else if (remove)
                    {
                        keywordsToRemove.Add(entry.Key);
                        removedCount++;
                    }
                    else
                    {
                        if (!entry.Value.IsSuppressed) suppresedCount++;
                        entry.Value.Suppress();
                    }
                }
            }
            //
            if (remove && keywordsToRemove.Count > 0)
            {
                foreach (var indices in keywordsToRemove)
                {
                    model.CalculixUserKeywords.Remove(indices);
                }
            }
        }
        //
        static public string GetShortKeywordData(CalculixKeyword keyword, bool useHiding)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(keyword.GetKeywordString());
            if (!(keyword is CalNode) &&
                !(keyword is CalElement) &&
                !(keyword is CalNodeSet) &&
                !(keyword is CalElementSet) &&
                !(keyword is CalInitialTemperature it && it.CanHideData) &&
                !(keyword is CalTemperatureBC tbc && tbc.CanHideData) &&
                !(keyword is CalDisplacementRotation cdr && cdr.CanHideData) &&
                !(keyword is CalVariablePressureLoad) &&
                !(keyword is CalImportedPressureLoad) &&
                !(keyword is CalSTLoad) &&
                !(keyword is CalDefinedTemperature dt && dt.CanHideData) &&
                !(keyword is CalAdditional))
            {
                sb.Append(keyword.GetDataString());
            }
            else
            {
            //    //int n = 10;
            //    //string[] lines = keyword.GetDataString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            //    //for (int i = 0; i < Math.Min(n,  lines.Length); i++) sb.AppendLine(lines[i]);
            //    //if (lines.Length > n) sb.AppendLine("... hidden data ...");
                if (useHiding) sb.AppendLine("... hidden data ...");
                else sb.Append(keyword.GetDataString());
            }
            //
            string text = sb.ToString();
            if (text.EndsWith(Environment.NewLine)) text = text.Substring(0, text.Length - Environment.NewLine.Length);
            return text;
        }
        static private string GetKeywordsTextWithoutComments(CalculixKeyword parent)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in parent.Keywords) WriteKeywordRecursively(sb, keyword);
            //
            StringBuilder withoutComments = new StringBuilder();
            foreach (var line in sb.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (!line.StartsWith("**")) withoutComments.AppendLine(line);
            }
            return withoutComments.ToString();
        }
        static private void WriteKeywordRecursively(StringBuilder sb, CalculixKeyword keyword)
        {
            sb.Append(ShortenLongNames(keyword.GetKeywordString()));
            sb.Append(ShortenLongNames(keyword.GetDataString()));
            //
            foreach (var childKeyword in keyword.Keywords)
            {
                WriteKeywordRecursively(sb, childKeyword);
            }
        }
        static private string ShortenLongNames(string text)
        {
            if (text == null || !ContainsLongNameCharacterRun(text)) return text;
            //
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("**")) continue;
                lines[i] = _nameTokenRegex.Replace(lines[i], ShortenLongNameToken);
            }
            return string.Join("\n", lines);
        }
        static private bool ContainsLongNameCharacterRun(string text)
        {
            int runLength = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!CalculixNameShortener.IsNameCharacter(text[i])) runLength = 0;
                else if (++runLength > CalculixNameShortener.MaxNameLength) return true;
            }
            return false;
        }
        static private string ShortenLongNameToken(Match match)
        {
            string token = match.Value;
            if (token.Length <= CalculixNameShortener.MaxNameLength) return token;
            //
            foreach (char character in token)
            {
                if (!CalculixNameShortener.IsNameCharacter(character)) return token;
            }
            return CalculixNameShortener.GetShortenedName(token);
        }
        //
        static private string[] GetAllSubmodelNodeSetNames(FeModel model)
        {
            HashSet<string> nodeSetNames = new HashSet<string>();
            foreach (var step in model.StepCollection.StepsList)
            {
                foreach (var entry in step.BoundaryConditions)
                {
                    if (entry.Value is SubmodelBC sm && sm.Active)
                    {
                        if (sm.RegionType == RegionTypeEnum.SurfaceName) 
                            nodeSetNames.Add(model.Mesh.Surfaces[sm.RegionName].NodeSetName);
                        else nodeSetNames.Add(sm.RegionName);
                    }
                } 
            }
            return nodeSetNames.ToArray();
        }
        //
        static private void AppendHeading(FeModel model, CalculixKeyword parent)
        {
            CalHeading heading = new CalHeading(model.Name, model.HashName, model.UnitSystem.UnitSystemType);
            parent.AddKeyword(heading);
        }
        static private void AppendSubmodel(FeModel model, string[] nodeSetNames, CalculixKeyword parent)
        {
            //*Submodel, TYPE = NODE, INPUT = Model.frd
            CalSubmodel submodel = new CalSubmodel(model.Properties.GlobalResultsFileName, nodeSetNames);
            parent.AddKeyword(submodel);
        }
        static private void AppendNodes(FeModel model, List<FeNode> additionalNodes,
                                        Dictionary<string, CalReferencePoint> referencePoints,
                                        Dictionary<int, double[]> deformations, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                if (model.Mesh.Nodes.Count + additionalNodes.Count + referencePoints.Count > 0)
                {
                    CalNode node = new CalNode(model, additionalNodes, referencePoints, deformations);
                    parent.AddKeyword(node);
                }
            }

        }
        static private void AppendElements(FeModel model, List<CalElement> additionalElementKeywords,
                                           CalculixKeyword parent, ConvertPyramidsToEnum convertPyramidsTo)
        {
            if (model.Mesh != null)
            {
                Dictionary<string, List<FeElement>> elementTypes = new Dictionary<string, List<FeElement>>();
                List<FeElement> elements;
                //
                string type;
                FeElement element;
                MeshPart part;
                CalElement elementKeyword;
                //
                foreach (var entry in model.Mesh.Parts)
                {
                    //if (!entry.Value.Active) continue;
                    //
                    elementTypes.Clear();
                    part = (MeshPart)entry.Value;
                    //
                    foreach (int elementId in part.Labels)
                    {
                        element = model.Mesh.Elements[elementId];
                        if (part.LinearTriaType != FeElementTypeLinearTria.None &&
                            element is LinearTriangleElement)
                            type = part.LinearTriaType.ToString();
                        else if (part.LinearQuadType != FeElementTypeLinearQuad.None &&
                            element is LinearQuadrilateralElement)
                            type = part.LinearQuadType.ToString();
                        else if (part.LinearTetraType != FeElementTypeLinearTetra.None &&
                            element is LinearTetraElement)
                            type = part.LinearTetraType.ToString();
                        else if (part.LinearPyramidType != FeElementTypeLinearPyramid.None &&
                            element is LinearPyramidElement)
                            type = part.LinearPyramidType.ToString();
                        else if (part.LinearWedgeType != FeElementTypeLinearWedge.None &&
                            element is LinearWedgeElement)
                            type = part.LinearWedgeType.ToString();
                        else if (part.LinearWedgeType == FeElementTypeLinearWedge.None &&   //
                            element is LinearWedgeElement)                                  // converted pyramids
                            type = FeElementTypeLinearWedge.C3D6.ToString();                //
                        else if (part.LinearHexaType == FeElementTypeLinearHexa.None &&     //
                            element is LinearHexaElement)                                   // converted pyramids
                            type = FeElementTypeLinearHexa.C3D8.ToString();                 //
                        else if (part.LinearHexaType != FeElementTypeLinearHexa.None &&
                            element is LinearHexaElement)
                            type = part.LinearHexaType.ToString();
                        else if (part.ParabolicTriaType != FeElementTypeParabolicTria.None &&
                            element is ParabolicTriangleElement)
                            type = part.ParabolicTriaType.ToString();
                        else if (part.ParabolicQuadType != FeElementTypeParabolicQuad.None &&
                            element is ParabolicQuadrilateralElement)
                            type = part.ParabolicQuadType.ToString();
                        else if (part.ParabolicTetraType != FeElementTypeParabolicTetra.None &&
                            element is ParabolicTetraElement)
                            type = part.ParabolicTetraType.ToString();
                        else if (part.ParabolicPyramidType != FeElementTypeParabolicPyramid.None &&
                            element is ParabolicPyramidElement)
                            type = part.ParabolicPyramidType.ToString();
                        else if (part.ParabolicWedgeType != FeElementTypeParabolicWedge.None &&
                            element is ParabolicWedgeElement)
                            type = part.ParabolicWedgeType.ToString();
                        else if (part.ParabolicWedgeType == FeElementTypeParabolicWedge.None && //
                            element is ParabolicWedgeElement)                                   // converted pyramids
                            type = FeElementTypeParabolicWedge.C3D15.ToString();                //
                        else if (part.ParabolicHexaType == FeElementTypeParabolicHexa.None &&   //
                            element is ParabolicHexaElement)                                    // converted pyramids
                            type = FeElementTypeParabolicHexa.C3D20.ToString();                 //
                        else if (part.ParabolicHexaType != FeElementTypeParabolicHexa.None &&
                            element is ParabolicHexaElement)
                            type = part.ParabolicHexaType.ToString();
                        else throw new NotImplementedException();
                        // Add element to the corresponding type
                        if (elementTypes.TryGetValue(type, out elements)) elements.Add(element);
                        else elementTypes.Add(type, new List<FeElement>() { element });
                    }
                    //
                    foreach (var typeEntry in elementTypes)
                    {
                        elementKeyword = new CalElement(typeEntry.Key, part.Name, typeEntry.Value, convertPyramidsTo);
                        parent.AddKeyword(elementKeyword);
                    }
                }
                // Additional elements
                AppendAdditionalElements(additionalElementKeywords, parent);
            }
        }
        static private void AppendAdditionalElements(List<CalElement> additionalElementKeywords, CalculixKeyword parent)
        {
            List<CalculixKeyword> keywords = new List<CalculixKeyword>();
            foreach (var item in additionalElementKeywords) keywords.Add(item);
            CalAdditional calAdditionalElements = new CalAdditional("Additional elements", keywords);
            if (calAdditionalElements.GetDataString().Length > 0) parent.AddKeyword(calAdditionalElements);
        }
        static private void AppendNodeSets(FeModel model, List<FeNodeSet> additionalNodeSets, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                HashSet<string> nodeSetNames = new HashSet<string>();
                //
                foreach (var entry in model.Mesh.NodeSets) AppendNodeSet(entry.Value, parent);
                nodeSetNames.UnionWith(model.Mesh.NodeSets.Keys);
                // Get additional node set keyword
                CalAdditional calAdditionalNodeSets = GetAdditionalNodeSetsKeyword(additionalNodeSets, ref nodeSetNames);
                // Append additional node set keyword
                if (calAdditionalNodeSets.GetDataString().Length > 0) parent.AddKeyword(calAdditionalNodeSets);
            }
        }
        static private void AppendNodeSet(FeNodeSet nodeSet, CalculixKeyword parent)
        {
            CalNodeSet calNodeSet;
            if (nodeSet.Active)
            {
                calNodeSet = new CalNodeSet(nodeSet);
                parent.AddKeyword(calNodeSet);
            }
            else parent.AddKeyword(new CalDeactivated(nodeSet.Name));
        }
        static private CalAdditional GetAdditionalNodeSetsKeyword(List<FeNodeSet> additionalNodeSets,
                                                                  ref HashSet<string> nodeSetNames)
        {
            CalculixKeyword keyword;
            List<CalculixKeyword> keywords = new List<CalculixKeyword>();
            foreach (var nodeSet in additionalNodeSets)
            {
                if (nodeSet.Active)
                {
                    keyword = new CalNodeSet(nodeSet);
                    keywords.Add(keyword);
                    //
                    nodeSetNames.Add(nodeSet.Name);
                }
            }
            //
            CalAdditional calAdditionalNodeSets = new CalAdditional("Additional node sets", keywords);
            return calAdditionalNodeSets;
        }
        static private void AppendParts(FeModel model, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                CalElementSet part;
                foreach (var entry in model.Mesh.Parts)
                {
                    if (entry.Value.Active)
                    {
                        part = new CalElementSet(entry.Value, model);
                        parent.AddKeyword(part);
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
            }
        }
        static private void AppendElementSets(FeModel model, HashSet<string> additionalElementSetNames, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                CalculixUserKeyword tmp = new CalculixUserKeyword("");
                foreach (var entry in model.Mesh.ElementSets)
                {
                    if (additionalElementSetNames.Contains(entry.Key)) AppendElementSet(model, entry.Value, tmp);
                    else AppendElementSet(model, entry.Value, parent);
                }
                //
                CalAdditional calAdditionalElementSets = new CalAdditional("Additional element sets", tmp.Keywords);
                if (calAdditionalElementSets.GetDataString().Length > 0) parent.AddKeyword(calAdditionalElementSets);
            }
        }
        static private void AppendElementSet(FeModel model, FeElementSet elementSet, CalculixKeyword parent)
        {
            CalElementSet calElementSet;
            if (elementSet.Active)
            {
                calElementSet = new CalElementSet(elementSet, model);
                parent.AddKeyword(calElementSet);
            }
            else parent.AddKeyword(new CalDeactivated(elementSet.Name));
        }
        static private void AppendSurfaces(FeModel model, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                FeSurface surface;
                CalSurface calSurface;
                foreach (var entry in model.Mesh.Surfaces)
                {
                    surface = entry.Value;
                    if (surface.Active &&
                        ((surface.Type == FeSurfaceType.Element && surface.ElementFaces != null) ||
                        (surface.Type == FeSurfaceType.Node && surface.NodeSetName != null)))
                    {
                        calSurface = new CalSurface(surface, model.Properties.ModelSpace.IsTwoD());
                        parent.AddKeyword(calSurface);
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
            }
        }
        static private void AppendPhysicalConstants(FeModel model, CalculixKeyword parent)
        {
            if (model != null)
            {
                CalPhysicalConstants calPhysicalConstants = new CalPhysicalConstants(model.Properties);
                if (calPhysicalConstants.GetKeywordString().Length > 0) parent.AddKeyword(calPhysicalConstants);
            }
        }
        static private void AppendCoordinateSystems(Dictionary<CoordinateSystem, HashSet<string>> coordinateSystemNodeSets,
                                                    CalculixKeyword parent)
        {
            foreach (var entry in coordinateSystemNodeSets)
            {
                foreach (var nodeSetName in entry.Value)
                {
                    parent.AddKeyword(new CalTransform(entry.Key, nodeSetName));
                }
            }    
        }
        static private void AppendMaterials(FeModel model, CalculixKeyword parent, 
                                            List<Material> additionalMaterials,
                                            string[] materialNames = null)
        {
            CalMaterial calMaterial;
            List<Material> materials = model.Materials.Values.ToList();
            if (additionalMaterials != null)
            {
                foreach (var additionalMaterial in additionalMaterials)
                {
                    materials.Add(additionalMaterial);
                }
            }
            //
            foreach (var material in materials)
            {
                if ((materialNames != null && materialNames.Contains(material.Name)) || material.Active)
                {
                    calMaterial = new CalMaterial(material);
                    parent.AddKeyword(calMaterial);
                    //
                    foreach (var property in material.Properties)
                    {
                        if (property is Density de)
                        {
                            calMaterial.AddKeyword(new CalDensity(de, material.TemperatureDependent));
                        }
                        else if (property is SlipWear sw)
                        {
                            if (materialNames != null)  // must be here
                                calMaterial.AddKeyword(new CalSlipWear(sw, material.TemperatureDependent));
                        }
                        else if (property is Elastic el)
                        {
                            calMaterial.AddKeyword(new CalElastic(el, material.TemperatureDependent));
                        }
                        else if (property is ElasticEngineeringConstants eec)
                        {
                            calMaterial.AddKeyword(new CalElasticEngineeringConstants(eec, material.TemperatureDependent));
                        }
                        else if (property is ElasticLamina ela)
                        {
                            calMaterial.AddKeyword(new CalElasticLamina(ela, material.TemperatureDependent));
                        }
                        else if (property is ElasticWithDensity ewd)
                        {
                            Density density = new Density(new double[][] { new double[] { ewd.Density.Value, 0 } });
                            calMaterial.AddKeyword(new CalDensity(density, material.TemperatureDependent));
                            //
                            Elastic elastic = new Elastic(new double[][] { new double[] { ewd.YoungsModulus.Value,
                                                                                          ewd.PoissonsRatio.Value,
                                                                                          0 } });
                            calMaterial.AddKeyword(new CalElastic(elastic, material.TemperatureDependent));
                        }
                        else if (property is Plastic pl)
                        {
                            calMaterial.AddKeyword(new CalPlastic(pl, material.TemperatureDependent));
                        }
                        else if (property is ThermalExpansion te)
                        {
                            calMaterial.AddKeyword(new CalThermalExpansion(te, material.TemperatureDependent));
                        }
                        else if (property is ThermalConductivity tc)
                        {
                            calMaterial.AddKeyword(new CalThermalConductivity(tc, material.TemperatureDependent));
                        }
                        else if (property is SpecificHeat sh)
                        {
                            calMaterial.AddKeyword(new CalSpecificHeat(sh, material.TemperatureDependent));
                        }
                        else throw new NotImplementedException();
                    }
                }
                else if (materialNames == null) parent.AddKeyword(new CalDeactivated(material.Name));
            }
            
        }
        static private void AppendSections(FeModel model, List<SectionData> additionalSectionData, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                foreach (var entry in model.Sections) AppendSection(entry.Value, parent);
                //
                AppendAdditionalSections(additionalSectionData, parent);
            }
        }
        static private void AppendSection(Section section, CalculixKeyword parent)
        {
            if (section.Active)
            {
                if (section is SolidSection ss) parent.AddKeyword(new CalSolidSection(ss));
                else if (section is ShellSection shs) parent.AddKeyword(new CalShellSection(shs));
                else if (section is MembraneSection ms) parent.AddKeyword(new CalMembraneSection(ms));
                else if (section is PointMassSection pms) { }
                else if (section is DistributedMassSection dms) { }
                else throw new NotSupportedException();
            }
            else parent.AddKeyword(new CalDeactivated(section.Name));
        }
        static private void AppendAdditionalSections(List<SectionData> additionalSectionData, CalculixKeyword parent)
        {
            CalculixKeyword keyword;
            List<CalculixKeyword> keywords = new List<CalculixKeyword>();
            //
            foreach (var section in additionalSectionData)
            {
                if (section.Active)
                {
                    if (section is LinearSpringSectionData lssd) keyword = new CalLinearSpringSection(lssd);
                    else if (section is GapSectionData gsd) keyword = new CalGapSection(gsd);
                    else if (section is MassSectionData msd) keyword = new CalMassSection(msd);
                    else throw new NotSupportedException();
                    //
                    if (keyword != null) keywords.Add(keyword);
                }
            }
            //
            CalAdditional calAdditionalSections = new CalAdditional("Additional sections", keywords);
            if (calAdditionalSections.GetDataString().Length > 0) parent.AddKeyword(calAdditionalSections);
        }
        static private HashSet<string> MaterialNamesUsedInActiveSections(FeModel model)
        {
            HashSet<string> materialNames = new HashSet<string>();
            if (model.Mesh != null)
            {
                foreach (var entry in model.Sections)
                {
                    if (entry.Value.Active) materialNames.Add(entry.Value.MaterialName);
                }
            }
            return materialNames;
        }
        static private void AppendPreTensionSections(OrderedDictionary<string, List<PreTensionLoad>> preTensionLoads,
                                                     Dictionary<string, CalReferencePoint> referencePoints,
                                                     CalculixKeyword parent)
        {
            if (preTensionLoads != null)
            {
                int nodeId;
                bool atLeastOneActive;
                PreTensionLoad ptl;
                foreach (var preTensionOnSurfaceEntry in preTensionLoads)
                {
                    atLeastOneActive = false;
                    foreach (var item in preTensionOnSurfaceEntry.Value) atLeastOneActive |= item.Active;
                    // Take the first one since all the others are the same
                    ptl = preTensionOnSurfaceEntry.Value[0];
                    if (atLeastOneActive)
                    {
                        nodeId = referencePoints[preTensionOnSurfaceEntry.Key].ReferenceNodeId;
                        CalPreTensionSection preTension;
                        if (ptl.AutoComputeDirection) preTension = new CalPreTensionSection(ptl.SurfaceName, nodeId);
                        else preTension = new CalPreTensionSection(ptl.SurfaceName, nodeId, ptl.X.Value, ptl.Y.Value, ptl.Z.Value);
                        parent.AddKeyword(preTension);
                    }
                    else parent.AddKeyword(new CalDeactivated("Pre-tension " + ptl.SurfaceName));
                }
            }
        }
        static private Dictionary<string, string> PrepareCyclicSymmetryElementSets(FeModel model)
        {
            Dictionary<string, string> elementSetNames = new Dictionary<string, string>();
            foreach (var entry in model.Constraints)
            {
                if (entry.Value is CyclicSymmetry cyclicSymmetry && cyclicSymmetry.Active &&
                    cyclicSymmetry.NumberOfGraphSectors > 1)
                {
                    if (string.IsNullOrWhiteSpace(cyclicSymmetry.RegionName))
                        throw new CaeException("The result expansion region of cyclic symmetry constraint '" +
                                               cyclicSymmetry.Name + "' is not defined.");
                    //
                    bool regionValid =
                        (cyclicSymmetry.RegionType == RegionTypeEnum.PartName &&
                         model.Mesh.Parts.ContainsValidKey(cyclicSymmetry.RegionName)) ||
                        (cyclicSymmetry.RegionType == RegionTypeEnum.ElementSetName &&
                         model.Mesh.ElementSets.ContainsValidKey(cyclicSymmetry.RegionName));
                    if (!regionValid)
                        throw new CaeException("The result expansion region of cyclic symmetry constraint '" +
                                               cyclicSymmetry.Name + "' is not valid.");
                    //
                    elementSetNames.Add(cyclicSymmetry.Name, cyclicSymmetry.RegionName);
                }
            }
            return elementSetNames;
        }
        static private void GetJointData(FeModel model, ref int maxNodeId, ref int maxElementId, ref List<FeNode> additionalNodes,
                                         ref List<FeNodeSet> additionalNodeSets, ref List<CalElement> additionalElementKeywords,
                                         ref HashSet<string> additionalElementSetNames,
                                         ref List<SectionData> additionalSectionData,
                                         ref List<BoundaryCondition> additionalBoundaryConditions,
                                         ref List<double[]> equationParameters,
                                         out Dictionary<string, CalReferencePoint[]> jointReferencePoints)
        {
            jointReferencePoints = new Dictionary<string, CalReferencePoint[]>();
            if (model.Mesh == null) return;
            //
            double[] center;
            double[] axis;
            int fixedNodeId = 0;
            CalReferencePoint master;
            CalReferencePoint slave;
            HashSet<string> reservedElementSetNames = model.Mesh.GetReservedElementSetNames();
            foreach (var entry in model.Constraints)
            {
                if (entry.Value is Joint joint && joint.Active && joint.Valid &&
                    model.TryGetJointCenterAndAxis(joint, out center, out axis))
                {
                    if (joint.Formulation == JointFormulationEnum.Springs)
                    {
                        AddJointSprings(model, joint, center, axis, ref maxNodeId, ref maxElementId, ref additionalNodes,
                                        ref additionalElementKeywords, ref additionalElementSetNames, ref additionalSectionData,
                                        equationParameters, reservedElementSetNames);
                        continue;
                    }
                    // A relative motion the joint locks completely is expressed by the two rigid bodies sharing the
                    // node that carries it, both placed at the joint centre. Calculix crashes on a rigid body whose
                    // rotation node is tied to another one by an equation, and on one whose rotation node is tied by
                    // nothing at all, so only the partially locked directions may be written as equations
                    master = new CalReferencePoint { UsedByRigidBody = true };
                    master.ReferenceNodeId = ++maxNodeId;
                    additionalNodes.Add(new FeNode(master.ReferenceNodeId, (double[])center.Clone()));
                    master.RotationNodeId = ++maxNodeId;
                    additionalNodes.Add(new FeNode(master.RotationNodeId.Value, (double[])center.Clone()));
                    //
                    slave = new CalReferencePoint { UsedByRigidBody = true };
                    if (AllTranslationsLocked(joint.JointType)) slave.ReferenceNodeId = master.ReferenceNodeId;
                    else
                    {
                        slave.ReferenceNodeId = ++maxNodeId;
                        additionalNodes.Add(new FeNode(slave.ReferenceNodeId, (double[])center.Clone()));
                    }
                    if (AllRotationsLocked(joint.JointType)) slave.RotationNodeId = master.RotationNodeId;
                    else
                    {
                        slave.RotationNodeId = ++maxNodeId;
                        additionalNodes.Add(new FeNode(slave.RotationNodeId.Value, (double[])center.Clone()));
                    }
                    jointReferencePoints.Add(joint.Name, new CalReferencePoint[] { master, slave });
                    //
                    if (fixedNodeId == 0 && NeedsFixedEquationNode(joint.JointType, axis))
                        fixedNodeId = AddFixedEquationNode(joint.TwoD, ref maxNodeId, ref additionalNodes,
                                                           ref additionalNodeSets, ref additionalBoundaryConditions);
                    AddJointEquations(joint.JointType, axis, master, slave, fixedNodeId, equationParameters);
                }
            }
        }
        static private void AddJointSprings(FeModel model, Joint joint, double[] center, double[] axis, ref int maxNodeId,
                                            ref int maxElementId, ref List<FeNode> additionalNodes,
                                            ref List<CalElement> additionalElementKeywords,
                                            ref HashSet<string> additionalElementSetNames,
                                            ref List<SectionData> additionalSectionData, List<double[]> equationParameters,
                                            HashSet<string> reservedElementSetNames)
        {
            // Every region node is connected by axial springs to a frame of nodes on the joint axis, so that a frame node
            // follows the rigid motion of its own position, and the master and slave frames are tied by linear equations
            // between plain nodes. A node has no rotational degrees of freedom, so a frame has two axis nodes to carry the
            // tilt and, when the rotation about the axis is locked, a third node off the axis
            if (axis == null) axis = new double[] { 0, 0, 1 };
            int[] masterNodeIds = model.Mesh.NodeSets[model.Mesh.Surfaces[joint.MasterRegionName].NodeSetName].Labels;
            int[] slaveNodeIds = model.Mesh.NodeSets[model.Mesh.Surfaces[joint.SlaveRegionName].NodeSetName].Labels;
            double[][] offsets = GetJointFrameOffsets(model, joint.JointType, center, axis, masterNodeIds, slaveNodeIds);
            double stiffness = model.GetJointSpringStiffness(joint);
            //
            int[] masterFrame = AddJointFrame(center, offsets, ref maxNodeId, ref additionalNodes);
            int[] slaveFrame = AddJointFrame(center, offsets, ref maxNodeId, ref additionalNodes);
            //
            List<FeElement> springs = new List<FeElement>();
            AddJointSpringSet(model, joint.Name + "_Master_Springs", masterNodeIds, masterFrame, stiffness, ref maxElementId,
                              ref additionalElementSetNames, ref additionalSectionData, reservedElementSetNames, springs);
            AddJointSpringSet(model, joint.Name + "_Slave_Springs", slaveNodeIds, slaveFrame, stiffness, ref maxElementId,
                              ref additionalElementSetNames, ref additionalSectionData, reservedElementSetNames, springs);
            string name = joint.Name + "_Springs";
            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
            reservedElementSetNames.Add(name);
            additionalElementKeywords.Add(new CalElement(FeElementTypeSpring.SPRINGA.ToString(), name, springs));
            //
            AddJointFrameEquations(joint.JointType, axis, masterFrame, slaveFrame, equationParameters);
        }
        static private double[][] GetJointFrameOffsets(FeModel model, JointTypeEnum jointType, double[] center, double[] axis,
                                                       int[] masterNodeIds, int[] slaveNodeIds)
        {
            // The frame nodes stand as far from the centre as the farthest region node, which gives the tilt and the
            // rotation about the axis a lever arm at least as long as the regions themselves
            double distance = 0;
            double[] coor;
            foreach (int[] nodeIds in new int[][] { masterNodeIds, slaveNodeIds })
            {
                foreach (int nodeId in nodeIds)
                {
                    coor = model.Mesh.Nodes[nodeId].Coor;
                    distance = Math.Max(distance, Math.Sqrt(Math.Pow(coor[0] - center[0], 2) + Math.Pow(coor[1] - center[1], 2) +
                                                            Math.Pow(coor[2] - center[2], 2)));
                }
            }
            if (distance <= 0) distance = 1;
            //
            if (jointType == JointTypeEnum.Ball) return new double[][] { new double[3] };
            //
            double[] along = new double[] { distance * axis[0], distance * axis[1], distance * axis[2] };
            double[] back = new double[] { -along[0], -along[1], -along[2] };
            if (jointType == JointTypeEnum.Hinge || jointType == JointTypeEnum.Cylindrical) return new double[][] { back, along };
            //
            double[] normal = GetPerpendicularDirections(axis)[0];
            double[] aside = new double[] { distance * normal[0], distance * normal[1], distance * normal[2] };
            return new double[][] { back, along, aside };
        }
        static private int[] AddJointFrame(double[] center, double[][] offsets, ref int maxNodeId, ref List<FeNode> additionalNodes)
        {
            int[] frame = new int[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                frame[i] = ++maxNodeId;
                additionalNodes.Add(new FeNode(frame[i], new double[] { center[0] + offsets[i][0], center[1] + offsets[i][1],
                                                                        center[2] + offsets[i][2] }));
            }
            return frame;
        }
        static private void AddJointSpringSet(FeModel model, string name, int[] nodeIds, int[] frame, double stiffness,
                                              ref int maxElementId, ref HashSet<string> additionalElementSetNames,
                                              ref List<SectionData> additionalSectionData,
                                              HashSet<string> reservedElementSetNames, List<FeElement> springs)
        {
            if (reservedElementSetNames.Contains(name)) name = reservedElementSetNames.GetNextNumberedKey(name);
            reservedElementSetNames.Add(name);
            //
            List<int> elementIds = new List<int>();
            foreach (int nodeId in nodeIds)
            {
                foreach (int frameNodeId in frame)
                {
                    springs.Add(new LinearSpringElement(++maxElementId, new int[] { nodeId, frameNodeId }));
                    elementIds.Add(maxElementId);
                }
            }
            FeElementSet elementSet = new FeElementSet(name, elementIds.ToArray());
            model.Mesh.ElementSets.Add(elementSet.Name, elementSet);
            additionalElementSetNames.Add(elementSet.Name);
            // A spring section without a direction is an axial spring
            additionalSectionData.Add(new LinearSpringSectionData("LinearSpringSection", name, 0, stiffness));
        }
        static private void AddJointFrameEquations(JointTypeEnum jointType, double[] axis, int[] masterFrame, int[] slaveFrame,
                                                   List<double[]> equationParameters)
        {
            // The frames carry exactly the motions a hinge, a ball and a weld free, so every component of every node pair
            // is tied. A cylindrical joint and a slider free the axial translation, so only the components perpendicular to
            // the axis are tied, and the slider locks the rotation about the axis by giving all node pairs the same axial
            // motion
            bool axialFree = jointType == JointTypeEnum.Cylindrical || jointType == JointTypeEnum.Slider;
            int[] dependentDofs;
            for (int i = 0; i < masterFrame.Length; i++)
            {
                if (!axialFree)
                {
                    for (int dof = 1; dof <= 3; dof++)
                        equationParameters.Add(new double[] { slaveFrame[i], dof, 1, masterFrame[i], dof, -1 });
                    continue;
                }
                dependentDofs = AddPerpendicularEquations(axis, slaveFrame[i], masterFrame[i], 0, equationParameters);
                if (i > 0 && jointType == JointTypeEnum.Slider)
                    AddCommonAxialEquation(axis, slaveFrame[i], masterFrame[i], slaveFrame[0], masterFrame[0], dependentDofs,
                                           equationParameters);
            }
        }
        static private void AddCommonAxialEquation(double[] axis, int dependentNodeId, int independentNodeId,
                                                   int firstDependentNodeId, int firstIndependentNodeId, int[] usedDependentDofs,
                                                   List<double[]> equationParameters)
        {
            // a.(u_s - u_m) - a.(u_s1 - u_m1) = 0, led by the degree of freedom of the slave node no other equation leads with
            int dependentDof = 0;
            double max = 0;
            for (int i = 0; i < 3; i++)
            {
                if (Array.IndexOf(usedDependentDofs, i + 1) < 0 && Math.Abs(axis[i]) > max)
                {
                    max = Math.Abs(axis[i]);
                    dependentDof = i + 1;
                }
            }
            if (dependentDof == 0) return;
            //
            List<double> parameters = new List<double>();
            parameters.Add(dependentNodeId);
            parameters.Add(dependentDof);
            parameters.Add(axis[dependentDof - 1]);
            AddEquationTerms(parameters, dependentNodeId, axis, 1, dependentDof);
            AddEquationTerms(parameters, independentNodeId, axis, -1, 0);
            AddEquationTerms(parameters, firstDependentNodeId, axis, -1, 0);
            AddEquationTerms(parameters, firstIndependentNodeId, axis, 1, 0);
            equationParameters.Add(parameters.ToArray());
        }
        static private void AddEquationTerms(List<double> parameters, int nodeId, double[] direction, double sign, int skipDof)
        {
            for (int i = 0; i < 3; i++)
            {
                if (i + 1 == skipDof || Math.Abs(direction[i]) < SmallestEquationCoefficient) continue;
                parameters.Add(nodeId);
                parameters.Add(i + 1);
                parameters.Add(sign * direction[i]);
            }
        }
        static private bool AllTranslationsLocked(JointTypeEnum jointType)
        {
            return jointType == JointTypeEnum.Hinge || jointType == JointTypeEnum.Ball ||
                   jointType == JointTypeEnum.Weld;
        }
        static private bool AllRotationsLocked(JointTypeEnum jointType)
        {
            return jointType == JointTypeEnum.Slider || jointType == JointTypeEnum.Weld;
        }
        static private void AddJointEquations(JointTypeEnum jointType, double[] axis, CalReferencePoint master,
                                              CalReferencePoint slave, int fixedNodeId,
                                              List<double[]> equationParameters)
        {
            bool perpendicularTranslations = jointType == JointTypeEnum.Cylindrical || jointType == JointTypeEnum.Slider;
            bool perpendicularRotations = jointType == JointTypeEnum.Hinge || jointType == JointTypeEnum.Cylindrical;
            //
            if (perpendicularTranslations)
                AddPerpendicularEquations(axis, slave.ReferenceNodeId, master.ReferenceNodeId, fixedNodeId,
                                          equationParameters);
            if (perpendicularRotations)
                AddPerpendicularEquations(axis, slave.RotationNodeId.Value, master.RotationNodeId.Value, fixedNodeId,
                                          equationParameters);
        }
        static private bool NeedsFixedEquationNode(JointTypeEnum jointType, double[] axis)
        {
            if (jointType != JointTypeEnum.Hinge && jointType != JointTypeEnum.Cylindrical &&
                jointType != JointTypeEnum.Slider) return false;
            //
            double[][] normals = GetPerpendicularDirections(axis);
            return CountUsedComponents(normals[0]) < 2 || CountUsedComponents(normals[1]) < 2;
        }
        static private int AddFixedEquationNode(bool twoD, ref int maxNodeId, ref List<FeNode> additionalNodes,
                                                ref List<FeNodeSet> additionalNodeSets,
                                                ref List<BoundaryCondition> additionalBoundaryConditions)
        {
            // Calculix crashes on an equation that ties two rigid body nodes through a single degree of freedom of
            // each. A joint axis along a global axis leaves nothing else to write, because the two directions
            // perpendicular to it lie along global axes as well. The equations therefore carry two degrees of freedom
            // of a node held at zero, which leaves the constraint untouched and gives them the length Calculix needs
            int fixedNodeId = ++maxNodeId;
            additionalNodes.Add(new FeNode(fixedNodeId, new double[3]));
            string nodeSetName = "Internal_All_Joints_Fixed_NodeSet";
            additionalNodeSets.Add(new FeNodeSet(nodeSetName, new int[] { fixedNodeId }));
            DisplacementRotation fixedNode = new DisplacementRotation("Joints_Fixed_BC", nodeSetName,
                                                                      RegionTypeEnum.NodeSetName, twoD, false, 0);
            fixedNode.U1.SetEquationFromValue(0);
            fixedNode.U2.SetEquationFromValue(0);
            fixedNode.U3.SetEquationFromValue(0);
            additionalBoundaryConditions.Add(fixedNode);
            //
            return fixedNodeId;
        }
        static private int[] AddPerpendicularEquations(double[] axis, int dependentNodeId, int independentNodeId,
                                                       int fixedNodeId, List<double[]> equationParameters)
        {
            // The relative motion has no component perpendicular to the axis. A degree of freedom may be the dependent
            // one of a single equation, so each equation leads with a different degree of freedom of the slave node,
            // and the degrees of freedom used are returned for the equations that follow
            double[][] normals = GetPerpendicularDirections(axis);
            List<int> usedDependentDofs = new List<int>();
            int dependentDof;
            double max;
            List<double> parameters;
            foreach (var normal in normals)
            {
                dependentDof = 0;
                max = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (!usedDependentDofs.Contains(i + 1) && Math.Abs(normal[i]) > max)
                    {
                        max = Math.Abs(normal[i]);
                        dependentDof = i + 1;
                    }
                }
                usedDependentDofs.Add(dependentDof);
                //
                parameters = new List<double>();
                parameters.Add(dependentNodeId);
                parameters.Add(dependentDof);
                parameters.Add(normal[dependentDof - 1]);
                for (int i = 0; i < 3; i++)
                {
                    if (i + 1 == dependentDof || Math.Abs(normal[i]) < SmallestEquationCoefficient) continue;
                    parameters.Add(dependentNodeId);
                    parameters.Add(i + 1);
                    parameters.Add(normal[i]);
                }
                for (int i = 0; i < 3; i++)
                {
                    if (Math.Abs(normal[i]) < SmallestEquationCoefficient) continue;
                    parameters.Add(independentNodeId);
                    parameters.Add(i + 1);
                    parameters.Add(-normal[i]);
                }
                if (fixedNodeId != 0)
                {
                    // The two perpendicular equations need different fixed terms or Calculix can crash during MPC setup.
                    parameters.Add(fixedNodeId);
                    parameters.Add(1);
                    parameters.Add(1);
                    parameters.Add(fixedNodeId);
                    parameters.Add(usedDependentDofs.Count + 1);
                    parameters.Add(1);
                }
                equationParameters.Add(parameters.ToArray());
            }
            return usedDependentDofs.ToArray();
        }
        static private double[][] GetPerpendicularDirections(double[] axis)
        {
            // The global axis least aligned with the joint axis seeds the first perpendicular direction
            int minIndex = 0;
            for (int i = 1; i < 3; i++)
            {
                if (Math.Abs(axis[i]) < Math.Abs(axis[minIndex])) minIndex = i;
            }
            double[] seed = new double[3];
            seed[minIndex] = 1;
            double[] n1 = new double[3];
            double[] n2 = new double[3];
            Vec3D.CrossProduct(axis, seed, ref n1);
            Vec3D.Normalize(ref n1);
            Vec3D.CrossProduct(axis, n1, ref n2);
            Vec3D.Normalize(ref n2);
            //
            return new double[][] { n1, n2 };
        }
        static private int CountUsedComponents(double[] direction)
        {
            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                if (Math.Abs(direction[i]) >= SmallestEquationCoefficient) count++;
            }
            return count;
        }
        static private CalRigidBody GetJointRigidBody(FeModel model, string name, string surfaceName,
                                                      CalReferencePoint referencePoint, bool twoD)
        {
            RigidBody rigidBody = new RigidBody(name, name, surfaceName, RegionTypeEnum.SurfaceName, twoD);
            Dictionary<string, CalReferencePoint> referencePoints = new Dictionary<string, CalReferencePoint>();
            referencePoints.Add(name, referencePoint);
            //
            return new CalRigidBody(rigidBody, referencePoints, model.Mesh.Surfaces[surfaceName].NodeSetName);
        }
        static private void AppendConstraints(FeModel model, Dictionary<string, CalReferencePoint> referencePoints,
                                              Dictionary<string, CalReferencePoint[]> jointReferencePoints,
                                              List<double[]> equationParameters,
                                              Dictionary<string, string> cyclicSymmetryElementSetNames,
                                              CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                CalReferencePoint[] jointRps;
                foreach (var entry in model.Constraints)
                {
                    if (entry.Value.Active)
                    {
                        if (entry.Value is PointSpring) { } // this constraint is split into elements and a section
                        else if (entry.Value is SurfaceSpring) { } // this constraint is split into point springs
                        else if (entry.Value is CompressionOnly) { } // this constraint is split into GAPUNI elements and GAP section
                        else if (entry.Value is Joint joint)
                        {
                            if (jointReferencePoints.TryGetValue(joint.Name, out jointRps))
                            {
                                parent.AddKeyword(GetJointRigidBody(model, joint.Name + "_Master", joint.MasterRegionName,
                                                                    jointRps[0], joint.TwoD));
                                parent.AddKeyword(GetJointRigidBody(model, joint.Name + "_Slave", joint.SlaveRegionName,
                                                                    jointRps[1], joint.TwoD));
                            }
                        }
                        else if (entry.Value is RigidBody rb)
                        {
                            string surfaceNodeSetName = null;
                            if (rb.RegionType == RegionTypeEnum.SurfaceName)
                                surfaceNodeSetName = model.Mesh.Surfaces[rb.RegionName].NodeSetName;
                            CalRigidBody calRigidBody = new CalRigidBody(rb, referencePoints, surfaceNodeSetName);
                            parent.AddKeyword(calRigidBody);
                        }
                        else if (entry.Value is Coupling coupling)
                        {
                            CalCoupling calCoupling = new CalCoupling(coupling, referencePoints);
                            if (coupling is KinematicCoupling kc)
                                calCoupling.AddKeyword(new CalKinematic(kc));
                            else if (coupling is DistributingCoupling dc)
                                calCoupling.AddKeyword(new CalDistributing(dc));
                            else throw new NotImplementedException();
                            parent.AddKeyword(calCoupling);
                        }
                        else if (entry.Value is CyclicSymmetry cyclicSymmetry)
                        {
                            CalTie calTie = new CalTie(cyclicSymmetry);
                            cyclicSymmetryElementSetNames.TryGetValue(cyclicSymmetry.Name,
                                                                      out string elementSetName);
                            CalCyclicSymmetry calCyclicSymmetry =
                                new CalCyclicSymmetry(cyclicSymmetry, elementSetName);
                            calTie.AddKeyword(calCyclicSymmetry);
                            parent.AddKeyword(calTie);
                        }
                        else if (entry.Value is Tie tie)
                        {
                            CalTie calTie = new CalTie(tie);
                            parent.AddKeyword(calTie);
                        }
                        else throw new NotImplementedException();
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
                // Equations
                foreach (var equationParameter in equationParameters)
                {
                    parent.AddKeyword(new CalEquation(equationParameter));
                }
            }
        }
        static private void AppendSurfaceInteractions(FeModel model, CalculixKeyword parent)
        {
            CalSurfaceInteraction surfaceInteraction;
            foreach (var entry in model.SurfaceInteractions)
            {
                if (entry.Value.Active)
                {
                    surfaceInteraction = new CalSurfaceInteraction(entry.Value);
                    parent.AddKeyword(surfaceInteraction);
                    //
                    foreach (var property in entry.Value.Properties)
                    {
                        if (property is SurfaceBehavior sb) surfaceInteraction.AddKeyword(new CalSurfaceBehavior(sb));
                        else if (property is Friction fr) surfaceInteraction.AddKeyword(new CalFriction(fr));
                        else if (property is GapConductance gc) surfaceInteraction.AddKeyword(new CalGapConductance(gc));
                        else throw new NotImplementedException();
                    }
                }
                else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
            }
        }
        static private void AppendContactPairs(FeModel model, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                foreach (var entry in model.ContactPairs)
                {
                    if (entry.Value.Active)
                    {
                        CalContactPair calContactPair = new CalContactPair(entry.Value);
                        parent.AddKeyword(calContactPair);
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
            }
        }
        static private void AppendAmplitudes(FeModel model, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                foreach (var entry in model.Amplitudes)
                {
                    if (entry.Value.Active)
                    {
                        if (entry.Value is Amplitude a)
                        {
                            CalAmplitude calAmplitude = new CalAmplitude(a);
                            parent.AddKeyword(calAmplitude);
                        }
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
            }
        }
        static private void AppendInitialConditions(FeModel model, Dictionary<string, CalReferencePoint> referencePoints,
                                                    CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                foreach (var entry in model.InitialConditions)
                {
                    if (entry.Value.Active)
                    {
                        if (entry.Value is InitialTemperature it)
                        {
                            CalInitialTemperature calInitialTemperature = new CalInitialTemperature(model, it);
                            parent.AddKeyword(calInitialTemperature);
                        }
                        else if (entry.Value is InitialTranslationalVelocity itv)
                        {
                            CalInitialTranslationalVelocity calInitialVelocity =
                                new CalInitialTranslationalVelocity(model, itv, referencePoints);
                            parent.AddKeyword(calInitialVelocity);
                        }
                        else if (entry.Value is InitialAngularVelocity iav)
                        {
                            CalInitialAngularVelocity calInitialVelocity =
                                new CalInitialAngularVelocity(model, iav, referencePoints);
                            parent.AddKeyword(calInitialVelocity);
                        }
                        else throw new NotSupportedException();
                    }
                    else parent.AddKeyword(new CalDeactivated(entry.Value.Name));
                }
            }
        }
        //
        static private void AppendSteps(FeModel model, List<BoundaryCondition> additionalBoundaryConditions,
                                        Dictionary<string, CalReferencePoint> referencePoints, CalculixKeyword parent)
        {
            CalTitle title;
            HashSet<Type> loadTypes = model.StepCollection.GetAllLoadTypes();
            bool cyclicSymmetry = model.Constraints.Any(entry => entry.Value.Active &&
                                                                 entry.Value is CyclicSymmetry);
            //
            if (model.StepCollection.GetNumberOfActiveSteps() == 0 && model.StepCollection.OnlyCheckModel)
            {
                StaticStep step = new StaticStep("CheckModel");
                step.RunAnalysis = false;
                //
                title = new CalTitle(step.Name, "");
                parent.AddKeyword(title);
                //
                CalculixKeyword calStepHeader = new CalStepHeader(step);
                title.AddKeyword(calStepHeader);
                //
                CalStaticStep calStaticStep = new CalStaticStep(step);
                calStepHeader.AddKeyword(calStaticStep);
                //
                CalEndStep endStep = new CalEndStep();
                title.AddKeyword(endStep);
                return;
            }
            //
            string previousBoundaryConditionsText = null;
            bool previousOutputFrequencySet = false;
            foreach (var step in model.StepCollection.StepsList)
            {
                if (step is InitialStep) continue;
                else if (step is BoundaryDisplacementStep boundaryDisplacementStep) continue;
                //
                title = new CalTitle(step.Name, "");
                parent.AddKeyword(title);
                //
                CalculixKeyword calStepHeader;
                if (step.Active) calStepHeader = new CalStepHeader(step);
                else calStepHeader = new CalDeactivated(step.Name);
                title.AddKeyword(calStepHeader);
                // Step type
                if (step.Active)
                {
                    if (step.GetType() == typeof(StaticStep) || step is SlipWearStep)
                    {
                        StaticStep staticStep = step as StaticStep;
                        CalStaticStep calStaticStep = new CalStaticStep(staticStep);
                        calStepHeader.AddKeyword(calStaticStep);                        
                    }
                    else if (step is FrequencyStep frequencyStep)
                    {
                        CalFrequencyStep calFrequencyStep = new CalFrequencyStep(frequencyStep);
                        calStepHeader.AddKeyword(calFrequencyStep);
                        if (cyclicSymmetry)
                            calStepHeader.AddKeyword(new CalSelectCyclicSymmetryModes(frequencyStep));
                    }
                    else if (step is ComplexFrequencyStep complexFrequencyStep)
                    {
                        CalComplexFrequencyStep calComplexFrequencyStep = new CalComplexFrequencyStep(complexFrequencyStep);
                        calStepHeader.AddKeyword(calComplexFrequencyStep);
                    }
                    else if (step is BuckleStep buckleStep)
                    {
                        CalBuckleStep calBuckleStep = new CalBuckleStep(buckleStep);
                        calStepHeader.AddKeyword(calBuckleStep);
                    }
                    else if (step is ModalDynamicsStep modalStep)
                    {
                        CalModalDynamicsStep calModalStep = new CalModalDynamicsStep(modalStep, cyclicSymmetry);
                        calStepHeader.AddKeyword(calModalStep);
                        // Damping
                        if (modalStep.ModalDamping.DampingType != ModalDampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalModalDamping calModalDamping = new CalModalDamping(modalStep.ModalDamping);
                            damping.AddKeyword(calModalDamping);
                            calStepHeader.AddKeyword(damping);
                        }
                    }
                    else if (step is SteadyStateDynamicsStep steadyStep)
                    {
                        CalSteadyStateDynamicsStep calSteadyStep =
                            new CalSteadyStateDynamicsStep(steadyStep, cyclicSymmetry);
                        calStepHeader.AddKeyword(calSteadyStep);
                        // Damping
                        if (steadyStep.ModalDamping.DampingType != ModalDampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalModalDamping calModalDamping = new CalModalDamping(steadyStep.ModalDamping);
                            damping.AddKeyword(calModalDamping);
                            calStepHeader.AddKeyword(damping);
                        }
                    }
                    else if (step.GetType() == typeof(DynamicStep))
                    {
                        DynamicStep dynamicStep = step as DynamicStep;
                        CalDynamicStep calDynamicStep = new CalDynamicStep(dynamicStep);
                        calStepHeader.AddKeyword(calDynamicStep);
                        // Damping
                        if (dynamicStep.Damping.DampingType != DampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalDamping calDamping = new CalDamping(dynamicStep.Damping);
                            damping.AddKeyword(calDamping);
                            calStepHeader.AddKeyword(damping);
                        }
                    }
                    else if (step.GetType() == typeof(HeatTransferStep))
                    {
                        HeatTransferStep heatTransferStep = step as HeatTransferStep;
                        CalHeatTransferStep calHeatTransferStep = new CalHeatTransferStep(heatTransferStep);
                        calStepHeader.AddKeyword(calHeatTransferStep);
                    }
                    else if (step.GetType() == typeof(UncoupledTempDispStep))
                    {
                        UncoupledTempDispStep uncoupledTempDispStep = step as UncoupledTempDispStep;
                        CalUncoupledTempDispStep calUncoupledTempDispStep = new CalUncoupledTempDispStep(uncoupledTempDispStep);
                        calStepHeader.AddKeyword(calUncoupledTempDispStep);
                    }
                    else if (step.GetType() == typeof(CoupledTempDispStep))
                    {
                        CoupledTempDispStep coupledTempDispStep = step as CoupledTempDispStep;
                        CalCoupledTempDispStep calCoupledTempDispStep = new CalCoupledTempDispStep(coupledTempDispStep);
                        calStepHeader.AddKeyword(calCoupledTempDispStep);
                    }
                    else throw new NotImplementedException();
                    // Controls
                    title = new CalTitle("Controls", "");
                    calStepHeader.AddKeyword(title);
                    CalculixKeyword calParameter;
                    foreach (var parameter in step.StepControls.Parameters)
                    {
                        if (parameter is ResetStepControlParameter rscp)
                            calParameter = new CalResetParameter(rscp);
                        else if (parameter is TimeIncrementationStepControlParameter tiscp)
                            calParameter = new CalTimeIncrementationParameter(tiscp);
                        else if (parameter is FieldStepControlParameter fscp)
                            calParameter = new CalFieldParameter(fscp);
                        else if (parameter is ContactStepControlParameter cscp)
                            calParameter = new CalContactParameter(cscp);
                        else throw new NotImplementedException();
                        //
                        title.AddKeyword(calParameter);
                    }
                    // Frequency
                    title = new CalTitle("Output frequency", "");
                    calStepHeader.AddKeyword(title);
                    CalOutputFrequency calOutputFrequency = new CalOutputFrequency(step.OutputFrequency);
                    // CalculiX keeps the frequency of the step before, so a default after a set one is written out
                    if (!calOutputFrequency.IsDefault() || previousOutputFrequencySet) title.AddKeyword(calOutputFrequency);
                    previousOutputFrequencySet = !calOutputFrequency.IsDefault();
                }
                else calStepHeader.AddKeyword(new CalDeactivated(step.GetType().ToString()));
                // Boundary conditions title
                title = new CalTitle("Boundary conditions", "");
                calStepHeader.AddKeyword(title);
                // Boundary conditions
                CalTitle boundaryConditions = new CalTitle("", "");
                foreach (var bcEntry in step.BoundaryConditions)
                {
                    if (step.Active && bcEntry.Value.Active)
                        AppendBoundaryCondition(model, step, bcEntry.Value, referencePoints, boundaryConditions);
                    else boundaryConditions.AddKeyword(new CalDeactivated(bcEntry.Value.Name));
                }
                // Additional boundary conditions
                AppendAdditionalBoundaryConditions(model, step, additionalBoundaryConditions, referencePoints,
                                                   boundaryConditions);
                // Boundary conditions op - a step that applies the boundary conditions of the step before it again keeps
                // them, because CalculiX breaks the rotational constraints of expanded shell nodes on op=New
                string boundaryConditionsText = GetKeywordsTextWithoutComments(boundaryConditions);
                if (step.Active && !(step is ModalDynamicsStep) && boundaryConditionsText != previousBoundaryConditionsText)
                {
                    title.AddKeyword(new CalOpParameter(OpKeywordEnum.Boundary, OpTypeEnum.New));
                }
                if (step.Active) previousBoundaryConditionsText = boundaryConditionsText;
                foreach (var keyword in boundaryConditions.Keywords) title.AddKeyword(keyword);
                // Loads title
                title = new CalTitle("Loads", "");
                calStepHeader.AddKeyword(title);
                // Load op
                if (step.Active)
                {
                    if (step.IsLoadTypeSupported(typeof(CLoad)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Cload, OpTypeEnum.New));
                    }
                    if (step.IsLoadTypeSupported(typeof(DLoad)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Dload, OpTypeEnum.New));
                    }
                    if (step.IsLoadTypeSupported(typeof(CFlux)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Cflux, OpTypeEnum.New));
                    }
                    if (step.IsLoadTypeSupported(typeof(DFlux)) && loadTypes.Contains(typeof(DFlux)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Dflux, OpTypeEnum.New));
                    }
                    if (step.IsLoadTypeSupported(typeof(FilmHeatTransfer)) && loadTypes.Contains(typeof(FilmHeatTransfer)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Film, OpTypeEnum.New));
                    }
                    if (step.IsLoadTypeSupported(typeof(RadiationHeatTransfer)) &&
                        loadTypes.Contains(typeof(RadiationHeatTransfer)))
                    {
                        title.AddKeyword(new CalOpParameter(OpKeywordEnum.Radiate, OpTypeEnum.New));
                    }
                }
                // Loads
                foreach (var loadEntry in step.Loads)
                {
                    if (step.Active && loadEntry.Value.Active) AppendLoad(model, step, loadEntry.Value, referencePoints,
                                                                          title);
                    else title.AddKeyword(new CalDeactivated(loadEntry.Value.Name));
                }
                // Defined fields title
                title = new CalTitle("Defined fields", "");
                calStepHeader.AddKeyword(title);
                // Defined fields op
                // A deactivated defined field is as good as none, which keeps the temperatures of the step before
                if (step.Active && step.DefinedFields.Values.Any(definedField => definedField.Active))
                {
                    title.AddKeyword(new CalOpParameter(OpKeywordEnum.Temperature, OpTypeEnum.New));
                }
                // Defined fields
                foreach (var definedFieldEntry in step.DefinedFields)
                {
                    if (step.Active && definedFieldEntry.Value.Active) AppendDefinedField(model, definedFieldEntry.Value, title);
                    else title.AddKeyword(new CalDeactivated(definedFieldEntry.Value.Name));
                }
                // History outputs
                title = new CalTitle("History outputs", "");
                calStepHeader.AddKeyword(title);
                //
                foreach (var historyOutputEntry in step.HistoryOutputs)
                {
                    if (step.Active && historyOutputEntry.Value.Active) AppendHistoryOutput(model, step, 
                                                                                            historyOutputEntry.Value,
                                                                                            title);
                    else title.AddKeyword(new CalDeactivated(historyOutputEntry.Value.Name));
                }
                // Field outputs
                title = new CalTitle("Field outputs", "");
                calStepHeader.AddKeyword(title);
                //
                foreach (var fieldOutputEntry in step.FieldOutputs)
                {
                    if (step.Active && fieldOutputEntry.Value.Active)
                        AppendFieldOutput(model, step, fieldOutputEntry.Value, title);
                    else title.AddKeyword(new CalDeactivated(fieldOutputEntry.Value.Name));
                }
                //
                title = new CalTitle("End step", "");
                calStepHeader.AddKeyword(title);
                if (step.Active)
                {
                    CalEndStep endStep = new CalEndStep();
                    title.AddKeyword(endStep);
                }
                else title.AddKeyword(new CalDeactivated(step.Name));
            }
        }
        static private void AppendBoundaryCondition(FeModel model, Step step, BoundaryCondition boundaryCondition,
                                                    Dictionary<string, CalReferencePoint> referencePoints, CalculixKeyword parent)
        {
            ComplexLoadTypeEnum complexLoadType;
            if (boundaryCondition.Complex) complexLoadType = ComplexLoadTypeEnum.Real;
            else complexLoadType = ComplexLoadTypeEnum.None;
            //
            AppendBoundaryCondition(model, boundaryCondition, referencePoints, complexLoadType, parent);
            // Load case=2 - Imaginary component
            if (step is SteadyStateDynamicsStep && boundaryCondition.Complex && boundaryCondition.PhaseDeg.Value != 0)
                AppendBoundaryCondition(model, boundaryCondition, referencePoints, ComplexLoadTypeEnum.Imaginary, parent);
        }
        static private void AppendBoundaryCondition(FeModel model, BoundaryCondition boundaryCondition,
                                                    Dictionary<string, CalReferencePoint> referencePoints,
                                                    ComplexLoadTypeEnum complexLoadType, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                if (boundaryCondition is FixedBC fix)
                {
                    string nodeSetNameOfSurface = null;
                    if (fix.RegionType == RegionTypeEnum.SurfaceName)
                        nodeSetNameOfSurface = model.Mesh.Surfaces[fix.RegionName].NodeSetName;
                    CalFixedBC calFixedBC = new CalFixedBC(fix, referencePoints, nodeSetNameOfSurface);
                    parent.AddKeyword(calFixedBC);
                }
                else if (boundaryCondition is DisplacementRotation dispRot)
                {
                    string nodeSetNameOfSurface = null;
                    if (dispRot.RegionType == RegionTypeEnum.SurfaceName)
                        nodeSetNameOfSurface = model.Mesh.Surfaces[dispRot.RegionName].NodeSetName;
                    CalDisplacementRotation calDisplacementRotation =
                        new CalDisplacementRotation(dispRot, referencePoints, nodeSetNameOfSurface, complexLoadType, model);
                    parent.AddKeyword(calDisplacementRotation);
                }
                else if (boundaryCondition is SubmodelBC sm)
                {
                    string surfaceNodeSetName = null;
                    if (sm.RegionType == RegionTypeEnum.SurfaceName)
                        surfaceNodeSetName = model.Mesh.Surfaces[sm.RegionName].NodeSetName;
                    CalSubmodelBC calSubmodelBC = new CalSubmodelBC(sm, surfaceNodeSetName);
                    parent.AddKeyword(calSubmodelBC);
                }
                else if (boundaryCondition is TemperatureBC tmp)
                {
                    string surfaceNodeSetName = null;
                    if (tmp.RegionType == RegionTypeEnum.SurfaceName)
                        surfaceNodeSetName = model.Mesh.Surfaces[tmp.RegionName].NodeSetName;
                    CalTemperatureBC calTemperatureBC = new CalTemperatureBC(model, tmp, surfaceNodeSetName);
                    parent.AddKeyword(calTemperatureBC);
                }
                else throw new NotImplementedException();
            }
        }
        static private void AppendAdditionalBoundaryConditions(FeModel model, Step step,
                                                               List<BoundaryCondition> additionalBoundaryConditions,
                                                               Dictionary<string, CalReferencePoint> referencePoints,
                                                               CalculixKeyword parent)
        {
            CalculixUserKeyword tmp = new CalculixUserKeyword("");
            //
            foreach (var additionalBoundaryCondition in additionalBoundaryConditions)
            {
                if (step.Active && additionalBoundaryCondition.Active)
                    AppendBoundaryCondition(model, step, additionalBoundaryCondition, referencePoints, tmp);
                else tmp.AddKeyword(new CalDeactivated(additionalBoundaryCondition.Name));
            }
            //
            CalAdditional calAdditionalBoundaryConditions = new CalAdditional("Additional boundary conditions", tmp.Keywords);
            if (calAdditionalBoundaryConditions.GetDataString().Length > 0) parent.AddKeyword(calAdditionalBoundaryConditions);
        }
        static private void AppendLoad(FeModel model, Step step, Load load, Dictionary<string, CalReferencePoint> referencePoints,
                                       CalculixKeyword parent)
        {
            ComplexLoadTypeEnum complexLoadType;
            if (load.Complex) complexLoadType = ComplexLoadTypeEnum.Real;
            else complexLoadType = ComplexLoadTypeEnum.None;
            //
            AppendLoad(model, load, referencePoints, complexLoadType, parent);
            // Load case=2 - Imaginary component
            if (step is SteadyStateDynamicsStep && load.Complex && load.PhaseDeg.Value != 0)
                AppendLoad(model, load, referencePoints, ComplexLoadTypeEnum.Imaginary, parent);
        }
        static private void AppendLoad(FeModel model, Load load, Dictionary<string, CalReferencePoint> referencePoints,
                                       ComplexLoadTypeEnum complexLoadType, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                if (load is CLoad cl)
                {
                    CalCLoad cLoad = new CalCLoad(model.Mesh, cl, referencePoints, complexLoadType);
                    parent.AddKeyword(cLoad);
                }
                else if (load is MomentLoad ml)
                {
                    CalMomentLoad mLoad = new CalMomentLoad(ml, referencePoints, complexLoadType);
                    parent.AddKeyword(mLoad);
                }
                else if (load is DLoad dl)
                {
                    if (!model.IsDistributionAvailable(dl.DistributionName))
                    {
                        CalDLoad dLoad = new CalDLoad(dl, model.Mesh.Surfaces[dl.RegionName], complexLoadType);
                        parent.AddKeyword(dLoad);
                    }
                    else
                    {
                        CalVariablePressureLoad vpLoad = new CalVariablePressureLoad(model, dl, complexLoadType);
                        parent.AddKeyword(vpLoad);
                    }
                }
                else if (load is HydrostaticPressure hpl)
                {
                    CalVariablePressureLoad vpLoad = new CalVariablePressureLoad(model, hpl, complexLoadType);
                    parent.AddKeyword(vpLoad);
                }
                else if (load is ImportedPressure ipl)
                {
                    CalImportedPressureLoad ipLoad = new CalImportedPressureLoad(model, ipl, complexLoadType);
                    parent.AddKeyword(ipLoad);
                }
                else if (load is STLoad stl)
                {
                    CalSTLoad stLoad = new CalSTLoad(model, stl, complexLoadType);
                    parent.AddKeyword(stLoad);
                }
                else if (load is ImportedSTLoad istl)
                {
                    CalImportedSTLoad istLoad = new CalImportedSTLoad(model, istl, complexLoadType);
                    parent.AddKeyword(istLoad);
                }
                else if (load is ShellEdgeLoad sel)
                {
                    CalShellEdgeLoad seLoad = new CalShellEdgeLoad(sel, model.Mesh.Surfaces[sel.SurfaceName], complexLoadType);
                    parent.AddKeyword(seLoad);
                }
                else if (load is GravityLoad gl)
                {
                    MassSection massSection; 
                    GravityLoad glClone = gl.DeepClone();
                    if (gl.RegionType == RegionTypeEnum.MassSection)
                    {
                        massSection = (MassSection)model.Sections[glClone.RegionName];
                        glClone.RegionName = massSection.ElementSetName;
                    }
                    //
                    CalGravityLoad gLoad = new CalGravityLoad(glClone, complexLoadType);
                    parent.AddKeyword(gLoad);
                }
                else if (load is CentrifLoad cfl)
                {
                    MassSection massSection;
                    CentrifLoad cflClone = cfl.DeepClone();
                    if (cfl.RegionType == RegionTypeEnum.MassSection)
                    {
                        massSection = (MassSection)model.Sections[cflClone.RegionName];
                        cflClone.RegionName = massSection.ElementSetName;
                    }
                    //
                    CalCentrifLoad cLoad = new CalCentrifLoad(cflClone, complexLoadType);
                    parent.AddKeyword(cLoad);
                }
                else if (load is PreTensionLoad ptl)
                {
                    string name = ptl.SurfaceName;
                    if (!ptl.AutoComputeDirection) name += "_" + ptl.X.ToString() + ptl.Y.ToString() + ptl.Z.ToString();
                    //
                    CalculixKeyword calKey;
                    if (ptl.Type == PreTensionLoadType.Force)
                    {
                        int nodeId = referencePoints[name].ReferenceNodeId;
                        CLoad cLoad = new CLoad(ptl.Name, nodeId, ptl.GetMagnitudeValue(), 0, 0, ptl.TwoD,
                                                ptl.Complex, ptl.PhaseDeg.Value);
                        cLoad.AmplitudeName = ptl.AmplitudeName;
                        calKey = new CalCLoad(model.Mesh, cLoad, referencePoints, complexLoadType);
                    }
                    else if (ptl.Type == PreTensionLoadType.Displacement)
                    {
                        DisplacementRotation dr = new DisplacementRotation(ptl.Name, name, RegionTypeEnum.ReferencePointName,
                                                                           ptl.TwoD, ptl.Complex, ptl.PhaseDeg.Value);
                        dr.U1.SetEquationFromValue(ptl.GetMagnitudeValue());
                        dr.AmplitudeName = ptl.AmplitudeName;
                        calKey = new CalDisplacementRotation(dr, referencePoints, null, complexLoadType);
                    }
                    else throw new NotSupportedException();
                    //
                    parent.AddKeyword(calKey);
                }
                // Thermo
                else if (load is CFlux cf)
                {
                    CalCFlux cFlux = new CalCFlux(cf);
                    parent.AddKeyword(cFlux);
                }
                else if (load is DFlux df)
                {
                    CalDFlux dFlux = new CalDFlux(df);
                    parent.AddKeyword(dFlux);
                }
                else if (load is BodyFlux bf)
                {
                    CalBodyFlux bFlux = new CalBodyFlux(bf);
                    parent.AddKeyword(bFlux);
                }
                else if (load is FilmHeatTransfer fht)
                {
                    CalFilmHeatTransfer filmHeatTransfer = new CalFilmHeatTransfer(fht, model.Mesh.Surfaces[fht.SurfaceName]);
                    parent.AddKeyword(filmHeatTransfer);
                }
                else if (load is RadiationHeatTransfer rht)
                {
                    CalRadiationHeatTransfer radiationHeatTransfer =
                        new CalRadiationHeatTransfer(rht, model.Mesh.Surfaces[rht.SurfaceName]);
                    parent.AddKeyword(radiationHeatTransfer);
                }
                else throw new NotImplementedException();
            }
        }
        static private void AppendDefinedField(FeModel model, DefinedField definedField, CalculixKeyword parent)
        {
            if (definedField is DefinedTemperature dt)
            {
                CalDefinedTemperature definedTemperature = new CalDefinedTemperature(model, dt);
                parent.AddKeyword(definedTemperature);
            }
            else throw new NotImplementedException();
        }
        static private void AppendHistoryOutput(FeModel model, Step step, HistoryOutput historyOutput,
                                                CalculixKeyword parent)
        {
            if (historyOutput is NodalHistoryOutput nho)
            {
                CalNodePrint nodePrint;
                if (nho.RegionType == RegionTypeEnum.ReferencePointName)
                {
                    FeReferencePoint rp = model.Mesh.ReferencePoints[nho.RegionName];
                    nodePrint = new CalNodePrint(nho, rp.RefNodeSetName, step.OutputFrequency);
                    parent.AddKeyword(nodePrint);
                    if (rp.RotNodeSetName != null)
                    {
                        nodePrint = new CalNodePrint(nho, rp.RotNodeSetName, step.OutputFrequency);
                        parent.AddKeyword(nodePrint);
                    }
                }
                else
                {
                    nodePrint = new CalNodePrint(model, nho, step.OutputFrequency);
                    parent.AddKeyword(nodePrint);
                }
            }
            else if (historyOutput is ElementHistoryOutput eho)
            {
                CalElPrint elPrint = new CalElPrint(eho, step.OutputFrequency);
                parent.AddKeyword(elPrint);
            }
            else if (historyOutput is ContactHistoryOutput cho)
            {
                ContactPair cp = model.ContactPairs[cho.RegionName];

                CalContactPrint contactPrint = new CalContactPrint(cho, cp.MasterRegionName, cp.SlaveRegionName,
                                                                   model.Mesh.Surfaces[cp.MasterRegionName].NodeSetName,
                                                                   model.Mesh.Surfaces[cp.SlaveRegionName].NodeSetName,
                                                                   step.OutputFrequency);
                parent.AddKeyword(contactPrint);
            }
            else if (historyOutput is SectionHistoryOutput sho)
            {
                CalSectionPrint sectionPrint = new CalSectionPrint(sho, step.OutputFrequency);
                parent.AddKeyword(sectionPrint);
            }
            else throw new NotImplementedException();
        }
        static private void AppendFieldOutput(FeModel model, Step step, FieldOutput fieldOutput, CalculixKeyword parent)
        {
            if (fieldOutput is NodalFieldOutput nfo)
            {
                CalNodeFile nodeFile = new CalNodeFile(nfo, step.OutputFrequency);
                parent.AddKeyword(nodeFile);
            }
            else if (fieldOutput is ElementFieldOutput efo)
            {
                CalElFile elFile = new CalElFile(efo, step.OutputFrequency);
                parent.AddKeyword(elFile);
            }
            else if (fieldOutput is ContactFieldOutput cfo)
            {
                CalContactFile conFile = new CalContactFile(cfo);
                parent.AddKeyword(conFile);
            }
            else throw new NotImplementedException();
        }
        //
        static void AppendTitle(StringBuilder sb, string title)
        {
            sb.AppendLine("************************************************************");
            sb.AppendLine("** " + title);
            sb.AppendLine("************************************************************");
        }
    }
}
