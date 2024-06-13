using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using CaeModel;
using System.Windows.Forms;
using System.Drawing;
using UserControls;

namespace PrePoMax.Forms
{
    class FrmCoordinateSystem : FrmProperties, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private HashSet<string> _coordinateSystemNames;
        private string _coordinateSystemToEditName;
        private ViewCoordinateSystem _viewCoordinateSystem;
        private Controller _controller;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        private double[][] _coorNodesToDraw;


        // Properties                                                                                                               
        public CoordinateSystem CoordinateSystem
        {
            get { return _viewCoordinateSystem.GetBase(); }
            set { _viewCoordinateSystem = new ViewCoordinateSystem(value.DeepClone()); }
        }
       

        // Constructors                                                                                                             
        public FrmCoordinateSystem(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewCoordinateSystem = null;
            _coordinateSystemNames = new HashSet<string>();
            //
            _coorNodesToDraw = new double[1][];
            _coorNodesToDraw[0] = new double[3];
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
            this.gbProperties.SuspendLayout();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(310, 534);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 506);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 546);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 546);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 546);
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(118, 26);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(117, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmCoordinateSystem
            // 
            this.ClientSize = new System.Drawing.Size(334, 581);
            this.Name = "FrmCoordinateSystem";
            this.Text = "Edit Coordinate System";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewCoordinateSystem.CreatedFrom)) { }
            //
            HighlightCoordinateSystem();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            object value = propertyGrid.SelectedGridItem.Value;
            if (value != null)
            {
                if (propertyGrid.SelectedObject == null) { }
                else if (propertyGrid.SelectedObject is ViewCoordinateSystem) { }
                else throw new NotImplementedException();
            }
            HighlightCoordinateSystem();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewCoordinateSystem = (ViewCoordinateSystem)propertyGrid.SelectedObject;
            // Check name
            CheckName(_coordinateSystemToEditName, _viewCoordinateSystem.Name, _coordinateSystemNames, "coordinate system");
            // Check equations
            CoordinateSystem.CheckEquations();
            // Check point selection
            if (!CoordinateSystem.IsProperlyDefined())
                throw new CaeException("One of the directions is not properly defined. The selected points must not be colinear." );
            // Create
            if (_coordinateSystemToEditName == null)
            {
                AddCoordinateSystemCommand(CoordinateSystem);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                 ReplaceCoordinateSystemCommand(_coordinateSystemToEditName, CoordinateSystem);
                _coordinateSystemToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the coordinate system from internal to show it
            else
            {
                CoordinateSystemInternal(false);
            }
            // If all is successful turn off the selection
            TurnOffSelection();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            TurnOffSelection();
            // Convert the coordinate system from internal to show it
            CoordinateSystemInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string coordinateSystemToEditName)
        {
            this.btnOkAddNew.Visible = coordinateSystemToEditName == null;
            //
            _propertyItemChanged = false;
            _coordinateSystemNames.Clear();
            _coordinateSystemToEditName = null;
            _viewCoordinateSystem = null;
            //
            _coordinateSystemNames.UnionWith(GetCoordinateSystemNames());
            _coordinateSystemToEditName = coordinateSystemToEditName;
            // Create new coordinate system
            if (_coordinateSystemToEditName == null)
            {
                CoordinateSystem = new CoordinateSystem(GetCoordinateSystemName(),
                                                        _controller.Model.Properties.ModelSpace.IsTwoD());
                //
                CoordinateSystem.Color = _controller.Settings.Pre.ConstraintSymbolColor;
            }
            // Edit existing coordinate system
            else
            {
                CoordinateSystem = GetCoordinateSystem(_coordinateSystemToEditName); // to clone
                // Convert the coordinate system to internal to hide it
                CoordinateSystemInternal(true);
            }
            //
            propertyGrid.SelectedObject = _viewCoordinateSystem;
            propertyGrid.Select();
            //
            TurnOffSelection();
            //
            HighlightCoordinateSystem();
            //
            return true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _viewCoordinateSystem.GetBase().Reset();
            _propertyItemChanged = true;
            propertyGrid.Refresh();
            HighlightCoordinateSystem();
        }

        // Methods                                                                                                                  
        public void PickedIds(int[] ids)
        {
            this.Enabled = true;
            // Disable selection
            _controller.SetSelectByToOff();
            _controller.Selection.SelectItem = vtkSelectItem.None;
            //
            FeMesh mesh = GetMesh();
            if (ids != null && ids.Length == 1)
            {
                FeNode node = mesh.Nodes[ids[0]];
                double x = node.X;
                double y = node.Y;
                double z = node.Z;
                //
                CoordinateSystem cs = _viewCoordinateSystem.GetBase();
                string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                if (propertyName == nameof(_viewCoordinateSystem.Point1ItemSet))
                {
                    double deltaX = x - cs.X1.Value;
                    double deltaY = y - cs.Y1.Value;
                    double deltaZ = z - cs.Z1.Value;
                    //
                    cs.X1.SetEquationFromValue(x, true);
                    cs.Y1.SetEquationFromValue(y, true);
                    cs.Z1.SetEquationFromValue(z, true);
                    //
                    if (cs.CreatedFrom == CoordinateSystemCreatedFromEnum.CenterXY)
                    {
                        cs.X2.SetEquationFromValue(cs.X2.Value + deltaX, true);
                        cs.Y2.SetEquationFromValue(cs.Y2.Value + deltaY, true);
                        cs.Z2.SetEquationFromValue(cs.Z2.Value + deltaZ, true);
                        //
                        cs.X3.SetEquationFromValue(cs.X3.Value + deltaX, true);
                        cs.Y3.SetEquationFromValue(cs.Y3.Value + deltaY, true);
                        cs.Z3.SetEquationFromValue(cs.Z3.Value + deltaZ, true);
                    }
                }
                else if (propertyName == nameof(_viewCoordinateSystem.Point2ItemSet))
                {
                    cs.X2.SetEquationFromValue(x, true);
                    cs.Y2.SetEquationFromValue(y, true);
                    cs.Z2.SetEquationFromValue(z, true);
                }
                else if (propertyName == nameof(_viewCoordinateSystem.Point3ItemSet))
                {
                    cs.X3.SetEquationFromValue(x, true);
                    cs.Y3.SetEquationFromValue(y, true);
                    cs.Z3.SetEquationFromValue(z, true);
                }
                else return;
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
                //
                _controller.ClearSelectionHistory();    // must be here to reset the number of picked ids
                //
                HighlightCoordinateSystem();
            }
        }
        private string GetCoordinateSystemName()
        {
            return _coordinateSystemNames.GetNextNumberedKey("Coordinate_System");
        }
        private void HighlightCoordinateSystem()
        {
            try
            {
                _controller.ClearSelectionHistory();
                _controller.HighlightCoordinateSystem(_viewCoordinateSystem.GetBase());
            }
            catch { }
        }
        private void TurnOffSelection()
        {
            _controller.SetSelectByToOff();
            _controller.Selection.SelectItem = vtkSelectItem.None;
        }
        private void CoordinateSystemInternal(bool toInternal)
        {
            // Convert the coordinate system from/to internal to hide/show it
            if (_coordinateSystemToEditName != null)
            {
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    _controller.GetModelCoordinateSystem(_coordinateSystemToEditName).Internal = toInternal;
                    _controller.FeModelUpdate(UpdateType.RedrawSymbols);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    _controller.GetResultCoordinateSystem(_coordinateSystemToEditName).Internal = toInternal;
                    _controller.FeResultsUpdate(UpdateType.RedrawSymbols);
                }
                else throw new NotSupportedException();
            }
        }
        //
        private FeMesh GetMesh()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.Model.Mesh;
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.AllResults.CurrentResult.Mesh;
            else throw new NotSupportedException();
        }
        private string[] GetCoordinateSystemNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetModelCoordinateSystemNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultCoordinateSystemNames();
            else throw new NotSupportedException();
        }
        private void AddCoordinateSystemCommand(CoordinateSystem coordinateSystem)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.AddModelCoordinateSystemCommand(coordinateSystem);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.AddResultCoordinateSystemCommand(coordinateSystem);
            else throw new NotSupportedException();
        }
        private CoordinateSystem GetCoordinateSystem(string coordinateSystemToEditName)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetModelCoordinateSystem(coordinateSystemToEditName);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultCoordinateSystem(coordinateSystemToEditName);
            else throw new NotSupportedException();
        }
        private void ReplaceCoordinateSystemCommand(string coordinateSystemToEditName, CoordinateSystem coordinateSystem)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.ReplaceModelCoordinateSystemCommand(coordinateSystemToEditName, coordinateSystem);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.ReplaceResultCoordinateSystemCommand(coordinateSystemToEditName, coordinateSystem);
            else throw new NotSupportedException();
        }
        // IFormHighlight
        public void Highlight()
        {
            HighlightCoordinateSystem();
        }

        
    }
}
