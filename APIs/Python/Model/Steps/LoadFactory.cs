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
    // another model item are resolved by the service, not here. A value the load would replace - an
    // out of plane component of a two dimensional model, an axisymmetric axis, a clamped value - is
    // refused instead.
    internal static class LoadFactory
    {
        // Methods
        public static void ApplyConcentratedForce(CLoad load, object f1, object f2, object f3)
        {
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetInPlaneEquation(load, load.F3, f3, "f3");
        }
        public static void ApplyMoment(MomentLoad load, object m1, object m2, object m3)
        {
            SetInPlaneEquation(load, load.M1, m1, "m1");
            SetInPlaneEquation(load, load.M2, m2, "m2");
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
                RefuseOutOfPlaneComponent(load, load.Z1, values[2], "first_point");
                PythonModelArgumentCheck.SetEquations(new[] { load.X1, load.Y1, load.Z1 }, values, "first_point");
            }
            if (secondPoint != null)
            {
                EquationString[] values = ToVector(secondPoint, "second_point");
                RefuseOutOfPlaneComponent(load, load.Z2, values[2], "second_point");
                PythonModelArgumentCheck.SetEquations(new[] { load.X2, load.Y2, load.Z2 }, values, "second_point");
            }
            if (direction != null)
            {
                EquationString[] values = ToVector(direction, "direction");
                RefuseOutOfPlaneComponent(load, load.N3, values[2], "direction");
                PythonModelArgumentCheck.SetEquations(new[] { load.N1, load.N2, load.N3 }, values, "direction");
                PythonModelArgumentCheck.CheckNonZeroDirection(new[] { load.N1, load.N2, load.N3 }, "direction");
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
        // The model writes the force components without a distribution and the force per area components with
        // one, and keeps the other group without effect, so a value for that group is refused. The distribution
        // is the one the load carries after the call.
        public static void ApplySurfaceTraction(STLoad load, object f1, object f2, object f3, object p1,
                                                object p2, object p3)
        {
            bool constant = PythonNameComparer.AreSame(load.DistributionName, Distribution.DefaultDistributionName);
            string unused = constant ? FirstGiven(p1, "p1", p2, "p2", p3, "p3") : FirstGiven(f1, "f1", f2, "f2", f3, "f3");
            if (unused != null && constant)
                throw new PythonAPIException("The argument '" + unused + "' is a force per area, which a surface traction " +
                                             "takes only with a distribution. Give the force in 'f1', 'f2' and 'f3', or " +
                                             "a 'distribution_name'.");
            if (unused != null)
                throw new PythonAPIException("The argument '" + unused + "' is a force, which a surface traction with the " +
                                             "distribution '" + load.DistributionName + "' does not use. Give the " +
                                             "force per area in 'p1', 'p2' and 'p3'.");
            //
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetInPlaneEquation(load, load.F3, f3, "f3");
            SetEquation(load.P1, p1, "p1");
            SetEquation(load.P2, p2, "p2");
            SetInPlaneEquation(load, load.P3, p3, "p3");
        }
        public static void ApplyShellEdgeLoad(ShellEdgeLoad load, object magnitude)
        {
            SetEquation(load.Magnitude, magnitude, "magnitude");
        }
        public static void ApplyGravity(GravityLoad load, object f1, object f2, object f3)
        {
            SetEquation(load.F1, f1, "f1");
            SetEquation(load.F2, f2, "f2");
            SetInPlaneEquation(load, load.F3, f3, "f3");
        }
        // An axisymmetric model turns about its y axis, which the load stores whatever it is given
        public static void ApplyCentrifugal(CentrifLoad load, object axisPoint, object axisDirection,
                                            object rotationalSpeed)
        {
            if (axisPoint != null)
            {
                EquationString[] values = ToVector(axisPoint, "axis_point");
                if (load.Axisymmetric) RefuseAxisymmetricAxis(load.X, values, new double[] { 0, 0, 0 }, "axis_point");
                PythonModelArgumentCheck.SetEquations(new[] { load.X, load.Y, load.Z }, values, "axis_point");
            }
            if (axisDirection != null)
            {
                EquationString[] values = ToVector(axisDirection, "axis_direction");
                if (load.Axisymmetric)
                    RefuseAxisymmetricAxis(load.N1, values, new double[] { 0, 1, 0 }, "axis_direction");
                PythonModelArgumentCheck.SetEquations(new[] { load.N1, load.N2, load.N3 }, values, "axis_direction");
                PythonModelArgumentCheck.CheckNonZeroDirection(new[] { load.N1, load.N2, load.N3 }, "axis_direction");
            }
            if (rotationalSpeed != null)
            {
                EquationString equation = PythonValueConverter.ToEquationString(rotationalSpeed, "rotational_speed");
                if (GivenEquationValue.Evaluate(load.RotationalSpeed, equation, "rotational_speed") < 0)
                    throw new PythonAPIException("The argument 'rotational_speed' must not be negative.");
                PythonModelArgumentCheck.SetEquation(load.RotationalSpeed, equation, "rotational_speed");
            }
        }
        // The force and the displacement are stored apart, so a type change without a magnitude would silently
        // take whatever the other one holds; it is refused instead
        public static void ApplyPreTension(PreTensionLoad load, object magnitude, object direction,
                                           object tensionType, object autoDirection)
        {
            if (tensionType != null)
            {
                PreTensionLoadType type =
                    PythonValueConverter.ToEnum<PreTensionLoadType>(AsString(tensionType, "tension_type"),
                                                                 "tension_type");
                if (type != load.Type && magnitude == null)
                    throw new PythonAPIException("The argument 'magnitude' must be given when 'tension_type' changes " +
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
                RefuseOutOfPlaneComponent(load, load.Z, values[2], "direction");
                load.AutoComputeDirection = false;
                PythonModelArgumentCheck.SetEquations(new[] { load.X, load.Y, load.Z }, values, "direction");
                PythonModelArgumentCheck.CheckNonZeroDirection(new[] { load.X, load.Y, load.Z }, "direction");
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
            if (emissivity != null)
            {
                // The load clamps an emissivity outside 0 to 1
                EquationString equation = PythonValueConverter.ToEquationString(emissivity, "emissivity");
                double value = GivenEquationValue.Evaluate(load.Emissivity, equation, "emissivity");
                if (!(value >= 0 && value <= 1))
                    throw new PythonAPIException("The argument 'emissivity' must be between 0 and 1.");
                PythonModelArgumentCheck.SetEquation(load.Emissivity, equation, "emissivity");
            }
            if (cavityRadiation != null)
                load.CavityRadiation = PythonValueConverter.ToBool(cavityRadiation, "cavity_radiation");
        }
        //
        private static void SetEquation(CaeGlobals.EquationContainer container, object value, string argumentName)
        {
            if (value == null) return;
            //
            PythonModelArgumentCheck.SetEquation(container, value, argumentName);
        }
        private static void SetInPlaneEquation(Load load, EquationContainer container, object value,
                                               string argumentName)
        {
            if (value == null) return;
            //
            EquationString equation = PythonValueConverter.ToEquationString(value, argumentName);
            if (load.TwoD)
                GivenEquationValue.RefuseOutOfPlaneValue(container, equation, argumentName,
                                                         "The argument '" + argumentName + "'");
            PythonModelArgumentCheck.SetEquation(container, equation, argumentName);
        }
        private static void RefuseOutOfPlaneComponent(Load load, EquationContainer container, EquationString equation,
                                                      string argumentName)
        {
            if (!load.TwoD) return;
            //
            GivenEquationValue.RefuseOutOfPlaneValue(container, equation, argumentName,
                                                     "The third component of '" + argumentName + "'");
        }
        private static void RefuseAxisymmetricAxis(EquationContainer container, EquationString[] values,
                                                   double[] axis, string argumentName)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (GivenEquationValue.Evaluate(container, values[i], argumentName) != axis[i])
                    throw new PythonAPIException("An axisymmetric model turns about its y axis, so the argument '" +
                                                 argumentName + "' must be [" + axis[0] + ", " + axis[1] + ", " +
                                                 axis[2] + "] or be left out.");
            }
        }
        private static string FirstGiven(object value1, string name1, object value2, string name2, object value3,
                                         string name3)
        {
            if (value1 != null) return name1;
            if (value2 != null) return name2;
            if (value3 != null) return name3;
            //
            return null;
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
