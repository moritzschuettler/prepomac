// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class ModelPropertiesAPI : BaseAPI
    {
        // Variables
        private readonly DocumentService _service;


        // Properties
        [PythonAPI("The model name.", Returns = "str")]
        public string name
        {
            get { return _service.GetModelName(); }
            set { _service.SetModelName(value); }
        }
        [PythonAPI("The model space: ThreeD, PlaneStress, PlaneStrain or Axisymmetric.", Returns = "str")]
        public string model_space
        {
            get { return _service.GetModelSpace(); }
            set { _service.SetModelSpaceAndUnitSystem(value, null); }
        }
        [PythonAPI("The unit system: M_KG_S_C, MM_TON_S_C, M_TON_S_C, IN_LB_S_F or UNIT_LESS.", Returns = "str")]
        public string unit_system
        {
            get { return _service.GetUnitSystem(); }
            set { _service.SetModelSpaceAndUnitSystem(null, value); }
        }
        [PythonAPI("The model type: GeneralModel, Submodel or SlipWearModel.", Returns = "str")]
        public string model_type
        {
            get { return _service.GetModelType(); }
            set { Update(model_type: value); }
        }
        [PythonAPI("The global results file a submodel reads its boundary values from.", Returns = "str")]
        public string global_results_file_name
        {
            get { return _service.GetGlobalResultsFileName(); }
            set { Update(global_results_file_name: value); }
        }
        [PythonAPI("The results a slip wear model keeps: All, LastIncrementOfEachStep or LastIncrement.",
                   Returns = "str")]
        public string slip_wear_results
        {
            get { return _service.GetSlipWearResults(); }
            set { Update(slip_wear_results: value); }
        }
        [PythonAPI("The number of slip wear cycles, a whole number evaluated from the value or equation it was " +
                   "entered with; get_equations() returns the equation.", Returns = "int")]
        public int number_of_cycles
        {
            get { return _service.GetNumberOfCycles(); }
            set { Update(number_of_cycles: value); }
        }
        [PythonAPI("The slip wear cycle increment, a whole number evaluated from the value or equation it was " +
                   "entered with; get_equations() returns the equation.", Returns = "int")]
        public int cycles_increment
        {
            get { return _service.GetCyclesIncrement(); }
            set { Update(cycles_increment: value); }
        }
        [PythonAPI("The number of slip wear smoothing steps, a whole number evaluated from the value or equation " +
                   "it was entered with; get_equations() returns the equation.", Returns = "int")]
        public int number_of_smoothing_steps
        {
            get { return _service.GetNumberOfSmoothingSteps(); }
            set { Update(number_of_smoothing_steps: value); }
        }
        [PythonAPI("Whether a slip wear model uses BDM remeshing.", Returns = "bool")]
        public bool bdm_remeshing
        {
            get { return _service.GetBdmRemeshing(); }
            set { Update(bdm_remeshing: value); }
        }
        [PythonAPI("The absolute zero temperature, or None when it is undefined.", Returns = "float")]
        public object absolute_zero
        {
            get { return _service.GetAbsoluteZero(); }
            set { Update(absolute_zero: value); }
        }
        [PythonAPI("The Stefan Boltzmann constant, or None when it is undefined.", Returns = "float")]
        public object stefan_boltzmann
        {
            get { return _service.GetStefanBoltzmann(); }
            set { Update(stefan_boltzmann: value); }
        }
        [PythonAPI("The Newton gravitational constant, or None when it is undefined.", Returns = "float")]
        public object newton_gravity
        {
            get { return _service.GetNewtonGravity(); }
            set { Update(newton_gravity: value); }
        }


        // Constructors
        internal ModelPropertiesAPI(DocumentService service)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Replaces the model properties using command history. A property that is not given is kept, " +
                   "unless clear_undefined_constants makes the omitted physical constants undefined. " +
                   "number_of_cycles, cycles_increment and number_of_smoothing_steps are whole numbers, or an " +
                   "equation given as a str whose value is rounded to a whole number.",
                   Arguments = "name: str = None, model_type: str = None, " +
                               "global_results_file_name: str = None, slip_wear_results: str = None, " +
                               "number_of_cycles: int or str = None, cycles_increment: int or str = None, " +
                               "number_of_smoothing_steps: int or str = None, bdm_remeshing: bool = None, " +
                               "absolute_zero: float = None, stefan_boltzmann: float = None, " +
                               "newton_gravity: float = None, clear_undefined_constants: bool = False",
                   Returns = "None",
                   Example = "pmx.model.properties.replace(model_type='Submodel', " +
                             "global_results_file_name='global.frd')")]
        public void replace(string name = null, string model_type = null, string global_results_file_name = null,
                            string slip_wear_results = null, object number_of_cycles = null,
                            object cycles_increment = null, object number_of_smoothing_steps = null,
                            object bdm_remeshing = null, object absolute_zero = null,
                            object stefan_boltzmann = null, object newton_gravity = null,
                            bool clear_undefined_constants = false)
        {
            _service.UpdateProperties(name, model_type, global_results_file_name, slip_wear_results,
                                      number_of_cycles, cycles_increment, number_of_smoothing_steps, bdm_remeshing,
                                      absolute_zero, stefan_boltzmann, newton_gravity, clear_undefined_constants);
        }
        [PythonAPI("Returns the expressions number_of_cycles, cycles_increment and number_of_smoothing_steps " +
                   "were entered with, without evaluating them. Only a value entered as an expression is " +
                   "returned, with its leading equals sign; a value entered as a plain number is left out, and " +
                   "the property of the same name returns it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            return _service.GetPropertyEquations();
        }
        public override string ToString()
        {
            return "ModelProperties(name=" + StringListAPI.ToDisplayString(_service.GetModelName()) +
                   ", model_space=" + StringListAPI.ToDisplayString(_service.GetModelSpace()) +
                   ", unit_system=" + StringListAPI.ToDisplayString(_service.GetUnitSystem()) + ")";
        }
        //
        private void Update(string model_type = null, string global_results_file_name = null,
                            string slip_wear_results = null, object number_of_cycles = null,
                            object cycles_increment = null, object number_of_smoothing_steps = null,
                            object bdm_remeshing = null, object absolute_zero = null,
                            object stefan_boltzmann = null, object newton_gravity = null)
        {
            _service.UpdateProperties(null, model_type, global_results_file_name, slip_wear_results,
                                      number_of_cycles, cycles_increment, number_of_smoothing_steps, bdm_remeshing,
                                      absolute_zero, stefan_boltzmann, newton_gravity, false);
        }
    }
}
