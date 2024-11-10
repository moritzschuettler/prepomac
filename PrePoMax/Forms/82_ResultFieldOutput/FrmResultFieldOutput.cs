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
using CaeMesh;
using System.Data.Common;

namespace PrePoMax.Forms
{
    class FrmResultFieldOutput : UserControls.FrmPropertyDataListView, IFormBase
    {
        // Variables                                                                                                                
        private string[] _resultFieldOutputNames;
        private string _resultFieldOutputToEditName;
        private ViewResultFieldOutput _viewResultFieldOutput;
        private Controller _controller;
        private TabPage[] _pages;


        // Properties                                                                                                               
        public ResultFieldOutput ResultFieldOutput
        {
            get { return _viewResultFieldOutput.GetBase(); }
            set
            {
                var clone = value.DeepClone();
                if (clone == null) _viewResultFieldOutput = null;
                else if (clone is ResultFieldOutputLimit rfol)
                    _viewResultFieldOutput = new ViewResultFieldOutputLimit(rfol, _controller.GetResultPartNames(),
                                                                            _controller.GetResultUserElementSetNames(),
                                                                            ref _propertyItemChanged);
                else if (clone is ResultFieldOutputEnvelope rfoen)
                    _viewResultFieldOutput = new ViewResultFieldOutputEnvelope(rfoen);
                else if (clone is ResultFieldOutputEquation rfoeq)
                    _viewResultFieldOutput = new ViewResultFieldOutputEquation(rfoeq);
                else if (clone is ResultFieldOutputCoordinateSystemTransform rfocst)
                    _viewResultFieldOutput = new ViewResultFieldOutputCoordinateSystemTransform(rfocst);
                else throw new NotImplementedException();
            }
        }


        // Constructors                                                                                                             
        public FrmResultFieldOutput(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewResultFieldOutput = null;
            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages)
            {
                tabPage.Paint += TabPage_Paint;
                _pages[i++] = tabPage;
            }
            //
            dgvData.EnableCutMenu = false;
            dgvData.EnablePasteMenu = false;
            dgvData.EnablePlotMenu = false;
        }
        private void InitializeComponent()
        {
            this.tcProperties.SuspendLayout();
            this.tpProperties.SuspendLayout();
            this.gbType.SuspendLayout();
            this.SuspendLayout();
            // 
            // propertyGrid
            // 
            this.propertyGrid.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid.Size = new System.Drawing.Size(296, 269);
            // 
            // FrmResultFieldOutput
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.ClientSize = new System.Drawing.Size(334, 461);
            this.Name = "FrmResultFieldOutput";
            this.Text = "Edit Field Output";
            this.VisibleChanged += new System.EventHandler(this.FrmResultFieldOutput_VisibleChanged);
            this.tcProperties.ResumeLayout(false);
            this.tpProperties.ResumeLayout(false);
            this.gbType.ResumeLayout(false);
            this.ResumeLayout(false);

        }


        // Event handlers                                                                                                           
        private void FrmResultFieldOutput_VisibleChanged(object sender, EventArgs e)
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
                dgvData.ClearSelection();
                if (dgvData.RowCount > 0 && dgvData.Columns.Count > 0) dgvData[0, 0].Selected = true;
            }
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
        }


        // Overrides                                                                                                                
        protected override void OnPropertyGridPropertyValueChanged()
        {
            string property = propertyGrid.SelectedGridItem.PropertyDescriptor.Name;
            //
            if (_viewResultFieldOutput is ViewResultFieldOutputLimit vrfol)
            {
                if (property == nameof(vrfol.LimitPlotBasedOn))
                {
                    SetTabPages(vrfol);
                }
            }
            else if (_viewResultFieldOutput is ViewResultFieldOutputEnvelope vrfoen)
            {
            }
            else if (_viewResultFieldOutput is ViewResultFieldOutputEquation vrfoeq)
            {
            }
            else if (_viewResultFieldOutput is ViewResultFieldOutputCoordinateSystemTransform vrfosct)
            {
            }
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count == 1)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                SetTabPages(itemTag);
                //
                propertyGrid.SelectedObject = lvTypes.SelectedItems[0].Tag;
            }
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject == null) throw new CaeException("No item selected.");
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewResultFieldOutput = (ViewResultFieldOutput)propertyGrid.SelectedObject;
            // Check if the name exists
            CheckName(_resultFieldOutputToEditName, _viewResultFieldOutput.Name, _resultFieldOutputNames, "field output");
            // Check for zero limits
            if (ResultFieldOutput is ResultFieldOutputLimit rfol)
            {
                foreach (var entry in rfol.ItemNameLimit)
                {
                    if (entry.Value == 0) throw new CaeException("All limit values must be different from 0.");
                }
            }
            // Check equation
            if (ResultFieldOutput is ResultFieldOutputEquation rfoe)
            {
                HashSet<string> parentNames;
                string error = _controller.CurrentResult.CheckResultFieldOutputEquation(rfoe.Equation, out parentNames, out _);
                if (error != null) throw new CaeException(error);
                if (parentNames.Contains(rfoe.Name)) throw new CaeException("The equation must not contain a self reference.");
                rfoe.SetParentNames(parentNames.ToArray());
            }
            // Cyclic reference
            if (_controller.CurrentResult.AreResultFieldOutputsInCyclicDependance(_resultFieldOutputToEditName, ResultFieldOutput))
            {
                throw new CaeException("The selected dependent field output creates a cyclic reference.");
            }
            // Check coordinate system transform
            if (ResultFieldOutput is ResultFieldOutputCoordinateSystemTransform rfocst)
            {
                DataTypeEnum dataType = _controller.CurrentResult.GetFieldDataType(rfocst.FieldName);
                if (dataType != DataTypeEnum.Vector && dataType != DataTypeEnum.Tensor)
                    throw new CaeException("Only vector or tensor field output can be transformed.");
                else if (!_controller.CurrentResult.DoesFieldContainsAllNecessaryComponents(rfocst.FieldName))
                    throw new CaeException("The field output does not contain all necessary componets.");
            }
            // Create
            if (_resultFieldOutputToEditName == null)
            {
                _controller.AddResultFieldOutputCommand(ResultFieldOutput);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceResultFieldOutputCommand(_resultFieldOutputToEditName, ResultFieldOutput);
            }
        }
        protected override bool OnPrepareForm(string stepName, string resultFieldOutputToEditName)
        {
            this.btnOkAddNew.Visible = resultFieldOutputToEditName == null;
            //
            _propertyItemChanged = false;
            _stepName = null;
            _resultFieldOutputNames = null;
            _resultFieldOutputToEditName = null;
            _viewResultFieldOutput = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            dgvData.DataSource = null;
            dgvData.Columns.Clear();
            //
            _stepName = stepName;
            _resultFieldOutputNames = _controller.GetResultFieldOutputNames();
            _resultFieldOutputToEditName = resultFieldOutputToEditName;
            Dictionary<string, string[]> filedNameComponentNames =
                _controller.CurrentResult.GetAllVisibleFiledNameComponentNames();
            // Remove self
            if (_resultFieldOutputToEditName != null) filedNameComponentNames.Remove(_resultFieldOutputToEditName);
            //
            string[] partNames = _controller.GetResultPartNames();
            string[] elementSetNames = _controller.GetResultUserElementSetNames();
            string[] coordinateSystemNames = _controller.GetResultCoordinateSystemNames();
            //
            if (_resultFieldOutputNames == null)
                throw new CaeException("The field output names must be defined first.");
            //
            PopulateListOfResultFieldOutputs(filedNameComponentNames, partNames, elementSetNames, coordinateSystemNames);
            //
            if (_resultFieldOutputToEditName == null)
            {
                lvTypes.Enabled = true;
                _viewResultFieldOutput = null;
                if (lvTypes.Items.Count == 1) _preselectIndex = 0;
                // Show only propertes tab
                tcProperties.TabPages.Clear();
                tcProperties.TabPages.Add(_pages[0]);   // properites
            }
            else
            {
                ResultFieldOutput = _controller.GetResultFieldOutput(_resultFieldOutputToEditName); // to clone
                _propertyItemChanged = !ResultFieldOutput.Valid;
                //
                int selectedId;
                if (_viewResultFieldOutput is ViewResultFieldOutputLimit vrfol)
                {
                    selectedId = 0;
                    // Check
                    string[] fieldNames = filedNameComponentNames.Keys.ToArray();
                    CheckMissingValueRef(ref fieldNames, vrfol.FieldName, s => { vrfol.FieldName = s; });
                    string[] componentNames = filedNameComponentNames[vrfol.FieldName];
                    CheckMissingValueRef(ref componentNames, vrfol.ComponentName, s => { vrfol.ComponentName = s; });
                    //
                    vrfol.PopulateDropDownLists(filedNameComponentNames);
                }
                else if (_viewResultFieldOutput is ViewResultFieldOutputEnvelope vrfoen)
                {
                    selectedId = 1;
                    // Check
                    string[] fieldNames = filedNameComponentNames.Keys.ToArray();
                    CheckMissingValueRef(ref fieldNames, vrfoen.FieldName, s => { vrfoen.FieldName = s; });
                    string[] componentNames = filedNameComponentNames[vrfoen.FieldName];
                    CheckMissingValueRef(ref componentNames, vrfoen.ComponentName, s => { vrfoen.ComponentName = s; });
                    //
                    vrfoen.PopulateDropDownLists(filedNameComponentNames);
                }
                else if (_viewResultFieldOutput is ViewResultFieldOutputEquation vrfoeq)
                {
                    selectedId = 2;
                    // Check
                }
                else if (_viewResultFieldOutput is ViewResultFieldOutputCoordinateSystemTransform vrfcst)
                {
                    selectedId = 3;
                    // Check
                    string[] fieldNames = filedNameComponentNames.Keys.ToArray();
                    CheckMissingValueRef(ref fieldNames, vrfcst.FieldName, s => { vrfcst.FieldName = s; });
                    //
                    CheckMissingValueRef(ref coordinateSystemNames, vrfcst.CoordinateSystemName, 
                                         s => { vrfcst.CoordinateSystemName = s; });
                    //
                    vrfcst.PopulateDropDownLists(filedNameComponentNames, coordinateSystemNames);
                }
                else throw new NotSupportedException();
                //
                lvTypes.Items[selectedId].Tag = _viewResultFieldOutput;
                _preselectIndex = selectedId;
            }
            //
            _controller.SetSelectByToOff();
            //
            return true;
        }
        

        // Methods                                                                                                                  
        private void PopulateListOfResultFieldOutputs(Dictionary<string, string[]> filedNameComponentNames,
                                                      string[] partNames, string[] elementSetNames,
                                                      string[] coordinateSystemNames)
        {
            string firstFieldName = null;
            string firstComponentName = null;
            //
            if (filedNameComponentNames.Count > 0)
            {
                foreach (var entry in filedNameComponentNames)
                {
                    if (entry.Value != null && entry.Value.Length > 0)
                    {
                        firstFieldName = entry.Key;
                        firstComponentName = entry.Value[0];
                        break;
                    }
                }
            }
            // Populate list view
            ListViewItem item;
            // Limit
            item = new ListViewItem("Limit");
            if (firstFieldName != null && firstComponentName != null)
            {
                ResultFieldOutputLimit rfosf = new ResultFieldOutputLimit(GetResultFieldOutputName("Limit"),
                                                                                        FOFieldNames.Stress,
                                                                                        FOComponentNames.Mises);
                ViewResultFieldOutputLimit vrfosf = new ViewResultFieldOutputLimit(rfosf, partNames, elementSetNames,
                                                                                                 ref _propertyItemChanged);
                vrfosf.PopulateDropDownLists(filedNameComponentNames);
                item.Tag = vrfosf;
            }
            else item.Tag = new ViewError("There are not field outputs or components for the creation of the field output.");
            lvTypes.Items.Add(item);
            // Envelope
            item = new ListViewItem("Envelope");
            if (firstFieldName != null && firstComponentName != null)
            {
                ResultFieldOutputEnvelope rfoe = new ResultFieldOutputEnvelope(GetResultFieldOutputName("Envelope"),
                                                                           FOFieldNames.Stress,
                                                                           FOComponentNames.Mises);
                ViewResultFieldOutputEnvelope vrfoe = new ViewResultFieldOutputEnvelope(rfoe);
                vrfoe.PopulateDropDownLists(filedNameComponentNames);
                item.Tag = vrfoe;
            }
            else item.Tag = new ViewError("There are not field outputs or components for the creation of the field output.");
            lvTypes.Items.Add(item);
            // Equation
            item = new ListViewItem("Equation");
            if (firstFieldName != null && firstComponentName != null)
            {
                ResultFieldOutputEquation rfoe = new ResultFieldOutputEquation(GetResultFieldOutputName("Equation"), "=");
                ViewResultFieldOutputEquation vrfoe = new ViewResultFieldOutputEquation(rfoe);
                item.Tag = vrfoe;
            }
            else item.Tag = new ViewError("There are not field outputs or components for the creation of the field output.");
            lvTypes.Items.Add(item);
            // Transform
            item = new ListViewItem("Coordinate System Transform");
            if (firstFieldName != null && firstComponentName != null)
            {
                if (coordinateSystemNames.Length > 0)
                {
                    ResultFieldOutputCoordinateSystemTransform rfocst =
                        new ResultFieldOutputCoordinateSystemTransform(GetResultFieldOutputName("Transform"),
                                                                       filedNameComponentNames.First().Key,
                                                                       coordinateSystemNames[0]);
                    ViewResultFieldOutputCoordinateSystemTransform vrfocst =
                        new ViewResultFieldOutputCoordinateSystemTransform(rfocst);
                    vrfocst.PopulateDropDownLists(filedNameComponentNames, coordinateSystemNames);
                    item.Tag = vrfocst;
                }
                else item.Tag = new ViewError("There is no coordinate system defined for the field output creation.");
            }
            else item.Tag = new ViewError("There are not field outputs or components for the creation of the field output.");
            lvTypes.Items.Add(item);
        }
        private string GetResultFieldOutputName(string name)
        {
            if (name == null || name == "") name = "FieldOutput";
            name = name.Replace(' ', '_');
            name = _resultFieldOutputNames.GetNextNumberedKey(name);
            //
            return name;
        }
        private void SetTabPages(object item)
        {
            if (item is ViewError ve)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                tcProperties.TabPages.Add(_pages[0]);   // properites
                //
                propertyGrid.SelectedObject = ve;
            }
            else if (item is ViewResultFieldOutputLimit vrfol)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                tcProperties.TabPages.Add(_pages[0]);   // properites
                tcProperties.TabPages.Add(_pages[1]);   // data points
                _pages[1].Text = "Limit Values";
                //
                SetDataGridViewBinding(vrfol.DataPoints);
                //
                dgvData.AllowUserToAddRows = false;
                dgvData.AllowUserToDeleteRows = false;
                dgvData.Columns[0].ReadOnly = true;
                dgvData.Columns[0].Width = 150;
                dgvData.Columns[0].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                //
                dgvData.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                //
                _viewResultFieldOutput = vrfol;
            }
            else if (item is ViewResultFieldOutputEnvelope vrfoen)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                tcProperties.TabPages.Add(_pages[0]);   // properites
                //
                _viewResultFieldOutput = vrfoen;
            }
            else if (item is ViewResultFieldOutputEquation vrfoeq)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                tcProperties.TabPages.Add(_pages[0]);   // properites
                //
                _viewResultFieldOutput = vrfoeq;
            }
            else if (item is ViewResultFieldOutputCoordinateSystemTransform vrfocst)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                tcProperties.TabPages.Add(_pages[0]);   // properites
                //
                _viewResultFieldOutput = vrfocst;
            }
            else throw new NotImplementedException();
        }
        private void SetDataGridViewBinding(object data)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = data;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
        }
        private void SetAllGridViewUnits()
        {
            string noUnit = "/";
            // Amplitude
            SetGridViewUnit(nameof(AmplitudeDataPoint.Time), _controller.Model.UnitSystem.TimeUnitAbbreviation,
                            _controller.Model.UnitSystem.FrequencyUnitAbbreviation);
            SetGridViewUnit(nameof(AmplitudeDataPoint.Amplitude), noUnit, null);
            //
            dgvData.XColIndex = 0;
            dgvData.StartPlotAtZero = true;
        }
        private void SetGridViewUnit(string columnName, string unit1, string unit2)
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
            }
        }

       
    }
}
