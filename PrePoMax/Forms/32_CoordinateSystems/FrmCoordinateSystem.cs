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
using System.Reflection.Emit;

namespace PrePoMax.Forms
{
    class FrmCoordinateSystem : FrmProperties, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private HashSet<string> _coordinateSystemNames;
        private string _coordinateSystemToEditName;
        private string _currSelectionFormDataName;
        private ViewCoordinateSystem _viewCoordinateSystem;
        private Controller _controller;
        //
        private System.ComponentModel.IContainer components;
        private ContextMenuStrip cmsPropertyGrid;
        private ToolStripMenuItem tsmiResetAll;


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
            this.gbProperties.Size = new System.Drawing.Size(310, 551);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 523);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 563);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 563);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 563);
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
            this.ClientSize = new System.Drawing.Size(334, 598);
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
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewCoordinateSystem.CenterCreatedFrom) ||
                property == nameof(_viewCoordinateSystem.PointXCreatedFrom) ||
                property == nameof(_viewCoordinateSystem.PointXYCreatedFrom))
            {
                SetSelectItem();
                //
                UpdateCoordinateSystem(CoordinateSystem);
            }
            HighlightCoordinateSystem();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            SetSelectItem();
            //
            HighlightCoordinateSystem();
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            _viewCoordinateSystem = (ViewCoordinateSystem)propertyGrid.SelectedObject;
            // Check name
            CheckName(_coordinateSystemToEditName, _viewCoordinateSystem.Name, _coordinateSystemNames, "coordinate system");
            //
            CoordinateSystem cs = CoordinateSystem;
            // Check equations
            cs.CheckEquations();
            // Check point selection
            string error;
            if (!cs.IsProperlyDefined(out error)) throw new CaeException(error);
            // Create
            if (_coordinateSystemToEditName == null)
            {
                AddCoordinateSystemCommand(cs);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                 ReplaceCoordinateSystemCommand(_coordinateSystemToEditName, cs);
                _coordinateSystemToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the coordinate system from internal to show it
            else
            {
                CoordinateSystemInternal(false);
            }
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Reset the maximum number of selected items
            _controller.Selection.MaxNumberOfItemIds = -1;
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
            _currSelectionFormDataName = null;
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
            SetSelectItem();
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
        public void SelectionChanged(int[] ids)
        {
            if (_controller.Selection.SelectItem == vtkSelectItem.None) return;
            //
            CoordinateSystem cs = CoordinateSystem;
            //
            if (IsCenterPropertySelectedForSelection())
            {
                cs.CenterCreationIds = ids;
                cs.CenterCreationData = _controller.Selection.DeepClone();
                //
                UpdateCoordinateSystem(CoordinateSystem);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
            else if (IsPointXPropertySelectedForSelection())
            {
                cs.PointXCreationIds = ids;
                cs.PointXCreationData = _controller.Selection.DeepClone();
                //
                UpdateCoordinateSystem(CoordinateSystem);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
            else if (IsPointXYPropertySelectedForSelection())
            {
                cs.PointXYCreationIds = ids;
                cs.PointXYCreationData = _controller.Selection.DeepClone();
                //
                UpdateCoordinateSystem(CoordinateSystem);
                //
                propertyGrid.Refresh();
                //
                _propertyItemChanged = true;
            }
            // Reset to null
            if (cs.CenterCreatedFrom == CsPointCreatedFromEnum.Coordinates)
            {
                cs.CenterCreationIds = null;
                cs.CenterCreationData = null;
            }
            if (cs.PointXCreatedFrom == CsPointCreatedFromEnum.Coordinates)
            {
                cs.PointXCreationIds = null;
                cs.PointXCreationData = null;
            }
            if (cs.PointXYCreatedFrom == CsPointCreatedFromEnum.Coordinates)
            {
                cs.PointXYCreationIds = null;
                cs.PointXYCreationData = null;
            }
            //
            HighlightCoordinateSystem();
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
                //
                double[][] coor = null;
                CoordinateSystem cs = CoordinateSystem;
                // Draw selection nodes
                if (cs != null)
                {
                    if (IsCenterPropertySelectedForSelection())
                    {
                        if (cs.CenterCreationData != null)
                        {
                            _controller.Selection = cs.CenterCreationData.DeepClone();
                            _controller.HighlightSelection(false, true, true);
                        }
                        coor = new double[][] { cs.Center().Coor };
                    }
                    else if (IsPointXPropertySelectedForSelection())
                    {
                        if (cs.PointXCreationData != null)
                        {
                            _controller.Selection = cs.PointXCreationData.DeepClone();
                            _controller.HighlightSelection(false, true, true);
                        }
                        coor = new double[][] { cs.PointX() };
                    }
                    else if (IsPointXYPropertySelectedForSelection())
                    {
                        if (cs.PointXYCreationData != null)
                        {
                            _controller.Selection = cs.PointXYCreationData.DeepClone();
                            _controller.HighlightSelection(false, true, true);
                        }
                        coor = new double[][] { cs.PointXY() };
                    }
                }
                // Draw point
                if (coor != null)
                {
                    _controller.DrawNodes(cs.Name + Globals.NameSeparator + "Point", coor, Color.Black,
                                          vtkControl.vtkRendererLayer.Selection);
                }
                // Draw coordinate system
                _controller.HighlightCoordinateSystem(cs);
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (IsCenterPropertySelectedForSelection())
            {
                if (_currSelectionFormDataName != "Center")
                {
                    _currSelectionFormDataName = "Center";
                    if (ItemSetDataEditor.SelectionForm.Visible) ItemSetDataEditor.SelectionForm.ResetSelection(false);
                    else ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                }
            }
            else if (IsPointXPropertySelectedForSelection())
            {
                if (_currSelectionFormDataName != "PointX")
                {
                    _currSelectionFormDataName = "PointX";
                    if (ItemSetDataEditor.SelectionForm.Visible) ItemSetDataEditor.SelectionForm.ResetSelection(false);
                    else ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                }
            }
            else if (IsPointXYPropertySelectedForSelection())
            {
                if (_currSelectionFormDataName != "PointXY")
                {
                    _currSelectionFormDataName = "PointXY";
                    if (ItemSetDataEditor.SelectionForm.Visible) ItemSetDataEditor.SelectionForm.ResetSelection(false);
                    else ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                }
            }
            else
            {
                _currSelectionFormDataName = null;
                ItemSetDataEditor.SelectionForm.Hide();
            }
        }
        private void SetSelectItem()
        {
            ShowHideSelectionForm(); // must be here before MaxNumberOfItemIds
            //
            if (IsCenterPropertySelectedForSelection())
            {
                _controller.SetSelectItemToNode();
                // Limit the number of selected nodes
                _controller.Selection.MaxNumberOfItemIds = GetMaxNumberOfItemIds(CoordinateSystem.CenterCreatedFrom);
            }
            else if (IsPointXPropertySelectedForSelection())
            {
                _controller.SetSelectItemToNode();
                // Limit the number of selected nodes
                _controller.Selection.MaxNumberOfItemIds = GetMaxNumberOfItemIds(CoordinateSystem.PointXCreatedFrom);
            }
            else if (IsPointXYPropertySelectedForSelection())
            {
                _controller.SetSelectItemToNode();
                // Limit the number of selected nodes
                _controller.Selection.MaxNumberOfItemIds = GetMaxNumberOfItemIds(CoordinateSystem.PointXYCreatedFrom);
            }
            else
            {
                _controller.SetSelectByToOff();
                _controller.Selection.MaxNumberOfItemIds = -1;
            }
        }
        private bool IsCenterPropertySelectedForSelection()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return false;
            //
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_viewCoordinateSystem.CenterCreatedFrom) && 
                IsCreatedFromSelection(_viewCoordinateSystem.CenterCreatedFrom))
                return true;
            else return false;
        }
        private bool IsPointXPropertySelectedForSelection()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return false;
            //
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_viewCoordinateSystem.PointXCreatedFrom) &&
                IsCreatedFromSelection(_viewCoordinateSystem.PointXCreatedFrom))
                return true;
            else return false;
        }
        private bool IsPointXYPropertySelectedForSelection()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return false;
            //
            string propertyName = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            if (propertyName == nameof(_viewCoordinateSystem.PointXYCreatedFrom) &&
                IsCreatedFromSelection(_viewCoordinateSystem.PointXYCreatedFrom))
                return true;
            else return false;
        }
        private int GetMaxNumberOfItemIds(CsPointCreatedFromEnum csPointCreatedFrom)
        {
            if (csPointCreatedFrom == CsPointCreatedFromEnum.OnPoint) return 1;
            else if (csPointCreatedFrom == CsPointCreatedFromEnum.BetweenTwoPoints) return 2;
            else if (csPointCreatedFrom == CsPointCreatedFromEnum.CircleCenter) return 3;
            else return -1;
        }
        private bool IsCreatedFromSelection(CsPointCreatedFromEnum csPointCreatedFrom)
        {
            if (csPointCreatedFrom == CsPointCreatedFromEnum.OnPoint ||
                csPointCreatedFrom == CsPointCreatedFromEnum.BetweenTwoPoints ||
                csPointCreatedFrom == CsPointCreatedFromEnum.CircleCenter)
                return true;
            else return false;
        }
        private void SetNumberOfSelectionItemIds(CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem == null) return;
            //
            if (coordinateSystem.CenterCreationData != null)
                coordinateSystem.CenterCreationData.MaxNumberOfItemIds = GetMaxNumberOfItemIds(coordinateSystem.CenterCreatedFrom);
            if (coordinateSystem.PointXCreationData != null)
                coordinateSystem.PointXCreationData.MaxNumberOfItemIds = GetMaxNumberOfItemIds(coordinateSystem.PointXCreatedFrom);
            if (coordinateSystem.PointXYCreationData != null)
                coordinateSystem.PointXYCreationData.MaxNumberOfItemIds = GetMaxNumberOfItemIds(coordinateSystem.PointXYCreatedFrom);
        }
        private void ResetNumberOfSelectionItemIds(CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem.CenterCreationData != null) coordinateSystem.CenterCreationData.MaxNumberOfItemIds = -1;
            if (coordinateSystem.PointXCreationData != null) coordinateSystem.PointXCreationData.MaxNumberOfItemIds = -1;
            if (coordinateSystem.PointXYCreationData != null) coordinateSystem.PointXYCreationData.MaxNumberOfItemIds = -1;
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
        private string[] GetCoordinateSystemNames()
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                return _controller.GetModelCoordinateSystemNames();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                return _controller.GetResultCoordinateSystemNames();
            else throw new NotSupportedException();
        }
        private void UpdateCoordinateSystem(CoordinateSystem coordinateSystem)
        {
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                _controller.UpdateModelCoordinateSystem(coordinateSystem);
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                _controller.UpdateResultCoordinateSystem(coordinateSystem);
            else throw new NotSupportedException();
        }
        private void AddCoordinateSystemCommand(CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem != null)
            {
                // Reset the max number of items for regenerate - the next selection should not be limited
                ResetNumberOfSelectionItemIds(coordinateSystem);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                    _controller.AddModelCoordinateSystemCommand(coordinateSystem);
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    _controller.AddResultCoordinateSystemCommand(coordinateSystem);
                else throw new NotSupportedException();
            }
        }
        private CoordinateSystem GetCoordinateSystem(string coordinateSystemToEditName)
        {
            CoordinateSystem coordinateSystem;
            if (_controller.CurrentView == ViewGeometryModelResults.Model)
                coordinateSystem = _controller.GetModelCoordinateSystem(coordinateSystemToEditName).DeepClone();
            else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                coordinateSystem = _controller.GetResultCoordinateSystem(coordinateSystemToEditName).DeepClone();
            else throw new NotSupportedException();
            //
            SetNumberOfSelectionItemIds(coordinateSystem);
            //
            return coordinateSystem;
        }
        private void ReplaceCoordinateSystemCommand(string coordinateSystemToEditName, CoordinateSystem coordinateSystem)
        {
            if (coordinateSystem != null)
            {
                // Reset the max number of items for regenerate - the next selection should not be limited
                ResetNumberOfSelectionItemIds(coordinateSystem);
                //
                if (_controller.CurrentView == ViewGeometryModelResults.Model)
                    _controller.ReplaceModelCoordinateSystemCommand(coordinateSystemToEditName, coordinateSystem);
                else if (_controller.CurrentView == ViewGeometryModelResults.Results)
                    _controller.ReplaceResultCoordinateSystemCommand(coordinateSystemToEditName, coordinateSystem);
                else throw new NotSupportedException();
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightCoordinateSystem();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            CoordinateSystem cs = CoordinateSystem;
            //
            if (cs != null)
            {
                if (IsCenterPropertySelectedForSelection())
                {
                    if (cs.CenterCreationData != null) return cs.CenterCreationData.IsGeometryBased(); 
                    else return true;
                }
                else if (IsPointXPropertySelectedForSelection())
                {
                    if (cs.PointXCreationData != null) return cs.PointXCreationData.IsGeometryBased();
                    else return true;
                }
                else if (IsPointXYPropertySelectedForSelection())
                {
                    if (cs.PointXYCreationData != null) return cs.PointXYCreationData.IsGeometryBased();
                    else return true;
                }
            }
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            CoordinateSystem cs = CoordinateSystem;
            //
            if (cs != null && IsSelectionGeometryBased())
            {
                if (IsCenterPropertySelectedForSelection())
                {
                    if (cs.CenterCreationData != null) return cs.CenterCreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (IsPointXPropertySelectedForSelection())
                {
                    if (cs.PointXCreationData != null) return cs.PointXCreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (IsPointXYPropertySelectedForSelection())
                {
                    if (cs.PointXYCreationData != null) return cs.PointXYCreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
            }
            return defaultMode;
        }

    }
}
