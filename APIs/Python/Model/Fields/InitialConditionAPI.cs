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
    public class InitialConditionAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(InitialTemperature), "temperature" },
            { typeof(InitialTranslationalVelocity), "translational_velocity" },
            { typeof(InitialAngularVelocity), "angular_velocity" }
        };
        private readonly string _initialConditionName;
        private readonly InitialConditionService _service;


        // Properties
        [PythonAPI("The initial condition name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The initial condition type, named after the add_* method that makes it without the add_ " +
                   "prefix, for example temperature or angular_velocity.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("The name of the region the initial condition is applied to.", Returns = "str")]
        public string region_name { get { return Resolve().RegionName; } }
        [PythonAPI("The region type the initial condition is applied to.", Returns = "str")]
        public string region_type { get { return Resolve().RegionType.ToString(); } }
        [PythonAPI("True when the initial condition is visible.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the initial condition resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal InitialConditionAPI(string initialConditionName, InitialConditionService service)
        {
            _initialConditionName = initialConditionName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(InitialCondition initialCondition)
        {
            string itemType;
            if (ItemTypes.TryGetValue(initialCondition.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The initial condition '" + initialCondition.Name + "' is a " +
                                         initialCondition.GetType().Name + ", which no add_* method of the " +
                                         "Python API makes.");
        }
        private static Dictionary<string, string[]> GetArgumentValueNames(InitialCondition initialCondition)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (initialCondition is InitialAngularVelocity)
            {
                names.Add("axis_point", new string[] { "x", "y", "z" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
            }
            //
            return names;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this initial condition type: the " +
                   "add_* argument names, followed by the stored values no argument sets.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            InitialCondition initialCondition = Resolve();
            return new StringListAPI(ModelItemValueReader.GetValues(initialCondition, typeof(InitialCondition),
                                                                    GetArgumentValueNames(initialCondition)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this initial condition type by the name of the add_* " +
                   "argument that sets it, for example temperature or distribution_name; a vector argument " +
                   "such as axis_point returns its components as a list. Use get_value_names to list them.",
                   Arguments = "name: str", Returns = "float, int, bool, str or NumericArray")]
        public object get_value(string name)
        {
            InitialCondition initialCondition = Resolve();
            return ModelItemValueReader.GetValue(initialCondition, typeof(InitialCondition),
                                                 GetArgumentValueNames(initialCondition), name,
                                                 GetItemType(initialCondition) + " initial condition");
        }
        [PythonAPI("Returns the stored equations of the initial condition without evaluating them, under the " +
                   "names get_value uses: an expression reads back with its leading equals sign and a plain " +
                   "value with its unit, and a vector argument such as axis_point as a list of the equations " +
                   "of its components.", Returns = "dict[str, str or StringList]")]
        public Dictionary<string, object> get_equations()
        {
            InitialCondition initialCondition = Resolve();
            return ModelItemValueReader.GetEquations(initialCondition, GetArgumentValueNames(initialCondition));
        }
        public override string ToString()
        {
            InitialCondition initialCondition = Resolve();
            return "InitialCondition(name=" + StringListAPI.ToDisplayString(initialCondition.Name) +
                   ", type=" + GetItemType(initialCondition) + ")";
        }
        //
        private InitialCondition Resolve()
        {
            return _service.ResolveInitialCondition(_initialConditionName);
        }
    }
}
