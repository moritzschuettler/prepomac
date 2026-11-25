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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(3205)]
    public class ShellSection : Section
    {
        // Variables
        [PmxKey(0)] private EquationContainer _offset;         //ISerializable
        [PmxKey(900)] private EquationContainer _angle;          //ISerializable
        [PmxKey(901)] private bool _composites;                  //ISerializable


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
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetOffset(_offset, false);
            // A file of a PrePoMax without the composite shells has no angle
            if (_angle == null) _angle = new EquationContainer(typeof(StringDoubleConverter), 0);
            SetAngle(_angle, false);
        }
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
    }
}
