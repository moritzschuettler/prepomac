// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeModel;
using CaeResults;

namespace APIs.Python.Model
{
    // Applies the given values to a load. A null value keeps what the load already carries, so a new
    // load keeps the model default and a replaced load keeps its stored value. The values that name
    // another model item are resolved by the service, not here.
    internal static class LoadFactory
    {
        // Methods
        public static void ApplyConcentratedForce(CLoad load, object f1, object f2, object f3)
        {
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetEquation(load.F3, f3, "f3");
        }
        public static void ApplyMoment(MomentLoad load, object m1, object m2, object m3)
        {
            SetEquation(load.M1, m1, "m1");
            SetEquation(load.M2, m2, "m2");
            SetEquation(load.M3, m3, "m3");
        }
        public static void ApplyPressure(DLoad load, object magnitude)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
        }
        public static void ApplyHydrostaticPressure(HydrostaticPressure load, object firstPoint,
                                                    object secondPoint, object direction,
                                                    object firstPointPressure, object secondPointPressure,
                                                    object cutoff)
        {
            if (firstPoint != null)
            {
                double[] values = ToVector(firstPoint, "first_point");
                load.X1.SetEquationFromValue(values[0]);
                load.Y1.SetEquationFromValue(values[1]);
                load.Z1.SetEquationFromValue(values[2]);
            }
            if (secondPoint != null)
            {
                double[] values = ToVector(secondPoint, "second_point");
                load.X2.SetEquationFromValue(values[0]);
                load.Y2.SetEquationFromValue(values[1]);
                load.Z2.SetEquationFromValue(values[2]);
            }
            if (direction != null)
            {
                double[] values = ToVector(direction, "direction");
                load.N1.SetEquationFromValue(values[0]);
                load.N2.SetEquationFromValue(values[1]);
                load.N3.SetEquationFromValue(values[2]);
            }
            SetEquation(load.FirstPointPressure, firstPointPressure, "first_point_pressure");
            SetEquation(load.SecondPointPressure, secondPointPressure, "second_point_pressure");
            if (cutoff != null)
                load.HydrostaticPressureCutoff =
                    PythonValueConverter.ToEnum<HydrostaticPressureCutoffEnum>(AsString(cutoff, "cutoff"),
                                                                               "cutoff");
        }
        public static void ApplyImportedPressure(ImportedPressure load, object pressureTime,
                                                 object pressureVariableName, object interpolatorType,
                                                 object magnitudeFactor, object geometryScaleFactor)
        {
            // The solver writer reads the field they name, so an imported pressure cannot do without them
            if (pressureTime != null) load.PressureTime = AsString(pressureTime, "pressure_time");
            if (pressureVariableName != null)
                load.PressureVariableName = AsString(pressureVariableName, "pressure_variable_name");
            if (load.PressureTime == null || load.PressureVariableName == null)
                throw new PythonAPIException("An imported pressure needs both 'pressure_time' and " +
                                              "'pressure_variable_name'.");
            if (interpolatorType != null)
                load.InterpolatorType =
                    PythonValueConverter.ToEnum<InterpolatorEnum>(AsString(interpolatorType, "interpolator_type"),
                                                                  "interpolator_type");
            SetEquation(load.MagnitudeFactor, magnitudeFactor, "magnitude_factor");
            SetEquation(load.GeometryScaleFactor, geometryScaleFactor, "geometry_scale_factor");
        }
        public static void ApplySurfaceTraction(STLoad load, object f1, object f2, object f3, object p1,
                                                object p2, object p3)
        {
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetEquation(load.F3, f3, "f3");
            SetEquation(load.P1, p1, "p1");
            SetEquation(load.P2, p2, "p2");
            SetEquation(load.P3, p3, "p3");
        }
        public static void ApplyShellEdgeLoad(ShellEdgeLoad load, object magnitude)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
        }
        public static void ApplyGravity(GravityLoad load, object f1, object f2, object f3)
        {
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetEquation(load.F3, f3, "f3");
        }
        public static void ApplyCentrifugal(CentrifLoad load, object axisPoint, object axisDirection,
                                            object rotationalSpeed)
        {
            if (axisPoint != null)
            {
                double[] values = ToVector(axisPoint, "axis_point");
                load.X.SetEquationFromValue(values[0]);
                load.Y.SetEquationFromValue(values[1]);
                load.Z.SetEquationFromValue(values[2]);
            }
            if (axisDirection != null)
            {
                double[] values = ToVector(axisDirection, "axis_direction");
                load.N1.SetEquationFromValue(values[0]);
                load.N2.SetEquationFromValue(values[1]);
                load.N3.SetEquationFromValue(values[2]);
            }
            SetEquation(load.RotationalSpeed, rotationalSpeed, "rotational_speed");
        }
        public static void ApplyPreTension(PreTensionLoad load, object magnitude, object direction,
                                           object loadType)
        {
            if (loadType != null)
                load.Type = PythonValueConverter.ToEnum<PreTensionLoadType>(AsString(loadType, "load_type"),
                                                                            "load_type");
            if (magnitude != null)
            {
                // The magnitude is a force or a displacement, depending on the pre-tension type
                if (load.Type == PreTensionLoadType.Displacement)
                    SetEquation(load.DisplacementMagnitude, magnitude, "magnitude");
                else SetEquation(load.ForceMagnitude, magnitude, "magnitude");
            }
            if (direction != null)
            {
                // A direction that is given is used instead of the surface normal
                double[] values = ToVector(direction, "direction");
                load.AutoComputeDirection = false;
                load.X.SetEquationFromValue(values[0]);
                load.Y.SetEquationFromValue(values[1]);
                load.Z.SetEquationFromValue(values[2]);
            }
        }
        public static void ApplyConcentratedFlux(CFlux load, object magnitude, object addFlux)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
            if (addFlux != null) load.AddFlux = PythonValueConverter.ToBool(addFlux, "add_flux");
        }
        public static void ApplySurfaceFlux(DFlux load, object magnitude)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
        }
        public static void ApplyBodyFlux(BodyFlux load, object magnitude)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
        }
        public static void ApplyFilmHeatTransfer(FilmHeatTransfer load, object sinkTemperature,
                                                 object filmCoefficient)
        {
            SetEquation(load.SinkTemperature, sinkTemperature, "sink_temperature");
            SetEquation(load.FilmCoefficient, filmCoefficient, "film_coefficient");
        }
        public static void ApplyRadiationHeatTransfer(RadiationHeatTransfer load, object sinkTemperature,
                                                      object emissivity, object cavityRadiation)
        {
            SetEquation(load.SinkTemperature, sinkTemperature, "sink_temperature");
            SetEquation(load.Emissivity, emissivity, "emissivity");
            if (cavityRadiation != null)
                load.CavityRadiation = PythonValueConverter.ToBool(cavityRadiation, "cavity_radiation");
        }
        //
        private static void SetEquation(CaeGlobals.EquationContainer container, object value, string argumentName)
        {
            if (value == null) return;
            //
            container.Equation = PythonValueConverter.ToEquationString(value, argumentName);
        }
        private static double[] ToVector(object value, string argumentName)
        {
            double[] vector = PythonDoubleListConverter.ToDoubleArray(value, argumentName);
            if (vector == null || vector.Length != 3)
                throw new PythonAPIException("The argument '" + argumentName + "' must contain 3 values.");
            //
            return vector;
        }
        private static string AsString(object value, string argumentName)
        {
            string text = value as string;
            if (text == null)
                throw new PythonAPIException("The argument '" + argumentName + "' must be a string.");
            //
            return text;
        }
    }
}
