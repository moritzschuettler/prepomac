using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;
using System.Drawing.Printing;

namespace PrePoMax.Forms
{
    [Serializable]
    public enum GmshElementQualityMetricEnum
    {
        minDetJac,
        maxDetJac,
        minSJ,
        minSICN,
        minSIGE,
        gamma,
        innerRadius,
        outerRadius,
        minIsotropy,
        angleShape,
        minEdge,
        maxEdge,
        volume
    }
    [Serializable]
    public enum GmshElementQualityHighlightCriteriaEnum
    {
        Smaller,
        Larger,
    }

    [Serializable]
    public class ViewElementQuality
    {
        // Variables                                                                                                                
        private GmshElementQualityMetricEnum _elementQualityMetric;
        private double _min;
        private double _max;
        private double _average;
        private double _standardDeviation;
        private GmshElementQualityHighlightCriteriaEnum _highlightCriteria;
        private double _highlightLimit;
        private DynamicCustomTypeDescriptor _dctd = null;


        // Properties                                                                                                               
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "Quality metric")]
        [DescriptionAttribute("Select the element quality metric.")]
        public GmshElementQualityMetricEnum ElementQualityMetric
        {
            get { return _elementQualityMetric; }
            set { _elementQualityMetric = value; }
        }
        //
        [CategoryAttribute("Results")]
        [OrderedDisplayName(0, 10, "Minimum")]
        [DescriptionAttribute("Minimum value of the selected element quality metric.")]
        [ReadOnly(true)]
        public double Min { get { return _min; } set { _min = value; } }
        //
        [CategoryAttribute("Results")]
        [OrderedDisplayName(1, 10, "Maximum")]
        [DescriptionAttribute("Maximum value of the selected element quality metric.")]
        [ReadOnly(true)]
        public double Max { get { return _max; } set { _max = value; } }
        //
        [CategoryAttribute("Results")]
        [OrderedDisplayName(2, 10, "Average")]
        [DescriptionAttribute("Average value of the selected element quality metric.")]
        [ReadOnly(true)]
        public double Average { get { return _average; } set { _average = value; } }
        //
        [CategoryAttribute("Results")]
        [OrderedDisplayName(3, 10, "Standard deviation")]
        [DescriptionAttribute("Standard deviation value of the selected element quality metric.")]
        [ReadOnly(true)]
        public double StandardDeviation { get { return _standardDeviation; } set { _standardDeviation = value; } }
        //
        [CategoryAttribute("Highlight")]
        [OrderedDisplayName(0, 10, "Highlight criteria")]
        [DescriptionAttribute("Select the highlight criteria.")]
        public GmshElementQualityHighlightCriteriaEnum HighlightCriteria
        {
            get { return _highlightCriteria; }
            set { _highlightCriteria = value; }
        }
        //
        [CategoryAttribute("Highlight")]
        [OrderedDisplayName(1, 10, "Highlight limit")]
        [DescriptionAttribute("Set the highlight limit value.")]
        public double HighlightLimit
        {
            get { return _highlightLimit; }
            set { _highlightLimit = value; }
        }


        // Constructors                                                                                                             
        public ViewElementQuality()
        {
            _elementQualityMetric = GmshElementQualityMetricEnum.minDetJac;
            _min = 0;
            _max = 0;
            _average = 0;
            _standardDeviation = 0;
            _highlightCriteria = GmshElementQualityHighlightCriteriaEnum.Smaller;
            _highlightLimit = 0;
            //
            _dctd = ProviderInstaller.Install(this);
        }


        // Methods                                                                                                                  
        public void SetValues(double min, double max, double average, double standardDeviation, double limit)
        {
            _min = Tools.RoundToSignificantDigits(min, 4);
            _max = Tools.RoundToSignificantDigits(max, 4);
            _average = Tools.RoundToSignificantDigits(average, 4);
            _standardDeviation = Tools.RoundToSignificantDigits(standardDeviation, 4);
            _highlightLimit = Tools.RoundToSignificantDigits(limit, 4);
        }


    }
}
