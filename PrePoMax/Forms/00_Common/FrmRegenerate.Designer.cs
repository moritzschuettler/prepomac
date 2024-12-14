namespace PrePoMax.Forms
{
    partial class FrmRegenerate
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
            this.gbData = new System.Windows.Forms.GroupBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.rbAll = new System.Windows.Forms.RadioButton();
            this.rbFEModel = new System.Windows.Forms.RadioButton();
            this.gbData.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbData
            // 
            this.gbData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbData.Controls.Add(this.rbFEModel);
            this.gbData.Controls.Add(this.rbAll);
            this.gbData.Location = new System.Drawing.Point(12, 12);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(183, 72);
            this.gbData.TabIndex = 0;
            this.gbData.TabStop = false;
            this.gbData.Text = "Type";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(33, 90);
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
            this.btnCancel.Location = new System.Drawing.Point(114, 90);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 12;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // rbAll
            // 
            this.rbAll.AutoSize = true;
            this.rbAll.Checked = true;
            this.rbAll.Location = new System.Drawing.Point(6, 22);
            this.rbAll.Name = "rbAll";
            this.rbAll.Size = new System.Drawing.Size(99, 19);
            this.rbAll.TabIndex = 0;
            this.rbAll.TabStop = true;
            this.rbAll.Text = "Regenerate all";
            this.rbAll.UseVisualStyleBackColor = true;
            // 
            // rbFEModel
            // 
            this.rbFEModel.AutoSize = true;
            this.rbFEModel.Location = new System.Drawing.Point(6, 47);
            this.rbFEModel.Name = "rbFEModel";
            this.rbFEModel.Size = new System.Drawing.Size(136, 19);
            this.rbFEModel.TabIndex = 1;
            this.rbFEModel.TabStop = true;
            this.rbFEModel.Text = "Regenerate FE model";
            this.rbFEModel.UseVisualStyleBackColor = true;
            // 
            // FrmRegenerate
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(208, 125);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.gbData);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(224, 164);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(224, 164);
            this.Name = "FrmRegenerate";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Regenerate History";
            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbData;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.RadioButton rbFEModel;
        private System.Windows.Forms.RadioButton rbAll;
    }
}