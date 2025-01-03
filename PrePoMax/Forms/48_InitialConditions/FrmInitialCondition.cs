using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using CaeMesh;
using PrePoMax.Settings;

namespace PrePoMax.Forms
{
    class FrmInitialCondition : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _initialConditionNames;
        private string _initialConditionToEditName;
        private ViewInitialCondition _viewInitialCondition;
        private Controller _controller;
        private Selection _selectionCopy;


        // Properties                                                                                                               
        public InitialCondition InitialCondition
        {
            get { return _viewInitialCondition != null ? _viewInitialCondition.GetBase() : null; }
            set
            {
                if (value is InitialTemperature it) _viewInitialCondition = new ViewInitialTemperature(it.DeepClone());
                else if (value is InitialTranslationalVelocity itv)
                    _viewInitialCondition = new ViewInitialTranslationalVelocity(itv.DeepClone());
                else if (value is InitialAngularVelocity iav)
                    _viewInitialCondition = new ViewInitialAngularVelocity(iav.DeepClone());
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmInitialCondition(Controller controller)
            : base(1.7)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewInitialCondition = null;
        }
        private void InitializeComponent()
        {
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbType
            // 
            this.gbType.Size = new System.Drawing.Size(310, 108);
            // 
            // lvTypes
            // 
            this.lvTypes.Size = new System.Drawing.Size(298, 80);
            // 
            // gbProperties
            // 
            this.gbProperties.Location = new System.Drawing.Point(12, 126);
            this.gbProperties.Size = new System.Drawing.Size(310, 314);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Size = new System.Drawing.Size(298, 286);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 446);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 446);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 446);
            // 
            // FrmInitialCondition
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 481);
            this.Name = "FrmInitialCondition";
            this.Text = "Edit Initial Condition";
            this.EnabledChanged += new System.EventHandler(this.FrmInitialCondition_EnabledChanged);
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers
        private void FrmInitialCondition_EnabledChanged(object sender, EventArgs e)
        {
            if (!Enabled)
            {
                _selectionCopy = _controller.Selection.DeepClone();
            }
            //
            ShowHideSelectionForm();
        }


        // Overrides                                                                                                                
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (Visible) ShowHideSelectionForm();   // accounts for minimizing/maximizing the main form
            //
            base.OnVisibleChanged(e);
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewError)  _viewInitialCondition = null;
                else if (itemTag is ViewInitialTemperature vit) _viewInitialCondition = vit;
                else if (itemTag is ViewInitialTranslationalVelocity vitv) _viewInitialCondition = vitv;
                else if (itemTag is ViewInitialAngularVelocity viav) _viewInitialCondition = viav;
                else throw new NotImplementedException();
                //
                ShowHideSelectionForm();
                //
                propertyGrid.SelectedObject = itemTag;
                //
                HighlightInitialCondition();
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewInitialCondition.RegionType))
            {
                ShowHideSelectionForm();
                //
                HighlightInitialCondition();
            }
            else if (_viewInitialCondition is ViewInitialTemperature vit &&
                     (property == nameof(vit.NodeSetName) ||
                      property == nameof(vit.SurfaceName)))
            {
                HighlightInitialCondition();
            }
            else if (_viewInitialCondition is ViewInitialTranslationalVelocity vitv &&
                     (property == nameof(vitv.NodeSetName) ||
                      property == nameof(vitv.SurfaceName) ||
                      property == nameof(vitv.ReferencePointName)))
            {
                HighlightInitialCondition();
            }
            else if (_viewInitialCondition is ViewInitialAngularVelocity viav &&
                     (property == nameof(vitv.NodeSetName) ||
                      property == nameof(vitv.SurfaceName) ||
                      property == nameof(vitv.ReferencePointName))||
                      property == nameof(viav.X) || property == nameof(viav.Y) || property == nameof(viav.Z))
            {
                HighlightInitialCondition();
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewInitialCondition = (ViewInitialCondition)propertyGrid.SelectedObject;
            //
            if (InitialCondition == null) throw new CaeException("No initial condition was selected.");
            //
            if (InitialCondition.RegionType == RegionTypeEnum.Selection &&
                (InitialCondition.CreationIds == null || InitialCondition.CreationIds.Length == 0))
                throw new CaeException("The initial condition selection must contain at least one item.");
            if (InitialCondition is InitialTranslationalVelocity itv)
            {
                if (itv.Magnitude.Value == 0)
                    throw new CaeException("At least one velocity component must not be equal to 0.");
            }
            if (InitialCondition is InitialAngularVelocity iav)
            {
                if (iav.N1.Value == 0 && iav.N2.Value == 0 && iav.N3.Value == 0)
                    throw new CaeException("At least one axis direction component must not be equal to 0.");
                if (iav.RotationalSpeed2 == 0)
                    throw new CaeException("Rotational speed must not be equal to 0.");
            }
            // Check if the name exists
            CheckName(_initialConditionToEditName, InitialCondition.Name, _initialConditionNames, "initial condition");
            // Create
            if (_initialConditionToEditName == null)
            {
                _controller.AddInitialConditionCommand(InitialCondition);
            }
            // Replace
            else if (_propertyItemChanged || !InitialCondition.Valid)
            {
                _controller.ReplaceInitialConditionCommand(_initialConditionToEditName, InitialCondition);
            }
            // Convert the initial condition from internal to show it
            else
            {
                InitialConditionInternal(false);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Convert the initial condition from internal to show it
            InitialConditionInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string initialConditionToEditName)
        {
            this.btnOkAddNew.Visible = initialConditionToEditName == null;
            //
            _propertyItemChanged = false;
            _stepName = null;
            _initialConditionNames = null;
            _initialConditionToEditName = null;
            _viewInitialCondition = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            //
            _stepName = stepName;
            _initialConditionNames = _controller.GetInitialConditionNames();
            _initialConditionToEditName = initialConditionToEditName;
            string[] nodeSetNames = _controller.GetUserNodeSetNames();
            string[] surfaceNames = _controller.GetUserSurfaceNames();
            string[] referencePointNames = _controller.GetModelReferencePointNames();
            //
            if (_initialConditionNames == null)
                throw new CaeException("The initial condition names must be defined first.");
            // Populate list view
            PopulateListOfInitialConditions(nodeSetNames, surfaceNames, referencePointNames);
            // Create new initial condition
            if (_initialConditionToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewInitialCondition = null;
                if (lvTypes.Items.Count == 1) _preselectIndex = 0;
                //
                HighlightInitialCondition(); // must be here if called from the menu
            }
            else
            // Edit existing initial condition
            {
                // Get and convert a converted initial condition back to selection
                InitialCondition = _controller.GetInitialCondition(_initialConditionToEditName); // to clone
                if (InitialCondition.CreationData != null)
                {
                    if (!_controller.Model.IsInitialConditionRegionValid(InitialCondition) || // do not use InitialCondition.Valid
                        !_controller.Model.RegionValid(InitialCondition))
                    {
                        // Region invalid
                        InitialCondition.CreationData = null;
                        InitialCondition.CreationIds = null;
                        _propertyItemChanged = true;
                    }
                    InitialCondition.RegionType = RegionTypeEnum.Selection;
                }
                // Convert the initial condition to internal to hide it
                InitialConditionInternal(true);
                //
                int selectedId;
                if (_viewInitialCondition is ViewInitialTemperature vit)
                {
                    selectedId = 0;
                    // Check for deleted entities
                    if (vit.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vit.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vit.NodeSetName, s => { vit.NodeSetName = s; });
                    else if (vit.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vit.SurfaceName, s => { vit.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vit.PopulateDropDownLists(nodeSetNames, surfaceNames);
                }
                else if (_viewInitialCondition is ViewInitialTranslationalVelocity vitv)
                {
                    selectedId = 1;
                    // Check for deleted entities
                    if (vitv.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vitv.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vitv.NodeSetName, s => { vitv.NodeSetName = s; });
                    else if (vitv.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vitv.SurfaceName, s => { vitv.SurfaceName = s; });
                    else if (vitv.RegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, vitv.ReferencePointName, 
                                             s => { vitv.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    vitv.PopulateDropDownLists(nodeSetNames, surfaceNames, referencePointNames);
                }
                else if (_viewInitialCondition is ViewInitialAngularVelocity viav)
                {
                    selectedId = 2;
                    // Check for deleted entities
                    if (viav.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (viav.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, viav.NodeSetName, s => { viav.NodeSetName = s; });
                    else if (viav.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, viav.SurfaceName, s => { viav.SurfaceName = s; });
                    else if (viav.RegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, viav.ReferencePointName,
                                             s => { viav.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    viav.PopulateDropDownLists(nodeSetNames, surfaceNames, referencePointNames);
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewInitialCondition;
                _preselectIndex = selectedId;
            }
            ShowHideSelectionForm();
            //
            return true;
        }


        // Methods                                                                                                                  
        private void PopulateListOfInitialConditions(string[] nodeSetNames, string[] surfaceNames, string[] referencePointNames)
        {
            ListViewItem item;
            System.Drawing.Color color = _controller.Settings.Pre.InitialConditionSymbolColor;
            bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
            // Initial temperature
            string name = "Temperature";
            item = new ListViewItem(name);
            InitialTemperature it = new InitialTemperature(GetInitialConditionName(name), "", RegionTypeEnum.Selection);
            ViewInitialTemperature vit = new ViewInitialTemperature(it);
            vit.PopulateDropDownLists(nodeSetNames, surfaceNames);
            vit.Color = color;
            item.Tag = vit;
            lvTypes.Items.Add(item);
            // Initial velocity
            name = "Velocity";
            item = new ListViewItem(name);
            InitialTranslationalVelocity itv = new InitialTranslationalVelocity(GetInitialConditionName(name), "",
                                                                                RegionTypeEnum.Selection, 0, 0, 0, twoD);
            ViewInitialTranslationalVelocity vitv = new ViewInitialTranslationalVelocity(itv);
            vitv.PopulateDropDownLists(nodeSetNames, surfaceNames, referencePointNames);
            vitv.Color = color;
            item.Tag = vitv;
            lvTypes.Items.Add(item);
            // Initial angular velocity
            name = "Angular Velocity";
            item = new ListViewItem(name);
            InitialAngularVelocity iav = new InitialAngularVelocity(GetInitialConditionName(name), "", RegionTypeEnum.Selection,
                                                                    twoD);
            ViewInitialAngularVelocity viav = new ViewInitialAngularVelocity(iav);
            viav.PopulateDropDownLists(nodeSetNames, surfaceNames, referencePointNames);
            viav.Color = color;
            item.Tag = viav;
            lvTypes.Items.Add(item);
        }
        private string GetInitialConditionName(string name)
        {
            if (name == null || name == "") name = "Initial Condition";
            name = name.Replace(' ', '_');
            name = _initialConditionNames.GetNextNumberedKey(name);
            //
            return name;
        }
        private void HighlightInitialCondition()
        {
            try
            {
                _controller.ClearSelectionHistory();
                //
                if (_viewInitialCondition == null) { }
                else if (InitialCondition is InitialTemperature || InitialCondition is InitialTranslationalVelocity ||
                         InitialCondition is InitialAngularVelocity)
                {
                    if (InitialCondition.RegionType == RegionTypeEnum.NodeSetName ||
                        InitialCondition.RegionType == RegionTypeEnum.SurfaceName ||
                        InitialCondition.RegionType == RegionTypeEnum.ReferencePointName)
                    {
                        _controller.Highlight3DObjects(new object[] { InitialCondition.RegionName });
                    }
                    else if (InitialCondition.RegionType == RegionTypeEnum.Selection)
                    {
                        SetSelectItem();
                        //
                        if (InitialCondition.CreationData != null)
                        {
                            _controller.Selection = InitialCondition.CreationData.DeepClone();
                            _controller.HighlightSelection();
                        }
                    }
                    else throw new NotImplementedException();
                    // Secondary selection
                    if (InitialCondition is InitialAngularVelocity iav)
                    {
                        double[][] nodeCoor = new double[][] { new double[] { iav.X.Value, iav.Y.Value, iav.Z.Value } };
                        _controller.HighlightNodes(nodeCoor, true);
                    }

                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void ShowHideSelectionForm()
        {
            if (InitialCondition != null && InitialCondition.RegionType == RegionTypeEnum.Selection && Enabled)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (InitialCondition != null && InitialCondition.RegionType == RegionTypeEnum.Selection)
            {
                if (InitialCondition is null) { }
                else if (InitialCondition is InitialTemperature) _controller.SetSelectItemToGeometry();
                else if (InitialCondition is InitialTranslationalVelocity) _controller.SetSelectItemToGeometry();
                else if (InitialCondition is InitialAngularVelocity) _controller.SetSelectItemToGeometry();
            }
            else _controller.SetSelectByToOff();
        }
        private void InitialConditionInternal(bool toInternal)
        {
            if (_initialConditionToEditName != null)
            {
                // Convert the initial condition from/to internal to hide/show it
                _controller.GetInitialCondition(_initialConditionToEditName).Internal = toInternal;
                _controller.FeModelUpdate(UpdateType.RedrawSymbols);
            }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (Enabled)
            {
                if (InitialCondition != null && InitialCondition.RegionType == RegionTypeEnum.Selection)
                {
                    if (InitialCondition is InitialTemperature || InitialCondition is InitialTranslationalVelocity ||
                        InitialCondition is InitialAngularVelocity)
                    {
                        InitialCondition.CreationIds = ids;
                        InitialCondition.CreationData = _controller.Selection.DeepClone();
                        //
                        propertyGrid.Refresh();
                        //
                        _propertyItemChanged = true;
                        //
                        Highlight();
                    }
                    else throw new NotSupportedException();
                }
            }
            else
            {
                if (ids != null && ids.Length > 0)
                {
                    bool changed = false;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (_viewInitialCondition is ViewInitialAngularVelocity viav)
                    {
                        if (property == nameof(viav.CenterPointItemSet))
                        {
                            if (ids.Length == 1)
                            {
                                FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                                viav.X = new EquationString(node.X.ToString());
                                viav.Y = new EquationString(node.Y.ToString());
                                viav.Z = new EquationString(node.Z.ToString());
                                changed = true;
                            }
                        }
                    }
                    //
                    if (changed)
                    {
                        Enabled = true; // must be first for the selection to work
                        //
                        propertyGrid.Refresh();
                        //
                        _propertyItemChanged = true;
                        //
                        _controller.Selection = _selectionCopy;
                        Highlight();
                    }
                }
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightInitialCondition();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            InitialCondition initialCondition = InitialCondition;
            //
            if (initialCondition.CreationData != null) return initialCondition.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            InitialCondition initialCondition = InitialCondition;
            //
            if (initialCondition.CreationData != null && IsSelectionGeometryBased())
                return initialCondition.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
