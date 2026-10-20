// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
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


namespace PrePoMax.Commands
{
    [Serializable]
    class CImportRosetteCurvesFromStep : PreprocessCommand
    {
        // Variables
        private string _rosetteName;
        private string _fileName;
        private double _chordTolerance;


        // Constructor
        public CImportRosetteCurvesFromStep(string rosetteName, string fileName, double chordTolerance)
            : base("Import rosette curves from STEP")
        {
            _rosetteName = rosetteName;
            _fileName = fileName;
            _chordTolerance = chordTolerance;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.ImportRosetteCurvesFromStep(_rosetteName, _fileName, _chordTolerance);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _rosetteName + ", " + _fileName;
        }
    }
}
