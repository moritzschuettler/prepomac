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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.ComponentModel;
using DynamicTypeDescriptor;

namespace PrePoMax.Forms
{
    [PmxType(10347)]
    public enum GmshElementQualityHighlightCriteriaEnum
    {
        Smaller,
        Larger,
    }

    [PmxType(10348)]
    public class ViewElementQuality
    {
        // Variables
        [PmxKey(8)] private ElementQualityMetricEnum _elementQualityMetric;
        [PmxKey(1)] private double _min;
        [PmxKey(2)] private double _max;
        [PmxKey(3)] private double _average;
        [PmxKey(4)] private double _standardDeviation;
        [PmxKey(9)] private int _numberOfElements;
        [PmxKey(10)] private int _numberOfHighlightedElements;
        [PmxKey(5)] private GmshElementQualityHighlightCriteriaEnum _highlightCriteria;
        [PmxKey(6)] private double _highlightLimit;
        [PmxKey(14)] private bool _highlightElements;
        [PmxKey(7)] private DynamicCustomTypeDescriptor _dctd = null;
        private static readonly Dictionary<ElementQualityMetricEnum, string> _metricDescriptions =
            new Dictionary<ElementQualityMetricEnum, string>()
            {
                { ElementQualityMetricEnum.ScaledJacobian, "Verdict scaled Jacobian: the smallest Jacobian determinant " +
                                                           "at the corners divided by the lengths of the edges there. " +
                                                           "It is 1 for an ideal element and zero or negative for an " +
                                                           "inverted one. All element types; parabolic elements are " +
                                                           "measured on their corner nodes." },
                { ElementQualityMetricEnum.AspectRatio, "Verdict aspect ratio. It is 1 for an equilateral element. " +
                                                        "Triangles, quadrilaterals and tetrahedra." },
                { ElementQualityMetricEnum.EdgeRatio, "The longest edge of the element divided by its shortest edge. " +
                                                      "All element types." },
                { ElementQualityMetricEnum.MinimumAngle, "The smallest corner angle in degrees of the element, or of " +
                                                         "the faces of a solid element. All element types." },
                { ElementQualityMetricEnum.MaximumAngle, "The largest corner angle in degrees of the element, or of " +
                                                         "the faces of a solid element. All element types." },
                { ElementQualityMetricEnum.Skew, "Verdict skew: the largest cosine of the angles between the " +
                                                 "principal axes. It is 0 for a rectangular element. Quadrilaterals " +
                                                 "and hexahedra." },
                { ElementQualityMetricEnum.Warpage, "The largest angle in degrees between the normals of the two " +
                                                    "triangles a quadrilateral face splits into, over both diagonals. " +
                                                    "It is 0 for a flat face. Elements with quadrilateral faces." },
                { ElementQualityMetricEnum.Taper, "Verdict taper: the largest difference of the opposite edges " +
                                                  "relative to their average. It is 0 for a parallelogram. " +
                                                  "Quadrilaterals and hexahedra." },
                { ElementQualityMetricEnum.minDetJac, "Gmsh: adaptively computed minimal Jacobian determinant. " +
                                                      "A negative value indicates an inverted element." },
                { ElementQualityMetricEnum.maxDetJac, "Gmsh: adaptively computed maximal Jacobian determinant." },
                { ElementQualityMetricEnum.minSJ, "Gmsh: sampled minimal scaled Jacobian. " +
                                                  "The value is 1 for an ideal element and zero or negative for an " +
                                                  "invalid element." },
                { ElementQualityMetricEnum.minSICN, "Gmsh: sampled minimal signed inverted condition number. " +
                                                    "The value ranges from -1 to 1; it is 1 for an ideal element " +
                                                    "and negative for an invalid element." },
                { ElementQualityMetricEnum.minSIGE, "Gmsh: sampled signed inverted gradient error. " +
                                                    "The value ranges from -1 to 1; it is 1 for an ideal element " +
                                                    "and negative for an invalid element." },
                { ElementQualityMetricEnum.gamma, "Gmsh: ratio of the inscribed to the circumscribed sphere radius, " +
                                                  "normalized so that it is 1 for an equilateral element." },
                { ElementQualityMetricEnum.innerRadius, "Gmsh: inner radius of the element." },
                { ElementQualityMetricEnum.outerRadius, "Gmsh: outer radius of the element." },
                { ElementQualityMetricEnum.minIsotropy, "Gmsh: minimum isotropy measure." },
                { ElementQualityMetricEnum.angleShape, "Gmsh: angle shape measure." },
                { ElementQualityMetricEnum.minEdge, "Gmsh: minimum straight edge length." },
                { ElementQualityMetricEnum.maxEdge, "Gmsh: maximum straight edge length." },
                { ElementQualityMetricEnum.volume, "Gmsh: volume of the element." },
            };


        // Properties
        [Browsable(false)]
        public ElementQualityMetricEnum ElementQualityMetric
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
        [CategoryAttribute("Results")]
        [OrderedDisplayName(4, 10, "Measured elements")]
        [DescriptionAttribute("The number of elements the metric is defined for.")]
        [ReadOnly(true)]
        public int NumberOfElements { get { return _numberOfElements; } set { _numberOfElements = value; } }
        //
        [CategoryAttribute("Results")]
        [OrderedDisplayName(5, 10, "Elements past the limit")]
        [DescriptionAttribute("The number of elements past the highlight limit.")]
        [ReadOnly(true)]
        public int NumberOfHighlightedElements
        {
            get { return _numberOfHighlightedElements; }
            set { _numberOfHighlightedElements = value; }
        }
        //
        [CategoryAttribute("Limit")]
        [OrderedDisplayName(0, 10, "Worse values")]
        [DescriptionAttribute("Select whether smaller or larger values of the metric are worse.")]
        public GmshElementQualityHighlightCriteriaEnum HighlightCriteria
        {
            get { return _highlightCriteria; }
            set { _highlightCriteria = value; }
        }
        //
        [CategoryAttribute("Limit")]
        [OrderedDisplayName(1, 10, "Highlight limit")]
        [DescriptionAttribute("Elements past this value are highlighted. Create Set adds an element set of them.")]
        public double HighlightLimit { get { return _highlightLimit; } set { _highlightLimit = value; } }
        //
        [CategoryAttribute("Display")]
        [OrderedDisplayName(0, 10, "Highlight elements")]
        [DescriptionAttribute("Highlight the elements past the highlight limit, the ones inside a solid included.")]
        public bool HighlightElements { get { return _highlightElements; } set { _highlightElements = value; } }
        //
        [Browsable(false)]
        public bool LargerIsWorse { get { return _highlightCriteria == GmshElementQualityHighlightCriteriaEnum.Larger; } }


        // Constructors
        public ViewElementQuality()
        {
            _elementQualityMetric = ElementQualityMetricEnum.ScaledJacobian;
            _min = 0;
            _max = 0;
            _average = 0;
            _standardDeviation = 0;
            _numberOfElements = 0;
            _numberOfHighlightedElements = 0;
            _highlightCriteria = GmshElementQualityHighlightCriteriaEnum.Smaller;
            _highlightLimit = 0;
            _highlightElements = true;
            //
            _dctd = ProviderInstaller.Install(this);
            _dctd.RenameBooleanPropertyToYesNo(nameof(HighlightElements));
        }


        // Static methods
        public static bool IsGmshMetric(ElementQualityMetricEnum metric)
        {
            return metric >= ElementQualityMetricEnum.minDetJac;
        }
        public static string GetMetricName(ElementQualityMetricEnum metric)
        {
            FieldInfo field = typeof(ElementQualityMetricEnum).GetField(metric.ToString());
            StandardValueAttribute attribute = field.GetCustomAttribute<StandardValueAttribute>();
            string name = attribute != null ? attribute.DisplayName : metric.ToString();
            return IsGmshMetric(metric) ? name + " (Gmsh)" : name;
        }
        public static string GetMetricDescription(ElementQualityMetricEnum metric)
        {
            return _metricDescriptions[metric];
        }
        // The limit a mesh is commonly judged by; a metric without one is judged against its own distribution
        public static bool GetDefaultLimit(ElementQualityMetricEnum metric, out bool largerIsWorse, out double limit)
        {
            largerIsWorse = false;
            limit = 0;
            switch (metric)
            {
                case ElementQualityMetricEnum.ScaledJacobian:
                case ElementQualityMetricEnum.minSJ:
                case ElementQualityMetricEnum.minSICN:
                case ElementQualityMetricEnum.minSIGE:
                case ElementQualityMetricEnum.gamma:
                    limit = 0.1;
                    return true;
                case ElementQualityMetricEnum.minDetJac:
                    return true;
                case ElementQualityMetricEnum.AspectRatio:
                case ElementQualityMetricEnum.EdgeRatio:
                    largerIsWorse = true;
                    limit = 10;
                    return true;
                case ElementQualityMetricEnum.MinimumAngle:
                    limit = 15;
                    return true;
                case ElementQualityMetricEnum.MaximumAngle:
                    largerIsWorse = true;
                    limit = 160;
                    return true;
                case ElementQualityMetricEnum.Skew:
                case ElementQualityMetricEnum.Taper:
                    largerIsWorse = true;
                    limit = 0.8;
                    return true;
                case ElementQualityMetricEnum.Warpage:
                    largerIsWorse = true;
                    limit = 20;
                    return true;
                case ElementQualityMetricEnum.maxDetJac:
                case ElementQualityMetricEnum.outerRadius:
                case ElementQualityMetricEnum.maxEdge:
                    largerIsWorse = true;
                    return false;
                default:
                    return false;
            }
        }


        // Methods
        public void SetValues(double min, double max, double average, double standardDeviation, int numberOfElements)
        {
            _min = Tools.RoundToSignificantDigits(min, 4);
            _max = Tools.RoundToSignificantDigits(max, 4);
            _average = Tools.RoundToSignificantDigits(average, 4);
            _standardDeviation = Tools.RoundToSignificantDigits(standardDeviation, 4);
            _numberOfElements = numberOfElements;
        }
        public void SetLimit(bool largerIsWorse, double limit)
        {
            _highlightCriteria = largerIsWorse ? GmshElementQualityHighlightCriteriaEnum.Larger
                                               : GmshElementQualityHighlightCriteriaEnum.Smaller;
            _highlightLimit = Tools.RoundToSignificantDigits(limit, 4);
        }
    }
}
