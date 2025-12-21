using CaeGlobals;
using CaeMesh;
using CaeModel;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax.Forms
{
    [Serializable]
    public class MirrorParameters
    {
        // Variables                                                                                                                      
        private DynamicCustomTypeDescriptor _dctd = null;
        private PointSelectionMethodEnum _mirrorPointSelectionMethod;
        private PointSelectionMethodEnum _startPointSelectionMethod;
        private PointSelectionMethodEnum _endPointSelectionMethod;
        private ItemSetData _mirrorPointItemSetData;
        private ItemSetData _startPointItemSetData;
        private ItemSetData _endPointItemSetData;
        private double[] _mirrorPoint;
        private double[] _startPoint;
        private double[] _endPoint;
        private bool _mirrorOnly;
        private bool _twoD;


        // Properties                                                                                                               
        [Category("Data")]
        [OrderedDisplayName(0, 10, "Operation")]
        [DescriptionAttribute("Select the mirror operation.")]
        [Id(1, 1)]
        public bool MirrorOnly { get { return _mirrorOnly; } set { _mirrorOnly = value; } }
        //                                                          
        [Category("Mirror point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method")]
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 2)]
        public PointSelectionMethodEnum MirrorPointSelectionMethod
        {
            get { return _mirrorPointSelectionMethod; }
            set
            {
                _mirrorPointSelectionMethod = value;
                //
                if (_mirrorPointSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _mirrorPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_mirrorPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _mirrorPointItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_mirrorPointSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _mirrorPointItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("Mirror point coordinates")]
        [OrderedDisplayName(1, 10, "By selection")]
        [DescriptionAttribute("Use selection for the definition of the mirror point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 2)]
        public ItemSetData MirrorPointItemSet
        {
            get { return _mirrorPointItemSetData; }
            set
            {
                if (value != _mirrorPointItemSetData) _mirrorPointItemSetData = value;
            }
        }
        //
        [Category("Mirror point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the mirror point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 2)]
        public double MirrorPointX { get { return _mirrorPoint[0]; } set { _mirrorPoint[0] = value; } }
        //
        [Category("Mirror point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the mirror point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 2)]
        public double MirrorPointY { get { return _mirrorPoint[1]; } set { _mirrorPoint[1] = value; } }
        //
        [Category("Mirror point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the mirror point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 2)]
        public double MirrorPointZ { get { return _mirrorPoint[2]; } set { _mirrorPoint[2] = value; } }
        //                                                          
        [Category("Start direction point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method ")]     // must be a different name than for the first point !!!
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 3)]
        public PointSelectionMethodEnum StartPointSelectionMethod
        {
            get { return _startPointSelectionMethod; }
            set
            {
                _startPointSelectionMethod = value;
                //
                if (_startPointSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_startPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_startPointSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("Start direction point coordinates")]
        [OrderedDisplayName(1, 10, "By selection ")]    // must be a different name than for the first point !!!
        [DescriptionAttribute("Use selection for the definition of the start point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 3)]
        public ItemSetData StartPointItemSet
        {
            get { return _startPointItemSetData; }
            set
            {
                if (value != _startPointItemSetData) _startPointItemSetData = value;
            }
        }
        //
        [Category("Start direction point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 3)]
        public double X1
        {
            get { return _startPoint[0]; }
            set
            {
                _startPoint[0] = value;
                if (_twoD) _endPoint[0] = value;
            }
        }
        //
        [Category("Start direction point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 3)]
        public double Y1
        {
            get { return _startPoint[1]; }
            set
            {
                _startPoint[1] = value;
                if (_twoD) _endPoint[1] = value;
            }
        }
        //
        [Category("Start direction point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the start point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 3)]
        public double Z1
        {
            get { return _startPoint[2]; }
            set
            {
                _startPoint[2] = value;
                if (_twoD) _startPoint[2] = 0;
            }
        }
        //                                                          
        [Category("End direction point coordinates")]
        [OrderedDisplayName(0, 10, "Selection method  ")]     // must be a different name than for the first point !!!
        [DescriptionAttribute("Choose the selection method.")]
        [Id(1, 4)]
        public PointSelectionMethodEnum EndPointSelectionMethod
        {
            get { return _endPointSelectionMethod; }
            set
            {
                _endPointSelectionMethod = value;
                //
                if (_endPointSelectionMethod == PointSelectionMethodEnum.OnPoint)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
                else if (_endPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectTwoPoints;
                else if (_endPointSelectionMethod == PointSelectionMethodEnum.CircleCenter)
                    _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectThreePoints;
                else throw new NotSupportedException();
            }
        }
        //
        [Category("End direction point coordinates")]
        [OrderedDisplayName(1, 10, "By selection  ")]    // must be a different name than for the first point !!!
        [DescriptionAttribute("Use selection for the definition of the end point.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(2, 4)]
        public ItemSetData EndPointItemSet
        {
            get { return _endPointItemSetData; }
            set
            {
                if (value != _endPointItemSetData) _endPointItemSetData = value;
            }
        }
        //
        [Category("End direction point coordinates")]
        [OrderedDisplayName(2, 10, "X")]
        [Description("X coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(3, 4)]
        public double X2 { get { return _endPoint[0]; } set { _endPoint[0] = value; } }
        //
        [Category("End direction point coordinates")]
        [OrderedDisplayName(3, 10, "Y")]
        [Description("Y coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(4, 4)]
        public double Y2 { get { return _endPoint[1]; } set { _endPoint[1] = value; } }
        //
        [Category("End direction point coordinates")]
        [OrderedDisplayName(4, 10, "Z")]
        [Description("Z coordinate of the end point.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(5, 4)]
        public double Z2
        {
            get { return _endPoint[2]; }
            set
            {
                _endPoint[2] = value;
                if (_twoD) _endPoint[2] = 1;
            }
        }
        //
        [Browsable(false)]
        public bool Copy { get { return !_mirrorOnly; } }
        [Browsable(false)]
        public double[] MirrorPoint { get { return _mirrorPoint.ToArray(); } }
        [Browsable(false)]
        public double[] MirrorDirection
        {
            get
            {
                Vec3D d = new Vec3D(_endPoint[0] - _startPoint[0], _endPoint[1] - _startPoint[1], _endPoint[2] - _startPoint[2]);
                d.Normalize();
                return d.Coor;
            }
        }


        // Constructors                                                                                                             
        public MirrorParameters(ModelSpaceEnum modelSpace)
        {
            Clear();
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.CategorySortOrder = CustomSortOrder.AscendingById;
            _dctd.PropertySortOrder = CustomSortOrder.AscendingById;
            //
            _mirrorPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _mirrorPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _startPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _startPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            _endPointItemSetData = new ItemSetData();   // needed to display ItemSetData.ToString()
            _endPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            _dctd.RenameBooleanProperty(nameof(MirrorOnly), "Mirror", "Copy and mirror");
            //
            if (modelSpace == ModelSpaceEnum.ThreeD) { _twoD = false; }
            else if (modelSpace.IsTwoD())
            {
                _twoD = true;
                Z1 = 0;
                Z2 = 1;
            }
            else throw new NotSupportedException();
            //
            UpdateVisibility();
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _mirrorOnly = true;
            ClearMirror();
        }
        public void ClearMirror()
        {
            _mirrorPoint = new double[3];
            _startPoint = new double[3];
            _endPoint = new double[3];
            //
            if (_twoD)
            {
                MirrorPointZ = 0;
                Z1 = 0;
                Z2 = 0;
            }
        }
        public void UpdateVisibility()
        {
            _dctd.GetProperty(nameof(MirrorPointZ)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(Z1)).SetIsBrowsable(!_twoD);
            _dctd.GetProperty(nameof(Z2)).SetIsBrowsable(!_twoD);
        }
    }
}
