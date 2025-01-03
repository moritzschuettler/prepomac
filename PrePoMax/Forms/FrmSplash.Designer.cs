namespace PrePoMax.Forms
{
    partial class FrmSplash
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSplash));
            this.labProgramName = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.panel1 = new System.Windows.Forms.Panel();
            this.llHomePage = new System.Windows.Forms.LinkLabel();
            this.llClose = new System.Windows.Forms.LinkLabel();
            this.lHelp = new System.Windows.Forms.Label();
            this.lSponsors = new System.Windows.Forms.Label();
            this.llKimm = new System.Windows.Forms.LinkLabel();
            this.llSmartDo = new System.Windows.Forms.LinkLabel();
            this.lCopyRight = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labProgramName
            // 
            this.labProgramName.BackColor = System.Drawing.Color.Transparent;
            this.labProgramName.Font = new System.Drawing.Font("Segoe UI Black", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.labProgramName.ForeColor = System.Drawing.Color.Black;
            this.labProgramName.Location = new System.Drawing.Point(41, -1);
            this.labProgramName.Name = "labProgramName";
            this.labProgramName.Size = new System.Drawing.Size(547, 44);
            this.labProgramName.TabIndex = 0;
            this.labProgramName.Text = "PrePoMax v0.0.0";
            this.labProgramName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(0, 265);
            this.progressBar.MarqueeAnimationSpeed = 20;
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(600, 15);
            this.progressBar.Step = 5;
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 1;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Transparent;
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.Location = new System.Drawing.Point(12, 10);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(32, 32);
            this.panel1.TabIndex = 2;
            // 
            // llHomePage
            // 
            this.llHomePage.AutoSize = true;
            this.llHomePage.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.llHomePage.Location = new System.Drawing.Point(3, 200);
            this.llHomePage.Name = "llHomePage";
            this.llHomePage.Size = new System.Drawing.Size(74, 20);
            this.llHomePage.TabIndex = 4;
            this.llHomePage.TabStop = true;
            this.llHomePage.Text = "PrePoMax";
            this.llHomePage.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llHomePage_LinkClicked);
            // 
            // llClose
            // 
            this.llClose.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.llClose.Location = new System.Drawing.Point(554, 356);
            this.llClose.Name = "llClose";
            this.llClose.Size = new System.Drawing.Size(45, 20);
            this.llClose.TabIndex = 5;
            this.llClose.TabStop = true;
            this.llClose.Text = "Close";
            this.llClose.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.llClose.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llClose_LinkClicked);
            // 
            // lHelp
            // 
            this.lHelp.BackColor = System.Drawing.Color.Transparent;
            this.lHelp.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lHelp.Location = new System.Drawing.Point(3, 200);
            this.lHelp.Name = "lHelp";
            this.lHelp.Size = new System.Drawing.Size(247, 62);
            this.lHelp.TabIndex = 6;
            this.lHelp.Text = "PrePoMax is a graphical pre and post-processor for the free CalculiX FEM solver o" +
    "n Windows platform.";
            // 
            // lSponsors
            // 
            this.lSponsors.BackColor = System.Drawing.Color.Transparent;
            this.lSponsors.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.lSponsors.Location = new System.Drawing.Point(3, 283);
            this.lSponsors.Name = "lSponsors";
            this.lSponsors.Size = new System.Drawing.Size(585, 33);
            this.lSponsors.TabIndex = 7;
            this.lSponsors.Text = "Sponsors";
            this.lSponsors.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // llKimm
            // 
            this.llKimm.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.llKimm.Location = new System.Drawing.Point(4, 316);
            this.llKimm.Name = "llKimm";
            this.llKimm.Size = new System.Drawing.Size(584, 20);
            this.llKimm.TabIndex = 8;
            this.llKimm.TabStop = true;
            this.llKimm.Text = "KIMM - Korean Institute of Machinery && Materials";
            this.llKimm.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.llKimm.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llKimm_LinkClicked);
            // 
            // llSmartDo
            // 
            this.llSmartDo.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.llSmartDo.Location = new System.Drawing.Point(4, 336);
            this.llSmartDo.Name = "llSmartDo";
            this.llSmartDo.Size = new System.Drawing.Size(584, 20);
            this.llSmartDo.TabIndex = 9;
            this.llSmartDo.TabStop = true;
            this.llSmartDo.Text = "SmartDO - Smart Design Optimization";
            this.llSmartDo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.llSmartDo.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llSmartDo_LinkClicked);
            // 
            // lCopyRight
            // 
            this.lCopyRight.BackColor = System.Drawing.Color.Transparent;
            this.lCopyRight.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lCopyRight.Location = new System.Drawing.Point(310, 51);
            this.lCopyRight.Name = "lCopyRight";
            this.lCopyRight.Size = new System.Drawing.Size(289, 20);
            this.lCopyRight.TabIndex = 10;
            this.lCopyRight.Text = "Copyright © 2016-2025 Matej Borovinšek";
            this.lCopyRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // FrmSplash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.BackgroundImage = global::PrePoMax.Properties.Resources.Splash_02;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CancelButton = this.llClose;
            this.ClientSize = new System.Drawing.Size(600, 380);
            this.Controls.Add(this.lCopyRight);
            this.Controls.Add(this.llKimm);
            this.Controls.Add(this.lSponsors);
            this.Controls.Add(this.llClose);
            this.Controls.Add(this.llHomePage);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.labProgramName);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.llSmartDo);
            this.Controls.Add(this.lHelp);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximumSize = new System.Drawing.Size(600, 380);
            this.MinimumSize = new System.Drawing.Size(600, 380);
            this.Name = "FrmSplash";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmSplash";
            this.TransparencyKey = System.Drawing.Color.Magenta;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labProgramName;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.LinkLabel llHomePage;
        private System.Windows.Forms.LinkLabel llClose;
        private System.Windows.Forms.Label lHelp;
        private System.Windows.Forms.Label lSponsors;
        private System.Windows.Forms.LinkLabel llKimm;
        private System.Windows.Forms.LinkLabel llSmartDo;
        private System.Windows.Forms.Label lCopyRight;
    }
}