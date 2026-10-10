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
        // The equation setter of a container evaluates the equation and throws a CaeException that names no argument
        // on one it cannot evaluate, so every equation the API stores goes through here
        public static void SetEquation(EquationContainer container, EquationString equation, string argumentName)
        {
            try
            {
                container.Equation = equation;
            }
            catch (Exception ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is not valid. " + ex.Message);
            }
        }
        public static void SetEquation(EquationContainer container, object value, string argumentName)
        {
            SetEquation(container, PythonValueConverter.ToEquationString(value, argumentName), argumentName);
        }
        public static void SetEquations(EquationContainer[] containers, EquationString[] equations, string argumentName)
        {
            for (int i = 0; i < containers.Length; i++)
                SetEquation(containers[i], equations[i], argumentName + "[" + i + "]");
        }
        // A direction or an axis of zero length leaves the solver input without one, and every dialog that takes one
        // refuses it as well
        public static void CheckNonZeroDirection(EquationContainer[] containers, string argumentName)
        {
            double[] components = new double[containers.Length];
            for (int i = 0; i < containers.Length; i++) components[i] = containers[i].Value;
            CheckNonZeroDirection(components, argumentName);
        }
        public static void CheckNonZeroDirection(double[] components, string argumentName)
        {
            double lengthSquared = 0;
            for (int i = 0; i < components.Length; i++) lengthSquared += components[i] * components[i];
            if (double.IsNaN(lengthSquared) || double.IsInfinity(lengthSquared) || lengthSquared <= 1E-24)
                throw new PythonAPIException("The argument '" + argumentName + "' must be non-zero.");
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
