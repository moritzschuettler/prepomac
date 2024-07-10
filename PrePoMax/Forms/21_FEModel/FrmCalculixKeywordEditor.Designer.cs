namespace PrePoMax.Forms
{
    partial class FrmCalculixKeywordEditor
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmCalculixKeywordEditor));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnAddKeyword = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.label2 = new System.Windows.Forms.Label();
            this.cltvKeywordsTree = new UserControls.CodersLabTreeView();
            this.btnMoveDown = new System.Windows.Forms.Button();
            this.btnMoveUp = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.cbHide = new System.Windows.Forms.CheckBox();
            this.fctbKeyword = new FastColoredTextBoxNS.FastColoredTextBox();
            this.fctbInpFile = new FastColoredTextBoxNS.FastColoredTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tUpdate = new System.Windows.Forms.Timer(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.fctbKeyword)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.fctbInpFile)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnCancel.Location = new System.Drawing.Point(944, 576);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnAddKeyword
            // 
            this.btnAddKeyword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAddKeyword.Location = new System.Drawing.Point(12, 576);
            this.btnAddKeyword.Name = "btnAddKeyword";
            this.btnAddKeyword.Size = new System.Drawing.Size(87, 23);
            this.btnAddKeyword.TabIndex = 9;
            this.btnAddKeyword.Text = "Add keyword";
            this.btnAddKeyword.UseVisualStyleBackColor = true;
            this.btnAddKeyword.Click += new System.EventHandler(this.btnAddKeyword_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnOK.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.btnOK.Location = new System.Drawing.Point(863, 576);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(12, 12);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            this.splitContainer1.Panel1.Controls.Add(this.label2);
            this.splitContainer1.Panel1.Controls.Add(this.cltvKeywordsTree);
            this.splitContainer1.Panel1.Controls.Add(this.btnMoveDown);
            this.splitContainer1.Panel1.Controls.Add(this.btnMoveUp);
            this.splitContainer1.Panel1.Controls.Add(this.btnDelete);
            this.splitContainer1.Panel1MinSize = 150;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.cbHide);
            this.splitContainer1.Panel2.Controls.Add(this.fctbKeyword);
            this.splitContainer1.Panel2.Controls.Add(this.fctbInpFile);
            this.splitContainer1.Panel2.Controls.Add(this.label3);
            this.splitContainer1.Panel2.Controls.Add(this.label1);
            this.splitContainer1.Panel2MinSize = 150;
            this.splitContainer1.Size = new System.Drawing.Size(1010, 558);
            this.splitContainer1.SplitterDistance = 469;
            this.splitContainer1.TabIndex = 0;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(126, 15);
            this.label2.TabIndex = 10;
            this.label2.Text = "CalculiX keywords tree";
            // 
            // cltvKeywordsTree
            // 
            this.cltvKeywordsTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cltvKeywordsTree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.cltvKeywordsTree.ChangeHighlightOnFocusLost = false;
            this.cltvKeywordsTree.DisableMouse = false;
            this.cltvKeywordsTree.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.cltvKeywordsTree.ForeColor = System.Drawing.SystemColors.WindowText;
            this.cltvKeywordsTree.HighlightForeErrorColor = System.Drawing.Color.Red;
            this.cltvKeywordsTree.Location = new System.Drawing.Point(3, 18);
            this.cltvKeywordsTree.Name = "cltvKeywordsTree";
            this.cltvKeywordsTree.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            this.cltvKeywordsTree.SelectionMode = UserControls.TreeViewSelectionMode.SingleSelect;
            this.cltvKeywordsTree.Size = new System.Drawing.Size(430, 537);
            this.cltvKeywordsTree.TabIndex = 2;
            this.cltvKeywordsTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.cltvKeywordsTree_AfterSelect);
            // 
            // btnMoveDown
            // 
            this.btnMoveDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveDown.Image = global::PrePoMax.Properties.Resources.Down_arrow;
            this.btnMoveDown.Location = new System.Drawing.Point(439, 51);
            this.btnMoveDown.Name = "btnMoveDown";
            this.btnMoveDown.Size = new System.Drawing.Size(27, 27);
            this.btnMoveDown.TabIndex = 7;
            this.btnMoveDown.UseVisualStyleBackColor = true;
            this.btnMoveDown.Click += new System.EventHandler(this.btnMoveDown_Click);
            // 
            // btnMoveUp
            // 
            this.btnMoveUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnMoveUp.Image = global::PrePoMax.Properties.Resources.Up_arrow;
            this.btnMoveUp.Location = new System.Drawing.Point(439, 18);
            this.btnMoveUp.Name = "btnMoveUp";
            this.btnMoveUp.Size = new System.Drawing.Size(27, 27);
            this.btnMoveUp.TabIndex = 5;
            this.btnMoveUp.UseVisualStyleBackColor = true;
            this.btnMoveUp.Click += new System.EventHandler(this.btnMoveUp_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Image = global::PrePoMax.Properties.Resources.Remove;
            this.btnDelete.Location = new System.Drawing.Point(439, 84);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(27, 27);
            this.btnDelete.TabIndex = 6;
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // cbHide
            // 
            this.cbHide.AutoSize = true;
            this.cbHide.Checked = true;
            this.cbHide.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbHide.Location = new System.Drawing.Point(166, 180);
            this.cbHide.Name = "cbHide";
            this.cbHide.Size = new System.Drawing.Size(193, 19);
            this.cbHide.TabIndex = 13;
            this.cbHide.Text = "Use hiding for faster operation";
            this.cbHide.UseVisualStyleBackColor = true;
            this.cbHide.CheckedChanged += new System.EventHandler(this.cbHide_CheckedChanged);
            // 
            // fctbKeyword
            // 
            this.fctbKeyword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fctbKeyword.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fctbKeyword.AutoScrollMinSize = new System.Drawing.Size(109, 14);
            this.fctbKeyword.BackBrush = null;
            this.fctbKeyword.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.fctbKeyword.CharHeight = 14;
            this.fctbKeyword.CharWidth = 7;
            this.fctbKeyword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fctbKeyword.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fctbKeyword.Font = new System.Drawing.Font("Consolas", 9F);
            this.fctbKeyword.IsReplaceMode = false;
            this.fctbKeyword.Location = new System.Drawing.Point(3, 18);
            this.fctbKeyword.Name = "fctbKeyword";
            this.fctbKeyword.Paddings = new System.Windows.Forms.Padding(0);
            this.fctbKeyword.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fctbKeyword.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fctbKeyword.ServiceColors")));
            this.fctbKeyword.Size = new System.Drawing.Size(531, 150);
            this.fctbKeyword.TabIndex = 12;
            this.fctbKeyword.Text = "User keyword";
            this.fctbKeyword.Zoom = 100;
            this.fctbKeyword.TextChanged += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.fctbKeyword_TextChanged);
            this.fctbKeyword.KeyDown += new System.Windows.Forms.KeyEventHandler(this.fctbKeyword_KeyDown);
            // 
            // fctbInpFile
            // 
            this.fctbInpFile.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fctbInpFile.AutoCompleteBracketsList = new char[] {
        '(',
        ')',
        '{',
        '}',
        '[',
        ']',
        '\"',
        '\"',
        '\'',
        '\''};
            this.fctbInpFile.AutoScrollMinSize = new System.Drawing.Size(137, 14);
            this.fctbInpFile.BackBrush = null;
            this.fctbInpFile.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.fctbInpFile.CharHeight = 14;
            this.fctbInpFile.CharWidth = 7;
            this.fctbInpFile.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.fctbInpFile.DisabledColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))), ((int)(((byte)(180)))));
            this.fctbInpFile.Font = new System.Drawing.Font("Consolas", 9F);
            this.fctbInpFile.IsReplaceMode = false;
            this.fctbInpFile.Location = new System.Drawing.Point(3, 199);
            this.fctbInpFile.Name = "fctbInpFile";
            this.fctbInpFile.Paddings = new System.Windows.Forms.Padding(0);
            this.fctbInpFile.ReadOnly = true;
            this.fctbInpFile.SelectionColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(255)))));
            this.fctbInpFile.ServiceColors = ((FastColoredTextBoxNS.ServiceColors)(resources.GetObject("fctbInpFile.ServiceColors")));
            this.fctbInpFile.Size = new System.Drawing.Size(531, 356);
            this.fctbInpFile.TabIndex = 10;
            this.fctbInpFile.Text = "Inp file content";
            this.fctbInpFile.Zoom = 100;
            this.fctbInpFile.TextChanged += new System.EventHandler<FastColoredTextBoxNS.TextChangedEventArgs>(this.fctbInpFile_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(121, 15);
            this.label3.TabIndex = 11;
            this.label3.Text = "Edit selected keyword";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 181);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(157, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Read-only CalculiX input file";
            // 
            // tUpdate
            // 
            this.tUpdate.Interval = 500;
            this.tUpdate.Tick += new System.EventHandler(this.tUpdate_Tick);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Location = new System.Drawing.Point(468, 18);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(131, 537);
            this.panel1.TabIndex = 11;
            // 
            // FrmCalculixKeywordEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1034, 611);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnAddKeyword);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 520);
            this.Name = "FrmCalculixKeywordEditor";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Calculix keyword editor";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.fctbKeyword)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.fctbInpFile)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Button btnMoveDown;
        private System.Windows.Forms.Button btnMoveUp;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAddKeyword;
        private System.Windows.Forms.Button btnOK;
        private UserControls.CodersLabTreeView cltvKeywordsTree; 
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private FastColoredTextBoxNS.FastColoredTextBox fctbKeyword;
        private FastColoredTextBoxNS.FastColoredTextBox fctbInpFile;
        private System.Windows.Forms.CheckBox cbHide;
        private System.Windows.Forms.Timer tUpdate;
        private System.Windows.Forms.Panel panel1;
    }
}