// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeMesh
{
    [Serializable]
    public enum NodesToKeepEnum
    {
        [StandardValue("SmallerID", DisplayName = "Smaller ID")]
        SmallerID,
        [StandardValue("LargerID", DisplayName = "Larger ID")]
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
