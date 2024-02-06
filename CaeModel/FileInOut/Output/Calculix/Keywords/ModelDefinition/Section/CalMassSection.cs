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
    internal class CalMassSection : CalculixKeyword
    {
        // Variables                                                                                                                
        private MassSectionData _massSectionData;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalMassSection(MassSectionData massSectionData)
        {
            _massSectionData = massSectionData;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Mass, Elset={0}{1}", _massSectionData.RegionName, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}{1}", _massSectionData.Mass.ToCalculiX16String(true), Environment.NewLine);
            return sb.ToString();
        }
    }
}
