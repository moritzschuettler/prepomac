// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Yvan Blanchard
// Contributors:
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CaeGlobals;

namespace PrePoMax.Forms
{
    // Ply region mapping from geometrical surface: modal dialog to pick one named CAD surface, the
    // shell part the elements must come from and the core sampling depth, with a preview that
    // paints the captured elements in the 3D view. Used by FrmCompositeLayup's Region column
    // context menu and by the Element Sets tree action of FrmMain.
    class FrmImportPlyRegionFromSurface : Form
    {
        // Variables
        private const string AllShellParts = "<all shell parts>";
        private Controller _controller;

        // Controls
        private Label lSurface;
        private ComboBox cbSurfaces;
        private Label lTargetPart;
        private ComboBox cbTargetPart;
        private Label lCoreSamplingDepth;
        private NumericUpDown nudCoreSamplingDepth;
        private Label lDepthHint;
        private Button btnPreview;
        private Label lPreviewResult;
        private Button btnOK;
        private Button btnCancel;


        // Properties
        public string SelectedPartName { get; private set; }
        public int SelectedFaceId { get; private set; }
        // Length of the drill line cast from each shell element centroid along its normal;
        // 0 lets the mapper derive it from the average element size
        public double CoreSamplingDepth { get; private set; }
        // Mesh parts the elements may come from; null searches every shell part
        public string[] TargetPartNames { get; private set; }


        // Constructors
        public FrmImportPlyRegionFromSurface(Controller controller, List<GeometrySurfaceOption> options)
        {
            InitializeComponent();
            //
            _controller = controller;
            string[] shellPartNames = controller.GetShellMeshPartNames();
            //
            SelectedPartName = null;
            SelectedFaceId = -1;
            CoreSamplingDepth = 0;
            TargetPartNames = null;
            //
            foreach (GeometrySurfaceOption option in options) cbSurfaces.Items.Add(option);
            if (cbSurfaces.Items.Count > 0) cbSurfaces.SelectedIndex = 0;
            // The skin of a solid mesh also lives in a shell part, so the target part has to be
            // selectable to keep those elements out of the region
            cbTargetPart.Items.Add(AllShellParts);
            if (shellPartNames != null) foreach (string name in shellPartNames) cbTargetPart.Items.Add(name);
            cbTargetPart.SelectedIndex = cbTargetPart.Items.Count == 2 ? 1 : 0;
        }

        private void InitializeComponent()
        {
            this.lSurface = new Label();
            this.cbSurfaces = new ComboBox();
            this.lTargetPart = new Label();
            this.cbTargetPart = new ComboBox();
            this.lCoreSamplingDepth = new Label();
            this.nudCoreSamplingDepth = new NumericUpDown();
            this.lDepthHint = new Label();
            this.btnPreview = new Button();
            this.lPreviewResult = new Label();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudCoreSamplingDepth)).BeginInit();
            this.SuspendLayout();
            //
            // lSurface
            //
            this.lSurface.AutoSize = true;
            this.lSurface.Location = new Point(12, 15);
            this.lSurface.Name = "lSurface";
            this.lSurface.Size = new Size(80, 13);
            this.lSurface.Text = "Surface:";
            //
            // cbSurfaces
            //
            this.cbSurfaces.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbSurfaces.FormattingEnabled = true;
            this.cbSurfaces.Location = new Point(12, 32);
            this.cbSurfaces.Name = "cbSurfaces";
            this.cbSurfaces.Size = new Size(340, 21);
            this.cbSurfaces.TabIndex = 0;
            //
            // lTargetPart
            //
            this.lTargetPart.AutoSize = true;
            this.lTargetPart.Location = new Point(12, 63);
            this.lTargetPart.Name = "lTargetPart";
            this.lTargetPart.Size = new Size(80, 13);
            this.lTargetPart.Text = "Target shell part:";
            //
            // cbTargetPart
            //
            this.cbTargetPart.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbTargetPart.FormattingEnabled = true;
            this.cbTargetPart.Location = new Point(12, 80);
            this.cbTargetPart.Name = "cbTargetPart";
            this.cbTargetPart.Size = new Size(340, 21);
            this.cbTargetPart.TabIndex = 1;
            //
            // lCoreSamplingDepth
            //
            this.lCoreSamplingDepth.AutoSize = true;
            this.lCoreSamplingDepth.Location = new Point(12, 116);
            this.lCoreSamplingDepth.Name = "lCoreSamplingDepth";
            this.lCoreSamplingDepth.Size = new Size(120, 13);
            this.lCoreSamplingDepth.Text = "Core sampling depth:";
            //
            // nudCoreSamplingDepth
            //
            this.nudCoreSamplingDepth.DecimalPlaces = 4;
            this.nudCoreSamplingDepth.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.nudCoreSamplingDepth.Location = new Point(140, 113);
            this.nudCoreSamplingDepth.Maximum = new decimal(new int[] { 1000000, 0, 0, 0 });
            this.nudCoreSamplingDepth.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudCoreSamplingDepth.Name = "nudCoreSamplingDepth";
            this.nudCoreSamplingDepth.Size = new Size(90, 20);
            this.nudCoreSamplingDepth.TabIndex = 2;
            this.nudCoreSamplingDepth.Value = new decimal(new int[] { 0, 0, 0, 0 });
            //
            // lDepthHint
            //
            this.lDepthHint.AutoSize = true;
            this.lDepthHint.ForeColor = SystemColors.GrayText;
            this.lDepthHint.Location = new Point(12, 140);
            this.lDepthHint.Name = "lDepthHint";
            this.lDepthHint.Size = new Size(300, 13);
            this.lDepthHint.Text = "0 = automatic (5 x the average shell element edge length)";
            //
            // btnPreview
            //
            this.btnPreview.Location = new Point(12, 166);
            this.btnPreview.Name = "btnPreview";
            this.btnPreview.Size = new Size(150, 23);
            this.btnPreview.TabIndex = 3;
            this.btnPreview.Text = "Preview captured elements";
            this.btnPreview.UseVisualStyleBackColor = true;
            this.btnPreview.Click += new EventHandler(this.btnPreview_Click);
            //
            // lPreviewResult
            //
            this.lPreviewResult.AutoSize = false;
            this.lPreviewResult.ForeColor = SystemColors.GrayText;
            this.lPreviewResult.Location = new Point(12, 194);
            this.lPreviewResult.Name = "lPreviewResult";
            this.lPreviewResult.Size = new Size(340, 30);
            this.lPreviewResult.Text = "";
            //
            // btnOK
            //
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Location = new Point(196, 220);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(277, 220);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // FrmImportPlyRegionFromSurface
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(364, 255);
            this.Controls.Add(this.lSurface);
            this.Controls.Add(this.cbSurfaces);
            this.Controls.Add(this.lTargetPart);
            this.Controls.Add(this.cbTargetPart);
            this.Controls.Add(this.lCoreSamplingDepth);
            this.Controls.Add(this.nudCoreSamplingDepth);
            this.Controls.Add(this.lDepthHint);
            this.Controls.Add(this.btnPreview);
            this.Controls.Add(this.lPreviewResult);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmImportPlyRegionFromSurface";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Import Region from Geometrical Surface";
            this.FormClosed += new FormClosedEventHandler(this.FrmImportPlyRegionFromSurface_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.nudCoreSamplingDepth)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }


        // Event handlers
        // Core samples with the values currently in the dialog and paints the captured elements;
        // the controller drops the previous preview before drawing the new one
        private void btnPreview_Click(object sender, EventArgs e)
        {
            try
            {
                ReadInputs();
                if (SelectedPartName == null) return;
                //
                int elementCount;
                string message;
                bool success = _controller.PreviewGeometrySurfaceElements(SelectedPartName, SelectedFaceId,
                                                                          CoreSamplingDepth, TargetPartNames,
                                                                          out elementCount, out message);
                lPreviewResult.Text = success ? message : message.Replace("\n", " ");
                lPreviewResult.ForeColor = success ? SystemColors.GrayText : Color.Firebrick;
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void FrmImportPlyRegionFromSurface_FormClosed(object sender, FormClosedEventArgs e)
        {
            // The preview must not outlive the dialog, whether it was validated or cancelled
            _controller.ClearGeometrySurfacePreview();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            ReadInputs();
        }


        // Methods
        private void ReadInputs()
        {
            if (cbSurfaces.SelectedItem is GeometrySurfaceOption option)
            {
                SelectedPartName = option.PartName;
                SelectedFaceId = option.FaceId;
            }
            CoreSamplingDepth = (double)nudCoreSamplingDepth.Value;
            //
            string targetPart = cbTargetPart.SelectedItem as string;
            TargetPartNames = targetPart == null || targetPart == AllShellParts
                              ? null : new string[] { targetPart };
        }
    }
}
