// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public class SplitByLineTwoPoints : GeometryFeature
    {
        // Variables
        private double[][] _points;                         //ISerializable
        private int _numberOfPoints;                        //ISerializable


        // Properties
        public double[][] Points { get { return _points; } set { _points = value; } }
        /// <summary>
        /// How many of the points were read from the picked geometry. A pick that does not land on a
        /// point takes part in the selection without contributing one, so the number of picks and the
        /// number of points are not the same.
        /// </summary>
        public int NumberOfPoints { get { return _numberOfPoints; } set { _numberOfPoints = value; } }
        public override SplitGeometryToolTypeEnum ToolType
        {
            get { return SplitGeometryToolTypeEnum.TwoPointLine; }
        }
        public override string DisplayName { get { return "Line by two points"; } }


        // Constructors
        public SplitByLineTwoPoints()
            : base()
        {
            _points = new double[][] { new double[3], new double[3] };
            _numberOfPoints = 0;
            _splitType = SplitTypeEnum.Shell;
        }
        public SplitByLineTwoPoints(SerializationInfo info, StreamingContext context)
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
                    default:
                        break;
                }
            }
        }


        // Methods
        /// <summary>
        /// The distance between the two points, which is zero while they are the same point and the line
        /// through them is not yet a line.
        /// </summary>
        public double GetLineLength()
        {
            if (_points == null || _points.Length != 2) return 0;
            //
            double dx = _points[1][0] - _points[0][0];
            double dy = _points[1][1] - _points[0][1];
            double dz = _points[1][2] - _points[0][2];
            //
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _points = new double[][] { new double[3], new double[3] };
            _numberOfPoints = 0;
        }
        public override string CheckValidity()
        {
            if (_splitterIds != null && _splitterIds.Length > 0 && _numberOfPoints < 2)
                return "Select two points. A pick that does not land on a point does not give one.";
            if (GetLineLength() <= 0)
                return "The two points of the splitting line are the same point.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_points", _points, typeof(double[][]));
            info.AddValue("_numberOfPoints", _numberOfPoints, typeof(int));
        }
    }
}
