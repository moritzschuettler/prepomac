using System;
using CaeGlobals;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public abstract class FeElement3D : FeElement
    {
        // Variables                                                                                                                
        private static CompareIntArray _comparer = new CompareIntArray();


        // Properties                                                                                                               
        public override int Dimension { get { return 3; } }


        // Constructors                                                                                                             
        public FeElement3D(int id, int[] nodeIds)
            : base(id, nodeIds)
        {
        }
        public FeElement3D(int id, int partId, int[] nodeIds)
            : base(id, partId, nodeIds)
        {
        }


        // Methods                                                                                                                  
        virtual public int GetVtkCellIdFromCell(int[] cell)
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
        public abstract double GetVolume(Dictionary<int, FeNode> nodes);
    }
}
