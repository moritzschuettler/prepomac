// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PrePoMax.API.Python
{
    public class StringListAPI : BaseAPI, IReadOnlyList<string>
    {
        // Variables
        private readonly string[] _items;


        // Properties
        public int Count { get { return _items.Length; } }

        [PythonAPI("The number of strings in the list.", Returns = "int")]
        public int count { get { return _items.Length; } }
        public string this[int index]
        {
            get
            {
                if (index < 0) index = _items.Length + index;
                if (index < 0 || index >= _items.Length) throw new IndexOutOfRangeException();
                return _items[index];
            }
        }


        // Constructors
        public StringListAPI(IEnumerable<string> items)
        {
            _items = items != null ? items.ToArray() : new string[0];
        }


        // Methods
        public int __len__()
        {
            return _items.Length;
        }
        public string __getitem__(int index)
        {
            return this[index];
        }
        public bool __contains__(string item)
        {
            return contains(item);
        }

        [PythonAPI("Returns true if the list contains the specified string.", Arguments = "item: str", Returns = "bool")]
        public bool contains(string item)
        {
            return _items.Contains(item);
        }

        [PythonAPI("Returns the list as a copied CLR string array.", Returns = "str[]")]
        public string[] to_array()
        {
            return _items.ToArray();
        }
        public IEnumerator<string> GetEnumerator()
        {
            return ((IEnumerable<string>)_items).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
        public override string ToString()
        {
            return "[" + string.Join(", ", _items.Select(ToDisplayString)) + "]";
        }
        public static string ToDisplayString(string item)
        {
            if (item == null) return "None";
            return "'" + item.Replace("\\", "\\\\")
                             .Replace("'", "\\'")
                             .Replace("\r", "\\r")
                             .Replace("\n", "\\n")
                             .Replace("\t", "\\t") + "'";
        }
    }
}
