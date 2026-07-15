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
    partial class AnnotationEditor
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
            this.aertbData = new UserControls.AnnotationEditorRichTextBox();
            this.pBorder.SuspendLayout();
            this.SuspendLayout();
            // 
            // pBorder
            // 
            this.pBorder.BackColor = System.Drawing.SystemColors.Highlight;
            this.pBorder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pBorder.Controls.Add(this.aertbData);
            this.pBorder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pBorder.Location = new System.Drawing.Point(0, 0);
            this.pBorder.Name = "pBorder";
            this.pBorder.Size = new System.Drawing.Size(119, 60);
            this.pBorder.TabIndex = 0;
            // 
            // aertbData
            // 
            this.aertbData.Beep = false;
            this.aertbData.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.aertbData.DetectUrls = false;
            this.aertbData.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.aertbData.Location = new System.Drawing.Point(2, 2);
            this.aertbData.MaxSize = new System.Drawing.Size(0, 0);
            this.aertbData.MinSize = new System.Drawing.Size(0, 0);
            this.aertbData.Name = "aertbData";
            this.aertbData.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.aertbData.Size = new System.Drawing.Size(98, 40);
            this.aertbData.TabIndex = 6;
            this.aertbData.Text = "Edit widgets";
            this.aertbData.WordWrap = false;
            this.aertbData.SizeChanged += new System.EventHandler(this.aertbData_SizeChanged);
            // 
            // AnnotationEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pBorder);
            this.Name = "AnnotationEditor";
            this.Size = new System.Drawing.Size(119, 60);
            this.pBorder.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pBorder;
        private AnnotationEditorRichTextBox aertbData;
    }
}
