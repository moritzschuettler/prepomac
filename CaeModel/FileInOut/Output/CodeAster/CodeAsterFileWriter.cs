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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace FileInOut.Output
{
    /// <summary>
    /// Writes a model for Code_Aster: the mesh as a gmsh file (.msh), the command file (.comm), the study file of run_aster
    /// (.export) and the data the results are written with (_ppm.json). Shells are DKT plates and shells, whose composite
    /// sections are composites of Code_Aster with the material axes of PrePoMax (the reference axis projected onto every
    /// element, the rotation of the orientation added to the plies); shells of second order are meshed on their corner
    /// nodes, and a concentrated force on a midside node goes half to each corner node of its edge. Solids of first order
    /// and tetrahedra of second order are kept as they are. Every active static step is a linear static analysis of its
    /// own boundary conditions and loads, whose results the command file writes into a CalculiX results file (.frd).
    /// </summary>
    public static class CodeAsterFileWriter
    {
        // Variables
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;


        // Classes
        private class ElementGroup
        {
            public string Name;
            public Section Section;
            public double[] Vector;                 // the reference axis projected onto the shells
            public double RotationDeg;              // the rotation of the material orientation
            public List<int> ElementIds = new List<int>();
        }
        private class ExportData
        {
            public FeModel Model;
            public Dictionary<int, int> NodeIndex = new Dictionary<int, int>();       // node id -> msh node number
            public List<int> NodeIds = new List<int>();
            public HashSet<int> ShellNodes = new HashSet<int>();
            public Dictionary<int, int[]> MidsideCorners = new Dictionary<int, int[]>();   // dropped midside node -> corners
            public List<ElementGroup> Groups = new List<ElementGroup>();
            public List<Tuple<string, int[]>> NodeGroups = new List<Tuple<string, int[]>>();
            public List<string> Warnings = new List<string>();
        }


        // Methods
        /// <summary>
        /// Writes the files of the study beside the export file and returns the warnings. containerDirectory is the folder
        /// the work directory is seen as by run_aster (the folder mounted into the Docker container).
        /// </summary>
        public static string[] Write(string exportFileName, FeModel model, string containerDirectory)
        {
            FeModel copy = model.DeepCopy();
            copy.Mesh.RemoveParts(copy.Mesh.GetInactivePartNames(), out _, false);
            if (copy.Properties.ModelSpace.IsTwoD())
                throw new CaeException("Code_Aster supports only 3D models in PrePoMax.");
            //
            string directory = Path.GetDirectoryName(exportFileName);
            string name = Path.GetFileNameWithoutExtension(exportFileName);
            ExportData data = new ExportData { Model = copy };
            List<Step> steps = copy.StepCollection.StepsList.Where(s => s.Active && s.GetType() == typeof(StaticStep))
                                                              .ToList();
            foreach (Step step in copy.StepCollection.StepsList)
            {
                if (step.Active && !(step is InitialStep) && step.GetType() != typeof(StaticStep))
                    data.Warnings.Add("The step " + step.Name + " is not a static step and is not solved by Code_Aster.");
            }
            if (steps.Count == 0) throw new CaeException("Code_Aster needs at least one active static step.");
            //
            CollectGroups(data);
            StringBuilder comm = new StringBuilder();
            WriteCommands(comm, data, steps, containerDirectory, name);
            WriteMesh(Path.Combine(directory, name + ".msh"), data);
            File.WriteAllText(Path.Combine(directory, name + ".comm"), comm.ToString());
            WriteResultData(Path.Combine(directory, name + "_ppm.json"), data);
            WriteExport(exportFileName, containerDirectory, name);
            return data.Warnings.ToArray();
        }

        // Mesh and groups
        private static bool IsShell(FeElement element)
        {
            return element is FeElement2D;
        }
        private static int[] GetCorners(FeElement element)
        {
            if (element is LinearTriangleElement || element is ParabolicTriangleElement) return element.NodeIds.Take(3).ToArray();
            if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
                return element.NodeIds.Take(4).ToArray();
            if (element is ParabolicHexaElement) return element.NodeIds.Take(8).ToArray();
            if (element is ParabolicWedgeElement) return element.NodeIds.Take(6).ToArray();
            return element.NodeIds;
        }
        // gmsh element type and the node order of gmsh
        private static void GetMshElement(FeElement element, out int type, out int[] nodes)
        {
            int[] n = element.NodeIds;
            if (element is LinearTriangleElement || element is ParabolicTriangleElement) { type = 2; nodes = GetCorners(element); }
            else if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement)
            { type = 3; nodes = GetCorners(element); }
            else if (element is LinearTetraElement) { type = 4; nodes = n; }
            // gmsh numbers the last two edges of a tetrahedron of second order the other way round
            else if (element is ParabolicTetraElement) { type = 11; nodes = new[] { n[0], n[1], n[2], n[3], n[4], n[5], n[6], n[7], n[9], n[8] }; }
            else if (element is LinearHexaElement || element is ParabolicHexaElement) { type = 5; nodes = GetCorners(element); }
            else if (element is LinearWedgeElement || element is ParabolicWedgeElement) { type = 6; nodes = GetCorners(element); }
            else throw new CaeException("The element type " + element.GetType().Name + " is not supported by Code_Aster in " +
                                        "PrePoMax.");
        }
        // CalculiX results file element type of what Code_Aster meshes
        private static int GetFrdType(FeElement element)
        {
            if (element is LinearTriangleElement || element is ParabolicTriangleElement) return 7;
            if (element is LinearQuadrilateralElement || element is ParabolicQuadrilateralElement) return 9;
            if (element is LinearTetraElement) return 3;
            if (element is ParabolicTetraElement) return 6;
            if (element is LinearHexaElement || element is ParabolicHexaElement) return 1;
            return 2;
        }
        private static int[] GetFrdNodes(FeElement element)
        {
            return element is ParabolicTetraElement ? element.NodeIds : GetCorners(element);
        }
        private static void CollectGroups(ExportData data)
        {
            FeModel model = data.Model;
            Dictionary<int, int> elementIdSectionId;
            List<Section> sections;
            model.GetSectionAssignments(out elementIdSectionId, out sections);
            Dictionary<int, MaterialOrientation> orientations = model.GetElementMaterialOrientations();
            Dictionary<string, ElementGroup> groups = new Dictionary<string, ElementGroup>();
            List<int> missing = new List<int>();
            bool secondOrderSolids = false;
            //
            foreach (var entry in model.Mesh.Elements.OrderBy(e => e.Key))
            {
                FeElement element = entry.Value;
                if (element is FeElement1D)
                    throw new CaeException("Beams, springs and gaps are not supported by Code_Aster in PrePoMax.");
                if (!elementIdSectionId.TryGetValue(entry.Key, out int sectionId) || sectionId < 0)
                {
                    missing.Add(entry.Key);
                    continue;
                }
                Section section = sections[sectionId];
                if (section is MembraneSection)
                    throw new CaeException("Membrane sections are not supported by Code_Aster in PrePoMax.");
                if (element is ParabolicHexaElement || element is ParabolicWedgeElement) secondOrderSolids = true;
                //
                double[] vector = null;
                double rotation = 0;
                string key = section.Name;
                if (IsShell(element))
                {
                    if (!(section is ShellSection))
                        throw new CaeException("The shell element " + entry.Key + " has the section " + section.Name +
                                               ", which is not a shell section.");
                    vector = GetReferenceVector(model, element, orientations, out rotation, out string orientationKey);
                    key += "|" + orientationKey;
                }
                if (!groups.TryGetValue(key, out ElementGroup group))
                {
                    group = new ElementGroup { Name = "G" + (groups.Count + 1), Section = section, Vector = vector,
                                               RotationDeg = rotation };
                    groups.Add(key, group);
                }
                group.ElementIds.Add(entry.Key);
            }
            if (missing.Count > 0) data.Warnings.Add(missing.Count + " elements have no section and are not solved.");
            if (secondOrderSolids)
                data.Warnings.Add("Hexahedra and wedges of second order are solved by Code_Aster on their corner nodes.");
            data.Groups = groups.Values.ToList();
            // The nodes the groups use, in the order of their ids
            SortedSet<int> used = new SortedSet<int>();
            foreach (ElementGroup group in data.Groups)
            {
                foreach (int id in group.ElementIds)
                {
                    FeElement element = model.Mesh.Elements[id];
                    GetMshElement(element, out _, out int[] nodes);
                    used.UnionWith(nodes);
                    if (IsShell(element))
                    {
                        data.ShellNodes.UnionWith(nodes);
                        CollectMidsideNodes(data, element);
                    }
                }
            }
            data.NodeIds = used.ToList();
            for (int i = 0; i < data.NodeIds.Count; i++) data.NodeIndex[data.NodeIds[i]] = i + 1;
        }
        // The midside nodes of a shell of second order, which Code_Aster meshes on its corners, with the corners of their edge
        private static void CollectMidsideNodes(ExportData data, FeElement element)
        {
            int[] n = element.NodeIds;
            if (element is ParabolicTriangleElement)
            {
                data.MidsideCorners[n[3]] = new[] { n[0], n[1] };
                data.MidsideCorners[n[4]] = new[] { n[1], n[2] };
                data.MidsideCorners[n[5]] = new[] { n[2], n[0] };
            }
            else if (element is ParabolicQuadrilateralElement)
            {
                data.MidsideCorners[n[4]] = new[] { n[0], n[1] };
                data.MidsideCorners[n[5]] = new[] { n[1], n[2] };
                data.MidsideCorners[n[6]] = new[] { n[2], n[3] };
                data.MidsideCorners[n[7]] = new[] { n[3], n[0] };
            }
        }
        // The axis Code_Aster projects onto a shell as its material axis 1, as PrePoMax projects the reference axis of the
        // material orientation, and the rotation of the orientation; without an orientation the global x axis
        private static double[] GetReferenceVector(FeModel model, FeElement element,
                                                   Dictionary<int, MaterialOrientation> orientations, out double rotation,
                                                   out string key)
        {
            rotation = 0;
            if (!orientations.TryGetValue(element.Id, out MaterialOrientation orientation))
            {
                key = "global";
                return new double[] { 1, 0, 0 };
            }
            rotation = orientation.RotationAngle.Value;
            CoordinateSystem coordinateSystem = null;
            string coordinateSystemName = orientation.CoordinateSystemName;
            if (coordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName)
                model.Mesh.CoordinateSystems.TryGetValue(coordinateSystemName, out coordinateSystem);
            if (coordinateSystem == null || coordinateSystem.Type == CoordinateSystemTypeEnum.Rectangular)
            {
                double[] axis;
                if (coordinateSystem == null)
                {
                    axis = orientation.ReferenceAxis == MaterialOrientationAxisEnum.X ? new double[] { 1, 0, 0 } :
                           orientation.ReferenceAxis == MaterialOrientationAxisEnum.Y ? new double[] { 0, 1, 0 } :
                           new double[] { 0, 0, 1 };
                }
                else
                {
                    Vec3D v = orientation.ReferenceAxis == MaterialOrientationAxisEnum.X ? coordinateSystem.DirectionX() :
                              orientation.ReferenceAxis == MaterialOrientationAxisEnum.Y ? coordinateSystem.DirectionY() :
                              coordinateSystem.DirectionZ();
                    axis = v.Coor;
                }
                key = orientation.Name;
                return axis;
            }
            // A cylindrical or spherical system turns from element to element: the material axis of the element itself,
            // to a tenth of a degree
            double[][] axes = orientation.GetElementAxes(model.Mesh, element, 0, out _);
            rotation = 0;
            double[] a = axes[0];
            key = orientation.Name + string.Join(",", a.Select(c => Math.Round(c, 3).ToString(Invariant)));
            return a;
        }
        private static void WriteMesh(string fileName, ExportData data)
        {
            FeModel model = data.Model;
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("$MeshFormat");
            sb.AppendLine("2.2 0 8");
            sb.AppendLine("$EndMeshFormat");
            int numOfGroups = data.Groups.Count + data.NodeGroups.Count;
            sb.AppendLine("$PhysicalNames");
            sb.AppendLine(numOfGroups.ToString());
            int tag = 1;
            Dictionary<ElementGroup, int> groupTags = new Dictionary<ElementGroup, int>();
            foreach (ElementGroup group in data.Groups)
            {
                int dimension = IsShell(model.Mesh.Elements[group.ElementIds[0]]) ? 2 : 3;
                groupTags[group] = tag;
                sb.AppendLine(dimension + " " + tag++ + " \"" + group.Name + "\"");
            }
            List<int> nodeGroupTags = new List<int>();
            foreach (var nodeGroup in data.NodeGroups)
            {
                nodeGroupTags.Add(tag);
                sb.AppendLine("0 " + tag++ + " \"" + nodeGroup.Item1 + "\"");
            }
            sb.AppendLine("$EndPhysicalNames");
            sb.AppendLine("$Nodes");
            sb.AppendLine(data.NodeIds.Count.ToString());
            foreach (int id in data.NodeIds)
            {
                FeNode node = model.Mesh.Nodes[id];
                sb.AppendLine(data.NodeIndex[id] + " " + R(node.X) + " " + R(node.Y) + " " + R(node.Z));
            }
            sb.AppendLine("$EndNodes");
            StringBuilder elements = new StringBuilder();
            int count = 0;
            foreach (ElementGroup group in data.Groups)
            {
                foreach (int id in group.ElementIds)
                {
                    GetMshElement(model.Mesh.Elements[id], out int type, out int[] nodes);
                    elements.AppendLine(++count + " " + type + " 2 " + groupTags[group] + " " + groupTags[group] + " " +
                                        string.Join(" ", nodes.Select(n => data.NodeIndex[n])));
                }
            }
            for (int i = 0; i < data.NodeGroups.Count; i++)
            {
                foreach (int nodeId in data.NodeGroups[i].Item2)
                    elements.AppendLine(++count + " 15 2 " + nodeGroupTags[i] + " " + nodeGroupTags[i] + " " +
                                        data.NodeIndex[nodeId]);
            }
            sb.AppendLine("$Elements");
            sb.AppendLine(count.ToString());
            sb.Append(elements);
            sb.AppendLine("$EndElements");
            File.WriteAllText(fileName, sb.ToString());
        }

        // Commands
        private static void WriteCommands(StringBuilder sb, ExportData data, List<Step> steps, string containerDirectory,
                                          string name)
        {
            FeModel model = data.Model;
            sb.AppendLine("# Code_Aster command file written by PrePoMax");
            sb.AppendLine("DEBUT(LANG='EN')");
            // The node groups are made while the steps are written, and the mesh is written after them
            StringBuilder body = new StringBuilder();
            List<ElementGroup> shells = data.Groups.Where(g => IsShell(model.Mesh.Elements[g.ElementIds[0]])).ToList();
            List<ElementGroup> solids = data.Groups.Except(shells).ToList();
            body.AppendLine("model = AFFE_MODELE(MAILLAGE=mesh, AFFE=(");
            if (shells.Count > 0)
                body.AppendLine("    _F(GROUP_MA=" + Names(shells) + ", PHENOMENE='MECANIQUE', MODELISATION='DKT'),");
            if (solids.Count > 0)
                body.AppendLine("    _F(GROUP_MA=" + Names(solids) + ", PHENOMENE='MECANIQUE', MODELISATION='3D'),");
            body.AppendLine("))");
            // Materials, composites of the shells and their assignment
            Dictionary<string, string> materialNames = new Dictionary<string, string>();
            Dictionary<ElementGroup, string> groupMaterial = new Dictionary<ElementGroup, string>();
            foreach (ElementGroup group in data.Groups)
            {
                if (group.Section is CompositeShellSection composite)
                {
                    StringBuilder layers = new StringBuilder();
                    foreach (CompositeLayer layer in composite.GetSolverLayers())
                    {
                        string material = GetMaterial(body, model, layer.MaterialName, materialNames);
                        layers.Append("_F(EPAIS=" + R(layer.Thickness.Value) + ", MATER=" + material + ", ORIENTATION=" +
                                      R(layer.Angle.Value + group.RotationDeg) + "), ");
                    }
                    string compositeName = "cmp_" + group.Name;
                    body.AppendLine(compositeName + " = DEFI_COMPOSITE(COUCHE=(" + layers + "))");
                    groupMaterial[group] = compositeName;
                }
                else
                {
                    string material = GetMaterial(body, model, group.Section.MaterialName, materialNames);
                    // An orthotropic shell is a composite of one ply
                    if (group.Section is ShellSection shell && IsOrthotropic(model, group.Section.MaterialName))
                    {
                        string compositeName = "cmp_" + group.Name;
                        body.AppendLine(compositeName + " = DEFI_COMPOSITE(COUCHE=_F(EPAIS=" + R(shell.Thickness.Value) +
                                        ", MATER=" + material + ", ORIENTATION=" + R(group.RotationDeg) + "))");
                        groupMaterial[group] = compositeName;
                    }
                    else groupMaterial[group] = material;
                }
            }
            body.AppendLine("materials = AFFE_MATERIAU(MAILLAGE=mesh, AFFE=(");
            foreach (ElementGroup group in data.Groups)
                body.AppendLine("    _F(GROUP_MA='" + group.Name + "', MATER=" + groupMaterial[group] + "),");
            body.AppendLine("))");
            // Shell thicknesses and material axes
            if (shells.Count > 0)
            {
                body.AppendLine("elements = AFFE_CARA_ELEM(MODELE=model, COQUE=(");
                foreach (ElementGroup group in shells)
                {
                    ShellSection shell = (ShellSection)group.Section;
                    int numOfLayers = shell is CompositeShellSection css ? css.GetSolverLayers().Count : 1;
                    double thickness = shell is CompositeShellSection c2 ? c2.GetTotalThickness() : shell.Thickness.Value;
                    if (!double.IsNaN(shell.Offset.Value) && shell.Offset.Value != 0)
                        data.Warnings.Add("The offset of the section " + shell.Name + " is not used by Code_Aster.");
                    body.AppendLine("    _F(GROUP_MA='" + group.Name + "', EPAIS=" + R(thickness) + ", VECTEUR=(" +
                                    string.Join(", ", group.Vector.Select(R)) + "), COQUE_NCOU=" + numOfLayers + "),");
                }
                body.AppendLine("))");
            }
            // The steps, each a linear static analysis of its own conditions
            List<string> results = new List<string>();
            for (int i = 0; i < steps.Count; i++)
            {
                List<string> loads = WriteStep(body, data, steps[i], i + 1);
                if (loads.Count == 0)
                    throw new CaeException("The step " + steps[i].Name + " has no boundary condition or load Code_Aster can " +
                                           "solve.");
                string result = "res" + (i + 1);
                body.AppendLine("# Step " + steps[i].Name);
                body.AppendLine(result + " = MECA_STATIQUE(MODELE=model, CHAM_MATER=materials" +
                                (shells.Count > 0 ? ", CARA_ELEM=elements" : "") + ", EXCIT=(" +
                                string.Join(", ", loads.Select(l => "_F(CHARGE=" + l + ")")) + "))");
                results.Add(result);
            }
            // The mesh with its node groups, then the model
            sb.AppendLine("mesh = LIRE_MAILLAGE(FORMAT='GMSH', UNITE=19)");
            if (data.NodeGroups.Count > 0)
            {
                sb.AppendLine("mesh = DEFI_GROUP(reuse=mesh, MAILLAGE=mesh, CREA_GROUP_NO=(");
                foreach (var nodeGroup in data.NodeGroups) sb.AppendLine("    _F(GROUP_MA='" + nodeGroup.Item1 + "'),");
                sb.AppendLine("))");
            }
            sb.Append(body);
            // The results as a CalculiX results file
            sb.AppendLine("_ppm_results = [" + string.Join(", ", results) + "]");
            sb.AppendLine("_PPM_DATA_FILE = '" + containerDirectory + "/" + name + "_ppm.json'");
            sb.AppendLine("_PPM_FRD_FILE = '" + containerDirectory + "/" + name + ".frd'");
            sb.AppendLine(CodeAsterPostScript.Text);
            sb.AppendLine("FIN()");
        }
        private static string Names(IEnumerable<ElementGroup> groups)
        {
            return "(" + string.Join(", ", groups.Select(g => "'" + g.Name + "'")) + ",)";
        }
        private static bool IsOrthotropic(FeModel model, string materialName)
        {
            Material material = model.Materials[materialName];
            return material.GetProperty<ElasticEngineeringConstants>() != null || material.GetProperty<ElasticLamina>() != null;
        }
        private static string GetMaterial(StringBuilder sb, FeModel model, string materialName,
                                          Dictionary<string, string> materialNames)
        {
            if (materialNames.TryGetValue(materialName, out string name)) return name;
            name = "mat" + (materialNames.Count + 1);
            materialNames.Add(materialName, name);
            Material material = model.Materials[materialName];
            List<string> keywords = new List<string>();
            double density = double.NaN;
            if (material.GetProperty<Density>() is Density d) density = d.DensityTemp[0][0].Value;
            string rho = double.IsNaN(density) ? "" : ", RHO=" + R(density);
            if (material.GetProperty<Elastic>() is Elastic elastic)
            {
                EquationContainer[] row = elastic.YoungsPoissonsTemp[0];
                keywords.Add("ELAS=_F(E=" + R(row[0].Value) + ", NU=" + R(row[1].Value) + rho + ")");
            }
            else if (material.GetProperty<ElasticEngineeringConstants>() is ElasticEngineeringConstants ec)
            {
                EquationContainer[] r = ec.EngineeringConstantsTemp[0];
                keywords.Add("ELAS_ORTH=_F(E_L=" + R(r[ElasticEngineeringConstants.YoungsModulus1Id].Value) +
                             ", E_T=" + R(r[ElasticEngineeringConstants.YoungsModulus2Id].Value) +
                             ", E_N=" + R(r[ElasticEngineeringConstants.YoungsModulus3Id].Value) +
                             ", NU_LT=" + R(r[ElasticEngineeringConstants.PoissonsRatio12Id].Value) +
                             ", NU_LN=" + R(r[ElasticEngineeringConstants.PoissonsRatio13Id].Value) +
                             ", NU_TN=" + R(r[ElasticEngineeringConstants.PoissonsRatio23Id].Value) +
                             ", G_LT=" + R(r[ElasticEngineeringConstants.ShearModulus12Id].Value) +
                             ", G_LN=" + R(r[ElasticEngineeringConstants.ShearModulus13Id].Value) +
                             ", G_TN=" + R(r[ElasticEngineeringConstants.ShearModulus23Id].Value) + rho + ")");
            }
            else if (material.GetProperty<ElasticLamina>() is ElasticLamina lamina)
            {
                EquationContainer[] r = lamina.LaminaConstantsTemp[0];
                double e2 = r[ElasticLamina.YoungsModulus2Id].Value;
                double v12 = r[ElasticLamina.PoissonsRatio12Id].Value;
                // The out-of-plane constants as the lamina completes them for CalculiX: E3 = E2, v13 = v12
                keywords.Add("ELAS_ORTH=_F(E_L=" + R(r[ElasticLamina.YoungsModulus1Id].Value) + ", E_T=" + R(e2) +
                             ", E_N=" + R(e2) + ", NU_LT=" + R(v12) + ", NU_LN=" + R(v12) +
                             ", NU_TN=" + R(lamina.GetPoissonsRatio23(0)) +
                             ", G_LT=" + R(r[ElasticLamina.ShearModulus12Id].Value) +
                             ", G_LN=" + R(r[ElasticLamina.ShearModulus13Id].Value) +
                             ", G_TN=" + R(r[ElasticLamina.ShearModulus23Id].Value) + rho + ")");
            }
            else throw new CaeException("The material " + materialName + " has no elastic property Code_Aster can use: " +
                                        "isotropic, engineering constants or lamina.");
            sb.AppendLine(name + " = DEFI_MATERIAU(" + string.Join(", ", keywords) + ")   # " + materialName);
            return name;
        }

        // Steps
        private static List<string> WriteStep(StringBuilder sb, ExportData data, Step step, int stepNumber)
        {
            List<string> loads = new List<string>();
            int count = 0;
            foreach (var entry in step.BoundaryConditions)
            {
                if (!entry.Value.Active) continue;
                string load = "bc" + stepNumber + "_" + (++count);
                if (WriteBoundaryCondition(sb, data, entry.Value, load)) loads.Add(load);
            }
            foreach (var entry in step.Loads)
            {
                if (!entry.Value.Active) continue;
                string load = "ld" + stepNumber + "_" + (++count);
                if (WriteLoad(sb, data, entry.Value, load)) loads.Add(load);
            }
            return loads;
        }
        private static string AddNodeGroup(ExportData data, int[] nodeIds)
        {
            string name = "N" + (data.NodeGroups.Count + 1);
            data.NodeGroups.Add(new Tuple<string, int[]>(name, nodeIds));
            return name;
        }
        private static int[] GetRegionNodeIds(FeModel model, string regionName, RegionTypeEnum regionType)
        {
            if (regionName == null) return null;
            if (regionType == RegionTypeEnum.NodeSetName && model.Mesh.NodeSets.TryGetValue(regionName, out FeNodeSet set))
                return set.Labels;
            if (regionType == RegionTypeEnum.SurfaceName && model.Mesh.Surfaces.TryGetValue(regionName, out FeSurface surface) &&
                surface.NodeSetName != null && model.Mesh.NodeSets.TryGetValue(surface.NodeSetName, out FeNodeSet surfaceSet))
                return surfaceSet.Labels;
            if (regionType == RegionTypeEnum.PartName && model.Mesh.Parts.TryGetValue(regionName, out BasePart part))
                return part.NodeLabels;
            return null;
        }
        private static bool WriteBoundaryCondition(StringBuilder sb, ExportData data, BoundaryCondition boundaryCondition,
                                                   string load)
        {
            int[] nodeIds = GetRegionNodeIds(data.Model, boundaryCondition.RegionName, boundaryCondition.RegionType);
            if (nodeIds == null)
            {
                data.Warnings.Add("The boundary condition " + boundaryCondition.Name + " is not on nodes, a surface or a " +
                                  "part and is not solved by Code_Aster.");
                return false;
            }
            // Only the nodes Code_Aster meshes; the midside nodes of shells move with their corners
            int[] shellNodes = nodeIds.Where(n => data.NodeIndex.ContainsKey(n) && data.ShellNodes.Contains(n)).ToArray();
            int[] solidNodes = nodeIds.Where(n => data.NodeIndex.ContainsKey(n) && !data.ShellNodes.Contains(n)).ToArray();
            string[] translations = new string[3];
            string[] rotations = new string[3];
            if (boundaryCondition is FixedBC)
            {
                translations = new[] { "0.0", "0.0", "0.0" };
                rotations = new[] { "0.0", "0.0", "0.0" };
            }
            else if (boundaryCondition is DisplacementRotation dr)
            {
                double[] values = { dr.U1.Value, dr.U2.Value, dr.U3.Value, dr.UR1.Value, dr.UR2.Value, dr.UR3.Value };
                for (int dof = 1; dof <= 6; dof++)
                {
                    DOFType type = dr.GetDofType(dof);
                    string value = type == DOFType.Free ? null : type == DOFType.Prescribed ? R(values[dof - 1]) : "0.0";
                    if (dof <= 3) translations[dof - 1] = value;
                    else rotations[dof - 4] = value;
                }
                if (dr.CoordinateSystemName != null && dr.CoordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName)
                    data.Warnings.Add("The boundary condition " + dr.Name + " is applied in global axes by Code_Aster.");
            }
            else
            {
                data.Warnings.Add("The boundary condition " + boundaryCondition.Name + " is not supported by Code_Aster.");
                return false;
            }
            string[] translationNames = { "DX", "DY", "DZ" };
            string[] rotationNames = { "DRX", "DRY", "DRZ" };
            List<string> entries = new List<string>();
            if (shellNodes.Length > 0)
            {
                string group = AddNodeGroup(data, shellNodes);
                string values = DofValues(translationNames, translations) + DofValues(rotationNames, rotations);
                if (values.Length > 0) entries.Add("_F(GROUP_NO='" + group + "'" + values + ")");
            }
            if (solidNodes.Length > 0)
            {
                string group = AddNodeGroup(data, solidNodes);
                string values = DofValues(translationNames, translations);
                if (values.Length > 0) entries.Add("_F(GROUP_NO='" + group + "'" + values + ")");
            }
            if (entries.Count == 0) return false;
            sb.AppendLine("# Boundary condition " + boundaryCondition.Name);
            // Eliminated, not dualized: a node of two boundary conditions, as the corner of two held edges, may be held
            // twice, which Lagrange multipliers refuse
            sb.AppendLine(load + " = AFFE_CHAR_CINE(MODELE=model, MECA_IMPO=(" + string.Join(", ", entries) + ",))");
            return true;
        }
        private static string DofValues(string[] names, string[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < names.Length; i++) if (values[i] != null) sb.Append(", " + names[i] + "=" + values[i]);
            return sb.ToString();
        }
        // The force of every node of a region, the force of a dropped midside node shared by the corners of its edge
        private static Dictionary<int, double> GetNodeFactors(ExportData data, int[] nodeIds)
        {
            Dictionary<int, double> factors = new Dictionary<int, double>();
            foreach (int id in nodeIds)
            {
                if (data.NodeIndex.ContainsKey(id)) Add(factors, id, 1);
                else if (data.MidsideCorners.TryGetValue(id, out int[] corners))
                {
                    Add(factors, corners[0], 0.5);
                    Add(factors, corners[1], 0.5);
                }
            }
            return factors;
        }
        private static void Add(Dictionary<int, double> factors, int id, double value)
        {
            factors.TryGetValue(id, out double old);
            factors[id] = old + value;
        }
        private static bool WriteLoad(StringBuilder sb, ExportData data, Load load, string name)
        {
            if (load is CLoad || load is MomentLoad)
            {
                int[] nodeIds = GetRegionNodeIds(data.Model, load.RegionName, load.RegionType);
                if (nodeIds == null && load is CLoad cl && load.RegionType == RegionTypeEnum.NodeId) nodeIds = new[] { cl.NodeId };
                if (nodeIds == null || nodeIds.Length == 0)
                {
                    data.Warnings.Add("The load " + load.Name + " is not on nodes and is not solved by Code_Aster.");
                    return false;
                }
                double[] components;
                string[] keys;
                if (load is CLoad cLoad)
                {
                    components = new[] { cLoad.F1.Value, cLoad.F2.Value, cLoad.F3.Value };
                    keys = new[] { "FX", "FY", "FZ" };
                }
                else
                {
                    MomentLoad moment = (MomentLoad)load;
                    components = new[] { moment.M1.Value, moment.M2.Value, moment.M3.Value };
                    keys = new[] { "MX", "MY", "MZ" };
                }
                // Nodes of the same share of the force are one group
                List<string> entries = new List<string>();
                foreach (var share in GetNodeFactors(data, nodeIds).GroupBy(f => Math.Round(f.Value, 6)))
                {
                    string group = AddNodeGroup(data, share.Select(f => f.Key).ToArray());
                    string values = string.Join("", keys.Select((k, i) => ", " + k + "=" + R(components[i] * share.Key)));
                    entries.Add("_F(GROUP_NO='" + group + "'" + values + ")");
                }
                sb.AppendLine("# Load " + load.Name);
                sb.AppendLine(name + " = AFFE_CHAR_MECA(MODELE=model, FORCE_NODALE=(" + string.Join(", ", entries) + ",))");
                return true;
            }
            if (load is GravityLoad gravity)
            {
                double[] g = { gravity.F1.Value, gravity.F2.Value, gravity.F3.Value };
                double magnitude = Math.Sqrt(g[0] * g[0] + g[1] * g[1] + g[2] * g[2]);
                if (magnitude == 0) return false;
                sb.AppendLine("# Load " + load.Name);
                sb.AppendLine(name + " = AFFE_CHAR_MECA(MODELE=model, PESANTEUR=_F(GRAVITE=" + R(magnitude) + ", DIRECTION=(" +
                              string.Join(", ", g.Select(v => R(v / magnitude))) + ")))");
                return true;
            }
            data.Warnings.Add("The load " + load.Name + " (" + load.GetType().Name + ") is not supported by Code_Aster in " +
                              "PrePoMax.");
            return false;
        }

        // Files of the study
        private static void WriteResultData(string fileName, ExportData data)
        {
            FeModel model = data.Model;
            StringBuilder sb = new StringBuilder();
            // The results reader takes the unit system from the end of a heading line, as CalculiX writes it
            sb.Append("{\"heading\": \"" + "Code_Aster, Unit system: " + model.UnitSystem.UnitSystemType + "\",\n");
            sb.Append("\"nodes\": [" + string.Join(",", data.NodeIds) + "],\n");
            sb.Append("\"elements\": [");
            bool first = true;
            foreach (ElementGroup group in data.Groups)
            {
                foreach (int id in group.ElementIds)
                {
                    FeElement element = model.Mesh.Elements[id];
                    if (!first) sb.Append(",");
                    first = false;
                    sb.Append("[" + id + "," + GetFrdType(element) + ",[" + string.Join(",", GetFrdNodes(element)) + "]]");
                }
            }
            sb.Append("],\n");
            List<ElementGroup> shells = data.Groups.Where(g => IsShell(model.Mesh.Elements[g.ElementIds[0]])).ToList();
            sb.Append("\"solid_groups\": [" + string.Join(",", data.Groups.Except(shells).Select(g => "\"" + g.Name + "\"")) +
                      "],\n");
            sb.Append("\"shell_groups\": [" +
                      string.Join(",", shells.Where(g => !(g.Section is CompositeShellSection) &&
                                                         !IsOrthotropic(model, g.Section.MaterialName))
                                             .Select(g => "\"" + g.Name + "\"")) + "],\n");
            sb.Append("\"composite_groups\": [");
            first = true;
            foreach (ElementGroup group in shells)
            {
                List<Tuple<string, double>> layers = new List<Tuple<string, double>>();
                if (group.Section is CompositeShellSection css)
                    layers.AddRange(css.GetSolverLayers().Select(l => new Tuple<string, double>(l.MaterialName, l.Angle.Value)));
                else if (IsOrthotropic(model, group.Section.MaterialName))
                    layers.Add(new Tuple<string, double>(group.Section.MaterialName, 0));
                else continue;
                if (!first) sb.Append(",");
                first = false;
                sb.Append("{\"group\": \"" + group.Name + "\", \"layers\": [");
                sb.Append(string.Join(",", layers.Select(l => "{\"angle\": " + R(l.Item2) + ", \"strength\": " +
                                                              GetStrength(model, l.Item1) + "}")));
                sb.Append("]}");
            }
            sb.Append("]}\n");
            File.WriteAllText(fileName, sb.ToString());
        }
        private static string GetStrength(FeModel model, string materialName)
        {
            if (!(model.Materials[materialName].GetProperty<CompositeStrength>() is CompositeStrength s)) return "null";
            return "[" + R(s.TensileStrength1.Value) + "," + R(s.CompressiveStrength1.Value) + "," +
                   R(s.TensileStrength2.Value) + "," + R(s.CompressiveStrength2.Value) + "," + R(s.ShearStrength12.Value) +
                   "," + R(s.TsaiWuCoefficient.Value) + "]";
        }
        private static void WriteExport(string fileName, string containerDirectory, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("P actions make_etude");
            sb.AppendLine("P version stable");
            sb.AppendLine("P ncpus " + Math.Max(1, Environment.ProcessorCount / 2));
            sb.AppendLine("P mpi_nbcpu 1");
            sb.AppendLine("P memory_limit 4096");
            sb.AppendLine("P time_limit 86400");
            sb.AppendLine("F comm " + containerDirectory + "/" + name + ".comm D 1");
            sb.AppendLine("F msh " + containerDirectory + "/" + name + ".msh D 19");
            sb.AppendLine("F mess " + containerDirectory + "/" + name + ".mess R 6");
            File.WriteAllText(fileName, sb.ToString());
        }
        private static string R(double value)
        {
            return value.ToString("R", Invariant);
        }
    }
}
