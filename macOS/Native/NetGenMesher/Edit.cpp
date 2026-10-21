// PrePoMax for macOS - NetGenMesher: splitting, offsetting, fusing and repairing CAD parts.
//
//   BREP_SPLIT | BREP_SPLIT_PREVIEW <in> <out> <tool file>
//       the solids are cut into cells (or, with "Split target: Shell", only the target faces are divided); <out>.origins
//       names the solid every cell was cut from; the preview writes the edges of the cut as "Edge: <n>" polylines
//   BREP_SPLIT_A_FACE_USING_TWO_POINTS <in> <out> <face id> <x1 y1 z1> <x2 y2 z2>
//   BREP_OFFSET_FACES <in> <out> <offset> <groups> (<solid> <n> <face ids>)...
//   BREP_FUSE_FACES <in> <out> <groups> (<solid> <n> <face ids>)...
//   BREP_REPAIR | BREP_REPAIR_PREVIEW <in> <out> <options> <tolerance> [number of split faces]
//       options: 1 fix shape, 2 small edges, 4 small faces, 8 sew, 16 fuse faces, 32 join edges; the preview writes
//       what the repair takes away: every edge as "Edge: <n>" polyline and every vertex as "Point: x y z"
#include "Common.h"

#include <BOPAlgo_Builder.hxx>
#include <BRepAdaptor_Curve.hxx>
#include <BRepAdaptor_Surface.hxx>
#include <BRepAlgoAPI_Section.hxx>
#include <BRepAlgoAPI_Splitter.hxx>
#include <BRepBndLib.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepBuilderAPI_Sewing.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepFeat_SplitShape.hxx>
#include <BRepLProp_SLProps.hxx>
#include <BRepOffset_MakeOffset.hxx>
#include <BRepOffsetAPI_MakeOffsetShape.hxx>
#include <BRepBuilderAPI_Copy.hxx>
#include <BRepBuilderAPI_MakeVertex.hxx>
#include <BRepExtrema_DistShapeShape.hxx>
#include <BRepPrimAPI_MakePrism.hxx>
#include <BRepTools.hxx>
#include <BRep_Builder.hxx>
#include <BRep_Tool.hxx>
#include <Bnd_Box.hxx>
#include <GCPnts_QuasiUniformDeflection.hxx>
#include <GeomAPI_ProjectPointOnSurf.hxx>
#include <GeomLib.hxx>
#include <Geom_OffsetSurface.hxx>
#include <Geom_Surface.hxx>
#include <ShapeFix_FixSmallFace.hxx>
#include <ShapeFix_Shape.hxx>
#include <ShapeFix_Wireframe.hxx>
#include <ShapeUpgrade_UnifySameDomain.hxx>
#include <TopExp.hxx>
#include <TopExp_Explorer.hxx>
#include <TopTools_IndexedDataMapOfShapeListOfShape.hxx>
#include <TopTools_ListOfShape.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Compound.hxx>
#include <gp_Pln.hxx>

#include <cmath>
#include <fstream>
#include <map>
#include <set>
#include <sstream>

namespace ngm
{
    namespace
    {
        struct SplitTool
        {
            std::string toolType;
            std::vector<double> extent, planeOrigin, planeNormal, point1, point2, extrusionOffset, extrusionVector;
            double fuzzy = 0, offset = 0;
            std::vector<int> solidIds;
            bool shellTarget = false;
            std::vector<std::pair<int, std::vector<int>>> targets;          // solid, face ids
            std::vector<std::pair<std::string, std::vector<int>>> faceIds;  // source file, ids
            std::vector<std::pair<std::string, std::vector<int>>> edgeIds;
        };
        std::vector<double> Numbers(const std::string& text)
        {
            std::vector<double> values;
            std::istringstream in(text);
            double value;
            while (in >> value) values.push_back(value);
            return values;
        }
        std::vector<int> Ints(const std::string& text)
        {
            std::vector<int> values;
            for (double v : Numbers(text)) values.push_back((int)v);
            return values;
        }
        bool ReadTool(const std::string& fileName, SplitTool& tool)
        {
            std::ifstream file(fileName);
            if (!file) { Error("The split tool file " + fileName + " could not be read."); return false; }
            std::string line, source;
            while (std::getline(file, line))
            {
                if (!line.empty() && line.back() == '\r') line.pop_back();
                size_t colon = line.find(':');
                if (colon == std::string::npos) continue;
                std::string key = line.substr(0, colon), value = line.substr(colon + 1);
                size_t start = value.find_first_not_of(' ');
                std::string text = start == std::string::npos ? "" : value.substr(start);
                if (key == "Tool type") tool.toolType = text;
                else if (key == "Extent") tool.extent = Numbers(text);
                else if (key == "Fuzzy value") { auto v = Numbers(text); if (!v.empty()) tool.fuzzy = v[0]; }
                else if (key == "Solid ids") tool.solidIds = Ints(text);
                else if (key == "Split target") tool.shellTarget = text == "Shell";
                else if (key == "Target solid") tool.targets.push_back({ std::stoi(text), {} });
                else if (key == "Target face ids" && !tool.targets.empty()) tool.targets.back().second = Ints(text);
                else if (key == "Plane origin") tool.planeOrigin = Numbers(text);
                else if (key == "Plane normal") tool.planeNormal = Numbers(text);
                else if (key == "Point 1") tool.point1 = Numbers(text);
                else if (key == "Point 2") tool.point2 = Numbers(text);
                else if (key == "Source file") source = text;
                else if (key == "Face ids") tool.faceIds.push_back({ source, Ints(text) });
                else if (key == "Edge ids") tool.edgeIds.push_back({ source, Ints(text) });
                else if (key == "Offset") { auto v = Numbers(text); if (!v.empty()) tool.offset = v[0]; }
                else if (key == "Extrusion offset") tool.extrusionOffset = Numbers(text);
                else if (key == "Extrusion vector") tool.extrusionVector = Numbers(text);
            }
            return true;
        }
        double ToolSize(const SplitTool& tool, const TopoDS_Shape& shape)
        {
            Bnd_Box box;
            BRepBndLib::Add(shape, box);
            double size = box.IsVoid() ? 1 : std::sqrt(box.SquareExtent());
            if (tool.extent.size() == 6)
                size = std::max(size, std::sqrt(std::pow(tool.extent[1] - tool.extent[0], 2) +
                                                std::pow(tool.extent[3] - tool.extent[2], 2) +
                                                std::pow(tool.extent[5] - tool.extent[4], 2)));
            return 2 * size + 1;
        }
        gp_Pnt P(const std::vector<double>& v) { return gp_Pnt(v[0], v[1], v[2]); }
        // The cutting shape of the tool, large enough to pass through the whole part
        TopoDS_Shape MakeToolShape(const SplitTool& tool, const TopoDS_Shape& shape)
        {
            double size = ToolSize(tool, shape);
            BRep_Builder builder;
            TopoDS_Compound compound;
            builder.MakeCompound(compound);
            if (tool.toolType == "Plane" && tool.planeOrigin.size() == 3 && tool.planeNormal.size() == 3)
            {
                gp_Pln plane(P(tool.planeOrigin), gp_Dir(tool.planeNormal[0], tool.planeNormal[1], tool.planeNormal[2]));
                return BRepBuilderAPI_MakeFace(plane, -size, size, -size, size).Face();
            }
            if (tool.toolType == "ExtendFace")
            {
                for (auto& group : tool.faceIds)
                {
                    TopoDS_Shape source;
                    if (!ReadShape(group.first, source)) continue;
                    TopTools_IndexedMapOfShape faces;
                    TopExp::MapShapes(source, TopAbs_FACE, faces);
                    for (int id : group.second)
                    {
                        if (id < 1 || id > faces.Extent()) continue;
                        TopoDS_Face face = TopoDS::Face(faces(id));
                        Handle(Geom_Surface) surface = BRep_Tool::Surface(face);
                        if (tool.offset != 0)
                        {
                            double offset = face.Orientation() == TopAbs_REVERSED ? -tool.offset : tool.offset;
                            surface = new Geom_OffsetSurface(surface, offset);
                        }
                        // The surface continued past the boundary of the face
                        double u1, u2, v1, v2;
                        BRepTools::UVBounds(face, u1, u2, v1, v2);
                        double du = u2 - u1, dv = v2 - v1;
                        BRepAdaptor_Surface adaptor(face);
                        bool uPeriodic = adaptor.IsUPeriodic(), vPeriodic = adaptor.IsVPeriodic();
                        double scale = size / std::max(1e-9, std::sqrt(du * du + dv * dv));
                        if (adaptor.GetType() != GeomAbs_Plane) scale = std::min(scale, 3.0);
                        double eu = uPeriodic ? 0 : du * scale, ev = vPeriodic ? 0 : dv * scale;
                        if (uPeriodic) { u1 = adaptor.FirstUParameter(); u2 = adaptor.LastUParameter(); eu = 0; }
                        if (vPeriodic) { v1 = adaptor.FirstVParameter(); v2 = adaptor.LastVParameter(); ev = 0; }
                        BRepBuilderAPI_MakeFace maker(surface, u1 - eu, u2 + eu, v1 - ev, v2 + ev, 1e-7);
                        if (maker.IsDone()) builder.Add(compound, maker.Face());
                    }
                }
                return compound;
            }
            if (tool.toolType == "ExtrudeEdges" && tool.extrusionVector.size() == 3)
            {
                gp_Vec vector(tool.extrusionVector[0], tool.extrusionVector[1], tool.extrusionVector[2]);
                gp_Vec offset = tool.extrusionOffset.size() == 3 ?
                                gp_Vec(tool.extrusionOffset[0], tool.extrusionOffset[1], tool.extrusionOffset[2]) : gp_Vec();
                for (auto& group : tool.edgeIds)
                {
                    TopoDS_Shape source;
                    if (!ReadShape(group.first, source)) continue;
                    TopTools_IndexedMapOfShape edges;
                    TopExp::MapShapes(source, TopAbs_EDGE, edges);
                    for (int id : group.second)
                    {
                        if (id < 1 || id > edges.Extent()) continue;
                        gp_Trsf move;
                        move.SetTranslation(offset);
                        TopoDS_Shape edge = BRepBuilderAPI_Transform(edges(id), move, true).Shape();
                        BRepPrimAPI_MakePrism prism(edge, vector);
                        if (prism.IsDone()) builder.Add(compound, prism.Shape());
                    }
                }
                return compound;
            }
            return TopoDS_Shape();
        }
        // The line through two points drawn on a face: the sheet the line sweeps along the normal of the face. The line
        // runs across the whole face, so the cut reaches its boundary and divides it
        TopoDS_Shape MakeLineTool(const gp_Pnt& a, const gp_Pnt& b, const TopoDS_Face& face, double size)
        {
            gp_Pnt middle((a.XYZ() + b.XYZ()) / 2);
            Handle(Geom_Surface) surface = BRep_Tool::Surface(face);
            GeomAPI_ProjectPointOnSurf projector(middle, surface);
            gp_Dir normal(0, 0, 1);
            if (projector.NbPoints() > 0)
            {
                double u, v;
                projector.LowerDistanceParameters(u, v);
                BRepAdaptor_Surface adaptor(face, false);
                BRepLProp_SLProps props(adaptor, u, v, 1, 1e-9);
                if (props.IsNormalDefined()) normal = props.Normal();
            }
            gp_Vec along(a, b);
            if (along.Magnitude() < 1e-12) return TopoDS_Shape();
            gp_Pnt a2 = a.Translated(-along.Normalized() * size), b2 = b.Translated(along.Normalized() * size);
            TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(a2.Translated(-gp_Vec(normal) * size), b2.Translated(-gp_Vec(normal) * size));
            BRepPrimAPI_MakePrism prism(edge, gp_Vec(normal) * 2 * size);
            return prism.Shape();
        }
        // Edges as polylines of the preview
        void WriteEdges(std::ostream& out, const TopoDS_Shape& shape, double deflection)
        {
            for (TopExp_Explorer ex(shape, TopAbs_EDGE); ex.More(); ex.Next())
            {
                TopoDS_Edge edge = TopoDS::Edge(ex.Current());
                if (BRep_Tool::Degenerated(edge)) continue;
                BRepAdaptor_Curve curve(edge);
                GCPnts_QuasiUniformDeflection points(curve, deflection);
                if (!points.IsDone() || points.NbPoints() < 2) continue;
                out << "Edge: " << points.NbPoints() << "\n";
                for (int i = 1; i <= points.NbPoints(); i++)
                {
                    gp_Pnt p = points.Value(i);
                    out << Num(p.X()) << " " << Num(p.Y()) << " " << Num(p.Z()) << "\n";
                }
            }
        }
        double Deflection(const TopoDS_Shape& shape)
        {
            Bnd_Box box;
            BRepBndLib::Add(shape, box);
            return box.IsVoid() ? 0.01 : std::sqrt(box.SquareExtent()) * 0.002;
        }

        // The faces of the body divided by the tool, the body kept whole
        TopoDS_Shape SplitFaces(const TopoDS_Shape& body, const std::vector<TopoDS_Face>& faces, const TopoDS_Shape& tool,
                                TopoDS_Compound& cutEdges)
        {
            BRep_Builder builder;
            builder.MakeCompound(cutEdges);
            BRepFeat_SplitShape splitter(body);
            int added = 0;
            for (const TopoDS_Face& face : faces)
            {
                BRepAlgoAPI_Section section(face, tool, false);
                section.ComputePCurveOn1(true);
                section.Approximation(true);
                section.Build();
                if (!section.IsDone()) continue;
                for (TopExp_Explorer ex(section.Shape(), TopAbs_EDGE); ex.More(); ex.Next())
                {
                    splitter.Add(TopoDS::Edge(ex.Current()), face);
                    builder.Add(cutEdges, ex.Current());
                    added++;
                }
            }
            if (added == 0) return body;
            splitter.Build();
            return splitter.IsDone() ? splitter.Shape() : TopoDS_Shape();
        }

        int Split(const std::vector<std::string>& args, bool preview)
        {
            if (args.size() < 3) { Error("BREP_SPLIT needs an input, an output and a tool file."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            SplitTool tool;
            if (!ReadTool(args[2], tool)) return 1;
            TopTools_IndexedMapOfShape solids;
            TopExp::MapShapes(shape, TopAbs_SOLID, solids);
            double size = ToolSize(tool, shape);
            BRep_Builder builder;
            //
            if (tool.shellTarget || tool.toolType == "TwoPointLine")
            {
                // Only the named faces are divided; the solid 0 is the shape itself
                TopoDS_Shape result = shape;
                TopoDS_Compound allEdges;
                builder.MakeCompound(allEdges);
                for (auto& target : tool.targets)
                {
                    TopoDS_Shape body = target.first == 0 || solids.Extent() == 0 ? result :
                                        (target.first <= solids.Extent() ? solids(target.first) : TopoDS_Shape());
                    if (body.IsNull()) continue;
                    TopTools_IndexedMapOfShape faces;
                    TopExp::MapShapes(body, TopAbs_FACE, faces);
                    std::vector<TopoDS_Face> targetFaces;
                    for (int id : target.second)
                        if (id >= 1 && id <= faces.Extent()) targetFaces.push_back(TopoDS::Face(faces(id)));
                    if (targetFaces.empty()) continue;
                    TopoDS_Shape cutter = tool.toolType == "TwoPointLine" && tool.point1.size() == 3 && tool.point2.size() == 3 ?
                                          MakeLineTool(P(tool.point1), P(tool.point2), targetFaces[0], size) :
                                          MakeToolShape(tool, shape);
                    if (cutter.IsNull()) { Error("The split tool could not be made."); return 1; }
                    TopoDS_Compound edges;
                    TopoDS_Shape divided = SplitFaces(body, targetFaces, cutter, edges);
                    if (divided.IsNull()) { Error("The faces could not be divided."); return 1; }
                    builder.Add(allEdges, edges);
                    if (target.first == 0 || solids.Extent() == 0) result = divided;
                    else
                    {
                        // The solid in the compound replaced by the divided one
                        TopoDS_Compound compound;
                        builder.MakeCompound(compound);
                        for (int s = 1; s <= solids.Extent(); s++)
                            builder.Add(compound, s == target.first ? divided : solids(s));
                        result = compound;
                        TopTools_IndexedMapOfShape newSolids;
                        TopExp::MapShapes(result, TopAbs_SOLID, newSolids);
                        solids = newSolids;
                    }
                }
                if (preview)
                {
                    std::ofstream out(args[1]);
                    WriteEdges(out, allEdges, Deflection(shape));
                    return 0;
                }
                if (!WriteBrep(result, args[1])) return 1;
                TopTools_IndexedMapOfShape resultSolids;
                TopExp::MapShapes(result, TopAbs_SOLID, resultSolids);
                std::ofstream origins(args[1] + ".origins");
                for (int s = 1; s <= resultSolids.Extent(); s++) origins << s << "\n";
                return 0;
            }
            // The solids are cut into cells
            TopoDS_Shape cutter = MakeToolShape(tool, shape);
            if (cutter.IsNull()) { Error("The split tool " + tool.toolType + " could not be made."); return 1; }
            std::set<int> selected(tool.solidIds.begin(), tool.solidIds.end());
            TopTools_ListOfShape arguments, tools;
            std::vector<TopoDS_Shape> sourceSolids;
            for (int s = 1; s <= solids.Extent(); s++)
            {
                sourceSolids.push_back(solids(s));
                if (selected.empty() || selected.count(s)) arguments.Append(solids(s));
            }
            if (solids.Extent() == 0) { arguments.Append(shape); sourceSolids.push_back(shape); }
            tools.Append(cutter);
            if (preview)
            {
                std::ofstream out(args[1]);
                for (TopTools_ListIteratorOfListOfShape it(arguments); it.More(); it.Next())
                {
                    BRepAlgoAPI_Section section(it.Value(), cutter);
                    if (section.IsDone()) WriteEdges(out, section.Shape(), Deflection(shape));
                }
                return 0;
            }
            BRepAlgoAPI_Splitter splitter;
            splitter.SetArguments(arguments);
            splitter.SetTools(tools);
            if (tool.fuzzy > 0) splitter.SetFuzzyValue(tool.fuzzy);
            splitter.SetRunParallel(true);
            splitter.Build();
            if (!splitter.IsDone() || splitter.HasErrors()) { Error("The split did not succeed."); return 1; }
            // The cells and the solids that were not cut, each with the solid it came from
            TopoDS_Compound result;
            builder.MakeCompound(result);
            std::vector<int> origins;
            for (size_t s = 0; s < sourceSolids.size(); s++)
            {
                const TopTools_ListOfShape& modified = splitter.Modified(sourceSolids[s]);
                bool any = false;
                for (TopTools_ListIteratorOfListOfShape it(modified); it.More(); it.Next())
                {
                    if (it.Value().ShapeType() != TopAbs_SOLID) continue;
                    builder.Add(result, it.Value());
                    origins.push_back((int)s + 1);
                    any = true;
                }
                if (!any && !splitter.IsDeleted(sourceSolids[s]))
                {
                    builder.Add(result, sourceSolids[s]);
                    origins.push_back((int)s + 1);
                }
            }
            if (!WriteBrep(result, args[1])) return 1;
            std::ofstream file(args[1] + ".origins");
            for (int origin : origins) file << origin << "\n";
            Log("Split: " + std::to_string(origins.size()) + " solids.");
            return 0;
        }

        int SplitFaceWithTwoPoints(const std::vector<std::string>& args)
        {
            if (args.size() < 9) { Error("BREP_SPLIT_A_FACE_USING_TWO_POINTS needs a face and two points."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            TopTools_IndexedMapOfShape faces;
            TopExp::MapShapes(shape, TopAbs_FACE, faces);
            int id = std::stoi(args[2]);
            if (id < 1 || id > faces.Extent()) { Error("The face " + args[2] + " does not exist."); return 1; }
            gp_Pnt a(std::stod(args[3]), std::stod(args[4]), std::stod(args[5]));
            gp_Pnt b(std::stod(args[6]), std::stod(args[7]), std::stod(args[8]));
            Bnd_Box box;
            BRepBndLib::Add(shape, box);
            TopoDS_Shape cutter = MakeLineTool(a, b, TopoDS::Face(faces(id)), 2 * std::sqrt(box.SquareExtent()) + 1);
            TopoDS_Compound edges;
            TopoDS_Shape result = SplitFaces(shape, { TopoDS::Face(faces(id)) }, cutter, edges);
            if (result.IsNull()) { Error("The face could not be divided."); return 1; }
            return WriteBrep(result, args[1]) ? 0 : 1;
        }

        bool ReadGroups(const std::vector<std::string>& args, size_t from, std::map<int, std::set<int>>& groups)
        {
            try
            {
                size_t i = from;
                int n = std::stoi(args.at(i++));
                for (int g = 0; g < n; g++)
                {
                    int solid = std::stoi(args.at(i++));
                    int count = std::stoi(args.at(i++));
                    for (int k = 0; k < count; k++) groups[solid].insert(std::stoi(args.at(i++)));
                }
                return true;
            }
            catch (...) { Error("The face ids of the command are not complete."); return false; }
        }
        // The faces of a group: of the solid named, or of the shape for solid 0
        std::vector<TopoDS_Shape> GroupFaces(const TopoDS_Shape& shape, int solid, const std::set<int>& ids)
        {
            std::vector<TopoDS_Shape> result;
            TopoDS_Shape body = shape;
            if (solid > 0)
            {
                TopTools_IndexedMapOfShape solids;
                TopExp::MapShapes(shape, TopAbs_SOLID, solids);
                if (solid > solids.Extent()) return result;
                body = solids(solid);
            }
            TopTools_IndexedMapOfShape faces;
            TopExp::MapShapes(body, TopAbs_FACE, faces);
            for (int id : ids) if (id >= 1 && id <= faces.Extent()) result.push_back(faces(id));
            return result;
        }

        // The picked faces lifted out of the part as new shells: the faces that touch are sewn into one shell, and a
        // non zero offset moves every shell along the normals of its faces, outwards when it is positive. The part
        // itself is not changed; <out> holds one shell for every connected patch
        int OffsetFaces(const std::vector<std::string>& args)
        {
            if (args.size() < 4) { Error("BREP_OFFSET_FACES needs an input, an output, an offset and faces."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            double offset = std::stod(args[2]);
            std::map<int, std::set<int>> groups;
            if (!ReadGroups(args, 3, groups)) return 1;
            TopTools_IndexedMapOfShape picked;
            for (auto& g : groups) for (auto& f : GroupFaces(shape, g.first, g.second)) picked.Add(f);
            if (picked.Extent() == 0) { Error("No face to offset was found."); return 1; }
            //
            double tolerance = 1e-6;
            Bnd_Box box;
            BRepBndLib::Add(shape, box);
            if (!box.IsVoid()) tolerance = std::max(1e-6, std::sqrt(box.SquareExtent()) * 1e-6);
            BRepBuilderAPI_Sewing sewing(tolerance);
            for (int i = 1; i <= picked.Extent(); i++)
                sewing.Add(BRepBuilderAPI_Copy(picked(i), true, false).Shape());
            sewing.Perform();
            TopoDS_Shape sewn = sewing.SewedShape();
            // Every connected patch is a shell of its own; a lone face is wrapped into one
            BRep_Builder builder;
            std::vector<TopoDS_Shape> patches;
            for (TopExp_Explorer ex(sewn, TopAbs_SHELL); ex.More(); ex.Next()) patches.push_back(ex.Current());
            for (TopExp_Explorer ex(sewn, TopAbs_FACE, TopAbs_SHELL); ex.More(); ex.Next())
            {
                TopoDS_Shell shell;
                builder.MakeShell(shell);
                builder.Add(shell, ex.Current());
                patches.push_back(shell);
            }
            TopoDS_Compound result;
            builder.MakeCompound(result);
            for (const TopoDS_Shape& patch : patches)
            {
                if (std::fabs(offset) < 1e-12) { builder.Add(result, patch); continue; }
                BRepOffsetAPI_MakeOffsetShape maker;
                maker.PerformByJoin(patch, offset, tolerance, BRepOffset_Skin, false, false, GeomAbs_Intersection);
                TopoDS_Shape moved;
                if (maker.IsDone()) moved = maker.Shape();
                if (moved.IsNull())
                {
                    // Face by face, when the patch cannot be offset as a whole
                    BRepBuilderAPI_Sewing faceSewing(tolerance);
                    for (TopExp_Explorer ex(patch, TopAbs_FACE); ex.More(); ex.Next())
                    {
                        BRepOffsetAPI_MakeOffsetShape faceMaker;
                        faceMaker.PerformBySimple(ex.Current(), offset);
                        if (!faceMaker.IsDone()) { Error("The faces could not be offset."); return 1; }
                        faceSewing.Add(faceMaker.Shape());
                    }
                    faceSewing.Perform();
                    moved = faceSewing.SewedShape();
                }
                for (TopExp_Explorer ex(moved, TopAbs_SHELL); ex.More(); ex.Next()) builder.Add(result, ex.Current());
                for (TopExp_Explorer ex(moved, TopAbs_FACE, TopAbs_SHELL); ex.More(); ex.Next())
                {
                    TopoDS_Shell shell;
                    builder.MakeShell(shell);
                    builder.Add(shell, ex.Current());
                    builder.Add(result, shell);
                }
            }
            Log("Offset faces: " + std::to_string(picked.Extent()) + " faces in " + std::to_string(patches.size()) +
                " patches.");
            return WriteBrep(result, args[1]) ? 0 : 1;
        }

        int FuseFaces(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_FUSE_FACES needs an input, an output and faces."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::map<int, std::set<int>> groups;
            if (!ReadGroups(args, 2, groups)) return 1;
            TopTools_IndexedMapOfShape fused;
            for (auto& g : groups) for (auto& f : GroupFaces(shape, g.first, g.second)) fused.Add(f);
            // Every face that is not selected is kept as it is: the unification keeps the edges and vertices it is given
            // (not faces), so every edge that does not lie between two selected faces is kept, which holds the other
            // faces apart
            ShapeUpgrade_UnifySameDomain unify(shape, true, true, false);
            TopTools_IndexedDataMapOfShapeListOfShape edgeFaces;
            TopExp::MapShapesAndAncestors(shape, TopAbs_EDGE, TopAbs_FACE, edgeFaces);
            TopTools_MapOfShape keep;
            for (int i = 1; i <= edgeFaces.Extent(); i++)
            {
                bool between = !edgeFaces(i).IsEmpty();
                for (TopTools_ListIteratorOfListOfShape it(edgeFaces(i)); it.More(); it.Next())
                    if (!fused.Contains(it.Value())) between = false;
                if (!between)
                {
                    keep.Add(edgeFaces.FindKey(i));
                    for (TopExp_Explorer ex(edgeFaces.FindKey(i), TopAbs_VERTEX); ex.More(); ex.Next()) keep.Add(ex.Current());
                }
            }
            unify.KeepShapes(keep);
            unify.Build();
            return WriteBrep(unify.Shape(), args[1]) ? 0 : 1;
        }

        int Repair(const std::vector<std::string>& args, bool preview)
        {
            if (args.size() < 4) { Error("BREP_REPAIR needs an input, an output, the options and a tolerance."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            int options = std::stoi(args[2]);
            double tolerance = std::stod(args[3]);
            if (!(tolerance > 0)) tolerance = 1e-6;
            TopoDS_Shape result = shape;
            if (options & 8)
            {
                BRepBuilderAPI_Sewing sewing(tolerance);
                sewing.Add(result);
                sewing.Perform();
                if (!sewing.SewedShape().IsNull()) result = sewing.SewedShape();
            }
            if (options & 2)
            {
                Handle(ShapeFix_Wireframe) wireframe = new ShapeFix_Wireframe(result);
                wireframe->SetPrecision(tolerance);
                wireframe->SetMaxTolerance(tolerance);
                wireframe->ModeDropSmallEdges() = true;
                wireframe->FixSmallEdges();
                wireframe->FixWireGaps();
                result = wireframe->Shape();
            }
            if (options & 4)
            {
                ShapeFix_FixSmallFace smallFaces;
                smallFaces.Init(result);
                smallFaces.SetPrecision(tolerance);
                smallFaces.SetMaxTolerance(tolerance);
                smallFaces.Perform();
                result = smallFaces.FixShape();
            }
            if (options & 1)
            {
                Handle(ShapeFix_Shape) fix = new ShapeFix_Shape(result);
                fix->SetPrecision(tolerance);
                fix->SetMaxTolerance(tolerance);
                fix->Perform();
                result = fix->Shape();
            }
            if (options & (16 | 32))
            {
                ShapeUpgrade_UnifySameDomain unify(result, (options & 32) != 0, (options & 16) != 0, false);
                unify.SetLinearTolerance(tolerance);
                unify.Build();
                result = unify.Shape();
            }
            if (!preview) return WriteBrep(result, args[1]) ? 0 : 1;
            // What the repair takes away: the edges and the vertices that are no longer there
            int numOfSplitFaces = args.size() > 4 ? std::stoi(args[4]) : 1;
            TopoDS_Shape before = SplitClosedFaces(shape, numOfSplitFaces);
            TopoDS_Shape after = SplitClosedFaces(result, numOfSplitFaces);
            double deflection = Deflection(shape);
            double match = std::max(tolerance, deflection);
            // An edge is kept where the result still has an edge through its midpoint: the pieces of an edge the
            // joining makes whole again are not taken away
            TopoDS_Compound afterEdges;
            BRep_Builder edgeBuilder;
            edgeBuilder.MakeCompound(afterEdges);
            for (TopExp_Explorer ex(after, TopAbs_EDGE); ex.More(); ex.Next())
                if (!BRep_Tool::Degenerated(TopoDS::Edge(ex.Current()))) edgeBuilder.Add(afterEdges, ex.Current());
            auto onResultEdge = [&](const gp_Pnt& point) {
                BRepExtrema_DistShapeShape distance(BRepBuilderAPI_MakeVertex(point).Vertex(), afterEdges);
                return distance.IsDone() && distance.NbSolution() > 0 && distance.Value() <= match;
            };
            std::ofstream out(args[1]);
            TopTools_IndexedMapOfShape edges;
            TopExp::MapShapes(before, TopAbs_EDGE, edges);
            for (int i = 1; i <= edges.Extent(); i++)
            {
                TopoDS_Edge e = TopoDS::Edge(edges(i));
                if (BRep_Tool::Degenerated(e)) continue;
                BRepAdaptor_Curve c(e);
                gp_Pnt m = c.Value(0.5 * (c.FirstParameter() + c.LastParameter()));
                if (!onResultEdge(m)) WriteEdges(out, e, deflection);
            }
            TopTools_IndexedMapOfShape vertices, keptVertices;
            TopExp::MapShapes(before, TopAbs_VERTEX, vertices);
            TopExp::MapShapes(after, TopAbs_VERTEX, keptVertices);
            for (int i = 1; i <= vertices.Extent(); i++)
            {
                gp_Pnt p = BRep_Tool::Pnt(TopoDS::Vertex(vertices(i)));
                bool found = false;
                for (int k = 1; k <= keptVertices.Extent() && !found; k++)
                    if (BRep_Tool::Pnt(TopoDS::Vertex(keptVertices(k))).Distance(p) <= match) found = true;
                if (!found) out << "Point: " << Num(p.X()) << " " << Num(p.Y()) << " " << Num(p.Z()) << "\n";
            }
            return 0;
        }
    }

    void RegisterEditCommands()
    {
        RegisterCommand("BREP_SPLIT", [](auto& a) { return Split(a, false); });
        RegisterCommand("BREP_SPLIT_PREVIEW", [](auto& a) { return Split(a, true); });
        RegisterCommand("BREP_SPLIT_A_FACE_USING_TWO_POINTS", SplitFaceWithTwoPoints);
        RegisterCommand("BREP_OFFSET_FACES", OffsetFaces);
        RegisterCommand("BREP_FUSE_FACES", FuseFaces);
        RegisterCommand("BREP_REPAIR", [](auto& a) { return Repair(a, false); });
        RegisterCommand("BREP_REPAIR_PREVIEW", [](auto& a) { return Repair(a, true); });
    }
}
