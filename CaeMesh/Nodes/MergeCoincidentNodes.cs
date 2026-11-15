// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
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
    [PmxType(2133)]
    public enum NodesToKeepEnum
    {
        [StandardValue("SmallerID", DisplayName = "Smaller ID")]
        SmallerID,
        [StandardValue("LargerID", DisplayName = "Larger ID")]
        LargerID
    }
    //
    [Serializable]
    [PmxType(2134)]
    public class MergeCoincidentNodes
    {
        // Properties                                                                                                               
        [PmxKey(0)] public int[] GeometryIds;
        [PmxKey(1)] public Selection CreationData;
        [PmxKey(2)] public double Tolerance;
        [PmxKey(3)] public NodesToKeepEnum NodesToKeep;


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
