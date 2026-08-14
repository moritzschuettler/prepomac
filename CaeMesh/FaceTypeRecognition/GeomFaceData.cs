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
    /// The result of the mesh based recognition of a visualization face.
    /// Besides the face type it also contains the parameters of the recognized analytic surface.
    /// </summary>
    [Serializable]
    public class GeomFaceData
    {
        // Variables
        private GeomFaceType _type;
        private double[] _point;
        private double[] _axis;
        private double _radius;
        private double _minorRadius;
        private double _halfAngleDeg;
        private bool _convex;
        private double _residual;
        private double _size;
        private int _cellCount;
        private int _nodeCount;


        // Properties
        /// <summary>
        /// The recognized face type
        /// </summary>
        public GeomFaceType Type { get { return _type; } set { _type = value; } }

        /// <summary>
        /// Plane      - a point on the plane
        /// Cylinder   - a point on the axis closest to the face center
        /// Cone       - the apex
        /// Sphere     - the center
        /// Torus      - the center
        /// Revolution - a point on the axis
        /// Extrusion  - a point on the face
        /// </summary>
        public double[] Point { get { return _point; } set { _point = value; } }

        /// <summary>
        /// Plane     - the plane normal (oriented as the mesh face normals)
        /// Extrusion - the direction of the rulings
        /// Others    - the axis of revolution (for a cone it points from the apex towards the increasing radius)
        /// </summary>
        public double[] Axis { get { return _axis; } set { _axis = value; } }

        /// <summary>
        /// Cylinder, Sphere - the radius; Torus - the major radius
        /// </summary>
        public double Radius { get { return _radius; } set { _radius = value; } }

        /// <summary>
        /// Torus - the minor (tube) radius
        /// </summary>
        public double MinorRadius { get { return _minorRadius; } set { _minorRadius = value; } }

        /// <summary>
        /// Cone - the half angle in degrees
        /// </summary>
        public double HalfAngleDeg { get { return _halfAngleDeg; } set { _halfAngleDeg = value; } }

        /// <summary>
        /// True if the mesh face normals point away from the axis/center - a shaft;
        /// false if they point towards the axis/center - a hole
        /// </summary>
        public bool Convex { get { return _convex; } set { _convex = value; } }

        /// <summary>
        /// The relative rms deviation of the mesh nodes from the recognized surface;
        /// it is relative to the radius for the curved surfaces and to the face extent for a plane
        /// </summary>
        public double Residual { get { return _residual; } set { _residual = value; } }

        /// <summary>
        /// The characteristic face size = sqrt(face area)
        /// </summary>
        public double Size { get { return _size; } set { _size = value; } }

        /// <summary>
        /// The number of the visualization cells of the face
        /// </summary>
        public int CellCount { get { return _cellCount; } set { _cellCount = value; } }

        /// <summary>
        /// The number of the corner nodes of the face
        /// </summary>
        public int NodeCount { get { return _nodeCount; } set { _nodeCount = value; } }


        // Constructors
        public GeomFaceData()
        {
            _type = GeomFaceType.Unknown;
            _point = null;
            _axis = null;
            _radius = 0;
            _minorRadius = 0;
            _halfAngleDeg = 0;
            _convex = true;
            _residual = 0;
            _size = 0;
            _cellCount = 0;
            _nodeCount = 0;
        }


        // Methods
        public override string ToString()
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            string text = _type.ToString();
            //
            if (_axis != null)
                text += string.Format(ci, "; axis: ({0:0.####}, {1:0.####}, {2:0.####})", _axis[0], _axis[1], _axis[2]);
            if (_point != null)
                text += string.Format(ci, "; point: ({0:0.####}, {1:0.####}, {2:0.####})", _point[0], _point[1], _point[2]);
            if (_type == GeomFaceType.Cylinder || _type == GeomFaceType.Sphere || _type == GeomFaceType.Torus)
                text += string.Format(ci, "; R: {0:0.####}", _radius);
            if (_type == GeomFaceType.Torus)
                text += string.Format(ci, "; r: {0:0.####}", _minorRadius);
            if (_type == GeomFaceType.Cone)
                text += string.Format(ci, "; half angle: {0:0.###}°", _halfAngleDeg);
            //
            text += string.Format(ci, "; residual: {0:0.###E+0}", _residual);
            //
            return text;
        }
    }
}
