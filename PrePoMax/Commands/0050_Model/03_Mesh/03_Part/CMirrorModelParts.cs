// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using PrePoMax.Python.Journal;
using CaeMesh;
using CaeModel;
using PrePoMax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;


namespace PrePoMax.Commands
{
    [PmxType(10090)]
    class CMirrorModelParts : PreprocessCommand
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private double[] _mirrorPoint;
        [PmxKey(2)] private double[] _mirrorDirection;
        [PmxKey(3)] private bool _copy;


        // Constructor                                                                                                              
        public CMirrorModelParts(string[] partNames, double[] mirrorPoint, double[] mirrorDirection, bool copy)
            : base("Mirror mesh parts")
        {
            _partNames = partNames;
            _mirrorPoint = mirrorPoint;
            _mirrorDirection = mirrorDirection;
            _copy = copy;
        }
        public CMirrorModelParts(SerializationInfo info, StreamingContext context)
           : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_partNames":
                        _partNames = (string[])entry.Value;
                        break;
                    case "_mirrorPoint":
                        _mirrorPoint = (double[])entry.Value;
                        break;
                    case "_mirrorDirection":
                        _mirrorDirection = (double[])entry.Value;
                        break;
                    case "_copy":
                        _copy = (bool)entry.Value;
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.MirrorModelParts(_partNames, _mirrorPoint, _mirrorDirection, _copy);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.parts", "mirror", PythonValueFormatter.List(_partNames),
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
