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

namespace CaeResults
{
    struct DatDataSet
    {
        public int StepId;
        public int IncrementId;
        public double Time;
        public string FieldName;
        public string SetName;
        public string BaseSetName;
        public string[] ComponentNames;
        public bool[] Locals;
        /// <summary>
        /// ComponentNames [0 ... num. of ids/values][0...num. of components] -> value
        /// </summary>
        public double[][] Values;

        public string GetHashKey()
        {
            return FieldName + "_" + SetName + "_" + StepId + "_" + IncrementId + "_" + Time;
        }
    }
}
