// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Drawing.Design;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewCyclicSymmetry : ViewTie
    {
        // Variables                                                                                                                
        private CyclicSymmetry _cyclicSymmetry;
        private ItemSetData _axisPointItemSetData;
        private string _affectedSelectionHidden;
        private Dictionary<RegionTypeEnum, string> _affectedRegionTypePropertyNamePairs;
        private Dictionary<string, RegionTypeEnum> _affectedRegionNameRegionTypePairs;


        // Properties                                                                                                               
        [CategoryAttribute("Cyclic symmetry")]
        [OrderedDisplayName(0, 10, "Number of sectors")]
        [DescriptionAttribute("Enter the number of identical sectors in the complete 360 degree model.")]
        [Id(1, 4)]
        public int NumberOfSectors
        {
            get { return _cyclicSymmetry.NumberOfSectors; }
            set
            {
                _cyclicSymmetry.NumberOfSectors = value;
                UpdateRegionVisibility();
            }
        }
        //
        [CategoryAttribute("Cyclic symmetry")]
        [OrderedDisplayName(1, 10, "Displayed sectors")]
        [DescriptionAttribute("Enter the number of sectors CalculiX generates for result visualization.")]
        [Id(2, 4)]
        public int NumberOfGraphSectors
        {
            get { return _cyclicSymmetry.NumberOfGraphSectors; }
            set
            {
                _cyclicSymmetry.NumberOfGraphSectors = value;
                UpdateRegionVisibility();
            }
        }
        //
        [CategoryAttribute("Cyclic symmetry")]
        [OrderedDisplayName(2, 10, "Check sector angle")]
        [DescriptionAttribute("Let CalculiX check that the angle between the cyclic surfaces agrees with the number of sectors.")]
        [Id(3, 4)]
        public bool CheckSectorAngle
        {
            get { return _cyclicSymmetry.CheckSectorAngle; }
            set { _cyclicSymmetry.CheckSectorAngle = value; }
        }
        //
        [CategoryAttribute("Result expansion region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the element region whose results CalculiX expands to the displayed cyclic sectors.")]
        [Id(1, 5)]
        public string AffectedRegionType
        {
            get
            {
                if (_affectedRegionTypePropertyNamePairs.ContainsKey(_cyclicSymmetry.RegionType))
                    return _cyclicSymmetry.RegionType.ToFriendlyString();
                else throw new NotSupportedException();
            }
            set
            {
                _cyclicSymmetry.RegionType = _affectedRegionNameRegionTypePairs[value];
                CustomPropertyDescriptor cpd =
                    DynamicCustomTypeDescriptor.GetProperty(
                        _affectedRegionTypePropertyNamePairs[_cyclicSymmetry.RegionType]);
                _cyclicSymmetry.RegionName = cpd.StatandardValues.First().ToString();
                UpdateRegionVisibility();
            }
        }
        //
        [Browsable(false)]
        [CategoryAttribute("Result expansion region")]
        [OrderedDisplayName(1, 10, "Hidden")]
        [DescriptionAttribute("Hidden.")]
        [Id(2, 5)]
        public string AffectedSelectionHidden
        {
            get { return _affectedSelectionHidden; }
            set { _affectedSelectionHidden = value; }
        }
        //
        [CategoryAttribute("Result expansion region")]
        [OrderedDisplayName(2, 10, "Part")]
        [DescriptionAttribute("Select the part whose results CalculiX expands to the displayed cyclic sectors.")]
        [Id(3, 5)]
        public string PartName
        {
            get { return _cyclicSymmetry.RegionName; }
            set { _cyclicSymmetry.RegionName = value; }
        }
        //
        [CategoryAttribute("Result expansion region")]
        [OrderedDisplayName(3, 10, "Element set")]
        [DescriptionAttribute("Select the element set whose results CalculiX expands to the displayed cyclic sectors.")]
        [Id(4, 5)]
        public string ElementSetName
        {
            get { return _cyclicSymmetry.RegionName; }
            set { _cyclicSymmetry.RegionName = value; }
        }
        //
        [CategoryAttribute("Axis point coordinates")]
        [OrderedDisplayName(0, 10, "By selection")]
        [DescriptionAttribute("Select a node defining a point on the cyclic symmetry axis.")]
        [EditorAttribute(typeof(SinglePointDataEditor), typeof(UITypeEditor))]
        [Id(1, 6)]
        public ItemSetData AxisPointItemSet
        {
            get { return _axisPointItemSetData; }
            set
            {
                if (value != _axisPointItemSetData) _axisPointItemSetData = value;
            }
        }
        //
        [CategoryAttribute("Axis point coordinates")]
        [OrderedDisplayName(1, 10, "X")]
        [DescriptionAttribute("X coordinate of a point on the cyclic symmetry axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 6)]
        public EquationString X
        {
            get { return _cyclicSymmetry.X.Equation; }
            set { _cyclicSymmetry.X.Equation = value; }
        }
        //
        [CategoryAttribute("Axis point coordinates")]
        [OrderedDisplayName(2, 10, "Y")]
        [DescriptionAttribute("Y coordinate of a point on the cyclic symmetry axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 6)]
        public EquationString Y
        {
            get { return _cyclicSymmetry.Y.Equation; }
            set { _cyclicSymmetry.Y.Equation = value; }
        }
        //
        [CategoryAttribute("Axis point coordinates")]
        [OrderedDisplayName(3, 10, "Z")]
        [DescriptionAttribute("Z coordinate of a point on the cyclic symmetry axis.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 6)]
        public EquationString Z
        {
            get { return _cyclicSymmetry.Z.Equation; }
            set { _cyclicSymmetry.Z.Equation = value; }
        }
        //
        [CategoryAttribute("Axis direction components")]
        [OrderedDisplayName(0, 10, "N1")]
        [DescriptionAttribute("First component of the positive cyclic symmetry axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(1, 7)]
        public EquationString N1
        {
            get { return _cyclicSymmetry.N1.Equation; }
            set { _cyclicSymmetry.N1.Equation = value; }
        }
        //
        [CategoryAttribute("Axis direction components")]
        [OrderedDisplayName(1, 10, "N2")]
        [DescriptionAttribute("Second component of the positive cyclic symmetry axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(2, 7)]
        public EquationString N2
        {
            get { return _cyclicSymmetry.N2.Equation; }
            set { _cyclicSymmetry.N2.Equation = value; }
        }
        //
        [CategoryAttribute("Axis direction components")]
        [OrderedDisplayName(2, 10, "N3")]
        [DescriptionAttribute("Third component of the positive cyclic symmetry axis direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(3, 7)]
        public EquationString N3
        {
            get { return _cyclicSymmetry.N3.Equation; }
            set { _cyclicSymmetry.N3.Equation = value; }
        }


        // Constructors                                                                                                             
        public ViewCyclicSymmetry(CyclicSymmetry cyclicSymmetry)
            : base(cyclicSymmetry, false)
        {
            _cyclicSymmetry = cyclicSymmetry;
            //
            _affectedRegionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            _affectedRegionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(AffectedSelectionHidden));
            _affectedRegionTypePropertyNamePairs.Add(RegionTypeEnum.PartName, nameof(PartName));
            _affectedRegionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            _affectedRegionNameRegionTypePairs = new Dictionary<string, RegionTypeEnum>();
            foreach (var entry in _affectedRegionTypePropertyNamePairs)
                _affectedRegionNameRegionTypePairs.Add(entry.Key.ToFriendlyString(), entry.Key);
            //
            _axisPointItemSetData = new ItemSetData(); // needed to display ItemSetData.ToString()
            _axisPointItemSetData.ToStringType = ItemSetDataToStringType.SelectSinglePoint;
            //
            InstallProvider();
            //
            DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(CheckSectorAngle));
            UpdateRegionVisibility();
        }


        // Methods                                                                                                                  
        public void PopulateDropDownLists(string[] surfaceNames, string[] partNames, string[] elementSetNames)
        {
            base.PopulateDropDownLists(surfaceNames);
            //
            Dictionary<RegionTypeEnum, string[]> affectedRegionTypeListItemsPairs =
                new Dictionary<RegionTypeEnum, string[]>();
            affectedRegionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            affectedRegionTypeListItemsPairs.Add(RegionTypeEnum.PartName, partNames);
            affectedRegionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, elementSetNames);
            foreach (var entry in affectedRegionTypeListItemsPairs)
                DynamicCustomTypeDescriptor.PopulateProperty(
                    _affectedRegionTypePropertyNamePairs[entry.Key], entry.Value);
            //
            List<string> types = new List<string>();
            foreach (var entry in affectedRegionTypeListItemsPairs)
            {
                if (entry.Value.Length > 0) types.Add(entry.Key.ToFriendlyString());
            }
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(AffectedRegionType), types.ToArray(), false, 1);
            UpdateRegionVisibility();
        }
        public override void UpdateRegionVisibility()
        {
            base.UpdateRegionVisibility();
            //
            bool expansionRegionVisible = _cyclicSymmetry.NumberOfGraphSectors > 1;
            DynamicCustomTypeDescriptor.GetProperty(nameof(AffectedRegionType))
                                       .SetIsBrowsable(expansionRegionVisible);
            foreach (var entry in _affectedRegionTypePropertyNamePairs)
            {
                bool browsable = expansionRegionVisible && _cyclicSymmetry.RegionType == entry.Key;
                DynamicCustomTypeDescriptor.GetProperty(entry.Value).SetIsBrowsable(browsable);
            }
            DynamicCustomTypeDescriptor.GetProperty(nameof(AffectedSelectionHidden)).SetIsBrowsable(false);
        }
        public override Constraint GetBase()
        {
            return _cyclicSymmetry;
        }
    }
}

