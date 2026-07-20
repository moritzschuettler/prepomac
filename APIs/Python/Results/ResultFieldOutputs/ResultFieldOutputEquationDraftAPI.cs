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
    public class ResultFieldOutputEquationDraftAPI : ResultFieldOutputDraftAPI
    {
        // Variables
        private readonly string _equation;
        private readonly string _unit;


        // Properties
        [PythonAPI("The field equation. Field and component references are case-sensitive.", Returns = "str")]
        public string equation { get { return _equation; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return _unit; } }
        [PythonAPI("The generated component name.", Returns = "str")]
        public string component_name { get { return ResultFieldOutputEquation.ComponentName; } }


        // Constructors
        internal ResultFieldOutputEquationDraftAPI(string name, string equation, string unit)
            : base(name)
        {
            _equation = equation;
            _unit = unit;
        }


        // Methods
        protected override ResultFieldOutput CreateFieldOutput()
        {
            ResultFieldOutputEquation fieldOutput = new ResultFieldOutputEquation(name, _equation);
            fieldOutput.Unit = _unit;
            return fieldOutput;
        }
        public override string ToString()
        {
            return "FieldOutputEquationDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", equation=" + StringListAPI.ToDisplayString(_equation) +
                   ", unit=" + StringListAPI.ToDisplayString(_unit) + ")";
        }
    }
}
