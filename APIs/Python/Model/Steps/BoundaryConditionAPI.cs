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


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this boundary condition type: the " +
                   "add_* argument names, followed by the stored values no argument sets.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(Resolve(), typeof(BoundaryCondition), null).Keys);
        }

        [PythonAPI("Returns a value that is specific to this boundary condition type by the name of the add_* " +
                   "argument that sets it, for example u1. A " +
                   "displacement degree of freedom that is not constrained reads back as None. Use " +
                   "get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            BoundaryCondition boundaryCondition = Resolve();
            return ModelItemValueReader.GetValue(boundaryCondition, typeof(BoundaryCondition), null, name,
                                                 GetItemType(boundaryCondition) + " boundary condition");
        }

        [PythonAPI("Returns the stored equations of the boundary condition without evaluating them, under the " +
                   "names get_value uses, for example u1 or phase_deg: an expression reads back with its " +
                   "leading equals sign and a plain value with its unit.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
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
