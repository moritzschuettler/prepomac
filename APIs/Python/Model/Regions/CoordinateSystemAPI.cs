// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeMesh;

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
        [PythonAPI("Returns the coordinate system origin.", Returns = "NumericArray")]
        public NumericArrayAPI get_origin()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return new NumericArrayAPI(new double[] { coordinateSystem.X1.Value, coordinateSystem.Y1.Value,
                                                      coordinateSystem.Z1.Value });
        }

        [PythonAPI("Returns the point that defines the local X axis.", Returns = "NumericArray")]
        public NumericArrayAPI get_point_on_x()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return new NumericArrayAPI(new double[] { coordinateSystem.X2.Value, coordinateSystem.Y2.Value,
                                                      coordinateSystem.Z2.Value });
        }

        [PythonAPI("Returns the point that defines the local XY plane.", Returns = "NumericArray")]
        public NumericArrayAPI get_point_in_xy()
        {
            CoordinateSystem coordinateSystem = Resolve();
            return new NumericArrayAPI(new double[] { coordinateSystem.X3.Value, coordinateSystem.Y3.Value,
                                                      coordinateSystem.Z3.Value });
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
