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
    [PmxType(3073)]
    internal class CalPreTensionSection : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _surfaceName;
        [PmxKey(1)] private int _nodeId;
        [PmxKey(2)] bool _autoCompute;
        [PmxKey(3)] double _x;
        [PmxKey(4)] double _y;
        [PmxKey(5)] double _z;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalPreTensionSection(string surfaceName, int nodeId)
        {
            _surfaceName = surfaceName;
            _nodeId = nodeId;
            _autoCompute = true;
        }
        public CalPreTensionSection(string surfaceName, int nodeId, double x, double y, double z)
        {
            _surfaceName = surfaceName;
            _nodeId = nodeId;
            _autoCompute = false;
            _x = x;
            _y = y;
            _z = z;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            return string.Format("*Pre-tension section, Surface={0}, Node={1}{2}", _surfaceName, _nodeId, Environment.NewLine);
        }
        public override string GetDataString()
        {
            if (_autoCompute) return "";
            else return string.Format("{0}, {1}, {2}{3}", _x.ToCalculiX16String(), _y.ToCalculiX16String(),
                                      _z.ToCalculiX16String(), Environment.NewLine);
        }
    }
}
