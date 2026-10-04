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
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmRepairGeometry
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmRepairGeometry";
            this.Text = "Repair Geometry";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else _controller.ClearAllSelection();
            //
            base.OnVisibleChanged(e);
        }
        protected override async Task OnApplyAsync(bool onOkAddNew)
        {
            GeometryRepair geometryRepair = GeometryRepair;
            if (geometryRepair == null) throw new CaeException("No repair was prepared.");
            //
            string error = geometryRepair.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            string[] partNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(geometryRepair.CreationIds);
            if (partNames == null || partNames.Length == 0) throw new CaeException("No parts were selected.");
            // The repair replaces the parts and redraws them, so the highlight drawn on the old parts is taken off
            try
            {
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
