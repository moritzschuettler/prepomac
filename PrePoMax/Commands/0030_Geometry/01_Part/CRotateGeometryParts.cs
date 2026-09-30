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
    class CRotateGeometryParts : PreprocessCommand
    {
        // Variables
        private string[] _partNames;
        private double[] _rotateCenter;
        private double[] _rotateAxis;
        private double _rotateAngle;
        private int _numberOfCopies;


        // Constructor
        public CRotateGeometryParts(string[] partNames, double[] rotateCenter, double[] rotateAxis, double rotateAngle,
                                    int numberOfCopies)
            : base("Rotate geometry parts")
        {
            _partNames = partNames;
            _rotateCenter = rotateCenter;
            _rotateAxis = rotateAxis;
            _rotateAngle = rotateAngle;
            _numberOfCopies = numberOfCopies;
        }


        // Methods
        public override bool Execute(Controller receiver)
        {
            receiver.RotateGeometryParts(_partNames, _rotateCenter, _rotateAxis, _rotateAngle, _numberOfCopies);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.geometry.parts", "rotate", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.List(_rotateCenter),
                                         PythonValueFormatter.List(_rotateAxis),
                                         PythonValueFormatter.Number(_rotateAngle * 180 / Math.PI),
                                         "number_of_copies=" + PythonValueFormatter.Format(Math.Max(_numberOfCopies, 0))));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
