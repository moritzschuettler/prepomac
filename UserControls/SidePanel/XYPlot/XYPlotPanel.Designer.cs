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
    partial class XYPlotPanel
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            this.panHeader = new System.Windows.Forms.Panel();
            this.labTitle = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.tlpPlots = new System.Windows.Forms.TableLayoutPanel();
            this.dgvData = new UserControls.DataGridViewCopyPaste();
            this.btnDoubleColumn = new UserControls.MenuStyleButton();
            this.btnSigleColumn = new UserControls.MenuStyleButton();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnSaveAs = new UserControls.MenuStyleButton();
            this.ttText = new System.Windows.Forms.ToolTip(this.components);
            this.panHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).BeginInit();
            this.SuspendLayout();
            // 
            // panHeader
            // 
            this.panHeader.BackColor = System.Drawing.Color.White;
            this.panHeader.Controls.Add(this.btnSaveAs);
            this.panHeader.Controls.Add(this.btnDoubleColumn);
            this.panHeader.Controls.Add(this.btnSigleColumn);
            this.panHeader.Controls.Add(this.labTitle);
            this.panHeader.Controls.Add(this.btnClose);
            this.panHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panHeader.Location = new System.Drawing.Point(0, 0);
            this.panHeader.Name = "panHeader";
            this.panHeader.Size = new System.Drawing.Size(500, 35);
            this.panHeader.TabIndex = 0;
            // 
            // labTitle
            // 
            this.labTitle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labTitle.BackColor = System.Drawing.SystemColors.Control;
            this.labTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.labTitle.ForeColor = System.Drawing.SystemColors.ControlText;
            this.labTitle.Location = new System.Drawing.Point(78, 5);
            this.labTitle.Name = "labTitle";
            this.labTitle.Size = new System.Drawing.Size(392, 25);
            this.labTitle.TabIndex = 0;
            this.labTitle.Text = "XY Plot";
            this.labTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labTitle.UseCompatibleTextRendering = true;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer.Location = new System.Drawing.Point(0, 35);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.tlpPlots);
            this.splitContainer.Panel1MinSize = 100;
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.dgvData);
            this.splitContainer.Panel2MinSize = 100;
            this.splitContainer.Size = new System.Drawing.Size(500, 565);
            this.splitContainer.SplitterDistance = 338;
            this.splitContainer.SplitterWidth = 6;
            this.splitContainer.TabIndex = 1;
            // 
            // tlpPlots
            // 
            this.tlpPlots.BackColor = System.Drawing.SystemColors.Control;
            this.tlpPlots.ColumnCount = 1;
            this.tlpPlots.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPlots.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpPlots.Location = new System.Drawing.Point(0, 0);
            this.tlpPlots.Name = "tlpPlots";
            this.tlpPlots.RowCount = 1;
            this.tlpPlots.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpPlots.Size = new System.Drawing.Size(500, 338);
            this.tlpPlots.TabIndex = 0;
            // 
            // dgvData
            // 
            this.dgvData.AllowUserToAddRows = false;
            this.dgvData.AllowUserToDeleteRows = false;
            this.dgvData.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.dgvData.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvData.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvData.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvData.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvData.EnableCutMenu = false;
            this.dgvData.EnablePasteMenu = false;
            this.dgvData.EnablePlotMenu = true;
            this.dgvData.Location = new System.Drawing.Point(0, 0);
            this.dgvData.Name = "dgvData";
            this.dgvData.ReadOnly = true;
            this.dgvData.RowHeadersVisible = false;
            this.dgvData.ShowErrorMsg = true;
            this.dgvData.ShowPlotMenu = false;
            this.dgvData.Size = new System.Drawing.Size(500, 221);
            this.dgvData.StartPlotAtZero = false;
            this.dgvData.TabIndex = 0;
            this.dgvData.XColName = "Default";
            // 
            // btnDoubleColumn
            // 
            this.btnDoubleColumn.BackColor = System.Drawing.Color.White;
            this.btnDoubleColumn.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnDoubleColumn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDoubleColumn.Image = global::UserControls.Properties.Resources.Double_column;
            this.btnDoubleColumn.Location = new System.Drawing.Point(53, 5);
            this.btnDoubleColumn.Name = "btnDoubleColumn";
            this.btnDoubleColumn.Size = new System.Drawing.Size(25, 25);
            this.btnDoubleColumn.TabIndex = 23;
            this.btnDoubleColumn.UseVisualStyleBackColor = false;
            this.btnDoubleColumn.Click += new System.EventHandler(this.btnDoubleColumn_Click);
            // 
            // btnSigleColumn
            // 
            this.btnSigleColumn.BackColor = System.Drawing.Color.White;
            this.btnSigleColumn.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnSigleColumn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSigleColumn.Image = global::UserControls.Properties.Resources.Single_column;
            this.btnSigleColumn.Location = new System.Drawing.Point(28, 5);
            this.btnSigleColumn.Name = "btnSigleColumn";
            this.btnSigleColumn.Size = new System.Drawing.Size(25, 25);
            this.btnSigleColumn.TabIndex = 22;
            this.btnSigleColumn.UseVisualStyleBackColor = false;
            this.btnSigleColumn.Click += new System.EventHandler(this.btnSigleColumn_Click);
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
            this.btnClose.Location = new System.Drawing.Point(470, 5);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(25, 25);
            this.btnClose.TabIndex = 1;
            this.btnClose.UseVisualStyleBackColor = false;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            this.btnClose.MouseEnter += new System.EventHandler(this.btnClose_MouseEnter);
            this.btnClose.MouseLeave += new System.EventHandler(this.btnClose_MouseLeave);
            // 
            // btnSaveAs
            // 
            this.btnSaveAs.BackColor = System.Drawing.Color.White;
            this.btnSaveAs.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
            this.btnSaveAs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveAs.Image = global::UserControls.Properties.Resources.Save_as;
            this.btnSaveAs.Location = new System.Drawing.Point(3, 5);
            this.btnSaveAs.Name = "btnSaveAs";
            this.btnSaveAs.Size = new System.Drawing.Size(25, 25);
            this.btnSaveAs.TabIndex = 29;
            this.btnSaveAs.UseVisualStyleBackColor = false;
            this.btnSaveAs.Click += new System.EventHandler(this.btnSaveAs_Click);
            // 
            // XYPlotPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.panHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "XYPlotPanel";
            this.Size = new System.Drawing.Size(500, 600);
            this.panHeader.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolTip ttText;
        private System.Windows.Forms.Panel panHeader;
        private System.Windows.Forms.Label labTitle;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.TableLayoutPanel tlpPlots;
        private UserControls.DataGridViewCopyPaste dgvData;
        private MenuStyleButton btnSigleColumn;
        private MenuStyleButton btnDoubleColumn;
        private MenuStyleButton btnSaveAs;
    }
}
