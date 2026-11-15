// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeGlobals;

namespace CaeMesh
{
    [PmxType(2143)]
    public struct PartProperties
    {
        // Only the properties, that cannot be changed by any other edit form (like MeshingParameters...)
        public string Name
        {
            get { return _name; }
            set
            {
                CaeGlobals.NamedClass.CheckNameForErrors(ref value, null);
                _name = value;
            }
        }
        [PmxKey(0)] public PartType PartType;
        [PmxKey(1)] public bool IsCADPart;
        [PmxKey(2)] public System.Drawing.Color Color;
        [PmxKey(3)] public bool ColorContours;
        [PmxKey(4)] public int NumberOfElements;
        [PmxKey(5)] public int NumberOfNodes;
        [PmxKey(6)] public PartMassProperties MassProperties;
        //
        [PmxKey(7)] public FeElementTypeLinearBeam LinearBeamType;
        [PmxKey(8)] public FeElementTypeParabolicBeam ParabolicBeamType;
        //
        [PmxKey(9)] public FeElementTypeLinearTria LinearTriaType;
        [PmxKey(10)] public FeElementTypeParabolicTria ParabolicTriaType;
        [PmxKey(11)] public FeElementTypeLinearQuad LinearQuadType;
        [PmxKey(12)] public FeElementTypeParabolicQuad ParabolicQuadType;
        //
        [PmxKey(13)] public FeElementTypeLinearTetra LinearTetraType;
        [PmxKey(14)] public FeElementTypeParabolicTetra ParabolicTetraType;
        [PmxKey(15)] public FeElementTypeLinearPyramid LinearPyramidType;
        [PmxKey(16)] public FeElementTypeParabolicPyramid ParabolicPyramidType;
        [PmxKey(17)] public FeElementTypeLinearWedge LinearWedgeType;
        [PmxKey(18)] public FeElementTypeParabolicWedge ParabolicWedgeType;
        [PmxKey(19)] public FeElementTypeLinearHexa LinearHexaType;
        [PmxKey(20)] public FeElementTypeParabolicHexa ParabolicHexaType;
        //
        [PmxKey(21)] private string _name;
    }
}
