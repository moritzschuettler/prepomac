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
