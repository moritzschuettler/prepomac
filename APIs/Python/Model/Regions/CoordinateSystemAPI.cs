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
                   "evaluating them: x1, y1, z1 of the origin, x2, y2, z2 of the point on X and x3, y3, z3 of " +
                   "the point in XY. Only a value entered as an expression is returned, with its leading equals " +
                   "sign; a coordinate entered as a plain number is left out, and get_origin, get_point_on_x " +
                   "and get_point_in_xy return it as a number.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_equations()
        {
            return ModelItemValueReader.GetEquations(Resolve());
        }
        [PythonAPI("Returns how each defining point was constructed.", Returns = "dict[str, str]")]
        public Dictionary<string, string> get_construction()
        {
            CoordinateSystem item = Resolve();
            return new Dictionary<string, string>
            {
                { "origin", item.CenterCreatedFrom.ToString() },
                { "point_on_x", item.PointXCreatedFrom.ToString() },
                { "point_in_xy", item.PointXYCreatedFrom.ToString() }
            };
        }
        public override string ToString()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return "CoordinateSystem(name=" + StringListAPI.ToDisplayString(coordinateSystem.Name) +
                   ", type=" + coordinateSystem.Type + ")";
        }
        //
        private CoordinateSystem Resolve()
        {
            return _service.ResolveCoordinateSystem(_coordinateSystemName);
        }
    }
}
