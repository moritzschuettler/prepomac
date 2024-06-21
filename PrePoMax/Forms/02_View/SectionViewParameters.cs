using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Design;
using System.Drawing;

namespace PrePoMax.Forms
{
    [Serializable]
    public class SectionViewParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private ItemSetData _pointItemSetData;
        private ItemSetData _normalItemSetData;
        private double[] _point;
        private double[] _normal;
        private bool _lightenColors = true;
        private bool _useSingleColor = false;
        private Color _sectionColor = Color.LightGreen;


        // Properties                                                                                                               
        [Category("Point coordinates")]
        [OrderedDisplayName(0, 10, "Select the plane point")]
        [DescriptionAttribute("Select the plane point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 1)]
        public ItemSetData PointItemSet
        {
            get { return _pointItemSetData; }
            set
            {
                if (value != _pointItemSetData)
                    _pointItemSetData = value;
            }
        }
        //
        [Category("Point coordinates")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X coordinate of the point on plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 1)]
        public double X { get { return _point[0]; } set { _point[0] = value; } }
        //
        [Category("Point coordinates")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y coordinate of the point on plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 1)]
        public double Y { get { return _point[1]; } set { _point[1] = value; } }
        //
        [Category("Point coordinates")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z coordinate of the point on plane.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 1)]
        public double Z { get { return _point[2]; } set { _point[2] = value; } }
        //
        //
        [Category("Normal direction")]
        [OrderedDisplayName(0, 10, "Select the normal vector")]
        [DescriptionAttribute("Select the plane normal as a vector between two points.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 2)]
        public ItemSetData NormalItemSet
        {
            get { return _normalItemSetData; }
            set
            {
                if (value != _normalItemSetData)
                    _normalItemSetData = value;
            }
        }
        //
        [Category("Normal direction")]
        [OrderedDisplayName(1, 10, "X")]
        [Description("X component of the normal direction.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(2, 2)]
        public double Nx { get { return _normal[0]; } set { _normal[0] = value; } }
        //
        [Category("Normal direction")]
        [OrderedDisplayName(2, 10, "Y")]
        [Description("Y component of the normal direction.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double Ny { get { return _normal[1]; } set { _normal[1] = value; } }
        //
        [Category("Normal direction")]
        [OrderedDisplayName(3, 10, "Z")]
        [Description("Z component of the normal direction.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 2)]
        public double Nz { get { return _normal[2]; } set { _normal[2] = value; } }
        //
        //
        [Category("Appearance")]
        [OrderedDisplayName(0, 10, "Lighten section colors")]
        [Description("Select Yes to lighten section colors.")]
        [Id(1, 3)]
        public bool LightenColors
        {
            get { return _lightenColors; }
            set
            {
                _lightenColors = value;
                if (_lightenColors) _useSingleColor = false;
                UpdateVisibility();
            }
        }
        [Category("Appearance")]
        [OrderedDisplayName(1, 10, "Use single section color")]
        [Description("Select Yes to apply a single color to the section.")]
        [Id(2, 3)]
        public bool UseSingleColor
        {
            get { return _useSingleColor; }
            set
            {
                _useSingleColor = value;
                if (_useSingleColor) _lightenColors = false;
                UpdateVisibility();
            }
        }
        //
        [Category("Appearance")]
        [OrderedDisplayName(2, 10, "Color")]
        [Description("Select the single color for the section.")]
        [Id(3, 3)]
        public Color SectionColor
        {
            get
            {
                if (_useSingleColor) return _sectionColor;
                else return Color.Empty;
            }
            set { _sectionColor = value; }
        }
        //
        [Browsable(false)]
        public double[] Point { get { return _point; } set { _point = value; } }
        [Browsable(false)]
        public double[] Normal { get { return _normal; } set { _normal = value; } }


        // Constructors                                                                                                             
        public SectionViewParameters()
        {
            Clear();
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _pointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _pointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _normalItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _normalItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
            //
            _dctd.RenameBooleanPropertyToYesNo(nameof(LightenColors));
            _dctd.RenameBooleanPropertyToYesNo(nameof(UseSingleColor));
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _point = new double[] { 0, 0, 0 };
            _normal = new double[] { 1, 0, 0 };
        }
        private void UpdateVisibility()
        {
            _dctd.GetProperty(nameof(SectionColor)).SetIsBrowsable(_useSingleColor);
        }

       
    }
}
