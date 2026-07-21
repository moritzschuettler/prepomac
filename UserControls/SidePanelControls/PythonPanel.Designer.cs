// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

namespace UserControls
{
    partial class PythonPanel
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PythonPanel));
            this.labTitle = new System.Windows.Forms.Label();
            this.panBackground = new System.Windows.Forms.Panel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.petbPythonEditor = new UserControls.PythonEditorTextBox();
            this.pctbPythonConsole = new UserControls.PythonConsoleTextBox();
            this.ttText = new System.Windows.Forms.ToolTip(this.components);
            this.btnSaveAs = new UserControls.MenuStyleButton();
            this.btnRunSelected = new UserControls.MenuStyleButton();
            this.btnRun = new UserControls.MenuStyleButton();
            this.btnRedo = new UserControls.MenuStyleButton();
            this.btnUndo = new UserControls.MenuStyleButton();
            this.btnSave = new UserControls.MenuStyleButton();
            this.btnOpen = new UserControls.MenuStyleButton();
            this.btnNew = new UserControls.MenuStyleButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.panBackground.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // labTitle
            // 
            this.labTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labTitle.BackColor = System.Drawing.SystemColors.Control;
            this.labTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.labTitle.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labTitle.Location = new System.Drawing.Point(203, 5);
            this.labTitle.Name = "labTitle";
            this.labTitle.Size = new System.Drawing.Size(556, 25);
            this.labTitle.TabIndex = 19;
            this.labTitle.Text = "Python Script Editor";
            this.labTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labTitle.UseCompatibleTextRendering = true;
            // 
            // panBackground
            // 
            this.panBackground.Controls.Add(this.splitContainer1);
            this.panBackground.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panBackground.Location = new System.Drawing.Point(0, 0);
            this.panBackground.Name = "panBackground";
            this.panBackground.Size = new System.Drawing.Size(789, 585);
            this.panBackground.TabIndex = 20;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.btnSaveAs);
            this.splitContainer1.Panel1.Controls.Add(this.btnRunSelected);
            this.splitContainer1.Panel1.Controls.Add(this.btnRun);
            this.splitContainer1.Panel1.Controls.Add(this.btnRedo);
            this.splitContainer1.Panel1.Controls.Add(this.btnUndo);
            this.splitContainer1.Panel1.Controls.Add(this.btnSave);
            this.splitContainer1.Panel1.Controls.Add(this.btnOpen);
            this.splitContainer1.Panel1.Controls.Add(this.btnNew);
            this.splitContainer1.Panel1.Controls.Add(this.petbPythonEditor);
            this.splitContainer1.Panel1.Controls.Add(this.btnClose);
            this.splitContainer1.Panel1.Controls.Add(this.labTitle);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pctbPythonConsole);
            this.splitContainer1.Size = new System.Drawing.Size(789, 585);
            this.splitContainer1.SplitterDistance = 454;
            this.splitContainer1.TabIndex = 21;
            // 
            // petbPythonEditor
            // 
            this.petbPythonEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.petbPythonEditor.Location = new System.Drawing.Point(5, 33);
            this.petbPythonEditor.Name = "petbPythonEditor";
            this.petbPythonEditor.Size = new System.Drawing.Size(779, 421);
            this.petbPythonEditor.TabIndex = 20;
            // 
            // pctbPythonConsole
            // 
            this.pctbPythonConsole.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pctbPythonConsole.Location = new System.Drawing.Point(5, 0);
            this.pctbPythonConsole.Name = "pctbPythonConsole";
            this.pctbPythonConsole.Size = new System.Drawing.Size(779, 123);
            this.pctbPythonConsole.TabIndex = 20;
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.BackColor = System.Drawing.Color.White;
            this.btnSaveAs.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnSaveAs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveAs.Image = global::UserControls.Properties.Resources.Save_as;
            this.btnSaveAs.Location = new System.Drawing.Point(81, 5);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(25, 25);
            this.btnSaveAs.TabIndex = 28;
            this.btnSaveAs.UseVisualStyleBackColor = false;
            this.btnSaveAs.Click += new System.EventHandler(this.btnSaveAs_Click);
            // 
            // btnRunSelected
            // 
            this.btnRunSelected.BackColor = System.Drawing.Color.White;
            this.btnRunSelected.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnRunSelected.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRunSelected.Image = global::UserControls.Properties.Resources.RunSelected;
            this.btnRunSelected.Location = new System.Drawing.Point(181, 5);
            this.btnRunSelected.Name = "btnRunSelected";
            this.btnRunSelected.Size = new System.Drawing.Size(25, 25);
            this.btnRunSelected.TabIndex = 27;
            this.btnRunSelected.UseVisualStyleBackColor = false;
            this.btnRunSelected.Click += new System.EventHandler(this.btnRunSelected_Click);
            // 
            // btnRun
            // 
            this.btnRun.BackColor = System.Drawing.Color.White;
            this.btnRun.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnRun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRun.Image = global::UserControls.Properties.Resources.Run;
            this.btnRun.Location = new System.Drawing.Point(156, 5);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(25, 25);
            this.btnRun.TabIndex = 26;
            this.btnRun.UseVisualStyleBackColor = false;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            // 
            // btnRedo
            // 
            this.btnRedo.BackColor = System.Drawing.Color.White;
            this.btnRedo.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnRedo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRedo.Image = global::UserControls.Properties.Resources.Redo;
            this.btnRedo.Location = new System.Drawing.Point(131, 5);
            this.btnRedo.Name = "btnRedo";
            this.btnRedo.Size = new System.Drawing.Size(25, 25);
            this.btnRedo.TabIndex = 25;
            this.btnRedo.UseVisualStyleBackColor = false;
            this.btnRedo.Click += new System.EventHandler(this.btnRedo_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.BackColor = System.Drawing.Color.White;
            this.btnUndo.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.Image = global::UserControls.Properties.Resources.Undo;
            this.btnUndo.Location = new System.Drawing.Point(106, 5);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(25, 25);
            this.btnUndo.TabIndex = 24;
            this.btnUndo.UseVisualStyleBackColor = false;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // btnSave
            // 
            this.btnSave.BackColor = System.Drawing.Color.White;
            this.btnSave.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnSave.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSave.Image = ((System.Drawing.Image)(resources.GetObject("btnSave.Image")));
            this.btnSave.Location = new System.Drawing.Point(56, 5);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(25, 25);
            this.btnSave.TabIndex = 23;
            this.btnSave.UseVisualStyleBackColor = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnOpen
            // 
            this.btnOpen.BackColor = System.Drawing.Color.White;
            this.btnOpen.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnOpen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpen.Image = global::UserControls.Properties.Resources.Open;
            this.btnOpen.Location = new System.Drawing.Point(31, 5);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(25, 25);
            this.btnOpen.TabIndex = 22;
            this.btnOpen.UseVisualStyleBackColor = false;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnNew
            // 
            this.btnNew.BackColor = System.Drawing.Color.White;
            this.btnNew.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnNew.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNew.Image = global::UserControls.Properties.Resources.New;
            this.btnNew.Location = new System.Drawing.Point(6, 5);
            this.btnNew.Name = "btnNew";
            this.btnNew.Size = new System.Drawing.Size(25, 25);
            this.btnNew.TabIndex = 21;
            this.btnNew.UseVisualStyleBackColor = false;
            this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.SystemColors.Control;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnClose.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(112)))), ((int)(((byte)(122)))));
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(232)))), ((int)(((byte)(17)))), ((int)(((byte)(35)))));
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Image = global::UserControls.Properties.Resources.Close;
            this.btnClose.Location = new System.Drawing.Point(759, 5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(25, 25);
            this.btnClose.TabIndex = 6;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            this.btnClose.MouseEnter += new System.EventHandler(this.btnClose_MouseEnter);
            this.btnClose.MouseLeave += new System.EventHandler(this.btnClose_MouseLeave);
            // 
            // PythonPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.panBackground);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Name = "PythonPanel";
            this.Size = new System.Drawing.Size(789, 585);
            this.Resize += new System.EventHandler(this.PythonPanel_Resize);
            this.panBackground.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label labTitle;
        private System.Windows.Forms.Panel panBackground;
        private PythonConsoleTextBox pctbPythonConsole;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private PythonEditorTextBox petbPythonEditor;
        private MenuStyleButton btnNew;
        private MenuStyleButton btnOpen;
        private MenuStyleButton btnSave;
        private MenuStyleButton btnUndo;
        private MenuStyleButton btnRedo;
        private MenuStyleButton btnRunSelected;
        private MenuStyleButton btnRun;
        private System.Windows.Forms.ToolTip ttText;
        private MenuStyleButton btnSaveAs;
    }
}
