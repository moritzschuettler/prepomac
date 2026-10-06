// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    // Every add method takes the values of its own step type first and the values shared by all step
    // types last. A value that is not given keeps the application default, and on a replace it keeps
    // the value the step already carries.
    public class StepsAPI : ModelItemsAPI<Step, StepAPI>
    {
        // Variables
        private readonly StepService _service;
        private readonly StepContentServices _contentServices;


        // Constructors
        internal StepsAPI(StepService service, StepContentServices contentServices)
            : base("step")
        {
            _service = service;
            _contentServices = contentServices;
        }


        // Methods
        [PythonAPI("Adds a static step using command history.",
                   Arguments = "name: str, time_period: float = None, initial_time_increment: float = None, " +
                               "min_time_increment: float = None, max_time_increment: float = None, " +
                               "direct: bool = None, nlgeom: bool = None, max_increments: int = None, " +
                               "incrementation_type: str = None, solver_type: str = None, " +
                               "output_frequency: int = None, perturbation: bool = None, " +
                               "run_analysis: bool = None, copy_bcs_and_loads: bool = None",
                   Returns = "Step", Example = "pmx.model.steps.add_static('Step-1', time_period=1.0)")]
        public StepAPI add_static(string name, object time_period = null, object initial_time_increment = null,
                                  object min_time_increment = null, object max_time_increment = null,
                                  object direct = null, object nlgeom = null, object max_increments = null,
                                  object incrementation_type = null, object solver_type = null,
                                  object output_frequency = null, object perturbation = null,
                                  object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            StaticStep step = new StaticStep(name);
            _service.ApplyNewStepDefaults(step);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing static step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_static without " +
                               "copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_static(string old_name, string name = null, object time_period = null,
                                      object initial_time_increment = null, object min_time_increment = null,
                                      object max_time_increment = null, object direct = null,
                                      object nlgeom = null, object max_increments = null,
                                      object incrementation_type = null, object solver_type = null,
                                      object output_frequency = null, object perturbation = null,
                                      object run_analysis = null)
        {
            StaticStep step = _service.CloneForReplace<StaticStep>(old_name, "static");
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a slip wear step using command history. It takes the arguments of add_static.",
                   Arguments = "name: str, and the arguments of add_static", Returns = "Step")]
        public StepAPI add_slip_wear(string name, object time_period = null, object initial_time_increment = null,
                                     object min_time_increment = null, object max_time_increment = null,
                                     object direct = null, object nlgeom = null, object max_increments = null,
                                     object incrementation_type = null, object solver_type = null,
                                     object output_frequency = null, object perturbation = null,
                                     object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            SlipWearStep step = new SlipWearStep(name);
            _service.ApplyNewStepDefaults(step);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing slip wear step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_slip_wear without " +
                               "copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_slip_wear(string old_name, string name = null, object time_period = null,
                                         object initial_time_increment = null, object min_time_increment = null,
                                         object max_time_increment = null, object direct = null,
                                         object nlgeom = null, object max_increments = null,
                                         object incrementation_type = null, object solver_type = null,
                                         object output_frequency = null, object perturbation = null,
                                         object run_analysis = null)
        {
            SlipWearStep step = _service.CloneForReplace<SlipWearStep>(old_name, "slip_wear");
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a boundary displacement step using command history. It follows a slip wear step.",
                   Arguments = "name: str, nlgeom: bool = None, max_increments: int = None, " +
                               "incrementation_type: str = None, solver_type: str = None, " +
                               "output_frequency: int = None, perturbation: bool = None, " +
                               "run_analysis: bool = None, copy_bcs_and_loads: bool = None", Returns = "Step")]
        public StepAPI add_boundary_displacement(string name, object nlgeom = null, object max_increments = null,
                                                 object incrementation_type = null, object solver_type = null,
                                                 object output_frequency = null, object perturbation = null,
                                                 object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            BoundaryDisplacementStep step = new BoundaryDisplacementStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing boundary displacement step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_boundary_displacement without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_boundary_displacement(string old_name, string name = null, object nlgeom = null,
                                                     object max_increments = null,
                                                     object incrementation_type = null, object solver_type = null,
                                                     object output_frequency = null, object perturbation = null,
                                                     object run_analysis = null)
        {
            BoundaryDisplacementStep step =
                _service.CloneForReplace<BoundaryDisplacementStep>(old_name, "boundary_displacement");
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a frequency step using command history.",
                   Arguments = "name: str, number_of_frequencies: int = None, lower_frequency: float = None, " +
                               "upper_frequency: float = None, storage: bool = None, " +
                               "cyclic_symmetry_mode_min: int = None, cyclic_symmetry_mode_max: int = None, " +
                               "and the shared step arguments", Returns = "Step",
                   Example = "pmx.model.steps.add_frequency('Step-2', number_of_frequencies=20)")]
        public StepAPI add_frequency(string name, object number_of_frequencies = null, object lower_frequency = null,
                                     object upper_frequency = null, object storage = null,
                                     object cyclic_symmetry_mode_min = null,
                                     object cyclic_symmetry_mode_max = null, object nlgeom = null,
                                     object max_increments = null, object incrementation_type = null,
                                     object solver_type = null, object output_frequency = null,
                                     object perturbation = null, object run_analysis = null,
                                     object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            FrequencyStep step = new FrequencyStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyFrequencyValues(step, number_of_frequencies, lower_frequency, upper_frequency, storage,
                                             cyclic_symmetry_mode_min, cyclic_symmetry_mode_max);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing frequency step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_frequency without " +
                               "copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_frequency(string old_name, string name = null, object number_of_frequencies = null,
                                         object lower_frequency = null, object upper_frequency = null,
                                         object storage = null, object cyclic_symmetry_mode_min = null,
                                         object cyclic_symmetry_mode_max = null, object nlgeom = null,
                                         object max_increments = null, object incrementation_type = null,
                                         object solver_type = null, object output_frequency = null,
                                         object perturbation = null, object run_analysis = null)
        {
            FrequencyStep step = _service.CloneForReplace<FrequencyStep>(old_name, "frequency");
            StepFactory.ApplyFrequencyValues(step, number_of_frequencies, lower_frequency, upper_frequency, storage,
                                             cyclic_symmetry_mode_min, cyclic_symmetry_mode_max);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a complex frequency step using command history. It follows a frequency step.",
                   Arguments = "name: str, number_of_frequencies: int = None, and the shared step arguments",
                   Returns = "Step")]
        public StepAPI add_complex_frequency(string name, object number_of_frequencies = null, object nlgeom = null,
                                             object max_increments = null, object incrementation_type = null,
                                             object solver_type = null, object output_frequency = null,
                                             object perturbation = null, object run_analysis = null,
                                             object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            ComplexFrequencyStep step = new ComplexFrequencyStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyComplexFrequencyValues(step, number_of_frequencies);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing complex frequency step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_complex_frequency " +
                               "without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_complex_frequency(string old_name, string name = null,
                                                 object number_of_frequencies = null, object nlgeom = null,
                                                 object max_increments = null, object incrementation_type = null,
                                                 object solver_type = null, object output_frequency = null,
                                                 object perturbation = null, object run_analysis = null)
        {
            ComplexFrequencyStep step =
                _service.CloneForReplace<ComplexFrequencyStep>(old_name, "complex_frequency");
            StepFactory.ApplyComplexFrequencyValues(step, number_of_frequencies);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a buckle step using command history.",
                   Arguments = "name: str, number_of_buckling_factors: int = None, accuracy: float = None, " +
                               "and the shared step arguments", Returns = "Step")]
        public StepAPI add_buckle(string name, object number_of_buckling_factors = null, object accuracy = null,
                                  object nlgeom = null, object max_increments = null,
                                  object incrementation_type = null, object solver_type = null,
                                  object output_frequency = null, object perturbation = null,
                                  object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            BuckleStep step = new BuckleStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyBuckleValues(step, number_of_buckling_factors, accuracy);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing buckle step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_buckle without " +
                               "copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_buckle(string old_name, string name = null, object number_of_buckling_factors = null,
                                      object accuracy = null, object nlgeom = null, object max_increments = null,
                                      object incrementation_type = null, object solver_type = null,
                                      object output_frequency = null, object perturbation = null,
                                      object run_analysis = null)
        {
            BuckleStep step = _service.CloneForReplace<BuckleStep>(old_name, "buckle");
            StepFactory.ApplyBuckleValues(step, number_of_buckling_factors, accuracy);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a modal dynamics step using command history. The modal damping is created by " +
                   "pmx.create.modal_damping.",
                   Arguments = "name: str, steady_state: bool = None, relative_error: float = None, " +
                               "modal_damping: ModalDamping = None, the arguments of add_static, and the " +
                               "shared step arguments", Returns = "Step")]
        public StepAPI add_modal_dynamics(string name, object steady_state = null, object relative_error = null,
                                          object modal_damping = null, object time_period = null,
                                          object initial_time_increment = null, object min_time_increment = null,
                                          object max_time_increment = null, object direct = null,
                                          object nlgeom = null, object max_increments = null,
                                          object incrementation_type = null, object solver_type = null,
                                          object output_frequency = null, object perturbation = null,
                                          object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            ModalDynamicsStep step = new ModalDynamicsStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyModalDynamicsValues(step, steady_state, relative_error, modal_damping);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing modal dynamics step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_modal_dynamics " +
                               "without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_modal_dynamics(string old_name, string name = null, object steady_state = null,
                                              object relative_error = null, object modal_damping = null,
                                              object time_period = null, object initial_time_increment = null,
                                              object min_time_increment = null, object max_time_increment = null,
                                              object direct = null, object nlgeom = null,
                                              object max_increments = null, object incrementation_type = null,
                                              object solver_type = null, object output_frequency = null,
                                              object perturbation = null, object run_analysis = null)
        {
            ModalDynamicsStep step = _service.CloneForReplace<ModalDynamicsStep>(old_name, "modal_dynamics");
            StepFactory.ApplyModalDynamicsValues(step, steady_state, relative_error, modal_damping);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a steady state dynamics step using command history. The modal damping is created " +
                   "by pmx.create.modal_damping.",
                   Arguments = "name: str, harmonic: bool = None, lower_frequency: float = None, " +
                               "upper_frequency: float = None, number_of_data_points: int = None, " +
                               "bias: float = None, number_of_fourier_terms: int = None, time_lower: float = None, " +
                               "time_upper: float = None, modal_damping: ModalDamping = None, and the shared " +
                               "step arguments", Returns = "Step")]
        public StepAPI add_steady_state_dynamics(string name, object harmonic = null, object lower_frequency = null,
                                                 object upper_frequency = null, object number_of_data_points = null,
                                                 object bias = null, object number_of_fourier_terms = null,
                                                 object time_lower = null, object time_upper = null,
                                                 object modal_damping = null, object nlgeom = null,
                                                 object max_increments = null, object incrementation_type = null,
                                                 object solver_type = null, object output_frequency = null,
                                                 object perturbation = null, object run_analysis = null,
                                                 object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            SteadyStateDynamicsStep step = new SteadyStateDynamicsStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplySteadyStateDynamicsValues(step, harmonic, lower_frequency, upper_frequency,
                                                       number_of_data_points, bias, number_of_fourier_terms, time_lower,
                                                       time_upper, modal_damping);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing steady state dynamics step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_steady_state_dynamics without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_steady_state_dynamics(string old_name, string name = null, object harmonic = null,
                                                     object lower_frequency = null, object upper_frequency = null,
                                                     object number_of_data_points = null, object bias = null,
                                                     object number_of_fourier_terms = null, object time_lower = null,
                                                     object time_upper = null, object modal_damping = null,
                                                     object nlgeom = null, object max_increments = null,
                                                     object incrementation_type = null, object solver_type = null,
                                                     object output_frequency = null, object perturbation = null,
                                                     object run_analysis = null)
        {
            SteadyStateDynamicsStep step =
                _service.CloneForReplace<SteadyStateDynamicsStep>(old_name, "steady_state_dynamics");
            StepFactory.ApplySteadyStateDynamicsValues(step, harmonic, lower_frequency, upper_frequency,
                                                       number_of_data_points, bias, number_of_fourier_terms, time_lower,
                                                       time_upper, modal_damping);
            StepFactory.ApplyCommonValues(step, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a dynamic step using command history. The damping is created by pmx.create.damping.",
                   Arguments = "name: str, alpha: float = None, solution_procedure: str = None, " +
                               "relative_to_absolute: bool = None, damping: Damping = None, the arguments of " +
                               "add_static, and the shared step arguments", Returns = "Step")]
        public StepAPI add_dynamic(string name, object alpha = null, object solution_procedure = null,
                                   object relative_to_absolute = null, object damping = null,
                                   object time_period = null, object initial_time_increment = null,
                                   object min_time_increment = null, object max_time_increment = null,
                                   object direct = null, object nlgeom = null, object max_increments = null,
                                   object incrementation_type = null, object solver_type = null,
                                   object output_frequency = null, object perturbation = null,
                                   object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            DynamicStep step = new DynamicStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyDynamicValues(step, alpha, solution_procedure, relative_to_absolute, damping);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing dynamic step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_dynamic without " +
                               "copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_dynamic(string old_name, string name = null, object alpha = null,
                                       object solution_procedure = null, object relative_to_absolute = null,
                                       object damping = null, object time_period = null,
                                       object initial_time_increment = null, object min_time_increment = null,
                                       object max_time_increment = null, object direct = null,
                                       object nlgeom = null, object max_increments = null,
                                       object incrementation_type = null, object solver_type = null,
                                       object output_frequency = null, object perturbation = null,
                                       object run_analysis = null)
        {
            DynamicStep step = _service.CloneForReplace<DynamicStep>(old_name, "dynamic");
            StepFactory.ApplyDynamicValues(step, alpha, solution_procedure, relative_to_absolute, damping);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a heat transfer step using command history.",
                   Arguments = "name: str, steady_state: bool = None, deltmx: float = None (positive; float('inf'), " +
                               "the default, does not limit the temperature change), the arguments of add_static, " +
                               "and the shared step arguments", Returns = "Step")]
        public StepAPI add_heat_transfer(string name, object steady_state = null, object deltmx = null,
                                         object time_period = null, object initial_time_increment = null,
                                         object min_time_increment = null, object max_time_increment = null,
                                         object direct = null, object nlgeom = null, object max_increments = null,
                                         object incrementation_type = null, object solver_type = null,
                                         object output_frequency = null, object perturbation = null,
                                         object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            HeatTransferStep step = new HeatTransferStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing heat transfer step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_heat_transfer " +
                               "without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_heat_transfer(string old_name, string name = null, object steady_state = null,
                                             object deltmx = null, object time_period = null,
                                             object initial_time_increment = null,
                                             object min_time_increment = null, object max_time_increment = null,
                                             object direct = null, object nlgeom = null,
                                             object max_increments = null, object incrementation_type = null,
                                             object solver_type = null, object output_frequency = null,
                                             object perturbation = null, object run_analysis = null)
        {
            HeatTransferStep step = _service.CloneForReplace<HeatTransferStep>(old_name, "heat_transfer");
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds an uncoupled temperature displacement step using command history. It takes the " +
                   "arguments of add_heat_transfer.",
                   Arguments = "name: str, and the arguments of add_heat_transfer", Returns = "Step")]
        public StepAPI add_uncoupled_temp_disp(string name, object steady_state = null, object deltmx = null,
                                               object time_period = null, object initial_time_increment = null,
                                               object min_time_increment = null,
                                               object max_time_increment = null, object direct = null,
                                               object nlgeom = null, object max_increments = null,
                                               object incrementation_type = null, object solver_type = null,
                                               object output_frequency = null, object perturbation = null,
                                               object run_analysis = null, object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            UncoupledTempDispStep step = new UncoupledTempDispStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing uncoupled temperature displacement step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_uncoupled_temp_disp without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_uncoupled_temp_disp(string old_name, string name = null, object steady_state = null,
                                                   object deltmx = null, object time_period = null,
                                                   object initial_time_increment = null,
                                                   object min_time_increment = null,
                                                   object max_time_increment = null, object direct = null,
                                                   object nlgeom = null, object max_increments = null,
                                                   object incrementation_type = null, object solver_type = null,
                                                   object output_frequency = null, object perturbation = null,
                                                   object run_analysis = null)
        {
            UncoupledTempDispStep step =
                _service.CloneForReplace<UncoupledTempDispStep>(old_name, "uncoupled_temp_disp");
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Adds a coupled temperature displacement step using command history. It takes the " +
                   "arguments of add_heat_transfer.",
                   Arguments = "name: str, and the arguments of add_heat_transfer", Returns = "Step")]
        public StepAPI add_coupled_temp_disp(string name, object steady_state = null, object deltmx = null,
                                             object time_period = null, object initial_time_increment = null,
                                             object min_time_increment = null, object max_time_increment = null,
                                             object direct = null, object nlgeom = null,
                                             object max_increments = null, object incrementation_type = null,
                                             object solver_type = null, object output_frequency = null,
                                             object perturbation = null, object run_analysis = null,
                                             object copy_bcs_and_loads = null)
        {
            _service.ValidateName(name, null);
            CoupledTempDispStep step = new CoupledTempDispStep(name);
            _service.ApplyNewStepDefaults(step);
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            _service.Add(step, copy_bcs_and_loads);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing coupled temperature displacement step using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_coupled_temp_disp " +
                               "without copy_bcs_and_loads", Returns = "Step")]
        public StepAPI replace_coupled_temp_disp(string old_name, string name = null, object steady_state = null,
                                                 object deltmx = null, object time_period = null,
                                                 object initial_time_increment = null,
                                                 object min_time_increment = null,
                                                 object max_time_increment = null, object direct = null,
                                                 object nlgeom = null, object max_increments = null,
                                                 object incrementation_type = null, object solver_type = null,
                                                 object output_frequency = null, object perturbation = null,
                                                 object run_analysis = null)
        {
            CoupledTempDispStep step =
                _service.CloneForReplace<CoupledTempDispStep>(old_name, "coupled_temp_disp");
            StepFactory.ApplyHeatTransferValues(step, steady_state, deltmx);
            ApplyStatic(step, time_period, initial_time_increment, min_time_increment, max_time_increment,
                        direct, Common(nlgeom, max_increments, incrementation_type, solver_type,
                                       output_frequency, perturbation, run_analysis));
            //
            return Replace(old_name, name, step);
        }

        [PythonAPI("Moves the named steps to the requested position using command history. The target index " +
                   "is zero based and is evaluated after the moved steps have been removed.",
                   Arguments = "names: list[str] or str, target_index: int", Returns = "None",
                   Example = "pmx.model.steps.move(['Step-2'], 0)")]
        public void move(object names, int target_index)
        {
            _service.Move(ResolveNames(names, "names"), target_index);
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override Step GetItem(string name)
        {
            return _service.GetStep(name);
        }
        protected override StepAPI CreateItemAPI(string name)
        {
            return new StepAPI(name, _service, _contentServices);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(Step[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Step item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Steps(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private static StepCommonValues Common(object nlgeom, object maxIncrements, object incrementationType,
                                               object solverType, object outputFrequency, object perturbation,
                                               object runAnalysis)
        {
            return new StepCommonValues(nlgeom, maxIncrements, incrementationType, solverType, outputFrequency,
                                        perturbation, runAnalysis);
        }
        private static void ApplyStatic(StaticStep step, object timePeriod, object initialTimeIncrement,
                                        object minTimeIncrement, object maxTimeIncrement, object direct,
                                        StepCommonValues common)
        {
            StepFactory.ApplyStaticValues(step, timePeriod, initialTimeIncrement, minTimeIncrement,
                                          maxTimeIncrement, direct);
            StepFactory.ApplyCommonValues(step, common);
        }
        private StepAPI Replace(string oldName, string name, Step step)
        {
            if (name != null)
            {
                _service.ValidateName(name, oldName);
                step.Name = name;
            }
            _service.Replace(oldName, step);
            //
            return this[step.Name];
        }
    }
}
