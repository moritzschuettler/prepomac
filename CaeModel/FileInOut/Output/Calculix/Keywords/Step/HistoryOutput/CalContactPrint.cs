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
    internal class CalContactPrint : CalculixKeyword
    {
        // Variables                                                                                                                
        protected readonly ContactHistoryOutput _contactHistoryOutput;
        protected string _masterSurfaceName;
        protected string _slaveSurfaceName;
        protected string _masterNodeSetName;
        protected string _slaveNodeSetName;
        protected int _outputFrequency;


        // Properties                                                                                                               
        public bool OutputFrequency { get; set; }


        // Constructor                                                                                                              
        public CalContactPrint(CalContactPrint calContactPrint)
            : this(calContactPrint._contactHistoryOutput, calContactPrint._masterSurfaceName, calContactPrint._slaveSurfaceName,
                   calContactPrint._masterNodeSetName, calContactPrint._slaveNodeSetName, calContactPrint._outputFrequency)
        {
        }
        public CalContactPrint(ContactHistoryOutput contactHistoryOutput, string masterSurfaceName, string slaveSurfaceName,
                               string masterNodeSetName, string slaveNodeSetName, int outputFrequency)
        {
            _contactHistoryOutput = contactHistoryOutput;
            _masterSurfaceName = masterSurfaceName;
            _slaveSurfaceName = slaveSurfaceName;
            _masterNodeSetName = masterNodeSetName;
            _slaveNodeSetName = slaveNodeSetName;
            _outputFrequency = outputFrequency;
            OutputFrequency = false;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string totals = "";
            if (_contactHistoryOutput.TotalsType == TotalsTypeEnum.Yes) totals = ", Totals=Yes";
            else if (_contactHistoryOutput.TotalsType == TotalsTypeEnum.Only) totals = ", Totals=Only";
            string masterSlave = "";
            if (_contactHistoryOutput.Variables.HasFlag(ContactHistoryVariable.CF))
                masterSlave = ", Master=" + _masterSurfaceName + ", Slave=" + _slaveSurfaceName;
            string frequency = OutputFrequency ? ", Frequency=" + _outputFrequency : "";
            //
            return string.Format("*Contact print{0}{1}{2}{3}", totals, masterSlave, frequency, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _contactHistoryOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
