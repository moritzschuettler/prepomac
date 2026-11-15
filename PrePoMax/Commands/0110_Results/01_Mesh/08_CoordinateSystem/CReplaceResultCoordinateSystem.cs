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
    [PmxType(10253)]
    class CReplaceResultCoordinateSystem : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string _oldCoordinateSystemName;
        [PmxKey(1)] private CoordinateSystem _newCoordinateSystem;

        // Constructor                                                                                                              
        public CReplaceResultCoordinateSystem(string oldCoordinateSystemName, CoordinateSystem newCoordinateSystem)
            : base("Edit result coordinate system")
        {
            _oldCoordinateSystemName = oldCoordinateSystemName;
            _newCoordinateSystem = newCoordinateSystem.DeepClone();
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ReplaceResultCoordinateSystem(_oldCoordinateSystemName, _newCoordinateSystem.DeepClone());
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            PythonResultEmitter.CoordinateSystem(writer, _oldCoordinateSystemName, _newCoordinateSystem);
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + _oldCoordinateSystemName + ", " + _newCoordinateSystem.ToString();
        }
    }
}
