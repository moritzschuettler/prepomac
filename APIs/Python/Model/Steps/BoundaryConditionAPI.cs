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
    public class BoundaryConditionAPI : BaseAPI
    {
        // Variables
        private readonly string _stepName;
        private readonly string _boundaryConditionName;
        private readonly BoundaryConditionService _service;


        // Properties
        [PythonAPI("The boundary condition name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The name of the step the boundary condition belongs to.", Returns = "str")]
        public string step_name { get { return _stepName; } }
        [PythonAPI("The boundary condition type, for example FixedBC or DisplacementRotation.",
                   Returns = "str")]
        public string boundary_condition_type { get { return Resolve().GetType().Name; } }
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
        [PythonAPI("The red, green and blue channels of the symbol color.", Returns = "IntList")]
        public IntListAPI color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }


        // Constructors
        internal BoundaryConditionAPI(string stepName, string boundaryConditionName,
                                      BoundaryConditionService service)
        {
            _stepName = stepName;
            _boundaryConditionName = boundaryConditionName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this boundary condition type.",
                   Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(Resolve(), typeof(BoundaryCondition)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this boundary condition type, for example u1. A " +
                   "displacement degree of freedom that is not constrained reads back as None. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            return ModelItemValueReader.GetValue(Resolve(), typeof(BoundaryCondition), name,
                                                 "boundary condition");
        }
        public override string ToString()
        {
            BoundaryCondition boundaryCondition = Resolve();
            return "BoundaryCondition(name=" + StringListAPI.ToDisplayString(boundaryCondition.Name) +
                   ", type=" + boundaryCondition.GetType().Name + ", step=" +
                   StringListAPI.ToDisplayString(_stepName) + ")";
        }
        //
        private BoundaryCondition Resolve()
        {
            return _service.ResolveItem(_stepName, _boundaryConditionName);
        }
    }
}
