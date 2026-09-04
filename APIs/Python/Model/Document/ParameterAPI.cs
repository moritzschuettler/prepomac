// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace APIs.Python.Model
{
    public class ParameterAPI : BaseAPI
    {
        // Variables
        private readonly string _parameterName;
        private readonly ParameterService _service;


        // Properties
        [PythonAPI("The parameter name.", Returns = "str")]
        public string name { get { return _service.ResolveParameter(_parameterName).Name; } }
        [PythonAPI("The parameter value or equation as it was entered.", Returns = "str")]
        public string equation { get { return _service.ResolveParameter(_parameterName).EquationStr.Equation; } }
        [PythonAPI("The evaluated parameter value.", Returns = "float")]
        public double value { get { return _service.ResolveParameter(_parameterName).Value; } }


        // Constructors
        internal ParameterAPI(string parameterName, ParameterService service)
        {
            _parameterName = parameterName;
            _service = service;
        }


        // Methods
        public override string ToString()
        {
            return "Parameter(name=" + StringListAPI.ToDisplayString(_parameterName) +
                   ", equation=" + StringListAPI.ToDisplayString(equation) + ", value=" + value + ")";
        }
    }
}
