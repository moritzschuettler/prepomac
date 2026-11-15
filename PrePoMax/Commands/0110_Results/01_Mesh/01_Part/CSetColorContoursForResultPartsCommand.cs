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
    [PmxType(10239)]
    class CSetColorContoursForResultPartsCommand : PostprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private bool _colorContours;


        // Constructor                                                                                                              
        public CSetColorContoursForResultPartsCommand(string[] partNames, bool resultRepresentation)
            : base("Set result representation for result parts")
        {
            _partNames = partNames;
            _colorContours = resultRepresentation;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetResultRepresentationForResultParts(_partNames, _colorContours);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call(PythonResultEmitter.Parts, "set_representation", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.Format(_colorContours)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            string onOff = _colorContours ? "On" : "Off";
            return base.GetCommandString() + onOff + ": " + GetArrayAsString(_partNames);
        }
    }
}
