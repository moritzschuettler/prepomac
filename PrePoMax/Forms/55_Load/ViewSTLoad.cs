using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeModel;
using System.Drawing.Design;

namespace PrePoMax
{
    [Serializable]
    public class ViewSTLoad : ViewLoad
    {
        // Variables                                                                                                                
        private STLoad _stLoad;


        // Properties                                                                                                               
        public override string Name { get { return _stLoad.Name; } set { _stLoad.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the load.")]
        [Id(3, 2)]
        public string SurfaceName { get { return _stLoad.SurfaceName; } set { _stLoad.SurfaceName = value; } }
        //
        [CategoryAttribute("Force components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Value of the force component in the direction of the first axis.")]
        [TypeConverter(typeof(EquationForceConverter))]
        [Id(1, 3)]
        public EquationString F1 { get { return _stLoad.F1.Equation; } set { _stLoad.F1.Equation = value; } }
        //
        [CategoryAttribute("Force components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Value of the force component in the direction of the second axis.")]
        [TypeConverter(typeof(EquationForceConverter))]
        [Id(2, 3)]
        public EquationString F2 { get { return _stLoad.F2.Equation; } set { _stLoad.F2.Equation = value; } }
        //
        [CategoryAttribute("Force components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Value of the force component in the direction of the third axis.")]
        [TypeConverter(typeof(EquationForceConverter))]
        [Id(3, 3)]
        public EquationString F3 { get { return _stLoad.F3.Equation; } set { _stLoad.F3.Equation = value; } }
        //
        [CategoryAttribute("Force magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the surface traction load magnitude.")]
        [TypeConverter(typeof(EquationForceConverter))]
        [Id(1, 4)]
        public EquationString FMagnitude
        {
            get { return _stLoad.FMagnitude.Equation; }
            set { _stLoad.FMagnitude.Equation = value; }
        }
        //
        [CategoryAttribute("Force per area components")]
        [OrderedDisplayName(0, 10, "F1")]
        [DescriptionAttribute("Value of the force component in the direction of the first axis.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        [Id(1, 3)]
        public EquationString P1 { get { return _stLoad.P1.Equation; } set { _stLoad.P1.Equation = value; } }
        //
        [CategoryAttribute("Force per area components")]
        [OrderedDisplayName(1, 10, "F2")]
        [DescriptionAttribute("Value of the force component in the direction of the second axis.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        [Id(2, 3)]
        public EquationString P2 { get { return _stLoad.P2.Equation; } set { _stLoad.P2.Equation = value; } }
        //
        [CategoryAttribute("Force per area components")]
        [OrderedDisplayName(2, 10, "F3")]
        [DescriptionAttribute("Value of the force component in the direction of the third axis.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        [Id(3, 3)]
        public EquationString P3 { get { return _stLoad.P3.Equation; } set { _stLoad.P3.Equation = value; } }
        //
        [CategoryAttribute("Force per area magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Value of the surface traction load magnitude.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        [Id(1, 4)]
        public EquationString PMagnitude
        {
            get { return _stLoad.PMagnitude.Equation; }
            set { _stLoad.PMagnitude.Equation = value; }
        }
        //
        [CategoryAttribute("Force phase")]
        [OrderedDisplayName(0, 10, "Phase")]
        [DescriptionAttribute("Value of the surface traction phase.")]
        [TypeConverter(typeof(EquationAngleDegConverter))]
        [Id(1, 5)]
        public EquationString Phase { get { return _stLoad.PhaseDeg.Equation; } set { _stLoad.PhaseDeg.Equation = value; } }
        //
        [CategoryAttribute("Distribution")]
        [OrderedDisplayName(0, 10, "Distribution")]
        [DescriptionAttribute("Select the distribution for the load.")]
        [Id(1, 17)]
        public string DistributionName
        {
            get { return _stLoad.DistributionName; }
            set
            {
                _stLoad.DistributionName = value;
                UpdateVisibility();
            }
        }
        //
        public override string AmplitudeName { get { return _stLoad.AmplitudeName; } set { _stLoad.AmplitudeName = value; } }
        public override string CoordinateSystemName
        {
            get { return _stLoad.CoordinateSystemName; }
            set { _stLoad.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color { get { return _stLoad.Color; } set { _stLoad.Color = value; } }


        // Constructors                                                                                                             
        public ViewSTLoad(STLoad stLoad)
        {
            // The order is important
            _stLoad = stLoad;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_stLoad, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            // Phase
            DynamicCustomTypeDescriptor.GetProperty(nameof(Phase)).SetIsBrowsable(stLoad.Complex);
            //
            UpdateVisibility();
        }



        // Methods                                                                                                                  
        public override Load GetBase()
        {
            return _stLoad;
        }
        public void PopulateDropDownLists(string[] surfaceNames, string[] distributionNames, string[] amplitudeNames,
                                          string[] coordinateSystemNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateDistributionNames(distributionNames);
            //
            PopulateAmplitudeNames(amplitudeNames);
            //
            PopulateCoordinateSystemNames(coordinateSystemNames);
        }
        public void PopulateDistributionNames(string[] distributionNames)
        {
            List<string> names = new List<string>() { Distribution.DefaultDistributionName };
            names.AddRange(distributionNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(DistributionName), names.ToArray(), false, 2);
        }
        public void UpdateVisibility()
        {
            bool visible = _stLoad.DistributionName == Distribution.DefaultDistributionName;
            DynamicCustomTypeDescriptor.GetProperty(nameof(F1)).SetIsBrowsable(visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(F2)).SetIsBrowsable(visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(F3)).SetIsBrowsable(visible && !_stLoad.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(FMagnitude)).SetIsBrowsable(visible);
            //
            DynamicCustomTypeDescriptor.GetProperty(nameof(P1)).SetIsBrowsable(!visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(P2)).SetIsBrowsable(!visible);
            DynamicCustomTypeDescriptor.GetProperty(nameof(P3)).SetIsBrowsable(!visible && !_stLoad.TwoD);
            DynamicCustomTypeDescriptor.GetProperty(nameof(PMagnitude)).SetIsBrowsable(!visible);
        }
    }

}
