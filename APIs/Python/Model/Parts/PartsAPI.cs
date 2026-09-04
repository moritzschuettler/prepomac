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
    // The collection verbs shared by the geometry and the model part collections
    public abstract class PartsAPI<TPartAPI> : ModelItemsAPI<BasePart, TPartAPI>
    {
        // Variables
        private readonly PartService _service;


        // Constructors
        internal PartsAPI(string itemTypeName, PartService service)
            : base(itemTypeName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Hides the named parts using command history.", Arguments = "part_names: list[str] or str",
                   Returns = "None")]
        public void hide(object part_names)
        {
            _service.Hide(_service.ResolveNames(part_names));
        }

        [PythonAPI("Shows the named parts using command history.", Arguments = "part_names: list[str] or str",
                   Returns = "None")]
        public void show(object part_names)
        {
            _service.Show(_service.ResolveNames(part_names));
        }

        [PythonAPI("Sets the color of the named parts. The color is [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "part_names: list[str] or str, color: list[int] or str", Returns = "None",
                   Example = "parts.set_color('Solid-1', [255, 0, 0])")]
        public void set_color(object part_names, object color)
        {
            _service.SetColor(_service.ResolveNames(part_names), color);
        }

        [PythonAPI("Resets the color of the named parts to the default.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void reset_color(object part_names)
        {
            _service.ResetColor(_service.ResolveNames(part_names));
        }

        [PythonAPI("Sets the transparency of the named parts. 0 is fully transparent, 255 is opaque.",
                   Arguments = "part_names: list[str] or str, transparency: int", Returns = "None")]
        public void set_transparency(object part_names, object transparency)
        {
            _service.SetTransparency(_service.ResolveNames(part_names), transparency);
        }

        [PythonAPI("Returns the supported element type names, for example linear_tetra.",
                   Returns = "StringList")]
        public StringListAPI get_element_type_names()
        {
            return _service.GetElementTypeNames();
        }

        [PythonAPI("Returns the element types of a part as a name to value mapping.",
                   Arguments = "part_name: str", Returns = "StringDoubleMap")]
        public StringDoubleMapAPI get_element_types(string part_name)
        {
            return _service.GetElementTypes(part_name);
        }

        [PythonAPI("Sets one element type of the named parts using command history.",
                   Arguments = "part_names: list[str] or str, element_type: str, value: str", Returns = "None",
                   Example = "parts.set_element_type('Solid-1', 'linear_tetra', 'C3D4')")]
        public void set_element_type(object part_names, string element_type, string value)
        {
            _service.SetElementType(_service.ResolveNames(part_names), element_type, value);
        }

        [PythonAPI("Scales the named parts using command history.",
                   Arguments = "part_names: list[str] or str, scale_center: list[float], " +
                               "scale_factors: list[float], copy: bool = False", Returns = "None")]
        public void scale(object part_names, object scale_center, object scale_factors, bool copy = false)
        {
            _service.Scale(_service.ResolveNames(part_names), scale_center, scale_factors, copy);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override BasePart GetItem(string name)
        {
            return _service.GetPart(name);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(BasePart item, string newName)
        {
            _service.Rename(item.Name, newName);
        }
    }
}
