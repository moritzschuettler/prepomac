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
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CaeGlobals;
using CaeModel;

namespace PrePoMax.Forms
{
    class FrmRosette : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables
        private Controller _controller;
        private string[] _rosetteNames;
        private string _rosetteToEditName;
        private Rosette _rosette;
        private bool _loadingControls;
        private System.ComponentModel.IContainer components;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private TextBox tbName;
        private Label lType;
        private ComboBox cbType;
        private GroupBox gbAxes;
        private Label lAxisX;
        private TextBox tbAxisXx;
        private TextBox tbAxisXy;
        private TextBox tbAxisXz;
        private Label lAxisY;
        private TextBox tbAxisYx;
        private TextBox tbAxisYy;
        private TextBox tbAxisYz;
        private Button btnNormalize;
        private Button btnOrthogonalize;
        private GroupBox gbCurves;
        private Label lStepFile;
        private TextBox tbStepFile;
        private Button btnImportStep;
        private Label lChordTolerance;
        private TextBox tbChordTolerance;
        private Label lChordHint;
        private DataGridView dgvCurves;
        private Button btnReverseSelected;
        private Button btnReverseAll;
        private Button btnRemoveSelected;
        private Label lDirectionTolerance;
        private NumericUpDown nudDirectionTolerance;
        private RadioButton rbGuide0;
        private RadioButton rbGuide90;
        private Label lCurveInfo;
        private Button btnOK;
        private Button btnCancel;


        // Properties
        public Rosette Rosette
        {
            get { return _rosette; }
            set { _rosette = value != null ? value.DeepCopy() : null; }
        }


        // Constructors
        public FrmRosette(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _rosette = null;
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.gbData = new System.Windows.Forms.GroupBox();
            this.lName = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.lType = new System.Windows.Forms.Label();
            this.cbType = new System.Windows.Forms.ComboBox();
            this.gbAxes = new System.Windows.Forms.GroupBox();
            this.lAxisX = new System.Windows.Forms.Label();
            this.tbAxisXx = new System.Windows.Forms.TextBox();
            this.tbAxisXy = new System.Windows.Forms.TextBox();
            this.tbAxisXz = new System.Windows.Forms.TextBox();
            this.lAxisY = new System.Windows.Forms.Label();
            this.tbAxisYx = new System.Windows.Forms.TextBox();
            this.tbAxisYy = new System.Windows.Forms.TextBox();
            this.tbAxisYz = new System.Windows.Forms.TextBox();
            this.btnNormalize = new System.Windows.Forms.Button();
            this.btnOrthogonalize = new System.Windows.Forms.Button();
            this.gbCurves = new System.Windows.Forms.GroupBox();
            this.lStepFile = new System.Windows.Forms.Label();
            this.tbStepFile = new System.Windows.Forms.TextBox();
            this.btnImportStep = new System.Windows.Forms.Button();
            this.lChordTolerance = new System.Windows.Forms.Label();
            this.tbChordTolerance = new System.Windows.Forms.TextBox();
            this.lChordHint = new System.Windows.Forms.Label();
            this.dgvCurves = new System.Windows.Forms.DataGridView();
            this.btnReverseSelected = new System.Windows.Forms.Button();
            this.btnReverseAll = new System.Windows.Forms.Button();
            this.btnRemoveSelected = new System.Windows.Forms.Button();
            this.lDirectionTolerance = new System.Windows.Forms.Label();
            this.nudDirectionTolerance = new System.Windows.Forms.NumericUpDown();
            this.rbGuide0 = new System.Windows.Forms.RadioButton();
            this.rbGuide90 = new System.Windows.Forms.RadioButton();
            this.lCurveInfo = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbData.SuspendLayout();
            this.gbAxes.SuspendLayout();
            this.gbCurves.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCurves)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDirectionTolerance)).BeginInit();
            this.SuspendLayout();
            //
            // gbData
            //
            this.gbData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbData.Controls.Add(this.lName);
            this.gbData.Controls.Add(this.tbName);
            this.gbData.Controls.Add(this.lType);
            this.gbData.Controls.Add(this.cbType);
            this.gbData.Location = new System.Drawing.Point(12, 12);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(440, 80);
            this.gbData.TabIndex = 0;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";
            //
            // lName
            //
            this.lName.AutoSize = true;
            this.lName.Location = new System.Drawing.Point(10, 25);
            this.lName.Name = "lName";
            this.lName.Size = new System.Drawing.Size(39, 15);
            this.lName.TabIndex = 0;
            this.lName.Text = "Name";
            //
            // tbName
            //
            this.tbName.Location = new System.Drawing.Point(120, 22);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(200, 23);
            this.tbName.TabIndex = 1;
            this.tbName.TextChanged += new System.EventHandler(this.tbName_TextChanged);
            //
            // lType
            //
            this.lType.AutoSize = true;
            this.lType.Location = new System.Drawing.Point(10, 53);
            this.lType.Name = "lType";
            this.lType.Size = new System.Drawing.Size(31, 15);
            this.lType.TabIndex = 2;
            this.lType.Text = "Type";
            //
            // cbType
            //
            this.cbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbType.FormattingEnabled = true;
            this.cbType.Location = new System.Drawing.Point(120, 50);
            this.cbType.Name = "cbType";
            this.cbType.Size = new System.Drawing.Size(200, 23);
            this.cbType.TabIndex = 3;
            this.cbType.SelectedIndexChanged += new System.EventHandler(this.cbType_SelectedIndexChanged);
            //
            // gbAxes
            //
            this.gbAxes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbAxes.Controls.Add(this.lAxisX);
            this.gbAxes.Controls.Add(this.tbAxisXx);
            this.gbAxes.Controls.Add(this.tbAxisXy);
            this.gbAxes.Controls.Add(this.tbAxisXz);
            this.gbAxes.Controls.Add(this.lAxisY);
            this.gbAxes.Controls.Add(this.tbAxisYx);
            this.gbAxes.Controls.Add(this.tbAxisYy);
            this.gbAxes.Controls.Add(this.tbAxisYz);
            this.gbAxes.Controls.Add(this.btnNormalize);
            this.gbAxes.Controls.Add(this.btnOrthogonalize);
            this.gbAxes.Location = new System.Drawing.Point(12, 98);
            this.gbAxes.Name = "gbAxes";
            this.gbAxes.Size = new System.Drawing.Size(440, 90);
            this.gbAxes.TabIndex = 1;
            this.gbAxes.TabStop = false;
            this.gbAxes.Text = "Global axes";
            //
            // lAxisX
            //
            this.lAxisX.AutoSize = true;
            this.lAxisX.Location = new System.Drawing.Point(10, 25);
            this.lAxisX.Name = "lAxisX";
            this.lAxisX.Size = new System.Drawing.Size(90, 15);
            this.lAxisX.TabIndex = 0;
            this.lAxisX.Text = "X axis direction";
            //
            // tbAxisXx
            //
            this.tbAxisXx.Location = new System.Drawing.Point(120, 22);
            this.tbAxisXx.Name = "tbAxisXx";
            this.tbAxisXx.Size = new System.Drawing.Size(70, 23);
            this.tbAxisXx.TabIndex = 1;
            this.tbAxisXx.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // tbAxisXy
            //
            this.tbAxisXy.Location = new System.Drawing.Point(196, 22);
            this.tbAxisXy.Name = "tbAxisXy";
            this.tbAxisXy.Size = new System.Drawing.Size(70, 23);
            this.tbAxisXy.TabIndex = 2;
            this.tbAxisXy.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // tbAxisXz
            //
            this.tbAxisXz.Location = new System.Drawing.Point(272, 22);
            this.tbAxisXz.Name = "tbAxisXz";
            this.tbAxisXz.Size = new System.Drawing.Size(70, 23);
            this.tbAxisXz.TabIndex = 3;
            this.tbAxisXz.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // lAxisY
            //
            this.lAxisY.AutoSize = true;
            this.lAxisY.Location = new System.Drawing.Point(10, 55);
            this.lAxisY.Name = "lAxisY";
            this.lAxisY.Size = new System.Drawing.Size(90, 15);
            this.lAxisY.TabIndex = 4;
            this.lAxisY.Text = "Y axis direction";
            //
            // tbAxisYx
            //
            this.tbAxisYx.Location = new System.Drawing.Point(120, 52);
            this.tbAxisYx.Name = "tbAxisYx";
            this.tbAxisYx.Size = new System.Drawing.Size(70, 23);
            this.tbAxisYx.TabIndex = 5;
            this.tbAxisYx.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // tbAxisYy
            //
            this.tbAxisYy.Location = new System.Drawing.Point(196, 52);
            this.tbAxisYy.Name = "tbAxisYy";
            this.tbAxisYy.Size = new System.Drawing.Size(70, 23);
            this.tbAxisYy.TabIndex = 6;
            this.tbAxisYy.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // tbAxisYz
            //
            this.tbAxisYz.Location = new System.Drawing.Point(272, 52);
            this.tbAxisYz.Name = "tbAxisYz";
            this.tbAxisYz.Size = new System.Drawing.Size(70, 23);
            this.tbAxisYz.TabIndex = 7;
            this.tbAxisYz.TextChanged += new System.EventHandler(this.Axis_TextChanged);
            //
            // btnNormalize
            //
            this.btnNormalize.Location = new System.Drawing.Point(350, 21);
            this.btnNormalize.Name = "btnNormalize";
            this.btnNormalize.Size = new System.Drawing.Size(80, 25);
            this.btnNormalize.TabIndex = 8;
            this.btnNormalize.Text = "Normalize";
            this.btnNormalize.UseVisualStyleBackColor = true;
            this.btnNormalize.Click += new System.EventHandler(this.btnNormalize_Click);
            //
            // btnOrthogonalize
            //
            this.btnOrthogonalize.Location = new System.Drawing.Point(350, 51);
            this.btnOrthogonalize.Name = "btnOrthogonalize";
            this.btnOrthogonalize.Size = new System.Drawing.Size(80, 25);
            this.btnOrthogonalize.TabIndex = 9;
            this.btnOrthogonalize.Text = "Orthogon.";
            this.btnOrthogonalize.UseVisualStyleBackColor = true;
            this.btnOrthogonalize.Click += new System.EventHandler(this.btnOrthogonalize_Click);
            //
            // gbCurves
            //
            this.gbCurves.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbCurves.Controls.Add(this.lStepFile);
            this.gbCurves.Controls.Add(this.tbStepFile);
            this.gbCurves.Controls.Add(this.btnImportStep);
            this.gbCurves.Controls.Add(this.lChordTolerance);
            this.gbCurves.Controls.Add(this.tbChordTolerance);
            this.gbCurves.Controls.Add(this.lChordHint);
            this.gbCurves.Controls.Add(this.dgvCurves);
            this.gbCurves.Controls.Add(this.btnReverseSelected);
            this.gbCurves.Controls.Add(this.btnReverseAll);
            this.gbCurves.Controls.Add(this.btnRemoveSelected);
            this.gbCurves.Controls.Add(this.lDirectionTolerance);
            this.gbCurves.Controls.Add(this.nudDirectionTolerance);
            this.gbCurves.Controls.Add(this.rbGuide0);
            this.gbCurves.Controls.Add(this.rbGuide90);
            this.gbCurves.Controls.Add(this.lCurveInfo);
            this.gbCurves.Location = new System.Drawing.Point(12, 194);
            this.gbCurves.Name = "gbCurves";
            this.gbCurves.Size = new System.Drawing.Size(440, 300);
            this.gbCurves.TabIndex = 2;
            this.gbCurves.TabStop = false;
            this.gbCurves.Text = "Curves";
            //
            // lStepFile
            //
            this.lStepFile.AutoSize = true;
            this.lStepFile.Location = new System.Drawing.Point(10, 25);
            this.lStepFile.Name = "lStepFile";
            this.lStepFile.Size = new System.Drawing.Size(60, 15);
            this.lStepFile.TabIndex = 0;
            this.lStepFile.Text = "STEP file";
            //
            // tbStepFile
            //
            this.tbStepFile.Location = new System.Drawing.Point(120, 22);
            this.tbStepFile.Name = "tbStepFile";
            this.tbStepFile.ReadOnly = true;
            this.tbStepFile.Size = new System.Drawing.Size(210, 23);
            this.tbStepFile.TabIndex = 1;
            //
            // btnImportStep
            //
            this.btnImportStep.Location = new System.Drawing.Point(336, 21);
            this.btnImportStep.Name = "btnImportStep";
            this.btnImportStep.Size = new System.Drawing.Size(94, 25);
            this.btnImportStep.TabIndex = 2;
            this.btnImportStep.Text = "Import STEP...";
            this.btnImportStep.UseVisualStyleBackColor = true;
            this.btnImportStep.Click += new System.EventHandler(this.btnImportStep_Click);
            //
            // lChordTolerance
            //
            this.lChordTolerance.AutoSize = true;
            this.lChordTolerance.Location = new System.Drawing.Point(10, 55);
            this.lChordTolerance.Name = "lChordTolerance";
            this.lChordTolerance.Size = new System.Drawing.Size(100, 15);
            this.lChordTolerance.TabIndex = 3;
            this.lChordTolerance.Text = "Chord tolerance";
            //
            // tbChordTolerance
            //
            this.tbChordTolerance.Location = new System.Drawing.Point(120, 52);
            this.tbChordTolerance.Name = "tbChordTolerance";
            this.tbChordTolerance.Size = new System.Drawing.Size(70, 23);
            this.tbChordTolerance.TabIndex = 4;
            this.tbChordTolerance.Text = "0";
            //
            // lChordHint
            //
            this.lChordHint.AutoSize = true;
            this.lChordHint.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lChordHint.Location = new System.Drawing.Point(196, 55);
            this.lChordHint.Name = "lChordHint";
            this.lChordHint.Size = new System.Drawing.Size(230, 15);
            this.lChordHint.TabIndex = 12;
            this.lChordHint.Text = "0 = automatic (bounding box diagonal / 2000)";
            //
            // lCurveInfo
            //
            this.lCurveInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lCurveInfo.AutoSize = true;
            this.lCurveInfo.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lCurveInfo.Location = new System.Drawing.Point(232, 275);
            this.lCurveInfo.Name = "lCurveInfo";
            this.lCurveInfo.Size = new System.Drawing.Size(200, 15);
            this.lCurveInfo.TabIndex = 5;
            this.lCurveInfo.Text = "";
            //
            // dgvCurves
            //
            this.dgvCurves.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
            | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCurves.AllowUserToAddRows = false;
            this.dgvCurves.AllowUserToDeleteRows = false;
            this.dgvCurves.ColumnHeadersHeightSizeMode =
                System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCurves.Location = new System.Drawing.Point(10, 82);
            this.dgvCurves.Name = "dgvCurves";
            this.dgvCurves.RowHeadersVisible = false;
            this.dgvCurves.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCurves.Size = new System.Drawing.Size(420, 125);
            this.dgvCurves.TabIndex = 6;
            this.dgvCurves.CellContentClick +=
                new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCurves_CellContentClick);
            this.dgvCurves.CellDoubleClick +=
                new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvCurves_CellDoubleClick);
            //
            // btnReverseSelected
            //
            this.btnReverseSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReverseSelected.Location = new System.Drawing.Point(10, 213);
            this.btnReverseSelected.Name = "btnReverseSelected";
            this.btnReverseSelected.Size = new System.Drawing.Size(120, 25);
            this.btnReverseSelected.TabIndex = 7;
            this.btnReverseSelected.Text = "⇄ Reverse selected";
            this.btnReverseSelected.UseVisualStyleBackColor = true;
            this.btnReverseSelected.Click += new System.EventHandler(this.btnReverseSelected_Click);
            //
            // btnReverseAll
            //
            this.btnReverseAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReverseAll.Location = new System.Drawing.Point(136, 213);
            this.btnReverseAll.Name = "btnReverseAll";
            this.btnReverseAll.Size = new System.Drawing.Size(90, 25);
            this.btnReverseAll.TabIndex = 8;
            this.btnReverseAll.Text = "⇄ Reverse all";
            this.btnReverseAll.UseVisualStyleBackColor = true;
            this.btnReverseAll.Click += new System.EventHandler(this.btnReverseAll_Click);
            //
            // btnRemoveSelected
            //
            this.btnRemoveSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRemoveSelected.Location = new System.Drawing.Point(232, 213);
            this.btnRemoveSelected.Name = "btnRemoveSelected";
            this.btnRemoveSelected.Size = new System.Drawing.Size(90, 25);
            this.btnRemoveSelected.TabIndex = 9;
            this.btnRemoveSelected.Text = "Remove";
            this.btnRemoveSelected.UseVisualStyleBackColor = true;
            this.btnRemoveSelected.Click += new System.EventHandler(this.btnRemoveSelected_Click);
            //
            // rbGuide0
            //
            this.rbGuide0.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.rbGuide0.AutoSize = true;
            this.rbGuide0.Checked = true;
            this.rbGuide0.Location = new System.Drawing.Point(10, 245);
            this.rbGuide0.Name = "rbGuide0";
            this.rbGuide0.Size = new System.Drawing.Size(175, 19);
            this.rbGuide0.TabIndex = 13;
            this.rbGuide0.TabStop = true;
            this.rbGuide0.Text = "0° guided (X along the curve)";
            this.rbGuide0.UseVisualStyleBackColor = true;
            this.rbGuide0.CheckedChanged += new System.EventHandler(this.rbGuide_CheckedChanged);
            //
            // rbGuide90
            //
            this.rbGuide90.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.rbGuide90.AutoSize = true;
            this.rbGuide90.Location = new System.Drawing.Point(210, 245);
            this.rbGuide90.Name = "rbGuide90";
            this.rbGuide90.Size = new System.Drawing.Size(200, 19);
            this.rbGuide90.TabIndex = 14;
            this.rbGuide90.Text = "90° guided (X perpendicular)";
            this.rbGuide90.UseVisualStyleBackColor = true;
            this.rbGuide90.CheckedChanged += new System.EventHandler(this.rbGuide_CheckedChanged);
            //
            // lDirectionTolerance
            //
            this.lDirectionTolerance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lDirectionTolerance.AutoSize = true;
            this.lDirectionTolerance.Location = new System.Drawing.Point(10, 275);
            this.lDirectionTolerance.Name = "lDirectionTolerance";
            this.lDirectionTolerance.Size = new System.Drawing.Size(130, 15);
            this.lDirectionTolerance.TabIndex = 10;
            this.lDirectionTolerance.Text = "Direction tolerance [°]";
            //
            // nudDirectionTolerance
            //
            this.nudDirectionTolerance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Left)));
            this.nudDirectionTolerance.DecimalPlaces = 1;
            this.nudDirectionTolerance.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
            this.nudDirectionTolerance.Location = new System.Drawing.Point(150, 273);
            this.nudDirectionTolerance.Maximum = new decimal(new int[] { 90, 0, 0, 0 });
            this.nudDirectionTolerance.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudDirectionTolerance.Name = "nudDirectionTolerance";
            this.nudDirectionTolerance.Size = new System.Drawing.Size(70, 23);
            this.nudDirectionTolerance.TabIndex = 11;
            this.nudDirectionTolerance.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.nudDirectionTolerance.ValueChanged +=
                new System.EventHandler(this.nudDirectionTolerance_ValueChanged);
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(296, 502);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 25);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(377, 502);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 25);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // FrmRosette
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(464, 539);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.gbAxes);
            this.Controls.Add(this.gbCurves);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmRosette";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Edit Rosette";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmRosette_FormClosing);
            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.gbAxes.ResumeLayout(false);
            this.gbAxes.PerformLayout();
            this.gbCurves.ResumeLayout(false);
            this.gbCurves.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCurves)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDirectionTolerance)).EndInit();
            this.ResumeLayout(false);
        }


        // Event handlers
        private void tbName_TextChanged(object sender, EventArgs e)
        {
            if (_loadingControls) return;
            _propertyItemChanged = true;
        }

        private void cbType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loadingControls || _rosette == null) return;
            _rosette.Type = (RosetteType)cbType.SelectedIndex;
            _propertyItemChanged = true;
            UpdateControlsVisibility();
            RedrawRosette();
        }

        private void Axis_TextChanged(object sender, EventArgs e)
        {
            if (_loadingControls || _rosette == null) return;
            _rosette.AxisX = ReadVector(tbAxisXx, tbAxisXy, tbAxisXz, _rosette.AxisX);
            _rosette.AxisY = ReadVector(tbAxisYx, tbAxisYy, tbAxisYz, _rosette.AxisY);
            _propertyItemChanged = true;
            RedrawRosette();
        }

        private void btnNormalize_Click(object sender, EventArgs e)
        {
            if (_rosette == null) return;
            Vec3D x = new Vec3D(_rosette.AxisX);
            Vec3D y = new Vec3D(_rosette.AxisY);
            if (x.Normalize() > 1E-12) _rosette.AxisX = x.Coor;
            if (y.Normalize() > 1E-12) _rosette.AxisY = y.Coor;
            _propertyItemChanged = true;
            UpdateAxisControls();
            RedrawRosette();
        }

        private void btnOrthogonalize_Click(object sender, EventArgs e)
        {
            if (_rosette == null) return;
            _rosette.Orthogonalize();
            _propertyItemChanged = true;
            UpdateAxisControls();
            RedrawRosette();
        }

        private void btnImportStep_Click(object sender, EventArgs e)
        {
            if (_rosette == null) return;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select a STEP file containing the rosette guide curves";
                ofd.Filter = "STEP files (*.stp;*.step)|*.stp;*.step|All files (*.*)|*.*";
                if (ofd.ShowDialog(this) != DialogResult.OK) return;
                //
                double chordTolerance;
                if (!double.TryParse(tbChordTolerance.Text, NumberStyles.Float, CultureInfo.CurrentCulture,
                                     out chordTolerance) || chordTolerance < 0)
                    chordTolerance = 0;
                //
                try
                {
                    Cursor = Cursors.WaitCursor;
                    string log;
                    double usedTolerance;
                    List<RosetteCurve> curves = _controller.ReadRosetteCurvesFromStep(ofd.FileName, chordTolerance,
                                                                                      out log, out usedTolerance);
                    _rosette.Curves = curves;
                    _rosette.StepFilePath = ofd.FileName;
                    _rosette.Type = RosetteType.FromCurves;
                    // Write back the tolerance that was really applied so that the panel never shows
                    // a meaningless 0 after an automatic import
                    _rosette.TessellationTolerance = usedTolerance;
                    _propertyItemChanged = true;
                    //
                    _controller.Form.WriteDataToOutput(log);
                    UpdateFormFromRosette();
                    RedrawRosette();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Import curves from STEP", MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        private void dgvCurves_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_rosette == null || e.RowIndex < 0 || e.RowIndex >= _rosette.Curves.Count) return;
            if (dgvCurves.Columns[e.ColumnIndex].Name != "colInclude") return;
            //
            dgvCurves.EndEdit();
            _rosette.Curves[e.RowIndex].Included = !_rosette.Curves[e.RowIndex].Included;
            _propertyItemChanged = true;
            RefreshCurvesGrid();
            RedrawRosette();
        }

        // A double click on a row is the quickest way to flip a single curve
        private void dgvCurves_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_rosette == null || e.RowIndex < 0 || e.RowIndex >= _rosette.Curves.Count) return;
            if (dgvCurves.Columns[e.ColumnIndex].Name == "colInclude") return;
            //
            _rosette.Curves[e.RowIndex].Reversed = !_rosette.Curves[e.RowIndex].Reversed;
            _propertyItemChanged = true;
            RefreshCurvesGrid();
            RedrawRosette();
        }

        private void btnReverseSelected_Click(object sender, EventArgs e)
        {
            if (_rosette == null) return;
            bool any = false;
            foreach (DataGridViewRow row in dgvCurves.SelectedRows)
            {
                if (row.Index < 0 || row.Index >= _rosette.Curves.Count) continue;
                _rosette.Curves[row.Index].Reversed = !_rosette.Curves[row.Index].Reversed;
                any = true;
            }
            if (!any) return;
            _propertyItemChanged = true;
            RefreshCurvesGrid();
            RedrawRosette();
        }

        private void btnReverseAll_Click(object sender, EventArgs e)
        {
            if (_rosette == null || _rosette.Curves == null) return;
            foreach (RosetteCurve curve in _rosette.Curves) curve.Reversed = !curve.Reversed;
            _propertyItemChanged = true;
            RefreshCurvesGrid();
            RedrawRosette();
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            if (_rosette == null || _rosette.Curves == null) return;
            List<int> indices = new List<int>();
            foreach (DataGridViewRow row in dgvCurves.SelectedRows) indices.Add(row.Index);
            indices.Sort();
            for (int i = indices.Count - 1; i >= 0; i--)
            {
                if (indices[i] >= 0 && indices[i] < _rosette.Curves.Count) _rosette.Curves.RemoveAt(indices[i]);
            }
            if (indices.Count == 0) return;
            _propertyItemChanged = true;
            RefreshCurvesGrid();
            RedrawRosette();
        }

        private void rbGuide_CheckedChanged(object sender, EventArgs e)
        {
            if (_loadingControls || _rosette == null) return;
            _rosette.GuideAngle = rbGuide90.Checked ? RosetteGuideAngle.Deg90 : RosetteGuideAngle.Deg0;
            _propertyItemChanged = true;
        }

        private void nudDirectionTolerance_ValueChanged(object sender, EventArgs e)
        {
            if (_loadingControls || _rosette == null) return;
            _rosette.DirectionToleranceDeg = (double)nudDirectionTolerance.Value;
            _propertyItemChanged = true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                Add();
                _controller.ClearRosetteCurves();
                Hide();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            _controller.ClearRosetteCurves();
            Hide();
        }

        private void FrmRosette_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _controller.ClearRosetteCurves();
                Hide();
            }
        }


        // Methods
        public bool PrepareForm(string stepName, string rosetteToEditName)
        {
            _propertyItemChanged = false;
            _rosetteNames = _controller.GetRosetteNames();
            _rosetteToEditName = rosetteToEditName;
            //
            InitializeComboBoxes();
            //
            if (_rosetteToEditName != null)
            {
                Rosette = _controller.GetRosette(_rosetteToEditName);
                Text = "Edit Rosette";
            }
            else
            {
                _rosette = new Rosette(_controller.GetNextRosetteName());
                Text = "Create Rosette";
            }
            //
            UpdateFormFromRosette();
            RedrawRosette();
            //
            _controller.SetSelectByToOff();
            return true;
        }

        private void InitializeComboBoxes()
        {
            cbType.Items.Clear();
            cbType.Items.Add("Cartesian");
            cbType.Items.Add("Guided by curves");
        }

        private void UpdateFormFromRosette()
        {
            if (_rosette == null) return;
            _loadingControls = true;
            try
            {
                tbName.Text = _rosette.Name;
                cbType.SelectedIndex = (int)_rosette.Type;
                tbStepFile.Text = _rosette.StepFilePath ?? "";
                tbChordTolerance.Text = _rosette.TessellationTolerance.ToString(CultureInfo.CurrentCulture);
                nudDirectionTolerance.Value = (decimal)Math.Max((double)nudDirectionTolerance.Minimum,
                    Math.Min((double)nudDirectionTolerance.Maximum, _rosette.DirectionToleranceDeg));
                rbGuide0.Checked = _rosette.GuideAngle == RosetteGuideAngle.Deg0;
                rbGuide90.Checked = _rosette.GuideAngle == RosetteGuideAngle.Deg90;
                UpdateAxisControls();
            }
            finally
            {
                _loadingControls = false;
            }
            SetupCurvesGrid();
            RefreshCurvesGrid();
            UpdateControlsVisibility();
        }

        private void UpdateAxisControls()
        {
            bool previous = _loadingControls;
            _loadingControls = true;
            try
            {
                tbAxisXx.Text = _rosette.AxisX[0].ToString(CultureInfo.CurrentCulture);
                tbAxisXy.Text = _rosette.AxisX[1].ToString(CultureInfo.CurrentCulture);
                tbAxisXz.Text = _rosette.AxisX[2].ToString(CultureInfo.CurrentCulture);
                tbAxisYx.Text = _rosette.AxisY[0].ToString(CultureInfo.CurrentCulture);
                tbAxisYy.Text = _rosette.AxisY[1].ToString(CultureInfo.CurrentCulture);
                tbAxisYz.Text = _rosette.AxisY[2].ToString(CultureInfo.CurrentCulture);
            }
            finally
            {
                _loadingControls = previous;
            }
        }

        private void UpdateControlsVisibility()
        {
            bool fromCurves = cbType.SelectedIndex == (int)RosetteType.FromCurves;
            gbCurves.Visible = fromCurves;
            // The global axes still matter for a curve rosette: the Y axis is the fallback used when
            // the curve direction happens to be perpendicular to the shell surface
            lAxisY.Text = fromCurves ? "Y axis (fallback)" : "Y axis direction";
        }

        private void SetupCurvesGrid()
        {
            if (dgvCurves.Columns.Count > 0) return;
            //
            DataGridViewCheckBoxColumn colInclude = new DataGridViewCheckBoxColumn();
            colInclude.Name = "colInclude";
            colInclude.HeaderText = "";
            colInclude.Width = 30;
            dgvCurves.Columns.Add(colInclude);
            //
            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.Name = "colName";
            colName.HeaderText = "Curve";
            colName.Width = 160;
            colName.ReadOnly = true;
            dgvCurves.Columns.Add(colName);
            //
            DataGridViewTextBoxColumn colPoints = new DataGridViewTextBoxColumn();
            colPoints.Name = "colPoints";
            colPoints.HeaderText = "Points";
            colPoints.Width = 55;
            colPoints.ReadOnly = true;
            dgvCurves.Columns.Add(colPoints);
            //
            DataGridViewTextBoxColumn colLength = new DataGridViewTextBoxColumn();
            colLength.Name = "colLength";
            colLength.HeaderText = "Length";
            colLength.Width = 80;
            colLength.ReadOnly = true;
            dgvCurves.Columns.Add(colLength);
            //
            DataGridViewTextBoxColumn colReversed = new DataGridViewTextBoxColumn();
            colReversed.Name = "colReversed";
            colReversed.HeaderText = "Dir.";
            colReversed.Width = 50;
            colReversed.ReadOnly = true;
            dgvCurves.Columns.Add(colReversed);
        }

        private void RefreshCurvesGrid()
        {
            dgvCurves.Rows.Clear();
            if (_rosette == null || _rosette.Curves == null) return;
            //
            foreach (RosetteCurve curve in _rosette.Curves)
            {
                dgvCurves.Rows.Add(curve.Included,
                                   curve.SourceLabel,
                                   curve.PointCount.ToString(),
                                   curve.Length.ToString("G6", CultureInfo.CurrentCulture),
                                   curve.Reversed ? "⇦" : "⇨");
            }
            lCurveInfo.Text = string.Format("{0} curve(s), {1} used", _rosette.Curves.Count,
                                            _rosette.IncludedCurveCount);
        }

        private double[] ReadVector(TextBox x, TextBox y, TextBox z, double[] previous)
        {
            double vx, vy, vz;
            if (!double.TryParse(x.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out vx)) vx = previous[0];
            if (!double.TryParse(y.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out vy)) vy = previous[1];
            if (!double.TryParse(z.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out vz)) vz = previous[2];
            return new double[] { vx, vy, vz };
        }

        private void RedrawRosette()
        {
            // No Visible check here: ShowForm calls PrepareForm before Show, so the preview would
            // never be drawn when the panel is opened
            if (_rosette == null) return;
            _controller.DrawRosetteCurves(_rosette);
        }

        private void Add()
        {
            if (_rosette == null) return;
            //
            _rosette.Name = tbName.Text.Trim();
            UserControls.FrmProperties.CheckName(_rosetteToEditName, _rosette.Name, _rosetteNames, "rosette");
            //
            string error = _rosette.CheckValidity();
            if (error != null) throw new CaeException(error);
            //
            if (_rosetteToEditName == null) _controller.AddRosetteCommand(_rosette);
            else if (_propertyItemChanged) _controller.ReplaceRosetteCommand(_rosetteToEditName, _rosette);
        }
    }
}
