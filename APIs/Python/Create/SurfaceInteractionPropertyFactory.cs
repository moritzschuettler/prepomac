// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;

namespace APIs.Python
{
    // Makes the detached properties a surface interaction is composed of, the same way a material is
    // composed of material properties.
    internal static class SurfaceInteractionPropertyFactory
    {
        // Methods
        public static Friction CreateFriction(object coefficient, object stickSlope)
        {
            Friction friction = new Friction(PythonValueConverter.ToDouble(coefficient, "coefficient"));
            if (stickSlope != null)
                friction.StickSlope = PythonValueConverter.ToDouble(stickSlope, "stick_slope");
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
            //
            if (c0 != null) surfaceBehavior.C0 = PythonValueConverter.ToDouble(c0, "c0");
            if (p0 != null) surfaceBehavior.P0 = PythonValueConverter.ToDouble(p0, "p0");
            if (k != null) surfaceBehavior.K = PythonValueConverter.ToDouble(k, "k");
            if (s_inf != null) surfaceBehavior.Sinf = PythonValueConverter.ToDouble(s_inf, "s_inf");
            if (pressureOverclosure != null)
            {
                // Every row is a pressure and an overclosure
                surfaceBehavior.PressureOverclosure =
                    PythonTableConverter.ToTable(pressureOverclosure, "pressure_overclosure", 2);
            }
            //
            return surfaceBehavior;
        }
    }
}
