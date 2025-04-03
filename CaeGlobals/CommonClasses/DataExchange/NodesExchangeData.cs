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
        public double[][] Normals;
        public float[] Values;


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
            if (nodesExchangeData.Normals != null)
            {
                Normals = new double[nodesExchangeData.Normals.Length][];
                for (int i = 0; i < Normals.Length; i++) Normals[i] = nodesExchangeData.Normals[i].ToArray();
            }
            if (nodesExchangeData.Values != null) Values = nodesExchangeData.Values.ToArray();
        }


        // Methods                                                                                                                  
        private void Reset()
        {
            Ids = null;
            Coor = null;
            Normals = null;
            Values = null;
        }
        public NodesExchangeData DeepCopy()
        {
            return new NodesExchangeData(this);
        }
    }
}
