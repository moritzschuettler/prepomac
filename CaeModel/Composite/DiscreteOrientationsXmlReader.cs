using System;
using System.Collections.Generic;
using System.Xml;
using CaeGlobals;

namespace CaeModel
{
    public static class DiscreteOrientationsXmlReader
    {
        public static DiscreteOrientationsData Read(string fileName)
        {
            var data = new DiscreteOrientationsData();
            data.XmlFilePath = fileName;
            data.Points = new List<StackingPoint>();

            var doc = new XmlDocument();
            doc.Load(fileName);

            XmlNodeList pointNodes = doc.SelectNodes("/DiscreteOrientations/StackingPoint");
            if (pointNodes == null || pointNodes.Count == 0)
                throw new CaeException("No StackingPoint elements found in: " + fileName);

            foreach (XmlNode pn in pointNodes)
            {
                var sp = new StackingPoint();
                sp.Position = ReadXYZ(pn, "Position", fileName);
                sp.MaterialRefDir = ReadXYZ(pn, "MaterialRefDir", fileName);
                sp.Layers = new List<StackingLayer>();

                Normalize(sp.MaterialRefDir, fileName);

                foreach (XmlNode ln in pn.SelectNodes("Layer"))
                {
                    var layer = new StackingLayer();
                    layer.Angle = GetDouble(ln, "Angle", fileName);
                    layer.Thickness = GetDouble(ln, "Thickness", fileName);
                    if (layer.Thickness <= 0)
                        throw new CaeException("Layer Thickness must be > 0 in: " + fileName);
                    sp.Layers.Add(layer);
                }

                if (sp.Layers.Count == 0)
                    throw new CaeException("StackingPoint has no Layer children in: " + fileName);

                data.Points.Add(sp);
            }

            return data;
        }

        private static double[] ReadXYZ(XmlNode parent, string childName, string fileName)
        {
            XmlNode n = parent.SelectSingleNode(childName);
            if (n == null)
                throw new CaeException("Missing <" + childName + "> in: " + fileName);
            return new double[]
            {
                GetDouble(n, "X", fileName),
                GetDouble(n, "Y", fileName),
                GetDouble(n, "Z", fileName)
            };
        }

        private static double GetDouble(XmlNode node, string attr, string fileName)
        {
            XmlAttribute a = node.Attributes[attr];
            if (a == null)
                throw new CaeException("Missing attribute '" + attr + "' on <" + node.Name + "> in: " + fileName);
            double v;
            if (!double.TryParse(a.Value, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out v))
                throw new CaeException("Invalid numeric value '" + a.Value + "' for attribute '" + attr + "' in: " + fileName);
            return v;
        }

        private static void Normalize(double[] v, string fileName)
        {
            double len = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (len < 1e-12)
                throw new CaeException("MaterialRefDir is a zero vector in: " + fileName);
            v[0] /= len; v[1] /= len; v[2] /= len;
        }
    }
}
