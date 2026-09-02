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
    internal class QuadWildEdgeGrid
    {
        // Variables
        private const int MaxCellsPerAxis = 256;
        private const int MaxCellsPerItem = 4096;
        //
        private double _cellSize;
        private double[] _origin;
        private int[] _cellCounts;
        private Dictionary<int, List<int>> _cellItemIds;
        private List<int> _oversizedItemIds;
        private int[] _itemQueryIds;
        private int _queryId;


        // Constructors
        public QuadWildEdgeGrid(List<double[]> itemBoxes)
        {
            _cellItemIds = new Dictionary<int, List<int>>();
            _oversizedItemIds = new List<int>();
            _itemQueryIds = new int[itemBoxes.Count];
            _queryId = 0;
            _origin = new double[] { 0, 0, 0 };
            _cellCounts = new int[] { 1, 1, 1 };
            _cellSize = 1;
            if (itemBoxes.Count == 0) return;
            //
            double[] boxMin = new double[] { Double.MaxValue, Double.MaxValue, Double.MaxValue };
            double[] boxMax = new double[] { -Double.MaxValue, -Double.MaxValue, -Double.MaxValue };
            double extentSum = 0;
            foreach (double[] box in itemBoxes)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (box[i] < boxMin[i]) boxMin[i] = box[i];
                    if (box[i + 3] > boxMax[i]) boxMax[i] = box[i + 3];
                }
                double dx = box[3] - box[0];
                double dy = box[4] - box[1];
                double dz = box[5] - box[2];
                extentSum += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            double diagonal = Math.Sqrt((boxMax[0] - boxMin[0]) * (boxMax[0] - boxMin[0]) +
                                        (boxMax[1] - boxMin[1]) * (boxMax[1] - boxMin[1]) +
                                        (boxMax[2] - boxMin[2]) * (boxMax[2] - boxMin[2]));
            double meanExtent = extentSum / itemBoxes.Count;
            if (meanExtent <= 0) meanExtent = diagonal > 0 ? diagonal / 64 : 1;
            _cellSize = Math.Max(meanExtent, diagonal / MaxCellsPerAxis);
            if (_cellSize <= 0) _cellSize = 1;
            _origin = boxMin;
            for (int i = 0; i < 3; i++)
            {
                _cellCounts[i] = Math.Max(1, Math.Min(MaxCellsPerAxis,
                                                      (int)((boxMax[i] - boxMin[i]) / _cellSize) + 1));
            }
            //
            for (int itemId = 0; itemId < itemBoxes.Count; itemId++) Add(itemId, itemBoxes[itemId]);
        }


        // Methods
        public void Query(double[] box, double tolerance, List<int> itemIds)
        {
            itemIds.Clear();
            _queryId++;
            foreach (int itemId in _oversizedItemIds)
            {
                _itemQueryIds[itemId] = _queryId;
                itemIds.Add(itemId);
            }
            GetCellRange(box, tolerance, out int[] minCell, out int[] maxCell);
            List<int> cellItemIds;
            for (int i = minCell[0]; i <= maxCell[0]; i++)
            {
                for (int j = minCell[1]; j <= maxCell[1]; j++)
                {
                    for (int k = minCell[2]; k <= maxCell[2]; k++)
                    {
                        if (!_cellItemIds.TryGetValue(GetCellKey(i, j, k), out cellItemIds)) continue;
                        foreach (int itemId in cellItemIds)
                        {
                            if (_itemQueryIds[itemId] == _queryId) continue;
                            _itemQueryIds[itemId] = _queryId;
                            itemIds.Add(itemId);
                        }
                    }
                }
            }
        }
        private void Add(int itemId, double[] box)
        {
            GetCellRange(box, 0, out int[] minCell, out int[] maxCell);
            long cellCount = (long)(maxCell[0] - minCell[0] + 1) * (maxCell[1] - minCell[1] + 1) *
                             (maxCell[2] - minCell[2] + 1);
            // An item whose box spans an unreasonable part of the grid is returned by every query instead
            // of being rasterized; this keeps a single outlier from filling the grid.
            if (cellCount > MaxCellsPerItem)
            {
                _oversizedItemIds.Add(itemId);
                return;
            }
            List<int> cellItemIds;
            for (int i = minCell[0]; i <= maxCell[0]; i++)
            {
                for (int j = minCell[1]; j <= maxCell[1]; j++)
                {
                    for (int k = minCell[2]; k <= maxCell[2]; k++)
                    {
                        int key = GetCellKey(i, j, k);
                        if (!_cellItemIds.TryGetValue(key, out cellItemIds))
                        {
                            cellItemIds = new List<int>();
                            _cellItemIds.Add(key, cellItemIds);
                        }
                        cellItemIds.Add(itemId);
                    }
                }
            }
        }
        private void GetCellRange(double[] box, double tolerance, out int[] minCell, out int[] maxCell)
        {
            minCell = new int[3];
            maxCell = new int[3];
            for (int i = 0; i < 3; i++)
            {
                minCell[i] = GetCellIndex(box[i] - tolerance, i);
                maxCell[i] = GetCellIndex(box[i + 3] + tolerance, i);
            }
        }
        private int GetCellIndex(double value, int axis)
        {
            int index = (int)Math.Floor((value - _origin[axis]) / _cellSize);
            if (index < 0) index = 0;
            else if (index >= _cellCounts[axis]) index = _cellCounts[axis] - 1;
            return index;
        }
        private int GetCellKey(int i, int j, int k)
        {
            return (i * _cellCounts[1] + j) * _cellCounts[2] + k;
        }
    }
}
