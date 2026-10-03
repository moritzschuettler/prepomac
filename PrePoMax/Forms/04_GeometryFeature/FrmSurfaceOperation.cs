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
    class FrmSurfaceOperation : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewSurfaceOperation _viewSurfaceOperation;
        private Controller _controller;
        private SurfaceOperationEnum _operation;
        private AnnotateWithColorEnum? _annotateWithColorBeforeShow;


        // Properties
        public GeometrySelection GeometrySelection
        {
            get { return _viewSurfaceOperation != null ? _viewSurfaceOperation.GetBase() : null; }
        }
        public SurfaceOperationEnum Operation { get { return _operation; } set { _operation = value; } }


        // Constructors
        public FrmSurfaceOperation(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewSurfaceOperation = null;
            _operation = SurfaceOperationEnum.DeleteCadFaces;
            _annotateWithColorBeforeShow = null;
            //
            SelectionClear = _controller.Selection.Clear;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmSurfaceOperation
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmSurfaceOperation";
            this.Text = "Surface Operation";
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
            GeometrySelection geometrySelection = GeometrySelection;
            //
            if (geometrySelection == null || geometrySelection.GeometryIds == null ||
                geometrySelection.GeometryIds.Length == 0)
                throw new CaeException("No surfaces were selected.");
            //
            try
            {
                Enabled = false;
                await _controller.SurfaceOperationCommandAsync(_operation, geometrySelection.DeepClone());
            }
            finally
            {
                Enabled = true;
            }
            // The operation has replaced the parts the surfaces were picked on, so the picking starts over
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
            RestoreAnnotateWithColor();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            DialogResult = DialogResult.None;
            //
            _propertyItemChanged = false;
            _viewSurfaceOperation = null;
            propertyGrid.SelectedObject = null;
            _controller.ClearAllSelection();
            //
            btnOkAddNew.Visible = true;
            //
            _viewSurfaceOperation = new ViewSurfaceOperation(new GeometrySelection("Selection"), _operation);
            propertyGrid.SelectedObject = _viewSurfaceOperation;
            //
            SetAnnotateWithColor();
            //
            SetSelectItem();
            //
            ShowHideSelectionForm();
            //
            HighlightSelection();
            //
            return true;
        }


        // Methods
        // Which way a surface faces is what a flip changes, so the orientation is shown while the dialog is open
        private void SetAnnotateWithColor()
        {
            if (_operation != SurfaceOperationEnum.FlipNormal || _annotateWithColorBeforeShow != null) return;
            //
            _annotateWithColorBeforeShow = _controller.AnnotateWithColor;
            _controller.AnnotateWithColor = AnnotateWithColorEnum.FaceOrientation;
        }
        private void RestoreAnnotateWithColor()
        {
            if (_annotateWithColorBeforeShow == null) return;
            //
            _controller.AnnotateWithColor = _annotateWithColorBeforeShow.Value;
            _annotateWithColorBeforeShow = null;
        }
        private void ShowHideSelectionForm()
        {
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            else
            {
                ItemSetDataEditor.SelectionForm.ItemSetData =
                    new ItemSetData(GeometrySelection == null ? null : GeometrySelection.GeometryIds);
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            }
        }
        private void SetSelectItem()
        {
            _controller.SetSelectItemToGeometrySurface();
            _controller.Selection.MaxNumberOfGeometryIds = -1;
        }
        private void HighlightSelection()
        {
            try
            {
                if (_closing || GeometrySelection == null) return;
                //
                _controller.ClearSelectionHistory();
                //
                if (GeometrySelection.CreationData != null)
                {
                    _controller.Selection = GeometrySelection.CreationData.DeepCopy(true, false);
                    _controller.HighlightSelection();
                }
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (GeometrySelection == null) return;
            //
            GeometrySelection.GeometryIds = ids;
            GeometrySelection.CreationData = _controller.Selection.DeepCopy(true, false);
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
            if (GeometrySelection != null && GeometrySelection.CreationData != null)
                return GeometrySelection.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
