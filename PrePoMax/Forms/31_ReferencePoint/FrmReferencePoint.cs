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
using System.Xml.Linq;

namespace PrePoMax.Forms
{
    class FrmReferencePoint : FrmProperties, IFormBase, IFormHighlight
    {
        // Variables                                                                                                                
        private HashSet<string> _referencePointNames;
        private string _referencePointToEditName;
        private ViewFeReferencePoint _viewReferencePoint;
        private Controller _controller;
        private double[][] _coorNodesToDraw;
        private string[] _nodeSetNames;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiResetAll;
        private string[] _surfaceNames;


        // Properties                                                                                                               
        public FeReferencePoint ReferencePoint
        {
            get { return _viewReferencePoint.GetBase(); }
            set { _viewReferencePoint = new ViewFeReferencePoint(value.DeepClone()); }
        }
       

        // Constructors                                                                                                             
        public FrmReferencePoint(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewReferencePoint = null;
            _referencePointNames = new HashSet<string>();
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
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
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
            // FrmReferencePoint
            // 
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Name = "FrmReferencePoint";
            this.Text = "Edit Reference Point";
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
            if (property == nameof(_viewReferencePoint.CreatedFrom))
            {
                SetSelectItemAndSelection();
                //
                UpdateReferencePoint(ReferencePoint);
            }
            else if (property == nameof(_viewReferencePoint.RegionType) || property == nameof(_viewReferencePoint.NodeSetName) ||
                     property == nameof(_viewReferencePoint.SurfaceName))
            {
                UpdateReferencePoint(ReferencePoint);
            }
            //
            HighlightReferencePoint();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            object value = propertyGrid.SelectedGridItem.Value;
            if (value != null)
            {
                string valueString = value.ToString();
                object[] objects = null;
                //
                if (propertyGrid.SelectedObject == null)
                { }
                else if (propertyGrid.SelectedObject is ViewFeReferencePoint)
                {
                    ViewFeReferencePoint vrp = propertyGrid.SelectedObject as ViewFeReferencePoint;
                    if (valueString == vrp.NodeSetName) objects = new object[] { vrp.NodeSetName };
                    else if (valueString == vrp.SurfaceName) objects = new object[] { vrp.SurfaceName };
                    else objects = null;
                }
                else throw new NotImplementedException();
                //
                _controller.Highlight3DObjects(objects);
            }
            HighlightReferencePoint();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewReferencePoint = (ViewFeReferencePoint)propertyGrid.SelectedObject;
            // Check name
            CheckName(_referencePointToEditName, _viewReferencePoint.Name, _referencePointNames, "reference point");
            // Check equations
            _viewReferencePoint.GetBase().CheckEquations();
            // Create
            if (_referencePointToEditName == null)
            {
                AddReferencePointCommand(ReferencePoint);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                ReplaceReferencePointCommand(_referencePointToEditName, ReferencePoint);
                //
                _referencePointToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the reference point from internal to show it
            else
            {
                ReferencePointInternal(false);
            }
            // If all is successful turn off the selection
            TurnOffSelection();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            TurnOffSelection();
            // Convert the reference point from internal to show it
            ReferencePointInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string referencePointToEditName)
        {
            this.btnOkAddNew.Visible = referencePointToEditName == null;
            //
            _propertyItemChanged = false;
            _referencePointNames.Clear();
            _referencePointToEditName = null;
            _viewReferencePoint = null;
            //
            _referencePointNames.UnionWith(GetReferencePointNames());
            _referencePointToEditName = referencePointToEditName;
            //
            _nodeSetNames = _controller.GetUserNodeSetNames();
            _surfaceNames = _controller.GetUserSurfaceNames();
            // Create new reference point
            if (_referencePointToEditName == null)
            {
                if (_controller.Model.Properties.ModelSpace.IsTwoD())
                    ReferencePoint = new FeReferencePoint(GetReferencePointName(), 0, 0);
                else  ReferencePoint = new FeReferencePoint(GetReferencePointName(), 0, 0, 0);
                //
                ReferencePoint.Color = _controller.Settings.Pre.ConstraintSymbolColor;
            }
            // Edit existing reference point
            else
            {
                ReferencePoint = GetReferencePoint(_referencePointToEditName); // to clone
                // Convert the reference point to internal to hide it
                ReferencePointInternal(true);
                // Check for deleted regions
                if (ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.BoundingBoxCenter ||
                    ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.CenterOfGravity)
                {
                    ViewFeReferencePoint vrp = _viewReferencePoint; // shorten
                    if (vrp.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref _nodeSetNames, vrp.NodeSetName, s => { vrp.NodeSetName = s; });
                    else if (vrp.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref _surfaceNames, vrp.SurfaceName, s => { vrp.SurfaceName = s; });
                    else throw new NotSupportedException();
                }
                // CheckMissingValue changes _propertyItemChanged -> update coordinates
                if (_propertyItemChanged)
                {
                    UpdateReferencePoint(ReferencePoint);   // to clone
                }
            }
            //
            _viewReferencePoint.PopulateDropDownLists(_nodeSetNames, _surfaceNames);
            //
            propertyGrid.SelectedObject = _viewReferencePoint;
            propertyGrid.Select();
            //
            SetSelectItemAndSelection();
            //
            HighlightReferencePoint();
            //
            return true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            _viewReferencePoint.GetBase().Reset();
            _propertyItemChanged = true;
            propertyGrid.Refresh();
            HighlightReferencePoint();
        }

        // Methods                                                                                                                  
        public void PickedIds(int[] ids)
        {
            Vec3D center = null;
            FeMesh mesh = GetMesh();
            //
            bool finished = false;
            FeReferencePoint rp = ReferencePoint;
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_viewReferencePoint.PointCenterItemSet))
            {
                if (rp.CreatedFrom == FeReferencePointCreatedFrom.OnPoint && ids.Length == 1)
                {
                    center = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    finished = true;
                }
                else if (rp.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints && ids.Length == 2)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    center = (v1 + v2) * 0.5;
                    finished = true;
                }
                else if (rp.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter && ids.Length == 3)
                {
                    Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
                    Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
                    Vec3D v3 = new Vec3D(mesh.Nodes[ids[2]].Coor);
                    Vec3D.GetCircle(v1, v2, v3, out double r, out center, out Vec3D axis);
                    finished = true;
                }
                //
                if (finished)
                {
                    rp.X.SetEquationFromValue(center.X, true);
                    rp.Y.SetEquationFromValue(center.Y, true);
                    rp.Z.SetEquationFromValue(center.Z, true);
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
                _controller.ClearSelectionHistory();    // must be here to reset the number of picked ids
                //
                HighlightReferencePoint();
            }












            //if (ids != null)
            //{
            //    FeReferencePoint rp = ReferencePoint;
            //    FeMesh mesh = GetMesh();
            //    //
            //    bool clear = false;
            //    bool finished = false;
            //    if (rp.CreatedFrom == FeReferencePointCreatedFrom.OnPoint)
            //    {
            //        if (ids.Length == 0) clear = true;
            //        else if (ids.Length == 1)
            //        {
            //            FeNode node = mesh.Nodes[ids[0]];
            //            _viewReferencePoint.X = new EquationString(node.X.ToString());
            //            _viewReferencePoint.Y = new EquationString(node.Y.ToString());
            //            _viewReferencePoint.Z = new EquationString(node.Z.ToString());
            //            finished = true;
            //        }
            //        else clear = true;
            //    }
            //    else if (rp.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints)
            //    {
            //        if (ids.Length == 0) clear = true;
            //        else if (ids.Length == 1) { }
            //        else if (ids.Length == 2)
            //        {
            //            FeNode node1 = mesh.Nodes[ids[0]];
            //            FeNode node2 = mesh.Nodes[ids[1]];
            //            _viewReferencePoint.X = new EquationString(((node1.X + node2.X) / 2).ToString());
            //            _viewReferencePoint.Y = new EquationString(((node1.Y + node2.Y) / 2).ToString());
            //            _viewReferencePoint.Z = new EquationString(((node1.Z + node2.Z) / 2).ToString());
            //            finished = true;
            //        }
            //        else clear = true;
            //    }
            //    else if (rp.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
            //    {
            //        if (ids.Length == 0) clear = true;
            //        else if (ids.Length == 1) { }
            //        else if (ids.Length == 2) { }
            //        else if (ids.Length == 3)
            //        {
            //            Vec3D v1 = new Vec3D(mesh.Nodes[ids[0]].Coor);
            //            Vec3D v2 = new Vec3D(mesh.Nodes[ids[1]].Coor);
            //            Vec3D v3 = new Vec3D(mesh.Nodes[ids[2]].Coor);
            //            Vec3D.GetCircle(v1, v2, v3, out double r, out Vec3D center, out Vec3D axis);
            //            _viewReferencePoint.X = new EquationString(center.X.ToString());
            //            _viewReferencePoint.Y = new EquationString(center.Y.ToString());
            //            _viewReferencePoint.Z = new EquationString(center.Z.ToString());
            //            finished = true;
            //        }
            //        else clear = true;
            //    }
            //    else clear = true;
            //    //
            //    if (finished)
            //    {
            //        propertyGrid.Refresh();
            //        //
            //        _propertyItemChanged = true;
            //    }
            //    //
            //    if (finished || clear) _controller.ClearSelectionHistory();    // resets the number of picked ids
            //    if (finished || clear) HighlightReferencePoint();
            //}
        }
        private string GetReferencePointName()
        {
            return _referencePointNames.GetNextNumberedKey("Reference_Point");
        }
        private void HighlightReferencePoint()
        {
            try
            {
                _coorNodesToDraw[0] = _viewReferencePoint.GetBase().Coor();
                //
                _controller.HighlightNodes(_coorNodesToDraw);
            }
            catch { }
        }
        private void TurnOffSelection()
        {
            _controller.SetSelectByToOff();
            _controller.Selection.SelectItem = vtkSelectItem.None;
        }
        private void SetSelectItemAndSelection()
        {
            //if (ReferencePoint is null) { }
            //else if (ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.OnPoint ||
            //         ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.BetweenTwoPoints ||
            //         ReferencePoint.CreatedFrom == FeReferencePointCreatedFrom.CircleCenter)
            //{
            //    _controller.SelectBy = vtkSelectBy.Node; // this disables the selection with area ...
            //    _controller.SetSelectItemToNode();
            //}
            //else
            //{
            //    TurnOffSelection();
            //}

            TurnOffSelection();
            _controller.ClearSelectionHistoryAndCallSelectionChanged();
        }
        private void ReferencePointInternal(bool toInternal)
        {
            // Convert the reference point from/to internal to hide/show it
            if (_referencePointToEditName != null)
            {
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                {
                    _controller.GetModelReferencePoint(_referencePointToEditName).Internal = toInternal;
                    _controller.FeModelUpdate(UpdateType.RedrawSymbols);
                }
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                {
                    _controller.GetResultReferencePoint(_referencePointToEditName).Internal = toInternal;
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
        private string[] GetReferencePointNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetAllMeshEntityNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultReferencePointNames();
            else throw new NotSupportedException();
        }
        private void UpdateReferencePoint(FeReferencePoint referencePoint)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.UpdateModelReferencePoint(referencePoint);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.UpdateResultReferencePoint(referencePoint);
            else throw new NotSupportedException();
        }
        private void AddReferencePointCommand(FeReferencePoint referencePoint)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.AddModelReferencePointCommand(referencePoint);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.AddResultReferencePointCommand(referencePoint);
            else throw new NotSupportedException();
        }
        private FeReferencePoint GetReferencePoint(string referencePointToEditName)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetModelReferencePoint(referencePointToEditName);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultReferencePoint(referencePointToEditName);
            else throw new NotSupportedException();
        }
        private void ReplaceReferencePointCommand(string referencePointToEditName, FeReferencePoint referencePoint)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.ReplaceModelReferencePointCommand(referencePointToEditName, referencePoint);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.ReplaceResultReferencePointCommand(referencePointToEditName, referencePoint);
            else throw new NotSupportedException();
        }
        // IFormHighlight
        public void Highlight()
        {
            HighlightReferencePoint();
        }

        
    }
}
