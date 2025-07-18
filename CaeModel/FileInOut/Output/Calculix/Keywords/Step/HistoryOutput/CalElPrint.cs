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
    internal class CalElPrint : CalculixKeyword
    {
        // Variables                                                                                                                
        protected readonly ElementHistoryOutput _elementHistoryOutput;
        protected int _outputFrequency;


        // Properties                                                                                                               
        public bool OutputFrequency { get; set; }
        public bool OutputGlobal { get; set; }


        // Constructor                                                                                                              
        public CalElPrint(CalElPrint calElPrint)
            : this(calElPrint._elementHistoryOutput, calElPrint._outputFrequency)
        {
        }
        public CalElPrint(ElementHistoryOutput elementHistoryOutput, int outputFrequency)
        {
            _elementHistoryOutput = elementHistoryOutput;
            _outputFrequency = outputFrequency;
            //
            OutputGlobal = true;
            OutputFrequency = false;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string regionName = ", Elset=" + _elementHistoryOutput.RegionName;
            string totals = "";
            if (_elementHistoryOutput.TotalsType == TotalsTypeEnum.Yes) totals = ", Totals=Yes";
            else if (_elementHistoryOutput.TotalsType == TotalsTypeEnum.Only) totals = ", Totals=Only";
            string global = OutputGlobal && _elementHistoryOutput.Global ? ", Global=Yes" : "";
            string frequency = OutputFrequency ? ", Frequency=" + _outputFrequency : "";
            //
            return string.Format("*El print{0}{1}{2}{3}{4}", regionName, totals, global, frequency, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _elementHistoryOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
