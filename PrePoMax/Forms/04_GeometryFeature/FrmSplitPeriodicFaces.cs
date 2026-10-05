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
    class FrmSplitPeriodicFaces : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewPeriodicFaceSplit _viewPeriodicFaceSplit;
        private Controller _controller;
        private bool _applying;


        // Properties
        public PeriodicFaceSplit PeriodicFaceSplit
        {
            get { return _viewPeriodicFaceSplit != null ? _viewPeriodicFaceSplit.GetBase() : null; }
        }


        // Constructors
        public FrmSplitPeriodicFaces(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewPeriodicFaceSplit = null;
            _applying = false;
            //
            SelectionClear = _controller.Selection.Clear;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmSplitPeriodicFaces
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmSplitPeriodicFaces";
            this.Text = "Split Periodic Faces";
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
            PeriodicFaceSplit periodicFaceSplit = PeriodicFaceSplit;
            if (periodicFaceSplit == null) throw new CaeException("No split was prepared.");
            //
            string error = periodicFaceSplit.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            string[] partNames = GetPartNames(periodicFaceSplit);
            // The split replaces the parts and redraws them, so the highlight drawn on the old parts is taken off
            try
            {
                _applying = true;
                Enabled = false;
                _controller.Clear3DSelection();
                await _controller.SplitPeriodicFacesCommandAsync(partNames, periodicFaceSplit.NumOfFaces);
            }
            finally
            {
                periodicFaceSplit.CreationIds = null;
                periodicFaceSplit.CreationData = null;
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
            _viewPeriodicFaceSplit = null;
            propertyGrid.SelectedObject = null;
            _controller.ClearAllSelection();
            //
            btnOkAddNew.Visible = true;
            //
            _viewPeriodicFaceSplit = new ViewPeriodicFaceSplit(new PeriodicFaceSplit());
            propertyGrid.SelectedObject = _viewPeriodicFaceSplit;
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
        private string[] GetPartNames(PeriodicFaceSplit periodicFaceSplit)
        {
            string[] partNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(periodicFaceSplit.CreationIds);
            if (partNames == null || partNames.Length == 0) throw new CaeException("No parts were selected.");
            return partNames;
        }
        private void ShowHideSelectionForm()
        {
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            else
            {
                ItemSetDataEditor.SelectionForm.ItemSetData =
                    new ItemSetData(PeriodicFaceSplit == null ? null : PeriodicFaceSplit.CreationIds);
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
                if (_closing || _applying || PeriodicFaceSplit == null) return;
                //
                _controller.ClearSelectionHistory();
                //
                if (PeriodicFaceSplit.CreationData != null)
                {
                    _controller.Selection = PeriodicFaceSplit.CreationData.DeepCopy(true, false);
                    _controller.HighlightSelection();
                }
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (PeriodicFaceSplit == null) return;
            //
            PeriodicFaceSplit.CreationIds = ids;
            PeriodicFaceSplit.CreationData = _controller.Selection.DeepCopy(true, false);
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
            if (PeriodicFaceSplit != null && PeriodicFaceSplit.CreationData != null)
                return PeriodicFaceSplit.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
