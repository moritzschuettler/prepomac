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
using System;
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    internal class StepService
    {
        // Variables
        private static readonly Dictionary<Type, string> ControlTypes = new Dictionary<Type, string>
        {
            { typeof(TimeIncrementationStepControlParameter), "time_incrementation_control" },
            { typeof(FieldStepControlParameter), "field_control" },
            { typeof(ContactStepControlParameter), "contact_control" },
            { typeof(ResetStepControlParameter), "reset_control" }
        };
        private readonly ModelResolver _modelResolver;
        private readonly IPythonModelGateway _modelGateway;


        // Constructors
        public StepService(ModelResolver modelResolver, IPythonModelGateway modelGateway)
        {
            _modelResolver = modelResolver;
            _modelGateway = modelGateway;
        }


        // Methods
        public string[] GetNames()
        {
            return _modelResolver.GetModel().StepCollection.GetStepNames();
        }
        // The step collection matches a name exactly, while the API compares names the way every other model
        // collection keys them, so the step is found here and its own name is what goes on to the controller
        public Step GetStep(string name)
        {
            if (name == null) return null;
            //
            foreach (Step step in _modelResolver.GetModel().StepCollection.StepsList)
            {
                if (PythonNameComparer.AreSame(step.Name, name)) return step;
            }
            return null;
        }
        public Step ResolveStep(string name)
        {
            Step step = GetStep(name);
            if (step == null) throw new PythonAPIException("The step '" + name + "' does not exist.");
            //
            return step;
        }
        public string[] ResolveStepNames(string[] names)
        {
            string[] stepNames = new string[names.Length];
            for (int i = 0; i < names.Length; i++) stepNames[i] = ResolveStep(names[i]).Name;
            //
            return stepNames;
        }
        // The step type cannot change on a replace, the same way the dialog only edits the type it opened
        public TStep CloneForReplace<TStep>(string name, string itemType) where TStep : Step
        {
            Step step = ResolveStep(name);
            // Several step types derive from another, so only the exact type is the one being replaced
            if (step.GetType() != typeof(TStep))
                throw new PythonAPIException("The step '" + name + "' is made by add_" + StepAPI.GetItemType(step) +
                                             ", so replace_" + itemType + " cannot replace it. Remove it and " +
                                             "add the new step type instead.");
            //
            return ((TStep)step).DeepClone();
        }
        // The dialog applies the default solver type from the application settings to every new step
        public void ApplyNewStepDefaults(Step step)
        {
            EnsureCommandsAvailable();
            step.SolverType = _modelGateway.GetDefaultSolverType();
        }
        public void Add(Step step, object copyBoundaryConditionsAndLoads)
        {
            bool copy = copyBoundaryConditionsAndLoads == null ? false :
                PythonValueConverter.ToBool(copyBoundaryConditionsAndLoads, "copy_bcs_and_loads");
            //
            EnsureCommandsAvailable();
            _modelGateway.AddStep(step, copy);
        }
        public void Replace(string oldName, Step step)
        {
            string stepName = ResolveStep(oldName).Name;
            ValidateName(step.Name, stepName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceStep(stepName, step);
        }
        public void Duplicate(string[] names)
        {
            string[] stepNames = ResolveStepNames(names);
            //
            EnsureCommandsAvailable();
            _modelGateway.DuplicateSteps(stepNames);
        }
        public void Remove(string[] names)
        {
            string[] stepNames = ResolveStepNames(names);
            //
            EnsureCommandsAvailable();
            _modelGateway.RemoveSteps(stepNames);
        }
        public void Move(string[] names, int targetIndex)
        {
            // The model refuses a step named twice only after the command is recorded
            for (int i = 0; i < names.Length; i++)
            {
                for (int j = i + 1; j < names.Length; j++)
                {
                    if (PythonNameComparer.AreSame(names[i], names[j]))
                        throw new PythonAPIException("The argument 'names' names the step '" + names[j] + "' more " +
                                                     "than once.");
                }
            }
            string[] stepNames = ResolveStepNames(names);
            // The target index is evaluated after the moved steps have been removed
            int count = GetNames().Length - stepNames.Length;
            if (targetIndex < 0 || targetIndex > count)
                throw new PythonAPIException("The argument 'target_index' must be between 0 and " + count + ".");
            //
            EnsureCommandsAvailable();
            _modelGateway.MoveSteps(stepNames, targetIndex);
        }
        // The dialog checks the modes against the active cyclic symmetry before it records the step
        public void EnsureCyclicSymmetryModesValid(FrequencyStep step)
        {
            EnsureCommandsAvailable();
            string error = _modelGateway.GetCyclicSymmetryModesError(step);
            if (error != null)
                throw new PythonAPIException("The arguments 'cyclic_symmetry_mode_min' and 'cyclic_symmetry_mode_max' " +
                                             "do not fit the cyclic symmetry of the model: " + error);
        }
        public void SetActive(NamedClass[] items, bool active)
        {
            EnsureCommandsAvailable();
            // Every step name is null, because a step does not belong to a step
            _modelGateway.ActivateDeactivateItems(items, active, new string[items.Length]);
        }
        public void Rename(NamedClass item, string newName)
        {
            ValidateName(newName, item.Name);
            //
            EnsureCommandsAvailable();
            _modelGateway.RenameModelItem(item, newName, null);
        }
        public void SetControls(string stepName, object parameters)
        {
            string resolvedStepName = ResolveStep(stepName).Name;
            StepControls stepControls = new StepControls();
            if (parameters != null)
            {
                List<StepControlParameter> converted =
                    PythonObjectListConverter.ToList<StepControlParameter>(parameters, "parameters",
                                                                            "a step control parameter created by " +
                                                                            "pmx.create, or a list of them");
                foreach (StepControlParameter parameter in converted) stepControls.AddParameter(parameter);
            }
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceStepControls(resolvedStepName, stepControls);
        }
        public string[] GetControlTypeNames(string stepName)
        {
            StepControls stepControls = ResolveStep(stepName).StepControls;
            if (stepControls == null) return new string[0];
            //
            return stepControls.Parameters.Select(parameter => GetControlType(parameter)).ToArray();
        }
        // Copies, so a parameter changed by the script changes nothing until it goes back into set_controls
        public object[] GetControls(string stepName)
        {
            StepControls stepControls = ResolveStep(stepName).StepControls;
            if (stepControls == null) return new object[0];
            //
            List<object> parameters = new List<object>();
            foreach (StepControlParameter parameter in stepControls.Parameters)
            {
                // A parameter no pmx.create method makes could not go back into set_controls
                GetControlType(parameter);
                parameters.Add(parameter.DeepClone());
            }
            //
            return parameters.ToArray();
        }
        private static string GetControlType(StepControlParameter parameter)
        {
            string controlType;
            if (ControlTypes.TryGetValue(parameter.GetType(), out controlType)) return controlType;
            //
            throw new PythonAPIException("The step control parameter " + parameter.GetType().Name + " is not made " +
                                         "by any pmx.create method.");
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            PythonNameComparer.ValidateFormat(name, "name", allowedExistingName);
            if (allowedExistingName != null && PythonNameComparer.AreSame(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (PythonNameComparer.AreSame(name, existingName))
                    throw new PythonAPIException("The step '" + name + "' already exists.");
            }
        }
        //
        private void EnsureCommandsAvailable()
        {
            if (_modelGateway == null)
                throw new PythonAPIException("Model changes are not available in this Python API context.");
        }
    }
}
