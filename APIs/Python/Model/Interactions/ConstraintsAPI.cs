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
    // Every region argument is a Selection or a Region. A tie and a cyclic symmetry take a master and a
    // slave region; every other constraint takes one, and the rigid body and the couplings name a
    // reference point instead of a second region. A value that is not given keeps the model default, and
    // on a replace it keeps the value the constraint already carries.
    public class ConstraintsAPI : ModelItemsAPI<Constraint, ConstraintAPI>
    {
        // Variables
        private const string NodeTarget = "nodes";
        private const string SurfaceTarget = "surfaces";
        private const string ElementTarget = "elements";
        private readonly ConstraintService _service;


        // Constructors
        internal ConstraintsAPI(ConstraintService service)
            : base("constraint")
        {
            _service = service;
        }


        // Methods
        [PythonAPI("Adds a tie constraint using command history. The selection target is surfaces for " +
                   "both sides.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, position_tolerance: float = None, adjust: bool = None",
                   Returns = "Constraint",
                   Example = "pmx.model.constraints.add_tie('Tie-1', master, slave)")]
        public ConstraintAPI add_tie(string name, object master_region, object slave_region,
                                     object position_tolerance = null, object adjust = null)
        {
            _service.ValidateName(name, null);
            Tie constraint = new Tie(name, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                     _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyTieValues(constraint, position_tolerance, adjust);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing tie constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_tie, all optional",
                   Returns = "Constraint")]
        public ConstraintAPI replace_tie(string old_name, string name = null, object master_region = null,
                                         object slave_region = null, object position_tolerance = null,
                                         object adjust = null)
        {
            Tie constraint = _service.CloneForReplace<Tie>(old_name, "Tie");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyTieValues(constraint, position_tolerance, adjust);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a cyclic symmetry constraint using command history. The master and the slave " +
                   "region are the two sector faces and their selection target is surfaces. The region is " +
                   "the result expansion region, its selection target is elements, and it is only used " +
                   "when number_of_graph_sectors is larger than 1. The axis is given by a point and a " +
                   "direction.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, region: Selection or Region = None, axis_point: list[float] = None, " +
                               "axis_direction: list[float] = None, number_of_sectors: int = None, " +
                               "number_of_graph_sectors: int = None, check_sector_angle: bool = None, " +
                               "position_tolerance: float = None, adjust: bool = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_cyclic_symmetry(string name, object master_region, object slave_region,
                                                 object region = null, object axis_point = null,
                                                 object axis_direction = null, object number_of_sectors = null,
                                                 object number_of_graph_sectors = null,
                                                 object check_sector_angle = null,
                                                 object position_tolerance = null, object adjust = null)
        {
            _service.ValidateName(name, null);
            CyclicSymmetry constraint =
                new CyclicSymmetry(name, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                   _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyCyclicSymmetryValues(constraint, axis_point, axis_direction, number_of_sectors,
                                      number_of_graph_sectors, check_sector_angle);
            ApplyExpansionRegion(constraint, region);
            ApplyTieValues(constraint, position_tolerance, adjust);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing cyclic symmetry constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_cyclic_symmetry, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_cyclic_symmetry(string old_name, string name = null,
                                                     object master_region = null, object slave_region = null,
                                                     object region = null, object axis_point = null,
                                                     object axis_direction = null,
                                                     object number_of_sectors = null,
                                                     object number_of_graph_sectors = null,
                                                     object check_sector_angle = null,
                                                     object position_tolerance = null, object adjust = null)
        {
            CyclicSymmetry constraint = _service.CloneForReplace<CyclicSymmetry>(old_name, "CyclicSymmetry");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyCyclicSymmetryValues(constraint, axis_point, axis_direction, number_of_sectors,
                                      number_of_graph_sectors, check_sector_angle);
            ApplyExpansionRegion(constraint, region);
            ApplyTieValues(constraint, position_tolerance, adjust);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a joint constraint using command history. The selection target is surfaces for both " +
                   "sides. The joint locks the relative motion of the two surfaces about the joint centre as a " +
                   "Hinge, Cylindrical, Slider, Ball or Weld. The centre and the axis direction default to the " +
                   "centroid and the axis of the master surface, which must then be cylindrical; a given center or " +
                   "axis_direction switches that value to user defined. The formulation is RigidBody, two rigid " +
                   "bodies coupled by equations, or Springs, axial springs from every surface node to nodes on the " +
                   "joint axis tied by equations, which needs no rigid body and solves on shell elements; its " +
                   "stiffness comes from the model unless a stiffness is given.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, joint_type: str = None, center_defined_by: str = None, " +
                               "center: list[float] = None, axis_defined_by: str = None, " +
                               "axis_direction: list[float] = None, formulation: str = None, " +
                               "stiffness_defined_by: str = None, stiffness: float or str = None",
                   Returns = "Constraint",
                   Example = "pmx.model.constraints.add_joint('Hinge-1', bore_a, bore_b, joint_type='Hinge')")]
        public ConstraintAPI add_joint(string name, object master_region, object slave_region, object joint_type = null,
                                       object center_defined_by = null, object center = null,
                                       object axis_defined_by = null, object axis_direction = null,
                                       object formulation = null, object stiffness_defined_by = null,
                                       object stiffness = null)
        {
            _service.ValidateName(name, null);
            Joint constraint = new Joint(name, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                         _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyJointValues(constraint, joint_type, center_defined_by, center, axis_defined_by, axis_direction,
                             formulation, stiffness_defined_by, stiffness);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing joint constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_joint, all optional",
                   Returns = "Constraint")]
        public ConstraintAPI replace_joint(string old_name, string name = null, object master_region = null,
                                           object slave_region = null, object joint_type = null,
                                           object center_defined_by = null, object center = null,
                                           object axis_defined_by = null, object axis_direction = null,
                                           object formulation = null, object stiffness_defined_by = null,
                                           object stiffness = null)
        {
            Joint constraint = _service.CloneForReplace<Joint>(old_name, "Joint");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyJointValues(constraint, joint_type, center_defined_by, center, axis_defined_by, axis_direction,
                             formulation, stiffness_defined_by, stiffness);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a rigid body constraint using command history. The selection target is nodes.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region",
                   Returns = "Constraint")]
        public ConstraintAPI add_rigid_body(string name, object reference_point_name, object region)
        {
            _service.ValidateName(name, null);
            RigidBody constraint =
                new RigidBody(name, _service.ResolveReferencePointName(reference_point_name), null,
                              RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, NodeTarget);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing rigid body constraint using command history.",
                   Arguments = "old_name: str, name: str = None, reference_point_name: str = None, " +
                               "region: Selection or Region = None", Returns = "Constraint")]
        public ConstraintAPI replace_rigid_body(string old_name, string name = null,
                                                object reference_point_name = null, object region = null)
        {
            RigidBody constraint = _service.CloneForReplace<RigidBody>(old_name, "RigidBody");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, NodeTarget);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a kinematic coupling constraint using command history. The selection target is " +
                   "surfaces.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region, " +
                               "u1: bool = None, u2: bool = None, u3: bool = None", Returns = "Constraint")]
        public ConstraintAPI add_kinematic_coupling(string name, object reference_point_name, object region,
                                                    object u1 = null, object u2 = null, object u3 = null)
        {
            _service.ValidateName(name, null);
            KinematicCoupling constraint =
                new KinematicCoupling(name, _service.ResolveReferencePointName(reference_point_name), null,
                                      RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            if (u1 != null) constraint.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) constraint.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) constraint.U3 = PythonValueConverter.ToBool(u3, "u3");
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing kinematic coupling constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_kinematic_coupling, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_kinematic_coupling(string old_name, string name = null,
                                                        object reference_point_name = null,
                                                        object region = null, object u1 = null,
                                                        object u2 = null, object u3 = null)
        {
            KinematicCoupling constraint =
                _service.CloneForReplace<KinematicCoupling>(old_name, "KinematicCoupling");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            if (u1 != null) constraint.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) constraint.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) constraint.U3 = PythonValueConverter.ToBool(u3, "u3");
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a distributing coupling constraint using command history. The selection target is " +
                   "surfaces.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region, " +
                               "u1: bool = None, u2: bool = None, u3: bool = None, ur1: bool = None, " +
                               "ur2: bool = None, ur3: bool = None", Returns = "Constraint")]
        public ConstraintAPI add_distributing_coupling(string name, object reference_point_name, object region,
                                                       object u1 = null, object u2 = null, object u3 = null,
                                                       object ur1 = null, object ur2 = null,
                                                       object ur3 = null)
        {
            _service.ValidateName(name, null);
            DistributingCoupling constraint =
                new DistributingCoupling(name, _service.ResolveReferencePointName(reference_point_name), null,
                                         RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyDistributingValues(constraint, u1, u2, u3, ur1, ur2, ur3);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing distributing coupling constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_distributing_coupling, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_distributing_coupling(string old_name, string name = null,
                                                           object reference_point_name = null,
                                                           object region = null, object u1 = null,
                                                           object u2 = null, object u3 = null,
                                                           object ur1 = null, object ur2 = null,
                                                           object ur3 = null)
        {
            DistributingCoupling constraint =
                _service.CloneForReplace<DistributingCoupling>(old_name, "DistributingCoupling");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyDistributingValues(constraint, u1, u2, u3, ur1, ur2, ur3);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a compression only constraint using command history. The selection target is " +
                   "surfaces.",
                   Arguments = "name: str, region: Selection or Region, clearance: float = None, " +
                               "spring_stiffness: float = None, " +
                               "tensile_force_at_negative_infinity: float = None, offset: float = None, " +
                               "non_linear: bool = None", Returns = "Constraint")]
        public ConstraintAPI add_compression_only(string name, object region, object clearance = null,
                                                  object spring_stiffness = null,
                                                  object tensile_force_at_negative_infinity = null,
                                                  object offset = null, object non_linear = null)
        {
            _service.ValidateName(name, null);
            CompressionOnly constraint =
                new CompressionOnly(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyCompressionOnlyValues(constraint, clearance, spring_stiffness,
                                       tensile_force_at_negative_infinity, offset, non_linear);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing compression only constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_compression_only, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_compression_only(string old_name, string name = null, object region = null,
                                                      object clearance = null, object spring_stiffness = null,
                                                      object tensile_force_at_negative_infinity = null,
                                                      object offset = null, object non_linear = null)
        {
            CompressionOnly constraint = _service.CloneForReplace<CompressionOnly>(old_name, "CompressionOnly");
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyCompressionOnlyValues(constraint, clearance, spring_stiffness,
                                       tensile_force_at_negative_infinity, offset, non_linear);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a point spring constraint using command history. The selection target is nodes.",
                   Arguments = "name: str, region: Selection or Region, k1: float = None, " +
                               "k2: float = None, k3: float = None", Returns = "Constraint")]
        public ConstraintAPI add_point_spring(string name, object region, object k1 = null, object k2 = null,
                                              object k3 = null)
        {
            _service.ValidateName(name, null);
            PointSpring constraint =
                new PointSpring(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional(), true);
            _service.ApplyRegion(constraint, region, NodeTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing point spring constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_point_spring, " +
                               "all optional", Returns = "Constraint")]
        public ConstraintAPI replace_point_spring(string old_name, string name = null, object region = null,
                                                  object k1 = null, object k2 = null, object k3 = null)
        {
            PointSpring constraint = _service.CloneForReplace<PointSpring>(old_name, "PointSpring");
            if (region != null) _service.ApplyRegion(constraint, region, NodeTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Adds a surface spring constraint using command history. The selection target is " +
                   "surfaces.",
                   Arguments = "name: str, region: Selection or Region, k1: float = None, " +
                               "k2: float = None, k3: float = None, stiffness_per_area: bool = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_surface_spring(string name, object region, object k1 = null, object k2 = null,
                                                object k3 = null, object stiffness_per_area = null)
        {
            _service.ValidateName(name, null);
            SurfaceSpring constraint =
                new SurfaceSpring(name, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional(), true);
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            if (stiffness_per_area != null)
                constraint.StiffnessPerArea =
                    PythonValueConverter.ToBool(stiffness_per_area, "stiffness_per_area");
            _service.Add(constraint);
            //
            return this[name];
        }

        [PythonAPI("Replaces an existing surface spring constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_surface_spring, " +
                               "all optional", Returns = "Constraint")]
        public ConstraintAPI replace_surface_spring(string old_name, string name = null, object region = null,
                                                    object k1 = null, object k2 = null, object k3 = null,
                                                    object stiffness_per_area = null)
        {
            SurfaceSpring constraint = _service.CloneForReplace<SurfaceSpring>(old_name, "SurfaceSpring");
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            if (stiffness_per_area != null)
                constraint.StiffnessPerArea =
                    PythonValueConverter.ToBool(stiffness_per_area, "stiffness_per_area");
            //
            return Replace(old_name, name, constraint);
        }

        [PythonAPI("Swaps the master and the slave side of the named constraints using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void swap_master_slave(object names)
        {
            _service.SwapMasterSlave(ResolveNames(names, "names"));
        }

        [PythonAPI("Merges the named constraints that share a master and a slave region, using command " +
                   "history.", Arguments = "names: list[str] or str", Returns = "None")]
        public void merge_by_master_slave(object names)
        {
            _service.MergeByMasterSlave(ResolveNames(names, "names"));
        }

        [PythonAPI("Hides the symbols of the named constraints using command history.",
                   Arguments = "names: list[str] or str", Returns = "None")]
        public void hide(object names)
        {
            _service.Hide(ResolveNames(names, "names"));
        }

        [PythonAPI("Shows the symbols of the named constraints using command history.",
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
        protected override Constraint GetItem(string name)
        {
            return _service.GetConstraint(name);
        }
        protected override ConstraintAPI CreateItemAPI(string name)
        {
            return new ConstraintAPI(name, _service);
        }
        protected override void DuplicateItems(string[] names)
        {
            _service.Duplicate(names);
        }
        protected override void SetItemsActive(Constraint[] items, bool active)
        {
            _service.SetActive(items, active);
        }
        protected override void RemoveItems(string[] names)
        {
            _service.Remove(names);
        }
        protected override void RenameItem(Constraint item, string newName)
        {
            _service.Rename(item, newName);
        }
        public override string ToString()
        {
            string[] names = GetItemNames();
            return "Constraints(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        //
        private static void ApplyTieValues(Tie constraint, object positionTolerance, object adjust)
        {
            if (positionTolerance != null)
                constraint.PositionTolerance.Equation =
                    PythonValueConverter.ToEquationString(positionTolerance, "position_tolerance");
            if (adjust != null) constraint.Adjust = PythonValueConverter.ToBool(adjust, "adjust");
        }
        private static void ApplyJointValues(Joint constraint, object jointType, object centerDefinedBy, object center,
                                             object axisDefinedBy, object axisDirection, object formulation,
                                             object stiffnessDefinedBy, object stiffness)
        {
            if (formulation != null)
                constraint.Formulation =
                    PythonValueConverter.ToEnum<JointFormulationEnum>(AsString(formulation, "formulation"), "formulation");
            // A given stiffness is the user defined one unless the definition says otherwise
            if (stiffness != null)
            {
                constraint.Stiffness.Equation = PythonValueConverter.ToEquationString(stiffness, "stiffness");
                constraint.StiffnessDefinition = JointStiffnessDefinitionEnum.UserDefined;
            }
            if (stiffnessDefinedBy != null)
                constraint.StiffnessDefinition =
                    PythonValueConverter.ToEnum<JointStiffnessDefinitionEnum>(AsString(stiffnessDefinedBy, "stiffness_defined_by"),
                                                                              "stiffness_defined_by");
            if (constraint.HasUserDefinedStiffness)
            {
                double value = constraint.Stiffness.Value;
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                    throw new PythonAPIException("The argument 'stiffness' must be positive.");
            }
            //
            if (jointType != null)
                constraint.JointType =
                    PythonValueConverter.ToEnum<JointTypeEnum>(AsString(jointType, "joint_type"), "joint_type");
            // A given value is the user defined one unless the definition says otherwise
            if (center != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(center, "center", 3);
                constraint.X.Equation = values[0];
                constraint.Y.Equation = values[1];
                constraint.Z.Equation = values[2];
                constraint.CenterDefinition = JointCenterDefinitionEnum.Coordinates;
            }
            if (centerDefinedBy != null)
            {
                constraint.CenterDefinition =
                    PythonValueConverter.ToEnum<JointCenterDefinitionEnum>(AsString(centerDefinedBy, "center_defined_by"),
                                                                           "center_defined_by");
                // The point selection modes pick nodes in the viewport, which a script cannot do; they resolve
                // to the coordinates a script gives directly
                if (constraint.IsCenterFromSelection)
                    throw new PythonAPIException("The argument 'center_defined_by' accepts 'MasterSurface' or " +
                                                 "'Coordinates'. The value '" + constraint.CenterDefinition +
                                                 "' selects the joint centre by picking nodes in the dialog; from a " +
                                                 "script give the argument 'center' instead.");
            }
            if (axisDirection != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axisDirection, "axis_direction", 3);
                constraint.N1.Equation = values[0];
                constraint.N2.Equation = values[1];
                constraint.N3.Equation = values[2];
                constraint.AxisDefinition = JointDefinitionEnum.UserDefined;
            }
            if (axisDefinedBy != null)
                constraint.AxisDefinition =
                    PythonValueConverter.ToEnum<JointDefinitionEnum>(AsString(axisDefinedBy, "axis_defined_by"),
                                                                     "axis_defined_by");
            //
            if (constraint.HasUserDefinedAxis)
            {
                double[] axis = constraint.GetAxisDirection();
                double lengthSquared = axis[0] * axis[0] + axis[1] * axis[1] + axis[2] * axis[2];
                if (double.IsNaN(lengthSquared) || double.IsInfinity(lengthSquared) || lengthSquared <= 1E-24)
                    throw new PythonAPIException("The argument 'axis_direction' must be non-zero.");
            }
        }
        private static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
        private static void ApplyCyclicSymmetryValues(CyclicSymmetry constraint, object axisPoint,
                                                      object axisDirection, object numberOfSectors,
                                                      object numberOfGraphSectors, object checkSectorAngle)
        {
            if (axisPoint != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axisPoint, "axis_point", 3);
                constraint.X.Equation = values[0];
                constraint.Y.Equation = values[1];
                constraint.Z.Equation = values[2];
            }
            if (axisDirection != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axisDirection, "axis_direction", 3);
                constraint.N1.Equation = values[0];
                constraint.N2.Equation = values[1];
                constraint.N3.Equation = values[2];
            }
            if (numberOfSectors != null)
                constraint.NumberOfSectors = PythonValueConverter.ToInt(numberOfSectors, "number_of_sectors");
            if (numberOfGraphSectors != null)
                constraint.NumberOfGraphSectors =
                    PythonValueConverter.ToInt(numberOfGraphSectors, "number_of_graph_sectors");
            if (checkSectorAngle != null)
                constraint.CheckSectorAngle =
                    PythonValueConverter.ToBool(checkSectorAngle, "check_sector_angle");
        }
        private void ApplyExpansionRegion(CyclicSymmetry constraint, object region)
        {
            if (region == null) return;
            // The controller converts the result expansion region only for more than one graph sector,
            // and discards it otherwise, so the combination that would be dropped is rejected here
            if (constraint.NumberOfGraphSectors <= 1)
                throw new PythonAPIException("The result expansion region of a cyclic symmetry is only " +
                                             "used when 'number_of_graph_sectors' is larger than 1.");
            //
            _service.ApplyRegion(constraint, region, ElementTarget);
        }
        private static void ApplyDistributingValues(DistributingCoupling constraint, object u1, object u2,
                                                    object u3, object ur1, object ur2, object ur3)
        {
            if (u1 != null) constraint.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) constraint.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) constraint.U3 = PythonValueConverter.ToBool(u3, "u3");
            if (ur1 != null) constraint.UR1 = PythonValueConverter.ToBool(ur1, "ur1");
            if (ur2 != null) constraint.UR2 = PythonValueConverter.ToBool(ur2, "ur2");
            if (ur3 != null) constraint.UR3 = PythonValueConverter.ToBool(ur3, "ur3");
        }
        private static void ApplyCompressionOnlyValues(CompressionOnly constraint, object clearance,
                                                       object springStiffness,
                                                       object tensileForceAtNegativeInfinity, object offset,
                                                       object nonLinear)
        {
            if (clearance != null)
                constraint.Clearance.Equation =
                    PythonValueConverter.ToEquationString(clearance, "clearance");
            if (springStiffness != null)
                constraint.SpringStiffness.Equation =
                    PythonValueConverter.ToEquationString(springStiffness, "spring_stiffness");
            if (tensileForceAtNegativeInfinity != null)
                constraint.TensileForceAtNegativeInfinity.Equation =
                    PythonValueConverter.ToEquationString(tensileForceAtNegativeInfinity,
                                                          "tensile_force_at_negative_infinity");
            if (offset != null)
                constraint.Offset.Equation = PythonValueConverter.ToEquationString(offset, "offset");
            if (nonLinear != null)
                constraint.NonLinear = PythonValueConverter.ToBool(nonLinear, "non_linear");
        }
        private static void ApplySpringValues(SpringConstraint constraint, object k1, object k2, object k3)
        {
            if (k1 != null) constraint.K1.Equation = PythonValueConverter.ToEquationString(k1, "k1");
            if (k2 != null) constraint.K2.Equation = PythonValueConverter.ToEquationString(k2, "k2");
            if (k3 != null) constraint.K3.Equation = PythonValueConverter.ToEquationString(k3, "k3");
        }
        private ConstraintAPI Replace(string oldName, string name, Constraint constraint)
        {
            if (name != null)
            {
                _service.ValidateName(name, oldName);
                constraint.Name = name;
            }
            _service.Replace(oldName, constraint);
            //
            return this[constraint.Name];
        }
    }
}
