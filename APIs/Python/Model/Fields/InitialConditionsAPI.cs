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
        [PythonAPI("Adds an initial temperature using command history. The region is a selection or a named " +
                   "region; the selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, temperature: float",
                   Returns = "InitialCondition",
                   Example = "pmx.model.initial_conditions.add_temperature('IC-1', selection, 20)")]
        public InitialConditionAPI add_temperature(string name, object region, object temperature)
        {
            _service.ValidateNewName(name, null);
            InitialTemperature initialCondition =
                new InitialTemperature(name, null, RegionTypeEnum.Selection, 0,
                                       _service.GetIsTwoDimensional());
            initialCondition.Temperature.Equation = PythonValueConverter.ToEquationString(temperature,
                                                                                          "temperature");
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
                new InitialTranslationalVelocity(name, null, RegionTypeEnum.Selection, 0, 0, 0,
                                                 _service.GetIsTwoDimensional());
            initialCondition.V1.Equation = PythonValueConverter.ToEquationString(v1, "v1");
            initialCondition.V2.Equation = PythonValueConverter.ToEquationString(v2, "v2");
            initialCondition.V3.Equation = PythonValueConverter.ToEquationString(v3, "v3");
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
            EquationString[] point = PythonValueConverter.ToEquationArray(axis_point, "axis_point", 3);
            EquationString[] direction = PythonValueConverter.ToEquationArray(axis_direction, "axis_direction", 3);
            InitialAngularVelocity initialCondition =
                new InitialAngularVelocity(name, null, RegionTypeEnum.Selection,
                                           _service.GetIsTwoDimensional());
            initialCondition.X.Equation = point[0];
            initialCondition.Y.Equation = point[1];
            initialCondition.Z.Equation = point[2];
            initialCondition.N1.Equation = direction[0];
            initialCondition.N2.Equation = direction[1];
            initialCondition.N3.Equation = direction[2];
            initialCondition.RotationalSpeed.Equation =
                PythonValueConverter.ToEquationString(rotational_speed, "rotational_speed");
            _service.ApplyRegion(initialCondition, region, "nodes");
            _service.Add(initialCondition);
            return this[name];
        }

        [PythonAPI("Replaces an initial temperature, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "temperature: float = None", Returns = "InitialCondition")]
        public InitialConditionAPI replace_temperature(string old_name, string name = null, object region = null,
                                                       object temperature = null)
        {
            InitialTemperature item = (InitialTemperature)_service.Clone(old_name);
            ApplyReplacedName(item, old_name, name, region);
            if (temperature != null)
                item.Temperature.Equation = PythonValueConverter.ToEquationString(temperature, "temperature");
            //
            return Replace(old_name, item);
        }

        [PythonAPI("Replaces an initial translational velocity, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "v1: float = None, v2: float = None, v3: float = None",
                   Returns = "InitialCondition")]
        public InitialConditionAPI replace_translational_velocity(string old_name, string name = null,
                                                                  object region = null, object v1 = null,
                                                                  object v2 = null, object v3 = null)
        {
            InitialTranslationalVelocity item = (InitialTranslationalVelocity)_service.Clone(old_name);
            ApplyReplacedName(item, old_name, name, region);
            if (v1 != null) item.V1.Equation = PythonValueConverter.ToEquationString(v1, "v1");
            if (v2 != null) item.V2.Equation = PythonValueConverter.ToEquationString(v2, "v2");
            if (v3 != null) item.V3.Equation = PythonValueConverter.ToEquationString(v3, "v3");
            //
            return Replace(old_name, item);
        }

        [PythonAPI("Replaces an initial angular velocity, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "axis_point: list[float] = None, axis_direction: list[float] = None, " +
                               "rotational_speed: float = None", Returns = "InitialCondition")]
        public InitialConditionAPI replace_angular_velocity(string old_name, string name = null,
                                                            object region = null, object axis_point = null,
                                                            object axis_direction = null,
                                                            object rotational_speed = null)
        {
            InitialAngularVelocity item = (InitialAngularVelocity)_service.Clone(old_name);
            ApplyReplacedName(item, old_name, name, region);
            if (axis_point != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axis_point, "axis_point", 3);
                item.X.Equation = values[0];
                item.Y.Equation = values[1];
                item.Z.Equation = values[2];
            }
            if (axis_direction != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axis_direction, "axis_direction", 3);
                item.N1.Equation = values[0];
                item.N2.Equation = values[1];
                item.N3.Equation = values[2];
            }
            if (rotational_speed != null)
                item.RotationalSpeed.Equation = PythonValueConverter.ToEquationString(rotational_speed,
                                                                                       "rotational_speed");
            //
            return Replace(old_name, item);
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
        private void ApplyReplacedName(InitialCondition item, string oldName, string name, object region)
        {
            string newName = name == null ? item.Name : name;
            _service.ValidateNewName(newName, oldName);
            item.Name = newName;
            if (region != null) _service.ApplyRegion(item, region, "nodes");
        }
        private InitialConditionAPI Replace(string oldName, InitialCondition item)
        {
            _service.Replace(oldName, item);
            //
            return this[item.Name];
        }
    }
}
