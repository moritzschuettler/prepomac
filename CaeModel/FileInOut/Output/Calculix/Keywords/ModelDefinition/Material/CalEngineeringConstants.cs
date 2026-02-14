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
    internal class CalEngineeringConstants : CalculixKeyword
    {
        // Variables           
        private EngineeringConstants _engineeringConstants;
        private bool _temperatureDependent;


        // Properties     


        // Constructor        
        public CalEngineeringConstants(EngineeringConstants engineeringConstants, bool temperatureDependent)
        {
            _engineeringConstants = engineeringConstants;
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
            EquationContainer[][] data = _engineeringConstants.EngineeringConstantsTemp;
            //
            for (int i = 0; i < data.Length; i++)
            {
                if (_temperatureDependent)
                {
                    // E1, E2, E3, Nu12, Nu13, Nu23 (calculated), G12, G13, G23, Temperature
                    // Nu23 = Nu13 * E2 / E3 (for orthotropic symmetry)
                    double nu23 = data[i][4].Value * data[i][1].Value / data[i][2].Value;
                    //
                    sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}{10}",
                    data[i][0].Value.ToCalculiX16String(), // E1
                     data[i][1].Value.ToCalculiX16String(), // E2
                             data[i][2].Value.ToCalculiX16String(), // E3
                          data[i][3].Value.ToCalculiX16String(), // Nu12
                                    data[i][4].Value.ToCalculiX16String(), // Nu13
                    nu23.ToCalculiX16String(),      // Nu23
                           data[i][5].Value.ToCalculiX16String(), // G12
                           data[i][6].Value.ToCalculiX16String(), // G13
                      data[i][7].Value.ToCalculiX16String(), // G23
                  data[i][8].Value.ToCalculiX16String(), // Temperature
                        Environment.NewLine);
                }
                else
                {
                    // E1, E2, E3, Nu12, Nu13, Nu23 (calculated), G12, G13, G23
                    // Nu23 = Nu13 * E2 / E3 (for orthotropic symmetry)
                    double nu23 = data[i][4].Value * data[i][1].Value / data[i][2].Value;
                    //
                    sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}{9}",
                         data[i][0].Value.ToCalculiX16String(), // E1
                     data[i][1].Value.ToCalculiX16String(), // E2
                    data[i][2].Value.ToCalculiX16String(), // E3
                  data[i][3].Value.ToCalculiX16String(), // Nu12
                        data[i][4].Value.ToCalculiX16String(), // Nu13
                   nu23.ToCalculiX16String(),         // Nu23
               data[i][5].Value.ToCalculiX16String(), // G12
                          data[i][6].Value.ToCalculiX16String(), // G13
                    data[i][7].Value.ToCalculiX16String(), // G23
                         Environment.NewLine);
                    break;
                }
            }
            return sb.ToString();
        }
    }
}
