// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using CaeGlobals;

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
        public StringListAPI get_item_types(string view)
        {
            return new StringListAPI(GetItemTypes(ToView(view)));
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
        public string add_group(string view, string item_type, string group_name, string group_id = null,
                                string parent_group_id = null, int target_index = 0, object item_names = null,
                                string step_name = null)
        {
            item_type = ToItemType(view, item_type);
            string viewName = Validate(view, item_type, step_name, group_name, target_index);
            step_name = ToStepName(viewName, item_type, step_name);
            ValidateGroupName(group_name);
            string id = group_id == null ? Guid.NewGuid().ToString() : group_id;
            Guid groupId = ValidateGroupId(id, "group_id");
            Guid? parentGroupId = null;
            if (parent_group_id != null) parentGroupId = ValidateGroupId(parent_group_id, "parent_group_id");
            string[] names = ToNames(item_names, "item_names");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, item_type, step_name);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            Guid? existingParentId;
            IList<OrderedDictionaryItemNode<string>> existingSiblings;
            if (FindNode(nodes, groupId, null, out existingParentId, out existingSiblings) != null)
                throw new PythonAPIException("The group id '" + id + "' already exists in the " + item_type +
                                             " tree of the " + viewName + " view.");
            IList<OrderedDictionaryItemNode<string>> siblings =
                GetGroupChildren(nodes, parentGroupId, "parent_group_id", viewName, item_type);
            CheckTargetIndex(target_index, siblings.Count);
            CheckSiblingGroupName(siblings, group_name, Guid.Empty, item_type);
            CheckPartGroupName(viewName, item_type, groupId, group_name);
            if (names != null) CheckItemNames(layout, names, "item_names", viewName, item_type);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("CreateGroup", viewName, item_type, step_name, names, null,
                                                         id, parent_group_id, target_index, group_name);
            return id;
        }
        [PythonAPI("Renames a model tree group. The view is 'geometry', 'femodel' or 'results'. A group name " +
                   "follows the rules of a model item name.",
                   Arguments = "view: str, item_type: str, group_id: str, group_name: str, " +
                               "step_name: str = None", Returns = "None")]
        public void rename_group(string view, string item_type, string group_id, string group_name,
                                 string step_name = null)
        {
            item_type = ToItemType(view, item_type);
            string viewName = Validate(view, item_type, step_name, group_name, 0);
            step_name = ToStepName(viewName, item_type, step_name);
            ValidateGroupName(group_name);
            Guid groupId = ValidateGroupId(group_id, "group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, item_type, step_name);
            Guid? parentId;
            IList<OrderedDictionaryItemNode<string>> siblings;
            ResolveGroup(layout.GetItemLayoutSnapshot().Nodes, groupId, "group_id", viewName, item_type,
                         out parentId, out siblings);
            CheckSiblingGroupName(siblings, group_name, groupId, item_type);
            CheckPartGroupName(viewName, item_type, groupId, group_name);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("RenameGroup", viewName, item_type, step_name, null, null,
                                                         group_id, null, -1, group_name);
        }
        [PythonAPI("Removes model tree groups. The items they hold stay in the tree. The view is 'geometry', " +
                   "'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, group_ids: list[str] or str, step_name: str = None",
                   Returns = "None")]
        public void remove_groups(string view, string item_type, object group_ids, string step_name = null)
        {
            item_type = ToItemType(view, item_type);
            string viewName = Validate(view, item_type, step_name, null, 0);
            step_name = ToStepName(viewName, item_type, step_name);
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
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, item_type, step_name);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            foreach (string id in ids)
            {
                Guid groupId = Guid.Parse(id);
                Guid? parentId;
                IList<OrderedDictionaryItemNode<string>> siblings;
                OrderedDictionaryItemNode<string> group = ResolveGroup(nodes, groupId, "group_ids", viewName,
                                                                       item_type, out parentId, out siblings);
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
            _modelResolver.ModelGateway.ChangeTreeLayout("RemoveGroup", viewName, item_type, step_name, null, ids,
                                                         null, null, -1, null);
        }
        [PythonAPI("Moves model tree items into a group, or out of every group when no group is given. The view " +
                   "is 'geometry', 'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, item_names: list[str] or str, " +
                               "target_group_id: str = None, target_index: int = 0, step_name: str = None",
                   Returns = "None")]
        public void move_items(string view, string item_type, object item_names, string target_group_id = null,
                               int target_index = 0, string step_name = null)
        {
            item_type = ToItemType(view, item_type);
            string viewName = Validate(view, item_type, step_name, null, target_index);
            step_name = ToStepName(viewName, item_type, step_name);
            string[] names = ToNames(item_names, "item_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'item_names' must not be empty.");
            Guid? targetGroupId = null;
            if (target_group_id != null) targetGroupId = ValidateGroupId(target_group_id, "target_group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, item_type, step_name);
            CheckItemNames(layout, names, "item_names", viewName, item_type);
            IList<OrderedDictionaryItemNode<string>> targetChildren =
                GetGroupChildren(layout.GetItemLayoutSnapshot().Nodes, targetGroupId, "target_group_id", viewName,
                                 item_type);
            // The index counts the target's entries after the moved items have left it
            HashSet<string> movedNames = new HashSet<string>(names, layout.Comparer);
            int count = 0;
            foreach (OrderedDictionaryItemNode<string> child in targetChildren)
            {
                if (!child.IsItem || !movedNames.Contains(child.Key)) count++;
            }
            CheckTargetIndex(target_index, count);
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveItems", viewName, item_type, step_name, names, null,
                                                         null, target_group_id, target_index, null);
        }
        [PythonAPI("Moves a model tree group into another group, or to the top level when no group is given. The " +
                   "view is 'geometry', 'femodel' or 'results'.",
                   Arguments = "view: str, item_type: str, group_id: str, target_group_id: str = None, " +
                               "target_index: int = 0, step_name: str = None", Returns = "None")]
        public void move_group(string view, string item_type, string group_id, string target_group_id = null,
                               int target_index = 0, string step_name = null)
        {
            item_type = ToItemType(view, item_type);
            string viewName = Validate(view, item_type, step_name, null, target_index);
            step_name = ToStepName(viewName, item_type, step_name);
            Guid groupId = ValidateGroupId(group_id, "group_id");
            Guid? targetGroupId = null;
            if (target_group_id != null) targetGroupId = ValidateGroupId(target_group_id, "target_group_id");
            //
            IOrderedDictionaryItemLayout<string> layout = ResolveLayout(viewName, item_type, step_name);
            IList<OrderedDictionaryItemNode<string>> nodes = layout.GetItemLayoutSnapshot().Nodes;
            Guid? parentId;
            IList<OrderedDictionaryItemNode<string>> siblings;
            OrderedDictionaryItemNode<string> group = ResolveGroup(nodes, groupId, "group_id", viewName, item_type,
                                                                   out parentId, out siblings);
            Guid? nestedParentId;
            IList<OrderedDictionaryItemNode<string>> nestedSiblings;
            if (targetGroupId.HasValue &&
                (targetGroupId.Value == groupId ||
                 FindNode(group.Children, targetGroupId.Value, groupId, out nestedParentId, out nestedSiblings) != null))
                throw new PythonAPIException("The group '" + group.Name + "' cannot be moved into itself or into one " +
                                             "of its own groups.");
            IList<OrderedDictionaryItemNode<string>> targetChildren =
                GetGroupChildren(nodes, targetGroupId, "target_group_id", viewName, item_type);
            CheckSiblingGroupName(targetChildren, group.Name, groupId, item_type);
            // The index counts the target's entries after the moved group has left it
            bool sameParent = parentId.HasValue == targetGroupId.HasValue &&
                              (!targetGroupId.HasValue || parentId.Value == targetGroupId.Value);
            CheckTargetIndex(target_index, targetChildren.Count - (sameParent ? 1 : 0));
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveGroup", viewName, item_type, step_name, null, null,
                                                         group_id, target_group_id, target_index, null);
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
        private static void CheckItemNames(IOrderedDictionaryItemLayout<string> layout, string[] names,
                                           string argumentName, string view, string itemType)
        {
            HashSet<string> givenNames = new HashSet<string>(layout.Comparer);
            foreach (string name in names)
            {
                if (!layout.ContainsKey(name))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + name + "', which " +
                                                 "does not exist in the " + itemType + " collection of the " +
                                                 view + " view.");
                if (!givenNames.Add(name))
                    throw new PythonAPIException("The argument '" + argumentName + "' names '" + name +
                                                 "' more than once.");
            }
        }
        private static string[] ToNames(object value, string argumentName)
        {
            if (value == null) return null;
            //
            return PythonStringListConverter.ToStringArrayOrSingle(value, argumentName);
        }
    }
}
