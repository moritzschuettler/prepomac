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
        [PythonAPI("Hides the named parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void hide(object part_names)
        {
            _service.Hide(_service.ResolveNames(part_names, "part_names"));
        }

        [PythonAPI("Shows the named parts using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void show(object part_names)
        {
            _service.Show(_service.ResolveNames(part_names, "part_names"));
        }

        [PythonAPI("Sets the color of the named parts. The color is [R, G, B] or [R, G, B, A] with channels " +
                   "from 0 to 255, or a .NET color name such as 'Red'.",
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

        [PythonAPI("Sets the opacity of the named parts, the alpha channel of their color, as an integer from " +
                   "0 for fully transparent to 255 for opaque.",
                   Arguments = "part_names: list[str] or str, opacity: int", Returns = "None",
                   Example = "parts.set_opacity('Solid-1', 128)")]
        public void set_opacity(object part_names, object opacity)
        {
            _service.SetOpacity(_service.ResolveNames(part_names), opacity);
        }

        [PythonAPI("Replaces the properties of a part using command history. A property that is not given " +
                   "is kept. The element types are a map of the element type name to its value, the way " +
                   "get_element_types names them. An element kind the part does not contain is refused.",
                   Arguments = "old_name: str, name: str = None, color: list[int] or str = None, " +
                               "element_types: dict[str, str] = None", Returns = "None",
                   Example = "parts.replace_properties('Solid-1', name='Bracket', " +
                             "element_types={'linear_tetra': 'C3D4'})")]
        public void replace_properties(string old_name, string name = null, object color = null,
                                       object element_types = null)
        {
            _service.ReplaceProperties(old_name, name, color, element_types);
        }
        [PythonAPI("Returns the supported element type names, for example linear_tetra.",
                   Returns = "StringList")]
        public StringListAPI get_element_type_names()
        {
            return _service.GetElementTypeNames();
        }

        [PythonAPI("Returns the element types of a part as a map of the element type name to its value, in " +
                   "the form set_element_type and replace_properties take them. Only the element kinds " +
                   "the part contains are listed.",
                   Arguments = "part_name: str", Returns = "dict[str, str]",
                   Example = "types = parts.get_element_types('Solid-1')  # {'linear_tetra': 'C3D4', ...}")]
        public Dictionary<string, string> get_element_types(string part_name)
        {
            return _service.GetElementTypes(part_name);
        }

        [PythonAPI("Sets one element type of the named parts using command history. An element kind a " +
                   "part does not contain is refused.",
                   Arguments = "part_names: list[str] or str, element_type: str, value: str", Returns = "None",
                   Example = "parts.set_element_type('Solid-1', 'linear_tetra', 'C3D4')")]
        public void set_element_type(object part_names, string element_type, string value)
        {
            _service.SetElementType(_service.ResolveNames(part_names), element_type, value);
        }

        [PythonAPI("Translates the named parts using command history. A number of copies of 0 moves the parts, and " +
                   "a larger number keeps them and creates that many copies, copy k translated k times.",
                   Arguments = "part_names: list[str] or str, translate_vector: list[float], " +
                               "number_of_copies: int = 0", Returns = "None",
                   Example = "pmx.model.parts.translate('Solid-1', [0, 0, 10])")]
        public void translate(object part_names, object translate_vector, object number_of_copies = null)
        {
            int numberOfCopies = PythonValueConverter.ToIntOrDefault(number_of_copies, 0, "number_of_copies");
            _service.Translate(_service.ResolveNames(part_names), translate_vector, numberOfCopies);
        }

        [PythonAPI("Rotates the named parts around an axis using command history. The angle is in degrees and " +
                   "right handed about the axis. A number of copies of 0 moves the parts, and a larger number " +
                   "keeps them and creates that many copies, copy k rotated k times.",
                   Arguments = "part_names: list[str] or str, rotate_center: list[float], " +
                               "rotate_axis: list[float], rotate_angle_deg: float, number_of_copies: int = 0",
                   Returns = "None",
                   Example = "pmx.model.geometry.parts.rotate('Solid-1', [0, 0, 0], [0, 0, 1], 90)")]
        public void rotate(object part_names, object rotate_center, object rotate_axis, object rotate_angle_deg,
                           object number_of_copies = null)
        {
            int numberOfCopies = PythonValueConverter.ToIntOrDefault(number_of_copies, 0, "number_of_copies");
            _service.Rotate(_service.ResolveNames(part_names), rotate_center, rotate_axis, rotate_angle_deg,
                            numberOfCopies);
        }

        [PythonAPI("Mirrors the named parts over the plane through a point with the given normal using command " +
                   "history. With copy the parts are kept and a mirrored copy is created.",
                   Arguments = "part_names: list[str] or str, mirror_point: list[float], " +
                               "mirror_direction: list[float], copy: bool = False", Returns = "None")]
        public void mirror(object part_names, object mirror_point, object mirror_direction, object copy = null)
        {
            bool keepOriginal = PythonValueConverter.ToBoolOrDefault(copy, false, "copy");
            _service.Mirror(_service.ResolveNames(part_names), mirror_point, mirror_direction, keepOriginal);
        }

        [PythonAPI("Scales the named parts using command history.",
                   Arguments = "part_names: list[str] or str, scale_center: list[float], " +
                               "scale_factors: list[float], copy: bool = False", Returns = "None")]
        public void scale(object part_names, object scale_center, object scale_factors, object copy = null)
        {
            bool keepOriginal = PythonValueConverter.ToBoolOrDefault(copy, false, "copy");
            _service.Scale(_service.ResolveNames(part_names), scale_center, scale_factors, keepOriginal);
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
