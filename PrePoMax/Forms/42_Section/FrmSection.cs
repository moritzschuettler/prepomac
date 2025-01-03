using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using CaeMesh;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    class FrmSection : UserControls.FrmPropertyListView, IFormBase, IFormItemSetDataParent, IFormHighlight
    {
        // Variables                                                                                                                
        private string[] _sectionNames;
        private string _sectionToEditName;
        private ViewSection _viewSection;
        private Controller _controller;


        // Properties                                                                                                               
        public Section Section
        {
            get { return _viewSection != null ? _viewSection.GetBase() : null; }
            set
            {
                if (value is SolidSection sose) _viewSection = new ViewSolidSection(sose.DeepClone());
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
        }
        private void InitializeComponent()
        {
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // FrmSection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 461);
            this.Name = "FrmSection";
            this.Text = "Edit Section";
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
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
            if ((Section is SolidSection sos && sos.TwoD && sos.Thickness.Value <= 0) ||
                (Section is ShellSection ss && ss.Thickness.Value <= 0) ||
                (Section is MembraneSection mes && mes.Thickness.Value <= 0))
                throw new CaeException("The section thickness must be larger than 0.");
            //
            if (Section is MassSection mas && mas.Mass.Value < 0)
                throw new CaeException("The mass must be larger or equal to 0.");
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
                    selectedId = 2;
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
                    selectedId = 3;
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
                    selectedId = 4;
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
                                int partId;
                                BasePart part;
                                int[] geometryIds = Section.CreationIds;
                                foreach (int geometryId in geometryIds)
                                {
                                    partId = FeMesh.GetItemTypePartIdsFromGeometryId(geometryId)[2];
                                    part = _controller.Model.Mesh.GetPartFromId(partId);
                                    if (part == null) { }
                                    else if (Section is SolidSection && part.PartType != PartType.Solid) return false;
                                    else if (Section is ShellSection && part.PartType != PartType.Shell) return false;
                                    else if (Section is MembraneSection && part.PartType != PartType.Shell) return false;
                                }
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
                //
                if (_viewSection == null) { }
                else if (_viewSection is ViewSolidSection || _viewSection is ViewShellSection ||
                         _viewSection is ViewMembraneSection || _viewSection is ViewPointMassSection ||
                         _viewSection is ViewDistributedMassSection)
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
                            _controller.Selection = Section.CreationData.DeepClone();
                            _controller.HighlightSelection(true, backfaceCulling);
                        }
                    }
                    else throw new NotSupportedException();
                }
                else throw new NotSupportedException();
            }
            catch { }
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
                else if (Section is ShellSection) _controller.SetSelectItemToGeometry();
                else if (Section is MembraneSection) _controller.SetSelectItemToGeometry();
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
                    Section.CreationData = _controller.Selection.DeepClone();
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
