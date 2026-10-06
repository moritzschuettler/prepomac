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
        [PythonAPI("The region type of the master side. For a rigid body and a coupling this is the " +
                   "reference point.", Returns = "str")]
        public string master_region_type { get { return Resolve().MasterRegionType.ToString(); } }
        [PythonAPI("The region name of the master side.", Returns = "str")]
        public string master_region_name { get { return Resolve().MasterRegionName; } }
        [PythonAPI("The region type of the slave side.", Returns = "str")]
        public string slave_region_type { get { return Resolve().SlaveRegionType.ToString(); } }
        [PythonAPI("The region name of the slave side.", Returns = "str")]
        public string slave_region_name { get { return Resolve().SlaveRegionName; } }


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
        private static Dictionary<string, string[]> GetArgumentValueNames(Constraint constraint)
        {
            Dictionary<string, string[]> names = new Dictionary<string, string[]>();
            if (constraint is CyclicSymmetry)
            {
                names.Add("axis_point", new string[] { "x", "y", "z" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
            }
            else if (constraint is Joint)
            {
                names.Add("joint_type", new string[] { "joint_type" });
                names.Add("center_defined_by", new string[] { "center_definition" });
                names.Add("center", new string[] { "x", "y", "z" });
                names.Add("axis_defined_by", new string[] { "axis_definition" });
                names.Add("axis_direction", new string[] { "n1", "n2", "n3" });
                names.Add("formulation", new string[] { "formulation" });
                names.Add("stiffness_defined_by", new string[] { "stiffness_definition" });
                names.Add("stiffness", new string[] { "stiffness" });
            }
            //
            return names;
        }


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this constraint type: the add_* " +
                   "argument names, followed by the stored values no argument sets.", Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            Constraint constraint = Resolve();
            return new StringListAPI(ModelItemValueReader.GetValues(constraint, typeof(Constraint),
                                                                    GetArgumentValueNames(constraint)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this constraint type by the name of the add_* argument " +
                   "that sets it, for example k1 or number_of_sectors; a vector argument such as axis_point " +
                   "returns its components as a list. Use get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool, str or list[float]")]
        public object get_value(string name)
        {
            Constraint constraint = Resolve();
            return ModelItemValueReader.GetValue(constraint, typeof(Constraint), GetArgumentValueNames(constraint),
                                                 name, GetItemType(constraint) + " constraint");
        }

        [PythonAPI("Returns the stored equations of the constraint without evaluating them, under the names " +
                   "get_value uses: an expression reads back with its leading equals sign and a plain value " +
                   "with its unit, and a vector argument such as axis_point as a list of the equations of its " +
                   "components.", Returns = "dict[str, str or StringList]")]
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
    }
}
