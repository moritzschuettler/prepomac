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
using System.Drawing;

namespace CaeGlobals
{
    [Serializable]
    public class PartExchangeData
    {
        // Variables                                                                                                                
        public NodesExchangeData Nodes;
        public CellsExchangeData Cells;
        public NodesExchangeData ExtremeNodes;              // [0 - min, 1 - max]
        public NodesExchangeData[] NodesAnimation;          // [frame]
        public NodesExchangeData[] ExtremeNodesAnimation;   // [frame][0 - min, 1 - max]
        

        // Constructors                                                                                                             
        public PartExchangeData()
        {
            Reset();
        }
        public PartExchangeData(PartExchangeData partExchangeData)
        {
            Reset();
            //
            if (partExchangeData.Nodes != null) Nodes = partExchangeData.Nodes.DeepCopy();
            if (partExchangeData.Cells != null) Cells = partExchangeData.Cells.DeepCopy();
            if (partExchangeData.ExtremeNodes != null) ExtremeNodes = partExchangeData.ExtremeNodes.DeepCopy();
            if (partExchangeData.NodesAnimation != null)
            {
                NodesAnimation = new NodesExchangeData[partExchangeData.NodesAnimation.Length];
                for (int i = 0; i < NodesAnimation.Length; i++) NodesAnimation[i] = partExchangeData.NodesAnimation[i].DeepCopy();
            }
            if (partExchangeData.ExtremeNodesAnimation != null)
            {
                ExtremeNodesAnimation = new NodesExchangeData[partExchangeData.ExtremeNodesAnimation.Length];
                for (int i = 0; i < ExtremeNodesAnimation.Length; i++)
                    ExtremeNodesAnimation[i] = partExchangeData.ExtremeNodesAnimation[i].DeepCopy();
            }
        }
        
        
        // Methods                                                                                                                  
        private void Reset()
        {
            Nodes = new NodesExchangeData();
            Cells = new CellsExchangeData();
            ExtremeNodes = new NodesExchangeData();
            NodesAnimation = null;
            ExtremeNodesAnimation = null;
        }
        public PartExchangeData DeepCopy()
        {
            return new PartExchangeData(this);
        }
        public void RemoveZeroLengthNormals()
        {
            List<double[]> normals = new List<double[]>();
            List<double[]> coor = new List<double[]>();
            //
            double len;
            double max = -double.MaxValue;
            double[] normal;
            //
            for (int i = 0; i < Nodes.Normals.Length; i++)
            {
                normal = Nodes.Normals[i];
                len = normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2];
                if (len > max) max = len;
            }
            //
            for (int i = 0; i < Nodes.Normals.Length; i++)
            {
                normal = Nodes.Normals[i];
                len = normal[0] * normal[0] + normal[1] * normal[1] + normal[2] * normal[2];
                if (len > max * 1E-3)
                {
                    normals.Add(normal);
                    coor.Add(Nodes.Coor[i]);
                }
            }
            //
            Nodes.Normals = normals.ToArray();
            Nodes.Coor = coor.ToArray();
        }

    }
}
