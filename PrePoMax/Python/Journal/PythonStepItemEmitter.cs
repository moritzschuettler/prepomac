// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Drawing;
using CaeGlobals;

namespace PrePoMax.Python.Journal
{
    // What a boundary condition, a load and a defined field all need: the collection path inside their
    // step, and the shared trailing arguments. The values a step type derives are never emitted -
    // Complex comes from the step type, so a phase is only meaningful when the step made it so.
    public static class PythonStepItemEmitter
    {
        // Methods                                                                                                                  
        public static string GetCollection(string stepName, string collection)
        {
            return "pmx.model.steps[" + PythonValueFormatter.Text(stepName) + "]." + collection;
        }
        public static void AddAmplitude(PythonJournalWriter writer, List<string> arguments, string amplitudeName,
                                        bool replace)
        {
            amplitudeName = writer.KeptReference("pmx.model.amplitudes", amplitudeName, replace);
            if (string.IsNullOrEmpty(amplitudeName)) return;
            //
            arguments.Add("amplitude_name=" + PythonValueFormatter.Text(amplitudeName));
        }
        public static void AddCoordinateSystem(PythonJournalWriter writer, List<string> arguments,
                                               string coordinateSystemName, bool replace)
        {
            coordinateSystemName = writer.KeptReference("pmx.model.coordinate_systems", coordinateSystemName, replace);
            if (string.IsNullOrEmpty(coordinateSystemName)) return;
            //
            arguments.Add("coordinate_system_name=" + PythonValueFormatter.Text(coordinateSystemName));
        }
        // A phase belongs to a complex step, and the step type is what decides that, so it is emitted
        // only when the recorded item actually carries one
        public static void AddPhase(List<string> arguments, bool complex, EquationContainer phaseDeg)
        {
            if (!complex || phaseDeg == null) return;
            //
            arguments.Add("phase_deg=" + PythonValueFormatter.Equation(phaseDeg));
        }
        // A replace keeps a color it is not given, so a color set back to the default is written out as well
        public static void AddColor(List<string> arguments, Color color, Color defaultColor, bool replace)
        {
            if (!replace && color.ToArgb() == defaultColor.ToArgb()) return;
            //
            arguments.Add("color=" + PythonValueFormatter.Channels(color));
        }
    }
}
