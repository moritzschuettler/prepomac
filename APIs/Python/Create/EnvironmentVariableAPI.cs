// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using System.Collections.Generic;

namespace APIs.Python
{
    // A solver environment variable, made by pmx.create.environment_variable or read back from an analysis
    public class EnvironmentVariableAPI : DetachedPartAPI
    {
        // Variables
        private readonly EnvironmentVariable _variable;


        // Properties
        [PythonAPI("The name of the environment variable.", Returns = "str")]
        public string name { get { return _variable.Name; } }
        [PythonAPI("The value of the environment variable.", Returns = "str")]
        public string value { get { return _variable.Value; } }
        [PythonAPI("True when the solver is given the environment variable.", Returns = "bool")]
        public bool active { get { return _variable.Active; } }


        // Constructors
        internal EnvironmentVariableAPI(EnvironmentVariable variable)
        {
            _variable = variable;
        }


        // Methods
        internal EnvironmentVariable GetVariableCopy()
        {
            return _variable.DeepClone();
        }
        protected override string GetTypeName()
        {
            return "EnvironmentVariable";
        }
        protected override string GetMakerName()
        {
            return "environment_variable";
        }
        protected override Dictionary<string, object> GetValues()
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            values["name"] = _variable.Name;
            values["value"] = _variable.Value;
            values["active"] = _variable.Active;
            //
            return values;
        }
    }
}
