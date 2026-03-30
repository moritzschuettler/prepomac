// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
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
    [Serializable]
    class CRotateModelParts : PreprocessCommand, ISerializable
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private double[] _rotateCenter;
        private double[] _rotateAxis;
        private double _rotateAngle;
        private int _numberOfCopies;


        // Constructor                                                                                                              
        public CRotateModelParts(string[] partNames, double[] rotateCenter, double[] rotateAxis, double rotateAngle,
                                 int numberOfCopies)
            : base("Rotate mesh parts")
        {
            _partNames = partNames;
            _rotateCenter = rotateCenter;
            _rotateAxis = rotateAxis;
            _rotateAngle = rotateAngle;
            _numberOfCopies = numberOfCopies;
        }
        public CRotateModelParts(SerializationInfo info, StreamingContext context)
           : base("") // this can be empty
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "Command+_name":
                        _name = (string)entry.Value; break;
                    case "Command+_dateCreated":
                        _dateCreated = (DateTime)entry.Value; break;
                    case "_partNames":
                        _partNames = (string[])entry.Value;
                        break;
                    case "_rotateCenter":
                        _rotateCenter = (double[])entry.Value;
                        break;
                    case "_rotateAxis":
                        _rotateAxis = (double[])entry.Value;
                        break;
                    case "_rotateAngle":
                        _rotateAngle = (double)entry.Value;
                        break;
                    case "_copy":   // Compatibility for version v.2.4.3
                        _numberOfCopies = (bool)entry.Value ? 1 : -1;
                        break;
                    case "_numberOfCopies":
                        _numberOfCopies = (int)entry.Value;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.RotateModelParts(_partNames, _rotateCenter, _rotateAxis, _rotateAngle, _numberOfCopies);
            return true;
        }
        public override string GetCommandString()
        {
            return base.GetCommandString() + GetArrayAsString(_partNames);
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("Command+_name", _name, typeof(string));
            info.AddValue("Command+_dateCreated", _dateCreated, typeof(DateTime));
            info.AddValue("_partNames", _partNames, typeof(string[]));
            info.AddValue("_rotateCenter", _rotateCenter, typeof(double[]));
            info.AddValue("_rotateAxis", _rotateAxis, typeof(double[]));
            info.AddValue("_rotateAngle", _rotateAngle, typeof(double));
            info.AddValue("_numberOfCopies", _numberOfCopies, typeof(int));
        }
    }
}
