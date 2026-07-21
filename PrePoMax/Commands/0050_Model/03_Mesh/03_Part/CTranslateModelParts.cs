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
using System.Runtime.Serialization;


namespace PrePoMax.Commands
{
    [Serializable]
    class CTranslateModelParts : PreprocessCommand, ISerializable 
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private double[] _translateVector;
        private int _numberOfCopies;


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
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            // Using typeof() works also for null fields
            info.AddValue("_partNames", _partNames, typeof(string[]));
            info.AddValue("_translateVector", _translateVector, typeof(double[]));
            info.AddValue("_numberOfCopies", _numberOfCopies, typeof(int));
        }
    }
}
