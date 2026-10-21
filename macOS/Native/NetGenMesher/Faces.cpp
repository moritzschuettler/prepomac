// PrePoMax for macOS - NetGenMesher: deleting and defeaturing faces of CAD parts.
//
//   BREP_REMOVE_FACES <in> <out> 1 0 <n> <face ids>
//       the faces are deleted; the largest connected piece left is <out>, the others <out>_NNNN.brep, and <out>.pieces
//       holds their number
//   BREP_REMOVE_COMPOUND_FACES <in> <out> <groups> (<solid> <n> <face ids of the solid>)...
//       <out> holds the solids that kept their faces; a solid that lost faces is written as its pieces to the files
//       <out>_NNNN.brep, the largest first, and <out>.opened has a line "<solid> <number of files>" for it; a solid
//       that lost every face has a line in <out>.removed
//   BREP_DEFEATURE <in> <out> <face ids>          the faces are removed and the solid is closed over them
//   BREP_DEFEATURE_COMPOUND <in> <out> <groups> (<solid> <n> <face ids of the solid>)...
#include "Common.h"
#include <BRepAdaptor_Surface.hxx>

#include <BRepAlgoAPI_Defeaturing.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <BRepGProp.hxx>
#include <BRep_Builder.hxx>
#include <GProp_GProps.hxx>
#include <ShapeFix_Shape.hxx>
#include <TopExp.hxx>
#include <TopTools_MapOfShape.hxx>
#include <TopExp_Explorer.hxx>
#include <TopTools_IndexedDataMapOfShapeListOfShape.hxx>
#include <TopTools_ListOfShape.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Compound.hxx>
#include <TopoDS_Shell.hxx>

#include <algorithm>
#include <cstdio>
#include <fstream>
#include <map>
#include <queue>
#include <set>

namespace ngm
{
    namespace
    {
        std::string Numbered(const std::string& out, int number)
        {
            char buffer[32];
            std::snprintf(buffer, sizeof(buffer), "_%04d", number);
            return WithoutExtension(out) + buffer + Extension(out);
        }
        // The face groups of the arguments: "<groups> (<solid> <n> <ids>)..."; solid 0 is the shape itself
        bool ReadFaceGroups(const std::vector<std::string>& args, size_t from, std::map<int, std::set<int>>& groups)
        {
            try
            {
                size_t i = from;
                int numOfGroups = std::stoi(args.at(i++));
                for (int g = 0; g < numOfGroups; g++)
                {
                    int solid = std::stoi(args.at(i++));
                    int n = std::stoi(args.at(i++));
                    for (int k = 0; k < n; k++) groups[solid].insert(std::stoi(args.at(i++)));
                }
                return true;
            }
            catch (...)
            {
                Error("The face ids of the command are not complete.");
                return false;
            }
        }
        // The faces of a shape that are left, joined into shells by the edges they share; the largest shell first
        std::vector<TopoDS_Shape> RemainingPieces(const TopoDS_Shape& shape, const std::set<int>& removedIds)
        {
            TopTools_IndexedMapOfShape faces;
            TopExp::MapShapes(shape, TopAbs_FACE, faces);
            TopTools_IndexedDataMapOfShapeListOfShape edgeFaces;
            TopExp::MapShapesAndAncestors(shape, TopAbs_EDGE, TopAbs_FACE, edgeFaces);
            std::vector<int> group(faces.Extent() + 1, -1);
            std::vector<std::vector<int>> groups;
            for (int i = 1; i <= faces.Extent(); i++)
            {
                if (removedIds.count(i) || group[i] >= 0) continue;
                int id = (int)groups.size();
                groups.push_back({});
                std::queue<int> next;
                next.push(i);
                group[i] = id;
                while (!next.empty())
                {
                    int f = next.front();
                    next.pop();
                    groups[id].push_back(f);
                    for (TopExp_Explorer ex(faces(f), TopAbs_EDGE); ex.More(); ex.Next())
                    {
                        int k = edgeFaces.FindIndex(ex.Current());
                        if (k <= 0) continue;
                        for (TopTools_ListIteratorOfListOfShape it(edgeFaces(k)); it.More(); it.Next())
                        {
                            int other = faces.FindIndex(it.Value());
                            if (other <= 0 || removedIds.count(other) || group[other] >= 0) continue;
                            group[other] = id;
                            next.push(other);
                        }
                    }
                }
            }
            std::vector<std::pair<double, TopoDS_Shape>> pieces;
            BRep_Builder builder;
            for (auto& ids : groups)
            {
                TopoDS_Shell shell;
                builder.MakeShell(shell);
                for (int f : ids) builder.Add(shell, faces(f));
                GProp_GProps props;
                BRepGProp::SurfaceProperties(shell, props);
                pieces.push_back({ props.Mass(), shell });
            }
            std::stable_sort(pieces.begin(), pieces.end(), [](auto& a, auto& b) { return a.first > b.first; });
            std::vector<TopoDS_Shape> result;
            for (auto& piece : pieces) result.push_back(piece.second);
            return result;
        }

        int RemoveFaces(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_REMOVE_FACES needs an input, an output and face ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::map<int, std::set<int>> groups;
            if (!ReadFaceGroups(args, 2, groups)) return 1;
            std::vector<TopoDS_Shape> pieces = RemainingPieces(shape, groups[0]);
            if (pieces.empty()) { Error("No face of the part is left."); return 1; }
            if (!WriteBrep(pieces[0], args[1])) return 1;
            for (size_t i = 1; i < pieces.size(); i++)
                if (!WriteBrep(pieces[i], Numbered(args[1], (int)i))) return 1;
            std::ofstream(args[1] + ".pieces") << (pieces.size() - 1) << "\n";
            Log("Delete faces: " + std::to_string(groups[0].size()) + " faces deleted, " +
                std::to_string(pieces.size()) + " pieces left.");
            return 0;
        }

        int RemoveCompoundFaces(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_REMOVE_COMPOUND_FACES needs an input, an output and face ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::map<int, std::set<int>> groups;
            if (!ReadFaceGroups(args, 2, groups)) return 1;
            TopTools_IndexedMapOfShape solids;
            TopExp::MapShapes(shape, TopAbs_SOLID, solids);
            TopoDS_Compound kept;
            BRep_Builder builder;
            builder.MakeCompound(kept);
            // A face the solids share is one face of the compound, so it is gone from every solid that has it
            TopTools_MapOfShape removedFaces;
            for (auto& group : groups)
            {
                if (group.first < 1 || group.first > solids.Extent()) continue;
                TopTools_IndexedMapOfShape faces;
                TopExp::MapShapes(solids(group.first), TopAbs_FACE, faces);
                for (int id : group.second)
                    if (id >= 1 && id <= faces.Extent()) removedFaces.Add(faces(id));
            }
            for (int s = 1; s <= solids.Extent(); s++)
            {
                TopTools_IndexedMapOfShape faces;
                TopExp::MapShapes(solids(s), TopAbs_FACE, faces);
                for (int id = 1; id <= faces.Extent(); id++)
                    if (removedFaces.Contains(faces(id))) groups[s].insert(id);
            }
            std::ofstream opened(args[1] + ".opened"), removed(args[1] + ".removed");
            int fileNumber = 1;
            for (int s = 1; s <= solids.Extent(); s++)
            {
                auto it = groups.find(s);
                if (it == groups.end() || it->second.empty())
                {
                    builder.Add(kept, solids(s));
                    continue;
                }
                std::vector<TopoDS_Shape> pieces = RemainingPieces(solids(s), it->second);
                if (pieces.empty())
                {
                    removed << s << "\n";
                    continue;
                }
                opened << s << " " << pieces.size() << "\n";
                for (const TopoDS_Shape& piece : pieces)
                    if (!WriteBrep(piece, Numbered(args[1], fileNumber++))) return 1;
            }
            opened.close();
            removed.close();
            return WriteBrep(kept, args[1]) ? 0 : 1;
        }

        // The faces removed by the defeaturing of OpenCASCADE, which extends the neighbouring faces over them
        TopoDS_Shape Defeature(const TopoDS_Shape& solid, const std::set<int>& faceIds, bool& ok)
        {
            ok = false;
            TopTools_IndexedMapOfShape faces;
            TopExp::MapShapes(solid, TopAbs_FACE, faces);
            TopTools_ListOfShape removed;
            for (int id : faceIds)
                if (id >= 1 && id <= faces.Extent()) removed.Append(faces(id));
            if (removed.IsEmpty()) { ok = true; return solid; }
            BRepAlgoAPI_Defeaturing defeaturing;
            defeaturing.SetShape(solid);
            defeaturing.AddFacesToRemove(removed);
            defeaturing.SetRunParallel(true);
            defeaturing.SetToFillHistory(true);
            defeaturing.Build();
            if (!defeaturing.IsDone() || defeaturing.HasErrors()) return solid;
            TopoDS_Shape result = defeaturing.Shape();
            // The kernel joins the faces that lie on one surface anywhere in the solid: a closed face the faces of
            // several kept faces went into is divided again into that many faces
            std::map<int, int> resultFacePieces;
            TopTools_IndexedMapOfShape resultFaces;
            TopExp::MapShapes(result, TopAbs_FACE, resultFaces);
            TopTools_MapOfShape removedMap;
            for (TopTools_ListIteratorOfListOfShape it(removed); it.More(); it.Next()) removedMap.Add(it.Value());
            for (int i = 1; i <= faces.Extent(); i++)
            {
                if (removedMap.Contains(faces(i))) continue;
                const TopTools_ListOfShape& modified = defeaturing.Modified(faces(i));
                if (modified.IsEmpty())
                {
                    int id = resultFaces.FindIndex(faces(i));
                    if (id > 0) resultFacePieces[id]++;
                }
                for (TopTools_ListIteratorOfListOfShape it(modified); it.More(); it.Next())
                {
                    int id = resultFaces.FindIndex(it.Value());
                    if (id > 0) resultFacePieces[id]++;
                }
            }
            int pieces = 0;
            bool mixed = false;
            for (auto& entry : resultFacePieces)
            {
                TopoDS_Face face = TopoDS::Face(resultFaces(entry.first));
                BRepAdaptor_Surface surface(face, false);
                bool closed = surface.IsUClosed() || surface.IsVClosed();
                if (!closed) continue;
                if (pieces == 0) pieces = entry.second;
                else if (pieces != entry.second) mixed = true;
            }
            if (pieces > 1 && !mixed) result = SplitClosedFaces(result, pieces);
            BRepCheck_Analyzer analyzer(result);
            if (!analyzer.IsValid())
            {
                Handle(ShapeFix_Shape) fix = new ShapeFix_Shape(result);
                fix->Perform();
                result = fix->Shape();
            }
            TopTools_IndexedMapOfShape resultSolids;
            TopExp::MapShapes(result, TopAbs_SOLID, resultSolids);
            ok = resultSolids.Extent() >= 1;
            return result;
        }
        int DefeatureCommand(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_DEFEATURE needs an input, an output and face ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::set<int> ids;
            for (size_t i = 2; i < args.size(); i++) ids.insert(std::stoi(args[i]));
            bool ok;
            TopoDS_Shape result = Defeature(shape, ids, ok);
            if (!ok) { Error("The faces could not be removed from the solid."); return 1; }
            Log("Defeature: " + std::to_string(ids.size()) + " faces removed.");
            return WriteBrep(result, args[1]) ? 0 : 1;
        }
        int DefeatureCompound(const std::vector<std::string>& args)
        {
            if (args.size() < 3) { Error("BREP_DEFEATURE_COMPOUND needs an input, an output and face ids."); return 1; }
            TopoDS_Shape shape;
            if (!ReadShape(args[0], shape)) return 1;
            std::map<int, std::set<int>> groups;
            if (!ReadFaceGroups(args, 2, groups)) return 1;
            TopTools_IndexedMapOfShape solids;
            TopExp::MapShapes(shape, TopAbs_SOLID, solids);
            TopoDS_Compound compound;
            BRep_Builder builder;
            builder.MakeCompound(compound);
            for (int s = 1; s <= solids.Extent(); s++)
            {
                auto it = groups.find(s);
                if (it == groups.end()) { builder.Add(compound, solids(s)); continue; }
                bool ok;
                TopoDS_Shape result = Defeature(solids(s), it->second, ok);
                if (!ok) { Error("The faces could not be removed from solid " + std::to_string(s) + "."); return 1; }
                for (TopExp_Explorer ex(result, TopAbs_SOLID); ex.More(); ex.Next()) builder.Add(compound, ex.Current());
            }
            return WriteBrep(compound, args[1]) ? 0 : 1;
        }
    }

    void RegisterFaceCommands()
    {
        RegisterCommand("BREP_REMOVE_FACES", RemoveFaces);
        RegisterCommand("BREP_REMOVE_COMPOUND_FACES", RemoveCompoundFaces);
        RegisterCommand("BREP_DEFEATURE", DefeatureCommand);
        RegisterCommand("BREP_DEFEATURE_COMPOUND", DefeatureCompound);
    }
}
