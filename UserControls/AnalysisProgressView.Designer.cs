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
        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this._statusGraph = new ZedGraph.ZedGraphControl();
            this._convergenceGraph = new ZedGraph.ZedGraphControl();
            this._tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _tableLayoutPanel
            // 
            this._tableLayoutPanel.ColumnCount = 1;
            this._tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent,
                                                                                         100F));
            this._tableLayoutPanel.Controls.Add(this._statusGraph, 0, 0);
            this._tableLayoutPanel.Controls.Add(this._convergenceGraph, 0, 1);
            this._tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this._tableLayoutPanel.Name = "tableLayoutPanelProgress";
            this._tableLayoutPanel.RowCount = 2;
            this._tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent,
                                                                                   50F));
            this._tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent,
                                                                                   50F));
            this._tableLayoutPanel.Size = new System.Drawing.Size(600, 300);
            this._tableLayoutPanel.TabIndex = 0;
            // 
            // _statusGraph
            // 
            this._statusGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this._statusGraph.IsShowPointValues = true;
            this._statusGraph.Location = new System.Drawing.Point(3, 3);
            this._statusGraph.Margin = new System.Windows.Forms.Padding(3, 3, 3, 2);
            this._statusGraph.Name = "zedGraphStatus";
            this._statusGraph.Size = new System.Drawing.Size(594, 145);
            this._statusGraph.TabIndex = 0;
            this._statusGraph.UseExtendedPrintDialog = true;
            // 
            // _convergenceGraph
            // 
            this._convergenceGraph.Dock = System.Windows.Forms.DockStyle.Fill;
            this._convergenceGraph.IsShowPointValues = true;
            this._convergenceGraph.Location = new System.Drawing.Point(3, 152);
            this._convergenceGraph.Margin = new System.Windows.Forms.Padding(3, 2, 3, 3);
            this._convergenceGraph.Name = "zedGraphConvergence";
            this._convergenceGraph.Size = new System.Drawing.Size(594, 145);
            this._convergenceGraph.TabIndex = 1;
            this._convergenceGraph.UseExtendedPrintDialog = true;
            // 
            // AnalysisProgressView
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._tableLayoutPanel);
            this.Name = "AnalysisProgressView";
            this.Size = new System.Drawing.Size(600, 300);
            this._tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel _tableLayoutPanel;
        private ZedGraph.ZedGraphControl _statusGraph;
        private ZedGraph.ZedGraphControl _convergenceGraph;
    }
}
