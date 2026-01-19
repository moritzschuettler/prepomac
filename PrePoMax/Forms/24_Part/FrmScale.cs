using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;

namespace PrePoMax.Forms
{
    class FrmScale : UserControls.FrmProperties, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private ScaleParameters _scaleParameters;
        private Controller _controller;
        private string[] _partNames;
        private double[] _scaleCenter;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        
        
        // Callbacks                                                                                                                
        public Func<string[], double[], double[], bool, Task> ScaleGeometryPartsAsync;


        // Properties                                                                                                               
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }
        

        // Constructors                                                                                                             
        public FrmScale(Controller controller) 
            : base(1.7)
        {
            InitializeComponent();
            //
            _controller = controller;
            //
            _scaleCenter = new double[3];
            //
            btnOK.Visible = false;
            btnOkAddNew.Width = btnOK.Width;
            btnOkAddNew.Left = btnOK.Left;
            btnOkAddNew.Text = "Apply";
            btnCancel.Text = "Close";
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
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(181, 48);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(180, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmScale
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmScale";
            this.Text = "Scale Parameters";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        protected override void OnPropertyGridPropertyValueChanged()
        {
            Highlight();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                ScaleGeometryParts();
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.ScaleModelPartsCommand(_partNames, _scaleParameters.ScaleCenter, _scaleParameters.ScaleFactors,
                                                   _scaleParameters.Copy);
            else throw new NotSupportedException();
            //
            Highlight();
        }
        async private void ScaleGeometryParts()
        {
            try
            {
                Enabled = false;
                await ScaleGeometryPartsAsync?.Invoke(_partNames, _scaleParameters.ScaleCenter, _scaleParameters.ScaleFactors,
                                                      _scaleParameters.Copy);
                Highlight(); // must be here to start after await
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                Enabled = true;
            }
            
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            // Clear
            tsmiResetAll_Click(null, null);
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            _scaleParameters.Clear();
            // Disable selection
            _controller.SetSelectByToOff();
            // Get center point grid item
            GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                          item.PropertyDescriptor != null &&
                          item.PropertyDescriptor.Name == nameof(_scaleParameters.ScaleOnly));
            // Select it
            gi.Select();
            //
            propertyGrid.Refresh();
            //
            Highlight();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _scaleParameters = new ScaleParameters(_controller.Model.Properties.ModelSpace);
            propertyGrid.SelectedObject = _scaleParameters;
            _controller.ClearAllSelection();
        }


        // Methods                                                                                                                  
        public void PickedIds(int[] ids)
        {
            Vec3D point = null;
            FeMesh mesh = _controller.DisplayedMesh;
            //
            bool finished = false;
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_scaleParameters.ScaleCenterItemSet))
            {
                if (_scaleParameters.ScaleCenterSelectionMethod == PointSelectionMethodEnum.OnPoint &&
                    ids.Length == 1)
                {
                    point = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (_scaleParameters.ScaleCenterSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints &&
                         ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    point = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (_scaleParameters.ScaleCenterSelectionMethod == PointSelectionMethodEnum.CircleCenter &&
                         ids.Length == 3)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    Vec3D v3 = new Vec3D(mesh.Nodes[ids[2]].Coor);
                    Vec3D.GetCircle(v1, v2, v3, out double r, out point, out Vec3D axis);
                    finished = true;
                }
                //
                if (finished)
                {
                    _scaleParameters.CenterX = point.X;
                    _scaleParameters.CenterY = point.Y;
                    _scaleParameters.CenterZ = point.Z;
                }
            }
            //
            if (finished)
            {
                // Disable selection
                this.Enabled = true;
                _controller.SetSelectByToOff();
                _controller.Selection.SelectItem = vtkSelectItem.None;
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
                //
                _controller.ClearSelectionHistory();
                //
                Highlight();
            }
        }
        public void Highlight()
        {
            _scaleCenter[0] = _scaleParameters.CenterX;
            _scaleCenter[1] = _scaleParameters.CenterY;
            _scaleCenter[2] = _scaleParameters.CenterZ;
            //
            _controller.ClearAllSelection();
            if (_controller.CurrentView == ViewGeometryModelResults.Geometry)
                _controller.HighlightGeometryParts(_partNames);
            else if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.HighlightModelParts(_partNames, false, false);
            //
            if (_scaleParameters.FactorX != 0 && _scaleParameters.FactorY != 0 && _scaleParameters.FactorZ != 0)
            {
                _controller.HighlightNodes(new double[][] { _scaleCenter }, true);
                _controller.HighlightScaledEdges(_partNames, _scaleCenter, _scaleParameters.ScaleFactors, true);
            }
        }
    }
}
