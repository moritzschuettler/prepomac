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
using System.Drawing;

namespace APIs.Python.Model
{
    public class ConstraintAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<Type, string> ItemTypes = new Dictionary<Type, string>
        {
            { typeof(Tie), "tie" },
            { typeof(CyclicSymmetry), "cyclic_symmetry" },
            { typeof(Joint), "joint" },
            { typeof(RigidBody), "rigid_body" },
            { typeof(KinematicCoupling), "kinematic_coupling" },
            { typeof(DistributingCoupling), "distributing_coupling" },
            { typeof(CompressionOnly), "compression_only" },
            { typeof(PointSpring), "point_spring" },
            { typeof(SurfaceSpring), "surface_spring" }
        };
        private readonly string _constraintName;
        private readonly ConstraintService _service;


        // Properties
        [PythonAPI("The constraint name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The constraint type, named after the add_* method that makes it without the add_ prefix, " +
                   "for example tie or point_spring.", Returns = "str")]
        public string item_type { get { return GetItemType(Resolve()); } }
        [PythonAPI("True when the constraint is written to the solver input file.", Returns = "bool")]
        public bool active { get { return Resolve().Active; } }
        [PythonAPI("True when the constraint symbols are drawn.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the constraint resolved during the last regeneration: its regions and equations.",
                   Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }
        [PythonAPI("The region type of the master side. For a rigid body and a coupling this is the " +
                   "reference point.", Returns = "str")]
        public string master_region_type { get { return Resolve().MasterRegionType.ToString(); } }
        [PythonAPI("The region name of the master side.", Returns = "str")]
        public string master_region_name { get { return Resolve().MasterRegionName; } }
        [PythonAPI("The region type of the slave side.", Returns = "str")]
        public string slave_region_type { get { return Resolve().SlaveRegionType.ToString(); } }
        [PythonAPI("The region name of the slave side.", Returns = "str")]
        public string slave_region_name { get { return Resolve().SlaveRegionName; } }
        [PythonAPI("The master surface color of a tie, a cyclic symmetry or a joint as a list [R, G, B, A] of " +
                   "channel values, which the master_color argument takes back. It is refused for the other " +
                   "constraint types, which have one color, read by color.", Returns = "list[int]")]
        public object master_color { get { return PythonColorConverter.ToChannels(GetColor("master_color")); } }
        [PythonAPI("The slave surface color of a tie, a cyclic symmetry or a joint as a list [R, G, B, A] of " +
                   "channel values, which the slave_color argument takes back. It is refused for the other " +
                   "constraint types, which have one color, read by color.", Returns = "list[int]")]
        public object slave_color { get { return PythonColorConverter.ToChannels(GetColor("slave_color")); } }
        [PythonAPI("The color of a rigid body, a coupling, a compression only constraint or a spring as a list " +
                   "[R, G, B, A] of channel values, which the color argument takes back. It is refused for a " +
                   "tie, a cyclic symmetry and a joint, read by master_color and slave_color.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(GetColor("color")); } }
        [PythonAPI("The region type of the region argument: the nodes or the surface of a rigid body, a coupling, " +
                   "a compression only constraint or a spring, and the result expansion region of a cyclic " +
                   "symmetry, None when it has none. It is refused for a tie and a joint, whose regions are " +
                   "master_region_type and slave_region_type.", Returns = "str or None")]
        public string region_type { get { return GetRegion("region_type")[0]; } }
        [PythonAPI("The region name of the region argument, None for a cyclic symmetry without a result " +
                   "expansion region. It is refused for a tie and a joint, whose regions are master_region_name " +
                   "and slave_region_name.", Returns = "str or None")]
        public string region_name { get { return GetRegion("region_name")[1]; } }
        [PythonAPI("The sector angle in degrees a cyclic symmetry derives from its number_of_sectors, None for " +
                   "the other constraint types.", Returns = "float or None")]
        public object sector_angle_deg
        {
            get
            {
                CyclicSymmetry cyclicSymmetry = Resolve() as CyclicSymmetry;
                return cyclicSymmetry == null ? null : (object)cyclicSymmetry.SectorAngleDeg;
            }
        }


        // Constructors
        internal ConstraintAPI(string constraintName, ConstraintService service)
        {
            _constraintName = constraintName;
            _service = service;
        }


        // Static methods
        internal static string GetItemType(Constraint constraint)
        {
            string itemType;
            if (ItemTypes.TryGetValue(constraint.GetType(), out itemType)) return itemType;
            //
            throw new PythonAPIException("The constraint '" + constraint.Name + "' is a " + constraint.GetType().Name +
                                         ", which no add_* method of the Python API makes.");
        }
        // The arguments of the replace_* method of the constraint type, each with the stored values it sets. A joint
        // value the argument would refuse in the state the joint is in - a center the joint does not take from its
        // coordinates, an axis it does not define itself, a stiffness of its rigid body formulation - is not read
        // back, nor a center definition picked in the dialog, which no argument takes, so a read-back always goes
        // back. The regions are properties, as the region arguments take a selection
        private static Dictionary<string, string[]> GetArgumentValueNames(Constraint constraint)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (constraint is CyclicSymmetry)
            {
                names.Add("axis_point", new string[] { "x", "y", "z" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
                ModelItemValueReader.AddArguments(names, "number_of_sectors", "number_of_graph_sectors",
                                                  "check_sector_angle", "position_tolerance", "adjust");
            }
            else if (constraint is Tie) ModelItemValueReader.AddArguments(names, "position_tolerance", "adjust");
            else if (constraint is Joint)
            {
                Joint joint = (Joint)constraint;
                names.Add("joint_type", new string[] { "joint_type" });
                if (!joint.IsCenterFromSelection) names.Add("center_defined_by", new string[] { "center_definition" });
                if (joint.CenterDefinition == JointCenterDefinitionEnum.Coordinates)
                    names.Add("center", new string[] { "x", "y", "z" });
                if (joint.HasAxis) names.Add("axis_defined_by", new string[] { "axis_definition" });
                if (joint.HasUserDefinedAxis) names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
                names.Add("formulation", new string[] { "formulation" });
                if (joint.HasSprings) names.Add("stiffness_defined_by", new string[] { "stiffness_definition" });
                if (joint.HasUserDefinedStiffness) names.Add("stiffness", new string[] { "stiffness" });
            }
            else if (constraint is RigidBody) ModelItemValueReader.AddArguments(names, "reference_point_name");
            else if (constraint is KinematicCoupling)
                ModelItemValueReader.AddArguments(names, "reference_point_name", "u1", "u2", "u3");
            else if (constraint is DistributingCoupling)
                ModelItemValueReader.AddArguments(names, "reference_point_name", "u1", "u2", "u3", "ur1", "ur2", "ur3");
            else if (constraint is CompressionOnly)
                ModelItemValueReader.AddArguments(names, "clearance", "spring_stiffness",
                                                  "tensile_force_at_negative_infinity", "offset", "non_linear");
            else if (constraint is SurfaceSpring)
                ModelItemValueReader.AddArguments(names, "k1", "k2", "k3", "stiffness_per_area");
            else if (constraint is PointSpring) ModelItemValueReader.AddArguments(names, "k1", "k2", "k3");
            //
            return names;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this constraint type: the names of the " +
                   "replace_* arguments that set them, and nothing else, so the values go back into the replace_* " +
                   "method of the constraint.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            Constraint constraint = Resolve();
            return new StringListAPI(ModelItemValueReader.GetValues(constraint,
                                                                    GetArgumentValueNames(constraint)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this constraint type by the name of the add_* argument " +
                   "that sets it, for example k1 or number_of_sectors; a vector argument such as axis_point " +
                   "returns its components as a list. A value the dialog shows as Default, such as an unset " +
                   "position_tolerance or spring_stiffness, reads back as None; the argument takes float('nan') " +
                   "to set it back to Default. A joint reads back center only when center_defined_by is " +
                   "Coordinates, axis_defined_by only on a joint with an axis, and axis_direction only when it " +
                   "is UserDefined there, " +
                   "stiffness_defined_by only with the Springs formulation and stiffness only when it is " +
                   "UserDefined, and no center_defined_by for a centre picked in the dialog. Use get_value_names " +
                   "to list them.", Arguments = "name: str",
                   Returns = "float, int, bool, str or list[float]")]
        public object get_value(object name)
        {
            string nameText = PythonValueConverter.ToText(name, "name", false);
            Constraint constraint = Resolve();
            return ModelItemValueReader.GetValue(ModelItemValueReader.GetValues(constraint,
                                                                                GetArgumentValueNames(constraint)),
                                                 nameText, GetItemType(constraint) + " constraint");
        }

        [PythonAPI("Returns the expressions the constraint was entered with, without evaluating them, under the " +
                   "names get_value uses. Only a value entered as an expression is returned, with its leading " +
                   "equals sign; a value entered as a plain number is left out, and get_value returns it as a " +
                   "number. A vector argument such as axis_point with an expression in any component is " +
                   "returned as a list, its plain components as numbers.", Returns = "dict[str, str or list]")]
        public Dictionary<string, object> get_equations()
        {
            Constraint constraint = Resolve();
            return ModelItemValueReader.GetEquations(constraint, GetArgumentValueNames(constraint));
        }
        public override string ToString()
        {
            Constraint constraint = Resolve();
            return "Constraint(name=" + StringListAPI.ToDisplayString(constraint.Name) + ", type=" +
                   GetItemType(constraint) + ")";
        }
        //
        private Constraint Resolve()
        {
            return _service.ResolveConstraint(_constraintName);
        }
        // A tie, a cyclic symmetry and a joint color their two surfaces; every other type has one color, which the
        // model keeps as its master color
        private Color GetColor(string argumentName)
        {
            Constraint constraint = Resolve();
            bool hasTwoColors = constraint is Tie || constraint is Joint;
            if (hasTwoColors == (argumentName == "color"))
                throw new PythonAPIException("The " + GetItemType(constraint) + " constraint '" + constraint.Name +
                                             "' has " + (hasTwoColors ? "master_color and slave_color" : "color") +
                                             ", not " + argumentName + ".");
            //
            return argumentName == "slave_color" ? constraint.SlaveColor : constraint.MasterColor;
        }
        // The region the region argument sets, as its type and its name: the model keeps it on the master side of
        // a compression only constraint and a spring, on the slave side of a rigid body and a coupling, whose master
        // is the reference point, and apart from both sides for the result expansion region of a cyclic symmetry
        private string[] GetRegion(string propertyName)
        {
            Constraint constraint = Resolve();
            CyclicSymmetry cyclicSymmetry = constraint as CyclicSymmetry;
            if (cyclicSymmetry != null)
            {
                if (string.IsNullOrEmpty(cyclicSymmetry.RegionName) && cyclicSymmetry.CreationData == null)
                    return new string[] { null, null };
                return new string[] { cyclicSymmetry.RegionType.ToString(), cyclicSymmetry.RegionName };
            }
            if (constraint is CompressionOnly || constraint is SpringConstraint)
                return new string[] { constraint.MasterRegionType.ToString(), constraint.MasterRegionName };
            if (constraint is RigidBody || constraint is Coupling)
                return new string[] { constraint.SlaveRegionType.ToString(), constraint.SlaveRegionName };
            //
            throw new PythonAPIException("The " + GetItemType(constraint) + " constraint '" + constraint.Name +
                                         "' has master_region and slave_region, not a region, so " + propertyName +
                                         " is read as master_" + propertyName + " and slave_" + propertyName + ".");
        }
    }
}
