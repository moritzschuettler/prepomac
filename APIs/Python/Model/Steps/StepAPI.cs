// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class StepAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(StaticStep), "static" },
            { typeof(SlipWearStep), "slip_wear" },
            { typeof(BoundaryDisplacementStep), "boundary_displacement" },
            { typeof(FrequencyStep), "frequency" },
            { typeof(ComplexFrequencyStep), "complex_frequency" },
            { typeof(BuckleStep), "buckle" },
            { typeof(ModalDynamicsStep), "modal_dynamics" },
            { typeof(SteadyStateDynamicsStep), "steady_state_dynamics" },
            { typeof(DynamicStep), "dynamic" },
            { typeof(HeatTransferStep), "heat_transfer" },
            { typeof(UncoupledTempDispStep), "uncoupled_temp_disp" },
            { typeof(CoupledTempDispStep), "coupled_temp_disp" }
        };
        private readonly string _stepName;
        private readonly StepService _service;
        private readonly StepContentServices _contentServices;


        // Properties
        [PythonAPI("The step name.", Returns = "str")]
        public string name { get { return _service.ResolveStep(_stepName).Name; } }
        [PythonAPI("The step type, named after the add_* method that makes it without the add_ prefix, for " +
                   "example static or frequency.", Returns = "str")]
        public string item_type { get { return GetItemType(_service.ResolveStep(_stepName)); } }
        [PythonAPI("True when the step is written to the solver input file.", Returns = "bool")]
        public bool active { get { return _service.ResolveStep(_stepName).Active; } }
        [PythonAPI("True when the geometric nonlinearity is accounted for.", Returns = "bool")]
        public bool nlgeom { get { return _service.ResolveStep(_stepName).Nlgeom; } }
        [PythonAPI("True when the step is a perturbation step.", Returns = "bool")]
        public bool perturbation { get { return _service.ResolveStep(_stepName).Perturbation; } }
        [PythonAPI("True when the step is solved, false when the model is only checked.", Returns = "bool")]
        public bool run_analysis { get { return _service.ResolveStep(_stepName).RunAnalysis; } }
        [PythonAPI("The maximum number of increments.", Returns = "int")]
        public int max_increments { get { return _service.ResolveStep(_stepName).MaxIncrements; } }
        [PythonAPI("The incrementation type: Default, Automatic or Direct.", Returns = "str")]
        public string incrementation_type
        {
            get { return _service.ResolveStep(_stepName).IncrementationType.ToString(); }
        }
        [PythonAPI("The equation solver type.", Returns = "str")]
        public string solver_type { get { return _service.ResolveStep(_stepName).SolverType.ToString(); } }
        [PythonAPI("The results output frequency.", Returns = "int")]
        public int output_frequency { get { return _service.ResolveStep(_stepName).OutputFrequency; } }
        [PythonAPI("The boundary conditions of the step.", Returns = "BoundaryConditions")]
        public BoundaryConditionsAPI bcs
        {
            get { return new BoundaryConditionsAPI(_stepName, _contentServices.BoundaryConditions); }
        }
        [PythonAPI("The loads of the step.", Returns = "Loads")]
        public LoadsAPI loads { get { return new LoadsAPI(_stepName, _contentServices.Loads); } }
        [PythonAPI("The field outputs of the step.", Returns = "FieldOutputs")]
        public FieldOutputsAPI field_outputs
        {
            get { return new FieldOutputsAPI(_stepName, _contentServices.FieldOutputs); }
        }
        [PythonAPI("The history outputs of the step.", Returns = "HistoryOutputs")]
        public HistoryOutputsAPI history_outputs
        {
            get { return new HistoryOutputsAPI(_stepName, _contentServices.HistoryOutputs); }
        }
        [PythonAPI("The defined fields of the step.", Returns = "DefinedFields")]
        public DefinedFieldsAPI defined_fields
        {
            get { return new DefinedFieldsAPI(_stepName, _contentServices.DefinedFields); }
        }


        // Constructors
        internal StepAPI(string stepName, StepService service, StepContentServices contentServices)
        {
            _stepName = stepName;
            _service = service;
            _contentServices = contentServices;
        }


        // Static methods
        internal static string GetItemType(Step step)
        {
            string itemType;
            if (ItemTypes.TryGetValue(step.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The step '" + step.Name + "' is a " + step.GetType().Name +
                                         ", which no add_* method of the Python API makes.");
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this step type: the add_* argument " +
                   "names, followed by the stored values no argument sets. The values shared by every step " +
                   "type are properties of the step.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(_service.ResolveStep(_stepName),
                                                                    typeof(Step), null).Keys);
        }

        [PythonAPI("Returns a value that is specific to this step type by the name of the add_* argument that " +
                   "sets it, for example time_period. Use get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            Step step = _service.ResolveStep(_stepName);
            return ModelItemValueReader.GetValue(step, typeof(Step), null, name, GetItemType(step) + " step");
        }

        [PythonAPI("Returns the types of the step control parameters of the step, named after the pmx.create " +
                   "method that makes each, for example contact_control.", Returns = "StringList")]
        public StringListAPI get_control_types()
        {
            return new StringListAPI(_service.GetControlTypeNames(_stepName));
        }

        [PythonAPI("Replaces the step control parameters using command history. The parameters are created " +
                   "by pmx.create.",
                   Arguments = "parameters: StepControlParameter or list[StepControlParameter]", Returns = "None",
                   Example = "step.set_controls([pmx.create.time_incrementation_control(i0=8)])")]
        public void set_controls(object parameters)
        {
            _service.SetControls(_stepName, parameters);
        }

        [PythonAPI("Removes the step control parameters of the step using command history.", Returns = "None")]
        public void clear_controls()
        {
            _service.SetControls(_stepName, null);
        }
        public override string ToString()
        {
            Step step = _service.ResolveStep(_stepName);
            return "Step(name=" + StringListAPI.ToDisplayString(step.Name) + ", type=" + GetItemType(step) +
                   ", active=" + step.Active + ")";
        }
    }
}
