// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
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
using System.Globalization;

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
        private int _contextMenuRegionRowIndex = -1;
        private int _contextMenuOrientationRowIndex = -1;
        // The Definition combo box only lists Rosette when the model contains rosettes, so the
        // combo index cannot be cast to the enum any more
        private List<OrientationDefinitionType> _definitionItems = new List<OrientationDefinitionType>();
        private List<ProcessType> _processTypeItems = new List<ProcessType>();
        private int _drapingGeneration = 0;
        private int _propagationGeneration = 0;
        private bool _loadingPropagationControls = false;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private TextBox tbName;
        private Label lElementType;
        private Label lElementTypeValue;
        private Label lProcessType;
        private ComboBox cbProcessType;
        private GroupBox gbLayupOrientation;
        private Label lDefinition;
        private ComboBox cbDefinition;
        private Label lNormalDirection;
        private ComboBox cbNormalDirection;
        private Label lAdditionalRotation;
        private ComboBox cbAdditionalRotation;
        private NumericUpDown nudAdditionalRotationAngle;
        private CheckBox cbSymmetric;
        private GroupBox gbDraping;
        private CheckBox cbDrapingSimulation;
        private Label lLimitShear;
        private NumericUpDown nudLimitShear;
        private Label lCellSize;
        private NumericUpDown nudCellSize;
        private Label lMaxShear;
        private CheckBox cbMapFiberFromDraping;
        private Label lDiscreteOrientationsFile;
        private TextBox tbDiscreteOrientationsFile;
        private Button btnBrowseDiscreteOrientations;
        private Button btnClearDiscreteOrientations;
        private Label lDiscreteSolidHint;
        private Label lLayupRosette;
        private ComboBox cbLayupRosette;
        private GroupBox gbPropagation;
        private CheckBox cbShowPropagation;
        private Label lPropagationStatus;
        private Label lSeedPoint;
        private TextBox tbSeedX;
        private TextBox tbSeedY;
        private TextBox tbSeedZ;
        private CheckBox chkPickSeedPoint;
        private Label lPropagationType;
        private ComboBox cbPropagationType;
        private Label lTapeWidth;
        private NumericUpDown nudTapeWidth;
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
        private RadioButton rbOrientationAll;
        private RadioButton rbOrientationLayup;
        private RadioButton rbOrientationXY;
        private RadioButton rbOrientationNone;
        private Label lArrowDensity;
        private NumericUpDown nudArrowDensity;
        private DataGridView dgvPlies;
        private ContextMenuStrip cmsRegionColumn;
        private ToolStripMenuItem tsmiImportRegionFromSurface;
        private ContextMenuStrip cmsOrientationColumn;
        private ToolStripMenuItem tsmiAssignRosette;
        private ToolStripMenuItem tsmiResetToLayupRosette;
        private ToolStripMenuItem tsmiClearRosette;
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
            this.lProcessType = new System.Windows.Forms.Label();
            this.cbProcessType = new System.Windows.Forms.ComboBox();
            this.gbLayupOrientation = new System.Windows.Forms.GroupBox();
            this.lDefinition = new System.Windows.Forms.Label();
            this.cbDefinition = new System.Windows.Forms.ComboBox();
            this.lNormalDirection = new System.Windows.Forms.Label();
            this.cbNormalDirection = new System.Windows.Forms.ComboBox();
            this.lAdditionalRotation = new System.Windows.Forms.Label();
            this.cbAdditionalRotation = new System.Windows.Forms.ComboBox();
            this.nudAdditionalRotationAngle = new System.Windows.Forms.NumericUpDown();
            this.cbSymmetric = new System.Windows.Forms.CheckBox();
            this.gbDraping = new System.Windows.Forms.GroupBox();
            this.cbDrapingSimulation = new System.Windows.Forms.CheckBox();
            this.lLimitShear = new System.Windows.Forms.Label();
            this.nudLimitShear = new System.Windows.Forms.NumericUpDown();
            this.lCellSize = new System.Windows.Forms.Label();
            this.nudCellSize = new System.Windows.Forms.NumericUpDown();
            this.lMaxShear = new System.Windows.Forms.Label();
            this.cbMapFiberFromDraping = new System.Windows.Forms.CheckBox();
            this.lDiscreteOrientationsFile = new System.Windows.Forms.Label();
            this.tbDiscreteOrientationsFile = new System.Windows.Forms.TextBox();
            this.btnBrowseDiscreteOrientations = new System.Windows.Forms.Button();
            this.btnClearDiscreteOrientations = new System.Windows.Forms.Button();
            this.lLayupRosette = new System.Windows.Forms.Label();
            this.cbLayupRosette = new System.Windows.Forms.ComboBox();
            this.lDiscreteSolidHint = new System.Windows.Forms.Label();
            this.gbPropagation = new System.Windows.Forms.GroupBox();
            this.cbShowPropagation = new System.Windows.Forms.CheckBox();
            this.lPropagationStatus = new System.Windows.Forms.Label();
            this.lSeedPoint = new System.Windows.Forms.Label();
            this.tbSeedX = new System.Windows.Forms.TextBox();
            this.tbSeedY = new System.Windows.Forms.TextBox();
            this.tbSeedZ = new System.Windows.Forms.TextBox();
            this.chkPickSeedPoint = new System.Windows.Forms.CheckBox();
            this.lPropagationType = new System.Windows.Forms.Label();
            this.cbPropagationType = new System.Windows.Forms.ComboBox();
            this.lTapeWidth = new System.Windows.Forms.Label();
            this.nudTapeWidth = new System.Windows.Forms.NumericUpDown();
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
            this.rbOrientationAll = new System.Windows.Forms.RadioButton();
            this.rbOrientationLayup = new System.Windows.Forms.RadioButton();
            this.rbOrientationXY = new System.Windows.Forms.RadioButton();
            this.rbOrientationNone = new System.Windows.Forms.RadioButton();
            this.lArrowDensity = new System.Windows.Forms.Label();
            this.nudArrowDensity = new System.Windows.Forms.NumericUpDown();
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
            ((System.ComponentModel.ISupportInitialize)(this.nudAdditionalRotationAngle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudLimitShear)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCellSize)).BeginInit();
            this.gbPropagation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTapeWidth)).BeginInit();
            this.tcPlies.SuspendLayout();
            this.tpPlies.SuspendLayout();
            this.tpOffset.SuspendLayout();
            this.gbShellOffset.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetRatio)).BeginInit();
            this.tpDisplay.SuspendLayout();
            this.gbHighlightPlyRegions.SuspendLayout();
            this.gbOrientationDisplay.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudArrowDensity)).BeginInit();
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
            this.gbData.Controls.Add(this.lProcessType);
            this.gbData.Controls.Add(this.cbProcessType);
            this.gbData.Location = new System.Drawing.Point(12, 12);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(560, 95);
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
            // lProcessType
            //
            this.lProcessType.AutoSize = true;
            this.lProcessType.Location = new System.Drawing.Point(10, 71);
            this.lProcessType.Name = "lProcessType";
            this.lProcessType.Size = new System.Drawing.Size(75, 15);
            this.lProcessType.TabIndex = 4;
            this.lProcessType.Text = "Process type:";

            //
            // cbProcessType
            //
            this.cbProcessType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProcessType.FormattingEnabled = true;
            this.cbProcessType.Location = new System.Drawing.Point(100, 68);
            this.cbProcessType.Name = "cbProcessType";
            this.cbProcessType.Size = new System.Drawing.Size(200, 23);
            this.cbProcessType.TabIndex = 5;
            this.cbProcessType.SelectedIndexChanged += new System.EventHandler(this.cbProcessType_SelectedIndexChanged);

            //
            // gbLayupOrientation
            //
            this.gbLayupOrientation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbLayupOrientation.Controls.Add(this.lDefinition);
            this.gbLayupOrientation.Controls.Add(this.cbDefinition);
            this.gbLayupOrientation.Controls.Add(this.lNormalDirection);
            this.gbLayupOrientation.Controls.Add(this.cbNormalDirection);
            this.gbLayupOrientation.Controls.Add(this.lAdditionalRotation);
            this.gbLayupOrientation.Controls.Add(this.cbAdditionalRotation);
            this.gbLayupOrientation.Controls.Add(this.nudAdditionalRotationAngle);
            this.gbLayupOrientation.Controls.Add(this.lDiscreteOrientationsFile);
            this.gbLayupOrientation.Controls.Add(this.tbDiscreteOrientationsFile);
            this.gbLayupOrientation.Controls.Add(this.btnBrowseDiscreteOrientations);
            this.gbLayupOrientation.Controls.Add(this.btnClearDiscreteOrientations);
            this.gbLayupOrientation.Controls.Add(this.lDiscreteSolidHint);
            this.gbLayupOrientation.Controls.Add(this.lLayupRosette);
            this.gbLayupOrientation.Controls.Add(this.cbLayupRosette);
            this.gbLayupOrientation.Location = new System.Drawing.Point(12, 113);
            this.gbLayupOrientation.Name = "gbLayupOrientation";
            this.gbLayupOrientation.Size = new System.Drawing.Size(560, 110);
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
            // lAdditionalRotation
            //
            this.lAdditionalRotation.AutoSize = true;
            this.lAdditionalRotation.Location = new System.Drawing.Point(10, 79);
            this.lAdditionalRotation.Name = "lAdditionalRotation";
            this.lAdditionalRotation.Size = new System.Drawing.Size(105, 15);
            this.lAdditionalRotation.TabIndex = 4;
            this.lAdditionalRotation.Text = "Additional rotation:";
            this.lAdditionalRotation.Visible = false;

            //
            // cbAdditionalRotation
            //
            this.cbAdditionalRotation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAdditionalRotation.FormattingEnabled = true;
            this.cbAdditionalRotation.Location = new System.Drawing.Point(120, 76);
            this.cbAdditionalRotation.Name = "cbAdditionalRotation";
            this.cbAdditionalRotation.Size = new System.Drawing.Size(120, 23);
            this.cbAdditionalRotation.TabIndex = 5;
            this.cbAdditionalRotation.Visible = false;
            this.cbAdditionalRotation.SelectedIndexChanged += new System.EventHandler(this.cbAdditionalRotation_SelectedIndexChanged);

            //
            // nudAdditionalRotationAngle
            //
            this.nudAdditionalRotationAngle.DecimalPlaces = 2;
            this.nudAdditionalRotationAngle.Minimum = new decimal(new int[] { 36000, 0, 0, -2147483648 });
            this.nudAdditionalRotationAngle.Maximum = new decimal(new int[] { 36000, 0, 0, 0 });
            this.nudAdditionalRotationAngle.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudAdditionalRotationAngle.Location = new System.Drawing.Point(248, 76);
            this.nudAdditionalRotationAngle.Name = "nudAdditionalRotationAngle";
            this.nudAdditionalRotationAngle.Size = new System.Drawing.Size(80, 23);
            this.nudAdditionalRotationAngle.TabIndex = 6;
            this.nudAdditionalRotationAngle.Visible = false;
            this.nudAdditionalRotationAngle.Enabled = false;
            this.nudAdditionalRotationAngle.ValueChanged += new System.EventHandler(this.nudAdditionalRotationAngle_ValueChanged);

            //
            // cbSymmetric
            //
            this.cbSymmetric.AutoSize = true;
            this.cbSymmetric.Location = new System.Drawing.Point(12, 231);
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
            this.gbDraping.Controls.Add(this.cbMapFiberFromDraping);
            this.gbDraping.Controls.Add(this.lLimitShear);
            this.gbDraping.Controls.Add(this.nudLimitShear);
            this.gbDraping.Controls.Add(this.lCellSize);
            this.gbDraping.Controls.Add(this.nudCellSize);
            this.gbDraping.Location = new System.Drawing.Point(12, 255);
            this.gbDraping.Name = "gbDraping";
            this.gbDraping.Size = new System.Drawing.Size(560, 97);
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
            this.lLimitShear.Location = new System.Drawing.Point(10, 72);
            this.lLimitShear.Name = "lLimitShear";
            this.lLimitShear.Size = new System.Drawing.Size(90, 15);
            this.lLimitShear.Text = "Limit shear (°):";
            this.lLimitShear.Visible = false;

            //
            // nudLimitShear
            //
            this.nudLimitShear.DecimalPlaces = 1;
            this.nudLimitShear.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudLimitShear.Location = new System.Drawing.Point(110, 70);
            this.nudLimitShear.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.nudLimitShear.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudLimitShear.Name = "nudLimitShear";
            this.nudLimitShear.Size = new System.Drawing.Size(70, 23);
            this.nudLimitShear.TabIndex = 1;
            this.nudLimitShear.Value = new decimal(new int[] { 45, 0, 0, 0 });
            this.nudLimitShear.Visible = false;
            this.nudLimitShear.ValueChanged += new System.EventHandler(this.nudLimitShear_ValueChanged);

            //
            // lCellSize
            //
            this.lCellSize.AutoSize = true;
            this.lCellSize.Location = new System.Drawing.Point(200, 72);
            this.lCellSize.Name = "lCellSize";
            this.lCellSize.Size = new System.Drawing.Size(55, 15);
            this.lCellSize.Text = "Cell size:";
            this.lCellSize.Visible = false;

            //
            // nudCellSize
            //
            this.nudCellSize.DecimalPlaces = 4;
            this.nudCellSize.Increment = new decimal(new int[] { 1, 0, 0, 131072 });
            this.nudCellSize.Location = new System.Drawing.Point(265, 70);
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
            // cbMapFiberFromDraping
            //
            this.cbMapFiberFromDraping.AutoSize = true;
            this.cbMapFiberFromDraping.Location = new System.Drawing.Point(10, 47);
            this.cbMapFiberFromDraping.Name = "cbMapFiberFromDraping";
            this.cbMapFiberFromDraping.Size = new System.Drawing.Size(130, 19);
            this.cbMapFiberFromDraping.TabIndex = 4;
            this.cbMapFiberFromDraping.Text = "Map fiber direction (discrete orientations)";
            this.cbMapFiberFromDraping.UseVisualStyleBackColor = true;
            this.cbMapFiberFromDraping.Visible = false;
            this.cbMapFiberFromDraping.CheckedChanged += new System.EventHandler(this.cbMapFiberFromDraping_CheckedChanged);

            //
            // lDiscreteOrientationsFile
            //
            this.lDiscreteOrientationsFile.AutoSize = true;
            this.lDiscreteOrientationsFile.Location = new System.Drawing.Point(10, 52);
            this.lDiscreteOrientationsFile.Name = "lDiscreteOrientationsFile";
            this.lDiscreteOrientationsFile.Text = "XML file:";
            this.lDiscreteOrientationsFile.Visible = false;

            //
            // tbDiscreteOrientationsFile
            //
            this.tbDiscreteOrientationsFile.Location = new System.Drawing.Point(75, 49);
            this.tbDiscreteOrientationsFile.Name = "tbDiscreteOrientationsFile";
            this.tbDiscreteOrientationsFile.ReadOnly = true;
            this.tbDiscreteOrientationsFile.Size = new System.Drawing.Size(350, 23);
            this.tbDiscreteOrientationsFile.TabIndex = 7;
            this.tbDiscreteOrientationsFile.Visible = false;

            //
            // btnBrowseDiscreteOrientations
            //
            this.btnBrowseDiscreteOrientations.Location = new System.Drawing.Point(432, 49);
            this.btnBrowseDiscreteOrientations.Name = "btnBrowseDiscreteOrientations";
            this.btnBrowseDiscreteOrientations.Size = new System.Drawing.Size(60, 23);
            this.btnBrowseDiscreteOrientations.TabIndex = 8;
            this.btnBrowseDiscreteOrientations.Text = "Browse...";
            this.btnBrowseDiscreteOrientations.UseVisualStyleBackColor = true;
            this.btnBrowseDiscreteOrientations.Visible = false;
            this.btnBrowseDiscreteOrientations.Click += new System.EventHandler(this.btnBrowseDiscreteOrientations_Click);

            //
            // btnClearDiscreteOrientations
            //
            this.btnClearDiscreteOrientations.Location = new System.Drawing.Point(498, 49);
            this.btnClearDiscreteOrientations.Name = "btnClearDiscreteOrientations";
            this.btnClearDiscreteOrientations.Size = new System.Drawing.Size(50, 23);
            this.btnClearDiscreteOrientations.TabIndex = 9;
            this.btnClearDiscreteOrientations.Text = "Clear";
            this.btnClearDiscreteOrientations.UseVisualStyleBackColor = true;
            this.btnClearDiscreteOrientations.Visible = false;
            this.btnClearDiscreteOrientations.Click += new System.EventHandler(this.btnClearDiscreteOrientations_Click);

            //
            // lLayupRosette
            //
            this.lLayupRosette.AutoSize = true;
            this.lLayupRosette.Location = new System.Drawing.Point(10, 52);
            this.lLayupRosette.Name = "lLayupRosette";
            this.lLayupRosette.Text = "Rosette:";
            this.lLayupRosette.Visible = false;

            //
            // cbLayupRosette
            //
            this.cbLayupRosette.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLayupRosette.FormattingEnabled = true;
            this.cbLayupRosette.Location = new System.Drawing.Point(75, 49);
            this.cbLayupRosette.Name = "cbLayupRosette";
            this.cbLayupRosette.Size = new System.Drawing.Size(200, 23);
            this.cbLayupRosette.TabIndex = 10;
            this.cbLayupRosette.Visible = false;
            this.cbLayupRosette.SelectedIndexChanged += new System.EventHandler(this.cbLayupRosette_SelectedIndexChanged);

            //
            // lDiscreteSolidHint
            //
            this.lDiscreteSolidHint.AutoSize = true;
            this.lDiscreteSolidHint.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lDiscreteSolidHint.Location = new System.Drawing.Point(10, 79);
            this.lDiscreteSolidHint.Name = "lDiscreteSolidHint";
            this.lDiscreteSolidHint.Text = "Solid: each ply region must be a one-element-thick layer set; " +
                "layer k of each stacking point maps to ply k.";
            this.lDiscreteSolidHint.Visible = false;

            //
            // gbPropagation
            //
            this.gbPropagation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbPropagation.Controls.Add(this.cbShowPropagation);
            this.gbPropagation.Controls.Add(this.lPropagationStatus);
            this.gbPropagation.Controls.Add(this.lSeedPoint);
            this.gbPropagation.Controls.Add(this.tbSeedX);
            this.gbPropagation.Controls.Add(this.tbSeedY);
            this.gbPropagation.Controls.Add(this.tbSeedZ);
            this.gbPropagation.Controls.Add(this.chkPickSeedPoint);
            this.gbPropagation.Controls.Add(this.lPropagationType);
            this.gbPropagation.Controls.Add(this.cbPropagationType);
            this.gbPropagation.Controls.Add(this.lTapeWidth);
            this.gbPropagation.Controls.Add(this.nudTapeWidth);
            this.gbPropagation.Location = new System.Drawing.Point(12, 358);
            this.gbPropagation.Name = "gbPropagation";
            this.gbPropagation.Size = new System.Drawing.Size(560, 110);
            this.gbPropagation.TabIndex = 3;
            this.gbPropagation.TabStop = false;
            this.gbPropagation.Text = "Tape Propagation";
            this.gbPropagation.Visible = false;

            //
            // cbShowPropagation
            //
            this.cbShowPropagation.AutoSize = true;
            this.cbShowPropagation.Location = new System.Drawing.Point(10, 22);
            this.cbShowPropagation.Name = "cbShowPropagation";
            this.cbShowPropagation.Size = new System.Drawing.Size(120, 19);
            this.cbShowPropagation.TabIndex = 0;
            this.cbShowPropagation.Text = "Show propagation";
            this.cbShowPropagation.UseVisualStyleBackColor = true;
            this.cbShowPropagation.CheckedChanged += new System.EventHandler(this.cbShowPropagation_CheckedChanged);

            //
            // lPropagationStatus
            //
            this.lPropagationStatus.AutoSize = true;
            this.lPropagationStatus.Location = new System.Drawing.Point(380, 22);
            this.lPropagationStatus.Name = "lPropagationStatus";
            this.lPropagationStatus.Size = new System.Drawing.Size(120, 15);
            this.lPropagationStatus.Text = "";
            this.lPropagationStatus.Visible = false;

            //
            // lSeedPoint
            //
            this.lSeedPoint.AutoSize = true;
            this.lSeedPoint.Location = new System.Drawing.Point(10, 50);
            this.lSeedPoint.Name = "lSeedPoint";
            this.lSeedPoint.Size = new System.Drawing.Size(65, 15);
            this.lSeedPoint.Text = "Seed point:";

            //
            // tbSeedX
            //
            this.tbSeedX.Location = new System.Drawing.Point(80, 47);
            this.tbSeedX.Name = "tbSeedX";
            this.tbSeedX.Size = new System.Drawing.Size(65, 23);
            this.tbSeedX.TabIndex = 1;
            this.tbSeedX.Text = "0";
            this.tbSeedX.Leave += new System.EventHandler(this.tbSeed_Leave);

            //
            // tbSeedY
            //
            this.tbSeedY.Location = new System.Drawing.Point(150, 47);
            this.tbSeedY.Name = "tbSeedY";
            this.tbSeedY.Size = new System.Drawing.Size(65, 23);
            this.tbSeedY.TabIndex = 2;
            this.tbSeedY.Text = "0";
            this.tbSeedY.Leave += new System.EventHandler(this.tbSeed_Leave);

            //
            // tbSeedZ
            //
            this.tbSeedZ.Location = new System.Drawing.Point(220, 47);
            this.tbSeedZ.Name = "tbSeedZ";
            this.tbSeedZ.Size = new System.Drawing.Size(65, 23);
            this.tbSeedZ.TabIndex = 3;
            this.tbSeedZ.Text = "0";
            this.tbSeedZ.Leave += new System.EventHandler(this.tbSeed_Leave);

            //
            // chkPickSeedPoint
            //
            this.chkPickSeedPoint.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkPickSeedPoint.Location = new System.Drawing.Point(295, 47);
            this.chkPickSeedPoint.Name = "chkPickSeedPoint";
            this.chkPickSeedPoint.Size = new System.Drawing.Size(60, 23);
            this.chkPickSeedPoint.TabIndex = 4;
            this.chkPickSeedPoint.Text = "Pick";
            this.chkPickSeedPoint.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPickSeedPoint.UseVisualStyleBackColor = false;
            this.chkPickSeedPoint.CheckedChanged += new System.EventHandler(this.chkPickSeedPoint_CheckedChanged);

            //
            // lPropagationType
            //
            this.lPropagationType.AutoSize = true;
            this.lPropagationType.Location = new System.Drawing.Point(10, 78);
            this.lPropagationType.Name = "lPropagationType";
            this.lPropagationType.Size = new System.Drawing.Size(80, 15);
            this.lPropagationType.Text = "Propagation:";

            //
            // cbPropagationType
            //
            this.cbPropagationType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPropagationType.FormattingEnabled = true;
            this.cbPropagationType.Location = new System.Drawing.Point(95, 75);
            this.cbPropagationType.Name = "cbPropagationType";
            this.cbPropagationType.Size = new System.Drawing.Size(150, 23);
            this.cbPropagationType.TabIndex = 5;
            this.cbPropagationType.SelectedIndexChanged += new System.EventHandler(this.cbPropagationType_SelectedIndexChanged);

            //
            // lTapeWidth
            //
            this.lTapeWidth.AutoSize = true;
            this.lTapeWidth.Location = new System.Drawing.Point(260, 78);
            this.lTapeWidth.Name = "lTapeWidth";
            this.lTapeWidth.Size = new System.Drawing.Size(70, 15);
            this.lTapeWidth.Text = "Tape width:";

            //
            // nudTapeWidth
            //
            this.nudTapeWidth.DecimalPlaces = 3;
            this.nudTapeWidth.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudTapeWidth.Location = new System.Drawing.Point(340, 75);
            this.nudTapeWidth.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            this.nudTapeWidth.Minimum = new decimal(new int[] { 1, 0, 0, 196608 });
            this.nudTapeWidth.Name = "nudTapeWidth";
            this.nudTapeWidth.Size = new System.Drawing.Size(90, 23);
            this.nudTapeWidth.TabIndex = 6;
            this.nudTapeWidth.Value = new decimal(new int[] { 254, 0, 0, 131072 });
            this.nudTapeWidth.ValueChanged += new System.EventHandler(this.nudTapeWidth_ValueChanged);

            //
            // tcPlies
            //
            this.tcPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcPlies.Controls.Add(this.tpPlies);
            this.tcPlies.Controls.Add(this.tpOffset);
            this.tcPlies.Controls.Add(this.tpDisplay);
            this.tcPlies.Location = new System.Drawing.Point(12, 474);
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
            this.dgvPlies.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvPlies_CellMouseDown);
            this.dgvPlies.SelectionChanged += new System.EventHandler(this.dgvPlies_SelectionChanged);
            this.dgvPlies.ColumnHeaderMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvPlies_ColumnHeaderMouseDoubleClick);
            //
            // cmsRegionColumn
            //
            this.cmsRegionColumn = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiImportRegionFromSurface = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiImportRegionFromSurface.Text = "Import region from geometrical surface...";
            this.tsmiImportRegionFromSurface.Click += new System.EventHandler(this.tsmiImportRegionFromSurface_Click);
            this.cmsRegionColumn.Items.Add(this.tsmiImportRegionFromSurface);

            //
            // cmsOrientationColumn
            //
            this.cmsOrientationColumn = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiAssignRosette = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiAssignRosette.Text = "Assign orientations from rosette...";
            this.tsmiAssignRosette.Click += new System.EventHandler(this.tsmiAssignRosette_Click);
            this.tsmiResetToLayupRosette = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiResetToLayupRosette.Text = "Reset to layup rosette";
            this.tsmiResetToLayupRosette.Click += new System.EventHandler(this.tsmiResetToLayupRosette_Click);
            this.tsmiClearRosette = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiClearRosette.Text = "Clear rosette (use angle)";
            this.tsmiClearRosette.Click += new System.EventHandler(this.tsmiClearRosette_Click);
            this.cmsOrientationColumn.Items.Add(this.tsmiAssignRosette);
            this.cmsOrientationColumn.Items.Add(this.tsmiResetToLayupRosette);
            this.cmsOrientationColumn.Items.Add(this.tsmiClearRosette);

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
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationAll);
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationLayup);
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationXY);
            this.gbOrientationDisplay.Controls.Add(this.rbOrientationNone);
            this.gbOrientationDisplay.Controls.Add(this.lArrowDensity);
            this.gbOrientationDisplay.Controls.Add(this.nudArrowDensity);
            this.gbOrientationDisplay.Location = new System.Drawing.Point(220, 8);
            this.gbOrientationDisplay.Name = "gbOrientationDisplay";
            this.gbOrientationDisplay.Size = new System.Drawing.Size(200, 142);
            this.gbOrientationDisplay.TabIndex = 1;
            this.gbOrientationDisplay.TabStop = false;
            this.gbOrientationDisplay.Text = "Orientation display";

            //
            // rbOrientationAll
            //
            this.rbOrientationAll.AutoSize = true;
            this.rbOrientationAll.Location = new System.Drawing.Point(10, 22);
            this.rbOrientationAll.Name = "rbOrientationAll";
            this.rbOrientationAll.Size = new System.Drawing.Size(33, 19);
            this.rbOrientationAll.TabIndex = 0;
            this.rbOrientationAll.Text = "All";
            this.rbOrientationAll.UseVisualStyleBackColor = true;
            this.rbOrientationAll.CheckedChanged += new System.EventHandler(this.rbOrientationAll_CheckedChanged);

            //
            // rbOrientationLayup
            //
            this.rbOrientationLayup.AutoSize = true;
            this.rbOrientationLayup.Checked = true;
            this.rbOrientationLayup.Location = new System.Drawing.Point(10, 44);
            this.rbOrientationLayup.Name = "rbOrientationLayup";
            this.rbOrientationLayup.Size = new System.Drawing.Size(130, 19);
            this.rbOrientationLayup.TabIndex = 1;
            this.rbOrientationLayup.TabStop = true;
            this.rbOrientationLayup.Text = "Ply fiber direction";
            this.rbOrientationLayup.UseVisualStyleBackColor = true;
            this.rbOrientationLayup.CheckedChanged += new System.EventHandler(this.rbOrientationLayup_CheckedChanged);

            //
            // rbOrientationXY
            //
            this.rbOrientationXY.AutoSize = true;
            this.rbOrientationXY.Location = new System.Drawing.Point(10, 66);
            this.rbOrientationXY.Name = "rbOrientationXY";
            this.rbOrientationXY.Size = new System.Drawing.Size(130, 19);
            this.rbOrientationXY.TabIndex = 2;
            this.rbOrientationXY.Text = "Coordinate system";
            this.rbOrientationXY.UseVisualStyleBackColor = true;
            this.rbOrientationXY.CheckedChanged += new System.EventHandler(this.rbOrientationXY_CheckedChanged);

            //
            // rbOrientationNone
            //
            this.rbOrientationNone.AutoSize = true;
            this.rbOrientationNone.Location = new System.Drawing.Point(10, 88);
            this.rbOrientationNone.Name = "rbOrientationNone";
            this.rbOrientationNone.Size = new System.Drawing.Size(50, 19);
            this.rbOrientationNone.TabIndex = 3;
            this.rbOrientationNone.Text = "None";
            this.rbOrientationNone.UseVisualStyleBackColor = true;

            //
            // lArrowDensity
            //
            this.lArrowDensity.AutoSize = true;
            this.lArrowDensity.Location = new System.Drawing.Point(10, 116);
            this.lArrowDensity.Name = "lArrowDensity";
            this.lArrowDensity.TabIndex = 4;
            this.lArrowDensity.Text = "Arrows density";

            //
            // nudArrowDensity
            //
            this.nudArrowDensity.DecimalPlaces = 2;
            this.nudArrowDensity.Increment = new decimal(new int[] { 5, 0, 0, 131072 }); // 0.05
            this.nudArrowDensity.Location = new System.Drawing.Point(120, 113);
            this.nudArrowDensity.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudArrowDensity.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudArrowDensity.Name = "nudArrowDensity";
            this.nudArrowDensity.Size = new System.Drawing.Size(65, 23);
            this.nudArrowDensity.TabIndex = 4;
            this.nudArrowDensity.Value = new decimal(new int[] { 1, 0, 0, 131072 }); // 0.01
            this.nudArrowDensity.ValueChanged += new System.EventHandler(this.nudArrowDensity_ValueChanged);

            //
            // btnOkAddNew
            //
            this.btnOkAddNew.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOkAddNew.Location = new System.Drawing.Point(335, 738);
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
            this.btnOK.Location = new System.Drawing.Point(416, 738);
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
            this.btnCancel.Location = new System.Drawing.Point(497, 738);
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
            this.ClientSize = new System.Drawing.Size(584, 773);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.gbLayupOrientation);
            this.Controls.Add(this.cbSymmetric);
            this.Controls.Add(this.gbDraping);
            this.Controls.Add(this.gbPropagation);
            this.Controls.Add(this.tcPlies);
            this.Controls.Add(this.btnOkAddNew);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(500, 694);
            this.Name = "FrmCompositeLayup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Create Composite Layup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCompositeLayup_FormClosing);

            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.gbLayupOrientation.ResumeLayout(false);
            this.gbLayupOrientation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAdditionalRotationAngle)).EndInit();
            this.gbDraping.ResumeLayout(false);
            this.gbDraping.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLimitShear)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCellSize)).EndInit();
            this.gbPropagation.ResumeLayout(false);
            this.gbPropagation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTapeWidth)).EndInit();
            this.tcPlies.ResumeLayout(false);
            this.tpPlies.ResumeLayout(false);
            this.gbShellOffset.ResumeLayout(false);
            this.gbShellOffset.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffsetRatio)).EndInit();
            this.tpOffset.ResumeLayout(false);
            this.gbHighlightPlyRegions.ResumeLayout(false);
            this.gbHighlightPlyRegions.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudArrowDensity)).EndInit();
            this.gbOrientationDisplay.ResumeLayout(false);
            this.gbOrientationDisplay.PerformLayout();
            this.tpDisplay.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).EndInit();
            this.ResumeLayout(false);
        }


        // Event handlers
        private void cbDefinition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbDefinition.SelectedIndex >= 0 &&
                cbDefinition.SelectedIndex < _definitionItems.Count)
            {
                _compositeLayup.OrientationDefinition = _definitionItems[cbDefinition.SelectedIndex];
                _propertyItemChanged = true;
            }
            ApplyRosetteOrientationDefaults();
            UpdateOrientationControlsVisibility();
            RefreshPliesGrid();
        }

        private OrientationDefinitionType GetSelectedDefinition()
        {
            if (cbDefinition.SelectedIndex < 0 || cbDefinition.SelectedIndex >= _definitionItems.Count)
                return OrientationDefinitionType.PartGlobal;
            return _definitionItems[cbDefinition.SelectedIndex];
        }

        private void SetSelectedDefinition(OrientationDefinitionType type)
        {
            int index = _definitionItems.IndexOf(type);
            if (index < 0) index = _definitionItems.IndexOf(OrientationDefinitionType.PartGlobal);
            if (index >= 0) cbDefinition.SelectedIndex = index;
        }

        private void cbLayupRosette_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup == null) return;
            string selected = cbLayupRosette.SelectedItem as string;
            // The placeholder of a deleted rosette must not be written back to the model
            if (selected != null && selected.EndsWith(" (missing)")) return;
            if (selected == _compositeLayup.OrientationRosette) return;
            //
            _compositeLayup.OrientationRosette = selected;
            _propertyItemChanged = true;
            RefreshPliesGrid();
            RefreshOrientationArrows();
        }

        // The normal axis and the additional rotation are hidden for a rosette layup, so they are
        // reset to the values the writer treats as neutral - a hidden control must never keep
        // acting on the exported orientation
        private void ApplyRosetteOrientationDefaults()
        {
            if (_compositeLayup == null) return;
            if (_compositeLayup.OrientationDefinition != OrientationDefinitionType.Rosette) return;
            //
            _compositeLayup.NormalDirection = NormalDirectionType.Axis3;
            _compositeLayup.AdditionalRotationType = AdditionalRotationType.None;
            _compositeLayup.AdditionalRotationAngle = 0.0;
            //
            cbNormalDirection.SelectedIndex = (int)NormalDirectionType.Axis3;
            cbAdditionalRotation.SelectedIndex = (int)AdditionalRotationType.None;
            nudAdditionalRotationAngle.Value = 0;
        }

        private void FillLayupRosetteComboBox()
        {
            cbLayupRosette.Items.Clear();
            foreach (string name in _controller.GetRosetteNames()) cbLayupRosette.Items.Add(name);
            //
            if (_compositeLayup == null) return;
            string current = _compositeLayup.OrientationRosette;
            if (!string.IsNullOrEmpty(current))
            {
                int index = cbLayupRosette.Items.IndexOf(current);
                if (index >= 0) cbLayupRosette.SelectedIndex = index;
                else
                {
                    // The rosette was deleted outside of this dialog
                    cbLayupRosette.Items.Insert(0, current + " (missing)");
                    cbLayupRosette.SelectedIndex = 0;
                }
            }
            else if (cbLayupRosette.Items.Count > 0 && _compositeLayup.RosetteEnabled)
            {
                cbLayupRosette.SelectedIndex = 0;
            }
        }

        private void cbProcessType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbProcessType.SelectedIndex >= 0 &&
                cbProcessType.SelectedIndex < _processTypeItems.Count)
            {
                _compositeLayup.ProcessType = _processTypeItems[cbProcessType.SelectedIndex];
                _propertyItemChanged = true;
            }
            UpdatePropagationControlsVisibility();
        }

        // None is listed first but is the last value of the enum, so the combo index is mapped
        // instead of cast
        private void SetSelectedProcessType(ProcessType type)
        {
            int index = _processTypeItems.IndexOf(type);
            if (index < 0) index = _processTypeItems.IndexOf(ProcessType.None);
            if (index >= 0) cbProcessType.SelectedIndex = index;
        }

        private void AddProcessTypeItem(ProcessType type, string displayName)
        {
            cbProcessType.Items.Add(displayName);
            _processTypeItems.Add(type);
        }

        private void cbNormalDirection_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbNormalDirection.SelectedIndex >= 0)
            {
                _compositeLayup.NormalDirection = (NormalDirectionType)cbNormalDirection.SelectedIndex;
                _propertyItemChanged = true;
            }
        }

        private void cbAdditionalRotation_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null && cbAdditionalRotation.SelectedIndex >= 0)
            {
                _compositeLayup.AdditionalRotationType = (AdditionalRotationType)cbAdditionalRotation.SelectedIndex;
                nudAdditionalRotationAngle.Enabled = _compositeLayup.AdditionalRotationType == AdditionalRotationType.Angle;
                _propertyItemChanged = true;
                RefreshOrientationArrows();
            }
        }

        private void nudAdditionalRotationAngle_ValueChanged(object sender, EventArgs e)
        {
            if (_compositeLayup != null)
            {
                _compositeLayup.AdditionalRotationAngle = (double)nudAdditionalRotationAngle.Value;
                _propertyItemChanged = true;
                RefreshOrientationArrows();
            }
        }

        private void UpdateOrientationControlsVisibility()
        {
            OrientationDefinitionType definition = GetSelectedDefinition();
            bool isCsys = definition == OrientationDefinitionType.CoordinateSystem;
            bool isDiscrete = definition == OrientationDefinitionType.DiscreteOrientations;
            bool isRosette = definition == OrientationDefinitionType.Rosette;
            // A rosette carries its own frame: the reference direction comes from the rosette and
            // the ply angle is applied about the element normal, so the global frame controls do
            // not apply and the rosette itself is defined from the Rosettes tree node, not here
            bool frameControls = isCsys;
            lNormalDirection.Visible = frameControls;
            cbNormalDirection.Visible = frameControls;
            lAdditionalRotation.Visible = frameControls;
            cbAdditionalRotation.Visible = frameControls;
            nudAdditionalRotationAngle.Visible = frameControls;
            lLayupRosette.Visible = isRosette;
            cbLayupRosette.Visible = isRosette;
            if (isRosette) FillLayupRosetteComboBox();
            lDiscreteOrientationsFile.Visible = isDiscrete;
            tbDiscreteOrientationsFile.Visible = isDiscrete;
            btnBrowseDiscreteOrientations.Visible = isDiscrete;
            btnClearDiscreteOrientations.Visible = isDiscrete;
            lDiscreteSolidHint.Visible = isDiscrete &&
                _compositeLayup != null && _compositeLayup.ElementType == CompositeElementType.Solid;
            if (isDiscrete)
                tbDiscreteOrientationsFile.Text = _compositeLayup?.DiscreteOrientationsData?.XmlFilePath ?? "";
        }

        private void nudArrowDensity_ValueChanged(object sender, EventArgs e)
        {
            RefreshOrientationArrows();
        }

        private void RefreshOrientationArrows()
        {
            if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                int plyIndex = dgvPlies.SelectedRows[0].Index;
                double density = (double)nudArrowDensity.Value;
                if (rbOrientationAll.Checked)
                    _controller.DrawAllOrientationArrows(_compositeLayup, plyIndex, density);
                else if (rbOrientationLayup.Checked)
                    _controller.DrawPlyOrientationArrows(_compositeLayup, plyIndex, density);
                else if (rbOrientationXY.Checked)
                    _controller.DrawLayupCoordinateArrows(_compositeLayup, plyIndex, density);
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
            UpdateDrapingControlsVisibility();
            if (!enabled)
            {
                cbMapFiberFromDraping.Checked = false;
                CancelDraping();
            }
            else
            {
                if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
                {
                    int plyIndex = dgvPlies.SelectedRows[0].Index;
                    // Auto-detect cellSize from ply region bounding box - it stays the draping
                    // resolution even when the material width hides the input
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

        // The cell size is taken from the ply material width when it is defined, so the input is hidden
        private void UpdateDrapingControlsVisibility()
        {
            bool enabled = cbDrapingSimulation.Checked;
            bool cellSizeFromMaterial = GetSelectedPlyMaterialWidth().HasValue;
            lLimitShear.Visible = enabled;
            nudLimitShear.Visible = enabled;
            lCellSize.Visible = enabled && !cellSizeFromMaterial;
            nudCellSize.Visible = enabled && !cellSizeFromMaterial;
            cbMapFiberFromDraping.Visible = enabled;
        }

        private double? GetSelectedPlyMaterialWidth()
        {
            if (_compositeLayup == null || dgvPlies.SelectedRows.Count == 0) return null;
            return _controller.GetPlyMaterialWidth(_compositeLayup, dgvPlies.SelectedRows[0].Index);
        }

        private void cbShowPropagation_CheckedChanged(object sender, EventArgs e)
        {
            lPropagationStatus.Visible = cbShowPropagation.Checked;
            if (!cbShowPropagation.Checked)
            {
                CancelPropagation();
            }
            else if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                TriggerPropagationAsync(dgvPlies.SelectedRows[0].Index);
            }
        }

        // Shows/hides the whole "Tape Propagation" section based on the layup's process type
        private void UpdatePropagationControlsVisibility()
        {
            bool isTapePlacement = _compositeLayup != null && _compositeLayup.ProcessType == ProcessType.TapePlacement;
            gbPropagation.Visible = isTapePlacement;
            if (!isTapePlacement) CancelPropagation();
        }

        private void chkPickSeedPoint_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPickSeedPoint.Checked)
            {
                chkPickSeedPoint.BackColor = Color.LightGreen;
                if (_controller != null)
                {
                    _controller.SelectBy = vtkSelectBy.Node;
                    _controller.Selection.SelectItem = vtkSelectItem.Node;
                }
            }
            else
            {
                chkPickSeedPoint.BackColor = SystemColors.Control;
                if (_controller != null) _controller.SetSelectByToOff();
            }
        }

        // Called from FrmMain when a node is picked in the 3D view while chkPickSeedPoint is active
        public void PickedIds(int[] ids)
        {
            if (!chkPickSeedPoint.Checked) return;
            if (ids == null || ids.Length == 0) return;
            if (_compositeLayup == null || dgvPlies.SelectedRows.Count == 0) return;
            try
            {
                double[] coor = _controller.GetNode(ids[0]).Coor;
                tbSeedX.Text = coor[0].ToString("G6", CultureInfo.InvariantCulture);
                tbSeedY.Text = coor[1].ToString("G6", CultureInfo.InvariantCulture);
                tbSeedZ.Text = coor[2].ToString("G6", CultureInfo.InvariantCulture);
                _controller.ClearSelectionHistoryAndCallSelectionChanged();
                ApplySeedPointFromTextBoxes();
            }
            catch { }
        }

        private void tbSeed_Leave(object sender, EventArgs e)
        {
            ApplySeedPointFromTextBoxes();
        }

        private void ApplySeedPointFromTextBoxes()
        {
            if (_compositeLayup == null || dgvPlies.SelectedRows.Count == 0) return;
            double x, y, z;
            if (!double.TryParse(tbSeedX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out x)) return;
            if (!double.TryParse(tbSeedY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out y)) return;
            if (!double.TryParse(tbSeedZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out z)) return;
            int plyIndex = dgvPlies.SelectedRows[0].Index;
            _compositeLayup.Plies[plyIndex].PropagationSeedPoint = new double[] { x, y, z };
            _propertyItemChanged = true;
            if (cbShowPropagation.Checked) TriggerPropagationAsync(plyIndex);
        }

        private void cbPropagationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingPropagationControls) return;
            if (_compositeLayup != null && dgvPlies.SelectedRows.Count > 0 && cbPropagationType.SelectedIndex >= 0)
            {
                int plyIndex = dgvPlies.SelectedRows[0].Index;
                _compositeLayup.Plies[plyIndex].PropagationType = (PropagationPathType)cbPropagationType.SelectedIndex;
                _propertyItemChanged = true;
                if (cbShowPropagation.Checked) TriggerPropagationAsync(plyIndex);
            }
        }

        private void nudTapeWidth_ValueChanged(object sender, EventArgs e)
        {
            if (_loadingPropagationControls) return;
            if (_compositeLayup != null && dgvPlies.SelectedRows.Count > 0)
            {
                int plyIndex = dgvPlies.SelectedRows[0].Index;
                _compositeLayup.Plies[plyIndex].TapeWidth = (double)nudTapeWidth.Value;
                _propertyItemChanged = true;
                if (cbShowPropagation.Checked) TriggerPropagationAsync(plyIndex);
            }
        }

        private void nudLimitShear_ValueChanged(object sender, EventArgs e)
        {
            RefreshDraping();
        }

        private void cbMapFiberFromDraping_CheckedChanged(object sender, EventArgs e)
        {
            if (_compositeLayup == null) return;
            _compositeLayup.UseDrapingForFiberDirection = cbMapFiberFromDraping.Checked;
            if (cbMapFiberFromDraping.Checked)
            {
                _compositeLayup.DrapingCellSize = (double)nudCellSize.Value;
                _compositeLayup.DrapingPlyIndex = dgvPlies.SelectedRows.Count > 0 ? dgvPlies.SelectedRows[0].Index : 0;
            }
            _propertyItemChanged = true;
        }

        private void btnBrowseDiscreteOrientations_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null) return;
            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Title = "Select Discrete Orientations XML File";
                dlg.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    try
                    {
                        DiscreteOrientationsData data = DiscreteOrientationsXmlReader.Read(dlg.FileName);
                        _compositeLayup.DiscreteOrientationsData = data;
                        tbDiscreteOrientationsFile.Text = dlg.FileName;
                        _propertyItemChanged = true;
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show(ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnClearDiscreteOrientations_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null) return;
            _compositeLayup.DiscreteOrientationsData = null;
            tbDiscreteOrientationsFile.Text = "";
            SetSelectedDefinition(OrientationDefinitionType.PartGlobal);
            _propertyItemChanged = true;
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
                // The fiber angle is drawn in the 3D view, so the arrows follow what is typed
                if (e.ColumnIndex >= 0 && dgvPlies.Columns[e.ColumnIndex].Name == "colOrientation")
                    RefreshOrientationArrows();
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
                    // Check if composite materials exist
                    if (!CheckCompositeMaterialsExist()) return;

                    _editingMaterialRowIndex = e.RowIndex;
                    string currentMaterial = _compositeLayup.Plies[e.RowIndex].Material;
                    _controller.Form.ShowSelectMaterial(currentMaterial, OnMaterialSelected, true);
                }
            }
        }

        // Ply region mapping from geometrical surface: right-click on the Region column shows a
        // context menu offering to import the region from a named CAD surface
        private void dgvPlies_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0 || _compositeLayup == null) return;
            string columnName = dgvPlies.Columns[e.ColumnIndex].Name;
            //
            if (columnName == "colRegion")
            {
                dgvPlies.CurrentCell = dgvPlies.Rows[e.RowIndex].Cells[e.ColumnIndex];
                _contextMenuRegionRowIndex = e.RowIndex;
                cmsRegionColumn.Show(dgvPlies, new Point(e.X, e.Y));
            }
            // The rosette of a ply is assigned from either of the two columns that show it
            else if (columnName == "colRosette" || columnName == "colOrientation")
            {
                dgvPlies.CurrentCell = dgvPlies.Rows[e.RowIndex].Cells[e.ColumnIndex];
                _contextMenuOrientationRowIndex = e.RowIndex;
                //
                bool hasOwnRosette = !string.IsNullOrEmpty(_compositeLayup.Plies[e.RowIndex].OrientationRosette);
                tsmiResetToLayupRosette.Visible = hasOwnRosette && _compositeLayup.RosetteEnabled &&
                                                  !string.IsNullOrEmpty(_compositeLayup.OrientationRosette);
                tsmiClearRosette.Visible = hasOwnRosette;
                // Discrete orientations and draping override the rosettes at export time
                bool overridden = _compositeLayup.DiscreteOrientationsEnabled ||
                                  _compositeLayup.UseDrapingForFiberDirection;
                tsmiAssignRosette.Enabled = !overridden;
                tsmiAssignRosette.ToolTipText = overridden
                    ? "Discrete orientations take precedence over the rosette definition."
                    : "";
                cmsOrientationColumn.Show(dgvPlies, new Point(e.X, e.Y));
            }
        }

        private void tsmiAssignRosette_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null || _contextMenuOrientationRowIndex < 0) return;
            int plyIndex = _contextMenuOrientationRowIndex;
            //
            string[] rosetteNames = _controller.GetRosetteNames();
            if (rosetteNames.Length == 0)
            {
                MessageBox.Show("No rosettes are defined in the model. Create a rosette first " +
                                "(Model tree: Rosettes -> Create).",
                                "No Rosettes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //
            Ply ply = _compositeLayup.Plies[plyIndex];
            string current = _compositeLayup.GetEffectivePlyRosette(plyIndex);
            using (FrmAssignRosetteToPlies frm =
                       new FrmAssignRosetteToPlies(rosetteNames, current, ply.RotationAngle))
            {
                if (frm.ShowDialog(this) != DialogResult.OK || frm.SelectedRosetteName == null) return;
                //
                if (frm.ApplyToAllPlies)
                {
                    foreach (Ply p in _compositeLayup.Plies)
                    {
                        p.OrientationRosette = frm.SelectedRosetteName;
                        p.RotationAngle = frm.RotationAngle;
                    }
                }
                else
                {
                    ply.OrientationRosette = frm.SelectedRosetteName;
                    ply.RotationAngle = frm.RotationAngle;
                }
                _propertyItemChanged = true;
                RefreshPliesGrid();
                RefreshOrientationArrows();
            }
        }

        private void tsmiResetToLayupRosette_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null || _contextMenuOrientationRowIndex < 0) return;
            _compositeLayup.Plies[_contextMenuOrientationRowIndex].OrientationRosette = null;
            _propertyItemChanged = true;
            RefreshPliesGrid();
            RefreshOrientationArrows();
        }

        private void tsmiClearRosette_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null || _contextMenuOrientationRowIndex < 0) return;
            _compositeLayup.Plies[_contextMenuOrientationRowIndex].OrientationRosette = null;
            _propertyItemChanged = true;
            RefreshPliesGrid();
            RefreshOrientationArrows();
            //
            if (_compositeLayup.RosetteEnabled && !string.IsNullOrEmpty(_compositeLayup.OrientationRosette))
            {
                MessageBox.Show("The ply now uses the layup rosette '" + _compositeLayup.OrientationRosette +
                                "'.\nChange the orientation definition of the layup to stop using rosettes.",
                                "Clear rosette", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void tsmiImportRegionFromSurface_Click(object sender, EventArgs e)
        {
            if (_compositeLayup == null || _contextMenuRegionRowIndex < 0) return;
            int plyIndex = _contextMenuRegionRowIndex;
            //
            List<GeometrySurfaceOption> options = _controller.GetGeometrySurfaceOptions();
            if (options.Count == 0)
            {
                MessageBox.Show("No geometrical surfaces are available. Import a CAD part first.",
                                "No Geometrical Surfaces", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            //
            using (FrmImportPlyRegionFromSurface frm =
                       new FrmImportPlyRegionFromSurface(_controller, options))
            {
                if (frm.ShowDialog(this) == DialogResult.OK && frm.SelectedPartName != null)
                {
                    string message;
                    bool success = _controller.ImportPlyRegionFromGeometrySurface(_compositeLayup, plyIndex,
                                                                                   frm.SelectedPartName, frm.SelectedFaceId,
                                                                                   frm.CoreSamplingDepth,
                                                                                   frm.TargetPartNames, out message);
                    if (success)
                    {
                        dgvPlies.Rows[plyIndex].Cells["colRegion"].Value = _compositeLayup.Plies[plyIndex].Region;
                        _propertyItemChanged = true;
                        RefreshDraping();
                        RefreshPropagation();
                        // The dialog cleared its element preview on closing, which also removed the
                        // ply region highlight
                        if (rbHighlightOn.Checked) HighlightSelectedPlyRegion(plyIndex);
                        MessageBox.Show(message, "Region Imported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(message, "Import Region from Geometrical Surface",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private void OnElementSetSelected(string elementSetName)
        {
            if (_editingRegionRowIndex >= 0 && _editingRegionRowIndex < _compositeLayup.Plies.Count)
            {
                bool isSolid = _compositeLayup.ElementType == CompositeElementType.Solid;
                bool valid = isSolid
                    ? _controller.IsElementSetValidForSolid(elementSetName)
                    : _controller.IsElementSetValidForComposite(elementSetName);
                if (!valid)
                {
                    string msg = isSolid
                        ? "The selected element set contains unsupported element types.\n" +
                          "Only solid elements (C3D8, C3D8R, C3D8I, C3D6, C3D6R, etc.) are supported."
                        : "The selected element set contains unsupported element types.\n" +
                          "Only S8R (parabolic quadrilateral, reduced integration) and\n" +
                          "S6 (parabolic triangle, full integration) elements are supported.";
                    MessageBox.Show(msg, "Unsupported Element Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _editingRegionRowIndex = -1;
                    return;
                }
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
            RefreshDraping();
        }

        private void dgvPlies_ColumnHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (_compositeLayup == null || _compositeLayup.Plies.Count == 0) return;
            if (e.ColumnIndex < 0) return;
            string columnName = dgvPlies.Columns[e.ColumnIndex].Name;
            if (columnName == "colRegion")
            {
                string[] elementSetNames = _controller.GetUserElementSetNames();
                if (elementSetNames == null || elementSetNames.Length == 0)
                {
                    MessageBox.Show("No element sets are defined in the model. Please create an element set first.",
                                    "No Element Sets", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string currentRegion = _compositeLayup.Plies[0].Region;
                _controller.Form.ShowSelectElementSet(currentRegion, OnElementSetSelectedForAll);
            }
            else if (columnName == "colMaterial")
            {
                if (!CheckCompositeMaterialsExist()) return;
                string currentMaterial = _compositeLayup.Plies[0].Material;
                _controller.Form.ShowSelectMaterial(currentMaterial, OnMaterialSelectedForAll, true);
            }
        }

        private bool CheckCompositeMaterialsExist()
        {
            Material[] materials = _controller.GetAllMaterials();
            if (materials == null || materials.Length == 0)
            {
                MessageBox.Show("No materials are defined in the model. Please create a material first.",
                                "No Materials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!materials.Any(m => m.CompositeMaterialType != CompositeMaterialType.None))
            {
                MessageBox.Show("No composite materials are defined in the model.\n" +
                                "Set the composites material type of a material to Unidirectional, Biaxial, Triaxial or NCF first.",
                                "No Composite Materials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private void OnElementSetSelectedForAll(string elementSetName)
        {
            bool isSolid = _compositeLayup.ElementType == CompositeElementType.Solid;
            bool valid = isSolid
                ? _controller.IsElementSetValidForSolid(elementSetName)
                : _controller.IsElementSetValidForComposite(elementSetName);
            if (!valid)
            {
                string msg = isSolid
                    ? "The selected element set contains unsupported element types.\n" +
                      "Only solid elements (C3D8, C3D8R, C3D8I, C3D6, C3D6R, etc.) are supported."
                    : "The selected element set contains unsupported element types.\n" +
                      "Only S8R (parabolic quadrilateral, reduced integration) and\n" +
                      "S6 (parabolic triangle, full integration) elements are supported.";
                MessageBox.Show(msg, "Unsupported Element Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            for (int i = 0; i < _compositeLayup.Plies.Count; i++)
            {
                _compositeLayup.Plies[i].Region = elementSetName;
                dgvPlies.Rows[i].Cells["colRegion"].Value = elementSetName;
            }
            _propertyItemChanged = true;
        }

        private void OnMaterialSelectedForAll(string materialName)
        {
            for (int i = 0; i < _compositeLayup.Plies.Count; i++)
            {
                _compositeLayup.Plies[i].Material = materialName;
                dgvPlies.Rows[i].Cells["colMaterial"].Value = materialName;
            }
            _propertyItemChanged = true;
            RefreshDraping();
            RefreshPropagation();
        }
        // The draping display depends on the ply material and on the limit shear
        private void RefreshDraping()
        {
            UpdateDrapingControlsVisibility();
            if (cbDrapingSimulation.Checked && dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
                TriggerDrapingAsync(dgvPlies.SelectedRows[0].Index);
        }
        // The default tape width (when not explicitly overridden) is taken from the ply material
        private void RefreshPropagation()
        {
            if (dgvPlies.SelectedRows.Count == 0 || _compositeLayup == null) return;
            int plyIndex = dgvPlies.SelectedRows[0].Index;
            LoadPropagationControlsFromPly(plyIndex);
            if (cbShowPropagation.Checked) TriggerPropagationAsync(plyIndex);
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
                double density = (double)nudArrowDensity.Value;
                if (rbOrientationAll.Checked)
                    _controller.DrawAllOrientationArrows(_compositeLayup, plyIndex, density);
                else if (rbOrientationLayup.Checked)
                    _controller.DrawPlyOrientationArrows(_compositeLayup, plyIndex, density);
                else if (rbOrientationXY.Checked)
                    _controller.DrawLayupCoordinateArrows(_compositeLayup, plyIndex, density);
                else
                    _controller.ClearPlyOrientationArrows();
                if (rbHighlightOn.Checked)
                    HighlightSelectedPlyRegion(plyIndex);
                else
                    _controller.Form.Clear3DSelection();
                UpdateDrapingControlsVisibility();
                if (cbDrapingSimulation.Checked)
                    TriggerDrapingAsync(plyIndex);
                else
                    CancelDraping();
                //
                LoadPropagationControlsFromPly(plyIndex);
                if (cbShowPropagation.Checked)
                    TriggerPropagationAsync(plyIndex);
                else
                    CancelPropagation();
            }
            else
            {
                _controller.ClearPlyOrientationArrows();
                _controller.Form.Clear3DSelection();
                CancelDraping();
                CancelPropagation();
            }
        }

        // Populates the seed point / propagation type / tape width controls from the selected ply's
        // stored values, without re-triggering their own change handlers (guarded by
        // _loadingPropagationControls, since assigning nudTapeWidth.Value would otherwise overwrite a
        // ply.TapeWidth == null "use material width" state with an explicit override)
        private void LoadPropagationControlsFromPly(int plyIndex)
        {
            if (_compositeLayup == null || plyIndex < 0 || plyIndex >= _compositeLayup.Plies.Count) return;
            Ply ply = _compositeLayup.Plies[plyIndex];
            _loadingPropagationControls = true;
            try
            {
                double[] seed = ply.PropagationSeedPoint;
                tbSeedX.Text = (seed != null ? seed[0] : 0.0).ToString("G6", CultureInfo.InvariantCulture);
                tbSeedY.Text = (seed != null ? seed[1] : 0.0).ToString("G6", CultureInfo.InvariantCulture);
                tbSeedZ.Text = (seed != null ? seed[2] : 0.0).ToString("G6", CultureInfo.InvariantCulture);
                cbPropagationType.SelectedIndex = (int)ply.PropagationType;
                double? materialWidth = _controller.GetPlyMaterialWidth(_compositeLayup, plyIndex);
                double width = ply.TapeWidth ?? materialWidth ?? (double)nudTapeWidth.Value;
                decimal clamped = Math.Max(nudTapeWidth.Minimum, Math.Min(nudTapeWidth.Maximum, (decimal)width));
                nudTapeWidth.Value = clamped;
            }
            finally
            {
                _loadingPropagationControls = false;
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

        private void rbOrientationAll_CheckedChanged(object sender, EventArgs e)
        {
            if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                if (rbOrientationAll.Checked)
                    _controller.DrawAllOrientationArrows(_compositeLayup, dgvPlies.SelectedRows[0].Index, (double)nudArrowDensity.Value);
                else
                    _controller.ClearPlyOrientationArrows();
            }
            else if (!rbOrientationAll.Checked)
            {
                _controller.ClearPlyOrientationArrows();
            }
        }

        private void rbOrientationLayup_CheckedChanged(object sender, EventArgs e)
        {
            if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                if (rbOrientationLayup.Checked)
                    _controller.DrawPlyOrientationArrows(_compositeLayup, dgvPlies.SelectedRows[0].Index, (double)nudArrowDensity.Value);
                else
                    _controller.ClearPlyOrientationArrows();
            }
            else if (!rbOrientationLayup.Checked)
            {
                _controller.ClearPlyOrientationArrows();
            }
        }

        private void rbOrientationXY_CheckedChanged(object sender, EventArgs e)
        {
            if (dgvPlies.SelectedRows.Count > 0 && _compositeLayup != null)
            {
                if (rbOrientationXY.Checked)
                    _controller.DrawLayupCoordinateArrows(_compositeLayup, dgvPlies.SelectedRows[0].Index, (double)nudArrowDensity.Value);
                else
                    _controller.ClearPlyOrientationArrows();
            }
            else if (!rbOrientationXY.Checked)
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
                CancelPropagation();
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
            CancelPropagation();
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
                CancelPropagation();
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

            // Edit existing composite layup; the layup must be loaded before the combo boxes are
            // filled because the Definition combo depends on the orientation definition in use
            if (_compositeLayupToEditName != null)
            {
                CompositeLayup = _controller.GetCompositeLayup(_compositeLayupToEditName);
            }

            // Initialize combo boxes
            InitializeComboBoxes();

            this.Text = "Edit Composite Layup";

            // The columns must exist before the form is filled: setting a combo box index fires its
            // SelectedIndexChanged handler, and some of those handlers refresh the plies grid
            SetupPliesGrid();

            // Update UI
            UpdateFormFromLayup();

            // Refresh plies grid
            RefreshPliesGrid();

            CheckPlyRegionsElementTypes();

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

            // The columns must exist before the form is filled: setting a combo box index fires its
            // SelectedIndexChanged handler, and some of those handlers refresh the plies grid
            SetupPliesGrid();

            // Update UI
            UpdateFormFromLayup();

            // Refresh plies grid
            RefreshPliesGrid();

            CheckPlyRegionsElementTypes();

            _controller.SetSelectByToOff();

            return true;
        }

        private void CheckPlyRegionsElementTypes()
        {
            if (_compositeLayup == null) return;
            bool isSolid = _compositeLayup.ElementType == CompositeElementType.Solid;
            foreach (Ply ply in _compositeLayup.Plies)
            {
                if (string.IsNullOrEmpty(ply.Region)) continue;
                bool valid = isSolid
                    ? _controller.IsElementSetValidForSolid(ply.Region)
                    : _controller.IsElementSetValidForComposite(ply.Region);
                if (!valid)
                {
                    string msg = isSolid
                        ? "Ply region '" + ply.Region + "' contains unsupported element types.\n" +
                          "Only 3D solid elements (C3D4, C3D8, C3D10, C3D20, etc.) are supported for solid composites."
                        : "Ply region '" + ply.Region + "' contains unsupported element types.\n" +
                          "Only S8R (parabolic quadrilateral, reduced integration) and\n" +
                          "S6 (parabolic triangle, full integration) elements are supported.";
                    MessageBox.Show(msg, "Unsupported Element Type", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void InitializeComboBoxes()
        {
            // Process type combo box
            cbProcessType.Items.Clear();
            _processTypeItems.Clear();
            AddProcessTypeItem(ProcessType.None, "None");
            AddProcessTypeItem(ProcessType.HandLayup, "Hand layup");
            AddProcessTypeItem(ProcessType.TapePlacement, "Tape placement");
            AddProcessTypeItem(ProcessType.Winding, "Winding");
            AddProcessTypeItem(ProcessType.Braiding, "Braiding");

            // Definition combo box; Rosette is offered only when the model contains rosettes, or
            // when the edited layup already uses that mode
            cbDefinition.Items.Clear();
            _definitionItems.Clear();
            bool rosettesExist = _controller.GetRosetteNames().Length > 0;
            bool layupUsesRosette = _compositeLayup != null && _compositeLayup.RosetteEnabled;
            foreach (var value in Enum.GetValues(typeof(OrientationDefinitionType)))
            {
                OrientationDefinitionType type = (OrientationDefinitionType)value;
                if (type == OrientationDefinitionType.Rosette && !rosettesExist && !layupUsesRosette) continue;
                string displayName = type.ToString();
                if (displayName == "PartGlobal") displayName = "Part global";
                if (displayName == "CoordinateSystem") displayName = "Coordinate system";
                if (displayName == "DiscreteOrientations") displayName = "Discrete orientations";
                cbDefinition.Items.Add(displayName);
                _definitionItems.Add(type);
            }

            // Normal direction combo box
            cbNormalDirection.Items.Clear();
            foreach (var value in Enum.GetValues(typeof(NormalDirectionType)))
            {
                cbNormalDirection.Items.Add(value.ToString());
            }

            // Additional rotation combo box
            cbAdditionalRotation.Items.Clear();
            cbAdditionalRotation.Items.Add("None");
            cbAdditionalRotation.Items.Add("Angle");

            // Propagation type combo box (tape placement); Rosette is not implemented yet
            cbPropagationType.Items.Clear();
            cbPropagationType.Items.Add("Natural (geodesic)");
            cbPropagationType.Items.Add("Rosette (fixed angle) - not implemented");
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

                SetSelectedProcessType(_compositeLayup.ProcessType);
                UpdatePropagationControlsVisibility();

                SetSelectedDefinition(_compositeLayup.OrientationDefinition);
                // Also needed here: SetSelectedDefinition only fires its handler when the index
                // actually changes, which it does not when the same layup is reopened
                ApplyRosetteOrientationDefaults();
                cbNormalDirection.SelectedIndex = (int)_compositeLayup.NormalDirection;
                cbAdditionalRotation.SelectedIndex = (int)_compositeLayup.AdditionalRotationType;
                nudAdditionalRotationAngle.Value = (decimal)Math.Max((double)nudAdditionalRotationAngle.Minimum,
                    Math.Min((double)nudAdditionalRotationAngle.Maximum, _compositeLayup.AdditionalRotationAngle));
                nudAdditionalRotationAngle.Enabled = _compositeLayup.AdditionalRotationType == AdditionalRotationType.Angle;
                UpdateOrientationControlsVisibility();
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
            cbMapFiberFromDraping.Visible = false;
            cbMapFiberFromDraping.Checked = false;
            if (_compositeLayup?.UseDrapingForFiberDirection == true)
            {
                cbDrapingSimulation.Checked = true;  // shows sub-controls + triggers draping
                cbMapFiberFromDraping.Checked = true;
            }
            // Reset propagation preview controls (visibility of the section itself was already set
            // above by UpdatePropagationControlsVisibility, driven by ProcessType)
            cbShowPropagation.Checked = false;
            lPropagationStatus.Visible = false;
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
            colThickness.Visible = true;
            dgvPlies.Columns.Add(colThickness);

            // CSYS column
            DataGridViewTextBoxColumn colCsys = new DataGridViewTextBoxColumn();
            colCsys.Name = "colCsys";
            colCsys.HeaderText = "CSYS";
            colCsys.Width = 60;
            dgvPlies.Columns.Add(colCsys);

            // Rosette column: read only, filled from the ply or from the layup. It is a column of
            // its own so that the Orientation cell never has to hold anything but a number - a cell
            // that means two different things depending on whether it is being edited loses what
            // the user typed on every commit path that skips CellValueChanged/CellEndEdit
            DataGridViewTextBoxColumn colRosette = new DataGridViewTextBoxColumn();
            colRosette.Name = "colRosette";
            colRosette.HeaderText = "Rosette";
            colRosette.Width = 110;
            colRosette.ReadOnly = true;
            dgvPlies.Columns.Add(colRosette);

            // Orientation column: the fiber angle, measured from the rosette reference direction
            // when a rosette applies, from the layup/CSYS reference direction otherwise
            DataGridViewTextBoxColumn colOrientation = new DataGridViewTextBoxColumn();
            colOrientation.Name = "colOrientation";
            colOrientation.HeaderText = "Angle [°]";
            colOrientation.Width = 70;
            dgvPlies.Columns.Add(colOrientation);
        }

        private void RefreshPliesGrid()
        {
            // A row cannot be added to a grid without columns; SetupPliesGrid fills them in and
            // calls this method afterwards
            if (dgvPlies.Columns.Count == 0) return;
            //
            dgvPlies.Rows.Clear();

            if (_compositeLayup != null)
            {
                // The Rosette column is only of interest once rosettes are in play
                dgvPlies.Columns["colRosette"].Visible = UsesAnyRosette();
                //
                for (int i = 0; i < _compositeLayup.Plies.Count; i++)
                {
                    Ply ply = _compositeLayup.Plies[i];
                    if (string.IsNullOrEmpty(ply.Csys)) ply.Csys = "<layup>";
                    bool isSolid = _compositeLayup.ElementType == CompositeElementType.Solid;
                    int rowIndex = dgvPlies.Rows.Add(
                        (i + 1).ToString(),
                        ply.Name,
                        ply.Region,
                        ply.Material,
                        ply.Thickness.ToString(),
                        ply.Csys,
                        GetRosetteCellText(i),
                        ply.RotationAngle.ToString()
                    );
                    string effectiveRosette = _compositeLayup.GetEffectivePlyRosette(i);
                    // An inherited rosette is shown greyed out to separate it from an explicit one
                    DataGridViewCell rosetteCell = dgvPlies.Rows[rowIndex].Cells["colRosette"];
                    if (_compositeLayup.IsPlyRosetteInherited(i))
                    {
                        rosetteCell.Style.ForeColor = SystemColors.GrayText;
                        rosetteCell.Style.Font = new Font(dgvPlies.Font, FontStyle.Italic);
                    }
                    rosetteCell.ToolTipText = "Right-click to assign a rosette to this ply.";
                    // The angle is an offset from whatever reference direction applies to the ply
                    dgvPlies.Rows[rowIndex].Cells["colOrientation"].ToolTipText =
                        string.IsNullOrEmpty(effectiveRosette)
                        ? "Fiber angle in degrees."
                        : "Fiber angle in degrees, measured from the reference direction given by " +
                          "rosette '" + effectiveRosette + "'.";
                }
            }
        }

        private bool UsesAnyRosette()
        {
            if (_compositeLayup == null) return false;
            if (_compositeLayup.RosetteEnabled) return true;
            return _compositeLayup.UsesAnyRosette();
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

                // The cell holds nothing but the angle, whether a rosette applies or not
                if (double.TryParse(row.Cells["colOrientation"].Value?.ToString(), out double rotation))
                    ply.RotationAngle = rotation;
            }
        }

        // Cell text of the Rosette column; empty when the ply does not use a rosette
        private string GetRosetteCellText(int plyIndex)
        {
            string rosette = _compositeLayup.GetEffectivePlyRosette(plyIndex);
            if (string.IsNullOrEmpty(rosette)) return "";
            return _compositeLayup.IsPlyRosetteInherited(plyIndex) ? rosette + " (layup)" : rosette;
        }

        private void Add()
        {
            // A cell left in edit mode must be committed first, otherwise what was just typed is lost
            dgvPlies.EndEdit();
            // Update all plies from grid
            for (int i = 0; i < dgvPlies.Rows.Count; i++)
            {
                UpdatePlyFromGrid(i);
            }

            // Assign the name from the TextBox (validation happens in the Name setter)
            _compositeLayup.Name = tbName.Text.Trim();

            // Check if the name exists
            UserControls.FrmProperties.CheckName(_compositeLayupToEditName, _compositeLayup.Name, _compositeLayupNames, "composite layup");

            CheckSolidDiscreteOrientations();
            CheckRosetteDefinition();

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

        private void CheckRosetteDefinition()
        {
            if (_compositeLayup == null) return;
            //
            if (_compositeLayup.RosetteEnabled && string.IsNullOrEmpty(_compositeLayup.OrientationRosette))
                throw new CaeException("The orientation definition is set to 'Rosette' but no rosette " +
                                       "is selected.");
            //
            List<string> problems = new List<string>();
            string[] rosetteNames = _controller.GetRosetteNames();
            foreach (string name in _compositeLayup.Plies.Select(p => p.OrientationRosette)
                                                        .Concat(new string[] { _compositeLayup.RosetteEnabled
                                                                               ? _compositeLayup.OrientationRosette
                                                                               : null })
                                                        .Where(n => !string.IsNullOrEmpty(n)).Distinct())
            {
                if (!rosetteNames.Contains(name))
                {
                    problems.Add("The rosette '" + name + "' does not exist any more.");
                    continue;
                }
                Rosette rosette = _controller.GetRosette(name);
                string error = rosette.CheckValidity();
                if (error != null) problems.Add("Rosette '" + name + "': " + error);
            }
            if (_compositeLayup.UsesAnyRosette() &&
                (_compositeLayup.DiscreteOrientationsEnabled || _compositeLayup.UseDrapingForFiberDirection))
            {
                problems.Add("Discrete orientations take precedence over the rosette definition; " +
                             "the assigned rosettes will be ignored at export.");
            }
            if (problems.Count > 0)
                MessageBox.Show(string.Join("\n", problems), "Rosette",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void CheckSolidDiscreteOrientations()
        {
            // Soft warnings only — the CalculiX export re-validates
            if (_compositeLayup == null ||
                _compositeLayup.ElementType != CompositeElementType.Solid ||
                !_compositeLayup.DiscreteOrientationsEnabled) return;

            List<string> problems = new List<string>();
            DiscreteOrientationsData data = _compositeLayup.DiscreteOrientationsData;
            if (data != null && data.Points != null && data.Points.Count > 0)
            {
                int maxLayerCount = 0;
                foreach (StackingPoint sp in data.Points)
                    if (sp.Layers != null && sp.Layers.Count > maxLayerCount) maxLayerCount = sp.Layers.Count;
                if (_compositeLayup.Plies.Count > maxLayerCount)
                    problems.Add(string.Format(
                        "The layup has {0} plies but the XML stacking points define at most {1} layer(s); " +
                        "extra plies will use the tow-drop fallback.",
                        _compositeLayup.Plies.Count, maxLayerCount));
            }
            foreach (Ply ply in _compositeLayup.Plies)
            {
                if (string.IsNullOrEmpty(ply.Region))
                    problems.Add("Ply '" + ply.Name + "' has no region; it will be skipped at export.");
                else if (!_controller.IsElementSetValidForSolid(ply.Region))
                    problems.Add("Ply '" + ply.Name + "' region '" + ply.Region +
                                 "' contains non-solid elements; they will be skipped at export.");
            }
            if (problems.Count > 0)
                MessageBox.Show(string.Join("\n", problems), "Discrete Orientations",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        private void TriggerPropagationAsync(int plyIndex)
        {
            int gen = ++_propagationGeneration;
            lPropagationStatus.Text = "Computing...";
            lPropagationStatus.Visible = true;
            //
            CompositeLayup layup = _compositeLayup;
            Task.Run(() => _controller.DrawTapePropagation(layup, plyIndex))
                .ContinueWith(t =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    BeginInvoke((Action)(() =>
                    {
                        if (gen != _propagationGeneration) return;
                        if (t.IsFaulted) { lPropagationStatus.Visible = false; return; }
                        if (t.Result == -2) lPropagationStatus.Text = "Rosette mode not implemented yet";
                        else if (t.Result >= 0) lPropagationStatus.Text = t.Result + " tape(s)";
                        else lPropagationStatus.Text = "No preview (set a seed point and tape width)";
                    }));
                });
        }
        private void CancelPropagation()
        {
            ++_propagationGeneration;
            _controller.ClearTapePropagation();
            lPropagationStatus.Visible = false;
        }

        private string GetCompositeLayupName(string name)
        {
            return _compositeLayupNames.GetNextNumberedKey(name);
        }
    }
}
