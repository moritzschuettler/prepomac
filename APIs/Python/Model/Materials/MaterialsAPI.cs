// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System.Collections.Generic;

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
        [PythonAPI("Adds a material using command history. The properties are created by pmx.create. " +
                   "Set temperature_dependent to write the temperature column and every property row.",
                   Arguments = "name: str, properties: MaterialProperty or list[MaterialProperty], " +
                               "description: str = None, temperature_dependent: bool = False",
                   Returns = "Material",
                   Example = "pmx.model.materials.add('Steel', [pmx.create.elastic(210000, 0.3)])")]
        public MaterialAPI add(string name, object properties, string description = null,
                               object temperature_dependent = null)
        {
            _service.Add(name, properties, description, temperature_dependent);
            return this[name];
        }

        [PythonAPI("Replaces an existing material using command history. A value that is not given keeps " +
                   "the value the material already carries.",
                   Arguments = "old_name: str, name: str = None, properties: MaterialProperty or " +
                               "list[MaterialProperty] = None, description: str = None, " +
                               "temperature_dependent: bool = None", Returns = "Material")]
        public MaterialAPI replace(string old_name, string name = null, object properties = null,
                                   string description = null, object temperature_dependent = null)
        {
            Material material = _service.Replace(old_name, name, properties, description,
                                                  temperature_dependent);
            return this[material.Name];
        }
        //
        // The characters Material adds to the ones every name may contain
        protected override HashSet<char> GetAdditionalNameCharacters()
        {
            return new HashSet<char>() { '@' };
        }
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
