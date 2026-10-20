// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors: Yvan Blanchard (Composites Fiber Modeler)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CaeGlobals;
using CaeMesh;
using CaeModel;

namespace PrePoMax.Forms
{
    // Creates and edits a composite layup: the layup data, the plies with their regions, materials, thicknesses, angles and
    // coordinate systems, and the orientation of the fibers from a coordinate system, discrete orientations or a draping
    // simulation
    class FrmCompositeLayup : UserControls.FrmPropertyDataListView, IFormBase, IFormHighlight
    {
        // Variables
        private string[] _compositeLayupNames;
        private string _compositeLayupToEditName;
        private string[] _materialNames;
        private string[] _regionNames;
        private string[] _coordinateSystemNames;
        private ViewCompositeLayup _viewCompositeLayup;
        private Controller _controller;
        private UserControls.DoubleBufferedPanel pnlLayup;
        private Panel pnlButtons;
        private Button btnImportOrientations;
        private Button btnDraping;
        private Label lblDraping;
        private const string _plyColumnName = "PlyNumber";
        private const double _layupHatchSpacing = 8;
        private const int _layupArrowWidth = 28;


        // Properties
        public CompositeLayup CompositeLayup
        {
            get { return _viewCompositeLayup != null ? _viewCompositeLayup.GetBase() : null; }
            set { _viewCompositeLayup = new ViewCompositeLayup(value.DeepClone()); }
        }


        // Constructors
        public FrmCompositeLayup(Controller controller)
        {
            InitializeComponent();
            //
            _controller = controller;
            _viewCompositeLayup = null;
        }
        private void InitializeComponent()
        {
            this.pnlLayup = new UserControls.DoubleBufferedPanel();
            this.pnlButtons = new Panel();
            this.btnImportOrientations = new Button();
            this.btnDraping = new Button();
            this.lblDraping = new Label();
            this.gbType.SuspendLayout();
            this.gbProperties.SuspendLayout();
            this.tpDataPoints.SuspendLayout();
            this.tpProperties.SuspendLayout();
            this.SuspendLayout();
            //
            // tpDataPoints
            //
            this.tpDataPoints.Controls.Add(this.pnlLayup);
            this.tpDataPoints.Text = "Plies";
            //
            // pnlLayup
            //
            this.pnlLayup.BackColor = SystemColors.Window;
            this.pnlLayup.BorderStyle = BorderStyle.FixedSingle;
            this.pnlLayup.Dock = DockStyle.Bottom;
            this.pnlLayup.Name = "pnlLayup";
            this.pnlLayup.Size = new Size(296, 120);
            this.pnlLayup.Paint += new PaintEventHandler(this.pnlLayup_Paint);
            this.pnlLayup.Resize += (s, e) => pnlLayup.Invalidate();
            this.pnlLayup.MouseClick += new MouseEventHandler(this.pnlLayup_MouseClick);
            //
            // pnlButtons: the discrete orientations and the draping preview
            //
            this.pnlButtons.Dock = DockStyle.Bottom;
            this.pnlButtons.Height = 33;
            this.pnlButtons.Controls.Add(this.lblDraping);
            this.pnlButtons.Controls.Add(this.btnDraping);
            this.pnlButtons.Controls.Add(this.btnImportOrientations);
            this.btnImportOrientations.Text = "Import Orientations...";
            this.btnImportOrientations.Location = new Point(3, 5);
            this.btnImportOrientations.Size = new Size(140, 23);
            this.btnImportOrientations.Click += btnImportOrientations_Click;
            this.btnDraping.Text = "Preview Draping";
            this.btnDraping.Location = new Point(147, 5);
            this.btnDraping.Size = new Size(115, 23);
            this.btnDraping.Click += btnDraping_Click;
            this.lblDraping.AutoSize = true;
            this.lblDraping.Location = new Point(266, 9);
            this.lblDraping.Text = "";
            this.tpProperties.Controls.Add(this.pnlButtons);
            //
            // tcProperties
            //
            this.tcProperties.SelectedIndexChanged += new EventHandler(this.tcProperties_SelectedIndexChanged);
            //
            // dgvData
            //
            this.dgvData.EnablePlotMenu = false;
            this.dgvData.SelectionChanged += new EventHandler(this.dgvData_SelectionChanged);
            this.dgvData.CellFormatting += new DataGridViewCellFormattingEventHandler(this.dgvData_CellFormatting);
            //
            // FrmCompositeLayup
            //
            this.AutoScaleDimensions = new SizeF(7F, 15F);
            this.ClientSize = new Size(334, 561);
            this.Name = "FrmCompositeLayup";
            this.Text = "Edit Composite Layup";
            this.gbType.ResumeLayout(false);
            this.gbProperties.ResumeLayout(false);
            this.tpDataPoints.ResumeLayout(false);
            this.tpProperties.ResumeLayout(false);
            this.ResumeLayout(false);
        }


        // Event handlers
        private void tcProperties_SelectedIndexChanged(object sender, EventArgs e)
        {
            HighlightCompositeLayup();
        }
        private void dgvData_SelectionChanged(object sender, EventArgs e)
        {
            HighlightCompositeLayup();
            pnlLayup.Invalidate();
        }
        private void dgvData_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && dgvData.Columns[e.ColumnIndex].Name == _plyColumnName)
            {
                e.Value = dgvData.Rows[e.RowIndex].IsNewRow ? "" : (e.RowIndex + 1).ToString();
                e.FormattingApplied = true;
            }
        }
        private void Binding_ListChanged(object sender, ListChangedEventArgs e)
        {
            _propertyItemChanged = true;
            propertyGrid.Refresh();
            dgvData.Invalidate();
            pnlLayup.Invalidate();
        }
        // A new ply starts as a copy of the last one
        private void Binding_AddingNew(object sender, AddingNewEventArgs e)
        {
            List<PlyDataPoint> plies = _viewCompositeLayup?.Plies;
            string name = "Ply-" + ((plies?.Count ?? 0) + 1);
            if (plies != null && plies.Count > 0)
            {
                Ply last = plies[plies.Count - 1].GetPly().DeepClone();
                last.Name = name;
                e.NewObject = new PlyDataPoint(last);
            }
            else
            {
                e.NewObject = new PlyDataPoint(new Ply(name, _regionNames.Length > 0 ? _regionNames[0] : null,
                                                       _materialNames.Length > 0 ? _materialNames[0] : null, 1, 0));
            }
        }
        private void pnlLayup_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                int row = GetLayupRowAt(e.Location);
                if (row < 0 || row >= dgvData.Rows.Count) return;
                //
                int columnIndex = dgvData.CurrentCell != null ? dgvData.CurrentCell.ColumnIndex : 0;
                dgvData.CurrentCell = dgvData[columnIndex, row];
                dgvData.Focus();
            }
            catch { }
        }
        private void pnlLayup_Paint(object sender, PaintEventArgs e)
        {
            try
            {
                if (_viewCompositeLayup != null) DrawLayup(e.Graphics);
            }
            catch { }
        }
        private void btnImportOrientations_Click(object sender, EventArgs e)
        {
            try
            {
                if (_viewCompositeLayup == null) return;
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Import Discrete Orientations";
                    openFileDialog.Filter = "Discrete orientations|*.xml|All files|*.*";
                    if (openFileDialog.ShowDialog() != DialogResult.OK) return;
                    //
                    DiscreteOrientationsData data = _controller.ReadDiscreteOrientations(openFileDialog.FileName);
                    CompositeLayup layup = _viewCompositeLayup.GetBase();
                    layup.DiscreteOrientationsData = data;
                    _viewCompositeLayup.OrientationType = CompositeLayupOrientationEnum.DiscreteOrientations;
                    _propertyItemChanged = true;
                    propertyGrid.Refresh();
                    HighlightCompositeLayup();
                }
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
        }
        private void btnDraping_Click(object sender, EventArgs e)
        {
            try
            {
                if (_viewCompositeLayup == null) return;
                CompositeLayup layup = _viewCompositeLayup.GetBase();
                layup.CheckEquations();
                _controller.ClearSelectionHistory();
                Cursor = Cursors.WaitCursor;
                double maxShear = _controller.DrawDrapingSimulation(layup);
                lblDraping.Text = maxShear >= 0 ? "Max shear: " + maxShear.ToString("F1") + "°" : "No draping result";
            }
            catch (Exception ex)
            {
                ExceptionTools.Show(this, ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }


        // Overrides
        protected override void OnListViewTypeSelectedIndexChanged()
        {
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count > 0)
            {
                object itemTag = lvTypes.SelectedItems[0].Tag;
                if (itemTag is ViewError) _viewCompositeLayup = null;
                else if (itemTag is ViewCompositeLayup vcl) _viewCompositeLayup = vcl;
                else throw new NotImplementedException();
                //
                dgvData.DataSource = null;
                dgvData.Columns.Clear();
                if (_viewCompositeLayup != null) SetPliesBinding(_viewCompositeLayup);
                //
                propertyGrid.SelectedObject = itemTag;
                lblDraping.Text = "";
                //
                HighlightCompositeLayup();
            }
        }
        protected override void OnPropertyGridPropertyValueChanged()
        {
            HighlightCompositeLayup();
            pnlLayup.Invalidate();
            //
            base.OnPropertyGridPropertyValueChanged();
        }
        protected override void OnApply(bool onOkAddNew)
        {
            if (propertyGrid.SelectedObject is ViewError ve) throw new CaeException(ve.Message);
            //
            _viewCompositeLayup = (ViewCompositeLayup)propertyGrid.SelectedObject;
            if (_viewCompositeLayup == null) throw new CaeException("No composite layup was selected.");
            //
            CompositeLayup layup = CompositeLayup;
            // Check if the name exists
            CheckName(_compositeLayupToEditName, layup.Name, _compositeLayupNames, "composite layup");
            // Check equations
            layup.CheckEquations();
            //
            foreach (Ply ply in layup.Plies)
            {
                if (string.IsNullOrEmpty(ply.Name)) throw new CaeException("Every ply must have a name.");
                if (!_materialNames.Contains(ply.MaterialName))
                    throw new CaeException("The ply " + ply.Name + " must name an existing material.");
                if (CompositeLayup.GetRegionElementIds(_controller.Model.Mesh, ply.RegionName) == null)
                    throw new CaeException("The ply " + ply.Name + " must name an existing element set or part.");
                if (ply.Thickness.Value <= 0) throw new CaeException("The thickness of the ply " + ply.Name +
                                                                     " must be larger than 0.");
            }
            if (layup.OrientationType == CompositeLayupOrientationEnum.DiscreteOrientations &&
                layup.DiscreteOrientationsData == null)
                throw new CaeException("Import the discrete orientations of the layup first.");
            //
            foreach (string partName in GetPartNamesToSetCompositeShellElementTypes(layup))
                SetCompositeShellElementTypes(partName);
            // Create
            if (_compositeLayupToEditName == null)
            {
                _controller.AddCompositeLayupCommand(layup);
            }
            // Replace
            else if (_propertyItemChanged)
            {
                _controller.ReplaceCompositeLayupCommand(_compositeLayupToEditName, layup);
            }
        }
        protected override bool OnPrepareForm(string stepName, string compositeLayupToEditName)
        {
            this.btnOkAddNew.Visible = compositeLayupToEditName == null;
            //
            _propertyItemChanged = false;
            _compositeLayupNames = null;
            _compositeLayupToEditName = null;
            _viewCompositeLayup = null;
            lvTypes.Items.Clear();
            propertyGrid.SelectedObject = null;
            lblDraping.Text = "";
            //
            _compositeLayupNames = _controller.GetCompositeLayupNames();
            _compositeLayupToEditName = compositeLayupToEditName;
            _materialNames = _controller.GetMaterialNames() ?? new string[0];
            List<string> regionNames = new List<string>();
            regionNames.AddRange(_controller.GetUserElementSetNames() ?? new string[0]);
            regionNames.AddRange(_controller.GetModelPartNames() ?? new string[0]);
            _regionNames = regionNames.ToArray();
            _coordinateSystemNames = _controller.GetModelCoordinateSystemNames() ?? new string[0];
            //
            ListViewItem item = new ListViewItem("Composite Layup");
            if (_materialNames.Length == 0)
                item.Tag = new ViewError("There is no material defined for the composite layup definition.");
            else if (_regionNames.Length == 0)
                item.Tag = new ViewError("There is no element set or part for the plies of the composite layup.");
            else
            {
                if (_compositeLayupToEditName == null)
                {
                    CompositeLayup layup = new CompositeLayup(GetCompositeLayupName());
                    layup.Plies.Add(new Ply("Ply-1", _regionNames[0], _materialNames[0], 1, 0));
                    CompositeLayup = layup;
                }
                else CompositeLayup = _controller.GetCompositeLayup(_compositeLayupToEditName);    // to clone
                _viewCompositeLayup.PopulateDropDownLists(_coordinateSystemNames);
                item.Tag = _viewCompositeLayup;
            }
            lvTypes.Items.Add(item);
            lvTypes.Enabled = false;
            _preselectIndex = 0;
            //
            propertyGrid.BuildAutocompleteMenu(_controller.GetAllParameterNames());
            //
            return true;
        }


        // Methods
        private string GetCompositeLayupName()
        {
            return _compositeLayupNames.GetNextNumberedKey("Layup");
        }
        private void SetPliesBinding(ViewCompositeLayup viewCompositeLayup)
        {
            BindingSource binding = new BindingSource();
            binding.DataSource = viewCompositeLayup.Plies;
            binding.AddingNew += Binding_AddingNew;
            dgvData.DataSource = binding; // bind datagridview to binding source - enables adding of new lines
            binding.ListChanged += Binding_ListChanged;
            // The regions, materials and coordinate systems are picked from the model; missing ones are kept to be seen
            ReplaceWithComboColumn(nameof(PlyDataPoint.RegionName), _regionNames,
                                   viewCompositeLayup.Plies.Select(p => p.RegionName));
            ReplaceWithComboColumn(nameof(PlyDataPoint.MaterialName), _materialNames,
                                   viewCompositeLayup.Plies.Select(p => p.MaterialName));
            ReplaceWithComboColumn(nameof(PlyDataPoint.CoordinateSystemName),
                                   new[] { Ply.LayupCoordinateSystemName }.Concat(_coordinateSystemNames).ToArray(),
                                   viewCompositeLayup.Plies.Select(p => p.CoordinateSystemName));
            // The plies are numbered
            DataGridViewTextBoxColumn plyColumn = new DataGridViewTextBoxColumn();
            plyColumn.Name = _plyColumnName;
            plyColumn.HeaderText = "Ply";
            plyColumn.ReadOnly = true;
            plyColumn.Width = 34;
            plyColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
            plyColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvData.Columns.Insert(0, plyColumn);
            //
            SetGridViewUnit(nameof(PlyDataPoint.ThicknessEq), _controller.Model.UnitSystem.LengthUnitAbbreviation,
                            new StringLengthFromConverter());
            SetGridViewUnit(nameof(PlyDataPoint.AngleEq), StringAngleDegConverter.GetUnitAbbreviation(),
                            new StringAngleDegFromConverter());
            dgvData.BuildAutocompleteMenu(_controller.GetAllParameterNames());
        }
        private void ReplaceWithComboColumn(string columnName, string[] names, IEnumerable<string> usedNames)
        {
            DataGridViewColumn column = dgvData.Columns[columnName];
            if (column == null) return;
            //
            DataGridViewComboBoxColumn comboColumn = new DataGridViewComboBoxColumn();
            comboColumn.DataPropertyName = column.DataPropertyName;
            comboColumn.Name = column.Name;
            comboColumn.HeaderText = column.HeaderText;
            comboColumn.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            comboColumn.FlatStyle = FlatStyle.Flat;
            List<string> items = new List<string>(names);
            foreach (string name in usedNames) if (name != null && !items.Contains(name)) items.Add(name);
            comboColumn.Items.AddRange(items.ToArray());
            int index = column.Index;
            dgvData.Columns.RemoveAt(index);
            dgvData.Columns.Insert(index, comboColumn);
        }
        private void SetGridViewUnit(string columnName, string unit, TypeConverter converter)
        {
            DataGridViewColumn column = dgvData.Columns[columnName];
            if (column == null) return;
            //
            if (column.HeaderText != null) column.HeaderText = column.HeaderText.Replace("?", unit);
            column.HeaderCell.Style.Alignment = DataGridViewContentAlignment.BottomCenter;
            column.Tag = converter;
        }
        private void HighlightCompositeLayup()
        {
            try
            {
                _controller.ClearSelectionHistory();
                if (_viewCompositeLayup == null) return;
                int plyIndex = dgvData.CurrentRow != null ? dgvData.CurrentRow.Index : 0;
                if (plyIndex >= _viewCompositeLayup.Plies.Count) plyIndex = _viewCompositeLayup.Plies.Count - 1;
                if (plyIndex < 0) return;
                _controller.HighlightCompositeLayup(_viewCompositeLayup.GetBase(), plyIndex);
            }
            catch { }
        }
        // Composite shell sections take S8R and S6 elements
        private string[] GetPartNamesToSetCompositeShellElementTypes(CompositeLayup layup)
        {
            string[] partNamesToChangeElementType;
            string[] partNamesToMesh;
            _controller.Model.GetCompositeShellElementTypeParts(layup.GetElementIds(_controller.Model.Mesh).ToArray(),
                                                                out partNamesToChangeElementType, out partNamesToMesh);
            string text = "A composite layup takes S8R and S6 elements only.";
            if (partNamesToMesh.Length > 0)
            {
                text += " Mesh " + GetPartNamesText(partNamesToMesh) + " with parabolic shell elements";
                if (partNamesToChangeElementType.Length > 0)
                    text += " and set the element type of " + GetPartNamesText(partNamesToChangeElementType) +
                            " to S8R and S6";
                text += "." + Environment.NewLine + Environment.NewLine +
                        "OK - save the composite layup, which stays invalid until then" + Environment.NewLine +
                        "Cancel - return to the dialog";
                if (MessageBoxes.ShowWarningQuestionOKCancel(text) == DialogResult.Cancel)
                    throw new CaeException("BreakOnApply");
            }
            else if (partNamesToChangeElementType.Length > 0)
            {
                text += " Set the element type of " + GetPartNamesText(partNamesToChangeElementType) + " to S8R and " +
                        "S6? The element type applies to all elements of a part." + Environment.NewLine +
                        Environment.NewLine +
                        "Yes - change the element type and save the composite layup" + Environment.NewLine +
                        "No - save the composite layup, which stays invalid until the element type is changed" +
                        Environment.NewLine +
                        "Cancel - return to the dialog";
                DialogResult result = MessageBoxes.ShowWarningQuestionYesNoCancel(text);
                if (result == DialogResult.Cancel) throw new CaeException("BreakOnApply");
                else if (result == DialogResult.Yes) return partNamesToChangeElementType;
            }
            return new string[0];
        }
        private string GetPartNamesText(string[] partNames)
        {
            return (partNames.Length == 1 ? "the part " : "the parts ") + string.Join(", ", partNames);
        }
        private void SetCompositeShellElementTypes(string partName)
        {
            PartProperties partProperties = _controller.GetModelPart(partName).GetProperties();
            if (partProperties.ParabolicQuadType != FeElementTypeParabolicQuad.None)
                partProperties.ParabolicQuadType = FeElementTypeParabolicQuad.S8R;
            if (partProperties.ParabolicTriaType != FeElementTypeParabolicTria.None)
                partProperties.ParabolicTriaType = FeElementTypeParabolicTria.S6;
            _controller.ReplaceModelPartPropertiesCommand(partName, partProperties);
        }
        // The layup diagram: the plies from ply 1 at the bottom to the top, each as high as it is thick, hatched as a cross
        // section, the mirrored plies of a symmetric layup as their mirror images, the ply of the selected row outlined
        private List<double> GetSolverThicknesses(out int numOfRows)
        {
            List<PlyDataPoint> plies = _viewCompositeLayup.Plies;
            numOfRows = plies.Count;
            List<double> thicknesses = new List<double>();
            foreach (PlyDataPoint ply in plies)
            {
                try { thicknesses.Add(Math.Max(0, ply.Thickness.Value)); }
                catch { thicknesses.Add(0); }
            }
            if (_viewCompositeLayup.Symmetric)
                for (int i = plies.Count - 1; i >= 0; i--) thicknesses.Add(thicknesses[i]);
            return thicknesses;
        }
        private void DrawLayup(Graphics graphics)
        {
            Rectangle stack;
            int numOfRows;
            Rectangle[] bars = GetLayupBars(out stack, out numOfRows);
            if (bars == null) return;
            //
            int selectedRow = dgvData.CurrentRow != null ? dgvData.CurrentRow.Index : -1;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Pen arrowPen = new Pen(Color.Black, 1.5f))
            {
                arrowPen.CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 5);
                int x = stack.Left - _layupArrowWidth + 4;
                graphics.DrawLine(arrowPen, x, stack.Bottom, x, stack.Top);
                TextRenderer.DrawText(graphics, "3", Font, new Point(x + 4, stack.Top - 2), Color.Black);
            }
            //
            double hatchPhase = 0;
            double[] risingPhases = Enumerable.Repeat(double.NaN, bars.Length).ToArray();
            double[] fallingPhases = Enumerable.Repeat(double.NaN, bars.Length).ToArray();
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height <= 0) continue;
                if (i < numOfRows)
                {
                    if (i % 3 != 1) risingPhases[i] = hatchPhase;
                    if (i % 3 != 0) fallingPhases[i] = hatchPhase;
                }
                else
                {
                    int mirroredPly = GetLayupRow(i, bars.Length, numOfRows);
                    risingPhases[i] = fallingPhases[mirroredPly] - bars[i].Height;
                    fallingPhases[i] = risingPhases[mirroredPly] + bars[i].Height;
                }
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(245, 245, 245)))
                    graphics.FillRectangle(brush, bars[i]);
                hatchPhase = DrawPlyHatch(graphics, bars[i], risingPhases[i], fallingPhases[i]);
                using (Pen pen = new Pen(Color.Black)) graphics.DrawRectangle(pen, bars[i]);
            }
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height <= 0 || selectedRow < 0 || GetLayupRow(i, bars.Length, numOfRows) != selectedRow)
                    continue;
                using (Pen pen = new Pen(SystemColors.Highlight, 3)) graphics.DrawRectangle(pen, bars[i]);
            }
        }
        private Rectangle[] GetLayupBars(out Rectangle stack, out int numOfRows)
        {
            Rectangle area = pnlLayup.ClientRectangle;
            area.Inflate(-16, -12);
            stack = new Rectangle(area.Left + _layupArrowWidth, area.Top, area.Width - _layupArrowWidth, area.Height);
            List<double> thicknesses = GetSolverThicknesses(out numOfRows);
            if (thicknesses.Count == 0 || stack.Width <= 0 || stack.Height <= 0) return null;
            double totalThickness = thicknesses.Sum();
            if (totalThickness <= 0) return null;
            //
            Rectangle[] bars = new Rectangle[thicknesses.Count];
            double bottom = stack.Bottom;
            for (int i = 0; i < thicknesses.Count; i++)
            {
                double height = stack.Height * thicknesses[i] / totalThickness;
                int top = (int)Math.Round(bottom - height);
                bars[i] = new Rectangle(stack.Left, top, stack.Width - 1, (int)Math.Round(bottom) - top);
                bottom -= height;
            }
            return bars;
        }
        private int GetLayupRow(int plyIndex, int numOfPlies, int numOfRows)
        {
            return plyIndex < numOfRows ? plyIndex : numOfPlies - 1 - plyIndex;
        }
        private int GetLayupRowAt(Point location)
        {
            if (_viewCompositeLayup == null) return -1;
            Rectangle[] bars = GetLayupBars(out _, out int numOfRows);
            if (bars == null) return -1;
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i].Height > 0 && bars[i].Left <= location.X && location.X <= bars[i].Right &&
                    bars[i].Top <= location.Y && location.Y < bars[i].Bottom)
                    return GetLayupRow(i, bars.Length, numOfRows);
            }
            return -1;
        }
        private double DrawPlyHatch(Graphics graphics, Rectangle bar, double risingPhase, double fallingPhase)
        {
            double spacing = _layupHatchSpacing;
            double height = bar.Height;
            bool rising = !double.IsNaN(risingPhase);
            bool falling = !double.IsNaN(fallingPhase);
            double risingTopPhase = rising ? ((risingPhase + height) % spacing + spacing) % spacing : 0;
            double fallingTopPhase = falling ? ((fallingPhase - height) % spacing + spacing) % spacing : 0;
            double nextPhase;
            if (rising && falling)
            {
                double gap = ((fallingTopPhase - risingTopPhase) % spacing + spacing) % spacing;
                if (gap >= spacing - gap) nextPhase = risingTopPhase + gap / 2;
                else nextPhase = fallingTopPhase + (spacing - gap) / 2;
            }
            else if (rising) nextPhase = risingTopPhase + spacing / 2;
            else if (falling) nextPhase = fallingTopPhase + spacing / 2;
            else nextPhase = 0;
            if (bar.Width < 2 || bar.Height < 2) return nextPhase;
            //
            int numOfBackSteps = (int)Math.Ceiling((height + spacing) / spacing) + 1;
            double end = bar.Right + height + spacing;
            System.Drawing.Drawing2D.GraphicsState state = graphics.Save();
            graphics.SetClip(bar);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(Color.Black, 1))
            {
                if (rising)
                    for (double x = bar.Left + risingPhase % spacing - numOfBackSteps * spacing; x <= end; x += spacing)
                        graphics.DrawLine(pen, (float)x, bar.Bottom, (float)(x + height), bar.Top);
                if (falling)
                    for (double x = bar.Left + fallingPhase % spacing - numOfBackSteps * spacing; x <= end; x += spacing)
                        graphics.DrawLine(pen, (float)x, bar.Bottom, (float)(x - height), bar.Top);
            }
            graphics.Restore(state);
            return nextPhase;
        }
        // IFormHighlight
        public void Highlight()
        {
            if (!_closing) HighlightCompositeLayup();
        }
    }
}
