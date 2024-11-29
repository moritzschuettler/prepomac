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

namespace PrePoMax
{
    [Serializable]
    public class ViewFilmHeatTransfer : ViewLoad
    {
        // Variables                                                                                                                
        private FilmHeatTransfer _filmHeatTransfer;


        // Properties                                                                                                               
        public override string Name { get { return _filmHeatTransfer.Name; } set { _filmHeatTransfer.Name = value; } }
        //
        [CategoryAttribute("Region")]
        [OrderedDisplayName(2, 10, "Surface")]
        [DescriptionAttribute("Select the surface for the creation of the load.")]
        [Id(3, 2)]
        public string SurfaceName { get { return _filmHeatTransfer.SurfaceName; } set {_filmHeatTransfer.SurfaceName = value;} }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(0, 10, "Sink temperature")]
        [DescriptionAttribute("Value of the sink temperature.")]
        [TypeConverter(typeof(EquationTemperatureConverter))]
        [Id(1, 3)]
        public EquationString SinkTemperature
        {
            get { return _filmHeatTransfer.SinkTemperature.Equation; }
            set { _filmHeatTransfer.SinkTemperature.Equation = value; }
        }
        //
        [CategoryAttribute("Parameters")]
        [OrderedDisplayName(1, 10, "Film coefficient")]
        [DescriptionAttribute("Value of the film coefficient.")]
        [TypeConverter(typeof(EquationHeatTransferCoefficientConverter))]
        [Id(2, 3)]
        public EquationString FilmCoefficient
        {
            get { return _filmHeatTransfer.FilmCoefficient.Equation; }
            set { _filmHeatTransfer.FilmCoefficient.Equation = value; }
        }
        //
        [CategoryAttribute("Time/Frequency")]
        [OrderedDisplayName(0, 10, "Sink amplitude")]
        [DescriptionAttribute("Select the amplitude for the sink temperature.")]
        [Id(1, 18)]
        public override string AmplitudeName
        {
            get { return _filmHeatTransfer.AmplitudeName; }
            set { _filmHeatTransfer.AmplitudeName = value; }
        }
        //
        [CategoryAttribute("Time/Frequency")]
        [OrderedDisplayName(1, 10, "Coefficient amplitude")]
        [DescriptionAttribute("Select the amplitude for the film coefficient.")]
        [Id(2, 18)]
        public string CoefficientAmplitudeName
        {
            get { return _filmHeatTransfer.CoefficientAmplitudeName; }
            set { _filmHeatTransfer.CoefficientAmplitudeName = value; }
        }
        //
        [Browsable(false)]
        public override string CoordinateSystemName
        {
            get { return _filmHeatTransfer.CoordinateSystemName; }
            set { _filmHeatTransfer.CoordinateSystemName = value; }
        }
        public override System.Drawing.Color Color
        {
            get { return _filmHeatTransfer.Color; }
            set { _filmHeatTransfer.Color = value; }
        }


        // Constructors                                                                                                             
        public ViewFilmHeatTransfer(FilmHeatTransfer filmHeatTransfer)
        {
            _filmHeatTransfer = filmHeatTransfer;
            //
            Dictionary<RegionTypeEnum, string> regionTypePropertyNamePairs = new Dictionary<RegionTypeEnum, string>();
            regionTypePropertyNamePairs.Add(RegionTypeEnum.Selection, nameof(SelectionHidden));
            regionTypePropertyNamePairs.Add(RegionTypeEnum.SurfaceName, nameof(SurfaceName));
            //
            SetBase(_filmHeatTransfer, regionTypePropertyNamePairs);
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public override Load GetBase()
        {
            return _filmHeatTransfer;
        }
        public void PopulateDropDownLists(string[] surfaceNames, string[] amplitudeNames)
        {
            Dictionary<RegionTypeEnum, string[]> regionTypeListItemsPairs = new Dictionary<RegionTypeEnum, string[]>();
            regionTypeListItemsPairs.Add(RegionTypeEnum.Selection, new string[] { "Hidden" });
            regionTypeListItemsPairs.Add(RegionTypeEnum.SurfaceName, surfaceNames);
            PopulateDropDownLists(regionTypeListItemsPairs);
            //
            PopulateSinkAmplitudeNames(amplitudeNames);
            PopulateCoefficientAmplitudeNames(amplitudeNames);
        }
        public void PopulateSinkAmplitudeNames(string[] amplitudeNames)
        {
            PopulateAmplitudeNames(amplitudeNames);
        }
        public void PopulateCoefficientAmplitudeNames(string[] amplitudeNames)
        {
            List<string> names = new List<string>() { Load.DefaultAmplitudeName };
            names.AddRange(amplitudeNames);
            DynamicCustomTypeDescriptor.PopulateProperty(nameof(CoefficientAmplitudeName), names.ToArray(), false, 2);
        }
    }
}
