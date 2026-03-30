// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using CaeGlobals;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public abstract class FeElement2D : FeElement
    {
        // Variables                                                                                                                
        private static CompareIntArray _comparer = new CompareIntArray();


        // Properties                                                                                                               
        public override int Dimension { get { return 2; } }


        // Constructors                                                                                                             
        public FeElement2D(int id, int[] nodeIds)
            : base(id, nodeIds)
        {
        }
        public FeElement2D(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        public int[] GetAllVtkCellIdsFromNodeIds(HashSet<int> nodeIds)
        {
            List<int> vtkCellIds = new List<int>();
            int[][] cells = GetAllVtkCells();
            bool allNodesInside;
            for (int i = 0; i < cells.Length; i++)
            {
                allNodesInside = true;
                for (int j = 0; j < cells[i].Length; j++)
                {
                    if (!nodeIds.Contains(cells[i][j]))
                    {
                        allNodesInside = false;
                        break;
                    }
                }
                if (allNodesInside) vtkCellIds.Add(i);
            }
            return vtkCellIds.ToArray();
        }
        public int GetVtkCellIdFromCell(int[] cell)
        {
            int vtkCellId = -1;
            int[][] cells = GetAllVtkCells();
            for (int i = 0; i < cells.Length; i++)
            {
                if (_comparer.Equals(cell, cells[i]))
                {
                    vtkCellId = i;
                    break;
                }
            }
            return vtkCellId;
        }
    }
}
