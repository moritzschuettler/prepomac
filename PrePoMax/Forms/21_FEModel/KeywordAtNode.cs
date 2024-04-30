using FileInOut.Output.Calculix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [Serializable]
    class KeywordAtNode
    {
        public CalculixKeyword Keyword;
        public string Data;
        public int NumOfLines;
    }
}
