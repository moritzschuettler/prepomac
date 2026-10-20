// PrePoMax for macOS - SplitContainer, TabControl, TableLayoutPanel and FlowLayoutPanel.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using SkiaSharp;

namespace System.Windows.Forms
{
    public class SplitContainer : ContainerControl, ISupportInitialize
    {
        private readonly SplitterPanel _panel1;
        private readonly SplitterPanel _panel2;
        private Orientation _orientation = Orientation.Vertical;
        private int _splitterDistance = 50;
        private int _splitterWidth = 4;
        private FixedPanel _fixedPanel;
        private bool _panel1Collapsed, _panel2Collapsed;
        private int _panel1MinSize = 25, _panel2MinSize = 25;
        private bool _dragging;
        private int _dragOffset;
        private bool _initializing;
        private Size _lastSize;
        private int _fixedPanelSize = -1;
        private BorderStyle _borderStyle;

        public SplitContainer()
        {
            _panel1 = new SplitterPanel(this);
            _panel2 = new SplitterPanel(this);
            Controls.Add(_panel1);
            Controls.Add(_panel2);
            SetStyle(ControlStyles.Selectable, false);
            UpdateSplitter();
        }
        protected override Size DefaultSize { get { return new Size(150, 100); } }
        public SplitterPanel Panel1 { get { return _panel1; } }
        public SplitterPanel Panel2 { get { return _panel2; } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set { _borderStyle = value; SetClientSizeOffset(BorderSizes.Of(value)); UpdateSplitter(); Host.InvalidateVisual(); }
        }
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height) { BorderSizes.Paint(canvas, _borderStyle, width, height); }
        public Orientation Orientation
        {
            get { return _orientation; }
            set { _orientation = value; UpdateSplitter(); }
        }
        public int SplitterDistance
        {
            get { return _splitterDistance; }
            set
            {
                int max = Total - _splitterWidth - (_initializing ? 0 : _panel2MinSize);
                int v = _initializing ? value : Math.Max(_panel1MinSize, Math.Min(value, Math.Max(_panel1MinSize, max)));
                if (v == _splitterDistance) return;
                _splitterDistance = v;
                RecordFixedPanelSize();
                UpdateSplitter();
                OnSplitterMoved(new SplitterEventArgs(0, 0, v, v));
            }
        }
        public int SplitterWidth { get { return _splitterWidth; } set { _splitterWidth = Math.Max(1, value); UpdateSplitter(); } }
        public int SplitterIncrement { get; set; } = 1;
        public Rectangle SplitterRectangle
        {
            get
            {
                if (_orientation == Orientation.Vertical) return new Rectangle(_splitterDistance, 0, _splitterWidth, ClientSize.Height);
                return new Rectangle(0, _splitterDistance, ClientSize.Width, _splitterWidth);
            }
        }
        public FixedPanel FixedPanel { get { return _fixedPanel; } set { _fixedPanel = value; RecordFixedPanelSize(); } }
        // The fixed panel keeps this size through every resize, also through passes that make the container too small
        private void RecordFixedPanelSize()
        {
            if (_fixedPanel == FixedPanel.Panel1) _fixedPanelSize = _splitterDistance;
            else if (_fixedPanel == FixedPanel.Panel2 && Total > 0) _fixedPanelSize = Math.Max(0, Total - _splitterDistance - _splitterWidth);
            else _fixedPanelSize = -1;
        }
        public bool IsSplitterFixed { get; set; }
        public bool Panel1Collapsed { get { return _panel1Collapsed; } set { _panel1Collapsed = value; UpdateSplitter(); } }
        public bool Panel2Collapsed { get { return _panel2Collapsed; } set { _panel2Collapsed = value; UpdateSplitter(); } }
        public int Panel1MinSize { get { return _panel1MinSize; } set { _panel1MinSize = value; } }
        public int Panel2MinSize { get { return _panel2MinSize; } set { _panel2MinSize = value; } }
        public event SplitterEventHandler SplitterMoved;
        public event SplitterCancelEventHandler SplitterMoving;
        protected virtual void OnSplitterMoved(SplitterEventArgs e) { SplitterMoved?.Invoke(this, e); }
        protected virtual void OnSplitterMoving(SplitterCancelEventArgs e) { SplitterMoving?.Invoke(this, e); }
        public void BeginInit() { _initializing = true; }
        public void EndInit() { _initializing = false; UpdateSplitter(); }
        private int Total { get { return _orientation == Orientation.Vertical ? ClientSize.Width : ClientSize.Height; } }
        internal override void LayoutChildren()
        {
            Size size = ClientSize;
            // The fixed panel keeps its size when the container is resized
            if (!_lastSize.IsEmpty && size != _lastSize && !_initializing)
            {
                int delta = _orientation == Orientation.Vertical ? size.Width - _lastSize.Width : size.Height - _lastSize.Height;
                if (_fixedPanel == FixedPanel.Panel2 && _fixedPanelSize >= 0) _splitterDistance = Total - _splitterWidth - _fixedPanelSize;
                else if (_fixedPanel == FixedPanel.Panel1 && _fixedPanelSize >= 0) _splitterDistance = _fixedPanelSize;
                else if (_fixedPanel == FixedPanel.Panel2) _splitterDistance += delta;
                else if (_fixedPanel == FixedPanel.None && (_orientation == Orientation.Vertical ? _lastSize.Width : _lastSize.Height) > 0)
                {
                    double ratio = (double)_splitterDistance / (_orientation == Orientation.Vertical ? _lastSize.Width : _lastSize.Height);
                    _splitterDistance = (int)Math.Round(ratio * Total);
                }
                _splitterDistance = Math.Max(_panel1MinSize, Math.Min(_splitterDistance, Math.Max(_panel1MinSize, Total - _splitterWidth - _panel2MinSize)));
            }
            _lastSize = size;
            UpdateSplitter();
        }
        private void UpdateSplitter()
        {
            if (_panel1 == null || _panel2 == null) return;
            Size size = ClientSize;
            _panel1.Visible = !_panel1Collapsed;
            _panel2.Visible = !_panel2Collapsed;
            if (_panel1Collapsed)
            {
                SetPanelBounds(_panel2, new Rectangle(0, 0, size.Width, size.Height));
                return;
            }
            if (_panel2Collapsed)
            {
                SetPanelBounds(_panel1, new Rectangle(0, 0, size.Width, size.Height));
                return;
            }
            if (_orientation == Orientation.Vertical)
            {
                SetPanelBounds(_panel1, new Rectangle(0, 0, _splitterDistance, size.Height));
                SetPanelBounds(_panel2, new Rectangle(_splitterDistance + _splitterWidth, 0, Math.Max(0, size.Width - _splitterDistance - _splitterWidth), size.Height));
            }
            else
            {
                SetPanelBounds(_panel1, new Rectangle(0, 0, size.Width, _splitterDistance));
                SetPanelBounds(_panel2, new Rectangle(0, _splitterDistance + _splitterWidth, size.Width, Math.Max(0, size.Height - _splitterDistance - _splitterWidth)));
            }
        }
        private static void SetPanelBounds(SplitterPanel panel, Rectangle r)
        {
            panel.SetBoundsCoreFromLayout(r.X, r.Y, r.Width, r.Height);
        }
        // The splitter is the area between the panels: the host gets the mouse there
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && !IsSplitterFixed && SplitterRectangle.Contains(e.Location))
            {
                _dragging = true;
                _dragOffset = (_orientation == Orientation.Vertical ? e.X : e.Y) - _splitterDistance;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!IsSplitterFixed && (SplitterRectangle.Contains(e.Location) || _dragging))
                Cursor = _orientation == Orientation.Vertical ? Cursors.VSplit : Cursors.HSplit;
            else Cursor = null;
            if (_dragging)
            {
                int d = (_orientation == Orientation.Vertical ? e.X : e.Y) - _dragOffset;
                SplitterCancelEventArgs args = new SplitterCancelEventArgs(e.X, e.Y, d, d);
                OnSplitterMoving(args);
                if (!args.Cancel) SplitterDistance = d;
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
        }
        internal override bool CanSelectOnClick { get { return false; } }
        internal override bool PaintsItself { get { return true; } }
    }

    public class TabControl : Control
    {
        private readonly TabPageCollection _pages;
        private int _selectedIndex = -1;
        private Size _itemSize = new Size(0, 18);
        private Point _padding = new Point(6, 3);
        private readonly List<Rectangle> _tabRects = new List<Rectangle>();
        private int _hotIndex = -1;

        public TabControl()
        {
            _pages = new TabPageCollection(this);
            SetStyle(ControlStyles.Selectable, true);
        }
        protected override Size DefaultSize { get { return new Size(200, 100); } }
        public TabPageCollection TabPages { get { return _pages; } }
        public int TabCount { get { return _pages.Count; } }
        public TabAlignment Alignment { get; set; }
        public TabAppearance Appearance { get; set; }
        public TabSizeMode SizeMode { get; set; }
        public TabDrawMode DrawMode { get; set; }
        public bool Multiline { get; set; }
        public bool HotTrack { get; set; }
        public bool ShowToolTips { get; set; }
        public ImageList ImageList { get; set; }
        public int RowCount { get { return 1; } }
        public Size ItemSize { get { return _itemSize; } set { _itemSize = value; PerformLayout(); } }
        public new Point Padding { get { return _padding; } set { _padding = value; PerformLayout(); } }
        public bool RightToLeftLayout { get; set; }
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _pages.Count) return;
                if (value == _selectedIndex) return;
                int old = _selectedIndex;
                TabPage oldPage = old >= 0 && old < _pages.Count ? _pages[old] : null;
                TabPage newPage = value >= 0 ? _pages[value] : null;
                if (oldPage != null)
                {
                    TabControlCancelEventArgs deselecting = new TabControlCancelEventArgs(oldPage, old, false, TabControlAction.Deselecting);
                    OnDeselecting(deselecting);
                    if (deselecting.Cancel) return;
                }
                TabControlCancelEventArgs selecting = new TabControlCancelEventArgs(newPage, value, false, TabControlAction.Selecting);
                OnSelecting(selecting);
                if (selecting.Cancel) return;
                _selectedIndex = value;
                UpdatePages();
                if (oldPage != null) OnDeselected(new TabControlEventArgs(oldPage, old, TabControlAction.Deselected));
                OnSelected(new TabControlEventArgs(newPage, value, TabControlAction.Selected));
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }
        public TabPage SelectedTab
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _pages.Count ? _pages[_selectedIndex] : null; }
            set { SelectedIndex = _pages.IndexOf(value); }
        }
        public event EventHandler SelectedIndexChanged;
        public event TabControlCancelEventHandler Selecting;
        public event TabControlEventHandler Selected;
        public event TabControlCancelEventHandler Deselecting;
        public event TabControlEventHandler Deselected;
        public event DrawItemEventHandler DrawItem;
        protected virtual void OnSelectedIndexChanged(EventArgs e) { SelectedIndexChanged?.Invoke(this, e); }
        protected virtual void OnSelecting(TabControlCancelEventArgs e) { Selecting?.Invoke(this, e); }
        protected virtual void OnSelected(TabControlEventArgs e) { Selected?.Invoke(this, e); }
        protected virtual void OnDeselecting(TabControlCancelEventArgs e) { Deselecting?.Invoke(this, e); }
        protected virtual void OnDeselected(TabControlEventArgs e) { Deselected?.Invoke(this, e); }
        protected virtual void OnDrawItem(DrawItemEventArgs e) { DrawItem?.Invoke(this, e); }
        public void SelectTab(int index) { SelectedIndex = index; }
        public void SelectTab(string tabPageName) { SelectedIndex = _pages.IndexOfKey(tabPageName); }
        public void SelectTab(TabPage tabPage) { SelectedTab = tabPage; }
        public void DeselectTab(int index) { if (index == _selectedIndex && _pages.Count > 1) SelectedIndex = (index + 1) % _pages.Count; }
        public Rectangle GetTabRect(int index)
        {
            ComputeTabs();
            return index >= 0 && index < _tabRects.Count ? _tabRects[index] : Rectangle.Empty;
        }
        public Control GetControl(int index) { return _pages[index]; }
        private int HeaderHeight { get { return Math.Max(_itemSize.Height, Font.Height + 2 * _padding.Y) + 3; } }
        public override Rectangle DisplayRectangle
        {
            get
            {
                Size s = ClientSize;
                int h = HeaderHeight;
                return new Rectangle(4, h + 1, Math.Max(0, s.Width - 8), Math.Max(0, s.Height - h - 5));
            }
        }
        internal void PageAdded(TabPage page)
        {
            if (_selectedIndex < 0) _selectedIndex = 0;
            UpdatePages();
        }
        internal void PageRemoved(TabPage page, int index)
        {
            if (_selectedIndex >= _pages.Count) _selectedIndex = _pages.Count - 1;
            UpdatePages();
            if (index <= _selectedIndex) OnSelectedIndexChanged(EventArgs.Empty);
        }
        private void UpdatePages()
        {
            Rectangle display = DisplayRectangle;
            for (int i = 0; i < _pages.Count; i++)
            {
                TabPage page = _pages[i];
                page.SetBoundsCoreFromLayout(display.X, display.Y, display.Width, display.Height);
                page.SetVisibleFromTab(i == _selectedIndex);
            }
            Host.InvalidateVisual();
        }
        internal override void LayoutChildren() { UpdatePages(); }
        private void ComputeTabs()
        {
            _tabRects.Clear();
            int x = 2;
            int h = HeaderHeight - 3;
            for (int i = 0; i < _pages.Count; i++)
            {
                int w = _itemSize.Width > 0 && SizeMode == TabSizeMode.Fixed ? _itemSize.Width :
                        ContentLayout.MeasureText(_pages[i].Text, Font).Width + 2 * _padding.X + 2;
                if (ImageList != null && _pages[i].ImageIndex >= 0) w += ImageList.ImageSize.Width + 3;
                _tabRects.Add(new Rectangle(x, 2, w, h));
                x += w;
            }
        }
        internal override bool PaintsItself { get { return true; } }
        internal override Color PaintedBackColor { get { return Color.Empty; } }
        internal override bool CanSelectOnClick { get { return false; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ComputeTabs();
            Size s = ClientSize;
            int top = HeaderHeight - 1;
            using (Pen border = new Pen(Color.FromArgb(0xD9, 0xD9, 0xD9)))
            using (SolidBrush pageBack = new SolidBrush(Color.White))
            using (SolidBrush tabBack = new SolidBrush(Color.FromArgb(0xF0, 0xF0, 0xF0)))
            using (SolidBrush hotBack = new SolidBrush(Color.FromArgb(0xD8, 0xEA, 0xF9)))
            using (SolidBrush text = new SolidBrush(Enabled ? ForeColor : SystemColors.GrayText))
            {
                g.FillRectangle(pageBack, 0, top, s.Width, s.Height - top);
                g.DrawRectangle(border, 0, top, s.Width - 1, s.Height - top - 1);
                for (int i = 0; i < _tabRects.Count; i++)
                {
                    Rectangle r = _tabRects[i];
                    bool selected = i == _selectedIndex;
                    if (selected) r = new Rectangle(r.X - 2, r.Y - 2, r.Width + 4, r.Height + 3);
                    g.FillRectangle(selected ? pageBack : i == _hotIndex ? hotBack : tabBack, r);
                    g.DrawLine(border, r.Left, r.Bottom - (selected ? 0 : 1), r.Left, r.Top);
                    g.DrawLine(border, r.Left, r.Top, r.Right - 1, r.Top);
                    g.DrawLine(border, r.Right - 1, r.Top, r.Right - 1, r.Bottom - (selected ? 0 : 1));
                    if (DrawMode == TabDrawMode.OwnerDrawFixed && DrawItem != null)
                    {
                        OnDrawItem(new DrawItemEventArgs(g, Font, r, i, selected ? DrawItemState.Selected : DrawItemState.None));
                        continue;
                    }
                    int tx = r.X + _padding.X;
                    if (ImageList != null && _pages[i].ImageIndex >= 0 && _pages[i].ImageIndex < ImageList.Images.Count)
                    {
                        Image image = ImageList.Images[_pages[i].ImageIndex];
                        g.DrawImage(image, tx, r.Y + (r.Height - image.Height) / 2);
                        tx += image.Width + 3;
                    }
                    StringFormat format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                    g.DrawString(_pages[i].Text, Font, text, new RectangleF(tx - 2, r.Y, r.Right - tx + 2, r.Height), format);
                }
            }
            base.OnPaint(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            ComputeTabs();
            for (int i = 0; i < _tabRects.Count; i++)
            {
                if (_tabRects[i].Contains(e.Location))
                {
                    SelectedIndex = i;
                    break;
                }
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            ComputeTabs();
            int hot = _tabRects.FindIndex(r => r.Contains(e.Location));
            if (hot != _hotIndex)
            {
                _hotIndex = hot;
                Invalidate();
            }
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hotIndex >= 0)
            {
                _hotIndex = -1;
                Invalidate();
            }
        }
        protected override ControlCollection CreateControlsInstance() { return new TabControlControlCollection(this); }

        public class TabControlControlCollection : ControlCollection
        {
            private readonly TabControl _owner;
            public TabControlControlCollection(TabControl owner) : base(owner) { _owner = owner; }
            public override void Add(Control value)
            {
                if (!(value is TabPage page)) throw new ArgumentException("Only tab pages can be added to a tab control.");
                base.Add(page);
                _owner.PageAdded(page);
            }
            public override void Remove(Control value)
            {
                int index = IndexOf(value);
                base.Remove(value);
                if (value is TabPage page) _owner.PageRemoved(page, index);
            }
        }

        public class TabPageCollection : IList
        {
            private readonly TabControl _owner;
            public TabPageCollection(TabControl owner) { _owner = owner; }
            public int Count { get { return _owner.Controls.Count; } }
            public TabPage this[int index] { get { return (TabPage)_owner.Controls[index]; } set { throw new NotSupportedException(); } }
            public TabPage this[string key] { get { int i = IndexOfKey(key); return i < 0 ? null : this[i]; } }
            public void Add(TabPage value) { _owner.Controls.Add(value); }
            public void Add(string text) { Add(new TabPage(text)); }
            public void Add(string key, string text) { Add(new TabPage(text) { Name = key }); }
            public void AddRange(TabPage[] pages) { foreach (TabPage p in pages) Add(p); }
            public void Insert(int index, TabPage tabPage)
            {
                _owner.Controls.Add(tabPage);
                _owner.Controls.SetChildIndex(tabPage, index);
                _owner.UpdatePages();
            }
            public void Remove(TabPage value) { _owner.Controls.Remove(value); }
            public void RemoveAt(int index) { _owner.Controls.RemoveAt(index); }
            public void RemoveByKey(string key) { int i = IndexOfKey(key); if (i >= 0) RemoveAt(i); }
            public void Clear() { _owner.Controls.Clear(); }
            public bool Contains(TabPage page) { return _owner.Controls.Contains(page); }
            public bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
            public int IndexOf(TabPage page) { return _owner.Controls.IndexOf(page); }
            public int IndexOfKey(string key) { return _owner.Controls.IndexOfKey(key); }
            public IEnumerator GetEnumerator() { return _owner.Controls.GetEnumerator(); }
            int IList.Add(object value) { Add((TabPage)value); return Count - 1; }
            bool IList.Contains(object value) { return value is TabPage p && Contains(p); }
            int IList.IndexOf(object value) { return value is TabPage p ? IndexOf(p) : -1; }
            void IList.Insert(int index, object value) { Insert(index, (TabPage)value); }
            void IList.Remove(object value) { if (value is TabPage p) Remove(p); }
            bool IList.IsFixedSize { get { return false; } }
            bool IList.IsReadOnly { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { for (int i = 0; i < Count; i++) array.SetValue(this[i], index + i); }
        }
    }

    public class TabPage : Panel
    {
        public TabPage() { }
        public TabPage(string text) { Text = text; }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; }
        public string ToolTipText { get; set; }
        public bool UseVisualStyleBackColor { get; set; }
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                (Parent as TabControl)?.Invalidate();
            }
        }
        internal override Color PaintedBackColor { get { return BackColorSet && !UseVisualStyleBackColor ? BackColor : Color.White; } }
        public override Color BackColor { get { return base.BackColor; } set { base.BackColor = value; } }
        internal void SetVisibleFromTab(bool visible) { base.SetVisibleCore(visible); }
        protected override void SetVisibleCore(bool value)
        {
            // The tab control shows the selected page
            if (Parent is TabControl) return;
            base.SetVisibleCore(value);
        }
        public static TabPage GetTabPageOfComponent(object comp)
        {
            for (Control c = comp as Control; c != null; c = c.Parent) if (c is TabPage p) return p;
            return null;
        }
    }

    public class ColumnStyle : TableLayoutStyle
    {
        public float Width { get; set; }
        public ColumnStyle() { }
        public ColumnStyle(SizeType sizeType) { SizeType = sizeType; }
        public ColumnStyle(SizeType sizeType, float width) { SizeType = sizeType; Width = width; }
    }

    public class RowStyle : TableLayoutStyle
    {
        public float Height { get; set; }
        public RowStyle() { }
        public RowStyle(SizeType sizeType) { SizeType = sizeType; }
        public RowStyle(SizeType sizeType, float height) { SizeType = sizeType; Height = height; }
    }

    public abstract class TableLayoutStyle
    {
        public SizeType SizeType { get; set; } = SizeType.AutoSize;
    }

    public class TableLayoutStyleCollection<T> : List<T> where T : TableLayoutStyle
    {
        public new int Add(T style) { base.Add(style); return Count - 1; }
    }

    public class TableLayoutColumnStyleCollection : TableLayoutStyleCollection<ColumnStyle> { }

    public class TableLayoutRowStyleCollection : TableLayoutStyleCollection<RowStyle> { }

    public struct TableLayoutPanelCellPosition
    {
        public int Column { get; set; }
        public int Row { get; set; }
        public TableLayoutPanelCellPosition(int column, int row) { Column = column; Row = row; }
    }

    public class TableLayoutPanel : Panel
    {
        private readonly Dictionary<Control, (int Column, int Row, int ColumnSpan, int RowSpan)> _cells = new Dictionary<Control, (int, int, int, int)>();
        private int _columnCount, _rowCount;
        public TableLayoutPanel() { }
        public int ColumnCount { get { return _columnCount; } set { _columnCount = value; PerformLayout(); } }
        public int RowCount { get { return _rowCount; } set { _rowCount = value; PerformLayout(); } }
        public TableLayoutColumnStyleCollection ColumnStyles { get; } = new TableLayoutColumnStyleCollection();
        public TableLayoutRowStyleCollection RowStyles { get; } = new TableLayoutRowStyleCollection();
        public TableLayoutPanelCellBorderStyle CellBorderStyle { get; set; }
        public TableLayoutPanelGrowStyle GrowStyle { get; set; } = TableLayoutPanelGrowStyle.AddRows;
        public event TableLayoutCellPaintEventHandler CellPaint;
        protected override ControlCollection CreateControlsInstance() { return new TableLayoutControlCollection(this); }
        public new TableLayoutControlCollection Controls { get { return (TableLayoutControlCollection)base.Controls; } }
        public void SetColumn(Control control, int column) { Set(control, column, null, null, null); }
        public void SetRow(Control control, int row) { Set(control, null, row, null, null); }
        public void SetColumnSpan(Control control, int value) { Set(control, null, null, value, null); }
        public void SetRowSpan(Control control, int value) { Set(control, null, null, null, value); }
        public void SetCellPosition(Control control, TableLayoutPanelCellPosition position) { Set(control, position.Column, position.Row, null, null); }
        public int GetColumn(Control control) { return _cells.TryGetValue(control, out var c) ? c.Column : -1; }
        public int GetRow(Control control) { return _cells.TryGetValue(control, out var c) ? c.Row : -1; }
        public int GetColumnSpan(Control control) { return _cells.TryGetValue(control, out var c) ? c.ColumnSpan : 1; }
        public int GetRowSpan(Control control) { return _cells.TryGetValue(control, out var c) ? c.RowSpan : 1; }
        public TableLayoutPanelCellPosition GetCellPosition(Control control) { return new TableLayoutPanelCellPosition(GetColumn(control), GetRow(control)); }
        public TableLayoutPanelCellPosition GetPositionFromControl(Control control) { return GetCellPosition(control); }
        public Control GetControlFromPosition(int column, int row)
        {
            foreach (var entry in _cells) if (entry.Value.Column == column && entry.Value.Row == row) return entry.Key;
            return null;
        }
        public int[] GetColumnWidths() { Compute(out int[] widths, out _); return widths; }
        public int[] GetRowHeights() { Compute(out _, out int[] heights); return heights; }
        private void Set(Control control, int? column, int? row, int? columnSpan, int? rowSpan)
        {
            _cells.TryGetValue(control, out var cell);
            if (!_cells.ContainsKey(control)) cell = (-1, -1, 1, 1);
            _cells[control] = (column ?? cell.Item1, row ?? cell.Item2, columnSpan ?? cell.Item3, rowSpan ?? cell.Item4);
            PerformLayout();
        }
        internal void RemoveCell(Control control) { _cells.Remove(control); }
        private void Compute(out int[] widths, out int[] heights)
        {
            AssignCells();
            int columns = Math.Max(1, Math.Max(_columnCount, _cells.Count == 0 ? 1 : _cells.Values.Max(c => c.Column + c.ColumnSpan)));
            int rows = Math.Max(1, Math.Max(_rowCount, _cells.Count == 0 ? 1 : _cells.Values.Max(c => c.Row + c.RowSpan)));
            Rectangle display = DisplayRectangle;
            widths = Sizes(columns, display.Width, i => i < ColumnStyles.Count ? ColumnStyles[i] : null, true);
            heights = Sizes(rows, display.Height, i => i < RowStyles.Count ? RowStyles[i] : null, false);
        }
        private int[] Sizes(int count, int total, Func<int, TableLayoutStyle> style, bool columns)
        {
            int[] sizes = new int[count];
            int used = 0;
            float percentTotal = 0;
            for (int i = 0; i < count; i++)
            {
                TableLayoutStyle s = style(i);
                if (s != null && s.SizeType == SizeType.Absolute)
                {
                    sizes[i] = (int)(columns ? ((ColumnStyle)s).Width : ((RowStyle)s).Height);
                    used += sizes[i];
                }
                else if (s != null && s.SizeType == SizeType.Percent) percentTotal += columns ? ((ColumnStyle)s).Width : ((RowStyle)s).Height;
                else
                {
                    int max = 0;
                    foreach (var entry in _cells)
                    {
                        if ((columns ? entry.Value.Column : entry.Value.Row) != i || !entry.Key.VisibleSelf) continue;
                        Control c = entry.Key;
                        Size preferred = c.AutoSize ? c.PreferredSize : c.Size;
                        max = Math.Max(max, (columns ? preferred.Width + c.Margin.Horizontal : preferred.Height + c.Margin.Vertical));
                    }
                    sizes[i] = max;
                    used += max;
                }
            }
            int remaining = Math.Max(0, total - used);
            if (percentTotal > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    TableLayoutStyle s = style(i);
                    if (s != null && s.SizeType == SizeType.Percent)
                        sizes[i] = (int)(remaining * (columns ? ((ColumnStyle)s).Width : ((RowStyle)s).Height) / percentTotal);
                }
            }
            return sizes;
        }
        private void AssignCells()
        {
            int columns = Math.Max(1, _columnCount);
            int next = 0;
            foreach (Control c in base.Controls.Items)
            {
                if (!_cells.TryGetValue(c, out var cell)) cell = (-1, -1, 1, 1);
                if (cell.Item1 < 0 || cell.Item2 < 0)
                {
                    while (GetControlFromPosition(next % columns, next / columns) != null) next++;
                    cell = (next % columns, next / columns, cell.Item3, cell.Item4);
                    _cells[c] = cell;
                    next++;
                }
            }
        }
        internal override void LayoutChildren()
        {
            if (!HasChildren) return;
            Compute(out int[] widths, out int[] heights);
            Rectangle display = DisplayRectangle;
            foreach (var entry in _cells.ToArray())
            {
                Control c = entry.Key;
                var cell = entry.Value;
                if (cell.Column < 0 || cell.Row < 0) continue;
                int x = display.X, y = display.Y;
                for (int i = 0; i < cell.Column && i < widths.Length; i++) x += widths[i];
                for (int i = 0; i < cell.Row && i < heights.Length; i++) y += heights[i];
                int w = 0, h = 0;
                for (int i = cell.Column; i < cell.Column + cell.ColumnSpan && i < widths.Length; i++) w += widths[i];
                for (int i = cell.Row; i < cell.Row + cell.RowSpan && i < heights.Length; i++) h += heights[i];
                Rectangle area = new Rectangle(x + c.Margin.Left, y + c.Margin.Top, Math.Max(0, w - c.Margin.Horizontal), Math.Max(0, h - c.Margin.Vertical));
                if (c.Dock == DockStyle.Fill)
                {
                    c.SetBoundsCoreFromLayout(area.X, area.Y, area.Width, area.Height);
                    continue;
                }
                Size size = c.AutoSize ? c.PreferredSize : c.Size;
                AnchorStyles a = c.Anchor;
                int cw = (a & (AnchorStyles.Left | AnchorStyles.Right)) == (AnchorStyles.Left | AnchorStyles.Right) ? area.Width : Math.Min(size.Width, area.Width);
                int ch = (a & (AnchorStyles.Top | AnchorStyles.Bottom)) == (AnchorStyles.Top | AnchorStyles.Bottom) ? area.Height : Math.Min(size.Height, area.Height);
                int cx = (a & AnchorStyles.Left) != 0 ? area.X : (a & AnchorStyles.Right) != 0 ? area.Right - cw : area.X + (area.Width - cw) / 2;
                int cy = (a & AnchorStyles.Top) != 0 ? area.Y : (a & AnchorStyles.Bottom) != 0 ? area.Bottom - ch : area.Y + (area.Height - ch) / 2;
                c.SetBoundsCoreFromLayout(cx, cy, cw, ch);
            }
        }

        public class TableLayoutControlCollection : ControlCollection
        {
            private readonly TableLayoutPanel _owner;
            public TableLayoutControlCollection(TableLayoutPanel container) : base(container) { _owner = container; }
            public TableLayoutPanel Container { get { return _owner; } }
            public virtual void Add(Control control, int column, int row)
            {
                base.Add(control);
                _owner.Set(control, column, row, null, null);
            }
            public override void Remove(Control value)
            {
                _owner.RemoveCell(value);
                base.Remove(value);
            }
        }
    }

    public class TableLayoutCellPaintEventArgs : PaintEventArgs
    {
        public Rectangle CellBounds { get; }
        public int Column { get; }
        public int Row { get; }
        public TableLayoutCellPaintEventArgs(Graphics g, Rectangle clipRectangle, Rectangle cellBounds, int column, int row) : base(g, clipRectangle)
        {
            CellBounds = cellBounds;
            Column = column;
            Row = row;
        }
    }

    public delegate void TableLayoutCellPaintEventHandler(object sender, TableLayoutCellPaintEventArgs e);

    public class FlowLayoutPanel : Panel
    {
        public FlowDirection FlowDirection { get; set; } = FlowDirection.LeftToRight;
        public bool WrapContents { get; set; } = true;
        public void SetFlowBreak(Control control, bool value) { }
        public bool GetFlowBreak(Control control) { return false; }
        internal override void LayoutChildren()
        {
            if (!HasChildren) return;
            Rectangle display = DisplayRectangle;
            int x = display.X, y = display.Y, line = 0;
            bool horizontal = FlowDirection == FlowDirection.LeftToRight || FlowDirection == FlowDirection.RightToLeft;
            foreach (Control c in base.Controls.Items)
            {
                if (!c.VisibleSelf) continue;
                Size size = c.AutoSize ? c.PreferredSize : c.Size;
                if (horizontal)
                {
                    if (WrapContents && x > display.X && x + c.Margin.Horizontal + size.Width > display.Right)
                    {
                        x = display.X;
                        y += line;
                        line = 0;
                    }
                    c.SetBoundsCoreFromLayout(x + c.Margin.Left, y + c.Margin.Top, size.Width, size.Height);
                    x += size.Width + c.Margin.Horizontal;
                    line = Math.Max(line, size.Height + c.Margin.Vertical);
                }
                else
                {
                    if (WrapContents && y > display.Y && y + c.Margin.Vertical + size.Height > display.Bottom)
                    {
                        y = display.Y;
                        x += line;
                        line = 0;
                    }
                    c.SetBoundsCoreFromLayout(x + c.Margin.Left, y + c.Margin.Top, size.Width, size.Height);
                    y += size.Height + c.Margin.Vertical;
                    line = Math.Max(line, size.Width + c.Margin.Horizontal);
                }
            }
        }
    }
}
