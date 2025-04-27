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
    class CImportParameters : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _fileName;


        // Constructor                                                                                                              
        public CImportParameters(string fileName)
            : base("Import parameters")
        {
            _fileName = fileName;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ImportParametersFromFile(_fileName);
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
