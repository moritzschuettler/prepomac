// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace APIs.Python.Model
{
    internal class GeometryPartService : PartService
    {
        // Properties
        protected override string ViewName { get { return "geometry"; } }


        // Constructors
        public GeometryPartService(ModelResolver modelResolver, SelectionService selectionService,
                                   IPythonMeshGateway meshGateway, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, meshGateway, modelGateway)
        {
        }


        // Methods
        public string GetRecognizedMeshType(string partName)
        {
            ResolvePart(partName);
            return ModelGateway.GetRecognizedMeshType(partName);
        }
        public void CreateCompound(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.CreateAndImportCompoundPart(partNames);
        }
        public void RegenerateCompound(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.RegenerateCompoundParts(partNames);
        }
        public void SwapGeometries(string partName1, string partName2)
        {
            ResolvePart(partName1);
            ResolvePart(partName2);
            EnsureCommandsAvailable();
            MeshGateway.SwapPartGeometries(partName1, partName2);
        }
        public void ConvertShellToSolid(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.ConvertShellToSolid(partNames);
        }
        public void ConvertSolidToShell(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.ConvertSolidToShell(partNames);
        }
        public void FindEdgesByAngle(string[] partNames, object edgeAngle)
        {
            double angle = PythonValueConverter.ToDouble(edgeAngle, "edge_angle_deg");
            EnsureCommandsAvailable();
            MeshGateway.FindEdgesByAngleForGeometryParts(partNames, angle);
        }
        public Dictionary<string, int> FlipFaceOrientations(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.FlipFaceOrientations(geometrySelection);
        }
        public Dictionary<string, int> SplitGeometry(SelectionAPI selection, SplitToolAPI tool, object fuzzyTolerance,
                                                     SplitTypeEnum splitType)
        {
            if (tool == null) throw new PythonAPIException("The argument 'tool' must not be None.");
            if (splitType == SplitTypeEnum.Solid && tool.GeometryFeature is SplitByLineTwoPoints)
                throw new PythonAPIException("A line drawn on the surfaces only splits faces. Use split_face " +
                                             "with the surfaces the line is drawn on.");
            // The tool can be used again, so what is resolved for this split goes on a copy of it
            GeometryFeature geometryFeature = tool.GeometryFeature.DeepClone();
            geometryFeature.SplitType = splitType;
            // A solid is split part by part, and faces by the surfaces that are divided, which is what the
            // two dialogs pick
            GeometrySelection region = BuildGeometrySelection(selection, "selection",
                                                              splitType == SplitTypeEnum.Solid ? "parts" :
                                                                                                 "geometry_surfaces");
            geometryFeature.CreationIds = region.GeometryIds;
            geometryFeature.CreationData = region.CreationData;
            //
            if (tool.Splitter != null)
            {
                string[] targets = geometryFeature is SplitByExtendedFace ?
                                   new string[] { "geometry", "geometry_surfaces" } : new string[] { "geometry" };
                GeometrySelection splitter = BuildGeometrySelection(tool.Splitter, tool.SplitterArgumentName, targets);
                geometryFeature.SplitterIds = splitter.GeometryIds;
                geometryFeature.SplitterData = splitter.CreationData;
            }
            if (tool.DirectionSelection != null && geometryFeature is SplitByExtrudedEdges splitByExtrudedEdges)
            {
                GeometrySelection direction = BuildGeometrySelection(tool.DirectionSelection, "direction_selection",
                                                                     "geometry");
                splitByExtrudedEdges.DirectionIds = direction.GeometryIds;
                splitByExtrudedEdges.DirectionData = direction.CreationData;
            }
            //
            double fuzzyValue = fuzzyTolerance == null ? 0 :
                                PythonValueConverter.ToFiniteDouble(fuzzyTolerance, "fuzzy_tolerance");
            if (fuzzyValue < 0) throw new PythonAPIException("The argument 'fuzzy_tolerance' must not be negative.");
            geometryFeature.FuzzyValue = fuzzyValue;
            //
            EnsureCommandsAvailable();
            return MeshGateway.SplitGeometry(geometryFeature);
        }
        // A split compound gathers its cells under the solids they were cut from, and those are the names
        // the tree shows and a rename changes; the cells themselves are named after them
        public string[] GetSplitSubPartNames(string partName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.CreatedBySplit) return new string[0];
            //
            return compoundPart.GetSplitSubPartNames();
        }
        public void RenameSplitSubPart(string partName, string oldName, string newName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.CreatedBySplit)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part made by a split.");
            if (!compoundPart.GetSplitSubPartNames().Contains(oldName))
                throw new PythonAPIException("The compound part '" + partName + "' has no sub-part named '" +
                                             oldName + "'.");
            if (newName == null || newName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'new_name' must not be empty.");
            string error = NamedClass.CheckNameError(newName, null);
            if (error != null) throw new PythonAPIException("The argument 'new_name' is not valid: " + error);
            // The names are compared the way the tree compares them, so two that differ in case only clash
            HashSet<string> reservedNames = ModelResolver.GetModel().GetReservedPartNames();
            if (!string.Equals(newName, oldName, System.StringComparison.OrdinalIgnoreCase) &&
                reservedNames.Contains(newName))
                throw new PythonAPIException("The name '" + newName + "' is already used by another part.");
            //
            EnsureCommandsAvailable();
            MeshGateway.RenameSplitSubPart(partName, oldName, newName);
        }
        public string[] GetSplitCellNames(string partName, string subPartName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.CreatedBySplit)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part made by a split.");
            if (!compoundPart.GetSplitSubPartNames().Contains(subPartName))
                throw new PythonAPIException("The compound part '" + partName + "' has no sub-part named '" +
                                             subPartName + "'.");
            //
            return compoundPart.GetCellNames(subPartName);
        }
        public string GroupSplitCells(string partName, object cellNames, string subPartName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.CreatedBySplit)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part made by a split.");
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(cellNames, "cell_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'cell_names' must name at least one cell.");
            foreach (string name in names)
            {
                if (!compoundPart.SubPartNames.Contains(name))
                    throw new PythonAPIException("The argument 'cell_names' names '" + name + "', which is not a " +
                                                 "cell of the compound part '" + partName + "'.");
            }
            if (names.Distinct().Count() != names.Length)
                throw new PythonAPIException("The argument 'cell_names' names a cell more than once.");
            if (subPartName != null)
            {
                if (subPartName.Trim().Length == 0)
                    throw new PythonAPIException("The argument 'sub_part_name' must not be empty.");
                string error = NamedClass.CheckNameError(subPartName, null);
                if (error != null) throw new PythonAPIException("The argument 'sub_part_name' is not valid: " + error);
                if (!compoundPart.GetSplitSubPartNames().Contains(subPartName) &&
                    ModelResolver.GetModel().GetReservedPartNames().Contains(subPartName))
                    throw new PythonAPIException("The name '" + subPartName + "' is already used by another part.");
            }
            //
            EnsureCommandsAvailable();
            return MeshGateway.GroupSplitCells(partName, names, subPartName);
        }
        public string FuseBodies(string partName, object bodyNames)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part.");
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(bodyNames, "body_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'body_names' must name at least one sub-part or cell.");
            string[] subPartNames = compoundPart.GetSplitSubPartNames();
            foreach (string name in names)
            {
                if (!compoundPart.SubPartNames.Contains(name) && !subPartNames.Contains(name))
                    throw new PythonAPIException("The argument 'body_names' names '" + name + "', which is not a " +
                                                 "sub-part or a cell of the compound part '" + partName + "'.");
            }
            if (names.Distinct().Count() != names.Length)
                throw new PythonAPIException("The argument 'body_names' names a body more than once.");
            //
            EnsureCommandsAvailable();
            return MeshGateway.FuseBodies(partName, names);
        }
        public Dictionary<string, int> Defeature(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.Defeature(geometrySelection);
        }
        public string[] OffsetFace(SelectionAPI selection, object offset)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            double offsetValue = offset == null ? 0 : PythonValueConverter.ToDouble(offset, "offset");
            EnsureCommandsAvailable();
            return MeshGateway.OffsetFace(geometrySelection, offsetValue);
        }
        public Dictionary<string, int> DeleteCadFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.DeleteCadPartFaces(geometrySelection);
        }
        public Dictionary<string, int> FuseFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection", "geometry",
                                                                         "geometry_surfaces");
            EnsureCommandsAvailable();
            return MeshGateway.FuseFaces(geometrySelection);
        }
        public Dictionary<string, int> Repair(string[] partNames, GeometryRepair geometryRepair, object tolerance)
        {
            CheckRepair(geometryRepair, tolerance);
            EnsureCommandsAvailable();
            return MeshGateway.RepairGeometry(partNames, geometryRepair);
        }
        public Dictionary<string, int> SplitPeriodicFaces(string[] partNames, int numOfFaces)
        {
            if (numOfFaces < 2) throw new PythonAPIException("The argument 'number_of_faces' must be at least 2.");
            EnsureCommandsAvailable();
            return MeshGateway.SplitPeriodicFaces(partNames, numOfFaces);
        }
        public Dictionary<string, RepairPreviewAPI> RepairPreview(string[] partNames, GeometryRepair geometryRepair,
                                                                  object tolerance)
        {
            CheckRepair(geometryRepair, tolerance);
            EnsureCommandsAvailable();
            //
            Dictionary<string, RepairPreviewAPI> previews = new Dictionary<string, RepairPreviewAPI>();
            foreach (var entry in MeshGateway.PreviewRepairGeometry(partNames, geometryRepair))
                previews.Add(entry.Key, new RepairPreviewAPI(entry.Key, entry.Value.Refused, entry.Value.Edges,
                                                             entry.Value.Points));
            return previews;
        }
        private static void CheckRepair(GeometryRepair geometryRepair, object tolerance)
        {
            if (tolerance != null)
            {
                geometryRepair.Tolerance = PythonValueConverter.ToDouble(tolerance, "tolerance");
                if (geometryRepair.Tolerance <= 0)
                    throw new PythonAPIException("The argument 'tolerance' must be larger than zero.");
            }
            if (geometryRepair.KernelOptions == 0)
                throw new PythonAPIException("At least one repair must be switched on.");
        }
        public void FlipStlNormals(string[] partNames)
        {
            EnsureCommandsAvailable();
            MeshGateway.FlipStlPartSurfacesNormal(partNames);
        }
        public Dictionary<string, int> DeleteStlFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildGeometrySelection(selection, "selection");
            EnsureCommandsAvailable();
            return MeshGateway.DeleteStlPartFaces(geometrySelection);
        }
        //
        protected override PartProperties GetPartProperties(string partName)
        {
            return MeshGateway.GetGeometryPartProperties(partName);
        }
        protected override FeMesh GetMeshOrNull()
        {
            return ModelResolver.GetModel().Geometry;
        }
        protected override void ReplacePartProperties(string oldPartName, PartProperties properties)
        {
            MeshGateway.ReplaceGeometryPartProperties(oldPartName, properties);
        }
        protected override void SetColorForParts(string[] partNames, Color color)
        {
            MeshGateway.SetColorForGeometryParts(partNames, color);
        }
        protected override void ResetColorForParts(string[] partNames)
        {
            MeshGateway.ResetColorForGeometryParts(partNames);
        }
        protected override void SetTransparencyForParts(string[] partNames, byte alpha)
        {
            MeshGateway.SetTransparencyForGeometryParts(partNames, alpha);
        }
        protected override void HideParts(string[] partNames)
        {
            MeshGateway.HideGeometryParts(partNames);
        }
        protected override void ShowParts(string[] partNames)
        {
            MeshGateway.ShowGeometryParts(partNames);
        }
        protected override void RemoveParts(string[] partNames)
        {
            MeshGateway.RemoveGeometryParts(partNames);
        }
        protected override void TranslateParts(string[] partNames, double[] translateVector, int numberOfCopies)
        {
            MeshGateway.TranslateGeometryParts(partNames, translateVector, numberOfCopies);
        }
        protected override void RotateParts(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                            double rotateAngle, int numberOfCopies)
        {
            MeshGateway.RotateGeometryParts(partNames, rotateCenter, rotateAxis, rotateAngle, numberOfCopies);
        }
        protected override void MirrorParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection,
                                            bool copy)
        {
            MeshGateway.MirrorGeometryParts(partNames, mirrorPoint, mirrorDirection, copy);
        }
        protected override void ScaleParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            MeshGateway.ScaleGeometryParts(partNames, scaleCenter, scaleFactors, copy);
        }
    }
}
