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
    partial class FrmSelectEntity
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
            this.dgvNames = new System.Windows.Forms.DataGridView();
            this.colName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnContinue = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbHighlight = new System.Windows.Forms.CheckBox();
            this.gbMore = new System.Windows.Forms.GroupBox();
            this.cbFilterByCoordinate = new System.Windows.Forms.CheckBox();
            this.lCoordAxis = new System.Windows.Forms.Label();
            this.cbCoordAxis = new System.Windows.Forms.ComboBox();
            this.lCoordLimit = new System.Windows.Forms.Label();
            this.nudCoordLimit = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.dgvNames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoordLimit)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.gbMore.SuspendLayout();
            this.SuspendLayout();
            //
            // dgvNames
            //
            this.dgvNames.AllowUserToAddRows = false;
            this.dgvNames.AllowUserToDeleteRows = false;
            this.dgvNames.AllowUserToResizeRows = false;
            this.dgvNames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvNames.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvNames.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colName});
            this.dgvNames.Location = new System.Drawing.Point(6, 37);
            this.dgvNames.Name = "dgvNames";
            this.dgvNames.RowHeadersVisible = false;
            this.dgvNames.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvNames.Size = new System.Drawing.Size(296, 216);
            this.dgvNames.TabIndex = 0;
            this.dgvNames.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvNames_CellDoubleClick);
            this.dgvNames.SelectionChanged += new System.EventHandler(this.dgvNames_SelectionChanged);
            //
            // colName
            //
            this.colName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.colName.HeaderText = "Name";
            this.colName.Name = "colName";
            this.colName.ReadOnly = true;
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnCancel.Location = new System.Drawing.Point(239, 376);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // btnContinue
            //
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnContinue.Location = new System.Drawing.Point(158, 376);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(75, 23);
            this.btnContinue.TabIndex = 2;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.cbHighlight);
            this.groupBox1.Controls.Add(this.gbMore);
            this.groupBox1.Controls.Add(this.dgvNames);
            this.groupBox1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(310, 358);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Available entities";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(163, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "Select one or multiple entities";
            //
            // cbHighlight
            //
            this.cbHighlight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbHighlight.AutoSize = true;
            this.cbHighlight.Checked = true;
            this.cbHighlight.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbHighlight.Enabled = false;
            this.cbHighlight.Location = new System.Drawing.Point(6, 331);
            this.cbHighlight.Name = "cbHighlight";
            this.cbHighlight.Size = new System.Drawing.Size(188, 19);
            this.cbHighlight.TabIndex = 1;
            this.cbHighlight.Text = "Highlight selection in viewport";
            this.cbHighlight.UseVisualStyleBackColor = true;
            this.cbHighlight.CheckedChanged += new System.EventHandler(this.cbHighlight_CheckedChanged);
            //
            // gbMore
            //
            this.gbMore.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbMore.Controls.Add(this.cbFilterByCoordinate);
            this.gbMore.Controls.Add(this.lCoordAxis);
            this.gbMore.Controls.Add(this.cbCoordAxis);
            this.gbMore.Controls.Add(this.lCoordLimit);
            this.gbMore.Controls.Add(this.nudCoordLimit);
            this.gbMore.Location = new System.Drawing.Point(6, 261);
            this.gbMore.Name = "gbMore";
            this.gbMore.Size = new System.Drawing.Size(298, 66);
            this.gbMore.TabIndex = 4;
            this.gbMore.TabStop = false;
            this.gbMore.Text = "More";
            this.gbMore.Visible = false;
            //
            // cbFilterByCoordinate
            //
            this.cbFilterByCoordinate.AutoSize = true;
            this.cbFilterByCoordinate.Location = new System.Drawing.Point(8, 15);
            this.cbFilterByCoordinate.Name = "cbFilterByCoordinate";
            this.cbFilterByCoordinate.Size = new System.Drawing.Size(130, 19);
            this.cbFilterByCoordinate.TabIndex = 0;
            this.cbFilterByCoordinate.Text = "Filter by coordinate";
            this.cbFilterByCoordinate.UseVisualStyleBackColor = true;
            this.cbFilterByCoordinate.CheckedChanged += new System.EventHandler(this.cbFilterByCoordinate_CheckedChanged);
            //
            // lCoordAxis
            //
            this.lCoordAxis.AutoSize = true;
            this.lCoordAxis.Location = new System.Drawing.Point(8, 43);
            this.lCoordAxis.Name = "lCoordAxis";
            this.lCoordAxis.Size = new System.Drawing.Size(32, 15);
            this.lCoordAxis.Text = "Axis:";
            this.lCoordAxis.Visible = false;
            //
            // cbCoordAxis
            //
            this.cbCoordAxis.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCoordAxis.FormattingEnabled = true;
            this.cbCoordAxis.Items.AddRange(new object[] { "Xmin", "Xmax", "Ymin", "Ymax", "Zmin", "Zmax" });
            this.cbCoordAxis.Location = new System.Drawing.Point(44, 40);
            this.cbCoordAxis.Name = "cbCoordAxis";
            this.cbCoordAxis.Size = new System.Drawing.Size(80, 23);
            this.cbCoordAxis.TabIndex = 1;
            this.cbCoordAxis.Visible = false;
            //
            // lCoordLimit
            //
            this.lCoordLimit.AutoSize = true;
            this.lCoordLimit.Location = new System.Drawing.Point(134, 43);
            this.lCoordLimit.Name = "lCoordLimit";
            this.lCoordLimit.Size = new System.Drawing.Size(37, 15);
            this.lCoordLimit.Text = "Limit:";
            this.lCoordLimit.Visible = false;
            //
            // nudCoordLimit
            //
            this.nudCoordLimit.DecimalPlaces = 2;
            this.nudCoordLimit.Increment = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudCoordLimit.Location = new System.Drawing.Point(176, 40);
            this.nudCoordLimit.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.nudCoordLimit.Minimum = new decimal(new int[] { 1000000, 0, 0, -2147483648 });
            this.nudCoordLimit.Name = "nudCoordLimit";
            this.nudCoordLimit.Size = new System.Drawing.Size(80, 23);
            this.nudCoordLimit.TabIndex = 2;
            this.nudCoordLimit.Value = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudCoordLimit.Visible = false;
            //
            // FrmSelectEntity
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(334, 411);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 450);
            this.Name = "FrmSelectEntity";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Entity selection";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmSelectEntity_FormClosing);
            this.VisibleChanged += new System.EventHandler(this.FrmSelectEntity_VisibleChanged);
            ((System.ComponentModel.ISupportInitialize)(this.dgvNames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoordLimit)).EndInit();
            this.gbMore.ResumeLayout(false);
            this.gbMore.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView dgvNames;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbHighlight;
        private System.Windows.Forms.DataGridViewTextBoxColumn colName;
        private System.Windows.Forms.GroupBox gbMore;
        private System.Windows.Forms.CheckBox cbFilterByCoordinate;
        private System.Windows.Forms.Label lCoordAxis;
        private System.Windows.Forms.ComboBox cbCoordAxis;
        private System.Windows.Forms.Label lCoordLimit;
        private System.Windows.Forms.NumericUpDown nudCoordLimit;
    }
}
