using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using DynamicTypeDescriptor;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using System.Drawing.Design;

namespace PrePoMax
{
    [Serializable]
    public class ViewImportedSTLoad : ViewLoad
    {
        // Variables                                                                                                                
        private ImportedSTLoad _importedSTLoad;


        // Properties                                                                                                               
        public override string Name { get { return _importedSTLoad.Name; } set { _importedSTLoad.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the load.")]
        [Id(3, 2)]
        public string SurfaceName { get { return _importedSTLoad.SurfaceName; } set {_importedSTLoad.SurfaceName = value;} }
        //
        [CategoryAttribute("Load magnitude")]
        [OrderedDisplayName(0, 10, "Import from file")]
        [DescriptionAttribute("Select the file from which the surface traction results will be imported.")]
        [EditorAttribute(typeof(FilteredFileNameEditor), typeof(UITypeEditor))]
        [Id(1, 3)]
        public string FileName
        {
            get { return _importedSTLoad.FileName; }
            set { _importedSTLoad.FileName = value; }
        }
        //
        [CategoryAttribute("Load magnitude")]
        [OrderedDisplayName(1, 10, "Interpolator")]
        [DescriptionAttribute("Select the interpolation type. The Gauss interpoaltion uses the kernel equation: exp(-(r/R)²), " +
                              "while the Shepard interpolation uses the kernel equation: 1/r². R is the interploator radius and " +
                              "r is the distance to the neighbouring point.")]
        [Id(2, 3)]
        public CaeResults.CloudInterpolatorEnum Interpolator
        {
            get { return _importedSTLoad.InterpolatorType; }
            set
            {
                _importedSTLoad.InterpolatorType = value;
                UpdateVisibility();
            }
        }
        //
        [CategoryAttribute("Load magnitude")]
        [OrderedDisplayName(2, 10, "Interpolator radius")]
        [DescriptionAttribute("Set the value of the interpolator kernel radius.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 3)]
        public EquationString InterpolatorRadius
        {
            get { return _importedSTLoad.InterpolatorRadius.Equation; }
            set { _importedSTLoad.InterpolatorRadius.Equation = value; }
        }
        //
        [CategoryAttribute("Load magnitude")]
        [OrderedDisplayName(3, 10, "Magnitude factor")]
        [DescriptionAttribute("Value of the surface traction magnitude scale factor.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(4, 3)]
        public EquationString MagnitudeFactor
        {
            get { return _importedSTLoad.MagnitudeFactor.Equation; }
            set { _importedSTLoad.MagnitudeFactor.Equation = value; }
        }
        //
        [CategoryAttribute("Load phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the surface traction phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 4)]
        public EquationString Phase
        {
            get { return _importedSTLoad.PhaseDeg.Equation; }
            set { _importedSTLoad.PhaseDeg.Equation = value; }
        }
        //
        [CategoryAttribute("Imported coordinates")]
        [OrderedDisplayName(0, 10, "Scale factor")]
        [DescriptionAttribute("Value of the scale factor for the imported coordinates.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(1, 5)]
        public EquationString GeometryScaleFactor
        {
            get { return _importedSTLoad.GeometryScaleFactor.Equation; }
            set { _importedSTLoad.GeometryScaleFactor.Equation = value; }
        }
        //
        public override string AmplitudeName
        {
            get { return _importedSTLoad.AmplitudeName; }
            set { _importedSTLoad.AmplitudeName = value; }
        }
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _importedSTLoad.CoordinateSystemName; }
            set { _importedSTLoad.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _importedSTLoad.Color; }
            set { _importedSTLoad.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewImportedSTLoad(ImportedSTLoad importedSTLoad)
        {
            _importedSTLoad = importedSTLoad;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_importedSTLoad, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(_importedSTLoad.Complex);
        }


        // Methods                                                                                                                  
        public override Load GetBase()
        {
            return _importedSTLoad;
        }
        public void PopulateDropDownLists(string[] surfaceNames, string[] amplitudeNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            UpdateVisibility();
        }
        private void UpdateVisibility()
        {
            bool visibility = _importedSTLoad.InterpolatorType != CaeResults.CloudInterpolatorEnum.ClosestPoint;
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(InterpolatorRadius)).SetIsBrowsable(visibility);
            DynamicCustomTypeDescriptor.GetProperty(nameof(InterpolatorRadius)).SetIsBrowsable(visibility);
        }
        public void UpdateFileBrowserDialog()
        {
            FilteredFileNameEditor.Filter = "Text files|*.txt|All files|*.*";
        }
    }
}
