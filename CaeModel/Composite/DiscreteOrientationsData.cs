// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using CaeGlobals;

namespace CaeModel
{
    [PmxType(50012)]
    public class StackingLayer
    {
        [PmxKey(0)] public double Angle;
        [PmxKey(1)] public double Thickness;
    }

    [PmxType(50013)]
    public class StackingPoint
    {
        [PmxKey(0)] public int Id;                  // 1-based identifier from XML
        [PmxKey(1)] public double[] Position;       // [3] XYZ
        [PmxKey(2)] public double[] MaterialRefDir; // [3] unit vector, null => use DiscreteOrientationsData.GlobalRosette
        [PmxKey(3)] public List<StackingLayer> Layers;
    }

    [PmxType(50014)]
    public class DiscreteOrientationsData
    {
        [PmxKey(0)] public string XmlFilePath;
        [PmxKey(1)] public double[] GlobalRosette;  // [3] part-level rosette main direction (mandatory)
        [PmxKey(2)] public string UnitsLength;      // e.g. "mm"
        [PmxKey(3)] public string UnitsAngle;       // e.g. "deg"
        [PmxKey(4)] public List<StackingPoint> Points;
    }
}
