// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeJob;
using CaeMesh;
using CaeModel;
using CaeResults;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static CaeGlobals.Geometry2;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

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
        public HashSet<Type> Types;
        //
        public int Create;
        public int Edit;
        public int EditStepControls;
        public int Query;
        public int Duplicate;
        public int Propagate;
        public int Preview;
        //
        public int AddToPlot;
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
        public int ColorTransparency;
        //
        public int ResultRepresentationOn;
        public int ResultRepresentationOff;
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
        private sealed class OrderedDictionaryTreeContext
        {
            public ViewType View;
            public Type ItemType;
            public string ParentName;
            public IOrderedDictionaryItemLayout<string> Dictionary;
            public TreeNode RootNode;
        }
        private sealed class OrderedDictionaryGroupInfo
        {
            public OrderedDictionaryTreeContext Context;
            public Guid Id;
            public string Name;
        }
        private sealed class OrderedDictionaryTreeDragData
        {
            public CodersLabTreeView Tree;
            public TreeNode[] Nodes;
            public string[] ItemNames;
            public OrderedDictionaryTreeContext Context;
            public bool IsGroup;
        }


        private sealed class StepTreeDragData
        {
            public CodersLabTreeView Tree;
            public TreeNode[] Nodes;
            public string[] StepNames;
        }
        // Variables                                                                                                                
        private bool _screenUpdating;
        private bool _doubleClick;
        private const int WM_MOUSEMOVE = 0x0200;
        private bool _disableMouse;
        private bool _disableSelectionsChanged;
        private Dictionary<CodersLabTreeView, bool[]> _prevStates;
        private Dictionary<TreeNode, OrderedDictionaryTreeContext> _orderedDictionaryContexts;
        private Dictionary<TreeNode, OrderedDictionaryGroupInfo> _orderedDictionaryGroups;
        private Dictionary<TreeNode, string> _splitSubPartNodes;
        private TreeNode[] _contextMenuNodes;
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
        private TreeNode _distributions;            //   2
        private TreeNode _amplitudes;               //   2
        private TreeNode _initialConditions;        //   2
        private TreeNode _steps;                    //   2
        private TreeNode _analyses;                 // 1
        private int _numOfUserKeywords;
        private int _numOfModifiedKeywords;
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
        private string _distributionsName = "Distributions";
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
                stbGeometrySearch.Enabled = !value;
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
                stbGeometrySearch.Enabled = !value;
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
        public bool EditModeActive { get { return GetActiveTree().EditModeActive; } }
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
        public string DistributionsName { get { return _distributionsName; } }
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
        public event Action<NamedClass[], bool> SelectEvent;
        public event Action ClearSelectionEvent;
        //
        public event Action<string, string> CreateEvent;
        public event Action<NamedClass, string> EditEvent;
        public event Action<NamedClass, string, string> RenameEvent;
        public event Action<string, string, string> RenameSplitSubPartEvent;
        public event Action<string, string[]> GroupSplitCellsEvent;
        //
        public event Action<string> EditStepControlsEvent;
        public event Action QueryEvent;
        public event Action<NamedClass[], string[]> DuplicateEvent;
        public event Action<NamedClass[], string[]> PropagateEvent;
        public event Action<NamedClass[], string[]> PreviewEvent;
        public event Action<NamedClass[]> AddItemsToPlotEvent;
        public event Action<NamedClass[]> AddItemsToNewPlotEvent;
        public Func<bool> CanAddItemsToNewPlot;
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
        public event Action<string[]> SetPartColorEvent;
        public event Action<string[]> ResetPartColorEvent;
        public event Action<string[]> SetTransparencyEvent;
        public event Action<NamedClass[], bool> ResultRepresentationVisibilityEvent;
        public event Action<string> RunEvent;
        public event Action<string> CheckModelEvent;
        public event Action<string> MonitorEvent;
        public event Action<string> ResultsEvent;
        public event Action<string> KillEvent;
        public event Action<NamedClass[], bool, string[]> ActivateDeactivateEvent;
        public event Action<NamedClass[], string[]> DeleteEvent;
        public event Action<OrderedDictionaryTreeLayoutChange> OrderedDictionaryTreeLayoutChangeEvent;
        public event Action<string[], int> MoveStepsEvent;
        //
        public event Action<string[]> FieldDataSelectEvent;
        public event Action RenderingOn;
        public event Action RenderingOff;


        // Callbacks                                                                                                                
        public Action RegenerateTreeCallBack;
        public Func<NamedClass, string, string, bool> CheckItemName;
        public Func<string> GetNextPartGroupName;
        public Func<Guid?, string, bool> CheckPartGroupName;
        // Returns the names of the geometry items related to the given focused items
        // (a part -> the mesh setup items that reference it, and a mesh setup item -> the parts it references).
        public Func<NamedClass[], string[]> GetRelatedGeometryItemNames;
        // Returns the names of the parts the given mesh setup items are defined on, as they are meshed
        public Func<string[], string[]> GetPartNamesToMeshFromMeshSetupItems;


        // Constructors                                                                                                             
        public ModelTree()
        {
            InitializeComponent();
            AssembleTreeContextMenu();
            //
            cltvGeometry.IsNodeRenameable = CanRename;
            cltvModel.IsNodeRenameable = CanRename;
            cltvResults.IsNodeRenameable = CanRename;
            //
            cltvGeometry.CheckNodeName = CheckNodeName;
            cltvModel.CheckNodeName = CheckNodeName;
            cltvResults.CheckNodeName = CheckNodeName;
            //
            _orderedDictionaryContexts = new Dictionary<TreeNode, OrderedDictionaryTreeContext>();
            _orderedDictionaryGroups = new Dictionary<TreeNode, OrderedDictionaryGroupInfo>();
            _splitSubPartNodes = new Dictionary<TreeNode, string>();
            InitializeOrderedDictionaryDragDrop(cltvGeometry);
            InitializeOrderedDictionaryDragDrop(cltvModel);
            InitializeOrderedDictionaryDragDrop(cltvResults);
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
            _distributions = cltvModel.Nodes.Find(_distributionsName, true)[0];
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
            _distributions.StateImageKey = "Distributions";
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


        // Event handlers                                                                                                           
        private void tcGeometryModelResults_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            e.Cancel = _disableMouse;
        }
        private void stbGeometry_TextChanged(object sender, EventArgs e)
        {
            FilterTree(cltvGeometry, stbGeometrySearch.Text);
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
        private ViewType GetViewType(CodersLabTreeView tree)
        {
            if (tree == cltvGeometry) return ViewType.Geometry;
            else if (tree == cltvModel) return ViewType.Model;
            else if (tree == cltvResults) return ViewType.Results;
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
        private void AssembleTreeContextMenu()
        {
            if (LicenseManager.UsageMode == LicenseUsageMode.Designtime) return;
            //
            while (cmsTreeSecondary.Items.Count > 0)
            {
                ToolStripItem item = cmsTreeSecondary.Items[0];
                cmsTreeSecondary.Items.RemoveAt(0);
                cmsTree.Items.Add(item);
            }
        }
        private void InitializeOrderedDictionaryDragDrop(CodersLabTreeView tree)
        {
            tree.AllowDrop = true;
            tree.ItemDrag += cltv_ItemDrag;
            tree.DragEnter += cltv_DragEnter;
            tree.DragOver += cltv_DragOver;
            tree.DragDrop += cltv_DragDrop;
            tree.DragLeave += cltv_DragLeave;
        }
        private bool TryGetOrderedDictionaryContext(TreeNode node, out OrderedDictionaryTreeContext context)
        {
            while (node != null)
            {
                if (_orderedDictionaryContexts.TryGetValue(node, out context)) return true;
                node = node.Parent;
            }
            //
            context = null;
            return false;
        }
        private OrderedDictionaryTreeContext EnsureOrderedDictionaryContext(
            TreeNode rootNode, ViewType view, Type itemType, string parentName,
            IOrderedDictionaryItemLayout<string> dictionary)
        {
            if (rootNode == null || dictionary == null) return null;
            if (_orderedDictionaryContexts.TryGetValue(rootNode, out OrderedDictionaryTreeContext context) &&
                ReferenceEquals(context.Dictionary, dictionary))
                return context;
            //
            context = new OrderedDictionaryTreeContext
            {
                View = view,
                ItemType = itemType,
                ParentName = parentName,
                Dictionary = dictionary,
                RootNode = rootNode
            };
            _orderedDictionaryContexts[rootNode] = context;
            return context;
        }
        private bool IsOrderedDictionaryItemNode(TreeNode node, OrderedDictionaryTreeContext context)
        {
            if (!(node?.Tag is NamedClass item) || context == null) return false;
            return ContainsOrderedDictionaryItem(context, item.Name);
        }
        private bool IsCompoundGeometrySubPartNode(TreeNode node)
        {
            if (node?.Parent == null) return false;
            return node.Parent.Tag is CompoundGeometryPart || _splitSubPartNodes.ContainsKey(node.Parent);
        }
        // The cells of one split compound, which can be put under one sub part unless they already are the whole
        // of a single sub part
        private bool TryGetSplitCellsToGroup(TreeNode[] nodes, out string compoundPartName, out string[] cellNames)
        {
            compoundPartName = null;
            cellNames = null;
            if (nodes.Length == 0) return false;
            //
            CompoundGeometryPart compoundPart = null;
            List<string> names = new List<string>();
            foreach (TreeNode node in nodes)
            {
                if (!(node.Tag is GeometryPart cell) || !IsCompoundGeometrySubPartNode(node)) return false;
                TreeNode compoundNode = _splitSubPartNodes.ContainsKey(node.Parent) ? node.Parent.Parent : node.Parent;
                if (!(compoundNode?.Tag is CompoundGeometryPart nodeCompoundPart) || !nodeCompoundPart.CreatedBySplit)
                    return false;
                if (compoundPart == null) compoundPart = nodeCompoundPart;
                else if (compoundPart != nodeCompoundPart) return false;
                names.Add(cell.Name);
            }
            //
            HashSet<string> subPartNames = new HashSet<string>();
            foreach (string name in names)
                subPartNames.Add(compoundPart.CellSubPartNames[Array.IndexOf(compoundPart.SubPartNames, name)]);
            if (subPartNames.Count == 1 && compoundPart.GetCellNames(subPartNames.First()).Length == names.Count)
                return false;
            //
            compoundPartName = compoundPart.Name;
            cellNames = names.ToArray();
            return true;
        }
        private NamedClass[] GetSplitSubPartCellItems(TreeNode subPartNode)
        {
            return subPartNode.Nodes.Cast<TreeNode>().Select(cellNode => cellNode.Tag as NamedClass)
                              .Where(cell => cell != null).ToArray();
        }
        private TreeNode[] GetSelectedNodesIncludingGroupItems(CodersLabTreeView tree)
        {
            // The context menu can be shown for nodes that are not the selected ones
            if (_contextMenuNodes != null && tree == cltvGeometry) return _contextMenuNodes;
            //
            List<TreeNode> nodes = new List<TreeNode>();
            HashSet<TreeNode> addedNodes = new HashSet<TreeNode>();
            //
            foreach (TreeNode selectedNode in tree.SelectedNodes)
            {
                AddSelectedNodeIncludingGroupItems(selectedNode, nodes, addedNodes);
            }
            return nodes.ToArray();
        }
        private TreeNode GetSingleSelectedNodeIncludingGroupItems(CodersLabTreeView tree)
        {
            TreeNode[] nodes = GetSelectedNodesIncludingGroupItems(tree);
            return nodes.Length == 1 ? nodes[0] : null;
        }
        private string[] GetSelectedItemNamesIncludingGroupItems(CodersLabTreeView tree)
        {
            return GetSelectedNodesIncludingGroupItems(tree)
                .Where(node => node.Tag is NamedClass)
                .Select(node => ((NamedClass)node.Tag).Name)
                .ToArray();
        }
        private void AddSelectedNodeIncludingGroupItems(TreeNode node, List<TreeNode> nodes,
                                                        HashSet<TreeNode> addedNodes)
        {
            if (_orderedDictionaryGroups.ContainsKey(node) || _splitSubPartNodes.ContainsKey(node))
            {
                foreach (TreeNode childNode in node.Nodes)
                {
                    if (_orderedDictionaryGroups.ContainsKey(childNode))
                        AddSelectedNodeIncludingGroupItems(childNode, nodes, addedNodes);
                    else if (childNode.Tag != null && addedNodes.Add(childNode))
                        nodes.Add(childNode);
                }
            }
            else if (addedNodes.Add(node)) nodes.Add(node);
        }
        private string[] GetOrderedDictionaryItemNames(IEnumerable<TreeNode> nodes,
                                                       OrderedDictionaryTreeContext context)
        {
            List<string> itemNames = new List<string>();
            HashSet<string> addedItemNames = new HashSet<string>(context.Dictionary.Comparer);
            //
            foreach (TreeNode node in nodes)
            {
                if (!(node.Tag is NamedClass item)) continue;
                if (addedItemNames.Add(item.Name)) itemNames.Add(item.Name);
                //
                if (context.View == ViewType.Geometry && item is CompoundGeometryPart compoundPart)
                {
                    foreach (string subPartName in compoundPart.SubPartNames)
                    {
                        if (ContainsOrderedDictionaryItem(context, subPartName) &&
                            addedItemNames.Add(subPartName))
                            itemNames.Add(subPartName);
                    }
                }
            }
            return itemNames.ToArray();
        }
        private static bool ContainsOrderedDictionaryItem(OrderedDictionaryTreeContext context, string itemName)
        {
            return context.Dictionary.ContainsKey(itemName);
        }
        private TreeNode FindOrderedDictionaryGroupNode(OrderedDictionaryTreeContext context, Guid groupId)
        {
            return _orderedDictionaryGroups
                .Where(entry => ReferenceEquals(entry.Value.Context, context) && entry.Value.Id == groupId)
                .Select(entry => entry.Key)
                .FirstOrDefault();
        }
        private bool CanCreateOrderedDictionaryGroup(CodersLabTreeView tree)
        {
            if (tree.SelectedNodes.Count == 0) return false;
            if (tree.SelectedNodes.Count == 1)
            {
                TreeNode node = tree.SelectedNode;
                if (_orderedDictionaryContexts.ContainsKey(node) || _orderedDictionaryGroups.ContainsKey(node))
                    return true;
            }
            //
            OrderedDictionaryTreeContext context = null;
            foreach (TreeNode node in tree.SelectedNodes)
            {
                if (IsCompoundGeometrySubPartNode(node)) return false;
                //
                OrderedDictionaryTreeContext nodeContext;
                if (!TryGetOrderedDictionaryContext(node, out nodeContext) ||
                    !IsOrderedDictionaryItemNode(node, nodeContext))
                    return false;
                if (context == null) context = nodeContext;
                else if (!ReferenceEquals(context, nodeContext)) return false;
            }
            return context != null;
        }
        private void tsmiCreateGroup_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                if (!CanCreateOrderedDictionaryGroup(tree)) return;
                //
                OrderedDictionaryTreeContext context;
                Guid? parentGroupId = null;
                int index;
                string[] itemNames = null;
                TreeNode selectedNode = tree.SelectedNode;
                OrderedDictionaryItemLayout<string> layout;
                //
                if (tree.SelectedNodes.Count == 1 &&
                    _orderedDictionaryContexts.TryGetValue(selectedNode, out context))
                {
                    layout = context.Dictionary.GetItemLayoutSnapshot();
                    index = layout.Nodes.Count;
                }
                else if (tree.SelectedNodes.Count == 1 &&
                         _orderedDictionaryGroups.TryGetValue(selectedNode, out OrderedDictionaryGroupInfo groupInfo))
                {
                    context = groupInfo.Context;
                    parentGroupId = groupInfo.Id;
                    layout = context.Dictionary.GetItemLayoutSnapshot();
                    index = GetLayoutChildren(layout.Nodes, parentGroupId.Value).Count;
                }
                else
                {
                    TryGetOrderedDictionaryContext(selectedNode, out context);
                    layout = context.Dictionary.GetItemLayoutSnapshot();
                    itemNames = GetOrderedDictionaryItemNames(tree.SelectedNodes.Cast<TreeNode>(), context);
                    Guid? commonParentId = null;
                    bool first = true;
                    bool sameParent = true;
                    int firstIndex = Int32.MaxValue;
                    //
                    foreach (string itemName in itemNames)
                    {
                        FindLayoutItem(layout.Nodes, itemName, context.Dictionary.Comparer,
                                       out Guid? itemParentId, out int itemIndex);
                        if (first)
                        {
                            commonParentId = itemParentId;
                            first = false;
                        }
                        else if (commonParentId != itemParentId) sameParent = false;
                        if (itemIndex < firstIndex) firstIndex = itemIndex;
                    }
                    //
                    if (sameParent)
                    {
                        parentGroupId = commonParentId;
                        index = firstIndex;
                    }
                    else
                    {
                        parentGroupId = null;
                        index = layout.Nodes.Count;
                    }
                }
                //
                string groupName;
                if ((context.View == ViewType.Geometry || context.View == ViewType.Model) &&
                    typeof(BasePart).IsAssignableFrom(context.ItemType) &&
                    GetNextPartGroupName != null)
                    groupName = GetNextPartGroupName();
                else
                    groupName = GetNextGroupName(layout, parentGroupId);
                Guid newGroupId = Guid.NewGuid();
                InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(
                    OrderedDictionaryTreeLayoutChange.CreateGroup(
                        context.View, context.ItemType, context.ParentName, itemNames, newGroupId,
                        parentGroupId, index, groupName),
                    context, parentGroupId);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void RemoveSelectedGroups()
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                TreeNode[] selectedNodes = tree.SelectedNodes.Cast<TreeNode>().ToArray();
                if (selectedNodes.Any(node => !_orderedDictionaryGroups.ContainsKey(node))) return;
                //
                OrderedDictionaryTreeLayoutChange change;
                Guid? parentGroupId;
                if (!TryGetSelectedGroupRemoval(selectedNodes, out change, out parentGroupId)) return;
                InvokeOrderedDictionaryGroupRemoval(change, parentGroupId);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private bool TryGetSelectedGroupRemoval(TreeNode[] selectedNodes,
                                                out OrderedDictionaryTreeLayoutChange change,
                                                out Guid? parentGroupId)
        {
            change = null;
            parentGroupId = null;
            //
            TreeNode[] groupNodes = selectedNodes.Where(node => _orderedDictionaryGroups.ContainsKey(node))
                                                 .OrderByDescending(node => node.Level)
                                                 .ThenBy(node => node.Index)
                                                 .ToArray();
            if (groupNodes.Length == 0) return false;
            //
            OrderedDictionaryGroupInfo firstGroupInfo = _orderedDictionaryGroups[groupNodes[0]];
            if (groupNodes.Any(node =>
                !ReferenceEquals(_orderedDictionaryGroups[node].Context, firstGroupInfo.Context)))
                return false;
            //
            TreeNode commonParent = groupNodes[0].Parent;
            if (groupNodes.All(node => ReferenceEquals(node.Parent, commonParent)) &&
                commonParent != null &&
                _orderedDictionaryGroups.TryGetValue(commonParent, out OrderedDictionaryGroupInfo parentGroupInfo))
                parentGroupId = parentGroupInfo.Id;
            //
            Guid[] groupIds = groupNodes.Select(node => _orderedDictionaryGroups[node].Id).ToArray();
            change = OrderedDictionaryTreeLayoutChange.RemoveGroups(
                firstGroupInfo.Context.View, firstGroupInfo.Context.ItemType,
                firstGroupInfo.Context.ParentName, groupIds);
            return true;
        }
        private void InvokeOrderedDictionaryGroupRemoval(OrderedDictionaryTreeLayoutChange change,
                                                         Guid? parentGroupId)
        {
            OrderedDictionaryTreeContext context = _orderedDictionaryContexts.Values.FirstOrDefault(
                item => item.View == change.View && item.ItemType == change.ItemType &&
                        String.Equals(item.ParentName, change.ParentName, StringComparison.Ordinal));
            if (context == null) return;
            //
            Guid[] groupIds = change.GroupIds
                .Where(groupId => FindOrderedDictionaryGroupNode(context, groupId) != null).ToArray();
            if (groupIds.Length == 0) return;
            if (groupIds.Length != change.GroupIds.Length)
                change = OrderedDictionaryTreeLayoutChange.RemoveGroups(change.View, change.ItemType,
                                                                        change.ParentName, groupIds);
            //
            InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(change, context, parentGroupId);
        }
        private void InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(
            OrderedDictionaryTreeLayoutChange change, OrderedDictionaryTreeContext previousContext,
            Guid? parentGroupId)
        {
            CodersLabTreeView previousTree = previousContext.RootNode.TreeView as CodersLabTreeView;
            CodersLabTreeView rebuiltTree = null;
            string topNodeIdentity = previousTree != null && previousTree.TopNode != null
                                   ? GetStableTreeNodeIdentity(previousTree.TopNode) : null;
            previousTree?.BeginUpdate();
            try
            {
                Dictionary<string, bool> expandCollapseState = GetStableTreeExpandCollapseState(previousTree);
                bool? movedGroupExpanded = null;
                if (change.Action == OrderedDictionaryTreeLayoutAction.MoveGroup && change.GroupId.HasValue)
                {
                    TreeNode movedGroupNode =
                        FindOrderedDictionaryGroupNode(previousContext, change.GroupId.Value);
                    if (movedGroupNode != null) movedGroupExpanded = movedGroupNode.IsExpanded;
                }
                //
                OrderedDictionaryTreeLayoutChangeEvent?.Invoke(change);
                //
                OrderedDictionaryTreeContext context = _orderedDictionaryContexts.Values.FirstOrDefault(
                    item => item.View == previousContext.View &&
                            item.ItemType == previousContext.ItemType &&
                            String.Equals(item.ParentName, previousContext.ParentName, StringComparison.Ordinal));
                if (context == null) return;
                //
                CodersLabTreeView tree = context.RootNode.TreeView as CodersLabTreeView;
                rebuiltTree = tree;
                SetStableTreeExpandCollapseState(tree, expandCollapseState);
                //
                TreeNode parentNode = context.RootNode;
                if (parentGroupId.HasValue)
                {
                    parentNode = FindOrderedDictionaryGroupNode(context, parentGroupId.Value);
                }
                //
                TreeNode nodeToExpand = parentNode;
                while (nodeToExpand != null)
                {
                    nodeToExpand.Expand();
                    nodeToExpand = nodeToExpand.Parent;
                }
                //
                if (change.Action == OrderedDictionaryTreeLayoutAction.CreateGroup)
                {
                    TreeNode groupNode = FindOrderedDictionaryGroupNode(context, change.GroupId.Value);
                    if (groupNode != null && !ReferenceEquals(groupNode.Parent, parentNode)) groupNode = null;
                    if (groupNode != null) tree.SelectedNode = groupNode;
                }
                else if (change.Action == OrderedDictionaryTreeLayoutAction.MoveItems)
                {
                    SelectOrderedDictionaryItems(tree, context, change.ItemNames);
                }
                else if (change.Action == OrderedDictionaryTreeLayoutAction.MoveGroup && change.GroupId.HasValue)
                {
                    TreeNode groupNode = FindOrderedDictionaryGroupNode(context, change.GroupId.Value);
                    if (groupNode != null)
                    {
                        tree.SelectedNode = groupNode;
                        if (movedGroupExpanded.HasValue)
                        {
                            if (movedGroupExpanded.Value) groupNode.Expand();
                            else groupNode.Collapse();
                        }
                    }
                }
            }
            finally
            {
                previousTree?.EndUpdate();
                // The rebuilt tree starts at its first row and the restored selection scrolls it again.
                // EndUpdate only invalidates, so the view is corrected before the single repaint.
                RestoreTreeTopNode(rebuiltTree, topNodeIdentity);
            }
        }
        private void RestoreTreeTopNode(CodersLabTreeView tree, string topNodeIdentity)
        {
            if (tree == null || topNodeIdentity == null) return;
            //
            TreeNode topNode = FindTreeNodeByStableIdentity(tree.Nodes, topNodeIdentity);
            if (topNode == null) return;
            //
            for (TreeNode parentNode = topNode.Parent; parentNode != null; parentNode = parentNode.Parent)
            {
                if (!parentNode.IsExpanded) return;
            }
            tree.TopNode = topNode;
        }
        private TreeNode FindTreeNodeByStableIdentity(TreeNodeCollection nodes, string topNodeIdentity)
        {
            foreach (TreeNode node in nodes)
            {
                if (String.Equals(GetStableTreeNodeIdentity(node), topNodeIdentity, StringComparison.Ordinal))
                    return node;
                //
                TreeNode foundNode = FindTreeNodeByStableIdentity(node.Nodes, topNodeIdentity);
                if (foundNode != null) return foundNode;
            }
            return null;
        }
        private void SelectOrderedDictionaryItems(CodersLabTreeView tree, OrderedDictionaryTreeContext context,
                                                  string[] itemNames)
        {
            if (tree == null || itemNames == null) return;
            //
            Dictionary<string, TreeNode> itemNodes =
                new Dictionary<string, TreeNode>(context.Dictionary.Comparer);
            GetOrderedDictionaryItemNodes(context.RootNode, context, itemNodes);
            //
            try
            {
                _disableSelectionsChanged = true;
                var selectedNodes = tree.SelectedNodes;
                selectedNodes.Clear();
                foreach (string itemName in itemNames)
                {
                    if (itemNodes.TryGetValue(itemName, out TreeNode itemNode)) selectedNodes.Add(itemNode);
                }
            }
            finally
            {
                _disableSelectionsChanged = false;
            }
            UpdateHighlight();
        }
        private void GetOrderedDictionaryItemNodes(TreeNode parentNode, OrderedDictionaryTreeContext context,
                                                   Dictionary<string, TreeNode> itemNodes)
        {
            foreach (TreeNode node in parentNode.Nodes)
            {
                if (_orderedDictionaryGroups.TryGetValue(node, out OrderedDictionaryGroupInfo groupInfo) &&
                    ReferenceEquals(groupInfo.Context, context))
                {
                    GetOrderedDictionaryItemNodes(node, context, itemNodes);
                }
                else if (IsOrderedDictionaryItemNode(node, context))
                {
                    NamedClass item = (NamedClass)node.Tag;
                    itemNodes[item.Name] = node;
                }
            }
        }
        private Dictionary<string, bool> GetStableTreeExpandCollapseState(CodersLabTreeView tree)
        {
            Dictionary<string, bool> state = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (tree == null) return state;
            //
            foreach (TreeNode node in tree.Nodes) GetStableNodeExpandCollapseState(node, state);
            return state;
        }
        private void GetStableNodeExpandCollapseState(TreeNode node, Dictionary<string, bool> state)
        {
            // A node without children cannot be expanded, so what it reports says nothing of what the user
            // wants once it has children again - after a regenerate emptied the tree, or a part became a
            // compound - and it is left to take its default
            if (node.Nodes.Count > 0) state[GetStableTreeNodeIdentity(node)] = node.IsExpanded;
            foreach (TreeNode child in node.Nodes) GetStableNodeExpandCollapseState(child, state);
        }
        private void SetStableTreeExpandCollapseState(CodersLabTreeView tree, Dictionary<string, bool> state)
        {
            if (tree == null || state == null) return;
            //
            try
            {
                tree.BeginUpdate();
                foreach (TreeNode node in tree.Nodes) SetStableNodeExpandCollapseState(node, state);
            }
            finally
            {
                tree.EndUpdate();
            }
        }
        private void SetStableNodeExpandCollapseState(TreeNode node, Dictionary<string, bool> state)
        {
            if (state.TryGetValue(GetStableTreeNodeIdentity(node), out bool expanded))
            {
                if (expanded) node.Expand();
                else node.Collapse();
            }
            //
            foreach (TreeNode child in node.Nodes) SetStableNodeExpandCollapseState(child, state);
        }
        private string GetStableTreeNodeIdentity(TreeNode node)
        {
            if (_orderedDictionaryGroups.TryGetValue(node, out OrderedDictionaryGroupInfo groupInfo))
                return "Group:" + groupInfo.Id.ToString("N");
            if (node.Tag is CompoundGeometryPart compoundPart)
                return "CompoundGeometryPart:" + compoundPart.Name;
            //
            Stack<string> names = new Stack<string>();
            while (node != null)
            {
                names.Push(node.Name ?? "");
                node = node.Parent;
            }
            //
            StringBuilder identity = new StringBuilder("Path:");
            foreach (string name in names)
            {
                identity.Append(name.Length);
                identity.Append(':');
                identity.Append(name);
            }
            return identity.ToString();
        }
        private static IList<OrderedDictionaryItemNode<string>> GetLayoutChildren(
            IEnumerable<OrderedDictionaryItemNode<string>> nodes, Guid groupId)
        {
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup) continue;
                if (node.Id == groupId) return node.Children;
                IList<OrderedDictionaryItemNode<string>> children = GetLayoutChildren(node.Children, groupId);
                if (children != null) return children;
            }
            return null;
        }
        private static void FindLayoutItem(IEnumerable<OrderedDictionaryItemNode<string>> nodes, string key,
                                           IEqualityComparer<string> comparer, out Guid? parentGroupId,
                                           out int index)
        {
            if (TryFindLayoutItem(nodes, key, comparer, null, out parentGroupId, out index)) return;
            throw new KeyNotFoundException("The item was not found in the ordered dictionary layout.");
        }
        private void MoveTreeNodeToOrderedDictionaryLayoutParent(TreeNode rootNode, TreeNode itemNode)
        {
            if (!_orderedDictionaryContexts.TryGetValue(rootNode, out OrderedDictionaryTreeContext context)) return;
            //
            OrderedDictionaryItemLayout<string> layout = context.Dictionary.GetItemLayoutSnapshot();
            if (!TryFindLayoutItem(layout.Nodes, itemNode.Name, context.Dictionary.Comparer, null,
                                   out Guid? parentGroupId, out _))
                return;
            //
            TreeNode targetParentNode = context.RootNode;
            if (parentGroupId.HasValue)
            {
                targetParentNode = FindOrderedDictionaryGroupNode(context, parentGroupId.Value);
                if (targetParentNode == null) return;
            }
            if (ReferenceEquals(itemNode.Parent, targetParentNode)) return;
            //
            itemNode.Remove();
            targetParentNode.Nodes.Add(itemNode);
        }
        public void UpdateTreeNodeLayout(ViewType view, string itemName, NamedClass item, string parentName)
        {
            if (!_screenUpdating) return;
            //
            CodersLabTreeView tree = GetTree(view);
            try
            {
                tree.BeginUpdate();
                TreeNode itemNode = FindTreeNode(view, itemName, item, parentName);
                if (itemNode == null ||
                    !TryGetOrderedDictionaryContext(itemNode, out OrderedDictionaryTreeContext context))
                    return;
                //
                MoveTreeNodeToOrderedDictionaryLayoutParent(context.RootNode, itemNode);
                TreeNode nodeToExpand = itemNode.Parent;
                while (nodeToExpand != null)
                {
                    nodeToExpand.Expand();
                    nodeToExpand = nodeToExpand.Parent;
                }
            }
            finally
            {
                tree.EndUpdate();
            }
        }
        private static bool TryFindLayoutItem(IEnumerable<OrderedDictionaryItemNode<string>> nodes, string key,
                                              IEqualityComparer<string> comparer, Guid? currentParentId,
                                              out Guid? parentGroupId, out int index)
        {
            int currentIndex = 0;
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (node.IsItem && comparer.Equals(node.Key, key))
                {
                    parentGroupId = currentParentId;
                    index = currentIndex;
                    return true;
                }
                if (node.IsGroup &&
                    TryFindLayoutItem(node.Children, key, comparer, node.Id, out parentGroupId, out index))
                    return true;
                currentIndex++;
            }
            //
            parentGroupId = null;
            index = -1;
            return false;
        }
        private static string GetNextGroupName(OrderedDictionaryItemLayout<string> layout, Guid? parentGroupId)
        {
            IEnumerable<OrderedDictionaryItemNode<string>> nodes = parentGroupId.HasValue
                ? GetLayoutChildren(layout.Nodes, parentGroupId.Value)
                : layout.Nodes;
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (node.IsGroup) names.Add(node.Name);
            }
            //
            string name = "Group";
            int id = 1;
            while (names.Contains(name)) name = "Group-" + id++;
            return name;
        }
        private bool TryGetStepTreeDragData(CodersLabTreeView tree, TreeNode[] nodes,
                                            out StepTreeDragData data)
        {
            data = null;
            if (nodes.Length == 0) return false;
            //
            foreach (TreeNode node in nodes)
            {
                if (!IsStepTreeNode(node)) return false;
            }
            //
            data = new StepTreeDragData
            {
                Tree = tree,
                Nodes = nodes,
                StepNames = nodes.Select(node => ((Step)node.Tag).Name).ToArray()
            };
            return true;
        }
        private bool IsStepTreeNode(TreeNode node)
        {
            return node != null && ReferenceEquals(node.Parent, _steps) && node.Tag is Step;
        }
        private void cltv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            try
            {
                CodersLabTreeView tree = (CodersLabTreeView)sender;
                TreeNode[] nodes = tree.SelectedNodes.Cast<TreeNode>().ToArray();
                if (nodes.Length == 0) return;
                if (IsCompoundGeometrySubPartNode(e.Item as TreeNode)) return;
                StepTreeDragData stepData;
                if (TryGetStepTreeDragData(tree, nodes, out stepData))
                {
                    tree.DoDragDrop(stepData, DragDropEffects.Move);
                    return;
                }
                //
                OrderedDictionaryTreeContext context = null;
                bool isGroup = false;
                if (nodes.Length == 1 &&
                    _orderedDictionaryGroups.TryGetValue(nodes[0], out OrderedDictionaryGroupInfo groupInfo))
                {
                    context = groupInfo.Context;
                    isGroup = true;
                }
                else
                {
                    foreach (TreeNode node in nodes)
                    {
                        if (IsCompoundGeometrySubPartNode(node)) return;
                        //
                        OrderedDictionaryTreeContext nodeContext;
                        if (!TryGetOrderedDictionaryContext(node, out nodeContext) ||
                            !IsOrderedDictionaryItemNode(node, nodeContext))
                            return;
                        if (context == null) context = nodeContext;
                        else if (!ReferenceEquals(context, nodeContext)) return;
                    }
                }
                //
                if (context == null) return;
                OrderedDictionaryTreeDragData data = new OrderedDictionaryTreeDragData
                {
                    Tree = tree,
                    Nodes = nodes,
                    ItemNames = isGroup ? null : GetOrderedDictionaryItemNames(nodes, context),
                    Context = context,
                    IsGroup = isGroup
                };
                tree.DoDragDrop(data, DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                HideDropIndicators();
            }
        }
        private void cltv_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(typeof(OrderedDictionaryTreeDragData)) ||
                       e.Data.GetDataPresent(typeof(StepTreeDragData))
                ? DragDropEffects.Move
                : DragDropEffects.None;
        }
        private void cltv_DragOver(object sender, DragEventArgs e)
        {
            CodersLabTreeView tree = (CodersLabTreeView)sender;
            StepTreeDragData stepData =
                e.Data.GetData(typeof(StepTreeDragData)) as StepTreeDragData;
            if (stepData != null)
            {
                if (!ReferenceEquals(stepData.Tree, tree))
                {
                    e.Effect = DragDropEffects.None;
                    tree.HideDropIndicator();
                    return;
                }
                //
                Point stepPoint = tree.PointToClient(new Point(e.X, e.Y));
                if (TryGetStepDropTarget(tree, stepPoint, stepData, out TreeNode stepTargetNode,
                                         out TreeDropPosition stepPosition, out int stepTargetIndex))
                {
                    e.Effect = DragDropEffects.Move;
                    tree.ShowDropIndicator(stepTargetNode, stepPosition);
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                    tree.HideDropIndicator();
                }
                return;
            }
            OrderedDictionaryTreeDragData data =
                e.Data.GetData(typeof(OrderedDictionaryTreeDragData)) as OrderedDictionaryTreeDragData;
            //
            if (data == null || !ReferenceEquals(data.Tree, tree))
            {
                e.Effect = DragDropEffects.None;
                tree.HideDropIndicator();
                return;
            }
            //
            Point point = tree.PointToClient(new Point(e.X, e.Y));
            if (TryGetOrderedDictionaryDropTarget(tree, point, data, out TreeNode targetNode,
                                                   out TreeDropPosition position, out Guid? targetGroupId,
                                                   out int targetIndex))
            {
                e.Effect = DragDropEffects.Move;
                tree.ShowDropIndicator(targetNode, position);
            }
            else
            {
                e.Effect = DragDropEffects.None;
                tree.HideDropIndicator();
            }
        }
        private void cltv_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                CodersLabTreeView tree = (CodersLabTreeView)sender;
                StepTreeDragData stepData =
                    e.Data.GetData(typeof(StepTreeDragData)) as StepTreeDragData;
                if (stepData != null)
                {
                    if (!ReferenceEquals(stepData.Tree, tree)) return;
                    //
                    Point stepPoint = tree.PointToClient(new Point(e.X, e.Y));
                    if (TryGetStepDropTarget(tree, stepPoint, stepData, out _, out _, out int stepTargetIndex))
                    {
                        tree.DiscardDropIndicator();
                        MoveStepsEvent?.Invoke(stepData.StepNames, stepTargetIndex);
                    }
                    return;
                }
                OrderedDictionaryTreeDragData data =
                    e.Data.GetData(typeof(OrderedDictionaryTreeDragData)) as OrderedDictionaryTreeDragData;
                if (data == null || !ReferenceEquals(data.Tree, tree)) return;
                //
                Point point = tree.PointToClient(new Point(e.X, e.Y));
                if (!TryGetOrderedDictionaryDropTarget(tree, point, data, out _, out _,
                                                        out Guid? targetGroupId, out int targetIndex))
                    return;
                //
                tree.DiscardDropIndicator();
                //
                if (data.IsGroup)
                {
                    OrderedDictionaryGroupInfo groupInfo = _orderedDictionaryGroups[data.Nodes[0]];
                    InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(
                        OrderedDictionaryTreeLayoutChange.MoveGroup(
                            data.Context.View, data.Context.ItemType, data.Context.ParentName, groupInfo.Id,
                            targetGroupId, targetIndex),
                        data.Context, targetGroupId);
                }
                else
                {
                    InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(
                        OrderedDictionaryTreeLayoutChange.MoveItems(
                            data.Context.View, data.Context.ItemType, data.Context.ParentName, data.ItemNames,
                            targetGroupId, targetIndex),
                        data.Context, targetGroupId);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                HideDropIndicators();
            }
        }
        private void cltv_DragLeave(object sender, EventArgs e)
        {
            ((CodersLabTreeView)sender).HideDropIndicator();
        }
        private bool TryGetStepDropTarget(CodersLabTreeView tree, Point point, StepTreeDragData data,
                                          out TreeNode targetNode, out TreeDropPosition position,
                                          out int targetIndex)
        {
            targetNode = tree.GetNodeAt(point);
            position = TreeDropPosition.Into;
            targetIndex = -1;
            if (targetNode == null) return false;
            //
            if (ReferenceEquals(targetNode, _steps)) targetIndex = _steps.Nodes.Count;
            else if (IsStepTreeNode(targetNode))
            {
                position = tree.GetDropPosition(targetNode, point, false);
                targetIndex = targetNode.Index;
                if (position == TreeDropPosition.After) targetIndex++;
            }
            else return false;
            //
            int dropTargetIndex = targetIndex;
            targetIndex -= data.Nodes.Count(node => node.Index < dropTargetIndex);
            return targetIndex >= 0 && !IsStepMoveUnchanged(data, targetIndex);
        }
        private bool IsStepMoveUnchanged(StepTreeDragData data, int targetIndex)
        {
            List<TreeNode> originalNodes = _steps.Nodes.Cast<TreeNode>().ToList();
            HashSet<TreeNode> draggedNodes = new HashSet<TreeNode>(data.Nodes);
            List<TreeNode> movedNodes = originalNodes.Where(node => draggedNodes.Contains(node)).ToList();
            List<TreeNode> remainingNodes = originalNodes.Where(node => !draggedNodes.Contains(node)).ToList();
            remainingNodes.InsertRange(targetIndex, movedNodes);
            return originalNodes.SequenceEqual(remainingNodes);
        }
        private bool TryGetOrderedDictionaryDropTarget(CodersLabTreeView tree, Point point,
                                                        OrderedDictionaryTreeDragData data,
                                                        out TreeNode targetNode, out TreeDropPosition position,
                                                        out Guid? targetGroupId, out int targetIndex)
        {
            targetNode = tree.GetNodeAt(point);
            position = TreeDropPosition.Into;
            targetGroupId = null;
            targetIndex = -1;
            if (targetNode == null) return false;
            //
            OrderedDictionaryTreeContext targetContext;
            if (!TryGetOrderedDictionaryContext(targetNode, out targetContext) ||
                !ReferenceEquals(data.Context, targetContext))
                return false;
            // Compound geometry parts are fixed presentation subtrees. Their child rows
            // cannot be reordered and their parent row cannot accept additional parts.
            if (targetContext.View == ViewType.Geometry &&
                (targetNode.Tag is CompoundGeometryPart || IsCompoundGeometrySubPartNode(targetNode)))
                return false;
            //
            OrderedDictionaryItemLayout<string> layout = data.Context.Dictionary.GetItemLayoutSnapshot();
            if (ReferenceEquals(targetNode, data.Context.RootNode))
            {
                targetIndex = layout.Nodes.Count;
            }
            else if (_orderedDictionaryGroups.TryGetValue(targetNode, out OrderedDictionaryGroupInfo targetGroup))
            {
                // The middle band of a group row drops the dragged nodes inside the group,
                // the top and the bottom band place them next to it.
                position = tree.GetDropPosition(targetNode, point, true);
                if (position == TreeDropPosition.Into)
                {
                    targetGroupId = targetGroup.Id;
                    targetIndex = GetLayoutChildren(layout.Nodes, targetGroup.Id).Count;
                }
                else
                {
                    FindLayoutGroup(layout.Nodes, targetGroup.Id, null, out targetGroupId, out targetIndex);
                    if (position == TreeDropPosition.After) targetIndex++;
                }
            }
            else if (IsOrderedDictionaryItemNode(targetNode, targetContext))
            {
                position = tree.GetDropPosition(targetNode, point, false);
                string key = ((NamedClass)targetNode.Tag).Name;
                FindLayoutItem(layout.Nodes, key, targetContext.Dictionary.Comparer,
                               out targetGroupId, out targetIndex);
                if (position == TreeDropPosition.After) targetIndex++;
            }
            else return false;
            //
            if (data.IsGroup)
            {
                Guid groupId = _orderedDictionaryGroups[data.Nodes[0]].Id;
                if (targetGroupId == groupId || IsLayoutGroupDescendant(layout.Nodes, groupId, targetGroupId))
                    return false;
            }
            //
            targetIndex = AdjustDropTargetIndex(layout, data, targetGroupId, targetIndex);
            if (targetIndex < 0) return false;
            return !IsOrderedDictionaryMoveUnchanged(layout, data, targetGroupId, targetIndex);
        }
        private bool IsOrderedDictionaryMoveUnchanged(OrderedDictionaryItemLayout<string> layout,
                                                      OrderedDictionaryTreeDragData data, Guid? targetGroupId,
                                                      int targetIndex)
        {
            IList<OrderedDictionaryItemNode<string>> targetChildren = targetGroupId.HasValue
                ? GetLayoutChildren(layout.Nodes, targetGroupId.Value) : layout.Nodes;
            if (targetChildren == null) return false;
            //
            List<OrderedDictionaryItemNode<string>> movedNodes = new List<OrderedDictionaryItemNode<string>>();
            if (data.IsGroup)
            {
                Guid groupId = _orderedDictionaryGroups[data.Nodes[0]].Id;
                movedNodes.AddRange(targetChildren.Where(node => node.IsGroup && node.Id == groupId));
                if (movedNodes.Count != 1) return false;
            }
            else
            {
                HashSet<string> itemNames = new HashSet<string>(data.ItemNames, data.Context.Dictionary.Comparer);
                movedNodes.AddRange(targetChildren.Where(node => node.IsItem && itemNames.Contains(node.Key)));
                if (movedNodes.Count != itemNames.Count) return false;
            }
            //
            List<OrderedDictionaryItemNode<string>> remainingNodes =
                targetChildren.Where(node => !movedNodes.Contains(node)).ToList();
            if (targetIndex > remainingNodes.Count) return false;
            remainingNodes.InsertRange(targetIndex, movedNodes);
            return targetChildren.SequenceEqual(remainingNodes);
        }
        private static void FindLayoutGroup(IEnumerable<OrderedDictionaryItemNode<string>> nodes, Guid groupId,
                                            Guid? currentParentId, out Guid? parentGroupId, out int index)
        {
            if (TryFindLayoutGroup(nodes, groupId, currentParentId, out parentGroupId, out index)) return;
            throw new KeyNotFoundException("The group was not found in the ordered dictionary layout.");
        }
        private static bool TryFindLayoutGroup(IEnumerable<OrderedDictionaryItemNode<string>> nodes, Guid groupId,
                                               Guid? currentParentId, out Guid? parentGroupId, out int index)
        {
            int currentIndex = 0;
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (node.IsGroup && node.Id == groupId)
                {
                    parentGroupId = currentParentId;
                    index = currentIndex;
                    return true;
                }
                if (node.IsGroup &&
                    TryFindLayoutGroup(node.Children, groupId, node.Id, out parentGroupId, out index))
                    return true;
                currentIndex++;
            }
            //
            parentGroupId = null;
            index = -1;
            return false;
        }
        private static bool IsLayoutGroupDescendant(IEnumerable<OrderedDictionaryItemNode<string>> nodes,
                                                     Guid groupId, Guid? possibleDescendantId)
        {
            if (!possibleDescendantId.HasValue) return false;
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup) continue;
                if (node.Id == groupId) return ContainsLayoutGroup(node.Children, possibleDescendantId.Value);
                if (IsLayoutGroupDescendant(node.Children, groupId, possibleDescendantId)) return true;
            }
            return false;
        }
        private static bool ContainsLayoutGroup(IEnumerable<OrderedDictionaryItemNode<string>> nodes, Guid groupId)
        {
            foreach (OrderedDictionaryItemNode<string> node in nodes)
            {
                if (!node.IsGroup) continue;
                if (node.Id == groupId || ContainsLayoutGroup(node.Children, groupId)) return true;
            }
            return false;
        }
        private int AdjustDropTargetIndex(OrderedDictionaryItemLayout<string> layout,
                                          OrderedDictionaryTreeDragData data, Guid? targetGroupId,
                                          int targetIndex)
        {
            int beforeTarget = 0;
            if (data.IsGroup)
            {
                OrderedDictionaryGroupInfo groupInfo = _orderedDictionaryGroups[data.Nodes[0]];
                FindLayoutGroup(layout.Nodes, groupInfo.Id, null, out Guid? parentId, out int index);
                if (parentId == targetGroupId && index < targetIndex) beforeTarget++;
            }
            else
            {
                foreach (string key in data.ItemNames)
                {
                    FindLayoutItem(layout.Nodes, key, data.Context.Dictionary.Comparer,
                                   out Guid? parentId, out int index);
                    if (parentId == targetGroupId && index < targetIndex) beforeTarget++;
                }
            }
            return targetIndex - beforeTarget;
        }
        private void HideDropIndicators()
        {
            cltvGeometry.HideDropIndicator();
            cltvModel.HideDropIndicator();
            cltvResults.HideDropIndicator();
        }

        private void PrepareToolStripItem(CodersLabTreeView tree)
        {
            TreeNode[] actionNodes = GetSelectedNodesIncludingGroupItems(tree);
            int n = actionNodes.Length;
            bool hasActionNodes = n > 0;
            //
            ContextMenuFields menuFields = new ContextMenuFields();
            menuFields.Types = new HashSet<Type>();
            foreach (TreeNode node in actionNodes)
            {
                AppendMenuFields(node, ref menuFields);
            }
            bool oneAboveVisible = false;
            // Visibility                                                                                           
            bool visible;
            // Create
            visible = hasActionNodes && menuFields.Create == n;
            tsmiCreate.Visible = visible;
            oneAboveVisible |= visible;
            // Edit
            visible = hasActionNodes && menuFields.Edit == n;
            tsmiEdit.Visible = visible;
            oneAboveVisible |= visible;
            // Edit step controls
            visible = hasActionNodes && menuFields.EditStepControls == n;
            tsmiEditStepControls.Visible = visible;
            oneAboveVisible |= visible;
            // Query
            visible = menuFields.Query > 0;
            tsmiQuery.Visible = visible;
            oneAboveVisible |= visible;
            // Duplicate
            visible = hasActionNodes && menuFields.Duplicate == n;
            tsmiDuplicate.Visible = visible;
            oneAboveVisible |= visible;
            // Propagate
            visible = hasActionNodes && menuFields.Propagate == n;
            tsmiPropagate.Visible = visible;
            oneAboveVisible |= visible;
            // Preview
            visible = hasActionNodes && menuFields.Preview == n;
            tsmiPreview.Visible = visible;
            oneAboveVisible |= visible;
            // Add to plot
            visible = hasActionNodes && menuFields.AddToPlot == n;
            tsmiSpaceAddToPlot.Visible = visible && oneAboveVisible;
            tsmiAddToPlot.Visible = visible;
            tsmiAddToNewPlot.Visible = visible;
            oneAboveVisible |= visible;
            // Geometry                                             
            visible = menuFields.CompoundPart == n && n > 1;
            tsmiSpaceCompoundPart.Visible = visible && oneAboveVisible;
            tsmiCompoundPart.Visible = visible;
            visible = menuFields.SwapParts == n && n == 2;
            tsmiSwapPartGeometries.Visible = visible;
            visible = TryGetSplitCellsToGroup(actionNodes, out _, out _);
            tsmiGroupSplitCells.Visible = visible;
            oneAboveVisible |= visible;
            // Mesh                                                 
            visible = hasActionNodes && menuFields.MeshingParameters == n;
            bool createMeshVisible = hasActionNodes && menuFields.CreateMesh == n;
            tsmiSpaceMesh.Visible = (visible || createMeshVisible) && oneAboveVisible;
            tsmiPreviewEdgeMesh.Visible = visible;
            tsmiCreateMesh.Visible = createMeshVisible;
            // Copy part                                            
            tsmiSpaceCopyPart.Visible = visible;
            tsmiCopyGeometryToResults.Visible = visible;
            oneAboveVisible |= visible || createMeshVisible;
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
            visible = hasActionNodes && menuFields.ConvertToPart == n;
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
            visible = hasActionNodes && menuFields.Hide + menuFields.Show == n;
            tsmiSpaceHideShow.Visible = visible && oneAboveVisible;
            tsmiHide.Visible = visible;
            tsmiShow.Visible = visible;
            tsmiShowOnly.Visible = visible;
            tsmiActivate.ShowShortcutKeys = !visible;
            tsmiDeactivate.ShowShortcutKeys = !visible;
            oneAboveVisible |= visible;
            // Color                                                
            visible = hasActionNodes && menuFields.ColorTransparency == n;
            tsmiSpaceColor.Visible = visible && oneAboveVisible;
            tsmiSetColor.Visible = visible;
            tsmiResetColor.Visible = visible;
            tsmiSetTransparency.Visible = visible; // transparency
            oneAboveVisible |= visible;
            // Result representation
            visible = menuFields.ResultRepresentationOn + menuFields.ResultRepresentationOff == n &&
                      n > 0;
            tsmiSpaceColorContours.Visible = visible && oneAboveVisible;
            tsmiColorContoursOff.Visible = visible;
            tsmiColorContoursOn.Visible = visible;
            oneAboveVisible |= visible;
            // Analysis                                             
            visible = hasActionNodes && menuFields.Run == n;
            tsmiSpaceAnalysis.Visible = visible && oneAboveVisible;
            tsmiRun.Visible = visible;
            tsmiCheckModel.Visible = visible;
            tsmiMonitor.Visible = visible;
            tsmiResults.Visible = visible;
            tsmiKill.Visible = visible;
            oneAboveVisible |= visible;
            // Activate/Deactivate                                  
            visible = hasActionNodes && menuFields.Activate + menuFields.Deactivate == n;
            tsmiSpaceActive.Visible = visible && oneAboveVisible;
            tsmiActivate.Visible = visible;
            tsmiDeactivate.Visible = visible;
            oneAboveVisible |= visible;
            // Ordered dictionary organization
            bool createGroupVisible = _contextMenuNodes == null && CanCreateOrderedDictionaryGroup(tree);
            tsmiSpaceOrganize.Visible = createGroupVisible && oneAboveVisible;
            tsmiCreateGroup.Visible = createGroupVisible;
            oneAboveVisible |= createGroupVisible;
            //Expand / Collapse                                     
            tsmiSpaceExpandColapse.Visible = oneAboveVisible;
            tsmiExpandAll.Visible = true;
            tsmiCollapseAll.Visible = true;
            oneAboveVisible = true;
            // Delete                                               
            TreeNode[] selectedNodes = tree.SelectedNodes.Cast<TreeNode>().ToArray();
            TreeNode[] selectedGroupNodes = selectedNodes
                                                     .Where(node => _orderedDictionaryGroups.ContainsKey(node))
                                                     .ToArray();
            bool compatibleGroupSelection = selectedGroupNodes.Length > 0 &&
                selectedGroupNodes.Select(node => _orderedDictionaryGroups[node].Context).Distinct().Count() == 1;
            if (selectedGroupNodes.Length == selectedNodes.Length) visible = compatibleGroupSelection;
            else if (selectedGroupNodes.Length > 0)
                visible = compatibleGroupSelection && hasActionNodes && menuFields.Delete == n;
            else visible = hasActionNodes && menuFields.Delete == n;
            tsmiSpaceDelete.Visible = visible && oneAboveVisible;
            tsmiDelete.Visible = visible;
            oneAboveVisible |= visible;
            // Enabled                                                                                              
            bool enabled;
            // Create
            enabled = menuFields.Create == 1;
            tsmiCreate.Enabled = enabled;
            // Edit
            if (menuFields.Types.Count == 1 && menuFields.Types.Contains(typeof(GeometryPart))) enabled = true;
            else if (menuFields.Types.Count == 2 && menuFields.Types.Contains(typeof(GeometryPart)) &&
                menuFields.Types.Contains(typeof(CompoundGeometryPart))) enabled = true;
            else enabled = menuFields.Edit == 1;
            tsmiEdit.Enabled = enabled;
            // Add to plot
            tsmiAddToPlot.Enabled = true;
            tsmiAddToNewPlot.Enabled = CanAddItemsToNewPlot == null || CanAddItemsToNewPlot();
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
            // Result representation
            tsmiColorContoursOff.Enabled = menuFields.ResultRepresentationOn > 0;
            tsmiColorContoursOn.Enabled = menuFields.ResultRepresentationOff > 0;
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
            bool subPart = IsCompoundGeometrySubPartNode(node);
            //
            if (item != null) menuFields.Types.Add(item.GetType());
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
            // Add to plot
            if (item != null && (item is HistoryResultData || item is Amplitude)) menuFields.AddToPlot++;
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
            // Mesh setup item - Mesh the parts it is defined on
            if (item != null && item is MeshSetupItem && GetActiveTree() == cltvGeometry) menuFields.CreateMesh++;
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
            // Color & Transparency
            if (item != null && item is BasePart) menuFields.ColorTransparency++;
            // Result representation
            if (item is ResultPart resultPart && ResultPart.ResultRepresentationAvailable)
            {
                if (resultPart.ResultRepresentation) menuFields.ResultRepresentationOn++;
                else menuFields.ResultRepresentationOff++;
            }
            // Analysis
            if (item is AnalysisJob &&
                TryGetOrderedDictionaryContext(node, out OrderedDictionaryTreeContext analysisContext) &&
                analysisContext.ItemType == typeof(AnalysisJob))
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
            // Delete - a sub part of a compound may be deleted: the compound loses that solid of its
            // geometry and keeps the rest of them as they are
            if (item != null)
            {
                if (node.TreeView == cltvResults &&
                    (item is FeNodeSet ||
                     item is FeElementSet ||
                     item is FeSurface)) { }
                else menuFields.Delete++;
            }
        }
        //
        // The click of a menu item is raised after the menu has closed, so the nodes the menu acts on are
        // released only after the message that carries the click has been processed
        private void cmsTree_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (_contextMenuNodes != null) BeginInvoke((MethodInvoker)(() => { _contextMenuNodes = null; }));
        }
        private void cltv_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                CodersLabTreeView tree = (CodersLabTreeView)sender;
                //
                _doubleClick = ModifierKeys != Keys.Shift && ModifierKeys != Keys.Control &&
                               e.Button == MouseButtons.Left && e.Clicks > 1;
                //
                TreeViewHitTestInfo hit = tree.HitTest(e.Location);
                if (hit.Location != TreeViewHitTestLocations.Label)
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
                    TreeViewHitTestInfo hit = tree.HitTest(e.Location);
                    if (hit.Location == TreeViewHitTestLocations.Label)
                    {
                        //if (tree.SelectedNodes.Count == 0 && hit.Node != null) tree.SelectedNodes.Add(hit.Node);
                        _contextMenuNodes = null;
                        PrepareToolStripItem(tree);
                        cmsTree.Show(tree, e.Location);
                    }
                }
                else if (e.Button == MouseButtons.Left && ModifierKeys == Keys.None)
                {
                    TreeViewHitTestInfo hit = tree.HitTest(e.Location);
                    if (hit.Location == TreeViewHitTestLocations.Image && hit.Node != null &&
                        hit.Node.Tag is NamedClass item)
                    {
                        string[] stepNames = new string[] { GetStepNameFromNode(hit.Node) };
                        if (!item.Active && CanDeactivate(hit.Node))
                        {
                            RenderingOff?.Invoke();
                            ActivateDeactivateEvent?.Invoke(new NamedClass[] { item }, true, stepNames);
                            RenderingOn?.Invoke();
                        }
                        else if (item.Active && !item.Visible && CanHide(item))
                        {
                            RenderingOff?.Invoke();
                            HideShowEvent?.Invoke(new NamedClass[] { item }, HideShowOperation.Show, stepNames);
                            RenderingOn?.Invoke();
                        }
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
                    node = tree.SelectedNode;
                    // Results
                    if (node.Tag is FieldData)
                    {
                        SelectEvent?.Invoke(null, false);   // clear selection
                        if (node.Parent != null)            // deleting tree nodes can delete the parent
                        {
                            FieldDataSelectEvent?.Invoke(new string[] { node.Parent.Name, node.Name });
                        }
                        ActiveControl = cltvResults;        // this is for the arrow keys to work on the results tree
                        return;
                    }
                }
                // Select
                TreeNode lastNode = null;
                foreach (TreeNode selectedNode in tree.SelectedNodes)
                {
                    if (_splitSubPartNodes.ContainsKey(selectedNode))
                        items.AddRange(GetSplitSubPartCellItems(selectedNode));
                    else if (selectedNode.Tag == null) continue;
                    else items.Add((NamedClass)selectedNode.Tag);
                    //
                    lastNode = selectedNode;
                }
                if (lastNode != null) lastNode.EnsureVisible();
                //
                SelectEvent?.Invoke(items.ToArray(), false);
            }
            else if (tree.SelectedNodes.Count == 0) ClearSelectionEvent();
            //
            UpdateRelatedNodesHighlight();
            //
            return;
        }
        private void cltv_MouseOverNodeChangedEvent(object sender)
        {
            if (_disableSelectionsChanged) return;
            //
            //timerMouseMove.Stop();
            timerMouseMove.Start();
        }
        private void cltv_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            CodersLabTreeView tree = GetActiveTree();
            if (tree.SelectedNodes.Count != 1) return;
            //
            TreeViewHitTestInfo hit = tree.HitTest(e.Location);
            if (e.Button == MouseButtons.Left && hit.Location == TreeViewHitTestLocations.Label)
            {
                TreeNode selectedNode = tree.SelectedNode;
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
            //Debug.WriteLine(e.KeyData);
            CodersLabTreeView tree = sender as CodersLabTreeView ?? GetActiveTree();
            // Let the native tree label editor consume Enter and Escape. Processing the
            // model-tree shortcuts here can act on the rebuilt tree after a rename.
            if (tree.EditModeActive) return;
            //if (tree.Focused) // Disable since called from FrmMain.KeyboardHook_KeyDown
            {
                if (e.KeyCode == Keys.Delete)
                {
                    tsmiDelete_Click(null, null);
                }
                else if (e.KeyCode == Keys.Space)
                {
                    InvertHideShowOrActivateDeactivate();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    if (tree.SelectedNodes.Count == 1 && tree.SelectedNode.Tag == null) tsmiCreate_Click(null, null);
                    else tsmiEdit_Click(null, null);
                }
                // No beep
                e.SuppressKeyPress = true;
            }
        }
        private void cltv_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            if (e.Label == null) return;
            CodersLabTreeView tree = GetActiveTree();
            //
            if (tree.SelectedNodes.Count == 1)
            {
                TreeNode selectedNode = tree.SelectedNode;
                string newName = e.Label.Trim();
                if (_orderedDictionaryGroups.TryGetValue(selectedNode, out OrderedDictionaryGroupInfo groupInfo))
                {
                    Guid? parentGroupId = null;
                    if (selectedNode.Parent != null &&
                        _orderedDictionaryGroups.TryGetValue(selectedNode.Parent,
                                                             out OrderedDictionaryGroupInfo parentGroupInfo))
                        parentGroupId = parentGroupInfo.Id;
                    //
                    OrderedDictionaryTreeLayoutChange change =
                        OrderedDictionaryTreeLayoutChange.RenameGroup(
                            groupInfo.Context.View, groupInfo.Context.ItemType, groupInfo.Context.ParentName,
                            groupInfo.Id, newName);
                    OrderedDictionaryTreeContext previousContext = groupInfo.Context;
                    //
                    // Rebuilding the tree before the native label-edit message completes can
                    // transfer its final Enter processing to a node in the rebuilt tree.
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        try
                        {
                            InvokeOrderedDictionaryTreeLayoutChangeAndExpandParent(
                                change, previousContext, parentGroupId);
                        }
                        catch (Exception ex)
                        {
                            ExceptionTools.Show(this, ex);
                        }
                    }));
                }
                else if (_splitSubPartNodes.TryGetValue(selectedNode, out string compoundPartName))
                {
                    string oldSubPartName = selectedNode.Name;
                    // Renaming the sub part renames its cells and rebuilds the tree, which has to wait until
                    // the native label-edit message has completed
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        try
                        {
                            RenameSplitSubPartEvent?.Invoke(compoundPartName, oldSubPartName, newName);
                        }
                        catch (Exception ex)
                        {
                            ExceptionTools.Show(this, ex);
                        }
                    }));
                }
                else if (e.Node.Tag is NamedClass item)
                {
                    string stepName = GetStepNameFromNode(selectedNode);
                    if (item is CompoundGeometryPart)
                    {
                        // The compound rename can also rebuild the model tree after renaming its paired
                        // finite element group. Wait until the native label-edit message has completed.
                        BeginInvoke((MethodInvoker)(() =>
                        {
                            try
                            {
                                RenameEvent?.Invoke(item, newName, stepName);
                            }
                            catch (Exception ex)
                            {
                                ExceptionTools.Show(this, ex);
                            }
                        }));
                    }
                    else RenameEvent?.Invoke(item, newName, stepName);
                }
            }
        }
        private void timerMouseMove_Tick(object sender, EventArgs e)
        {
            CodersLabTreeView tree = GetActiveTree();
            List<NamedClass> items = new List<NamedClass>();
            // Select
            foreach (TreeNode selectedNode in tree.SelectedNodes)
            {
                if (_splitSubPartNodes.ContainsKey(selectedNode))
                    items.AddRange(GetSplitSubPartCellItems(selectedNode));
                else if (selectedNode.Tag != null) items.Add((NamedClass)selectedNode.Tag);
            }
            // Add mouse over node
            TreeNode node = tree.MouseOverNode;
            //
            if (node != null && _splitSubPartNodes.ContainsKey(node)) items.AddRange(GetSplitSubPartCellItems(node));
            else if (node != null && node.Tag != null)
            {
                items.Add((NamedClass)node.Tag);
            }
            //
            //if (items.Count > 0)
                SelectEvent?.Invoke(items.ToArray(), true);
            //
            UpdateRelatedNodesHighlight();
            //
            timerMouseMove.Stop();
        }
        // Highlights the geometry items related to the currently focused items (selected + mouse-over)
        // with the related (light green) highlight. Only the geometry tree shows part <-> mesh-setup-item
        // relations; on the other trees any related highlight is cleared.
        private void UpdateRelatedNodesHighlight()
        {
            CodersLabTreeView tree = GetActiveTree();
            if (tree != cltvGeometry || GetRelatedGeometryItemNames == null)
            {
                cltvGeometry.SetRelatedNodes(null);
                return;
            }
            // Gather the focused items. When nodes are selected, the related highlight tracks the
            // selection only; the node under the mouse contributes only when nothing is selected (otherwise
            // an item could be highlighted both from the selection and from the hover).
            List<NamedClass> focusItems = new List<NamedClass>();
            foreach (TreeNode selectedNode in tree.SelectedNodes)
            {
                if (_splitSubPartNodes.ContainsKey(selectedNode))
                    focusItems.AddRange(GetSplitSubPartCellItems(selectedNode));
                else if (selectedNode.Tag is NamedClass selectedItem) focusItems.Add(selectedItem);
            }
            if (focusItems.Count == 0)
            {
                TreeNode overNode = tree.MouseOverNode;
                if (overNode != null && _splitSubPartNodes.ContainsKey(overNode))
                    focusItems.AddRange(GetSplitSubPartCellItems(overNode));
                else if (overNode != null && overNode.Tag is NamedClass overItem) focusItems.Add(overItem);
            }
            //
            if (focusItems.Count == 0)
            {
                tree.SetRelatedNodes(null);
                return;
            }
            //
            string[] relatedNames = GetRelatedGeometryItemNames(focusItems.ToArray());
            if (relatedNames == null || relatedNames.Length == 0)
            {
                tree.SetRelatedNodes(null);
                return;
            }
            //
            List<TreeNode> relatedNodes = new List<TreeNode>();
            foreach (string name in relatedNames)
            {
                TreeNode[] found = cltvGeometry.Nodes.Find(name, true);
                foreach (TreeNode node in found)
                {
                    if (!(node.Tag is NamedClass)) continue;
                    //
                    relatedNodes.Add(node);
                    if (node.Parent != null && _splitSubPartNodes.ContainsKey(node.Parent))
                        relatedNodes.Add(node.Parent);
                }
            }
            tree.SetRelatedNodes(relatedNodes);
        }
        private string GetStepNameFromNode(TreeNode node)
        {
            while (node != null)
            {
                if (node.Tag is Step step) return step.Name;
                node = node.Parent;
            }
            return null;
        }
        #endregion

        #region Tree context menu
        private void tsmiCreate_Click(object sender, EventArgs e)
        {
            try
            {
                CodersLabTreeView tree = GetActiveTree();
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;
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
                // Edit Part Color
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;
                //
                if (selectedNode.Tag == null) return;
                //
                string stepName = GetStepNameFromNode(selectedNode);
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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;
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
                if (GetSelectedItemNamesIncludingGroupItems(GetActiveTree()).Length > 0)
                    QueryEvent?.Invoke();
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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = GetStepNameFromNode(selectedNode);
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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = GetStepNameFromNode(selectedNode);
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
        private void tsmiAddToPlot_Click(object sender, EventArgs e)
        {
            try
            {
                NamedClass[] items = GetSelectedPlotItems();
                if (items.Length > 0) AddItemsToPlotEvent?.Invoke(items);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiAddToNewPlot_Click(object sender, EventArgs e)
        {
            try
            {
                NamedClass[] items = GetSelectedPlotItems();
                if (items.Length > 0) AddItemsToNewPlotEvent?.Invoke(items);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private NamedClass[] GetSelectedPlotItems()
        {
            TreeNode[] nodes = GetSelectedNodesIncludingGroupItems(GetActiveTree());
            List<NamedClass> items = new List<NamedClass>();
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is HistoryResultData || node.Tag is Amplitude) items.Add((NamedClass)node.Tag);
                else return new NamedClass[0];
            }
            return items.ToArray();
        }
        private void tsmiPreview_Click(object sender, EventArgs e)
        {
            try
            {
                string stepName;
                List<NamedClass> items = new List<NamedClass>();
                List<string> stepNames = new List<string>();
                //
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    stepName = GetStepNameFromNode(selectedNode);
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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    if (CanHide(selectedNode.Tag))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = GetStepNameFromNode(selectedNode);
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
        private void InvertHideShowOrActivateDeactivate()
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();
                string stepName;
                List<string> stepNames = new List<string>();
                List<NamedClass> activeItems = new List<NamedClass>();
                List<string> activeStepNames = new List<string>();

                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;

                    if (CanHide(selectedNode.Tag))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = GetStepNameFromNode(selectedNode);
                        stepNames.Add(stepName);
                    }
                    else if (CanDeactivate(selectedNode))
                    {
                        activeItems.Add((NamedClass)selectedNode.Tag);
                        stepName = GetStepNameFromNode(selectedNode);
                        activeStepNames.Add(stepName);
                    }
                }

                if (items.Count > 0 || activeItems.Count > 0)
                {
                    RenderingOff?.Invoke();
                    List<NamedClass> toShow = new List<NamedClass>();
                    List<NamedClass> toHide = new List<NamedClass>();
                    List<string> toShowStepNames = new List<string>();
                    List<string> toHideStepNames = new List<string>();

                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i].Visible)
                        {
                            toHide.Add(items[i]);
                            toHideStepNames.Add(stepNames[i]);
                        }
                        else
                        {
                            toShow.Add(items[i]);
                            toShowStepNames.Add(stepNames[i]);
                        }
                    }

                    if (toShow.Count > 0)
                        HideShowEvent?.Invoke(toShow.ToArray(), HideShowOperation.Show, toShowStepNames.ToArray());
                    if (toHide.Count > 0)
                        HideShowEvent?.Invoke(toHide.ToArray(), HideShowOperation.Hide, toHideStepNames.ToArray());

                    List<NamedClass> toActivate = new List<NamedClass>();
                    List<NamedClass> toDeactivate = new List<NamedClass>();
                    List<string> toActivateStepNames = new List<string>();
                    List<string> toDeactivateStepNames = new List<string>();

                    for (int i = 0; i < activeItems.Count; i++)
                    {
                        if (activeItems[i].Active)
                        {
                            toDeactivate.Add(activeItems[i]);
                            toDeactivateStepNames.Add(activeStepNames[i]);
                        }
                        else
                        {
                            toActivate.Add(activeItems[i]);
                            toActivateStepNames.Add(activeStepNames[i]);
                        }
                    }

                    if (toActivate.Count > 0)
                        ActivateDeactivateEvent?.Invoke(toActivate.ToArray(), true, toActivateStepNames.ToArray());
                    if (toDeactivate.Count > 0)
                        ActivateDeactivateEvent?.Invoke(toDeactivate.ToArray(), false, toDeactivateStepNames.ToArray());
                    RenderingOn?.Invoke();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColor_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> parts = new List<string>();
                //
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    if (selectedNode.Tag is BasePart part) parts.Add(part.Name);
                }
                //
                if (parts.Count > 0) SetPartColorEvent?.Invoke(parts.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColor_Click(object sender, EventArgs e)
        {
            try
            {
                List<string> parts = new List<string>();
                //
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    if (selectedNode.Tag is BasePart part) parts.Add(part.Name);
                }
                //
                if (parts.Count > 0) ResetPartColorEvent?.Invoke(parts.ToArray());
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
                //
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;
                    if (selectedNode.Tag is BasePart part) parts.Add(part.Name);
                }
                //
                if (parts.Count > 0) SetTransparencyEvent?.Invoke(parts.ToArray());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResultRepresentationOnOff_Click(object sender, EventArgs e)
        {
            try
            {
                List<NamedClass> items = new List<NamedClass>();

                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
                {
                    if (selectedNode.Tag == null) continue;

                    if (selectedNode.Tag is ResultPart)
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                    }
                }

                if (items.Count > 0)
                {
                    bool resultRepresentation = (sender == tsmiColorContoursOn);
                    ResultRepresentationVisibilityEvent?.Invoke(items.ToArray(), resultRepresentation);
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
                string[] names = GetSelectedItemNamesIncludingGroupItems(cltvGeometry);
                if (names.Length > 1) CreateCompoundPart?.Invoke(names);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiGroupSplitCells_Click(object sender, EventArgs e)
        {
            try
            {
                string compoundPartName;
                string[] cellNames;
                if (TryGetSplitCellsToGroup(GetSelectedNodesIncludingGroupItems(cltvGeometry), out compoundPartName,
                                            out cellNames))
                    GroupSplitCellsEvent?.Invoke(compoundPartName, cellNames);
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
                string[] names = GetSelectedItemNamesIncludingGroupItems(cltvGeometry);
                if (names.Length == 2) SwapPartGeometries?.Invoke(names);
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
                string[] names = GetSelectedItemNamesIncludingGroupItems(cltvGeometry);
                if (names.Length > 0) await PreviewEdgeMesh?.Invoke(names, null);
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
                string[] names = GetPartNamesToMesh();
                if (names.Length > 0) CreateMeshEvent?.Invoke(names);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // The selected parts are meshed as they are; a selected mesh setup item is meshed through the parts
        // it is defined on
        private string[] GetPartNamesToMesh()
        {
            List<string> partNames = new List<string>();
            List<string> meshSetupItemNames = new List<string>();
            //
            foreach (TreeNode node in GetSelectedNodesIncludingGroupItems(cltvGeometry))
            {
                if (node.Tag is MeshSetupItem meshSetupItem) meshSetupItemNames.Add(meshSetupItem.Name);
                else if (node.Tag is NamedClass item) partNames.Add(item.Name);
            }
            //
            if (meshSetupItemNames.Count > 0 && GetPartNamesToMeshFromMeshSetupItems != null)
            {
                string[] meshSetupItemPartNames = GetPartNamesToMeshFromMeshSetupItems(meshSetupItemNames.ToArray());
                if (meshSetupItemPartNames != null)
                {
                    foreach (string partName in meshSetupItemPartNames)
                        if (!partNames.Contains(partName)) partNames.Add(partName);
                }
            }
            //
            return partNames.ToArray();
        }
        // Returns the names of the selected mesh setup items, but only when every selected item is one
        public string[] GetSelectedMeshSetupItemNames()
        {
            if (GetActiveTree() != cltvGeometry) return null;
            //
            TreeNode[] nodes = GetSelectedNodesIncludingGroupItems(cltvGeometry);
            if (nodes.Length == 0) return null;
            //
            List<string> meshSetupItemNames = new List<string>();
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is MeshSetupItem meshSetupItem) meshSetupItemNames.Add(meshSetupItem.Name);
                else return null;
            }
            //
            return meshSetupItemNames.ToArray();
        }
        // Geometry - copy
        private void tsmiCopyGeometryPartToResults_Click(object sender, EventArgs e)
        {
            try
            {
                string[] names = GetSelectedItemNamesIncludingGroupItems(cltvGeometry);
                if (names.Length > 0) CopyGeometryToResultsEvent?.Invoke(names);
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
                TreeNode selectedNode = tree.SelectedNode;
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
                CodersLabTreeView tree = GetActiveTree();
                string[] names = GetSelectedItemNamesIncludingGroupItems(tree);
                if (names.Length > 0)
                {
                    if (tree == cltvModel) MergeParts?.Invoke(names);
                    else if (tree == cltvResults) MergeResultParts?.Invoke(names);
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
                string[] names = GetSelectedItemNamesIncludingGroupItems(cltvModel);
                if (names.Length > 0) ConvertElementSetsToMeshParts?.Invoke(names);
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
                TreeNode selectedNode = tree.SelectedNode;
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
                TreeNode selectedNode = tree.SelectedNode;
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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(GetActiveTree()))
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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;
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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;
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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;

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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;

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
                TreeNode selectedNode = GetSingleSelectedNodeIncludingGroupItems(tree);
                if (selectedNode == null) return;

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
                foreach (TreeNode selectedNode in GetSelectedNodesIncludingGroupItems(tree))
                {
                    if (selectedNode.Tag == null) continue;
                    //
                    if (CanDeactivate(selectedNode))
                    {
                        items.Add((NamedClass)selectedNode.Tag);
                        stepName = GetStepNameFromNode(selectedNode);
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
                CodersLabTreeView tree = GetActiveTree();
                TreeNode[] selectedNodes = tree.SelectedNodes.Cast<TreeNode>().ToArray();
                bool groupSelected = selectedNodes.Any(node => _orderedDictionaryGroups.ContainsKey(node));
                if (groupSelected && selectedNodes.All(node => _orderedDictionaryGroups.ContainsKey(node)))
                {
                    RemoveSelectedGroups();
                    return;
                }
                // The selected groups are removed after the selected items are deleted, since deleting an item
                // rebuilds the tree and invalidates the tree nodes. The group ids outlive the rebuild.
                OrderedDictionaryTreeLayoutChange groupRemoval = null;
                Guid? groupRemovalParentGroupId = null;
                if (groupSelected)
                    TryGetSelectedGroupRemoval(selectedNodes, out groupRemoval, out groupRemovalParentGroupId);
                //
                List<NamedClass> items = new List<NamedClass>();
                string parentName;
                List<string> parentNames = new List<string>();
                //
                HashSet<NamedClass> addedItems = new HashSet<NamedClass>();
                foreach (TreeNode selectedNode in selectedNodes)
                {
                    if (_splitSubPartNodes.ContainsKey(selectedNode))
                    {
                        foreach (NamedClass cell in GetSplitSubPartCellItems(selectedNode))
                        {
                            if (!addedItems.Add(cell)) continue;
                            items.Add(cell);
                            parentNames.Add(null);
                        }
                        continue;
                    }
                    if (selectedNode.Tag == null) continue;
                    if (!addedItems.Add((NamedClass)selectedNode.Tag)) continue;
                    //
                    items.Add((NamedClass)selectedNode.Tag);
                    parentName = GetStepNameFromNode(selectedNode);
                    if (parentName == null && selectedNode.Parent != null && selectedNode.Parent.Parent != null)
                    {
                        if (selectedNode.Parent.Tag is Field) parentName = selectedNode.Parent.Text;
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
                //
                if (groupRemoval != null) InvokeOrderedDictionaryGroupRemoval(groupRemoval, groupRemovalParentGroupId);
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
            HideDropIndicators();
            _orderedDictionaryContexts.Clear();
            _orderedDictionaryGroups.Clear();
            _splitSubPartNodes.Clear();
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
            _distributions.Nodes.Clear();
            _amplitudes.Nodes.Clear();
            _initialConditions.Nodes.Clear();
            _steps.Nodes.Clear();
            _analyses.Nodes.Clear();
            //
            SetNumberOfUserKeywords(0);
            SetNumberOfModifiedKeywords(0);
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
            _distributions.Text = _distributionsName;
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
            _model.Nodes.Add(_distributions);
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
            foreach (TreeNode node in _orderedDictionaryContexts
                         .Where(entry => entry.Value.View == ViewType.Results)
                         .Select(entry => entry.Key).ToArray())
                _orderedDictionaryContexts.Remove(node);
            foreach (TreeNode node in _orderedDictionaryGroups
                         .Where(entry => entry.Value.Context.View == ViewType.Results)
                         .Select(entry => entry.Key).ToArray())
                _orderedDictionaryGroups.Remove(node);
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
            if (part == null) return -1;
            //
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
        // Holds the painting of the geometry tree, so that a change that rebuilds the tree and then selects in it
        // is painted once. The holds nest, so the rebuild's own hold does not end this one.
        public void BeginGeometryTreeUpdate()
        {
            cltvGeometry.BeginUpdate();
        }
        public void EndGeometryTreeUpdate()
        {
            cltvGeometry.EndUpdate();
        }
        // A sub part of a split compound with several cells is a node of its own, and one with a single cell is
        // shown as that cell
        public void SelectSplitSubPart(string compoundPartName, string subPartName)
        {
            try
            {
                _disableSelectionsChanged = true;
                //
                TreeNode subPartNode = null;
                foreach (var entry in _splitSubPartNodes)
                {
                    if (entry.Value == compoundPartName && entry.Key.Name == subPartName && entry.Key.TreeView != null)
                    {
                        subPartNode = entry.Key;
                        break;
                    }
                }
                if (subPartNode == null)
                {
                    foreach (TreeNode node in cltvGeometry.Nodes[0].Nodes.Find(subPartName, true))
                    {
                        if (node.Tag is GeometryPart && node.Parent?.Tag is CompoundGeometryPart compoundPart &&
                            compoundPart.Name == compoundPartName)
                        {
                            subPartNode = node;
                            break;
                        }
                    }
                }
                if (subPartNode == null) return;
                //
                for (TreeNode parent = subPartNode.Parent; parent != null; parent = parent.Parent) parent.Expand();
                subPartNode.Expand();
                cltvGeometry.SelectedNodes.Clear();
                cltvGeometry.SelectedNodes.Add(subPartNode);
                subPartNode.EnsureVisible();
            }
            finally
            {
                _disableSelectionsChanged = false;
            }
            UpdateHighlight();
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
                Dictionary<string, bool> prevGeometryTreeState = GetStableTreeExpandCollapseState(cltvGeometry);
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
                    // Modified keywords
                    if (model.CalculixKeywordOverrides != null)
                        SetNumberOfModifiedKeywords(model.GetNumberOfUnsuppressedKeywordOverrides());
                    // Geom Parts
                    if (model.Geometry != null)
                    {
                        AddObjectsToNode(_geomPartsName, _geomParts, model.Geometry.Parts);
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
                    // Distributions
                    AddObjectsToNode(_distributionsName, _distributions, model.Distributions);
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
                            fieldNames = results.GetVisibleFieldNames();
                            allComponents = new string[fieldNames.Length][];
                            for (int i = 0; i < fieldNames.Length; i++)
                            {
                                allComponents[i] = results.GetFieldComponentNames(fieldNames[i]);
                            }
                            CreateOverwriteFieldOutputNodes(fieldNames, allComponents, results.GetResultFieldOutputs());
                        }
                        // History outputs
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
                SetStableTreeExpandCollapseState(cltvGeometry, prevGeometryTreeState);
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
        public void AddTreeNode(ViewType view, NamedClass item, string parentName,
                                IOrderedDictionaryItemLayout<string> orderedDictionary)
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
                else if (item is Distribution)
                {
                    parent = _distributions;
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
                EnsureOrderedDictionaryContext(parent, view, item.GetType(), parentName, orderedDictionary);
                MoveTreeNodeToOrderedDictionaryLayoutParent(parent, node);
                //
                parent.Text = parent.Name;
                int parentItemCount = _orderedDictionaryContexts.ContainsKey(parent)
                    ? CountOrderedDictionaryItemNodes(parent)
                    : parent.Nodes.Count;
                if (parentItemCount > 0) parent.Text += " (" + parentItemCount + ")";
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
                if (TryGetOrderedDictionaryContext(parent, out OrderedDictionaryTreeContext context))
                {
                    context.RootNode.Text = context.RootNode.Name;
                    int itemCount = CountOrderedDictionaryItemNodes(context.RootNode);
                    if (itemCount > 0) context.RootNode.Text += " (" + itemCount + ")";
                }
                else if (parent.Tag is Field || parent.Tag is ResultFieldOutput ||
                    parent.Tag is HistoryResultSet || parent.Tag is HistoryResultField || parent.Tag is ResultHistoryOutput)
                {
                    SetNodeStatus(parent);  // remove dotted T icon
                }
                else
                {
                    parent.Text = parent.Name;
                    if (parent.Nodes.Count > 0) parent.Text += " (" + parent.Nodes.Count + ")";
                }
            }
            catch { }
            finally
            {
                tree.EndUpdate();
                tree.ResumeLayout();
            }
            //
            UpdateRelatedNodesHighlight();
        }
        private void AddObjectsToNode<TKey, TVal>(string initialNodeName, TreeNode node, IDictionary<TKey, TVal> dictionary,
                                                  bool countNodes = true, string parentName = null)
        {
            TreeNode nodeToAdd;
            //
            if (dictionary is IOrderedDictionaryItemLayout<string> orderedDictionary &&
                typeof(TKey) == typeof(string) && typeof(NamedClass).IsAssignableFrom(typeof(TVal)))
            {
                OrderedDictionaryTreeContext context = EnsureOrderedDictionaryContext(
                    node, GetViewType((CodersLabTreeView)node.TreeView), typeof(TVal), parentName,
                    orderedDictionary);
                HashSet<string> compoundSubPartNames = null;
                if (context.View == ViewType.Geometry)
                {
                    compoundSubPartNames = new HashSet<string>(orderedDictionary.Comparer);
                    foreach (TVal value in dictionary.Values)
                    {
                        if (value is CompoundGeometryPart compoundPart)
                            compoundSubPartNames.UnionWith(compoundPart.SubPartNames);
                    }
                }
                AddOrderedDictionaryLayoutNodes(node, orderedDictionary.GetItemLayoutSnapshot().Nodes,
                                                dictionary, context, compoundSubPartNames);
            }
            else
            {
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
            }
            //
            int itemCount = CountOrderedDictionaryItemNodes(node);
            if (countNodes && itemCount > 0) node.Text = initialNodeName + " (" + itemCount.ToString() + ")";
            else node.Text = initialNodeName;
        }
        private void AddOrderedDictionaryLayoutNodes<TKey, TVal>(
            TreeNode parentNode, IEnumerable<OrderedDictionaryItemNode<string>> layoutNodes,
            IDictionary<TKey, TVal> dictionary, OrderedDictionaryTreeContext context,
            HashSet<string> compoundSubPartNames)
        {
            foreach (OrderedDictionaryItemNode<string> layoutNode in layoutNodes)
            {
                if (layoutNode.IsGroup)
                {
                    TreeNode groupNode = parentNode.Nodes.Add("OrderedDictionaryGroup:" + layoutNode.Id.ToString("N"),
                                                              layoutNode.Name);
                    SetNodeImage(groupNode, "Folder.ico");
                    _orderedDictionaryGroups[groupNode] = new OrderedDictionaryGroupInfo
                    {
                        Context = context,
                        Id = layoutNode.Id,
                        Name = layoutNode.Name
                    };
                    AddOrderedDictionaryLayoutNodes(groupNode, layoutNode.Children, dictionary, context,
                                                    compoundSubPartNames);
                    groupNode.Expand();
                }
                else
                {
                    if (compoundSubPartNames != null && compoundSubPartNames.Contains(layoutNode.Key)) continue;
                    //
                    TKey key = (TKey)(object)layoutNode.Key;
                    if (!dictionary.TryGetValue(key, out TVal value)) continue;
                    if (value is NamedClass nc && nc.Internal) continue;
                    //
                    TreeNode itemNode = parentNode.Nodes.Add(layoutNode.Key);
                    itemNode.Name = itemNode.Text;
                    itemNode.Tag = value;
                    SetNodeStatus(itemNode);
                    //
                    if (context.View == ViewType.Geometry && value is CompoundGeometryPart compoundPart)
                    {
                        // The cells of a split compound are gathered under the part each of them was cut
                        // from, and those parts start collapsed, since a part may be cut into many cells.
                        // A part the split did not cut is its only cell and is shown as that cell. The parts are
                        // listed in the order the compound keeps for them, so one made by grouping cells comes last
                        List<int> cellIndices = new List<int>();
                        foreach (string splitSubPartName in compoundPart.GetSplitSubPartNames())
                        {
                            for (int i = 0; i < compoundPart.SubPartNames.Length; i++)
                            {
                                if (compoundPart.CellSubPartNames[i] == splitSubPartName) cellIndices.Add(i);
                            }
                        }
                        for (int i = 0; i < compoundPart.SubPartNames.Length; i++)
                        {
                            if (compoundPart.CellSubPartNames?[i] == null) cellIndices.Add(i);
                        }
                        //
                        Dictionary<string, TreeNode> splitSubPartNodes = new Dictionary<string, TreeNode>();
                        foreach (int i in cellIndices)
                        {
                            string subPartName = compoundPart.SubPartNames[i];
                            TKey subPartKey = (TKey)(object)subPartName;
                            if (!dictionary.TryGetValue(subPartKey, out TVal subPart)) continue;
                            if (subPart is NamedClass subPartNamedClass && subPartNamedClass.Internal) continue;
                            //
                            TreeNode parentOfSubPartNode = itemNode;
                            string splitSubPartName = compoundPart.CellSubPartNames?[i];
                            if (splitSubPartName != null && compoundPart.GetCellNames(splitSubPartName).Length > 1)
                            {
                                if (!splitSubPartNodes.TryGetValue(splitSubPartName, out parentOfSubPartNode))
                                {
                                    parentOfSubPartNode = itemNode.Nodes.Add(splitSubPartName);
                                    parentOfSubPartNode.Name = parentOfSubPartNode.Text;
                                    _splitSubPartNodes[parentOfSubPartNode] = compoundPart.Name;
                                    splitSubPartNodes[splitSubPartName] = parentOfSubPartNode;
                                }
                            }
                            //
                            TreeNode subPartNode = parentOfSubPartNode.Nodes.Add(subPartName);
                            subPartNode.Name = subPartNode.Text;
                            subPartNode.Tag = subPart;
                            SetNodeStatus(subPartNode);
                        }
                        itemNode.Expand();
                    }
                }
            }
        }
        private int CountOrderedDictionaryItemNodes(TreeNode node)
        {
            int count = 0;
            foreach (TreeNode child in node.Nodes)
            {
                if (_orderedDictionaryGroups.ContainsKey(child)) count += CountOrderedDictionaryItemNodes(child);
                else if (child.Tag is NamedClass) count++;
            }
            return count;
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
            AddObjectsToNode<string, HistoryOutput>(_modelHistoryOutputsName, tmp, step.HistoryOutputs, true, step.Name);
            SetNodeImage(tmp, "History_output.ico");
            // Field outputs
            tmp = stepNode.Nodes.Add(_modelFieldOutputsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, FieldOutput>(_modelFieldOutputsName, tmp, step.FieldOutputs, true, step.Name);
            SetNodeImage(tmp, "Field_output.ico");
            // Boundary conditions
            tmp = stepNode.Nodes.Add(_boundaryConditionsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, BoundaryCondition>(_boundaryConditionsName, tmp, step.BoundaryConditions, true,
                                                        step.Name);
            SetNodeImage(tmp, "Bc.ico");
            // Loads
            tmp = stepNode.Nodes.Add(_loadsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, Load>(_loadsName, tmp, step.Loads, true, step.Name);
            SetNodeImage(tmp, "Load.ico");
            // Defined fields
            tmp = stepNode.Nodes.Add(_definedFieldsName);
            tmp.Name = tmp.Text;
            AddObjectsToNode<string, DefinedField>(_definedFieldsName, tmp, step.DefinedFields, true, step.Name);
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
                // A sub part of a split compound shows the state of its cells
                if (node.Parent != null && _splitSubPartNodes.ContainsKey(node.Parent))
                    SetSplitSubPartNodeStatus(node.Parent);
            }
        }
        private void SetSplitSubPartNodeStatus(TreeNode subPartNode)
        {
            CodersLabTreeView cltv = (CodersLabTreeView)subPartNode.TreeView;
            if (cltv == null) return;
            //
            TreeNode visibleCellNode = subPartNode.Nodes.Cast<TreeNode>()
                                                  .FirstOrDefault(cellNode => cellNode.Tag is NamedClass cell &&
                                                                              cell.Visible);
            if (visibleCellNode == null)
            {
                cltv.SetNodeForeColor(subPartNode, Color.Gray);
                SetNodeImage(subPartNode, "Hide.ico");
            }
            else
            {
                cltv.SetNodeForeColor(subPartNode, Color.Black);
                SetNodeImage(subPartNode, visibleCellNode.ImageKey);
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
        private bool CheckNodeName(TreeNode node, string newName)
        {
            if (_splitSubPartNodes.ContainsKey(node)) return true;
            //
            if (_orderedDictionaryGroups.TryGetValue(node, out OrderedDictionaryGroupInfo groupInfo))
            {
                if ((groupInfo.Context.View == ViewType.Geometry ||
                     groupInfo.Context.View == ViewType.Model) &&
                    typeof(BasePart).IsAssignableFrom(groupInfo.Context.ItemType) &&
                    CheckPartGroupName != null)
                    return CheckPartGroupName(groupInfo.Id, newName);
                //
                foreach (TreeNode sibling in node.Parent.Nodes)
                {
                    if (!ReferenceEquals(sibling, node) &&
                        _orderedDictionaryGroups.TryGetValue(sibling, out OrderedDictionaryGroupInfo siblingInfo) &&
                        String.Equals(siblingInfo.Name, newName, StringComparison.OrdinalIgnoreCase))
                        return false;
                }
                return true;
            }
            //
            NamedClass item = (NamedClass)node.Tag;
            string stepName = GetStepNameFromNode(node);
            if (CheckItemName?.Invoke(item, newName, stepName) == false) return false;
            else return true;
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
                    if (_resultHistoryOutputs.Nodes.Count <= 1 && node2.Nodes.Count > 0) node2.Expand();
                }
                if (_resultHistoryOutputs.Nodes.Count <= 1 && node1.Nodes.Count > 0) node1.Expand();
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
        /// <summary>
        /// The expand state of the geometry and the model tree by node name, so that it can be put back on
        /// trees that were emptied and rebuilt in between, as a regenerate does.
        /// </summary>
        public Dictionary<string, bool>[] GetStableTreesExpandCollapseState()
        {
            return new Dictionary<string, bool>[] { GetStableTreeExpandCollapseState(cltvGeometry),
                                                    GetStableTreeExpandCollapseState(cltvModel) };
        }
        public void SetStableTreesExpandCollapseState(Dictionary<string, bool>[] states)
        {
            if (states == null || states.Length != 2) return;
            //
            SetStableTreeExpandCollapseState(cltvGeometry, states[0]);
            SetStableTreeExpandCollapseState(cltvModel, states[1]);
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
            else if (node.Name == _distributionsName) return true;
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
        private bool CanRename(TreeNode node)
        {
            if (_orderedDictionaryGroups.ContainsKey(node)) return true;
            else if (_splitSubPartNodes.ContainsKey(node)) return true;
            else if (node.Tag is BasePart) return true;
            else if (node.Tag is MeshSetupItem) return true;
            //
            else if (node.Tag is FeNodeSet) return true;
            else if (node.Tag is FeElementSet) return true;
            else if (node.Tag is FeSurface) return true;
            else if (node.Tag is FeReferencePoint) return true;
            else if (node.Tag is CoordinateSystem) return true;
            else if (node.Tag is Material) return true;
            else if (node.Tag is Section) return true;
            else if (node.Tag is Constraint) return true;
            else if (node.Tag is SurfaceInteraction) return true;
            else if (node.Tag is ContactPair) return true;
            else if (node.Tag is Distribution) return true;
            else if (node.Tag is Amplitude) return true;
            else if (node.Tag is InitialCondition) return true;
            else if (node.Tag is Step) return true;
            else if (node.Tag is HistoryOutput) return true;
            else if (node.Tag is FieldOutput) return true;
            else if (node.Tag is BoundaryCondition) return true;
            else if (node.Tag is Load) return true;
            else if (node.Tag is DefinedField) return true;
            //
            else if (node.Tag is AnalysisJob) return true;
            //
            else if (node.Tag is ResultFieldOutput) return true;
            else if (node.Tag is ResultHistoryOutput) return true;
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
            else if (node.Tag is Distribution) return true;
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
            else if (node.TreeView == cltvResults && node.Tag is ResultFieldOutput) return true;
            else if (node.TreeView == cltvResults && node.Tag is ResultHistoryOutput) return true;
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
            else if (node.Tag is Joint) return true;
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
            else if (item is InitialCondition) return true;
            else if (item is BoundaryCondition) return true;
            else if (item is Load) return true;
            else if (item is DefinedField) return true;
            else return false;
        }
        private bool CanDeactivate(TreeNode node)
        {
            if (node.Tag is MeshSetupItem) return true;
            else if (node.Tag is MeshPart) return true;
            else if (node.Tag is Material) return true;
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
            _numOfUserKeywords = numOfUserKeywords;
            UpdateNumberOfKeywords();
        }
        public void SetNumberOfModifiedKeywords(int numOfModifiedKeywords)
        {
            _numOfModifiedKeywords = numOfModifiedKeywords;
            UpdateNumberOfKeywords();
        }
        private void UpdateNumberOfKeywords()
        {
            _model.Text = _modelName;
            List<string> counts = new List<string>();
            if (_numOfUserKeywords > 0) counts.Add("User Keywords: " + _numOfUserKeywords);
            if (_numOfModifiedKeywords > 0) counts.Add("Modified Keywords: " + _numOfModifiedKeywords);
            if (counts.Count > 0) _model.Text += " (" + string.Join("; ", counts) + ")";
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
        // Shows the context menu of the given geometry parts without selecting them in the tree. The 3D view
        // uses it for a right click on a part that a selected mesh setup item highlights: the menu is the one
        // of the parts the item is defined on, while the item itself stays selected
        public void ShowContextMenuForGeometryParts(Control control, int x, int y, string[] partNames)
        {
            if (partNames == null || partNames.Length == 0) return;
            //
            List<TreeNode> partNodes = new List<TreeNode>();
            foreach (string partName in partNames)
            {
                TreeNode[] foundNodes = cltvGeometry.Nodes.Find(partName, true);
                foreach (TreeNode foundNode in foundNodes)
                    if (foundNode.Tag is BasePart) partNodes.Add(foundNode);
            }
            if (partNodes.Count == 0) return;
            //
            _contextMenuNodes = partNodes.ToArray();
            ShowContextMenu(control, x, y);
        }


    }
}
