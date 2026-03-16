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
        private int _editingRegionRowIndex = -1;
        private int _editingMaterialRowIndex = -1;
        private int _drapingGeneration = 0;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private TextBox tbName;
        private Label lElementType;
        private Label lElementTypeValue;
        private GroupBox gbLayupOrientation;
        private Label lDefinition;
        private ComboBox cbDefinition;
        private Label lNormalDirection;
        private ComboBox cbNormalDirection;
        private CheckBox cbSymmetric;
        private GroupBox gbDraping;
        private CheckBox cbDrapingSimulation;
        private Label lLimitShear;
        private NumericUpDown nudLimitShear;
        private Label lCellSize;
        private NumericUpDown nudCellSize;
        private Label lMaxShear;
        private TabControl tcPlies;
        private TabPage tpPlies;
        private TabPage tpOffset;
        private GroupBox gbShellOffset;
        private RadioButton rbMiddleSurface;
        private RadioButton rbTopSurface;
        private RadioButton rbBottomSurface;
        private RadioButton rbSpecifyRatio;
        private NumericUpDown nudOffsetRatio;
        private TabPage tpDisplay;
        private GroupBox gbHighlightPlyRegions;
        private RadioButton rbHighlightOn;
        private RadioButton rbHighlightOff;
        private GroupBox gbOrientationDisplay;
        private RadioButton rbOrientationLayup;
        private RadioButton rbOrientationNone;
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
            this.tbName = new System.Windows.Forms.TextBox();
            this.lElementType = new System.Windows.Forms.Label();
            this.lElementTypeValue = new System.Windows.Forms.Label();
            this.gbLayupOrientation = new System.Windows.Forms.GroupBox();
            this.lDefinition = new System.Windows.Forms.Label();
            this.cbDefinition = new System.Windows.Forms.ComboBox();
            this.lNormalDirection = new System.Windows.Forms.Label();
            this.cbNormalDirection = new System.Windows.Forms.ComboBox();
            this.cbSymmetric = new System.Windows.Forms.CheckBox();
            this.gbDraping = new System.Windows.Forms.GroupBox();
            this.cbDrapingSimulation = new System.Windows.Forms.CheckBox();
            this.lLimitShear = new System.Windows.Forms.Label();
            this.nudLimitShear = new System.Windows.Forms.NumericUpDown();
            this.lCellSize = new System.Windows.Forms.Label();
            this.nudCellSize = new System.Windows.Forms.NumericUpDown();
            this.lMaxShear = new System.Windows.Forms.Label();
            this.tcPlies = new System.Windows.Forms.TabControl();
            this.tpPlies = new System.Windows.Forms.TabPage();
            this.tpOffset = new System.Windows.Forms.TabPage();
            this.gbShellOffset = new System.Windows.Forms.GroupBox();
            this.rbMiddleSurface = new System.Windows.Forms.RadioButton();
            this.rbTopSurface = new System.Windows.Forms.RadioButton();
            this.rbBottomSurface = new System.Windows.Forms.RadioButton();
            this.rbSpecifyRatio = new System.Windows.Forms.RadioButton();
            this.nudOffsetRatio = new System.Windows.Forms.NumericUpDown();
            this.tpDisplay = new System.Windows.Forms.TabPage();
            this.gbHighlightPlyRegions = new System.Windows.Forms.GroupBox();
            this.rbHighlightOn = new System.Windows.Forms.RadioButton();
            this.rbHighlightOff = new System.Windows.Forms.RadioButton();
            this.gbOrientationDisplay = new System.Windows.Forms.GroupBox();
            this.rbOrientationLayup = new System.Windows.Forms.RadioButton();
            this.rbOrientationNone = new System.Windows.Forms.RadioButton();
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
            this.gbDraping.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLimitShear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCellSize)).BeginInit();
            this.tcPlies.SuspendLayout();
            this.tpPlies.SuspendLayout();
            this.tpOffset.SuspendLayout();
            this.gbShellOffset.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetRatio)).BeginInit();
            this.tpDisplay.SuspendLayout();
            this.gbHighlightPlyRegions.SuspendLayout();
            this.gbOrientationDisplay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).BeginInit();
            this.SuspendLayout();

            //
            // gbData
            //
            this.gbData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbData.Controls.Add(this.lName);
            this.gbData.Controls.Add(this.tbName);
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
            // tbName
            //
            this.tbName.Location = new System.Drawing.Point(100, 22);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(200, 23);
            this.tbName.TabIndex = 1;
            this.tbName.TextChanged += new System.EventHandler(this.tbName_TextChanged);

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
            // cbSymmetric
            //
            this.cbSymmetric.AutoSize = true;
            this.cbSymmetric.Location = new System.Drawing.Point(12, 176);
            this.cbSymmetric.Name = "cbSymmetric";
            this.cbSymmetric.Size = new System.Drawing.Size(80, 19);
            this.cbSymmetric.TabIndex = 2;
            this.cbSymmetric.Text = "Symmetric";
            this.cbSymmetric.UseVisualStyleBackColor = true;
            this.cbSymmetric.CheckedChanged += new System.EventHandler(this.cbSymmetric_CheckedChanged);

            //
            // gbDraping
            //
            this.gbDraping.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDraping.Controls.Add(this.cbDrapingSimulation);
            this.gbDraping.Controls.Add(this.lMaxShear);
            this.gbDraping.Controls.Add(this.lLimitShear);
            this.gbDraping.Controls.Add(this.nudLimitShear);
            this.gbDraping.Controls.Add(this.lCellSize);
            this.gbDraping.Controls.Add(this.nudCellSize);
            this.gbDraping.Location = new System.Drawing.Point(12, 200);
            this.gbDraping.Name = "gbDraping";
            this.gbDraping.Size = new System.Drawing.Size(560, 82);
            this.gbDraping.TabIndex = 3;
            this.gbDraping.TabStop = false;
            this.gbDraping.Text = "Manufacturing";

            //
            // cbDrapingSimulation
            //
            this.cbDrapingSimulation.AutoSize = true;
            this.cbDrapingSimulation.Location = new System.Drawing.Point(10, 22);
            this.cbDrapingSimulation.Name = "cbDrapingSimulation";
            this.cbDrapingSimulation.Size = new System.Drawing.Size(120, 19);
            this.cbDrapingSimulation.TabIndex = 0;
            this.cbDrapingSimulation.Text = "Draping simulation";
            this.cbDrapingSimulation.UseVisualStyleBackColor = true;
            this.cbDrapingSimulation.CheckedChanged += new System.EventHandler(this.cbDrapingSimulation_CheckedChanged);

            //
            // lLimitShear
            //
            this.lLimitShear.AutoSize = true;
            this.lLimitShear.Location = new System.Drawing.Point(10, 50);
            this.lLimitShear.Name = "lLimitShear";
            this.lLimitShear.Size = new System.Drawing.Size(90, 15);
            this.lLimitShear.Text = "Limit shear (°):";
            this.lLimitShear.Visible = false;

            //
            // nudLimitShear
            //
            this.nudLimitShear.DecimalPlaces = 1;
            this.nudLimitShear.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudLimitShear.Location = new System.Drawing.Point(110, 47);
            this.nudLimitShear.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.nudLimitShear.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudLimitShear.Name = "nudLimitShear";
            this.nudLimitShear.Size = new System.Drawing.Size(70, 23);
            this.nudLimitShear.TabIndex = 1;
            this.nudLimitShear.Value = new decimal(new int[] { 45, 0, 0, 0 });
            this.nudLimitShear.Visible = false;

            //
            // lCellSize
            //
            this.lCellSize.AutoSize = true;
            this.lCellSize.Location = new System.Drawing.Point(200, 50);
            this.lCellSize.Name = "lCellSize";
            this.lCellSize.Size = new System.Drawing.Size(55, 15);
            this.lCellSize.Text = "Cell size:";
            this.lCellSize.Visible = false;

            //
            // nudCellSize
            //
            this.nudCellSize.DecimalPlaces = 4;
            this.nudCellSize.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudCellSize.Location = new System.Drawing.Point(265, 47);
            this.nudCellSize.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudCellSize.Minimum = new decimal(new int[] { 1, 0, 0, 262144 });
            this.nudCellSize.Name = "nudCellSize";
            this.nudCellSize.Size = new System.Drawing.Size(90, 23);
            this.nudCellSize.TabIndex = 2;
            this.nudCellSize.Value = new decimal(new int[] { 5, 0, 0, 131072 });
            this.nudCellSize.Visible = false;

            //
            // lMaxShear
            //
            this.lMaxShear.AutoSize = true;
            this.lMaxShear.Location = new System.Drawing.Point(380, 22);
            this.lMaxShear.Name = "lMaxShear";
            this.lMaxShear.Size = new System.Drawing.Size(120, 15);
            this.lMaxShear.Text = "Max shear: --";
            this.lMaxShear.Visible = false;

            //
            // tcPlies
            //
            this.tcPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcPlies.Controls.Add(this.tpPlies);
            this.tcPlies.Controls.Add(this.tpOffset);
            this.tcPlies.Controls.Add(this.tpDisplay);
            this.tcPlies.Location = new System.Drawing.Point(12, 290);
            this.tcPlies.Name = "tcPlies";
            this.tcPlies.SelectedIndex = 0;
            this.tcPlies.Size = new System.Drawing.Size(560, 256);
            this.tcPlies.TabIndex = 4;

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
            this.dgvPlies.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvPlies_CellClick);
            this.dgvPlies.SelectionChanged += new System.EventHandler(this.dgvPlies_SelectionChanged);

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
            // tpOffset
            //
            this.tpOffset.BackColor = System.Drawing.SystemColors.Control;
            this.tpOffset.Controls.Add(this.gbShellOffset);
            this.tpOffset.Location = new System.Drawing.Point(4, 24);
            this.tpOffset.Name = "tpOffset";
            this.tpOffset.Padding = new System.Windows.Forms.Padding(3);
            this.tpOffset.Size = new System.Drawing.Size(552, 252);
            this.tpOffset.TabIndex = 1;
            this.tpOffset.Text = "Offset";

            //
            // gbShellOffset
            //
            this.gbShellOffset.Controls.Add(this.rbMiddleSurface);
            this.gbShellOffset.Controls.Add(this.rbTopSurface);
            this.gbShellOffset.Controls.Add(this.rbBottomSurface);
            this.gbShellOffset.Controls.Add(this.rbSpecifyRatio);
            this.gbShellOffset.Controls.Add(this.nudOffsetRatio);
            this.gbShellOffset.Location = new System.Drawing.Point(8, 8);
            this.gbShellOffset.Name = "gbShellOffset";
            this.gbShellOffset.Size = new System.Drawing.Size(400, 130);
            this.gbShellOffset.TabIndex = 0;
            this.gbShellOffset.TabStop = false;
            this.gbShellOffset.Text = "Shell reference surface and offsets";

            //
            // rbMiddleSurface
            //
            this.rbMiddleSurface.AutoSize = true;
            this.rbMiddleSurface.Checked = true;
            this.rbMiddleSurface.Location = new System.Drawing.Point(10, 22);
            this.rbMiddleSurface.Name = "rbMiddleSurface";
            this.rbMiddleSurface.Size = new System.Drawing.Size(110, 19);
            this.rbMiddleSurface.TabIndex = 0;
            this.rbMiddleSurface.TabStop = true;
            this.rbMiddleSurface.Text = "Middle surface";
            this.rbMiddleSurface.UseVisualStyleBackColor = true;
            this.rbMiddleSurface.CheckedChanged += new System.EventHandler(this.rbShellOffset_CheckedChanged);

            //
            // rbTopSurface
            //
            this.rbTopSurface.AutoSize = true;
            this.rbTopSurface.Location = new System.Drawing.Point(10, 44);
            this.rbTopSurface.Name = "rbTopSurface";
            this.rbTopSurface.Size = new System.Drawing.Size(90, 19);
            this.rbTopSurface.TabIndex = 1;
            this.rbTopSurface.Text = "Top surface";
            this.rbTopSurface.UseVisualStyleBackColor = true;
            this.rbTopSurface.CheckedChanged += new System.EventHandler(this.rbShellOffset_CheckedChanged);

            //
            // rbBottomSurface
            //
            this.rbBottomSurface.AutoSize = true;
            this.rbBottomSurface.Location = new System.Drawing.Point(10, 66);
            this.rbBottomSurface.Name = "rbBottomSurface";
            this.rbBottomSurface.Size = new System.Drawing.Size(103, 19);
            this.rbBottomSurface.TabIndex = 2;
            this.rbBottomSurface.Text = "Bottom surface";
            this.rbBottomSurface.UseVisualStyleBackColor = true;
            this.rbBottomSurface.CheckedChanged += new System.EventHandler(this.rbShellOffset_CheckedChanged);

            //
            // rbSpecifyRatio
            //
            this.rbSpecifyRatio.AutoSize = true;
            this.rbSpecifyRatio.Location = new System.Drawing.Point(10, 88);
            this.rbSpecifyRatio.Name = "rbSpecifyRatio";
            this.rbSpecifyRatio.Size = new System.Drawing.Size(105, 19);
            this.rbSpecifyRatio.TabIndex = 3;
            this.rbSpecifyRatio.Text = "Specify offset ratio";
            this.rbSpecifyRatio.UseVisualStyleBackColor = true;
            this.rbSpecifyRatio.CheckedChanged += new System.EventHandler(this.rbShellOffset_CheckedChanged);

            //
            // nudOffsetRatio
            //
            this.nudOffsetRatio.DecimalPlaces = 4;
            this.nudOffsetRatio.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudOffsetRatio.Location = new System.Drawing.Point(130, 86);
            this.nudOffsetRatio.Minimum = new decimal(new int[] { 10, 0, 0, -2147418112 });
            this.nudOffsetRatio.Maximum = new decimal(new int[] { 10, 0, 0, 65536 });
            this.nudOffsetRatio.Name = "nudOffsetRatio";
            this.nudOffsetRatio.Size = new System.Drawing.Size(90, 23);
            this.nudOffsetRatio.TabIndex = 4;
            this.nudOffsetRatio.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudOffsetRatio.Enabled = false;
            this.nudOffsetRatio.ValueChanged += new System.EventHandler(this.nudOffsetRatio_ValueChanged);

            //
            // tpDisplay
            //
            this.tpDisplay.BackColor = System.Drawing.SystemColors.Control;
            this.tpDisplay.Controls.Add(this.gbHighlightPlyRegions);
            this.tpDisplay.Controls.Add(this.gbOrientationDisplay);
            this.tpDisplay.Location = new System.Drawing.Point(4, 24);
            this.tpDisplay.Name = "tpDisplay";
            this.tpDisplay.Padding = new System.Windows.Forms.Padding(3);
            this.tpDisplay.Size = new System.Drawing.Size(552, 252);
            this.tpDisplay.TabIndex = 2;
            this.tpDisplay.Text = "Display";

            //
            // gbHighlightPlyRegions
            //
            this.gbHighlightPlyRegions.Controls.Add(this.rbHighlightOn);
            this.gbHighlightPlyRegions.Controls.Add(this.rbHighlightOff);
            this.gbHighlightPlyRegions.Location = new System.Drawing.Point(8, 8);
            this.gbHighlightPlyRegions.Name = "gbHighlightPlyRegions";
            this.gbHighlightPlyRegions.Size = new System.Drawing.Size(200, 70);
            this.gbHighlightPlyRegions.TabIndex = 0;
            this.gbHighlightPlyRegions.TabStop = false;
            this.gbHighlightPlyRegions.Text = "Highlight ply regions";

            //
            // rbHighlightOn
            //
            this.rbHighlightOn.AutoSize = true;
            this.rbHighlightOn.Checked = true;
            this.rbHighlightOn.Location = new System.Drawing.Point(10, 22);
            this.rbHighlightOn.Name = "rbHighlightOn";
            this.rbHighlightOn.Size = new System.Drawing.Size(40, 19);
            this.rbHighlightOn.TabIndex = 0;
            this.rbHighlightOn.TabStop = true;
            this.rbHighlightOn.Text = "On";
            this.rbHighlightOn.UseVisualStyleBackColor = true;
            this.rbHighlightOn.CheckedChanged += new System.EventHandler(this.rbHighlightOn_CheckedChanged);

            //
            // rbHighlightOff
            //
            this.rbHighlightOff.AutoSize = true;
            this.rbHighlightOff.Location = new System.Drawing.Point(10, 44);
            this.rbHighlightOff.Name = "rbHighlightOff";
            this.rbHighlightOff.Size = new System.Drawing.Size(41, 19);
            this.rbHighlightOff.TabIndex = 1;
            this.rbHighlightOff.Text = "Off";
            this.rbHighlightOff.UseVisualStyleBackColor = true;

            //
            // gbOrientationDisplay
            //
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationLayup);
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationNone);
            this.gbOrientationDisplay.Location = new System.Drawing.Point(220, 8);
            this.gbOrientationDisplay.Name = "gbOrientationDisplay";
            this.gbOrientationDisplay.Size = new System.Drawing.Size(200, 70);
            this.gbOrientationDisplay.TabIndex = 1;
            this.gbOrientationDisplay.TabStop = false;
            this.gbOrientationDisplay.Text = "Orientation display";

            //
            // rbOrientationLayup
            //
            this.rbOrientationLayup.AutoSize = true;
            this.rbOrientationLayup.Checked = true;
            this.rbOrientationLayup.Location = new System.Drawing.Point(10, 22);
            this.rbOrientationLayup.Name = "rbOrientationLayup";
            this.rbOrientationLayup.Size = new System.Drawing.Size(60, 19);
            this.rbOrientationLayup.TabIndex = 0;
            this.rbOrientationLayup.TabStop = true;
            this.rbOrientationLayup.Text = "Layup";
            this.rbOrientationLayup.UseVisualStyleBackColor = true;
            this.rbOrientationLayup.CheckedChanged += new System.EventHandler(this.rbOrientationLayup_CheckedChanged);

            //
            // rbOrientationNone
            //
            this.rbOrientationNone.AutoSize = true;
            this.rbOrientationNone.Location = new System.Drawing.Point(10, 44);
            this.rbOrientationNone.Name = "rbOrientationNone";
            this.rbOrientationNone.Size = new System.Drawing.Size(50, 19);
            this.rbOrientationNone.TabIndex = 1;
            this.rbOrientationNone.Text = "None";
            this.rbOrientationNone.UseVisualStyleBackColor = true;

            //
            // btnOkAddNew
            //
            this.btnOkAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOkAddNew.Location = new System.Drawing.Point(335, 554);
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
            this.btnOK.Location = new System.Drawing.Point(416, 554);
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
            this.btnCancel.Location = new System.Drawing.Point(497, 554);
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
            this.ClientSize = new System.Drawing.Size(584, 589);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.gbLayupOrientation);
            this.Controls.Add(this.cbSymmetric);
            this.Controls.Add(this.gbDraping);
            this.Controls.Add(this.tcPlies);
            this.Controls.Add(this.btnOkAddNew);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 540);
            this.Name = "FrmCompositeLayup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Create Composite Layup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCompositeLayup_FormClosing);

            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.gbLayupOrientation.ResumeLayout(false);
            this.gbLayupOrientation.PerformLayout();
            this.gbDraping.ResumeLayout(false);
            this.gbDraping.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLimitShear)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCellSize)).EndInit();
            this.tcPlies.ResumeLayout(false);
            this.tpPlies.ResumeLayout(false);
            this.gbShellOffset.ResumeLayout(false);
            this.gbShellOffset.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetRatio)).EndInit();
            this.tpOffset.ResumeLayout(false);
            this.gbHighlightPlyRegions.ResumeLayout(false);
            this.gbHighlightPlyRegions.PerformLayout();
            this.gbOrientationDisplay.ResumeLayout(false);
            this.gbOrientationDisplay.PerformLayout();
            this.tpDisplay.ResumeLayout(false);
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

        private void cbSymmetric_CheckedChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null)
            {
                _compositeLayup.Symmetric = cbSymmetric.Checked;
                _propertyItemChanged = true;
            }
        }

        private void cbDrapingSimulation_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = cbDrapingSimulation.Checked;
            lLimitShear.Visible = enabled;
            nudLimitShear.Visible = enabled;
            lCellSize.Visible = enabled;
            nudCellSize.Visible = enabled;
            if (!enabled)
            {
                CancelDraping();
            }
            else
            {
                if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
                {
                    int plyIndex = dgvPlies.SelectedRows[0].Index;
                    // Auto-detect cellSize from ply region bounding box
                    double suggested = _controller.SuggestDrapingCellSize(_compositeLayup, plyIndex);
                    if (suggested > 0)
                    {
                        decimal clamped = Math.Max(nudCellSize.Minimum,
                            Math.Min(nudCellSize.Maximum, Math.Round((decimal)suggested, 4)));
                        nudCellSize.Value = clamped;
                    }
                    TriggerDrapingAsync(plyIndex);
                }
            }
        }

        private void tbName_TextChanged(object sender, EventArgs e)
        {
            // Don't assign the name immediately - validation will happen in Add() method
            // This allows users to type characters that will be validated later
            _propertyItemChanged = true;
        }

        private void dgvPlies_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && _compositeLayup != null)
            {
                UpdatePlyFromGrid(e.RowIndex);
                _propertyItemChanged = true;
            }
        }

        private void dgvPlies_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && _compositeLayup != null)
            {
                string columnName = dgvPlies.Columns[e.ColumnIndex].Name;
                if (columnName == "colRegion")
                {
                    // Check if element sets exist
                    string[] elementSetNames = _controller.GetUserElementSetNames();
                    if (elementSetNames == null || elementSetNames.Length == 0)
                    {
                        MessageBox.Show("No element sets are defined in the model. Please create an element set first.",
                                        "No Element Sets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _editingRegionRowIndex = e.RowIndex;
                    string currentRegion = _compositeLayup.Plies[e.RowIndex].Region;
                    _controller.Form.ShowSelectElementSet(currentRegion, OnElementSetSelected);
                }
                else if (columnName == "colMaterial")
                {
                    // Check if materials exist
                    Material[] materials = _controller.GetAllMaterials();
                    if (materials == null || materials.Length == 0)
                    {
                        MessageBox.Show("No materials are defined in the model. Please create a material first.",
                                        "No Materials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    _editingMaterialRowIndex = e.RowIndex;
                    string currentMaterial = _compositeLayup.Plies[e.RowIndex].Material;
                    _controller.Form.ShowSelectMaterial(currentMaterial, OnMaterialSelected);
                }
            }
        }

        private void OnElementSetSelected(string elementSetName)
        {
            if (_editingRegionRowIndex >= 0 && _editingRegionRowIndex < _compositeLayup.Plies.Count)
            {
                _compositeLayup.Plies[_editingRegionRowIndex].Region = elementSetName;
                dgvPlies.Rows[_editingRegionRowIndex].Cells["colRegion"].Value = elementSetName;
                _propertyItemChanged = true;
            }
            _editingRegionRowIndex = -1;
        }

        private void OnMaterialSelected(string materialName)
        {
            if (_editingMaterialRowIndex >= 0 && _editingMaterialRowIndex < _compositeLayup.Plies.Count)
            {
                _compositeLayup.Plies[_editingMaterialRowIndex].Material = materialName;
                dgvPlies.Rows[_editingMaterialRowIndex].Cells["colMaterial"].Value = materialName;
                _propertyItemChanged = true;
            }
            _editingMaterialRowIndex = -1;
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

        private void dgvPlies_SelectionChanged(object sender, EventArgs e)
        {
            if (_compositeLayup == null) return;
            if (dgvPlies.SelectedRows.Count > 0)
            {
                int plyIndex = dgvPlies.SelectedRows[0].Index;
                if (rbOrientationLayup.Checked)
                    _controller.DrawPlyOrientationArrows(_compositeLayup, plyIndex);
                else
                    _controller.ClearPlyOrientationArrows();
                if (rbHighlightOn.Checked)
                    HighlightSelectedPlyRegion(plyIndex);
                else
                    _controller.Form.Clear3DSelection();
                if (cbDrapingSimulation.Checked)
                    TriggerDrapingAsync(plyIndex);
                else
                    CancelDraping();
            }
            else
            {
                _controller.ClearPlyOrientationArrows();
                _controller.Form.Clear3DSelection();
                CancelDraping();
            }
        }

        private void HighlightSelectedPlyRegion(int plyIndex)
        {
            if (_compositeLayup == null || plyIndex < 0 || plyIndex >= _compositeLayup.Plies.Count) return;
            string region = _compositeLayup.Plies[plyIndex].Region;
            _controller.Form.Clear3DSelection();
            if (string.IsNullOrEmpty(region)) return;
            _controller.HighlightElementSet(region);
        }

        private void rbHighlightOn_CheckedChanged(object sender, EventArgs e)
        {
            if (rbHighlightOn.Checked)
            {
                if (dgvPlies.SelectedRows.Count > 0)
                    HighlightSelectedPlyRegion(dgvPlies.SelectedRows[0].Index);
            }
            else
            {
                _controller.Form.Clear3DSelection();
            }
        }

        private void rbOrientationLayup_CheckedChanged(object sender, EventArgs e)
        {
            if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                if (rbOrientationLayup.Checked)
                    _controller.DrawPlyOrientationArrows(_compositeLayup, dgvPlies.SelectedRows[0].Index);
                else
                    _controller.ClearPlyOrientationArrows();
            }
            else if (!rbOrientationLayup.Checked)
            {
                _controller.ClearPlyOrientationArrows();
            }
        }

        private void rbShellOffset_CheckedChanged(object sender, EventArgs e)
        {
            nudOffsetRatio.Enabled = rbSpecifyRatio.Checked;
            if (_compositeLayup == null) return;
            if (rbMiddleSurface.Checked) _compositeLayup.ShellOffset = ShellOffsetType.MiddleSurface;
            else if (rbTopSurface.Checked) _compositeLayup.ShellOffset = ShellOffsetType.TopSurface;
            else if (rbBottomSurface.Checked) _compositeLayup.ShellOffset = ShellOffsetType.BottomSurface;
            else if (rbSpecifyRatio.Checked) _compositeLayup.ShellOffset = ShellOffsetType.SpecifyRatio;
            _propertyItemChanged = true;
        }

        private void nudOffsetRatio_ValueChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null)
            {
                _compositeLayup.OffsetRatio = (double)nudOffsetRatio.Value;
                _propertyItemChanged = true;
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                _controller.ClearPlyOrientationArrows();
                _controller.Form.Clear3DSelection();
                CancelDraping();
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
            _controller.ClearPlyOrientationArrows();
            _controller.Form.Clear3DSelection();
            CancelDraping();
            Hide();
        }

        private void FrmCompositeLayup_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _controller.ClearPlyOrientationArrows();
                _controller.Form.Clear3DSelection();
                CancelDraping();
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
                tbName.Text = _compositeLayup.Name;

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
                cbSymmetric.Checked = _compositeLayup.Symmetric;

                rbMiddleSurface.Checked = _compositeLayup.ShellOffset == ShellOffsetType.MiddleSurface;
                rbTopSurface.Checked = _compositeLayup.ShellOffset == ShellOffsetType.TopSurface;
                rbBottomSurface.Checked = _compositeLayup.ShellOffset == ShellOffsetType.BottomSurface;
                rbSpecifyRatio.Checked = _compositeLayup.ShellOffset == ShellOffsetType.SpecifyRatio;
                nudOffsetRatio.Value = (decimal)Math.Max((double)nudOffsetRatio.Minimum,
                    Math.Min((double)nudOffsetRatio.Maximum, _compositeLayup.OffsetRatio));
                nudOffsetRatio.Enabled = rbSpecifyRatio.Checked;
            }
            // Reset draping controls
            cbDrapingSimulation.Checked = false;
            lLimitShear.Visible = false;
            nudLimitShear.Visible = false;
            lCellSize.Visible = false;
            nudCellSize.Visible = false;
            lMaxShear.Visible = false;
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

            // Region column (read-only, click to select from element sets)
            DataGridViewTextBoxColumn colRegion = new DataGridViewTextBoxColumn();
            colRegion.Name = "colRegion";
            colRegion.HeaderText = "Region";
            colRegion.Width = 80;
            colRegion.ReadOnly = true;
            dgvPlies.Columns.Add(colRegion);

            // Material column (read-only, click to select from materials)
            DataGridViewTextBoxColumn colMaterial = new DataGridViewTextBoxColumn();
            colMaterial.Name = "colMaterial";
            colMaterial.HeaderText = "Material";
            colMaterial.Width = 80;
            colMaterial.ReadOnly = true;
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
                    if (string.IsNullOrEmpty(ply.Csys)) ply.Csys = "<layup>";
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

            // Assign the name from the TextBox (validation happens in the Name setter)
            _compositeLayup.Name = tbName.Text.Trim();

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

        private void UpdateMaxShearLabel(double maxShear)
        {
            if (maxShear >= 0)
            {
                lMaxShear.Text = $"Max shear: {maxShear:F1}°";
                lMaxShear.Visible = true;
            }
            else
            {
                lMaxShear.Visible = false;
            }
        }
        private void TriggerDrapingAsync(int plyIndex)
        {
            int gen = ++_drapingGeneration;
            lMaxShear.Text = "Computing...";
            lMaxShear.Visible = true;
            //
            CompositeLayup layup = _compositeLayup;
            double limitShear = (double)nudLimitShear.Value;
            double cellSize = (double)nudCellSize.Value;
            //
            Task.Run(() => _controller.DrawDrapingSimulation(layup, plyIndex, limitShear, cellSize))
                .ContinueWith(t =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((Action)(() =>
                    {
                        if (gen != _drapingGeneration) return;
                        if (!t.IsFaulted && t.Result >= 0)
                            UpdateMaxShearLabel(t.Result);
                        else
                            lMaxShear.Visible = false;
                    }));
                });
        }
        private void CancelDraping()
        {
            ++_drapingGeneration;
            _controller.ClearDrapingSimulation();
            lMaxShear.Visible = false;
        }

        private string GetCompositeLayupName(string name)
        {
            return _compositeLayupNames.GetNextNumberedKey(name);
        }
    }
}
