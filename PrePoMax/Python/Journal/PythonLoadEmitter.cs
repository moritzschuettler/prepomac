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
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // Every load type has its own API member. What the model computes from the user's inputs is never
    // emitted: the magnitude of a force comes from its components, the squared rotational speed from
    // the speed, and the axisymmetric flag from the model space.
    public static class PythonLoadEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, string stepName, Load load)
        {
            Emit(writer, stepName, "add", null, load);
        }
        public static void Replace(PythonJournalWriter writer, string stepName, string oldName, Load load)
        {
            Emit(writer, stepName, "replace", oldName, load);
        }
        private static void Emit(PythonJournalWriter writer, string stepName, string verb, string oldName,
                                 Load load)
        {
            string collection = PythonStepItemEmitter.GetCollection(stepName, "loads");
            if (load is GravityLoad || load is CentrifLoad || load is BodyFlux)
                PythonRegionEmitter.RefuseSelectionOtherThanParts(load.CreationData, "the region of the load '" +
                                                                                    load.Name + "'");
            string region = PythonRegionEmitter.Emit(writer, load.RegionName, load.RegionType, load.CreationData);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(load.Name));
            arguments.Add(region);
            //
            bool replace = oldName != null;
            string method = verb + "_" + AddTypeArguments(arguments, load, replace);
            //
            PythonStepItemEmitter.AddAmplitude(arguments, load.AmplitudeName);
            // The dialog offers a coordinate system for a concentrated force and a surface traction only, and the
            // API refuses one elsewhere
            if (load is CLoad || load is STLoad)
                PythonStepItemEmitter.AddCoordinateSystem(arguments, load.CoordinateSystemName);
            // A pre-tension is complex in a steady state dynamics step, but the solver input carries no phase for it
            PythonStepItemEmitter.AddPhase(arguments, load.Complex && !(load is PreTensionLoad), load.PhaseDeg);
            PythonStepItemEmitter.AddColor(arguments, load.Color, DefaultColor(), replace);
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, load);
            writer.CountEmittedCommand();
        }
        // A replace keeps every value it is not given, so what an add leaves to the default - a flag that is off,
        // the surface normal - is written out on a replace
        private static string AddTypeArguments(List<string> arguments, Load load, bool replace)
        {
            if (load is CLoad concentratedForce)
            {
                AddComponent(arguments, "f1", concentratedForce.F1);
                AddComponent(arguments, "f2", concentratedForce.F2);
                AddComponent(arguments, "f3", concentratedForce.F3);
                return "concentrated_force";
            }
            if (load is MomentLoad moment)
            {
                AddComponent(arguments, "m1", moment.M1);
                AddComponent(arguments, "m2", moment.M2);
                AddComponent(arguments, "m3", moment.M3);
                return "moment";
            }
            if (load is ImportedPressure importedPressure)
            {
                arguments.Add(PythonValueFormatter.Path(importedPressure.FileName));
                arguments.Add(PythonValueFormatter.Text(importedPressure.PressureTime));
                arguments.Add(PythonValueFormatter.Text(importedPressure.PressureVariableName));
                arguments.Add("interpolator_type=" +
                              PythonValueFormatter.Text(importedPressure.InterpolatorType.ToString()));
                arguments.Add("magnitude_factor=" + PythonValueFormatter.Equation(importedPressure.MagnitudeFactor));
                arguments.Add("geometry_scale_factor=" +
                              PythonValueFormatter.Equation(importedPressure.GeometryScaleFactor));
                return "imported_pressure";
            }
            if (load is HydrostaticPressure hydrostatic)
            {
                arguments.Add("first_point=" + Point(hydrostatic.X1, hydrostatic.Y1, hydrostatic.Z1));
                arguments.Add("second_point=" + Point(hydrostatic.X2, hydrostatic.Y2, hydrostatic.Z2));
                arguments.Add("direction=" + Point(hydrostatic.N1, hydrostatic.N2, hydrostatic.N3));
                arguments.Add("first_point_pressure=" +
                              PythonValueFormatter.Equation(hydrostatic.FirstPointPressure));
                arguments.Add("second_point_pressure=" +
                              PythonValueFormatter.Equation(hydrostatic.SecondPointPressure));
                arguments.Add("cutoff=" +
                              PythonValueFormatter.Text(hydrostatic.HydrostaticPressureCutoff.ToString()));
                return "hydrostatic_pressure";
            }
            if (load is DLoad pressure)
            {
                arguments.Add(PythonValueFormatter.Equation(pressure.Magnitude));
                AddName(arguments, "distribution_name", pressure.DistributionName);
                return "pressure";
            }
            if (load is STLoad surfaceTraction)
            {
                // The API refuses the group of components the distribution leaves without effect
                if (surfaceTraction.DistributionName == Distribution.DefaultDistributionName)
                {
                    AddComponent(arguments, "f1", surfaceTraction.F1);
                    AddComponent(arguments, "f2", surfaceTraction.F2);
                    AddComponent(arguments, "f3", surfaceTraction.F3);
                }
                else
                {
                    AddComponent(arguments, "p1", surfaceTraction.P1);
                    AddComponent(arguments, "p2", surfaceTraction.P2);
                    AddComponent(arguments, "p3", surfaceTraction.P3);
                }
                AddName(arguments, "distribution_name", surfaceTraction.DistributionName);
                return "surface_traction";
            }
            if (load is ShellEdgeLoad shellEdge)
            {
                arguments.Add(PythonValueFormatter.Equation(shellEdge.Magnitude));
                return "shell_edge_load";
            }
            if (load is GravityLoad gravity)
            {
                AddComponent(arguments, "f1", gravity.F1);
                AddComponent(arguments, "f2", gravity.F2);
                AddComponent(arguments, "f3", gravity.F3);
                return "gravity";
            }
            if (load is CentrifLoad centrifugal)
            {
                arguments.Add("axis_point=" + Point(centrifugal.X, centrifugal.Y, centrifugal.Z));
                arguments.Add("axis_direction=" + Point(centrifugal.N1, centrifugal.N2, centrifugal.N3));
                arguments.Add("rotational_speed=" + PythonValueFormatter.Equation(centrifugal.RotationalSpeed));
                return "centrifugal";
            }
            if (load is PreTensionLoad preTension)
            {
                bool isDisplacement = preTension.Type == PreTensionLoadType.Displacement;
                arguments.Add(PythonValueFormatter.Equation(isDisplacement ? preTension.DisplacementMagnitude
                                                                           : preTension.ForceMagnitude));
                if (!preTension.AutoComputeDirection)
                    arguments.Add("direction=" + Point(preTension.X, preTension.Y, preTension.Z));
                else if (replace) arguments.Add("auto_direction=True");
                arguments.Add("tension_type=" + PythonValueFormatter.Text(preTension.Type.ToString()));
                return "pre_tension";
            }
            if (load is CFlux concentratedFlux)
            {
                arguments.Add(PythonValueFormatter.Equation(concentratedFlux.Magnitude));
                if (concentratedFlux.AddFlux) arguments.Add("add_flux=True");
                else if (replace) arguments.Add("add_flux=False");
                return "concentrated_flux";
            }
            if (load is DFlux surfaceFlux)
            {
                arguments.Add(PythonValueFormatter.Equation(surfaceFlux.Magnitude));
                return "surface_flux";
            }
            if (load is BodyFlux bodyFlux)
            {
                arguments.Add(PythonValueFormatter.Equation(bodyFlux.Magnitude));
                return "body_flux";
            }
            if (load is FilmHeatTransfer film)
            {
                arguments.Add(PythonValueFormatter.Equation(film.SinkTemperature));
                arguments.Add(PythonValueFormatter.Equation(film.FilmCoefficient));
                AddName(arguments, "coefficient_amplitude_name", film.CoefficientAmplitudeName);
                return "film_heat_transfer";
            }
            if (load is RadiationHeatTransfer radiation)
            {
                arguments.Add(PythonValueFormatter.Equation(radiation.SinkTemperature));
                arguments.Add(PythonValueFormatter.Equation(radiation.Emissivity));
                if (radiation.CavityRadiation)
                {
                    arguments.Add("cavity_radiation=True");
                    AddName(arguments, "cavity_name", radiation.CavityName);
                }
                else if (replace) arguments.Add("cavity_radiation=False");
                AddName(arguments, "emissivity_amplitude_name", radiation.EmissivityAmplitudeName);
                return "radiation_heat_transfer";
            }
            // Kept only so that older files still open; the API offers no load that reads a traction field
            if (load is ImportedSTLoad)
                throw new NotSupportedException("an imported surface traction is a load of older versions, kept " +
                                                "only so that their files still open, and the API has no load " +
                                                "that reads a surface traction from a file");
            //
            throw new NotSupportedException("the load type " + load.GetType().Name + " has no API form");
        }
        // A component that is not applied carries a NaN, and leaving the argument out is what the API
        // takes for that
        private static void AddComponent(List<string> arguments, string name, EquationContainer value)
        {
            if (value == null || double.IsNaN(value.Value)) return;
            //
            arguments.Add(name + "=" + PythonValueFormatter.Equation(value));
        }
        private static void AddName(List<string> arguments, string name, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            //
            arguments.Add(name + "=" + PythonValueFormatter.Text(value));
        }
        private static string Point(EquationContainer x, EquationContainer y, EquationContainer z)
        {
            return "[" + PythonValueFormatter.Equation(x) + ", " + PythonValueFormatter.Equation(y) + ", " +
                   PythonValueFormatter.Equation(z) + "]";
        }
        private static System.Drawing.Color DefaultColor()
        {
            return new CLoad("Default", null, RegionTypeEnum.Selection, 0, 0, 0, false, false, 0).Color;
        }
    }
}
