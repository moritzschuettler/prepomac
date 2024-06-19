using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using CaeGlobals;
using CaeModel;
using CaeMesh;
using CaeJob;
using CaeResults;
using System.Xml.Linq;
using System.Security.Cryptography;
using static CaeGlobals.Geometry2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UserControls
{
    public enum ViewType
    {
        Geometry,
        Model,
        Results
    }
    public struct ContextMenuFields
    {
        public int Create;
        public int Edit;
        public int EditStepControls;
        public int Query;
        public int Duplicate;
        public int Propagate;
        public int Preview;
        //
        public int CompoundPart;
        public int SwapParts;
        //
        public int MeshingParameters;
        public int PreviewEdgeMesh;
        public int CreateMesh;
        //
        public int CopyPartToGeometry;
        //
        public int EditCalculixKeywords;
        //
        public int MergePart;
        //
        public int ConvertToPart;
        //
        public int MaterialLibrary;
        //
        public int SearchContactPairs;
        public int SwapMergeMasterSlave;
        //
        public int Hide;
        public int Show;
        public int Transparency;
        //
        public int Deformed;
        public int ColorContours;
        //
        public int Run;
        public int Monitor;
        public int Results;
        public int Kill;
        //
        public int Activate;
        public int Deactivate;
        //
        public int Expand;
        public int Collapse;
        //
        public int Delete;
    }
    public enum HideShowOperation
    {
        Hide,
        Show,
        ShowOnly
    }


    public partial class ModelTree : UserControl
    {
        // Variables                                                                                                                
        private bool _screenUpdating;
        private bool _doubleClick;
        private const int WM_MOUSEMOVE = 0x0200;
        private bool _disableMouse;
        private bool _disableSelectionsChanged;
        private Dictionary<CodersLabTreeView, bool[]> _prevStates;
        // Geometry
        private TreeNode _geomParts;                // 1
        private TreeNode _meshSetupItems;           // 1
        // Model
        private TreeNode _model;                    // 1
        private TreeNode _modelMesh;                //   2
        private TreeNode _modelParts;               //     3
        private TreeNode _modelNodeSets;            //     3
        private TreeNode _modelElementSets;         //     3
        private TreeNode _modelSurfaces;            //     3
        private TreeNode _modelFeatures;            //   2
        private TreeNode _modelReferencePoints;     //     3
        private TreeNode _modelCoordinateSystems;   //     3
        private TreeNode _materials;                //   2
        private TreeNode _sections;                 //   2
        private TreeNode _constraints;              //   2
        private TreeNode _contacts;                 //   2
        private TreeNode _surfaceInteractions;      //      3
        private TreeNode _contactPairs;             //      3
        private TreeNode _amplitudes;               //   2
        private TreeNode _initialConditions;        //   2
        private TreeNode _steps;                    //   2
        private TreeNode _analyses;                 // 1
        // Results
        private TreeNode _resultModel;              // 1
        private TreeNode _resultMesh;               //   2
        private TreeNode _resultParts;              //     3
        private TreeNode _resultNodeSets;           //     3
        private TreeNode _resultElementSets;        //     3
        private TreeNode _resultSurfaces;           //     3
        private TreeNode _resultFeatures;           //   2
        private TreeNode _resultReferencePoints;    //     3
        private TreeNode _resultCoordinateSystems;  //     3
        private TreeNode _results;                  // 1
        private TreeNode _resultFieldOutputs;       //   2
        private TreeNode _resultHistoryOutputs;     //   2
        // Geometry
        private string _geomPartsName = "Parts";
        private string _meshSetupItemsName = "Mesh Setup";
        // Model
        private string _modelName = "Model";
        private string _modelMeshName = "Mesh";
        private string _modelPartsName = "Parts";
        private string _modelNodeSetsName = "Node Sets";
        private string _modelElementSetsName = "Element Sets";
        private string _modelSurfacesName = "Surfaces";
        private string _modelFeaturesName = "Features";
        private string _modelReferencePointsName = "Reference Points";
        private string _modelCoordinateSystemsName = "Coordinate Systems";
        private string _materialsName = "Materials";
        private string _sectionsName = "Sections";
        private string _constraintsName = "Constraints";
        private string _contactsName = "Contacts";
        private string _surfaceInteractionsName = "Surface Interactions";
        private string _contactPairsName = "Contact Pairs";
        private string _amplitudesName = "Amplitudes";
        private string _initialConditionsName = "Initial Conditions";
        private string _stepsName = "Steps";
        private string _modelFieldOutputsName = "Field Outputs";
        private string _modelHistoryOutputsName = "History Outputs";
        private string _boundaryConditionsName = "BCs";
        private string _loadsName = "Loads";
        private string _definedFieldsName = "Defined Fields";
        private string _analysesName = "Analyses";
        // Results
        private string _resultModelName = "Model";
        private string _resultMeshName = "Mesh";
        private string _resultPartsName = "Parts";
        private string _resultNodeSetsName = "Node Sets";
        private string _resultElementSetsName = "Element Sets";
        private string _resultSurfacesName = "Surfaces";
        private string _resultFeaturesName = "Features";
        private string _resultReferencePointsName = "Reference Points";
        private string _resultCoordinateSystemsName = "Coordinate Systems";
        private string _resultsName = "Results";
        private string _resultFieldOutputsName = "Field Outputs";
        private string _resultHistoryOutputsName = "History Outputs";


        // Properties                                                                                                               
        public bool ScreenUpdating
        {
            get { return _screenUpdating; }
            set { _screenUpdating = value; }
        }
        public bool DisableSelectionsChanged
        {
            get { return _disableSelectionsChanged; }
            set { _disableSelectionsChanged = value; }
        }
        public bool DisableMouse
        {
            get { return _disableMouse; }
            set
            {
                _disableMouse = value;
                cltvGeometry.DisableMouse = value;
                cltvModel.DisableMouse = value;
                cltvResults.DisableMouse = value;
                //
                stbGeometry.Enabled = !value;
                stbModel.Enabled = !value;
                stbResults.Enabled = !value;
            }
        }
        public bool DisableGeometryAndModelTreeMouse
        {
            set
            {
                cltvGeometry.DisableMouse = value;
                cltvModel.DisableMouse = value;
                //
                stbGeometry.Enabled = !value;
                stbModel.Enabled = !value;
            }
        }
        public bool DisableResultsTreeMouse
        {
            set
            {
                cltvResults.DisableMouse = value;
                //
                stbResults.Enabled = !value;
            }
        }
        public string[] IntersectSelectionWithList(NamedClass[] list)
        {
            List<string> selected = new List<string>();

            CodersLabTreeView tree = GetActiveTree();
            foreach (TreeNode node in tree.SelectedNodes)
            {
                if (node.Tag != null && list.Contains(node.Tag))
                    selected.Add(node.Text);
            }

            return selected.ToArray();
        }
        //
        public string MeshSetupItemsName { get { return _meshSetupItemsName; } }
        public string NodeSetsName { get { return _modelNodeSetsName; } }
        public string ElementSetsName { get { return _modelElementSetsName; } }
        public string SurfacesName { get { return _modelSurfacesName; } }
        public string ModelReferencePointsName { get { return _modelReferencePointsName; } }
        public string ModelCoordinateSystemsName { get { return _modelCoordinateSystemsName; } }
        public string MaterialsName { get { return _materialsName; } }
        public string SectionsName { get { return _sectionsName; } }
        public string ConstraintsName { get { return _constraintsName; } }
        public string SurfaceInteractionsName { get { return _surfaceInteractionsName; } }
        public string ContactPairsName { get { return _contactPairsName; } }
        public string AmplitudesName { get { return _amplitudesName; } }
        public string InitialConditionsName { get { return _initialConditionsName; } }
        public string StepsName { get { return _stepsName; } }
        public string ModelHistoryOutputsName { get { return _modelHistoryOutputsName; } }
        public string ModelFieldOutputsName { get { return _modelFieldOutputsName; } }
        public string BoundaryConditionsName { get { return _boundaryConditionsName; } }
        public string LoadsName { get { return _loadsName; } }
        public string DefinedFieldsName { get { return _definedFieldsName; } }
        public string AnalysesName { get { return _analysesName; } }
        //Results
        public string ResultReferencePointsName { get { return _resultReferencePointsName; } }
        public string ResultCoordinateSystemsName { get { return _resultCoordinateSystemsName; } }
        public string ResultHistoryOutputsName { get { return _resultHistoryOutputsName; } }
        public string ResultFieldOutputsName { get { return _resultFieldOutputsName; } }


        // Events                                                                                                                   
        public event Action<ViewType> GeometryMeshResultsEvent;
        public event Action<NamedClass[]> SelectEvent;
        public event Action ClearSelectionEvent;
        //
        public event Action<string, string> CreateEvent;
        public event Action<NamedClass, string> EditEvent;
        public event Action<string> EditStepControlsEvent;
        public event Action QueryEvent;
        public event Action<NamedClass[], string[]> DuplicateEvent;
        public event Action<NamedClass[], string[]> PropagateEvent;
        public event Action<NamedClass[], string[]> PreviewEvent;
        public event Action<string[]> CreateCompoundPart;
        public event Action<string[]> SwapPartGeometries;
        public event Func<string[], MeshSetupItem, Task> PreviewEdgeMesh;
        public event Action<string[]> CreateMeshEvent;
        public event Action<string[]> CopyGeometryToResultsEvent;
        public event Action EditCalculixKeywords;
        public event Action<string[]> MergeParts;
        public event Action<string[]> MergeResultParts;
        public event Action<string[]> ConvertElementSetsToMeshParts;
        public event Action MaterialLibrary;
        public event Action SearchContactPairs;
        public event Action<NamedClass[]> SwapMasterSlave;
        public event Action<NamedClass[]> MergeByMasterSlave;
        public event Action<NamedClass[], HideShowOperation, string[]> HideShowEvent;
        public event Action<string[]> SetTransparencyEvent;
        public event Action<NamedClass[], bool> ColorContoursVisibilityEvent;
        public event Action<string> RunEvent;
        public event Action<string> CheckModelEvent;
        public event Action<string> MonitorEvent;
        public event Action<string> ResultsEvent;
        public event Action<string> KillEvent;
        public event Action<NamedClass[], bool, string[]> ActivateDeactivateEvent;
        public event Action<NamedClass[], string[]> DeleteEvent;
        //
        public event Action<string[]> FieldDataSelectEvent;
        public event Action RenderingOn;
        public event Action RenderingOff;


        // Callbacks                                                                                                                
        public Action RegenerateTreeCallBack;


        // Constructors                                                                                                             
        public ModelTree()
        {
            InitializeComponent();
            // Geometry
            _geomParts = cltvGeometry.Nodes.Find(_geomPartsName, true)[0];
            _meshSetupItems = cltvGeometry.Nodes.Find(_meshSetupItemsName, true)[0];
            // Model
            _model = cltvModel.Nodes.Find(_modelName, true)[0];
            _modelMesh = cltvModel.Nodes.Find(_modelMeshName, true)[0];
            _modelParts = cltvModel.Nodes.Find(_modelPartsName, true)[0];
            _modelNodeSets = cltvModel.Nodes.Find(_modelNodeSetsName, true)[0];
            _modelElementSets = cltvModel.Nodes.Find(_modelElementSetsName, true)[0];
            _modelSurfaces = cltvModel.Nodes.Find(_modelSurfacesName, true)[0];
            _modelFeatures = cltvModel.Nodes.Find(_modelFeaturesName, true)[0];
            _modelReferencePoints = cltvModel.Nodes.Find(_modelReferencePointsName, true)[0];
            _modelCoordinateSystems = cltvModel.Nodes.Find(_modelCoordinateSystemsName, true)[0];
            _materials = cltvModel.Nodes.Find(_materialsName, true)[0];
            _sections = cltvModel.Nodes.Find(_sectionsName, true)[0];
            _constraints = cltvModel.Nodes.Find(_constraintsName, true)[0];
            _contacts = cltvModel.Nodes.Find(_contactsName, true)[0];
            _surfaceInteractions = cltvModel.Nodes.Find(_surfaceInteractionsName, true)[0];
            _contactPairs = cltvModel.Nodes.Find(_contactPairsName, true)[0];
            _amplitudes = cltvModel.Nodes.Find(_amplitudesName, true)[0];
            _initialConditions = cltvModel.Nodes.Find(_initialConditionsName, true)[0];
            _steps = cltvModel.Nodes.Find(_stepsName, true)[0];
            _analyses = cltvModel.Nodes.Find(_analysesName, true)[0];
            // Results
            _resultModel = cltvResults.Nodes.Find(_resultModelName, true)[0];
            _resultMesh = cltvResults.Nodes.Find(_resultMeshName, true)[0];
            _resultParts = cltvResults.Nodes.Find(_resultPartsName, true)[0];
            _resultNodeSets = cltvResults.Nodes.Find(_resultNodeSetsName, true)[0];
            _resultElementSets = cltvResults.Nodes.Find(_resultElementSetsName, true)[0];
            _resultSurfaces = cltvResults.Nodes.Find(_resultSurfacesName, true)[0];
            _resultFeatures = cltvResults.Nodes.Find(_resultFeaturesName, true)[0];
            _resultReferencePoints = cltvResults.Nodes.Find(_resultReferencePointsName, true)[0];
            _resultCoordinateSystems = cltvResults.Nodes.Find(_resultCoordinateSystemsName, true)[0];
            _results = cltvResults.Nodes.Find(_resultsName, true)[0];
            _resultFieldOutputs = cltvResults.Nodes.Find(_resultFieldOutputsName, true)[0];
            _resultHistoryOutputs = cltvResults.Nodes.Find(_resultHistoryOutputsName, true)[0];
            //
            // Add NamedClasses to static items
            _model.Tag = new EmptyNamedClass(typeof(FeModel).ToString());
            // Geometry icons
            _geomParts.StateImageKey = "GeomPart";
            // Model icons
            _modelMesh.StateImageKey = "Mesh";
            _modelParts.StateImageKey = "BasePart";
            _modelNodeSets.StateImageKey = "Node_set";
            _modelElementSets.StateImageKey = "Element_set";
            _modelSurfaces.StateImageKey = "Surface";
            _modelFeatures.StateImageKey = "Features";
            _modelReferencePoints.StateImageKey = "Reference_point";
            _modelCoordinateSystems.StateImageKey = "CoordinateSystem";
            _materials.StateImageKey = "Material";
            _sections.StateImageKey = "Section";
            _constraints.StateImageKey = "Constraints";
            _surfaceInteractions.StateImageKey = "SurfaceInteractions";
            _contactPairs.StateImageKey = "ContactPairs";
            _amplitudes.StateImageKey = "Amplitudes";
            _initialConditions.StateImageKey = "InitialConditions";
            _steps.StateImageKey = "Step";
            _analyses.StateImageKey = "Bc";
            // Results icons
            _resultMesh.StateImageKey = "Mesh";
            _resultParts.StateImageKey = "BasePart";
            _resultNodeSets.StateImageKey = "Node_set";
            _resultElementSets.StateImageKey = "Element_set";
            _resultSurfaces.StateImageKey = "Surface";
            _resultFeatures.StateImageKey = "Features";
            _resultReferencePoints.StateImageKey = "Reference_point";
            _resultCoordinateSystems.StateImageKey = "CoordinateSystem";
            _resultFieldOutputs.StateImageKey = "Field_output";
            _resultHistoryOutputs.StateImageKey = "History_output";
            //
            _doubleClick = false;
            _screenUpdating = true;
            _prevStates = new Dictionary<CodersLabTreeView, bool[]>();
            _prevStates.Add(cltvGeometry, null);
            _prevStates.Add(cltvModel, null);
            _prevStates.Add(cltvResults, null);
            //
            Clear();
        }


        // Event hadlers                                                                                                            
        private void tcGeometryModelResults_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            e.Cancel = _disableMouse;
        }
        private void stbGeometry_TextChanged(object sender, EventArgs e)
        {
            FilterTree(cltvGeometry, stbGeometry.Text);
        }
        private void stbModel_TextChanged(object sender, EventArgs e)
        {
            FilterTree(cltvModel, stbModel.Text);
        }
        private void stbResults_TextChanged(object sender, EventArgs e)
        {
            FilterTree(cltvResults, stbResults.Text);
        }


        #region Geometry-Model-Results
        private ViewType GetViewType()
        {
            if (tcGeometryModelResults.SelectedTab == tpGeometry) return ViewType.Geometry;
            else if (tcGeometryModelResults.SelectedTab == tpModel) return ViewType.Model;
            else if (tcGeometryModelResults.SelectedTab == tpResults) return ViewType.Results;
            else throw new NotSupportedException();
        }
        private void tcGeometryModelResults_SelectedIndexChanged(object sender, EventArgs e)
        {
            GeometryMeshResultsEvent?.Invoke(GetViewType());
        }
        public void SetGeometryTab()
        {
            tcGeometryModelResults.SelectedIndexChanged -= tcGeometryModelResults_SelectedIndexChanged;
            bool prevMouseState = _disableMouse;
            _disableMouse = false;
            tcGeometryModelResults.SelectedTab = tpGeometry;
            _disableMouse = prevMouseState;
            tcGeometryModelResults.SelectedIndexChanged += tcGeometryModelResults_SelectedIndexChanged;
        }
        public void SetModelTab()
        {
            tcGeometryModelResults.SelectedIndexChanged -= tcGeometryModelResults_SelectedIndexChanged;
            bool prevMouseState = _disableMouse;
            _disableMouse = false;
            tcGeometryModelResults.SelectedTab = tpModel;
            _disableMouse = prevMouseState;
            tcGeometryModelResults.SelectedIndexChanged += tcGeometryModelResults_SelectedIndexChanged;
        }
        public void SetResultsTab()
        {
            tcGeometryModelResults.SelectedIndexChanged -= tcGeometryModelResults_SelectedIndexChanged;
            bool prevMouseState = _disableMouse;
            _disableMouse = false;
            tcGeometryModelResults.SelectedTab = tpResults;
            _disableMouse = prevMouseState;
            tcGeometryModelResults.SelectedIndexChanged += tcGeometryModelResults_SelectedIndexChanged;
        }
        #endregion

        #region Tree event handlers
        private void PrepareToolStripItem(CodersLabTreeView tree)
        {
            int n = tree.SelectedNodes.Count;
            //
            ContextMenuFields menuFields = new ContextMenuFields();
            foreach (TreeNode node in tree.SelectedNodes)
            {
                AppendMenuFields(node, ref menuFields);
            }
            bool oneAboveVisible = false;
            // Visibility                                                                                           
            bool visible;
            // Create
            visible = menuFields.Create == n;
            tsmiCreate.Visible = visible;
            oneAboveVisible |= visible;
            // Edit
            visible = menuFields.Edit == n;
            tsmiEdit.Visible = visible;
            oneAboveVisible |= visible;
            // Edit step controls
            visible = menuFields.EditStepControls == n;
            tsmiEditStepControls.Visible = visible;
            oneAboveVisible |= visible;
            // Query
            visible = menuFields.Query > 0;
            tsmiQuery.Visible = visible;
            oneAboveVisible |= visible;
            // Duplicate
            visible = menuFields.Duplicate == n;
            tsmiDuplicate.Visible = visible;
            oneAboveVisible |= visible;
            // Propagate
            visible = menuFields.Propagate == n;
            tsmiPropagate.Visible = visible;
            oneAboveVisible |= visible;
            // Preview
            visible = menuFields.Preview == n;
            tsmiPreview.Visible = visible;
            oneAboveVisible |= visible;
            //Geometry                                              
            visible = menuFields.CompoundPart == n && n > 1;
            tsmiSpaceCompoundPart.Visible = visible && oneAboveVisible;
            tsmiCompoundPart.Visible = visible;
            visible = menuFields.SwapParts == n && n == 2;
            tsmiSwapPartGeometries.Visible = visible;
            oneAboveVisible |= visible;
            //Mesh                                                  
            visible = menuFields.MeshingParameters == n;
            tsmiSpaceMesh.Visible = visible && oneAboveVisible;
            tsmiPreviewEdgeMesh.Visible = visible;
            tsmiCreateMesh.Visible = visible;
            // Copy part                                            
            tsmiSpaceCopyPart.Visible = visible;
            tsmiCopyGeometryToResults.Visible = visible;
            oneAboveVisible |= visible;
            // Edit Calculix Keywords                               
            visible = menuFields.EditCalculixKeywords == n && n > 0;
            tsmiSpaceEditCalculiXKeywords.Visible = visible && oneAboveVisible;
            tsmiEditCalculiXKeywords.Visible = visible;
            oneAboveVisible |= visible;
            // Merge mesh parts                                     
            visible = menuFields.MergePart == n && n > 1;
            tsmiSpaceMergeParts.Visible = visible && oneAboveVisible;
            tsmiMergeParts.Visible = visible;
            oneAboveVisible |= visible;
            // Convert element set                                  
            visible = menuFields.ConvertToPart == n;
            tsmiSpaceConvertToPart.Visible = visible && oneAboveVisible;
            tsmiConvertToPart.Visible = visible;
            oneAboveVisible |= visible;
            // Material library                                     
            visible = menuFields.MaterialLibrary == n && n > 0;
            tsmiSpaceMaterialLibrary.Visible = visible && oneAboveVisible;
            tsmiMaterialLibrary.Visible = visible;
            oneAboveVisible |= visible;
            // Search contact pairs                                 
            visible = menuFields.SearchContactPairs == n && n > 0;
            tsmiSpaceSearchContactPairs.Visible = visible && oneAboveVisible;
            tsmiSearchContactPairs.Visible = visible;
            oneAboveVisible |= visible;
            // Swap Merge Master/Slave                              
            visible = menuFields.SwapMergeMasterSlave == n && n > 0;
            tsmiSpaceSwapMergeMasterSlave.Visible = visible && oneAboveVisible;
            tsmiSwapMasterSlave.Visible = visible;
            tsmiMergeByMasterSlave.Visible = visible && n > 1;
            oneAboveVisible |= visible;
            // Hide/Show                                            
            visible = menuFields.Hide + menuFields.Show == n;
            tsmiSpaceHideShow.Visible = visible && oneAboveVisible;
            tsmiHide.Visible = visible;
            tsmiShow.Visible = visible;
            tsmiShowOnly.Visible = visible;
            // Transparency                                         
            tsmiSetTransparency.Visible = menuFields.Transparency == n;
            oneAboveVisible |= visible; // Hide/Show
            // Deformed/Color contours                              
            visible = menuFields.Deformed == n;
            tsmiSpaceColorContours.Visible = visible && oneAboveVisible;
            tsmiColorContoursOff.Visible = visible;
            tsmiColorContoursOn.Visible = visible;
            oneAboveVisible |= visible;
            // Analysis                                             
            visible = menuFields.Run == n;
            tsmiSpaceAnalysis.Visible = visible && oneAboveVisible;
            tsmiRun.Visible = visible;
            tsmiCheckModel.Visible = visible;
            tsmiMonitor.Visible = visible;
            tsmiResults.Visible = visible;
            tsmiKill.Visible = visible;
            oneAboveVisible |= visible;
            // Activate/Deactivate                                  
            visible = menuFields.Activate + menuFields.Deactivate == n;
            tsmiSpaceActive.Visible = visible && oneAboveVisible;
            tsmiActivate.Visible = visible;
            tsmiDeactivate.Visible = visible;
            oneAboveVisible |= visible;
            //Expand / Collapse                                     
            tsmiSpaceExpandColapse.Visible = oneAboveVisible;
            tsmiExpandAll.Visible = true;
            tsmiCollapseAll.Visible = true;
            oneAboveVisible = true;
            // Delete                                               
            visible = menuFields.Delete == n;
            tsmiSpaceDelete.Visible = visible && oneAboveVisible;
            tsmiDelete.Visible = visible;
            oneAboveVisible |= visible;

            // Enabled                                                                                              
            bool enabled;
            // Create
            enabled = menuFields.Create == 1;
            tsmiCreate.Enabled = enabled;
            // Edit
            enabled = menuFields.Edit == 1;
            tsmiEdit.Enabled = enabled;
            // Mesh
            tsmiCreateMesh.Enabled = true;
            // Copy part
            tsmiCopyGeometryToResults.Enabled = true;
            // Material library
            tsmiMaterialLibrary.Enabled = true;
            // Hide/Show
            tsmiHide.Enabled = true;
            tsmiShow.Enabled = true;
            tsmiShowOnly.Enabled = true;
            // Deformed/Color contours
            tsmiColorContoursOff.Enabled = true;
            tsmiColorContoursOn.Enabled = true;
            // Analysis
            enabled = menuFields.Run == 1;
            tsmiRun.Enabled = enabled;
            tsmiMonitor.Enabled = enabled;
            tsmiResults.Enabled = enabled;
            tsmiKill.Enabled = enabled;
            // Activate/Deactivate 
            tsmiActivate.Enabled = true;
            tsmiDeactivate.Enabled = true;
            // Delete
            tsmiDelete.Enabled = true;
        }
        //
        private void AppendMenuFields(TreeNode node, ref ContextMenuFields menuFields)
        {
            // Check if selected node is Model
            // Edit Calculix Keywords
            if (node == _model)
            {
                menuFields.Edit++;
                menuFields.EditCalculixKeywords++;
                return;
            }
            //
            NamedClass item = (NamedClass)node.Tag;
            bool subPart = node.Parent != null && node.Parent.Tag is CompoundGeometryPart;
            // Create
            if (CanCreate(node)) menuFields.Create++;
            // Edit
            if (item != null)
            {
                if (item is Field ||
                    item is HistoryResultSet ||
                    item is HistoryResultField) { }
                else if (node.TreeView == cltvResults &&
                         (item is FeNodeSet ||
                          item is FeElementSet ||
                          item is FeSurface)) { }
                else menuFields.Edit++;
            }
            // Edit step controls
            if (item != null && item is Step) menuFields.EditStepControls++;
            // Query
            if (item != null && item is BasePart) menuFields.Query++;
            //Duplicate
            if (item != null && CanDuplicate(node)) menuFields.Duplicate++;
            //Propagate
            if (item != null && CanPropagate(node)) menuFields.Propagate++;
            //Propagate
            if (item != null && CanPreview(node)) menuFields.Preview++;
            // Geometry part - Geometry
            if (item != null && item is GeometryPart && GetActiveTree() == cltvGeometry)
            {
                menuFields.CompoundPart++;
                menuFields.SwapParts++;
            }
            // Geometry part - Mesh
            if (item != null && item is GeometryPart && GetActiveTree() == cltvGeometry)
            {
                if (!subPart)
                {
                    menuFields.MeshingParameters++;
                    menuFields.PreviewEdgeMesh++;
                    menuFields.CreateMesh++;
                }
                menuFields.CopyPartToGeometry++;
            }
            // Merge mesh parts
            if (item != null && item is MeshPart && GetActiveTree() == cltvModel) menuFields.MergePart++;
            if (item != null && item is ResultPart && GetActiveTree() == cltvResults) menuFields.MergePart++;
            // Convert element set to part
            if (item != null && item is FeElementSet && GetActiveTree() == cltvModel) menuFields.ConvertToPart++;
            // Material library
            if (node == _materials) menuFields.MaterialLibrary++;
            // Swap Merge Master/Slave
            if (CanSearchContactPairs(node)) menuFields.SearchContactPairs++;
            if (item != null && CanSwapMergeMasterSlave(node)) menuFields.SwapMergeMasterSlave++;
            // Hide/Show
            if (item != null && CanHide(item))
            {
                if (item.Visible) menuFields.Hide++;
                else menuFields.Show++;
            }
            //Transparency
            if (item != null && item is BasePart) menuFields.Transparency++;
            // Deformed/Color contours
            if (item != null && item is ResultPart)
            {
                if (!ResultPart.Undeformed)
                {
                    menuFields.Deformed++;
                    menuFields.ColorContours++;
                }
            }
            // Analysis
            if (node.Parent == _analyses)
            {
                menuFields.Run++;
                menuFields.Monitor++;
                menuFields.Results++;
                menuFields.Kill++;
            }
            // Activate/Deactivate 
            if (item != null && CanDeactivate(node))
            {
                if (item.Active) menuFields.Deactivate++;
                else menuFields.Activate++;
            }
            // Delete
            if (item != null && !subPart)
            {
                if (node.TreeView == cltvResults &&
                    (item is FeNodeSet ||
                     item is FeElementSet ||
                     item is FeSurface)) { }
                else menuFields.Delete++;
            }
        }
        //
        private void cltv_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                CodersLabTreeView tree = (CodersLabTreeView)sender;
                //
                if (ModifierKeys != Keys.Shift && ModifierKeys != Keys.Control && e.Clicks > 1) _doubleClick = true;
                else _doubleClick = false;
                //
                TreeNode node = tree.GetNodeAt(e.Location);
                if (node == null)
                {
                    tree.SelectedNodes.Clear();
                    ClearSelectionEvent();
                }
            }
            catch
            { }
        }
        private void cltv_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                CodersLabTreeView tree = (CodersLabTreeView)sender;
                //
                if (e.Button == MouseButtons.Right)
                {
                    if (tree.SelectedNodes.Count > 0)
                    {
                        PrepareToolStripItem(tree);
                        cmsTree.Show(tree, e.Location);
                    }
                }
            }
            catch
            { }
        }
        private void cltv_SelectionsChanged(object sender, EventArgs e)
        {
            if (_disableSelectionsChanged) return;
            // This function is also called with sender as null parameter
            CodersLabTreeView tree = GetActiveTree();
            TreeNode node;
            List<NamedClass> items = new List<NamedClass>();
            //
            if (!_doubleClick && tree.SelectedNodes.Count > 0)
            {
                // Select field data
                if (tree.SelectedNodes.Count == 1)
                {
                    node = tree.SelectedNodes[0];
                    // Results
                    if (node.Tag is FieldData)
                    {
                        SelectEvent?.Invoke(null);      // clear selection
                        FieldDataSelectEvent?.Invoke(new string[] { node.Parent.Name, node.Name });
                        ActiveControl = cltvResults;    // this is for the arrow keys to work on the results tree
                        return;
                    }
                }
                // Select
                TreeNode lastNode = null;
                foreach (TreeNode selectedNode in tree.SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    lastNode = selectedNode;
                }
                if (lastNode != null) lastNode.EnsureVisible();
                //
                SelectEvent?.Invoke(items.ToArray());
            }
            else if (tree.SelectedNodes.Count == 0) ClearSelectionEvent();
            //
            return;
        }
        private void cltv_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CodersLabTreeView tree = GetActiveTree();
            if (tree.SelectedNodes.Count != 1) return;
            //
            TreeNode selectedNode = tree.SelectedNodes[0];
            //
            if (selectedNode == null || ModifierKeys == Keys.Shift || ModifierKeys == Keys.Control) return;
            //
            if (selectedNode == tree.HitTest(e.Location).Node)
            {
                if (selectedNode.Tag == null)
                {
                    if (CanCreate(selectedNode)) tsmiCreate_Click(null, null);
                    else
                    {
                        _doubleClick = false;   // must be here to allow expand/collapse
                        //
                        if (selectedNode.IsExpanded) selectedNode.Collapse();
                        else selectedNode.Expand();
                    }
                }
                else tsmiEdit_Click(null, null);
            }
            _doubleClick = false;
        }
        private void cltv_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (_doubleClick == true && e.Action == TreeViewAction.Expand)
            {
                e.Cancel = true;
            }
        }
        private void cltv_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            if (_doubleClick == true && e.Action == TreeViewAction.Collapse)
            {
                e.Cancel = true;
            }
        }
        private void cltv_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            CodersLabTreeView tree = (CodersLabTreeView)sender;
            foreach (TreeNode node in tree.Nodes) SetAllNodesStatusIcons(node);
        }
        private void cltv_AfterExpand(object sender, TreeViewEventArgs e)
        {
            CodersLabTreeView tree = (CodersLabTreeView)sender;
            foreach (TreeNode node in tree.Nodes) SetAllNodesStatusIcons(node);
        }
        public void cltv_KeyDown(object sender, KeyEventArgs e)
        {
            CodersLabTreeView tree = GetActiveTree();
            //
            //if (tree.Focused) // Disable since called from FrmMain.KeyboardHook_KeyDown
            {
                if (e.KeyCode == Keys.Delete)
                {
                    tsmiDelete_Click(null, null);
                }
                else if (e.KeyCode == Keys.Space)
                {
                    tsmiInvertHideShow_Click(null, null);
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (tree.SelectedNodes.Count == 1 && tree.SelectedNodes[0].Tag == null) tsmiCreate_Click(null, null);
                    else tsmiEdit_Click(null, null);
                }
                // No beep
                e.SuppressKeyPress = true;
            }
        }

        #endregion

        #region Tree context menu
        private void tsmiCreate_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                //
                if (selectedNode.Tag == null)
                {
                    if (CanCreate(selectedNode))
                    {
                        string stepName = null;
                        if (selectedNode.Parent != null && selectedNode.Parent.Tag is Step)
                            stepName = selectedNode.Parent.Name;
                        //
                        CreateEvent?.Invoke(selectedNode.Name, stepName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEdit_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                //
                if (selectedNode.Tag == null) return;
                //
                string stepName = null;
                if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                    stepName = selectedNode.Parent.Parent.Name;
                //
                EditEvent?.Invoke((NamedClass)selectedNode.Tag, stepName);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditStepControls_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                //
                if (selectedNode.Tag == null) return;
                //
                if (selectedNode.Tag is Step step) EditStepControlsEvent?.Invoke(step.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiQuery_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in GetActiveTree().SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0) QueryEvent?.Invoke();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicate_Click(object sender, EventArgs e)
        {
            try
            {
                string stepName;
                List<NamedClass> items = new List<NamedClass>();
                List<string> stepNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = null;
                    if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                        stepName = selectedNode.Parent.Parent.Name;
                    //
                    if (stepNames == null) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    stepNames.Add(stepName);
                }
                //
                if (items.Count > 0)
                {
                    RenderingOff?.Invoke();
                    DuplicateEvent?.Invoke(items.ToArray(), stepNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagate_Click(object sender, EventArgs e)
        {
            try
            {
                string stepName;
                List<NamedClass> items = new List<NamedClass>();
                List<string> stepNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = null;
                    if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                        stepName = selectedNode.Parent.Parent.Name;
                    //
                    if (stepNames == null) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    stepNames.Add(stepName);
                }
                //
                if (items.Count > 0)
                {
                    RenderingOff?.Invoke();
                    PropagateEvent?.Invoke(items.ToArray(), stepNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreview_Click(object sender, EventArgs e)
        {
            try
            {
                string stepName;
                List<NamedClass> items = new List<NamedClass>();
                List<string> stepNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = null;
                    if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                        stepName = selectedNode.Parent.Parent.Name;
                    //
                    if (stepNames == null) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    stepNames.Add(stepName);
                }
                //
                if (items.Count > 0)
                {
                    PreviewEvent?.Invoke(items.ToArray(), stepNames.ToArray());
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Visibility
        private void tsmiHideShow_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                string stepName;
                List<string> stepNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    if (CanHide(selectedNode.Tag))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = null;
                        if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                            stepName = selectedNode.Parent.Parent.Name;
                        stepNames.Add(stepName);
                    }
                }
                //
                if (items.Count > 0)
                {
                    RenderingOff?.Invoke();
                    HideShowOperation operation;
                    if (sender == tsmiHide) operation = HideShowOperation.Hide;
                    else if (sender == tsmiShow) operation = HideShowOperation.Show;
                    else if (sender == tsmiShowOnly) operation = HideShowOperation.ShowOnly;
                    else throw new NotSupportedException();

                    HideShowEvent?.Invoke(items.ToArray(), operation, stepNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiInvertHideShow_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                string stepName;
                List<string> stepNames = new List<string>();

                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;

                    if (CanHide(selectedNode.Tag))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = null;
                        if (selectedNode.Parent != null && selectedNode.Parent.Parent != null && selectedNode.Parent.Parent.Tag is Step)
                            stepName = selectedNode.Parent.Parent.Name;
                        stepNames.Add(stepName);
                    }
                }

                if (items.Count > 0)
                {
                    RenderingOff?.Invoke();
                    List<NamedClass> toShow = new List<NamedClass>();
                    List<NamedClass> toHide = new List<NamedClass>();

                    foreach (var item in items)
                    {
                        if (item.Visible) toHide.Add(item);
                        else toShow.Add(item);
                    }

                    if (toShow.Count > 0) HideShowEvent?.Invoke(toShow.ToArray(), HideShowOperation.Show, stepNames.ToArray());
                    if (toHide.Count > 0) HideShowEvent?.Invoke(toHide.ToArray(), HideShowOperation.Hide, stepNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparency_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> parts = new List<string>();

                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    if (selectedNode.Tag is BasePart part) parts.Add(part.Name);
                }

                if (parts.Count > 0) SetTransparencyEvent?.Invoke(parts.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResultColorContoutsVisibility_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();

                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;

                    if (selectedNode.Tag is ResultPart)
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                    }
                }

                if (items.Count > 0)
                {
                    bool colorContours = (sender == tsmiColorContoursOn);
                    ColorContoursVisibilityEvent?.Invoke(items.ToArray(), colorContours);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Geometry - compound, swap
        private void tsmiCompoundPart_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvGeometry.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 1) CreateCompoundPart?.Invoke(names.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapPartGeometries_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvGeometry.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count == 2) SwapPartGeometries?.Invoke(names.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Meshing
        async private void tsmiPreviewEdgeMesh_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvGeometry.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0) await PreviewEdgeMesh?.Invoke(names.ToArray(), null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCreateMesh_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvGeometry.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0) CreateMeshEvent?.Invoke(names.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Geometry - copy
        private void tsmiCopyGeometryPartToResults_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvGeometry.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0) CopyGeometryToResultsEvent?.Invoke(names.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Model
        private void tsmiEditCalculiXKeywords_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                if (selectedNode != _model) return;
                //
                EditCalculixKeywords?.Invoke();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeParts_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                CodersLabTreeView tree = GetActiveTree();
                //
                foreach (TreeNode node in tree.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0)
                {
                    if (tree == cltvModel) MergeParts?.Invoke(names.ToArray());
                    else if (tree == cltvResults) MergeResultParts?.Invoke(names.ToArray());
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiConvertToPart_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> names = new List<string>();
                foreach (TreeNode node in cltvModel.SelectedNodes)
                {
                    if (node.Tag != null) names.Add(((NamedClass)node.Tag).Name);
                }
                if (names.Count > 0) ConvertElementSetsToMeshParts?.Invoke(names.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Material
        private void tsmiMaterialLibrary_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                if (selectedNode.Tag != null) return;
                //
                MaterialLibrary?.Invoke();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Master/Slave
        private void tsmiSearchContactPairs_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                if (selectedNode.Tag != null) return;
                //
                SearchContactPairs?.Invoke();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapMasterSlave_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    items.Add((NamedClass)selectedNode.Tag);
                }
                //
                if (items.Count > 0) SwapMasterSlave?.Invoke(items.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeByMasterSlave_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    items.Add((NamedClass)selectedNode.Tag);
                }
                //
                if (items.Count > 0) MergeByMasterSlave?.Invoke(items.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Analysis
        private void tsmiRun_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                //
                if (selectedNode.Tag == null) return;
                //
                RunEvent?.Invoke(selectedNode.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCheckModel_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;
                //
                TreeNode selectedNode = tree.SelectedNodes[0];
                //
                if (selectedNode.Tag == null) return;
                //
                CheckModelEvent?.Invoke(selectedNode.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMonitor_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;

                TreeNode selectedNode = tree.SelectedNodes[0];

                if (selectedNode.Tag == null) return;

                MonitorEvent?.Invoke(selectedNode.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResults_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;

                TreeNode selectedNode = tree.SelectedNodes[0];

                if (selectedNode.Tag == null) return;

                ResultsEvent?.Invoke(selectedNode.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiKill_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (tree.SelectedNodes.Count != 1) return;

                TreeNode selectedNode = tree.SelectedNodes[0];

                if (selectedNode.Tag == null) return;

                KillEvent?.Invoke(selectedNode.Name);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Activate/Deactivate
        private void ActivateDeactivate_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                string stepName;
                List<string> stepNames = new List<string>();
                CodersLabTreeView tree = GetActiveTree();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    if (CanDeactivate(selectedNode))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = null;
                        if (selectedNode.Parent != null && selectedNode.Parent.Parent != null
                            && selectedNode.Parent.Parent.Tag is Step)
                            stepName = selectedNode.Parent.Parent.Name;
                        stepNames.Add(stepName);
                    }
                }
                //
                RenderingOff?.Invoke();
                bool activate = (sender == tsmiActivate);
                ActivateDeactivateEvent?.Invoke(items.ToArray(), activate, stepNames.ToArray());
                RenderingOn?.Invoke();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Expand/Collapse
        private void tsmiExpandAll_Click(object sender, EventArgs e)
        {
            foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
            {
                selectedNode.ExpandAll();
            }
        }
        private void tsmiCollapseAll_Click(object sender, EventArgs e)
        {
            foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
            {
                selectedNode.Collapse();
            }
        }
        // Delete
        private void tsmiDelete_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                string parentName;
                List<string> parentNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetActiveTree().SelectedNodes)
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    parentName = null;
                    if (selectedNode.Parent != null && selectedNode.Parent.Parent != null) 
                    {
                        if (selectedNode.Parent.Parent.Tag is Step) parentName = selectedNode.Parent.Parent.Name;
                        else if (selectedNode.Parent.Tag is Field) parentName = selectedNode.Parent.Text;
                        else if (selectedNode.Parent.Tag is ResultFieldOutput) parentName = selectedNode.Parent.Text;
                        else if (selectedNode.Parent.Tag is HistoryResultSet) parentName = selectedNode.Parent.Text;
                        else if (selectedNode.Parent.Tag is ResultHistoryOutput) parentName = selectedNode.Parent.Text;
                    }
                    parentNames.Add(parentName);
                }
                //
                if (items.Count > 0)
                {
                    RenderingOff?.Invoke();
                    DeleteEvent?.Invoke(items.ToArray(), parentNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        #endregion


        // Methods                                                                                                                  
        protected override void WndProc(ref Message m)
        {
            //
            // 0200        512     WM_MOUSEFIRST
            // 0200        512     WM_MOUSEMOVE
            // 0201        513     WM_LBUTTONDOWN
            // 0202        514     WM_LBUTTONUP
            // 0203        515     WM_LBUTTONDBLCLK
            // 0204        516     WM_RBUTTONDOWN
            // 0205        517     WM_RBUTTONUP
            // 0206        518     WM_RBUTTONDBLCLK
            // 0207        519     WM_MBUTTONDOWN
            // 0208        520     WM_MBUTTONUP
            // 0209        521     WM_MBUTTONDBLCLK
            // 0209        521     WM_MOUSELAST
            // 020a        522     WM_MOUSEWHEEL
            //

            // Eat left and right mouse clicks
            if (_disableMouse && (m.Msg >= 513 && m.Msg <= 522))
            {
                // eat message
            }
            else
            {
                // Eat button highlighting if the form is not in focus
                if (m.Msg == WM_MOUSEMOVE)
                {
                    if (!this.TopLevelControl.ContainsFocus)
                    {
                        // eat message
                    }
                    else
                    {
                        // handle messages normally
                        base.WndProc(ref m);
                    }
                }
                else
                {
                    // handle messages normally
                    base.WndProc(ref m);
                }
            }
        }
        // Clear                                                                                                                    
        public void Clear()
        {
            // Geometry
            cltvGeometry.SelectedNodes.Clear();
            cltvGeometry.Nodes.Clear();
            _geomParts.Nodes.Clear();
            _meshSetupItems.Nodes.Clear();
            // Model
            cltvModel.SelectedNodes.Clear();
            cltvModel.Nodes.Clear();
            _model.Nodes.Clear();
            _modelMesh.Nodes.Clear();
            _modelParts.Nodes.Clear();
            _modelNodeSets.Nodes.Clear();
            _modelElementSets.Nodes.Clear();
            _modelSurfaces.Nodes.Clear();
            _modelFeatures.Nodes.Clear();
            _modelReferencePoints.Nodes.Clear();
            _modelCoordinateSystems.Nodes.Clear();
            _materials.Nodes.Clear();
            _sections.Nodes.Clear();
            _constraints.Nodes.Clear();
            _contacts.Nodes.Clear();
            _surfaceInteractions.Nodes.Clear();
            _contactPairs.Nodes.Clear();
            _amplitudes.Nodes.Clear();
            _initialConditions.Nodes.Clear();
            _steps.Nodes.Clear();
            _analyses.Nodes.Clear();
            //
            SetNumberOfUserKeywords(0);
            // Geometry
            _geomParts.Text = _geomPartsName;
            _meshSetupItems.Text = _meshSetupItemsName;
            // Model
            _modelParts.Text = _modelPartsName;
            _modelNodeSets.Text = _modelNodeSetsName;
            _modelElementSets.Text = _modelElementSetsName;
            _modelSurfaces.Text = _modelSurfacesName;
            _modelFeatures.Text = _modelFeaturesName;
            _modelReferencePoints.Text = _modelReferencePointsName;
            _modelCoordinateSystems.Text = _modelCoordinateSystemsName;
            _materials.Text = _materialsName;
            _sections.Text = _sectionsName;
            _constraints.Text = _constraintsName;
            _surfaceInteractions.Text = _surfaceInteractionsName;
            _contactPairs.Text = _contactPairsName;
            _amplitudes.Text = _amplitudesName;
            _initialConditions.Text = _initialConditionsName;
            _steps.Text = _stepsName;
            _analyses.Text = _analysesName;
            //
            // Fill trees
            //
            // Geometry
            cltvGeometry.Nodes.Add(_geomParts);
            cltvGeometry.Nodes.Add(_meshSetupItems);
            // Model
            cltvModel.Nodes.Add(_model);
            _model.Nodes.Add(_modelMesh);
            _modelMesh.Nodes.Add(_modelParts);
            _modelMesh.Nodes.Add(_modelNodeSets);
            _modelMesh.Nodes.Add(_modelElementSets);
            _modelMesh.Nodes.Add(_modelSurfaces);
            _model.Nodes.Add(_modelFeatures);
            _modelFeatures.Nodes.Add(_modelReferencePoints);
            _modelFeatures.Nodes.Add(_modelCoordinateSystems);
            _model.Nodes.Add(_materials);
            _model.Nodes.Add(_sections);
            _model.Nodes.Add(_constraints);
            _model.Nodes.Add(_contacts);
            _contacts.Nodes.Add(_surfaceInteractions);
            _contacts.Nodes.Add(_contactPairs);
            _model.Nodes.Add(_amplitudes);
            _model.Nodes.Add(_initialConditions);
            _model.Nodes.Add(_steps);
            cltvModel.Nodes.Add(_analyses);
            // Expand/Collapse
            _geomParts.ExpandAll();
            _model.ExpandAll();
            _contacts.Collapse();
            //
            ClearResults(); //calls cltvResults.SelectedNodes.Clear();
        }
        public void ClearActiveTreeSelection()
        {
            GetActiveTree().SelectedNodes.Clear();
        }
        public void ClearTreeSelection(ViewType view)
        {
            CodersLabTreeView tree = null;
            if (view == ViewType.Geometry) tree = cltvGeometry;
            else if (view == ViewType.Model) tree = cltvModel;
            else if (view == ViewType.Results) tree = cltvResults;
            else throw new NotSupportedException();
            tree.SelectedNodes.Clear();
        }
        public void ClearResults()
        {
            cltvResults.SelectedNodes.Clear();
            cltvResults.Nodes.Clear();
            _resultModel.Nodes.Clear();
            _resultMesh.Nodes.Clear();
            _resultParts.Nodes.Clear();
            _resultNodeSets.Nodes.Clear();
            _resultElementSets.Nodes.Clear();
            _resultSurfaces.Nodes.Clear();
            _resultFeatures.Nodes.Clear();
            _resultReferencePoints.Nodes.Clear();
            _resultCoordinateSystems.Nodes.Clear();
            _results.Nodes.Clear();
            _resultFieldOutputs.Nodes.Clear();
            _resultHistoryOutputs.Nodes.Clear();
            //
            _resultParts.Text = _resultPartsName;
            _resultNodeSets.Text = _resultNodeSetsName;
            _resultElementSets.Text = _resultElementSetsName;
            _resultSurfaces.Text = _resultSurfacesName;
            _resultFeatures.Text = _resultFeaturesName;
            _resultReferencePoints.Text = _resultReferencePointsName;
            _resultCoordinateSystems.Text = _resultCoordinateSystemsName;
            _resultFieldOutputs.Text = _resultFieldOutputsName;
            _resultHistoryOutputs.Text = _resultHistoryOutputsName;
            // Fill the tree
            cltvResults.Nodes.Add(_resultModel);
            _resultModel.Nodes.Add(_resultMesh);
            _resultMesh.Nodes.Add(_resultParts);
            _resultMesh.Nodes.Add(_resultNodeSets);
            _resultMesh.Nodes.Add(_resultElementSets);
            _resultMesh.Nodes.Add(_resultSurfaces);
            _resultModel.Nodes.Add(_resultFeatures);
            _resultFeatures.Nodes.Add(_resultReferencePoints);
            _resultFeatures.Nodes.Add(_resultCoordinateSystems);
            cltvResults.Nodes.Add(_results);
            _results.Nodes.Add(_resultFieldOutputs);
            _results.Nodes.Add(_resultHistoryOutputs);
            // Expand/Collapse
            _resultModel.ExpandAll();
            _resultMesh.ExpandAll();
            _results.ExpandAll();
        }
        //
        public void UpdateHighlight()
        {
            cltv_SelectionsChanged(null, null);
        }
        public int SelectBasePart(MouseEventArgs e, Keys modifierKeys, BasePart part, bool highlight)
        {
            try
            {
                _disableSelectionsChanged = true;
                //
                CodersLabTreeView tree = GetActiveTree();
                TreeNode baseNode = tree.Nodes[0];
                TreeNode[] tmp = baseNode.Nodes.Find(part.Name, true, true);
                //
                if (tmp.Length > 1)
                {
                    foreach (var treeNode in tmp)
                    {
                        if (treeNode.Tag.GetType() == part.GetType())
                        {
                            baseNode = treeNode;
                            break;
                        }
                    }
                }
                else
                {
                    if (tmp.Length > 0) baseNode = tmp[0];
                    else return -1;
                }
                //
                if (baseNode.Parent != null) baseNode.Parent.Expand();   // expand if called as function
                //
                if (e.Button == MouseButtons.Left)
                {
                    if (modifierKeys == Keys.Shift && modifierKeys == Keys.Control) { }
                    else if (modifierKeys == Keys.Shift) tree.SelectedNodes.Add(baseNode);
                    else if (modifierKeys == Keys.Control)
                    {
                        if (tree.SelectedNodes.Contains(baseNode))
                            tree.SelectedNodes.Remove(baseNode);
                        else
                            tree.SelectedNodes.Add(baseNode);
                    }
                    else
                    {
                        // This is without modifier keys - a new selection
                        tree.SelectedNodes.Clear();
                        tree.SelectedNodes.Add(baseNode);
                    }
                }
                else if (e.Button == MouseButtons.Right)
                {
                    if (!tree.SelectedNodes.Contains(baseNode))
                    {
                        tree.SelectedNodes.Clear();
                        tree.SelectedNodes.Add(baseNode);
                    }
                }
                //
                _disableSelectionsChanged = false;
                if (highlight) UpdateHighlight();
                //
                return tree.SelectedNodes.Count;
            }
            catch { return -1; }
            finally { _disableSelectionsChanged = false; }
        }
        public void EditSelectedPart()
        {
            tsmiEdit_Click(null, null);
        }

        // Regenerate tree                                                                                                          
        public void RegenerateTree(FeModel model, IDictionary<string, AnalysisJob> jobs, FeResults results,
                                   bool remeshing = false)
        {
            if (!_screenUpdating) return;
            //
            try
            {
                // Expand/Collapse
                string[][] prevNodeNames;
                bool[][] prevModelTreeStates = GetAllTreesExpandCollapseState(out prevNodeNames, remeshing);
                //
                cltvGeometry.BeginUpdate();
                cltvModel.BeginUpdate();
                cltvResults.BeginUpdate();
                Dictionary<CodersLabTreeView, string[]> selectedNodePaths = GetSelectedNodePaths();
                Clear();
                if (model != null)
                {
                    // User keywords
                    if (model.CalculixUserKeywords != null) SetNumberOfUserKeywords(model.CalculixUserKeywords.Count);
                    // Geom Parts
                    if (model.Geometry != null)
                    {
                        AddGeometryParts(model.Geometry.Parts);
                        _geomParts.Expand();
                        // Mesh setup items
                        AddObjectsToNode(_meshSetupItemsName, _meshSetupItems, model.Geometry.MeshSetupItems);
                        _meshSetupItems.Expand();
                    }
                    //
                    if (model.Mesh != null)
                    {
                        // Mesh Parts
                        AddObjectsToNode(_modelPartsName, _modelParts, model.Mesh.Parts);
                        // Node sets
                        AddObjectsToNode(_modelNodeSetsName, _modelNodeSets, model.Mesh.NodeSets);
                        // Element sets
                        AddObjectsToNode(_modelElementSetsName, _modelElementSets, model.Mesh.ElementSets);
                        // Surfaces
                        AddObjectsToNode(_modelSurfacesName, _modelSurfaces, model.Mesh.Surfaces);
                        // Reference points
                        AddObjectsToNode(_modelReferencePointsName, _modelReferencePoints, model.Mesh.ReferencePoints);
                        // Coordinate systems
                        AddObjectsToNode(_modelCoordinateSystemsName, _modelCoordinateSystems, model.Mesh.CoordinateSystems);
                    }
                    // Materials
                    AddObjectsToNode(_materialsName, _materials, model.Materials);
                    // Sections
                    AddObjectsToNode(_sectionsName, _sections, model.Sections);
                    // Constraints
                    AddObjectsToNode(_constraintsName, _constraints, model.Constraints);
                    // Surface interactions
                    AddObjectsToNode(_surfaceInteractionsName, _surfaceInteractions, model.SurfaceInteractions);
                    // Contact pairs
                    AddObjectsToNode(_contactPairsName, _contactPairs, model.ContactPairs);
                    // Amplitudes
                    AddObjectsToNode(_amplitudesName, _amplitudes, model.Amplitudes);
                    // Initial conditions
                    AddObjectsToNode(_initialConditionsName, _initialConditions, model.InitialConditions);
                    // Steps
                    AddSteps(model.StepCollection.StepsList);
                    // Analyses
                    AddObjectsToNode(_analysesName, _analyses, jobs);
                    _analyses.ExpandAll();
                    // Results
                    if (results != null)
                    {
                        if (results.Mesh != null)
                        {
                            // Results parts
                            AddObjectsToNode(_resultPartsName, _resultParts, results.Mesh.Parts);
                            // Node sets
                            AddObjectsToNode(_resultNodeSetsName, _resultNodeSets, results.Mesh.NodeSets);
                            // Element sets
                            AddObjectsToNode(_resultElementSetsName, _resultElementSets, results.Mesh.ElementSets);
                            // Surfaces
                            AddObjectsToNode(_resultSurfacesName, _resultSurfaces, results.Mesh.Surfaces);
                            // Reference points
                            AddObjectsToNode(_resultReferencePointsName, _resultReferencePoints, results.Mesh.ReferencePoints);
                            // Coordinate systems
                            AddObjectsToNode(_resultCoordinateSystemsName, _resultCoordinateSystems, results.Mesh.CoordinateSystems);
                            // Field outputs
                            string[] fieldNames;
                            string[][] allComponents;
                            //
                            fieldNames = results.GetVisibleFieldNames();
                            //
                            allComponents = new string[fieldNames.Length][];
                            for (int i = 0; i < fieldNames.Length; i++)
                            {
                                allComponents[i] = results.GetFieldComponentNames(fieldNames[i]);
                            }
                            CreateOverwriteFieldOutputNodes(fieldNames, allComponents, results.GetResultFieldOutputs());
                        }
                        //
                        if (results.GetHistory() != null)
                            CreateOverwriteHistoryOutputNodes(results.GetHistory().Sets.Values.ToArray(),
                                                              results.GetResultHistoryOutputs());
                    }
                }
                //
                SelectNodesByPath(selectedNodePaths);
                // Expand/Collapse
                bool[][] afterModelTreeStates = GetAllTreesExpandCollapseState(out string[][] afterNodeNames, remeshing);
                // Geometry
                if (prevModelTreeStates[0].Length == afterModelTreeStates[0].Length)
                    SetTreeExpandCollapseState(cltvGeometry, prevModelTreeStates[0]);
                // Model
                if (prevModelTreeStates[1].Length == afterModelTreeStates[1].Length)
                    SetTreeExpandCollapseState(cltvModel, prevModelTreeStates[1], remeshing);
            }
            catch { }
            finally
            {
                cltvGeometry.EndUpdate();
                cltvModel.EndUpdate();
                cltvResults.EndUpdate();
            }
        }
        private Dictionary<CodersLabTreeView, string[]> GetSelectedNodePaths()
        {
            string splitter = "\\";
            string path;
            TreeNode node;
            Dictionary<CodersLabTreeView, string[]> selectedNodePaths = new Dictionary<CodersLabTreeView, string[]>();
            List<string> nodePaths = new List<string>();
            CodersLabTreeView[] trees = new CodersLabTreeView[] { cltvGeometry, cltvModel, cltvResults };
            //
            for (int i = 0; i < trees.Length; i++)
            {
                if (trees[i].SelectedNodes.Count > 0)
                {
                    nodePaths.Clear();
                    foreach (TreeNode selectedNode in trees[i].SelectedNodes)
                    {
                        path = "";
                        node = selectedNode;
                        while (node != null)
                        {
                            path = node.Name + splitter + path;
                            node = node.Parent;
                        }
                        nodePaths.Add(path);
                    }
                    selectedNodePaths.Add(trees[i], nodePaths.ToArray());
                }
            }
            return selectedNodePaths;
        }
        private void SelectNodesByPath(Dictionary<CodersLabTreeView, string[]> selectedNodeNames)
        {
            string[] splitter = new string[] { "\\" };
            string[] tmp;
            TreeNode node;
            foreach (var entry in selectedNodeNames)
            {
                node = null;
                for (int i = 0; i < entry.Value.Length; i++)
                {
                    tmp = entry.Value[i].Split(splitter, StringSplitOptions.RemoveEmptyEntries);
                    node = entry.Key.Nodes[tmp[0]];
                    for (int j = 1; j < tmp.Length; j++)
                    {
                        if (node != null) node = node.Nodes[tmp[j]];
                    }
                    if (node != null) entry.Key.SelectedNodes.Add(node);
                }
            }
        }
        public void AddTreeNode(ViewType view, NamedClass item, string parentName)
        {
            if (!_screenUpdating) return;
            if (item.Internal) return;
            //
            CodersLabTreeView tree = GetTree(view);
            try
            {
                tree.SuspendLayout();
                tree.BeginUpdate();
                //
                TreeNode node;
                TreeNode parent;
                TreeNode[] tmp;
                //
                if (item is MeshSetupItem)
                {
                    parent = _meshSetupItems;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is MeshPart)
                {
                    parent = _modelParts;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is FeNodeSet)
                {
                    parent = _modelNodeSets;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is FeElementSet)
                {
                    parent = _modelElementSets;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is FeSurface)
                {
                    parent = _modelSurfaces;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is FeReferencePoint)
                {
                    if (view == ViewType.Model) parent = _modelReferencePoints;
                    else if (view == ViewType.Results) parent = _resultReferencePoints;
                    else throw new NotSupportedException();
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is CoordinateSystem)
                {
                    if (view == ViewType.Model) parent = _modelCoordinateSystems;
                    else if (view == ViewType.Results) parent = _resultCoordinateSystems;
                    else throw new NotSupportedException();
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Material)
                {
                    parent = _materials;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Section)
                {
                    parent = _sections;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Constraint)
                {
                    parent = _constraints;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is SurfaceInteraction)
                {
                    parent = _surfaceInteractions;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is ContactPair)
                {
                    parent = _contactPairs;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is InitialCondition)
                {
                    parent = _initialConditions;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Amplitude)
                {
                    parent = _amplitudes;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Step)
                {
                    parent = _steps;
                    node = AddStep((Step)item);
                }
                else if (item is HistoryOutput && parentName != null)
                {
                    tmp = _steps.Nodes.Find(parentName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. More than one step named: " + parentName);
                    //
                    tmp = tmp[0].Nodes.Find(_modelHistoryOutputsName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. There is no history output node to add to.");
                    //
                    parent = tmp[0];
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is FieldOutput && parentName != null)
                {
                    tmp = _steps.Nodes.Find(parentName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. More than one step named: " + parentName);
                    //
                    tmp = tmp[0].Nodes.Find(_modelFieldOutputsName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. There is no field output node to add to.");
                    //
                    parent = tmp[0];
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is BoundaryCondition && parentName != null)
                {
                    tmp = _steps.Nodes.Find(parentName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. More than one step named: " + parentName);
                    //
                    tmp = tmp[0].Nodes.Find(_boundaryConditionsName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. There is no bounday condition node to add to.");
                    //
                    parent = tmp[0];
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is Load && parentName != null)
                {
                    tmp = _steps.Nodes.Find(parentName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. More than one step named: " + parentName);
                    //
                    tmp = tmp[0].Nodes.Find(_loadsName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. There is no load node to add to.");
                    //
                    parent = tmp[0];
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is DefinedField && parentName != null)
                {
                    tmp = _steps.Nodes.Find(parentName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. More than one step named: " + parentName);
                    //
                    tmp = tmp[0].Nodes.Find(_definedFieldsName, true, true);
                    if (tmp.Length > 1) throw new Exception("Adding operation failed. There is no defined field node to add to.");
                    //
                    parent = tmp[0];
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is AnalysisJob)
                {
                    parent = _analyses;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is ResultPart)
                {
                    parent = _resultParts;
                    node = parent.Nodes.Add(item.Name);
                    node.Name = node.Text;
                    node.Tag = item;
                }
                else if (item is ResultFieldOutput rfo)
                {
                    CreateOverwriteFieldOutputNodes(new string[] { rfo.Name }, new string[][] { rfo.GetComponentNames() },
                                                    new ResultFieldOutput[] { rfo });
                    //
                    parent = _resultFieldOutputs;
                    node = parent.Nodes[rfo.Name];
                }
                else if (item is ResultHistoryOutput hro)
                {
                    CreateOverwriteHistoryOutputNodes(new HistoryResultSet[] { hro.HistoryResultSet },
                                                      new ResultHistoryOutput[] { hro });
                    parent = _resultHistoryOutputs;
                    node = parent.Nodes[hro.Name];
                }
                else throw new NotImplementedException();
                //
                parent.Text = parent.Name;
                if (parent.Nodes.Count > 0) parent.Text += " (" + parent.Nodes.Count + ")";
                //
                SetNodeStatus(node);
                //
                GetTree(view).SelectedNode = node;
                // Expand for propagate
                while (node != null && node.Parent != null)
                {
                    node = node.Parent;
                    if (!node.IsExpanded) node.Expand();
                }
            }
            catch { }
            finally
            {
                tree.EndUpdate();
                tree.ResumeLayout();
            }
        }
        public void UpdateTreeNode(ViewType view, string oldItemName, NamedClass item, string parentName, bool updateSelection)
        {
            if (!_screenUpdating) return;   // must be here; when _screenUpdating = false the function add tree node is not working
            //
            CodersLabTreeView tree = GetTree(view);
            try
            {
                tree.SuspendLayout();
                tree.BeginUpdate();
                //
                TreeNode baseNode = FindTreeNode(view, oldItemName, item, parentName);
                if (baseNode == null) return;
                //
                if (item is ResultFieldOutput rfo)
                {
                    int[] indices = new int[] { baseNode.Index };
                    CreateOverwriteFieldOutputNodes(new string[] { rfo.Name }, new string[][] { rfo.GetComponentNames() },
                                                    new ResultFieldOutput[] { rfo }, indices);
                }
                else if (item is ResultHistoryOutput rho)
                {
                    int[] indices = new int[] { baseNode.Index };
                    CreateOverwriteHistoryOutputNodes(new HistoryResultSet[] { rho.HistoryResultSet },
                                                      new ResultHistoryOutput[] { rho }, indices);
                }
                else
                {
                    baseNode.Text = item.Name;
                    baseNode.Name = item.Name;
                    baseNode.Tag = item;
                }
                SetNodeStatus(baseNode);
                // Update selection
                if (updateSelection)
                {
                    if (tree != null && tree.SelectedNodes.Contains(baseNode)) UpdateHighlight();    // update only once
                    else tree.SelectedNode = baseNode; // for job the tree is null
                                                       // Expand for propagate
                    while (baseNode.Parent != null)
                    {
                        baseNode = baseNode.Parent;
                        if (!baseNode.IsExpanded) baseNode.Expand();
                    }
                }
            }
            catch { }
            finally
            {
                tree.EndUpdate();
                tree.ResumeLayout();
            }
        }
        public void SwapTreeNodes(ViewType view, string firstItemName, NamedClass firstItem,
                                  string secondItemName, NamedClass secondItem, string parentName)
        {
            CodersLabTreeView tree = GetTree(view);
            try
            {
                tree.SuspendLayout();
                tree.BeginUpdate();
                //
                TreeNode firstNode = FindTreeNode(view, firstItemName, firstItem, parentName);
                if (firstNode == null) return;
                //
                TreeNode secondNode = FindTreeNode(view, secondItemName, secondItem, parentName);
                if (secondNode == null) return;
                //
                if (firstNode.Parent != secondNode.Parent) return;
                //
                TreeNode parent = firstNode.Parent;
                //
                int firstIndex = firstNode.Index;
                int secondIndex = secondNode.Index;
                //
                firstNode.Remove();
                secondNode.Remove();
                //
                if (firstIndex < secondIndex)
                {
                    parent.Nodes.Insert(firstIndex, secondNode);
                    parent.Nodes.Insert(secondIndex, firstNode);
                }
                else
                {
                    parent.Nodes.Insert(secondIndex, firstNode);
                    parent.Nodes.Insert(firstIndex, secondNode);
                }
            }
            catch { }
            finally
            {
                tree.EndUpdate();
                tree.ResumeLayout();
            }
        }
        private TreeNode FindTreeNode(ViewType view, string itemName, NamedClass item, string parentName)
        {
            CodersLabTreeView tree = GetTree(view);
            //
            TreeNode baseNode;
            if (item is MeshSetupItem) baseNode = _meshSetupItems;
            else if (item is AnalysisJob) baseNode = _analyses;
            else if (item is ResultFieldOutput) baseNode = _resultFieldOutputs;
            else if (item is ResultHistoryOutput) baseNode = _resultHistoryOutputs;
            else baseNode = tree.Nodes[0];
            //
            TreeNode[] tmpNodes;
            if (parentName != null)
            {
                tmpNodes = _steps.Nodes.Find(parentName, true);
                int count = 0;
                foreach (TreeNode tmpNode in tmpNodes)
                {
                    if (tmpNode.Nodes.Count > 0)
                    {
                        baseNode = tmpNode;
                        count++;
                    }
                }
                if (count > 1) throw new Exception("Node search failed. More than one step named: " + parentName);
                baseNode = tmpNodes[0];
            }
            //
            bool nodeFound;
            tmpNodes = baseNode.Nodes.Find(itemName, true);
            if (tmpNodes.Length > 1)
            {
                nodeFound = false;
                foreach (var treeNode in tmpNodes)
                {
                    if (treeNode.Tag != null && treeNode.Tag.GetType() == item.GetType())
                    {
                        baseNode = treeNode;
                        nodeFound = true;
                        break;
                    }
                }
                if (!nodeFound) throw new Exception("Tree search failed. The item name to edit was not found: " + item.Name);
            }
            else
            {
                if (tmpNodes.Length == 1) baseNode = tmpNodes[0];
                else baseNode = null;
            }
            //
            return baseNode;
        }
        public void RemoveTreeNode<T>(ViewType view, string nodeName, string parentName) where T : NamedClass
        {
            if (!_screenUpdating) return;
            //
            CodersLabTreeView tree = GetTree(view);
            //
            try
            {
                tree.SuspendLayout();
                tree.BeginUpdate();
                //
                // No parent
                TreeNode baseNode = null;
                if (typeof(T) == typeof(MeshSetupItem)) baseNode = _meshSetupItems;
                else if (typeof(T) == typeof(AnalysisJob)) baseNode = _analyses;
                //
                else if (typeof(T) == typeof(Field)) baseNode = _resultFieldOutputs;
                else if (typeof(T) == typeof(ResultFieldOutput)) baseNode = _resultFieldOutputs;
                else if (typeof(T) == typeof(HistoryResultSet)) baseNode = _resultHistoryOutputs;
                else if (typeof(T) == typeof(ResultHistoryOutput)) baseNode = _resultHistoryOutputs;
                else baseNode = tree.Nodes[0];
                // Find parent
                TreeNode[] tmp;
                int count;
                if (parentName != null)
                {
                    if (view == ViewType.Model) tmp = _steps.Nodes.Find(parentName, true, true);
                    else if (view == ViewType.Results)
                    {
                        if (typeof(T) == typeof(FieldData)) tmp = _resultFieldOutputs.Nodes.Find(parentName, true, true);
                        else if (typeof(T) == typeof(HistoryResultField))
                            tmp = _resultHistoryOutputs.Nodes.Find(parentName, true, true);
                        else if (typeof(T) == typeof(HistoryResultData))
                        {
                            string[] split = parentName.Split(new string[] { "@@@" }, StringSplitOptions.None);
                            if (split.Length == 2)
                            {
                                tmp = _resultHistoryOutputs.Nodes.Find(split[0], true, true);
                                if (tmp.Length == 1) tmp = tmp[0].Nodes.Find(split[1], true, true);
                                else throw new NotSupportedException();
                            }
                            else throw new NotSupportedException();
                        }
                        else throw new NotSupportedException();
                    }
                    else throw new NotSupportedException();
                    //
                    if (tmp.Length > 1)
                    {
                        count = 0;
                        for (int i = 0; i < tmp.Length; i++)
                        {
                            if (tmp[i].Nodes.Find(nodeName, true).Length > 0)
                            {
                                tmp[0] = tmp[i];
                                count++;
                            }
                        }
                        if (count > 1) throw new Exception("Tree update failed. More than one parent named: " + parentName);
                    }
                    baseNode = tmp[0];
                }
                //
                tmp = baseNode.Nodes.Find(nodeName, true, true);
                count = 0;
                //
                for (int i = 0; i < tmp.Length; i++)
                {
                    if (tmp[i].Tag is T)
                    {
                        tmp[0] = tmp[i];
                        count++;
                    }
                }
                //
                if (count > 1) throw new Exception("Tree update failed. More than one tree node named: " + nodeName);
                if (count < 1) throw new Exception("Tree update failed. There is no tree node named: " + nodeName);
                //
                TreeNode parent = tmp[0].Parent;
                //
                tmp[0].Remove();
                //
                _disableSelectionsChanged = true;
                tree.SelectedNodes.Remove(tmp[0]);
                _disableSelectionsChanged = false;
                //
                parent.Text = parent.Name;
                if (parent.Tag is Field || parent.Tag is ResultFieldOutput ||
                    parent.Tag is HistoryResultSet || parent.Tag is HistoryResultField || parent.Tag is ResultHistoryOutput)
                {
                    SetNodeStatus(parent);  // remove dotted T icon
                }
                else if (parent.Nodes.Count > 0) parent.Text += " (" + parent.Nodes.Count + ")";
            }
            catch { }
            finally
            {
                tree.EndUpdate();
                tree.ResumeLayout();
            }
        }
        private void AddObjectsToNode<TKey, TVal>(string initialNodeName, TreeNode node, IDictionary<TKey, TVal> dictionary,
                                                  bool countNodes = true)
        {
            TreeNode nodeToAdd;
            //
            var list = dictionary.Keys.ToList();
            //
            foreach (var key in list)
            {
                if (dictionary[key] is NamedClass nc && nc.Internal) continue;
                //
                nodeToAdd = node.Nodes.Add(key.ToString());
                nodeToAdd.Name = nodeToAdd.Text;
                nodeToAdd.Tag = dictionary[key];
                SetNodeStatus(nodeToAdd);
            }
            //
            if (countNodes && node.Nodes.Count > 0) node.Text = initialNodeName + " (" + node.Nodes.Count.ToString() + ")";
            else node.Text = initialNodeName;
        }
        private void AddGeometryParts(IDictionary<string, BasePart> parts)
        {
            HashSet<string> dependentPartNames = new HashSet<string>();
            foreach (var entry in parts)
            {
                if (entry.Value is CompoundGeometryPart cgp)
                {
                    dependentPartNames.UnionWith(cgp.SubPartNames);
                }
            }
            //
            BasePart part;
            BasePart subPart;
            TreeNode nodeToAdd;
            TreeNode subNodeToAdd;
            foreach (var entry in parts)
            {
                part = entry.Value;
                if (!dependentPartNames.Contains(part.Name))
                {
                    // Independent parts
                    nodeToAdd = _geomParts.Nodes.Add(part.Name);
                    nodeToAdd.Name = nodeToAdd.Text;
                    nodeToAdd.Tag = part;
                    SetNodeStatus(nodeToAdd);
                    //Compound parts
                    if (part is CompoundGeometryPart cgp)
                    {
                        for (int i = 0; i < cgp.SubPartNames.Length; i++)
                        {
                            subPart = parts[cgp.SubPartNames[i]];
                            subNodeToAdd = nodeToAdd.Nodes.Add(subPart.Name);
                            subNodeToAdd.Name = subNodeToAdd.Text;
                            subNodeToAdd.Tag = subPart;
                            SetNodeStatus(subNodeToAdd);
                        }
                        //
                        nodeToAdd.Expand();
                    }
                }
            }
            //
            if (_geomParts.Nodes.Count > 0) _geomParts.Text = _geomPartsName + " (" + _geomParts.Nodes.Count.ToString() + ")";
            else _geomParts.Text = _geomPartsName;
        }
        private void AddSteps(List<Step> steps)
        {
            foreach (var step in steps) AddStep(step);
            //
            if (_steps.Nodes.Count > 0) _steps.Text = _stepsName + " (" + _steps.Nodes.Count.ToString() + ")";
            else _steps.Text = _stepsName;
            //
            _steps.Expand();
        }
        private TreeNode AddStep(Step step)
        {
            TreeNode stepNode = _steps.Nodes.Add(step.Name);
            stepNode.Name = stepNode.Text;
            stepNode.Tag = step;
            SetNodeStatus(stepNode);
            //
            TreeNode tmp;
            // History outputs
            tmp = stepNode.Nodes.Add(_modelHistoryOutputsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, HistoryOutput>(_modelHistoryOutputsName, tmp, step.HistoryOutputs);
            SetNodeImage(tmp, "History_output.ico");
            // Field outputs
            tmp = stepNode.Nodes.Add(_modelFieldOutputsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, FieldOutput>(_modelFieldOutputsName, tmp, step.FieldOutputs);
            SetNodeImage(tmp, "Field_output.ico");
            // Boundary conditions
            tmp = stepNode.Nodes.Add(_boundaryConditionsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, BoundaryCondition>(_boundaryConditionsName, tmp, step.BoundaryConditions);
            SetNodeImage(tmp, "Bc.ico");
            // Loads
            tmp = stepNode.Nodes.Add(_loadsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, Load>(_loadsName, tmp, step.Loads);
            SetNodeImage(tmp, "Load.ico");
            // Defined fields
            tmp = stepNode.Nodes.Add(_definedFieldsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, DefinedField>(_definedFieldsName, tmp, step.DefinedFields);
            SetNodeImage(tmp, "Defined_field.ico");
            //
            stepNode.Expand();
            return stepNode;
        }
        private void FilterTree(CodersLabTreeView tree, string text)
        {
            try
            {
                if (RegenerateTreeCallBack != null)
                {
                    if (_prevStates[tree] == null) _prevStates[tree] = GetTreeExpandCollapseState(tree, out string[] names);
                    //
                    //tree.Visible = false;
                    //
                    RegenerateTreeCallBack();
                    if (text.Length > 0)
                    {
                        string[] texts = text.ToUpper().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        FilterNodes(tree.Nodes, texts);
                        tree.ExpandAll();
                    }
                    else
                    {
                        SetTreeExpandCollapseState(tree, _prevStates[tree]);
                        _prevStates[tree] = null;
                    }
                }
            }
            catch { }
            finally
            {
                //tree.Visible = true;
            }
        }
        private void FilterNodes(TreeNodeCollection nodes, string[] texts)
        {
            bool contains;
            List<TreeNode> nodesToDelete = new List<TreeNode>();
            //
            foreach (TreeNode node in nodes)
            {
                FilterNodes(node.Nodes, texts);
                //
                if (node.Nodes.Count == 0)
                {
                    contains = false;
                    foreach (var text in texts)
                    {
                        if (node.Text.Trim().ToUpper().Contains(text))
                        {
                            contains = true;
                            break;
                        }
                    }
                    if (!contains) nodesToDelete.Add(node);
                }
            }
            //
            foreach (var node in nodesToDelete) nodes.Remove(node);
        }

        // Node status                                                                                                              
        private void SetAllNodesStatusIcons(TreeNode node)
        {
            if (node.ImageKey == "Dots.ico" || node.ImageKey == "Dots_t.ico")
            {
                if (node.Nodes.Count == 0 || !node.IsExpanded) SetNodeImage(node, "Dots.ico");
                else SetNodeImage(node, "Dots_t.ico");
            }

            foreach (TreeNode child in node.Nodes)
            {
                SetAllNodesStatusIcons(child);
            }
        }
        private void SetNodeStatus(TreeNode node)
        {
            if (node != null && node.Tag != null)
            {
                CodersLabTreeView cltv = (CodersLabTreeView)node.TreeView;
                if (cltv == null) return;
                //
                NamedClass item = (NamedClass)node.Tag;
                bool warning;
                //
                if (item.Active)
                {
                    if (node.Tag is AnalysisJob)
                    {
                        AnalysisJob job = node.Tag as AnalysisJob;
                        if (job.JobStatus == JobStatus.OK) SetNodeImage(node, "OK.ico");
                        else if (job.JobStatus == JobStatus.Running) SetNodeImage(node, "Running.ico");
                        else if (job.JobStatus == JobStatus.FailedWithResults) SetNodeImage(node, "Warning.ico");
                        else SetNodeImage(node, "NoResult.ico");
                    }
                    else
                    {
                        // Hide
                        if (CanHide(item) && !item.Visible)
                        {
                            cltv.SetNodeForeColor(node, Color.Gray);
                            SetNodeImage(node, "Hide.ico");
                        }
                        // Show
                        else
                        {
                            cltv.SetNodeForeColor(node, Color.Black);
                            //
                            if (node.Tag is BasePart bp)
                            {
                                PartType partType = bp.PartType;
                                //
                                if (partType == PartType.Solid || partType == PartType.SolidAsShell)
                                {
                                    if (bp.Color.A == 255) SetNodeImage(node, "Solid.ico");
                                    else SetNodeImage(node, "Solid_transparent.ico");
                                }
                                else if (partType == PartType.Shell)
                                {
                                    if (bp.Color.A == 255) SetNodeImage(node, "Shell.ico");
                                    else SetNodeImage(node, "Shell_transparent.ico");
                                }
                                else if (partType == PartType.Wire)
                                {
                                    if (bp.Color.A == 255) SetNodeImage(node, "Wire.ico");
                                    else SetNodeImage(node, "Wire_transparent.ico");
                                }
                                else if (partType == PartType.Compound)
                                {
                                    if (bp.Color.A == 255) SetNodeImage(node, "Compound.ico");
                                    else SetNodeImage(node, "Compound_transparent.ico");
                                }
                            }
                            else if (node.Tag is Step)
                            {
                                SetNodeImage(node, "Step.ico");
                            }
                            else
                            {
                                if (node.Nodes.Count == 0 || !node.IsExpanded) SetNodeImage(node, "Dots.ico");
                                else SetNodeImage(node, "Dots_t.ico");
                            }
                            //
                            if (item is GeometryPart gp)
                            {
                                if (gp.PartType == PartType.Solid || gp.PartType == PartType.SolidAsShell)
                                    warning = gp.HasFreeEdges;
                                else if (gp.PartType == PartType.Shell)
                                    warning = gp.HasErrors;
                                else
                                    warning = false;
                                //
                                if (warning) SetNodeImage(node, "Warning.ico");
                            }
                        }
                    }
                }
                else
                {
                    cltv.SetNodeForeColor(node, Color.Gray);
                    SetNodeImage(node, "Unactive.ico");
                }
                //
                if (!item.Valid)
                {
                    cltv.SetNodeForeColor(node, cltv.HighlightForeErrorColor);
                    node.Parent.Expand();
                }
                // Set status of component nodes
                if (item is ResultFieldOutput)
                {
                    foreach (TreeNode componentNode in node.Nodes)
                    {
                        if (componentNode.Tag != null && componentNode.Tag is NamedClass nc)
                        {
                            nc.Valid = item.Valid;
                            SetNodeStatus(componentNode);
                        }
                    }
                }
                // Set status of the step controls
                if (item != null && item is Step) UpdateNumberOfStepControls(node);
            }
        }
        private void SetNodeImage(TreeNode node, string imageName)
        {
            if (node.ImageKey != imageName)
            {
                node.ImageKey = imageName;
                node.SelectedImageKey = imageName;
            }
        }

        // Results                                                                                                                  
        private void CreateOverwriteFieldOutputNodes(string[] fieldNames, string[][] components,
                                                     ResultFieldOutput[] resultFieldOutputs, int[] indices = null)
        {
            TreeNode node;
            TreeNode child;
            for (int i = 0; i < fieldNames.Length; i++)
            {
                if (indices == null) node = _resultFieldOutputs.Nodes.Add(fieldNames[i]);
                else
                {
                    node = _resultFieldOutputs.Nodes[indices[i]];
                    node.Nodes.Clear();
                    node.Text = fieldNames[i];
                }
                node.Name = node.Text;
                node.Tag = new Field(fieldNames[i]);
                SetNodeImage(node, "Dots.ico");
                //
                for (int j = 0; j < components[i].Length; j++)
                {
                    child = node.Nodes.Add(components[i][j]);
                    child.Name = child.Text;
                    child.Tag = new FieldData(child.Name);
                    SetNodeImage(child, "Dots.ico");
                }
                //
                if (i <= 1) node.Expand();
            }
            //
            _resultFieldOutputs.Expand();
            //
            int n = _resultFieldOutputs.Nodes.Count;
            if (n > 0) _resultFieldOutputs.Text = _resultFieldOutputsName + " (" + n + ")";
            // Add result field outputs
            foreach (var resultFieldOutput in resultFieldOutputs)
            {
                node = _resultFieldOutputs.Nodes[resultFieldOutput.Name];
                if (node != null) node.Tag = resultFieldOutput;    // overwrite Field with ResultFieldOutput
                SetNodeStatus(node);
            }
        }
        public void SelectFirstComponentOfFirstFieldOutput()
        {
            if (_resultFieldOutputs.Nodes.Count > 0)
            {
                foreach (TreeNode node in _resultFieldOutputs.Nodes)
                {
                    if (node.Nodes.Count > 0)
                    {
                        node.Expand();
                        cltvResults.SelectedNodes.Clear();  // for the shift key to work
                        cltvResults.SelectedNode = node.Nodes[0];
                        return;
                    }
                }
            }
        }
        private void CreateOverwriteHistoryOutputNodes(HistoryResultSet[] historyResultSets,
                                                       ResultHistoryOutput[] resultHistoryOutputs,
                                                       int[] indices = null)
        {
            TreeNode node1;
            TreeNode node2;
            TreeNode node3;
            for (int i = 0; i < historyResultSets.Length; i++)
            {
                if (historyResultSets[i] == null) continue;
                //
                if (indices == null) node1 = _resultHistoryOutputs.Nodes.Add(historyResultSets[i].Name);
                else
                {
                    node1 = _resultHistoryOutputs.Nodes[indices[i]];
                    node1.Nodes.Clear();
                    node1.Text = historyResultSets[i].Name;
                }
                node1.Name = node1.Text;
                node1.Tag = historyResultSets[i];
                SetNodeImage(node1, "Dots.ico");
                //
                foreach (var fieldEntry in historyResultSets[i].Fields)
                {
                    node2 = node1.Nodes.Add(fieldEntry.Key);
                    node2.Name = node2.Text;
                    node2.Tag = fieldEntry.Value;
                    SetNodeImage(node2, "Dots.ico");
                    //
                    foreach (var componentEntry in fieldEntry.Value.Components)
                    {
                        node3 = node2.Nodes.Add(componentEntry.Key);
                        node3.Name = node3.Text;
                        node3.Tag = new HistoryResultData(historyResultSets[i].Name, fieldEntry.Key, componentEntry.Key); // to view
                        SetNodeImage(node3, "Dots.ico");
                    }
                    if (node2.Nodes.Count > 0) node2.Expand();
                }
                if (node1.Nodes.Count > 0) node1.Expand();
            }
            //
            _resultHistoryOutputs.Expand();
            //
            int n = _resultHistoryOutputs.Nodes.Count;
            if (n > 0) _resultHistoryOutputs.Text = _resultHistoryOutputsName + " (" + n + ")";
            // Add result history outputs
            TreeNode node;
            foreach (var resultHistoryOutput in resultHistoryOutputs)
            {
                node = _resultHistoryOutputs.Nodes[resultHistoryOutput.Name];
                if (node != null) node.Tag = resultHistoryOutput;    // overwrite Field with ResultFieldOutput
                SetNodeStatus(node);
            }
        }
        // Expand/Collapse                                                                                                          
        private CodersLabTreeView GetActiveTree()
        {
            if (tcGeometryModelResults.TabPages.Count <= 0) return null;
            //
            if (tcGeometryModelResults.SelectedTab == tpGeometry) return cltvGeometry;
            else if (tcGeometryModelResults.SelectedTab == tpModel) return cltvModel;
            else if (tcGeometryModelResults.SelectedTab == tpResults) return cltvResults;
            else throw new NotSupportedException();
        }
        private CodersLabTreeView GetTree(ViewType view)
        {
            if (view == ViewType.Geometry) return cltvGeometry;
            else if (view == ViewType.Model) return cltvModel;
            else if (view == ViewType.Results) return cltvResults;
            else throw new NotSupportedException();
        }
        public bool[][] GetAllTreesExpandCollapseState(out string[][] allNames, bool skipModelParts = false)
        {
            bool[][] allStates = new bool[3][];
            allNames = new string[3][];
            allStates[0] = GetTreeExpandCollapseState(cltvGeometry, out allNames[0], skipModelParts);
            allStates[1] = GetTreeExpandCollapseState(cltvModel, out allNames[1], skipModelParts);
            allStates[2] = GetTreeExpandCollapseState(cltvResults, out allNames[2], skipModelParts);
            return allStates;
        }
        public bool[] GetTreeExpandCollapseState(CodersLabTreeView tree, out string[] names, bool skipModelParts = false)
        {
            List<bool> states = new List<bool>();
            List<string> namesList = new List<string>();
            foreach (TreeNode node in tree.Nodes) GetNodeExpandCollapseState(node, ref states, ref namesList, skipModelParts);
            names = namesList.ToArray();
            return states.ToArray();
        }
        private void GetNodeExpandCollapseState(TreeNode node, ref List<bool> states,
                                                ref List<string> names, bool skipModelParts)
        {
            if (node == _modelParts && skipModelParts) return;
            //
            states.Add(node.IsExpanded);
            names.Add(node.Text);
            //
            foreach (TreeNode child in node.Nodes)
            {
                GetNodeExpandCollapseState(child, ref states, ref names, skipModelParts);
            }
        }
        public void SetAllTreeExpandCollapseState(bool[][] states)
        {
            try
            {
                if (states == null || states.Length == 0) return;
                //
                SetTreeExpandCollapseState(cltvGeometry, states[0]);
                SetTreeExpandCollapseState(cltvModel, states[1]);
                SetTreeExpandCollapseState(cltvResults, states[2]);
            }
            catch (Exception)
            {
            }
        }
        public void SetTreeExpandCollapseState(CodersLabTreeView tree, bool[] states, bool skipModelParts = false)
        {
            try
            {
                if (states == null || states.Length == 0) return;
                //
                int count = 0;
                foreach (TreeNode node in tree.Nodes) CountAllTreeNodes(node, ref count, skipModelParts);
                //
                if (states.Length == count)
                {
                    count = 0;
                    //
                    tree.BeginUpdate();
                    foreach (TreeNode node in tree.Nodes) SetNodeExpandCollapseState(node, states, ref count, skipModelParts);
                    tree.EndUpdate();
                }
            }
            catch (Exception)
            {
            }

        }
        private void SetNodeExpandCollapseState(TreeNode node, bool[] states, ref int count, bool skipModelParts)
        {
            if (node == _modelParts && skipModelParts) return;
            //
            if (states[count]) node.Expand();
            else node.Collapse();
            count++;
            //
            foreach (TreeNode child in node.Nodes) SetNodeExpandCollapseState(child, states, ref count, skipModelParts);
        }
        private void CountAllTreeNodes(TreeNode node, ref int count, bool skipModelParts)
        {
            if (node == _modelParts && skipModelParts) return;
            //
            count++;
            //
            foreach (TreeNode child in node.Nodes) CountAllTreeNodes(child, ref count, skipModelParts);
        }
        //                                                                                                                          
        private bool CanCreate(TreeNode node)
        {
            if (node.Name == _meshSetupItemsName) return true;
            else if (node.TreeView == cltvModel && node.Name == _modelNodeSetsName) return true;
            else if (node.TreeView == cltvModel && node.Name == _modelElementSetsName) return true;
            else if (node.TreeView == cltvModel && node.Name == _modelSurfacesName) return true;
            else if (node.Name == _modelReferencePointsName) return true;
            else if (node.Name == _modelCoordinateSystemsName) return true;
            else if (node.Name == _materialsName) return true;
            else if (node.Name == _sectionsName) return true;
            else if (node.Name == _constraintsName) return true;
            else if (node.Name == _surfaceInteractionsName) return true;
            else if (node.Name == _contactPairsName) return true;
            else if (node.Name == _amplitudesName) return true;
            else if (node.Name == _initialConditionsName) return true;
            else if (node.Name == _stepsName) return true;
            else if (node.Name == _modelHistoryOutputsName) return true;
            else if (node.TreeView == cltvModel && node.Name == _modelFieldOutputsName) return true;
            else if (node.Name == _boundaryConditionsName) return true;
            else if (node.Name == _loadsName) return true;
            else if (node.Name == _definedFieldsName) return true;
            else if (node.Name == _analysesName) return true;
            //
            else if (node.TreeView == cltvResults && node.Name == _resultNodeSetsName) return false;
            else if (node.TreeView == cltvResults && node.Name == _resultElementSetsName) return false;
            else if (node.TreeView == cltvResults && node.Name == _resultSurfacesName) return false;
            //
            else if (node.TreeView == cltvResults && node.Name == _resultFieldOutputsName) return true;
            else if (node.TreeView == cltvResults && node.Name == _resultHistoryOutputsName) return true;
            //
            else return false;
        }
        private bool CanDuplicate(TreeNode node)
        {
            if (node.TreeView == cltvGeometry && node.Tag is MeshSetupItem) return true;
            else if (node.TreeView == cltvModel && node.Tag is FeNodeSet) return true;
            else if (node.TreeView == cltvModel && node.Tag is FeElementSet) return true;
            else if (node.TreeView == cltvModel && node.Tag is FeSurface) return true;
            else if (node.TreeView == cltvModel && node.Tag is FeReferencePoint) return true;
            else if (node.TreeView == cltvModel && node.Tag is CoordinateSystem) return true;
            else if (node.Tag is Material) return true;
            else if (node.Tag is Section) return true;
            else if (node.Tag is Constraint) return true;
            else if (node.Tag is SurfaceInteraction) return true;
            else if (node.Tag is ContactPair) return true;
            else if (node.Tag is Amplitude) return true;
            else if (node.Tag is InitialCondition) return true;
            else if (node.Tag is Step) return true;
            else if (node.Tag is HistoryOutput) return true;
            else if (node.Tag is FieldOutput) return true;
            else if (node.Tag is BoundaryCondition) return true;
            else if (node.Tag is Load) return true;
            else if (node.Tag is DefinedField) return true;
            else if (node.Tag is AnalysisJob) return true;
            else if (node.TreeView == cltvResults && node.Tag is FeReferencePoint) return true;
            else if (node.TreeView == cltvResults && node.Tag is CoordinateSystem) return true;
            else return false;
        }
        private bool CanPropagate(TreeNode node)
        {
            if (node.Tag is HistoryOutput) return true;
            else if (node.Tag is FieldOutput) return true;
            else if (node.Tag is BoundaryCondition) return true;
            else if (node.Tag is Load) return true;
            else if (node.Tag is DefinedField) return true;
            else return false;
        }
        private bool CanPreview(TreeNode node)
        {
            if (node.Tag is IPreviewable) return true;
            else return false;
        }
        private bool CanSearchContactPairs(TreeNode node)
        {
            if (node == _constraints) return true;
            else if (node == _contactPairs) return true;
            else return false;
        }
        private bool CanSwapMergeMasterSlave(TreeNode node)
        {
            if (node.Tag is Tie) return true;
            else if (node.Tag is ContactPair) return true;
            else return false;
        }
        private bool CanHide(object item)
        {
            if (item is BasePart) return true;
            else if (item is FeReferencePoint) return true;
            else if (item is CoordinateSystem) return true;
            else if (item is Constraint) return true;
            else if (item is ContactPair) return true;
            else if (item is BoundaryCondition) return true;
            else if (item is Load) return true;
            else return false;
        }
        private bool CanDeactivate(TreeNode node)
        {
            if (node.Tag is MeshSetupItem) return true;
            else if (node.Tag is Constraint) return true;
            else if (node.Tag is ContactPair) return true;
            else if (node.Tag is InitialCondition) return true;
            else if (node.Tag is Step) return true;
            else if (node.Tag is HistoryOutput) return true;
            else if (node.Tag is FieldOutput) return true;
            else if (node.Tag is BoundaryCondition) return true;
            else if (node.Tag is Load) return true;
            else if (node.Tag is DefinedField) return true;
            else return false;
        }


        //                                                                                                                          
        public void SetNumberOfUserKeywords(int numOfUserKeywords)
        {
            _model.Text = _modelName;
            if (numOfUserKeywords > 0) _model.Text += " (User Keywords: " + numOfUserKeywords + ")";
        }

        public void UpdateNumberOfStepControls(TreeNode stepNode)
        {
            if (stepNode != null && stepNode.Tag is Step step)
            {
                stepNode.Text = step.Name;
                int numOfStepControls = step.GetNumberOfStepControls();
                if (numOfStepControls > 0) stepNode.Text += " (Step Controls: " + numOfStepControls + ")";
            }
        }

        //                                                                                                                          
        public void ShowContextMenu(Control control, int x, int y)
        {
            CodersLabTreeView tree = GetActiveTree();
            if (tree.SelectedNodes.Count > 0)
            {
                PrepareToolStripItem(tree);
                //Expand / Collapse
                tsmiSpaceExpandColapse.Visible = false;
                tsmiExpandAll.Visible = false;
                tsmiCollapseAll.Visible = false;
                //
                cmsTree.Show(control, x, y);
            }
        }

        
    }
}
