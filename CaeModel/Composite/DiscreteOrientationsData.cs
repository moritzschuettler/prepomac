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

namespace CaeModel
{
    [Serializable]
    public class StackingLayer
    {
        public double Angle;
        public double Thickness;
    }

    [Serializable]
    public class StackingPoint
    {
        public int Id;                  // 1-based identifier from XML
        public double[] Position;       // [3] XYZ
        public double[] MaterialRefDir; // [3] unit vector, null => use DiscreteOrientationsData.GlobalRosette
        public List<StackingLayer> Layers;
    }

    [Serializable]
    public class DiscreteOrientationsData
    {
        public string XmlFilePath;
        public double[] GlobalRosette;  // [3] part-level rosette main direction (mandatory)
        public string UnitsLength;      // e.g. "mm"
        public string UnitsAngle;       // e.g. "deg"
        public List<StackingPoint> Points;
    }
}
