using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public class ListViewItemCollectionWithEvents : ListView.ListViewItemCollection
    {
        // Variables                                                                                                                
        private ListView parent;


        // Constructor                                                                                                              
        public ListViewItemCollectionWithEvents(ListView owner)
            : base(owner)
        {
            parent = owner;
        }


        // Methods                                                                                                                  
        public new ListViewItem Add(ListViewItem listViewItem)
        {
            base.Add(listViewItem);
            ((ListViewWithSelection)parent).ItemAdded(listViewItem);
            return listViewItem;
        }
        public new ListViewItem[] AddRange(ListViewItem[] listViewItems)
        {
            base.AddRange(listViewItems);
            ((ListViewWithSelection)parent).ItemRangeAdded(listViewItems);
            return listViewItems;
        }
        public new ListViewItem Remove(ListViewItem listViewItem)
        {
            base.Remove(listViewItem);
            ((ListViewWithSelection)parent).ItemRemoved(listViewItem);
            return listViewItem;
        }
        public new ListViewItem RemoveAt(int index)
        {
            ListViewItem listViewItem = this[index];
            base.RemoveAt(index);
            ((ListViewWithSelection)parent).ItemRemovedAt(index, listViewItem);
            return listViewItem;
        }
    }

    public partial class ListViewWithSelection : ListView
    {
        // Delegates
        public delegate void ListViewItemAddedDelegate(ListViewItem item);
        public delegate void ListViewItemRangeAddedDelegate(ListViewItem[] item);
        public delegate void ListViewItemRemovedDelegate(ListViewItem item);
        public delegate void ListViewItemRemovedAtDelegate(int index, ListViewItem item);
        

        // Variables                                                                                                                
        private bool _disableMouse;
        private bool _resizeNeeded;
        private ListViewItem _prevSelectedItem;
        private Color[] _prevSelectedItemsColors;
        private Color _mouseOverColor = Color.FromArgb((int)(SystemColors.Highlight.R + (255 - SystemColors.Highlight.R) * 0.8),
                                                       (int)(SystemColors.Highlight.G + (255 - SystemColors.Highlight.G) * 0.8),
                                                       (int)(SystemColors.Highlight.B + (255 - SystemColors.Highlight.B) * 0.8));
        private ListViewItem _prevMouseOverItem;
        private Color[] _prevMouseOverNodeColors;
        public new ListViewItemCollectionWithEvents Items;


        // Properties                                                                                                               
        public bool DisableMouse { get { return _disableMouse; } set { _disableMouse = value; } }
        public Color MouseOverColor
        {
            get { return _mouseOverColor; }
            set { _mouseOverColor = value; }
        }

        public List<ListViewItem> PossiblySelectedItems
        {
            get 
            {
                if (base.SelectedItems.Count > 0)
                {
                    List<ListViewItem> list = new List<ListViewItem>();
                    foreach (ListViewItem item in base.SelectedItems) list.Add(item);
                    return list;
                }
                else if (_prevSelectedItem != null)
                {
                    List<ListViewItem> list = new List<ListViewItem>() { _prevSelectedItem };
                    return list;
                }
                return new List<ListViewItem>();
            }
        }


        // Events                                                                                                                   
        public event ListViewItemAddedDelegate ItemAddedEvent;
        public event ListViewItemRangeAddedDelegate ItemRangeAddedEvent;
        public event ListViewItemRemovedDelegate ItemRemovedEvent;
        public event ListViewItemRemovedAtDelegate ItemRemovedAtEvent;


        // Constructor                                                                                                              
        public ListViewWithSelection()
        {
            InitializeComponent();
            // Activate double buffering
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            //
            DoubleBuffered = true;
            //
            _disableMouse = false;
            _resizeNeeded = true;
            Items = new ListViewItemCollectionWithEvents(this);
        }


        // Methods                                                                                                                  
        protected override void WndProc(ref Message m)
        {
            // Keeps the selection at all times - even if clicked outside client area
            // Swallow mouse messages that are not in the client area
            if (m.Msg >= 0x201 && m.Msg <= 0x209)
            {
                // Eat left and right mouse clicks
                if (_disableMouse) return;
                //
                Point pos = new Point(m.LParam.ToInt32());
                var hit = this.HitTest(pos);
                switch (hit.Location)
                {
                    case ListViewHitTestLocations.AboveClientArea:
                    case ListViewHitTestLocations.BelowClientArea:
                    case ListViewHitTestLocations.LeftOfClientArea:
                    case ListViewHitTestLocations.RightOfClientArea:
                    case ListViewHitTestLocations.None:
                        return;
                }
            }
            else if (m.Msg == 0x0F) // WM_PAINT
            {
                ResizeColumnHeaders();
            }
            //
            //switch (m.Msg)
            //{
            //    case 0x1007:    //ListViewItemAdd-A
            //        System.Diagnostics.Debug.WriteLine("Item added (A)");
            //        //ResizeColumnHeaders();
            //        break;
            //    case 0x104D:    //ListViewItemAdd-W
            //        System.Diagnostics.Debug.WriteLine("Item added (W)");
            //        //ResizeColumnHeaders();
            //        break;
            //    case 0x1008:
            //        System.Diagnostics.Debug.WriteLine("Item removed");
            //        //ResizeColumnHeaders();
            //        break;
            //    case 0x1009:
            //        System.Diagnostics.Debug.WriteLine("Item removed (All)");
            //        //ResizeColumnHeaders();
            //        break;
            //    default:
            //        break;
            //}
            //
            base.WndProc(ref m);
        }
        public void ResizeColumnHeaders()
        {
            if (_resizeNeeded)
            {
                _resizeNeeded = false;
                //
                if (this.Columns.Count > 0)
                {
                    for (int i = 0; i < this.Columns.Count - 1; i++) AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                    // -1 Contents
                    // -2 fill
                    this.Columns[this.Columns.Count - 1].Width = -1;
                    //this.Columns[this.Columns.Count - 1].Width = -2;
                    //this.Columns[this.Columns.Count - 1].Width -= 2;
                }
            }
        }
        internal void ItemAdded(ListViewItem listViewItem)
        {
            //ResizeColumnHeaders();
            _resizeNeeded = true;
            ItemAddedEvent?.Invoke(listViewItem);
        }
        internal void ItemRangeAdded(ListViewItem[] listViewItems)
        {
            //ResizeColumnHeaders();
            _resizeNeeded = true;
            ItemRangeAddedEvent?.Invoke(listViewItems);
        }
        internal void ItemRemoved(ListViewItem listViewItem)
        {
            //ResizeColumnHeaders();
            _resizeNeeded = true;
            ItemRemovedEvent?.Invoke(listViewItem);
        }
        internal void ItemRemovedAt(int index, ListViewItem listViewItem)
        {
            //ResizeColumnHeaders();
            _resizeNeeded = true;
            ItemRemovedAtEvent?.Invoke(index, listViewItem);
        }
        

        //https://stackoverflow.com/questions/2691726/how-can-i-remove-the-selection-border-on-a-listviewitem                       
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e); // must be first
            //
            if (SelectedItems.Count > 0)
            {
                ClearPrevSelection();
                //
                if (Enabled)
                {
                    // Remove the selection and use color to show the previously selected item
                    foreach (ListViewItem item in SelectedItems)
                    {
                        _prevSelectedItem = item;
                        _prevSelectedItemsColors = new Color[] { item.BackColor, item.ForeColor };
                        //
                        item.BackColor = SystemColors.Highlight;
                        item.ForeColor = SystemColors.HighlightText;
                        //
                        item.Selected = false;  // calls itself
                    }
                }
            }
            //
            Message m = Message.Create(this.Handle, 0x127, new IntPtr(0x10001), new IntPtr(0));
            this.WndProc(ref m);
        }
        protected override void OnEnter(EventArgs e)
        {
            base.OnEnter(e);
            Message m = Message.Create(this.Handle, 0x127, new IntPtr(0x10001), new IntPtr(0));
            this.WndProc(ref m);
        }
        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            //Message m = Message.Create(this.Handle, 0x127, new IntPtr(0x10001), new IntPtr(0));
            //this.WndProc(ref m);
        }
        protected override void OnEnabledChanged(EventArgs e)
        {
            if (Enabled == false && _prevSelectedItem != null)
            {
                _prevSelectedItem.Selected = true;
            }
            base.OnEnabledChanged(e);
        }
        //
        public ListViewItem SelectedItem
        {
            get
            {
                if (SelectedItems != null && SelectedItems.Count == 1)
                    return SelectedItems[0];
                else if (_prevSelectedItem != null) return _prevSelectedItem;
                else return null;
            }
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            ClearMouseOverSelection();
            //
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            ListViewItem item = this.GetItemAt(e.X, e.Y);
            //
            if (item != null && !item.Selected && item != _prevSelectedItem)
            {
                if (item != _prevMouseOverItem)
                {
                    ClearMouseOverSelection();
                    //
                    _prevMouseOverNodeColors = new Color[] { item.BackColor, item.ForeColor };
                    //
                    item.BackColor = _mouseOverColor;
                    _prevMouseOverItem = item;
                }
            }
            else ClearMouseOverSelection();
            //
            base.OnMouseMove(e);    
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            ClearMouseOverSelection();
            //
            base.OnMouseLeave(e);
        }
        private void ClearMouseOverSelection()
        {
            if (_prevMouseOverItem != null && !_prevMouseOverItem.Selected)
            {
                _prevMouseOverItem.BackColor = _prevMouseOverNodeColors[0];
                _prevMouseOverItem.ForeColor = _prevMouseOverNodeColors[1];
            }
            //
            _prevMouseOverItem = null;
            _prevMouseOverNodeColors = null;
        }
        private void ClearPrevSelection()
        {
            if (_prevSelectedItem != null)
            {
                _prevSelectedItem.BackColor = _prevSelectedItemsColors[0];
                _prevSelectedItem.ForeColor = _prevSelectedItemsColors[1];
            }
            //
            _prevSelectedItem = null;
            _prevSelectedItemsColors = null;
        }
    }
}
