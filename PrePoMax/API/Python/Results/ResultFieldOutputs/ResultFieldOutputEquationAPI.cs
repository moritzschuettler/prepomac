// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeResults;

namespace PrePoMax.API.Python
{
    public class ResultFieldOutputEquationAPI : ResultFieldOutputAPI
    {
        // Properties
        [PythonAPI("The field equation. Field and component references are case-sensitive.", Returns = "str")]
        public string equation { get { return ResolveEquation().Equation; } }
        [PythonAPI("The user-defined result unit.", Returns = "str")]
        public string unit { get { return ResolveEquation().Unit; } }
        [PythonAPI("The generated component name.", Returns = "str")]
        public string component_name { get { return ResultFieldOutputEquation.ComponentName; } }


        // Constructors
        internal ResultFieldOutputEquationAPI(string resultName, string fieldOutputName,
                                              ResultFieldOutputService service)
            : base(resultName, fieldOutputName, service)
        {
        }


        // Methods
        private ResultFieldOutputEquation ResolveEquation()
        {
            ResultFieldOutputEquation fieldOutput = Resolve() as ResultFieldOutputEquation;
            if (fieldOutput == null)
                throw new PythonAPIException("The field output '" + _fieldOutputName +
                                             "' is not an equation field output.");
            return fieldOutput;
        }
        public override string ToString()
        {
            ResultFieldOutputEquation fieldOutput = ResolveEquation();
            return "FieldOutputEquation(name=" + StringListAPI.ToDisplayString(_fieldOutputName) +
                   ", equation=" + StringListAPI.ToDisplayString(fieldOutput.Equation) +
                   ", unit=" + StringListAPI.ToDisplayString(fieldOutput.Unit) + ")";
        }
    }
}
