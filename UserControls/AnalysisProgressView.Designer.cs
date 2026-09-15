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
    partial class AnalysisProgressView
    {
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
            this._tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._statusTabControl = new System.Windows.Forms.TabControl();
            this._statusTabPage = new System.Windows.Forms.TabPage();
            this._statusGraph = new ZedGraph.ZedGraphControl();
            this._detailTabControl = new System.Windows.Forms.TabControl();
            this._convergenceTabPage = new System.Windows.Forms.TabPage();
            this._convergenceGraph = new ZedGraph.ZedGraphControl();
            this._energyTabPage = new System.Windows.Forms.TabPage();
            this._energyGraph = new ZedGraph.ZedGraphControl();
            this._tableLayoutPanel.SuspendLayout();
            this._statusTabControl.SuspendLayout();
            this._statusTabPage.SuspendLayout();
            this._detailTabControl.SuspendLayout();
            this._convergenceTabPage.SuspendLayout();
            this._energyTabPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tableLayoutPanel
            // 
            this._tableLayoutPanel.ColumnCount = 1;
            this._tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tableLayoutPanel.Controls.Add(this._statusTabControl, 0, 0);
            this._tableLayoutPanel.Controls.Add(this._detailTabControl, 0, 1);
            this._tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this._tableLayoutPanel.Name = "_tableLayoutPanel";
            this._tableLayoutPanel.RowCount = 2;
            this._tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayoutPanel.Size = new System.Drawing.Size(600, 300);
            this._tableLayoutPanel.TabIndex = 0;
            // 
            // _statusTabControl
            // 
            this._statusTabControl.Controls.Add(this._statusTabPage);
            this._statusTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusTabControl.Location = new System.Drawing.Point(3, 3);
            this._statusTabControl.Margin = new System.Windows.Forms.Padding(3, 3, 3, 2);
            this._statusTabControl.Name = "_statusTabControl";
            this._statusTabControl.SelectedIndex = 0;
            this._statusTabControl.Size = new System.Drawing.Size(594, 145);
            this._statusTabControl.TabIndex = 0;
            // 
            // _statusTabPage
            // 
            this._statusTabPage.Controls.Add(this._statusGraph);
            this._statusTabPage.Location = new System.Drawing.Point(4, 22);
            this._statusTabPage.Name = "_statusTabPage";
            this._statusTabPage.Padding = new System.Windows.Forms.Padding(3);
            this._statusTabPage.Size = new System.Drawing.Size(586, 119);
            this._statusTabPage.TabIndex = 0;
            this._statusTabPage.Text = "Status";
            this._statusTabPage.UseVisualStyleBackColor = true;
            // 
            // _statusGraph
            // 
            this._statusGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusGraph.IsShowPointValues = true;
            this._statusGraph.Location = new System.Drawing.Point(3, 3);
            this._statusGraph.Margin = new System.Windows.Forms.Padding(0);
            this._statusGraph.Name = "_statusGraph";
            this._statusGraph.ScrollGrace = 0D;
            this._statusGraph.ScrollMaxX = 0D;
            this._statusGraph.ScrollMaxY = 0D;
            this._statusGraph.ScrollMaxY2 = 0D;
            this._statusGraph.ScrollMinX = 0D;
            this._statusGraph.ScrollMinY = 0D;
            this._statusGraph.ScrollMinY2 = 0D;
            this._statusGraph.Size = new System.Drawing.Size(580, 113);
            this._statusGraph.TabIndex = 0;
            this._statusGraph.UseExtendedPrintDialog = true;
            // 
            // _detailTabControl
            // 
            this._detailTabControl.Controls.Add(this._convergenceTabPage);
            this._detailTabControl.Controls.Add(this._energyTabPage);
            this._detailTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this._detailTabControl.Location = new System.Drawing.Point(3, 152);
            this._detailTabControl.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this._detailTabControl.Name = "_detailTabControl";
            this._detailTabControl.SelectedIndex = 0;
            this._detailTabControl.Size = new System.Drawing.Size(594, 145);
            this._detailTabControl.TabIndex = 1;
            // 
            // _convergenceTabPage
            // 
            this._convergenceTabPage.Controls.Add(this._convergenceGraph);
            this._convergenceTabPage.Location = new System.Drawing.Point(4, 22);
            this._convergenceTabPage.Name = "_convergenceTabPage";
            this._convergenceTabPage.Padding = new System.Windows.Forms.Padding(3);
            this._convergenceTabPage.Size = new System.Drawing.Size(586, 119);
            this._convergenceTabPage.TabIndex = 0;
            this._convergenceTabPage.Text = "Convergence";
            this._convergenceTabPage.UseVisualStyleBackColor = true;
            // 
            // _convergenceGraph
            // 
            this._convergenceGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this._convergenceGraph.IsShowPointValues = true;
            this._convergenceGraph.Location = new System.Drawing.Point(3, 3);
            this._convergenceGraph.Margin = new System.Windows.Forms.Padding(0);
            this._convergenceGraph.Name = "_convergenceGraph";
            this._convergenceGraph.ScrollGrace = 0D;
            this._convergenceGraph.ScrollMaxX = 0D;
            this._convergenceGraph.ScrollMaxY = 0D;
            this._convergenceGraph.ScrollMaxY2 = 0D;
            this._convergenceGraph.ScrollMinX = 0D;
            this._convergenceGraph.ScrollMinY = 0D;
            this._convergenceGraph.ScrollMinY2 = 0D;
            this._convergenceGraph.Size = new System.Drawing.Size(580, 113);
            this._convergenceGraph.TabIndex = 0;
            this._convergenceGraph.UseExtendedPrintDialog = true;
            // 
            // _energyTabPage
            // 
            this._energyTabPage.Controls.Add(this._energyGraph);
            this._energyTabPage.Location = new System.Drawing.Point(4, 22);
            this._energyTabPage.Name = "_energyTabPage";
            this._energyTabPage.Padding = new System.Windows.Forms.Padding(3);
            this._energyTabPage.Size = new System.Drawing.Size(586, 119);
            this._energyTabPage.TabIndex = 1;
            this._energyTabPage.Text = "Energy";
            this._energyTabPage.UseVisualStyleBackColor = true;
            // 
            // _energyGraph
            // 
            this._energyGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this._energyGraph.IsShowPointValues = true;
            this._energyGraph.Location = new System.Drawing.Point(3, 3);
            this._energyGraph.Margin = new System.Windows.Forms.Padding(0);
            this._energyGraph.Name = "_energyGraph";
            this._energyGraph.ScrollGrace = 0D;
            this._energyGraph.ScrollMaxX = 0D;
            this._energyGraph.ScrollMaxY = 0D;
            this._energyGraph.ScrollMaxY2 = 0D;
            this._energyGraph.ScrollMinX = 0D;
            this._energyGraph.ScrollMinY = 0D;
            this._energyGraph.ScrollMinY2 = 0D;
            this._energyGraph.Size = new System.Drawing.Size(580, 113);
            this._energyGraph.TabIndex = 0;
            this._energyGraph.UseExtendedPrintDialog = true;
            // 
            // AnalysisProgressView
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._tableLayoutPanel);
            this.Name = "AnalysisProgressView";
            this.Size = new System.Drawing.Size(600, 300);
            this._tableLayoutPanel.ResumeLayout(false);
            this._statusTabControl.ResumeLayout(false);
            this._statusTabPage.ResumeLayout(false);
            this._detailTabControl.ResumeLayout(false);
            this._convergenceTabPage.ResumeLayout(false);
            this._energyTabPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tableLayoutPanel;
        private System.Windows.Forms.TabControl _statusTabControl;
        private System.Windows.Forms.TabPage _statusTabPage;
        private ZedGraph.ZedGraphControl _statusGraph;
        private System.Windows.Forms.TabControl _detailTabControl;
        private System.Windows.Forms.TabPage _convergenceTabPage;
        private ZedGraph.ZedGraphControl _convergenceGraph;
        private System.Windows.Forms.TabPage _energyTabPage;
        private ZedGraph.ZedGraphControl _energyGraph;
        private System.ComponentModel.IContainer components;
    }
}
