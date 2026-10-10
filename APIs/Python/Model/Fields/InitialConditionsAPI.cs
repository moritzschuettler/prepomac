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
        // The dialog picks geometry, which the controller turns into the nodes it covers
        private static readonly string[] NodeTargets = new string[] { "nodes", "geometry" };
        private readonly InitialConditionService _service;


        // Constructors
        internal InitialConditionsAPI(InitialConditionService service)
            : base("initial condition")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds an initial temperature using command history. The region is a selection or a named " +
                   "region; the selection target is geometry, as the dialog picks it, or nodes. A distribution " +
                   "scales the temperature over the region. color is the symbol color, given as [R, G, B], " +
                   "[R, G, B, A] or a color name, as " +
                   "every initial condition takes it.",
                   Arguments = "name: str, region: Selection or Region, temperature: float, " +
                               "distribution_name: str = None, color: list[int] or str = None",
                   Returns = "InitialCondition",
                   Example = "pmx.model.initial_conditions.add_temperature('IC-1', selection, 20)")]
        public InitialConditionAPI add_temperature(object name, object region, object temperature,
                                                   object distribution_name = null, object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            InitialTemperature initialCondition =
                new InitialTemperature(nameText, null, RegionTypeEnum.Selection, 0,
                                       _service.GetIsTwoDimensional());
            PythonModelArgumentCheck.SetEquation(initialCondition.Temperature, temperature, "temperature");
            _service.ApplyRegion(initialCondition, region, NodeTargets);
            if (distribution_name != null)
                initialCondition.DistributionName = _service.ResolveScalarDistributionName(distribution_name);
            ApplyColor(initialCondition, color);
            _service.Add(initialCondition);
            return this[nameText];
        }

        [PythonAPI("Adds an initial translational velocity using command history. The selection target is " +
                   "geometry, as the dialog picks it, or nodes. At least one of v1, v2 and v3 must be nonzero, " +
                   "and in a 2D model v3 must be zero.",
                   Arguments = "name: str, region: Selection or Region, v1: float, v2: float, v3: float, " +
                               "color: list[int] or str = None",
                   Returns = "InitialCondition")]
        public InitialConditionAPI add_translational_velocity(object name, object region, object v1,
                                                              object v2, object v3, object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            InitialTranslationalVelocity initialCondition =
                new InitialTranslationalVelocity(nameText, null, RegionTypeEnum.Selection, 0, 0, 0,
                                                 _service.GetIsTwoDimensional());
            PythonModelArgumentCheck.SetEquation(initialCondition.V1, v1, "v1");
            PythonModelArgumentCheck.SetEquation(initialCondition.V2, v2, "v2");
            PythonModelArgumentCheck.SetEquation(initialCondition.V3,
                ToInPlane(initialCondition, initialCondition.V3, PythonValueConverter.ToEquationString(v3, "v3"),
                          "v3", null), "v3");
            _service.ApplyRegion(initialCondition, region, NodeTargets);
            ApplyColor(initialCondition, color);
            _service.Add(initialCondition);
            return this[nameText];
        }

        [PythonAPI("Adds an initial angular velocity using command history. The axis is given by a point " +
                   "and a non-zero direction. The region is a selection or a named region; the selection " +
                   "target is geometry, " +
                   "as the dialog picks it, or nodes. " +
                   "The rotational_speed is in rad/s and must be larger than 0. In a 2D model the third " +
                   "value of axis_point and the first two of axis_direction must be zero.",
                   Arguments = "name: str, region: Selection or Region, axis_point: list[float], " +
                               "axis_direction: list[float], rotational_speed: float, color: list[int] or str = None",
                   Returns = "InitialCondition")]
        public InitialConditionAPI add_angular_velocity(object name, object region, object axis_point,
                                                        object axis_direction, object rotational_speed,
                                                        object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateNewName(nameText, null);
            EquationString[] point = PythonValueConverter.ToEquationArray(axis_point, "axis_point", 3);
            EquationString[] direction = PythonValueConverter.ToEquationArray(axis_direction, "axis_direction", 3);
            InitialAngularVelocity initialCondition =
                new InitialAngularVelocity(nameText, null, RegionTypeEnum.Selection,
                                           _service.GetIsTwoDimensional());
            ApplyAxisPoint(initialCondition, point);
            ApplyAxisDirection(initialCondition, direction);
            PythonModelArgumentCheck.SetEquation(initialCondition.RotationalSpeed,
                                                 ToRotationalSpeed(initialCondition, rotational_speed),
                                                 "rotational_speed");
            _service.ApplyRegion(initialCondition, region, NodeTargets);
            ApplyColor(initialCondition, color);
            _service.Add(initialCondition);
            return this[nameText];
        }

        [PythonAPI("Replaces an initial temperature, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "temperature: float = None, distribution_name: str = None, color: list[int] or str = None",
                   Returns = "InitialCondition")]
        public InitialConditionAPI replace_temperature(object old_name, object name = null, object region = null,
                                                       object temperature = null,
                                                       object distribution_name = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            InitialTemperature item = _service.Clone<InitialTemperature>(oldName, "temperature");
            ApplyReplacedName(item, oldName, nameText, region, color);
            if (temperature != null)
                PythonModelArgumentCheck.SetEquation(item.Temperature, temperature, "temperature");
            if (distribution_name != null)
                item.DistributionName = _service.ResolveScalarDistributionName(distribution_name);
            //
            return Replace(oldName, item);
        }

        [PythonAPI("Replaces an initial translational velocity, keeping every value that is not given.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "v1: float = None, v2: float = None, v3: float = None, color: list[int] or str = None",
                   Returns = "InitialCondition")]
        public InitialConditionAPI replace_translational_velocity(object old_name, object name = null,
                                                                  object region = null, object v1 = null,
                                                                  object v2 = null, object v3 = null,
                                                                  object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            InitialTranslationalVelocity item =
                _service.Clone<InitialTranslationalVelocity>(oldName, "translational_velocity");
            ApplyReplacedName(item, oldName, nameText, region, color);
            if (v1 != null) PythonModelArgumentCheck.SetEquation(item.V1, v1, "v1");
            if (v2 != null) PythonModelArgumentCheck.SetEquation(item.V2, v2, "v2");
            if (v3 != null)
                PythonModelArgumentCheck.SetEquation(item.V3, ToInPlane(item, item.V3,
                    PythonValueConverter.ToEquationString(v3, "v3"), "v3", null), "v3");
            //
            return Replace(oldName, item);
        }

        [PythonAPI("Replaces an initial angular velocity, keeping every value that is not given. The " +
                   "rotational_speed is in rad/s.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "axis_point: list[float] = None, axis_direction: list[float] = None, " +
                               "rotational_speed: float = None, color: list[int] or str = None", Returns = "InitialCondition")]
        public InitialConditionAPI replace_angular_velocity(object old_name, object name = null,
                                                            object region = null, object axis_point = null,
                                                            object axis_direction = null,
                                                            object rotational_speed = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            InitialAngularVelocity item =
                _service.Clone<InitialAngularVelocity>(oldName, "angular_velocity");
            ApplyReplacedName(item, oldName, nameText, region, color);
            if (axis_point != null)
                ApplyAxisPoint(item, PythonValueConverter.ToEquationArray(axis_point, "axis_point", 3));
            if (axis_direction != null)
                ApplyAxisDirection(item, PythonValueConverter.ToEquationArray(axis_direction, "axis_direction", 3));
            if (rotational_speed != null)
                PythonModelArgumentCheck.SetEquation(item.RotationalSpeed, ToRotationalSpeed(item, rotational_speed),
                                                     "rotational_speed");
            //
            return Replace(oldName, item);
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
        private void ApplyReplacedName(InitialCondition item, string oldName, string name, object region,
                                       object color)
        {
            string newName = name == null ? item.Name : name;
            _service.ValidateNewName(newName, oldName);
            item.Name = newName;
            if (region != null) _service.ApplyRegion(item, region, NodeTargets);
            ApplyColor(item, color);
        }
        private static void ApplyColor(InitialCondition item, object color)
        {
            if (color != null) item.Color = PythonColorConverter.ToColor(color, "color");
        }
        private InitialConditionAPI Replace(string oldName, InitialCondition item)
        {
            _service.Replace(oldName, item);
            //
            return this[item.Name];
        }
        private static void ApplyAxisPoint(InitialAngularVelocity item, EquationString[] point)
        {
            PythonModelArgumentCheck.SetEquation(item.X, point[0], "axis_point[0]");
            PythonModelArgumentCheck.SetEquation(item.Y, point[1], "axis_point[1]");
            PythonModelArgumentCheck.SetEquation(item.Z, ToInPlane(item, item.Z, point[2], "axis_point", "third"),
                                                 "axis_point[2]");
        }
        private static void ApplyAxisDirection(InitialAngularVelocity item, EquationString[] direction)
        {
            PythonModelArgumentCheck.SetEquation(item.N1, ToInPlane(item, item.N1, direction[0], "axis_direction",
                                                                    "first"), "axis_direction[0]");
            PythonModelArgumentCheck.SetEquation(item.N2, ToInPlane(item, item.N2, direction[1], "axis_direction",
                                                                    "second"), "axis_direction[1]");
            PythonModelArgumentCheck.SetEquation(item.N3, direction[2], "axis_direction[2]");
            PythonModelArgumentCheck.CheckNonZeroDirection(new[] { item.N1, item.N2, item.N3 }, "axis_direction");
        }
        // The model sets an out of plane value of a 2D initial condition to zero, so a given one is refused instead
        private static EquationString ToInPlane(InitialCondition item, EquationContainer container,
                                                EquationString equation, string argumentName, string component)
        {
            if (!item.TwoD) return equation;
            //
            if (PythonModelArgumentCheck.Evaluate(container, equation, argumentName) != 0)
                throw new PythonAPIException((component == null ? "The argument '" + argumentName + "'" :
                                              "The " + component + " value of the argument '" + argumentName + "'") +
                                             " must be zero in a 2D model, which has no out of plane direction.");
            //
            return equation;
        }
        private static EquationString ToRotationalSpeed(InitialAngularVelocity item, object rotationalSpeed)
        {
            EquationString equation = PythonValueConverter.ToEquationString(rotationalSpeed, "rotational_speed");
            if (PythonModelArgumentCheck.Evaluate(item.RotationalSpeed, equation, "rotational_speed") < 0)
                throw new PythonAPIException("The argument 'rotational_speed' must not be negative.");
            //
            return equation;
        }
    }
}
