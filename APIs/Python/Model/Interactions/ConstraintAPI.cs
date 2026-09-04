// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python.Model
{
    public class ConstraintAPI : BaseAPI
    {
        // Variables
        private readonly string _constraintName;
        private readonly ConstraintService _service;


        // Properties
        [PythonAPI("The constraint name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The constraint type, for example Tie or PointSpring.", Returns = "str")]
        public string constraint_type { get { return Resolve().GetType().Name; } }
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


        // Methods
        [PythonAPI("Returns the names of the values that are specific to this constraint type.",
                   Returns = "StringList")]
        public StringListAPI get_value_names()
        {
            return new StringListAPI(ModelItemValueReader.GetValues(Resolve(), typeof(Constraint)).Keys);
        }

        [PythonAPI("Returns a value that is specific to this constraint type, for example k1 or " +
                   "number_of_sectors. Use get_value_names to list them.", Arguments = "name: str",
                   Returns = "float, int, bool or str")]
        public object get_value(string name)
        {
            return ModelItemValueReader.GetValue(Resolve(), typeof(Constraint), name, "constraint");
        }
        public override string ToString()
        {
            Constraint constraint = Resolve();
            return "Constraint(name=" + StringListAPI.ToDisplayString(constraint.Name) + ", type=" +
                   constraint.GetType().Name + ")";
        }
        //
        private Constraint Resolve()
        {
            return _service.ResolveConstraint(_constraintName);
        }
    }
}
