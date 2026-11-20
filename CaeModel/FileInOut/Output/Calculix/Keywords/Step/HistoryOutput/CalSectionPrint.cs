// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [PmxType(3144)]
    internal class CalSectionPrint : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected readonly SectionHistoryOutput _sectionHistoryOutput;
        [PmxKey(1)] protected int _outputFrequency;


        // Properties                                                                                                               
        [field: PmxKey(2)] public bool OutputFrequency { get; set; }


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
