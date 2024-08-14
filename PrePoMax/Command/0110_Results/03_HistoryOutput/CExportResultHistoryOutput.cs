using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeGlobals;
using CaeResults;

namespace PrePoMax.Commands
{
    [Serializable]
    class CExportResultHistoryOutput : PostprocessCommand
    {
        // Variables                                                                                                                
        private HistoryResultSetExporter _historyResultSetExporter;


        // Constructor                                                                                                              
        public CExportResultHistoryOutput(HistoryResultSetExporter historyResultSetExporter)
            :base("Export result history output")
        {
            _historyResultSetExporter = historyResultSetExporter.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportResultHistoryOutput(_historyResultSetExporter.DeepClone());
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _historyResultSetExporter.FileName;
        }
    }
}
