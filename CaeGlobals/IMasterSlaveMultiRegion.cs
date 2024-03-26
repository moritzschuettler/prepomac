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
