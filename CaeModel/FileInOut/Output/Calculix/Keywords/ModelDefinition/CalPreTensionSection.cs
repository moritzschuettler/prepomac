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
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    internal class CalPreTensionSection : CalculixKeyword
    {
        // Variables                                                                                                                
        private string _surfaceName;
        private int _nodeId;
        bool _autoCompute;
        double _x;
        double _y;
        double _z;


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
