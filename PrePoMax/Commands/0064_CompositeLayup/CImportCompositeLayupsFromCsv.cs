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
    class CImportCompositeLayupsFromCsv : PreprocessCommand, IImportFileCommand
    {
        // Variables
        private string _fileName;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CImportCompositeLayupsFromCsv(string fileName)
            : base("Import composite layups from CSV")
        {
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ImportCompositeLayupsFromCsv(Tools.GetGlobalPath(_fileName));
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
