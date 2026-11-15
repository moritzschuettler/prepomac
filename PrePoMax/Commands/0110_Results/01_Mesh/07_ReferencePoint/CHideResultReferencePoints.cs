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
    [Serializable]
    [PmxType(10245)]
    class CHideResultReferencePoints : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _referencePoints;


        // Constructor                                                                                                              
        public CHideResultReferencePoints(string[] referencePoints)
            : base("Hide result reference points")
        {
            _referencePoints = referencePoints;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.HideResultReferencePoints(_referencePoints);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call(PythonResultEmitter.ReferencePoints, "hide", PythonValueFormatter.List(_referencePoints)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_referencePoints);
        }
    }
}
