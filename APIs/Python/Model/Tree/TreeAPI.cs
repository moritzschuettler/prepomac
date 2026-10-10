// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using CaeGlobals;
using Python.Runtime;

namespace APIs.Python.Model
{
    // The grouping and ordering of the model tree. A group is identified by a GUID, which the
    // application stores with the layout, so a script that reproduces a recorded layout passes the
    // recorded id; a script that creates a group of its own lets this API generate one and returns it.
    public class TreeAPI : BaseAPI
    {
        // Variables
        // The views are named the way the selections and pmx.view name them
        private static readonly string[] _views = { "geometry", "femodel", "results" };
        // The collections the application can organize, in the words of the API. A step owned
        // collection also needs the name of the step that owns it.
        private static readonly string[] _geometryItemTypes = { "parts", "mesh_setup_items" };
        private static readonly string[] _modelItemTypes = { "parts", "node_sets", "element_sets", "surfaces",
                                                             "reference_points", "coordinate_systems", "materials",
                                                             "sections", "constraints", "surface_interactions",
                                                             "contact_pairs", "distributions", "amplitudes",
                                                             "initial_conditions", "jobs" };
        private static readonly string[] _stepItemTypes = { "history_outputs", "field_outputs",
                                                            "boundary_conditions", "loads", "defined_fields" };
        private static readonly string[] _resultItemTypes = { "parts", "node_sets", "element_sets", "surfaces",
                                                              "reference_points", "coordinate_systems" };
        private readonly ModelResolver _modelResolver;


        // Constructors
        internal TreeAPI(ModelResolver modelResolver)
        {
            _modelResolver = modelResolver;
        }


        // Methods
        [PythonAPI("Returns the collections that can be organized in a model tree view. The view is " +
                   "'geometry', 'femodel' or 'results'.", Arguments = "view: str", Returns = "StringList")]
        public StringListAPI get_item_types(object view)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            return new StringListAPI(GetItemTypes(ToView(viewText)));
        }
        [PythonAPI("Adds a model tree group and optionally moves items into it. The view is 'geometry', " +
                   "'femodel' or 'results'. It returns the group id, which is generated when it is not given. " +
                   "A step owned collection also needs the name of the step that owns it. A group name follows the " +
                   "rules of a model item name, and a group of the geometry or the mesh parts also shares the " +
                   "names of the parts.",
                   Arguments = "view: str, item_type: str, group_name: str, group_id: str = None, " +
                               "parent_group_id: str = None, target_index: int = 0, " +
                               "item_names: list[str] = None, step_name: str = None", Returns = "str",
                   Example = "group = pmx.model.tree.add_group('femodel', 'parts', 'Housing')")]
        public string add_group(object view, object item_type, object group_name, object group_id = null,
                                object parent_group_id = null, object target_index = null, object item_names = null,
                                object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string groupName = PythonValueConverter.ToText(group_name, "group_name", false);
            string groupIdText = PythonValueConverter.ToText(group_id, "group_id", true);
            string parentGroupIdText = PythonValueConverter.ToText(parent_group_id, "parent_group_id", true);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            int targetIndex = PythonValueConverter.ToIntOrDefault(target_index, 0, "target_index");
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, groupName, targetIndex);
            stepName = ToStepName(viewName, itemType, stepName);
            ValidateGroupName(groupName);
            string id = groupIdText == null ? Guid.NewGuid().ToString() : groupIdText;
            Guid groupId = ValidateGroupId(id, "group_id");
            Guid? parentGroupId = null;
            if (parentGroupIdText != null) parentGroupId = ValidateGroupId(parentGroupIdText, "parent_group_id");
            string[] names = ToNames(item_names, "item_names");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            Guid? existingParentId;
            IList<OrderedDictionaryItemNode<string>> existingSiblings;
            if (FindNode(nodes, groupId, null, out existingParentId, out existingSiblings) != null)
                throw new PythonAPIException("The group id '" + id + "' already exists in the " + itemType +
                                             " tree of the " + viewName + " view.");
            IList<OrderedDictionaryItemNode<string>> siblings =
                GetGroupChildren(nodes, parentGroupId, "parent_group_id", viewName, itemType);
            CheckTargetIndex(targetIndex, siblings.Count);
            CheckSiblingGroupName(siblings, groupName, Guid.Empty, itemType);
            CheckPartGroupName(viewName, itemType, groupId, groupName);
            if (names != null) names = ToStoredItemNames(layout, names, "item_names", viewName, itemType);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("CreateGroup", viewName, itemType, stepName, names, null,
                                                         id, parentGroupIdText, targetIndex, groupName);
            return id;
        }
        [PythonAPI("Renames a model tree group. The view is 'geometry', 'femodel' or 'results'. A group name " +
                   "follows the rules of a model item name.",
                   Arguments = "view: str, item_type: str, group_id: str, group_name: str, " +
                               "step_name: str = None", Returns = "None")]
        public void rename_group(object view, object item_type, object group_id, object group_name,
                                 object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string groupIdText = PythonValueConverter.ToText(group_id, "group_id", false);
            string groupName = PythonValueConverter.ToText(group_name, "group_name", false);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, groupName, 0);
            stepName = ToStepName(viewName, itemType, stepName);
            ValidateGroupName(groupName);
            Guid groupId = ValidateGroupId(groupIdText, "group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            Guid? parentId;
            IList<OrderedDictionaryItemNode<string>> siblings;
            ResolveGroup(layout.GetItemLayoutSnapshot().Nodes, groupId, "group_id", viewName, itemType,
                         out parentId, out siblings);
            CheckSiblingGroupName(siblings, groupName, groupId, itemType);
            CheckPartGroupName(viewName, itemType, groupId, groupName);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("RenameGroup", viewName, itemType, stepName, null, null,
                                                         groupIdText, null, -1, groupName);
        }
        [PythonAPI("Removes model tree groups. The items they hold stay in the tree. The view is 'geometry', " +
                   "'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, group_ids: list[str] or str, step_name: str = None",
                   Returns = "None")]
        public void remove_groups(object view, object item_type, object group_ids, object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, null, 0);
            stepName = ToStepName(viewName, itemType, stepName);
            string[] ids = ToNames(group_ids, "group_ids");
            if (ids == null || ids.Length == 0)
                throw new PythonAPIException("The argument 'group_ids' must not be empty.");
            HashSet<Guid> groupIds = new HashSet<Guid>();
            foreach (string id in ids)
            {
                if (!groupIds.Add(ValidateGroupId(id, "group_ids")))
                    throw new PythonAPIException("The group id '" + id + "' is given more than once in 'group_ids'.");
            }
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            foreach (string id in ids)
            {
                Guid groupId = Guid.Parse(id);
                Guid? parentId;
                IList<OrderedDictionaryItemNode<string>> siblings;
                OrderedDictionaryItemNode<string> group = ResolveGroup(nodes, groupId, "group_ids", viewName,
                                                                       itemType, out parentId, out siblings);
                // The children of a removed group take its place, beside groups that may carry their names
                foreach (OrderedDictionaryItemNode<string> child in group.Children)
                {
                    if (!child.IsGroup) continue;
                    //
                    foreach (OrderedDictionaryItemNode<string> sibling in siblings)
                    {
                        if (sibling.IsGroup && sibling.Id != groupId &&
                            string.Equals(sibling.Name, child.Name, StringComparison.OrdinalIgnoreCase))
                            throw new PythonAPIException("The group '" + group.Name + "' cannot be " +
                                                         "removed, because its group '" + child.Name + "' would " +
                                                         "then stand beside a group of the same name.");
                    }
                }
            }
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("RemoveGroup", viewName, itemType, stepName, null, ids,
                                                         null, null, -1, null);
        }
        [PythonAPI("Moves model tree items into a group, or out of every group when no group is given. The view " +
                   "is 'geometry', 'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, item_names: list[str] or str, " +
                               "target_group_id: str = None, target_index: int = 0, step_name: str = None",
                   Returns = "None")]
        public void move_items(object view, object item_type, object item_names, object target_group_id = null,
                               object target_index = null, object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string targetGroupIdText = PythonValueConverter.ToText(target_group_id, "target_group_id", true);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            int targetIndex = PythonValueConverter.ToIntOrDefault(target_index, 0, "target_index");
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, null, targetIndex);
            stepName = ToStepName(viewName, itemType, stepName);
            string[] names = ToNames(item_names, "item_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'item_names' must not be empty.");
            Guid? targetGroupId = null;
            if (targetGroupIdText != null) targetGroupId = ValidateGroupId(targetGroupIdText, "target_group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            names = ToStoredItemNames(layout, names, "item_names", viewName, itemType);
            IList<OrderedDictionaryItemNode<string>> targetChildren =
                GetGroupChildren(layout.GetItemLayoutSnapshot().Nodes, targetGroupId, "target_group_id", viewName,
                                 itemType);
            // The index counts the target's entries after the moved items have left it
            HashSet<string> movedNames = new HashSet<string>(names, layout.Comparer);
            int count = 0;
            foreach (OrderedDictionaryItemNode<string> child in targetChildren)
            {
                if (!child.IsItem || !movedNames.Contains(child.Key)) count++;
            }
            CheckTargetIndex(targetIndex, count);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveItems", viewName, itemType, stepName, names, null,
                                                         null, targetGroupIdText, targetIndex, null);
        }
        [PythonAPI("Moves a model tree group into another group, or to the top level when no group is given. The " +
                   "view is 'geometry', 'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, group_id: str, target_group_id: str = None, " +
                               "target_index: int = 0, step_name: str = None", Returns = "None")]
        public void move_group(object view, object item_type, object group_id, object target_group_id = null,
                               object target_index = null, object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string groupIdText = PythonValueConverter.ToText(group_id, "group_id", false);
            string targetGroupIdText = PythonValueConverter.ToText(target_group_id, "target_group_id", true);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            int targetIndex = PythonValueConverter.ToIntOrDefault(target_index, 0, "target_index");
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, null, targetIndex);
            stepName = ToStepName(viewName, itemType, stepName);
            Guid groupId = ValidateGroupId(groupIdText, "group_id");
            Guid? targetGroupId = null;
            if (targetGroupIdText != null) targetGroupId = ValidateGroupId(targetGroupIdText, "target_group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            Guid? parentId;
            IList<OrderedDictionaryItemNode<string>> siblings;
            OrderedDictionaryItemNode<string> group = ResolveGroup(nodes, groupId, "group_id", viewName, itemType,
                                                                   out parentId, out siblings);
            Guid? nestedParentId;
            IList<OrderedDictionaryItemNode<string>> nestedSiblings;
            if (targetGroupId.HasValue &&
                (targetGroupId.Value == groupId ||
                 FindNode(group.Children, targetGroupId.Value, groupId, out nestedParentId, out nestedSiblings) != null))
                throw new PythonAPIException("The group '" + group.Name + "' cannot be moved into itself or into one " +
                                             "of its own groups.");
            IList<OrderedDictionaryItemNode<string>> targetChildren =
                GetGroupChildren(nodes, targetGroupId, "target_group_id", viewName, itemType);
            CheckSiblingGroupName(targetChildren, group.Name, groupId, itemType);
            // The index counts the target's entries after the moved group has left it
            bool sameParent = parentId.HasValue == targetGroupId.HasValue &&
                              (!targetGroupId.HasValue || parentId.Value == targetGroupId.Value);
            CheckTargetIndex(targetIndex, targetChildren.Count - (sameParent ? 1 : 0));
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveGroup", viewName, itemType, stepName, null, null,
                                                         groupIdText, targetGroupIdText, targetIndex, null);
        }
        [PythonAPI("Returns the whole layout of a collection in the form set_layout takes it: a list whose " +
                   "entries are item names and groups, in tree order. A group is a dict with the keys " +
                   "'group_name', 'group_id' and 'items', the last one a list of the same form. The view is " +
                   "'geometry', 'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, step_name: str = None", Returns = "list",
                   Example = "layout = pmx.model.tree.get_layout('femodel', 'parts')")]
        public object get_layout(object view, object item_type, object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, null, 0);
            stepName = ToStepName(viewName, itemType, stepName);
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, itemType, stepName);
            using (Py.GIL())
            {
                return ToPythonLayout(layout.GetItemLayoutSnapshot().Nodes);
            }
        }
        [PythonAPI("Sets the whole layout of a collection at once: its groups, their names and ids, and the " +
                   "order of every item and group. The layout is a list whose entries are item names and " +
                   "groups, as get_layout returns it; a group is a dict with the keys 'group_name', " +
                   "'group_id' and 'items', and a group given without an id gets a new one. Every item of " +
                   "the collection must be named exactly once, and a list of item names alone removes every " +
                   "group. Setting the layout the collection already has records nothing. A part group of the " +
                   "geometry and the mesh that is renamed is renamed in the other of the two views as well, as " +
                   "rename_group does, and group names are checked against the names they carry once the " +
                   "layout is set, so two groups may swap their names. The view is 'geometry', 'femodel' or " +
                   "'results'.",
                   Arguments = "view: str, item_type: str, layout: list, step_name: str = None",
                   Returns = "None",
                   Example = "pmx.model.tree.set_layout('femodel', 'parts', ['Solid-2', {'group_name': 'Housing', " +
                             "'items': ['Solid-1']}])")]
        public void set_layout(object view, object item_type, object layout, object step_name = null)
        {
            string viewText = PythonValueConverter.ToText(view, "view", false);
            string itemType = PythonValueConverter.ToText(item_type, "item_type", false);
            string stepName = PythonValueConverter.ToText(step_name, "step_name", true);
            itemType = ToItemType(viewText, itemType);
            string viewName = Validate(viewText, itemType, stepName, null, 0);
            stepName = ToStepName(viewName, itemType, stepName);
            if (layout == null) throw new PythonAPIException("The argument 'layout' must not be None.");
            List<object> entries = ToLayoutEntries(layout, "layout");
            //
            IOrderedDictionaryItemLayout<string> dictionary = ResolveLayout(viewName, itemType, stepName);
            OrderedDictionaryItemLayout<string> oldLayout = dictionary.GetItemLayoutSnapshot();
            HashSet<string> itemNames = new HashSet<string>(dictionary.Comparer);
            HashSet<Guid> groupIds = new HashSet<Guid>();
            List<TreeLayoutGroup> groups = new List<TreeLayoutGroup>();
            CheckLayoutEntries(dictionary, entries, itemNames, groupIds, groups, viewName, itemType);
            List<string> missingNames = new List<string>();
            foreach (string key in dictionary.Keys)
            {
                if (!itemNames.Contains(key)) missingNames.Add(key);
            }
            if (missingNames.Count > 0)
                throw new PythonAPIException("The argument 'layout' must name every item of the " + itemType +
                                             " collection exactly once, and it leaves out " +
                                             string.Join(", ", missingNames) + ".");
            Dictionary<Guid, string> linkedRenames = new Dictionary<Guid, string>();
            if (viewName != "results" && itemType == "parts")
            {
                linkedRenames = GetLinkedPartGroupRenames(groups, oldLayout.Nodes, viewName);
                CheckPartGroupNames(groups, oldLayout.Nodes, viewName, linkedRenames);
            }
            //
            OrderedDictionary<string, object> preview =
                new OrderedDictionary<string, object>("Tree layout", dictionary.Comparer);
            foreach (string key in dictionary.Keys) preview.Add(key, null);
            PlaceLayoutEntries(preview, entries, null);
            OrderedDictionaryItemLayout<string> newLayout = preview.GetItemLayoutSnapshot();
            //
            EnsureCommandsAvailable();
            if (linkedRenames.Count == 0)
            {
                _modelResolver.ModelGateway.SetTreeLayout(viewName, itemType, stepName, newLayout);
                return;
            }
            // Both views change in one command, as the tree records a linked rename, so one undo reverts both
            OrderedDictionaryItemLayout<string> linkedLayout = CreateRenamedLayout(GetLinkedPartLayout(viewName),
                                                                                   linkedRenames);
            if (viewName == "geometry") _modelResolver.ModelGateway.SetLinkedPartTreeLayouts(newLayout, linkedLayout);
            else _modelResolver.ModelGateway.SetLinkedPartTreeLayouts(linkedLayout, newLayout);
        }
        public override string ToString()
        {
            return "Tree";
        }
        //
        private void EnsureCommandsAvailable()
        {
            _modelResolver.EnsureModelAvailable();
        }
        private static string ToView(string view)
        {
            if (view != null)
            {
                foreach (string name in _views)
                {
                    if (string.Equals(name, view.Trim(), StringComparison.OrdinalIgnoreCase)) return name;
                }
            }
            throw new PythonAPIException("The argument 'view' must be 'geometry', 'femodel' or 'results'.");
        }
        // The gateway matches a collection regardless of case, so the API does as well and passes the word on
        // in its own spelling
        private static string ToItemType(string view, string itemType)
        {
            if (itemType == null) return null;
            //
            foreach (string supportedType in GetItemTypes(ToView(view)))
            {
                if (string.Equals(supportedType, itemType.Trim(), StringComparison.OrdinalIgnoreCase))
                    return supportedType;
            }
            return itemType;
        }
        // The step collection matches a name exactly, so a step owned collection is reached through the step's
        // own name, found the way every model collection finds a name
        private string ToStepName(string view, string itemType, string stepName)
        {
            if (view == "results" || !IsOwnedByStep(itemType)) return stepName;
            //
            return new StepService(_modelResolver, _modelResolver.ModelGateway).ResolveStep(stepName).Name;
        }
        private static string[] GetItemTypes(string view)
        {
            if (view == "geometry") return _geometryItemTypes;
            if (view == "results") return _resultItemTypes;
            //
            string[] modelItemTypes = new string[_modelItemTypes.Length + _stepItemTypes.Length];
            _modelItemTypes.CopyTo(modelItemTypes, 0);
            _stepItemTypes.CopyTo(modelItemTypes, _modelItemTypes.Length);
            //
            return modelItemTypes;
        }
        private static bool IsOwnedByStep(string itemType)
        {
            foreach (string stepItemType in _stepItemTypes)
            {
                if (stepItemType == itemType) return true;
            }
            return false;
        }
        private static string Validate(string view, string itemType, string stepName, string groupName, int index)
        {
            string viewName = ToView(view);
            string[] itemTypes = GetItemTypes(viewName);
            bool supported = false;
            foreach (string supportedType in itemTypes) supported |= supportedType == itemType;
            if (!supported)
                throw new PythonAPIException("The argument 'item_type' must be one of " +
                                             string.Join(", ", itemTypes) + " in the " + viewName + " view.");
            // Only a step owns a collection of its own, and it is named rather than derived from the view
            bool ownedByStep = IsOwnedByStep(itemType);
            if (ownedByStep && (stepName == null || stepName.Trim().Length == 0))
                throw new PythonAPIException("The argument 'step_name' must name the step that owns the " +
                                             itemType + " collection.");
            if (!ownedByStep && stepName != null)
                throw new PythonAPIException("The argument 'step_name' is only used by a step owned " +
                                             "collection, and the " + itemType + " collection is not one.");
            if (groupName != null && groupName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'group_name' must not be empty.");
            if (index < 0) throw new PythonAPIException("The argument 'target_index' must not be negative.");
            //
            return viewName;
        }
        private IOrderedDictionaryItemLayout<string> ResolveLayout(string view, string itemType, string stepName)
        {
            EnsureCommandsAvailable();
            if (view != "results")
            {
                CaeModel.FeModel model = _modelResolver.GetModel();
                if (IsOwnedByStep(itemType) && model.StepCollection.GetStep(stepName) == null)
                    throw new PythonAPIException("The step '" + stepName + "' does not exist.");
            }
            //
            IOrderedDictionaryItemLayout<string> layout =
                _modelResolver.ModelGateway.GetTreeLayout(view, itemType, stepName);
            if (layout == null)
            {
                string reason;
                if (view == "geometry") reason = "the model has no geometry";
                else if (view == "femodel") reason = "the model has no mesh";
                else reason = "no result is open";
                throw new PythonAPIException("The " + itemType + " collection of the " + view + " view cannot be " +
                                             "organized, because " + reason + ".");
            }
            return layout;
        }
        private static Guid ValidateGroupId(string value, string argumentName)
        {
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
                throw new PythonAPIException("The argument '" + argumentName + "' must be a group id.");
            if (parsed == Guid.Empty)
                throw new PythonAPIException("The argument '" + argumentName + "' must not be the empty group id " +
                                             "'" + Guid.Empty + "'.");
            //
            return parsed;
        }
        private static OrderedDictionaryItemNode<string> FindNode(IList<OrderedDictionaryItemNode<string>> nodes,
                                                                  Guid id, Guid? parentId, out Guid? nodeParentId,
                                                                  out IList<OrderedDictionaryItemNode<string>> siblings)
        {
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (node.Id == id)
                {
                    nodeParentId = parentId;
                    siblings = nodes;
                    return node;
                }
                if (!node.IsGroup) continue;
                //
                OrderedDictionaryItemNode<string> found = FindNode(node.Children, id, node.Id, out nodeParentId,
                                                                   out siblings);
                if (found != null) return found;
            }
            nodeParentId = null;
            siblings = null;
            return null;
        }
        private static OrderedDictionaryItemNode<string> ResolveGroup(
            IList<OrderedDictionaryItemNode<string>> nodes, Guid groupId, string argumentName, string view,
            string itemType, out Guid? parentId, out IList<OrderedDictionaryItemNode<string>> siblings)
        {
            OrderedDictionaryItemNode<string> group = FindNode(nodes, groupId, null, out parentId, out siblings);
            if (group == null || !group.IsGroup)
                throw new PythonAPIException("The argument '" + argumentName + "' names the group '" + groupId +
                                             "', which does not exist in the " + itemType + " tree of the " + view +
                                             " view.");
            //
            return group;
        }
        private static IList<OrderedDictionaryItemNode<string>> GetGroupChildren(
            IList<OrderedDictionaryItemNode<string>> nodes, Guid? groupId, string argumentName, string view,
            string itemType)
        {
            if (!groupId.HasValue) return nodes;
            //
            Guid? parentId;
            IList<OrderedDictionaryItemNode<string>> siblings;
            return ResolveGroup(nodes, groupId.Value, argumentName, view, itemType, out parentId, out siblings)
                   .Children;
        }
        private static void CheckTargetIndex(int index, int count)
        {
            if (index > count)
                throw new PythonAPIException("The argument 'target_index' is " + index + ", but the target holds " +
                                             count + " entries, so it must be at most " + count + ".");
        }
        private static void CheckSiblingGroupName(IList<OrderedDictionaryItemNode<string>> siblings, string name,
                                                  Guid ignoredGroupId, string itemType)
        {
            string trimmed = name.Trim();
            foreach (OrderedDictionaryItemNode<string> sibling in siblings)
            {
                if (sibling.IsGroup && sibling.Id != ignoredGroupId &&
                    string.Equals(sibling.Name, trimmed, StringComparison.OrdinalIgnoreCase))
                    throw new PythonAPIException("A group named '" + sibling.Name + "' already exists at that " +
                                                 "place in the " + itemType + " tree. Uppercase and lowercase " +
                                                 "letters are regarded as equal.");
            }
        }
        // A group name follows the rules of a model item name in every collection, and a part group of the
        // geometry and the mesh also shares the namespace of the parts, which CheckPartGroupName checks
        private static void ValidateGroupName(string name)
        {
            if (name == null) throw new PythonAPIException("The argument 'group_name' must not be None.");
            //
            PythonNameComparer.ValidateFormat(name, "group_name", null);
        }
        private void CheckPartGroupName(string view, string itemType, Guid groupId, string name)
        {
            if (view == "results" || itemType != "parts") return;
            //
            string trimmed = name.Trim();
            if (_modelResolver.GetModel().GetPartAndPartGroupNames(groupId).Contains(trimmed))
                throw new PythonAPIException("The group name '" + trimmed + "' is already the name of a part or " +
                                             "of a part group. Uppercase and lowercase letters are regarded as " +
                                             "equal.");
        }
        // The gateway matches the names exactly, so each one is passed on in the collection's own spelling
        private static string[] ToStoredItemNames(IOrderedDictionaryItemLayout<string> layout, string[] names,
                                                  string argumentName, string view, string itemType)
        {
            HashSet<string> givenNames = new HashSet<string>(layout.Comparer);
            string[] storedNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i];
                if (!layout.ContainsKey(name))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + name + "', which " +
                                                 "does not exist in the " + itemType + " collection of the " +
                                                 view + " view.");
                if (!givenNames.Add(name))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + name +
                                                 "' more than once.");
                foreach (string key in layout.Keys)
                {
                    if (layout.Comparer.Equals(key, name))
                    {
                        storedNames[i] = key;
                        break;
                    }
                }
            }
            return storedNames;
        }
        private static string[] ToNames(object value, string argumentName)
        {
            if (value == null) return null;
            //
            return PythonStringListConverter.ToStringArrayOrSingle(value, argumentName);
        }
        // Layout
        private static PyList ToPythonLayout(IEnumerable<OrderedDictionaryItemNode<string>> nodes)
        {
            PyList list = new PyList();
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (node.IsItem)
                {
                    using (PyString name = new PyString(node.Key)) list.Append(name);
                    continue;
                }
                //
                using (PyDict group = new PyDict())
                using (PyString name = new PyString(node.Name))
                using (PyString id = new PyString(node.Id.ToString()))
                using (PyList items = ToPythonLayout(node.Children))
                {
                    group.SetItem("group_name", name);
                    group.SetItem("group_id", id);
                    group.SetItem("items", items);
                    list.Append(group);
                }
            }
            return list;
        }
        private static List<object> ToLayoutEntries(object value, string argumentName)
        {
            PyObject pyObject = value as PyObject;
            if (pyObject == null) return ToManagedLayoutEntries(value, argumentName);
            //
            try
            {
                using (Py.GIL())
                {
                    return ToPythonLayoutEntries(pyObject, argumentName);
                }
            }
            catch (PythonAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' could not be read as a tree " +
                                             "layout: " + ex.Message);
            }
        }
        private static List<object> ToPythonLayoutEntries(PyObject value, string argumentName)
        {
            if (value.IsNone() || (!PyList.IsListType(value) && !PyTuple.IsTupleType(value)))
                throw InvalidLayout(argumentName);
            //
            List<object> entries = new List<object>();
            long count = value.Length();
            for (int i = 0; i < count; i++)
            {
                using (PyObject entry = value[i])
                {
                    if (PyString.IsStringType(entry))
                    {
                        entries.Add(entry.As<string>());
                        continue;
                    }
                    if (!PyDict.IsDictType(entry)) throw InvalidLayout(argumentName);
                    //
                    string groupName = null;
                    string groupId = null;
                    List<object> items = new List<object>();
                    using (PyDict group = new PyDict(entry))
                    using (PyObject keys = group.Keys())
                    {
                        long keyCount = keys.Length();
                        for (int j = 0; j < keyCount; j++)
                        {
                            using (PyObject keyObject = keys.GetItem(j))
                            using (PyObject itemObject = group.GetItem(keyObject))
                            {
                                string key = PyString.IsStringType(keyObject) ? keyObject.As<string>() : null;
                                if (key == "group_name" && PyString.IsStringType(itemObject))
                                    groupName = itemObject.As<string>();
                                else if (key == "group_id" && itemObject.IsNone()) { }
                                else if (key == "group_id" && PyString.IsStringType(itemObject))
                                    groupId = itemObject.As<string>();
                                else if (key == "items") items = ToPythonLayoutEntries(itemObject, argumentName);
                                else throw InvalidGroup(argumentName, keyObject.ToString());
                            }
                        }
                    }
                    entries.Add(CreateLayoutGroup(groupName, groupId, items, argumentName));
                }
            }
            return entries;
        }
        private static List<object> ToManagedLayoutEntries(object value, string argumentName)
        {
            IEnumerable list = value as IEnumerable;
            if (list == null || value is string || value is IDictionary) throw InvalidLayout(argumentName);
            //
            List<object> entries = new List<object>();
            foreach (object entry in list)
            {
                if (entry is string)
                {
                    entries.Add(entry);
                    continue;
                }
                IDictionary group = entry as IDictionary;
                if (group == null) throw InvalidLayout(argumentName);
                //
                string groupName = null;
                string groupId = null;
                List<object> items = new List<object>();
                foreach (DictionaryEntry field in group)
                {
                    string key = field.Key as string;
                    if (key == "group_name" && field.Value is string) groupName = (string)field.Value;
                    else if (key == "group_id" && field.Value == null) { }
                    else if (key == "group_id" && field.Value is string) groupId = (string)field.Value;
                    else if (key == "items" && field.Value != null)
                        items = ToManagedLayoutEntries(field.Value, argumentName);
                    else throw InvalidGroup(argumentName, Convert.ToString(field.Key));
                }
                entries.Add(CreateLayoutGroup(groupName, groupId, items, argumentName));
            }
            return entries;
        }
        private static TreeLayoutGroup CreateLayoutGroup(string groupName, string groupId, List<object> items,
                                                         string argumentName)
        {
            if (groupName == null)
                throw new PythonAPIException("A group in '" + argumentName + "' must carry its name as " +
                                             "'group_name'.");
            ValidateGroupName(groupName);
            Guid id = groupId == null ? Guid.NewGuid() : ValidateGroupId(groupId, "group_id");
            //
            return new TreeLayoutGroup(groupName.Trim(), id, items);
        }
        private static PythonAPIException InvalidLayout(string argumentName)
        {
            return new PythonAPIException("The argument '" + argumentName + "' must be a list whose entries are " +
                                          "item names and groups, a group being a dict with the keys " +
                                          "'group_name', 'group_id' and 'items'.");
        }
        private static PythonAPIException InvalidGroup(string argumentName, string key)
        {
            return new PythonAPIException("A group in '" + argumentName + "' takes 'group_name' as a str, " +
                                          "'group_id' as a str or None and 'items' as a list, and the entry '" +
                                          key + "' is none of them.");
        }
        private static void CheckLayoutEntries(IOrderedDictionaryItemLayout<string> dictionary, List<object> entries,
                                               HashSet<string> itemNames, HashSet<Guid> groupIds,
                                               List<TreeLayoutGroup> groups, string view, string itemType)
        {
            HashSet<string> siblingGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (object entry in entries)
            {
                string itemName = entry as string;
                if (itemName != null)
                {
                    if (!dictionary.ContainsKey(itemName))
                        throw new PythonAPIException("The argument 'layout' names '" + itemName + "', which does " +
                                                     "not exist in the " + itemType + " collection of the " + view +
                                                     " view.");
                    if (!itemNames.Add(itemName))
                        throw new PythonAPIException("The argument 'layout' names '" + itemName + "' more than " +
                                                     "once; names are compared regardless of case.");
                    continue;
                }
                //
                TreeLayoutGroup group = (TreeLayoutGroup)entry;
                if (!groupIds.Add(group.Id))
                    throw new PythonAPIException("The group id '" + group.Id + "' is given more than once in " +
                                                 "'layout'.");
                if (!siblingGroupNames.Add(group.Name))
                    throw new PythonAPIException("The argument 'layout' places two groups named '" + group.Name +
                                                 "' side by side. Uppercase and lowercase letters are regarded " +
                                                 "as equal.");
                groups.Add(group);
                CheckLayoutEntries(dictionary, group.Entries, itemNames, groupIds, groups, view, itemType);
            }
        }
        // A part group shares the namespace of the parts and of every part group of the geometry and the mesh. A
        // group that keeps its id and its name was already checked when it got them, so only a new or renamed
        // group is checked, as the tree checks a group it creates or renames, and against the names every group
        // carries once the layout and its linked renames are set, so two groups may swap their names. A compound
        // part takes the name of its group only when the group is renamed, one part after the other, so it
        // keeps its present name here.
        private void CheckPartGroupNames(List<TreeLayoutGroup> groups,
                                         IEnumerable<OrderedDictionaryItemNode<string>> oldNodes, string view,
                                         Dictionary<Guid, string> linkedRenames)
        {
            HashSet<string> layoutGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (TreeLayoutGroup group in groups)
            {
                if (!layoutGroupNames.Add(group.Name))
                    throw new PythonAPIException("The argument 'layout' names two part groups '" + group.Name +
                                                 "'. A part group shares the names of the parts and of the other " +
                                                 "part groups, and uppercase and lowercase letters are regarded " +
                                                 "as equal.");
            }
            //
            Dictionary<string, HashSet<Guid>> groupOwners = new Dictionary<string, HashSet<Guid>>(
                StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> partNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, Guid> compoundPartGroups = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            IOrderedDictionaryItemLayout<string> linkedLayout = GetLinkedPartLayout(view);
            if (linkedLayout != null)
                AddFinalGroupNames(linkedLayout.GetItemLayoutSnapshot().Nodes, linkedRenames, groupOwners);
            foreach (TreeLayoutGroup group in groups) AddOwner(groupOwners, group.Name, group.Id);
            AddPartNames(_modelResolver.GetModel(), partNames, compoundPartGroups);
            //
            List<OrderedDictionaryItemNode<string>> oldNodeList = new List<OrderedDictionaryItemNode<string>>(oldNodes);
            foreach (TreeLayoutGroup group in groups)
            {
                Guid? parentId;
                IList<OrderedDictionaryItemNode<string>> siblings;
                OrderedDictionaryItemNode<string> oldGroup = FindNode(oldNodeList, group.Id, null, out parentId,
                                                                      out siblings);
                if (oldGroup != null && oldGroup.IsGroup && oldGroup.Name == group.Name) continue;
                //
                HashSet<Guid> owners;
                bool otherGroup = groupOwners.TryGetValue(group.Name, out owners) &&
                                  (owners.Count > 1 || !owners.Contains(group.Id));
                Guid compoundGroupId;
                bool otherCompound = compoundPartGroups.TryGetValue(group.Name, out compoundGroupId) &&
                                     compoundGroupId != group.Id;
                if (otherCompound && !otherGroup)
                    throw new PythonAPIException("The group name '" + group.Name + "' is the name of the compound " +
                                                 "part of another part group, which takes the name of its group " +
                                                 "only after its group is renamed. Rename that group first, in " +
                                                 "a separate set_layout or with rename_group.");
                if (otherGroup || otherCompound || partNames.ContainsKey(group.Name))
                    throw new PythonAPIException("The group name '" + group.Name + "' is already the name of a " +
                                                 "part or of a part group. Uppercase and lowercase letters are " +
                                                 "regarded as equal.");
            }
        }
        private static void AddFinalGroupNames(IEnumerable<OrderedDictionaryItemNode<string>> nodes,
                                               Dictionary<Guid, string> renames,
                                               Dictionary<string, HashSet<Guid>> groupOwners)
        {
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup) continue;
                //
                string name;
                if (!renames.TryGetValue(node.Id, out name)) name = node.Name;
                AddOwner(groupOwners, name, node.Id);
                AddFinalGroupNames(node.Children, renames, groupOwners);
            }
        }
        private static void AddOwner(Dictionary<string, HashSet<Guid>> groupOwners, string name, Guid groupId)
        {
            HashSet<Guid> owners;
            if (!groupOwners.TryGetValue(name, out owners))
            {
                owners = new HashSet<Guid>();
                groupOwners.Add(name, owners);
            }
            owners.Add(groupId);
        }
        private static void AddPartNames(CaeModel.FeModel model, Dictionary<string, string> partNames,
                                         Dictionary<string, Guid> compoundPartGroups)
        {
            if (model.Geometry != null && model.Geometry.Parts != null)
            {
                foreach (KeyValuePair<string, CaeMesh.BasePart> entry in model.Geometry.Parts)
                {
                    CaeMesh.CompoundGeometryPart compoundPart = entry.Value as CaeMesh.CompoundGeometryPart;
                    if (compoundPart != null)
                    {
                        foreach (string subPartName in compoundPart.GetSplitSubPartNames())
                            partNames[subPartName] = subPartName;
                        if (compoundPart.LinkedGroupId != Guid.Empty)
                        {
                            compoundPartGroups[entry.Key] = compoundPart.LinkedGroupId;
                            continue;
                        }
                    }
                    partNames[entry.Key] = entry.Key;
                }
            }
            if (model.Mesh != null && model.Mesh.Parts != null)
            {
                foreach (string partName in model.Mesh.Parts.Keys) partNames[partName] = partName;
            }
        }
        // A group of the geometry parts and one of the mesh parts that carry the same id are one group shown in
        // both views, which rename_group renames together, so a layout that renames it renames it in the other
        // view as well
        private Dictionary<Guid, string> GetLinkedPartGroupRenames(List<TreeLayoutGroup> groups,
                                                                   IEnumerable<OrderedDictionaryItemNode<string>> oldNodes,
                                                                   string view)
        {
            Dictionary<Guid, string> renames = new Dictionary<Guid, string>();
            IOrderedDictionaryItemLayout<string> linkedLayout = GetLinkedPartLayout(view);
            if (linkedLayout == null) return renames;
            //
            List<OrderedDictionaryItemNode<string>> oldNodeList = new List<OrderedDictionaryItemNode<string>>(oldNodes);
            List<OrderedDictionaryItemNode<string>> linkedNodes =
                new List<OrderedDictionaryItemNode<string>>(linkedLayout.GetItemLayoutSnapshot().Nodes);
            foreach (TreeLayoutGroup group in groups)
            {
                Guid? parentId;
                IList<OrderedDictionaryItemNode<string>> siblings;
                OrderedDictionaryItemNode<string> oldGroup = FindNode(oldNodeList, group.Id, null, out parentId,
                                                                      out siblings);
                if (oldGroup == null || !oldGroup.IsGroup || oldGroup.Name == group.Name) continue;
                OrderedDictionaryItemNode<string> linkedGroup = FindNode(linkedNodes, group.Id, null, out parentId,
                                                                         out siblings);
                if (linkedGroup == null || !linkedGroup.IsGroup || linkedGroup.Name == group.Name) continue;
                //
                renames.Add(group.Id, group.Name);
            }
            return renames;
        }
        private IOrderedDictionaryItemLayout<string> GetLinkedPartLayout(string view)
        {
            if (view == "results") return null;
            //
            return _modelResolver.ModelGateway.GetTreeLayout(GetLinkedPartView(view), "parts", null);
        }
        private static string GetLinkedPartView(string view)
        {
            return view == "geometry" ? "femodel" : "geometry";
        }
        // Two renamed groups may swap their names, so every one first takes a name of its own
        private static OrderedDictionaryItemLayout<string> CreateRenamedLayout(IOrderedDictionaryItemLayout<string> layout,
                                                                               Dictionary<Guid, string> renames)
        {
            OrderedDictionary<string, object> preview =
                new OrderedDictionary<string, object>("Linked part group layout", layout.Comparer);
            foreach (string key in layout.Keys) preview.Add(key, null);
            preview.SetItemLayout(layout.GetItemLayoutSnapshot());
            foreach (Guid groupId in renames.Keys) preview.RenameGroup(groupId, groupId.ToString());
            foreach (KeyValuePair<Guid, string> rename in renames) preview.RenameGroup(rename.Key, rename.Value);
            //
            return preview.GetItemLayoutSnapshot();
        }
        // The items not yet placed stay at the root behind the entries already placed there, so placing every
        // entry at its own index, a group's entries before the next one of its parent, rebuilds the layout
        private static void PlaceLayoutEntries(OrderedDictionary<string, object> preview, List<object> entries,
                                               Guid? parentGroupId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                string itemName = entries[i] as string;
                if (itemName != null)
                {
                    preview.MoveItems(new[] { itemName }, parentGroupId, i);
                    continue;
                }
                //
                TreeLayoutGroup group = (TreeLayoutGroup)entries[i];
                preview.CreateGroup(group.Id, group.Name, parentGroupId, i);
                PlaceLayoutEntries(preview, group.Entries, group.Id);
            }
        }
    }
}
