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
using System.Linq;

namespace APIs.Python
{
    public class StringListAPI : BaseAPI, IReadOnlyList<string>
    {
        // Variables
        private const int CompactItemLimit = 8;
        private const int EdgeItemCount = 3;
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
            return FormatItems(true);
        }
        protected override string ToFullString()
        {
            return FormatItems(false);
        }
        internal string GetFullDisplayString()
        {
            return FormatItems(false);
        }
        private string FormatItems(bool compact)
        {
            if (!compact || _items.Length <= CompactItemLimit)
                return "[" + string.Join(", ", _items.Select(ToDisplayString)) + "]";
            IEnumerable<string> first = _items.Take(EdgeItemCount).Select(ToDisplayString);
            IEnumerable<string> last = _items.Skip(_items.Length - EdgeItemCount).Select(ToDisplayString);
            return "[" + string.Join(", ", first) + ", ..., " + string.Join(", ", last) + "]";
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
