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
    internal class CalLineSearchParameter : CalculixKeyword
    {
        // Variables
        private LineSearchStepControlParameter _parameter;


        // Properties


        // Constructor
        public CalLineSearchParameter(LineSearchStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Parameters=Line search{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(", {0}, {1},,{2}",
                            _parameter.SlsMax.ToCalculiX16String(),
                            _parameter.SlsMin.ToCalculiX16String(),
                            Environment.NewLine);
            return sb.ToString();
        }
    }
}
