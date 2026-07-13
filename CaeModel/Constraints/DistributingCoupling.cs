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
    public class DistributingCoupling : Coupling, ISerializable // ISerializable must be here
    {
        // Variables                                                                                                                
        private bool _u1;                           //ISerializable
        private bool _u2;                           //ISerializable
        private bool _u3;                           //ISerializable
        private bool _ur1;                          //ISerializable
        private bool _ur2;                          //ISerializable
        private bool _ur3;                          //ISerializable


        // Properties                                                                                                               
        public bool U1 { get { return _u1; } set { _u1 = value; } }
        public bool U2 { get { return _u2; } set { _u2 = value; } }
        public bool U3 { get { return _u3; } set { _u3 = value; if (TwoD) _u3 = false; } }
        public bool UR1 { get { return _ur1; } set { _ur1 = value; if (TwoD) _ur1 = false; } }
        public bool UR2 { get { return _ur2; } set { _ur2 = value; if (TwoD) _ur2 = false; } }
        public bool UR3 { get { return _ur3; } set { _ur3 = value; } }


        // Constructors                                                                                                             
        public DistributingCoupling(string name, string referencePointName, string regionName,
                                    RegionTypeEnum regionType, bool twoD)
            : base(name, referencePointName, regionName, regionType, twoD)
        {
            U1 = true;
            U2 = true;
            U3 = true;
            UR1 = true;
            UR2 = true;
            UR3 = true;
        }
        public DistributingCoupling(SerializationInfo info, StreamingContext context)
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
                    case "_ur1":
                        _ur1 = (bool)entry.Value; break;
                    case "_ur2":
                        _ur2 = (bool)entry.Value; break;
                    case "_ur3":
                        _ur3 = (bool)entry.Value; break;
                    //
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        public int[] GetDistributedDirections()
        {
            List<int> directions = new List<int>();
            if (_u1) directions.Add(1);
            if (_u2) directions.Add(2);
            if (_u3) directions.Add(3);
            if (_ur1) directions.Add(4);
            if (_ur2) directions.Add(5);
            if (_ur3) directions.Add(6);
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
            info.AddValue("_ur1", _ur1, typeof(bool));
            info.AddValue("_ur2", _ur2, typeof(bool));
            info.AddValue("_ur3", _ur3, typeof(bool));
        }
    }
}
