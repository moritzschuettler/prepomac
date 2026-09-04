// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class MaterialsAPI : ModelItemsAPI<Material, MaterialAPI>
    {
        // Variables
        private readonly MaterialService _service;


        // Constructors
        internal MaterialsAPI(MaterialService service)
            : base("material")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a material using command history. The properties are created by pmx.create.",
                   Arguments = "name: str, properties: MaterialProperty or list[MaterialProperty], " +
                               "description: str = None", Returns = "Material",
                   Example = "pmx.model.materials.add('Steel', [pmx.create.elastic(210000, 0.3)])")]
        public MaterialAPI add(string name, object properties, string description = null)
        {
            _service.Add(name, properties, description);
            return this[name];
        }

        [PythonAPI("Replaces an existing material using command history.",
                   Arguments = "old_name: str, name: str, properties: MaterialProperty or " +
                               "list[MaterialProperty], description: str = None", Returns = "Material")]
        public MaterialAPI replace(string old_name, string name, object properties, string description = null)
        {
            _service.Replace(old_name, name, properties, description);
            return this[name];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override Material GetItem(string name)
        {
            return _service.GetMaterial(name);
        }
        protected override MaterialAPI CreateItemAPI(string name)
        {
            return new MaterialAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(Material[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Material item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Materials(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
