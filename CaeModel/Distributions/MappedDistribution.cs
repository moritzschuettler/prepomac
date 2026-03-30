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
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using CaeGlobals;
using CaeResults;
using DynamicTypeDescriptor;

namespace CaeModel
{
    [Serializable]
    public abstract class MappedDistribution : Distribution, ISerializable
    {
        // Variables                                                                                                                
        protected CloudInterpolatorEnum _interpolatorType;      //ISerializable
        protected EquationContainer _interpolatorRadius;        //ISerializable
        private EquationContainer _scaleX;                      //ISerializable
        private EquationContainer _scaleY;                      //ISerializable
        private EquationContainer _scaleZ;                      //ISerializable
        private EquationContainer _translateX;                  //ISerializable
        private EquationContainer _translateY;                  //ISerializable
        private EquationContainer _translateZ;                  //ISerializable
        protected double[][] _coorValues;                       //ISerializable
        //
        [NonSerialized] protected CloudInterpolator _interpolator;
        [NonSerialized] protected CloudPoint[] _cloudPoints;

        // Properties                                                                                                               
        public CloudInterpolatorEnum InterpolatorType { get { return _interpolatorType; } set { _interpolatorType = value; } }
        public EquationContainer InterpolatorRadius
        {
            get { return _interpolatorRadius; }
            set { SetInterpolatorRadius(value); }
        }
        
        public EquationContainer ScaleX { get { return _scaleX; } set { SetScaleX(value); } }
        public EquationContainer ScaleY { get { return _scaleY; } set { SetScaleY(value); } }
        public EquationContainer ScaleZ { get { return _scaleZ; } set { SetScaleZ(value); } }
        public EquationContainer TranslateX { get { return _translateX; } set { SetTranslateX(value); } }
        public EquationContainer TranslateY { get { return _translateY; } set { SetTranslateY(value); } }
        public EquationContainer TranslateZ { get { return _translateZ; } set { SetTranslateZ(value); } }
        public double[][] CoorValues { get { return _coorValues; } set { _coorValues = value; } }
        //
        public CloudInterpolator Interpolator { get { return _interpolator; } }


        // Constructors                                                                                                             
        public MappedDistribution(string name)
            : base(name)
        {
            ScaleX = new EquationContainer(typeof(StringDoubleConverter), 1);
            ScaleY = new EquationContainer(typeof(StringDoubleConverter), 1);
            ScaleZ = new EquationContainer(typeof(StringDoubleConverter), 1);
            TranslateX = new EquationContainer(typeof(StringLengthConverter), 0);
            TranslateY = new EquationContainer(typeof(StringLengthConverter), 0);
            TranslateZ = new EquationContainer(typeof(StringLengthConverter), 0);
        }
        public MappedDistribution(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_interpolatorType":
                        _interpolatorType = (CloudInterpolatorEnum)entry.Value; break;
                    case "_interpolatorRadius":
                        SetInterpolatorRadius((EquationContainer)entry.Value, false); break;
                    case "_scaleX":
                        SetScaleX((EquationContainer)entry.Value, false); break;
                    case "_scaleY":
                        SetScaleY((EquationContainer)entry.Value, false); break;
                    case "_scaleZ":
                        SetScaleZ((EquationContainer)entry.Value, false); break;
                    case "_translateX":
                        SetTranslateX((EquationContainer)entry.Value, false); break;
                    case "_translateY":
                        SetTranslateY((EquationContainer)entry.Value, false); break;
                    case "_translateZ":
                        SetTranslateZ((EquationContainer)entry.Value, false); break;
                    case "_coorValues":
                        _coorValues = (double[][])entry.Value; break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetInterpolatorRadius(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _interpolatorRadius, value, CheckPositive, EquationChanged, checkEquation);
        }
        private void SetScaleX(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _scaleX, value, CheckPositive, EquationChanged, checkEquation);
        }
        private void SetScaleY(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _scaleY, value, CheckPositive, EquationChanged, checkEquation);
        }
        private void SetScaleZ(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _scaleZ, value, CheckPositive, EquationChanged, checkEquation);
        }
        private void SetTranslateX(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _translateX, value, null, EquationChanged, checkEquation);
        }
        private void SetTranslateY(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _translateY, value, null, EquationChanged, checkEquation);
        }
        private void SetTranslateZ(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _translateZ, value, null, EquationChanged, checkEquation);
        }
        //
        protected virtual void EquationChanged()
        { }
        private double CheckPositive(double value)
        {
            if (value <= 0) throw new CaeException("The value must be larger than 0.");
            else return value;
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Using typeof() works also for null fields
            base.GetObjectData(info, context);
            //
            info.AddValue("_interpolatorType", _interpolatorType, typeof(CloudInterpolatorEnum));
            info.AddValue("_interpolatorRadius", _interpolatorRadius, typeof(EquationContainer));
            info.AddValue("_scaleX", _scaleX, typeof(EquationContainer));
            info.AddValue("_scaleY", _scaleY, typeof(EquationContainer));
            info.AddValue("_scaleZ", _scaleZ, typeof(EquationContainer));
            info.AddValue("_translateX", _translateX, typeof(EquationContainer));
            info.AddValue("_translateY", _translateY, typeof(EquationContainer));
            info.AddValue("_translateZ", _translateZ, typeof(EquationContainer));
            info.AddValue("_coorValues", _coorValues, typeof(double[][]));
        }
    }
}
