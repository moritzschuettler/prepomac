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
    // A reference point is created in one of three ways, and each has its own API member: coordinates
    // typed by the user, mesh points picked from a selection, or a named region whose centre the model
    // computes. The coordinates of the latter two are derived, so they are never emitted.
    public static class PythonReferencePointEmitter
    {
        // Methods                                                                                                                  
        public static void Add(PythonJournalWriter writer, FeReferencePoint referencePoint)
        {
            Emit(writer, "add", null, referencePoint);
        }
        public static void Replace(PythonJournalWriter writer, string oldName, FeReferencePoint referencePoint)
        {
            Emit(writer, "replace", oldName, referencePoint);
        }
        private static void Emit(PythonJournalWriter writer, string verb, string oldName,
                                 FeReferencePoint referencePoint)
        {
            List<string> arguments = new List<string>();
            if (oldName != null) arguments.Add(PythonValueFormatter.Text(oldName));
            arguments.Add(PythonValueFormatter.Text(referencePoint.Name));
            //
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
            AddDisplayValues(arguments, referencePoint);
            //
            writer.Statement(writer.Call("pmx.model.reference_points", method, arguments.ToArray()));
            PythonModelItemEmitter.EmitState(writer, "pmx.model.reference_points", referencePoint);
            writer.CountEmittedCommand();
        }
        // Only what the user changed from a new reference point is worth a named argument
        private static void AddDisplayValues(List<string> arguments, FeReferencePoint referencePoint)
        {
            FeReferencePoint defaults = new FeReferencePoint("Default", 0, 0, 0);
            //
            if (referencePoint.NameVisible != defaults.NameVisible)
                arguments.Add("name_visible=" + PythonValueFormatter.Format(referencePoint.NameVisible));
            if (referencePoint.Color.ToArgb() != defaults.Color.ToArgb())
                arguments.Add("color=" + PythonValueFormatter.Channels(referencePoint.Color));
        }
    }
}
