// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;
using CaeGlobals;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    [PmxType(3057)]
    public class CalReferencePoint
    {
        [field: PmxKey(0)] public int ReferenceNodeId { get; set; }
        [field: PmxKey(1)] public int? RotationNodeId { get; set; }
        [field: PmxKey(2)] public string ReferenceNodeSetName { get; set; }
        [field: PmxKey(3)] public string RotationNodeSetName { get; set; }
        [field: PmxKey(4)] public bool UsedByRigidBody { get; set; }
        [field: PmxKey(5)] public bool UsedByCoupling { get; set; }
        [field: PmxKey(6)] public int MassNodeId { get; set; }

        public bool HasRotationNode { get { return RotationNodeId.HasValue; } }
    }
}
