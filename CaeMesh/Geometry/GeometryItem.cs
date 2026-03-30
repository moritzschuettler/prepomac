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

namespace CaeMesh
{
    public abstract class GeometryItem
    {
        // Variables                                                                                                                        
        public int Id;
        public int[] Labels;


        // Constructors                                                                                                             
        public GeometryItem(int id, int[] labels)
        {
            Id = id;
            Labels = labels;
        }
    }
}
