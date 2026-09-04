// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;

namespace APIs.Python.Model
{
    // A defined temperature is either given by value on a region, or read from a results file for the
    // whole model. The two forms are two add methods, so the type is derived from the one that is used.
    public class DefinedFieldsAPI : StepItemsAPI<DefinedField, DefinedFieldAPI>
    {
        // Variables
        private readonly DefinedFieldService _service;


        // Constructors
        internal DefinedFieldsAPI(string stepName, DefinedFieldService service)
            : base("defined field", stepName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a defined temperature using command history. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, temperature: float, " +
                               "distribution_name: str = None, amplitude_name: str = None, " +
                               "color: list[int] = None", Returns = "DefinedField",
                   Example = "step.defined_fields.add_temperature('DF-1', selection, 100)")]
        public DefinedFieldAPI add_temperature(string name, object region, object temperature,
                                               object distribution_name = null, object amplitude_name = null,
                                               object color = null)
        {
            _service.ValidateName(StepName, name, null);
            // The constructor takes a plain value, while the temperature can also be an equation
            DefinedTemperature definedField =
                new DefinedTemperature(name, null, RegionTypeEnum.Selection, 0);
            definedField.Type = DefinedTemperatureTypeEnum.ByValue;
            definedField.Temperature.Equation =
                PythonValueConverter.ToEquationString(temperature, "temperature");
            _service.ApplyRegion(definedField, region, "nodes");
            if (distribution_name != null)
                definedField.DistributionName = _service.ResolveDistributionName(distribution_name);
            //
            return Add(definedField, amplitude_name, color);
        }

        [PythonAPI("Replaces an existing defined temperature using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_temperature with " +
                               "region and temperature optional", Returns = "DefinedField")]
        public DefinedFieldAPI replace_temperature(string old_name, string name = null, object region = null,
                                                   object temperature = null, object distribution_name = null,
                                                   object amplitude_name = null, object color = null)
        {
            DefinedTemperature definedField =
                _service.CloneForReplace<DefinedTemperature>(StepName, old_name, "DefinedTemperature");
            if (temperature != null)
                definedField.Temperature.Equation =
                    PythonValueConverter.ToEquationString(temperature, "temperature");
            if (region != null) _service.ApplyRegion(definedField, region, "nodes");
            if (distribution_name != null)
                definedField.DistributionName = _service.ResolveDistributionName(distribution_name);
            //
            return Replace(old_name, name, definedField, amplitude_name, color);
        }

        [PythonAPI("Adds a defined temperature read from a results file using command history. It covers " +
                   "the whole model, so it takes no region.",
                   Arguments = "name: str, file_name: str, step_number: int = None, " +
                               "amplitude_name: str = None, color: list[int] = None",
                   Returns = "DefinedField")]
        public DefinedFieldAPI add_temperature_from_file(string name, object file_name, object step_number = null,
                                                         object amplitude_name = null, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            DefinedTemperature definedField =
                new DefinedTemperature(name, null, RegionTypeEnum.Selection, 0);
            definedField.Type = DefinedTemperatureTypeEnum.FromFile;
            definedField.FileName = _service.ResolveExistingFileName(file_name, "file_name");
            if (step_number != null)
                definedField.StepNumber = PythonValueConverter.ToInt(step_number, "step_number");
            //
            return Add(definedField, amplitude_name, color);
        }

        [PythonAPI("Replaces an existing defined temperature read from a results file using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_temperature_from_file with file_name optional", Returns = "DefinedField")]
        public DefinedFieldAPI replace_temperature_from_file(string old_name, string name = null,
                                                             object file_name = null, object step_number = null,
                                                             object amplitude_name = null, object color = null)
        {
            DefinedTemperature definedField =
                _service.CloneForReplace<DefinedTemperature>(StepName, old_name, "DefinedTemperature");
            if (definedField.Type != DefinedTemperatureTypeEnum.FromFile)
                throw new PythonAPIException("The defined field '" + old_name + "' is given by value, so it " +
                                             "cannot be replaced by one read from a file. Use " +
                                             "replace_temperature instead.");
            if (file_name != null)
                definedField.FileName = _service.ResolveExistingFileName(file_name, "file_name");
            if (step_number != null)
                definedField.StepNumber = PythonValueConverter.ToInt(step_number, "step_number");
            //
            return Replace(old_name, name, definedField, amplitude_name, color);
        }

        [PythonAPI("Copies the defined field to every following step using command history.",
                   Arguments = "name: str", Returns = "None")]
        public void propagate(string name)
        {
            _service.Propagate(StepName, name);
        }

        [PythonAPI("Hides the symbols of the named defined fields using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(StepName, ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the symbols of the named defined fields using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(StepName, ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames(StepName);
        }
        protected override DefinedField GetItem(string name)
        {
            return _service.GetItem(StepName, name);
        }
        protected override DefinedFieldAPI CreateItemAPI(string name)
        {
            return new DefinedFieldAPI(StepName, name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(StepName, names);
        }
        protected override void SetItemsActive(DefinedField[] items, bool active)
        {
            _service.SetActive(StepName, items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(StepName, names);
        }
        protected override void RenameItem(DefinedField item, string newName)
        {
            _service.Rename(StepName, item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "DefinedFields(step=" + StringListAPI.ToDisplayString(StepName) + ", count=" + names.Length +
                   ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private DefinedFieldAPI Add(DefinedField definedField, object amplitudeName, object color)
        {
            _service.ApplyCommonValues(definedField, amplitudeName, color);
            _service.Add(StepName, definedField);
            //
            return this[definedField.Name];
        }
        private DefinedFieldAPI Replace(string oldName, string name, DefinedField definedField,
                                        object amplitudeName, object color)
        {
            _service.ApplyCommonValues(definedField, amplitudeName, color);
            if (name != null) definedField.Name = name;
            _service.Replace(StepName, oldName, definedField);
            //
            return this[definedField.Name];
        }
    }
}
