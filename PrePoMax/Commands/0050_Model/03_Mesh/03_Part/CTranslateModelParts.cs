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
using System.Runtime.Serialization;


namespace PrePoMax.Commands
{
    [PmxType(10099)]
    class CTranslateModelParts : PreprocessCommand 
    {
        // Variables                                                                                                                
        [PmxKey(0)] private string[] _partNames;
        [PmxKey(1)] private double[] _translateVector;
        [PmxKey(2)] private int _numberOfCopies;


        // Constructor                                                                                                              
        public CTranslateModelParts(string[] partNames, double[] translateVector, int numberOfCopies)
            : base("Translate mesh parts")
        {
            _partNames = partNames;
            _translateVector = translateVector;
            _numberOfCopies = numberOfCopies;
        }
        public CTranslateModelParts(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_partNames":
                        _partNames = (string[])entry.Value;
                        break;
                    case "_translateVector":
                        _translateVector = (double[])entry.Value;
                        break;
                    case "_copy":   // Compatibility for version v.2.4.3
                        _numberOfCopies = (bool)entry.Value ? 1 : -1;
                        break;
                    case "_numberOfCopies":
                        _numberOfCopies = (int)entry.Value;
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.TranslateModelParts(_partNames, _translateVector, _numberOfCopies);
            return true;
        }
        public override void ToPython(PythonJournalWriter writer)
        {
            writer.Statement(writer.Call("pmx.model.parts", "translate", PythonValueFormatter.List(_partNames),
                                         PythonValueFormatter.List(_translateVector),
                                         "number_of_copies=" + PythonValueFormatter.Format(Math.Max(_numberOfCopies, 0))));
            writer.CountEmittedCommand();
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
    }
}
