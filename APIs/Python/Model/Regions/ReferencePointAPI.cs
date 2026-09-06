// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;
using System.Collections.Generic;

namespace APIs.Python.Model
{
    public class ReferencePointAPI : BaseAPI
    {
        // Variables
        private readonly string _referencePointName;
        private readonly ReferencePointService _service;


        // Properties
        [PythonAPI("The reference point name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("How the reference point was created.", Returns = "str")]
        public string created_from { get { return Resolve().CreatedFrom.ToString(); } }
        [PythonAPI("True when the reference point is visible.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the reference point resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal ReferencePointAPI(string referencePointName, ReferencePointService service)
        {
            _referencePointName = referencePointName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the reference point coordinates.", Returns = "NumericArray")]
        public NumericArrayAPI get_coordinates()
        {
            FeReferencePoint referencePoint = Resolve();
            return new NumericArrayAPI(new double[] { referencePoint.X.Value, referencePoint.Y.Value,
                                                      referencePoint.Z.Value });
        }
        [PythonAPI("Returns the stored coordinate expressions.", Returns = "dict")]
        public Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
        }
        [PythonAPI("Returns the stored region construction mode and name.", Returns = "dict")]
        public Dictionary<string, string> get_construction()
        {
            FeReferencePoint item = Resolve();
            return new Dictionary<string, string>
            {
                { "created_from", item.CreatedFrom.ToString() },
                { "region_type", item.RegionType.ToString() },
                { "region_name", item.RegionName }
            };
        }
        public override string ToString()
        {
            FeReferencePoint referencePoint = Resolve();
            return "ReferencePoint(name=" + StringListAPI.ToDisplayString(referencePoint.Name) +
                   ", coordinates=[" + referencePoint.X.Value + ", " + referencePoint.Y.Value + ", " +
                   referencePoint.Z.Value + "])";
        }
        //
        private FeReferencePoint Resolve()
        {
            return _service.ResolveReferencePoint(_referencePointName);
        }
    }
}
