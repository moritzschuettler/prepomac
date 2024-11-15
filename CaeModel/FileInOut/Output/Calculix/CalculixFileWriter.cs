using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace FileInOut.Output
{
    [Serializable]
    public static class CalculixFileWriter
    {
        // Variables                                                                                                                
        private static ConvertPyramidsToEnum _convertPyramidsToEnum;


        // Methods                                                                                                                  
        static public void Write(string fileName, FeModel model, ConvertPyramidsToEnum convertPyramidsTo,
                                 Dictionary<int, double[]> deformations = null)
        {
            List<CalculixKeyword> keywords = GetAllKeywords(model, convertPyramidsTo, deformations);
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
            AppendMaterials(model, title, null, materialNames, true);
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
                Dictionary<string, int[]> referencePointsNodeIds;
                Dictionary<string, string[]> referencePointsNodeSetNames;
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
                GetReferencePoints(model, preTensionLoads, ref maxNodeId, ref additionalNodeSets,
                                   out referencePointsNodeSetNames, out referencePointsNodeIds);
                // Prepare coordinate system-node set pairs
                GetCoordinateSystemNodeSet(model, referencePointsNodeSetNames, ref coordinateSystemNodeSets);
                // Fix pyramid surfaces
                FixPyramidSurfaces(model, convertPyramidsTo, ref replacedElements, ref additionalElementSetNames);
                // Prepare mass sections
                GetMassSections(model, referencePointsNodeIds, ref maxElementId, ref additionalElementKeywords,
                                ref additionalElementSetNames, ref additionalSectionData);
                // Prepare point springs
                GetPointSprings(model, referencePointsNodeIds, ref maxElementId, ref additionalElementKeywords,
                                ref additionalElementSetNames, ref additionalSectionData);
                // Collect compression only constraints
                GetCompressionOnlyConstraintData(model, ref maxNodeId, ref maxElementId, ref additionalNodes,
                                                 ref additionalNodeSets, ref additionalElementKeywords,
                                                 ref additionalElementSetNames, ref additionalSectionData,
                                                 ref additionalMaterials, ref additionalBoundaryConditions,
                                                 ref equationParameters);
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
                AppendNodes(model, additionalNodes, referencePointsNodeIds, deformations, title);
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
                AppendMaterials(model, title, additionalMaterials, null, addAllMaterials);
                // Sections
                title = new CalTitle("Sections", "");
                keywords.Add(title);
                AppendSections(model, additionalSectionData, title);
                // Pre-tension sections
                title = new CalTitle("Pre-tension sections", "");
                keywords.Add(title);
                AppendPreTensionSections(preTensionLoads, referencePointsNodeIds, title);
                // Constraints
                title = new CalTitle("Constraints", "");
                keywords.Add(title);
                AppendConstraints(model, referencePointsNodeIds, equationParameters, title);
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
                AppendInitialConditions(model, referencePointsNodeIds, title);
                // Steps
                title = new CalTitle("Steps", "");
                keywords.Add(title);
                AppendSteps(model, additionalBoundaryConditions, referencePointsNodeIds, title);
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
        static private void GetReferencePoints(FeModel model, OrderedDictionary<string, List<PreTensionLoad>> preTensionLoads,
                                               ref int maxNodeId, ref List<FeNodeSet> additionalNodeSets,
                                               out Dictionary<string, string[]> referencePointsNodeSetNames,
                                               out Dictionary<string, int[]> referencePointsNodeIds)
        {
            HashSet<string> allNodeSetNames = new HashSet<string>(model.Mesh.NodeSets.Keys);
            foreach (var nodeSet in additionalNodeSets) allNodeSetNames.Add(nodeSet.Name);
            //
            string[] nodeSetNames;
            FeNodeSet rpNodeSet;
            referencePointsNodeIds = new Dictionary<string, int[]>();
            referencePointsNodeSetNames = new Dictionary<string, string[]>();
            if (model.Mesh != null)
            {
                // Fill reference point nodes
                int id = maxNodeId;
                FeReferencePoint rp;
                foreach (var entry in model.Mesh.ReferencePoints)
                {
                    rp = entry.Value;
                    // Check name
                    rp.RefNodeSetName = rp.Name + FeReferencePoint.RefName + id + 1;
                    if (allNodeSetNames.Contains(rp.RefNodeSetName))
                        rp.RefNodeSetName = allNodeSetNames.GetNextNumberedKey(rp.RefNodeSetName);
                    rp.RotNodeSetName = rp.Name + FeReferencePoint.RotName + id + 2;
                    if (allNodeSetNames.Contains(rp.RotNodeSetName))
                        rp.RotNodeSetName = allNodeSetNames.GetNextNumberedKey(rp.RotNodeSetName);
                    //
                    rpNodeSet = new FeNodeSet(rp.RefNodeSetName, new int[] { id + 1 });
                    additionalNodeSets.Add(rpNodeSet);
                    allNodeSetNames.Add(rpNodeSet.Name);
                    //
                    rpNodeSet = new FeNodeSet(rp.RotNodeSetName, new int[] { id + 2 });
                    additionalNodeSets.Add(rpNodeSet);
                    allNodeSetNames.Add(rpNodeSet.Name);
                    //
                    nodeSetNames = new string[] { rp.RefNodeSetName, rp.RotNodeSetName };
                    referencePointsNodeSetNames.Add(entry.Key, nodeSetNames);
                    referencePointsNodeIds.Add(entry.Key, new int[] { id + 1, id + 2 });
                    id += 2;
                }
                //
                foreach (var entry in preTensionLoads)
                {
                    referencePointsNodeIds.Add(entry.Key, new int[] { id + 1 });
                    id++;
                }
                //
                maxNodeId = id;
            }
        }
        static private void GetCoordinateSystemNodeSet(FeModel model, Dictionary<string, string[]> referencePointsNodeSetNames,
                                                       ref Dictionary<CoordinateSystem, HashSet<string>> coordinateSystemNodeSets)
        {
            if (model == null || model.StepCollection == null || model.Mesh.CoordinateSystems == null ||
                model.Mesh.CoordinateSystems.Count() == 0) return;
            //
            string nodeSetName;
            HashSet<string> nodeSetNames;
            FeSurface surface;
            FeReferencePoint referencePoint;
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
            // Loads
            foreach (var load in model.StepCollection.GetAllLoads())
            {
                if (load.Active &&
                    model.Mesh.CoordinateSystems.TryGetValue(load.CoordinateSystemName, out coordinateSystem) &&
                    coordinateSystem != null)
                {
                    if (load is CLoad cLoad)
                    {
                        // Node set
                        if (load.RegionType == RegionTypeEnum.NodeSetName)
                        {
                            nodeSetName = load.RegionName;
                            if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                                nodeSetNames.Add(nodeSetName);
                            else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                        }
                        // Reference point
                        else if (load.RegionType == RegionTypeEnum.ReferencePointName)
                        {
                            referencePoint = model.Mesh.ReferencePoints[load.RegionName];
                            nodeSetName = referencePointsNodeSetNames[referencePoint.Name][0];
                            //
                            if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                                nodeSetNames.Add(nodeSetName);
                            else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                        }
                    }
                    else if (load is STLoad stLoad)
                    {
                        // Surface
                        if (stLoad.RegionType == RegionTypeEnum.SurfaceName)
                        {
                            surface = model.Mesh.Surfaces[stLoad.RegionName];
                            nodeSetName = surface.NodeSetName;
                            if (coordinateSystemNodeSets.TryGetValue(coordinateSystem, out nodeSetNames))
                                nodeSetNames.Add(nodeSetName);
                            else coordinateSystemNodeSets.Add(coordinateSystem, new HashSet<string>() { nodeSetName });
                        }
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
                Dictionary<FeFaceName, FeFaceName> pyramidToNormalFaceName;
                if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                {
                    pyramidToNormalFaceName = new Dictionary<FeFaceName, FeFaceName>() {{ FeFaceName.S1, FeFaceName.S5 },
                                                                                        { FeFaceName.S2, FeFaceName.S1 },
                                                                                        { FeFaceName.S3, FeFaceName.S4 },
                                                                                        { FeFaceName.S4, FeFaceName.S2 },
                                                                                        { FeFaceName.S5, FeFaceName.S3 }};
                }
                else if (convertPyramidsTo == ConvertPyramidsToEnum.Hexahedrons)
                {
                    pyramidToNormalFaceName = new Dictionary<FeFaceName, FeFaceName>() {{ FeFaceName.S1, FeFaceName.S1 },
                                                                                        { FeFaceName.S2, FeFaceName.S3 },
                                                                                        { FeFaceName.S3, FeFaceName.S4 },
                                                                                        { FeFaceName.S4, FeFaceName.S5 },
                                                                                        { FeFaceName.S5, FeFaceName.S6 }};
                }
                else throw new NotSupportedException();
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
                            else
                                throw new NotSupportedException();
                        }
                        else if (element is ParabolicPyramidElement ppe)
                        {
                            if (convertPyramidsTo == ConvertPyramidsToEnum.Wedges)
                                model.Mesh.Elements[elementId] = ppe.ConvertToWedge();
                            else if (convertPyramidsTo == ConvertPyramidsToEnum.Hexahedrons)
                                model.Mesh.Elements[elementId] = ppe.ConvertToHex();
                            else
                                throw new NotSupportedException();
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
                                faceName = pyramidToNormalFaceName[entry.Key];
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
        static private void GetMassSections(FeModel model, Dictionary<string, int[]> referencePointsNodeIds,
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
                            int nodeId = referencePointsNodeIds[referencePoint.Name][0];
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
        static private void GetPointSprings(FeModel model, Dictionary<string, int[]> referencePointsNodeIds, ref int maxElementId,
                                            ref List<CalElement> additionalElementKeywords,
                                            ref HashSet<string> additionalElementSetNames,
                                            ref List<SectionData> additionalSectionData)
        {
            if (model.Mesh != null)
            {
                bool twoD = model.Properties.ModelSpace.IsTwoD();
                int count;
                int[] elementIds;
                int[] refPointIds;
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
                                if (referencePointsNodeIds.TryGetValue(psd.RegionName, out refPointIds))
                                {
                                    newElements.Add(new LinearSpringElement(elementId + 1, new int[] { refPointIds[0] }));
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
                                                                                  stiffnesses[i]));
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
                                additionalSectionData.Add(new GapSectionData("GapSectionData", name, 0, normal,
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
        static public bool AddUserKeywordByIndices(List<CalculixKeyword> keywords, int[] indices, CalculixKeyword userKeyword)
        {
            if (indices.Length == 1)
            {
                keywords.Insert(indices[0], userKeyword);
            }
            else
            {
                bool deactivated = false;
                CalculixKeyword keywordParent = keywords[indices[0]];
                if (keywordParent is CalDeactivated) deactivated = true;
                // Find a parent
                for (int i = 1; i < indices.Length - 1; i++)
                {
                    if (indices[i] < keywordParent.Keywords.Count)
                    {
                        keywordParent = keywordParent.Keywords[indices[i]];
                        if (keywordParent is CalDeactivated) deactivated = true;
                    }
                    else return false;
                }

                // Add the keyword
                if (keywordParent.Keywords.Count < indices[indices.Length - 1]) return false;

                if (!deactivated) keywordParent.Keywords.Insert(indices[indices.Length - 1], userKeyword);
                else keywordParent.Keywords.Insert(indices[indices.Length - 1], new CalDeactivated("User keyword"));
            }
            return true;
        }
        //
        static public void RemoveLostUserKeywords(FeModel model)
        {
            List<CalculixKeyword> keywords = GetModelKeywords(model, ConvertPyramidsToEnum.Wedges, null, true);
            // Add user keywords
            List<int[]> keywordKeysToRemove = new List<int[]>();
            if (model.CalculixUserKeywords != null)
            {
                foreach (var entry in model.CalculixUserKeywords)
                {
                    if (!AddUserKeywordByIndices(keywords, entry.Key, entry.Value.DeepClone())) keywordKeysToRemove.Add(entry.Key);
                }
            }
            // Remove lost user keywords
            foreach (var indices in keywordKeysToRemove)
            {
                model.CalculixUserKeywords.Remove(indices);
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
                !(keyword is CalHydrostaticPressureLoad) &&
                !(keyword is CalImportedPressureLoad) &&
                !(keyword is CalSTLoad) &&
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
        static private void WriteKeywordRecursively(StringBuilder sb, CalculixKeyword keyword)
        {
            sb.Append(keyword.GetKeywordString());
            sb.Append(keyword.GetDataString());
            //
            foreach (var childKeyword in keyword.Keywords)
            {
                WriteKeywordRecursively(sb, childKeyword);
            }
        }
        //
        static private string[] GetAllSubmodelNodeSetNames(FeModel model)
        {
            List<string> nodeSetNames = new List<string>();
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
                                        Dictionary<string, int[]> referencePointsNodeIds,
                                        Dictionary<int, double[]> deformations, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                CalNode node = new CalNode(model, additionalNodes, referencePointsNodeIds, deformations);
                parent.AddKeyword(node);
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
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in additionalElementKeywords)
            {
                sb.Append(keyword.GetKeywordString());
                sb.Append(keyword.GetDataString());
            }
            //
            CalAdditional calAdditionalElements = new CalAdditional("Additional elements", sb.ToString());
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
            StringBuilder sb = new StringBuilder();
            foreach (var nodeSet in additionalNodeSets)
            {
                if (nodeSet.Active)
                {
                    keyword = new CalNodeSet(nodeSet);
                    //
                    sb.Append(keyword.GetKeywordString());
                    sb.Append(keyword.GetDataString());
                    //
                    nodeSetNames.Add(nodeSet.Name);
                }
            }
            //
            CalAdditional calAdditionalNodeSets = new CalAdditional("Additional node sets", sb.ToString());
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
                StringBuilder sb = new StringBuilder();
                foreach (var keyword in tmp.Keywords)
                {
                    sb.Append(keyword.GetKeywordString());
                    sb.Append(keyword.GetDataString());
                }
                //
                CalAdditional calAdditionalElementSets = new CalAdditional("Additional element sets", sb.ToString());
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
                CalSurface calSurface;
                foreach (var entry in model.Mesh.Surfaces)
                {
                    if (entry.Value.Active && entry.Value.ElementFaces != null)
                    {
                        calSurface = new CalSurface(entry.Value, model.Properties.ModelSpace.IsTwoD());
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
                                            string[] materialNames = null,
                                            bool addAllMaterials = false)
        {
            CalMaterial calMaterial;
            HashSet<string> activeMaterialNames;
            //
            if (addAllMaterials) activeMaterialNames = new HashSet<string>(model.Materials.Keys);
            else activeMaterialNames = MaterialNamesUsedInActiveSections(model);
            //
            List<Material> materials = model.Materials.Values.ToList();
            if (additionalMaterials != null)
            {
                foreach (var additionalMaterial in additionalMaterials)
                {
                    materials.Add(additionalMaterial);
                    activeMaterialNames.Add(additionalMaterial.Name);
                }
            }
            //
            foreach (var material in materials)
            {
                if ((material.Active && activeMaterialNames.Contains(material.Name)) ||
                    (materialNames != null && materialNames.Contains(material.Name)))
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
                        else if (property is ElasticWithDensity ewd)
                        {
                            Density density = new Density(new double[][] { new double[] { ewd.Density.Value } });
                            calMaterial.AddKeyword(new CalDensity(density, material.TemperatureDependent));
                            //
                            Elastic elastic = new Elastic(new double[][] { new double[] { ewd.YoungsModulus.Value,
                                                                                          ewd.PoissonsRatio.Value } });
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
            StringBuilder sb = new StringBuilder();
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
                    if (keyword != null)
                    {
                        sb.Append(keyword.GetKeywordString());
                        sb.Append(keyword.GetDataString());
                    }
                }
            }
            //
            CalAdditional calAdditionalSections = new CalAdditional("Additional sections", sb.ToString());
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
                                                     Dictionary<string, int[]> referencePointsNodeIds,
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
                        nodeId = referencePointsNodeIds[preTensionOnSurfaceEntry.Key][0];
                        CalPreTensionSection preTension;
                        if (ptl.AutoComputeDirection) preTension = new CalPreTensionSection(ptl.SurfaceName, nodeId);
                        else preTension = new CalPreTensionSection(ptl.SurfaceName, nodeId, ptl.X.Value, ptl.Y.Value, ptl.Z.Value);
                        parent.AddKeyword(preTension);
                    }
                    else parent.AddKeyword(new CalDeactivated("Pre-tension " + ptl.SurfaceName));
                }
            }
        }
        static private void AppendConstraints(FeModel model, Dictionary<string, int[]> referencePointsNodeIds,
                                              List<double[]> equationParameters, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                foreach (var entry in model.Constraints)
                {
                    if (entry.Value.Active)
                    {
                        if (entry.Value is PointSpring) { } // this constraint is split into elements and a section
                        else if (entry.Value is SurfaceSpring) { } // this constraint is split into point springs
                        else if (entry.Value is CompressionOnly) { } // this constraint is split into GAPUNI elements and GAP section
                        else if (entry.Value is RigidBody rb)
                        {
                            string surfaceNodeSetName = null;
                            if (rb.RegionType == RegionTypeEnum.SurfaceName)
                                surfaceNodeSetName = model.Mesh.Surfaces[rb.RegionName].NodeSetName;
                            CalRigidBody calRigidBody = new CalRigidBody(rb, referencePointsNodeIds, surfaceNodeSetName);
                            parent.AddKeyword(calRigidBody);
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
        static private void AppendInitialConditions(FeModel model, Dictionary<string, int[]> referencePointsNodeIds,
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
                                new CalInitialTranslationalVelocity(model, itv, referencePointsNodeIds);
                            parent.AddKeyword(calInitialVelocity);
                        }
                        else if (entry.Value is InitialAngularVelocity iav)
                        {
                            CalInitialAngularVelocity calInitialVelocity =
                                new CalInitialAngularVelocity(model, iav, referencePointsNodeIds);
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
                                        Dictionary<string, int[]> referencePointsNodeIds, CalculixKeyword parent)
        {
            CalTitle title;
            HashSet<Type> loadTypes = model.StepCollection.GetAllLoadTypes();
            //
            foreach (var step in model.StepCollection.StepsList)
            {
                if (step is InitialStep) continue;
                else if (step is BoundaryDisplacementStep boundaryDisplacementStep) continue;
                //
                title = new CalTitle(step.Name, "");
                parent.AddKeyword(title);
                //
                CalculixKeyword calStep;
                if (step.Active) calStep = new CalStep(step);
                else calStep = new CalDeactivated(step.Name);
                title.AddKeyword(calStep);
                // Step type
                if (step.Active)
                {
                    if (step.GetType() == typeof(StaticStep) || step is SlipWearStep)
                    {
                        StaticStep staticStep = step as StaticStep;
                        CalStaticStep calStaticStep = new CalStaticStep(staticStep);
                        calStep.AddKeyword(calStaticStep);                        
                    }
                    else if (step is FrequencyStep frequencyStep)
                    {
                        CalFrequencyStep calFrequencyStep = new CalFrequencyStep(frequencyStep);
                        calStep.AddKeyword(calFrequencyStep);
                    }
                    else if (step is BuckleStep buckleStep)
                    {
                        CalBuckleStep calBuckleStep = new CalBuckleStep(buckleStep);
                        calStep.AddKeyword(calBuckleStep);
                    }
                    else if (step is ModalDynamicsStep modalStep)
                    {
                        CalModalDynamicsStep calModalStep = new CalModalDynamicsStep(modalStep);
                        calStep.AddKeyword(calModalStep);
                        // Damping
                        if (modalStep.ModalDamping.DampingType != ModalDampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalModalDamping calModalDamping = new CalModalDamping(modalStep.ModalDamping);
                            damping.AddKeyword(calModalDamping);
                            calStep.AddKeyword(damping);
                        }
                    }
                    else if (step is SteadyStateDynamicsStep steadyStep)
                    {
                        CalSteadyStateDynamicsStep calSteadyStep = new CalSteadyStateDynamicsStep(steadyStep);
                        calStep.AddKeyword(calSteadyStep);
                        // Damping
                        if (steadyStep.ModalDamping.DampingType != ModalDampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalModalDamping calModalDamping = new CalModalDamping(steadyStep.ModalDamping);
                            damping.AddKeyword(calModalDamping);
                            calStep.AddKeyword(damping);
                        }
                    }
                    else if (step.GetType() == typeof(DynamicStep))
                    {
                        DynamicStep dynamicStep = step as DynamicStep;
                        CalDynamicStep calDynamicStep = new CalDynamicStep(dynamicStep);
                        calStep.AddKeyword(calDynamicStep);
                        // Damping
                        if (dynamicStep.Damping.DampingType != DampingTypeEnum.Off)
                        {
                            CalTitle damping = new CalTitle("Damping", "");
                            CalDamping calDamping = new CalDamping(dynamicStep.Damping);
                            damping.AddKeyword(calDamping);
                            calStep.AddKeyword(damping);
                        }
                    }
                    else if (step.GetType() == typeof(HeatTransferStep))
                    {
                        HeatTransferStep heatTransferStep = step as HeatTransferStep;
                        CalHeatTransferStep calHeatTransferStep = new CalHeatTransferStep(heatTransferStep);
                        calStep.AddKeyword(calHeatTransferStep);
                    }
                    else if (step.GetType() == typeof(UncoupledTempDispStep))
                    {
                        UncoupledTempDispStep uncoupledTempDispStep = step as UncoupledTempDispStep;
                        CalUncoupledTempDispStep calUncoupledTempDispStep = new CalUncoupledTempDispStep(uncoupledTempDispStep);
                        calStep.AddKeyword(calUncoupledTempDispStep);
                    }
                    else if (step.GetType() == typeof(CoupledTempDispStep))
                    {
                        CoupledTempDispStep coupledTempDispStep = step as CoupledTempDispStep;
                        CalCoupledTempDispStep calCoupledTempDispStep = new CalCoupledTempDispStep(coupledTempDispStep);
                        calStep.AddKeyword(calCoupledTempDispStep);
                    }
                    else throw new NotImplementedException();
                    // Controls
                    title = new CalTitle("Controls", "");
                    calStep.AddKeyword(title);
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
                    calStep.AddKeyword(title);
                    CalOutput calOutput = new CalOutput(step.OutputFrequency);
                    title.AddKeyword(calOutput);
                }
                else calStep.AddKeyword(new CalDeactivated(step.GetType().ToString()));
                // Boundary conditions title
                title = new CalTitle("Boundary conditions", "");
                calStep.AddKeyword(title);
                // Boundary conditions op
                if (step.Active && !(step is ModalDynamicsStep))
                {
                    title.AddKeyword(new CalOpParameter(OpKeywordEnum.Boundary, OpTypeEnum.New));
                }
                // Boundary conditions
                foreach (var bcEntry in step.BoundaryConditions)
                {
                    if (step.Active && bcEntry.Value.Active)
                        AppendBoundaryCondition(model, step, bcEntry.Value, referencePointsNodeIds, title);
                    else title.AddKeyword(new CalDeactivated(bcEntry.Value.Name));
                }
                // Additional boundary conditions
                AppendAdditionalBoundaryConditions(model, step, additionalBoundaryConditions, referencePointsNodeIds, title);
                // Loads title
                title = new CalTitle("Loads", "");
                calStep.AddKeyword(title);
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
                    if (step.Active && loadEntry.Value.Active) AppendLoad(model, step, loadEntry.Value, referencePointsNodeIds,
                                                                          title);
                    else title.AddKeyword(new CalDeactivated(loadEntry.Value.Name));
                }
                // Defined fields title
                title = new CalTitle("Defined fields", "");
                calStep.AddKeyword(title);
                // Defined fields op
                if (step.Active && step.DefinedFields.Count > 0)
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
                calStep.AddKeyword(title);
                //
                foreach (var historyOutputEntry in step.HistoryOutputs)
                {
                    if (step.Active && historyOutputEntry.Value.Active) AppendHistoryOutput(model, historyOutputEntry.Value,
                                                                                            title);
                    else title.AddKeyword(new CalDeactivated(historyOutputEntry.Value.Name));
                }
                // Field outputs
                title = new CalTitle("Field outputs", "");
                calStep.AddKeyword(title);
                //
                foreach (var fieldOutputEntry in step.FieldOutputs)
                {
                    if (step.Active && fieldOutputEntry.Value.Active) AppendFieldOutput(model, fieldOutputEntry.Value, title);
                    else title.AddKeyword(new CalDeactivated(fieldOutputEntry.Value.Name));
                }
                //
                title = new CalTitle("End step", "");
                calStep.AddKeyword(title);
                if (step.Active)
                {
                    CalEndStep endStep = new CalEndStep();
                    title.AddKeyword(endStep);
                }
                else title.AddKeyword(new CalDeactivated(step.Name));
            }
        }
        static private void AppendBoundaryCondition(FeModel model, Step step, BoundaryCondition boundaryCondition,
                                                    Dictionary<string, int[]> referencePointsNodeIds, CalculixKeyword parent)
        {
            ComplexLoadTypeEnum complexLoadType;
            if (boundaryCondition.Complex) complexLoadType = ComplexLoadTypeEnum.Real;
            else complexLoadType = ComplexLoadTypeEnum.None;
            //
            AppendBoundaryCondition(model, boundaryCondition, referencePointsNodeIds, complexLoadType, parent);
            // Load case=2 - Imaginary component
            if (step is SteadyStateDynamicsStep && boundaryCondition.Complex && boundaryCondition.PhaseDeg.Value != 0)
                AppendBoundaryCondition(model, boundaryCondition, referencePointsNodeIds, ComplexLoadTypeEnum.Imaginary, parent);
        }
        static private void AppendBoundaryCondition(FeModel model, BoundaryCondition boundaryCondition,
                                                    Dictionary<string, int[]> referencePointsNodeIds,
                                                    ComplexLoadTypeEnum complexLoadType, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                if (boundaryCondition is FixedBC fix)
                {
                    string nodeSetNameOfSurface = null;
                    if (fix.RegionType == RegionTypeEnum.SurfaceName)
                        nodeSetNameOfSurface = model.Mesh.Surfaces[fix.RegionName].NodeSetName;
                    CalFixedBC calFixedBC = new CalFixedBC(fix, referencePointsNodeIds, nodeSetNameOfSurface);
                    parent.AddKeyword(calFixedBC);
                }
                else if (boundaryCondition is DisplacementRotation dispRot)
                {
                    string nodeSetNameOfSurface = null;
                    if (dispRot.RegionType == RegionTypeEnum.SurfaceName)
                        nodeSetNameOfSurface = model.Mesh.Surfaces[dispRot.RegionName].NodeSetName;
                    CalDisplacementRotation calDisplacementRotation =
                        new CalDisplacementRotation(dispRot, referencePointsNodeIds, nodeSetNameOfSurface, complexLoadType);
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
                    CalTemperatureBC calTemperatureBC = new CalTemperatureBC(tmp, surfaceNodeSetName);
                    parent.AddKeyword(calTemperatureBC);
                }
                else throw new NotImplementedException();
            }
        }
        static private void AppendAdditionalBoundaryConditions(FeModel model, Step step, 
                                                               List<BoundaryCondition> additionalBoundaryConditions,
                                                               Dictionary<string, int[]> referencePointsNodeIds,
                                                               CalculixKeyword parent)
        {
            CalculixUserKeyword tmp = new CalculixUserKeyword("");
            //
            foreach (var additionalBoundaryCondition in additionalBoundaryConditions)
            {
                if (step.Active && additionalBoundaryCondition.Active)
                    AppendBoundaryCondition(model, step, additionalBoundaryCondition, referencePointsNodeIds, tmp);
                else tmp.AddKeyword(new CalDeactivated(additionalBoundaryCondition.Name));
            }
            //
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in tmp.Keywords)
            {
                sb.Append(keyword.GetKeywordString());
                sb.Append(keyword.GetDataString());
            }
            //
            CalAdditional calAdditionalBoundaryConditions = new CalAdditional("Additional boundary conditions", sb.ToString());
            if (calAdditionalBoundaryConditions.GetDataString().Length > 0) parent.AddKeyword(calAdditionalBoundaryConditions);
        }
        static private void AppendLoad(FeModel model, Step step, Load load, Dictionary<string, int[]> referencePointsNodeIds,
                                       CalculixKeyword parent)
        {
            ComplexLoadTypeEnum complexLoadType;
            if (load.Complex) complexLoadType = ComplexLoadTypeEnum.Real;
            else complexLoadType = ComplexLoadTypeEnum.None;
            //
            AppendLoad(model, load, referencePointsNodeIds, complexLoadType, parent);
            // Load case=2 - Imaginary component
            if (step is SteadyStateDynamicsStep && load.Complex && load.PhaseDeg.Value != 0)
                AppendLoad(model, load, referencePointsNodeIds, ComplexLoadTypeEnum.Imaginary, parent);
        }
        static private void AppendLoad(FeModel model, Load load, Dictionary<string, int[]> referencePointsNodeIds,
                                       ComplexLoadTypeEnum complexLoadType, CalculixKeyword parent)
        {
            if (model.Mesh != null)
            {
                if (load is CLoad cl)
                {
                    CalCLoad cLoad = new CalCLoad(cl, referencePointsNodeIds, complexLoadType);
                    parent.AddKeyword(cLoad);
                }
                else if (load is MomentLoad ml)
                {
                    CalMomentLoad mLoad = new CalMomentLoad(ml, referencePointsNodeIds, complexLoadType);
                    parent.AddKeyword(mLoad);
                }
                else if (load is DLoad dl)
                {
                    CalDLoad dLoad = new CalDLoad(dl, model.Mesh.Surfaces[dl.SurfaceName], complexLoadType);
                    parent.AddKeyword(dLoad);
                }
                else if (load is HydrostaticPressure hpl)
                {
                    CalHydrostaticPressureLoad hpLoad = new CalHydrostaticPressureLoad(model, hpl, complexLoadType);
                    parent.AddKeyword(hpLoad);
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
                        int nodeId = referencePointsNodeIds[name][0];
                        CLoad cLoad = new CLoad(ptl.Name, nodeId, ptl.GetMagnitudeValue(), 0, 0, ptl.TwoD,
                                                ptl.Complex, ptl.PhaseDeg.Value);
                        cLoad.AmplitudeName = ptl.AmplitudeName;
                        calKey = new CalCLoad(cLoad, referencePointsNodeIds, complexLoadType);
                    }
                    else if (ptl.Type == PreTensionLoadType.Displacement)
                    {
                        DisplacementRotation dr = new DisplacementRotation(ptl.Name, name, RegionTypeEnum.ReferencePointName,
                                                                           ptl.TwoD, ptl.Complex, ptl.PhaseDeg.Value);
                        dr.U1.SetEquationFromValue(ptl.GetMagnitudeValue());
                        dr.AmplitudeName = ptl.AmplitudeName;
                        calKey = new CalDisplacementRotation(dr, referencePointsNodeIds, null, complexLoadType);
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
        static private void AppendHistoryOutput(FeModel model, HistoryOutput historyOutput, CalculixKeyword parent)
        {
            if (historyOutput is NodalHistoryOutput nho)
            {
                CalNodePrint nodePrint;
                if (nho.RegionType == RegionTypeEnum.ReferencePointName)
                {
                    FeReferencePoint rp = model.Mesh.ReferencePoints[nho.RegionName];
                    nodePrint = new CalNodePrint(model, nho, rp.RefNodeSetName);
                    parent.AddKeyword(nodePrint);
                    nodePrint = new CalNodePrint(model, nho, rp.RotNodeSetName);
                    parent.AddKeyword(nodePrint);
                }
                else
                {
                    nodePrint = new CalNodePrint(model, nho);
                    parent.AddKeyword(nodePrint);
                }
            }
            else if (historyOutput is ElementHistoryOutput eho)
            {
                CalElPrint elPrint = new CalElPrint(eho);
                parent.AddKeyword(elPrint);
            }
            else if (historyOutput is ContactHistoryOutput cho)
            {
                ContactPair cp = model.ContactPairs[cho.RegionName];
                CalContactPrint contactPrint = new CalContactPrint(cho, cp.MasterRegionName, cp.SlaveRegionName);
                parent.AddKeyword(contactPrint);
            }
            else throw new NotImplementedException();
        }
        static private void AppendFieldOutput(FeModel model, FieldOutput fieldOutput, CalculixKeyword parent)
        {
            if (fieldOutput is NodalFieldOutput nfo)
            {
                CalNodeFile nodeFile = new CalNodeFile(nfo);
                parent.AddKeyword(nodeFile);
            }
            else if (fieldOutput is ElementFieldOutput efo)
            {
                CalElFile elFile = new CalElFile(efo);
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
