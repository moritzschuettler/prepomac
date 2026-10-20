// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
// Source code: https://gitlab.com/MatejB/PrePoMax
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    // A ply of a composite layup: a composite layer with a name, the region it covers and an optional coordinate
    // system of its own. The angle turns the fiber direction about the shell normal from the layup orientation.
    [Serializable]
    public class Ply : CompositeLayer, ISerializable
    {
        // Variables
        public const string LayupCoordinateSystemName = "<layup>";
        //
        private string _name;                       //ISerializable
        private string _regionName;                 //ISerializable
        private string _coordinateSystemName;       //ISerializable


        // Properties
        public string Name { get { return _name; } set { _name = value; } }
        // The name of an element set or of a part
        public string RegionName { get { return _regionName; } set { _regionName = value; } }
        // Null uses the orientation of the layup
        public string CoordinateSystemName
        {
            get { return _coordinateSystemName; }
            set
            {
                _coordinateSystemName = value;
                if (_coordinateSystemName == LayupCoordinateSystemName || _coordinateSystemName == "")
                    _coordinateSystemName = null;
            }
        }


        // Constructors
        public Ply(string name, string regionName, string materialName, double thickness, double angleDeg)
            : base(materialName, thickness, angleDeg)
        {
            _name = name;
            _regionName = regionName;
            _coordinateSystemName = null;
        }
        public Ply(string name, string regionName, string materialName, EquationContainer thickness,
                   EquationContainer angle)
            : base(materialName, thickness, angle)
        {
            _name = name;
            _regionName = regionName;
            _coordinateSystemName = null;
        }
        public Ply(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_name":
                        _name = (string)entry.Value; break;
                    case "_regionName":
                        _regionName = (string)entry.Value; break;
                    case "_coordinateSystemName":
                        _coordinateSystemName = (string)entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_name", _name, typeof(string));
            info.AddValue("_regionName", _regionName, typeof(string));
            info.AddValue("_coordinateSystemName", _coordinateSystemName, typeof(string));
        }
    }
}
