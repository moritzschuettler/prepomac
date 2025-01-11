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
using System.Collections;
using PrePoMax.Settings;

namespace PrePoMax.Forms
{    
    public partial class FrmStepControls : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables                                                                                                                
        private string _stepName;
        private StepControls _stepControls;
        private Controller _controller;
        private TabPage[] _pages;
        private bool _showWarning;
        

        // Properties                                                                                                               
        public StepControls StepControls
        { 
            get { return _stepControls; } 
            set { _stepControls = value.DeepClone(); } 
        }


        // Constructors                                                                                                             
        public FrmStepControls(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _stepControls = null;
            //
            int i = 0;
            _pages = new TabPage[tcProperties.TabPages.Count];
            foreach (TabPage tabPage in tcProperties.TabPages)
            {
                tabPage.Paint += TabPage_Paint;
                _pages[i++] = tabPage;
            }
            //
            _showWarning = true;
            //
            ClearControls();
        }


        // Event handling
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
        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (tvProperties.SelectedNode != null && tvProperties.SelectedNode.Tag != null)
            {
                string propertyName = tvProperties.SelectedNode.Text;
                //
                if (lvAddedProperties.FindItemWithText(propertyName) == null)
                {
                    ListViewItem item = new ListViewItem(propertyName);
                    if (tvProperties.SelectedNode.Tag is StepControlParameter scp)
                    {
                        if (scp is ResetStepControlParameter rscp)
                            item.Tag = new ViewResetStepControlParameter(rscp.DeepClone());
                        else if (scp is TimeIncrementationStepControlParameter tiscp)
                            item.Tag = new ViewTimeIncrementationStepControlParameter(tiscp.DeepClone());
                        else if (scp is FieldStepControlParameter fscp)
                            item.Tag = new ViewFieldStepControlParameter(fscp.DeepClone());
                        else if (scp is ContactStepControlParameter cscp)
                            item.Tag = new ViewContactStepControlParameter(cscp.DeepClone());
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
            }
            _propertyItemChanged = true;
        }
        private void lvAddedProperties_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvAddedProperties.SelectedItems.Count == 1)
            {
                // Clear
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                tcProperties.TabPages.Clear();
                //
                string gridItemTextToSelect;
                //
                if (lvAddedProperties.SelectedItems[0].Tag is ViewResetStepControlParameter vrscp)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    gridItemTextToSelect = nameof(vrscp.Reset);
                }
                else if (lvAddedProperties.SelectedItems[0].Tag is ViewTimeIncrementationStepControlParameter vtiscp)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    gridItemTextToSelect = nameof(vtiscp.I0);
                }
                else if (lvAddedProperties.SelectedItems[0].Tag is ViewFieldStepControlParameter vfscp)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    gridItemTextToSelect = nameof(vfscp.Rna);
                    if (gridItemTextToSelect == "Rna") gridItemTextToSelect = "Rnα";
                    else throw new NotSupportedException();
                }
                else if (lvAddedProperties.SelectedItems[0].Tag is ViewContactStepControlParameter vcscp)
                {
                    tcProperties.TabPages.Add(_pages[0]);   // properties
                    gridItemTextToSelect = nameof(vcscp.Delcon);
                    if (gridItemTextToSelect == "Delcon") gridItemTextToSelect = "delcon";
                    else throw new NotSupportedException();
                }
                else throw new NotSupportedException();
                //
                propertyGrid.SelectedObject = lvAddedProperties.SelectedItems[0].Tag;
                // Select grid item
                try
                {
                    // Get start point grid item
                    GridItem gi = propertyGrid.EnumerateAllItems().First((item) =>
                                  item.PropertyDescriptor != null &&
                                  item.PropertyDescriptor.DisplayName.TrimStart(new char[] { '\t' }) == gridItemTextToSelect);
                    // Select it
                    gi.Select();
                }
                catch { }
                //
                SetAllGridViewUnits();
            }
            lvAddedProperties.Select();
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
        }
        private void propertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            _propertyItemChanged = true;
        }
        private void tsmiResetAll_Click(object sender, EventArgs e)
        {
            if (propertyGrid.SelectedObject is ViewStepControlParameter vscp)
            {
                StepControlParameter scp = vscp.Base;
                scp.Reset();
                propertyGrid.Refresh();
                _propertyItemChanged = true;
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (Add()) Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }
        private void FrmSurfaceInteraction_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }


        // Methods                                                                                                                  
        public bool PrepareForm(string stepName, string notUsed)
        {
            _propertyItemChanged = false;
            _stepControls = null;
            lvAddedProperties.Items.Clear();
            ClearControls();
            //
            _stepName = stepName;
            // Initialize control parameters
            tvProperties.Nodes.Find("Reset", true)[0].Tag = new ResetStepControlParameter();
            tvProperties.Nodes.Find("Time Incrementation", true)[0].Tag = new TimeIncrementationStepControlParameter();
            tvProperties.Nodes.Find("Field", true)[0].Tag = new FieldStepControlParameter();
            tvProperties.Nodes.Find("Contact", true)[0].Tag = new ContactStepControlParameter();
            tvProperties.ExpandAll();
            //
            StepControls = _controller.GetStep(stepName).StepControls; // to clone
            //
            if (_stepControls.Parameters.Count > 0)
            {
                ListViewItem item;
                ViewStepControlParameter view;
                foreach (var parameter in _stepControls.Parameters)
                {
                    if (parameter is ResetStepControlParameter rscp)
                        view = new ViewResetStepControlParameter(rscp);
                    else if (parameter is TimeIncrementationStepControlParameter tiscp)
                        view = new ViewTimeIncrementationStepControlParameter(tiscp);
                    else if (parameter is FieldStepControlParameter fscp)
                        view = new ViewFieldStepControlParameter(fscp);
                    else if (parameter is ContactStepControlParameter cscp)
                        view = new ViewContactStepControlParameter(cscp);
                    else throw new NotSupportedException();
                    //
                    item = new ListViewItem(view.Name);
                    item.Tag = view;
                    lvAddedProperties.Items.Add(item);
                }
                //
                lvAddedProperties.Items[0].Selected = true;
                lvAddedProperties.Select();
            }
            else _showWarning = true;
            //
            _controller.SetSelectByToOff();
            //
            return true;
        }
        private void ClearControls()
        {
            propertyGrid.SelectedObject = null;
            dgvData.DataSource = null;
            //
            tcProperties.TabPages.Clear();
            tcProperties.TabPages.Add(_pages[0]);
        }
        public bool Add()
        {
            _stepControls = new StepControls();
            foreach (ListViewItem item in lvAddedProperties.Items)
            {
                _stepControls.AddParameter(((ViewStepControlParameter)(item.Tag)).Base);
            }
            // Replace
            bool applyControls = false;
            string message = "Customized solution controls are not needed in most nonlinear analyses." +
                             " They should only be used by those users who know what they are doing and" +
                             " are expert in the field.";
            if (_propertyItemChanged)
            {
                if (_showWarning)
                {
                    if (MessageBoxes.ShowWarningQuestionOKCancel(message) == DialogResult.OK)
                    { 
                        applyControls = true;
                        _showWarning = false;
                    }
                }
                else applyControls = true;
            }
            else return true;   // nothing changed
            //
            if (applyControls)
            {
                _controller.ReplaceStepControlsCommand(_stepName, StepControls);
                return true;
            }
            else return false;
        }
        private void SetAllGridViewUnits()
        {
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
        private void SetDataGridViewBinding(object data)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = data;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
        }
    }
}
