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
    internal class CalContactParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private ContactStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalContactParameter(ContactStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Parameters=Contact{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}, {1}, {2}, {3}{4}",
                            _parameter.Delcon.ToCalculiX16String(), _parameter.Alea.ToCalculiX16String(),
                            _parameter.Kscalemax, _parameter.Itf2f, Environment.NewLine);
            return sb.ToString();
        }
    }
}
