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
    class CMirrorGeometryParts : PreprocessCommand
    {
        // Variables
        private string[] _partNames;
        private double[] _mirrorPoint;
        private double[] _mirrorDirection;
        private bool _copy;


        // Constructor
        public CMirrorGeometryParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy)
            : base("Mirror geometry parts")
        {
            _partNames = partNames;
            _mirrorPoint = mirrorPoint;
            _mirrorDirection = mirrorDirection;
            _copy = copy;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.MirrorGeometryParts(_partNames, _mirrorPoint, _mirrorDirection, _copy);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "mirror", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.List(_mirrorPoint),
                                         PythonValueFormatter.List(_mirrorDirection),
                                         "copy=" + PythonValueFormatter.Format(_copy)));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
