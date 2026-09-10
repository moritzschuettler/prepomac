// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using CaeGlobals;
using CaeMesh;
using CaeModel;
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
        private static readonly Color ErrorColor = Color.FromArgb(255, 225, 225);
        private Controller _controller;
        private List<ViewEquationParameter> _parameters;
        private Dictionary<string, object> _propertyParameters;
        private HashSet<string> _unusableNames;
        private int _nameColumnIndex;
        private int _equationColumnIndex;
        private int _cellRow;
        private int _cellCol;
        private bool _bindingInProgress;



        // Constructors                                                                                                             
        public FrmParametersEditor(Controller controller)
        {
            InitializeComponent();
            //
            dgvData.EnableDragAndDropRows();
            _controller = controller;
            _nameColumnIndex = 0;
            _equationColumnIndex = 1;
            _cellRow = -1;
            _cellCol = -1;
            dgvData.ShowErrorMsg = false;
            //
            _propertyParameters = _controller.GetPropertyParameters();
            _unusableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _unusableNames.UnionWith(MyNCalc.GetReservedParameterNames());
            _unusableNames.UnionWith(_propertyParameters.Keys);
            _bindingInProgress = false;
        }


        // Event handlers                                                                                                           
        private void FrmParametersEditor_Load(object sender, EventArgs e)
        {
            try
            {
                if (_controller.Model != null)
                {
                    _parameters = new List<ViewEquationParameter>();
                    foreach (var entry in _controller.Model.Parameters)
                        _parameters.Add(new ViewEquationParameter(entry.Value));
                    // Binding
                    _bindingInProgress = true;
                    SetDataGridViewBinding(_parameters);
                    _bindingInProgress = false;
                    //
                    EvaluateParameters();
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
            if (!_bindingInProgress) EvaluateParameters();
        }
        private void dgvData_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (!_bindingInProgress) EvaluateParameters();
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
                // Only a name is refused at the cell. An equation that cannot be evaluated yet is kept and
                // reported in the Evaluates to column, so that a parameter may use one defined below it
                if (e.RowIndex == _cellRow && e.ColumnIndex == _cellCol && e.ColumnIndex == _nameColumnIndex)
                {
                    string name = e.FormattedValue.ToString().Trim();
                    string error = GetNameError(name);
                    if (error == null && ContainsName(name, e.RowIndex)) error = GetDuplicateNameError(name);
                    if (error != null) throw new CaeException(error);
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
            _cellRow = -1;
            _cellCol = -1;
        }
        private void dgvData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // A pasted cell is changed without an edit, so this is the event that sees every change
            if (!_bindingInProgress) EvaluateParameters();
        }
        private void dgvData_DragDrop(object sender, DragEventArgs e)
        {
            // The rows are reordered by a handler that runs after this one, so the errors of the rows
            // are repainted once the new order is in
            BeginInvoke(new Action(EvaluateParameters));
        }
        //
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                dgvData.EndEdit();
                //
                EvaluateParameters();
                int errorRow = GetFirstErrorRow();
                if (errorRow >= 0)
                {
                    SelectRow(errorRow);
                    string name = _parameters[errorRow].Name;
                    string message = name.Length > 0 ? "The parameter '" + name + "' cannot be used."
                                                     : "The parameter in row " + (errorRow + 1) + " cannot be used.";
                    throw new CaeException(message + Environment.NewLine + _parameters[errorRow].Error);
                }
                // The values the user typed replace the ones a batch run overrode
                _controller.Model.Parameters.OverriddenParameters.Clear();
                //
                List<EquationParameter> parameters = new List<EquationParameter>();
                foreach (ViewEquationParameter parameter in _parameters) parameters.Add(parameter.GetParameter());
                // The whole table is one command: applied one parameter at a time, an intermediate state
                // would hold an equation whose parameter is renamed, removed or not added yet
                if (AreParametersChanged(parameters)) _controller.SetParametersCommand(parameters.ToArray());
                //
                Close();
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
            _nameColumnIndex = dgvData.Columns["Name"].Index;
            _equationColumnIndex = dgvData.Columns["Equation"].Index;
            //
            foreach (DataGridViewColumn column in dgvData.Columns)
            {
                if (column.Index == _equationColumnIndex)
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
        private void EvaluateParameters()
        {
            if (_parameters == null) return;
            //
            double value;
            string error;
            HashSet<string> usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<KeyValuePair<string, string>> nameEquations = new List<KeyValuePair<string, string>>();
            Dictionary<int, string> nameErrors = new Dictionary<int, string>();
            // A row whose name cannot be used is not given to the resolver, it is reported on its own
            for (int i = 0; i < _parameters.Count; i++)
            {
                error = GetNameError(_parameters[i].Name);
                if (error == null && !usedNames.Add(_parameters[i].Name))
                    error = GetDuplicateNameError(_parameters[i].Name);
                //
                if (error != null) nameErrors.Add(i, error);
                else nameEquations.Add(new KeyValuePair<string, string>(_parameters[i].Name, _parameters[i].Equation));
            }
            //
            Dictionary<string, double> values;
            Dictionary<string, string> errors = EquationParameterResolver.Evaluate(nameEquations, _propertyParameters,
                                                                                   out values);
            //
            HashSet<string> autocompleteItems = new HashSet<string>(_propertyParameters.Keys);
            for (int i = 0; i < _parameters.Count; i++)
            {
                if (nameErrors.TryGetValue(i, out error)) _parameters[i].SetError(error);
                else if (errors.TryGetValue(_parameters[i].Name, out error)) _parameters[i].SetError(error);
                else if (values.TryGetValue(_parameters[i].Name, out value))
                {
                    _parameters[i].SetValue(value);
                    autocompleteItems.Add(_parameters[i].Name);
                }
            }
            //
            dgvData.BuildAutocompleteMenu(autocompleteItems, _equationColumnIndex);
            UpdateRowErrors();
            dgvData.Refresh();
        }
        private void UpdateRowErrors()
        {
            DataGridViewRow row;
            for (int i = 0; i < _parameters.Count && i < dgvData.Rows.Count; i++)
            {
                row = dgvData.Rows[i];
                if (_parameters[i].Error == null)
                {
                    row.ErrorText = "";
                    row.DefaultCellStyle.BackColor = Color.Empty;
                }
                else
                {
                    row.ErrorText = _parameters[i].Error;
                    row.DefaultCellStyle.BackColor = ErrorColor;
                }
            }
        }
        private string GetNameError(string name)
        {
            string error = NamedClass.CheckNameError(name, null);
            if (error != null) return error;
            //
            if (char.IsDigit(name[0]))
                return "The parameter name '" + name + "' must not start with a digit.";
            if (_unusableNames.Contains(name))
                return "The parameter name '" + name + "' cannot be used since it is a reserved name.";
            //
            return null;
        }
        private string GetDuplicateNameError(string name)
        {
            return "The parameter named '" + name + "' already exists.";
        }
        private bool ContainsName(string name, int skipRowIndex)
        {
            for (int i = 0; i < _parameters.Count; i++)
            {
                if (i == skipRowIndex) continue;
                if (StringComparer.OrdinalIgnoreCase.Equals(_parameters[i].Name, name)) return true;
            }
            //
            return false;
        }
        private int GetFirstErrorRow()
        {
            for (int i = 0; i < _parameters.Count; i++)
            {
                if (_parameters[i].Error != null) return i;
            }
            //
            return -1;
        }
        private void SelectRow(int rowIndex)
        {
            dgvData.ClearSelection();
            dgvData.Rows[rowIndex].Selected = true;
            dgvData.CurrentCell = dgvData.Rows[rowIndex].Cells[_nameColumnIndex];
        }
        private bool AreParametersChanged(List<EquationParameter> parameters)
        {
            EquationParameterCollection existingParameters = _controller.Model.Parameters;
            if (existingParameters.Keys.Count != parameters.Count) return true;
            //
            int i = 0;
            foreach (var entry in existingParameters)
            {
                if (entry.Key != parameters[i].Name) return true;
                if (!entry.Value.EquationStr.EqualsEquation(parameters[i].EquationStr)) return true;
                i++;
            }
            //
            return false;
        }


    }
}
