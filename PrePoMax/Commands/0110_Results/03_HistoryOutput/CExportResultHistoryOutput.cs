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


        // Properties                                                                                                               
        public string FileName
        {
            get { return _historyResultSetExporter.FileName; }
            set { _historyResultSetExporter.FileName = value; }
        }


        // Constructor                                                                                                              
        public CExportResultHistoryOutput(HistoryResultSetExporter historyResultSetExporter)
            :base("Export result history output")
        {
            _historyResultSetExporter = historyResultSetExporter.DeepClone();
            _historyResultSetExporter.FileName = Tools.GetLocalPath(_historyResultSetExporter.FileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            HistoryResultSetExporter historyResultSetExporter = _historyResultSetExporter.DeepClone();
            historyResultSetExporter.FileName = Tools.GetGlobalPath(_historyResultSetExporter.FileName);
            receiver.ExportResultHistoryOutput(historyResultSetExporter);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _historyResultSetExporter.FileName;
        }
    }
}
