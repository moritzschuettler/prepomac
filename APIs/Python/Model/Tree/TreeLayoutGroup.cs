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

namespace APIs.Python.Model
{
    // One group of a layout given to TreeAPI.set_layout, read from the script before anything is checked
    // against the model. An entry of a group is an item name or a nested group.
    internal class TreeLayoutGroup
    {
        // Variables
        private readonly string _name;
        private readonly Guid _id;
        private readonly List<object> _entries;


        // Properties
        public string Name { get { return _name; } }
        public Guid Id { get { return _id; } }
        public List<object> Entries { get { return _entries; } }


        // Constructors
        public TreeLayoutGroup(string name, Guid id, List<object> entries)
        {
            _name = name;
            _id = id;
            _entries = entries;
        }
    }
}
