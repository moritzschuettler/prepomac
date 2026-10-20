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
    [Serializable]
    public static class OpenRadiossFileWriter
    {
        // OpenRadioss block format: 10 character integer fields, 20 character double fields, 100 character lines
        private const int RampFunctionId = 1;


        // Methods
        static public void Write(string fileName, FeModel model)
        {
            // Copy model
            FeModel copy = model.DeepCopy();
            string[] inactivePartNames = copy.Mesh.GetInactivePartNames();
            copy.Mesh.RemoveParts(inactivePartNames, out _, false);
            //
            if (copy.Properties.ModelSpace.IsTwoD())
                throw new CaeException("The OpenRadioss export supports only 3D models.");
            //
            string workDirectory = Path.GetDirectoryName(fileName);
            string rootName = Path.GetFileNameWithoutExtension(fileName);
            //
            List<Step> steps = GetSupportedSteps(copy);
            if (steps.Count == 0) throw new CaeException("The OpenRadioss export needs at least one active " +
                                                         "static or dynamic step.");
            //
            WriteStarterFile(Path.Combine(workDirectory, rootName + "_0000.rad"), rootName, copy, steps);
            //
            double startTime = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                string engineFileName = Path.Combine(workDirectory, rootName + "_" + (i + 1).ToString().PadLeft(4, '0') + ".rad");
                bool previousExplicit = i > 0 && IsExplicit(steps[i - 1]);
                WriteEngineFile(engineFileName, rootName, steps[i], i + 1, startTime, previousExplicit);
                startTime += GetTimePeriod(steps[i]);
            }
        }
        static private List<Step> GetSupportedSteps(FeModel model)
        {
            List<Step> steps = new List<Step>();
            foreach (Step step in model.StepCollection.StepsList)
            {
                if (!step.Active) continue;
                if (step is InitialStep || step is BoundaryDisplacementStep) continue;
                if (step is StaticStep) steps.Add(step);    // also covers DynamicStep : StaticStep
            }
            return steps;
        }
        static private bool IsExplicit(Step step)
        {
            if (step is DynamicStep dynamicStep)
                return dynamicStep.SolutionProcedure == SolutionProcedureEnum.ExplicitExplicit ||
                       dynamicStep.SolutionProcedure == SolutionProcedureEnum.ExplicitImplicit;
            return false;
        }
        static private double GetTimePeriod(Step step)
        {
            if (step is StaticStep staticStep) return staticStep.TimePeriod;
            return 1;
        }

        // Starter file
        static private void WriteStarterFile(string fileName, string rootName, FeModel model, List<Step> steps)
        {
            StringBuilder sb = new StringBuilder();
            List<string> warnings = new List<string>();
            //
            AppendHeader(sb, rootName, model);
            //
            // Materials
            Dictionary<string, int> materialIds = new Dictionary<string, int>();
            int materialId = 0;
            foreach (var entry in model.Materials)
            {
                materialId++;
                materialIds.Add(entry.Key, materialId);
                AppendMaterial(sb, entry.Value, materialId, warnings);
            }
            // Sections: one property and one part per section
            Dictionary<int, List<int>> partElementIds = new Dictionary<int, List<int>>();
            Dictionary<int, string> partNames = new Dictionary<int, string>();
            HashSet<int> assignedElementIds = new HashSet<int>();
            int partId = 0;
            foreach (var entry in model.Sections)
            {
                Section section = entry.Value;
                if (!(section is SolidSection || section is ShellSection))
                {
                    warnings.Add("Section " + section.Name + " of type " + section.GetType().Name + " was not exported.");
                    continue;
                }
                if (!materialIds.ContainsKey(section.MaterialName))
                {
                    warnings.Add("Section " + section.Name + ": material " + section.MaterialName + " was not found.");
                    continue;
                }
                int[] regionElementIds = GetRegionElementIds(model, section.RegionName, section.RegionType);
                if (regionElementIds == null)
                {
                    warnings.Add("Section " + section.Name + ": the region " + section.RegionName + " was not exported.");
                    continue;
                }
                //
                partId++;
                List<int> elementIds = new List<int>();
                foreach (int elementId in regionElementIds)
                {
                    if (assignedElementIds.Add(elementId)) elementIds.Add(elementId);
                }
                partElementIds.Add(partId, elementIds);
                partNames.Add(partId, section.Name);
                //
                AppendProperty(sb, section, partId);
                //
                sb.AppendLine("/PART/" + partId);
                sb.AppendLine(GetTitle(section.RegionName));
                sb.AppendLine(I(partId) + I(materialIds[section.MaterialName]) + I(0));
            }
            if (assignedElementIds.Count == 0)
                throw new CaeException("The OpenRadioss export needs at least one solid or shell section " +
                                       "assigned to the mesh.");
            foreach (var entry in model.Mesh.Elements)
            {
                if (!assignedElementIds.Contains(entry.Key))
                {
                    warnings.Add("Some elements have no section assigned and were not exported.");
                    break;
                }
            }
            // Nodes
            sb.AppendLine("/NODE");
            int[] sortedNodeIds = model.Mesh.Nodes.Keys.ToArray();
            Array.Sort(sortedNodeIds);
            foreach (int nodeId in sortedNodeIds)
            {
                FeNode node = model.Mesh.Nodes[nodeId];
                sb.AppendLine(I(nodeId) + D(node.X) + D(node.Y) + D(node.Z));
            }
            // Elements
            foreach (var entry in partElementIds)
            {
                AppendElements(sb, model, entry.Key, partNames[entry.Key], entry.Value, warnings);
            }
            // Ramp function: 0 -> 1 over the first step, then constant
            double firstStepTime = GetTimePeriod(steps[0]);
            double totalTime = 0;
            foreach (Step step in steps) totalTime += GetTimePeriod(step);
            sb.AppendLine("/FUNCT/" + RampFunctionId);
            sb.AppendLine("Ramp");
            sb.AppendLine(D(0) + D(0));
            sb.AppendLine(D(firstStepTime) + D(1));
            sb.AppendLine(D(totalTime * 1000 + 1) + D(1));
            // Boundary conditions and loads collected over all steps
            int groupId = 0;
            int surfaceId = 0;
            int conditionId = 0;
            HashSet<string> writtenNames = new HashSet<string>();
            foreach (Step step in steps)
            {
                foreach (var entry in step.BoundaryConditions)
                {
                    if (!entry.Value.Active || !writtenNames.Add(entry.Key)) continue;
                    AppendBoundaryCondition(sb, model, entry.Value, ref groupId, ref conditionId, warnings);
                }
                foreach (var entry in step.Loads)
                {
                    if (!entry.Value.Active || !writtenNames.Add(entry.Key)) continue;
                    AppendLoad(sb, model, entry.Value, ref groupId, ref surfaceId, ref conditionId, warnings);
                }
            }
            //
            foreach (string warning in warnings.Distinct())
            {
                sb.AppendLine("# WARNING: " + warning);
            }
            sb.AppendLine("/END");
            //
            File.WriteAllText(fileName, sb.ToString());
        }
        static private void AppendHeader(StringBuilder sb, string rootName, FeModel model)
        {
            GetUnitNames(model, out string massUnit, out string lengthUnit, out string timeUnit);
            //
            sb.AppendLine("#RADIOSS STARTER");
            sb.AppendLine("# Created by PrePoMax");
            sb.AppendLine("/BEGIN");
            sb.AppendLine(rootName);
            sb.AppendLine(I(2022) + I(0));
            sb.AppendLine(U(massUnit) + U(lengthUnit) + U(timeUnit));
            sb.AppendLine(U(massUnit) + U(lengthUnit) + U(timeUnit));
        }
        static private void GetUnitNames(FeModel model, out string massUnit, out string lengthUnit, out string timeUnit)
        {
            UnitSystemType unitSystemType = model.UnitSystem != null ? model.UnitSystem.UnitSystemType
                                                                     : UnitSystemType.Undefined;
            switch (unitSystemType)
            {
                case UnitSystemType.MM_TON_S_C:
                    massUnit = "Mg"; lengthUnit = "mm"; timeUnit = "s"; break;
                case UnitSystemType.M_TON_S_C:
                    massUnit = "Mg"; lengthUnit = "m"; timeUnit = "s"; break;
                case UnitSystemType.M_KG_S_C:
                default:
                    massUnit = "kg"; lengthUnit = "m"; timeUnit = "s"; break;
            }
        }
        static private void AppendMaterial(StringBuilder sb, Material material, int materialId, List<string> warnings)
        {
            double density = 0;
            double youngsModulus = 0;
            double poissonsRatio = 0;
            Plastic plastic = null;
            foreach (MaterialProperty property in material.Properties)
            {
                if (property is Density de) density = de.DensityTemp[0][0].Value;
                else if (property is Elastic el)
                {
                    youngsModulus = el.YoungsPoissonsTemp[0][0].Value;
                    poissonsRatio = el.YoungsPoissonsTemp[0][1].Value;
                }
                else if (property is Plastic pl) plastic = pl;
            }
            if (density <= 0)
            {
                density = 1;
                warnings.Add("Material " + material.Name + " has no density; 1.0 was used.");
            }
            if (youngsModulus <= 0)
                warnings.Add("Material " + material.Name + " has no elasticity defined.");
            //
            if (plastic != null)
            {
                // Johnson-Cook without rate and temperature effects: sigma = a + b * eps_p ^ n
                double a = plastic.StressStrainTemp[0][0].Value;
                double b = 0;
                double n = 1;
                if (plastic.StressStrainTemp.Length > 1)
                {
                    double stress2 = plastic.StressStrainTemp[1][0].Value;
                    double strain2 = plastic.StressStrainTemp[1][1].Value;
                    double strain1 = plastic.StressStrainTemp[0][1].Value;
                    if (strain2 > strain1) b = (stress2 - a) / (strain2 - strain1);
                }
                if (plastic.StressStrainTemp.Length > 2)
                    warnings.Add("Material " + material.Name + ": only the first two plastic points were used " +
                                 "(Johnson-Cook a + b*eps^n fit).");
                //
                sb.AppendLine("/MAT/LAW2/" + materialId);
                sb.AppendLine(GetTitle(material.Name));
                sb.AppendLine(D(density));
                sb.AppendLine(D(youngsModulus) + D(poissonsRatio));
                sb.AppendLine(D(a) + D(b) + D(n) + D(0) + D(0));                // a, b, n, eps_max, sig_max
                sb.AppendLine(D(0) + D(0) + I(0) + I(0) + D(0));                // c, eps_0, ICC, Fsmooth, F_cut
                sb.AppendLine(D(0) + D(0) + D(0) + D(0));                       // m, T_melt, rhoC_p, T_r
            }
            else
            {
                sb.AppendLine("/MAT/LAW1/" + materialId);
                sb.AppendLine(GetTitle(material.Name));
                sb.AppendLine(D(density));
                sb.AppendLine(D(youngsModulus) + D(poissonsRatio));
            }
        }
        static private void AppendProperty(StringBuilder sb, Section section, int propertyId)
        {
            if (section is ShellSection)
            {
                sb.AppendLine("/PROP/TYPE1/" + propertyId);
                sb.AppendLine(GetTitle(section.Name));
                sb.AppendLine(I(24) + I(0) + I(0) + I(0));                      // Ishell = 24: QEPH
                sb.AppendLine(D(0) + D(0) + D(0) + D(0) + D(0));                // hm, hf, hr, dm, dn
                sb.AppendLine(I(5) + I(1) + D(section.Thickness.Value));        // N, Istrain, Thick
            }
            else
            {
                sb.AppendLine("/PROP/TYPE14/" + propertyId);
                sb.AppendLine(GetTitle(section.Name));
                sb.AppendLine(I(0) + I(0) + I(0) + I(0) + I(0) + I(0) + I(0) + I(0) + D(0));    // Isolid ... Iframe, dn
                sb.AppendLine(D(0) + D(0) + D(0) + D(0) + D(0));                // q_a, q_b, h, Lambda_v, Mu_v
                sb.AppendLine(D(0) + I(1) + I(0));                              // deltaT_min, Istrain, Ihkt
            }
        }
        static private void AppendElements(StringBuilder sb, FeModel model, int partId, string partName,
                                           List<int> elementIds, List<string> warnings)
        {
            List<string> sh3n = new List<string>();
            List<string> shell = new List<string>();
            List<string> brick = new List<string>();
            List<string> tetra4 = new List<string>();
            List<string> tetra10 = new List<string>();
            //
            foreach (int elementId in elementIds)
            {
                FeElement element = model.Mesh.Elements[elementId];
                int[] nodeIds = element.NodeIds;
                //
                if (element is LinearTriangleElement)
                    sh3n.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]));
                else if (element is ParabolicTriangleElement)
                {
                    sh3n.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]));
                    warnings.Add("Parabolic shell elements were exported as linear elements (corner nodes).");
                }
                else if (element is LinearQuadrilateralElement)
                    shell.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]));
                else if (element is ParabolicQuadrilateralElement)
                {
                    shell.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]));
                    warnings.Add("Parabolic shell elements were exported as linear elements (corner nodes).");
                }
                else if (element is LinearTetraElement)
                    tetra4.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]));
                else if (element is ParabolicTetraElement)
                {
                    tetra10.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]) +
                                I(nodeIds[4]) + I(nodeIds[5]) + I(nodeIds[6]) + I(nodeIds[7]) + I(nodeIds[8]) +
                                Environment.NewLine + I(nodeIds[9]));
                }
                else if (element is LinearHexaElement)
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]) +
                              I(nodeIds[4]) + I(nodeIds[5]) + I(nodeIds[6]) + I(nodeIds[7]));
                else if (element is LinearWedgeElement)
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[2]) +
                              I(nodeIds[3]) + I(nodeIds[4]) + I(nodeIds[5]) + I(nodeIds[5]));
                else if (element is LinearPyramidElement)
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]) +
                              I(nodeIds[4]) + I(nodeIds[4]) + I(nodeIds[4]) + I(nodeIds[4]));
                else if (element is ParabolicHexaElement)
                {
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]) +
                              I(nodeIds[4]) + I(nodeIds[5]) + I(nodeIds[6]) + I(nodeIds[7]));
                    warnings.Add("Parabolic solid elements were exported as linear elements (corner nodes).");
                }
                else if (element is ParabolicWedgeElement)
                {
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[2]) +
                              I(nodeIds[3]) + I(nodeIds[4]) + I(nodeIds[5]) + I(nodeIds[5]));
                    warnings.Add("Parabolic solid elements were exported as linear elements (corner nodes).");
                }
                else if (element is ParabolicPyramidElement)
                {
                    brick.Add(I(elementId) + I(nodeIds[0]) + I(nodeIds[1]) + I(nodeIds[2]) + I(nodeIds[3]) +
                              I(nodeIds[4]) + I(nodeIds[4]) + I(nodeIds[4]) + I(nodeIds[4]));
                    warnings.Add("Parabolic solid elements were exported as linear elements (corner nodes).");
                }
                else warnings.Add("Elements of type " + element.GetType().Name + " were not exported.");
            }
            //
            AppendElementBlock(sb, "/SH3N/" + partId, sh3n);
            AppendElementBlock(sb, "/SHELL/" + partId, shell);
            AppendElementBlock(sb, "/BRICK/" + partId, brick);
            AppendElementBlock(sb, "/TETRA4/" + partId, tetra4);
            AppendElementBlock(sb, "/TETRA10/" + partId, tetra10);
        }
        static private void AppendElementBlock(StringBuilder sb, string keyword, List<string> lines)
        {
            if (lines.Count == 0) return;
            sb.AppendLine(keyword);
            foreach (string line in lines) sb.AppendLine(line);
        }
        static private void AppendNodeGroup(StringBuilder sb, int groupId, string title, int[] nodeIds)
        {
            sb.AppendLine("/GRNOD/NODE/" + groupId);
            sb.AppendLine(GetTitle(title));
            for (int i = 0; i < nodeIds.Length; i += 10)
            {
                StringBuilder line = new StringBuilder();
                for (int j = i; j < Math.Min(i + 10, nodeIds.Length); j++) line.Append(I(nodeIds[j]));
                sb.AppendLine(line.ToString());
            }
        }
        static private void AppendBoundaryCondition(StringBuilder sb, FeModel model, BoundaryCondition boundaryCondition,
                                                    ref int groupId, ref int conditionId, List<string> warnings)
        {
            int[] nodeIds = GetRegionNodeIds(model, boundaryCondition.RegionName, boundaryCondition.RegionType);
            if (nodeIds == null || nodeIds.Length == 0)
            {
                warnings.Add("Boundary condition " + boundaryCondition.Name + " on region " +
                             boundaryCondition.RegionName + " was not exported.");
                return;
            }
            //
            string translations;
            string rotations;
            if (boundaryCondition is FixedBC)
            {
                translations = "111";
                rotations = "111";
            }
            else if (boundaryCondition is DisplacementRotation displacementRotation)
            {
                char[] tra = new char[] { '0', '0', '0' };
                char[] rot = new char[] { '0', '0', '0' };
                for (int dof = 1; dof <= 6; dof++)
                {
                    DOFType dofType = displacementRotation.GetDofType(dof);
                    if (dofType == DOFType.Zero || dofType == DOFType.Fixed)
                    {
                        if (dof <= 3) tra[dof - 1] = '1';
                        else rot[dof - 4] = '1';
                    }
                }
                translations = new string(tra);
                rotations = new string(rot);
            }
            else
            {
                warnings.Add("Boundary condition " + boundaryCondition.Name + " of type " +
                             boundaryCondition.GetType().Name + " was not exported.");
                return;
            }
            //
            groupId++;
            AppendNodeGroup(sb, groupId, boundaryCondition.Name, nodeIds);
            //
            if (translations != "000" || rotations != "000")
            {
                conditionId++;
                sb.AppendLine("/BCS/" + conditionId);
                sb.AppendLine(GetTitle(boundaryCondition.Name));
                sb.AppendLine("   " + translations + " " + rotations + I(0) + I(groupId));
            }
            // Prescribed displacements
            if (boundaryCondition is DisplacementRotation dr)
            {
                string[] directions = new string[] { "X", "Y", "Z", "XX", "YY", "ZZ" };
                EquationContainer[] values = new EquationContainer[] { dr.U1, dr.U2, dr.U3, dr.UR1, dr.UR2, dr.UR3 };
                for (int dof = 1; dof <= 6; dof++)
                {
                    if (dr.GetDofType(dof) == DOFType.Prescribed)
                    {
                        conditionId++;
                        sb.AppendLine("/IMPDISP/" + conditionId);
                        sb.AppendLine(GetTitle(boundaryCondition.Name + " D" + dof));
                        sb.AppendLine(I(RampFunctionId) + A(directions[dof - 1]) + I(0) + I(0) + I(groupId) + I(0) + I(0));
                        sb.AppendLine(D(1) + D(values[dof - 1].Value) + D(0) + D(0));
                    }
                }
            }
        }
        static private void AppendLoad(StringBuilder sb, FeModel model, Load load, ref int groupId, ref int surfaceId,
                                       ref int conditionId, List<string> warnings)
        {
            if (load is CLoad cLoad)
            {
                int[] nodeIds = GetRegionNodeIds(model, cLoad.RegionName, cLoad.RegionType);
                if (nodeIds == null && cLoad.RegionType == RegionTypeEnum.NodeId) nodeIds = new int[] { cLoad.NodeId };
                if (nodeIds == null || nodeIds.Length == 0)
                {
                    warnings.Add("Load " + load.Name + " on region " + load.RegionName + " was not exported.");
                    return;
                }
                groupId++;
                AppendNodeGroup(sb, groupId, load.Name, nodeIds);
                //
                double[] components = new double[] { cLoad.F1.Value, cLoad.F2.Value, cLoad.F3.Value };
                AppendConcentratedLoadComponents(sb, load.Name, components, new string[] { "X", "Y", "Z" },
                                                 groupId, ref conditionId);
            }
            else if (load is MomentLoad momentLoad)
            {
                int[] nodeIds = GetRegionNodeIds(model, momentLoad.RegionName, momentLoad.RegionType);
                if (nodeIds == null || nodeIds.Length == 0)
                {
                    warnings.Add("Load " + load.Name + " on region " + load.RegionName + " was not exported.");
                    return;
                }
                groupId++;
                AppendNodeGroup(sb, groupId, load.Name, nodeIds);
                //
                double[] components = new double[] { momentLoad.M1.Value, momentLoad.M2.Value, momentLoad.M3.Value };
                AppendConcentratedLoadComponents(sb, load.Name, components, new string[] { "XX", "YY", "ZZ" },
                                                 groupId, ref conditionId);
            }
            else if (load is DLoad dLoad)
            {
                int newSurfaceId = AppendSurface(sb, model, dLoad.RegionName, ref surfaceId, warnings);
                if (newSurfaceId == 0)
                {
                    warnings.Add("Load " + load.Name + " on region " + load.RegionName + " was not exported.");
                    return;
                }
                conditionId++;
                sb.AppendLine("/PLOAD/" + conditionId);
                sb.AppendLine(GetTitle(load.Name));
                sb.AppendLine(I(newSurfaceId) + I(RampFunctionId) + I(0) + B() + D(1) + D(dLoad.Magnitude.Value));
            }
            else if (load is GravityLoad gravityLoad)
            {
                int[] nodeIds = GetRegionNodeIds(model, gravityLoad.RegionName, gravityLoad.RegionType);
                int gravityGroupId = 0;     // node group id 0: gravity on the whole model
                if (nodeIds != null && nodeIds.Length > 0)
                {
                    groupId++;
                    gravityGroupId = groupId;
                    AppendNodeGroup(sb, groupId, load.Name, nodeIds);
                }
                //
                string[] directions = new string[] { "X", "Y", "Z" };
                double[] components = new double[] { gravityLoad.F1.Value, gravityLoad.F2.Value, gravityLoad.F3.Value };
                for (int i = 0; i < 3; i++)
                {
                    if (components[i] == 0 || double.IsNaN(components[i])) continue;
                    conditionId++;
                    sb.AppendLine("/GRAV/" + conditionId);
                    sb.AppendLine(GetTitle(load.Name + " " + directions[i]));
                    sb.AppendLine(I(RampFunctionId) + A(directions[i]) + I(0) + I(0) + I(gravityGroupId));
                    sb.AppendLine(D(1) + D(components[i]));
                }
            }
            else
            {
                warnings.Add("Load " + load.Name + " of type " + load.GetType().Name + " was not exported.");
            }
        }
        static private void AppendConcentratedLoadComponents(StringBuilder sb, string loadName, double[] components,
                                                             string[] directions, int groupId, ref int conditionId)
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == 0 || double.IsNaN(components[i])) continue;
                conditionId++;
                sb.AppendLine("/CLOAD/" + conditionId);
                sb.AppendLine(GetTitle(loadName + " " + directions[i]));
                sb.AppendLine(I(RampFunctionId) + A(directions[i]) + I(0) + I(0) + I(groupId) + D(1) + D(components[i]));
            }
        }
        static private int AppendSurface(StringBuilder sb, FeModel model, string surfaceName, ref int surfaceId,
                                         List<string> warnings)
        {
            FeSurface surface;
            if (!model.Mesh.Surfaces.TryGetValue(surfaceName, out surface)) return 0;
            if (surface.ElementFaces == null || surface.ElementFaces.Count == 0) return 0;
            //
            List<string> segments = new List<string>();
            foreach (var entry in surface.ElementFaces)
            {
                FeElementSet elementSet;
                if (!model.Mesh.ElementSets.TryGetValue(entry.Value, out elementSet)) continue;
                foreach (int elementId in elementSet.Labels)
                {
                    FeElement element = model.Mesh.Elements[elementId];
                    int[] cell = element.GetVtkCellFromFaceName(entry.Key);
                    if (cell.Length >= 4)
                        segments.Add(I(cell[0]) + I(cell[1]) + I(cell[2]) + I(cell[3]));
                    else if (cell.Length == 3)
                        segments.Add(I(cell[0]) + I(cell[1]) + I(cell[2]) + I(cell[2]));
                }
            }
            if (segments.Count == 0) return 0;
            //
            surfaceId++;
            sb.AppendLine("/SURF/SEG/" + surfaceId);
            sb.AppendLine(GetTitle(surfaceName));
            foreach (string segment in segments) sb.AppendLine(segment);
            return surfaceId;
        }
        static private int[] GetRegionNodeIds(FeModel model, string regionName, RegionTypeEnum regionType)
        {
            if (regionName == null) return null;
            //
            if (regionType == RegionTypeEnum.NodeSetName)
            {
                if (model.Mesh.NodeSets.TryGetValue(regionName, out FeNodeSet nodeSet)) return nodeSet.Labels;
            }
            else if (regionType == RegionTypeEnum.SurfaceName)
            {
                if (model.Mesh.Surfaces.TryGetValue(regionName, out FeSurface surface) && surface.NodeSetName != null &&
                    model.Mesh.NodeSets.TryGetValue(surface.NodeSetName, out FeNodeSet surfaceNodeSet))
                    return surfaceNodeSet.Labels;
            }
            else if (regionType == RegionTypeEnum.PartName)
            {
                if (model.Mesh.Parts.TryGetValue(regionName, out BasePart part)) return part.NodeLabels;
            }
            return null;
        }
        static private int[] GetRegionElementIds(FeModel model, string regionName, RegionTypeEnum regionType)
        {
            if (regionName == null) return null;
            //
            if (regionType == RegionTypeEnum.ElementSetName)
            {
                if (model.Mesh.ElementSets.TryGetValue(regionName, out FeElementSet elementSet)) return elementSet.Labels;
            }
            else if (regionType == RegionTypeEnum.PartName)
            {
                if (model.Mesh.Parts.TryGetValue(regionName, out BasePart part)) return part.Labels;
            }
            return null;
        }

        // Engine file
        static private void WriteEngineFile(string fileName, string rootName, Step step, int runNumber, double startTime,
                                            bool previousExplicit)
        {
            StringBuilder sb = new StringBuilder();
            double timePeriod = GetTimePeriod(step);
            double stopTime = startTime + timePeriod;
            bool explicitRun = IsExplicit(step);
            //
            sb.AppendLine("#RADIOSS ENGINE");
            sb.AppendLine("# Created by PrePoMax");
            sb.AppendLine("/VERS/2022");
            sb.AppendLine("/RUN/" + rootName + "/" + runNumber);
            sb.AppendLine(E(stopTime));
            sb.AppendLine("/PRINT/-100");
            // Time history output
            sb.AppendLine("/TFILE");
            sb.AppendLine(E(timePeriod / 100));
            // Animation output
            sb.AppendLine("/ANIM/DT");
            sb.AppendLine(E(startTime) + E(timePeriod / 20));
            sb.AppendLine("/ANIM/VECT/DISP");
            sb.AppendLine("/ANIM/VECT/VEL");
            sb.AppendLine("/ANIM/ELEM/VONM");
            sb.AppendLine("/ANIM/ELEM/EPSP");
            //
            // Explicit run with a constant nodal time step (no added mass: dTmin = 0)
            sb.AppendLine("/DT/NODA/CST");
            sb.AppendLine(E(0.9) + E(0));
            if (!explicitRun)
            {
                // Quasi-static (or spring-back) run: explicit with dynamic relaxation.
                // An implicit run (/IMPL cards) needs an OpenRadioss build with MUMPS support.
                sb.AppendLine("# Quasi-static run: dynamic relaxation" + (previousExplicit ? " (spring-back)" : ""));
                sb.AppendLine("/DYREL");
                sb.AppendLine(E(0.995) + E(timePeriod / 10));
            }
            //
            File.WriteAllText(fileName, sb.ToString());
        }

        // Field formatting
        static private string I(int value)
        {
            return value.ToString().PadLeft(10);
        }
        static private string B()
        {
            return new string(' ', 10);
        }
        static private string A(string value)
        {
            return value.PadLeft(10);
        }
        static private string U(string value)
        {
            return value.PadLeft(20);
        }
        static private string D(double value)
        {
            return value.ToString("0.0########E+00", CultureInfo.InvariantCulture).PadLeft(20);
        }
        static private string E(double value)
        {
            // Free format engine value
            return " " + value.ToString("0.0########E+00", CultureInfo.InvariantCulture);
        }
        static private string GetTitle(string title)
        {
            if (title == null) return "";
            if (title.Length > 100) return title.Substring(0, 100);
            return title;
        }
    }
}
