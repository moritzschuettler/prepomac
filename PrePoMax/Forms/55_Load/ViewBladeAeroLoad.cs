using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;


namespace PrePoMax
{
    [Serializable]
    public class ViewBladeAeroLoad : ViewLoad
    {
        // Variables
        internal const string AbsoluteDistanceText = "Absolute distance";
        internal const string SpanRatioText = "Span ratio (0-1 or %)";
        private BladeAeroLoad _load;


        // Properties
        public override string Name { get { return _load.Name; } set { _load.Name = value; } }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(0, 10, "Spine direction")]
        [DescriptionAttribute("Main axis along which sections are sliced (X, Y, or Z).")]
        [Id(1, 6)]
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
        [OrderedDisplayName(1, 10, "Force direction")]
        [DescriptionAttribute("Axis along which the aerodynamic force is applied (X, Y, or Z). Cannot be the same as the spine direction.")]
        [Id(2, 6)]
        public string ForceDirection
        {
            get
            {
                if (_load.ForceAxis == 0) return "X";
                if (_load.ForceAxis == 1) return "Y";
                return "Z";
            }
            set
            {
                if (value == "X") _load.ForceAxis = 0;
                else if (value == "Y") _load.ForceAxis = 1;
                else _load.ForceAxis = 2;
            }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(2, 10, "Distance type")]
        [DescriptionAttribute("Meaning of the first column of the load forces table: an absolute distance " +
                              "measured from the origin in model units, or a ratio/percentage of the blade " +
                              "span measured from the blade root.")]
        [Id(3, 6)]
        public string DistanceType
        {
            get
            {
                return _load.DistanceType == BladeAeroDistanceTypeEnum.Absolute
                    ? AbsoluteDistanceText : SpanRatioText;
            }
            set
            {
                _load.DistanceType = value == AbsoluteDistanceText
                    ? BladeAeroDistanceTypeEnum.Absolute : BladeAeroDistanceTypeEnum.Relative;
            }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(3, 10, "Origin X")]
        [DescriptionAttribute("X coordinate the table distances are measured from. Leave at 0 when the table " +
                              "holds model coordinates; set it to the blade root when the distances start there.")]
        [Id(4, 6)]
        [DefaultValue(0.0)]
        public double OriginX
        {
            get { return _load.OriginX; }
            set { _load.OriginX = value; }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(4, 10, "Origin Y")]
        [DescriptionAttribute("Y coordinate the table distances are measured from. Leave at 0 when the table " +
                              "holds model coordinates; set it to the blade root when the distances start there.")]
        [Id(5, 6)]
        [DefaultValue(0.0)]
        public double OriginY
        {
            get { return _load.OriginY; }
            set { _load.OriginY = value; }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(5, 10, "Origin Z")]
        [DescriptionAttribute("Z coordinate the table distances are measured from. Leave at 0 when the table " +
                              "holds model coordinates; set it to the blade root when the distances start there.")]
        [Id(6, 6)]
        [DefaultValue(0.0)]
        public double OriginZ
        {
            get { return _load.OriginZ; }
            set { _load.OriginZ = value; }
        }
        //
        [Browsable(false)]
        public bool InvertForces
        {
            get { return _load.InvertForces; }
            set { _load.InvertForces = value; }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(6, 10, "Load forces table")]
        [DescriptionAttribute("Click ... to open the table editor. Enter one row per section: distance " +
                              "(as set by 'Distance type') and force (N).")]
        [Id(7, 6)]
        [Editor(typeof(Forms.BladeAeroTableUIEditor), typeof(UITypeEditor))]
        [TypeConverter(typeof(BladeAeroTableConverter))]
        public List<BladeAeroTableRow> LoadForces
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
        public ViewBladeAeroLoad(BladeAeroLoad load)
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
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(ForceDirection),
                new string[] { "X", "Y", "Z" }, false, 1);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistanceType),
                new string[] { AbsoluteDistanceText, SpanRatioText }, false, 1);
        }


        // Methods
        public override Load GetBase() { return _load; }
    }


    class BladeAeroTableConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string)) return true;
            return base.CanConvertTo(context, destinationType);
        }
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is List<BladeAeroTableRow> rows)
                return rows.Count == 0 ? "(empty — click ... to edit)" : $"({rows.Count} rows)";
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
