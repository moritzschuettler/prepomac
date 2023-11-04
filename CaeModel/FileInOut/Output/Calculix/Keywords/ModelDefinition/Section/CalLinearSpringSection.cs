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
    internal class CalLinearSpringSection : CalculixKeyword
    {
        // Variables                                                                                                                
        private LinearSpringSection _linearSpringSection;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalLinearSpringSection(LinearSpringSection linearSpringSection)
        {
            _linearSpringSection = linearSpringSection;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Spring, Elset={0}{1}", _linearSpringSection.RegionName, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}{1}", _linearSpringSection.Direction, Environment.NewLine);
            sb.AppendFormat("{0}{1}", _linearSpringSection.Stiffness.Value.ToCalculiX16String(true), Environment.NewLine);
            return sb.ToString();
        }
    }
}
