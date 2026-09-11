// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace vtkControl
{
    public struct FaceNodeKey : IEquatable<FaceNodeKey>
    {
        // Variables                                                                                                    
        private readonly int[] _sortedNodeIds;
        private readonly int _hashCode;


        // Constructors                                                                                                 
        public FaceNodeKey(long[] connectivity, int start, int count, int[] pointNodeIds)
        {
            _sortedNodeIds = new int[count];
            for (int i = 0; i < count; i++) _sortedNodeIds[i] = pointNodeIds[connectivity[start + i]];
            _hashCode = SortAndHash(_sortedNodeIds);
        }
        public FaceNodeKey(int[] nodeIds)
        {
            _sortedNodeIds = nodeIds;
            _hashCode = SortAndHash(_sortedNodeIds);
        }


        // Methods                                                                                                      
        private static int SortAndHash(int[] nodeIds)
        {
            Array.Sort(nodeIds);
            int hashCode = nodeIds.Length;
            for (int i = 0; i < nodeIds.Length; i++) hashCode = unchecked(hashCode * 31 + nodeIds[i]);
            return hashCode;
        }
        public bool Equals(FaceNodeKey other)
        {
            if (_sortedNodeIds.Length != other._sortedNodeIds.Length) return false;
            for (int i = 0; i < _sortedNodeIds.Length; i++)
            {
                if (_sortedNodeIds[i] != other._sortedNodeIds[i]) return false;
            }
            return true;
        }
        public override bool Equals(object obj)
        {
            return obj is FaceNodeKey && Equals((FaceNodeKey)obj);
        }
        public override int GetHashCode()
        {
            return _hashCode;
        }
    }
}
