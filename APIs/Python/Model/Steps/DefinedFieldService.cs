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
    internal class DefinedFieldService : StepItemService<DefinedField>
    {
        // Constructors
        public DefinedFieldService(ModelResolver modelResolver, SelectionService selectionService,
                                   StepService stepService, IPythonModelGateway modelGateway,
                                   PythonFileNameResolver fileNameResolver)
            : base(modelResolver, selectionService, stepService, modelGateway, "defined field", fileNameResolver)
        {
        }


        // Methods
        public override string GetItemType(DefinedField definedField)
        {
            return DefinedFieldAPI.GetItemType(definedField);
        }
        public void ApplyCommonValues(DefinedField definedField, object amplitudeName, object color)
        {
            if (amplitudeName != null)
                definedField.AmplitudeName = ResolveAmplitudeName(amplitudeName, "amplitude_name");
            if (color != null) definedField.Color = PythonColorConverter.ToColor(color, "color");
        }
        public void Add(string stepName, DefinedField definedField)
        {
            // The step silently refuses an unsupported type, so the check is done before the command
            Step step = StepService.ResolveStep(stepName);
            if (!step.IsDefinedFieldSupported(definedField))
                throw new PythonAPIException("A " + DefinedFieldAPI.GetItemType(definedField) + " defined field is not " +
                                             "supported by the step '" + stepName + "', which is a " +
                                             StepAPI.GetItemType(step) + " step.");
            //
            EnsureCommandsAvailable();
            ModelGateway.AddDefinedField(stepName, definedField);
        }
        public void Replace(string stepName, string oldName, DefinedField definedField)
        {
            string storedName = ResolveItem(stepName, oldName).Name;
            ValidateName(stepName, definedField.Name, storedName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceDefinedField(stepName, storedName, definedField);
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateDefinedFields(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            string storedName = ResolveItem(stepName, name).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateDefinedField(stepName, storedName);
        }
        public void Remove(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveDefinedFields(stepName, names);
        }
        public void Hide(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideDefinedFields(stepName, names);
        }
        public void Show(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowDefinedFields(stepName, names);
        }
        //
        protected override OrderedDictionary<string, DefinedField> GetItems(Step step)
        {
            return step.DefinedFields;
        }
    }
}
