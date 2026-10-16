// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Text;
using CaeGlobals;
using CaeModel;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalElasticLamina : CalculixKeyword
    {
        // Variables                                                                                                                
        private ElasticLamina _elasticLamina;
        private bool _temperatureDependent;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalElasticLamina(ElasticLamina elasticLamina, bool temperatureDependent)
        {
            _elasticLamina = elasticLamina;
            _temperatureDependent = temperatureDependent;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Elastic, Type=Engineering constants{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            EquationContainer[][] data = _elasticLamina.LaminaConstantsTemp;
            //
            for (int i = 0; i < data.Length; i++)
            {
                // Complete the out-of-plane constants of a transversely isotropic lamina:
                // E3 = E2, v13 = v12 and v23 = E2 / (2 G23) - 1.
                double e2 = data[i][ElasticLamina.YoungsModulus2Id].Value;
                double v23 = ElasticLamina.GetPoissonsRatio23(e2, data[i][ElasticLamina.ShearModulus23Id].Value);
                sb.Append(data[i][ElasticLamina.YoungsModulus1Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(e2.ToCalculiX16String()).Append(", ");
                sb.Append(e2.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticLamina.PoissonsRatio12Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticLamina.PoissonsRatio12Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(v23.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticLamina.ShearModulus12Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticLamina.ShearModulus13Id].Value.ToCalculiX16String());
                sb.Append(Environment.NewLine);
                sb.Append(data[i][ElasticLamina.ShearModulus23Id].Value.ToCalculiX16String());
                if (_temperatureDependent)
                {
                    sb.Append(", ");
                    sb.Append(data[i][ElasticLamina.TemperatureId].Value.ToCalculiX16String());
                }
                sb.Append(Environment.NewLine);
                if (!_temperatureDependent) break;
            }
            return sb.ToString();
        }
    }
}
