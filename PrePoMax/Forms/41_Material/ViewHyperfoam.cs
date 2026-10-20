using System;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewHyperfoam : ViewMaterialProperty
    {
        // Variables
        private Hyperfoam _hyperfoam;


        // Properties
        public override string Name
        {
            get { return "Hyperfoam - Ogden"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "N")]
        [DescriptionAttribute("Order of the Hyperfoam Ogden series (1, 2 or 3).")]
        public int N
        {
            get { return _hyperfoam.N; }
            set
            {
                if (value < 1) value = 1;
                if (value > 3) value = 3;
                _hyperfoam.N = value;
                UpdateBrowsability();
            }
        }
        // Term 1
        [CategoryAttribute("Term 1")]
        [OrderedDisplayName(1, 10, "Mu1")]
        [DescriptionAttribute("Shear modulus-like parameter Mu1 for term 1 [pressure unit].")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString Mu1
        {
            get { return _hyperfoam.Mu1.Equation; }
            set { _hyperfoam.Mu1.Equation = value; }
        }
        //
        [CategoryAttribute("Term 1")]
        [OrderedDisplayName(2, 10, "Alpha1")]
        [DescriptionAttribute("Strain exponent Alpha1 for term 1 (dimensionless).")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Alpha1
        {
            get { return _hyperfoam.Alpha1.Equation; }
            set { _hyperfoam.Alpha1.Equation = value; }
        }
        //
        [CategoryAttribute("Term 1")]
        [OrderedDisplayName(3, 10, "Nu1")]
        [DescriptionAttribute("Compressibility (Poisson's ratio) Nu1 for term 1. Use 0 for incompressible.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu1
        {
            get { return _hyperfoam.Nu1.Equation; }
            set { _hyperfoam.Nu1.Equation = value; }
        }
        // Term 2
        [CategoryAttribute("Term 2")]
        [OrderedDisplayName(4, 10, "Mu2")]
        [DescriptionAttribute("Shear modulus-like parameter Mu2 for term 2 [pressure unit].")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString Mu2
        {
            get { return _hyperfoam.Mu2.Equation; }
            set { _hyperfoam.Mu2.Equation = value; }
        }
        //
        [CategoryAttribute("Term 2")]
        [OrderedDisplayName(5, 10, "Alpha2")]
        [DescriptionAttribute("Strain exponent Alpha2 for term 2 (dimensionless).")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Alpha2
        {
            get { return _hyperfoam.Alpha2.Equation; }
            set { _hyperfoam.Alpha2.Equation = value; }
        }
        //
        [CategoryAttribute("Term 2")]
        [OrderedDisplayName(6, 10, "Nu2")]
        [DescriptionAttribute("Compressibility (Poisson's ratio) Nu2 for term 2. Use 0 for incompressible.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu2
        {
            get { return _hyperfoam.Nu2.Equation; }
            set { _hyperfoam.Nu2.Equation = value; }
        }
        // Term 3
        [CategoryAttribute("Term 3")]
        [OrderedDisplayName(7, 10, "Mu3")]
        [DescriptionAttribute("Shear modulus-like parameter Mu3 for term 3 [pressure unit].")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString Mu3
        {
            get { return _hyperfoam.Mu3.Equation; }
            set { _hyperfoam.Mu3.Equation = value; }
        }
        //
        [CategoryAttribute("Term 3")]
        [OrderedDisplayName(8, 10, "Alpha3")]
        [DescriptionAttribute("Strain exponent Alpha3 for term 3 (dimensionless).")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Alpha3
        {
            get { return _hyperfoam.Alpha3.Equation; }
            set { _hyperfoam.Alpha3.Equation = value; }
        }
        //
        [CategoryAttribute("Term 3")]
        [OrderedDisplayName(9, 10, "Nu3")]
        [DescriptionAttribute("Compressibility (Poisson's ratio) Nu3 for term 3. Use 0 for incompressible.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu3
        {
            get { return _hyperfoam.Nu3.Equation; }
            set { _hyperfoam.Nu3.Equation = value; }
        }


        // Constructors
        public ViewHyperfoam(Hyperfoam hyperfoam)
        {
            _hyperfoam = hyperfoam;
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
            UpdateBrowsability();
        }


        // Methods
        public override MaterialProperty GetBase()
        {
            return _hyperfoam;
        }
        public double GetMu1Value()
        {
            return _hyperfoam.Mu1.Value;
        }
        private void UpdateBrowsability()
        {
            bool term2 = _hyperfoam.N >= 2;
            bool term3 = _hyperfoam.N >= 3;
            DynamicCustomTypeDescriptor.GetProperty(nameof(Mu2)).SetIsBrowsable(term2);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Alpha2)).SetIsBrowsable(term2);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Nu2)).SetIsBrowsable(term2);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Mu3)).SetIsBrowsable(term3);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Alpha3)).SetIsBrowsable(term3);
            DynamicCustomTypeDescriptor.GetProperty(nameof(Nu3)).SetIsBrowsable(term3);
        }
    }
}
