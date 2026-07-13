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
using System.Runtime.Serialization;
using CaeGlobals;


namespace CaeModel
{
    [Serializable]
    public class KinematicCoupling : Coupling, ISerializable // ISerializable must be here
    {
        // Variables                                                                                                                
        private bool _u1;                           //ISerializable
        private bool _u2;                           //ISerializable
        private bool _u3;                           //ISerializable


        // Properties                                                                                                               
        public bool U1 { get { return _u1; } set { _u1 = value; } }
        public bool U2 { get { return _u2; } set { _u2 = value; } }
        public bool U3 { get { return _u3; } set { _u3 = value; if (TwoD) _u3 = false; } }


        // Constructors                                                                                                             
        public KinematicCoupling(string name, string referencePointName, string regionName,
                                 RegionTypeEnum regionType, bool twoD)
            : base(name, referencePointName, regionName, regionType, twoD)
        {
            U1 = true;
            U2 = true;
            U3 = true;
        }
        public KinematicCoupling(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_u1":
                        _u1 = (bool)entry.Value; break;
                    case "_u2":
                        _u2 = (bool)entry.Value; break;
                    case "_u3":
                        _u3 = (bool)entry.Value; break;
                    //
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public int[] GetConstrainedDirections()
        {
            List<int> directions = new List<int>();
            if (_u1) directions.Add(1);
            if (_u2) directions.Add(2);
            if (_u3) directions.Add(3);
            return directions.ToArray();
        }

        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_u1", _u1, typeof(bool));
            info.AddValue("_u2", _u2, typeof(bool));
            info.AddValue("_u3", _u3, typeof(bool));
        }
    }
}
