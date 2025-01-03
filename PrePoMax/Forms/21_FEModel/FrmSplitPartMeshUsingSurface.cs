using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using UserControls;
using PrePoMax.Commands;

namespace PrePoMax.Forms
{
    class FrmSplitPartMeshUsingSurface : FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        bool _highlightEnabled;
        private ViewSplitPartMeshData _viewSplitPartMeshData;
        private SplitPartMeshData _prevSplitPartMeshData;
        private string _prevSelectionFormProperty;
        private Button btnPreview;
        private Controller _controller;


        // Properties                                                                                                               
        public SplitPartMeshData SplitPartMeshData
        {
            get { return _viewSplitPartMeshData.GetBase(); }
            set { _viewSplitPartMeshData = new ViewSplitPartMeshData(value.DeepClone()); }
        }


        // Callbacks                                                                                                                
        public Action<SplitPartMeshData> SplitPartMeshUsingSurface;


        // Constructors                                                                                                             
        public FrmSplitPartMeshUsingSurface(Controller controller)
            : base(1.6)
        {
            InitializeComponent();
            //
            this.btnOkAddNew.Visible = false;
            this.btnPreview.Left = this.btnOkAddNew.Right - this.btnPreview.Width;
            //
            _controller = controller;
            _highlightEnabled = true;
            _viewSplitPartMeshData = null;
            _prevSplitPartMeshData = null;
            //
            SelectionClear = _controller.Selection.Clear;
        }
        private void InitializeComponent()
        {
            this.btnPreview = new System.Windows.Forms.Button();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnPreview
            // 
            this.btnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPreview.Image = global::PrePoMax.Properties.Resources.Show;
            this.btnPreview.Location = new System.Drawing.Point(45, 376);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(28, 23);
            this.btnPreview.TabIndex = 18;
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // FrmSplitPartMeshUsingSurface
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.btnPreview);
            this.Name = "FrmSplitPartMeshUsingSurface";
            this.Text = "Split Part Mesh Using Surface";
            this.VisibleChanged += new System.EventHandler(this.FrmSplitPartMeshUsingSurface_VisibleChanged);
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.btnPreview, 0);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void FrmSplitPartMeshUsingSurface_VisibleChanged(object sender, EventArgs e)
        {
            // Limit selection to the first selected part
            _controller.Selection.LimitSelectionToFirstPart = false;
            btnPreview.Enabled = true;
        }
        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                HighlightSplit();
                //
                btnPreview.Enabled = false;
                //
                CheckSplitPartMeshData(true, out _);
                //
                Hide();
                //
                _controller.PreviewSplitPartMeshUsingSurface(SplitPartMeshData);
            }
            catch (Exception ex)
            {
                btnPreview.Enabled = true;
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                btnPreview.Enabled = true;
            }
        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            //if (propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            ShowHideSelectionForm();
            //
            HighlightSplit();
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            //string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            ////
            //if (property == nameof(_viewSplitPartMeshData.MasterRegionType))
            //{
            //    ShowHideSelectionForm();
            //    //
            //    HighlightSplit();
            //}
            //else if (property == nameof(_viewSplitPartMeshData.BasePartPartName))
            //{
            //    HighlightSplit();
            //}
            //else if (property == nameof(_viewSplitPartMeshData.SlaveRegionType))
            //{
            //    ShowHideSelectionForm();
            //    //
            //    HighlightSplit();
            //}
            //else if (property == nameof(_viewSplitPartMeshData.SplitterSurfaceSurfaceName))
            //{
            //    HighlightSplit();
            //}
            ////
            //base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _highlightEnabled = false;
            //
            CheckSplitPartMeshData(true, out _);
            //
            if (SplitPartMeshUsingSurface != null) SplitPartMeshUsingSurface(SplitPartMeshData);
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (onOkAddNew) _prevSplitPartMeshData = SplitPartMeshData.DeepClone();
            else ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            _prevSplitPartMeshData = null;
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            
            //
            _highlightEnabled = true;
            _propertyItemChanged = false;
            _viewSplitPartMeshData = null;
            propertyGrid.SelectedObject = null;
            //
            string[] partNames = _controller.GetModelPartNames();
            string[] surfaceNames = _controller.GetUserSurfaceNames();
            // Create new
            if (_prevSplitPartMeshData == null)
            {
                SplitPartMeshData = new SplitPartMeshData(RegionTypeEnum.Selection, "", RegionTypeEnum.Selection, "");
                //
                SplitPartMeshData.BasePartCreationData = new Selection();
                SplitPartMeshData.BasePartCreationData.SelectItem = vtkSelectItem.Part;
                //
                SplitPartMeshData.SplitterSurfaceCreationData = new Selection();
                SplitPartMeshData.SplitterSurfaceCreationData.SelectItem = vtkSelectItem.Surface;
            }
            else SplitPartMeshData = _prevSplitPartMeshData;
            //
            _controller.Selection.Clear();                
            //
            _viewSplitPartMeshData.PopulateDropDownLists(partNames, surfaceNames);
            //
            propertyGrid.SelectedObject = _viewSplitPartMeshData;
            propertyGrid.Focus();
            // Get start point grid item and select it
            GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                          item.PropertyDescriptor != null &&
                          item.PropertyDescriptor.Name == nameof(_viewSplitPartMeshData.MasterRegionType));
            gi.Select();
            //
            ShowHideSelectionForm();
            //
            HighlightSplit();
            //
            return true;
        }


        // Methods                                                                                                                  
        private void CheckSplitPartMeshData(bool throwExceptions, out BasePart basePart)
        {
            basePart = null;
            //
            try
            {
                if (SplitPartMeshData == null) { throw new NotSupportedException(); }
                else if (SplitPartMeshData is SplitPartMeshData sp)
                {
                    CheckBasePartMeshData(throwExceptions, out basePart);
                    // Splitter surface
                    if (sp.SlaveRegionType == RegionTypeEnum.Selection &&
                        (sp.SplitterSurfaceCreationIds == null || sp.SplitterSurfaceCreationIds.Length == 0))
                        throw new CaeException("The splitter surface region must contain at least one item.");
                }
            }
            catch (Exception ex)
            {
                if (throwExceptions) throw ex;
            }
        }
        private void CheckBasePartMeshData(bool throwExceptions, out BasePart basePart)
        {
            basePart = null;
            //
            try
            {
                if (SplitPartMeshData == null) { throw new NotSupportedException(); }
                else if (SplitPartMeshData is SplitPartMeshData sp)
                {
                    // Base part
                    if (sp.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (sp.BasePartCreationIds == null || sp.BasePartCreationIds.Length == 0)
                            throw new CaeException("The base part region must contain at least one item.");
                        //
                        //else if (sp.BasePartCreationIds.Count() != 1)
                        //    throw new CaeException("The base part region can only contain a single part.");
                        //
                        foreach (var geometryId in sp.BasePartCreationIds)
                        {
                            basePart = _controller.Model.Mesh.GetPartFromGeometryId(geometryId);
                            if (basePart.PartType != PartType.Solid)
                                throw new CaeException("The base part region can only contain a solid part.");
                        }
                    }
                    else if (sp.BasePartRegionType == RegionTypeEnum.PartName)
                    {
                        basePart = _controller.Model.Mesh.Parts[sp.BasePartRegionName];
                    }
                    // Tetrahedrons only
                    HashSet<Type> tetraTypes = new HashSet<Type>();
                    tetraTypes.Add(typeof(LinearTetraElement));
                    tetraTypes.Add(typeof(ParabolicTetraElement));
                    if (((MeshPart)basePart).ElementTypes.Except(tetraTypes).Count() != 0)
                        throw new CaeException("The base part region can only contain tetrahedral mesh elements.");
                }
            }
            catch (Exception ex)
            {
                if (throwExceptions) throw ex;
            }
        }
        private void HighlightSplit()
        {
            try
            {
                if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                //
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                _controller.ClearSelectionHistory();
                //
                if (SplitPartMeshData == null) { }
                else if (SplitPartMeshData is SplitPartMeshData sp)
                {
                    if (property == nameof(ViewSplitPartMeshData.MasterRegionType))
                    {
                        HighlightRegion(sp.BasePartRegionType, sp.BasePartRegionName, sp.BasePartCreationData, true, false);
                    }
                    else if (property == nameof(ViewSplitPartMeshData.SlaveRegionType))
                    {
                        HighlightRegion(sp.SplitterSurfaceRegionType, sp.SplitterSurfaceRegionName,
                                        sp.SplitterSurfaceCreationData, true, true);
                    }
                    else
                    {
                        HighlightRegion(sp.BasePartRegionType, sp.BasePartRegionName, sp.BasePartCreationData, true, false);
                        HighlightRegion(sp.SplitterSurfaceRegionType, sp.SplitterSurfaceRegionName,
                                        sp.SplitterSurfaceCreationData, false, true);
                    }
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void HighlightRegion(RegionTypeEnum regionType, string regionName, Selection creationData,
                                    bool clear, bool useSecondaryHighlightColor)
        {
            if (regionType == RegionTypeEnum.PartName)
                _controller.Highlight3DObjects(new object[] { regionName });
            else if (regionType == RegionTypeEnum.SurfaceName)
                _controller.HighlightSurface(regionName, useSecondaryHighlightColor);
            else if (regionType == RegionTypeEnum.Selection)
            {
                SetSelectItem();
                //
                if (creationData != null)
                {
                    _controller.Selection = creationData.DeepClone();
                    _controller.HighlightSelection(clear, true, useSecondaryHighlightColor);
                }
            }
        }
        //
        private void ShowHideSelectionForm()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property != _prevSelectionFormProperty) ItemSetDataEditor.SelectionForm.ResetSelection(false);
            _prevSelectionFormProperty = property;
            //
            if (SplitPartMeshData != null && SplitPartMeshData is SplitPartMeshData sp)
            {
                if (property == nameof(ViewSplitPartMeshData.MasterRegionType) &&
                    sp.BasePartRegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (property == nameof(ViewSplitPartMeshData.SlaveRegionType) &&
                    sp.SplitterSurfaceRegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else ItemSetDataEditor.SelectionForm.Hide();
            }
            else ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (SplitPartMeshData == null) { }
            else if (SplitPartMeshData is SplitPartMeshData sp)
            {
                if (sp.BasePartRegionType == RegionTypeEnum.Selection &&
                    property == nameof(ViewSplitPartMeshData.MasterRegionType))
                    _controller.SetSelectItemToPart();
                else if (sp.SplitterSurfaceRegionType == RegionTypeEnum.Selection &&
                    property == nameof(ViewSplitPartMeshData.SlaveRegionType))
                    _controller.SetSelectItemToSurface();
                else
                    _controller.SetSelectByToOff();
            }
        }
        public void SelectionChanged(int[] ids)
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            bool changed = false;
            if (SplitPartMeshData != null && SplitPartMeshData is SplitPartMeshData sp)
            {
                if (property == nameof(ViewSplitPartMeshData.MasterRegionType) &&
                    sp.BasePartRegionType == RegionTypeEnum.Selection)
                {
                    sp.BasePartCreationIds = ids;
                    sp.BasePartCreationData = _controller.Selection.DeepClone();
                    //
                    bool sizeSet = false;
                    if (ids != null && ids.Length > 0)
                    {
                        // Meshing parameters
                        BasePart basePart;
                        CheckBasePartMeshData(false, out basePart);
                        if (basePart != null)
                        {
                            MeshingParameters meshingParameters = _controller.GetPartDefaultMeshingParameters(basePart.Name);
                            SplitPartMeshData.MaxH = meshingParameters.MaxH;
                            SplitPartMeshData.MinH = meshingParameters.MinH;
                            SplitPartMeshData.Hausdorff = meshingParameters.Hausdorff;
                            sizeSet = true;
                        }
                    }
                    if (!sizeSet)
                    {
                        SplitPartMeshData.MaxH = 1000;
                        SplitPartMeshData.MinH = 0;
                        SplitPartMeshData.Hausdorff = 0.1;
                    }
                    //
                    changed = true;
                }
                else if (property == nameof(ViewSplitPartMeshData.SlaveRegionType) &&
                         sp.SplitterSurfaceRegionType == RegionTypeEnum.Selection)
                {
                    sp.SplitterSurfaceCreationIds = ids;
                    sp.SplitterSurfaceCreationData = _controller.Selection.DeepClone();
                    changed = true;
                }
                //
                if (changed)
                {
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                }
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if(_highlightEnabled && !_closing) HighlightSplit();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return true;
            SplitPartMeshData splitPartMeshData = SplitPartMeshData;
            //
            if (splitPartMeshData != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (splitPartMeshData is SplitPartMeshData sp)
                {
                    if (property == nameof(ViewSplitPartMeshData.MasterRegionType) &&
                        sp.BasePartRegionType == RegionTypeEnum.Selection)
                    {
                        if (sp.BasePartCreationData != null) return sp.BasePartCreationData.IsGeometryBased();
                        else return true;
                    }
                    else if (property == nameof(ViewSplitPartMeshData.SlaveRegionType) &&
                             sp.SplitterSurfaceRegionType == RegionTypeEnum.Selection)
                    {
                        if (sp.SplitterSurfaceCreationData != null) return sp.SplitterSurfaceCreationData.IsGeometryBased();
                        else return true;
                    }
                }
                else throw new NotSupportedException();
            }
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null)
                return defaultMode;
            SplitPartMeshData splitPartMeshData = SplitPartMeshData;
            //
            if (splitPartMeshData != null && IsSelectionGeometryBased())
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (splitPartMeshData is SplitPartMeshData sp)
                {
                    if (property == nameof(ViewSplitPartMeshData.MasterRegionType) &&
                        sp.BasePartRegionType == RegionTypeEnum.Selection)
                    {
                        if (sp.BasePartCreationData != null) return sp.BasePartCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                    else if (property == nameof(ViewSplitPartMeshData.SlaveRegionType) &&
                             sp.SplitterSurfaceRegionType == RegionTypeEnum.Selection)
                    {
                        if (sp.SplitterSurfaceCreationData != null) return sp.SplitterSurfaceCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                }
                else throw new NotSupportedException();
            }
            return defaultMode;
        }
    }


}
