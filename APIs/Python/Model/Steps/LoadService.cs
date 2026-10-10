// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    internal class LoadService : StepItemService<Load>
    {
        // Constructors
        public LoadService(ModelResolver modelResolver, SelectionService selectionService,
                           StepService stepService, IPythonModelGateway modelGateway)
            : base(modelResolver, selectionService, stepService, modelGateway, "load")
        {
        }


        // Methods
        public override string GetItemType(Load load)
        {
            return LoadAPI.GetItemType(load);
        }
        // The step makes an item complex only in a steady state dynamics step, and the solver writes the phase of a
        // complex item only, so a phase anywhere else would be kept without effect
        public void ApplyCommonValues(Load load, object amplitudeName, object coordinateSystemName,
                                      object phaseDeg, object color)
        {
            if (amplitudeName != null) load.AmplitudeName = ResolveAmplitudeName(amplitudeName, "amplitude_name");
            if (coordinateSystemName != null)
            {
                string storedName = ResolveCoordinateSystemName(coordinateSystemName);
                if (storedName != CoordinateSystem.DefaultCoordinateSystemName && !LoadAPI.TakesCoordinateSystem(load))
                    throw new PythonAPIException("The argument 'coordinate_system_name' applies only to a " +
                                                 "concentrated_force or a surface_traction load; the solver input " +
                                                 "applies a " + LoadAPI.GetItemType(load) + " load in the global " +
                                                 "coordinate system. Leave it out here.");
                load.CoordinateSystemName = storedName;
            }
            if (phaseDeg != null)
            {
                if (!load.Complex)
                    throw new PythonAPIException("The argument 'phase_deg' applies only to a load of a " +
                                                 "steady_state_dynamics step, which is complex. Leave it out here.");
                if (!LoadAPI.TakesPhase(load))
                    throw new PythonAPIException("The argument 'phase_deg' does not apply to a " +
                                                 LoadAPI.GetItemType(load) + " load, which the solver input " +
                                                 "carries without a phase. Leave it out here.");
                PythonModelArgumentCheck.SetEquation(load.PhaseDeg, ToPhaseDeg(load.PhaseDeg, phaseDeg), "phase_deg");
            }
            if (color != null) load.Color = PythonColorConverter.ToColor(color, "color");
        }
        public void Add(string stepName, Load load)
        {
            // The step silently refuses an unsupported type, so the check is done before the command
            Step step = StepService.ResolveStep(stepName);
            if (!step.IsLoadSupported(load))
                throw new PythonAPIException("A " + LoadAPI.GetItemType(load) + " load is not supported by the step '" +
                                             stepName + "', which is a " + StepAPI.GetItemType(step) + " step.");
            // The dialog offers a pre-tension load only for a three dimensional model
            if (load is PreTensionLoad && GetIsTwoDimensional())
                throw new PythonAPIException("A pre-tension load needs a three dimensional model.");
            EnsureDefined(load);
            //
            EnsureCommandsAvailable();
            ModelGateway.AddLoad(stepName, load);
        }
        public void Replace(string stepName, string oldName, Load load)
        {
            string storedName = ResolveItem(stepName, oldName).Name;
            ValidateName(stepName, load.Name, storedName);
            EnsureDefined(load);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceLoad(stepName, storedName, load);
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateLoads(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            string storedName = ResolveItem(stepName, name).Name;
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateLoad(stepName, storedName);
        }
        public void Remove(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.RemoveLoads(stepName, names);
        }
        public void Hide(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.HideLoads(stepName, names);
        }
        public void Show(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.ShowLoads(stepName, names);
        }
        //
        protected override OrderedDictionary<string, Load> GetItems(Step step)
        {
            return step.Loads;
        }
        // The dialog refuses on OK a load without a magnitude, an axis or a direction, in these words, and judges the
        // load it ends with, so an add and a replace are checked on the final load, its evaluated equations included
        private static void EnsureDefined(Load load)
        {
            string error = GetDefinitionError(load);
            if (error != null) throw new PythonAPIException(error);
        }
        private static string GetDefinitionError(Load load)
        {
            if (load is CLoad concentratedForce && concentratedForce.Magnitude.Value == 0)
                return "At least one force component must not be equal to 0. Give a nonzero 'f1', 'f2' or 'f3'.";
            if (load is MomentLoad moment && moment.Magnitude.Value == 0)
                return "At least one moment component must not be equal to 0. Give a nonzero 'm1', 'm2' or 'm3'.";
            if (load is DLoad pressure && pressure.Magnitude.Value == 0)
                return "The pressure load magnitude must not be equal to 0. Give a nonzero 'magnitude'.";
            if (load is HydrostaticPressure hydrostaticPressure)
                return GetHydrostaticPressureError(hydrostaticPressure);
            if (load is ImportedPressure importedPressure)
                return GetImportedPressureError(importedPressure);
            if (load is STLoad surfaceTraction)
                return GetSurfaceTractionError(surfaceTraction);
            if (load is ShellEdgeLoad shellEdgeLoad && shellEdgeLoad.Magnitude.Value == 0)
                return "The shell edge load magnitude must not be equal to 0. Give a nonzero 'magnitude'.";
            if (load is GravityLoad gravity && gravity.Magnitude.Value == 0)
                return "At least one gravity load component must not be equal to 0. Give a nonzero 'f1', 'f2' or 'f3'.";
            if (load is CentrifLoad centrifugal)
            {
                if (centrifugal.N1.Value == 0 && centrifugal.N2.Value == 0 && centrifugal.N3.Value == 0)
                    return "At least one axis direction component must not be equal to 0. Give a nonzero " +
                           "'axis_direction'.";
                if (centrifugal.RotationalSpeed2 == 0)
                    return "Rotational speed must not be equal to 0. Give a nonzero 'rotational_speed'.";
            }
            if (load is PreTensionLoad preTension)
            {
                if (!preTension.AutoComputeDirection && preTension.X.Value == 0 && preTension.Y.Value == 0 &&
                    preTension.Z.Value == 0)
                    return "At least one pre-tension direction component must not be equal to 0. Give a nonzero " +
                           "'direction'.";
                if (preTension.Type == PreTensionLoadType.Force && preTension.GetMagnitudeValue() == 0)
                    return "Pre-tension magnitude must not be equal to 0. Give a nonzero 'magnitude'.";
            }
            //
            return null;
        }
        private static string GetHydrostaticPressureError(HydrostaticPressure load)
        {
            string error;
            if (load.IsProperlyDefined(out error)) return null;
            //
            if (load.N1.Value == 0 && load.N2.Value == 0 && load.N3.Value == 0)
                return error + " Give a nonzero 'direction'.";
            return error + " Give a 'first_point' and a 'second_point' that lie apart along the 'direction'.";
        }
        // The dialog refuses an imported pressure whose OpenFOAM case does not hold the field it names
        private static string GetImportedPressureError(ImportedPressure load)
        {
            string error;
            if (load.IsProperlyDefined(out error)) return null;
            //
            Dictionary<string, string[]> timeVariableNames =
                OpenFoamFileReader.GetTimeResultScalarVariableNames(load.FileName);
            string[] variableNames;
            if (timeVariableNames.Count == 0)
                return error + " Give in 'file_name' the .foam file of an OpenFOAM case whose time folders hold " +
                       "the results.";
            if (!timeVariableNames.TryGetValue(load.PressureTime, out variableNames))
                return error + " Give in 'pressure_time' one of the times of the case: " +
                       string.Join(", ", timeVariableNames.Keys) + ".";
            if (variableNames.Length == 0)
                return error + " The time " + load.PressureTime + " holds no scalar field, so give another " +
                       "'pressure_time'.";
            return error + " Give in 'pressure_variable_name' one of the scalar fields of time " + load.PressureTime +
                   ": " + string.Join(", ", variableNames) + ".";
        }
        // The dialog writes the group the distribution does not use as zero, so only the used group is judged
        private static string GetSurfaceTractionError(STLoad load)
        {
            bool constant = PythonNameComparer.AreSame(load.DistributionName, Distribution.DefaultDistributionName);
            if (constant && load.FMagnitude.Value == 0)
                return "At least one surface traction load component must not be equal to 0. Give a nonzero 'f1', " +
                       "'f2' or 'f3'.";
            if (!constant && load.PMagnitude.Value == 0)
                return "At least one surface traction load component must not be equal to 0. Give a nonzero 'p1', " +
                       "'p2' or 'p3'.";
            //
            return null;
        }
    }
}
