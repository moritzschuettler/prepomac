using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;

namespace PrePoMax.Forms
{
    [Serializable]
    public class ViewCoordinateSystem
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private CoordinateSystem _coordinateSystem;
        private ItemSetData _point1ItemSetData;
        private ItemSetData _point2ItemSetData;
        private ItemSetData _point3ItemSetData;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the coordinate system.")]
        [Id(1, 1)]
        public string Name { get { return _coordinateSystem.Name; } set { _coordinateSystem.Name = value; } }
        //
        [Category("Data")]
        [OrderedDisplayName(1, 10, "Type")]
        [DescriptionAttribute("Type of the coordinate system.")]
        [Id(2, 1)]
        public CoordinateSystemTypeEnum Type { get { return _coordinateSystem.Type; } set { _coordinateSystem.Type = value; } }
        //
        [Category("Data")]
        [OrderedDisplayName(2, 10, "Created from")]
        [DescriptionAttribute("Select the method for the creation of the coordinate system.")]
        [Id(3, 1)]
        public CoordinateSystemCreatedFromEnum CreatedFrom
        {
            get { return _coordinateSystem.CreatedFrom; }
            set { _coordinateSystem.CreatedFrom = value; } 
        }
        //
        [Category("Point 1")]
        [OrderedDisplayName(0, 10, "By selection")]
        [DescriptionAttribute("Use selection to define point 1.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 2)]
        public ItemSetData Point1ItemSet
        {
            get { return _point1ItemSetData; }
            set
            {
                if (value != _point1ItemSetData) _point1ItemSetData = value;
            }
        }
        //
        [Category("Point 1")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X coordinate of point 1.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 2)]
        public EquationString X1
        {
            get { return _coordinateSystem.X1.Equation; }
            set
            {
                double oldX = _coordinateSystem.X1.Value;
                _coordinateSystem.X1.Equation = value;
                //
                if (_coordinateSystem.CreatedFrom == CoordinateSystemCreatedFromEnum.CenterXY)
                {
                    double deltaX = _coordinateSystem.X1.Value - oldX;
                    //
                    _coordinateSystem.X2.SetEquationFromValue(_coordinateSystem.X2.Value + deltaX, true);
                    _coordinateSystem.X3.SetEquationFromValue(_coordinateSystem.X3.Value + deltaX, true);
                }
            }
        }
        //
        [Category("Point 1")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y coordinate of point 1.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 2)]
        public EquationString Y1
        {
            get { return _coordinateSystem.Y1.Equation; }
            set
            {
                double oldY = _coordinateSystem.Y1.Value;
                _coordinateSystem.Y1.Equation = value;
                //
                if (_coordinateSystem.CreatedFrom == CoordinateSystemCreatedFromEnum.CenterXY)
                {
                    double deltaY = _coordinateSystem.Y1.Value - oldY;
                    //
                    _coordinateSystem.Y2.SetEquationFromValue(_coordinateSystem.Y2.Value + deltaY, true);
                    _coordinateSystem.Y3.SetEquationFromValue(_coordinateSystem.Y3.Value + deltaY, true);
                }
            }
        }
        //
        [Category("Point 1")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z coordinate of point 1.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 2)]
        public EquationString Z1
        {
            get { return _coordinateSystem.Z1.Equation; }
            set
            {
                double oldZ = _coordinateSystem.Z1.Value;
                _coordinateSystem.Z1.Equation = value;
                //
                if (_coordinateSystem.CreatedFrom == CoordinateSystemCreatedFromEnum.CenterXY)
                {
                    double deltaZ = _coordinateSystem.Z1.Value - oldZ;
                    //
                    _coordinateSystem.Z2.SetEquationFromValue(_coordinateSystem.Z2.Value + deltaZ, true);
                    _coordinateSystem.Z3.SetEquationFromValue(_coordinateSystem.Z3.Value + deltaZ, true);
                }
            }
        }
        //
        [Category("Point 2")]
        [OrderedDisplayName(0, 10, "By selection ")] // must be a different name than for the first point !!!
        [DescriptionAttribute("Use selection to define point 2.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public ItemSetData Point2ItemSet
        {
            get { return _point2ItemSetData; }
            set
            {
                if (value != _point2ItemSetData) _point2ItemSetData = value;
            }
        }
        //
        [Category("Point 2")]
        [OrderedDisplayName(1, 10, "X ")] // must be a different name than for the first point for auto select after edit
        [Description("X coordinate of point 2.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 3)]
        public EquationString X2 { get { return _coordinateSystem.X2.Equation; } set { _coordinateSystem.X2.Equation = value; } }
        //
        [Category("Point 2")]
        [OrderedDisplayName(2, 10, "Y ")] // must be a different name than for the first point for auto select after edit
        [Description("Y coordinate of point 2.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 3)]
        public EquationString Y2 { get { return _coordinateSystem.Y2.Equation; } set { _coordinateSystem.Y2.Equation = value; } }
        //
        [Category("Point 2")]
        [OrderedDisplayName(3, 10, "Z ")] // must be a different name than for the first point for auto select after edit
        [Description("Z coordinate of point 2.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 3)]
        public EquationString Z2 { get { return _coordinateSystem.Z2.Equation; } set { _coordinateSystem.Z2.Equation = value; } }
        //
        [Category("Point 3")]
        [OrderedDisplayName(0, 10, "By selection  ")] // must be a different name than for the first point !!!
        [DescriptionAttribute("Use selection to define point 3.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 4)]
        public ItemSetData Point3ItemSet
        {
            get { return _point3ItemSetData; }
            set
            {
                if (value != _point3ItemSetData) _point3ItemSetData = value;
            }
        }
        //
        [Category("Point 3")]
        [OrderedDisplayName(1, 10, "X  ")] // must be a different name than for the first point for auto select after edit
        [Description("X coordinate of point 3.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 4)]
        public EquationString X3 { get { return _coordinateSystem.X3.Equation; } set { _coordinateSystem.X3.Equation = value; } }
        //
        [Category("Point 3")]
        [OrderedDisplayName(2, 10, "Y  ")] // must be a different name than for the first point for auto select after edit
        [Description("Y coordinate of point 3.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 4)]
        public EquationString Y3 { get { return _coordinateSystem.Y3.Equation; } set { _coordinateSystem.Y3.Equation = value; } }
        //
        [Category("Point 3")]
        [OrderedDisplayName(3, 10, "Z  ")] // must be a different name than for the first point for auto select after edit
        [Description("Z coordinate of point 3.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 4)]
        public EquationString Z3 { get { return _coordinateSystem.Z3.Equation; } set { _coordinateSystem.Z3.Equation = value; } }
        //
        [Category("Directions")]
        [OrderedDisplayName(0, 10, "X   ")] // must be a different name than for the first point for auto select after edit
        [Description("Direction of the X axis.")]
        [Id(1, 5)]
        public string Nx
        {
            get
            {
                double[] nx = _coordinateSystem.DirectionX();
                return Math.Round(nx[0], 4).ToString() + "; " +
                       Math.Round(nx[1], 4).ToString() + "; " +
                       Math.Round(nx[2], 4).ToString();
            }
        }
        //
        [Category("Directions")]
        [OrderedDisplayName(1, 10, "Y   ")] // must be a different name than for the first point for auto select after edit
        [Description("Direction of the Y axis.")]
        [Id(2, 5)]
        public string Ny
        {
            get
            {
                double[] ny = _coordinateSystem.DirectionY();
                return Math.Round(ny[0], 4).ToString() + "; " +
                       Math.Round(ny[1], 4).ToString() + "; " +
                       Math.Round(ny[2], 4).ToString();
            }
        }
        //
        [Category("Directions")]
        [OrderedDisplayName(2, 10, "Z   ")] // must be a different name than for the first point for auto select after edit
        [Description("Direction of the Z axis.")]
        [Id(3, 5)]
        public string Nz
        {
            get
            {
                double[] nz = _coordinateSystem.DirectionZ();
                return Math.Round(nz[0], 4).ToString() + "; " +
                       Math.Round(nz[1], 4).ToString() + "; " +
                       Math.Round(nz[2], 4).ToString();
            }
        }


        // Constructors                                                                                                             
        public ViewCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            // The order is important
            _coordinateSystem = coordinateSystem;
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _point1ItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _point1ItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _point2ItemSetData = new ItemSetData();   // needed to display ItemSetData.ToString()
            _point2ItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _point3ItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _point3ItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _dctd.GetProperty(nameof(Z1)).SetIsBrowsable(!_coordinateSystem.TwoD);
            _dctd.GetProperty(nameof(Z2)).SetIsBrowsable(!_coordinateSystem.TwoD);
            _dctd.GetProperty(nameof(Z3)).SetIsBrowsable(!_coordinateSystem.TwoD);
            //
            if (_coordinateSystem.TwoD) _coordinateSystem.Type = CoordinateSystemTypeEnum.Rectangular;
            _dctd.GetProperty(nameof(Type)).SetIsReadOnly(_coordinateSystem.TwoD);
        }


        // Methods                                                                                                                  
        public CoordinateSystem GetBase()
        {
            return _coordinateSystem;
        }
    }
}
