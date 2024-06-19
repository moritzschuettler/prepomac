using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicTypeDescriptor
{
    [Serializable]
    [Editor(typeof(StandardValueEditor), typeof(System.Drawing.Design.UITypeEditor))]
    [Flags]
    public enum MultiChoiceEnum
    {
        // Must start at 1 for the UI to work
        // NumAll = 2147483647,
        Num1 = 1,
        Num2 = 2,
        Num3 = 4,
        Num4 = 8,
        Num5 = 16,
        Num6 = 32,
        Num7 = 64,
        Num8 = 128,
        Num9 = 256,
        Num10 = 512,
        Num11 = 1024,
        Num12 = 2048,
        Num13 = 4096,
        Num14 = 8192,
        Num15 = 16384,
        Num16 = 32768,
        Num17 = 65536,
        Num18 = 131072,
        Num19 = 262144,
        Num20 = 524288,
        Num21 = 1048576,
        Num22 = 2097152,
        Num23 = 4194304,
        Num24 = 8388608,
        Num25 = 16777216,
        Num26 = 33554432,
        Num27 = 67108864,
        Num28 = 134217728,
        Num29 = 268435456,
        Num30 = 536870912,
        Num31 = 1073741824,
    }
}
