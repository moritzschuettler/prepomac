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

namespace PrePoMax.API.Python
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
        [PythonAPI("How limits are assigned. Possible values are 'parts', 'element_sets', and 'all_elements'.",
                   Returns = "str")]
        public string based_on
        {
            get
            {
                if (_basedOn == LimitPlotBasedOnEnum.Parts) return "parts";
                if (_basedOn == LimitPlotBasedOnEnum.ElementSets) return "element_sets";
                if (_basedOn == LimitPlotBasedOnEnum.AllElements) return "all_elements";
                throw new PythonAPIException("Unsupported limit assignment type '" + _basedOn + "'.");
            }
        }
        [PythonAPI("Limit values indexed by part, element set, or all-elements name.",
                   Returns = "StringDoubleMap")]
        public StringDoubleMapAPI item_limits { get { return new StringDoubleMapAPI(_itemLimits); } }


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
