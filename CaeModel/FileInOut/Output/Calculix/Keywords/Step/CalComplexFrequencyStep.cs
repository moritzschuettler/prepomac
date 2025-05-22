using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalComplexFrequencyStep : CalculixKeyword
    {
        // Variables                                                                                                                
        private ComplexFrequencyStep _step;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalComplexFrequencyStep(ComplexFrequencyStep step)
        {
            _step = step;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Complex frequency, Coriolis{0}", Environment.NewLine);
        }
        public override string GetDataString()
        {
            string data = string.Format("{0}{1}", _step.NumOfFrequencies, Environment.NewLine);
            if (!_step.RunAnalysis) data += "*No Analysis" + Environment.NewLine;
            return data;
        }
    }
}
