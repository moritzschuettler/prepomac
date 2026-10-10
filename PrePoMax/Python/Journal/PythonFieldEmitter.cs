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
using CaeMesh;
using CaeModel;

namespace PrePoMax.Python.Journal
{
    // Coordinate systems, amplitudes, distributions and initial conditions. A coordinate system point
    // is either typed or picked, and the picked one keeps its selection, because the coordinates the
    // model computed from it are derived.
    public static class PythonFieldEmitter
    {
        // Methods                                                                                                                  
        public static void CoordinateSystem(PythonJournalWriter writer, string oldName,
                                            CoordinateSystem coordinateSystem)
        {
            const string collection = "pmx.model.coordinate_systems";
            bool picked = IsPicked(coordinateSystem);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(coordinateSystem.Name));
            //
            string method = oldName == null ? "add" : "replace";
            if (picked)
            {
                method += "_from_selections";
                AddPoint(writer, arguments, "origin", coordinateSystem.CenterCreatedFrom,
                         coordinateSystem.CenterCreationData, coordinateSystem.X1, coordinateSystem.Y1,
                         coordinateSystem.Z1);
                AddPoint(writer, arguments, "point_on_x", coordinateSystem.PointXCreatedFrom,
                         coordinateSystem.PointXCreationData, coordinateSystem.X2, coordinateSystem.Y2,
                         coordinateSystem.Z2);
                AddPoint(writer, arguments, "point_in_xy", coordinateSystem.PointXYCreatedFrom,
                         coordinateSystem.PointXYCreationData, coordinateSystem.X3, coordinateSystem.Y3,
                         coordinateSystem.Z3);
            }
            else
            {
                arguments.Add(Point(coordinateSystem.X1, coordinateSystem.Y1, coordinateSystem.Z1));
                arguments.Add(Point(coordinateSystem.X2, coordinateSystem.Y2, coordinateSystem.Z2));
                arguments.Add(Point(coordinateSystem.X3, coordinateSystem.Y3, coordinateSystem.Z3));
            }
            arguments.Add("coordinate_system_type=" +
                          PythonValueFormatter.Text(coordinateSystem.Type.ToString()));
            arguments.Add("name_visible=" + PythonValueFormatter.Format(coordinateSystem.NameVisible));
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, coordinateSystem);
            writer.CountEmittedCommand();
        }
        public static void Amplitude(PythonJournalWriter writer, string oldName, Amplitude amplitude)
        {
            const string collection = "pmx.model.amplitudes";
            AmplitudeTabular tabular = amplitude as AmplitudeTabular;
            if (tabular == null)
                throw new NotSupportedException("the amplitude type " + amplitude.GetType().Name +
                                                " has no API form");
            //
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(amplitude.Name));
            arguments.Add(Table(tabular.TimeAmplitude));
            arguments.Add("time_span=" + PythonValueFormatter.Text(tabular.TimeSpan.ToString()));
            // A replace keeps the shifts it is not given, so a shift back to zero is written out there
            if (oldName != null || amplitude.ShiftX != 0)
                arguments.Add("shift_time=" + PythonValueFormatter.Number(amplitude.ShiftX));
            if (oldName != null || amplitude.ShiftY != 0)
                arguments.Add("shift_amplitude=" + PythonValueFormatter.Number(amplitude.ShiftY));
            //
            writer.Statement(writer.Call(collection, oldName == null ? "add" : "replace", arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, amplitude);
            writer.CountEmittedCommand();
        }
        public static void Distribution(PythonJournalWriter writer, string oldName, Distribution distribution)
        {
            const string collection = "pmx.model.distributions";
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(distribution.Name));
            //
            string method;
            if (distribution is DistributionFromEquation fromEquation)
            {
                method = (oldName == null ? "add" : "replace") + "_from_equation";
                // A vector distribution keeps its magnitude equation unused, so the three directions are written
                if (fromEquation.DistributionType == DistributionTypeEnum.Vector)
                    arguments.Add(PythonValueFormatter.List(new string[] { fromEquation.EquationD1,
                                                                           fromEquation.EquationD2,
                                                                           fromEquation.EquationD3 }));
                else arguments.Add(PythonValueFormatter.Text(fromEquation.EquationMagnitude));
            }
            else if (distribution is DistributionFromFile fromFile)
            {
                method = (oldName == null ? "add" : "replace") + "_from_file";
                arguments.Add(writer.Path(fromFile.FileName));
                arguments.Add("interpolator_type=" +
                              PythonValueFormatter.Text(fromFile.InterpolatorType.ToString()));
                // The closest point interpolation has no radius, which the dialog hides and the API refuses
                if (fromFile.InterpolatorType != CaeResults.CloudInterpolatorEnum.ClosestPoint)
                    arguments.Add("interpolator_radius=" +
                                  PythonValueFormatter.Equation(fromFile.InterpolatorRadius));
                arguments.Add("scale=" + Point(fromFile.ScaleX, fromFile.ScaleY, fromFile.ScaleZ));
                arguments.Add("translate=" +
                              Point(fromFile.TranslateX, fromFile.TranslateY, fromFile.TranslateZ));
            }
            else throw new NotSupportedException("the distribution type " + distribution.GetType().Name +
                                                 " has no API form");
            // A replace keeps the coordinate system it is not given, so the global one is written out there
            if (oldName != null ||
                distribution.CoordinateSystemName != CaeMesh.CoordinateSystem.DefaultCoordinateSystemName)
                arguments.Add("coordinate_system_name=" + PythonValueFormatter.Text(distribution.CoordinateSystemName));
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, distribution);
            writer.CountEmittedCommand();
        }
        public static void InitialCondition(PythonJournalWriter writer, string oldName,
                                            InitialCondition condition)
        {
            const string collection = "pmx.model.initial_conditions";
            if (condition is InitialVelocity initialVelocity) condition = ToTranslationalVelocity(initialVelocity);
            string region = PythonRegionEmitter.Emit(writer, condition.RegionName, condition.RegionType,
                                                     condition.CreationData);
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(condition.Name));
            arguments.Add(region);
            //
            string verb = oldName == null ? "add" : "replace";
            string method;
            if (condition is InitialTemperature temperature)
            {
                method = verb + "_temperature";
                arguments.Add(PythonValueFormatter.Equation(temperature.Temperature));
                if (!string.IsNullOrEmpty(temperature.DistributionName))
                    arguments.Add("distribution_name=" +
                                  PythonValueFormatter.Text(temperature.DistributionName));
            }
            else if (condition is InitialTranslationalVelocity translational)
            {
                method = verb + "_translational_velocity";
                arguments.Add(PythonValueFormatter.Equation(translational.V1));
                arguments.Add(PythonValueFormatter.Equation(translational.V2));
                arguments.Add(PythonValueFormatter.Equation(translational.V3));
            }
            else if (condition is InitialAngularVelocity angular)
            {
                method = verb + "_angular_velocity";
                arguments.Add(Point(angular.X, angular.Y, angular.Z));
                arguments.Add(Point(angular.N1, angular.N2, angular.N3));
                arguments.Add(PythonValueFormatter.Equation(angular.RotationalSpeed));
            }
            else throw new NotSupportedException("the initial condition type " + condition.GetType().Name +
                                                 " has no API form");
            PythonStepItemEmitter.AddColor(arguments, condition.Color,
                                           new InitialTemperature("Default", null, RegionTypeEnum.Selection, 0,
                                                                  false).Color, oldName != null);
            //
            writer.Statement(writer.Call(collection, method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, collection, condition);
            writer.CountEmittedCommand();
        }
        // A v2.2.2 history adds the initial velocity that opening a file converts into a translational one
        // applied to the parts its selection picked, so it is written as that conversion makes it. The
        // conversion changes the selection it is given, so it is given a copy.
        private static InitialTranslationalVelocity ToTranslationalVelocity(InitialVelocity initialVelocity)
        {
            InitialTranslationalVelocity translational = new InitialTranslationalVelocity(initialVelocity);
            if (initialVelocity.CreationData == null) return translational;
            //
            translational.RegionType = RegionTypeEnum.Selection;
            translational.CreationData = initialVelocity.CreationData.DeepCopy(true, true);
            foreach (var node in translational.CreationData.Nodes)
            {
                if (node is SelectionNodeMouse selectionNodeMouse) selectionNodeMouse.SelectBy = vtkSelectBy.GeometryPart;
            }
            translational.CreationData.SelectItem = vtkSelectItem.Geometry;
            translational.CreationIds = initialVelocity.CreationIds;
            //
            return translational;
        }
        private static bool IsPicked(CoordinateSystem coordinateSystem)
        {
            return coordinateSystem.CenterCreationData != null || coordinateSystem.PointXCreationData != null ||
                   coordinateSystem.PointXYCreationData != null;
        }
        // A point of a picked coordinate system is either its own selection or a typed coordinate, and
        // the two arguments are named apart
        private static void AddPoint(PythonJournalWriter writer, List<string> arguments, string name,
                                     CsPointCreatedFromEnum createdFrom, Selection creationData,
                                     EquationContainer x, EquationContainer y, EquationContainer z)
        {
            if (creationData != null && creationData.Nodes != null && creationData.Nodes.Count > 0)
            {
                arguments.Add(name + "_created_from=" + PythonValueFormatter.Text(createdFrom.ToString()));
                arguments.Add(name + "_selection=" + PythonSelectionEmitter.Emit(writer, creationData));
            }
            else arguments.Add(name + "=" + Point(x, y, z));
        }
        private static string Point(EquationContainer x, EquationContainer y, EquationContainer z)
        {
            return "[" + PythonValueFormatter.Equation(x) + ", " + PythonValueFormatter.Equation(y) + ", " +
                   PythonValueFormatter.Equation(z) + "]";
        }
        private static string Table(double[][] table)
        {
            if (table == null) return "None";
            //
            List<string> rows = new List<string>();
            foreach (double[] row in table) rows.Add(PythonValueFormatter.List(row));
            //
            return "[" + string.Join(", ", rows) + "]";
        }
    }
}
