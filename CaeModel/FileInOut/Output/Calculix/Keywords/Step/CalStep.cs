using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileInOut.Output.Calculix
{
    public abstract class CalStep : CalculixKeyword
    {
        public bool OutputSolver { get; set; }
        public bool OutputNoAnalysis { get; set; }
    }
}
