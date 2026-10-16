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
using System.Linq;

namespace APIs.Python.Model
{
    internal class MaterialOrientationService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public MaterialOrientationService(ModelResolver modelResolver, SelectionService selectionService,
                                          IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().MaterialOrientations.Keys.ToArray();
        }
        public MaterialOrientation GetMaterialOrientation(string name)
        {
            if (name == null) return null;
            //
            MaterialOrientation materialOrientation;
            if (!_modelResolver.GetModel().MaterialOrientations.TryGetValue(name, out materialOrientation)) return null;
            //
            return materialOrientation;
        }
        public MaterialOrientation ResolveMaterialOrientation(string name)
        {
            MaterialOrientation materialOrientation = GetMaterialOrientation(name);
            if (materialOrientation == null)
                throw new PythonAPIException("The material orientation '" + name + "' does not exist.");
            //
            return materialOrientation;
        }
        public void ValidateNewName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The material orientation '" + name + "' already exists.");
            }
        }
        // A name is compared without regard to case, and the name the mesh keys the coordinate system by is stored,
        // because the model looks the coordinate system up by it when the orientation is evaluated
        public void ApplyCoordinateSystemName(MaterialOrientation materialOrientation, object coordinateSystemName)
        {
            if (coordinateSystemName == null) return;
            //
            string name = coordinateSystemName as string;
            if (name == null) throw new PythonAPIException("The argument 'coordinate_system_name' must be a string.");
            if (PythonNameComparer.AreSame(name, CoordinateSystem.DefaultCoordinateSystemName))
            {
                materialOrientation.CoordinateSystemName = CoordinateSystem.DefaultCoordinateSystemName;
                return;
            }
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh != null)
            {
                foreach (string modelName in mesh.CoordinateSystems.Keys)
                {
                    if (!PythonNameComparer.AreSame(modelName, name)) continue;
                    //
                    materialOrientation.CoordinateSystemName = modelName;
                    return;
                }
            }
            throw new PythonAPIException("The coordinate system '" + name + "' given as 'coordinate_system_name' " +
                                         "does not exist.");
        }
        // A replace keeps a coordinate system a rename or a remove took away, the orientation staying invalid, also
        // when it is given by the name the orientation already holds
        public bool IsKeptMissingCoordinateSystem(MaterialOrientation materialOrientation, object coordinateSystemName)
        {
            string name = coordinateSystemName as string;
            if (name == null || !PythonNameComparer.AreSame(name, materialOrientation.CoordinateSystemName)) return false;
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            return materialOrientation.CoordinateSystemName != CoordinateSystem.DefaultCoordinateSystemName &&
                   (mesh == null || !mesh.CoordinateSystems.ContainsKey(materialOrientation.CoordinateSystemName));
        }
        public bool IsKeptMissingRegion(MaterialOrientation materialOrientation, object region)
        {
            RegionAPI named = region as RegionAPI;
            if (named == null || named.RegionType != materialOrientation.RegionType) return false;
            //
            return PythonNameComparer.AreSame(named.name, materialOrientation.RegionName) &&
                   IsMissingRegion(materialOrientation);
        }
        // The dialog picks elements, and a part selection is turned into an element set made from the parts
        public void ApplyRegion(MaterialOrientation materialOrientation, object region)
        {
            RegionApplier.Apply(materialOrientation, region, "region", new string[] { "elements", "parts" },
                                _selectionService, _modelResolver);
        }
        public void Add(MaterialOrientation materialOrientation)
        {
            EnsureShellRegion(materialOrientation);
            //
            EnsureCommandsAvailable();
            _modelGateway.AddMaterialOrientation(materialOrientation);
        }
        public void Replace(string oldName, MaterialOrientation materialOrientation)
        {
            MaterialOrientation existing = ResolveMaterialOrientation(oldName);
            string storedName = existing.Name;
            bool keptMissingRegion = materialOrientation.RegionType == existing.RegionType &&
                                     materialOrientation.RegionName == existing.RegionName &&
                                     IsMissingRegion(materialOrientation);
            if (!keptMissingRegion) EnsureShellRegion(materialOrientation);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceMaterialOrientation(storedName, materialOrientation);
        }
        // Controller.ReplaceMaterialOrientation deletes the internal set of the old orientation and converts the new
        // one, so a region that was selection based is reset to its selection for the conversion to recreate it,
        // the way FrmMaterialOrientation does before it replaces
        public MaterialOrientation Clone(string name)
        {
            MaterialOrientation clone = ResolveMaterialOrientation(name).DeepClone();
            if (clone.CreationData != null) clone.RegionType = RegionTypeEnum.Selection;
            //
            return clone;
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateMaterialOrientations(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveMaterialOrientations(names);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateNewName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        public void SetActive(MaterialOrientation[] items, bool active)
        {
            EnsureCommandsAvailable();
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        // The dialog refuses on OK a region that is not made of shell elements, in these words
        private void EnsureShellRegion(MaterialOrientation materialOrientation)
        {
            if (IsShellRegion(materialOrientation)) return;
            //
            throw new PythonAPIException("A material orientation applies to shell elements only. Give in 'region' " +
                                         "shell parts or shell elements.");
        }
        private bool IsShellRegion(MaterialOrientation materialOrientation)
        {
            FeModel model = _modelResolver.GetModel();
            FeMesh mesh = model.Mesh;
            if (mesh == null || model.Properties.ModelSpace != ModelSpaceEnum.ThreeD) return false;
            //
            if (materialOrientation.RegionType == RegionTypeEnum.PartName)
            {
                BasePart part;
                return mesh.Parts.TryGetValue(materialOrientation.RegionName, out part) && part.PartType == PartType.Shell;
            }
            if (materialOrientation.RegionType == RegionTypeEnum.ElementSetName)
            {
                if (!mesh.ElementSets.ContainsKey(materialOrientation.RegionName)) return false;
                return AreShellElements(mesh, model.GetElementIdsOfRegion(materialOrientation));
            }
            if (materialOrientation.RegionType == RegionTypeEnum.Selection)
            {
                if (materialOrientation.CreationIds == null) return false;
                if (materialOrientation.CreationData != null &&
                    materialOrientation.CreationData.SelectItem == vtkSelectItem.Part)
                {
                    BasePart part;
                    foreach (int geometryId in materialOrientation.CreationIds)
                    {
                        part = mesh.GetPartFromId(FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2]);
                        if (part == null || part.PartType != PartType.Shell) return false;
                    }
                    return true;
                }
                return AreShellElements(mesh, materialOrientation.CreationIds);
            }
            return false;
        }
        private static bool AreShellElements(FeMesh mesh, int[] elementIds)
        {
            FeElement element;
            foreach (int elementId in elementIds)
            {
                if (!mesh.Elements.TryGetValue(elementId, out element) ||
                    !MaterialOrientationCalculator.IsShellElement(element)) return false;
            }
            return true;
        }
        private bool IsMissingRegion(MaterialOrientation materialOrientation)
        {
            if (materialOrientation.RegionType == RegionTypeEnum.Selection || materialOrientation.RegionName == null)
                return false;
            //
            FeMesh mesh = _modelResolver.GetModel().Mesh;
            if (mesh == null) return true;
            //
            if (materialOrientation.RegionType == RegionTypeEnum.PartName)
                return !mesh.Parts.ContainsKey(materialOrientation.RegionName);
            if (materialOrientation.RegionType == RegionTypeEnum.ElementSetName)
                return !mesh.ElementSets.ContainsKey(materialOrientation.RegionName);
            return false;
        }
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
