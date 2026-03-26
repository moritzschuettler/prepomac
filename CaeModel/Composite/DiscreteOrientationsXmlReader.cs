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

            XmlNode root = doc.SelectSingleNode("/DiscreteOrientations");
            if (root == null)
                throw new CaeException("Missing root <DiscreteOrientations> element in: " + fileName);

            // GlobalRosette — mandatory child element
            data.GlobalRosette = ReadXYZ(root, "GlobalRosette", fileName);
            Normalize(data.GlobalRosette, "GlobalRosette", fileName);

            // Units — mandatory child element immediately after GlobalRosette
            XmlNode unitsNode = root.SelectSingleNode("Units");
            if (unitsNode == null)
                throw new CaeException("Missing <Units> element in: " + fileName);
            data.UnitsLength = GetString(unitsNode, "Length", fileName);
            data.UnitsAngle  = GetString(unitsNode, "Angle",  fileName);

            XmlNodeList pointNodes = root.SelectNodes("StackingPoint");
            if (pointNodes == null || pointNodes.Count == 0)
                throw new CaeException("No <StackingPoint> elements found in: " + fileName);

            foreach (XmlNode pn in pointNodes)
            {
                var sp = new StackingPoint();
                sp.Id = GetInt(pn, "Id", fileName);
                sp.Position = ReadXYZ(pn, "Position", fileName);

                // <Stacking> wrapper — mandatory child of StackingPoint
                XmlNode stackingNode = pn.SelectSingleNode("Stacking");
                if (stackingNode == null)
                    throw new CaeException("<StackingPoint> is missing a <Stacking> child element in: " + fileName);

                // MaterialRefDir — optional attributes on <Stacking>; absent or incomplete => null (use GlobalRosette)
                sp.MaterialRefDir = ReadOptionalRefDir(stackingNode, fileName);

                sp.Layers = new List<StackingLayer>();
                foreach (XmlNode ln in stackingNode.SelectNodes("Layer"))
                {
                    var layer = new StackingLayer();
                    layer.Angle = GetDouble(ln, "Angle", fileName);
                    layer.Thickness = GetDouble(ln, "Thickness", fileName);
                    if (layer.Thickness <= 0)
                        throw new CaeException("Layer Thickness must be > 0 in: " + fileName);
                    sp.Layers.Add(layer);
                }

                if (sp.Layers.Count == 0)
                    throw new CaeException("<Stacking> has no <Layer> children in: " + fileName);

                data.Points.Add(sp);
            }

            return data;
        }

        // Reads optional MaterialRefDirX/Y/Z attributes from a <Stacking> node.
        // Returns null if any component attribute is absent (caller will use GlobalRosette).
        private static double[] ReadOptionalRefDir(XmlNode stackingNode, string fileName)
        {
            XmlAttribute ax = stackingNode.Attributes["MaterialRefDirX"];
            XmlAttribute ay = stackingNode.Attributes["MaterialRefDirY"];
            XmlAttribute az = stackingNode.Attributes["MaterialRefDirZ"];

            if (ax == null || ay == null || az == null)
                return null; // use GlobalRosette

            double[] v = new double[3];
            if (!TryParseDouble(ax.Value, out v[0]) ||
                !TryParseDouble(ay.Value, out v[1]) ||
                !TryParseDouble(az.Value, out v[2]))
                throw new CaeException(
                    "Invalid numeric value for MaterialRefDir attributes on <Stacking> in: " + fileName);

            Normalize(v, "Stacking MaterialRefDir", fileName);
            return v;
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

        private static string GetString(XmlNode node, string attr, string fileName)
        {
            XmlAttribute a = node.Attributes[attr];
            if (a == null)
                throw new CaeException("Missing attribute '" + attr + "' on <" + node.Name + "> in: " + fileName);
            return a.Value;
        }

        private static int GetInt(XmlNode node, string attr, string fileName)
        {
            XmlAttribute a = node.Attributes[attr];
            if (a == null)
                throw new CaeException("Missing attribute '" + attr + "' on <" + node.Name + "> in: " + fileName);
            int v;
            if (!int.TryParse(a.Value, out v))
                throw new CaeException(
                    "Invalid integer value '" + a.Value + "' for attribute '" + attr + "' in: " + fileName);
            return v;
        }

        private static double GetDouble(XmlNode node, string attr, string fileName)
        {
            XmlAttribute a = node.Attributes[attr];
            if (a == null)
                throw new CaeException("Missing attribute '" + attr + "' on <" + node.Name + "> in: " + fileName);
            double v;
            if (!TryParseDouble(a.Value, out v))
                throw new CaeException(
                    "Invalid numeric value '" + a.Value + "' for attribute '" + attr + "' in: " + fileName);
            return v;
        }

        private static bool TryParseDouble(string s, out double v)
        {
            return double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out v);
        }

        private static void Normalize(double[] v, string label, string fileName)
        {
            double len = Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
            if (len < 1e-12)
                throw new CaeException(label + " is a zero vector in: " + fileName);
            v[0] /= len; v[1] /= len; v[2] /= len;
        }
    }
}
