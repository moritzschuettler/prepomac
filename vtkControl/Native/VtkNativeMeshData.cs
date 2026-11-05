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
using CaeGlobals;

namespace vtkControl.Native
{
    // A mesh as the flat arrays the C API takes: the coordinates, the cells in the VTK legacy layout, the cell types
    // and the PrePoMax ids. Pin hands the native side a vtkn_mesh that points into these arrays until it is disposed
    public sealed class VtkNativeMeshData
    {
        // Variables
        private readonly double[] _coordinates;
        private readonly int[] _cells;
        private readonly byte[] _cellTypes;
        private readonly int[] _pointIds;
        private readonly int[] _cellIds;


        // Properties
        public int PointCount { get { return _coordinates.Length / 3; } }
        public int CellCount { get { return _cellTypes.Length; } }


        // Constructors
        public VtkNativeMeshData(double[] coordinates, int[] cells, byte[] cellTypes, int[] pointIds, int[] cellIds)
        {
            if (coordinates == null || coordinates.Length % 3 != 0)
                throw new ArgumentException("The coordinates must be a multiple of 3 values.", "coordinates");
            if (cells == null) throw new ArgumentNullException("cells");
            if (cellTypes == null) throw new ArgumentNullException("cellTypes");
            if (pointIds != null && pointIds.Length != coordinates.Length / 3)
                throw new ArgumentException("There must be one point id per point.", "pointIds");
            if (cellIds != null && cellIds.Length != cellTypes.Length)
                throw new ArgumentException("There must be one cell id per cell.", "cellIds");
            //
            _coordinates = coordinates;
            _cells = cells;
            _cellTypes = cellTypes;
            _pointIds = pointIds;
            _cellIds = cellIds;
        }


        // Static methods
        public static VtkNativeMeshData FromPartExchangeData(PartExchangeData data)
        {
            if (data == null) return null;
            double[] coordinates;
            if (data.Nodes.Coor != null)
            {
                coordinates = new double[3 * data.Nodes.Coor.Length];
                for (int i = 0; i < data.Nodes.Coor.Length; i++)
                {
                    coordinates[3 * i] = data.Nodes.Coor[i][0];
                    coordinates[3 * i + 1] = data.Nodes.Coor[i][1];
                    coordinates[3 * i + 2] = data.Nodes.Coor[i][2];
                }
            }
            else if (data.Nodes.FlatCoor != null)
            {
                coordinates = new double[data.Nodes.FlatCoor.Length];
                for (int i = 0; i < coordinates.Length; i++) coordinates[i] = data.Nodes.FlatCoor[i];
            }
            else coordinates = new double[0];
            //
            int[][] cellNodeIds = data.Cells.CellNodeIds ?? new int[0][];
            int length = 0;
            for (int i = 0; i < cellNodeIds.Length; i++) length += cellNodeIds[i].Length + 1;
            List<int> cells = new List<int>(length);
            byte[] cellTypes = new byte[cellNodeIds.Length];
            for (int i = 0; i < cellNodeIds.Length; i++)
            {
                cells.Add(cellNodeIds[i].Length);
                cells.AddRange(cellNodeIds[i]);
                cellTypes[i] = (byte)data.Cells.Types[i];
            }
            //
            int[] pointIds = data.Nodes.Ids != null && data.Nodes.Ids.Length == coordinates.Length / 3 ? data.Nodes.Ids : null;
            int[] cellIds = data.Cells.Ids != null && data.Cells.Ids.Length == cellTypes.Length ? data.Cells.Ids : null;
            return new VtkNativeMeshData(coordinates, cells.ToArray(), cellTypes, pointIds, cellIds);
        }


        // Methods                                                                                                                  
        internal VtkNativePinnedMesh Pin()
        {
            return new VtkNativePinnedMesh(_coordinates, _cells, _cellTypes, _pointIds, _cellIds);
        }
    }
}
