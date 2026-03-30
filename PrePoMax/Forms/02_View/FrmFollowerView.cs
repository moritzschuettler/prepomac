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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;


namespace PrePoMax.Forms
{    
    public partial class FrmFollowerView : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables                                                                                                                
        private Controller _controller;
        private ViewFollowerViewParameters _viewFollowerViewParameters;


        // Properties                                                                                                               
        public void SetFollowerViewParameters(FollowerViewParameters parameters)
        {
            if (parameters == null)
                _viewFollowerViewParameters = new ViewFollowerViewParameters(new FollowerViewParameters());
            else
                _viewFollowerViewParameters = new ViewFollowerViewParameters(parameters.DeepClone());
            //
            propertyGrid.SelectedObject = _viewFollowerViewParameters;
        }


        // Constructors                                                                                                             
        public FrmFollowerView(Controller controller)
        {
            InitializeComponent();
            //
            _viewFollowerViewParameters = new ViewFollowerViewParameters(new FollowerViewParameters());
            propertyGrid.SelectedObject = _viewFollowerViewParameters;
            //
            _controller = controller;
            //
            propertyGrid.SetLabelColumnWidth(1.75);
        }


        // Event handlers                                                                                                           
        private void FrmFollowerView_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                btnCancel_Click(null, null);
            }
        }
        private void FrmFollowerView_VisibleChanged(object sender, EventArgs e)
        {
            try
            {
                if (this.Visible)
                {
                    this.Enabled = true;    // activating selection disables the form
                }
                else
                {
                    if (DialogResult == DialogResult.OK) _controller.ApplyFollowerView(_viewFollowerViewParameters.GetBase());
                    else if (DialogResult == DialogResult.Abort) Disable();
                    else if (DialogResult == DialogResult.Cancel) Cancel();
                    // The form was closed from frmMain.CloseAllForms
                    else if (DialogResult == DialogResult.None) Cancel();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewFollowerViewParameters.Type))
            {
                propertyGrid.Refresh();
                //
                HighlightFollowerView();
            }
        }
        //
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                FollowerViewParameters parameters = _viewFollowerViewParameters.GetBase();
                //
                if (!_controller.DisplayedMesh.Nodes.ContainsKey(parameters.CenterNodeId))
                    throw new CaeException("The center node must be selected.");
                if (parameters.Type == FollowerViewTypeEnum.Plane)
                {
                    if (!_controller.DisplayedMesh.Nodes.ContainsKey(parameters.Direction1NodeId))
                        throw new CaeException("The direction 1 node must be selected.");
                    if (!_controller.DisplayedMesh.Nodes.ContainsKey(parameters.Direction2NodeId))
                        throw new CaeException("The direction 2 node must be selected.");
                }
                //
                this.DialogResult = DialogResult.OK;
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnDisable_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Abort;
            Hide();
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Hide();
        }
        

        // Methods                                                                                                                  

        // IFormBase
        public bool PrepareForm(string stepName, string itemToEditName)
        {
            this.DialogResult = DialogResult.None;
            //
            propertyGrid.Refresh();
            //
            _controller.ClearSelectionHistory();
            _controller.SetSelectByToOff();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            HighlightFollowerView();
            //
            return true;
        }
        //
        private void Cancel()
        {
        }
        private void Disable()
        {
            _controller.RemoveCurrentFollowerView();
        }
        //
        public void PickedIds(int[] ids)
        {
            bool selectionFinished = false;
            if (ids != null)
            {
                string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                if (propertyName == nameof(_viewFollowerViewParameters.CenterNodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;  // empty selection
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.CenterNodeId = ids[0];
                        selectionFinished = true;
                    }
                }
                else if (propertyName == nameof(_viewFollowerViewParameters.Direction1NodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;  // empty selection
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.Direction1NodeId = ids[0];
                        selectionFinished = true;
                    }
                }
                else if (propertyName == nameof(_viewFollowerViewParameters.Direction2NodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;  // empty selection
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.Direction2NodeId = ids[0];
                        selectionFinished = true;
                    }
                }
            }
            //
            if (selectionFinished)
            {
                propertyGrid.Refresh();
                this.Enabled = true;    // activating selection disables the form
                _controller.ClearSelectionHistoryAndCallSelectionChanged();
                _controller.SetSelectByToOff();
                _controller.Selection.SelectItem = vtkSelectItem.None;
                //
                HighlightFollowerView();
            }
        }
        private void HighlightFollowerView()
        {
            FeNode node;
            List<double[]> _coorNodesToDraw = new List<double[]>();
            _controller.ClearSelectionHistory();
            //
            if (_controller.DisplayedMesh.Nodes.TryGetValue(_viewFollowerViewParameters.CenterNodeId, out node))
                _coorNodesToDraw.Add(node.Coor);
            if (_coorNodesToDraw.Count > 0) _controller.HighlightNodes(_coorNodesToDraw.ToArray(), true);
            //
            _coorNodesToDraw.Clear();
            if (_viewFollowerViewParameters.Type == FollowerViewTypeEnum.Plane)
            {
                if (_controller.DisplayedMesh.Nodes.TryGetValue(_viewFollowerViewParameters.Direction1NodeId, out node))
                    _coorNodesToDraw.Add(node.Coor);
                if (_controller.DisplayedMesh.Nodes.TryGetValue(_viewFollowerViewParameters.Direction2NodeId, out node))
                    _coorNodesToDraw.Add(node.Coor);
            }
            if (_coorNodesToDraw.Count > 0) _controller.HighlightNodes(_coorNodesToDraw.ToArray(), true);

        }
    }
}
