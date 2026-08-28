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

namespace CaeMesh
{
    /// <summary>
    /// A uniform spatial hash of the cell bounding boxes of one visualization face, used to answer the
    /// exploded view face to face proximity queries in a time that does not depend on the face cell count.
    /// The grid spacing is the largest cell bounding box diagonal, so a box never spans more than a few
    /// grid cells.
    /// </summary>
    public class ExplodedViewCellGrid
    {
        // Variables                                                                                                                
        private const int _indexOffset = 1 << 20;
        private const int _maxIndex = (1 << 20) - 1;
        //
        private BoundingBox[] _boxes;
        private double _spacing;
        private Dictionary<long, List<int>> _boxIdsByGridCell;


        // Constructors                                                                                                             
        public ExplodedViewCellGrid(BoundingBox[] boxes)
        {
            _boxes = boxes;
            //
            _spacing = 0;
            for (int i = 0; i < _boxes.Length; i++)
            {
                double diagonal = _boxes[i].GetDiagonal();
                if (diagonal > _spacing) _spacing = diagonal;
            }
            if (_spacing <= 0) _spacing = 1;
            //
            _boxIdsByGridCell = new Dictionary<long, List<int>>();
            for (int i = 0; i < _boxes.Length; i++)
            {
                foreach (long key in GetKeys(_boxes[i]))
                {
                    List<int> boxIds;
                    if (!_boxIdsByGridCell.TryGetValue(key, out boxIds))
                    {
                        boxIds = new List<int>();
                        _boxIdsByGridCell.Add(key, boxIds);
                    }
                    boxIds.Add(i);
                }
            }
        }


        // Methods                                                                                                                  
        public bool Intersects(BoundingBox box)
        {
            foreach (long key in GetKeys(box))
            {
                List<int> boxIds;
                if (!_boxIdsByGridCell.TryGetValue(key, out boxIds)) continue;
                //
                for (int i = 0; i < boxIds.Count; i++)
                {
                    if (_boxes[boxIds[i]].Intersects(box)) return true;
                }
            }
            return false;
        }
        private IEnumerable<long> GetKeys(BoundingBox box)
        {
            int minIndexX = GetIndex(box.MinX);
            int maxIndexX = GetIndex(box.MaxX);
            int minIndexY = GetIndex(box.MinY);
            int maxIndexY = GetIndex(box.MaxY);
            int minIndexZ = GetIndex(box.MinZ);
            int maxIndexZ = GetIndex(box.MaxZ);
            //
            for (int i = minIndexX; i <= maxIndexX; i++)
            {
                for (int j = minIndexY; j <= maxIndexY; j++)
                {
                    for (int k = minIndexZ; k <= maxIndexZ; k++) yield return GetKey(i, j, k);
                }
            }
        }
        private int GetIndex(double coordinate)
        {
            double index = Math.Floor(coordinate / _spacing);
            if (index < -_indexOffset) return -_indexOffset;
            else if (index > _maxIndex) return _maxIndex;
            else return (int)index;
        }
        private static long GetKey(int indexX, int indexY, int indexZ)
        {
            return ((long)(indexX + _indexOffset) << 42) |
                   ((long)(indexY + _indexOffset) << 21) |
                   (long)(indexZ + _indexOffset);
        }
    }
}
