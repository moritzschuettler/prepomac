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
    class FrmRepairGeometry : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewGeometryRepair _viewGeometryRepair;
        private Controller _controller;
        private bool _applying;
        private Button btnPreview;


        // Properties
        public GeometryRepair GeometryRepair
        {
            get { return _viewGeometryRepair != null ? _viewGeometryRepair.GetBase() : null; }
        }


        // Constructors
        public FrmRepairGeometry(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewGeometryRepair = null;
            _applying = false;
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
            // FrmRepairGeometry
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.btnPreview);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmRepairGeometry";
            this.Text = "Repair Geometry";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.btnPreview, 0);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers
        private async void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                GeometryRepair geometryRepair = GeometryRepair;
                if (geometryRepair == null) throw new CaeException("No repair was prepared.");
                //
                string error = geometryRepair.CheckValidity();
                if (error != null) throw new CaeException(error);
                //
                btnPreview.Enabled = false;
                await _controller.PreviewRepairGeometryAsync(GetPartNames(geometryRepair), geometryRepair.DeepClone());
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


        // Overrides
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else
            {
                _controller.ClearRepairGeometryPreview();
                _controller.ClearAllSelection();
            }
            //
            base.OnVisibleChanged(e);
        }
        // A preview shows the repair as it stood when the button was pressed, so a change of a repair, of the
        // tolerance or of the picked parts takes it off the screen rather than leaving a stale one there
        protected override void OnPropertyGridPropertyValueChanged()
        {
            _controller.ClearRepairGeometryPreview();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override async Task OnApplyAsync(bool onOkAddNew)
        {
            GeometryRepair geometryRepair = GeometryRepair;
            if (geometryRepair == null) throw new CaeException("No repair was prepared.");
            //
            string error = geometryRepair.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            string[] partNames = GetPartNames(geometryRepair);
            // The repair replaces the parts and redraws them, so the highlight drawn on the old parts is taken off
            try
            {
                _controller.ClearRepairGeometryPreview();
                _applying = true;
                Enabled = false;
                _controller.Clear3DSelection();
                await _controller.RepairGeometryCommandAsync(partNames, geometryRepair.DeepClone());
            }
            finally
            {
                geometryRepair.CreationIds = null;
                geometryRepair.CreationData = null;
                _applying = false;
                Enabled = true;
            }
            // The picking starts over
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            propertyGrid.Refresh();
            //
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
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            DialogResult = DialogResult.None;
            //
            _propertyItemChanged = false;
            _viewGeometryRepair = null;
            propertyGrid.SelectedObject = null;
            _controller.ClearAllSelection();
            //
            btnOkAddNew.Visible = true;
            //
            _viewGeometryRepair = new ViewGeometryRepair(new GeometryRepair(), GetInitialTolerance());
            propertyGrid.SelectedObject = _viewGeometryRepair;
            //
            SetSelectItem();
            //
            ShowHideSelectionForm();
            //
            HighlightSelection();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods
        private string[] GetPartNames(GeometryRepair geometryRepair)
        {
            string[] partNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(geometryRepair.CreationIds);
            if (partNames == null || partNames.Length == 0) throw new CaeException("No parts were selected.");
            return partNames;
        }
        // The value offered beside the default, sized as the default is but on the whole geometry
        private double GetInitialTolerance()
        {
            BoundingBox box = _controller.Model.Geometry.BoundingBox;
            double diagonal = Math.Sqrt((box.MaxX - box.MinX) * (box.MaxX - box.MinX) +
                                        (box.MaxY - box.MinY) * (box.MaxY - box.MinY) +
                                        (box.MaxZ - box.MinZ) * (box.MaxZ - box.MinZ));
            return diagonal > 0 && !double.IsInfinity(diagonal) ? 1E-4 * diagonal : 1E-7;
        }
        private void ShowHideSelectionForm()
        {
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            else
            {
                ItemSetDataEditor.SelectionForm.ItemSetData =
                    new ItemSetData(GeometryRepair == null ? null : GeometryRepair.CreationIds);
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            }
        }
        private void SetSelectItem()
        {
            _controller.SetSelectItemToPart();
        }
        private void HighlightSelection()
        {
            try
            {
                if (_closing || _applying || GeometryRepair == null) return;
                //
                _controller.ClearSelectionHistory();
                //
                if (GeometryRepair.CreationData != null)
                {
                    _controller.Selection = GeometryRepair.CreationData.DeepCopy(true, false);
                    _controller.HighlightSelection();
                }
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (GeometryRepair == null) return;
            //
            _controller.ClearRepairGeometryPreview();
            //
            GeometryRepair.CreationIds = ids;
            GeometryRepair.CreationData = _controller.Selection.DeepCopy(true, false);
            //
            propertyGrid.Refresh();
            //
            _propertyItemChanged = true;
        }

        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightSelection();
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
            if (GeometryRepair != null && GeometryRepair.CreationData != null)
                return GeometryRepair.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
