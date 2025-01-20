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
using System.Diagnostics;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace PrePoMax.Forms
{

    public partial class FrmEditCommands : Form
    {
        // Variables                                                                                                                
        private Controller _controller;
        private List<ViewCommand> _viewCommands;
        private Dictionary<double, int> _timeColorId;
        private bool _modified;


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
        //
        private void dgvCommands_DragDrop(object sender, DragEventArgs e)
        {
            _modified = true;
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _modified = true;
            //
           UpdateExecutionTimeColors();
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
                if (meshedPartNames.Add(command.PartName)) commands.Insert(index++, command);
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
                string message = "The history was modified. Changing the history might break the model regeneration." +
                                 " OK to confirm changes?";
                if (_modified && MessageBoxes.ShowWarningQuestionOKCancel(message) == DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                    Hide();
                }
                else if (!_modified)
                {
                    DialogResult = DialogResult.Cancel;
                    Hide();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
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
            //
            SetCommands(commands);
            //
            _modified = false;
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

        
    }
}
