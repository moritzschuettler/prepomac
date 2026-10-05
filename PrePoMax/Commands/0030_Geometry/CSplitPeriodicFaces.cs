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
    class CSplitPeriodicFaces : PreprocessCommand
    {
        // Variables
        private string[] _partNames;
        private int _numOfFaces;


        // Constructor
        public CSplitPeriodicFaces(string[] partNames, int numOfFaces)
            : base("Split periodic faces")
        {
            _partNames = partNames.ToArray();
            _numOfFaces = numOfFaces;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.SplitPeriodicFaces(_partNames, _numOfFaces);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "split_periodic_faces", writer.Value(_partNames),
                _numOfFaces == 2 ? null : "number_of_faces=" + PythonValueFormatter.Format(_numOfFaces)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + string.Join(", ", _partNames);
        }
    }
}
