// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class InitialConditionAPI : BaseAPI
    {
        // Variables
        private readonly string _initialConditionName;
        private readonly InitialConditionService _service;


        // Properties
        [PythonAPI("The initial condition name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The initial condition type, for example InitialTemperature.", Returns = "str")]
        public string condition_type { get { return Resolve().GetType().Name; } }
        [PythonAPI("The name of the region the initial condition is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The region type the initial condition is applied to.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("True when the initial condition is visible.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the initial condition resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal InitialConditionAPI(string initialConditionName, InitialConditionService service)
        {
            _initialConditionName = initialConditionName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns stored component equations without evaluating them.", Returns = "dict[str, str]")]
        public System.Collections.Generic.Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
        }
        public override string ToString()
        {
            InitialCondition initialCondition = Resolve();
            return "InitialCondition(name=" + StringListAPI.ToDisplayString(initialCondition.Name) +
                   ", type=" + initialCondition.GetType().Name + ")";
        }
        //
        private InitialCondition Resolve()
        {
            return _service.ResolveInitialCondition(_initialConditionName);
        }
    }
}
