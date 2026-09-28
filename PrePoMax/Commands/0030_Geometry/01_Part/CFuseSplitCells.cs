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
    class CFuseSplitCells : PreprocessCommand
    {
        // Variables
        private string _compoundPartName;
        private string[] _cellNames;


        // Constructor
        public CFuseSplitCells(string compoundPartName, string[] cellNames)
            : base("Fuse cells")
        {
            _compoundPartName = compoundPartName;
            _cellNames = cellNames.ToArray();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.FuseSplitCells(_compoundPartName, _cellNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "fuse_split_cells",
                                         writer.Value(_compoundPartName), writer.Value(_cellNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _compoundPartName + ": " + string.Join(", ", _cellNames);
        }
    }
}
