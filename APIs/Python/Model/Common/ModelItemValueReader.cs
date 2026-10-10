// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeModel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace APIs.Python.Model
{
    // Reads the values a model item takes as add_* and replace_* arguments. Each item type names the arguments it
    // reads back, each with the stored values it is made of - one, or the components of a vector argument, which
    // is read as one list - and nothing else is read, so whatever a read-back returns goes back into the replace_*
    // method of the item. The values every type of a collection shares are properties of the wrapping object.
    internal static class ModelItemValueReader
    {
        // Variables
        private static readonly HashSet<Type> FlagsEnumTypes = new HashSet<Type>
        {
            typeof(NodalFieldVariable), typeof(ElementFieldVariable), typeof(ContactFieldVariable),
            typeof(NodalHistoryVariable), typeof(ElementHistoryVariable), typeof(SectionHistoryVariableEnum),
            typeof(ContactHistoryVariable)
        };


        // Methods
        public static Dictionary<string, string[]> Arguments(params string[] argumentNames)
        {
            Dictionary<string, string[]> argumentValueNames = new Dictionary<string, string[]>();
            AddArguments(argumentValueNames, argumentNames);
            //
            return argumentValueNames;
        }
        // An argument whose value is stored under the name of the argument itself
        public static void AddArguments(Dictionary<string, string[]> argumentValueNames, params string[] argumentNames)
        {
            foreach (string argumentName in argumentNames)
                argumentValueNames.Add(argumentName, new string[] { argumentName });
        }
        public static Dictionary<string, object> GetValues(object item, Dictionary<string, string[]> argumentValueNames)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (var entry in argumentValueNames)
            {
                object[] components = new object[entry.Value.Length];
                for (int i = 0; i < components.Length; i++) components[i] = ReadValue(item, entry.Value[i]);
                //
                values[entry.Key] = components.Length == 1 ? components[0] :
                                                             PythonListConverter.ToPythonList(components);
            }
            //
            return values;
        }
        // Only what the user typed as an expression is returned, under the names GetValues uses: a value stored as
        // a plain number is left out, and get_value returns it as a number. A vector argument is one list, so when
        // any of its components is an expression, a component stored as a plain value stays in the list as the
        // number get_value returns for it, and the list can be passed back
        public static Dictionary<string, object> GetEquations(object item,
                                                              Dictionary<string, string[]> argumentValueNames)
        {
            Dictionary<string, object> equations = new Dictionary<string, object>();
            foreach (var entry in argumentValueNames)
            {
                EquationContainer[] components = new EquationContainer[entry.Value.Length];
                bool allEquations = true;
                for (int i = 0; i < components.Length && allEquations; i++)
                {
                    components[i] = FindProperty(item, entry.Value[i]).GetValue(item, null) as EquationContainer;
                    allEquations = components[i] != null;
                }
                if (!allEquations) continue;
                //
                object[] values = new object[components.Length];
                bool hasExpression = false;
                for (int i = 0; i < components.Length; i++)
                {
                    // The stored text is the expression the user entered, which is what a journal has to emit
                    if (components[i].IsEquation())
                    {
                        values[i] = components[i].String;
                        hasExpression = true;
                    }
                    else values[i] = double.IsNaN(components[i].Value) ? null : (object)components[i].Value;
                }
                if (!hasExpression) continue;
                //
                equations[entry.Key] = values.Length == 1 ? values[0] : PythonListConverter.ToPythonList(values);
            }
            //
            return equations;
        }
        // The equations of an item type whose arguments are all single values
        public static Dictionary<string, string> GetScalarEquations(object item,
                                                                    Dictionary<string, string[]> argumentValueNames)
        {
            Dictionary<string, string> equations = new Dictionary<string, string>();
            foreach (var entry in GetEquations(item, argumentValueNames))
            {
                string equation = entry.Value as string;
                if (equation == null)
                    throw new InvalidOperationException("The argument '" + entry.Key + "' of a " +
                                                        item.GetType().Name + " is not a single value.");
                equations[entry.Key] = equation;
            }
            //
            return equations;
        }
        public static object GetValue(Dictionary<string, object> values, string name, string itemDescription)
        {
            if (name == null) throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            object value;
            if (!values.TryGetValue(name, out value))
                throw new PythonAPIException("The value '" + name + "' is not defined for a " + itemDescription +
                                             ". Possible values are " + string.Join(", ", values.Keys) + ".");
            //
            return value;
        }
        //
        private static object ReadValue(object item, string valueName)
        {
            object value = FindProperty(item, valueName).GetValue(item, null);
            if (value == null) return null;
            //
            EquationContainer equation = value as EquationContainer;
            // A value the dialog shows as Default, and a degree of freedom that is not constrained, carry a NaN
            if (equation != null) return double.IsNaN(equation.Value) ? null : (object)equation.Value;
            // A damping is handed over as the part pmx.create makes, a copy, so it goes back into the step
            // argument it came from and changing it changes nothing in the model
            if (value is Damping damping) return new DampingAPI(damping.DeepClone());
            if (value is ModalDamping modalDamping) return new ModalDampingAPI(modalDamping.DeepClone());
            // A flag like enum is set from a list of names, so it is read back as one, even with one flag
            if (FlagsEnumTypes.Contains(value.GetType()))
                return PythonListConverter.ToPythonList(PythonValueConverter.FromFlagsEnum(value.GetType(), value));
            if (value.GetType().IsEnum) return value.ToString();
            // A list valued setting, such as the layers of a sweep, is handed over as a Python list, the form the
            // argument that sets it takes
            Array storedValues = value as Array;
            if (value is double[] || value is int[])
            {
                object[] items = new object[storedValues.Length];
                storedValues.CopyTo(items, 0);
                return PythonListConverter.ToPythonList(items);
            }
            if (IsSimpleType(value.GetType())) return value;
            //
            throw new InvalidOperationException("The value '" + valueName + "' of a " + item.GetType().Name +
                                                " is a " + value.GetType().Name + ", which no argument takes.");
        }
        private static PropertyInfo FindProperty(object item, string valueName)
        {
            foreach (PropertyInfo property in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (ToPythonName(property.Name) == valueName) return property;
            }
            //
            throw new InvalidOperationException("A " + item.GetType().Name + " stores no value '" + valueName + "'.");
        }
        private static bool IsSimpleType(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(bool) || type == typeof(string);
        }
        private static string ToPythonName(string name)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char character = name[i];
                if (char.IsUpper(character) && builder.Length > 0)
                {
                    // A run of capitals stays together, unless the last one starts a word: FMagnitude
                    bool startsWord = !char.IsUpper(name[i - 1]) ||
                                      (i + 1 < name.Length && char.IsLower(name[i + 1]));
                    if (startsWord) builder.Append('_');
                }
                builder.Append(char.ToLowerInvariant(character));
            }
            // The API names a count number_of_ and the step of a results file step_id, whatever the model calls them
            string pythonName = builder.ToString();
            if (pythonName == "step_number") return "step_id";
            if (pythonName.StartsWith("num_of_")) return "number_of_" + pythonName.Substring("num_of_".Length);
            if (pythonName.StartsWith("num_")) return "number_of_" + pythonName.Substring("num_".Length);
            //
            return pythonName;
        }
    }
}
