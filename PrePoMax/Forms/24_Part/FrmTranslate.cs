using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeMesh;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;

namespace PrePoMax.Forms
{
    class FrmTranslate : UserControls.FrmProperties, IFormBase
    {
        // Variables                                                                                                                
        private TranslateParameters _translateParameters;
        private Controller _controller;
        private string[] _partNames;
        private double[] _startPoint;
        private double[] _endPoint;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        private double[][] _coorLinesToDraw;


        // Properties                                                                                                               
        public string[] PartNames { get { return _partNames; } set { _partNames = value; } }
        

        // Constructors                                                                                                             
        public FrmTranslate(Controller controller) 
            : base(1.7)
        {
            InitializeComponent();
            //
            _controller = controller;
            //
            _startPoint = new double[3];
            _endPoint = new double[3];
            //
            _coorLinesToDraw = new double[2][];
            _coorLinesToDraw[0] = new double[3];
            _coorLinesToDraw[1] = new double[3];
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
            this.gbProperties.Size = new System.Drawing.Size(310, 384);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 356);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 396);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 396);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 396);
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
            // FrmTranslate
            // 
            this.ClientSize = new System.Drawing.Size(334, 431);
            this.Name = "FrmTranslate";
            this.Text = "Translate Parameters";
            this.VisibleChanged += new System.EventHandler(this.FrmTranslate_VisibleChanged);
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.gbProperties.ResumeLayout(false);
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void FrmTranslate_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible) { }
            else
            {
                tsmiResetAll_Click(null, null);
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            HighlightNodes();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _translateParameters = (TranslateParameters)propertyGrid.SelectedObject;
            //
            _controller.TranslateModelPartsCommand(_partNames, _translateParameters.TranslateVector,
                                                   _translateParameters.NumberOfCopies);
            //
            HighlightNodes();
        }
        protected override bool OnPrepareForm(string stepName, string itemToEditName)
        {
            // Clear
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
            if (_translateParameters == null) tsmiResetAll_Click(null, null);   // first time only
            _translateParameters.ClearTranslation();    // called on OkAddNew
            // Disable selection
            _controller.SetSelectByToOff();
            // Get start point grid item
            GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                          item.PropertyDescriptor != null &&
                          item.PropertyDescriptor.Name == nameof(_translateParameters.Translate));
            // Select it
            gi.Select();
            //
            propertyGrid.Refresh();
            //
            HighlightNodes();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _translateParameters = new TranslateParameters(_controller.Model.Properties.ModelSpace);
            propertyGrid.SelectedObject = _translateParameters;
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
            if (propertyName == nameof(_translateParameters.StartPointItemSet))
            {
                if (_translateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.OnPoint &&
                    ids.Length == 1)
                {
                    point = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (_translateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints &&
                         ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    point = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (_translateParameters.StartPointSelectionMethod == PointSelectionMethodEnum.CircleCenter &&
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
                    _translateParameters.X1 = point.X;
                    _translateParameters.Y1 = point.Y;
                    _translateParameters.Z1 = point.Z;
                }
            }
            else if (propertyName == nameof(_translateParameters.EndPointItemSet))
            {
                if (_translateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.OnPoint &&
                    ids.Length == 1)
                {
                    point = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (_translateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.BetweenTwoPoints &&
                         ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    point = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (_translateParameters.EndPointSelectionMethod == PointSelectionMethodEnum.CircleCenter &&
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
                    _translateParameters.X2 = point.X;
                    _translateParameters.Y2 = point.Y;
                    _translateParameters.Z2 = point.Z;
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
                HighlightNodes();
            }
        }
        private void HighlightNodes()
        {
            _startPoint[0] = _translateParameters.X1;
            _startPoint[1] = _translateParameters.Y1;
            _startPoint[2] = _translateParameters.Z1;
            //
            _endPoint[0] = _translateParameters.X2;
            _endPoint[1] = _translateParameters.Y2;
            _endPoint[2] = _translateParameters.Z2;
            //
            double[] translateVector = _translateParameters.TranslateVector;
            _controller.ClearAllSelection();
            _controller.HighlightModelParts(_partNames, false, false);
            //
            if (translateVector[0] != 0 || translateVector[1] != 0 || translateVector[2] != 0)
            {
                _controller.HighlightLineWithArrow(_startPoint, _endPoint, false, true, true);
                _controller.HighlightTranslatedEdges(_partNames, translateVector, _translateParameters.NumberOfCopies, true);
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
