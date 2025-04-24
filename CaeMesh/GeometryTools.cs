using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace CaeMesh
{
    static class GeometryTools
    {
        // Variables                                                                                                                
        static double eps = 1E-10;
        // Edge
        static double[] e_xi = new double[] { -1.0 / Math.Sqrt(3), 1.0 / Math.Sqrt(3) };
        static double[] e_w = new double[] { 1.0, 1.0 };
        // Triangle
        static double[] t_xi = new double[] { 1.0 / 6.0, 2.0 / 3.0, 1.0 / 6.0 };
        static double[] t_eta = new double[] { 1.0 / 6.0, 1.0 / 6.0, 2.0 / 3.0 };
        static double[] t_w = new double[] { 1.0 / 6.0, 1.0 / 6.0, 1.0 / 6.0 };
        // Tetrahedron
        static double a = 0.7272727272727273;
        static double b = 0.0909090909090909;
        static double c = 0.315701149778202;
        static double d = 0.0531450498448169;
        static double e = 0.1884185567365411;
        static double f = 0.6220084679281462;
        static double w1 = 0.0300328053555596;  // scaled to 1/6
        static double w2 = 0.0059768592737158;  // scaled to 1/6
        static double w3 = 0.0115487787842829;  // scaled to 1/6
        static double w4 = 0.0108584378776366;  // scaled to 1/6
        static double[] te_xi = new double[]   { 0.25, a, b, b, b, c, c, d, c, d, d, e, e, f, e };
        static double[] te_eta = new double[]  { 0.25, b, a, b, b, c, d, c, d, c, d, e, f, e, e };
        static double[] te_zeta = new double[] { 0.25, b, b, a, b, d, c, c, d, d, c, f, e, e, e };
        static double[] te_w = new double[]    { w1, w2, w2, w2, w2, w3, w3, w3, w3, w3, w3, w4, w4, w4, w4 };


        // Methods                                                                                                                  
        // Length
        static public double EdgeLength(FeNode n1, FeNode n2)
        {
            return Math.Sqrt(Math.Pow(n1.X - n2.X, 2) + Math.Pow(n1.Y - n2.Y, 2) + Math.Pow(n1.Z - n2.Z, 2));
        }
        static public double EdgeLength(FeNode n1, FeNode n2, FeNode n3)
        {
            double length = e_w[0] * EdgeJNorm(n1, n2, n3, e_xi[0]) +
                            e_w[1] * EdgeJNorm(n1, n2, n3, e_xi[1]);
            return length;
        }
        // Area
        static public double TriangleArea(FeNode n1, FeNode n2, FeNode n3)
        {
            // Heron's formula
            double a = Math.Sqrt(Math.Pow(n1.X - n2.X, 2) + Math.Pow(n1.Y - n2.Y, 2) + Math.Pow(n1.Z - n2.Z, 2));
            double b = Math.Sqrt(Math.Pow(n2.X - n3.X, 2) + Math.Pow(n2.Y - n3.Y, 2) + Math.Pow(n2.Z - n3.Z, 2));
            double c = Math.Sqrt(Math.Pow(n3.X - n1.X, 2) + Math.Pow(n3.Y - n1.Y, 2) + Math.Pow(n3.Z - n1.Z, 2));
            double s = (a + b + c) * 0.5;
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }
        static public double TriangleAreaWithInterpolation(FeNode n1, FeNode n2, FeNode n3)
        {
            return Math.Sqrt(((n2.Y * n2.Y) - 2 * n1.Y * n2.Y + (n2.X * n2.X) - 2 * n1.X * n2.X + (n1.Y * n1.Y) +
                (n1.X * n1.X)) * (n3.Z * n3.Z) + (((2 * n1.Y - 2 * n2.Y) * n2.Z + 2 * n1.Z * n2.Y -
                2 * n1.Y * n1.Z) * n3.Y + ((2 * n1.X - 2 * n2.X) * n2.Z + 2 * n1.Z * n2.X -
                2 * n1.X * n1.Z) * n3.X + (2 * n1.Y * n2.Y + 2 * n1.X * n2.X - 2 * (n1.Y * n1.Y) -
                2 * (n1.X * n1.X)) * n2.Z - 2 * n1.Z * (n2.Y * n2.Y) + 2 * n1.Y * n1.Z * n2.Y -
                2 * n1.Z * (n2.X * n2.X) + 2 * n1.X * n1.Z * n2.X) * n3.Z +
                ((n2.Z * n2.Z) - 2 * n1.Z * n2.Z + (n2.X * n2.X) - 2 * n1.X * n2.X + (n1.Z * n1.Z) +
                (n1.X * n1.X)) * (n3.Y * n3.Y) + (((2 * n1.X - 2 * n2.X) * n2.Y +
                2 * n1.Y * n2.X - 2 * n1.X * n1.Y) * n3.X - 2 * n1.Y * (n2.Z * n2.Z) +
                (2 * n1.Z * n2.Y + 2 * n1.Y * n1.Z) * n2.Z + (2 * n1.X * n2.X - 2 * (n1.Z * n1.Z) -
                2 * (n1.X * n1.X)) * n2.Y - 2 * n1.Y * (n2.X * n2.X) + 2 * n1.X * n1.Y * n2.X) * n3.Y +
                ((n2.Z * n2.Z) - 2 * n1.Z * n2.Z + (n2.Y * n2.Y) - 2 * n1.Y * n2.Y + (n1.Z * n1.Z) + (n1.Y * n1.Y)) *
                (n3.X * n3.X) + (-2 * n1.X * (n2.Z * n2.Z) + (2 * n1.Z * n2.X + 2 * n1.X * n1.Z) * n2.Z -
                2 * n1.X * (n2.Y * n2.Y) + (2 * n1.Y * n2.X + 2 * n1.X * n1.Y) * n2.Y + (-2 * (n1.Z * n1.Z) -
                2 * (n1.Y * n1.Y)) * n2.X) * n3.X + ((n1.Y * n1.Y) + (n1.X * n1.X)) * (n2.Z * n2.Z) +
                (-2 * n1.Y * n1.Z * n2.Y - 2 * n1.X * n1.Z * n2.X) * n2.Z + ((n1.Z * n1.Z) +
                (n1.X * n1.X)) * (n2.Y * n2.Y) - 2 * n1.X * n1.Y * n2.X * n2.Y + ((n1.Z * n1.Z) +
                (n1.Y * n1.Y)) * (n2.X * n2.X)) / 2;
        }
        static public double TriangleArea(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6)
        {
            double area = t_w[0] * TriangleJNorm(n1, n2, n3, n4, n5, n6, t_xi[0], t_eta[0]) +
                          t_w[1] * TriangleJNorm(n1, n2, n3, n4, n5, n6, t_xi[1], t_eta[1]) +
                          t_w[2] * TriangleJNorm(n1, n2, n3, n4, n5, n6, t_xi[2], t_eta[2]);
            return area;
        }
        static public double TriangleAreaByDivision(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6)
        {
            double area = TriangleArea(n4, n6, n1);
            area += TriangleArea(n4, n5, n6);
            area += TriangleArea(n4, n2, n5);
            area += TriangleArea(n6, n5, n3);
            return area;
        }
        static public double TriangleArea6GaussPoints(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6)
        {
            // https://doi.org/10.1002/nme.1620070316
            double[] xi = new double[] { 0.091576213509771, 0.816847572980459, 0.091576213509771,
                                         0.445948490915965, 0.108103018168070, 0.445948490915965 };
            double[] yi = new double[] { 0.091576213509771, 0.091576213509771, 0.816847572980459,
                                         0.445948490915965, 0.445948490915965, 0.108103018168070 };
            double[] w = new double[] { 0.109951743655322, 0.109951743655322, 0.109951743655322,
                                        0.223381589678011, 0.223381589678011, 0.223381589678011 };
            //
            double area = w[0] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[0], yi[0]) +
                          w[1] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[1], yi[1]) +
                          w[2] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[2], yi[2]) +
                          w[3] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[3], yi[3]) +
                          w[4] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[4], yi[4]) +
                          w[5] * TriangleJNorm(n1, n2, n3, n4, n5, n6, xi[5], yi[5]);
            return area * 0.5;
        }
        static public double RectangleArea(FeNode n1, FeNode n2, FeNode n3, FeNode n4)
        {
            double area = 0;
            area += TriangleArea(n1, n2, n3);
            area += TriangleArea(n1, n3, n4);
            return area;
        }
        static public double RectangleArea(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                           FeNode n5, FeNode n6, FeNode n7, FeNode n8)
        {
            double x;
            double y;
            double z;
            //
            x = QuadMidPoint(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X);
            y = QuadMidPoint(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y);
            z = QuadMidPoint(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z);
            FeNode n9 = new FeNode(0, x, y, z);
            //
            double area = TriangleArea(n1, n2, n3, n5, n6, n9);
            area += TriangleArea(n1, n3, n4, n9, n7, n8);
            return area;
        }
        // Volume
        public static double TetrahedronVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4)
        {
            double V =
                  ((n2.X - n1.X) * ((n3.Y - n1.Y) * (n4.Z - n1.Z) - (n3.Z - n1.Z) * (n4.Y - n1.Y)) -
                  (n3.X - n1.X) * ((n2.Y - n1.Y) * (n4.Z - n1.Z) - (n2.Z - n1.Z) * (n4.Y - n1.Y)) +
                  ((n2.Y - n1.Y) * (n3.Z - n1.Z) - (n2.Z - n1.Z) * (n3.Y - n1.Y)) * (n4.X - n1.X)) / 6;
            //
            return V;
        }
        public static double TetrahedronVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                               FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                               FeNode n9, FeNode n10)
        {
            if (Math.Abs((n1.X + n2.X) / 2 - n5.X) < eps &&
                Math.Abs((n1.Y + n2.Y) / 2 - n5.Y) < eps &&
                Math.Abs((n1.Z + n2.Z) / 2 - n5.Z) < eps &&
                Math.Abs((n2.X + n3.X) / 2 - n6.X) < eps &&
                Math.Abs((n2.Y + n3.Y) / 2 - n6.Y) < eps &&
                Math.Abs((n2.Z + n3.Z) / 2 - n6.Z) < eps &&
                Math.Abs((n3.X + n1.X) / 2 - n7.X) < eps &&
                Math.Abs((n3.Y + n1.Y) / 2 - n7.Y) < eps &&
                Math.Abs((n3.Z + n1.Z) / 2 - n7.Z) < eps &&
                Math.Abs((n1.X + n4.X) / 2 - n8.X) < eps &&
                Math.Abs((n1.Y + n4.Y) / 2 - n8.Y) < eps &&
                Math.Abs((n1.Z + n4.Z) / 2 - n8.Z) < eps &&
                Math.Abs((n2.X + n4.X) / 2 - n9.X) < eps &&
                Math.Abs((n2.Y + n4.Y) / 2 - n9.Y) < eps &&
                Math.Abs((n2.Z + n4.Z) / 2 - n9.Z) < eps &&
                Math.Abs((n3.X + n4.X) / 2 - n10.X) < eps &&
                Math.Abs((n3.Y + n4.Y) / 2 - n10.Y) < eps &&
                Math.Abs((n3.Z + n4.Z) / 2 - n10.Z) < eps)
                return TetrahedronVolume(n1, n2, n3, n4);
            //
            return
                te_w[0] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[0], te_eta[0], te_zeta[0]) +
                te_w[1] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[1], te_eta[1], te_zeta[1]) +
                te_w[2] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[2], te_eta[2], te_zeta[2]) +
                te_w[3] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[3], te_eta[3], te_zeta[3]) +
                te_w[4] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[4], te_eta[4], te_zeta[4]) +
                te_w[5] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[5], te_eta[5], te_zeta[5]) +
                te_w[6] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[6], te_eta[6], te_zeta[6]) +
                te_w[7] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[7], te_eta[7], te_zeta[7]) +
                te_w[8] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[8], te_eta[8], te_zeta[8]) +
                te_w[9] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[9], te_eta[9], te_zeta[9]) +
                te_w[10] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[10], te_eta[10], te_zeta[10]) +
                te_w[11] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[11], te_eta[11], te_zeta[11]) +
                te_w[12] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[12], te_eta[12], te_zeta[12]) +
                te_w[13] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[13], te_eta[13], te_zeta[13]) +
                te_w[14] * TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[14], te_eta[14], te_zeta[14]);
        }
        public static double PyramidVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5)
        {
            double vol = TetrahedronVolume(n1, n2, n3, n5);
            vol += TetrahedronVolume(n1, n3, n4, n5);
            return vol;
        }
        public static double PyramidVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                           FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                           FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                           FeNode n13)
        {
            FeNode[] faceNodes = GetInterpolatedMidNodesOnPyramid(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13);
            FeNode n14 = faceNodes[0];
            double vol = TetrahedronVolume(n1, n2, n3, n5, n6, n7, n14, n10, n11, n12);
            vol += TetrahedronVolume(n1, n3, n4, n5, n14, n8, n9, n10, n12, n13);
            return vol;
        }
        public static double WedgeVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                         FeNode n5, FeNode n6)
        {
            double vol = TetrahedronVolume(n2, n4, n5, n3);
            vol += TetrahedronVolume(n3, n6, n4, n5);
            vol += TetrahedronVolume(n3, n4, n1, n2);
            return vol;
        }
        public static double WedgeVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                         FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                         FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                         FeNode n13, FeNode n14, FeNode n15)
        {
            FeNode[] faceNodes = GetInterpolatedMidNodesOnWedge(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15);
            FeNode n16 = faceNodes[0];
            FeNode n17 = faceNodes[1];
            FeNode n18 = faceNodes[2];
            double vol = TetrahedronVolume(n2, n4, n5, n3, n16, n10, n14, n8, n18, n17);
            vol += TetrahedronVolume(n3, n6, n4, n5, n15, n12, n18, n17, n11, n10);
            vol += TetrahedronVolume(n3, n4, n1, n2, n18, n13, n9, n8, n16, n7);
            return vol;
        }
        public static double HexahedronVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                              FeNode n5, FeNode n6, FeNode n7, FeNode n8)
        {
            double vol = WedgeVolume(n1, n2, n3, n5, n6, n7);
            vol += WedgeVolume(n1, n3, n4, n5, n7, n8);
            return vol;
        }
        public static double HexahedronVolume(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                              FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                              FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                              FeNode n13, FeNode n14, FeNode n15, FeNode n16,
                                              FeNode n17, FeNode n18, FeNode n19, FeNode n20)
        {
            FeNode[] faceNodes = GetInterpolatedMidNodesOnHexahedron(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10,
                                                                  n11, n12, n13, n14, n15, n16, n17, n18, n19, n20);
            FeNode n21 = faceNodes[0];
            FeNode n22 = faceNodes[1];
            double vol = WedgeVolume(n1, n2, n3, n5, n6, n7, n9, n10, n21, n13, n14, n22, n17, n18, n19);
            vol += WedgeVolume(n1, n3, n4, n5, n7, n8, n21, n11, n12, n22, n15, n16, n17, n19, n20);
            return vol;
        }
        // Center of mass
        static public double[] EdgeCG(FeNode n1, FeNode n2, out double length)
        {
            length = EdgeLength(n1, n2);
            //
            double[] cg = new double[3];
            cg[0] = (n1.X + n2.X) / 2;
            cg[1] = (n1.Y + n2.Y) / 2;
            cg[2] = (n1.Z + n2.Z) / 2;
            return cg;
        }
        static public double[] EdgeCG(FeNode n1, FeNode n2, FeNode n3, out double length)
        {
            double[] cg = new double[3];
            double[] cg1;
            double l;
            //
            cg1 = EdgeCG(n1, n3, out l);
            cg[0] += cg1[0] * l;
            cg[1] += cg1[1] * l;
            cg[2] += cg1[2] * l;
            length = l;
            //
            cg1 = EdgeCG(n3, n2, out l);
            cg[0] += cg1[0] * l;
            cg[1] += cg1[1] * l;
            cg[2] += cg1[2] * l;
            length += l;
            //
            cg[0] /= length;
            cg[1] /= length;
            cg[2] /= length;
            //
            return cg;
        }
        //
        static public double[] TriangleCG(FeNode n1, FeNode n2, FeNode n3, out double area)
        {
            area = TriangleArea(n1, n2, n3);
            //
            double[] cg = new double[3];
            cg[0] = (n1.X + n2.X + n3.X) / 3;
            cg[1] = (n1.Y + n2.Y + n3.Y) / 3;
            cg[2] = (n1.Z + n2.Z + n3.Z) / 3;
            return cg;
        }
        static public double[] TriangleCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6,
                                          out double area)
        {
            double[] cg = new double[3];
            double[] cg1;
            double a;
            //
            cg1 = TriangleCG(n4, n6, n1, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area = a;
            //
            cg1 = TriangleCG(n4, n5, n6, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg1 = TriangleCG(n4, n2, n5, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg1 = TriangleCG(n6, n5, n3, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg[0] /= area;
            cg[1] /= area;
            cg[2] /= area;
            //
            return cg;
        }
        static public double[] RectangleCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4, out double area)
        {
            double[] cg = new double[3];
            double[] cg1;
            double a;
            //
            cg1 = TriangleCG(n1, n2, n3, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area = a;
            //
            cg1 = TriangleCG(n1, n3, n4, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg[0] /= area;
            cg[1] /= area;
            cg[2] /= area;
            //
            return cg;
        }
        static public double[] RectangleCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6,
                                           FeNode n7, FeNode n8, out double area)
        {
            double[] cg = new double[3];
            double[] cg1;
            double a;
            //
            cg1 = TriangleCG(n8, n1, n5, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area = a;
            //
            cg1 = TriangleCG(n8, n5, n7, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg1 = TriangleCG(n8, n7, n4, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            //
            cg1 = TriangleCG(n6, n3, n7, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg1 = TriangleCG(n6, n7, n5, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg1 = TriangleCG(n6, n5, n2, out a);
            cg[0] += cg1[0] * a;
            cg[1] += cg1[1] * a;
            cg[2] += cg1[2] * a;
            area += a;
            //
            cg[0] /= area;
            cg[1] /= area;
            cg[2] /= area;
            //
            return cg;
        }
        //
        public static double[] TetrahedronCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4, out double volume)
        {
            volume = TetrahedronVolume(n1, n2, n3, n4);
            //
            double x = (n1.X + n2.X + n3.X + n4.X) * 0.25;
            double y = (n1.Y + n2.Y + n3.Y + n4.Y) * 0.25;
            double z = (n1.Z + n2.Z + n3.Z + n4.Z) * 0.25;
            //
            return new double[] { x, y, z };
        }
        public static double[] TetrahedronCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                             FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                             FeNode n9, FeNode n10, out double volume)
        {
            if (Math.Abs((n1.X + n2.X) / 2 - n5.X) < eps &&
                Math.Abs((n1.Y + n2.Y) / 2 - n5.Y) < eps &&
                Math.Abs((n1.Z + n2.Z) / 2 - n5.Z) < eps &&
                Math.Abs((n2.X + n3.X) / 2 - n6.X) < eps &&
                Math.Abs((n2.Y + n3.Y) / 2 - n6.Y) < eps &&
                Math.Abs((n2.Z + n3.Z) / 2 - n6.Z) < eps &&
                Math.Abs((n3.X + n1.X) / 2 - n7.X) < eps &&
                Math.Abs((n3.Y + n1.Y) / 2 - n7.Y) < eps &&
                Math.Abs((n3.Z + n1.Z) / 2 - n7.Z) < eps &&
                Math.Abs((n1.X + n4.X) / 2 - n8.X) < eps &&
                Math.Abs((n1.Y + n4.Y) / 2 - n8.Y) < eps &&
                Math.Abs((n1.Z + n4.Z) / 2 - n8.Z) < eps &&
                Math.Abs((n2.X + n4.X) / 2 - n9.X) < eps &&
                Math.Abs((n2.Y + n4.Y) / 2 - n9.Y) < eps &&
                Math.Abs((n2.Z + n4.Z) / 2 - n9.Z) < eps &&
                Math.Abs((n3.X + n4.X) / 2 - n10.X) < eps &&
                Math.Abs((n3.Y + n4.Y) / 2 - n10.Y) < eps &&
                Math.Abs((n3.Z + n4.Z) / 2 - n10.Z) < eps)
            {
                return TetrahedronCG(n1, n2, n3, n4, out volume);
            }
            //
            double JNorm0 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[0], te_eta[0], te_zeta[0]);
            double JNorm1 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[1], te_eta[1], te_zeta[1]);
            double JNorm2 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[2], te_eta[2], te_zeta[2]);
            double JNorm3 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[3], te_eta[3], te_zeta[3]);
            double JNorm4 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[4], te_eta[4], te_zeta[4]);
            double JNorm5 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[5], te_eta[5], te_zeta[5]);
            double JNorm6 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[6], te_eta[6], te_zeta[6]);
            double JNorm7 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[7], te_eta[7], te_zeta[7]);
            double JNorm8 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[8], te_eta[8], te_zeta[8]);
            double JNorm9 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[9], te_eta[9], te_zeta[9]);
            double JNorm10 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[10], te_eta[10], te_zeta[10]);
            double JNorm11 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[11], te_eta[11], te_zeta[11]);
            double JNorm12 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[12], te_eta[12], te_zeta[12]);
            double JNorm13 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[13], te_eta[13], te_zeta[13]);
            double JNorm14 = TetrahedronJNorm(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, te_xi[14], te_eta[14], te_zeta[14]);
            //
            double x0 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[0], te_eta[0], te_zeta[0]);
            double x1 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[1], te_eta[1], te_zeta[1]);
            double x2 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[2], te_eta[2], te_zeta[2]);
            double x3 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[3], te_eta[3], te_zeta[3]);
            double x4 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[4], te_eta[4], te_zeta[4]);
            double x5 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[5], te_eta[5], te_zeta[5]);
            double x6 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[6], te_eta[6], te_zeta[6]);
            double x7 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[7], te_eta[7], te_zeta[7]);
            double x8 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[8], te_eta[8], te_zeta[8]);
            double x9 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[9], te_eta[9], te_zeta[9]);
            double x10 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[10], te_eta[10], te_zeta[10]);
            double x11 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[11], te_eta[11], te_zeta[11]);
            double x12 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[12], te_eta[12], te_zeta[12]);
            double x13 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[13], te_eta[13], te_zeta[13]);
            double x14 = InterpolateInTetrahedron(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, n7.X, n8.X, n9.X, n10.X, te_xi[14], te_eta[14], te_zeta[14]);
            //
            double y0 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[0], te_eta[0], te_zeta[0]);
            double y1 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[1], te_eta[1], te_zeta[1]);
            double y2 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[2], te_eta[2], te_zeta[2]);
            double y3 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[3], te_eta[3], te_zeta[3]);
            double y4 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[4], te_eta[4], te_zeta[4]);
            double y5 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[5], te_eta[5], te_zeta[5]);
            double y6 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[6], te_eta[6], te_zeta[6]);
            double y7 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[7], te_eta[7], te_zeta[7]);
            double y8 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[8], te_eta[8], te_zeta[8]);
            double y9 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[9], te_eta[9], te_zeta[9]);
            double y10 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[10], te_eta[10], te_zeta[10]);
            double y11 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[11], te_eta[11], te_zeta[11]);
            double y12 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[12], te_eta[12], te_zeta[12]);
            double y13 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[13], te_eta[13], te_zeta[13]);
            double y14 = InterpolateInTetrahedron(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, n7.Y, n8.Y, n9.Y, n10.Y, te_xi[14], te_eta[14], te_zeta[14]);
            //
            double z0 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[0], te_eta[0], te_zeta[0]);
            double z1 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[1], te_eta[1], te_zeta[1]);
            double z2 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[2], te_eta[2], te_zeta[2]);
            double z3 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[3], te_eta[3], te_zeta[3]);
            double z4 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[4], te_eta[4], te_zeta[4]);
            double z5 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[5], te_eta[5], te_zeta[5]);
            double z6 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[6], te_eta[6], te_zeta[6]);
            double z7 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[7], te_eta[7], te_zeta[7]);
            double z8 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[8], te_eta[8], te_zeta[8]);
            double z9 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[9], te_eta[9], te_zeta[9]);
            double z10 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[10], te_eta[10], te_zeta[10]);
            double z11 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[11], te_eta[11], te_zeta[11]);
            double z12 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[12], te_eta[12], te_zeta[12]);
            double z13 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[13], te_eta[13], te_zeta[13]);
            double z14 = InterpolateInTetrahedron(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, n7.Z, n8.Z, n9.Z, n10.Z, te_xi[14], te_eta[14], te_zeta[14]);
            //
            volume = te_w[0] * JNorm0 + te_w[1] * JNorm1 + te_w[2] * JNorm2 + te_w[3] * JNorm3 + te_w[4] * JNorm4 +
                     te_w[5] * JNorm5 + te_w[6] * JNorm6 + te_w[7] * JNorm7 + te_w[8] * JNorm8 + te_w[9] * JNorm9 +
                     te_w[10] * JNorm10 + te_w[11] * JNorm11 + te_w[12] * JNorm12 + te_w[13] * JNorm13 + te_w[14] * JNorm14;
            double x = te_w[0] * x0 * JNorm0 + te_w[1] * x1 * JNorm1 + te_w[2] * x2 * JNorm2 + te_w[3] * x3 * JNorm3 + te_w[4] * x4 * JNorm4 +
                       te_w[5] * x5 * JNorm5 + te_w[6] * x6 * JNorm6 + te_w[7] * x7 * JNorm7 + te_w[8] * x8 * JNorm8 + te_w[9] * x9 * JNorm9 +
                       te_w[10] * x10 * JNorm10 + te_w[11] * x11 * JNorm11 + te_w[12] * x12 * JNorm12 + te_w[13] * x13 * JNorm13 +
                       te_w[14] * x14 * JNorm14;
            double y = te_w[0] * y0 * JNorm0 + te_w[1] * y1 * JNorm1 + te_w[2] * y2 * JNorm2 + te_w[3] * y3 * JNorm3 + te_w[4] * y4 * JNorm4 +
                       te_w[5] * y5 * JNorm5 + te_w[6] * y6 * JNorm6 + te_w[7] * y7 * JNorm7 + te_w[8] * y8 * JNorm8 + te_w[9] * y9 * JNorm9 +
                       te_w[10] * y10 * JNorm10 + te_w[11] * y11 * JNorm11 + te_w[12] * y12 * JNorm12 + te_w[13] * y13 * JNorm13 +
                       te_w[14] * y14 * JNorm14;
            double z = te_w[0] * z0 * JNorm0 + te_w[1] * z1 * JNorm1 + te_w[2] * z2 * JNorm2 + te_w[3] * z3 * JNorm3 + te_w[4] * z4 * JNorm4 +
                       te_w[5] * z5 * JNorm5 + te_w[6] * z6 * JNorm6 + te_w[7] * z7 * JNorm7 + te_w[8] * z8 * JNorm8 + te_w[9] * z9 * JNorm9 +
                       te_w[10] * z10 * JNorm10 + te_w[11] * z11 * JNorm11 + te_w[12] * z12 * JNorm12 + te_w[13] * z13 * JNorm13 +
                       te_w[14] * z14 * JNorm14;
            //
            double vInv = 1.0 / volume;
            return new double[] { vInv * x, vInv * y, vInv * z };
        }
        public static double[] PyramidCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, out double volume)
        {
            double v1;
            double v2;
            double[] cg1 = TetrahedronCG(n1, n2, n3, n5, out v1);
            double[] cg2 = TetrahedronCG(n1, n3, n4, n5, out v2);
            volume = v1 + v2;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2) * vInv;
            //
            return cg1;
        }
        public static double[] PyramidCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                         FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                         FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                         FeNode n13, out double volume)
        {
            double v1;
            double v2;
            FeNode[] faceNodes = GetInterpolatedMidNodesOnPyramid(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13);
            FeNode n14 = faceNodes[0];
            double[] cg1 = TetrahedronCG(n1, n2, n3, n5, n6, n7, n14, n10, n11, n12, out v1);
            double[] cg2 = TetrahedronCG(n1, n3, n4, n5, n14, n8, n9, n10, n12, n13, out v2);
            volume = v1 + v2;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2) * vInv;
            //
            return cg1;
        }
        public static double[] WedgeCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                       FeNode n5, FeNode n6, out double volume)
        {
            double v1;
            double v2;
            double v3;
            double[] cg1 = TetrahedronCG(n2, n4, n5, n3, out v1);
            double[] cg2 = TetrahedronCG(n3, n6, n4, n5, out v2);
            double[] cg3 = TetrahedronCG(n3, n4, n1, n2, out v3);
            volume = v1 + v2 + v3;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2 + cg3[0] * v3) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2 + cg3[1] * v3) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2 + cg3[2] * v3) * vInv;
            //
            return cg1;
        }
        public static double[] WedgeCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                       FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                       FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                       FeNode n13, FeNode n14, FeNode n15, out double volume)
        {
            double v1;
            double v2;
            double v3;
            FeNode[] faceNodes = GetInterpolatedMidNodesOnWedge(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10, n11, n12, n13, n14, n15);
            FeNode n16 = faceNodes[0];
            FeNode n17 = faceNodes[1];
            FeNode n18 = faceNodes[2];
            double[] cg1 = TetrahedronCG(n2, n4, n5, n3, n16, n10, n14, n8, n18, n17, out v1);
            double[] cg2 = TetrahedronCG(n3, n6, n4, n5, n15, n12, n18, n17, n11, n10, out v2);
            double[] cg3 = TetrahedronCG(n3, n4, n1, n2, n18, n13, n9, n8, n16, n7, out v3);
            volume = v1 + v2 + v3;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2 + cg3[0] * v3) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2 + cg3[1] * v3) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2 + cg3[2] * v3) * vInv;
            //
            return cg1;
        }
        public static double[] HexahedronCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                            FeNode n5, FeNode n6, FeNode n7, FeNode n8, out double volume)
        {
            double v1;
            double v2;
            double[] cg1 = WedgeCG(n1, n2, n3, n5, n6, n7, out v1);
            double[] cg2 = WedgeCG(n1, n3, n4, n5, n7, n8, out v2);
            volume = v1 + v2;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2) * vInv;
            //
            return cg1;
        }
        public static double[] HexahedronCG(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                            FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                            FeNode n9, FeNode n10, FeNode n11, FeNode n12,
                                            FeNode n13, FeNode n14, FeNode n15, FeNode n16,
                                            FeNode n17, FeNode n18, FeNode n19, FeNode n20, out double volume)
        {
            double v1;
            double v2;
            FeNode[] faceNodes = GetInterpolatedMidNodesOnHexahedron(n1, n2, n3, n4, n5, n6, n7, n8, n9, n10,
                                                                     n11, n12, n13, n14, n15, n16, n17, n18, n19, n20);
            FeNode n21 = faceNodes[0];
            FeNode n22 = faceNodes[1];
            double[] cg1 = WedgeCG(n1, n2, n3, n5, n6, n7, n9, n10, n21, n13, n14, n22, n17, n18, n19, out v1);
            double[] cg2 = WedgeCG(n1, n3, n4, n5, n7, n8, n21, n11, n12, n22, n15, n16, n17, n19, n20, out v2);
            volume = v1 + v2;
            double vInv = 1.0 / volume;
            //
            cg1[0] = (cg1[0] * v1 + cg2[0] * v2) * vInv;
            cg1[1] = (cg1[1] * v1 + cg2[1] * v2) * vInv;
            cg1[2] = (cg1[2] * v1 + cg2[2] * v2) * vInv;
            //
            return cg1;
        }
        // Mid-nodes
        private static FeNode[] GetInterpolatedMidNodesOnPyramid(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                                                 FeNode n6, FeNode n7, FeNode n8, FeNode n9, FeNode n10,
                                                                 FeNode n11, FeNode n12, FeNode n13)
        {
            double x;
            double y;
            double z;
            FeNode[] faceNodes = new FeNode[1];
            //
            x = QuadMidPoint(n1.X, n2.X, n3.X, n4.X, n6.X, n7.X, n8.X, n9.X);
            y = QuadMidPoint(n1.Y, n2.Y, n3.Y, n4.Y, n6.Y, n7.Y, n8.Y, n9.Y);
            z = QuadMidPoint(n1.Z, n2.Z, n3.Z, n4.Z, n6.Z, n7.Z, n8.Z, n9.Z);
            faceNodes[0] = new FeNode(0, x, y, z);
            //
            return faceNodes;
        }
        private static FeNode[] GetInterpolatedMidNodesOnWedge(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                                               FeNode n6, FeNode n7, FeNode n8, FeNode n9, FeNode n10,
                                                               FeNode n11, FeNode n12, FeNode n13, FeNode n14, FeNode n15)
        {
            double x;
            double y;
            double z;
            FeNode[] faceNodes = new FeNode[3];
            //
            x = QuadMidPoint(n1.X, n2.X, n5.X, n4.X, n7.X, n14.X, n10.X, n13.X);
            y = QuadMidPoint(n1.Y, n2.Y, n5.Y, n4.Y, n7.Y, n14.Y, n10.Y, n13.Y);
            z = QuadMidPoint(n1.Z, n2.Z, n5.Z, n4.Z, n7.Z, n14.Z, n10.Z, n13.Z);
            faceNodes[0] = new FeNode(0, x, y, z);
            //
            x = QuadMidPoint(n2.X, n3.X, n6.X, n5.X, n8.X, n15.X, n11.X, n14.X);
            y = QuadMidPoint(n2.Y, n3.Y, n6.Y, n5.Y, n8.Y, n15.Y, n11.Y, n14.Y);
            z = QuadMidPoint(n2.Z, n3.Z, n6.Z, n5.Z, n8.Z, n15.Z, n11.Z, n14.Z);
            faceNodes[1] = new FeNode(0, x, y, z);
            //
            x = QuadMidPoint(n1.X, n3.X, n6.X, n4.X, n9.X, n15.X, n12.X, n13.X);
            y = QuadMidPoint(n1.Y, n3.Y, n6.Y, n4.Y, n9.Y, n15.Y, n12.Y, n13.Y);
            z = QuadMidPoint(n1.Z, n3.Z, n6.Z, n4.Z, n9.Z, n15.Z, n12.Z, n13.Z);
            faceNodes[2] = new FeNode(0, x, y, z);
            //
            return faceNodes;
        }
        private static FeNode[] GetInterpolatedMidNodesOnHexahedron(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5,
                                                                    FeNode n6, FeNode n7, FeNode n8, FeNode n9, FeNode n10,
                                                                    FeNode n11, FeNode n12, FeNode n13, FeNode n14, FeNode n15,
                                                                    FeNode n16, FeNode n17, FeNode n18, FeNode n19, FeNode n20)
        {
            double x;
            double y;
            double z;
            FeNode[] faceNodes = new FeNode[2];
            //
            x = QuadMidPoint(n1.X, n2.X, n3.X, n4.X, n9.X, n10.X, n11.X, n12.X);
            y = QuadMidPoint(n1.Y, n2.Y, n3.Y, n4.Y, n9.Y, n10.Y, n11.Y, n12.Y);
            z = QuadMidPoint(n1.Z, n2.Z, n3.Z, n4.Z, n9.Z, n10.Z, n11.Z, n12.Z);
            faceNodes[0] = new FeNode(0, x, y, z);
            //
            x = QuadMidPoint(n5.X, n6.X, n7.X, n8.X, n13.X, n14.X, n15.X, n16.X);
            y = QuadMidPoint(n5.Y, n6.Y, n7.Y, n8.Y, n13.Y, n14.Y, n15.Y, n16.Y);
            z = QuadMidPoint(n5.Z, n6.Z, n7.Z, n8.Z, n13.Z, n14.Z, n15.Z, n16.Z);
            faceNodes[1] = new FeNode(0, x, y, z);
            //
            return faceNodes;
        }
        private static double QuadMidPoint(double u1, double u2, double u3, double u4, double u5, double u6, double u7, double u8)
        {
            return -0.25 * u1 - 0.25 * u2 - 0.25 * u3 - 0.25 * u4 + 0.5 * u5 + 0.5 * u6 + 0.5 * u7 + 0.5 * u8;
        }
        private static double InterpolateInQuad(double u1, double u2, double u3, double u4,
                                                double u5, double u6, double u7, double u8,
                                                double g, double h)
        {
            return -0.25 * (1 - g) * (1 - h) * (1 + g + h) * u1 -
                0.25 * (1 + g) * (1 - h) * (1 - g + h) * u2 -
                0.25 * (1 + g) * (1 + h) * (1 - g - h) * u3 -
                0.25 * (1 - g) * (1 + h) * (1 + g - h) * u4 +
                0.5 * (1 - g) * (1 + g) * (1 - h) * u5 +
                0.5 * (1 - h) * (1 + h) * (1 + g) * u6 +
                0.5 * (1 - g) * (1 + g) * (1 + h) * u7 +
                0.5 * (1 - h) * (1 + h) * (1 - g) * u8;
        }
        private static double InterpolateInWedge(double u1, double u2, double u3, double u4, double u5,
                                                 double u6, double u7, double u8, double u9, double u10,
                                                 double u11, double u12, double u13, double u14, double u15,
                                                 double g, double h, double i)
        {
            // Abaqus Theory Guide/Elements/Continuum elements/Triangular, tetrahedral and wedge elements
            return 0.5 * ((1 - g - h) * (2 * (1 - g - h) - 1) * (1 - i) - (1 - g - h) * (1 - i * i)) * u1 +
                0.5 * (g * (2 * g - 1) * (1 - i) - g * (1 - i * i)) * u2 +
                0.5 * (h * (2 * h - 1) * (1 - i) - h * (1 - i * i)) * u3 +
                0.5 * ((1 - g - h) * (2 * (1 - g - h) - 1) * (1 + i) - (1 - g - h) * (1 - i * i)) * u4 +
                0.5 * (g * (2 * g - 1) * (1 + i) - g * (1 - i * i)) * u5 +
                0.5 * (h * (2 * h - 1) * (1 + i) - h * (1 - i * i)) * u6 +
                2 * (1 - g - h) * g * (1 - i) * u7 +
                2 * g * h * (1 - i) * u8 +
                2 * h * (1 - g - h) * (1 - i) * u9 +
                2 * (1 - g - h) * g * (1 + i) * u10 +
                2 * g * h * (1 + i) * u11 +
                2 * h * (1 - g - h) * (1 + i) * u12 +
                (1 - g - h) * (1 - i * i) * u13 +
                g * (1 - i * i) * u14 +
                h * (1 - i * i) * u15;
        }
        private static double InterpolateInTetrahedron(double u1, double u2, double u3, double u4, double u5,
                                                       double u6, double u7, double u8, double u9, double u10,
                                                       double g, double h, double i)
        {
            // Abaqus Theory Guide/Elements/Continuum elements/Triangular, tetrahedral and wedge elements
            double ghi_1 = 1 - g - h - i;
            double ghi_1x4 = 4 * ghi_1;
            return (2 * ghi_1 - 1) * ghi_1 * u1 +
                (2 * g - 1) * g * u2 +
                (2 * h - 1) * h * u3 +
                (2 * i - 1) * i * u4 +
                ghi_1x4 * g * u5 +
                4 * g * h * u6 +
                ghi_1x4 * h * u7 +
                ghi_1x4 * i * u8 +
                4 * g * i * u9 +
                4 * h * i * u10;
        }
        private static double InterpolateInHexahedron(double u1, double u2, double u3, double u4, double u5,
                                                      double u6, double u7, double u8, double u9, double u10,
                                                      double u11, double u12, double u13, double u14, double u15,
                                                      double u16, double u17, double u18, double u19, double u20,
                                                      double g, double h, double r)
        {
            // Abaqus Theory Guide/Elements/Continuum elements/Solid isoparametric quadrilaterals and hexahedra
            return -0.125 * (1 - g) * (1 - h) * (1 - r) * (2 + g + h + r) * u1 -
                0.125 * (1 + g) * (1 - h) * (1 - r) * (2 - g + h + r) * u2 -
                0.125 * (1 + g) * (1 + h) * (1 - r) * (2 - g - h + r) * u3 -
                0.125 * (1 - g) * (1 + h) * (1 - r) * (2 + g - h + r) * u4 -
                0.125 * (1 - g) * (1 - h) * (1 + r) * (2 + g + h - r) * u5 -
                0.125 * (1 + g) * (1 - h) * (1 + r) * (2 - g + h - r) * u6 -
                0.125 * (1 + g) * (1 + h) * (1 + r) * (2 - g - h - r) * u7 -
                0.125 * (1 - g) * (1 + h) * (1 + r) * (2 + g - h - r) * u8 +
                0.25 * (1 - g) * (1 + g) * (1 - h) * (1 - r) * u9 +
                0.25 * (1 - h) * (1 + h) * (1 - g) * (1 - r) * u10 +
                0.25 * (1 - g) * (1 + g) * (1 + h) * (1 - r) * u11 +
                0.25 * (1 - h) * (1 + h) * (1 + g) * (1 - r) * u12 +
                0.25 * (1 - g) * (1 + g) * (1 - h) * (1 + r) * u13 +
                0.25 * (1 - h) * (1 + h) * (1 - g) * (1 + r) * u14 +
                0.25 * (1 - g) * (1 + g) * (1 + h) * (1 + r) * u15 +
                0.25 * (1 - h) * (1 + h) * (1 + g) * (1 + r) * u16 +
                0.25 * (1 - r) * (1 + r) * (1 - g) * (1 - h) * u17 +
                0.25 * (1 - r) * (1 + r) * (1 + g) * (1 - h) * u18 +
                0.25 * (1 - r) * (1 + r) * (1 + g) * (1 + h) * u19 +
                0.25 * (1 - r) * (1 + r) * (1 - g) * (1 + h) * u20;
        }
        // Gauss
        static public double EdgeJNorm(FeNode n1, FeNode n2, FeNode n3, double g)
        {
            double a = -2 * g * n3.Z + (g + 1) * n2.Z / 2 + g * n2.Z / 2 + g * n1.Z / 2 + (g - 1) * n1.Z / 2;
            double b = -2 * g * n3.Y + (g + 1) * n2.Y / 2 + g * n2.Y / 2 + g * n1.Y / 2 + (g - 1) * n1.Y / 2;
            double c = -2 * g * n3.X + (g + 1) * n2.X / 2 + g * n2.X / 2 + g * n1.X / 2 + (g - 1) * n1.X / 2;
            return Math.Sqrt(a * a + b * b + c * c);
        }
        static public double TriangleJNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4, FeNode n5, FeNode n6,
                                           double g, double h)
        {
            double a = (-4 * h * n6.Y + 4 * h * n5.Y + 4 * (-h - g + 1) * n4.Y - 4 * g * n4.Y + (2 * g - 1) * n2.Y +
                2 * g * n2.Y - 2 * (-h - g + 1) * n1.Y - (-2 * h - 2 * g + 1) * n1.Y) * (-4 * h * n6.Z +
                4 * (-h - g + 1) * n6.Z + 4 * g * n5.Z - 4 * g * n4.Z + (2 * h - 1) * n3.Z + 2 * h * n3.Z -
                2 * (-h - g + 1) * n1.Z - (-2 * h - 2 * g + 1) * n1.Z) - (-4 * h * n6.Y + 4 * (-h - g + 1) * n6.Y +
                4 * g * n5.Y - 4 * g * n4.Y + (2 * h - 1) * n3.Y + 2 * h * n3.Y - 2 * (-h - g + 1) * n1.Y -
                (-2 * h - 2 * g + 1) * n1.Y) * (-4 * h * n6.Z + 4 * h * n5.Z + 4 * (-h - g + 1) * n4.Z - 4 * g * n4.Z +
                (2 * g - 1) * n2.Z + 2 * g * n2.Z - 2 * (-h - g + 1) * n1.Z - (-2 * h - 2 * g + 1) * n1.Z);
            double b = (-4 * h * n6.X + 4 * (-h - g + 1) * n6.X + 4 * g * n5.X - 4 * g * n4.X + (2 * h - 1) * n3.X +
                2 * h * n3.X - 2 * (-h - g + 1) * n1.X - (-2 * h - 2 * g + 1) * n1.X) * (-4 * h * n6.Z + 4 * h * n5.Z +
                4 * (-h - g + 1) * n4.Z - 4 * g * n4.Z + (2 * g - 1) * n2.Z + 2 * g * n2.Z - 2 * (-h - g + 1) * n1.Z -
                (-2 * h - 2 * g + 1) * n1.Z) - (-4 * h * n6.X + 4 * h * n5.X + 4 * (-h - g + 1) * n4.X -
                4 * g * n4.X + (2 * g - 1) * n2.X + 2 * g * n2.X - 2 * (-h - g + 1) * n1.X -
                (-2 * h - 2 * g + 1) * n1.X) * (-4 * h * n6.Z + 4 * (-h - g + 1) * n6.Z + 4 * g * n5.Z - 4 * g *
                n4.Z + (2 * h - 1) * n3.Z + 2 * h * n3.Z - 2 * (-h - g + 1) * n1.Z - (-2 * h - 2 * g + 1) * n1.Z);
            double c = (-4 * h * n6.X + 4 * h * n5.X + 4 * (-h - g + 1) * n4.X - 4 * g * n4.X + (2 * g - 1) * n2.X +
                2 * g * n2.X - 2 * (-h - g + 1) * n1.X - (-2 * h - 2 * g + 1) * n1.X) * (-4 * h * n6.Y +
                4 * (-h - g + 1) * n6.Y + 4 * g * n5.Y - 4 * g * n4.Y + (2 * h - 1) * n3.Y + 2 * h * n3.Y -
                2 * (-h - g + 1) * n1.Y - (-2 * h - 2 * g + 1) * n1.Y) - (-4 * h * n6.X + 4 * (-h - g + 1) * n6.X +
                4 * g * n5.X - 4 * g * n4.X + (2 * h - 1) * n3.X + 2 * h * n3.X - 2 * (-h - g + 1) * n1.X -
                (-2 * h - 2 * g + 1) * n1.X) * (-4 * h * n6.Y + 4 * h * n5.Y + 4 * (-h - g + 1) * n4.Y - 4 * g *
                n4.Y + (2 * g - 1) * n2.Y + 2 * g * n2.Y - 2 * (-h - g + 1) * n1.Y - (-2 * h - 2 * g + 1) * n1.Y);
            return Math.Sqrt(a * a + b * b + c * c);
            //double J11 = IntFnByG(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, g, h);
            //double J12 = IntFnByH(n1.X, n2.X, n3.X, n4.X, n5.X, n6.X, g, h);
            //double J21 = IntFnByG(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, g, h);
            //double J22 = IntFnByH(n1.Y, n2.Y, n3.Y, n4.Y, n5.Y, n6.Y, g, h);
            //double J31 = IntFnByG(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, g, h);
            //double J32 = IntFnByH(n1.Z, n2.Z, n3.Z, n4.Z, n5.Z, n6.Z, g, h);
            //double[] Jcross = new double[] { J21 * J32 - J31 * J22, J31 * J12 - J11 * J32, J11 * J22 - J21 * J12 };
            //double Jnorm = Math.Sqrt(Jcross[0] * Jcross[0] + Jcross[1] * Jcross[1] + Jcross[2] * Jcross[2]);
        }
        static public double TetrahedronJNorm(FeNode n1, FeNode n2, FeNode n3, FeNode n4,
                                              FeNode n5, FeNode n6, FeNode n7, FeNode n8,
                                              FeNode n9, FeNode n10, double g, double h, double i)
        {
            double gx2 = g * 2;
            double gx2_1 = gx2 - 1;
            double gx4 = g * 4;
            double hx2 = h * 2;
            double hx2_1 = hx2 - 1;
            double hx4 = h * 4;
            double ix2 = i * 2;
            double ix2_1 = ix2 - 1;
            double ix4 = i * 4;
            double ihg1 = -i - h - g + 1;
            double ihg1x2 = ihg1 * 2;
            double ihg1x2_1 = ihg1x2 - 1;
            double ihg1x4 = ihg1 * 4;
            return
                -(-ix4 * n8.X + ihg1x4 * n7.X - hx4 * n7.X + gx4 * n6.X - gx4 * n5.X +
                hx2_1 * n3.X + hx2 * n3.X + ix4 * n10.X - ihg1x2 * n1.X - ihg1x2_1 * n1.X)
                *
                ((ix4 * n9.Y - ix4 * n8.Y - hx4 * n7.Y + hx4 * n6.Y + ihg1x4 * n5.Y - gx4 * n5.Y +
                gx2_1 * n2.Y + gx2 * n2.Y - ihg1x2 * n1.Y - 
                ihg1x2_1 * n1.Y) * (gx4 * n9.Z - ix4 * n8.Z + ihg1x4 * n8.Z -
                hx4 * n7.Z - gx4 * n5.Z + ix2_1 * n4.Z + ix2 * n4.Z + hx4 * n10.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z) - (gx4 * n9.Y - ix4 * n8.Y + ihg1x4 * n8.Y -
                hx4 * n7.Y - gx4 * n5.Y + ix2_1 * n4.Y + ix2 * n4.Y + hx4 * n10.Y - ihg1x2 * n1.Y -
                ihg1x2_1 * n1.Y) * (ix4 * n9.Z - ix4 * n8.Z - hx4 * n7.Z + hx4 * n6.Z +
                ihg1x4 * n5.Z - gx4 * n5.Z + gx2_1 * n2.Z + gx2 * n2.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z))
                +
                (gx4 * n9.X - ix4 * n8.X + ihg1x4 * n8.X - hx4 * n7.X - gx4 * n5.X +
                ix2_1 * n4.X + ix2 * n4.X + hx4 * n10.X - ihg1x2 * n1.X - ihg1x2_1 * n1.X)
                *
                ((-ix4 * n8.Z + ihg1x4 * n7.Z - hx4 * n7.Z + gx4 * n6.Z - gx4 * n5.Z +
                hx2_1 * n3.Z + hx2 * n3.Z + ix4 * n10.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z) * (ix4 * n9.Y - ix4 * n8.Y - hx4 * n7.Y + hx4 * n6.Y +
                ihg1x4 * n5.Y - gx4 * n5.Y + gx2_1 * n2.Y + gx2 * n2.Y - ihg1x2 * n1.Y -
                ihg1x2_1 * n1.Y) - (-ix4 * n8.Y + ihg1x4 * n7.Y - hx4 * n7.Y +
                gx4 * n6.Y - gx4 * n5.Y + hx2_1 * n3.Y + hx2 * n3.Y + ix4 * n10.Y - ihg1x2 * n1.Y -
                ihg1x2_1 * n1.Y) * (ix4 * n9.Z - ix4 * n8.Z - hx4 * n7.Z + hx4 * n6.Z +
                ihg1x4 * n5.Z - gx4 * n5.Z + gx2_1 * n2.Z + gx2 * n2.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z))
                +
                (ix4 * n9.X - ix4 * n8.X - hx4 * n7.X + hx4 * n6.X + ihg1x4 * n5.X -
                gx4 * n5.X + gx2_1 * n2.X + gx2 * n2.X - ihg1x2 * n1.X - ihg1x2_1 * n1.X)
                *
                ((-ix4 * n8.Y + ihg1x4 * n7.Y - hx4 * n7.Y + gx4 * n6.Y - gx4 * n5.Y +
                hx2_1 * n3.Y + hx2 * n3.Y + ix4 * n10.Y - ihg1x2 * n1.Y -
                ihg1x2_1 * n1.Y) * (gx4 * n9.Z - ix4 * n8.Z + ihg1x4 * n8.Z -
                hx4 * n7.Z - gx4 * n5.Z + ix2_1 * n4.Z + ix2 * n4.Z + hx4 * n10.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z) - (-ix4 * n8.Z + ihg1x4 * n7.Z - hx4 * n7.Z +
                gx4 * n6.Z - gx4 * n5.Z + hx2_1 * n3.Z + hx2 * n3.Z + ix4 * n10.Z - ihg1x2 * n1.Z -
                ihg1x2_1 * n1.Z) * (gx4 * n9.Y - ix4 * n8.Y + ihg1x4 * n8.Y -
                hx4 * n7.Y - gx4 * n5.Y + ix2_1 * n4.Y + ix2 * n4.Y + hx4 * n10.Y - ihg1x2 * n1.Y -
                ihg1x2_1 * n1.Y));
        }
        //
       
    }
}
