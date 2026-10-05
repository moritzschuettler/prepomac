// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class MeshSetupItemAPI : BaseAPI
    {
        // Variables
        private readonly string _itemName;
        private readonly MeshSetupItemService _service;


        // Properties
        [PythonAPI("The mesh setup item name.", Returns = "str")]
        public string name { get { return _service.ResolveItem(_itemName).Name; } }
        [PythonAPI("The mesh setup item type, for example MeshingParameters or LocalMeshSize.", Returns = "str")]
        public string item_type { get { return _service.ResolveItem(_itemName).GetType().Name; } }
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


        // Methods
        [PythonAPI("Returns the geometry IDs the item was created from.", Returns = "IntList")]
        public IntListAPI get_geometry_ids()
        {
            int[] creationIds = _service.ResolveItem(_itemName).CreationIds;
            return new IntListAPI(creationIds == null ? new int[0] : creationIds);
        }
        [PythonAPI("Returns the stored user meshing settings, including Gmsh and QuadWild options.",
                   Returns = "dict[str, Any]")]
        public Dictionary<string, object> get_settings()
        {
            MeshSetupItem item = _service.ResolveItem(_itemName);
            return ModelItemValueReader.GetValues(item, typeof(MeshSetupItem));
        }
        public override string ToString()
        {
            MeshSetupItem item = _service.ResolveItem(_itemName);
            return "MeshSetupItem(name=" + StringListAPI.ToDisplayString(item.Name) +
                   ", type=" + item.GetType().Name + ")";
        }
    }
}
