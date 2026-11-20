// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrePoMax
{
    [PmxType(10007)]
    public class MinValueAnnotation : AnnotationBase
    {
        // Variables                                                                                                                


        // Properties                                                                                                               


        // Constructors                                                                                                             
        public MinValueAnnotation()
            : base(AnnotationContainer.MinAnnotationName)
        {
        }


        // Methods
        public override void GetAnnotationData(out string text, out double[] coor)
        {
            text = "MinAnnotation";
            coor = new double[3];
        }

    }
}
