// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrePoMax
{
    public static class Globals
    {
        public static string HomePage = "https://prepomax.fs.um.si/";
        public static string ReleaseCandidate = "RC2";
        //
        public static string ProgramName
        {
            get
            {
                string name = "PrePoMax";
                Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
                name += " v" + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build;
                if (ReleaseCandidate != null && ReleaseCandidate.Length > 0) name += " " + ReleaseCandidate;
                else if (appVersion.Build != 0) name += " dev";
                return name;
            }
        }
        public static string WindowTitle
        {
            get { return ProgramName; }
        }
        // State names
        public static string ReadyText = "Ready";
        public static string RenderingText = "Rendering...";
        public static string ChangingView = "Changing view...";
        public static string OpeningText = "Opening...";
        public static string ImportingText = "Importing...";
        public static string SavingText = "Saving...";
        public static string SavingAsText = "Saving As...";
        public static string ExportingText = "Exporting...";
        public static string TransformingText = "Transforming...";
        public static string PreviewText = "Preview...";
        public static string MeshingText = "Meshing...";
        public static string UndoingText = "Undoing...";
        public static string CreatingCompoundText = "Creating compound...";
        public static string RegeneratingCompoundText = "Regenerating compound...";
        public static string RegeneratingText = "Regenerating history...";
        public static string FlippingNormalsText = "Flipping normals...";
        public static string SplittingGeometryText = "Splitting geometry...";
        public static string DefeaturingText = "Defeaturing...";
        public static string ExtractingSurfaceText = "Extracting surface...";
        public static string DeletingFacesText = "Deleting faces...";
        public static string FusingFacesText = "Fusing faces...";
        public static string RepairingGeometryText = "Repairing geometry...";
        public static string SplittingPeriodicFacesText = "Splitting periodic faces...";
        public static string MergingText = "Merging...";
        public static string ExplodePartsText = "Explode parts...";
        public static string SelectionText = "Selection...";
        public static string PythonScriptText = "Running Python script...";
        public static string AutomationText = "Automation server...";
        // Symbol names
        public static string SymbolNone = "None";
        public static string SymbolModel = "Model";
        public static string SymbolResult = "Result";
        // Color by names
        public static string ColorByNone = "None";
        // File names
        public static string SettingsFileName = "settings.bin";
        public static string MaterialLibraryFileName = "materials.lib";
        public static string HistoryFileName = "history.pmh";
        private static bool IsWindows { get { return System.Environment.OSVersion.Platform == System.PlatformID.Win32NT; } }
        // The paths of the programs PrePoMax runs are relative to the program folder; macOS uses slashes and its
        // executables have no extension
        private static string ProgramPath(string windowsPath)
        {
            if (IsWindows) return windowsPath;
            string path = windowsPath.Replace('\\', '/');
            if (path.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase)) path = path.Substring(0, path.Length - 4);
            return path;
        }
        // The settings, the history and the material library are written next to the program on Windows; the program
        // folder of a macOS application bundle is read-only, so they are kept in the application support folder there
        public static string GetUserDataFileName(string fileName, bool copyDelivered = false)
        {
            string directory = System.Windows.Forms.Application.StartupPath;
            if (System.Environment.OSVersion.Platform != System.PlatformID.Win32NT)
            {
                string userDirectory = System.IO.Path.Combine(System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.UserProfile), "Library", "Application Support", "PrePoMax");
                try
                {
                    System.IO.Directory.CreateDirectory(userDirectory);
                    // A file delivered with the program, such as the material library, is the starting version
                    string userFileName = System.IO.Path.Combine(userDirectory, fileName);
                    string defaultFileName = System.IO.Path.Combine(directory, fileName);
                    if (copyDelivered && !System.IO.File.Exists(userFileName) && System.IO.File.Exists(defaultFileName))
                        System.IO.File.Copy(defaultFileName, userFileName);
                    directory = userDirectory;
                }
                catch { }
            }
            return System.IO.Path.Combine(directory, fileName);
        }
        // The captured viewport is named after the model; this stands in before one is saved
        public static string ViewportImageBaseName = "Viewport";
        // Settings
        public static string GeneralSettingsName = "General";
        public static string NumberFormatSettingsName = "Number formatting";
        public static string GraphicsSettingsName = "Graphics";
        public static string NavigationSettingsName = "Navigation";
        public static string ColorSettingsName = "Default Colors";
        public static string AnnotationSettingsName = "Annotations";
        public static string MeshingSettingsName = "Meshing";
        public static string PreSettingsName = "Pre-processing";
        public static string SolverSettingsName = "Solver";
        public static string PostSettingsName = "Post-processing";
        public static string LegendSettingsName = "Legend";
        public static string StatusBlockSettingsName = "Status Block";
        public static string UnitSystemSettingsName = "Unit System";
        public static string AutomationSettingsName = "Automation";
        // Work files
        public static string NetGenMesher = ProgramPath(@"\Meshers\Netgen\NetGenMesher.exe");
        public static string MmgsMesher = ProgramPath(@"\Meshers\Mmg\mmgs.exe");
        public static string Mmg3DMesher = ProgramPath(@"\Meshers\Mmg\mmg3d.exe");
        public static string QuadWildMesher = ProgramPath(@"\Meshers\QuadWild\quadwild.exe");
        public static string QuadWildPatchMesher = ProgramPath(@"\Meshers\QuadWild\quad_from_patches.exe");
        // The macOS build places the Gmsh process next to PrePoMax, where it shares the libraries
        public static string GmshCaller = IsWindows ? @"\lib\GmshCaller.exe" : "/GmshCaller";
        public static string PythonDll = ProgramPath(@"\Python\python312.dll");
        public static string BidirectionalMeshConverter = ProgramPath(@"\Tools\BidirectionalMeshConverter.exe");
        public static string OdbToFrd = ProgramPath(@"\Tools\abqodb_to_ccxfrd.py");
        public static string TempFolder = ProgramPath(@"\Temp\");
        public static string VisFileName = "geometry.vis";
        public static string BrepFileName = "geometry.brep";
        public static string SplitSourceBrepFileName = "splitSource.brep";
        public static string SplitToolFileName = "splitTool";
        public static string TransformFileName = "transform";
        public static string StlFileName = "geometry.stl";        
        public static string MeshParametersFileName = "meshParameters";
        public static string LocalMeshSizesFileName = "localMeshSizes";
        public static string TransfiniteFacesFileName = "transfiniteFaces";
        public static string GmshMeshFileName = "gmsh.msh";
        public static string GmshDataFileName = "gmshData";
        public static string QuadWildGmshDataFileName = "quadwildGmshData";
        public static string QuadWildGmshInpFileName = "quadwildTriangles.inp";
        public static string QuadWildInputObjFileName = "quadwildInput.obj";
        public static string QuadWildSharpFileName = "quadwildInput.sharp";
        public static string QuadWildPrepConfigFileName = "quadwildPrep.txt";
        public static string QuadWildFlowConfigFileName = "quadwildFlow.txt";
        public static string VolFileName = "geometry.vol";
        public static string MmgMeshFileName = "mesh.mesh";
        public static string InpMeshFileName = "mesh.inp";
        public static string EdgeNodesFileName = "edgeNodes";
        // Names
        public static string NameSeparator = ":";        
        public static string MissingSectionName = "Missing_section";
        public static string NonpositiveJacobian = "Nonpositive_Jacobian";
        public static string RegenerateAll = "RegenerateAll";
        public static string FromFileOpenMenu = "FromFileOpenMenu";
        public static string FromMonitorForm = "FromMonitorForm";
        public static string FromLastSave = "FromLastSave";
        public static string OpenRunningJobResults = "OpenRunningJobResults";
        public static string FromAbaqus = "FromAbaqus";
        // Graphics
        public static int BeamNodeSize = 5;
        public static int ExplodedViewStepTimeMs = 400;
        // slection
        public static uint MaxSelectionBufferSizeMB = 200;
    }
}
