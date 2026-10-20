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

namespace CaeGlobals
{
    public static class EquationParameterResolver
    {
        // Methods                                                                                                                  
        /// <summary>
        /// Evaluates a set of named equations into the existing parameters, in the order their dependencies
        /// allow rather than in the order they are given, and reports the ones that could not be evaluated.
        /// </summary>
        public static Dictionary<string, string> Evaluate(IList<KeyValuePair<string, string>> nameEquations,
                                                          Dictionary<string, object> propertyParameters,
                                                          out Dictionary<string, double> values)
        {
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> errors = new Dictionary<string, string>(comparer);
            values = new Dictionary<string, double>(comparer);
            //
            MyNCalc.ExistingParameters = new CaeGlobals.OrderedDictionary<string, object>("Parameters");
            if (propertyParameters != null) MyNCalc.ExistingParameters.AddRange(propertyParameters);
            if (nameEquations == null || nameEquations.Count == 0) return errors;
            // An equation may use a parameter that is defined below it, so the equations are evaluated for as
            // long as one of them succeeds and only the leftovers of the last pass are reported
            List<KeyValuePair<string, string>> unresolved = new List<KeyValuePair<string, string>>(nameEquations);
            List<KeyValuePair<string, string>> remaining;
            int resolvedCount;
            double value;
            do
            {
                resolvedCount = 0;
                remaining = new List<KeyValuePair<string, string>>();
                foreach (KeyValuePair<string, string> nameEquation in unresolved)
                {
                    try
                    {
                        value = GetValue(nameEquation.Value);
                        MyNCalc.SetParameterValue(nameEquation.Key, value);
                        values[nameEquation.Key] = value;
                        errors.Remove(nameEquation.Key);
                        resolvedCount++;
                    }
                    catch (Exception ex)
                    {
                        errors[nameEquation.Key] = ex.Message;
                        remaining.Add(nameEquation);
                    }
                }
                unresolved = remaining;
            }
            while (resolvedCount > 0 && unresolved.Count > 0);
            //
            if (unresolved.Count > 0) SetErrorsForUnresolvedNames(nameEquations, unresolved, errors, comparer);
            //
            return errors;
        }
        private static double GetValue(string equation)
        {
            double value = MyNCalc.ConvertFromString(equation == null ? "" : equation,
                                                     StringDoubleConverter.ConvertToCurrentUnits);
            if (double.IsNaN(value)) throw new CaeException("The equation value is equal to NaN.");
            if (double.IsInfinity(value)) throw new CaeException("The equation value is equal to infinity.");
            return value;
        }
        private static void SetErrorsForUnresolvedNames(IList<KeyValuePair<string, string>> nameEquations,
                                                        List<KeyValuePair<string, string>> unresolved,
                                                        Dictionary<string, string> errors, StringComparer comparer)
        {
            // The error of the last evaluation names a missing parameter but not the reason it is missing
            Dictionary<string, HashSet<string>> dependencies = GetDependencies(nameEquations, comparer);
            HashSet<string> unresolvedNames = new HashSet<string>(comparer);
            foreach (KeyValuePair<string, string> nameEquation in unresolved) unresolvedNames.Add(nameEquation.Key);
            //
            List<string> cycle;
            foreach (KeyValuePair<string, string> nameEquation in unresolved)
            {
                cycle = GetCycle(nameEquation.Key, dependencies, comparer);
                if (cycle != null)
                {
                    errors[nameEquation.Key] = "Circular reference: " + string.Join(" -> ", cycle) + ".";
                    continue;
                }
                //
                foreach (string usedName in dependencies[nameEquation.Key])
                {
                    if (unresolvedNames.Contains(usedName))
                    {
                        errors[nameEquation.Key] = "The parameter '" + usedName + "' cannot be evaluated.";
                        break;
                    }
                }
            }
        }
        private static Dictionary<string, HashSet<string>> GetDependencies(
            IList<KeyValuePair<string, string>> nameEquations, StringComparer comparer)
        {
            // A name is used in an equation in any casing, and is reported by the declared one
            Dictionary<string, string> declaredNames = new Dictionary<string, string>(comparer);
            foreach (KeyValuePair<string, string> nameEquation in nameEquations)
                declaredNames[nameEquation.Key] = nameEquation.Key;
            //
            Dictionary<string, HashSet<string>> dependencies = new Dictionary<string, HashSet<string>>(comparer);
            foreach (KeyValuePair<string, string> nameEquation in nameEquations)
                dependencies[nameEquation.Key] = GetUsedNames(nameEquation.Value, declaredNames, comparer);
            //
            return dependencies;
        }
        private static HashSet<string> GetUsedNames(string equation, Dictionary<string, string> declaredNames,
                                                    StringComparer comparer)
        {
            HashSet<string> usedNames = new HashSet<string>(comparer);
            if (equation == null) return usedNames;
            equation = equation.Trim();
            if (!equation.StartsWith("=")) return usedNames;
            //
            equation = MyNCalc.AddBracketsToSpecialParameterNames(equation.Substring(1));
            string declaredName;
            foreach (string usedName in MyNCalc.GetParameters(equation))
            {
                if (declaredNames.TryGetValue(usedName, out declaredName)) usedNames.Add(declaredName);
            }
            return usedNames;
        }
        private static List<string> GetCycle(string name, Dictionary<string, HashSet<string>> dependencies,
                                             StringComparer comparer)
        {
            List<string> path = new List<string>();
            HashSet<string> visitedNames = new HashSet<string>(comparer);
            visitedNames.Add(name);
            //
            if (AddPathToName(name, name, dependencies, visitedNames, path, comparer)) return path;
            else return null;
        }
        private static bool AddPathToName(string name, string searchedName,
                                          Dictionary<string, HashSet<string>> dependencies, HashSet<string> visitedNames,
                                          List<string> path, StringComparer comparer)
        {
            HashSet<string> usedNames;
            if (!dependencies.TryGetValue(name, out usedNames)) return false;
            //
            path.Add(name);
            foreach (string usedName in usedNames)
            {
                if (comparer.Equals(usedName, searchedName))
                {
                    path.Add(searchedName);
                    return true;
                }
                if (visitedNames.Add(usedName) &&
                    AddPathToName(usedName, searchedName, dependencies, visitedNames, path, comparer)) return true;
            }
            path.RemoveAt(path.Count - 1);
            return false;
        }
    }
}
