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
    internal class CalElasticEngineeringConstants : CalculixKeyword
    {
        // Variables                                                                                                                
        private ElasticEngineeringConstants _elasticEngineeringConstants;
        private bool _temperatureDependent;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalElasticEngineeringConstants(ElasticEngineeringConstants elasticEngineeringConstants,
                                              bool temperatureDependent)
        {
            _elasticEngineeringConstants = elasticEngineeringConstants;
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
            EquationContainer[][] data = _elasticEngineeringConstants.EngineeringConstantsTemp;
            //
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i][ElasticEngineeringConstants.YoungsModulus1Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.YoungsModulus2Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.YoungsModulus3Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.PoissonsRatio12Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.PoissonsRatio13Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.PoissonsRatio23Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.ShearModulus12Id].Value.ToCalculiX16String()).Append(", ");
                sb.Append(data[i][ElasticEngineeringConstants.ShearModulus13Id].Value.ToCalculiX16String());
                sb.Append(Environment.NewLine);
                sb.Append(data[i][ElasticEngineeringConstants.ShearModulus23Id].Value.ToCalculiX16String());
                if (_temperatureDependent)
                {
                    sb.Append(", ");
                    sb.Append(data[i][ElasticEngineeringConstants.TemperatureId].Value.ToCalculiX16String());
                }
                sb.Append(Environment.NewLine);
                if (!_temperatureDependent) break;
            }
            return sb.ToString();
        }
    }
}
