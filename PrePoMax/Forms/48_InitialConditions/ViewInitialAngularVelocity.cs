using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;
using System.Drawing.Design;

namespace PrePoMax
{
    [Serializable]
    public class ViewInitialAngularVelocity : ViewInitialCondition
    {
        // Variables                                                                                                                
        private InitialAngularVelocity _initialVelocity;
        private ItemSetData _centerPointItemSetData;


        // Properties                                                                                                               
        public override string Name { get { return _initialVelocity.Name; } set { _initialVelocity.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the assignment of the initial velocity.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _initialVelocity.RegionName; } set { _initialVelocity.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the assignment of the initial velocity.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _initialVelocity.RegionName; } set { _initialVelocity.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(4, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the initial velocity.")]
        [Id(5, 2)]
        public string ReferencePointName
        {
            get { return _initialVelocity.RegionName; }
            set { _initialVelocity.RegionName = value; }
        }
        //
        [Category("Rotation center coordinates")]
        [OrderedDisplayName(0, 10, "By selection")]
        [DescriptionAttribute("Use selection for the definition of the rotation center.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public ItemSetData CenterPointItemSet
        {
            get { return _centerPointItemSetData; }
            set
            {
                if (value != _centerPointItemSetData)
                    _centerPointItemSetData = value;
            }
        }
        //
        [CategoryAttribute("Rotation center coordinates")]
        [OrderedDisplayName(1, 10, "X")]
        [DescriptionAttribute("X coordinate of the axis point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 3)]
        public EquationString X { get { return _initialVelocity.X.Equation; } set { _initialVelocity.X.Equation = value; } }
        //
        [CategoryAttribute("Rotation center coordinates")]
        [OrderedDisplayName(2, 10, "Y")]
        [DescriptionAttribute("Y coordinate of the axis point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 3)]
        public EquationString Y { get { return _initialVelocity.Y.Equation; } set { _initialVelocity.Y.Equation = value; } }
        //
        [CategoryAttribute("Rotation center coordinates")]
        [OrderedDisplayName(3, 10, "Z")]
        [DescriptionAttribute("Z coordinate of the axis point.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 3)]
        public EquationString Z { get { return _initialVelocity.Z.Equation; } set { _initialVelocity.Z.Equation = value; } }
        //
        [CategoryAttribute("Rotation axis components")]
        [OrderedDisplayName(0, 10, "N1")]
        [DescriptionAttribute("Axis component in the direction of the first axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(1, 4)]
        public EquationString N1 { get { return _initialVelocity.N1.Equation; } set { _initialVelocity.N1.Equation = value; } }
        //
        [CategoryAttribute("Rotation axis components")]
        [OrderedDisplayName(1, 10, "N2")]
        [DescriptionAttribute("Axis component in the direction of the second axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 4)]
        public EquationString N2 { get { return _initialVelocity.N2.Equation; } set { _initialVelocity.N2.Equation = value; } }
        //
        [CategoryAttribute("Rotation axis components")]
        [OrderedDisplayName(2, 10, "N3")]
        [DescriptionAttribute("Axis component in the direction of the third axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 4)]
        public EquationString N3 { get { return _initialVelocity.N3.Equation; } set { _initialVelocity.N3.Equation = value; } }
        //
        [CategoryAttribute("Angular velocity magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the angular velocity magnitude around the axis defined by the point and direction.")]
        [TypeConverter(typeof(EquationRotationalSpeedConverter))]
        [Id(1, 5)]
        public EquationString RotationalSpeed
        {
            get { return _initialVelocity.RotationalSpeed.Equation; }
            set { _initialVelocity.RotationalSpeed.Equation = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _initialVelocity.Color; }
            set { _initialVelocity.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewInitialAngularVelocity(InitialAngularVelocity initialVelocity)
        {
            // The order is important
            _initialVelocity = initialVelocity;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            SetBase(_initialVelocity, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(Z)).SetIsBrowsable(!initialVelocity.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N1)).SetIsBrowsable(!initialVelocity.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(N2)).SetIsBrowsable(!initialVelocity.TwoD);
            //
            _centerPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _centerPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
        }


        // Methods                                                                                                                  
        public override InitialCondition GetBase()
        {
            return _initialVelocity;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames, string[] referencePointNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            base.PopulateDropDownLists(regionTypeListItemsPairs);
        }
    }



   
}
