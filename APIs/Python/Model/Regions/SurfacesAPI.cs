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
        public SurfaceAPI add(object name, object selection, object surface_type = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", false);
            string surfaceType = PythonValueConverter.ToText(surface_type, "surface_type", true);
            _service.Add(nameText, selectionValue, surfaceType);
            return this[nameText];
        }

        [PythonAPI("Creates a surface from an existing node set using command history. The surface_type " +
                   "is taken as add takes it. An internal node set, which the dialog does not list, is refused.",
                   Arguments = "name: str, node_set_name: str, surface_type: str = None", Returns = "Surface")]
        public SurfaceAPI add_from_node_set(object name, object node_set_name, object surface_type = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string nodeSetName = PythonValueConverter.ToText(node_set_name, "node_set_name", false);
            string surfaceType = PythonValueConverter.ToText(surface_type, "surface_type", true);
            _service.AddFromNodeSet(nameText, nodeSetName, surfaceType);
            return this[nameText];
        }

        [PythonAPI("Replaces an existing surface using command history. A value that is not given is kept, " +
                   "the surface_type included.",
                   Arguments = "old_name: str, name: str = None, selection: Selection = None, " +
                               "surface_type: str = None",
                   Returns = "Surface")]
        public SurfaceAPI replace(object old_name, object name = null, object selection = null,
                                  object surface_type = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SelectionAPI selectionValue = PythonValueConverter.ToApiObject<SelectionAPI>(selection, "selection", true);
            string surfaceType = PythonValueConverter.ToText(surface_type, "surface_type", true);
            _service.Replace(oldName, nameText, selectionValue, surfaceType);
            return this[nameText == null ? oldName : nameText];
        }
        [PythonAPI("Replaces an existing surface from a node set using command history. A value that is not " +
                   "given is kept, the surface_type included. A node set a rename or a remove took away is kept " +
                   "when it is not given or is given by the name the surface holds, and the surface stays invalid.",
                   Arguments = "old_name: str, name: str = None, node_set_name: str = None, " +
                               "surface_type: str = None",
                   Returns = "Surface")]
        public SurfaceAPI replace_from_node_set(object old_name, object name = null, object node_set_name = null,
                                                object surface_type = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string nodeSetName = PythonValueConverter.ToText(node_set_name, "node_set_name", true);
            string surfaceType = PythonValueConverter.ToText(surface_type, "surface_type", true);
            _service.ReplaceFromNodeSet(oldName, nameText, nodeSetName, surfaceType);
            return this[nameText == null ? oldName : nameText];
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
