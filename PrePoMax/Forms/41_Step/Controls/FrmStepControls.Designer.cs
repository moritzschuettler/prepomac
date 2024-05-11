namespace PrePoMax.Forms
{
    partial class FrmStepControls
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.TreeNode treeNode5 = new System.Windows.Forms.TreeNode("Reset");
            System.Windows.Forms.TreeNode treeNode6 = new System.Windows.Forms.TreeNode("Time Incrementation");
            System.Windows.Forms.TreeNode treeNode7 = new System.Windows.Forms.TreeNode("Field");
            System.Windows.Forms.TreeNode treeNode8 = new System.Windows.Forms.TreeNode("Contact");
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.tvProperties = new System.Windows.Forms.TreeView();
            this.propertyGrid = new UserControls.TabEnabledPropertyGrid();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbProperties = new System.Windows.Forms.GroupBox();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.tcProperties = new System.Windows.Forms.TabControl();
            this.tpProperties = new System.Windows.Forms.TabPage();
            this.tpDataPoints = new System.Windows.Forms.TabPage();
            this.dgvData = new UserControls.DataGridViewCopyPaste();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.lvAddedProperties = new UserControls.ListViewWithSelection();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.cmsPropertyGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tsmiResetAll = new System.Windows.Forms.ToolStripMenuItem();
            this.gbProperties.SuspendLayout();
            this.tcProperties.SuspendLayout();
            this.tpProperties.SuspendLayout();
            this.tpDataPoints.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).BeginInit();
            this.cmsPropertyGrid.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvProperties
            // 
            this.tvProperties.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.tvProperties.Location = new System.Drawing.Point(5, 38);
            this.tvProperties.Name = "tvProperties";
            treeNode5.Name = "Reset";
            treeNode5.Text = "Reset";
            treeNode5.ToolTipText = "Reset";
            treeNode6.Name = "Time Incrementation";
            treeNode6.Text = "Time Incrementation";
            treeNode6.ToolTipText = "Time Incrementation";
            treeNode7.Name = "Field";
            treeNode7.Text = "Field";
            treeNode7.ToolTipText = "Field";
            treeNode8.Name = "Contact";
            treeNode8.Text = "Contact";
            treeNode8.ToolTipText = "Contact";
            this.tvProperties.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode5,
            treeNode6,
            treeNode7,
            treeNode8});
            this.tvProperties.Size = new System.Drawing.Size(144, 101);
            this.tvProperties.TabIndex = 2;
            this.tvProperties.DoubleClick += new System.EventHandler(this.tvProperties_DoubleClick);
            // 
            // propertyGrid
            // 
            this.propertyGrid.ContextMenuStrip = this.cmsPropertyGrid;
            this.propertyGrid.DisabledItemForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.propertyGrid.LineColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.Location = new System.Drawing.Point(3, 3);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.ReadOnly = false;
            this.propertyGrid.Size = new System.Drawing.Size(339, 315);
            this.propertyGrid.TabIndex = 6;
            this.propertyGrid.ToolbarVisible = false;
            this.propertyGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.propertyGrid_PropertyValueChanged);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(210, 512);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 8;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(291, 512);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // gbProperties
            // 
            this.gbProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbProperties.Controls.Add(this.btnMoveDown);
            this.gbProperties.Controls.Add(this.btnMoveUp);
            this.gbProperties.Controls.Add(this.tcProperties);
            this.gbProperties.Controls.Add(this.label2);
            this.gbProperties.Controls.Add(this.label1);
            this.gbProperties.Controls.Add(this.lvAddedProperties);
            this.gbProperties.Controls.Add(this.btnAdd);
            this.gbProperties.Controls.Add(this.btnRemove);
            this.gbProperties.Controls.Add(this.tvProperties);
            this.gbProperties.Location = new System.Drawing.Point(12, 12);
            this.gbProperties.Name = "gbProperties";
            this.gbProperties.Size = new System.Drawing.Size(360, 494);
            this.gbProperties.TabIndex = 0;
            this.gbProperties.TabStop = false;
            this.gbProperties.Text = "Controls";
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveDown.Image = global::PrePoMax.Properties.Resources.Down_arrow;
            this.btnMoveDown.Location = new System.Drawing.Point(331, 68);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(24, 24);
            this.btnMoveDown.TabIndex = 16;
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveUp.Image = global::PrePoMax.Properties.Resources.Up_arrow;
            this.btnMoveUp.Location = new System.Drawing.Point(331, 38);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(24, 24);
            this.btnMoveUp.TabIndex = 15;
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // tcProperties
            // 
            this.tcProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tcProperties.Controls.Add(this.tpProperties);
            this.tcProperties.Controls.Add(this.tpDataPoints);
            this.tcProperties.Location = new System.Drawing.Point(6, 142);
            this.tcProperties.Margin = new System.Windows.Forms.Padding(0);
            this.tcProperties.Name = "tcProperties";
            this.tcProperties.SelectedIndex = 0;
            this.tcProperties.Size = new System.Drawing.Size(353, 349);
            this.tcProperties.TabIndex = 11;
            // 
            // tpProperties
            // 
            this.tpProperties.BackColor = System.Drawing.SystemColors.Control;
            this.tpProperties.Controls.Add(this.propertyGrid);
            this.tpProperties.Location = new System.Drawing.Point(4, 24);
            this.tpProperties.Name = "tpProperties";
            this.tpProperties.Padding = new System.Windows.Forms.Padding(3);
            this.tpProperties.Size = new System.Drawing.Size(345, 321);
            this.tpProperties.TabIndex = 0;
            this.tpProperties.Text = "Properties";
            // 
            // tpDataPoints
            // 
            this.tpDataPoints.BackColor = System.Drawing.SystemColors.Control;
            this.tpDataPoints.Controls.Add(this.dgvData);
            this.tpDataPoints.Location = new System.Drawing.Point(4, 24);
            this.tpDataPoints.Name = "tpDataPoints";
            this.tpDataPoints.Padding = new System.Windows.Forms.Padding(3);
            this.tpDataPoints.Size = new System.Drawing.Size(345, 321);
            this.tpDataPoints.TabIndex = 1;
            this.tpDataPoints.Text = "Data points";
            // 
            // dgvData
            // 
            this.dgvData.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvData.DefaultCellStyle = dataGridViewCellStyle2;
            this.dgvData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvData.EnableCutMenu = true;
            this.dgvData.EnablePasteMenu = true;
            this.dgvData.EnablePlotMenu = true;
            this.dgvData.Location = new System.Drawing.Point(3, 3);
            this.dgvData.Name = "dgvData";
            this.dgvData.ShowErrorMsg = true;
            this.dgvData.Size = new System.Drawing.Size(339, 315);
            this.dgvData.StartPlotAtZero = false;
            this.dgvData.TabIndex = 0;
            this.dgvData.XColIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(183, 19);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(51, 15);
            this.label2.TabIndex = 8;
            this.label2.Text = "Selected";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(55, 15);
            this.label1.TabIndex = 7;
            this.label1.Text = "Available";
            // 
            // lvAddedProperties
            // 
            this.lvAddedProperties.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvAddedProperties.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName});
            this.lvAddedProperties.DisableMouse = false;
            this.lvAddedProperties.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lvAddedProperties.FullRowSelect = true;
            this.lvAddedProperties.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvAddedProperties.HideSelection = false;
            this.lvAddedProperties.Location = new System.Drawing.Point(181, 38);
            this.lvAddedProperties.MultiSelect = false;
            this.lvAddedProperties.Name = "lvAddedProperties";
            this.lvAddedProperties.ShowGroups = false;
            this.lvAddedProperties.Size = new System.Drawing.Size(146, 101);
            this.lvAddedProperties.TabIndex = 5;
            this.lvAddedProperties.UseCompatibleStateImageBehavior = false;
            this.lvAddedProperties.View = System.Windows.Forms.View.Details;
            this.lvAddedProperties.SelectedIndexChanged += new System.EventHandler(this.lvAddedProperties_SelectedIndexChanged);
            // 
            // colName
            // 
            this.colName.Width = 25;
            // 
            // btnAdd
            // 
            this.btnAdd.Image = global::PrePoMax.Properties.Resources.Right_arrow;
            this.btnAdd.Location = new System.Drawing.Point(153, 38);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(24, 24);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Image = global::PrePoMax.Properties.Resources.Remove;
            this.btnRemove.Location = new System.Drawing.Point(331, 98);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(24, 24);
            this.btnRemove.TabIndex = 4;
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // cmsPropertyGrid
            // 
            this.cmsPropertyGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiResetAll});
            this.cmsPropertyGrid.Name = "cmsPropertyGrid";
            this.cmsPropertyGrid.Size = new System.Drawing.Size(181, 48);
            // 
            // tsmiResetAll
            // 
            this.tsmiResetAll.Name = "tsmiResetAll";
            this.tsmiResetAll.Size = new System.Drawing.Size(180, 22);
            this.tsmiResetAll.Text = "Reset all";
            this.tsmiResetAll.Click += new System.EventHandler(this.tsmiResetAll_Click);
            // 
            // FrmStepControls
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 546);
            this.Controls.Add(this.gbProperties);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(350, 530);
            this.Name = "FrmStepControls";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Edit Step Controls";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmSurfaceInteraction_FormClosing);
            this.gbProperties.ResumeLayout(false);
            this.gbProperties.PerformLayout();
            this.tcProperties.ResumeLayout(false);
            this.tpProperties.ResumeLayout(false);
            this.tpDataPoints.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvData)).EndInit();
            this.cmsPropertyGrid.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView tvProperties;
        private UserControls.TabEnabledPropertyGrid propertyGrid;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox gbProperties;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private UserControls.ListViewWithSelection lvAddedProperties;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TabControl tcProperties;
        private System.Windows.Forms.TabPage tpProperties;
        private System.Windows.Forms.TabPage tpDataPoints;
        private UserControls.DataGridViewCopyPaste dgvData;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.ContextMenuStrip cmsPropertyGrid;
        private System.Windows.Forms.ToolStripMenuItem tsmiResetAll;
    }
}