// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeResults;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    public class ViewResultHistoryOutputFromIntegration : ViewResultHistoryOutput
    {
        // Variables                                                                                                                
        private ResultHistoryOutputFromIntegration _historyOutput;
        private string[] _elementSetNames;
        private string[] _surfaceNames;
        private MultiChoiceContainer _componentContainer;
        private Dictionary<string, string[]> _filedNameComponentNames;
        private IntegrationScaleType _defaultCrossSection;


        // Properties                                                                                                               
        public override string Name { get { return _historyOutput.Name; } set { _historyOutput.Name = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "Integal type")]
        [DescriptionAttribute("Select the integral type.")]
        [Id(2, 1)]
        public IntegralTypeEnum IntegralType
        {
            get { return _historyOutput.IntegralType; }
            set
            {
                IntegralTypeEnum previousIntegralType = _historyOutput.IntegralType;
                _historyOutput.IntegralType = value;
                if (value == IntegralTypeEnum.Surface) _historyOutput.CrossSection = IntegrationScaleType.None;
                else if (previousIntegralType == IntegralTypeEnum.Surface) _historyOutput.CrossSection = _defaultCrossSection;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "Integral result")]
        [DescriptionAttribute("Select what the integral evaluates: Geometry (the region size), " +
                              "Field (the field integral) or Field by Geometry (the region average of the field).")]
        [Id(3, 1)]
        public IntegralResultEnum IntegralResult
        {
            get { return _historyOutput.IntegralResult; }
            set { _historyOutput.IntegralResult = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(3, 10, "User defined unit")]
        [DescriptionAttribute("User defined unit for the history output.")]
        [Id(4, 1)]
        public string Unit { get { return _historyOutput.Unit; } set { _historyOutput.Unit = value; } }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(4, 10, "Cross section")]
        [DescriptionAttribute("Out-of-plane scaling for a 2D region: None (plain integral), Thickness " +
                              "(plane stress / plane strain) or Axisymmetric (2*pi*r revolution).")]
        [Id(5, 1)]
        public IntegrationScaleType CrossSection
        {
            get { return _historyOutput.CrossSection; }
            set { _historyOutput.CrossSection = value; UpdateVisibility(); }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(5, 10, "Thickness")]
        [DescriptionAttribute("Out-of-plane thickness used when the cross section is set to Thickness.")]
        [TypeConverter(typeof(StringLengthConverter))]
        [Id(6, 1)]
        public double Thickness
        {
            get { return _historyOutput.Thickness; }
            set { _historyOutput.Thickness = value; }
        }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Element set")]
        [DescriptionAttribute("Select the element set for the creation of the history output.")]
        [Id(3, 2)]
        public string ElementSetName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(3, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the history output.")]
        [Id(4, 2)]
        public string SurfaceName { get { return _historyOutput.RegionName; } set { _historyOutput.RegionName = value; } }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(0, 10, "Field name")]
        [DescriptionAttribute("Filed name for the history output.")]
        [Id(1, 3)]
        public string FieldName
        {
            get { return _historyOutput.FieldName; }
            set
            {
                if (_historyOutput.FieldName != value)
                {
                    _historyOutput.FieldName = value;
                    UpdateComponents();
                }
            }
        }
        //
        [CategoryAttribute("Source")]
        [OrderedDisplayName(1, 10, "Components")]
        [DescriptionAttribute("Component names for the history output.")]
        [Editor(typeof(UserControls.FlagsEnumEditorUIEditor), typeof(UITypeEditor))]
        [Id(2, 3)]
        public MultiChoiceEnum ComponentNames
        {
            get
            {
                if (_componentContainer == null) return MultiChoiceEnum.Num1;   // at initialization
                else return _componentContainer.MultiChoice;
            }
            set
            {
                _componentContainer.MultiChoice = value;
                _historyOutput.ComponentNames = _componentContainer.Names;
            }
        }
        [CategoryAttribute("Mesh deformation")]
        [OrderedDisplayName(0, 10, "Variable")]
        [DescriptionAttribute("Select the deformation variable to be applied in the geometry calculation.")]
        [Id(1, 4)]
        public string DeformationVariableName
        {
            get { return _historyOutput.DeformationVariableName; }
            set { _historyOutput.DeformationVariableName = value; }
        }


        // Constructors                                                                                                             
        public ViewResultHistoryOutputFromIntegration(ResultHistoryOutputFromIntegration historyOutput)
            : base(historyOutput)
        {
            // The order is important
            _historyOutput = historyOutput;
            _defaultCrossSection = IntegrationScaleType.None;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.ElementSetName, nameof(ElementSetName));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_historyOutput, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override ResultHistoryOutput GetBase()
        {
            return _historyOutput;
        }
        // The cross section a line or volume integral takes again when the integral type changes from Surface
        public void SetDefaultCrossSection(IntegrationScaleType defaultCrossSection)
        {
            _defaultCrossSection = defaultCrossSection;
        }
        public void PopulateDropDownLists(string[] elementSetNames, string[] surfaceNames,
                                          Dictionary<string, string[]> filedNameComponentNames)
        {
            _elementSetNames = elementSetNames;
            _surfaceNames = surfaceNames;
            // Components
            List<string> componentNames;
            _filedNameComponentNames = new Dictionary<string, string[]>();
            foreach (var fieldEntry in filedNameComponentNames)
            {
                componentNames = new List<string>();
                foreach (var componentName in fieldEntry.Value) componentNames.Add(componentName);
                if (componentNames.Count > 0) _filedNameComponentNames.Add(fieldEntry.Key, componentNames.ToArray());
            }
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(FieldName), _filedNameComponentNames.Keys.ToArray());
            UpdateComponents(_historyOutput.ComponentNames);
            //
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DeformationVariableName),
                                                         FeResults.GetPossibleDeformationFieldOutputNamesMap().Keys.ToArray());
            // Must be last: PopulateProperty resets the browsable state of the populated properties
            UpdateVisibility();
        }
        private void UpdateComponents(string[] selectedComponentNames = null)
        {
            string[] componentNames;
            if (_filedNameComponentNames != null && _filedNameComponentNames.TryGetValue(FieldName, out componentNames) &&
                componentNames.Length > 0)
            {
                if (selectedComponentNames == null) selectedComponentNames = componentNames;
                _componentContainer = new MultiChoiceContainer(componentNames, selectedComponentNames);
                DynamicCustomTypeDescriptor.RenameMultiChoiceEnumProperty(nameof(ComponentNames), _componentContainer.EnumData);
                //
                _historyOutput.ComponentNames = _componentContainer.Names;
            }
        }
        private void UpdateVisibility()
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            //
            bool crossSectionVisible;
            if (_historyOutput.IntegralType == IntegralTypeEnum.Line)
            {
                // The line region is a set of geometry edges - selection only
                crossSectionVisible = true;
            }
            else if (_historyOutput.IntegralType == IntegralTypeEnum.Surface)
            {
                regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, _surfaceNames);
                crossSectionVisible = false;
            }
            else if (_historyOutput.IntegralType == IntegralTypeEnum.Volume)
            {
                regionTypeListItemsPairs.Add(RegionTypeEnum.ElementSetName, _elementSetNames);
                crossSectionVisible = true;
            }
            else throw new NotSupportedException();
            //
            base.PopulateDropDownLists(regionTypeListItemsPairs);
            // The geometry (size) result integrates the unit field, so no field/components are needed
            bool fieldVisible = _historyOutput.IntegralResult != IntegralResultEnum.Geometry &&
                                _filedNameComponentNames != null && _filedNameComponentNames.Count > 0;
            DynamicCustomTypeDescriptor.GetProperty(nameof(FieldName)).SetIsBrowsable(fieldVisible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(ComponentNames)).SetIsBrowsable(fieldVisible);
            // Out-of-plane cross section scaling (Line and Volume integrals of a 2D region)
            DynamicCustomTypeDescriptor.GetProperty(nameof(CrossSection)).SetIsBrowsable(crossSectionVisible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Thickness)).SetIsBrowsable(
                crossSectionVisible && _historyOutput.CrossSection == IntegrationScaleType.Thickness);
        }
    }
}