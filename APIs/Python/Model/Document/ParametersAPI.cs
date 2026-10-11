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
        [PythonAPI("Adds a model parameter using command history. The value is a number or an equation string; " +
                   "one that cannot be evaluated, or uses its own parameter, is refused.",
                   Arguments = "name: str, value: float or str, description: str = None", Returns = "Parameter",
                   Example = "pmx.model.parameters.add('t', 2.5, description='Plate thickness')")]
        public ParameterAPI add(object name, object value, object description = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            string descriptionText = PythonValueConverter.ToText(description, "description", true);
            _service.Add(nameText, value, descriptionText);
            return this[nameText];
        }

        [PythonAPI("Replaces a model parameter using command history. A value that is not given is kept. A " +
                   "rename or a value that would leave another parameter unable to be evaluated is refused, and " +
                   "so is a remove of a parameter that another one uses; set_all changes them together.",
                   Arguments = "old_name: str, name: str = None, value: float or str = None, " +
                               "description: str = None",
                   Returns = "Parameter")]
        public ParameterAPI replace(object old_name, object name = null, object value = null,
                                    object description = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string descriptionText = PythonValueConverter.ToText(description, "description", true);
            _service.Replace(oldName, nameText, value, descriptionText);
            return this[nameText == null ? oldName : nameText];
        }

        [PythonAPI("Replaces the whole parameter table using command history. The names, the values and the " +
                   "descriptions are given in the order the parameters are shown, and a parameter may use a " +
                   "parameter that is given after it. A parameter with no description takes an empty string.",
                   Arguments = "names: list[str], values: list[float or str], descriptions: list[str] = None",
                   Returns = "None",
                   Example = "pmx.model.parameters.set_all(['t', 'h'], [2.5, 't*2'])")]
        public void set_all(object names, object values, object descriptions = null)
        {
            _service.SetAll(names, values, descriptions);
        }

        [PythonAPI("Imports the parameters from a .pmp file using command history. A file with a line the import " +
                   "cannot read or evaluate is refused naming the file and the line, before anything is recorded.",
                   Arguments = "file_name: str", Returns = "None")]
        public void import_from_file(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _service.ImportFromFile(fileName);
        }

        [PythonAPI("Exports the parameters to a .pmp file using command history.",
                   Arguments = "file_name: str", Returns = "None")]
        public void export_to_file(object file_name)
        {
            string fileName = PythonValueConverter.ToText(file_name, "file_name", false);
            _service.ExportToFile(fileName);
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
