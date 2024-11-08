using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;

namespace CaeResults
{
    public enum InterpolatorEnum
    {
        [StandardValue("ClosestNode", DisplayName = "Closest node")]
        ClosestNode,
        [StandardValue("ClosestPoint", DisplayName = "Closest point")]
        ClosestPoint
    }
}
