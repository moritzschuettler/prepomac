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
using CaeJob;
using PrePoMax.Forms;

namespace PrePoMax.Commands
{
    [Serializable]
    class CAddDeafaultJob : PreprocessCommand
    {
        // Variables                                                                                                                
        private string _defaultName;

        // Constructor                                                                                                              
        public CAddDeafaultJob()
            : base("Add default analysis")
        {
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            _defaultName = receiver.AddDefaultJob();
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            // An analysis already existed, so the command added none and the replay has nothing to do
            if (_defaultName == null) writer.Comment(Name + ": an analysis already existed, so none was added");
            else writer.Statement(writer.Call("pmx.jobs", "add", PythonValueFormatter.Text(_defaultName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            string data = _defaultName != null ? _defaultName : "";
            return base.GetCommandString() + data;
        }
    }
}
