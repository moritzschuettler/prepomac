// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    /// <summary>
    /// Two sides of a structured face that must carry the same number of elements, each a chain of edges.
    /// </summary>
    public class TransfiniteSidePair
    {
        // Variables                                                                                                                
        public int FaceId;
        public int[] SideA;
        public int[] SideB;


        // Constructors                                                                                                             
        public TransfiniteSidePair(int faceId, int[] sideA, int[] sideB)
        {
            FaceId = faceId;
            SideA = sideA;
            SideB = sideB;
        }
    }
}
