// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.InteropServices;
using System.Text;
using CaeGlobals;
using CaeMesh;

namespace CaeModel
{
    [Serializable]
    public class EquationParameterCollection
    {
        // Variables                                                                                                                
        private OrderedDictionary<string, EquationParameter> _parameters;
        private OrderedDictionary<string, EquationParameter> _overriddenParameters;


        // Properties                                                                                                               
        public EquationParameter this[string name]
        {
            get { return _parameters[name]; }
            set
            {
                EquationParameter parameter;
                if (_overriddenParameters.TryGetValue(name, out parameter))
                {
                    KeepDescription(parameter, value);
                    _parameters[name] = parameter;
                }
                else _parameters[name] = value;
            }
        }
        // OverriddenParameters are used only during regeneration
        public OrderedDictionary<string, EquationParameter> OverriddenParameters { get { return _overriddenParameters; } }
        public ICollection<string> Keys
        {
            get { return _parameters.Keys; }
        }
        public ICollection<EquationParameter> Values
        {
            get { return _parameters.Values; }
        }


        // Constructors                                                                                                             
        public EquationParameterCollection()
        {
            StringComparer sc = StringComparer.OrdinalIgnoreCase;
            _parameters = new OrderedDictionary<string, EquationParameter>("Parameters", sc);
            _overriddenParameters = new OrderedDictionary<string, EquationParameter>("OverriddenParameters", sc);
        }
        public EquationParameterCollection(OrderedDictionary<string, EquationParameter> parameters)
            : this()
        {
            // Use for each to keep the string comparer OrdinalIgnoreCase
            foreach (var entry in parameters) Add(entry.Key, entry.Value);
        }
        public EquationParameterCollection(EquationParameterCollection collection)
            : this()
        {
            if (collection._parameters != null) _parameters = collection._parameters.DeepCopy();
            if (collection._overriddenParameters != null) _overriddenParameters = collection._overriddenParameters.DeepCopy();
        }


        // Methods
        public IEnumerator<KeyValuePair<string, EquationParameter>> GetEnumerator()
        {
            foreach (var entry in _parameters)
                yield return new KeyValuePair<string, EquationParameter>(entry.Key, this[entry.Key]);
        }
        public void Add(string name, EquationParameter value)
        {
            EquationParameter parameter;
            if (_overriddenParameters.TryGetValue(name, out parameter))
            {
                KeepDescription(parameter, value);
                _parameters.Add(name, parameter);
            }
            else _parameters.Add(name, value);
        }
        // An overridden parameter is built from a value alone, and it replaces the parameter of the model
        // rather than only its equation, so the description the model carries would be lost with it
        private static void KeepDescription(EquationParameter overriddenParameter, EquationParameter parameter)
        {
            if (overriddenParameter.Description == null && parameter != null)
                overriddenParameter.Description = parameter.Description;
        }
        public void AddOverriddenParametersFromString(string parametersString)
        {
            double value;
            string[] tmp;
            //đ+'špoiuztrfedsa
            if (parametersString.Contains("="))
            {
                string[] parameters = parametersString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                EquationParameter p;
                //
                foreach (string parameter in parameters)
                {
                    tmp = parameter.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    if (tmp.Length == 2 && double.TryParse(tmp[1], out value))
                    {
                        p = new EquationParameter();
                        p.Name = tmp[0];
                        p.Equation.SetEquationFromValue(value);
                        AddOverriddenParameter(p.Name, p);
                    }
                    else throw new CaeException("The parameter " + parameter + " cannot be parsed.");
                }
            }
            else
            {
                ImportFromFile(parametersString, true);
            }
        }
        public void AddOverriddenParameter(string name, EquationParameter parameter)
        {
            _overriddenParameters.Add(name, parameter);
        }
        public bool TryGetValue(string name, out EquationParameter parameter)
        {
            return _parameters.TryGetValue(name, out parameter);
        }
        public void Replace(string oldName, EquationParameter parameter)
        {
            EquationParameter op;
            if (_overriddenParameters.TryGetValue(parameter.Name, out op))
            {
                KeepDescription(op, parameter);
                _parameters.Replace(oldName, parameter.Name, op);
            }
            else _parameters.Replace(oldName, parameter.Name, parameter);
        }
        public bool Remove(string name)
        {
            return _parameters.Remove(name);
        }
        public void Clear()
        {
            _parameters.Clear();
        }
        public void ClearOverriddenParameters()
        {
            _overriddenParameters.Clear();
        }
        //
        public string GetNextNumberedKey(string key, string postFix = "", string separator = "-")
        {
            return _parameters.GetNextNumberedKey(key, postFix, separator);
        }
        public EquationParameterCollection DeepCopy()
        {
            return new EquationParameterCollection(this);
        }
        //
        public void ExportToFile(string fileName)
        {
           StringBuilder sb = new StringBuilder();
            //
            sb.AppendFormat("${0};{1};{2};{3}", "Name", "Value/Equation", "Evaluates to", "Description");
            //
            foreach (var entry in _parameters)
            {
                sb.AppendLine();
                sb.AppendFormat("{0};{1};{2};{3}", entry.Value.Name, entry.Value.EquationStr.Equation, entry.Value.Value,
                                GetFieldFromDescription(entry.Value.Description));
            }
            //
            System.IO.File.WriteAllText(fileName, sb.ToString());
        }
        public void ImportFromFile(string fileName, bool overriddenParameters = false)
        {
            string[] lines = System.IO.File.ReadAllLines(fileName);
            //
            string line;
            string[] tmp;
            string[] splitter = new string[] { ";" };
            //
            List<EquationParameter> parameters = new List<EquationParameter>();
            //
            for (int i = 0; i < lines.Length; i++)
            {
                line = lines[i].Trim();
                // A file with a description column is one a user opens in an editor, so it can end in a
                // blank line
                if (line.Length == 0) continue;
                if (line[0] == '$') continue;
                tmp = line.Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                if (tmp.Length >= 2)
                {
                    try
                    {
                        EquationParameter parameter = new EquationParameter();
                        parameter.Name = tmp[0].Trim();
                        parameter.Equation.SetEquation(new EquationString(tmp[1].Trim()));
                        // The third field is the evaluated value, which is written for the reader of the
                        // file and is not read back; a file written before the description has three
                        if (tmp.Length >= 4) parameter.Description = tmp[3].Trim();
                        //
                        parameters.Add(parameter);
                    }
                    catch (Exception ex)
                    {
                        throw new CaeException("Line " + (i + 1) + " of the parametes file in not correctly formated.");
                    }
                }
                else throw new CaeException("Line " + (i+1) + " of the parametes file in not correctly formated.");
            }
            //
            if (overriddenParameters)
            {
                _overriddenParameters.Clear();
                foreach (var parameter in parameters) _overriddenParameters.Add(parameter.Name, parameter);
            }
            else
            {
                _parameters.Clear();
                foreach (var parameter in parameters) Add(parameter.Name, parameter);
            }
        }
        private static string GetFieldFromDescription(string description)
        {
            if (description == null) return "";
            // The file is one parameter a line, with the fields separated by a semicolon
            return description.Replace(';', ',').Replace(Environment.NewLine, " ").Replace('\n', ' ').Replace('\r', ' ');
        }
        public void OnDeserialization(object sender)
        {
            _parameters.OnDeserialization(sender);
            _overriddenParameters.OnDeserialization(sender);
        }
    }
}
