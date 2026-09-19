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
    public class SplitByPointAndNormal : GeometryFeature
    {
        // Variables
        private double[] _point;                            //ISerializable
        private double[] _normal;                           //ISerializable
        private double _offset;                             //ISerializable


        // Properties
        public double[] Point { get { return _point; } set { _point = value; } }
        public double[] Normal { get { return _normal; } set { _normal = value; } }
        public double Offset { get { return _offset; } set { _offset = value; } }
        public override SplitGeometryToolTypeEnum ToolType { get { return SplitGeometryToolTypeEnum.Plane; } }
        public override string DisplayName { get { return "Plane by point and normal"; } }


        // Constructors
        public SplitByPointAndNormal()
            : base()
        {
            _point = new double[3];
            _normal = new double[] { 0, 0, 1 };
            _offset = 0;
        }
        public SplitByPointAndNormal(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_point":
                        _point = (double[])entry.Value; break;
                    case "_normal":
                        _normal = (double[])entry.Value; break;
                    case "_offset":
                        _offset = (double)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public double[] GetPlaneNormal()
        {
            if (_normal == null || _normal.Length != 3) return null;
            //
            double length = Math.Sqrt(_normal[0] * _normal[0] + _normal[1] * _normal[1] + _normal[2] * _normal[2]);
            if (length <= 0) return null;
            //
            return new double[] { _normal[0] / length, _normal[1] / length, _normal[2] / length };
        }
        public double[] GetPlaneOrigin()
        {
            double[] normal = GetPlaneNormal();
            if (normal == null) return null;
            //
            return new double[] { _point[0] + _offset * normal[0],
                                  _point[1] + _offset * normal[1],
                                  _point[2] + _offset * normal[2] };
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _point = new double[3];
            _normal = new double[] { 0, 0, 1 };
        }
        public override string CheckValidity()
        {
            if (GetPlaneNormal() == null) return "The normal of the splitting plane is zero.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_point", _point, typeof(double[]));
            info.AddValue("_normal", _normal, typeof(double[]));
            info.AddValue("_offset", _offset, typeof(double));
        }
    }
}
