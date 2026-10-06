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
    internal class BoundaryConditionService : StepItemService<BoundaryCondition>
    {
        // Constructors
        public BoundaryConditionService(ModelResolver modelResolver, SelectionService selectionService,
                                        StepService stepService, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, stepService, modelGateway, "boundary condition")
        {
        }


        // Methods
        public override string GetItemType(BoundaryCondition boundaryCondition)
        {
            return BoundaryConditionAPI.GetItemType(boundaryCondition);
        }
        public void ApplyCommonValues(BoundaryCondition boundaryCondition, object phaseDeg, object color)
        {
            if (phaseDeg != null)
                boundaryCondition.PhaseDeg.Equation = PythonValueConverter.ToEquationString(phaseDeg, "phase_deg");
            if (color != null) boundaryCondition.Color = PythonColorConverter.ToColor(color, "color");
        }
        public void ApplyAmplitudeName(BoundaryCondition boundaryCondition, object amplitudeName)
        {
            if (amplitudeName == null) return;
            //
            boundaryCondition.AmplitudeName = ResolveAmplitudeName(amplitudeName, "amplitude_name");
        }
        public void ApplyCoordinateSystemName(BoundaryCondition boundaryCondition, object coordinateSystemName)
        {
            if (coordinateSystemName == null) return;
            //
            boundaryCondition.CoordinateSystemName = ResolveCoordinateSystemName(coordinateSystemName);
        }
        public void Add(string stepName, BoundaryCondition boundaryCondition)
        {
            // The step silently refuses an unsupported type, so the check is done before the command
            Step step = StepService.ResolveStep(stepName);
            if (!step.IsBoundaryConditionSupported(boundaryCondition))
                throw new PythonAPIException("A " + BoundaryConditionAPI.GetItemType(boundaryCondition) + " boundary " +
                                             "condition is not supported by the step '" + stepName + "', " +
                                             "which is a " + StepAPI.GetItemType(step) + " step.");
            EnsureReferencePointRegionSupported(boundaryCondition);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddBoundaryCondition(stepName, boundaryCondition);
        }
        public void Replace(string stepName, string oldName, BoundaryCondition boundaryCondition)
        {
            ResolveItem(stepName, oldName);
            ValidateName(stepName, boundaryCondition.Name, oldName);
            EnsureReferencePointRegionSupported(boundaryCondition);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceBoundaryCondition(stepName, oldName, boundaryCondition);
        }
        // Only some of the types support a reference point, and the dialogs of the others do not
        // offer one. The model check that would catch it runs after the command is already recorded.
        private static void EnsureReferencePointRegionSupported(BoundaryCondition boundaryCondition)
        {
            if (boundaryCondition.RegionType == RegionTypeEnum.ReferencePointName &&
                !IsReferencePointRegionSupported(boundaryCondition))
                throw new PythonAPIException("A " + BoundaryConditionAPI.GetItemType(boundaryCondition) + " boundary " +
                                             "condition does not support a reference point region. Give a region of another type.");
        }
        private static bool IsReferencePointRegionSupported(BoundaryCondition boundaryCondition)
        {
            return boundaryCondition is FixedBC || boundaryCondition is DisplacementRotation;
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateBoundaryConditions(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            ResolveItem(stepName, name);
            EnsureFollowingStepExists(stepName, name);
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateBoundaryCondition(stepName, name);
        }
        public void Remove(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveBoundaryConditions(stepName, names);
        }
        public void Hide(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideBoundaryConditions(stepName, names);
        }
        public void Show(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowBoundaryConditions(stepName, names);
        }
        //
        protected override OrderedDictionary<string, BoundaryCondition> GetItems(Step step)
        {
            return step.BoundaryConditions;
        }
    }
}
