using System;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class ViewHyperelasticNeoHooke : ViewMaterialProperty
    {
        // Variables
        private HyperelasticNeoHooke _neoHooke;


        // Properties
        public override string Name
        {
            get { return "Hyperelastic - Neo Hooke"; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(0, 10, "C10")]
        [DescriptionAttribute("Neo-Hookean constant C10 [pressure unit]. Initial shear modulus: G = 2*C10.")]
        [TypeConverter(typeof(EquationPressureConverter))]
        public EquationString C10
        {
            get { return _neoHooke.C10.Equation; }
            set { _neoHooke.C10.Equation = value; }
        }
        //
        [CategoryAttribute("Data")]
        [OrderedDisplayName(1, 10, "D1")]
        [DescriptionAttribute("Compressibility parameter D1 = 2/K [1/pressure unit]. Use 0 for incompressible (requires hybrid elements).")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString D1
        {
            get { return _neoHooke.D1.Equation; }
            set { _neoHooke.D1.Equation = value; }
        }


        // Constructors
        public ViewHyperelasticNeoHooke(HyperelasticNeoHooke neoHooke)
        {
            _neoHooke = neoHooke;
            base.DynamicCustomTypeDescriptor = ProviderInstaller.Install(this);
        }


        // Methods
        public override MaterialProperty GetBase()
        {
            return _neoHooke;
        }
        public double GetC10Value()
        {
            return _neoHooke.C10.Value;
        }
    }
}
