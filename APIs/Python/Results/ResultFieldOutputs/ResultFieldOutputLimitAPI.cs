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
    public class ResultFieldOutputLimitAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The source field name.", Returns = "str")]
        public string field_name { get { return ResolveLimit().FieldName; } }
        [PythonAPI("The source field component name.", Returns = "str")]
        public string component_name { get { return ResolveLimit().ComponentName; } }
        [PythonAPI("How limits are assigned: Parts, ElementSets or AllElements.", Returns = "str")]
        public string based_on { get { return ResolveLimit().LimitPlotBasedOn.ToString(); } }
        [PythonAPI("Limit values keyed by part or element set name; the dict goes back into item_limits of add_limit.",
                   Returns = "dict[str, float]")]
        public Dictionary<string, double> item_limits { get { return ToDictionary(ResolveLimit().ItemNameLimit); } }


        // Constructors
        internal ResultFieldOutputLimitAPI(string resultName, string fieldOutputName, ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        internal static Dictionary<string, double> ToDictionary(OrderedDictionary<string, double> itemLimits)
        {
            Dictionary<string, double> limits = new Dictionary<string, double>();
            if (itemLimits == null) return limits;
            //
            foreach (var entry in itemLimits) limits.Add(entry.Key, entry.Value);
            return limits;
        }
        private ResultFieldOutputLimit ResolveLimit()
        {
            ResultFieldOutputLimit fieldOutput = Resolve() as ResultFieldOutputLimit;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName +
                                             "' is not a limit field output.");
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
