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
    internal class CalLoad : CalculixKeyword
    {
        // Variables                                                                                                                
        private OpTypeEnum _opType;


        // Properties                                                                                                               
        public OpTypeEnum OpType { get { return _opType; } set { _opType = value; } }


        // Constructor                                                                                                              
        public CalLoad()
        {
            _opType = OpTypeEnum.None;
        }


        // Methods                                                                                                                  
        public string OpTypeString()
        {
            if (_opType == OpTypeEnum.None) return "";
            else if (_opType == OpTypeEnum.New) return ", op=New";
            else if (_opType == OpTypeEnum.Mod) return ", op=Mod";
            else throw new NotSupportedException();
        }
        public override string GetKeywordString()
        {
            return "";
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}