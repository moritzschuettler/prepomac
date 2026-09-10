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
            Array.Sort(_sortedNodeIds);
            //
            int hashCode = count;
            for (int i = 0; i < count; i++) hashCode = unchecked(hashCode * 31 + _sortedNodeIds[i]);
            _hashCode = hashCode;
        }


        // Methods                                                                                                      
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
