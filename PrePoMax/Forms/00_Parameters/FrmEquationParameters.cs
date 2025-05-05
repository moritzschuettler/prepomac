using CaeGlobals;
using CaeMesh;
using CaeModel;
using FastColoredTextBoxNS;
using PrePoMax.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace PrePoMax.Forms
{
    public partial class FrmParametersEditor : Form
    {
        // Variables                                                                                                                
        private Controller _controller;
        private List<EquationParameter> _parameters;
        private Dictionary<string, object> _propertyParameters;
        private int _cellRow;
        private int _cellCol;
        private bool _bindingInProgress;



        // Constructors                                                                                                             
        public FrmParametersEditor(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _cellRow = -1;
            _cellCol = -1;
            dgvData.ShowErrorMsg = false;
            //
            _propertyParameters = _controller.GetPropertyParameters();
            _bindingInProgress = false;
        }


        // Event handlers                                                                                                           
        private void FrmParametersEditor_Load(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null)
                {
                    _parameters = new List<EquationParameter>();
                    foreach (var entry in _controller.Model.Parameters) _parameters.Add(entry.Value.DeepClone());
                    // Binding
                    _bindingInProgress = true;
                    SetDataGridViewBinding(_parameters);
                    _bindingInProgress = false;
                    //
                    UpdateNCalcParameters();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
                _bindingInProgress = false;
            }
        }
        private void FrmParametersEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            _controller.UpdateNCalcParameters();
        }
        //
        private void dgvData_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (!_bindingInProgress) UpdateNCalcParameters();
        }
        private void dgvData_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (!_bindingInProgress) UpdateNCalcParameters();
        }
        //
        private void dgvData_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            _cellRow = e.RowIndex;
            _cellCol = e.ColumnIndex;
        }
        private void dgvData_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // This is executed before the actual value of the cell is modified
            try
            {
                if (e.RowIndex == _cellRow && e.ColumnIndex == _cellCol)
                {
                    HashSet<string> existingNames = new HashSet<string>(MyNCalc.ExistingParameters.Keys);
                    // If an existing parameter is renamed remove it from a list
                    existingNames.Remove(_parameters[e.RowIndex].Name);
                    //
                    UpdateNCalcParameters(e.RowIndex, false);
                    //
                    string value = e.FormattedValue.ToString();
                    EquationParameter ep = new EquationParameter();
                    // Test the name
                    if (e.ColumnIndex == 0)
                    {
                        if (value.Length > 0 && char.IsDigit(value[0]))
                            throw new CaeException("The parameter name '" + value + "' must not start with a digit.");
                        if (existingNames.Contains(value))
                            throw new CaeException("The parameter named '" + value + "' already exists.");
                        //
                        ep.Name = value;
                    }
                    // Test the equation
                    else if (e.ColumnIndex == 1)
                    {
                        ep.Equation.SetEquation(value);
                        double tmp = ep.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.Message);
                e.Cancel = true;
            }
        }
        private void dgvData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == _cellRow && e.ColumnIndex == _cellCol)
            {
                // At CellEndEdit the value of the cell actually changed
                UpdateNCalcParameters();
            }
            //
            _cellRow = -1;
            _cellCol = -1;
        }
        //
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                double tmp;
                EquationParameter existingParameter;
                HashSet<string> parameterNames = new HashSet<string>();
                // Check all equation
                foreach (var parameter in _parameters)
                {
                    try
                    {
                        tmp = parameter.Value;
                    }
                    catch (Exception ex)
                    {
                        throw new CaeException("There is an error in the '" + parameter.Name + "' parameter equation.");
                    }
                }
                // Clear overridden parameters
                _controller.Model.Parameters.OverriddenParameters.Clear();
                // Add parameters
                foreach (var parameter in _parameters)
                {
                    parameterNames.Add(parameter.Name);
                    //
                    if (_controller.Model.Parameters.TryGetValue(parameter.Name, out existingParameter))
                    {
                        // No changes
                        if (parameter.EquationStr.EqualsEquation(existingParameter.EquationStr)) { }
                        // Replace
                        else _controller.ReplaceParameterCommand(existingParameter.Name, parameter);
                    }
                    // Add
                    else _controller.AddParameterCommand(parameter);
                }
                // Remove
                string[] parameterNamesToRemove = _controller.Model.Parameters.Keys.Except(parameterNames).ToArray();
                if (parameterNamesToRemove.Length > 0) _controller.RemoveParametersCommand(parameterNamesToRemove);
                //
                btnCancel_Click(null, null);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }


        // Methods                                                                                                                  
        private void SetDataGridViewBinding(object data)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = data;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            //
            foreach (DataGridViewColumn column in dgvData.Columns)
            {
                if (column.Index == 1)
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                    column.MinimumWidth = 150;
                }
                //
                column.Width = 150;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
                column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomLeft;
            }
        }
        private void UpdateNCalcParameters(int upToRow = int.MaxValue, bool refresh = true)
        {
            int rowCount = 0;
            HashSet<string> autocompleteItems = new HashSet<string>();
            //
            MyNCalc.ExistingParameters.Clear();
            MyNCalc.AddPropertyParameters(_propertyParameters);
            autocompleteItems.UnionWith(_propertyParameters.Keys);
            //
            foreach (var parameter in _parameters)
            {
                // Add parameters up to this row
                if (rowCount < upToRow)
                {
                    try
                    {
                        MyNCalc.ExistingParameters.Add(parameter.Name, parameter.Value);
                        autocompleteItems.Add(parameter.Name);
                    }
                    catch { }
                }
                rowCount++;
            }
            //
            dgvData.BuildAutocompleteMenu(autocompleteItems, 1);
            //
            if (refresh) dgvData.Refresh();
        }
        private void SavePmp(string fileName)
        {
            bool focused = dgvData.Focused;
            if (focused) btnCancel.Focus(); // removes the last uncomitted row
            //
            //_controller.AddParameter
            //
            if (focused)
            {
                dgvData.ClearSelection();
                dgvData.Focus();
            }
        }


    }
}
