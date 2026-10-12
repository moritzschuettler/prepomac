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
            if (partNames.Length < 2)
                throw new PythonAPIException("The argument 'part_names' must name at least two parts, which is what " +
                                             "a compound part is made from.");
            FeMesh geometry = ResolveMesh();
            HashSet<PartType> stlPartTypes = new HashSet<PartType>();
            HashSet<PartType> cadPartTypes = new HashSet<PartType>();
            foreach (string partName in geometry.GetMeshablePartNames(partNames))
            {
                GeometryPart part = GetPart(partName) as GeometryPart;
                if (part == null)
                    throw new PythonAPIException("The argument 'part_names' names the part '" + partName + "', which " +
                                                 "is not a geometry part.");
                if (part.IsCADPart) cadPartTypes.Add(part.PartType);
                else stlPartTypes.Add(part.PartType);
            }
            if (stlPartTypes.Count + cadPartTypes.Count != 1)
                throw new PythonAPIException("Compound part can be made from only CAD or only stl based geometry " +
                                             "parts of the same type. Give 'part_names' of one kind.");
            //
            EnsureCommandsAvailable();
            MeshGateway.CreateAndImportCompoundPart(partNames);
        }
        public void RegenerateCompound(string[] partNames)
        {
            foreach (string partName in partNames)
            {
                CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
                if (compoundPart == null)
                    throw new PythonAPIException("The argument 'part_names' names the part '" + partName + "', " +
                                                 "which is not a compound part.");
                if (compoundPart.CreatedFromPartNames == null || compoundPart.CreatedFromPartNames.Length <= 1)
                    continue;
                //
                foreach (string createdFromPartName in compoundPart.CreatedFromPartNames)
                {
                    if (GetPart(createdFromPartName) == null)
                        throw new PythonAPIException("The part '" + createdFromPartName + "' that was used to " +
                                                     "create a compound part '" + partName + "' is missing. " +
                                                     "Leave the compound out of 'part_names'.");
                }
            }
            //
            EnsureCommandsAvailable();
            MeshGateway.RegenerateCompoundParts(partNames);
        }
        public void SwapGeometries(string partName1, string partName2)
        {
            string storedPartName1 = ResolvePart(partName1).Name;
            string storedPartName2 = ResolvePart(partName2).Name;
            EnsureCommandsAvailable();
            MeshGateway.SwapPartGeometries(storedPartName1, storedPartName2);
        }
        // The two conversions and the normal flip list only the parts they apply to, so any other part is refused
        public void ConvertShellToSolid(string[] partNames)
        {
            foreach (string partName in partNames)
            {
                GeometryPart part = ResolvePart(partName) as GeometryPart;
                if (part == null || !part.IsCADPart || part.PartType != PartType.Shell)
                    throw new PythonAPIException("The argument 'part_names' names the part '" + partName + "', " +
                                                 "which is not a CAD shell part. Only a CAD shell part can be " +
                                                 "converted to a solid part.");
            }
            //
            EnsureCommandsAvailable();
            MeshGateway.ConvertShellToSolid(partNames);
        }
        public void ConvertSolidToShell(string[] partNames)
        {
            HashSet<string> subPartNames = new HashSet<string>();
            foreach (BasePart basePart in ResolveMesh().Parts.Values)
            {
                if (basePart is CompoundGeometryPart compoundPart) subPartNames.UnionWith(compoundPart.SubPartNames);
            }
            foreach (string partName in partNames)
            {
                GeometryPart part = ResolvePart(partName) as GeometryPart;
                bool solid = part is CompoundGeometryPart ||
                             (part != null && part.IsCADPart &&
                              (part.PartType == PartType.Solid || part.PartType == PartType.SolidAsShell));
                if (!solid || subPartNames.Contains(partName))
                    throw new PythonAPIException("The argument 'part_names' names the part '" + partName + "', " +
                                                 "which is not a CAD solid part or a compound part. Only those can " +
                                                 "be converted to a shell part.");
            }
            //
            EnsureCommandsAvailable();
            MeshGateway.ConvertSolidToShell(partNames);
        }
        public void FindEdgesByAngle(string[] partNames, object edgeAngle)
        {
            double angle = ToEdgeAngle(edgeAngle);
            EnsureCommandsAvailable();
            MeshGateway.FindEdgesByAngleForGeometryParts(partNames, angle);
        }
        public Dictionary<string, int> FlipFaceOrientations(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
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
            if (compoundPart == null || !compoundPart.HasCellSubParts) return new string[0];
            //
            return compoundPart.GetSplitSubPartNames();
        }
        public string[] GetCellNames(string partName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null) return new string[0];
            //
            return (string[])compoundPart.SubPartNames.Clone();
        }
        public string GetMeshGroupId(string partName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || compoundPart.LinkedGroupId == System.Guid.Empty) return null;
            //
            return compoundPart.LinkedGroupId.ToString();
        }
        public void RenameSplitSubPart(string partName, string oldName, string newName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.HasCellSubParts)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part made by a split " +
                                             "or with grouped sub-parts.");
            string storedOldName = FindName(compoundPart.GetSplitSubPartNames(), oldName);
            if (storedOldName == null)
                throw new PythonAPIException("The compound part '" + partName + "' has no sub-part named '" +
                                             oldName + "'.");
            PythonNameComparer.ValidateFormat(newName, "new_name", storedOldName);
            // The names are compared the way the tree compares them, so two that differ in case only clash
            HashSet<string> reservedNames = ModelResolver.GetModel().GetReservedPartNames();
            if (!PythonNameComparer.AreSame(newName, storedOldName) && reservedNames.Contains(newName))
                throw new PythonAPIException("The name '" + newName + "' is already used by another part.");
            //
            EnsureCommandsAvailable();
            MeshGateway.RenameSplitSubPart(compoundPart.Name, storedOldName, newName);
        }
        public string[] GetSplitCellNames(string partName, string subPartName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null || !compoundPart.HasCellSubParts)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part made by a split " +
                                             "or with grouped sub-parts.");
            string storedSubPartName = FindName(compoundPart.GetSplitSubPartNames(), subPartName);
            if (storedSubPartName == null)
                throw new PythonAPIException("The compound part '" + partName + "' has no sub-part named '" +
                                             subPartName + "'.");
            //
            return compoundPart.GetCellNames(storedSubPartName);
        }
        public string GroupSplitCells(string partName, object cellNames, string subPartName)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part.");
            // The sub parts of a compound that was not made by a split are its cells, each a sub part of its own
            string[] subPartNames = compoundPart.HasCellSubParts ? compoundPart.GetSplitSubPartNames() :
                                                                   compoundPart.SubPartNames;
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(cellNames, "cell_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'cell_names' must name at least one cell.");
            string[] storedNames = ResolveBodyNames(names, compoundPart.SubPartNames, null, "cell_names", "cell",
                                                    partName);
            string storedSubPartName = subPartName;
            if (subPartName != null)
            {
                string existingSubPartName = FindName(subPartNames, subPartName);
                PythonNameComparer.ValidateFormat(subPartName, "sub_part_name", existingSubPartName);
                if (existingSubPartName != null) storedSubPartName = existingSubPartName;
                else if (ModelResolver.GetModel().GetReservedPartNames().Contains(subPartName))
                    throw new PythonAPIException("The name '" + subPartName + "' is already used by another part.");
            }
            //
            EnsureCommandsAvailable();
            return MeshGateway.GroupSplitCells(compoundPart.Name, storedNames, storedSubPartName);
        }
        public string FuseBodies(string partName, object bodyNames)
        {
            CompoundGeometryPart compoundPart = ResolvePart(partName) as CompoundGeometryPart;
            if (compoundPart == null)
                throw new PythonAPIException("The part '" + partName + "' is not a compound part.");
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(bodyNames, "body_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'body_names' must name at least one sub-part or cell.");
            string[] storedNames = ResolveBodyNames(names, compoundPart.SubPartNames,
                                                    compoundPart.GetSplitSubPartNames(), "body_names",
                                                    "sub-part or a cell", partName);
            //
            EnsureCommandsAvailable();
            return MeshGateway.FuseBodies(compoundPart.Name, storedNames);
        }
        public Dictionary<string, int> Defeature(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
            EnsureCommandsAvailable();
            return MeshGateway.Defeature(geometrySelection);
        }
        public string[] OffsetFace(SelectionAPI selection, object offset)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
            double offsetValue = offset == null ? 0 : PythonValueConverter.ToFiniteDouble(offset, "offset");
            EnsureCommandsAvailable();
            return MeshGateway.OffsetFace(geometrySelection, offsetValue);
        }
        public Dictionary<string, int> DeleteCadFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
            EnsureCommandsAvailable();
            return MeshGateway.DeleteCadPartFaces(geometrySelection);
        }
        public Dictionary<string, int> FuseFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
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
        // The face dialogs pick geometry surfaces, so that is what their journal names; any geometry naming the
        // same faces is taken as well
        private GeometrySelection BuildFaceSelection(SelectionAPI selection)
        {
            return BuildGeometrySelection(selection, "selection", "geometry", "geometry_surfaces");
        }
        // The stored spelling of a name, compared the way the model compares names, or null
        private static string FindName(IEnumerable<string> names, string name)
        {
            if (name == null) return null;
            //
            foreach (string existingName in names)
            {
                if (PythonNameComparer.AreSame(existingName, name)) return existingName;
            }
            return null;
        }
        // The stored spelling of every named body, each one at most once however it is spelled
        private static string[] ResolveBodyNames(string[] names, string[] cellNames, string[] subPartNames,
                                                 string argumentName, string bodyDescription, string partName)
        {
            string[] storedNames = new string[names.Length];
            HashSet<string> usedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < names.Length; i++)
            {
                string storedName = FindName(cellNames, names[i]);
                if (storedName == null && subPartNames != null) storedName = FindName(subPartNames, names[i]);
                if (storedName == null)
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + names[i] + "', which " +
                                                 "is not a " + bodyDescription + " of the compound part '" + partName +
                                                 "'.");
                if (!usedNames.Add(storedName.Trim()))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + names[i] + "' more " +
                                                 "than once.");
                storedNames[i] = storedName;
            }
            return storedNames;
        }
        private static void CheckRepair(GeometryRepair geometryRepair, object tolerance)
        {
            if (tolerance != null)
            {
                geometryRepair.Tolerance = PythonValueConverter.ToFiniteDouble(tolerance, "tolerance");
                if (geometryRepair.Tolerance <= 0)
                    throw new PythonAPIException("The argument 'tolerance' must be larger than zero.");
            }
            if (geometryRepair.KernelOptions == 0)
                throw new PythonAPIException("At least one repair must be switched on.");
        }
        public void FlipStlNormals(string[] partNames)
        {
            foreach (string partName in partNames)
            {
                GeometryPart part = ResolvePart(partName) as GeometryPart;
                if (part == null || part.IsCADPart)
                    throw new PythonAPIException("The argument 'part_names' names the part '" + partName + "', " +
                                                 "which is not an STL part. Only the normals of an STL part can be " +
                                                 "flipped.");
            }
            //
            EnsureCommandsAvailable();
            MeshGateway.FlipStlPartSurfacesNormal(partNames);
        }
        public Dictionary<string, int> DeleteStlFaces(SelectionAPI selection)
        {
            GeometrySelection geometrySelection = BuildFaceSelection(selection);
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
        // A compound is transformed as one shape, so its CAD sub-parts are moved only with it
        protected override void EnsureTransformable(string[] partNames, string transformName)
        {
            if (MeshGateway != null && MeshGateway.IsExplodedViewActive())
                throw new PythonAPIException("The " + transformName + " transform can only be done when the exploded " +
                                             "view is turned off. Turn it off with pmx.view.remove_exploded_view().");
            foreach (string partName in partNames)
            {
                GeometryPart part = ResolvePart(partName) as GeometryPart;
                if (part == null || !part.IsCADPart || part is CompoundGeometryPart) continue;
                //
                CompoundGeometryPart compoundPart = ModelResolver.GetModel().GetCompoundPartOfSubPart(partName);
                if (compoundPart != null)
                    throw new PythonAPIException("The " + transformName + " transform cannot be done for the " +
                                                 "compound sub-part '" + partName + "'. The argument 'part_names' " +
                                                 "must name its compound '" + compoundPart.Name + "' instead.");
            }
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
