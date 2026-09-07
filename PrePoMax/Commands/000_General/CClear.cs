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


namespace PrePoMax.Commands
{
    [Serializable]
    class CClear : PreprocessCommand
    {
        // Variables                                                                                                                


        // Constructor                                                                                                              
        public CClear()
            : base("Clear")
        {
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.Clear();
            return true;
        }

        // A journal is replayed into a fresh instance, which is already empty, so a clear at the very
        // start needs nothing; anywhere else it would discard what the script built and there is no
        // API form for that, because replacing the model rebuilds the command collection
        public override void ToPython(PythonJournalWriter writer)
        {
            if (writer.CommandIndex == 0)
            {
                writer.Comment("New model - the journal starts from an empty model");
                writer.CountEmittedCommand();
            }
            else writer.Unsupported(this, "a model can only be cleared before the journal starts");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "All";
        }
    }
}
