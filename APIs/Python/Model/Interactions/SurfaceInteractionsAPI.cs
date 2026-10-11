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
    // A surface interaction is composed of properties made by pmx.create, the same way a material is.
    public class SurfaceInteractionsAPI : ModelItemsAPI<SurfaceInteraction, SurfaceInteractionAPI>
    {
        // Variables
        private readonly SurfaceInteractionService _service;


        // Constructors
        internal SurfaceInteractionsAPI(SurfaceInteractionService service)
            : base("surface interaction")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a surface interaction using command history. The properties are created by " +
                   "pmx.create. As the dialog does, the properties must hold a surface behavior, and a Tied " +
                   "surface behavior also a friction, which gives the stick slope.",
                   Arguments = "name: str, properties: SurfaceInteractionProperty or " +
                               "list[SurfaceInteractionProperty]", Returns = "SurfaceInteraction",
                   Example = "pmx.model.surface_interactions.add('SI-1', [pmx.create.surface_behavior('Hard'), " +
                             "pmx.create.friction(0.1)])")]
        public SurfaceInteractionAPI add(object name, object properties)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            _service.Add(_service.Build(nameText, properties));
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing surface interaction using command history. A value that is not " +
                   "given keeps the value the surface interaction already carries; the properties, when " +
                   "given, replace the whole property list and are refused as on add. Every replace refuses an " +
                   "interaction left without a surface behavior, or a tied one without friction, as the " +
                   "dialog does on OK.",
                   Arguments = "old_name: str, name: str = None, properties: SurfaceInteractionProperty or " +
                               "list[SurfaceInteractionProperty] = None", Returns = "SurfaceInteraction")]
        public SurfaceInteractionAPI replace(object old_name, object name = null, object properties = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SurfaceInteraction surfaceInteraction = _service.Replace(oldName, nameText, properties);
            //
            return this[surfaceInteraction.Name];
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override SurfaceInteraction GetItem(string name)
        {
            return _service.GetSurfaceInteraction(name);
        }
        protected override SurfaceInteractionAPI CreateItemAPI(string name)
        {
            return new SurfaceInteractionAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(SurfaceInteraction item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "SurfaceInteractions(count=" + names.Length + ", names=" +
                   new StringListAPI(names).ToString() + ")";
        }
    }
}
