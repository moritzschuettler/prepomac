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
    internal class FieldOutputService : StepItemService<FieldOutput>
    {
        // Constructors
        public FieldOutputService(ModelResolver modelResolver, SelectionService selectionService,
                                  StepService stepService, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, stepService, modelGateway, "field output")
        {
        }


        // Methods
        public override string GetItemType(FieldOutput fieldOutput)
        {
            return FieldOutputAPI.GetItemType(fieldOutput);
        }
        public void ApplyCommonValues(FieldOutput fieldOutput, object lastIterations, object contactElements,
                                      object global)
        {
            if (lastIterations != null)
                fieldOutput.LastIterations = PythonValueConverter.ToBool(lastIterations, "last_iterations");
            if (contactElements != null)
                fieldOutput.ContactElements = PythonValueConverter.ToBool(contactElements, "contact_elements");
            if (global != null) fieldOutput.Global = PythonValueConverter.ToBool(global, "global_output");
        }
        public void Add(string stepName, FieldOutput fieldOutput)
        {
            EnsureCommandsAvailable();
            ModelGateway.AddFieldOutput(stepName, fieldOutput);
        }
        public void Replace(string stepName, string oldName, FieldOutput fieldOutput)
        {
            string storedName = ResolveItem(stepName, oldName).Name;
            ValidateName(stepName, fieldOutput.Name, storedName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceFieldOutput(stepName, storedName, fieldOutput);
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateFieldOutputs(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            string storedName = ResolveItem(stepName, name).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateFieldOutput(stepName, storedName);
        }
        public void Remove(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveFieldOutputs(stepName, names);
        }
        //
        protected override CaeGlobals.OrderedDictionary<string, FieldOutput> GetItems(Step step)
        {
            return step.FieldOutputs;
        }
    }
}
