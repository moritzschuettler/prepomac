using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using CaeModel;
using CaeMesh;

namespace PrePoMax.Forms
{
    public class FrmBladeAerodynamicLoad : Form
    {
        // Variables
        private Controller _controller;
        private string _stepName;
        private (double distance, double force)[] _rows;

        // Controls
        private Label lDistanceType;
        private ComboBox cmbDistanceType;
        private Button btnLoadFromFile;
        private DataGridView dgvSections;
        private Label lblStatus;
        private CheckBox cbPressureMode;
        private Label lFaceSurface;
        private ComboBox cmbFaceSurface;
        private Button btnOK;
        private Button btnCancel;


        // Constructor
        public FrmBladeAerodynamicLoad(Controller controller, string stepName)
        {
            _controller = controller;
            _stepName = stepName;
            _rows = null;
            InitializeComponent();
            SetupColumns();
            PopulateSurfaces();
        }


        // Event handlers
        private void btnLoadFromFile_Click(object sender, EventArgs e)
        {
            try
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = "Select Blade Load CSV File";
                    dlg.Filter = "CSV files (*.csv;*.txt)|*.csv;*.txt|All files (*.*)|*.*";
                    if (dlg.ShowDialog(this) != DialogResult.OK) return;
                    LoadCsvFile(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Load CSV Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void cbPressureMode_CheckedChanged(object sender, EventArgs e)
        {
            bool pressure = cbPressureMode.Checked;
            lFaceSurface.Visible = pressure;
            cmbFaceSurface.Visible = pressure;
        }
        private void cmbDistanceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (dgvSections.Columns.Count == 0) return;
            dgvSections.Columns["colDist"].HeaderText = DistanceType == BladeAeroDistanceTypeEnum.Absolute
                ? "Spine coordinate (model units)" : "Span ratio (0-100 % or 0.0-1.0)";
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                if (_rows == null || _rows.Length == 0)
                    throw new Exception("No CSV data loaded. Use 'Load from CSV file...' first.");
                //
                var mesh = _controller.DisplayedMesh;
                if (mesh == null || mesh.Nodes == null || mesh.Nodes.Count == 0)
                    throw new Exception("No mesh found in the model. Import a mesh before applying blade aerodynamic loads.");
                //
                if (cbPressureMode.Checked)
                {
                    string surfName = cmbFaceSurface.SelectedItem as string;
                    if (string.IsNullOrEmpty(surfName))
                        throw new Exception("Select a face surface for pressure mode.");
                    //
                    var sections = BladeAeroAlgorithm.ComputePressureSections(mesh, _rows, surfName, 2, null,
                                                                              DistanceType);
                    _controller.AddBladeAeroPressureLoadCommand(_stepName, sections);
                }
                else
                {
                    var sections = BladeAeroAlgorithm.ComputeSections(mesh, _rows, 2, 0, null, DistanceType);
                    _controller.AddBladeAerodynamicLoadCommand(_stepName, sections);
                }
                //
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Blade Aerodynamic Load Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }


        // Methods
        private BladeAeroDistanceTypeEnum DistanceType
        {
            get
            {
                return cmbDistanceType.SelectedIndex == 1
                    ? BladeAeroDistanceTypeEnum.Relative : BladeAeroDistanceTypeEnum.Absolute;
            }
        }
        private void LoadCsvFile(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var rows = new System.Collections.Generic.List<(double, double)>();
            //
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                    continue;
                //
                char sep = line.Contains(";") ? ';' : ',';
                string[] parts = line.Split(sep);
                if (parts.Length < 2) continue;
                //
                if (!double.TryParse(parts[0].Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double dist)) continue;
                if (!double.TryParse(parts[1].Trim(), NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double force)) continue;
                //
                rows.Add((dist, force));
            }
            //
            if (rows.Count == 0)
                throw new Exception("No valid numeric rows found in the CSV file.\n" +
                    "Expected two columns: " +
                    (DistanceType == BladeAeroDistanceTypeEnum.Absolute
                     ? "distance from the blade root (model units)" : "span ratio (0-1 or %)") +
                    " and force/pressure, separated by comma or semicolon.");
            //
            _rows = rows.ToArray();
            //
            dgvSections.Rows.Clear();
            foreach (var row in _rows)
                dgvSections.Rows.Add(
                    row.distance.ToString("G6", CultureInfo.InvariantCulture),
                    row.force.ToString("G6", CultureInfo.InvariantCulture));
            //
            lblStatus.Text = string.Format("{0} row(s) loaded.", _rows.Length);
        }
        private void SetupColumns()
        {
            dgvSections.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDist",
                HeaderText = "Spine coordinate (model units)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            dgvSections.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colForce",
                HeaderText = "Force (N) / Pressure (Pa)",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
        }
        private void PopulateSurfaces()
        {
            var mesh = _controller.DisplayedMesh;
            if (mesh == null) return;
            //
            // Show all user-visible (non-internal) element-type surfaces — same items that
            // appear under "Surfaces" in the model tree
            foreach (var entry in mesh.Surfaces)
            {
                FeSurface s = entry.Value;
                if (!s.Internal && s.Type == FeSurfaceType.Element)
                    cmbFaceSurface.Items.Add(s.Name);
            }
            if (cmbFaceSurface.Items.Count > 0)
                cmbFaceSurface.SelectedIndex = 0;
        }
        private void InitializeComponent()
        {
            this.lDistanceType = new Label();
            this.cmbDistanceType = new ComboBox();
            this.btnLoadFromFile = new Button();
            this.dgvSections = new DataGridView();
            this.lblStatus = new Label();
            this.cbPressureMode = new CheckBox();
            this.lFaceSurface = new Label();
            this.cmbFaceSurface = new ComboBox();
            this.btnOK = new Button();
            this.btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSections)).BeginInit();
            this.SuspendLayout();
            //
            // lDistanceType
            //
            this.lDistanceType.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.lDistanceType.Location = new Point(12, 14);
            this.lDistanceType.Name = "lDistanceType";
            this.lDistanceType.Size = new Size(88, 22);
            this.lDistanceType.Text = "Distance type:";
            this.lDistanceType.TextAlign = ContentAlignment.MiddleLeft;
            //
            // cmbDistanceType
            //
            this.cmbDistanceType.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.cmbDistanceType.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbDistanceType.Items.AddRange(new object[] { "Absolute distance (spine coordinate)",
                                                               "Span ratio (0-1 or %)" });
            this.cmbDistanceType.Location = new Point(104, 12);
            this.cmbDistanceType.Name = "cmbDistanceType";
            this.cmbDistanceType.SelectedIndex = 0;
            this.cmbDistanceType.Size = new Size(284, 22);
            this.cmbDistanceType.TabIndex = 0;
            this.cmbDistanceType.SelectedIndexChanged += new EventHandler(this.cmbDistanceType_SelectedIndexChanged);
            //
            // btnLoadFromFile
            //
            this.btnLoadFromFile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.btnLoadFromFile.Location = new Point(12, 44);
            this.btnLoadFromFile.Name = "btnLoadFromFile";
            this.btnLoadFromFile.Size = new Size(376, 26);
            this.btnLoadFromFile.TabIndex = 1;
            this.btnLoadFromFile.Text = "Load from CSV file...";
            this.btnLoadFromFile.UseVisualStyleBackColor = true;
            this.btnLoadFromFile.Click += new EventHandler(this.btnLoadFromFile_Click);
            //
            // dgvSections
            //
            this.dgvSections.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgvSections.AllowUserToAddRows = false;
            this.dgvSections.AllowUserToDeleteRows = false;
            this.dgvSections.AllowUserToResizeRows = false;
            this.dgvSections.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSections.Location = new Point(12, 80);
            this.dgvSections.Name = "dgvSections";
            this.dgvSections.ReadOnly = true;
            this.dgvSections.RowHeadersVisible = false;
            this.dgvSections.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvSections.Size = new Size(376, 200);
            this.dgvSections.TabIndex = 2;
            //
            // lblStatus
            //
            this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lblStatus.Location = new Point(12, 288);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new Size(376, 18);
            this.lblStatus.Text = "No file loaded.";
            //
            // cbPressureMode
            //
            this.cbPressureMode.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.cbPressureMode.Location = new Point(12, 314);
            this.cbPressureMode.Name = "cbPressureMode";
            this.cbPressureMode.Size = new Size(200, 22);
            this.cbPressureMode.TabIndex = 3;
            this.cbPressureMode.Text = "Pressure (DLOAD on face surface)";
            this.cbPressureMode.UseVisualStyleBackColor = true;
            this.cbPressureMode.CheckedChanged += new EventHandler(this.cbPressureMode_CheckedChanged);
            //
            // lFaceSurface
            //
            this.lFaceSurface.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.lFaceSurface.Location = new Point(12, 342);
            this.lFaceSurface.Name = "lFaceSurface";
            this.lFaceSurface.Size = new Size(88, 22);
            this.lFaceSurface.Text = "Face surface:";
            this.lFaceSurface.TextAlign = ContentAlignment.MiddleLeft;
            this.lFaceSurface.Visible = false;
            //
            // cmbFaceSurface
            //
            this.cmbFaceSurface.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.cmbFaceSurface.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cmbFaceSurface.Location = new Point(104, 342);
            this.cmbFaceSurface.Name = "cmbFaceSurface";
            this.cmbFaceSurface.Size = new Size(284, 22);
            this.cmbFaceSurface.TabIndex = 4;
            this.cmbFaceSurface.Visible = false;
            //
            // btnOK
            //
            this.btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnOK.Location = new Point(232, 374);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new Size(75, 26);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new EventHandler(this.btnOK_Click);
            //
            // btnCancel
            //
            this.btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnCancel.DialogResult = DialogResult.Cancel;
            this.btnCancel.Location = new Point(313, 374);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new Size(75, 26);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new EventHandler(this.btnCancel_Click);
            //
            // FrmBladeAerodynamicLoad
            //
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new Size(400, 412);
            this.Controls.Add(this.lDistanceType);
            this.Controls.Add(this.cmbDistanceType);
            this.Controls.Add(this.btnLoadFromFile);
            this.Controls.Add(this.dgvSections);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.cbPressureMode);
            this.Controls.Add(this.lFaceSurface);
            this.Controls.Add(this.cmbFaceSurface);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Font = new Font("Segoe UI", 9F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmBladeAerodynamicLoad";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Blade Aerodynamic Load";
            ((System.ComponentModel.ISupportInitialize)(this.dgvSections)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
