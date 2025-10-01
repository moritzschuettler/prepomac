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
    public class ViewUserViewParameters
    {
        // Variables                                                                                                                
        private DynamicCustomTypeDescriptor _dctd = null;
        private UserViewParameters _parameters;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [DescriptionAttribute("Name of the view.")]
        [Id(1, 1)]
        public string Name { get { return _parameters.Name; } set { _parameters.Name = value; } }
        //
        [CategoryAttribute("Position")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the camera.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 2)]
        public double PositionX
        {
            get { return _parameters.Position == null? 0 : _parameters.Position[0]; }
            set { _parameters.Position[0] = value; }
        }
        //
        [CategoryAttribute("Position")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the camera.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 2)]
        public double PositionY
        {
            get { return _parameters.Position == null ? 0 : _parameters.Position[1]; }
            set { _parameters.Position[1] = value; }
        }
        //
        [CategoryAttribute("Position")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the camera.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double PositionZ
        {
            get { return _parameters.Position == null ? 0 : _parameters.Position[2]; }
            set { _parameters.Position[2] = value; }
        }
        //
        [CategoryAttribute("Focal point")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the focal point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 3)]
        public double FocalPointX
        {
            get { return _parameters.FocalPoint == null ? 0 : _parameters.FocalPoint[0]; }
            set { _parameters.FocalPoint[0] = value; }
        }
        //
        [CategoryAttribute("Focal point")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the focal point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 3)]
        public double FocalPointY
        {
            get { return _parameters.FocalPoint == null ? 0 : _parameters.FocalPoint[1]; }
            set { _parameters.FocalPoint[1] = value; }
        }
        //
        [CategoryAttribute("Focal point")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the focal point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double FocalPointZ
        {
            get { return _parameters.FocalPoint == null ? 0 : _parameters.FocalPoint[2]; }
            set { _parameters.FocalPoint[2] = value; }
        }
        //
        [CategoryAttribute("Up vector")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("X coordinate of the up vector.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(1, 4)]
        public double UpVectorX
        {
            get { return _parameters.UpVector == null ? 0 : _parameters.UpVector[0]; }
            set { _parameters.UpVector[0] = value; }
        }
        //
        [CategoryAttribute("Up vector")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the up vector.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 4)]
        public double UpVectorY
        {
            get { return _parameters.UpVector == null ? 0 : _parameters.UpVector[1]; }
            set { _parameters.UpVector[1] = value; }
        }
        //
        [CategoryAttribute("Up vector")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the up vector.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 4)]
        public double UpVectorZ
        {
            get { return _parameters.UpVector == null ? 0 : _parameters.UpVector[2]; }
            set { _parameters.UpVector[2] = value; }
        }
        //
        [CategoryAttribute("Zoom")]
        [OrderedDisplayName(0, 10, "Value")]
        [DescriptionAttribute("Zoom value.")]
        [Id(1, 5)]
        public double Zoom
        {
            get { return 1 / _parameters.ParallelScale; }
            set
            {
                if (value > 0) _parameters.ParallelScale = 1 / value;
            }
        }


        // Constructors                                                                                                             
        public ViewUserViewParameters(UserViewParameters parameters)
        {
            _parameters = parameters;
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            UpdateVisibility();
        }
        public UserViewParameters GetBase()
        {
            return _parameters;
        }


        // Methods                                                                                                                  
        public void UpdateVisibility()
        {
            
        }
    }
}
