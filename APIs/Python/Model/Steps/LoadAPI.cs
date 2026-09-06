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
    public class LoadAPI : BaseAPI
    {
        // Variables
        private readonly string _stepName;
        private readonly string _loadName;
        private readonly LoadService _service;


        // Properties
        [PythonAPI("The load name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the load belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The load type, for example CLoad or DLoad.", Returns = "str")]
        public string load_type { get { return Resolve().GetType().Name; } }
        [PythonAPI("True when the load is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the load symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("The region type, for example NodeSetName or SurfaceName.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the region the load is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The amplitude name.", Returns = "str")]
        public string amplitude_name { get { return Resolve().AmplitudeName; } }
        [PythonAPI("The coordinate system name.", Returns = "str")]
        public string coordinate_system_name { get { return Resolve().CoordinateSystemName; } }
        [PythonAPI("The red, green, blue and alpha channels of the symbol color.", Returns = "IntList")]
        public IntListAPI color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }


        // Constructors
        internal LoadAPI(string stepName, string loadName, LoadService service)
        {
            _stepName = stepName;
            _loadName = loadName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this load type.",
                   Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(Resolve(), typeof(Load)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this load type, for example f1 or magnitude. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            return ModelItemValueReader.GetValue(Resolve(), typeof(Load), name, "load");
        }
        [PythonAPI("Returns stored component equations without evaluating them.", Returns = "dict[str, str]")]
        public System.Collections.Generic.Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
        }
        public override string ToString()
        {
            Load load = Resolve();
            return "Load(name=" + StringListAPI.ToDisplayString(load.Name) + ", type=" + load.GetType().Name +
                   ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private Load Resolve()
        {
            return _service.ResolveItem(_stepName, _loadName);
        }
    }
}
