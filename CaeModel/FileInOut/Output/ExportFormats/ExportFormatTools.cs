// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.IO;

namespace FileInOut.Output
{
    public static class ExportFormatTools
    {
        // Methods
        public static string GetExtension(GeometryExportFormatEnum format)
        {
            switch (format)
            {
                case GeometryExportFormatEnum.Step: return ".stp";
                case GeometryExportFormatEnum.StepAssembly: return ".stp";
                case GeometryExportFormatEnum.Brep: return ".brep";
                case GeometryExportFormatEnum.BrepAssembly: return ".brep";
                case GeometryExportFormatEnum.Stl: return ".stl";
                case GeometryExportFormatEnum.Mmg: return ".mesh";
                default: throw new NotSupportedException();
            }
        }
        public static string GetExtension(MeshExportFormatEnum format)
        {
            switch (format)
            {
                case MeshExportFormatEnum.Calculix: return ".inp";
                case MeshExportFormatEnum.Abaqus: return ".inp";
                case MeshExportFormatEnum.Gmsh: return ".msh";
                case MeshExportFormatEnum.Mmg: return ".mesh";
                case MeshExportFormatEnum.Stl: return ".stl";
                default: throw new NotSupportedException();
            }
        }
        public static string GetExtension(ModelExportFormatEnum format)
        {
            switch (format)
            {
                case ModelExportFormatEnum.Calculix: return ".inp";
                case ModelExportFormatEnum.Abaqus: return ".inp";
                default: throw new NotSupportedException();
            }
        }
        public static string GetExtension(ResultsExportFormatEnum format)
        {
            switch (format)
            {
                case ResultsExportFormatEnum.Calculix: return ".inp";
                case ResultsExportFormatEnum.Stl: return ".stl";
                default: throw new NotSupportedException();
            }
        }
        //
        public static bool TryGetGeometryFormat(string fileName, out GeometryExportFormatEnum format)
        {
            switch (GetExtension(fileName))
            {
                case ".stp":
                case ".step": format = GeometryExportFormatEnum.Step; return true;
                case ".brep": format = GeometryExportFormatEnum.Brep; return true;
                case ".stl": format = GeometryExportFormatEnum.Stl; return true;
                case ".mesh": format = GeometryExportFormatEnum.Mmg; return true;
                default: format = GeometryExportFormatEnum.Step; return false;
            }
        }
        public static bool TryGetMeshFormat(string fileName, out MeshExportFormatEnum format)
        {
            switch (GetExtension(fileName))
            {
                case ".inp": format = MeshExportFormatEnum.Calculix; return true;
                case ".msh": format = MeshExportFormatEnum.Gmsh; return true;
                case ".mesh": format = MeshExportFormatEnum.Mmg; return true;
                case ".stl": format = MeshExportFormatEnum.Stl; return true;
                default: format = MeshExportFormatEnum.Calculix; return false;
            }
        }
        public static bool TryGetModelFormat(string fileName, out ModelExportFormatEnum format)
        {
            switch (GetExtension(fileName))
            {
                case ".inp": format = ModelExportFormatEnum.Calculix; return true;
                default: format = ModelExportFormatEnum.Calculix; return false;
            }
        }
        public static bool TryGetResultsFormat(string fileName, out ResultsExportFormatEnum format)
        {
            switch (GetExtension(fileName))
            {
                case ".inp": format = ResultsExportFormatEnum.Calculix; return true;
                case ".stl": format = ResultsExportFormatEnum.Stl; return true;
                default: format = ResultsExportFormatEnum.Calculix; return false;
            }
        }
        // The extension the user typed decides the format; the chosen filter decides only when the
        // extension is unknown or is shared by several formats
        public static GeometryExportFormatEnum GetGeometryFormat(string fileName, GeometryExportFormatEnum filterFormat)
        {
            GeometryExportFormatEnum format;
            if (!TryGetGeometryFormat(fileName, out format)) return filterFormat;
            if (GetExtension(format) == GetExtension(filterFormat)) return filterFormat;
            return format;
        }
        public static MeshExportFormatEnum GetMeshFormat(string fileName, MeshExportFormatEnum filterFormat)
        {
            MeshExportFormatEnum format;
            if (!TryGetMeshFormat(fileName, out format)) return filterFormat;
            if (GetExtension(format) == GetExtension(filterFormat)) return filterFormat;
            return format;
        }
        public static ResultsExportFormatEnum GetResultsFormat(string fileName, ResultsExportFormatEnum filterFormat)
        {
            ResultsExportFormatEnum format;
            if (!TryGetResultsFormat(fileName, out format)) return filterFormat;
            if (GetExtension(format) == GetExtension(filterFormat)) return filterFormat;
            return format;
        }
        //
        private static string GetExtension(string fileName)
        {
            if (fileName == null) return "";
            return Path.GetExtension(fileName).ToLowerInvariant();
        }
    }
}
