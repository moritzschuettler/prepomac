// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

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
    internal class CalSectionPrint : CalculixKeyword
    {
        // Variables                                                                                                                
        protected readonly SectionHistoryOutput _sectionHistoryOutput;
        protected int _outputFrequency;


        // Properties                                                                                                               
        public bool OutputFrequency { get; set; }


        // Constructors                                                                                                             
        public CalSectionPrint(CalSectionPrint calSectionPrint)
            : this(calSectionPrint._sectionHistoryOutput, calSectionPrint._outputFrequency)
        {
        }
        public CalSectionPrint(SectionHistoryOutput sectionHistoryOutput, int outputFrequency)
        {
            _sectionHistoryOutput = sectionHistoryOutput;
            _outputFrequency = outputFrequency;
            OutputFrequency = false;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string regionName = _sectionHistoryOutput.RegionName;
            string name = _sectionHistoryOutput.Name;
            string frequency = OutputFrequency ? ", Frequency=" + _outputFrequency : "";
            //
            return string.Format("*Section print, Surface={0}, Name={1}{2}{3}", regionName, name, frequency, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _sectionHistoryOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
