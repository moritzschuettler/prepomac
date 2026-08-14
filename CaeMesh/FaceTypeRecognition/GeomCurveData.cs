// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Globalization;

namespace CaeMesh
{
    /// <summary>
    /// The result of the mesh based recognition of a visualization edge.
    /// Besides the curve type it also contains the parameters of the recognized analytic curve.
    /// </summary>
    [Serializable]
    public class GeomCurveData
    {
        // Variables
        private GeomCurveType _type;
        private double[] _point;
        private double[] _direction;
        private double[] _majorAxis;
        private double _radius;
        private double _minorRadius;
        private double _arcAngleDeg;
        private double _length;
        private bool _closed;
        private double _residual;
        private int _nodeCount;


        // Properties
        /// <summary>
        /// The recognized curve type
        /// </summary>
        public GeomCurveType Type { get { return _type; } set { _type = value; } }

        /// <summary>
        /// Line              - a point on the line
        /// Circle, Ellipse   - the center
        /// Hyperbola         - the center; Parabola - the vertex
        /// Other             - the center of the edge nodes
        /// </summary>
        public double[] Point { get { return _point; } set { _point = value; } }

        /// <summary>
        /// Line   - the direction of the line
        /// Others - the normal of the plane of the curve (null for a non planar curve)
        /// </summary>
        public double[] Direction { get { return _direction; } set { _direction = value; } }

        /// <summary>
        /// Ellipse, Hyperbola, Parabola - the direction of the major axis
        /// </summary>
        public double[] MajorAxis { get { return _majorAxis; } set { _majorAxis = value; } }

        /// <summary>
        /// Circle - the radius; Ellipse - the major radius
        /// </summary>
        public double Radius { get { return _radius; } set { _radius = value; } }

        /// <summary>
        /// Ellipse - the minor radius
        /// </summary>
        public double MinorRadius { get { return _minorRadius; } set { _minorRadius = value; } }

        /// <summary>
        /// Circle, Ellipse - the angle swept by the edge; 360 for a closed curve
        /// </summary>
        public double ArcAngleDeg { get { return _arcAngleDeg; } set { _arcAngleDeg = value; } }

        /// <summary>
        /// The length of the edge measured along the mesh edge cells
        /// </summary>
        public double Length { get { return _length; } set { _length = value; } }

        /// <summary>
        /// True for a closed edge - the first and the last edge node are the same
        /// </summary>
        public bool Closed { get { return _closed; } set { _closed = value; } }

        /// <summary>
        /// The relative rms deviation of the edge nodes from the recognized curve;
        /// it is relative to the radius for a circle and to the edge extent for the other curves
        /// </summary>
        public double Residual { get { return _residual; } set { _residual = value; } }

        /// <summary>
        /// The number of the corner nodes of the edge
        /// </summary>
        public int NodeCount { get { return _nodeCount; } set { _nodeCount = value; } }


        // Constructors
        public GeomCurveData()
        {
            _type = GeomCurveType.Unknown;
            _point = null;
            _direction = null;
            _majorAxis = null;
            _radius = 0;
            _minorRadius = 0;
            _arcAngleDeg = 0;
            _length = 0;
            _closed = false;
            _residual = 0;
            _nodeCount = 0;
        }


        // Methods
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            string text = _type.ToString();
            //
            if (_direction != null)
                text += string.Format(ci, "; {0}: ({1:0.####}, {2:0.####}, {3:0.####})",
                                      _type == GeomCurveType.Line ? "direction" : "normal",
                                      _direction[0], _direction[1], _direction[2]);
            if (_point != null)
                text += string.Format(ci, "; point: ({0:0.####}, {1:0.####}, {2:0.####})",
                                      _point[0], _point[1], _point[2]);
            if (_type == GeomCurveType.Circle || _type == GeomCurveType.Ellipse)
            {
                text += string.Format(ci, "; R: {0:0.####}", _radius);
                if (_type == GeomCurveType.Ellipse) text += string.Format(ci, "; r: {0:0.####}", _minorRadius);
                text += string.Format(ci, "; arc: {0:0.##}°", _arcAngleDeg);
            }
            text += string.Format(ci, "; L: {0:0.####}", _length);
            text += string.Format(ci, "; residual: {0:0.###E+0}", _residual);
            //
            return text;
        }
    }
}
