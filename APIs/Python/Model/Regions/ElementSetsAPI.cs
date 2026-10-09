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
    public class ElementSetsAPI : ModelItemsAPI<FeElementSet, ElementSetAPI>
    {
        // Variables
        private readonly ElementSetService _service;
        private readonly MeshEditService _meshEditService;


        // Constructors
        internal ElementSetsAPI(ElementSetService service, MeshEditService meshEditService)
            : base("element set")
        {
            _service = service;
            _meshEditService = meshEditService;
        }


        // Methods
        [PythonAPI("Creates an element set from a selection and adds it to the model using command history.",
                   Arguments = "name: str, selection: Selection", Returns = "ElementSet")]
        public ElementSetAPI add(string name, SelectionAPI selection)
        {
            _service.Add(name, selection);
            return this[name];
        }

        [PythonAPI("Replaces an existing element set using command history. A value that is not given is " +
                   "kept.", Arguments = "old_name: str, name: str = None, selection: Selection = None",
                   Returns = "ElementSet")]
        public ElementSetAPI replace(string old_name, string name = null, SelectionAPI selection = null)
        {
            _service.Replace(old_name, name, selection);
            return this[name == null ? old_name : name];
        }

        [PythonAPI("Adds an element set of the elements of the named model parts whose quality metric is " +
                   "larger, or not larger, than the limit using command history. The metric is a gmsh quality " +
                   "measure such as minSICN or gamma, in any case, and the name must not be used by another mesh " +
                   "entity.",
                   Arguments = "name: str, metric: str, part_names: list[str] or str, larger_than: bool, " +
                               "limit: float", Returns = "ElementSet")]
        public ElementSetAPI add_from_quality(string name, string metric, object part_names, bool larger_than,
                                              object limit)
        {
            _meshEditService.CreateElementSetFromQuality(name, metric, part_names, larger_than, limit);
            return this[name];
        }

        [PythonAPI("Converts the named element sets into mesh parts using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void convert_to_mesh_parts(object names)
        {
            _service.ConvertToMeshParts(ResolveNames(names, "names"));
        }
        //
        // The characters FeElementSet adds to the ones every name may contain
        protected override HashSet<char> GetAdditionalNameCharacters()
        {
            return new HashSet<char>() { '@' };
        }
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override FeElementSet GetItem(string name)
        {
            return _service.GetElementSet(name);
        }
        protected override ElementSetAPI CreateItemAPI(string name)
        {
            return new ElementSetAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(FeElementSet item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "ElementSets(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
