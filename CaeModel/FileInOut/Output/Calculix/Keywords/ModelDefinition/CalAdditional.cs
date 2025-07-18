using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeMesh;
using System.Xml.Linq;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    public class CalAdditional : CalTitle
    {
        // Variables                                                                                                                
        private List<CalculixKeyword> _additionalKeywords;


        // Properties                                                                                                               
        public List<CalculixKeyword> AdditionalKeywords { get { return _additionalKeywords; } }


        // Constructor                                                                                                              
        public CalAdditional(string title, List<CalculixKeyword> additionalKeywords)
            : base(title, "")
        {
            _additionalKeywords = additionalKeywords;
        }


        // Methods                                                                                                                  
        public override string GetDataString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var keyword in _additionalKeywords)
            {
                sb.Append(keyword.GetKeywordString());
                sb.Append(keyword.GetDataString());
            }
            _data = sb.ToString();
            //
            return base.GetDataString();
        }
    }
}


