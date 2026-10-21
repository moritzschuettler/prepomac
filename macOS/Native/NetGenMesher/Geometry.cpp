// PrePoMax for macOS - NetGenMesher: reading CAD files into parts, writing assemblies and the transformations.
//
//   STEP_ASSEMBLY_SPLIT_TO_COMPOUNDS | IGES_ASSEMBLY_SPLIT_TO_COMPOUNDS | BREP_ASSEMBLY_SPLIT_TO_COMPOUNDS |
//   BREP_ASSEMBLY_SPLIT_TO_PARTS <in> <out.brep> [number of split faces]
//       writes <out>_NNNN.brep for every part and <out>_compound_NNNN.brep for a body of several solids
//   BREP_SPLIT_CLOSED_FACES <in> <out> <number of split faces>
//   BREP_COMPOUND <in 1> ... <in n> <out>        the solids of the parts glued so they share the faces they touch
//   STEP_ASSEMBLY | BREP_ASSEMBLY <in 1> ... <in n> <out>
//   SAVE_BREP_AS_STEP <in> <out>
//   BREP_TRANSFORM <in> <out> <transform file>
#include "Common.h"

#include <BOPAlgo_Builder.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepBuilderAPI_GTransform.hxx>
#include <BRep_Builder.hxx>
#include <STEPCAFControl_Reader.hxx>
#include <TDF_LabelSequence.hxx>
#include <TDocStd_Document.hxx>
#include <TopExp.hxx>
#include <TopExp_Explorer.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Compound.hxx>
#include <TopoDS_Iterator.hxx>
#include <XCAFApp_Application.hxx>
#include <XCAFDoc_DocumentTool.hxx>
#include <XCAFDoc_ShapeTool.hxx>
#include <gp_GTrsf.hxx>
#include <gp_Trsf.hxx>
#include <Interface_Static.hxx>
#include <IFSelect_ReturnStatus.hxx>

#include <cmath>
#include <cstdio>
#include <fstream>
#include <sstream>

namespace ngm
{
    namespace
    {
        int CountSolids(const TopoDS_Shape& shape)
        {
            TopTools_IndexedMapOfShape solids;
            TopExp::MapShapes(shape, TopAbs_SOLID, solids);
            return solids.Extent();
        }
        std::string Numbered(const std::string& base, const std::string& infix, int number)
        {
            char buffer[32];
            std::snprintf(buffer, sizeof(buffer), "%04d", number);
            return base + infix + buffer + ".brep";
        }

        // The bodies of a STEP file: the shapes of the leaf products of its assembly tree, placed in the assembly
        void CollectStepBodies(const Handle(XCAFDoc_ShapeTool)& tool, const TDF_Label& label, const TopLoc_Location& location,
                               std::vector<TopoDS_Shape>& bodies)
        {
            TDF_Label referred = label;
            TopLoc_Location placed = location;
            if (XCAFDoc_ShapeTool::IsReference(label))
            {
                XCAFDoc_ShapeTool::GetReferredShape(label, referred);
                placed = location * XCAFDoc_ShapeTool::GetLocation(label);
            }
            if (XCAFDoc_ShapeTool::IsAssembly(referred))
            {
                TDF_LabelSequence components;
                XCAFDoc_ShapeTool::GetComponents(referred, components, false);
                for (int i = 1; i <= components.Length(); i++) CollectStepBodies(tool, components(i), placed, bodies);
                return;
            }
            TopoDS_Shape shape = XCAFDoc_ShapeTool::GetShape(referred);
            if (shape.IsNull()) return;
            bodies.push_back(shape.Moved(placed));
        }
        bool ReadStepBodies(const std::string& fileName, std::vector<TopoDS_Shape>& bodies)
        {
            Interface_Static::SetCVal("xstep.cascade.unit", "MM");
            Handle(XCAFApp_Application) application = XCAFApp_Application::GetApplication();
            Handle(TDocStd_Document) document;
            application->NewDocument("MDTV-XCAF", document);
            STEPCAFControl_Reader reader;
            reader.SetNameMode(false);
            reader.SetColorMode(false);
            reader.SetLayerMode(false);
            if (reader.ReadFile(fileName.c_str()) != IFSelect_RetDone || !reader.Transfer(document))
            {
                Error("The STEP file " + fileName + " could not be read.");
                return false;
            }
            Handle(XCAFDoc_ShapeTool) tool = XCAFDoc_DocumentTool::ShapeTool(document->Main());
            TDF_LabelSequence roots;
            tool->GetFreeShapes(roots);
            for (int i = 1; i <= roots.Length(); i++) CollectStepBodies(tool, roots(i), TopLoc_Location(), bodies);
            application->Close(document);
            if (bodies.empty())
            {
                Error("The STEP file " + fileName + " contains no geometry.");
                return false;
            }
            return true;
        }

        int SplitAssembly(const std::string& kind, bool toCompounds, const std::vector<std::string>& args)
        {
            if (args.size() < 2)
            {
                Error("The assembly split needs an input and an output file name.");
                return 1;
            }
            int numOfSplitFaces = args.size() > 2 ? std::stoi(args[2]) : 1;
            std::string base = WithoutExtension(args[1]);
            // The bodies: a body of several solids is a compound part, the others are divided into their parts
            std::vector<TopoDS_Shape> bodies;
            if (kind == "STEP")
            {
                if (!ReadStepBodies(args[0], bodies)) return 1;
            }
            else
            {
                TopoDS_Shape shape;
                if (!ReadShape(args[0], shape, kind)) return 1;
                // The solids that share faces are found over the whole file
                bodies.push_back(shape);
            }
            int partCount = 0, compoundCount = 0;
            for (const TopoDS_Shape& body : bodies)
            {
                // Solids that share faces (a part cut into cells, fragmented solids) are one compound part; solids that
                // only lie side by side and the free shells and faces are parts of their own
                std::vector<TopoDS_Shape> parts = GetParts(body);
                std::vector<TopoDS_Shape> solids, others;
                for (const TopoDS_Shape& part : parts)
                    (part.ShapeType() == TopAbs_SOLID ? solids : others).push_back(part);
                std::vector<int> group(solids.size(), -1);
                std::vector<std::vector<int>> groups;
                if (toCompounds)
                {
                    std::vector<TopTools_IndexedMapOfShape> faces(solids.size());
                    for (size_t i = 0; i < solids.size(); i++) TopExp::MapShapes(solids[i], TopAbs_FACE, faces[i]);
                    for (size_t i = 0; i < solids.size(); i++)
                    {
                        if (group[i] >= 0) continue;
                        group[i] = (int)groups.size();
                        groups.push_back({ (int)i });
                        for (size_t g = 0; g < groups.back().size(); g++)
                        {
                            int a = groups.back()[g];
                            for (size_t j = 0; j < solids.size(); j++)
                            {
                                if (group[j] >= 0) continue;
                                bool shared = false;
                                for (int f = 1; f <= faces[j].Extent() && !shared; f++) shared = faces[a].Contains(faces[j](f));
                                if (shared)
                                {
                                    group[j] = group[i];
                                    groups.back().push_back((int)j);
                                }
                            }
                        }
                    }
                }
                else for (size_t i = 0; i < solids.size(); i++) groups.push_back({ (int)i });
                // A body of several solids a STEP product makes is one compound part as well
                bool oneCompound = toCompounds && kind != "IGES" && solids.size() > 1 && groups.size() > 1 &&
                                   kind == "STEP" && others.empty() &&
                                   (body.ShapeType() == TopAbs_COMPOUND || body.ShapeType() == TopAbs_COMPSOLID) &&
                                   bodies.size() > 1;
                if (oneCompound) groups = { [&]() { std::vector<int> all; for (size_t i = 0; i < solids.size(); i++) all.push_back((int)i); return all; }() };
                for (auto& members : groups)
                {
                    if (members.size() > 1)
                    {
                        TopoDS_Compound compound;
                        BRep_Builder builder;
                        builder.MakeCompound(compound);
                        for (int m : members) builder.Add(compound, solids[m]);
                        TopoDS_Shape divided = SplitClosedFaces(compound, numOfSplitFaces);
                        if (!WriteBrep(divided, Numbered(base, "_compound_", ++compoundCount))) return 1;
                    }
                    else
                    {
                        TopoDS_Shape divided = SplitClosedFaces(solids[members[0]], numOfSplitFaces);
                        if (!WriteBrep(divided, Numbered(base, "_", ++partCount))) return 1;
                    }
                }
                for (const TopoDS_Shape& part : others)
                {
                    TopoDS_Shape divided = SplitClosedFaces(part, numOfSplitFaces);
                    if (!WriteBrep(divided, Numbered(base, "_", ++partCount))) return 1;
                }
            }
            Log("Split: " + std::to_string(partCount) + " parts and " + std::to_string(compoundCount) +
                " compound parts.");
            if (partCount + compoundCount == 0)
            {
                Error("The file " + args[0] + " contains no solids, shells or faces.");
                return 1;
            }
            return 0;
        }

        int SplitClosedFacesCommand(const std::vector<std::string>& args)
        {
            if (args.size() < 3)
            {
                Error("BREP_SPLIT_CLOSED_FACES needs an input, an output and a number of faces.");
                return 1;
            }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            return WriteBrep(SplitClosedFaces(shape, std::stoi(args[2])), args[1]) ? 0 : 1;
        }

        // The parts glued together: touching solids share the faces between them, so the mesh is conformal. Beside
        // the result the origins file names, for every solid of the result, the parts it lies in
        int Compound(const std::vector<std::string>& args)
        {
            if (args.size() < 2)
            {
                Error("BREP_COMPOUND needs input files and an output file.");
                return 1;
            }
            std::vector<TopoDS_Shape> parts;
            for (size_t i = 0; i + 1 < args.size(); i++)
            {
                TopoDS_Shape shape;
                if (!ReadShape(args[i], shape)) return 1;
                parts.push_back(shape);
            }
            BOPAlgo_Builder builder;
            for (const TopoDS_Shape& part : parts) builder.AddArgument(part);
            builder.SetRunParallel(true);
            builder.SetNonDestructive(true);
            builder.Perform();
            TopoDS_Shape result;
            if (builder.HasErrors())
            {
                Log("The parts could not be glued; they are combined without shared faces.");
                TopoDS_Compound compound;
                BRep_Builder b;
                b.MakeCompound(compound);
                for (const TopoDS_Shape& part : parts) b.Add(compound, part);
                result = compound;
            }
            else result = builder.Shape();
            if (!WriteBrep(result, args.back())) return 1;
            // The origins: the solids of the inputs, numbered over all inputs
            std::vector<TopoDS_Shape> sourceSolids;
            for (const TopoDS_Shape& part : parts)
                for (TopExp_Explorer ex(part, TopAbs_SOLID); ex.More(); ex.Next()) sourceSolids.push_back(ex.Current());
            TopTools_IndexedMapOfShape resultSolids;
            TopExp::MapShapes(result, TopAbs_SOLID, resultSolids);
            std::ofstream origins(args.back() + ".origins");
            for (int i = 1; i <= resultSolids.Extent(); i++)
            {
                std::ostringstream line;
                for (size_t s = 0; s < sourceSolids.size(); s++)
                {
                    bool from = sourceSolids[s].IsSame(resultSolids(i));
                    if (!from && !builder.HasErrors())
                    {
                        for (TopTools_ListIteratorOfListOfShape it(builder.Modified(sourceSolids[s])); it.More(); it.Next())
                            if (it.Value().IsSame(resultSolids(i))) from = true;
                    }
                    if (from) line << (line.tellp() > 0 ? " " : "") << (s + 1);
                }
                origins << line.str() << "\n";
            }
            Log("Compound: " + std::to_string(resultSolids.Extent()) + " solids.");
            return 0;
        }

        int Assembly(bool step, const std::vector<std::string>& args)
        {
            if (args.size() < 2)
            {
                Error("The assembly export needs input files and an output file.");
                return 1;
            }
            TopoDS_Compound compound;
            BRep_Builder builder;
            builder.MakeCompound(compound);
            for (size_t i = 0; i + 1 < args.size(); i++)
            {
                TopoDS_Shape shape;
                if (!ReadShape(args[i], shape)) return 1;
                builder.Add(compound, shape);
            }
            return (step ? WriteStep(compound, args.back()) : WriteBrep(compound, args.back())) ? 0 : 1;
        }

        int SaveAsStep(const std::vector<std::string>& args)
        {
            if (args.size() < 2)
            {
                Error("SAVE_BREP_AS_STEP needs an input and an output file.");
                return 1;
            }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            return WriteStep(shape, args[1]) ? 0 : 1;
        }

        // Transform type: Translate | Rotate | Mirror | Scale; Point, Direction, Angle (degrees), Factors, Number of copies
        int Transform(const std::vector<std::string>& args)
        {
            if (args.size() < 3)
            {
                Error("BREP_TRANSFORM needs an input, an output and a transform file.");
                return 1;
            }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::ifstream file(args[2]);
            std::string line, type;
            std::vector<double> point, direction, factors;
            double angle = 0;
            int copies = 0;
            auto numbers = [](const std::string& text) {
                std::vector<double> values;
                std::istringstream in(text);
                double value;
                while (in >> value) values.push_back(value);
                return values;
            };
            while (std::getline(file, line))
            {
                if (!line.empty() && line.back() == '\r') line.pop_back();
                size_t colon = line.find(':');
                if (colon == std::string::npos) continue;
                std::string key = line.substr(0, colon), value = line.substr(colon + 1);
                if (key == "Transform type") type = value.substr(value.find_first_not_of(' '));
                else if (key == "Point") point = numbers(value);
                else if (key == "Direction") direction = numbers(value);
                else if (key == "Angle") { auto v = numbers(value); if (!v.empty()) angle = v[0]; }
                else if (key == "Factors") factors = numbers(value);
                else if (key == "Number of copies") { auto v = numbers(value); if (!v.empty()) copies = (int)v[0]; }
            }
            auto transformed = [&](const TopoDS_Shape& source) -> TopoDS_Shape {
                if (type == "Scale")
                {
                    gp_GTrsf scale;
                    double fx = factors.size() > 0 ? factors[0] : 1, fy = factors.size() > 1 ? factors[1] : fx,
                           fz = factors.size() > 2 ? factors[2] : fx;
                    double cx = point.size() > 2 ? point[0] : 0, cy = point.size() > 2 ? point[1] : 0,
                           cz = point.size() > 2 ? point[2] : 0;
                    scale.SetValue(1, 1, fx); scale.SetValue(2, 2, fy); scale.SetValue(3, 3, fz);
                    scale.SetTranslationPart(gp_XYZ(cx * (1 - fx), cy * (1 - fy), cz * (1 - fz)));
                    BRepBuilderAPI_GTransform builder(source, scale, true);
                    return builder.Shape();
                }
                gp_Trsf trsf;
                if (type == "Translate" && direction.size() == 3)
                    trsf.SetTranslation(gp_Vec(direction[0], direction[1], direction[2]));
                else if (type == "Rotate" && point.size() == 3 && direction.size() == 3)
                    trsf.SetRotation(gp_Ax1(gp_Pnt(point[0], point[1], point[2]),
                                            gp_Dir(direction[0], direction[1], direction[2])), angle * M_PI / 180);
                else if (type == "Mirror" && point.size() == 3 && direction.size() == 3)
                    trsf.SetMirror(gp_Ax2(gp_Pnt(point[0], point[1], point[2]),
                                          gp_Dir(direction[0], direction[1], direction[2])));
                BRepBuilderAPI_Transform builder(source, trsf, true);
                return builder.Shape();
            };
            if (copies <= 0) return WriteBrep(transformed(shape), args[1]) ? 0 : 1;
            std::string base = WithoutExtension(args[1]);
            TopoDS_Shape current = shape;
            for (int i = 1; i <= copies; i++)
            {
                current = transformed(current);
                if (!WriteBrep(current, Numbered(base, "_", i))) return 1;
            }
            return 0;
        }
    }

    void RegisterGeometryCommands()
    {
        RegisterCommand("STEP_ASSEMBLY_SPLIT_TO_COMPOUNDS", [](auto& a) { return SplitAssembly("STEP", true, a); });
        RegisterCommand("IGES_ASSEMBLY_SPLIT_TO_COMPOUNDS", [](auto& a) { return SplitAssembly("IGES", true, a); });
        RegisterCommand("BREP_ASSEMBLY_SPLIT_TO_COMPOUNDS", [](auto& a) { return SplitAssembly("BREP", true, a); });
        RegisterCommand("BREP_ASSEMBLY_SPLIT_TO_PARTS", [](auto& a) { return SplitAssembly("BREP", false, a); });
        RegisterCommand("BREP_SPLIT_CLOSED_FACES", SplitClosedFacesCommand);
        RegisterCommand("BREP_COMPOUND", Compound);
        RegisterCommand("STEP_ASSEMBLY", [](auto& a) { return Assembly(true, a); });
        RegisterCommand("BREP_ASSEMBLY", [](auto& a) { return Assembly(false, a); });
        RegisterCommand("SAVE_BREP_AS_STEP", SaveAsStep);
        RegisterCommand("BREP_TRANSFORM", Transform);
        RegisterCommand("BREP_VISUALIZATION", Visualization);
    }
}
