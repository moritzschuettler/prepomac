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
    public class CoordinateSystemAPI : BaseAPI
    {
        // Variables
        private static readonly Dictionary<string, string[]> PointValueNames = new Dictionary<string, string[]>
        {
            { "origin", new[] { "x1", "y1", "z1" } },
            { "point_on_x", new[] { "x2", "y2", "z2" } },
            { "point_in_xy", new[] { "x3", "y3", "z3" } }
        };
        private readonly string _coordinateSystemName;
        private readonly CoordinateSystemService _service;


        // Properties
        [PythonAPI("The coordinate system name.", Returns = "str")]
        public string name { get { return Resolve().Name; } }
        [PythonAPI("The coordinate system type: Rectangular or Cylindrical.", Returns = "str")]
        public string coordinate_system_type { get { return Resolve().Type.ToString(); } }
        [PythonAPI("True when the coordinate system name is drawn next to its symbol.", Returns = "bool")]
        public bool name_visible { get { return Resolve().NameVisible; } }
        [PythonAPI("True when the coordinate system is visible.", Returns = "bool")]
        public bool visible { get { return Resolve().Visible; } }
        [PythonAPI("True when the coordinate system resolved during the last regeneration.", Returns = "bool")]
        public bool valid { get { return Resolve().Valid; } }


        // Constructors
        internal CoordinateSystemAPI(string coordinateSystemName, CoordinateSystemService service)
        {
            _coordinateSystemName = coordinateSystemName;
            _service = service;
        }


        // Methods
        [PythonAPI("Returns the coordinate system origin as [x, y, z].", Returns = "list[float]")]
        public object get_origin()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return PythonListConverter.ToPythonList(new double[] { coordinateSystem.X1.Value,
                                                                   coordinateSystem.Y1.Value,
                                                                   coordinateSystem.Z1.Value });
        }

        [PythonAPI("Returns the point that defines the local X axis as [x, y, z].", Returns = "list[float]")]
        public object get_point_on_x()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return PythonListConverter.ToPythonList(new double[] { coordinateSystem.X2.Value,
                                                                   coordinateSystem.Y2.Value,
                                                                   coordinateSystem.Z2.Value });
        }

        [PythonAPI("Returns the point that defines the local XY plane as [x, y, z].", Returns = "list[float]")]
        public object get_point_in_xy()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return PythonListConverter.ToPythonList(new double[] { coordinateSystem.X3.Value,
                                                                   coordinateSystem.Y3.Value,
                                                                   coordinateSystem.Z3.Value });
        }
        [PythonAPI("Returns the coordinate expressions the coordinate system was entered with, without " +
                   "evaluating them, under origin, point_on_x and point_in_xy, the arguments of add and replace, " +
                   "each as a list of three. A point is returned when any of its coordinates is an expression, " +
                   "with its leading equals sign, its plain coordinates as numbers; a point without an " +
                   "expression is left out, and get_origin, get_point_on_x and get_point_in_xy return its " +
                   "numbers.", Returns = "dict[str, list]")]
        public Dictionary<string, object> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve(), PointValueNames);
        }
        [PythonAPI("Returns how each defining point was constructed, under origin_created_from, " +
                   "point_on_x_created_from and point_in_xy_created_from, the arguments of " +
                   "add_from_selections: OnPoint, BetweenTwoPoints or CircleCenter for a point computed from " +
                   "a selection, and None for a point given by its coordinates.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_construction()
        {
            CoordinateSystem item = Resolve();
            return new Dictionary<string, string>
            {
                { "origin_created_from", ToCreatedFromArgument(item.CenterCreatedFrom) },
                { "point_on_x_created_from", ToCreatedFromArgument(item.PointXCreatedFrom) },
                { "point_in_xy_created_from", ToCreatedFromArgument(item.PointXYCreatedFrom) }
            };
        }
        public override string ToString()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return "CoordinateSystem(name=" + StringListAPI.ToDisplayString(coordinateSystem.Name) +
                   ", type=" + coordinateSystem.Type + ")";
        }
        //
        private static string ToCreatedFromArgument(CsPointCreatedFromEnum createdFrom)
        {
            return createdFrom == CsPointCreatedFromEnum.Coordinates ? null : createdFrom.ToString();
        }
        private CoordinateSystem Resolve()
        {
            return _service.ResolveCoordinateSystem(_coordinateSystemName);
        }
    }
}
