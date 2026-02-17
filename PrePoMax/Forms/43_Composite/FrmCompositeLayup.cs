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

namespace PrePoMax.Forms
{
    class FrmCompositeLayup : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables
        private string[] _compositeLayupNames;
        private string _compositeLayupToEditName;
        private CompositeLayup _compositeLayup;
        private Controller _controller;
        private System.ComponentModel.IContainer components;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private Label lNameValue;
        private Label lElementType;
        private Label lElementTypeValue;
        private GroupBox gbLayupOrientation;
        private Label lDefinition;
        private ComboBox cbDefinition;
        private Label lNormalDirection;
        private ComboBox cbNormalDirection;
        private TabControl tcPlies;
        private TabPage tpPlies;
        private DataGridView dgvPlies;
        private Button btnOK;
        private Button btnCancel;
        private Button btnOkAddNew;
        private Button btnAddPly;
        private Button btnRemovePly;
        private Button btnMoveUp;
        private Button btnMoveDown;


        // Properties
        public CompositeLayup CompositeLayup
        {
            get { return _compositeLayup; }
            set { _compositeLayup = value?.DeepClone(); }
        }


        // Constructors
        public FrmCompositeLayup(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _compositeLayup = null;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();

            this.gbData = new System.Windows.Forms.GroupBox();
            this.lName = new System.Windows.Forms.Label();
            this.lNameValue = new System.Windows.Forms.Label();
            this.lElementType = new System.Windows.Forms.Label();
            this.lElementTypeValue = new System.Windows.Forms.Label();
            this.gbLayupOrientation = new System.Windows.Forms.GroupBox();
            this.lDefinition = new System.Windows.Forms.Label();
            this.cbDefinition = new System.Windows.Forms.ComboBox();
            this.lNormalDirection = new System.Windows.Forms.Label();
            this.cbNormalDirection = new System.Windows.Forms.ComboBox();
            this.tcPlies = new System.Windows.Forms.TabControl();
            this.tpPlies = new System.Windows.Forms.TabPage();
            this.dgvPlies = new System.Windows.Forms.DataGridView();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOkAddNew = new System.Windows.Forms.Button();
            this.btnAddPly = new System.Windows.Forms.Button();
            this.btnRemovePly = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();

            this.gbData.SuspendLayout();
            this.gbLayupOrientation.SuspendLayout();
            this.tcPlies.SuspendLayout();
            this.tpPlies.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).BeginInit();
            this.SuspendLayout();

            //
            // gbData
            //
            this.gbData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbData.Controls.Add(this.lName);
            this.gbData.Controls.Add(this.lNameValue);
            this.gbData.Controls.Add(this.lElementType);
            this.gbData.Controls.Add(this.lElementTypeValue);
            this.gbData.Location = new System.Drawing.Point(12, 12);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(560, 70);
            this.gbData.TabIndex = 0;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";

            //
            // lName
            //
            this.lName.AutoSize = true;
            this.lName.Location = new System.Drawing.Point(10, 25);
            this.lName.Name = "lName";
            this.lName.Size = new System.Drawing.Size(38, 15);
            this.lName.TabIndex = 0;
            this.lName.Text = "Name:";

            //
            // lNameValue
            //
            this.lNameValue.AutoSize = true;
            this.lNameValue.Location = new System.Drawing.Point(100, 25);
            this.lNameValue.Name = "lNameValue";
            this.lNameValue.Size = new System.Drawing.Size(100, 15);
            this.lNameValue.TabIndex = 1;
            this.lNameValue.Text = "<compositeLayupName>";

            //
            // lElementType
            //
            this.lElementType.AutoSize = true;
            this.lElementType.Location = new System.Drawing.Point(10, 45);
            this.lElementType.Name = "lElementType";
            this.lElementType.Size = new System.Drawing.Size(75, 15);
            this.lElementType.TabIndex = 2;
            this.lElementType.Text = "Element type:";

            //
            // lElementTypeValue
            //
            this.lElementTypeValue.AutoSize = true;
            this.lElementTypeValue.Location = new System.Drawing.Point(100, 45);
            this.lElementTypeValue.Name = "lElementTypeValue";
            this.lElementTypeValue.Size = new System.Drawing.Size(100, 15);
            this.lElementTypeValue.TabIndex = 3;
            this.lElementTypeValue.Text = "Conventional shell";

            //
            // gbLayupOrientation
            //
            this.gbLayupOrientation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbLayupOrientation.Controls.Add(this.lDefinition);
            this.gbLayupOrientation.Controls.Add(this.cbDefinition);
            this.gbLayupOrientation.Controls.Add(this.lNormalDirection);
            this.gbLayupOrientation.Controls.Add(this.cbNormalDirection);
            this.gbLayupOrientation.Location = new System.Drawing.Point(12, 88);
            this.gbLayupOrientation.Name = "gbLayupOrientation";
            this.gbLayupOrientation.Size = new System.Drawing.Size(560, 80);
            this.gbLayupOrientation.TabIndex = 1;
            this.gbLayupOrientation.TabStop = false;
            this.gbLayupOrientation.Text = "Layup orientation";

            //
            // lDefinition
            //
            this.lDefinition.AutoSize = true;
            this.lDefinition.Location = new System.Drawing.Point(10, 25);
            this.lDefinition.Name = "lDefinition";
            this.lDefinition.Size = new System.Drawing.Size(60, 15);
            this.lDefinition.TabIndex = 0;
            this.lDefinition.Text = "Definition:";

            //
            // cbDefinition
            //
            this.cbDefinition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbDefinition.FormattingEnabled = true;
            this.cbDefinition.Location = new System.Drawing.Point(120, 22);
            this.cbDefinition.Name = "cbDefinition";
            this.cbDefinition.Size = new System.Drawing.Size(200, 23);
            this.cbDefinition.TabIndex = 1;
            this.cbDefinition.SelectedIndexChanged += new System.EventHandler(this.cbDefinition_SelectedIndexChanged);

            //
            // lNormalDirection
            //
            this.lNormalDirection.AutoSize = true;
            this.lNormalDirection.Location = new System.Drawing.Point(10, 52);
            this.lNormalDirection.Name = "lNormalDirection";
            this.lNormalDirection.Size = new System.Drawing.Size(95, 15);
            this.lNormalDirection.TabIndex = 2;
            this.lNormalDirection.Text = "Normal direction:";

            //
            // cbNormalDirection
            //
            this.cbNormalDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbNormalDirection.FormattingEnabled = true;
            this.cbNormalDirection.Location = new System.Drawing.Point(120, 49);
            this.cbNormalDirection.Name = "cbNormalDirection";
            this.cbNormalDirection.Size = new System.Drawing.Size(200, 23);
            this.cbNormalDirection.TabIndex = 3;
            this.cbNormalDirection.SelectedIndexChanged += new System.EventHandler(this.cbNormalDirection_SelectedIndexChanged);

            //
            // tcPlies
            //
            this.tcPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcPlies.Controls.Add(this.tpPlies);
            this.tcPlies.Location = new System.Drawing.Point(12, 174);
            this.tcPlies.Name = "tcPlies";
            this.tcPlies.SelectedIndex = 0;
            this.tcPlies.Size = new System.Drawing.Size(560, 280);
            this.tcPlies.TabIndex = 2;

            //
            // tpPlies
            //
            this.tpPlies.BackColor = System.Drawing.SystemColors.Control;
            this.tpPlies.Controls.Add(this.dgvPlies);
            this.tpPlies.Controls.Add(this.btnAddPly);
            this.tpPlies.Controls.Add(this.btnRemovePly);
            this.tpPlies.Controls.Add(this.btnMoveUp);
            this.tpPlies.Controls.Add(this.btnMoveDown);
            this.tpPlies.Location = new System.Drawing.Point(4, 24);
            this.tpPlies.Name = "tpPlies";
            this.tpPlies.Padding = new System.Windows.Forms.Padding(3);
            this.tpPlies.Size = new System.Drawing.Size(552, 252);
            this.tpPlies.TabIndex = 0;
            this.tpPlies.Text = "Plies";

            //
            // dgvPlies
            //
            this.dgvPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPlies.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 9F);
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvPlies.DefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPlies.Location = new System.Drawing.Point(6, 6);
            this.dgvPlies.Name = "dgvPlies";
            this.dgvPlies.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlies.Size = new System.Drawing.Size(510, 240);
            this.dgvPlies.TabIndex = 0;
            this.dgvPlies.AllowUserToAddRows = false;
            this.dgvPlies.AllowUserToDeleteRows = false;
            this.dgvPlies.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvPlies_CellValueChanged);

            //
            // btnAddPly
            //
            this.btnAddPly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAddPly.Location = new System.Drawing.Point(522, 6);
            this.btnAddPly.Name = "btnAddPly";
            this.btnAddPly.Size = new System.Drawing.Size(24, 24);
            this.btnAddPly.TabIndex = 1;
            this.btnAddPly.Text = "+";
            this.btnAddPly.UseVisualStyleBackColor = true;
            this.btnAddPly.Click += new System.EventHandler(this.btnAddPly_Click);

            //
            // btnRemovePly
            //
            this.btnRemovePly.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemovePly.Location = new System.Drawing.Point(522, 36);
            this.btnRemovePly.Name = "btnRemovePly";
            this.btnRemovePly.Size = new System.Drawing.Size(24, 24);
            this.btnRemovePly.TabIndex = 2;
            this.btnRemovePly.Text = "-";
            this.btnRemovePly.UseVisualStyleBackColor = true;
            this.btnRemovePly.Click += new System.EventHandler(this.btnRemovePly_Click);

            //
            // btnMoveUp
            //
            this.btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveUp.Location = new System.Drawing.Point(522, 76);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(24, 24);
            this.btnMoveUp.TabIndex = 3;
            this.btnMoveUp.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMoveUp.Text = "\u25B2";
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);

            //
            // btnMoveDown
            //
            this.btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveDown.Location = new System.Drawing.Point(522, 106);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(24, 24);
            this.btnMoveDown.TabIndex = 4;
            this.btnMoveDown.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnMoveDown.Text = "\u25BC";
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);

            //
            // btnOkAddNew
            //
            this.btnOkAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOkAddNew.Location = new System.Drawing.Point(335, 462);
            this.btnOkAddNew.Name = "btnOkAddNew";
            this.btnOkAddNew.Size = new System.Drawing.Size(75, 23);
            this.btnOkAddNew.TabIndex = 3;
            this.btnOkAddNew.Text = "OK - New";
            this.btnOkAddNew.UseVisualStyleBackColor = true;
            this.btnOkAddNew.Click += new System.EventHandler(this.btnOkAddNew_Click);

            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(416, 462);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(497, 462);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            //
            // FrmCompositeLayup
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(584, 497);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.gbLayupOrientation);
            this.Controls.Add(this.tcPlies);
            this.Controls.Add(this.btnOkAddNew);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 450);
            this.Name = "FrmCompositeLayup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Create Composite Layup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCompositeLayup_FormClosing);

            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.gbLayupOrientation.ResumeLayout(false);
            this.gbLayupOrientation.PerformLayout();
            this.tcPlies.ResumeLayout(false);
            this.tpPlies.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).EndInit();
            this.ResumeLayout(false);
        }


        // Event handlers
        private void cbDefinition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbDefinition.SelectedIndex >= 0)
            {
                _compositeLayup.OrientationDefinition = (OrientationDefinitionType)cbDefinition.SelectedIndex;
                _propertyItemChanged = true;
            }
        }

        private void cbNormalDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbNormalDirection.SelectedIndex >= 0)
            {
                _compositeLayup.NormalDirection = (NormalDirectionType)cbNormalDirection.SelectedIndex;
                _propertyItemChanged = true;
            }
        }

        private void dgvPlies_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && _compositeLayup != null)
            {
                UpdatePlyFromGrid(e.RowIndex);
                _propertyItemChanged = true;
            }
        }

        private void btnAddPly_Click(object sender, EventArgs e)
        {
            if (_compositeLayup != null)
            {
                int newIndex = _compositeLayup.Plies.Count + 1;
                Ply newPly = new Ply("Ply-" + newIndex);
                _compositeLayup.AddPly(newPly);
                RefreshPliesGrid();
                _propertyItemChanged = true;
            }
        }

        private void btnRemovePly_Click(object sender, EventArgs e)
        {
            if (_compositeLayup != null && dgvPlies.SelectedRows.Count > 0)
            {
                int index = dgvPlies.SelectedRows[0].Index;
                _compositeLayup.RemovePly(index);
                RefreshPliesGrid();
                _propertyItemChanged = true;
            }
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            if (_compositeLayup != null && dgvPlies.SelectedRows.Count > 0)
            {
                int index = dgvPlies.SelectedRows[0].Index;
                if (index > 0)
                {
                    Ply ply = _compositeLayup.Plies[index];
                    _compositeLayup.Plies.RemoveAt(index);
                    _compositeLayup.Plies.Insert(index - 1, ply);
                    RefreshPliesGrid();
                    dgvPlies.Rows[index - 1].Selected = true;
                    _propertyItemChanged = true;
                }
            }
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            if (_compositeLayup != null && dgvPlies.SelectedRows.Count > 0)
            {
                int index = dgvPlies.SelectedRows[0].Index;
                if (index < _compositeLayup.Plies.Count - 1)
                {
                    Ply ply = _compositeLayup.Plies[index];
                    _compositeLayup.Plies.RemoveAt(index);
                    _compositeLayup.Plies.Insert(index + 1, ply);
                    RefreshPliesGrid();
                    dgvPlies.Rows[index + 1].Selected = true;
                    _propertyItemChanged = true;
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Add();
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void btnOkAddNew_Click(object sender, EventArgs e)
        {
            try
            {
                Add();
                PrepareForm(null, null);
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

        private void FrmCompositeLayup_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }


        // Methods
        public bool PrepareForm(string stepName, string compositeLayupToEditName)
        {
            _propertyItemChanged = false;
            _compositeLayupNames = null;
            _compositeLayupToEditName = null;
            _compositeLayup = null;

            this.btnOkAddNew.Visible = false; // Always hide OK-New in edit mode

            _compositeLayupNames = _controller.GetCompositeLayupNames();
            _compositeLayupToEditName = compositeLayupToEditName;

            if (_compositeLayupNames == null)
                throw new CaeException("The composite layup names must be defined first.");

            // Initialize combo boxes
            InitializeComboBoxes();

            // Edit existing composite layup
            if (_compositeLayupToEditName != null)
            {
                CompositeLayup = _controller.GetCompositeLayup(_compositeLayupToEditName);
            }

            this.Text = "Edit Composite Layup";

            // Update UI
            UpdateFormFromLayup();

            // Setup data grid columns
            SetupPliesGrid();

            // Refresh plies grid
            RefreshPliesGrid();

            _controller.SetSelectByToOff();

            return true;
        }

        // Prepare form with a new CompositeLayup (called from FrmCreateCompositeLayup)
        public bool PrepareFormForNewLayup(CompositeLayup newCompositeLayup)
        {
            _propertyItemChanged = false;
            _compositeLayupNames = _controller.GetCompositeLayupNames();
            _compositeLayupToEditName = null; // This is a new layup
            _compositeLayup = newCompositeLayup;

            this.btnOkAddNew.Visible = false;

            if (_compositeLayupNames == null)
                throw new CaeException("The composite layup names must be defined first.");

            // Initialize combo boxes
            InitializeComboBoxes();

            this.Text = "Edit Composite Layup";

            // Update UI
            UpdateFormFromLayup();

            // Setup data grid columns
            SetupPliesGrid();

            // Refresh plies grid
            RefreshPliesGrid();

            _controller.SetSelectByToOff();

            return true;
        }

        private void InitializeComboBoxes()
        {
            // Definition combo box
            cbDefinition.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(OrientationDefinitionType)))
            {
                string displayName = value.ToString();
                if (displayName == "PartGlobal") displayName = "Part global";
                if (displayName == "CoordinateSystem") displayName = "Coordinate system";
                cbDefinition.Items.Add(displayName);
            }

            // Normal direction combo box
            cbNormalDirection.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(NormalDirectionType)))
            {
                cbNormalDirection.Items.Add(value.ToString());
            }
        }

        private void UpdateFormFromLayup()
        {
            if (_compositeLayup != null)
            {
                lNameValue.Text = _compositeLayup.Name;

                // Element type display
                switch (_compositeLayup.ElementType)
                {
                    case CompositeElementType.ConventionalShell:
                        lElementTypeValue.Text = "Conventional shell";
                        break;
                    case CompositeElementType.ContinuumShell:
                        lElementTypeValue.Text = "Continuum shell";
                        break;
                    case CompositeElementType.Solid:
                        lElementTypeValue.Text = "Solid";
                        break;
                }

                cbDefinition.SelectedIndex = (int)_compositeLayup.OrientationDefinition;
                cbNormalDirection.SelectedIndex = (int)_compositeLayup.NormalDirection;
            }
        }

        private void SetupPliesGrid()
        {
            dgvPlies.Columns.Clear();

            // Ply index column (no header text)
            DataGridViewTextBoxColumn colIndex = new DataGridViewTextBoxColumn();
            colIndex.Name = "colIndex";
            colIndex.HeaderText = "";
            colIndex.Width = 30;
            colIndex.ReadOnly = true;
            dgvPlies.Columns.Add(colIndex);

            // Ply Name column
            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.Name = "colName";
            colName.HeaderText = "Ply Name";
            colName.Width = 80;
            dgvPlies.Columns.Add(colName);

            // Region column
            DataGridViewTextBoxColumn colRegion = new DataGridViewTextBoxColumn();
            colRegion.Name = "colRegion";
            colRegion.HeaderText = "Region";
            colRegion.Width = 80;
            dgvPlies.Columns.Add(colRegion);

            // Material column
            DataGridViewTextBoxColumn colMaterial = new DataGridViewTextBoxColumn();
            colMaterial.Name = "colMaterial";
            colMaterial.HeaderText = "Material";
            colMaterial.Width = 80;
            dgvPlies.Columns.Add(colMaterial);

            // Thickness column
            DataGridViewTextBoxColumn colThickness = new DataGridViewTextBoxColumn();
            colThickness.Name = "colThickness";
            colThickness.HeaderText = "Thickness";
            colThickness.Width = 70;
            dgvPlies.Columns.Add(colThickness);

            // CSYS column
            DataGridViewTextBoxColumn colCsys = new DataGridViewTextBoxColumn();
            colCsys.Name = "colCsys";
            colCsys.HeaderText = "CSYS";
            colCsys.Width = 60;
            dgvPlies.Columns.Add(colCsys);

            // Rotation Angle column
            DataGridViewTextBoxColumn colRotation = new DataGridViewTextBoxColumn();
            colRotation.Name = "colRotation";
            colRotation.HeaderText = "Rotation Angle";
            colRotation.Width = 90;
            dgvPlies.Columns.Add(colRotation);
        }

        private void RefreshPliesGrid()
        {
            dgvPlies.Rows.Clear();

            if (_compositeLayup != null)
            {
                for (int i = 0; i < _compositeLayup.Plies.Count; i++)
                {
                    Ply ply = _compositeLayup.Plies[i];
                    dgvPlies.Rows.Add(
                        (i + 1).ToString(),
                        ply.Name,
                        ply.Region,
                        ply.Material,
                        ply.Thickness.ToString(),
                        ply.Csys,
                        ply.RotationAngle.ToString()
                    );
                }
            }
        }

        private void UpdatePlyFromGrid(int rowIndex)
        {
            if (_compositeLayup != null && rowIndex >= 0 && rowIndex < _compositeLayup.Plies.Count)
            {
                Ply ply = _compositeLayup.Plies[rowIndex];
                DataGridViewRow row = dgvPlies.Rows[rowIndex];

                ply.Name = row.Cells["colName"].Value?.ToString() ?? "";
                ply.Region = row.Cells["colRegion"].Value?.ToString() ?? "";
                ply.Material = row.Cells["colMaterial"].Value?.ToString() ?? "";

                if (double.TryParse(row.Cells["colThickness"].Value?.ToString(), out double thickness))
                    ply.Thickness = thickness;

                ply.Csys = row.Cells["colCsys"].Value?.ToString() ?? "";

                if (double.TryParse(row.Cells["colRotation"].Value?.ToString(), out double rotation))
                    ply.RotationAngle = rotation;
            }
        }

        private void Add()
        {
            // Update all plies from grid
            for (int i = 0; i < dgvPlies.Rows.Count; i++)
            {
                UpdatePlyFromGrid(i);
            }

            // Check if the name exists
            UserControls.FrmProperties.CheckName(_compositeLayupToEditName, _compositeLayup.Name, _compositeLayupNames, "composite layup");

            // Create
            if (_compositeLayupToEditName == null)
            {
                _controller.AddCompositeLayupCommand(_compositeLayup);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceCompositeLayupCommand(_compositeLayupToEditName, _compositeLayup);
            }
        }

        private string GetCompositeLayupName(string name)
        {
            return _compositeLayupNames.GetNextNumberedKey(name);
        }
    }
}
