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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CaeModel;
using CaeGlobals;
using System.Windows.Forms;
using System.Drawing;
using System.ComponentModel;

namespace PrePoMax.Forms
{
    class FrmCreateCompositeLayup : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables
        private string[] _compositeLayupNames;
        private Controller _controller;
        private Action<CompositeLayup> _onContinue;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private TextBox tbName;
        private Label lInitialPlyCount;
        private NumericUpDown nudInitialPlyCount;
        private Label lElementType;
        private ComboBox cbElementType;
        private Button btnContinue;
        private Button btnCancel;


        // Constructors
        public FrmCreateCompositeLayup(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
        }

        private void InitializeComponent()
        {
            this.gbData = new System.Windows.Forms.GroupBox();
            this.lName = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.lInitialPlyCount = new System.Windows.Forms.Label();
            this.nudInitialPlyCount = new System.Windows.Forms.NumericUpDown();
            this.lElementType = new System.Windows.Forms.Label();
            this.cbElementType = new System.Windows.Forms.ComboBox();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.gbData.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudInitialPlyCount)).BeginInit();
            this.SuspendLayout();

            //
            // gbData
            //
            this.gbData.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbData.Controls.Add(this.lName);
            this.gbData.Controls.Add(this.tbName);
            this.gbData.Controls.Add(this.lInitialPlyCount);
            this.gbData.Controls.Add(this.nudInitialPlyCount);
            this.gbData.Controls.Add(this.lElementType);
            this.gbData.Controls.Add(this.cbElementType);
            this.gbData.Location = new System.Drawing.Point(12, 12);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(310, 115);
            this.gbData.TabIndex = 0;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";

            //
            // lName
            //
            this.lName.AutoSize = true;
            this.lName.Location = new System.Drawing.Point(10, 25);
            this.lName.Name = "lName";
            this.lName.Size = new System.Drawing.Size(42, 15);
            this.lName.TabIndex = 0;
            this.lName.Text = "Name:";

            //
            // tbName
            //
            this.tbName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbName.Location = new System.Drawing.Point(110, 22);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(190, 23);
            this.tbName.TabIndex = 1;

            //
            // lInitialPlyCount
            //
            this.lInitialPlyCount.AutoSize = true;
            this.lInitialPlyCount.Location = new System.Drawing.Point(10, 55);
            this.lInitialPlyCount.Name = "lInitialPlyCount";
            this.lInitialPlyCount.Size = new System.Drawing.Size(90, 15);
            this.lInitialPlyCount.TabIndex = 2;
            this.lInitialPlyCount.Text = "Initial ply count:";

            //
            // nudInitialPlyCount
            //
            this.nudInitialPlyCount.Location = new System.Drawing.Point(110, 52);
            this.nudInitialPlyCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudInitialPlyCount.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudInitialPlyCount.Name = "nudInitialPlyCount";
            this.nudInitialPlyCount.Size = new System.Drawing.Size(80, 23);
            this.nudInitialPlyCount.TabIndex = 3;
            this.nudInitialPlyCount.Value = new decimal(new int[] { 3, 0, 0, 0 });

            //
            // lElementType
            //
            this.lElementType.AutoSize = true;
            this.lElementType.Location = new System.Drawing.Point(10, 85);
            this.lElementType.Name = "lElementType";
            this.lElementType.Size = new System.Drawing.Size(78, 15);
            this.lElementType.TabIndex = 4;
            this.lElementType.Text = "Element type:";

            //
            // cbElementType
            //
            this.cbElementType.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbElementType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbElementType.FormattingEnabled = true;
            this.cbElementType.Location = new System.Drawing.Point(110, 82);
            this.cbElementType.Name = "cbElementType";
            this.cbElementType.Size = new System.Drawing.Size(190, 23);
            this.cbElementType.TabIndex = 5;

            //
            // btnContinue
            //
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.Location = new System.Drawing.Point(156, 135);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(85, 23);
            this.btnContinue.TabIndex = 1;
            this.btnContinue.Text = "Continue...";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);

            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(247, 135);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            //
            // FrmCreateCompositeLayup
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(334, 170);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmCreateCompositeLayup";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Create Composite Layup";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmCreateCompositeLayup_FormClosing);

            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudInitialPlyCount)).EndInit();
            this.ResumeLayout(false);
        }


        // Event handlers
        private void btnContinue_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate name
                string name = tbName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                    throw new CaeException("Please enter a name for the composite layup.");

                // Check if name exists
                UserControls.FrmProperties.CheckName(null, name, _compositeLayupNames, "composite layup");

                // Get values
                int initialPlyCount = (int)nudInitialPlyCount.Value;
                CompositeElementType elementType = (CompositeElementType)cbElementType.SelectedIndex;

                // Create the composite layup
                CompositeLayup compositeLayup = new CompositeLayup(name, initialPlyCount, elementType);

                // Hide this form
                Hide();

                // Call the continue action
                _onContinue?.Invoke(compositeLayup);
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void FrmCreateCompositeLayup_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }


        // Methods
        public bool PrepareForm(string stepName, string itemToEditName)
        {
            _compositeLayupNames = _controller.GetCompositeLayupNames();

            // Initialize element type combo box
            cbElementType.Items.Clear();
            cbElementType.Items.Add("Conventional shell");
            cbElementType.Items.Add("Continuum shell");
            cbElementType.Items.Add("Solid");
            cbElementType.SelectedIndex = 0;

            // Set default name
            tbName.Text = _compositeLayupNames.GetNextNumberedKey("CompositeLayup");

            // Set default ply count
            nudInitialPlyCount.Value = 3;

            _controller.SetSelectByToOff();

            return true;
        }

        public void SetOnContinueAction(Action<CompositeLayup> onContinue)
        {
            _onContinue = onContinue;
        }
    }
}
