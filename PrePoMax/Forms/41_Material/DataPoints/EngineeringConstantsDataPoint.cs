using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using DynamicTypeDescriptor;
using CaeGlobals;

namespace PrePoMax
{
    [Serializable]
    public class EngineeringConstantsDataPoint : TempDataPoint
    {
        // Variables      
        private EquationContainer _e1;
        private EquationContainer _e2;
        private EquationContainer _e3;
        private EquationContainer _nu12;
        private EquationContainer _nu13;
        private EquationContainer _nu23;
        private EquationContainer _g12;
        private EquationContainer _g13;
        private EquationContainer _g23;


        // Properties        
        [DisplayName("E1\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString E1Eq { get { return _e1.Equation; } set { _e1.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer E1 { get { return _e1; } set { _e1 = value; } }
        //
        [DisplayName("E2\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString E2Eq { get { return _e2.Equation; } set { _e2.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer E2 { get { return _e2; } set { _e2 = value; } }
        //
        [DisplayName("E3\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString E3Eq { get { return _e3.Equation; } set { _e3.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer E3 { get { return _e3; } set { _e3 = value; } }
        //
        [DisplayName("Nu12\n[?]")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu12Eq { get { return _nu12.Equation; } set { _nu12.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer Nu12 { get { return _nu12; } set { _nu12 = value; } }
        //
        [DisplayName("Nu13\n[?]")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu13Eq { get { return _nu13.Equation; } set { _nu13.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer Nu13 { get { return _nu13; } set { _nu13 = value; } }
        //
        [DisplayName("Nu23\n[?]")]
        [TypeConverter(typeof(EquationDoubleConverter))]
        public EquationString Nu23Eq { get { return _nu23.Equation; } set { _nu23.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer Nu23 { get { return _nu23; } set { _nu23 = value; } }
        //
        [DisplayName("G12\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString G12Eq { get { return _g12.Equation; } set { _g12.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer G12 { get { return _g12; } set { _g12 = value; } }
        //
        [DisplayName("G13\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString G13Eq { get { return _g13.Equation; } set { _g13.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer G13 { get { return _g13; } set { _g13 = value; } }
        //
        [DisplayName("G23\n[?]")]
        [TypeConverter(typeof(EquationPressureFromConverter))]
        public EquationString G23Eq { get { return _g23.Equation; } set { _g23.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer G23 { get { return _g23; } set { _g23 = value; } }


        // Constructors         
        public EngineeringConstantsDataPoint()
             : base(0)
        {
            _e1 = new EquationContainer(typeof(StringPressureConverter), 0); // must not use FROM converter
            _e2 = new EquationContainer(typeof(StringPressureConverter), 0);
            _e3 = new EquationContainer(typeof(StringPressureConverter), 0);
            _nu12 = new EquationContainer(typeof(StringDoubleConverter), 0);
            _nu13 = new EquationContainer(typeof(StringDoubleConverter), 0);
            _nu23 = new EquationContainer(typeof(StringDoubleConverter), 0);
            _g12 = new EquationContainer(typeof(StringPressureConverter), 0);
            _g13 = new EquationContainer(typeof(StringPressureConverter), 0);
            _g23 = new EquationContainer(typeof(StringPressureConverter), 0);
        }
        public EngineeringConstantsDataPoint(EquationContainer e1, EquationContainer e2, EquationContainer e3,
            EquationContainer nu12, EquationContainer nu13, EquationContainer nu23,
            EquationContainer g12, EquationContainer g13, EquationContainer g23,
            EquationContainer temperature)
            : base(temperature)
        {
            _e1 = e1;
            _e2 = e2;
            _e3 = e3;
            _nu12 = nu12;
            _nu13 = nu13;
            _nu23 = nu23;
            _g12 = g12;
            _g13 = g13;
            _g23 = g23;
        }
    }
}
