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

namespace PrePoMax
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using vtkControl;

    public sealed class SelectionCache
    {
        private sealed class Entry
        {
            public vtkMaxActor[] Actors;
            public uint MemoryKB;
            public DateTime LastUsed;
        }


        // Variables                                                                                                                
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private uint _currentMemoryKB;
        private readonly uint _maxMemoryKB;


        // Properties                                                                                                               
        public uint CurrentSizeMB => _currentMemoryKB / 1000;
        public uint MaxSizeMB => _maxMemoryKB / 1000;
        public int Count => _entries.Count;


        // Constructors                                                                                                             
        public SelectionCache(uint maxSizeMB)
        {
            _maxMemoryKB = maxSizeMB * 1000;
        }


        // Methods                                                                                                                  
        public void Clear()
        {
            _entries.Clear();
            _currentMemoryKB = 0;
        }
        public bool TryGet(string key, out vtkMaxActor[] actors)
        {
            if (_entries.TryGetValue(key, out var entry))
            {
                entry.LastUsed = DateTime.UtcNow;
                actors = entry.Actors;
                return true;
            }
            //
            actors = null;
            return false;
        }
        public void Add(string key, vtkMaxActor[] actors)
        {
            if (actors == null || actors.Length == 0) return;
            //
            uint entryMemoryKB = CalculateMemoryKB(actors);
            // Item too large to ever fit → reject
            if (entryMemoryKB > _maxMemoryKB) return;
            // Evict until it fits
            EvictUntilFits(entryMemoryKB);
            //
            _entries[key] = new Entry { Actors = actors, MemoryKB = entryMemoryKB, LastUsed = DateTime.UtcNow };
            //
            _currentMemoryKB += entryMemoryKB;
        }
        private uint CalculateMemoryKB(vtkMaxActor[] actors)
        {
            uint sum = 0;
            foreach (var actor in actors) sum += actor.MemorySize; // assumed KB
            //
            return sum;
        }
        private void EvictUntilFits(uint incomingKB)
        {
            while (_currentMemoryKB + incomingKB > _maxMemoryKB && _entries.Count > 0)
            {
                // Oldest first, prefer removing large entries
                var victim = _entries
                    .OrderBy(e => e.Value.LastUsed)
                    .ThenByDescending(e => e.Value.MemoryKB)
                    .First();
                //
                _currentMemoryKB -= victim.Value.MemoryKB;
                _entries.Remove(victim.Key);
            }
        }
    }

}
