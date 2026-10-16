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
    public class ElasticLamina : MaterialProperty, ISerializable
    {
        // Variables                                                                                                                
        public const int YoungsModulus1Id = 0;
        public const int YoungsModulus2Id = 1;
        public const int PoissonsRatio12Id = 2;
        public const int ShearModulus12Id = 3;
        public const int ShearModulus13Id = 4;
        public const int ShearModulus23Id = 5;
        public const int TemperatureId = 6;
        public const int NumberOfValues = 7;
        private EquationContainer[][] _laminaConstantsTemp;         //ISerializable


        // Properties                                                                                                               
        public EquationContainer[][] LaminaConstantsTemp
        {
            get { return _laminaConstantsTemp; }
            set { SetLaminaConstantsTemp(value); }
        }


        // Constructors                                                                                                             
        public ElasticLamina(double[][] laminaConstantsTemp)
        {
            SetLaminaConstantsTemp(laminaConstantsTemp, false);
        }
        public ElasticLamina(EquationContainer[][] laminaConstantsTemp)
        {
            SetLaminaConstantsTemp(laminaConstantsTemp, false);
        }
        public ElasticLamina(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            foreach (SerializationEntry entry in info)
            {
                switch (entry.Name)
                {
                    case "_laminaConstantsTemp":
                        if (entry.Value is double[][] values)
                            SetLaminaConstantsTemp(values, false);
                        else
                            SetLaminaConstantsTemp((EquationContainer[][])entry.Value, false);
                        break;
                    default:
                        break;
                }
            }
        }


        // Methods                                                                                                                  
        private void SetLaminaConstantsTemp(double[][] value, bool checkEquation = true)
        {
            CheckDataSize(value);
            _laminaConstantsTemp = new EquationContainer[value.Length][];
            //
            for (int i = 0; i < value.Length; i++)
            {
                _laminaConstantsTemp[i] = new EquationContainer[NumberOfValues];
                _laminaConstantsTemp[i][YoungsModulus1Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][YoungsModulus1Id]);
                _laminaConstantsTemp[i][YoungsModulus2Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][YoungsModulus2Id]);
                _laminaConstantsTemp[i][PoissonsRatio12Id] =
                    new EquationContainer(typeof(StringDoubleConverter), value[i][PoissonsRatio12Id]);
                _laminaConstantsTemp[i][ShearModulus12Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus12Id]);
                _laminaConstantsTemp[i][ShearModulus13Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus13Id]);
                _laminaConstantsTemp[i][ShearModulus23Id] =
                    new EquationContainer(typeof(StringPressureConverter), value[i][ShearModulus23Id]);
                _laminaConstantsTemp[i][TemperatureId] =
                    new EquationContainer(typeof(StringTemperatureConverter), value[i][TemperatureId]);
            }
            SetLaminaConstantsTemp(_laminaConstantsTemp, checkEquation);
        }
        private void SetLaminaConstantsTemp(EquationContainer[][] value, bool checkEquation = true)
        {
            CheckDataSize(value);
            Func<double, double>[] checkValues = new Func<double, double>[]
            {
                CheckPositive, CheckPositive, CheckFinite, CheckPositive, CheckPositive, CheckPositive, CheckFinite
            };
            EquationContainer.SetAndCheck(ref _laminaConstantsTemp, value, checkValues, checkEquation);
        }
        private void CheckDataSize(double[][] value)
        {
            if (value == null || value.Length == 0)
                throw new CaeException("At least one lamina data point must be defined.");
            //
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null || value[i].Length != NumberOfValues)
                    throw new CaeException("Each lamina data point must contain 7 values.");
            }
        }
        private void CheckDataSize(EquationContainer[][] value)
        {
            if (value == null || value.Length == 0)
                throw new CaeException("At least one lamina data point must be defined.");
            //
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == null || value[i].Length != NumberOfValues)
                    throw new CaeException("Each lamina data point must contain 7 values.");
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
            EquationContainer[] row = _laminaConstantsTemp[rowId];
            double e1 = row[YoungsModulus1Id].Value;
            double e2 = row[YoungsModulus2Id].Value;
            double v12 = row[PoissonsRatio12Id].Value;
            double v23 = GetPoissonsRatio23(e2, row[ShearModulus23Id].Value);
            double s11 = 1 / e1;
            double s22 = 1 / e2;
            double s12 = -v12 / e1;
            double s23 = -v23 / e2;
            double tolerance = Math.Max(s11, s22) * 1E-12;
            // Cholesky factorization of the normal compliance block completed with E3 = E2 and v13 = v12.
            double l11 = Math.Sqrt(s11);
            double l21 = s12 / l11;
            double diagonal22 = s22 - l21 * l21;
            if (diagonal22 <= tolerance)
                throw new CaeException("Lamina data point " + (rowId + 1) + " is not physically stable.");
            //
            double l22 = Math.Sqrt(diagonal22);
            double l32 = (s23 - l21 * l21) / l22;
            double diagonal33 = s22 - l21 * l21 - l32 * l32;
            if (diagonal33 <= tolerance)
                throw new CaeException("Lamina data point " + (rowId + 1) + " is not physically stable: the Poisson's " +
                                       "ratio v23 = E2 / (2 G23) - 1 = " + v23.ToString("G6") + " that G23 implies " +
                                       "is too large.");
        }
        public static double GetPoissonsRatio23(double youngsModulus2, double shearModulus23)
        {
            return youngsModulus2 / (2 * shearModulus23) - 1;
        }
        public void CheckTemperatures()
        {
            for (int i = 1; i < _laminaConstantsTemp.Length; i++)
            {
                if (_laminaConstantsTemp[i][TemperatureId].Value <=
                    _laminaConstantsTemp[i - 1][TemperatureId].Value)
                {
                    throw new CaeException("Lamina temperatures must be strictly increasing.");
                }
            }
        }
        // IContainsEquations
        public override void CheckEquations()
        {
            for (int i = 0; i < _laminaConstantsTemp.Length; i++)
            {
                for (int j = 0; j < NumberOfValues; j++) _laminaConstantsTemp[i][j].CheckEquation();
                CheckStability(i);
            }
            CheckTemperatures();
        }
        // ISerialization
        public new void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            //
            info.AddValue("_laminaConstantsTemp", _laminaConstantsTemp, typeof(EquationContainer[][]));
        }
    }
}
