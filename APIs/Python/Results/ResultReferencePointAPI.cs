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
        [PythonAPI("Returns one displacement component of the result reference point, as get_nodal_value of the " +
                   "result returns it for a node, computed from the displacements of the nodes the point is " +
                   "connected to: the rigid motion of the region of a rigid body or of the surface of a kinematic " +
                   "coupling, and the weighted mean motion of the surface of a distributing coupling with its " +
                   "rotation times the lever arm of the point. Only the field DISP or DISPR with U1, U2, U3 or ALL " +
                   "is computed. A direction a kinematic coupling does not couple, or that the coupled nodes do not " +
                   "determine, is refused, and a point nothing moves, or of results opened without the model, has no " +
                   "displacement and is refused.",
                   Arguments = "step_id: int, increment_id: int, field_name: str, component_name: str",
                   Returns = "float")]
        public float get_nodal_value(object step_id, object increment_id, object field_name, object component_name)
        {
            string fieldName = PythonValueConverter.ToText(field_name, "field_name", false);
            string componentName = PythonValueConverter.ToText(component_name, "component_name", false);
            return _referencePoints.GetNodalValue(_referencePointName, PythonValueConverter.ToInt(step_id, "step_id"),
                                                  PythonValueConverter.ToInt(increment_id, "increment_id"), fieldName,
                                                  componentName);
        }
        [PythonAPI("Returns the true-scale deformed coordinates [x, y, z] of the result reference point using the " +
                   "DISP field, as the results view draws the point. A direction get_nodal_value does not compute " +
                   "stays undeformed. A point that has no displacement is refused, as by get_nodal_value.",
                   Arguments = "step_id: int, increment_id: int", Returns = "list[float]")]
        public object get_deformed_coordinates(object step_id, object increment_id)
        {
            int stepId = PythonValueConverter.ToInt(step_id, "step_id");
            int incrementId = PythonValueConverter.ToInt(increment_id, "increment_id");
            return PythonListConverter.ToPythonList(_referencePoints.GetDeformedCoordinates(_referencePointName, stepId,
                                                                                            incrementId));
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
