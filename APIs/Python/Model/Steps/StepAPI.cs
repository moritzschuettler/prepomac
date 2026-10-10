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
        [PythonAPI("The maximum number of increments, or None where the solver input does not carry it: for a " +
                   "step of the add_static arguments whose incrementation type is Default, and for a frequency, " +
                   "complex frequency, buckle, steady state dynamics or boundary displacement step.",
                   Returns = "int or None")]
        public object max_increments
        {
            get
            {
                Step step = _service.ResolveStep(_stepName);
                if (StepFactory.GetUnusedValueReason(step, "max_increments") != null) return null;
                //
                return step.MaxIncrements;
            }
        }
        [PythonAPI("The incrementation type: Default, Automatic or Direct.", Returns = "str")]
        public string incrementation_type
        {
            get { return _service.ResolveStep(_stepName).IncrementationType.ToString(); }
        }
        [PythonAPI("The equation solver type.", Returns = "str")]
        public string solver_type { get { return _service.ResolveStep(_stepName).SolverType.ToString(); } }
        [PythonAPI("The results output frequency, or None when it is not set: the solver input then names no " +
                   "frequency and the solver writes the results of every increment. output_frequency=" +
                   "float('nan') unsets it.", Returns = "int or None")]
        public object output_frequency { get { return FromUnsetInt(_service.ResolveStep(_stepName).OutputFrequency); } }
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
        // The arguments of the replace_* method of the step type that the step types do not share, each with the
        // stored value it sets
        private static Dictionary<string, string[]> GetArgumentValueNames(Step step)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (step is FrequencyStep)
                ModelItemValueReader.AddArguments(names, "number_of_frequencies", "lower_frequency", "upper_frequency",
                                                  "storage", "cyclic_symmetry_mode_min", "cyclic_symmetry_mode_max");
            else if (step is ComplexFrequencyStep) ModelItemValueReader.AddArguments(names, "number_of_frequencies");
            else if (step is BuckleStep)
                ModelItemValueReader.AddArguments(names, "number_of_buckling_factors", "accuracy");
            else if (step is SteadyStateDynamicsStep)
                ModelItemValueReader.AddArguments(names, "harmonic", "lower_frequency", "upper_frequency",
                                                  "number_of_data_points", "bias", "number_of_fourier_terms",
                                                  "time_lower", "time_upper", "modal_damping");
            else if (step is ModalDynamicsStep)
                ModelItemValueReader.AddArguments(names, "steady_state", "relative_error", "modal_damping");
            else if (step is DynamicStep)
                ModelItemValueReader.AddArguments(names, "alpha", "solution_procedure", "relative_to_absolute",
                                                  "damping");
            else if (step is HeatTransferStep) ModelItemValueReader.AddArguments(names, "steady_state", "deltmx");
            // The step types that take the arguments of add_static derive from the static step
            if (step is StaticStep)
                ModelItemValueReader.AddArguments(names, "time_period", "initial_time_increment", "min_time_increment",
                                                  "max_time_increment");
            //
            return names;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this step type: the names of the " +
                   "replace_* arguments that set them, and nothing else, so the values go back into the " +
                   "replace_* method of the step. The values shared by every step type are properties of the " +
                   "step.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            Step step = _service.ResolveStep(_stepName);
            return new StringListAPI(ModelItemValueReader.GetValues(step, GetArgumentValueNames(step)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this step type by the name of the add_* argument that " +
                   "sets it, for example time_period. Use get_value_names to list them. A frequency limit or a " +
                   "cyclic symmetry mode that is not set is None, and so is a value the step does not use: a " +
                   "time value or relative_error, which add_static and add_modal_dynamics describe, and the " +
                   "Fourier terms and time range of a harmonic steady state dynamics step. A damping or " +
                   "modal_damping is returned as a copy of the part pmx.create makes, which the argument takes " +
                   "back.", Arguments = "name: str",
                   Returns = "float, int, bool, str, Damping, ModalDamping or None")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            Step step = _service.ResolveStep(_stepName);
            object value = ModelItemValueReader.GetValue(ModelItemValueReader.GetValues(step,
                                                                                        GetArgumentValueNames(step)),
                                                         nameText, GetItemType(step) + " step");
            // A value the step does not use is kept hidden, as the dialog hides it, and would be refused back
            if (StepFactory.GetUnusedValueReason(step, nameText) != null) return null;
            // A frequency limit that is not set is stored as NaN, and a count as the smallest integer
            if (value is double number && double.IsNaN(number)) return null;
            if (value is int count) return FromUnsetInt(count);
            //
            return value;
        }

        [PythonAPI("Returns the types of the step control parameters of the step, named after the pmx.create " +
                   "method that makes each, for example contact_control.", Returns = "StringList")]
        public StringListAPI get_control_types()
        {
            return new StringListAPI(_service.GetControlTypeNames(_stepName));
        }

        [PythonAPI("Returns detached copies of the step control parameters of the step, the parts pmx.create " +
                   "makes, as a plain list that goes back into set_controls; changing a copy changes nothing in " +
                   "the model.", Returns = "list[StepControlParameter]",
                   Example = "step.set_controls(step.get_controls())")]
        public object get_controls()
        {
            return PythonListConverter.ToPythonList(_service.GetControls(_stepName));
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
        private static object FromUnsetInt(int value)
        {
            if (value == int.MinValue) return null;
            //
            return value;
        }
        public override string ToString()
        {
            Step step = _service.ResolveStep(_stepName);
            return "Step(name=" + StringListAPI.ToDisplayString(step.Name) + ", type=" + GetItemType(step) +
                   ", active=" + step.Active + ")";
        }
    }
}
