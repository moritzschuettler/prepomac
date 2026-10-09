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
using System;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class BoundaryConditionAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(FixedBC), "fixed" },
            { typeof(DisplacementRotation), "displacement_rotation" },
            { typeof(SubmodelBC), "submodel" },
            { typeof(TemperatureBC), "temperature" }
        };
        private readonly string _stepName;
        private readonly string _boundaryConditionName;
        private readonly BoundaryConditionService _service;


        // Properties
        [PythonAPI("The boundary condition name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the boundary condition belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The boundary condition type, named after the add_* method that makes it without the add_ " +
                   "prefix, for example fixed or displacement_rotation.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the boundary condition is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the boundary condition symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("The region type, for example Selection or NodeSetName.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("The name of the region the boundary condition is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The amplitude name.", Returns = "str")]
        public string amplitude_name { get { return Resolve().AmplitudeName; } }
        [PythonAPI("The coordinate system name.", Returns = "str")]
        public string coordinate_system_name { get { return Resolve().CoordinateSystemName; } }
        [PythonAPI("The symbol color as a list [R, G, B, A] of channel values, which the color argument takes " +
                   "back.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }


        // Constructors
        internal BoundaryConditionAPI(string stepName, string boundaryConditionName,
                                      BoundaryConditionService service)
        {
            _stepName = stepName;
            _boundaryConditionName = boundaryConditionName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(BoundaryCondition boundaryCondition)
        {
            string itemType;
            if (ItemTypes.TryGetValue(boundaryCondition.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The boundary condition '" + boundaryCondition.Name + "' is a " +
                                         boundaryCondition.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }
        // Only a displacement rotation takes a phase, and only when a steady state dynamics step makes it complex
        private static bool TakesPhase(BoundaryCondition boundaryCondition)
        {
            return boundaryCondition is DisplacementRotation && boundaryCondition.Complex;
        }
        // The phase is stored on every boundary condition, so the shared reader leaves it out; it is read back
        // only where the phase_deg argument takes it, because elsewhere the argument is refused
        private static Dictionary<string, object> GetValues(BoundaryCondition boundaryCondition)
        {
            Dictionary<string, object> values = ModelItemValueReader.GetValues(boundaryCondition,
                                                                               typeof(BoundaryCondition), null);
            if (TakesPhase(boundaryCondition)) values["phase_deg"] = boundaryCondition.PhaseDeg.Value;
            //
            return values;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this boundary condition type: the " +
                   "add_* argument names, followed by the stored values no argument sets.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(GetValues(Resolve()).Keys);
        }

        [PythonAPI("Returns a value that is specific to this boundary condition type by the name of the add_* " +
                   "argument that sets it, for example u1. phase_deg is read back only for a displacement " +
                   "rotation of a steady_state_dynamics step, which is complex and takes it. A " +
                   "displacement degree of freedom that is not constrained reads back as None. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            BoundaryCondition boundaryCondition = Resolve();
            return ModelItemValueReader.GetValue(GetValues(boundaryCondition), name,
                                                 GetItemType(boundaryCondition) + " boundary condition");
        }

        [PythonAPI("Returns the expressions the boundary condition was entered with, without evaluating them, " +
                   "under the names get_value uses, for example u1 or phase_deg. Only a value entered as an " +
                   "expression is returned, with its leading equals sign; a value entered as a plain number is " +
                   "left out, and get_value returns it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            BoundaryCondition boundaryCondition = Resolve();
            Dictionary<string, string> equations = ModelItemValueReader.GetEquations(boundaryCondition);
            if (!TakesPhase(boundaryCondition)) equations.Remove("phase_deg");
            //
            return equations;
        }
        public override string ToString()
        {
            BoundaryCondition boundaryCondition = Resolve();
            return "BoundaryCondition(name=" + StringListAPI.ToDisplayString(boundaryCondition.Name) +
                   ", type=" + GetItemType(boundaryCondition) + ", step=" +
                   StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private BoundaryCondition Resolve()
        {
            return _service.ResolveItem(_stepName, _boundaryConditionName);
        }
    }
}
