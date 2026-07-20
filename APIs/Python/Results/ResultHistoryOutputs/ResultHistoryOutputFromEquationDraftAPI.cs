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
    public class ResultHistoryOutputFromEquationDraftAPI : ResultHistoryOutputDraftAPI
    {
        // Variables
        private readonly string _equation;
        private string _unit;


        // Properties
        [PythonAPI("The history output equation.", Returns = "str")]
        public string equation { get { return _equation; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return _unit; } set { _unit = value; } }


        // Constructors
        internal ResultHistoryOutputFromEquationDraftAPI(string name, string equation, string unit)
            : base(name, null)
        {
            _equation = equation;
            _unit = unit;
        }


        // Methods
        protected override ResultHistoryOutput CreateHistoryOutput()
        {
            ResultHistoryOutputFromEquation output = new ResultHistoryOutputFromEquation(name, _equation);
            output.Unit = _unit;
            return output;
        }
        protected override string GetRequiredTarget()
        {
            return null;
        }
        public override string ToString()
        {
            return "HistoryOutputFromEquationDraft(name=" + StringListAPI.ToDisplayString(name) +
                   ", equation=" + StringListAPI.ToDisplayString(_equation) + ")";
        }
    }
}
