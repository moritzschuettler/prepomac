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
using CaeMesh;

namespace PrePoMax.Python.Journal
{
    // The post-processing side. Everything here reaches the result that is current when the command
    // replays, which is what the recorded session did too - a result command carries no result name,
    // because the application applies it to the selected result.
    public static class PythonResultEmitter
    {
        // Variables                                                                                                                
        public const string Mesh = "pmx.results.current.mesh";
        public const string Parts = Mesh + ".parts";
        public const string ReferencePoints = Mesh + ".reference_points";
        public const string CoordinateSystems = Mesh + ".coordinate_systems";


        // Methods                                                                                                                  
        public static void ReferencePoint(PythonJournalWriter writer, string oldName,
                                          FeReferencePoint referencePoint)
        {
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(referencePoint.Name));
            //
            string verb = oldName == null ? "add" : "replace";
            string method = verb;
            FeReferencePointCreatedFrom createdFrom = referencePoint.CreatedFrom;
            //
            if (createdFrom == FeReferencePointCreatedFrom.Coordinates)
            {
                arguments.Add("[" + PythonValueFormatter.Equation(referencePoint.X) + ", " +
                              PythonValueFormatter.Equation(referencePoint.Y) + ", " +
                              PythonValueFormatter.Equation(referencePoint.Z) + "]");
            }
            else if (createdFrom == FeReferencePointCreatedFrom.OnPoint ||
                     createdFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                     createdFrom == FeReferencePointCreatedFrom.CircleCenter)
            {
                method = verb + "_from_selection";
                arguments.Add(PythonValueFormatter.Text(createdFrom.ToString()));
                arguments.Add(PythonSelectionEmitter.Emit(writer, referencePoint.CreationData));
            }
            else if (createdFrom == FeReferencePointCreatedFrom.CenterOfGravity ||
                     createdFrom == FeReferencePointCreatedFrom.BoundingBoxCenter)
            {
                method = verb + "_from_region";
                arguments.Add(PythonValueFormatter.Text(createdFrom.ToString()));
                arguments.Add(PythonValueFormatter.Text(referencePoint.RegionType.ToString()));
                arguments.Add(PythonValueFormatter.Text(referencePoint.RegionName));
            }
            else throw new NotSupportedException("a reference point created from " + createdFrom +
                                                 " has no API form");
            //
            FeReferencePoint defaults = new FeReferencePoint("Default", 0, 0, 0);
            if (referencePoint.NameVisible != defaults.NameVisible)
                arguments.Add("name_visible=" + PythonValueFormatter.Format(referencePoint.NameVisible));
            if (referencePoint.Color.ToArgb() != defaults.Color.ToArgb())
                arguments.Add("color=" + PythonValueFormatter.Channels(referencePoint.Color));
            //
            writer.Statement(writer.Call(ReferencePoints, method, arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        public static void CoordinateSystem(PythonJournalWriter writer, string oldName,
                                            CoordinateSystem coordinateSystem)
        {
            bool picked = coordinateSystem.CenterCreationData != null ||
                          coordinateSystem.PointXCreationData != null ||
                          coordinateSystem.PointXYCreationData != null;
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(coordinateSystem.Name));
            //
            string method = oldName == null ? "add" : "replace";
            // The result side names the type before the points, unlike the model side
            if (picked)
            {
                method += "_from_selections";
                arguments.Add("coordinate_system_type=" +
                              PythonValueFormatter.Text(coordinateSystem.Type.ToString()));
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
                arguments.Add("coordinate_system_type=" +
                              PythonValueFormatter.Text(coordinateSystem.Type.ToString()));
            }
            arguments.Add("name_visible=" + PythonValueFormatter.Format(coordinateSystem.NameVisible));
            //
            writer.Statement(writer.Call(CoordinateSystems, method, arguments.ToArray()));
            writer.CountEmittedCommand();
        }
        private static void AddPoint(PythonJournalWriter writer, List<string> arguments, string name,
                                     CsPointCreatedFromEnum createdFrom, CaeGlobals.Selection creationData,
                                     CaeGlobals.EquationContainer x, CaeGlobals.EquationContainer y,
                                     CaeGlobals.EquationContainer z)
        {
            if (creationData != null && creationData.Nodes != null && creationData.Nodes.Count > 0)
            {
                arguments.Add(name + "_created_from=" + PythonValueFormatter.Text(createdFrom.ToString()));
                arguments.Add(name + "_selection=" + PythonSelectionEmitter.Emit(writer, creationData));
            }
            else arguments.Add(name + "=" + Point(x, y, z));
        }
        private static string Point(CaeGlobals.EquationContainer x, CaeGlobals.EquationContainer y,
                                    CaeGlobals.EquationContainer z)
        {
            return "[" + PythonValueFormatter.Equation(x) + ", " + PythonValueFormatter.Equation(y) + ", " +
                   PythonValueFormatter.Equation(z) + "]";
        }
    }
}
