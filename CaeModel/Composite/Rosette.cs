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
using System.Text.RegularExpressions;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public enum RosetteType
    {
        Cartesian,
        FromCurves
    }

    // Meaning of the guide curve tangent: either the 0 degree material direction, or the 90 degree
    // one - in the latter case the material X axis is perpendicular to the curve
    [Serializable]
    public enum RosetteGuideAngle
    {
        Deg0,
        Deg90
    }

    // One tessellated guide curve of a FROM_CURVES rosette; the points are stored in the order
    // they were read from the STEP file and the Reversed flag flips the direction on the fly
    [Serializable]
    public class RosetteCurve
    {
        // Variables
        private double[][] _points;
        private bool _reversed;
        private bool _included;
        private string _sourceLabel;


        // Properties
        public double[][] Points
        {
            get { return _points; }
            set { _points = value; }
        }

        public bool Reversed
        {
            get { return _reversed; }
            set { _reversed = value; }
        }

        public bool Included
        {
            get { return _included; }
            set { _included = value; }
        }

        public string SourceLabel
        {
            get { return _sourceLabel; }
            set { _sourceLabel = value; }
        }

        public int PointCount { get { return _points == null ? 0 : _points.Length; } }

        public double Length
        {
            get
            {
                if (_points == null || _points.Length < 2) return 0;
                double length = 0;
                for (int i = 0; i < _points.Length - 1; i++)
                {
                    double dx = _points[i + 1][0] - _points[i][0];
                    double dy = _points[i + 1][1] - _points[i][1];
                    double dz = _points[i + 1][2] - _points[i][2];
                    length += Math.Sqrt(dx * dx + dy * dy + dz * dz);
                }
                return length;
            }
        }


        // Constructors
        public RosetteCurve()
        {
            _points = null;
            _reversed = false;
            _included = true;
            _sourceLabel = "";
        }

        public RosetteCurve(double[][] points, string sourceLabel)
        {
            _points = points;
            _reversed = false;
            _included = true;
            _sourceLabel = sourceLabel;
        }


        // Methods
        public double[][] GetEffectivePoints()
        {
            if (_points == null) return null;
            if (!_reversed) return _points;
            //
            double[][] reversed = new double[_points.Length][];
            for (int i = 0; i < _points.Length; i++) reversed[i] = _points[_points.Length - 1 - i];
            return reversed;
        }

        public RosetteCurve DeepCopy()
        {
            RosetteCurve copy = new RosetteCurve();
            copy._reversed = _reversed;
            copy._included = _included;
            copy._sourceLabel = _sourceLabel;
            if (_points != null)
            {
                copy._points = new double[_points.Length][];
                for (int i = 0; i < _points.Length; i++)
                    copy._points[i] = new double[] { _points[i][0], _points[i][1], _points[i][2] };
            }
            return copy;
        }
    }

    [Serializable]
    public class Rosette : NamedClass
    {
        // Variables
        private RosetteType _type;
        private double[] _axisX;
        private double[] _axisY;
        private List<RosetteCurve> _curves;
        private string _stepFilePath;
        private double _tessellationTolerance;
        private double _directionToleranceDeg;
        private RosetteGuideAngle _guideAngle;

        // The rosette name is never written to the CalculiX input file - the orientation names are
        // generated - so a dot is safe here and allows the default 'rosette.1' naming
        private static readonly HashSet<char> _nameCharacters = new HashSet<char>(new char[] { '.' });


        // Properties
        public RosetteType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public double[] AxisX
        {
            get { return _axisX; }
            set { _axisX = value; }
        }

        public double[] AxisY
        {
            get { return _axisY; }
            set { _axisY = value; }
        }

        public List<RosetteCurve> Curves
        {
            get { return _curves; }
            set { _curves = value; }
        }

        public string StepFilePath
        {
            get { return _stepFilePath; }
            set { _stepFilePath = value; }
        }

        // Chord tolerance used when tessellating the STEP curves; 0 = automatic
        public double TessellationTolerance
        {
            get { return _tessellationTolerance; }
            set { _tessellationTolerance = value; }
        }

        // Elements whose rosette directions agree within this angle share one *ORIENTATION
        public double DirectionToleranceDeg
        {
            get { return _directionToleranceDeg; }
            set
            {
                if (value <= 0) throw new CaeException("The direction tolerance must be greater than 0.");
                _directionToleranceDeg = value;
            }
        }

        // Only meaningful for a rosette guided by curves
        public RosetteGuideAngle GuideAngle
        {
            get { return _guideAngle; }
            set { _guideAngle = value; }
        }

        public int IncludedCurveCount
        {
            get { return _curves == null ? 0 : _curves.Count(c => c.Included && c.PointCount >= 2); }
        }


        // Constructors
        public Rosette(string name)
            : base(name, _nameCharacters)
        {
            _type = RosetteType.Cartesian;
            _axisX = new double[] { 1, 0, 0 };
            _axisY = new double[] { 0, 1, 0 };
            _curves = new List<RosetteCurve>();
            _stepFilePath = null;
            _tessellationTolerance = 0;
            _directionToleranceDeg = 5.0;
            _guideAngle = RosetteGuideAngle.Deg0;
        }


        // Methods
        public Rosette DeepCopy()
        {
            Rosette copy = new Rosette(Name);
            copy.CopyFrom(this);
            copy._type = _type;
            copy._axisX = new double[] { _axisX[0], _axisX[1], _axisX[2] };
            copy._axisY = new double[] { _axisY[0], _axisY[1], _axisY[2] };
            copy._stepFilePath = _stepFilePath;
            copy._tessellationTolerance = _tessellationTolerance;
            copy._directionToleranceDeg = _directionToleranceDeg;
            copy._guideAngle = _guideAngle;
            copy._curves = new List<RosetteCurve>();
            if (_curves != null)
                foreach (RosetteCurve curve in _curves) copy._curves.Add(curve.DeepCopy());
            return copy;
        }

        public string CheckValidity()
        {
            Vec3D x = new Vec3D(_axisX);
            Vec3D y = new Vec3D(_axisY);
            if (x.Len < 1E-10) return "The rosette X axis direction cannot be a zero vector.";
            if (y.Len < 1E-10) return "The rosette Y axis direction cannot be a zero vector.";
            Vec3D cross = Vec3D.CrossProduct(x, y);
            if (cross.Len < 1E-6 * x.Len * y.Len)
                return "The rosette X and Y axis directions cannot be parallel.";
            if (_type == RosetteType.FromCurves && IncludedCurveCount == 0)
                return "A rosette of type 'Guided by curves' must contain at least one curve with " +
                       "two or more points.";
            return null;
        }

        // Makes Y perpendicular to X inside the X-Y plane; keeps the handedness of the pair
        public void Orthogonalize()
        {
            Vec3D x = new Vec3D(_axisX);
            if (x.Normalize() < 1E-10) return;
            Vec3D y = new Vec3D(_axisY);
            double dot = Vec3D.DotProduct(y, x);
            Vec3D yOrt = new Vec3D(y.X - dot * x.X, y.Y - dot * x.Y, y.Z - dot * x.Z);
            if (yOrt.Normalize() < 1E-10) return;
            _axisX = x.Coor;
            _axisY = yOrt.Coor;
        }

        public static string GetNextName(IEnumerable<string> existingNames)
        {
            int max = 0;
            Regex regex = new Regex(@"^rosette\.(\d+)$", RegexOptions.IgnoreCase);
            if (existingNames != null)
            {
                foreach (string name in existingNames)
                {
                    Match match = regex.Match(name);
                    int index;
                    if (match.Success && int.TryParse(match.Groups[1].Value, out index) && index > max) max = index;
                }
            }
            return "rosette." + (max + 1);
        }
    }
}
