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
                                                                         // E1, E2, E3, Nu12, Nu13, Nu23, G12, G13, G23, Temperature


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
                _engineeringConstantsTemp[i] = new EquationContainer[10]; // E1, E2, E3, Nu12, Nu13, Nu23, G12, G13, G23, T
                _engineeringConstantsTemp[i][0] = new EquationContainer(typeof(StringPressureConverter), value[i][0]); // E1
                _engineeringConstantsTemp[i][1] = new EquationContainer(typeof(StringPressureConverter), value[i][1]); // E2
                _engineeringConstantsTemp[i][2] = new EquationContainer(typeof(StringPressureConverter), value[i][2]); // E3
                _engineeringConstantsTemp[i][3] = new EquationContainer(typeof(StringDoubleConverter), value[i][3]);   // Nu12
                _engineeringConstantsTemp[i][4] = new EquationContainer(typeof(StringDoubleConverter), value[i][4]);   // Nu13
                // Backward compat: old files (9 values) had no Nu23; migrate using Nu23 = Nu13*E2/E3
                double e3d = value[i][2], nu13d = value[i][4], e2d = value[i][1];
                double nu23 = value[i].Length >= 10 ? value[i][5]
                    : (e3d != 0 ? nu13d * e2d / e3d : 0.0);
                _engineeringConstantsTemp[i][5] = new EquationContainer(typeof(StringDoubleConverter), nu23);          // Nu23
                int g = value[i].Length >= 10 ? 6 : 5; // index offset for G values in old vs new format
                _engineeringConstantsTemp[i][6] = new EquationContainer(typeof(StringPressureConverter), value[i][g]);     // G12
                _engineeringConstantsTemp[i][7] = new EquationContainer(typeof(StringPressureConverter), value[i][g + 1]); // G13
                _engineeringConstantsTemp[i][8] = new EquationContainer(typeof(StringPressureConverter), value[i][g + 2]); // G23
                _engineeringConstantsTemp[i][9] = new EquationContainer(typeof(StringTemperatureConverter), value[i][g + 3]); // T
            }
            SetEngineeringConstantsTemp(_engineeringConstantsTemp, checkEquation);
        }
        private void SetEngineeringConstantsTemp(EquationContainer[][] value, bool checkEquation = true)
        {
            // Migrate old 9-element rows (Nu23 was absent) to new 10-element format
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i].Length == 9)
                {
                    EquationContainer[] row = new EquationContainer[10];
                    row[0] = value[i][0]; row[1] = value[i][1]; row[2] = value[i][2];
                    row[3] = value[i][3]; row[4] = value[i][4];
                    
                    // Calculate Nu23 = Nu13 * E2 / E3
                    double nu23 = 0.0;
                    try
                    {
                        double e2v = value[i][1].Value;
                        double e3v = value[i][2].Value;
                        double nu13v = value[i][4].Value;
                        nu23 = (e3v != 0) ? (nu13v * e2v / e3v) : 0.0;
                    }
                    catch
                    {
                        nu23 = 0.0;
                    }
     
                    row[5] = new EquationContainer(typeof(StringDoubleConverter), nu23);
                    row[6] = value[i][5]; row[7] = value[i][6]; row[8] = value[i][7]; row[9] = value[i][8];
                    value[i] = row;
                }
            }
            EquationContainer.SetAndCheck(ref _engineeringConstantsTemp, value,
                new Func<double, double>[] { CheckPositive, CheckPositive, CheckPositive,
                    null, null, null, CheckPositive, CheckPositive, CheckPositive, null },
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
                for (int j = 0; j < 10; j++)
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
