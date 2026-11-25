// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeMesh.ContactSearchNamespace;
using CaeModel;
using System.Collections.Generic;

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

        [PythonAPI("Searches the mesh for faces of different parts that lie within distance of each other and " +
                   "face each other within angle_deg, as Search Contact Pairs of the Interaction menu does, and " +
                   "returns what it found without changing the model; add() of the result adds it. distance is " +
                   "in model units and larger than 0, angle_deg lies between 0 and 180. pair_type is Tie or " +
                   "Contact. group_by is None, one pair per two touching faces, ByParts, one pair per two touching " +
                   "parts, or ByContactGraph, which merges the faces that share nodes so that a node is a slave " +
                   "node only once. solid, shell and shell_edges choose the faces searched, at least one of them " +
                   "True, and ignore_hidden leaves hidden parts out. The defaults are those of the dialog. The " +
                   "result depends on the mesh, so a recorded script that is regenerated on another mesh can find " +
                   "other pairs.",
                   Arguments = "distance: float, angle_deg: float = 35, pair_type: str = 'Tie', " +
                               "group_by: str = 'ByParts', solid: bool = True, shell: bool = True, " +
                               "shell_edges: bool = True, ignore_hidden: bool = True",
                   Returns = "ContactSearchResult",
                   Example = "pmx.model.contact_pairs.search(0.01, pair_type='Tie').add()")]
        public ContactSearchResultAPI search(object distance, object angle_deg = null, object pair_type = null,
                                             object group_by = null, object solid = null, object shell = null,
                                             object shell_edges = null, object ignore_hidden = null)
        {
            double distanceValue = PythonValueConverter.ToDouble(distance, "distance");
            if (distanceValue <= 0)
                throw new PythonAPIException("The argument 'distance' must be larger than 0.");
            double angleDeg = angle_deg == null ? 35 : PythonValueConverter.ToDouble(angle_deg, "angle_deg");
            if (angleDeg < 0 || angleDeg > 180)
                throw new PythonAPIException("The argument 'angle_deg' must lie between 0 and 180.");
            ContactSearchPairTypeEnum pairType = PythonValueConverter.ToEnum<ContactSearchPairTypeEnum>(
                PythonValueConverter.ToTextOrDefault(pair_type, "Tie", "pair_type"), "pair_type");
            GroupContactPairsByEnum groupBy = PythonValueConverter.ToEnum<GroupContactPairsByEnum>(
                PythonValueConverter.ToTextOrDefault(group_by, "ByParts", "group_by"), "group_by");
            if (groupBy == GroupContactPairsByEnum.BySurfaceAngle)
                throw new PythonAPIException("The search does not group by surface angle. Possible values of " +
                                             "'group_by' are None, ByParts and ByContactGraph.");
            //
            GeometryFilterEnum filter = GeometryFilterEnum.None;
            if (PythonValueConverter.ToBoolOrDefault(solid, true, "solid")) filter |= GeometryFilterEnum.Solid;
            if (PythonValueConverter.ToBoolOrDefault(shell, true, "shell")) filter |= GeometryFilterEnum.Shell;
            if (PythonValueConverter.ToBoolOrDefault(shell_edges, true, "shell_edges"))
                filter |= GeometryFilterEnum.ShellEdge;
            if (filter == GeometryFilterEnum.None)
                throw new PythonAPIException("At least one of 'solid', 'shell' and 'shell_edges' must be True.");
            if (PythonValueConverter.ToBoolOrDefault(ignore_hidden, true, "ignore_hidden"))
                filter |= GeometryFilterEnum.IgnoreHidden;
            // The dialog resolves the sides of ties only, and adds what it cannot resolve as a surface
            bool tryToResolve = pairType == ContactSearchPairTypeEnum.Tie;
            List<MasterSlaveItem> masterSlaveItems = _service.Search(distanceValue, angleDeg, filter, groupBy,
                                                                     tryToResolve);
            //
            return new ContactSearchResultAPI(_service, pairType, distanceValue, masterSlaveItems);
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
