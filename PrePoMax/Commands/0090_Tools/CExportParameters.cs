using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PrePoMax;
using CaeModel;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Commands
{
    [Serializable]
    class CExportParameters : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Constructor                                                                                                              
        public CExportParameters(string fileName)
            : base("Export parameters")
        {
            _fileName = fileName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ExportParametersToFile(_fileName);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
