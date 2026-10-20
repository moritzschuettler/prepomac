// PrePoMax for macOS - the WinForms PropertyGrid: categories, properties and expandable sub-properties with text editing,
// lists of standard values and UITypeEditors (modal dialogs and drop-downs), drawn like the property grid of WinForms.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Windows.Forms.Design;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using AC = Avalonia.Controls;

namespace System.Windows.Forms
{
    public enum PropertySort { NoSort = 0, Alphabetical = 1, Categorized = 2, CategorizedAlphabetical = 3 }

    public enum GridItemType { Property = 0, Category = 1, ArrayValue = 2, Root = 3 }

    public abstract class GridItem
    {
        public abstract GridItemCollection GridItems { get; }
        public abstract GridItemType GridItemType { get; }
        public abstract string Label { get; }
        public abstract GridItem Parent { get; }
        public abstract PropertyDescriptor PropertyDescriptor { get; }
        public abstract object Value { get; }
        public virtual bool Expandable { get { return false; } }
        public virtual bool Expanded { get { return false; } set { } }
        public object Tag { get; set; }
        public abstract bool Select();
    }

    public class GridItemCollection : ICollection
    {
        public static GridItemCollection Empty = new GridItemCollection(new GridItem[0]);
        private readonly GridItem[] _items;
        internal GridItemCollection(GridItem[] items) { _items = items ?? new GridItem[0]; }
        public int Count { get { return _items.Length; } }
        public GridItem this[int index] { get { return _items[index]; } }
        public GridItem this[string label] { get { return _items.FirstOrDefault(i => i.Label == label); } }
        public IEnumerator GetEnumerator() { return _items.GetEnumerator(); }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array dest, int index) { _items.CopyTo(dest, index); }
    }

    public class SelectedGridItemChangedEventArgs : EventArgs
    {
        public GridItem OldSelection { get; }
        public GridItem NewSelection { get; }
        public SelectedGridItemChangedEventArgs(GridItem oldSel, GridItem newSel) { OldSelection = oldSel; NewSelection = newSel; }
    }

    public delegate void SelectedGridItemChangedEventHandler(object sender, SelectedGridItemChangedEventArgs e);

    public class PropertyValueChangedEventArgs : EventArgs
    {
        public GridItem ChangedItem { get; }
        public object OldValue { get; }
        public PropertyValueChangedEventArgs(GridItem changedItem, object oldValue) { ChangedItem = changedItem; OldValue = oldValue; }
    }

    public delegate void PropertyValueChangedEventHandler(object s, PropertyValueChangedEventArgs e);

    public class PropertyTabCollection : IEnumerable
    {
        public int Count { get { return 0; } }
        public IEnumerator GetEnumerator() { return new object[0].GetEnumerator(); }
        public void AddTabType(Type propertyTabType) { }
        public void AddTabType(Type propertyTabType, PropertyTabScope tabScope) { }
    }

    public enum PropertyTabScope { Static = 0, Global = 1, Document = 2, Component = 3 }

    // An item of the grid; it is the type descriptor context of the converters and editors of its property
    internal sealed class PropertyGridEntry : GridItem, ITypeDescriptorContext, IWindowsFormsEditorService, IServiceProvider
    {
        private readonly PropertyGrid _grid;
        private readonly PropertyGridEntry _parent;
        private readonly GridItemType _type;
        private readonly string _label;
        private readonly PropertyDescriptor _property;
        private readonly object _component;
        internal readonly List<PropertyGridEntry> Children = new List<PropertyGridEntry>();
        private bool _expanded;

        public PropertyGridEntry(PropertyGrid grid, PropertyGridEntry parent, GridItemType type, string label, PropertyDescriptor property, object component)
        {
            _grid = grid;
            _parent = parent;
            _type = type;
            _label = label;
            _property = property;
            _component = component;
            _expanded = type == GridItemType.Category || type == GridItemType.Root;
        }
        public override GridItemCollection GridItems { get { return new GridItemCollection(Children.Cast<GridItem>().ToArray()); } }
        public override GridItemType GridItemType { get { return _type; } }
        public override string Label { get { return _label; } }
        public override GridItem Parent { get { return _parent; } }
        internal PropertyGridEntry ParentEntry { get { return _parent; } }
        public override PropertyDescriptor PropertyDescriptor { get { return _property; } }
        public object Component { get { return _component; } }
        public override object Value
        {
            get
            {
                if (_property == null) return _type == GridItemType.Root ? _component : null;
                try { return _property.GetValue(_component); }
                catch { return null; }
            }
        }
        public override bool Expandable { get { return Children.Count > 0; } }
        internal bool ExpandedInternal { get { return _expanded; } set { _expanded = value; } }
        public override bool Expanded
        {
            get { return _expanded; }
            set
            {
                if (_expanded == value) return;
                _expanded = value;
                _grid.GridView.LayoutRows();
            }
        }
        public override bool Select()
        {
            _grid.SelectEntry(this, true);
            return true;
        }
        internal int Level { get { int l = 0; for (PropertyGridEntry p = _parent; p != null && p._type != GridItemType.Root; p = p._parent) if (p._type != GridItemType.Category) l++; return l; } }
        internal bool IsReadOnly
        {
            get
            {
                if (_property == null) return true;
                if (_grid.ReadOnlyAll) return true;
                if (_property.IsReadOnly) return true;
                ReadOnlyAttribute attr = _property.Attributes[typeof(ReadOnlyAttribute)] as ReadOnlyAttribute;
                if (attr != null && attr.IsReadOnly) return true;
                for (PropertyGridEntry p = _parent; p != null; p = p._parent)
                    if (p._property != null && p._property.IsReadOnly && !(p._property.Converter is ExpandableObjectConverter)) return true;
                return false;
            }
        }
        internal TypeConverter Converter { get { return _property?.Converter ?? TypeDescriptor.GetConverter(typeof(object)); } }
        internal UITypeEditor Editor
        {
            get
            {
                if (_property == null) return null;
                try { return _property.GetEditor(typeof(UITypeEditor)) as UITypeEditor; }
                catch { return null; }
            }
        }
        internal string ValueText
        {
            get
            {
                object value = Value;
                try
                {
                    string text = Converter.ConvertToString(this, CultureInfo.CurrentCulture, value);
                    return text ?? string.Empty;
                }
                catch
                {
                    return value?.ToString() ?? string.Empty;
                }
            }
        }
        internal bool IsModified
        {
            get
            {
                try { return _property != null && _property.ShouldSerializeValue(_component); }
                catch { return false; }
            }
        }
        internal string Description { get { return _property?.Description ?? string.Empty; } }
        // Sets the value of the property; the value of the parent is set again for value types (Point, Size, ...)
        internal void SetValue(object value)
        {
            object old = Value;
            _property.SetValue(_component, value);
            if (_parent != null && _parent._property != null && _parent.Value is object parentValue && parentValue.GetType().IsValueType)
                _parent.SetValue(parentValue);
            _grid.OnValueSet(this, old);
        }
        internal bool HasStandardValues
        {
            get
            {
                try
                {
                    TypeConverter c = Converter;
                    return c.GetStandardValuesSupported(this) && (c.GetStandardValues(this)?.Count ?? 0) > 0;
                }
                catch { return false; }
            }
        }
        internal bool StandardValuesExclusive
        {
            get
            {
                try { return Converter.GetStandardValuesExclusive(this); }
                catch { return true; }
            }
        }
        internal IList StandardValues
        {
            get
            {
                try
                {
                    ICollection values = Converter.GetStandardValues(this);
                    return values == null ? new List<object>() : values.Cast<object>().ToList();
                }
                catch { return new List<object>(); }
            }
        }

        // ITypeDescriptorContext
        public IContainer Container { get { return null; } }
        public object Instance { get { return _component; } }
        public void OnComponentChanged() { }
        public bool OnComponentChanging() { return true; }
        public object GetService(Type serviceType)
        {
            if (serviceType == typeof(IWindowsFormsEditorService)) return this;
            if (serviceType == typeof(ITypeDescriptorContext)) return this;
            return _grid.Site?.GetService(serviceType);
        }
        // IWindowsFormsEditorService
        public void CloseDropDown() { _grid.GridView.CloseDropDown(); }
        public void DropDownControl(Control control) { _grid.GridView.DropDownControl(this, control); }
        public DialogResult ShowDialog(Form dialog) { return dialog.ShowDialog(_grid.FindForm()); }
        public override string ToString() { return _label; }
    }

    public class PropertyGrid : ContainerControl, IComPropertyBrowser
    {
        private object[] _selectedObjects = new object[0];
        internal readonly PropertyGridView gridView;
        private readonly HelpPane _helpPane;
        private PropertyGridEntry _root;
        private PropertyGridEntry _selected;
        private bool _helpVisible = true;
        private bool _toolbarVisible = true;
        private PropertySort _propertySort = PropertySort.CategorizedAlphabetical;
        private AttributeCollection _browsableAttributes = new AttributeCollection(BrowsableAttribute.Yes);
        internal bool ReadOnlyAll;

        public PropertyGrid()
        {
            gridView = new PropertyGridView(this);
            _helpPane = new HelpPane(this);
            Controls.Add(gridView);
            Controls.Add(_helpPane);
            SetStyle(ControlStyles.Selectable, true);
            LayoutParts();
        }
        protected override Size DefaultSize { get { return new Size(130, 130); } }
        internal PropertyGridView GridView { get { return gridView; } }

        // Properties
        public object SelectedObject
        {
            get { return _selectedObjects.Length > 0 ? _selectedObjects[0] : null; }
            set { SelectedObjects = value == null ? new object[0] : new[] { value }; }
        }
        public object[] SelectedObjects
        {
            get { return (object[])_selectedObjects.Clone(); }
            set
            {
                _selectedObjects = value ?? new object[0];
                string selectedLabel = _selected?.Label;
                Rebuild(selectedLabel, null);
                OnSelectedObjectsChanged(EventArgs.Empty);
            }
        }
        public GridItem SelectedGridItem
        {
            get { return _selected; }
            set { SelectEntry(value as PropertyGridEntry, true); }
        }
        public bool HelpVisible { get { return _helpVisible; } set { _helpVisible = value; LayoutParts(); } }
        public bool ToolbarVisible { get { return _toolbarVisible; } set { _toolbarVisible = value; LayoutParts(); } }
        public bool CommandsVisibleIfAvailable { get; set; } = true;
        public bool CanShowCommands { get { return false; } }
        public bool CommandsVisible { get { return false; } }
        public bool LargeButtons { get; set; }
        public bool UseCompatibleTextRendering { get; set; }
        public PropertySort PropertySort { get { return _propertySort; } set { _propertySort = value; Rebuild(_selected?.Label, null); OnPropertySortChanged(EventArgs.Empty); } }
        public AttributeCollection BrowsableAttributes { get { return _browsableAttributes; } set { _browsableAttributes = value ?? new AttributeCollection(BrowsableAttribute.Yes); Rebuild(_selected?.Label, null); } }
        public PropertyTabCollection PropertyTabs { get; } = new PropertyTabCollection();
        public Color LineColor { get; set; } = SystemColors.InactiveBorder;
        public Color CategoryForeColor { get; set; } = SystemColors.ControlText;
        public Color CategorySplitterColor { get; set; } = SystemColors.Control;
        public Color ViewBackColor { get; set; } = SystemColors.Window;
        public Color ViewForeColor { get; set; } = SystemColors.WindowText;
        public Color ViewBorderColor { get; set; } = SystemColors.ControlDark;
        public Color HelpBackColor { get; set; } = SystemColors.Control;
        public Color HelpForeColor { get; set; } = SystemColors.ControlText;
        public Color HelpBorderColor { get; set; } = SystemColors.ControlDark;
        public Color CommandsBackColor { get; set; } = SystemColors.Control;
        public Color CommandsForeColor { get; set; } = SystemColors.ControlText;
        public Color DisabledItemForeColor { get; set; } = SystemColors.GrayText;
        public Color SelectedItemWithFocusBackColor { get; set; } = SystemColors.Highlight;
        public Color SelectedItemWithFocusForeColor { get; set; } = SystemColors.HighlightText;
        public bool InPropertySet { get; private set; }
        public override string Text { get { return base.Text; } set { base.Text = value; } }

        // Events
        public event PropertyValueChangedEventHandler PropertyValueChanged;
        public event SelectedGridItemChangedEventHandler SelectedGridItemChanged;
        public event EventHandler SelectedObjectsChanged;
        public event EventHandler PropertySortChanged;
        public event PropertyTabChangedEventHandler PropertyTabChanged;
        public event EventHandler ComComponentNameChanged;
        protected virtual void OnPropertyValueChanged(PropertyValueChangedEventArgs e) { PropertyValueChanged?.Invoke(this, e); }
        protected virtual void OnSelectedGridItemChanged(SelectedGridItemChangedEventArgs e) { SelectedGridItemChanged?.Invoke(this, e); }
        protected virtual void OnSelectedObjectsChanged(EventArgs e) { SelectedObjectsChanged?.Invoke(this, e); }
        protected virtual void OnPropertySortChanged(EventArgs e) { PropertySortChanged?.Invoke(this, e); }

        // Methods
        public override void Refresh()
        {
            gridView.EndEdit(true);
            Rebuild(_selected?.Label, _selected?.ParentEntry?.Label);
            base.Refresh();
        }
        public void RefreshTabs(PropertyTabScope tabScope) { }
        public void ExpandAllGridItems()
        {
            if (_root == null) return;
            foreach (PropertyGridEntry e in All(_root)) if (e.Children.Count > 0) e.Expanded = true;
        }
        public void CollapseAllGridItems()
        {
            if (_root == null) return;
            foreach (PropertyGridEntry e in All(_root)) if (e.GridItemType == GridItemType.Property && e.Children.Count > 0) e.Expanded = false;
        }
        public void ResetSelectedProperty()
        {
            if (_selected?.PropertyDescriptor == null) return;
            object old = _selected.Value;
            _selected.PropertyDescriptor.ResetValue(_selected.Component);
            OnValueSet(_selected, old);
        }
        internal static IEnumerable<PropertyGridEntry> All(PropertyGridEntry entry)
        {
            yield return entry;
            foreach (PropertyGridEntry child in entry.Children)
                foreach (PropertyGridEntry e in All(child)) yield return e;
        }
        internal void OnValueSet(PropertyGridEntry entry, object oldValue)
        {
            InPropertySet = true;
            try
            {
                string label = entry.Label;
                string parentLabel = entry.ParentEntry?.Label;
                Rebuild(label, parentLabel);
                PropertyGridEntry changed = _selected ?? entry;
                OnPropertyValueChanged(new PropertyValueChangedEventArgs(changed, oldValue));
            }
            finally
            {
                InPropertySet = false;
            }
        }
        // Builds the items of the selected object and selects the item with the label
        private void Rebuild(string selectedLabel, string selectedParentLabel)
        {
            Dictionary<string, bool> expansion = new Dictionary<string, bool>();
            if (_root != null)
                foreach (PropertyGridEntry e in All(_root))
                    if (e.Children.Count > 0) expansion[Path(e)] = e.Expanded;
            PropertyGridEntry oldSelected = _selected;
            object component = SelectedObject;
            _root = component == null ? null : new PropertyGridEntry(this, null, GridItemType.Root, component.GetType().Name, null, component);
            if (_root != null)
            {
                List<PropertyDescriptor> properties = GetProperties(component, null, null);
                if (_propertySort == PropertySort.Categorized || _propertySort == PropertySort.CategorizedAlphabetical)
                {
                    var groups = new List<(string Category, List<PropertyDescriptor> Properties)>();
                    foreach (PropertyDescriptor pd in properties)
                    {
                        string category = string.IsNullOrEmpty(pd.Category) ? "Misc" : pd.Category;
                        int index = groups.FindIndex(g => g.Category == category);
                        if (index < 0) groups.Add((category, new List<PropertyDescriptor> { pd }));
                        else groups[index].Properties.Add(pd);
                    }
                    groups.Sort((a, b) => CompareNames(a.Category, b.Category));
                    foreach (var group in groups)
                    {
                        PropertyGridEntry category = new PropertyGridEntry(this, _root, GridItemType.Category, group.Category, null, component);
                        _root.Children.Add(category);
                        AddProperties(category, component, group.Properties, 0);
                    }
                }
                else AddProperties(_root, component, properties, 0);
                foreach (PropertyGridEntry e in All(_root))
                    if (e.Children.Count > 0 && expansion.TryGetValue(Path(e), out bool expanded)) e.ExpandedInternal = expanded;
            }
            // Selection by label like WinForms
            PropertyGridEntry select = null;
            if (_root != null && selectedLabel != null)
            {
                select = All(_root).FirstOrDefault(e => e.Label == selectedLabel && e.ParentEntry?.Label == selectedParentLabel && e.GridItemType != GridItemType.Root)
                         ?? All(_root).FirstOrDefault(e => e.Label == selectedLabel && e.GridItemType != GridItemType.Root);
            }
            if (select == null && _root != null)
                select = All(_root).FirstOrDefault(e => e.GridItemType == GridItemType.Property) ?? All(_root).FirstOrDefault(e => e.GridItemType == GridItemType.Category);
            _selected = select;
            gridView.LayoutRows();
            _helpPane.Invalidate();
            if (!ReferenceEquals(oldSelected, select) && (oldSelected?.Label != select?.Label || oldSelected == null))
                OnSelectedGridItemChanged(new SelectedGridItemChangedEventArgs(oldSelected, select));
        }
        private static string Path(PropertyGridEntry e)
        {
            string path = e.Label;
            for (PropertyGridEntry p = e.ParentEntry; p != null; p = p.ParentEntry) path = p.Label + "/" + path;
            return path;
        }
        private void AddProperties(PropertyGridEntry parent, object component, List<PropertyDescriptor> properties, int depth)
        {
            if (_propertySort != PropertySort.NoSort && _propertySort != PropertySort.Categorized)
                properties.Sort((a, b) => CompareNames(a.DisplayName, b.DisplayName));
            foreach (PropertyDescriptor pd in properties)
            {
                PropertyGridEntry entry = new PropertyGridEntry(this, parent, GridItemType.Property, pd.DisplayName, pd, component);
                parent.Children.Add(entry);
                if (depth < 4)
                {
                    try
                    {
                        TypeConverter converter = pd.Converter;
                        object value = pd.GetValue(component);
                        if (value != null && converter.GetPropertiesSupported(entry))
                        {
                            List<PropertyDescriptor> children = GetProperties(value, converter, entry);
                            AddProperties(entry, value, children, depth + 1);
                            entry.ExpandedInternal = false;
                        }
                    }
                    catch { }
                }
            }
        }
        private List<PropertyDescriptor> GetProperties(object component, TypeConverter converter, ITypeDescriptorContext context)
        {
            Attribute[] attributes = _browsableAttributes.Cast<Attribute>().ToArray();
            PropertyDescriptorCollection collection;
            try
            {
                collection = converter != null ? converter.GetProperties(context, component, attributes) : TypeDescriptor.GetProperties(component, attributes);
            }
            catch
            {
                collection = TypeDescriptor.GetProperties(component, attributes);
            }
            List<PropertyDescriptor> list = new List<PropertyDescriptor>();
            if (collection == null) return list;
            foreach (PropertyDescriptor pd in collection)
            {
                if (!pd.IsBrowsable) continue;
                list.Add(pd);
            }
            return list;
        }
        // Names with more leading zero width spaces come first (the order of the DynamicTypeDescriptor); then alphabetically
        internal static int CompareNames(string a, string b)
        {
            int za = LeadingZeroWidth(a), zb = LeadingZeroWidth(b);
            if (za != zb) return zb.CompareTo(za);
            return string.Compare(a.Substring(za), b.Substring(zb), CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
        }
        private static int LeadingZeroWidth(string s)
        {
            int n = 0;
            while (n < s.Length && (s[n] == '​' || s[n] == '\t')) n++;
            return n;
        }
        internal void SelectEntry(PropertyGridEntry entry, bool raise)
        {
            if (entry == _selected) return;
            gridView.EndEdit(false);
            PropertyGridEntry old = _selected;
            _selected = entry;
            if (entry != null)
            {
                // The parents of the selection are expanded
                for (PropertyGridEntry p = entry.ParentEntry; p != null; p = p.ParentEntry) if (!p.Expanded) p.ExpandedInternal = true;
            }
            gridView.LayoutRows();
            gridView.EnsureVisible(entry);
            _helpPane.Invalidate();
            if (raise) OnSelectedGridItemChanged(new SelectedGridItemChangedEventArgs(old, entry));
        }
        internal PropertyGridEntry SelectedEntry { get { return _selected; } }
        internal PropertyGridEntry Root { get { return _root; } }
        private void LayoutParts()
        {
            if (gridView == null || _helpPane == null) return;
            Size client = ClientSize;
            int helpHeight = _helpVisible ? Math.Min(Math.Max(0, client.Height / 3), 60) : 0;
            int toolbarHeight = 0;
            gridView.SetBoundsCoreFromLayout(0, toolbarHeight, client.Width, Math.Max(0, client.Height - toolbarHeight - helpHeight - (_helpVisible ? 3 : 0)));
            _helpPane.Visible = _helpVisible;
            _helpPane.SetBoundsCoreFromLayout(0, client.Height - helpHeight, client.Width, helpHeight);
        }
        internal override void LayoutChildren() { LayoutParts(); }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            gridView?.LayoutRows();
        }
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            gridView.Focus();
        }
        public event EventHandler ComComponentNameChanged_ { add { } remove { } }
        bool IComPropertyBrowser.InPropertySet { get { return InPropertySet; } }

        private sealed class HelpPane : Control
        {
            private readonly PropertyGrid _grid;
            public HelpPane(PropertyGrid grid)
            {
                _grid = grid;
                SetStyle(ControlStyles.Selectable, false);
            }
            internal override bool PaintsItself { get { return true; } }
            protected override void OnPaint(PaintEventArgs e)
            {
                Graphics g = e.Graphics;
                using (SolidBrush back = new SolidBrush(_grid.HelpBackColor)) g.FillRectangle(back, ClientRectangle);
                using (Pen border = new Pen(_grid.HelpBorderColor)) g.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
                PropertyGridEntry entry = _grid.SelectedEntry;
                if (entry != null)
                {
                    Font bold = new Font(_grid.Font, FontStyle.Bold);
                    using (SolidBrush text = new SolidBrush(_grid.HelpForeColor))
                    {
                        string label = entry.Label.TrimStart('​', '\t');
                        g.DrawString(label, bold, text, new RectangleF(3, 3, Width - 6, bold.Height + 2), new StringFormat { FormatFlags = StringFormatFlags.NoWrap });
                        g.DrawString(entry.Description, _grid.Font, text, new RectangleF(3, 5 + bold.Height, Width - 6, Height - bold.Height - 7));
                    }
                }
                base.OnPaint(e);
            }
        }
    }

    public interface IComPropertyBrowser
    {
        bool InPropertySet { get; }
    }

    public class PropertyTabChangedEventArgs : EventArgs { }

    public delegate void PropertyTabChangedEventHandler(object s, PropertyTabChangedEventArgs e);

    // The view of the rows of the grid (the WinForms PropertyGridView); the field labelRatio is set by PrePoMax through reflection
    internal sealed class PropertyGridView : Control
    {
        public double labelRatio = 2.0;
        private readonly PropertyGrid _grid;
        private readonly ScrollHelper _scroll;
        private readonly List<PropertyGridEntry> _rows = new List<PropertyGridEntry>();
        private readonly TextBox _edit;
        private PropertyGridEntry _editEntry;
        private bool _editing;
        private Popup _dropDown;
        private bool _dropDownOpen;
        private bool _resizingLabel;
        private const int Margin = 16;

        public PropertyGridView(PropertyGrid grid)
        {
            _grid = grid;
            _scroll = new ScrollHelper(this, () => ClientRectangle);
            _scroll.Scrolled += PlaceEditor;
            _edit = new TextBox { BorderStyle = BorderStyle.None, Visible = false };
            Controls.Add(_edit);
            _edit.KeyDown += Edit_KeyDown;
            _edit.LostFocus += (s, e) => Dispatcher.UIThread.Post(() => { if (_editing && !_edit.Focused && !_dropDownOpen) EndEdit(false); });
            _edit.DoubleClick += (s, e) => CycleValue();
            SetStyle(ControlStyles.Selectable, true);
        }
        private int RowHeight { get { return Math.Max(16, _grid.Font.Height + 3); } }
        private int LabelWidth
        {
            get
            {
                int width = ClientSize.Width - (_scroll.VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0);
                return Math.Max(Margin + 20, (int)(width / Math.Max(1.01, labelRatio)));
            }
        }
        private int ContentWidth { get { return ClientSize.Width - (_scroll.VerticalVisible ? SystemInformation.VerticalScrollBarWidth : 0); } }

        internal void LayoutRows()
        {
            _rows.Clear();
            if (_grid.Root != null) AddRows(_grid.Root);
            _scroll.SmallChangeY = RowHeight;
            _scroll.SetExtent(0, _rows.Count * RowHeight);
            PlaceEditor();
            Invalidate();
        }
        private void AddRows(PropertyGridEntry entry)
        {
            foreach (PropertyGridEntry child in entry.Children)
            {
                _rows.Add(child);
                if (child.Expanded && child.Children.Count > 0) AddRows(child);
            }
        }
        internal void EnsureVisible(PropertyGridEntry entry)
        {
            int index = _rows.IndexOf(entry);
            if (index >= 0) _scroll.EnsureVisible(new Rectangle(0, index * RowHeight, 1, RowHeight));
            PlaceEditor();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            LayoutRows();
        }

        // Painting
        internal override bool PaintsItself { get { return true; } }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Size client = ClientSize;
            int h = RowHeight;
            int labelWidth = LabelWidth;
            int width = ContentWidth;
            using (SolidBrush back = new SolidBrush(_grid.ViewBackColor)) g.FillRectangle(back, ClientRectangle);
            using (SolidBrush line = new SolidBrush(_grid.LineColor)) g.FillRectangle(line, 0, 0, Margin, client.Height);
            int first = Math.Max(0, _scroll.OffsetY / h);
            int last = Math.Min(_rows.Count - 1, (_scroll.OffsetY + client.Height) / h + 1);
            Font font = _grid.Font;
            Font bold = new Font(font, FontStyle.Bold);
            bool focused = ContainsFocus;
            using (Pen linePen = new Pen(_grid.LineColor))
            using (SolidBrush lineBrush = new SolidBrush(_grid.LineColor))
            {
                for (int i = first; i <= last; i++)
                {
                    PropertyGridEntry entry = _rows[i];
                    int y = i * h - _scroll.OffsetY;
                    bool selected = entry == _grid.SelectedEntry;
                    StringFormat format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.None };
                    if (entry.GridItemType == GridItemType.Category)
                    {
                        g.FillRectangle(lineBrush, 0, y, width, h);
                        DrawGlyph(g, 4, y + (h - 9) / 2, entry.Expanded);
                        Rectangle textRect = new Rectangle(Margin + 2, y, width - Margin - 2, h);
                        if (selected)
                        {
                            Size textSize = ContentLayout.MeasureText(entry.Label, bold);
                            using (SolidBrush sel = new SolidBrush(focused ? _grid.SelectedItemWithFocusBackColor : SystemColors.ControlDark))
                                g.FillRectangle(sel, Margin + 1, y + 1, textSize.Width + 4, h - 2);
                        }
                        using (SolidBrush text = new SolidBrush(selected && focused ? _grid.SelectedItemWithFocusForeColor : _grid.CategoryForeColor))
                            g.DrawString(Clean(entry.Label), bold, text, textRect, format);
                        continue;
                    }
                    int indent = Margin + entry.Level * 10;
                    // Label
                    Rectangle label = new Rectangle(indent, y, labelWidth - indent, h);
                    if (selected)
                    {
                        using (SolidBrush sel = new SolidBrush(focused ? _grid.SelectedItemWithFocusBackColor : SystemColors.ControlDark))
                            g.FillRectangle(sel, Margin, y, labelWidth - Margin, h - 1);
                    }
                    if (entry.Children.Count > 0) DrawGlyph(g, indent - 12, y + (h - 9) / 2, entry.Expanded);
                    bool readOnly = entry.IsReadOnly;
                    Color labelColor = selected && focused ? _grid.SelectedItemWithFocusForeColor : selected ? Color.White : readOnly ? _grid.DisabledItemForeColor : _grid.ViewForeColor;
                    using (SolidBrush text = new SolidBrush(labelColor))
                        g.DrawString(Clean(entry.Label), font, text, new RectangleF(label.X + 2, y, label.Width - 3, h), format);
                    // Value
                    int valueX = labelWidth + 1;
                    UITypeEditor editor = entry.Editor;
                    bool paintValue = false;
                    try { paintValue = editor != null && editor.GetPaintValueSupported(entry); } catch { }
                    if (paintValue)
                    {
                        Rectangle swatch = new Rectangle(valueX + 2, y + 2, 20, h - 5);
                        try { editor.PaintValue(new PaintValueEventArgs(entry, entry.Value, g, swatch)); } catch { }
                        g.DrawRectangle(Pens.Black, swatch);
                        valueX += 24;
                    }
                    if (!(_editing && _editEntry == entry))
                    {
                        Font valueFont = entry.IsModified ? bold : font;
                        using (SolidBrush text = new SolidBrush(readOnly ? _grid.DisabledItemForeColor : _grid.ViewForeColor))
                            g.DrawString(entry.ValueText, valueFont, text, new RectangleF(valueX + 1, y, width - valueX - 2 - (selected ? DropButtonWidth(entry) : 0), h), format);
                    }
                    if (selected && !readOnly) DrawDropButton(g, entry, y, h, width);
                    // Grid lines
                    g.DrawLine(linePen, Margin, y + h - 1, width, y + h - 1);
                    g.DrawLine(linePen, labelWidth, y, labelWidth, y + h - 1);
                }
            }
            base.OnPaint(e);
        }
        private static string Clean(string label) { return label.TrimStart('​', '\t'); }
        private static void DrawGlyph(Graphics g, int x, int y, bool expanded)
        {
            using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, x, y, 9, 9);
            using (Pen border = new Pen(Color.FromArgb(0x80, 0x80, 0x80))) g.DrawRectangle(border, x, y, 8, 8);
            using (Pen sign = new Pen(Color.Black))
            {
                g.DrawLine(sign, x + 2, y + 4, x + 6, y + 4);
                if (!expanded) g.DrawLine(sign, x + 4, y + 2, x + 4, y + 6);
            }
        }
        private int DropButtonWidth(PropertyGridEntry entry)
        {
            if (entry.IsReadOnly) return 0;
            UITypeEditorEditStyle style = EditStyle(entry);
            return style != UITypeEditorEditStyle.None || entry.HasStandardValues ? 17 : 0;
        }
        private UITypeEditorEditStyle EditStyle(PropertyGridEntry entry)
        {
            UITypeEditor editor = entry.Editor;
            if (editor == null) return UITypeEditorEditStyle.None;
            try { return editor.GetEditStyle(entry); }
            catch { return UITypeEditorEditStyle.None; }
        }
        private void DrawDropButton(Graphics g, PropertyGridEntry entry, int y, int h, int width)
        {
            int w = DropButtonWidth(entry);
            if (w == 0) return;
            Rectangle r = new Rectangle(width - w, y, w, h - 1);
            using (SolidBrush face = new SolidBrush(Color.FromArgb(0xE1, 0xE1, 0xE1))) g.FillRectangle(face, r);
            using (Pen border = new Pen(Color.FromArgb(0xAD, 0xAD, 0xAD))) g.DrawRectangle(border, r.X, r.Y, r.Width - 1, r.Height - 1);
            using (SolidBrush glyph = new SolidBrush(Color.Black))
            {
                int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
                if (EditStyle(entry) == UITypeEditorEditStyle.Modal)
                {
                    for (int i = -1; i <= 1; i++) g.FillRectangle(glyph, cx + i * 3 - 1, cy + 2, 2, 2);
                }
                else g.FillPolygon(glyph, new[] { new Point(cx - 3, cy - 1), new Point(cx + 4, cy - 1), new Point(cx, cy + 3) });
            }
        }

        // Mouse
        internal override bool CanSelectOnClick { get { return true; } }
        private PropertyGridEntry EntryAt(int y)
        {
            int index = (y + _scroll.OffsetY) / RowHeight;
            return index >= 0 && index < _rows.Count ? _rows[index] : null;
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (Math.Abs(e.X - LabelWidth) <= 2 && e.Button == MouseButtons.Left)
            {
                _resizingLabel = true;
                return;
            }
            PropertyGridEntry entry = EntryAt(e.Y);
            if (entry == null) return;
            int indent = Margin + entry.Level * 10;
            bool onGlyph = (entry.GridItemType == GridItemType.Category && e.X < Margin) ||
                           (entry.Children.Count > 0 && e.X >= indent - 14 && e.X < indent);
            if (onGlyph || (e.Clicks == 2 && entry.Children.Count > 0 && e.X < LabelWidth))
            {
                entry.Expanded = !entry.Expanded;
                _grid.SelectEntry(entry, true);
                return;
            }
            bool wasSelected = entry == _grid.SelectedEntry;
            _grid.SelectEntry(entry, true);
            if (entry.GridItemType != GridItemType.Property || entry.IsReadOnly || e.Button != MouseButtons.Left) return;
            int width = ContentWidth;
            int buttonWidth = DropButtonWidth(entry);
            if (buttonWidth > 0 && e.X >= width - buttonWidth)
            {
                OpenEditor(entry);
                return;
            }
            if (e.X > LabelWidth)
            {
                if (e.Clicks == 2) CycleValue();
                else BeginEdit(entry);
            }
            else if (e.Clicks == 2) CycleValue();
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_resizingLabel)
            {
                int width = ContentWidth;
                int x = Math.Max(Margin + 20, Math.Min(width - 20, e.X));
                labelRatio = (double)width / x;
                PlaceEditor();
                Invalidate();
            }
            else Cursor = Math.Abs(e.X - LabelWidth) <= 2 ? Cursors.VSplit : null;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _resizingLabel = false;
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
            return key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || key == Keys.Enter || base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;
            PropertyGridEntry entry = _grid.SelectedEntry;
            int index = _rows.IndexOf(entry);
            switch (e.KeyCode)
            {
                case Keys.Down: if (index + 1 < _rows.Count) _grid.SelectEntry(_rows[index + 1], true); e.Handled = true; break;
                case Keys.Up: if (index > 0) _grid.SelectEntry(_rows[index - 1], true); e.Handled = true; break;
                case Keys.Right: if (entry != null && entry.Children.Count > 0) entry.Expanded = true; e.Handled = true; break;
                case Keys.Left: if (entry != null && entry.Children.Count > 0) entry.Expanded = false; e.Handled = true; break;
                case Keys.Enter: if (entry != null && entry.GridItemType == GridItemType.Property && !entry.IsReadOnly) BeginEdit(entry); e.Handled = true; break;
                default:
                    break;
            }
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            // Typing on a selected property starts editing with the typed character
            PropertyGridEntry entry = _grid.SelectedEntry;
            if (!e.Handled && !char.IsControl(e.KeyChar) && entry != null && entry.GridItemType == GridItemType.Property && !entry.IsReadOnly && CanEditText(entry))
            {
                BeginEdit(entry);
                _edit.Text = e.KeyChar.ToString();
                _edit.Select(1, 0);
                e.Handled = true;
            }
        }

        // Editing
        private bool CanEditText(PropertyGridEntry entry)
        {
            try
            {
                return entry.Converter.CanConvertFrom(entry, typeof(string)) && !(entry.HasStandardValues && entry.StandardValuesExclusive);
            }
            catch { return false; }
        }
        internal void BeginEdit(PropertyGridEntry entry)
        {
            EndEdit(false);
            if (entry == null || entry.IsReadOnly || entry.GridItemType != GridItemType.Property) return;
            _editEntry = entry;
            _editing = true;
            bool textEditable = CanEditText(entry);
            _edit.ReadOnly = !textEditable;
            _edit.Font = entry.IsModified ? new Font(_grid.Font, FontStyle.Bold) : _grid.Font;
            _edit.Text = entry.ValueText;
            _edit.BackColor = _grid.ViewBackColor;
            _edit.ForeColor = _grid.ViewForeColor;
            PlaceEditor();
            _edit.Visible = true;
            _edit.Focus();
            _edit.SelectAll();
            Invalidate();
        }
        private void PlaceEditor()
        {
            if (!_editing || _editEntry == null) return;
            int index = _rows.IndexOf(_editEntry);
            if (index < 0)
            {
                EndEdit(true);
                return;
            }
            int h = RowHeight;
            int y = index * h - _scroll.OffsetY;
            int x = LabelWidth + 2;
            UITypeEditor editor = _editEntry.Editor;
            try { if (editor != null && editor.GetPaintValueSupported(_editEntry)) x += 24; } catch { }
            int right = ContentWidth - DropButtonWidth(_editEntry);
            _edit.SetBounds(x, y + 1, Math.Max(10, right - x - 1), h - 2);
        }
        internal void EndEdit(bool cancel)
        {
            if (!_editing) return;
            PropertyGridEntry entry = _editEntry;
            string text = _edit.Text;
            bool readOnly = _edit.ReadOnly;
            _editing = false;
            _editEntry = null;
            _edit.Visible = false;
            if (!cancel && !readOnly && entry != null && text != entry.ValueText) Commit(entry, text);
            Invalidate();
        }
        private void Commit(PropertyGridEntry entry, string text)
        {
            try
            {
                object value = entry.Converter.ConvertFromString(entry, CultureInfo.CurrentCulture, text);
                entry.SetValue(value);
            }
            catch (Exception ex)
            {
                Exception inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                if (_grid.Site?.GetService(typeof(IUIService)) is IUIService ui) ui.ShowError(inner);
                else MessageBox.Show("Property value is not valid." + Environment.NewLine + Environment.NewLine + inner.Message, "Properties Window",
                                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                Invalidate();
            }
        }
        private void Edit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                EndEdit(false);
                Focus();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                EndEdit(true);
                Focus();
                e.Handled = true;
            }
            else if ((e.KeyCode == Keys.Down || e.KeyCode == Keys.Up) && e.Alt && _editEntry != null)
            {
                OpenEditor(_editEntry);
                e.Handled = true;
            }
        }
        // A double click selects the next standard value (true/false, enums)
        private void CycleValue()
        {
            PropertyGridEntry entry = _editEntry ?? _grid.SelectedEntry;
            if (entry == null || entry.IsReadOnly || !entry.HasStandardValues) return;
            IList values = entry.StandardValues;
            if (values.Count == 0) return;
            object current = entry.Value;
            int index = -1;
            for (int i = 0; i < values.Count; i++) if (Equals(values[i], current)) { index = i; break; }
            object next = values[(index + 1) % values.Count];
            EndEdit(true);
            try { entry.SetValue(next); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Properties Window", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        // The button of a property: the list of standard values or the UITypeEditor
        private void OpenEditor(PropertyGridEntry entry)
        {
            EndEdit(false);
            UITypeEditor editor = entry.Editor;
            UITypeEditorEditStyle style = EditStyle(entry);
            if (editor != null && style != UITypeEditorEditStyle.None && !(editor is DynamicStandardValuesMarker))
            {
                object value = entry.Value;
                object newValue;
                try
                {
                    newValue = editor.EditValue(entry, entry, value);
                    // The editors of the macOS application edit through the host (color and flags drop-downs)
                    if (ReferenceEquals(newValue, value) && UITypeEditorHost.Edit != null && IsHostEditor(editor))
                        newValue = UITypeEditorHost.Edit(editor, entry, value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Properties Window", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                if (!Equals(newValue, value) || (newValue != null && !newValue.GetType().IsValueType && !ReferenceEquals(newValue, value)))
                {
                    try { entry.SetValue(newValue); }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Properties Window", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
                else Invalidate();
                return;
            }
            if (entry.HasStandardValues) ShowStandardValues(entry);
        }
        private static bool IsHostEditor(UITypeEditor editor) { return editor.GetType().Name == "ColorEditorEx" || editor.GetType().Name == "ColorEditor"; }
        private void ShowStandardValues(PropertyGridEntry entry)
        {
            IList values = entry.StandardValues;
            ListBox list = new ListBox { BorderStyle = BorderStyle.FixedSingle, Font = _grid.Font };
            object current = entry.Value;
            int selected = -1;
            foreach (object v in values)
            {
                string text;
                try { text = entry.Converter.ConvertToString(entry, CultureInfo.CurrentCulture, v); }
                catch { text = v?.ToString(); }
                list.Items.Add(new StandardValue { Value = v, Text = text });
                if (Equals(v, current)) selected = list.Items.Count - 1;
            }
            int h = RowHeight;
            int rowHeight = Math.Max(16, _grid.Font.Height + 4);
            list.Size = new Size(ContentWidth - LabelWidth, Math.Min(values.Count, 12) * rowHeight + 4);
            if (selected >= 0) list.SelectedIndex = selected;
            object chosen = null;
            bool picked = false;
            list.SelectedIndexChanged += (s, e) =>
            {
                if (!_dropDownOpen || list.SelectedIndex < 0) return;
                chosen = ((StandardValue)list.SelectedItem).Value;
                picked = true;
                CloseDropDown();
            };
            DropDownControl(entry, list);
            if (picked && !Equals(chosen, current))
            {
                try { entry.SetValue(chosen); }
                catch (Exception ex)
                {
                    Exception inner = ex is System.Reflection.TargetInvocationException tie && tie.InnerException != null ? tie.InnerException : ex;
                    MessageBox.Show("Property value is not valid." + Environment.NewLine + Environment.NewLine + inner.Message, "Properties Window",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private sealed class StandardValue
        {
            public object Value;
            public string Text;
            public override string ToString() { return Text; }
        }
        // Shows the control under the value of the entry until CloseDropDown is called or the user clicks outside
        internal void DropDownControl(PropertyGridEntry entry, Control control)
        {
            int index = _rows.IndexOf(entry);
            int h = RowHeight;
            int y = (index < 0 ? 0 : index * h - _scroll.OffsetY) + h;
            int x = LabelWidth;
            Size size = control.Size;
            if (size.Width < ContentWidth - LabelWidth) size.Width = ContentWidth - LabelWidth;
            control.Size = size;
            _dropDown = new Popup
            {
                IsLightDismissEnabled = true,
                Placement = AC.PlacementMode.AnchorAndGravity,
                PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft,
                PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight,
                PlacementTarget = NativeControl,
                PlacementRect = new Avalonia.Rect(x + NonClientInsets.Left, y + NonClientInsets.Top, 1, 1),
            };
            ContainerControl holder = new ContainerControl { Size = size };
            holder.Controls.Add(control);
            control.Location = Point.Empty;
            holder.CreateControl();
            _dropDown.Child = new AC.Border
            {
                Child = holder.NativeControl,
                Width = size.Width,
                Height = size.Height,
                BorderBrush = Avalonia.Media.Brushes.Black,
                BorderThickness = new Avalonia.Thickness(0),
            };
            ((AC.Panel)NativeControl).Children.Add(_dropDown);
            _dropDownOpen = true;
            _dropDown.Closed += (s, e) => _dropDownOpen = false;
            _dropDown.IsOpen = true;
            Dispatcher.UIThread.Post(() => control.Focus(), DispatcherPriority.Input);
            Application.RunModal(() => !_dropDownOpen);
            ((AC.Panel)NativeControl).Children.Remove(_dropDown);
            _dropDown.Child = null;
            _dropDown = null;
            holder.Controls.Remove(control);
            holder.Dispose();
            Focus();
            Invalidate();
        }
        internal void CloseDropDown()
        {
            if (_dropDown != null && _dropDown.IsOpen) _dropDown.IsOpen = false;
            _dropDownOpen = false;
        }
    }

    // The standard values editor of the DynamicTypeDescriptor of PrePoMax opens the list of the grid
    internal interface DynamicStandardValuesMarker { }
}
