// PrePoMax for macOS - NetGenMesher, the CAD and meshing utility PrePoMax runs as a separate process.
// The Windows build ships its own NetGenMesher.exe (OpenCASCADE 8 and Netgen 5.3). This program implements the same
// commands, arguments and output files with the OpenCASCADE and the gmsh (with its Netgen mesher) libraries of macOS.
#pragma once

#include <TopoDS_Shape.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
#include <TopTools_IndexedDataMapOfShapeListOfShape.hxx>

#include <functional>
#include <map>
#include <string>
#include <vector>

namespace ngm
{
    // Messages go to the standard output, which PrePoMax shows in its output window
    void Log(const std::string& text);
    // Errors go to the standard error; PrePoMax shows them when a command fails
    void Error(const std::string& text);

    // Numbers are written with the invariant format PrePoMax reads
    std::string Num(double value);

    // Reads a BREP, STEP or IGES file by its extension (or as the given kind: "BREP", "STEP", "IGES")
    bool ReadShape(const std::string& fileName, TopoDS_Shape& shape, const std::string& kind = "");
    bool WriteBrep(const TopoDS_Shape& shape, const std::string& fileName);
    bool WriteStep(const TopoDS_Shape& shape, const std::string& fileName);

    // The entities of a shape numbered like the Windows mesher: 1-based in the order TopExp::MapShapes finds them
    struct ShapeMaps
    {
        TopTools_IndexedMapOfShape solids, shells, faces, edges, vertices;
        explicit ShapeMaps(const TopoDS_Shape& shape);
    };

    // Divides every closed (periodic) face into the given number of faces
    TopoDS_Shape SplitClosedFaces(const TopoDS_Shape& shape, int numOfSplitFaces);

    // The parts of a shape: its solids, then the shells and faces that belong to no solid
    std::vector<TopoDS_Shape> GetParts(const TopoDS_Shape& shape);

    std::string ToLower(std::string text);
    std::string Extension(const std::string& fileName);
    std::string WithoutExtension(const std::string& fileName);
    bool FileExists(const std::string& fileName);

    // Key value file of the mesh parameters (the description line names the value on the next line)
    std::map<std::string, std::string> ReadParameterFile(const std::string& fileName);
    double GetDouble(const std::map<std::string, std::string>& values, const std::string& name, double defaultValue);
    int GetInt(const std::map<std::string, std::string>& values, const std::string& name, int defaultValue);
    std::string GetString(const std::map<std::string, std::string>& values, const std::string& name);

    // Commands
    using Command = std::function<int(const std::vector<std::string>& args)>;
    void RegisterCommand(const std::string& name, Command command);

    int Visualization(const std::vector<std::string>& args);
    void RegisterGeometryCommands();
    void RegisterMeshCommands();
    void RegisterOperationCommands();
    void RegisterFaceCommands();
    void RegisterEditCommands();
}
