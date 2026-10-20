// PrePoMax for macOS - ListView, ListViewItem and ColumnHeader, drawn like the WinForms list view (details and list views).

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace System.Windows.Forms
{
    [Flags]
    public enum ListViewHitTestLocations { None = 1, Image = 2, Label = 4, BelowClientArea = 16, RightOfClientArea = 32, LeftOfClientArea = 64, AboveClientArea = 256, StateImage = 512 }

    [Flags]
    public enum ListViewItemStates { Selected = 1, Grayed = 2, Checked = 8, Focused = 16, Default = 32, Hot = 64, Marked = 128, Indeterminate = 256, ShowKeyboardCues = 512 }

    [Serializable]
    public class ListViewItem : ICloneable
    {
        private string _text = string.Empty;
        private readonly ListViewSubItemCollection _subItems;
        internal ListView ListViewInternal;
        private bool _selected;
        private bool _checked;
        private Color _backColor = Color.Empty;
        private Color _foreColor = Color.Empty;
        private Font _font;
        private ListViewGroup _group;

        public ListViewItem()
        {
            _subItems = new ListViewSubItemCollection(this);
            _subItems.AddInternal(new ListViewSubItem(this, string.Empty));
        }
        public ListViewItem(string text) : this() { Text = text; }
        public ListViewItem(string text, int imageIndex) : this(text) { ImageIndex = imageIndex; }
        public ListViewItem(string text, string imageKey) : this(text) { ImageKey = imageKey; }
        public ListViewItem(string[] items) : this()
        {
            if (items == null || items.Length == 0) return;
            Text = items[0];
            for (int i = 1; i < items.Length; i++) _subItems.Add(items[i]);
        }
        public ListViewItem(string[] items, int imageIndex) : this(items) { ImageIndex = imageIndex; }
        public ListViewItem(string[] items, string imageKey) : this(items) { ImageKey = imageKey; }
        public ListViewItem(ListViewGroup group) : this() { Group = group; }
        public ListViewItem(string text, ListViewGroup group) : this(text) { Group = group; }
        public ListViewItem(string[] items, ListViewGroup group) : this(items) { Group = group; }
        public ListViewItem(ListViewSubItem[] subItems, int imageIndex) : this()
        {
            _subItems.Clear();
            foreach (ListViewSubItem s in subItems) _subItems.Add(s);
            ImageIndex = imageIndex;
        }

        public string Text
        {
            get { return _subItems.Count > 0 ? _subItems[0].Text : _text; }
            set
            {
                _text = value ?? string.Empty;
                if (_subItems.Count > 0) _subItems[0].Text = _text;
                Changed();
            }
        }
        public string Name { get { return _subItems.Count > 0 ? _subItems[0].Name : string.Empty; } set { if (_subItems.Count > 0) _subItems[0].Name = value; } }
        public object Tag { get; set; }
        public string ToolTipText { get; set; }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; } = string.Empty;
        public int StateImageIndex { get; set; } = -1;
        public int IndentCount { get; set; }
        public bool UseItemStyleForSubItems { get; set; } = true;
        public ListViewSubItemCollection SubItems { get { return _subItems; } }
        public ListView ListView { get { return ListViewInternal; } }
        public int Index { get { return ListViewInternal?.ItemList.IndexOf(this) ?? -1; } }
        public ListViewGroup Group
        {
            get { return _group; }
            set
            {
                _group?.Items.RemoveInternal(this);
                _group = value;
                _group?.Items.AddInternal(this);
                Changed();
            }
        }
        public Color BackColor
        {
            get { return !_backColor.IsEmpty ? _backColor : ListViewInternal?.BackColor ?? SystemColors.Window; }
            set { _backColor = value; Changed(); }
        }
        public Color ForeColor
        {
            get { return !_foreColor.IsEmpty ? _foreColor : ListViewInternal?.ForeColor ?? SystemColors.WindowText; }
            set { _foreColor = value; Changed(); }
        }
        internal bool BackColorSet { get { return !_backColor.IsEmpty; } }
        public Font Font { get { return _font ?? ListViewInternal?.Font ?? Control.DefaultFont; } set { _font = value; Changed(); } }
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value) return;
                if (value && ListViewInternal != null && !ListViewInternal.MultiSelect)
                {
                    foreach (ListViewItem other in ListViewInternal.ItemList)
                        if (other != this && other._selected) other.SetSelected(false, true);
                }
                SetSelected(value, true);
            }
        }
        internal void SetSelected(bool value, bool raise)
        {
            if (_selected == value) return;
            _selected = value;
            Changed();
            if (raise) ListViewInternal?.ItemSelectionChangedInternal(this, value);
        }
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                ListView lv = ListViewInternal;
                if (lv != null)
                {
                    ItemCheckEventArgs e = new ItemCheckEventArgs(Index, value ? CheckState.Checked : CheckState.Unchecked,
                                                                  _checked ? CheckState.Checked : CheckState.Unchecked);
                    lv.RaiseItemCheck(e);
                    value = e.NewValue != CheckState.Unchecked;
                    if (value == _checked) return;
                }
                _checked = value;
                Changed();
                lv?.RaiseItemChecked(this);
            }
        }
        public bool Focused { get; set; }
        public Rectangle Bounds { get { return ListViewInternal?.GetItemRect(Index) ?? Rectangle.Empty; } }
        public Point Position { get { return Bounds.Location; } set { } }
        public void EnsureVisible() { ListViewInternal?.EnsureVisible(Index); }
        public void BeginEdit() { }
        public void Remove() { ListViewInternal?.Items.Remove(this); }
        public Rectangle GetBounds(ItemBoundsPortion portion) { return Bounds; }
        public ListViewSubItem GetSubItemAt(int x, int y) { return ListViewInternal?.HitTest(x, y).SubItem; }
        internal void Changed() { ListViewInternal?.ItemChanged(); }
        public virtual object Clone()
        {
            ListViewItem clone = new ListViewItem();
            clone._subItems.Clear();
            foreach (ListViewSubItem s in _subItems) clone._subItems.Add(new ListViewSubItem(clone, s.Text, s.ForeColorInternal, s.BackColorInternal, s.FontInternal) { Name = s.Name, Tag = s.Tag });
            clone.Tag = Tag;
            clone.ImageIndex = ImageIndex;
            clone.ImageKey = ImageKey;
            clone._backColor = _backColor;
            clone._foreColor = _foreColor;
            clone._font = _font;
            clone._checked = _checked;
            clone.UseItemStyleForSubItems = UseItemStyleForSubItems;
            clone.ToolTipText = ToolTipText;
            return clone;
        }
        public override string ToString() { return "ListViewItem: {" + Text + "}"; }

        [Serializable]
        public class ListViewSubItem
        {
            internal ListViewItem Owner;
            private string _text = string.Empty;
            internal Color ForeColorInternal = Color.Empty;
            internal Color BackColorInternal = Color.Empty;
            internal Font FontInternal;
            public ListViewSubItem() { }
            public ListViewSubItem(ListViewItem owner, string text) { Owner = owner; _text = text ?? string.Empty; }
            public ListViewSubItem(ListViewItem owner, string text, Color foreColor, Color backColor, Font font) : this(owner, text)
            {
                ForeColorInternal = foreColor;
                BackColorInternal = backColor;
                FontInternal = font;
            }
            public string Text { get { return _text; } set { _text = value ?? string.Empty; Owner?.Changed(); } }
            public string Name { get; set; } = string.Empty;
            public object Tag { get; set; }
            public Color ForeColor { get { return !ForeColorInternal.IsEmpty ? ForeColorInternal : Owner?.ForeColor ?? SystemColors.WindowText; } set { ForeColorInternal = value; Owner?.Changed(); } }
            public Color BackColor { get { return !BackColorInternal.IsEmpty ? BackColorInternal : Owner?.BackColor ?? SystemColors.Window; } set { BackColorInternal = value; Owner?.Changed(); } }
            public Font Font { get { return FontInternal ?? Owner?.Font ?? Control.DefaultFont; } set { FontInternal = value; Owner?.Changed(); } }
            public Rectangle Bounds { get { return Rectangle.Empty; } }
            public void ResetStyle() { ForeColorInternal = Color.Empty; BackColorInternal = Color.Empty; FontInternal = null; Owner?.Changed(); }
            public override string ToString() { return "ListViewSubItem: {" + _text + "}"; }
        }

        public class ListViewSubItemCollection : IList
        {
            private readonly ListViewItem _owner;
            private readonly List<ListViewSubItem> _list = new List<ListViewSubItem>();
            public ListViewSubItemCollection(ListViewItem owner) { _owner = owner; }
            public int Count { get { return _list.Count; } }
            public bool IsReadOnly { get { return false; } }
            public ListViewSubItem this[int index] { get { return _list[index]; } set { value.Owner = _owner; _list[index] = value; _owner.Changed(); } }
            public virtual ListViewSubItem this[string key] { get { return _list.FirstOrDefault(s => string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase)); } }
            internal void AddInternal(ListViewSubItem item) { item.Owner = _owner; _list.Add(item); }
            public ListViewSubItem Add(ListViewSubItem item) { item.Owner = _owner; _list.Add(item); _owner.Changed(); return item; }
            public ListViewSubItem Add(string text) { return Add(new ListViewSubItem(_owner, text)); }
            public ListViewSubItem Add(string text, Color foreColor, Color backColor, Font font) { return Add(new ListViewSubItem(_owner, text, foreColor, backColor, font)); }
            public void AddRange(ListViewSubItem[] items) { foreach (var i in items) Add(i); }
            public void AddRange(string[] items) { foreach (var i in items) Add(i); }
            public void Clear() { _list.Clear(); _owner.Changed(); }
            public bool Contains(ListViewSubItem subItem) { return _list.Contains(subItem); }
            public virtual bool ContainsKey(string key) { return this[key] != null; }
            public int IndexOf(ListViewSubItem subItem) { return _list.IndexOf(subItem); }
            public virtual int IndexOfKey(string key) { return _list.FindIndex(s => string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase)); }
            public void Insert(int index, ListViewSubItem item) { item.Owner = _owner; _list.Insert(index, item); _owner.Changed(); }
            public void Remove(ListViewSubItem item) { _list.Remove(item); _owner.Changed(); }
            public void RemoveAt(int index) { _list.RemoveAt(index); _owner.Changed(); }
            public virtual void RemoveByKey(string key) { int i = IndexOfKey(key); if (i >= 0) RemoveAt(i); }
            public IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
            int IList.Add(object item) { Add((ListViewSubItem)item); return _list.Count - 1; }
            bool IList.Contains(object item) { return item is ListViewSubItem s && Contains(s); }
            int IList.IndexOf(object item) { return item is ListViewSubItem s ? IndexOf(s) : -1; }
            void IList.Insert(int index, object item) { Insert(index, (ListViewSubItem)item); }
            void IList.Remove(object item) { if (item is ListViewSubItem s) Remove(s); }
            bool IList.IsFixedSize { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { this[index] = (ListViewSubItem)value; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
        }
    }

    public enum ItemBoundsPortion { Entire = 0, Icon = 1, Label = 2, ItemOnly = 3 }

    [Serializable]
    public sealed class ListViewGroup
    {
        public string Header { get; set; } = string.Empty;
        public string Name { get; set; }
        public object Tag { get; set; }
        public HorizontalAlignment HeaderAlignment { get; set; }
        public ListViewGroupItemCollection Items { get; }
        public ListView ListView { get; internal set; }
        public ListViewGroup() { Items = new ListViewGroupItemCollection(); }
        public ListViewGroup(string header) : this() { Header = header; }
        public ListViewGroup(string key, string headerText) : this(headerText) { Name = key; }
        public ListViewGroup(string header, HorizontalAlignment headerAlignment) : this(header) { HeaderAlignment = headerAlignment; }
        public override string ToString() { return Header; }
        public class ListViewGroupItemCollection : IEnumerable
        {
            private readonly List<ListViewItem> _list = new List<ListViewItem>();
            public int Count { get { return _list.Count; } }
            public ListViewItem this[int index] { get { return _list[index]; } }
            internal void AddInternal(ListViewItem item) { if (!_list.Contains(item)) _list.Add(item); }
            internal void RemoveInternal(ListViewItem item) { _list.Remove(item); }
            public void Add(ListViewItem item) { item.Group = null; item.Group = null; _list.Add(item); }
            public void Remove(ListViewItem item) { _list.Remove(item); }
            public void Clear() { _list.Clear(); }
            public bool Contains(ListViewItem item) { return _list.Contains(item); }
            public IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
        }
    }

    public class ListViewGroupCollection : IList
    {
        private readonly ListView _owner;
        private readonly List<ListViewGroup> _list = new List<ListViewGroup>();
        internal ListViewGroupCollection(ListView owner) { _owner = owner; }
        public int Count { get { return _list.Count; } }
        public ListViewGroup this[int index] { get { return _list[index]; } set { _list[index] = value; } }
        public ListViewGroup this[string key] { get { return _list.FirstOrDefault(g => g.Name == key); } }
        public int Add(ListViewGroup group) { group.ListView = _owner; _list.Add(group); _owner.ItemChanged(); return _list.Count - 1; }
        public ListViewGroup Add(string key, string headerText) { ListViewGroup g = new ListViewGroup(key, headerText); Add(g); return g; }
        public void AddRange(ListViewGroup[] groups) { foreach (var g in groups) Add(g); }
        public void Clear() { _list.Clear(); _owner.ItemChanged(); }
        public bool Contains(ListViewGroup value) { return _list.Contains(value); }
        public int IndexOf(ListViewGroup value) { return _list.IndexOf(value); }
        public void Insert(int index, ListViewGroup group) { group.ListView = _owner; _list.Insert(index, group); _owner.ItemChanged(); }
        public void Remove(ListViewGroup group) { _list.Remove(group); _owner.ItemChanged(); }
        public void RemoveAt(int index) { _list.RemoveAt(index); _owner.ItemChanged(); }
        public IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
        int IList.Add(object value) { return Add((ListViewGroup)value); }
        bool IList.Contains(object value) { return value is ListViewGroup g && Contains(g); }
        int IList.IndexOf(object value) { return value is ListViewGroup g ? IndexOf(g) : -1; }
        void IList.Insert(int index, object value) { Insert(index, (ListViewGroup)value); }
        void IList.Remove(object value) { if (value is ListViewGroup g) Remove(g); }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { this[index] = (ListViewGroup)value; } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { ((ICollection)_list).CopyTo(array, index); }
    }

    [DesignTimeVisible(false)]
    public class ColumnHeader : Component, ICloneable
    {
        private int _width = 60;
        private string _text = string.Empty;
        internal ListView ListViewInternal;
        public ColumnHeader() { }
        public ColumnHeader(int imageIndex) { ImageIndex = imageIndex; }
        public ColumnHeader(string imageKey) { ImageKey = imageKey; }
        public string Text { get { return _text; } set { _text = value ?? string.Empty; ListViewInternal?.ItemChanged(); } }
        public string Name { get; set; } = string.Empty;
        public object Tag { get; set; }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; } = string.Empty;
        public int DisplayIndex { get; set; }
        public HorizontalAlignment TextAlign { get; set; }
        public ListView ListView { get { return ListViewInternal; } }
        public int Index { get { return ListViewInternal?.Columns.IndexOf(this) ?? -1; } }
        public int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                // -1: width of the content; -2: the rest of the list view
                if (ListViewInternal != null && value < 0) _width = ListViewInternal.AutoWidth(this, value == -2 ? ColumnHeaderAutoResizeStyle.HeaderSize : ColumnHeaderAutoResizeStyle.ColumnContent, value == -2);
                ListViewInternal?.ItemChanged();
            }
        }
        internal int WidthInternal { set { _width = value; } }
        public void AutoResize(ColumnHeaderAutoResizeStyle headerAutoResize) { ListViewInternal?.AutoResizeColumn(Index, headerAutoResize); }
        public object Clone() { return new ColumnHeader { _text = _text, _width = _width, Name = Name, TextAlign = TextAlign, Tag = Tag }; }
        public override string ToString() { return "ColumnHeader: Text: " + _text; }
    }

    public class ListViewHitTestInfo
    {
        public ListViewItem Item { get; }
        public ListViewItem.ListViewSubItem SubItem { get; }
        public ListViewHitTestLocations Location { get; }
        public ListViewHitTestInfo(ListViewItem hitItem, ListViewItem.ListViewSubItem hitSubItem, ListViewHitTestLocations hitLocation)
        {
            Item = hitItem;
            SubItem = hitSubItem;
            Location = hitLocation;
        }
    }

    public class ListViewItemSelectionChangedEventArgs : EventArgs
    {
        public ListViewItem Item { get; }
        public int ItemIndex { get; }
        public bool IsSelected { get; }
        public ListViewItemSelectionChangedEventArgs(ListViewItem item, int itemIndex, bool isSelected) { Item = item; ItemIndex = itemIndex; IsSelected = isSelected; }
    }

    public delegate void ListViewItemSelectionChangedEventHandler(object sender, ListViewItemSelectionChangedEventArgs e);

    public class ItemCheckedEventArgs : EventArgs
    {
        public ListViewItem Item { get; }
        public ItemCheckedEventArgs(ListViewItem item) { Item = item; }
    }

    public delegate void ItemCheckedEventHandler(object sender, ItemCheckedEventArgs e);

    public class ColumnClickEventArgs : EventArgs
    {
        public int Column { get; }
        public ColumnClickEventArgs(int column) { Column = column; }
    }

    public delegate void ColumnClickEventHandler(object sender, ColumnClickEventArgs e);

    public class ColumnWidthChangedEventArgs : EventArgs
    {
        public int ColumnIndex { get; }
        public ColumnWidthChangedEventArgs(int columnIndex) { ColumnIndex = columnIndex; }
    }

    public delegate void ColumnWidthChangedEventHandler(object sender, ColumnWidthChangedEventArgs e);

    public class LabelEditEventArgs : EventArgs
    {
        public int Item { get; }
        public string Label { get; }
        public bool CancelEdit { get; set; }
        public LabelEditEventArgs(int item) { Item = item; }
        public LabelEditEventArgs(int item, string label) { Item = item; Label = label; }
    }

    public delegate void LabelEditEventHandler(object sender, LabelEditEventArgs e);

    public class ListView : Control
    {
        private readonly List<ListViewItem> _items = new List<ListViewItem>();
        private readonly List<ColumnHeader> _columns = new List<ColumnHeader>();
        private readonly ListViewItemCollection _itemCollection;
        private readonly ColumnHeaderCollection _columnCollection;
        private readonly SelectedListViewItemCollection _selectedItems;
        private readonly SelectedIndexCollection _selectedIndices;
        private readonly CheckedListViewItemCollection _checkedItems;
        private readonly ListViewGroupCollection _groups;
        private readonly ScrollHelper _scroll;
        private View _view = View.LargeIcon;
        private ColumnHeaderStyle _headerStyle = ColumnHeaderStyle.Clickable;
        private BorderStyle _borderStyle = BorderStyle.Fixed3D;
        private int _updateCount;
        private bool _selectionChangedPending;
        private int _focusedIndex = -1;
        private int _anchorIndex = -1;
        private int _resizingColumn = -1;
        private int _resizeStartX, _resizeStartWidth;
        private bool _layoutValid;

        public ListView()
        {
            _itemCollection = new ListViewItemCollection(this);
            _columnCollection = new ColumnHeaderCollection(this);
            _selectedItems = new SelectedListViewItemCollection(this);
            _selectedIndices = new SelectedIndexCollection(this);
            _checkedItems = new CheckedListViewItemCollection(this);
            _groups = new ListViewGroupCollection(this);
            _scroll = new ScrollHelper(this, () => ClientRectangle);
            SetStyle(ControlStyles.Selectable, true);
            SetClientSizeOffset(BorderSizes.Of(_borderStyle));
        }
        protected override Size DefaultSize { get { return new Size(121, 97); } }
        internal List<ListViewItem> ItemList { get { return _items; } }
        public ListViewItemCollection Items { get { return _itemCollection; } }
        public ColumnHeaderCollection Columns { get { return _columnCollection; } }
        public SelectedListViewItemCollection SelectedItems { get { return _selectedItems; } }
        public SelectedIndexCollection SelectedIndices { get { return _selectedIndices; } }
        public CheckedListViewItemCollection CheckedItems { get { return _checkedItems; } }
        public CheckedIndexCollection CheckedIndices { get { return new CheckedIndexCollection(this); } }
        public ListViewGroupCollection Groups { get { return _groups; } }
        public override Color BackColor { get { return BackColorSet ? base.BackColor : SystemColors.Window; } set { base.BackColor = value; } }
        public override Color ForeColor { get { return ForeColorSet ? base.ForeColor : SystemColors.WindowText; } set { base.ForeColor = value; } }
        public View View { get { return _view; } set { _view = value; ItemChanged(); } }
        public ColumnHeaderStyle HeaderStyle { get { return _headerStyle; } set { _headerStyle = value; ItemChanged(); } }
        public BorderStyle BorderStyle { get { return _borderStyle; } set { _borderStyle = value; SetClientSizeOffset(BorderSizes.Of(value)); ItemChanged(); } }
        public bool FullRowSelect { get; set; }
        public bool GridLines { get; set; }
        public bool MultiSelect { get; set; } = true;
        public bool HideSelection { get; set; } = true;
        public bool CheckBoxes { get; set; }
        public bool ShowGroups { get; set; } = true;
        public bool ShowItemToolTips { get; set; }
        public bool LabelEdit { get; set; }
        public bool LabelWrap { get; set; } = true;
        public bool HotTracking { get; set; }
        public bool HoverSelection { get; set; }
        public bool AllowColumnReorder { get; set; }
        public bool Scrollable { get; set; } = true;
        public bool OwnerDraw { get; set; }
        public bool VirtualMode { get; set; }
        public int VirtualListSize { get; set; }
        public bool UseCompatibleStateImageBehavior { get; set; } = true;
        public bool RightToLeftLayout { get; set; }
        public ItemActivation Activation { get; set; }
        public ListViewAlignment Alignment { get; set; }
        public bool AutoArrange { get; set; } = true;
        public SortOrder Sorting { get; set; }
        public IComparer ListViewItemSorter { get; set; }
        public Size TileSize { get; set; }
        public ImageList SmallImageList { get; set; }
        public ImageList LargeImageList { get; set; }
        public ImageList StateImageList { get; set; }
        public ListViewItem FocusedItem
        {
            get { return _focusedIndex >= 0 && _focusedIndex < _items.Count ? _items[_focusedIndex] : null; }
            set { _focusedIndex = value == null ? -1 : _items.IndexOf(value); Invalidate(); }
        }
        public ListViewItem TopItem
        {
            get
            {
                int index = _scroll.OffsetY / Math.Max(1, RowHeight);
                return index >= 0 && index < _items.Count ? _items[index] : null;
            }
            set { if (value != null) _scroll.SetOffset(_scroll.OffsetX, _items.IndexOf(value) * RowHeight); }
        }

        public event EventHandler SelectedIndexChanged;
        public event ListViewItemSelectionChangedEventHandler ItemSelectionChanged;
        public event ItemCheckEventHandler ItemCheck;
        public event ItemCheckedEventHandler ItemChecked;
        public event ColumnClickEventHandler ColumnClick;
        public event ColumnWidthChangedEventHandler ColumnWidthChanged;
        public event EventHandler ItemActivate;
        public event ItemDragEventHandler ItemDrag;
        public event LabelEditEventHandler AfterLabelEdit;
        public event LabelEditEventHandler BeforeLabelEdit;
        public event DrawListViewItemEventHandler DrawItem;
        public event DrawListViewSubItemEventHandler DrawSubItem;
        public event DrawListViewColumnHeaderEventHandler DrawColumnHeader;
        protected virtual void OnSelectedIndexChanged(EventArgs e) { SelectedIndexChanged?.Invoke(this, e); }
        protected virtual void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e) { ItemSelectionChanged?.Invoke(this, e); }
        protected virtual void OnItemCheck(ItemCheckEventArgs ice) { ItemCheck?.Invoke(this, ice); }
        protected virtual void OnItemChecked(ItemCheckedEventArgs e) { ItemChecked?.Invoke(this, e); }
        protected virtual void OnColumnClick(ColumnClickEventArgs e) { ColumnClick?.Invoke(this, e); }
        protected virtual void OnColumnWidthChanged(ColumnWidthChangedEventArgs e) { ColumnWidthChanged?.Invoke(this, e); }
        protected virtual void OnItemActivate(EventArgs e) { ItemActivate?.Invoke(this, e); }
        protected virtual void OnItemDrag(ItemDragEventArgs e) { ItemDrag?.Invoke(this, e); }
        protected virtual void OnAfterLabelEdit(LabelEditEventArgs e) { AfterLabelEdit?.Invoke(this, e); }
        protected virtual void OnBeforeLabelEdit(LabelEditEventArgs e) { BeforeLabelEdit?.Invoke(this, e); }
        internal void RaiseItemCheck(ItemCheckEventArgs e) { OnItemCheck(e); }
        internal void RaiseItemChecked(ListViewItem item) { OnItemChecked(new ItemCheckedEventArgs(item)); }
        internal void ItemSelectionChangedInternal(ListViewItem item, bool selected)
        {
            OnItemSelectionChanged(new ListViewItemSelectionChangedEventArgs(item, item.Index, selected));
            if (_updateCount > 0) { _selectionChangedPending = true; return; }
            OnSelectedIndexChanged(EventArgs.Empty);
        }

        // Methods
        public void BeginUpdate() { _updateCount++; }
        public void EndUpdate()
        {
            if (_updateCount > 0) _updateCount--;
            if (_updateCount == 0)
            {
                ItemChanged();
                if (_selectionChangedPending)
                {
                    _selectionChangedPending = false;
                    OnSelectedIndexChanged(EventArgs.Empty);
                }
            }
        }
        public void Clear() { Items.Clear(); Columns.Clear(); }
        public void Sort()
        {
            if (ListViewItemSorter != null) _items.Sort((a, b) => ListViewItemSorter.Compare(a, b));
            else if (Sorting != SortOrder.None)
            {
                _items.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));
                if (Sorting == SortOrder.Descending) _items.Reverse();
            }
            ItemChanged();
        }
        public void ArrangeIcons() { }
        public void ArrangeIcons(ListViewAlignment value) { }
        public void EnsureVisible(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            EnsureLayout();
            _scroll.EnsureVisible(new Rectangle(0, index * RowHeight, 1, RowHeight), HeaderHeight);
        }
        public ListViewItem GetItemAt(int x, int y)
        {
            ListViewHitTestInfo hit = HitTest(x, y);
            return hit.Item;
        }
        public ListViewHitTestInfo HitTest(Point point) { return HitTest(point.X, point.Y); }
        public ListViewHitTestInfo HitTest(int x, int y)
        {
            Size client = ClientSize;
            if (y < 0) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.AboveClientArea);
            if (x < 0) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.LeftOfClientArea);
            if (x >= client.Width) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.RightOfClientArea);
            if (y >= client.Height) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.BelowClientArea);
            if (y < HeaderHeight) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
            int index = (y - HeaderHeight + _scroll.OffsetY) / RowHeight;
            if (index < 0 || index >= _items.Count) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
            ListViewItem item = _items[index];
            int cx = x + _scroll.OffsetX;
            int left = 0;
            ListViewItem.ListViewSubItem subItem = null;
            int column = -1;
            for (int i = 0; i < _columns.Count; i++)
            {
                if (cx >= left && cx < left + _columns[i].Width) { column = i; break; }
                left += _columns[i].Width;
            }
            if (column >= 0 && column < item.SubItems.Count) subItem = item.SubItems[column];
            ListViewHitTestLocations location = ListViewHitTestLocations.Label;
            if (_view == View.Details && !FullRowSelect && column > 0) location = ListViewHitTestLocations.None;
            if (_view == View.Details && column < 0) location = ListViewHitTestLocations.None;
            if (_view == View.Details && column == 0 && !FullRowSelect)
            {
                int textRight = ItemTextLeft(item) + ContentLayout.MeasureText(item.Text, item.Font).Width + 6 - _scroll.OffsetX;
                if (x > Math.Min(textRight, _columns[0].Width - _scroll.OffsetX)) location = ListViewHitTestLocations.None;
            }
            if (_view == View.List && x > ItemTextLeft(item) + ContentLayout.MeasureText(item.Text, item.Font).Width + 6) location = ListViewHitTestLocations.None;
            if (location == ListViewHitTestLocations.None && !FullRowSelect) return new ListViewHitTestInfo(null, null, ListViewHitTestLocations.None);
            return new ListViewHitTestInfo(item, subItem, location);
        }
        public ListViewItem FindItemWithText(string text) { return FindItemWithText(text, true, 0, true); }
        public ListViewItem FindItemWithText(string text, bool includeSubItemsInSearch, int startIndex) { return FindItemWithText(text, includeSubItemsInSearch, startIndex, true); }
        public ListViewItem FindItemWithText(string text, bool includeSubItemsInSearch, int startIndex, bool isPrefixSearch)
        {
            for (int i = Math.Max(0, startIndex); i < _items.Count; i++)
            {
                ListViewItem item = _items[i];
                for (int s = 0; s < (includeSubItemsInSearch ? item.SubItems.Count : 1); s++)
                {
                    string t = item.SubItems[s].Text;
                    if (isPrefixSearch ? t.StartsWith(text, StringComparison.CurrentCultureIgnoreCase) : string.Equals(t, text, StringComparison.CurrentCultureIgnoreCase)) return item;
                }
            }
            return null;
        }
        public ListViewItem FindNearestItem(SearchDirectionHint dir, Point point) { return GetItemAt(point.X, point.Y); }
        public Rectangle GetItemRect(int index)
        {
            if (index < 0 || index >= _items.Count) return Rectangle.Empty;
            int y = HeaderHeight + index * RowHeight - _scroll.OffsetY;
            int width = _view == View.Details ? Math.Max(_columns.Sum(c => c.Width), 1) : ClientSize.Width;
            return new Rectangle(-_scroll.OffsetX, y, width, RowHeight);
        }
        public Rectangle GetItemRect(int index, ItemBoundsPortion portion) { return GetItemRect(index); }
        public void AutoResizeColumns(ColumnHeaderAutoResizeStyle headerAutoResize)
        {
            for (int i = 0; i < _columns.Count; i++) AutoResizeColumn(i, headerAutoResize);
        }
        public void AutoResizeColumn(int columnIndex, ColumnHeaderAutoResizeStyle headerAutoResize)
        {
            if (columnIndex < 0 || columnIndex >= _columns.Count || headerAutoResize == ColumnHeaderAutoResizeStyle.None) return;
            _columns[columnIndex].WidthInternal = AutoWidth(_columns[columnIndex], headerAutoResize, false);
            ItemChanged();
            OnColumnWidthChanged(new ColumnWidthChangedEventArgs(columnIndex));
        }
        internal int AutoWidth(ColumnHeader column, ColumnHeaderAutoResizeStyle style, bool fill)
        {
            int index = _columns.IndexOf(column);
            if (fill && index == _columns.Count - 1)
            {
                int others = 0;
                for (int i = 0; i < index; i++) others += _columns[i].Width;
                int rest = ClientSize.Width - others - (_scroll.VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0);
                return Math.Max(rest, ContentLayout.MeasureText(column.Text, Font).Width + 12);
            }
            int width = style == ColumnHeaderAutoResizeStyle.HeaderSize || _headerStyle != ColumnHeaderStyle.None ? ContentLayout.MeasureText(column.Text, Font).Width + 12 : 0;
            if (style == ColumnHeaderAutoResizeStyle.ColumnContent || style == ColumnHeaderAutoResizeStyle.HeaderSize)
            {
                foreach (ListViewItem item in _items)
                {
                    if (index >= item.SubItems.Count) continue;
                    int w = ContentLayout.MeasureText(item.SubItems[index].Text, item.Font).Width + 12;
                    if (index == 0) w += ItemTextLeft(item);
                    width = Math.Max(width, w);
                }
            }
            return Math.Max(width, 8);
        }
        internal void ItemChanged()
        {
            _layoutValid = false;
            if (_updateCount > 0) return;
            EnsureLayout();
            Invalidate();
        }
        private void EnsureLayout()
        {
            if (_layoutValid) return;
            _layoutValid = true;
            int width = _view == View.Details ? _columns.Sum(c => c.Width) : ClientSize.Width;
            _scroll.SmallChangeY = RowHeight;
            if (Scrollable) _scroll.SetExtent(width, _items.Count * RowHeight, HeaderHeight);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _layoutValid = false;
            EnsureLayout();
        }
        internal int RowHeight { get { return Math.Max(Font.Height + 4, SmallImageList != null ? SmallImageList.ImageSize.Height + 1 : 0); } }
        internal int HeaderHeight { get { return _view == View.Details && _headerStyle != ColumnHeaderStyle.None ? Font.Height + 8 : 0; } }
        private int ItemTextLeft(ListViewItem item)
        {
            int x = 4;
            if (CheckBoxes) x += 16;
            if (SmallImageList != null && (item.ImageIndex >= 0 || !string.IsNullOrEmpty(item.ImageKey))) x += SmallImageList.ImageSize.Width + 2;
            return x;
        }

        // Painting
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SkiaSharp.SKCanvas canvas, float width, float height)
        {
            if (_borderStyle == BorderStyle.Fixed3D)
            {
                using (SkiaSharp.SKPaint paint = new SkiaSharp.SKPaint { Color = new SkiaSharp.SKColor(0x82, 0x87, 0x90), Style = SkiaSharp.SKPaintStyle.Stroke, StrokeWidth = 1 })
                    canvas.DrawRect(new SkiaSharp.SKRect(0.5f, 0.5f, width - 0.5f, height - 0.5f), paint);
            }
            else BorderSizes.Paint(canvas, _borderStyle, width, height);
        }
        internal override bool PaintsItself { get { return true; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            EnsureLayout();
            Graphics g = e.Graphics;
            Size client = ClientSize;
            using (SolidBrush back = new SolidBrush(BackColor)) g.FillRectangle(back, ClientRectangle);
            int h = RowHeight;
            int top = HeaderHeight;
            int first = Math.Max(0, _scroll.OffsetY / h);
            int last = Math.Min(_items.Count - 1, (_scroll.OffsetY + client.Height) / h + 1);
            GraphicsState state = g.Save();
            g.SetClip(new Rectangle(0, top, client.Width, Math.Max(0, client.Height - top)));
            for (int i = first; i <= last; i++) DrawItemRow(g, _items[i], i, top + i * h - _scroll.OffsetY, h);
            if (GridLines && _view == View.Details)
            {
                using (Pen grid = new Pen(Color.FromArgb(0xF0, 0xF0, 0xF0)))
                {
                    int x = -_scroll.OffsetX;
                    foreach (ColumnHeader c in _columns) { x += c.Width; g.DrawLine(grid, x - 1, top, x - 1, client.Height); }
                    for (int i = first; i <= last + 1; i++) g.DrawLine(grid, 0, top + i * h - _scroll.OffsetY - 1, client.Width, top + i * h - _scroll.OffsetY - 1);
                }
            }
            g.Restore(state);
            if (top > 0) DrawHeader(g, client.Width, top);
            base.OnPaint(e);
        }
        private void DrawHeader(Graphics g, int width, int height)
        {
            using (SolidBrush back = new SolidBrush(Color.White)) g.FillRectangle(back, 0, 0, width, height);
            using (Pen separator = new Pen(Color.FromArgb(0xE5, 0xE5, 0xE5)))
            using (SolidBrush text = new SolidBrush(Color.FromArgb(0x4C, 0x60, 0x7A)))
            {
                int x = -_scroll.OffsetX;
                foreach (ColumnHeader c in _columns)
                {
                    StringFormat format = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter,
                        Alignment = c.TextAlign == HorizontalAlignment.Right ? StringAlignment.Far : c.TextAlign == HorizontalAlignment.Center ? StringAlignment.Center : StringAlignment.Near,
                    };
                    if (OwnerDraw && DrawColumnHeader != null)
                    {
                        DrawListViewColumnHeaderEventArgs args = new DrawListViewColumnHeaderEventArgs(g, new Rectangle(x, 0, c.Width, height), c.Index, c, ListViewItemStates.Default, ForeColor, BackColor, Font);
                        DrawColumnHeader(this, args);
                        if (!args.DrawDefault) { x += c.Width; continue; }
                    }
                    g.DrawString(c.Text, Font, text, new RectangleF(x + 4, 0, c.Width - 8, height), format);
                    g.DrawLine(separator, x + c.Width - 1, 0, x + c.Width - 1, height);
                    x += c.Width;
                }
                g.DrawLine(separator, 0, height - 1, width, height - 1);
            }
        }
        private void DrawItemRow(Graphics g, ListViewItem item, int index, int y, int h)
        {
            bool focused = ContainsFocus;
            bool showSelection = item.Selected && (focused || !HideSelection);
            Color selectionBack = focused ? SystemColors.Highlight : SystemColors.Control;
            Color selectionFore = focused ? SystemColors.HighlightText : SystemColors.ControlText;
            int ox = -_scroll.OffsetX;
            Size client = ClientSize;
            if (OwnerDraw && DrawItem != null)
            {
                DrawListViewItemEventArgs args = new DrawListViewItemEventArgs(g, item, new Rectangle(ox, y, client.Width, h), index, item.Selected ? ListViewItemStates.Selected : 0);
                DrawItem(this, args);
                if (!args.DrawDefault) return;
            }
            int textLeft = ItemTextLeft(item);
            if (_view != View.Details || _columns.Count == 0)
            {
                // List: the label of the item
                Rectangle label = new Rectangle(textLeft - 2, y, ContentLayout.MeasureText(item.Text, item.Font).Width + 6, h);
                if (item.BackColorSet) using (SolidBrush b = new SolidBrush(item.BackColor)) g.FillRectangle(b, label);
                if (showSelection) using (SolidBrush b = new SolidBrush(selectionBack)) g.FillRectangle(b, label);
                DrawItemIcons(g, item, ox, y, h);
                using (SolidBrush b = new SolidBrush(showSelection ? selectionFore : Enabled ? item.ForeColor : SystemColors.GrayText))
                    g.DrawString(item.Text, item.Font, b, new RectangleF(label.X + 2, y, label.Width, h), new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap });
                return;
            }
            int x = ox;
            for (int c = 0; c < _columns.Count; c++)
            {
                ColumnHeader column = _columns[c];
                ListViewItem.ListViewSubItem sub = c < item.SubItems.Count ? item.SubItems[c] : null;
                Rectangle cell = new Rectangle(x, y, column.Width, h);
                Color back = item.UseItemStyleForSubItems || sub == null ? (item.BackColorSet ? item.BackColor : Color.Empty) :
                             (!sub.BackColorInternal.IsEmpty ? sub.BackColorInternal : item.BackColorSet ? item.BackColor : Color.Empty);
                Color fore = item.UseItemStyleForSubItems || sub == null ? item.ForeColor : sub.ForeColor;
                Font font = item.UseItemStyleForSubItems || sub == null ? item.Font : sub.Font;
                Rectangle fill = cell;
                if (c == 0 && !FullRowSelect) fill = new Rectangle(x + textLeft - 2, y, Math.Min(column.Width - textLeft + 2, ContentLayout.MeasureText(sub?.Text, font).Width + 6), h);
                if (!back.IsEmpty) using (SolidBrush b = new SolidBrush(back)) g.FillRectangle(b, fill);
                bool cellSelected = showSelection && (FullRowSelect || c == 0);
                if (cellSelected) using (SolidBrush b = new SolidBrush(selectionBack)) g.FillRectangle(b, fill);
                if (c == 0) DrawItemIcons(g, item, x, y, h);
                if (sub != null)
                {
                    StringFormat format = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        FormatFlags = StringFormatFlags.NoWrap,
                        Trimming = StringTrimming.EllipsisCharacter,
                        Alignment = column.TextAlign == HorizontalAlignment.Right ? StringAlignment.Far : column.TextAlign == HorizontalAlignment.Center ? StringAlignment.Center : StringAlignment.Near,
                    };
                    int left = c == 0 ? textLeft : 4;
                    using (SolidBrush b = new SolidBrush(cellSelected ? selectionFore : Enabled ? fore : SystemColors.GrayText))
                        g.DrawString(sub.Text, font, b, new RectangleF(x + left - 2, y, column.Width - left - 2, h), format);
                }
                x += column.Width;
            }
        }
        private void DrawItemIcons(Graphics g, ListViewItem item, int x, int y, int h)
        {
            int left = x + 4;
            if (CheckBoxes)
            {
                Rectangle check = new Rectangle(left, y + (h - 13) / 2, 13, 13);
                using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, check);
                using (Pen border = new Pen(Color.FromArgb(0x33, 0x33, 0x33))) g.DrawRectangle(border, check.X, check.Y, 12, 12);
                if (item.Checked)
                {
                    using (Pen mark = new Pen(Color.Black, 1.5f))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawLines(mark, new[] { new PointF(check.X + 3, check.Y + 6), new PointF(check.X + 5, check.Y + 9), new PointF(check.X + 10, check.Y + 3) });
                        g.SmoothingMode = SmoothingMode.None;
                    }
                }
                left += 16;
            }
            if (SmallImageList != null)
            {
                Image image = null;
                if (!string.IsNullOrEmpty(item.ImageKey) && SmallImageList.Images.ContainsKey(item.ImageKey)) image = SmallImageList.Images[item.ImageKey];
                else if (item.ImageIndex >= 0 && item.ImageIndex < SmallImageList.Images.Count) image = SmallImageList.Images[item.ImageIndex];
                if (image != null)
                {
                    Size size = SmallImageList.ImageSize;
                    g.DrawImage(image, new Rectangle(left, y + (h - size.Height) / 2, size.Width, size.Height));
                }
            }
        }

        // Mouse and keyboard
        internal override bool CanSelectOnClick { get { return true; } }
        private int ColumnDividerAt(int x)
        {
            int edge = -_scroll.OffsetX;
            for (int i = 0; i < _columns.Count; i++)
            {
                edge += _columns[i].Width;
                if (Math.Abs(x - edge) <= 3) return i;
            }
            return -1;
        }
        private Point _mouseDownPoint;
        private ListViewItem _mouseDownItem;
        private bool _dragStarted;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Y < HeaderHeight)
            {
                int divider = ColumnDividerAt(e.X);
                if (divider >= 0 && e.Button == MouseButtons.Left)
                {
                    _resizingColumn = divider;
                    _resizeStartX = e.X;
                    _resizeStartWidth = _columns[divider].Width;
                }
                base.OnMouseDown(e);
                return;
            }
            ListViewItem item = GetItemAt(e.X, e.Y);
            _mouseDownItem = item;
            _mouseDownPoint = e.Location;
            _dragStarted = false;
            if (item != null)
            {
                int index = item.Index;
                if (CheckBoxes && e.X >= 4 - _scroll.OffsetX && e.X < 18 - _scroll.OffsetX && e.Button == MouseButtons.Left) item.Checked = !item.Checked;
                else if (e.Button == MouseButtons.Left || (e.Button == MouseButtons.Right && !item.Selected)) SelectByMouse(index, Control.ModifierKeys);
                _focusedIndex = index;
                if (e.Clicks == 2 && e.Button == MouseButtons.Left) OnItemActivate(EventArgs.Empty);
            }
            else if (e.Button == MouseButtons.Left && (Control.ModifierKeys & (Keys.Control | Keys.Shift)) == 0) ClearSelection();
            base.OnMouseDown(e);
        }
        private void SelectByMouse(int index, Keys modifiers)
        {
            BeginUpdate();
            if (MultiSelect && (modifiers & Keys.Shift) != 0 && _anchorIndex >= 0)
            {
                int a = Math.Min(_anchorIndex, index), b = Math.Max(_anchorIndex, index);
                for (int i = 0; i < _items.Count; i++) if (_items[i].Selected != (i >= a && i <= b)) _items[i].SetSelected(i >= a && i <= b, true);
            }
            else if (MultiSelect && (modifiers & Keys.Control) != 0)
            {
                _items[index].SetSelected(!_items[index].Selected, true);
                _anchorIndex = index;
            }
            else
            {
                for (int i = 0; i < _items.Count; i++) if (i != index && _items[i].Selected) _items[i].SetSelected(false, true);
                if (!_items[index].Selected) _items[index].SetSelected(true, true);
                _anchorIndex = index;
            }
            EndUpdate();
        }
        private void ClearSelection()
        {
            BeginUpdate();
            foreach (ListViewItem item in _items) if (item.Selected) item.SetSelected(false, true);
            EndUpdate();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_resizingColumn >= 0)
            {
                _columns[_resizingColumn].WidthInternal = Math.Max(8, _resizeStartWidth + e.X - _resizeStartX);
                ItemChanged();
            }
            else if (e.Y < HeaderHeight && ColumnDividerAt(e.X) >= 0) Cursor = Cursors.VSplit;
            else if (Cursor == Cursors.VSplit) Cursor = null;
            if (e.Button == MouseButtons.Left && _mouseDownItem != null && !_dragStarted &&
                (Math.Abs(e.X - _mouseDownPoint.X) > SystemInformation.DragSize.Width || Math.Abs(e.Y - _mouseDownPoint.Y) > SystemInformation.DragSize.Height))
            {
                _dragStarted = true;
                OnItemDrag(new ItemDragEventArgs(e.Button, _mouseDownItem));
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_resizingColumn >= 0)
            {
                int column = _resizingColumn;
                _resizingColumn = -1;
                OnColumnWidthChanged(new ColumnWidthChangedEventArgs(column));
            }
            else if (e.Y < HeaderHeight && _headerStyle == ColumnHeaderStyle.Clickable && e.Button == MouseButtons.Left)
            {
                int x = -_scroll.OffsetX;
                for (int i = 0; i < _columns.Count; i++)
                {
                    if (e.X >= x && e.X < x + _columns[i].Width) { OnColumnClick(new ColumnClickEventArgs(i)); break; }
                    x += _columns[i].Width;
                }
            }
            _mouseDownItem = null;
            base.OnMouseUp(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_scroll.Wheel(e.Delta) && e is HandledMouseEventArgs h) h.Handled = true;
            base.OnMouseWheel(e);
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down || key == Keys.Home || key == Keys.End || key == Keys.PageUp || key == Keys.PageDown) return true;
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || _items.Count == 0) return;
            int index = _focusedIndex >= 0 ? _focusedIndex : (SelectedIndices.Count > 0 ? SelectedIndices[0] : -1);
            int next = index;
            switch (e.KeyCode)
            {
                case Keys.Down: next = Math.Min(_items.Count - 1, index + 1); break;
                case Keys.Up: next = Math.Max(0, index - 1); break;
                case Keys.Home: next = 0; break;
                case Keys.End: next = _items.Count - 1; break;
                case Keys.PageDown: next = Math.Min(_items.Count - 1, index + Math.Max(1, ClientSize.Height / RowHeight - 1)); break;
                case Keys.PageUp: next = Math.Max(0, index - Math.Max(1, ClientSize.Height / RowHeight - 1)); break;
                case Keys.A:
                    if (e.Control && MultiSelect)
                    {
                        BeginUpdate();
                        foreach (ListViewItem item in _items) if (!item.Selected) item.SetSelected(true, true);
                        EndUpdate();
                        e.Handled = true;
                    }
                    return;
                case Keys.Space:
                    if (CheckBoxes && index >= 0) _items[index].Checked = !_items[index].Checked;
                    return;
                case Keys.Enter:
                    if (index >= 0) OnItemActivate(EventArgs.Empty);
                    return;
                default: return;
            }
            if (next < 0) next = 0;
            SelectByMouse(next, e.Shift ? Keys.Shift : Keys.None);
            _focusedIndex = next;
            EnsureVisible(next);
            e.Handled = true;
        }

        // Collections
        internal void InsertItem(int index, ListViewItem item)
        {
            if (item.ListViewInternal != null) throw new ArgumentException("Cannot add or insert the item '" + item.Text + "' in more than one place.");
            item.ListViewInternal = this;
            _items.Insert(Math.Max(0, Math.Min(index, _items.Count)), item);
            if (Sorting != SortOrder.None || ListViewItemSorter != null) Sort();
            ItemChanged();
            if (item.Selected) ItemSelectionChangedInternal(item, true);
        }
        internal void RemoveItem(ListViewItem item)
        {
            int index = _items.IndexOf(item);
            if (index < 0) return;
            bool selected = item.Selected;
            _items.RemoveAt(index);
            item.ListViewInternal = null;
            if (_focusedIndex >= _items.Count) _focusedIndex = _items.Count - 1;
            if (_anchorIndex >= _items.Count) _anchorIndex = -1;
            ItemChanged();
            if (selected) OnSelectedIndexChanged(EventArgs.Empty);
        }
        internal void ClearItems()
        {
            bool selected = _items.Any(i => i.Selected);
            foreach (ListViewItem item in _items) item.ListViewInternal = null;
            _items.Clear();
            _focusedIndex = -1;
            _anchorIndex = -1;
            ItemChanged();
            if (selected) OnSelectedIndexChanged(EventArgs.Empty);
        }

        public class ListViewItemCollection : IList
        {
            private readonly ListView _owner;
            public ListViewItemCollection(ListView owner) { _owner = owner; }
            public int Count { get { return _owner._items.Count; } }
            public bool IsReadOnly { get { return false; } }
            public virtual ListViewItem this[int index]
            {
                get { return _owner._items[index]; }
                set
                {
                    _owner.RemoveItem(_owner._items[index]);
                    _owner.InsertItem(index, value);
                }
            }
            public virtual ListViewItem this[string key] { get { int i = IndexOfKey(key); return i < 0 ? null : _owner._items[i]; } }
            public virtual ListViewItem Add(string text) { return Add(new ListViewItem(text)); }
            public virtual ListViewItem Add(string text, int imageIndex) { return Add(new ListViewItem(text, imageIndex)); }
            public virtual ListViewItem Add(string text, string imageKey) { return Add(new ListViewItem(text, imageKey)); }
            public virtual ListViewItem Add(string key, string text, int imageIndex) { ListViewItem i = new ListViewItem(text, imageIndex) { Name = key }; return Add(i); }
            public virtual ListViewItem Add(string key, string text, string imageKey) { ListViewItem i = new ListViewItem(text, imageKey) { Name = key }; return Add(i); }
            public virtual ListViewItem Add(ListViewItem value)
            {
                _owner.InsertItem(_owner._items.Count, value);
                return value;
            }
            public void AddRange(ListViewItem[] items)
            {
                _owner.BeginUpdate();
                foreach (ListViewItem item in items) Add(item);
                _owner.EndUpdate();
            }
            public void AddRange(ListViewItemCollection items) { AddRange(items.Cast<ListViewItem>().ToArray()); }
            public ListViewItem Insert(int index, ListViewItem item) { _owner.InsertItem(index, item); return item; }
            public ListViewItem Insert(int index, string text) { return Insert(index, new ListViewItem(text)); }
            public ListViewItem Insert(int index, string text, int imageIndex) { return Insert(index, new ListViewItem(text, imageIndex)); }
            public ListViewItem Insert(int index, string text, string imageKey) { return Insert(index, new ListViewItem(text, imageKey)); }
            public virtual void Remove(ListViewItem item) { _owner.RemoveItem(item); }
            public virtual void RemoveAt(int index) { _owner.RemoveItem(_owner._items[index]); }
            public virtual void RemoveByKey(string key) { int i = IndexOfKey(key); if (i >= 0) RemoveAt(i); }
            public virtual void Clear() { _owner.ClearItems(); }
            public bool Contains(ListViewItem item) { return _owner._items.Contains(item); }
            public virtual bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
            public int IndexOf(ListViewItem item) { return _owner._items.IndexOf(item); }
            public virtual int IndexOfKey(string key)
            {
                if (string.IsNullOrEmpty(key)) return -1;
                return _owner._items.FindIndex(i => string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
            }
            public ListViewItem[] Find(string key, bool searchAllSubItems)
            {
                return _owner._items.Where(i => string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            public void CopyTo(Array dest, int index) { ((ICollection)_owner._items).CopyTo(dest, index); }
            public IEnumerator GetEnumerator() { return _owner._items.ToList().GetEnumerator(); }
            int IList.Add(object item) { Add(item is ListViewItem i ? i : new ListViewItem(item?.ToString())); return Count - 1; }
            bool IList.Contains(object item) { return item is ListViewItem i && Contains(i); }
            int IList.IndexOf(object item) { return item is ListViewItem i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object item) { Insert(index, (ListViewItem)item); }
            void IList.Remove(object item) { if (item is ListViewItem i) Remove(i); }
            bool IList.IsFixedSize { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { this[index] = (ListViewItem)value; } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
        }

        public class ColumnHeaderCollection : IList
        {
            private readonly ListView _owner;
            public ColumnHeaderCollection(ListView owner) { _owner = owner; }
            public int Count { get { return _owner._columns.Count; } }
            public bool IsReadOnly { get { return false; } }
            public virtual ColumnHeader this[int index] { get { return _owner._columns[index]; } }
            public virtual ColumnHeader this[string key] { get { return _owner._columns.FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)); } }
            public virtual int Add(ColumnHeader value)
            {
                value.ListViewInternal = _owner;
                _owner._columns.Add(value);
                if (value.Width < 0) value.Width = value.Width;
                _owner.ItemChanged();
                return _owner._columns.Count - 1;
            }
            public virtual ColumnHeader Add(string text) { ColumnHeader c = new ColumnHeader { Text = text }; Add(c); return c; }
            public virtual ColumnHeader Add(string text, int width) { ColumnHeader c = new ColumnHeader { Text = text }; c.WidthInternal = width; Add(c); return c; }
            public virtual ColumnHeader Add(string text, int width, HorizontalAlignment textAlign) { ColumnHeader c = Add(text, width); c.TextAlign = textAlign; return c; }
            public virtual ColumnHeader Add(string key, string text) { ColumnHeader c = Add(text); c.Name = key; return c; }
            public virtual ColumnHeader Add(string key, string text, int width) { ColumnHeader c = Add(text, width); c.Name = key; return c; }
            public virtual void AddRange(ColumnHeader[] values) { foreach (ColumnHeader c in values) Add(c); }
            public void Insert(int index, ColumnHeader value) { value.ListViewInternal = _owner; _owner._columns.Insert(index, value); _owner.ItemChanged(); }
            public void Insert(int index, string text, int width) { ColumnHeader c = new ColumnHeader { Text = text }; c.WidthInternal = width; Insert(index, c); }
            public virtual void Remove(ColumnHeader column) { _owner._columns.Remove(column); column.ListViewInternal = null; _owner.ItemChanged(); }
            public virtual void RemoveAt(int index) { Remove(_owner._columns[index]); }
            public virtual void RemoveByKey(string key) { ColumnHeader c = this[key]; if (c != null) Remove(c); }
            public virtual void Clear() { foreach (ColumnHeader c in _owner._columns) c.ListViewInternal = null; _owner._columns.Clear(); _owner.ItemChanged(); }
            public bool Contains(ColumnHeader value) { return _owner._columns.Contains(value); }
            public virtual bool ContainsKey(string key) { return this[key] != null; }
            public int IndexOf(ColumnHeader value) { return _owner._columns.IndexOf(value); }
            public virtual int IndexOfKey(string key) { return _owner._columns.FindIndex(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)); }
            public IEnumerator GetEnumerator() { return _owner._columns.ToList().GetEnumerator(); }
            int IList.Add(object value) { return Add((ColumnHeader)value); }
            bool IList.Contains(object value) { return value is ColumnHeader c && Contains(c); }
            int IList.IndexOf(object value) { return value is ColumnHeader c ? IndexOf(c) : -1; }
            void IList.Insert(int index, object value) { Insert(index, (ColumnHeader)value); }
            void IList.Remove(object value) { if (value is ColumnHeader c) Remove(c); }
            bool IList.IsFixedSize { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
            void ICollection.CopyTo(Array array, int index) { ((ICollection)_owner._columns).CopyTo(array, index); }
        }

        public class SelectedListViewItemCollection : IList
        {
            private readonly ListView _owner;
            public SelectedListViewItemCollection(ListView owner) { _owner = owner; }
            private List<ListViewItem> List() { return _owner._items.Where(i => i.Selected).ToList(); }
            public int Count { get { return _owner._items.Count(i => i.Selected); } }
            public bool IsReadOnly { get { return true; } }
            public ListViewItem this[int index] { get { return List()[index]; } }
            public virtual ListViewItem this[string key] { get { return List().FirstOrDefault(i => i.Name == key); } }
            public void Clear() { _owner.ClearSelection(); }
            public bool Contains(ListViewItem item) { return item.Selected && item.ListViewInternal == _owner; }
            public virtual bool ContainsKey(string key) { return this[key] != null; }
            public int IndexOf(ListViewItem item) { return List().IndexOf(item); }
            public virtual int IndexOfKey(string key) { return List().FindIndex(i => i.Name == key); }
            public void CopyTo(Array dest, int index) { ((ICollection)List()).CopyTo(dest, index); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
            int IList.Add(object value) { throw new NotSupportedException(); }
            bool IList.Contains(object value) { return value is ListViewItem i && Contains(i); }
            int IList.IndexOf(object value) { return value is ListViewItem i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.Remove(object value) { throw new NotSupportedException(); }
            void IList.RemoveAt(int index) { throw new NotSupportedException(); }
            bool IList.IsFixedSize { get { return true; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
        }

        public class SelectedIndexCollection : IList
        {
            private readonly ListView _owner;
            public SelectedIndexCollection(ListView owner) { _owner = owner; }
            private List<int> List() { List<int> l = new List<int>(); for (int i = 0; i < _owner._items.Count; i++) if (_owner._items[i].Selected) l.Add(i); return l; }
            public int Count { get { return List().Count; } }
            public bool IsReadOnly { get { return false; } }
            public int this[int index] { get { return List()[index]; } }
            public bool Contains(int selectedIndex) { return selectedIndex >= 0 && selectedIndex < _owner._items.Count && _owner._items[selectedIndex].Selected; }
            public int IndexOf(int selectedIndex) { return List().IndexOf(selectedIndex); }
            public int Add(int itemIndex) { _owner._items[itemIndex].Selected = true; return Count; }
            public void Remove(int itemIndex) { _owner._items[itemIndex].Selected = false; }
            public void Clear() { _owner.ClearSelection(); }
            public void CopyTo(Array dest, int index) { ((ICollection)List()).CopyTo(dest, index); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
            int IList.Add(object value) { return Add((int)value); }
            bool IList.Contains(object value) { return value is int i && Contains(i); }
            int IList.IndexOf(object value) { return value is int i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.Remove(object value) { if (value is int i) Remove(i); }
            void IList.RemoveAt(int index) { Remove(this[index]); }
            bool IList.IsFixedSize { get { return false; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
        }

        public class CheckedListViewItemCollection : IList
        {
            private readonly ListView _owner;
            public CheckedListViewItemCollection(ListView owner) { _owner = owner; }
            private List<ListViewItem> List() { return _owner._items.Where(i => i.Checked).ToList(); }
            public int Count { get { return List().Count; } }
            public bool IsReadOnly { get { return true; } }
            public ListViewItem this[int index] { get { return List()[index]; } }
            public bool Contains(ListViewItem item) { return item.Checked && item.ListViewInternal == _owner; }
            public int IndexOf(ListViewItem item) { return List().IndexOf(item); }
            public void CopyTo(Array dest, int index) { ((ICollection)List()).CopyTo(dest, index); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
            int IList.Add(object value) { throw new NotSupportedException(); }
            void IList.Clear() { throw new NotSupportedException(); }
            bool IList.Contains(object value) { return value is ListViewItem i && Contains(i); }
            int IList.IndexOf(object value) { return value is ListViewItem i ? IndexOf(i) : -1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.Remove(object value) { throw new NotSupportedException(); }
            void IList.RemoveAt(int index) { throw new NotSupportedException(); }
            bool IList.IsFixedSize { get { return true; } }
            object IList.this[int index] { get { return this[index]; } set { throw new NotSupportedException(); } }
            bool ICollection.IsSynchronized { get { return false; } }
            object ICollection.SyncRoot { get { return this; } }
        }

        public class CheckedIndexCollection : IEnumerable
        {
            private readonly ListView _owner;
            public CheckedIndexCollection(ListView owner) { _owner = owner; }
            private List<int> List() { List<int> l = new List<int>(); for (int i = 0; i < _owner._items.Count; i++) if (_owner._items[i].Checked) l.Add(i); return l; }
            public int Count { get { return List().Count; } }
            public int this[int index] { get { return List()[index]; } }
            public bool Contains(int checkedIndex) { return List().Contains(checkedIndex); }
            public IEnumerator GetEnumerator() { return List().GetEnumerator(); }
        }
    }

    public enum SearchDirectionHint { Left = 37, Up = 38, Right = 39, Down = 40 }

    public class DrawListViewItemEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ListViewItem Item { get; }
        public Rectangle Bounds { get; }
        public int ItemIndex { get; }
        public ListViewItemStates State { get; }
        public bool DrawDefault { get; set; }
        public DrawListViewItemEventArgs(Graphics graphics, ListViewItem item, Rectangle bounds, int itemIndex, ListViewItemStates state)
        {
            Graphics = graphics; Item = item; Bounds = bounds; ItemIndex = itemIndex; State = state;
        }
        public void DrawBackground() { using (SolidBrush b = new SolidBrush(Item.BackColor)) Graphics.FillRectangle(b, Bounds); }
        public void DrawFocusRectangle() { }
        public void DrawText() { using (SolidBrush b = new SolidBrush(Item.ForeColor)) Graphics.DrawString(Item.Text, Item.Font, b, Bounds); }
        public void DrawText(TextFormatFlags flags) { DrawText(); }
    }

    public delegate void DrawListViewItemEventHandler(object sender, DrawListViewItemEventArgs e);

    public class DrawListViewSubItemEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public Rectangle Bounds { get; }
        public ListViewItem Item { get; }
        public ListViewItem.ListViewSubItem SubItem { get; }
        public int ItemIndex { get; }
        public int ColumnIndex { get; }
        public ColumnHeader Header { get; }
        public ListViewItemStates ItemState { get; }
        public bool DrawDefault { get; set; }
        public DrawListViewSubItemEventArgs(Graphics graphics, Rectangle bounds, ListViewItem item, ListViewItem.ListViewSubItem subItem, int itemIndex,
                                            int columnIndex, ColumnHeader header, ListViewItemStates itemState)
        {
            Graphics = graphics; Bounds = bounds; Item = item; SubItem = subItem; ItemIndex = itemIndex; ColumnIndex = columnIndex; Header = header; ItemState = itemState;
        }
    }

    public delegate void DrawListViewSubItemEventHandler(object sender, DrawListViewSubItemEventArgs e);

    public class DrawListViewColumnHeaderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public Rectangle Bounds { get; }
        public int ColumnIndex { get; }
        public ColumnHeader Header { get; }
        public ListViewItemStates State { get; }
        public Color ForeColor { get; }
        public Color BackColor { get; }
        public Font Font { get; }
        public bool DrawDefault { get; set; }
        public DrawListViewColumnHeaderEventArgs(Graphics graphics, Rectangle bounds, int columnIndex, ColumnHeader header, ListViewItemStates state,
                                                 Color foreColor, Color backColor, Font font)
        {
            Graphics = graphics; Bounds = bounds; ColumnIndex = columnIndex; Header = header; State = state; ForeColor = foreColor; BackColor = backColor; Font = font;
        }
        public void DrawBackground() { }
        public void DrawText() { }
    }

    public delegate void DrawListViewColumnHeaderEventHandler(object sender, DrawListViewColumnHeaderEventArgs e);
}
