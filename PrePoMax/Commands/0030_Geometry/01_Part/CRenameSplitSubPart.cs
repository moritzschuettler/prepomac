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
    class CRenameSplitSubPart : PreprocessCommand
    {
        // Variables
        private string _compoundPartName;
        private string _oldSubPartName;
        private string _newSubPartName;


        // Constructor
        public CRenameSplitSubPart(string compoundPartName, string oldSubPartName, string newSubPartName)
            : base("Rename sub-part")
        {
            _compoundPartName = compoundPartName;
            _oldSubPartName = oldSubPartName;
            _newSubPartName = newSubPartName;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.RenameSplitSubPart(_compoundPartName, _oldSubPartName, _newSubPartName);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Comment("Renaming a sub-part of a split compound is not yet available in the Python API.");
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _compoundPartName + ": " + _oldSubPartName + " -> " + _newSubPartName;
        }
    }
}
