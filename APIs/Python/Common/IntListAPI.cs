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
    public class IntListAPI : BaseAPI, IReadOnlyList<int>
    {
        // Variables
        private const int CompactItemLimit = 8;
        private const int EdgeItemCount = 3;
        private readonly int[] _items;


        // Properties
        public int Count { get { return _items.Length; } }

        [PythonAPI("The number of integers in the list.", Returns = "int")]
        public int count { get { return _items.Length; } }
        public int this[int index]
        {
            get
            {
                if (index < 0) index = _items.Length + index;
                if (index < 0 || index >= _items.Length) throw new IndexOutOfRangeException();
                return _items[index];
            }
        }


        // Constructors
        public IntListAPI(IEnumerable<int> items)
        {
            _items = items != null ? items.ToArray() : new int[0];
        }


        // Methods
        public int __len__()
        {
            return _items.Length;
        }
        public int __getitem__(int index)
        {
            return this[index];
        }
        public bool __contains__(object item)
        {
            return contains(item);
        }

        [PythonAPI("Returns true if the list contains the specified integer.", Arguments = "item: int", Returns = "bool")]
        public bool contains(object item)
        {
            return _items.Contains(PythonValueConverter.ToInt(item, "item"));
        }

        [PythonAPI("Returns a copy of the integers as a plain Python list.", Returns = "list[int]")]
        public object to_array()
        {
            return PythonListConverter.ToPythonList(_items.Cast<object>().ToArray());
        }
        internal int[] ToArray()
        {
            return _items.ToArray();
        }
        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)_items).GetEnumerator();
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
                return "[" + string.Join(", ", _items.Select(item => item.ToString())) + "]";
            IEnumerable<string> first = _items.Take(EdgeItemCount).Select(item => item.ToString());
            IEnumerable<string> last = _items.Skip(_items.Length - EdgeItemCount).Select(item => item.ToString());
            return "[" + string.Join(", ", first) + ", ..., " + string.Join(", ", last) + "]";
        }
    }
}
