// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using CaeMesh;

namespace APIs.Python
{
    // A model setter may clamp a value through the check of its equation container, or refuse it with a message
    // that names no argument. These helpers let the API see the value the caller gave and refuse it by the
    // argument name before anything is recorded.
    internal static class PythonModelArgumentCheck
    {
        // Methods
        public static double Evaluate(EquationContainer container, EquationString equation, string argumentName)
        {
            // The check of a container is not serialized, so the copy evaluates the equation without clamping it
            EquationContainer probe = container.DeepClone();
            try
            {
                probe.SetEquation(equation.Equation);
                return probe.Value;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is not valid. " + ex.Message);
            }
        }
        // The controller and the view match part names exactly - the view hides a part by the name of its actor -
        // so a part name is passed on in the model's spelling rather than the caller's
        public static string[] ToStoredPartNames(FeMesh mesh, string[] partNames, string partKind)
        {
            string[] storedNames = new string[partNames.Length];
            for (int i = 0; i < partNames.Length; i++)
            {
                BasePart part;
                if (mesh == null || partNames[i] == null || !mesh.Parts.TryGetValue(partNames[i], out part))
                    throw new PythonAPIException("The " + partKind + " part '" + partNames[i] + "' does not exist.");
                storedNames[i] = part.Name;
            }
            //
            return storedNames;
        }
        public static void Apply(Action modelChange, string argumentName)
        {
            try
            {
                modelChange();
            }
            catch (PythonAPIException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is not valid. " + ex.Message);
            }
        }
    }
}
