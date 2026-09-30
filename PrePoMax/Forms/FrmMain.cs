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
using FileInOut.Output;
using PrePoMax.Commands;
using PrePoMax.Forms;
using PrePoMax.Python;
using PrePoMax.Python.Journal;
using Python.Runtime;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using UserControls;
using vtkControl;


namespace PrePoMax
{
    public partial class FrmMain : MainMouseWheelManagedForm
    {
        private class ViewportLegendValueEditData
        {
            public readonly double Minimum;
            public readonly double Maximum;
            public readonly string OriginalText;

            public ViewportLegendValueEditData(double minimum, double maximum, string originalText)
            {
                Minimum = minimum;
                Maximum = maximum;
                OriginalText = originalText;
            }
        }


        // Variables                                                                                                                
        #region Variables ##########################################################################################################
        //
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        //
        FrmSplash _splash;
        //
        private vtkControl.vtkControl _vtk;
        private ModelTree _modelTree;
        private Controller _controller;
        private CommandLineOptions _cmdOptions;
        private bool _hideWindow;
        private const int _numOfOutputDateChars = 20;
        private const int _numOfOutputLines = 500;
        private List<string> _outputLines = new List<string>();
        // Whether each output line is an error or a warning, which are the lines whose geometry names are links
        private List<bool> _outputLineIsMessage = new List<bool>();
        private SidePanel _visibleSidePanel;
        private SplitContainer _sidePanelHost;
        private PythonPanel _pythonPanel;
        private AdvisorPanel _advisorPanel;
        private SidePanelsState _sidePanelsState;
        private SidePanelsState _pendingSidePanelsState;
        private Rpc.RpcServer _automationServer;
        private bool _isFirstRunInFolder;
        private bool _startupCompleted;
        private bool _pythonScriptTookState;
        private bool _pythonPanelStateApplied;
        private bool _xyPlotPanelStateApplied;
        private bool _advisorPanelStateApplied;
        private bool _restoringSidePanelsState;
        private KeyboardHook _keyboardHook;
        private Dictionary<ViewGeometryModelResults, int> _selectedSymbolIndex;
        private Stack<bool[]> _prevMenuStates;
        private bool _closeAfterRegeneration;
        private float _prevScaleFactor;
        private bool _suppressSymbolChanged;
        private bool _suppressColorByChanged;
        private vtkResultWidgetContextMenuTypeEnum _resultWidgetContextMenuType;
        private bool _resultWidgetContextMenuMinimum;
        private ContextMenuStrip _cmsViewport;
        private ToolStripMenuItem _tsmiViewportUserViews;
        private ToolStripMenuItem _tsmiViewportCoorSys;
        //
        private Point _formLocation;
        private List<Form> _allForms;
        private FrmSelectEntity _frmSelectEntity;
        private FrmSelectGeometry _frmSelectGeometry;
        private FrmSelectItemSet _frmSelectItemSet;
        private FrmUserVew _frmUserView;
        private FrmSectionView _frmSectionView;
        private FrmExplodedView _frmExplodedView;
        private FrmFollowerView _frmFollowerView;
        private FrmNewModel _frmNewModel;
        private FrmExportFileWithProperties _frmExportFileWithProperties;
        private FrmEditCommands _frmEditCommands;
        private FrmRegenerate _frmRegenerate;
        private FrmAnalyzeGeometry _frmAnalyzeGeometry;
        private FrmMeshSetupItem _frmMeshSetupItem;
        private FrmSplitSolid _frmSplitSolid;
        private FrmSplitShell _frmSplitShell;
        private FrmOffsetFace _frmOffsetFace;
        private FrmModelProperties _frmModelProperties;
        private FrmCalculixKeywordEditor _frmCalculixKeywordEditor;
        private FrmBoundaryLayer _frmBoundaryLayer;
        private FrmRemeshingParameters _frmRemeshingParameters;
        private FrmThickenShellMesh _frmThickenShellMesh;
        private FrmSplitPartMeshUsingSurface _frmSplitPartMeshUsingSurface;
        private FrmMergeCoincidentNodes _frmMergeCoincidentNodes;
        private FrmElementQuality _frmElementQuality;
        private FrmPartProperties _frmPartProperties;
        private FrmTranslate _frmTranslate;
        private FrmRotate _frmRotate;
        private FrmMirror _frmMirror;
        private FrmScale _frmScale;
        private FrmNodeSet _frmNodeSet;
        private FrmElementSet _frmElementSet;
        private FrmSurface _frmSurface;
        private FrmReferencePoint _frmReferencePoint;
        private FrmCoordinateSystem _frmCoordinateSystem;
        private FrmMaterial _frmMaterial;
        private FrmSection _frmSection;
        private FrmConstraint _frmConstraint;
        private FrmSurfaceInteraction _frmSurfaceInteraction;
        private FrmContactPair _frmContactPair;
        private FrmSearchContactPairs _frmSearchContactPairs;
        private FrmInitialCondition _frmInitialCondition;
        private FrmDistribution _frmDistribution;
        private FrmAmplitude _frmAmplitude;
        private FrmStep _frmStep;
        private FrmStepControls _frmStepControls;
        private FrmHistoryOutput _frmHistoryOutput;
        private FrmFieldOutput _frmFieldOutput;
        private FrmBC _frmBoundaryCondition;
        private FrmLoad _frmLoad;
        private FrmDefinedField _frmDefinedField;
        private FrmSettings _frmSettings;
        private FrmQuery _frmQuery;
        private FrmFind _frmFind;
        private FrmCaptureViewport _frmCaptureViewport;
        private FrmAnalysis _frmAnalysis;
        private FrmMonitor _frmMonitor;
        private FrmAnimation _frmAnimation;
        private FrmResultFieldOutput _frmResultFieldOutput;
        private FrmResultHistoryOutput _frmResultHistoryOutput;
        private FrmViewResultHistoryOutput _frmViewResultHistoryOutput;
        private FrmTransformation _frmTransformation;
        //
        #endregion  ################################################################################################################

        #region Properties #########################################################################################################
        public Controller Controller { get { return _controller; } set { _controller = value; } }
        public ViewGeometryModelResults GetCurrentView()
        {
            return _controller.CurrentView;
        }
        public void SetCurrentView(ViewGeometryModelResults view)
        {
            // This gets called from: _controller.CurrentView
            InvokeIfRequired(() =>
            {
                if (view == ViewGeometryModelResults.Geometry)
                {
                    _modelTree.SetGeometryTab();
                    if (_controller.Model != null) UpdateUnitSystem(_controller.Model.UnitSystem);
                }
                else if (view == ViewGeometryModelResults.Model)
                {
                    _modelTree.SetModelTab();
                    if (_controller.Model != null) UpdateUnitSystem(_controller.Model.UnitSystem);
                }
                else if (view == ViewGeometryModelResults.Results)
                {
                    _modelTree.SetResultsTab();
                    if (_controller.CurrentResult != null) UpdateUnitSystem(_controller.CurrentResult.UnitSystem);
                    InitializeResultWidgetPositions();
                }
                else throw new NotSupportedException();
                //
                if (_visibleSidePanel != null)
                {
                    ViewType viewType = GetViewType(view);
                    //
                    _visibleSidePanel.PrepareControls(viewType);
                }
                //
                SetMenuAndToolStripVisibility();
                // This calls the saved action
                SetCurrentEdgesVisibilities(_controller.CurrentEdgesVisibility);    // highlights selected buttons
                //
                this.ActiveControl = null;
            });
        }
        public void SetCurrentEdgesVisibilities(vtkControl.vtkEdgesVisibility edgesVisibility)
        {
            InvokeIfRequired(() =>
            {
                // Highlight selected buttons
                tsbShowWireframeEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.Wireframe;
                tsbShowElementEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.ElementEdges;
                tsbShowModelEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.ModelEdges;
                tsbShowNoEdges.Checked = edgesVisibility == vtkControl.vtkEdgesVisibility.NoEdges;
                //
                _vtk.EdgesVisibility = edgesVisibility;
                //
                UpdateHighlight();
            });
        }
        public bool ScreenUpdating { get { return _modelTree.ScreenUpdating; } set { _modelTree.ScreenUpdating = value; } }
        public bool RenderingOn
        {
            get { return InvokeIfRequired(() => _vtk.RenderingOn); }
            set { InvokeIfRequired(() => _vtk.RenderingOn = value); }
        }
        private ViewType GetViewType(ViewGeometryModelResults view)
        {
            ViewType viewType;
            if (view == ViewGeometryModelResults.Geometry) viewType = ViewType.Geometry;
            else if (view == ViewGeometryModelResults.Model) viewType = ViewType.Model;
            else if (view == ViewGeometryModelResults.Results) viewType = ViewType.Results;
            else throw new NotSupportedException();
            return viewType;
        }
        public bool LastOpenResultsAccepted { get; private set; }

        #endregion  ################################################################################################################


        // Constructors                                                                                                             
        public FrmMain(CommandLineOptions cmdOptions)
        {
            // Initialize               
            InitializeComponent();
            ToolStripMenuItem duplicateResultFieldOutputMenuItem = new ToolStripMenuItem();
            duplicateResultFieldOutputMenuItem.Name = "tsmiDuplicateResultFieldOutput";
            duplicateResultFieldOutputMenuItem.Text = "Duplicate";
            duplicateResultFieldOutputMenuItem.Click += tsmiDuplicateResultFieldOutput_Click;
            tsmiResultFieldOutput.DropDownItems.Insert(2, duplicateResultFieldOutputMenuItem);
            ToolStripMenuItem duplicateResultHistoryOutputMenuItem = new ToolStripMenuItem();
            duplicateResultHistoryOutputMenuItem.Name = "tsmiDuplicateResultHistoryOutput";
            duplicateResultHistoryOutputMenuItem.Text = "Duplicate";
            duplicateResultHistoryOutputMenuItem.Click += tsmiDuplicateResultHistoryOutput_Click;
            tsmiResultHistoryOutput.DropDownItems.Insert(2, duplicateResultHistoryOutputMenuItem);
            InitializeViewportContextMenu();
            //SettingsContainer settings = new SettingsContainer();
            //settings.LoadFromFile();
            //if (settings.General.Maximized)
            //{
            //    Rectangle resolution = Screen.FromControl(this).Bounds;
            //    this.Location = new Point(0, 0);
            //    this.Size = new Size(resolution.Width, resolution.Height);
            //    this.WindowState = FormWindowState.Maximized;
            //}
            _vtk = null;
            _controller = null;
            _modelTree = null;
            _prevMenuStates = new Stack<bool[]>();
            _closeAfterRegeneration = false;
            _cmdOptions = cmdOptions;
            CommandLineOptions.CheckForErrors(_cmdOptions); // make sure options are compatible
            //
            MessageBoxes.ParentForm = this;
            //
            if (_cmdOptions.ShowGui == "No")
            {
                _hideWindow = true;
                MessageBoxes.WriteDataToOutput = WriteDataToOutput;
                AutoClosingMessageBox.WriteDataToOutput = WriteDataToOutput;
            }
            MessageBoxes.Mode = _cmdOptions.GetMessageBoxMode();
            // Every dialog shown without a user present belongs in the run report
            BatchRunReport report = BatchRunReport.Current;
            if (report != null && MessageBoxes.Mode != MessageBoxModeEnum.Interactive)
            {
                MessageBoxes.PromptRecorder = report.AddPrompt;
                AutoClosingMessageBox.PromptRecorder = report.AddPrompt;
            }
        }
        protected override bool ShowWithoutActivation { get { return _hideWindow; } }
        protected override CreateParams CreateParams
        {
            get
            {
                // An unattended run must not take the foreground, but the window is still created so that vtkControl
                // gets a handle in FrmMain_Load and the render window a parent id
                CreateParams createParams = base.CreateParams;
                if (_hideWindow) createParams.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return createParams;
            }
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            PythonPanel pythonPanel = _visibleSidePanel as PythonPanel;
            if (pythonPanel != null && pythonPanel.Focused && pythonPanel.ProcessShortcutKey(keyData))
                return true;

            if ((keyData & Keys.Alt) == Keys.Alt && (keyData & Keys.Control) == Keys.Control)
                return false;

            if ((keyData & Keys.KeyCode) == Keys.F &&
                (keyData & (Keys.Control | Keys.Alt | Keys.Shift)) == Keys.Control)
            {
                tsmiFind_Click(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        // Event handling                                                                                                           
        private void FrmMain_Load(object sender, EventArgs e)
        {
            if (TestWriteAccess() == false)
            {
                MessageBoxes.ShowError("PrePoMax has no write access for the folder: " + Application.StartupPath +
                                       Environment.NewLine + Environment.NewLine +
                                       "To run PrePoMax, move the base PrePoMax folder to another, non-protected folder.");
                Close();
                return;
            }
            //
            //SettingsContainer settings = new SettingsContainer();
            //settings.LoadFromFile();
            //if (settings.General.Maximized)
            //{
            //    Rectangle resolution = Screen.FromControl(this).Bounds;
            //    this.Location = new Point(0, 0);
            //    this.Size = new Size(resolution.Width, resolution.Height);
            //    this.WindowState = FormWindowState.Maximized;
            //}
            Text = Globals.ProgramName;
            if (!_hideWindow) this.TopMost = true;
            //
            if (_cmdOptions.ShowGui == "Yes")
            {
                _splash = new FrmSplash { TopMost = true };
                Task.Run(() => _splash.ShowDialog());
            }
            //
            try
            {
                // Viewport text editor
                panelControl.Controls.Remove(viewportTextEditor);
                this.Controls.Add(viewportTextEditor);
                viewportTextEditor.CommitRequested += ViewportTextEditor_CommitRequested;
                viewportTextEditor.CancelRequested += CancelViewportEdit;
                viewportTextEditor.NextRequested += ViewportTextEditor_NextRequested;
                // Vtk
                _vtk = new vtkControl.vtkControl();
                _vtk.ViewAnimationEnabled = _cmdOptions.ShowGui != "No";
                panelControl.Parent.Controls.Add(_vtk);
                panelControl.SendToBack();
                // Menu
                tsmiColorAnnotations.DropDown.Closing += DropDown_Closing;
                tsmiViewItems.DropDown.Closing += DropDown_Closing;
                // Tree
                _modelTree = new ModelTree();
                // Controller
                _controller = new Controller(this);
                //
                _modelTree.Name = "modelTree";
                splitContainer1.Panel1.Controls.Add(this._modelTree);
                _modelTree.Dock = DockStyle.Fill;
                _modelTree.TabIndex = 0;
                _modelTree.RegenerateTreeCallBack = RegenerateTreeCallback;
                _modelTree.CheckItemName = _controller.CheckItemName;
                _modelTree.GetNextPartGroupName = _controller.GetNextPartGroupName;
                _modelTree.CheckPartGroupName = _controller.CheckPartGroupName;
                _modelTree.GetRelatedGeometryItemNames = _controller.GetRelatedGeometryItemNames;
                _modelTree.GetPartNamesToMeshFromMeshSetupItems = _controller.GetPartNamesToMeshFromMeshSetupItems;
                //
                _modelTree.GeometryMeshResultsEvent += ModelTree_ViewEvent;
                _modelTree.SelectEvent += ModelTree_SelectEvent;
                _modelTree.ClearSelectionEvent += Clear3DSelection;
                _modelTree.ContextMenuForPartsClosedEvent += UpdateHighlight;
                _modelTree.CreateEvent += ModelTree_CreateEvent;
                _modelTree.EditEvent += ModelTree_EditEvent;
                _modelTree.RenameEvent += ModelTree_RenameEvent;
                _modelTree.RenameSplitSubPartEvent += ModelTree_RenameSplitSubPartEvent;
                _modelTree.GroupSplitCellsEvent += ModelTree_GroupSplitCellsEvent;
                _modelTree.FuseBodiesEvent += ModelTree_FuseBodiesEvent;
                _modelTree.SetPartColorEvent += ModelTree_SetPartColorEvent;
                _modelTree.ResetPartColorEvent += ModelTree_ResetPartColorEvent;
                _modelTree.EditStepControlsEvent += EditStepControls;
                _modelTree.QueryEvent += ModelTree_QueryEvent;
                _modelTree.DuplicateEvent += ModelTree_DuplicateEvent;
                _modelTree.PropagateEvent += ModelTree_PropagateEvent;
                _modelTree.PreviewEvent += ModelTree_PreviewEvent;
                _modelTree.AddItemsToPlotEvent += AddItemsToPlot;
                _modelTree.AddItemsToNewPlotEvent += AddItemsToNewPlot;
                _modelTree.CanAddItemsToNewPlot = CanAddItemsToNewPlot;
                _modelTree.CreateCompoundPart += CreateAndImportCompoundPart;
                _modelTree.SwapPartGeometries += SwapPartGeometries;
                _modelTree.PreviewEdgeMesh += PreviewEdgeMeshAsync;
                _modelTree.CreateMeshEvent += CreatePartMeshes;
                _modelTree.CopyGeometryToResultsEvent += CopyGeometryPartsToResults;
                _modelTree.EditCalculixKeywords += EditCalculiXKeywords;
                _modelTree.MergeParts += MergeModelParts;
                _modelTree.MergeResultParts += MergeResultParts;
                _modelTree.ConvertElementSetsToMeshParts += ConvertElementSetsToMeshParts;
                _modelTree.MaterialLibrary += ShowMaterialLibrary;
                _modelTree.SearchContactPairs += SearchContactPairs;
                _modelTree.SwapMasterSlave += ModelTree_SwapMasterSlave;
                _modelTree.MergeByMasterSlave += ModelTree_MergeByMasterSlave;
                _modelTree.HideShowEvent += ModelTree_HideShowEvent;
                _modelTree.SetTransparencyEvent += ModelTree_SetTransparencyEvent;
                _modelTree.ResultRepresentationVisibilityEvent += ModelTree_ResultRepresentationVisibilityEvent;
                _modelTree.RunEvent += RunAnalysis;
                _modelTree.CheckModelEvent += CheckModel;
                _modelTree.MonitorEvent += MonitorAnalysis;
                _modelTree.ResultsEvent += ResultsAnalysis;
                _modelTree.KillEvent += KillAnalysis;
                _modelTree.ActivateDeactivateEvent += ModelTree_ActivateDeactivateEvent;
                _modelTree.DeleteEvent += ModelTree_DeleteEvent;
                _modelTree.OrderedDictionaryTreeLayoutChangeEvent += ModelTree_OrderedDictionaryTreeLayoutChangeEvent;
                _modelTree.MoveStepsEvent += ModelTree_MoveStepsEvent;
                _modelTree.FieldDataSelectEvent += ModelTree_FieldDataSelectEvent;
                _modelTree.RenderingOff += () => _vtk.RenderingOn = false;
                _modelTree.RenderingOn += () => _vtk.RenderingOn = true;
                // Strip menus
                tsFile.Location = new Point(0, 0);
                tsViews.Location = new Point(tsFile.Left + tsFile.Width, 0);
                tsSymbols.Location = new Point(tsViews.Left + tsViews.Width, 0);
                tsResultDeformation.Location = new Point(0, tsFile.Height);
                tsResults.Location = new Point(tsResultDeformation.Left + tsResultDeformation.Width, tsFile.Height);
                tscbSymbols.SelectedIndexChanged += tscbSymbols_SelectedIndexChanged;
                tscbColorBy.SelectedIndex = 0;
                tscbColorBy.SelectedIndexChanged += tscbColorBy_SelectedIndexChanged;
                // Vtk
                _vtk.OnMouseLeftButtonUpSelection += _vtk_SelectPointOrArea;
                _vtk.RotationPointChanged += _vtk_RotationPointChanged;
                _vtk.Controller_GetAnnotationText += _controller.GetAnnotationText;
                _vtk.Controller_GetSectionViewHatchGroup = _controller.GetSectionViewHatchGroup;
                _vtk.Controller_AnimationFrameChanged = _controller.AnimationFrameChanged;
                _vtk.Controller_GetAnimationLocatorNodes = _controller.GetAnimationLocatorNodes;
                _vtk.Controller_GetVolumeGridData = _controller.GetVolumeGridData;
                _vtk.Controller_GetAnimationNodes = _controller.GetAnimationNodes;
                _vtk.Controller_GetNodeActorData = _controller.GetNodeActorData;
                _vtk.Controller_GetCellActorData = _controller.GetCellActorData;
                _vtk.Controller_GetCellFaceActorData = _controller.GetCellFaceActorData;
                _vtk.Controller_GetEdgeActorData = _controller.GetEdgeActorData;
                _vtk.Controller_GetSurfaceEdgesActorDataFromElementId = _controller.GetSurfaceEdgesActorDataFromElementId;
                _vtk.Controller_GetSurfaceEdgesActorDataFromNodeAndElementIds =
                    _controller.GetSurfaceEdgesActorDataFromNodeAndElementIds;
                _vtk.Controller_GetPartActorData = _controller.GetPartActorData;
                _vtk.Controller_GetGeometryActorData = _controller.GetGeometryActorData;
                _vtk.Controller_GetGeometryVertexActorData = _controller.GetGeometryVertexActorData;
                _vtk.Controller_ActorsPicked = SelectBaseParts;
                _vtk.Form_EditLegendValue = StartEditLegendValue;
                _vtk.Form_ShowResultWidgetContextMenu = ShowResultWidgetContextMenu;
                _vtk.Form_ShowViewportContextMenu = ShowViewportContextMenu;
                _vtk.Form_ShowColorBarSettings = ShowColorBarSettings;
                _vtk.Form_ShowLegendSettings = ShowLegendSettings;
                _vtk.Form_ShowStatusBlockSettings = ShowStatusBlockSettings;
                _vtk.Form_CommitViewportEdit = CommitViewportEdit;
                _vtk.Form_WidgetPicked = AnnotationPicked;
                _vtk.Form_BoxZoomEnded = BoxZoomEnded;
                // Output
                tbOutput.Dock = DockStyle.Fill;
                tbOutput.LinkFinder = FindOutputLinks;
                // Forms
                _formLocation = new Point(100, 100);
                _allForms = new List<Form>();
                //
                _frmSelectEntity = new FrmSelectEntity(_controller);
                AddFormToAllForms(_frmSelectEntity);
                //
                _frmSelectGeometry = new FrmSelectGeometry(_controller);
                AddFormToAllForms(_frmSelectGeometry);
                //
                _frmSelectItemSet = new FrmSelectItemSet(_controller);
                AddFormToAllForms(_frmSelectItemSet);
                //
                _frmUserView = new FrmUserVew(_controller);
                AddFormToAllForms(_frmUserView);
                //
                _frmSectionView = new FrmSectionView(_controller);
                AddFormToAllForms(_frmSectionView);
                //
                _frmExplodedView = new FrmExplodedView(_controller);
                _frmExplodedView.Clear3D = Clear3DSelection;
                AddFormToAllForms(_frmExplodedView);
                //
                _frmFollowerView = new FrmFollowerView(_controller);
                AddFormToAllForms(_frmFollowerView);
                //
                _frmNewModel = new FrmNewModel(_controller);
                AddFormToAllForms(_frmNewModel);
                //
                _frmExportFileWithProperties = new FrmExportFileWithProperties(_controller);
                _frmExportFileWithProperties.ExportUsingFileProperties = ExportUsingFileProperties;
                AddFormToAllForms(_frmExportFileWithProperties);
                //
                _frmEditCommands = new FrmEditCommands(_controller);
                AddFormToAllForms(_frmEditCommands);
                //
                _frmRegenerate = new FrmRegenerate();
                AddFormToAllForms(_frmRegenerate);
                //
                _frmAnalyzeGeometry = new FrmAnalyzeGeometry(_controller);
                AddFormToAllForms(_frmAnalyzeGeometry);
                //
                _frmMeshSetupItem = new FrmMeshSetupItem(_controller);
                _frmMeshSetupItem.PreviewEdgeMeshAsync = PreviewEdgeMeshAsync;
                AddFormToAllForms(_frmMeshSetupItem);
                //
                _frmSplitSolid = new FrmSplitSolid(_controller);
                AddFormToAllForms(_frmSplitSolid);
                //
                _frmSplitShell = new FrmSplitShell(_controller);
                AddFormToAllForms(_frmSplitShell);
                //
                _frmOffsetFace = new FrmOffsetFace(_controller);
                AddFormToAllForms(_frmOffsetFace);
                //
                _frmModelProperties = new FrmModelProperties(_controller);
                AddFormToAllForms(_frmModelProperties);
                //
                _frmBoundaryLayer = new FrmBoundaryLayer(_controller);
                AddFormToAllForms(_frmBoundaryLayer);
                //
                _frmRemeshingParameters = new FrmRemeshingParameters(_controller);
                AddFormToAllForms(_frmRemeshingParameters);
                //
                _frmThickenShellMesh = new FrmThickenShellMesh(_controller);
                AddFormToAllForms(_frmThickenShellMesh);
                //
                _frmSplitPartMeshUsingSurface = new FrmSplitPartMeshUsingSurface(_controller);
                _frmSplitPartMeshUsingSurface.SplitPartMeshUsingSurface += SplitPartMeshUsingSurface;
                AddFormToAllForms(_frmSplitPartMeshUsingSurface);
                //
                _frmMergeCoincidentNodes = new FrmMergeCoincidentNodes(_controller);
                AddFormToAllForms(_frmMergeCoincidentNodes);
                //
                _frmElementQuality = new FrmElementQuality(_controller);
                AddFormToAllForms(_frmElementQuality);
                //
                _frmPartProperties = new FrmPartProperties(_controller);
                AddFormToAllForms(_frmPartProperties);
                //
                _frmTranslate = new FrmTranslate(_controller);
                _frmTranslate.TranslateGeometryPartsAsync = TranslateGeometryPartsAsync;
                AddFormToAllForms(_frmTranslate);
                //
                _frmRotate = new FrmRotate(_controller);
                _frmRotate.RotateGeometryPartsAsync = RotateGeometryPartsAsync;
                AddFormToAllForms(_frmRotate);
                //
                _frmMirror = new FrmMirror(_controller);
                _frmMirror.MirrorGeometryPartsAsync = MirrorGeometryPartsAsync;
                AddFormToAllForms(_frmMirror);
                //
                _frmScale = new FrmScale(_controller);
                _frmScale.ScaleGeometryPartsAsync = ScaleGeometryPartsAsync;
                AddFormToAllForms(_frmScale);
                //
                _frmNodeSet = new FrmNodeSet(_controller);
                AddFormToAllForms(_frmNodeSet);
                //
                _frmElementSet = new FrmElementSet(_controller);
                AddFormToAllForms(_frmElementSet);
                //
                _frmSurface = new FrmSurface(_controller);
                AddFormToAllForms(_frmSurface);
                //
                _frmReferencePoint = new FrmReferencePoint(_controller);
                AddFormToAllForms(_frmReferencePoint);
                //
                _frmCoordinateSystem = new FrmCoordinateSystem(_controller);
                AddFormToAllForms(_frmCoordinateSystem);
                //
                _frmMaterial = new FrmMaterial(_controller);
                AddFormToAllForms(_frmMaterial);
                //
                _frmSection = new FrmSection(_controller);
                AddFormToAllForms(_frmSection);
                //
                _frmConstraint = new FrmConstraint(_controller);
                AddFormToAllForms(_frmConstraint);
                //
                _frmSurfaceInteraction = new FrmSurfaceInteraction(_controller);
                AddFormToAllForms(_frmSurfaceInteraction);
                //
                _frmContactPair = new FrmContactPair(_controller);
                AddFormToAllForms(_frmContactPair);
                //
                _frmSearchContactPairs = new FrmSearchContactPairs(_controller);
                AddFormToAllForms(_frmSearchContactPairs);
                //
                _frmInitialCondition = new FrmInitialCondition(_controller);
                AddFormToAllForms(_frmInitialCondition);
                //
                _frmDistribution = new FrmDistribution(_controller);
                AddFormToAllForms(_frmDistribution);
                //
                _frmAmplitude = new FrmAmplitude(_controller);
                AddFormToAllForms(_frmAmplitude);
                //
                _frmStep = new FrmStep(_controller);
                AddFormToAllForms(_frmStep);
                //
                _frmStepControls = new FrmStepControls(_controller);
                AddFormToAllForms(_frmStepControls);
                //
                _frmHistoryOutput = new FrmHistoryOutput(_controller);
                AddFormToAllForms(_frmHistoryOutput);
                //
                _frmFieldOutput = new FrmFieldOutput(_controller);
                AddFormToAllForms(_frmFieldOutput);
                //
                _frmBoundaryCondition = new FrmBC(_controller);
                AddFormToAllForms(_frmBoundaryCondition);
                //
                _frmLoad = new FrmLoad(_controller);
                AddFormToAllForms(_frmLoad);
                //
                _frmDefinedField = new FrmDefinedField(_controller);
                AddFormToAllForms(_frmDefinedField);
                //
                _frmAnalysis = new FrmAnalysis(_controller);
                AddFormToAllForms(_frmAnalysis);
                //
                _frmMonitor = new FrmMonitor(_controller);
                _frmMonitor.KillJob += KillAnalysis;
                _frmMonitor.Results += ResultsAnalysis;
                AddFormToAllForms(_frmMonitor);
                //
                _frmSettings = new FrmSettings();
                _frmSettings.UpdateSettings += UpdateSettings;
                _frmSettings.AutomationAccessTokenChanged += SetAutomationAccessToken;
                AddFormToAllForms(_frmSettings);
                //
                _frmQuery = new FrmQuery();
                _frmQuery.Form_WriteDataToOutput = WriteDataToOutput;
                _frmQuery.Form_RemoveAnnotations = tsbRemoveAnnotations_Click;
                AddFormToAllForms(_frmQuery);
                //
                _frmFind = new FrmFind();
                _frmFind.Form_RemoveAnnotations = tsbRemoveAnnotations_Click;
                AddFormToAllForms(_frmFind);
                //
                _frmCaptureViewport = new FrmCaptureViewport();
                _frmCaptureViewport.Form_GetDefaultProperties = GetDefaultCaptureViewportProperties;
                _frmCaptureViewport.Form_Capture = CaptureViewport;
                AddFormToAllForms(_frmCaptureViewport);
                //
                _frmAnimation = new FrmAnimation();
                _frmAnimation.Form_ControlsEnable = SetMenuAndToolStripVisibilityByAnimation;
                AddFormToAllForms(_frmAnimation);
                //
                _frmResultFieldOutput = new FrmResultFieldOutput(_controller);
                AddFormToAllForms(_frmResultFieldOutput);
                //
                _frmResultHistoryOutput = new FrmResultHistoryOutput(_controller);
                AddFormToAllForms(_frmResultHistoryOutput);
                //
                _frmViewResultHistoryOutput = new FrmViewResultHistoryOutput(_controller);
                AddFormToAllForms(_frmViewResultHistoryOutput);
                //
                _frmTransformation = new FrmTransformation(_controller);
                AddFormToAllForms(_frmTransformation);
                // Deformation toolstrip
                InitializeDeformationComboBoxes();
                InitializeComplexComboBoxes();
                // Converters
                tstbDeformationFactor.UnitConverter = new StringDoubleConverter();
                tstbAngle.UnitConverter = new StringAngleDegConverter();
                // Create the Keyboard Hook
                _keyboardHook = new KeyboardHook();
                // Capture the events
                _keyboardHook.KeyDown += KeyboardHook_KeyDown;
                // Install the hook
                _keyboardHook.Install();
            }
            catch
            {
                // If no error the splash is closed latter
                _splash?.BeginInvoke((MethodInvoker)delegate () { _splash.Close(); });
            }
            finally
            {
                // Set form size if visible - after top most
                if (_cmdOptions.ShowGui == "Yes") _controller.Settings.General.ApplyFormSize(this);
            }
            //
#if !DEBUG
            tsmiTest.Visible = false;
            tsmiCropStlPartWithCylinder.Visible = false;
            tsmiCropStlPartWithCube.Visible = false;
#endif
        }
        //
        private async void FrmMain_Shown(object sender, EventArgs e)
        {
            // Set vtk control size
            UpdateVtkControlSize();
            Application.DoEvents(); // draws the menus
            //
            _vtk.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            // Set pass through control for the mouse wheel event
            this.PassThroughControl = _vtk;
            // Vtk
            // Reduce flicker
            _vtk.Visible = true;
            _vtk.ResetScaleBarPosition();   // start after the vtk size is set
            _vtk.SetZoomFactor(1000);       // set starting zoom larger that the object
            _controller.Redraw();
            // Move _vtk to the edge of the screen to hide the black background
            int left = _vtk.Left;
            _vtk.Left = _vtk.Width + 4;
            Application.DoEvents(); // draws the vtk
            _vtk.Visible = false;
            _vtk.Left = left;
            // Close splash 
            if (_splash != null) _splash.BeginInvoke((MethodInvoker)delegate () { _splash.Close(); });
            // At the end when vtk is loaded open the file
            string fileName = null;
            UnitSystemType unitSystemType = UnitSystemType.Undefined;
            //
            // Asked here rather than at the end of this method, because it has to be answered before
            // anything can start the automation server: a folder that was just unpacked must not serve
            // the access token that shipped in its settings file. IsFirstRun writes the marker, so it
            // can only be asked once and the answer is kept for the working directory check below
            _isFirstRunInFolder = FirstRunChecker.IsFirstRun(Application.StartupPath);
            if (_isFirstRunInFolder) ClearAutomationAccessTokenOfAnUnpackedFolder();
            //
            try
            {
                StartAutomationServerIfConfigured();
                // Regeneration
                if (_cmdOptions.RegenerationFileName != null)
                {
                    WriteDataToOutput("Starting regeneration");
                    //
                    fileName = _cmdOptions.RegenerationFileName;
                    //
                    if (fileName == null)
                        throw new CaeException("The regeneration file name is null.");
                    else if (!File.Exists(fileName))
                        throw new CaeException("The regeneration file " + fileName + " does not exist.");
                    //
                    _controller.RegenerationWorkDirectory = _cmdOptions.WorkDirectory;
                    // Open
                    await Task.Run(() => OpenAsync(fileName, _controller.Open));
                    //
                    if (_cmdOptions.ParametersExportFileName != null)
                    {
                        // Parameters export
                        await Task.Run(() => _controller.ExportParametersToFile(_cmdOptions.ParametersExportFileName));
                    }
                    else
                    {
                        // Parameters override
                        if (_cmdOptions.Parameters != null)
                            _controller.AddOverriddenParametersFromString(_cmdOptions.Parameters);
                        // Regenerate
                        await Task.Run(() => _controller.RegenerateHistoryCommands(false, false, _cmdOptions.GetRegenerateType()));
                        // Check for errors
                        if (_controller.GetCommandCollectionErrors() != null && _controller.GetCommandCollectionErrors().Count > 0)
                            throw new CaeException("Failed to regenerate some commands.");
                        // Overwrite
                        if (_cmdOptions.Overwrite == "Yes")
                        {
                            WriteDataToOutput("Overwrite: " + fileName);
                            _controller.Model.Parameters.ClearOverriddenParameters();
                            _controller.SaveToPmx(fileName);
                        }
                    }
                    // Exit
                    if (_cmdOptions.ExitAfterRegeneration == "Yes")
                    {
                        _closeAfterRegeneration = true;
                        WriteDataToOutput("Close application.");
                        Close();
                    }
                }
                else
                {
                    // must be here - otherwise the main form is hidden when started from debugger
                    if (!_hideWindow) this.Activate();
                    this.TopMost = false;
                    //
                    string parameters = null;
                    bool scriptMode = _cmdOptions.ScriptFileName != null;
                    // Must be set before the first command, so that a script run leaves the recovery file
                    // of the interactive user untouched
                    if (scriptMode) _controller.RegenerationWorkDirectory = _cmdOptions.WorkDirectory;
                    // Try to recover unsaved progress due to crushed PrePoMax - a question for a
                    // person, so a session without a window is not asked: the unattended answer is the
                    // affirmative one and would regenerate the crash file of the interactive user
                    if (!scriptMode && !_hideWindow && File.Exists(_controller.GetHistoryFileNameBin()))
                    {
                        if (MessageBoxes.ShowWarningQuestionOKCancel("A recovery file from a previous PrePoMax session exists. " +
                                                                     "Would you like to try to recover it?") == DialogResult.OK)
                        {
                            fileName = _controller.GetHistoryFileNameBin();
                            //
                            bool regenerateTypeUsed;
                            try { regenerateTypeUsed = _controller.IsRegenerateTypeUsedByHistoryFile(fileName); }
                            catch { regenerateTypeUsed = true; }
                            //
                            if (regenerateTypeUsed)
                            {
                                DialogResult dialogResult = MessageBoxes.ShowWarningQuestionYesNoCancel("Regenerate All",
                                    "Select Yes to regenerate all commands or No to regenerate FE model commands only. " +
                                    "All commands can be regenerated later using Edit -> Regenerate All.");
                                if (dialogResult == DialogResult.Yes) parameters = Globals.RegenerateAll;
                                else if (dialogResult == DialogResult.Cancel) fileName = null;
                            }
                        }
                    }
                    if (fileName == null)
                    {
                        // Open file from exe arguments
                        if (_cmdOptions.FileName != null)
                        {
                            fileName = _cmdOptions.FileName;
                            //
                            if (_cmdOptions.UnitSystem != null)
                            {
                                if (!Enum.TryParse(_cmdOptions.UnitSystem.ToUpper(), out unitSystemType))
                                    throw new CaeException("The unit system type " + _cmdOptions.UnitSystem + " is not supported.");
                                parameters = _cmdOptions.UnitSystem;
                            }
                        }
                        // Check for open last file - for the same reason the recovery question above
                        // is not asked, a session without a window does not open it: the setting is a
                        // convenience for the person who last closed the application, and a hidden
                        // session an agent or a script drives would inherit that person's model and
                        // their whole command history without being told to
                        else if (!scriptMode && !_hideWindow && _controller.Settings.General.OpenLastFile)
                            fileName = _controller.OpenedFileName;
                    }
                    //
                    if (File.Exists(fileName))
                    {
                        fileName = Path.GetFullPath(fileName);  // change local file name to global
                        string extension = Path.GetExtension(fileName).ToLower();
                        HashSet<string> importExtensions = GetFileImportExtensions();
                        //
                        if (extension == ".pmx" || extension == ".pmh" || extension == ".frd")
                            await Task.Run(() => OpenAsync(fileName, _controller.Open, true, null, parameters));
                        else if (importExtensions.Contains(extension))
                        {
                            // Create new model
                            if (New(ModelSpaceEnum.ThreeD, unitSystemType))
                            {
                                // Import
                                await _controller.ImportFileAsync(fileName, false);
                                // Set to null, otherwise the previous OpenedFileName gets overwritten on Save
                                _controller.OpenedFileName = null;
                            }
                        }
                        else MessageBoxes.ShowError("The file name extension is not supported.");
                        //
                        SetFrontBackView(false, true);
                    }
                    else
                    {
                        _controller.CurrentView = ViewGeometryModelResults.Geometry;
                        //
                        UpdateRecentFilesThreadSafe();
                    }
                    // The automation server is started before this, so that a script can be its
                    // own client; from here on there is a model for a caller to read
                    _startupCompleted = true;
                    // Script - runs after the file defined by the -f switch was opened
                    if (scriptMode) await RunBatchScriptAsync();
                }
            }
            catch (Exception ex)
            {
                // Regeneration
                if (_controller.BatchRegenerationMode)
                {
                    throw new CaeException(ex.Message, ex);
                }
                else
                {
                    ExceptionTools.Show(this, ex);
                    _controller.ModelChanged = false;   // hide messageBox
                    tsmiNew_Click(null, null);
                }
            }
            finally
            {
                _startupCompleted = true;
                // must be here - otherwise the main form is hidden when started from debugger
                if (!_hideWindow) this.Activate();
                this.TopMost = false;
                //
                if (_isFirstRunInFolder)
                {
                    if (_controller.BatchRegenerationMode) WriteDataToOutput("Starting regeneration");
                    else _controller.Settings.CheckWorkingDirectory();
                }
            }
        }
        private async Task RunBatchScriptAsync()
        {
            WriteDataToOutput("Starting script: " + _cmdOptions.ScriptFileName);
            //
            string[] arguments = null;
            if (_cmdOptions.ScriptArguments != null)
                arguments = _cmdOptions.ScriptArguments.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            PythonEngineManager.SetScriptArguments(_cmdOptions.ScriptFileName, arguments);
            //
            string script = File.ReadAllText(_cmdOptions.ScriptFileName);
            PythonEngineResult result = null;
            await Task.Run(() => result = _controller.RunPythonScriptCommand(script, _cmdOptions.ScriptFileName, arguments));
            //
            if (result != null)
            {
                if (!string.IsNullOrEmpty(result.Output)) WriteDataToOutput(result.Output);
                if (!string.IsNullOrEmpty(result.Error)) throw new PythonScriptException(result.Error.TrimEnd());
            }
            // Save after the script command was recorded, so that the saved history can be regenerated
            if (_cmdOptions.SaveFileName != null)
            {
                WriteDataToOutput("Saving: " + _cmdOptions.SaveFileName);
                await Task.Run(() => _controller.SaveToPmxCommand(_cmdOptions.SaveFileName));
            }
            // Exit
            if (_cmdOptions.ExitAfterRegeneration == "Yes")
            {
                _closeAfterRegeneration = true;
                WriteDataToOutput("Close application.");
                Close();
            }
        }
        private void FrmMain_Resize(object sender, EventArgs e)
        {
            CommitViewportEdit();
            //
            if (this.WindowState == FormWindowState.Minimized && _frmAnimation != null && _frmAnimation.Visible)
                _frmAnimation.UpdateAnimation();
            //
            if (Debugger.IsAttached)
            {
                WriteDataToOutput(Width + " : " + Height);
            }
        }
        private async void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            string error = null;
            try
            {
                e.Cancel = false;   // close the form
                DialogResult response = DialogResult.None;
                // No write access
                if (_controller == null) return;
                // A hidden session has no window and nobody to answer its questions
                bool isUnattended = _controller.BatchRegenerationMode || _hideWindow;
                // Analysis in progress - first check: it has to kill running analyses
                foreach (var entry in _controller.Jobs)
                {
                    if (entry.Value.JobStatus == JobStatus.Running)
                    {
                        if (!isUnattended)
                            response = MessageBoxes.ShowWarningQuestionOKCancel("There is an analysis running. " +
                                "Closing will kill the analysis. Close anyway?");
                        else response = DialogResult.OK;
                        //
                        if (response == DialogResult.Cancel) e.Cancel = true;
                        else if (response == DialogResult.OK)
                        {
                            _controller.KillAllJobs();
                            error = "The analysis was killed by the user.";
                            throw new CaeException(error);
                        }
                        break;
                    }
                }
                // Regeneration in progress
                if (_controller.BatchRegenerationMode && !_closeAfterRegeneration)
                {
                    error = "PrePoMax closed by the user.";
                    throw new CaeException(error);
                }
                // Saving in progress
                if (tsslState.Text != Globals.ReadyText)
                {
                    if (!isUnattended)
                        response = MessageBoxes.ShowWarningQuestionOKCancel("There is a task running. Close anyway?");
                    else response = DialogResult.OK;
                    //
                    if (response == DialogResult.Cancel) e.Cancel = true;
                    else if (response == DialogResult.OK && _controller.SavingFile)
                    {
                        // Wait for saving to finish
                        while (_controller.SavingFile) Thread.Sleep(100);
                    }
                }
                // Model changed
                else if (_controller.ModelChanged)
                {
                    if (!isUnattended)
                        response = MessageBoxes.ShowWarningQuestionYesNoCancel("Save file before closing?");
                    else response = DialogResult.No;
                    //
                    if (response == DialogResult.Yes)
                    {
                        e.Cancel = true;            // stop the form from closing before saving
                        await SaveAsync();          // save
                        Close();                    // close the control
                    }
                    else if (response == DialogResult.Cancel) e.Cancel = true;
                }
                // Save form size and location and delete history files
                if (e.Cancel == false && _controller != null)
                {
                    StopAutomationServer();
                    //
                    if (!_controller.BatchRegenerationMode)
                    {
                        SettingsContainer settings = _controller.Settings;  // get a clone
                        settings.General.SaveFormSize(this);                // save form size
                        _controller.Settings = settings;                    // update values and save to file
                        //
                        _controller.DeleteHistoryFiles();
                        //
                        _vtk.RenderingOn = false;       // the viewport is being destroyed; do not queue redraws while clearing it
                        _vtk.Clear();
                        _vtk.Dispose();
                        _vtk = null;
                    }
                }
            }
            catch
            { 
                if (error != null) throw new CaeException(error);
            }
        }
        private void FrmMain_Move(object sender, EventArgs e)
        {
            if (_allForms == null) return;

            foreach (Form form in _allForms)
            {
                if (form.Visible)
                {
                    //form.Location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
                }
            }
        }
        private void UpdateVtkControlSize()
        {
            // Update vtk control size
            Rectangle bounds = panelControl.Bounds;
            bounds.Inflate(-2, -2);
            if (_vtk.Bounds != bounds) _vtk.Bounds = bounds;
        }
        private bool TestWriteAccess()
        {
            try
            {
                string fileName = Tools.GetNonExistentRandomFileName(Application.StartupPath, ".test");
                File.WriteAllText(fileName, "");
                //
                File.Delete(fileName);
                //
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Forms
        private void itemForm_VisibleChanged(object sender, EventArgs e)
        {
            Form form = sender as Form;
            bool menusActive = !form.Visible;
            //
            if (form is FrmSelectItemSet) return;
            //
            if (form is FrmAnimation)
            {
                SetMenuAndToolStripVisibilityByAnimation(!form.Visible);
            }
            else
            {
                SetMenuAndToolStripVisibilityByItemForm(menusActive);
            }
            // This gets also called from item selection form: by angle, by edge ...
            if (menusActive)
            {
                UpdateHighlightFromTree();
                SaveFormLocation(form);
                //
                _controller.SetSelectByToDefault();
                //
                Focus();
            }
        }
        private void itemForm_Move(object sender, EventArgs e)
        {
            Form form = sender as Form;
            //Size screenSize =  Screen.GetWorkingArea(form).Size;
            //if (form.Left < 0) form.Left = 0;
            //else if (form.Left + form.Width > screenSize.Width) form.Left = screenSize.Width - form.Width;
            //if (form.Top < 0) form.Top = 0;
            //else if (form.Top + form.Height > screenSize.Height) form.Top = screenSize.Height - form.Height;
            SaveFormLocation(form);
        }
        // Keyboard
        private void KeyboardHook_KeyDown(KeyboardHook.VKeys vKey)
        {
            if (this != ActiveForm) return;
            if (tbOutput.Focused) return;
            if (_modelTree.ContainsFocus) return;
            if (_visibleSidePanel != null && _visibleSidePanel.ContainsFocus) return;
            //
            Keys key = (Keys)vKey;
            if (viewportTextEditor.Visible)
            {
                if (key == Keys.Escape) CancelViewportEdit();
                return;
            }
            if (key == Keys.Escape)
            {
                if (!_vtk.IsBoxSelectionActive) CloseAllForms();
                return;
            }
            // Check for toolstrip
            Control focusedControl = FindFocusedControl(this);
            if (focusedControl != null && focusedControl.Parent is ToolStripFocus) return;
            // Model tree
            if (_modelTree.ActiveControl != null && _modelTree.ActiveControl.Focused) return;
            // Check for model tree search box
            if (_modelTree.ActiveControl != null && _modelTree.ActiveControl is SearchTextBox) return;
            // Model tree edit mode active
            if (_modelTree.EditModeActive) return;
            // Forward to tree
            _modelTree.cltv_KeyDown(this, new KeyEventArgs(key));
            // Prevent that the coders lab tree calls _modelTree.cltv_KeyDown again
            _vtk.Focus();
        }
        public static Control FindFocusedControl(Control control)
        {
            var container = control as IContainerControl;
            while (container != null)
            {
                control = container.ActiveControl;
                container = control as IContainerControl;
            }
            return control;
        }
        //
        private void timerOutput_Tick(object sender, EventArgs e)
        {
            timerOutput.Stop();
            //
            UpdateOutputTextBox();
        }
        private void tbOutput_LinkClicked(string line, int position)
        {
            try
            {
                // Following a name changes the view and clears the selection, which would pull the ground from under
                // a running operation or an open dialog
                if (IsStateWorking()) return;
                if (_allForms != null)
                {
                    foreach (Form form in _allForms)
                    {
                        if (form.Visible) return;
                    }
                }
                if (!_controller.HighlightGeometryNamedAt(line, position))
                    _controller.HighlightInvertedElementsNamedAt(line, position);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        #region ModelTree Events ###################################################################################################
        //
        internal void ModelTree_ViewEvent(ViewType viewType)
        {
            try
            {
                if ((viewType == ViewType.Geometry && GetCurrentView() == ViewGeometryModelResults.Geometry) ||
                    (viewType == ViewType.Model && GetCurrentView() == ViewGeometryModelResults.Model) ||
                    (viewType == ViewType.Results && GetCurrentView() == ViewGeometryModelResults.Results)) return;
                //
                CloseAllForms();
                _controller.SelectBy = vtkSelectBy.Default;
                //
                if (viewType == ViewType.Geometry) _controller.CurrentView = ViewGeometryModelResults.Geometry;
                else if (viewType == ViewType.Model) _controller.CurrentView = ViewGeometryModelResults.Model;
                else if (viewType == ViewType.Results) _controller.CurrentView = ViewGeometryModelResults.Results;
                else throw new NotSupportedException();
                //
                _visibleSidePanel?.PrepareControls(viewType);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ModelTree_SelectEvent(NamedClass[] items, bool mouseOver)
        {
            try
            {
                _controller.Highlight3DObjects(items, mouseOver);
            }
            catch { }
        }
        //
        private void ModelTree_CreateEvent(string nodeName, string stepName)
        {
            if (_controller.Model.Geometry != null && _controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                if (nodeName == _modelTree.MeshSetupItemsName) tsmiCreateMeshSetupItem_Click(null, null);
            }
            else if (_controller.Model.Mesh != null && _controller.CurrentView == ViewGeometryModelResults.Model)
            {
                // _controller.Model.Mesh defines the unit system and must
                if (nodeName == _modelTree.NodeSetsName) tsmiCreateNodeSet_Click(null, null);
                else if (nodeName == _modelTree.ElementSetsName) tsmiCreateElementSet_Click(null, null);
                else if (nodeName == _modelTree.SurfacesName) tsmiCreateSurface_Click(null, null);
                else if (nodeName == _modelTree.ModelReferencePointsName) tsmiCreateModelReferencePoint_Click(null, null);
                else if (nodeName == _modelTree.ModelCoordinateSystemsName) tsmiCreateModelCoordinateSystem_Click(null, null);
                else if (nodeName == _modelTree.MaterialsName) tsmiCreateMaterial_Click(null, null);
                else if (nodeName == _modelTree.SectionsName) tsmiCreateSection_Click(null, null);
                else if (nodeName == _modelTree.ConstraintsName) tsmiCreateConstraint_Click(null, null);
                else if (nodeName == _modelTree.SurfaceInteractionsName) tsmiCreateSurfaceInteraction_Click(null, null);
                else if (nodeName == _modelTree.ContactPairsName) tsmiCreateContactPair_Click(null, null);
                else if (nodeName == _modelTree.DistributionsName) tsmiCreateDistribution_Click(null, null);
                else if (nodeName == _modelTree.AmplitudesName) tsmiCreateAmplitude_Click(null, null);
                else if (nodeName == _modelTree.InitialConditionsName) tsmiCreateInitialCondition_Click(null, null);
                else if (nodeName == _modelTree.StepsName) tsmiCreateStep_Click(null, null);
                else if (nodeName == _modelTree.ModelHistoryOutputsName && stepName != null) CreateHistoryOutput(stepName);
                else if (nodeName == _modelTree.ModelFieldOutputsName && stepName != null) CreateFieldOutput(stepName);
                else if (nodeName == _modelTree.BoundaryConditionsName && stepName != null) CreateBoundaryCondition(stepName);
                else if (nodeName == _modelTree.LoadsName && stepName != null) CreateLoad(stepName);
                else if (nodeName == _modelTree.DefinedFieldsName && stepName != null) CreateDefinedField(stepName);
                else if (nodeName == _modelTree.AnalysesName) tsmiCreateAnalysis_Click(null, null);
            }
            else if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null &&
                     _controller.CurrentView == ViewGeometryModelResults.Results)
            {
                if (nodeName == _modelTree.ResultReferencePointsName) tsmiCreateResultReferencePoint_Click(null, null);
                else if (nodeName == _modelTree.ResultCoordinateSystemsName) tsmiCreateResultCoordinateSystem_Click(null, null);
                else if (nodeName == _modelTree.ResultFieldOutputsName) tsmiCreateResultFieldOutput_Click(null, null);
                else if (nodeName == _modelTree.ResultHistoryOutputsName) tsmiCreateResultHistoryOutput_Click(null, null);
            }
        }
        private void ModelTree_EditEvent(NamedClass namedClass, string stepName)
        {
            // Geometry
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                if (namedClass is GeometryPart) EditGeometryPart(namedClass.Name);
                else if (namedClass is MeshSetupItem) EditMeshSetupItem(namedClass.Name);
            }
            // Model
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                if (namedClass is EmptyNamedClass) // empty named class is used to transfer the name only
                {
                    if (namedClass.Name == typeof(FeModel).ToString()) tsmiEditModel_Click(null, null);
                }
                else if (namedClass is MeshPart) EditModelPart(namedClass.Name);
                else if (namedClass is FeNodeSet) EditNodeSet(namedClass.Name);
                else if (namedClass is FeElementSet) EditElementSet(namedClass.Name);
                else if (namedClass is FeSurface) EditSurface(namedClass.Name);
                else if (namedClass is FeReferencePoint) EditModelReferencePoint(namedClass.Name);
                else if (namedClass is CoordinateSystem) EditModelCoordinateSystem(namedClass.Name);
                else if (namedClass is Material) EditMaterial(namedClass.Name);
                else if (namedClass is Section) EditSection(namedClass.Name);
                else if (namedClass is CaeModel.Constraint) EditConstraint(namedClass.Name);
                else if (namedClass is SurfaceInteraction) EditSurfaceInteraction(namedClass.Name);
                else if (namedClass is ContactPair) EditContactPair(namedClass.Name);
                else if (namedClass is Distribution) EditDistribution(namedClass.Name);
                else if (namedClass is Amplitude) EditAmplitude(namedClass.Name);
                else if (namedClass is InitialCondition) EditInitialCondition(namedClass.Name);
                else if (namedClass is Step) EditStep(namedClass.Name);
                else if (namedClass is HistoryOutput) EditHistoryOutput(stepName, namedClass.Name);
                else if (namedClass is FieldOutput) EditFieldOutput(stepName, namedClass.Name);
                else if (namedClass is BoundaryCondition) EditBoundaryCondition(stepName, namedClass.Name);
                else if (namedClass is Load) EditLoad(stepName, namedClass.Name);
                else if (namedClass is DefinedField) EditDefinedField(stepName, namedClass.Name);
                else if (namedClass is AnalysisJob) EditAnalysis(namedClass.Name);
            }
            // Results
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                if (namedClass is ResultPart || namedClass is GeometryPart) EditResultPart(namedClass.Name);
                else if (namedClass is FeReferencePoint) EditResultReferencePoint(namedClass.Name);
                else if (namedClass is CoordinateSystem) EditResultCoordinateSystem(namedClass.Name);
                else if (namedClass is ResultFieldOutput rfo) EditResultFieldOutput(rfo.Name);
                else if (namedClass is ResultHistoryOutput rho) EditResultHistoryOutput(rho.Name);
                else if (namedClass is HistoryResultData hd) ViewResultHistoryOutputData(hd);
                else if (namedClass is FieldData) ShowLegendSettings();
            }
        }
        private void ModelTree_RenameEvent(NamedClass item, string newName, string stepName)
        {
            _controller.RenameCommand(item, newName, stepName);
        }
        private void ModelTree_RenameSplitSubPartEvent(string compoundPartName, string oldSubPartName,
                                                       string newSubPartName)
        {
            _controller.RenameSplitSubPartCommand(compoundPartName, oldSubPartName, newSubPartName);
        }
        private void ModelTree_GroupSplitCellsEvent(string compoundPartName, string[] cellNames)
        {
            GroupSplitCellsAndSelect(compoundPartName, cellNames);
        }
        private void ModelTree_FuseBodiesEvent(string compoundPartName, string[] cellNames)
        {
            FuseBodiesAndSelect(compoundPartName, cellNames);
        }
        private void ModelTree_QueryEvent()
        {
            tsmiQuery_Click(null, null);
        }
        private void ModelTree_DuplicateEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                ApplyActionOnItems<MeshSetupItem>(items, DuplicateMeshSetupItems);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<FeNodeSet>(items, DuplicateNodeSets);
                ApplyActionOnItems<FeElementSet>(items, DuplicateElementSets);
                ApplyActionOnItems<FeSurface>(items, DuplicateSurfaces);
                ApplyActionOnItems<FeReferencePoint>(items, DuplicateModelReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DuplicateModelCoordinateSystems);
                //
                ApplyActionOnItems<Material>(items, DuplicateMaterials);
                ApplyActionOnItems<Section>(items, DuplicateSections);
                ApplyActionOnItems<CaeModel.Constraint>(items, DuplicateConstraints);
                ApplyActionOnItems<SurfaceInteraction>(items, DuplicateSurfaceInteractions);
                ApplyActionOnItems<ContactPair>(items, DuplicateContactPairs);
                ApplyActionOnItems<Distribution>(items, DuplicateDistributions);
                ApplyActionOnItems<Amplitude>(items, DuplicateAmplitudes);
                ApplyActionOnItems<InitialCondition>(items, DuplicateInitialConditions);
                //
                ApplyActionOnItems<Step>(items, DuplicateSteps);
                ApplyActionOnItemsInStep<HistoryOutput>(items, stepNames, DuplicateHistoryOutputs);
                ApplyActionOnItemsInStep<FieldOutput>(items, stepNames, DuplicateFieldOutputs);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, stepNames, DuplicateBoundaryConditions);
                ApplyActionOnItemsInStep<Load>(items, stepNames, DuplicateLoads);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, DuplicateDefinedFields);
                //
                ApplyActionOnItems<AnalysisJob>(items, DuplicateAnalyses);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                ApplyActionOnItems<FeReferencePoint>(items, DuplicateResultReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DuplicateResultCoordinateSystems);
                ApplyActionOnItems<ResultFieldOutput>(items, DuplicateResultFieldOutputs);
                ApplyActionOnItems<ResultHistoryOutput>(items, DuplicateResultHistoryOutputs);
            }
        }
        private void ModelTree_PropagateEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItemsInStep<HistoryOutput>(items, stepNames, PropagateHistoryOutput);
                ApplyActionOnItemsInStep<FieldOutput>(items, stepNames, PropagateFieldOutput);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, stepNames, PropagateBoundaryCondition);
                ApplyActionOnItemsInStep<Load>(items, stepNames, PropagateLoad);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, PropagateDefinedField);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
            }
        }
        private void ModelTree_PreviewEvent(NamedClass[] items, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<InitialCondition>(items, PreviewInitialConditions);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, stepNames, PreviewBoundaryCondition);
                ApplyActionOnItemsInStep<Load>(items, stepNames, PreviewLoad);
                ApplyActionOnItemsInStep<DefinedField>(items, stepNames, PreviewDefinedField);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
            }
        }
        //
        private void ModelTree_SwapMasterSlave(NamedClass[] items)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<CaeModel.Constraint>(items, SwapMasterSlaveConstraints);
                ApplyActionOnItems<ContactPair>(items, SwapMasterSlaveContactPairs);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            { }
        }
        private void ModelTree_MergeByMasterSlave(NamedClass[] items)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            { }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<CaeModel.Constraint>(items, MergeByMasterSlaveConstraints);
                ApplyActionOnItems<ContactPair>(items, MergeByMasterSlaveContactPairs);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            { }
        }
        //
        private void ModelTree_HideShowEvent(NamedClass[] items, HideShowOperation operation, string[] stepNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                HideShowItems<GeometryPart>(items, operation, HideGeometryParts, ShowGeometryParts, ShowOnlyGeometryParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                HideShowItems<MeshPart>(items, operation, HideModelParts, ShowModelParts, ShowOnlyModelParts);
                HideShowItems<FeReferencePoint>(items, operation, HideModelReferencePoints,
                                                ShowModelReferencePoints, ShowOnlyModelReferencePoints);
                HideShowItems<CoordinateSystem>(items, operation, HideModelCoordinateSystems,
                                                ShowModelCoordinateSystems, ShowOnlyModelCoordinateSystems);
                HideShowItems<CaeModel.Constraint>(items, operation, HideConstraints, ShowConstraints, ShowOnlyConstraints);
                HideShowItems<ContactPair>(items, operation, HideContactPairs, ShowContactPairs, ShowOnlyContactPairs);
                HideShowItems<InitialCondition>(items, operation, HideInitialConditions, ShowInitialConditions,
                                                ShowOnlyInitialConditions);
                HideShowStepItems<BoundaryCondition>(items, operation, stepNames, HideBoundaryConditions,
                                                     ShowBoundaryConditions, ShowOnlyBoundaryConditions);
                HideShowStepItems<Load>(items, operation, stepNames, HideLoads, ShowLoads, ShowOnlyLoads);
                HideShowStepItems<DefinedField>(items, operation, stepNames, HideDefinedFields,
                                                ShowDefinedFields, ShowOnlyDefinedFields);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                HideShowItems<ResultPart>(items, operation, HideResultParts, ShowResultParts, ShowOnlyResultParts);
                HideShowItems<GeometryPart>(items, operation, HideResultParts, ShowResultParts, ShowOnlyResultParts);
                HideShowItems<FeReferencePoint>(items, operation, HideResultReferencePoints,
                                                ShowResultReferencePoints, ShowOnlyResultReferencePoints);
                HideShowItems<CoordinateSystem>(items, operation, HideResultCoordinateSystems,
                                                ShowResultCoordinateSystems, ShowOnlyResultCoordinateSystems);
            }
        }
        private void ModelTree_SetPartColorEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) SetColorForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) SetColorForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) SetColorForResultParts(partNames);
        }
        private void ModelTree_ResetPartColorEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) ResetColorForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) ResetColorForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) ResetColorForResultParts(partNames);
        }
        private void ModelTree_SetTransparencyEvent(string[] partNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) SetTransparencyForGeometryParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) SetTransparencyForModelParts(partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results) SetTransparencyForResultParts(partNames);
        }
        private void ModelTree_ResultRepresentationVisibilityEvent(NamedClass[] items, bool resultRepresentation)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                names.Add(items[i].Name);
            }
            //
            if (names.Count > 0)
            {
                if (resultRepresentation) ResultRepresentationOnResultPart(names.ToArray());
                else ResultRepresentationOffResultPart(names.ToArray());
            }
        }
        //
        private void ModelTree_DeleteEvent(NamedClass[] items, string[] parentNames)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
            {
                ApplyActionOnItems<MeshSetupItem>(items, DeleteMeshSetupItems);
                // At last delete the parts
                ApplyActionOnItems<GeometryPart>(items, DeleteGeometryParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
            {
                ApplyActionOnItems<FeNodeSet>(items, DeleteNodeSets);
                ApplyActionOnItems<FeElementSet>(items, DeleteElementSets);
                ApplyActionOnItems<FeSurface>(items, DeleteSurfaces);
                ApplyActionOnItems<FeReferencePoint>(items, DeleteModelReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DeleteModelCoordinateSystems);
                ApplyActionOnItems<Material>(items, DeleteMaterials);
                ApplyActionOnItems<Section>(items, DeleteSections);
                ApplyActionOnItems<CaeModel.Constraint>(items, DeleteConstraints);
                ApplyActionOnItems<SurfaceInteraction>(items, DeleteSurfaceInteractions);
                ApplyActionOnItems<ContactPair>(items, DeleteContactPairs);
                ApplyActionOnItems<Distribution>(items, DeleteDistributions);
                ApplyActionOnItems<Amplitude>(items, DeleteAmplitudes);
                ApplyActionOnItems<InitialCondition>(items, DeleteInitialConditions);
                // First delete step items and then steps
                ApplyActionOnItemsInStep<HistoryOutput>(items, parentNames, DeleteHistoryOutputs);
                ApplyActionOnItemsInStep<FieldOutput>(items, parentNames, DeleteFieldOutputs);
                ApplyActionOnItemsInStep<BoundaryCondition>(items, parentNames, DeleteBoundaryConditions);
                ApplyActionOnItemsInStep<Load>(items, parentNames, DeleteLoads);
                ApplyActionOnItemsInStep<DefinedField>(items, parentNames, DeleteDefinedFields);
                ApplyActionOnItems<Step>(items, DeleteSteps);
                //
                ApplyActionOnItems<AnalysisJob>(items, DeleteAnalyses);
                // At last delete the parts
                ApplyActionOnItems<MeshPart>(items, DeleteModelParts);
            }
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                ApplyActionOnItems<ResultPart>(items, DeleteResultParts);
                ApplyActionOnItems<GeometryPart>(items, DeleteResultParts);
                ApplyActionOnItems<FeReferencePoint>(items, DeleteResultReferencePoints);
                ApplyActionOnItems<CoordinateSystem>(items, DeleteResultCoordinateSystems);
                // First delete components and then field outputs
                ApplyActionOnItemsInStep<FieldData>(items, parentNames, DeleteResultFieldOutputComponents);
                ApplyActionOnItems<Field>(items, DeleteResultFieldOutputs);
                ApplyActionOnItems<ResultFieldOutput>(items, DeleteResultFieldOutputs);
                //
                DeleteResultHistoryComponents(items);
                ApplyActionOnItemsInStep<HistoryResultField>(items, parentNames, DeleteResultHistoryFields);
                ApplyActionOnItems<HistoryResultSet>(items, DeleteResultHistoryOutputs);
                ApplyActionOnItems<ResultHistoryOutput>(items, DeleteResultHistoryOutputs);
            }
        }
        private void ModelTree_ActivateDeactivateEvent(NamedClass[] items, bool activate, string[] stepNames)
        {
            _controller.ActivateDeactivateMultipleCommand(items, activate, stepNames);
        }
        private void ModelTree_OrderedDictionaryTreeLayoutChangeEvent(OrderedDictionaryTreeLayoutChange change)
        {
            _controller.ChangeOrderedDictionaryItemLayoutCommand(change);
        }
        private void ModelTree_MoveStepsEvent(string[] stepNames, int targetIndex)
        {
            _controller.MoveStepsCommand(stepNames, targetIndex);
        }
        private void ModelTree_FieldDataSelectEvent(string[] obj)
        {
            try
            {
                SetFieldData(obj[0], obj[1], GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            }
            catch { }
        }
        //                                                                                                                          
        private void HideShowItems<T>(NamedClass[] items, HideShowOperation operation, Action<string[]> Hide,
                                      Action<string[]> Show, Action<string[]> ShowOnly)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) names.Add(items[i].Name);
            }
            if (names.Count > 0)
            {
                if (operation == HideShowOperation.Hide) Hide(names.ToArray());
                else if (operation == HideShowOperation.Show) Show(names.ToArray());
                else if (operation == HideShowOperation.ShowOnly) ShowOnly(names.ToArray());
                else throw new NotSupportedException();
            }
        }
        private void HideShowStepItems<T>(NamedClass[] items, HideShowOperation operation, string[] stepNames,
                                          Action<string, string[]> Hide,
                                          Action<string, string[]> Show,
                                          Action<string, string[]> ShowOnly)
        {
            Dictionary<string, List<string>> stepItems = new Dictionary<string, List<string>>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                {
                    if (stepItems.ContainsKey(stepNames[i])) stepItems[stepNames[i]].Add(items[i].Name);
                    else stepItems.Add(stepNames[i], new List<string>() { items[i].Name });
                }
            }
            if (stepItems.Count > 0)
            {
                foreach (var entry in stepItems)
                {
                    if (operation == HideShowOperation.Hide) Hide(entry.Key, entry.Value.ToArray());
                    else if (operation == HideShowOperation.Show) Show(entry.Key, entry.Value.ToArray());
                    else if (operation == HideShowOperation.ShowOnly) ShowOnly(entry.Key, entry.Value.ToArray());
                    else throw new NotSupportedException();
                }
            }
        }
        private void ApplyActionOnItems<T>(NamedClass[] items, Action<string[]> Action)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) names.Add(items[i].Name);
            }
            if (names.Count > 0) Action(names.ToArray());
        }
        private void ApplyActionOnItemsInStep<T>(NamedClass[] items, string[] steps, Action<string, string> Action)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T) Action(steps[i], items[i].Name);
            }
        }
        private void ApplyActionOnItemsInStep<T>(NamedClass[] items, string[] parentNames, Action<string, string[]> Action)
        {
            List<string> itemList;
            Dictionary<string, List<string>> parentItems = new Dictionary<string, List<string>>();
            //
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] is T)
                {
                    if (parentItems.TryGetValue(parentNames[i], out itemList)) itemList.Add(items[i].Name);
                    else parentItems.Add(parentNames[i], new List<string>() { items[i].Name });
                }
            }
            if (parentItems.Count > 0)
            {
                foreach (var entry in parentItems)
                {
                    Action(entry.Key, entry.Value.ToArray());
                }
            }
        }
        //                                                                                                                          

        #endregion #################################################################################################################


        // Menus                                                                                                                    
        private void SetMenuAndToolStripVisibility()
        {
            if (IsStateRegeneratingOrUndoing()) return;
            //
            InvokeIfRequired(() =>
            {
                //                      Disable                                                         
                // Main menu
                foreach (ToolStripMenuItem item in menuStripMain.Items) item.Enabled = false;
                tsmiFile.Enabled = true;
                tsmiResults.Enabled = true;
                tsmiTools.Enabled = true;
                tsmiHelp.Enabled = true;
                // File menu
                foreach (ToolStripItem item in tsmiFile.DropDownItems) item.Enabled = false;
                // View menu
                tsmiFollowerView.Enabled = false;
                tsbFollowerView.Enabled = false;
                // Tools menu
                tsmiParameters.Enabled = false;
                tsmiQuery.Enabled = false;
                tsmiArrangeAnnotations.Enabled = false;
                tsmiRemoveAnnotations.Enabled = false;
                tsmiFind.Enabled = false;
                tsmiCaptureViewport.Enabled = false;
                tsmiCaptureViewportAs.Enabled = false;
                // Results menu
                foreach (ToolStripItem item in tsmiResults.DropDownItems) item.Enabled = false;
                tsmiConvertOdbToFrd.Enabled = true;
                // Toolbar File
                tsbImport.Enabled = false;
                tsbSave.Enabled = false;
                // Toolbar View
                tsViews.DisableMouseButtons = true;
                // Toolbar Symbols
                tslColorBy.Enabled = false;
                tscbColorBy.Enabled = false;
                tslSymbols.Enabled = false;
                tscbSymbols.Enabled = false;
                UpdateSymbolsList();
                // Toolbar Results
                tsResultDeformation.Enabled = false;
                tsResults.Enabled = false;
                // Vtk
                bool vtkVisible = false;
                // Tree
                SetStateWorking(Globals.RenderingText);
                //                      Enable                                                          
                if (_controller.ModelInitialized || _controller.ResultsInitialized)
                {
                    // Main menu
                    tsmiEdit.Enabled = true;
                    // File menu
                    foreach (ToolStripItem item in tsmiFile.DropDownItems) item.Enabled = true;
                    tsmiImportFile.Enabled = _controller.ModelInitialized;
                    // Tools menu
                    tsmiParameters.Enabled = true;
                    tsmiQuery.Enabled = true;
                    tsmiFind.Enabled = true;
                    tsmiCaptureViewport.Enabled = true;
                    tsmiCaptureViewportAs.Enabled = true;
                    // Toolbar File
                    tsbImport.Enabled = _controller.ModelInitialized;
                    tsbSave.Enabled = true;
                }
                else
                {
                    // File menu
                    tsmiNew.Enabled = true;
                    tsmiOpen.Enabled = true;
                    tsmiRunHistoryFile.Enabled = true;
                    tsmiOpenRecent.Enabled = true;
                    tsmiExit.Enabled = true;
                }
                //
                bool setGeometryView = _controller.CurrentView == ViewGeometryModelResults.Geometry;
                bool setModelView = _controller.CurrentView == ViewGeometryModelResults.Model;
                bool setResultsView = _controller.CurrentView == ViewGeometryModelResults.Results;
                bool setEmptyView = (setGeometryView && !_controller.ModelInitialized) ||
                                    (setModelView && !_controller.ModelInitialized) ||
                                    (setResultsView && !_controller.ResultsInitialized);

                // Only for individual views !!!
                if (setEmptyView) { }
                else if (setGeometryView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    tsmiGeometry.Enabled = true;
                    tsmiMesh.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Toolbar Symbols
                    tslColorBy.Enabled = true;
                    tscbColorBy.Enabled = true;
                    // Vtk
                    vtkVisible = true;
                }
                else if (setModelView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    tsmiModel.Enabled = true;
                    tsmiProperty.Enabled = true;
                    tsmiInteraction.Enabled = true;
                    tsmiInitialCondition.Enabled = true;
                    tsmiDistribution.Enabled = true;
                    tsmiAmplitude.Enabled = true;
                    tsmiStepMenu.Enabled = true;
                    tsmiBC.Enabled = true;
                    tsmiLoad.Enabled = true;
                    tsmiAnalysis.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Toolbar Symbols
                    tslColorBy.Enabled = true;
                    tscbColorBy.Enabled = true;
                    tslSymbols.Enabled = true;
                    tscbSymbols.Enabled = true;
                    // Vtk
                    vtkVisible = true;
                }
                else if (setResultsView)
                {
                    // Main menu
                    tsmiView.Enabled = true;
                    // View menu
                    tsmiFollowerView.Enabled = true;
                    tsbFollowerView.Enabled = true;
                    // Results menu
                    foreach (ToolStripItem item in tsmiResults.DropDownItems) item.Enabled = true;
                    // Toolbar View
                    tsViews.DisableMouseButtons = false;
                    // Toolbar Symbols
                    tslColorBy.Enabled = true;
                    tscbColorBy.Enabled = true;
                    tslSymbols.Enabled = true;
                    tscbSymbols.Enabled = true;
                    // Toolbar Results
                    tsResultDeformation.Enabled = true;
                    tsResults.Enabled = true;
                    // Vtk
                    vtkVisible = true;
                }
                // Tree
                SetStateReady(Globals.RenderingText);
                //                      Buttons                                                         
                tsbSectionView.Checked = _controller.IsSectionViewActive();
                tsbExplodedView.Checked = _controller.IsExplodedViewActive();
                tsbFollowerView.Checked = _controller.IsFollowerViewActive();
                tsbTransformation.Checked = _controller.AreTransformationsActive();
                //                      Icons                                                           
                UpdateResultsTypeIconStates();
                //
                UpdateComplexControlStates();
                //
                _vtk.Visible = vtkVisible;
            });
        }
        private void SetMenuAndToolStripVisibilityByItemForm(bool menusActive)
        {
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            tsFile.DisableMouseButtons = !menusActive;
            tsSymbols.Enabled = menusActive; // changing the symbols clears the selection - unwanted during selection
        }
        private void SetMenuAndToolStripVisibilityBySetState(bool menusActive)
        {
            // Do not use Enable here - use DisableMouseButtons
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            tsFile.DisableMouseButtons = !menusActive;
            tsViews.DisableMouseButtons = !menusActive;
            tsResultDeformation.DisableMouseButtons = !menusActive;
            tsResults.DisableMouseButtons = !menusActive;
        }
        private void SetMenuAndToolStripVisibilityByAnimation(bool menusActive)
        {
            if (!menusActive)
            {
                CancelViewportEdit();
                cmsAnnotation.Close();
                cmsResultWidget.Close();
            }
            //
            _vtk.SetWidgetInteractionEnabled(menusActive);
            //
            _modelTree.DisableMouse = !menusActive;
            menuStripMain.DisableMouseButtons = !menusActive;
            // DisableMouseButtons does not work for combo boxes so use .Enabled
            tsFile.Enabled = menusActive;
            tsSymbols.Enabled = menusActive;
            tsResultDeformation.Enabled = menusActive;
            //
            tsbAnimate.Enabled = menusActive;   // first - removes the selection
            tsResults.Enabled = menusActive;    // second
            // Individual buttons
            tsbSectionView.Enabled = menusActive;
            tsbExplodedView.Enabled = menusActive;
            tsbFollowerView.Enabled = menusActive;
            tsbQuery.Enabled = menusActive;
            tsbRemoveAnnotations.Enabled = menusActive;
            tsbCapture.Enabled = menusActive;
            tsbShowAllParts.Enabled = menusActive;
            tsbHideAllParts.Enabled = menusActive;
            tsbInvertVisibleParts.Enabled = menusActive;
            //
            if (menusActive) UpdateComplexControlStates();
            
        }
        private void PushMenuStates()
        {
            List<bool> states = new List<bool>();
            states.Add(_modelTree.DisableMouse);                    // SetMenuAndToolStripVisibilityBySetState
            states.Add(menuStripMain.DisableMouseButtons);          // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsFile.DisableMouseButtons);                 // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsViews.DisableMouseButtons);                // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsResultDeformation.DisableMouseButtons);    // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsResults.DisableMouseButtons);              // SetMenuAndToolStripVisibilityBySetState
            states.Add(tsSymbols.Enabled);                          // SetMenuAndToolStripVisibilityByItemForm
            //
            _prevMenuStates.Push(states.ToArray());
        }
        private void PopMenuStates()
        {
            if (_prevMenuStates != null && _prevMenuStates.Count > 0)
            {
                int n = 0;
                bool[] states = _prevMenuStates.Pop();
                //
                _modelTree.DisableMouse = states[n++];
                menuStripMain.DisableMouseButtons = states[n++];
                tsFile.DisableMouseButtons = states[n++];
                tsViews.DisableMouseButtons = states[n++];
                tsResultDeformation.DisableMouseButtons = states[n++];
                tsResults.DisableMouseButtons = states[n++];
                tsSymbols.Enabled = states[n++];
            }
            //else throw new NotSupportedException();
        }

        #region File menu ##########################################################################################################
        internal void tsmiNew_Click(object sender, EventArgs e)
        {
            New(ModelSpaceEnum.Undefined, UnitSystemType.Undefined);
        }
        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = GetFileOpenFilter();
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        TryOpen(openFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRunHistoryFile_Click(object sender, EventArgs e)
        {
            // This adds commands to the history - it is not the same as Open
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PrePoMax history|*.pmh";
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK) RunHistoryFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiImportFile_Click(object sender, EventArgs e)
        {
            ImportFile(false);
        }        
        internal async void tsmiSave_Click(object sender, EventArgs e)
        {
            try
            {
                await SaveAsync();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized && !_controller.ResultsInitialized)
                    throw new CaeException("There is no model or results to save.");
                //
                SetStateWorking(Globals.SavingAsText);
                await Task.Run(() => _controller.SaveAs());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.SavingAsText);
            }
        }
        //
        private void tsmiExportGeometry_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Geometry;
                //
                if (_controller.Model.Geometry != null && _controller.Model.Geometry.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ExportGeometryParts);
                }
                else throw new CaeException("No geometry to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportMesh_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                if (_controller.Model.Mesh != null && _controller.Model.Mesh.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetModelParts(), ExportMeshParts);
                }
                else throw new CaeException("No mesh to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportModel_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                if (CheckValidity()) ExportModel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportDeformedParts_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Results;
                //
                if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null &&
                    _controller.CurrentResult.Mesh.Parts != null)
                {
                    SelectMultipleEntities("Parts", _controller.GetResultParts(), ExportResultParts);
                }
                else throw new CaeException("No results to export.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportTo3mf_Click(object sender, EventArgs e)
        {
            try
            {
                ShowForm(_frmExportFileWithProperties, _frmExportFileWithProperties.Text, null);
                //
                _frmExportFileWithProperties.ExportFileProperties = new Export3mfFileProperties();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportToPythonJournal_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Python files | *.py";
                    if (_controller.OpenedFileName != null)
                        saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName) + ".py";
                    //
                    if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        SetStateWorking(Globals.ExportingText);
                        WriteSessionJournal(_controller.GetCommands(), saveFileDialog.FileName,
                                            _controller.OpenedFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void WriteSessionJournal(List<PrePoMax.Commands.Command> commands, string fileName, string source)
        {
            PythonJournalReport report = PythonJournalExporter.Export(commands, fileName, source);
            //
            WriteDataToOutput(report.GetSummary());
            foreach (string refusal in report.Refusals) WriteDataToOutput("   Not emitted: " + refusal);
        }
        private async void ExportUsingFileProperties(ExportFileProperties properties)
        {
            try
            {
                SetStateWorking(Globals.ExportingText);
                await Task.Run(() => _controller.ExportUsingFileProperties(properties));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        //
        private bool New(ModelSpaceEnum modelSpace, UnitSystemType unitSystemType)
        {
            try
            {
                TopMost = true;    // fix the problem with Solidworks opened in the background
                //
                if ((_controller.ModelChanged || _controller.ModelInitialized || _controller.ResultsInitialized) &&
                    MessageBoxes.ShowWarningQuestionOKCancel("OK to close the current model?") != DialogResult.OK) return false;
                //
                _controller.DeInitialize();
                SetMenuAndToolStripVisibility();
                //
                bool update = false;
                // The model space and the unit system are undefined
                if (modelSpace == ModelSpaceEnum.Undefined || unitSystemType == UnitSystemType.Undefined)
                {
                    if (SelectNewModelProperties(true))
                    {
                        _controller.New();
                        SetNewModelProperties();
                        update = true;
                    }
                }
                else
                {
                    _controller.New();
                    _controller.SetNewModelPropertiesCommand(modelSpace, unitSystemType);
                    update = true;
                }
                //
                if (update)
                {
                    SetMenuAndToolStripVisibility();
                    //
                    _controller.ModelChanged = false; // must be here since adding a unit system changes the model
                }
                UpdatePythonEngine();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                TopMost = false;
            }
            return true;
        }
        private bool CheckBeforeOpen(string fileName)
        {
            if (!File.Exists(fileName)) return false;
            //
            if (_controller.ModelChanged)
            {
                string extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".pmx" || extension == ".pmh")
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to close the current model?") != DialogResult.OK)
                        return false;
                }
                else if ((extension == ".frd" || extension == ".foam") && _controller.AllResults.ContainsResult(fileName))
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to reopen the existing results?") != DialogResult.OK)
                        return false;
                    //
                    _selectedSymbolIndex.Remove(ViewGeometryModelResults.Results);
                }
            }
            return true;
        }
        private async void TryOpen(string fileName)
        {
            try
            {
                string parameters = Globals.FromFileOpenMenu;
                if (CheckBeforeOpen(fileName))
                    await OpenAsync(fileName, _controller.OpenFileCommand, true, null, parameters);
                //
                string extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".frd") RunHistoryPostprocessing();    // must be here
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                _controller.ModelChanged = false;   // hide message box
                tsmiNew_Click(null, null);
            }
        }
        private async Task OpenAsync(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true,
                                     Action callback = null, string parameters = null)
        {
            bool stateSet = false;
            string stateText = Globals.OpeningText;
            try
            {
                string extension = Path.GetExtension(fileName).ToLower();
                if (extension == ".pmh") stateText = Globals.RegeneratingText;
                //
                if (SetStateWorking(stateText) || IsStateRegeneratingOrUndoing())
                {
                    stateSet = true;
                    await Task.Run(() => Open(fileName, ActionOnFile, resetCamera, parameters));
                    callback?.Invoke(); // close monitor window
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
                // If the model space or the unit system are undefined
                if (_controller.ModelInitialized) IfNeededSelectAndSetNewModelProperties();
                if (_controller.ResultsInitialized) SelectResultsUnitSystem(parameters);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(stateText);
                SetMenuAndToolStripVisibility();
            }
        }
        public void Open(string fileName, Action<string, string> ActionOnFile, bool resetCamera = true, string parameters = null)
        {
            ActionOnFile(fileName, parameters);
            //
            UpdateResultMenus();
            UpdatePythonEngine();
            //
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry) _controller.DrawGeometry(resetCamera);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model) _controller.DrawModel(resetCamera);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
            {
                // Set the representation which also calls Draw
                _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
                //
                if (resetCamera) tsmiFrontView_Click(null, null);
            }
            else throw new NotSupportedException();
            //
            _controller.Annotations.DrawAnnotations(false, true);
            //
            RestorePendingSidePanelsState();
        }
        private void UpdateResultMenus()
        {
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds(true);
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
        }
        public async void RunHistoryFile(string fileName)
        {
            // This adds commands to the history - it is not the same as Open
            try
            {
                CloseAllForms();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                await Task.Run(() => _controller.RunHistoryFile(fileName));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput();
                //
                SetZoomToFit(true);
            }
        }
        private async void ImportFile(bool onlyMaterials)
        {
            try
            {
                if (!_controller.ModelInitialized)
                    throw new CaeException("There is no model to import into. First create a new model.");
                // If the model space or the unit system are undefined
                IfNeededSelectAndSetNewModelProperties();
                //
                string filter = GetFileImportFilter(onlyMaterials);
                string[] files = GetFileNamesToImport(filter);
                //
                if (files != null && files.Length > 0)
                {
                    _controller.ClearErrors();
                    _controller.ClearWarnings();
                    //
                    SetStateWorking(Globals.ImportingText);
                    foreach (var file in files)
                    {
                        await _controller.ImportFileAsync(file, onlyMaterials);
                    }
                    SetFrontBackView(true, true);   // animate must be true in order for the scale bar to work correctly
                    //
                    int numErrors = _controller.GetNumberOfErrors();
                    int numWarnings = _controller.GetNumberOfWarnings();
                    if (numWarnings > 0) _controller.OutputWarnings();
                    if (numErrors > 0)
                    {
                        _controller.OutputErrors();
                        string message = "There were errors while importing the file/files.";
                        WriteDataToOutput(message);
                        AutoClosingMessageBox.ShowError(message, 3000);
                    }
                    else if (numWarnings > 0)
                    {
                        string message = "There were warnings while importing the file/files.";
                        WriteDataToOutput(message);
                        AutoClosingMessageBox.ShowWarning(message, 3000);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ImportingText);
            }
        }
        private async Task SaveAsync()
        {
            if (!_controller.ModelInitialized && !_controller.ResultsInitialized)
                throw new CaeException("There is no model or results to save.");
            //
            try
            {
                SetStateWorking(Globals.SavingText);
                await Task.Run(() => _controller.Save());
                UpdatePythonEngine();
            }
            finally
            {
                SetStateReady(Globals.SavingText);
            }
        }
        private bool GetExportFileName(string filter, out string fileName, out int filterIndex)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = filter;
                if (_controller.OpenedFileName != null)
                    saveFileDialog.FileName = Path.GetFileNameWithoutExtension(_controller.OpenedFileName);
                //
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    // The filter adds its extension to a file name typed without one
                    fileName = saveFileDialog.FileName;
                    filterIndex = saveFileDialog.FilterIndex - 1;
                    return true;
                }
            }
            fileName = null;
            filterIndex = -1;
            return false;
        }
        private async void ExportGeometryParts(string[] partNames)
        {
            try
            {
                string fileName;
                int filterIndex;
                string filter = "STEP parts (*.stp)|*.stp|STEP assembly (*.stp)|*.stp|" +
                                "BREP parts (*.brep)|*.brep|BREP assembly (*.brep)|*.brep|" +
                                "Stereolithography files (*.stl)|*.stl|Mmg files (*.mesh)|*.mesh";
                GeometryExportFormatEnum[] formats = new GeometryExportFormatEnum[] { GeometryExportFormatEnum.Step,
                                                                                      GeometryExportFormatEnum.StepAssembly,
                                                                                      GeometryExportFormatEnum.Brep,
                                                                                      GeometryExportFormatEnum.BrepAssembly,
                                                                                      GeometryExportFormatEnum.Stl,
                                                                                      GeometryExportFormatEnum.Mmg };
                if (GetExportFileName(filter, out fileName, out filterIndex))
                {
                    GeometryExportFormatEnum format = ExportFormatTools.GetGeometryFormat(fileName, formats[filterIndex]);
                    SetStateWorking(Globals.ExportingText);
                    //
                    await Task.Run(() => _controller.ExportGeometryPartsCommand(partNames, fileName, format));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void ExportMeshParts(string[] partNames)
        {
            try
            {
                string fileName;
                int filterIndex;
                string filter = "CalculiX files (*.inp)|*.inp|Abaqus files (*.inp)|*.inp|Gmsh mesh files (*.msh)|*.msh|" +
                                "Mmg files (*.mesh)|*.mesh|Stereolithography files (*.stl)|*.stl";
                MeshExportFormatEnum[] formats = new MeshExportFormatEnum[] { MeshExportFormatEnum.Calculix,
                                                                              MeshExportFormatEnum.Abaqus,
                                                                              MeshExportFormatEnum.Gmsh,
                                                                              MeshExportFormatEnum.Mmg,
                                                                              MeshExportFormatEnum.Stl };
                if (GetExportFileName(filter, out fileName, out filterIndex))
                {
                    MeshExportFormatEnum format = ExportFormatTools.GetMeshFormat(fileName, formats[filterIndex]);
                    SetStateWorking(Globals.ExportingText);
                    //
                    await Task.Run(() => _controller.ExportMeshPartsCommand(partNames, fileName, format));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void ExportModel()
        {
            try
            {
                string fileName;
                int filterIndex;
                string filter = "CalculiX files (*.inp)|*.inp|Abaqus files (*.inp)|*.inp";
                if (GetExportFileName(filter, out fileName, out filterIndex))
                {
                    SetStateWorking(Globals.ExportingText);
                    //
                    if (filterIndex == 1) _controller.ExportToAbaqusCommand(fileName);
                    else _controller.ExportToCalculixCommand(fileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private async void ExportResultParts(string[] partNames)
        {
            try
            {
                string fileName;
                int filterIndex;
                string filter = "CalculiX files (*.inp)|*.inp|Stereolithography files (*.stl)|*.stl";
                ResultsExportFormatEnum[] formats = new ResultsExportFormatEnum[] { ResultsExportFormatEnum.Calculix,
                                                                                    ResultsExportFormatEnum.Stl };
                if (GetExportFileName(filter, out fileName, out filterIndex))
                {
                    ResultsExportFormatEnum format = ExportFormatTools.GetResultsFormat(fileName, formats[filterIndex]);
                    SetStateWorking(Globals.ExportingText);
                    //
                    await Task.Run(() => _controller.ExportResultPartsCommand(partNames, fileName, format));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        //
        private void tsmiCloseCurrentResult_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.AllResults.Count <= 1) tsmiCloseAllResults_Click(null, null);
                else
                {
                    _controller.RemoveCurrentResultsCommand();  // do not set the current result
                    if (tscbResultNames.Items.Count > 0) tscbResultNames.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCloseAllResults_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.ClearResultsCommand(); // calls this.ClearResults();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        //Recent
        public void UpdateRecentFilesThreadSafe()
        {
            InvokeIfRequired(UpdateRecentFiles);
        }
        public void UpdateRecentFiles()
        {
            try
            {
                string[] fileNames = _controller.Settings.General.GetRecentFiles();
                if (fileNames != null)
                {
                    tsmiOpenRecent.DropDownItems.Clear();
                    //
                    ToolStripMenuItem menuItem;
                    foreach (var fileName in fileNames)
                    {
                        menuItem = new ToolStripMenuItem(fileName.Replace("&", "&&"));
                        menuItem.Name = fileName;
                        menuItem.Click += tsmiRecentFile_Click;
                        tsmiOpenRecent.DropDownItems.Add(menuItem);
                    }
                    if (fileNames.Length > 0)
                    {
                        ToolStripSeparator separator = new ToolStripSeparator();
                        tsmiOpenRecent.DropDownItems.Add(separator);
                    }
                    menuItem = new ToolStripMenuItem("Clear Recent Files");
                    menuItem.Click += tsmiClearRecentFiles_Click;
                    tsmiOpenRecent.DropDownItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRecentFile_Click(object sender, EventArgs e)
        {
            try
            {
                string fileName = ((ToolStripMenuItem)sender).Name;
                if (!File.Exists(fileName))
                {
                    string text = "The file " + fileName + " does not exist. Remove it from the recent list?";
                    if (MessageBoxes.ShowWarningQuestionYesNo(text) == DialogResult.Yes)
                    {
                        _controller.RemoveFileNameFromRecentFiles(fileName);
                        UpdateRecentFilesThreadSafe();
                    }
                }
                else TryOpen(fileName);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiClearRecentFiles_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.ClearRecentFiles();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        #endregion  ################################################################################################################

        #region Edit menu ##########################################################################################################
        private async void tsmiUndo_Click(object sender, EventArgs e)
        {
            try
            {
                RegenerateTypeEnum regenerateType;
                if (TryGetRegenerateType(_controller.IsRegenerateTypeUsedByUndo(), out regenerateType))
                {
                    SetStateWorking(Globals.UndoingText);
                    _modelTree.ScreenUpdating = false;
                    //
                    await Task.Run(() => _controller.UndoHistory(regenerateType));
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.UndoingText);
                _modelTree.ScreenUpdating = true;
                _modelTree.RegenerateTree(_controller.Model, _controller.Jobs, _controller.CurrentResult);
                //
                SetMenuAndToolStripVisibility();
                //
                SetZoomToFit(true);
            }
        }
        private void tsmiRedo_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.RedoHistory();
                //
                SetMenuAndToolStripVisibility();
                //
                SetZoomToFit(true);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditHistory_Click(object sender, EventArgs e)
        {
            try
            {
                CloseAllForms();
                //
                _frmEditCommands.PrepareForm();
                //
                SetFormLocation(_frmEditCommands);
                _frmEditCommands.Show(this);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiRegenerateHistory_Click(object sender, EventArgs e)
        {
            RegenerateTypeEnum regenerateType;
            if (TryGetRegenerateType(_controller.IsRegenerateTypeUsed(), out regenerateType))
            {
                RegenerateHistory(false, false, regenerateType);
            }
        }
        private void tsmiRegenerateHistoryUsingOtherFiles_Click(object sender, EventArgs e)
        {
            RegenerateTypeEnum regenerateType;
            if (TryGetRegenerateType(_controller.IsRegenerateTypeUsed(), out regenerateType))
            {
                RegenerateHistory(true, false, regenerateType);
            }
        }
        private void tsmiRegenerateHistoryWithRemeshing_Click(object sender, EventArgs e)
        {
            RegenerateTypeEnum regenerateType;
            if (TryGetRegenerateType(_controller.IsRegenerateTypeUsed(), out regenerateType))
            {
                RegenerateHistory(false, true, regenerateType);
            }
        }
        private bool TryGetRegenerateType(bool regenerateTypeUsed, out RegenerateTypeEnum regenerateType)
        {
            regenerateType = RegenerateTypeEnum.All;
            //
            if (!regenerateTypeUsed) return true;
            //
            SetFormLocation(_frmRegenerate);
            if (_frmRegenerate.ShowDialog(this) != DialogResult.OK) return false;
            //
            regenerateType = _frmRegenerate.RegenerateType;
            return true;
        }
        //
        public async void RegenerateHistory(bool showFileDialog, bool showMeshDialog, RegenerateTypeEnum regenerateType)
        {
            try
            {
                CloseAllForms();
                Clear3D();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                await Task.Run(() => _controller.RegenerateHistoryCommands(showFileDialog, showMeshDialog, regenerateType));
                //
                UpdateResultMenus();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput(); 
                //
                SetZoomToFit(true);
            }
        }
        public void EnableDisableUndoRedo(string undo, string redo)
        {
            InvokeIfRequired(EnableDisable, undo, redo);
        }
        private void EnableDisable(string undo, string redo)
        {
            if (undo == null)
            {
                tsmiUndo.Text = "Undo";
                tsmiUndo.Enabled = false;
            }
            else
            {
                tsmiUndo.Text = "Undo - " + undo;
                tsmiUndo.Enabled = true;
            }

            if (redo == null)
            {
                tsmiRedo.Text = "Redo";
                tsmiRedo.Enabled = false;
            }
            else
            {
                tsmiRedo.Text = "Redo - " + redo;
                tsmiRedo.Enabled = true;
            }
        }

        #endregion  ################################################################################################################

        #region View menu ##########################################################################################################
        // Controls
        private void tsmiPythonScriptEditor_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized) return;
                // Change the panel check state
                tsmiPythonScriptEditor.Checked = !tsmiPythonScriptEditor.Checked;
                // Add Python panel
                if (tsmiPythonScriptEditor.Checked == true)
                {
                    PythonPanel pythonPanel = GetPythonPanel();
                    UpdatePythonEngine(true);
                    ShowSidePanel(pythonPanel, 0.5, 100, 1000, false);
                }
                else HideSidePanel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiAdvisor_Click(object sender, EventArgs e)
        {
            if (!_controller.ModelInitialized) return;
            // Change the wizard check state
            tsmiSidePanelAdvisor.Checked = !tsmiSidePanelAdvisor.Checked;
            tsmiHelpAdvisor.Checked = tsmiSidePanelAdvisor.Checked;
            // Add wizard panel
            if (tsmiSidePanelAdvisor.Checked == true) ShowSidePanel(GetAdvisorPanel(), 0.2, 100, 300, true);
            else HideSidePanel();
        }
        private PythonPanel GetPythonPanel()
        {
            if (_pythonPanel == null)
            {
                _pythonPanel = new PythonPanel();
                _pythonPanel.ExecuteCommand += PythonPanel_ExecuteCommand;
                _pythonPanel.ExecuteScript += PythonPanel_ExecuteScript;
                _pythonPanel.RecordAndExecuteScript += PythonPanel_RecordAndExecuteScript;
                _pythonPanel.AutocompleteRequested += PythonPanel_AutocompleteRequested;
                _pythonPanel.CloseButtonPressedEvent += tsmiPythonScriptEditor_Click;
                _pythonPanel.StateChanged += SidePanel_StateChanged;
            }
            if (!_pythonPanelStateApplied)
            {
                _pythonPanelStateApplied = true;
                _pythonPanel.RestoreState(_sidePanelsState?.PythonPanelState);
            }
            return _pythonPanel;
        }
        private AdvisorPanel GetAdvisorPanel()
        {
            if (_advisorPanel == null)
            {
                _advisorPanel = AdvisorCreator.CreatePanel(this);
                _advisorPanel.StateChanged += SidePanel_StateChanged;
            }
            if (!_advisorPanelStateApplied)
            {
                _advisorPanelStateApplied = true;
                _advisorPanel.RestoreState(_sidePanelsState?.AdvisorPanelState);
            }
            return _advisorPanel;
        }
        private void ShowSidePanel(SidePanel sidePanel, double relativeSize, int min, int max, bool fixPanelWith)
        {
            // Reset
            if (sidePanel != null) HideSidePanel();
            // Set
            tsmiSidePanelAdvisor.Checked = sidePanel is AdvisorPanel;
            tsmiHelpAdvisor.Checked = tsmiSidePanelAdvisor.Checked;
            tsmiPythonScriptEditor.Checked = sidePanel is PythonPanel;
            tsmiXYPlotPanel.Checked = sidePanel is XYPlotPanel;
            //
            Control parent = splitContainer1.Panel2;
            // First remove the splitContainer2
            parent.Controls.Remove(splitContainer2);
            // Add split container
            _sidePanelHost = new SplitContainer();
            if (fixPanelWith) _sidePanelHost.FixedPanel = FixedPanel.Panel2;
            _sidePanelHost.Dock = DockStyle.Fill;
            parent.Controls.Add(_sidePanelHost);
            // Set the Panel 2 size - min 100 max 300
            _sidePanelHost.SplitterDistance = Math.Max(min, Math.Max(parent.Width - max,
                                                                     (int)(parent.Width * relativeSize)));
            // Panel 1 - LEFT
            _sidePanelHost.Panel1.Controls.Add(splitContainer2);
            // Update vtk control size
            UpdateVtkControlSize();
            // Panel 2 - RIGHT
            _visibleSidePanel = sidePanel;
            _sidePanelHost.Panel2.Controls.Add(_visibleSidePanel);
            _visibleSidePanel.Dock = DockStyle.Fill;
            _visibleSidePanel.UpdateDesign();
            _visibleSidePanel.AutoScroll = !(sidePanel is XYPlotPanel);
            //
            SetVisibleSidePanelWidthRatio(GetStoredSidePanelWidthRatio());
            _sidePanelHost.SplitterMoved += sidePanelHost_SplitterMoved;
            SidePanel_StateChanged();
        }
        private void HideSidePanel()
        {
            bool wasVisible = _visibleSidePanel != null;
            if (!wasVisible && _sidePanelHost == null) return;
            // Save state
            StoreVisibleSidePanelWidthRatio();
            // Detach controls before disposing only the temporary host
            if (_sidePanelHost != null)
            {
                _sidePanelHost.SplitterMoved -= sidePanelHost_SplitterMoved;
                _sidePanelHost.Panel1.Controls.Remove(splitContainer2);
                if (_visibleSidePanel != null) _sidePanelHost.Panel2.Controls.Remove(_visibleSidePanel);
                splitContainer1.Panel2.Controls.Remove(_sidePanelHost);
                _sidePanelHost.Dispose();
                _sidePanelHost = null;
            }
            if (!splitContainer1.Panel2.Controls.Contains(splitContainer2))
                splitContainer1.Panel2.Controls.Add(splitContainer2);
            // Reset
            _visibleSidePanel = null;
            tsmiSidePanelAdvisor.Checked = false;
            tsmiHelpAdvisor.Checked = false;
            tsmiPythonScriptEditor.Checked = false;
            tsmiXYPlotPanel.Checked = false;
            // Update vtk control size
            UpdateVtkControlSize();
            if (wasVisible) SidePanel_StateChanged();
        }
        private void sidePanelHost_SplitterMoved(object sender, SplitterEventArgs e)
        {
            StoreVisibleSidePanelWidthRatio();
            SidePanel_StateChanged();
        }
        private SidePanelTypeEnum GetVisibleSidePanelType()
        {
            if (_visibleSidePanel is PythonPanel) return SidePanelTypeEnum.Python;
            else if (_visibleSidePanel is XYPlotPanel) return SidePanelTypeEnum.XYPlot;
            else if (_visibleSidePanel is AdvisorPanel) return SidePanelTypeEnum.Advisor;
            else return SidePanelTypeEnum.None;
        }
        private void EnsureSidePanelsState()
        {
            if (_sidePanelsState == null) _sidePanelsState = new SidePanelsState();
        }
        private double GetStoredSidePanelWidthRatio()
        {
            EnsureSidePanelsState();
            //
            SidePanelTypeEnum type = GetVisibleSidePanelType();
            if (type == SidePanelTypeEnum.Python) return _sidePanelsState.PythonPanelWidthRatio;
            else if (type == SidePanelTypeEnum.XYPlot) return _sidePanelsState.XYPlotPanelWidthRatio;
            else if (type == SidePanelTypeEnum.Advisor) return _sidePanelsState.AdvisorPanelWidthRatio;
            else return 0;
        }
        private void StoreVisibleSidePanelWidthRatio()
        {
            if (_sidePanelHost == null || _sidePanelHost.Width <= 0) return;
            //
            EnsureSidePanelsState();
            double ratio = _sidePanelHost.Panel2.Width / (double)_sidePanelHost.Width;
            SidePanelTypeEnum type = GetVisibleSidePanelType();
            if (type == SidePanelTypeEnum.Python) _sidePanelsState.PythonPanelWidthRatio = ratio;
            else if (type == SidePanelTypeEnum.XYPlot) _sidePanelsState.XYPlotPanelWidthRatio = ratio;
            else if (type == SidePanelTypeEnum.Advisor) _sidePanelsState.AdvisorPanelWidthRatio = ratio;
        }
        private void SetVisibleSidePanelWidthRatio(double ratio)
        {
            if (_sidePanelHost == null || ratio <= 0 || ratio >= 1 || _sidePanelHost.Width <= 0) return;
            //
            int available = _sidePanelHost.Width - _sidePanelHost.SplitterWidth;
            if (available < 200) return;
            //
            int panel2Width = (int)Math.Round(_sidePanelHost.Width * ratio);
            panel2Width = Math.Max(100, Math.Min(available - 100, panel2Width));
            //
            _sidePanelHost.SplitterDistance = available - panel2Width;
        }
        private void SidePanel_StateChanged()
        {
            if (!_restoringSidePanelsState && _controller != null && _controller.ModelInitialized)
                _controller.ModelChanged = true;
        }
        public SidePanelsState GetSidePanelsState()
        {
            if (InvokeRequired)
                return (SidePanelsState)Invoke((Func<SidePanelsState>)GetSidePanelsState);
            //
            EnsureSidePanelsState();
            StoreVisibleSidePanelWidthRatio();
            _sidePanelsState.VisiblePanel = GetVisibleSidePanelType();
            if (_pythonPanel != null) _sidePanelsState.PythonPanelState = _pythonPanel.CaptureState();
            if (_xyPlotPanel != null) _sidePanelsState.XYPlotPanelState = _xyPlotPanel.CaptureState();
            if (_advisorPanel != null) _sidePanelsState.AdvisorPanelState = _advisorPanel.CaptureState();
            return _sidePanelsState;
        }
        public void SetPendingSidePanelsState(SidePanelsState state)
        {
            _pendingSidePanelsState = state;
        }
        private void RestorePendingSidePanelsState()
        {
            if (_pendingSidePanelsState == null) return;
            //
            SidePanelsState state = _pendingSidePanelsState;
            _pendingSidePanelsState = null;
            InvokeIfRequired(RestoreSidePanelsState, state);
        }
        private void RestoreSidePanelsState(SidePanelsState state)
        {
            _restoringSidePanelsState = true;
            try
            {
                HideSidePanel();
                _sidePanelsState = state ?? new SidePanelsState();
                _pythonPanelStateApplied = false;
                _xyPlotPanelStateApplied = false;
                _advisorPanelStateApplied = false;
                //
                if (_sidePanelsState.VisiblePanel == SidePanelTypeEnum.Python)
                {
                    UpdatePythonEngine(true);
                    ShowSidePanel(GetPythonPanel(), 0.5, 100, 1000, false);
                }
                else if (_sidePanelsState.VisiblePanel == SidePanelTypeEnum.XYPlot) ShowXYPlotPanel();
                else if (_sidePanelsState.VisiblePanel == SidePanelTypeEnum.Advisor)
                    ShowSidePanel(GetAdvisorPanel(), 0.2, 100, 300, true);
            }
            finally
            {
                _restoringSidePanelsState = false;
            }
        }
        private void ResetSidePanelsState()
        {
            _restoringSidePanelsState = true;
            try
            {
                HideSidePanel();
                _pythonPanel?.Dispose();
                _xyPlotPanel?.Dispose();
                _advisorPanel?.Dispose();
                _pythonPanel = null;
                _xyPlotPanel = null;
                _advisorPanel = null;
                _sidePanelsState = new SidePanelsState();
                _pendingSidePanelsState = null;
                _pythonPanelStateApplied = false;
                _xyPlotPanelStateApplied = false;
                _advisorPanelStateApplied = false;
            }
            finally
            {
                _restoringSidePanelsState = false;
            }
        }
        private void UpdatePythonEngine(bool forceUpdate = false)
        {
            if (_visibleSidePanel is PythonPanel || forceUpdate)
                PythonEngineManager.SetResults(_controller.AllResults, _controller);
        }
        private void ResetPythonEngineContext()
        {
            if (!PythonEngineManager.Initialized) return;
            //
            PythonEngineManager.ResetScope();
            UpdatePythonEngine(true);
        }
        private void PythonPanel_ExecuteCommand(object sender, PythonTextBoxCommandEventArgs e)
        {
            PythonEngineResult result = PythonEngineManager.PushLine(e.Command);
            //
            e.Output = result.Output;
            e.Error = result.Error;
            e.NeedMoreInput = result.NeedMoreInput;
        }
        private void PythonPanel_ExecuteScript(object sender, PythonTextBoxCommandEventArgs e)
        {
            if (!TakeStateForPythonScript(e)) return;
            try
            {
                PythonEngineResult result = PythonEngineManager.ExecuteScript(e.Command);
                //
                e.Output = result.Output;
                e.Error = result.Error;
                e.NeedMoreInput = result.NeedMoreInput;
            }
            finally
            {
                ReleaseStateForPythonScript();
            }
        }
        private void PythonPanel_RecordAndExecuteScript(object sender, PythonTextBoxCommandEventArgs e)
        {
            if (!TakeStateForPythonScript(e)) return;
            try
            {
                PythonEngineResult result = _controller.RunPythonScriptCommand(e.Command);
                //
                e.Output = result.Output;
                e.Error = result.Error;
                e.NeedMoreInput = result.NeedMoreInput;
            }
            finally
            {
                ReleaseStateForPythonScript();
            }
        }
        // A script of the user's own is marked the way any other long operation is, so that a call
        // from the automation server arriving in the middle of it is refused rather than interleaved:
        // a script that opens a dialog, or that waits for work of its own, lets the window pump
        // messages and would otherwise look idle.
        //
        // It also works the other way round, and only that way round: a script does not start while
        // an agent call holds the state, and says so. Anything else the application is doing already
        // makes it busy, so an agent call arriving then is refused for that reason anyway, and
        // refusing the user's script as well would only cost a retry
        private bool TakeStateForPythonScript(PythonTextBoxCommandEventArgs e)
        {
            if (tsslState.Text == Globals.AutomationText)
            {
                e.Output = "";
                e.Error = "The automation server is running a call, so the script was not started. " +
                          "Wait for it to finish, or switch the server off in Tools - Automation Server." +
                          Environment.NewLine;
                e.NeedMoreInput = false;
                return false;
            }
            _pythonScriptTookState = SetStateWorking(Globals.PythonScriptText);
            return true;
        }
        private void ReleaseStateForPythonScript()
        {
            if (!_pythonScriptTookState) return;
            //
            _pythonScriptTookState = false;
            SetStateReady(Globals.PythonScriptText);
        }
        private void PythonPanel_AutocompleteRequested(object sender, PythonTextBoxAutocompleteEventArgs e)
        {
            e.Completions.AddRange(PythonEngineManager.GetCompletions(e.Expression, e.Prefix));
        }
        // Standard views
        private void tsmiFrontView_Click(object sender, EventArgs e)
        {
            SetFrontBackView(true, true);
        }
        private void tsmiBackView_Click(object sender, EventArgs e)
        {
            SetFrontBackView(true, false);
        }
        private void tsmiTopView_Click(object sender, EventArgs e)
        {
            _vtk.SetTopBottomView(true, true);
        }
        private void tsmiBottomView_Click(object sender, EventArgs e)
        {
            _vtk.SetTopBottomView(true, false);
        }
        private void tsmiLeftView_Click(object sender, EventArgs e)
        {
            _vtk.SetLeftRightView(true, true);
        }
        private void tsmiRightView_Click(object sender, EventArgs e)
        {
            _vtk.SetLeftRightView(true, false);
        }
        // Normal and vertical views
        private void tsmiNormalView_Click(object sender, EventArgs e)
        {
            _vtk.SetNormalView(true);
        }
        private void tsmiVerticalView_Click(object sender, EventArgs e)
        {
            _vtk.SetVerticalView(true, true);
        }
        // Isometric view and zoom to fit
        private void tsmiIsometricView_Click(object sender, EventArgs e)
        {
            _vtk.SetIsometricView(true, true);
        }
        private void tsmiZoomToFit_Click(object sender, EventArgs e)
        {
            SetZoomToFit(true);
        }
        private void tsmiBoxZoom_Click(object sender, EventArgs e)
        {
            StartBoxZoom();
        }
        // Rotation center
        private void _vtk_RotationPointChanged(double[] rotationPoint)
        {
            _controller.RotationPointChanged(rotationPoint);
        }
        public void SetRotationPoint(double[] rotationPoint)
        {
            InvokeIfRequired(_vtk.SetRotationPoint, rotationPoint); 
        }
        // User views
        private void tsmiEditUserViews_Click(object sender, EventArgs e)
        {
            try
            {
                _frmUserView.UserViewParameters = _controller.GetUserViewParameters();
                ShowForm(_frmUserView, tsmiUserViews.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        public void UpdateUserViewsThreadSafe(string[] viewNames)
        {
            InvokeIfRequired(UpdateUserViews, viewNames);
        }
        public void UpdateUserViews(string[] viewNames)
        {
            try
            {
                if (viewNames != null)
                {
                    tsmiUserViews.DropDownItems.Clear();
                    tsmiUserViews.DropDownItems.Add(tsmiEditUserViews);
                    // Separator
                    if (viewNames.Length > 0) tsmiUserViews.DropDownItems.Add(new ToolStripSeparator());
                    //
                    ToolStripMenuItem menuItem;
                    foreach (var viewName in viewNames)
                    {
                        menuItem = new ToolStripMenuItem(viewName);
                        menuItem.Name = viewName;
                        menuItem.Click += tsmiUserView_Click;
                        tsmiUserViews.DropDownItems.Add(menuItem);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiUserView_Click(object sender, EventArgs e)
        {
            try
            {
                string viewName = ((ToolStripMenuItem)sender).Name;
                _controller.SetUserView(viewName);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Model appearance
        private void tsmiShowWireframeEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.Wireframe;
            }
            catch { }
        }
        private void tsmiShowElementEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.ElementEdges;
            }
            catch { }
        }
        private void tsmiShowModelEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.ModelEdges;
            }
            catch { }
        }
        private void tsmiShowNoEdges_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.CurrentEdgesVisibility = vtkControl.vtkEdgesVisibility.NoEdges;
            }
            catch { }
        }
        // Section view
        private void tsmiSectionView_Click(object sender, EventArgs e)
        {
            try
            {
                SinglePointDataEditor.ParentForm = _frmSectionView;
                SinglePointDataEditor.Controller = _controller;
                //
                ShowForm(_frmSectionView, tsmiSectionView.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void TurnSectionViewOnOff()
        {
            try
            {
                if (!_frmSectionView.Visible) _controller.TurnSectionViewOnOff();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExplodePartsText);
            }
        }
        // Exploded view
        private void tsmiExplodedView_Click(object sender, EventArgs e)
        {
            try
            {
                ExplodedViewParameters parameters = _controller.GetCurrentExplodedViewParameters();
                _frmExplodedView.SetExplodedViewParameters(parameters);
                //
                ShowForm(_frmExplodedView, _frmExplodedView.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void TurnExplodedViewOnOff(bool animate)
        {
            try
            {
                SetStateWorking(Globals.ExplodePartsText);
                _controller.TurnExplodedViewOnOff(animate);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExplodePartsText);
            }
        }
        private void tsmiFollowerView_Click(object sender, EventArgs e)
        {
            try
            {
                SinglePointDataEditor.ParentForm = _frmFollowerView;
                SinglePointDataEditor.Controller = _controller;
                //
                FollowerViewParameters parameters = _controller.GetCurrentFollowerViewParameters();
                _frmFollowerView.SetFollowerViewParameters(parameters);
                //
                ShowForm(_frmFollowerView, _frmFollowerView.Text, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Hide/Show
        private void tsmiHideAllParts_Click(object sender, EventArgs e)
        {
            try
            {
                string[] partNames;
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    partNames = _controller.GetGeometryPartNames();
                    _controller.HideGeometryPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    partNames = _controller.GetModelPartNames();
                    _controller.HideModelPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    partNames = _controller.GetResultPartNames();
                    _controller.HideResultPartsCommand(partNames);
                }
            }
            catch { }
        }
        private void tsmiShowAllParts_Click(object sender, EventArgs e)
        {
            try
            {
                string[] partNames;
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    partNames = _controller.GetGeometryPartNames();
                    _controller.ShowGeometryPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    partNames = _controller.GetModelPartNames();
                    _controller.ShowModelPartsCommand(partNames);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    partNames = _controller.GetResultPartNames();
                    _controller.ShowResultPartsCommand(partNames);
                }
            }
            catch { }
        }
        private void tsmiInvertVisibleParts_Click(object sender, EventArgs e)
        {
            try
            {
                BasePart[] parts;
                List<string> partNamesToHide = new List<string>();
                List<string> partNamesToShow = new List<string>();

                if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                {
                    parts = _controller.GetGeometryParts();
                    foreach (var part in parts)
                    {
                        if (part.Visible) partNamesToHide.Add(part.Name);
                        else partNamesToShow.Add(part.Name);
                    }
                    if (partNamesToHide.Count > 0) _controller.HideGeometryPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowGeometryPartsCommand(partNamesToShow.ToArray());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    parts = _controller.GetModelParts();
                    foreach (var part in parts)
                    {
                        if (part.Visible) partNamesToHide.Add(part.Name);
                        else partNamesToShow.Add(part.Name);
                    }
                    if (partNamesToHide.Count > 0) _controller.HideModelPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowModelPartsCommand(partNamesToShow.ToArray());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    parts = _controller.GetResultParts();
                    if (parts !=null)
                    {
                        foreach (var part in parts)
                        {
                            if (part.Visible) partNamesToHide.Add(part.Name);
                            else partNamesToShow.Add(part.Name);
                        }
                    }
                    if (partNamesToHide.Count > 0) _controller.HideResultPartsCommand(partNamesToHide.ToArray());
                    if (partNamesToShow.Count > 0) _controller.ShowResultPartsCommand(partNamesToShow.ToArray());
                }
            }
            catch { }
        }
        // Color annotations
        private void DropDown_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                e.Cancel = true;
        }
        private void tsmiAnnotateFaceOrientations_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateMeshTypes_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateParts_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateMaterials_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateSections_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateSectionThicknesses_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateAllSymbols_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                // Turn off
                if (tsmi.Checked)
                {
                    ClearAnnotationStatus();  // first check and then clear
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.None;
                }
                // Turn on
                else
                {
                    ClearAnnotationStatus();
                    //
                    tsmi.Checked = true;
                    tsmiAnnotateReferencePoints.Checked = true;
                    tsmiAnnotateConstraints.Checked = true;
                    tsmiAnnotateContactPairs.Checked = true;
                    tsmiAnnotateInitialConditions.Checked = true;
                    tsmiAnnotateBCs.Checked = true;
                    tsmiAnnotateLoads.Checked = true;
                    tsmiAnnotateDefinedFields.Checked = true;
                    //
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.ReferencePoints |
                                                    AnnotateWithColorEnum.Constraints |
                                                    AnnotateWithColorEnum.ContactPairs |
                                                    AnnotateWithColorEnum.InitialConditions |
                                                    AnnotateWithColorEnum.BoundaryConditions |
                                                    AnnotateWithColorEnum.Loads |
                                                    AnnotateWithColorEnum.DefinedFields;
                }
            }
        }
        private void tsmiAnnotateReferencePoints_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateConstraints_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateContactPairs_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateInitialConditions_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateBCs_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        private void tsmiAnnotateLoads_Click(object sender, EventArgs e)
        {
            _controller.AnnotateWithColor = ChangeAnnotationStatus(sender);
        }
        //
        private AnnotateWithColorEnum ChangeAnnotationStatus(object sender)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                // Turn off
                if (tsmi.Checked) tsmi.Checked = false;
                // Turn on
                else
                {
                    // Only one possibility - Face orientations or parts
                    if (tsmi == tsmiAnnotateFaceOrientations || tsmi == tsmiAnnotateMeshTypes ||
                        tsmi == tsmiAnnotateParts ||
                        tsmi == tsmiAnnotateMaterials || tsmi == tsmiAnnotateSections ||
                        tsmi == tsmiAnnotateSectionThicknesses)
                    {
                        ClearAnnotationStatus();
                        tsmi.Checked = true;
                    }
                    // Symbols
                    else
                    {
                        tsmiAnnotateFaceOrientations.Checked = false;
                        tsmiAnnotateMeshTypes.Checked = false;
                        tsmiAnnotateParts.Checked = false;
                        tsmiAnnotateMaterials.Checked = false;
                        tsmiAnnotateSections.Checked = false;
                        tsmiAnnotateSectionThicknesses.Checked = false;
                        //
                        tsmi.Checked = true;
                    }
                }
                //
                UpdateColorByComboBox();
                //
                if (tsmiAnnotateFaceOrientations.Checked) return AnnotateWithColorEnum.FaceOrientation;
                else if (tsmiAnnotateMeshTypes.Checked) return AnnotateWithColorEnum.MeshTypes;
                else if (tsmiAnnotateParts.Checked) return AnnotateWithColorEnum.Parts;
                else if (tsmiAnnotateMaterials.Checked) return AnnotateWithColorEnum.Materials;
                else if (tsmiAnnotateSections.Checked) return AnnotateWithColorEnum.Sections;
                else if (tsmiAnnotateSectionThicknesses.Checked) return AnnotateWithColorEnum.SectionThicknesses;
                else
                {
                    AnnotateWithColorEnum status = AnnotateWithColorEnum.None;
                    if (tsmiAnnotateReferencePoints.Checked) status |= AnnotateWithColorEnum.ReferencePoints;
                    if (tsmiAnnotateConstraints.Checked) status |= AnnotateWithColorEnum.Constraints;
                    if (tsmiAnnotateContactPairs.Checked) status |= AnnotateWithColorEnum.ContactPairs;
                    if (tsmiAnnotateInitialConditions.Checked) status |= AnnotateWithColorEnum.InitialConditions;
                    if (tsmiAnnotateBCs.Checked) status |= AnnotateWithColorEnum.BoundaryConditions;
                    if (tsmiAnnotateLoads.Checked) status |= AnnotateWithColorEnum.Loads;
                    if (tsmiAnnotateDefinedFields.Checked) status |= AnnotateWithColorEnum.DefinedFields;
                    //
                    tsmiAnnotateAllSymbols.Checked = status.HasFlag(AnnotateWithColorEnum.ReferencePoints |
                                                                    AnnotateWithColorEnum.Constraints |
                                                                    AnnotateWithColorEnum.ContactPairs |
                                                                    AnnotateWithColorEnum.InitialConditions |
                                                                    AnnotateWithColorEnum.BoundaryConditions |
                                                                    AnnotateWithColorEnum.Loads |
                                                                    AnnotateWithColorEnum.DefinedFields);
                    //
                    return status;
                }
            }
            return AnnotateWithColorEnum.None;
        }
        public void SetAnnotateWithColor(AnnotateWithColorEnum annotateWithColor)
        {
            InvokeIfRequired(() =>
            {
                ToolStripMenuItem tsmi = GetAnnotateWithColorMenuItem(annotateWithColor);
                if (tsmi == null)
                {
                    ClearAnnotationStatus();
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.None;
                }
                else if (!tsmi.Checked) _controller.AnnotateWithColor = ChangeAnnotationStatus(tsmi);
            });
        }
        private ToolStripMenuItem GetAnnotateWithColorMenuItem(AnnotateWithColorEnum annotateWithColor)
        {
            switch (annotateWithColor)
            {
                case AnnotateWithColorEnum.FaceOrientation: return tsmiAnnotateFaceOrientations;
                case AnnotateWithColorEnum.MeshTypes: return tsmiAnnotateMeshTypes;
                case AnnotateWithColorEnum.Parts: return tsmiAnnotateParts;
                case AnnotateWithColorEnum.Materials: return tsmiAnnotateMaterials;
                case AnnotateWithColorEnum.Sections: return tsmiAnnotateSections;
                case AnnotateWithColorEnum.SectionThicknesses: return tsmiAnnotateSectionThicknesses;
                default: return null;
            }
        }
        // Items
        private void tsmiViewVertexIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewEdgeIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewSurfaceIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewPartIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewPartNames_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewNodeIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        private void tsmiViewElementIds_Click(object sender, EventArgs e)
        {
            _controller.ShowItemIds = ChangeVisibleItemIdsStatus(sender);
        }
        //
        private ShowItemIdsEnum ChangeVisibleItemIdsStatus(object sender)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                // Turn off
                if (tsmi.Checked) tsmi.Checked = false;
                // Turn on
                else
                {
                    if (tsmi == tsmiViewNodeIds || tsmi == tsmiViewElementIds)
                    {
                        ClearGeometryItemIdsStatus();
                    }
                    else
                    {
                        ClearMeshItemIdsStatus();
                        if (tsmi == tsmiViewPartIds) tsmiViewPartNames.Checked = false;
                        else if (tsmi == tsmiViewPartNames) tsmiViewPartIds.Checked = false;
                    }
                    tsmi.Checked = true;
                }
                //
                ShowItemIdsEnum status = ShowItemIdsEnum.None;
                if (tsmiViewVertexIds.Checked) status |= ShowItemIdsEnum.Vertices;
                if (tsmiViewEdgeIds.Checked) status |= ShowItemIdsEnum.Edges;
                if (tsmiViewSurfaceIds.Checked) status |= ShowItemIdsEnum.Surfaces;
                if (tsmiViewPartIds.Checked) status |= ShowItemIdsEnum.Parts;
                if (tsmiViewPartNames.Checked) status |= ShowItemIdsEnum.PartNames;
                if (tsmiViewNodeIds.Checked) status |= ShowItemIdsEnum.Nodes;
                if (tsmiViewElementIds.Checked) status |= ShowItemIdsEnum.Elements;
                return status;
            }
            else return ShowItemIdsEnum.None;
        }
        // Results
        private void tsmiResultsUndeformed_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.Undeformed, UndeformedModelTypeEnum.None);
        }
        private void tsmiResultsDeformed_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.Deformed, UndeformedModelTypeEnum.None);
        }
        private void tsmiResultsColorContours_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.ColorContours, _controller.GetUndeformedModelType());
        }
        private void tsmiResultsVectorPlot_Click(object sender, EventArgs e)
        {
            SetUndeformedModelType(ViewResultsTypeEnum.VectorPlot, _controller.GetUndeformedModelType());
        }
        private void tsmiResultsUndeformedWireframe_Click(object sender, EventArgs e)
        {
            ToggleUndeformedModelType(UndeformedModelTypeEnum.WireframeBody);
        }
        private void tsmiResultsUndeformedSolid_Click(object sender, EventArgs e)
        {
            ToggleUndeformedModelType(UndeformedModelTypeEnum.SolidBody);
        }
        private void ToggleUndeformedModelType(UndeformedModelTypeEnum undeformedModelType)
        {
            if (_controller.ViewResultsType != ViewResultsTypeEnum.ColorContours &&
                _controller.ViewResultsType != ViewResultsTypeEnum.VectorPlot &&
                _controller.ViewResultsType != ViewResultsTypeEnum.TensorPlot) return;
            //
            if (_controller.GetUndeformedModelType() == undeformedModelType)
                undeformedModelType = UndeformedModelTypeEnum.None;
            //
            SetUndeformedModelType(_controller.ViewResultsType, undeformedModelType);
        }
        private void SetUndeformedModelType(ViewResultsTypeEnum viewResultsType, UndeformedModelTypeEnum undeformedModelType)
        {
            _controller.SetUndeformedModelType(undeformedModelType);
            //
            if (GetCurrentView() == ViewGeometryModelResults.Results)
            {
                if (_frmAnimation.Visible) _frmAnimation.Hide();
                _controller.ViewResultsType = viewResultsType;
            }
            //
            UpdateResultsTypeIconStates();
        }
        private void UpdateResultsTypeIconStates()
        {
            bool resultRepresentationAvailable =
                GetCurrentView() == ViewGeometryModelResults.Results &&
                (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours ||
                 _controller.ViewResultsType == ViewResultsTypeEnum.VectorPlot ||
                 _controller.ViewResultsType == ViewResultsTypeEnum.TensorPlot);
            tsmiDividerResultPart2.Visible = resultRepresentationAvailable;
            tsmiColorContoursOff.Visible = resultRepresentationAvailable;
            tsmiColorContoursOn.Visible = resultRepresentationAvailable;
            //
            tsbResultsUndeformed.Checked = false;
            tsbResultsDeformed.Checked = false;
            tsbResultsColorContours.Checked = false;
            tsbResultsVectorPlot.Checked = false;
            tsbResultsUndeformedWireframe.Checked = false;
            tsbResultsUndeformedSolid.Checked = false;
            tsmiResultsUndeformed.Checked = false;
            tsmiResultsDeformed.Checked = false;
            tsmiResultsColorContours.Checked = false;
            tsmiResultsVectorPlot.Checked = false;
            tsmiResultsUndeformedWireframe.Checked = false;
            tsmiResultsUndeformedSolid.Checked = false;
            //
            if (GetCurrentView() == ViewGeometryModelResults.Results)
            {
                if (_controller.ViewResultsType == ViewResultsTypeEnum.Undeformed)
                {
                    tsbResultsUndeformed.Checked = true;
                    tsmiResultsUndeformed.Checked = true;
                }
                else if (_controller.ViewResultsType == ViewResultsTypeEnum.Deformed)
                {
                    tsbResultsDeformed.Checked = true;
                    tsmiResultsDeformed.Checked = true;
                }
                else if (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours)
                {
                    tsbResultsColorContours.Checked = true;
                    tsmiResultsColorContours.Checked = true;
                }
                else if (_controller.ViewResultsType == ViewResultsTypeEnum.VectorPlot ||
                         _controller.ViewResultsType == ViewResultsTypeEnum.TensorPlot)
                {
                    tsbResultsVectorPlot.Checked = true;
                    tsmiResultsVectorPlot.Checked = true;
                }
                //
                if (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours ||
                    _controller.ViewResultsType == ViewResultsTypeEnum.VectorPlot ||
                    _controller.ViewResultsType == ViewResultsTypeEnum.TensorPlot)
                {
                    if (_controller.GetUndeformedModelType() == UndeformedModelTypeEnum.WireframeBody)
                    {
                        tsbResultsUndeformedWireframe.Checked = true;
                        tsmiResultsUndeformedWireframe.Checked = true;
                    }
                    else if (_controller.GetUndeformedModelType() == UndeformedModelTypeEnum.SolidBody)
                    {
                        tsbResultsUndeformedSolid.Checked = true;
                        tsmiResultsUndeformedSolid.Checked = true;
                    }
                }
            }
        }
        #endregion

        #region Geometry ###########################################################################################################
        internal void tsmiEditGeometryPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), EditGeometryPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetColorForGeometryParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Transform
        private void tsmiTranslateGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), TranslateGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRotateGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), RotateGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMirrorGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), MirrorGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiScaleGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), ScaleGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // End Transform
        private void tsmiCopyGeometryPartsToResults_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), CopyGeometryPartsToResults);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), HideGeometryParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ShowGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ShowOnlyGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetColorForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), ResetColorForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SetTransparencyForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), DeleteGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiCreateAndImportCompoundPart_Click(object sender, EventArgs e)
        {
            try
            {
                Clear3DSelection();
                SelectMultipleEntities("Parts", _controller.GetCADGeometryParts(), CreateAndImportCompoundPart, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRegenerateCompoundPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetCompoundParts(), RegenerateCompoundParts);
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
                SelectMultipleEntities("Cells", _controller.GetSplitCells(), GroupSplitCells);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiFuseBodies_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Bodies", _controller.GetFusableBodies(), FuseBodies);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapGeometryPartGeometries_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), SwapPartGeometries, 2, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Analyze geometry
        private void tsmiGeometryAnalyze_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryParts(), AnalyzeGeometry);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }

        }
        //                                                                                                                          
        private void EditGeometryPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Geometry;
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        // Transform
        private void TranslateGeometryParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmTranslate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for translation
            _frmTranslate.PartNames = partNames;
            //
            ShowForm(_frmTranslate, "Translate parts: " + partNames.ToShortString(), null);
        }
        public async Task TranslateGeometryPartsAsync(string[] partNames, double[] translateVector, int numberOfCopies)
        {
            await RunGeometryTransformAsync(partNames, () => _controller.TranslateGeometryPartsCommand(partNames,
                                                                                                      translateVector,
                                                                                                      numberOfCopies));
        }
        private void RotateGeometryParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmRotate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for rotation
            _frmRotate.PartNames = partNames;
            //
            ShowForm(_frmRotate, "Rotate parts: " + partNames.ToShortString(), null);
        }
        public async Task RotateGeometryPartsAsync(string[] partNames, double[] rotateCenter, double[] rotateAxis,
                                                   double rotateAngle, int numberOfCopies)
        {
            await RunGeometryTransformAsync(partNames, () => _controller.RotateGeometryPartsCommand(partNames, rotateCenter,
                                                                                                   rotateAxis, rotateAngle,
                                                                                                   numberOfCopies));
        }
        private void MirrorGeometryParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmMirror;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for mirror
            _frmMirror.PartNames = partNames;
            //
            ShowForm(_frmMirror, "Mirror parts: " + partNames.ToShortString(), null);
        }
        public async Task MirrorGeometryPartsAsync(string[] partNames, double[] mirrorPoint, double[] mirrorDirection,
                                                   bool copy)
        {
            await RunGeometryTransformAsync(partNames, () => _controller.MirrorGeometryPartsCommand(partNames, mirrorPoint,
                                                                                                   mirrorDirection, copy));
        }
        private async Task RunGeometryTransformAsync(string[] partNames, Action transform)
        {
            bool stateSet = false;
            try
            {
                if (SetStateWorking(Globals.TransformingText))
                {
                    stateSet = true;
                    //
                    if (partNames != null && partNames.Length > 0) await Task.Run(transform);
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(Globals.TransformingText);
            }
        }
        private void ScaleGeometryParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmScale;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for scaling
            _frmScale.PartNames = partNames;
            //
            ShowForm(_frmScale, "Scale parts: " + partNames.ToShortString(), null);
        }
        public async Task ScaleGeometryPartsAsync(string[] partNames, double[] scaleCenter, double[] scaleFactors, bool copy)
        {
            await RunGeometryTransformAsync(partNames, () => _controller.ScaleGeometryPartsCommand(partNames, scaleCenter,
                                                                                                  scaleFactors, copy));
        }
        // End Transform
        private void CopyGeometryPartsToResults(string[] partNames)
        {
            CloseAllForms();
            _controller.CopyGeometryPartsToResults(partNames);
        }
        private void HideGeometryParts(string[] partNames)
        {
            _controller.HideGeometryPartsCommand(partNames);            
        }
        private void ShowGeometryParts(string[] partNames)
        {
            _controller.ShowGeometryPartsCommand(partNames);
        }
        private void ShowOnlyGeometryParts(string[] partNames)
        {
            // If sub part is selected add the whole compound part
            HashSet<string> partsToShow = new HashSet<string>(partNames);
            foreach (var entry in _controller.Model.Geometry.Parts)
            {
                if (entry.Value is CompoundGeometryPart cgp)
                {
                    if (partNames.Contains(cgp.Name))
                    {
                        partsToShow.Add(cgp.Name);
                        partsToShow.UnionWith(cgp.SubPartNames);
                    }
                }
            }
            //
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Geometry.Parts.Keys);
            allNames.ExceptWith(partsToShow);
            _controller.HideGeometryPartsCommand(allNames.ToArray());
            _controller.ShowGeometryPartsCommand(partsToShow.ToArray());
        }
        private void SetColorForGeometryParts(string[] partNames)
        {
            if (_controller.Model.Geometry == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.Model.Geometry.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetColorForGeometryPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForGeometryParts(string[] partNames)
        {
            _controller.ResetColorForGeometryPartsCommand(partNames);
        }
        private void SetTransparencyForGeometryParts(string[] partNames)
        {
            if (_controller.Model.Geometry == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues =
                    new OrderedDictionary<string, double>("Preset Transparency values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetTransparencyForGeometryPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void DeleteGeometryParts(string[] partNames)
        {
            if (partNames == null || partNames.Length == 0) return;
            // A sub part of a compound is deleted like any other part: the compound loses that solid of
            // its geometry and the solids that stay are not touched
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?") == DialogResult.OK)
                _controller.RemoveGeometryPartsCommand(partNames);
        }
        //
        private async void CreateAndImportCompoundPart(string[] partNames)
        {
            try
            {
                SetStateWorking(Globals.CreatingCompoundText, true);
                //
                GeometryPart part;
                HashSet<PartType> stlPartTypes = new HashSet<PartType>();
                HashSet<PartType> cadPartTypes = new HashSet<PartType>();
                //
                string[] allPartNames = _controller.GetMeshablePartNames(partNames);
                foreach (var partName in allPartNames)
                {
                    part = (GeometryPart)_controller.Model.Geometry.Parts[partName];
                    if (part.IsCADPart) cadPartTypes.Add(part.PartType);
                    else stlPartTypes.Add(part.PartType);
                }
                if (stlPartTypes.Count + cadPartTypes.Count != 1)
                    throw new CaeException("Compound part can be made from only CAD or only stl based geometry parts " + 
                                           "of the same type.");
                await Task.Run(() => _controller.CreateAndImportCompoundPartCommand(partNames));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.CreatingCompoundText);
            }
        }
        private async void RegenerateCompoundParts(string[] compoundPartNames)
        {
            try
            {
                SetStateWorking(Globals.RegeneratingCompoundText, true);
                //
                string missingPartName = null;
                string errorCompoundPartName = null;
                CompoundGeometryPart part;
                foreach (var compoundPartName in compoundPartNames)
                {
                    part = (CompoundGeometryPart)_controller.Model.Geometry.Parts[compoundPartName];
                    if (part.CreatedFromPartNames != null && part.CreatedFromPartNames.Length > 1)
                    {
                        foreach (var createdFromPartName in part.CreatedFromPartNames)
                        {
                            if (!_controller.Model.Geometry.Parts.ContainsKey(createdFromPartName))
                            {
                                missingPartName = createdFromPartName;
                                errorCompoundPartName = compoundPartName;
                                break;
                            }
                        }
                        //
                        if (missingPartName != null) break;
                    }
                }
                if (missingPartName != null)
                    throw new CaeException("The part '" + missingPartName + "' that was used to create a compound part '" +
                                           errorCompoundPartName + "' is missing.");
                //
                await Task.Run(() => _controller.RegenerateCompoundPartsCommand(compoundPartNames));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingCompoundText);
            }
        }
        private void GroupSplitCells(string[] cellNames)
        {
            string[] compoundPartNames = cellNames.Select(_controller.GetCompoundPartNameOfBody).Distinct().ToArray();
            if (compoundPartNames.Length != 1)
                MessageBoxes.ShowError("Only the cells of one compound part can be grouped.");
            else GroupSplitCellsAndSelect(compoundPartNames[0], cellNames);
        }
        private void FuseBodies(string[] bodyNames)
        {
            string[] compoundPartNames = bodyNames.Select(_controller.GetCompoundPartNameOfBody).Distinct().ToArray();
            if (compoundPartNames.Length != 1)
                MessageBoxes.ShowError("Only the bodies of one compound part can be fused.");
            else FuseBodiesAndSelect(compoundPartNames[0], bodyNames);
        }
        // The fused cell is shown in its sub part, and a part that is whole again is selected itself. The fuse
        // replaces the compound, clears the selection and highlights the result, each of which would draw the
        // scene, so the tree and the view are both held until the result is selected and drawn once.
        // A fuse also imports the parts it replaces, and the import switches the view, the menus and the tool
        // strips, so the whole window is held, not only the tree
        private void FuseBodiesAndSelect(string compoundPartName, string[] cellNames)
        {
            bool rendering = _vtk.RenderingOn;
            _vtk.RenderingOn = false;
            SuspendWindowDrawing();
            _modelTree.BeginGeometryTreeUpdate();
            try
            {
                string fusedName = _controller.FuseBodiesCommand(compoundPartName, cellNames);
                string fusedSubPartName = _controller.GetSplitSubPartNameOfCell(fusedName);
                if (fusedSubPartName != null)
                    _modelTree.SelectSplitSubPart(_controller.GetCompoundPartNameOfBody(fusedName), fusedSubPartName);
                else if (fusedName != null) SelectBaseParts(new string[] { fusedName });
            }
            finally
            {
                _modelTree.EndGeometryTreeUpdate();
                ResumeWindowDrawing();
                _vtk.RenderingOn = rendering;
            }
        }
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
        private const int WM_SETREDRAW = 0x000B;
        private const uint RDW_INVALIDATE = 0x0001;
        private const uint RDW_ERASE = 0x0004;
        private const uint RDW_ALLCHILDREN = 0x0080;
        private const uint RDW_FRAME = 0x0400;
        // The window and every control in it stop painting until drawing is resumed, when all of them are
        // painted once
        private void SuspendWindowDrawing()
        {
            SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }
        private void ResumeWindowDrawing()
        {
            SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RDW_INVALIDATE | RDW_ERASE | RDW_ALLCHILDREN | RDW_FRAME);
        }
        // The grouping rebuilds the tree, which is painted once, with the sub part the cells went to already
        // selected and expanded, instead of first with the cells that were selected before
        private void GroupSplitCellsAndSelect(string compoundPartName, string[] cellNames)
        {
            _modelTree.BeginGeometryTreeUpdate();
            try
            {
                string subPartName = _controller.GroupSplitCellsCommand(compoundPartName, cellNames, null);
                _modelTree.SelectSplitSubPart(compoundPartName, subPartName);
            }
            finally
            {
                _modelTree.EndGeometryTreeUpdate();
            }
        }
        private void SwapPartGeometries(string[] partNames)
        {
            GeometryPart[] parts = _controller.GetGeometryPartsWithoutSubParts();
            GeometryPart part1 = _controller.GetGeometryPart(partNames[0]);
            GeometryPart part2 = _controller.GetGeometryPart(partNames[1]);
            if (parts.Contains(part1) && parts.Contains(part2))
            {
                if (part1 is CompoundGeometryPart || part2 is CompoundGeometryPart)
                    MessageBoxes.ShowError("Compound parts cannot be swapped.");
                else
                    _controller.SwapPartGeometriesCommand(partNames[0], partNames[1]);
            }
            else MessageBoxes.ShowError("Compound subparts cannot be swapped.");
        }
        // Analyze geometry
        private void AnalyzeGeometry(string[] partNames)
        {
            if (!_frmAnalyzeGeometry.Visible)
            {
                CloseAllForms();
                SetFormLocation((Form)_frmAnalyzeGeometry);
                _frmAnalyzeGeometry.PartNamesToAnalyze = partNames;
                _frmAnalyzeGeometry.Show();
            }
        }

        #endregion  ################################################################################################################

        #region Geometry CAD part menu   ###########################################################################################
        private void tsmiConvertShellToSolidCAD_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetCADGeometryShellParts(), ConvertShellToSolid);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void TsmiConvertSolidToShellCAD_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetCADGeometrySolidParts(), ConvertSolidToShell);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiFlipFaceNormalCAD_Click(object sender, EventArgs e)
        {
            try
            {
                // Must be outside the await part otherwise causes screen flickering
                AnnotateWithColorEnum _prevShowWithColor = _controller.AnnotateWithColor;
                _controller.AnnotateWithColor = AnnotateWithColorEnum.FaceOrientation;
                //
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = FlipFaces;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to flip", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
                //
                _controller.AnnotateWithColor = _prevShowWithColor;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.FlippingNormalsText);
            }
        }
        private void tsmiSplitSolid_Click(object sender, EventArgs e)
        {
            try
            {
                CreateSplitSolid();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSplitShell_Click(object sender, EventArgs e)
        {
            try
            {
                CreateSplitShell();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Geometry feature
        private void CreateSplitSolid()
        {
            if (_controller.Model.Geometry == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmSplitSolid);
            ShowForm(_frmSplitSolid, "Split Solid", null);
        }
        private void CreateSplitShell()
        {
            if (_controller.Model.Geometry == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmSplitShell);
            ShowForm(_frmSplitShell, "Split Shell", null);
        }
        private void tsmiOffsetFace_Click(object sender, EventArgs e)
        {
            try
            {
                CreateOffsetFace();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void CreateOffsetFace()
        {
            if (_controller.Model.Geometry == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmOffsetFace);
            ShowForm(_frmOffsetFace, "Offset Face", null);
        }
        private async void tsmiDefeature_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = Defeature;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to defeature", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.DefeaturingText);
            }
        }
        private async void tsmiDeleteFacesCAD_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = DeleteCadPartFaces;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to delete", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.DeletingFacesText);
            }
        }
        //
        private void ConvertShellToSolid(string[] partNames)
        {
            _controller.ConvertShellToSolidCommand(partNames);
        }
        private void ConvertSolidToShell(string[] partNames)
        {
            _controller.ConvertSolidToShellCommand(partNames);
        }
        private void FlipFaces(GeometrySelection geometrySelection)
        {
            try
            {
                SetStateWorking(Globals.FlippingNormalsText);
                _controller.FlipFaceOrientationsCommand(geometrySelection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetStateReady(Globals.FlippingNormalsText);
            }
        }
        private void Defeature(GeometrySelection geometrySelection)
        {
            try
            {
                SetStateWorking(Globals.DefeaturingText, true);
                _controller.DefeatureCommand(geometrySelection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                SetStateReady(Globals.DefeaturingText);
            }
        }
        private void DeleteCadPartFaces(GeometrySelection geometrySelection)
        {
            try
            {
                SetStateWorking(Globals.DeletingFacesText);
                _controller.DeleteCadPartFacesCommand(geometrySelection);
            }
            finally
            {
                SetStateReady(Globals.DeletingFacesText);
            }
        }


        #endregion #################################################################################################################

        #region Geometry Stl part menu   ###########################################################################################
        private void tsmiFindStlEdgesByAngleForGeometryParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADGeometryParts(), FindEdgesByAngleForGeometryParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiFlipStlPartFaceNormals_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADGeometryParts(), FlipStlPartSurfacesNormal);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSmoothStlPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetNonCADGeometryParts(), _controller.SmoothGeometryPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiDeleteStlPartFaces_Click(object sender, EventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    _frmSelectGeometry.HideFormOnOK = false;
                    _frmSelectGeometry.SelectionFilter = SelectGeometryEnum.Surface;
                    _frmSelectGeometry.OnOKCallback = DeleteStlPartFaces;
                    //
                    InvokeIfRequired(() => { ShowForm(_frmSelectGeometry, "Select faces to delete", null); });
                    while (_frmSelectGeometry.Visible) Thread.Sleep(100);
                });
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.DeletingFacesText);
            }
        }
        private void tsmiCropStlPartWithCylinder_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), _controller.CropGeometryPartWithCylinder);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCropStlPartWithCube_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetGeometryParts(), _controller.CropGeometryPartWithCube);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void FlipStlPartSurfacesNormal(string[] partNames)
        {
            _controller.FlipStlPartSurfacesNormalCommand(partNames);
        }
        private void FindEdgesByAngleForGeometryParts(string[] partNames)
        {
            using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
            {
                SetUpFrmGetValueForEdgeAngle(frmGetValue, partNames);
                //
                if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.FindEdgesByAngleForGeometryPartsCommand(partNames, frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void SetUpFrmGetValueForEdgeAngle(FrmGetValue frmGetValue, string[] partNames)
        {
            frmGetValue.NumOfDigits = 2;
            frmGetValue.MinValue = 0;
            frmGetValue.MaxValue = 90;
            SetFormLocation(frmGetValue);
            OrderedDictionary<string, double> presetValues =
                new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
            presetValues.Add("Default", CaeMesh.Globals.EdgeAngle);
            string desc = "Enter the face angle for model edges detection.";
            frmGetValue.PrepareForm("Find model edges: " + partNames.ToShortString(), "Angle", desc,
                                    CaeMesh.Globals.EdgeAngle, presetValues, new StringAngleDegConverter());
        }
        private void DeleteStlPartFaces(GeometrySelection geometrySelection)
        {
            SetStateWorking(Globals.DeletingFacesText);
            _controller.DeleteStlPartFacesCommand(geometrySelection);
            SetStateReady(Globals.DeletingFacesText);
        }
        
        #endregion #################################################################################################################

        #region Mesh ###############################################################################################################
        // Mesh setup item
        private void tsmiCreateMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                CreateMeshSetupItem();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Mesh Setup Items", _controller.GetMeshSetupItems(), EditMeshSetupItem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Mesh Setup Items", _controller.GetMeshSetupItems(), DuplicateMeshSetupItems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteMeshSetupItem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Mesh Setup Items", _controller.GetMeshSetupItems(), DeleteMeshSetupItems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Preview
        private void tsmiPreviewEdgeMesh_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), PreviewEdgeMesh);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Create mesh
        internal void tsmiCreateMesh_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreatePartMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }            
        }
        // Mesh setup item
        private void CreateMeshSetupItem()
        {
            if (_controller.Model.Geometry == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmMeshSetupItem);
            ShowForm(_frmMeshSetupItem, "Create Mesh Setup Item", null);
        }
        private void EditMeshSetupItem(string meshSetupItemName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmMeshSetupItem);
            ShowForm(_frmMeshSetupItem, "Edit Mesh Setup Item", meshSetupItemName);
        }
        private void DuplicateMeshSetupItems(string[] meshSetupItemNames)
        {
            _controller.DuplicateMeshSetupItemsCommand(meshSetupItemNames);
        }
        private void DeleteMeshSetupItems(string[] meshSetupItemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected mesh setup items?" + Environment.NewLine
                                                 + meshSetupItemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveMeshSetupItemsCommand(meshSetupItemNames);
            }
        }
        public MeshSetupItem EditMeshingParametersFromHistory(MeshSetupItem meshSetupItem)
        {
            InvokeIfRequired(() =>
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, true, null);
                //
                CloseAllForms();
                SetFormLocation(_frmMeshSetupItem);
                _frmMeshSetupItem.Text = "Redefine " + meshSetupItem.Name;
                //
                _frmMeshSetupItem.PrepareForm(null, null);
                _frmMeshSetupItem.SetMeshSetupItem(meshSetupItem.DeepClone());
                _frmMeshSetupItem.ShowDialog(this);
                //
                if (_frmMeshSetupItem.DialogResult == DialogResult.OK) meshSetupItem = _frmMeshSetupItem.MeshSetupItem;
            });
            //
            return meshSetupItem;
        }
        // Preview
        private async void PreviewEdgeMesh(string[] partNames)
        {
            await PreviewEdgeMeshAsync(partNames, null);
        }
        private async Task PreviewEdgeMeshAsync(string[] partNames, MeshSetupItem meshSetupItem)
        {
            bool stateSet = false;
            try
            {
                if (SetStateWorking(Globals.PreviewText))
                {
                    stateSet = true;
                    //
                    await Task.Run(() =>
                    {
                        foreach (var partName in partNames)
                        {
                            _controller.PreviewEdgeMesh(partName, meshSetupItem);
                        }
                    });
                }
                else MessageBoxes.ShowWarning("Another task is already running.");
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                if (stateSet) SetStateReady(Globals.PreviewText);
            }
        }
        // Create mesh
        private async void CreatePartMeshes(string[] partNames)
        {
            try
            {
                List<string> errors = new List<string>();
                SetStateWorking(Globals.MeshingText, true);
                MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0);
                Keys modifierKeys = Keys.Control;
                _modelTree.ClearTreeSelection(ViewType.Model);                
                //
                CloseAllForms();
                //
                Stopwatch watch = new Stopwatch();
                watch.Start();
                //
                await Task.Run(() => _controller.CreateMeshesCommand(partNames));
                //
                errors.AddRange(_controller.LastMeshingErrors);
                if (IsStateWorking())
                {
                    foreach (string partName in _controller.LastMeshedModelPartNames)
                    {
                        MeshPart part = _controller.GetModelPart(partName);
                        _modelTree.SelectBasePart(e, modifierKeys, part, true);
                    }
                }
                watch.Stop();
                if (partNames.Length > 1)
                {
                    WriteDataToOutput("");
                    WriteDataToOutput("Elapsed time [s]: " + watch.Elapsed.TotalSeconds.ToString());
                }
                //
                _controller.UpdateExplodedView(true);
                //
                string[] invertedPartNames = _controller.LastInvertedElementPartNames;
                if (errors.Count > 0)
                {
                    string message = "Errors occurred during meshing. Please check the output window.";
                    if (_controller.HighlightLastMeshingErrorGeometry())
                        message += " The surfaces and edges the errors name are highlighted; click a name in the " +
                                   "output window to show that one alone.";
                    else if (_controller.GetLastMeshingErrorGeometryIds().Length > 0)
                        message += " Click a surface or an edge the errors name in the output window to show it in the " +
                                   "geometry.";
                    if (invertedPartNames.Length > 0)
                        message += " The mesh of " + string.Join(", ", invertedPartNames) + " has inverted or " +
                                   "degenerate elements; click its warning in the output window to show them.";
                    MessageBoxes.ShowError(message);
                }
                else if (invertedPartNames.Length > 0)
                {
                    _controller.ClearAllSelection();
                    _controller.HighlightInvertedElements(invertedPartNames);
                    MessageBoxes.ShowWarning("The mesh of " + string.Join(", ", invertedPartNames) + " has inverted " +
                                             "or degenerate elements, which are highlighted. Click a warning in the " +
                                             "output window to show the elements of that part alone.");
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.MeshingText);
            }
        }
        // Advisor
        internal void CreateDefaultMesh(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreateDefaultMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void CreateUserDefinedMesh(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetGeometryPartsWithoutSubParts(), CreateUserDefinedMeshes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void CreateDefaultMeshes(string[] partNames)
        {
            CreatePartMeshes(partNames);
        }
        private async void CreateUserDefinedMeshes(string[] partNames)
        {
            await Task.Run(() =>
            {
                InvokeIfRequired(() => { CreateMeshSetupItem(); });
                //
                bool firstTime = true;
                do
                {
                    Thread.Sleep(250);
                    if (firstTime)
                    {
                        Selection selection = new Selection();
                        selection.Add(new SelectionNodeIds(vtkSelectOperation.None, false,
                                                           _controller.Model.Geometry.GetPartIdsFomPartNames(partNames)));
                        selection.SelectItem = vtkSelectItem.Part;
                        selection.CurrentView = (int)ViewGeometryModelResults.Geometry;
                        _controller.Selection = selection;
                        _controller.SetSelectByToOff();
                        //
                        InvokeIfRequired(() => {
                            //_controller.HighlightSelection();
                            SelectionChanged(_controller.Model.Geometry.GetPartIdsFomPartNames(partNames));
                        });
                        //
                        firstTime = false;
                    }
                }
                while (_frmMeshSetupItem.Visible);

            });
            if (_frmMeshSetupItem.DialogResult == DialogResult.OK) CreatePartMeshes(partNames);
        }

        #endregion  ################################################################################################################

        #region Model edit  ########################################################################################################
        private void tsmiEditModel_Click(object sender, EventArgs e)
        {
            try
            {
                EditModel();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditCalculiXKeywords_Click(object sender, EventArgs e)
        {
            try
            {
                EditCalculiXKeywords();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Tools
        private void tsmiFindEdgesByAngleForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetNonCADModelParts(), FindEdgesByAngleForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCreateBoundaryLayer_Click(object sender, EventArgs e)
        {
            try
            {
                CreateBoundaryLayer();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRemeshElements_Click(object sender, EventArgs e)
        {
            try
            {
                RemeshElements();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiThickenShellMesh_Click(object sender, EventArgs e)
        {
            try
            {
                ThickenShellMesh();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSplitPartMeshUsingSurface_Click(object sender, EventArgs e)
        {
            try
            {
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSplitPartMeshUsingSurface);
                ShowForm(_frmSplitPartMeshUsingSurface, "Split Part Mesh Using Surface", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiUpdateNodalCoordinatesFromFile_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_controller.ModelInitialized || _controller.Model.Mesh == null) return;
                //
                string fileName = GetFileNameToImport("", "Abaqus/Calculix inp files|*.inp");
                //
                SetStateWorking(Globals.ImportingText);
                await Task.Run(() => _controller.UpdateNodalCoordinatesFromFileCommand(fileName));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ImportingText);
            }
        }
        //                                                                                                                          
        private void EditModel()
        {
            if (_controller.Model.UnitSystem.UnitSystemType != UnitSystemType.Undefined)
                ShowForm(_frmModelProperties, "Edit Model", null);

        }
        private void EditCalculiXKeywords()
        {
            // This is also called from the model tree - needs try, catch
            try
            {
                if (CheckValidity())
                {
                    using (_frmCalculixKeywordEditor = new FrmCalculixKeywordEditor())
                    {
                        _frmCalculixKeywordEditor.Icon = this.Icon;
                        _frmCalculixKeywordEditor.Keywords = _controller.GetCalculixModelKeywords();
                        _frmCalculixKeywordEditor.UserKeywords = _controller.GetCalculixUserKeywords();
                        _frmCalculixKeywordEditor.KeywordOverrides = _controller.GetCalculixKeywordOverrides();
                        //
                        if (_frmCalculixKeywordEditor.Keywords != null)
                        {
                            _frmCalculixKeywordEditor.PrepareForm(); // must be here to check for errors
                            if (_frmCalculixKeywordEditor.ShowDialog(this) == DialogResult.OK)
                            {
                                _controller.SetCalculixKeywordEditorStateCommand(
                                    _frmCalculixKeywordEditor.UserKeywords, _frmCalculixKeywordEditor.KeywordOverrides);
                                _controller.CheckAndUpdateModelValidity(); // update keyword states
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Tools
        private void FindEdgesByAngleForModelParts(string[] partNames)
        {
            using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
            {
                SetUpFrmGetValueForEdgeAngle(frmGetValue, partNames);
                //
                if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.FindEdgesByAngleForModelPartsCommand(partNames, frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void CreateBoundaryLayer()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryLayer);
            ShowForm(_frmBoundaryLayer, "Create Boundary Layer", null);
        }
        private void RemeshElements()
        {
            if (_controller.Model == null || _controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmRemeshingParameters);
            ShowForm(_frmRemeshingParameters, "Remeshing Parameters", null);
        }
        private void ThickenShellMesh()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmThickenShellMesh);
            ShowForm(_frmThickenShellMesh, "Thicken Shell Mesh", null);
        }
        private async void SplitPartMeshUsingSurface(SplitPartMeshData splitPartMeshData)
        {
            try
            {
                if (!_controller.ModelInitialized || _controller.Model.Mesh == null) return;
                //
                SetStateWorking(Globals.MeshingText, true);
                await Task.Run(() => _controller.SplitPartMeshUsingSurfaceCommand(splitPartMeshData));
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.MeshingText);
            }
        }
        public void ShowSplitMeshResults()
        {
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void UpdateNodalCoordinatesFromFile()
        {

        }

        #endregion  ################################################################################################################

        #region Node menu  #########################################################################################################
        private void tsmiRenumberAllNodes_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
                {
                    frmGetValue.NumOfDigits = 0;
                    frmGetValue.MinValue = 1;
                    SetFormLocation(frmGetValue);
                    string desc = "Enter the starting node id for the node renumbering.";
                    frmGetValue.PrepareForm("Renumber Nodes", "Start node id", desc, 1, null);
                    if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                    {
                        _controller.RenumberNodesCommand((int)frmGetValue.Value);
                    }
                    SaveFormLocation(frmGetValue);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeCoincidentNodes_Click(object sender, EventArgs e)
        {
            try
            {
                MergeCoincidentNodes();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //                                                                                                                          
        private void MergeCoincidentNodes()
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmMergeCoincidentNodes);
            ShowForm(_frmMergeCoincidentNodes, "Merge Coincident Nodes", null);
        }
        #endregion  ################################################################################################################

        #region Element menu  ######################################################################################################
        private void tsmiRenumberAllElements_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
                {
                    frmGetValue.NumOfDigits = 0;
                    frmGetValue.MinValue = 1;
                    SetFormLocation(frmGetValue);
                    string desc = "Enter the starting element id for the element renumbering.";
                    frmGetValue.PrepareForm("Renumber Elements", "Start element id", desc, 1, null);
                    if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                    {
                        _controller.RenumberElementsCommand((int)frmGetValue.Value);
                    }
                    SaveFormLocation(frmGetValue);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiElementQuality_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ElementQuality);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void ElementQuality(string[] partNames)
        {
            // Set all part names for quality analysis
            _frmElementQuality.PartNames = partNames;
            //
            ShowForm(_frmElementQuality, "Element Quality", null);
        }
        #endregion  ################################################################################################################

        #region Model part menu  ###################################################################################################
        private void tsmiEditModelPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetModelParts(), EditModelPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Transform
        private void tsmiTranslateModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), TranslateModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRotateModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), RotateModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMirrorModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), MirrorModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiScaleModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ScaleModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiMergeModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), MergeModelParts, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), HideModelParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ShowModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ShowOnlyModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), SetColorForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), ResetColorForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), SetTransparencyForModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetModelParts(), DeleteModelParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //                                                                                                                          
        private void EditModelPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Model; 
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        // Transform
        private void TranslateModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmTranslate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for translation
            _frmTranslate.PartNames = partNames;    
            //
            ShowForm(_frmTranslate, "Translate Parts: " + partNames.ToShortString(), null);
        }
        private void RotateModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmRotate;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for rotation
            _frmRotate.PartNames = partNames;
            //
            ShowForm(_frmRotate, "Rotate Parts: " + partNames.ToShortString(), null);
        }
        private void MirrorModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmMirror;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for mirror
            _frmMirror.PartNames = partNames;    
            //
            ShowForm(_frmMirror, "Mirror Parts: " + partNames.ToShortString(), null);
        }
        private void ScaleModelParts(string[] partNames)
        {
            SinglePointDataEditor.ParentForm = _frmScale;
            SinglePointDataEditor.Controller = _controller;
            // Set all part names for scaling
            _frmScale.PartNames = partNames;
            //
            ShowForm(_frmScale, "Scale Parts: " + partNames.ToShortString(), null);
        }
        //
        private void MergeModelParts(string[] partNames)
        {
            if (_controller.AreModelPartsMergeable(partNames))
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to merge selected parts?") == DialogResult.OK)
                {
                    _controller.MergeModelPartsCommand(partNames);
                }
            }
            else MessageBoxes.ShowError("Selected parts are of a different type and thus cannot be merged.");
        }
        private void HideModelParts(string[] partNames)
        {
            _controller.HideModelPartsCommand(partNames);
        }
        private void ShowModelParts(string[] partNames)
        {
            _controller.ShowModelPartsCommand(partNames);
        }
        private void ShowOnlyModelParts(string[] partNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Mesh.Parts.Keys);
            allNames.ExceptWith(partNames);
            _controller.HideModelPartsCommand(allNames.ToArray());
            _controller.ShowModelPartsCommand(partNames);
        }
        private void SetColorForModelParts(string[] partNames)
        {
            if (_controller.Model.Mesh == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.Model.Mesh.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetColorForModelPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForModelParts(string[] partNames)
        {
            _controller.ResetColorForModelPartsCommand(partNames);
        }
        private void SetTransparencyForModelParts(string[] partNames)
        {
            if (_controller.Model.Mesh == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues
                    = new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetTransparencyForModelPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void DeleteModelParts(string[] partNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?" + Environment.NewLine
                                                 + partNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelPartsCommand(partNames);
            }
        }
        
        #endregion  ################################################################################################################

        #region Node set menu  #####################################################################################################
        private void tsmiCreateNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmNodeSet);
                ShowForm(_frmNodeSet, "Create Node Set", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Node Sets", _controller.GetUserNodeSets(), EditNodeSet);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Node Sets", _controller.GetUserNodeSets(), DuplicateNodeSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteNodeSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Node Sets", _controller.GetUserNodeSets(), DeleteNodeSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditNodeSet(string nodeSetName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmNodeSet);
            ShowForm(_frmNodeSet, "Edit Node Set", nodeSetName);
        }
        private void DuplicateNodeSets(string[] nodeSetNames)
        {
            _controller.DuplicateNodeSetsCommand(nodeSetNames);
        }
        private void DeleteNodeSets(string[] nodeSetNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected node sets?" + Environment.NewLine
                                                 + nodeSetNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveNodeSetsCommand(nodeSetNames);
            }
        }

        #endregion  ################################################################################################################

        #region Element set menu  ##################################################################################################
        private void tsmiCreateElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmElementSet);
                ShowForm(_frmElementSet, "Create Element Set", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Element Sets", _controller.GetUserElementSets(), EditElementSet);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), DuplicateElementSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiConvertElementSetsToMeshParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), ConvertElementSetsToMeshParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteElementSet_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Element Sets", _controller.GetUserElementSets(), DeleteElementSets);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditElementSet(string elementSetName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmElementSet);
            ShowForm(_frmElementSet, "Edit Element Set", elementSetName);
        }
        private void DuplicateElementSets(string[] elementSetNames)
        {
            _controller.DuplicateElementSetsCommand(elementSetNames);
        }
        private void ConvertElementSetsToMeshParts(string[] elementSetNames)
        {
            _controller.ConvertElementSetsToMeshPartsCommand(elementSetNames);
        }
        private void DeleteElementSets(string[] elementSetNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected element sets?" + Environment.NewLine
                                                 + elementSetNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveElementSetsCommand(elementSetNames);
            }
        }

        #endregion  ################################################################################################################

        #region Surface menu  ######################################################################################################
        private void tsmiCreateSurface_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSurface);
                ShowForm(_frmSurface, "Create Surface", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Surfaces", _controller.GetUserSurfaces(), EditSurface);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surfaces", _controller.GetUserSurfaces(), DuplicateSurfaces);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteSurface_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surfaces", _controller.GetUserSurfaces(), DeleteSurfaces);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditSurface(string surfaceName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSurface);
            ShowForm(_frmSurface, "Edit Surface", surfaceName);
        }
        private void DuplicateSurfaces(string[] surfaceNames)
        {
            _controller.DuplicateSurfacesCommand(surfaceNames);
        }
        private void DeleteSurfaces(string[] surfaceNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected surfaces?" + Environment.NewLine
                                                 + surfaceNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSurfacesCommand(surfaceNames);
            }
        }

        #endregion  ################################################################################################################

        #region Model Reference point  #############################################################################################
        private void tsmiCreateModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
                ShowForm(_frmReferencePoint, "Create Model Reference Point", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Model Reference Points", _controller.GetAllModelReferencePoints(), EditModelReferencePoint);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       DuplicateModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       HideModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       ShowModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       ShowOnlyModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Reference Points", _controller.GetAllModelReferencePoints(),
                                       DeleteModelReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditModelReferencePoint(string referencePointName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
            ShowForm(_frmReferencePoint, "Edit Reference Point", referencePointName);
        }
        private void DuplicateModelReferencePoints(string[] referencePointNames)
        {
            _controller.DuplicateModelReferencePointsCommand(referencePointNames);
        }
        private void HideModelReferencePoints(string[] referencePointNames)
        {
            _controller.HideModelReferencePointsCommand(referencePointNames);
        }
        private void ShowModelReferencePoints(string[] referencePointNames)
        {
            _controller.ShowModelReferencePointsCommand(referencePointNames);
        }
        private void ShowOnlyModelReferencePoints(string[] referencePointNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.GetModelReferencePointNames());
            allNames.ExceptWith(referencePointNames);
            _controller.HideModelReferencePointsCommand(allNames.ToArray());
            _controller.ShowModelReferencePointsCommand(referencePointNames);
        }
        private void DeleteModelReferencePoints(string[] referencePointNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected model reference points?" + Environment.NewLine
                                                 + referencePointNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelReferencePointsCommand(referencePointNames);
            }
        }

        #endregion  ################################################################################################################

        #region Model Coordinate system  ###########################################################################################
        private void tsmiCreateModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
                ShowForm(_frmCoordinateSystem, "Create Model Coordinate System", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                EditModelCoordinateSystem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       DuplicateModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       HideModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       ShowModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       ShowOnlyModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteModelCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Model Coordinate Systems", _controller.GetAllModelCoordinateSystems(),
                                       DeleteModelCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditModelCoordinateSystem(string coordinateSystemName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
            ShowForm(_frmCoordinateSystem, "Edit Coordinate System", coordinateSystemName);
        }
        private void DuplicateModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.DuplicateModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void HideModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.HideModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowModelCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.ShowModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowOnlyModelCoordinateSystems(string[] coordinateSystemNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Mesh.CoordinateSystems.Keys);
            allNames.ExceptWith(coordinateSystemNames);
            _controller.HideModelCoordinateSystemsCommand(allNames.ToArray());
            _controller.ShowModelCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void DeleteModelCoordinateSystems(string[] coordinateSystemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected model coordinate systems?" + Environment.NewLine
                                                 + coordinateSystemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveModelCoordinateSystemsCommand(coordinateSystemNames);
            }
        }

        #endregion  ################################################################################################################

        #region Material menu  #####################################################################################################
        internal void tsmiCreateMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmMaterial, "Create Material", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void CreateSimpleMaterial(object sender, EventArgs e)
        {
            _frmMaterial.UseSimpleEditor = true;
            tsmiCreateMaterial_Click(sender, e);
        }
        private void tsmiEditMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Materials", _controller.GetAllMaterials(), EditMaterial);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), DuplicateMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private  void tsmiImportMaterial_Click(object sender, EventArgs e)
        {
            ImportFile(true);
        }
        private void tsmiExportMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), ExportMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteMaterial_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Materials", _controller.GetAllMaterials(), DeleteMaterials);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
           
        }
        //
        private void EditMaterial(string materialName)
        {
            ShowForm(_frmMaterial, "Edit Material", materialName);
        }
        private void DuplicateMaterials(string[] materialNames)
        {
            _controller.DuplicateMaterialsCommand(materialNames);
        }
        private async void ExportMaterials(string[] materialNames)
        {
            try
            {
                _controller.CurrentView = ViewGeometryModelResults.Model;
                //
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Calculix files | *.inp";
                    //
                    if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        // The filter adds the extension to the file name
                        SetStateWorking(Globals.ExportingText);
                        //
                        await Task.Run(() => _controller.ExportMaterials(materialNames, saveFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.ExportingText);
            }
        }
        private void DeleteMaterials(string[] materialNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected materials?" + Environment.NewLine
                                                 + materialNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveMaterialsCommand(materialNames);
            }
        }
        //
        internal void tsmiMaterialLibrary_Click(object sender, EventArgs e)
        {
            try
            {
                ShowMaterialLibrary();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void ShowMaterialLibrary()
        {
            if (_controller.Model.Mesh != null)
            {
                using (FrmMaterialLibrary fml = new FrmMaterialLibrary(_controller))
                {
                    CloseAllForms();
                    SetFormLocation(fml);
                    fml.ShowDialog(this);
                }
            }
        }

        #endregion  ################################################################################################################

        #region Section menu  ######################################################################################################
        internal void tsmiCreateSection_Click(object sender, EventArgs e)
        {
            try
            {
                CreateSection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSection_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Sections", _controller.GetAllSections(), EditSection);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSection_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Sections", _controller.GetAllSections(), DuplicateSections);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDelete_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Sections", _controller.GetAllSections(), DeleteSections);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateSection()
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSection);
            ShowForm(_frmSection, "Create Section", null);
        }
        private void EditSection(string sectionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmSection);
            ShowForm(_frmSection, "Edit Section", sectionName);
        }
        private void DuplicateSections(string[] sectionNames)
        {
            _controller.DuplicateSectionsCommand(sectionNames);
        }
        private void DeleteSections(string[] sectionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected sections?" + Environment.NewLine
                                                 + sectionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSectionsCommand(sectionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Interaction menu  ##################################################################################################

        #region Constraint menu  ###################################################################################################
        private void tsmiCreateConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmConstraint);
                //
                SinglePointDataEditor.ParentForm = _frmConstraint;
                SinglePointDataEditor.Controller = _controller;
                //
                ShowForm(_frmConstraint, "Create Constraint", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditConstraint_Click(object sender, EventArgs e)
        {
             try
             {
                 SelectOneEntity("Constraints", _controller.GetAllConstraints(), EditConstraint);
             }
             catch (Exception ex)
             {
                 ExceptionTools.Show(this, ex);
             }
        }
        private void tsmiDuplicateConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), DuplicateConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapMasterSlaveConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), SwapMasterSlaveConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeByMasterSlaveConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), MergeByMasterSlaveConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), HideConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), ShowConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteConstraint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Constraints", _controller.GetAllConstraints(), DeleteConstraints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditConstraint(string constraintName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmConstraint);
            //
            SinglePointDataEditor.ParentForm = _frmConstraint;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmConstraint, "Edit Constraint", constraintName);
        }
        private void DuplicateConstraints(string[] constraintNames)
        {
            _controller.DuplicateConstraintsCommand(constraintNames);
        }
        private void SwapMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.SwapMasterSlaveConstraintsCommand(constraintNames);
        }
        private void MergeByMasterSlaveConstraints(string[] constraintNames)
        {
            _controller.MergeByMasterSlaveConstraintsCommand(constraintNames);
        }
        private void HideConstraints(string[] constraintNames)
        {
            _controller.HideConstraintsCommand(constraintNames);
        }
        private void ShowConstraints(string[] constraintNames)
        {
            _controller.ShowConstraintsCommand(constraintNames);
        }
        private void ShowOnlyConstraints(string[] constraintNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.Constraints.Keys);
            allNames.ExceptWith(constraintNames);
            _controller.HideConstraintsCommand(allNames.ToArray());
            _controller.ShowConstraintsCommand(constraintNames);
        }
        private void DeleteConstraints(string[] constraintNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected constraints?" + Environment.NewLine
                                                 + constraintNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveConstraintsCommand(constraintNames);
            }
        }

        #endregion  ################################################################################################################
        
        #region Surface interaction menu  ##########################################################################################
        private void tsmiCreateSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmSurfaceInteraction, "Create surface interaction", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Surface interactions", _controller.GetAllSurfaceInteractions(), EditSurfaceInteraction);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surface interactions", _controller.GetAllSurfaceInteractions(),
                                       DuplicateSurfaceInteractions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteSurfaceInteraction_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Surface interactions", _controller.GetAllSurfaceInteractions(), DeleteSurfaceInteractions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }

        }
        //
        private void EditSurfaceInteraction(string surfaceInteractionName)
        {
            ShowForm(_frmSurfaceInteraction, "Edit surface interaction", surfaceInteractionName);
        }
        private void DuplicateSurfaceInteractions(string[] surfaceInteractionNames)
        {
            _controller.DuplicateSurfaceInteractionsCommand(surfaceInteractionNames);
        }
        private void DeleteSurfaceInteractions(string[] surfaceInteractionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected surface interactions?" + Environment.NewLine
                                                 + surfaceInteractionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveSurfaceInteractionsCommand(surfaceInteractionNames);
            }
        }



        #endregion  ################################################################################################################

        #region Contact pair menu  #################################################################################################
        private void tsmiCreateContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                // Data editor
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmContactPair);
                ShowForm(_frmContactPair, "Create Contact Pair", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Contact pairs", _controller.GetAllContactPairs(), EditContactPair);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), DuplicateContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSwapMasterSlaveContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), SwapMasterSlaveContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeByMasterSlaveContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), MergeByMasterSlaveContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), HideContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), ShowContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteContactPair_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Contact pairs", _controller.GetAllContactPairs(), DeleteContactPairs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditContactPair(string contactPairName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmContactPair);
            ShowForm(_frmContactPair, "Edit Contact Pair", contactPairName);
        }
        private void DuplicateContactPairs(string[] contactPairNames)
        {
            _controller.DuplicateContactPairsCommand(contactPairNames);
        }
        private void SwapMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.SwapMasterSlaveContactPairsCommand(contactPairNames);
        }
        private void MergeByMasterSlaveContactPairs(string[] contactPairNames)
        {
            _controller.MergeByMasterSlaveContactPairsCommand(contactPairNames);
        }
        private void HideContactPairs(string[] contactPairNames)
        {
            _controller.HideContactPairsCommand(contactPairNames);
        }
        private void ShowContactPairs(string[] contactPairNames)
        {
            _controller.ShowContactPairsCommand(contactPairNames);
        }
        private void ShowOnlyContactPairs(string[] contactPairNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.ContactPairs.Keys);
            allNames.ExceptWith(contactPairNames);
            _controller.HideContactPairsCommand(allNames.ToArray());
            _controller.ShowContactPairsCommand(contactPairNames);
        }
        private void DeleteContactPairs(string[] contactPairNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected contact pairs?" + Environment.NewLine
                                                 + contactPairNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveContactPairsCommand(contactPairNames);
            }
        }
        #endregion  ################################################################################################################

        private void tsmiSearchContactPairs_Click(object sender, EventArgs e)
        {
            try
            {
                SearchContactPairs();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void SearchContactPairs()
        {
            if (_controller.Model == null || _controller.Model.Mesh == null) return;
            //
            if (!_frmSearchContactPairs.Visible)
            {
                CloseAllForms();
                SetFormLocation(_frmSearchContactPairs);
                _frmSearchContactPairs.PrepareForm();
                _frmSearchContactPairs.Show(this);
            }
        }

        #endregion  ################################################################################################################

        #region Distribution menu  #################################################################################################
        private void tsmiCreateDistribution_Click(object sender, EventArgs e)
        {
            try
            {
                CreateDistribution();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void tsmiEditDistribution_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Distributions", _controller.GetAllDistributions(), EditDistribution);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void tsmiDuplicateDistribution_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Distributions", _controller.GetAllDistributions(), DuplicateDistributions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void tsmiDeleteDistribution_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Distributions", _controller.GetAllDistributions(), DeleteDistributions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateDistribution()
        {
            if (_controller.Model.Mesh == null) return;
            ShowForm(_frmDistribution, "Create Distribution", null);
        }
        private void EditDistribution(string amplitudeName)
        {
            ShowForm(_frmDistribution, "Edit Distribution", amplitudeName);
        }
        private void DuplicateDistributions(string[] distributionNames)
        {
            _controller.DuplicateDistributionsCommand(distributionNames);
        }
        private void DeleteDistributions(string[] distributionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected distribution?" + Environment.NewLine
                                                 + distributionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveDistributionsCommand(distributionNames);
            }
        }
        #endregion  ################################################################################################################

        #region Amplitude menu  ####################################################################################################
        private void tsmiCreateAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                CreateAmplitude();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Amplitudes", _controller.GetAllAmplitudes(), EditAmplitude);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Amplitudes", _controller.GetAllAmplitudes(), DuplicateAmplitudes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAmplitude_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Amplitudes", _controller.GetAllAmplitudes(), DeleteAmplitudes);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateAmplitude()
        {
            if (_controller.Model.Mesh == null) return;
            ShowForm(_frmAmplitude, "Create Amplitude", null);
        }
        private void EditAmplitude(string amplitudeName)
        {
            ShowForm(_frmAmplitude, "Edit Amplitude", amplitudeName);
        }
        private void DuplicateAmplitudes(string[] amplitudeNames)
        {
            _controller.DuplicateAmplitudesCommand(amplitudeNames);
        }
        private void DeleteAmplitudes(string[] amplitudeNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected amplitudes?" + Environment.NewLine
                                                 + amplitudeNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveAmplitudesCommand(amplitudeNames);
            }
        }

        #endregion  ################################################################################################################

        #region Initial condition menu  ############################################################################################
        private void tsmiCreateInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                CreateInitialCondition();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Initial Conditions", _controller.GetAllInitialConditions(), EditInitialCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), DuplicateInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Initial Conditions", _controller.GetAllInitialConditions(), PreviewInitialCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), HideInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), ShowInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteInitialCondition_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Initial Conditions", _controller.GetAllInitialConditions(), DeleteInitialConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateInitialCondition()
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmInitialCondition);
            //
            SinglePointDataEditor.ParentForm = _frmInitialCondition;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmInitialCondition, "Create Initial Condition", null);
        }
        private void EditInitialCondition(string initialConditionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmInitialCondition);
            //
            SinglePointDataEditor.ParentForm = _frmInitialCondition;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmInitialCondition, "Edit Initial Condition", initialConditionName);
        }
        private void DuplicateInitialConditions(string[] initialConditionNames)
        {
            _controller.DuplicateInitialConditionsCommand(initialConditionNames);
        }
        private void PreviewInitialConditions(string[] initialConditionNames)
        {
            foreach (var name in initialConditionNames) PreviewInitialCondition(name);
        }
        private void PreviewInitialCondition(string initialConditionName)
        {
            _controller.PreviewInitialCondition(initialConditionName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideInitialConditions(string[] initialConditionNames)
        {
            _controller.HideInitialConditionsCommand(initialConditionNames);
        }
        private void ShowInitialConditions(string[] initialConditionNames)
        {
            _controller.ShowInitialConditionsCommand(initialConditionNames);
        }
        private void ShowOnlyInitialConditions(string[] initialConditionNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.InitialConditions.Keys);
            allNames.ExceptWith(initialConditionNames);
            _controller.HideInitialConditionsCommand(allNames.ToArray());
            _controller.ShowInitialConditionsCommand(initialConditionNames);
        }
        private void DeleteInitialConditions(string[] initialConditionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected initial conditions?" + Environment.NewLine
                                                 + initialConditionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveInitialConditionsCommand(initialConditionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Step menu  #########################################################################################################
        internal void tsmiCreateStep_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                //
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmStep.SetPreselectListViewItem(selectedIndex);
                //
                ShowForm(_frmStep, "Create Step", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), EditStep);
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
                SelectOneEntity("Steps", _controller.GetAllSteps(), EditStepControls);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Steps", _controller.GetAllSteps(), DuplicateSteps);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteStep_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Steps", _controller.GetAllSteps(), DeleteSteps);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void EditStep(string stepName)
        {
            ShowForm(_frmStep, "Edit Step", stepName);
        }
        private void EditStepControls(string stepName)
        {
            ShowForm(_frmStepControls, "Edit Step Controls", stepName, null);
        }
        private void DuplicateSteps(string[] stepNames)
        {
            _controller.DuplicateStepsCommand(stepNames);
        }
        private void DeleteSteps(string[] stepNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected steps?" + Environment.NewLine
                                                 + stepNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveStepsCommand(stepNames);
            }
        }

        #endregion  ################################################################################################################

        #region History output menu  ###############################################################################################
        private void tsmiCreateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditHistoryOutput(string stepName)
        {
            SelectOneEntityInStep("History outputs", _controller.GetAllHistoryOutputs(stepName), stepName, EditHistoryOutput);
        }
        private void SelectAndDuplicateHistoryOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("History outputs", _controller.GetAllHistoryOutputs(stepName),
                                         stepName, DuplicateHistoryOutputs);
        }
        private void SelectAndPropagateHistoryOutput(string stepName)
        {
            SelectOneEntityInStep("History outputs", _controller.GetAllHistoryOutputs(stepName), stepName, PropagateHistoryOutput);
        }
        private void SelectAndDeleteHistoryOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("History outputs", _controller.GetAllHistoryOutputs(stepName),
                                         stepName, DeleteHistoryOutputs);
        }
        //
        private void CreateHistoryOutput(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmHistoryOutput);
            ShowForm(_frmHistoryOutput, "Create History Output", stepName, null);
        }
        private void EditHistoryOutput(string stepName, string historyOutputName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmHistoryOutput);
            ShowForm(_frmHistoryOutput, "Edit History Output", stepName, historyOutputName);
        }
        private void DuplicateHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            _controller.DuplicateHistoryOutputsCommand(stepName, historyOutputNames);
        }
        private void PropagateHistoryOutput(string stepName, string historyOutputName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).HistoryOutputs.ContainsKey(historyOutputName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing history output " + historyOutputName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateHistoryOutputCommand(stepName, historyOutputName);
        }
        private void DeleteHistoryOutputs(string stepName, string[] historyOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected history outputs from step " + stepName + "?"
                                                 + Environment.NewLine + historyOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveHistoryOutputsCommand(stepName, historyOutputNames);
            }
        }
        #endregion  ################################################################################################################

        #region Field output menu  #################################################################################################
        private void tsmiCreateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditFieldOutput(string stepName)
        {
            SelectOneEntityInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, EditFieldOutput);
        }
        private void SelectAndDuplicateFieldOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("Field outputs", _controller.GetAllFieldOutputs(stepName),
                                         stepName, DuplicateFieldOutputs);
        }
        private void SelectAndPropagateFieldOutput(string stepName)
        {
            SelectOneEntityInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, PropagateFieldOutput);
        }
        private void SelectAndDeleteFieldOutputs(string stepName)
        {
            SelectMultipleEntitiesInStep("Field outputs", _controller.GetAllFieldOutputs(stepName), stepName, DeleteFieldOutputs);
        }
        //
        private void CreateFieldOutput(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            //
            ShowForm(_frmFieldOutput, "Create Field Output", stepName, null);
        }
        private void EditFieldOutput(string stepName, string fieldOutputName)
        {
            ShowForm(_frmFieldOutput, "Edit Field Output", stepName, fieldOutputName);
        }
        private void DuplicateFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            _controller.DuplicateFieldOutputsCommand(stepName, fieldOutputNames);
        }
        private void PropagateFieldOutput(string stepName, string fieldOutputName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).FieldOutputs.ContainsKey(fieldOutputName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing filed output " + fieldOutputName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateFieldOutputCommand(stepName, fieldOutputName);
        }
        private void DeleteFieldOutputs(string stepName, string[] fieldOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected field outputs from step " + stepName + "?"
                                                 + Environment.NewLine + fieldOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveFieldOutputsCommand(stepName, fieldOutputNames);
            }
        }

        #endregion  ################################################################################################################

        #region Boundary condition menu  ###########################################################################################
        internal void tsmiCreateBC_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmBoundaryCondition.SetPreselectListViewItem(selectedIndex);
                //
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPreviewBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void tsmiHideBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideBoundaryConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowBoundaryConditions);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteBC_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteBoundaryCondition);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditBoundaryCondition(string stepName)
        {
            SelectOneEntityInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName), stepName,
                                  EditBoundaryCondition);
        }
        private void SelectAndDuplicateBoundaryCondition(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, DuplicateBoundaryConditions);
        }
        private void SelectAndPropagateBoundaryCondition(string stepName)
        {
            SelectOneEntityInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName), stepName,
                                  PropagateBoundaryCondition);
        }
        private void SelectAndPreviewBoundaryCondition(string stepName)
        {
            SelectOneEntityInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                  stepName, PreviewBoundaryCondition);
        }
        private void SelectAndHideBoundaryConditions(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, HideBoundaryConditions);
        }
        private void SelectAndShowBoundaryConditions(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, ShowBoundaryConditions);
        }
        private void SelectAndDeleteBoundaryCondition(string stepName)
        {
            SelectMultipleEntitiesInStep("Boundary conditions", _controller.GetStepBoundaryConditions(stepName),
                                         stepName, DeleteBoundaryConditions);
        }
        //
        private void CreateBoundaryCondition(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryCondition);
            ShowForm(_frmBoundaryCondition, "Create Boundary Condition", stepName, null);
        }
        private void EditBoundaryCondition(string stepName, string boundaryConditionName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, true, _frmBoundaryCondition);
            ShowForm(_frmBoundaryCondition, "Edit Boundary Condition", stepName, boundaryConditionName);
        }
        private void DuplicateBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.DuplicateBoundaryConditionsCommand(stepName, boundaryConditionNames);
        }
        private void PropagateBoundaryCondition(string stepName, string boundaryConditionName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).BoundaryConditions.ContainsKey(boundaryConditionName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing boundary condition " + boundaryConditionName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateBoundaryConditionCommand(stepName, boundaryConditionName);
        }
        private void PreviewBoundaryCondition(string stepName, string boundaryConditionName)
        {
            _controller.PreviewBoundaryCondition(stepName, boundaryConditionName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.HideBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void ShowBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            _controller.ShowBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void ShowOnlyBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            HashSet<string> allNames =
                new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).BoundaryConditions.Keys);
            allNames.ExceptWith(boundaryConditionNames);
            _controller.HideBoundaryConditionCommand(stepName, allNames.ToArray());
            _controller.ShowBoundaryConditionCommand(stepName, boundaryConditionNames);
        }
        private void DeleteBoundaryConditions(string stepName, string[] boundaryConditionNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected boundary conditions from step " + stepName + "?"
                                                 + Environment.NewLine + boundaryConditionNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveBoundaryConditionsCommand(stepName, boundaryConditionNames);
            }
        }

        #endregion  ################################################################################################################

        #region Load menu  #########################################################################################################
        internal void tsmiCreateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                int selectedIndex = -1;
                if (e is EventArgs<int> ea) selectedIndex = ea.Value;
                _frmLoad.SetPreselectListViewItem(selectedIndex);
                //
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPreviewLoad);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteLoad_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteLoads);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, EditLoad);
        }
        private void SelectAndDuplicateLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, DuplicateLoads);
        }
        private void SelectAndPropagateLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, PropagateLoad);
        }
        private void SelectAndPreviewLoad(string stepName)
        {
            SelectOneEntityInStep("Loads", _controller.GetStepLoads(stepName), stepName, PreviewLoad);
        }
        private void SelectAndHideLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, HideLoads);
        }
        private void SelectAndShowLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, ShowLoads);
        }
        private void SelectAndDeleteLoads(string stepName)
        {
            SelectMultipleEntitiesInStep("Loads", _controller.GetStepLoads(stepName), stepName, DeleteLoads);
        }
        //
        private void CreateLoad(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmLoad);
            //
            SinglePointDataEditor.ParentForm = _frmLoad;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmLoad, "Create Load", stepName, null);
        }
        private void EditLoad(string stepName, string loadName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmLoad);
            //
            SinglePointDataEditor.ParentForm = _frmLoad;
            SinglePointDataEditor.Controller = _controller;
            //
            ShowForm(_frmLoad, "Edit Load", stepName, loadName);
        }
        private void DuplicateLoads(string stepName, string[] loadNames)
        {
            _controller.DuplicateLoadsCommand(stepName, loadNames);
        }
        private void PropagateLoad(string stepName, string loadName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).Loads.ContainsKey(loadName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing load " + loadName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateLoadCommand(stepName, loadName);
        }
        private void PreviewLoad(string stepName, string loadName)
        {
            _controller.PreviewLoad(stepName, loadName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideLoads(string stepName, string[] loadNames)
        {
            _controller.HideLoadsCommand(stepName, loadNames);
        }
        private void ShowLoads(string stepName, string[] loadNames)
        {
            _controller.ShowLoadsCommand(stepName, loadNames);
        }
        private void ShowOnlyLoads(string stepName, string[] loadNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).Loads.Keys);
            allNames.ExceptWith(loadNames);
            _controller.HideLoadsCommand(stepName, allNames.ToArray());
            _controller.ShowLoadsCommand(stepName, loadNames);
        }
        private void DeleteLoads(string stepName, string[] loadNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected loads from step " + stepName + "?"
                                                 + Environment.NewLine + loadNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveLoadsCommand(stepName, loadNames);
            }
        }

        #endregion  ################################################################################################################

        #region Defined field menu #################################################################################################
        private void tsmiCreateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), CreateDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndEditDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDuplicateDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPropagateDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPropagateDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiPreviewDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndPreviewDefinedField);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndHideDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndShowDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteDefinedField_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Steps", _controller.GetAllSteps(), SelectAndDeleteDefinedFields);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void SelectAndEditDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, EditDefinedField);
        }
        private void SelectAndDuplicateDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, DuplicateDefinedFields);
        }
        private void SelectAndPropagateDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, PropagateDefinedField);
        }
        private void SelectAndPreviewDefinedField(string stepName)
        {
            SelectOneEntityInStep("Defined fields", _controller.GetStepDefinedFields(stepName), stepName, PreviewDefinedField);
        }
        private void SelectAndHideDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, HideDefinedFields);
        }
        private void SelectAndShowDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, ShowDefinedFields);
        }
        private void SelectAndDeleteDefinedFields(string stepName)
        {
            SelectMultipleEntitiesInStep("Defined fields", _controller.GetStepDefinedFields(stepName),
                                         stepName, DeleteDefinedFields);
        }
        //
        private void CreateDefinedField(string stepName)
        {
            if (_controller.Model.Mesh == null) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmDefinedField);
            ShowForm(_frmDefinedField, "Create Defined Field", stepName, null);
        }
        private void EditDefinedField(string stepName, string definedFieldName)
        {
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmDefinedField);
            ShowForm(_frmDefinedField, "Edit Defined Field", stepName, definedFieldName);
        }
        private void DuplicateDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.DuplicateDefinedFieldsForStepCommand(stepName, definedFieldNames);
        }
        private void PropagateDefinedField(string stepName, string definedFieldName)
        {
            bool exists = false;
            string[] nextStepNames = _controller.Model.StepCollection.GetNextStepNames(stepName);
            //
            foreach (var nextStepName in nextStepNames)
            {
                if (_controller.Model.StepCollection.GetStep(nextStepName).DefinedFields.ContainsKey(definedFieldName))
                {
                    exists = true;
                    break;
                }
            }
            //
            bool propagate = true;
            if (exists)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to overwrite the existing defined field " + definedFieldName
                                                     + "?") == DialogResult.Cancel) propagate = false;
            }
            if (propagate) _controller.PropagateDefinedFieldCommand(stepName, definedFieldName);
        }
        private void PreviewDefinedField(string stepName, string definedFieldName)
        {
            _controller.PreviewDefinedField(stepName, definedFieldName);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void HideDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.HideDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void ShowDefinedFields(string stepName, string[] definedFieldNames)
        {
            _controller.ShowDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void ShowOnlyDefinedFields(string stepName, string[] definedFieldNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.Model.StepCollection.GetStep(stepName).DefinedFields.Keys);
            allNames.ExceptWith(definedFieldNames);
            _controller.HideDefinedFieldsCommand(stepName, allNames.ToArray());
            _controller.ShowDefinedFieldsCommand(stepName, definedFieldNames);
        }
        private void DeleteDefinedFields(string stepName, string[] definedFieldNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected defined fields from step " + stepName + "?"
                                                 + Environment.NewLine + definedFieldNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveDefinedFieldsForStepCommand(stepName, definedFieldNames);
            }
        }
        
        #endregion  ################################################################################################################

        #region Tools ##############################################################################################################
        private void tsmiSettings_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_frmSettings.Visible)
                {
                    CloseAllForms();
                    SetFormLocation(_frmSettings);
                    _frmSettings.PrepareForm(_controller);
                    _frmSettings.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
           
        }
        private void tsmiEditParameters_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null)
                {
                    using (FrmParametersEditor fpe = new FrmParametersEditor(_controller))
                    {
                        fpe.Icon = Icon;
                        fpe.Owner = this;
                        CloseAllForms();
                        SetFormLocation(fpe);
                        fpe.ShowDialog(this);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportParameters_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PrePoMax parameters|*.pmp";
                    saveFileDialog.FileName = "Parameters";
                    //
                    if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _controller.ExportParametersCommand(saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiImportParameters_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PrePoMax parameters|*.pmp";
                    openFileDialog.FileName = "";
                    //
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        _controller.ImportParametersCommand(openFileDialog.FileName);
                        //
                        //tsmiEditParameters_Click(null, null);
                    }
                }
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
                if (!_frmQuery.Visible)
                {
                    ClearSelection();
                    //
                    CloseAllForms();
                    SetFormLocation(_frmQuery);
                    _frmQuery.PrepareForm(_controller);
                    _frmQuery.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiArrangeAnnotations_Click(object sender, EventArgs e)
        {
            try
            {
                ArrangeVisibleArrowWidgets();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiRemoveAnnotations_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Annotations.GetCurrentAnnotationNames().Length > 0)
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete all current view annotations?") == DialogResult.OK)
                    {
                        _controller.Annotations.RemoveCurrentArrowAnnotations();
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiFind_Click(object sender, EventArgs e)
        {
            try
            {
                PythonPanel pythonPanel = _visibleSidePanel as PythonPanel;
                if (pythonPanel != null && pythonPanel.Focused)
                {
                    pythonPanel.ShowFind();
                    return;
                }

                if (!_frmFind.Visible)
                {
                    ClearSelection();
                    //
                    CloseAllForms();
                    SetFormLocation(_frmFind);
                    _frmFind.PrepareForm(_controller);
                    _frmFind.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Takes the picture without asking anything, using the settings the capture form holds. Until
        // the user changes them there, those are a picture of the model with the background left out,
        // copied to the clipboard
        private void tsmiCaptureViewport_Click(object sender, EventArgs e)
        {
            try
            {
                CaptureViewport(_frmCaptureViewport.GetProperties());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCaptureViewportAs_Click(object sender, EventArgs e)
        {
            try
            {
                ShowForm(_frmCaptureViewport, "Capture Viewport", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiBidirectionalMeshConverter_Click(object sender, EventArgs e)
        {
            try
            {
                string converterPath = Application.StartupPath + Globals.BidirectionalMeshConverter;
                string tempPath = Application.StartupPath + Globals.TempFolder;
                //
                if (!File.Exists(converterPath))
                    throw new FileNotFoundException("The Bidirectional Mesh Converter was not found.", converterPath);
                //
                if (!Directory.Exists(tempPath))
                    throw new DirectoryNotFoundException("The temporary folder '" + tempPath + "' was not found.");
                //
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = converterPath,
                    WorkingDirectory = Path.GetDirectoryName(converterPath),
                    UseShellExecute = true
                };
                // Start the process
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiConvertOdbToFrd_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Abaqus result files|*.odb";
                    openFileDialog.Multiselect = false;
                    //
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        await Task.Run(() => _controller.ConvertOdbToFrd(openFileDialog.FileName));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void tsmiAutomationServer_Click(object sender, EventArgs e)
        {
            ToggleAutomationServer();
        }
        private void tsslAutomation_Click(object sender, EventArgs e)
        {
            ToggleAutomationServer();
        }
        private void ToggleAutomationServer()
        {
            try
            {
                if (_automationServer != null && _automationServer.IsRunning) StopAutomationServer();
                else StartAutomationServer();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void StartAutomationServerIfConfigured()
        {
            if (_controller == null || _controller.BatchRegenerationMode) return;
            // The server drives the session the user is looking at, so an unattended run opens it
            // only when the caller asked for it explicitly
            bool requested = _cmdOptions != null && _cmdOptions.Rpc != null && _cmdOptions.Rpc != "Off";
            if (!requested)
            {
                if (_hideWindow) return;
                if (_cmdOptions != null && _cmdOptions.ScriptFileName != null) return;
                if (!_controller.AutomationSettings.StartWithApplication) return;
            }
            //
            try
            {
                StartAutomationServer();
            }
            catch (Exception ex)
            {
                WriteDataToOutput("The automation server could not be started: " + ex.Message);
            }
        }
        private void StartAutomationServer()
        {
            if (_automationServer == null)
            {
                _automationServer = new Rpc.RpcServer(this, _controller);
                _automationServer.StateChanged += AutomationServer_StateChanged;
            }
            if (_automationServer.IsRunning) return;
            //
            if (_cmdOptions != null && _cmdOptions.Rpc == "ReadOnly") _automationServer.IsReadOnlyRequested = true;
            // A run started for an agent may name its own port. It is given to the server rather than
            // written into the settings: a switch for one run must not change the address the user
            // configured a client with
            if (_cmdOptions != null) _automationServer.RequestedPort = _cmdOptions.RpcPort;
            if (!EnsureAutomationAccessToken()) return;
            _automationServer.Start();
            WriteDataToOutput("The MCP endpoint of the automation server is " + _automationServer.EndpointUrl +
                              ", and it also accepts calls on the pipe " + _automationServer.PipeName + ".");
            UpdateAutomationServerIndicators();
        }
        // A session with no window has nobody to ask, and there is deliberately no command line
        // switch for the token: the user runs PrePoMax once and generates it, and only then can an
        // unattended run serve. So a hidden session is refused with the reason rather than prompted
        private bool EnsureAutomationAccessToken()
        {
            if (_controller == null) return false;
            if (_controller.AutomationSettings.AccessToken.Length > 0) return true;
            //
            if (_hideWindow)
            {
                WriteDataToOutput("The automation server has no access token. Start PrePoMax with its window, " +
                                  "generate one in Tools - Settings - Automation, and run this again.");
                return false;
            }
            string question = "The automation server needs an access token, which an AI assistant sends to prove " +
                              "it is the assistant you set up." + Environment.NewLine + Environment.NewLine +
                              "Generate one now? You will find it, and the address, in " +
                              "Tools - Settings - Automation.";
            if (MessageBoxes.ShowQuestionYesNo("Automation", question) != DialogResult.Yes) return false;
            //
            SetAutomationAccessToken(Mcp.McpAccessToken.Generate());
            return true;
        }
        // Called by the settings dialog, which generates the value. An empty one means it was cleared,
        // and the server cannot run without it, so it is stopped rather than left serving a token that
        // is no longer written down anywhere
        public void SetAutomationAccessToken(string accessToken)
        {
            if (_controller == null) return;
            //
            bool wasRunning = _automationServer != null && _automationServer.IsRunning;
            if (wasRunning) StopAutomationServer();
            _controller.SaveAutomationAccessToken(accessToken);
            //
            if (string.IsNullOrEmpty(accessToken))
            {
                WriteDataToOutput("The access token of the automation server was cleared, so the server cannot " +
                                  "run until a new one is generated.");
                return;
            }
            WriteDataToOutput("The access token of the automation server was set.");
            if (wasRunning) StartAutomationServer();
        }
        // The settings file is part of the redistributable, so a folder that has just been unpacked
        // may hold the token of whoever prepared the release. It is cleared before anything can serve
        // with it; the release is also prepared with it cleared, and this is the net under that
        private void ClearAutomationAccessTokenOfAnUnpackedFolder()
        {
            if (_controller == null || _controller.AutomationSettings.AccessToken.Length == 0) return;
            //
            _controller.SaveAutomationAccessToken("");
            WriteDataToOutput("This PrePoMax folder is being run for the first time, so the access token found in " +
                              "its settings was cleared. Generate one in Tools - Settings - Automation before " +
                              "starting the automation server.");
        }
        private void StopAutomationServer()
        {
            if (_automationServer == null || !_automationServer.IsRunning) return;
            //
            _automationServer.Stop();
            WriteDataToOutput("The automation server was stopped.");
            UpdateAutomationServerIndicators();
        }
        // Only the renderer can read its own buffer, so a picture of the viewport is taken here
        public void SaveAutomationScreenshot(string fileName, bool transparentBackground, int magnification)
        {
            if (_vtk == null) throw new CaeException("The 3D view is not available.");
            InvokeIfRequired(() => _vtk.RenderToPNG(fileName, transparentBackground, magnification));
        }
        // The picture the capture settings describe, taken because the user asked for it - from the
        // toolbar button as well as from the form, so that the two are one setting. The file is named
        // in the output, so that a capture can be found without opening the folder it went into.
        // False means the user did not want the file replaced after all, and nothing was written
        private bool CaptureViewport(CaptureViewportProperties properties)
        {
            if (!properties.SaveToFile && !properties.CopyToClipboard)
                throw new CaeException("Select to save the picture to a file, to copy it to the clipboard or both.");
            //
            string fileName;
            if (properties.SaveToFile)
            {
                fileName = properties.GetFileName();
                // A numbered name is free by the time it is returned, so this only ever asks about a name
                // the user chose to keep as it is
                if (File.Exists(fileName) &&
                    MessageBoxes.ShowWarningQuestionYesNo("The file '" + fileName + "' already exists. Replace it?") !=
                    DialogResult.Yes)
                    return false;
            }
            // Only the renderer writes the picture, and it writes it to a file
            else fileName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
            //
            try
            {
                SaveAutomationScreenshot(fileName, properties.TransparentBackground, properties.Magnification);
                if (properties.SaveToFile) WriteDataToOutput("Viewport captured into: " + fileName);
                if (properties.CopyToClipboard)
                {
                    // A clipboard held open by another application costs only the copy
                    try
                    {
                        CopyPngFileToClipboard(fileName);
                        WriteDataToOutput("Viewport captured to the clipboard.");
                    }
                    catch (ExternalException ex)
                    {
                        WriteDataToOutput("The viewport capture could not be copied to the clipboard: " + ex.Message);
                    }
                }
            }
            finally
            {
                if (!properties.SaveToFile && File.Exists(fileName)) File.Delete(fileName);
            }
            return true;
        }
        // The copy is offered twice. The PNG keeps the transparency for the applications that read it, and
        // the bitmap, which has no alpha the pasting applications honor, is put on white as a page would be
        private static void CopyPngFileToClipboard(string fileName)
        {
            byte[] pngBytes = File.ReadAllBytes(fileName);
            using (MemoryStream imageStream = new MemoryStream(pngBytes))
            using (Image image = Image.FromStream(imageStream))
            using (Bitmap bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(image, 0, 0, image.Width, image.Height);
                }
                //
                DataObject dataObject = new DataObject();
                dataObject.SetData("PNG", false, new MemoryStream(pngBytes));
                dataObject.SetImage(bitmap);
                Clipboard.SetDataObject(dataObject, true, 5, 100);
            }
        }
        // A picture of the model with the background left out, copied to the clipboard. That is what the
        // toolbar button takes without asking, and what the dialog starts from. A picture saved to a file
        // is proposed as a numbered one in the work directory, where the rest of the model's files are,
        // and a work directory that does not exist is left for the user to replace before saving
        private CaptureViewportProperties GetDefaultCaptureViewportProperties()
        {
            string workDirectory = _controller.GetWorkDirectory();
            if (workDirectory == null || !Directory.Exists(workDirectory)) workDirectory = "";
            //
            string openedFileName = _controller.OpenedFileName;
            string name = string.IsNullOrEmpty(openedFileName) ? Globals.ViewportImageBaseName
                                                               : Path.GetFileNameWithoutExtension(openedFileName);
            return new CaptureViewportProperties(workDirectory, name);
        }
        // A session started with -g No has no window anyone is looking at
        public bool IsWindowHidden { get { return _hideWindow; } }
        // Closes the application on behalf of the automation server. The server only offers it for a
        // hidden session, and it goes through the ordinary close, so the model is saved or discarded
        // by the same rules as always
        public void RequestAutomationExit()
        {
            if (!IsHandleCreated || IsDisposed) return;
            BeginInvoke(new MethodInvoker(delegate () { Close(); }));
        }
        // The Python scope an agent uses is the one the console uses, and pmx is put into it when the
        // Python panel is shown. An agent may never have shown it, so the server asks for it here
        public void PrepareAutomationPythonEngine()
        {
            UpdatePythonEngine(true);
        }
        // Everything an agent runs is written where the user can see it. The Python panel is not
        // created for it: opening a side panel the user did not ask for would rearrange the window,
        // so the echo goes to the console when it exists and to the output in any case
        public void EchoAutomationExecution(string label, string source, string output, string error)
        {
            if (_controller == null || !_controller.AutomationSettings.EchoToPythonPanel) return;
            //
            if (_pythonPanel != null)
            {
                try
                {
                    // The console is a control, and this is called from the connection thread
                    InvokeIfRequired(delegate ()
                    {
                        _pythonPanel.WriteExternalExecution(label, source, output, error);
                    });
                }
                catch (Exception exception)
                {
                    // An echo that fails must not fail the call it echoes, but it must not be silent
                    // either: a swallowed cross thread write is how this was missed the first time
                    WriteDataToOutput("The automation echo could not be written: " + exception.Message);
                }
            }
            WriteDataToOutput(label);
        }
        // Answers on the user interface thread, which is the only place the truth is visible: an item
        // dialog of this application is not modal, and a modal one keeps pumping messages, so neither
        // can be recognized from the thread that asks
        public string GetAutomationBusyReason()
        {
            if (_controller == null) return "PrePoMax is still starting.";
            if (!_startupCompleted)
                return "PrePoMax is still starting: the file it was given is not open yet.";
            if (_controller.SavingFile) return "PrePoMax is saving a file.";
            // BatchRegenerationMode is not asked: it says this process was started for unattended work,
            // not that anything is running right now - a running regeneration shows in the state text
            // The state texts already end in an ellipsis, so nothing is added to them
            if (tsslState.Text != Globals.ReadyText) return "PrePoMax is busy: " + tsslState.Text;
            //
            Form openForm = GetOpenAutomationBlockingForm();
            if (openForm != null)
            {
                string name = openForm.Text != null && openForm.Text.Length > 0 ? openForm.Text : openForm.Name;
                return "A dialog is open: " + name + ". The user is editing something.";
            }
            return null;
        }
        private Form GetOpenAutomationBlockingForm()
        {
            if (_allForms != null)
            {
                foreach (Form form in _allForms)
                {
                    if (form.Visible) return form;
                }
            }
            // A message box, a file dialog or anything else that was shown modally
            try
            {
                FormCollection openForms = Application.OpenForms;
                for (int i = 0; i < openForms.Count; i++)
                {
                    Form form = openForms[i];
                    if (form != this && form.Modal && form.Visible) return form;
                }
            }
            catch
            {
                // The collection changes while a form is being opened or closed
            }
            return null;
        }
        private void AutomationServer_StateChanged()
        {
            // A connection is accepted on the listener thread, and the last one is reported while
            // the application is already closing, so the window may be gone by the time it lands
            if (!IsHandleCreated || IsDisposed) return;
            try
            {
                InvokeIfRequired(new Action(UpdateAutomationServerIndicators));
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
        private void UpdateAutomationServerIndicators()
        {
            bool isRunning = _automationServer != null && _automationServer.IsRunning;
            tsmiAutomationServer.Checked = isRunning;
            tsslAutomation.Visible = isRunning;
            if (!isRunning) return;
            //
            int connections = _automationServer.ConnectionCount;
            bool isReadOnly = _automationServer.IsReadOnly;
            string state = connections == 0 ? "waiting" : connections + " connected";
            // The port is in the label because a second instance answers on another one, and the
            // address is what the user has to give the client
            tsslAutomation.Text = "Automation " + _automationServer.EndpointPort + ": " + state +
                                  (isReadOnly ? ", read only" : "");
        }
        //
        private void ShowColorBarSettings()
        {
            if (_controller.AnnotateWithColor == AnnotateWithColorEnum.FaceOrientation ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Parts ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Materials ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.Sections ||
                _controller.AnnotateWithColor == AnnotateWithColorEnum.SectionThicknesses)
            {
                _frmSettings.SetSettingsToShow(Globals.ColorSettingsName);
            }
            else
            {
                _frmSettings.SetSettingsToShow(Globals.PreSettingsName);
            }
            tsmiSettings_Click(null, null);
        }
        private void ShowAnnotationSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.AnnotationSettingsName);
            tsmiSettings_Click(null, null);
        }
        private void ShowLegendSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.LegendSettingsName);
            tsmiSettings_Click(null, null);
        }
        private void ShowResultWidgetContextMenu(vtkResultWidgetContextMenuTypeEnum contextMenuType,
                                                 bool minimum, Point vtkPoint)
        {
            _resultWidgetContextMenuType = contextMenuType;
            _resultWidgetContextMenuMinimum = minimum;
            //
            bool legendValue = contextMenuType == vtkResultWidgetContextMenuTypeEnum.LegendValue;
            bool legend = contextMenuType == vtkResultWidgetContextMenuTypeEnum.Legend;
            bool statusBlock = contextMenuType == vtkResultWidgetContextMenuTypeEnum.StatusBlock;
            // Min/max limit colors: only when the manual limit clips the data (a color band exists)
            vtkMaxColorSpectrum colorSpectrum = _controller.Settings.Legend.ColorSpectrum;
            bool maxColor = legend && _vtk.ScalarBarAddMaxColor;
            bool minColor = legend && _vtk.ScalarBarAddMinColor;
            //
            tsmiEditLegendValue.Visible = legendValue;
            tsmiSetMaxColor.Visible = maxColor;
            tsmiMaxColorVisible.Visible = maxColor;
            tsmiMaxColorVisible.Checked = colorSpectrum.MaxColorVisible;
            tsmiSetMinColor.Visible = minColor;
            tsmiMinColorVisible.Visible = minColor;
            tsmiMinColorVisible.Checked = colorSpectrum.MinColorVisible;
            tsmiResultWidgetSettings.Visible = legend || statusBlock;
            tsmiResetLegend.Visible = legend;
            // Collapse separators so only a single one shows between visible groups
            UpdateSeparatorVisibility(cmsResultWidget.Items);
            //
            int x = Math.Max(0, Math.Min(vtkPoint.X, _vtk.Width - 1));
            int y = Math.Max(0, Math.Min(_vtk.Height - 1 - vtkPoint.Y, _vtk.Height - 1));
            _vtk.SetWidgetHighlightLocked(true);
            cmsResultWidget.Show(_vtk, new Point(x, y));
        }
        // Hides leading, trailing and consecutive separators so only a single separator
        // remains visible between two visible groups of items.
        private void UpdateSeparatorVisibility(ToolStripItemCollection items)
        {
            bool visibleSinceSeparator = false;
            ToolStripSeparator pendingSeparator = null;
            foreach (ToolStripItem item in items)
            {
                if (item is ToolStripSeparator separator)
                {
                    separator.Visible = false;
                    if (visibleSinceSeparator)
                    {
                        pendingSeparator = separator;
                        visibleSinceSeparator = false;
                    }
                }
                else if (item.Available)   // intended visibility (parent not shown yet)
                {
                    if (pendingSeparator != null)
                    {
                        pendingSeparator.Visible = true;
                        pendingSeparator = null;
                    }
                    visibleSinceSeparator = true;
                }
            }
        }
        private void cmsViewportWidget_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            _vtk.SetWidgetHighlightLocked(false);
        }
        // Viewport background
        private void InitializeViewportContextMenu()
        {
            _cmsViewport = new ContextMenuStrip();
            _cmsViewport.Name = "cmsViewport";
            _cmsViewport.Closed += cmsViewportWidget_Closed;
            //
            _cmsViewport.Items.Add(CreateMirroredMenuItem(tsmiQuery));
            _cmsViewport.Items.Add(new ToolStripSeparator());
            // The view items are organized as in the main View menu
            _cmsViewport.Items.Add(CreateMirroredSubMenu(tsmiStandardViews));
            //
            _tsmiViewportUserViews = new ToolStripMenuItem(tsmiUserViews.Text, tsmiUserViews.Image);
            _tsmiViewportUserViews.Tag = tsmiUserViews;   // only to follow the enabled state
            _cmsViewport.Items.Add(_tsmiViewportUserViews);
            //
            _cmsViewport.Items.Add(CreateMirroredMenuItem(tsmiZoomToFit));
            _cmsViewport.Items.Add(CreateMirroredMenuItem(tsmiBoxZoom));
            _cmsViewport.Items.Add(new ToolStripSeparator());
            // Vertical axis
            ToolStripMenuItem verticalAxis = new ToolStripMenuItem("Vertical Axis");
            verticalAxis.DropDownItems.Add(CreateVerticalAxisMenuItem("X", new double[] { 1, 0, 0 }));
            verticalAxis.DropDownItems.Add(CreateVerticalAxisMenuItem("Y", new double[] { 0, 1, 0 }));
            verticalAxis.DropDownItems.Add(CreateVerticalAxisMenuItem("Z", new double[] { 0, 0, 1 }));
            _cmsViewport.Items.Add(verticalAxis);
            _cmsViewport.Items.Add(new ToolStripSeparator());
            // Coordinate system
            _tsmiViewportCoorSys = new ToolStripMenuItem();   // the text follows the visibility
            _tsmiViewportCoorSys.Click += tsmiCoorSysVisibility_Click;
            _cmsViewport.Items.Add(_tsmiViewportCoorSys);
            //
            ToolStripMenuItem coorSysSettings = new ToolStripMenuItem("Settings");
            coorSysSettings.Tag = tsmiSettings;   // only to follow the enabled state
            coorSysSettings.Click += tsmiCoorSysSettings_Click;
            _cmsViewport.Items.Add(coorSysSettings);
        }
        private ToolStripMenuItem CreateMirroredSubMenu(ToolStripMenuItem sourceItem)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(sourceItem.Text, sourceItem.Image);
            item.Tag = sourceItem;   // only to follow the enabled state
            AddMirroredMenuItems(item.DropDownItems, sourceItem.DropDownItems);
            return item;
        }
        private void AddMirroredMenuItems(ToolStripItemCollection items, ToolStripItemCollection sourceItems)
        {
            foreach (ToolStripItem sourceItem in sourceItems)
            {
                if (sourceItem is ToolStripMenuItem sourceMenuItem) items.Add(CreateMirroredMenuItem(sourceMenuItem));
                else if (sourceItem is ToolStripSeparator) items.Add(new ToolStripSeparator());
            }
        }
        private ToolStripMenuItem CreateMirroredMenuItem(ToolStripMenuItem sourceItem)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(sourceItem.Text, sourceItem.Image);
            item.Tag = sourceItem;
            item.Click += mirroredMenuItem_Click;
            return item;
        }
        private ToolStripMenuItem CreateVerticalAxisMenuItem(string text, double[] axis)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.Tag = axis;
            item.Click += verticalAxisMenuItem_Click;
            return item;
        }
        private void UpdateMirroredMenuItemsState(ToolStripItemCollection items)
        {
            foreach (ToolStripItem item in items)
            {
                if (item.Tag is ToolStripMenuItem sourceItem) item.Enabled = IsMenuItemUsable(sourceItem);
                if (item is ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                    UpdateMirroredMenuItemsState(menuItem.DropDownItems);
            }
        }
        private static bool IsMenuItemUsable(ToolStripMenuItem menuItem)
        {
            ToolStripItem item = menuItem;
            while (item != null)
            {
                if (!item.Enabled) return false;
                item = item.OwnerItem;
            }
            return true;
        }
        private void mirroredMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem item && item.Tag is ToolStripMenuItem sourceItem) sourceItem.PerformClick();
        }
        private void verticalAxisMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (sender is ToolStripMenuItem item && item.Tag is double[] axis) _vtk.SetVerticalAxisView(true, axis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void UpdateViewportUserViews()
        {
            _tsmiViewportUserViews.DropDownItems.Clear();
            //
            ToolStripMenuItem saveUserView = new ToolStripMenuItem("Save Current View");
            saveUserView.Click += tsmiSaveCurrentUserView_Click;
            _tsmiViewportUserViews.DropDownItems.Add(saveUserView);
            // The saved views are added to the main View menu as the model changes
            AddMirroredMenuItems(_tsmiViewportUserViews.DropDownItems, tsmiUserViews.DropDownItems);
        }
        private void ShowViewportContextMenu(Point vtkPoint)
        {
            UpdateViewportUserViews();
            _tsmiViewportCoorSys.Text = _controller.Settings.Graphics.CoorSysVisibility
                ? "Hide Coordinate System"
                : "Show Coordinate System";
            UpdateMirroredMenuItemsState(_cmsViewport.Items);
            //
            int x = Math.Max(0, Math.Min(vtkPoint.X, _vtk.Width - 1));
            int y = Math.Max(0, Math.Min(_vtk.Height - 1 - vtkPoint.Y, _vtk.Height - 1));
            _vtk.SetWidgetHighlightLocked(true);
            _cmsViewport.Show(_vtk, new Point(x, y));
        }
        private void tsmiSaveCurrentUserView_Click(object sender, EventArgs e)
        {
            try
            {
                List<UserViewParameters> userViews = _controller.GetUserViewParameters();
                HashSet<string> names = new HashSet<string>();
                foreach (UserViewParameters userView in userViews) names.Add(userView.Name);
                //
                UserViewParameters parameters = new UserViewParameters(names.GetNextNumberedKey("User_view"));
                _controller.GetViewParameters(out double[] position, out double[] focalPoint,
                                              out double[] upVector, out double parallelScale);
                parameters.Position = position;
                parameters.FocalPoint = focalPoint;
                parameters.UpVector = upVector;
                parameters.ParallelScale = parallelScale;
                //
                userViews.Add(parameters);
                _controller.SetUserViewParameters(userViews);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCoorSysVisibility_Click(object sender, EventArgs e)
        {
            try
            {
                SettingsContainer settings = _controller.Settings;
                settings.Graphics.CoorSysVisibility = !settings.Graphics.CoorSysVisibility;
                _controller.Settings = settings;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiCoorSysSettings_Click(object sender, EventArgs e)
        {
            _frmSettings.SetSettingsToShow(Globals.GraphicsSettingsName);
            tsmiSettings_Click(null, null);
        }
        private void tsmiEditLegendValue_Click(object sender, EventArgs e)
        {
            if (_resultWidgetContextMenuType != vtkResultWidgetContextMenuTypeEnum.LegendValue) return;
            //
            if (_vtk.TryGetScalarBarValueEditData(_resultWidgetContextMenuMinimum, out Rectangle rectangle,
                                                  out string valueText, out double minimum, out double maximum))
            {
                StartEditLegendValue(_resultWidgetContextMenuMinimum, rectangle, valueText, minimum, maximum);
            }
        }
        private void tsmiSetMaxColor_Click(object sender, EventArgs e)
        {
            SetLegendColor(false);
        }
        private void tsmiMaxColorVisible_Click(object sender, EventArgs e)
        {
            SettingsContainer settings = _controller.Settings;
            settings.Legend.ColorSpectrum.MaxColorVisible = !settings.Legend.ColorSpectrum.MaxColorVisible;
            _controller.Settings = settings;
        }
        private void tsmiSetMinColor_Click(object sender, EventArgs e)
        {
            SetLegendColor(true);
        }
        private void tsmiMinColorVisible_Click(object sender, EventArgs e)
        {
            SettingsContainer settings = _controller.Settings;
            settings.Legend.ColorSpectrum.MinColorVisible = !settings.Legend.ColorSpectrum.MinColorVisible;
            _controller.Settings = settings;
        }
        private void tsmiResultWidgetSettings_Click(object sender, EventArgs e)
        {
            if (_resultWidgetContextMenuType == vtkResultWidgetContextMenuTypeEnum.StatusBlock)
                ShowStatusBlockSettings();
            else if (_resultWidgetContextMenuType == vtkResultWidgetContextMenuTypeEnum.Legend)
                ShowLegendSettings();
        }
        private void tsmiResetLegend_Click(object sender, EventArgs e)
        {
            if (_resultWidgetContextMenuType != vtkResultWidgetContextMenuTypeEnum.Legend) return;
            //
            SettingsContainer settings = _controller.Settings;
            settings.Legend.Reset();
            _controller.Settings = settings;
        }
        private void SetLegendColor(bool minimumColor)
        {
            if (_frmQuery.Visible) return;
            //
            SettingsContainer settings = _controller.Settings;
            vtkMaxColorSpectrum colorSpectrum = settings.Legend.ColorSpectrum;
            Color color = minimumColor ? colorSpectrum.MinColor : colorSpectrum.MaxColor;
            string title = minimumColor ? "Set Below Minimum Color" : "Set Above Maximum Color";
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm(title, color);
                if (frmGetColor.ShowDialog(this) == DialogResult.OK && frmGetColor.Color != color)
                {
                    if (minimumColor) colorSpectrum.MinColor = frmGetColor.Color;
                    else colorSpectrum.MaxColor = frmGetColor.Color;
                    _controller.Settings = settings;
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ShowStatusBlockSettings()
        {
            _frmSettings.SetSettingsToShow(Globals.StatusBlockSettingsName);
            tsmiSettings_Click(null, null);
        }
        // Viewport text editing
        private void StartEditArrowAnnotation(string name, Rectangle rectangle)
        {
            if (AnnotationContainer.IsAnnotationNameReserved(name)) return;
            if (_frmQuery.Visible) return;
            //
            AnnotationBase annotation = _controller.Annotations.GetCurrentAnnotation(name);
            string text = annotation.GetAnnotationText();
            rectangle.Offset(-4, 4);
            //
            StartViewportEdit(rectangle, text, ViewportEditModeEnum.Annotation, annotation);
        }
        private void StartEditLegendValue(bool editMinimum, Rectangle rectangle, string valueText,
                                          double minimum, double maximum)
        {
            if (_frmQuery.Visible) return;
            //
            ViewportEditModeEnum editMode = editMinimum
                ? ViewportEditModeEnum.LegendMinimum
                : ViewportEditModeEnum.LegendMaximum;
            rectangle.Offset(-1, 0);
            ViewportLegendValueEditData editData =
                new ViewportLegendValueEditData(minimum, maximum, valueText);
            StartViewportEdit(rectangle, valueText, editMode, editData);
        }
        private void StartViewportEdit(Rectangle rectangle, string text, ViewportEditModeEnum editMode, object tag)
        {
            if (viewportTextEditor.Visible && !CommitViewportEdit()) return;
            //
            Point vtkLocation = this.PointToClient(_vtk.PointToScreen(Point.Empty));
            Point location = new Point(vtkLocation.X + rectangle.X,
                                       vtkLocation.Y + (_vtk.Height - rectangle.Y - rectangle.Height));
            Rectangle vtkArea = new Rectangle(vtkLocation, _vtk.Size);
            //
            viewportTextEditor.Location = location;
            viewportTextEditor.Size = rectangle.Size;
            viewportTextEditor.MinSize = rectangle.Size;
            viewportTextEditor.ParentArea = vtkArea;
            viewportTextEditor.ViewportEditMode = editMode;
            viewportTextEditor.Text = text;
            viewportTextEditor.Tag = tag;
            //
            bool legendValue = editMode == ViewportEditModeEnum.LegendMinimum ||
                               editMode == ViewportEditModeEnum.LegendMaximum;
            if (legendValue)
            {
                double highlightCenterY = vtkLocation.Y + _vtk.Height - rectangle.Y - rectangle.Height / 2.0;
                int editorY = (int)Math.Round(highlightCenterY - viewportTextEditor.Height / 2.0,
                                              MidpointRounding.AwayFromZero);
                viewportTextEditor.Location = new Point(location.X, editorY);
            }
            //
            viewportTextEditor.BringToFront();
            viewportTextEditor.Visible = true;
            viewportTextEditor.SetCaretAtEnd();
            //
            _vtk.SelectBy = vtkSelectBy.Widget;
        }
        private void ViewportTextEditor_CommitRequested()
        {
            CommitViewportEdit();
        }
        private void ViewportTextEditor_NextRequested()
        {
            ViewportEditModeEnum editMode = viewportTextEditor.ViewportEditMode;
            if (editMode != ViewportEditModeEnum.LegendMinimum &&
                editMode != ViewportEditModeEnum.LegendMaximum) return;
            //
            bool editMinimum = editMode == ViewportEditModeEnum.LegendMaximum;
            if (CommitViewportEdit() &&
                _vtk.TryGetScalarBarValueEditData(editMinimum, out Rectangle rectangle,
                                                  out string valueText, out double minimum, out double maximum))
            {
                StartEditLegendValue(editMinimum, rectangle, valueText, minimum, maximum);
            }
        }
        private bool CommitViewportEdit()
        {
            if (InvokeRequired)
                return (bool)Invoke(new Func<bool>(CommitViewportEdit));
            //
            if (!viewportTextEditor.Visible) return false;
            //
            ViewportEditModeEnum editMode = viewportTextEditor.ViewportEditMode;
            if (editMode == ViewportEditModeEnum.Annotation)
            {
                AnnotationBase annotation = (AnnotationBase)viewportTextEditor.Tag;
                string nonOverriddenText = annotation.GetNotOverriddenAnnotationText();
                //
                nonOverriddenText = nonOverriddenText.Replace("\r\n", "\n");
                string newText = viewportTextEditor.Text.Replace("\r\n", "\n");
                //
                if (newText.Length > 0 && newText != nonOverriddenText)
                    annotation.OverriddenText = viewportTextEditor.Text;
                else
                    annotation.OverriddenText = null;
                //
                EndViewportEdit();
                _controller.Annotations.DrawAnnotations();  // redraw in both cases
                return true;
            }
            else if (editMode == ViewportEditModeEnum.LegendMinimum ||
                     editMode == ViewportEditModeEnum.LegendMaximum)
            {
                try
                {
                    StringDoubleConverter converter = new StringDoubleConverter();
                    string valueText = viewportTextEditor.Text.Replace(" ", "");
                    double value = Convert.ToDouble(converter.ConvertFrom(null, CultureInfo.CurrentCulture,
                                                                           valueText),
                                                    CultureInfo.CurrentCulture);
                    if (double.IsNaN(value) || double.IsInfinity(value)) throw new FormatException();
                    //
                    ViewportLegendValueEditData editData =
                        (ViewportLegendValueEditData)viewportTextEditor.Tag;
                    string originalText = editData.OriginalText.Replace(" ", "");
                    double displayedValue =
                        Convert.ToDouble(converter.ConvertFrom(null, CultureInfo.CurrentCulture, originalText),
                                         CultureInfo.CurrentCulture);
                    double originalValue = editMode == ViewportEditModeEnum.LegendMinimum
                        ? editData.Minimum
                        : editData.Maximum;
                    //
                    if (value == displayedValue || value == originalValue)
                    {
                        EndViewportEdit();
                        return true;
                    }
                    //
                    double minimum = editMode == ViewportEditModeEnum.LegendMinimum ? value : editData.Minimum;
                    double maximum = editMode == ViewportEditModeEnum.LegendMaximum ? value : editData.Maximum;
                    if (minimum >= maximum) throw new FormatException();
                    //
                    SettingsContainer settings = _controller.Settings;
                    settings.Legend.ColorSpectrum.SetUserMinMax(minimum, maximum);
                    settings.Legend.ColorSpectrum.MinMaxType = vtkColorSpectrumMinMaxType.Manual;
                    //
                    EndViewportEdit();
                    _controller.Settings = settings;
                    return true;
                }
                catch
                {
                    viewportTextEditor.SetError(true);
                    viewportTextEditor.SetCaretAtEnd();
                    return false;
                }
            }
            //
            CancelViewportEdit();
            return false;
        }
        private void EndViewportEdit()
        {
            viewportTextEditor.Visible = false;
            viewportTextEditor.ViewportEditMode = ViewportEditModeEnum.None;
            viewportTextEditor.Tag = null;
            _vtk.SelectBy = vtkSelectBy.Default;
        }
        private void CancelViewportEdit()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(CancelViewportEdit));
                return;
            }
            //
            if (viewportTextEditor != null && viewportTextEditor.Visible) EndViewportEdit();
        }
        public override void MouseInteractionOnForm(Control sender)
        {
            if (viewportTextEditor.Visible && !viewportTextEditor.IsOrContainsControl(sender))
                CommitViewportEdit();
        }
        public void AnnotationPicked(MouseEventArgs e, Keys modifierKeys, string annotationName, Rectangle annotationRectangle)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                StartEditArrowAnnotation(annotationName, annotationRectangle);
            }
            else if (e.Button == MouseButtons.Right)
            {
                tsmiDeleteAnnotation.Tag = new object[] { annotationName, annotationRectangle };
                //
                bool reserved = AnnotationContainer.IsAnnotationNameReserved(annotationName);
                tsmiEditAnnotation.Enabled = !reserved;
                tsmiResetAnnotation.Enabled = !reserved;
                tsmiDeleteAnnotation.Enabled = !reserved;
                _vtk.SetWidgetHighlightLocked(true);
                cmsAnnotation.Show(_vtk, new Point(e.X, _vtk.Height-  e.Y));
            }
        }
        private void tsmiEditAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName && tag[1] is Rectangle annotationRectangle)
                {
                    StartEditArrowAnnotation(annotationName, annotationRectangle);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName)
                {
                    if (AnnotationContainer.IsAnnotationNameReserved(annotationName)) return;
                    //
                    _controller.Annotations.ResetAnnotation(annotationName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiAnnotationSettings_Click(object sender, EventArgs e)
        {
            try
            {
                ShowAnnotationSettings();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAnnotation_Click(object sender, EventArgs e)
        {
            try
            {
                object[] tag = (object[])tsmiDeleteAnnotation.Tag;
                if (tag[0] is string annotationName)
                {
                    if (AnnotationContainer.IsAnnotationNameReserved(annotationName)) return;
                    //
                    //if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected annotation?") == DialogResult.OK)
                    {
                        _controller.Annotations.RemoveCurrentArrowAnnotation(annotationName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        // Settings
        private void UpdateSettings(SettingsContainer settings)
        {
            _controller.Settings = settings;
        }

        // Unit system
        private void tsslUnitSystem_Click(object sender, EventArgs e)
        {
            try
            {
                if (Debugger.IsAttached)
                {
                    if (GetCurrentView() == ViewGeometryModelResults.Geometry ||
                        GetCurrentView() == ViewGeometryModelResults.Model)
                    {
                        IfNeededSelectAndSetNewModelProperties();
                    }
                    else SelectResultsUnitSystem();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void IfNeededSelectAndSetNewModelProperties()
        {
            try
            {
                UnitSystemType unitSystemType = _controller.Model.UnitSystem.UnitSystemType;
                ModelSpaceEnum modelSpace = _controller.Model.Properties.ModelSpace;
                // If needed
                if (modelSpace == ModelSpaceEnum.Undefined || unitSystemType == UnitSystemType.Undefined)
                {
                    // Select and set
                    if (SelectNewModelProperties(false)) SetNewModelProperties();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private bool SelectNewModelProperties(bool cancelPossible)
        {
            DialogResult dialogResult = DialogResult.Cancel;
            //
            InvokeIfRequired(() =>
            {
                try
                {                    
                    // Disable the form during regenerate - check that the state is ready
                    if (tsslState.Text != Globals.RegeneratingText)
                    {
                        CloseAllForms();
                        SetFormLocation(_frmNewModel);
                        //
                        if (_frmNewModel.PrepareForm("", "New Model"))
                        {
                            _frmNewModel.SetCancelPossible(cancelPossible);
                            dialogResult = _frmNewModel.ShowDialog(this);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
            //
            return dialogResult == DialogResult.OK;
        }
        private void SetNewModelProperties()
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    if (_frmNewModel.ModelSpace.IsTwoD())
                    {
                        FeMesh geometry = _controller.Model.Geometry;
                        FeMesh mesh = _controller.Model.Mesh;
                        if ((geometry != null && geometry.Nodes.Count > 0 && !geometry.BoundingBox.Is2D()) ||
                            (mesh != null && mesh.Nodes.Count > 0 && !mesh.BoundingBox.Is2D()))
                            throw new CaeException("Use of the 2D model space is not possible. The geometry or the mesh " +
                                                   "do not contain 2D geometry in x-y plane.");
                    }
                    _controller.SetNewModelPropertiesCommand(_frmNewModel.ModelSpace, _frmNewModel.UnitSystem.UnitSystemType);
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
        }
        public void SelectResultsUnitSystem(string parameters = null)
        {
            InvokeIfRequired(() =>
            {
                try
                {
                    // Disable unit system selection during regenerate - check that the state is ready
                    if (tsslState.Text != Globals.RegeneratingText)
                    {
                        UnitSystemType unitSystemType = _controller.CurrentResult.UnitSystem.UnitSystemType;
                        //
                        if (unitSystemType == UnitSystemType.Undefined)
                        {
                            UnitSystemType unitSystemTypeFromParameters;
                            if (Enum.TryParse(parameters, out unitSystemTypeFromParameters))
                            {
                                _controller.SetResultsUnitSystem(unitSystemTypeFromParameters);
                            }
                            else
                            {
                                CloseAllForms();
                                SetFormLocation(_frmNewModel);
                                //
                                if (_frmNewModel.PrepareForm("", "Results"))
                                {
                                    if (_frmNewModel.ShowDialog(this) == DialogResult.OK)
                                    {
                                        _controller.SetResultsUnitSystem(_frmNewModel.UnitSystem.UnitSystemType);
                                    }
                                    else throw new NotSupportedException();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ExceptionTools.Show(this, ex);
                }
            });
        }
        public void UpdateUnitSystem(UnitSystem unitSystem)
        {
            tsslUnitSystem.Text = "Unit system: " + unitSystem.UnitSystemType.GetDescription();
            //
            SetScaleWidgetUnit(unitSystem);
        }
        #endregion  ################################################################################################################

        #region Analysis menu  #####################################################################################################
        private void tsmiCreateAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model.Mesh == null) return;
                ShowForm(_frmAnalysis, "Create Analysis", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), EditAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Analyses", _controller.GetAllJobs(), DuplicateAnalyses);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiRunAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), RunAnalysis);
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
                SelectOneEntity("Analyses", _controller.GetAllJobs(), CheckModel);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMonitorAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), MonitorAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        internal void tsmiResultsAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), ResultsAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiKillAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Analyses", _controller.GetAllJobs(), KillAnalysis);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteAnalysis_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Analyses", _controller.GetAllJobs(), DeleteAnalyses);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditAnalysis(string jobName)
        {
            ShowForm(_frmAnalysis, "Edit Analysis", jobName);
        }
        private void DuplicateAnalyses(string[] jobNames)
        {
            _controller.DuplicateJobsCommand(jobNames);
        }
        private void RunAnalysis(string jobName)
        {
            RunAnalysis(jobName, false);
        }
        private void CheckModel(string jobName)
        {
            RunAnalysis(jobName, true);
        }
        private void RunAnalysis(string jobName, bool onlyCheckModel)
        {
            // Check validity
            if (CheckValidity())
            {
                string workDirectory = _controller.GetJobWorkDirectory(jobName);
                //
                if (workDirectory == null || !Directory.Exists(workDirectory))
                    throw new Exception("The work directory of the analysis does not exist.");
                //
                AnalysisJob job = _controller.GetJob(jobName);
                if (job.JobStatus != JobStatus.Running)
                {
                    string inputFileName = _controller.GetCalculiXInpFileName(jobName);
                    if (File.Exists(inputFileName))
                    {
                        if (MessageBoxes.ShowWarningQuestionOKCancel("Overwrite existing analysis files?") != DialogResult.OK) return;
                    }
                    //
                    if (_controller.PrepareAndRunJobCommand(jobName, onlyCheckModel)) MonitorAnalysis(jobName);
                }
                else MessageBoxes.ShowError("The analysis is already running or is in queue.");
            }
        }
        private void MonitorAnalysis(string jobName)
        {
            try
            {
                CloseAllForms();
                SetFormLocation(_frmMonitor);
                _frmMonitor.PrepareForm(jobName);
                _frmMonitor.Show(this);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void ResultsAnalysis(string jobName)
        {
            await Task.Run(() => _controller.OpenResultsCommand(jobName));
            //
            if (LastOpenResultsAccepted) RunHistoryPostprocessing();
        }
        public void RunHistoryPostprocessing()
        {
            try
            {
                CloseAllForms();
                Application.DoEvents();
                SetStateWorking(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = false;
                _controller.RunHistoryPostprocessing();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                SetStateReady(Globals.RegeneratingText);
                _modelTree.ScreenUpdating = true;
                RegenerateTree();
                //
                SetMenuAndToolStripVisibility();
                // During regeneration the tree is empty
                if (_controller.CurrentResult != null) SelectFirstComponentOfFirstFieldOutput();
                //
                SetZoomToFit(true);
            }
        }
        public async void OpenAnalysisResults(string jobName, bool asynchronous = true)
        {
            LastOpenResultsAccepted = false;
            //
            AnalysisJob job = _controller.GetJob(jobName);
            //
            bool openResults = true;
            string parameters = Globals.FromMonitorForm;
            //
            if (!_controller.BatchRegenerationMode)
            {
                if (job.JobStatus == JobStatus.Running)
                {
                    string question = $"The analysis {job.Name} is still running. Continue?";
                    openResults = MessageBoxes.ShowWarningQuestionOKCancel(question) == DialogResult.OK;
                    if (openResults) parameters += Globals.NameSeparator + Globals.OpenRunningJobResults;
                }
                else if (!job.IsUpToDate)
                {
                    string question = $"The result file was modified after the analysis {job.Name} completed. Continue?";
                    openResults = MessageBoxes.ShowWarningQuestionOKCancel(question) == DialogResult.OK;
                }
            }
            //
            if (!openResults) return;
            //
            LastOpenResultsAccepted = true;
            //
            string resultsFileName = Path.Combine(job.WorkDirectory, job.ResultsFileName);
            //
            if (asynchronous)
                await OpenAsync(resultsFileName, _controller.Open, false, CloseMonitorWindow, parameters);
            else
                OpenAsync(resultsFileName, _controller.Open, false, CloseMonitorWindow, parameters).Wait();
        }
        private void CloseMonitorWindow()
        {
            // This hides the monitor window
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
                _frmMonitor.DialogResult = DialogResult.OK;
        }
        private void KillAnalysis(string jobName)
        {
            if (_controller.GetJob(jobName).JobStatus == JobStatus.Running)
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to kill selected analysis?") == DialogResult.OK)
                {
                    _controller.KillJob(jobName);
                }
            }
            else
            {
                MessageBoxes.ShowError("The analysis is not running.");
            }
        }
        private void DeleteAnalyses(string[] jobNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected analyses?" + Environment.NewLine
                                                 + jobNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveJobsCommand(jobNames);
            }
        }
        public void UpdateAnalysisProgress()
        {
            InvokeIfRequired(_frmMonitor.UpdateProgress);
        }
        //
        public AnalysisJob GetDefaultJob()
        {
            try
            {
                AnalysisJob analysisJob = null;
                //
                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)delegate () { GetDefaultJob(out analysisJob); });
                }
                else
                {
                    GetDefaultJob(out analysisJob);
                }
                //
                return analysisJob;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                return null;
            }
        }
        private void GetDefaultJob(out AnalysisJob defaultJob)
        {
            _frmAnalysis.PrepareForm(null, null);
            defaultJob = _frmAnalysis.Job;
        }
        public string GetDefaultJobName()
        {
            string name = null;
            //
            if (_controller.OpenedFileName != null)
            {
                name = NamedClass.GetErrorFreeName(Path.GetFileNameWithoutExtension(_controller.OpenedFileName),
                                                   "Analysis", null);
            }
            return name;
        }

        #endregion  ################################################################################################################

        #region Results  ###########################################################################################################
        public void ViewResultHistoryOutputData(HistoryResultData historyData)
        {
            try
            {
                if (!_frmViewResultHistoryOutput.Visible)
                {
                    CloseAllForms();
                    SetFormLocation(_frmViewResultHistoryOutput);
                    //
                    string[] columnNames;
                    object[][] rowBasedData;
                    _controller.GetHistoryOutputData(historyData, out columnNames, out rowBasedData, false);
                    //
                    _frmViewResultHistoryOutput.SetData(columnNames, rowBasedData);
                    _frmViewResultHistoryOutput.Show();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiTransformation_Click(object sender, EventArgs e)
        {
            try
            {
                SinglePointDataEditor.ParentForm = _frmTransformation;
                SinglePointDataEditor.Controller = _controller;
                //
                ShowForm(_frmTransformation, "Create Transformation", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private async void tsmiAppendResults_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Calculix result files|*.frd";
                    openFileDialog.Multiselect = false;
                    //
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        foreach (var fileName in openFileDialog.FileNames)
                        {
                            // Do not use: if (CheckBeforeOpen(fileName))
                            if (File.Exists(fileName)) await OpenAsync(fileName, _controller.AppendResult);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                _controller.ModelChanged = false;   // hide message box
                tsmiNew_Click(null, null);
            }
        }

        #endregion  ################################################################################################################

        #region Result part menu  ##################################################################################################
        private void tsmiEditResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Parts", _controller.GetResultParts(), EditResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiMergeResultPart_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), MergeResultParts, 2);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), HideResultParts);
                Clear3DSelection();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ShowResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ShowOnlyResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetColorForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), SetColorForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResetColorForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), ResetColorForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSetTransparencyForResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), SetTransparencyForResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResultRepresentationOff_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts<ResultPart>(),
                                       ResultRepresentationOffResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiResultRepresentationOn_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts<ResultPart>(),
                                       ResultRepresentationOnResultPart);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultParts_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Parts", _controller.GetResultParts(), DeleteResultParts);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultPart(string partName)
        {
            _frmPartProperties.View = ViewGeometryModelResults.Results;
            ShowForm(_frmPartProperties, "Edit Part", partName);
        }
        private void MergeResultParts(string[] partNames)
        {
            if (_controller.AreResultPartsMergeable(partNames))
            {
                if (MessageBoxes.ShowWarningQuestionOKCancel("OK to merge selected parts?") == DialogResult.OK)
                {
                    _controller.MergeResultPartsCommand(partNames);
                }
            }
            else MessageBoxes.ShowError("Selected parts are of a different type and thus cannot be merged.");
        }
        private void HideResultParts(string[] partNames)
        {
            _controller.HideResultPartsCommand(partNames);
        }
        private void ShowResultParts(string[] partNames)
        {
            _controller.ShowResultPartsCommand(partNames);
        }
        private void ShowOnlyResultParts(string[] partNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.CurrentResult.Mesh.Parts.Keys);
            allNames.ExceptWith(partNames);
            _controller.ShowResultPartsCommand(partNames);
            _controller.HideResultPartsCommand(allNames.ToArray());
        }
        private void SetColorForResultParts(string[] partNames)
        {
            if (_controller.AllResults.CurrentResult.Mesh == null) return;
            //
            using (FrmGetColor frmGetColor = new FrmGetColor())
            {
                Color color = _controller.AllResults.CurrentResult.Mesh.Parts[partNames[0]].GetProperties().Color;
                SetFormLocation(frmGetColor);
                frmGetColor.PrepareForm("Set Part Color: " + partNames.ToShortString(), color);
                if (frmGetColor.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetColorForResultPartsCommand(partNames, frmGetColor.Color);
                }
                SaveFormLocation(frmGetColor);
            }
        }
        private void ResetColorForResultParts(string[] partNames)
        {
            _controller.ResetColorForResultPartsCommand(partNames);
        }
        private void SetTransparencyForResultParts(string[] partNames)
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            //
            using (FrmGetValue frmGetValue = new FrmGetValue(_controller))
            {
                frmGetValue.NumOfDigits = 0;
                frmGetValue.MinValue = 25;
                frmGetValue.MaxValue = 255;
                SetFormLocation(frmGetValue);
                OrderedDictionary<string, double> presetValues =
                    new OrderedDictionary<string, double>("Preset Transparency Values", StringComparer.OrdinalIgnoreCase);
                presetValues.Add("Semi-transparent", 128);
                presetValues.Add("Opaque", 255);
                string desc = "Enter the transparency between 0 and 255.\n" + "(0 - transparent; 255 - opaque)";
                frmGetValue.PrepareForm("Set Transparency: " + partNames.ToShortString(), "Transparency", desc, 128, presetValues);
                if (frmGetValue.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.SetTransparencyForResultPartsCommand(partNames, (byte)frmGetValue.Value);
                }
                SaveFormLocation(frmGetValue);
            }
        }
        private void ResultRepresentationOffResultPart(string[] partNames)
        {
            _controller.SetResultRepresentationForResultPartsCommand(partNames, false);
        }
        private void ResultRepresentationOnResultPart(string[] partNames)
        {
            _controller.SetResultRepresentationForResultPartsCommand(partNames, true);
        }
        private void DeleteResultParts(string[] partNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected parts?" + Environment.NewLine + 
                                                 partNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultPartsCommand(partNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result Reference point  ############################################################################################
        private void tsmiCreateResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.AllResults.CurrentResult.Mesh == null) return;
                //
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
                ShowForm(_frmReferencePoint, "Create Result Reference Point", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Result Reference Points", _controller.GetAllResultReferencePoints(), EditResultReferencePoint);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       DuplicateResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       HideResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       ShowResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       ShowOnlyResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultReferencePoint_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Reference Points", _controller.GetAllResultReferencePoints(),
                                       DeleteResultReferencePoints);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultReferencePoint(string referencePointName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmReferencePoint);
            ShowForm(_frmReferencePoint, "Edit Reference Point", referencePointName);
        }
        private void DuplicateResultReferencePoints(string[] referencePointNames)
        {
            _controller.DuplicateResultReferencePointsCommand(referencePointNames);
        }
        private void HideResultReferencePoints(string[] referencePointNames)
        {
            _controller.HideResultReferencePointsCommand(referencePointNames);
        }
        private void ShowResultReferencePoints(string[] referencePointNames)
        {
            _controller.ShowResultReferencePointsCommand(referencePointNames);
        }
        private void ShowOnlyResultReferencePoints(string[] referencePointNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.GetResultReferencePointNames());
            allNames.ExceptWith(referencePointNames);
            _controller.HideResultReferencePointsCommand(allNames.ToArray());
            _controller.ShowResultReferencePointsCommand(referencePointNames);
        }
        private void DeleteResultReferencePoints(string[] referencePointNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected result reference points?" + Environment.NewLine
                                                 + referencePointNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultReferencePointsCommand(referencePointNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result Coordinate system  ##########################################################################################
        private void tsmiCreateResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
                ShowForm(_frmCoordinateSystem, "Create Result Coordinate System", null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                EditResultCoordinateSystem);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       DuplicateResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiHideResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       HideResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       ShowResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiShowOnlyResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       ShowOnlyResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultCoordinateSystem_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Result Coordinate Systems", _controller.GetAllResultCoordinateSystems(),
                                       DeleteResultCoordinateSystems);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void EditResultCoordinateSystem(string coordinateSystemName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmCoordinateSystem);
            ShowForm(_frmCoordinateSystem, "Edit Coordinate System", coordinateSystemName);
        }
        private void DuplicateResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.DuplicateResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void HideResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.HideResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowResultCoordinateSystems(string[] coordinateSystemNames)
        {
            _controller.ShowResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void ShowOnlyResultCoordinateSystems(string[] coordinateSystemNames)
        {
            HashSet<string> allNames = new HashSet<string>(_controller.AllResults.CurrentResult.Mesh.CoordinateSystems.Keys);
            allNames.ExceptWith(coordinateSystemNames);
            _controller.HideResultCoordinateSystemsCommand(allNames.ToArray());
            _controller.ShowResultCoordinateSystemsCommand(coordinateSystemNames);
        }
        private void DeleteResultCoordinateSystems(string[] coordinateSystemNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected result coordinate systems?" + Environment.NewLine
                                                 + coordinateSystemNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultCoordinateSystemsCommand(coordinateSystemNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result field output  ###############################################################################################
        private void tsmiCreateResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                CreateResultFieldOutput();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("Field Outputs", _controller.GetResultFieldOutputs(), EditResultFieldOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Field Outputs", _controller.GetResultFieldOutputs(), DuplicateResultFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = "FieldOutput.csv";
                saveFileDialog.Filter = "Comma separated values (*.csv)|*.csv";
                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    _controller.ExportResultFieldOutputCommand(saveFileDialog.FileName, _controller.AllResults.GetCurrentResultName(),
                                                               _controller.CurrentFieldData);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultFieldOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("Field Outputs", _controller.GetVisibleResultFieldOutputsAsNamedItems(),
                                       DeleteResultFieldOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateResultFieldOutput()
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            if (_controller.CurrentResult.GetAllFiledNameComponentNames().Count == 0) return;
            //
            ShowForm(_frmResultFieldOutput, "Create Field Output", null);
        }
        private void EditResultFieldOutput(string resultFieldOutputName)
        {
            ShowForm(_frmResultFieldOutput, "Edit Field Output", resultFieldOutputName);
        }
        private void DuplicateResultFieldOutputs(string[] resultFieldOutputNames)
        {
            _controller.DuplicateResultFieldOutputsCommand(resultFieldOutputNames);
        }
        public void DeleteResultFieldOutputs(string[] fieldOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected field outputs?" + Environment.NewLine
                                                 + fieldOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultFieldOutputsCommand(fieldOutputNames);
            }
        }
        public void DeleteResultFieldOutputComponents(string fieldOutputName, string[] componentNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected components from field output " + fieldOutputName + "?"
                                                 + Environment.NewLine + componentNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultFieldOutputComponentsCommand(fieldOutputName, componentNames);
            }
        }

        #endregion  ################################################################################################################

        #region Result history output  #############################################################################################
        private void tsmiCreateResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                CreateResultHistoryOutput();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiEditResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectOneEntity("History Outputs", _controller.GetResultHistoryOutputs(), EditResultHistoryOutput);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDuplicateResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("History Outputs", _controller.GetResultHistoryOutputs(),
                                       DuplicateResultHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiExportResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {

                ExportHistoryResultSetFileProperties properties = new ExportHistoryResultSetFileProperties();
                properties.HistoryOutputNames = _controller.GetHistoryResultSetNames();
                if (properties.HistoryOutputNames == null || properties.HistoryOutputNames.Length == 0)
                    MessageBoxes.ShowWarning("There are no history output items to export!");
                else
                {
                    _frmExportFileWithProperties.ExportFileProperties = properties;
                    ShowForm(_frmExportFileWithProperties, _frmExportFileWithProperties.Text, null);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiDeleteResultHistoryOutput_Click(object sender, EventArgs e)
        {
            try
            {
                SelectMultipleEntities("History Outputs", _controller.GetResultHistoryOutputsAsNamedItems(),
                                       DeleteResultHistoryOutputs);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        private void CreateResultHistoryOutput()
        {
            if (_controller.CurrentResult == null || _controller.CurrentResult.Mesh == null) return;
            if (_controller.CurrentResult.GetAllFiledNameComponentNames().Count == 0) return;
            // Data editor
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmResultHistoryOutput);
            ShowForm(_frmResultHistoryOutput, "Create History Output", null);
        }
        private void EditResultHistoryOutput(string resultHistoryOutputName)
        {
            ItemSetDataEditor.SetForms(_frmSelectItemSet, false, _frmResultHistoryOutput);
            ShowForm(_frmResultHistoryOutput, "Edit History Output", resultHistoryOutputName);
        }
        private void DuplicateResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            _controller.DuplicateResultHistoryOutputsCommand(resultHistoryOutputNames);
        }
        public void DeleteResultHistoryOutputs(string[] resultHistoryOutputNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected history outputs?" + Environment.NewLine
                                                 + resultHistoryOutputNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultHistoryOutputsCommand(resultHistoryOutputNames);
            }
        }
        public void DeleteResultHistoryFields(string historyResultSetName, string[] historyResultFieldNames)
        {
            if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected fields from history output " + historyResultSetName + "?"
                                                 + Environment.NewLine + historyResultFieldNames.ToRows()) == DialogResult.OK)
            {
                _controller.RemoveResultHistoryFieldsCommand(historyResultSetName, historyResultFieldNames);
            }
        }
        public void DeleteResultHistoryComponents(NamedClass[] items)
        {
            Dictionary<string, List<string>> parentItemNames;
            Dictionary<string, Dictionary<string, List<string>>> parentParentItemNames =
                new Dictionary<string, Dictionary<string, List<string>>>();
            //
            foreach (var item in items)
            {
                if (item is HistoryResultData hrd)
                {
                    if (parentParentItemNames.TryGetValue(hrd.SetName, out parentItemNames))
                    {
                        parentItemNames[hrd.FieldName].Add(hrd.ComponentName);
                    }
                    else
                    {
                        parentParentItemNames.Add(hrd.SetName, new Dictionary<string, List<string>>()
                                                  { { hrd.FieldName, new List<string>() { hrd.ComponentName} } });
                    }
                }
            }
            //
            string[] itemNames;
            foreach (var parentParentEntry in parentParentItemNames)
            {
                foreach (var parentEntry in parentParentEntry.Value)
                {
                    itemNames = parentEntry.Value.ToArray();
                    if (MessageBoxes.ShowWarningQuestionOKCancel("OK to delete selected components from history field " +
                                                         parentEntry.Key + "?" + Environment.NewLine +
                                                         itemNames.ToRows()) == DialogResult.OK)
                    {
                        _controller.RemoveResultHistoryFieldsCommand(parentParentEntry.Key,
                                                                     parentEntry.Key,
                                                                     itemNames);
                    }
                }
            }
        }
        #endregion  ################################################################################################################

        #region Help menu  #########################################################################################################
        private void tsmiHomePage_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Globals.HomePage);
        }
        private void tsmiAbout_Click(object sender, EventArgs e)
        {
            using (FrmSplash frmSplash = new FrmSplash())
            {
                frmSplash.ShowHelp = true;
                frmSplash.ShowDialog(this);
            }
        }
        
        #endregion  ################################################################################################################

        #region Selection methods  #################################################################################################
        private void SelectOneEntity(string title, NamedClass[] entities, Action<string> OperateOnEntity)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnEntity(entities[0].Name);
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, false, entities, preSelectedEntityNames, null);
                _frmSelectEntity.OneEntitySelected = OperateOnEntity;
                _frmSelectEntity.Show();
            }
        }
        private void SelectOneEntityInStep(string title, NamedClass[] entities, string stepName,
                                           Action<string, string> OperateOnEntityInStep)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnEntityInStep(stepName, entities[0].Name);
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, false, entities, preSelectedEntityNames, stepName);
                _frmSelectEntity.OneEntitySelectedInStep = OperateOnEntityInStep;
                _frmSelectEntity.Show();
            }
        }
        private void SelectMultipleEntities(string title, NamedClass[] entities, Action<string[]> OperateOnMultipleEntities,
                                            int minNumberOfEntities = 1, int maxNumberOfEntities = int.MaxValue)
        {
            if (entities == null || entities.Length == 0 || entities.Length < minNumberOfEntities) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                if (minNumberOfEntities == 1) OperateOnMultipleEntities(entities.GetNames());
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                SetFormLocation(_frmSelectEntity);
                _frmSelectEntity.PrepareForm(title, true, entities, preSelectedEntityNames, null);
                _frmSelectEntity.MultipleEntitiesSelected = OperateOnMultipleEntities;
                _frmSelectEntity.MinNumberOfEntities = minNumberOfEntities;
                _frmSelectEntity.MaxNumberOfEntities = maxNumberOfEntities;
                _frmSelectEntity.Show();
            }
        }
        private void SelectMultipleEntitiesInStep(string title, NamedClass[] entities, string stepName,
                                                  Action<string, string[]> OperateOnMultipleEntitiesInStep)
        {
            if (entities == null || entities.Length == 0) return;
            // Only one entity exists
            if (entities.Length == 1)
            {
                OperateOnMultipleEntitiesInStep(stepName, entities.GetNames());
            }
            // Multiple entities exists
            else
            {
                string[] preSelectedEntityNames = _modelTree.IntersectSelectionWithList(entities);
                //
                _frmSelectEntity.Location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
                _frmSelectEntity.PrepareForm(title, true, entities, preSelectedEntityNames, stepName);
                _frmSelectEntity.MultipleEntitiesSelectedInStep = OperateOnMultipleEntitiesInStep;
                _frmSelectEntity.Show();
            }
        }

        #endregion  ################################################################################################################

        #region Mouse selection methods  ###########################################################################################
        public void _vtk_SelectPointOrArea(double[] pickedPoint, double[] selectionDirection,
                                           double[][] planeParameters, bool completelyInside,
                                           vtkSelectOperation selectOperation, string[] pickedPartNames)
        {
            //pickedPoint = null;
            //selectionDirection = null;
            //completelyInside = true;
            //selectOperation = vtkSelectOperation.None;
            ////
            //if (false)
            //{
            //    planeParameters.DumpToFile(@"C:\temp\planeParameters");
            //    pickedPartNames.DumpToFile(@"C:\temp\partNames");
            //}
            //else
            //{
            //    planeParameters = CaeGlobals.ExtensionMethods.LoadFromFile<double[][]>(@"C:\temp\planeParameters");
            //    pickedPartNames = CaeGlobals.ExtensionMethods.LoadFromFile<string[]>(@"C:\temp\partNames");
            //}
            //
            PushMenuStates();
            //
            SetStateWorking(Globals.SelectionText);
            //
            _controller.SelectPointOrArea(pickedPoint, selectionDirection,
                                          planeParameters, completelyInside,
                                          selectOperation, pickedPartNames);
            //
            int[] ids = _controller.GetSelectionIds();
            // Must be here since it calls Clear which calls SelectionChanged
            if (_frmSectionView != null && _frmSectionView.Visible) _frmSectionView.PickedIds(ids);
            if (_frmFollowerView != null && _frmFollowerView.Visible) _frmFollowerView.PickedIds(ids);
            if (_frmTranslate != null && _frmTranslate.Visible) _frmTranslate.PickedIds(ids);
            if (_frmRotate != null && _frmRotate.Visible) _frmRotate.PickedIds(ids);
            if (_frmMirror != null && _frmMirror.Visible) _frmMirror.PickedIds(ids);
            if (_frmScale != null && _frmScale.Visible) _frmScale.PickedIds(ids);
            if (_frmQuery != null && _frmQuery.Visible) _frmQuery.PickedIds(ids);
            if (_frmTransformation != null && _frmTransformation.Visible) _frmTransformation.PickedIds(ids);
            //
            SelectionChanged(ids);
            //
            SetStateReady(Globals.SelectionText);
            //
            PopMenuStates();
        }
        public void SelectionChanged(int[] ids = null)
        {
            // A command that runs on a background thread changes the selection as well, and the dialogs told
            // about it are controls of the user interface thread
            if (InvokeRequired)
            {
                InvokeIfRequired(() => SelectionChanged(ids));
                return;
            }
            //
            if (ids == null) ids = _controller.GetSelectionIds();
            //
            if (_frmMeshSetupItem != null && _frmMeshSetupItem.Visible) _frmMeshSetupItem.SelectionChanged(ids);
            if (_frmSplitSolid != null && _frmSplitSolid.Visible) _frmSplitSolid.SelectionChanged(ids);
            if (_frmSplitShell != null && _frmSplitShell.Visible) _frmSplitShell.SelectionChanged(ids);
            if (_frmOffsetFace != null && _frmOffsetFace.Visible) _frmOffsetFace.SelectionChanged(ids);
            if (_frmSelectGeometry != null && _frmSelectGeometry.Visible) _frmSelectGeometry.SelectionChanged(ids);
            //
            if (_frmBoundaryLayer != null && _frmBoundaryLayer.Visible) _frmBoundaryLayer.SelectionChanged(ids);
            if (_frmRemeshingParameters != null && _frmRemeshingParameters.Visible) _frmRemeshingParameters.SelectionChanged(ids);
            if (_frmThickenShellMesh != null && _frmThickenShellMesh.Visible) _frmThickenShellMesh.SelectionChanged(ids);
            if (_frmSplitPartMeshUsingSurface != null && _frmSplitPartMeshUsingSurface.Visible)
                _frmSplitPartMeshUsingSurface.SelectionChanged(ids);
            if (_frmMergeCoincidentNodes != null && _frmMergeCoincidentNodes.Visible)
                _frmMergeCoincidentNodes.SelectionChanged(ids);
            if (_frmNodeSet != null && _frmNodeSet.Visible) _frmNodeSet.SelectionChanged(ids);
            if (_frmElementSet != null && _frmElementSet.Visible) _frmElementSet.SelectionChanged(ids);
            if (_frmSurface != null && _frmSurface.Visible) _frmSurface.SelectionChanged(ids);
            if (_frmReferencePoint != null && _frmReferencePoint.Visible) _frmReferencePoint.SelectionChanged(ids);
            if (_frmCoordinateSystem != null && _frmCoordinateSystem.Visible) _frmCoordinateSystem.SelectionChanged(ids);
            if (_frmSection != null && _frmSection.Visible) _frmSection.SelectionChanged(ids);
            if (_frmConstraint != null && _frmConstraint.Visible) _frmConstraint.SelectionChanged(ids);
            if (_frmContactPair != null && _frmContactPair.Visible) _frmContactPair.SelectionChanged(ids);
            if (_frmInitialCondition != null && _frmInitialCondition.Visible) _frmInitialCondition.SelectionChanged(ids);
            //
            if (_frmHistoryOutput != null && _frmHistoryOutput.Visible) _frmHistoryOutput.SelectionChanged(ids);
            if (_frmBoundaryCondition != null && _frmBoundaryCondition.Visible) _frmBoundaryCondition.SelectionChanged(ids);
            if (_frmLoad != null && _frmLoad.Visible) _frmLoad.SelectionChanged(ids);
            if (_frmDefinedField != null && _frmDefinedField.Visible) _frmDefinedField.SelectionChanged(ids);
            //
            if (_frmResultHistoryOutput != null && _frmResultHistoryOutput.Visible) _frmResultHistoryOutput.SelectionChanged(ids);
        }
        public void SetQuerySelection(bool querySelection)
        {
            InvokeIfRequired(() => _vtk.QuerySelectionInProgress = querySelection);
        }
        public bool GetQuerySelection()
        {
            return InvokeIfRequired(() => _vtk.QuerySelectionInProgress);
        }
        public void SetSelectBy(vtkSelectBy selectBy)
        {
            InvokeIfRequired(() => _vtk.SelectBy = selectBy);
        }
        public void SetSelectItem(vtkSelectItem selectItem)
        {
            InvokeIfRequired(() => _vtk.SelectItem = selectItem);
        }
        public void GetGeometryPickProperties(double[] point, out int elementId, out int[] edgeNodeIds,
                                              out int[] cellFaceNodeIds, string[] selectionPartNames = null)
        {
            elementId = -1;
            edgeNodeIds = null;
            cellFaceNodeIds = null;
            try
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(selectionPartNames);
                _vtk.GetGeometryPickProperties(point, out elementId, out edgeNodeIds, out cellFaceNodeIds);
            }
            catch { }
            finally
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(null);
            }
        }
        public void GetClosestCellsOnParts(double[] point, string[] partNames, out double[] distances,
                                           out double[][] normals)
        {
            _vtk.GetClosestCellsOnActors(point, partNames, out distances, out normals);
        }
        public double GetSelectionPrecision()
        {
            return _vtk.GetSelectionPrecision();
        }
        public void GetPointAndCellIdsInsideFrustum(double[][] planeParameters, string[] selectionPartNames,
                                                    out int[] pointIds, out int[] cellIds)
        {
            cellIds = null;
            pointIds = null;
            try
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(selectionPartNames);
                _vtk.GetPointAndCellIdsInsideFrustum(planeParameters, out pointIds, out cellIds);
            }
            catch { }
            finally
            {
                if (selectionPartNames != null) _vtk.SetSelectableActorsFilter(null);
            }
        }

        #endregion  ################################################################################################################
        //
        #region Child form methods  ################################################################################################
        private void AddFormToAllForms(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Icon = Icon;
            form.Owner = this;
            form.Move += itemForm_Move;
            form.VisibleChanged += itemForm_VisibleChanged;
            _allForms.Add(form);
        }
        private void ShowForm(IFormBase form, string text, string itemToEditName)
        {
            ShowForm(form, text, null, itemToEditName);
        }
        private void ShowForm(IFormBase form, string text, string stepName, string itemToEditName)
        {
            if (!form.Visible)
            {
                CloseAllForms();
                SetFormLocation((Form)form);
                form.Text = text;
                if (itemToEditName != null) form.Text += ": " + itemToEditName;
                if (form.PrepareForm(stepName, itemToEditName))
                {
                    ((Form)form).Show(this);
                }
                else
                {
                    if (stepName != null)
                    {
                        string itemName = text.Replace("Create ", "").Replace("Edit ", "").ToLower();
                        MessageBoxes.ShowWarning("Creating/editing of a " + itemName + " in the step: "
                                                 + stepName + " is not supported.");
                    }
                }
            }
        }
        private void SetFormLocation(Form form)
        {
            Rectangle screenBounds;
            bool intersects = false;
            Rectangle formSize = form.ClientRectangle;
            Point location = new Point(Left + _formLocation.X, Top + _formLocation.Y);
            Rectangle locationRect = new Rectangle(location, new Size(1, 1));
            //
            foreach (var screen in Screen.AllScreens)
            {
                screenBounds = screen.Bounds;
                if (screenBounds.IntersectsWith(locationRect))
                {
                    intersects = true;
                    // Size
                    if (formSize.Width > screenBounds.Width) formSize.Width = screenBounds.Width;
                    if (formSize.Height > screenBounds.Height) formSize.Height = screenBounds.Height;
                    // Location X
                    if (location.X < screenBounds.Left) location.X = screenBounds.Left;
                    else if (location.X + formSize.Width > screenBounds.Left + screenBounds.Width)
                        location.X = screenBounds.Left + screenBounds.Width - formSize.Width;
                    // Location Y
                    if (location.Y < screenBounds.Top) location.Y = screenBounds.Top;
                    else if (location.Y + formSize.Height > screenBounds.Top + screenBounds.Height)
                        location.Y = screenBounds.Top + screenBounds.Height - formSize.Height;
                }
            }
            //
            if (!intersects) location = Location;
            //
            if (form.Location != location) form.Location = location;
        }
        private void SaveFormLocation(Form form)
        {
            _formLocation.X = form.Location.X - Left;
            _formLocation.Y = form.Location.Y - Top;
        }
        public void CloseAllForms()
        {
            InvokeIfRequired(() =>
            {
                CommitViewportEdit();
                //
                if (_allForms != null)
                {
                    foreach (var form in _allForms)
                    {
                        if (form.Visible) form.Hide();
                    }
                }
           });
        }

        #endregion  ################################################################################################################

        // Toolbars                                                                                                                 
        #region File toolbar #######################################################################################################
        private void tsbNew_Click(object sender, EventArgs e)
        {
            tsmiNew_Click(null, null);
        }
        private void tsbOpen_Click(object sender, EventArgs e)
        {
            tsmiOpen_Click(null, null);
        }
        private void tsbImport_Click(object sender, EventArgs e)
        {
            tsmiImportFile_Click(null, null);
        }
        private void tsbSave_Click(object sender, EventArgs e)
        {
            tsmiSave_Click(null, null);
        }
        #endregion  ################################################################################################################

        #region View toolbar  ######################################################################################################
        private void tsbFrontView_Click(object sender, EventArgs e)
        {
            tsmiFrontView_Click(null, null);
        }
        private void tsbBackView_Click(object sender, EventArgs e)
        {
            tsmiBackView_Click(null, null);
        }
        private void tsbTopView_Click(object sender, EventArgs e)
        {
            tsmiTopView_Click(null, null);
        }
        private void tsbBottomView_Click(object sender, EventArgs e)
        {
            tsmiBottomView_Click(null, null);
        }
        private void tsbLeftView_Click(object sender, EventArgs e)
        {
            tsmiLeftView_Click(null, null);
        }
        private void tsbRightView_Click(object sender, EventArgs e)
        {
            tsmiRightView_Click(null, null);
        }
        //
        private void tsbNormalView_Click(object sender, EventArgs e)
        {
            tsmiNormalView_Click(null, null);
        }
        private void tsbVerticalView_Click(object sender, EventArgs e)
        {
            tsmiVerticalView_Click(null, null);
        }
        //
        private void tsbIsometric_Click(object sender, EventArgs e)
        {
            tsmiIsometricView_Click(null, null);
        }
        //
        private void tsbZoomToFit_Click(object sender, EventArgs e)
        {
            tsmiZoomToFit_Click(null, null);
        }
        private void tsbBoxZoom_Click(object sender, EventArgs e)
        {
            tsmiBoxZoom_Click(null, null);
        }
        //
        private void tsbShowWireframeEdges_Click(object sender, EventArgs e)
        {
            tsmiShowWireframeEdges_Click(null, null);
        }
        private void tsbShowElementEdges_Click(object sender, EventArgs e)
        {
            tsmiShowElementEdges_Click(null, null);
        }
        private void tsbShowModelEdges_Click(object sender, EventArgs e)
        {
            tsmiShowModelEdges_Click(null, null);
        }
        private void tsbShowNoEdges_Click(object sender, EventArgs e)
        {
            tsmiShowNoEdges_Click(null, null);
        }
        //
        private void tsbSectionView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TurnSectionViewOnOff();
            else if (e.Button == MouseButtons.Right) tsmiSectionView_Click(null, null);
        }
        private void tsbExplodedView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) TurnExplodedViewOnOff(true);
            else if (e.Button == MouseButtons.Right) tsmiExplodedView_Click(null, null);
        }
        private void tsbFollowerView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) tsmiFollowerView_Click(null, null);
        }
        //
        private void tsbHideAllParts_Click(object sender, EventArgs e)
        {
            tsmiHideAllParts_Click(sender, e);
        }
        private void tsbShowAllParts_Click(object sender, EventArgs e)
        {
            tsmiShowAllParts_Click(sender, e);
        }
        private void tsbInvertVisibleParts_Click(object sender, EventArgs e)
        {
            tsmiInvertVisibleParts_Click(sender, e);
        }
        //
        private void tsbQuery_Click(object sender, EventArgs e)
        {
            tsmiQuery_Click(sender, e);
        }
        private void tsbArrangeAnnotations_Click(object sender, EventArgs e)
        {
            tsmiArrangeAnnotations_Click(sender, e);
        }
        private void tsbRemoveAnnotations_Click(object sender, EventArgs e)
        {
            tsmiRemoveAnnotations_Click(sender, e);
        }
        private void tsbCapture_Click(object sender, EventArgs e)
        {
            tsmiCaptureViewport_Click(sender, e);
        }
        // The left button takes the picture the button decides on; the right one opens the dialog that
        // lets the picture be decided on. A right click raises no Click of its own
        private void tsbCapture_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) tsmiCaptureViewportAs_Click(sender, e);
        }
        //
        #endregion  ################################################################################################################

        #region Color by toolbar  ##################################################################################################
        private void tscbColorBy_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressColorByChanged) return;
            //
            try
            {
                // The combo box already shows the selection
                _suppressColorByChanged = true;
                //
                ToolStripMenuItem tsmi = GetColorByMenuItem(tscbColorBy.SelectedItem.ToString());
                if (tsmi == null)
                {
                    ClearAnnotationStatus();
                    _controller.AnnotateWithColor = AnnotateWithColorEnum.None;
                }
                else if (!tsmi.Checked) _controller.AnnotateWithColor = ChangeAnnotationStatus(tsmi);
                //
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                _suppressColorByChanged = false;
            }
        }
        private ToolStripMenuItem GetColorByMenuItem(string name)
        {
            if (name == tsmiAnnotateFaceOrientations.Text) return tsmiAnnotateFaceOrientations;
            else if (name == tsmiAnnotateMeshTypes.Text) return tsmiAnnotateMeshTypes;
            else if (name == tsmiAnnotateParts.Text) return tsmiAnnotateParts;
            else if (name == tsmiAnnotateMaterials.Text) return tsmiAnnotateMaterials;
            else if (name == tsmiAnnotateSections.Text) return tsmiAnnotateSections;
            else if (name == tsmiAnnotateSectionThicknesses.Text) return tsmiAnnotateSectionThicknesses;
            else return null;
        }
        private void UpdateColorByComboBox()
        {
            if (_suppressColorByChanged) return;
            //
            string name;
            if (tsmiAnnotateFaceOrientations.Checked) name = tsmiAnnotateFaceOrientations.Text;
            else if (tsmiAnnotateMeshTypes.Checked) name = tsmiAnnotateMeshTypes.Text;
            else if (tsmiAnnotateParts.Checked) name = tsmiAnnotateParts.Text;
            else if (tsmiAnnotateMaterials.Checked) name = tsmiAnnotateMaterials.Text;
            else if (tsmiAnnotateSections.Checked) name = tsmiAnnotateSections.Text;
            else if (tsmiAnnotateSectionThicknesses.Checked) name = tsmiAnnotateSectionThicknesses.Text;
            else name = Globals.ColorByNone;
            //
            if (tscbColorBy.SelectedItem != null && tscbColorBy.SelectedItem.ToString() == name) return;
            //
            _suppressColorByChanged = true;
            tscbColorBy.SelectedItem = name;
            _suppressColorByChanged = false;
        }
        #endregion  ################################################################################################################

        #region Symbol toolbar  ####################################################################################################
        private void tscbSymbols_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_suppressSymbolChanged)
            {
                // Only set the name here
                _controller.SetSymbolsName(tscbSymbols.SelectedItem.ToString());
            }
            else
            {
                // If BC or load from one step-1 is selected its selection requires the step-1 to be selected.
                // Changing the step symbols is not possible -> Clear selection
                _controller.ClearAllSelection();
                //
                _controller.DrawSymbols(tscbSymbols.SelectedItem.ToString(), false);
                // Save the selected index
                _selectedSymbolIndex[_controller.CurrentView] = tscbSymbols.SelectedIndex;
                //
                this.ActiveControl = null;
            }
        }
        public void SelectLastSymbolName()
        {
            InvokeIfRequired(() =>
            {
                _suppressSymbolChanged = true;
                tscbSymbols.SelectedIndex = tscbSymbols.Items.Count - 1;
                _suppressSymbolChanged = false;
            });
        }
        public void UpdateSymbolsList()
        {
            InvokeIfRequired(() =>
            {
                _suppressSymbolChanged = true;
                // Clear symbols dropdown
                tscbSymbols.Items.Clear();
                tscbSymbols.Items.Add(Globals.SymbolNone);
                tscbSymbols.SelectedIndex = 0;
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Geometry) { }
                else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    tscbSymbols.Items.Add(Globals.SymbolModel);
                    tscbSymbols.Items.AddRange(_controller.GetStepNames());
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    tscbSymbols.Items.Add(Globals.SymbolResult);
                // Retrieve the selected index
                int index = int.MaxValue;
                if (_selectedSymbolIndex != null && !_selectedSymbolIndex.TryGetValue(_controller.CurrentView, out index))
                {
                    index = tscbSymbols.Items.Count - 1;
                }
                // Check the index
                index = Math.Min(index, tscbSymbols.Items.Count - 1);
                // Set the index
                tscbSymbols.SelectedIndex = index;
                //
                _suppressSymbolChanged = false;
            });
        }
        public void SelectOneNameInSymbolsNames(string name)
        {
            InvokeIfRequired(() =>
            {
                int index = -1;
                for (int i = 0; i < tscbSymbols.Items.Count; i++)
                {
                    if (tscbSymbols.Items[i].ToString() == name)
                    {
                        index = i;
                        break;
                    }
                }
                if (index != -1 && index != tscbSymbols.SelectedIndex)
                {
                    _suppressSymbolChanged = true;
                    tscbSymbols.SelectedIndex = index;
                    _selectedSymbolIndex[_controller.CurrentView] = index;
                    _suppressSymbolChanged = false;
                    //
                    _controller.DrawSymbols(tscbSymbols.SelectedItem.ToString(), false); // do not highlight!
                }
            });
        }

        #endregion  ################################################################################################################

        #region Deformation toolbar  ###############################################################################################
        private void tscbResultNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string currentResultName = _controller.AllResults.GetCurrentResultName();
                string newResultName = tscbResultNames.SelectedItem.ToString();
                if (newResultName != currentResultName)
                {
                    _controller.SetCurrentResultsCommand(newResultName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }      
        private void tscbDeformationVariable_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tscbDeformationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Enable scale factor text box if needed
                UpdateScaleFactorTextBoxState();
                //
                if (ScaleFactorChanged()) _controller.ClearSelectionBuffer();
                //
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbDeformationFactor_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    if (ScaleFactorChanged()) _controller.ClearSelectionBuffer();
                    //
                    _controller.Redraw();
                    this.ActiveControl = null;
                    // No beep
                    e.SuppressKeyPress = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbDeformationFactor_EnabledChanged(object sender, EventArgs e)
        {
            if (tstbDeformationFactor.Enabled) UpdateScaleFactorTextBoxState();
        }
        // Complex
        private void tscbComplex_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Enable angle text box if needed
                UpdateAngleTextBoxState();
                //
                _controller.Redraw();
                this.ActiveControl = null;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tstbAngle_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Enter)
                {
                    tstbAngle.Text = Tools.GetPhase360(tstbAngle.Value).ToString();
                    //
                    _controller.Redraw();
                    this.ActiveControl = null;
                    // No beep
                    e.SuppressKeyPress = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        //
        public void SetResultNames()
        {
            InvokeIfRequired(() =>
            {
                tscbResultNames.Items.Clear();
                string[] allResultNames = _controller.AllResults.GetResultNames();
                if (allResultNames != null && allResultNames.Length > 0)
                {
                    foreach (var name in allResultNames) tscbResultNames.Items.Add(name);
                    // Drop down width
                    int maxWidth = GetMaxStringWidth(allResultNames, tscbResultNames.Font);
                    tscbResultNames.DropDownWidth = maxWidth;
                    //
                    string currentResultName = _controller.AllResults.GetCurrentResultName();
                    if (currentResultName != null) tscbResultNames.SelectedItem = currentResultName;
                    //
                    ResizeResultNamesComboBox();
                }
            });
        }
        public void SetCurrentResults(string resultsName)
        {
            InvokeIfRequired(() =>
            {
                ResizeResultNamesComboBox(); // must be here
                //
                string currentResultName = _controller.AllResults.GetCurrentResultName();
                if (resultsName != currentResultName)
                {
                    // Clear
                    ClearActiveTreeSelection();
                    Clear3D();
                    // Set results
                    _controller.AllResults.SetCurrentResults(resultsName);
                    // Regenerate tree
                    RegenerateTree();
                    // Get first component of the first field for the last increment in the last step
                    if (_controller.ResultsInitialized) _controller.CurrentFieldData =
                            _controller.AllResults.CurrentResult.GetFirstComponentOfTheFirstFieldAtDefaultIncrement();
                    //
                    if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
                    {
                        // Reset the previous step and increment
                        SetAllStepAndIncrementIds();
                        // Set last increment
                        SetDefaultStepAndIncrementIds();
                        // Show the selection in the results tree
                        SelectFirstComponentOfFirstFieldOutput();
                        //
                        _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
                        //
                        SetMenuAndToolStripVisibility();
                        //tsmiZoomToFit_Click(null, null);    // different results have different views
                        SetCurrentEdgesVisibilities(_controller.CurrentEdgesVisibility);
                    }
                    //
                    UpdateComplexControlStates();
                    // Running this by from a command must change the results name
                    if (resultsName != tscbResultNames.SelectedItem.ToString())
                    {
                        tscbResultNames.SelectedIndexChanged -= tscbResultNames_SelectedIndexChanged;
                        tscbResultNames.SelectedItem = resultsName;
                        tscbResultNames.SelectedIndexChanged += tscbResultNames_SelectedIndexChanged;
                    }
                }
                //
                this.ActiveControl = null;
            });
        }
        private void ResizeResultNamesComboBox()
        {
            if (tscbResultNames.SelectedItem == null) return;
            //
            string[] allResultNames = new string[] { tscbResultNames.SelectedItem.ToString() };
            int currentWidth = GetMaxStringWidth(allResultNames, tscbResultNames.Font);
            // Control width
            currentWidth += 20; // to account for the drop down arrow
            if (currentWidth < 125) currentWidth = 125;
            else if (currentWidth > 400) currentWidth = 400;
            tscbResultNames.Size = new Size(currentWidth, tscbResultNames.Height);
            //
            Application.DoEvents();
        }
        private int GetMaxStringWidth(IEnumerable<string> items, Font font)
        {
            int maxWidth = 0;
            using (Graphics graphics = CreateGraphics())
            {
                foreach (string item in items)
                {
                    SizeF area = graphics.MeasureString(item, font);
                    maxWidth = Math.Max((int)area.Width, maxWidth);
                }
            }
            return maxWidth;
        }
        private void InitializeDeformationComboBoxes()
        {
            tscbDeformationVariable.Items.Clear();
            string[] variableNames = FeResults.GetPossibleDeformationFieldOutputNames();
            tscbDeformationVariable.Items.AddRange(variableNames);
            tscbDeformationVariable.SelectedIndex = 0;  // Displacements
            //
            tscbDeformationType.Items.Clear();
            Type type = typeof(DeformationScaleFactorTypeEnum);
            string[] typeNames = Enum.GetNames(type);
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = ((DeformationScaleFactorTypeEnum)Enum.Parse(type, typeNames[i])).GetDisplayedName();
            }
            tscbDeformationType.Items.AddRange(typeNames);
            tscbDeformationType.SelectedIndex = 2;      // Automatic

            //if (controller.Results != null)
            //    vps.PopulateDropDownList(controller.Results.GetExistingDeformationFieldOutputNames());
            //else
            //    vps.PopulateDropDownList(CaeResults.FeResults.GetPossibleDeformationFieldOutputNames());
        }
        private void UpdateScaleFactorTextBoxState()
        {
            tslDeformationFactor.Enabled = GetDeformationType() == DeformationScaleFactorTypeEnum.UserDefined;
            tstbDeformationFactor.Enabled = tslDeformationFactor.Enabled;
        }
        public string GetDeformationVariable()
        {
            if (InvokeRequired) return (string)Invoke(new Func<string>(GetDeformationVariable));
            //
            return tscbDeformationVariable.SelectedItem.ToString();
        }
        public DeformationScaleFactorTypeEnum GetDeformationType()
        {
            // Invoke
            if (InvokeRequired)
                return (DeformationScaleFactorTypeEnum)Invoke(new Func<DeformationScaleFactorTypeEnum>(GetDeformationType));
            //
            if (tscbDeformationType.SelectedItem == null) return DeformationScaleFactorTypeEnum.Automatic;
            //
            string displayName = tscbDeformationType.SelectedItem.ToString();
            DeformationScaleFactorTypeEnum[] scaleFactorTypes =
                (DeformationScaleFactorTypeEnum[])Enum.GetValues(typeof(DeformationScaleFactorTypeEnum));
            //
            for (int i = 0; i < scaleFactorTypes.Length; i++)
            {
                if (displayName == scaleFactorTypes[i].GetDisplayedName()) return scaleFactorTypes[i];
            }
            //
            throw new NotSupportedException();
        }
        public float GetDeformationFactor()
        {
            if (InvokeRequired) return (float)Invoke(new Func<float>(GetDeformationFactor));
            //
            return (float)tstbDeformationFactor.Value;
        }
        // The combo box change redraws through its SelectedIndexChanged event, the same as a click
        public void SetDeformationType(DeformationScaleFactorTypeEnum deformationType)
        {
            InvokeIfRequired(() => tscbDeformationType.SelectedItem = deformationType.GetDisplayedName());
        }
        public void SetDeformationFactor(float deformationFactor)
        {
            InvokeIfRequired(() =>
            {
                tstbDeformationFactor.Text = tstbDeformationFactor.UnitConverter.ConvertToString(deformationFactor);
                if (ScaleFactorChanged()) _controller.ClearSelectionBuffer();
                _controller.Redraw();
            });
        }
        private bool ScaleFactorChanged()
        {
            float scaleFactor = _controller.GetScale();
            if (scaleFactor != _prevScaleFactor)
            {
                _prevScaleFactor = scaleFactor;
                return true;
            }
            return false;
        }
        // Complex
        private void InitializeComplexComboBoxes()
        {
            tscbComplex.Items.Clear();
            //
            Type type = typeof(ComplexResultTypeEnum);
            string[] typeNames = Enum.GetNames(type);
            for (int i = 0; i < typeNames.Length; i++)
            {
                typeNames[i] = ((ComplexResultTypeEnum)Enum.Parse(type, typeNames[i])).GetDisplayedName();
            }
            tscbComplex.Items.AddRange(typeNames);
            tscbComplex.SelectedIndex = 0;  // Real
            //
            UpdateComplexControlStates();
        }
        private void UpdateComplexControlStates()
        {
            bool visible;
            if (_controller.ContainsComplexResults)
            {
                visible = true;
                bool enabled = true;
                //
                tslComplex.Enabled = enabled;
                tscbComplex.Enabled = enabled;
                if (enabled) UpdateAngleTextBoxState();
                else
                {
                    tslAngle.Enabled = false;
                    tstbAngle.Enabled = false;
                }
            }
            else
            {
                visible = false;
            }
            //
            SetComplexControlsVisibility(visible);
        }
        private void SetComplexControlsVisibility(bool visible)
        {
            tslComplex.Visible = visible;
            tscbComplex.Visible = visible;
            tslAngle.Visible = visible;
            tstbAngle.Visible = visible;
        }
        private void UpdateAngleTextBoxState()
        {
            bool enabled = GetComplexResultType() == ComplexResultTypeEnum.RealAtAngle;
            tslAngle.Enabled = enabled;
            tstbAngle.Enabled = enabled;
        }
        public ComplexResultTypeEnum GetComplexResultType()
        {
            // Invoke
            if (InvokeRequired)
                return (ComplexResultTypeEnum)Invoke(new Func<ComplexResultTypeEnum>(GetComplexResultType));
            //
            string displayName = tscbComplex.SelectedItem.ToString();
            ComplexResultTypeEnum[] complexResultTypes = (ComplexResultTypeEnum[])Enum.GetValues(typeof(ComplexResultTypeEnum));
            //
            for (int i = 0; i < complexResultTypes.Length; i++)
            {
                if (displayName == complexResultTypes[i].GetDisplayedName()) return complexResultTypes[i];
            }
            //
            throw new NotSupportedException();
        }
        public double GetComplexAngleDeg()
        {
            return tstbAngle.Value;
        }

        #endregion  ################################################################################################################

        #region Results field toolbar  #############################################################################################

        private void tsbResultsUndeformed_Click(object sender, EventArgs e)
        {
            tsmiResultsUndeformed_Click(null, null);
        }
        private void tsbResultsDeformed_Click(object sender, EventArgs e)
        {
            tsmiResultsDeformed_Click(null, null);
        }
        private void tsbResultsColorContours_Click(object sender, EventArgs e)
        {
            tsmiResultsColorContours_Click(null, null);
        }
        private void tsbResultsVectorPlot_Click(object sender, EventArgs e)
        {
            tsmiResultsVectorPlot_Click(null, null);
        }
        private void tsbResultsUndeformedWireframe_Click(object sender, EventArgs e)
        {
            tsmiResultsUndeformedWireframe_Click(null, null);
        }
        private void tsbResultsUndeformedSolid_Click(object sender, EventArgs e)
        {
            tsmiResultsUndeformedSolid_Click(null, null);
        }
        private void tsbTransformation_Click(object sender, EventArgs e)
        {
            tsmiTransformation_Click(null, null);
        }
        //
        private void tscbStepAndIncrement_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                FieldData current = _controller.CurrentFieldData;
                SetFieldData(current.Name, current.Component, GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            }
            catch
            { }
        }
        private void tsbPreviousStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled && tscbStepAndIncrement.SelectedIndex > 0) 
                    tscbStepAndIncrement.SelectedIndex--;
            }
            catch
            { }
        }
        private void tsbNextStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled && tscbStepAndIncrement.SelectedIndex < tscbStepAndIncrement.Items.Count - 1) 
                    tscbStepAndIncrement.SelectedIndex++;
            }
            catch
            { }
        }
        private void tsbFirstStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled) tscbStepAndIncrement.SelectedIndex = 0;
            }
            catch
            { }
        }
        private void tsbLastStepIncrement_Click(object sender, EventArgs e)
        {
            try
            {
                if (tscbStepAndIncrement.Enabled) tscbStepAndIncrement.SelectedIndex = tscbStepAndIncrement.Items.Count - 1;
            }
            catch
            { }
        }
        private void tsbAnimate_Click(object sender, EventArgs e)
        {
            try
            {
                if (_controller.ViewResultsType != ViewResultsTypeEnum.Undeformed &&
                    _controller.GetResultStepIDs().Length > 0 && !_frmAnimation.Visible)
                {
                    CloseAllForms();
                    SetMenuAndToolStripVisibilityByAnimation(false);
                    SetFormLocation(_frmAnimation);
                    _frmAnimation.PrepareForm(this, _controller);
                    if (_frmAnimation.DialogResult == DialogResult.Abort) SetMenuAndToolStripVisibilityByAnimation(true);
                    else _frmAnimation.Show();
                }
            }
            catch
            { }
        }

        #endregion  ################################################################################################################

        #region Status strip  ######################################################################################################
        public void SetState(string text, bool working)
        {
            InvokeIfRequired(() =>
            {
                tsslState.Text = text;
                // Progress
                if (working) tspbProgress.Style = ProgressBarStyle.Marquee;
                else tspbProgress.Style = ProgressBarStyle.Blocks;
                tspbProgress.Visible = working;
                // Rendering - vtk
                if (text == Globals.ExplodePartsText) _vtk.RenderingOn = true;
                else _vtk.RenderingOn = !working;
                _vtk.Enabled = !working;
                //_vtk.Visible = !working;
                // Hack
                //if (!working) _vtk.Left = 1;
                //else _vtk.Left = 100000;
                //
                bool menusActive = !working;
                SetMenuAndToolStripVisibilityBySetState(menusActive);
            });
        }
        public void SetStateReady(string currentText)
        {
            if (tsslState.Text == currentText) // check that the same command is being canceled
            {
                SetState(Globals.ReadyText, false);
                tsslCancel.Visible = false;
            }
        }
        public bool SetStateWorking(string text, bool showCancelButton = false)
        {
            if (tsslState.Text == Globals.ReadyText) // check that the state is ready
            {
                SetState(text, true);
                tsslCancel.Visible = showCancelButton;
                return true;
            }
            return false;
        }
        private bool IsStateWorking()
        {
            return tsslState.Text != Globals.ReadyText;
        }
        public bool IsStateOpening()
        {
            return tsslState.Text == Globals.OpeningText;
        }
        public bool IsStateRegeneratingOrUndoing()
        {
            return tsslState.Text == Globals.RegeneratingText || tsslState.Text == Globals.UndoingText;
        }
        //
        private void tsslCancel_MouseDown(object sender, MouseEventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Sunken;
        }
        private void tsslCancel_MouseUp(object sender, MouseEventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Raised;
        }
        private void tsslCancel_MouseLeave(object sender, EventArgs e)
        {
            tsslCancel.BorderStyle = Border3DStyle.Raised;
        }
        private void tsslCancel_Click(object sender, EventArgs e)
        {
            _controller.StopExecutableJob();
        }
        #endregion  ################################################################################################################

        // Methods                                                                                                                  
        #region Methods
        public void SetTitle(string title)
        {
            InvokeIfRequired(() => this.Text = title);
        }
        private bool CheckValidity()
        {
            string[] invalidItems = _controller.CheckAndUpdateModelValidity();
            if (invalidItems.Length > 0)
            {
                string text = "The model contains active invalid items:" + Environment.NewLine;
                foreach (var item in invalidItems) text += Environment.NewLine + item;
                text += Environment.NewLine + Environment.NewLine + "Continue?";
                return MessageBoxes.ShowWarningQuestionOKCancel(text) == DialogResult.OK;
            }
            return true;
        }
        public double[] GetBoundingBox()
        {
            // xMin, xMax, yMin, yMax, zMin, zMax
            return _vtk.GetBoundingBox();
        }
        public double[] GetBondingBoxSize()
        {
            return _vtk.GetBoundingBoxSize();
        }
        public string GetFileNameToOpen(string title)
        {
            return GetFileNameToOpen(title, GetFileOpenFilter());
        }
        public string GetFileNameToOpen(string title, string filter)
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    if (title != null) openFileDialog.Title = title;
                    openFileDialog.Filter = filter;
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        fileName = openFileDialog.FileName;
                    }
                }
            });
            return fileName;
        }
        public string GetFileNameToImport(string title, bool onlyMaterials)
        {
            return GetFileNameToImport(title, GetFileImportFilter(onlyMaterials));
        }
        public string GetFileNameToImport(string title, string filter)
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    if (title != null) openFileDialog.Title = title;
                    openFileDialog.Filter = filter;
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        fileName = openFileDialog.FileName;
                    }
                }
            });
            return fileName;
        }
        public string GetFileNameToSaveAs()
        {
            string fileName = null;
            InvokeIfRequired(() =>
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PrePoMax files | *.pmx";
                    //
                    fileName = Path.GetFileName(_controller.OpenedFileName);
                    saveFileDialog.FileName = fileName;
                    //
                    saveFileDialog.OverwritePrompt = true;
                    //
                    if (saveFileDialog.ShowDialog(this) == DialogResult.OK) fileName = saveFileDialog.FileName;
                    else fileName = null;
                }
            });
            return fileName;
        }
        public string[] GetFileNamesToImport(string filter)
        {
            string[] fileNames = null;
            InvokeIfRequired(() =>
            {
                // create new dialog to enable multiFilter
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Multiselect = true;
                    openFileDialog.Filter = filter;
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                    {
                        fileNames = openFileDialog.FileNames;
                    }
                }
            });
            return fileNames;
        }
        private string GetFileOpenFilter()
        {
            string filter;
            // Debugger attached
            if (Debugger.IsAttached)
            {
                filter = "All files|*.pmx;*.pmh;*.frd;*.dat;*.odb;*.foam" +
                         "|PrePoMax files|*.pmx" +
                         "|PrePoMax history|*.pmh" +
                         "|Calculix result files|*.frd" +
                         "|Calculix dat files|*.dat" +       // added .dat file
                         "|Abaqus result files|*.odb" +
                         "|OpenFoam files|*.foam";

            }
            // No debugger
            else
            {
                filter = "All files|*.pmx;*.pmh;*.frd;*.odb;*.foam" +
                         "|PrePoMax files|*.pmx" +
                         "|PrePoMax history|*.pmh" +
                         "|Calculix result files|*.frd" +
                         "|Abaqus result files|*.odb" +
                         "|OpenFoam files|*.foam";
            }
            return filter;
        }
        private string GetFileImportFilter(bool onlyMaterials)
        {
            if (onlyMaterials) return "Abaqus/Calculix inp files|*.inp";
            // Debugger attached
            string filter;
            if (Debugger.IsAttached)
            {
                filter = "All supported files|*.stp;*.step;*.igs;*.iges;*.brep;*.stl;*.unv;*.vol;*.inp;*.mesh;*.obj;*.pmp" +
                         "|Step files|*.stp;*.step" +
                         "|Iges files|*.igs;*.iges" +
                         "|Brep files|*.brep" +
                         "|Stereolithography files|*.stl" +
                         "|Universal files|*.unv" +
                         "|Netgen files|*.vol" +
                         "|Abaqus/Calculix inp files|*.inp" +
                         "|Mmg mesh files|*.mesh" +
                         "|Wavefront obj files|*.obj" +              // obj mesh reader added
                         "|Parameters files|*.pmp";

            }
            // No debugger
            else
            {
                filter = "All supported files|*.stp;*.step;*.igs;*.iges;*.brep;*.stl;*.unv;*.vol;*.inp;*.mesh;*.pmp" +
                         "|Step files|*.stp;*.step" +
                         "|Iges files|*.igs;*.iges" +
                         "|Brep files|*.brep" +
                         "|Stereolithography files|*.stl" +
                         "|Universal files|*.unv" +
                         "|Netgen files|*.vol" +
                         "|Abaqus/Calculix inp files|*.inp" +
                         "|Mmg mesh files|*.mesh" +
                         "|Parameters files|*.pmp";
            }
            return filter;
        }
        private HashSet<string> GetFileImportExtensions()
        {
            string[] tmp = GetFileImportFilter(false).Split(new string[] { "*", "\"", ";", "|" },
                                                            StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> extensions = new HashSet<string>();
            foreach (var entry in tmp)
            {
                if (entry.StartsWith(".")) extensions.Add(entry);
            }
            return extensions;
        }

        #region Clear  #############################################################################################################
        public void ClearControls()
        {
            InvokeIfRequired(() =>
            {
                _vtk.Clear();
                _vtk.RemoveAllArrowWidgets();
                _modelTree.Clear();
                _outputLines.Clear();
                _outputLineIsMessage.Clear();
                tbOutput.Text = "";
                ClearResults();
                ClearAnnotationStatus();
                ClearItemIdsStatus();
                _selectedSymbolIndex = new Dictionary<ViewGeometryModelResults, int>();
                InvokeIfRequired(ResetSidePanelsState);
                InvokeIfRequired(ResetPythonEngineContext);
            });
        }
        public void ClearResults()
        {
            InvokeIfRequired(() =>
            {
                tscbResultNames.Items.Clear();
                tscbResultNames.Size = new Size(125, tscbResultNames.Height);
                tscbStepAndIncrement.Items.Clear();
                _modelTree.ClearResults();
                //
                SetMenuAndToolStripVisibility();
            });
        }
        public void Clear3D()
        {
            CommitViewportEdit();
            InvokeIfRequired(_vtk.Clear);
        }
        public void ClearButKeepParts()
        {
            InvokeIfRequired(_vtk.ClearButKeepParts);
        }
        public void ClearSelection()
        {
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
        }
        public void Clear3DSelection()
        {
            InvokeIfRequired(() => _vtk.ClearSelection());
        }
        public void ClearActiveTreeSelection()
        {
            InvokeIfRequired(_modelTree.ClearActiveTreeSelection);
        }
        private void ClearAnnotationStatus()
        {
            tsmiAnnotateFaceOrientations.Checked = false;
            tsmiAnnotateMeshTypes.Checked = false;
            tsmiAnnotateParts.Checked = false;
            tsmiAnnotateMaterials.Checked = false;
            tsmiAnnotateSections.Checked = false;
            tsmiAnnotateSectionThicknesses.Checked = false;
            tsmiAnnotateReferencePoints.Checked = false;
            tsmiAnnotateConstraints.Checked = false;
            tsmiAnnotateContactPairs.Checked = false;
            tsmiAnnotateInitialConditions.Checked = false;
            tsmiAnnotateBCs.Checked = false;
            tsmiAnnotateLoads.Checked = false;
            tsmiAnnotateDefinedFields.Checked = false;
            tsmiAnnotateAllSymbols.Checked = false;
            //
            UpdateColorByComboBox();
            //
            HideColorBar();
        }
        private void ClearItemIdsStatus()
        {
            ClearGeometryItemIdsStatus();
            ClearMeshItemIdsStatus();
        }
        private void ClearGeometryItemIdsStatus()
        {
            tsmiViewVertexIds.Checked = false;
            tsmiViewEdgeIds.Checked = false;
            tsmiViewSurfaceIds.Checked = false;
            tsmiViewPartIds.Checked = false;
            tsmiViewPartNames.Checked = false;
        }
        private void ClearMeshItemIdsStatus()
        {
            tsmiViewNodeIds.Checked = false;
            tsmiViewElementIds.Checked = false;
        }

        #endregion  ################################################################################################################

        #region vtkControl  ########################################################################################################
        // vtkControl
        public void GetViewParameters(out double[] position, out double[] focalPoint, out double[] viewUp, out double parallelScale)
        {
            double[] pos = null;
            double[] foc = null;
            double[] up = null;
            double ps = 1;
            //
            if (this.InvokeRequired) this.Invoke((MethodInvoker)delegate
            {
                _vtk.GetViewParameters(out pos, out foc, out up, out ps);
            });
            else _vtk.GetViewParameters(out pos, out foc, out up, out ps);
            // Assign to out parameters after Invoke completes
            position = pos;
            focalPoint = foc;
            viewUp = up;
            parallelScale = ps;
        }
        public void SetViewParameters(bool animate, double[] position, double[] focalPoint, double[] viewUp, double parallelScale)
        {
            InvokeIfRequired(_vtk.SetViewParameters, animate, position, focalPoint, viewUp, parallelScale);
        }
        public void SetFrontBackView(bool animate, bool front)
        {
            InvokeIfRequired(_vtk.SetFrontBackView, animate, front);
        }
        // The standard views of the View menu, by name, for a script or the automation server
        public void SetStandardView(string orientation, bool animate)
        {
            InvokeIfRequired(() =>
            {
                if (orientation == "Front") _vtk.SetFrontBackView(animate, true);
                else if (orientation == "Back") _vtk.SetFrontBackView(animate, false);
                else if (orientation == "Top") _vtk.SetTopBottomView(animate, true);
                else if (orientation == "Bottom") _vtk.SetTopBottomView(animate, false);
                else if (orientation == "Left") _vtk.SetLeftRightView(animate, true);
                else if (orientation == "Right") _vtk.SetLeftRightView(animate, false);
                else if (orientation == "Isometric") _vtk.SetIsometricView(animate, true);
                else if (orientation == "IsometricNegative") _vtk.SetIsometricView(animate, false);
                else if (orientation == "Normal") _vtk.SetNormalView(animate);
                else if (orientation == "Vertical") _vtk.SetVerticalView(animate, true);
                else throw new CaeException("The view orientation " + orientation + " does not exist.");
            });
        }
        public void SetZoomToFit(bool animate)
        {
            InvokeIfRequired(_vtk.SetZoomToFit, animate);
        }
        public void Rotate(double azimuthAngle, double elevationAngle, double rollAngle)
        {
            InvokeIfRequired(_vtk.Rotate, azimuthAngle, elevationAngle, rollAngle);
        }
        public void StartBoxZoom()
        {
            tsbBoxZoom.Checked = true;
            InvokeIfRequired(_vtk.StartBoxZoom);
        }
        public void BoxZoomEnded()
        {
            tsbBoxZoom.Checked = false;
        }
        public double[] GetViewPlaneNormal()
        {
            return InvokeIfRequired(() => _vtk.GetViewPlaneNormal());
        }
        public void AdjustCameraDistanceAndClipping()
        {
            InvokeIfRequired(_vtk.AdjustCameraDistanceAndClippingRedraw);
        }
        public void UpdateScalarsAndRedraw()
        {
            InvokeIfRequired(_vtk.UpdateScalarsAndRedraw);
        }
        public void UpdateScalarsAndCameraAndRedraw()
        {
            InvokeIfRequired(_vtk.UpdateScalarsAndCameraAndRedraw);
        }
        //
        public vtkMaxActorData[] GetVtkMaxActorData()
        {
            return InvokeIfRequired(() => _vtk.GetVtkMaxActorData());
        }
        public vtkMaxLookupTable GetLookupTable()
        {
            return InvokeIfRequired(() => _vtk.GetLookupTable());
        }
        public double[] GetClosestUpView()
        {
            return InvokeIfRequired(() => _vtk.GetClosestUpView());
        }

        // Section view
        public void CreateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor,
                                      bool elementWise, bool hatch)
        {
            InvokeIfRequired(() => _vtk.CreateSectionView(point, normal, lightenColors, sectionColor, elementWise, hatch));
            InvokeIfRequired(() => { tsbSectionView.Checked = true; });
        }
        public void UpdateSectionView(double[] point, double[] normal, bool lightenColors, Color sectionColor,
                                      bool elementWise, bool hatch)
        {
            InvokeIfRequired(() => _vtk.UpdateSectionView(point, normal, lightenColors, sectionColor, elementWise, hatch));
            InvokeIfRequired(() => { tsbSectionView.Checked = true; });
        }
        public void RemoveSectionView(bool releaseVolumeGrids = false)
        {
            InvokeIfRequired(() => _vtk.RemoveSectionView(releaseVolumeGrids));
            InvokeIfRequired(() => { tsbSectionView.Checked = false; });
        }
        // Exploded view
        public void PreviewExplodedView(Dictionary<string, double[]> partOffsets, bool animate, int timeMs)
        {
            InvokeIfRequired(_vtk.PreviewExplodedView, partOffsets, animate, timeMs);
        }
        public void RemovePreviewedExplodedView(string[] partNames)
        {
            InvokeIfRequired(_vtk.RemovePreviewedExplodedView, partNames);
        }
        public void SetExplodedViewStatus(bool status)
        {
            InvokeIfRequired(() => { tsbExplodedView.Checked = status; });
        }
        // Follower view
        public void ApplyFollowerView(string[] partName, int[] nodeIds)
        {
            InvokeIfRequired(_vtk.ApplyFollowerView, partName, nodeIds);
            InvokeIfRequired(() => { tsbFollowerView.Checked = true; });
        }
        public void UpdateFollowerView()
        {
            InvokeIfRequired(_vtk.UpdateFollowerView);
        }
        public void RemoveFollowerView()
        {
            InvokeIfRequired(_vtk.RemoveFollowerView);
            InvokeIfRequired(() => { tsbFollowerView.Checked = false; });
        }
        // Transformations
        public void AddSymmetry(int symmetryPlane, double[] symmetryPoint)
        {
            InvokeIfRequired(_vtk.AddSymmetry, symmetryPlane, symmetryPoint);
        }
        public void AddLinearPattern(double[] displacement, int numOfItems)
        {
            InvokeIfRequired(_vtk.AddLinearPattern, displacement, numOfItems);
        }
        public void AddCircularPattern(double[] axisPoint, double[] axisNormal, double angle, int numOfItems)
        {
            InvokeIfRequired(_vtk.AddCircularPattern, axisPoint, axisNormal, angle, numOfItems);
        }
        public void ApplyTransformations()
        {
            InvokeIfRequired(_vtk.ApplyTransforms);
        }
        public void SetTransformationsStatus(bool status)
        {
            InvokeIfRequired(() => { tsbTransformation.Checked = status; });
        }
        public void HideTransformedActors()
        {
            InvokeIfRequired(_vtk.HideTransformedActors);
        }
        public void ShowTransformedActors()
        {
            InvokeIfRequired(_vtk.ShowTransformedActors);
        }
        //
        public vtkMaxActor Add3DNodes(vtkMaxActorData nodeData)
        {
            return InvokeIfRequired(() => _vtk.AddPoints(nodeData));
        }
        public vtkMaxActor Add3DCells(vtkMaxActorData cellData)
        {
            return InvokeIfRequired(() => _vtk.AddCells(cellData));
        }
        public void Add3DCells(vtkMaxActorData[] cellData)
        {
            InvokeIfRequired(_vtk.AddCells, cellData);
        }
        public void AddActor(vtkMaxActor actor)
        {
            InvokeIfRequired(_vtk.AddActor, actor);
        }
        public void AddScalarFieldOn3DCells(vtkMaxActorData actorData, bool update)
        {
            InvokeIfRequired(_vtk.AddScalarFieldOnCells, actorData, update);
        }
        public void AddVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            InvokeIfRequired(_vtk.AddVectorFieldActor, actorData, maxMagnitude, maxArrowLength, arrowSize);
        }
        public void AddTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            InvokeIfRequired(_vtk.AddTensorFieldActor, actorData, maxAbsValue, maxGlyphLength, arrowSize);
        }
        public bool AddAnimatedVectorFieldActor(vtkMaxActorData actorData, double maxMagnitude, double maxArrowLength, double arrowSize)
        {
            return InvokeIfRequired(() => _vtk.AddAnimatedVectorFieldActor(actorData, maxMagnitude, maxArrowLength, arrowSize));
        }
        public bool AddAnimatedTensorFieldActor(vtkMaxActorData actorData, double maxAbsValue, double maxGlyphLength, double arrowSize)
        {
            return InvokeIfRequired(() => _vtk.AddAnimatedTensorFieldActor(actorData, maxAbsValue, maxGlyphLength, arrowSize));
        }
        public bool AddAnimatedScalarFieldOn3DCells(vtkMaxActorData actorData)
        {
            return _vtk.AddAnimatedScalarFieldOnCells(actorData);
        }
        public void UpdateActorSurfaceScalarField(string actorName, float[] values, NodesExchangeData extremeNodes,
                                                  ResultFieldExchangeData resultField, bool update)
        {
            InvokeIfRequired(() => _vtk.UpdateActorScalarField(actorName, values, extremeNodes, resultField, update));
        }
        public void UpdateActorColorContoursVisibility(string[] actorNames, bool colorContour)
        {
            InvokeIfRequired(_vtk.UpdateActorsColorContoursVisibility, actorNames, colorContour);
        }
        public void UpdateResultRepresentationVisibility(string[] partNames, bool visible, bool update)
        {
            InvokeIfRequired(() => _vtk.UpdateResultRepresentationVisibility(partNames, visible, update));
        }
        public void AddSphereActor(vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddSphereActor, actorData, symbolSize);
        }
        public void AddCoordinateSystemSymbol(double[] origin, double[][] axisDirections, string[] labels,
                                              double symbolSize, vtkRendererLayer layer)
        {
            InvokeIfRequired(_vtk.AddCoordinateSystemSymbol, origin, axisDirections, labels, symbolSize, layer);
        }
        public void AddCaptionActor(string name, string caption, Color color, double[] position,
                                    double[] offsetVector, double fontScaleFactor, vtkRendererLayer layer)
        {
            InvokeIfRequired(_vtk.AddCaptionActor, name, caption, color, position, offsetVector, fontScaleFactor, layer);
        }
        public void AddLabelActor(string name, double[][] positions, int[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            InvokeIfRequired(_vtk.AddLabelActor, name, positions, labels, color, fontScaleFactor, layer);
        }
        public void AddLabelActor(string name, double[][] positions, string[] labels, Color color,
                                  double fontScaleFactor, vtkRendererLayer layer)
        {
            InvokeIfRequired(_vtk.AddLabelActor, name, positions, labels, color, fontScaleFactor, layer);
        }
        public void RemoveOverlayActors(string[] actorNames)
        {
            InvokeIfRequired(_vtk.RemoveOverlayActors, actorNames);
        }
        public void AddMeasurementAnnotation(string name, vtkMaxActorData nodeData, vtkMaxActorData lineData,
                                             double[][] arrowCoordinates, double[][] arrowNormals,
                                             double symbolSize, bool visible)
        {
            InvokeIfRequired(() => _vtk.AddMeasurementAnnotation(name, nodeData, lineData, arrowCoordinates,
                                                                  arrowNormals, symbolSize, visible));
        }
        public void UpdateMeasurementAnnotation(string name, double[][] nodeCoordinates, double[][] lineCoordinates,
                                                double[][] arrowCoordinates, double[][] arrowNormals, bool visible)
        {
            InvokeIfRequired(() => _vtk.UpdateMeasurementAnnotation(name, nodeCoordinates, lineCoordinates,
                                                                     arrowCoordinates, arrowNormals, visible));
        }
        public void RemoveMeasurementAnnotations(string[] names)
        {
            InvokeIfRequired(_vtk.RemoveMeasurementAnnotations, names);
        }
        public void AddOrientedDisplacementConstraintActor(vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedDisplacementConstraintActor, actorData, symbolSize);
        }
        public void AddOrientedConeActors(vtkMaxActorData data, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedConeActors, data, symbolSize);
        }
        public void AddOrientedRotationalConstraintActor(vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedRotationalConstraintActor, actorData, symbolSize);
        }
        public void AddOrientedJointActor(vtkMaxActorData actorData, double symbolSize, vtkJointSymbolEnum symbol)
        {
            InvokeIfRequired(_vtk.AddOrientedJointActor, actorData, symbolSize, symbol);
        }
        public void AddOrientedArrowsActor(vtkMaxActorData actorData, double symbolSize, bool invert = false, 
                                           double relativeSize = 1)
        {
            InvokeIfRequired(_vtk.AddOrientedArrowsActor, actorData, symbolSize, invert, relativeSize);
        }
        public void AddOrientedDoubleArrowsActor(vtkMaxActorData actorData, double symbolSize)
        {
            InvokeIfRequired(_vtk.AddOrientedDoubleArrowsActor, actorData, symbolSize);
        }
        public void AddOrientedSpringActor(vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
            InvokeIfRequired(_vtk.AddOrientedSpringActor, actorData, symbolSize, invert);
        }
        public void AddOrientedThermosActor(vtkMaxActorData actorData, double symbolSize, bool invert = false)
        {
            InvokeIfRequired(_vtk.AddOrientedThermoActor, actorData, symbolSize, invert);
        }
        public void AddOrientedFluxActor(vtkMaxActorData actorData, double symbolSize, bool center, bool invert)
        {
            InvokeIfRequired(_vtk.AddOrientedFluxActor, actorData, symbolSize, center, invert);
        }
        //
       
        //
        public bool ContainsActor(string actorName)
        {
            return _vtk.ContainsActor(actorName);
        }
        public void HighlightActor(string actorName)
        {
            InvokeIfRequired(_vtk.HighlightActor, actorName);
        }
        public void UpdateActor(string oldName, string newName, Color newColor)
        {
            InvokeIfRequired(_vtk.UpdateActor, oldName, newName, newColor);
        }
        public void UpdateActorModelEdgesToDraw(string actorName, PartExchangeData modelEdgesToDraw)
        {
            InvokeIfRequired(_vtk.UpdateActorModelEdgesToDraw, actorName, modelEdgesToDraw);
        }
        public void HideActors(string[] actorNames, bool updateColorContours)
        {
            InvokeIfRequired(_vtk.HideActors, actorNames, updateColorContours);
        }
        public void ShowActors(string[] actorNames, bool updateColorContours)
        {
            InvokeIfRequired(_vtk.ShowActors, actorNames, updateColorContours);
        }
        // Settings                                             
        public void SetCoorSysVisibility(bool visibility)
        {
            InvokeIfRequired(_vtk.SetCoorSysVisibility, visibility);
        }
        // Scale bar
        public void SetScaleWidgetVisibility(bool visibility)
        {
            InvokeIfRequired(_vtk.SetScaleWidgetVisibility, visibility);
        }
        public void SetWidgetHighlightEnabled(bool enabled)
        {
            InvokeIfRequired(_vtk.SetWidgetHighlightEnabled, enabled);
        }
        public void SetSelectionDistance(int distanceInPixels)
        {
            InvokeIfRequired(_vtk.SetSelectionDistance, distanceInPixels);
        }
        private void SetScaleWidgetUnit(UnitSystem unitSystem)
        {
            string unit = "";
            if (unitSystem.UnitSystemType != UnitSystemType.Undefined) unit = unitSystem.LengthUnitAbbreviation;
            //
            InvokeIfRequired(_vtk.SetScaleWidgetUnit, unit);
        }
        // Scalar bar
        public void InitializeResultWidgetPositions()
        {
            InvokeIfRequired(_vtk.InitializeResultWidgetPositions);
        }
        public void SetScalarBarColorSpectrum(vtkControl.vtkMaxColorSpectrum colorSpectrum)
        {
            InvokeIfRequired(_vtk.SetScalarBarColorSpectrum, colorSpectrum);
        }
        public void SetScalarBarVisibility(bool visibility)
        {
            InvokeIfRequired(_vtk.SetScalarBarVisibility, visibility);
        }
        public void SetScalarBarNumberFormat(string numberFormat)
        {
            InvokeIfRequired(_vtk.SetScalarBarNumberFormat, numberFormat);
        }
        public void SetScalarBarText(string fieldName, string componentName, string unitAbbreviation, string complexComponent,
                                     string minMaxType)
        {
            InvokeIfRequired(_vtk.SetScalarBarText, fieldName, componentName, unitAbbreviation, complexComponent, minMaxType);
        }
        public void DrawLegendBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawScalarBarBackground, drawBackground);
        }
        public void DrawLegendBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawScalarBarBorder, drawBorder);
        }
        // Color bar
        public void InitializeColorBarWidgetPosition()
        {
            InvokeIfRequired(_vtk.InitializeColorBarWidgetPosition);
        }
        public void SetColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            InvokeIfRequired(_vtk.SetColorBarColorsAndLabels, colors, labels);
        }
        public void AddColorBarColorsAndLabels(Color[] colors, string[] labels)
        {
            InvokeIfRequired(_vtk.AddColorBarColorsAndLabels, colors, labels);
        }
        public void DrawColorBarBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawColorBarBackground, drawBackground);
        }
        public void DrawColorBarBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawColorBarBorder, drawBorder);
        }
        public void HideColorBar()
        {
            InvokeIfRequired(_vtk.HideColorBar);
        }
        // Status bar
        public void SetStatusBlockVisibility(bool draw)
        {
            InvokeIfRequired(_vtk.SetStatusBlockVisibility, draw);
        }
        public void SetStatusBlockNumberFormat(string numberFormat)
        {
            InvokeIfRequired(_vtk.SetStatusBlockNumberFormat, numberFormat);
        }
        public void DrawStatusBlockBackground(bool drawBackground)
        {
            InvokeIfRequired(_vtk.DrawStatusBlockBackground, drawBackground);
        }
        public void DrawStatusBlockBorder(bool drawBorder)
        {
            InvokeIfRequired(_vtk.DrawStatusBlockBorder, drawBorder);
        }
        public void SetStatusBlock(string name, DateTime dateTime, float analysisTime, string unit, string deformationVariable,
                                   float scaleFactor, vtkControl.vtkMaxFieldDataType fieldType, int stepNumber,
                                   int incrementNumber, int nodalDiameter)
        {
            InvokeIfRequired(() => _vtk.SetStatusBlock(name, dateTime, analysisTime, unit, deformationVariable, scaleFactor,
                                                        fieldType, stepNumber, incrementNumber, nodalDiameter));
        }
        // General
        public void SetBackground(bool gradient, Color topColor, Color bottomColor, bool redraw)
        {
            InvokeIfRequired(_vtk.SetBackground, gradient, topColor, bottomColor, redraw);
        }
        public void SetLighting(double ambient, double diffuse, bool redraw)
        {
            InvokeIfRequired(_vtk.SetLighting, ambient, diffuse, redraw);
        }
        public void SetNavigationSensitivity(double rotateSensitivity, double zoomSensitivity, bool invertZoom, bool smoothZoom)
        {
            InvokeIfRequired(() => _vtk.SetNavigationSensitivity(rotateSensitivity, zoomSensitivity, invertZoom, smoothZoom));
        }
        public void SetTrackpadNavigation(bool enabled, bool useWheelFallback, bool twoFingerDragPans,
                                          double panSensitivity, bool invertX, bool invertY)
        {
            InvokeIfRequired(() => _vtk.SetTrackpadNavigation(enabled, useWheelFallback, twoFingerDragPans,
                                                              panSensitivity, invertX, invertY));
        }
        public void SetSmoothing(bool pointSmoothing, bool lineSmoothing, bool redraw)
        {
            InvokeIfRequired(_vtk.SetSmoothing, pointSmoothing, lineSmoothing, redraw);
        }
        // Highlight
        public void SetHighlightColor(Color primaryHighlightColor, Color secondaryHighlightColor)
        {
            InvokeIfRequired(_vtk.SetHighlightColor, primaryHighlightColor, secondaryHighlightColor);
        }
        public void SetMouseHighlightColor(Color mouseHighlightColor)
        {
            InvokeIfRequired(_vtk.SetMouseHighlightColor, mouseHighlightColor);
        }
        // Symbols
        public void SetDrawSymbolEdges(bool drawSilhouettes)
        {
            InvokeIfRequired(_vtk.SetDrawSymbolEdges, drawSilhouettes);
        }
        //
        public void CropPartWithCylinder(string partName, double r, string fileName)
        {
            InvokeIfRequired(_vtk.CropPartWithCylinder, partName, r, fileName);
        }
        public void CropPartWithCube(string partName, double x, double y, double z, double a, string fileName)
        {
            InvokeIfRequired(_vtk.CropPartWithCube, partName, x, y, z, a, fileName);
        }
        public void SmoothPart(string partName, double a, string fileName)
        {
            InvokeIfRequired(_vtk.SmoothPart, partName, a, fileName);
        }
        // User pick
        public void ActivateUserPick()
        {
            _vtk.UserPick = true;
        }
        public void DeactivateUserPick()
        {
            _vtk.UserPick = false;
        }
        #endregion  ################################################################################################################

        #region Results  ###########################################################################################################
        // Results
        public void SetFieldData(string name, string component)
        {
            InvokeIfRequired(() =>
            {
                SetFieldData(name, component, GetCurrentFieldOutputStepId(), GetCurrentFieldOutputStepIncrementId());
            });
        }
        // The results plot type, as the results toolbar sets it. Setting the controller property alone
        // leaves the undeformed model and the toolbar states behind, so this takes the same path
        public void SetViewResultsType(ViewResultsTypeEnum viewResultsType)
        {
            InvokeIfRequired(() =>
            {
                UndeformedModelTypeEnum undeformedModelType =
                    viewResultsType == ViewResultsTypeEnum.Undeformed ||
                    viewResultsType == ViewResultsTypeEnum.Deformed
                    ? UndeformedModelTypeEnum.None : _controller.GetUndeformedModelType();
                SetUndeformedModelType(viewResultsType, undeformedModelType);
            });
        }
        public void SetResultsUndeformedModelType(UndeformedModelTypeEnum undeformedModelType)
        {
            InvokeIfRequired(() => SetUndeformedModelType(_controller.ViewResultsType, undeformedModelType));
        }
        public void SetFieldData(string name, string component, int stepId, int stepIncrementId)
        {
            CommitViewportEdit();
            //
            FieldData fieldData = new FieldData(name, component, stepId, stepIncrementId);
            FieldData currentData = _controller.CurrentFieldData;
            // In case the currentData is null exit
            if (currentData == null) return;
            //
            if (!fieldData.Equals(currentData)) // update results only if field data changed
            {
                // Stop and update animation data only if field data changed
                if (_frmAnimation.Visible) _frmAnimation.Hide();
                //
                if (fieldData.Name == currentData.Name && fieldData.Component == currentData.Component)
                {
                    // Step id or increment id changed                                              

                    // Find the chosen data; also contains info about type of step ...
                    fieldData = _controller.CurrentResult.GetFieldData(fieldData.Name,
                                                                       fieldData.Component,
                                                                       fieldData.StepId,
                                                                       fieldData.StepIncrementId,
                                                                       true);
                    // Update controller field data
                    _controller.CurrentFieldData = fieldData;
                    // Draw deformation or field data
                    if (_controller.ViewResultsType != ViewResultsTypeEnum.Undeformed) _controller.DrawResults(false);
                }
                else
                {
                    // Field of field component changed                                                 

                    // Update controller field data; this is used for the SetStepAndIncrementIds to detect missing ids
                    _controller.CurrentFieldData = fieldData;
                    // Find the existing chosen data; also contains info about type of step ...
                    fieldData = _controller.CurrentResult.GetFieldData(fieldData.Name,
                                                                       fieldData.Component,
                                                                       fieldData.StepId,
                                                                       fieldData.StepIncrementId,
                                                                       true);
                    // Update controller field data
                    _controller.CurrentFieldData = fieldData;
                    // Draw field data
                    if (_controller.ViewResultsType == ViewResultsTypeEnum.ColorContours)
                        _controller.UpdatePartsScalarFields();
                    else if (_controller.ViewResultsType == ViewResultsTypeEnum.VectorPlot ||
                             _controller.ViewResultsType == ViewResultsTypeEnum.TensorPlot)
                        _controller.DrawResults(false);
                }
                //
                UpdateComplexControlStates();
                // Move focus from step and step increment dropdown menus
                this.ActiveControl = null;
            }
        }
        public void SetAllStepAndIncrementIds(bool reset = false)
        {
            InvokeIfRequired(() =>
            {
                // Save current step and increment id
                string currentStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
                string[] prevStepIncrementIds = null;
                if (!reset && currentStepIncrement != null)
                {
                    prevStepIncrementIds = currentStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                }
                // Set all increments
                tscbStepAndIncrement.SelectedIndexChanged -= tscbStepAndIncrement_SelectionChanged;  // detach event
                tscbStepAndIncrement.Items.Clear();
                Dictionary<int, int[]> allIds = _controller.CurrentResult.GetAllExistingIncrementIds();
                int lastStepId = 1;
                int lastIncrementId = 0;
                foreach (var entry in allIds)
                {
                    foreach (int incrementId in entry.Value)
                    {
                        tscbStepAndIncrement.Items.Add(entry.Key.ToString() + ", " + incrementId);
                        lastIncrementId = incrementId;
                    }
                    lastStepId = entry.Key;
                }
                tscbStepAndIncrement.SelectedIndexChanged += tscbStepAndIncrement_SelectionChanged;  // reattach event
                // Reselect previous step and increment
                if (prevStepIncrementIds != null)
                {
                    int stepId = Math.Min(int.Parse(prevStepIncrementIds[0]), lastStepId);
                    int incrementId = Math.Min(int.Parse(prevStepIncrementIds[1]), lastIncrementId);
                    SetStepAndIncrementIds(stepId, incrementId);
                }
                else SetDefaultStepAndIncrementIds();
            });
        }
        public void SetStepAndIncrementIds(int stepId, int incrementId)
        {
            InvokeIfRequired(() =>
            {
                string stepIncrement = stepId + ", " + incrementId;
                // Set the combo box
                if (tscbStepAndIncrement.Items.Contains(stepIncrement))
                {
                    tscbStepAndIncrement.SelectedIndexChanged -= tscbStepAndIncrement_SelectionChanged;    // detach event
                    tscbStepAndIncrement.SelectedItem = stepIncrement;
                    // Set the step and increment if the combo box set was successful
                    FieldData data = _controller.CurrentFieldData;
                    data.StepId = stepId;
                    data.StepIncrementId = incrementId;
                    _controller.CurrentFieldData = data;   // to correctly update the increment time
                    tscbStepAndIncrement.SelectedIndexChanged += tscbStepAndIncrement_SelectionChanged;  // reattach event
                }
                else SetDefaultStepAndIncrementIds();
            });
        }
        public void SetDefaultStepAndIncrementIds()
        {
            string[] tmp;
            FieldData fieldData = _controller.CurrentFieldData;
            if (fieldData != null)
            {
                if (fieldData.StepId == -1 && fieldData.StepIncrementId == -1) return;
                else SetStepAndIncrementIds(fieldData.StepId, fieldData.StepIncrementId);
            }
            //
            return;
            //
            if (_controller.CurrentFieldData.StepType == CaeResults.StepTypeEnum.Frequency)
            {
                string firstStepIncrement = (string)tscbStepAndIncrement.Items[tscbStepAndIncrement.Items.Count - 1];
            }
            else
            {
                string lastStepIncrement = (string)tscbStepAndIncrement.Items[tscbStepAndIncrement.Items.Count - 1];
                tmp = lastStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
                
            }
        }
        public int GetCurrentFieldOutputStepId()
        {
            string selectedStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
            string[] tmp = selectedStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            return int.Parse(tmp[0]);
        }
        public int GetCurrentFieldOutputStepIncrementId()
        {
            string selectedStepIncrement = (string)tscbStepAndIncrement.SelectedItem;
            string[] tmp = selectedStepIncrement.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            return int.Parse(tmp[1]);
        }
        public float GetAnimationScaleFactor(int frameNumber)
        {
            return _vtk.GetAnimationScaleFactor(frameNumber);
        }
        public void SetAnimationAcceleration(bool animationAcceleration)
        {
            InvokeIfRequired(_vtk.SetAnimationAcceleration, animationAcceleration);
        }
        public void SetAnimationFrameData(float[] time, int[] stepId, int[] stepIncrementId, int[] modeNumber,
                                          int[] nodalDiameter, float[] scale, double[] allFramesScalarRange, vtkMaxAnimationType animationType)
        {
            InvokeIfRequired(_vtk.SetAnimationFrameData, time, stepId, stepIncrementId, modeNumber, nodalDiameter, scale,
                             allFramesScalarRange, animationType);
        }
        public void SetAnimationFrame(int frameNum, bool scalarRangeFromAllFrames)
        {
            CommitViewportEdit();
            InvokeIfRequired(_vtk.SetAnimationFrame, frameNum, scalarRangeFromAllFrames);
        }
        public void SaveAnimationAsAVI(string fileName, int[] firstLastFrame, int step, int fps, bool scalarRangeFromAllFrames,
                                       bool swing, bool encoderOptions)
        {
            InvokeIfRequired(_vtk.SaveAnimationAsAVI, fileName, firstLastFrame, step, fps, scalarRangeFromAllFrames,
                             swing, encoderOptions);
        }
        public void SaveAnimationAsImages(string fileName, int[] firstLastFrame, int step, bool scalarRangeFromAllFrames,
                                          bool swing)
        {
            InvokeIfRequired(_vtk.SaveAnimationAsImages, fileName, firstLastFrame, step, scalarRangeFromAllFrames, swing);
        }
        // Widgets
        public void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint,
                                   bool drawBackground, bool drawBorder, bool visible)
        {
            AddArrowWidget(name, text, numberFormat, anchorPoint, drawBackground, drawBorder, visible, null);
        }
        public void AddArrowWidget(string name, string text, string numberFormat, double[] anchorPoint,
                                   bool drawBackground, bool drawBorder, bool visible, string[] animationActorNames)
        {
            InvokeIfRequired(() => _vtk.AddArrowWidget(name, text, numberFormat, anchorPoint, drawBackground, drawBorder,
                                                       visible, animationActorNames));
        }
        public void UpdateArrowWidgetText(string name, string text)
        {
            InvokeIfRequired(_vtk.UpdateArrowWidgetText, name, text);
        }
        public void UpdateArrowWidgetAnimationData(string name, string text, double[] anchorPoint)
        {
            InvokeIfRequired(_vtk.UpdateArrowWidgetAnimationData, name, text, anchorPoint);
        }
        public bool TryGetAnimationNodeData(int nodeId, out double[] coordinates, out float value)
        {
            return _vtk.TryGetAnimationNodeData(nodeId, out coordinates, out value);
        }
        public void ArrangeVisibleArrowWidgets()
        {
            InvokeIfRequired(_vtk.ArrangeVisibleArrowWidgets);
        }
        public void RemoveAllArrowWidgets()
        {
            InvokeIfRequired(_vtk.RemoveAllArrowWidgets);
        }
        public void RemoveArrowWidgets(string[] widgetNames)
        {
            InvokeIfRequired(_vtk.RemoveArrowWidgets, widgetNames);
        }

        #endregion  ################################################################################################################

        #region Tree  ##############################################################################################################
        // Tree
        public void RegenerateTreeCallback()
        {
            RegenerateTree();
        }
        public void RegenerateTree(bool remeshing = false)
        {
            InvokeIfRequired(_modelTree.RegenerateTree, _controller.Model, _controller.Jobs, _controller.CurrentResult, remeshing);
            InvokeIfRequired(UpdateSymbolsList);
            InvokeIfRequired(SelectLastSymbolName);
        }
        public void AddTreeNode(ViewGeometryModelResults view, NamedClass item, string parentName)
        {
            ViewType viewType = GetViewType(view);
            _controller.TryGetOrderedDictionaryItemLayout(item.GetType(), view, parentName,
                                                          out IOrderedDictionaryItemLayout<string> orderedDictionary);
            //
            InvokeIfRequired(_modelTree.AddTreeNode, viewType, item, parentName, orderedDictionary);
            if (item is Step) UpdateSymbolsList();
        }
        public void UpdateTreeNode(ViewGeometryModelResults view, string oldItemName, NamedClass item, string parentName,
                                   bool updateSelection = true)
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.UpdateTreeNode, viewType, oldItemName, item, parentName, updateSelection);
            if (item is Step) UpdateSymbolsList();
        }
        public void UpdateTreeNodeLayout(ViewGeometryModelResults view, string itemName, NamedClass item,
                                         string parentName)
        {
            ViewType viewType = GetViewType(view);
            InvokeIfRequired(_modelTree.UpdateTreeNodeLayout, viewType, itemName, item, parentName);
        }
        public void SwapTreeNode(ViewGeometryModelResults view, string firstItemName, NamedClass firstItem,
                                string secondItemName, NamedClass secondItem, string parentName)
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.SwapTreeNodes, viewType, firstItemName, firstItem, secondItemName,
                             secondItem, parentName);
            //if (item is Step) UpdateOneStepInSymbolsForStepList(oldItemName, item.Name);
        }
        public void RemoveTreeNode<T>(ViewGeometryModelResults view, string nodeName, string parentName) where T : NamedClass
        {
            ViewType viewType = GetViewType(view);
            //
            InvokeIfRequired(_modelTree.RemoveTreeNode<T>, viewType, nodeName, parentName);
            if (typeof(T) == typeof(Step)) UpdateSymbolsList();
        }
        public bool[][] GetTreeExpandCollapseState()
        {
            if (InvokeRequired)
            {
                return (bool[][])Invoke((Func<bool[][]>)delegate
                    { return _modelTree.GetAllTreesExpandCollapseState(out string[][] afterNodeNames); });
            }
            else
            {
                return _modelTree.GetAllTreesExpandCollapseState(out string[][] afterNodeNames);
            }
        }
        public void SetTreeExpandCollapseState(bool[][] states)
        {
            InvokeIfRequired(_modelTree.SetAllTreeExpandCollapseState, states);
        }
        public Dictionary<string, bool>[] GetStableTreeExpandCollapseState()
        {
            if (InvokeRequired)
                return (Dictionary<string, bool>[])Invoke((Func<Dictionary<string, bool>[]>)
                    _modelTree.GetStableTreesExpandCollapseState);
            else return _modelTree.GetStableTreesExpandCollapseState();
        }
        public void SetStableTreeExpandCollapseState(Dictionary<string, bool>[] states)
        {
            InvokeIfRequired(_modelTree.SetStableTreesExpandCollapseState, states);
        }
        public void UpdateHighlight()
        {
            if (_allForms == null) return;
            //
            List<IFormHighlight> highlightForms = new List<IFormHighlight>();
            foreach (var aForm in _allForms)
            {
                // Do not count the Query form
                if (aForm.Visible && (aForm is IFormHighlight ihf)) highlightForms.Add(ihf);
            }
            if (highlightForms.Count == 0) UpdateHighlightFromTree();
            else if (highlightForms.Count == 1) highlightForms[0].Highlight();
            else throw new NotSupportedException();
        }
        public void UpdateHighlightFromTree()
        {
            InvokeIfRequired(_modelTree.UpdateHighlight);
        }
        public void SelectBaseParts(string[] partNames)
        {
            MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0);
            InvokeIfRequired(SelectBaseParts, e, Keys.None, partNames, false);
        }
        public void SelectBaseParts(MouseEventArgs e, Keys modifierKeys, string[] partNames)
        {
            // While a dialog picks the sub-parts of a compound one by one, a right click on one acts on that
            // sub-part too, and not on the compound it belongs to
            SelectBaseParts(e, modifierKeys, partNames, !_controller.PickCompoundSubPartsSeparately);
        }
        // A pick in the viewport selects the whole compound of a picked sub-part unless a dialog picks sub-parts
        // one by one, while parts named in a list are
        // selected as named, so a single sub-part can be chosen
        private void SelectBaseParts(MouseEventArgs e, Keys modifierKeys, string[] partNames,
                                     bool selectCompoundOfSubParts)
        {
            if ((partNames != null && partNames.Length == 0) || 
                (partNames != null && partNames.Length > 0 && partNames[0] == null))
            {
                if (modifierKeys != Keys.Shift && modifierKeys != Keys.Control)
                {
                    if (e.Clicks == 2) SetZoomToFit(true);
                    else _controller.ClearAllSelection();
                }
            }
            else
            {
                //
                if (e.Clicks == 2) _modelTree.EditSelectedPart();
                else
                {
                    int count = 0;
                    BasePart[] parts;
                    int numOfSelectedTreeNodes = 0;
                    //
                    if (GetCurrentView() == ViewGeometryModelResults.Geometry)
                    {
                        if (selectCompoundOfSubParts) parts = _controller.GetGeometryPartsForSelection(partNames);
                        else parts = _controller.GetGeometryParts(partNames).Where(p => p != null).ToArray();
                    }
                    else if (GetCurrentView() == ViewGeometryModelResults.Model)
                        parts = _controller.GetModelParts(partNames);
                    else if (GetCurrentView() == ViewGeometryModelResults.Results)
                        parts = _controller.GetResultParts(partNames);
                    else throw new NotSupportedException();
                    // While a dialog has the tree locked, the picking belongs to the dialog, and the menu of a
                    // part is shown without selecting it in the tree
                    if (e.Button == MouseButtons.Right && parts.Length > 0 && _modelTree.DisableMouse &&
                        GetCurrentView() == ViewGeometryModelResults.Geometry)
                    {
                        string[] menuPartNames = parts.Select(p => p.Name).ToArray();
                        // A dialog that picks parts treats a right click the way the parts are treated without
                        // one: a part that is not selected is picked as a left click would pick it, and a
                        // selected one keeps the selection, so the menu acts on all of it
                        if (_controller.Selection.SelectItem == vtkSelectItem.Part)
                        {
                            string[] selectedPartNames = GetSelectedGeometryPartNames();
                            if (!menuPartNames.All(selectedPartNames.Contains))
                            {
                                _vtk.Pick(e.X, e.Y, false, e.X, e.Y);
                                selectedPartNames = GetSelectedGeometryPartNames();
                            }
                            if (selectedPartNames.Length > 0) menuPartNames = selectedPartNames;
                        }
                        // Otherwise the part is highlighted over what the dialog draws - which is drawn again
                        // first, so that a part an earlier right click highlighted is let go - and the dialog
                        // draws its highlight again once the menu has closed
                        else
                        {
                            UpdateHighlight();
                            _controller.Highlight3DObjects(ViewGeometryModelResults.Geometry, parts, false, false);
                        }
                        _modelTree.ShowContextMenuForGeometryParts(_vtk, e.X, _vtk.Height - e.Y, menuPartNames);
                        return;
                    }
                    // A selected mesh setup item highlights the parts it is defined on, so a right click on
                    // one of them acts on all of those parts at once; the mesh setup item stays selected in
                    // the tree and the parts are not selected there
                    if (e.Button == MouseButtons.Right && parts.Length > 0)
                    {
                        string[] meshSetupItemPartNames = GetSelectedMeshSetupItemPartNames(parts);
                        if (meshSetupItemPartNames != null)
                        {
                            _modelTree.ShowContextMenuForGeometryParts(_vtk, e.X, _vtk.Height - e.Y,
                                                                       meshSetupItemPartNames);
                            return;
                        }
                    }
                    //
                    foreach (var part in parts)
                    {
                        numOfSelectedTreeNodes = _modelTree.SelectBasePart(e, modifierKeys, part, false);
                        count++;
                        //
                        if (count == 1 && modifierKeys == Keys.None) modifierKeys |= Keys.Shift;
                    }
                    _modelTree.UpdateHighlight();
                    //
                    if (numOfSelectedTreeNodes > 0 && e.Button == MouseButtons.Right)
                    {
                        _modelTree.ShowContextMenu(_vtk, e.X, _vtk.Height - e.Y);
                    }
                }
            }
        }
        // The geometry parts the current selection names, when it picks parts
        private string[] GetSelectedGeometryPartNames()
        {
            List<string> partNames = new List<string>();
            foreach (int id in _controller.GetSelectionIds())
            {
                BasePart part = _controller.Model.Geometry.GetPartFromGeometryId(id);
                if (part != null && !partNames.Contains(part.Name)) partNames.Add(part.Name);
            }
            return partNames.ToArray();
        }
        // Returns the names of the parts the selected mesh setup items are defined on, but only when the
        // given parts are among them
        private string[] GetSelectedMeshSetupItemPartNames(BasePart[] parts)
        {
            string[] meshSetupItemNames = _modelTree.GetSelectedMeshSetupItemNames();
            if (meshSetupItemNames == null || meshSetupItemNames.Length == 0) return null;
            //
            string[] meshSetupItemPartNames = _controller.GetPartNamesToMeshFromMeshSetupItems(meshSetupItemNames);
            if (meshSetupItemPartNames == null || meshSetupItemPartNames.Length == 0) return null;
            //
            HashSet<string> partNames = new HashSet<string>(meshSetupItemPartNames);
            foreach (BasePart part in parts)
            {
                if (!partNames.Contains(part.Name)) return null;
            }
            return meshSetupItemPartNames;
        }
        public void SelectFirstComponentOfFirstFieldOutput()
        {
            InvokeIfRequired(_modelTree.SelectFirstComponentOfFirstFieldOutput);
        }
        //
        public void SetNumberOfModelUserKeywords(int numOfUserKeywords)
        {
            InvokeIfRequired(_modelTree.SetNumberOfUserKeywords, numOfUserKeywords);
        }
        public void SetNumberOfModelModifiedKeywords(int numOfModifiedKeywords)
        {
            InvokeIfRequired(_modelTree.SetNumberOfModifiedKeywords, numOfModifiedKeywords);
        }

        #endregion  ################################################################################################################

        // Output
        private void tsmiClearOutputTextbox_Click(object sender, EventArgs e)
        {
            _outputLines.Clear();
            _outputLineIsMessage.Clear();
            tbOutput.Clear();
        }
        public void WriteDataToOutput(string data)
        {
            WriteDataToOutput(data, false);
        }
        /// <summary>
        /// Writes an error, whose surfaces and edges become links to the geometry. A line of ordinary output
        /// names geometry too, but a link on every one of them buries the ones worth following.
        /// </summary>
        public void WriteErrorToOutput(string data)
        {
            WriteDataToOutput(data, true);
        }
        private void WriteDataToOutput(string data, bool isError)
        {
            if (data == null) return;
            // An empty line contains only the date
            if (data.Length == 0 && _outputLines.Count > 0 &&
                _outputLines.Last().Length == _numOfOutputDateChars) return;
            //
            InvokeIfRequired(() =>
            {
                data = data.Replace("\r\n", "\n");
                data = data.Replace('\r', '\n');
                string[] lines = data.Split('\n');
                //
                foreach (var line in lines)
                {
                    WriteLineToOutputWithDate(line, isError);
                    //
                    Console.WriteLine(line);
                }
                //
                timerOutput.Start();
            });
        }
        private void WriteLineToOutputWithDate(string data, bool isError)
        {
            _outputLines.Add(DateTime.Now.ToString("MM/dd/yy HH:mm:ss").PadRight(_numOfOutputDateChars) + data);
            _outputLineIsMessage.Add(isError || IsWarningOrErrorLine(data));
            //
            if (_outputLines.Count > _numOfOutputLines)
            {
                _outputLines.RemoveRange(0, _outputLines.Count - _numOfOutputLines);
                _outputLineIsMessage.RemoveRange(0, _outputLineIsMessage.Count - _numOfOutputLines);
            }
        }
        /// <summary>
        /// A warning or an error written as ordinary output, the way the meshers and gmsh write them.
        /// </summary>
        private static bool IsWarningOrErrorLine(string data)
        {
            string text = data.TrimStart();
            return text.StartsWith("Warning", StringComparison.OrdinalIgnoreCase) ||
                   text.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
        }
        private int[][] FindOutputLinks(int line, string text)
        {
            if (line < 0 || line >= _outputLineIsMessage.Count || !_outputLineIsMessage[line]) return null;
            int[][] geometryRanges = _controller.GetGeometryNameRanges(text);
            int[][] invertedElementRanges = _controller.GetInvertedElementReferenceRanges(text);
            if (geometryRanges == null) return invertedElementRanges;
            if (invertedElementRanges == null) return geometryRanges;
            return geometryRanges.Concat(invertedElementRanges).ToArray();
        }
        private void UpdateOutputTextBox()
        {
            // The text box wraps the lines itself, indented past the date, so a line stays one line of text and
            // a name split by the wrapping is still one link
            tbOutput.SetOutputText(string.Join("\n", _outputLines));
        }

        #region Invoke  ############################################################################################################
        // Invoke
        private void InvokeMarshaled(MethodInvoker method)
        {
            // Control.Invoke rethrows with a plain throw, which replaces the stack trace of the exception with
            // the one of the marshalling call, so the frames that actually failed on the user interface thread
            // are captured here and restored by the rethrow
            ExceptionDispatchInfo capturedException = null;
            //
            this.Invoke((MethodInvoker)delegate ()
            {
                try
                {
                    method();
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }
            });
            //
            if (capturedException != null) capturedException.Throw();
        }
        public T InvokeIfRequired<T>(Func<T> func)
        {
            if (this.InvokeRequired)
            {
                T result = default(T);
                InvokeMarshaled(delegate () { result = func(); });
                return result;
            }
            else
            {
                return func();
            }
        }
        public void InvokeIfRequired(Action action)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate () { action(); });
            }
            else
            {
                action();
            }
        }
        public void InvokeIfRequired<T>(Action<T> action, T parameter)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate() { action(parameter); });
            }
            else
            {
                action(parameter);
            }
        }
        public void InvokeIfRequired<T1,T2>(Action<T1, T2> action, T1 parameter1, T2 parameter2)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate() { action(parameter1, parameter2); });
            }
            else
            {
                action(parameter1, parameter2);
            }
        }
        public void InvokeIfRequired<T1, T2, T3>(Action<T1, T2, T3> action, T1 parameter1, T2 parameter2, T3 parameter3)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate() { action(parameter1, parameter2, parameter3); });
            }
            else
            {
                action(parameter1, parameter2, parameter3);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 parameter1, T2 parameter2, T3 parameter3, 
                                                     T4 parameter4)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate() { action(parameter1, parameter2, parameter3, parameter4); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 parameter1, T2 parameter2,
                                     T3 parameter3, T4 parameter4, T5 parameter5)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate() { action(parameter1, parameter2, parameter3, parameter4, parameter5); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 parameter1, T2 parameter2, 
                                     T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate () { action(parameter1, parameter2, parameter3, parameter4, 
                                                         parameter5, parameter6); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 parameter1, 
                                     T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate () { action(parameter1, parameter2, parameter3, parameter4, parameter5,
                                                         parameter6, parameter7); });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 parameter1,
                                     T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6, T7 parameter7,
                                     T8 parameter8)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate () {
                    action(parameter1, parameter2, parameter3, parameter4, parameter5,
                    parameter6, parameter7, parameter8);
                });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8);
            }
        }
        public void InvokeIfRequired<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action,
                                     T1 parameter1, T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6,
                                     T7 parameter7, T8 parameter8, T9 parameter9)
        {
            if (this.InvokeRequired)
            {
                InvokeMarshaled(delegate () {
                    action(parameter1, parameter2, parameter3, parameter4, parameter5,
                    parameter6, parameter7, parameter8, parameter9);
                });
            }
            else
            {
                action(parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8, parameter9);
            }
        }



































        #endregion  ################################################################################################################

        #endregion

        private void tsmiTest_Click(object sender, EventArgs e)
        {
            try
            {
                //_vtk.SwitchLights();
                //_controller.TestCreateSurface();
                //AnimateModel58();
                //_vtk.Export(GetFileNameToSaveAs());
                //TestSpring();
                //TestSuperposition();
                //TestNormals();
                //TestWearResults();
                //TestMmg();
                //TestGmshReadMesh();
                //TestDefeature();
                //AnimateRotation();
                TestParameterization();
                //TestImportedStLoad();
                //Test3MF();
                //TestPython();
            }
            catch
            {

            }
        }
        private void TestPython()
        {
            UpdatePythonEngine();
            string result = PythonEngineManager.Execute(
                "result_names = pmx.results.get_result_names()\n" +
                "for rn in result_names:\n" +
                "    print(rn)\n" +
                "    field_names = pmx.results.get_field_names(rn)\n" +
                "    for fn in field_names:\n" +
                "        print('    ' + fn)\n"
                );
            MessageBoxes.ShowInfo(result);
        }
        private void Test3MF()
        {
            string fileName = @"C:\Temp\Test.3mf";
            vtkMaxActorData[] vtkMaxActorDatas = _vtk.GetVtkMaxActorData();
            vtkMaxLookupTable lookupTable = _vtk.GetLookupTable();
            double[] closestUpView = _vtk.GetClosestUpView();
            ThreeMFFileWriter.Write(fileName, vtkMaxActorDatas, lookupTable, 1, closestUpView);
        }
        private void TestImportedStLoad()
        {
            int n = 32;
            int count = 0;
            double delta = 2.0 / (n - 1);
            double[][] coor = new double[n * n][];
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    coor[count] = new double[] { -1 + i * delta, -1 + j * delta, 0 };
                    count++;
                }
            }
            //
            double v1 = -1;
            double v2 = -2;
            double v3 = -3;
            double v4 = -4;
            double x;
            double y;

            double minX = 13.15;
            double maxX = minX + 300;
            double minY = 20.87;
            double maxY = minY + 300;
            double z = 129.5;

            count = 0;
            double[] values = new double[n * n];
            string[] lines = new string[n * n];
            for (int i = 0; i < values.Length; i++)
            {
                x = coor[count][0];
                y = coor[count][1];
                values[count] = v1 * 0.25 * (1 - x) * (1 - y) +
                                v2 * 0.25 * (1 + x) * (1 - y) +
                                v3 * 0.25 * (1 + x) * (1 + y) +
                                v4 * 0.25 * (1 - x) * (1 + y);
                //
                coor[count][0] = minX + (maxX - minX) * (x + 1) / 2;
                coor[count][1] = minY + (maxY - minY) * (y + 1) / 2;
                coor[count][2] = z;
                //
                lines[count] = string.Format("{0} {1} {2} {3} {4} {5}{6}", coor[count][0], coor[count][1], coor[count][2],
                                                                           0, 0, values[count], Environment.NewLine);
                //
                count++;

            }

            File.WriteAllLines(@"c:\Temp\load.txt", lines);
        }
        private void TestMmg()
        {
            string partName1 = "Solid_part-1";
            string partName2 = "Solid_part-2";
            string fileName = @"C:\Temp\mmg\tmp.sol";
            //
            //_controller.ExportSignedDistance(fileName, partName2, partName1);
            //
            if (_controller.CurrentResult != null && _controller.CurrentResult.Mesh != null)
            {
                SetResultNames();
                // Reset the previous step and increment
                SetAllStepAndIncrementIds();
                // Set last increment
                SetDefaultStepAndIncrementIds();
                // Show the selection in the results tree
                SelectFirstComponentOfFirstFieldOutput();
            }
            // Set the representation which also calls Draw
            _controller.ViewResultsType = ViewResultsTypeEnum.ColorContours;  // Draw
            //
            SetMenuAndToolStripVisibility();
        }
        private void TestWearResults()
        {
            string fileName = @"C:\Temp\Ignatijev\Model8\Analysis-1.frd";
            _controller.ReadFrdFileAsWear(fileName);
        }
        private void TestNormals()
        {
            string partName = "Shell_part-1";
            _controller.SuppressExplodedView(new string[] { partName });
            //
            MeshPart part = _controller.GetModelPart(partName);
            GeometryPart geometryPart = _controller.GetGeometryPart(partName);
            //
            Dictionary<int, FeNode[]> faceIdNodes = new Dictionary<int, FeNode[]>();
            //
            HashSet<int> nodeIds;
            List<FeNode> nodes;
            for (int i = 0; i < part.Visualization.FaceCount; i++)
            {
                nodes = new List<FeNode>();
                nodeIds = part.Visualization.GetNodeIdsForSurfaceId(i);
                foreach (var nodeId in nodeIds)
                {
                    nodes.Add(_controller.Model.Mesh.Nodes[nodeId]);
                }
                //
                faceIdNodes.Add(FeMesh.GmshTopologyId(i, part.PartId), nodes.ToArray());
            }
            //
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //
            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return;
            }
            string brepFileName = Path.Combine(workDirectory, Globals.BrepFileName);
            //
            if (File.Exists(brepFileName)) File.Delete(brepFileName);
            //
            File.WriteAllText(brepFileName, geometryPart.CADFileData);
            GmshData gmshData = new GmshData();
            gmshData.GeometryFileName = brepFileName;
            gmshData.FaceIdNodes = faceIdNodes;
            _controller.Model.Geometry.GetPartTopologyForGmsh(geometryPart.Name, ref gmshData);
            _controller.ResumeExplodedViews(false);
            //
            GmshAPI gmshAPI = new GmshAPI(gmshData, WriteDataToOutput);
            string error = gmshAPI.GetOccNormals();
            Dictionary<int, List<Vec3D>> normals = gmshAPI.GmshData.NodeIdNormals;
        }
        private void TestGmshReadMesh()
        {
            string workDirectory = _controller.Settings.GetWorkDirectory();
            //

            if (workDirectory == null || !Directory.Exists(workDirectory))
            {
                MessageBoxes.ShowWorkDirectoryError();
                return;
            }
            string meshFileName = Path.Combine(workDirectory, Globals.GmshMeshFileName);
            //
            if (File.Exists(meshFileName)) File.Delete(meshFileName);
            //
            GmshMshFileWriter.Write(meshFileName, _controller.Model.Mesh);
            //
            GmshData gmshData = new GmshData();
            gmshData.MeshFileName = meshFileName;
            //
            GmshAPI gmshAPI = new GmshAPI(gmshData, null);
            string error = gmshAPI.GetElementQualities();
        }
        private void TestDefeature()
        {
            _controller.Defeature(null);
        }
        private void TestParameterization(int uLineCount = 3, int vLineCount = 3)
        {
            _controller.ViewParameterization(uLineCount, vLineCount);
            _vtk.Invalidate();
        }
        private void TestSuperposition()
        {
            _controller.CurrentResult.TestSuperposition();
        }
        private void TestEquation()
        {
            EquationContainer equationContainer1;
            EquationContainer equationContainer2;
            for (int i = 0; i < 10000; i++)
            {
                equationContainer1 = new EquationContainer(typeof(StringLengthConverter), 1.4);
                equationContainer2 = new EquationContainer(typeof(StringLengthConverter), 1);
                EquationContainer.SetAndCheck(ref equationContainer2, equationContainer1, null, false);
            }
        }
        private void TestSpring()
        {
            int[] directions;
            PointSpring pointSpring;
            PointSpringData pointSpringData;
            List<CaeModel.Constraint> constraints = new List<CaeModel.Constraint>();
            for (int i = 0; i < 10000; i++)
            {
                pointSpring = new PointSpring("spring", 101, 1, 2, 3, false, false);
                directions = pointSpring.GetSpringDirections();
            }
            directions = null;
            //
            for (int i = 0; i < 10000; i++)
            {
                pointSpringData = new PointSpringData("spring", 101, 1, 2, 3);
                directions = pointSpringData.GetSpringDirections();
            }
        }
        private void AnimateRotation()
        {
            tbOutput.Clear();
            Thread.Sleep(1000);
            int nFrames = 360;
            double delta = 360.0 / nFrames;
            double angle;
            double alpha;
            string[] partNames = new string[] { "Solid_part-1" };
            //
            //AnimateTransparency("Solid_part-1", 230, 20, 10);
            string fileName = @"c:\Temp\screen";
            _controller.SetTransparencyForGeometryParts(partNames, 255);
            _vtk.RenderToPNG(fileName + 0.ToString().PadLeft(3, '0') + ".png");
            //
            for (int i = 1; i <= nFrames; i++)
            {
                //_vtk.Rotate(-delta, 0, 0);
                _vtk.Rotate(0, 0, delta);
                angle = i * delta;

                if (i == nFrames / 4)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)100);
                else if (i == nFrames / 2)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)24);
                else if (i == 3 * nFrames / 4)
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)100);



                //if (angle > 80 && angle < 180)
                //{
                //    alpha =  (angle - 80) / (180 - 80);    // 0...1
                //    alpha = (1 - alpha) * 250;
                //    _controller.SetTransparencyForGeometryParts(partNames, (byte)alpha);
                //}
                //if (angle > 270)
                //{
                //    alpha = (angle - 270) / (90);    // 0...1
                //    alpha = (alpha) * 250;
                //    _controller.SetTransparencyForGeometryParts(partNames, (byte)alpha);
                //}

                //Thread.Sleep(10);
                //Application.DoEvents();
                _vtk.RenderToPNG(fileName + i.ToString().PadLeft(3, '0') + ".png");
            }


            //
            //_vtk.AnimateZoomToFactor(3000, 100);
            //
            //if (true)
            //{
            //    int alphaStart = 230;
            //    int alphaEnd = 50;
            //    int alphaStep = 5;
            //    AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-6", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-11", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-10", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-2", alphaStart, alphaEnd, alphaStep);
            //    AnimateTransparency("Shell_part-14", alphaStart, alphaEnd, alphaStep);
            //}
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(6000, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(3000, 1500);
        }
        private void AnimateModel58()
        {
            tbOutput.Clear();
            Thread.Sleep(3000);
            //
            //_vtk.AnimateZoomToFactor(3000, 100);
            //
            if (true)
            {
                int alphaStart = 230;
                int alphaEnd = 50;
                int alphaStep = 5;
                AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-6", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-11", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-10", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-2", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-14", alphaStart, alphaEnd, alphaStep);
            }
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(6000, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            ////
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(3000, 1500);
        }
        private void AnimateSierra()
        {
            tbOutput.Clear();
            Thread.Sleep(3000);
            //
            _vtk.AnimateZoomToFactor(800, 100);
            //
            if (true)
            {
                int alphaStart = 230;
                int alphaEnd = 50;
                int alphaStep = 5;
                AnimateTransparency("Shell_part-16", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-4", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-1", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-17", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-12", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-9", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-7", alphaStart, alphaEnd, alphaStep);
                AnimateTransparency("Shell_part-13", alphaStart, alphaEnd, alphaStep);
            }
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(2000, 1500);
            //
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            //
            //Thread.Sleep(1000);
            //_controller.TurnExplodedViewOnOff(true, 1500);
            //
            //Thread.Sleep(1000);
            //_vtk.AnimateZoomToFactor(800, 1500);
        }
        private void AnimateTransparency(string partName, int alphaFrom, int alphaTo, int alphaStep)
        {
            string[] partNames = new string[] { partName };

            if (alphaFrom > alphaTo)
            {
                for (int i = alphaFrom; i >= alphaTo; i -= alphaStep)
                {
                    //_controller.SetTransparencyForResultParts(partNames, (byte)i);
                    _controller.SetTransparencyForGeometryParts(partNames, (byte)i);
                    Application.DoEvents();
                }
            }
            
        }
        private void timerTest_Tick(object sender, EventArgs e)
        {
            //TestAnimation();
            //TestSelection1();
            TestSelection2();
        }
        private void TestAnimation()
        {
            try
            {
                timerTest.Interval = 10;
                //timerTest.Stop();

                string[] names = new string[] { CaeResults.FOFieldNames.Stress, CaeResults.FOFieldNames.Disp };
                string[] components = new string[] { "SZZ", "D2" };

                CaeResults.FieldData currentData = _controller.CurrentFieldData;
                int i = 0;
                if (currentData.Component == components[i]) i++;

                int len = names.Length;

                SetFieldData(names[i % len], components[i % len], 1, 1);
            }
            catch
            { }
        }
        private void TestSelection1()
        {
            try
            {
                timerTest.Interval = 100;
                //
                string[] allPartNames = _controller.GetGeometryPartNames();
                HashSet<string> selectedPartNames = new HashSet<string>();
                //
                Random rand = new Random();
                for (int i = 0; i < 100; i++)
                {
                    selectedPartNames.Add(allPartNames[(int)(rand.NextDouble() * allPartNames.Length - 1)]);
                }
                Clear3DSelection();
                _controller.HighlightGeometryParts(selectedPartNames.ToArray());
                //
                Application.DoEvents();
            }
            catch
            { }
        }
        private void TestSelection2()
        {
            try
            {
                timerTest.Interval = 50;
                //
                Random rand = new Random();
                int x1 = _vtk.Width / 4 + rand.Next(_vtk.Width / 2);
                int y1 = _vtk.Height / 4 + rand.Next(_vtk.Height / 2);

                //x1 = 523;
                //y1 = 421;

                Clear3DSelection();
                _vtk.Pick(x1, y1, false, 0, 0);
                //
                Application.DoEvents();
            }
            catch
            { }
        }

        
    }
}
