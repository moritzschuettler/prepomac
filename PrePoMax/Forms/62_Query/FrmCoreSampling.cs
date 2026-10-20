using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;
using System.Collections.Generic;
using CaeModel;

namespace PrePoMax.Forms
{
    public class FrmCoreSampling : UserControls.PrePoMaxChildForm
    {
        // Variables
        private Controller _controller;

        // Controls
        private GroupBox gbSamplingPoint;
        private Label lblX;
        private TextBox tbX;
        private Label lblY;
        private TextBox tbY;
        private Label lblZ;
        private TextBox tbZ;
        private CheckBox chkPick;
        private Button btnSample;
        private CheckBox cbRealFiberAngle;
        private GroupBox gbLayers;
        private DataGridView dgvLayers;
        private Label lblTotalThickness;
        private Button btnClose;


        // Constructors
        public FrmCoreSampling()
        {
            InitializeComponent();
            SetupColumns();
        }


        // Event handlers
        private void chkPick_CheckedChanged(object sender, EventArgs e)
        {
            if (chkPick.Checked)
            {
                chkPick.BackColor = Color.LightGreen;
                if (_controller != null)
                {
                    _controller.SelectBy = vtkSelectBy.Node;
                    _controller.Selection.SelectItem = vtkSelectItem.Node;
                }
            }
            else
            {
                chkPick.BackColor = SystemColors.Control;
                if (_controller != null) _controller.SetSelectByToOff();
            }
        }
        private void btnSample_Click(object sender, EventArgs e)
        {
            Sample();
            DrawSamplingPointSymbol();
        }
        private void cbRealFiberAngle_CheckedChanged(object sender, EventArgs e)
        {
            dgvLayers.Columns["colRealAngle"].Visible = cbRealFiberAngle.Checked;
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            Hide();
        }
        private void FrmCoreSampling_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
        private void FrmCoreSampling_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible)
            {
                chkPick.Checked = false;
                chkPick.BackColor = SystemColors.Control;
                if (_controller != null)
                {
                    _controller.SetSelectByToOff();
                    _controller.ClearCoreSamplingPoint();
                }
            }
        }


        // Methods
        public void PrepareForm(Controller controller)
        {
            _controller = controller;
            chkPick.Checked = false;
            chkPick.BackColor = SystemColors.Control;
            _controller.SetSelectByToOff();
            _controller.ClearCoreSamplingPoint();
            dgvLayers.Rows.Clear();
            lblTotalThickness.Text = "Total thickness: -";
        }
        public void PickedIds(int[] ids)
        {
            if (!chkPick.Checked) return;
            if (ids == null || ids.Length == 0) return;
            try
            {
                double[] coor = _controller.GetNode(ids[0]).Coor;
                double tol = 1e-10;
                tbX.Text = (Math.Abs(coor[0]) < tol ? 0.0 : coor[0]).ToString("G6", CultureInfo.InvariantCulture);
                tbY.Text = (Math.Abs(coor[1]) < tol ? 0.0 : coor[1]).ToString("G6", CultureInfo.InvariantCulture);
                tbZ.Text = (Math.Abs(coor[2]) < tol ? 0.0 : coor[2]).ToString("G6", CultureInfo.InvariantCulture);
                _controller.ClearSelectionHistoryAndCallSelectionChanged();
                Sample();
                DrawSamplingPointSymbol();
            }
            catch { }
        }
        private void DrawSamplingPointSymbol()
        {
            if (_controller == null) return;
            //
            if (!double.TryParse(tbX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) ||
                !double.TryParse(tbY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) ||
                !double.TryParse(tbZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
                _controller.ClearCoreSamplingPoint();
                return;
            }
            //
            _controller.DrawCoreSamplingPoint(new double[] { x, y, z });
        }
        private void Sample()
        {
            try
            {
                if (!double.TryParse(tbX.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double x)) return;
                if (!double.TryParse(tbY.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double y)) return;
                if (!double.TryParse(tbZ.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double z)) return;
                //
                dgvLayers.Rows.Clear();
                lblTotalThickness.Text = "Total thickness: -";
                //
                FeMesh mesh = _controller.DisplayedMesh;
                if (mesh == null) return;
                //
                // Collect result groups; each group has a Z for sorting, a solid flag, rows, and thickness sum
                var groups = new List<(double SortZ, bool IsSolid, List<object[]> Rows, double Thickness)>();
                //
                // Composite layup groups
                CompositeLayup[] layups = _controller.GetAllCompositeLayups();
                if (layups != null)
                {
                    foreach (CompositeLayup layup in layups)
                    {
                        int nearestElemId = FindNearestElement(mesh, layup, x, y, z);
                        if (nearestElemId < 0) continue;
                        //
                        double[] cg = mesh.Elements[nearestElemId].GetCG(mesh.Nodes, out _);
                        double sortZ = cg[2];
                        //
                        var rows = new List<object[]>();
                        double groupThk = 0.0;
                        for (int i = 0; i < layup.Plies.Count; i++)
                        {
                            Ply ply = layup.Plies[i];
                            if (string.IsNullOrEmpty(ply.Region)) continue;
                            if (!mesh.ElementSets.ContainsKey(ply.Region)) continue;
                            //
                            bool inRegion = false;
                            foreach (int eid in mesh.ElementSets[ply.Region].Labels)
                            {
                                if (eid == nearestElemId) { inRegion = true; break; }
                            }
                            if (!inRegion) continue;
                            //
                            string realAngle = GetRealFiberAngle(layup, i, x, y, z);
                            rows.Add(new object[] {
                                layup.Name, i + 1, ply.Material,
                                ply.Thickness.ToString("G6", CultureInfo.InvariantCulture),
                                ply.RotationAngle.ToString("G6", CultureInfo.InvariantCulture),
                                realAngle
                            });
                            groupThk += ply.Thickness;
                        }
                        if (rows.Count > 0)
                            groups.Add((sortZ, false, rows, groupThk));
                    }
                }
                //
                // Solid mesh groups (one row per part, no ply detail)
                foreach (var entry in mesh.Parts)
                {
                    BasePart part = entry.Value;
                    if (part.PartType != PartType.Solid && part.PartType != PartType.SolidAsShell) continue;
                    if (part.Labels == null || part.Labels.Length == 0) continue;
                    //
                    int nearestElemId = FindNearestElementByLabels(mesh, part.Labels, x, y, z);
                    if (nearestElemId < 0) continue;
                    //
                    double[] cg = mesh.Elements[nearestElemId].GetCG(mesh.Nodes, out _);
                    double sortZ = cg[2];
                    var solidRows = new List<object[]>();
                    solidRows.Add(new object[] { part.Name, "-", "-", "-", "-", "-" });
                    groups.Add((sortZ, true, solidRows, 0.0));
                }
                //
                // Sort by ascending Z (bottom-to-top stacking order)
                groups.Sort((a, b) => a.SortZ.CompareTo(b.SortZ));
                //
                // Populate grid
                double totalThickness = 0.0;
                foreach (var g in groups)
                {
                    foreach (object[] rowData in g.Rows)
                    {
                        int rowIdx = dgvLayers.Rows.Add(rowData);
                        if (g.IsSolid)
                        {
                            dgvLayers.Rows[rowIdx].DefaultCellStyle.BackColor = Color.LightSteelBlue;
                            dgvLayers.Rows[rowIdx].DefaultCellStyle.ForeColor = Color.DimGray;
                        }
                    }
                    totalThickness += g.Thickness;
                }
                //
                if (dgvLayers.Rows.Count > 0)
                    lblTotalThickness.Text = string.Format(
                        CultureInfo.InvariantCulture, "Total thickness: {0:G6} mm", totalThickness);
            }
            catch { }
        }
        private int FindNearestElement(FeMesh mesh, CompositeLayup layup, double x, double y, double z)
        {
            double minDist2 = double.MaxValue;
            int nearestElemId = -1;
            //
            foreach (Ply ply in layup.Plies)
            {
                if (string.IsNullOrEmpty(ply.Region)) continue;
                if (!mesh.ElementSets.ContainsKey(ply.Region)) continue;
                //
                foreach (int eid in mesh.ElementSets[ply.Region].Labels)
                {
                    double[] cg = mesh.Elements[eid].GetCG(mesh.Nodes, out _);
                    double dx = cg[0] - x;
                    double dy = cg[1] - y;
                    double dz = cg[2] - z;
                    double dist2 = dx * dx + dy * dy + dz * dz;
                    if (dist2 < minDist2) { minDist2 = dist2; nearestElemId = eid; }
                }
            }
            return nearestElemId;
        }
        private int FindNearestElementByLabels(FeMesh mesh, int[] labels, double x, double y, double z)
        {
            double minDist2 = double.MaxValue;
            int nearestElemId = -1;
            foreach (int eid in labels)
            {
                if (!mesh.Elements.TryGetValue(eid, out FeElement element)) continue;
                double[] cg = element.GetCG(mesh.Nodes, out _);
                double dx = cg[0] - x, dy = cg[1] - y, dz = cg[2] - z;
                double dist2 = dx * dx + dy * dy + dz * dz;
                if (dist2 < minDist2) { minDist2 = dist2; nearestElemId = eid; }
            }
            return nearestElemId;
        }
        private string GetRealFiberAngle(CompositeLayup layup, int plyIndex, double x, double y, double z)
        {
            // DiscreteOrientations: spatially varying angles from stacking data
            if (layup.OrientationDefinition == OrientationDefinitionType.DiscreteOrientations &&
                layup.DiscreteOrientationsData != null &&
                layup.DiscreteOrientationsData.Points != null &&
                layup.DiscreteOrientationsData.Points.Count > 0)
            {
                StackingPoint nearest = FindNearestStackingPoint(layup.DiscreteOrientationsData, x, y, z);
                if (nearest != null && plyIndex < nearest.Layers.Count)
                    return nearest.Layers[plyIndex].Angle.ToString("G6", CultureInfo.InvariantCulture);
                return "N/A";
            }
            // PartGlobal / CoordinateSystem: angle relative to the ply's local CS reference direction
            return layup.Plies[plyIndex].RotationAngle.ToString("G6", CultureInfo.InvariantCulture);
        }
        private StackingPoint FindNearestStackingPoint(DiscreteOrientationsData data, double x, double y, double z)
        {
            double minDist2 = double.MaxValue;
            StackingPoint nearest = null;
            foreach (StackingPoint pt in data.Points)
            {
                double dx = pt.Position[0] - x;
                double dy = pt.Position[1] - y;
                double dz = pt.Position[2] - z;
                double dist2 = dx * dx + dy * dy + dz * dz;
                if (dist2 < minDist2) { minDist2 = dist2; nearest = pt; }
            }
            return nearest;
        }
        private void SetupColumns()
        {
            dgvLayers.AutoGenerateColumns = false;
            //
            var colLayup = new DataGridViewTextBoxColumn();
            colLayup.Name = "colLayup";
            colLayup.HeaderText = "Layup";
            colLayup.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colLayup.ReadOnly = true;
            dgvLayers.Columns.Add(colLayup);
            //
            dgvLayers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colLayer", HeaderText = "Layer", Width = 40, ReadOnly = true
            });
            dgvLayers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colMaterial", HeaderText = "Material", Width = 75, ReadOnly = true
            });
            dgvLayers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colThickness", HeaderText = "Thickness", Width = 60, ReadOnly = true
            });
            dgvLayers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAngle", HeaderText = "Angle [°]", Width = 60, ReadOnly = true
            });
            //
            var colRealAngle = new DataGridViewTextBoxColumn();
            colRealAngle.Name = "colRealAngle";
            colRealAngle.HeaderText = "Real Angle [°]";
            colRealAngle.Width = 80;
            colRealAngle.ReadOnly = true;
            colRealAngle.Visible = false;
            colRealAngle.ToolTipText =
                "Fiber angle relative to the ply's local CS reference direction. " +
                "Discrete orientations: nearest stacking point angle. " +
                "Other: ply rotation angle.";
            dgvLayers.Columns.Add(colRealAngle);
        }
        private void InitializeComponent()
        {
            this.gbSamplingPoint = new GroupBox();
            this.lblX = new Label();
            this.tbX = new TextBox();
            this.lblY = new Label();
            this.tbY = new TextBox();
            this.lblZ = new Label();
            this.tbZ = new TextBox();
            this.chkPick = new CheckBox();
            this.btnSample = new Button();
            this.cbRealFiberAngle = new CheckBox();
            this.gbLayers = new GroupBox();
            this.dgvLayers = new DataGridView();
            this.lblTotalThickness = new Label();
            this.btnClose = new Button();
            this.gbSamplingPoint.SuspendLayout();
            this.gbLayers.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayers)).BeginInit();
            this.SuspendLayout();
            //
            // gbSamplingPoint
            //
            this.gbSamplingPoint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.gbSamplingPoint.Controls.Add(this.lblX);
            this.gbSamplingPoint.Controls.Add(this.tbX);
            this.gbSamplingPoint.Controls.Add(this.lblY);
            this.gbSamplingPoint.Controls.Add(this.tbY);
            this.gbSamplingPoint.Controls.Add(this.lblZ);
            this.gbSamplingPoint.Controls.Add(this.tbZ);
            this.gbSamplingPoint.Controls.Add(this.chkPick);
            this.gbSamplingPoint.Controls.Add(this.btnSample);
            this.gbSamplingPoint.Location = new Point(12, 12);
            this.gbSamplingPoint.Name = "gbSamplingPoint";
            this.gbSamplingPoint.Size = new Size(426, 148);
            this.gbSamplingPoint.TabIndex = 0;
            this.gbSamplingPoint.TabStop = false;
            this.gbSamplingPoint.Text = "Sampling Point";
            //
            // lblX
            //
            this.lblX.AutoSize = true;
            this.lblX.Location = new Point(6, 30);
            this.lblX.Name = "lblX";
            this.lblX.Text = "X:";
            //
            // tbX
            //
            this.tbX.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tbX.Location = new Point(25, 27);
            this.tbX.Name = "tbX";
            this.tbX.Size = new Size(395, 23);
            this.tbX.TabIndex = 0;
            this.tbX.Text = "0";
            //
            // lblY
            //
            this.lblY.AutoSize = true;
            this.lblY.Location = new Point(6, 59);
            this.lblY.Name = "lblY";
            this.lblY.Text = "Y:";
            //
            // tbY
            //
            this.tbY.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tbY.Location = new Point(25, 56);
            this.tbY.Name = "tbY";
            this.tbY.Size = new Size(395, 23);
            this.tbY.TabIndex = 1;
            this.tbY.Text = "0";
            //
            // lblZ
            //
            this.lblZ.AutoSize = true;
            this.lblZ.Location = new Point(6, 88);
            this.lblZ.Name = "lblZ";
            this.lblZ.Text = "Z:";
            //
            // tbZ
            //
            this.tbZ.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.tbZ.Location = new Point(25, 85);
            this.tbZ.Name = "tbZ";
            this.tbZ.Size = new Size(395, 23);
            this.tbZ.TabIndex = 2;
            this.tbZ.Text = "0";
            //
            // chkPick
            //
            this.chkPick.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.chkPick.Appearance = Appearance.Button;
            this.chkPick.Location = new Point(240, 114);
            this.chkPick.Name = "chkPick";
            this.chkPick.Size = new Size(85, 23);
            this.chkPick.TabIndex = 3;
            this.chkPick.Text = "Pick";
            this.chkPick.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkPick.UseVisualStyleBackColor = false;
            this.chkPick.CheckedChanged += new EventHandler(this.chkPick_CheckedChanged);
            //
            // btnSample
            //
            this.btnSample.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.btnSample.Location = new Point(333, 114);
            this.btnSample.Name = "btnSample";
            this.btnSample.Size = new Size(87, 23);
            this.btnSample.TabIndex = 4;
            this.btnSample.Text = "Sample";
            this.btnSample.UseVisualStyleBackColor = true;
            this.btnSample.Click += new EventHandler(this.btnSample_Click);
            //
            // cbRealFiberAngle
            //
            this.cbRealFiberAngle.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            this.cbRealFiberAngle.AutoSize = true;
            this.cbRealFiberAngle.Location = new Point(12, 168);
            this.cbRealFiberAngle.Name = "cbRealFiberAngle";
            this.cbRealFiberAngle.TabIndex = 5;
            this.cbRealFiberAngle.Text = "Real fiber angle (from local rosette)";
            this.cbRealFiberAngle.UseVisualStyleBackColor = true;
            this.cbRealFiberAngle.CheckedChanged += new EventHandler(this.cbRealFiberAngle_CheckedChanged);
            //
            // gbLayers
            //
            this.gbLayers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.gbLayers.Controls.Add(this.dgvLayers);
            this.gbLayers.Controls.Add(this.lblTotalThickness);
            this.gbLayers.Location = new Point(12, 193);
            this.gbLayers.Name = "gbLayers";
            this.gbLayers.Size = new Size(426, 258);
            this.gbLayers.TabIndex = 1;
            this.gbLayers.TabStop = false;
            this.gbLayers.Text = "Layers";
            //
            // dgvLayers
            //
            this.dgvLayers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.dgvLayers.AllowUserToAddRows = false;
            this.dgvLayers.AllowUserToDeleteRows = false;
            this.dgvLayers.AllowUserToResizeRows = false;
            this.dgvLayers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvLayers.Location = new Point(6, 22);
            this.dgvLayers.Name = "dgvLayers";
            this.dgvLayers.ReadOnly = true;
            this.dgvLayers.RowHeadersVisible = false;
            this.dgvLayers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            this.dgvLayers.Size = new Size(414, 211);
            this.dgvLayers.TabIndex = 0;
            //
            // lblTotalThickness
            //
            this.lblTotalThickness.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lblTotalThickness.Location = new Point(6, 237);
            this.lblTotalThickness.Name = "lblTotalThickness";
            this.lblTotalThickness.Size = new Size(414, 17);
            this.lblTotalThickness.Text = "Total thickness: -";
            //
            // btnClose
            //
            this.btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnClose.DialogResult = DialogResult.Cancel;
            this.btnClose.Location = new Point(363, 457);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new Size(75, 23);
            this.btnClose.TabIndex = 6;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new EventHandler(this.btnClose_Click);
            //
            // FrmCoreSampling
            //
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new Size(450, 492);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.cbRealFiberAngle);
            this.Controls.Add(this.gbLayers);
            this.Controls.Add(this.gbSamplingPoint);
            this.Font = new Font("Segoe UI", 9F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new Size(466, 531);
            this.Name = "FrmCoreSampling";
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.Text = "Core Sampling";
            this.FormClosing += new FormClosingEventHandler(this.FrmCoreSampling_FormClosing);
            this.VisibleChanged += new EventHandler(this.FrmCoreSampling_VisibleChanged);
            this.gbSamplingPoint.ResumeLayout(false);
            this.gbSamplingPoint.PerformLayout();
            this.gbLayers.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvLayers)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
