using System;
using CaeModel;
using CaeGlobals;

namespace PrePoMax.Commands
{
    [Serializable]
    class CImportDiscreteOrientationsXml : PreprocessCommand, IImportFileCommand
    {
        // Variables
        private string _layupName;
        private string _fileName;


        // Properties
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor
        public CImportDiscreteOrientationsXml(string layupName, string fileName)
            : base("Import discrete orientations XML")
        {
            _layupName = layupName;
            _fileName = Tools.GetLocalPath(fileName);
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ImportDiscreteOrientationsXml(_layupName, Tools.GetGlobalPath(_fileName));
            return true;
        }

        public override string GetCommandString()
        {
            return base.GetCommandString() + _layupName + " | " + _fileName;
        }
    }
}
