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
        private static readonly Dictionary<string, string[]> CoordinateArguments = new Dictionary<string, string[]>
        {
            { "coordinates", new string[] { "x", "y", "z" } }
        };
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
        [PythonAPI("The symbol color as a list [R, G, B, A] of channel values, which the color argument takes " +
                   "back.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }
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
        [PythonAPI("Returns the coordinate expressions the result reference point was entered with, without " +
                   "evaluating them, under the argument coordinates as a list [x, y, z]. The coordinates are " +
                   "returned only when one of them was entered as an expression, with its leading equals sign; " +
                   "the others stay numbers. Coordinates entered as plain numbers, or computed from a " +
                   "selection, are left out, and get_coordinates returns them as numbers.",
                   Returns = "dict[str, list]")]
        public Dictionary<string, object> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve(), CoordinateArguments);
        }
        [PythonAPI("Returns how the result reference point is built, under created_from, region_type and " +
                   "region_name. region_type and region_name are None unless created_from is CenterOfGravity or " +
                   "BoundingBoxCenter, the modes add_from_region takes.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_construction()
        {
            FeReferencePoint item = Resolve();
            bool isRegion = item.CreatedFrom == FeReferencePointCreatedFrom.CenterOfGravity ||
                            item.CreatedFrom == FeReferencePointCreatedFrom.BoundingBoxCenter;
            return new Dictionary<string, string>
            {
                { "created_from", item.CreatedFrom.ToString() },
                { "region_type", isRegion ? item.RegionType.ToString() : null },
                { "region_name", isRegion ? item.RegionName : null }
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
