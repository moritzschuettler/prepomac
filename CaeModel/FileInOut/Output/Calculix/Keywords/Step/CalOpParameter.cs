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
    [Serializable]
    internal enum OpTypeEnum
    {
        None,
        Mod,
        New
    }
    //
    [Serializable]
    internal class CalOpParameter : CalculixKeyword
    {
        // Variables                                                                                                                
        private OpKeywordEnum _opKeyword;
        private OpTypeEnum _opType;


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
