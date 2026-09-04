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
    public class FieldOutputAPI : BaseAPI
    {
        // Variables
        private readonly string _stepName;
        private readonly string _fieldOutputName;
        private readonly FieldOutputService _service;


        // Properties
        [PythonAPI("The field output name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the field output belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The field output type: NodalFieldOutput, ElementFieldOutput or ContactFieldOutput.",
                   Returns = "str")]
        public string field_output_type { get { return Resolve().GetType().Name; } }
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

        [PythonAPI("Returns the names of the values that are specific to this field output type.",
                   Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(Resolve(), typeof(FieldOutput)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this field output type, for example output. Use " +
                   "get_value_names to list them.", Arguments = "name: str", Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            return ModelItemValueReader.GetValue(Resolve(), typeof(FieldOutput), name, "field output");
        }
        public override string ToString()
        {
            FieldOutput fieldOutput = Resolve();
            return "FieldOutput(name=" + StringListAPI.ToDisplayString(fieldOutput.Name) + ", type=" +
                   fieldOutput.GetType().Name + ", step=" + StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private FieldOutput Resolve()
        {
            return _service.ResolveItem(_stepName, _fieldOutputName);
        }
    }
}
