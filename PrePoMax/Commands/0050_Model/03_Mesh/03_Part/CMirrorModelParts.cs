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
    class CMirrorModelParts : PreprocessCommand, ISerializable
    {
        // Variables                                                                                                                
        private string[] _partNames;
        private double[] _mirrorPoint;
        private double[] _mirrorDirection;
        private bool _copy;


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
                    case "_mirrorPoint":
                        _mirrorPoint = (double[])entry.Value;
                        break;
                    case "_mirrorDirection":
                        _mirrorDirection = (double[])entry.Value;
                        break;
                    case "_copy":
                        _copy = (bool)entry.Value;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }


        // Methods                                                                                                                  
        public override bool Execute(Controller receiver)
        {
            receiver.MirrorModelParts(_partNames, _mirrorPoint, _mirrorDirection, _copy);
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
            info.AddValue("_mirrorPoint", _mirrorPoint, typeof(double[]));
            info.AddValue("_mirrorDirection", _mirrorDirection, typeof(double[]));
            info.AddValue("_copy", _copy, typeof(bool));
        }
    }
}
