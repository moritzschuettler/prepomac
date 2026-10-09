// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

namespace APIs.Python.Model
{
    public class SurfacesAPI : ModelItemsAPI<FeSurface, SurfaceAPI>
    {
        // Variables
        private readonly SurfaceService _service;


        // Constructors
        internal SurfacesAPI(SurfaceService service)
            : base("surface")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates a surface from a selection and adds it to the model using command history. The " +
                   "surface_type is Element, a surface of element faces, or Node, a surface of the nodes on " +
                   "those faces; it is Element when not given. A load acts on an Element surface only.",
                   Arguments = "name: str, selection: Selection, surface_type: str = None", Returns = "Surface")]
        public SurfaceAPI add(string name, SelectionAPI selection, string surface_type = null)
        {
            _service.Add(name, selection, surface_type);
            return this[name];
        }

        [PythonAPI("Creates a surface from an existing node set using command history. The surface_type " +
                   "is taken as add takes it.",
                   Arguments = "name: str, node_set_name: str, surface_type: str = None", Returns = "Surface")]
        public SurfaceAPI add_from_node_set(string name, string node_set_name, string surface_type = null)
        {
            _service.AddFromNodeSet(name, node_set_name, surface_type);
            return this[name];
        }

        [PythonAPI("Replaces an existing surface using command history. A value that is not given is kept, " +
                   "the surface_type included.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "surface_type: str = None",
                   Returns = "Surface")]
        public SurfaceAPI replace(string old_name, string name = null, SelectionAPI selection = null,
                                  string surface_type = null)
        {
            _service.Replace(old_name, name, selection, surface_type);
            return this[name == null ? old_name : name];
        }
        [PythonAPI("Replaces an existing surface from a node set using command history. A value that is not " +
                   "given is kept, the surface_type included.",
                   Arguments = "old_name: str, name: str = None, node_set_name: str = None, " +
                               "surface_type: str = None",
                   Returns = "Surface")]
        public SurfaceAPI replace_from_node_set(string old_name, string name = null, string node_set_name = null,
                                                string surface_type = null)
        {
            _service.ReplaceFromNodeSet(old_name, name, node_set_name, surface_type);
            return this[name == null ? old_name : name];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override FeSurface GetItem(string name)
        {
            return _service.GetSurface(name);
        }
        protected override SurfaceAPI CreateItemAPI(string name)
        {
            return new SurfaceAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(FeSurface item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Surfaces(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
