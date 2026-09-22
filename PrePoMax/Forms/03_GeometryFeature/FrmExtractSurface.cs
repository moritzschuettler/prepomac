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
    class FrmExtractSurface : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables
        private ViewSurfaceExtraction _viewSurfaceExtraction;
        private Controller _controller;
        private Button btnPreview;


        // Properties
        public SurfaceExtraction SurfaceExtraction
        {
            get { return _viewSurfaceExtraction != null ? _viewSurfaceExtraction.GetBase() : null; }
        }


        // Constructors
        public FrmExtractSurface(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewSurfaceExtraction = null;
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
            // FrmExtractSurface
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.btnPreview);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmExtractSurface";
            this.Text = "Extract Surface";
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
                SurfaceExtraction surfaceExtraction = SurfaceExtraction;
                //
                if (surfaceExtraction == null) throw new CaeException("No surface extraction was prepared.");
                //
                string error = surfaceExtraction.CheckValidity();
                if (error != null) throw new CaeException(error);
                //
                await _controller.PreviewExtractSurfaceAsync(surfaceExtraction.DeepClone());
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }


        // Overrides
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            else
            {
                _controller.ClearExtractSurfacePreview();
                _controller.ClearAllSelection();
            }
            //
            base.OnVisibleChanged(e);
        }
        // A preview shows the extraction as it stood when the button was pressed, so a change of the
        // offset or of the picked surfaces takes it off the screen rather than leaving a stale one there
        protected override void OnPropertyGridPropertyValueChanged()
        {
            _controller.ClearExtractSurfacePreview();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            SurfaceExtraction surfaceExtraction = SurfaceExtraction;
            //
            if (surfaceExtraction == null) throw new CaeException("No surface extraction was prepared.");
            //
            string error = surfaceExtraction.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            _controller.ClearExtractSurfacePreview();
            _controller.ExtractSurfaceCommand(surfaceExtraction.DeepClone());
            // The parts the surfaces were taken from are left as they were, but a selection carried into the
            // next extraction would extract the same surfaces again, so the picking starts over
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
            _viewSurfaceExtraction = null;
            propertyGrid.SelectedObject = null;
            _controller.ClearExtractSurfacePreview();
            _controller.ClearAllSelection();
            //
            btnOkAddNew.Visible = true;
            //
            _viewSurfaceExtraction = new ViewSurfaceExtraction(new SurfaceExtraction());
            propertyGrid.SelectedObject = _viewSurfaceExtraction;
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
        private void ShowHideSelectionForm()
        {
            if (!Visible || this.Modal) ItemSetDataEditor.SelectionForm.Hide();
            else
            {
                ItemSetDataEditor.SelectionForm.ItemSetData =
                    new ItemSetData(SurfaceExtraction == null ? null : SurfaceExtraction.CreationIds);
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            }
        }
        private void SetSelectItem()
        {
            // The surfaces are picked on the geometry; how they are picked stays the choice of the user
            _controller.SetSelectItemToGeometrySurface();
            _controller.Selection.MaxNumberOfGeometryIds = -1;
        }
        private void HighlightSelection()
        {
            try
            {
                if (_closing || SurfaceExtraction == null) return;
                //
                _controller.ClearSelectionHistory();
                //
                if (SurfaceExtraction.CreationData != null)
                {
                    _controller.Selection = SurfaceExtraction.CreationData.DeepCopy(true, false);
                    _controller.HighlightSelection();
                }
            }
            catch { }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (SurfaceExtraction == null) return;
            //
            _controller.ClearExtractSurfacePreview();
            //
            SurfaceExtraction.CreationIds = ids;
            SurfaceExtraction.CreationData = _controller.Selection.DeepCopy(true, false);
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
            if (SurfaceExtraction != null && SurfaceExtraction.CreationData != null)
                return SurfaceExtraction.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
