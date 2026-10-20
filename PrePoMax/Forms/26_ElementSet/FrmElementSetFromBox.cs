using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using CaeMesh;
using CaeGlobals;

namespace PrePoMax.Forms
{
    class FrmElementSetFromBox : UserControls.PrePoMaxChildForm, IFormBase
    {
        // Variables
        private string _partName;
        private string _editingSetName;
        private HashSet<string> _allEntityNames;
        private Controller _controller;
        private bool _bbInitialized;

        // Controls
        private GroupBox gbData;
        private Label lName;
        private TextBox tbName;
        private Label lMode;
        private ComboBox cbMode;
        private GroupBox gbBoundingBox;
        private Label lCenterX;
        private NumericUpDown nudCenterX;
        private Label lCenterY;
        private NumericUpDown nudCenterY;
        private Label lCenterZ;
        private NumericUpDown nudCenterZ;
        private Label lSizeX;
        private NumericUpDown nudSizeX;
        private Label lSizeY;
        private NumericUpDown nudSizeY;
        private Label lSizeZ;
        private NumericUpDown nudSizeZ;
        private Button btnSelectElements;
        private Label lblCount;
        private Button btnOK;
        private Button btnCancel;


        // Constructors
        public FrmElementSetFromBox(Controller controller)
        {
            InitializeComponent();
            _controller = controller;
        }
        private void InitializeComponent()
        {
            gbData = new GroupBox();
            lName = new Label();
            tbName = new TextBox();
            lMode = new Label();
            cbMode = new ComboBox();
            gbBoundingBox = new GroupBox();
            lCenterX = new Label();
            nudCenterX = new NumericUpDown();
            lCenterY = new Label();
            nudCenterY = new NumericUpDown();
            lCenterZ = new Label();
            nudCenterZ = new NumericUpDown();
            lSizeX = new Label();
            nudSizeX = new NumericUpDown();
            lSizeY = new Label();
            nudSizeY = new NumericUpDown();
            lSizeZ = new Label();
            nudSizeZ = new NumericUpDown();
            btnSelectElements = new Button();
            lblCount = new Label();
            btnOK = new Button();
            btnCancel = new Button();

            gbData.SuspendLayout();
            gbBoundingBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(nudCenterX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudCenterY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudCenterZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeZ)).BeginInit();
            this.SuspendLayout();

            // gbData
            gbData.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbData.Controls.Add(lName);
            gbData.Controls.Add(tbName);
            gbData.Controls.Add(lMode);
            gbData.Controls.Add(cbMode);
            gbData.Location = new Point(12, 12);
            gbData.Name = "gbData";
            gbData.Size = new Size(310, 82);
            gbData.TabIndex = 0;
            gbData.TabStop = false;
            gbData.Text = "Data";

            lName.AutoSize = true;
            lName.Location = new Point(10, 25);
            lName.Name = "lName";
            lName.Text = "Name:";

            tbName.Location = new Point(110, 22);
            tbName.Name = "tbName";
            tbName.Size = new Size(190, 23);
            tbName.TabIndex = 0;

            lMode.AutoSize = true;
            lMode.Location = new Point(10, 53);
            lMode.Name = "lMode";
            lMode.Text = "Selection:";

            cbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMode.Location = new Point(110, 50);
            cbMode.Name = "cbMode";
            cbMode.Size = new Size(190, 23);
            cbMode.TabIndex = 1;
            cbMode.Items.AddRange(new object[] { "All", "From Bounding Box" });
            cbMode.SelectedIndexChanged += new EventHandler(cbMode_SelectedIndexChanged);

            // gbBoundingBox
            gbBoundingBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            gbBoundingBox.Controls.Add(lCenterX);
            gbBoundingBox.Controls.Add(nudCenterX);
            gbBoundingBox.Controls.Add(lCenterY);
            gbBoundingBox.Controls.Add(nudCenterY);
            gbBoundingBox.Controls.Add(lCenterZ);
            gbBoundingBox.Controls.Add(nudCenterZ);
            gbBoundingBox.Controls.Add(lSizeX);
            gbBoundingBox.Controls.Add(nudSizeX);
            gbBoundingBox.Controls.Add(lSizeY);
            gbBoundingBox.Controls.Add(nudSizeY);
            gbBoundingBox.Controls.Add(lSizeZ);
            gbBoundingBox.Controls.Add(nudSizeZ);
            gbBoundingBox.Location = new Point(12, 100);
            gbBoundingBox.Name = "gbBoundingBox";
            gbBoundingBox.Size = new Size(310, 205);
            gbBoundingBox.TabIndex = 1;
            gbBoundingBox.TabStop = false;
            gbBoundingBox.Text = "Bounding Box";
            gbBoundingBox.Visible = false;

            lCenterX.AutoSize = true;
            lCenterX.Location = new Point(10, 26);
            lCenterX.Text = "Center X:";

            nudCenterX.Location = new Point(110, 23);
            nudCenterX.Size = new Size(130, 23);
            nudCenterX.TabIndex = 2;
            nudCenterX.DecimalPlaces = 4;
            nudCenterX.Minimum = -1000000000;
            nudCenterX.Maximum = 1000000000;
            nudCenterX.Increment = 0.01m;
            nudCenterX.ValueChanged += new EventHandler(NudBB_ValueChanged);

            lCenterY.AutoSize = true;
            lCenterY.Location = new Point(10, 53);
            lCenterY.Text = "Center Y:";

            nudCenterY.Location = new Point(110, 50);
            nudCenterY.Size = new Size(130, 23);
            nudCenterY.TabIndex = 3;
            nudCenterY.DecimalPlaces = 4;
            nudCenterY.Minimum = -1000000000;
            nudCenterY.Maximum = 1000000000;
            nudCenterY.Increment = 0.01m;
            nudCenterY.ValueChanged += new EventHandler(NudBB_ValueChanged);

            lCenterZ.AutoSize = true;
            lCenterZ.Location = new Point(10, 80);
            lCenterZ.Text = "Center Z:";

            nudCenterZ.Location = new Point(110, 77);
            nudCenterZ.Size = new Size(130, 23);
            nudCenterZ.TabIndex = 4;
            nudCenterZ.DecimalPlaces = 4;
            nudCenterZ.Minimum = -1000000000;
            nudCenterZ.Maximum = 1000000000;
            nudCenterZ.Increment = 0.01m;
            nudCenterZ.ValueChanged += new EventHandler(NudBB_ValueChanged);

            lSizeX.AutoSize = true;
            lSizeX.Location = new Point(10, 113);
            lSizeX.Text = "Size X:";

            nudSizeX.Location = new Point(110, 110);
            nudSizeX.Size = new Size(130, 23);
            nudSizeX.TabIndex = 5;
            nudSizeX.DecimalPlaces = 4;
            nudSizeX.Minimum = 0.0001m;
            nudSizeX.Maximum = 1000000000;
            nudSizeX.Increment = 0.01m;
            nudSizeX.ValueChanged += new EventHandler(NudBB_ValueChanged);

            lSizeY.AutoSize = true;
            lSizeY.Location = new Point(10, 140);
            lSizeY.Text = "Size Y:";

            nudSizeY.Location = new Point(110, 137);
            nudSizeY.Size = new Size(130, 23);
            nudSizeY.TabIndex = 6;
            nudSizeY.DecimalPlaces = 4;
            nudSizeY.Minimum = 0.0001m;
            nudSizeY.Maximum = 1000000000;
            nudSizeY.Increment = 0.01m;
            nudSizeY.ValueChanged += new EventHandler(NudBB_ValueChanged);

            lSizeZ.AutoSize = true;
            lSizeZ.Location = new Point(10, 167);
            lSizeZ.Text = "Size Z:";

            nudSizeZ.Location = new Point(110, 164);
            nudSizeZ.Size = new Size(130, 23);
            nudSizeZ.TabIndex = 7;
            nudSizeZ.DecimalPlaces = 4;
            nudSizeZ.Minimum = 0.0001m;
            nudSizeZ.Maximum = 1000000000;
            nudSizeZ.Increment = 0.01m;
            nudSizeZ.ValueChanged += new EventHandler(NudBB_ValueChanged);

            // btnSelectElements
            btnSelectElements.Location = new Point(12, 313);
            btnSelectElements.Name = "btnSelectElements";
            btnSelectElements.Size = new Size(195, 26);
            btnSelectElements.TabIndex = 8;
            btnSelectElements.Text = "Select Elements in Box";
            btnSelectElements.Click += new EventHandler(btnSelectElements_Click);

            // lblCount
            lblCount.AutoSize = true;
            lblCount.Location = new Point(215, 319);
            lblCount.Name = "lblCount";
            lblCount.Text = "0 elements";

            // btnOK
            btnOK.Location = new Point(90, 349);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(70, 26);
            btnOK.TabIndex = 9;
            btnOK.Text = "OK";
            btnOK.Click += new EventHandler(btnOK_Click);

            // btnCancel
            btnCancel.Location = new Point(172, 349);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(70, 26);
            btnCancel.TabIndex = 10;
            btnCancel.Text = "Cancel";
            btnCancel.Click += new EventHandler(btnCancel_Click);

            gbData.ResumeLayout(false);
            gbData.PerformLayout();
            gbBoundingBox.ResumeLayout(false);
            gbBoundingBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(nudCenterX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudCenterY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudCenterZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(nudSizeZ)).EndInit();

            // Form
            this.ClientSize = new Size(334, 387);
            this.Controls.Add(gbData);
            this.Controls.Add(gbBoundingBox);
            this.Controls.Add(btnSelectElements);
            this.Controls.Add(lblCount);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmElementSetFromBox";
            this.Text = "Element Set From Box";
            this.FormClosing += new FormClosingEventHandler(FrmElementSetFromBox_FormClosing);

            this.ResumeLayout(false);
        }


        // IFormBase
        public bool PrepareForm(string stepName, string itemToEdit)
        {
            _partName = null;
            _editingSetName = null;
            _allEntityNames = null;
            _bbInitialized = false;
            lblCount.Text = "0 elements";

            if (itemToEdit == null) return false;

            _allEntityNames = new HashSet<string>(_controller.GetAllMeshEntityNames());

            if (_controller.Model?.Mesh?.ElementSets != null &&
                _controller.Model.Mesh.ElementSets.TryGetValue(itemToEdit, out FeElementSet es) &&
                es is FeElementSetFromBox boxSet)
            {
                // Edit mode
                _editingSetName = itemToEdit;
                _partName = boxSet.PartName;
                tbName.Text = boxSet.Name;
                _allEntityNames.Remove(boxSet.Name);
                SetMode(boxSet.Mode);
                if (boxSet.Mode == FeElementSetFromBox.BoxSelectionMode.FromBoundingBox)
                {
                    _bbInitialized = true;
                    SetNudValuesWithoutEvent(boxSet.CenterX, boxSet.CenterY, boxSet.CenterZ,
                                            boxSet.SizeX, boxSet.SizeY, boxSet.SizeZ);
                    UpdatePreview();
                    UpdateBoundingBoxCount();
                }
            }
            else if (_controller.Model?.Mesh?.Parts != null &&
                     _controller.Model.Mesh.Parts.ContainsKey(itemToEdit))
            {
                // Create mode
                _partName = itemToEdit;
                tbName.Text = _allEntityNames.GetNextNumberedKey("Element_Set");
                SetMode(FeElementSetFromBox.BoxSelectionMode.All);
            }
            else
            {
                return false;
            }

            return true;
        }


        // Event handlers
        private void cbMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool fromBox = cbMode.SelectedIndex == 1;
            gbBoundingBox.Visible = fromBox;
            btnSelectElements.Enabled = fromBox;
            lblCount.Text = "0 elements";

            if (fromBox && !_bbInitialized)
            {
                _bbInitialized = true;
                SetDefaultBB();
                UpdatePreview();
            }
            else if (fromBox)
            {
                UpdatePreview();
                UpdateBoundingBoxCount();
            }
            else
            {
                _controller.ClearBoundingBoxPreview();
                UpdateAllModeCount();
            }
        }
        private void NudBB_ValueChanged(object sender, EventArgs e)
        {
            if (gbBoundingBox.Visible) UpdatePreview();
        }
        private void btnSelectElements_Click(object sender, EventArgs e)
        {
            try
            {
                int[] ids = _controller.GetElementsInBox(_partName,
                    (double)nudCenterX.Value, (double)nudCenterY.Value, (double)nudCenterZ.Value,
                    (double)nudSizeX.Value, (double)nudSizeY.Value, (double)nudSizeZ.Value);
                _controller.HighlightElements(ids);
                lblCount.Text = ids.Length + " elements";
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.Message);
            }
        }
        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                string name = tbName.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    throw new CaeException("The element set name must not be empty.");
                if (_allEntityNames != null && _allEntityNames.Contains(name))
                    throw new CaeException("The name '" + name + "' is already used.");

                FeElementSetFromBox.BoxSelectionMode mode = cbMode.SelectedIndex == 1
                    ? FeElementSetFromBox.BoxSelectionMode.FromBoundingBox
                    : FeElementSetFromBox.BoxSelectionMode.All;

                double cx = 0, cy = 0, cz = 0, dx = 1, dy = 1, dz = 1;
                if (mode == FeElementSetFromBox.BoxSelectionMode.FromBoundingBox)
                {
                    cx = (double)nudCenterX.Value;
                    cy = (double)nudCenterY.Value;
                    cz = (double)nudCenterZ.Value;
                    dx = (double)nudSizeX.Value;
                    dy = (double)nudSizeY.Value;
                    dz = (double)nudSizeZ.Value;
                }

                FeElementSetFromBox newSet = new FeElementSetFromBox(name, _partName, mode, cx, cy, cz, dx, dy, dz);

                if (mode == FeElementSetFromBox.BoxSelectionMode.All)
                    newSet.Labels = _controller.GetAllElementIdsFromPart(_partName);
                else
                    newSet.Labels = _controller.GetElementsInBox(_partName, cx, cy, cz, dx, dy, dz);

                if (newSet.Labels == null || newSet.Labels.Length == 0)
                    throw new CaeException("No elements found for the selected criteria.");

                if (_editingSetName == null)
                    _controller.AddElementSetCommand(newSet);
                else
                    _controller.ReplaceElementSetCommand(_editingSetName, newSet);

                _controller.ClearBoundingBoxPreview();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBoxes.ShowError(ex.Message);
            }
        }
        private void btnCancel_Click(object sender, EventArgs e)
        {
            CloseForm();
        }
        private void FrmElementSetFromBox_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                CloseForm();
            }
        }


        // Methods
        private void CloseForm()
        {
            _controller.ClearBoundingBoxPreview();
            _controller.Redraw();
            this.Hide();
        }
        private void SetMode(FeElementSetFromBox.BoxSelectionMode mode)
        {
            cbMode.SelectedIndexChanged -= cbMode_SelectedIndexChanged;
            cbMode.SelectedIndex = mode == FeElementSetFromBox.BoxSelectionMode.All ? 0 : 1;
            cbMode.SelectedIndexChanged += cbMode_SelectedIndexChanged;
            bool fromBox = mode == FeElementSetFromBox.BoxSelectionMode.FromBoundingBox;
            gbBoundingBox.Visible = fromBox;
            btnSelectElements.Enabled = fromBox;
            if (!fromBox) UpdateAllModeCount();
        }
        private void UpdateAllModeCount()
        {
            if (_partName == null) return;
            try
            {
                int count = _controller.GetAllElementIdsFromPart(_partName).Length;
                lblCount.Text = count + " elements";
            }
            catch { lblCount.Text = "0 elements"; }
        }
        private void UpdateBoundingBoxCount()
        {
            if (_partName == null) return;
            try
            {
                int[] ids = _controller.GetElementsInBox(_partName,
                    (double)nudCenterX.Value, (double)nudCenterY.Value, (double)nudCenterZ.Value,
                    (double)nudSizeX.Value, (double)nudSizeY.Value, (double)nudSizeZ.Value);
                lblCount.Text = ids.Length + " elements";
            }
            catch { lblCount.Text = "0 elements"; }
        }
        private void SetDefaultBB()
        {
            if (_partName == null) return;
            try
            {
                double[] bb = _controller.GetPartBoundingBox(_partName);
                SetNudValuesWithoutEvent(bb[0], bb[1], bb[2], 100.0, 100.0, 100.0);
            }
            catch
            {
                SetNudValuesWithoutEvent(0, 0, 0, 100.0, 100.0, 100.0);
            }
        }
        private void SetNudValuesWithoutEvent(double cx, double cy, double cz,
                                              double dx, double dy, double dz)
        {
            nudCenterX.ValueChanged -= NudBB_ValueChanged;
            nudCenterY.ValueChanged -= NudBB_ValueChanged;
            nudCenterZ.ValueChanged -= NudBB_ValueChanged;
            nudSizeX.ValueChanged -= NudBB_ValueChanged;
            nudSizeY.ValueChanged -= NudBB_ValueChanged;
            nudSizeZ.ValueChanged -= NudBB_ValueChanged;

            nudCenterX.Value = ClampDecimal(cx);
            nudCenterY.Value = ClampDecimal(cy);
            nudCenterZ.Value = ClampDecimal(cz);
            nudSizeX.Value = ClampDecimal(dx, nudSizeX.Minimum);
            nudSizeY.Value = ClampDecimal(dy, nudSizeY.Minimum);
            nudSizeZ.Value = ClampDecimal(dz, nudSizeZ.Minimum);

            nudCenterX.ValueChanged += NudBB_ValueChanged;
            nudCenterY.ValueChanged += NudBB_ValueChanged;
            nudCenterZ.ValueChanged += NudBB_ValueChanged;
            nudSizeX.ValueChanged += NudBB_ValueChanged;
            nudSizeY.ValueChanged += NudBB_ValueChanged;
            nudSizeZ.ValueChanged += NudBB_ValueChanged;
        }
        private decimal ClampDecimal(double value, decimal minimum = -1000000000)
        {
            decimal d = (decimal)value;
            if (d < minimum) d = minimum;
            if (d > 1000000000) d = 1000000000;
            return d;
        }
        private void UpdatePreview()
        {
            if (_partName == null || !gbBoundingBox.Visible) return;
            try
            {
                _controller.DrawBoundingBoxPreview(
                    (double)nudCenterX.Value, (double)nudCenterY.Value, (double)nudCenterZ.Value,
                    (double)nudSizeX.Value, (double)nudSizeY.Value, (double)nudSizeZ.Value);
            }
            catch { }
        }
    }
}
