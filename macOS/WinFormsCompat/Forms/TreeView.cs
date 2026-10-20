// PrePoMax for macOS - TreeView and TreeNode, drawn like the classic WinForms tree (dotted lines and plus/minus boxes).

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Avalonia.Threading;

namespace System.Windows.Forms
{
    [Serializable]
    public class TreeNode : MarshalByRefObject, ICloneable
    {
        private string _text = string.Empty;
        private TreeNodeCollection _nodes;
        internal TreeNode ParentInternal;
        internal TreeView TreeViewInternal;
        internal bool ExpandedInternal;
        private Color _backColor = Color.Empty;
        private Color _foreColor = Color.Empty;
        private Font _nodeFont;
        private bool _checked;
        internal Rectangle RowBounds;       // in content coordinates (set by the tree layout)
        internal int TextWidth;

        public TreeNode() { }
        public TreeNode(string text) { _text = text ?? string.Empty; }
        public TreeNode(string text, TreeNode[] children) : this(text) { Nodes.AddRange(children); }
        public TreeNode(string text, int imageIndex, int selectedImageIndex) : this(text)
        {
            ImageIndex = imageIndex;
            SelectedImageIndex = selectedImageIndex;
        }
        public TreeNode(string text, int imageIndex, int selectedImageIndex, TreeNode[] children) : this(text, children)
        {
            ImageIndex = imageIndex;
            SelectedImageIndex = selectedImageIndex;
        }

        public string Text { get { return _text; } set { _text = value ?? string.Empty; Changed(true); } }
        public string Name { get; set; } = string.Empty;
        public object Tag { get; set; }
        public string ToolTipText { get; set; }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; } = string.Empty;
        public int SelectedImageIndex { get; set; } = -1;
        public string SelectedImageKey { get; set; } = string.Empty;
        public int StateImageIndex { get; set; } = -1;
        public string StateImageKey { get; set; } = string.Empty;
        public ContextMenuStrip ContextMenuStrip { get; set; }
        public Color BackColor { get { return _backColor; } set { _backColor = value; Changed(false); } }
        public Color ForeColor { get { return _foreColor; } set { _foreColor = value; Changed(false); } }
        public Font NodeFont { get { return _nodeFont; } set { _nodeFont = value; Changed(true); } }
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                TreeView tv = TreeView;
                if (tv != null && !tv.RaiseBeforeCheck(this, TreeViewAction.Unknown)) return;
                _checked = value;
                Changed(false);
                tv?.RaiseAfterCheck(this, TreeViewAction.Unknown);
            }
        }
        public TreeNodeCollection Nodes { get { return _nodes ?? (_nodes = new TreeNodeCollection(this, null)); } }
        public TreeNode Parent { get { return ParentInternal; } }
        public TreeView TreeView
        {
            get
            {
                TreeNode n = this;
                while (n.ParentInternal != null) n = n.ParentInternal;
                return n.TreeViewInternal;
            }
        }
        public int Level { get { int l = 0; for (TreeNode n = ParentInternal; n != null; n = n.ParentInternal) l++; return l; } }
        public int Index
        {
            get
            {
                if (ParentInternal != null) return ParentInternal.Nodes.IndexOf(this);
                return TreeViewInternal?.Nodes.IndexOf(this) ?? -1;
            }
        }
        public bool IsExpanded { get { return ExpandedInternal; } }
        public bool IsSelected { get { TreeView tv = TreeView; return tv != null && tv.SelectedNodeInternal == this; } }
        public bool IsEditing { get { TreeView tv = TreeView; return tv != null && tv.EditingNode == this; } }
        public bool IsVisible
        {
            get
            {
                TreeView tv = TreeView;
                if (tv == null) return false;
                for (TreeNode p = ParentInternal; p != null; p = p.ParentInternal) if (!p.ExpandedInternal) return false;
                Rectangle b = Bounds;
                return b.Bottom > 0 && b.Top < tv.ClientSize.Height;
            }
        }
        public string FullPath
        {
            get
            {
                TreeView tv = TreeView;
                string separator = tv?.PathSeparator ?? "\\";
                return ParentInternal == null ? _text : ParentInternal.FullPath + separator + _text;
            }
        }
        public Rectangle Bounds
        {
            get
            {
                TreeView tv = TreeView;
                if (tv == null) return Rectangle.Empty;
                tv.EnsureLayout();
                return tv.LabelBounds(this);
            }
        }
        public TreeNode FirstNode { get { return _nodes != null && _nodes.Count > 0 ? _nodes[0] : null; } }
        public TreeNode LastNode { get { return _nodes != null && _nodes.Count > 0 ? _nodes[_nodes.Count - 1] : null; } }
        public TreeNode NextNode
        {
            get
            {
                TreeNodeCollection siblings = Siblings;
                int i = siblings?.IndexOf(this) ?? -1;
                return i >= 0 && i + 1 < siblings.Count ? siblings[i + 1] : null;
            }
        }
        public TreeNode PrevNode
        {
            get
            {
                TreeNodeCollection siblings = Siblings;
                int i = siblings?.IndexOf(this) ?? -1;
                return i > 0 ? siblings[i - 1] : null;
            }
        }
        public TreeNode NextVisibleNode
        {
            get
            {
                TreeView tv = TreeView;
                if (tv == null) return null;
                List<TreeNode> visible = tv.VisibleNodes();
                int i = visible.IndexOf(this);
                return i >= 0 && i + 1 < visible.Count ? visible[i + 1] : null;
            }
        }
        public TreeNode PrevVisibleNode
        {
            get
            {
                TreeView tv = TreeView;
                if (tv == null) return null;
                List<TreeNode> visible = tv.VisibleNodes();
                int i = visible.IndexOf(this);
                return i > 0 ? visible[i - 1] : null;
            }
        }
        private TreeNodeCollection Siblings { get { return ParentInternal != null ? ParentInternal.Nodes : TreeViewInternal?.Nodes; } }

        public void Expand()
        {
            TreeView tv = TreeView;
            if (ExpandedInternal) return;
            if (tv != null && !tv.RaiseBeforeExpand(this)) return;
            ExpandedInternal = true;
            if (tv != null)
            {
                tv.LayoutChanged();
                tv.RaiseAfterExpand(this);
            }
        }
        public void ExpandAll()
        {
            Expand();
            if (_nodes != null) foreach (TreeNode n in _nodes.List.ToArray()) n.ExpandAll();
        }
        public void Collapse() { Collapse(true); }
        public void Collapse(bool ignoreChildren)
        {
            if (!ignoreChildren && _nodes != null) foreach (TreeNode n in _nodes.List.ToArray()) n.Collapse(false);
            if (!ExpandedInternal) return;
            TreeView tv = TreeView;
            if (tv != null && !tv.RaiseBeforeCollapse(this)) return;
            ExpandedInternal = false;
            if (tv != null)
            {
                if (tv.SelectedNodeInternal != null && IsAncestorOf(tv.SelectedNodeInternal)) tv.SelectedNode = this;
                tv.LayoutChanged();
                tv.RaiseAfterCollapse(this);
            }
        }
        public void Toggle()
        {
            if (ExpandedInternal) Collapse();
            else Expand();
        }
        public void EnsureVisible()
        {
            for (TreeNode p = ParentInternal; p != null; p = p.ParentInternal) if (!p.ExpandedInternal) p.Expand();
            TreeView?.ScrollIntoView(this);
        }
        public void BeginEdit() { TreeView?.BeginEditNode(this); }
        public void EndEdit(bool cancel) { TreeView?.EndEditNode(cancel); }
        public void Remove()
        {
            if (ParentInternal != null) ParentInternal.Nodes.Remove(this);
            else TreeViewInternal?.Nodes.Remove(this);
        }
        public int GetNodeCount(bool includeSubTrees)
        {
            if (_nodes == null) return 0;
            int count = _nodes.Count;
            if (includeSubTrees) foreach (TreeNode n in _nodes.List) count += n.GetNodeCount(true);
            return count;
        }
        internal bool IsAncestorOf(TreeNode node)
        {
            for (TreeNode p = node?.ParentInternal; p != null; p = p.ParentInternal) if (p == this) return true;
            return false;
        }
        internal bool HasChildren { get { return _nodes != null && _nodes.Count > 0; } }
        internal void Changed(bool layout)
        {
            TreeView tv = TreeView;
            if (tv == null) return;
            if (layout) tv.LayoutChanged();
            else tv.Invalidate();
        }
        public virtual object Clone()
        {
            TreeNode clone = (TreeNode)Activator.CreateInstance(GetType());
            clone._text = _text;
            clone.Name = Name;
            clone.Tag = Tag;
            clone.ImageIndex = ImageIndex;
            clone.ImageKey = ImageKey;
            clone.SelectedImageIndex = SelectedImageIndex;
            clone.SelectedImageKey = SelectedImageKey;
            clone._backColor = _backColor;
            clone._foreColor = _foreColor;
            clone._nodeFont = _nodeFont;
            clone._checked = _checked;
            clone.ToolTipText = ToolTipText;
            if (_nodes != null) foreach (TreeNode n in _nodes.List) clone.Nodes.Add((TreeNode)n.Clone());
            return clone;
        }
        public override string ToString() { return "TreeNode: " + _text; }
    }

    public class TreeNodeCollection : IList
    {
        private readonly TreeNode _ownerNode;
        private readonly TreeView _ownerView;
        private readonly List<TreeNode> _list = new List<TreeNode>();
        internal TreeNodeCollection(TreeNode ownerNode, TreeView ownerView)
        {
            _ownerNode = ownerNode;
            _ownerView = ownerView;
        }
        internal List<TreeNode> List { get { return _list; } }
        private TreeView View { get { return _ownerView ?? _ownerNode?.TreeView; } }
        public int Count { get { return _list.Count; } }
        public bool IsReadOnly { get { return false; } }
        public virtual TreeNode this[int index]
        {
            get { return _list[index]; }
            set
            {
                RemoveAt(index);
                Insert(index, value);
            }
        }
        public virtual TreeNode this[string key] { get { int i = IndexOfKey(key); return i < 0 ? null : _list[i]; } }
        public virtual TreeNode Add(string text) { TreeNode n = new TreeNode(text); Add(n); return n; }
        public virtual TreeNode Add(string key, string text) { TreeNode n = new TreeNode(text) { Name = key }; Add(n); return n; }
        public virtual TreeNode Add(string key, string text, int imageIndex) { TreeNode n = Add(key, text); n.ImageIndex = imageIndex; return n; }
        public virtual TreeNode Add(string key, string text, string imageKey) { TreeNode n = Add(key, text); n.ImageKey = imageKey; return n; }
        public virtual TreeNode Add(string key, string text, int imageIndex, int selectedImageIndex)
        {
            TreeNode n = Add(key, text, imageIndex);
            n.SelectedImageIndex = selectedImageIndex;
            return n;
        }
        public virtual TreeNode Add(string key, string text, string imageKey, string selectedImageKey)
        {
            TreeNode n = Add(key, text, imageKey);
            n.SelectedImageKey = selectedImageKey;
            return n;
        }
        public virtual int Add(TreeNode node)
        {
            Insert(_list.Count, node);
            return _list.Count - 1;
        }
        public virtual void AddRange(TreeNode[] nodes)
        {
            TreeView view = View;
            view?.BeginUpdate();
            foreach (TreeNode n in nodes) Add(n);
            view?.EndUpdate();
        }
        public virtual void Insert(int index, TreeNode node)
        {
            if (node == null) return;
            if (node.ParentInternal != null || node.TreeViewInternal != null) node.Remove();
            node.ParentInternal = _ownerNode;
            node.TreeViewInternal = _ownerView;
            _list.Insert(Math.Max(0, Math.Min(index, _list.Count)), node);
            TreeView view = View;
            if (view != null)
            {
                if (view.Sorted) view.SortCollection(this);
                view.LayoutChanged();
            }
        }
        public virtual TreeNode Insert(int index, string text) { TreeNode n = new TreeNode(text); Insert(index, n); return n; }
        public virtual TreeNode Insert(int index, string key, string text) { TreeNode n = new TreeNode(text) { Name = key }; Insert(index, n); return n; }
        public virtual TreeNode Insert(int index, string key, string text, string imageKey, string selectedImageKey)
        {
            TreeNode n = new TreeNode(text) { Name = key, ImageKey = imageKey, SelectedImageKey = selectedImageKey };
            Insert(index, n);
            return n;
        }
        public void Remove(TreeNode node)
        {
            int i = _list.IndexOf(node);
            if (i >= 0) RemoveAt(i);
        }
        public virtual void RemoveAt(int index)
        {
            TreeNode node = _list[index];
            TreeView view = View;
            _list.RemoveAt(index);
            node.ParentInternal = null;
            node.TreeViewInternal = null;
            if (view != null)
            {
                view.NodeRemoved(node);
                view.LayoutChanged();
            }
        }
        public virtual void RemoveByKey(string key) { int i = IndexOfKey(key); if (i >= 0) RemoveAt(i); }
        public virtual void Clear()
        {
            TreeView view = View;
            foreach (TreeNode node in _list)
            {
                node.ParentInternal = null;
                node.TreeViewInternal = null;
                view?.NodeRemoved(node);
            }
            _list.Clear();
            view?.LayoutChanged();
        }
        public bool Contains(TreeNode node) { return _list.Contains(node); }
        public virtual bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
        public int IndexOf(TreeNode node) { return _list.IndexOf(node); }
        public virtual int IndexOfKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return -1;
            for (int i = 0; i < _list.Count; i++) if (string.Equals(_list[i].Name, key, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        public TreeNode[] Find(string key, bool searchAllChildren)
        {
            List<TreeNode> found = new List<TreeNode>();
            foreach (TreeNode n in _list)
            {
                if (string.Equals(n.Name, key, StringComparison.OrdinalIgnoreCase)) found.Add(n);
                if (searchAllChildren && n.HasChildren) found.AddRange(n.Nodes.Find(key, true));
            }
            return found.ToArray();
        }
        public void CopyTo(Array dest, int index) { ((ICollection)_list).CopyTo(dest, index); }
        public IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
        int IList.Add(object node) { return node is TreeNode n ? Add(n) : Add(node?.ToString()).Index; }
        bool IList.Contains(object node) { return node is TreeNode n && Contains(n); }
        int IList.IndexOf(object node) { return node is TreeNode n ? IndexOf(n) : -1; }
        void IList.Insert(int index, object node) { Insert(index, (TreeNode)node); }
        void IList.Remove(object node) { if (node is TreeNode n) Remove(n); }
        bool IList.IsFixedSize { get { return false; } }
        object IList.this[int index] { get { return this[index]; } set { this[index] = (TreeNode)value; } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
    }

    public class TreeView : Control
    {
        private readonly TreeNodeCollection _nodes;
        private readonly ScrollHelper _scroll;
        internal TreeNode SelectedNodeInternal;
        private TreeNode _hotNode;
        private ImageList _imageList;
        private ImageList _stateImageList;
        private int _itemHeight = -1;
        private int _indent = 19;
        private bool _layoutValid;
        private int _updateCount;
        private readonly List<TreeNode> _rows = new List<TreeNode>();
        private TreeNode _mouseDownNode;
        private Point _mouseDownPoint;
        private bool _dragStarted;
        private TextBox _editor;
        internal TreeNode EditingNode;
        private BorderStyle _borderStyle = BorderStyle.Fixed3D;

        public TreeView()
        {
            _nodes = new TreeNodeCollection(null, this);
            _scroll = new ScrollHelper(this, () => ClientRectangle);
            _scroll.Scrolled += () =>
            {
                FilterMessage(0x115, new IntPtr(5), IntPtr.Zero);
                if (_editor != null) PlaceEditor();
            };
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
            SetClientSizeOffset(BorderSizes.Of(_borderStyle));
        }
        protected override Size DefaultSize { get { return new Size(121, 97); } }
        public TreeNodeCollection Nodes { get { return _nodes; } }
        public override Color BackColor { get { return BackColorSet ? base.BackColor : SystemColors.Window; } set { base.BackColor = value; } }
        public override Color ForeColor { get { return ForeColorSet ? base.ForeColor : SystemColors.WindowText; } set { base.ForeColor = value; } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set { _borderStyle = value; SetClientSizeOffset(BorderSizes.Of(value)); LayoutChanged(); }
        }
        public ImageList ImageList { get { return _imageList; } set { _imageList = value; LayoutChanged(); } }
        public ImageList StateImageList { get { return _stateImageList; } set { _stateImageList = value; LayoutChanged(); } }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; } = string.Empty;
        public int SelectedImageIndex { get; set; } = -1;
        public string SelectedImageKey { get; set; } = string.Empty;
        public int ItemHeight
        {
            get { return _itemHeight > 0 ? _itemHeight : Math.Max(16, Font.Height + 3); }
            set { _itemHeight = value; LayoutChanged(); }
        }
        public int Indent { get { return _indent; } set { _indent = value; LayoutChanged(); } }
        public bool ShowLines { get; set; } = true;
        public bool ShowPlusMinus { get; set; } = true;
        public bool ShowRootLines { get; set; } = true;
        public bool ShowNodeToolTips { get; set; }
        public bool FullRowSelect { get; set; }
        public bool HideSelection { get; set; } = true;
        public bool HotTracking { get; set; }
        public bool LabelEdit { get; set; }
        public bool CheckBoxes { get; set; }
        public bool Scrollable { get; set; } = true;
        public bool Sorted { get; set; }
        public TreeViewDrawMode DrawMode { get; set; }
        public Color LineColor { get; set; } = Color.FromArgb(0xA0, 0xA0, 0xA0);
        public string PathSeparator { get; set; } = "\\";
        public IComparer TreeViewNodeSorter { get; set; }
        public bool RightToLeftLayout { get; set; }
        public int VisibleCount { get { return Math.Max(1, ClientSize.Height / ItemHeight); } }
        public TreeNode TopNode
        {
            get
            {
                EnsureLayout();
                int index = _scroll.OffsetY / ItemHeight;
                return index >= 0 && index < _rows.Count ? _rows[index] : null;
            }
            set { if (value != null) { EnsureLayout(); int i = _rows.IndexOf(value); if (i >= 0) _scroll.SetOffset(_scroll.OffsetX, i * ItemHeight); } }
        }
        public TreeNode SelectedNode
        {
            get { return SelectedNodeInternal; }
            set { SelectNode(value, TreeViewAction.Unknown); }
        }

        public event TreeViewCancelEventHandler BeforeSelect;
        public event TreeViewEventHandler AfterSelect;
        public event TreeViewCancelEventHandler BeforeExpand;
        public event TreeViewEventHandler AfterExpand;
        public event TreeViewCancelEventHandler BeforeCollapse;
        public event TreeViewEventHandler AfterCollapse;
        public event TreeViewCancelEventHandler BeforeCheck;
        public event TreeViewEventHandler AfterCheck;
        public event TreeNodeMouseClickEventHandler NodeMouseClick;
        public event TreeNodeMouseClickEventHandler NodeMouseDoubleClick;
        public event TreeNodeMouseHoverEventHandler NodeMouseHover;
        public event ItemDragEventHandler ItemDrag;
        public event NodeLabelEditEventHandler BeforeLabelEdit;
        public event NodeLabelEditEventHandler AfterLabelEdit;
        public event DrawTreeNodeEventHandler DrawNode;

        protected virtual void OnBeforeSelect(TreeViewCancelEventArgs e) { BeforeSelect?.Invoke(this, e); }
        protected virtual void OnAfterSelect(TreeViewEventArgs e) { AfterSelect?.Invoke(this, e); }
        protected virtual void OnBeforeExpand(TreeViewCancelEventArgs e) { BeforeExpand?.Invoke(this, e); }
        protected virtual void OnAfterExpand(TreeViewEventArgs e) { AfterExpand?.Invoke(this, e); }
        protected virtual void OnBeforeCollapse(TreeViewCancelEventArgs e) { BeforeCollapse?.Invoke(this, e); }
        protected virtual void OnAfterCollapse(TreeViewEventArgs e) { AfterCollapse?.Invoke(this, e); }
        protected virtual void OnBeforeCheck(TreeViewCancelEventArgs e) { BeforeCheck?.Invoke(this, e); }
        protected virtual void OnAfterCheck(TreeViewEventArgs e) { AfterCheck?.Invoke(this, e); }
        protected virtual void OnNodeMouseClick(TreeNodeMouseClickEventArgs e) { NodeMouseClick?.Invoke(this, e); }
        protected virtual void OnNodeMouseDoubleClick(TreeNodeMouseClickEventArgs e) { NodeMouseDoubleClick?.Invoke(this, e); }
        protected virtual void OnNodeMouseHover(TreeNodeMouseHoverEventArgs e) { NodeMouseHover?.Invoke(this, e); }
        protected virtual void OnItemDrag(ItemDragEventArgs e) { ItemDrag?.Invoke(this, e); }
        protected virtual void OnBeforeLabelEdit(NodeLabelEditEventArgs e) { BeforeLabelEdit?.Invoke(this, e); }
        protected virtual void OnAfterLabelEdit(NodeLabelEditEventArgs e) { AfterLabelEdit?.Invoke(this, e); }
        protected virtual void OnDrawNode(DrawTreeNodeEventArgs e) { DrawNode?.Invoke(this, e); }
        internal bool RaiseBeforeExpand(TreeNode node)
        {
            TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(node, false, TreeViewAction.Expand);
            OnBeforeExpand(e);
            return !e.Cancel;
        }
        internal void RaiseAfterExpand(TreeNode node) { OnAfterExpand(new TreeViewEventArgs(node, TreeViewAction.Expand)); }
        internal bool RaiseBeforeCollapse(TreeNode node)
        {
            TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(node, false, TreeViewAction.Collapse);
            OnBeforeCollapse(e);
            return !e.Cancel;
        }
        internal void RaiseAfterCollapse(TreeNode node) { OnAfterCollapse(new TreeViewEventArgs(node, TreeViewAction.Collapse)); }
        internal bool RaiseBeforeCheck(TreeNode node, TreeViewAction action)
        {
            TreeViewCancelEventArgs e = new TreeViewCancelEventArgs(node, false, action);
            OnBeforeCheck(e);
            return !e.Cancel;
        }
        internal void RaiseAfterCheck(TreeNode node, TreeViewAction action) { OnAfterCheck(new TreeViewEventArgs(node, action)); }

        // Methods
        public void BeginUpdate() { _updateCount++; }
        public void EndUpdate()
        {
            if (_updateCount > 0) _updateCount--;
            if (_updateCount == 0) LayoutChanged();
        }
        public void ExpandAll() { BeginUpdate(); foreach (TreeNode n in _nodes.List.ToArray()) n.ExpandAll(); EndUpdate(); }
        public void CollapseAll() { BeginUpdate(); foreach (TreeNode n in _nodes.List.ToArray()) n.Collapse(false); EndUpdate(); }
        public TreeNode GetNodeAt(Point pt) { return GetNodeAt(pt.X, pt.Y); }
        public TreeNode GetNodeAt(int x, int y)
        {
            EnsureLayout();
            int index = (y + _scroll.OffsetY) / ItemHeight;
            if (y < 0 || index < 0 || index >= _rows.Count) return null;
            return _rows[index];
        }
        public TreeViewHitTestInfo HitTest(Point pt) { return HitTest(pt.X, pt.Y); }
        public TreeViewHitTestInfo HitTest(int x, int y)
        {
            TreeNode node = GetNodeAt(x, y);
            if (node == null) return new TreeViewHitTestInfo(null, y < 0 ? TreeViewHitTestLocations.AboveClientArea : TreeViewHitTestLocations.None);
            int level = node.Level + (ShowRootLines ? 1 : 0);
            int cx = x + _scroll.OffsetX;
            int plusRight = level * _indent;
            Rectangle label = LabelBounds(node);
            TreeViewHitTestLocations location;
            if (cx < plusRight - _indent) location = TreeViewHitTestLocations.Indent;
            else if (cx < plusRight) location = node.HasChildren ? TreeViewHitTestLocations.PlusMinus : TreeViewHitTestLocations.Indent;
            else if (x < label.Left) location = HasImages ? TreeViewHitTestLocations.Image : TreeViewHitTestLocations.Indent;
            else if (x < label.Right) location = TreeViewHitTestLocations.Label;
            else location = TreeViewHitTestLocations.RightOfLabel;
            return new TreeViewHitTestInfo(node, location);
        }
        public int GetNodeCount(bool includeSubTrees)
        {
            int count = _nodes.Count;
            if (includeSubTrees) foreach (TreeNode n in _nodes.List) count += n.GetNodeCount(true);
            return count;
        }
        public void Sort()
        {
            Sorted = true;
            SortRecursive(_nodes);
            LayoutChanged();
        }
        private void SortRecursive(TreeNodeCollection nodes)
        {
            SortCollection(nodes);
            foreach (TreeNode n in nodes.List) if (n.HasChildren) SortRecursive(n.Nodes);
        }
        internal void SortCollection(TreeNodeCollection nodes)
        {
            if (TreeViewNodeSorter != null) nodes.List.Sort((a, b) => TreeViewNodeSorter.Compare(a, b));
            else nodes.List.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));
        }
        internal void NodeRemoved(TreeNode node)
        {
            if (SelectedNodeInternal != null && (SelectedNodeInternal == node || node.IsAncestorOf(SelectedNodeInternal))) SelectedNodeInternal = null;
            if (_hotNode == node) _hotNode = null;
            if (EditingNode == node) EndEditNode(true);
        }
        internal void SelectNode(TreeNode node, TreeViewAction action)
        {
            if (node == SelectedNodeInternal) return;
            TreeViewCancelEventArgs before = new TreeViewCancelEventArgs(node, false, action);
            OnBeforeSelect(before);
            if (before.Cancel) return;
            SelectedNodeInternal = node;
            if (node != null) ScrollIntoView(node);
            Invalidate();
            OnAfterSelect(new TreeViewEventArgs(node, action));
        }

        // Layout
        internal void LayoutChanged()
        {
            _layoutValid = false;
            if (_updateCount > 0) return;
            EnsureLayout();
            Invalidate();
        }
        internal void EnsureLayout()
        {
            if (_layoutValid) return;
            _layoutValid = true;
            _rows.Clear();
            int maxWidth = 0;
            int h = ItemHeight;
            AddRows(_nodes, 0, ref maxWidth, h);
            _scroll.SmallChangeY = h;
            if (Scrollable) _scroll.SetExtent(maxWidth + 4, _rows.Count * h);
        }
        private void AddRows(TreeNodeCollection nodes, int level, ref int maxWidth, int h)
        {
            foreach (TreeNode node in nodes.List)
            {
                node.RowBounds = new Rectangle(0, _rows.Count * h, 0, h);
                Font font = node.NodeFont ?? Font;
                node.TextWidth = ContentLayout.MeasureText(node.Text, font).Width + 4;
                int x = TextLeft(node, level);
                maxWidth = Math.Max(maxWidth, x + node.TextWidth);
                _rows.Add(node);
                if (node.ExpandedInternal && node.HasChildren) AddRows(node.Nodes, level + 1, ref maxWidth, h);
            }
        }
        internal List<TreeNode> VisibleNodes()
        {
            EnsureLayout();
            return new List<TreeNode>(_rows);
        }
        private bool HasImages { get { return _imageList != null && _imageList.Images.Count > 0; } }
        // X of the label of a node in content coordinates
        private int TextLeft(TreeNode node, int level)
        {
            int x = (level + (ShowRootLines ? 1 : 0)) * _indent + 3;
            if (CheckBoxes) x += 16;
            if (HasImages) x += _imageList.ImageSize.Width + 3;
            return x;
        }
        internal Rectangle LabelBounds(TreeNode node)
        {
            int x = TextLeft(node, node.Level) - _scroll.OffsetX;
            int y = node.RowBounds.Y - _scroll.OffsetY;
            return new Rectangle(x, y, node.TextWidth, node.RowBounds.Height);
        }
        internal void ScrollIntoView(TreeNode node)
        {
            EnsureLayout();
            _scroll.EnsureVisible(node.RowBounds);
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            _layoutValid = false;
            EnsureLayout();
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            LayoutChanged();
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
            using (SolidBrush back = new SolidBrush(BackColor)) g.FillRectangle(back, ClientRectangle);
            int h = ItemHeight;
            int first = Math.Max(0, _scroll.OffsetY / h);
            int last = Math.Min(_rows.Count - 1, (_scroll.OffsetY + ClientSize.Height) / h + 1);
            using (Pen linePen = new Pen(LineColor) { DashStyle = DashStyle.Dot })
            {
                for (int i = first; i <= last; i++)
                {
                    TreeNode node = _rows[i];
                    DrawRow(g, node, i, linePen);
                }
            }
            base.OnPaint(e);
        }
        private void DrawRow(Graphics g, TreeNode node, int rowIndex, Pen linePen)
        {
            int h = ItemHeight;
            int level = node.Level;
            int y = rowIndex * h - _scroll.OffsetY;
            int ox = -_scroll.OffsetX;
            int baseLevel = ShowRootLines ? 1 : 0;
            int mid = y + h / 2;
            Rectangle label = LabelBounds(node);
            bool selected = node == SelectedNodeInternal;
            bool focused = ContainsFocus;
            if (FullRowSelect && selected && (focused || !HideSelection))
            {
                using (SolidBrush b = new SolidBrush(focused ? SystemColors.Highlight : SystemColors.Control)) g.FillRectangle(b, 0, y, ClientSize.Width, h);
            }
            if (DrawMode == TreeViewDrawMode.OwnerDrawAll && DrawNode != null)
            {
                DrawTreeNodeEventArgs args = new DrawTreeNodeEventArgs(g, node, new Rectangle(0, y, ClientSize.Width, h), selected ? TreeNodeStates.Selected : 0);
                OnDrawNode(args);
                if (!args.DrawDefault) return;
            }
            // Lines
            if (ShowLines)
            {
                int x = (level + baseLevel) * _indent - _indent / 2 + ox;
                if (level + baseLevel > 0)
                {
                    bool hasPrev = node.PrevNode != null || node.ParentInternal != null;
                    bool hasNext = node.NextNode != null;
                    g.DrawLine(linePen, x, hasPrev ? y : mid, x, hasNext ? y + h : mid);
                    g.DrawLine(linePen, x, mid, x + _indent / 2 + 2, mid);
                }
                // Vertical lines of the ancestors that have next siblings
                TreeNode ancestor = node.ParentInternal;
                int alevel = level - 1;
                while (ancestor != null)
                {
                    if (ancestor.NextNode != null && alevel + baseLevel > 0)
                    {
                        int ax = (alevel + baseLevel) * _indent - _indent / 2 + ox;
                        g.DrawLine(linePen, ax, y, ax, y + h);
                    }
                    ancestor = ancestor.ParentInternal;
                    alevel--;
                }
            }
            // Plus and minus
            if (ShowPlusMinus && node.HasChildren && level + baseLevel > 0)
            {
                int cx = (level + baseLevel) * _indent - _indent / 2 + ox;
                Rectangle box = new Rectangle(cx - 4, mid - 4, 8, 8);
                using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, box);
                using (Pen border = new Pen(Color.FromArgb(0x91, 0x91, 0x91))) g.DrawRectangle(border, box);
                using (Pen sign = new Pen(Color.Black))
                {
                    g.DrawLine(sign, cx - 2, mid, cx + 2, mid);
                    if (!node.ExpandedInternal) g.DrawLine(sign, cx, mid - 2, cx, mid + 2);
                }
            }
            // Check box
            int imageX = label.Left - (HasImages ? _imageList.ImageSize.Width + 3 : 0);
            if (CheckBoxes)
            {
                Rectangle check = new Rectangle(imageX - 16, mid - 6, 13, 13);
                using (SolidBrush white = new SolidBrush(Color.White)) g.FillRectangle(white, check);
                using (Pen border = new Pen(Color.FromArgb(0x33, 0x33, 0x33))) g.DrawRectangle(border, check.X, check.Y, check.Width - 1, check.Height - 1);
                if (node.Checked)
                {
                    using (Pen mark = new Pen(Color.Black, 1.5f))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawLines(mark, new[] { new PointF(check.X + 3, check.Y + 6), new PointF(check.X + 5, check.Y + 9), new PointF(check.X + 10, check.Y + 3) });
                        g.SmoothingMode = SmoothingMode.None;
                    }
                }
            }
            // Image
            if (HasImages)
            {
                Image image = NodeImage(node, selected);
                if (image != null)
                {
                    Size size = _imageList.ImageSize;
                    g.DrawImage(image, new Rectangle(imageX, mid - size.Height / 2, size.Width, size.Height));
                }
            }
            // Text
            Color back = node.BackColor;
            Color fore = node.ForeColor.IsEmpty ? ForeColor : node.ForeColor;
            if (selected && !FullRowSelect && (focused || !HideSelection) && node.BackColor.IsEmpty)
            {
                back = focused ? SystemColors.Highlight : SystemColors.Control;
                if (focused && node.ForeColor.IsEmpty) fore = SystemColors.HighlightText;
            }
            else if (selected && FullRowSelect && focused && node.ForeColor.IsEmpty) fore = SystemColors.HighlightText;
            if (!back.IsEmpty)
            {
                using (SolidBrush b = new SolidBrush(back)) g.FillRectangle(b, label);
            }
            if (DrawMode == TreeViewDrawMode.OwnerDrawText && DrawNode != null)
            {
                DrawTreeNodeEventArgs args = new DrawTreeNodeEventArgs(g, node, label, selected ? TreeNodeStates.Selected : 0);
                OnDrawNode(args);
                if (!args.DrawDefault) return;
            }
            using (SolidBrush b = new SolidBrush(Enabled ? fore : SystemColors.GrayText))
            {
                StringFormat format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap };
                g.DrawString(node.Text, node.NodeFont ?? Font, b, new RectangleF(label.X, label.Y, label.Width + 20, label.Height), format);
            }
        }
        private Image NodeImage(TreeNode node, bool selected)
        {
            if (_imageList == null) return null;
            string key = selected && !string.IsNullOrEmpty(node.SelectedImageKey) ? node.SelectedImageKey : node.ImageKey;
            int index = selected && node.SelectedImageIndex >= 0 ? node.SelectedImageIndex : node.ImageIndex;
            if (!string.IsNullOrEmpty(key) && _imageList.Images.ContainsKey(key)) return _imageList.Images[key];
            if (index >= 0 && index < _imageList.Images.Count) return _imageList.Images[index];
            if (!string.IsNullOrEmpty(ImageKey) && _imageList.Images.ContainsKey(ImageKey)) return _imageList.Images[ImageKey];
            if (ImageIndex >= 0 && ImageIndex < _imageList.Images.Count) return _imageList.Images[ImageIndex];
            return _imageList.Images.Count > 0 && string.IsNullOrEmpty(key) && index < 0 ? _imageList.Images[0] : null;
        }

        // Mouse
        internal override bool CanSelectOnClick { get { return true; } }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (_editor != null) EndEditNode(false);
            TreeViewHitTestInfo hit = HitTest(e.X, e.Y);
            TreeNode node = hit.Node;
            _mouseDownNode = node;
            _mouseDownPoint = e.Location;
            _dragStarted = false;
            if (node != null)
            {
                if (hit.Location == TreeViewHitTestLocations.PlusMinus && e.Button == MouseButtons.Left) node.Toggle();
                else if (CheckBoxes && e.X < LabelBounds(node).Left - (HasImages ? _imageList.ImageSize.Width + 3 : 0) && e.X >= LabelBounds(node).Left - (HasImages ? _imageList.ImageSize.Width + 3 : 0) - 16)
                {
                    if (RaiseBeforeCheck(node, TreeViewAction.ByMouse)) { node.Checked = !node.Checked; }
                }
                else if (hit.Location != TreeViewHitTestLocations.Indent || FullRowSelect)
                {
                    if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right) SelectNode(node, TreeViewAction.ByMouse);
                }
                if (e.Clicks == 2 && e.Button == MouseButtons.Left && hit.Location != TreeViewHitTestLocations.PlusMinus)
                {
                    TreeNodeMouseClickEventArgs args = new TreeNodeMouseClickEventArgs(node, e.Button, e.Clicks, e.X, e.Y);
                    OnNodeMouseDoubleClick(args);
                    if (node.HasChildren && !IsDisposed) node.Toggle();
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.None && _mouseDownNode != null && !_dragStarted &&
                (Math.Abs(e.X - _mouseDownPoint.X) > SystemInformation.DragSize.Width || Math.Abs(e.Y - _mouseDownPoint.Y) > SystemInformation.DragSize.Height))
            {
                _dragStarted = true;
                OnItemDrag(new ItemDragEventArgs(e.Button, _mouseDownNode));
            }
            if (HotTracking || ShowNodeToolTips)
            {
                TreeNode hot = GetNodeAt(e.X, e.Y);
                if (hot != _hotNode)
                {
                    _hotNode = hot;
                    if (ShowNodeToolTips) Avalonia.Controls.ToolTip.SetTip(NativeControl, hot?.ToolTipText);
                    if (hot != null) OnNodeMouseHover(new TreeNodeMouseHoverEventArgs(hot));
                }
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            TreeNode node = GetNodeAt(e.X, e.Y);
            if (node != null && node == _mouseDownNode && !_dragStarted)
            {
                OnNodeMouseClick(new TreeNodeMouseClickEventArgs(node, e.Button, 1, e.X, e.Y));
                if (e.Button == MouseButtons.Right && node.ContextMenuStrip != null) node.ContextMenuStrip.Show(this, e.Location);
            }
            _mouseDownNode = null;
            base.OnMouseUp(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_scroll.Wheel(e.Delta) && e is HandledMouseEventArgs h) h.Handled = true;
            base.OnMouseWheel(e);
        }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        // Keyboard
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Up || key == Keys.Down || key == Keys.Left || key == Keys.Right || key == Keys.Home || key == Keys.End ||
                key == Keys.PageUp || key == Keys.PageDown) return true;
            if (_editor != null && (key == Keys.Enter || key == Keys.Escape)) return true;
            return base.IsInputKey(keyData);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled) return;
            EnsureLayout();
            TreeNode node = SelectedNodeInternal;
            int index = node == null ? -1 : _rows.IndexOf(node);
            switch (e.KeyCode)
            {
                case Keys.Down: if (index + 1 < _rows.Count) SelectNode(_rows[index + 1], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.Up: if (index > 0) SelectNode(_rows[index - 1], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.Home: if (_rows.Count > 0) SelectNode(_rows[0], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.End: if (_rows.Count > 0) SelectNode(_rows[_rows.Count - 1], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.PageDown: if (_rows.Count > 0) SelectNode(_rows[Math.Min(_rows.Count - 1, Math.Max(0, index) + VisibleCount)], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.PageUp: if (_rows.Count > 0) SelectNode(_rows[Math.Max(0, index - VisibleCount)], TreeViewAction.ByKeyboard); e.Handled = true; break;
                case Keys.Right:
                    if (node != null)
                    {
                        if (node.HasChildren && !node.IsExpanded) node.Expand();
                        else if (node.HasChildren) SelectNode(node.FirstNode, TreeViewAction.ByKeyboard);
                    }
                    e.Handled = true;
                    break;
                case Keys.Left:
                    if (node != null)
                    {
                        if (node.IsExpanded) node.Collapse();
                        else if (node.ParentInternal != null) SelectNode(node.ParentInternal, TreeViewAction.ByKeyboard);
                    }
                    e.Handled = true;
                    break;
                case Keys.F2:
                    if (LabelEdit && node != null) node.BeginEdit();
                    break;
            }
        }

        // Label editing
        internal void BeginEditNode(TreeNode node)
        {
            if (!LabelEdit || node == null) return;
            NodeLabelEditEventArgs before = new NodeLabelEditEventArgs(node);
            OnBeforeLabelEdit(before);
            if (before.CancelEdit) return;
            EditingNode = node;
            _editor = new TextBox { BorderStyle = BorderStyle.FixedSingle, Text = node.Text, Font = node.NodeFont ?? Font };
            Controls.Add(_editor);
            PlaceEditor();
            _editor.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { EndEditNode(false); e.Handled = true; }
                else if (e.KeyCode == Keys.Escape) { EndEditNode(true); e.Handled = true; }
            };
            _editor.LostFocus += (s, e) => Dispatcher.UIThread.Post(() => EndEditNode(false));
            _editor.Focus();
            _editor.SelectAll();
        }
        private void PlaceEditor()
        {
            if (_editor == null || EditingNode == null) return;
            Rectangle b = LabelBounds(EditingNode);
            _editor.Bounds = new Rectangle(b.X, b.Y, Math.Max(b.Width + 30, 80), b.Height + 2);
        }
        internal void EndEditNode(bool cancel)
        {
            if (_editor == null) return;
            TextBox editor = _editor;
            TreeNode node = EditingNode;
            _editor = null;
            EditingNode = null;
            string label = cancel ? null : editor.Text;
            NodeLabelEditEventArgs e = new NodeLabelEditEventArgs(node, label);
            OnAfterLabelEdit(e);
            if (!cancel && !e.CancelEdit && label != null) node.Text = label;
            Controls.Remove(editor);
            editor.Dispose();
            Focus();
            Invalidate();
        }
    }

    public class TreeViewEventArgs : EventArgs
    {
        public TreeNode Node { get; }
        public TreeViewAction Action { get; }
        public TreeViewEventArgs(TreeNode node) : this(node, TreeViewAction.Unknown) { }
        public TreeViewEventArgs(TreeNode node, TreeViewAction action) { Node = node; Action = action; }
    }

    public delegate void TreeViewEventHandler(object sender, TreeViewEventArgs e);

    public class TreeViewCancelEventArgs : CancelEventArgs
    {
        public TreeNode Node { get; }
        public TreeViewAction Action { get; }
        public TreeViewCancelEventArgs(TreeNode node, bool cancel, TreeViewAction action) : base(cancel) { Node = node; Action = action; }
    }

    public delegate void TreeViewCancelEventHandler(object sender, TreeViewCancelEventArgs e);

    public class TreeNodeMouseClickEventArgs : MouseEventArgs
    {
        public TreeNode Node { get; }
        public TreeNodeMouseClickEventArgs(TreeNode node, MouseButtons button, int clicks, int x, int y) : base(button, clicks, x, y, 0) { Node = node; }
    }

    public delegate void TreeNodeMouseClickEventHandler(object sender, TreeNodeMouseClickEventArgs e);

    public class TreeNodeMouseHoverEventArgs : EventArgs
    {
        public TreeNode Node { get; }
        public TreeNodeMouseHoverEventArgs(TreeNode node) { Node = node; }
    }

    public delegate void TreeNodeMouseHoverEventHandler(object sender, TreeNodeMouseHoverEventArgs e);

    public class ItemDragEventArgs : EventArgs
    {
        public MouseButtons Button { get; }
        public object Item { get; }
        public ItemDragEventArgs(MouseButtons button) : this(button, null) { }
        public ItemDragEventArgs(MouseButtons button, object item) { Button = button; Item = item; }
    }

    public delegate void ItemDragEventHandler(object sender, ItemDragEventArgs e);

    public class NodeLabelEditEventArgs : EventArgs
    {
        public TreeNode Node { get; }
        public string Label { get; }
        public bool CancelEdit { get; set; }
        public NodeLabelEditEventArgs(TreeNode node) { Node = node; }
        public NodeLabelEditEventArgs(TreeNode node, string label) { Node = node; Label = label; }
    }

    public delegate void NodeLabelEditEventHandler(object sender, NodeLabelEditEventArgs e);

    public class DrawTreeNodeEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public TreeNode Node { get; }
        public Rectangle Bounds { get; }
        public TreeNodeStates State { get; }
        public bool DrawDefault { get; set; }
        public DrawTreeNodeEventArgs(Graphics graphics, TreeNode node, Rectangle bounds, TreeNodeStates state)
        {
            Graphics = graphics;
            Node = node;
            Bounds = bounds;
            State = state;
        }
    }

    public delegate void DrawTreeNodeEventHandler(object sender, DrawTreeNodeEventArgs e);

    public class TreeViewHitTestInfo
    {
        public TreeNode Node { get; }
        public TreeViewHitTestLocations Location { get; }
        public TreeViewHitTestInfo(TreeNode hitNode, TreeViewHitTestLocations hitLocation) { Node = hitNode; Location = hitLocation; }
    }
}
