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
    public class ModelPartsAPI : PartsAPI<ModelPartAPI>
    {
        // Variables
        private readonly ModelPartService _service;


        // Constructors
        internal ModelPartsAPI(ModelPartService service)
            : base("model part", service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Translates the named parts using command history. A number of copies larger than zero " +
                   "keeps the original and creates that many translated copies.",
                   Arguments = "part_names: list[str] or str, translate_vector: list[float], " +
                               "number_of_copies: int = 0", Returns = "None",
                   Example = "pmx.model.parts.translate('Solid-1', [0, 0, 10])")]
        public void translate(object part_names, object translate_vector, int number_of_copies = 0)
        {
            _service.Translate(_service.ResolveNames(part_names), translate_vector, number_of_copies);
        }

        [PythonAPI("Rotates the named parts around an axis using command history. The angle is in degrees.",
                   Arguments = "part_names: list[str] or str, rotate_center: list[float], " +
                               "rotate_axis: list[float], rotate_angle: float, number_of_copies: int = 0",
                   Returns = "None")]
        public void rotate(object part_names, object rotate_center, object rotate_axis, object rotate_angle,
                           int number_of_copies = 0)
        {
            _service.Rotate(_service.ResolveNames(part_names), rotate_center, rotate_axis, rotate_angle,
                            number_of_copies);
        }

        [PythonAPI("Mirrors the named parts over a plane using command history.",
                   Arguments = "part_names: list[str] or str, mirror_point: list[float], " +
                               "mirror_direction: list[float], copy: bool = False", Returns = "None")]
        public void mirror(object part_names, object mirror_point, object mirror_direction, bool copy = false)
        {
            _service.Mirror(_service.ResolveNames(part_names), mirror_point, mirror_direction, copy);
        }

        [PythonAPI("Merges the named parts into a single part using command history.",
                   Arguments = "part_names: list[str] or str", Returns = "None")]
        public void merge(object part_names)
        {
            _service.Merge(_service.ResolveNames(part_names));
        }

        [PythonAPI("Recomputes the part edges from the angle between the neighbouring faces.",
                   Arguments = "part_names: list[str] or str, edge_angle: float", Returns = "None")]
        public void find_edges_by_angle(object part_names, object edge_angle)
        {
            _service.FindEdgesByAngle(_service.ResolveNames(part_names), edge_angle);
        }
        //
        protected override void SetItemsActive(BasePart[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override ModelPartAPI CreateItemAPI(string name)
        {
            return new ModelPartAPI(name, _service);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "ModelParts(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
