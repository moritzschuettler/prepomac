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
using CaeMesh;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace PrePoMax.Forms
{
    class FrmSection : UserControls.FrmPropertyDataListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _sectionNames;
        private string[] _materialNames;
        private TabPage[] _pages;
        private string _sectionToEditName;
        private ViewSection _viewSection;
        private Controller _controller;
        private UserControls.DoubleBufferedPanel pnlLayup;
        private const string _layerColumnName = "LayerName";
        private const double _layupHatchSpacing = 8;
        private const int _layupArrowWidth = 28;


        // Properties                                                                                                               
        public Section Section
        {
            get { return _viewSection != null ? _viewSection.GetBase() : null; }
            set
            {
                if (value is SolidSection sose) _viewSection = new ViewSolidSection(sose.DeepClone());
                else if (value is CompositeShellSection csse)
                    _viewSection = new ViewCompositeShellSection(csse.DeepClone());
                else if (value is ShellSection shse) _viewSection = new ViewShellSection(shse.DeepClone());
                else if (value is MembraneSection mese) _viewSection = new ViewMembraneSection(mese.DeepClone());
                else if (value is PointMassSection pmse) _viewSection = new ViewPointMassSection(pmse.DeepClone());
                else if (value is DistributedMassSection dmse) _viewSection = new ViewDistributedMassSection(dmse.DeepClone());
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmSection(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewSection = null;
            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages) _pages[i++] = tabPage;
        }
        private void InitializeComponent()
        {
            this.pnlLayup = new UserControls.DoubleBufferedPanel();
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.tpDataPoints.SuspendLayout();
            this.SuspendLayout();
            //
            // tpDataPoints
            //
            this.tpDataPoints.Controls.Add(this.pnlLayup);
            this.tpDataPoints.Text = "Layers";
            //
            // pnlLayup
            //
            this.pnlLayup.BackColor = System.Drawing.SystemColors.Window;
            this.pnlLayup.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLayup.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlLayup.Location = new System.Drawing.Point(3, 150);
            this.pnlLayup.Name = "pnlLayup";
            this.pnlLayup.Size = new System.Drawing.Size(296, 120);
            this.pnlLayup.TabIndex = 1;
            this.pnlLayup.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlLayup_Paint);
            this.pnlLayup.Resize += new System.EventHandler(this.pnlLayup_Resize);
            this.pnlLayup.MouseClick += new System.Windows.Forms.MouseEventHandler(this.pnlLayup_MouseClick);
            //
            // tcProperties
            //
            this.tcProperties.SelectedIndexChanged += new System.EventHandler(this.tcProperties_SelectedIndexChanged);
            //
            // dgvData
            //
            this.dgvData.EnablePlotMenu = false;
            this.dgvData.SelectionChanged += new System.EventHandler(this.dgvData_SelectionChanged);
            this.dgvData.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dgvData_CellFormatting);
            //
            // FrmSection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 461);
            this.Name = "FrmSection";
            this.Text = "Edit Section";
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.tpDataPoints.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void tcProperties_SelectedIndexChanged(object sender, EventArgs e)
        {
            HighlightSection();
        }
        private void dgvData_SelectionChanged(object sender, EventArgs e)
        {
            HighlightSection();
            pnlLayup.Invalidate();
        }
        private void dgvData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvData.Columns[e.ColumnIndex].Name == _layerColumnName)
            {
                e.Value = dgvData.Rows[e.RowIndex].IsNewRow ? "" : "Layer " + (e.RowIndex + 1);
                e.FormattingApplied = true;
            }
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
            propertyGrid.Refresh();
            dgvData.Invalidate();
            pnlLayup.Invalidate();
        }
        private void pnlLayup_Resize(object sender, EventArgs e)
        {
            pnlLayup.Invalidate();
        }
        private void pnlLayup_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                int row = GetLayupRowAt(e.Location);
                if (row < 0 || row >= dgvData.Rows.Count) return;
                //
                int columnIndex = dgvData.CurrentCell != null ? dgvData.CurrentCell.ColumnIndex : 0;
                dgvData.CurrentCell = dgvData[columnIndex, row];
                dgvData.Focus();
            }
            catch { }
        }
        private void pnlLayup_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (Section is CompositeShellSection css) DrawLayup(e.Graphics, css);
            }
            catch { }
        }
        // A new layer starts as a copy of the last one
        private void Binding_AddingNew(object sender, AddingNewEventArgs e)
        {
            if (_viewSection is ViewCompositeShellSection vcss && vcss.Layers.Count > 0)
                e.NewObject = new CompositeLayerDataPoint(vcss.Layers[vcss.Layers.Count - 1].GetLayer().DeepClone());
            else
                e.NewObject = new CompositeLayerDataPoint(new CompositeLayer(_materialNames.Length > 0 ? _materialNames[0] : null,
                                                                             1, 0));
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
                if (itemTag is ViewError)
                {
                    _viewSection = null;
                }
                else if (itemTag is ViewSection vs)
                {
                    _viewSection = vs;
                }
                else throw new NotImplementedException();
                // Layers
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                tcProperties.TabPages.Add(_pages[0]);   // properties
                if (_viewSection is ViewCompositeShellSection vcss)
                {
                    tcProperties.TabPages.Add(_pages[1]);   // layers
                    SetLayersBinding(vcss);
                }
                //
                ShowHideSelectionForm();
                //
                propertyGrid.SelectedObject = itemTag;
                //
                HighlightSection();
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (property == nameof(_viewSection.RegionType))
            {
                ShowHideSelectionForm();
            }
            //
            HighlightSection();
            pnlLayup.Invalidate();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewSection = (ViewSection)propertyGrid.SelectedObject;
            //
            if (_viewSection == null) throw new CaeException("No section was selected.");
            // Check if the name exists
            CheckName(_sectionToEditName, Section.Name, _sectionNames, "section");
            // Check equations
            _viewSection.GetBase().CheckEquations();
            //
            if (Section.RegionType == RegionTypeEnum.Selection &&
                (Section.CreationIds == null || Section.CreationIds.Length == 0))
                throw new CaeException("The section selection must contain at least one item.");
            //
            if (!CheckSectionElementTypes())
                throw new CaeException("The section type and the section region are not compatible.");
            //
            if (Section is CompositeShellSection css)
            {
                foreach (CompositeLayer layer in css.Layers)
                {
                    if (!_materialNames.Contains(layer.MaterialName))
                        throw new CaeException("Every layer must name an existing material.");
                }
            }
            //
            if ((Section is SolidSection sos && sos.TwoD && sos.Thickness.Value <= 0) ||
                (Section is ShellSection ss && ss.Thickness.Value <= 0) ||
                (Section is MembraneSection mes && mes.Thickness.Value <= 0))
                throw new CaeException("The section thickness must be larger than 0.");
            //
            if (Section is MassSection mas && mas.Mass.Value < 0)
                throw new CaeException("The mass must be larger or equal to 0.");
            //
            if (Section is CompositeShellSection)
            {
                foreach (string partName in GetPartNamesToSetCompositeShellElementTypes())
                    SetCompositeShellElementTypes(partName);
            }
            // Create
            if (_sectionToEditName == null)
            {
                _controller.AddSectionCommand(Section);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceSectionCommand(_sectionToEditName, Section);
            }
            // If all is successful close the ItemSetSelectionForm - except for OKAddNew
            if (!onOkAddNew) ItemSetDataEditor.SelectionForm.Hide();
        }
        protected override void OnHideOrClose()
        {
            // Close the ItemSetSelectionForm
            ItemSetDataEditor.SelectionForm.Hide();
            //
            base.OnHideOrClose();
        }
        protected override bool OnPrepareForm(string stepName, string sectionToEditName)
        {
            this.btnOkAddNew.Visible = sectionToEditName == null;
            //
            _propertyItemChanged = false;
            _sectionNames = null;
            _sectionToEditName = null;
            _viewSection = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;            
            //
            _sectionNames = _controller.GetSectionNames();
            _sectionToEditName = sectionToEditName;
            string[] materialNames = _controller.GetMaterialNames();
            _materialNames = materialNames ?? new string[0];
            string[] partNames = _controller.GetModelPartNames();
            string[] nodeSetNames = _controller.GetUserNodeSetNames();
            string[] elementSetNames = _controller.GetUserElementSetNames();
            string[] surfaceNames = _controller.GetUserSurfaceNames();
            string[] referencePointNames = _controller.GetModelReferencePointNames();
            //
            if (materialNames == null) materialNames = new string[0];
            if (partNames == null) partNames = new string[0];
            if (nodeSetNames == null) nodeSetNames = new string[0];
            if (elementSetNames == null) elementSetNames = new string[0];
            if (surfaceNames == null) surfaceNames = new string[0];
            if (referencePointNames == null) referencePointNames = new string[0];
            //
            if (_sectionNames == null)
                throw new CaeException("The section names must be defined first.");
            //
            PopulateListOfSections(materialNames, partNames, nodeSetNames, elementSetNames, surfaceNames, referencePointNames);
            // Create new section
            if (_sectionToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewSection = null;
            }
            // Edit existing section
            else
            {
                Section = _controller.GetSection(_sectionToEditName); // to clone
                if (Section.CreationData != null)
                {
                    if (!_controller.Model.IsSectionRegionValid(Section) || // do not use Section.Valid
                        !_controller.Model.RegionValid(Section))
                    {
                        // Region invalid
                        Section.CreationData = null;
                        Section.CreationIds = null;
                        _propertyItemChanged = true;
                    }
                    Section.RegionType = RegionTypeEnum.Selection;
                }
                //
                int selectedId;
                if (_viewSection is ViewSolidSection vss)
                {
                    selectedId = 0;
                    // Check for deleted entities
                    CheckMissingValueRef(ref materialNames, vss.MaterialName, s => { vss.MaterialName = s; });
                    if (vss.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vss.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vss.PartName, s => { vss.PartName = s; });
                    else if (vss.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vss.ElementSetName, s => { vss.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vss.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                }
                else if (_viewSection is ViewCompositeShellSection vcss)
                {
                    selectedId = 2;
                    // Check for deleted entities
                    if (vcss.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vcss.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vcss.PartName, s => { vcss.PartName = s; });
                    else if (vcss.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vcss.ElementSetName, s => { vcss.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vcss.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                }
                else if (_viewSection is ViewShellSection vshs)
                {
                    selectedId = 1;
                    // Check for deleted entities
                    CheckMissingValueRef(ref materialNames, vshs.MaterialName, s => { vshs.MaterialName = s; });
                    if (vshs.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vshs.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vshs.PartName, s => { vshs.PartName = s; });
                    else if (vshs.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vshs.ElementSetName, s => { vshs.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vshs.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                }
                else if (_viewSection is ViewMembraneSection vms)
                {
                    selectedId = 3;
                    // Check for deleted entities
                    CheckMissingValueRef(ref materialNames, vms.MaterialName, s => { vms.MaterialName = s; });
                    if (vms.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (vms.RegionType == RegionTypeEnum.PartName.ToFriendlyString())
                        CheckMissingValueRef(ref partNames, vms.PartName, s => { vms.PartName = s; });
                    else if (vms.RegionType == RegionTypeEnum.ElementSetName.ToFriendlyString())
                        CheckMissingValueRef(ref elementSetNames, vms.ElementSetName, s => { vms.ElementSetName = s; });
                    else throw new NotSupportedException();
                    //
                    vms.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                }
                else if (_viewSection is ViewPointMassSection pms)
                {
                    selectedId = 4;
                    // Check for deleted entities
                    if (pms.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (pms.RegionType == RegionTypeEnum.NodeSetName.ToFriendlyString())
                        CheckMissingValueRef(ref nodeSetNames, pms.ReferencePointName, s => { pms.ReferencePointName = s; });
                    else if (pms.RegionType == RegionTypeEnum.ReferencePointName.ToFriendlyString())
                        CheckMissingValueRef(ref referencePointNames, pms.ReferencePointName, s => { pms.ReferencePointName = s; });
                    else throw new NotSupportedException();
                    //
                    pms.PopulateDropDownLists(null, null, nodeSetNames, null, null, referencePointNames);
                }
                else if (_viewSection is ViewDistributedMassSection dms)
                {
                    selectedId = 5;
                    // Check for deleted entities
                    if (dms.RegionType == RegionTypeEnum.Selection.ToFriendlyString()) { }
                    else if (dms.RegionType == RegionTypeEnum.SurfaceName.ToFriendlyString())
                        CheckMissingValueRef(ref surfaceNames, dms.SurfaceName, s => { dms.SurfaceName = s; });
                    else throw new NotSupportedException();
                    //
                    dms.PopulateDropDownLists(null, null, null, null, surfaceNames, null);
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewSection;
                _preselectIndex = selectedId;
            }
            ShowHideSelectionForm();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods                                                                                                                  
        private void PopulateListOfSections(string[] materialNames, string[] partNames, string[] nodeSetNames,
                                            string[] elementSetNames, string[] surfaceNames, string[] referencePointNames)
        {
            ListViewItem item;
            bool twoD = _controller.Model.Properties.ModelSpace.IsTwoD();
            // Solid section
            item = new ListViewItem("Solid Section");
            if (materialNames.Length > 0)
            {
                bool showThickness = _controller.Model.Properties.ModelSpace == ModelSpaceEnum.PlaneStress ||
                                     _controller.Model.Properties.ModelSpace == ModelSpaceEnum.PlaneStrain;
                SolidSection ss = new SolidSection(GetSectionName("Solid_Section"), materialNames[0], "",
                                                   RegionTypeEnum.Selection, 1, twoD);
                ViewSolidSection vss = new ViewSolidSection(ss);
                vss.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                item.Tag = vss;
            }
            else item.Tag = new ViewError("There is no material defined for the solid section definition.");
            lvTypes.Items.Add(item);
            // Shell section
            item = new ListViewItem("Shell Section");
            if (materialNames.Length > 0)
            {
                ShellSection ss = new ShellSection(GetSectionName("Shell_Section"), materialNames[0], "",
                                                   RegionTypeEnum.Selection, 1, twoD);
                ViewShellSection vss = new ViewShellSection(ss);
                vss.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                item.Tag = vss;
            }
            else item.Tag = new ViewError("There is no material defined for the shell section definition.");
            lvTypes.Items.Add(item);
            // Composite shell section
            item = new ListViewItem("Composite Shell Section");
            if (materialNames.Length > 0)
            {
                List<CompositeLayer> layers = new List<CompositeLayer>() { new CompositeLayer(materialNames[0], 1, 0) };
                CompositeShellSection css = new CompositeShellSection(GetSectionName("Composite_Shell_Section"), "",
                                                                      RegionTypeEnum.Selection, layers, twoD);
                ViewCompositeShellSection vcss = new ViewCompositeShellSection(css);
                vcss.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                item.Tag = vcss;
            }
            else item.Tag = new ViewError("There is no material defined for the composite shell section definition.");
            lvTypes.Items.Add(item);
            // Membrane section
            item = new ListViewItem("Membrane Section");
            if (materialNames.Length > 0)
            {
                MembraneSection ms = new MembraneSection(GetSectionName("Membrane_Section"), materialNames[0], "",
                                                         RegionTypeEnum.Selection, 1, twoD);
                ViewMembraneSection vms = new ViewMembraneSection(ms);
                vms.PopulateDropDownLists(materialNames, partNames, null, elementSetNames, null, null);
                item.Tag = vms;
            }
            else item.Tag = new ViewError("There is no material defined for the membrane section definition.");
            lvTypes.Items.Add(item);
            // Point mass section
            item = new ListViewItem("Point Mass");
            PointMassSection pms = new PointMassSection(GetSectionName("Point_Mass"), "", RegionTypeEnum.Selection, 0, twoD);
            ViewPointMassSection vpms = new ViewPointMassSection(pms);
            vpms.PopulateDropDownLists(null, null, nodeSetNames, null, null, referencePointNames);
            item.Tag = vpms;
            lvTypes.Items.Add(item);
            // Distributed mass section
            item = new ListViewItem("Distributed Mass");
            DistributedMassSection dms = new DistributedMassSection(GetSectionName("Distributed_Mass"), "",
                                                                    RegionTypeEnum.Selection, 0, twoD);
            ViewDistributedMassSection vdms = new ViewDistributedMassSection(dms);
            vdms.PopulateDropDownLists(null, null, null, null, surfaceNames, null);
            item.Tag = vdms;
            lvTypes.Items.Add(item);
        }
        private string GetSectionName(string name)
        {
            return _sectionNames.GetNextNumberedKey(name);
        }
        private bool CheckSectionElementTypes()
        {
            try
            {
                if (Section == null) { }
                else if (Section is SolidSection || Section is ShellSection || Section is MembraneSection ||
                         Section is PointMassSection || Section is DistributedMassSection)
                {
                    // 3D
                    if (_controller.Model.Properties.ModelSpace == ModelSpaceEnum.ThreeD)
                    {
                        if (Section.RegionType == RegionTypeEnum.PartName)
                        {
                            PartType partType = _controller.Model.Mesh.Parts[Section.RegionName].PartType;
                            if (Section is SolidSection && partType == PartType.Solid) return true;
                            else if (Section is ShellSection && partType == PartType.Shell) return true;
                            else if (Section is MembraneSection && partType == PartType.Shell) return true;
                            else return false;  // must be here
                        }
                        else if (Section.RegionType == RegionTypeEnum.NodeSetName)
                        {
                            return true;
                        }
                        else if (Section.RegionType == RegionTypeEnum.ElementSetName)
                        {
                            FeElementSet elementSet = _controller.Model.Mesh.ElementSets[Section.RegionName];
                            if (Section is SolidSection && _controller.AreElementsAllSolidElements3D(elementSet.Labels))
                                return true;
                            else if (Section is ShellSection && _controller.AreElementsAllShellElements(elementSet.Labels))
                                return true;
                            else if (Section is MembraneSection && _controller.AreElementsAllShellElements(elementSet.Labels))
                                return true;
                            else return false;  // must be here
                        }
                        else if (Section.RegionType == RegionTypeEnum.SurfaceName)
                        {
                            return true;
                        }
                        else if (Section.RegionType == RegionTypeEnum.ReferencePointName)
                        {
                            return true;
                        }
                        else if (Section.RegionType == RegionTypeEnum.Selection)
                        {
                            if (Section.CreationIds != null)
                            {
                                BasePart part;
                                int[] ids = Section.CreationIds;
                                HashSet<int> partIds = new HashSet<int>();

                                if (Section is SolidSection)
                                {
                                    if (Section.CreationData.IsGeometryBased()) // solid
                                    {
                                        foreach (int geometryId in ids)
                                        {
                                            partIds.Add(FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2]);
                                        }
                                    }
                                    else throw new NotSupportedException();

                                }
                                else if (Section is ShellSection || Section is MembraneSection)
                                {
                                    foreach (int elementId in ids)  // shell, membrane
                                    {
                                        partIds.Add(_controller.Model.Mesh.Elements[elementId].PartId);
                                    }
                                }
                                else if (Section is PointMassSection) { }
                                else if (Section is DistributedMassSection) { }
                                else throw new NotSupportedException();
                                //
                                foreach (int partId in partIds)
                                {
                                    part = _controller.Model.Mesh.GetPartFromId(partId);
                                    if (part == null) { }
                                    else if (Section is SolidSection && part.PartType != PartType.Solid) return false;
                                    else if (Section is ShellSection && part.PartType != PartType.Shell) return false;
                                    else if (Section is MembraneSection && part.PartType != PartType.Shell) return false;
                                }
                                //
                                return true;
                            }
                        }
                        else throw new NotSupportedException();
                    }
                    // 2D
                    else if (_controller.Model.Properties.ModelSpace.IsTwoD())
                    {
                        if (Section.RegionType == RegionTypeEnum.PartName)
                        {
                            PartType partType = _controller.Model.Mesh.Parts[Section.RegionName].PartType;
                            if (Section is SolidSection && partType == PartType.Shell) return true;
                            else return false;
                        }
                        else if (Section.RegionType == RegionTypeEnum.ElementSetName)
                        {
                            FeElementSet elementSet = _controller.Model.Mesh.ElementSets[Section.RegionName];
                            if (Section is SolidSection && _controller.AreElementsAllShellElements(elementSet.Labels))
                                return true;
                            else return false;
                        }
                        else if (Section.RegionType == RegionTypeEnum.SurfaceName)
                        {
                            return true;
                        }
                        else if (Section.RegionType == RegionTypeEnum.Selection)
                        {
                            if (Section.CreationIds != null)
                            {
                                int partId;
                                BasePart part;
                                int[] geometryIds = Section.CreationIds;
                                foreach (int geometryId in geometryIds)
                                {
                                    partId = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2];
                                    part = _controller.Model.Mesh.GetPartFromId(partId);
                                    if (part == null) { }
                                    else if (Section is SolidSection && part.PartType != PartType.Shell) return false;
                                }
                                return true;
                            }
                        }
                        else throw new NotSupportedException();
                    }
                }
                else throw new NotSupportedException();
                //
                return false;
            }
            catch
            {
                return false;
            }
        }
        private void HighlightSection()
        {
            try
            {
                _controller.ClearSelectionHistory();
                // A selected layer shows its material axes over the outline of the region
                if (Section is CompositeShellSection css && tcProperties.SelectedTab == tpDataPoints &&
                    dgvData.CurrentRow != null && dgvData.CurrentRow.Index < css.Layers.Count)
                {
                    if (css.RegionType == RegionTypeEnum.Selection) SetSelectItem();
                    if (css.RegionType == RegionTypeEnum.Selection && css.CreationData != null)
                        _controller.Selection = css.CreationData.DeepCopy(true, false);
                    _controller.HighlightCompositeLayer(css, dgvData.CurrentRow.Index);
                }
                else if (_viewSection == null) { }
                else if (_viewSection is ViewSolidSection || _viewSection is ViewShellSection ||
                         _viewSection is ViewCompositeShellSection || _viewSection is ViewMembraneSection ||
                         _viewSection is ViewPointMassSection || _viewSection is ViewDistributedMassSection)
                {
                    if (Section.RegionType == RegionTypeEnum.PartName || Section.RegionType == RegionTypeEnum.NodeSetName ||
                        Section.RegionType == RegionTypeEnum.ElementSetName || Section.RegionType == RegionTypeEnum.SurfaceName ||
                        Section.RegionType == RegionTypeEnum.ReferencePointName)
                    {
                        _controller.Highlight3DObjects(new object[] { Section.RegionName });
                    }
                    else if (Section.RegionType == RegionTypeEnum.Selection)
                    {
                        SetSelectItem();
                        //
                        if (Section.CreationData != null)
                        {
                            bool backfaceCulling = _viewSection is ViewSolidSection;
                            _controller.Selection = Section.CreationData.DeepCopy(true, false);
                            _controller.HighlightSelection(true, backfaceCulling);
                        }
                    }
                    else throw new NotSupportedException();
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
        private void SetLayersBinding(ViewCompositeShellSection viewCompositeShellSection)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = viewCompositeShellSection.Layers;
            binding.AddingNew += Binding_AddingNew;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
            // The material is picked from the materials of the model, and a missing one is kept to be seen
            DataGridViewColumn column = dgvData.Columns[nameof(CompositeLayerDataPoint.MaterialName)];
            if (column != null)
            {
                DataGridViewComboBoxColumn materialColumn = new DataGridViewComboBoxColumn();
                materialColumn.DataPropertyName = column.DataPropertyName;
                materialColumn.Name = column.Name;
                materialColumn.HeaderText = column.HeaderText;
                materialColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
                materialColumn.FlatStyle = FlatStyle.Flat;
                HashSet<string> materialNames = new HashSet<string>(_materialNames);
                foreach (CompositeLayerDataPoint layer in viewCompositeShellSection.Layers)
                {
                    if (layer.MaterialName != null) materialNames.Add(layer.MaterialName);
                }
                materialColumn.Items.AddRange(materialNames.ToArray());
                int index = column.Index;
                dgvData.Columns.RemoveAt(index);
                dgvData.Columns.Insert(index, materialColumn);
            }
            // The layers are named by their number
            DataGridViewTextBoxColumn layerColumn = new DataGridViewTextBoxColumn();
            layerColumn.Name = _layerColumnName;
            layerColumn.HeaderText = "Layer";
            layerColumn.ReadOnly = true;
            layerColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            layerColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvData.Columns.Insert(0, layerColumn);
            //
            SetGridViewUnit(nameof(CompositeLayerDataPoint.ThicknessEq), _controller.Model.UnitSystem.LengthUnitAbbreviation,
                            new StringLengthFromConverter());
            SetGridViewUnit(nameof(CompositeLayerDataPoint.AngleEq), StringAngleDegConverter.GetUnitAbbreviation(),
                            new StringAngleDegFromConverter());
            dgvData.BuildAutocompleteMenu(_controller.GetAllParameterNames());
        }
        // The layers from layer 1 at the bottom, on the negative side of the normal (axis 3), to the top, each as high as
        // it is thick, as a cross section hatched at 45 degrees, at -45 degrees and at both in turn, with the layer of
        // the selected row outlined
        private void DrawLayup(Graphics graphics, CompositeShellSection section)
        {
            Rectangle stack;
            Rectangle[] bars = GetLayupBars(section, out stack);
            if (bars == null) return;
            //
            int numOfRows = section.Layers.Count;
            int selectedRow = dgvData.CurrentRow != null ? dgvData.CurrentRow.Index : -1;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            // Axis 3 of the material axes points to the top
            using (Pen arrowPen = new Pen(Color.Black, 1.5f))
            {
                arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 5);
                int x = stack.Left - _layupArrowWidth + 4;
                graphics.DrawLine(arrowPen, x, stack.Bottom, x, stack.Top);
                TextRenderer.DrawText(graphics, "3", Font, new Point(x + 4, stack.Top - 2), Color.Black);
            }
            //
            double hatchPhase = 0;
            double[] risingPhases = Enumerable.Repeat(double.NaN, bars.Length).ToArray();
            double[] fallingPhases = Enumerable.Repeat(double.NaN, bars.Length).ToArray();
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height <= 0) continue;
                //
                if (i < numOfRows)
                {
                    if (i % 3 != 1) risingPhases[i] = hatchPhase;
                    if (i % 3 != 0) fallingPhases[i] = hatchPhase;
                }
                // A mirrored layer is the mirror image of the layer it mirrors: its lines turn the other way and meet
                // its top where those meet their bottom
                else
                {
                    int mirroredLayer = GetLayupRow(i, bars.Length, numOfRows);
                    risingPhases[i] = fallingPhases[mirroredLayer] - bars[i].Height;
                    fallingPhases[i] = risingPhases[mirroredLayer] + bars[i].Height;
                }
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    graphics.FillRectangle(brush, bars[i]);
                hatchPhase = DrawLayerHatch(graphics, bars[i], risingPhases[i], fallingPhases[i]);
                using (Pen pen = new Pen(Color.Black)) graphics.DrawRectangle(pen, bars[i]);
            }
            // The selected layer is outlined over the borders of its neighbours
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height <= 0 || selectedRow < 0 || GetLayupRow(i, bars.Length, numOfRows) != selectedRow)
                    continue;
                using (Pen pen = new Pen(SystemColors.Highlight, 3)) graphics.DrawRectangle(pen, bars[i]);
            }
        }
        // The bar of every solver layer, from layer 1 at the bottom, empty for a layer too thin to be drawn
        private Rectangle[] GetLayupBars(CompositeShellSection section, out Rectangle stack)
        {
            Rectangle area = pnlLayup.ClientRectangle;
            area.Inflate(-16, -12);
            stack = new Rectangle(area.Left + _layupArrowWidth, area.Top, area.Width - _layupArrowWidth, area.Height);
            List<CompositeLayer> layers = section.GetSolverLayers();
            if (layers.Count == 0 || stack.Width <= 0 || stack.Height <= 0) return null;
            //
            double[] thicknesses = new double[layers.Count];
            double totalThickness = 0;
            for (int i = 0; i < layers.Count; i++)
            {
                try { thicknesses[i] = Math.Max(0, layers[i].Thickness.Value); }
                catch { thicknesses[i] = 0; }
                totalThickness += thicknesses[i];
            }
            if (totalThickness <= 0) return null;
            //
            Rectangle[] bars = new Rectangle[layers.Count];
            double bottom = stack.Bottom;
            double height;
            int top;
            for (int i = 0; i < layers.Count; i++)
            {
                height = stack.Height * thicknesses[i] / totalThickness;
                top = (int)Math.Round(bottom - height);
                bars[i] = new Rectangle(stack.Left, top, stack.Width - 1, (int)Math.Round(bottom) - top);
                bottom -= height;
            }
            return bars;
        }
        // The row of a solver layer: a mirrored layer of a symmetric layup belongs to the row it mirrors
        private int GetLayupRow(int layerIndex, int numOfLayers, int numOfRows)
        {
            return layerIndex < numOfRows ? layerIndex : numOfLayers - 1 - layerIndex;
        }
        private int GetLayupRowAt(Point location)
        {
            CompositeShellSection section = Section as CompositeShellSection;
            if (section == null) return -1;
            //
            Rectangle stack;
            Rectangle[] bars = GetLayupBars(section, out stack);
            if (bars == null) return -1;
            //
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height > 0 && bars[i].Left <= location.X && location.X <= bars[i].Right &&
                    bars[i].Top <= location.Y && location.Y < bars[i].Bottom)
                    return GetLayupRow(i, bars.Length, section.Layers.Count);
            }
            return -1;
        }
        // Lines at 45 degrees rising to the right that cross the bottom of the bar at the rising phase from its left side,
        // and lines at -45 degrees that cross it at the falling phase, either left out by NaN; returns the phase farthest
        // from where they cross its top, at which the layer above starts so that no line runs on into it
        private double DrawLayerHatch(Graphics graphics, Rectangle bar, double risingPhase, double fallingPhase)
        {
            double spacing = _layupHatchSpacing;
            double height = bar.Height;
            bool rising = !double.IsNaN(risingPhase);
            bool falling = !double.IsNaN(fallingPhase);
            double risingTopPhase = rising ? ((risingPhase + height) % spacing + spacing) % spacing : 0;
            double fallingTopPhase = falling ? ((fallingPhase - height) % spacing + spacing) % spacing : 0;
            double nextPhase;
            if (rising && falling)
            {
                double gap = ((fallingTopPhase - risingTopPhase) % spacing + spacing) % spacing;
                if (gap >= spacing - gap) nextPhase = risingTopPhase + gap / 2;
                else nextPhase = fallingTopPhase + (spacing - gap) / 2;
            }
            else if (rising) nextPhase = risingTopPhase + spacing / 2;
            else if (falling) nextPhase = fallingTopPhase + spacing / 2;
            else nextPhase = 0;
            if (bar.Width < 2 || bar.Height < 2) return nextPhase;
            //
            int numOfBackSteps = (int)Math.Ceiling((height + spacing) / spacing) + 1;
            double end = bar.Right + height + spacing;
            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
            graphics.SetClip(bar);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.Black, 1))
            {
                if (rising)
                {
                    for (double x = bar.Left + risingPhase % spacing - numOfBackSteps * spacing; x <= end; x += spacing)
                        graphics.DrawLine(pen, (float)x, bar.Bottom, (float)(x + height), bar.Top);
                }
                if (falling)
                {
                    for (double x = bar.Left + fallingPhase % spacing - numOfBackSteps * spacing; x <= end; x += spacing)
                        graphics.DrawLine(pen, (float)x, bar.Bottom, (float)(x - height), bar.Top);
                }
            }
            graphics.Restore(state);
            return nextPhase;
        }
        private void SetGridViewUnit(string columnName, string unit, TypeConverter converter)
        {
            DataGridViewColumn column = dgvData.Columns[columnName];
            if (column == null) return;
            //
            if (column.HeaderText != null) column.HeaderText = column.HeaderText.Replace("?", unit);
            column.HeaderCell.Style.Alignment = System.Windows.Forms.DataGridViewContentAlignment.BottomCenter;
            column.Tag = converter;
        }
        private string[] GetPartNamesToSetCompositeShellElementTypes()
        {
            string[] partNamesToChangeElementType;
            string[] partNamesToMesh;
            _controller.Model.GetCompositeShellElementTypeParts(GetRegionElementIds(), out partNamesToChangeElementType,
                                                                out partNamesToMesh);
            string text = "A composite shell section takes S8R and S6 elements only.";
            if (partNamesToMesh.Length > 0)
            {
                text += " Mesh " + GetPartNamesText(partNamesToMesh) + " with parabolic shell elements";
                if (partNamesToChangeElementType.Length > 0)
                    text += " and set the element type of " + GetPartNamesText(partNamesToChangeElementType) +
                            " to S8R and S6";
                text += "." + Environment.NewLine + Environment.NewLine +
                        "OK - save the section, which stays invalid until then" + Environment.NewLine +
                        "Cancel - return to the dialog";
                if (MessageBoxes.ShowWarningQuestionOKCancel(text) == DialogResult.Cancel)
                    throw new CaeException("BreakOnApply");
            }
            else if (partNamesToChangeElementType.Length > 0)
            {
                text += " Set the element type of " + GetPartNamesText(partNamesToChangeElementType) + " to S8R and " +
                        "S6? The element type applies to all elements of a part." + Environment.NewLine +
                        Environment.NewLine +
                        "Yes - change the element type and save the section" + Environment.NewLine +
                        "No - save the section, which stays invalid until the element type is changed" +
                        Environment.NewLine +
                        "Cancel - return to the dialog";
                DialogResult result = MessageBoxes.ShowWarningQuestionYesNoCancel(text);
                if (result == DialogResult.Cancel) throw new CaeException("BreakOnApply");
                else if (result == DialogResult.Yes) return partNamesToChangeElementType;
            }
            return new string[0];
        }
        private string GetPartNamesText(string[] partNames)
        {
            return (partNames.Length == 1 ? "the part " : "the parts ") + string.Join(", ", partNames);
        }
        private void SetCompositeShellElementTypes(string partName)
        {
            PartProperties partProperties = _controller.GetModelPart(partName).GetProperties();
            if (partProperties.ParabolicQuadType != FeElementTypeParabolicQuad.None)
                partProperties.ParabolicQuadType = FeElementTypeParabolicQuad.S8R;
            if (partProperties.ParabolicTriaType != FeElementTypeParabolicTria.None)
                partProperties.ParabolicTriaType = FeElementTypeParabolicTria.S6;
            _controller.ReplaceModelPartPropertiesCommand(partName, partProperties);
        }
        private int[] GetRegionElementIds()
        {
            Section section = Section;
            if (section.RegionType == RegionTypeEnum.Selection)
            {
                if (section.CreationIds == null) return new int[0];
                if (section.CreationData == null || section.CreationData.SelectItem != vtkSelectItem.Part)
                    return section.CreationIds;
                //
                List<int> elementIds = new List<int>();
                foreach (int geometryId in section.CreationIds)
                {
                    BasePart part = _controller.Model.Mesh.GetPartFromId(FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2]);
                    if (part != null) elementIds.AddRange(part.Labels);
                }
                return elementIds.ToArray();
            }
            return _controller.Model.GetElementIdsOfRegion(section);
        }
        private void ShowHideSelectionForm()
        {
            if (Section != null && Section.RegionType == RegionTypeEnum.Selection)
                ItemSetDataEditor.SelectionForm.ShowIfHidden(this.Owner);
            else
                ItemSetDataEditor.SelectionForm.Hide();
            //
            SetSelectItem();
        }
        private void SetSelectItem()
        {
            if (Section != null && Section.RegionType == RegionTypeEnum.Selection)
            {
                if (Section is null) { }
                else if (Section is SolidSection) _controller.SetSelectItemToPart();
                else if (Section is ShellSection) _controller.SetSelectItemToElement();
                else if (Section is MembraneSection) _controller.SetSelectItemToElement();
                else if (Section is PointMassSection) _controller.SetSelectItemToNode();
                else if (Section is DistributedMassSection) _controller.SetSelectItemToSurface();
                else throw new NotSupportedException();
            }
            else _controller.SetSelectByToOff();
        }
        //
        public void SelectionChanged(int[] ids)
        {
            if (Section != null && Section.RegionType == RegionTypeEnum.Selection)
            {
                if (Section is SolidSection || Section is ShellSection || Section is MembraneSection ||
                    Section is PointMassSection || Section is DistributedMassSection)
                {
                    Section.CreationIds = ids;
                    Section.CreationData = _controller.Selection.DeepCopy(true, false);
                    //
                    propertyGrid.Refresh();
                    //
                    _propertyItemChanged = true;
                    //
                    if (ids.Length != 0) HighlightSection(); // this will redraw the selection with correct backfaceCulling
                }
                else throw new NotSupportedException();
            }
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightSection();
        }
        // IFormItemSetDataParent
        public bool IsSelectionGeometryBased()
        {
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            Section section = Section;
            //
            if (section.CreationData != null) return section.CreationData.IsGeometryBased();
            else return true;
        }
        public bool IsGeometrySelectionIdBased()
        {
            bool defaultMode = _controller.Settings.Pre.GeometrySelectMode == GeometrySelectModeEnum.SelectId;
            // Prepare ItemSetDataEditor - prepare Geometry or Mesh based selection
            Section section = Section;
            //
            if (section.CreationData != null && IsSelectionGeometryBased())
                return section.CreationData.IsGeometryIdBased(defaultMode);
            else return defaultMode;
        }
    }
}
