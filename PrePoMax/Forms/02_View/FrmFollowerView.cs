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
    public partial class FrmFollowerView : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private ViewFollowerViewParameters _viewFollowerViewParameters;
        private Controller _controller;


        // Properties                                                                                                               
        public FollowerViewParameters Parameters
        {
            get { return _viewFollowerViewParameters.GetBase(); }
            set
            {
                if (value == null) _viewFollowerViewParameters = new ViewFollowerViewParameters(value.DeepClone());
                else _viewFollowerViewParameters = null;
            }
        }


        // Constructors                                                                                                             
        public FrmFollowerView(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewFollowerViewParameters = null;
            //
            propertyGrid.SetLabelColumnWidth(1.75);
        }


        // Event handlers                                                                                                           
        protected override void OnApply(bool onOkAddNew)
        {
            FollowerViewParameters parameters = Parameters;
            //
            if (!_controller.CurrentResult.Mesh.Nodes.ContainsKey(parameters.CenterNodeId))
                throw new CaeException("The center node must be selected.");
            if (parameters.Type == FollowerViewTypeEnum.Point) { }
            else if (parameters.Type == FollowerViewTypeEnum.Plane)
            {
                if (!_controller.CurrentResult.Mesh.Nodes.ContainsKey(parameters.Direction1NodeId))
                    throw new CaeException("The direction node 1 must be selected.");
                if (!_controller.CurrentResult.Mesh.Nodes.ContainsKey(parameters.Direction2NodeId))
                    throw new CaeException("The direction node 2 must be selected.");
            }
            else throw new NotSupportedException();
            // Create
            _controller.ApplyFollowerView(parameters);
        }
        protected override bool OnPrepareForm(string stepName, string viewToEditName)
        {
            _propertyItemChanged = false;
            _viewFollowerViewParameters = null;
            // Create new view
            Parameters = _controller.GetFollowerViewParameters();   // to clone
            //
            if (Parameters == null)
            {
                Parameters = new FollowerViewParameters();
                _controller.Selection.Clear();
            }
            //
            propertyGrid.SelectedObject = _viewFollowerViewParameters;
            propertyGrid.Select();
            //
            HighlightFollowerView();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods                                                                                                                  

        private void HighlightFollowerView()
        {
            try
            {
            }
            catch { }
        }

        //
        public void PickedIds(int[] ids)
        {
            bool selectionFinished = false;
            if (ids != null)
            {
                string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (propertyName == nameof(_viewFollowerViewParameters.CenterNodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.GetBase().CenterNodeId = ids[0];
                        selectionFinished = true;
                    }
                }
                else if (propertyName == nameof(_viewFollowerViewParameters.Direction1NodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.GetBase().Direction1NodeId = ids[0];
                        selectionFinished = true;
                    }
                }
                else if (propertyName == nameof(_viewFollowerViewParameters.Direction2NodeItemSetData))
                {
                    if (ids.Length == 0) selectionFinished = true;
                    else if (ids.Length == 1)
                    {
                        _viewFollowerViewParameters.GetBase().Direction2NodeId = ids[0];
                        selectionFinished = true;
                    }
                }
            }
            //
            if (selectionFinished)
            {
                this.Enabled = true;
                _controller.ClearSelectionHistoryAndCallSelectionChanged();
                _controller.SetSelectByToOff();
                _controller.Selection.SelectItem = vtkSelectItem.None;
                //
                HighlightFollowerView();
            }
        }
        //
        
       
    }
}
