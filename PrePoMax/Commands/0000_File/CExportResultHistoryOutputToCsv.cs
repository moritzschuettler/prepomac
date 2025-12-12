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
        public override string GetCommandString()
        {
            return base.GetCommandString() + _properties.FileName;
        }
    }
}
