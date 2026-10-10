// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class FieldOutputAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(NodalFieldOutput), "nodal" },
            { typeof(ElementFieldOutput), "element" },
            { typeof(ContactFieldOutput), "contact" }
        };
        private readonly string _stepName;
        private readonly string _fieldOutputName;
        private readonly FieldOutputService _service;


        // Properties
        [PythonAPI("The field output name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the field output belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The field output type, named after the add_* method that makes it without the add_ prefix: " +
                   "nodal, element or contact.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the field output is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the results are written for the last iteration of a diverged increment.",
                   Returns = "bool")]
        public bool last_iterations { get { return Resolve().LastIterations; } }
        [PythonAPI("True when the results are written for the contact elements.", Returns = "bool")]
        public bool contact_elements { get { return Resolve().ContactElements; } }
        [PythonAPI("True when the results are written in the global coordinate system.", Returns = "bool")]
        public bool global_output { get { return Resolve().Global; } }


        // Constructors
        internal FieldOutputAPI(string stepName, string fieldOutputName, FieldOutputService service)
        {
            _stepName = stepName;
            _fieldOutputName = fieldOutputName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(FieldOutput fieldOutput)
        {
            string itemType;
            if (ItemTypes.TryGetValue(fieldOutput.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The field output '" + fieldOutput.Name + "' is a " +
                                         fieldOutput.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }
        // The arguments of the replace_* method of the field output type that the types do not share
        private static Dictionary<string, string[]> GetArgumentValueNames(FieldOutput fieldOutput)
        {
            if (fieldOutput is ElementFieldOutput) return ModelItemValueReader.Arguments("variables", "output");
            //
            return ModelItemValueReader.Arguments("variables");
        }


        // Methods
        [PythonAPI("Returns the names of the requested output variables.", Returns = "StringList")]
        public StringListAPI get_variables()
        {
            FieldOutput fieldOutput = Resolve();
            NodalFieldOutput nodal = fieldOutput as NodalFieldOutput;
            if (nodal != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(NodalFieldVariable),
                                                                            nodal.Variables));
            ElementFieldOutput element = fieldOutput as ElementFieldOutput;
            if (element != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(ElementFieldVariable),
                                                                             element.Variables));
            ContactFieldOutput contact = fieldOutput as ContactFieldOutput;
            if (contact != null)
                return new StringListAPI(PythonValueConverter.FromFlagsEnum(typeof(ContactFieldVariable),
                                                                             contact.Variables));
            //
            throw new PythonAPIException("The field output type " + fieldOutput.GetType().Name +
                                         " is not supported.");
        }

        [PythonAPI("Returns the names of the values that are specific to this field output type: the names of " +
                   "the replace_* arguments that set them, and nothing else, so the values go back into the " +
                   "replace_* method of the field output.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            FieldOutput fieldOutput = Resolve();
            return new StringListAPI(ModelItemValueReader.GetValues(fieldOutput,
                                                                    GetArgumentValueNames(fieldOutput)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this field output type by the name of the add_* argument " +
                   "that sets it, for example output. Use get_value_names to list them. The variables are " +
                   "returned as a list of names, the form the variables argument takes.", Arguments = "name: str",
                   Returns = "float, int, bool, str or list[str]")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            FieldOutput fieldOutput = Resolve();
            return ModelItemValueReader.GetValue(ModelItemValueReader.GetValues(fieldOutput,
                                                                                GetArgumentValueNames(fieldOutput)),
                                                 nameText, GetItemType(fieldOutput) + " field output");
        }
        public override string ToString()
        {
            FieldOutput fieldOutput = Resolve();
            return "FieldOutput(name=" + StringListAPI.ToDisplayString(fieldOutput.Name) + ", type=" +
                   GetItemType(fieldOutput) + ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private FieldOutput Resolve()
        {
            return _service.ResolveItem(_stepName, _fieldOutputName);
        }
    }
}
