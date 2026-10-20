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
        // The step makes an item complex only in a steady state dynamics step, and the solver writes the phase of a
        // complex item only, so a phase anywhere else would be kept without effect
        public void ApplyCommonValues(BoundaryCondition boundaryCondition, object phaseDeg, object color)
        {
            if (phaseDeg != null)
            {
                if (!boundaryCondition.Complex)
                    throw new PythonAPIException("The argument 'phase_deg' applies only to a boundary condition of a " +
                                                 "steady_state_dynamics step, which is complex. Leave it out here.");
                PythonModelArgumentCheck.SetEquation(boundaryCondition.PhaseDeg,
                                                     ToPhaseDeg(boundaryCondition.PhaseDeg, phaseDeg), "phase_deg");
            }
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
        // The dialog refuses these combinations on OK, and the model refuses a vector distribution with
        // prescribed rotations only when the deck is written, so they are rejected here instead
        public void EnsureDistributionApplicable(DisplacementRotation boundaryCondition)
        {
            string name = boundaryCondition.DistributionName;
            if (PythonNameComparer.AreSame(name, Distribution.DefaultDistributionName)) return;
            //
            if (boundaryCondition.RegionType == RegionTypeEnum.ReferencePointName)
                throw new PythonAPIException("The argument 'distribution_name' cannot be used with a reference " +
                                             "point region. Give a node set or surface region, or " +
                                             "distribution_name='" + Distribution.DefaultDistributionName + "'.");
            if (boundaryCondition.IsFreeFixedOrZero())
                throw new PythonAPIException("The distribution '" + name + "' given as 'distribution_name' scales " +
                                             "prescribed values only, and none of u1, u2, u3, ur1, ur2 and ur3 has " +
                                             "a nonzero value.");
            // A replace keeps the name of a distribution that may have been removed since
            Distribution distribution;
            if (!ModelResolver.GetModel().Distributions.TryGetValue(name, out distribution))
                throw new PythonAPIException("The distribution '" + name + "' given as 'distribution_name' does not " +
                                             "exist. Give an existing distribution or distribution_name='" +
                                             Distribution.DefaultDistributionName + "'.");
            string error;
            if (!boundaryCondition.IsDistributionTypeSupported(distribution.DistributionType, out error))
                throw new PythonAPIException("The distribution '" + name + "' given as 'distribution_name' is a " +
                                             "Vector distribution, which scales only u1, u2 and u3. Give a Scalar " +
                                             "distribution or leave ur1, ur2 and ur3 without a nonzero value.");
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
            EnsureDefined(boundaryCondition);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddBoundaryCondition(stepName, boundaryCondition);
        }
        public void Replace(string stepName, string oldName, BoundaryCondition boundaryCondition)
        {
            string storedName = ResolveItem(stepName, oldName).Name;
            ValidateName(stepName, boundaryCondition.Name, storedName);
            EnsureReferencePointRegionSupported(boundaryCondition);
            EnsureDefined(boundaryCondition);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceBoundaryCondition(stepName, storedName, boundaryCondition);
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
        // The dialog refuses on OK a boundary condition that constrains nothing, in these words, and judges the one it
        // ends with, so a replace that frees every degree of freedom is refused as an add without one is
        private static void EnsureDefined(BoundaryCondition boundaryCondition)
        {
            string error;
            if (boundaryCondition is DisplacementRotation displacementRotation &&
                !displacementRotation.IsProperlyDefined(out error))
            {
                if (displacementRotation.GetConstrainedDirections().Length == 0)
                    error += " Give a value to one of u1, u2, u3, ur1, ur2 and ur3, or name one in 'fixed_dofs'.";
                throw new PythonAPIException(error);
            }
            if (boundaryCondition is SubmodelBC submodel && submodel.GetConstrainedDirections().Length == 0)
                throw new PythonAPIException("At least one degree of freedom must be defined for the boundary " +
                                             "condition. Give True to one of 'u1', 'u2', 'u3', 'ur1', 'ur2' and " +
                                             "'ur3'.");
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateBoundaryConditions(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            string storedName = ResolveItem(stepName, name).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateBoundaryCondition(stepName, storedName);
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
        protected override CaeGlobals.OrderedDictionary<string, BoundaryCondition> GetItems(Step step)
        {
            return step.BoundaryConditions;
        }
    }
}
