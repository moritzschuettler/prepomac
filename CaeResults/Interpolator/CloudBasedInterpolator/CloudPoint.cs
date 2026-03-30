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
using CaeGlobals;
using CaeMesh;

namespace CaeResults
{
    public class CloudPoint
    {
        // Properties                                                                                                               
        public double[] Coor;
        public double[] Values;


        // Constructor                                                                                                              
        public CloudPoint()
        {
            Coor = null;
            Values = null;
        }
        public CloudPoint(CloudPoint cloudPoint)
        {
            Coor = cloudPoint.Coor.ToArray();
            Values = cloudPoint.Values.ToArray();
        }
    }
}
