using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CaeGlobals;
using CaeModel;

namespace TestRosetteCurves
{
    // Tests the rosette creation with curves:
    //   1. writes synthetic STEP files covering every supported curve entity
    //   2. reads them back with StepCurveReader and checks the tessellated geometry
    //   3. builds a Rosette and checks the directions returned by RosetteEvaluator
    //   4. exports an OBJ file of the polylines for a visual check
    //
    // Usage: TestRosetteCurves.exe [pathToRealStepFile]
    internal class Program
    {
        private static int _passed;
        private static int _failed;

        static int Main(string[] args)
        {
            string outputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
            Directory.CreateDirectory(outputDirectory);
            //
            Console.WriteLine("=== Rosette curve test ===");
            Console.WriteLine("Output directory: " + outputDirectory);
            Console.WriteLine();
            //
            try
            {
                TestGeometricCurveSet(outputDirectory);
                TestEdgeCurves(outputDirectory);
                TestEvaluatorCartesian();
                TestEvaluatorFromCurves();
                TestDirectionKey();
                if (args.Length > 0) DumpRealFile(args[0], outputDirectory);
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine("UNHANDLED: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                _failed++;
            }
            //
            Console.WriteLine();
            Console.WriteLine("=== " + _passed + " passed, " + _failed + " failed ===");
            return _failed == 0 ? 0 : 1;
        }


        //
        // Tests
        //
        private static void TestGeometricCurveSet(string outputDirectory)
        {
            Console.WriteLine("--- GEOMETRIC_CURVE_SET ---");
            string fileName = Path.Combine(outputDirectory, "curves_geomset.stp");
            StepFileGenerator.WriteGeometricCurveSet(fileName);
            //
            string log;
            List<RosetteCurve> curves = StepCurveReader.Read(fileName, 0, out log);
            Console.WriteLine(log.TrimEnd());
            //
            Check("curve count", curves.Count == 5, curves.Count + " curves");
            //
            RosetteCurve polyline = FindCurve(curves, "polyline");
            Check("polyline length = 100", polyline != null && Near(polyline.Length, 100, 1E-6),
                  polyline == null ? "not found" : polyline.Length.ToString("G6"));
            //
            RosetteCurve bspline = FindCurve(curves, "b-spline");
            if (bspline != null)
            {
                double[] first = bspline.Points[0];
                double[] last = bspline.Points[bspline.Points.Length - 1];
                Check("b-spline start = (0,20,0)", NearPoint(first, 0, 20, 0, 1E-6), Format(first));
                Check("b-spline end = (100,20,0)", NearPoint(last, 100, 20, 0, 1E-6), Format(last));
                Check("b-spline is refined", bspline.PointCount > 20, bspline.PointCount + " points");
            }
            else Check("b-spline found", false, "not found");
            //
            // The rational quadratic must reproduce a circle of radius 50 around the origin
            RosetteCurve rational = curves.FirstOrDefault(c => c.SourceLabel.Contains("#33"));
            if (rational != null)
            {
                double maxError = 0;
                foreach (double[] p in rational.Points)
                {
                    double radius = Math.Sqrt(p[0] * p[0] + p[1] * p[1] + p[2] * p[2]);
                    maxError = Math.Max(maxError, Math.Abs(radius - 50));
                }
                Check("rational b-spline lies on r = 50", maxError < 1E-6,
                      "max radius error " + maxError.ToString("G4"));
            }
            else Check("rational b-spline found", false, "not found");
            //
            RosetteCurve trimmed = curves.FirstOrDefault(c => c.SourceLabel.Contains("trimmed"));
            if (trimmed != null)
            {
                Check("trimmed line length = 50", Near(trimmed.Length, 50, 1E-6), trimmed.Length.ToString("G6"));
                Check("trimmed line starts at x = 10", Near(trimmed.Points[0][0], 10, 1E-6),
                      Format(trimmed.Points[0]));
            }
            else Check("trimmed line found", false, "not found");
            //
            RosetteCurve composite = curves.FirstOrDefault(c => c.SourceLabel.Contains("composite"));
            Check("composite curve length = 120", composite != null && Near(composite.Length, 120, 1E-6),
                  composite == null ? "not found" : composite.Length.ToString("G6"));
            //
            WriteObj(Path.Combine(outputDirectory, "curves_geomset.obj"), curves);
            Console.WriteLine();
        }

        private static void TestEdgeCurves(string outputDirectory)
        {
            Console.WriteLine("--- EDGE_CURVE ---");
            string fileName = Path.Combine(outputDirectory, "curves_edges.stp");
            StepFileGenerator.WriteEdgeCurves(fileName);
            //
            string log;
            List<RosetteCurve> curves = StepCurveReader.Read(fileName, 0, out log);
            Console.WriteLine(log.TrimEnd());
            //
            Check("edge count", curves.Count == 2, curves.Count + " curves");
            //
            RosetteCurve line = FindCurve(curves, "line");
            Check("line edge length = 100", line != null && Near(line.Length, 100, 1E-6),
                  line == null ? "not found" : line.Length.ToString("G6"));
            //
            RosetteCurve circle = FindCurve(curves, "circle");
            if (circle != null)
            {
                double expected = 2 * Math.PI * 50 / 4;
                // A tessellated arc is slightly shorter than the exact one
                Check("circle edge length ~ " + expected.ToString("G6"),
                      Math.Abs(circle.Length - expected) < 0.05, circle.Length.ToString("G6"));
                Check("circle edge starts at (50,0,0)", NearPoint(circle.Points[0], 50, 0, 0, 1E-6),
                      Format(circle.Points[0]));
                Check("circle edge ends at (0,50,0)",
                      NearPoint(circle.Points[circle.Points.Length - 1], 0, 50, 0, 1E-6),
                      Format(circle.Points[circle.Points.Length - 1]));
            }
            else Check("circle edge found", false, "not found");
            //
            WriteObj(Path.Combine(outputDirectory, "curves_edges.obj"), curves);
            Console.WriteLine();
        }

        private static void TestEvaluatorCartesian()
        {
            Console.WriteLine("--- Cartesian rosette ---");
            Rosette rosette = new Rosette("rosette.1");
            rosette.AxisX = new double[] { 1, 1, 0 };
            rosette.AxisY = new double[] { -1, 1, 0 };
            //
            Check("default name pattern", Rosette.GetNextName(new string[] { "rosette.1", "rosette.4" })
                  == "rosette.5", Rosette.GetNextName(new string[] { "rosette.1", "rosette.4" }));
            Check("validity of a valid rosette", rosette.CheckValidity() == null,
                  rosette.CheckValidity() ?? "ok");
            //
            RosetteEvaluator evaluator = new RosetteEvaluator(rosette);
            Vec3D direction;
            bool ok = evaluator.TryGetRefDirection(new double[] { 123, -45, 6 }, out direction);
            double expected = 1.0 / Math.Sqrt(2.0);
            Check("cartesian direction is the normalized X axis",
                  ok && Near(direction.X, expected, 1E-9) && Near(direction.Y, expected, 1E-9) &&
                  Near(direction.Z, 0, 1E-9),
                  ok ? Format(direction.Coor) : "no direction");
            //
            // Projection onto a shell whose normal is Z must keep the direction unchanged
            Vec3D localX, localY;
            RosetteEvaluator.ProjectOntoPlane(direction, new Vec3D(0, 0, 1), evaluator.AxisY, out localX, out localY);
            Check("projection onto the XY plane is the identity",
                  Near(localX.X, expected, 1E-9) && Near(localX.Y, expected, 1E-9), Format(localX.Coor));
            Check("local Y is perpendicular to local X",
                  Near(Vec3D.DotProduct(localX, localY), 0, 1E-9),
                  Vec3D.DotProduct(localX, localY).ToString("G4"));
            //
            // A direction parallel to the normal must fall back onto the Y axis of the rosette
            RosetteEvaluator.ProjectOntoPlane(new Vec3D(0, 0, 1), new Vec3D(0, 0, 1), evaluator.AxisY,
                                              out localX, out localY);
            Check("degenerate projection falls back to the Y axis",
                  Near(localX.X, -expected, 1E-9) && Near(localX.Y, expected, 1E-9), Format(localX.Coor));
            //
            Rosette invalid = new Rosette("rosette.2");
            invalid.AxisY = new double[] { 2, 0, 0 };
            Check("parallel axes are rejected", invalid.CheckValidity() != null,
                  invalid.CheckValidity() ?? "accepted");
            Console.WriteLine();
        }

        private static void TestEvaluatorFromCurves()
        {
            Console.WriteLine("--- Curve rosette ---");
            // Two parallel guide curves along X at y = 0 and y = 100
            Rosette rosette = new Rosette("rosette.3");
            rosette.Type = RosetteType.FromCurves;
            rosette.Curves = new List<RosetteCurve>
            {
                new RosetteCurve(Sample(0, 0, 100, 0, 40), "#1 lower"),
                new RosetteCurve(Sample(0, 100, 100, 100, 40), "#2 upper")
            };
            //
            RosetteEvaluator evaluator = new RosetteEvaluator(rosette);
            Check("evaluator is usable", evaluator.IsUsable, evaluator.SegmentCount + " segments");
            //
            Vec3D direction;
            bool ok = evaluator.TryGetRefDirection(new double[] { 50, 5, 0 }, out direction);
            Check("direction near the lower curve is +X",
                  ok && Near(direction.X, 1, 1E-9) && Near(direction.Y, 0, 1E-9),
                  ok ? Format(direction.Coor) : "no direction");
            //
            // Reversing the second curve must flip only its own direction
            rosette.Curves[1].Reversed = true;
            evaluator = new RosetteEvaluator(rosette);
            ok = evaluator.TryGetRefDirection(new double[] { 50, 95, 0 }, out direction);
            Check("reversed curve returns -X",
                  ok && Near(direction.X, -1, 1E-9), ok ? Format(direction.Coor) : "no direction");
            ok = evaluator.TryGetRefDirection(new double[] { 50, 5, 0 }, out direction);
            Check("the other curve is unaffected",
                  ok && Near(direction.X, 1, 1E-9), ok ? Format(direction.Coor) : "no direction");
            //
            // Excluding a curve must remove it from the search
            rosette.Curves[1].Included = false;
            evaluator = new RosetteEvaluator(rosette);
            ok = evaluator.TryGetRefDirection(new double[] { 50, 95, 0 }, out direction);
            Check("excluded curve is ignored", ok && Near(direction.X, 1, 1E-9),
                  ok ? Format(direction.Coor) : "no direction");
            Check("included curve count", rosette.IncludedCurveCount == 1,
                  rosette.IncludedCurveCount.ToString());
            //
            // A rosette of type FromCurves without any curve must be reported as invalid
            Rosette empty = new Rosette("rosette.4");
            empty.Type = RosetteType.FromCurves;
            Check("empty curve rosette is rejected", empty.CheckValidity() != null,
                  empty.CheckValidity() ?? "accepted");
            //
            // 90 degree guided: the curve tangent becomes the 90 degree material direction, so the
            // material X axis must be perpendicular to the curve and the local Y must be the tangent
            rosette.Curves[1].Included = true;
            rosette.Curves[1].Reversed = false;
            rosette.GuideAngle = RosetteGuideAngle.Deg90;
            evaluator = new RosetteEvaluator(rosette);
            Vec3D shellNormal = new Vec3D(0, 0, 1);
            ok = evaluator.TryGetRefDirection(new double[] { 50, 5, 0 }, shellNormal, out direction);
            Check("90 deg guided rotates X perpendicular to the curve",
                  ok && Near(direction.X, 0, 1E-9) && Near(direction.Y, -1, 1E-9),
                  ok ? Format(direction.Coor) : "no direction");
            if (ok)
            {
                Vec3D gx, gy;
                RosetteEvaluator.ProjectOntoPlane(direction, shellNormal, evaluator.AxisY, out gx, out gy);
                Check("90 deg guided puts the tangent on the local Y axis",
                      Near(gy.X, 1, 1E-9) && Near(gy.Y, 0, 1E-9), Format(gy.Coor));
            }
            // The 0 degree mode must leave the tangent on the X axis
            rosette.GuideAngle = RosetteGuideAngle.Deg0;
            evaluator = new RosetteEvaluator(rosette);
            ok = evaluator.TryGetRefDirection(new double[] { 50, 5, 0 }, shellNormal, out direction);
            Check("0 deg guided keeps X along the curve",
                  ok && Near(direction.X, 1, 1E-9), ok ? Format(direction.Coor) : "no direction");
            //
            // A single long segment: the vertex based search must still find it from a point that
            // is closest to the middle of the segment, far from both end vertices
            Rosette single = new Rosette("rosette.6");
            single.Type = RosetteType.FromCurves;
            single.Curves = new List<RosetteCurve>
            {
                new RosetteCurve(new double[][]
                {
                    new double[] { -5000, 0, 0 },
                    new double[] { 5000, 0, 0 }
                }, "#4 long line")
            };
            RosetteEvaluator singleEvaluator = new RosetteEvaluator(single);
            ok = singleEvaluator.TryGetRefDirection(new double[] { 0, 3, 0 }, out direction);
            Check("two point segment is found from its middle",
                  ok && Near(direction.X, 1, 1E-9), ok ? Format(direction.Coor) : "no direction");
            //
            // A curved guide: the direction must follow the tangent of a circle
            Rosette arc = new Rosette("rosette.5");
            arc.Type = RosetteType.FromCurves;
            arc.Curves = new List<RosetteCurve> { new RosetteCurve(SampleArc(50, 400), "#3 arc") };
            RosetteEvaluator arcEvaluator = new RosetteEvaluator(arc);
            ok = arcEvaluator.TryGetRefDirection(new double[] { 0, 52, 0 }, out direction);
            Check("tangent at the top of the arc is -X",
                  ok && Near(direction.X, -1, 5E-3) && Math.Abs(direction.Y) < 5E-3,
                  ok ? Format(direction.Coor) : "no direction");
            Console.WriteLine();
        }

        private static void TestDirectionKey()
        {
            Console.WriteLine("--- Direction quantization ---");
            Vec3D a = new Vec3D(1, 0, 0);
            Vec3D b = new Vec3D(Math.Cos(0.02), Math.Sin(0.02), 0);       // about 1.1 degrees apart
            Vec3D c = new Vec3D(Math.Cos(0.5), Math.Sin(0.5), 0);         // about 28.6 degrees apart
            Vec3D opposite = new Vec3D(-1, 0, 0);
            //
            string keyA = RosetteEvaluator.GetDirectionKey(a, 5);
            Check("close directions share one orientation",
                  keyA == RosetteEvaluator.GetDirectionKey(b, 5), keyA + " vs " +
                  RosetteEvaluator.GetDirectionKey(b, 5));
            Check("distant directions get their own orientation",
                  keyA != RosetteEvaluator.GetDirectionKey(c, 5), keyA + " vs " +
                  RosetteEvaluator.GetDirectionKey(c, 5));
            Check("a fiber direction has no sign",
                  keyA == RosetteEvaluator.GetDirectionKey(opposite, 5), keyA + " vs " +
                  RosetteEvaluator.GetDirectionKey(opposite, 5));
            Console.WriteLine();
        }

        private static void DumpRealFile(string fileName, string outputDirectory)
        {
            Console.WriteLine("--- " + Path.GetFileName(fileName) + " ---");
            string log;
            List<RosetteCurve> curves = StepCurveReader.Read(fileName, 0, out log);
            Console.WriteLine(log.TrimEnd());
            string objFile = Path.Combine(outputDirectory,
                                          Path.GetFileNameWithoutExtension(fileName) + ".obj");
            WriteObj(objFile, curves);
            Console.WriteLine("Wrote " + objFile);
            Console.WriteLine();
        }


        //
        // Helpers
        //
        private static double[][] Sample(double x0, double y0, double x1, double y1, int count)
        {
            double[][] points = new double[count + 1][];
            for (int i = 0; i <= count; i++)
            {
                double t = (double)i / count;
                points[i] = new double[] { x0 + t * (x1 - x0), y0 + t * (y1 - y0), 0 };
            }
            return points;
        }

        // Counter clockwise arc of the given radius from 0 to 180 degrees in the XY plane
        private static double[][] SampleArc(double radius, int count)
        {
            double[][] points = new double[count + 1][];
            for (int i = 0; i <= count; i++)
            {
                double angle = Math.PI * i / count;
                points[i] = new double[] { radius * Math.Cos(angle), radius * Math.Sin(angle), 0 };
            }
            return points;
        }

        private static RosetteCurve FindCurve(List<RosetteCurve> curves, string keyword)
        {
            return curves.FirstOrDefault(c => c.SourceLabel.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool Near(double value, double expected, double tolerance)
        {
            return Math.Abs(value - expected) <= tolerance;
        }

        private static bool NearPoint(double[] point, double x, double y, double z, double tolerance)
        {
            return point != null && Near(point[0], x, tolerance) && Near(point[1], y, tolerance) &&
                   Near(point[2], z, tolerance);
        }

        private static string Format(double[] point)
        {
            if (point == null) return "null";
            return string.Format(CultureInfo.InvariantCulture, "({0:G6}, {1:G6}, {2:G6})",
                                 point[0], point[1], point[2]);
        }

        private static void Check(string what, bool condition, string detail)
        {
            if (condition) { _passed++; Console.WriteLine("  PASS  " + what + "  [" + detail + "]"); }
            else { _failed++; Console.WriteLine("  FAIL  " + what + "  [" + detail + "]"); }
        }

        private static void WriteObj(string fileName, List<RosetteCurve> curves)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# Rosette guide curves");
            int offset = 1;
            foreach (RosetteCurve curve in curves)
            {
                double[][] points = curve.GetEffectivePoints();
                if (points == null || points.Length < 2) continue;
                sb.AppendLine("o " + curve.SourceLabel.Replace(' ', '_'));
                foreach (double[] p in points)
                    sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "v {0:G9} {1:G9} {2:G9}",
                                                p[0], p[1], p[2]));
                for (int i = 0; i < points.Length - 1; i++)
                    sb.AppendLine("l " + (offset + i) + " " + (offset + i + 1));
                offset += points.Length;
            }
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
