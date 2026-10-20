// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalLayupOrientation : CalculixKeyword
    {
        // Variables
        private string _name;
        private CoordinateSystemTypeEnum _systemType;
        private double[] _pointA;   // Point on X-axis (rectangular) or on Z-axis (cylindrical)
        private double[] _pointB;   // Point in XY-plane (rectangular) or on Z-axis (cylindrical)
        private int _rotationAxis;  // 1, 2, or 3 for additional rotation
        private double _rotationAngle;  // Rotation angle in degrees


        // Properties


        // Constructor
        /// <summary>
        /// Creates an orientation using global rectangular coordinate system with ply rotation angle.
        /// </summary>
        public CalLayupOrientation(string name, double rotationAngle)
        {
            _name = name;
            _systemType = CoordinateSystemTypeEnum.Rectangular;
            // Global coordinate system: X along (1,0,0), Y along (0,1,0)
            _pointA = new double[] { 1.0, 0.0, 0.0 };
            _pointB = new double[] { 0.0, 1.0, 0.0 };
            _rotationAxis = 1;  // Rotate about local axis 1 (X-axis projected to shell)
            _rotationAngle = rotationAngle;
        }

        /// <summary>
        /// Creates an orientation from explicit axis vectors with ply rotation angle.
        /// </summary>
        public CalLayupOrientation(string name, double[] pointA, double[] pointB, double rotationAngle, int rotationAxis = 1)
        {
            _name = name;
            _systemType = CoordinateSystemTypeEnum.Rectangular;
            _pointA = pointA;
            _pointB = pointB;
            _rotationAxis = rotationAxis;
            _rotationAngle = rotationAngle;
        }

        /// <summary>
        /// Creates an orientation from a coordinate system with ply rotation angle.
        /// </summary>
        public CalLayupOrientation(string name, CoordinateSystem coordinateSystem, double rotationAngle)
        {
            _name = name;
            _systemType = coordinateSystem.Type;
            _rotationAxis = 1;
            _rotationAngle = rotationAngle;
            //
            if (_systemType == CoordinateSystemTypeEnum.Rectangular)
            {
                _pointA = coordinateSystem.DirectionX().Coor;
                _pointB = coordinateSystem.DirectionY().Coor;
            }
            else if (_systemType == CoordinateSystemTypeEnum.Cylindrical)
            {
                _pointA = coordinateSystem.Center().Coor;
                _pointB = (coordinateSystem.Center() + coordinateSystem.DirectionZ()).Coor;
            }
            else throw new NotSupportedException();
        }


        // Methods
        private static double CleanCoord(double value)
        {
            return Math.Abs(value) < 1e-10 ? 0.0 : value;
        }

        public override string GetKeywordString()
        {
            string systemType;
            if (_systemType == CoordinateSystemTypeEnum.Rectangular) systemType = "RECTANGULAR";
            else if (_systemType == CoordinateSystemTypeEnum.Cylindrical) systemType = "CYLINDRICAL";
            else throw new NotSupportedException();
            //
            return string.Format("*Orientation, Name={0}, System={1}{2}", _name, systemType, Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            // First data line: coordinates of points a and b
            sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}{6}",
                CleanCoord(_pointA[0]).ToCalculiX16String(), CleanCoord(_pointA[1]).ToCalculiX16String(), CleanCoord(_pointA[2]).ToCalculiX16String(),
                CleanCoord(_pointB[0]).ToCalculiX16String(), CleanCoord(_pointB[1]).ToCalculiX16String(), CleanCoord(_pointB[2]).ToCalculiX16String(),
                Environment.NewLine);
            // Second data line: rotation axis and angle (optional for rectangular, but we always include it for plies)
            sb.AppendFormat("{0}, {1}{2}", _rotationAxis, _rotationAngle.ToCalculiX16String(), Environment.NewLine);
            //
            return sb.ToString();
        }
    }
}
