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
using CaeModel;
using System.Drawing;

namespace PrePoMax
{
    [Serializable]
    public class ViewDefinedTemperature : ViewDefinedField
    {
        // Variables                                                                                                                
        private DefinedTemperature _definedTemperature;


        // Properties                                                                                                               
        public override string Name { get { return _definedTemperature.Name; } set { _definedTemperature.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Define temperature")]
        [DescriptionAttribute("Define the temperature by a constant value or read the temperature from a file.")]
        [Id(2, 1)]
        public DefinedTemperatureTypeEnum DefinedTemperatureType
        {
            get { return _definedTemperature.Type; }
            set { _definedTemperature.Type = value; UpdateVisibilities(); }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the creation of the defined temperature.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _definedTemperature.RegionName; } set { _definedTemperature.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(4, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the defined temperature.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _definedTemperature.RegionName; } set { _definedTemperature.RegionName = value; } }
        //
        [CategoryAttribute("Magnitude")]
        [OrderedDisplayName(0, 10, "Temperature")]
        [DescriptionAttribute("Value of the defined temperature.")]
        [TypeConverter(typeof(EquationTemperatureConverter))]
        [Id(1, 3)]
        public EquationString Temperature
        {
            get { return _definedTemperature.Temperature.Equation; }
            set { _definedTemperature.Temperature.Equation = value; }
        }
        //
        [CategoryAttribute("Magnitude")]
        [OrderedDisplayName(1, 10, "Results file .frd")]
        [DescriptionAttribute("Results file name (.frd) without path.")]
        [EditorAttribute(typeof(FilteredFileNameEditor), typeof(System.Drawing.Design.UITypeEditor))]
        [Id(2, 3)]
        public string FileName
        {
            get { return _definedTemperature.FileName; }
            set
            {
                value = System.IO.Path.GetFileName(value);
                //
                if (value.Contains(" ") || value.Contains(":") || value.Contains("\\") || value.Contains("/")
                    || value.ToUTF8() != value)
                    throw new Exception("Enter the results file name (.frd) without path. " +
                                        "The results file name must not contain any special characters.");
                _definedTemperature.FileName = value;
            }
        }
        //
        [CategoryAttribute("Magnitude")]
        [OrderedDisplayName(2, 10, "Step number")]
        [DescriptionAttribute("Enter the results step number from which to read the temperatures.")]
        [Id(3, 3)]
        public int StepNumber
        {
            get { return _definedTemperature.StepNumber; }
            set { _definedTemperature.StepNumber = value; }
        }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the initial condition.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _definedTemperature.DistributionName; }
            set { _definedTemperature.DistributionName = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _definedTemperature.AmplitudeName; }
            set { _definedTemperature.AmplitudeName = value; }
        }
        public override Color Color { get { return _definedTemperature.Color; } set { _definedTemperature.Color = value; } }


        // Constructors                                                                                                             
        public ViewDefinedTemperature(DefinedTemperature definedTemperature)
        {
            // The order is important
            _definedTemperature = definedTemperature;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_definedTemperature, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            FilteredFileNameEditor.Filter = "Calculix result files|*.frd";
        }


        // Methods                                                                                                                  
        public override DefinedField GetBase()
        {
            return _definedTemperature;
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
            //
            UpdateVisibilities();
        }
        public void PopulateDistributionNames(string[] distributionNames)
        {
            List<string> names = new List<string>() { Distribution.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistributionName), names.ToArray(), false, 2);
        }
        private void UpdateVisibilities()
        {
            bool byValue = _definedTemperature.Type == DefinedTemperatureTypeEnum.ByValue;
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(Temperature)).SetIsBrowsable(byValue);
            DynamicCustomTypeDescriptor.GetProperty(nameof(FileName)).SetIsBrowsable(!byValue);
            DynamicCustomTypeDescriptor.GetProperty(nameof(StepNumber)).SetIsBrowsable(!byValue);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(RegionType)).SetIsBrowsable(byValue);
            if (byValue)
            {
                RegionType = RegionType;
            }
            else
            {
                DynamicCustomTypeDescriptor.GetProperty(nameof(NodeSetName)).SetIsBrowsable(byValue);
                DynamicCustomTypeDescriptor.GetProperty(nameof(SurfaceName)).SetIsBrowsable(byValue);
            }
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(DistributionName)).SetIsBrowsable(byValue);
            DynamicCustomTypeDescriptor.GetProperty(nameof(AmplitudeName)).SetIsBrowsable(byValue);
        }
    }
}
