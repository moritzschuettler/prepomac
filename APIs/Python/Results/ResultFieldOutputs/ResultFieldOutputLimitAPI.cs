// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace APIs.Python
{
    public class ResultFieldOutputLimitAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source field name.", Returns = "str")]
        public string field_name { get { return ResolveLimit().FieldName; } }
        [PythonAPI("The source field component name.", Returns = "str")]
        public string component_name { get { return ResolveLimit().ComponentName; } }
        [PythonAPI("How limits are assigned. Possible values are 'parts', 'element_sets', and 'all_elements'.",
                   Returns = "str")]
        public string based_on
        {
            get
            {
                LimitPlotBasedOnEnum basedOn = ResolveLimit().LimitPlotBasedOn;
                if (basedOn == LimitPlotBasedOnEnum.Parts) return "parts";
                if (basedOn == LimitPlotBasedOnEnum.ElementSets) return "element_sets";
                if (basedOn == LimitPlotBasedOnEnum.AllElements) return "all_elements";
                throw new PythonAPIException("Unsupported limit assignment type '" + basedOn + "'.");
            }
        }
        [PythonAPI("Limit values indexed by part or element set name.", Returns = "StringDoubleMap")]
        public StringDoubleMapAPI item_limits { get { return new StringDoubleMapAPI(ResolveLimit().ItemNameLimit); } }


        // Constructors
        internal ResultFieldOutputLimitAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputLimit ResolveLimit()
        {
            ResultFieldOutputLimit fieldOutput = Resolve() as ResultFieldOutputLimit;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName + "' is not a limit field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputLimit fieldOutput = ResolveLimit();
            return "FieldOutputLimit(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", field_name=" + StringListAPI.ToDisplayString(fieldOutput.FieldName) +
                   ", component_name=" + StringListAPI.ToDisplayString(fieldOutput.ComponentName) +
                   ", based_on=" + StringListAPI.ToDisplayString(based_on) +
                   ", items=" + fieldOutput.ItemNameLimit.Count + ")";
        }
    }
}
