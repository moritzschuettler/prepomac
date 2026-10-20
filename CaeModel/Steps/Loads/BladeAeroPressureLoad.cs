using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CaeMesh;
using CaeGlobals;


namespace CaeModel
{
    [Serializable]
    public class BladeAeroPressureLoad : Load, ISerializable
    {
        // Variables
        private string _regionName;                     //ISerializable
        private RegionTypeEnum _regionType;             //ISerializable
        private int _axis;                              //ISerializable  0=X 1=Y 2=Z  spine direction
        private string _surfaceName;                    //ISerializable  source face surface for DLoad zones
        private BladeAeroDistanceTypeEnum _distanceType;//ISerializable  meaning of the table first column
        private List<BladeAeroTableRow> _tableRows;     //ISerializable  distance + force (N)
        private int _sectionCount;                      //ISerializable  number of DLoad zones created last time


        // Properties
        public override string RegionName { get { return _regionName; } set { _regionName = value; } }
        public override RegionTypeEnum RegionType { get { return _regionType; } set { _regionType = value; } }
        public int Axis { get { return _axis; } set { _axis = value; } }
        public string SurfaceName { get { return _surfaceName; } set { _surfaceName = value; } }
        public BladeAeroDistanceTypeEnum DistanceType { get { return _distanceType; } set { _distanceType = value; } }
        public List<BladeAeroTableRow> TableRows { get { return _tableRows; } set { _tableRows = value; } }
        public int SectionCount { get { return _sectionCount; } set { _sectionCount = value; } }


        // Constructors
        public BladeAeroPressureLoad(string name, int axis, string surfaceName)
            : base(name, false, false, 0)
        {
            _regionName = "";
            _regionType = RegionTypeEnum.Selection;
            _axis = axis;
            _surfaceName = surfaceName ?? "";
            _distanceType = BladeAeroDistanceTypeEnum.Absolute;
            _tableRows = new List<BladeAeroTableRow>();
        }
        public BladeAeroPressureLoad(SerializationInfo info, StreamingContext context)
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
                    case "_surfaceName": _surfaceName = (string)entry.Value; break;
                    case "_distanceType": _distanceType = (BladeAeroDistanceTypeEnum)entry.Value; break;
                    case "_tableRows": _tableRows = (List<BladeAeroTableRow>)entry.Value; break;
                    case "_sectionCount": _sectionCount = (int)entry.Value; break;
                }
            }
            if (_regionName == null) _regionName = "";
            if (_surfaceName == null) _surfaceName = "";
            if (_tableRows == null) _tableRows = new List<BladeAeroTableRow>();
        }


        // Methods
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_regionType", _regionType, typeof(RegionTypeEnum));
            info.AddValue("_axis", _axis, typeof(int));
            info.AddValue("_surfaceName", _surfaceName, typeof(string));
            info.AddValue("_distanceType", _distanceType, typeof(BladeAeroDistanceTypeEnum));
            info.AddValue("_tableRows", _tableRows, typeof(List<BladeAeroTableRow>));
            info.AddValue("_sectionCount", _sectionCount, typeof(int));
        }
        public override void CheckEquations() { }
    }
}
