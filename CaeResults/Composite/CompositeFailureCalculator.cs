// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeResults
{
    // The failure criteria of a ply in plane stress, from the stresses s1 and s2 along the ply axes 1 and 2 and the
    // in-plane shear stress t12. The failure index reaches 1 at failure. The reserve factor is the factor the stresses
    // can be multiplied by to reach failure, NaN where they cannot. The mode is 0 for a criterion without modes, 1 for
    // fiber tension, 2 for fiber compression, 3 for matrix tension, 4 for matrix compression and 5 for in-plane shear.
    public static class CompositeFailureCalculator
    {
        // Variables
        public const int ModeNone = 0;
        public const int ModeFiberTension = 1;
        public const int ModeFiberCompression = 2;
        public const int ModeMatrixTension = 3;
        public const int ModeMatrixCompression = 4;
        public const int ModeShear = 5;


        // Methods
        public static void Evaluate(CompositeFailureCriterionEnum criterion, double s1, double s2, double t12,
                                    PlyStrength strength, out double failureIndex, out double reserveFactor, out int mode)
        {
            if (criterion == CompositeFailureCriterionEnum.MaxStress)
                MaxStress(s1, s2, t12, strength, out failureIndex, out reserveFactor, out mode);
            else if (criterion == CompositeFailureCriterionEnum.TsaiHill)
                TsaiHill(s1, s2, t12, strength, out failureIndex, out reserveFactor, out mode);
            else if (criterion == CompositeFailureCriterionEnum.TsaiWu)
                TsaiWu(s1, s2, t12, strength, out failureIndex, out reserveFactor, out mode);
            else if (criterion == CompositeFailureCriterionEnum.Hashin)
                Hashin(s1, s2, t12, strength, out failureIndex, out reserveFactor, out mode);
            else throw new NotSupportedException();
        }
        private static void MaxStress(double s1, double s2, double t12, PlyStrength strength, out double failureIndex,
                                      out double reserveFactor, out int mode)
        {
            double f1 = s1 >= 0 ? s1 / strength.Tensile1 : -s1 / strength.Compressive1;
            double f2 = s2 >= 0 ? s2 / strength.Tensile2 : -s2 / strength.Compressive2;
            double f12 = Math.Abs(t12) / strength.Shear12;
            //
            failureIndex = f1;
            mode = s1 >= 0 ? ModeFiberTension : ModeFiberCompression;
            if (f2 > failureIndex)
            {
                failureIndex = f2;
                mode = s2 >= 0 ? ModeMatrixTension : ModeMatrixCompression;
            }
            if (f12 > failureIndex)
            {
                failureIndex = f12;
                mode = ModeShear;
            }
            reserveFactor = failureIndex > 0 ? 1 / failureIndex : double.NaN;
        }
        private static void TsaiHill(double s1, double s2, double t12, PlyStrength strength, out double failureIndex,
                                     out double reserveFactor, out int mode)
        {
            double x = s1 >= 0 ? strength.Tensile1 : strength.Compressive1;
            double y = s2 >= 0 ? strength.Tensile2 : strength.Compressive2;
            double s = strength.Shear12;
            failureIndex = (s1 * s1 - s1 * s2) / (x * x) + s2 * s2 / (y * y) + t12 * t12 / (s * s);
            reserveFactor = GetReserveFactor(failureIndex, 0);
            mode = ModeNone;
        }
        private static void TsaiWu(double s1, double s2, double t12, PlyStrength strength, out double failureIndex,
                                   out double reserveFactor, out int mode)
        {
            double f1 = 1 / strength.Tensile1 - 1 / strength.Compressive1;
            double f2 = 1 / strength.Tensile2 - 1 / strength.Compressive2;
            double f11 = 1 / (strength.Tensile1 * strength.Compressive1);
            double f22 = 1 / (strength.Tensile2 * strength.Compressive2);
            double f66 = 1 / (strength.Shear12 * strength.Shear12);
            double f12 = strength.TsaiWuCoefficient * Math.Sqrt(f11 * f22);
            //
            double quadratic = f11 * s1 * s1 + f22 * s2 * s2 + f66 * t12 * t12 + 2 * f12 * s1 * s2;
            double linear = f1 * s1 + f2 * s2;
            failureIndex = quadratic + linear;
            reserveFactor = GetReserveFactor(quadratic, linear);
            mode = ModeNone;
        }
        // Hashin's plane stress criterion of 1980
        private static void Hashin(double s1, double s2, double t12, PlyStrength strength, out double failureIndex,
                                   out double reserveFactor, out int mode)
        {
            double shear = t12 * t12 / (strength.Shear12 * strength.Shear12);
            double fiberQuadratic;
            int fiberMode;
            if (s1 >= 0)
            {
                fiberQuadratic = s1 * s1 / (strength.Tensile1 * strength.Tensile1) + shear;
                fiberMode = ModeFiberTension;
            }
            else
            {
                fiberQuadratic = s1 * s1 / (strength.Compressive1 * strength.Compressive1);
                fiberMode = ModeFiberCompression;
            }
            double matrixQuadratic;
            double matrixLinear = 0;
            int matrixMode;
            if (s2 >= 0)
            {
                matrixQuadratic = s2 * s2 / (strength.Tensile2 * strength.Tensile2) + shear;
                matrixMode = ModeMatrixTension;
            }
            else
            {
                double twoS23 = 2 * strength.Shear23;
                matrixQuadratic = s2 * s2 / (twoS23 * twoS23) + shear;
                matrixLinear = ((strength.Compressive2 / twoS23) * (strength.Compressive2 / twoS23) - 1) * s2 /
                               strength.Compressive2;
                matrixMode = ModeMatrixCompression;
            }
            double matrixIndex = matrixQuadratic + matrixLinear;
            if (fiberQuadratic >= matrixIndex)
            {
                failureIndex = fiberQuadratic;
                mode = fiberMode;
            }
            else
            {
                failureIndex = matrixIndex;
                mode = matrixMode;
            }
            double fiberReserve = GetReserveFactor(fiberQuadratic, 0);
            double matrixReserve = GetReserveFactor(matrixQuadratic, matrixLinear);
            if (double.IsNaN(fiberReserve)) reserveFactor = matrixReserve;
            else if (double.IsNaN(matrixReserve)) reserveFactor = fiberReserve;
            else reserveFactor = Math.Min(fiberReserve, matrixReserve);
        }
        // The factor R that brings a criterion of the form quadratic R^2 + linear R to 1
        private static double GetReserveFactor(double quadratic, double linear)
        {
            if (quadratic > 0) return (-linear + Math.Sqrt(linear * linear + 4 * quadratic)) / (2 * quadratic);
            if (linear > 0) return 1 / linear;
            return double.NaN;
        }
    }
}
