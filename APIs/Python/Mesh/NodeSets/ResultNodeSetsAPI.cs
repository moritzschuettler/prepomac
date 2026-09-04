// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Linq;

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class ResultNodeSetsAPI : BaseAPI, IReadOnlyList<ResultNodeSetAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Constructors
        internal ResultNodeSetsAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public ResultNodeSetAPI __getitem__(string nodeSetName)
        {
            return this[nodeSetName];
        }
        public ResultNodeSetAPI this[string nodeSetName]
        {
            get { return _meshService.GetNodeSet(_resultName, nodeSetName); }
        }
        public int __len__()
        {
            return _meshService.GetNodeSetNames(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        ResultNodeSetAPI IReadOnlyList<ResultNodeSetAPI>.this[int index]
        {
            get
            {
                string[] keys = get_names().to_array();
                if (index < 0) index = keys.Length + index;
                if (index < 0 || index >= keys.Length)
                    throw new PythonAPIException("The index " + index + " is out of range.");
                //
                return this[keys[index]];
            }
        }
        public IEnumerator<ResultNodeSetAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool __contains__(string nodeSetName)
        {
            return contains(nodeSetName);
        }

        [PythonAPI("Returns user-visible result node set names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetNodeSetNames(_resultName));
        }

        [PythonAPI("Returns the first result node set.", Returns = "NodeSet")]
        public ResultNodeSetAPI first()
        {
            string[] names = _meshService.GetNodeSetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The node sets collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result node set.", Returns = "NodeSet")]
        public ResultNodeSetAPI last()
        {
            string[] names = _meshService.GetNodeSetNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The node sets collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result contains the specified user-visible node set.",
                   Arguments = "node_set_name: str", Returns = "bool")]
        public bool contains(string nodeSetName)
        {
            return _meshService.GetNodeSetNames(_resultName).Contains(nodeSetName);
        }
        public override string ToString()
        {
            string[] names = _meshService.GetNodeSetNames(_resultName);
            return "NodeSets(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _meshService.GetNodeSetNames(_resultName);
            return "NodeSets(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
