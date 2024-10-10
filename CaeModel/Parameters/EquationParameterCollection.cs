using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CaeGlobals;

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
                if (_overriddenParameters.TryGetValue(name, out parameter)) _parameters[name] = parameter;
                else _parameters[name] = value;
            }
        }
        public OrderedDictionary<string, EquationParameter> OverriddenParameters { get { return _overriddenParameters; } }
        public ICollection<string> Keys
        {
            get { return _parameters.Keys; }
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
            foreach (var entry in parameters) _parameters.Add(entry.Key, entry.Value);
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
            if (_overriddenParameters.TryGetValue(name, out parameter)) _parameters.Add(name, parameter);
            else _parameters.Add(name, value);
        }
        public void AddOverriddenParametersFromString(string parametersString)
        {
            double value;
            string[] tmp;
            string[] parameters = parametersString.Split(new string[] { ";"}, StringSplitOptions.RemoveEmptyEntries);
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
            if (_overriddenParameters.TryGetValue(parameter.Name, out op)) _parameters.Replace(oldName, parameter.Name, op);
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
        //
        public string GetNextNumberedKey(string key, string postFix = "", string separator = "-")
        {
            return _parameters.GetNextNumberedKey(key, postFix, separator);
        }
        public EquationParameterCollection DeepCopy()
        {
            return new EquationParameterCollection(this);
        }

        public void OnDeserialization(object sender)
        {
            _parameters.OnDeserialization(sender);
            _overriddenParameters.OnDeserialization(sender);
        }
    }
}
