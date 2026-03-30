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
using DynamicTypeDescriptor;

namespace CaeResults
{
    [Serializable]
    public enum SlipWearResultsEnum
    {
        [StandardValue("All", DisplayName = "All")]
        All,
        //
        [StandardValue("SlipWearSteps", DisplayName = "Slip wear steps", Description = "SWS")]
        SlipWearSteps,
        //
        [StandardValue("LastIncrementOfSlipWearSteps", DisplayName = "Last increment of slip wear steps")]
        LastIncrementOfSlipWearSteps,
        //
        [StandardValue("LastIncrementOfLastSlipWearStep", DisplayName = "Last increment of last slip wear step")]
        LastIncrementOfLastSlipWearStep,
        //
        [StandardValue("LastIncrementOfAllSteps", DisplayName = "Last increment of all steps")]
        LastIncrementOfAllSteps
    }
}
