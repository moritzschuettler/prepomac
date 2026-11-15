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
using CaeGlobals;

namespace CaeResults
{
    [Serializable]
    [PmxType(5039)]
    public enum HistoryResultFilterTypeEnum
    {
        None,
        Minimum,
        Maximum,
        Sum,
        Average
    }
    //
    [Serializable]
    [PmxType(5040)]
    public class HistoryResultFilter
    {
        // Variables                                                                                                                
        public static string Row = "Row";
        public static string Column = "Column";
        public static string Rows = "Rows";
        public static string Columns = "Columns";
        //
        [PmxKey(0)] protected HistoryResultFilterTypeEnum _type;
        [PmxKey(1)] protected string _option;


        // Properties                                                                                                               
        public HistoryResultFilterTypeEnum Type { get { return _type; } set { _type = value; } }
        public string Option { get { return _option; } set { _option = value; } }


        // Constructors                                                                                                             
        public HistoryResultFilter()
        {
            _type = HistoryResultFilterTypeEnum.None;
            _option = null;
        }
    }
}
