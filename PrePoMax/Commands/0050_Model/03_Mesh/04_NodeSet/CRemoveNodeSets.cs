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
    class CRemoveNodeSets : PreprocessCommand
    {
        // Variables                                                                                                                
        private string[] _nodeSetNames;


        // Constructor                                                                                                              
        public CRemoveNodeSets(string[] nodeSetNames)
            : base("Remove node sets")
        {
            _nodeSetNames = nodeSetNames;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RemoveNodeSets(_nodeSetNames);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonModelItemEmitter.Remove(writer, "pmx.model.node_sets", _nodeSetNames);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_nodeSetNames);
        }
    }
}
