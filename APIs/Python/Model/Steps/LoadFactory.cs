// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
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
                EquationString[] values = ToVector(firstPoint, "first_point");
                load.X1.Equation = values[0];
                load.Y1.Equation = values[1];
                load.Z1.Equation = values[2];
            }
            if (secondPoint != null)
            {
                EquationString[] values = ToVector(secondPoint, "second_point");
                load.X2.Equation = values[0];
                load.Y2.Equation = values[1];
                load.Z2.Equation = values[2];
            }
            if (direction != null)
            {
                EquationString[] values = ToVector(direction, "direction");
                load.N1.Equation = values[0];
                load.N2.Equation = values[1];
                load.N3.Equation = values[2];
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
                EquationString[] values = ToVector(axisPoint, "axis_point");
                load.X.Equation = values[0];
                load.Y.Equation = values[1];
                load.Z.Equation = values[2];
            }
            if (axisDirection != null)
            {
                EquationString[] values = ToVector(axisDirection, "axis_direction");
                load.N1.Equation = values[0];
                load.N2.Equation = values[1];
                load.N3.Equation = values[2];
            }
            SetEquation(load.RotationalSpeed, rotationalSpeed, "rotational_speed");
        }
        // The force and the displacement are stored apart, so a type change without a magnitude would silently
        // take whatever the other one holds; it is refused instead
        public static void ApplyPreTension(PreTensionLoad load, object magnitude, object direction,
                                           object loadType, object autoDirection)
        {
            if (loadType != null)
            {
                PreTensionLoadType type =
                    PythonValueConverter.ToEnum<PreTensionLoadType>(AsString(loadType, "load_type"), "load_type");
                if (type != load.Type && magnitude == null)
                    throw new PythonAPIException("The argument 'magnitude' must be given when 'load_type' changes " +
                                                 "the pre-tension from " + load.Type + " to " + type + ".");
                load.Type = type;
            }
            if (magnitude != null)
            {
                // The magnitude is a force or a displacement, depending on the pre-tension type
                if (load.Type == PreTensionLoadType.Displacement)
                    SetEquation(load.DisplacementMagnitude, magnitude, "magnitude");
                else SetEquation(load.ForceMagnitude, magnitude, "magnitude");
            }
            bool useSurfaceNormal = autoDirection != null && PythonValueConverter.ToBool(autoDirection,
                                                                                          "auto_direction");
            if (useSurfaceNormal && direction != null)
                throw new PythonAPIException("The arguments 'direction' and 'auto_direction' cannot be combined.");
            if (direction != null)
            {
                // A direction that is given is used instead of the surface normal
                EquationString[] values = ToVector(direction, "direction");
                load.AutoComputeDirection = false;
                load.X.Equation = values[0];
                load.Y.Equation = values[1];
                load.Z.Equation = values[2];
            }
            else if (autoDirection != null)
            {
                if (!useSurfaceNormal && load.AutoComputeDirection)
                    throw new PythonAPIException("The argument 'direction' must be given to stop using the " +
                                                 "surface normal.");
                load.AutoComputeDirection = useSurfaceNormal;
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
        private static EquationString[] ToVector(object value, string argumentName)
        {
            return PythonValueConverter.ToEquationArray(value, argumentName, 3);
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
