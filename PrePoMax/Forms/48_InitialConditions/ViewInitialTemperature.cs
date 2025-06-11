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
    public class ViewInitialTemperature : ViewInitialCondition
    {
        // Variables                                                                                                                
        private InitialTemperature _initialTemperature;


        // Properties                                                                                                               
        public override string Name { get { return _initialTemperature.Name; } set { _initialTemperature.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the assignment of the initial temperature.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _initialTemperature.RegionName; } set { _initialTemperature.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the assignment of the initial temperature.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _initialTemperature.RegionName; } set { _initialTemperature.RegionName = value; } }
        //
        [CategoryAttribute("Magnitude")]
        [OrderedDisplayName(0, 10, "Temperature")]
        [DescriptionAttribute("Value of the initial temperature.")]
        [TypeConverter(typeof(EquationTemperatureConverter))]
        [Id(1, 3)]
        public EquationString Temperature
        {
            get { return _initialTemperature.Temperature.Equation; }
            set { _initialTemperature.Temperature.Equation = value; }
        }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the initial condition.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _initialTemperature.DistributionName; }
            set { _initialTemperature.DistributionName = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _initialTemperature.Color; }
            set { _initialTemperature.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewInitialTemperature(InitialTemperature initialTemperature)
        {
            // The order is important
            _initialTemperature = initialTemperature;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            base.SetBase(_initialTemperature, regionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override InitialCondition GetBase()
        {
            return _initialTemperature;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames, string[] distributionNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateDistributionNames(distributionNames);
        }
        public void PopulateDistributionNames(string[] distributionNames)
        {
            List<string> names = new List<string>() { Load.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistributionName), names.ToArray(), false, 2);
        }
    }



   
}
