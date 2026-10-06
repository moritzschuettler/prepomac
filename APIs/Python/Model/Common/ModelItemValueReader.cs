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
using System.Linq;
using System.Reflection;
using System.Text;

namespace APIs.Python.Model
{
    // Reads the values a concrete model type adds to its base type. The values the base type carries
    // are exposed as properties of the wrapping object, so only the type specific ones are read here.
    // Writing is always done with named arguments, so this is a read side only.
    internal static class ModelItemValueReader
    {
        // Methods
        public static Dictionary<string, object> GetValues(object item, Type baseType)
        {
            Dictionary<string, object> values = new Dictionary<string, object>();
            PropertyInfo[] properties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties.OrderBy(entry => entry.Name))
            {
                if (property.DeclaringType.IsAssignableFrom(baseType)) continue;
                if (property.GetIndexParameters().Length > 0) continue;
                //
                object value = property.GetValue(item, null);
                string name = ToPythonName(property.Name);
                //
                EquationContainer equation = value as EquationContainer;
                if (equation != null)
                {
                    // A degree of freedom that is not constrained carries a NaN
                    values[name] = double.IsNaN(equation.Value) ? null : (object)equation.Value;
                    continue;
                }
                Damping damping = value as Damping;
                if (damping != null)
                {
                    values["damping_type"] = damping.DampingType.ToString();
                    values["damping_alpha"] = damping.Alpha;
                    values["damping_beta"] = damping.Beta;
                    continue;
                }
                ModalDamping modalDamping = value as ModalDamping;
                if (modalDamping != null)
                {
                    values["damping_type"] = modalDamping.DampingType.ToString();
                    values["damping_alpha"] = modalDamping.Alpha;
                    values["damping_beta"] = modalDamping.Beta;
                    values["damping_viscous_damping_ratio"] = modalDamping.ViscousDampingRatio;
                    values["damping_ratio_count"] = modalDamping.DampingRatiosAndRanges == null ? 0 :
                                                    modalDamping.DampingRatiosAndRanges.Count;
                    continue;
                }
                if (value != null && value.GetType().IsEnum)
                {
                    // A combined value of a flag like enum has no name of its own
                    Type enumType = value.GetType();
                    if (Enum.IsDefined(enumType, value)) value = value.ToString();
                    else value = string.Join(", ", PythonValueConverter.FromFlagsEnum(enumType, value));
                }
                // A list valued setting, such as the layer sizes of a multi layer mesh, is part of the
                // definition, so a snapshot that leaves it out is not a complete one. It is handed over as a
                // Python list, the form the argument that sets it takes
                else if (property.PropertyType == typeof(double[]) || property.PropertyType == typeof(int[]))
                {
                    Array storedValues = value as Array;
                    if (storedValues == null) value = null;
                    else
                    {
                        object[] items = new object[storedValues.Length];
                        storedValues.CopyTo(items, 0);
                        value = PythonListConverter.ToPythonList(items);
                    }
                }
                else if (!IsSimpleType(property.PropertyType)) continue;
                //
                values[name] = value;
            }
            //
            return values;
        }
        // A value an add_* argument sets is reported under the argument's name, a vector argument as one
        // list of its components; a value no argument covers keeps the name of the property it is stored in
        public static Dictionary<string, object> GetValues(object item, Type baseType,
                                                           Dictionary<string, string[]> argumentValueNames)
        {
            Dictionary<string, object> storedValues = GetValues(item, baseType);
            if (argumentValueNames == null) return storedValues;
            //
            Dictionary<string, object> values = new Dictionary<string, object>();
            HashSet<string> coveredNames = new HashSet<string>();
            foreach (var entry in argumentValueNames)
            {
                object[] components = new object[entry.Value.Length];
                for (int i = 0; i < components.Length; i++)
                {
                    if (!storedValues.TryGetValue(entry.Value[i], out components[i]))
                        throw new InvalidOperationException("The argument '" + entry.Key + "' of a " +
                                                            item.GetType().Name + " names the missing value '" +
                                                            entry.Value[i] + "'.");
                    coveredNames.Add(entry.Value[i]);
                }
                values[entry.Key] = components.Length == 1 ? components[0] :
                                                             PythonListConverter.ToPythonList(components);
            }
            foreach (var entry in storedValues)
            {
                if (!coveredNames.Contains(entry.Key) && !values.ContainsKey(entry.Key)) values[entry.Key] = entry.Value;
            }
            //
            return values;
        }
        public static Dictionary<string, string> GetEquations(object item)
        {
            Dictionary<string, string> equations = new Dictionary<string, string>();
            PropertyInfo[] properties = item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (property.GetIndexParameters().Length > 0) continue;
                if (property.PropertyType != typeof(EquationContainer)) continue;
                //
                EquationContainer equation = property.GetValue(item, null) as EquationContainer;
                // The stored text is the expression the user entered, which is what a journal has to emit
                if (equation != null) equations[ToPythonName(property.Name)] = equation.String;
            }
            //
            return equations;
        }
        // The equations are named the way GetValues names the values, a vector argument as one list of the
        // equations of its components; an argument whose values are not equations is left out
        public static Dictionary<string, object> GetEquations(object item,
                                                              Dictionary<string, string[]> argumentValueNames)
        {
            Dictionary<string, string> storedEquations = GetEquations(item);
            Dictionary<string, object> equations = new Dictionary<string, object>();
            HashSet<string> coveredNames = new HashSet<string>();
            if (argumentValueNames != null)
            {
                foreach (var entry in argumentValueNames)
                {
                    string[] components = new string[entry.Value.Length];
                    bool allEquations = true;
                    for (int i = 0; i < components.Length && allEquations; i++)
                        allEquations = storedEquations.TryGetValue(entry.Value[i], out components[i]);
                    if (!allEquations) continue;
                    //
                    coveredNames.UnionWith(entry.Value);
                    equations[entry.Key] = components.Length == 1 ? (object)components[0] :
                                                                    new StringListAPI(components);
                }
            }
            foreach (var entry in storedEquations)
            {
                if (!coveredNames.Contains(entry.Key) && !equations.ContainsKey(entry.Key))
                    equations[entry.Key] = entry.Value;
            }
            //
            return equations;
        }
        public static object GetValue(object item, Type baseType, Dictionary<string, string[]> argumentValueNames,
                                      string name, string itemDescription)
        {
            if (name == null) throw new PythonAPIException("The argument 'name' must not be empty.");
            //
            Dictionary<string, object> values = GetValues(item, baseType, argumentValueNames);
            object value;
            if (!values.TryGetValue(name.Trim(), out value))
                throw new PythonAPIException("The value '" + name + "' is not defined for a " + itemDescription +
                                             ". Possible values are " + string.Join(", ", values.Keys) + ".");
            //
            return value;
        }
        //
        private static bool IsSimpleType(Type type)
        {
            return type == typeof(double) || type == typeof(int) || type == typeof(bool) ||
                   type == typeof(string) || type.IsEnum;
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
