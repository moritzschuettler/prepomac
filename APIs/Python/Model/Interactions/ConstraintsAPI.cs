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
                   "both sides. A position_tolerance must be zero or larger; it is Default when not given, " +
                   "float('nan') sets it back to Default, which is how a replace clears it, and get_value reads " +
                   "Default as None. master_color and slave_color color the two surfaces, given as " +
                   "[R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, position_tolerance: float = None, adjust: bool = None, " +
                               "master_color: list[int] or str = None, slave_color: list[int] or str = None",
                   Returns = "Constraint",
                   Example = "pmx.model.constraints.add_tie('Tie-1', master, slave)")]
        public ConstraintAPI add_tie(object name, object master_region, object slave_region,
                                     object position_tolerance = null, object adjust = null,
                                     object master_color = null, object slave_color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            Tie constraint = new Tie(nameText, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                     _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyTieValues(constraint, position_tolerance, adjust);
            ApplyColors(constraint, master_color, slave_color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing tie constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_tie, all optional",
                   Returns = "Constraint")]
        public ConstraintAPI replace_tie(object old_name, object name = null, object master_region = null,
                                         object slave_region = null, object position_tolerance = null,
                                         object adjust = null, object master_color = null,
                                         object slave_color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            Tie constraint = _service.CloneForReplace<Tie>(oldName, "tie");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyTieValues(constraint, position_tolerance, adjust);
            ApplyColors(constraint, master_color, slave_color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a cyclic symmetry constraint using command history. The master and the slave " +
                   "region are the two sector faces and their selection target is surfaces. The region is " +
                   "the result expansion region, its selection target is elements, and it is required " +
                   "when number_of_graph_sectors is larger than 1 and refused otherwise. The axis is given " +
                   "by a point and a direction. number_of_sectors is at least 2 and number_of_graph_sectors " +
                   "between 1 and number_of_sectors. Cyclic symmetry is refused in a 2D model. The " +
                   "position_tolerance is taken as add_tie takes it, float('nan') meaning Default. " +
                   "master_color and slave_color color the two surfaces, given as [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, region: Selection or Region = None, axis_point: list[float] = None, " +
                               "axis_direction: list[float] = None, number_of_sectors: int = None, " +
                               "number_of_graph_sectors: int = None, check_sector_angle: bool = None, " +
                               "position_tolerance: float = None, adjust: bool = None, " +
                               "master_color: list[int] or str = None, slave_color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_cyclic_symmetry(object name, object master_region, object slave_region,
                                                 object region = null, object axis_point = null,
                                                 object axis_direction = null, object number_of_sectors = null,
                                                 object number_of_graph_sectors = null,
                                                 object check_sector_angle = null,
                                                 object position_tolerance = null, object adjust = null,
                                                 object master_color = null, object slave_color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            EnsureThreeDimensional("Cyclic symmetry");
            CyclicSymmetry constraint =
                new CyclicSymmetry(nameText, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                   _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyCyclicSymmetryValues(constraint, axis_point, axis_direction, number_of_sectors,
                                      number_of_graph_sectors, check_sector_angle);
            ApplyExpansionRegion(constraint, region);
            ApplyTieValues(constraint, position_tolerance, adjust);
            ApplyColors(constraint, master_color, slave_color);
            CheckExpansionRegion(constraint, nameText);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing cyclic symmetry constraint using command history. A replace that " +
                   "lowers number_of_graph_sectors to 1 must also drop the result expansion region, which " +
                   "clear_expansion_region=True does; it cannot be combined with region. A number_of_sectors " +
                   "below the kept number_of_graph_sectors is refused rather than lowering it; give both.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_cyclic_symmetry, all optional, clear_expansion_region: bool = None",
                   Returns = "Constraint")]
        public ConstraintAPI replace_cyclic_symmetry(object old_name, object name = null,
                                                     object master_region = null, object slave_region = null,
                                                     object region = null, object axis_point = null,
                                                     object axis_direction = null,
                                                     object number_of_sectors = null,
                                                     object number_of_graph_sectors = null,
                                                     object check_sector_angle = null,
                                                     object position_tolerance = null, object adjust = null,
                                                     object master_color = null, object slave_color = null,
                                                     object clear_expansion_region = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            CyclicSymmetry constraint = _service.CloneForReplace<CyclicSymmetry>(oldName, "cyclic_symmetry");
            bool clearExpansionRegion = clear_expansion_region != null &&
                PythonValueConverter.ToBool(clear_expansion_region, "clear_expansion_region");
            if (clearExpansionRegion && region != null)
                throw new PythonAPIException("The arguments 'region' and 'clear_expansion_region' cannot be " +
                                             "given together.");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyCyclicSymmetryValues(constraint, axis_point, axis_direction, number_of_sectors,
                                      number_of_graph_sectors, check_sector_angle);
            if (clearExpansionRegion) ClearExpansionRegion(constraint);
            ApplyExpansionRegion(constraint, region);
            ApplyTieValues(constraint, position_tolerance, adjust);
            ApplyColors(constraint, master_color, slave_color);
            CheckExpansionRegion(constraint, oldName);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a joint constraint using command history. The selection target is surfaces for both " +
                   "sides. The joint locks the relative motion of the two surfaces about the joint centre as a " +
                   "Hinge, Cylindrical, Slider, Ball or Weld. The centre and the axis direction default to the " +
                   "centroid and the axis of the master surface, which must then be cylindrical; a given center or " +
                   "axis_direction switches that value to user defined. The formulation is RigidBody, two rigid " +
                   "bodies coupled by equations, or Springs, axial springs from every surface node to nodes on the " +
                   "joint axis tied by equations, which needs no rigid body and solves on shell elements; its " +
                   "stiffness comes from the model unless a stiffness is given. stiffness and " +
                   "stiffness_defined_by are refused with the RigidBody formulation, axis_direction with a Ball " +
                   "or Weld joint, which has no axis, and a joint is refused in a 2D model. A center, " +
                   "axis_direction or stiffness given together with a definition that does not use it - " +
                   "MasterSurface or FromModel - is refused. master_color and slave_color color the two surfaces, given as " +
                   "[R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, master_region: Selection or Region, slave_region: Selection or " +
                               "Region, joint_type: str = None, center_defined_by: str = None, " +
                               "center: list[float] = None, axis_defined_by: str = None, " +
                               "axis_direction: list[float] = None, formulation: str = None, " +
                               "stiffness_defined_by: str = None, stiffness: float or str = None, " +
                               "master_color: list[int] or str = None, slave_color: list[int] or str = None",
                   Returns = "Constraint",
                   Example = "pmx.model.constraints.add_joint('Hinge-1', bore_a, bore_b, joint_type='Hinge')")]
        public ConstraintAPI add_joint(object name, object master_region, object slave_region, object joint_type = null,
                                       object center_defined_by = null, object center = null,
                                       object axis_defined_by = null, object axis_direction = null,
                                       object formulation = null, object stiffness_defined_by = null,
                                       object stiffness = null, object master_color = null,
                                       object slave_color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            EnsureThreeDimensional("A joint");
            Joint constraint = new Joint(nameText, null, RegionTypeEnum.Selection, null, RegionTypeEnum.Selection,
                                         _service.GetIsTwoDimensional());
            _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyJointValues(constraint, joint_type, center_defined_by, center, axis_defined_by, axis_direction,
                             formulation, stiffness_defined_by, stiffness);
            ApplyColors(constraint, master_color, slave_color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing joint constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_joint, all optional",
                   Returns = "Constraint")]
        public ConstraintAPI replace_joint(object old_name, object name = null, object master_region = null,
                                           object slave_region = null, object joint_type = null,
                                           object center_defined_by = null, object center = null,
                                           object axis_defined_by = null, object axis_direction = null,
                                           object formulation = null, object stiffness_defined_by = null,
                                           object stiffness = null, object master_color = null,
                                           object slave_color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            Joint constraint = _service.CloneForReplace<Joint>(oldName, "joint");
            if (master_region != null) _service.ApplyMaster(constraint, master_region, SurfaceTarget);
            if (slave_region != null) _service.ApplySlave(constraint, slave_region, SurfaceTarget);
            ApplyJointValues(constraint, joint_type, center_defined_by, center, axis_defined_by, axis_direction,
                             formulation, stiffness_defined_by, stiffness);
            ApplyColors(constraint, master_color, slave_color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a rigid body constraint using command history. The selection target is nodes. " +
                   "color is the constraint color, given as [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region, " +
                               "color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_rigid_body(object name, object reference_point_name, object region,
                                            object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            RigidBody constraint =
                new RigidBody(nameText, _service.ResolveReferencePointName(reference_point_name), null,
                              RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, NodeTarget);
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing rigid body constraint using command history.",
                   Arguments = "old_name: str, name: str = None, reference_point_name: str = None, " +
                               "region: Selection or Region = None, color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI replace_rigid_body(object old_name, object name = null,
                                                object reference_point_name = null, object region = null,
                                                object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            RigidBody constraint = _service.CloneForReplace<RigidBody>(oldName, "rigid_body");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, NodeTarget);
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a kinematic coupling constraint using command history. The selection target is " +
                   "surfaces. In a 2D model u3=True is refused. color is the constraint color, given as " +
                   "[R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region, " +
                               "u1: bool = None, u2: bool = None, u3: bool = None, color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_kinematic_coupling(object name, object reference_point_name, object region,
                                                    object u1 = null, object u2 = null, object u3 = null,
                                                    object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            KinematicCoupling constraint =
                new KinematicCoupling(nameText, _service.ResolveReferencePointName(reference_point_name), null,
                                      RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyKinematicValues(constraint, u1, u2, u3);
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing kinematic coupling constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_kinematic_coupling, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_kinematic_coupling(object old_name, object name = null,
                                                        object reference_point_name = null,
                                                        object region = null, object u1 = null,
                                                        object u2 = null, object u3 = null,
                                                        object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            KinematicCoupling constraint =
                _service.CloneForReplace<KinematicCoupling>(oldName, "kinematic_coupling");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyKinematicValues(constraint, u1, u2, u3);
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a distributing coupling constraint using command history. The selection target is " +
                   "surfaces. In a 2D model u3, ur1 and ur2 set to True are refused. " +
                   "color is the constraint color, given as [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, reference_point_name: str, region: Selection or Region, " +
                               "u1: bool = None, u2: bool = None, u3: bool = None, ur1: bool = None, " +
                               "ur2: bool = None, ur3: bool = None, color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_distributing_coupling(object name, object reference_point_name, object region,
                                                       object u1 = null, object u2 = null, object u3 = null,
                                                       object ur1 = null, object ur2 = null,
                                                       object ur3 = null, object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            DistributingCoupling constraint =
                new DistributingCoupling(nameText, _service.ResolveReferencePointName(reference_point_name), null,
                                         RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyDistributingValues(constraint, u1, u2, u3, ur1, ur2, ur3);
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing distributing coupling constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_distributing_coupling, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_distributing_coupling(object old_name, object name = null,
                                                           object reference_point_name = null,
                                                           object region = null, object u1 = null,
                                                           object u2 = null, object u3 = null,
                                                           object ur1 = null, object ur2 = null,
                                                           object ur3 = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            DistributingCoupling constraint =
                _service.CloneForReplace<DistributingCoupling>(oldName, "distributing_coupling");
            if (reference_point_name != null)
                constraint.ReferencePointName = _service.ResolveReferencePointName(reference_point_name);
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyDistributingValues(constraint, u1, u2, u3, ur1, ur2, ur3);
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a compression only constraint using command history. The selection target is " +
                   "surfaces. spring_stiffness and tensile_force_at_negative_infinity are Default when not " +
                   "given; float('nan') sets either back to Default, which is how a replace clears it, and " +
                   "get_value reads Default as None. color is the constraint color, given as [R, G, B], " +
                   "[R, G, B, A] or a color name.",
                   Arguments = "name: str, region: Selection or Region, clearance: float = None, " +
                               "spring_stiffness: float = None, " +
                               "tensile_force_at_negative_infinity: float = None, offset: float = None, " +
                               "non_linear: bool = None, color: list[int] or str = None", Returns = "Constraint")]
        public ConstraintAPI add_compression_only(object name, object region, object clearance = null,
                                                  object spring_stiffness = null,
                                                  object tensile_force_at_negative_infinity = null,
                                                  object offset = null, object non_linear = null,
                                                  object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            CompressionOnly constraint =
                new CompressionOnly(nameText, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional());
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyCompressionOnlyValues(constraint, clearance, spring_stiffness,
                                       tensile_force_at_negative_infinity, offset, non_linear);
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing compression only constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of " +
                               "add_compression_only, all optional", Returns = "Constraint")]
        public ConstraintAPI replace_compression_only(object old_name, object name = null, object region = null,
                                                      object clearance = null, object spring_stiffness = null,
                                                      object tensile_force_at_negative_infinity = null,
                                                      object offset = null, object non_linear = null,
                                                      object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            CompressionOnly constraint = _service.CloneForReplace<CompressionOnly>(oldName, "compression_only");
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplyCompressionOnlyValues(constraint, clearance, spring_stiffness,
                                       tensile_force_at_negative_infinity, offset, non_linear);
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a point spring constraint using command history. The selection target is nodes. " +
                   "A stiffness must be zero or larger, and k3 must be zero in a 2D model. " +
                   "color is the constraint color, given as [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, region: Selection or Region, k1: float = None, " +
                               "k2: float = None, k3: float = None, color: list[int] or str = None", Returns = "Constraint")]
        public ConstraintAPI add_point_spring(object name, object region, object k1 = null, object k2 = null,
                                              object k3 = null, object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            PointSpring constraint =
                new PointSpring(nameText, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional(), true);
            _service.ApplyRegion(constraint, region, NodeTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing point spring constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_point_spring, " +
                               "all optional", Returns = "Constraint")]
        public ConstraintAPI replace_point_spring(object old_name, object name = null, object region = null,
                                                  object k1 = null, object k2 = null, object k3 = null,
                                                  object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            PointSpring constraint = _service.CloneForReplace<PointSpring>(oldName, "point_spring");
            if (region != null) _service.ApplyRegion(constraint, region, NodeTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
        }

        [PythonAPI("Adds a surface spring constraint using command history. The selection target is " +
                   "surfaces. A stiffness must be zero or larger, and k3 must be zero in a 2D model. " +
                   "color is the constraint color, given as [R, G, B], [R, G, B, A] or a color name.",
                   Arguments = "name: str, region: Selection or Region, k1: float = None, " +
                               "k2: float = None, k3: float = None, stiffness_per_area: bool = None, " +
                               "color: list[int] or str = None",
                   Returns = "Constraint")]
        public ConstraintAPI add_surface_spring(object name, object region, object k1 = null, object k2 = null,
                                                object k3 = null, object stiffness_per_area = null,
                                                object color = null)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            _service.ValidateName(nameText, null);
            SurfaceSpring constraint =
                new SurfaceSpring(nameText, null, RegionTypeEnum.Selection, _service.GetIsTwoDimensional(), true);
            _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            if (stiffness_per_area != null)
                constraint.StiffnessPerArea =
                    PythonValueConverter.ToBool(stiffness_per_area, "stiffness_per_area");
            ApplyColor(constraint, color);
            _service.Add(constraint);
            //
            return this[nameText];
        }

        [PythonAPI("Replaces an existing surface spring constraint using command history.",
                   Arguments = "old_name: str, name: str = None, and the arguments of add_surface_spring, " +
                               "all optional", Returns = "Constraint")]
        public ConstraintAPI replace_surface_spring(object old_name, object name = null, object region = null,
                                                    object k1 = null, object k2 = null, object k3 = null,
                                                    object stiffness_per_area = null, object color = null)
        {
            string oldName = PythonValueConverter.ToText(old_name, "old_name", false);
            string nameText = PythonValueConverter.ToText(name, "name", true);
            SurfaceSpring constraint = _service.CloneForReplace<SurfaceSpring>(oldName, "surface_spring");
            if (region != null) _service.ApplyRegion(constraint, region, SurfaceTarget);
            ApplySpringValues(constraint, k1, k2, k3);
            if (stiffness_per_area != null)
                constraint.StiffnessPerArea =
                    PythonValueConverter.ToBool(stiffness_per_area, "stiffness_per_area");
            ApplyColor(constraint, color);
            //
            return Replace(oldName, nameText, constraint);
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
            if (IsDefaultValue(positionTolerance, "position_tolerance"))
                constraint.PositionTolerance.SetEquationFromValue(double.NaN);
            else if (positionTolerance != null)
            {
                EquationString equation = PythonValueConverter.ToEquationString(positionTolerance, "position_tolerance");
                double value = PythonModelArgumentCheck.Evaluate(constraint.PositionTolerance, equation,
                                                                 "position_tolerance");
                if (value < 0)
                    throw new PythonAPIException("The argument 'position_tolerance' must be zero or larger.");
                PythonModelArgumentCheck.SetEquation(constraint.PositionTolerance, equation, "position_tolerance");
            }
            if (adjust != null) constraint.Adjust = PythonValueConverter.ToBool(adjust, "adjust");
        }
        private static void ApplyJointValues(Joint constraint, object jointType, object centerDefinedBy, object center,
                                             object axisDefinedBy, object axisDirection, object formulation,
                                             object stiffnessDefinedBy, object stiffness)
        {
            if (formulation != null)
                constraint.Formulation =
                    PythonValueConverter.ToEnum<JointFormulationEnum>(AsString(formulation, "formulation"), "formulation");
            // The rigid body formulation has no springs, so a stiffness given for it would never be written
            if (!constraint.HasSprings && (stiffness != null || stiffnessDefinedBy != null))
                throw new PythonAPIException("The argument '" + (stiffness != null ? "stiffness" : "stiffness_defined_by") +
                                             "' applies only to the Springs formulation, and the joint uses " +
                                             constraint.Formulation + ". Give formulation='Springs' together with it.");
            if (jointType != null)
                constraint.JointType =
                    PythonValueConverter.ToEnum<JointTypeEnum>(AsString(jointType, "joint_type"), "joint_type");
            if (axisDirection != null && !constraint.HasAxis)
                throw new PythonAPIException("The argument 'axis_direction' does not apply to a " + constraint.JointType +
                                             " joint, which has no axis.");
            // A given stiffness is the user defined one unless the definition says otherwise
            if (stiffness != null)
            {
                PythonModelArgumentCheck.SetEquation(constraint.Stiffness, stiffness, "stiffness");
                constraint.StiffnessDefinition = JointStiffnessDefinitionEnum.UserDefined;
            }
            if (stiffnessDefinedBy != null)
                constraint.StiffnessDefinition =
                    PythonValueConverter.ToEnum<JointStiffnessDefinitionEnum>(AsString(stiffnessDefinedBy, "stiffness_defined_by"),
                                                                              "stiffness_defined_by");
            // A value the definition does not use would be stored and never written, so the pair is refused
            if (stiffness != null && constraint.StiffnessDefinition != JointStiffnessDefinitionEnum.UserDefined)
                throw new PythonAPIException("The argument 'stiffness' is not used when the argument " +
                                             "'stiffness_defined_by' is '" + constraint.StiffnessDefinition + "'. Give " +
                                             "stiffness_defined_by='UserDefined' with it, or leave stiffness out.");
            if (constraint.HasUserDefinedStiffness)
            {
                double value = constraint.Stiffness.Value;
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                    throw new PythonAPIException("The argument 'stiffness' must be positive.");
            }
            // A given value is the user defined one unless the definition says otherwise
            if (center != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(center, "center", 3);
                PythonModelArgumentCheck.SetEquations(new[] { constraint.X, constraint.Y, constraint.Z }, values, "center");
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
            if (center != null && constraint.CenterDefinition != JointCenterDefinitionEnum.Coordinates)
                throw new PythonAPIException("The argument 'center' is not used when the argument " +
                                             "'center_defined_by' is '" + constraint.CenterDefinition + "'. Give " +
                                             "center_defined_by='Coordinates' with it, or leave center out.");
            if (axisDirection != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axisDirection, "axis_direction", 3);
                PythonModelArgumentCheck.SetEquations(new[] { constraint.N1, constraint.N2, constraint.N3 }, values,
                                                      "axis_direction");
                constraint.AxisDefinition = JointDefinitionEnum.UserDefined;
            }
            if (axisDefinedBy != null)
                constraint.AxisDefinition =
                    PythonValueConverter.ToEnum<JointDefinitionEnum>(AsString(axisDefinedBy, "axis_defined_by"),
                                                                     "axis_defined_by");
            if (axisDirection != null && constraint.AxisDefinition != JointDefinitionEnum.UserDefined)
                throw new PythonAPIException("The argument 'axis_direction' is not used when the argument " +
                                             "'axis_defined_by' is '" + constraint.AxisDefinition + "'. Give " +
                                             "axis_defined_by='UserDefined' with it, or leave axis_direction out.");
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
                PythonModelArgumentCheck.SetEquations(new[] { constraint.X, constraint.Y, constraint.Z }, values,
                                                      "axis_point");
            }
            if (axisDirection != null)
            {
                EquationString[] values = PythonValueConverter.ToEquationArray(axisDirection, "axis_direction", 3);
                PythonModelArgumentCheck.SetEquations(new[] { constraint.N1, constraint.N2, constraint.N3 }, values,
                                                      "axis_direction");
            }
            int sectors = constraint.NumberOfSectors;
            if (numberOfSectors != null)
            {
                sectors = PythonValueConverter.ToInt(numberOfSectors, "number_of_sectors");
                if (sectors < 2)
                    throw new PythonAPIException("The argument 'number_of_sectors' must be at least 2, but it is " +
                                                 sectors + ".");
            }
            // The model lowers the graph sectors to fewer sectors on its own, so the merged pair is checked
            int graphSectors = constraint.NumberOfGraphSectors;
            if (numberOfGraphSectors != null)
                graphSectors = PythonValueConverter.ToInt(numberOfGraphSectors, "number_of_graph_sectors");
            if (graphSectors < 1 || graphSectors > sectors)
                throw new PythonAPIException("The argument 'number_of_graph_sectors' must be between 1 and " +
                                             "number_of_sectors, which is " + sectors + ", but it is " +
                                             graphSectors + (numberOfGraphSectors == null ? ", the value kept" : "") +
                                             ".");
            constraint.NumberOfSectors = sectors;
            constraint.NumberOfGraphSectors = graphSectors;
            if (checkSectorAngle != null)
                constraint.CheckSectorAngle =
                    PythonValueConverter.ToBool(checkSectorAngle, "check_sector_angle");
        }
        private void ApplyExpansionRegion(CyclicSymmetry constraint, object region)
        {
            if (region == null) return;
            //
            _service.ApplyRegion(constraint, region, ElementTarget);
        }
        private static void ClearExpansionRegion(CyclicSymmetry constraint)
        {
            constraint.RegionName = "";
            constraint.RegionType = RegionTypeEnum.Selection;
            constraint.CreationData = null;
            constraint.CreationIds = null;
        }
        // The controller needs the result expansion region for more than one graph sector and discards it
        // otherwise, so the final state is checked rather than any single argument
        private static void CheckExpansionRegion(CyclicSymmetry constraint, string name)
        {
            bool hasExpansionRegion = constraint.CreationData != null ||
                                      (constraint.RegionType != RegionTypeEnum.Selection &&
                                       constraint.RegionType != RegionTypeEnum.None &&
                                       !string.IsNullOrEmpty(constraint.RegionName));
            int numberOfGraphSectors = constraint.NumberOfGraphSectors;
            if (numberOfGraphSectors > 1 && !hasExpansionRegion)
                throw new PythonAPIException("The cyclic symmetry '" + name + "' has 'number_of_graph_sectors' " +
                                             numberOfGraphSectors + ", which needs a result expansion region. " +
                                             "Give it as 'region'.");
            if (numberOfGraphSectors <= 1 && hasExpansionRegion)
                throw new PythonAPIException("The cyclic symmetry '" + name + "' has a result expansion region " +
                                             "and 'number_of_graph_sectors' " + numberOfGraphSectors + ", but " +
                                             "the region is only used when 'number_of_graph_sectors' is " +
                                             "larger than 1. Give a larger 'number_of_graph_sectors', or drop " +
                                             "the region with 'clear_expansion_region=True' on a replace.");
        }
        private static void ApplyDistributingValues(DistributingCoupling constraint, object u1, object u2,
                                                    object u3, object ur1, object ur2, object ur3)
        {
            if (u1 != null) constraint.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) constraint.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) constraint.U3 = ToInPlaneBool(constraint, u3, "u3");
            if (ur1 != null) constraint.UR1 = ToInPlaneBool(constraint, ur1, "ur1");
            if (ur2 != null) constraint.UR2 = ToInPlaneBool(constraint, ur2, "ur2");
            if (ur3 != null) constraint.UR3 = PythonValueConverter.ToBool(ur3, "ur3");
        }
        private static void ApplyKinematicValues(KinematicCoupling constraint, object u1, object u2, object u3)
        {
            if (u1 != null) constraint.U1 = PythonValueConverter.ToBool(u1, "u1");
            if (u2 != null) constraint.U2 = PythonValueConverter.ToBool(u2, "u2");
            if (u3 != null) constraint.U3 = ToInPlaneBool(constraint, u3, "u3");
        }
        // The model clears an out of plane degree of freedom of a 2D constraint, so a given one is refused instead
        private static bool ToInPlaneBool(Constraint constraint, object value, string argumentName)
        {
            bool result = PythonValueConverter.ToBool(value, argumentName);
            if (result && constraint.TwoD)
                throw new PythonAPIException("The argument '" + argumentName + "' cannot be True in a 2D model, " +
                                             "which has no such degree of freedom.");
            //
            return result;
        }
        private void EnsureThreeDimensional(string itemDescription)
        {
            if (_service.GetIsTwoDimensional())
                throw new PythonAPIException(itemDescription + " is supported only in a 3D model.");
        }
        private static void ApplyCompressionOnlyValues(CompressionOnly constraint, object clearance,
                                                       object springStiffness,
                                                       object tensileForceAtNegativeInfinity, object offset,
                                                       object nonLinear)
        {
            if (clearance != null)
                PythonModelArgumentCheck.SetEquation(constraint.Clearance, clearance, "clearance");
            if (IsDefaultValue(springStiffness, "spring_stiffness"))
                constraint.SpringStiffness.SetEquationFromValue(double.NaN);
            else if (springStiffness != null)
                PythonModelArgumentCheck.SetEquation(constraint.SpringStiffness, springStiffness, "spring_stiffness");
            if (IsDefaultValue(tensileForceAtNegativeInfinity, "tensile_force_at_negative_infinity"))
                constraint.TensileForceAtNegativeInfinity.SetEquationFromValue(double.NaN);
            else if (tensileForceAtNegativeInfinity != null)
                PythonModelArgumentCheck.SetEquation(constraint.TensileForceAtNegativeInfinity,
                                                     tensileForceAtNegativeInfinity,
                                                     "tensile_force_at_negative_infinity");
            if (offset != null)
                PythonModelArgumentCheck.SetEquation(constraint.Offset, offset, "offset");
            if (nonLinear != null)
                constraint.NonLinear = PythonValueConverter.ToBool(nonLinear, "non_linear");
        }
        // A value the dialog offers as Default is stored as NaN, and None keeps the value on a replace, so
        // float('nan') is how a script asks for Default, as it is for the frequency limits of a step
        private static bool IsDefaultValue(object value, string argumentName)
        {
            if (value == null || value is string) return false;
            //
            return double.IsNaN(PythonValueConverter.ToDouble(value, argumentName));
        }
        private static void ApplySpringValues(SpringConstraint constraint, object k1, object k2, object k3)
        {
            if (k1 != null)
                PythonModelArgumentCheck.SetEquation(constraint.K1, ToSpringStiffness(constraint, constraint.K1, k1, "k1"), "k1");
            if (k2 != null)
                PythonModelArgumentCheck.SetEquation(constraint.K2, ToSpringStiffness(constraint, constraint.K2, k2, "k2"), "k2");
            if (k3 != null)
                PythonModelArgumentCheck.SetEquation(constraint.K3, ToSpringStiffness(constraint, constraint.K3, k3, "k3"), "k3");
        }
        // The model sets a negative stiffness, and an out of plane one in 2D, to zero, so they are refused instead
        private static EquationString ToSpringStiffness(SpringConstraint constraint, EquationContainer container,
                                                        object value, string argumentName)
        {
            EquationString equation = PythonValueConverter.ToEquationString(value, argumentName);
            double stiffness = PythonModelArgumentCheck.Evaluate(container, equation, argumentName);
            if (stiffness < 0)
                throw new PythonAPIException("The argument '" + argumentName + "' must be zero or larger.");
            if (stiffness != 0 && argumentName == "k3" && constraint.TwoD)
                throw new PythonAPIException("The argument 'k3' must be zero in a 2D model, which has no out of " +
                                             "plane direction.");
            //
            return equation;
        }
        // A tie, a cyclic symmetry and a joint color their two surfaces, as the dialog shows them; every other
        // constraint has one color, which the model keeps as its master color
        private static void ApplyColors(Constraint constraint, object masterColor, object slaveColor)
        {
            if (masterColor != null) constraint.MasterColor = PythonColorConverter.ToColor(masterColor, "master_color");
            if (slaveColor != null) constraint.SlaveColor = PythonColorConverter.ToColor(slaveColor, "slave_color");
        }
        private static void ApplyColor(Constraint constraint, object color)
        {
            if (color != null) constraint.MasterColor = PythonColorConverter.ToColor(color, "color");
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
