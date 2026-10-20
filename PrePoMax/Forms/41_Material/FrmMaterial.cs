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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CaeModel;
using CaeGlobals;
using PrePoMax.PropertyViews;
using System.Runtime.CompilerServices;
using System.Reflection;
using CaeResults;
using CaeMesh;

namespace PrePoMax.Forms
{
    public partial class FrmMaterial : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables                                                                                                                
        private string[] _materialNames;
        private string _materialToEditName;
        private Material _material;
        private Controller _controller;
        private TabPage[] _pages;
        private bool _useSimpleEditor;      // a switch to change the form into a simple material editor form
        private bool _preview;              // a switch to change the form into a preview material form
        private bool _updatingCompositeControls;


        // Properties                                                                                                               
        public Material Material
        {
            get { return _material; }
            set
            {
                _material = value.DeepClone();
                // Save selected property
                int selectedId = -1;
                if (lvAddedProperties.SelectedIndices.Count > 0)
                    selectedId = lvAddedProperties.SelectedIndices[0];
                //
                ShowMaterial();
                // Select previous property
                if (selectedId >= 0 && selectedId < lvAddedProperties.Items.Count) 
                    lvAddedProperties.Items[selectedId].Selected = true;
            }
        }
        public bool UseSimpleEditor { get { return _useSimpleEditor; } set { _useSimpleEditor = value.DeepClone(); } }


        // Constructors                                                                                                             
        public FrmMaterial(Controller controller)
        {
            InitializeComponent();
            if (tvProperties.Nodes.Find("Engineering Constants", true).Length == 0)
            {
                TreeNode engineeringConstantsNode = new TreeNode("Engineering Constants");
                engineeringConstantsNode.Name = "Engineering Constants";
                engineeringConstantsNode.ToolTipText = "Orthotropic elasticity defined by engineering constants.";
                tvProperties.Nodes.Find("Elasticity", true)[0].Nodes.Add(engineeringConstantsNode);
            }
            if (tvProperties.Nodes.Find("Lamina", true).Length == 0)
            {
                TreeNode laminaNode = new TreeNode("Lamina");
                laminaNode.Name = "Lamina";
                laminaNode.ToolTipText = "Plane-stress orthotropic elasticity defined by lamina constants.";
                tvProperties.Nodes.Find("Elasticity", true)[0].Nodes.Add(laminaNode);
            }
            //
            _controller = controller;
            _material = null;
            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages)
            {
                tabPage.Paint += TabPage_Paint;
                _pages[i++] = tabPage;
            }
            //
            ClearControls();
            //
            _useSimpleEditor = false;
            //
            ttText.SetToolTip(btnAdd, "Add model");
            ttText.SetToolTip(btnMoveUp, "Move up");
            ttText.SetToolTip(btnMoveDown, "Move down");
            ttText.SetToolTip(btnRemove, "Remove model");
            //
            FillCompositeMaterialTypes();
            //
            propertyGrid.SetSelectFirstCategory();
        }


        // Event handlers                                                                                                           
        private void FrmMaterial_VisibleChanged(object sender, EventArgs e)
        {
            cbTemperatureDependent.Enabled = !(_useSimpleEditor || _preview);
            tvProperties.Visible = !_useSimpleEditor;
            lvAddedProperties.Visible = !_useSimpleEditor;
            //
            if (Visible) { }
            else
            {
                dgvData.HidePlot();
            }
        }
        private void TabPage_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush fillBrush = new SolidBrush(((TabPage)sender).BackColor);
            e.Graphics.FillRectangle(fillBrush, e.ClipRectangle);
            // Enable copy/paste without first selecting the cell 0,0
            if (sender == tpDataPoints)
            {
                ActiveControl = dgvData;
                dgvData[0, 0].Selected = true;
            }
        }
        private void tvProperties_DoubleClick(object sender, EventArgs e)
        {
            btnAdd_Click(null, null);
        }
        private void tbDescription_TextChanged(object sender, EventArgs e)
        {
            _propertyItemChanged = true;
        }
        private void cbTemperatureDependent_CheckedChanged(object sender, EventArgs e)
        {
            HideShowTemperature();
            propertyGrid.Refresh();
            _propertyItemChanged = true;
        }
        private void cbCompositeMaterialType_SelectedIndexChanged(object sender, EventArgs e)
        {
            HideShowWidth();
            //
            if (_updatingCompositeControls) return;
            _propertyItemChanged = true;
        }
        private void tbWidth_TextChanged(object sender, EventArgs e)
        {
            if (_updatingCompositeControls) return;
            _propertyItemChanged = true;
        }
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!_preview && tvProperties.SelectedNodes != null)
            {
                foreach (TreeNode treeNode in tvProperties.SelectedNodes)
                {
                    if (treeNode.Tag != null)
                    {
                        string propertyText = treeNode.Text;
                        //
                        ListViewItem existingItem = null;
                        if (lvAddedProperties.Items.Count > 0)
                            existingItem = lvAddedProperties.FindItemWithText(propertyText, true, 0, false);
                        if (existingItem == null &&
                            (treeNode.Tag is Elastic ||
                             treeNode.Tag is ElasticEngineeringConstants ||
                             treeNode.Tag is ElasticLamina ||
                             treeNode.Tag is Hyperelastic ||
                             treeNode.Tag is HyperelasticNeoHooke ||
                             treeNode.Tag is HyperelasticOgden ||
                             treeNode.Tag is Hyperfoam))
                        {
                            foreach (ListViewItem addedItem in lvAddedProperties.Items)
                            {
                                if (addedItem.Tag is ViewElastic ||
                                    addedItem.Tag is ViewElasticEngineeringConstants ||
                                    addedItem.Tag is ViewElasticLamina ||
                                    addedItem.Tag is ViewElasticWithDensity ||
                                    addedItem.Tag is ViewHyperelastic ||
                                    addedItem.Tag is ViewHyperelasticNeoHooke ||
                                    addedItem.Tag is ViewHyperelasticOgden ||
                                    addedItem.Tag is ViewHyperfoam)
                                {
                                    existingItem = addedItem;
                                    MessageBoxes.ShowWarning("A material can contain only one elastic property.");
                                    break;
                                }
                            }
                        }
                        //
                        if (existingItem == null)
                        {
                            ListViewItem item = new ListViewItem(propertyText);
                            if (treeNode.Tag is MaterialProperty mp)
                            {
                                if (mp is Density de)
                                    item.Tag = new ViewDensity(de.DeepClone());
                                else if (mp is Hyperelastic hyp)
                                    item.Tag = new ViewHyperelastic(hyp.DeepClone());
                                else if (mp is HyperelasticNeoHooke nh)
                                    item.Tag = new ViewHyperelasticNeoHooke(nh.DeepClone());
                                else if (mp is HyperelasticOgden ogd)
                                    item.Tag = new ViewHyperelasticOgden(ogd.DeepClone());
                                else if (mp is Hyperfoam hf)
                                    item.Tag = new ViewHyperfoam(hf.DeepClone());
                                else if (mp is SlipWear sw)
                                    item.Tag = new ViewSlipWear(sw.DeepClone());
                                else if (mp is Elastic el)
                                    item.Tag = new ViewElastic(el.DeepClone());
                                else if (mp is ElasticEngineeringConstants eec)
                                    item.Tag = new ViewElasticEngineeringConstants(eec.DeepClone());
                                else if (mp is ElasticLamina ela)
                                    item.Tag = new ViewElasticLamina(ela.DeepClone());
                                else if (mp is Plastic pl)
                                    item.Tag = new ViewPlastic(pl.DeepClone());
                                else if (mp is ThermalExpansion te)
                                    item.Tag = new ViewThermalExpansion(te.DeepClone(), cbTemperatureDependent.Checked);
                                else if (mp is ThermalConductivity tc)
                                    item.Tag = new ViewThermalConductivity(tc.DeepClone());
                                else if (mp is SpecificHeat sh)
                                    item.Tag = new ViewSpecificHeat(sh.DeepClone());
                                else if (mp is CompositeStrength cs)
                                    item.Tag = new ViewCompositeStrength(cs.DeepClone());
                                else throw new NotSupportedException();
                            }
                            else throw new NotSupportedException();
                            //
                            lvAddedProperties.Items.Add(item);
                            int id = lvAddedProperties.Items.IndexOf(item);
                            lvAddedProperties.Items[id].Selected = true;
                            lvAddedProperties.Select();
                        }
                    }
                }
                tvProperties.SelectedNodes.Clear();
            }
            _propertyItemChanged = true;
        }
        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            try
            {
                int currentIndex = lvAddedProperties.PossiblySelectedItems[0].Index;
                ListViewItem item = lvAddedProperties.Items[currentIndex];
                if (currentIndex > 0)
                {
                    lvAddedProperties.Items.RemoveAt(currentIndex);
                    lvAddedProperties.Items.Insert(currentIndex - 1, item);
                }
                _propertyItemChanged = true;
            }
            catch
            { }
        }
        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            try
            {
                int currentIndex = lvAddedProperties.PossiblySelectedItems[0].Index;
                ListViewItem item = lvAddedProperties.Items[currentIndex];
                if (currentIndex < lvAddedProperties.Items.Count - 1)
                {
                    lvAddedProperties.Items.RemoveAt(currentIndex);
                    lvAddedProperties.Items.Insert(currentIndex + 1, item);
                }
                _propertyItemChanged = true;
            }
            catch
            { }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lvAddedProperties.PossiblySelectedItems.Count == 1)
            {
                ListViewItem item = lvAddedProperties.PossiblySelectedItems[0];
                int index = item.Index;
                if (index == lvAddedProperties.Items.Count - 1) index--;
                lvAddedProperties.Items.Remove(item);
                //
                if (lvAddedProperties.Items.Count > 0) lvAddedProperties.Items[index].Selected = true;
                else ClearControls();
                //
                _propertyItemChanged = true;
            }
        }
        //
        private void lvAddedProperties_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvAddedProperties.PossiblySelectedItems.Count == 1)
            {
                // Clear
                dgvData.DataSource = null;      
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewDensity vd)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(vd.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElastic ve)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(ve.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElasticEngineeringConstants veec)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(veec.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElasticLamina vela)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(vela.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElasticWithDensity)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewPlastic vp)
                {
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    //
                    SetDataGridViewBinding(vp.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewThermalExpansion vte)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(vte.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewThermalConductivity vtc)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(vtc.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewSpecificHeat vsh)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    tcProperties.TabPages.Add(_pages[1]);   // data points
                    //
                    SetDataGridViewBinding(vsh.DataPoints);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewSlipWear vsw)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelastic ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelasticNeoHooke ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelasticOgden ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperfoam)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewCompositeStrength)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                }
                else throw new NotSupportedException();
                //
                propertyGrid.SelectedObject = lvAddedProperties.PossiblySelectedItems[0].Tag;
                //
                SetAllGridViewUnits();
                //
                HideShowTemperature();
            }
            lvAddedProperties.Select();
        }        
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
        }
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            propertyGrid.Refresh();
            _propertyItemChanged = true;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Add();
                //
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            
        }
        private void btnOKAddNew_Click(object sender, EventArgs e)
        {
            try
            {
                Add();
                //
                
                PrepareForm(null, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            _useSimpleEditor = false;
            //
            Hide();
        }
        private void FrmMaterial_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                btnCancel_Click(null, null);
            }
        }
    

        // Methods                                                                                                                  
        public bool PrepareForm(string stepName, string materialToEditName)
        {
            _propertyItemChanged = false;
            _materialNames = null;
            _materialToEditName = null;
            _material = null;
            _preview = false;
            lvAddedProperties.Items.Clear();
            ClearControls();
            SetControlStates();
            //
            _materialNames = _controller.GetMaterialNames();
            lWidthUnit.Text = _controller.Model.UnitSystem.LengthUnitAbbreviation;
            _materialToEditName = materialToEditName;
            this.btnOKAddNew.Visible = _materialToEditName == null; // must be here
            // Initialize material properties
            TreeNode node;
            node = tvProperties.Nodes.Find("Density", true)[0];
            node.Tag = new Density(new double[][] { new double[] { 0, 0 } });
            node = tvProperties.Nodes.Find("Slip Wear", true)[0];
            node.Tag = new SlipWear(0, 0);
            node = tvProperties.Nodes.Find("Elastic", true)[0];
            node.Tag = new Elastic(new double[][] { new double[] { 0, 0, 0 } });
            node = tvProperties.Nodes.Find("Engineering Constants", true)[0];
            node.Tag = new ElasticEngineeringConstants(new double[][]
            {
                new double[ElasticEngineeringConstants.NumberOfValues]
            });
            node = tvProperties.Nodes.Find("Lamina", true)[0];
            node.Tag = new ElasticLamina(new double[][]
            {
                new double[ElasticLamina.NumberOfValues]
            });
            node = tvProperties.Nodes.Find("Plastic", true)[0];
            node.Tag = new Plastic(new double[][] { new double[] { 0, 0, 0 } });
            node = tvProperties.Nodes.Find("Thermal Expansion", true)[0];
            node.Tag = new ThermalExpansion(new double[][] { new double[] { 0, 0 } });
            node = tvProperties.Nodes.Find("Thermal Conductivity", true)[0];
            node.Tag = new ThermalConductivity(new double[][] { new double[] { 0, 0 } });
            node = tvProperties.Nodes.Find("Specific Heat", true)[0];
            node.Tag = new SpecificHeat(new double[][] { new double[] { 0, 0 } });
            node = tvProperties.Nodes.Find("Composite Strength", true)[0];
            node.Tag = new CompositeStrength(0, 0, 0, 0, 0, 0, CompositeStrength.DefaultTsaiWuCoefficient);
            node = tvProperties.Nodes.Find("Hyperelastic - Mooney-Rivlin", true)[0];
            node.Tag = new Hyperelastic(0, 0, 0);
            node = tvProperties.Nodes.Find("Hyperelastic - Neo Hooke", true)[0];
            node.Tag = new HyperelasticNeoHooke(0, 0);
            node = tvProperties.Nodes.Find("Hyperelastic - Ogden", true)[0];
            node.Tag = new HyperelasticOgden(1, 1, 2, 0, 0, 0, 0, 0, 0, 0);
            node = tvProperties.Nodes.Find("Hyperfoam - Ogden", true)[0];
            node.Tag = new Hyperfoam(2, 1, 2, 0, 0, -2, 0, 0, 0, 0);
            //
            tvProperties.ExpandAll();
            //
            if (_materialToEditName == null)
            {
                _material = null;
                tbName.Text = GetMaterialName();
                tbDescription.Text = "";
                cbTemperatureDependent.Checked = false;
                _updatingCompositeControls = true;
                cbCompositeMaterialType.SelectedIndex = (int)CompositeMaterialType.None;
                tbWidth.Text = "";
                _updatingCompositeControls = false;
                HideShowWidth();
            }
            else
            {
                Material = _controller.GetMaterial(_materialToEditName); // to clone
                ShowMaterial();
            }
            // Simple material editor
            int delta;
            if (_useSimpleEditor)
            {                
                if (_materialToEditName == null)
                {
                    ViewMaterialProperty view = new ViewElasticWithDensity(new ElasticWithDensity(0, 0, 0));
                    ListViewItem item = new ListViewItem(view.Name);
                    item.Tag = view;
                    lvAddedProperties.Items.Add(item);
                    lvAddedProperties.Items[0].Selected = true;
                    lvAddedProperties.Select();
                    lvAddedProperties_SelectedIndexChanged(null, null);
                }
                delta = tcProperties.Top - labAvailable.Top;
                tcProperties.Top = labAvailable.Top;
                tcProperties.Height += delta;
                this.Height -= delta;
            }
            else
            {
                delta = (tvProperties.Bottom + 5) - tcProperties.Top;
                tcProperties.Top = tvProperties.Bottom + 5;
                tcProperties.Height -= delta;
                this.Height += delta;
            }
            //
            _controller.SetSelectByToOff();
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            dgvData.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }
        public void ShowMaterial()
        {
            lvAddedProperties.Items.Clear();
            //
            tbName.Text = _material.Name;
            tbDescription.Text = _material.Description;
            cbTemperatureDependent.Checked = _material.TemperatureDependent;
            // The type and the width of the composite material are used by the draping simulation
            _updatingCompositeControls = true;
            cbCompositeMaterialType.SelectedIndex = (int)_material.CompositeMaterialType;
            tbWidth.Text = _material.Width.HasValue ? _material.Width.Value.ToString() : "";
            _updatingCompositeControls = false;
            HideShowWidth();
            //
            if (_material.Properties.Count > 0)
            {
                ListViewItem item;
                ViewMaterialProperty view = null;
                foreach (var property in _material.Properties)
                {
                    if (property is Density den) view = new ViewDensity(den);
                    else if (property is SlipWear sw) view = new ViewSlipWear(sw);
                    else if (property is Elastic el) view = new ViewElastic(el);
                    else if (property is ElasticEngineeringConstants eec)
                        view = new ViewElasticEngineeringConstants(eec);
                    else if (property is ElasticLamina ela)
                        view = new ViewElasticLamina(ela);
                    else if (property is ElasticWithDensity ewd)
                    {
                        view = new ViewElasticWithDensity(ewd);
                        _useSimpleEditor = true;
                    }
                    else if (property is Plastic pl) view = new ViewPlastic(pl);
                    else if (property is ThermalExpansion te)
                        view = new ViewThermalExpansion(te, cbTemperatureDependent.Checked);
                    else if (property is ThermalConductivity tc) view = new ViewThermalConductivity(tc);
                    else if (property is SpecificHeat sh) view = new ViewSpecificHeat(sh);
                    else if (property is CompositeStrength cs) view = new ViewCompositeStrength(cs);
                    else if (property is Hyperelastic hyp) view = new ViewHyperelastic(hyp);
                    else if (property is HyperelasticNeoHooke nh) view = new ViewHyperelasticNeoHooke(nh);
                    else if (property is HyperelasticOgden ogd) view = new ViewHyperelasticOgden(ogd);
                    else if (property is Hyperfoam hf) view = new ViewHyperfoam(hf);
                    else throw new NotSupportedException();
                    //
                    item = new ListViewItem(view.Name);
                    item.Tag = view;
                    item = lvAddedProperties.Items.Add(item);
                }
                //
                lvAddedProperties.Items[0].Selected = true;
                lvAddedProperties.Select();
            }
        }
        public void PrepareFormForPreview()
        {
            PrepareForm(null, null);
            tbName.Text = "";
            //
            _preview = true;
            SetControlStates();
        }
        private void SetControlStates()
        {
            tbName.ReadOnly = _preview;
            tbName.BackColor = SystemColors.Window;
            tbDescription.ReadOnly = _preview;
            tbDescription.BackColor = SystemColors.Window;
            cbCompositeMaterialType.Enabled = !_preview;
            tbWidth.ReadOnly = _preview;
            tbWidth.BackColor = SystemColors.Window;
            // Buttons
            btnAdd.Visible = !_preview;
            btnMoveUp.Visible = !_preview;
            btnMoveDown.Visible = !_preview;
            btnRemove.Visible = !_preview;
            // All models
            tvProperties.Visible = !_preview;
            // Added models
            if (_preview)
            {
                lvAddedProperties.Left = tvProperties.Left;
                lvAddedProperties.Width = (btnMoveUp.Left + btnMoveUp.Width) - lvAddedProperties.Left;
                lvAddedProperties.Top = labAvailable.Top;
                lvAddedProperties.Height = tvProperties.Bottom - lvAddedProperties.Top;
                lvAddedProperties.BringToFront();
            }
            // Property grid
            propertyGrid.ReadOnly = _preview;
            // Data grid
            dgvData.AllowUserToAddRows = !_preview;
            dgvData.AllowUserToDeleteRows = !_preview;
            dgvData.ReadOnly = _preview;
            // Buttons
            btnOKAddNew.Visible = !_preview;
            btnOK.Visible = !_preview;
            //btnCancel.Visible = !_preview;
        }
        private void ClearControls()
        {
            propertyGrid.SelectedObject = null;
            dgvData.DataSource = null;
            //
            tcProperties.TabPages.Clear();
            tcProperties.TabPages.Add(_pages[0]);
        }
        public void Add()
        {
            // Check if the name exists
            UserControls.FrmProperties.CheckName(_materialToEditName, tbName.Text, _materialNames, "material");
            //
            _material = new Material(tbName.Text);
            _material.Description = tbDescription.Text;
            _material.TemperatureDependent = cbTemperatureDependent.Checked;
            _material.CompositeMaterialType = GetSelectedCompositeMaterialType();
            _material.Width = GetWidthValue();
            //
            ViewMaterialProperty property;
            MaterialProperty materialProperty;
            int elasticityCount = 0;
            int densityCount = 0;
            foreach (ListViewItem item in lvAddedProperties.Items)
            {
                property = (ViewMaterialProperty)item.Tag;
                if (property is ViewElastic ||
                    property is ViewElasticEngineeringConstants ||
                    property is ViewElasticLamina ||
                    property is ViewElasticWithDensity)
                {
                    elasticityCount++;
                    if (elasticityCount > 1) throw new CaeException("A material can contain only one elastic property.");
                }
                if (property is ViewDensity || property is ViewElasticWithDensity)
                {
                    densityCount++;
                    if (densityCount > 1) throw new CaeException("A material can contain only one density.");
                }
                materialProperty = property.GetBase();
                // Check equations
                materialProperty.CheckEquations();
                //
                if (property is ViewDensity vd)
                {
                    for (int i = 0; i < vd.DataPoints.Count; i++)
                    {
                        if (vd.DataPoints[i].Density.Value <= 0) throw new CaeException("The density must be larger than 0.");
                    }
                }
                else if (property is ViewElastic ve && ve.GetYoungsModulusValue() <= 0)
                {
                    throw new CaeException("The Young's modulus must be larger than 0.");
                }
                else if (property is ViewElasticEngineeringConstants && cbTemperatureDependent.Checked)
                {
                    ((ElasticEngineeringConstants)materialProperty).CheckTemperatures();
                }
                else if (property is ViewElasticLamina && cbTemperatureDependent.Checked)
                {
                    ((ElasticLamina)materialProperty).CheckTemperatures();
                }
                else if (property is ViewElasticWithDensity ewd)
                {
                    if (ewd.GetYoungsModulusValue() <= 0) throw new CaeException("The Young's modulus must be larger than 0.");
                    if (ewd.GetDensityValue() <= 0) throw new CaeException("The density must be larger than 0.");
                }
                else if (property is ViewThermalExpansion vex)
                {
                    for (int i = 0; i < vex.DataPoints.Count; i++)
                    {
                        if (vex.DataPoints[i].ThermalExpansion.Value <= 0)
                            throw new CaeException("The thermal expansion coefficient must be larger than 0.");
                    }
                }
                else if (property is ViewThermalConductivity vtc)
                {
                    for (int i = 0; i < vtc.DataPoints.Count; i++)
                    {
                        if (vtc.DataPoints[i].ThermalConductivity.Value <= 0)
                            throw new CaeException("The thermal conductivity coefficient must be larger than 0.");
                    }
                }
                else if (property is ViewSpecificHeat vsh)
                {
                    for (int i = 0; i < vsh.DataPoints.Count; i++)
                    {
                        if (vsh.DataPoints[i].SpecificHeat.Value <= 0)
                            throw new CaeException("The thermal conductivity coefficient must be larger than 0.");
                    }
                }
                else if (property is ViewSlipWear vsw)
                {
                    if (vsw.GetHardnessValue() <= 0) throw new CaeException("The hardness must be larger than 0.");
                    if (vsw.GetWearCoefficientValue() <= 0) throw new CaeException("The wear coefficient must be larger than 0.");
                }
                else if (property is ViewHyperelastic vh)
                {
                    if (vh.GetC10Value() <= 0) throw new CaeException("The Mooney-Rivlin constant C10 must be larger than 0.");
                }
                else if (property is ViewHyperelasticNeoHooke vnh)
                {
                    if (vnh.GetC10Value() <= 0) throw new CaeException("The Neo-Hookean constant C10 must be larger than 0.");
                }
                else if (property is ViewHyperelasticOgden vogd)
                {
                    if (vogd.GetMu1Value() == 0) throw new CaeException("The Ogden parameter Mu1 must be non-zero.");
                }
                else if (property is ViewHyperfoam vhf)
                {
                    if (vhf.GetMu1Value() == 0) throw new CaeException("The Hyperfoam parameter Mu1 must be non-zero.");
                }
                //
                _material.AddProperty(materialProperty);
            }
            //
            if (_materialToEditName == null)
            {
                // Create
                _controller.AddMaterialCommand(Material);
            }
            else
            {
                // Replace
                if (_materialToEditName != Material.Name || _propertyItemChanged)
                {
                    _controller.ReplaceMaterialCommand(_materialToEditName, Material);
                }
            }
        }
        private void SetAllGridViewUnits()
        {
            string noUnit = "/";
            // Density
            SetGridViewUnit(nameof(DensityDataPoint.DensityEq), _controller.Model.UnitSystem.DensityUnitAbbreviation,
                            new StringDensityFromConverter());
            // Elastic
            SetGridViewUnit(nameof(ElasticDataPoint.YoungsModulusEq), _controller.Model.UnitSystem.PressureUnitAbbreviation,
                            new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticDataPoint.PoissonsRatioEq), noUnit,
                            new StringDoubleConverter());
            // Engineering constants
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.YoungsModulus1Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.YoungsModulus2Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.YoungsModulus3Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.PoissonsRatio12Eq), noUnit,
                            new StringDoubleConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.PoissonsRatio13Eq), noUnit,
                            new StringDoubleConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.PoissonsRatio23Eq), noUnit,
                            new StringDoubleConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.ShearModulus12Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.ShearModulus13Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticEngineeringConstantsDataPoint.ShearModulus23Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            // Lamina
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.YoungsModulus1Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.YoungsModulus2Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.PoissonsRatio12Eq), noUnit,
                            new StringDoubleConverter());
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.ShearModulus12Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.ShearModulus13Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            SetGridViewUnit(nameof(ElasticLaminaDataPoint.ShearModulus23Eq),
                            _controller.Model.UnitSystem.PressureUnitAbbreviation, new StringPressureFromConverter());
            // Plastic
            SetGridViewUnit(nameof(PlasticDataPoint.StressEq), _controller.Model.UnitSystem.PressureUnitAbbreviation,
                            new StringPressureFromConverter());
            SetGridViewUnit(nameof(PlasticDataPoint.StrainEq), noUnit,
                            new StringDoubleConverter());
            // Thermal expansion
            SetGridViewUnit(nameof(ThermalExpansionDataPoint.ThermalExpansionEq),
                            _controller.Model.UnitSystem.ThermalExpansionUnitAbbreviation,
                            new StringThermalExpansionFromConverter());
            // Thermal conductivity
            SetGridViewUnit(nameof(ThermalConductivityDataPoint.ThermalConductivityEq),
                            _controller.Model.UnitSystem.ThermalConductivityUnitAbbreviation,
                            new StringThermalConductivityFromConverter());
            // Specific heat
            SetGridViewUnit(nameof(SpecificHeatDataPoint.SpecificHeatEq), _controller.Model.UnitSystem.SpecificHeatUnitAbbreviation,
                            new StringSpecificHeatFromConverter());
            // Temperature
            SetGridViewUnit(nameof(TempDataPoint.TemperatureEq), _controller.Model.UnitSystem.TemperatureUnitAbbreviation,
                            new StringTemperatureFromConverter());
            //
            dgvData.XColName = "Plastic strain";
            //
            dgvData.StartPlotAtZero = true;
        }
        private void SetGridViewUnit(string columnName, string unit, TypeConverter converter)
        {
            DataGridViewColumn col = dgvData.Columns[columnName];
            if (col != null)
            {
                // Unit
                if (col.HeaderText != null) col.HeaderText = col.HeaderText.Replace("?", unit);
                // Alignment
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
                // Converter
                col.Tag = converter;
            }
        }
        private void FillCompositeMaterialTypes()
        {
            cbCompositeMaterialType.Items.Clear();
            foreach (CompositeMaterialType value in Enum.GetValues(typeof(CompositeMaterialType)))
                cbCompositeMaterialType.Items.Add(value.ToString());
            cbCompositeMaterialType.SelectedIndex = (int)CompositeMaterialType.None;
        }
        private CompositeMaterialType GetSelectedCompositeMaterialType()
        {
            if (cbCompositeMaterialType.SelectedIndex < 0) return CompositeMaterialType.None;
            return (CompositeMaterialType)cbCompositeMaterialType.SelectedIndex;
        }
        // The width is only meaningful for composite materials
        private void HideShowWidth()
        {
            bool visible = GetSelectedCompositeMaterialType() != CompositeMaterialType.None;
            lWidth.Visible = visible;
            tbWidth.Visible = visible;
            lWidthUnit.Visible = visible;
        }
        private double? GetWidthValue()
        {
            if (GetSelectedCompositeMaterialType() == CompositeMaterialType.None) return null;
            //
            string widthText = tbWidth.Text.Trim();
            if (widthText.Length == 0) return null;
            //
            double width = (double)new StringLengthConverter().ConvertFrom(widthText);
            if (width <= 0) throw new CaeException("The composite material width must be larger than 0.");
            return width;
        }
        private string GetMaterialName()
        {
            return _materialNames.GetNextNumberedKey("Material");
        }
        private void SetDataGridViewBinding(object data)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = data;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
        }
        private void HideShowTemperature()
        {
            if (lvAddedProperties.PossiblySelectedItems.Count > 0)
            {
                if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewDensity ||
                    lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElastic ||
                    lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElasticEngineeringConstants ||
                    lvAddedProperties.PossiblySelectedItems[0].Tag is ViewElasticLamina ||
                    lvAddedProperties.PossiblySelectedItems[0].Tag is ViewThermalConductivity ||
                    lvAddedProperties.PossiblySelectedItems[0].Tag is ViewSpecificHeat)
                {
                    tcProperties.TabPages.Clear();
                    // Properites
                    if (!cbTemperatureDependent.Checked) tcProperties.TabPages.Add(_pages[0]);
                    // Data points
                    if (cbTemperatureDependent.Checked) tcProperties.TabPages.Add(_pages[1]);
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewThermalExpansion vte)
                {
                    tcProperties.TabPages.Clear();
                    // Properites
                    tcProperties.TabPages.Add(_pages[0]);
                    // Data points
                    if (cbTemperatureDependent.Checked) tcProperties.TabPages.Add(_pages[1]);
                    //
                    vte.SetTemperatureDependence(cbTemperatureDependent.Checked);
                    propertyGrid.Refresh();
                }
                else if (lvAddedProperties.PossiblySelectedItems[0].Tag is ViewSlipWear ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewCompositeStrength ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelastic ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelasticNeoHooke ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperelasticOgden ||
                         lvAddedProperties.PossiblySelectedItems[0].Tag is ViewHyperfoam)
                {
                    tcProperties.TabPages.Clear();
                    tcProperties.TabPages.Add(_pages[0]);
                }
            }
            //
            string temperatureName = nameof(TempDataPoint.TemperatureEq);
            DataGridViewColumn col = dgvData.Columns[temperatureName];
            if (col != null) col.Visible = cbTemperatureDependent.Checked;
        }

        
    }
}
