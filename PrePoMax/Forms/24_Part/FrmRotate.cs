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
    class FrmRotate : UserControls.FrmProperties, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private RotateParameters _rotateParameters;
        private Controller _controller;
        private string[] _partNames;
        private double[] _startPoint;
        private double[] _endPoint;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;


        // Properties                                                                                                               
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }


        // Constructors                                                                                                             
        public FrmRotate(Controller controller) 
            : base(1.7)
        {
            InitializeComponent();
            //
            _controller = controller;
            //
            _startPoint = new double[3];
            _endPoint = new double[3];
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
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(310, 399);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 371);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 411);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 411);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 411);
            // 
            // cmsProperyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsPropertyGrid.Name = "cmsProperyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(118, 26);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(117, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmRotate
            // 
            this.ClientSize = new System.Drawing.Size(334, 446);
            this.Name = "FrmRotate";
            this.Text = "Rotate Parameters";
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
            double angle = _rotateParameters.AngleDeg * Math.PI / 180;
            _controller.RotateModelPartsCommand(_partNames, _rotateParameters.RotateCenter, _rotateParameters.RotateAxis,
                                                angle, _rotateParameters.NumberOfCopies);
            //
            Highlight();
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            // Clear
            tsmiResetAll_Click(null, null);
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            _rotateParameters.Clear();
            // Disable selection
            _controller.SetSelectByToOff();
            // Get start point grid item
            GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                          item.PropertyDescriptor != null &&
                          item.PropertyDescriptor.Name == nameof(_rotateParameters.Rotate));
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
            _rotateParameters = new RotateParameters(_controller.Model.Properties.ModelSpace);
            propertyGrid.SelectedObject = _rotateParameters;
            _controller.ClearAllSelection();
        }


        // Methods                                                                                                                  
        public void PickedIds(int[] ids)
        {
            Vec3D point = null;
            FeMesh mesh = GetMesh();
            //
            bool finished = false;
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_rotateParameters.StartPointItemSet))
            {
                if (_rotateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.OnPoint &&
                    ids.Length == 1)
                {
                    point = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (_rotateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints &&
                         ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    point = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (_rotateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.CircleCenter &&
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
                    _rotateParameters.X1 = point.X;
                    _rotateParameters.Y1 = point.Y;
                    _rotateParameters.Z1 = point.Z;
                }
            }
            else if (propertyName == nameof(_rotateParameters.EndPointItemSet))
            {
                if (_rotateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.OnPoint &&
                    ids.Length == 1)
                {
                    point = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (_rotateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints &&
                         ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    point = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (_rotateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.CircleCenter &&
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
                    _rotateParameters.X2 = point.X;
                    _rotateParameters.Y2 = point.Y;
                    _rotateParameters.Z2 = point.Z;
                }
            }
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
            _startPoint[0] = _rotateParameters.X1;
            _startPoint[1] = _rotateParameters.Y1;
            _startPoint[2] = _rotateParameters.Z1;
            //
            _endPoint[0] = _rotateParameters.X2;
            _endPoint[1] = _rotateParameters.Y2;
            _endPoint[2] = _rotateParameters.Z2;
            //
            _controller.ClearAllSelection();
            _controller.HighlightModelParts(_partNames, false, false);
            //
            double[] rotateAxis = _rotateParameters.RotateAxis;
            if (rotateAxis[0] != 0 || rotateAxis[1] != 0 || rotateAxis[2] != 0)
            {
                _controller.HighlightLineWithArrow(_startPoint, _endPoint, false, true, true);
                _controller.HighlightRotatedEdges(_partNames, _rotateParameters.RotateCenter, _rotateParameters.RotateAxis,
                                                  _rotateParameters.AngleRad, _rotateParameters.NumberOfCopies, true);
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
    }
}
