// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class DefinedFieldAPI : BaseAPI
    {
        // Variables
        private readonly string _stepName;
        private readonly string _definedFieldName;
        private readonly DefinedFieldService _service;


        // Properties
        [PythonAPI("The defined field name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the defined field belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The defined field type, named after the add_* method that makes it without the add_ " +
                   "prefix: temperature or temperature_from_file.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the defined field is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the defined field symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the defined field resolved during the last regeneration: its region, " +
                   "distribution and equations.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }
        [PythonAPI("The region type, for example NodeSetName.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the region the defined field is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The amplitude name.", Returns = "str")]
        public string amplitude_name { get { return Resolve().AmplitudeName; } }
        [PythonAPI("The symbol color as a list [R, G, B, A] of channel values, which the color argument takes " +
                   "back.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }


        // Constructors
        internal DefinedFieldAPI(string stepName, string definedFieldName, DefinedFieldService service)
        {
            _stepName = stepName;
            _definedFieldName = definedFieldName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(DefinedField definedField)
        {
            // One model type serves both methods, told apart by where the temperature comes from
            DefinedTemperature definedTemperature = definedField as DefinedTemperature;
            if (definedTemperature != null && definedTemperature.GetType() == typeof(DefinedTemperature))
                return definedTemperature.Type == DefinedTemperatureTypeEnum.FromFile ? "temperature_from_file" :
                                                                                         "temperature";
            //
            throw new PythonAPIException("The defined field '" + definedField.Name + "' is a " +
                                         definedField.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }
        // The arguments of the replace_* method of the defined field type, each with the stored value it sets
        private static Dictionary<string, string[]> GetArgumentValueNames(DefinedField definedField)
        {
            if (GetItemType(definedField) == "temperature_from_file")
                return ModelItemValueReader.Arguments("file_name", "step_id");
            //
            return ModelItemValueReader.Arguments("temperature", "distribution_name");
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this defined field type: the names of " +
                   "the replace_* arguments that set them, and nothing else, so the values go back into the " +
                   "replace_* method of the defined field.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            DefinedField definedField = Resolve();
            return new StringListAPI(ModelItemValueReader.GetValues(definedField,
                                                                    GetArgumentValueNames(definedField)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this defined field type by the name of the add_* " +
                   "argument that sets it: temperature and distribution_name, or file_name and step_id for a " +
                   "temperature read from a file. Use get_value_names to list them.",
                   Arguments = "name: str", Returns = "float, int or str")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            DefinedField definedField = Resolve();
            return ModelItemValueReader.GetValue(ModelItemValueReader.GetValues(definedField,
                                                                                GetArgumentValueNames(definedField)),
                                                 nameText, GetItemType(definedField) + " defined field");
        }

        [PythonAPI("Returns the expressions the defined field was entered with, without evaluating them, under " +
                   "the names get_value uses, for example temperature. Only a value entered as an expression is " +
                   "returned, with its leading equals sign; a value entered as a plain number is left out, and " +
                   "get_value returns it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            DefinedField definedField = Resolve();
            return ModelItemValueReader.GetScalarEquations(definedField, GetArgumentValueNames(definedField));
        }
        public override string ToString()
        {
            DefinedField definedField = Resolve();
            return "DefinedField(name=" + StringListAPI.ToDisplayString(definedField.Name) + ", type=" +
                   GetItemType(definedField) + ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private DefinedField Resolve()
        {
            return _service.ResolveItem(_stepName, _definedFieldName);
        }
    }
}
