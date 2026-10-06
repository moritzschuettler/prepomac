// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class MeshSetupItemAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(MeshingParameters), "meshing_parameters" },
            { typeof(LocalMeshSize), "local_mesh_size" },
            { typeof(ShellMmg), "shell_mmg" },
            { typeof(ThickenShellMesh), "thicken_shell_mesh" },
            { typeof(NetgenMesh), "netgen_mesh" },
            { typeof(TetrahedralGmsh), "tetrahedral_gmsh" },
            { typeof(ShellGmsh), "shell_gmsh" },
            { typeof(TransfiniteMesh), "transfinite_mesh" },
            { typeof(SweepMesh), "sweep_mesh" },
            // A revolve item of an older model is meshed, and replaced, as a sweep item
            { typeof(RevolveMesh), "sweep_mesh" },
            { typeof(ShellQuadWild), "shell_quad_wild" }
        };
        private readonly string _itemName;
        private readonly MeshSetupItemService _service;


        // Properties
        [PythonAPI("The mesh setup item name.", Returns = "str")]
        public string name { get { return _service.ResolveItem(_itemName).Name; } }
        [PythonAPI("The mesh setup item type, named after the add_* method that makes it without the add_ " +
                   "prefix, for example meshing_parameters or local_mesh_size.", Returns = "str")]
        public string item_type { get { return GetItemType(_service.ResolveItem(_itemName)); } }
        [PythonAPI("The mesher used by the item: None, Netgen, Mmg, Gmsh or QuadWild.", Returns = "str")]
        public string mesher_type { get { return _service.ResolveItem(_itemName).MesherType.ToString(); } }
        [PythonAPI("True when the item takes part in the meshing.", Returns = "bool")]
        public bool active { get { return _service.ResolveItem(_itemName).Active; } }
        [PythonAPI("True when the item resolved successfully during the last regeneration.", Returns = "bool")]
        public bool valid { get { return _service.ResolveItem(_itemName).Valid; } }


        // Constructors
        internal MeshSetupItemAPI(string itemName, MeshSetupItemService service)
        {
            _itemName = itemName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(MeshSetupItem item)
        {
            string itemType = GetItemType(item.GetType());
            if (itemType != null) return itemType;
            //
            throw new PythonAPIException("The mesh setup item '" + item.Name + "' is a " + item.GetType().Name +
                                         ", which no add_* method of the Python API makes.");
        }
        internal static string GetItemType(Type type)
        {
            string itemType;
            return ItemTypes.TryGetValue(type, out itemType) ? itemType : null;
        }


        // Methods
        [PythonAPI("Returns the geometry IDs the item was created from.", Returns = "IntList")]
        public IntListAPI get_geometry_ids()
        {
            int[] creationIds = _service.ResolveItem(_itemName).CreationIds;
            return new IntListAPI(creationIds == null ? new int[0] : creationIds);
        }
        [PythonAPI("Returns the stored user meshing settings, including Gmsh and QuadWild options, keyed by " +
                   "the name of the add_* argument that sets each, for example max_size or algorithm_2d; a " +
                   "stored value no argument sets keeps the name of its property.",
                   Returns = "dict[str, Any]")]
        public Dictionary<string, object> get_settings()
        {
            MeshSetupItem item = _service.ResolveItem(_itemName);
            return ModelItemValueReader.GetValues(item, typeof(MeshSetupItem),
                                                  MeshSetupItemsAPI.GetArgumentValueNames(item));
        }
        public override string ToString()
        {
            MeshSetupItem item = _service.ResolveItem(_itemName);
            return "MeshSetupItem(name=" + StringListAPI.ToDisplayString(item.Name) +
                   ", type=" + GetItemType(item) + ")";
        }
    }
}
