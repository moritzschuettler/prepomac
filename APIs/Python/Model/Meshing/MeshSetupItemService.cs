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
using System.Linq;

namespace APIs.Python.Model
{
    internal class MeshSetupItemService
    {
        // Variables
        private readonly ModelResolver _modelResolver;
        private readonly SelectionService _selectionService;
        private readonly IPythonMeshGateway _meshGateway;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public MeshSetupItemService(ModelResolver modelResolver, SelectionService selectionService,
                                    IPythonMeshGateway meshGateway, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _selectionService = selectionService;
            _meshGateway = meshGateway;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            FeMesh geometry = _modelResolver.GetModel().Geometry;
            if (geometry == null) return new string[0];
            //
            return geometry.MeshSetupItems.Keys.ToArray();
        }
        public MeshSetupItem GetItem(string name)
        {
            FeMesh geometry = _modelResolver.GetModel().Geometry;
            if (geometry == null || name == null) return null;
            //
            MeshSetupItem item;
            if (!geometry.MeshSetupItems.TryGetValue(name, out item)) return null;
            //
            return item;
        }
        public MeshSetupItem ResolveItem(string name)
        {
            MeshSetupItem item = GetItem(name);
            if (item == null)
                throw new PythonAPIException("The mesh setup item '" + name + "' does not exist.");
            //
            return item;
        }
        public MeshSetupItem CloneItem(string name)
        {
            MeshSetupItem item = ResolveItem(name);
            if (item is MeshingParameters) return new MeshingParameters((MeshingParameters)item);
            if (item is LocalMeshSize) return new LocalMeshSize((LocalMeshSize)item);
            if (item is ShellMmg) return new ShellMmg((ShellMmg)item);
            if (item is ThickenShellMesh) return new ThickenShellMesh((ThickenShellMesh)item);
            if (item is TetrahedralGmsh) return new TetrahedralGmsh((TetrahedralGmsh)item);
            if (item is ShellGmsh) return new ShellGmsh((ShellGmsh)item);
            if (item is TransfiniteMesh) return new TransfiniteMesh((TransfiniteMesh)item);
            if (item is ExtrudeMesh) return new ExtrudeMesh((ExtrudeMesh)item);
            if (item is RevolveMesh) return new RevolveMesh((RevolveMesh)item);
            if (item is SweepMesh)
            {
                SweepMesh sweepMesh = new SweepMesh(item.Name);
                sweepMesh.CopyFrom((SweepMesh)item);
                return sweepMesh;
            }
            if (item is ShellQuadWild) return new ShellQuadWild((ShellQuadWild)item);
            //
            throw new PythonAPIException("The mesh setup item type '" + item.GetType().Name + "' cannot be cloned.");
        }
        public MeshingParameters CreateDefaultMeshingParameters(string name)
        {
            EnsureCommandsAvailable();
            //
            MeshingParameters parameters = _meshGateway.GetDefaultMeshingParameters(name);
            if (parameters == null)
                throw new PythonAPIException("The default meshing parameters are not available.");
            //
            return parameters;
        }
        public ShellMmg CreateDefaultShellMmg(string name)
        {
            return new ShellMmg(name);
        }
        public void Add(MeshSetupItem item, string name, SelectionAPI selection)
        {
            PrepareItem(item, name, selection);
            AddPreparedItem(item);
        }
        public void PrepareItem(MeshSetupItem item, string name, SelectionAPI selection)
        {
            ValidateName(name, null);
            if (selection == null) throw new PythonAPIException("The argument 'selection' must not be None.");
            item.Name = name;
            ApplySelection(item, selection);
        }
        public void AddPreparedItem(MeshSetupItem item)
        {
            EnsureCommandsAvailable();
            _meshGateway.AddMeshSetupItem(item);
        }
        // The dialog rebuilds the meshing parameters from the size of the selected parts in
        // FrmMeshSetupItem.SelectionChanged, so a scripted item is given the same sizes before the
        // arguments of the call are applied on top of them
        public void ApplyPartDefaults(MeshingParameters item)
        {
            MeshingParameters partDefaults = _meshGateway.GetPartDefaultMeshingParameters(item.Name,
                                                                                          item.CreationIds);
            if (partDefaults == null) return;
            //
            int[] creationIds = item.CreationIds;
            Selection creationData = item.CreationData;
            item.CopyFrom(partDefaults);
            item.CreationIds = creationIds;
            item.CreationData = creationData;
        }
        public void ApplyPartDefaults(ShellMmg item)
        {
            ShellMmg partDefaults = _meshGateway.GetPartDefaultShellMmg(item.Name, item.CreationIds);
            if (partDefaults == null) return;
            //
            int[] creationIds = item.CreationIds;
            Selection creationData = item.CreationData;
            item.CopyFrom(partDefaults);
            item.CreationIds = creationIds;
            item.CreationData = creationData;
        }
        public void Replace(string oldName, MeshSetupItem item, string name, SelectionAPI selection)
        {
            ResolveItem(oldName);
            ValidateName(name, oldName);
            item.Name = name;
            // A replacement keeps the region it was created from until a new selection is given
            if (selection != null) ApplySelection(item, selection);
            //
            EnsureCommandsAvailable();
            _meshGateway.ReplaceMeshSetupItem(oldName, item);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _meshGateway.DuplicateMeshSetupItems(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _meshGateway.RemoveMeshSetupItems(names);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because these items do not belong to a step
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        public void Rename(MeshSetupItem item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _meshGateway.RenameMeshSetupItem(item, newName);
        }
        public void CreateMeshes(string[] partNames)
        {
            EnsureCommandsAvailable();
            _meshGateway.CreateMeshes(partNames);
        }
        public string[] ResolveMeshablePartNames(object partNames)
        {
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(partNames, "part_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'part_names' must not be empty.");
            //
            FeMesh geometry = _modelResolver.GetModel().Geometry;
            foreach (string name in names)
            {
                if (geometry == null || !geometry.Parts.ContainsKey(name))
                    throw new PythonAPIException("The geometry part '" + name + "' does not exist.");
            }
            //
            return names;
        }
        //
        private void ApplySelection(MeshSetupItem item, SelectionAPI selection)
        {
            // A mesh setup item without creation data is rejected by the controller
            SelectionCompilation compilation = _selectionService.CompileForView(selection, "geometry",
                                                                                GetSelectionTargets(item));
            item.CreationData = compilation.Selection;
            item.CreationIds = compilation.Ids;
        }
        // The dialog points the picker at what the item type is defined on, so a whole part mesher records
        // a part selection and a local mesh size records a geometry entity or an edge. A script may say
        // either what the dialog would have recorded, which is what a session journal replays, or the
        // geometry the entity covers
        private static string[] GetSelectionTargets(MeshSetupItem item)
        {
            if (item is LocalMeshSize localMeshSize)
            {
                if (localMeshSize.Type == LocalMeshSizeTypeEnum.NumberOfElements)
                    return new string[] { "edges", "geometry" };
                //
                return new string[] { "geometry" };
            }
            if (item is ExtrudeMesh || item is RevolveMesh || item is SweepMesh)
                return new string[] { "geometry_surfaces", "geometry" };
            //
            return new string[] { "parts", "geometry" };
        }
        private void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName))
                    throw new PythonAPIException("The mesh setup item '" + name + "' already exists.");
            }
        }
        private void EnsureCommandsAvailable()
        {
            if (_meshGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
