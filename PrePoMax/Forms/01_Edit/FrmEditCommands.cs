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
using CaeMesh;
using System.Reflection;
using CaeGlobals;
using DynamicTypeDescriptor;
using CaeJob;
using PrePoMax.Commands;
using PrePoMax.Settings;
using PrePoMax.Python.Journal;
using System.Diagnostics;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace PrePoMax.Forms
{
    public partial class FrmEditCommands : Form
    {
        // Variables                                                                                                                
        private const string PythonScriptName = "History.py";
        //
        private Controller _controller;
        private List<ViewCommand> _viewCommands;
        private Dictionary<double, int> _timeColorId;
        private bool _modified;
        private FrmViewObjectProperties _frmViewCommandProperties;
        private int[] _pythonCommandFirstLines;
        private bool _pythonScriptOutdated;
        private List<Command> _preparedCommands;
        private bool _symbolsDrawn;


        // Properties                                                                                                               
        public List<Command> Commands
        {
            get
            {
                List<Command> commands = new List<Command>();
                foreach (var viewCommand in _viewCommands) commands.Add(viewCommand.Command);
                return commands;
            }
        }


        // Constructors                                                                                                             
        public FrmEditCommands(Controller controller)
        {
            InitializeComponent();
            //
            dgvCommands.EnableDragAndDropRows();
            _controller = controller;
            _viewCommands = null;
            _modified = false;
            _frmViewCommandProperties = new FrmViewObjectProperties();
            _frmViewCommandProperties.StartPosition = FormStartPosition.CenterParent;
            _pythonCommandFirstLines = null;
            _pythonScriptOutdated = true;
            // The script is a view of the history, not a place to write one
            petbPython.ReadOnly = true;
            petbPython.AllowRunCommands = false;
            splMain.Panel2Collapsed = !tsmiShowPython.Checked;
        }


        // Event handlers                                                                                                           
        private void tsmiOpen_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PrePoMax history|*.pmh";
                    openFileDialog.FileName = "";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        OpenPmh(openFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "PrePoMax history|*.pmh";
                    saveFileDialog.FileName = "History";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        SavePmh(saveFileDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiSaveAsPython_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "Python files|*.py";
                    saveFileDialog.FileName = "History";
                    //
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // The dialog may hold a reordered or shortened history, so what is shown is emitted. An
                        // unedited history is shown with the commands an undo took back, which the model is not
                        // built by, so the journal leaves them out as Regenerate does
                        List<Command> commands = _viewCommands == null || IsShowingPreparedHistory() ?
                                                 _controller.GetActiveCommands() : Commands;
                        PythonJournalReport report =
                            PythonJournalExporter.Export(commands, saveFileDialog.FileName, _controller.OpenedFileName,
                                                         _controller.Settings.General.NumOfSplitFaces,
                                                         _controller.Settings.GetWorkDirectory());
                        if (report.RefusedCommandCount > 0)
                            MessageBoxes.ShowWarning(report.GetSummary() + Environment.NewLine +
                                                     string.Join(Environment.NewLine, report.Refusals));
                    }
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void tsmiClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
        //
        private void tsmiColorByType_Click(object sender, EventArgs e)
        {
            ColorTypeChanged(sender);
        }
        private void tsmiColorByTime_Click(object sender, EventArgs e)
        {
            ColorTypeChanged(sender);
        }
        private void tsmiShowPython_Click(object sender, EventArgs e)
        {
            tsmiShowPython.Checked = !tsmiShowPython.Checked;
            splMain.Panel2Collapsed = !tsmiShowPython.Checked;
            //
            if (tsmiShowPython.Checked && _pythonScriptOutdated) UpdatePythonScript();
        }
        //
        private void dgvCommands_DragDrop(object sender, DragEventArgs e)
        {
            _modified = true;
            // The rows are reordered by a handler that runs after this one, so the script waits for the new order
            BeginInvoke(new Action(UpdatePythonScript));
        }
        private void dgvCommands_SelectionChanged(object sender, EventArgs e)
        {
            HighlightSelectedCommandInPythonScript();
            DrawSelectedCommandSymbols();
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _modified = true;
            //
           UpdateExecutionTimeColors();
            //
            UpdatePythonScript();
        }
        private void dgvCommands_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            Color blue = Color.FromArgb(225, 245, 255);
            Color green = Color.FromArgb(235, 255, 235);
            Color yellow = Color.FromArgb(255, 255, 205);
            Color red = Color.FromArgb(255, 235, 215);
            //
            if (tsmiColorByType.Checked)
            {
                string type = dgvCommands.Rows[e.RowIndex].Cells[3].Value.ToString();
                if (type == "Pre-process")
                {
                    dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = green;
                }
                else if (type == "Analysis")
                {
                    dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = red;
                }
                else if (type == "Post-process")
                {
                    dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = blue;
                }
                else if (type == "File")
                {
                    dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = yellow;
                }
            }
            else if (tsmiColorByTime.Checked)
            {
                double time;
                if (double.TryParse(dgvCommands.Rows[e.RowIndex].Cells[5].Value.ToString(), out time))
                {
                    int colorId = _timeColorId[time];
                    if (colorId == 1) dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = blue;
                    else if (colorId == 2) dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = green;
                    else if (colorId == 3) dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = yellow;
                    else if (colorId >= 4) dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = red;
                }
            }
            else throw new NotSupportedException();
        }
        //
        private void btnReset_Click(object sender, EventArgs e)
        {
            PrepareForm();
        }
        private void btnReorganize_Click(object sender, EventArgs e)
        {
            List<Command> importCommands = new List<Command>();
            List<Command> meshSetupCommands = new List<Command>();
            List<CCreateMesh> meshCommands = new List<CCreateMesh>();
            //
            int count = 0;
            int index = -1;
            List<Command> commands = Commands;
            foreach (var command in commands)
            {
                if (command is CSetNewModelProperties)
                {
                    index = count;
                }
                else if (command is CImportFile)
                {
                    importCommands.Add(command);
                }
                else if (command is CAddMeshSetupItem || command is CReplaceMeshSetupItem || command is CDuplicateMeshSetupItems ||
                         command is CRemoveMeshSetupItems)
                {
                    meshSetupCommands.Add(command);
                }
                else if (command is CActivateDeactivate ad && ad.ContainsMeshSetupItem())
                {
                    meshSetupCommands.Add(command);
                }
                else if (command is CActivateDeactivateMultiple adm && adm.ContainsMeshSetupItem())
                {
                    meshSetupCommands.Add(command);
                }
                else if (command is CCreateMesh cm)
                {
                    meshCommands.Add(cm);
                }
                //
                count++;
            }
            //
            foreach (var command in importCommands) commands.Remove(command);
            foreach (var command in meshSetupCommands) commands.Remove(command);
            foreach (var command in meshCommands) commands.Remove(command);
            //
            index++;
            HashSet<string> meshedPartNames = new HashSet<string>();
            foreach (var command in importCommands) commands.Insert(index++, command);
            foreach (var command in meshSetupCommands) commands.Insert(index++, command);
            foreach (var command in meshCommands)
            {
                string[] newPartNames = command.PartNames.Where(name => meshedPartNames.Add(name)).ToArray();
                if (newPartNames.Length == command.PartNames.Length) commands.Insert(index++, command);
                else if (newPartNames.Length > 0) commands.Insert(index++, new CCreateMesh(newPartNames));
            }
            //
            SetCommands(commands);
        }
        private void btnClearAll_Click(object sender, EventArgs e)
        {
            dgvCommands.DataSource = null;
            //
            List<ViewCommand> _readOnly = new List<ViewCommand>();
            for (int i = 0; i < 2 && i < _viewCommands.Count(); i++) _readOnly.Add(_viewCommands[i]);
            _viewCommands = _readOnly;
            //
            SetBinding();
            //
            _modified = true;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_modified)
                {
                    Hide();
                    return;
                }
                // The dialog does not block the main window, so the history can grow while it is open, and
                // applying the copy shown here would throw the new commands away
                if (IsHistoryChangedSincePrepare())
                {
                    MessageBoxes.ShowWarning("The history was changed while this dialog was open. Press Reset " +
                                             "to load the current history before editing it.");
                    return;
                }
                //
                string message = "The history was modified. Changing the history might break the model regeneration." +
                                 " OK to confirm changes?";
                if (MessageBoxes.ShowWarningQuestionOKCancel(message) == DialogResult.OK)
                {
                    List<Command> commands = Commands;
                    Hide();
                    _controller.SetCommands(commands);
                }
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
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            // The form is created once and shown again, so closing it only hides it
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void dgvCommands_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            //if (e.Row.Index < 2)
            //{
            //    e.Cancel = true;
            //}
            UpdateExecutionTimeColors();
        }


        // Methods                                                                                                                  
        public void PrepareForm()
        {
            List<Command> commands = _controller.GetCommands();
            _preparedCommands = new List<Command>(commands);
            //
            SetCommands(commands);
            //
            _modified = false;
        }
        private bool IsHistoryChangedSincePrepare()
        {
            List<Command> commands = _controller.GetCommands();
            if (_preparedCommands == null || commands.Count != _preparedCommands.Count) return true;
            //
            for (int i = 0; i < commands.Count; i++)
            {
                if (!ReferenceEquals(commands[i], _preparedCommands[i])) return true;
            }
            return false;
        }
        private bool IsShowingPreparedHistory()
        {
            if (_viewCommands == null || _preparedCommands == null || _viewCommands.Count != _preparedCommands.Count)
                return false;
            //
            for (int i = 0; i < _viewCommands.Count; i++)
            {
                if (!ReferenceEquals(_viewCommands[i].Command, _preparedCommands[i])) return false;
            }
            return !IsHistoryChangedSincePrepare();
        }
        private void OpenPmh(string fileName)
        {
            List<Command> commands;
            CommandsCollection.ReadFromFile(fileName, out commands);
            //
            if (commands != null)
            {
                _viewCommands.Clear();
                //
                int id = 1;
                foreach (var command in commands) _viewCommands.Add(new ViewCommand(id++, command));
                //
                SetBinding();
            }
        }
        private void SavePmh(string fileName)
        {
            CommandsCollection.WriteToFile(Commands, fileName);
        }
        private void SetBinding()
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = _viewCommands;
            binding.ListChanged += Binding_ListChanged;
            dgvCommands.DataSource = binding; //bind dataGridView to binding source - enables adding of new lines
            //
            dgvCommands.Columns["Id"].Width = 40;
            dgvCommands.Columns["Id"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCommands.Columns["DateTime"].Width = 110;
            dgvCommands.Columns["Name"].Width = 170;
            dgvCommands.Columns["Type"].Width = 100;
            dgvCommands.Columns["Data"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvCommands.Columns["ExecutionTimeString"].Width = 75;
            dgvCommands.Columns["ExecutionTimeString"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            //
            UpdateExecutionTimeColors();
            //
            UpdatePythonScript();
        }
        //
        private void SetCommands(List<Command> commands)
        {
            _viewCommands = new List<ViewCommand>();
            //
            if (commands != null)
            {
                int id = 1;
                foreach (var command in commands) _viewCommands.Add(new ViewCommand(id++, command));
            }
            //
            SetBinding();
            //
            _modified = true;
        }
        private void ColorTypeChanged(object sender)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                if (tsmi == tsmiColorByType)
                {
                    tsmiColorByType.Checked = true;
                    tsmiColorByTime.Checked = false;
                }
                else if (tsmi == tsmiColorByTime)
                {
                    tsmiColorByType.Checked = false;
                    tsmiColorByTime.Checked = true;
                }
                else throw new NotSupportedException();
            }
            //
            dgvCommands.Invalidate();
        }
        private void UpdateExecutionTimeColors()
        {
            int count = 0;
            double[] times = new double[_viewCommands.Count];
            //
            foreach (var command in _viewCommands) times[count++] = command.ExecutionTime;
            Array.Sort(times);
            //
            double position;
            _timeColorId = new Dictionary<double, int>();
            for (int i = 0; i < times.Length; i++)
            {
                position = (double)i / times.Length;
                if (position < 0.4) _timeColorId[times[i]] = 1;
                else if (position < 0.8) _timeColorId[times[i]] = 2;
                else if (position < 0.9) _timeColorId[times[i]] = 3;
                else if (position <= 1) _timeColorId[times[i]] = 4;
            }  
            //
            dgvCommands.Invalidate();
        }
        // The script mirrors the rows as they are now, so a reordered or shortened history is shown as it would be saved
        private void UpdatePythonScript()
        {
            if (_viewCommands == null) return;
            if (splMain.Panel2Collapsed)
            {
                _pythonScriptOutdated = true;
                return;
            }
            //
            PythonJournalReport report;
            int[] commandFirstLines;
            string text = PythonJournalExporter.GetText(Commands, PythonScriptName, _controller.OpenedFileName,
                                                        out report, out commandFirstLines,
                                                        _controller.Settings.General.NumOfSplitFaces,
                                                        _controller.Settings.GetWorkDirectory());
            //
            _pythonCommandFirstLines = commandFirstLines;
            _pythonScriptOutdated = false;
            petbPython.Text = text;
            //
            HighlightSelectedCommandInPythonScript();
        }
        private void HighlightSelectedCommandInPythonScript()
        {
            if (splMain.Panel2Collapsed || _pythonCommandFirstLines == null) return;
            if (dgvCommands.CurrentRow == null) return;
            //
            int rowIndex = dgvCommands.CurrentRow.Index;
            if (rowIndex < 0 || rowIndex >= _pythonCommandFirstLines.Length) return;
            //
            int firstLine = _pythonCommandFirstLines[rowIndex];
            int lastLine = int.MaxValue;
            if (rowIndex + 1 < _pythonCommandFirstLines.Length) lastLine = _pythonCommandFirstLines[rowIndex + 1] - 1;
            //
            petbPython.SelectLines(firstLine, lastLine);
        }
        private void DrawSelectedCommandSymbols()
        {
            try
            {
                if (!Visible || _viewCommands == null) return;
                //
                CSplitGeometry splitGeometry = null;
                int rowIndex = dgvCommands.CurrentRow != null ? dgvCommands.CurrentRow.Index : -1;
                if (rowIndex >= 0 && rowIndex < _viewCommands.Count)
                    splitGeometry = _viewCommands[rowIndex].Command as CSplitGeometry;
                //
                SplitToolSymbol toolSymbol = splitGeometry?.ToolSymbol;
                if (toolSymbol != null || _symbolsDrawn) _controller.DrawSplitGeometryCommandSymbols(toolSymbol);
                //
                _symbolsDrawn = toolSymbol != null;
            }
            catch { }
        }
        private void ClearCommandSymbols()
        {
            if (!_symbolsDrawn) return;
            //
            _symbolsDrawn = false;
            try
            {
                _controller.DrawSplitGeometryCommandSymbols(null);
            }
            catch { }
        }
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            //
            if (Visible) DrawSelectedCommandSymbols();
            else ClearCommandSymbols();
        }

        private void dgvCommands_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (!_frmViewCommandProperties.Visible && e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                List<Command> commands = _controller.GetCommands();
                Command command = commands[e.RowIndex];
                ViewObjectTypeDescriptor viewObject = new ViewObjectTypeDescriptor(command);
                //
                _frmViewCommandProperties.PrepareForm("Command Properties", viewObject);
                _frmViewCommandProperties.ShowDialog(this);
            }
        }
    }
}
