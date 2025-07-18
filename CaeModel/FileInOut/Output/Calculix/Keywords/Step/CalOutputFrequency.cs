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
    internal class CalOutputFrequency : CalculixKeyword
    {
        // Variables                                                                                                                
        private int _outputFrequency;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalOutputFrequency(int outputFrequency)
        {
            _outputFrequency = outputFrequency;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string frequency;
            if (IsDefault()) frequency = ", Frequency=1";
            else frequency = ", Frequency=" + _outputFrequency;
            //
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*Output{0}{1}", frequency, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
        public bool IsDefault()
        {
            return _outputFrequency == int.MinValue;
        }
    }
}
