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
    internal class CalFieldParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private FieldStepControlParameter _parameter;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalFieldParameter(FieldStepControlParameter parameter)
        {
            _parameter = parameter;
        }


        // Methods                                                                                                                  
        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Controls, Parameters=Field{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            string q0a;
            if (double.IsNaN(_parameter.Q0a)) q0a = "";
            else q0a = _parameter.Q0a.ToCalculiX16String();
            //
            string qua;
            if (double.IsNaN(_parameter.Qua)) qua = "";
            else qua = _parameter.Qua.ToCalculiX16String();
            //
            sb.AppendFormat("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}{8}",
                            _parameter.Rna.ToCalculiX16String(), _parameter.Cna.ToCalculiX16String(), q0a, qua,
                            _parameter.Rpa.ToCalculiX16String(), _parameter.Ea.ToCalculiX16String(),
                            _parameter.Cea.ToCalculiX16String(), _parameter.Rla.ToCalculiX16String(), Environment.NewLine);
            return sb.ToString();
        }
    }
}
