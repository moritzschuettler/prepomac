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
    public class Lamina : MaterialProperty, ISerializable
    {
 // Variables       
        private EquationContainer[][] _laminaTemp;       //ISerializable
        // E1, E2, Nu12, G12, G13, G23, Temperature


  // Properties   
 public EquationContainer[][] LaminaTemp
        {
      get { return _laminaTemp; }
      set { SetLaminaTemp(value); }
      }


        // Constructors        
        public Lamina(double[][] laminaTemp)
      {
     SetLaminaTemp(laminaTemp, false);
      }
        public Lamina(EquationContainer[][] laminaTemp)
  {
            SetLaminaTemp(laminaTemp, false);
      }
      public Lamina(SerializationInfo info, StreamingContext context)
          : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
 switch (entry.Name)
      {
          case "_laminaTemp":
    if (entry.Value is double[][] values)
        SetLaminaTemp(values, false);
        else
   SetLaminaTemp((EquationContainer[][])entry.Value, false);
break;
         default:
      break;
          }
       }
   }


        // Methods   
        private void SetLaminaTemp(double[][] value, bool checkEquation = true)
 {
       _laminaTemp = new EquationContainer[value.Length][];
   //
            for (int i = 0; i < value.Length; i++)
   {
        _laminaTemp[i] = new EquationContainer[7]; // E1, E2, Nu12, G12, G13, G23, T
         _laminaTemp[i][0] = new EquationContainer(typeof(StringPressureConverter), value[i][0]); // E1
     _laminaTemp[i][1] = new EquationContainer(typeof(StringPressureConverter), value[i][1]); // E2
       _laminaTemp[i][2] = new EquationContainer(typeof(StringDoubleConverter), value[i][2]); // Nu12
     _laminaTemp[i][3] = new EquationContainer(typeof(StringPressureConverter), value[i][3]); // G12
        _laminaTemp[i][4] = new EquationContainer(typeof(StringPressureConverter), value[i][4]); // G13
     _laminaTemp[i][5] = new EquationContainer(typeof(StringPressureConverter), value[i][5]); // G23
      _laminaTemp[i][6] = new EquationContainer(typeof(StringTemperatureConverter), value[i][6]); // T
    }
        SetLaminaTemp(_laminaTemp, checkEquation);
        }
        private void SetLaminaTemp(EquationContainer[][] value, bool checkEquation = true)
        {
    EquationContainer.SetAndCheck(ref _laminaTemp, value, 
      new Func<double, double>[] { CheckPositive, CheckPositive, null, 
       CheckPositive, CheckPositive, CheckPositive, null },
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
     for (int i = 0; i < _laminaTemp.Length; i++)
            {
                for (int j = 0; j < 7; j++)
                {
         _laminaTemp[i][j].CheckEquation();
           }
      }
        }
     // ISerialization
   public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
    // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_laminaTemp", _laminaTemp, typeof(EquationContainer[][]));
        }
    }
}
