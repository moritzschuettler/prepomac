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
    public class NodesExchangeData
    {
        // Variables                                                                                                                
        public int[] Ids;
        public double[][] Coor;
        public float[] FlatCoor;                            // x, y, z, x, y, z, ...
        public double[][] Normals;
        public float[] Values;
        public ResultFieldExchangeData ResultField;


        // Constructors                                                                                                             
        public NodesExchangeData()
        {
            Reset();
        }
        public NodesExchangeData(NodesExchangeData nodesExchangeData)
        {
            Reset();
            //
            if (nodesExchangeData.Ids != null) Ids = nodesExchangeData.Ids.ToArray();
            if (nodesExchangeData.Coor != null)
            {
                Coor = new double[nodesExchangeData.Coor.Length][];
                for (int i = 0; i < Coor.Length; i++) Coor[i] = nodesExchangeData.Coor[i].ToArray();
            }
            if (nodesExchangeData.FlatCoor != null) FlatCoor = nodesExchangeData.FlatCoor.ToArray();
            if (nodesExchangeData.Normals != null)
            {
                Normals = new double[nodesExchangeData.Normals.Length][];
                for (int i = 0; i < Normals.Length; i++) Normals[i] = nodesExchangeData.Normals[i].ToArray();
            }
            if (nodesExchangeData.Values != null) Values = nodesExchangeData.Values.ToArray();
            if (nodesExchangeData.ResultField != null) ResultField = nodesExchangeData.ResultField.DeepCopy();
        }


        // Methods                                                                                                                  
        private void Reset()
        {
            Ids = null;
            Coor = null;
            FlatCoor = null;
            Normals = null;
            Values = null;
            ResultField = null;
        }
        public NodesExchangeData DeepCopy()
        {
            return new NodesExchangeData(this);
        }
    }
}
