using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CaeMesh;
using CaeGlobals;


namespace CaeModel
{
    [Serializable]
    public class BladeAeroLoad : Load, ISerializable
    {
        // Variables
        private string _regionName;                     //ISerializable
        private RegionTypeEnum _regionType;             //ISerializable
        private int _axis;                              //ISerializable  0=X 1=Y 2=Z  spine direction
        private int _forceAxis;                         //ISerializable  0=X 1=Y 2=Z  force direction (must differ from _axis)
        private double _originX;                        //ISerializable
        private double _originY;                        //ISerializable
        private double _originZ;                        //ISerializable
        private bool _invertForces;                     //ISerializable
        private BladeAeroDistanceTypeEnum _distanceType;//ISerializable  meaning of the table first column
        private List<BladeAeroTableRow> _tableRows;     //ISerializable


        // Properties
        public override string RegionName { get { return _regionName; } set { _regionName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int Axis { get { return _axis; } set { _axis = value; } }
        public int ForceAxis { get { return _forceAxis; } set { _forceAxis = value; } }
        public double OriginX { get { return _originX; } set { _originX = value; } }
        public double OriginY { get { return _originY; } set { _originY = value; } }
        public double OriginZ { get { return _originZ; } set { _originZ = value; } }
        public bool InvertForces { get { return _invertForces; } set { _invertForces = value; } }
        public BladeAeroDistanceTypeEnum DistanceType { get { return _distanceType; } set { _distanceType = value; } }
        public List<BladeAeroTableRow> TableRows { get { return _tableRows; } set { _tableRows = value; } }


        // Constructors
        public BladeAeroLoad(string name, int axis, int forceAxis)
            : base(name, false, false, 0)
        {
            _regionName = "";
            _regionType = RegionTypeEnum.Selection;
            _axis = axis;
            _forceAxis = forceAxis;
            _originX = 0;
            _originY = 0;
            _originZ = 0;
            _invertForces = false;
            _distanceType = BladeAeroDistanceTypeEnum.Absolute;
            _tableRows = new List<BladeAeroTableRow>();
        }
        public BladeAeroLoad(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // Models saved before the distance type existed always used span ratios
            _distanceType = BladeAeroDistanceTypeEnum.Relative;
            //
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_regionName": _regionName = (string)entry.Value; break;
                    case "_regionType": _regionType = (RegionTypeEnum)entry.Value; break;
                    case "_axis": _axis = (int)entry.Value; break;
                    case "_forceAxis": _forceAxis = (int)entry.Value; break;
                    case "_originX": _originX = (double)entry.Value; break;
                    case "_originY": _originY = (double)entry.Value; break;
                    case "_originZ": _originZ = (double)entry.Value; break;
                    case "_invertForces": _invertForces = (bool)entry.Value; break;
                    case "_distanceType": _distanceType = (BladeAeroDistanceTypeEnum)entry.Value; break;
                    case "_tableRows": _tableRows = (List<BladeAeroTableRow>)entry.Value; break;
                }
            }
            if (_regionName == null) _regionName = "";
            if (_tableRows == null) _tableRows = new List<BladeAeroTableRow>();
        }


        // Methods
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_axis", _axis, typeof(int));
            info.AddValue("_forceAxis", _forceAxis, typeof(int));
            info.AddValue("_originX", _originX, typeof(double));
            info.AddValue("_originY", _originY, typeof(double));
            info.AddValue("_originZ", _originZ, typeof(double));
            info.AddValue("_invertForces", _invertForces, typeof(bool));
            info.AddValue("_distanceType", _distanceType, typeof(BladeAeroDistanceTypeEnum));
            info.AddValue("_tableRows", _tableRows, typeof(List<BladeAeroTableRow>));
        }
        public override void CheckEquations() { }
    }
}
