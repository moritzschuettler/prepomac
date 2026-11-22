// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmChangeElementOrder : UserControls.FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private ViewChangeElementOrder _viewChangeElementOrder;
        private Controller _controller;


        // Constructor                                                                                                              
        public FrmChangeElementOrder(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewChangeElementOrder = null;
            //
            SelectionClear = _controller.Selection.Clear;
        }
        private void InitializeComponent()
        {
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // FrmChangeElementOrder
            //
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.MinimumSize = new System.Drawing.Size(350, 350);
            this.Name = "FrmChangeElementOrder";
            this.Text = "Change Element Order";
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (_viewChangeElementOrder.CreationIds == null || _viewChangeElementOrder.CreationIds.Length == 0)
                throw new CaeException("The part selection must contain at least one item.");
            //
            string[] partNames = _controller.DisplayedMesh.GetPartNamesFromGeometryIds(_viewChangeElementOrder.CreationIds);
            string error = _controller.GetChangeElementOrderError(partNames);
            if (error != null) throw new CaeException(error);
            //
            _controller.ChangeElementOrderCommand(partNames, _viewChangeElementOrder.SecondOrder);
            //
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string unusedName)
        {
            _propertyItemChanged = false;
            propertyGrid.SelectedObject = null;
            //
            _viewChangeElementOrder = new ViewChangeElementOrder(!_controller.Model.Mesh.HasParabolicElements());
            _controller.Selection.Clear();
            //
            propertyGrid.SelectedObject = _viewChangeElementOrder;
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
        private void HighlightSelection()
        {
            try
            {
                if (_controller != null)
                {
                    _controller.ClearSelectionHistory();
                    if (_viewChangeElementOrder.CreationData != null)
                    {
                        _controller.Selection = _viewChangeElementOrder.CreationData.DeepCopy(true, false);
                        _controller.HighlightSelection();
                    }
                }
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
        }
        private void SetSelectItem()
        {
            _controller.SetSelectItemToPart();
        }
        public void SelectionChanged(int[] ids)
        {
            if (_viewChangeElementOrder != null)
            {
                _viewChangeElementOrder.CreationIds = ids;
                _viewChangeElementOrder.CreationData = _controller.Selection.DeepCopy(true, false);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightSelection();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            // Always use geometry based selection
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            // Always use default selection mode
            return defaultMode;
        }

    }


}
