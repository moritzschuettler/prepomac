using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Reflection;

namespace PrePoMax
{
    public static class Globals
    {
        public static string HomePage = "https://prepomax.fs.um.si/";
        //
        public static string ProgramName
        {
            get
            {
                string name = "PrePoMax";
                Version appVersion = Assembly.GetExecutingAssembly().GetName().Version;
                name += " v" + appVersion.Major + "." + appVersion.Minor + "." + appVersion.Build;
                if (appVersion.Build != 0) name += " dev";
                return name;
            }
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
        public static string SplittingFacesText = "Splitting faces...";
        public static string DefeaturingText = "Defeaturing...";
        public static string DeletingFacesText = "Deleting faces...";
        public static string ExplodePartsText = "Explode parts...";
        public static string SelectionText = "Selection...";
        // File names
        public static string SettingsFileName = "settings.bin";
        public static string MaterialLibraryFileName = "materials.lib";
        public static string HistoryFileName = "history.pmh";
        // Settings
        public static string GeneralSettingsName = "General";
        public static string GraphicsSettingsName = "Graphics";
        public static string ColorSettingsName = "Default Colors";
        public static string AnnotationSettingsName = "Annotations";
        public static string MeshingSettingsName = "Meshing";
        public static string PreSettingsName = "Pre-processing";
        public static string CalculixSettingsName = "Calculix";
        public static string PostSettingsName = "Post-processing";
        public static string LegendSettingsName = "Legend";
        public static string StatusBlockSettingsName = "Status Block";
        public static string UnitSystemSettingsName = "Unit System";
        // Work files
        public static string NetGenMesher = @"\NetGen\NetGenMesher.exe";
        public static string MmgsMesher = @"\NetGen\mmgs.exe";
        public static string Mmg3DMesher = @"\NetGen\mmg3d.exe";
        public static string GmshCaller = @"\lib\GmshCaller.exe";
        public static string VisFileName = "geometry.vis";
        public static string BrepFileName = "geometry.brep";
        public static string StlFileName = "geometry.stl";        
        public static string MeshParametersFileName = "meshParameters";
        public static string MeshRefinementFileName = "meshRefinement";
        public static string GmshMeshFileName = "gmsh.msh";
        public static string GmshDataFileName = "gmshData";
        public static string VolFileName = "geometry.vol";
        public static string MmgMeshFileName = "mesh.mesh";
        public static string InpMeshFileName = "mesh.inp";
        public static string EdgeNodesFileName = "edgeNodes";
        // Names
        public static string NameSeparator = ":";        
        public static string MissingSectionName = "Missing_section";
        public static string RegenerateAll = "RegenerateAll";
        public static string FromFileOpenMenu = "FromFileOpenMenu";
        public static string FromMonitorForm = "FromMonitorForm";
        public static string OpenRunningJobResults = "OpenRunningJobResults";
        // Graphics
        public static int BeamNodeSize = 5;
        // slection
        public static int SelectionBufferSize = 20;
    }
}
