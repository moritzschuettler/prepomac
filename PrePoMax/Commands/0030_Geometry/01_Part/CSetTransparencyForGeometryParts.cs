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
    [PmxType(10051)]
    class CSetTransparencyForGeometryParts : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private byte _alpha;


        // Constructor                                                                                                              
        public CSetTransparencyForGeometryParts(string[] partNames, byte alpha)
            : base("Set transparency for geometry parts")
        {
            _partNames = partNames;
            _alpha = alpha;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetTransparencyForGeometryParts(_partNames, _alpha);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonPartPropertiesEmitter.RefuseAlphaBelowMinimum(_alpha, "the transparency set for the parts", "set_opacity");
            writer.Statement(writer.Call("pmx.model.geometry.parts", "set_opacity", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.Format(_alpha)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Alpha = " + _alpha.ToString() + ": " + GetArrayAsString(_partNames);
        }
    }
}
