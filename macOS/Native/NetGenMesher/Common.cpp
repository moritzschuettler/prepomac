// PrePoMax for macOS - NetGenMesher: reading and writing of shapes and the helpers of all commands.
#include "Common.h"

#include <BRepTools.hxx>
#include <BRep_Builder.hxx>
#include <IFSelect_ReturnStatus.hxx>
#include <IGESControl_Reader.hxx>
#include <Interface_Static.hxx>
#include <STEPControl_Reader.hxx>
#include <STEPControl_Writer.hxx>
#include <ShapeUpgrade_ShapeDivideClosed.hxx>
#include <TopExp.hxx>
#include <TopExp_Explorer.hxx>
#include <TopoDS.hxx>
#include <TopoDS_Iterator.hxx>

#include <algorithm>
#include <cstdio>
#include <fstream>
#include <iostream>
#include <sstream>
#include <sys/stat.h>

namespace ngm
{
    void Log(const std::string& text)
    {
        std::cout << text << std::endl;
    }
    void Error(const std::string& text)
    {
        std::cerr << text << std::endl;
        std::cout << "Error: " << text << std::endl;
    }
    std::string Num(double value)
    {
        char buffer[64];
        std::snprintf(buffer, sizeof(buffer), "%.17g", value);
        return buffer;
    }
    std::string ToLower(std::string text)
    {
        std::transform(text.begin(), text.end(), text.begin(), [](unsigned char c) { return (char)std::tolower(c); });
        return text;
    }
    std::string Extension(const std::string& fileName)
    {
        size_t slash = fileName.find_last_of("/\\");
        size_t dot = fileName.find_last_of('.');
        if (dot == std::string::npos || (slash != std::string::npos && dot < slash)) return "";
        return ToLower(fileName.substr(dot));
    }
    std::string WithoutExtension(const std::string& fileName)
    {
        size_t slash = fileName.find_last_of("/\\");
        size_t dot = fileName.find_last_of('.');
        if (dot == std::string::npos || (slash != std::string::npos && dot < slash)) return fileName;
        return fileName.substr(0, dot);
    }
    bool FileExists(const std::string& fileName)
    {
        struct stat info;
        return stat(fileName.c_str(), &info) == 0;
    }

    bool ReadShape(const std::string& fileName, TopoDS_Shape& shape, const std::string& kind)
    {
        std::string type = kind;
        if (type.empty())
        {
            std::string extension = Extension(fileName);
            if (extension == ".stp" || extension == ".step") type = "STEP";
            else if (extension == ".igs" || extension == ".iges") type = "IGES";
            else type = "BREP";
        }
        if (!FileExists(fileName))
        {
            Error("The file " + fileName + " does not exist.");
            return false;
        }
        if (type == "STEP")
        {
            // The lengths are converted to millimetres like Netgen does
            Interface_Static::SetCVal("xstep.cascade.unit", "MM");
            STEPControl_Reader reader;
            if (reader.ReadFile(fileName.c_str()) != IFSelect_RetDone)
            {
                Error("The STEP file " + fileName + " could not be read.");
                return false;
            }
            reader.TransferRoots();
            shape = reader.OneShape();
        }
        else if (type == "IGES")
        {
            Interface_Static::SetCVal("xstep.cascade.unit", "MM");
            IGESControl_Reader reader;
            if (reader.ReadFile(fileName.c_str()) != IFSelect_RetDone)
            {
                Error("The IGES file " + fileName + " could not be read.");
                return false;
            }
            reader.TransferRoots();
            shape = reader.OneShape();
        }
        else
        {
            BRep_Builder builder;
            if (!BRepTools::Read(shape, fileName.c_str(), builder))
            {
                Error("The BREP file " + fileName + " could not be read.");
                return false;
            }
        }
        if (shape.IsNull())
        {
            Error("The file " + fileName + " contains no geometry.");
            return false;
        }
        return true;
    }
    bool WriteBrep(const TopoDS_Shape& shape, const std::string& fileName)
    {
        if (!BRepTools::Write(shape, fileName.c_str()))
        {
            Error("The BREP file " + fileName + " could not be written.");
            return false;
        }
        return true;
    }
    bool WriteStep(const TopoDS_Shape& shape, const std::string& fileName)
    {
        Interface_Static::SetCVal("write.step.unit", "MM");
        STEPControl_Writer writer;
        if (writer.Transfer(shape, STEPControl_AsIs) != IFSelect_RetDone || writer.Write(fileName.c_str()) != IFSelect_RetDone)
        {
            Error("The STEP file " + fileName + " could not be written.");
            return false;
        }
        return true;
    }

    ShapeMaps::ShapeMaps(const TopoDS_Shape& shape)
    {
        TopExp::MapShapes(shape, TopAbs_SOLID, solids);
        TopExp::MapShapes(shape, TopAbs_SHELL, shells);
        TopExp::MapShapes(shape, TopAbs_FACE, faces);
        TopExp::MapShapes(shape, TopAbs_EDGE, edges);
        TopExp::MapShapes(shape, TopAbs_VERTEX, vertices);
    }

    TopoDS_Shape SplitClosedFaces(const TopoDS_Shape& shape, int numOfSplitFaces)
    {
        if (numOfSplitFaces <= 1) return shape;
        ShapeUpgrade_ShapeDivideClosed divider(shape);
        divider.SetNbSplitPoints(numOfSplitFaces - 1);
        if (!divider.Perform()) return shape;
        TopoDS_Shape result = divider.Result();
        return result.IsNull() ? shape : result;
    }

    std::vector<TopoDS_Shape> GetParts(const TopoDS_Shape& shape)
    {
        std::vector<TopoDS_Shape> parts;
        TopTools_IndexedMapOfShape used;
        for (TopExp_Explorer solid(shape, TopAbs_SOLID); solid.More(); solid.Next())
        {
            if (used.Contains(solid.Current())) continue;
            used.Add(solid.Current());
            parts.push_back(solid.Current());
        }
        // Shells and faces of no solid
        TopTools_IndexedMapOfShape solidFaces;
        for (int i = 1; i <= used.Extent(); i++) TopExp::MapShapes(used(i), TopAbs_FACE, solidFaces);
        for (TopExp_Explorer shell(shape, TopAbs_SHELL, TopAbs_SOLID); shell.More(); shell.Next())
        {
            if (used.Contains(shell.Current())) continue;
            used.Add(shell.Current());
            parts.push_back(shell.Current());
            TopExp::MapShapes(shell.Current(), TopAbs_FACE, solidFaces);
        }
        for (TopExp_Explorer face(shape, TopAbs_FACE, TopAbs_SHELL); face.More(); face.Next())
        {
            if (solidFaces.Contains(face.Current()) || used.Contains(face.Current())) continue;
            used.Add(face.Current());
            parts.push_back(face.Current());
        }
        return parts;
    }

    std::map<std::string, std::string> ReadParameterFile(const std::string& fileName)
    {
        std::map<std::string, std::string> values;
        std::ifstream file(fileName);
        std::vector<std::string> lines;
        std::string line;
        while (std::getline(file, line))
        {
            if (!line.empty() && line.back() == '\r') line.pop_back();
            lines.push_back(line);
        }
        for (size_t i = 0; i + 1 < lines.size(); i++)
        {
            std::istringstream description(lines[i]);
            std::string type, name;
            description >> type >> name;
            if (type.empty() || name.empty()) continue;
            if (type != "int" && type != "double" && type != "char*") continue;
            values[name] = lines[i + 1];
            i++;
        }
        return values;
    }
    double GetDouble(const std::map<std::string, std::string>& values, const std::string& name, double defaultValue)
    {
        auto it = values.find(name);
        if (it == values.end() || it->second.empty()) return defaultValue;
        try { return std::stod(it->second); }
        catch (...) { return defaultValue; }
    }
    int GetInt(const std::map<std::string, std::string>& values, const std::string& name, int defaultValue)
    {
        auto it = values.find(name);
        if (it == values.end() || it->second.empty()) return defaultValue;
        try { return std::stoi(it->second); }
        catch (...) { return defaultValue; }
    }
    std::string GetString(const std::map<std::string, std::string>& values, const std::string& name)
    {
        auto it = values.find(name);
        return it == values.end() ? "" : it->second;
    }
}
