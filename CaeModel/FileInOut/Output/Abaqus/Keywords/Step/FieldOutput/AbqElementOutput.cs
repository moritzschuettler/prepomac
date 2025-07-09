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
    internal class AbqElementOutput : CalculixKeyword
    {
        // Variables                                                                                                                
        private ElementFieldOutput _elementFieldOutput;
        private int _outputFrequency;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqElementOutput(ElementFieldOutput elementFieldOutput, int outputFrequency)
        {
            _elementFieldOutput = elementFieldOutput;
            _outputFrequency = outputFrequency;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string frequency = "";
            if (_outputFrequency > 0) frequency = ", Frequency=" + _outputFrequency;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Output, Field{0}{1}", frequency, Environment.NewLine);
            sb.AppendFormat("*Element Output{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            /*
            S = 1,
            PHS = 2,
            E = 4,
            ME = 8,
            PEEQ = 16,
            ENER = 32,
            // Thermal
            HFL = 64,
            // Error
            ERR = 128,
            HER = 256,
            ZZS = 512,
            //
            SDV = 1073741824,
            */
            string variables = _elementFieldOutput.GetVariablesString();
            //variables = variables.Replace("ME", "");
            //variables = variables.Replace("ERR", "");
            //variables = variables.Replace("HER", "");
            //variables = variables.Replace("ZZS", "");
            variables = variables.Replace("NOE", "");
            //
            return string.Format("{0}{1}", variables, Environment.NewLine);
        }
    }
}
