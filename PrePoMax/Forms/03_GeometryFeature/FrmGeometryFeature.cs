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
    class FrmGeometryFeature : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewGeometryFeature _viewGeometryFeature;
        private Controller _controller;
        private FeatureSelectionTargetEnum _selectionTarget;
        // OK - New rebuilds the form, and the operation that was just used comes back selected
        private int _repeatTypeIndex;


        // Properties
        public GeometryFeature GeometryFeature
        {
            get { return _viewGeometryFeature != null ? _viewGeometryFeature.GetBase() : null; }
        }


        // Constructors
        public FrmGeometryFeature(Controller controller)
            : base(1.72)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewGeometryFeature = null;
            _selectionTarget = FeatureSelectionTargetEnum.Region;
            _repeatTypeIndex = -1;
        }
        private void InitializeComponent()
        {
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // gbType
            //
            this.gbType.Size = new System.Drawing.Size(340, 101);
            this.gbType.Text = "Operation";
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
            // FrmGeometryFeature
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(364, 471);
            this.MinimumSize = new System.Drawing.Size(380, 510);
            this.Name = "FrmGeometryFeature";
            this.Text = "Split Geometry";
            this.VisibleChanged += new System.EventHandler(this.FrmGeometryFeature_VisibleChanged);
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.gbType, 0);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers
        private void FrmGeometryFeature_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else
            {
                _controller.ClearGeometryFeatureSymbols();
                _controller.ClearAllSelection();
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
        protected override void OnApply(bool onOkAddNew)
        {
            _viewGeometryFeature = (ViewGeometryFeature)propertyGrid.SelectedObject;
            //
            if (_viewGeometryFeature == null) throw new CaeException("No geometry feature was selected.");
            //
            GeometryFeature geometryFeature = GeometryFeature;
            //
            if (geometryFeature.CreationIds == null || geometryFeature.CreationIds.Length == 0)
                throw new CaeException("Select at least one part to split.");
            //
            string error = geometryFeature.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            _controller.SplitGeometryCommand(geometryFeature.DeepClone());
            // The split has changed the geometry, so the picked ids and everything read from them are
            // stale - a part that was cut has been replaced by a compound of the pieces, and the ids of
            // the region name parts that are gone. Resolving them again would look them up in a model
            // that no longer holds them. Clearing the splitter is also what takes the tool symbol off
            // the screen.
            geometryFeature.ClearRegionSelection();
            geometryFeature.ClearSplitterSelection();
            SetSelectionTarget(FeatureSelectionTargetEnum.Region);
            propertyGrid.Refresh();
            HighlightGeometryFeature();
            //
            // OK - New rebuilds the form, so what is to survive it is put aside here
            if (onOkAddNew) _repeatTypeIndex = GeometryFeatureTypes.GetIndex(_viewGeometryFeature);
            else
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
            //
            PopulateListOfGeometryFeatures();
            lvTypes.Enabled = true;
            btnOkAddNew.Visible = true;
            //
            ShowHideSelectionForm();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            // After OK - New the operation that was just used comes back selected
            if (_repeatTypeIndex >= 0 && _repeatTypeIndex < lvTypes.Items.Count)
                lvTypes.Items[_repeatTypeIndex].Selected = true;
            _repeatTypeIndex = -1;
            //
            return true;
        }


        // Methods
        private void PopulateListOfGeometryFeatures()
        {
            ListViewItem item;
            //
            for (int i = 0; i < GeometryFeatureTypes.Count; i++)
            {
                item = new ListViewItem(GeometryFeatureTypes.GetDisplayName(i));
                item.Tag = GeometryFeatureTypes.CreateDefaultView(i);
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
            // point or the direction and lets the pick position choose
            if (_selectionTarget != FeatureSelectionTargetEnum.Splitter)
                _controller.GeometryFeaturePointPicking = GeometryFeaturePointPickingEnum.None;
            else if (GeometryFeature is SplitByPlaneThreePoints)
                _controller.GeometryFeaturePointPicking = GeometryFeaturePointPickingEnum.PointOnly;
            else if (GeometryFeature is SplitByPointAndNormal)
                _controller.GeometryFeaturePointPicking = GeometryFeaturePointPickingEnum.PointOrEntity;
            else _controller.GeometryFeaturePointPicking = GeometryFeaturePointPickingEnum.None;
            //
            // A split names the solids it cuts, so one pick on a compound is one of its sub parts and the
            // whole compound is named by dragging an area over it
            _controller.PickCompoundSubPartsSeparately = _selectionTarget == FeatureSelectionTargetEnum.Region;
            //
            if (_selectionTarget == FeatureSelectionTargetEnum.Region) _controller.SetSelectItemToPart();
            // The picker takes any geometry and the pick is sorted by the kind of entity it hit, which
            // is also why the dialog does not force a "Select by" mode of its own
            else _controller.SetSelectItemToGeometry();
            // One point, one face or one chain of edges - a cap only helps where the count is fixed
            if (GeometryFeature is SplitByPlaneThreePoints && _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 3;
            else if (GeometryFeature is SplitByPointAndNormal && _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 2;
            else if (GeometryFeature is SplitByExtendedFace && _selectionTarget == FeatureSelectionTargetEnum.Splitter)
                _controller.Selection.MaxNumberOfGeometryIds = 1;
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
                if (_closing || GeometryFeature == null) return;
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
