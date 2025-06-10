using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;
using CaeResults;

namespace PrePoMax.Forms
{
    class FrmDistribution : UserControls.FrmPropertyDataListView, IFormBase
    {
        // Variables                                                                                                                
        private string[] _distributionNames;
        private string _distributionToEditName;
        private ViewDistribution _viewDistribution;
        private Controller _controller;
        private TabPage[] _pages;


        // Properties                                                                                                               
        public Distribution Distribution
        {
            get { return _viewDistribution.GetBase(); }
            set
            {
                var clone = value.DeepClone();
                if (clone == null) _viewDistribution = null;
                else if (clone is DistributionFromEquation dfe) _viewDistribution = new ViewDistributionFromEquation(dfe);
                else if (clone is DistributionFromFile dff) _viewDistribution = new ViewDistributionFromFile(dff);
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmDistribution(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewDistribution = null;
            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages)
            {
                tabPage.Paint += TabPage_Paint;
                _pages[i++] = tabPage;
            }
            
        }

        private void InitializeComponent()
        {
            this.tcProperties.SuspendLayout();
            this.tpProperties.SuspendLayout();
            this.gbType.SuspendLayout();
            this.SuspendLayout();
            // 
            // tpDataPoints
            // 
            this.tpDataPoints.Size = new System.Drawing.Size(302, 217);
            // 
            // propertyGrid
            // 
            this.propertyGrid.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid.Size = new System.Drawing.Size(296, 269);
            // 
            // FrmAmplitude
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 461);
            this.Name = "FrmDistribution";
            this.Text = "Edit Distribution";
            this.VisibleChanged += new System.EventHandler(this.FrmDistribution_VisibleChanged);
            this.tcProperties.ResumeLayout(false);
            this.tpProperties.ResumeLayout(false);
            this.gbType.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void FrmDistribution_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible) { }
            else dgvData.HidePlot();
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
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
        }


        // Overrides                                                                                                                
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count == 1)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                HashSet<string> additionalParameterNames = new HashSet<string>();
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewDistributionFromEquation vdfe)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    //
                    _viewDistribution = vdfe;
                    //
                    additionalParameterNames.Add("x");
                    additionalParameterNames.Add("y");
                    additionalParameterNames.Add("z");
                }
                else if (itemTag is ViewDistributionFromFile vdff)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    //
                    vdff.UpdateFileBrowserDialog();
                    _viewDistribution = vdff;
                }
                //if (itemTag is ViewAmplitudeTabular vat)
                //{
                //    tcProperties.TabPages.Add(_pages[0]);   // properties
                //    tcProperties.TabPages.Add(_pages[1]);   // data points
                //    //
                //    SetDataGridViewBinding(vat.DataPoints);
                //    //
                //    _viewAmplitude = vat;
                //}
                else throw new NotImplementedException();
                //
                propertyGrid.SelectedObject = lvTypes.SelectedItems[0].Tag;
                //
                SetAllGridViewUnits();
                //
                HashSet<string> parameterNames = new HashSet<string>(_controller.GetAllParameterNames());
                parameterNames.UnionWith(additionalParameterNames);
                propertyGrid.BuildAutocompleteMenu(parameterNames);
                dgvData.BuildAutocompleteMenu(parameterNames);
                //
                HighlightDistribution();
            }
        }
        protected override void OnPropertyGridSelectedGridItemChanged()
        {
            if (propertyGrid.SelectedGridItem.PropertyDescriptor != null)
            {
                string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                //
                if (_viewDistribution is ViewDistributionFromEquation vdfe) { }
                else if (_viewDistribution is ViewDistributionFromFile vdff)
                {
                    HighlightDistribution();
                }
            }
            //
            base.OnPropertyGridSelectedGridItemChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject == null) throw new CaeException("No item selected.");
            //
            _viewDistribution = (ViewDistribution)propertyGrid.SelectedObject;
            // Check if the name exists
            CheckName(_distributionToEditName, _viewDistribution.Name, _distributionNames, "distribution");
            // Check the equations
            if (Distribution is DistributionFromEquation dfe)
            {
                string error = dfe.CheckEquations();
                if (error != null) throw new CaeException(error);
            }
            else if (Distribution is DistributionFromFile dff)
            {
                string error;
                dff.CheckEquations();
                dff.IsProperlyDefined(out error);
                if (error != null) throw new CaeException(error);
            }
            // Create
            if (_distributionToEditName == null)
            {
                _controller.AddDistributionCommand(Distribution);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceDistributionCommand(_distributionToEditName, Distribution);
            }
        }
        protected override bool OnPrepareForm(string stepName, string distributionToEditName)
        {
            this.btnOkAddNew.Visible = distributionToEditName == null;
            //
            _propertyItemChanged = false;
            _stepName = null;
            _distributionNames = null;
            _distributionToEditName = null;
            _viewDistribution = null;
            lvTypes.Items.Clear();
            tcProperties.TabPages.Clear();
            tcProperties.TabPages.Add(_pages[0]);   // properties
            propertyGrid.SelectedObject = null;
            //
            _stepName = stepName;
            _distributionNames = _controller.GetDistributionNames();
            _distributionToEditName = distributionToEditName;
            //
            if (_distributionNames == null)
                throw new CaeException("The distribution names must be defined first.");
            //
            PopulateListOfDistributions();
            //
            if (distributionToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewDistribution = null;
                if (lvTypes.Items.Count == 1) _preselectIndex = 0;
            }
            else
            {
                Distribution = _controller.GetDistribution(distributionToEditName); // to clone
                //
                int selectedId;
                if (_viewDistribution.GetBase() is DistributionFromEquation) selectedId = 0;
                else if (_viewDistribution.GetBase() is DistributionFromFile) selectedId = 1;
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewDistribution;
                _preselectIndex = selectedId;
            }
            //
            _controller.SetSelectByToOff();
            //
            return true;
        }
        

        // Methods                                                                                                                  
        private void PopulateListOfDistributions()
        {
            // Populate list view
            ListViewItem item;
            // From equation
            item = new ListViewItem("From Equation");
            ViewDistributionFromEquation vdfe = 
                new ViewDistributionFromEquation(new DistributionFromEquation(GetDistributionName("From Equation"), "=1"));
            item.Tag = vdfe;
            lvTypes.Items.Add(item);
            // From file
            item = new ListViewItem("From File");
            ViewDistributionFromFile vdff =
                new ViewDistributionFromFile(new DistributionFromFile(GetDistributionName("From File")));
            item.Tag = vdff;
            lvTypes.Items.Add(item);
        }
        private string GetDistributionName(string name)
        {
            if (name == null || name == "") name = "Distribution";
            name = name.Replace(' ', '_');
            name = _distributionNames.GetNextNumberedKey(name);
            //
            return name;
        }
        private void SetDataGridViewBinding(object data)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = data;
            dgvData.DataSource = binding; // bind datagridView to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
        }
        private void SetAllGridViewUnits()
        {
            string noUnit = "/";
            // Amplitude
            SetGridViewUnit(nameof(AmplitudeDataPoint.Time), _controller.Model.UnitSystem.TimeUnitAbbreviation,
                            _controller.Model.UnitSystem.FrequencyUnitAbbreviation,
                            new StringDoubleConverter());
            SetGridViewUnit(nameof(AmplitudeDataPoint.Amplitude), noUnit, null,
                            new StringDoubleConverter());
            //
            dgvData.XColIndex = 0;
            dgvData.StartPlotAtZero = true;
        }
        private void SetGridViewUnit(string columnName, string unit1, string unit2, TypeConverter converter)
        {
            DataGridViewColumn col = dgvData.Columns[columnName];
            if (col != null)
            {
                // Unit
                if (col.HeaderText != null)
                {
                    col.HeaderText = col.HeaderText.ReplaceFirst("?", unit1);
                    if (unit2 != null) col.HeaderText = col.HeaderText.ReplaceFirst("?", unit2);
                }
                // Alignment
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
                // Width
                col.Width += 10;
                // Converter
                col.Tag = converter;
            }
        }
        //
        private void HighlightDistribution()
        {
            try
            {
                _controller.ClearSelectionHistory();
                Distribution distribution = Distribution;
                //
                if (distribution is DistributionFromEquation dfe) { }
                else if (distribution is DistributionFromFile dff)
                {
                    string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
                    if (dff.Interpolator == null) dff.ImportDistribution();
                    //
                    if (dff.Interpolator != null && dff.Interpolator.CloudPoints != null)
                    {
                        CloudPoint[] cloudPoints = dff.Interpolator.CloudPoints;
                        double[][] nodeCoor = new double[cloudPoints.Length][];
                        //
                        for (int i = 0; i < cloudPoints.Length; i++)
                        {
                            nodeCoor[i] = new double[] { cloudPoints[i].Coor[0], cloudPoints[i].Coor[1], cloudPoints[i].Coor[2] };
                        }
                        _controller.HighlightNodes(nodeCoor, true);
                    }
                }
                else throw new NotSupportedException();
            }
            catch { }
        }
    }
}
