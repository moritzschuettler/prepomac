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
    internal class AbqContactOutput : CalculixKeyword
    {
        // Variables                                                                                                                
        private ContactFieldOutput _contactFieldOutput;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public AbqContactOutput(ContactFieldOutput contactFieldOutput)
        {
            _contactFieldOutput = contactFieldOutput;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Contact output{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            string variables = _contactFieldOutput.Variables.ToString();
            variables = variables.Replace("CDIS", "CDISP");
            variables = variables.Replace("CSTR", "CSTRESS");
            return string.Format("{0}{1}", variables, Environment.NewLine);
        }
    }
}
