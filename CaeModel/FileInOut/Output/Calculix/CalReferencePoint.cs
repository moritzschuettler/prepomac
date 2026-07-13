// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.

using System;

namespace FileInOut.Output.Calculix
{
    [Serializable]
    public class CalReferencePoint
    {
        public int ReferenceNodeId { get; set; }
        public int? RotationNodeId { get; set; }
        public string ReferenceNodeSetName { get; set; }
        public string RotationNodeSetName { get; set; }
        public bool UsedByRigidBody { get; set; }
        public bool UsedByCoupling { get; set; }

        public bool HasRotationNode { get { return RotationNodeId.HasValue; } }
    }
}
