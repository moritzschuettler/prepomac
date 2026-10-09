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
        private static readonly Dictionary<string, string[]> CoordinatesValueNames =
            new Dictionary<string, string[]> { { "coordinates", new[] { "x", "y", "z" } } };
        private readonly string _referencePointName;
        private readonly ReferencePointService _service;


        // Properties
        [PythonAPI("The reference point name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("How the reference point was created.", Returns = "str")]
        public string created_from { get { return Resolve().CreatedFrom.ToString(); } }
        [PythonAPI("True when the reference point name is drawn next to its symbol.", Returns = "bool")]
        public bool name_visible { get { return Resolve().NameVisible; } }
        [PythonAPI("The symbol color as a list [R, G, B, A] of channel values, which the color argument takes " +
                   "back.", Returns = "list[int]")]
        public object color { get { return PythonColorConverter.ToChannels(Resolve().Color); } }
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
        [PythonAPI("Returns the reference point coordinates as [x, y, z].", Returns = "list[float]")]
        public object get_coordinates()
        {
            FeReferencePoint referencePoint = Resolve();
            return PythonListConverter.ToPythonList(new double[] { referencePoint.X.Value, referencePoint.Y.Value,
                                                                   referencePoint.Z.Value });
        }
        [PythonAPI("Returns the coordinate expressions the reference point was entered with, without " +
                   "evaluating them, under coordinates, the argument of add and replace, as a list of three. " +
                   "The list is returned when any coordinate is an expression, with its leading equals sign, " +
                   "its plain coordinates as numbers; a point without an expression returns an empty dict, " +
                   "and get_coordinates returns its numbers.", Returns = "dict[str, list]")]
        public Dictionary<string, object> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve(), CoordinatesValueNames);
        }
        [PythonAPI("Returns how the reference point is built, under created_from, region_type and region_name. " +
                   "region_type and region_name are None unless created_from is CenterOfGravity or " +
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
            return _service.ResolveReferencePoint(_referencePointName);
        }
    }
}
