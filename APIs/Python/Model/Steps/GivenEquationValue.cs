// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;

namespace APIs.Python.Model
{
    // A model item container replaces a value it does not take - an out of plane component of a two dimensional
    // model, an emissivity above one - as soon as it is given one, so the value a caller gave is read from a copy
    // of the container, which carries the unit converter but not the check.
    internal static class GivenEquationValue
    {
        // Methods
        public static double Evaluate(EquationContainer container, EquationString equation, string argumentName)
        {
            EquationContainer copy = container.DeepClone();
            try
            {
                copy.Equation = equation;
            }
            catch (CaeException ex)
            {
                throw new PythonAPIException("The argument '" + argumentName + "' is not valid: " + ex.Message);
            }
            return copy.Value;
        }
        public static void RefuseOutOfPlaneValue(EquationContainer container, EquationString equation,
                                                 string argumentName, string component)
        {
            double value = Evaluate(container, equation, argumentName);
            if (value != 0)
                throw new PythonAPIException(component + " is " + value + ", but a two dimensional model has no " +
                                             "value out of its plane. Give 0 or leave it out.");
        }
    }
}
