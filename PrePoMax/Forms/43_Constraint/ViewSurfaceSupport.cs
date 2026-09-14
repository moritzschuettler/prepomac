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
    public enum SurfaceSupportTypeEnum
    {
        [StandardValue("Spring", "Spring", Description =
                       "The surface is supported by springs of the given stiffness.")]
        Spring,
        [StandardValue("Compression only", "Compression only", Description =
                       "The surface is supported only while it is pressed against the support and is free to lift off.")]
        CompressionOnly
    }

    [Serializable]
    public class ViewSurfaceSupport : ViewConstraint
    {
        // Variables
        private SurfaceSpring _surfaceSpring;
        private CompressionOnly _compressionOnly;
        private Constraint _constraint;


        // Properties
        public override string Name { get { return _constraint.Name; } set { _constraint.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Support type")]
        [DescriptionAttribute("Select the surface support type.")]
        [Id(2, 1)]
        public SurfaceSupportTypeEnum SupportType
        {
            get
            {
                if (_constraint == _surfaceSpring) return SurfaceSupportTypeEnum.Spring;
                else return SurfaceSupportTypeEnum.CompressionOnly;
            }
            set { SetSupportType(value); }
        }
        // REGION ------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Region")]
        [OrderedDisplayName(0, 10, "Region type")]
        [DescriptionAttribute("Select the region type for the creation of the constraint definition.")]
        [Id(1, 2)]
        public override string MasterRegionType { get { return base.MasterRegionType; } set { base.MasterRegionType = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(1, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the constraint definition.")]
        [Id(2, 2)]
        public string SurfaceName
        {
            get { return _constraint.MasterRegionName; }
            set { _constraint.MasterRegionName = value; }
        }
        // SPRING ------------------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Stiffness definition")]
        [OrderedDisplayName(0, 10, "Stiffness per area")]
        [DescriptionAttribute("Select yes if stiffness is defined per area of the selected surface.")]
        [Id(1, 3)]
        public bool StiffnessPerArea
        {
            get { return _surfaceSpring.StiffnessPerArea; }
            set
            {
                _surfaceSpring.StiffnessPerArea = value;
                UpdateSurfaceSupportVisibility();
            }
        }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(0, 10, "K1")]
        [DescriptionAttribute("Value of the stiffness in the direction of the first axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(1, 4)]
        public EquationString K1 { get { return _surfaceSpring.K1.Equation; } set { _surfaceSpring.K1.Equation = value; } }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(1, 10, "K2")]
        [DescriptionAttribute("Value of the stiffness in the direction of the second axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(2, 4)]
        public EquationString K2 { get { return _surfaceSpring.K2.Equation; } set { _surfaceSpring.K2.Equation = value; } }
        //
        [CategoryAttribute("Stiffness")]
        [OrderedDisplayName(2, 10, "K3")]
        [DescriptionAttribute("Value of the stiffness in the direction of the third axis.")]
        [TypeConverter(typeof(EquationForcePerLengthConverter))]
        [Id(3, 4)]
        public EquationString K3 { get { return _surfaceSpring.K3.Equation; } set { _surfaceSpring.K3.Equation = value; } }
        //
        [CategoryAttribute("Stiffness per area")]
        [OrderedDisplayName(0, 10, "K1")]
        [DescriptionAttribute("Value of the stiffness per area in the direction of the first axis.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(1, 4)]
        public EquationString K1A { get { return _surfaceSpring.K1.Equation; } set { _surfaceSpring.K1.Equation = value; } }
        //
        [CategoryAttribute("Stiffness per area")]
        [OrderedDisplayName(1, 10, "K2")]
        [DescriptionAttribute("Value of the stiffness per area in the direction of the second axis.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(2, 4)]
        public EquationString K2A { get { return _surfaceSpring.K2.Equation; } set { _surfaceSpring.K2.Equation = value; } }
        //
        [CategoryAttribute("Stiffness per area")]
        [OrderedDisplayName(2, 10, "K3")]
        [DescriptionAttribute("Value of the stiffness per area in the direction of the third axis.")]
        [TypeConverter(typeof(EquationForcePerVolumeConverter))]
        [Id(3, 4)]
        public EquationString K3A { get { return _surfaceSpring.K3.Equation; } set { _surfaceSpring.K3.Equation = value; } }
        // COMPRESSION ONLY --------------------------------------------------------------------------------------------------------
        [CategoryAttribute("Gap")]
        [OrderedDisplayName(0, 10, "Gap clearance")]
        [DescriptionAttribute("Value of the gap clearance.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(1, 5)]
        public EquationString Clearance
        {
            get { return _compressionOnly.Clearance.Equation; }
            set { _compressionOnly.Clearance.Equation = value; }
        }
        //
        [CategoryAttribute("Gap")]
        [OrderedDisplayName(1, 10, "Spring stiffness")]
        [DescriptionAttribute("Value of the spring stiffness for the gap property definition.")]
        [TypeConverter(typeof(EquationForcePerLengthDefaultConverter))]
        [Id(2, 5)]
        public EquationString SpringStiffness
        {
            get { return _compressionOnly.SpringStiffness.Equation; }
            set { _compressionOnly.SpringStiffness.Equation = value; }
        }
        //
        [CategoryAttribute("Gap")]
        [OrderedDisplayName(2, 10, "Tensile force")]
        [DescriptionAttribute("Value of the tensile force for the gap property definition.")]
        [TypeConverter(typeof(EquationForceDefaultConverter))]
        [Id(3, 5)]
        public EquationString TensileForceAtNegativeInfinity
        {
            get { return _compressionOnly.TensileForceAtNegativeInfinity.Equation; }
            set { _compressionOnly.TensileForceAtNegativeInfinity.Equation = value; }
        }
        //
        [CategoryAttribute("Gap")]
        [OrderedDisplayName(3, 10, "Offset")]
        [DescriptionAttribute("The value for which one gap element node will be offset from the selected region.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(4, 5)]
        public EquationString Offset
        {
            get { return _compressionOnly.Offset.Equation; }
            set { _compressionOnly.Offset.Equation = value; }
        }
        //
        [CategoryAttribute("Solution")]
        [OrderedDisplayName(0, 10, "Nonlinear")]
        [DescriptionAttribute("Compression only support is linearized if a linear solution procedure is used.")]
        [Id(1, 6)]
        public bool NonLinear { get { return _compressionOnly.NonLinear; } set { _compressionOnly.NonLinear = value; } }
        // -------------------------------------------------------------------------------------------------------------------------
        [Category("Appearance")]
        [DisplayName("Color")]
        [Description("Select the constraint color.")]
        [Editor(typeof(UserControls.ColorEditorEx), typeof(UITypeEditor))]
        [Id(1, 10)]
        public Color Color { get { return _constraint.MasterColor; } set { _constraint.MasterColor = value; } }


        // Constructors
        public ViewSurfaceSupport(Constraint constraint)
        {
            // The order is important
            if (constraint is SurfaceSpring surfaceSpring)
            {
                _surfaceSpring = surfaceSpring;
                _compressionOnly = new CompressionOnly(surfaceSpring.Name, surfaceSpring.RegionName,
                                                       surfaceSpring.RegionType, surfaceSpring.TwoD);
                CopySurfaceSupportData(surfaceSpring, _compressionOnly);
            }
            else if (constraint is CompressionOnly compressionOnly)
            {
                _compressionOnly = compressionOnly;
                _surfaceSpring = new SurfaceSpring(compressionOnly.Name, compressionOnly.RegionName,
                                                   compressionOnly.RegionType, compressionOnly.TwoD, true);
                CopySurfaceSupportData(compressionOnly, _surfaceSpring);
            }
            else throw new NotSupportedException();
            //
            _constraint = constraint;
            //
            SetSurfaceSupportBase();
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            SetGapInitialValues();
            // Now lets display Yes/No instead of True/False
            base.DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(StiffnessPerArea));
            base.DynamicCustomTypeDescriptor.RenameBooleanPropertyToYesNo(nameof(NonLinear));
            //
            UpdateSurfaceSupportVisibility();
        }


        // Methods
        public override Constraint GetBase()
        {
            return _constraint;
        }
        public void PopulateDropDownLists(string[] surfaceNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            //
            base.PopulateDropDownLists(regionTypeListItemsPairs, null);
        }
        private void SetSurfaceSupportBase()
        {
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(MasterSelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            base.SetBase(_constraint, regionTypePropertyNamePairs, null);
        }
        private void SetSupportType(SurfaceSupportTypeEnum supportType)
        {
            if (supportType == SupportType) return;
            //
            Constraint previousConstraint = _constraint;
            if (supportType == SurfaceSupportTypeEnum.Spring) _constraint = _surfaceSpring;
            else _constraint = _compressionOnly;
            //
            CopySurfaceSupportData(previousConstraint, _constraint);
            //
            SetSurfaceSupportBase();
            //
            if (_constraint == _compressionOnly) SetGapInitialValues();
            //
            UpdateSurfaceSupportVisibility();
        }
        private void CopySurfaceSupportData(Constraint source, Constraint target)
        {
            target.CopyFrom(source);
            //
            target.MasterRegionName = source.MasterRegionName;
            target.MasterRegionType = source.MasterRegionType;
            target.MasterCreationIds = source.MasterCreationIds;
            target.MasterCreationData = source.MasterCreationData;
            target.MasterColor = source.MasterColor;
            target.SlaveColor = source.SlaveColor;
        }
        private void SetGapInitialValues()
        {
            StringForcePerLengthDefaultConverter.SetInitialValue = GapSectionData.InitialSpringStiffness.ToString();
            StringForceDefaultConverter.SetInitialValue = GapSectionData.InitialTensileForceAtNegativeInfinity.ToString();
        }
        private void UpdateSurfaceSupportVisibility()
        {
            if (DynamicCustomTypeDescriptor == null) return;
            //
            bool spring = _constraint == _surfaceSpring;
            bool twoD = _constraint.TwoD;
            bool stiffnessPerArea = _surfaceSpring.StiffnessPerArea;
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(StiffnessPerArea)).SetIsBrowsable(spring);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(K1)).SetIsBrowsable(spring && !stiffnessPerArea);
            DynamicCustomTypeDescriptor.GetProperty(nameof(K2)).SetIsBrowsable(spring && !stiffnessPerArea);
            DynamicCustomTypeDescriptor.GetProperty(nameof(K3)).SetIsBrowsable(spring && !stiffnessPerArea && !twoD);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(K1A)).SetIsBrowsable(spring && stiffnessPerArea);
            DynamicCustomTypeDescriptor.GetProperty(nameof(K2A)).SetIsBrowsable(spring && stiffnessPerArea);
            DynamicCustomTypeDescriptor.GetProperty(nameof(K3A)).SetIsBrowsable(spring && stiffnessPerArea && !twoD);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(Clearance)).SetIsBrowsable(!spring);
            DynamicCustomTypeDescriptor.GetProperty(nameof(SpringStiffness)).SetIsBrowsable(!spring);
            DynamicCustomTypeDescriptor.GetProperty(nameof(TensileForceAtNegativeInfinity)).SetIsBrowsable(!spring);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Offset)).SetIsBrowsable(!spring);
            DynamicCustomTypeDescriptor.GetProperty(nameof(NonLinear)).SetIsBrowsable(!spring);
        }
    }
}
