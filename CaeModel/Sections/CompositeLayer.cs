// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [Serializable]
    public class CompositeLayer : ISerializable
    {
        // Variables                                                                                                                
        private string _materialName;               //ISerializable
        private EquationContainer _thickness;       //ISerializable
        private EquationContainer _angle;           //ISerializable


        // Properties                                                                                                               
        public string MaterialName { get { return _materialName; } set { _materialName = value; } }
        public EquationContainer Thickness { get { return _thickness; } set { SetThickness(value); } }
        public EquationContainer Angle { get { return _angle; } set { SetAngle(value); } }


        // Constructors                                                                                                             
        public CompositeLayer(string materialName, double thickness, double angleDeg)
        {
            _materialName = materialName;
            // The constructor must work with a zero thickness
            SetThickness(new EquationContainer(typeof(StringLengthConverter), thickness), false);
            SetAngle(new EquationContainer(typeof(StringAngleDegConverter), angleDeg), false);
        }
        public CompositeLayer(string materialName, EquationContainer thickness, EquationContainer angle)
        {
            _materialName = materialName;
            SetThickness(thickness, false);
            SetAngle(angle, false);
        }
        public CompositeLayer(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_materialName":
                        _materialName = (string)entry.Value; break;
                    case "_thickness":
                        SetThickness((EquationContainer)entry.Value, false); break;
                    case "_angle":
                        SetAngle((EquationContainer)entry.Value, false); break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetThickness(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _thickness, value, CheckPositive, checkEquation);
        }
        private void SetAngle(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _angle, value, CheckFinite, checkEquation);
        }
        private double CheckPositive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                throw new CaeException("The layer thickness must be larger than 0.");
            else return value;
        }
        private double CheckFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CaeException("The layer angle must be finite.");
            else return value;
        }
        public void CheckEquations()
        {
            _thickness.CheckEquation();
            _angle.CheckEquation();
        }
        // ISerialization
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            info.AddValue("_materialName", _materialName, typeof(string));
            info.AddValue("_thickness", _thickness, typeof(EquationContainer));
            info.AddValue("_angle", _angle, typeof(EquationContainer));
        }
    }
}
