// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;

namespace CaeMesh
{
    /// <summary>
    /// The way a geometry part can be meshed, from the most structured mesh it supports to the least.
    /// </summary>
    public enum MeshTypeEnum
    {
        Unknown,
        Transfinite,
        Sweep,
        // Swept by turning the source about an axis it touches, which the sweep between two ends cannot do
        Revolve,
        Tetrahedral,
        // A shell face meshed without a structured grid
        Free,
        Other
    }
}
