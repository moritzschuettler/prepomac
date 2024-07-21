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
        [OrderedDisplayName(2, 10, "Name visible")]
        [DescriptionAttribute("Display the name of the coordinate system.")]
        [Id(3, 1)]
        public bool NameVisible { get { return _coordinateSystem.NameVisible; } set { _coordinateSystem.NameVisible = value; } }
        //
        [Category("Center")]
        [OrderedDisplayName(0, 10, "Create by/from")]
        [DescriptionAttribute("Select the method for the creation of the center point.")]
        [Id(1, 2)]
        public CsPointCreatedFromEnum CenterCreatedFrom
        {
            get { return _coordinateSystem.CenterCreatedFrom; }
            set
            {
                if (value != _coordinateSystem.CenterCreatedFrom)
                {
                    _coordinateSystem.CenterCreatedFrom = value;
                    UpdateVisibility();
                }
            }
        }
        //
        [Category("Center")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X coordinate of the center.")]
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
                double deltaX = _coordinateSystem.X1.Value - oldX;
                _coordinateSystem.X2.SetEquationFromValue(_coordinateSystem.X2.Value + deltaX, true);
                _coordinateSystem.X3.SetEquationFromValue(_coordinateSystem.X3.Value + deltaX, true);
            }
        }
        //
        [Category("Center")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y coordinate of the center.")]
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
                double deltaY = _coordinateSystem.Y1.Value - oldY;
                //
                _coordinateSystem.Y2.SetEquationFromValue(_coordinateSystem.Y2.Value + deltaY, true);
                _coordinateSystem.Y3.SetEquationFromValue(_coordinateSystem.Y3.Value + deltaY, true);
            }
        }
        //
        [Category("Center")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z coordinate of the center.")]
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
                double deltaZ = _coordinateSystem.Z1.Value - oldZ;
                //
                _coordinateSystem.Z2.SetEquationFromValue(_coordinateSystem.Z2.Value + deltaZ, true);
                _coordinateSystem.Z3.SetEquationFromValue(_coordinateSystem.Z3.Value + deltaZ, true);
            }
        }
        //
        [Category("Point in 1st axis direction")]
        [OrderedDisplayName(0, 10, "Create by/from ")] // must be a different name
        [DescriptionAttribute("Select the method for the creation of the point in the 1st axis direction.")]
        [Id(1, 3)]
        public CsPointCreatedFromEnum PointXCreatedFrom
        {
            get { return _coordinateSystem.PointXCreatedFrom; }
            set
            {
                if (value != _coordinateSystem.PointXCreatedFrom)
                {
                    _coordinateSystem.PointXCreatedFrom = value;
                    UpdateVisibility();
                }
            }
        }
        //
        [Category("Point in 1st axis direction")]
        [OrderedDisplayName(1, 10, "X ")] // must be a different name than for the first point for auto select after edit
        [Description("X coordinate of the point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 3)]
        public EquationString X2 { get { return _coordinateSystem.X2.Equation; } set { _coordinateSystem.X2.Equation = value; } }
        //
        [Category("Point in 1st axis direction")]
        [OrderedDisplayName(2, 10, "Y ")] // must be a different name than for the first point for auto select after edit
        [Description("Y coordinate of the point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 3)]
        public EquationString Y2 { get { return _coordinateSystem.Y2.Equation; } set { _coordinateSystem.Y2.Equation = value; } }
        //
        [Category("Point in 1st axis direction")]
        [OrderedDisplayName(3, 10, "Z ")] // must be a different name than for the first point for auto select after edit
        [Description("Z coordinate of the point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 3)]
        public EquationString Z2 { get { return _coordinateSystem.Z2.Equation; } set { _coordinateSystem.Z2.Equation = value; } }
        //
        [Category("Point on 1-2 plane")]
        [OrderedDisplayName(0, 10, "Create by/from  ")] // must be a different name
        [DescriptionAttribute("Select the method for the creation of the point in the 1st axis direction.")]
        [Id(1, 4)]
        public CsPointCreatedFromEnum PointXYCreatedFrom
        {
            get { return _coordinateSystem.PointXYCreatedFrom; }
            set
            {
                if (value != _coordinateSystem.PointXYCreatedFrom)
                {
                    _coordinateSystem.PointXYCreatedFrom = value;
                    UpdateVisibility();
                }
            }
        }
        //
        [Category("Point on 1-2 plane")]
        [OrderedDisplayName(1, 10, "X  ")] // must be a different name than for the first point for auto select after edit
        [Description("X coordinate of the point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 4)]
        public EquationString X3 { get { return _coordinateSystem.X3.Equation; } set { _coordinateSystem.X3.Equation = value; } }
        //
        [Category("Point on 1-2 plane")]
        [OrderedDisplayName(2, 10, "Y  ")] // must be a different name than for the first point for auto select after edit
        [Description("Y coordinate of the point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 4)]
        public EquationString Y3 { get { return _coordinateSystem.Y3.Equation; } set { _coordinateSystem.Y3.Equation = value; } }
        //
        [Category("Point on 1-2 plane")]
        [OrderedDisplayName(3, 10, "Z  ")] // must be a different name than for the first point for auto select after edit
        [Description("Z coordinate of the point.")]
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
                double[] nx = _coordinateSystem.DirectionX().CoorRounded(4);
                return nx[0] + "; " + nx[1] + "; " + nx[2];
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
                double[] ny = _coordinateSystem.DirectionY().CoorRounded(4);
                return ny[0] + "; " + ny[1] + "; " + ny[2];
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
                double[] nz = _coordinateSystem.DirectionZ().CoorRounded(4);
                return nz[0] + "; " + nz[1] + "; " + nz[2];
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
            _dctd.GetProperty(nameof(Z1)).SetIsBrowsable(!_coordinateSystem.TwoD);
            _dctd.GetProperty(nameof(Z2)).SetIsBrowsable(!_coordinateSystem.TwoD);
            _dctd.GetProperty(nameof(Z3)).SetIsBrowsable(!_coordinateSystem.TwoD);
            //
            if (_coordinateSystem.TwoD) _coordinateSystem.Type = CoordinateSystemTypeEnum.Rectangular;
            _dctd.GetProperty(nameof(Type)).SetIsReadOnly(_coordinateSystem.TwoD);
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(NameVisible));
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public CoordinateSystem GetBase()
        {
            return _coordinateSystem;
        }
        private void UpdateVisibility()
        {
            bool readOnly;
            //
            readOnly = _coordinateSystem.CenterCreatedFrom != CsPointCreatedFromEnum.Coordinates;
            _dctd.GetProperty(nameof(X1)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Y1)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Z1)).SetIsReadOnly(readOnly);
            //
            readOnly = _coordinateSystem.PointXCreatedFrom != CsPointCreatedFromEnum.Coordinates;
            _dctd.GetProperty(nameof(X2)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Y2)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Z2)).SetIsReadOnly(readOnly);
            //
            readOnly = _coordinateSystem.PointXYCreatedFrom != CsPointCreatedFromEnum.Coordinates;
            _dctd.GetProperty(nameof(X3)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Y3)).SetIsReadOnly(readOnly);
            _dctd.GetProperty(nameof(Z3)).SetIsReadOnly(readOnly);
        }
    }
}
