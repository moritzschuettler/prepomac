// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Linq;

using System.Collections;
using System.Collections.Generic;

namespace APIs.Python
{
    public class PartsAPI : BaseAPI, IReadOnlyList<PartAPI>
    {
        // Variables
        private readonly string _resultName;
        private readonly MeshDataService _meshService;


        // Constructors
        internal PartsAPI(string resultName, MeshDataService meshService)
        {
            _resultName = resultName;
            _meshService = meshService;
        }


        // Methods
        public PartAPI __getitem__(string partName)
        {
            return this[partName];
        }
        public PartAPI this[string partName]
        {
            get { return _meshService.GetPart(_resultName, partName); }
        }
        public int __len__()
        {
            return _meshService.GetPartNames(_resultName).Length;
        }
        // The collection interface is what makes len() and iteration work on the Python side; the
        // positional indexer is explicit so that the Python side resolves the name indexer
        public int Count { get { return __len__(); } }
        PartAPI IReadOnlyList<PartAPI>.this[int index]
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
        public IEnumerator<PartAPI> GetEnumerator()
        {
            foreach (string key in get_names().to_array()) yield return this[key];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [PythonAPI("Returns result mesh part names.", Returns = "StringList")]
        public StringListAPI get_names()
        {
            return new StringListAPI(_meshService.GetPartNames(_resultName));
        }

        [PythonAPI("Returns the first result mesh part.", Returns = "Part")]
        public PartAPI first()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The parts collection is empty.");
            return this[names[0]];
        }

        [PythonAPI("Returns the last result mesh part.", Returns = "Part")]
        public PartAPI last()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            if (names.Length == 0) throw new PythonAPIException("The parts collection is empty.");
            return this[names[names.Length - 1]];
        }

        [PythonAPI("Returns true if the result mesh contains the specified part.", Arguments = "part_name: str", Returns = "bool")]
        public bool contains(string partName)
        {
            return _meshService.GetPartNames(_resultName).Contains(partName);
        }
        public override string ToString()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            return "Parts(count=" + names.Length + ", names=" + new StringListAPI(names).ToString() + ")";
        }
        protected override string ToFullString()
        {
            string[] names = _meshService.GetPartNames(_resultName);
            return "Parts(count=" + names.Length + ", names=" +
                   new StringListAPI(names).GetFullDisplayString() + ")";
        }
    }
}
