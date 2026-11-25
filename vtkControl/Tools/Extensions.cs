// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vtkControl
{
    public static class Extensions
    {
        public static void Subtract(this HashSet<int> items, int[] itemsToRemove)
        {
            for (int i = 0; i < itemsToRemove.Length; i++)
            {
                items.Remove(itemsToRemove[i]);
            }
        }

       
    }
}
