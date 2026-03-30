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
using CaeMesh;
using System.ComponentModel;
using CaeGlobals;
using DynamicTypeDescriptor;
using System.IO;

namespace PrePoMax
{
    [Serializable]
    public class ExportHistoryResultSetFileProperties : ExportFileProperties
    {
        // Variables                                                                                                                
        public static readonly string DefaultFileName = "HistoryOutput.csv";
        public static readonly string DefaultFilter = "Comma separated values (*.csv)|*.csv";
        public static readonly string[] DefaultDelimiters = new string[] {",", ";", ":"};
        private string[] _historyOutputNames;
        private string _delimiter;


        // Properties                                                                                                               
        public string[] HistoryOutputNames { get { return _historyOutputNames; } set { _historyOutputNames = value; } }
        public string Delimiter { get { return _delimiter; } set { _delimiter = value; } }


        // Constructors                                                                                                             
        public ExportHistoryResultSetFileProperties()
            : base(DefaultFileName, DefaultFilter)
        {
            _historyOutputNames = null;
            _delimiter = DefaultDelimiters[0];
        }


        // Methods                                                                                                                  
    }
}
