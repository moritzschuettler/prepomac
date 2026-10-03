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
    /// A mesh of parts and cells that are revolved about an axis their source touches. It is built by the blocks of
    /// the sweep, every one of them revolved, so it meshes one part, several parts and the cells of compounds alike,
    /// and the sweep item takes the compounds that mix swept and revolved cells.
    /// </summary>
    [Serializable]
    public class RevolveMesh : SweepMesh, ISerializable
    {
        // Constructors
        public RevolveMesh(string name)
            : base(name)
        {
        }
        public RevolveMesh(SweepMesh sweepMesh)
            : base("tmpName")
        {
            CopyFrom(sweepMesh);
        }
        public RevolveMesh(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            // An item written by the revolve mesher this one replaced carried the axis, the angle and the full 360
            // degree model, which the blocks now find for themselves; its source type is read by the sweep item
        }
    }
}
