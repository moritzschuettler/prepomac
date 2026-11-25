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
    [PmxType(10247)]
    class CReplaceResultReferencePoint : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldReferencePointName;
        [PmxKey(1)] private FeReferencePoint _newReferencePoint;

        // Constructor                                                                                                              
        public CReplaceResultReferencePoint(string oldReferencePointName, FeReferencePoint newReferencePoint)
            : base("Edit result reference point")
        {
            _oldReferencePointName = oldReferencePointName;
            _newReferencePoint = newReferencePoint.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultReferencePoint(_oldReferencePointName, _newReferencePoint.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonResultEmitter.ReferencePoint(writer, _oldReferencePointName, _newReferencePoint);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldReferencePointName + ", " + _newReferencePoint.ToString();
        }
    }
}
