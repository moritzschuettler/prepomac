using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using CaeGlobals;
using CaeModel;


namespace PrePoMax.Forms
{
    public class FrmBladeAeroTable : Form
    {
        // Variables
        private List<BladeAeroTableRow> _rows;
        private BladeAeroDistanceTypeEnum _distanceType;
        private BindingSource _binding;
        private bool _valueChanged;
        private DataGridView _dgvData;
        private CheckBox _chkInvert;
        private Button _btnLoadCsv;
        private Button _btnOk;
        private Button _btnCancel;


        // Properties
        public List<BladeAeroTableRow> TableRows
        {
            get
            {
                if (_valueChanged) return new List<BladeAeroTableRow>(_rows);
                else return _rows;
            }
        }
        public bool InvertForces { get { return _chkInvert.Checked; } }


        // Constructors
        public FrmBladeAeroTable(List<BladeAeroTableRow> rows, bool invertForces = false,
                                 BladeAeroDistanceTypeEnum distanceType = BladeAeroDistanceTypeEnum.Absolute)
        {
            _valueChanged = false;
            _rows = rows != null ? new List<BladeAeroTableRow>(rows) : new List<BladeAeroTableRow>();
            _distanceType = distanceType;
            //
            InitializeComponent();
            //
            _binding = new BindingSource();
            _binding.DataSource = _rows;
            _dgvData.DataSource = _binding;
            //
            _dgvData.Columns["DistanceRatio"].HeaderText =
                _distanceType == BladeAeroDistanceTypeEnum.Absolute
                ? "Distance from origin (model units)" : "Span ratio (0-1 or %)";
            _dgvData.Columns["DistanceRatio"].Width = 180;
            _dgvData.Columns["Force"].HeaderText = "Force (N)";
            _dgvData.Columns["Force"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            foreach (DataGridViewColumn col in _dgvData.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            //
            // Set initial state without firing the handler
            _chkInvert.CheckedChanged -= ChkInvert_CheckedChanged;
            _chkInvert.Checked = invertForces;
            _chkInvert.CheckedChanged += ChkInvert_CheckedChanged;
        }


        // Event handlers
        private void BtnOk_Click(object sender, EventArgs e)
        {
            try
            {
                _dgvData.EndEdit();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void BtnLoadCsv_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog dlg = new OpenFileDialog())
                {
                    dlg.Filter = "CSV files (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    dlg.Title = "Load load forces table from CSV";
                    if (dlg.ShowDialog() != DialogResult.OK) return;
                    //
                    var loaded = ParseCsv(dlg.FileName);
                    // If forces are currently inverted, negate the loaded values too
                    if (_chkInvert.Checked)
                        for (int i = 0; i < loaded.Count; i++)
                            loaded[i] = new BladeAeroTableRow(loaded[i].DistanceRatio, -loaded[i].Force);
                    _rows.Clear();
                    foreach (var r in loaded) _rows.Add(r);
                    _binding.ResetBindings(false);
                    _valueChanged = true;
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void DgvData_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            _valueChanged = true;
        }
        private void ChkInvert_CheckedChanged(object sender, EventArgs e)
        {
            _dgvData.EndEdit();
            for (int i = 0; i < _rows.Count; i++)
                _rows[i] = new BladeAeroTableRow(_rows[i].DistanceRatio, -_rows[i].Force);
            _binding.ResetBindings(false);
            _valueChanged = true;
        }


        // Methods
        private List<BladeAeroTableRow> ParseCsv(string path)
        {
            string[] lines = File.ReadAllLines(path);
            var result = new List<BladeAeroTableRow>();
            //
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//")) continue;
                //
                char sep = line.Contains(";") ? ';' : ',';
                string[] parts = line.Split(sep);
                if (parts.Length < 2) continue;
                //
                string part0 = parts[0].Trim().TrimEnd('%');
                if (!double.TryParse(part0, NumberStyles.Float, CultureInfo.InvariantCulture, out double ratio)) continue;
                if (!double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double force)) continue;
                //
                result.Add(new BladeAeroTableRow(ratio, force));
            }
            //
            if (result.Count == 0)
                throw new CaeException("No valid rows found in the CSV file.\n" +
                    "Expected two columns: " +
                    (_distanceType == BladeAeroDistanceTypeEnum.Absolute
                     ? "distance from the origin (model units)" : "span ratio (0-1 or %)") +
                    " and force (N).");
            return result;
        }
        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            _dgvData = new DataGridView();
            _dgvData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _dgvData.Location = new Point(12, 12);
            _dgvData.Size = new Size(376, 200);
            _dgvData.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            _dgvData.CellValueChanged += DgvData_CellValueChanged;
            //
            _chkInvert = new CheckBox();
            _chkInvert.Text = "Invert forces (negate all values)";
            _chkInvert.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _chkInvert.Location = new Point(12, 224);
            _chkInvert.Size = new Size(220, 20);
            _chkInvert.CheckedChanged += ChkInvert_CheckedChanged;
            //
            _btnLoadCsv = new Button();
            _btnLoadCsv.Text = "Load from CSV...";
            _btnLoadCsv.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _btnLoadCsv.Location = new Point(12, 252);
            _btnLoadCsv.Size = new Size(120, 26);
            _btnLoadCsv.Click += BtnLoadCsv_Click;
            //
            _btnOk = new Button();
            _btnOk.Text = "OK";
            _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnOk.Location = new Point(232, 252);
            _btnOk.Size = new Size(75, 26);
            _btnOk.Click += BtnOk_Click;
            //
            _btnCancel = new Button();
            _btnCancel.Text = "Cancel";
            _btnCancel.DialogResult = DialogResult.Cancel;
            _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _btnCancel.Location = new Point(313, 252);
            _btnCancel.Size = new Size(75, 26);
            //
            this.AutoScaleMode = AutoScaleMode.None;
            this.CancelButton = _btnCancel;
            this.ClientSize = new Size(400, 290);
            this.Font = new Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(416, 328);
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Edit Load Forces Table";
            //
            ((System.ComponentModel.ISupportInitialize)(_dgvData)).BeginInit();
            this.Controls.Add(_dgvData);
            this.Controls.Add(_chkInvert);
            this.Controls.Add(_btnLoadCsv);
            this.Controls.Add(_btnOk);
            this.Controls.Add(_btnCancel);
            ((System.ComponentModel.ISupportInitialize)(_dgvData)).EndInit();
            //
            this.ResumeLayout(false);
        }
    }
}
