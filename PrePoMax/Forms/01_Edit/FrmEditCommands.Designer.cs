// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace PrePoMax.Forms
{
    partial class FrmEditCommands
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.splMain = new System.Windows.Forms.SplitContainer();
            this.gbProperties = new System.Windows.Forms.GroupBox();
            this.dgvCommands = new System.Windows.Forms.DataGridView();
            this.gbPython = new System.Windows.Forms.GroupBox();
            this.petbPython = new UserControls.PythonEditorTextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnClearAll = new System.Windows.Forms.Button();
            this.msMain = new System.Windows.Forms.MenuStrip();
            this.tsmiFile = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSaveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiSaveAsPython = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiDividerFile1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiClose = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiView = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiColorByType = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiColorByTime = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiDividerView1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsmiShowPython = new System.Windows.Forms.ToolStripMenuItem();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnReorganize = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).BeginInit();
            this.splMain.Panel1.SuspendLayout();
            this.splMain.Panel2.SuspendLayout();
            this.splMain.SuspendLayout();
            this.gbProperties.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvCommands)).BeginInit();
            this.gbPython.SuspendLayout();
            this.msMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // splMain
            // 
            this.splMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splMain.Location = new System.Drawing.Point(12, 27);
            this.splMain.Name = "splMain";
            // 
            // splMain.Panel1
            // 
            this.splMain.Panel1.Controls.Add(this.gbProperties);
            this.splMain.Panel1MinSize = 200;
            // 
            // splMain.Panel2
            // 
            this.splMain.Panel2.Controls.Add(this.gbPython);
            this.splMain.Panel2MinSize = 200;
            this.splMain.Size = new System.Drawing.Size(760, 393);
            this.splMain.SplitterDistance = 432;
            this.splMain.TabIndex = 0;
            // 
            // gbProperties
            // 
            this.gbProperties.Controls.Add(this.dgvCommands);
            this.gbProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbProperties.Location = new System.Drawing.Point(0, 0);
            this.gbProperties.Name = "gbProperties";
            this.gbProperties.Size = new System.Drawing.Size(432, 393);
            this.gbProperties.TabIndex = 0;
            this.gbProperties.TabStop = false;
            this.gbProperties.Text = "Data";
            // 
            // dgvCommands
            // 
            this.dgvCommands.AllowDrop = true;
            this.dgvCommands.AllowUserToAddRows = false;
            this.dgvCommands.AllowUserToResizeRows = false;
            this.dgvCommands.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvCommands.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvCommands.Location = new System.Drawing.Point(3, 19);
            this.dgvCommands.Name = "dgvCommands";
            this.dgvCommands.ReadOnly = true;
            this.dgvCommands.Size = new System.Drawing.Size(423, 368);
            this.dgvCommands.TabIndex = 0;
            this.dgvCommands.CellMouseDoubleClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dgvCommands_CellMouseDoubleClick);
            this.dgvCommands.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.dgvCommands_RowPrePaint);
            this.dgvCommands.SelectionChanged += new System.EventHandler(this.dgvCommands_SelectionChanged);
            this.dgvCommands.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dgvCommands_UserDeletingRow);
            this.dgvCommands.DragDrop += new System.Windows.Forms.DragEventHandler(this.dgvCommands_DragDrop);
            // 
            // gbPython
            // 
            this.gbPython.Controls.Add(this.petbPython);
            this.gbPython.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbPython.Location = new System.Drawing.Point(0, 0);
            this.gbPython.Name = "gbPython";
            this.gbPython.Size = new System.Drawing.Size(324, 393);
            this.gbPython.TabIndex = 1;
            this.gbPython.TabStop = false;
            this.gbPython.Text = "Python";
            // 
            // petbPython
            // 
            this.petbPython.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.petbPython.Location = new System.Drawing.Point(3, 19);
            this.petbPython.Name = "petbPython";
            this.petbPython.Size = new System.Drawing.Size(315, 368);
            this.petbPython.TabIndex = 0;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(610, 426);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 13;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(691, 426);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnClearAll
            // 
            this.btnClearAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClearAll.Location = new System.Drawing.Point(177, 426);
            this.btnClearAll.Name = "btnClearAll";
            this.btnClearAll.Size = new System.Drawing.Size(75, 23);
            this.btnClearAll.TabIndex = 14;
            this.btnClearAll.Text = "Clear All";
            this.btnClearAll.UseVisualStyleBackColor = true;
            this.btnClearAll.Click += new System.EventHandler(this.btnClearAll_Click);
            // 
            // msMain
            // 
            this.msMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiFile,
            this.tsmiView});
            this.msMain.Location = new System.Drawing.Point(0, 0);
            this.msMain.Name = "msMain";
            this.msMain.Size = new System.Drawing.Size(784, 24);
            this.msMain.TabIndex = 15;
            this.msMain.Text = "menuStrip1";
            // 
            // tsmiFile
            // 
            this.tsmiFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiOpen,
            this.tsmiSaveAs,
            this.tsmiSaveAsPython,
            this.tsmiDividerFile1,
            this.tsmiClose});
            this.tsmiFile.Name = "tsmiFile";
            this.tsmiFile.Size = new System.Drawing.Size(37, 20);
            this.tsmiFile.Text = "File";
            // 
            // tsmiOpen
            // 
            this.tsmiOpen.Image = global::PrePoMax.Properties.Resources.Open;
            this.tsmiOpen.Name = "tsmiOpen";
            this.tsmiOpen.Size = new System.Drawing.Size(155, 22);
            this.tsmiOpen.Text = "Open";
            this.tsmiOpen.Click += new System.EventHandler(this.tsmiOpen_Click);
            // 
            // tsmiSaveAs
            // 
            this.tsmiSaveAs.Image = global::PrePoMax.Properties.Resources.Save;
            this.tsmiSaveAs.Name = "tsmiSaveAs";
            this.tsmiSaveAs.Size = new System.Drawing.Size(155, 22);
            this.tsmiSaveAs.Text = "Save As";
            this.tsmiSaveAs.Click += new System.EventHandler(this.tsmiSaveAs_Click);
            // 
            // tsmiSaveAsPython
            // 
            this.tsmiSaveAsPython.Name = "tsmiSaveAsPython";
            this.tsmiSaveAsPython.Size = new System.Drawing.Size(155, 22);
            this.tsmiSaveAsPython.Text = "Save As Python";
            this.tsmiSaveAsPython.Click += new System.EventHandler(this.tsmiSaveAsPython_Click);
            // 
            // tsmiDividerFile1
            // 
            this.tsmiDividerFile1.Name = "tsmiDividerFile1";
            this.tsmiDividerFile1.Size = new System.Drawing.Size(152, 6);
            // 
            // tsmiClose
            // 
            this.tsmiClose.Name = "tsmiClose";
            this.tsmiClose.Size = new System.Drawing.Size(155, 22);
            this.tsmiClose.Text = "Close";
            this.tsmiClose.Click += new System.EventHandler(this.tsmiClose_Click);
            // 
            // tsmiView
            // 
            this.tsmiView.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiColorByType,
            this.tsmiColorByTime,
            this.tsmiDividerView1,
            this.tsmiShowPython});
            this.tsmiView.Name = "tsmiView";
            this.tsmiView.Size = new System.Drawing.Size(44, 20);
            this.tsmiView.Text = "View";
            // 
            // tsmiColorByType
            // 
            this.tsmiColorByType.Checked = true;
            this.tsmiColorByType.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tsmiColorByType.Name = "tsmiColorByType";
            this.tsmiColorByType.Size = new System.Drawing.Size(146, 22);
            this.tsmiColorByType.Text = "Color by type";
            this.tsmiColorByType.Click += new System.EventHandler(this.tsmiColorByType_Click);
            // 
            // tsmiColorByTime
            // 
            this.tsmiColorByTime.Name = "tsmiColorByTime";
            this.tsmiColorByTime.Size = new System.Drawing.Size(146, 22);
            this.tsmiColorByTime.Text = "Color by time";
            this.tsmiColorByTime.Click += new System.EventHandler(this.tsmiColorByTime_Click);
            // 
            // tsmiDividerView1
            // 
            this.tsmiDividerView1.Name = "tsmiDividerView1";
            this.tsmiDividerView1.Size = new System.Drawing.Size(143, 6);
            // 
            // tsmiShowPython
            // 
            this.tsmiShowPython.Name = "tsmiShowPython";
            this.tsmiShowPython.Size = new System.Drawing.Size(180, 22);
            this.tsmiShowPython.Text = "Show Python";
            this.tsmiShowPython.Click += new System.EventHandler(this.tsmiShowPython_Click);
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReset.Location = new System.Drawing.Point(15, 426);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(75, 23);
            this.btnReset.TabIndex = 16;
            this.btnReset.Text = "Reset";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
            // 
            // btnReorganize
            // 
            this.btnReorganize.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReorganize.Location = new System.Drawing.Point(96, 426);
            this.btnReorganize.Name = "btnReorganize";
            this.btnReorganize.Size = new System.Drawing.Size(75, 23);
            this.btnReorganize.TabIndex = 17;
            this.btnReorganize.Text = "Reorganize";
            this.btnReorganize.UseVisualStyleBackColor = true;
            this.btnReorganize.Click += new System.EventHandler(this.btnReorganize_Click);
            // 
            // FrmEditCommands
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.btnReorganize);
            this.Controls.Add(this.btnReset);
            this.Controls.Add(this.btnClearAll);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.splMain);
            this.Controls.Add(this.msMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MainMenuStrip = this.msMain;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "FrmEditCommands";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Commands";
            this.splMain.Panel1.ResumeLayout(false);
            this.splMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splMain)).EndInit();
            this.splMain.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvCommands)).EndInit();
            this.gbPython.ResumeLayout(false);
            this.msMain.ResumeLayout(false);
            this.msMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splMain;
        private System.Windows.Forms.GroupBox gbProperties;
        private System.Windows.Forms.GroupBox gbPython;
        private UserControls.PythonEditorTextBox petbPython;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.DataGridView dgvCommands;
        private System.Windows.Forms.Button btnClearAll;
        private System.Windows.Forms.MenuStrip msMain;
        private System.Windows.Forms.ToolStripMenuItem tsmiFile;
        private System.Windows.Forms.ToolStripMenuItem tsmiOpen;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveAs;
        private System.Windows.Forms.ToolStripMenuItem tsmiSaveAsPython;
        private System.Windows.Forms.ToolStripMenuItem tsmiClose;
        private System.Windows.Forms.ToolStripSeparator tsmiDividerFile1;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.ToolStripMenuItem tsmiView;
        private System.Windows.Forms.ToolStripMenuItem tsmiColorByType;
        private System.Windows.Forms.ToolStripMenuItem tsmiColorByTime;
        private System.Windows.Forms.ToolStripSeparator tsmiDividerView1;
        private System.Windows.Forms.ToolStripMenuItem tsmiShowPython;
        private System.Windows.Forms.Button btnReorganize;
    }
}