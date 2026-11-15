// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace CaeGlobals
{
    // What the reader of the files an older version wrote had to leave out: an object of a type that no longer exists is
    // read as null. The reader hands the report to the deserialization callbacks as their sender, so a collection can drop
    // the entries such an object left, and the one who opened the file reports the rest and removes the history commands
    // that held one.
    public class LegacyReadReport
    {
        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y) { return ReferenceEquals(x, y); }
            public int GetHashCode(object item) { return RuntimeHelpers.GetHashCode(item); }
        }


        // Variables
        private readonly List<string> _removedTypeNames;
        private readonly Dictionary<object, List<string>> _droppedItemNamesByOwner;
        private readonly List<string> _warnings;


        // Properties
        public bool HasDroppedObjects { get { return _removedTypeNames.Count > 0; } }
        public string[] RemovedTypeNames { get { return _removedTypeNames.ToArray(); } }
        public string[] Warnings { get { return _warnings.ToArray(); } }


        // Constructors
        public LegacyReadReport()
        {
            _removedTypeNames = new List<string>();
            _droppedItemNamesByOwner = new Dictionary<object, List<string>>(new ReferenceComparer());
            _warnings = new List<string>();
        }


        // Methods
        public void AddRemovedType(string typeName)
        {
            if (!_removedTypeNames.Contains(typeName)) _removedTypeNames.Add(typeName);
        }
        // The name is the one the dropped object carried, or null when it had none
        public void AddDroppedObject(object owner, string droppedItemName)
        {
            List<string> names;
            if (!_droppedItemNamesByOwner.TryGetValue(owner, out names))
            {
                names = new List<string>();
                _droppedItemNamesByOwner.Add(owner, names);
            }
            names.Add(droppedItemName);
        }
        public bool HeldDroppedObject(object owner)
        {
            return _droppedItemNamesByOwner.ContainsKey(owner);
        }
        public string[] GetDroppedItemNames(object owner)
        {
            List<string> names;
            if (!_droppedItemNamesByOwner.TryGetValue(owner, out names)) return new string[0];
            return names.ToArray();
        }
        public void AddWarning(string warning)
        {
            _warnings.Add(warning);
        }
        public void Append(LegacyReadReport report)
        {
            foreach (var typeName in report._removedTypeNames) AddRemovedType(typeName);
            foreach (var entry in report._droppedItemNamesByOwner)
                foreach (var name in entry.Value) AddDroppedObject(entry.Key, name);
            _warnings.AddRange(report._warnings);
        }
    }
}
