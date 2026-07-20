// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using System.Linq;

namespace APIs.Python
{
    internal class SelectionItemAPI
    {
        // Variables
        private readonly string _source;
        private readonly int[] _ids;
        private readonly SortedDictionary<int, int[]> _partIdItemIds;


        // Properties
        public string Source { get { return _source; } }
        public int[] Ids { get { return _ids; } }
        public SortedDictionary<int, int[]> PartIdItemIds { get { return _partIdItemIds; } }
        public int Count
        {
            get
            {
                if (_ids != null) return _ids.Length;
                return _partIdItemIds.Values.Sum(ids => ids.Length);
            }
        }


        // Constructors
        public SelectionItemAPI(string source, int[] ids)
        {
            _source = source;
            _ids = ids.ToArray();
            _partIdItemIds = null;
        }
        public SelectionItemAPI(string source, SortedDictionary<int, int[]> partIdItemIds)
        {
            _source = source;
            _ids = null;
            _partIdItemIds = new SortedDictionary<int, int[]>();
            foreach (var entry in partIdItemIds) _partIdItemIds.Add(entry.Key, entry.Value.ToArray());
        }


        // Methods
        public SelectionItemAPI DeepCopy()
        {
            if (_ids != null) return new SelectionItemAPI(_source, _ids);
            return new SelectionItemAPI(_source, _partIdItemIds);
        }
    }
}
