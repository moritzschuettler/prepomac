// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System.Collections.Generic;
using CaeGlobals;

namespace CaeMesh
{
    /// <summary>
    /// The node and the element labels of the visible parts of a mesh, kept for as long as the parts they
    /// were built from do not change. A symbol is drawn per item but asks the same question of the whole
    /// mesh, so without this every constraint, boundary condition and load symbol swept all model nodes or
    /// all model elements once, and a surface based one did it once per element face group. The labels of the
    /// active parts, hidden ones included, are kept beside them for resolving a stored selection, which hiding
    /// a part must not change.
    ///
    /// The cache is not invalidated by the callers but by the state of the parts themselves: the part
    /// count, the part ids, whether each part is active and visible, and the identity of the label arrays
    /// the parts carry. Adding, removing, hiding or remeshing a part changes that state and the sets are
    /// rebuilt. The one change it cannot see is a renumbering that rewrites a label array in place, which
    /// is why FeMesh drops the cache in RenumberNodes, RenumberElements and SplitSolidCompoundParts.
    /// </summary>
    public class MeshVisibilityCache
    {
        // Variables                                                                                                                
        private int[] _partIds;
        private bool[] _partVisibilities;
        private bool[] _partActivities;
        private int[][] _partNodeLabels;
        private int[][] _partElementLabels;
        private HashSet<int> _visibleNodeIds;
        private HashSet<int> _visibleElementIds;
        private HashSet<int> _activeNodeIds;
        private HashSet<int> _activeElementIds;


        // Constructor                                                                                                              
        public MeshVisibilityCache(OrderedDictionary<string, BasePart> parts)
        {
            _partIds = new int[parts.Count];
            _partVisibilities = new bool[parts.Count];
            _partActivities = new bool[parts.Count];
            _partNodeLabels = new int[parts.Count][];
            _partElementLabels = new int[parts.Count][];
            //
            BasePart part;
            int i = 0;
            foreach (var entry in parts)
            {
                part = entry.Value;
                _partIds[i] = part.PartId;
                _partVisibilities[i] = part.Active && part.Visible;
                _partActivities[i] = part.Active;
                _partNodeLabels[i] = part.NodeLabels;
                _partElementLabels[i] = part.Labels;
                i++;
            }
        }


        // Methods                                                                                                                  
        public bool IsValidFor(OrderedDictionary<string, BasePart> parts)
        {
            if (parts.Count != _partIds.Length) return false;
            //
            BasePart part;
            int i = 0;
            foreach (var entry in parts)
            {
                part = entry.Value;
                if (_partIds[i] != part.PartId) return false;
                if (_partVisibilities[i] != (part.Active && part.Visible)) return false;
                if (_partActivities[i] != part.Active) return false;
                if (!ReferenceEquals(_partNodeLabels[i], part.NodeLabels)) return false;
                if (!ReferenceEquals(_partElementLabels[i], part.Labels)) return false;
                i++;
            }
            return true;
        }
        public HashSet<int> GetVisibleNodeIds()
        {
            if (_visibleNodeIds == null) _visibleNodeIds = GetUnionOfVisibleLabels(_partNodeLabels);
            return _visibleNodeIds;
        }
        public HashSet<int> GetVisibleElementIds()
        {
            if (_visibleElementIds == null) _visibleElementIds = GetUnionOfVisibleLabels(_partElementLabels);
            return _visibleElementIds;
        }
        public HashSet<int> GetActiveNodeIds()
        {
            if (_activeNodeIds == null) _activeNodeIds = GetUnionOfLabels(_partNodeLabels, _partActivities);
            return _activeNodeIds;
        }
        public HashSet<int> GetActiveElementIds()
        {
            if (_activeElementIds == null) _activeElementIds = GetUnionOfLabels(_partElementLabels, _partActivities);
            return _activeElementIds;
        }
        private HashSet<int> GetUnionOfVisibleLabels(int[][] partLabels)
        {
            return GetUnionOfLabels(partLabels, _partVisibilities);
        }
        private static HashSet<int> GetUnionOfLabels(int[][] partLabels, bool[] partIncluded)
        {
            HashSet<int> ids = new HashSet<int>();
            for (int i = 0; i < partLabels.Length; i++)
            {
                if (partIncluded[i] && partLabels[i] != null) ids.UnionWith(partLabels[i]);
            }
            return ids;
        }
    }
}
