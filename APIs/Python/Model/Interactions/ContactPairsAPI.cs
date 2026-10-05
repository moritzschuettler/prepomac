// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;

namespace APIs.Python.Model
{
    // A contact pair has two regions rather than one, so it takes a master and a slave region. Both are
    // a Selection or a Region, and both name surfaces.
    public class ContactPairsAPI : ModelItemsAPI<ContactPair, ContactPairAPI>
    {
        // Variables
        private readonly ContactPairService _service;


        // Constructors
        internal ContactPairsAPI(ContactPairService service)
            : base("contact pair")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a contact pair using command history. Possible method values are NodeToSurface, " +
                   "SurfaceToSurface and Mortar; pairs may mix methods, which the analysis warns about. " +
                   "Small sliding is supported by NodeToSurface and Mortar only, so it is rejected " +
                   "together with SurfaceToSurface rather than silently dropped. The selection target is " +
                   "surfaces for both sides.",
                   Arguments = "name: str, surface_interaction_name: str, master_region: Selection or " +
                               "Region, slave_region: Selection or Region, method: str = None, " +
                               "small_sliding: bool = None, adjust: bool = None, " +
                               "adjustment_size: float = None", Returns = "ContactPair",
                   Example = "pmx.model.contact_pairs.add('CP-1', 'SI-1', master, slave)")]
        public ContactPairAPI add(string name, object surface_interaction_name, object master_region,
                                  object slave_region, object method = null, object small_sliding = null,
                                  object adjust = null, object adjustment_size = null)
        {
            _service.ValidateName(name, null);
            ContactPair contactPair =
                new ContactPair(name, _service.ResolveSurfaceInteractionName(surface_interaction_name),
                                null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection);
            _service.ApplyMaster(contactPair, master_region);
            _service.ApplySlave(contactPair, slave_region);
            _service.ApplyValues(contactPair, method, small_sliding, adjust, adjustment_size);
            _service.Add(contactPair);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing contact pair using command history. A value that is not given " +
                   "keeps the value the contact pair already carries.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add, all optional",
                   Returns = "ContactPair")]
        public ContactPairAPI replace(string old_name, string name = null,
                                      object surface_interaction_name = null, object master_region = null,
                                      object slave_region = null, object method = null,
                                      object small_sliding = null, object adjust = null,
                                      object adjustment_size = null)
        {
            ContactPair contactPair = _service.CloneForReplace(old_name);
            if (surface_interaction_name != null)
                contactPair.SurfaceInteractionName =
                    _service.ResolveSurfaceInteractionName(surface_interaction_name);
            if (master_region != null) _service.ApplyMaster(contactPair, master_region);
            if (slave_region != null) _service.ApplySlave(contactPair, slave_region);
            _service.ApplyValues(contactPair, method, small_sliding, adjust, adjustment_size);
            if (name != null)
            {
                _service.ValidateName(name, old_name);
                contactPair.Name = name;
            }
            _service.Replace(old_name, contactPair);
            //
            return this[contactPair.Name];
        }

        [PythonAPI("Swaps the master and the slave side of the named contact pairs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void swap_master_slave(object names)
        {
            _service.SwapMasterSlave(ResolveNames(names, "names"));
        }

        [PythonAPI("Merges the named contact pairs that share a master and a slave surface, using command " +
                   "history.", Arguments = "names: list[str] or str", Returns = "None")]
        public void merge_by_master_slave(object names)
        {
            _service.MergeByMasterSlave(ResolveNames(names, "names"));
        }

        [PythonAPI("Hides the symbols of the named contact pairs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the symbols of the named contact pairs using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override ContactPair GetItem(string name)
        {
            return _service.GetContactPair(name);
        }
        protected override ContactPairAPI CreateItemAPI(string name)
        {
            return new ContactPairAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(ContactPair[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(ContactPair item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "ContactPairs(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
