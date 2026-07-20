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
    public class SelectionAPI : BaseAPI
    {
        // Variables
        private readonly SelectionService _service;
        private readonly string _view;
        private readonly string _target;
        private readonly List<SelectionItemAPI> _items;


        // Properties
        [PythonAPI("The selection view. Possible values are 'geometry', 'femodel', and 'results'.", Returns = "str")]
        public string view { get { return _view; } }
        [PythonAPI("The selected target item type.", Returns = "str")]
        public string target { get { return _target; } }
        [PythonAPI("The number of source IDs in the selection.", Returns = "int")]
        public int count { get { return _items.Sum(item => item.Count); } }


        // Constructors
        internal SelectionAPI(SelectionService service, string view, string target)
        {
            _service = service;
            _view = view;
            _target = target;
            _items = new List<SelectionItemAPI>();
        }


        // Methods
        [PythonAPI("Adds IDs to the selection. Edges and surfaces use dict[part_id, list[item_id]]; " +
                   "nodes, elements, vertices, and parts use list[int].",
                   Arguments = "source: str, ids: list[int] or dict[int, list[int]]", Returns = "Selection")]
        public SelectionAPI add_by_ids(string source, object ids)
        {
            _service.AddByIds(this, source, ids);
            return this;
        }
        [PythonAPI("Returns the source item types used by this selection.", Returns = "StringList")]
        public StringListAPI get_sources()
        {
            return new StringListAPI(_items.Select(item => item.Source).Distinct().ToArray());
        }
        internal void AddItem(SelectionItemAPI item)
        {
            _items.Add(item);
        }
        internal SelectionItemAPI[] GetItems()
        {
            return _items.ToArray();
        }
        internal SelectionAPI DeepCopy()
        {
            SelectionAPI copy = new SelectionAPI(_service, _view, _target);
            foreach (SelectionItemAPI item in _items) copy.AddItem(item.DeepCopy());
            return copy;
        }
        public override string ToString()
        {
            StringListAPI sources = new StringListAPI(_items.Select(item => item.Source).Distinct().ToArray());
            return "Selection(view=" + StringListAPI.ToDisplayString(_view) +
                   ", target=" + StringListAPI.ToDisplayString(_target) +
                   ", sources=" + sources.ToString() + ", count=" + count + ")";
        }
    }
}
