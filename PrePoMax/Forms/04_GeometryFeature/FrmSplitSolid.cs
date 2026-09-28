// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    class FrmSplitSolid : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewGeometryFeature _viewGeometryFeature;
        protected Controller _controller;
        private FeatureSelectionTargetEnum _selectionTarget;
        // OK - New rebuilds the form, and the operation that was just used comes back selected
        private int _repeatTypeIndex;
        private bool _suppressAnnotateMeshTypesChanged;
        // The color annotation the view had before the dialog opened, which it gets back when the dialog closes
        private AnnotateWithColorEnum? _annotateWithColorBeforeForm;
        // The split redraws the view while it replaces the parts, and the picks still name the old ones
        private bool _splitting;
        //
        private System.ComponentModel.IContainer components;
        private ToolTip ttText;
        private CheckBox cbAnnotateMeshTypes;
        private Button btnPreview;
        private Button btnUndo;


        // Properties
        public GeometryFeature GeometryFeature
        {
            get { return _viewGeometryFeature != null ? _viewGeometryFeature.GetBase() : null; }
        }
        /// <summary>
        /// What the region of this splitting tool is picked on, which is the only thing that separates
        /// the two tools and decides which operations they offer.
        /// </summary>
        protected virtual SplitTypeEnum SplitType { get { return SplitTypeEnum.Solid; } }
        private bool RegionPicksParts { get { return SplitType == SplitTypeEnum.Solid; } }


        // Constructors
        // The designer opens a form derived from this one on an instance of this one, which it can only
        // create without arguments
        public FrmSplitSolid()
            : this(null)
        {
        }
        public FrmSplitSolid(Controller controller)
            : base(1.72)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewGeometryFeature = null;
            _selectionTarget = FeatureSelectionTargetEnum.Region;
            _repeatTypeIndex = -1;
            _suppressAnnotateMeshTypesChanged = false;
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cbAnnotateMeshTypes = new System.Windows.Forms.CheckBox();
            this.btnPreview = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.ttText = new System.Windows.Forms.ToolTip(this.components);
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(350, 108);
            this.gbType.Text = "Operation";
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(338, 73);
            // 
            // gbProperties
            // 
            this.gbProperties.Location = new System.Drawing.Point(12, 126);
            this.gbProperties.Size = new System.Drawing.Size(350, 304);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Location = new System.Drawing.Point(6, 16);
            this.propertyGrid.Size = new System.Drawing.Size(338, 282);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(200, 436);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(281, 436);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(119, 436);
            // 
            // cbAnnotateMeshTypes
            // 
            this.cbAnnotateMeshTypes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cbAnnotateMeshTypes.Appearance = System.Windows.Forms.Appearance.Button;
            this.cbAnnotateMeshTypes.Image = global::PrePoMax.Properties.Resources.Color_palette;
            this.cbAnnotateMeshTypes.Location = new System.Drawing.Point(85, 436);
            this.cbAnnotateMeshTypes.Name = "cbAnnotateMeshTypes";
            this.cbAnnotateMeshTypes.Size = new System.Drawing.Size(28, 23);
            this.cbAnnotateMeshTypes.TabIndex = 17;
            this.ttText.SetToolTip(this.cbAnnotateMeshTypes, "Color the parts by the mesh type they are meshed with");
            this.cbAnnotateMeshTypes.UseVisualStyleBackColor = true;
            this.cbAnnotateMeshTypes.CheckedChanged += new System.EventHandler(this.cbAnnotateMeshTypes_CheckedChanged);
            // 
            // btnPreview
            // 
            this.btnPreview.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPreview.Image = global::PrePoMax.Properties.Resources.Show;
            this.btnPreview.Location = new System.Drawing.Point(51, 436);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new System.Drawing.Size(28, 23);
            this.btnPreview.TabIndex = 18;
            this.ttText.SetToolTip(this.btnPreview, "Preview the edges the split makes");
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new System.EventHandler(this.btnPreview_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUndo.Enabled = false;
            this.btnUndo.Image = global::PrePoMax.Properties.Resources.Undo;
            this.btnUndo.Location = new System.Drawing.Point(17, 436);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(28, 23);
            this.btnUndo.TabIndex = 19;
            this.ttText.SetToolTip(this.btnUndo, "Undo the last split made while this dialog is open");
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // FrmSplitSolid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(374, 471);
            this.Controls.Add(this.btnUndo);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.cbAnnotateMeshTypes);
            this.MinimumSize = new System.Drawing.Size(390, 510);
            this.Name = "FrmSplitSolid";
            this.Text = "Split Solid";
            this.Activated += new System.EventHandler(this.FrmSplitSolid_Activated);
            this.VisibleChanged += new System.EventHandler(this.FrmSplitSolid_VisibleChanged);
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.gbType, 0);
            this.Controls.SetChildIndex(this.cbAnnotateMeshTypes, 0);
            this.Controls.SetChildIndex(this.btnPreview, 0);
            this.Controls.SetChildIndex(this.btnUndo, 0);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers
        private void FrmSplitSolid_VisibleChanged(object sender, EventArgs e)
        {
            // Only the designer makes the form without a controller, and there is no model to show it on
            if (_controller == null) return;
            //
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else
            {
                _controller.ClearGeometryFeatureSymbols();
                _controller.ClearAllSelection();
            }
        }
        private void FrmSplitSolid_Activated(object sender, EventArgs e)
        {
            if (_controller == null) return;
            // The color annotation can also be changed from the main window while this form is open
            UpdateAnnotateMeshTypesButton();
        }
        private void cbAnnotateMeshTypes_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressAnnotateMeshTypesChanged) return;
            //
            _controller.SetAnnotateWithColor(cbAnnotateMeshTypes.Checked ? AnnotateWithColorEnum.MeshTypes :
                                                                          AnnotateWithColorEnum.None);
            // Switching the annotation redraws everything, which takes the tool symbols off the screen
            // and draws the parts outside the region with their own color again, so what the dialog
            // had drawn is drawn once more
            HighlightGeometryFeature();
        }
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                GeometryFeature geometryFeature = GeometryFeature;
                ThrowIfGeometryFeatureCannotSplit(geometryFeature);
                // The symbols are drawn again, which takes an earlier preview off the screen
                HighlightGeometryFeature();
                //
                btnPreview.Enabled = false;
                await _controller.PreviewSplitGeometryAsync(geometryFeature.DeepClone());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                btnPreview.Enabled = true;
            }
        }
        private void btnUndo_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.UndoSplitGeometry();
                HighlightGeometryFeature();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                UpdateUndoButton();
            }
        }


        // Overrides
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                ViewGeometryFeature previousView = _viewGeometryFeature;
                _viewGeometryFeature = (ViewGeometryFeature)lvTypes.SelectedItems[0].Tag;
                // The region is the same question for every operation, so the parts already picked
                // stay picked when the operation changes; only the splitter geometry is operation specific
                if (previousView != null) _viewGeometryFeature.GetBase().CopyRegionFrom(previousView.GetBase());
                //
                propertyGrid.SelectedObject = _viewGeometryFeature;
                // Picking continues where the previous operation left off
                SetSelectionTarget(GeometryFeature.CreationIds == null || GeometryFeature.CreationIds.Length == 0 ?
                                   FeatureSelectionTargetEnum.Region : FeatureSelectionTargetEnum.Splitter);
                // The armed target can be the one that was already armed, and then arming it again does
                // nothing - but the operation has changed, and how many items it takes changed with it
                SetSelectItem();
                //
                HighlightGeometryFeature();
            }
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            //
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            // Selecting the row arms its picker - there is no second click on an editor button
            if (property == ViewGeometryFeature.RegionSelectionName)
                SetSelectionTarget(FeatureSelectionTargetEnum.Region);
            else if (property == ViewGeometryFeature.SplitterSelectionName)
                SetSelectionTarget(FeatureSelectionTargetEnum.Splitter);
            else if (property == ViewGeometryFeature.DirectionSelectionName)
                SetSelectionTarget(FeatureSelectionTargetEnum.Direction);
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            _viewGeometryFeature.UpdateVisibility();
            //
            HighlightGeometryFeature();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override async Task OnApplyAsync(bool onOkAddNew)
        {
            _viewGeometryFeature = (ViewGeometryFeature)propertyGrid.SelectedObject;
            //
            GeometryFeature geometryFeature = GeometryFeature;
            ThrowIfGeometryFeatureCannotSplit(geometryFeature);
            // The split runs while the application keeps answering, and the dialog is held until it ends,
            // since its picks name the geometry the split is replacing
            try
            {
                _splitting = true;
                Enabled = false;
                await _controller.SplitGeometryCommandAsync(geometryFeature.DeepClone());
            }
            finally
            {
                _splitting = false;
                Enabled = true;
                UpdateUndoButton();
            }
            // The split has changed the geometry, so the picked ids and everything read from them are
            // stale - a part that was cut has been replaced by a compound of the pieces, and a part whose
            // surfaces were divided has been renumbered, so the ids of the region name geometry that is
            // gone. Resolving them again would look them up in a model that no longer holds them.
            // Clearing the splitter is also what takes the tool symbol off the screen.
            geometryFeature.ClearRegionSelection();
            geometryFeature.ClearSplitterSelection();
            SetSelectionTarget(FeatureSelectionTargetEnum.Region);
            propertyGrid.Refresh();
            HighlightGeometryFeature();
            //
            // OK - New rebuilds the form, so what is to survive it is put aside here
            if (onOkAddNew)
                _repeatTypeIndex = GeometryFeatureTypes.GetIndex(SplitType, _viewGeometryFeature);
            else
            {
                DialogResult = DialogResult.OK;
                ItemSetDataEditor.SelectionForm.Hide();
            }
        }
        protected override void OnHideOrClose()
        {
            if (_controller == null)
            {
                base.OnHideOrClose();
                return;
            }
            // The splits of this dialog can be undone only while it is open
            _controller.EndSplitGeometryUndo();
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
            //
            if (_annotateWithColorBeforeForm.HasValue &&
                _controller.AnnotateWithColor != _annotateWithColorBeforeForm.Value)
                _controller.SetAnnotateWithColor(_annotateWithColorBeforeForm.Value);
            _annotateWithColorBeforeForm = null;
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            DialogResult = DialogResult.None;
            //
            btnOkAddNew.Visible = true;
            //
            _propertyItemChanged = false;
            _viewGeometryFeature = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            _controller.ClearAllSelection();
            _controller.BeginSplitGeometryUndo();
            UpdateUndoButton();
            // OK - New prepares the form again without closing it, and the view to return to stays the first one
            if (!_annotateWithColorBeforeForm.HasValue) _annotateWithColorBeforeForm = _controller.AnnotateWithColor;
            //
            PopulateListOfGeometryFeatures();
            lvTypes.Enabled = true;
            btnOkAddNew.Visible = true;
            //
            ShowHideSelectionForm();
            //
            UpdateAnnotateMeshTypesButton();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            // After OK - New the operation that was just used comes back selected, and otherwise the first one
            // is, so that the parts picked in the view are the region from the start
            if (_repeatTypeIndex >= 0 && _repeatTypeIndex < lvTypes.Items.Count)
                lvTypes.Items[_repeatTypeIndex].Selected = true;
            else if (lvTypes.Items.Count > 0)
                lvTypes.Items[0].Selected = true;
            _repeatTypeIndex = -1;
            //
            return true;
        }


        // Methods
        private void UpdateUndoButton()
        {
            btnUndo.Enabled = _controller.CanUndoSplitGeometry;
        }
        private void ThrowIfGeometryFeatureCannotSplit(GeometryFeature geometryFeature)
        {
            if (geometryFeature == null) throw new CaeException("No geometry feature was selected.");
            //
            if (geometryFeature.CreationIds == null || geometryFeature.CreationIds.Length == 0)
                throw new CaeException(RegionPicksParts ? "Select at least one part to split." :
                                                          "Select at least one surface to split.");
            //
            string error = geometryFeature.CheckValidity();
            if (error != null) throw new CaeException(error);
        }
        private void UpdateAnnotateMeshTypesButton()
        {
            bool annotated = _controller.AnnotateWithColor == AnnotateWithColorEnum.MeshTypes;
            if (cbAnnotateMeshTypes.Checked == annotated) return;
            //
            _suppressAnnotateMeshTypesChanged = true;
            cbAnnotateMeshTypes.Checked = annotated;
            _suppressAnnotateMeshTypesChanged = false;
        }
        /// <summary>
        /// Tells a newly built view what its region is, which is what makes one of the two splitting tools
        /// out of the operations they share.
        /// </summary>
        protected virtual void PrepareGeometryFeatureView(ViewGeometryFeature viewGeometryFeature)
        {
            viewGeometryFeature.GetBase().SplitType = SplitType;
        }
        private void PopulateListOfGeometryFeatures()
        {
            ListViewItem item;
            //
            for (int i = 0; i < GeometryFeatureTypes.Count(SplitType); i++)
            {
                item = new ListViewItem(GeometryFeatureTypes.GetDisplayName(SplitType, i));
                ViewGeometryFeature view = GeometryFeatureTypes.CreateDefaultView(SplitType, i);
                PrepareGeometryFeatureView(view);
                item.Tag = view;
                lvTypes.Items.Add(item);
            }
        }
        private void ShowHideSelectionForm()
        {
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            else
            {
                if (GeometryFeature != null)
                {
                    ItemSetDataEditor.SelectionForm.ItemSetData = new ItemSetData(GetTargetIds());
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                }
                else ItemSetDataEditor.SelectionForm.Hide();
            }
            //
            SetSelectItem();
        }
        private void SetSelectionTarget(FeatureSelectionTargetEnum selectionTarget)
        {
            if (_selectionTarget == selectionTarget && ItemSetDataEditor.SelectionForm.Visible) return;
            //
            _selectionTarget = selectionTarget;
            // A plane the user has not picked yet is put through the middle of the region, so that arming
            // the splitter shows a plane that cuts the parts rather than one at the global origin
            if (_selectionTarget == FeatureSelectionTargetEnum.Splitter)
            {
                _controller.SetSplitGeometryDefaultPlanePoint(GeometryFeature);
                propertyGrid.Refresh();
            }
            //
            SelectPropertyGridRow(GetSelectionTargetPropertyName(selectionTarget));
            //
            _controller.ClearSelectionHistory();
            // Continue the selection that belongs to the newly armed target. It is installed before the
            // picker is set up, because setting the picker up puts the limits of the armed target on
            // whichever selection is live, and a selection installed after that carries its own back.
            Selection targetData = GetTargetData();
            if (targetData != null) _controller.Selection = targetData.DeepCopy(true, false);
            //
            ShowHideSelectionForm();
            // Arming a picker clears the selection that was drawn, so what the feature shows of itself
            // - the highlighted parts and the tool symbol - is drawn again
            HighlightGeometryFeature();
        }
        private void SetSelectItem()
        {
            if (GeometryFeature == null) { _controller.SetSelectByToOff(); return; }
            //
            // Three points reads a point out of every pick, while point and normal reads either the
            // point or the direction and lets the pick position choose. What a feature reads out of a
            // pick is the feature's own, so the hovering follows the same rule the split itself does
            if (_selectionTarget != FeatureSelectionTargetEnum.Splitter)
                _controller.GeometryFeaturePointPicking = GeometryFeaturePointPickingEnum.None;
            else _controller.GeometryFeaturePointPicking = Controller.GetSplitGeometryPointPicking(GeometryFeature);
            //
            // A split names the solids it cuts, so one pick on a compound is one of its sub parts and the
            // whole compound is named by dragging an area over it. A picked surface already names one sub
            // part, so there the question does not arise
            _controller.PickCompoundSubPartsSeparately =
                _selectionTarget == FeatureSelectionTargetEnum.Region && RegionPicksParts;
            //
            if (_selectionTarget == FeatureSelectionTargetEnum.Region)
            {
                if (RegionPicksParts) _controller.SetSelectItemToPart();
                else _controller.SetSelectItemToGeometrySurface();
            }
            // The picker takes any geometry and the pick is sorted by the kind of entity it hit, which
            // is also why the dialog does not force a "Select by" mode of its own
            else _controller.SetSelectItemToGeometry();
            // One point, one face or one chain of edges - a cap only helps where the count is fixed
            if (GeometryFeature is SplitByPlaneThreePoints && _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 3;
            else if (GeometryFeature is SplitByLineTwoPoints &&
                     _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 2;
            else if (GeometryFeature is SplitByPointAndNormal && _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 2;
            // One item gives a direction of its own, two give the direction across both
            else if (_selectionTarget == FeatureSelectionTargetEnum.Direction)
                _controller.Selection.MaxNumberOfGeometryIds = 2;
            else _controller.Selection.MaxNumberOfGeometryIds = -1;
        }
        private int[] GetTargetIds()
        {
            if (GeometryFeature == null) return null;
            //
            if (_selectionTarget == FeatureSelectionTargetEnum.Region) return GeometryFeature.CreationIds;
            else if (_selectionTarget == FeatureSelectionTargetEnum.Direction &&
                     GeometryFeature is SplitByExtrudedEdges see) return see.DirectionIds;
            else return GeometryFeature.SplitterIds;
        }
        private Selection GetTargetData()
        {
            if (GeometryFeature == null) return null;
            //
            if (_selectionTarget == FeatureSelectionTargetEnum.Region) return GeometryFeature.CreationData;
            else if (_selectionTarget == FeatureSelectionTargetEnum.Direction &&
                     GeometryFeature is SplitByExtrudedEdges see) return see.DirectionData;
            else return GeometryFeature.SplitterData;
        }
        private void HighlightGeometryFeature()
        {
            try
            {
                if (_closing || _splitting || GeometryFeature == null) return;
                //
                _controller.DrawSplitGeometrySymbols(GeometryFeature,
                                                     _selectionTarget == FeatureSelectionTargetEnum.Region);
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (GeometryFeature == null) return;
            // A pick on a part the exploded view holds somewhere else cannot take part in the
            // operation, and taking it back as it is made says so where building a tool and refusing
            // it at the end would not. Undoing the pick asks for the selection again, which arrives
            // here without it
            bool idsArePartIds = _selectionTarget == FeatureSelectionTargetEnum.Region && RegionPicksParts;
            if (_controller.DropSplitGeometryPickAtOtherOffset(GeometryFeature, ids, idsArePartIds,
                                                              _selectionTarget)) return;
            //
            if (_selectionTarget == FeatureSelectionTargetEnum.Region)
            {
                GeometryFeature.CreationIds = ids;
                GeometryFeature.CreationData = _controller.Selection.DeepCopy(true, false);
            }
            else if (_selectionTarget == FeatureSelectionTargetEnum.Direction &&
                     GeometryFeature is SplitByExtrudedEdges splitByExtrudedEdges)
            {
                splitByExtrudedEdges.DirectionIds = ids;
                splitByExtrudedEdges.DirectionData = _controller.Selection.DeepCopy(true, false);
                _controller.UpdateSplitGeometryFromDirectionSelection(GeometryFeature);
            }
            else
            {
                GeometryFeature.SplitterIds = ids;
                GeometryFeature.SplitterData = _controller.Selection.DeepCopy(true, false);
                _controller.UpdateSplitGeometryFromSplitterSelection(GeometryFeature);
            }
            //
            _viewGeometryFeature.UpdateVisibility();
            propertyGrid.Refresh();
            //
            HighlightGeometryFeature();
            //
            _propertyItemChanged = true;
        }
        private static string GetSelectionTargetPropertyName(FeatureSelectionTargetEnum selectionTarget)
        {
            if (selectionTarget == FeatureSelectionTargetEnum.Region)
                return ViewGeometryFeature.RegionSelectionName;
            else if (selectionTarget == FeatureSelectionTargetEnum.Direction)
                return ViewGeometryFeature.DirectionSelectionName;
            else return ViewGeometryFeature.SplitterSelectionName;
        }
        private void SelectPropertyGridRow(string propertyName)
        {
            GridItem root = propertyGrid.SelectedGridItem;
            while (root != null && root.Parent != null) root = root.Parent;
            if (root == null) return;
            //
            GridItem item = FindPropertyGridRow(root, propertyName);
            if (item != null && item != propertyGrid.SelectedGridItem) propertyGrid.SelectedGridItem = item;
        }
        private GridItem FindPropertyGridRow(GridItem parent, string propertyName)
        {
            foreach (GridItem child in parent.GridItems)
            {
                if (child.PropertyDescriptor != null && child.PropertyDescriptor.Name == propertyName) return child;
                //
                GridItem item = FindPropertyGridRow(child, propertyName);
                if (item != null) return item;
            }
            return null;
        }

        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightGeometryFeature();
        }

        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            //
            Selection targetData = GetTargetData();
            if (targetData != null) return targetData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
