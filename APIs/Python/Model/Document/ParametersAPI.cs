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
    public class ParametersAPI : ModelItemsAPI<EquationParameter, ParameterAPI>
    {
        // Variables
        private readonly ParameterService _service;


        // Constructors
        internal ParametersAPI(ParameterService service)
            : base("parameter")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a model parameter using command history. The value is a number or an equation string.",
                   Arguments = "name: str, value: float or str", Returns = "Parameter",
                   Example = "pmx.model.parameters.add('t', 2.5)")]
        public ParameterAPI add(string name, object value)
        {
            _service.Add(name, value);
            return this[name];
        }

        [PythonAPI("Replaces a model parameter using command history.",
                   Arguments = "old_name: str, name: str, value: float or str", Returns = "Parameter")]
        public ParameterAPI replace(string old_name, string name, object value)
        {
            _service.Replace(old_name, name, value);
            return this[name];
        }

        [PythonAPI("Imports the parameters from a .pmp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void import_from_file(string file_name)
        {
            _service.ImportFromFile(file_name);
        }

        [PythonAPI("Exports the parameters to a .pmp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_file(string file_name)
        {
            _service.ExportToFile(file_name);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override EquationParameter GetItem(string name)
        {
            return _service.GetParameter(name);
        }
        protected override ParameterAPI CreateItemAPI(string name)
        {
            return new ParameterAPI(name, _service);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(EquationParameter item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Parameters(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
    }
}
