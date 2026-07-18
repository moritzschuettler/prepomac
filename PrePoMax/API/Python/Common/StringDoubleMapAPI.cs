// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PrePoMax.API.Python
{
    public class StringDoubleMapAPI : BaseAPI, IReadOnlyDictionary<string, double>
    {
        // Variables
        private readonly string[] _keys;
        private readonly Dictionary<string, double> _values;


        // Properties
        [PythonAPI("The number of name-value pairs.", Returns = "int")]
        public int count { get { return _keys.Length; } }
        public int Count { get { return _keys.Length; } }
        public IEnumerable<string> Keys { get { return _keys; } }
        public IEnumerable<double> Values { get { return _keys.Select(key => _values[key]); } }
        public double this[string key] { get { return _values[key]; } }


        // Constructors
        internal StringDoubleMapAPI(OrderedDictionary<string, double> values)
        {
            _keys = values != null ? values.Keys.ToArray() : new string[0];
            _values = new Dictionary<string, double>();
            if (values != null)
            {
                foreach (var entry in values) _values.Add(entry.Key, entry.Value);
            }
        }


        // Methods
        public int __len__()
        {
            return _keys.Length;
        }
        public double __getitem__(string key)
        {
            return this[key];
        }
        public bool __contains__(string key)
        {
            return contains(key);
        }

        [PythonAPI("Returns the item names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_keys);
        }

        [PythonAPI("Returns true if the item name exists.", Arguments = "name: str", Returns = "bool")]
        public bool contains(string name)
        {
            return name != null && _values.ContainsKey(name);
        }
        public bool ContainsKey(string key)
        {
            return contains(key);
        }
        public bool TryGetValue(string key, out double value)
        {
            return _values.TryGetValue(key, out value);
        }
        public IEnumerator<KeyValuePair<string, double>> GetEnumerator()
        {
            foreach (string key in _keys) yield return new KeyValuePair<string, double>(key, _values[key]);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override string ToString()
        {
            return Format(true);
        }
        protected override string ToFullString()
        {
            return Format(false);
        }
        private string Format(bool compact)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{");
            if (compact && _keys.Length > 8)
            {
                AppendRange(sb, 0, 3);
                sb.Append(", ...");
                AppendRange(sb, _keys.Length - 3, _keys.Length);
            }
            else AppendRange(sb, 0, _keys.Length);
            sb.Append("}");
            return sb.ToString();
        }
        private void AppendRange(StringBuilder sb, int start, int end)
        {
            for (int i = start; i < end; i++)
            {
                if (sb.Length > 1) sb.Append(", ");
                string key = _keys[i];
                sb.Append(StringListAPI.ToDisplayString(key));
                sb.Append(": ");
                sb.Append(_values[key].ToString("R", CultureInfo.InvariantCulture));
            }
        }
    }
}
