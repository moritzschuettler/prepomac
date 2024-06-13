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
    internal class CalContactFile : CalculixKeyword
    {
        // Variables                                                                                                                
        private ContactFieldOutput _contactFieldOutput;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalContactFile(ContactFieldOutput contactFieldOutput)
        {
            _contactFieldOutput = contactFieldOutput;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string lastIterations = _contactFieldOutput.LastIterations ? ", Last iterations" : "";
            string contactElements = _contactFieldOutput.ContactElements ? ", Contact elements" : "";
            //
            return string.Format("*Contact file{0}{1}{2}", lastIterations, contactElements, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _contactFieldOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
