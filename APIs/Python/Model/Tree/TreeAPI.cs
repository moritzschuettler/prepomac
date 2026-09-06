// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace APIs.Python.Model
{
    // The grouping and ordering of the model tree. A group is identified by a GUID, which the
    // application stores with the layout, so a script that reproduces a recorded layout passes the
    // recorded id; a script that creates a group of its own lets this API generate one and returns it.
    public class TreeAPI : BaseAPI
    {
        // Variables
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
                   "Geometry, Model or Results.", Arguments = "view: str", Returns = "StringList")]
        public StringListAPI get_item_types(string view)
        {
            return new StringListAPI(GetItemTypes(view));
        }
        [PythonAPI("Creates a model tree group and optionally moves items into it. It returns the group id, " +
                   "which is generated when it is not given. A step owned collection also needs the name of " +
                   "the step that owns it.",
                   Arguments = "view: str, item_type: str, group_name: str, group_id: str = None, " +
                               "parent_group_id: str = None, target_index: int = 0, " +
                               "item_names: list[str] = None, step_name: str = None", Returns = "str",
                   Example = "group = pmx.model.tree.create_group('Model', 'parts', 'Housing')")]
        public string create_group(string view, string item_type, string group_name, string group_id = null,
                                   string parent_group_id = null, int target_index = 0, object item_names = null,
                                   string step_name = null)
        {
            Validate(view, item_type, step_name, group_name, target_index);
            string id = group_id == null ? Guid.NewGuid().ToString() : ValidateGroupId(group_id, "group_id");
            if (parent_group_id != null) ValidateGroupId(parent_group_id, "parent_group_id");
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("CreateGroup", view, item_type, step_name,
                                                         ToNames(item_names, "item_names"), null, id,
                                                         parent_group_id, target_index, group_name);
            return id;
        }
        [PythonAPI("Renames a model tree group.",
                   Arguments = "view: str, item_type: str, group_id: str, group_name: str, " +
                               "step_name: str = None", Returns = "None")]
        public void rename_group(string view, string item_type, string group_id, string group_name,
                                 string step_name = null)
        {
            Validate(view, item_type, step_name, group_name, 0);
            ValidateGroupId(group_id, "group_id");
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("RenameGroup", view, item_type, step_name, null, null,
                                                         group_id, null, -1, group_name);
        }
        [PythonAPI("Removes model tree groups. The items they hold stay in the tree.",
                   Arguments = "view: str, item_type: str, group_ids: list[str] or str, step_name: str = None",
                   Returns = "None")]
        public void remove_groups(string view, string item_type, object group_ids, string step_name = null)
        {
            Validate(view, item_type, step_name, null, 0);
            string[] ids = ToNames(group_ids, "group_ids");
            if (ids == null || ids.Length == 0)
                throw new PythonAPIException("The argument 'group_ids' must not be empty.");
            foreach (string id in ids) ValidateGroupId(id, "group_ids");
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("RemoveGroup", view, item_type, step_name, null, ids,
                                                         null, null, -1, null);
        }
        [PythonAPI("Moves model tree items into a group, or out of every group when no group is given.",
                   Arguments = "view: str, item_type: str, item_names: list[str] or str, " +
                               "target_group_id: str = None, target_index: int = 0, step_name: str = None",
                   Returns = "None")]
        public void move_items(string view, string item_type, object item_names, string target_group_id = null,
                               int target_index = 0, string step_name = null)
        {
            Validate(view, item_type, step_name, null, target_index);
            string[] names = ToNames(item_names, "item_names");
            if (names == null || names.Length == 0)
                throw new PythonAPIException("The argument 'item_names' must not be empty.");
            if (target_group_id != null) ValidateGroupId(target_group_id, "target_group_id");
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveItems", view, item_type, step_name, names, null,
                                                         null, target_group_id, target_index, null);
        }
        [PythonAPI("Moves a model tree group into another group, or to the top level when no group is given.",
                   Arguments = "view: str, item_type: str, group_id: str, target_group_id: str = None, " +
                               "target_index: int = 0, step_name: str = None", Returns = "None")]
        public void move_group(string view, string item_type, string group_id, string target_group_id = null,
                               int target_index = 0, string step_name = null)
        {
            Validate(view, item_type, step_name, null, target_index);
            ValidateGroupId(group_id, "group_id");
            if (target_group_id != null) ValidateGroupId(target_group_id, "target_group_id");
            //
            EnsureCommandsAvailable();
            _modelResolver.ModelGateway.ChangeTreeLayout("MoveGroup", view, item_type, step_name, null, null,
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
        private static string[] GetItemTypes(string view)
        {
            if (view == "Geometry") return _geometryItemTypes;
            if (view == "Results") return _resultItemTypes;
            if (view != "Model")
                throw new PythonAPIException("The argument 'view' must be Geometry, Model or Results.");
            //
            string[] modelItemTypes = new string[_modelItemTypes.Length + _stepItemTypes.Length];
            _modelItemTypes.CopyTo(modelItemTypes, 0);
            _stepItemTypes.CopyTo(modelItemTypes, _modelItemTypes.Length);
            //
            return modelItemTypes;
        }
        private static void Validate(string view, string itemType, string stepName, string groupName, int index)
        {
            string[] itemTypes = GetItemTypes(view);
            bool supported = false;
            foreach (string supportedType in itemTypes) supported |= supportedType == itemType;
            if (!supported)
                throw new PythonAPIException("The argument 'item_type' must be one of " +
                                             string.Join(", ", itemTypes) + " in the " + view + " view.");
            // Only a step owns a collection of its own, and it is named rather than derived from the view
            bool ownedByStep = false;
            foreach (string stepItemType in _stepItemTypes) ownedByStep |= stepItemType == itemType;
            if (ownedByStep && (stepName == null || stepName.Trim().Length == 0))
                throw new PythonAPIException("The argument 'step_name' must name the step that owns the " +
                                             itemType + " collection.");
            if (!ownedByStep && stepName != null)
                throw new PythonAPIException("The argument 'step_name' is only used by a step owned " +
                                             "collection, and the " + itemType + " collection is not one.");
            if (groupName != null && groupName.Trim().Length == 0)
                throw new PythonAPIException("The argument 'group_name' must not be empty.");
            if (index < 0) throw new PythonAPIException("The argument 'target_index' must not be negative.");
        }
        private static string ValidateGroupId(string value, string argumentName)
        {
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
                throw new PythonAPIException("The argument '" + argumentName + "' must be a group id.");
            //
            return value;
        }
        private static string[] ToNames(object value, string argumentName)
        {
            if (value == null) return null;
            //
            return PythonStringListConverter.ToStringArrayOrSingle(value, argumentName);
        }
    }
}
