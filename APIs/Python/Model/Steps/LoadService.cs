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
                load.PhaseDeg.Equation = ToPhaseDeg(load.PhaseDeg, phaseDeg);
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
            //
            EnsureCommandsAvailable();
            ModelGateway.AddLoad(stepName, load);
        }
        public void Replace(string stepName, string oldName, Load load)
        {
            ResolveItem(stepName, oldName);
            ValidateName(stepName, load.Name, oldName);
            //
            EnsureCommandsAvailable();
            ModelGateway.ReplaceLoad(stepName, oldName, load);
        }
        public void Duplicate(string stepName, string[] names)
        {
            EnsureCommandsAvailable();
            ModelGateway.DuplicateLoads(stepName, names);
        }
        public void Propagate(string stepName, string name)
        {
            ResolveItem(stepName, name);
            //
            EnsureCommandsAvailable();
            ModelGateway.PropagateLoad(stepName, name);
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
    }
}
