using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalResetParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private ResetStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalResetParameter(ResetStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Reset{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
