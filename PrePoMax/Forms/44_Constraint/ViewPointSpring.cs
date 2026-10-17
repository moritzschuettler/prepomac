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
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;


namespace PrePoMax
{
    [Serializable]
    public class ViewPointSpring : ViewConstraint
    {
        // Variables
        private PointSpring _pointSpring;


        // Properties
        public override string Name { get { return _pointSpring.Name; } set { _pointSpring.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the constraint definition.")]
        [Id(1, 2)]
        public override string MasterRegionType { get { return base.MasterRegionType; } set { base.MasterRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Node set")]
        [DescriptionAttribute("Select the node set for the creation of the constraint definition.")]
        [Id(2, 2)]
        public string NodeSetName { get { return _pointSpring.RegionName; } set { _pointSpring.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Reference point")]
        [DescriptionAttribute("Select the reference point for the creation of the constraint definition.")]
        [Id(3, 2)]
        public string ReferencePointName { get { return _pointSpring.RegionName; } set { _pointSpring.RegionName = value; } }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(0, 10, "K1")]
        [DescriptionAttribute("Value of the stiffness per node in the direction of the first axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(1, 3)]
        public EquationString K1 { get { return _pointSpring.K1.Equation; } set { _pointSpring.K1.Equation = value; } }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(1, 10, "K2")]
        [DescriptionAttribute("Value of the stiffness per node in the direction of the second axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(2, 3)]
        public EquationString K2 { get { return _pointSpring.K2.Equation; } set { _pointSpring.K2.Equation = value; } }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(2, 10, "K3")]
        [DescriptionAttribute("Value of the stiffness per node in the direction of the third axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(3, 3)]
        public EquationString K3 { get { return _pointSpring.K3.Equation; } set { _pointSpring.K3.Equation = value; } }
        //
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select the constraint color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color Color { get { return _pointSpring.MasterColor; } set { _pointSpring.MasterColor = value; } }


        // Constructors
        public ViewPointSpring(PointSpring pointSpring)
        {
            // The order is important
            _pointSpring = pointSpring;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(MasterSelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.NodeSetName, nameof(NodeSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ReferencePointName, nameof(ReferencePointName));
            //
            SetBase(_pointSpring, regionTypePropertyNamePairs, null);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // 2D
            DynamicCustomTypeDescriptor.GetProperty(nameof(K3)).SetIsBrowsable(!_pointSpring.TwoD);
        }


        // Methods
        public override Constraint GetBase()
        {
            return _pointSpring;
        }
        public void PopulateDropDownLists(string[] nodeSetNames, string[] referencePointNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.NodeSetName, nodeSetNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ReferencePointName, referencePointNames);
            //
            PopulateDropDownLists(regionTypeListItemsPairs, null);
        }
    }
}
