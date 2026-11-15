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
    [PmxType(10095)]
    class CScaleModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private double[] _scaleCenter;
        [PmxKey(2)] private double[] _scaleFactors;
        [PmxKey(3)] private bool _copy;

        // Constructor                                                                                                              
        public CScaleModelParts(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
            : base("Scale mesh parts")
        {
            _partNames = partNames;
            _scaleCenter = scaleCenter;
            _scaleFactors = scaleFactors;
            _copy = copy;
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.ScaleModelParts(_partNames, _scaleCenter, _scaleFactors, _copy);
            return true;
        }

        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.parts", "scale", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.List(_scaleCenter),
                                         PythonValueFormatter.List(_scaleFactors),
                                         "copy=" + PythonValueFormatter.Format(_copy)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
