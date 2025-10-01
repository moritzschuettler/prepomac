namespace PrePoMax.Forms
{
    partial class FrmUserVew
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
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("1");
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("2");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("3");
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.gbViews = new System.Windows.Forms.GroupBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCreate = new System.Windows.Forms.Button();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lvViews = new UserControls.ListViewWithSelection();
            this.colName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.gbProperties = new UserControls.CollapsableGroupBox();
            this.pgProperties = new UserControls.TabEnabledPropertyGrid();
            this.gbViews.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(217, 581);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 14;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOK.Location = new System.Drawing.Point(136, 581);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 15;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // gbViews
            // 
            this.gbViews.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbViews.Controls.Add(this.btnUpdate);
            this.gbViews.Controls.Add(this.btnApply);
            this.gbViews.Controls.Add(this.btnCreate);
            this.gbViews.Controls.Add(this.btnMoveDown);
            this.gbViews.Controls.Add(this.btnMoveUp);
            this.gbViews.Controls.Add(this.btnRemove);
            this.gbViews.Controls.Add(this.lvViews);
            this.gbViews.Location = new System.Drawing.Point(12, 12);
            this.gbViews.Name = "gbViews";
            this.gbViews.Size = new System.Drawing.Size(280, 202);
            this.gbViews.TabIndex = 17;
            this.gbViews.TabStop = false;
            this.gbViews.Text = "Views";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdate.Location = new System.Drawing.Point(194, 82);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(80, 24);
            this.btnUpdate.TabIndex = 20;
            this.btnUpdate.Text = "Update";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Location = new System.Drawing.Point(194, 52);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(80, 24);
            this.btnApply.TabIndex = 19;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCreate
            // 
            this.btnCreate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCreate.Location = new System.Drawing.Point(194, 22);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(80, 24);
            this.btnCreate.TabIndex = 18;
            this.btnCreate.Text = "Create";
            this.btnCreate.UseVisualStyleBackColor = true;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveDown.Location = new System.Drawing.Point(194, 142);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(80, 24);
            this.btnMoveDown.TabIndex = 16;
            this.btnMoveDown.Text = "Move down";
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveUp.Location = new System.Drawing.Point(194, 112);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(80, 24);
            this.btnMoveUp.TabIndex = 15;
            this.btnMoveUp.Text = "Move up";
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(194, 172);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(80, 24);
            this.btnRemove.TabIndex = 14;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // lvViews
            // 
            this.lvViews.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvViews.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName});
            this.lvViews.DisableMouse = false;
            this.lvViews.FullRowSelect = true;
            this.lvViews.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.lvViews.HideSelection = false;
            this.lvViews.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3});
            this.lvViews.Location = new System.Drawing.Point(6, 22);
            this.lvViews.MouseOverColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(228)))), ((int)(((byte)(247)))));
            this.lvViews.MultiSelect = false;
            this.lvViews.Name = "lvViews";
            this.lvViews.ShowGroups = false;
            this.lvViews.Size = new System.Drawing.Size(182, 174);
            this.lvViews.TabIndex = 0;
            this.lvViews.UseCompatibleStateImageBehavior = false;
            this.lvViews.View = System.Windows.Forms.View.Details;
            this.lvViews.SelectedIndexChanged += new System.EventHandler(this.lvViews_SelectedIndexChanged);
            this.lvViews.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lvViews_MouseDoubleClick);
            // 
            // colName
            // 
            this.colName.Width = 27;
            // 
            // gbProperties
            // 
            this.gbProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbProperties.Controls.Add(this.pgProperties);
            this.gbProperties.IsCollapsed = false;
            this.gbProperties.Location = new System.Drawing.Point(12, 220);
            this.gbProperties.Name = "gbProperties";
            this.gbProperties.Size = new System.Drawing.Size(280, 355);
            this.gbProperties.TabIndex = 17;
            this.gbProperties.TabStop = false;
            this.gbProperties.Text = "Properties";
            this.gbProperties.OnCollapsedChanged += new UserControls.CollapsableGroupBox.CollapseChangeEventHandler(this.gbProperties_OnCollapsedChanged);
            // 
            // pgProperties
            // 
            this.pgProperties.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pgProperties.DisabledItemForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.pgProperties.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.pgProperties.LineColor = System.Drawing.SystemColors.Control;
            this.pgProperties.Location = new System.Drawing.Point(6, 17);
            this.pgProperties.Name = "pgProperties";
            this.pgProperties.ReadOnly = false;
            this.pgProperties.Size = new System.Drawing.Size(268, 332);
            this.pgProperties.TabIndex = 17;
            this.pgProperties.ToolbarVisible = false;
            this.pgProperties.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.pgProperties_PropertyValueChanged);
            // 
            // FrmUserVew
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(304, 616);
            this.Controls.Add(this.gbProperties);
            this.Controls.Add(this.gbViews);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(300, 300);
            this.Name = "FrmUserVew";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "User Views";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmFollowerView_FormClosing);
            this.Load += new System.EventHandler(this.FrmUserVew_Load);
            this.VisibleChanged += new System.EventHandler(this.FrmFollowerView_VisibleChanged);
            this.gbViews.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.GroupBox gbViews;
        private UserControls.ListViewWithSelection lvViews;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnApply;
        private UserControls.CollapsableGroupBox gbProperties;
        protected UserControls.TabEnabledPropertyGrid pgProperties;
        private System.Windows.Forms.ColumnHeader colName;
    }
}