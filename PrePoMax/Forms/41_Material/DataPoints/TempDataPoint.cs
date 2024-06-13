using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using CaeGlobals;
using CaeModel;

namespace PrePoMax
{
    [Serializable]
    public class TempDataPoint
    {
        // Variables                                                                                                                
        private EquationContainer _temperature;


        // Properties                                                                                                               
        [DisplayName("Temperature\n[?]")]
        [TypeConverter(typeof(EquationTemperatureFromConverter))]
        public EquationString TemperatureEq { get { return _temperature.Equation; } set { _temperature.Equation = value; } }
        //
        [Browsable(false)]
        public EquationContainer Temperature { get { return _temperature; } set { _temperature = value; } }


        // Constructors                                                                                                             
        public TempDataPoint()
            :this(0)
        {
        }
        public TempDataPoint(double temperature)
        {
            _temperature = new EquationContainer(typeof(StringTemperatureConverter), temperature); // must not use FROM converter
        }
        public TempDataPoint(EquationContainer temperature)
        {
            _temperature = temperature;
        }
    }
}
