using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using System.Windows.Forms;
using System.Drawing;
using UserControls;
using System.Xml.Linq;

namespace PrePoMax.Forms
{
    class FrmReferencePoint : FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlightSymbol
    {
        // Variables                                                                                                                
        private HashSet<string> _referencePointNames;
        private string _referencePointToEditName;
        private string[] _nodeSetNames;
        private string[] _surfaceNames;
        private ViewFeReferencePoint _viewReferencePoint;
        private Controller _controller;
        //
        private System.ComponentModel.IContainer components;
        private ContextMenuStrip cmsPropertyGrid;
        private ToolStripMenuItem tsmiResetAll;


        // Properties                                                                                                               
        public FeReferencePoint ReferencePoint
        {
            get { return _viewReferencePoint.GetBase(); }
            set { _viewReferencePoint = new ViewFeReferencePoint(value.DeepClone()); }
        }
       

        // Constructors                                                                                                             
        public FrmReferencePoint(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewReferencePoint = null;
            _referencePointNames = new HashSet<string>();
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
            this.gbProperties.SuspendLayout();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(118, 26);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(117, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmReferencePoint
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmReferencePoint";
            this.Text = "Edit Reference Point";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewReferencePoint.CreatedFrom))
            {
                SetSelectItem();
                //
                UpdateReferencePoint(ReferencePoint);
            }
            else if (property == nameof(_viewReferencePoint.RegionType) || property == nameof(_viewReferencePoint.NodeSetName) ||
                     property == nameof(_viewReferencePoint.SurfaceName))
            {
                UpdateReferencePoint(ReferencePoint);
            }
            //
            HighlightReferencePoint();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewReferencePoint = (ViewFeReferencePoint)propertyGrid.SelectedObject;
            // Check namem
            CheckName(_referencePointToEditName, _viewReferencePoint.Name, _referencePointNames, "reference point");
            //
            FeReferencePoint rp = ReferencePoint;
            // Check selection
            if ((rp.CreatedFrom == FeReferencePointCreatedFrom.OnPoint &&
                 (rp.CreationIds == null || rp.CreationIds.Length != 1)) ||
                (rp.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints &&
                 (rp.CreationIds == null || rp.CreationIds.Length != 2)) ||
                (rp.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter &&
                 (rp.CreationIds == null || rp.CreationIds.Length != 3)))
                throw new CaeException("The selection of the reference point is not complete.");
            // Check region
            if (rp.CreatedFrom == FeReferencePointCreatedFrom.CenterOfGravity ||
                rp.CreatedFrom == FeReferencePointCreatedFrom.BoundingBoxCenter)
            {
                if (rp.RegionType == RegionTypeEnum.NodeSetName && !_nodeSetNames.Contains(rp.RegionName))
                    throw new CaeException("The selected node set does not exist.");
                else if (rp.RegionType == RegionTypeEnum.SurfaceName && !_surfaceNames.Contains(rp.RegionName))
                    throw new CaeException("The selected surface does not exist.");
            }
            // Check equations
            _viewReferencePoint.GetBase().CheckEquations();
            // Create
            if (_referencePointToEditName == null)
            {
                AddReferencePointCommand(rp);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                ReplaceReferencePointCommand(_referencePointToEditName, rp);
                //
                _referencePointToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the reference point from internal to show it
            else
            {
                ReferencePointInternal(false);
            }
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Reset the maximum number of selected items
            _controller.Selection.MaxNumberOfItemIds = -1;
            // Convert the reference point from internal to show it
            ReferencePointInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string referencePointToEditName)
        {
            this.btnOkAddNew.Visible = referencePointToEditName == null;
            //
            _propertyItemChanged = false;
            _referencePointNames.Clear();
            _referencePointToEditName = null;
            _viewReferencePoint = null;
            //
            _referencePointNames.UnionWith(GetReferencePointNames());
            _referencePointToEditName = referencePointToEditName;
            //
            _nodeSetNames = GetNodeSetNames();
            _surfaceNames = GetSurfaceNames();
            // Create new reference point
            if (_referencePointToEditName == null)
            {
                if (_controller.Model.Properties.ModelSpace.IsTwoD())
                    ReferencePoint = new FeReferencePoint(GetReferencePointName(), 0, 0);
                else  ReferencePoint = new FeReferencePoint(GetReferencePointName(), 0, 0, 0);
                //
                ReferencePoint.Color = _controller.Settings.Pre.ConstraintSymbolColor;
            }
            // Edit existing reference point
            else
            {
                ReferencePoint = GetReferencePoint(_referencePointToEditName); // to clone
                // Convert the reference point to internal to hide it
                ReferencePointInternal(true);
                // Check for deleted regions
                if (ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.BoundingBoxCenter ||
                    ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.CenterOfGravity)
                {
                    ViewFeReferencePoint vrp = _viewReferencePoint; // shorten
                    if (vrp.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref _nodeSetNames, vrp.NodeSetName, s => { vrp.NodeSetName = s; });
                    else if (vrp.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref _surfaceNames, vrp.SurfaceName, s => { vrp.SurfaceName = s; });
                    else throw new NotSupportedException();
                }
                // CheckMissingValue changes _propertyItemChanged -> update coordinates
                if (_propertyItemChanged)
                {
                    UpdateReferencePoint(ReferencePoint);   // to clone
                }
            }
            //
            _viewReferencePoint.PopulateDropDownLists(_nodeSetNames, _surfaceNames);
            //
            propertyGrid.SelectedObject = _viewReferencePoint;
            propertyGrid.Select();
            //
            SetSelectItem();
            //
            HighlightReferencePoint();
            //
            return true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _viewReferencePoint.GetBase().Reset();
            _propertyItemChanged = true;
            propertyGrid.Refresh();
            HighlightReferencePoint();
        }

        // Methods                                                                                                                  
        public void SelectionChanged(int[] ids)
        {
            FeMesh mesh = _controller.DisplayedMesh;
            //
            if (ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.OnPoint ||
                ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
            {
                ReferencePoint.CreationIds = ids;
                ReferencePoint.CreationData = _controller.Selection.DeepClone();
                //
                mesh.UpdateReferencePoint(ReferencePoint);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
            else
            {
                ReferencePoint.CreationIds = null;
                ReferencePoint.CreationData = null;
            }
            //
            HighlightReferencePoint();
        }
        private string GetReferencePointName()
        {
            return _referencePointNames.GetNextNumberedKey("Reference_Point");
        }
        private void HighlightReferencePoint()
        {
            try
            {
                _controller.ClearSelectionHistory();
                // Draw selection nodes
                FeReferencePoint rp = ReferencePoint;
                if (rp != null && (rp.CreatedFrom == FeReferencePointCreatedFrom.OnPoint ||
                                   rp.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                                   rp.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter))
                {
                    SetSelectItem();
                    //
                    if (rp.CreationData != null)
                    {
                        _controller.Selection = rp.CreationData.DeepClone();
                        _controller.HighlightSelection(false, true, true);
                    }
                }
                // Draw node set
                if (rp != null && rp.RegionType == RegionTypeEnum.NodeSetName)
                    _controller.HighlightNodeSet(rp.RegionName, true);
                // Draw surface
                else if (rp != null && rp.RegionType == RegionTypeEnum.SurfaceName)
                    _controller.HighlightSurface(rp.RegionName, true);
                // Draw reference point
                _controller.HighlightReferencePoint(rp);
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (ReferencePoint != null && ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.OnPoint ||
                ReferencePoint != null && ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                ReferencePoint != null && ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
        }
        private void SetSelectItem()
        {
            ShowHideSelectionForm();
            FeReferencePoint rp = ReferencePoint;
            //
            if (rp != null && rp.CreatedFrom == FeReferencePointCreatedFrom.OnPoint ||
                rp != null && rp.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
                rp != null && rp.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
                _controller.SetSelectItemToNode();
            else _controller.SetSelectByToOff();
            // Set the number of selected ids
            SetNumberOfSelectionItemIds(rp, _controller.Selection);
        }
        private void SetNumberOfSelectionItemIds(FeReferencePoint referencePoint, Selection selection)
        {
            if (selection == null) return;
            if (referencePoint == null) selection.MaxNumberOfItemIds = -1;
            //
            if (referencePoint.CreatedFrom == FeReferencePointCreatedFrom.OnPoint)
                selection.MaxNumberOfItemIds = 1;
            else if (referencePoint.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints)
                selection.MaxNumberOfItemIds = 2;
            else if (referencePoint.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
                selection.MaxNumberOfItemIds = 3;
            else selection.MaxNumberOfItemIds = -1;
        }
        private void ResetNumberOfSelectionItemIds(FeReferencePoint referencePoint)
        {
            if (referencePoint.CreationData != null) referencePoint.CreationData.MaxNumberOfItemIds = -1;
        }
        private void ReferencePointInternal(bool toInternal)
        {
            // Convert the reference point from/to internal to hide/show it
            if (_referencePointToEditName != null)
            {
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    _controller.GetModelReferencePoint(_referencePointToEditName).Internal = toInternal;
                    _controller.FeModelUpdate(UpdateType.RedrawSymbols);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    _controller.GetResultReferencePoint(_referencePointToEditName).Internal = toInternal;
                    _controller.FeResultsUpdate(UpdateType.RedrawSymbols);
                }
                else throw new NotSupportedException();
            }
        }
        //
        private string[] GetReferencePointNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetAllMeshEntityNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultReferencePointNames();
            else throw new NotSupportedException();
        }
        private string[] GetNodeSetNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetUserNodeSetNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultUserNodeSetNames();
            else throw new NotSupportedException();
        }
        private string[] GetSurfaceNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetUserSurfaceNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultUserSurfaceNames();
            else throw new NotSupportedException();
        }
        private void UpdateReferencePoint(FeReferencePoint referencePoint)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.UpdateModelReferencePoint(referencePoint);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.UpdateResultReferencePoint(referencePoint);
            else throw new NotSupportedException();
        }
        private void AddReferencePointCommand(FeReferencePoint referencePoint)
        {
            if (referencePoint != null)
            {
                // Reset the max number of items for regenerate - the next selection should not be limited
                ResetNumberOfSelectionItemIds(referencePoint);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                    _controller.AddModelReferencePointCommand(referencePoint);
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    _controller.AddResultReferencePointCommand(referencePoint);
                else throw new NotSupportedException();
            }
        }
        private FeReferencePoint GetReferencePoint(string referencePointToEditName)
        {
            FeReferencePoint referencePoint;
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                referencePoint = _controller.GetModelReferencePoint(referencePointToEditName).DeepClone(); // copy
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                referencePoint = _controller.GetResultReferencePoint(referencePointToEditName).DeepClone(); // copy
            else throw new NotSupportedException();
            // Set the number of selected ids
            SetNumberOfSelectionItemIds(referencePoint, referencePoint.CreationData);
            //
            return referencePoint;
        }
        private void ReplaceReferencePointCommand(string referencePointToEditName, FeReferencePoint referencePoint)
        {
            if (referencePoint != null)
            {
                // Reset the max number of items for regenerate - the next selection should not be limited
                ResetNumberOfSelectionItemIds(referencePoint);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                    _controller.ReplaceModelReferencePointCommand(referencePointToEditName, referencePoint);
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    _controller.ReplaceResultReferencePointCommand(referencePointToEditName, referencePoint);
                else throw new NotSupportedException();
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightReferencePoint();
        }
        //
        public void ClearCurrntSelectionAndHighlight()
        {
            ReferencePoint.CreationIds = null;
            ReferencePoint.CreationData = null;
            //
            Highlight();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            FeReferencePoint rp = ReferencePoint;
            //
            if (rp != null && rp.CreationData != null) return rp.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            FeReferencePoint rp = ReferencePoint;
            //
            if (rp != null && rp.CreationData != null && IsSelectionGeometryBased())
                return rp.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
