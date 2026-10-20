using System;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewHyperelastic : ViewMaterialProperty
    {
        // Variables
        private Hyperelastic _hyperelastic;


        // Properties
        public override string Name
        {
            get { return "Hyperelastic - Mooney-Rivlin"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "C10")]
        [DescriptionAttribute("First Mooney-Rivlin constant C10 [pressure unit].")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString C10
        {
            get { return _hyperelastic.C10.Equation; }
            set { _hyperelastic.C10.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "C01")]
        [DescriptionAttribute("Second Mooney-Rivlin constant C01 [pressure unit]. Use 0 for Neo-Hookean.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString C01
        {
            get { return _hyperelastic.C01.Equation; }
            set { _hyperelastic.C01.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(2, 10, "D1")]
        [DescriptionAttribute("Compressibility parameter D1 = 2/K [1/pressure unit]. Use 0 for incompressible material.")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString D1
        {
            get { return _hyperelastic.D1.Equation; }
            set { _hyperelastic.D1.Equation = value; }
        }


        // Constructors
        public ViewHyperelastic(Hyperelastic hyperelastic)
        {
            _hyperelastic = hyperelastic;
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods
        public override MaterialProperty GetBase()
        {
            return _hyperelastic;
        }
        public double GetC10Value()
        {
            return _hyperelastic.C10.Value;
        }
    }
}
