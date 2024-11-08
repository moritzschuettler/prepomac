using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{
    public enum CloudInterpolatorEnum
    {
        [StandardValue("ClosestPoint", DisplayName = "Closest point")]
        ClosestPoint,
        [StandardValue("Gauss", DisplayName = "Gauss")]
        Gauss,
        [StandardValue("Shepard", DisplayName = "Shepard")]
        Shepard
    }
}
