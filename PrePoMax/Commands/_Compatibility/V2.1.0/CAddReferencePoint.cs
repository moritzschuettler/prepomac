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
    [PmxType(10278)]
    class CAddReferencePoint : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private FeReferencePoint _referencePoint;


        // Constructor                                                                                                              
        public CAddReferencePoint(FeReferencePoint referencePoint)
            : base("Add reference point")
        {
            _referencePoint = referencePoint.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.AddModelReferencePoint(_referencePoint.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonReferencePointEmitter.Add(writer, _referencePoint);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _referencePoint.ToString();
        }
    }
}
