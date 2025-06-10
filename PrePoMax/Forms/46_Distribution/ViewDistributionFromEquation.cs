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
    public class ViewDistributionFromEquation : ViewDistribution
    {
        // Variables                                                                                                                
        private DistributionFromEquation _distributionFromEquation;


        // Properties                                                                                                               
        public override string Name
        {
            get { return _distributionFromEquation.Name; }
            set { _distributionFromEquation.Name = value; }
        }
        public override DistributionTypeEnum DistributionType
        {
            get { return _distributionFromEquation.DistributionType; }
            set
            {
                if (_distributionFromEquation.DistributionType != value)
                {
                    _distributionFromEquation.DistributionType = value;
                    UpdateVisibility();
                }
            }
        }
        //
        [CategoryAttribute("Distribution magnitude")]
        [OrderedDisplayName(0, 10, "Magnitude")]
        [DescriptionAttribute("Enter magnitude equation. Example equation: =Sin(x+y+z). " +
                              "To include coordinates into the equation refer to them as x, y and z.")]
        [Id(1, 2)]
        public string EquationMagnitude
        {
            get { return _distributionFromEquation.EquationMagnitude; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _distributionFromEquation.EquationMagnitude = value;
            }
        }
        //
        [CategoryAttribute("Distribution components")]
        [OrderedDisplayName(0, 10, "D1")]
        [DescriptionAttribute("Enter distribution equation for the direction of the first axis. Example equation: " +
                              "=Sin(x+y+z). To include coordinates into the equation refer to them as x, y and z.")]
        [Id(2, 2)]
        public string EquationD1
        {
            get { return _distributionFromEquation.EquationD1; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _distributionFromEquation.EquationD1 = value;
            }
        }
        //
        [CategoryAttribute("Distribution components")]
        [OrderedDisplayName(1, 10, "D2")]
        [DescriptionAttribute("Enter distribution equation for the direction of the second axis. Example equation: " +
                              "=Sin(x+y+z). To include coordinates into the equation refer to them as x, y and z.")]
        [Id(3, 2)]
        public string EquationD2
        {
            get { return _distributionFromEquation.EquationD2; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _distributionFromEquation.EquationD2 = value;
            }
        }
        //
        [CategoryAttribute("Distribution components")]
        [OrderedDisplayName(2, 10, "D3")]
        [DescriptionAttribute("Enter distribution equation for the direction of the third axis. Example equation: " +
                              "=Sin(x+y+z). To include coordinates into the equation refer to them as x, y and z.")]
        [Id(4, 2)]
        public string EquationD3
        {
            get { return _distributionFromEquation.EquationD3; }
            set
            {
                if (!value.Trim().StartsWith("=")) value = "=" + value;
                _distributionFromEquation.EquationD3 = value;
            }
        }


        // Constructors                                                                                                             
        public ViewDistributionFromEquation(DistributionFromEquation distributionFromEquation)
        {
            _distributionFromEquation = distributionFromEquation;
            //
            DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            //
            UpdateVisibility();
        }


        // Methods
        public override Distribution GetBase()
        {
            return _distributionFromEquation;
        }
        private void UpdateVisibility()
        {
            bool scalar = _distributionFromEquation.DistributionType == DistributionTypeEnum.Scalar;
            DynamicCustomTypeDescriptor.GetProperty(nameof(EquationMagnitude)).SetIsBrowsable(scalar);
            DynamicCustomTypeDescriptor.GetProperty(nameof(EquationD1)).SetIsBrowsable(!scalar);
            DynamicCustomTypeDescriptor.GetProperty(nameof(EquationD2)).SetIsBrowsable(!scalar);
            DynamicCustomTypeDescriptor.GetProperty(nameof(EquationD3)).SetIsBrowsable(!scalar);
        }
    }
}
