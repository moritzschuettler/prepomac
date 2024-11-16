using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewInitialTranslationalVelocity : ViewInitialCondition
    {
        // Variables                                                                                                                
        private InitialTranslationalVelocity _initialVelocity;


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
        [CategoryAttribute("Velocity components")]
        [OrderedDisplayName(0, 10, "V1")]
        [DescriptionAttribute("Value of the velocity component in the direction of the first axis.")]
        [TypeConverter(typeof(EquationVelocityConverter))]
        [Id(1, 3)]
        public EquationString V1 { get { return _initialVelocity.V1.Equation; } set { _initialVelocity.V1.Equation = value; } }
        //
        [CategoryAttribute("Velocity components")]
        [OrderedDisplayName(1, 10, "V2")]
        [DescriptionAttribute("Value of the velocity component in the direction of the second axis.")]
        [TypeConverter(typeof(EquationVelocityConverter))]
        [Id(2, 3)]
        public EquationString V2 { get { return _initialVelocity.V2.Equation; } set { _initialVelocity.V2.Equation = value; } }
        //
        [CategoryAttribute("Velocity components")]
        [OrderedDisplayName(2, 10, "V3")]
        [DescriptionAttribute("Value of the velocity component in the direction of the third axis.")]
        [TypeConverter(typeof(EquationVelocityConverter))]
        [Id(3, 3)]
        public EquationString V3 { get { return _initialVelocity.V3.Equation; } set { _initialVelocity.V3.Equation = value; } }
        //
        [CategoryAttribute("Velocity magnitude")]
        [OrderedDisplayName(3, 10, "Magnitude")]
        [DescriptionAttribute("Value of the velocity magnitude.")]
        [TypeConverter(typeof(EquationVelocityConverter))]
        [Id(1, 4)]
        public EquationString Magnitude
        {
            get { return _initialVelocity.Magnitude.Equation; }
            set { _initialVelocity.Magnitude.Equation = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _initialVelocity.Color; }
            set { _initialVelocity.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewInitialTranslationalVelocity(InitialTranslationalVelocity initialVelocity)
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
            DynamicCustomTypeDescriptor.GetProperty(nameof(V3)).SetIsBrowsable(!initialVelocity.TwoD);
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
