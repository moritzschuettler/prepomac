// PrePoMax for macOS - NetGenMesher: the geometry operations on CAD parts.
//
//   BREP_CONVERT_SOLID_TO_SHELL | BREP_CONVERT_SHELL_TO_SOLID <in> <out>
//   BREP_REVERSE_FACES <in> <out> <face ids>
//   BREP_REMOVE_SOLIDS <in> <out> <solid ids>
//   BREP_MERGE_SOLIDS <in> <out> <solid ids>        the solids fused into one; <out>.origins is written
#include "Common.h"

#include <TopTools_MapOfShape.hxx>

#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepBuilderAPI_MakeSolid.hxx>
#include <BRepBuilderAPI_Sewing.hxx>
#include <BRepLib.hxx>
#include <BRep_Builder.hxx>
#include <ShapeFix_Shape.hxx>
#include <ShapeFix_Solid.hxx>
#include <ShapeUpgrade_UnifySameDomain.hxx>
#include <TopExp.hxx>
#include <TopExp_Explorer.hxx>
#include <TopTools_ListOfShape.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Compound.hxx>
#include <TopoDS_Shell.hxx>
#include <TopoDS_Solid.hxx>
#include <BRepTools_ReShape.hxx>

#include <fstream>
#include <set>

namespace ngm
{
    namespace
    {
        std::vector<int> Ids(const std::vector<std::string>& args, size_t from)
        {
            std::vector<int> ids;
            for (size_t i = from; i < args.size(); i++)
            {
                if (args[i].empty()) continue;
                try { ids.push_back(std::stoi(args[i])); }
                catch (...) {}
            }
            return ids;
        }

        int SolidToShell(const std::vector<std::string>& args)
        {
            if (args.size() < 2) { Error("The conversion needs an input and an output file."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            TopoDS_Compound compound;
            BRep_Builder builder;
            builder.MakeCompound(compound);
            int count = 0;
            for (TopExp_Explorer ex(shape, TopAbs_SHELL); ex.More(); ex.Next(), count++) builder.Add(compound, ex.Current());
            for (TopExp_Explorer ex(shape, TopAbs_FACE, TopAbs_SHELL); ex.More(); ex.Next(), count++) builder.Add(compound, ex.Current());
            if (count == 0) { Error("The geometry has no shells."); return 1; }
            return WriteBrep(count == 1 ? TopoDS_Shape(TopoDS_Iterator(compound).Value()) : TopoDS_Shape(compound), args[1]) ? 0 : 1;
        }
        int ShellToSolid(const std::vector<std::string>& args)
        {
            if (args.size() < 2) { Error("The conversion needs an input and an output file."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            BRepBuilderAPI_Sewing sewing(1e-6);
            sewing.Add(shape);
            sewing.Perform();
            TopoDS_Shape sewed = sewing.SewedShape();
            BRepBuilderAPI_MakeSolid maker;
            int shells = 0;
            for (TopExp_Explorer ex(sewed, TopAbs_SHELL); ex.More(); ex.Next(), shells++) maker.Add(TopoDS::Shell(ex.Current()));
            if (shells == 0 || !maker.IsDone())
            {
                Error("The shell could not be converted to a solid. The shell must be closed.");
                return 1;
            }
            TopoDS_Solid solid = maker.Solid();
            BRepLib::OrientClosedSolid(solid);
            ShapeFix_Solid fix(solid);
            fix.Perform();
            TopoDS_Shape result = fix.Solid();
            if (!BRep_Tool::IsClosed(result))
            {
                Error("The shell is not closed and cannot become a solid.");
                return 1;
            }
            return WriteBrep(result, args[1]) ? 0 : 1;
        }
        int ReverseFaces(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_REVERSE_FACES needs an input, an output and face ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            ShapeMaps maps(shape);
            Handle(BRepTools_ReShape) reshape = new BRepTools_ReShape();
            for (int id : Ids(args, 2))
            {
                if (id < 1 || id > maps.faces.Extent()) continue;
                reshape->Replace(maps.faces(id), maps.faces(id).Reversed());
            }
            return WriteBrep(reshape->Apply(shape), args[1]) ? 0 : 1;
        }
        int RemoveSolids(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_REMOVE_SOLIDS needs an input, an output and solid ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            ShapeMaps maps(shape);
            std::vector<int> ids = Ids(args, 2);
            std::set<int> removed(ids.begin(), ids.end());
            TopoDS_Compound compound;
            BRep_Builder builder;
            builder.MakeCompound(compound);
            for (int i = 1; i <= maps.solids.Extent(); i++)
                if (!removed.count(i)) builder.Add(compound, maps.solids(i));
            return WriteBrep(compound, args[1]) ? 0 : 1;
        }
        int MergeSolids(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_MERGE_SOLIDS needs an input, an output and solid ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            ShapeMaps maps(shape);
            std::vector<int> ids = Ids(args, 2);
            std::set<int> merged(ids.begin(), ids.end());
            TopTools_ListOfShape arguments, tools;
            bool first = true;
            for (int id : merged)
            {
                if (id < 1 || id > maps.solids.Extent()) continue;
                if (first) arguments.Append(maps.solids(id));
                else tools.Append(maps.solids(id));
                first = false;
            }
            BRepAlgoAPI_Fuse fuse;
            fuse.SetArguments(arguments);
            fuse.SetTools(tools);
            fuse.Build();
            if (!fuse.IsDone()) { Error("The solids could not be merged."); return 1; }
            ShapeUpgrade_UnifySameDomain unify(fuse.Shape(), true, true, false);
            // The faces the merged solid shares with the other solids stay divided as those solids divide them: their
            // edges are kept
            TopTools_MapOfShape keep;
            for (int i = 1; i <= maps.solids.Extent(); i++)
            {
                if (merged.count(i)) continue;
                for (TopExp_Explorer ex(maps.solids(i), TopAbs_EDGE); ex.More(); ex.Next())
                {
                    keep.Add(ex.Current());
                    for (TopTools_ListIteratorOfListOfShape it(fuse.Modified(ex.Current())); it.More(); it.Next())
                        keep.Add(it.Value());
                }
            }
            unify.KeepShapes(keep);
            unify.Build();
            // Solids that do not touch over faces stay apart, and are not merged
            int numOfMerged = 0;
            for (TopExp_Explorer ex(unify.Shape(), TopAbs_SOLID); ex.More(); ex.Next()) numOfMerged++;
            if (merged.size() > 1 && numOfMerged != 1)
            {
                Error("The solids could not be merged into one solid: they must touch over faces.");
                return 1;
            }
            TopoDS_Compound compound;
            BRep_Builder builder;
            builder.MakeCompound(compound);
            std::vector<int> origins;
            int mergedOrigin = merged.empty() ? 1 : *merged.begin();
            bool mergedWritten = false;
            for (int i = 1; i <= maps.solids.Extent(); i++)
            {
                if (merged.count(i))
                {
                    if (mergedWritten) continue;
                    for (TopExp_Explorer ex(unify.Shape(), TopAbs_SOLID); ex.More(); ex.Next())
                    {
                        builder.Add(compound, ex.Current());
                        origins.push_back(mergedOrigin);
                    }
                    mergedWritten = true;
                }
                else
                {
                    builder.Add(compound, maps.solids(i));
                    origins.push_back(i);
                }
            }
            if (!WriteBrep(compound, args[1])) return 1;
            std::ofstream file(args[1] + ".origins");
            for (int origin : origins) file << origin << "\n";
            return 0;
        }
    }

    void RegisterOperationCommands()
    {
        RegisterCommand("BREP_CONVERT_SOLID_TO_SHELL", SolidToShell);
        RegisterCommand("BREP_CONVERT_SHELL_TO_SOLID", ShellToSolid);
        RegisterCommand("BREP_REVERSE_FACES", ReverseFaces);
        RegisterCommand("BREP_REMOVE_SOLIDS", RemoveSolids);
        RegisterCommand("BREP_MERGE_SOLIDS", MergeSolids);
    }
}
