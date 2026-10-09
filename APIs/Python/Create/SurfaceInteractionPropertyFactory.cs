// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using System;
using System.Collections.Generic;

namespace APIs.Python
{
    // Makes the detached properties a surface interaction is composed of, the same way a material is
    // composed of material properties.
    internal static class SurfaceInteractionPropertyFactory
    {
        // Methods
        public static Friction CreateFriction(object coefficient, object stickSlope)
        {
            Friction friction = new Friction(ToPositive(coefficient, "coefficient"));
            if (stickSlope != null) friction.StickSlope = ToPositive(stickSlope, "stick_slope");
            //
            return friction;
        }
        public static GapConductance CreateGapConductance(object conductance, object conductanceTable)
        {
            GapConductance gapConductance = new GapConductance();
            if (conductance != null && conductanceTable != null)
                throw new PythonAPIException("Give either 'conductance' for a constant gap conductance, or " +
                                             "'conductance_table' for a tabular one, not both.");
            if (conductance == null && conductanceTable == null)
                throw new PythonAPIException("A gap conductance needs either 'conductance' or " +
                                             "'conductance_table'.");
            //
            if (conductance != null)
            {
                gapConductance.GapConductanceType = GapConductanceEnum.Constant;
                gapConductance.ConductnancePressureTemp = new double[][]
                {
                    new double[] { PythonValueConverter.ToDouble(conductance, "conductance"), 0, 0 }
                };
            }
            else
            {
                gapConductance.GapConductanceType = GapConductanceEnum.Tabular;
                // Every row is conductance, pressure and temperature
                gapConductance.ConductnancePressureTemp =
                    PythonTableConverter.ToTable(conductanceTable, "conductance_table", 3);
            }
            //
            return gapConductance;
        }
        public static SurfaceBehavior CreateSurfaceBehavior(string pressureOverclosureType, object c0, object p0,
                                                            object k, object s_inf, object pressureOverclosure)
        {
            SurfaceBehavior surfaceBehavior = new SurfaceBehavior();
            // The type must be set first, because it resets the values to the defaults it implies
            if (pressureOverclosureType != null)
                surfaceBehavior.PressureOverclosureType =
                    PythonValueConverter.ToEnum<PressureOverclosureEnum>(pressureOverclosureType,
                                                                         "pressure_overclosure_type");
            // The solver input carries only the values the type uses, so any other given value would be lost
            PressureOverclosureEnum type = surfaceBehavior.PressureOverclosureType;
            CheckApplies(type, c0, "c0");
            CheckApplies(type, p0, "p0");
            CheckApplies(type, k, "k");
            CheckApplies(type, s_inf, "s_inf");
            CheckApplies(type, pressureOverclosure, "pressure_overclosure");
            //
            if (c0 != null) surfaceBehavior.C0 = ToPositive(c0, "c0");
            if (p0 != null) surfaceBehavior.P0 = ToPositive(p0, "p0");
            if (k != null) surfaceBehavior.K = ToPositive(k, "k");
            if (s_inf != null) surfaceBehavior.Sinf = ToPositive(s_inf, "s_inf");
            if (pressureOverclosure != null)
            {
                // Every row is a pressure and an overclosure
                surfaceBehavior.PressureOverclosure =
                    PythonTableConverter.ToTable(pressureOverclosure, "pressure_overclosure", 2);
            }
            //
            return surfaceBehavior;
        }
        public static bool UsesValue(PressureOverclosureEnum type, string argumentName)
        {
            switch (argumentName)
            {
                case "c0": return type == PressureOverclosureEnum.Linear || type == PressureOverclosureEnum.Exponential;
                case "p0": return type == PressureOverclosureEnum.Exponential;
                case "k": return type == PressureOverclosureEnum.Linear || type == PressureOverclosureEnum.Tied;
                case "s_inf": return type == PressureOverclosureEnum.Linear;
                case "pressure_overclosure": return type == PressureOverclosureEnum.Tabular;
                default: return false;
            }
        }
        //
        private static void CheckApplies(PressureOverclosureEnum type, object value, string argumentName)
        {
            if (value == null || UsesValue(type, argumentName)) return;
            //
            List<string> usedBy = new List<string>();
            foreach (PressureOverclosureEnum candidate in Enum.GetValues(typeof(PressureOverclosureEnum)))
            {
                if (UsesValue(candidate, argumentName)) usedBy.Add(candidate.ToString());
            }
            throw new PythonAPIException("The argument '" + argumentName + "' does not apply to the " + type +
                                         " pressure_overclosure_type; it is used by " + string.Join(" and ", usedBy) +
                                         ".");
        }
        private static double ToPositive(object value, string argumentName)
        {
            double number = PythonValueConverter.ToDouble(value, argumentName);
            if (!(number > 0))
                throw new PythonAPIException("The argument '" + argumentName + "' must be larger than 0.");
            //
            return number;
        }
    }
}
