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
    public class InitialConditionsAPI : ModelItemsAPI<InitialCondition, InitialConditionAPI>
    {
        // Variables
        private readonly InitialConditionService _service;


        // Constructors
        internal InitialConditionsAPI(InitialConditionService service)
            : base("initial condition")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds an initial temperature using command history. The region is a selection or a named region; the selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, temperature: float",
                   Returns = "InitialCondition",
                   Example = "pmx.model.initial_conditions.add_temperature('IC-1', selection, 20)")]
        public InitialConditionAPI add_temperature(string name, object region, object temperature)
        {
            _service.ValidateNewName(name, null);
            InitialTemperature initialCondition =
                new InitialTemperature(name, null, RegionTypeEnum.Selection,
                                       PythonValueConverter.ToDouble(temperature, "temperature"),
                                       _service.GetIsTwoDimensional());
            _service.ApplyRegion(initialCondition, region, "nodes");
            _service.Add(initialCondition);
            return this[name];
        }

        [PythonAPI("Adds an initial translational velocity using command history. The selection target is " +
                   "nodes.",
                   Arguments = "name: str, region: Selection or Region, v1: float, v2: float, v3: float",
                   Returns = "InitialCondition")]
        public InitialConditionAPI add_translational_velocity(string name, object region, object v1,
                                                              object v2, object v3)
        {
            _service.ValidateNewName(name, null);
            InitialTranslationalVelocity initialCondition =
                new InitialTranslationalVelocity(name, null, RegionTypeEnum.Selection,
                                                 PythonValueConverter.ToDouble(v1, "v1"),
                                                 PythonValueConverter.ToDouble(v2, "v2"),
                                                 PythonValueConverter.ToDouble(v3, "v3"),
                                                 _service.GetIsTwoDimensional());
            _service.ApplyRegion(initialCondition, region, "nodes");
            _service.Add(initialCondition);
            return this[name];
        }

        [PythonAPI("Adds an initial angular velocity using command history. The axis is given by a point " +
                   "and a direction. The region is a selection or a named region; the selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, axis_point: list[float], " +
                               "axis_direction: list[float], rotational_speed: float",
                   Returns = "InitialCondition")]
        public InitialConditionAPI add_angular_velocity(string name, object region, object axis_point,
                                                        object axis_direction, object rotational_speed)
        {
            _service.ValidateNewName(name, null);
            InitialAngularVelocity initialCondition =
                new InitialAngularVelocity(name, null, RegionTypeEnum.Selection,
                                           ToVector(axis_point, "axis_point"),
                                           ToVector(axis_direction, "axis_direction"),
                                           PythonValueConverter.ToDouble(rotational_speed, "rotational_speed"),
                                           _service.GetIsTwoDimensional());
            _service.ApplyRegion(initialCondition, region, "nodes");
            _service.Add(initialCondition);
            return this[name];
        }

        [PythonAPI("Hides the named initial conditions using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the named initial conditions using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames();
        }
        protected override InitialCondition GetItem(string name)
        {
            return _service.GetInitialCondition(name);
        }
        protected override InitialConditionAPI CreateItemAPI(string name)
        {
            return new InitialConditionAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(InitialCondition[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(InitialCondition item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "InitialConditions(count=" + names.Length + ", names=" +
                   new StringListAPI(names).ToString() + ")";
        }
        //
        private static double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return vector;
        }
    }
}
