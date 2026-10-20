// PrePoMax for macOS - BindingSource of WinForms: a list view over a list, a DataTable or a DataView.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace System.Windows.Forms
{
    [DefaultEvent("CurrentChanged")]
    public class BindingSource : Component, IBindingListView, ITypedList, ICancelAddNew, ISupportInitialize
    {
        private object _dataSource;
        private string _dataMember = string.Empty;
        private IList _list = new List<object>();
        private int _position = -1;
        private string _filter;
        private string _sort;
        private bool _raiseListChanged = true;
        private bool _allowNewSet;
        private bool _allowNew;
        private object _pendingNew;

        public BindingSource() { }
        public BindingSource(IContainer container) : this() { container?.Add(this); }
        public BindingSource(object dataSource, string dataMember) : this()
        {
            _dataMember = dataMember ?? string.Empty;
            DataSource = dataSource;
        }

        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                if (_dataSource is IBindingList oldBinding) oldBinding.ListChanged -= InnerListChanged;
                _dataSource = value;
                _list = ResolveList(value, _dataMember);
                if (_list is IBindingList binding) binding.ListChanged += InnerListChanged;
                ApplyFilterAndSort();
                _position = _list.Count > 0 ? 0 : -1;
                DataSourceChanged?.Invoke(this, EventArgs.Empty);
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
                CurrentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public string DataMember
        {
            get { return _dataMember; }
            set
            {
                _dataMember = value ?? string.Empty;
                DataSource = _dataSource;
                DataMemberChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        private static IList ResolveList(object dataSource, string dataMember)
        {
            if (dataSource == null) return new List<object>();
            if (dataSource is BindingSource bs) return bs;
            if (dataSource is IListSource listSource)
            {
                IList list = listSource.GetList();
                if (!string.IsNullOrEmpty(dataMember) && list is ITypedList typed)
                {
                    PropertyDescriptor member = typed.GetItemProperties(null).Find(dataMember, true);
                    if (member != null && list.Count > 0) return member.GetValue(list[0]) as IList ?? list;
                }
                return list;
            }
            if (!string.IsNullOrEmpty(dataMember))
            {
                PropertyDescriptor member = TypeDescriptor.GetProperties(dataSource).Find(dataMember, true);
                if (member?.GetValue(dataSource) is IList memberList) return memberList;
            }
            if (dataSource is IList ilist) return ilist;
            if (dataSource is IEnumerable enumerable && !(dataSource is string)) return enumerable.Cast<object>().ToList();
            return new List<object> { dataSource };
        }
        public IList List { get { return _list; } }
        public int Count { get { return _list.Count; } }
        public object Current { get { return _position >= 0 && _position < _list.Count ? _list[_position] : null; } }
        public int Position
        {
            get { return _position; }
            set
            {
                int p = Math.Max(-1, Math.Min(value, _list.Count - 1));
                if (p == _position) return;
                _position = p;
                PositionChanged?.Invoke(this, EventArgs.Empty);
                CurrentChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public bool AllowNew
        {
            get
            {
                if (_allowNewSet) return _allowNew;
                if (_list is IBindingList b) return b.AllowNew;
                if (_list.IsFixedSize || _list.IsReadOnly) return false;
                Type type = ItemType;
                return type != null && (AddingNew != null || type.GetConstructor(Type.EmptyTypes) != null);
            }
            set { _allowNewSet = true; _allowNew = value; }
        }
        public bool AllowEdit { get { return !(_list.IsReadOnly); } }
        public bool AllowRemove { get { return !(_list.IsFixedSize || _list.IsReadOnly); } }
        public bool IsBindingSuspended { get; private set; }
        public bool RaiseListChangedEvents { get { return _raiseListChanged; } set { _raiseListChanged = value; } }
        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                ApplyFilterAndSort();
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }
        public string Sort
        {
            get { return _sort; }
            set
            {
                _sort = value;
                ApplyFilterAndSort();
                OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
            }
        }
        private void ApplyFilterAndSort()
        {
            if (_list is System.Data.DataView view)
            {
                view.RowFilter = _filter ?? string.Empty;
                view.Sort = _sort ?? string.Empty;
            }
            else if (_list is IBindingListView blv)
            {
                if (blv.SupportsFiltering) blv.Filter = _filter;
            }
        }
        public void RemoveFilter() { Filter = null; }
        public void RemoveSort() { Sort = null; }
        public Type ItemType
        {
            get
            {
                Type listType = _list.GetType();
                foreach (Type i in listType.GetInterfaces())
                    if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>)) return i.GetGenericArguments()[0];
                if (_list.Count > 0 && _list[0] != null) return _list[0].GetType();
                return null;
            }
        }
        public event EventHandler CurrentChanged;
        public event EventHandler CurrentItemChanged;
        public event EventHandler PositionChanged;
        public event EventHandler DataSourceChanged;
        public event EventHandler DataMemberChanged;
        public event ListChangedEventHandler ListChanged;
        public event AddingNewEventHandler AddingNew;
        public event BindingCompleteEventHandler BindingComplete;
        public event BindingManagerDataErrorEventHandler DataError;
        protected virtual void OnListChanged(ListChangedEventArgs e)
        {
            if (!_raiseListChanged || IsBindingSuspended) return;
            ListChanged?.Invoke(this, e);
        }
        protected virtual void OnAddingNew(AddingNewEventArgs e) { AddingNew?.Invoke(this, e); }
        private void InnerListChanged(object sender, ListChangedEventArgs e)
        {
            if (_position >= _list.Count) _position = _list.Count - 1;
            OnListChanged(e);
        }

        // List
        public object this[int index] { get { return _list[index]; } set { _list[index] = value; Changed(ListChangedType.ItemChanged, index); } }
        public virtual int Add(object value)
        {
            int index;
            if (_dataSource == null && _list.Count == 0 && value != null && !(_list is IList<object>)) _list = new List<object>();
            index = _list.Add(value);
            Changed(ListChangedType.ItemAdded, index);
            return index;
        }
        public virtual object AddNew()
        {
            if (_list is System.Data.DataView view)
            {
                System.Data.DataRowView rowView = view.AddNew();
                rowView.EndEdit();
                Position = view.Count - 1;
                return rowView;
            }
            AddingNewEventArgs e = new AddingNewEventArgs();
            OnAddingNew(e);
            object item = e.NewObject;
            if (item == null)
            {
                if (_list is IBindingList binding && binding.AllowNew)
                {
                    object added = binding.AddNew();
                    Position = _list.IndexOf(added);
                    return added;
                }
                Type type = ItemType;
                if (type == null) throw new InvalidOperationException("The item type of the list is not known.");
                item = Activator.CreateInstance(type);
            }
            int index = _list.Add(item);
            _pendingNew = item;
            Changed(ListChangedType.ItemAdded, index);
            Position = index;
            return item;
        }
        public virtual void Clear()
        {
            _list.Clear();
            _position = -1;
            Changed(ListChangedType.Reset, -1);
        }
        public virtual bool Contains(object value) { return _list.Contains(value); }
        public virtual int IndexOf(object value) { return _list.IndexOf(value); }
        public virtual void Insert(int index, object value) { _list.Insert(index, value); Changed(ListChangedType.ItemAdded, index); }
        public virtual void Remove(object value)
        {
            int index = _list.IndexOf(value);
            if (index >= 0) RemoveAt(index);
        }
        public virtual void RemoveAt(int index)
        {
            if (_list is System.Data.DataView view) view[index].Delete();
            else _list.RemoveAt(index);
            if (_position >= _list.Count) _position = _list.Count - 1;
            Changed(ListChangedType.ItemDeleted, index);
        }
        public void RemoveCurrent() { if (_position >= 0) RemoveAt(_position); }
        public void MoveFirst() { Position = 0; }
        public void MoveLast() { Position = _list.Count - 1; }
        public void MoveNext() { Position = _position + 1; }
        public void MovePrevious() { Position = _position - 1; }
        public void ResetBindings(bool metadataChanged)
        {
            if (metadataChanged) OnListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, (PropertyDescriptor)null));
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        public void ResetCurrentItem() { if (_position >= 0) Changed(ListChangedType.ItemChanged, _position); }
        public void ResetItem(int itemIndex) { Changed(ListChangedType.ItemChanged, itemIndex); }
        public void ResetAllowNew() { _allowNewSet = false; }
        public void SuspendBinding() { IsBindingSuspended = true; }
        public void ResumeBinding()
        {
            IsBindingSuspended = false;
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        public void EndEdit() { _pendingNew = null; }
        public void CancelEdit() { }
        public void CancelNew(int itemIndex)
        {
            if (_pendingNew != null && itemIndex >= 0 && itemIndex < _list.Count && ReferenceEquals(_list[itemIndex], _pendingNew))
            {
                _pendingNew = null;
                RemoveAt(itemIndex);
            }
        }
        public void EndNew(int itemIndex) { _pendingNew = null; }
        public int Find(string propertyName, object key)
        {
            PropertyDescriptor pd = GetItemProperties(null).Find(propertyName, true);
            if (pd == null) return -1;
            for (int i = 0; i < _list.Count; i++) if (Equals(pd.GetValue(_list[i]), key)) return i;
            return -1;
        }
        private void Changed(ListChangedType type, int index)
        {
            OnListChanged(new ListChangedEventArgs(type, index));
            if (type == ListChangedType.ItemChanged && index == _position) CurrentItemChanged?.Invoke(this, EventArgs.Empty);
        }
        public void BeginInit() { }
        public void EndInit() { }

        // ITypedList
        public PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            if (_list is ITypedList typed) return typed.GetItemProperties(listAccessors);
            Type type = ItemType;
            if (type != null) return TypeDescriptor.GetProperties(type);
            return new PropertyDescriptorCollection(new PropertyDescriptor[0]);
        }
        public string GetListName(PropertyDescriptor[] listAccessors) { return _dataMember; }

        // IList, IBindingListView
        public bool IsFixedSize { get { return _list.IsFixedSize; } }
        public bool IsReadOnly { get { return _list.IsReadOnly; } }
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return this; } }
        public void CopyTo(Array arr, int index) { _list.CopyTo(arr, index); }
        public IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
        public bool SupportsChangeNotification { get { return true; } }
        public bool SupportsSearching { get { return true; } }
        public bool SupportsSorting { get { return _list is IBindingList b && b.SupportsSorting; } }
        public bool SupportsFiltering { get { return _list is IBindingListView v && v.SupportsFiltering; } }
        public bool SupportsAdvancedSorting { get { return false; } }
        public bool IsSorted { get { return _list is IBindingList b && b.IsSorted; } }
        public PropertyDescriptor SortProperty { get { return (_list as IBindingList)?.SortProperty; } }
        public ListSortDirection SortDirection { get { return (_list as IBindingList)?.SortDirection ?? ListSortDirection.Ascending; } }
        public ListSortDescriptionCollection SortDescriptions { get { return (_list as IBindingListView)?.SortDescriptions; } }
        public void ApplySort(PropertyDescriptor property, ListSortDirection sort)
        {
            if (_list is IBindingList b && b.SupportsSorting) b.ApplySort(property, sort);
            OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }
        public void ApplySort(ListSortDescriptionCollection sorts) { (_list as IBindingListView)?.ApplySort(sorts); }
        public int Find(PropertyDescriptor prop, object key)
        {
            for (int i = 0; i < _list.Count; i++) if (Equals(prop.GetValue(_list[i]), key)) return i;
            return -1;
        }
        public void AddIndex(PropertyDescriptor property) { }
        public void RemoveIndex(PropertyDescriptor property) { }
        void IBindingList.RemoveSort() { RemoveSort(); }
        void IBindingListView.RemoveFilter() { RemoveFilter(); }
        bool IBindingList.AllowNew { get { return AllowNew; } }
        object IBindingList.AddNew() { return AddNew(); }
    }

    public class BindingCompleteEventArgs : CancelEventArgs { }

    public delegate void BindingCompleteEventHandler(object sender, BindingCompleteEventArgs e);

    public class BindingManagerDataErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public BindingManagerDataErrorEventArgs(Exception exception) { Exception = exception; }
    }

    public delegate void BindingManagerDataErrorEventHandler(object sender, BindingManagerDataErrorEventArgs e);
}
