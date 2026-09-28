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
    class CGroupSplitCells : PreprocessCommand
    {
        // Variables
        private string _compoundPartName;
        private string[] _cellNames;
        private string _subPartName;


        // Constructor
        public CGroupSplitCells(string compoundPartName, string[] cellNames, string subPartName)
            : base("Group cells")
        {
            _compoundPartName = compoundPartName;
            _cellNames = cellNames.ToArray();
            _subPartName = subPartName;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.GroupSplitCells(_compoundPartName, _cellNames, _subPartName);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "group_split_cells",
                                         writer.Value(_compoundPartName), writer.Value(_cellNames),
                                         writer.Value(_subPartName)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _compoundPartName + ": " + string.Join(", ", _cellNames) + " -> " +
                   _subPartName;
        }
    }
}
