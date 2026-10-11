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
using PrePoMax;
using CaeModel;
using CaeGlobals;
using PrePoMax.Python.Journal;
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExportResultHistoryOutputToCsv : PostprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private ExportHistoryResultSetFileProperties _properties;


        // Properties                                                                                                               
        public string FileName
        {
            get { return _properties.FileName; }
            set { _properties.FileName = value; }
        }


        // Constructor                                                                                                              
        public CExportResultHistoryOutputToCsv(ExportHistoryResultSetFileProperties properties)
            :base("Export result history output")
        {
            _properties = properties.DeepClone();
            _properties.FileName = Tools.GetLocalPath(_properties.FileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            ExportHistoryResultSetFileProperties properties = _properties.DeepClone();
            properties.FileName = Tools.GetGlobalPath(_properties.FileName);
            receiver.ExportResultHistoryOutput(properties.FileName, properties.HistoryOutputNames, properties.Delimiter);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.results.history_outputs", "export_to_csv",
                                         writer.OutputPath(_properties.FileName),
                                         PythonValueFormatter.List(_properties.HistoryOutputNames),
                                         "delimiter=" + PythonValueFormatter.Text(_properties.Delimiter)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _properties.FileName;
        }
    }
}
