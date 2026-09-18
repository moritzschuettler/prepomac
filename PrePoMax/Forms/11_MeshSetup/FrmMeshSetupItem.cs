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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using CaeMesh;
using System.Windows.Forms;


namespace PrePoMax.Forms
{
    class FrmMeshSetupItem : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _meshSetupItemNames;
        private string _meshSetupItemToEditName;
        private ViewMeshSetupItem _viewMeshSetupItem;
        private Controller _controller;
        private HashSet<ViewMeshSetupItem> _viewsChangedByUser;
        //
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        private ToolTip ttText;
        private Button btnPreview;


        // Callbacks                                                                                                                
        public Func<string[], MeshSetupItem, Task> PreviewEdgeMeshAsync;


        // Properties                                                                                                               
        public MeshSetupItem MeshSetupItem
        {
            get { return _viewMeshSetupItem != null ? _viewMeshSetupItem.GetBase() : null; }
            set
            {
                if (value is MeshingParameters mp)
                {
                    bool defaultSizeIsRelative = _controller.Settings.Meshing.MeshingParameters.RelativeSize;
                    //
                    bool advancedView = mp.AdvancedView;
                    if (_viewMeshSetupItem != null && _viewMeshSetupItem is ViewMeshingParameters vmp)
                        advancedView = vmp.AdvancedView;
                    //
                    _viewMeshSetupItem = new ViewMeshingParameters(mp.DeepClone(), defaultSizeIsRelative, advancedView);
                }
                else _viewMeshSetupItem = MeshSetupItemTypes.CreateView(_controller, value);
            }
        }


        // Constructors                                                                                                             
        public FrmMeshSetupItem(Controller controller)
            : base(1.72)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewMeshSetupItem = null;
            _viewsChangedByUser = new HashSet<ViewMeshSetupItem>();
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
            this.btnPreview = new System.Windows.Forms.Button();
            this.ttText = new System.Windows.Forms.ToolTip(this.components);
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(340, 101);
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(328, 73);
            // 
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(340, 310);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(328, 282);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(190, 436);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(271, 436);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(109, 436);
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
            // btnPreview
            // 
            this.btnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPreview.Image = global::PrePoMax.Properties.Resources.Show;
            this.btnPreview.Location = new System.Drawing.Point(75, 436);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(28, 23);
            this.btnPreview.TabIndex = 17;
            this.ttText.SetToolTip(this.btnPreview, "Preview");
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // FrmMeshSetupItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(364, 471);
            this.Controls.Add(this.btnPreview);
            this.MinimumSize = new System.Drawing.Size(380, 510);
            this.Name = "FrmMeshSetupItem";
            this.Text = "Edit Mesh Setup Item";
            this.VisibleChanged += new System.EventHandler(this.FrmMeshSetupItem_VisibleChanged);
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.gbType, 0);
            this.Controls.SetChildIndex(this.btnPreview, 0);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void FrmMeshSetupItem_VisibleChanged(object sender, EventArgs e)
        {
            _controller.Selection.LimitSelectionToFirstMesherType = Visible;
            //
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            if (MeshSetupItem is MeshingParameters mp)
            {
                mp.CopyFrom(GetDefaultMeshingParameters(MeshSetupItem.CreationIds));
                ((ViewMeshingParameters)_viewMeshSetupItem).AdvancedView = mp.AdvancedView;  // update advanced view
                _viewsChangedByUser.Remove(_viewMeshSetupItem);
            }
            else if (MeshSetupItem is ShellMmg smmg)
            {
                smmg.CopyFrom(GetDefaultShellMmg(MeshSetupItem.CreationIds));
                ((ViewShellMmg)_viewMeshSetupItem).UpdateVisibility();
                _viewsChangedByUser.Remove(_viewMeshSetupItem);
            }
            else
            {
                MeshSetupItem.Reset();
                if (_viewMeshSetupItem is ViewRevolveMesh vrm) vrm.UpdateVisibility();
                if (_viewMeshSetupItem is ViewSweepMesh vsw) SetSelectItem();
            }
            //
            _propertyItemChanged = true;
            //
            propertyGrid.Refresh();
        }
        async private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                Enabled = false;
                ItemSetDataEditor.SelectionForm.Enabled = false;
                //
                string[] partNames;
                if (MeshSetupItem is MeshingParameters || MeshSetupItem is ShellMmg ||
                    MeshSetupItem is TetrahedralNetgen || MeshSetupItem is TetrahedralGmsh ||
                    MeshSetupItem is TransfiniteMesh)
                {
                    partNames = _controller.DisplayedMesh.GetPartNamesFromPartIds(MeshSetupItem.CreationIds);
                }
                else if (MeshSetupItem is ShellGmsh || MeshSetupItem is ShellQuadWild ||
                         MeshSetupItem is ThickenShellMesh || MeshSetupItem is LocalMeshSize ||
                         MeshSetupItem is ExtrudeMesh || MeshSetupItem is SweepMesh || MeshSetupItem is RevolveMesh)
                {
                    partNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(MeshSetupItem.CreationIds);
                }
                else throw new NotSupportedException("MeshSetupItemTypeException");
                //
                if (partNames != null && partNames.Length > 0)
                {
                    HighlightMeshSetupItem(false);
                    //
                    await PreviewEdgeMeshAsync?.Invoke(partNames, MeshSetupItem);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                Enabled = true;
                ItemSetDataEditor.SelectionForm.Enabled = true;
            }
        }


        // Overrides                                                                                                                
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewMeshingParameters vmp) _viewMeshSetupItem = vmp;
                else if (itemTag is ViewLocalMeshSize vlms) _viewMeshSetupItem = vlms;
                else if (itemTag is ViewShellMmg vsmmg) _viewMeshSetupItem = vsmmg;
                else if (itemTag is ViewShellGmsh vsg) _viewMeshSetupItem = vsg;
                else if (itemTag is ViewShellQuadWild vsqw) _viewMeshSetupItem = vsqw;
                else if (itemTag is ViewThickenShellMesh vtsm) _viewMeshSetupItem = vtsm;
                else if (itemTag is ViewTetrahedralNetgen vtn) _viewMeshSetupItem = vtn;
                else if (itemTag is ViewTetrahedralGmsh vtg) _viewMeshSetupItem = vtg;
                else if (itemTag is ViewTransfiniteMesh vtm) _viewMeshSetupItem = vtm;
                else if (itemTag is ViewExtrudeMesh vem) _viewMeshSetupItem = vem;
                else if (itemTag is ViewSweepMesh vsm) _viewMeshSetupItem = vsm;
                else if (itemTag is ViewRevolveMesh vrm) _viewMeshSetupItem = vrm;
                else throw new NotImplementedException("MeshSetupItemTypeException");
                //
                ShowHideSelectionForm();
                //
                propertyGrid.SelectedObject = _viewMeshSetupItem;
                btnPreview.Enabled = true;
                //
                HighlightMeshSetupItem();
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (_viewMeshSetupItem is ViewMeshingParameters vmp &&
                property != nameof(vmp.Name) && property != nameof(vmp.AdvancedView) && property != nameof(vmp.Relative))
            {
                _viewsChangedByUser.Add(_viewMeshSetupItem);
            }
            else if (_viewMeshSetupItem is ViewShellMmg vsmmg &&
                property != nameof(vsmmg.Name) && property != nameof(vsmmg.Relative))
            {
                _viewsChangedByUser.Add(_viewMeshSetupItem);
            }
            else if (_viewMeshSetupItem is ViewLocalMeshSize vlms && property == nameof(vlms.Type))
            {
                SetSelectItem();
            }
            else if (_viewMeshSetupItem is ViewSweepMesh vsm && property == nameof(vsm.SourceType))
            {
                // A surface selection means nothing to an automatic sweep and a part selection nothing to a
                // manual one. The picker mode also lives in the selection that the highlight below restores,
                // so the selection is dropped before the picker is switched, or it would switch right back
                MeshSetupItem.CreationIds = new int[0];
                MeshSetupItem.CreationData = null;
                _controller.ClearSelectionHistory();
                ShowHideSelectionForm();
            }
            //
            HighlightMeshSetupItem();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewMeshSetupItem = (ViewMeshSetupItem)propertyGrid.SelectedObject;
            //
            if (_viewMeshSetupItem == null) throw new CaeException("No mesh setup item was selected.");
            // Check if the name exists
            CheckName(_meshSetupItemToEditName, MeshSetupItem.Name, _meshSetupItemNames, "mesh setup item");
            //
            if (MeshSetupItem.CreationIds == null || MeshSetupItem.CreationIds.Length == 0)
                throw new CaeException("The mesh setup item selection must contain at least one item.");
            //
            string error = _controller.IsMeshSetupItemProperlyDefined(MeshSetupItem);
            if (error != null) throw new CaeException(error);
            // When opened from regenerate
            if (this.Modal) { }
            // When opened normally
            else
            {
                // Create
                if (_meshSetupItemToEditName == null)
                {
                    _controller.AddMeshSetupItemCommand(MeshSetupItem);
                }
                // Replace
                else if (_propertyItemChanged)
                {
                    _controller.ReplaceMeshSetupItemCommand(_meshSetupItemToEditName, MeshSetupItem);
                }
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew)
            {
                DialogResult = DialogResult.OK;
                ItemSetDataEditor.SelectionForm.Hide();
            }
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string meshSetupItemToEditName)
        {
            DialogResult = DialogResult.None;
            //
            int dx = btnCancel.Left - btnOK.Right;
            if (meshSetupItemToEditName == null)
            {
                btnOkAddNew.Visible = true;
                btnPreview.Visible = true;
                btnPreview.Location = new System.Drawing.Point(btnOkAddNew.Left - dx - btnPreview.Width, btnOkAddNew.Top);
            }
            else
            {
                btnOkAddNew.Visible = false;
                btnPreview.Visible = true;
                btnPreview.Location = new System.Drawing.Point(btnOK.Left - dx - btnPreview.Width, btnOkAddNew.Top);
            }
            //
            _propertyItemChanged = false;
            _meshSetupItemNames = null;
            _meshSetupItemToEditName = null;
            _viewMeshSetupItem = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            _viewsChangedByUser.Clear();
            _controller.ClearAllSelection();
            btnPreview.Enabled = true;
            //
            _meshSetupItemNames = _controller.GetMeshSetupItemNames();
            _meshSetupItemToEditName = meshSetupItemToEditName;
            //
            if (_meshSetupItemNames == null)
                throw new CaeException("The mesh setup item names must be defined first.");
            //
            PopulateListOfMeshSetupItems();
            // Create new mesh setup item
            if (_meshSetupItemToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewMeshSetupItem = null;
            }
            // Edit existing mesh setup item
            else
            {
                MeshSetupItem = _controller.GetMeshSetupItem(_meshSetupItemToEditName);   // to clone
                _viewsChangedByUser.Add(_viewMeshSetupItem);   // the stored values are the user's
                // Check validity
                if (!MeshSetupItem.Valid)
                {
                    MeshSetupItem.CreationData = null;
                    MeshSetupItem.CreationIds = null;
                    MeshSetupItem.Valid = true;
                }
                int selectedId = MeshSetupItemTypes.GetIndex(IsTwoD(), _viewMeshSetupItem);
                //
                lvTypes.Items[selectedId].Tag = _viewMeshSetupItem;
                _preselectIndex = selectedId;
            }
            //
            ShowHideSelectionForm();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods                                                                                                                  
        // The Hausdorff distance is a length, so it follows the size of the selected parts the same
        // way the maximum and the minimum element size do
        private ShellMmg GetDefaultShellMmg(int[] ids)
        {
            ShellMmg shellMmg = (ShellMmg)MeshSetupItem;
            ShellMmg defaultShellMmg = null;
            //
            if (ids != null && ids.Length > 0)
            {
                string[] partNames = _controller.Model.Geometry.GetPartNamesFromPartIds(ids);
                defaultShellMmg = _controller.GetPartDefaultShellMmg(partNames);
            }
            if (defaultShellMmg == null) defaultShellMmg = new ShellMmg(shellMmg.Name);
            //
            defaultShellMmg.Name = shellMmg.Name;
            defaultShellMmg.RelativeSize = shellMmg.RelativeSize;
            defaultShellMmg.CreationIds = shellMmg.CreationIds;
            defaultShellMmg.CreationData = shellMmg.CreationData;
            //
            return defaultShellMmg;
        }
        private MeshingParameters GetDefaultMeshingParameters(int[] ids)
        {
            MeshingParameters meshingParameters = _controller.Settings.Meshing.MeshingParameters;
            //
            if (MeshSetupItem is MeshingParameters mp)
            {
                meshingParameters.Name = mp.Name;
                //
                if (ids != null && ids.Length > 0)
                {
                    string[] partNames = _controller.Model.Geometry.GetPartNamesFromPartIds(ids);
                    MeshingParameters defaultMeshingParameters = _controller.GetPartDefaultMeshingParameters(partNames);
                    //
                    if (defaultMeshingParameters != null)
                    {
                        defaultMeshingParameters.Name = mp.Name;
                        meshingParameters = defaultMeshingParameters;
                    }
                }
                // Copy relative
                meshingParameters.RelativeSize = mp.RelativeSize;
                // Copy creation data
                meshingParameters.CreationIds = mp.CreationIds;
                meshingParameters.CreationData = mp.CreationData;
            }
            return meshingParameters;
        }
        public void SetMeshSetupItem(MeshSetupItem meshSetupItem)
        {
            _meshSetupItemToEditName = meshSetupItem.Name;  // the OnApply checks this name
            //
            int selectedId = MeshSetupItemTypes.GetIndex(IsTwoD(), meshSetupItem);
            _viewMeshSetupItem = MeshSetupItemTypes.CreateView(_controller, meshSetupItem);
            _viewsChangedByUser.Add(_viewMeshSetupItem);   // the stored values are the user's
            //
            _viewMeshSetupItem.HideName();
            btnOkAddNew.Visible = false;
            btnPreview.Visible = false;
            //
            lvTypes.Items[selectedId].Tag = _viewMeshSetupItem;
            //
            PreselectListViewItem(selectedId);
            //
            ItemSetDataEditor.SelectionForm.Hide();
        }
        //
        private bool IsTwoD()
        {
            return _controller.Model.Properties.ModelSpace.IsTwoD();
        }
        private void PopulateListOfMeshSetupItems()
        {
            ListViewItem item;
            //
            foreach (var meshSetupItemType in MeshSetupItemTypes.GetAvailable(IsTwoD()))
            {
                item = new ListViewItem(meshSetupItemType.DisplayName);
                item.Tag = meshSetupItemType.CreateDefaultView(_controller,
                                                               GetMeshSetupItemName(meshSetupItemType.DefaultNameRoot));
                lvTypes.Items.Add(item);
            }
        }
        private string GetMeshSetupItemName(string name)
        {
            return _meshSetupItemNames.GetNextNumberedKey(name);
        }
        private void HighlightMeshSetupItem(bool highlightNodes = true)
        {
            try
            {
                // This calls selection which might change the view which calls clear
                // which calls SelectionChanged which changes the MeshSetupItem !!!
                if (Modal) return;
                //
                _controller.ClearSelectionHistory();
                //
                if (_viewMeshSetupItem == null) { }
                else
                {
                    if (MeshSetupItem.CreationData != null && MeshSetupItem.CreationIds != null &&
                        MeshSetupItem.CreationIds.Length > 0)
                    {
                        try
                        {
                            if (MeshSetupItem is ExtrudeMesh || MeshSetupItem is SweepMesh || MeshSetupItem is RevolveMesh)
                            {
                                _controller.IsMeshSetupItemProperlyDefined(MeshSetupItem);
                            }
                            //
                            _controller.HighlightMeshSetupItem(MeshSetupItem, highlightNodes);
                        }
                        catch { }
                        // Set previous selection to continue it
                        _controller.Selection = MeshSetupItem.CreationData.DeepCopy(true, false);
                    }
                }
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            // When opened from regenerate
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            // When opened normally
            else
            {
                if (MeshSetupItem != null)
                {
                    ItemSetDataEditor.SelectionForm.ItemSetData = new ItemSetData(MeshSetupItem.CreationIds);
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                }
                else ItemSetDataEditor.SelectionForm.Hide();
            }
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (MeshSetupItem != null)
            {
                if (MeshSetupItem is MeshingParameters) _controller.SetSelectItemToPart();
                // A number of elements is applied to every edge of a picked surface or part, so the picker
                // takes any geometry for both kinds of local size
                else if (MeshSetupItem is LocalMeshSize) _controller.SetSelectItemToGeometry();
                else if (MeshSetupItem is ShellMmg) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is ShellGmsh) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is ShellQuadWild) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is ThickenShellMesh) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is TetrahedralNetgen) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is TetrahedralGmsh) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is TransfiniteMesh) _controller.SetSelectItemToPart();
                else if (MeshSetupItem is ExtrudeMesh) _controller.SetSelectItemToGeometrySurface();
                else if (MeshSetupItem is SweepMesh sm)
                {
                    if (sm.SourceType == SweepSourceTypeEnum.Automatic) _controller.SetSelectItemToPart();
                    else _controller.SetSelectItemToGeometrySurface();
                }
                else if (MeshSetupItem is RevolveMesh) _controller.SetSelectItemToGeometrySurface();
                else throw new NotSupportedException("MeshSetupItemTypeException");
            }
            else _controller.SetSelectByToOff();
        }
        //
        int[] _prevIds = new int[0];
        public void SelectionChanged(int[] ids)
        {
            if (MeshSetupItem != null)
            {
                if (MeshSetupItem is MeshingParameters || MeshSetupItem is LocalMeshSize ||
                    MeshSetupItem is ShellMmg || MeshSetupItem is ShellGmsh || MeshSetupItem is ShellQuadWild ||
                    MeshSetupItem is ThickenShellMesh || MeshSetupItem is TetrahedralNetgen ||
                    MeshSetupItem is TetrahedralGmsh ||
                    MeshSetupItem is TransfiniteMesh || MeshSetupItem is ExtrudeMesh || MeshSetupItem is SweepMesh ||
                    MeshSetupItem is RevolveMesh)
                {
                    bool changedByUser = _viewsChangedByUser.Contains(_viewMeshSetupItem);
                    if (MeshSetupItem is MeshingParameters mp && !changedByUser)
                    {
                        mp.CopyFrom(GetDefaultMeshingParameters(ids));
                    }
                    else if (MeshSetupItem is ShellMmg smmg && !changedByUser)
                    {
                        smmg.CopyFrom(GetDefaultShellMmg(ids));
                    }
                    else if (MeshSetupItem is RevolveMesh rm)
                    {
                        if (_prevIds.Length == 0 && ids.Length == 0) rm.AngleDeg = -1;
                    }
                    //
                    MeshSetupItem.CreationIds = ids;
                    MeshSetupItem.CreationData = _controller.Selection.DeepCopy(true, false);
                    //
                    if (ids.Length != 0) HighlightMeshSetupItem(); // this will redraw the selection with correct backfaceCulling
                    // The highlight computes the revolve mesh properties - update
                    if (_viewMeshSetupItem is ViewMeshingParameters vmp) vmp.UpdateVisibility();
                    else if (_viewMeshSetupItem is ViewShellMmg vsmmg) vsmmg.UpdateVisibility();
                    else if (_viewMeshSetupItem is ViewRevolveMesh vrm) vrm.UpdateVisibility();
                    //
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                }
                else throw new NotSupportedException("MeshSetupItemTypeException");
            }
            _prevIds = ids.ToArray();
        }

        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightMeshSetupItem();
        }

        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            // All meshing parameters are geometry based
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            MeshSetupItem meshSetupItem = MeshSetupItem;
            //
            if (meshSetupItem.CreationData != null && IsSelectionGeometryBased())
                return meshSetupItem.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
