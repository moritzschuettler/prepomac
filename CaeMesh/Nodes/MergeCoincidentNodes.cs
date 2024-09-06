using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public enum NodesToKeepEnum
    {
        SmallerID,
        LargerID
    }
    //
    [Serializable]
    public class MergeCoincidentNodes
    {
        // Properties                                                                                                               
        public int[] GeometryIds;
        public Selection CreationData;
        public double Tolerance;
        public NodesToKeepEnum NodesToKeep;


        // Constructors                                                                                                             
        public MergeCoincidentNodes()
        {
            GeometryIds = null;
            CreationData = null;
            Tolerance = 1E-3;
            NodesToKeep = NodesToKeepEnum.SmallerID;
        }
    }
    //
}
