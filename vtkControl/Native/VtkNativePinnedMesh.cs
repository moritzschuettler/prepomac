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
using System.Runtime.InteropServices;

namespace vtkControl.Native
{
    // A vtkn_mesh pointing into pinned managed arrays; the arrays stay pinned, and the mesh valid, until Dispose
    internal sealed class VtkNativePinnedMesh : IDisposable
    {
        // Variables                                                                                                                
        private readonly List<GCHandle> _handles;
        private VtkNativeMesh _mesh;


        // Properties                                                                                                               
        public VtkNativeMesh Mesh { get { return _mesh; } }


        // Constructors                                                                                                             
        public VtkNativePinnedMesh(double[] coordinates, int[] cells, byte[] cellTypes, int[] pointIds, int[] cellIds)
        {
            _handles = new List<GCHandle>();
            try
            {
                _mesh.Coordinates = PinArray(coordinates);
                _mesh.PointCount = coordinates.Length / 3;
                _mesh.Cells = PinArray(cells);
                _mesh.CellsLength = cells.Length;
                _mesh.CellTypes = PinArray(cellTypes);
                _mesh.CellCount = cellTypes.Length;
                _mesh.PointIds = PinArray(pointIds);
                _mesh.CellIds = PinArray(cellIds);
            }
            catch
            {
                Dispose();
                throw;
            }
        }


        // Methods                                                                                                                  
        public void Dispose()
        {
            foreach (GCHandle handle in _handles) handle.Free();
            _handles.Clear();
        }
        private IntPtr PinArray(Array array)
        {
            if (array == null || array.Length == 0) return IntPtr.Zero;
            GCHandle handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            _handles.Add(handle);
            return handle.AddrOfPinnedObject();
        }
    }
}
