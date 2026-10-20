using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace TestRosetteCurves
{
    // Writes small ISO-10303-21 files that exercise every curve entity the STEP curve reader
    // supports, so the reader can be tested without a CAD system
    internal static class StepFileGenerator
    {
        private static string Header(string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ISO-10303-21;");
            sb.AppendLine("HEADER;");
            sb.AppendLine("FILE_DESCRIPTION(('Rosette guide curves test'),'2;1');");
            sb.AppendLine("FILE_NAME('" + name + "','2026-08-07T00:00:00',('PrePoMax'),(''),'test','test','');");
            sb.AppendLine("FILE_SCHEMA(('AUTOMOTIVE_DESIGN { 1 0 10303 214 -1 1 5 4 }'));");
            sb.AppendLine("ENDSEC;");
            sb.AppendLine("DATA;");
            return sb.ToString();
        }

        private static string Footer()
        {
            return "ENDSEC;" + Environment.NewLine + "END-ISO-10303-21;" + Environment.NewLine;
        }

        private static string N(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        // Free curves collected in a GEOMETRIC_CURVE_SET - the usual shape of an exported wire file
        public static void WriteGeometricCurveSet(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Header("curves_geomset.stp"));
            //
            // 1. Polyline along X at y = 10, length 100
            sb.AppendLine("#10 = CARTESIAN_POINT('',(0.,10.,0.));");
            sb.AppendLine("#11 = CARTESIAN_POINT('',(50.,10.,0.));");
            sb.AppendLine("#12 = CARTESIAN_POINT('',(100.,10.,0.));");
            sb.AppendLine("#13 = POLYLINE('polyline-x',(#10,#11,#12));");
            //
            // 2. Cubic Bezier as a clamped B-spline from (0,20,0) to (100,20,0)
            sb.AppendLine("#20 = CARTESIAN_POINT('',(0.,20.,0.));");
            sb.AppendLine("#21 = CARTESIAN_POINT('',(30.,60.,0.));");
            sb.AppendLine("#22 = CARTESIAN_POINT('',(70.,-20.,0.));");
            sb.AppendLine("#23 = CARTESIAN_POINT('',(100.,20.,0.));");
            sb.AppendLine("#24 = B_SPLINE_CURVE_WITH_KNOTS('bspline',3,(#20,#21,#22,#23),.UNSPECIFIED.,.F.,.F.," +
                          "(4,4),(0.,1.),.UNSPECIFIED.);");
            //
            // 3. Rational quadratic B-spline: exact quarter circle of radius 50 around the origin
            sb.AppendLine("#30 = CARTESIAN_POINT('',(50.,0.,0.));");
            sb.AppendLine("#31 = CARTESIAN_POINT('',(50.,50.,0.));");
            sb.AppendLine("#32 = CARTESIAN_POINT('',(0.,50.,0.));");
            sb.AppendLine("#33 = ( BOUNDED_CURVE() B_SPLINE_CURVE(2,(#30,#31,#32),.UNSPECIFIED.,.F.,.F.) " +
                          "B_SPLINE_CURVE_WITH_KNOTS((3,3),(0.,1.),.UNSPECIFIED.) CURVE() " +
                          "GEOMETRIC_REPRESENTATION_ITEM() RATIONAL_B_SPLINE_CURVE((1.," +
                          N(Math.Sqrt(2.0) / 2.0) + ",1.)) REPRESENTATION_ITEM('') );");
            //
            // 4. Trimmed line from parameter 10 to 60 at y = -10, length 50
            sb.AppendLine("#40 = CARTESIAN_POINT('',(0.,-10.,0.));");
            sb.AppendLine("#41 = DIRECTION('',(1.,0.,0.));");
            sb.AppendLine("#42 = VECTOR('',#41,1.);");
            sb.AppendLine("#43 = LINE('',#40,#42);");
            sb.AppendLine("#44 = TRIMMED_CURVE('trimmed-line',#43,(PARAMETER_VALUE(10.)),(PARAMETER_VALUE(60.))," +
                          ".T.,.PARAMETER.);");
            //
            // 5. Composite curve of two polylines, total length 120
            sb.AppendLine("#50 = CARTESIAN_POINT('',(0.,-30.,0.));");
            sb.AppendLine("#51 = CARTESIAN_POINT('',(60.,-30.,0.));");
            sb.AppendLine("#52 = CARTESIAN_POINT('',(60.,-30.,0.));");
            sb.AppendLine("#53 = CARTESIAN_POINT('',(60.,-90.,0.));");
            sb.AppendLine("#54 = POLYLINE('',(#50,#51));");
            sb.AppendLine("#55 = POLYLINE('',(#52,#53));");
            sb.AppendLine("#56 = COMPOSITE_CURVE_SEGMENT(.CONTINUOUS.,.T.,#54);");
            sb.AppendLine("#57 = COMPOSITE_CURVE_SEGMENT(.CONTINUOUS.,.T.,#55);");
            sb.AppendLine("#58 = COMPOSITE_CURVE('composite',(#56,#57),.U.);");
            //
            sb.AppendLine("#90 = GEOMETRIC_CURVE_SET('guides',(#13,#24,#33,#44,#58));");
            //
            sb.Append(Footer());
            File.WriteAllText(fileName, sb.ToString());
        }

        // Edge curves of a wireframe: a line edge and a quarter circle edge
        public static void WriteEdgeCurves(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Header("curves_edges.stp"));
            //
            // Straight edge from (0,0,0) to (100,0,0)
            sb.AppendLine("#10 = CARTESIAN_POINT('',(0.,0.,0.));");
            sb.AppendLine("#11 = CARTESIAN_POINT('',(100.,0.,0.));");
            sb.AppendLine("#12 = VERTEX_POINT('',#10);");
            sb.AppendLine("#13 = VERTEX_POINT('',#11);");
            sb.AppendLine("#14 = DIRECTION('',(1.,0.,0.));");
            sb.AppendLine("#15 = VECTOR('',#14,1.);");
            sb.AppendLine("#16 = LINE('',#10,#15);");
            sb.AppendLine("#17 = EDGE_CURVE('line-edge',#12,#13,#16,.T.);");
            //
            // Quarter circle edge of radius 50 in the XY plane, from (50,0,0) to (0,50,0)
            sb.AppendLine("#20 = CARTESIAN_POINT('',(0.,0.,0.));");
            sb.AppendLine("#21 = DIRECTION('',(0.,0.,1.));");
            sb.AppendLine("#22 = DIRECTION('',(1.,0.,0.));");
            sb.AppendLine("#23 = AXIS2_PLACEMENT_3D('',#20,#21,#22);");
            sb.AppendLine("#24 = CIRCLE('',#23,50.);");
            sb.AppendLine("#25 = CARTESIAN_POINT('',(50.,0.,0.));");
            sb.AppendLine("#26 = CARTESIAN_POINT('',(0.,50.,0.));");
            sb.AppendLine("#27 = VERTEX_POINT('',#25);");
            sb.AppendLine("#28 = VERTEX_POINT('',#26);");
            sb.AppendLine("#29 = EDGE_CURVE('circle-edge',#27,#28,#24,.T.);");
            //
            sb.Append(Footer());
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
