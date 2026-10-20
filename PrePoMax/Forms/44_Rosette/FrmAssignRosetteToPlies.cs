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

namespace PrePoMax.Forms
{
    // Small modal dialog used by the Orientation column context menu of FrmCompositeLayup to pick
    // the rosette that defines the material reference direction of a ply
    class FrmAssignRosetteToPlies : Form
    {
        // Controls
        private Label lRosette;
        private ComboBox cbRosettes;
        private Label lAngle;
        private NumericUpDown nudAngle;
        private CheckBox cbApplyToAllPlies;
        private Label lHint;
        private Button btnOK;
        private Button btnCancel;


        // Properties
        public string SelectedRosetteName { get; private set; }
        public double RotationAngle { get; private set; }
        public bool ApplyToAllPlies { get; private set; }


        // Constructors
        public FrmAssignRosetteToPlies(string[] rosetteNames, string currentRosette, double currentAngle)
        {
            InitializeComponent();
            //
            SelectedRosetteName = null;
            RotationAngle = currentAngle;
            ApplyToAllPlies = false;
            //
            foreach (string name in rosetteNames) cbRosettes.Items.Add(name);
            int index = currentRosette != null ? cbRosettes.Items.IndexOf(currentRosette) : -1;
            if (index < 0 && cbRosettes.Items.Count > 0) index = 0;
            if (index >= 0) cbRosettes.SelectedIndex = index;
            //
            nudAngle.Value = (decimal)Math.Max((double)nudAngle.Minimum,
                                               Math.Min((double)nudAngle.Maximum, currentAngle));
        }

        private void InitializeComponent()
        {
            this.lRosette = new Label();
            this.cbRosettes = new ComboBox();
            this.lAngle = new Label();
            this.nudAngle = new NumericUpDown();
            this.cbApplyToAllPlies = new CheckBox();
            this.lHint = new Label();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.nudAngle)).BeginInit();
            this.SuspendLayout();
            //
            // lRosette
            //
            this.lRosette.AutoSize = true;
            this.lRosette.Location = new Point(12, 18);
            this.lRosette.Name = "lRosette";
            this.lRosette.Size = new Size(50, 15);
            this.lRosette.Text = "Rosette";
            //
            // cbRosettes
            //
            this.cbRosettes.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbRosettes.Location = new Point(140, 15);
            this.cbRosettes.Name = "cbRosettes";
            this.cbRosettes.Size = new Size(190, 23);
            this.cbRosettes.TabIndex = 0;
            //
            // lAngle
            //
            this.lAngle.AutoSize = true;
            this.lAngle.Location = new Point(12, 50);
            this.lAngle.Name = "lAngle";
            this.lAngle.Size = new Size(120, 15);
            this.lAngle.Text = "Rotation angle [°]";
            //
            // nudAngle
            //
            this.nudAngle.DecimalPlaces = 2;
            this.nudAngle.Location = new Point(140, 47);
            this.nudAngle.Minimum = new decimal(new int[] { 360, 0, 0, -2147483648 });
            this.nudAngle.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
            this.nudAngle.Name = "nudAngle";
            this.nudAngle.Size = new Size(90, 23);
            this.nudAngle.TabIndex = 1;
            //
            // cbApplyToAllPlies
            //
            this.cbApplyToAllPlies.AutoSize = true;
            this.cbApplyToAllPlies.Location = new Point(140, 78);
            this.cbApplyToAllPlies.Name = "cbApplyToAllPlies";
            this.cbApplyToAllPlies.Size = new Size(120, 19);
            this.cbApplyToAllPlies.TabIndex = 2;
            this.cbApplyToAllPlies.Text = "Apply to all plies";
            this.cbApplyToAllPlies.UseVisualStyleBackColor = true;
            //
            // lHint
            //
            this.lHint.ForeColor = SystemColors.GrayText;
            this.lHint.Location = new Point(12, 104);
            this.lHint.Name = "lHint";
            this.lHint.Size = new Size(320, 34);
            this.lHint.Text = "The rosette replaces the reference direction only; the rotation angle is " +
                              "still applied on top of it.";
            //
            // btnOK
            //
            this.btnOK.DialogResult = DialogResult.OK;
            this.btnOK.Location = new Point(174, 145);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 25);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(255, 145);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 25);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            //
            // FrmAssignRosetteToPlies
            //
            this.AcceptButton = this.btnOK;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(344, 180);
            this.Controls.Add(this.lRosette);
            this.Controls.Add(this.cbRosettes);
            this.Controls.Add(this.lAngle);
            this.Controls.Add(this.nudAngle);
            this.Controls.Add(this.cbApplyToAllPlies);
            this.Controls.Add(this.lHint);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAssignRosetteToPlies";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Assign orientations from rosette";
            ((System.ComponentModel.ISupportInitialize)(this.nudAngle)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }


        // Event handlers
        private void btnOK_Click(object sender, EventArgs e)
        {
            SelectedRosetteName = cbRosettes.SelectedItem as string;
            RotationAngle = (double)nudAngle.Value;
            ApplyToAllPlies = cbApplyToAllPlies.Checked;
        }
    }
}
