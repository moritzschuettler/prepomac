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
    class CFuseBodies : PreprocessCommand
    {
        // Variables
        private string _compoundPartName;
        private string[] _bodyNames;


        // Constructor
        public CFuseBodies(string compoundPartName, string[] bodyNames)
            : base("Fuse bodies")
        {
            _compoundPartName = compoundPartName;
            _bodyNames = bodyNames.ToArray();
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.FuseBodies(_compoundPartName, _bodyNames);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "fuse_bodies",
                                         writer.Value(_compoundPartName), writer.Value(_bodyNames)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _compoundPartName + ": " + string.Join(", ", _bodyNames);
        }
    }
}
