// PrePoMax for macOS - ComboBox, ListBox, CheckedListBox, NumericUpDown and ToolTip.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AC = Avalonia.Controls;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;
using Brushes = Avalonia.Media.Brushes;

namespace System.Windows.Forms
{
    public class ListControlConvertEventArgs : ConvertEventArgs
    {
        public object ListItem { get; }
        public ListControlConvertEventArgs(object value, Type desiredType, object listItem) : base(value, desiredType) { ListItem = listItem; }
    }

    public class ConvertEventArgs : EventArgs
    {
        public object Value { get; set; }
        public Type DesiredType { get; }
        public ConvertEventArgs(object value, Type desiredType) { Value = value; DesiredType = desiredType; }
    }

    public delegate void ListControlConvertEventHandler(object sender, ListControlConvertEventArgs e);

    public abstract class ListControl : Control
    {
        private object _dataSource;
        private string _displayMember = string.Empty;
        private string _valueMember = string.Empty;
        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                _dataSource = value;
                OnDataSourceChanged(EventArgs.Empty);
            }
        }
        public string DisplayMember { get { return _displayMember; } set { _displayMember = value ?? string.Empty; OnDisplayMemberChanged(EventArgs.Empty); } }
        public string ValueMember { get { return _valueMember; } set { _valueMember = value ?? string.Empty; } }
        public bool FormattingEnabled { get; set; }
        public string FormatString { get; set; } = string.Empty;
        public IFormatProvider FormatInfo { get; set; }
        public abstract int SelectedIndex { get; set; }
        public object SelectedValue
        {
            get
            {
                object item = SelectedItemCore;
                return item == null ? null : GetMember(item, _valueMember);
            }
            set
            {
                IList items = ItemsCore;
                for (int i = 0; i < items.Count; i++)
                {
                    if (Equals(GetMember(items[i], _valueMember), value))
                    {
                        SelectedIndex = i;
                        return;
                    }
                }
            }
        }
        internal abstract object SelectedItemCore { get; }
        internal abstract IList ItemsCore { get; }
        public event EventHandler DataSourceChanged;
        public event EventHandler DisplayMemberChanged;
        public event EventHandler ValueMemberChanged;
        public event EventHandler SelectedValueChanged;
        public event ListControlConvertEventHandler Format;
        protected virtual void OnDataSourceChanged(EventArgs e)
        {
            if (_dataSource is IEnumerable enumerable && !(_dataSource is string))
            {
                IList items = ItemsCore;
                items.Clear();
                foreach (object o in enumerable) items.Add(o);
                RefreshItemsCore();
            }
            DataSourceChanged?.Invoke(this, e);
        }
        protected virtual void OnDisplayMemberChanged(EventArgs e) { RefreshItemsCore(); DisplayMemberChanged?.Invoke(this, e); }
        protected virtual void OnSelectedValueChanged(EventArgs e) { SelectedValueChanged?.Invoke(this, e); }
        internal abstract void RefreshItemsCore();
        internal static object GetMember(object item, string member)
        {
            if (item == null || string.IsNullOrEmpty(member)) return item;
            PropertyDescriptor property = TypeDescriptor.GetProperties(item).Find(member, true);
            return property != null ? property.GetValue(item) : item;
        }
        public string GetItemText(object item)
        {
            object value = GetMember(item, _displayMember);
            ListControlConvertEventArgs e = new ListControlConvertEventArgs(value, typeof(string), item);
            Format?.Invoke(this, e);
            return e.Value?.ToString() ?? string.Empty;
        }
    }

    // A text entry of the Avalonia list of a combo or list box
    internal sealed class ListEntry
    {
        public object Item;
        public string Text;
        public override string ToString() { return Text; }
    }

    public class ComboBox : ListControl
    {
        private readonly ObjectCollection _items;
        private AC.ComboBox _combo;
        private AC.TextBox _editor;
        private AC.Grid _editableGrid;
        private ComboBoxStyle _dropDownStyle = ComboBoxStyle.DropDown;
        private int _selectedIndex = -1;
        private bool _updating;
        private int _updateCount;

        public ComboBox()
        {
            _items = new ObjectCollection(this);
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
        }
        protected override Size DefaultSize { get { return new Size(121, 21); } }
        public ObjectCollection Items { get { return _items; } }
        internal override IList ItemsCore { get { return _items; } }
        public ComboBoxStyle DropDownStyle
        {
            get { return _dropDownStyle; }
            set
            {
                _dropDownStyle = value;
                if (Content != null) Host.SetContent(CreateContent());
                RefreshItemsCore();
                DropDownStyleChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public int DropDownWidth { get; set; } = 121;
        public int DropDownHeight { get; set; } = 106;
        public int MaxDropDownItems { get; set; } = 8;
        public int ItemHeight { get; set; } = 15;
        public DrawMode DrawMode { get; set; }
        public FlatStyle FlatStyle { get; set; } = FlatStyle.Standard;
        public bool Sorted { get; set; }
        public bool IntegralHeight { get; set; } = true;
        public int MaxLength { get; set; }
        public AutoCompleteMode AutoCompleteMode { get; set; }
        public AutoCompleteSource AutoCompleteSource { get; set; }
        public AutoCompleteStringCollection AutoCompleteCustomSource { get; set; } = new AutoCompleteStringCollection();
        public bool DroppedDown
        {
            get { return _combo?.IsDropDownOpen ?? false; }
            set { if (_combo != null) _combo.IsDropDownOpen = value; }
        }
        public override int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
                if (_selectedIndex == value) return;
                _selectedIndex = value;
                SyncSelection();
                base.Text = value >= 0 ? GetItemText(_items[value]) : (_dropDownStyle == ComboBoxStyle.DropDownList ? string.Empty : base.Text);
                if (_editor != null && _editor.Text != base.Text) { _updating = true; _editor.Text = base.Text; _updating = false; }
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }
        public object SelectedItem
        {
            get { return _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null; }
            set { SelectedIndex = value == null ? -1 : _items.IndexOf(value); }
        }
        internal override object SelectedItemCore { get { return SelectedItem; } }
        public string SelectedText { get { return _editor?.SelectedText ?? string.Empty; } set { if (_editor != null) _editor.SelectedText = value; } }
        public int SelectionStart { get { return _editor?.SelectionStart ?? 0; } set { if (_editor != null) _editor.SelectionStart = value; } }
        public int SelectionLength
        {
            get { return _editor == null ? 0 : Math.Abs(_editor.SelectionEnd - _editor.SelectionStart); }
            set { if (_editor != null) _editor.SelectionEnd = _editor.SelectionStart + value; }
        }
        public override string Text
        {
            get { return base.Text; }
            set
            {
                value = value ?? string.Empty;
                int index = FindStringExact(value);
                if (index >= 0)
                {
                    SelectedIndex = index;
                    return;
                }
                if (_dropDownStyle == ComboBoxStyle.DropDownList)
                {
                    if (value.Length == 0) SelectedIndex = -1;
                    return;
                }
                base.Text = value;
                if (_editor != null && _editor.Text != value)
                {
                    _updating = true;
                    _editor.Text = value;
                    _updating = false;
                }
            }
        }
        public event EventHandler SelectedIndexChanged;
        public event EventHandler SelectionChangeCommitted;
        public event EventHandler DropDown;
        public event EventHandler DropDownClosed;
        public event EventHandler DropDownStyleChanged;
        public event EventHandler TextUpdate;
        public event DrawItemEventHandler DrawItem;
        public event MeasureItemEventHandler MeasureItem;
        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
            OnSelectedValueChanged(e);
        }
        protected virtual void OnSelectionChangeCommitted(EventArgs e) { SelectionChangeCommitted?.Invoke(this, e); }
        protected virtual void OnDropDown(EventArgs e) { DropDown?.Invoke(this, e); }
        protected virtual void OnDropDownClosed(EventArgs e) { DropDownClosed?.Invoke(this, e); }
        protected virtual void OnDrawItem(DrawItemEventArgs e) { DrawItem?.Invoke(this, e); }
        public void BeginUpdate() { _updateCount++; }
        public void EndUpdate()
        {
            if (_updateCount > 0) _updateCount--;
            if (_updateCount == 0) RefreshItemsCore();
        }
        public int FindString(string s) { return FindString(s, -1); }
        public int FindString(string s, int startIndex)
        {
            if (s == null) return -1;
            for (int i = startIndex + 1; i < _items.Count; i++)
                if (GetItemText(_items[i]).StartsWith(s, StringComparison.CurrentCultureIgnoreCase)) return i;
            return -1;
        }
        public int FindStringExact(string s) { return FindStringExact(s, -1); }
        public int FindStringExact(string s, int startIndex)
        {
            if (s == null) return -1;
            for (int i = startIndex + 1; i < _items.Count; i++)
                if (string.Equals(GetItemText(_items[i]), s, StringComparison.CurrentCultureIgnoreCase)) return i;
            return -1;
        }
        public void Select(int start, int length) { if (_editor != null) { _editor.SelectionStart = start; _editor.SelectionEnd = start + length; } }
        public void SelectAll() { _editor?.SelectAll(); }
        public int GetItemHeight(int index) { return ItemHeight; }
        internal void ItemsChanged(int removedIndex = -2)
        {
            if (removedIndex >= 0 && _selectedIndex >= removedIndex)
            {
                if (_selectedIndex == removedIndex) { _selectedIndex = -1; base.Text = _dropDownStyle == ComboBoxStyle.DropDownList ? string.Empty : base.Text; }
                else _selectedIndex--;
            }
            if (removedIndex == -1) { _selectedIndex = -1; if (_dropDownStyle == ComboBoxStyle.DropDownList) base.Text = string.Empty; }
            if (_updateCount == 0) RefreshItemsCore();
        }
        internal override void RefreshItemsCore()
        {
            if (_combo == null) return;
            _updating = true;
            try
            {
                List<ListEntry> entries = new List<ListEntry>();
                foreach (object item in _items) entries.Add(new ListEntry { Item = item, Text = GetItemText(item) });
                _combo.ItemsSource = entries;
                SyncSelection();
            }
            finally
            {
                _updating = false;
            }
        }
        private void SyncSelection()
        {
            if (_combo == null) return;
            bool updating = _updating;
            _updating = true;
            _combo.SelectedIndex = _selectedIndex;
            _updating = updating;
        }
        protected internal override AC.Control CreateContent()
        {
            _combo = new AC.ComboBox
            {
                MinHeight = 0,
                Padding = new Thickness(3, 0, 2, 0),
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Center,
                MaxDropDownHeight = 400,
                ItemTemplate = new FuncDataTemplate<ListEntry>((entry, ns) =>
                    new AC.TextBlock { Text = entry?.Text, VerticalAlignment = VerticalAlignment.Center }),
            };
            _combo.Classes.Add("winforms");
            _combo.SelectionChanged += (s, e) =>
            {
                if (_updating) return;
                int index = _combo.SelectedIndex;
                Toolkit.Dispatch(() =>
                {
                    if (index != _selectedIndex && index >= 0)
                    {
                        SelectedIndex = index;
                        OnSelectionChangeCommitted(EventArgs.Empty);
                    }
                });
            };
            _combo.DropDownOpened += (s, e) => Toolkit.Dispatch(() => OnDropDown(EventArgs.Empty));
            _combo.DropDownClosed += (s, e) => Toolkit.Dispatch(() => OnDropDownClosed(EventArgs.Empty));
            if (_dropDownStyle == ComboBoxStyle.DropDownList)
            {
                _editor = null;
                return _combo;
            }
            // An editable combo box: a text box over the selection box of the combo box
            _editor = new AC.TextBox { MinHeight = 0, BorderThickness = new Thickness(0), Padding = new Thickness(3, 0, 0, 0), VerticalContentAlignment = VerticalAlignment.Center, Background = Brushes.White };
            _editor.Classes.Add("winforms");
            _editor.Classes.Add("noborder");
            _editor.Text = base.Text;
            _editor.PropertyChanged += (s, e) =>
            {
                if (e.Property != AC.TextBox.TextProperty || _updating) return;
                string text = _editor.Text ?? string.Empty;
                Toolkit.Dispatch(() =>
                {
                    base.Text = text;
                    TextUpdate?.Invoke(this, EventArgs.Empty);
                });
            };
            _editableGrid = new AC.Grid();
            _editableGrid.Children.Add(_combo);
            _editableGrid.Children.Add(_editor);
            _editableGrid.SizeChanged += (s, e) => _editor.Margin = new Thickness(2, 2, 22, 2);
            return _editableGrid;
        }
        protected internal override void OnContentCreated() { RefreshItemsCore(); }
        public override Size GetPreferredSize(Size proposedSize) { return new Size(Width, Font.Height + 8); }

        public class ObjectCollection : IList
        {
            private readonly ComboBox _owner;
            private readonly List<object> _list = new List<object>();
            public ObjectCollection(ComboBox owner) { _owner = owner; }
            public int Count { get { return _list.Count; } }
            public bool IsReadOnly { get { return false; } }
            public virtual object this[int index]
            {
                get { return _list[index]; }
                set { _list[index] = value; _owner.ItemsChanged(); }
            }
            public int Add(object item)
            {
                _list.Add(item);
                if (_owner.Sorted) _list.Sort((a, b) => string.Compare(_owner.GetItemText(a), _owner.GetItemText(b), StringComparison.CurrentCulture));
                _owner.ItemsChanged();
                return _list.IndexOf(item);
            }
            public void AddRange(object[] items)
            {
                _list.AddRange(items);
                _owner.ItemsChanged();
            }
            public void AddRange(IList items)
            {
                foreach (object o in items) _list.Add(o);
                _owner.ItemsChanged();
            }
            public void Clear()
            {
                _list.Clear();
                _owner.ItemsChanged(-1);
            }
            public bool Contains(object value) { return _list.Contains(value); }
            public int IndexOf(object value) { return _list.IndexOf(value); }
            public void Insert(int index, object item)
            {
                _list.Insert(index, item);
                _owner.ItemsChanged();
            }
            public void Remove(object value)
            {
                int index = _list.IndexOf(value);
                if (index >= 0) RemoveAt(index);
            }
            public void RemoveAt(int index)
            {
                _list.RemoveAt(index);
                _owner.ItemsChanged(index);
            }
            public void CopyTo(object[] destination, int arrayIndex) { _list.CopyTo(destination, arrayIndex); }
            public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
            bool IList.IsFixedSize { get { return false; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
        }
    }

    public class ListBox : ListControl
    {
        private readonly ObjectCollection _items;
        private readonly SelectedIndexCollection _selectedIndices;
        private readonly SelectedObjectCollection _selectedItems;
        protected internal AC.ListBox NativeList;
        private SelectionMode _selectionMode = SelectionMode.One;
        private readonly List<int> _selected = new List<int>();
        private bool _updating;
        private int _updateCount;
        private BorderStyle _borderStyle = BorderStyle.Fixed3D;

        public ListBox()
        {
            _items = new ObjectCollection(this);
            _selectedIndices = new SelectedIndexCollection(this);
            _selectedItems = new SelectedObjectCollection(this);
            SetStyle(ControlStyles.Selectable, true);
        }
        protected override Size DefaultSize { get { return new Size(120, 96); } }
        public ObjectCollection Items { get { return _items; } }
        internal override IList ItemsCore { get { return _items; } }
        public SelectionMode SelectionMode { get { return _selectionMode; } set { _selectionMode = value; UpdateNativeMode(); } }
        public SelectedIndexCollection SelectedIndices { get { return _selectedIndices; } }
        public SelectedObjectCollection SelectedItems { get { return _selectedItems; } }
        public BorderStyle BorderStyle { get { return _borderStyle; } set { _borderStyle = value; } }
        public DrawMode DrawMode { get; set; }
        public int ItemHeight { get; set; } = 13;
        public bool IntegralHeight { get; set; } = true;
        public bool MultiColumn { get; set; }
        public bool HorizontalScrollbar { get; set; }
        public bool ScrollAlwaysVisible { get; set; }
        public bool Sorted { get; set; }
        public int ColumnWidth { get; set; }
        public bool UseTabStops { get; set; } = true;
        public int TopIndex { get; set; }
        public override Color BackColor { get { return BackColorSet ? base.BackColor : SystemColors.Window; } set { base.BackColor = value; } }
        public override int SelectedIndex
        {
            get { return _selected.Count > 0 ? _selected[0] : -1; }
            set
            {
                if (value < -1 || value >= _items.Count) throw new ArgumentOutOfRangeException(nameof(value));
                if (_selected.Count == 1 && _selected[0] == value) return;
                if (value == -1 && _selected.Count == 0) return;
                _selected.Clear();
                if (value >= 0) _selected.Add(value);
                SyncSelection();
                OnSelectedIndexChanged(EventArgs.Empty);
            }
        }
        public object SelectedItem
        {
            get { int i = SelectedIndex; return i >= 0 ? _items[i] : null; }
            set { SelectedIndex = value == null ? -1 : _items.IndexOf(value); }
        }
        internal override object SelectedItemCore { get { return SelectedItem; } }
        public event EventHandler SelectedIndexChanged;
        public event DrawItemEventHandler DrawItem;
        public event MeasureItemEventHandler MeasureItem;
        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            SelectedIndexChanged?.Invoke(this, e);
            OnSelectedValueChanged(e);
        }
        public void BeginUpdate() { _updateCount++; }
        public void EndUpdate() { if (_updateCount > 0) _updateCount--; if (_updateCount == 0) RefreshItemsCore(); }
        public void ClearSelected()
        {
            if (_selected.Count == 0) return;
            _selected.Clear();
            SyncSelection();
            OnSelectedIndexChanged(EventArgs.Empty);
        }
        public bool GetSelected(int index) { return _selected.Contains(index); }
        public void SetSelected(int index, bool value)
        {
            if (value == _selected.Contains(index)) return;
            if (value)
            {
                if (_selectionMode == SelectionMode.One) _selected.Clear();
                _selected.Add(index);
                _selected.Sort();
            }
            else _selected.Remove(index);
            SyncSelection();
            OnSelectedIndexChanged(EventArgs.Empty);
        }
        public int FindString(string s) { return FindString(s, -1); }
        public int FindString(string s, int startIndex)
        {
            for (int i = startIndex + 1; i < _items.Count; i++)
                if (GetItemText(_items[i]).StartsWith(s ?? "", StringComparison.CurrentCultureIgnoreCase)) return i;
            return -1;
        }
        public int FindStringExact(string s) { return FindStringExact(s, -1); }
        public int FindStringExact(string s, int startIndex)
        {
            for (int i = startIndex + 1; i < _items.Count; i++)
                if (string.Equals(GetItemText(_items[i]), s, StringComparison.CurrentCultureIgnoreCase)) return i;
            return -1;
        }
        public int IndexFromPoint(Point p) { return IndexFromPoint(p.X, p.Y); }
        public int IndexFromPoint(int x, int y)
        {
            int index = TopIndex + y / Math.Max(1, ItemHeight + 4);
            return index >= 0 && index < _items.Count ? index : -1;
        }
        public Rectangle GetItemRectangle(int index) { return new Rectangle(0, (index - TopIndex) * (ItemHeight + 4), ClientSize.Width, ItemHeight + 4); }
        public int GetItemHeight(int index) { return ItemHeight; }
        internal void ItemsChanged(int removedIndex = -2)
        {
            if (removedIndex == -1) _selected.Clear();
            else if (removedIndex >= 0)
            {
                _selected.Remove(removedIndex);
                for (int i = 0; i < _selected.Count; i++) if (_selected[i] > removedIndex) _selected[i]--;
            }
            if (_updateCount == 0) RefreshItemsCore();
        }
        internal virtual AC.Control CreateItemElement(ListEntry entry, int index)
        {
            return new AC.TextBlock { Text = entry.Text, VerticalAlignment = VerticalAlignment.Center };
        }
        internal override void RefreshItemsCore()
        {
            if (NativeList == null) return;
            _updating = true;
            try
            {
                List<ListEntry> entries = new List<ListEntry>();
                foreach (object item in _items) entries.Add(new ListEntry { Item = item, Text = GetItemText(item) });
                NativeList.ItemsSource = entries;
                SyncSelection();
            }
            finally
            {
                _updating = false;
            }
        }
        private void SyncSelection()
        {
            if (NativeList == null) return;
            bool updating = _updating;
            _updating = true;
            NativeList.SelectedItems?.Clear();
            if (NativeList.ItemsSource is List<ListEntry> entries)
            {
                foreach (int i in _selected)
                {
                    if (i < entries.Count)
                    {
                        if (_selectionMode == SelectionMode.One) NativeList.SelectedIndex = i;
                        else NativeList.SelectedItems?.Add(entries[i]);
                    }
                }
            }
            _updating = updating;
        }
        private void UpdateNativeMode()
        {
            if (NativeList == null) return;
            NativeList.SelectionMode = _selectionMode == SelectionMode.MultiExtended || _selectionMode == SelectionMode.MultiSimple ?
                                       AC.SelectionMode.Multiple : AC.SelectionMode.Single;
        }
        protected internal override AC.Control CreateContent()
        {
            NativeList = new AC.ListBox
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0x82, 0x87, 0x90)),
                BorderThickness = new Thickness(1),
                ItemTemplate = new FuncDataTemplate<ListEntry>((entry, ns) => entry == null ? null :
                    CreateItemElement(entry, _items.IndexOf(entry.Item))),
            };
            NativeList.Classes.Add("winforms");
            NativeList.SelectionChanged += (s, e) =>
            {
                if (_updating) return;
                List<int> indices = new List<int>();
                if (NativeList.ItemsSource is List<ListEntry> entries && NativeList.SelectedItems != null)
                {
                    foreach (object o in NativeList.SelectedItems)
                    {
                        int i = entries.IndexOf(o as ListEntry);
                        if (i >= 0) indices.Add(i);
                    }
                }
                indices.Sort();
                Toolkit.Dispatch(() =>
                {
                    if (indices.SequenceEqual(_selected)) return;
                    _selected.Clear();
                    _selected.AddRange(indices);
                    OnSelectedIndexChanged(EventArgs.Empty);
                });
            };
            UpdateNativeMode();
            return NativeList;
        }
        protected internal override void OnContentCreated() { RefreshItemsCore(); }

        public class ObjectCollection : IList
        {
            private readonly ListBox _owner;
            private readonly List<object> _list = new List<object>();
            public ObjectCollection(ListBox owner) { _owner = owner; }
            public int Count { get { return _list.Count; } }
            public bool IsReadOnly { get { return false; } }
            public virtual object this[int index] { get { return _list[index]; } set { _list[index] = value; _owner.ItemsChanged(); } }
            public int Add(object item) { _list.Add(item); _owner.ItemsChanged(); return _list.Count - 1; }
            public void AddRange(object[] items) { _list.AddRange(items); _owner.ItemsChanged(); }
            public void Clear() { _list.Clear(); _owner.ItemsChanged(-1); }
            public bool Contains(object value) { return _list.Contains(value); }
            public int IndexOf(object value) { return _list.IndexOf(value); }
            public void Insert(int index, object item) { _list.Insert(index, item); _owner.ItemsChanged(); }
            public void Remove(object value) { int i = _list.IndexOf(value); if (i >= 0) RemoveAt(i); }
            public void RemoveAt(int index) { _list.RemoveAt(index); _owner.ItemsChanged(index); }
            public void CopyTo(object[] destination, int arrayIndex) { _list.CopyTo(destination, arrayIndex); }
            public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
            bool IList.IsFixedSize { get { return false; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
        }

        public class SelectedIndexCollection : IList
        {
            private readonly ListBox _owner;
            public SelectedIndexCollection(ListBox owner) { _owner = owner; }
            public int Count { get { return _owner._selected.Count; } }
            public int this[int index] { get { return _owner._selected[index]; } }
            public bool Contains(int selectedIndex) { return _owner._selected.Contains(selectedIndex); }
            public int IndexOf(int selectedIndex) { return _owner._selected.IndexOf(selectedIndex); }
            public void Add(int index) { _owner.SetSelected(index, true); }
            public void Remove(int index) { _owner.SetSelected(index, false); }
            public void Clear() { _owner.ClearSelected(); }
            public IEnumerator GetEnumerator() { return _owner._selected.ToList().GetEnumerator(); }
            int IList.Add(object value) { Add((int)value); return Count - 1; }
            bool IList.Contains(object value) { return value is int i && Contains(i); }
            int IList.IndexOf(object value) { return value is int i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.Remove(object value) { if (value is int i) Remove(i); }
            void IList.RemoveAt(int index) { Remove(this[index]); }
            bool IList.IsFixedSize { get { return false; } }
            bool IList.IsReadOnly { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { for (int i = 0; i < Count; i++) array.SetValue(this[i], index + i); }
        }

        public class SelectedObjectCollection : IList
        {
            private readonly ListBox _owner;
            public SelectedObjectCollection(ListBox owner) { _owner = owner; }
            public int Count { get { return _owner._selected.Count; } }
            public object this[int index] { get { return _owner._items[_owner._selected[index]]; } set { throw new NotSupportedException(); } }
            public bool Contains(object selectedObject) { int i = _owner._items.IndexOf(selectedObject); return i >= 0 && _owner._selected.Contains(i); }
            public int IndexOf(object selectedObject) { int i = _owner._items.IndexOf(selectedObject); return _owner._selected.IndexOf(i); }
            public void Add(object value) { _owner.SetSelected(_owner._items.IndexOf(value), true); }
            public void Remove(object value) { _owner.SetSelected(_owner._items.IndexOf(value), false); }
            public void Clear() { _owner.ClearSelected(); }
            public IEnumerator GetEnumerator() { return _owner._selected.Select(i => _owner._items[i]).ToList().GetEnumerator(); }
            int IList.Add(object value) { Add(value); return Count - 1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.RemoveAt(int index) { Remove(this[index]); }
            bool IList.IsFixedSize { get { return false; } }
            bool IList.IsReadOnly { get { return false; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { for (int i = 0; i < Count; i++) array.SetValue(this[i], index + i); }
        }
    }

    public class CheckedListBox : ListBox
    {
        private readonly Dictionary<object, CheckState> _checks = new Dictionary<object, CheckState>();
        private readonly CheckedIndexCollection _checkedIndices;
        private readonly CheckedItemCollection _checkedItems;
        public CheckedListBox()
        {
            _checkedIndices = new CheckedIndexCollection(this);
            _checkedItems = new CheckedItemCollection(this);
        }
        public bool CheckOnClick { get; set; }
        public bool ThreeDCheckBoxes { get; set; }
        public CheckedIndexCollection CheckedIndices { get { return _checkedIndices; } }
        public CheckedItemCollection CheckedItems { get { return _checkedItems; } }
        public event ItemCheckEventHandler ItemCheck;
        protected virtual void OnItemCheck(ItemCheckEventArgs ice) { ItemCheck?.Invoke(this, ice); }
        public bool GetItemChecked(int index) { return GetItemCheckState(index) != CheckState.Unchecked; }
        public CheckState GetItemCheckState(int index)
        {
            return _checks.TryGetValue(Items[index], out CheckState state) ? state : CheckState.Unchecked;
        }
        public void SetItemChecked(int index, bool value) { SetItemCheckState(index, value ? CheckState.Checked : CheckState.Unchecked); }
        public void SetItemCheckState(int index, CheckState value)
        {
            CheckState current = GetItemCheckState(index);
            if (current == value) return;
            ItemCheckEventArgs e = new ItemCheckEventArgs(index, value, current);
            OnItemCheck(e);
            _checks[Items[index]] = e.NewValue;
            RefreshItemsCore();
        }
        internal override AC.Control CreateItemElement(ListEntry entry, int index)
        {
            AC.CheckBox box = new AC.CheckBox { Content = entry.Text, MinHeight = 0, IsChecked = index >= 0 && GetItemChecked(index), Padding = new Thickness(3, 0, 0, 0) };
            box.Classes.Add("winforms");
            box.IsCheckedChanged += (s, e) =>
            {
                int i = Items.IndexOf(entry.Item);
                if (i >= 0 && (box.IsChecked == true) != GetItemChecked(i)) Toolkit.Dispatch(() => SetItemChecked(i, box.IsChecked == true));
            };
            return box;
        }
        public class CheckedIndexCollection : IEnumerable
        {
            private readonly CheckedListBox _owner;
            internal CheckedIndexCollection(CheckedListBox owner) { _owner = owner; }
            private List<int> List() { List<int> l = new List<int>(); for (int i = 0; i < _owner.Items.Count; i++) if (_owner.GetItemChecked(i)) l.Add(i); return l; }
            public int Count { get { return List().Count; } }
            public int this[int index] { get { return List()[index]; } }
            public bool Contains(int index) { return List().Contains(index); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
        }
        public class CheckedItemCollection : IEnumerable
        {
            private readonly CheckedListBox _owner;
            internal CheckedItemCollection(CheckedListBox owner) { _owner = owner; }
            private List<object> List() { List<object> l = new List<object>(); for (int i = 0; i < _owner.Items.Count; i++) if (_owner.GetItemChecked(i)) l.Add(_owner.Items[i]); return l; }
            public int Count { get { return List().Count; } }
            public object this[int index] { get { return List()[index]; } }
            public bool Contains(object item) { return List().Contains(item); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
        }
    }

    public abstract class UpDownBase : ContainerControl
    {
        public BorderStyle BorderStyle { get; set; } = BorderStyle.Fixed3D;
        public bool ReadOnly { get; set; }
        public HorizontalAlignment TextAlign { get; set; }
        public LeftRightAlignment UpDownAlign { get; set; } = LeftRightAlignment.Right;
        public bool InterceptArrowKeys { get; set; } = true;
        public abstract void DownButton();
        public abstract void UpButton();
        protected bool UserEdit { get; set; }
        protected override Size DefaultSize { get { return new Size(120, 20); } }
    }

    public class NumericUpDown : UpDownBase, ISupportInitialize
    {
        private AC.NumericUpDown _native;
        private decimal _value, _minimum, _maximum = 100, _increment = 1;
        private int _decimalPlaces;
        private bool _thousandsSeparator, _hexadecimal;
        private bool _updating;
        public NumericUpDown() { SetStyle(ControlStyles.Selectable, true); }
        public decimal Value
        {
            get { return _value; }
            set
            {
                if (value < _minimum || value > _maximum) throw new ArgumentOutOfRangeException(nameof(value));
                if (_value == value) return;
                _value = value;
                UpdateNative();
                OnValueChanged(EventArgs.Empty);
            }
        }
        public decimal Minimum { get { return _minimum; } set { _minimum = value; if (_value < value) _value = value; UpdateNative(); } }
        public decimal Maximum { get { return _maximum; } set { _maximum = value; if (_value > value) _value = value; UpdateNative(); } }
        public decimal Increment { get { return _increment; } set { _increment = value; UpdateNative(); } }
        public int DecimalPlaces { get { return _decimalPlaces; } set { _decimalPlaces = value; UpdateNative(); } }
        public bool ThousandsSeparator { get { return _thousandsSeparator; } set { _thousandsSeparator = value; UpdateNative(); } }
        public bool Hexadecimal { get { return _hexadecimal; } set { _hexadecimal = value; UpdateNative(); } }
        public NumericUpDownAccelerationCollection Accelerations { get; } = new NumericUpDownAccelerationCollection();
        public override string Text { get { return _value.ToString("F" + _decimalPlaces); } set { if (decimal.TryParse(value, out decimal d)) Value = d; } }
        public event EventHandler ValueChanged;
        protected virtual void OnValueChanged(EventArgs e) { ValueChanged?.Invoke(this, e); }
        public override void DownButton() { Value = Math.Max(_minimum, _value - _increment); }
        public override void UpButton() { Value = Math.Min(_maximum, _value + _increment); }
        public void BeginInit() { }
        public void EndInit() { }
        public void Select(int start, int length) { }
        private void UpdateNative()
        {
            if (_native == null) return;
            _updating = true;
            _native.Minimum = _minimum;
            _native.Maximum = _maximum;
            _native.Increment = _increment;
            _native.FormatString = "N" + _decimalPlaces;
            if (!_thousandsSeparator) _native.FormatString = "F" + _decimalPlaces;
            _native.Value = _value;
            _native.IsReadOnly = ReadOnly;
            _updating = false;
        }
        protected internal override AC.Control CreateContent()
        {
            _native = new AC.NumericUpDown { MinHeight = 0, MinWidth = 0, Padding = new Thickness(2, 0), ButtonSpinnerLocation = Avalonia.Controls.Location.Right, VerticalContentAlignment = VerticalAlignment.Center };
            _native.Classes.Add("winforms");
            _native.ValueChanged += (s, e) =>
            {
                if (_updating || e.NewValue == null) return;
                decimal v = Math.Max(_minimum, Math.Min(_maximum, e.NewValue.Value));
                Toolkit.Dispatch(() =>
                {
                    if (v != _value)
                    {
                        _value = v;
                        OnValueChanged(EventArgs.Empty);
                    }
                });
            };
            return _native;
        }
        protected internal override void OnContentCreated() { UpdateNative(); }
    }

    public class NumericUpDownAcceleration
    {
        public int Seconds { get; set; }
        public decimal Increment { get; set; }
        public NumericUpDownAcceleration(int seconds, decimal increment) { Seconds = seconds; Increment = increment; }
    }

    public class NumericUpDownAccelerationCollection : List<NumericUpDownAcceleration> { }

    public enum ToolTipIcon { None = 0, Info = 1, Warning = 2, Error = 3 }

    public class ToolTip : Component
    {
        private readonly Dictionary<Control, string> _tips = new Dictionary<Control, string>();
        private bool _active = true;
        public ToolTip() { }
        public ToolTip(IContainer cont) : this() { cont?.Add(this); }
        public bool Active
        {
            get { return _active; }
            set
            {
                _active = value;
                foreach (var entry in _tips) Apply(entry.Key, entry.Value);
            }
        }
        public int AutomaticDelay { get; set; } = 500;
        public int AutoPopDelay { get; set; } = 5000;
        public int InitialDelay { get; set; } = 500;
        public int ReshowDelay { get; set; } = 100;
        public bool ShowAlways { get; set; }
        public bool IsBalloon { get; set; }
        public bool OwnerDraw { get; set; }
        public bool StripAmpersands { get; set; }
        public bool UseAnimation { get; set; } = true;
        public bool UseFading { get; set; } = true;
        public ToolTipIcon ToolTipIcon { get; set; }
        public string ToolTipTitle { get; set; } = string.Empty;
        public Color BackColor { get; set; } = SystemColors.Info;
        public Color ForeColor { get; set; } = SystemColors.InfoText;
        public object Tag { get; set; }
        public event PopupEventHandler Popup;
        public event DrawToolTipEventHandler Draw;
        public void SetToolTip(Control control, string caption)
        {
            if (control == null) return;
            _tips[control] = caption;
            Apply(control, caption);
        }
        public string GetToolTip(Control control) { return control != null && _tips.TryGetValue(control, out string s) ? s : string.Empty; }
        public void RemoveAll()
        {
            foreach (Control c in _tips.Keys.ToArray()) Apply(c, null);
            _tips.Clear();
        }
        private void Apply(Control control, string caption)
        {
            AC.Control native = control.NativeControl;
            if (!_active || string.IsNullOrEmpty(caption)) AC.ToolTip.SetTip(native, null);
            else
            {
                string text = string.IsNullOrEmpty(ToolTipTitle) ? caption : ToolTipTitle + Environment.NewLine + caption;
                AC.ToolTip.SetTip(native, text);
                AC.ToolTip.SetShowDelay(native, InitialDelay);
            }
        }
        // Shows the text at a position of the control for a time
        public void Show(string text, IWin32Window window) { Show(text, window, 0, 0, AutoPopDelay); }
        public void Show(string text, IWin32Window window, int duration) { Show(text, window, 0, 0, duration); }
        public void Show(string text, IWin32Window window, Point point) { Show(text, window, point.X, point.Y, AutoPopDelay); }
        public void Show(string text, IWin32Window window, Point point, int duration) { Show(text, window, point.X, point.Y, duration); }
        public void Show(string text, IWin32Window window, int x, int y) { Show(text, window, x, y, AutoPopDelay); }
        public void Show(string text, IWin32Window window, int x, int y, int duration)
        {
            if (!(window is Control control)) return;
            AC.Control native = control.NativeControl;
            AC.ToolTip.SetTip(native, text);
            AC.ToolTip.SetPlacement(native, AC.PlacementMode.Pointer);
            AC.ToolTip.SetIsOpen(native, true);
            if (duration > 0)
            {
                DispatcherTimer.RunOnce(() =>
                {
                    AC.ToolTip.SetIsOpen(native, false);
                    Apply(control, GetToolTip(control));
                }, TimeSpan.FromMilliseconds(duration));
            }
        }
        public void Hide(IWin32Window win)
        {
            if (win is Control control)
            {
                AC.ToolTip.SetIsOpen(control.NativeControl, false);
                Apply(control, GetToolTip(control));
            }
        }
    }

    public class PopupEventArgs : CancelEventArgs
    {
        public IWin32Window AssociatedWindow { get; }
        public Control AssociatedControl { get; }
        public Size ToolTipSize { get; set; }
        public bool IsBalloon { get; }
        public PopupEventArgs(IWin32Window associatedWindow, Control associatedControl, bool isBalloon, Size size)
        {
            AssociatedWindow = associatedWindow;
            AssociatedControl = associatedControl;
            IsBalloon = isBalloon;
            ToolTipSize = size;
        }
    }

    public delegate void PopupEventHandler(object sender, PopupEventArgs e);

    public class DrawToolTipEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public Rectangle Bounds { get; }
        public string ToolTipText { get; }
        public Font Font { get; }
        public DrawToolTipEventArgs(Graphics graphics, IWin32Window associatedWindow, Control associatedControl, Rectangle bounds,
                                    string toolTipText, Color backColor, Color foreColor, Font font)
        {
            Graphics = graphics;
            Bounds = bounds;
            ToolTipText = toolTipText;
            Font = font;
        }
        public void DrawBackground() { }
        public void DrawBorder() { }
        public void DrawText() { }
    }

    public delegate void DrawToolTipEventHandler(object sender, DrawToolTipEventArgs e);
}
