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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class ShellSection : Section, ISerializable
    {
        // Variables
        private EquationContainer _offset;         //ISerializable
        private EquationContainer _angle;          //ISerializable
        private bool _composites;                  //ISerializable


        // Properties
        public EquationContainer Offset { get { return _offset; } set { SetOffset(value); } }
        public EquationContainer Angle { get { return _angle; } set { SetAngle(value); } }
        public bool Composites { get { return _composites; } set { _composites = value; } }


        // Constructors
        public ShellSection(string name, string materialName, string regionName, RegionTypeEnum regionType, double thickness,
                            bool twoD)
            : base(name, materialName, regionName, regionType, thickness, twoD)
        {
            Offset = new EquationContainer(typeof(StringDoubleConverter), 0);
            Angle = new EquationContainer(typeof(StringDoubleConverter), 0);
        }
        public ShellSection(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_offset":
                        // Compatibility for version v1.4.0
                        if (entry.Value is double value)
                            Offset = new EquationContainer(typeof(StringDoubleConverter), value);
                        else
                            SetOffset((EquationContainer)entry.Value, false);
                        break;
                    case "_angle":
                        if (entry.Value is double angleValue)
                            Angle = new EquationContainer(typeof(StringDoubleConverter), angleValue);
                        else
                            SetAngle((EquationContainer)entry.Value, false);
                        break;
                    case "_composites":
                        _composites = (bool)entry.Value; break;
                    default:
                        break;
                }
            }
            // Backwards compatibility: fields not present in older saved files
            if (_angle == null) Angle = new EquationContainer(typeof(StringDoubleConverter), 0);
        }


        // Methods
        private void SetOffset(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _offset, value, null, checkEquation);
        }
        private void SetAngle(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _angle, value, null, checkEquation);
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            base.CheckEquations();
            //
            _offset.CheckEquation();
            _angle.CheckEquation();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_offset", _offset, typeof(EquationContainer));
            info.AddValue("_angle", _angle, typeof(EquationContainer));
            info.AddValue("_composites", _composites, typeof(bool));
        }
    }
}
