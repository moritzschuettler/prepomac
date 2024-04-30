using Octree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Windows.Forms;

namespace CaeGlobals
{
    public class Geometry2
    {
        public struct Result
        {
            public double Distance;
            public double SqrDistance;
            public double[] Parameter;
            public Vec3D[] Closest;
        }
        //
        public Result ComputeClosest(Vec3D P0, Vec3D P1, Vec3D Q0, Vec3D Q1)
        {
            Vec3D P1mP0 = P1 - P0;
            Vec3D Q1mQ0 = Q1 - Q0;
            Vec3D P0mQ0 = P0 - Q0;
            double a = Vec3D.DotProduct(P1mP0, P1mP0);
            double b = Vec3D.DotProduct(P1mP0, Q1mQ0);
            double c = Vec3D.DotProduct(Q1mQ0, Q1mQ0);
            double d = Vec3D.DotProduct(P1mP0, P0mQ0);
            double e = Vec3D.DotProduct(Q1mQ0, P0mQ0);
            double det = a * c - b * b;
            double s, t, nd, bmd, bte, ctd, bpe, ate, btd;

            double zero = 0;
            double one = 1;
            if (det.CompareTo(zero) > 0)
            {
                bte = b * e;
                ctd = c * d;
                if (bte.CompareTo(ctd) <= 0)  // s <= 0
                {
                    s = zero;
                    if (e.CompareTo(zero) <= 0)  // t <= 0
                    {
                        t = zero;
                        nd = -d;
                        if (nd.CompareTo(a) >= 0)
                        {
                            s = one;
                        }
                        else if (nd.CompareTo(zero) > 0)
                        {
                            s = nd / a;
                        }
                    }
                    else if (e.CompareTo(c) < 0)  // 0 < t < 1
                    {
                        t = e / c;
                    }
                    else  // t >= 1
                    {
                        t = one;
                        bmd = b - d;
                        if (bmd.CompareTo(a) >= 0)
                        {
                            s = one;
                        }
                        else if (bmd.CompareTo(zero) > 0)
                        {
                            s = bmd / a;
                        }
                    }
                }
                else  // s > 0
                {
                    s = bte - ctd;
                    if (s.CompareTo(det) >= 0)  // s >= 1
                    {
                        s = one;
                        bpe = b + e;
                        if (bpe.CompareTo(zero) <= 0)  // t <= 0
                        {
                            t = zero;
                            nd = -d;
                            if (nd.CompareTo(zero) <= 0)
                            {
                                s = zero;
                            }
                            else if (nd.CompareTo(a) < 0)
                            {
                                s = nd / a;
                            }
                        }
                        else if (bpe.CompareTo(c) < 0)  // 0 < t < 1
                        {
                            t = bpe / c;
                        }
                        else  // t >= 1
                        {
                            t = one;
                            bmd = b - d;
                            if (bmd.CompareTo(zero) <= 0)
                            {
                                s = zero;
                            }
                            else if (bmd.CompareTo(a) < 0)
                            {
                                s = bmd / a;
                            }
                        }
                    }
                    else  // 0 < s < 1
                    {
                        ate = a * e;
                        btd = b * d;
                        if (ate.CompareTo(btd) <= 0)  // t <= 0
                        {
                            t = zero;
                            nd = -d;
                            if (nd.CompareTo(zero) <= 0)
                            {
                                s = zero;
                            }
                            else if (nd.CompareTo(a) >= 0)
                            {
                                s = one;
                            }
                            else
                            {
                                s = nd / a;
                            }
                        }
                        else  // t > 0
                        {
                            t = ate - btd;
                            if (t.CompareTo(det) >= 0)  // t >= 1
                            {
                                t = one;
                                bmd = b - d;
                                if (bmd.CompareTo(zero) <= 0)
                                {
                                    s = zero;
                                }
                                else if (bmd.CompareTo(a) >= 0)
                                {
                                    s = one;
                                }
                                else
                                {
                                    s = bmd / a;
                                }
                            }
                            else  // 0 < t < 1
                            {
                                s /= det;
                                t /= det;
                            }
                        }
                    }
                }
            }
            else
            {
                if (a.CompareTo(zero) > 0 && c.CompareTo(zero) > 0)
                {
                    // Compute the solutions to dR/ds(s0,0) = 0 and dR/ds(s1,1) = 0.
                    double[] sValue =
                    {
                        GetClampedRoot(a, b, d),
                        GetClampedRoot(a, -b, -d)
                    };
                    //
                    int[] classify = new int[2];
                    for (int i = 0; i < 2; ++i)
                    {
                        if (sValue[i].CompareTo(zero) <= 0)
                        {
                            classify[i] = -1;
                        }
                        else if (sValue[i].CompareTo(one) >= 0)
                        {
                            classify[i] = 1;
                        }
                        else
                        {
                            classify[i] = 0;
                        }
                    }

                    if (classify[0] == -1 && classify[1] == -1)
                    {
                        s = zero;
                        t = GetClampedRoot(c, e, zero);
                    }
                    else if (classify[0] == 1 && classify[1] == 1)
                    {
                        s = one;
                        t = GetClampedRoot(c, e - b, c);
                    }
                    else
                    {
                        // doublehe line dR/ds = 0 intersects the domain [0,1]^2 in a
                        // nondegenerate segment.
                        // Compute the endpoints of that segment, end[0] and end[1].
                        int[] edge = new int[2];
                        double[][] end = new double[2][];
                        ComputeIntersection(sValue, classify, b, d, a, c, e, out edge, out end);

                        // Compute the minimum of R on [0,1]^2.
                        ComputeMinimumParameters(edge, end, b, c, e, d, a, out s, out t);
                    }
                }
                else
                {
                    if (a.CompareTo(zero) > 0)
                    {
                        // doublehe Q segment is degenerate (Q0 = Q1), which implies line0 and line1 are parallel.
                        s = GetClampedRoot(a, -b, -d);
                        if (s.CompareTo(zero) < 0)
                        {
                            s = zero;
                        }
                        else if (s.CompareTo(one) > 0)
                        {
                            s = one;
                        }

                        t = zero;
                    }
                    else if (c.CompareTo(zero) > 0)
                    {
                        // doublehe P segment is degenerate (P0 = P1), which implies line0 and line1 are parallel.
                        s = zero;
                        t = GetClampedRoot(c, e, zero);
                        if (t.CompareTo(zero) < 0)
                        {
                            t = zero;
                        }
                        else if (t.CompareTo(one) > 0)
                        {
                            t = one;
                        }
                    }
                    else
                    {
                        // Both segments are degenerate, which implies the lines are parallel.
                        s = zero;
                        t = zero;
                    }
                }
            }

            Vec3D closest0 = P0 + s * P1mP0;
            Vec3D closest1 = Q0 + t * Q1mQ0;
            Vec3D diff = closest1 - closest0;
            double sqrDistance = Vec3D.DotProduct(diff, diff);

            return new Result
            {
                Distance = Math.Sqrt(sqrDistance),
                SqrDistance = sqrDistance,
                Parameter = new double[] { s, t },
                Closest = new Vec3D[] { closest0, closest1 }
            };
        }
        private double GetClampedRoot(double a, double b, double c)
        {
            double discr = b * b - a * c;
            if (discr.CompareTo(default(double)) < 0)
            {
                return default(double);
            }

            double root = Math.Sqrt(discr);
            if (b.CompareTo(default(double)) < 0)
            {
                root = -root;
            }

            double max = a.CompareTo(default(double)) > 0 ? -b / a : default(double);
            if (root.CompareTo(max) > 0)
            {
                root = max;
            }
            else if (root.CompareTo(-max) < 0)
            {
                root = -max;
            }

            return root;
        }
        private void ComputeIntersection(double[] sValue, int[] classify, double b, double d, double a, double c, double e, out int[] edge, out double[][] end)
        {
            edge = new int[2];
            end = new double[2][];
            for (int i = 0; i < 2; ++i)
            {
                if (classify[i] == 0)
                {
                    edge[i] = 1;
                    end[i] = new double[2] { default(double), default(double) };
                }
                else
                {
                    edge[i] = 0;
                    double invDet = 1 / (a * c);
                    end[i] = new double[2];
                    end[i][0] = sValue[i];
                    end[i][1] = (a * e - b * d) * invDet;
                }
            }
        }
        private void ComputeMinimumParameters(int[] edge, double[][] end, double b, double c, double e, double d, double a, out double s, out double t)
        {
            double zero = default(double);
            double one = (double)(dynamic)1;
            if (edge[0] == 1)
            {
                s = end[0][0];
                if (s.CompareTo(zero) < 0)
                {
                    s = zero;
                }
                else if (s.CompareTo(one) > 0)
                {
                    s = one;
                }

                t = GetClampedRoot(c, e, zero);
                if (t.CompareTo(zero) < 0)
                {
                    t = zero;
                }
                else if (t.CompareTo(one) > 0)
                {
                    t = one;
                }
            }
            else if (edge[1] == 1)
            {
                s = GetClampedRoot(a, b, d);
                if (s.CompareTo(zero) < 0)
                {
                    s = zero;
                }
                else if (s.CompareTo(one) > 0)
                {
                    s = one;
                }

                t = end[1][0];
                if (t.CompareTo(zero) < 0)
                {
                    t = zero;
                }
                else if (t.CompareTo(one) > 0)
                {
                    t = one;
                }
            }
            else
            {
                // doublehe solutions end[0][1] and end[1][1] are in (0,1).
                s = end[0][1];
                t = end[1][1];
            }
        }
    }
}
