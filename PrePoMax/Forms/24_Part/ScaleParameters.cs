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
    public class ScaleParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private PointSelectionMethodEnum _scaleCenterSelectionMethod;
        private ItemSetData _scaleCenterItemSetData;
        private double[] _scaleCenter;
        private double[] _scaleFactors;
        private bool _scaleOnly;
        private bool _twoD;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Operation")]
        [DescriptionAttribute("Select the scale operation.")]
        [Id(1, 1)]
        public bool ScaleOnly { get { return _scaleOnly; } set { _scaleOnly = value; } }
        //
        [Category("Center point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method")]
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 2)]
        public PointSelectionMethodEnum ScaleCenterSelectionMethod
        {
            get { return _scaleCenterSelectionMethod; }
            set
            {
                _scaleCenterSelectionMethod = value;
                //
                if (_scaleCenterSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _scaleCenterItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_scaleCenterSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _scaleCenterItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_scaleCenterSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _scaleCenterItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("Center point coordinates")]
        [OrderedDisplayName(1, 10, "By selection")]
        [DescriptionAttribute("Use selection for the definition of the center point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 2)]
        public ItemSetData ScaleCenterItemSet
        {
            get { return _scaleCenterItemSetData; }
            set
            {
                if (value != _scaleCenterItemSetData)
                    _scaleCenterItemSetData = value;
            }
        }
        //
        [Category("Center point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the center point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double CenterX { get { return _scaleCenter[0]; } set { _scaleCenter[0] = value; } }
        //
        [Category("Center point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the center point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 2)]
        public double CenterY { get { return _scaleCenter[1]; } set { _scaleCenter[1] = value; } }
        //
        [Category("Center point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the center point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 2)]
        public double CenterZ
        {
            get { return _scaleCenter[2]; }
            set
            {
                _scaleCenter[2] = value;
                if (_twoD) _scaleCenter[2] = 0;
            }
        }
        //
        [Category("Scale factors")]
        [OrderedDisplayName(0, 10, "X")]
        [Description("Scale factor in the X direction.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(1, 3)]
        public double FactorX
        {
            get { return _scaleFactors[0]; }
            set
            {
                _scaleFactors[0] = value;
                if (_scaleFactors[0] <= 0) _scaleFactors[0] = 1;
            }
        }
        //
        [Category("Scale factors")]
        [OrderedDisplayName(1, 10, "Y")]
        [Description("Scale factor in the Y direction.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(2, 3)]
        public double FactorY
        {
            get { return _scaleFactors[1]; }
            set 
            {
                _scaleFactors[1] = value;
                if (_scaleFactors[1] <= 0) _scaleFactors[1] = 1;
            }
        }
        //
        [Category("Scale factors")]
        [OrderedDisplayName(2, 10, "Z")]
        [Description("Scale factor in the Z direction.")]
        [TypeConverter(typeof(StringDoubleConverter))]
        [Id(3, 3)]
        public double FactorZ
        {
            get { return _scaleFactors[2]; }
            set
            {
                _scaleFactors[2] = value;
                if (_scaleFactors[2] <= 0 || _twoD) _scaleFactors[2] = 1;
            }
        }
        //
        [Browsable(false)]
        public bool Copy { get { return !_scaleOnly; } }
        [Browsable(false)]
        public double[] ScaleCenter { get { return _scaleCenter.ToArray(); } }
        [Browsable(false)]
        public double[] ScaleFactors { get { return _scaleFactors.ToArray(); } }


        // Constructors                                                                                                             
        public ScaleParameters(ModelSpaceEnum modelSpace)
        {
            Clear();
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _scaleCenterItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _scaleCenterItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _dctd.RenameBooleanProperty(nameof(ScaleOnly), "Scale", "Copy and scale");
            //
            if (modelSpace == ModelSpaceEnum.ThreeD) { _twoD = false; }
            else if (modelSpace.IsTwoD())
            {
                _twoD = true;
                CenterZ = 0;
                FactorZ = 1;
            }
            else throw new NotSupportedException();
            //
            _dctd.GetProperty(nameof(CenterZ)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(FactorZ)).SetIsBrowsable(!_twoD);
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _scaleOnly = true;
            //
            _scaleCenter = new double[3];
            _scaleFactors = new double[] { 1, 1, 1 };
            //
            if (_twoD)
            {
                CenterZ = 0;
                FactorZ = 1;
            }
        }
    }
}
