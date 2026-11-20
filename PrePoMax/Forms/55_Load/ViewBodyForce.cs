// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [PmxType(10546)]
    public class ViewBodyForce : ViewLoad
    {
        // Variables
        [PmxKey(0)] private BodyForce _bForce;


        // Properties
        public override string Name { get { return _bForce.Name; } set { _bForce.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Part")]
        [DescriptionAttribute("Select the part for the creation of the load.")]
        [Id(3, 2)]
        public string PartName { get { return _bForce.RegionName; } set { _bForce.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Element set")]
        [DescriptionAttribute("Select the element set for the creation of the load.")]
        [Id(4, 2)]
        public string ElementSetName { get { return _bForce.RegionName; } set { _bForce.RegionName = value; } }
        //
        [CategoryAttribute("Force per volume components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Value of the force per volume component in the direction of the first axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(1, 3)]
        public EquationString F1 { get { return _bForce.F1.Equation; } set { _bForce.F1.Equation = value; } }
        //
        [CategoryAttribute("Force per volume components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Value of the force per volume component in the direction of the second axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(2, 3)]
        public EquationString F2 { get { return _bForce.F2.Equation; } set { _bForce.F2.Equation = value; } }
        //
        [CategoryAttribute("Force per volume components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Value of the force per volume component in the direction of the third axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(3, 3)]
        public EquationString F3 { get { return _bForce.F3.Equation; } set { _bForce.F3.Equation = value; } }
        //
        [CategoryAttribute("Force per volume magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the body force magnitude.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(1, 4)]
        public EquationString Magnitude { get { return _bForce.Magnitude.Equation; } set { _bForce.Magnitude.Equation = value; } }
        //
        [CategoryAttribute("Force phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the body force phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 5)]
        public EquationString Phase { get { return _bForce.PhaseDeg.Equation; } set { _bForce.PhaseDeg.Equation = value; } }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the load. Its values are evaluated at the element centroids.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _bForce.DistributionName; }
            set { _bForce.DistributionName = value; }
        }
        //
        public override string AmplitudeName { get { return _bForce.AmplitudeName; } set { _bForce.AmplitudeName = value; } }
        public override string CoordinateSystemName
        {
            get { return _bForce.CoordinateSystemName; }
            set { _bForce.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color { get { return _bForce.Color; } set { _bForce.Color = value; } }


        // Constructor
        public ViewBodyForce(BodyForce bForce)
        {
            // The order is important
            _bForce = bForce;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(PartName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            //
            SetBase(_bForce, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // 2D
            DynamicCustomTypeDescriptor.GetProperty(nameof(F3)).SetIsBrowsable(!bForce.TwoD);
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(bForce.Complex);
        }


        // Methods
        public override Load GetBase()
        {
            return _bForce;
        }
        public void PopulateDropDownLists(string[] partNames, string[] elementSetNames, string[] distributionNames,
                                          string[] amplitudeNames, string[] coordinateSystemNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            List<string> names = new List<string>() { Distribution.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateOptionalProperty(nameof(DistributionName), names.ToArray(), false, 2);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            PopulateCoordinateSystemNames(coordinateSystemNames);
        }
    }
}
