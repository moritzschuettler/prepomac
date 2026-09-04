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
using System.Collections.Generic;
using System.Linq;

namespace APIs.Python.Model
{
    internal class StepService
    {
        // Variables
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
        public Step GetStep(string name)
        {
            if (name == null) return null;
            //
            return _modelResolver.GetModel().StepCollection.GetStep(name);
        }
        public Step ResolveStep(string name)
        {
            Step step = GetStep(name);
            if (step == null) throw new PythonAPIException("The step '" + name + "' does not exist.");
            //
            return step;
        }
        // The step type cannot change on a replace, the same way the dialog only edits the type it opened
        public TStep CloneForReplace<TStep>(string name, string stepTypeName) where TStep : Step
        {
            Step step = ResolveStep(name);
            TStep typed = step as TStep;
            if (typed == null)
                throw new PythonAPIException("The step '" + name + "' is a " + step.GetType().Name +
                                             ", so it cannot be replaced by a " + stepTypeName +
                                             ". Remove it and add the new step type instead.");
            //
            return typed.DeepClone();
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
            ValidateName(step.Name, oldName);
            //
            EnsureCommandsAvailable();
            _modelGateway.ReplaceStep(oldName, step);
        }
        public void Duplicate(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.DuplicateSteps(names);
        }
        public void Remove(string[] names)
        {
            EnsureCommandsAvailable();
            _modelGateway.RemoveSteps(names);
        }
        public void Move(string[] names, int targetIndex)
        {
            // The target index is evaluated after the moved steps have been removed
            int count = GetNames().Length - names.Length;
            if (targetIndex < 0 || targetIndex > count)
                throw new PythonAPIException("The argument 'target_index' must be between 0 and " + count + ".");
            //
            EnsureCommandsAvailable();
            _modelGateway.MoveSteps(names, targetIndex);
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
            ResolveStep(stepName);
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
            _modelGateway.ReplaceStepControls(stepName, stepControls);
        }
        public string[] GetControlTypeNames(string stepName)
        {
            StepControls stepControls = ResolveStep(stepName).StepControls;
            if (stepControls == null) return new string[0];
            //
            return stepControls.Parameters.Select(parameter => parameter.GetType().Name).ToArray();
        }
        public void ValidateName(string name, string allowedExistingName)
        {
            if (name == null || name.Trim().Length == 0)
                throw new PythonAPIException("The argument 'name' must not be empty.");
            if (allowedExistingName != null && string.Equals(name, allowedExistingName)) return;
            //
            foreach (string existingName in GetNames())
            {
                if (string.Equals(name, existingName))
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
