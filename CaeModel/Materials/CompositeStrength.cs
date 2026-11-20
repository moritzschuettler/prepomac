// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Runtime.Serialization;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(3174)]
    public class CompositeStrength : MaterialProperty
    {
        // Variables                                                                                                                
        public const double DefaultTsaiWuCoefficient = -0.5;
        [PmxKey(0)] private EquationContainer _tensileStrength1;            //ISerializable
        [PmxKey(1)] private EquationContainer _compressiveStrength1;        //ISerializable
        [PmxKey(2)] private EquationContainer _tensileStrength2;            //ISerializable
        [PmxKey(3)] private EquationContainer _compressiveStrength2;        //ISerializable
        [PmxKey(4)] private EquationContainer _shearStrength12;             //ISerializable
        [PmxKey(5)] private EquationContainer _shearStrength23;             //ISerializable
        [PmxKey(6)] private EquationContainer _tsaiWuCoefficient;           //ISerializable


        // Properties                                                                                                               
        public EquationContainer TensileStrength1
        {
            get { return _tensileStrength1; }
            set { SetTensileStrength1(value); }
        }
        public EquationContainer CompressiveStrength1
        {
            get { return _compressiveStrength1; }
            set { SetCompressiveStrength1(value); }
        }
        public EquationContainer TensileStrength2
        {
            get { return _tensileStrength2; }
            set { SetTensileStrength2(value); }
        }
        public EquationContainer CompressiveStrength2
        {
            get { return _compressiveStrength2; }
            set { SetCompressiveStrength2(value); }
        }
        public EquationContainer ShearStrength12 { get { return _shearStrength12; } set { SetShearStrength12(value); } }
        public EquationContainer ShearStrength23 { get { return _shearStrength23; } set { SetShearStrength23(value); } }
        public EquationContainer TsaiWuCoefficient
        {
            get { return _tsaiWuCoefficient; }
            set { SetTsaiWuCoefficient(value); }
        }


        // Constructors                                                                                                             
        public CompositeStrength(double tensileStrength1, double compressiveStrength1, double tensileStrength2,
                                 double compressiveStrength2, double shearStrength12, double shearStrength23,
                                 double tsaiWuCoefficient)
        {
            // The constructor must work with zero strengths
            SetTensileStrength1(new EquationContainer(typeof(StringPressureConverter), tensileStrength1), false);
            SetCompressiveStrength1(new EquationContainer(typeof(StringPressureConverter), compressiveStrength1), false);
            SetTensileStrength2(new EquationContainer(typeof(StringPressureConverter), tensileStrength2), false);
            SetCompressiveStrength2(new EquationContainer(typeof(StringPressureConverter), compressiveStrength2), false);
            SetShearStrength12(new EquationContainer(typeof(StringPressureConverter), shearStrength12), false);
            SetShearStrength23(new EquationContainer(typeof(StringPressureConverter), shearStrength23), false);
            SetTsaiWuCoefficient(new EquationContainer(typeof(StringDoubleConverter), tsaiWuCoefficient), false);
        }
        public CompositeStrength(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_tensileStrength1":
                        SetTensileStrength1((EquationContainer)entry.Value, false); break;
                    case "_compressiveStrength1":
                        SetCompressiveStrength1((EquationContainer)entry.Value, false); break;
                    case "_tensileStrength2":
                        SetTensileStrength2((EquationContainer)entry.Value, false); break;
                    case "_compressiveStrength2":
                        SetCompressiveStrength2((EquationContainer)entry.Value, false); break;
                    case "_shearStrength12":
                        SetShearStrength12((EquationContainer)entry.Value, false); break;
                    case "_shearStrength23":
                        SetShearStrength23((EquationContainer)entry.Value, false); break;
                    case "_tsaiWuCoefficient":
                        SetTsaiWuCoefficient((EquationContainer)entry.Value, false); break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        [OnDeserialized]
        private void RestoreEquationCallbacks(StreamingContext context)
        {
            SetTensileStrength1(_tensileStrength1, false);
            SetCompressiveStrength1(_compressiveStrength1, false);
            SetTensileStrength2(_tensileStrength2, false);
            SetCompressiveStrength2(_compressiveStrength2, false);
            SetShearStrength12(_shearStrength12, false);
            SetShearStrength23(_shearStrength23, false);
            SetTsaiWuCoefficient(_tsaiWuCoefficient, false);
        }
        private void SetTensileStrength1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _tensileStrength1, value, CheckPositive, checkEquation);
        }
        private void SetCompressiveStrength1(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _compressiveStrength1, value, CheckPositive, checkEquation);
        }
        private void SetTensileStrength2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _tensileStrength2, value, CheckPositive, checkEquation);
        }
        private void SetCompressiveStrength2(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _compressiveStrength2, value, CheckPositive, checkEquation);
        }
        private void SetShearStrength12(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _shearStrength12, value, CheckPositive, checkEquation);
        }
        private void SetShearStrength23(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _shearStrength23, value, CheckPositive, checkEquation);
        }
        private void SetTsaiWuCoefficient(EquationContainer value, bool checkEquation = true)
        {
            EquationContainer.SetAndCheck(ref _tsaiWuCoefficient, value, CheckTsaiWuCoefficient, checkEquation);
        }
        //
        private double CheckPositive(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CaeException("The value must be finite.");
            if (value <= 0) throw new CaeException(_positive);
            else return value;
        }
        private double CheckTsaiWuCoefficient(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= -1 || value >= 1)
                throw new CaeException("The Tsai-Wu interaction coefficient must be larger than -1 and smaller than 1.");
            else return value;
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            _tensileStrength1.CheckEquation();
            _compressiveStrength1.CheckEquation();
            _tensileStrength2.CheckEquation();
            _compressiveStrength2.CheckEquation();
            _shearStrength12.CheckEquation();
            _shearStrength23.CheckEquation();
            _tsaiWuCoefficient.CheckEquation();
        }
    }
}
