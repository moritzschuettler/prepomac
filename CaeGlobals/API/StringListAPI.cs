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

namespace CaeGlobals
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    namespace CaeResults
    {
        public class StringListAPI : BaseAPI, IEnumerable
        {
            // Variables                                                                                                            
            private readonly List<string> _items;
            
            
            // Properties                                                                                                           
            public int Count
            {
                get { return _items.Count; }
            }
            public bool IsReadOnly
            {
                get { return true; }
            }
            public string this[int index]
            {
                get { return _items[index]; }
                set { throw new NotSupportedException(); }
            }


            // Constructors                                                                                                         
            public StringListAPI(IEnumerable<string> items)
            {
                _items = items != null ? new List<string>(items) : new List<string>();
            }


            // Methods                                                                                                              
            public IEnumerator<string> GetEnumerator()
            {
                return _items.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }
            public int IndexOf(string item)
            {
                return _items.IndexOf(item);
            }
            public bool Contains(string item)
            {
                return _items.Contains(item);
            }
            public void CopyTo(string[] array, int arrayIndex)
            {
                _items.CopyTo(array, arrayIndex);
            }
            public string[] ToArray()
            {
                return _items.ToArray();
            }
            public override string ToString()
            {
                return "[" + string.Join(", ", _items.Select(item => "'" + item.Replace("\\", "\\\\") + "'")) + "]";
            }
            public void Add(string item)
            {
                throw new NotSupportedException();
            }
            public void Clear()
            {
                throw new NotSupportedException();
            }
            public void Insert(int index, string item)
            {
                throw new NotSupportedException();
            }
            public bool Remove(string item)
            {
                throw new NotSupportedException();
            }
            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }
        }
    }
}
