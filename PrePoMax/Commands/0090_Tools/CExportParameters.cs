using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;
using CaeResults;


namespace PrePoMax.Commands
{
    [Serializable]
    class CExportParameters : PreprocessCommand, IExportFileCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor                                                                                                              
        public CExportParameters(string fileName)
            : base("Export parameters")
        {
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportParametersToFile(Tools.GetGlobalPath(_fileName));
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
