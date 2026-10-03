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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;
using DynamicTypeDescriptor;
using GmshCommon;

namespace CaeMesh
{
    /// <summary>
    /// The revolve mesh setup item of older files. The sweep item revolves every part that is revolved about an axis
    /// its source touches, so an item read from a file is turned into a sweep item when the file is opened.
    /// </summary>
    [Serializable]
    public class RevolveMesh : SweepMesh, ISerializable
    {
        // Constructors
        public RevolveMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // The axis, the angle and the full 360 degree model of the revolve mesher are found by the blocks now;
            // its source type is read by the sweep item
        }
    }
}
