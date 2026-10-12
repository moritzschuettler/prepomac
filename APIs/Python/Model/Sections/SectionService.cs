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
using CaeModel;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    internal class SectionService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public SectionService(ModelResolver modelResolver, SelectionService selectionService,
                              IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().Sections.Keys.ToArray();
        }
        public Section GetSection(string name)
        {
            if (name == null) return null;
            //
            Section section;
            if (!_modelResolver.GetModel().Sections.TryGetValue(name, out section)) return null;
            //
            return section;
        }
        public Section ResolveSection(string name)
        {
            Section section = GetSection(name);
            if (section == null) throw new PythonAPIException("The section '" + name + "' does not exist.");
            //
            return section;
        }
        public bool GetIsTwoDimensional()
        {
            ModelSpaceEnum modelSpace = _modelResolver.GetModel().Properties.ModelSpace;
            return modelSpace != ModelSpaceEnum.ThreeD && modelSpace != ModelSpaceEnum.Undefined;
        }
        public void ValidateNewName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The section '" + name + "' already exists.");
            }
        }
        // The controller follows a renamed material by comparing names exactly, so the name is stored in the
        // model's own spelling rather than the caller's
        public string GetStoredMaterialName(string materialName)
        {
            if (materialName == null || materialName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'material_name' must not be empty.");
            Material material;
            if (!_modelResolver.GetModel().Materials.TryGetValue(materialName, out material))
                throw new PythonAPIException("The material '" + materialName + "' does not exist.");
            //
            return material.Name;
        }
        // A replace keeps a reference a rename or a remove took away, the section staying invalid, also when it
        // is given by the name the section already holds
        public bool IsKeptMissingMaterial(Section section, string materialName)
        {
            return PythonNameComparer.AreSame(materialName, section.MaterialName) &&
                   !_modelResolver.GetModel().Materials.ContainsKey(section.MaterialName);
        }
        public bool IsKeptMissingRegion(Section section, object region)
        {
            RegionAPI named = region as RegionAPI;
            if (named == null || named.RegionType != section.RegionType) return false;
            //
            return PythonNameComparer.AreSame(named.name, section.RegionName) && IsMissingRegion(section);
        }
        // The solid, shell and membrane dialogs build an element set, and the solid one does it by
        // picking parts, so an element region accepts either target
        public void ApplyRegion(Section section, object region, string target)
        {
            string[] targets = target == "elements" ? new string[] { "elements", "parts" }
                                                    : new string[] { target };
            RegionApplier.Apply(section, region, "region", targets, _selectionService, _modelResolver);
        }
        public void Add(Section section)
        {
            EnsureCompatibleRegion(section);
            //
            EnsureCommandsAvailable();
            _modelGateway.AddSection(section);
        }
        public void Replace(string oldName, Section section)
        {
            Section existing = ResolveSection(oldName);
            string storedName = existing.Name;
            bool keptMissingRegion = section.RegionType == existing.RegionType &&
                                     section.RegionName == existing.RegionName && IsMissingRegion(section);
            if (!keptMissingRegion) EnsureCompatibleRegion(section);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceSection(storedName, section);
        }
        // Controller.ReplaceSection deletes the internal set of the old section and converts the new one, so a
        // region that was selection based is reset to its selection for the conversion to recreate it, the way
        // FrmSection does before it replaces
        public Section Clone(string name)
        {
            Section clone = ResolveSection(name).DeepClone();
            if (clone.CreationData != null) clone.RegionType = RegionTypeEnum.Selection;
            //
            return clone;
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateSections(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveSections(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        // The dialog refuses on OK a section whose type does not suit the elements of its region, in these words,
        // and judges the section it ends with
        private void EnsureCompatibleRegion(Section section)
        {
            if (IsRegionCompatible(section)) return;
            //
            string hint;
            bool twoD = GetIsTwoDimensional();
            if (_modelResolver.GetModel().Properties.ModelSpace == ModelSpaceEnum.Undefined)
                hint = "The model space is undefined; set pmx.model.properties.model_space first.";
            else if (section is SolidSection && twoD)
                hint = "Give in 'region' the parts of the 2D mesh or their elements.";
            else if (section is SolidSection)
                hint = "Give in 'region' solid parts or solid elements.";
            else if ((section is ShellSection || section is MembraneSection) && !twoD)
                hint = "Give in 'region' shell parts or shell elements.";
            else
                hint = "In a 2D model the dialog takes this section on a selection only, so give a selection in " +
                       "'region'.";
            throw new PythonAPIException("The section type and the section region are not compatible. " + hint);
        }
        private bool IsRegionCompatible(Section section)
        {
            FeModel model = _modelResolver.GetModel();
            FeMesh mesh = model.Mesh;
            ModelSpaceEnum modelSpace = model.Properties.ModelSpace;
            bool solid = section is SolidSection;
            bool shell = section is ShellSection || section is MembraneSection;
            if (mesh == null) return false;
            // 3D
            if (modelSpace == ModelSpaceEnum.ThreeD)
            {
                if (section.RegionType == RegionTypeEnum.PartName)
                {
                    BasePart part;
                    if (!mesh.Parts.TryGetValue(section.RegionName, out part)) return false;
                    return (solid && part.PartType == PartType.Solid) || (shell && part.PartType == PartType.Shell);
                }
                if (section.RegionType == RegionTypeEnum.ElementSetName)
                {
                    FeElementSet elementSet;
                    if (!mesh.ElementSets.TryGetValue(section.RegionName, out elementSet)) return false;
                    return (solid && AreAllElementsOfType<FeElement3D>(mesh, elementSet.Labels)) ||
                           (shell && AreAllElementsOfType<FeElement2D>(mesh, elementSet.Labels));
                }
                if (section.RegionType == RegionTypeEnum.Selection)
                {
                    foreach (BasePart part in GetSelectedParts(mesh, section))
                    {
                        if (solid && part.PartType != PartType.Solid) return false;
                        if (shell && part.PartType != PartType.Shell) return false;
                    }
                    return true;
                }
                return section.RegionType == RegionTypeEnum.NodeSetName ||
                       section.RegionType == RegionTypeEnum.SurfaceName ||
                       section.RegionType == RegionTypeEnum.ReferencePointName;
            }
            // 2D
            if (modelSpace.IsTwoD())
            {
                if (section.RegionType == RegionTypeEnum.PartName)
                {
                    BasePart part;
                    if (!mesh.Parts.TryGetValue(section.RegionName, out part)) return false;
                    return solid && part.PartType == PartType.Shell;
                }
                if (section.RegionType == RegionTypeEnum.ElementSetName)
                {
                    FeElementSet elementSet;
                    if (!mesh.ElementSets.TryGetValue(section.RegionName, out elementSet)) return false;
                    return solid && AreAllElementsOfType<FeElement2D>(mesh, elementSet.Labels);
                }
                if (section.RegionType == RegionTypeEnum.Selection)
                {
                    foreach (BasePart part in GetSelectedParts(mesh, section))
                    {
                        if (solid && part.PartType != PartType.Shell) return false;
                    }
                    return true;
                }
                return section.RegionType == RegionTypeEnum.SurfaceName;
            }
            //
            return false;
        }
        private bool IsMissingRegion(Section section)
        {
            if (section.RegionType == RegionTypeEnum.Selection || section.RegionName == null) return false;
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh == null) return true;
            //
            switch (section.RegionType)
            {
                case RegionTypeEnum.PartName: return !mesh.Parts.ContainsKey(section.RegionName);
                case RegionTypeEnum.ElementSetName: return !mesh.ElementSets.ContainsKey(section.RegionName);
                case RegionTypeEnum.NodeSetName: return !mesh.NodeSets.ContainsKey(section.RegionName);
                case RegionTypeEnum.SurfaceName: return !mesh.Surfaces.ContainsKey(section.RegionName);
                case RegionTypeEnum.ReferencePointName: return !mesh.ReferencePoints.ContainsKey(section.RegionName);
                default: return false;
            }
        }
        // A part selection carries part or geometry ids and an element selection element ids
        private static HashSet<BasePart> GetSelectedParts(FeMesh mesh, Section section)
        {
            HashSet<BasePart> parts = new HashSet<BasePart>();
            if (section.CreationIds == null) return parts;
            //
            bool partIds = section.CreationData != null && section.CreationData.SelectItem == vtkSelectItem.Part;
            bool elementIds = section is SolidSection || section is ShellSection || section is MembraneSection;
            BasePart part;
            FeElement element;
            foreach (int id in section.CreationIds)
            {
                part = null;
                if (partIds) part = mesh.GetPartFromId(FeMesh.GetItemTypePartIdsFromGeometryId(id)[2]);
                else if (elementIds && mesh.Elements.TryGetValue(id, out element))
                    part = mesh.GetPartFromId(element.PartId);
                if (part != null) parts.Add(part);
            }
            return parts;
        }
        private static bool AreAllElementsOfType<TElement>(FeMesh mesh, int[] elementIds) where TElement : FeElement
        {
            FeElement element;
            foreach (int elementId in elementIds)
            {
                if (!mesh.Elements.TryGetValue(elementId, out element) || !(element is TElement)) return false;
            }
            return true;
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}


