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
using CaeGlobals;
using CaeMesh;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public class ViewMaterialOrientation : ViewMultiRegion
    {
        // Variables
        private string _selectionHidden;
        private MaterialOrientation _materialOrientation;


        // Properties
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Name")]
        [Id(1, 1)]
        [DescriptionAttribute("Name of the material orientation.")]
        public string Name { get { return _materialOrientation.Name; } set { _materialOrientation.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [Id(1, 2)]
        [DescriptionAttribute("Select the region type of the shell elements the orientation applies to.")]
        public override string RegionType { get { return base.RegionType; } set { base.RegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [Id(2, 2)]
        [DescriptionAttribute("Hidden.")]
        public string SelectionHidden { get { return _selectionHidden; } set { _selectionHidden = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Part")]
        [Id(3, 2)]
        [DescriptionAttribute("Select the part the orientation applies to.")]
        public string PartName
        {
            get { return _materialOrientation.RegionName; }
            set { _materialOrientation.RegionName = value; }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Element set")]
        [Id(4, 2)]
        [DescriptionAttribute("Select the element set the orientation applies to.")]
        public string ElementSetName
        {
            get { return _materialOrientation.RegionName; }
            set { _materialOrientation.RegionName = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(0, 10, "Coordinate system")]
        [Id(1, 3)]
        [DescriptionAttribute("Select the coordinate system whose axis defines the major material direction.")]
        public string CoordinateSystemName
        {
            get { return _materialOrientation.CoordinateSystemName; }
            set { _materialOrientation.CoordinateSystemName = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(1, 10, "Reference axis")]
        [Id(2, 3)]
        [DescriptionAttribute("Select the axis of the coordinate system that is projected onto each shell element to " +
                              "become the material axis 1. An axis closer than 0.1 degrees to the shell normal is " +
                              "replaced by the z axis, or by the x axis when it is the z axis.")]
        public MaterialOrientationAxisEnum ReferenceAxis
        {
            get { return _materialOrientation.ReferenceAxis; }
            set { _materialOrientation.ReferenceAxis = value; }
        }
        //
        [CategoryAttribute("Orientation")]
        [OrderedDisplayName(2, 10, "Rotation angle")]
        [Id(3, 3)]
        [DescriptionAttribute("The angle that turns the projected axis about the shell normal.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        public EquationString RotationAngle
        {
            get { return _materialOrientation.RotationAngle.Equation; }
            set { _materialOrientation.RotationAngle.Equation = value; }
        }
        //
        [CategoryAttribute("Appearance")]
        [OrderedDisplayName(0, 10, "Symbol density")]
        [Id(1, 4)]
        [DescriptionAttribute("Set the density of the spatially distributed material axes symbols as a percentage " +
                              "of the full density.")]
        [TypeConverter(typeof(StringIntegerPercentConverter))]
        public int SymbolDensity
        {
            get { return _materialOrientation.SymbolDensity; }
            set { _materialOrientation.SymbolDensity = value; }
        }


        // Constructors
        public ViewMaterialOrientation(MaterialOrientation materialOrientation)
        {
            _materialOrientation = materialOrientation;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(PartName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            //
            base.SetBase(_materialOrientation, regionTypePropertyNamePairs);
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            base.DynamicCustomTypeDescriptor.CategorySortOrder = CustomSortOrder.AscendingById;
        }


        // Methods
        public MaterialOrientation GetBase()
        {
            return _materialOrientation;
        }
        public void PopulateDropDownLists(string[] partNames, string[] elementSetNames, string[] coordinateSystemNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            //
            List<string> names = new List<string>() { CoordinateSystem.DefaultCoordinateSystemName };
            names.AddRange(coordinateSystemNames);
            DynamicCustomTypeDescriptor.PopulateOptionalProperty(nameof(CoordinateSystemName), names.ToArray(), false, 2);
        }
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            //
            if (base.RegionType == RegionTypeEnum.Selection.ToFriendlyString())
                DynamicCustomTypeDescriptor.GetProperty(nameof(SelectionHidden)).SetIsBrowsable(false);
        }
    }
}
