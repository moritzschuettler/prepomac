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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace PrePoMax.Forms
{
    class FrmImportCompositeLayupFromH5 : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables
        private Controller _controller;
        private CompositeCaeData _data;

        // Controls
        private GroupBox gbFile;
        private Label lFile;
        private TextBox tbFile;
        private Button btnBrowse;
        private Label lLayupName;
        private TextBox tbLayupName;
        private GroupBox gbMapping;
        private Label lRegion;
        private ComboBox cbRegion;
        private CheckBox cbAutoDrillDepth;
        private Label lDrillDepth;
        private TextBox tbDrillDepth;
        private Label lTolerance;
        private TextBox tbTolerance;
        private GroupBox gbPlies;
        private DataGridView dgvPlies;
        private Label lInfo;
        private Button btnOK;
        private Button btnCancel;


        // Constructors
        public FrmImportCompositeLayupFromH5(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
        }

        private void InitializeComponent()
        {
            this.gbFile = new System.Windows.Forms.GroupBox();
            this.lFile = new System.Windows.Forms.Label();
            this.tbFile = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.lLayupName = new System.Windows.Forms.Label();
            this.tbLayupName = new System.Windows.Forms.TextBox();
            this.gbMapping = new System.Windows.Forms.GroupBox();
            this.lRegion = new System.Windows.Forms.Label();
            this.cbRegion = new System.Windows.Forms.ComboBox();
            this.cbAutoDrillDepth = new System.Windows.Forms.CheckBox();
            this.lDrillDepth = new System.Windows.Forms.Label();
            this.tbDrillDepth = new System.Windows.Forms.TextBox();
            this.lTolerance = new System.Windows.Forms.Label();
            this.tbTolerance = new System.Windows.Forms.TextBox();
            this.gbPlies = new System.Windows.Forms.GroupBox();
            this.dgvPlies = new System.Windows.Forms.DataGridView();
            this.lInfo = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            //
            this.gbFile.SuspendLayout();
            this.gbMapping.SuspendLayout();
            this.gbPlies.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).BeginInit();
            this.SuspendLayout();
            //
            // gbFile
            //
            this.gbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.gbFile.Controls.Add(this.lFile);
            this.gbFile.Controls.Add(this.tbFile);
            this.gbFile.Controls.Add(this.btnBrowse);
            this.gbFile.Controls.Add(this.lLayupName);
            this.gbFile.Controls.Add(this.tbLayupName);
            this.gbFile.Location = new System.Drawing.Point(12, 12);
            this.gbFile.Name = "gbFile";
            this.gbFile.Size = new System.Drawing.Size(510, 85);
            this.gbFile.TabIndex = 0;
            this.gbFile.TabStop = false;
            this.gbFile.Text = "File";
            //
            // lFile
            //
            this.lFile.AutoSize = true;
            this.lFile.Location = new System.Drawing.Point(10, 25);
            this.lFile.Name = "lFile";
            this.lFile.Size = new System.Drawing.Size(60, 15);
            this.lFile.TabIndex = 0;
            this.lFile.Text = "HDF5 file:";
            //
            // tbFile
            //
            this.tbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFile.Location = new System.Drawing.Point(110, 22);
            this.tbFile.Name = "tbFile";
            this.tbFile.ReadOnly = true;
            this.tbFile.Size = new System.Drawing.Size(345, 23);
            this.tbFile.TabIndex = 1;
            //
            // btnBrowse
            //
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(461, 21);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(39, 25);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            //
            // lLayupName
            //
            this.lLayupName.AutoSize = true;
            this.lLayupName.Location = new System.Drawing.Point(10, 55);
            this.lLayupName.Name = "lLayupName";
            this.lLayupName.Size = new System.Drawing.Size(78, 15);
            this.lLayupName.TabIndex = 3;
            this.lLayupName.Text = "Layup name:";
            //
            // tbLayupName
            //
            this.tbLayupName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.tbLayupName.Location = new System.Drawing.Point(110, 52);
            this.tbLayupName.Name = "tbLayupName";
            this.tbLayupName.Size = new System.Drawing.Size(390, 23);
            this.tbLayupName.TabIndex = 4;
            //
            // gbMapping
            //
            this.gbMapping.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.gbMapping.Controls.Add(this.lRegion);
            this.gbMapping.Controls.Add(this.cbRegion);
            this.gbMapping.Controls.Add(this.cbAutoDrillDepth);
            this.gbMapping.Controls.Add(this.lDrillDepth);
            this.gbMapping.Controls.Add(this.tbDrillDepth);
            this.gbMapping.Controls.Add(this.lTolerance);
            this.gbMapping.Controls.Add(this.tbTolerance);
            this.gbMapping.Location = new System.Drawing.Point(12, 103);
            this.gbMapping.Name = "gbMapping";
            this.gbMapping.Size = new System.Drawing.Size(510, 85);
            this.gbMapping.TabIndex = 1;
            this.gbMapping.TabStop = false;
            this.gbMapping.Text = "Core sampling";
            //
            // lRegion
            //
            this.lRegion.AutoSize = true;
            this.lRegion.Location = new System.Drawing.Point(10, 25);
            this.lRegion.Name = "lRegion";
            this.lRegion.Size = new System.Drawing.Size(50, 15);
            this.lRegion.TabIndex = 0;
            this.lRegion.Text = "Sample:";
            //
            // cbRegion
            //
            this.cbRegion.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.cbRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbRegion.FormattingEnabled = true;
            this.cbRegion.Location = new System.Drawing.Point(110, 22);
            this.cbRegion.Name = "cbRegion";
            this.cbRegion.Size = new System.Drawing.Size(390, 23);
            this.cbRegion.TabIndex = 1;
            //
            // cbAutoDrillDepth
            //
            this.cbAutoDrillDepth.AutoSize = true;
            this.cbAutoDrillDepth.Checked = true;
            this.cbAutoDrillDepth.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbAutoDrillDepth.Location = new System.Drawing.Point(13, 56);
            this.cbAutoDrillDepth.Name = "cbAutoDrillDepth";
            this.cbAutoDrillDepth.Size = new System.Drawing.Size(94, 19);
            this.cbAutoDrillDepth.TabIndex = 2;
            this.cbAutoDrillDepth.Text = "Auto depth";
            this.cbAutoDrillDepth.UseVisualStyleBackColor = true;
            this.cbAutoDrillDepth.CheckedChanged += new System.EventHandler(this.cbAutoDrillDepth_CheckedChanged);
            //
            // lDrillDepth
            //
            this.lDrillDepth.AutoSize = true;
            this.lDrillDepth.Location = new System.Drawing.Point(115, 57);
            this.lDrillDepth.Name = "lDrillDepth";
            this.lDrillDepth.Size = new System.Drawing.Size(66, 15);
            this.lDrillDepth.TabIndex = 3;
            this.lDrillDepth.Text = "Drill depth:";
            //
            // tbDrillDepth
            //
            this.tbDrillDepth.Location = new System.Drawing.Point(190, 53);
            this.tbDrillDepth.Name = "tbDrillDepth";
            this.tbDrillDepth.ReadOnly = true;
            this.tbDrillDepth.Size = new System.Drawing.Size(100, 23);
            this.tbDrillDepth.TabIndex = 4;
            //
            // lTolerance
            //
            this.lTolerance.AutoSize = true;
            this.lTolerance.Location = new System.Drawing.Point(310, 57);
            this.lTolerance.Name = "lTolerance";
            this.lTolerance.Size = new System.Drawing.Size(62, 15);
            this.lTolerance.TabIndex = 5;
            this.lTolerance.Text = "Tolerance:";
            //
            // tbTolerance
            //
            this.tbTolerance.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTolerance.Location = new System.Drawing.Point(400, 53);
            this.tbTolerance.Name = "tbTolerance";
            this.tbTolerance.Size = new System.Drawing.Size(100, 23);
            this.tbTolerance.TabIndex = 6;
            //
            // gbPlies
            //
            this.gbPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gbPlies.Controls.Add(this.dgvPlies);
            this.gbPlies.Location = new System.Drawing.Point(12, 194);
            this.gbPlies.Name = "gbPlies";
            this.gbPlies.Size = new System.Drawing.Size(510, 235);
            this.gbPlies.TabIndex = 2;
            this.gbPlies.TabStop = false;
            this.gbPlies.Text = "Plies";
            //
            // dgvPlies
            //
            this.dgvPlies.AllowUserToAddRows = false;
            this.dgvPlies.AllowUserToDeleteRows = false;
            this.dgvPlies.AllowUserToResizeRows = false;
            this.dgvPlies.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top
                        | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvPlies.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPlies.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.dgvPlies.Location = new System.Drawing.Point(10, 22);
            this.dgvPlies.MultiSelect = false;
            this.dgvPlies.Name = "dgvPlies";
            this.dgvPlies.ReadOnly = true;
            this.dgvPlies.RowHeadersVisible = false;
            this.dgvPlies.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvPlies.Size = new System.Drawing.Size(490, 203);
            this.dgvPlies.TabIndex = 0;
            //
            // lInfo
            //
            this.lInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom
                        | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lInfo.AutoEllipsis = true;
            this.lInfo.Location = new System.Drawing.Point(12, 437);
            this.lInfo.Name = "lInfo";
            this.lInfo.Size = new System.Drawing.Size(510, 32);
            this.lInfo.TabIndex = 3;
            this.lInfo.Text = "Select a Composite CAE HDF5 file.";
            //
            // btnOK
            //
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Enabled = false;
            this.btnOK.Location = new System.Drawing.Point(356, 475);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 23);
            this.btnOK.TabIndex = 4;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(442, 475);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 23);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            //
            // FrmImportCompositeLayupFromH5
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(534, 510);
            this.Controls.Add(this.gbFile);
            this.Controls.Add(this.gbMapping);
            this.Controls.Add(this.gbPlies);
            this.Controls.Add(this.lInfo);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(550, 549);
            this.Name = "FrmImportCompositeLayupFromH5";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Import Composite Layup from HDF5";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmImportCompositeLayupFromH5_FormClosing);
            //
            this.gbFile.ResumeLayout(false);
            this.gbFile.PerformLayout();
            this.gbMapping.ResumeLayout(false);
            this.gbMapping.PerformLayout();
            this.gbPlies.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPlies)).EndInit();
            this.ResumeLayout(false);
        }


        // Event handlers
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Import Composite Layup from HDF5";
                    openFileDialog.Filter = "HDF5 files (*.hdf5;*.h5)|*.hdf5;*.h5|All files (*.*)|*.*";
                    if (openFileDialog.ShowDialog(this) != DialogResult.OK) return;
                    //
                    LoadFile(openFileDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void cbAutoDrillDepth_CheckedChanged(object sender, EventArgs e)
        {
            tbDrillDepth.ReadOnly = cbAutoDrillDepth.Checked;
            if (cbAutoDrillDepth.Checked && _data != null) tbDrillDepth.Text = GetAutoDrillDepth().ToString("G6", CultureInfo.InvariantCulture);
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (_data == null) throw new CaeException("Select a Composite CAE HDF5 file first.");
                //
                string layupName = tbLayupName.Text.Trim();
                if (string.IsNullOrEmpty(layupName)) throw new CaeException("Enter a name for the composite layup.");
                //
                double drillDepth = cbAutoDrillDepth.Checked ? 0 : GetDouble(tbDrillDepth, "drill depth");
                double tolerance = GetDouble(tbTolerance, "tolerance");
                if (!cbAutoDrillDepth.Checked && drillDepth <= 0)
                    throw new CaeException("The drill depth must be larger than zero.");
                if (tolerance < 0) throw new CaeException("The tolerance cannot be negative.");
                //
                string regionName = cbRegion.SelectedIndex <= 0 ? null : (string)cbRegion.SelectedItem;
                //
                Hide();
                //
                _controller.ImportCompositeLayupFromH5Command(tbFile.Text, layupName, regionName, drillDepth, tolerance);
                //
                string summary = _controller.LastCompositeCaeImportSummary;
                if (!string.IsNullOrEmpty(summary)) MessageBoxes.ShowInfo(summary, "Import Composite Layup from HDF5");
            }
            catch (Exception ex)
            {
                Show();
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Hide();
        }
        private void FrmImportCompositeLayupFromH5_FormClosing(object sender, FormClosingEventArgs e)
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
            _data = null;
            //
            tbFile.Text = "";
            tbLayupName.Text = "";
            tbDrillDepth.Text = "";
            tbTolerance.Text = GetDefaultTolerance().ToString("G6", CultureInfo.InvariantCulture);
            cbAutoDrillDepth.Checked = true;
            tbDrillDepth.ReadOnly = true;
            btnOK.Enabled = false;
            lInfo.Text = "Select a Composite CAE HDF5 file.";
            //
            SetupPliesGrid();
            FillRegionComboBox();
            //
            _controller.SetSelectByToOff();
            //
            return true;
        }
        private void SetupPliesGrid()
        {
            dgvPlies.Rows.Clear();
            dgvPlies.Columns.Clear();
            //
            AddColumn("colLayer", "Layer", 50);
            AddColumn("colPly", "Ply", 180);
            AddColumn("colGroup", "Group", 140);
            AddColumn("colMaterial", "Material", 90);
            AddColumn("colAngle", "Angle [°]", 70);
            AddColumn("colThickness", "Thickness", 80);
            AddColumn("colTriangles", "Triangles", 70);
        }
        private void AddColumn(string name, string headerText, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = headerText;
            column.Width = width;
            column.ReadOnly = true;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            dgvPlies.Columns.Add(column);
        }
        private void FillRegionComboBox()
        {
            cbRegion.Items.Clear();
            cbRegion.Items.Add("All shell elements");
            //
            FeMesh mesh = _controller.Model != null ? _controller.Model.Mesh : null;
            if (mesh != null)
            {
                foreach (var entry in mesh.Parts)
                {
                    if (entry.Value.PartType == PartType.Shell || entry.Value.PartType == PartType.SolidAsShell)
                        cbRegion.Items.Add(entry.Key);
                }
                foreach (string name in _controller.GetUserElementSetNames()) cbRegion.Items.Add(name);
            }
            cbRegion.SelectedIndex = 0;
        }
        private void LoadFile(string fileName)
        {
            _data = null;
            btnOK.Enabled = false;
            dgvPlies.Rows.Clear();
            //
            tbFile.Text = fileName;
            //
            CompositeCaeData data = CompositeCaeH5Reader.Read(fileName);
            _data = data;
            //
            foreach (CompositeCaePly ply in data.Plies)
            {
                dgvPlies.Rows.Add(new object[] { ply.LayerId, ply.RawName, ply.GroupName, ply.MaterialName,
                                                 ply.Orientation.ToString("G6", CultureInfo.InvariantCulture),
                                                 ply.Thickness.ToString("G6", CultureInfo.InvariantCulture),
                                                 ply.Surface.TriangleCount });
            }
            //
            string layupName = NamedClass.GetErrorFreeName(Path.GetFileNameWithoutExtension(fileName), "Layup", null);
            tbLayupName.Text = _controller.GetCompositeLayupNames().GetNextNumberedKey(layupName);
            //
            tbDrillDepth.Text = GetAutoDrillDepth().ToString("G6", CultureInfo.InvariantCulture);
            //
            int materialCount = data.Materials.Count(x => x.HasMechanicalData);
            string info = data.Plies.Count + " plies in " + data.GetLayerCount() + " layers, " +
                          data.GetTriangleCount() + " triangles." + Environment.NewLine +
                          "Vendor: " + (data.Vendor ?? "-") + " · format " + (data.FormatVersion ?? "-") +
                          " · length unit " + (data.LengthUnit ?? "-") + " · " + materialCount + " of " +
                          data.Materials.Count + " materials with mechanical data.";
            if (data.Warnings.Count > 0) info += Environment.NewLine + "Warning: " + data.Warnings[0];
            lInfo.Text = info;
            //
            FeMesh mesh = _controller.Model != null ? _controller.Model.Mesh : null;
            if (mesh == null || mesh.Elements.Count == 0)
                lInfo.Text += Environment.NewLine + "The model has no mesh; the mesh of the file will be imported.";
            //
            btnOK.Enabled = true;
        }
        private double GetAutoDrillDepth()
        {
            return _data == null ? 0 : _data.GetAutoDrillDepth();
        }
        private double GetDefaultTolerance()
        {
            FeMesh mesh = _controller.Model != null ? _controller.Model.Mesh : null;
            if (mesh != null && mesh.Nodes.Count > 0)
            {
                BoundingBox box = new BoundingBox();
                foreach (var entry in mesh.Nodes) box.IncludeNode(entry.Value);
                double diagonal = box.GetDiagonal();
                if (diagonal > 0) return Tools.RoundToSignificantDigits(1E-6 * diagonal, 2);
            }
            return 1E-6;
        }
        private double GetDouble(TextBox textBox, string valueName)
        {
            double value;
            if (!double.TryParse(textBox.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                throw new CaeException("The " + valueName + " is not a valid number: " + textBox.Text);
            return value;
        }
    }
}
