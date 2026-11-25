using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using CaeMesh;
using CaeGlobals;


namespace CaeModel
{
    [PmxType(50039)]
    public class BladeAeroPressureLoad : Load
    {
        // Variables
        [PmxKey(0)] private string _regionName;                     //ISerializable
        [PmxKey(1)] private RegionTypeEnum _regionType;             //ISerializable
        [PmxKey(2)] private int _axis;                              //ISerializable  0=X 1=Y 2=Z  spine direction
        [PmxKey(3)] private string _surfaceName;                    //ISerializable  source face surface for DLoad zones
        [PmxKey(4)] private BladeAeroDistanceTypeEnum _distanceType;//ISerializable  meaning of the table first column
        [PmxKey(5)] private List<BladeAeroTableRow> _tableRows;     //ISerializable  distance + force (N)
        [PmxKey(6)] private int _sectionCount;                      //ISerializable  number of DLoad zones created last time


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
        public override void CheckEquations() { }
    }
}
