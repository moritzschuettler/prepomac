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
                   "SurfaceToSurface, Mortar and Massless; pairs may mix methods, which the analysis warns " +
                   "about. Small sliding is written for NodeToSurface only, so small_sliding=True is " +
                   "rejected together with any other method rather than silently dropped. An " +
                   "adjustment_size turns the adjustment on; it must be finite and zero or larger, or float('nan') for " +
                   "the Default size the dialog offers, which reads back as None, and it is refused together " +
                   "with adjust=False. The selection target is surfaces for both sides. master_color and " +
                   "slave_color are the colors of the two surfaces, given as [R, G, B], [R, G, B, A] or a " +
                   "color name.",
                   Arguments = "name: str, surface_interaction_name: str, master_region: Selection or " +
                               "Region, slave_region: Selection or Region, method: str = None, " +
                               "small_sliding: bool = None, adjust: bool = None, " +
                               "adjustment_size: float = None, master_color: list[int] or str = None, " +
                               "slave_color: list[int] or str = None", Returns = "ContactPair",
                   Example = "pmx.model.contact_pairs.add('CP-1', 'SI-1', master, slave)")]
        public ContactPairAPI add(object name, object surface_interaction_name, object master_region,
                                  object slave_region, object method = null, object small_sliding = null,
                                  object adjust = null, object adjustment_size = null, object master_color = null,
                                  object slave_color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            ContactPair contactPair =
                new ContactPair(nameText, _service.ResolveSurfaceInteractionName(surface_interaction_name),
                                null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection);
            _service.ApplyMaster(contactPair, master_region);
            _service.ApplySlave(contactPair, slave_region);
            _service.ApplyValues(contactPair, method, small_sliding, adjust, adjustment_size);
            ContactPairService.ApplyColors(contactPair, master_color, slave_color);
            _service.Add(contactPair);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing contact pair using command history. A value that is not given " +
                   "keeps the value the contact pair already carries. A method other than NodeToSurface " +
                   "on a pair with small sliding is refused unless small_sliding=False is given too.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add, all optional",
                   Returns = "ContactPair")]
        public ContactPairAPI replace(object old_name, object name = null,
                                      object surface_interaction_name = null, object master_region = null,
                                      object slave_region = null, object method = null,
                                      object small_sliding = null, object adjust = null,
                                      object adjustment_size = null, object master_color = null,
                                      object slave_color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            ContactPair contactPair = _service.CloneForReplace(oldName);
            if (surface_interaction_name != null)
                contactPair.SurfaceInteractionName =
                    _service.ResolveSurfaceInteractionName(surface_interaction_name);
            if (master_region != null) _service.ApplyMaster(contactPair, master_region);
            if (slave_region != null) _service.ApplySlave(contactPair, slave_region);
            _service.ApplyValues(contactPair, method, small_sliding, adjust, adjustment_size);
            ContactPairService.ApplyColors(contactPair, master_color, slave_color);
            if (nameText != null)
            {
                _service.ValidateName(nameText, oldName);
                contactPair.Name = nameText;
            }
            _service.Replace(oldName, contactPair);
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
