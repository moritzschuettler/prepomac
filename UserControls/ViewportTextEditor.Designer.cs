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
    partial class ViewportTextEditor
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
            this.pBorder = new System.Windows.Forms.Panel();
            this.vertbData = new UserControls.ViewportEditorRichTextBox();
            this.pBorder.SuspendLayout();
            this.SuspendLayout();
            // 
            // pBorder
            // 
            this.pBorder.BackColor = System.Drawing.SystemColors.Highlight;
            this.pBorder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pBorder.Controls.Add(this.vertbData);
            this.pBorder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pBorder.Location = new System.Drawing.Point(0, 0);
            this.pBorder.Name = "pBorder";
            this.pBorder.Size = new System.Drawing.Size(119, 60);
            this.pBorder.TabIndex = 0;
            // 
            // vertbData
            // 
            this.vertbData.Beep = false;
            this.vertbData.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.vertbData.DetectUrls = false;
            this.vertbData.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.vertbData.Location = new System.Drawing.Point(2, 2);
            this.vertbData.MaxSize = new System.Drawing.Size(0, 0);
            this.vertbData.MinSize = new System.Drawing.Size(0, 0);
            this.vertbData.Name = "vertbData";
            this.vertbData.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.vertbData.Size = new System.Drawing.Size(98, 40);
            this.vertbData.TabIndex = 6;
            this.vertbData.Text = "Edit widgets";
            this.vertbData.WordWrap = false;
            this.vertbData.SizeChanged += new System.EventHandler(this.vertbData_SizeChanged);
            // 
            // ViewportTextEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pBorder);
            this.Name = "ViewportTextEditor";
            this.Size = new System.Drawing.Size(119, 60);
            this.pBorder.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pBorder;
        private ViewportEditorRichTextBox vertbData;
    }
}
