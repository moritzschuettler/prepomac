using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;
using DynamicTypeDescriptor;

namespace PrePoMax
{
    [Serializable]
    public abstract class ViewMappedDistribution : ViewDistribution
    {
        // Variables                                                                                                                
        private DynamicCustomTypeDescriptor _dctd;
        private MappedDistribution _mappedDistribution;


        // Properties                                                                                                               
        public override string Name
        {
            get { return _mappedDistribution.Name; }
            set { _mappedDistribution.Name = value; }
        }
        //
        [Browsable(false)]
        public override DistributionTypeEnum DistributionType
        {
            get { return _mappedDistribution.DistributionType; }
            set { _mappedDistribution.DistributionType = value; }
        }
        //
        [CategoryAttribute("Scale factors")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("Scale factor in the X direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(1, 9)]
        public EquationString ScaleX
        {
            get { return _mappedDistribution.ScaleX.Equation; }
            set { _mappedDistribution.ScaleX.Equation = value; }
        }
        //
        [CategoryAttribute("Scale factors")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Scale factor in the Y direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(2, 9)]
        public EquationString ScaleY
        {
            get { return _mappedDistribution.ScaleY.Equation; }
            set { _mappedDistribution.ScaleY.Equation = value; }
        }
        //
        [CategoryAttribute("Scale factors")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Scale factor in the Z direction.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        [Id(3, 9)]
        public EquationString ScaleZ
        {
            get { return _mappedDistribution.ScaleZ.Equation; }
            set { _mappedDistribution.ScaleZ.Equation = value; }
        }
        //
        [CategoryAttribute("Translations")]
        [OrderedDisplayName(0, 10, "X")]
        [DescriptionAttribute("Translation in the X direction.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(1, 10)]
        public EquationString TranslateX
        {
            get { return _mappedDistribution.TranslateX.Equation; }
            set { _mappedDistribution.TranslateX.Equation = value; }
        }
        //
        [CategoryAttribute("Translations")]
        [OrderedDisplayName(1, 10, "Y")]
        [DescriptionAttribute("Translation in the Y direction.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(2, 10)]
        public EquationString TranslateY
        {
            get { return _mappedDistribution.TranslateY.Equation; }
            set { _mappedDistribution.TranslateY.Equation = value; }
        }
        //
        [CategoryAttribute("Translations")]
        [OrderedDisplayName(2, 10, "Z")]
        [DescriptionAttribute("Translation in the Z direction.")]
        [TypeConverter(typeof(EquationLengthConverter))]
        [Id(3, 10)]
        public EquationString TranslateZ
        {
            get { return _mappedDistribution.TranslateZ.Equation; }
            set { _mappedDistribution.TranslateZ.Equation = value; }
        }
        //
        public override string CoordinateSystemName
        {
            get { return _mappedDistribution.CoordinateSystemName; }
            set { _mappedDistribution.CoordinateSystemName = value; }
        }


        // Constructors                                                                                                             
        public ViewMappedDistribution(MappedDistribution mappedDistribution)
        {
            _mappedDistribution = mappedDistribution;
        }


        // Methods
    }
}
