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
using System.Text;

namespace PrePoMax
{
    public enum GeometryTransformTypeEnum
    {
        Translate,
        Rotate,
        Mirror,
        Scale
    }

    internal class GeometryTransform
    {
        // Variables
        private GeometryTransformTypeEnum _type;
        private double[] _point;
        private double[] _direction;
        private double _angleRad;
        private double[] _factors;
        private int _numberOfCopies;


        // Properties
        public GeometryTransformTypeEnum Type { get { return _type; } }
        public int NumberOfCopies { get { return _numberOfCopies; } }


        // Constructors
        private GeometryTransform(GeometryTransformTypeEnum type, double[] point, double[] direction, double angleRad,
                                  double[] factors, int numberOfCopies)
        {
            _type = type;
            _point = point;
            _direction = direction;
            _angleRad = angleRad;
            _factors = factors;
            _numberOfCopies = numberOfCopies;
        }


        // Methods
        public static GeometryTransform Translate(double[] translateVector, int numberOfCopies)
        {
            return new GeometryTransform(GeometryTransformTypeEnum.Translate, null, translateVector, 0, null,
                                         Math.Max(numberOfCopies, 0));
        }
        public static GeometryTransform Rotate(double[] rotateCenter, double[] rotateAxis, double rotateAngleRad,
                                               int numberOfCopies)
        {
            return new GeometryTransform(GeometryTransformTypeEnum.Rotate, rotateCenter, rotateAxis, rotateAngleRad, null,
                                         Math.Max(numberOfCopies, 0));
        }
        public static GeometryTransform Mirror(double[] mirrorPoint, double[] mirrorDirection, bool copy)
        {
            return new GeometryTransform(GeometryTransformTypeEnum.Mirror, mirrorPoint, mirrorDirection, 0, null,
                                         copy ? 1 : 0);
        }
        public static GeometryTransform Scale(double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            return new GeometryTransform(GeometryTransformTypeEnum.Scale, scaleCenter, null, 0, scaleFactors,
                                         copy ? 1 : 0);
        }
        public string GetDefinition()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Transform type: " + _type.ToString());
            if (_point != null) sb.AppendLine("Point: " + ValuesToString(_point));
            if (_direction != null) sb.AppendLine("Direction: " + ValuesToString(_direction));
            if (_type == GeometryTransformTypeEnum.Rotate)
                sb.AppendLine("Angle: " + ValuesToString(new double[] { _angleRad * 180 / Math.PI }));
            if (_factors != null) sb.AppendLine("Factors: " + ValuesToString(_factors));
            sb.AppendLine("Number of copies: " + _numberOfCopies);
            return sb.ToString();
        }
        private static string ValuesToString(double[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) sb.Append(" ");
                sb.Append(values[i].ToString("G17", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
