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

namespace PrePoMax.Forms
{
   
    public partial class FrmEditCommands : Form
    {
        // Variables                                                                                                                
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
        public FrmEditCommands(List<Command> commands)
        {
            InitializeComponent();
            _modified = false;
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
        }


        // Event handlers                                                                                                           
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _modified = true;
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                string message = "The history was modified. Changing the history might break the model regeneration." +
                                 " OK to confirm changes?";
                if (_modified && MessageBoxes.ShowWarningQuestion(message) == DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnClearAll_Click(object sender, EventArgs e)
        {
            dgvCommands.DataSource = null;
            //
            List<ViewCommand> _readOnly = new List<ViewCommand>();
            for (int i = 0; i < 2; i++) _readOnly.Add(_viewCommands[i]);
            _viewCommands = _readOnly;
            //
            SetBinding();
            //
            _modified = true;
        }
        private void dgvCommands_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            if (e.Row.Index < 2)
            {
                e.Cancel = true;
            }
        }


        // Methods                                                                                                                  
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
            dgvCommands.Columns["Data"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

    }
}
