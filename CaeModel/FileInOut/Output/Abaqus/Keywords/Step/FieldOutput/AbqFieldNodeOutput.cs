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
    internal class AbqFieldNodeOutput : CalNodeFile
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqFieldNodeOutput(CalNodeFile calNodeFile)
            : base(calNodeFile)
        {
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string frequency = "";
            if (_outputFrequency > 0) frequency = ", Frequency=" + _outputFrequency;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Output, Field{0}{1}", frequency, Environment.NewLine);
            sb.AppendFormat("*Node Output{0}", Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            /*
            RF = 1,
            U = 2,
            PU = 4,
            V = 8,
            // Thermal
            NT = 16,
            PNT = 32,
            RFL = 64,
            */
            string variables = _nodalFieldOutput.Variables.ToString();
            //variables = variables.Replace("PU", "");
            //variables = variables.Replace("PNT", "");
            //variables = variables.Replace("RF", "");
            //
            return string.Format("{0}{1}", variables, Environment.NewLine);
        }
    }
}
