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
    // The output variables are given by name, one or a list of them. A field output covers the whole
    // model, so it carries no region.
    public class FieldOutputsAPI : StepItemsAPI<FieldOutput, FieldOutputAPI>
    {
        // Variables
        private readonly FieldOutputService _service;


        // Constructors
        internal FieldOutputsAPI(string stepName, FieldOutputService service)
            : base("field output", stepName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a nodal field output using command history. Possible variables are RF, U, PU, V, " +
                   "NT, PNT and RFL.",
                   Arguments = "name: str, variables: list[str] or str, last_iterations: bool = None, " +
                               "contact_elements: bool = None, global_output: bool = None",
                   Returns = "FieldOutput",
                   Example = "step.field_outputs.add_nodal('NF-Output-1', ['U', 'RF'])")]
        public FieldOutputAPI add_nodal(string name, object variables, object last_iterations = null,
                                        object contact_elements = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            NodalFieldOutput fieldOutput =
                new NodalFieldOutput(name, PythonValueConverter.ToFlagsEnum<NodalFieldVariable>(variables,
                                                                                                "variables"));
            //
            return Add(fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Replaces an existing nodal field output using command history.",
                   Arguments = "old_name: str, name: str = None, variables: list[str] or str = None, and " +
                               "the arguments of add_nodal", Returns = "FieldOutput")]
        public FieldOutputAPI replace_nodal(string old_name, string name = null, object variables = null,
                                            object last_iterations = null, object contact_elements = null,
                                            object global_output = null)
        {
            NodalFieldOutput fieldOutput =
                _service.CloneForReplace<NodalFieldOutput>(StepName, old_name, "nodal");
            if (variables != null)
                fieldOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<NodalFieldVariable>(variables, "variables");
            //
            return Replace(old_name, name, fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Adds an element field output using command history. Possible variables are S, PHS, E, " +
                   "ME, PEEQ, ENER, HFL, ERR, HER, ZZS and SDV. Possible output values are Default, TwoD " +
                   "and ThreeD.",
                   Arguments = "name: str, variables: list[str] or str, output: str = None, and the shared " +
                               "field output arguments", Returns = "FieldOutput",
                   Example = "step.field_outputs.add_element('EF-Output-1', ['S', 'E'])")]
        public FieldOutputAPI add_element(string name, object variables, object output = null,
                                          object last_iterations = null, object contact_elements = null,
                                          object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            ElementFieldOutput fieldOutput =
                new ElementFieldOutput(name,
                                       PythonValueConverter.ToFlagsEnum<ElementFieldVariable>(variables,
                                                                                              "variables"));
            ApplyElementOutput(fieldOutput, output);
            //
            return Add(fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Replaces an existing element field output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_element with " +
                               "variables optional", Returns = "FieldOutput")]
        public FieldOutputAPI replace_element(string old_name, string name = null, object variables = null,
                                              object output = null, object last_iterations = null,
                                              object contact_elements = null, object global_output = null)
        {
            ElementFieldOutput fieldOutput =
                _service.CloneForReplace<ElementFieldOutput>(StepName, old_name, "element");
            if (variables != null)
                fieldOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<ElementFieldVariable>(variables, "variables");
            ApplyElementOutput(fieldOutput, output);
            //
            return Replace(old_name, name, fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Adds a contact field output using command history. Possible variables are CDIS, CSTR, " +
                   "CELS and PCON.",
                   Arguments = "name: str, variables: list[str] or str, and the shared field output " +
                               "arguments", Returns = "FieldOutput")]
        public FieldOutputAPI add_contact(string name, object variables, object last_iterations = null,
                                          object contact_elements = null, object global_output = null)
        {
            _service.ValidateName(StepName, name, null);
            ContactFieldOutput fieldOutput =
                new ContactFieldOutput(name,
                                       PythonValueConverter.ToFlagsEnum<ContactFieldVariable>(variables,
                                                                                              "variables"));
            //
            return Add(fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Replaces an existing contact field output using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_contact with " +
                               "variables optional", Returns = "FieldOutput")]
        public FieldOutputAPI replace_contact(string old_name, string name = null, object variables = null,
                                              object last_iterations = null, object contact_elements = null,
                                              object global_output = null)
        {
            ContactFieldOutput fieldOutput =
                _service.CloneForReplace<ContactFieldOutput>(StepName, old_name, "contact");
            if (variables != null)
                fieldOutput.Variables =
                    PythonValueConverter.ToFlagsEnum<ContactFieldVariable>(variables, "variables");
            //
            return Replace(old_name, name, fieldOutput, last_iterations, contact_elements, global_output);
        }

        [PythonAPI("Copies the field output to every following step using command history. From the last " +
                   "step nothing changes, as in the dialog.",
                   Arguments = "name: str", Returns = "None")]
        public void propagate(string name)
        {
            _service.Propagate(StepName, name);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames(StepName);
        }
        protected override FieldOutput GetItem(string name)
        {
            return _service.GetItem(StepName, name);
        }
        protected override FieldOutputAPI CreateItemAPI(string name)
        {
            return new FieldOutputAPI(StepName, name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(StepName, names);
        }
        protected override void SetItemsActive(FieldOutput[] items, bool active)
        {
            _service.SetActive(StepName, items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(StepName, names);
        }
        protected override void RenameItem(FieldOutput item, string newName)
        {
            _service.Rename(StepName, item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "FieldOutputs(step=" + StringListAPI.ToDisplayString(StepName) + ", count=" + names.Length +
                   ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private static void ApplyElementOutput(ElementFieldOutput fieldOutput, object output)
        {
            if (output == null) return;
            //
            string text = output as string;
            if (text == null) throw new PythonAPIException("The argument 'output' must be a string.");
            //
            fieldOutput.Output = PythonValueConverter.ToEnum<ElementFieldOutputOutputEnum>(text, "output");
        }
        private FieldOutputAPI Add(FieldOutput fieldOutput, object lastIterations, object contactElements,
                                   object global)
        {
            _service.ApplyCommonValues(fieldOutput, lastIterations, contactElements, global);
            _service.Add(StepName, fieldOutput);
            //
            return this[fieldOutput.Name];
        }
        private FieldOutputAPI Replace(string oldName, string name, FieldOutput fieldOutput,
                                       object lastIterations, object contactElements, object global)
        {
            _service.ApplyCommonValues(fieldOutput, lastIterations, contactElements, global);
            if (name != null)
            {
                _service.ValidateName(StepName, name, oldName);
                fieldOutput.Name = name;
            }
            _service.Replace(StepName, oldName, fieldOutput);
            //
            return this[fieldOutput.Name];
        }
    }
}
