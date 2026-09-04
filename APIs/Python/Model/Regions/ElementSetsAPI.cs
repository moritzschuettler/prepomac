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
    public class ElementSetsAPI : ModelItemsAPI<FeElementSet, ElementSetAPI>
    {
        // Variables
        private readonly ElementSetService _service;


        // Constructors
        internal ElementSetsAPI(ElementSetService service)
            : base("element set")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Creates an element set from a selection and adds it to the model using command history.",
                   Arguments = "name: str, selection: Selection", Returns = "ElementSet")]
        public ElementSetAPI add(string name, SelectionAPI selection)
        {
            _service.Add(name, selection);
            return this[name];
        }

        [PythonAPI("Replaces an existing element set using command history.",
                   Arguments = "old_name: str, name: str, selection: Selection", Returns = "ElementSet")]
        public ElementSetAPI replace(string old_name, string name, SelectionAPI selection)
        {
            _service.Replace(old_name, name, selection);
            return this[name];
        }

        [PythonAPI("Converts the named element sets into mesh parts using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void convert_to_mesh_parts(object names)
        {
            _service.ConvertToMeshParts(ResolveNames(names, "names"));
        }
        //
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
