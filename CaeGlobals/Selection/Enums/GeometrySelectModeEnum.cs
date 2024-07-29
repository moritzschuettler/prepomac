using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaeGlobals
{
    public enum GeometrySelectModeEnum
    {
        [StandardValue("SelectLocation", DisplayName = "Selection by location")]
        SelectLocation,
        [StandardValue("SelectId", DisplayName = "Selection by ID")]
        SelectId
    }
}
