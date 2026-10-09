// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeResults;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultFieldOutputLimitDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _fieldName;
        private readonly string _componentName;
        private readonly LimitPlotBasedOnEnum _basedOn;
        private readonly OrderedDictionary<string, double> _itemLimits;


        // Properties
        [PythonAPI("The source field name.", Returns = "str")]
        public string field_name { get { return _fieldName; } }
        [PythonAPI("The source field component name.", Returns = "str")]
        public string component_name { get { return _componentName; } }
        [PythonAPI("How limits are assigned: Parts, ElementSets or AllElements.", Returns = "str")]
        public string based_on { get { return _basedOn.ToString(); } }
        [PythonAPI("Limit values keyed by part, element set, or all-elements name; the dict goes back into " +
                   "item_limits of add_limit.", Returns = "dict[str, float]")]
        public Dictionary<string, double> item_limits
        {
            get { return ResultFieldOutputLimitAPI.ToDictionary(_itemLimits); }
        }


        // Constructors
        internal ResultFieldOutputLimitDraftAPI(string name, string fieldName, string componentName,
                                                LimitPlotBasedOnEnum basedOn,
                                                OrderedDictionary<string, double> itemLimits)
            : base(name)
        {
            _fieldName = fieldName;
            _componentName = componentName;
            _basedOn = basedOn;
            _itemLimits = itemLimits;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            ResultFieldOutputLimit fieldOutput = new ResultFieldOutputLimit(name, _fieldName, _componentName);
            fieldOutput.LimitPlotBasedOn = _basedOn;
            foreach (var entry in _itemLimits) fieldOutput.ItemNameLimit.Add(entry.Key, entry.Value);
            return fieldOutput;
        }
        public override string ToString()
        {
            return "FieldOutputLimitDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", field_name=" + StringListAPI.ToDisplayString(_fieldName) +
                   ", component_name=" + StringListAPI.ToDisplayString(_componentName) +
                   ", based_on=" + StringListAPI.ToDisplayString(based_on) +
                   ", items=" + _itemLimits.Count + ")";
        }
    }
}
