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
    class CTranslateGeometryParts : PreprocessCommand
    {
        // Variables
        private string[] _partNames;
        private double[] _translateVector;
        private int _numberOfCopies;


        // Constructor
        public CTranslateGeometryParts(string[] partNames, double[] translateVector, int numberOfCopies)
            : base("Translate geometry parts")
        {
            _partNames = partNames;
            _translateVector = translateVector;
            _numberOfCopies = numberOfCopies;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.TranslateGeometryParts(_partNames, _translateVector, _numberOfCopies);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "translate", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.List(_translateVector),
                                         "number_of_copies=" + PythonValueFormatter.Format(Math.Max(_numberOfCopies, 0))));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
