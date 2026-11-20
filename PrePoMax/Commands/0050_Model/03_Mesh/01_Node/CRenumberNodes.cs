// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(10085)]
    class CRenumberNodes : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private int _startNodeId;


        // Constructor                                                                                                              
        public CRenumberNodes(int startNodeId)
            : base("Renumber nodes")
        {
            _startNodeId = startNodeId;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RenumberNodes(_startNodeId);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.nodes", "renumber", PythonValueFormatter.Format(_startNodeId)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _startNodeId;
        }
    }
}
