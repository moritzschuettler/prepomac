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
    [Serializable]
    [PmxType(3143)]
    internal class CalNodePrint : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] protected readonly NodalHistoryOutput _nodalHistoryOutput;
        [PmxKey(1)] protected string _regionName;
        [PmxKey(2)] protected int _outputFrequency;


        // Properties                                                                                                               
        public string RegionName { get { return _regionName; } }
        [field: PmxKey(3)] public bool OutputFrequency { get; set; }


        // Constructors                                                                                                             
        public CalNodePrint(CalNodePrint calNodePrint)
            : this(calNodePrint._nodalHistoryOutput, calNodePrint._regionName, calNodePrint._outputFrequency)
        {
        }
        public CalNodePrint(FeModel model, NodalHistoryOutput nodalHistoryOutput, int outputFrequency)
        {
            _nodalHistoryOutput = nodalHistoryOutput;   // set this first
            //
            _regionName = "";
            if (_nodalHistoryOutput.RegionType == CaeGlobals.RegionTypeEnum.NodeSetName)
                _regionName += _nodalHistoryOutput.RegionName;
            else if (_nodalHistoryOutput.RegionType == CaeGlobals.RegionTypeEnum.SurfaceName)
                _regionName += model.Mesh.Surfaces[_nodalHistoryOutput.RegionName].NodeSetName;
            else if (_nodalHistoryOutput.RegionType == CaeGlobals.RegionTypeEnum.Selection)
            { }
            else throw new NotSupportedException();
            //
            _outputFrequency = outputFrequency;
            OutputFrequency = false;
        }
        public CalNodePrint(NodalHistoryOutput nodalHistoryOutput, string regionName, int outputFrequency)
        {
            _nodalHistoryOutput = nodalHistoryOutput;
            _regionName = regionName;
            _outputFrequency = outputFrequency;
            OutputFrequency = false;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            string regionName = ", Nset=" + _regionName;
            string totals = "";
            if (_nodalHistoryOutput.TotalsType == TotalsTypeEnum.Yes) totals = ", Totals=Yes";
            else if (_nodalHistoryOutput.TotalsType == TotalsTypeEnum.Only) totals = ", Totals=Only";
            string global = _nodalHistoryOutput.Global ? ", Global=Yes" : "";
            string frequency = OutputFrequency && _outputFrequency > 0 ? ", Frequency=" + _outputFrequency : "";
            //
            return string.Format("*Node print{0}{1}{2}{3}{4}", regionName, totals, global, frequency, Environment.NewLine);
        }
        public override string GetDataString()
        {
            return string.Format("{0}{1}", _nodalHistoryOutput.Variables.ToString(), Environment.NewLine);
        }
    }
}
