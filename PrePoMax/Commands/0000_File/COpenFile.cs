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
    class COpenFile : PreprocessCommand, IFileCommand, ICommandWithDialog
    {
        // Variables                                                                                                                
        private string _fileName;
        private string _parameters;


        // Properties                                                                                                               
        public string FileName { get { return _fileName; } set { _fileName = value; } }


        // Constructor                                                                                                              
        public COpenFile(string fileName, string parameters)
            :base("Open file")
        {
            _fileName = Tools.GetLocalPath(fileName);
            _parameters = parameters;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.Open(Tools.GetGlobalPath(_fileName), _parameters);
            return true;
        }
        // ICommandWithDialog
        public bool ExecuteWithDialog(Controller receiver)
        {
            string fileName = receiver.GetFileNameToOpen();
            if (fileName != null) _fileName = Tools.GetLocalPath(fileName);
            return Execute(receiver);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
