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


namespace CaeGlobals
{
    public interface IMasterSlaveMultiRegion
    {
        string MasterRegionName { get; set; }
        RegionTypeEnum MasterRegionType { get; set; }
        string SlaveRegionName { get; set; }
        RegionTypeEnum SlaveRegionType { get; set; }
        //
        int[] MasterCreationIds { get; set; }
        Selection MasterCreationData { get; set; }
        int[] SlaveCreationIds { get; set; }
        Selection SlaveCreationData { get; set; }
    }
}
