using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;


namespace PrePoMax
{
    [Serializable]
    public class ViewBladeAeroPressureLoad : ViewLoad
    {
        // Variables
        private BladeAeroPressureLoad _load;


        // Properties
        public override string Name { get { return _load.Name; } set { _load.Name = value; } }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(0, 10, "Spine direction")]
        [DescriptionAttribute("Main axis along which pressure sections are sliced (X, Y, or Z).")]
        [Id(1, 3)]
        public string Direction
        {
            get
            {
                if (_load.Axis == 0) return "X";
                if (_load.Axis == 1) return "Y";
                return "Z";
            }
            set
            {
                if (value == "X") _load.Axis = 0;
                else if (value == "Y") _load.Axis = 1;
                else _load.Axis = 2;
            }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(1, 10, "Face surface")]
        [DescriptionAttribute("Select the face surface to which the pressure zones will be applied.")]
        [Id(2, 3)]
        public string SurfaceName
        {
            get { return _load.SurfaceName; }
            set { _load.SurfaceName = value; }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(2, 10, "Distance type")]
        [DescriptionAttribute("Meaning of the first column of the load pressures table: an absolute distance " +
                              "from the surface start in model units, or a ratio/percentage of the span.")]
        [Id(3, 3)]
        public string DistanceType
        {
            get
            {
                return _load.DistanceType == BladeAeroDistanceTypeEnum.Absolute
                    ? ViewBladeAeroLoad.AbsoluteDistanceText : ViewBladeAeroLoad.SpanRatioText;
            }
            set
            {
                _load.DistanceType = value == ViewBladeAeroLoad.AbsoluteDistanceText
                    ? BladeAeroDistanceTypeEnum.Absolute : BladeAeroDistanceTypeEnum.Relative;
            }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(3, 10, "Load pressures table")]
        [DescriptionAttribute("Click ... to open the table editor. Enter one row per zone: distance " +
                              "(as set by 'Distance type') and force (N). Pressure = force / zone area.")]
        [Id(4, 3)]
        [Editor(typeof(Forms.BladeAeroTableUIEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(BladeAeroTableConverter))]
        public List<BladeAeroTableRow> LoadPressures
        {
            get { return _load.TableRows; }
            set { _load.TableRows = value; }
        }
        //
        [Browsable(false)]
        public override string AmplitudeName
        {
            get { return _load.AmplitudeName; }
            set { _load.AmplitudeName = value; }
        }
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _load.CoordinateSystemName; }
            set { _load.CoordinateSystemName = value; }
        }
        public override Color Color { get { return _load.Color; } set { _load.Color = value; } }


        // Constructors
        public ViewBladeAeroPressureLoad(BladeAeroPressureLoad load)
        {
            _load = load;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            SetBase(_load, regionTypePropertyNamePairs);
            //
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
            DynamicCustomTypeDescriptor.GetProperty(nameof(RegionType)).SetIsBrowsable(false);
            //
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(Direction),
                new string[] { "X", "Y", "Z" }, false, 1);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistanceType),
                new string[] { ViewBladeAeroLoad.AbsoluteDistanceText, ViewBladeAeroLoad.SpanRatioText },
                false, 1);
        }


        // Methods
        public void PopulateDropDownLists(string[] surfaceNames)
        {
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(SurfaceName), surfaceNames, true);
            if (surfaceNames.Length > 0 && string.IsNullOrEmpty(_load.SurfaceName))
                _load.SurfaceName = surfaceNames[0];
        }
        public override Load GetBase() { return _load; }
    }
}
