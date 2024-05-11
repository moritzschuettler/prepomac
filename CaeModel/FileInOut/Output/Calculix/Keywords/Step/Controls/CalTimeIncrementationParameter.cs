using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using System.Data.Common;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalTimeIncrementationParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private TimeIncrementationStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalTimeIncrementationParameter(TimeIncrementationStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Parameters=Time incrementation{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}{10}",
                            "", _parameter.IR, _parameter.IP, _parameter.IC,
                            _parameter.IL, _parameter.IG, "", _parameter.IA, "", "", Environment.NewLine);
            sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}{8}",
                            _parameter.Df.ToCalculiX16String(), _parameter.DC.ToCalculiX16String(),
                            _parameter.DB.ToCalculiX16String(), _parameter.DA.ToCalculiX16String(),
                            "", "", _parameter.DD.ToCalculiX16String(), "", Environment.NewLine);
            return sb.ToString();
        }
    }
}
