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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CaeGlobals;

namespace CaeModel
{
    // Reads guide curves from an ISO-10303-21 (STEP) file and tessellates them into polylines.
    // Only the curve geometry is interpreted - surfaces, solids and topology beyond edge curves
    // are ignored. Coordinates are used as they are stored in the file; no unit conversion is
    // applied, which matches the behaviour of the CAD import of PrePoMax.
    public static class StepCurveReader
    {
        // Value kinds of a STEP parameter
        private enum ValKind { Ref, Num, Str, Enum, List, Null, Typed }

        private class StepVal
        {
            public ValKind Kind;
            public int Ref;
            public double Num;
            public string Str;
            public string TypeName;
            public List<StepVal> Items;
        }

        private class StepEntity
        {
            public int Id;
            public string Type;                                 // simple records
            public List<StepVal> Args;                          // simple records
            public Dictionary<string, List<StepVal>> Parts;     // complex records

            public bool Has(string type)
            {
                if (Type == type) return true;
                return Parts != null && Parts.ContainsKey(type);
            }
            public List<StepVal> ArgsOf(string type)
            {
                if (Type == type) return Args;
                List<StepVal> args;
                if (Parts != null && Parts.TryGetValue(type, out args)) return args;
                return null;
            }
        }

        private class Frame
        {
            public double[] Origin;
            public Vec3D X;
            public Vec3D Y;
            public Vec3D Z;
        }

        private static readonly string[] _curveTypes = new string[]
        {
            "LINE", "CIRCLE", "ELLIPSE", "POLYLINE", "B_SPLINE_CURVE_WITH_KNOTS",
            "B_SPLINE_CURVE", "BEZIER_CURVE", "TRIMMED_CURVE", "COMPOSITE_CURVE"
        };


        // Reads every guide curve of the file. Throws a CaeException when nothing could be read.
        public static List<RosetteCurve> Read(string fileName, double chordTolerance, out string log)
        {
            double usedTolerance;
            return Read(fileName, chordTolerance, out log, out usedTolerance);
        }

        // usedTolerance returns the chord tolerance that was applied; it differs from chordTolerance
        // when the automatic tolerance (chordTolerance <= 0) is used
        public static List<RosetteCurve> Read(string fileName, double chordTolerance, out string log,
                                              out double usedTolerance)
        {
            if (!File.Exists(fileName)) throw new CaeException("The file '" + fileName + "' does not exist.");
            //
            usedTolerance = chordTolerance;
            StringBuilder logBuilder = new StringBuilder();
            Dictionary<int, StepEntity> entities = ParseFile(fileName);
            logBuilder.AppendLine("STEP file: " + fileName);
            logBuilder.AppendLine("Entities read: " + entities.Count);
            //
            List<int> curveIds = CollectTopLevelCurves(entities, logBuilder);
            if (curveIds.Count == 0)
            {
                log = logBuilder.ToString();
                throw new CaeException("No curve geometry was found in the STEP file '" +
                                       Path.GetFileName(fileName) + "'.\n" +
                                       "Supported entities: LINE, CIRCLE, ELLIPSE, POLYLINE, " +
                                       "B_SPLINE_CURVE_WITH_KNOTS (also rational), TRIMMED_CURVE, " +
                                       "COMPOSITE_CURVE and EDGE_CURVE.");
            }
            // Automatic chord tolerance from the size of the whole curve set
            double tolerance = chordTolerance;
            bool automatic = tolerance <= 0;
            if (automatic) tolerance = ComputeAutoTolerance(entities, curveIds);
            usedTolerance = tolerance;
            logBuilder.AppendLine("Chord tolerance: " + tolerance.ToString("G4", CultureInfo.InvariantCulture) +
                                  (automatic ? " (automatic: bounding box diagonal / 2000)" : ""));
            logBuilder.AppendLine();
            //
            List<RosetteCurve> curves = new List<RosetteCurve>();
            HashSet<string> unsupported = new HashSet<string>();
            foreach (int id in curveIds)
            {
                StepEntity entity = entities[id];
                List<double[]> points = null;
                try
                {
                    points = TessellateTopLevel(entity, entities, tolerance);
                }
                catch (Exception ex)
                {
                    logBuilder.AppendLine("  #" + id + " " + entity.Type + ": " + ex.Message);
                    continue;
                }
                if (points == null)
                {
                    unsupported.Add(entity.Type ?? "COMPLEX");
                    continue;
                }
                points = CleanUp(points);
                if (points.Count < 2) continue;
                //
                RosetteCurve curve = new RosetteCurve(points.ToArray(), "#" + id + " " + GetCurveKindName(entity, entities));
                curves.Add(curve);
                logBuilder.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "  {0}: {1} points, length {2:G6}", curve.SourceLabel, curve.PointCount, curve.Length));
            }
            if (unsupported.Count > 0)
                logBuilder.AppendLine("Unsupported curve types skipped: " + string.Join(", ", unsupported));
            //
            log = logBuilder.ToString();
            if (curves.Count == 0)
                throw new CaeException("No curve of the STEP file '" + Path.GetFileName(fileName) +
                                       "' could be tessellated." +
                                       (unsupported.Count > 0
                                            ? "\nUnsupported curve types: " + string.Join(", ", unsupported)
                                            : ""));
            return curves;
        }


        //
        // Part 21 parsing
        //
        private static Dictionary<int, StepEntity> ParseFile(string fileName)
        {
            string text = File.ReadAllText(fileName);
            int dataStart = text.IndexOf("DATA;", StringComparison.OrdinalIgnoreCase);
            if (dataStart < 0) throw new CaeException("The file is not a valid STEP (ISO-10303-21) file: " +
                                                     "no DATA section was found.");
            string data = text.Substring(dataStart + 5);
            int dataEnd = data.IndexOf("ENDSEC;", StringComparison.OrdinalIgnoreCase);
            if (dataEnd >= 0) data = data.Substring(0, dataEnd);
            //
            Dictionary<int, StepEntity> entities = new Dictionary<int, StepEntity>();
            foreach (string statement in SplitStatements(data))
            {
                StepEntity entity = ParseStatement(statement);
                if (entity != null) entities[entity.Id] = entity;
            }
            return entities;
        }

        // Splits the DATA section on the semicolons that are outside strings and comments
        private static IEnumerable<string> SplitStatements(string data)
        {
            StringBuilder current = new StringBuilder();
            bool inString = false;
            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];
                if (inString)
                {
                    current.Append(c);
                    if (c == '\'')
                    {
                        if (i + 1 < data.Length && data[i + 1] == '\'') { current.Append(data[++i]); }
                        else inString = false;
                    }
                    continue;
                }
                if (c == '\'') { inString = true; current.Append(c); continue; }
                if (c == '/' && i + 1 < data.Length && data[i + 1] == '*')
                {
                    int end = data.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? data.Length : end + 1;
                    continue;
                }
                if (c == ';')
                {
                    string statement = current.ToString().Trim();
                    current.Clear();
                    if (statement.Length > 0) yield return statement;
                    continue;
                }
                current.Append(c);
            }
        }

        private static StepEntity ParseStatement(string statement)
        {
            if (statement.Length == 0 || statement[0] != '#') return null;
            int eq = statement.IndexOf('=');
            if (eq < 0) return null;
            int id;
            if (!int.TryParse(statement.Substring(1, eq - 1).Trim(), out id)) return null;
            //
            string body = statement.Substring(eq + 1).Trim();
            if (body.Length == 0) return null;
            //
            StepEntity entity = new StepEntity();
            entity.Id = id;
            //
            if (body[0] == '(')
            {
                // Complex instance: (TYPE_A(...)TYPE_B(...))
                entity.Parts = new Dictionary<string, List<StepVal>>();
                int pos = 1;
                while (pos < body.Length)
                {
                    while (pos < body.Length && (char.IsWhiteSpace(body[pos]) || body[pos] == ',')) pos++;
                    if (pos >= body.Length || body[pos] == ')') break;
                    int nameStart = pos;
                    while (pos < body.Length && body[pos] != '(') pos++;
                    if (pos >= body.Length) break;
                    string typeName = body.Substring(nameStart, pos - nameStart).Trim().ToUpperInvariant();
                    List<StepVal> args = ParseList(body, ref pos);
                    entity.Parts[typeName] = args;
                }
                entity.Type = null;
                return entity;
            }
            //
            int paren = body.IndexOf('(');
            if (paren < 0) return null;
            entity.Type = body.Substring(0, paren).Trim().ToUpperInvariant();
            int p = paren;
            entity.Args = ParseList(body, ref p);
            return entity;
        }

        // Parses a parenthesized list starting at body[pos] == '('; leaves pos after the ')'
        private static List<StepVal> ParseList(string body, ref int pos)
        {
            List<StepVal> items = new List<StepVal>();
            pos++;      // skip '('
            while (pos < body.Length)
            {
                while (pos < body.Length && (char.IsWhiteSpace(body[pos]) || body[pos] == ',')) pos++;
                if (pos >= body.Length) break;
                if (body[pos] == ')') { pos++; break; }
                items.Add(ParseValue(body, ref pos));
            }
            return items;
        }

        private static StepVal ParseValue(string body, ref int pos)
        {
            char c = body[pos];
            StepVal val = new StepVal();
            if (c == '(')
            {
                val.Kind = ValKind.List;
                val.Items = ParseList(body, ref pos);
                return val;
            }
            if (c == '#')
            {
                int start = ++pos;
                while (pos < body.Length && char.IsDigit(body[pos])) pos++;
                val.Kind = ValKind.Ref;
                int id;
                int.TryParse(body.Substring(start, pos - start), out id);
                val.Ref = id;
                return val;
            }
            if (c == '\'')
            {
                StringBuilder sb = new StringBuilder();
                pos++;
                while (pos < body.Length)
                {
                    if (body[pos] == '\'')
                    {
                        if (pos + 1 < body.Length && body[pos + 1] == '\'') { sb.Append('\''); pos += 2; continue; }
                        pos++;
                        break;
                    }
                    sb.Append(body[pos++]);
                }
                val.Kind = ValKind.Str;
                val.Str = sb.ToString();
                return val;
            }
            if (c == '.')
            {
                int start = pos++;
                while (pos < body.Length && body[pos] != '.') pos++;
                if (pos < body.Length) pos++;
                val.Kind = ValKind.Enum;
                val.Str = body.Substring(start, pos - start).Trim('.');
                return val;
            }
            if (c == '$') { pos++; val.Kind = ValKind.Null; return val; }
            if (c == '*') { pos++; val.Kind = ValKind.Null; return val; }
            if (c == '-' || c == '+' || char.IsDigit(c))
            {
                int start = pos;
                pos++;
                while (pos < body.Length &&
                       (char.IsDigit(body[pos]) || body[pos] == '.' || body[pos] == 'E' || body[pos] == 'e' ||
                        ((body[pos] == '-' || body[pos] == '+') && (body[pos - 1] == 'E' || body[pos - 1] == 'e'))))
                    pos++;
                val.Kind = ValKind.Num;
                double number;
                double.TryParse(body.Substring(start, pos - start), NumberStyles.Float,
                                CultureInfo.InvariantCulture, out number);
                val.Num = number;
                return val;
            }
            // Inline typed value: NAME(...)
            {
                int start = pos;
                while (pos < body.Length && body[pos] != '(' && body[pos] != ',' && body[pos] != ')') pos++;
                string name = body.Substring(start, pos - start).Trim().ToUpperInvariant();
                val.Kind = ValKind.Typed;
                val.TypeName = name;
                if (pos < body.Length && body[pos] == '(') val.Items = ParseList(body, ref pos);
                else val.Items = new List<StepVal>();
                return val;
            }
        }


        //
        // Curve collection
        //
        private static List<int> CollectTopLevelCurves(Dictionary<int, StepEntity> entities, StringBuilder log)
        {
            List<int> result = new List<int>();
            HashSet<int> added = new HashSet<int>();
            // 1. Curves listed in a geometric curve set - the usual shape of an exported wire file
            foreach (var entry in entities)
            {
                StepEntity entity = entry.Value;
                if (!entity.Has("GEOMETRIC_CURVE_SET") && !entity.Has("GEOMETRIC_SET")) continue;
                List<StepVal> args = entity.ArgsOf("GEOMETRIC_CURVE_SET") ?? entity.ArgsOf("GEOMETRIC_SET");
                if (args == null || args.Count < 2 || args[1].Kind != ValKind.List) continue;
                foreach (StepVal item in args[1].Items)
                {
                    if (item.Kind != ValKind.Ref) continue;
                    StepEntity target;
                    if (!entities.TryGetValue(item.Ref, out target)) continue;
                    if (IsCurve(target) && added.Add(item.Ref)) result.Add(item.Ref);
                }
            }
            if (result.Count > 0)
            {
                log.AppendLine("Curve source: geometric curve set (" + result.Count + " curves)");
                result.Sort();
                return result;
            }
            // 2. Edge curves - a wireframe or a solid whose edges are used as guides
            foreach (var entry in entities)
            {
                if (entry.Value.Has("EDGE_CURVE") && added.Add(entry.Key)) result.Add(entry.Key);
            }
            if (result.Count > 0)
            {
                log.AppendLine("Curve source: edge curves (" + result.Count + " edges)");
                result.Sort();
                return result;
            }
            // 3. Every free curve that is not consumed by another curve
            HashSet<int> consumed = CollectConsumedCurves(entities);
            foreach (var entry in entities)
            {
                if (IsCurve(entry.Value) && !consumed.Contains(entry.Key) && added.Add(entry.Key))
                    result.Add(entry.Key);
            }
            if (result.Count > 0) log.AppendLine("Curve source: free curves (" + result.Count + " curves)");
            result.Sort();
            return result;
        }

        private static HashSet<int> CollectConsumedCurves(Dictionary<int, StepEntity> entities)
        {
            HashSet<int> consumed = new HashSet<int>();
            foreach (var entry in entities)
            {
                StepEntity entity = entry.Value;
                List<StepVal> args;
                if ((args = entity.ArgsOf("TRIMMED_CURVE")) != null && args.Count > 1 && args[1].Kind == ValKind.Ref)
                    consumed.Add(args[1].Ref);
                if ((args = entity.ArgsOf("COMPOSITE_CURVE_SEGMENT")) != null && args.Count > 2 &&
                    args[2].Kind == ValKind.Ref)
                    consumed.Add(args[2].Ref);
                if ((args = entity.ArgsOf("COMPOSITE_CURVE")) != null && args.Count > 1 && args[1].Kind == ValKind.List)
                    foreach (StepVal item in args[1].Items) if (item.Kind == ValKind.Ref) consumed.Add(item.Ref);
                if ((args = entity.ArgsOf("SURFACE_CURVE")) != null && args.Count > 1 && args[1].Kind == ValKind.Ref)
                    consumed.Add(args[1].Ref);
                if ((args = entity.ArgsOf("SEAM_CURVE")) != null && args.Count > 1 && args[1].Kind == ValKind.Ref)
                    consumed.Add(args[1].Ref);
                if ((args = entity.ArgsOf("EDGE_CURVE")) != null && args.Count > 3 && args[3].Kind == ValKind.Ref)
                    consumed.Add(args[3].Ref);
            }
            return consumed;
        }

        private static bool IsCurve(StepEntity entity)
        {
            foreach (string type in _curveTypes) if (entity.Has(type)) return true;
            return false;
        }

        private static string GetCurveKindName(StepEntity entity, Dictionary<int, StepEntity> entities)
        {
            if (entity.Has("EDGE_CURVE"))
            {
                List<StepVal> args = entity.ArgsOf("EDGE_CURVE");
                StepEntity geometry;
                if (args != null && args.Count > 3 && args[3].Kind == ValKind.Ref &&
                    entities.TryGetValue(args[3].Ref, out geometry))
                    return "edge " + GetCurveKindName(geometry, entities);
                return "edge";
            }
            if (entity.Has("B_SPLINE_CURVE_WITH_KNOTS")) return "b-spline";
            if (entity.Has("LINE")) return "line";
            if (entity.Has("CIRCLE")) return "circle";
            if (entity.Has("ELLIPSE")) return "ellipse";
            if (entity.Has("POLYLINE")) return "polyline";
            if (entity.Has("TRIMMED_CURVE")) return "trimmed";
            if (entity.Has("COMPOSITE_CURVE")) return "composite";
            return entity.Type != null ? entity.Type.ToLowerInvariant() : "curve";
        }


        //
        // Tessellation
        //
        private static List<double[]> TessellateTopLevel(StepEntity entity, Dictionary<int, StepEntity> entities,
                                                         double tolerance)
        {
            if (entity.Has("EDGE_CURVE"))
            {
                List<StepVal> args = entity.ArgsOf("EDGE_CURVE");
                if (args == null || args.Count < 5) return null;
                double[] start = GetVertexPoint(args[1], entities);
                double[] end = GetVertexPoint(args[2], entities);
                bool sameSense = args[4].Kind != ValKind.Enum || args[4].Str == "T";
                StepEntity geometry;
                if (args[3].Kind != ValKind.Ref || !entities.TryGetValue(args[3].Ref, out geometry)) return null;
                List<double[]> points = TessellateCurve(geometry, entities, tolerance, start, end);
                if (points == null) return null;
                if (!sameSense) points.Reverse();
                return points;
            }
            return TessellateCurve(entity, entities, tolerance, null, null);
        }

        // startPoint/endPoint are the optional trim points coming from an edge curve
        private static List<double[]> TessellateCurve(StepEntity entity, Dictionary<int, StepEntity> entities,
                                                      double tolerance, double[] startPoint, double[] endPoint)
        {
            List<StepVal> args;
            //
            if ((args = entity.ArgsOf("POLYLINE")) != null)
            {
                if (args.Count < 2 || args[1].Kind != ValKind.List) return null;
                List<double[]> points = new List<double[]>();
                foreach (StepVal item in args[1].Items)
                {
                    double[] point = GetPoint(item, entities);
                    if (point != null) points.Add(point);
                }
                return points;
            }
            if ((args = entity.ArgsOf("LINE")) != null)
            {
                if (startPoint != null && endPoint != null)
                    return new List<double[]> { startPoint, endPoint };
                // An untrimmed line has no bounds - use the point and the vector magnitude
                if (args.Count < 3) return null;
                double[] origin = GetPoint(args[1], entities);
                double[] direction = GetVector(args[2], entities);
                if (origin == null || direction == null) return null;
                return new List<double[]>
                {
                    origin,
                    new double[] { origin[0] + direction[0], origin[1] + direction[1], origin[2] + direction[2] }
                };
            }
            if ((args = entity.ArgsOf("CIRCLE")) != null)
            {
                if (args.Count < 3) return null;
                Frame frame = GetFrame(args[1], entities);
                if (frame == null) return null;
                double radius = args[2].Num;
                return TessellateArc(frame, radius, radius, tolerance, startPoint, endPoint);
            }
            if ((args = entity.ArgsOf("ELLIPSE")) != null)
            {
                if (args.Count < 4) return null;
                Frame frame = GetFrame(args[1], entities);
                if (frame == null) return null;
                return TessellateArc(frame, args[2].Num, args[3].Num, tolerance, startPoint, endPoint);
            }
            if (entity.Has("B_SPLINE_CURVE_WITH_KNOTS") || entity.Has("BEZIER_CURVE"))
            {
                return TessellateBSpline(entity, entities, tolerance, double.NaN, double.NaN);
            }
            if ((args = entity.ArgsOf("TRIMMED_CURVE")) != null)
            {
                return TessellateTrimmed(entity, entities, tolerance);
            }
            if ((args = entity.ArgsOf("COMPOSITE_CURVE")) != null)
            {
                if (args.Count < 2 || args[1].Kind != ValKind.List) return null;
                List<double[]> points = new List<double[]>();
                foreach (StepVal item in args[1].Items)
                {
                    StepEntity segment;
                    if (item.Kind != ValKind.Ref || !entities.TryGetValue(item.Ref, out segment)) continue;
                    List<StepVal> segmentArgs = segment.ArgsOf("COMPOSITE_CURVE_SEGMENT");
                    if (segmentArgs == null || segmentArgs.Count < 3 || segmentArgs[2].Kind != ValKind.Ref) continue;
                    StepEntity parent;
                    if (!entities.TryGetValue(segmentArgs[2].Ref, out parent)) continue;
                    List<double[]> part = TessellateCurve(parent, entities, tolerance, null, null);
                    if (part == null || part.Count == 0) continue;
                    bool sameSense = segmentArgs[1].Kind != ValKind.Enum || segmentArgs[1].Str == "T";
                    if (!sameSense) part.Reverse();
                    if (points.Count > 0 && Distance(points[points.Count - 1], part[0]) < 1E-9) part.RemoveAt(0);
                    points.AddRange(part);
                }
                return points;
            }
            return null;
        }

        private static List<double[]> TessellateTrimmed(StepEntity entity, Dictionary<int, StepEntity> entities,
                                                        double tolerance)
        {
            List<StepVal> args = entity.ArgsOf("TRIMMED_CURVE");
            if (args == null || args.Count < 5 || args[1].Kind != ValKind.Ref) return null;
            StepEntity basis;
            if (!entities.TryGetValue(args[1].Ref, out basis)) return null;
            //
            double parameter1, parameter2;
            double[] point1, point2;
            GetTrim(args.Count > 2 ? args[2] : null, entities, out parameter1, out point1);
            GetTrim(args.Count > 3 ? args[3] : null, entities, out parameter2, out point2);
            bool senseAgreement = args[4].Kind != ValKind.Enum || args[4].Str == "T";
            bool hasParameters = !double.IsNaN(parameter1) && !double.IsNaN(parameter2);
            //
            // A line and a conic are unbounded: the trim values must be evaluated on the curve to
            // obtain the end points of the trimmed part
            List<StepVal> lineArgs = basis.ArgsOf("LINE");
            if (lineArgs != null && lineArgs.Count > 2 && hasParameters && point1 == null && point2 == null)
            {
                double[] origin = GetPoint(lineArgs[1], entities);
                double[] vector = GetVector(lineArgs[2], entities);
                if (origin != null && vector != null)
                {
                    point1 = PointOnLine(origin, vector, parameter1);
                    point2 = PointOnLine(origin, vector, parameter2);
                }
            }
            List<StepVal> conicArgs = basis.ArgsOf("CIRCLE") ?? basis.ArgsOf("ELLIPSE");
            if (conicArgs != null && conicArgs.Count > 2 && hasParameters && point1 == null && point2 == null)
            {
                Frame frame = GetFrame(conicArgs[1], entities);
                if (frame != null)
                {
                    double radius1 = conicArgs[2].Num;
                    double radius2 = conicArgs.Count > 3 ? conicArgs[3].Num : radius1;
                    // The trim parameter of a conic is an angle in degrees
                    point1 = PointOnArc(frame, radius1, radius2, parameter1 * Math.PI / 180.0);
                    point2 = PointOnArc(frame, radius1, radius2, parameter2 * Math.PI / 180.0);
                }
            }
            //
            List<double[]> points;
            if (basis.Has("B_SPLINE_CURVE_WITH_KNOTS") || basis.Has("BEZIER_CURVE"))
                points = TessellateBSpline(basis, entities, tolerance, parameter1, parameter2);
            else
                points = TessellateCurve(basis, entities, tolerance, point1, point2);
            //
            if (points != null && !senseAgreement) points.Reverse();
            return points;
        }

        private static double[] PointOnLine(double[] origin, double[] vector, double parameter)
        {
            return new double[]
            {
                origin[0] + parameter * vector[0],
                origin[1] + parameter * vector[1],
                origin[2] + parameter * vector[2]
            };
        }

        private static double[] PointOnArc(Frame frame, double radius1, double radius2, double angle)
        {
            double c = radius1 * Math.Cos(angle);
            double s = radius2 * Math.Sin(angle);
            return new double[]
            {
                frame.Origin[0] + c * frame.X.X + s * frame.Y.X,
                frame.Origin[1] + c * frame.X.Y + s * frame.Y.Y,
                frame.Origin[2] + c * frame.X.Z + s * frame.Y.Z
            };
        }

        private static void GetTrim(StepVal value, Dictionary<int, StepEntity> entities,
                                    out double parameter, out double[] point)
        {
            parameter = double.NaN;
            point = null;
            if (value == null) return;
            List<StepVal> items = value.Kind == ValKind.List ? value.Items : new List<StepVal> { value };
            foreach (StepVal item in items)
            {
                if (item.Kind == ValKind.Typed && item.TypeName == "PARAMETER_VALUE" &&
                    item.Items != null && item.Items.Count > 0)
                    parameter = item.Items[0].Num;
                else if (item.Kind == ValKind.Ref)
                {
                    double[] candidate = GetPoint(item, entities);
                    if (candidate != null) point = candidate;
                }
                else if (item.Kind == ValKind.Num) parameter = item.Num;
            }
        }

        private static List<double[]> TessellateArc(Frame frame, double radius1, double radius2, double tolerance,
                                                    double[] startPoint, double[] endPoint)
        {
            double angle0 = 0;
            double sweep = 2 * Math.PI;
            if (startPoint != null && endPoint != null)
            {
                angle0 = GetAngle(frame, startPoint);
                double angle1 = GetAngle(frame, endPoint);
                sweep = angle1 - angle0;
                while (sweep < 0) sweep += 2 * Math.PI;
                if (sweep < 1E-9) sweep = 2 * Math.PI;         // closed circle
            }
            double radius = Math.Max(Math.Abs(radius1), Math.Abs(radius2));
            int count = 16;
            if (radius > 1E-12 && tolerance > 0 && tolerance < radius)
            {
                double maxAngle = 2 * Math.Acos(1 - tolerance / radius);
                if (maxAngle > 1E-9) count = (int)Math.Ceiling(Math.Abs(sweep) / maxAngle);
            }
            count = Math.Max(8, Math.Min(2000, count));
            //
            List<double[]> points = new List<double[]>(count + 1);
            for (int i = 0; i <= count; i++)
            {
                double t = angle0 + sweep * i / count;
                double c = radius1 * Math.Cos(t);
                double s = radius2 * Math.Sin(t);
                points.Add(new double[]
                {
                    frame.Origin[0] + c * frame.X.X + s * frame.Y.X,
                    frame.Origin[1] + c * frame.X.Y + s * frame.Y.Y,
                    frame.Origin[2] + c * frame.X.Z + s * frame.Y.Z
                });
            }
            return points;
        }

        private static double GetAngle(Frame frame, double[] point)
        {
            double dx = point[0] - frame.Origin[0];
            double dy = point[1] - frame.Origin[1];
            double dz = point[2] - frame.Origin[2];
            double u = dx * frame.X.X + dy * frame.X.Y + dz * frame.X.Z;
            double v = dx * frame.Y.X + dy * frame.Y.Y + dz * frame.Y.Z;
            return Math.Atan2(v, u);
        }


        //
        // B-spline evaluation
        //
        private class BSpline
        {
            public int Degree;
            public double[][] ControlPoints;
            public double[] Weights;
            public double[] Knots;              // expanded knot vector

            public double[] Evaluate(double u)
            {
                int n = ControlPoints.Length - 1;
                int span = FindSpan(n, Degree, u);
                double[] basis = BasisFunctions(span, u);
                double x = 0, y = 0, z = 0, w = 0;
                for (int i = 0; i <= Degree; i++)
                {
                    int index = span - Degree + i;
                    double weight = Weights != null ? Weights[index] : 1.0;
                    double factor = basis[i] * weight;
                    x += factor * ControlPoints[index][0];
                    y += factor * ControlPoints[index][1];
                    z += factor * ControlPoints[index][2];
                    w += factor;
                }
                if (Math.Abs(w) < 1E-14) return new double[] { x, y, z };
                return new double[] { x / w, y / w, z / w };
            }

            private int FindSpan(int n, int degree, double u)
            {
                if (u >= Knots[n + 1]) return n;
                if (u <= Knots[degree]) return degree;
                int low = degree, high = n + 1, mid = (low + high) / 2;
                while (u < Knots[mid] || u >= Knots[mid + 1])
                {
                    if (u < Knots[mid]) high = mid;
                    else low = mid;
                    mid = (low + high) / 2;
                }
                return mid;
            }

            private double[] BasisFunctions(int span, double u)
            {
                double[] basis = new double[Degree + 1];
                double[] left = new double[Degree + 1];
                double[] right = new double[Degree + 1];
                basis[0] = 1.0;
                for (int j = 1; j <= Degree; j++)
                {
                    left[j] = u - Knots[span + 1 - j];
                    right[j] = Knots[span + j] - u;
                    double saved = 0.0;
                    for (int r = 0; r < j; r++)
                    {
                        double denominator = right[r + 1] + left[j - r];
                        double temp = Math.Abs(denominator) < 1E-14 ? 0.0 : basis[r] / denominator;
                        basis[r] = saved + right[r + 1] * temp;
                        saved = left[j - r] * temp;
                    }
                    basis[j] = saved;
                }
                return basis;
            }
        }

        private static List<double[]> TessellateBSpline(StepEntity entity, Dictionary<int, StepEntity> entities,
                                                        double tolerance, double from, double to)
        {
            BSpline spline = BuildBSpline(entity, entities);
            if (spline == null) return null;
            //
            double start = spline.Knots[spline.Degree];
            double end = spline.Knots[spline.ControlPoints.Length];
            if (!double.IsNaN(from) && from > start && from < end) start = from;
            if (!double.IsNaN(to) && to > start && to < end) end = to;
            if (end - start < 1E-12) return null;
            //
            // Sample every knot span and refine each interval until the chord error is small
            List<double> parameters = new List<double> { start };
            foreach (double knot in spline.Knots)
                if (knot > start + 1E-12 && knot < end - 1E-12 && knot > parameters[parameters.Count - 1] + 1E-12)
                    parameters.Add(knot);
            parameters.Add(end);
            //
            List<double[]> points = new List<double[]> { spline.Evaluate(start) };
            for (int i = 0; i < parameters.Count - 1; i++)
            {
                double u0 = parameters[i];
                double u1 = parameters[i + 1];
                Refine(spline, u0, u1, spline.Evaluate(u0), spline.Evaluate(u1), tolerance, 0, points);
            }
            return points;
        }

        private static void Refine(BSpline spline, double u0, double u1, double[] p0, double[] p1,
                                   double tolerance, int depth, List<double[]> points)
        {
            double um = 0.5 * (u0 + u1);
            double[] pm = spline.Evaluate(um);
            double[] chordMid = new double[] { 0.5 * (p0[0] + p1[0]), 0.5 * (p0[1] + p1[1]), 0.5 * (p0[2] + p1[2]) };
            bool flat = Distance(pm, chordMid) <= tolerance;
            if ((flat && depth >= 2) || depth >= 12)
            {
                points.Add(pm);
                points.Add(p1);
                return;
            }
            Refine(spline, u0, um, p0, pm, tolerance, depth + 1, points);
            Refine(spline, um, u1, pm, p1, tolerance, depth + 1, points);
        }

        private static BSpline BuildBSpline(StepEntity entity, Dictionary<int, StepEntity> entities)
        {
            List<StepVal> baseArgs = entity.ArgsOf("B_SPLINE_CURVE_WITH_KNOTS");
            List<StepVal> knotArgs = baseArgs;
            int degreeIndex, controlIndex, multiplicityIndex, knotIndex;
            //
            if (entity.Type == "B_SPLINE_CURVE_WITH_KNOTS")
            {
                // name, degree, control_points, form, closed, self_intersect, multiplicities, knots, spec
                degreeIndex = 1; controlIndex = 2; multiplicityIndex = 6; knotIndex = 7;
            }
            else
            {
                // Complex instance: the degree and the control points live in B_SPLINE_CURVE
                baseArgs = entity.ArgsOf("B_SPLINE_CURVE") ?? entity.ArgsOf("BEZIER_CURVE");
                knotArgs = entity.ArgsOf("B_SPLINE_CURVE_WITH_KNOTS");
                if (baseArgs == null || knotArgs == null) return null;
                degreeIndex = 0; controlIndex = 1; multiplicityIndex = 0; knotIndex = 1;
            }
            if (baseArgs == null || knotArgs == null) return null;
            if (baseArgs.Count <= controlIndex || knotArgs.Count <= knotIndex) return null;
            //
            int degree = (int)Math.Round(baseArgs[degreeIndex].Num);
            if (degree < 1) return null;
            //
            if (baseArgs[controlIndex].Kind != ValKind.List) return null;
            List<double[]> controlPoints = new List<double[]>();
            foreach (StepVal item in baseArgs[controlIndex].Items)
            {
                double[] point = GetPoint(item, entities);
                if (point == null) return null;
                controlPoints.Add(point);
            }
            if (controlPoints.Count <= degree) return null;
            //
            if (knotArgs[multiplicityIndex].Kind != ValKind.List || knotArgs[knotIndex].Kind != ValKind.List)
                return null;
            List<double> knots = new List<double>();
            List<StepVal> multiplicities = knotArgs[multiplicityIndex].Items;
            List<StepVal> knotValues = knotArgs[knotIndex].Items;
            int count = Math.Min(multiplicities.Count, knotValues.Count);
            for (int i = 0; i < count; i++)
            {
                int multiplicity = (int)Math.Round(multiplicities[i].Num);
                for (int m = 0; m < multiplicity; m++) knots.Add(knotValues[i].Num);
            }
            if (knots.Count != controlPoints.Count + degree + 1)
            {
                // Some exporters clamp the end knots implicitly; pad or trim to the required size
                while (knots.Count < controlPoints.Count + degree + 1) knots.Add(knots[knots.Count - 1]);
                while (knots.Count > controlPoints.Count + degree + 1) knots.RemoveAt(knots.Count - 1);
            }
            //
            double[] weights = null;
            List<StepVal> rationalArgs = entity.ArgsOf("RATIONAL_B_SPLINE_CURVE");
            if (rationalArgs != null && rationalArgs.Count > 0 && rationalArgs[0].Kind == ValKind.List)
            {
                weights = new double[controlPoints.Count];
                for (int i = 0; i < controlPoints.Count; i++)
                    weights[i] = i < rationalArgs[0].Items.Count ? rationalArgs[0].Items[i].Num : 1.0;
            }
            //
            BSpline spline = new BSpline();
            spline.Degree = degree;
            spline.ControlPoints = controlPoints.ToArray();
            spline.Weights = weights;
            spline.Knots = knots.ToArray();
            return spline;
        }


        //
        // Helpers
        //
        private static double[] GetPoint(StepVal value, Dictionary<int, StepEntity> entities)
        {
            if (value == null || value.Kind != ValKind.Ref) return null;
            StepEntity entity;
            if (!entities.TryGetValue(value.Ref, out entity)) return null;
            List<StepVal> args = entity.ArgsOf("CARTESIAN_POINT");
            if (args == null || args.Count < 2 || args[1].Kind != ValKind.List) return null;
            List<StepVal> coordinates = args[1].Items;
            return new double[]
            {
                coordinates.Count > 0 ? coordinates[0].Num : 0,
                coordinates.Count > 1 ? coordinates[1].Num : 0,
                coordinates.Count > 2 ? coordinates[2].Num : 0
            };
        }

        private static double[] GetVertexPoint(StepVal value, Dictionary<int, StepEntity> entities)
        {
            if (value == null || value.Kind != ValKind.Ref) return null;
            StepEntity entity;
            if (!entities.TryGetValue(value.Ref, out entity)) return null;
            List<StepVal> args = entity.ArgsOf("VERTEX_POINT");
            if (args == null || args.Count < 2) return null;
            return GetPoint(args[1], entities);
        }

        private static double[] GetDirection(StepVal value, Dictionary<int, StepEntity> entities)
        {
            if (value == null || value.Kind != ValKind.Ref) return null;
            StepEntity entity;
            if (!entities.TryGetValue(value.Ref, out entity)) return null;
            List<StepVal> args = entity.ArgsOf("DIRECTION");
            if (args == null || args.Count < 2 || args[1].Kind != ValKind.List) return null;
            List<StepVal> ratios = args[1].Items;
            return new double[]
            {
                ratios.Count > 0 ? ratios[0].Num : 0,
                ratios.Count > 1 ? ratios[1].Num : 0,
                ratios.Count > 2 ? ratios[2].Num : 0
            };
        }

        private static double[] GetVector(StepVal value, Dictionary<int, StepEntity> entities)
        {
            if (value == null || value.Kind != ValKind.Ref) return null;
            StepEntity entity;
            if (!entities.TryGetValue(value.Ref, out entity)) return null;
            List<StepVal> args = entity.ArgsOf("VECTOR");
            if (args == null || args.Count < 3) return null;
            double[] direction = GetDirection(args[1], entities);
            if (direction == null) return null;
            double magnitude = args[2].Num;
            return new double[] { direction[0] * magnitude, direction[1] * magnitude, direction[2] * magnitude };
        }

        private static Frame GetFrame(StepVal value, Dictionary<int, StepEntity> entities)
        {
            if (value == null || value.Kind != ValKind.Ref) return null;
            StepEntity entity;
            if (!entities.TryGetValue(value.Ref, out entity)) return null;
            List<StepVal> args = entity.ArgsOf("AXIS2_PLACEMENT_3D");
            if (args == null || args.Count < 2) return null;
            //
            double[] origin = GetPoint(args[1], entities);
            if (origin == null) return null;
            double[] axis = args.Count > 2 ? GetDirection(args[2], entities) : null;
            double[] reference = args.Count > 3 ? GetDirection(args[3], entities) : null;
            //
            Vec3D z = axis != null ? new Vec3D(axis) : new Vec3D(0, 0, 1);
            if (z.Normalize() < 1E-12) z = new Vec3D(0, 0, 1);
            Vec3D x = reference != null ? new Vec3D(reference) : GetAnyPerpendicular(z);
            double dot = Vec3D.DotProduct(x, z);
            x = new Vec3D(x.X - dot * z.X, x.Y - dot * z.Y, x.Z - dot * z.Z);
            if (x.Normalize() < 1E-12) x = GetAnyPerpendicular(z);
            Vec3D y = Vec3D.CrossProduct(z, x);
            y.Normalize();
            //
            Frame frame = new Frame();
            frame.Origin = origin;
            frame.X = x;
            frame.Y = y;
            frame.Z = z;
            return frame;
        }

        private static Vec3D GetAnyPerpendicular(Vec3D v)
        {
            Vec3D candidate = Math.Abs(v.X) < 0.9 ? new Vec3D(1, 0, 0) : new Vec3D(0, 1, 0);
            double dot = Vec3D.DotProduct(candidate, v);
            Vec3D result = new Vec3D(candidate.X - dot * v.X, candidate.Y - dot * v.Y, candidate.Z - dot * v.Z);
            result.Normalize();
            return result;
        }

        private static double Distance(double[] a, double[] b)
        {
            double dx = a[0] - b[0], dy = a[1] - b[1], dz = a[2] - b[2];
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private static List<double[]> CleanUp(List<double[]> points)
        {
            List<double[]> result = new List<double[]>(points.Count);
            foreach (double[] point in points)
            {
                if (point == null) continue;
                if (result.Count > 0 && Distance(result[result.Count - 1], point) < 1E-12) continue;
                result.Add(point);
            }
            return result;
        }

        private static double ComputeAutoTolerance(Dictionary<int, StepEntity> entities, List<int> curveIds)
        {
            double[] min = new double[] { double.MaxValue, double.MaxValue, double.MaxValue };
            double[] max = new double[] { double.MinValue, double.MinValue, double.MinValue };
            bool any = false;
            foreach (var entry in entities)
            {
                List<StepVal> args = entry.Value.ArgsOf("CARTESIAN_POINT");
                if (args == null || args.Count < 2 || args[1].Kind != ValKind.List) continue;
                List<StepVal> coordinates = args[1].Items;
                if (coordinates.Count < 3) continue;
                for (int i = 0; i < 3; i++)
                {
                    double value = coordinates[i].Num;
                    if (value < min[i]) min[i] = value;
                    if (value > max[i]) max[i] = value;
                }
                any = true;
            }
            if (!any) return 1E-3;
            double diagonal = Math.Sqrt((max[0] - min[0]) * (max[0] - min[0]) +
                                        (max[1] - min[1]) * (max[1] - min[1]) +
                                        (max[2] - min[2]) * (max[2] - min[2]));
            if (diagonal < 1E-12) return 1E-3;
            return diagonal / 2000.0;
        }
    }
}
