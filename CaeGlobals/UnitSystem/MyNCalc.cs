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
        public static OrderedDictionary<string, object> ExistingParameters = null;


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
                    valueString = PreprocessExpression(valueString);
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
                equation = PreprocessExpression(equation);
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
            double valueDouble;
            parameterNames = null;
            //
            equation = equation.Trim();
            //
            if (equation.Length == 0 || equation == "=") return false;
            if (!double.TryParse(equation, out valueDouble))
            {
                if (equation.StartsWith("="))
                {
                    equation = equation.Substring(1, equation.Length - 1);
                    equation = PreprocessExpression(equation);
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
        static public Expression GetExpression(string expression)
        {
            Expression e = new Expression(expression, EvaluateOptions.IgnoreCase);
            // Add constants
            e.Parameters.Add("pi", Math.PI);
            e.Parameters.Add("Pi", Math.PI);
            //
            if (ExistingParameters != null)
            {
                foreach (var entry in ExistingParameters) e.Parameters.Add(entry.Key, entry.Value);
            }
            return e;
        }
        //
        static public Expression GetArrayExpression(string expression)
        {
            Expression e = new Expression(expression, EvaluateOptions.IgnoreCase | EvaluateOptions.IterateParameters);
            // Add constants
            e.Parameters.Add("pi", Math.PI);
            e.Parameters.Add("Pi", Math.PI);
            //
            if (ExistingParameters != null)
            {
                foreach (var entry in ExistingParameters) e.Parameters.Add(entry.Key, entry.Value);
            }
            return e;
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
        static public void AddPropertyParameters(Dictionary<string, object> propParameters)
        {
            ExistingParameters.AddRange(propParameters);
        }
        //
        static public string PreprocessExpression(string expr)
        {
            // Sort parameter names by length (longest first)
            var sortedNames = ExistingParameters.Keys
                .OrderByDescending(name => name.Length)
                .ToList();
            // Step 1: Replace each parameter with a unique placeholder
            int count = 0;
            string placeholder;
            string bracketedParam;
            Dictionary<string, string> placeholders = new Dictionary<string, string>();
            //
            foreach (var param in sortedNames)
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
        //
        static public string[] GetFunctionSnippets()
        {
            return new string[] { "Abs(^)",
                                  "Acos(^)",
                                  "Asin(^)",
                                  "Atan(^)",
                                  "Ceiling(^)",
                                  "Cos(^)",
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
                                  "If(^, , )",
            };
        }
    }
}
