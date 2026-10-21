// PrePoMax for macOS - DataGridView with columns, rows and cells, data binding (lists, binding sources, data tables), editing
// with a text box or a combo box, selection, sorting and the clipboard; drawn like the WinForms data grid view.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Drawing.Drawing2D;
using Avalonia.Threading;

namespace System.Windows.Forms
{
    // Enums
    public enum DataGridViewContentAlignment { NotSet = 0, TopLeft = 1, TopCenter = 2, TopRight = 4, MiddleLeft = 16, MiddleCenter = 32, MiddleRight = 64, BottomLeft = 256, BottomCenter = 512, BottomRight = 1024 }
    public enum DataGridViewTriState { NotSet = 0, True = 1, False = 2 }
    public enum DataGridViewAutoSizeColumnMode { NotSet = 0, None = 1, ColumnHeader = 2, AllCellsExceptHeader = 4, AllCells = 6, DisplayedCellsExceptHeader = 8, DisplayedCells = 10, Fill = 16 }
    public enum DataGridViewAutoSizeColumnsMode { None = 1, ColumnHeader = 2, AllCellsExceptHeader = 4, AllCells = 6, DisplayedCellsExceptHeader = 8, DisplayedCells = 10, Fill = 16 }
    public enum DataGridViewAutoSizeRowsMode { None = 0, AllHeaders = 5, AllCellsExceptHeaders = 6, AllCells = 7, DisplayedHeaders = 9, DisplayedCellsExceptHeaders = 10, DisplayedCells = 11 }
    public enum DataGridViewColumnHeadersHeightSizeMode { EnableResizing = 0, DisableResizing = 1, AutoSize = 2 }
    public enum DataGridViewRowHeadersWidthSizeMode { EnableResizing = 0, DisableResizing = 1, AutoSizeToAllHeaders = 2, AutoSizeToDisplayedHeaders = 3, AutoSizeToFirstHeader = 4 }
    public enum DataGridViewSelectionMode { CellSelect = 0, FullRowSelect = 1, FullColumnSelect = 2, RowHeaderSelect = 3, ColumnHeaderSelect = 4 }
    public enum DataGridViewEditMode { EditOnEnter = 0, EditOnKeystroke = 1, EditOnKeystrokeOrF2 = 2, EditOnF2 = 3, EditProgrammatically = 4 }
    public enum DataGridViewColumnSortMode { NotSortable = 0, Automatic = 1, Programmatic = 2 }
    public enum DataGridViewClipboardCopyMode { Disable = 0, EnableWithAutoHeaderText = 1, EnableWithoutHeaderText = 2, EnableAlwaysIncludeHeaderText = 3 }
    public enum DataGridViewCellBorderStyle { Custom = 0, Single = 1, Raised = 2, Sunken = 3, None = 4, SingleVertical = 5, RaisedVertical = 6, SunkenVertical = 7, SingleHorizontal = 8, RaisedHorizontal = 9, SunkenHorizontal = 10 }
    public enum DataGridViewHeaderBorderStyle { Custom = 0, Single = 1, Raised = 2, Sunken = 3, None = 4 }
    public enum DataGridViewComboBoxDisplayStyle { ComboBox = 0, DropDownButton = 1, Nothing = 2 }
    [Flags] public enum DataGridViewElementStates { None = 0, Displayed = 1, Frozen = 2, ReadOnly = 4, Resizable = 8, ResizableSet = 16, Selected = 32, Visible = 64 }
    [Flags] public enum DataGridViewDataErrorContexts { Formatting = 1, Display = 2, PreferredSize = 4, RowDeletion = 8, Parsing = 256, Commit = 512, InitialValueRestoration = 1024, LeaveControl = 2048, CurrentCellChange = 4096, Scroll = 8192, ClipboardContent = 16384 }
    [Flags] public enum DataGridViewPaintParts { None = 0, Background = 1, Border = 2, ContentBackground = 4, ContentForeground = 8, ErrorIcon = 16, Focus = 32, SelectionBackground = 64, All = 127 }
    public enum DataGridViewHitTestType { None = 0, Cell = 1, ColumnHeader = 2, RowHeader = 3, TopLeftHeader = 4, HorizontalScrollBar = 5, VerticalScrollBar = 6 }

    // Styles
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class DataGridViewCellStyle : ICloneable
    {
        public DataGridViewContentAlignment Alignment { get; set; }
        public Color BackColor { get; set; } = Color.Empty;
        public Color ForeColor { get; set; } = Color.Empty;
        public Color SelectionBackColor { get; set; } = Color.Empty;
        public Color SelectionForeColor { get; set; } = Color.Empty;
        public Font Font { get; set; }
        public string Format { get; set; } = string.Empty;
        public IFormatProvider FormatProvider { get; set; }
        public object NullValue { get; set; } = string.Empty;
        public object DataSourceNullValue { get; set; } = DBNull.Value;
        public Padding Padding { get; set; }
        public DataGridViewTriState WrapMode { get; set; }
        public object Tag { get; set; }
        public bool IsFormatProviderDefault { get { return FormatProvider == null; } }
        public bool IsNullValueDefault { get { return NullValue is string s && s.Length == 0; } }
        public DataGridViewCellStyle() { }
        public DataGridViewCellStyle(DataGridViewCellStyle dataGridViewCellStyle) { ApplyStyle(dataGridViewCellStyle); }
        public virtual void ApplyStyle(DataGridViewCellStyle s)
        {
            if (s == null) return;
            if (s.Alignment != DataGridViewContentAlignment.NotSet) Alignment = s.Alignment;
            if (!s.BackColor.IsEmpty) BackColor = s.BackColor;
            if (!s.ForeColor.IsEmpty) ForeColor = s.ForeColor;
            if (!s.SelectionBackColor.IsEmpty) SelectionBackColor = s.SelectionBackColor;
            if (!s.SelectionForeColor.IsEmpty) SelectionForeColor = s.SelectionForeColor;
            if (s.Font != null) Font = s.Font;
            if (!string.IsNullOrEmpty(s.Format)) Format = s.Format;
            if (s.FormatProvider != null) FormatProvider = s.FormatProvider;
            if (!s.IsNullValueDefault) NullValue = s.NullValue;
            if (s.Padding != Padding.Empty) Padding = s.Padding;
            if (s.WrapMode != DataGridViewTriState.NotSet) WrapMode = s.WrapMode;
        }
        public virtual DataGridViewCellStyle Clone() { return new DataGridViewCellStyle(this) { Tag = Tag }; }
        object ICloneable.Clone() { return Clone(); }
    }

    // Cells
    public abstract class DataGridViewElement
    {
        public DataGridView DataGridView { get; internal set; }
        public virtual DataGridViewElementStates State { get { return DataGridViewElementStates.Visible; } }
    }

    public class DataGridViewCell : DataGridViewElement, ICloneable, IDisposable
    {
        internal object StoredValue;
        internal DataGridViewRow OwningRowInternal;
        internal DataGridViewColumn OwningColumnInternal;
        private DataGridViewCellStyle _style;
        private bool _readOnly;
        public DataGridViewCell() { }
        public DataGridViewColumn OwningColumn { get { return OwningColumnInternal; } }
        public DataGridViewRow OwningRow { get { return OwningRowInternal; } }
        public int ColumnIndex { get { return OwningColumnInternal?.Index ?? -1; } }
        public int RowIndex { get { return OwningRowInternal?.Index ?? -1; } }
        public object Tag { get; set; }
        public string ToolTipText { get; set; } = string.Empty;
        public string ErrorText { get; set; } = string.Empty;
        public DataGridViewCellStyle Style { get { return _style ?? (_style = new DataGridViewCellStyle()); } set { _style = value; } }
        public bool HasStyle { get { return _style != null; } }
        public DataGridViewCellStyle InheritedStyle { get { return DataGridView?.InheritedStyle(this) ?? Style; } }
        public virtual bool ReadOnly
        {
            get { return _readOnly || (OwningRowInternal?.ReadOnly ?? false) || (OwningColumnInternal?.ReadOnly ?? false) || (DataGridView?.ReadOnly ?? false); }
            set { _readOnly = value; }
        }
        public virtual bool Selected
        {
            get { return DataGridView != null && DataGridView.IsCellSelected(this); }
            set { DataGridView?.SetCellSelected(this, value); }
        }
        public bool Visible { get { return (OwningColumnInternal?.Visible ?? true) && (OwningRowInternal?.Visible ?? true); } }
        public bool Displayed { get { return Visible; } }
        public bool Frozen { get { return false; } }
        public bool IsInEditMode { get { return DataGridView != null && DataGridView.IsCurrentCellInEditMode && DataGridView.CurrentCell == this; } }
        public virtual Type ValueType
        {
            get { return OwningColumnInternal?.ValueType ?? typeof(object); }
            set { }
        }
        public virtual Type FormattedValueType { get { return typeof(string); } }
        public virtual Type EditType { get { return typeof(DataGridViewTextBoxEditingControl); } }
        public object Value
        {
            get { return DataGridView != null ? DataGridView.GetCellValue(this) : StoredValue; }
            set
            {
                if (DataGridView != null) DataGridView.SetCellValue(this, value, true);
                else StoredValue = value;
            }
        }
        public object FormattedValue { get { return DataGridView != null ? DataGridView.FormatCellValue(this) : StoredValue?.ToString(); } }
        public object EditedFormattedValue
        {
            get
            {
                if (IsInEditMode && DataGridView.EditingControl != null) return DataGridView.EditingControl.Text;
                return FormattedValue;
            }
        }
        public Size PreferredSize { get { return ContentLayout.MeasureText(FormattedValue?.ToString(), InheritedStyle.Font ?? DataGridView?.Font ?? Control.DefaultFont); } }
        public Rectangle ContentBounds { get { return Rectangle.Empty; } }
        public virtual object ParseFormattedValue(object formattedValue, DataGridViewCellStyle cellStyle, TypeConverter formattedValueTypeConverter, TypeConverter valueTypeConverter)
        {
            return DataGridView.ParseValue(this, formattedValue as string);
        }
        public virtual object Clone()
        {
            DataGridViewCell cell = (DataGridViewCell)Activator.CreateInstance(GetType());
            cell._style = _style?.Clone();
            cell._readOnly = _readOnly;
            cell.ToolTipText = ToolTipText;
            return cell;
        }
        public void Dispose() { }
        public override string ToString() { return GetType().Name + " { ColumnIndex=" + ColumnIndex + ", RowIndex=" + RowIndex + " }"; }
    }

    public class DataGridViewTextBoxCell : DataGridViewCell
    {
        public int MaxInputLength { get; set; } = 32767;
    }

    public class DataGridViewCheckBoxCell : DataGridViewCell
    {
        public override Type EditType { get { return null; } }
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }
        public bool ThreeState { get; set; }
    }

    public class DataGridViewComboBoxCell : DataGridViewCell
    {
        private readonly ObjectCollection _items;
        public DataGridViewComboBoxCell() { _items = new ObjectCollection(); }
        public ObjectCollection Items { get { return OwningColumnInternal is DataGridViewComboBoxColumn c && _items.Count == 0 ? c.Items : _items; } }
        public object DataSource { get; set; }
        public string DisplayMember { get; set; } = string.Empty;
        public string ValueMember { get; set; } = string.Empty;
        public DataGridViewComboBoxDisplayStyle DisplayStyle { get; set; }
        public FlatStyle FlatStyle { get; set; }
        public override Type EditType { get { return typeof(DataGridViewComboBoxEditingControl); } }
        public class ObjectCollection : List<object>
        {
            public void AddRange(params object[] items) { base.AddRange(items); }
        }
    }

    public class DataGridViewButtonCell : DataGridViewCell
    {
        public override Type EditType { get { return null; } }
        public FlatStyle FlatStyle { get; set; }
        public bool UseColumnTextForButtonValue { get; set; }
    }

    public class DataGridViewHeaderCell : DataGridViewCell
    {
        internal object HeaderValue;
        public new object Value { get { return HeaderValue; } set { HeaderValue = value; DataGridView?.Invalidate(); } }
        public SortOrder SortGlyphDirection { get; set; }
    }

    public class DataGridViewColumnHeaderCell : DataGridViewHeaderCell { }

    public class DataGridViewRowHeaderCell : DataGridViewHeaderCell { }

    // Columns
    [DesignTimeVisible(false)]
    public class DataGridViewColumn : DataGridViewElement, ICloneable, IComponent, IDisposable
    {
        private int _width = 100;
        private string _headerText;
        private bool _visible = true;
        private DataGridViewColumnHeaderCell _headerCell = new DataGridViewColumnHeaderCell();
        private DataGridViewCellStyle _defaultCellStyle;
        internal DataGridViewColumn() : this(new DataGridViewTextBoxCell()) { }
        public DataGridViewColumn(DataGridViewCell cellTemplate) { CellTemplate = cellTemplate; }
        public DataGridViewCell CellTemplate { get; set; }
        public string Name { get; set; } = string.Empty;
        public object Tag { get; set; }
        public string DataPropertyName { get; set; } = string.Empty;
        public string HeaderText
        {
            get { return _headerText ?? (_headerCell.HeaderValue as string) ?? Name; }
            set { _headerText = value; _headerCell.HeaderValue = value; DataGridView?.ColumnsChanged(); }
        }
        public DataGridViewColumnHeaderCell HeaderCell { get { return _headerCell; } set { _headerCell = value ?? new DataGridViewColumnHeaderCell(); } }
        public string ToolTipText { get; set; } = string.Empty;
        public int Width
        {
            get { return _width; }
            set
            {
                _width = Math.Max(MinimumWidth, value);
                DataGridView?.ColumnsChanged();
                DataGridView?.RaiseColumnWidthChanged(this);
            }
        }
        internal int WidthInternal { get { return _width; } set { _width = value; } }
        public int MinimumWidth { get; set; } = 5;
        public float FillWeight { get; set; } = 100;
        public int DividerWidth { get; set; }
        public DataGridViewAutoSizeColumnMode AutoSizeMode { get; set; } = DataGridViewAutoSizeColumnMode.NotSet;
        public DataGridViewAutoSizeColumnMode InheritedAutoSizeMode
        {
            get
            {
                if (AutoSizeMode != DataGridViewAutoSizeColumnMode.NotSet) return AutoSizeMode;
                return DataGridView == null ? DataGridViewAutoSizeColumnMode.None : (DataGridViewAutoSizeColumnMode)(int)DataGridView.AutoSizeColumnsMode;
            }
        }
        public bool Visible { get { return _visible; } set { _visible = value; DataGridView?.ColumnsChanged(); } }
        public virtual bool ReadOnly { get; set; }
        public bool Frozen { get; set; }
        public bool Resizable { get; set; } = true;
        public DataGridViewTriState Resizable_ { get; set; }
        public int DisplayIndex { get; set; }
        public int Index { get { return DataGridView?.Columns.IndexOf(this) ?? -1; } }
        public bool IsDataBound { get { return !string.IsNullOrEmpty(DataPropertyName) && DataGridView?.DataSource != null; } }
        public DataGridViewColumnSortMode SortMode { get; set; } = DataGridViewColumnSortMode.Automatic;
        public Type ValueType { get; set; }
        public DataGridViewCellStyle DefaultCellStyle { get { return _defaultCellStyle ?? (_defaultCellStyle = new DataGridViewCellStyle()); } set { _defaultCellStyle = value; } }
        public bool HasDefaultCellStyle { get { return _defaultCellStyle != null; } }
        public bool Selected { get; set; }
        public bool Displayed { get { return _visible; } }
        public ContextMenuStrip ContextMenuStrip { get; set; }
        public ISite Site { get; set; }
        public event EventHandler Disposed;
        public virtual object Clone()
        {
            DataGridViewColumn c = (DataGridViewColumn)Activator.CreateInstance(GetType());
            c.Name = Name;
            c._headerText = _headerText;
            c._width = _width;
            c.DataPropertyName = DataPropertyName;
            c._defaultCellStyle = _defaultCellStyle?.Clone();
            c.AutoSizeMode = AutoSizeMode;
            c.FillWeight = FillWeight;
            c.ReadOnly = ReadOnly;
            c.SortMode = SortMode;
            c.ValueType = ValueType;
            c.Visible = _visible;
            return c;
        }
        public void Dispose() { Disposed?.Invoke(this, EventArgs.Empty); }
        public virtual int GetPreferredWidth(DataGridViewAutoSizeColumnMode autoSizeColumnMode, bool fixedHeight)
        {
            return DataGridView?.PreferredColumnWidth(this, autoSizeColumnMode) ?? _width;
        }
        public override string ToString() { return "DataGridViewColumn { Name=" + Name + ", Index=" + Index + " }"; }
    }

    public class DataGridViewTextBoxColumn : DataGridViewColumn
    {
        public DataGridViewTextBoxColumn() : base(new DataGridViewTextBoxCell()) { }
        public int MaxInputLength { get; set; } = 32767;
    }

    public class DataGridViewCheckBoxColumn : DataGridViewColumn
    {
        public DataGridViewCheckBoxColumn() : base(new DataGridViewCheckBoxCell()) { ValueType = typeof(bool); }
        public DataGridViewCheckBoxColumn(bool threeState) : this() { ThreeState = threeState; }
        public bool ThreeState { get; set; }
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }
        public FlatStyle FlatStyle { get; set; }
    }

    public class DataGridViewComboBoxColumn : DataGridViewColumn
    {
        public DataGridViewComboBoxColumn() : base(new DataGridViewComboBoxCell()) { }
        public DataGridViewComboBoxCell.ObjectCollection Items { get; } = new DataGridViewComboBoxCell.ObjectCollection();
        public object DataSource { get; set; }
        public string DisplayMember { get; set; } = string.Empty;
        public string ValueMember { get; set; } = string.Empty;
        public DataGridViewComboBoxDisplayStyle DisplayStyle { get; set; }
        public bool DisplayStyleForCurrentCellOnly { get; set; }
        public FlatStyle FlatStyle { get; set; }
        public int DropDownWidth { get; set; } = 1;
        public int MaxDropDownItems { get; set; } = 8;
        public bool Sorted { get; set; }
        public bool AutoComplete { get; set; } = true;
    }

    public class DataGridViewButtonColumn : DataGridViewColumn
    {
        public DataGridViewButtonColumn() : base(new DataGridViewButtonCell()) { }
        public string Text { get; set; }
        public bool UseColumnTextForButtonValue { get; set; }
        public FlatStyle FlatStyle { get; set; }
    }

    // Rows
    public class DataGridViewRow : DataGridViewElement, ICloneable
    {
        private readonly DataGridViewCellCollection _cells;
        private DataGridViewRowHeaderCell _headerCell = new DataGridViewRowHeaderCell();
        private DataGridViewCellStyle _defaultCellStyle;
        private int _height = -1;
        internal object BoundItem;
        internal bool IsNewRowInternal;
        public DataGridViewRow() { _cells = new DataGridViewCellCollection(this); }
        public DataGridViewCellCollection Cells { get { return _cells; } }
        public int Index { get { return DataGridView?.RowsInternal.IndexOf(this) ?? -1; } }
        public bool IsNewRow { get { return IsNewRowInternal; } }
        public object DataBoundItem { get { return IsNewRowInternal ? null : BoundItem; } }
        public DataGridViewRowHeaderCell HeaderCell { get { return _headerCell; } set { _headerCell = value ?? new DataGridViewRowHeaderCell(); } }
        public DataGridViewCellStyle DefaultCellStyle { get { return _defaultCellStyle ?? (_defaultCellStyle = new DataGridViewCellStyle()); } set { _defaultCellStyle = value; } }
        public bool HasDefaultCellStyle { get { return _defaultCellStyle != null; } }
        public int Height
        {
            get { return _height > 0 ? _height : DataGridView?.RowTemplate.Height ?? 22; }
            set { _height = value; DataGridView?.RowsChanged(); }
        }
        public int MinimumHeight { get; set; } = 3;
        public bool ReadOnly { get; set; }
        public bool Visible { get; set; } = true;
        public bool Frozen { get; set; }
        public bool Resizable { get; set; } = true;
        public object Tag { get; set; }
        public string ErrorText { get; set; } = string.Empty;
        public ContextMenuStrip ContextMenuStrip { get; set; }
        public bool Displayed { get { return Visible; } }
        public bool Selected
        {
            get { return DataGridView != null && DataGridView.IsRowSelected(this); }
            set { DataGridView?.SetRowSelected(this, value); }
        }
        public void CreateCells(DataGridView dataGridView)
        {
            _cells.Clear();
            foreach (DataGridViewColumn c in dataGridView.Columns) _cells.Add((DataGridViewCell)(c.CellTemplate?.Clone() ?? new DataGridViewTextBoxCell()));
        }
        public void CreateCells(DataGridView dataGridView, params object[] values)
        {
            CreateCells(dataGridView);
            SetValues(values);
        }
        public bool SetValues(params object[] values)
        {
            for (int i = 0; i < values.Length && i < _cells.Count; i++) _cells[i].StoredValue = values[i];
            DataGridView?.Invalidate();
            return values.Length <= _cells.Count;
        }
        public virtual object Clone()
        {
            DataGridViewRow r = (DataGridViewRow)Activator.CreateInstance(GetType());
            r._height = _height;
            r._defaultCellStyle = _defaultCellStyle?.Clone();
            r.ReadOnly = ReadOnly;
            foreach (DataGridViewCell c in _cells) r.Cells.Add((DataGridViewCell)c.Clone());
            return r;
        }
        public override string ToString() { return "DataGridViewRow { Index=" + Index + " }"; }
    }

    public class DataGridViewCellCollection : IList
    {
        private readonly DataGridViewRow _owner;
        private readonly List<DataGridViewCell> _list = new List<DataGridViewCell>();
        public DataGridViewCellCollection(DataGridViewRow dataGridViewRow) { _owner = dataGridViewRow; }
        public int Count { get { return _list.Count; } }
        public DataGridViewCell this[int index]
        {
            get
            {
                _owner.DataGridView?.EnsureCells(_owner);
                return _list[index];
            }
            set { Attach(value); _list[index] = value; }
        }
        public DataGridViewCell this[string columnName]
        {
            get
            {
                DataGridView grid = _owner.DataGridView;
                if (grid == null) return null;
                grid.EnsureCells(_owner);
                int index = grid.Columns.IndexOfName(columnName);
                return index >= 0 && index < _list.Count ? _list[index] : null;
            }
        }
        internal List<DataGridViewCell> List { get { return _list; } }
        private void Attach(DataGridViewCell cell)
        {
            cell.OwningRowInternal = _owner;
            cell.DataGridView = _owner.DataGridView;
        }
        public int Add(DataGridViewCell cell) { Attach(cell); _list.Add(cell); return _list.Count - 1; }
        public void AddRange(params DataGridViewCell[] cells) { foreach (DataGridViewCell c in cells) Add(c); }
        public void Clear() { _list.Clear(); }
        public bool Contains(DataGridViewCell cell) { return _list.Contains(cell); }
        public int IndexOf(DataGridViewCell cell) { return _list.IndexOf(cell); }
        public void Insert(int index, DataGridViewCell cell) { Attach(cell); _list.Insert(index, cell); }
        public void Remove(DataGridViewCell cell) { _list.Remove(cell); }
        public void RemoveAt(int index) { _list.RemoveAt(index); }
        public IEnumerator GetEnumerator()
        {
            _owner.DataGridView?.EnsureCells(_owner);
            return _list.ToList().GetEnumerator();
        }
        int IList.Add(object value) { return Add((DataGridViewCell)value); }
        bool IList.Contains(object value) { return value is DataGridViewCell c && Contains(c); }
        int IList.IndexOf(object value) { return value is DataGridViewCell c ? IndexOf(c) : -1; }
        void IList.Insert(int index, object value) { Insert(index, (DataGridViewCell)value); }
        void IList.Remove(object value) { if (value is DataGridViewCell c) Remove(c); }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { this[index] = (DataGridViewCell)value; } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
    }

    public class DataGridViewColumnCollection : IList
    {
        private readonly DataGridView _owner;
        private readonly List<DataGridViewColumn> _list = new List<DataGridViewColumn>();
        public DataGridViewColumnCollection(DataGridView dataGridView) { _owner = dataGridView; }
        public int Count { get { return _list.Count; } }
        public DataGridViewColumn this[int index] { get { return _list[index]; } }
        public DataGridViewColumn this[string columnName] { get { int i = IndexOfName(columnName); return i < 0 ? null : _list[i]; } }
        internal List<DataGridViewColumn> List { get { return _list; } }
        internal int IndexOfName(string name)
        {
            for (int i = 0; i < _list.Count; i++) if (string.Equals(_list[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        public virtual int Add(DataGridViewColumn column)
        {
            column.DataGridView = _owner;
            column.DisplayIndex = _list.Count;
            _list.Add(column);
            _owner.OnColumnAddedInternal(column);
            return _list.Count - 1;
        }
        public virtual int Add(string columnName, string headerText)
        {
            return Add(new DataGridViewTextBoxColumn { Name = columnName, HeaderText = headerText });
        }
        public virtual void AddRange(params DataGridViewColumn[] columns) { foreach (DataGridViewColumn c in columns) Add(c); }
        public virtual void Insert(int columnIndex, DataGridViewColumn column)
        {
            column.DataGridView = _owner;
            _list.Insert(columnIndex, column);
            _owner.OnColumnAddedInternal(column);
        }
        public virtual void Remove(DataGridViewColumn column)
        {
            int index = _list.IndexOf(column);
            if (index < 0) return;
            _list.RemoveAt(index);
            column.DataGridView = null;
            _owner.OnColumnRemovedInternal(index);
        }
        public virtual void Remove(string columnName) { DataGridViewColumn c = this[columnName]; if (c != null) Remove(c); }
        public virtual void RemoveAt(int index) { Remove(_list[index]); }
        public virtual void Clear()
        {
            foreach (DataGridViewColumn c in _list) c.DataGridView = null;
            _list.Clear();
            _owner.OnColumnRemovedInternal(-1);
        }
        public virtual bool Contains(DataGridViewColumn column) { return _list.Contains(column); }
        public virtual bool Contains(string columnName) { return IndexOfName(columnName) >= 0; }
        public int IndexOf(DataGridViewColumn column) { return _list.IndexOf(column); }
        public int GetColumnCount(DataGridViewElementStates includeFilter) { return _list.Count(c => c.Visible || (includeFilter & DataGridViewElementStates.Visible) == 0); }
        public DataGridViewColumn GetFirstColumn(DataGridViewElementStates includeFilter) { return _list.FirstOrDefault(c => c.Visible); }
        public DataGridViewColumn GetLastColumn(DataGridViewElementStates includeFilter, DataGridViewElementStates excludeFilter) { return _list.LastOrDefault(c => c.Visible); }
        public IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
        int IList.Add(object value) { return Add((DataGridViewColumn)value); }
        bool IList.Contains(object value) { return value is DataGridViewColumn c && Contains(c); }
        int IList.IndexOf(object value) { return value is DataGridViewColumn c ? IndexOf(c) : -1; }
        void IList.Insert(int index, object value) { Insert(index, (DataGridViewColumn)value); }
        void IList.Remove(object value) { if (value is DataGridViewColumn c) Remove(c); }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
    }

    public class DataGridViewRowCollection : IList
    {
        private readonly DataGridView _owner;
        public DataGridViewRowCollection(DataGridView dataGridView) { _owner = dataGridView; }
        private List<DataGridViewRow> List { get { return _owner.RowsInternal; } }
        public int Count { get { return List.Count; } }
        public DataGridViewRow this[int index] { get { return List[index]; } }
        public virtual int Add() { return Add(new object[0]); }
        public virtual int Add(params object[] values)
        {
            if (_owner.DataSource != null) throw new InvalidOperationException("Rows cannot be programmatically added to the DataGridView's rows collection when the control is data-bound.");
            DataGridViewRow row = (DataGridViewRow)_owner.RowTemplate.Clone();
            row.CreateCells(_owner, values);
            return _owner.InsertRow(_owner.NewRowIndex >= 0 ? _owner.NewRowIndex : List.Count, row);
        }
        public virtual int Add(int count)
        {
            int index = -1;
            for (int i = 0; i < count; i++) index = Add();
            return index;
        }
        public virtual int Add(DataGridViewRow dataGridViewRow)
        {
            if (dataGridViewRow.Cells.Count == 0) dataGridViewRow.CreateCells(_owner);
            return _owner.InsertRow(_owner.NewRowIndex >= 0 ? _owner.NewRowIndex : List.Count, dataGridViewRow);
        }
        public virtual void AddRange(params DataGridViewRow[] dataGridViewRows) { foreach (DataGridViewRow r in dataGridViewRows) Add(r); }
        public virtual void Insert(int rowIndex, params object[] values)
        {
            DataGridViewRow row = (DataGridViewRow)_owner.RowTemplate.Clone();
            row.CreateCells(_owner, values);
            _owner.InsertRow(rowIndex, row);
        }
        public virtual void Insert(int rowIndex, DataGridViewRow dataGridViewRow)
        {
            if (dataGridViewRow.Cells.Count == 0) dataGridViewRow.CreateCells(_owner);
            _owner.InsertRow(rowIndex, dataGridViewRow);
        }
        public virtual void Remove(DataGridViewRow dataGridViewRow) { _owner.RemoveRow(dataGridViewRow); }
        public virtual void RemoveAt(int index) { _owner.RemoveRow(List[index]); }
        public virtual void Clear() { _owner.ClearRows(); }
        public virtual bool Contains(DataGridViewRow dataGridViewRow) { return List.Contains(dataGridViewRow); }
        public int IndexOf(DataGridViewRow dataGridViewRow) { return List.IndexOf(dataGridViewRow); }
        public int GetRowCount(DataGridViewElementStates includeFilter) { return List.Count(r => r.Visible || (includeFilter & DataGridViewElementStates.Visible) == 0); }
        public int GetFirstRow(DataGridViewElementStates includeFilter) { return List.Count > 0 ? 0 : -1; }
        public int GetLastRow(DataGridViewElementStates includeFilter) { return List.Count - 1; }
        public int GetRowsHeight(DataGridViewElementStates includeFilter) { return List.Sum(r => r.Height); }
        public DataGridViewRow SharedRow(int rowIndex) { return List[rowIndex]; }
        public IEnumerator GetEnumerator() { return List.ToList().GetEnumerator(); }
        int IList.Add(object value) { return Add((DataGridViewRow)value); }
        bool IList.Contains(object value) { return value is DataGridViewRow r && Contains(r); }
        int IList.IndexOf(object value) { return value is DataGridViewRow r ? IndexOf(r) : -1; }
        void IList.Insert(int index, object value) { Insert(index, (DataGridViewRow)value); }
        void IList.Remove(object value) { if (value is DataGridViewRow r) Remove(r); }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)List).CopyTo(array, index); }
    }

    public class DataGridViewSelectedCellCollection : IList
    {
        private readonly List<DataGridViewCell> _list;
        internal DataGridViewSelectedCellCollection(List<DataGridViewCell> list) { _list = list; }
        public int Count { get { return _list.Count; } }
        public DataGridViewCell this[int index] { get { return _list[index]; } }
        public bool Contains(DataGridViewCell dataGridViewCell) { return _list.Contains(dataGridViewCell); }
        public void CopyTo(DataGridViewCell[] array, int index) { _list.CopyTo(array, index); }
        public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
        int IList.Add(object value) { throw new NotSupportedException(); }
        void IList.Clear() { throw new NotSupportedException(); }
        bool IList.Contains(object value) { return value is DataGridViewCell c && Contains(c); }
        int IList.IndexOf(object value) { return value is DataGridViewCell c ? _list.IndexOf(c) : -1; }
        void IList.Insert(int index, object value) { throw new NotSupportedException(); }
        void IList.Remove(object value) { throw new NotSupportedException(); }
        void IList.RemoveAt(int index) { throw new NotSupportedException(); }
        bool IList.IsFixedSize { get { return true; } }
        bool IList.IsReadOnly { get { return true; } }
        object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
    }

    public class DataGridViewSelectedRowCollection : IList
    {
        private readonly List<DataGridViewRow> _list;
        internal DataGridViewSelectedRowCollection(List<DataGridViewRow> list) { _list = list; }
        public int Count { get { return _list.Count; } }
        public DataGridViewRow this[int index] { get { return _list[index]; } }
        public bool Contains(DataGridViewRow dataGridViewRow) { return _list.Contains(dataGridViewRow); }
        public void CopyTo(DataGridViewRow[] array, int index) { _list.CopyTo(array, index); }
        public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
        int IList.Add(object value) { throw new NotSupportedException(); }
        void IList.Clear() { throw new NotSupportedException(); }
        bool IList.Contains(object value) { return value is DataGridViewRow r && Contains(r); }
        int IList.IndexOf(object value) { return value is DataGridViewRow r ? _list.IndexOf(r) : -1; }
        void IList.Insert(int index, object value) { throw new NotSupportedException(); }
        void IList.Remove(object value) { throw new NotSupportedException(); }
        void IList.RemoveAt(int index) { throw new NotSupportedException(); }
        bool IList.IsFixedSize { get { return true; } }
        bool IList.IsReadOnly { get { return true; } }
        object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
    }

    // Editing controls
    public interface IDataGridViewEditingControl
    {
        DataGridView EditingControlDataGridView { get; set; }
        object EditingControlFormattedValue { get; set; }
        int EditingControlRowIndex { get; set; }
        bool EditingControlValueChanged { get; set; }
        Cursor EditingPanelCursor { get; }
        bool RepositionEditingControlOnValueChange { get; }
        void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle);
        bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey);
        object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context);
        void PrepareEditingControlForEdit(bool selectAll);
    }

    public class DataGridViewTextBoxEditingControl : TextBox, IDataGridViewEditingControl
    {
        public DataGridViewTextBoxEditingControl() { BorderStyle = BorderStyle.None; }
        public DataGridView EditingControlDataGridView { get; set; }
        public object EditingControlFormattedValue { get { return Text; } set { Text = value as string ?? value?.ToString() ?? string.Empty; } }
        public int EditingControlRowIndex { get; set; }
        public bool EditingControlValueChanged { get; set; }
        public Cursor EditingPanelCursor { get { return Cursors.IBeam; } }
        public bool RepositionEditingControlOnValueChange { get { return false; } }
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
        {
            if (style.Font != null) Font = style.Font;
            if (!style.BackColor.IsEmpty) BackColor = style.BackColor;
            if (!style.ForeColor.IsEmpty) ForeColor = style.ForeColor;
        }
        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey)
        {
            Keys key = keyData & Keys.KeyCode;
            return key == Keys.Left || key == Keys.Right || key == Keys.Home || key == Keys.End || key == Keys.Delete || key == Keys.Back;
        }
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) { return Text; }
        public void PrepareEditingControlForEdit(bool selectAll) { if (selectAll) SelectAll(); else Select(TextLength, 0); }
        protected override void OnTextChanged(EventArgs e)
        {
            EditingControlValueChanged = true;
            base.OnTextChanged(e);
        }
    }

    public class DataGridViewComboBoxEditingControl : ComboBox, IDataGridViewEditingControl
    {
        public DataGridViewComboBoxEditingControl() { DropDownStyle = ComboBoxStyle.DropDownList; }
        public DataGridView EditingControlDataGridView { get; set; }
        public object EditingControlFormattedValue { get { return Text; } set { Text = value?.ToString(); } }
        public int EditingControlRowIndex { get; set; }
        public bool EditingControlValueChanged { get; set; }
        public Cursor EditingPanelCursor { get { return Cursors.Default; } }
        public bool RepositionEditingControlOnValueChange { get { return false; } }
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style) { if (style.Font != null) Font = style.Font; }
        public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey) { return false; }
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) { return Text; }
        public void PrepareEditingControlForEdit(bool selectAll) { }
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            EditingControlValueChanged = true;
            base.OnSelectedIndexChanged(e);
        }
    }

    // Event arguments
    public class DataGridViewCellEventArgs : EventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public DataGridViewCellEventArgs(int columnIndex, int rowIndex) { ColumnIndex = columnIndex; RowIndex = rowIndex; }
    }
    public delegate void DataGridViewCellEventHandler(object sender, DataGridViewCellEventArgs e);

    public class DataGridViewCellCancelEventArgs : CancelEventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public DataGridViewCellCancelEventArgs(int columnIndex, int rowIndex) { ColumnIndex = columnIndex; RowIndex = rowIndex; }
    }
    public delegate void DataGridViewCellCancelEventHandler(object sender, DataGridViewCellCancelEventArgs e);

    public class DataGridViewCellMouseEventArgs : MouseEventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public DataGridViewCellMouseEventArgs(int columnIndex, int rowIndex, int localX, int localY, MouseEventArgs e)
            : base(e.Button, e.Clicks, localX, localY, e.Delta) { ColumnIndex = columnIndex; RowIndex = rowIndex; }
    }
    public delegate void DataGridViewCellMouseEventHandler(object sender, DataGridViewCellMouseEventArgs e);

    public class DataGridViewCellValidatingEventArgs : CancelEventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public object FormattedValue { get; }
        public DataGridViewCellValidatingEventArgs(int columnIndex, int rowIndex, object formattedValue) { ColumnIndex = columnIndex; RowIndex = rowIndex; FormattedValue = formattedValue; }
    }
    public delegate void DataGridViewCellValidatingEventHandler(object sender, DataGridViewCellValidatingEventArgs e);

    public class DataGridViewCellFormattingEventArgs : ConvertEventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public DataGridViewCellStyle CellStyle { get; set; }
        public bool FormattingApplied { get; set; }
        public DataGridViewCellFormattingEventArgs(int columnIndex, int rowIndex, object value, Type desiredType, DataGridViewCellStyle cellStyle)
            : base(value, desiredType) { ColumnIndex = columnIndex; RowIndex = rowIndex; CellStyle = cellStyle; }
    }
    public delegate void DataGridViewCellFormattingEventHandler(object sender, DataGridViewCellFormattingEventArgs e);

    public class DataGridViewCellParsingEventArgs : ConvertEventArgs
    {
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public DataGridViewCellStyle InheritedCellStyle { get; set; }
        public bool ParsingApplied { get; set; }
        public DataGridViewCellParsingEventArgs(int rowIndex, int columnIndex, object value, Type desiredType, DataGridViewCellStyle inheritedCellStyle)
            : base(value, desiredType) { RowIndex = rowIndex; ColumnIndex = columnIndex; InheritedCellStyle = inheritedCellStyle; }
    }
    public delegate void DataGridViewCellParsingEventHandler(object sender, DataGridViewCellParsingEventArgs e);

    public class DataGridViewDataErrorEventArgs : DataGridViewCellCancelEventArgs
    {
        public DataGridViewDataErrorContexts Context { get; }
        public Exception Exception { get; }
        public bool ThrowException { get; set; }
        public DataGridViewDataErrorEventArgs(Exception exception, int columnIndex, int rowIndex, DataGridViewDataErrorContexts context)
            : base(columnIndex, rowIndex) { Exception = exception; Context = context; }
    }
    public delegate void DataGridViewDataErrorEventHandler(object sender, DataGridViewDataErrorEventArgs e);

    public class DataGridViewColumnEventArgs : EventArgs
    {
        public DataGridViewColumn Column { get; }
        public DataGridViewColumnEventArgs(DataGridViewColumn dataGridViewColumn) { Column = dataGridViewColumn; }
    }
    public delegate void DataGridViewColumnEventHandler(object sender, DataGridViewColumnEventArgs e);

    public class DataGridViewRowEventArgs : EventArgs
    {
        public DataGridViewRow Row { get; }
        public DataGridViewRowEventArgs(DataGridViewRow dataGridViewRow) { Row = dataGridViewRow; }
    }
    public delegate void DataGridViewRowEventHandler(object sender, DataGridViewRowEventArgs e);

    public class DataGridViewRowCancelEventArgs : CancelEventArgs
    {
        public DataGridViewRow Row { get; }
        public DataGridViewRowCancelEventArgs(DataGridViewRow dataGridViewRow) { Row = dataGridViewRow; }
    }
    public delegate void DataGridViewRowCancelEventHandler(object sender, DataGridViewRowCancelEventArgs e);

    public class DataGridViewRowsAddedEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public int RowCount { get; }
        public DataGridViewRowsAddedEventArgs(int rowIndex, int rowCount) { RowIndex = rowIndex; RowCount = rowCount; }
    }
    public delegate void DataGridViewRowsAddedEventHandler(object sender, DataGridViewRowsAddedEventArgs e);

    public class DataGridViewRowsRemovedEventArgs : EventArgs
    {
        public int RowIndex { get; }
        public int RowCount { get; }
        public DataGridViewRowsRemovedEventArgs(int rowIndex, int rowCount) { RowIndex = rowIndex; RowCount = rowCount; }
    }
    public delegate void DataGridViewRowsRemovedEventHandler(object sender, DataGridViewRowsRemovedEventArgs e);

    public class DataGridViewRowPrePaintEventArgs : HandledEventArgs
    {
        public Graphics Graphics { get; }
        public Rectangle ClipBounds { get; }
        public Rectangle RowBounds { get; }
        public int RowIndex { get; }
        public DataGridViewElementStates State { get; }
        public string ErrorText { get; }
        public DataGridViewCellStyle InheritedRowStyle { get; }
        public bool IsFirstDisplayedRow { get; }
        public bool IsLastVisibleRow { get; }
        public DataGridViewPaintParts PaintParts { get; set; } = DataGridViewPaintParts.All;
        public DataGridViewRowPrePaintEventArgs(DataGridView dataGridView, Graphics graphics, Rectangle clipBounds, Rectangle rowBounds, int rowIndex,
                                                DataGridViewElementStates rowState, string errorText, DataGridViewCellStyle inheritedRowStyle,
                                                bool isFirstDisplayedRow, bool isLastVisibleRow)
        {
            Graphics = graphics; ClipBounds = clipBounds; RowBounds = rowBounds; RowIndex = rowIndex; State = rowState; ErrorText = errorText;
            InheritedRowStyle = inheritedRowStyle; IsFirstDisplayedRow = isFirstDisplayedRow; IsLastVisibleRow = isLastVisibleRow;
        }
        public void DrawFocus(Rectangle bounds, bool cellsPaintSelectionBackground) { }
        public void PaintCells(Rectangle clipBounds, DataGridViewPaintParts paintParts) { }
        public void PaintCellsBackground(Rectangle clipBounds, bool cellsPaintSelectionBackground) { }
        public void PaintCellsContent(Rectangle clipBounds) { }
        public void PaintHeader(DataGridViewPaintParts paintParts) { }
    }
    public delegate void DataGridViewRowPrePaintEventHandler(object sender, DataGridViewRowPrePaintEventArgs e);

    public class DataGridViewCellPaintingEventArgs : HandledEventArgs
    {
        public Graphics Graphics { get; }
        public Rectangle CellBounds { get; }
        public Rectangle ClipBounds { get; }
        public int ColumnIndex { get; }
        public int RowIndex { get; }
        public object Value { get; }
        public object FormattedValue { get; }
        public DataGridViewCellStyle CellStyle { get; }
        public DataGridViewElementStates State { get; }
        public DataGridViewPaintParts PaintParts { get; }
        public DataGridViewCellPaintingEventArgs(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex, int columnIndex,
                                                 DataGridViewElementStates cellState, object value, object formattedValue, DataGridViewCellStyle cellStyle)
        {
            Graphics = graphics; ClipBounds = clipBounds; CellBounds = cellBounds; RowIndex = rowIndex; ColumnIndex = columnIndex; State = cellState;
            Value = value; FormattedValue = formattedValue; CellStyle = cellStyle; PaintParts = DataGridViewPaintParts.All;
        }
        public void Paint(Rectangle clipBounds, DataGridViewPaintParts paintParts) { }
        public void PaintBackground(Rectangle clipBounds, bool cellsPaintSelectionBackground) { }
        public void PaintContent(Rectangle clipBounds) { }
    }
    public delegate void DataGridViewCellPaintingEventHandler(object sender, DataGridViewCellPaintingEventArgs e);

    public class DataGridViewEditingControlShowingEventArgs : EventArgs
    {
        public Control Control { get; }
        public DataGridViewCellStyle CellStyle { get; set; }
        public DataGridViewEditingControlShowingEventArgs(Control control, DataGridViewCellStyle cellStyle) { Control = control; CellStyle = cellStyle; }
    }
    public delegate void DataGridViewEditingControlShowingEventHandler(object sender, DataGridViewEditingControlShowingEventArgs e);

    public class DataGridViewBindingCompleteEventArgs : EventArgs
    {
        public ListChangedType ListChangedType { get; }
        public DataGridViewBindingCompleteEventArgs(ListChangedType listChangedType) { ListChangedType = listChangedType; }
    }
    public delegate void DataGridViewBindingCompleteEventHandler(object sender, DataGridViewBindingCompleteEventArgs e);

    public class DataGridViewSortCompareEventArgs : HandledEventArgs
    {
        public DataGridViewColumn Column { get; }
        public object CellValue1 { get; }
        public object CellValue2 { get; }
        public int RowIndex1 { get; }
        public int RowIndex2 { get; }
        public int SortResult { get; set; }
        public DataGridViewSortCompareEventArgs(DataGridViewColumn dataGridViewColumn, object cellValue1, object cellValue2, int rowIndex1, int rowIndex2)
        {
            Column = dataGridViewColumn; CellValue1 = cellValue1; CellValue2 = cellValue2; RowIndex1 = rowIndex1; RowIndex2 = rowIndex2;
        }
    }
    public delegate void DataGridViewSortCompareEventHandler(object sender, DataGridViewSortCompareEventArgs e);

    // The grid
    [ComplexBindingProperties("DataSource", "DataMember")]
    public class DataGridView : Control, ISupportInitialize
    {
        private readonly DataGridViewColumnCollection _columns;
        private readonly DataGridViewRowCollection _rows;
        internal readonly List<DataGridViewRow> RowsInternal = new List<DataGridViewRow>();
        private readonly HashSet<DataGridViewCell> _selectedCells = new HashSet<DataGridViewCell>();
        private readonly ScrollHelper _scroll;
        private object _dataSource;
        private IList _boundList;
        private PropertyDescriptorCollection _boundProperties;
        private DataGridViewCell _currentCell;
        private DataGridViewCell _anchorCell;
        private Control _editingControl;
        private DataGridViewCell _editingCell;
        private bool _editCanceled;
        private bool _allowUserToAddRows = true;
        private bool _selectionChangeSuspended;
        private bool _selectionChanged;
        private int _resizingColumn = -1, _resizeStartX, _resizeStartWidth;
        private bool _dragSelecting;
        private int _updating;
        private DataGridViewColumn _sortedColumn;
        private SortOrder _sortOrder;
        private BorderStyle _borderStyle = BorderStyle.FixedSingle;

        public DataGridView()
        {
            _columns = new DataGridViewColumnCollection(this);
            _rows = new DataGridViewRowCollection(this);
            _scroll = new ScrollHelper(this, () => new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            _scroll.Scrolled += PlaceEditingControl;
            SetStyle(ControlStyles.Selectable, true);
            SetClientSizeOffset(BorderSizes.Of(_borderStyle));
            RowTemplate = new DataGridViewRow();
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.ControlText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                WrapMode = DataGridViewTriState.False,
                Font = null,
            };
            ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.WindowText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                WrapMode = DataGridViewTriState.True,
            };
            RowHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                BackColor = SystemColors.Control,
                ForeColor = SystemColors.WindowText,
                SelectionBackColor = SystemColors.Highlight,
                SelectionForeColor = SystemColors.HighlightText,
                WrapMode = DataGridViewTriState.True,
            };
            RowsDefaultCellStyle = new DataGridViewCellStyle();
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle();
        }
        protected override Size DefaultSize { get { return new Size(240, 150); } }

        // Properties
        public DataGridViewColumnCollection Columns { get { return _columns; } }
        public DataGridViewRowCollection Rows { get { return _rows; } }
        public DataGridViewCell this[int columnIndex, int rowIndex] { get { return RowsInternal[rowIndex].Cells[columnIndex]; } set { RowsInternal[rowIndex].Cells[columnIndex] = value; } }
        public DataGridViewCell this[string columnName, int rowIndex] { get { return RowsInternal[rowIndex].Cells[columnName]; } }
        public int ColumnCount
        {
            get { return _columns.Count; }
            set
            {
                while (_columns.Count < value) _columns.Add(new DataGridViewTextBoxColumn { Name = "Column" + (_columns.Count + 1) });
                while (_columns.Count > value) _columns.RemoveAt(_columns.Count - 1);
            }
        }
        public int RowCount
        {
            get { return RowsInternal.Count; }
            set
            {
                while (RowsInternal.Count < value) _rows.Add();
                while (RowsInternal.Count > value && RowsInternal.Count > 0) RemoveRow(RowsInternal[RowsInternal.Count - 1 - (NewRowIndex >= 0 ? 1 : 0)]);
            }
        }
        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                EndEdit();
                if (_dataSource is IBindingList oldList) oldList.ListChanged -= BoundListChanged;
                _dataSource = value;
                _boundList = ResolveList(value);
                if (_boundList is IBindingList list) list.ListChanged += BoundListChanged;
                _boundProperties = BoundProperties();
                if (AutoGenerateColumns) GenerateColumns();
                RebuildBoundRows();
                OnDataSourceChanged(EventArgs.Empty);
                OnDataBindingComplete(new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
            }
        }
        public string DataMember { get; set; } = string.Empty;
        public bool AutoGenerateColumns { get; set; } = true;
        public bool AllowUserToAddRows { get { return _allowUserToAddRows; } set { _allowUserToAddRows = value; UpdateNewRow(); } }
        public bool AllowUserToDeleteRows { get; set; } = true;
        public bool AllowUserToResizeRows { get; set; } = true;
        public bool AllowUserToResizeColumns { get; set; } = true;
        public bool AllowUserToOrderColumns { get; set; }
        public bool ReadOnly { get; set; }
        public bool MultiSelect { get; set; } = true;
        public DataGridViewSelectionMode SelectionMode { get; set; } = DataGridViewSelectionMode.RowHeaderSelect;
        public DataGridViewEditMode EditMode { get; set; } = DataGridViewEditMode.EditOnKeystrokeOrF2;
        public DataGridViewClipboardCopyMode ClipboardCopyMode { get; set; } = DataGridViewClipboardCopyMode.EnableWithAutoHeaderText;
        public bool RowHeadersVisible { get; set; } = true;
        public int RowHeadersWidth { get; set; } = 41;
        public DataGridViewRowHeadersWidthSizeMode RowHeadersWidthSizeMode { get; set; }
        public bool ColumnHeadersVisible { get; set; } = true;
        public int ColumnHeadersHeight { get; set; } = 23;
        public DataGridViewColumnHeadersHeightSizeMode ColumnHeadersHeightSizeMode { get; set; } = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
        public DataGridViewCellStyle DefaultCellStyle { get; set; }
        public DataGridViewCellStyle ColumnHeadersDefaultCellStyle { get; set; }
        public DataGridViewCellStyle RowHeadersDefaultCellStyle { get; set; }
        public DataGridViewCellStyle RowsDefaultCellStyle { get; set; }
        public DataGridViewCellStyle AlternatingRowsDefaultCellStyle { get; set; }
        public DataGridViewRow RowTemplate { get; set; }
        public DataGridViewAutoSizeColumnsMode AutoSizeColumnsMode { get; set; } = DataGridViewAutoSizeColumnsMode.None;
        public DataGridViewAutoSizeRowsMode AutoSizeRowsMode { get; set; } = DataGridViewAutoSizeRowsMode.None;
        public Color BackgroundColor { get; set; } = SystemColors.AppWorkspace;
        public Color GridColor { get; set; } = Color.FromArgb(0xA0, 0xA0, 0xA0);
        public BorderStyle BorderStyle { get { return _borderStyle; } set { _borderStyle = value; SetClientSizeOffset(BorderSizes.Of(value)); Invalidate(); } }
        public DataGridViewCellBorderStyle CellBorderStyle { get; set; } = DataGridViewCellBorderStyle.Single;
        public DataGridViewHeaderBorderStyle ColumnHeadersBorderStyle { get; set; } = DataGridViewHeaderBorderStyle.Raised;
        public DataGridViewHeaderBorderStyle RowHeadersBorderStyle { get; set; } = DataGridViewHeaderBorderStyle.Raised;
        public bool EnableHeadersVisualStyles { get; set; } = true;
        public bool ShowCellToolTips { get; set; } = true;
        public bool ShowCellErrors { get; set; } = true;
        public bool ShowRowErrors { get; set; } = true;
        public bool ShowEditingIcon { get; set; } = true;
        public bool StandardTab { get; set; }
        public bool VirtualMode { get; set; }
        public ScrollBars ScrollBars { get; set; } = ScrollBars.Both;
        public DataGridViewColumn SortedColumn { get { return _sortedColumn; } }
        public SortOrder SortOrder { get { return _sortOrder; } }
        public int NewRowIndex { get { int i = RowsInternal.FindIndex(r => r.IsNewRowInternal); return i; } }
        public bool IsCurrentCellInEditMode { get { return _editingControl != null; } }
        public bool IsCurrentCellDirty { get { return _editingControl is IDataGridViewEditingControl e && e.EditingControlValueChanged; } }
        public bool IsCurrentRowDirty { get { return IsCurrentCellDirty; } }
        public Control EditingControl { get { return _editingControl; } }
        public Panel EditingPanel { get; } = new Panel();
        public DataGridViewRow CurrentRow { get { return _currentCell?.OwningRowInternal; } }
        public Point CurrentCellAddress { get { return _currentCell == null ? new Point(-1, -1) : new Point(_currentCell.ColumnIndex, _currentCell.RowIndex); } }
        public DataGridViewCell CurrentCell
        {
            get { return _currentCell; }
            set { SetCurrentCell(value, true, true); }
        }
        public DataGridViewCell FirstDisplayedCell { get { return RowsInternal.Count > 0 && _columns.Count > 0 ? this[0, FirstDisplayedScrollingRowIndex] : null; } set { } }
        public int FirstDisplayedScrollingRowIndex
        {
            get { return RowIndexAtY(ColumnHeaderHeight); }
            set { if (value >= 0 && value < RowsInternal.Count) _scroll.SetOffset(_scroll.OffsetX, RowTop(value)); }
        }
        public int FirstDisplayedScrollingColumnIndex { get { return 0; } set { } }
        public int HorizontalScrollingOffset { get { return _scroll.OffsetX; } set { _scroll.SetOffset(value, _scroll.OffsetY); } }
        public DataGridViewSelectedCellCollection SelectedCells
        {
            get
            {
                List<DataGridViewCell> list = _selectedCells.Where(c => c.OwningRowInternal?.DataGridView == this && c.OwningRowInternal.Index >= 0 && c.ColumnIndex >= 0).ToList();
                list.Sort((a, b) => a.RowIndex != b.RowIndex ? b.RowIndex.CompareTo(a.RowIndex) : b.ColumnIndex.CompareTo(a.ColumnIndex));
                return new DataGridViewSelectedCellCollection(list);
            }
        }
        public DataGridViewSelectedRowCollection SelectedRows
        {
            get
            {
                List<DataGridViewRow> rows = RowsInternal.Where(r => IsRowSelected(r)).Reverse().ToList();
                return new DataGridViewSelectedRowCollection(rows);
            }
        }
        public override Color BackColor { get { return BackColorSet ? base.BackColor : SystemColors.Control; } set { base.BackColor = value; } }

        // Events
        public event DataGridViewCellEventHandler CellClick;
        public event DataGridViewCellEventHandler CellContentClick;
        public event DataGridViewCellEventHandler CellDoubleClick;
        public event DataGridViewCellEventHandler CellContentDoubleClick;
        public event DataGridViewCellMouseEventHandler CellMouseClick;
        public event DataGridViewCellMouseEventHandler CellMouseDoubleClick;
        public event DataGridViewCellMouseEventHandler CellMouseDown;
        public event DataGridViewCellMouseEventHandler CellMouseUp;
        public event DataGridViewCellEventHandler CellMouseEnter;
        public event DataGridViewCellEventHandler CellMouseLeave;
        public event DataGridViewCellEventHandler CellEnter;
        public event DataGridViewCellEventHandler CellLeave;
        public event DataGridViewCellCancelEventHandler CellBeginEdit;
        public event DataGridViewCellEventHandler CellEndEdit;
        public event DataGridViewCellValidatingEventHandler CellValidating;
        public event DataGridViewCellEventHandler CellValidated;
        public event DataGridViewCellEventHandler CellValueChanged;
        public event DataGridViewCellFormattingEventHandler CellFormatting;
        public event DataGridViewCellParsingEventHandler CellParsing;
        public event DataGridViewCellPaintingEventHandler CellPainting;
        public event DataGridViewDataErrorEventHandler DataError;
        public event EventHandler SelectionChanged;
        public event EventHandler CurrentCellChanged;
        public event EventHandler CurrentCellDirtyStateChanged;
        public event DataGridViewRowsAddedEventHandler RowsAdded;
        public event DataGridViewRowsRemovedEventHandler RowsRemoved;
        public event DataGridViewRowCancelEventHandler UserDeletingRow;
        public event DataGridViewRowEventHandler UserDeletedRow;
        public event DataGridViewRowEventHandler UserAddedRow;
        public event DataGridViewRowEventHandler DefaultValuesNeeded;
        public event DataGridViewCellEventHandler RowEnter;
        public event DataGridViewCellEventHandler RowLeave;
        public event DataGridViewCellCancelEventHandler RowValidating;
        public event DataGridViewCellEventHandler RowValidated;
        public event DataGridViewRowPrePaintEventHandler RowPrePaint;
        public event DataGridViewColumnEventHandler ColumnAdded;
        public event DataGridViewColumnEventHandler ColumnRemoved;
        public event DataGridViewColumnEventHandler ColumnWidthChanged;
        public event DataGridViewCellMouseEventHandler ColumnHeaderMouseClick;
        public event DataGridViewCellMouseEventHandler ColumnHeaderMouseDoubleClick;
        public event DataGridViewCellMouseEventHandler RowHeaderMouseClick;
        public event DataGridViewCellMouseEventHandler RowHeaderMouseDoubleClick;
        public event DataGridViewEditingControlShowingEventHandler EditingControlShowing;
        public event DataGridViewBindingCompleteEventHandler DataBindingComplete;
        public event EventHandler DataSourceChanged;
        public event DataGridViewSortCompareEventHandler SortCompare;
        public event EventHandler Sorted;

        protected virtual void OnCellClick(DataGridViewCellEventArgs e) { CellClick?.Invoke(this, e); }
        // The test automation clicks a cell as the mouse does
        internal void AutomationClickCell(int columnIndex, int rowIndex)
        {
            CurrentCell = Rows[rowIndex].Cells[columnIndex];
            OnCellMouseDown(new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 1, 1,
                                                                new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0)));
            OnCellClick(new DataGridViewCellEventArgs(columnIndex, rowIndex));
            OnCellContentClick(new DataGridViewCellEventArgs(columnIndex, rowIndex));
            OnCellMouseUp(new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 1, 1,
                                                              new MouseEventArgs(MouseButtons.Left, 1, 1, 1, 0)));
        }
        protected virtual void OnCellContentClick(DataGridViewCellEventArgs e) { CellContentClick?.Invoke(this, e); }
        protected virtual void OnCellDoubleClick(DataGridViewCellEventArgs e) { CellDoubleClick?.Invoke(this, e); }
        protected virtual void OnCellMouseClick(DataGridViewCellMouseEventArgs e) { CellMouseClick?.Invoke(this, e); }
        protected virtual void OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e) { CellMouseDoubleClick?.Invoke(this, e); }
        protected virtual void OnCellMouseDown(DataGridViewCellMouseEventArgs e) { CellMouseDown?.Invoke(this, e); }
        protected virtual void OnCellMouseUp(DataGridViewCellMouseEventArgs e) { CellMouseUp?.Invoke(this, e); }
        protected virtual void OnCellEnter(DataGridViewCellEventArgs e) { CellEnter?.Invoke(this, e); }
        protected virtual void OnCellLeave(DataGridViewCellEventArgs e) { CellLeave?.Invoke(this, e); }
        protected virtual void OnCellBeginEdit(DataGridViewCellCancelEventArgs e) { CellBeginEdit?.Invoke(this, e); }
        protected virtual void OnCellEndEdit(DataGridViewCellEventArgs e) { CellEndEdit?.Invoke(this, e); }
        protected virtual void OnCellValidating(DataGridViewCellValidatingEventArgs e) { CellValidating?.Invoke(this, e); }
        protected virtual void OnCellValidated(DataGridViewCellEventArgs e) { CellValidated?.Invoke(this, e); }
        protected virtual void OnCellValueChanged(DataGridViewCellEventArgs e) { CellValueChanged?.Invoke(this, e); }
        protected virtual void OnCellFormatting(DataGridViewCellFormattingEventArgs e) { CellFormatting?.Invoke(this, e); }
        protected virtual void OnCellParsing(DataGridViewCellParsingEventArgs e) { CellParsing?.Invoke(this, e); }
        protected virtual void OnSelectionChanged(EventArgs e) { SelectionChanged?.Invoke(this, e); }
        protected virtual void OnCurrentCellChanged(EventArgs e) { CurrentCellChanged?.Invoke(this, e); }
        protected virtual void OnRowsAdded(DataGridViewRowsAddedEventArgs e) { RowsAdded?.Invoke(this, e); }
        protected virtual void OnRowsRemoved(DataGridViewRowsRemovedEventArgs e) { RowsRemoved?.Invoke(this, e); }
        protected virtual void OnUserDeletingRow(DataGridViewRowCancelEventArgs e) { UserDeletingRow?.Invoke(this, e); }
        protected virtual void OnUserDeletedRow(DataGridViewRowEventArgs e) { UserDeletedRow?.Invoke(this, e); }
        protected virtual void OnUserAddedRow(DataGridViewRowEventArgs e) { UserAddedRow?.Invoke(this, e); }
        protected virtual void OnRowEnter(DataGridViewCellEventArgs e) { RowEnter?.Invoke(this, e); }
        protected virtual void OnRowLeave(DataGridViewCellEventArgs e) { RowLeave?.Invoke(this, e); }
        protected virtual void OnRowPrePaint(DataGridViewRowPrePaintEventArgs e) { RowPrePaint?.Invoke(this, e); }
        protected virtual void OnColumnAdded(DataGridViewColumnEventArgs e) { ColumnAdded?.Invoke(this, e); }
        protected virtual void OnColumnRemoved(DataGridViewColumnEventArgs e) { ColumnRemoved?.Invoke(this, e); }
        protected virtual void OnColumnWidthChanged(DataGridViewColumnEventArgs e) { ColumnWidthChanged?.Invoke(this, e); }
        protected virtual void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e) { ColumnHeaderMouseClick?.Invoke(this, e); }
        protected virtual void OnRowHeaderMouseClick(DataGridViewCellMouseEventArgs e) { RowHeaderMouseClick?.Invoke(this, e); }
        protected virtual void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e) { EditingControlShowing?.Invoke(this, e); }
        protected virtual void OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e) { DataBindingComplete?.Invoke(this, e); }
        protected virtual void OnDataSourceChanged(EventArgs e) { DataSourceChanged?.Invoke(this, e); }
        protected virtual void OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e)
        {
            if (DataError != null) DataError(this, e);
            else if (displayErrorDialogIfNoHandler)
            {
                MessageBox.Show("The following exception occurred in the DataGridView:" + Environment.NewLine + Environment.NewLine + e.Exception?.Message,
                                "DataGridView Default Error Dialog", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (e.ThrowException && e.Exception != null) throw e.Exception;
        }
        internal void RaiseColumnWidthChanged(DataGridViewColumn column) { OnColumnWidthChanged(new DataGridViewColumnEventArgs(column)); }

        // Data binding
        private static IList ResolveList(object dataSource)
        {
            if (dataSource == null) return null;
            if (dataSource is IListSource source) return source.GetList();
            if (dataSource is IList list) return list;
            if (dataSource is IEnumerable enumerable && !(dataSource is string)) return enumerable.Cast<object>().ToList();
            return null;
        }
        private PropertyDescriptorCollection BoundProperties()
        {
            if (_boundList == null) return null;
            if (_boundList is ITypedList typed) return typed.GetItemProperties(null);
            Type itemType = null;
            foreach (Type i in _boundList.GetType().GetInterfaces())
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)) itemType = i.GetGenericArguments()[0];
            if ((itemType == null || itemType == typeof(object)) && _boundList.Count > 0 && _boundList[0] != null) itemType = _boundList[0].GetType();
            return itemType == null ? new PropertyDescriptorCollection(new PropertyDescriptor[0]) : TypeDescriptor.GetProperties(itemType, new Attribute[] { BrowsableAttribute.Yes });
        }
        private void GenerateColumns()
        {
            foreach (DataGridViewColumn c in _columns.List.ToArray()) if (c.IsDataBound && c.Tag == null && c.DataPropertyName.Length > 0 && AutoGeneratedColumns.Contains(c)) _columns.Remove(c);
            if (_boundProperties == null) return;
            foreach (PropertyDescriptor pd in _boundProperties)
            {
                if (!pd.IsBrowsable) continue;
                if (_columns.List.Any(c => string.Equals(c.DataPropertyName, pd.Name, StringComparison.OrdinalIgnoreCase))) continue;
                DataGridViewColumn column = pd.PropertyType == typeof(bool) ? new DataGridViewCheckBoxColumn() : (DataGridViewColumn)new DataGridViewTextBoxColumn();
                column.Name = pd.Name;
                column.DataPropertyName = pd.Name;
                column.HeaderText = pd.DisplayName;
                column.ValueType = pd.PropertyType;
                column.ReadOnly = pd.IsReadOnly;
                AutoGeneratedColumns.Add(column);
                _columns.Add(column);
            }
        }
        private readonly HashSet<DataGridViewColumn> AutoGeneratedColumns = new HashSet<DataGridViewColumn>();
        private void BoundListChanged(object sender, ListChangedEventArgs e)
        {
            if (_updating > 0) return;
            if (e.ListChangedType == ListChangedType.PropertyDescriptorChanged || e.ListChangedType == ListChangedType.PropertyDescriptorAdded ||
                e.ListChangedType == ListChangedType.PropertyDescriptorDeleted)
            {
                _boundProperties = BoundProperties();
                if (AutoGenerateColumns) GenerateColumns();
            }
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                Invalidate();
                return;
            }
            // A list that got its first item tells the item type
            if (_boundProperties == null || _boundProperties.Count == 0)
            {
                _boundProperties = BoundProperties();
                if (AutoGenerateColumns) GenerateColumns();
            }
            RebuildBoundRows();
            OnDataBindingComplete(new DataGridViewBindingCompleteEventArgs(e.ListChangedType));
        }
        private void RebuildBoundRows()
        {
            if (_boundList == null && _dataSource == null)
            {
                UpdateNewRow();
                return;
            }
            int currentColumn = _currentCell?.ColumnIndex ?? -1;
            int currentRow = _currentCell?.RowIndex ?? -1;
            int oldCount = RowsInternal.Count;
            CancelEditInternal();
            RowsInternal.Clear();
            _selectedCells.Clear();
            _currentCell = null;
            if (_boundList != null)
            {
                foreach (object item in _boundList)
                {
                    DataGridViewRow row = (DataGridViewRow)RowTemplate.Clone();
                    row.DataGridView = this;
                    row.BoundItem = item;
                    row.CreateCells(this);
                    foreach (DataGridViewCell cell in row.Cells.List) cell.DataGridView = this;
                    RowsInternal.Add(row);
                }
            }
            if (oldCount > 0) OnRowsRemoved(new DataGridViewRowsRemovedEventArgs(0, oldCount));
            UpdateNewRow();
            if (RowsInternal.Count > 0) OnRowsAdded(new DataGridViewRowsAddedEventArgs(0, RowsInternal.Count));
            if (RowsInternal.Count > 0 && _columns.Count > 0)
            {
                int r = Math.Max(0, Math.Min(currentRow < 0 ? 0 : currentRow, RowsInternal.Count - 1));
                int c = FirstVisibleColumn(currentColumn < 0 ? 0 : Math.Min(currentColumn, _columns.Count - 1));
                if (c >= 0) SetCurrentCell(this[c, r], true, false);
            }
            LayoutChanged();
        }
        private int FirstVisibleColumn(int start)
        {
            for (int i = start; i < _columns.Count; i++) if (_columns[i].Visible) return i;
            for (int i = 0; i < start && i < _columns.Count; i++) if (_columns[i].Visible) return i;
            return -1;
        }
        private bool CanAddNewRow
        {
            get
            {
                if (!_allowUserToAddRows || ReadOnly || _columns.Count == 0) return false;
                if (_boundList == null) return _dataSource == null;
                if (_dataSource is BindingSource bs) return bs.AllowNew;
                if (_boundList is IBindingList b) return b.AllowNew;
                return false;
            }
        }
        private void UpdateNewRow()
        {
            int index = NewRowIndex;
            bool need = CanAddNewRow;
            if (need && index < 0)
            {
                DataGridViewRow row = (DataGridViewRow)RowTemplate.Clone();
                row.DataGridView = this;
                row.IsNewRowInternal = true;
                row.CreateCells(this);
                foreach (DataGridViewCell cell in row.Cells.List) cell.DataGridView = this;
                RowsInternal.Add(row);
            }
            else if (!need && index >= 0)
            {
                DataGridViewRow row = RowsInternal[index];
                if (_currentCell?.OwningRowInternal == row) _currentCell = null;
                RowsInternal.RemoveAt(index);
            }
            LayoutChanged();
        }
        // The new row becomes a row of the list when a value is entered
        private void CommitNewRow(DataGridViewRow row)
        {
            if (!row.IsNewRowInternal) return;
            if (_boundList != null)
            {
                _updating++;
                try
                {
                    object item;
                    if (_dataSource is BindingSource bs) item = bs.AddNew();
                    else if (_boundList is IBindingList bl) item = bl.AddNew();
                    else throw new InvalidOperationException("No new rows can be added.");
                    if (item is System.Data.DataRowView drv) drv.EndEdit();
                    row.BoundItem = item;
                }
                finally
                {
                    _updating--;
                }
            }
            row.IsNewRowInternal = false;
            UpdateNewRow();
            OnUserAddedRow(new DataGridViewRowEventArgs(RowsInternal[RowsInternal.Count - 1]));
            OnRowsAdded(new DataGridViewRowsAddedEventArgs(row.Index, 1));
        }
        internal void EnsureCells(DataGridViewRow row)
        {
            List<DataGridViewCell> cells = row.Cells.List;
            while (cells.Count < _columns.Count)
            {
                DataGridViewCell cell = (DataGridViewCell)(_columns[cells.Count].CellTemplate?.Clone() ?? new DataGridViewTextBoxCell());
                cell.OwningRowInternal = row;
                cell.DataGridView = row.DataGridView;
                cells.Add(cell);
            }
            for (int i = 0; i < cells.Count && i < _columns.Count; i++)
            {
                cells[i].OwningColumnInternal = _columns[i];
                cells[i].OwningRowInternal = row;
                if (cells[i].DataGridView == null) cells[i].DataGridView = row.DataGridView;
            }
        }
        internal object GetCellValue(DataGridViewCell cell)
        {
            DataGridViewRow row = cell.OwningRowInternal;
            DataGridViewColumn column = cell.OwningColumnInternal;
            if (row == null || column == null) return cell.StoredValue;
            if (row.IsNewRowInternal) return cell.StoredValue;
            if (row.BoundItem != null && !string.IsNullOrEmpty(column.DataPropertyName) && _boundProperties != null)
            {
                PropertyDescriptor pd = _boundProperties.Find(column.DataPropertyName, true);
                if (pd != null)
                {
                    object value = pd.GetValue(row.BoundItem);
                    return value is DBNull ? null : value;
                }
            }
            return cell.StoredValue;
        }
        internal void SetCellValue(DataGridViewCell cell, object value, bool raise)
        {
            DataGridViewRow row = cell.OwningRowInternal;
            DataGridViewColumn column = cell.OwningColumnInternal;
            if (row != null && row.IsNewRowInternal && column != null)
            {
                CommitNewRow(row);
                cell = row.Cells[column.Index];
            }
            if (row?.BoundItem != null && column != null && !string.IsNullOrEmpty(column.DataPropertyName) && _boundProperties != null)
            {
                PropertyDescriptor pd = _boundProperties.Find(column.DataPropertyName, true);
                if (pd != null)
                {
                    object converted = value;
                    if (value is string s && pd.PropertyType != typeof(string) && pd.PropertyType != typeof(object))
                        converted = ConvertForProperty(pd, s);
                    else if (value == null && pd.PropertyType.IsValueType && Nullable.GetUnderlyingType(pd.PropertyType) == null)
                        converted = row.BoundItem is System.Data.DataRowView ? (object)DBNull.Value : Activator.CreateInstance(pd.PropertyType);
                    _updating++;
                    try
                    {
                        pd.SetValue(row.BoundItem, converted);
                    }
                    finally
                    {
                        _updating--;
                    }
                }
            }
            else cell.StoredValue = value;
            Invalidate();
            if (raise && column != null && row != null) OnCellValueChanged(new DataGridViewCellEventArgs(column.Index, row.Index));
        }
        private static object ConvertForProperty(PropertyDescriptor pd, string text)
        {
            Type type = Nullable.GetUnderlyingType(pd.PropertyType) ?? pd.PropertyType;
            if (string.IsNullOrEmpty(text) && type != pd.PropertyType) return null;
            TypeConverter converter = pd.Converter;
            if (converter != null && converter.CanConvertFrom(typeof(string))) return converter.ConvertFromString(null, CultureInfo.CurrentCulture, text);
            return Convert.ChangeType(text, type, CultureInfo.CurrentCulture);
        }
        internal object ParseValue(DataGridViewCell cell, string text)
        {
            DataGridViewColumn column = cell.OwningColumnInternal;
            DataGridViewCellParsingEventArgs parsing = new DataGridViewCellParsingEventArgs(cell.RowIndex, cell.ColumnIndex, text, cell.ValueType, InheritedStyle(cell));
            OnCellParsing(parsing);
            if (parsing.ParsingApplied) return parsing.Value;
            PropertyDescriptor pd = column != null && _boundProperties != null && !string.IsNullOrEmpty(column.DataPropertyName) ? _boundProperties.Find(column.DataPropertyName, true) : null;
            if (pd != null) return pd.PropertyType == typeof(string) || pd.PropertyType == typeof(object) ? text : ConvertForProperty(pd, text);
            Type type = column?.ValueType;
            if (type == null || type == typeof(string) || type == typeof(object)) return text;
            if (string.IsNullOrEmpty(text)) return null;
            TypeConverter converter = TypeDescriptor.GetConverter(type);
            return converter.ConvertFromString(null, CultureInfo.CurrentCulture, text);
        }
        internal string FormatCellValue(DataGridViewCell cell)
        {
            object value = cell.Value;
            DataGridViewCellStyle style = InheritedStyle(cell);
            if (Toolkit.IsInitialized)
            {
                DataGridViewCellFormattingEventArgs formatting = new DataGridViewCellFormattingEventArgs(cell.ColumnIndex, cell.RowIndex, value, typeof(string), style);
                OnCellFormatting(formatting);
                if (formatting.FormattingApplied) return formatting.Value?.ToString() ?? string.Empty;
                value = formatting.Value;
            }
            if (value == null || value is DBNull) return style.NullValue?.ToString() ?? string.Empty;
            if (value is IFormattable formattable && !string.IsNullOrEmpty(style.Format))
                return formattable.ToString(style.Format, style.FormatProvider ?? CultureInfo.CurrentCulture);
            if (value is double d) return d.ToString(CultureInfo.CurrentCulture);
            if (value is float f) return f.ToString(CultureInfo.CurrentCulture);
            PropertyDescriptor pd = cell.OwningColumnInternal != null && _boundProperties != null && !string.IsNullOrEmpty(cell.OwningColumnInternal.DataPropertyName)
                                    ? _boundProperties.Find(cell.OwningColumnInternal.DataPropertyName, true) : null;
            if (pd != null && pd.Converter != null && pd.Converter.CanConvertTo(typeof(string)))
            {
                try { return pd.Converter.ConvertToString(null, CultureInfo.CurrentCulture, value); }
                catch { }
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }
        internal DataGridViewCellStyle InheritedStyle(DataGridViewCell cell)
        {
            DataGridViewCellStyle style = DefaultCellStyle.Clone();
            if (style.Font == null) style.Font = Font;
            if (cell.OwningColumnInternal != null && cell.OwningColumnInternal.HasDefaultCellStyle) style.ApplyStyle(cell.OwningColumnInternal.DefaultCellStyle);
            style.ApplyStyle(RowsDefaultCellStyle);
            if (cell.OwningRowInternal != null && cell.OwningRowInternal.Index % 2 == 1) style.ApplyStyle(AlternatingRowsDefaultCellStyle);
            if (cell.OwningRowInternal != null && cell.OwningRowInternal.HasDefaultCellStyle) style.ApplyStyle(cell.OwningRowInternal.DefaultCellStyle);
            if (cell.HasStyle) style.ApplyStyle(cell.Style);
            return style;
        }

        // Rows and columns
        internal int InsertRow(int index, DataGridViewRow row)
        {
            row.DataGridView = this;
            foreach (DataGridViewCell cell in row.Cells.List) cell.DataGridView = this;
            index = Math.Max(0, Math.Min(index, RowsInternal.Count));
            RowsInternal.Insert(index, row);
            EnsureCells(row);
            LayoutChanged();
            OnRowsAdded(new DataGridViewRowsAddedEventArgs(index, 1));
            return index;
        }
        internal void RemoveRow(DataGridViewRow row)
        {
            int index = RowsInternal.IndexOf(row);
            if (index < 0 || row.IsNewRowInternal) return;
            if (_currentCell?.OwningRowInternal == row) CancelEditInternal();
            foreach (DataGridViewCell cell in row.Cells.List) _selectedCells.Remove(cell);
            if (row.BoundItem != null && _boundList != null)
            {
                _updating++;
                try
                {
                    if (_dataSource is BindingSource bs) bs.Remove(row.BoundItem);
                    else if (row.BoundItem is System.Data.DataRowView drv) drv.Delete();
                    else _boundList.Remove(row.BoundItem);
                }
                finally
                {
                    _updating--;
                }
            }
            RowsInternal.Remove(row);
            if (_currentCell?.OwningRowInternal == row)
            {
                _currentCell = null;
                if (RowsInternal.Count > 0 && _columns.Count > 0)
                {
                    int c = FirstVisibleColumn(0);
                    if (c >= 0) SetCurrentCell(this[c, Math.Min(index, RowsInternal.Count - 1)], true, true);
                }
            }
            LayoutChanged();
            OnRowsRemoved(new DataGridViewRowsRemovedEventArgs(index, 1));
        }
        internal void ClearRows()
        {
            if (_dataSource != null) throw new InvalidOperationException("Rows cannot be cleared when the control is data-bound.");
            CancelEditInternal();
            int count = RowsInternal.Count(r => !r.IsNewRowInternal);
            RowsInternal.RemoveAll(r => !r.IsNewRowInternal);
            _selectedCells.Clear();
            _currentCell = null;
            LayoutChanged();
            if (count > 0) OnRowsRemoved(new DataGridViewRowsRemovedEventArgs(0, count));
        }
        internal void OnColumnAddedInternal(DataGridViewColumn column)
        {
            foreach (DataGridViewRow row in RowsInternal) EnsureCells(row);
            UpdateNewRow();
            LayoutChanged();
            OnColumnAdded(new DataGridViewColumnEventArgs(column));
        }
        internal void OnColumnRemovedInternal(int index)
        {
            CancelEditInternal();
            foreach (DataGridViewRow row in RowsInternal)
            {
                if (index < 0) row.Cells.List.Clear();
                else if (index < row.Cells.List.Count) row.Cells.List.RemoveAt(index);
            }
            if (index < 0) _currentCell = null;
            _selectedCells.RemoveWhere(c => c.OwningColumnInternal == null || c.OwningColumnInternal.DataGridView != this);
            UpdateNewRow();
            LayoutChanged();
        }
        internal void ColumnsChanged() { LayoutChanged(); }
        internal void RowsChanged() { LayoutChanged(); }

        // Layout
        private int RowHeaderWidth { get { return RowHeadersVisible ? RowHeadersWidth : 0; } }
        private int ColumnHeaderHeight
        {
            get
            {
                if (!ColumnHeadersVisible) return 0;
                if (ColumnHeadersHeightSizeMode != DataGridViewColumnHeadersHeightSizeMode.AutoSize) return ColumnHeadersHeight;
                int height = 23;
                Font font = ColumnHeadersDefaultCellStyle.Font ?? Font;
                foreach (DataGridViewColumn c in _columns.List)
                {
                    if (!c.Visible) continue;
                    Size s = ContentLayout.MeasureText(c.HeaderText, font, Math.Max(10, c.WidthInternal - 8));
                    height = Math.Max(height, s.Height + 8);
                }
                return height;
            }
        }
        private int RowTop(int index)
        {
            int y = 0;
            for (int i = 0; i < index && i < RowsInternal.Count; i++) if (RowsInternal[i].Visible) y += RowsInternal[i].Height;
            return y;
        }
        private int RowIndexAtY(int y)
        {
            int content = y - ColumnHeaderHeight + _scroll.OffsetY;
            if (y < ColumnHeaderHeight) return -1;
            int top = 0;
            for (int i = 0; i < RowsInternal.Count; i++)
            {
                if (!RowsInternal[i].Visible) continue;
                int h = RowsInternal[i].Height;
                if (content >= top && content < top + h) return i;
                top += h;
            }
            return -1;
        }
        private int ColumnLeft(int index)
        {
            int x = 0;
            for (int i = 0; i < index; i++) if (_columns[i].Visible) x += _columns[i].WidthInternal;
            return x;
        }
        private int ColumnIndexAtX(int x)
        {
            if (x < RowHeaderWidth) return -1;
            int content = x - RowHeaderWidth + _scroll.OffsetX;
            int left = 0;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].Visible) continue;
                int w = _columns[i].WidthInternal;
                if (content >= left && content < left + w) return i;
                left += w;
            }
            return -1;
        }
        private bool _layoutPending;
        private void LayoutChanged()
        {
            if (_layoutPending) return;
            _layoutPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _layoutPending = false;
                ApplyLayout();
            }, DispatcherPriority.Render);
            ApplyLayout();
            _layoutPending = false;
        }
        private void ApplyLayout()
        {
            if (IsDisposed) return;
            AutoSizeColumns();
            int width = _columns.List.Where(c => c.Visible).Sum(c => c.WidthInternal);
            int height = RowsInternal.Where(r => r.Visible).Sum(r => r.Height);
            _scroll.SmallChangeY = RowTemplate.Height;
            _scroll.SetExtent(width + RowHeaderWidth, height, ColumnHeaderHeight);
            PlaceEditingControl();
            Invalidate();
        }
        private void AutoSizeColumns()
        {
            int available = ClientSize.Width - RowHeaderWidth - (_scroll.VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0) - 1;
            float fillWeight = 0;
            int fixedWidth = 0;
            foreach (DataGridViewColumn c in _columns.List)
            {
                if (!c.Visible) continue;
                DataGridViewAutoSizeColumnMode mode = c.InheritedAutoSizeMode;
                if (mode == DataGridViewAutoSizeColumnMode.Fill) fillWeight += c.FillWeight;
                else
                {
                    if (mode == DataGridViewAutoSizeColumnMode.AllCells || mode == DataGridViewAutoSizeColumnMode.AllCellsExceptHeader ||
                        mode == DataGridViewAutoSizeColumnMode.DisplayedCells || mode == DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader ||
                        mode == DataGridViewAutoSizeColumnMode.ColumnHeader)
                        c.WidthInternal = Math.Max(c.MinimumWidth, PreferredColumnWidth(c, mode));
                    fixedWidth += c.WidthInternal;
                }
            }
            if (fillWeight > 0)
            {
                int rest = Math.Max(0, available - fixedWidth);
                foreach (DataGridViewColumn c in _columns.List)
                    if (c.Visible && c.InheritedAutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                        c.WidthInternal = Math.Max(c.MinimumWidth, (int)(rest * c.FillWeight / fillWeight));
            }
        }
        internal int PreferredColumnWidth(DataGridViewColumn column, DataGridViewAutoSizeColumnMode mode)
        {
            int width = 0;
            if (mode != DataGridViewAutoSizeColumnMode.AllCellsExceptHeader && mode != DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader)
                width = ContentLayout.MeasureText(column.HeaderText, ColumnHeadersDefaultCellStyle.Font ?? Font).Width + 12;
            if (mode != DataGridViewAutoSizeColumnMode.ColumnHeader)
            {
                int index = column.Index;
                foreach (DataGridViewRow row in RowsInternal)
                {
                    if (row.IsNewRowInternal || index >= row.Cells.Count) continue;
                    DataGridViewCell cell = row.Cells[index];
                    width = Math.Max(width, ContentLayout.MeasureText(FormatCellValue(cell), InheritedStyle(cell).Font ?? Font).Width + 8);
                }
            }
            return width;
        }
        public void AutoResizeColumns() { AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells); }
        public void AutoResizeColumns(DataGridViewAutoSizeColumnsMode autoSizeColumnsMode)
        {
            foreach (DataGridViewColumn c in _columns.List) c.WidthInternal = Math.Max(c.MinimumWidth, PreferredColumnWidth(c, (DataGridViewAutoSizeColumnMode)(int)autoSizeColumnsMode));
            LayoutChanged();
        }
        public void AutoResizeColumn(int columnIndex) { AutoResizeColumn(columnIndex, DataGridViewAutoSizeColumnMode.AllCells); }
        public void AutoResizeColumn(int columnIndex, DataGridViewAutoSizeColumnMode autoSizeColumnMode)
        {
            DataGridViewColumn c = _columns[columnIndex];
            c.WidthInternal = Math.Max(c.MinimumWidth, PreferredColumnWidth(c, autoSizeColumnMode));
            LayoutChanged();
        }
        public void AutoResizeRows() { }
        public void AutoResizeRows(DataGridViewAutoSizeRowsMode mode) { }
        public void AutoResizeColumnHeadersHeight() { LayoutChanged(); }
        public void AutoResizeRowHeadersWidth(DataGridViewRowHeadersWidthSizeMode mode) { }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            LayoutChanged();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            LayoutChanged();
        }
        public Rectangle GetCellDisplayRectangle(int columnIndex, int rowIndex, bool cutOverflow)
        {
            int x = columnIndex < 0 ? 0 : RowHeaderWidth + ColumnLeft(columnIndex) - _scroll.OffsetX;
            int y = rowIndex < 0 ? 0 : ColumnHeaderHeight + RowTop(rowIndex) - _scroll.OffsetY;
            int w = columnIndex < 0 ? RowHeaderWidth : _columns[columnIndex].WidthInternal;
            int h = rowIndex < 0 ? ColumnHeaderHeight : RowsInternal[rowIndex].Height;
            return new Rectangle(x, y, w, h);
        }
        public Rectangle GetRowDisplayRectangle(int rowIndex, bool cutOverflow)
        {
            return new Rectangle(0, ColumnHeaderHeight + RowTop(rowIndex) - _scroll.OffsetY, ClientSize.Width, RowsInternal[rowIndex].Height);
        }
        public Rectangle GetColumnDisplayRectangle(int columnIndex, bool cutOverflow)
        {
            return new Rectangle(RowHeaderWidth + ColumnLeft(columnIndex) - _scroll.OffsetX, 0, _columns[columnIndex].WidthInternal, ClientSize.Height);
        }
        public int DisplayedRowCount(bool includePartialRow) { return Math.Max(0, (ClientSize.Height - ColumnHeaderHeight) / Math.Max(1, RowTemplate.Height)); }
        public int DisplayedColumnCount(bool includePartialColumns) { return _columns.Count; }
        public HitTestInfo HitTest(int x, int y)
        {
            int row = RowIndexAtY(y);
            int column = ColumnIndexAtX(x);
            DataGridViewHitTestType type;
            if (y < ColumnHeaderHeight && x < RowHeaderWidth) type = DataGridViewHitTestType.TopLeftHeader;
            else if (y < ColumnHeaderHeight) type = column >= 0 ? DataGridViewHitTestType.ColumnHeader : DataGridViewHitTestType.None;
            else if (x < RowHeaderWidth) type = row >= 0 ? DataGridViewHitTestType.RowHeader : DataGridViewHitTestType.None;
            else type = row >= 0 && column >= 0 ? DataGridViewHitTestType.Cell : DataGridViewHitTestType.None;
            return new HitTestInfo(type, type == DataGridViewHitTestType.ColumnHeader ? -1 : row, column);
        }
        public sealed class HitTestInfo
        {
            public static readonly HitTestInfo Nowhere = new HitTestInfo(DataGridViewHitTestType.None, -1, -1);
            public DataGridViewHitTestType Type { get; }
            public int RowIndex { get; }
            public int ColumnIndex { get; }
            public int ColumnX { get { return 0; } }
            public int RowY { get { return 0; } }
            internal HitTestInfo(DataGridViewHitTestType type, int row, int column) { Type = type; RowIndex = row; ColumnIndex = column; }
        }

        // Selection and the current cell
        internal bool IsCellSelected(DataGridViewCell cell)
        {
            if (_selectedCells.Contains(cell)) return true;
            return false;
        }
        internal void SetCellSelected(DataGridViewCell cell, bool selected)
        {
            bool changed = selected ? _selectedCells.Add(cell) : _selectedCells.Remove(cell);
            if (changed) SelectionChangedInternal();
        }
        internal bool IsRowSelected(DataGridViewRow row)
        {
            if (row.Cells.List.Count == 0) return false;
            foreach (DataGridViewCell cell in row.Cells.List)
                if (cell.OwningColumnInternal != null && cell.OwningColumnInternal.Visible && !_selectedCells.Contains(cell)) return false;
            return row.Cells.List.Any(c => _selectedCells.Contains(c));
        }
        internal void SetRowSelected(DataGridViewRow row, bool selected)
        {
            EnsureCells(row);
            bool changed = false;
            foreach (DataGridViewCell cell in row.Cells.List) changed |= selected ? _selectedCells.Add(cell) : _selectedCells.Remove(cell);
            if (changed) SelectionChangedInternal();
        }
        public void ClearSelection()
        {
            if (_selectedCells.Count == 0) return;
            _selectedCells.Clear();
            SelectionChangedInternal();
        }
        public void SelectAll()
        {
            if (!MultiSelect) return;
            foreach (DataGridViewRow row in RowsInternal)
            {
                EnsureCells(row);
                foreach (DataGridViewCell cell in row.Cells.List) _selectedCells.Add(cell);
            }
            SelectionChangedInternal();
        }
        private void SelectionChangedInternal()
        {
            Invalidate();
            if (_selectionChangeSuspended) { _selectionChanged = true; return; }
            OnSelectionChanged(EventArgs.Empty);
        }
        private void SelectRange(DataGridViewCell from, DataGridViewCell to, bool rows)
        {
            int r0 = Math.Min(from.RowIndex, to.RowIndex), r1 = Math.Max(from.RowIndex, to.RowIndex);
            int c0 = Math.Min(from.ColumnIndex, to.ColumnIndex), c1 = Math.Max(from.ColumnIndex, to.ColumnIndex);
            for (int r = r0; r <= r1; r++)
            {
                DataGridViewRow row = RowsInternal[r];
                EnsureCells(row);
                for (int c = rows ? 0 : c0; c <= (rows ? _columns.Count - 1 : c1); c++) _selectedCells.Add(row.Cells.List[c]);
            }
        }
        private bool FullRows { get { return SelectionMode == DataGridViewSelectionMode.FullRowSelect; } }
        // Makes the cell current: validates and ends the edit of the old cell, raises the leave and enter events
        private bool SetCurrentCell(DataGridViewCell cell, bool select, bool raise)
        {
            if (cell == _currentCell)
            {
                if (select && cell != null && !IsCellSelected(cell))
                {
                    _selectionChangeSuspended = true;
                    _selectedCells.Clear();
                    if (FullRows) SelectRange(cell, cell, true); else _selectedCells.Add(cell);
                    _selectionChangeSuspended = false;
                    SelectionChangedInternal();
                }
                return true;
            }
            DataGridViewCell old = _currentCell;
            if (old != null && old.OwningRowInternal?.DataGridView == this)
            {
                if (!EndEdit()) return false;
                if (raise)
                {
                    OnCellLeave(new DataGridViewCellEventArgs(old.ColumnIndex, old.RowIndex));
                    if (cell == null || cell.RowIndex != old.RowIndex) OnRowLeave(new DataGridViewCellEventArgs(old.ColumnIndex, old.RowIndex));
                }
            }
            _currentCell = cell;
            if (select)
            {
                _selectionChangeSuspended = true;
                _selectedCells.Clear();
                if (cell != null)
                {
                    if (FullRows) SelectRange(cell, cell, true);
                    else _selectedCells.Add(cell);
                }
                _selectionChangeSuspended = false;
                _selectionChanged = false;
            }
            if (cell != null)
            {
                EnsureVisible(cell);
                if (raise)
                {
                    if (old == null || old.RowIndex != cell.RowIndex) OnRowEnter(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
                    OnCellEnter(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
                }
            }
            Invalidate();
            if (raise) OnCurrentCellChanged(EventArgs.Empty);
            if (select) OnSelectionChanged(EventArgs.Empty);
            if (cell != null && EditMode == DataGridViewEditMode.EditOnEnter && ContainsFocus) BeginEdit(false);
            return true;
        }
        private void EnsureVisible(DataGridViewCell cell)
        {
            int row = cell.RowIndex;
            if (row < 0) return;
            _scroll.EnsureVisible(new Rectangle(0, RowTop(row), 1, RowsInternal[row].Height), ColumnHeaderHeight);
            int left = ColumnLeft(cell.ColumnIndex);
            int width = ClientSize.Width - RowHeaderWidth - (_scroll.VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0);
            int x = _scroll.OffsetX;
            if (left < x) x = left;
            else if (left + _columns[cell.ColumnIndex].WidthInternal > x + width) x = left + _columns[cell.ColumnIndex].WidthInternal - width;
            _scroll.SetOffset(x, _scroll.OffsetY);
        }

        // Editing
        public bool BeginEdit(bool selectAll)
        {
            DataGridViewCell cell = _currentCell;
            if (cell == null || cell.ReadOnly || _editingControl != null) return false;
            if (cell is DataGridViewCheckBoxCell)
            {
                object value = cell.Value;
                SetCellValue(cell, !(value is bool b && b), true);
                return true;
            }
            DataGridViewCellCancelEventArgs begin = new DataGridViewCellCancelEventArgs(cell.ColumnIndex, cell.RowIndex);
            OnCellBeginEdit(begin);
            if (begin.Cancel) return false;
            Type editType = cell.EditType ?? typeof(DataGridViewTextBoxEditingControl);
            Control control = (Control)Activator.CreateInstance(editType);
            IDataGridViewEditingControl editing = (IDataGridViewEditingControl)control;
            editing.EditingControlDataGridView = this;
            editing.EditingControlRowIndex = cell.RowIndex;
            DataGridViewCellStyle style = InheritedStyle(cell);
            if (control is DataGridViewComboBoxEditingControl combo && cell is DataGridViewComboBoxCell comboCell)
            {
                foreach (object item in comboCell.Items) combo.Items.Add(item);
            }
            editing.EditingControlFormattedValue = FormatCellValue(cell);
            editing.ApplyCellStyleToEditingControl(style);
            editing.EditingControlValueChanged = false;
            _editingControl = control;
            _editingCell = cell;
            _editCanceled = false;
            Controls.Add(control);
            PlaceEditingControl();
            control.KeyDown += EditingControl_KeyDown;
            control.CreateControl();
            OnEditingControlShowing(new DataGridViewEditingControlShowingEventArgs(control, style));
            control.Focus();
            editing.PrepareEditingControlForEdit(selectAll);
            Invalidate();
            return true;
        }
        private void PlaceEditingControl()
        {
            if (_editingControl == null || _editingCell == null) return;
            int row = _editingCell.RowIndex, column = _editingCell.ColumnIndex;
            if (row < 0 || column < 0) { CancelEditInternal(); return; }
            Rectangle r = GetCellDisplayRectangle(column, row, false);
            if (_editingControl is ComboBox) _editingControl.SetBounds(r.X, r.Y, r.Width - 1, r.Height - 1);
            else _editingControl.SetBounds(r.X + 3, r.Y + Math.Max(1, (r.Height - _editingControl.Font.Height) / 2 - 1), r.Width - 5, Math.Min(r.Height - 2, _editingControl.Font.Height + 4));
        }
        public bool EndEdit() { return EndEdit(DataGridViewDataErrorContexts.Commit); }
        public bool EndEdit(DataGridViewDataErrorContexts context)
        {
            if (_editingControl == null) return true;
            DataGridViewCell cell = _editingCell;
            IDataGridViewEditingControl editing = (IDataGridViewEditingControl)_editingControl;
            string text = editing.GetEditingControlFormattedValue(context)?.ToString() ?? string.Empty;
            DataGridViewCellValidatingEventArgs validating = new DataGridViewCellValidatingEventArgs(cell.ColumnIndex, cell.RowIndex, text);
            OnCellValidating(validating);
            if (validating.Cancel) return false;
            if (editing.EditingControlValueChanged)
            {
                try
                {
                    object value = cell.ParseFormattedValue(text, InheritedStyle(cell), null, null);
                    SetCellValue(cell, value, true);
                    cell = _editingCell;
                }
                catch (Exception ex)
                {
                    Exception inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                    DataGridViewDataErrorEventArgs error = new DataGridViewDataErrorEventArgs(inner, cell.ColumnIndex, cell.RowIndex, context | DataGridViewDataErrorContexts.Parsing);
                    OnDataError(true, error);
                    if (error.Cancel) return false;
                }
            }
            RemoveEditingControl();
            OnCellValidated(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
            OnCellEndEdit(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
            return true;
        }
        public bool CancelEdit()
        {
            if (_editingControl == null) return true;
            DataGridViewCell cell = _editingCell;
            _editCanceled = true;
            RemoveEditingControl();
            OnCellEndEdit(new DataGridViewCellEventArgs(cell.ColumnIndex, cell.RowIndex));
            return true;
        }
        private void CancelEditInternal()
        {
            if (_editingControl == null) return;
            RemoveEditingControl();
        }
        public bool CommitEdit(DataGridViewDataErrorContexts context)
        {
            if (_editingControl == null) return true;
            IDataGridViewEditingControl editing = (IDataGridViewEditingControl)_editingControl;
            if (!editing.EditingControlValueChanged) return true;
            try
            {
                SetCellValue(_editingCell, _editingCell.ParseFormattedValue(editing.GetEditingControlFormattedValue(context), null, null, null), true);
                editing.EditingControlValueChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                OnDataError(true, new DataGridViewDataErrorEventArgs(ex, _editingCell.ColumnIndex, _editingCell.RowIndex, context));
                return false;
            }
        }
        public void NotifyCurrentCellDirty(bool dirty)
        {
            if (_editingControl is IDataGridViewEditingControl e) e.EditingControlValueChanged = dirty;
            CurrentCellDirtyStateChanged?.Invoke(this, EventArgs.Empty);
        }
        private void RemoveEditingControl()
        {
            Control control = _editingControl;
            _editingControl = null;
            _editingCell = null;
            if (control == null) return;
            bool hadFocus = control.ContainsFocus;
            control.KeyDown -= EditingControl_KeyDown;
            Controls.Remove(control);
            control.Dispose();
            if (hadFocus) Focus();
            Invalidate();
        }
        private void EditingControl_KeyDown(object sender, KeyEventArgs e)
        {
            Keys key = e.KeyCode;
            if (key == Keys.Escape)
            {
                CancelEdit();
                e.Handled = true;
            }
            else if (key == Keys.Enter)
            {
                if (EndEdit()) MoveCurrent(0, 1);
                e.Handled = true;
            }
            else if (key == Keys.Tab)
            {
                if (EndEdit()) MoveCurrent(e.Shift ? -1 : 1, 0, true);
                e.Handled = true;
            }
            else if ((key == Keys.Up || key == Keys.Down) && !(sender is ComboBox))
            {
                if (EndEdit()) MoveCurrent(0, key == Keys.Up ? -1 : 1);
                e.Handled = true;
            }
        }
        private void MoveCurrent(int dx, int dy, bool wrap = false)
        {
            if (_currentCell == null || RowsInternal.Count == 0) return;
            int row = _currentCell.RowIndex, column = _currentCell.ColumnIndex;
            int r = row + dy, c = column;
            if (dx != 0)
            {
                c = column + dx;
                while (c >= 0 && c < _columns.Count && !_columns[c].Visible) c += dx;
                if (c < 0 || c >= _columns.Count)
                {
                    if (!wrap) return;
                    r += dx;
                    c = dx > 0 ? FirstVisibleColumn(0) : _columns.List.FindLastIndex(x => x.Visible);
                }
            }
            if (r < 0 || r >= RowsInternal.Count || c < 0) return;
            DataGridViewCell target = this[c, r];
            if (MultiSelect && ModifierKeys.HasFlag(Keys.Shift) && !wrap)
            {
                if (_anchorCell == null) _anchorCell = _currentCell;
                SetCurrentCell(target, false, true);
                _selectedCells.Clear();
                SelectRange(_anchorCell, target, FullRows);
                SelectionChangedInternal();
            }
            else
            {
                _anchorCell = target;
                SetCurrentCell(target, true, true);
            }
        }

        // Keyboard
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || key == Keys.Home || key == Keys.End ||
                key == Keys.PageUp || key == Keys.PageDown || key == Keys.Enter || key == Keys.Delete || key == Keys.F2 || key == Keys.Escape) return true;
            if (key == Keys.Tab && !StandardTab) return true;
            return base.IsInputKey(keyData);
        }
        protected virtual bool ProcessDataGridViewKey(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up: MoveCurrent(0, -1); return true;
                case Keys.Down: MoveCurrent(0, 1); return true;
                case Keys.Left: MoveCurrent(-1, 0); return true;
                case Keys.Right: MoveCurrent(1, 0); return true;
                case Keys.Home: if (_currentCell != null) SetCurrentCell(this[FirstVisibleColumn(0), _currentCell.RowIndex], true, true); return true;
                case Keys.End: if (_currentCell != null) SetCurrentCell(this[_columns.List.FindLastIndex(c => c.Visible), _currentCell.RowIndex], true, true); return true;
                case Keys.PageDown: MoveCurrent(0, Math.Max(1, DisplayedRowCount(false) - 1)); return true;
                case Keys.PageUp: MoveCurrent(0, -Math.Max(1, DisplayedRowCount(false) - 1)); return true;
                case Keys.Tab: MoveCurrent(e.Shift ? -1 : 1, 0, true); return true;
                case Keys.Enter: MoveCurrent(0, 1); return true;
                case Keys.F2: BeginEdit(false); return true;
                case Keys.Escape: CancelEdit(); return true;
                case Keys.A:
                    if (e.Control) { SelectAll(); return true; }
                    return false;
                case Keys.C:
                    if (e.Control)
                    {
                        DataObject data = GetClipboardContent();
                        if (data != null) Clipboard.SetDataObject(data);
                        return true;
                    }
                    return false;
                case Keys.Delete:
                    if (AllowUserToDeleteRows && !ReadOnly && SelectedRows.Count > 0)
                    {
                        foreach (DataGridViewRow row in SelectedRows.Cast<DataGridViewRow>().ToArray())
                        {
                            if (row.IsNewRowInternal) continue;
                            DataGridViewRowCancelEventArgs deleting = new DataGridViewRowCancelEventArgs(row);
                            OnUserDeletingRow(deleting);
                            if (deleting.Cancel) continue;
                            RemoveRow(row);
                            OnUserDeletedRow(new DataGridViewRowEventArgs(row));
                        }
                        return true;
                    }
                    return false;
            }
            return false;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || _editingControl != null) return;
            if (ProcessDataGridViewKey(e)) e.Handled = true;
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.Handled || _editingControl != null || char.IsControl(e.KeyChar)) return;
            if (EditMode == DataGridViewEditMode.EditOnKeystroke || EditMode == DataGridViewEditMode.EditOnKeystrokeOrF2 || EditMode == DataGridViewEditMode.EditOnEnter)
            {
                if (BeginEdit(false) && _editingControl is TextBox tb)
                {
                    tb.Text = e.KeyChar.ToString();
                    tb.Select(1, 0);
                    ((IDataGridViewEditingControl)tb).EditingControlValueChanged = true;
                    e.Handled = true;
                }
            }
        }

        // Clipboard
        public virtual DataObject GetClipboardContent()
        {
            if (ClipboardCopyMode == DataGridViewClipboardCopyMode.Disable) return null;
            List<DataGridViewCell> cells = _selectedCells.Where(c => c.RowIndex >= 0 && c.ColumnIndex >= 0 && c.OwningColumnInternal.Visible).ToList();
            if (cells.Count == 0) return null;
            int r0 = cells.Min(c => c.RowIndex), r1 = cells.Max(c => c.RowIndex);
            List<int> columns = cells.Select(c => c.ColumnIndex).Distinct().OrderBy(c => c).ToList();
            StringBuilder sb = new StringBuilder();
            bool header = ClipboardCopyMode == DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText ||
                          (ClipboardCopyMode == DataGridViewClipboardCopyMode.EnableWithAutoHeaderText && FullRows);
            if (header)
            {
                sb.Append(string.Join("\t", columns.Select(c => _columns[c].HeaderText)));
                sb.Append("\r\n");
            }
            for (int r = r0; r <= r1; r++)
            {
                DataGridViewRow row = RowsInternal[r];
                if (!row.Cells.List.Any(c => cells.Contains(c))) continue;
                List<string> values = new List<string>();
                foreach (int c in columns)
                {
                    DataGridViewCell cell = row.Cells[c];
                    values.Add(cells.Contains(cell) ? FormatCellValue(cell) : string.Empty);
                }
                sb.Append(string.Join("\t", values));
                sb.Append("\r\n");
            }
            DataObject data = new DataObject();
            data.SetText(sb.ToString());
            data.SetData(DataFormats.Text, sb.ToString());
            return data;
        }

        // Sorting
        public virtual void Sort(DataGridViewColumn dataGridViewColumn, ListSortDirection direction)
        {
            EndEdit();
            _sortedColumn = dataGridViewColumn;
            _sortOrder = direction == ListSortDirection.Ascending ? SortOrder.Ascending : SortOrder.Descending;
            if (_boundList != null)
            {
                PropertyDescriptor pd = _boundProperties?.Find(dataGridViewColumn.DataPropertyName, true);
                if (_boundList is System.Data.DataView view) view.Sort = dataGridViewColumn.DataPropertyName + (direction == ListSortDirection.Descending ? " DESC" : "");
                else if (_dataSource is BindingSource bs && bs.SupportsSorting && pd != null) bs.ApplySort(pd, direction);
                else if (_boundList is IBindingList bl && bl.SupportsSorting && pd != null) bl.ApplySort(pd, direction);
                RebuildBoundRows();
            }
            else
            {
                int index = dataGridViewColumn.Index;
                List<DataGridViewRow> rows = RowsInternal.Where(r => !r.IsNewRowInternal).ToList();
                rows.Sort((a, b) =>
                {
                    object va = a.Cells[index].Value, vb = b.Cells[index].Value;
                    DataGridViewSortCompareEventArgs compare = new DataGridViewSortCompareEventArgs(dataGridViewColumn, va, vb, a.Index, b.Index);
                    SortCompare?.Invoke(this, compare);
                    int result = compare.Handled ? compare.SortResult :
                                 va is IComparable ca && vb != null && va.GetType() == vb.GetType() ? ca.CompareTo(vb) : string.Compare(va?.ToString(), vb?.ToString(), StringComparison.CurrentCulture);
                    return direction == ListSortDirection.Ascending ? result : -result;
                });
                DataGridViewRow newRow = RowsInternal.FirstOrDefault(r => r.IsNewRowInternal);
                RowsInternal.Clear();
                RowsInternal.AddRange(rows);
                if (newRow != null) RowsInternal.Add(newRow);
                Invalidate();
            }
            foreach (DataGridViewColumn c in _columns.List) c.HeaderCell.SortGlyphDirection = c == dataGridViewColumn ? _sortOrder : SortOrder.None;
            Sorted?.Invoke(this, EventArgs.Empty);
        }
        public virtual void Sort(IComparer comparer)
        {
            List<DataGridViewRow> rows = RowsInternal.Where(r => !r.IsNewRowInternal).ToList();
            rows.Sort((a, b) => comparer.Compare(a, b));
            DataGridViewRow newRow = RowsInternal.FirstOrDefault(r => r.IsNewRowInternal);
            RowsInternal.Clear();
            RowsInternal.AddRange(rows);
            if (newRow != null) RowsInternal.Add(newRow);
            Invalidate();
        }

        // Mouse
        internal override bool CanSelectOnClick { get { return true; } }
        private int ColumnDividerAt(int x, int y)
        {
            if (y >= ColumnHeaderHeight || !AllowUserToResizeColumns) return -1;
            int edge = RowHeaderWidth - _scroll.OffsetX;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (!_columns[i].Visible) continue;
                edge += _columns[i].WidthInternal;
                if (Math.Abs(x - edge) <= 3) return i;
            }
            return -1;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int divider = ColumnDividerAt(e.X, e.Y);
            if (divider >= 0 && e.Button == MouseButtons.Left)
            {
                _resizingColumn = divider;
                _resizeStartX = e.X;
                _resizeStartWidth = _columns[divider].WidthInternal;
                return;
            }
            int row = RowIndexAtY(e.Y);
            int column = ColumnIndexAtX(e.X);
            if (e.Y < ColumnHeaderHeight)
            {
                if (column >= 0)
                {
                    OnColumnHeaderMouseClick(new DataGridViewCellMouseEventArgs(column, -1, e.X, e.Y, e));
                    DataGridViewColumn c = _columns[column];
                    if (e.Button == MouseButtons.Left && c.SortMode == DataGridViewColumnSortMode.Automatic && RowsInternal.Count > 0)
                        Sort(c, _sortedColumn == c && _sortOrder == SortOrder.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending);
                }
                return;
            }
            if (row < 0) return;
            if (column < 0)
            {
                // Row header: selects the row
                OnRowHeaderMouseClick(new DataGridViewCellMouseEventArgs(-1, row, e.X, e.Y, e));
                int first = FirstVisibleColumn(0);
                if (first < 0) return;
                DataGridViewCell target = this[first, row];
                if (MultiSelect && (ModifierKeys & Keys.Shift) != 0 && _anchorCell != null)
                {
                    SetCurrentCell(target, false, true);
                    _selectedCells.Clear();
                    SelectRange(_anchorCell, target, true);
                    SelectionChangedInternal();
                }
                else if (MultiSelect && (ModifierKeys & Keys.Control) != 0)
                {
                    SetCurrentCell(target, false, true);
                    SetRowSelected(RowsInternal[row], !IsRowSelected(RowsInternal[row]));
                    _anchorCell = target;
                }
                else
                {
                    _anchorCell = target;
                    if (SetCurrentCell(target, false, true))
                    {
                        _selectedCells.Clear();
                        SelectRange(target, target, true);
                        SelectionChangedInternal();
                    }
                }
                _dragSelecting = e.Button == MouseButtons.Left;
                return;
            }
            DataGridViewCell cell = this[column, row];
            OnCellMouseDown(new DataGridViewCellMouseEventArgs(column, row, e.X, e.Y, e));
            if (e.Button == MouseButtons.Right && IsCellSelected(cell)) return;
            bool wasCurrent = cell == _currentCell;
            if (MultiSelect && (ModifierKeys & Keys.Shift) != 0 && _anchorCell != null)
            {
                SetCurrentCell(cell, false, true);
                _selectedCells.Clear();
                SelectRange(_anchorCell, cell, FullRows);
                SelectionChangedInternal();
            }
            else if (MultiSelect && (ModifierKeys & Keys.Control) != 0)
            {
                SetCurrentCell(cell, false, true);
                if (FullRows) SetRowSelected(cell.OwningRowInternal, !IsRowSelected(cell.OwningRowInternal));
                else SetCellSelected(cell, !IsCellSelected(cell));
                _anchorCell = cell;
            }
            else
            {
                _anchorCell = cell;
                SetCurrentCell(cell, true, true);
                _dragSelecting = e.Button == MouseButtons.Left && MultiSelect;
            }
            if (e.Button == MouseButtons.Left)
            {
                if (e.Clicks == 2)
                {
                    OnCellDoubleClick(new DataGridViewCellEventArgs(column, row));
                    OnCellMouseDoubleClick(new DataGridViewCellMouseEventArgs(column, row, e.X, e.Y, e));
                    BeginEdit(true);
                }
                else if (cell is DataGridViewCheckBoxCell && !cell.ReadOnly) BeginEdit(false);
                else if (cell is DataGridViewComboBoxCell && !cell.ReadOnly) BeginEdit(false);
                else if (wasCurrent && EditMode != DataGridViewEditMode.EditProgrammatically && EditMode != DataGridViewEditMode.EditOnF2 && EditMode != DataGridViewEditMode.EditOnKeystroke)
                    BeginEdit(false);
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_resizingColumn >= 0)
            {
                DataGridViewColumn c = _columns[_resizingColumn];
                c.WidthInternal = Math.Max(c.MinimumWidth, _resizeStartWidth + e.X - _resizeStartX);
                LayoutChanged();
                return;
            }
            Cursor = ColumnDividerAt(e.X, e.Y) >= 0 ? Cursors.VSplit : null;
            if (_dragSelecting && e.Button == MouseButtons.Left && _anchorCell != null)
            {
                int row = RowIndexAtY(Math.Max(ColumnHeaderHeight, e.Y));
                int column = ColumnIndexAtX(Math.Max(RowHeaderWidth, e.X));
                if (row >= 0 && column >= 0)
                {
                    _selectedCells.Clear();
                    SelectRange(_anchorCell, this[column, row], FullRows || _anchorCell.ColumnIndex < 0);
                    SelectionChangedInternal();
                }
            }
            if (ShowCellToolTips)
            {
                int row = RowIndexAtY(e.Y), column = ColumnIndexAtX(e.X);
                string tip = null;
                if (row >= 0 && column >= 0) tip = this[column, row].ToolTipText;
                else if (row < 0 && e.Y < ColumnHeaderHeight && column >= 0) tip = _columns[column].ToolTipText;
                Avalonia.Controls.ToolTip.SetTip(NativeControl, string.IsNullOrEmpty(tip) ? null : tip);
            }
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (_resizingColumn >= 0)
            {
                DataGridViewColumn c = _columns[_resizingColumn];
                _resizingColumn = -1;
                OnColumnWidthChanged(new DataGridViewColumnEventArgs(c));
                return;
            }
            _dragSelecting = false;
            int row = RowIndexAtY(e.Y), column = ColumnIndexAtX(e.X);
            if (row >= 0 && column >= 0)
            {
                OnCellMouseUp(new DataGridViewCellMouseEventArgs(column, row, e.X, e.Y, e));
                OnCellMouseClick(new DataGridViewCellMouseEventArgs(column, row, e.X, e.Y, e));
                OnCellClick(new DataGridViewCellEventArgs(column, row));
                OnCellContentClick(new DataGridViewCellEventArgs(column, row));
            }
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_scroll.Wheel(e.Delta) && e is HandledMouseEventArgs h) h.Handled = true;
            base.OnMouseWheel(e);
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        // Painting
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SkiaSharp.SKCanvas canvas, float width, float height) { BorderSizes.Paint(canvas, _borderStyle, width, height); }
        internal override bool PaintsItself { get { return true; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Size client = ClientSize;
            int headerHeight = ColumnHeaderHeight;
            int rowHeaderWidth = RowHeaderWidth;
            using (SolidBrush back = new SolidBrush(BackgroundColor)) g.FillRectangle(back, ClientRectangle);
            bool focused = ContainsFocus;
            using (Pen grid = new Pen(GridColor))
            {
                // Rows
                GraphicsState state = g.Save();
                g.SetClip(new Rectangle(0, headerHeight, client.Width, Math.Max(0, client.Height - headerHeight)));
                int y = headerHeight - _scroll.OffsetY;
                for (int r = 0; r < RowsInternal.Count; r++)
                {
                    DataGridViewRow row = RowsInternal[r];
                    if (!row.Visible) continue;
                    int h = row.Height;
                    if (y + h >= headerHeight && y < client.Height)
                    {
                        EnsureCells(row);
                        if (RowPrePaint != null)
                        {
                            DataGridViewRowPrePaintEventArgs pre = new DataGridViewRowPrePaintEventArgs(this, g, ClientRectangle, new Rectangle(0, y, client.Width, h), r,
                                                                                                       DataGridViewElementStates.Visible, row.ErrorText, row.DefaultCellStyle, false, false);
                            OnRowPrePaint(pre);
                            if (pre.Handled) { y += h; continue; }
                        }
                        PaintRow(g, row, r, y, h, rowHeaderWidth, grid, focused);
                    }
                    y += h;
                    if (y > client.Height) break;
                }
                g.Restore(state);
                // Column headers
                if (headerHeight > 0) PaintColumnHeaders(g, headerHeight, rowHeaderWidth, client.Width, grid);
                if (headerHeight > 0 && rowHeaderWidth > 0)
                {
                    using (SolidBrush b = new SolidBrush(EnableHeadersVisualStyles ? Color.White : ColumnHeadersDefaultCellStyle.BackColor)) g.FillRectangle(b, 0, 0, rowHeaderWidth, headerHeight);
                    g.DrawLine(grid, rowHeaderWidth - 1, 0, rowHeaderWidth - 1, headerHeight - 1);
                    g.DrawLine(grid, 0, headerHeight - 1, rowHeaderWidth - 1, headerHeight - 1);
                }
            }
            base.OnPaint(e);
        }
        private void PaintColumnHeaders(Graphics g, int height, int left, int width, Pen grid)
        {
            DataGridViewCellStyle headerStyle = ColumnHeadersDefaultCellStyle;
            Font font = headerStyle.Font ?? Font;
            Color back = EnableHeadersVisualStyles ? Color.White : headerStyle.BackColor;
            using (SolidBrush b = new SolidBrush(back)) g.FillRectangle(b, left, 0, width - left, height);
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(left, 0, width - left, height));
            int x = left - _scroll.OffsetX;
            int currentColumn = _currentCell?.ColumnIndex ?? -1;
            using (Pen separator = new Pen(EnableHeadersVisualStyles ? Color.FromArgb(0xE5, 0xE5, 0xE5) : GridColor))
            {
                for (int i = 0; i < _columns.Count; i++)
                {
                    DataGridViewColumn c = _columns[i];
                    if (!c.Visible) continue;
                    int w = c.WidthInternal;
                    DataGridViewCellStyle style = headerStyle.Clone();
                    if (c.HeaderCell.HasStyle) style.ApplyStyle(c.HeaderCell.Style);
                    if (i == currentColumn && EnableHeadersVisualStyles && SelectionMode != DataGridViewSelectionMode.FullRowSelect)
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(0xD9, 0xEB, 0xF9))) g.FillRectangle(b, x, 0, w, height);
                    else if (!EnableHeadersVisualStyles || c.HeaderCell.HasStyle && !c.HeaderCell.Style.BackColor.IsEmpty)
                        using (SolidBrush b = new SolidBrush(style.BackColor)) g.FillRectangle(b, x, 0, w, height);
                    DrawText(g, c.HeaderText, style.Font ?? font, style.ForeColor.IsEmpty ? SystemColors.WindowText : style.ForeColor,
                             new Rectangle(x + 4, 0, w - 8 - (c.HeaderCell.SortGlyphDirection != SortOrder.None ? 10 : 0), height), style.Alignment, style.WrapMode != DataGridViewTriState.False);
                    if (c.HeaderCell.SortGlyphDirection != SortOrder.None)
                    {
                        int cx = x + w - 10, cy = height / 2;
                        Point[] glyph = c.HeaderCell.SortGlyphDirection == SortOrder.Ascending ?
                            new[] { new Point(cx - 3, cy + 2), new Point(cx + 3, cy + 2), new Point(cx, cy - 2) } :
                            new[] { new Point(cx - 3, cy - 2), new Point(cx + 3, cy - 2), new Point(cx, cy + 2) };
                        using (SolidBrush b = new SolidBrush(Color.FromArgb(0x70, 0x70, 0x70))) g.FillPolygon(b, glyph);
                    }
                    g.DrawLine(separator, x + w - 1, 0, x + w - 1, height - 1);
                    x += w;
                }
                g.DrawLine(EnableHeadersVisualStyles ? separator : grid, left, height - 1, width, height - 1);
            }
            g.Restore(state);
        }
        private void PaintRow(Graphics g, DataGridViewRow row, int index, int y, int h, int rowHeaderWidth, Pen grid, bool focused)
        {
            int x = rowHeaderWidth - _scroll.OffsetX;
            Size client = ClientSize;
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(rowHeaderWidth, Math.Max(y, ColumnHeaderHeight), client.Width - rowHeaderWidth, h));
            for (int c = 0; c < _columns.Count; c++)
            {
                DataGridViewColumn column = _columns[c];
                if (!column.Visible) continue;
                int w = column.WidthInternal;
                if (x + w >= rowHeaderWidth && x < client.Width)
                {
                    DataGridViewCell cell = row.Cells.List[c];
                    DataGridViewCellStyle style = InheritedStyle(cell);
                    bool selected = _selectedCells.Contains(cell) && !(cell == _editingCell);
                    Color back = selected ? style.SelectionBackColor : style.BackColor;
                    if (selected && !focused && false) back = SystemColors.ControlDark;
                    string text = cell == _editingCell ? string.Empty : FormatCellValue(cell);
                    if (CellPainting != null)
                    {
                        DataGridViewCellPaintingEventArgs paint = new DataGridViewCellPaintingEventArgs(g, ClientRectangle, new Rectangle(x, y, w, h), index, c,
                                                                                                        selected ? DataGridViewElementStates.Selected : DataGridViewElementStates.None,
                                                                                                        cell.Value, text, style);
                        CellPainting(this, paint);
                        if (paint.Handled) { x += w; continue; }
                    }
                    using (SolidBrush b = new SolidBrush(back.IsEmpty ? SystemColors.Window : back)) g.FillRectangle(b, x, y, w, h);
                    if (cell is DataGridViewCheckBoxCell)
                    {
                        Rectangle box = new Rectangle(x + (w - 13) / 2, y + (h - 13) / 2, 13, 13);
                        using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, box);
                        using (Pen border = new Pen(Color.FromArgb(0x33, 0x33, 0x33))) g.DrawRectangle(border, box.X, box.Y, 12, 12);
                        if (cell.Value is bool b2 && b2)
                        {
                            using (Pen mark = new Pen(Color.Black, 1.5f))
                            {
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                g.DrawLines(mark, new[] { new PointF(box.X + 3, box.Y + 6), new PointF(box.X + 5, box.Y + 9), new PointF(box.X + 10, box.Y + 3) });
                                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                            }
                        }
                    }
                    else
                    {
                        Color fore = selected ? style.SelectionForeColor : style.ForeColor;
                        Padding p = style.Padding;
                        int right = cell is DataGridViewComboBoxCell cc && cc.DisplayStyle != DataGridViewComboBoxDisplayStyle.Nothing ? 16 : 0;
                        DrawText(g, text, style.Font ?? Font, fore.IsEmpty ? SystemColors.ControlText : fore,
                                 new Rectangle(x + 2 + p.Left, y + p.Top, w - 5 - p.Horizontal - right, h - p.Vertical), style.Alignment, style.WrapMode == DataGridViewTriState.True);
                        if (right > 0)
                        {
                            using (SolidBrush arrow = new SolidBrush(Color.Black))
                            {
                                int cx = x + w - 9, cy = y + h / 2;
                                g.FillPolygon(arrow, new[] { new Point(cx - 3, cy - 1), new Point(cx + 4, cy - 1), new Point(cx, cy + 3) });
                            }
                        }
                    }
                    if (CellBorderStyle != DataGridViewCellBorderStyle.None)
                    {
                        g.DrawLine(grid, x + w - 1, y, x + w - 1, y + h - 1);
                        g.DrawLine(grid, x, y + h - 1, x + w - 1, y + h - 1);
                    }
                    if (cell == _currentCell && focused && !FullRows && _editingControl == null)
                    {
                        using (Pen dots = new Pen(Color.Black) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dot })
                            g.DrawRectangle(dots, x + 1, y + 1, w - 4, h - 4);
                    }
                }
                x += w;
            }
            g.Restore(state);
            // Row header
            if (rowHeaderWidth > 0)
            {
                bool rowSelected = IsRowSelected(row);
                Color back = EnableHeadersVisualStyles ? (rowSelected && SelectionMode != DataGridViewSelectionMode.CellSelect ? Color.FromArgb(0xD9, 0xEB, 0xF9) : Color.White) : RowHeadersDefaultCellStyle.BackColor;
                using (SolidBrush b = new SolidBrush(back)) g.FillRectangle(b, 0, y, rowHeaderWidth, h);
                using (Pen separator = new Pen(EnableHeadersVisualStyles ? Color.FromArgb(0xE5, 0xE5, 0xE5) : GridColor))
                {
                    g.DrawLine(grid, rowHeaderWidth - 1, y, rowHeaderWidth - 1, y + h - 1);
                    g.DrawLine(separator, 0, y + h - 1, rowHeaderWidth - 2, y + h - 1);
                }
                if (_currentCell?.OwningRowInternal == row)
                {
                    using (SolidBrush b = new SolidBrush(Color.Black))
                    {
                        int cx = rowHeaderWidth - 14, cy = y + h / 2;
                        if (_editingControl != null)
                        {
                            using (Pen pen = new Pen(Color.Black)) g.DrawLine(pen, cx - 2, cy + 4, cx + 3, cy - 4);
                        }
                        else g.FillPolygon(b, new[] { new Point(cx - 2, cy - 4), new Point(cx + 2, cy), new Point(cx - 2, cy + 4) });
                    }
                }
                else if (row.IsNewRowInternal)
                {
                    using (Font f = new Font(Font, FontStyle.Bold)) using (SolidBrush b = new SolidBrush(Color.Black)) g.DrawString("*", f, b, rowHeaderWidth - 18, y + (h - f.Height) / 2);
                }
                object headerValue = row.HeaderCell.HeaderValue;
                if (headerValue != null)
                    DrawText(g, headerValue.ToString(), RowHeadersDefaultCellStyle.Font ?? Font, SystemColors.WindowText, new Rectangle(4, y, rowHeaderWidth - 18, h),
                             DataGridViewContentAlignment.MiddleLeft, false);
            }
        }
        private static void DrawText(Graphics g, string text, Font font, Color color, Rectangle r, DataGridViewContentAlignment alignment, bool wrap)
        {
            if (string.IsNullOrEmpty(text) || r.Width <= 0) return;
            StringFormat format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter };
            if (!wrap) format.FormatFlags = StringFormatFlags.NoWrap;
            int a = (int)alignment;
            format.Alignment = (a & (4 | 64 | 1024)) != 0 ? StringAlignment.Far : (a & (2 | 32 | 512)) != 0 ? StringAlignment.Center : StringAlignment.Near;
            format.LineAlignment = (a & (1 | 2 | 4)) != 0 ? StringAlignment.Near : (a & (256 | 512 | 1024)) != 0 ? StringAlignment.Far : StringAlignment.Center;
            using (SolidBrush b = new SolidBrush(color)) g.DrawString(text, font, b, new RectangleF(r.X, r.Y, r.Width, r.Height), format);
        }

        public void BeginInit() { }
        public void EndInit() { LayoutChanged(); }
        public void InvalidateCell(DataGridViewCell dataGridViewCell) { Invalidate(); }
        public void InvalidateCell(int columnIndex, int rowIndex) { Invalidate(); }
        public void InvalidateRow(int rowIndex) { Invalidate(); }
        public void InvalidateColumn(int columnIndex) { Invalidate(); }
        public void UpdateCellValue(int columnIndex, int rowIndex) { Invalidate(); }
        public void RefreshEdit() { }
        protected override void Dispose(bool disposing)
        {
            if (disposing && _dataSource is IBindingList list) list.ListChanged -= BoundListChanged;
            base.Dispose(disposing);
        }
    }
}
