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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;
using UnitsNet.Units;
using NCalc;
using System.Text.RegularExpressions;
using System.Reflection;


namespace CaeGlobals
{
    public static class MyNCalc
    {
        // Variables                                                                                                                
        public static CaeGlobals.OrderedDictionary<string, object> _existingParameters = null;
        // The parameter count grows with the model - a parameter is registered for every face of every
        // part - and an equation is evaluated once per symbol draw, so nothing that is derived from the
        // parameters may be rebuilt per evaluation. These three are, and they are dropped together
        // whenever the parameters are replaced or a name is added
        private static List<string> _sortedDottedParameterNames = null;
        private static Dictionary<string, string> _bracketedEquations = null;
        private static Dictionary<string, object> _ncalcParameters = null;
        public static CaeGlobals.OrderedDictionary<string, object> ExistingParameters
        {
            get { return _existingParameters; }
            set { _existingParameters = value; ClearParameterCaches(); }
        }
        public static Func<int, int, double> Dist;
        public static Func<int, int, double> DistX;
        public static Func<int, int, double> DistY;
        public static Func<int, int, double> DistZ;


        // Methods                                                                                                                  
        static public double ConvertFromString(string valueString, Func<string, double> ConvertToCurrentUnits)
        {
            double valueDouble;
            valueString = valueString.Trim();
            //
            if (valueString.Length == 0 || valueString == "=") return 0;   // empty string -> 0
            if (!double.TryParse(valueString, out valueDouble))
            {
                if (valueString.StartsWith("="))
                {
                    valueString = valueString.Substring(1, valueString.Length - 1);
                    valueString = AddBracketsToSpecialParameterNames(valueString);
                    //List<string> parameters = GetParameters(valueString);
                    Expression e = GetExpression(valueString);
                    if (!e.HasErrors())
                    {
                        object result = e.Evaluate();
                        if (result is bool bl) valueDouble = bl ? 1 : 0;
                        else if (result is byte byt) valueDouble = byt;
                        else if (result is decimal dec) valueDouble = (double)dec;
                        else if (result is int i) valueDouble = i;
                        else if (result is float f) valueDouble = f;
                        else if (result is double d) valueDouble = d;
                        else
                        {
                            double.TryParse(result.ToString(), out valueDouble);
                        }
                    }
                    else
                    {
                        throw new CaeException("Equation error:" + Environment.NewLine + e.Error);
                    }
                }
                else valueDouble = ConvertToCurrentUnits(valueString);
            }
            //
            return valueDouble;
        }
        static public double SolveEquation(string valueString)
        {
            double valueDouble;
            valueString = valueString.Trim();
            //
            if (valueString.Length == 0 || valueString == "=") return 0;   // empty string -> 0
            if (!double.TryParse(valueString, out valueDouble))
            {
                if (valueString.StartsWith("="))
                {
                    valueString = valueString.Substring(1, valueString.Length - 1);
                    valueString = AddBracketsToSpecialParameterNames(valueString);
                    Expression e = GetExpression(valueString);
                    if (!e.HasErrors())
                    {
                        object result = e.Evaluate();
                        if (result is bool bl) valueDouble = bl ? 1 : 0;
                        else if (result is byte byt) valueDouble = byt;
                        else if (result is decimal dec) valueDouble = (double)dec;
                        else if (result is int i) valueDouble = i;
                        else if (result is float f) valueDouble = f;
                        else if (result is double d) valueDouble = d;
                        else
                        {
                            double.TryParse(result.ToString(), out valueDouble);
                        }
                    }
                    else
                    {
                        throw new CaeException("Equation error:" + Environment.NewLine + e.Error);
                    }
                }
                else throw new CaeException("Equation error:" + Environment.NewLine + "Equation must start with = sign.");
            }
            //
            return valueDouble;
        }
        static public double[] SolveArrayEquation(string equation)
        {
            double[] values;
            string scalarEquation = equation;
            equation = equation.Trim();
            //
            if (equation.Length == 0 || equation == "=")
                throw new CaeException("Equation error:" + Environment.NewLine + "Equation cannot be evaluated.");
            //
            if (equation.StartsWith("="))
            {
                equation = equation.Substring(1, equation.Length - 1);
                equation = AddBracketsToSpecialParameterNames(equation);
                //
                Expression e = GetArrayExpression(equation);
                if (!e.HasErrors())
                {
                    object result = e.Evaluate();
                    if (result is List<object> list)
                    {
                        // Array equation
                        if (list.Count > 0)
                        {
                            int count = 0;
                            values = new double[list.Count];
                            foreach (var obj in list)
                            {
                                if (obj is bool bl) values[count++] = bl ? 1 : 0;
                                else if (obj is byte byt) values[count++] = byt;
                                else if (obj is decimal dec) values[count++] = (double)dec;
                                else if (obj is int i) values[count++] = i;
                                else if (obj is float f) values[count++] = f;
                                else if (obj is double d) values[count++] = d;
                                else count++;
                            }
                        }
                        // Scalar equation
                        else
                        {
                            double value = ConvertFromString(scalarEquation, null);
                            return new double[] { value };
                        }
                    }
                    else
                    {
                        throw new CaeException("Equation error:" + Environment.NewLine + "Equation return type unrecognized.");
                    }
                }
                else
                {
                    throw new CaeException("Equation error:" + Environment.NewLine + e.Error);
                }
            }
            else throw new CaeException("Equation error:" + Environment.NewLine + "Equation must start with = sign.");
            //
            return values;
        }
        static public bool HasErrors(string equation, out HashSet<string> parameterNames)
        {
            parameterNames = null;
            try
            {
                double valueDouble;
                //
                equation = equation.Trim();
                //
                if (equation.Length == 0 || equation == "=") return false;
                if (!double.TryParse(equation, out valueDouble))
                {
                    if (equation.StartsWith("="))
                    {
                        equation = equation.Substring(1, equation.Length - 1);
                        equation = AddBracketsToSpecialParameterNames(equation);
                        Expression e = GetExpression(equation);
                        parameterNames = GetParameters(equation);
                        //
                        if (!e.HasErrors())
                        {
                            object result = e.Evaluate();
                        }
                        return e.HasErrors();
                    }
                    else return false;
                }
                //
                return false;
            }
            catch
            {
                return true;
            }
        }
        //
        static public Expression GetExpression(string expression)
        {
            Expression e = new Expression(expression, EvaluateOptions.IgnoreCase);
            e.Parameters = GetNCalcParameters();
            AddFunctionsToExpression(e);
            return e;
        }
        static public Expression GetArrayExpression(string expression)
        {
            // IterateParameters writes the current value of each enumerated parameter back into
            // Parameters, so this must not be given the shared dictionary
            Expression e = new Expression(expression, EvaluateOptions.IgnoreCase | EvaluateOptions.IterateParameters);
            AddParametersToExpression(e);
            return e;
        }
        static private void AddParametersToExpression(Expression e)
        {
            // Add constants
            e.Parameters.Add("pi", Math.PI);
            e.Parameters.Add("Pi", Math.PI);
            //
            if (_existingParameters != null)
            {
                foreach (var entry in _existingParameters) e.Parameters.Add(entry.Key, entry.Value);
            }
        }
        /// <summary>
        /// Sets the value of a single parameter. Writing to ExistingParameters directly leaves the
        /// prepared parameters of the expressions holding the previous value
        /// </summary>
        static public void SetParameterValue(string name, object value)
        {
            if (_existingParameters == null) return;
            //
            bool newName = !_existingParameters.ContainsKey(name);
            _existingParameters[name] = value;
            // A name that already exists cannot change how an equation is rewritten or which
            // parameters an expression is given, so only its value is carried over
            if (newName) ClearParameterCaches();
            else if (_ncalcParameters != null) _ncalcParameters[name] = value;
        }
        static private void ClearParameterCaches()
        {
            _sortedDottedParameterNames = null;
            _bracketedEquations = null;
            _ncalcParameters = null;
        }
        static private Dictionary<string, object> GetNCalcParameters()
        {
            PrepareParameterCaches();
            return _ncalcParameters;
        }
        static private void PrepareParameterCaches()
        {
            if (_ncalcParameters != null) return;
            //
            _sortedDottedParameterNames = new List<string>();
            _ncalcParameters = new Dictionary<string, object>();
            _bracketedEquations = new Dictionary<string, string>();
            //
            if (_existingParameters != null)
            {
                foreach (var entry in _existingParameters)
                {
                    if (entry.Key.Contains('.')) _sortedDottedParameterNames.Add(entry.Key);
                    _ncalcParameters[entry.Key] = entry.Value;
                }
                // Longest first, so that a name that contains a shorter one is replaced before it
                _sortedDottedParameterNames.Sort((first, second) => second.Length.CompareTo(first.Length));
            }
            // The constants are added last so that they cannot be overwritten by a parameter
            _ncalcParameters["pi"] = Math.PI;
            _ncalcParameters["Pi"] = Math.PI;
        }
        static private void AddFunctionsToExpression(Expression e)
        {
            e.EvaluateFunction += ExtensionFunctions;
        }
        static public HashSet<string> GetParameters(string expression)
        {
            HashSet<string> parameters = new HashSet<string>();
            Expression e = new Expression(expression);
            //
            e.EvaluateFunction += delegate (string name, FunctionArgs args) {
                args.EvaluateParameters();
                args.Result = 0;
            };
            e.EvaluateParameter += delegate (string name, ParameterArgs args) {
                parameters.Add(name);
                args.Result = 0;
            };
            try
            {
                e.Evaluate();
            }
            catch
            {
            }
            return parameters;
        }
        //
        static public string AddBracketsToSpecialParameterNames(string expr)
        {
            PrepareParameterCaches();
            // The rewriting depends on the equation and on the parameter names, and both are stable
            // between the evaluations of one drawn model
            string bracketedExpr;
            if (_bracketedEquations.TryGetValue(expr, out bracketedExpr)) return bracketedExpr;
            //
            bracketedExpr = GetExprWithBracketedParameterNames(expr);
            _bracketedEquations[expr] = bracketedExpr;
            return bracketedExpr;
        }
        static private string GetExprWithBracketedParameterNames(string expr)
        {
            // Step 1: Replace each parameter with a unique placeholder
            int count = 0;
            string placeholder;
            string bracketedParam;
            Dictionary<string, string> placeholders = new Dictionary<string, string>();
            //
            foreach (var param in _sortedDottedParameterNames)
            {
                // Create a unique placeholder
                placeholder = $"@{count++}@";
                bracketedParam = $"[{param}]";
                // Replace all instances of the parameter with the placeholder
                if (expr.Contains(bracketedParam))
                {
                    expr = expr.Replace(bracketedParam, placeholder);
                    // Store the mapping between placeholder and actual parameter
                    placeholders[placeholder] = param;
                }
                else if (expr.Contains(param))
                {
                    expr = expr.Replace(param, placeholder);
                    // Store the mapping between placeholder and actual parameter
                    placeholders[placeholder] = param;
                }
            }
            // Step 2: Replace the placeholders with the bracketed parameter names
            string bracketed;
            foreach (var entry in placeholders)
            {
                bracketed = $"[{entry.Value}]";
                expr = expr.Replace(entry.Key, bracketed);
            }
            //
            return expr;
        }
        static public string[] GetReservedParameterNames()
        {
            return new string[] { "Pi", "pi", "x", "y", "z" };
        }
        //
        static public string[] GetFunctionConstants()
        {
            return new string[] { "Pi" };
        }
        static public string[] GetFunctionSnippets()
        {
            return new string[] { "Abs(^)",
                                  "Acos(^)",
                                  "Asin(^)",
                                  "Atan(^)",
                                  "Ceiling(^)",
                                  "Cos(^)",
                                  "Dist(^, )",
                                  "DistX(^, )",
                                  "DistY(^, )",
                                  "DistZ(^, )",
                                  "Exp(^)",
                                  "Floor(^)",
                                  "IEEERemainder(^, )",
                                  "Ln(^)",
                                  "Log(^, )",
                                  "Log10(^)",
                                  "Max(^, )",
                                  "Min(^, )",
                                  "Pow(^, )",
                                  "Round(^, )",
                                  "Sign(^)",
                                  "Sin(^)",
                                  "Sqrt(^)",
                                  "Tan(^)",
                                  "Truncate(^)",
                                  "If(^, , )" };
        }
        // User functions
        private static void ExtensionFunctions(string name, FunctionArgs functionArgs)
        {
            if (name.ToLower() == "ln")
            {
                double param1 = Convert.ToDouble(functionArgs.Parameters[0].Evaluate());
                //
                functionArgs.Result = (double)Math.Log(param1, Math.E);
            }
            else if (name.ToLower() == "dist")
            {
                if (functionArgs.Parameters.Length != 2)
                    throw new ArgumentException("Dist() requires two parameters: Dist(NodeId1, NodeId2).");
                functionArgs.EvaluateParameters();
                int id1 = Convert.ToInt32(functionArgs.Parameters[0].Evaluate());
                int id2 = Convert.ToInt32(functionArgs.Parameters[1].Evaluate());
                //
                if (Dist != null) functionArgs.Result = Dist(id1, id2);
                else functionArgs.Result = double.NaN;
            }
            else if (name.ToLower() == "distx")
            {
                if (functionArgs.Parameters.Length != 2)
                    throw new ArgumentException("DistX() requires two parameters: DistX(NodeId1, NodeId2).");
                functionArgs.EvaluateParameters();
                int id1 = Convert.ToInt32(functionArgs.Parameters[0].Evaluate());
                int id2 = Convert.ToInt32(functionArgs.Parameters[1].Evaluate());
                //
                if (DistX != null) functionArgs.Result = DistX(id1, id2);
                else functionArgs.Result = double.NaN;
            }
            else if (name.ToLower() == "disty")
            {
                if (functionArgs.Parameters.Length != 2)
                    throw new ArgumentException("DistY() requires two parameters: DistY(NodeId1, NodeId2).");
                functionArgs.EvaluateParameters();
                int id1 = Convert.ToInt32(functionArgs.Parameters[0].Evaluate());
                int id2 = Convert.ToInt32(functionArgs.Parameters[1].Evaluate());
                //
                if (DistY != null) functionArgs.Result = DistY(id1, id2);
                else functionArgs.Result = double.NaN;
            }
            else if (name.ToLower() == "distz")
            {
                if (functionArgs.Parameters.Length != 2)
                    throw new ArgumentException("DistZ() requires two parameters: DistZ(NodeId1, NodeId2).");
                functionArgs.EvaluateParameters();
                int id1 = Convert.ToInt32(functionArgs.Parameters[0].Evaluate());
                int id2 = Convert.ToInt32(functionArgs.Parameters[1].Evaluate());
                //
                if (DistZ != null) functionArgs.Result = DistZ(id1, id2);
                else functionArgs.Result = double.NaN;
            }
        }
    }
}
