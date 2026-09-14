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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using CaeMesh;

namespace PrePoMax.Forms
{
    class FrmConstraint : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _constraintNames;
        private string _constraintToEditName;
        private ViewConstraint _viewConstraint;
        private ContextMenuStrip cmsPropertyGrid;
        private System.ComponentModel.IContainer components;
        private ToolStripMenuItem tsmiSwapMasterSlave;
        private string _prevSelectionFormProperty;
        private Controller _controller;


        // Properties                                                                                                               
        public Constraint Constraint
        {
            get { return _viewConstraint != null ? _viewConstraint.GetBase() : null; }
            set
            {
                if (value is PointSpring ps) _viewConstraint = new ViewPointSpring(ps.DeepClone());
                else if (value is SurfaceSpring || value is CompressionOnly)
                    _viewConstraint = new ViewSurfaceSupport(value.DeepClone());
                else if (value is RigidBody rb) _viewConstraint = new ViewRigidBody(rb.DeepClone());
                else if (value is Coupling coupling) _viewConstraint = new ViewCoupling(coupling.DeepClone());
                else if (value is CyclicSymmetry cs) _viewConstraint = new ViewCyclicSymmetry(cs.DeepClone());
                else if (value is Tie tie) _viewConstraint = new ViewTie(tie.DeepClone());
                else if (value is Joint joint) _viewConstraint = new ViewJoint(joint.DeepClone());
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmConstraint(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewConstraint = null;
        }
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiSwapMasterSlave = new System.Windows.Forms.ToolStripMenuItem();
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbProperties
            // 
            this.gbProperties.Size = new System.Drawing.Size(310, 353);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.Size = new System.Drawing.Size(298, 325);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(160, 476);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(241, 476);
            // 
            // btnOkAddNew
            // 
            this.btnOkAddNew.Location = new System.Drawing.Point(79, 476);
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiSwapMasterSlave});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(174, 26);
            // 
            // tsmiSwapMasterSlave
            // 
            this.tsmiSwapMasterSlave.Name = "tsmiSwapMasterSlave";
            this.tsmiSwapMasterSlave.Size = new System.Drawing.Size(173, 22);
            this.tsmiSwapMasterSlave.Text = "Swap Master/Slave";
            this.tsmiSwapMasterSlave.Click += new System.EventHandler(this.tsmiSwapMasterSlave_Click);
            // 
            // FrmConstraint
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 511);
            this.Name = "FrmConstraint";
            this.Text = "Edit Constraint";
            this.Controls.SetChildIndex(this.gbProperties, 0);
            this.Controls.SetChildIndex(this.btnCancel, 0);
            this.Controls.SetChildIndex(this.btnOK, 0);
            this.Controls.SetChildIndex(this.btnOkAddNew, 0);
            this.Controls.SetChildIndex(this.gbType, 0);
            this.gbType.ResumeLayout(false);
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
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            // Deactivate selection limits
            _controller.Selection.EnableShellEdgeFaceSelection = false;
            //
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewError) _viewConstraint = null;
                else if (itemTag is ViewPointSpring vps)
                {
                    _viewConstraint = vps;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else if (itemTag is ViewSurfaceSupport vss)
                {
                    _viewConstraint = vss;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if (vss.GetBase().TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewRigidBody vrd)
                {
                    _viewConstraint = vrd;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else if (itemTag is ViewCoupling vc)
                {
                    _viewConstraint = vc;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else if (itemTag is ViewCyclicSymmetry vcs)
                {
                    _viewConstraint = vcs;
                    _controller.Selection.EnableShellEdgeFaceSelection = false;
                }
                else if (itemTag is ViewTie vt)
                {
                    _viewConstraint = vt;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                    // 2D
                    if ((vt.GetBase() as Tie).TwoD) _controller.Selection.LimitSelectionToShellEdges = true;
                }
                else if (itemTag is ViewJoint vj)
                {
                    _viewConstraint = vj;
                    _controller.Selection.EnableShellEdgeFaceSelection = true;
                }
                else throw new NotImplementedException();
                //
                propertyGrid.SelectedObject = itemTag;
                //
                ShowHideSelectionForm();
                //
                HighlightConstraint();
                // Context menu
                if (propertyGrid.SelectedObject is ViewRigidBody ||
                    propertyGrid.SelectedObject is ViewCoupling) propertyGrid.ContextMenuStrip = null;
                // Swap Master/Slave
                else if (propertyGrid.SelectedObject is ViewTie || propertyGrid.SelectedObject is ViewJoint)
                    propertyGrid.ContextMenuStrip = cmsPropertyGrid;
            }
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            ShowHideContextMenu();
            //
            ShowHideSelectionForm();
            //
            HighlightConstraint();
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            if (propertyGrid.SelectedGridItem != null &&
                propertyGrid.SelectedGridItem.PropertyDescriptor != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                if (_viewConstraint is ViewCyclicSymmetry vcs)
                {
                    if (property == nameof(ViewCyclicSymmetry.NumberOfGraphSectors))
                    {
                        vcs.UpdateRegionVisibility();
                        propertyGrid.Refresh();
                        ShowHideSelectionForm();
                        HighlightConstraint();
                    }
                    else if (property == nameof(ViewCyclicSymmetry.AffectedRegionType))
                    {
                        propertyGrid.Refresh();
                        ShowHideSelectionForm();
                        HighlightConstraint();
                    }
                    else if (property == nameof(ViewCyclicSymmetry.PartName) ||
                             property == nameof(ViewCyclicSymmetry.ElementSetName))
                    {
                        HighlightConstraint();
                    }
                }
                else if (_viewConstraint is ViewJoint vj)
                {
                    _controller.Model.UpdateJointAutoValues((Joint)vj.GetBase());
                    propertyGrid.Refresh();
                    if (property == nameof(ViewJoint.JointType) || property == nameof(ViewJoint.CenterDefinition) ||
                        property == nameof(ViewJoint.AxisDefinition))
                        ShowHideSelectionForm();
                    HighlightConstraint();
                }
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewConstraint = (ViewConstraint)propertyGrid.SelectedObject;
            //
            if (Constraint == null) throw new CaeException("No constraint was selected.");
            // Check if the name exists
            CheckName(_constraintToEditName, Constraint.Name, _constraintNames, "constraint");
            // Check equations
            _viewConstraint.GetBase().CheckEquations();
            //
            if (Constraint is PointSpring ps)
            {
                if (ps.RegionType == RegionTypeEnum.Selection && (ps.CreationIds == null || ps.CreationIds.Length == 0))
                    throw new CaeException("The point spring region selection must contain at least one item.");
                //
                if (ps.GetSpringStiffnessValues().Length == 0)
                    throw new CaeException("At least one stiffness must be larger than 0.");
            }
            //
            if (Constraint is SurfaceSpring ss)
            {
                if (ss.RegionType == RegionTypeEnum.Selection && (ss.CreationIds == null || ss.CreationIds.Length == 0))
                    throw new CaeException("The surface spring region selection must contain at least one item.");
                //
                if (ss.GetSpringStiffnessValues().Length == 0)
                    throw new CaeException("At least one stiffness must be larger than 0.");
            }
            //
            if (Constraint is CompressionOnly co)
            {
                if (co.RegionType == RegionTypeEnum.Selection && (co.CreationIds == null || co.CreationIds.Length == 0))
                    throw new CaeException("The compression only constraint region selection must contain at least one item.");
                if (co.NonLinear == false && MessageBoxes.ShowWarningQuestionOKCancel("Using a linear solution procedure will " +
                    "linearize the compression only support. Continue?") == DialogResult.Cancel) 
                        throw new CaeException("BreakOnApply");
            }
            //
            if (Constraint is RigidBody rb && rb.RegionType == RegionTypeEnum.Selection &&
                (rb.CreationIds == null || rb.CreationIds.Length == 0))
                throw new CaeException("The rigid body region selection must contain at least one item.");
            //
            if (Constraint is KinematicCoupling kc)
            {
                if (kc.RegionType == RegionTypeEnum.Selection &&
                    (kc.CreationIds == null || kc.CreationIds.Length == 0))
                    throw new CaeException("The kinematic coupling surface selection must contain at least one item.");
                if (!kc.U1 && !kc.U2 && !kc.U3)
                    throw new CaeException("At least one kinematic coupling degree of freedom must be coupled.");
            }
            //
            if (Constraint is DistributingCoupling dc)
            {
                if (dc.RegionType == RegionTypeEnum.Selection &&
                    (dc.CreationIds == null || dc.CreationIds.Length == 0))
                    throw new CaeException("The distributing coupling surface selection must contain at least one item.");
                if (!dc.U1 && !dc.U2 && !dc.U3 && !dc.UR1 && !dc.UR2 && !dc.UR3)
                    throw new CaeException("At least one distributing coupling degree of freedom must be distributed.");
            }
            //
            if (Constraint is Joint joint)
            {
                if (joint.MasterRegionType == RegionTypeEnum.Selection &&
                    (joint.MasterCreationIds == null || joint.MasterCreationIds.Length == 0))
                    throw new CaeException("The joint master region selection must contain at least one item.");
                //
                if (joint.SlaveRegionType == RegionTypeEnum.Selection &&
                    (joint.SlaveCreationIds == null || joint.SlaveCreationIds.Length == 0))
                    throw new CaeException("The joint slave region selection must contain at least one item.");
                // Check for errors with the appropriate constructor
                var tmp = new Joint(joint.Name, joint.JointType, joint.CenterDefinition, joint.GetCenter(),
                                    joint.AxisDefinition, joint.GetAxisDirection(), joint.MasterRegionName,
                                    joint.MasterRegionType, joint.SlaveRegionName, joint.SlaveRegionType,
                                    _controller.Model.Properties.ModelSpace.IsTwoD());
            }
            //
            if (Constraint is Tie tie)
            {
                if (tie.MasterRegionType == RegionTypeEnum.Selection &&
                    (tie.MasterCreationIds == null || tie.MasterCreationIds.Length == 0))
                    throw new CaeException("The tie master region selection must contain at least one item.");
                //
                if (tie.SlaveRegionType == RegionTypeEnum.Selection &&
                    (tie.SlaveCreationIds == null || tie.SlaveCreationIds.Length == 0))
                    throw new CaeException("The tie slave region selection must contain at least one item.");
                // Check for errors with the appropriate constructor
                bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
                if (tie is CyclicSymmetry cyclicSymmetry)
                {
                    if (twoD) throw new CaeException("Cyclic symmetry is supported only for 3D models.");
                    if (cyclicSymmetry.NumberOfGraphSectors > 1)
                    {
                        if (cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                        {
                            if (cyclicSymmetry.CreationData == null ||
                                cyclicSymmetry.CreationIds == null ||
                                cyclicSymmetry.CreationIds.Length == 0)
                                throw new CaeException("The cyclic symmetry result expansion region selection must " +
                                                       "contain at least one item.");
                        }
                        else if (cyclicSymmetry.RegionType == RegionTypeEnum.PartName ||
                                 cyclicSymmetry.RegionType == RegionTypeEnum.ElementSetName)
                        {
                            if (string.IsNullOrWhiteSpace(cyclicSymmetry.RegionName))
                                throw new CaeException("The cyclic symmetry result expansion region must be defined.");
                        }
                        else throw new CaeException("The cyclic symmetry result expansion region type is not supported.");
                    }
                    var tmp = new CyclicSymmetry(cyclicSymmetry.Name, cyclicSymmetry.PositionTolerance.Value,
                                                 cyclicSymmetry.Adjust, cyclicSymmetry.NumberOfSectors,
                                                 cyclicSymmetry.NumberOfGraphSectors,
                                                 cyclicSymmetry.CheckSectorAngle,
                                                 cyclicSymmetry.GetAxisPoint(),
                                                 cyclicSymmetry.GetAxisDirection(),
                                                 cyclicSymmetry.MasterRegionName,
                                                 cyclicSymmetry.MasterRegionType,
                                                 cyclicSymmetry.SlaveRegionName,
                                                 cyclicSymmetry.SlaveRegionType, false);
                    _controller.Model.CheckCyclicSymmetryCompatibility(cyclicSymmetry, _constraintToEditName);
                }
                else
                {
                    var tmp = new Tie(tie.Name, tie.MasterRegionName, tie.MasterRegionType,
                                      tie.SlaveRegionName, tie.SlaveRegionType, twoD);
                }
            }
            // Create
            if (_constraintToEditName == null)
            {
                _controller.AddConstraintCommand(Constraint);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceConstraintCommand(_constraintToEditName, Constraint);
                _constraintToEditName = null; // prevents the execution of toInternal in OnHideOrClose
            }
            // Convert the constraint from internal to show it
            else
            {
                ConstraintInternal(false);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            // Deactivate selection limits
            _controller.Selection.EnableShellEdgeFaceSelection = false;
            // Convert the constraint from internal to show it
            ConstraintInternal(false);
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string constraintToEditName)
        {
            this.btnOkAddNew.Visible = constraintToEditName == null;
            //
            _propertyItemChanged = false;
            _constraintNames = null;
            _constraintToEditName = null;
            _viewConstraint = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            //
            _constraintNames = _controller.GetConstraintNames();
            _constraintToEditName = constraintToEditName;
            //
            string[] referencePointNames = _controller.GetModelReferencePointNames();
            string[] nodeSetNames = _controller.GetUserNodeSetNames();
            string[] surfaceNames = _controller.GetUserSurfaceNames();
            string[] cyclicSurfaceNames = surfaceNames;
            string[] partNames = _controller.GetModelPartNames();
            string[] elementSetNames = _controller.GetUserElementSetNames();
            if (referencePointNames == null) referencePointNames = new string[0];
            if (nodeSetNames == null) nodeSetNames = new string[0];
            if (surfaceNames == null) surfaceNames = new string[0];
            if (cyclicSurfaceNames == null) cyclicSurfaceNames = new string[0];
            if (partNames == null) partNames = new string[0];
            if (elementSetNames == null) elementSetNames = new string[0];
            //
            if (_constraintNames == null)
                throw new CaeException("The constraint names must be defined first.");
            // Populate list view
            PopulateListOfConstraints(referencePointNames, nodeSetNames, surfaceNames, cyclicSurfaceNames,
                                      partNames, elementSetNames);
            // Create new constraint
            if (_constraintToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewConstraint = null;
                //
                HighlightConstraint(); // must be here if called from the menu
            }
            // Edit existing constraint
            else
            {
                // Get and convert a converted constraint back to selection
                Constraint = _controller.GetConstraint(_constraintToEditName); // to clone                
                SetCreationDataToSelection(Constraint);
                // Convert the constraint to internal to hide it
                ConstraintInternal(true);
                //
                int selectedId;
                if (_viewConstraint is ViewPointSpring vps)
                {
                    selectedId = lvTypes.FindItemWithText("Point Spring").Index;
                    // Master
                    if (vps.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vps.MasterRegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vps.NodeSetName, s => { vps.NodeSetName = s; });
                    else if (vps.MasterRegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, vps.ReferencePointName, s => { vps.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    vps.PopulateDropDownLists(nodeSetNames, referencePointNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewSurfaceSupport vss)
                {
                    selectedId = lvTypes.FindItemWithText("Surface Support").Index;
                    // Master
                    if (vss.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vss.MasterRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vss.SurfaceName, s => { vss.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vss.PopulateDropDownLists(surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewRigidBody vrb)
                {
                    selectedId = lvTypes.FindItemWithText("Rigid Body").Index;
                    // Reference point
                    CheckMissingValueRef(ref referencePointNames, vrb.ReferencePointName, s => { vrb.ReferencePointName = s; });
                    // Slave
                    if (vrb.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vrb.SlaveRegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, vrb.NodeSetName, s => { vrb.NodeSetName = s; });
                    else if (vrb.SlaveRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vrb.SurfaceName, s => { vrb.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vrb.PopulateDropDownLists(referencePointNames, nodeSetNames, surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewCoupling vc)
                {
                    selectedId = lvTypes.FindItemWithText("Coupling").Index;
                    // Reference point
                    CheckMissingValueRef(ref referencePointNames, vc.ReferencePointName,
                                         s => { vc.ReferencePointName = s; });
                    // Slave
                    if (vc.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vc.SlaveRegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, vc.SurfaceName, s => { vc.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    vc.PopulateDropDownLists(referencePointNames, surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = null;
                }
                else if (_viewConstraint is ViewCyclicSymmetry vcs)
                {
                    selectedId = lvTypes.FindItemWithText("Cyclic Symmetry").Index;
                    // Master
                    if (vcs.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref cyclicSurfaceNames, vcs.MasterSurfaceName,
                                              s => { vcs.MasterSurfaceName = s; });
                    // Slave
                    if (vcs.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref cyclicSurfaceNames, vcs.SlaveSurfaceName,
                                              s => { vcs.SlaveSurfaceName = s; });
                    // Result expansion region
                    if (vcs.AffectedRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vcs.AffectedRegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vcs.PartName, s => { vcs.PartName = s; });
                    else if (vcs.AffectedRegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vcs.ElementSetName,
                                             s => { vcs.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vcs.PopulateDropDownLists(cyclicSurfaceNames, partNames, elementSetNames);
                    propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
                else if (_viewConstraint is ViewJoint vj)
                {
                    selectedId = lvTypes.FindItemWithText("Joint").Index;
                    // Master
                    if (vj.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vj.MasterSurfaceName, s => { vj.MasterSurfaceName = s; });
                    // Slave
                    if (vj.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vj.SlaveSurfaceName, s => { vj.SlaveSurfaceName = s; });
                    //
                    vj.PopulateDropDownLists(surfaceNames);
                    _controller.Model.UpdateJointAutoValues((Joint)vj.GetBase());
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
                else if (_viewConstraint is ViewTie vt)
                {
                    selectedId = lvTypes.FindItemWithText("Tie").Index;
                    // Master
                    if (vt.MasterRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vt.MasterSurfaceName, s => { vt.MasterSurfaceName = s; });
                    // Slave
                    if (vt.SlaveRegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else CheckMissingValueRef(ref surfaceNames, vt.SlaveSurfaceName, s => { vt.SlaveSurfaceName = s; });
                    //
                    vt.PopulateDropDownLists(surfaceNames);
                    // Context menu strip
                    propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewConstraint;
                _preselectIndex = selectedId;
            }
            ShowHideSelectionForm();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        private void SetCreationDataToSelection(Constraint constraint)
        {
            if (constraint is PointSpring ps && ps.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(ps) || // do not use BoundaryCondition.Valid
                    !_controller.Model.RegionValid(ps))
                {
                    // Region invalid
                    ps.CreationData = null;
                    ps.CreationIds = null;
                    _propertyItemChanged = true;
                }
                ps.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is SurfaceSpring ss && ss.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(ss) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(ss))
                {
                    // Region invalid
                    ss.CreationData = null;
                    ss.CreationIds = null;
                    _propertyItemChanged = true;
                }
                ss.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is CompressionOnly co && co.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(co) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(co))
                {
                    // Region invalid
                    co.CreationData = null;
                    co.CreationIds = null;
                    _propertyItemChanged = true;
                }
                co.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is RigidBody rb && rb.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(rb) || // do not use BoundaryCondition.Valid
                   !_controller.Model.RegionValid(rb))
                {
                    // Region invalid
                    rb.CreationData = null;
                    rb.CreationIds = null;
                    _propertyItemChanged = true;
                }
                rb.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is KinematicCoupling kc && kc.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(kc) || // do not use BoundaryCondition.Valid
                    !_controller.Model.RegionValid(kc))
                {
                    // Region invalid
                    kc.CreationData = null;
                    kc.CreationIds = null;
                    _propertyItemChanged = true;
                }
                kc.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is DistributingCoupling dc && dc.CreationData != null)
            {
                if (!_controller.Model.IsConstraintRegionValid(dc) || // do not use BoundaryCondition.Valid
                    !_controller.Model.RegionValid(dc))
                {
                    // Region invalid
                    dc.CreationData = null;
                    dc.CreationIds = null;
                    _propertyItemChanged = true;
                }
                dc.RegionType = RegionTypeEnum.Selection;
            }
            else if (constraint is Joint joint)
            {
                if (joint.MasterCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(joint.MasterRegionName))
                    {
                        // Region invalid
                        joint.MasterCreationData = null;
                        joint.MasterCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    joint.MasterRegionType = RegionTypeEnum.Selection;
                }
                if (joint.SlaveCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(joint.SlaveRegionName))
                    {
                        // Region invalid
                        joint.SlaveCreationData = null;
                        joint.SlaveCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    joint.SlaveRegionType = RegionTypeEnum.Selection;
                }
            }
            else if (constraint is Tie tie)
            {
                if (tie is CyclicSymmetry cyclicSymmetry && cyclicSymmetry.CreationData != null)
                {
                    if (!_controller.Model.Mesh.ElementSets.ContainsValidKey(cyclicSymmetry.RegionName))
                    {
                        // Region invalid
                        cyclicSymmetry.CreationData = null;
                        cyclicSymmetry.CreationIds = null;
                        _propertyItemChanged = true;
                    }
                    cyclicSymmetry.RegionType = RegionTypeEnum.Selection;
                }
                if (tie.MasterCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(tie.MasterRegionName))
                    {
                        // Region invalid
                        tie.MasterCreationData = null;
                        tie.MasterCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    tie.MasterRegionType = RegionTypeEnum.Selection;
                }
                if (tie.SlaveCreationData != null)
                {
                    if (!_controller.Model.Mesh.Surfaces.ContainsValidKey(tie.SlaveRegionName))
                    {
                        // Region invalid
                        tie.SlaveCreationData = null;
                        tie.SlaveCreationIds = null;
                        _propertyItemChanged = true;
                    }
                    tie.SlaveRegionType = RegionTypeEnum.Selection;
                }
            }
        }

        // Methods                                                                                                                  
        private void PopulateListOfConstraints(string[] referencePointNames, string[] nodeSetNames,
                                               string[] surfaceNames, string[] cyclicSurfaceNames,
                                               string[] partNames, string[] elementSetNames)
        {
            Color color = _controller.Settings.Pre.ConstraintSymbolColor;
            bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
            // Populate list view
            ListViewItem item;
            // Point spring
            item = new ListViewItem("Point Spring");
            PointSpring pointSpring = new PointSpring(GetConstraintName("Point_Spring"), "", RegionTypeEnum.Selection, twoD, true);
            ViewPointSpring vps = new ViewPointSpring(pointSpring);
            vps.PopulateDropDownLists(nodeSetNames, referencePointNames);
            item.Tag = vps;
            vps.Color = color;
            lvTypes.Items.Add(item);
            // Surface support
            item = new ListViewItem("Surface Support");
            SurfaceSpring surfaceSpring = new SurfaceSpring(GetConstraintName("Surface_Support"),
                                                            "", RegionTypeEnum.Selection, twoD, true);
            ViewSurfaceSupport vss = new ViewSurfaceSupport(surfaceSpring);
            vss.PopulateDropDownLists(surfaceNames);
            item.Tag = vss;
            vss.Color = color;
            lvTypes.Items.Add(item);
            // Rigid body
            item = new ListViewItem("Rigid Body");
            if (referencePointNames.Length > 0)
            {
                RigidBody rb = new RigidBody(GetConstraintName("Rigid_Body"), referencePointNames[0],
                                             "", RegionTypeEnum.Selection, twoD);
                ViewRigidBody vrb = new ViewRigidBody(rb);
                vrb.PopulateDropDownLists(referencePointNames, nodeSetNames, surfaceNames);
                vrb.Color = color;
                item.Tag = vrb;
            }
            else item.Tag = new ViewError("There is no reference point defined for the rigid body constraint definition.");
            lvTypes.Items.Add(item);
            // Coupling
            item = new ListViewItem("Coupling");
            if (referencePointNames.Length > 0)
            {
                KinematicCoupling kc = new KinematicCoupling(GetConstraintName("Coupling"),
                                                             referencePointNames[0], "",
                                                             RegionTypeEnum.Selection, twoD);
                ViewCoupling vc = new ViewCoupling(kc);
                vc.PopulateDropDownLists(referencePointNames, surfaceNames);
                vc.Color = color;
                item.Tag = vc;
            }
            else item.Tag = new ViewError("There is no reference point defined for the coupling constraint definition.");
            lvTypes.Items.Add(item);
            // Tie
            item = new ListViewItem("Tie");
            Tie tie = new Tie(GetConstraintName("Tie"), "", RegionTypeEnum.Selection, "", RegionTypeEnum.Selection, twoD);
            ViewTie vt = new ViewTie(tie);
            vt.PopulateDropDownLists(surfaceNames);
            item.Tag = vt;
            vt.MasterColor = color;
            vt.SlaveColor = color;
            lvTypes.Items.Add(item);
            // Cyclic symmetry
            item = new ListViewItem("Cyclic Symmetry");
            if (twoD)
                item.Tag = new ViewError("Cyclic symmetry is supported only for 3D models.");
            else
            {
                CyclicSymmetry cyclicSymmetry =
                    new CyclicSymmetry(GetConstraintName("Cyclic_Symmetry"), "", RegionTypeEnum.Selection,
                                       "", RegionTypeEnum.Selection, false);
                ViewCyclicSymmetry vcs = new ViewCyclicSymmetry(cyclicSymmetry);
                vcs.PopulateDropDownLists(cyclicSurfaceNames, partNames, elementSetNames);
                vcs.MasterColor = color;
                vcs.SlaveColor = color;
                item.Tag = vcs;
            }
            lvTypes.Items.Add(item);
            // Joint
            item = new ListViewItem("Joint");
            if (twoD)
                item.Tag = new ViewError("Joints are supported only for 3D models.");
            else
            {
                Joint joint = new Joint(GetConstraintName("Joint"), "", RegionTypeEnum.Selection, "", RegionTypeEnum.Selection,
                                        false);
                ViewJoint vj = new ViewJoint(joint);
                vj.PopulateDropDownLists(surfaceNames);
                vj.MasterColor = color;
                vj.SlaveColor = color;
                item.Tag = vj;
            }
            lvTypes.Items.Add(item);
        }
        private string GetConstraintName(string namePrefix)
        {
            return _constraintNames.GetNextNumberedKey(namePrefix);
        }
        //
        private void ShowHideContextMenu()
        {
            propertyGrid.ContextMenuStrip = null;
            //
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (Constraint != null)
            {
                if (Constraint is Tie || Constraint is Joint)
                {
                    if (property == nameof(ViewTie.MasterRegionType) || property == nameof(ViewTie.SlaveRegionType))
                        propertyGrid.ContextMenuStrip = cmsPropertyGrid;
                }
            }
        }
        private void tsmiSwapMasterSlave_Click(object sender, EventArgs e)
        {
            bool swapped = false;
            if (propertyGrid.SelectedObject is ViewTie vt && Constraint is Tie tie)
            {
                tie.SwapMasterSlave();
                vt.UpdateRegionVisibility();
                swapped = true;
            }
            else if (propertyGrid.SelectedObject is ViewJoint vj && Constraint is Joint joint)
            {
                joint.SwapMasterSlave();
                vj.UpdateRegionVisibility();
                swapped = true;
            }
            //
            if (swapped)
            {
                propertyGrid.Refresh();
                //
                _prevSelectionFormProperty = null;   // calls ItemSetDataEditor.SelectionForm.ResetSelection
                ShowHideSelectionForm();
                //
                OnPropertyGridSelectedGridItemChanged();    // highlight
                _propertyItemChanged = true;
            }
        }
        //
        private void HighlightConstraint()
        {
            if (!this.Visible) return;
            //
            try
            {
                _controller.ClearSelectionHistory();
                //
                if (_viewConstraint == null) { }
                else if (Constraint is PointSpring ps)
                {
                    // Master
                    HighlightRegion(ps.MasterRegionType, ps.MasterRegionName, ps.CreationData, true, false);
                }
                else if (Constraint is SurfaceSpring ss)
                {
                    // Master
                    HighlightRegion(ss.MasterRegionType, ss.MasterRegionName, ss.CreationData, true, false);
                }
                else if (Constraint is CompressionOnly co)
                {
                    // Master
                    HighlightRegion(co.MasterRegionType, co.MasterRegionName, co.CreationData, true, false);
                }
                else if (Constraint is RigidBody rb)
                {
                    // Master
                    _controller.HighlightReferencePoint(rb.ReferencePointName);
                    // Slave
                    HighlightRegion(rb.SlaveRegionType, rb.SlaveRegionName, rb.CreationData, false, true);
                }
                else if (Constraint is KinematicCoupling kc)
                {
                    // Master
                    _controller.HighlightReferencePoint(kc.ReferencePointName);
                    // Slave
                    HighlightRegion(kc.SlaveRegionType, kc.SlaveRegionName, kc.CreationData, false, true);
                }
                else if (Constraint is DistributingCoupling dc)
                {
                    // Master
                    _controller.HighlightReferencePoint(dc.ReferencePointName);
                    // Slave
                    HighlightRegion(dc.SlaveRegionType, dc.SlaveRegionName, dc.CreationData, false, true);
                }
                else if (Constraint is Joint joint)
                {
                    // Property grid item - must be here
                    if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (property == nameof(ViewJoint.MasterRegionType))
                    {
                        // Master
                        HighlightRegion(joint.MasterRegionType, joint.MasterRegionName, joint.MasterCreationData, true, false);
                    }
                    else if (property == nameof(ViewJoint.SlaveRegionType))
                    {
                        // Slave
                        HighlightRegion(joint.SlaveRegionType, joint.SlaveRegionName, joint.SlaveCreationData, true, true);
                    }
                    else
                    {
                        // Master
                        HighlightRegion(joint.MasterRegionType, joint.MasterRegionName, joint.MasterCreationData, true, false);
                        // Slave
                        HighlightRegion(joint.SlaveRegionType, joint.SlaveRegionName, joint.SlaveCreationData, false, true);
                    }
                    //
                    _controller.HighlightJointSymbol(joint);
                }
                else if (Constraint is Tie tie)
                {
                    // Property grid item - must be here
                    if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (tie is CyclicSymmetry affectedCyclicSymmetry &&
                        affectedCyclicSymmetry.NumberOfGraphSectors > 1 &&
                        (property == nameof(ViewCyclicSymmetry.AffectedRegionType) ||
                         property == nameof(ViewCyclicSymmetry.PartName) ||
                         property == nameof(ViewCyclicSymmetry.ElementSetName)))
                    {
                        HighlightRegion(affectedCyclicSymmetry.RegionType, affectedCyclicSymmetry.RegionName,
                                        affectedCyclicSymmetry.CreationData, true, false);
                    }
                    else if (property == nameof(ViewTie.MasterRegionType))
                    {
                        // Master
                        HighlightRegion(tie.MasterRegionType, tie.MasterRegionName, tie.MasterCreationData, true, false);
                    }
                    else if(property == nameof(ViewTie.SlaveRegionType))
                    {
                        // Slave
                        HighlightRegion(tie.SlaveRegionType, tie.SlaveRegionName, tie.SlaveCreationData, true, true);
                    }
                    else
                    {
                        // Master
                        HighlightRegion(tie.MasterRegionType, tie.MasterRegionName, tie.MasterCreationData, true, false);
                        // Slave
                        HighlightRegion(tie.SlaveRegionType, tie.SlaveRegionName, tie.SlaveCreationData, false, true);
                    }
                    //
                    if (tie is CyclicSymmetry cyclicSymmetry) _controller.HighlightCyclicSymmetryAxis(cyclicSymmetry);
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void HighlightRegion(RegionTypeEnum regionType, string regionName, Selection creationData,
                                     bool clear, bool useSecondaryHighlightColor)
        {
            if (regionType == RegionTypeEnum.NodeSetName)
                _controller.HighlightNodeSet(regionName, useSecondaryHighlightColor);
            else if (regionType == RegionTypeEnum.SurfaceName)
                _controller.HighlightSurface(regionName, useSecondaryHighlightColor);
            else if (regionType == RegionTypeEnum.ReferencePointName)
                _controller.HighlightReferencePoint(regionName);
            else if (regionType == RegionTypeEnum.PartName ||
                     regionType == RegionTypeEnum.ElementSetName)
                _controller.Highlight3DObjects(new object[] { regionName });
            else if (regionType == RegionTypeEnum.Selection)
            {
                SetSelectItem();
                //
                if (creationData != null)
                {
                    _controller.Selection = creationData.DeepCopy(true, false);
                    _controller.HighlightSelection(clear, true, useSecondaryHighlightColor);
                }
            }
        }
        private void ShowHideSelectionForm()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property != _prevSelectionFormProperty) ItemSetDataEditor.SelectionForm.ResetSelection(false);
            _prevSelectionFormProperty = property;
            //
            if (Constraint != null)
            {
                ItemSetDataEditor.SelectionForm.SetOnlyGeometrySelection(false, false);
                //
                if (Constraint is PointSpring ps && ps.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is SurfaceSpring ss && ss.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is CompressionOnly co && co.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is RigidBody rb && rb.RegionType == RegionTypeEnum.Selection)
                {
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    ItemSetDataEditor.SelectionForm.SetOnlyGeometrySelection(true, false);
                }
                else if (Constraint is KinematicCoupling kc && kc.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is DistributingCoupling dc && dc.RegionType == RegionTypeEnum.Selection)
                    ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                else if (Constraint is Joint joint)
                {
                    if (property == nameof(ViewJoint.MasterRegionType) && joint.MasterRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else if (property == nameof(ViewJoint.SlaveRegionType) && joint.SlaveRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else ItemSetDataEditor.SelectionForm.Hide();
                }
                else if (Constraint is Tie tie)
                {
                    if (tie is CyclicSymmetry cyclicSymmetry &&
                        cyclicSymmetry.NumberOfGraphSectors > 1 &&
                        property == nameof(ViewCyclicSymmetry.AffectedRegionType) &&
                        cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else if (property == nameof(ViewTie.MasterRegionType) &&
                             tie.MasterRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                        ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
                    else ItemSetDataEditor.SelectionForm.Hide();
                }
                else ItemSetDataEditor.SelectionForm.Hide();
            }
            else ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (Constraint == null) { }
            else if (Constraint is PointSpring ps)
            {
                if (ps.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToNode();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is SurfaceSpring ss)
            {
                if (ss.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is CompressionOnly co)
            {
                if (co.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is RigidBody rb)
            {
                if (rb.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToGeometry();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is KinematicCoupling kc)
            {
                if (kc.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is DistributingCoupling dc)
            {
                if (dc.RegionType == RegionTypeEnum.Selection) _controller.SetSelectItemToSurface();
                else _controller.SetSelectByToOff();
            }
            else if (Constraint is Joint joint)
            {
                if ((joint.MasterRegionType == RegionTypeEnum.Selection && property == nameof(ViewJoint.MasterRegionType)) ||
                    (joint.SlaveRegionType == RegionTypeEnum.Selection && property == nameof(ViewJoint.SlaveRegionType)))
                    _controller.SetSelectItemToSurface();
                else
                    _controller.SetSelectByToOff();
            }
            else if (Constraint is Tie tie)
            {
                if (tie is CyclicSymmetry cyclicSymmetry &&
                    cyclicSymmetry.NumberOfGraphSectors > 1 &&
                    property == nameof(ViewCyclicSymmetry.AffectedRegionType) &&
                    cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                    _controller.SetSelectItemToPart();
                else if ((tie.MasterRegionType == RegionTypeEnum.Selection &&
                          property == nameof(ViewTie.MasterRegionType)) ||
                         (tie.SlaveRegionType == RegionTypeEnum.Selection &&
                          property == nameof(ViewTie.SlaveRegionType)))
                    _controller.SetSelectItemToSurface();
                else
                    _controller.SetSelectByToOff();
            }
            else throw new NotSupportedException();
        }
        private void ConstraintInternal(bool toInternal)
        {
            if (_constraintToEditName != null)
            {
                // Convert the constraint from/to internal to hide/show it
                _controller.GetConstraint(_constraintToEditName).Internal = toInternal;
                _controller.FeModelUpdate(UpdateType.RedrawSymbols);
            }
        }
        //
        public void SelectionChanged(int[] ids)
        {
            bool changed = false;
            //
            if (Constraint != null)
            {
                if (_viewConstraint is ViewCyclicSymmetry vcs &&
                    propertyGrid.SelectedGridItem != null &&
                    propertyGrid.SelectedGridItem.PropertyDescriptor != null &&
                    propertyGrid.SelectedGridItem.PropertyDescriptor.Name == nameof(ViewCyclicSymmetry.AxisPointItemSet))
                {
                    if (ids != null && ids.Length == 1)
                    {
                        FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                        vcs.X = new EquationString(node.X.ToString());
                        vcs.Y = new EquationString(node.Y.ToString());
                        vcs.Z = new EquationString(node.Z.ToString());
                        changed = true;
                    }
                }
                else if (_viewConstraint is ViewJoint vj &&
                         propertyGrid.SelectedGridItem != null &&
                         propertyGrid.SelectedGridItem.PropertyDescriptor != null &&
                         propertyGrid.SelectedGridItem.PropertyDescriptor.Name == nameof(ViewJoint.CenterItemSet))
                {
                    if (ids != null && ids.Length == 1)
                    {
                        FeNode node = _controller.Model.Mesh.Nodes[ids[0]];
                        vj.X = new EquationString(node.X.ToString());
                        vj.Y = new EquationString(node.Y.ToString());
                        vj.Z = new EquationString(node.Z.ToString());
                        changed = true;
                    }
                }
                else if (Constraint is PointSpring ps)
                {
                    if (ps.RegionType == RegionTypeEnum.Selection)
                    {
                        ps.CreationIds = ids;
                        ps.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is SurfaceSpring ss)
                {
                    if (ss.RegionType == RegionTypeEnum.Selection)
                    {
                        ss.CreationIds = ids;
                        ss.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is CompressionOnly co)
                {
                    if (co.RegionType == RegionTypeEnum.Selection)
                    {
                        co.CreationIds = ids;
                        co.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is RigidBody rb)
                {
                    if (rb.RegionType == RegionTypeEnum.Selection)
                    {
                        rb.CreationIds = ids;
                        rb.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is KinematicCoupling kc)
                {
                    if (kc.RegionType == RegionTypeEnum.Selection)
                    {
                        kc.CreationIds = ids;
                        kc.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is DistributingCoupling dc)
                {
                    if (dc.RegionType == RegionTypeEnum.Selection)
                    {
                        dc.CreationIds = ids;
                        dc.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else if (Constraint is Joint joint)
                {
                    // Property grid item - must be here
                    if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (property == nameof(ViewJoint.MasterRegionType) && joint.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        joint.MasterCreationIds = ids;
                        joint.MasterCreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                    else if (property == nameof(ViewJoint.SlaveRegionType) && joint.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        joint.SlaveCreationIds = ids;
                        joint.SlaveCreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                    //
                    if (changed) _controller.Model.UpdateJointAutoValues(joint);
                }
                else if (Constraint is Tie tie)
                {
                    // Property grid item - must be here
                    if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return;
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    //
                    if (tie is CyclicSymmetry cyclicSymmetry &&
                        cyclicSymmetry.NumberOfGraphSectors > 1 &&
                        property == nameof(ViewCyclicSymmetry.AffectedRegionType) &&
                        cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                    {
                        cyclicSymmetry.CreationIds = ids;
                        cyclicSymmetry.CreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                    else if (property == nameof(ViewTie.MasterRegionType) &&
                             tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        tie.MasterCreationIds = ids;
                        tie.MasterCreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        tie.SlaveCreationIds = ids;
                        tie.SlaveCreationData = _controller.Selection.DeepCopy(true, false);
                        changed = true;
                    }
                }
                else throw new NotSupportedException();
                //
                if (changed)
                {
                    Enabled = true; // must be first for the selection to work
                    //
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                }
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightConstraint();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return true;
            Constraint constraint = Constraint;
            //
            if (constraint != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (constraint is PointSpring ps)
                {
                    if (ps.CreationData != null) return ps.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is SurfaceSpring ss)
                {
                    if (ss.CreationData != null) return ss.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is CompressionOnly co)
                {
                    if (co.CreationData != null) return co.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is RigidBody rb)
                {
                    if (rb.CreationData != null) return rb.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is KinematicCoupling kc)
                {
                    if (kc.CreationData != null) return kc.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is DistributingCoupling dc)
                {
                    if (dc.CreationData != null) return dc.CreationData.IsGeometryBased();
                    else return true;
                }
                else if (constraint is Joint joint)
                {
                    if (property == nameof(ViewJoint.MasterRegionType) && joint.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (joint.MasterCreationData != null) return joint.MasterCreationData.IsGeometryBased();
                        else return true;
                    }
                    else if (property == nameof(ViewJoint.SlaveRegionType) && joint.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (joint.SlaveCreationData != null) return joint.SlaveCreationData.IsGeometryBased();
                        else return true;
                    }
                }
                else if (constraint is Tie tie)
                {
                    if (tie is CyclicSymmetry cyclicSymmetry &&
                        cyclicSymmetry.NumberOfGraphSectors > 1 &&
                        property == nameof(ViewCyclicSymmetry.AffectedRegionType) &&
                        cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                    {
                        if (cyclicSymmetry.CreationData != null)
                            return cyclicSymmetry.CreationData.IsGeometryBased();
                        else return true;
                    }
                    else if (property == nameof(ViewTie.MasterRegionType) &&
                             tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.MasterCreationData != null) return tie.MasterCreationData.IsGeometryBased();
                        else return true;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.SlaveCreationData != null) return tie.SlaveCreationData.IsGeometryBased();
                        else return true;
                    }
                }
                else throw new NotSupportedException();
            }
            return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            if (propertyGrid.SelectedGridItem == null || propertyGrid.SelectedGridItem.PropertyDescriptor == null) return defaultMode;
            Constraint constraint = Constraint;
            //
            if (constraint != null && IsSelectionGeometryBased())
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (constraint is PointSpring ps)
                {
                    if (ps.CreationData != null) return ps.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is SurfaceSpring ss)
                {
                    if (ss.CreationData != null) return ss.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is CompressionOnly co)
                {
                    if (co.CreationData != null) return co.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is RigidBody rb)
                {
                    if (rb.CreationData != null) return rb.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is KinematicCoupling kc)
                {
                    if (kc.CreationData != null) return kc.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is DistributingCoupling dc)
                {
                    if (dc.CreationData != null) return dc.CreationData.IsGeometryIdBased(defaultMode);
                    else return defaultMode;
                }
                else if (constraint is Joint joint)
                {
                    if (property == nameof(ViewJoint.MasterRegionType) && joint.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (joint.MasterCreationData != null) return joint.MasterCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                    else if (property == nameof(ViewJoint.SlaveRegionType) && joint.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (joint.SlaveCreationData != null) return joint.SlaveCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                }
                else if (constraint is Tie tie)
                {
                    if (tie is CyclicSymmetry cyclicSymmetry &&
                        cyclicSymmetry.NumberOfGraphSectors > 1 &&
                        property == nameof(ViewCyclicSymmetry.AffectedRegionType) &&
                        cyclicSymmetry.RegionType == RegionTypeEnum.Selection)
                    {
                        if (cyclicSymmetry.CreationData != null)
                            return cyclicSymmetry.CreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                    else if (property == nameof(ViewTie.MasterRegionType) &&
                             tie.MasterRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.MasterCreationData != null) return tie.MasterCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                    else if (property == nameof(ViewTie.SlaveRegionType) && tie.SlaveRegionType == RegionTypeEnum.Selection)
                    {
                        if (tie.SlaveCreationData != null) return tie.SlaveCreationData.IsGeometryIdBased(defaultMode);
                        else return defaultMode;
                    }
                }
                else throw new NotSupportedException();
            }
            return defaultMode;
        }
    }
}
