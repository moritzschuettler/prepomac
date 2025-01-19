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

namespace PrePoMax.Forms
{
   
    public partial class FrmEditCommands : Form
    {
        // Variables                                                                                                                
        private Controller _controller;
        private List<ViewCommand> _viewCommands;
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
        private void dgvCommands_DragDrop(object sender, DragEventArgs e)
        {
            _modified = true;
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _modified = true;
        }
        private void dgvCommands_RowPrePaint(object sender, DataGridViewRowPrePaintEventArgs e)
        {
            string type = dgvCommands.Rows[e.RowIndex].Cells[3].Value.ToString();
            if (type == "Pre-process")
            {
                dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(235, 255, 235);
            }
            else if (type == "Analysis")
            {
                dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 215);
            }
            else if (type == "Post-process")
            {
                dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(235, 235, 255);
            }
            else if (type == "File")
            {
                dgvCommands.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 205);
            }
        }
        private void btnReset_Click(object sender, EventArgs e)
        {
            PrepareForm();
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
        }


        // Methods                                                                                                                  
        public void PrepareForm()
        {
            List<Command> commands = _controller.GetCommands();
            //
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
            dgvCommands.Columns["ExecutionTime"].Width = 75;
            dgvCommands.Columns["ExecutionTime"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }
    }
}
