// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
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
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName))
                    throw new PythonAPIException("The section '" + name + "' already exists.");
            }
        }
        public void ValidateMaterialName(string materialName)
        {
            if (materialName == null || materialName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'material_name' must not be empty.");
            if (!_modelResolver.GetModel().Materials.ContainsKey(materialName))
                throw new PythonAPIException("The material '" + materialName + "' does not exist.");
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
            EnsureCommandsAvailable();
            _modelGateway.AddSection(section);
        }
        public void Replace(string oldName, Section section)
        {
            ResolveSection(oldName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceSection(oldName, section);
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
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}


