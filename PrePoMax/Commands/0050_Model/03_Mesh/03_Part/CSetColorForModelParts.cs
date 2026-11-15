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
    [PmxType(10096)]
    class CSetColorForModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private System.Drawing.Color _color;


        // Constructor                                                                                                              
        public CSetColorForModelParts(string[] partNames, System.Drawing.Color color)
            : base("Set color for model parts")
        {
            _partNames = partNames;
            _color = color;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.SetColorForModelParts(_partNames, _color);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            PythonPartPropertiesEmitter.RefuseAlphaBelowMinimum(_color.A, "the color set for the parts", "set_color");
            writer.Statement(writer.Call("pmx.model.parts", "set_color", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.Channels(_color)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + "Color = " + _color.ToString() + ": " + GetArrayAsString(_partNames);
        }
    }
}
