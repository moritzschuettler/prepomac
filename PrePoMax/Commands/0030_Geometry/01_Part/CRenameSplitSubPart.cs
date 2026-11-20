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
    [PmxType(10045)]
    class CRenameSplitSubPart : PreprocessCommand
    {
        // Variables
        [PmxKey(0)] private string _compoundPartName;
        [PmxKey(1)] private string _oldSubPartName;
        [PmxKey(2)] private string _newSubPartName;


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
            writer.Statement(writer.Call("pmx.model.geometry.parts", "rename_split_sub_part",
                                         writer.Value(_compoundPartName), writer.Value(_oldSubPartName),
                                         writer.Value(_newSubPartName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _compoundPartName + ": " + _oldSubPartName + " -> " + _newSubPartName;
        }
    }
}
