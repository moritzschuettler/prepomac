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
using CaeMesh;
using CaeGlobals;
using System.IO;


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
        public string FileExtension { get { return Path.GetExtension(_fileName).ToLower(); } }


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
