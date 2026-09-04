// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public class ElasticLaminaDataPoint : TempDataPoint
    {
        // Variables                                                                                                                
        private EquationContainer _youngsModulus1;
        private EquationContainer _youngsModulus2;
        private EquationContainer _poissonsRatio12;
        private EquationContainer _shearModulus12;
        private EquationContainer _shearModulus13;
        private EquationContainer _shearModulus23;


        // Properties                                                                                                               
        [DisplayName("E1\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString YoungsModulus1Eq { get { return _youngsModulus1.Equation; } set { _youngsModulus1.Equation = value; } }
        [Browsable(false)]
        public EquationContainer YoungsModulus1 { get { return _youngsModulus1; } set { _youngsModulus1 = value; } }
        //
        [DisplayName("E2\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString YoungsModulus2Eq { get { return _youngsModulus2.Equation; } set { _youngsModulus2.Equation = value; } }
        [Browsable(false)]
        public EquationContainer YoungsModulus2 { get { return _youngsModulus2; } set { _youngsModulus2 = value; } }
        //
        [DisplayName("v12\n[?]")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString PoissonsRatio12Eq
        {
            get { return _poissonsRatio12.Equation; }
            set { _poissonsRatio12.Equation = value; }
        }
        [Browsable(false)]
        public EquationContainer PoissonsRatio12 { get { return _poissonsRatio12; } set { _poissonsRatio12 = value; } }
        //
        [DisplayName("G12\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString ShearModulus12Eq { get { return _shearModulus12.Equation; } set { _shearModulus12.Equation = value; } }
        [Browsable(false)]
        public EquationContainer ShearModulus12 { get { return _shearModulus12; } set { _shearModulus12 = value; } }
        //
        [DisplayName("G13\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString ShearModulus13Eq { get { return _shearModulus13.Equation; } set { _shearModulus13.Equation = value; } }
        [Browsable(false)]
        public EquationContainer ShearModulus13 { get { return _shearModulus13; } set { _shearModulus13 = value; } }
        //
        [DisplayName("G23\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString ShearModulus23Eq { get { return _shearModulus23.Equation; } set { _shearModulus23.Equation = value; } }
        [Browsable(false)]
        public EquationContainer ShearModulus23 { get { return _shearModulus23; } set { _shearModulus23 = value; } }


        // Constructors                                                                                                             
        public ElasticLaminaDataPoint()
            : base(0)
        {
            _youngsModulus1 = new EquationContainer(typeof(StringPressureConverter), 0);
            _youngsModulus2 = new EquationContainer(typeof(StringPressureConverter), 0);
            _poissonsRatio12 = new EquationContainer(typeof(StringDoubleConverter), 0);
            _shearModulus12 = new EquationContainer(typeof(StringPressureConverter), 0);
            _shearModulus13 = new EquationContainer(typeof(StringPressureConverter), 0);
            _shearModulus23 = new EquationContainer(typeof(StringPressureConverter), 0);
        }
        public ElasticLaminaDataPoint(EquationContainer[] data)
            : base(data[CaeModel.ElasticLamina.TemperatureId])
        {
            _youngsModulus1 = data[CaeModel.ElasticLamina.YoungsModulus1Id];
            _youngsModulus2 = data[CaeModel.ElasticLamina.YoungsModulus2Id];
            _poissonsRatio12 = data[CaeModel.ElasticLamina.PoissonsRatio12Id];
            _shearModulus12 = data[CaeModel.ElasticLamina.ShearModulus12Id];
            _shearModulus13 = data[CaeModel.ElasticLamina.ShearModulus13Id];
            _shearModulus23 = data[CaeModel.ElasticLamina.ShearModulus23Id];
        }
    }
}
