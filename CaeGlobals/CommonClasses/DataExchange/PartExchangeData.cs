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

    }
}
