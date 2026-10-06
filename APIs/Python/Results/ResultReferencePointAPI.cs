// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using APIs.Python.Model;
using CaeMesh;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultReferencePointAPI : BaseAPI
    {
        // Variables
        private readonly string _referencePointName;
        private readonly ResultReferencePointsAPI _referencePoints;


        // Properties
        [PythonAPI("The result reference point name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("How the result reference point was created: Coordinates, OnPoint, BetweenTwoPoints, " +
                   "CircleCenter, CenterOfGravity or BoundingBoxCenter.", Returns = "str")]
        public string created_from { get { return Resolve().CreatedFrom.ToString(); } }
        [PythonAPI("True when the result reference point name is drawn next to its symbol.", Returns = "bool")]
        public bool name_visible { get { return Resolve().NameVisible; } }
        [PythonAPI("The red, green, blue and alpha channels of the symbol color.", Returns = "IntList")]
        public IntListAPI color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }
        [PythonAPI("True when the result reference point is visible.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }


        // Constructors
        internal ResultReferencePointAPI(string referencePointName, ResultReferencePointsAPI referencePoints)
        {
            _referencePointName = referencePointName;
            _referencePoints = referencePoints;
        }


        // Methods
        [PythonAPI("Returns the result reference point coordinates as [x, y, z], as computed on the result " +
                   "mesh.", Returns = "list[float]")]
        public object get_coordinates()
        {
            FeReferencePoint referencePoint = Resolve();
            return PythonListConverter.ToPythonList(new double[] { referencePoint.X.Value, referencePoint.Y.Value,
                                                                   referencePoint.Z.Value });
        }
        [PythonAPI("Returns the stored coordinate expressions, as the values or equations they were entered with.",
                   Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
        }
        [PythonAPI("Returns the stored region construction mode and name.", Returns = "dict[str, str]")]
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
            return _referencePoints.ResolveReferencePoint(_referencePointName);
        }
    }
}
