using DynamicTypeDescriptor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    public enum ConvertPyramidsToEnum
    {
        [StandardValue("Wedges", DisplayName = "Collapsed wedges")]
        Wedges,
        [StandardValue("Hexahedrons", DisplayName = "Collapsed hexahedrons")]
        Hexahedrons
    }

}
