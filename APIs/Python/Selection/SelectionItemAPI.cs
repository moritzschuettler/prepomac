// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
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
        private readonly SelectionPickAPI _pick;
        private readonly bool _invert;
        private readonly bool _selectAll;
        private readonly vtkSelectOperation? _operation;
        private readonly GeometrySelectModeEnum _geometryMode;


        // Properties
        public string Source { get { return _source; } }
        public int[] Ids { get { return _ids; } }
        public SortedDictionary<int, int[]> PartIdItemIds { get { return _partIdItemIds; } }
        // A picked item names a point in space rather than IDs, so the application resolves it
        public SelectionPickAPI Pick { get { return _pick; } }
        public bool IsPick { get { return _pick != null; } }
        // An invert flips what the nodes before it selected, so only the application can resolve it
        public bool IsInvert { get { return _invert; } }
        public bool SelectAll { get { return _selectAll; } }
        public vtkSelectOperation? Operation { get { return _operation; } }
        public GeometrySelectModeEnum GeometryMode { get { return _geometryMode; } }
        public int Count
        {
            get
            {
                if (_pick != null || _invert || _selectAll) return 1;
                if (_ids != null) return _ids.Length;
                return _partIdItemIds.Values.Sum(ids => ids.Length);
            }
        }


        // Constructors
        public SelectionItemAPI(string source, int[] ids, vtkSelectOperation? operation = null,
                                GeometrySelectModeEnum geometryMode = GeometrySelectModeEnum.SelectId)
        {
            _geometryMode = geometryMode;
            _operation = operation;
            _source = source;
            _ids = ids.ToArray();
            _partIdItemIds = null;
        }
        public SelectionItemAPI(string source, SortedDictionary<int, int[]> partIdItemIds,
                                vtkSelectOperation? operation = null,
                                GeometrySelectModeEnum geometryMode = GeometrySelectModeEnum.SelectId)
        {
            _geometryMode = geometryMode;
            _operation = operation;
            _source = source;
            _ids = null;
            _partIdItemIds = new SortedDictionary<int, int[]>();
            foreach (var entry in partIdItemIds) _partIdItemIds.Add(entry.Key, entry.Value.ToArray());
        }
        public SelectionItemAPI(SelectionPickAPI pick, vtkSelectOperation? operation = null)
        {
            _operation = operation;
            _source = pick.IsArea ? "area" : "pick";
            _ids = null;
            _partIdItemIds = null;
            _pick = pick;
        }
        public SelectionItemAPI(vtkSelectOperation operation)
        {
            _source = "all";
            _selectAll = true;
            _operation = operation;
        }
        public SelectionItemAPI()
        {
            _source = "invert";
            _ids = null;
            _partIdItemIds = null;
            _invert = true;
        }


        // Methods
        public SelectionItemAPI DeepCopy()
        {
            if (_invert) return new SelectionItemAPI();
            if (_selectAll) return new SelectionItemAPI(_operation.Value);
            if (_pick != null) return new SelectionItemAPI(_pick, _operation);
            if (_ids != null) return new SelectionItemAPI(_source, _ids, _operation, _geometryMode);
            return new SelectionItemAPI(_source, _partIdItemIds, _operation, _geometryMode);
        }
    }
}
