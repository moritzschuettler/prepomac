using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmExtractShellMesh : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent
    {
        // Variables
        private string _solidPartName;
        private int[] _selectedFaceIds;
        private ViewExtractShellMesh _viewData;
        private Controller _controller;


        // Constructors
        public FrmExtractShellMesh(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _solidPartName = null;
            _selectedFaceIds = new int[0];
            _viewData = null;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmExtractShellMesh
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmExtractShellMesh";
            this.Text = "Extract Shell Mesh";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Overrides
        protected override bool OnPrepareForm(string stepName, string solidPartName)
        {
            this.btnOkAddNew.Visible = false;
            //
            _propertyItemChanged = false;
            _solidPartName = solidPartName;
            _selectedFaceIds = new int[0];
            _viewData = new ViewExtractShellMesh(solidPartName);
            propertyGrid.SelectedObject = _viewData;
            //
            ItemSetDataEditor.SelectionForm.ItemSetData = new ItemSetData(new int[0]);
            ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            //
            _controller.SetSelectItemToSurface();
            _controller.Selection.LimitSelectionToFirstPart = true;
            _controller.Selection.Clear();
            //
            return true;
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewData = (ViewExtractShellMesh)propertyGrid.SelectedObject;
            //
            if (string.IsNullOrWhiteSpace(_viewData.Name))
                throw new CaeException("The new part name must not be empty.");
            //
            if (_selectedFaceIds == null || _selectedFaceIds.Length == 0)
                throw new CaeException("At least one face must be selected.");
            //
            _controller.ExtractShellMeshCommand(_solidPartName, _viewData.Name, _selectedFaceIds, _viewData.CondenseNodes, _viewData.SecondOrder);
            //
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            ItemSetDataEditor.SelectionForm.Hide();
            _controller.Selection.LimitSelectionToFirstPart = false;
            //
            base.OnHideOrClose();
        }


        // IFormItemSetDataParent
        public void SelectionChanged(int[] ids)
        {
            _selectedFaceIds = ids ?? new int[0];
            //
            if (_viewData != null)
            {
                _viewData.SelectedFaces = _selectedFaceIds.Length;
                propertyGrid.Refresh();
            }
            //
            _propertyItemChanged = true;
        }
        public bool IsSelectionGeometryBased()
        {
            return false;
        }
        public bool IsGeometrySelectionIdBased()
        {
            return false;
        }
    }
}
