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
    // The region is a selection created by any pmx.create.selection_* function, or a named region created by
    // pmx.create.region. A value that is not given keeps the model default, and on a replace it keeps
    // the value the boundary condition already carries.
    public class BoundaryConditionsAPI : StepItemsAPI<BoundaryCondition, BoundaryConditionAPI>
    {
        // Variables
        private readonly BoundaryConditionService _service;


        // Constructors
        internal BoundaryConditionsAPI(string stepName, BoundaryConditionService service)
            : base("boundary condition", stepName)
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a fixed boundary condition using command history. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, color: list[int] or str = None",
                   Returns = "BoundaryCondition",
                   Example = "step.bcs.add_fixed('Fix-1', pmx.create.region('NodeSetName', 'Bottom'))")]
        public BoundaryConditionAPI add_fixed(string name, object region, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            FixedBC boundaryCondition =
                new FixedBC(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(boundaryCondition, region, "nodes");
            _service.ApplyCommonValues(boundaryCondition, null, color);
            _service.Add(StepName, boundaryCondition);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing fixed boundary condition using command history.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "color: list[int] or str = None", Returns = "BoundaryCondition")]
        public BoundaryConditionAPI replace_fixed(string old_name, string name = null, object region = null,
                                                  object color = null)
        {
            FixedBC boundaryCondition = _service.CloneForReplace<FixedBC>(StepName, old_name, "fixed");
            if (region != null) _service.ApplyRegion(boundaryCondition, region, "nodes");
            _service.ApplyCommonValues(boundaryCondition, null, color);
            //
            return Replace(old_name, name, boundaryCondition);
        }

        [PythonAPI("Adds a displacement and rotation boundary condition using command history. A degree of " +
                   "freedom that is not given is left unconstrained, and a value of 0 fixes it. A scalar " +
                   "distribution scales every prescribed degree of freedom, a vector one scales u1, u2 and u3 by " +
                   "its three components; either needs a nonzero prescribed value and a region that is not a " +
                   "reference point. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, u1: float = None, u2: float = None, " +
                               "u3: float = None, ur1: float = None, ur2: float = None, ur3: float = None, " +
                               "distribution_name: str = None, " +
                               "amplitude_name: str = None, coordinate_system_name: str = None, " +
                               "phase_deg: float = None, color: list[int] or str = None",
                   Returns = "BoundaryCondition",
                   Example = "step.bcs.add_displacement_rotation('BC-1', selection, u1=0, u2=0, u3=0)")]
        public BoundaryConditionAPI add_displacement_rotation(string name, object region, object u1 = null,
                                                              object u2 = null, object u3 = null,
                                                              object ur1 = null, object ur2 = null,
                                                              object ur3 = null, object distribution_name = null,
                                                              object amplitude_name = null,
                                                              object coordinate_system_name = null,
                                                              object phase_deg = null, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            // The dialog derives the complex flag from the step type
            DisplacementRotation boundaryCondition =
                new DisplacementRotation(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional(),
                                         _service.GetIsComplex(StepName), 0);
            _service.ApplyRegion(boundaryCondition, region, "nodes");
            ApplyDegreesOfFreedom(boundaryCondition, u1, u2, u3, ur1, ur2, ur3);
            if (distribution_name != null)
                boundaryCondition.DistributionName = _service.ResolveDistributionName(distribution_name);
            _service.ApplyAmplitudeName(boundaryCondition, amplitude_name);
            _service.ApplyCoordinateSystemName(boundaryCondition, coordinate_system_name);
            _service.ApplyCommonValues(boundaryCondition, phase_deg, color);
            _service.EnsureDistributionApplicable(boundaryCondition);
            _service.Add(StepName, boundaryCondition);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing displacement and rotation boundary condition using command history. " +
                   "A degree of freedom that is not given keeps its value; give free_dofs to unconstrain one. " +
                   "A degree of freedom named in free_dofs cannot also be given a value.",
                   Arguments = "old_name: str, name: str = None, the arguments of " +
                               "add_displacement_rotation, and free_dofs: list[str] = None",
                   Returns = "BoundaryCondition")]
        public BoundaryConditionAPI replace_displacement_rotation(string old_name, string name = null,
                                                                  object region = null, object u1 = null,
                                                                  object u2 = null, object u3 = null,
                                                                  object ur1 = null, object ur2 = null,
                                                                  object ur3 = null,
                                                                  object distribution_name = null,
                                                                  object amplitude_name = null,
                                                                  object coordinate_system_name = null,
                                                                  object phase_deg = null, object color = null,
                                                                  object free_dofs = null)
        {
            DisplacementRotation boundaryCondition =
                _service.CloneForReplace<DisplacementRotation>(StepName, old_name, "displacement_rotation");
            if (region != null) _service.ApplyRegion(boundaryCondition, region, "nodes");
            ApplyDegreesOfFreedom(boundaryCondition, u1, u2, u3, ur1, ur2, ur3);
            if (free_dofs != null) FreeDegreesOfFreedom(boundaryCondition, free_dofs, u1, u2, u3, ur1, ur2, ur3);
            if (distribution_name != null)
                boundaryCondition.DistributionName = _service.ResolveDistributionName(distribution_name);
            _service.ApplyAmplitudeName(boundaryCondition, amplitude_name);
            _service.ApplyCoordinateSystemName(boundaryCondition, coordinate_system_name);
            _service.ApplyCommonValues(boundaryCondition, phase_deg, color);
            _service.EnsureDistributionApplicable(boundaryCondition);
            //
            return Replace(old_name, name, boundaryCondition);
        }

        [PythonAPI("Adds a submodel boundary condition using command history. The degrees of freedom are " +
                   "read from the global model results of the given step number. The selection target is " +
                   "nodes.",
                   Arguments = "name: str, region: Selection or Region, step_id: int = None, " +
                               "u1: bool = None, u2: bool = None, u3: bool = None, ur1: bool = None, " +
                               "ur2: bool = None, ur3: bool = None, color: list[int] or str = None",
                   Returns = "BoundaryCondition")]
        public BoundaryConditionAPI add_submodel(string name, object region, object step_id = null,
                                                 object u1 = null, object u2 = null, object u3 = null,
                                                 object ur1 = null, object ur2 = null, object ur3 = null,
                                                 object color = null)
        {
            _service.ValidateName(StepName, name, null);
            SubmodelBC boundaryCondition =
                new SubmodelBC(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(boundaryCondition, region, "nodes");
            ApplySubmodelValues(boundaryCondition, step_id, u1, u2, u3, ur1, ur2, ur3);
            _service.ApplyCommonValues(boundaryCondition, null, color);
            _service.Add(StepName, boundaryCondition);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing submodel boundary condition using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_submodel",
                   Returns = "BoundaryCondition")]
        public BoundaryConditionAPI replace_submodel(string old_name, string name = null, object region = null,
                                                     object step_id = null, object u1 = null,
                                                     object u2 = null, object u3 = null, object ur1 = null,
                                                     object ur2 = null, object ur3 = null, object color = null)
        {
            SubmodelBC boundaryCondition = _service.CloneForReplace<SubmodelBC>(StepName, old_name, "submodel");
            if (region != null) _service.ApplyRegion(boundaryCondition, region, "nodes");
            ApplySubmodelValues(boundaryCondition, step_id, u1, u2, u3, ur1, ur2, ur3);
            _service.ApplyCommonValues(boundaryCondition, null, color);
            //
            return Replace(old_name, name, boundaryCondition);
        }

        [PythonAPI("Adds a temperature boundary condition using command history. The selection target is " +
                   "nodes.",
                   Arguments = "name: str, region: Selection or Region, temperature: float, " +
                               "distribution_name: str = None, amplitude_name: str = None, " +
                               "color: list[int] or str = None", Returns = "BoundaryCondition",
                   Example = "step.bcs.add_temperature('BC-2', selection, 100)")]
        public BoundaryConditionAPI add_temperature(string name, object region, object temperature,
                                                    object distribution_name = null,
                                                    object amplitude_name = null, object color = null)
        {
            _service.ValidateName(StepName, name, null);
            // The constructor takes a plain value, while the temperature can also be an equation
            TemperatureBC boundaryCondition =
                new TemperatureBC(name, null, RegionTypeEnum.Selection, 0, _service.GetIsTwoDimensional());
            boundaryCondition.Temperature.Equation =
                PythonValueConverter.ToEquationString(temperature, "temperature");
            _service.ApplyRegion(boundaryCondition, region, "nodes");
            if (distribution_name != null)
                boundaryCondition.DistributionName =
                    _service.ResolveScalarDistributionName(distribution_name, "a temperature");
            _service.ApplyAmplitudeName(boundaryCondition, amplitude_name);
            _service.ApplyCommonValues(boundaryCondition, null, color);
            _service.Add(StepName, boundaryCondition);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing temperature boundary condition using command history.",
                   Arguments = "old_name: str, name: str = None, region: Selection or Region = None, " +
                               "temperature: float = None, distribution_name: str = None, " +
                               "amplitude_name: str = None, color: list[int] or str = None",
                   Returns = "BoundaryCondition")]
        public BoundaryConditionAPI replace_temperature(string old_name, string name = null, object region = null,
                                                        object temperature = null,
                                                        object distribution_name = null,
                                                        object amplitude_name = null, object color = null)
        {
            TemperatureBC boundaryCondition =
                _service.CloneForReplace<TemperatureBC>(StepName, old_name, "temperature");
            if (region != null) _service.ApplyRegion(boundaryCondition, region, "nodes");
            if (temperature != null)
                boundaryCondition.Temperature.Equation =
                    PythonValueConverter.ToEquationString(temperature, "temperature");
            if (distribution_name != null)
                boundaryCondition.DistributionName =
                    _service.ResolveScalarDistributionName(distribution_name, "a temperature");
            _service.ApplyAmplitudeName(boundaryCondition, amplitude_name);
            _service.ApplyCommonValues(boundaryCondition, null, color);
            //
            return Replace(old_name, name, boundaryCondition);
        }

        [PythonAPI("Copies the boundary condition to every following step using command history.",
                   Arguments = "name: str", Returns = "None")]
        public void propagate(string name)
        {
            _service.Propagate(StepName, name);
        }

        [PythonAPI("Hides the symbols of the named boundary conditions using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(StepName, ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the symbols of the named boundary conditions using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void show(object names)
        {
            _service.Show(StepName, ResolveNames(names, "names"));
        }
        //
        protected override string[] GetItemNames()
        {
            return _service.GetNames(StepName);
        }
        protected override BoundaryCondition GetItem(string name)
        {
            return _service.GetItem(StepName, name);
        }
        protected override BoundaryConditionAPI CreateItemAPI(string name)
        {
            return new BoundaryConditionAPI(StepName, name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(StepName, names);
        }
        protected override void SetItemsActive(BoundaryCondition[] items, bool active)
        {
            _service.SetActive(StepName, items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(StepName, names);
        }
        protected override void RenameItem(BoundaryCondition item, string newName)
        {
            _service.Rename(StepName, item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "BoundaryConditions(step=" + StringListAPI.ToDisplayString(StepName) + ", count=" +
                   names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private static void ApplyDegreesOfFreedom(DisplacementRotation boundaryCondition, object u1, object u2,
                                                  object u3, object ur1, object ur2, object ur3)
        {
            if (u1 != null) boundaryCondition.U1.Equation = PythonValueConverter.ToEquationString(u1, "u1");
            if (u2 != null) boundaryCondition.U2.Equation = PythonValueConverter.ToEquationString(u2, "u2");
            if (u3 != null) boundaryCondition.U3.Equation = PythonValueConverter.ToEquationString(u3, "u3");
            if (ur1 != null) boundaryCondition.UR1.Equation = PythonValueConverter.ToEquationString(ur1, "ur1");
            if (ur2 != null) boundaryCondition.UR2.Equation = PythonValueConverter.ToEquationString(ur2, "ur2");
            if (ur3 != null) boundaryCondition.UR3.Equation = PythonValueConverter.ToEquationString(ur3, "ur3");
        }
        private static void FreeDegreesOfFreedom(DisplacementRotation boundaryCondition, object freeDofs,
                                                 object u1, object u2, object u3, object ur1, object ur2,
                                                 object ur3)
        {
            // An unconstrained degree of freedom carries a NaN
            string[] names = PythonStringListConverter.ToStringArrayOrSingle(freeDofs, "free_dofs");
            foreach (string dof in names)
            {
                switch (dof.Trim().ToUpperInvariant())
                {
                    case "U1":
                        RefuseValueOfFreeDegreeOfFreedom(u1, "u1");
                        boundaryCondition.U1.SetEquationFromValue(double.NaN);
                        break;
                    case "U2":
                        RefuseValueOfFreeDegreeOfFreedom(u2, "u2");
                        boundaryCondition.U2.SetEquationFromValue(double.NaN);
                        break;
                    case "U3":
                        RefuseValueOfFreeDegreeOfFreedom(u3, "u3");
                        boundaryCondition.U3.SetEquationFromValue(double.NaN);
                        break;
                    case "UR1":
                        RefuseValueOfFreeDegreeOfFreedom(ur1, "ur1");
                        boundaryCondition.UR1.SetEquationFromValue(double.NaN);
                        break;
                    case "UR2":
                        RefuseValueOfFreeDegreeOfFreedom(ur2, "ur2");
                        boundaryCondition.UR2.SetEquationFromValue(double.NaN);
                        break;
                    case "UR3":
                        RefuseValueOfFreeDegreeOfFreedom(ur3, "ur3");
                        boundaryCondition.UR3.SetEquationFromValue(double.NaN);
                        break;
                    default:
                        throw new PythonAPIException("The value '" + dof + "' is not supported for the " +
                                                     "argument 'free_dofs'. Possible values are U1, U2, U3, " +
                                                     "UR1, UR2 and UR3.");
                }
            }
        }
        // Freeing a degree of freedom the same call gives a value would silently discard that value
        private static void RefuseValueOfFreeDegreeOfFreedom(object value, string argumentName)
        {
            if (value == null) return;
            //
            throw new PythonAPIException("The degree of freedom " + argumentName.ToUpperInvariant() + " is given " +
                                         "a value in '" + argumentName + "' and is also named in 'free_dofs'. " +
                                         "Give it either a value or free it, not both.");
        }
        private static void ApplySubmodelValues(SubmodelBC boundaryCondition, object stepNumber, object u1,
                                                object u2, object u3, object ur1, object ur2, object ur3)
        {
            if (stepNumber != null)
                boundaryCondition.StepNumber.Equation =
                    PythonValueConverter.ToEquationString(stepNumber, "step_id");
            if (u1 != null) boundaryCondition.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) boundaryCondition.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) boundaryCondition.U3 = PythonValueConverter.ToBool(u3, "u3");
            if (ur1 != null) boundaryCondition.UR1 = PythonValueConverter.ToBool(ur1, "ur1");
            if (ur2 != null) boundaryCondition.UR2 = PythonValueConverter.ToBool(ur2, "ur2");
            if (ur3 != null) boundaryCondition.UR3 = PythonValueConverter.ToBool(ur3, "ur3");
        }
        private BoundaryConditionAPI Replace(string oldName, string name, BoundaryCondition boundaryCondition)
        {
            if (name != null)
            {
                _service.ValidateName(StepName, name, oldName);
                boundaryCondition.Name = name;
            }
            _service.Replace(StepName, oldName, boundaryCondition);
            //
            return this[boundaryCondition.Name];
        }
    }
}
