// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class SplitByPlaneThreePoints : GeometryFeature
    {
        // Variables
        private double[][] _points;                         //ISerializable
        private int _numberOfPoints;                        //ISerializable
        private double _offset;                             //ISerializable


        // Properties
        public double[][] Points { get { return _points; } set { _points = value; } }
        /// <summary>
        /// How many of the points were read from the picked geometry. A pick that does not land on a
        /// point takes part in the selection without contributing one, so the number of picks and the
        /// number of points are not the same.
        /// </summary>
        public int NumberOfPoints { get { return _numberOfPoints; } set { _numberOfPoints = value; } }
        public double Offset { get { return _offset; } set { _offset = value; } }
        public override SplitGeometryToolTypeEnum ToolType { get { return SplitGeometryToolTypeEnum.Plane; } }
        public override string DisplayName { get { return "Plane by three points"; } }


        // Constructors
        public SplitByPlaneThreePoints()
            : base()
        {
            _points = new double[][] { new double[3], new double[3], new double[3] };
            _numberOfPoints = 0;
            _offset = 0;
        }
        public SplitByPlaneThreePoints(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_points":
                        _points = (double[][])entry.Value; break;
                    case "_numberOfPoints":
                        _numberOfPoints = (int)entry.Value; break;
                    case "_offset":
                        _offset = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public double[] GetPlaneOrigin()
        {
            double[] normal = GetPlaneNormal();
            if (normal == null) return null;
            //
            return new double[] { _points[0][0] + _offset * normal[0],
                                  _points[0][1] + _offset * normal[1],
                                  _points[0][2] + _offset * normal[2] };
        }
        public double[] GetPlaneNormal()
        {
            if (_points == null || _points.Length != 3) return null;
            //
            double[] first = new double[] { _points[1][0] - _points[0][0],
                                            _points[1][1] - _points[0][1],
                                            _points[1][2] - _points[0][2] };
            double[] second = new double[] { _points[2][0] - _points[0][0],
                                             _points[2][1] - _points[0][1],
                                             _points[2][2] - _points[0][2] };
            double[] normal = new double[] { first[1] * second[2] - first[2] * second[1],
                                             first[2] * second[0] - first[0] * second[2],
                                             first[0] * second[1] - first[1] * second[0] };
            double length = Math.Sqrt(normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2]);
            // Three points on one line do not define a plane
            double firstLength = Math.Sqrt(first[0] * first[0] + first[1] * first[1] + first[2] * first[2]);
            double secondLength = Math.Sqrt(second[0] * second[0] + second[1] * second[1] + second[2] * second[2]);
            if (firstLength <= 0 || secondLength <= 0 || length <= 1E-8 * firstLength * secondLength) return null;
            //
            return new double[] { normal[0] / length, normal[1] / length, normal[2] / length };
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _points = new double[][] { new double[3], new double[3], new double[3] };
            _numberOfPoints = 0;
        }
        public override string CheckValidity()
        {
            if (_splitterIds != null && _splitterIds.Length > 0 && _numberOfPoints < 3)
                return "Select three points. A pick that does not land on a point does not give one.";
            if (GetPlaneNormal() == null)
                return "The three points of the splitting plane lie on one line.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_points", _points, typeof(double[][]));
            info.AddValue("_numberOfPoints", _numberOfPoints, typeof(int));
            info.AddValue("_offset", _offset, typeof(double));
        }
    }
}
