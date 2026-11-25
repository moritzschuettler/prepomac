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
    [PmxType(10547)]
    public class ViewEdgeLoad : ViewLoad
    {
        // Variables
        [PmxKey(0)] private EdgeLoad _edgeLoad;


        // Properties
        public override string Name { get { return _edgeLoad.Name; } set { _edgeLoad.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Node set")]
        [DescriptionAttribute("Select a node set created by picking geometry edges for the creation of the load.")]
        [Id(3, 2)]
        public string NodeSetName { get { return _edgeLoad.RegionName; } set { _edgeLoad.RegionName = value; } }
        //
        [CategoryAttribute("Force per length direction")]
        [OrderedDisplayName(0, 10, "Direction")]
        [DescriptionAttribute("Select the direction of the force per length: by its components, or normal to the edge " +
                              "in the plane of the shell or of the two dimensional part.")]
        [Id(1, 3)]
        public EdgeLoadDirectionEnum Direction
        {
            get { return _edgeLoad.Direction; }
            set
            {
                _edgeLoad.Direction = value;
                UpdateDirectionVisibility();
            }
        }
        //
        [CategoryAttribute("Force per length normal")]
        [OrderedDisplayName(0, 10, "Normal")]
        [DescriptionAttribute("Value of the force per length normal to the edge, in the plane of the shell or of the " +
                              "two dimensional part. A positive value points into the part, as a pressure does. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(1, 5)]
        public EquationString Normal { get { return _edgeLoad.Normal.Equation; } set { _edgeLoad.Normal.Equation = value; } }
        //
        [CategoryAttribute("Force per length components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Value of the force per length component in the direction of the first axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(1, 4)]
        public EquationString F1 { get { return _edgeLoad.F1.Equation; } set { _edgeLoad.F1.Equation = value; } }
        //
        [CategoryAttribute("Force per length components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Value of the force per length component in the direction of the second axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(2, 4)]
        public EquationString F2 { get { return _edgeLoad.F2.Equation; } set { _edgeLoad.F2.Equation = value; } }
        //
        [CategoryAttribute("Force per length components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Value of the force per length component in the direction of the third axis. With a " +
                              "distribution the value multiplies the distribution value.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(3, 4)]
        public EquationString F3 { get { return _edgeLoad.F3.Equation; } set { _edgeLoad.F3.Equation = value; } }
        //
        [CategoryAttribute("Force per length magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the edge load magnitude.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(1, 6)]
        public EquationString Magnitude
        {
            get { return _edgeLoad.Magnitude.Equation; }
            set { _edgeLoad.Magnitude.Equation = value; }
        }
        //
        [CategoryAttribute("Force phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the edge load phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 7)]
        public EquationString Phase { get { return _edgeLoad.PhaseDeg.Equation; } set { _edgeLoad.PhaseDeg.Equation = value; } }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the load. Its values are evaluated at the edge nodes.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _edgeLoad.DistributionName; }
            set { _edgeLoad.DistributionName = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _edgeLoad.AmplitudeName; }
            set { _edgeLoad.AmplitudeName = value; }
        }
        public override string CoordinateSystemName
        {
            get { return _edgeLoad.CoordinateSystemName; }
            set { _edgeLoad.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color { get { return _edgeLoad.Color; } set { _edgeLoad.Color = value; } }


        // Constructors
        public ViewEdgeLoad(EdgeLoad edgeLoad)
        {
            // The order is important
            _edgeLoad = edgeLoad;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            //
            SetBase(_edgeLoad, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Direction and 2D
            UpdateDirectionVisibility();
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(edgeLoad.Complex);
        }


        // Methods
        public override Load GetBase()
        {
            return _edgeLoad;
        }
        private void UpdateDirectionVisibility()
        {
            bool components = _edgeLoad.Direction == EdgeLoadDirectionEnum.Components;
            DynamicCustomTypeDescriptor.GetProperty(nameof(F1)).SetIsBrowsable(components);
            DynamicCustomTypeDescriptor.GetProperty(nameof(F2)).SetIsBrowsable(components);
            DynamicCustomTypeDescriptor.GetProperty(nameof(F3)).SetIsBrowsable(components && !_edgeLoad.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Magnitude)).SetIsBrowsable(components);
            DynamicCustomTypeDescriptor.GetProperty(nameof(CoordinateSystemName)).SetIsBrowsable(components);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Normal)).SetIsBrowsable(!components);
        }
        public void PopulateDropDownLists(string[] edgeNodeSetNames, string[] distributionNames, string[] amplitudeNames,
                                          string[] coordinateSystemNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, edgeNodeSetNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            List<string> names = new List<string>() { Distribution.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateOptionalProperty(nameof(DistributionName), names.ToArray(), false, 2);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            PopulateCoordinateSystemNames(coordinateSystemNames);
            UpdateDirectionVisibility();
        }
    }
}
