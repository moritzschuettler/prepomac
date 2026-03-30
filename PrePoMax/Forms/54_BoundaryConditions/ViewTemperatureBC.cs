// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.Drawing;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewTemperatureBC : ViewBoundaryCondition
    {
        // Variables                                                                                                                
        private CaeModel.TemperatureBC _temperatureBC;


        // Properties                                                                                                               
        public override string Name { get { return _temperatureBC.Name; } set { _temperatureBC.Name = value; } }
        public override string NodeSetName { get { return _temperatureBC.RegionName; } set { _temperatureBC.RegionName = value; } }
        public override string ReferencePointName
        {
            get { return _temperatureBC.RegionName; }
            set { _temperatureBC.RegionName = value; }
        }
        public override string SurfaceName { get { return _temperatureBC.RegionName; } set { _temperatureBC.RegionName = value; } }
        //
        [CategoryAttribute("Temperature magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the temperature.")]
        [TypeConverter(typeof(EquationTemperatureConverter))]
        [Id(1, 3)]
        public EquationString Temperature
        {
            get { return _temperatureBC.Temperature.Equation; }
            set { _temperatureBC.Temperature.Equation = value; }
        }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the initial condition.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _temperatureBC.DistributionName; }
            set { _temperatureBC.DistributionName = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _temperatureBC.AmplitudeName; }
            set { _temperatureBC.AmplitudeName = value; }
        }
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _temperatureBC.CoordinateSystemName; }
            set { _temperatureBC.CoordinateSystemName = value; }
        }

        public override Color Color { get { return _temperatureBC.Color; } set { _temperatureBC.Color = value; } }


        // Constructors                                                                                                             
        public ViewTemperatureBC(TemperatureBC temperatureBC)
        {
            // The order is important
            _temperatureBC = temperatureBC;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            // Must be here to correctly hide the RPs defined in base class
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            SetBase(_temperatureBC, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override BoundaryCondition GetBase()
        {
            return _temperatureBC;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] surfaceNames, string[] distributionNames,
                                          string[] amplitudeNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateDistributionNames(distributionNames);
            //
            PopulateAmplitudeNames(amplitudeNames);
        }
        public void PopulateDistributionNames(string[] distributionNames)
        {
            List<string> names = new List<string>() { Distribution.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistributionName), names.ToArray(), false, 2);
        }
    }

}
