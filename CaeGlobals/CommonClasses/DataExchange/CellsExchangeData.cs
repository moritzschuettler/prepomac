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
    public class CellsExchangeData
    {
        // Variables                                                                                                                
        public int[] Ids;
        public int[][] CellNodeIds;
        public int[] Types;
        public float[] Values;


        // Constructors                                                                                                             
        public CellsExchangeData()
        {
            Reset();
        }
        public CellsExchangeData(CellsExchangeData cellsExchangeData)
        {
            Reset();
            //
            if (cellsExchangeData.Ids != null) Ids = cellsExchangeData.Ids.ToArray();
            if (cellsExchangeData.CellNodeIds != null)
            {
                CellNodeIds = new int[cellsExchangeData.CellNodeIds.Length][];
                for (int i = 0; i < CellNodeIds.Length; i++) CellNodeIds[i] = cellsExchangeData.CellNodeIds[i].ToArray();
            }
            if (cellsExchangeData.Types != null) Types = cellsExchangeData.Types.ToArray();
            if (cellsExchangeData.Values != null) Values = cellsExchangeData.Values.ToArray();
        }


        // Methods                                                                                                                  
        private void Reset()
        {
            Ids = null;
            CellNodeIds = null;
            Types = null;
            Values = null;
        }
        public CellsExchangeData DeepCopy()
        {
            return new CellsExchangeData(this);
        }
    }
}
