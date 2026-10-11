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
        [PythonAPI("The model name. An empty name, or one that starts or ends with a space, is refused.",
                   Returns = "str")]
        public object name
        {
            get { return _service.GetModelName(); }
            set { _service.SetModelName(ToText(value, "name")); }
        }
        [PythonAPI("The model space: ThreeD, PlaneStress, PlaneStrain or Axisymmetric; Undefined and None are " +
                   "refused. Assigning it sets the model space of a new model, as the new model dialog does, " +
                   "and leaves the element types as they are, so once the model has a geometry or a mesh a " +
                   "defined model space is changed by replace(model_space=...) instead. As the dialog does, a 2D " +
                   "space is refused when the geometry or the mesh is not in the x-y plane.", Returns = "str")]
        public object model_space
        {
            get { return _service.GetModelSpace(); }
            set { _service.SetModelSpaceAndUnitSystem(ToText(value, "model_space"), null); }
        }
        [PythonAPI("The unit system: M_KG_S_C, MM_TON_S_C, M_TON_S_C, IN_LB_S_F or UNIT_LESS; Undefined and " +
                   "None are refused. Assigning it sets the unit system of a new model, as the new model dialog " +
                   "does, and converts no value, so a defined unit system is changed only while the model has " +
                   "no geometry, mesh or model item, or its model space is undefined; otherwise it is refused " +
                   "and a model in another unit system is started by pmx.new_model.", Returns = "str")]
        public object unit_system
        {
            get { return _service.GetUnitSystem(); }
            set { _service.SetModelSpaceAndUnitSystem(null, ToText(value, "unit_system")); }
        }
        [PythonAPI("The model type: GeneralModel, Submodel or SlipWearModel. A Submodel needs an existing " +
                   "global results file and a SlipWearModel a number of cycles its cycle increment divides, so " +
                   "a change to one of them is made by replace together with those values.", Returns = "str")]
        public object model_type
        {
            get { return _service.GetModelType(); }
            set { Update(model_type: ToText(value, "model_type")); }
        }
        [PythonAPI("The global results file a submodel reads its boundary values from. As the dialog does, it " +
                   "takes a file name without path, with no space, colon, slash or non UTF-8 character, and the " +
                   "file must exist in the work directory. It is set on a Submodel only, and another model type " +
                   "reads it as None. Regenerate with file dialogs and a batch regeneration store a full path: " +
                   "one in the work directory whose file name the setter takes reads as that bare file name; " +
                   "one anywhere else, or one whose file name has a character the setter refuses, reads as the " +
                   "full path. The setter refuses the value read back only when it is such a full path, or when " +
                   "the file it names is no longer in the work directory.",
                   Returns = "str or None")]
        public object global_results_file_name
        {
            get { return _service.GetGlobalResultsFileName(); }
            set { Update(global_results_file_name: ToText(value, "global_results_file_name")); }
        }
        [PythonAPI("The results a slip wear model keeps: All, LastIncrementOfEachStep or LastIncrement. It is " +
                   "set on a SlipWearModel only, and another model type reads it as None.", Returns = "str or None")]
        public object slip_wear_results
        {
            get { return _service.GetSlipWearResults(); }
            set { Update(slip_wear_results: ToText(value, "slip_wear_results")); }
        }
        [PythonAPI("The number of slip wear cycles, a whole number evaluated from the value or equation it was " +
                   "entered with; get_equations() returns the equation. It takes a whole number or an equation " +
                   "given as a str, as replace does, on a SlipWearModel only; another model type reads it as None.",
                   Returns = "int or None")]
        public object number_of_cycles
        {
            get { return _service.GetNumberOfCycles(); }
            set { Update(number_of_cycles: RefuseNone(value, "number_of_cycles")); }
        }
        [PythonAPI("The slip wear cycle increment, a whole number evaluated from the value or equation it was " +
                   "entered with; get_equations() returns the equation. It takes a whole number or an equation " +
                   "given as a str, as replace does, on a SlipWearModel only; another model type reads it as None.",
                   Returns = "int or None")]
        public object cycles_increment
        {
            get { return _service.GetCyclesIncrement(); }
            set { Update(cycles_increment: RefuseNone(value, "cycles_increment")); }
        }
        [PythonAPI("The number of slip wear smoothing steps, a whole number evaluated from the value or equation " +
                   "it was entered with; get_equations() returns the equation. It takes a whole number or an " +
                   "equation given as a str, as replace does, on a SlipWearModel only; another model type reads it " +
                   "as None.", Returns = "int or None")]
        public object number_of_smoothing_steps
        {
            get { return _service.GetNumberOfSmoothingSteps(); }
            set { Update(number_of_smoothing_steps: RefuseNone(value, "number_of_smoothing_steps")); }
        }
        [PythonAPI("Whether a slip wear model uses BDM remeshing. It is set on a SlipWearModel only, and " +
                   "another model type reads it as None.", Returns = "bool or None")]
        public object bdm_remeshing
        {
            get { return _service.GetBdmRemeshing(); }
            set { Update(bdm_remeshing: RefuseNone(value, "bdm_remeshing")); }
        }
        [PythonAPI("The absolute zero temperature, or None when it is undefined. Assigning None makes it " +
                   "undefined.", Returns = "float or None")]
        public object absolute_zero
        {
            get { return _service.GetAbsoluteZero(); }
            set { _service.SetPhysicalConstant("absolute_zero", value); }
        }
        [PythonAPI("The Stefan Boltzmann constant, or None when it is undefined. Assigning None makes it " +
                   "undefined.", Returns = "float or None")]
        public object stefan_boltzmann
        {
            get { return _service.GetStefanBoltzmann(); }
            set { _service.SetPhysicalConstant("stefan_boltzmann", value); }
        }
        [PythonAPI("The Newton gravitational constant, or None when it is undefined. Assigning None makes " +
                   "it undefined.", Returns = "float or None")]
        public object newton_gravity
        {
            get { return _service.GetNewtonGravity(); }
            set { _service.SetPhysicalConstant("newton_gravity", value); }
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
                   "equation given as a str whose value is rounded to a whole number. number_of_cycles and " +
                   "cycles_increment must be 1 or larger and number_of_smoothing_steps 0 or larger; a smaller " +
                   "value is refused rather than raised to the minimum. A value the model type does not use - " +
                   "global_results_file_name on a model that is not a Submodel, or a slip wear value on one " +
                   "that is not a SlipWearModel - is refused, judged by the model_type the call ends with. " +
                   "global_results_file_name is a file name without path, with no space, colon, slash or non " +
                   "UTF-8 character, as the dialog takes it. As the dialog does, it refuses a SlipWearModel " +
                   "whose cycles_increment does not divide its number_of_cycles, and a Submodel whose global " +
                   "results file is not in the work directory, looked up when the call gives " +
                   "global_results_file_name or makes the model a Submodel. " +
                   "model_space changes the model space between the 2D spaces PlaneStress, PlaneStrain and " +
                   "Axisymmetric and updates the element types of the mesh, as the dialog does; a change " +
                   "between 2D and ThreeD, and Undefined, are refused.",
                   Arguments = "name: str = None, model_space: str = None, model_type: str = None, " +
                               "global_results_file_name: str = None, slip_wear_results: str = None, " +
                               "number_of_cycles: int or str = None, cycles_increment: int or str = None, " +
                               "number_of_smoothing_steps: int or str = None, bdm_remeshing: bool = None, " +
                               "absolute_zero: float = None, stefan_boltzmann: float = None, " +
                               "newton_gravity: float = None, clear_undefined_constants: bool = False",
                   Returns = "None",
                   Example = "pmx.model.properties.replace(model_type='Submodel', " +
                             "global_results_file_name='global.frd')")]
        public void replace(object name = null, object model_space = null, object model_type = null,
                            object global_results_file_name = null, object slip_wear_results = null,
                            object number_of_cycles = null,
                            object cycles_increment = null, object number_of_smoothing_steps = null,
                            object bdm_remeshing = null, object absolute_zero = null,
                            object stefan_boltzmann = null, object newton_gravity = null,
                            object clear_undefined_constants = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", true);
            string modelSpace = PythonValueConverter.ToText(model_space, "model_space", true);
            string modelType = PythonValueConverter.ToText(model_type, "model_type", true);
            string globalResultsFileName =
                PythonValueConverter.ToText(global_results_file_name, "global_results_file_name", true);
            string slipWearResults = PythonValueConverter.ToText(slip_wear_results, "slip_wear_results", true);
            bool clearUndefinedConstants =
                PythonValueConverter.ToBoolOrDefault(clear_undefined_constants, false, "clear_undefined_constants");
            _service.UpdateProperties(nameText, modelSpace, modelType, globalResultsFileName, slipWearResults,
                                      number_of_cycles, cycles_increment, number_of_smoothing_steps, bdm_remeshing,
                                      absolute_zero, stefan_boltzmann, newton_gravity, clearUndefinedConstants);
        }
        [PythonAPI("Returns the expressions number_of_cycles, cycles_increment and number_of_smoothing_steps " +
                   "were entered with, without evaluating them, on a SlipWearModel; another model type returns " +
                   "an empty dict. Only a value entered as an expression is " +
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
        // A property assignment that Update took as not given would leave the value unchanged without a word
        private static T RefuseNone<T>(T value, string propertyName) where T : class
        {
            if (value == null)
                throw new PythonAPIException("The property '" + propertyName + "' cannot be set to None.");
            //
            return value;
        }
        // A property typed as a .NET string would let pythonnet refuse a value of another type with a TypeError that
        // names no property, so the value is taken as an object and converted here
        private static string ToText(object value, string propertyName)
        {
            return PythonValueConverter.ToText(RefuseNone(value, propertyName), propertyName, false);
        }
        private void Update(string model_type = null, string global_results_file_name = null,
                            string slip_wear_results = null, object number_of_cycles = null,
                            object cycles_increment = null, object number_of_smoothing_steps = null,
                            object bdm_remeshing = null)
        {
            _service.UpdateProperties(null, null, model_type, global_results_file_name, slip_wear_results,
                                      number_of_cycles, cycles_increment, number_of_smoothing_steps, bdm_remeshing,
                                      null, null, null, false);
        }
    }
}
