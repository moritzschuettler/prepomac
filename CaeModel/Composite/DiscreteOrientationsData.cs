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
        public double[] Position;       // [3] XYZ
        public double[] MaterialRefDir; // [3] unit vector
        public List<StackingLayer> Layers;
    }

    [Serializable]
    public class DiscreteOrientationsData
    {
        public string XmlFilePath;
        public List<StackingPoint> Points;
    }
}
