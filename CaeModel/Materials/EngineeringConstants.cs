using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using System.Runtime.Serialization;
using CaeResults;
using FileInOut.Output.Calculix;

namespace CaeModel
{
    [Serializable]
    public class EngineeringConstants : MaterialProperty, ISerializable
    {
        // Variables         
        private EquationContainer[][] _engineeringConstantsTemp;         //ISerializable
                                                                         // E1, E2, E3, Nu12, Nu13, G12, G13, G23, Temperature


        // Properties       
        public EquationContainer[][] EngineeringConstantsTemp
        {
            get { return _engineeringConstantsTemp; }
            set { SetEngineeringConstantsTemp(value); }
        }


        // Constructors    
        public EngineeringConstants(double[][] engineeringConstantsTemp)
        {
            SetEngineeringConstantsTemp(engineeringConstantsTemp, false);
        }
        public EngineeringConstants(EquationContainer[][] engineeringConstantsTemp)
        {
            SetEngineeringConstantsTemp(engineeringConstantsTemp, false);
        }
        public EngineeringConstants(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_engineeringConstantsTemp":
                        if (entry.Value is double[][] values)
                            SetEngineeringConstantsTemp(values, false);
                        else
                            SetEngineeringConstantsTemp((EquationContainer[][])entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods
        private void SetEngineeringConstantsTemp(double[][] value, bool checkEquation = true)
        {
            _engineeringConstantsTemp = new EquationContainer[value.Length][];
            //
            for (int i = 0; i < value.Length; i++)
            {
                _engineeringConstantsTemp[i] = new EquationContainer[9]; // E1, E2, E3, Nu12, Nu13, G12, G13, G23, T
                _engineeringConstantsTemp[i][0] = new EquationContainer(typeof(StringPressureConverter), value[i][0]); // E1
                _engineeringConstantsTemp[i][1] = new EquationContainer(typeof(StringPressureConverter), value[i][1]); // E2
                _engineeringConstantsTemp[i][2] = new EquationContainer(typeof(StringPressureConverter), value[i][2]); // E3
                _engineeringConstantsTemp[i][3] = new EquationContainer(typeof(StringDoubleConverter), value[i][3]); // Nu12
                _engineeringConstantsTemp[i][4] = new EquationContainer(typeof(StringDoubleConverter), value[i][4]); // Nu13
                _engineeringConstantsTemp[i][5] = new EquationContainer(typeof(StringPressureConverter), value[i][5]); // G12
                _engineeringConstantsTemp[i][6] = new EquationContainer(typeof(StringPressureConverter), value[i][6]); // G13
                _engineeringConstantsTemp[i][7] = new EquationContainer(typeof(StringPressureConverter), value[i][7]); // G23
                _engineeringConstantsTemp[i][8] = new EquationContainer(typeof(StringTemperatureConverter), value[i][8]); // T
            }
            SetEngineeringConstantsTemp(_engineeringConstantsTemp, checkEquation);
        }
        private void SetEngineeringConstantsTemp(EquationContainer[][] value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _engineeringConstantsTemp, value,
                new Func<double, double>[] { CheckPositive, CheckPositive, CheckPositive,
  null, null, CheckPositive, CheckPositive, CheckPositive, null },
            checkEquation);
        }
        //
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException(_positive);
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            for (int i = 0; i < _engineeringConstantsTemp.Length; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    _engineeringConstantsTemp[i][j].CheckEquation();
                }
            }
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_engineeringConstantsTemp", _engineeringConstantsTemp, typeof(EquationContainer[][]));
        }
    }
}
