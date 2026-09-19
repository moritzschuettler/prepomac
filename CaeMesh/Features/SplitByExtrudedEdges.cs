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
    public class SplitByExtrudedEdges : GeometryFeature
    {
        // Variables
        private SplitExtrusionDirectionTypeEnum _directionType;     //ISerializable
        private double[] _direction;                                //ISerializable
        private SplitExtrusionLengthTypeEnum _lengthType;           //ISerializable
        private double _length;                                     //ISerializable
        private bool _bothDirections;                               //ISerializable
        private int[] _directionIds;                                //ISerializable
        private Selection _directionData;                           //ISerializable


        // Properties
        public SplitExtrusionDirectionTypeEnum DirectionType
        {
            get { return _directionType; }
            set { _directionType = value; }
        }
        public double[] Direction { get { return _direction; } set { _direction = value; } }
        public SplitExtrusionLengthTypeEnum LengthType { get { return _lengthType; } set { _lengthType = value; } }
        public double Length
        {
            get { return _length; }
            set
            {
                _length = value;
                if (_length < 0) _length = 0;
            }
        }
        public bool BothDirections { get { return _bothDirections; } set { _bothDirections = value; } }
        public int[] DirectionIds { get { return _directionIds; } set { _directionIds = value; } }
        public Selection DirectionData { get { return _directionData; } set { _directionData = value; } }
        public override SplitGeometryToolTypeEnum ToolType { get { return SplitGeometryToolTypeEnum.ExtrudeEdges; } }
        public override string DisplayName { get { return "Extrude edge"; } }


        // Constructors
        public SplitByExtrudedEdges()
            : base()
        {
            _directionType = SplitExtrusionDirectionTypeEnum.NormalToFace;
            _direction = new double[] { 0, 0, 1 };
            _lengthType = SplitExtrusionLengthTypeEnum.ThroughRegion;
            _length = 0;
            _bothDirections = true;
            _directionIds = null;
            _directionData = null;
        }
        public SplitByExtrudedEdges(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_directionType":
                        _directionType = (SplitExtrusionDirectionTypeEnum)entry.Value; break;
                    case "_direction":
                        _direction = (double[])entry.Value; break;
                    case "_lengthType":
                        _lengthType = (SplitExtrusionLengthTypeEnum)entry.Value; break;
                    case "_length":
                        _length = (double)entry.Value; break;
                    case "_bothDirections":
                        _bothDirections = (bool)entry.Value; break;
                    case "_directionIds":
                        _directionIds = (int[])entry.Value; break;
                    case "_directionData":
                        _directionData = (Selection)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        public double[] GetExtrusionDirection()
        {
            if (_direction == null || _direction.Length != 3) return null;
            //
            double length = Math.Sqrt(_direction[0] * _direction[0] + _direction[1] * _direction[1] +
                                      _direction[2] * _direction[2]);
            if (length <= 0) return null;
            //
            return new double[] { _direction[0] / length, _direction[1] / length, _direction[2] / length };
        }
        public override void ClearSplitterSelection()
        {
            base.ClearSplitterSelection();
            //
            _directionIds = null;
            _directionData = null;
            if (_directionType != SplitExtrusionDirectionTypeEnum.Vector) _direction = new double[] { 0, 0, 0 };
        }
        public override string CheckValidity()
        {
            if (_splitterIds == null || _splitterIds.Length == 0)
                return "Select at least one edge to extrude into the splitting tool.";
            if (GetExtrusionDirection() == null)
                return "The extrusion direction is zero.";
            if (_lengthType == SplitExtrusionLengthTypeEnum.Value && _length <= 0)
                return "The extrusion length must be larger than zero.";
            return null;
        }
        // ISerialization
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_directionType", _directionType, typeof(SplitExtrusionDirectionTypeEnum));
            info.AddValue("_direction", _direction, typeof(double[]));
            info.AddValue("_lengthType", _lengthType, typeof(SplitExtrusionLengthTypeEnum));
            info.AddValue("_length", _length, typeof(double));
            info.AddValue("_bothDirections", _bothDirections, typeof(bool));
            info.AddValue("_directionIds", _directionIds, typeof(int[]));
            info.AddValue("_directionData", _directionData, typeof(Selection));
        }
    }
}
