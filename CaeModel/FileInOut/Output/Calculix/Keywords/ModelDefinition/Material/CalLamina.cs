using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalLamina : CalculixKeyword
    {
 // Variables    
 private Lamina _lamina;
    private bool _temperatureDependent;


     // Properties      


   // Constructor      
     public CalLamina(Lamina lamina, bool temperatureDependent)
        {
    _lamina = lamina;
  _temperatureDependent = temperatureDependent;
        }


   // Methods 
        public override string GetKeywordString()
        {
      return string.Format("*Elastic, TYPE=ORTHO{0}", Environment.NewLine);
    }
      public override string GetDataString()
  {
    StringBuilder sb = new StringBuilder();
     EquationContainer[][] data = _lamina.LaminaTemp;
//
        for (int i = 0; i < data.Length; i++)
   {
     if (_temperatureDependent)
        {
    // E1, E2, E3 (=E2), Nu12, Nu13 (=Nu12), Nu23 (calculated), G12, G13, G23, Temperature
 // For lamina: E3 = E2, Nu13 = Nu12
        // Nu23 = Nu12 * E2 / E2 = Nu12
 double e1 = data[i][0].Value;
    double e2 = data[i][1].Value;
  double nu12 = data[i][2].Value;
         double g12 = data[i][3].Value;
        double g13 = data[i][4].Value;
     double g23 = data[i][5].Value;
 double temp = data[i][6].Value;
 //
       // E3 = E2 (transverse isotropy assumption)
     double e3 = e2;
     // Nu13 = Nu12 (transverse isotropy assumption)
    double nu13 = nu12;
 // Nu23 for transverse isotropy
          double nu23 = nu12;
//
        // CalculiX limit: 8 values per line
                    // Line 1: E1, E2, E3, Nu12, Nu13, Nu23, G12, G13
                    // Line 2: G23, Temperature
                    sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7},{8}{9}, {10}{11}",
                        e1.ToCalculiX16String(),   // E1
                        e2.ToCalculiX16String(),   // E2
                        e3.ToCalculiX16String(),   // E3 = E2
                        nu12.ToCalculiX16String(), // Nu12
                        nu13.ToCalculiX16String(), // Nu13 = Nu12
                        nu23.ToCalculiX16String(), // Nu23 = Nu12
                        g12.ToCalculiX16String(),  // G12
                        g13.ToCalculiX16String(),  // G13
                        Environment.NewLine,
                        g23.ToCalculiX16String(),  // G23
                        temp.ToCalculiX16String(), // Temperature
                        Environment.NewLine);
   }
     else
  {
        // E1, E2, E3 (=E2), Nu12, Nu13 (=Nu12), Nu23 (calculated), G12, G13, G23
     // For lamina: E3 = E2, Nu13 = Nu12
    // Nu23 = Nu12 * E2 / E2 = Nu12
   double e1 = data[i][0].Value;
 double e2 = data[i][1].Value;
         double nu12 = data[i][2].Value;
       double g12 = data[i][3].Value;
          double g13 = data[i][4].Value;
        double g23 = data[i][5].Value;
 //
    // E3 = E2 (transverse isotropy assumption)
   double e3 = e2;
       // Nu13 = Nu12 (transverse isotropy assumption)
  double nu13 = nu12;
   // Nu23 for transverse isotropy
       double nu23 = nu12;
   //
        // CalculiX limit: 8 values per line
                    // Line 1: E1, E2, E3, Nu12, Nu13, Nu23, G12, G13
                    // Line 2: G23
                    sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7},{8}{9}{10}",
                        e1.ToCalculiX16String(),   // E1
                        e2.ToCalculiX16String(),   // E2
                        e3.ToCalculiX16String(),   // E3 = E2
                        nu12.ToCalculiX16String(), // Nu12
                        nu13.ToCalculiX16String(), // Nu13 = Nu12
                        nu23.ToCalculiX16String(), // Nu23 = Nu12
                        g12.ToCalculiX16String(),  // G12
                        g13.ToCalculiX16String(),  // G13
                        Environment.NewLine,
                        g23.ToCalculiX16String(),  // G23
                        Environment.NewLine);
            break;
           }
 }
     return sb.ToString();
        }
    }
}
