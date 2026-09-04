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
    public class ElasticEngineeringConstants : MaterialProperty, ISerializable
    {
        // Variables                                                                                                                
        public const int YoungsModulus1Id = 0;
        public const int YoungsModulus2Id = 1;
        public const int YoungsModulus3Id = 2;
        public const int PoissonsRatio12Id = 3;
        public const int PoissonsRatio13Id = 4;
        public const int PoissonsRatio23Id = 5;
        public const int ShearModulus12Id = 6;
        public const int ShearModulus13Id = 7;
        public const int ShearModulus23Id = 8;
        public const int TemperatureId = 9;
        public const int NumberOfValues = 10;
        private EquationContainer[][] _engineeringConstantsTemp;    //ISerializable


        // Properties                                                                                                               
        public EquationContainer[][] EngineeringConstantsTemp
        {
            get { return _engineeringConstantsTemp; }
            set { SetEngineeringConstantsTemp(value); }
        }


        // Constructors                                                                                                             
        public ElasticEngineeringConstants(double[][] engineeringConstantsTemp)
        {
            SetEngineeringConstantsTemp(engineeringConstantsTemp, false);
        }
        public ElasticEngineeringConstants(EquationContainer[][] engineeringConstantsTemp)
        {
            SetEngineeringConstantsTemp(engineeringConstantsTemp, false);
        }
        public ElasticEngineeringConstants(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_engineeringConstantsTemp":
                        if (entry.Value is double[][] values)
                            SetEngineeringConstantsTemp(values, false);
                        else
                            SetEngineeringConstantsTemp((EquationContainer[][])entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetEngineeringConstantsTemp(double[][] value, bool checkEquation = true)
        {
            CheckDataSize(value);
            _engineeringConstantsTemp = new EquationContainer[value.Length][];
            //
            for (int i = 0; i < value.Length; i++)
            {
                _engineeringConstantsTemp[i] = new EquationContainer[NumberOfValues];
                _engineeringConstantsTemp[i][YoungsModulus1Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][YoungsModulus1Id]);
                _engineeringConstantsTemp[i][YoungsModulus2Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][YoungsModulus2Id]);
                _engineeringConstantsTemp[i][YoungsModulus3Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][YoungsModulus3Id]);
                _engineeringConstantsTemp[i][PoissonsRatio12Id] =
                    new EquationContainer(typeof(StringDoubleConverter), value[i][PoissonsRatio12Id]);
                _engineeringConstantsTemp[i][PoissonsRatio13Id] =
                    new EquationContainer(typeof(StringDoubleConverter), value[i][PoissonsRatio13Id]);
                _engineeringConstantsTemp[i][PoissonsRatio23Id] =
                    new EquationContainer(typeof(StringDoubleConverter), value[i][PoissonsRatio23Id]);
                _engineeringConstantsTemp[i][ShearModulus12Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus12Id]);
                _engineeringConstantsTemp[i][ShearModulus13Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus13Id]);
                _engineeringConstantsTemp[i][ShearModulus23Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus23Id]);
                _engineeringConstantsTemp[i][TemperatureId] =
                    new EquationContainer(typeof(StringTemperatureConverter), value[i][TemperatureId]);
            }
            SetEngineeringConstantsTemp(_engineeringConstantsTemp, checkEquation);
        }
        private void SetEngineeringConstantsTemp(EquationContainer[][] value, bool checkEquation = true)
        {
            CheckDataSize(value);
            Func<double, double>[] checkValues = new Func<double, double>[]
            {
                CheckPositive, CheckPositive, CheckPositive, CheckFinite, CheckFinite,
                CheckFinite, CheckPositive, CheckPositive, CheckPositive, CheckFinite
            };
            EquationContainer.SetAndCheck(ref _engineeringConstantsTemp, value, checkValues, checkEquation);
        }
        private void CheckDataSize(double[][] value)
        {
            if (value == null || value.Length == 0)
                throw new CaeException("At least one engineering constants data point must be defined.");
            //
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null || value[i].Length != NumberOfValues)
                    throw new CaeException("Each engineering constants data point must contain 10 values.");
            }
        }
        private void CheckDataSize(EquationContainer[][] value)
        {
            if (value == null || value.Length == 0)
                throw new CaeException("At least one engineering constants data point must be defined.");
            //
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null || value[i].Length != NumberOfValues)
                    throw new CaeException("Each engineering constants data point must contain 10 values.");
            }
        }
        private double CheckPositive(double value)
        {
            CheckFinite(value);
            if (value <= 0) throw new CaeException(_positive);
            else return value;
        }
        private double CheckFinite(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new CaeException("The value must be finite.");
            else return value;
        }
        private void CheckStability(int rowId)
        {
            EquationContainer[] row = _engineeringConstantsTemp[rowId];
            double e1 = row[YoungsModulus1Id].Value;
            double e2 = row[YoungsModulus2Id].Value;
            double e3 = row[YoungsModulus3Id].Value;
            double v12 = row[PoissonsRatio12Id].Value;
            double v13 = row[PoissonsRatio13Id].Value;
            double v23 = row[PoissonsRatio23Id].Value;
            double s11 = 1 / e1;
            double s22 = 1 / e2;
            double s33 = 1 / e3;
            double s12 = -v12 / e1;
            double s13 = -v13 / e1;
            double s23 = -v23 / e2;
            double tolerance = Math.Max(s11, Math.Max(s22, s33)) * 1E-12;
            // Cholesky factorization of the normal compliance block.
            double l11 = Math.Sqrt(s11);
            double l21 = s12 / l11;
            double l31 = s13 / l11;
            double diagonal22 = s22 - l21 * l21;
            if (diagonal22 <= tolerance)
                throw new CaeException("Engineering constants data point " + (rowId + 1) + " is not physically stable.");
            //
            double l22 = Math.Sqrt(diagonal22);
            double l32 = (s23 - l31 * l21) / l22;
            double diagonal33 = s33 - l31 * l31 - l32 * l32;
            if (diagonal33 <= tolerance)
                throw new CaeException("Engineering constants data point " + (rowId + 1) + " is not physically stable.");
        }
        public void CheckTemperatures()
        {
            for (int i = 1; i < _engineeringConstantsTemp.Length; i++)
            {
                if (_engineeringConstantsTemp[i][TemperatureId].Value <=
                    _engineeringConstantsTemp[i - 1][TemperatureId].Value)
                {
                    throw new CaeException("Engineering constants temperatures must be strictly increasing.");
                }
            }
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            for (int i = 0; i < _engineeringConstantsTemp.Length; i++)
            {
                for (int j = 0; j < NumberOfValues; j++) _engineeringConstantsTemp[i][j].CheckEquation();
                CheckStability(i);
            }
            CheckTemperatures();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_engineeringConstantsTemp", _engineeringConstantsTemp, typeof(EquationContainer[][]));
        }
    }
}
