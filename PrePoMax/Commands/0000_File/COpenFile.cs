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
using PrePoMax.Python.Journal;
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
            string fileName = receiver.GetFileNameToOpen("Replace file: " + _fileName);
            if (fileName != null) _fileName = Tools.GetLocalPath(fileName);
            return Execute(receiver);
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            // A result file is opened beside the model, so the command history it belongs to goes on
            if (FileExtension != ".pmx" && FileExtension != ".pmh")
            {
                writer.Statement(writer.Call("pmx.results", "open", writer.Path(Tools.GetGlobalPath(_fileName))));
                writer.CountEmittedCommand();
            }
            else if (writer.IsAtSessionStart)
            {
                writer.Comment("Session start: " + Tools.GetGlobalPath(_fileName));
                writer.SessionStartFileName = Tools.GetGlobalPath(_fileName);
                writer.CountSessionStartCommand();
            }
            else writer.Unsupported(this, "opening a model file replaces the command history, so a journal " +
                                          "cannot replay it");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _fileName;
        }
    }
}
