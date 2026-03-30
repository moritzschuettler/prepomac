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

namespace CaeResults
{
    [Serializable]
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
    public class HistoryResultFilter
    {
        // Variables                                                                                                                
        public static string Row = "Row";
        public static string Column = "Column";
        public static string Rows = "Rows";
        public static string Columns = "Columns";
        //
        protected HistoryResultFilterTypeEnum _type;
        protected string _option;


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
