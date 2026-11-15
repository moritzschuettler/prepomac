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
    [PmxType(3122)]
    internal enum OpKeywordEnum
    {
        None,
        Boundary,
        Cload,
        Dload,
        Cflux,
        Dflux,
        Film,
        Radiate,
        Temperature
    }
    //
    [PmxType(3123)]
    internal enum OpTypeEnum
    {
        None,
        Mod,
        New
    }
    //
    [PmxType(3124)]
    internal class CalOpParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        [PmxKey(0)] private OpKeywordEnum _opKeyword;
        [PmxKey(1)] private OpTypeEnum _opType;


        // Properties                                                                                                               


        // Constructor                                                                                                              
        public CalOpParameter(OpKeywordEnum opKeyword, OpTypeEnum opType)
        {
            if (opKeyword == OpKeywordEnum.None || opType == OpTypeEnum.None)
                throw new NotSupportedException();
            //
            _opKeyword = opKeyword;
            _opType = opType;
        }


        // Methods                                                                                                                  
        public override string GetKeywordString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("*{0}, op={1}{2}", _opKeyword, _opType, Environment.NewLine);
            return sb.ToString();
        }
        public override string GetDataString()
        {
            return "";
        }
    }
}
