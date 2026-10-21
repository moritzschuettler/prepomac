// PrePoMax for macOS - ToolStrip, MenuStrip, StatusStrip, ContextMenuStrip and their items.
// The strips are drawn by a ToolStripRenderer like in WinForms; drop-down menus open in Avalonia popups.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AC = Avalonia.Controls;

namespace System.Windows.Forms
{
    // Items
    [DesignTimeVisible(false)]
    [DefaultEvent("Click")]
    public abstract class ToolStripItem : Component
    {
        private string _text = string.Empty;
        private Image _image;
        private bool _enabled = true;
        private bool _visible = true;
        private Font _font;
        private Color _foreColor = Color.Empty;
        private Color _backColor = Color.Empty;
        private Size _size;
        private bool _autoSize = true;
        private ToolStripItemDisplayStyle _displayStyle = ToolStripItemDisplayStyle.ImageAndText;
        internal Rectangle BoundsInternal;
        internal ToolStrip OwnerInternal;
        internal ToolStripItem OwnerItemInternal;

        protected ToolStripItem()
        {
            _size = DefaultSize;
            Margin = DefaultMargin;
            Padding = DefaultPadding;
        }
        protected ToolStripItem(string text, Image image, EventHandler onClick) : this()
        {
            _text = text ?? string.Empty;
            _image = image;
            if (onClick != null) Click += onClick;
        }
        protected ToolStripItem(string text, Image image, EventHandler onClick, string name) : this(text, image, onClick) { Name = name; }

        public string Name { get; set; } = string.Empty;
        public object Tag { get; set; }
        public string AccessibleName { get; set; }
        public string AccessibleDescription { get; set; }
        public AccessibleRole AccessibleRole { get; set; }
        public virtual string Text
        {
            get { return _text; }
            set
            {
                if (_text == value) return;
                _text = value ?? string.Empty;
                OnTextChanged(EventArgs.Empty);
                Changed(true);
            }
        }
        public virtual Image Image { get { return _image; } set { _image = value; Changed(true); } }
        public Color ImageTransparentColor { get; set; }
        public ContentAlignment ImageAlign { get; set; } = ContentAlignment.MiddleCenter;
        public ToolStripItemImageScaling ImageScaling { get; set; } = ToolStripItemImageScaling.SizeToFit;
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; }
        public virtual ContentAlignment TextAlign { get; set; } = ContentAlignment.MiddleCenter;
        public virtual ToolStripTextDirection TextDirection { get; set; } = ToolStripTextDirection.Horizontal;
        public TextImageRelation TextImageRelation { get; set; } = TextImageRelation.ImageBeforeText;
        public virtual ToolStripItemDisplayStyle DisplayStyle { get { return _displayStyle; } set { _displayStyle = value; Changed(true); } }
        public string ToolTipText { get; set; }
        public bool AutoToolTip { get; set; } = true;
        public virtual bool Enabled
        {
            get { return _enabled && (OwnerInternal == null || OwnerInternal.Enabled); }
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                OnEnabledChanged(EventArgs.Empty);
                Changed(false);
            }
        }
        internal bool EnabledSelf { get { return _enabled; } }
        public bool Visible
        {
            get { return _visible && (OwnerInternal == null || OwnerInternal.Visible); }
            set
            {
                if (_visible == value) return;
                _visible = value;
                OnVisibleChanged(EventArgs.Empty);
                Changed(true);
            }
        }
        public bool Available { get { return _visible; } set { Visible = value; } }
        internal bool VisibleSelf { get { return _visible; } }
        public virtual Font Font { get { return _font ?? OwnerInternal?.Font ?? SystemFonts.MenuFont; } set { _font = value; Changed(true); } }
        public virtual Color ForeColor { get { return !_foreColor.IsEmpty ? _foreColor : OwnerInternal?.ForeColor ?? SystemColors.ControlText; } set { _foreColor = value; Changed(false); } }
        public virtual Color BackColor { get { return !_backColor.IsEmpty ? _backColor : OwnerInternal?.BackColor ?? SystemColors.Control; } set { _backColor = value; Changed(false); } }
        internal bool BackColorSet { get { return !_backColor.IsEmpty; } }
        public Image BackgroundImage { get; set; }
        public ImageLayout BackgroundImageLayout { get; set; }
        public virtual bool AutoSize { get { return _autoSize; } set { _autoSize = value; Changed(true); } }
        public virtual Size Size
        {
            get { return _autoSize ? GetPreferredSize(Size.Empty) : _size; }
            set
            {
                _size = value;
                Changed(true);
            }
        }
        public int Width { get { return Size.Width; } set { Size = new Size(value, Size.Height); } }
        public int Height { get { return Size.Height; } set { Size = new Size(Size.Width, value); } }
        public Rectangle Bounds { get { return BoundsInternal; } }
        public Rectangle ContentRectangle { get { return new Rectangle(Point.Empty, BoundsInternal.Size); } }
        public Padding Margin { get; set; }
        public virtual Padding Padding { get; set; }
        protected virtual Padding DefaultMargin { get { return new Padding(0, 1, 0, 2); } }
        protected virtual Padding DefaultPadding { get { return Padding.Empty; } }
        protected virtual Size DefaultSize { get { return new Size(23, 23); } }
        public ToolStripItemAlignment Alignment { get; set; }
        public ToolStripItemOverflow Overflow { get; set; } = ToolStripItemOverflow.AsNeeded;
        public ToolStripItemPlacement Placement { get { return OwnerInternal == null ? ToolStripItemPlacement.None : ToolStripItemPlacement.Main; } }
        public MergeAction MergeAction { get; set; }
        public int MergeIndex { get; set; } = -1;
        public bool DoubleClickEnabled { get; set; }
        public bool RightToLeftAutoMirrorImage { get; set; }
        public RightToLeft RightToLeft { get; set; }
        public bool AllowDrop { get; set; }
        public AnchorStyles Anchor { get; set; }
        public DockStyle Dock { get; set; }
        public ToolStrip Owner
        {
            get { return OwnerInternal; }
            set
            {
                if (OwnerInternal == value) return;
                OwnerInternal?.Items.Remove(this);
                value?.Items.Add(this);
            }
        }
        public ToolStrip Parent { get { return OwnerInternal; } }
        public ToolStripItem OwnerItem { get { return OwnerItemInternal ?? (OwnerInternal as ToolStripDropDown)?.OwnerItem; } }
        public ToolStrip GetCurrentParent() { return OwnerInternal; }
        public virtual bool Selected { get { return OwnerInternal != null && OwnerInternal.HotItem == this; } }
        public virtual bool Pressed { get { return OwnerInternal != null && OwnerInternal.PressedItem == this; } }
        public virtual bool CanSelect { get { return true; } }
        public bool IsOnDropDown { get { return OwnerInternal is ToolStripDropDown; } }
        public bool IsOnOverflow { get { return false; } }
        public bool IsDisposed { get; private set; }

        public event EventHandler Click;
        public event EventHandler DoubleClick;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        public event MouseEventHandler MouseMove;
        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        public event EventHandler MouseHover;
        public event EventHandler EnabledChanged;
        public event EventHandler VisibleChanged;
        public event EventHandler TextChanged;
        public event EventHandler OwnerChanged;
        public event EventHandler LocationChanged;
        public event PaintEventHandler Paint;
        public event EventHandler AvailableChanged;
        public event DragEventHandler DragDrop;
        public event DragEventHandler DragEnter;
        public event EventHandler DragLeave;
        public event DragEventHandler DragOver;
        public event EventHandler BackColorChanged;
        public event EventHandler ForeColorChanged;
        public event EventHandler DisplayStyleChanged;

        protected virtual void OnClick(EventArgs e) { Click?.Invoke(this, e); }
        protected virtual void OnDoubleClick(EventArgs e) { DoubleClick?.Invoke(this, e); }
        protected virtual void OnMouseDown(MouseEventArgs e) { MouseDown?.Invoke(this, e); }
        protected virtual void OnMouseUp(MouseEventArgs e) { MouseUp?.Invoke(this, e); }
        protected virtual void OnMouseMove(MouseEventArgs mea) { MouseMove?.Invoke(this, mea); }
        protected virtual void OnMouseEnter(EventArgs e) { MouseEnter?.Invoke(this, e); }
        protected virtual void OnMouseLeave(EventArgs e) { MouseLeave?.Invoke(this, e); }
        protected virtual void OnMouseHover(EventArgs e) { MouseHover?.Invoke(this, e); }
        protected virtual void OnEnabledChanged(EventArgs e) { EnabledChanged?.Invoke(this, e); }
        protected virtual void OnVisibleChanged(EventArgs e) { VisibleChanged?.Invoke(this, e); }
        protected virtual void OnTextChanged(EventArgs e) { TextChanged?.Invoke(this, e); }
        protected virtual void OnOwnerChanged(EventArgs e) { OwnerChanged?.Invoke(this, e); }
        protected virtual void OnPaint(PaintEventArgs e) { Paint?.Invoke(this, e); }
        internal void RaiseClick() { OnClick(EventArgs.Empty); }
        internal void RaiseDoubleClick() { OnDoubleClick(EventArgs.Empty); }
        internal void RaiseMouseDown(MouseEventArgs e) { OnMouseDown(e); }
        internal void RaiseMouseUp(MouseEventArgs e) { OnMouseUp(e); }
        internal void RaiseMouseMove(MouseEventArgs e) { OnMouseMove(e); }
        internal void RaiseMouseEnter() { OnMouseEnter(EventArgs.Empty); }
        internal void RaiseMouseLeave() { OnMouseLeave(EventArgs.Empty); }
        internal void RaiseOwnerChanged() { OnOwnerChanged(EventArgs.Empty); }
        internal bool HasPaintHandlers { get { return Paint != null; } }
        internal void RaisePaint(PaintEventArgs e) { OnPaint(e); }

        public void PerformClick()
        {
            if (!Enabled) return;
            OnPerformClick();
        }
        internal virtual void OnPerformClick() { OnClick(EventArgs.Empty); }
        public void Select() { if (OwnerInternal != null) OwnerInternal.HotItem = this; }
        public void Invalidate() { OwnerInternal?.Invalidate(); }
        public void Invalidate(Rectangle r) { OwnerInternal?.Invalidate(); }
        protected void Changed(bool layout)
        {
            if (OwnerInternal == null) return;
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) { Toolkit.OnUiThread(() => Changed(layout)); return; }
            if (layout) OwnerInternal.LayoutItems();
            OwnerInternal.Invalidate();
        }
        public virtual Size GetPreferredSize(Size constrainingSize)
        {
            Size size = ToolStripLayout.ContentSize(this);
            Padding p = Padding;
            return new Size(size.Width + p.Horizontal, size.Height + p.Vertical);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) OwnerInternal?.Items.Remove(this);
            IsDisposed = true;
            base.Dispose(disposing);
        }
        public override string ToString() { return string.IsNullOrEmpty(_text) ? base.ToString() : _text; }
        internal string DisplayText { get { return ContentLayout.StripMnemonic(_text, true); } }
        internal virtual bool ShowsImage { get { return Image != null && (_displayStyle == ToolStripItemDisplayStyle.Image || _displayStyle == ToolStripItemDisplayStyle.ImageAndText); } }
        internal virtual bool ShowsText { get { return !string.IsNullOrEmpty(_text) && (_displayStyle == ToolStripItemDisplayStyle.Text || _displayStyle == ToolStripItemDisplayStyle.ImageAndText); } }
        // A tool strip item can be clicked; separators and labels cannot
        internal virtual bool IsClickable { get { return true; } }
        internal virtual void OnOwnerSet() { }
    }

    public enum ToolStripItemPlacement { Main = 0, Overflow = 1, None = 2 }

    public class ToolStripButton : ToolStripItem
    {
        private CheckState _checkState;
        public ToolStripButton() { }
        public ToolStripButton(string text) : base(text, null, null) { }
        public ToolStripButton(Image image) : base(null, image, null) { }
        public ToolStripButton(string text, Image image) : base(text, image, null) { }
        public ToolStripButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
        public ToolStripButton(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }
        public bool CheckOnClick { get; set; }
        public bool Checked { get { return _checkState != CheckState.Unchecked; } set { CheckState = value ? CheckState.Checked : CheckState.Unchecked; } }
        public CheckState CheckState
        {
            get { return _checkState; }
            set
            {
                if (_checkState == value) return;
                _checkState = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
                OnCheckStateChanged(EventArgs.Empty);
            }
        }
        public event EventHandler CheckedChanged;
        public event EventHandler CheckStateChanged;
        protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }
        protected virtual void OnCheckStateChanged(EventArgs e) { CheckStateChanged?.Invoke(this, e); }
        protected override void OnClick(EventArgs e)
        {
            if (CheckOnClick) Checked = !Checked;
            base.OnClick(e);
        }
    }

    public class ToolStripLabel : ToolStripItem
    {
        public ToolStripLabel() { }
        public ToolStripLabel(string text) : base(text, null, null) { }
        public ToolStripLabel(Image image) : base(null, image, null) { }
        public ToolStripLabel(string text, Image image) : base(text, image, null) { }
        public ToolStripLabel(string text, Image image, bool isLink) : base(text, image, null) { IsLink = isLink; }
        public ToolStripLabel(string text, Image image, bool isLink, EventHandler onClick) : base(text, image, onClick) { IsLink = isLink; }
        public bool IsLink { get; set; }
        public LinkBehavior LinkBehavior { get; set; }
        public Color LinkColor { get; set; } = Color.Blue;
        public Color ActiveLinkColor { get; set; } = Color.Red;
        public Color VisitedLinkColor { get; set; } = Color.Purple;
        public bool LinkVisited { get; set; }
        public override bool CanSelect { get { return IsLink; } }
        internal override bool IsClickable { get { return IsLink || true; } }
    }

    public class ToolStripStatusLabel : ToolStripLabel
    {
        public ToolStripStatusLabel() { }
        public ToolStripStatusLabel(string text) : base(text) { }
        public ToolStripStatusLabel(Image image) : base(image) { }
        public ToolStripStatusLabel(string text, Image image) : base(text, image) { }
        public bool Spring { get; set; }
        public ToolStripStatusLabelBorderSides BorderSides { get; set; }
        public Border3DStyle BorderStyle { get; set; } = Border3DStyle.Flat;
        public ToolStripItemAlignment Alignment_ { get { return Alignment; } }
        protected override Padding DefaultMargin { get { return new Padding(0, 3, 0, 2); } }
    }

    public enum Border3DStyle { Adjust = 8192, Bump = 9, Etched = 6, Flat = 16394, Raised = 5, RaisedInner = 4, RaisedOuter = 1, Sunken = 10, SunkenInner = 8, SunkenOuter = 2 }

    public class ToolStripSeparator : ToolStripItem
    {
        public ToolStripSeparator() { }
        protected override Size DefaultSize { get { return new Size(6, 6); } }
        public override bool CanSelect { get { return false; } }
        internal override bool IsClickable { get { return false; } }
        public override Size GetPreferredSize(Size constrainingSize)
        {
            return OwnerInternal is ToolStripDropDownMenu ? new Size(Math.Max(1, constrainingSize.Width), 6) : new Size(6, 25);
        }
        protected override Padding DefaultMargin { get { return Padding.Empty; } }
    }

    public abstract class ToolStripDropDownItem : ToolStripItem
    {
        private ToolStripDropDown _dropDown;
        protected ToolStripDropDownItem() { }
        protected ToolStripDropDownItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
        protected ToolStripDropDownItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }
        protected ToolStripDropDownItem(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, null)
        {
            if (dropDownItems != null) DropDownItems.AddRange(dropDownItems);
        }
        public ToolStripDropDown DropDown
        {
            get
            {
                if (_dropDown == null)
                {
                    _dropDown = CreateDefaultDropDown();
                    _dropDown.OwnerItem = this;
                }
                return _dropDown;
            }
            set
            {
                _dropDown = value;
                if (value != null) value.OwnerItem = this;
            }
        }
        public ToolStripItemCollection DropDownItems { get { return DropDown.Items; } }
        public bool HasDropDownItems { get { return _dropDown != null && _dropDown.Items.Count > 0; } }
        public bool HasDropDown { get { return _dropDown != null; } }
        public bool DropDownIsOpen { get { return _dropDown != null && _dropDown.Visible; } }
        public ToolStripDropDownDirection DropDownDirection { get; set; } = ToolStripDropDownDirection.Default;
        public event EventHandler DropDownOpening;
        public event EventHandler DropDownOpened;
        public event EventHandler DropDownClosed;
        public event ToolStripItemClickedEventHandler DropDownItemClicked;
        protected virtual ToolStripDropDown CreateDefaultDropDown() { return new ToolStripDropDownMenu(); }
        protected internal virtual void OnDropDownOpening(EventArgs e) { DropDownOpening?.Invoke(this, e); }
        protected internal virtual void OnDropDownShow(EventArgs e) { DropDownOpened?.Invoke(this, e); }
        protected internal virtual void OnDropDownHide(EventArgs e) { }
        protected internal virtual void OnDropDownClosed(EventArgs e) { DropDownClosed?.Invoke(this, e); }
        protected internal virtual void OnDropDownItemClicked(ToolStripItemClickedEventArgs e) { DropDownItemClicked?.Invoke(this, e); }
        internal void RaiseDropDownOpened() { OnDropDownShow(EventArgs.Empty); }
        internal void RaiseDropDownClosed() { OnDropDownClosed(EventArgs.Empty); }
        public void ShowDropDown()
        {
            if (OwnerInternal == null) return;
            OnDropDownOpening(EventArgs.Empty);
            if (!HasDropDownItems) return;
            Rectangle b = Bounds;
            bool below = !(OwnerInternal is ToolStripDropDown);
            Point local = below ? new Point(b.Left, b.Bottom) : new Point(b.Right - 2, b.Top - 3);
            DropDown.ShowFrom(OwnerInternal, local, this);
        }
        public void HideDropDown() { _dropDown?.Close(ToolStripDropDownCloseReason.CloseCalled); }
        internal override void OnPerformClick()
        {
            if (HasDropDownItems && this is ToolStripMenuItem && OwnerInternal != null) ShowDropDown();
            base.OnPerformClick();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) _dropDown?.Dispose();
            base.Dispose(disposing);
        }
    }

    public class ToolStripMenuItem : ToolStripDropDownItem
    {
        private CheckState _checkState;
        private Keys _shortcutKeys;
        public ToolStripMenuItem() { }
        public ToolStripMenuItem(string text) : base(text, null, (EventHandler)null) { }
        public ToolStripMenuItem(Image image) : base(null, image, (EventHandler)null) { }
        public ToolStripMenuItem(string text, Image image) : base(text, image, (EventHandler)null) { }
        public ToolStripMenuItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
        public ToolStripMenuItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) { }
        public ToolStripMenuItem(string text, Image image, params ToolStripItem[] dropDownItems) : base(text, image, dropDownItems) { }
        public ToolStripMenuItem(string text, Image image, EventHandler onClick, Keys shortcutKeys) : base(text, image, onClick) { _shortcutKeys = shortcutKeys; }
        public bool CheckOnClick { get; set; }
        public bool Checked { get { return _checkState != CheckState.Unchecked; } set { CheckState = value ? CheckState.Checked : CheckState.Unchecked; } }
        public CheckState CheckState
        {
            get { return _checkState; }
            set
            {
                if (_checkState == value) return;
                _checkState = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
                OnCheckStateChanged(EventArgs.Empty);
            }
        }
        public Keys ShortcutKeys { get { return _shortcutKeys; } set { _shortcutKeys = value; Changed(true); } }
        public string ShortcutKeyDisplayString { get; set; }
        public bool ShowShortcutKeys { get; set; } = true;
        public bool IsMdiWindowListEntry { get { return false; } }
        public event EventHandler CheckedChanged;
        public event EventHandler CheckStateChanged;
        protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }
        protected virtual void OnCheckStateChanged(EventArgs e) { CheckStateChanged?.Invoke(this, e); }
        protected override Padding DefaultMargin { get { return Padding.Empty; } }
        protected override Padding DefaultPadding { get { return new Padding(4, 0, 4, 0); } }
        protected override void OnClick(EventArgs e)
        {
            if (CheckOnClick) Checked = !Checked;
            base.OnClick(e);
        }
        internal string ShortcutText
        {
            get
            {
                if (!ShowShortcutKeys) return null;
                if (!string.IsNullOrEmpty(ShortcutKeyDisplayString)) return ShortcutKeyDisplayString;
                return _shortcutKeys == Keys.None ? null : ToolStripLayout.ShortcutToText(_shortcutKeys);
            }
        }
        public override Size GetPreferredSize(Size constrainingSize)
        {
            if (OwnerInternal is ToolStripDropDownMenu menu) return menu.MenuItemSize(this);
            Size size = ToolStripLayout.ContentSize(this);
            return new Size(size.Width + Padding.Horizontal, Math.Max(size.Height, 19));
        }
    }

    public class ToolStripDropDownButton : ToolStripDropDownItem
    {
        public ToolStripDropDownButton() { }
        public ToolStripDropDownButton(string text) : base(text, null, (EventHandler)null) { }
        public ToolStripDropDownButton(Image image) : base(null, image, (EventHandler)null) { }
        public ToolStripDropDownButton(string text, Image image) : base(text, image, (EventHandler)null) { }
        public ToolStripDropDownButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
        public bool ShowDropDownArrow { get; set; } = true;
        internal override void OnPerformClick()
        {
            ShowDropDown();
            base.OnPerformClick();
        }
        public override Size GetPreferredSize(Size constrainingSize)
        {
            Size size = base.GetPreferredSize(constrainingSize);
            return new Size(size.Width + (ShowDropDownArrow ? 10 : 0), size.Height);
        }
    }

    public class ToolStripSplitButton : ToolStripDropDownItem
    {
        public ToolStripSplitButton() { }
        public ToolStripSplitButton(string text) : base(text, null, (EventHandler)null) { }
        public ToolStripSplitButton(string text, Image image) : base(text, image, (EventHandler)null) { }
        public ToolStripSplitButton(string text, Image image, EventHandler onClick) : base(text, image, onClick) { }
        public ToolStripItem DefaultItem { get; set; }
        public int DropDownButtonWidth { get; set; } = 11;
        public bool ButtonPressed { get { return Pressed; } }
        public bool ButtonSelected { get { return Selected; } }
        public event EventHandler ButtonClick;
        public event EventHandler ButtonDoubleClick;
        public event EventHandler DefaultItemChanged;
        protected virtual void OnButtonClick(EventArgs e) { ButtonClick?.Invoke(this, e); }
        public void PerformButtonClick() { OnButtonClick(EventArgs.Empty); }
        public override Size GetPreferredSize(Size constrainingSize)
        {
            Size size = base.GetPreferredSize(constrainingSize);
            return new Size(size.Width + DropDownButtonWidth, size.Height);
        }
        internal void ClickAt(Point p)
        {
            if (p.X >= Bounds.Right - DropDownButtonWidth) ShowDropDown();
            else
            {
                OnButtonClick(EventArgs.Empty);
                RaiseClick();
            }
        }
    }

    public class ToolStripControlHost : ToolStripItem
    {
        private readonly Control _control;
        public ToolStripControlHost(Control c) { _control = c; }
        public ToolStripControlHost(Control c, string name) : this(c) { Name = name; }
        public Control Control { get { return _control; } }
        public ContentAlignment ControlAlign { get; set; } = ContentAlignment.MiddleCenter;
        public override bool Enabled { get { return base.Enabled; } set { base.Enabled = value; _control.Enabled = value; } }
        public override Font Font { get { return _control.Font; } set { _control.Font = value; } }
        public override Color BackColor { get { return _control.BackColor; } set { _control.BackColor = value; } }
        public override Color ForeColor { get { return _control.ForeColor; } set { _control.ForeColor = value; } }
        public override string Text { get { return _control.Text; } set { _control.Text = value; } }
        public bool Focused { get { return _control.Focused; } }
        public void Focus() { _control.Focus(); }
        public event EventHandler GotFocus;
        public event EventHandler LostFocus;
        public event EventHandler Enter;
        public event EventHandler Leave;
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        public event KeyPressEventHandler KeyPress;
        public event EventHandler Validated;
        public event CancelEventHandler Validating;
        internal void HookEvents()
        {
            _control.GotFocus += (s, e) => GotFocus?.Invoke(this, e);
            _control.LostFocus += (s, e) => LostFocus?.Invoke(this, e);
            _control.Enter += (s, e) => Enter?.Invoke(this, e);
            _control.Leave += (s, e) => Leave?.Invoke(this, e);
            _control.KeyDown += (s, e) => KeyDown?.Invoke(this, e);
            _control.KeyUp += (s, e) => KeyUp?.Invoke(this, e);
            _control.KeyPress += (s, e) => KeyPress?.Invoke(this, e);
            _control.Validated += (s, e) => Validated?.Invoke(this, e);
            _control.Validating += (s, e) => Validating?.Invoke(this, e);
        }
        public override Size Size
        {
            get { return AutoSize ? new Size(_control.Width, _control.Height) : base.Size; }
            set
            {
                // The control first: the layout of the strip reads its size
                _control.Size = value;
                base.Size = value;
            }
        }
        public override Size GetPreferredSize(Size constrainingSize) { return _control.Size; }
        internal override bool IsClickable { get { return false; } }
        internal override void OnOwnerSet()
        {
            if (OwnerInternal != null && _control.Parent != OwnerInternal) OwnerInternal.Controls.Add(_control);
        }
    }

    public class ToolStripComboBox : ToolStripControlHost
    {
        public ToolStripComboBox() : base(CreateComboBox())
        {
            HookEvents();
            ComboBox.SelectedIndexChanged += (s, e) => OnSelectedIndexChanged(e);
            ComboBox.DropDown += (s, e) => DropDown?.Invoke(this, e);
            ComboBox.DropDownClosed += (s, e) => DropDownClosed?.Invoke(this, e);
            ComboBox.TextUpdate += (s, e) => TextUpdate?.Invoke(this, e);
            ComboBox.SelectionChangeCommitted += (s, e) => SelectionChangeCommitted?.Invoke(this, e);
            ComboBox.DropDownStyleChanged += (s, e) => DropDownStyleChanged?.Invoke(this, e);
        }
        public ToolStripComboBox(string name) : this() { Name = name; }
        private static Control CreateComboBox() { return new ComboBox { Size = new Size(121, 23) }; }
        public ComboBox ComboBox { get { return (ComboBox)Control; } }
        public ComboBox.ObjectCollection Items { get { return ComboBox.Items; } }
        public int SelectedIndex { get { return ComboBox.SelectedIndex; } set { ComboBox.SelectedIndex = value; } }
        public object SelectedItem { get { return ComboBox.SelectedItem; } set { ComboBox.SelectedItem = value; } }
        public string SelectedText { get { return ComboBox.SelectedText; } set { ComboBox.SelectedText = value; } }
        public ComboBoxStyle DropDownStyle { get { return ComboBox.DropDownStyle; } set { ComboBox.DropDownStyle = value; } }
        public int DropDownWidth { get { return ComboBox.DropDownWidth; } set { ComboBox.DropDownWidth = value; } }
        public int DropDownHeight { get { return ComboBox.DropDownHeight; } set { ComboBox.DropDownHeight = value; } }
        public int MaxDropDownItems { get { return ComboBox.MaxDropDownItems; } set { ComboBox.MaxDropDownItems = value; } }
        public bool DroppedDown { get { return ComboBox.DroppedDown; } set { ComboBox.DroppedDown = value; } }
        public FlatStyle FlatStyle { get { return ComboBox.FlatStyle; } set { ComboBox.FlatStyle = value; } }
        public bool Sorted { get { return ComboBox.Sorted; } set { ComboBox.Sorted = value; } }
        public int MaxLength { get { return ComboBox.MaxLength; } set { ComboBox.MaxLength = value; } }
        public AutoCompleteMode AutoCompleteMode { get { return ComboBox.AutoCompleteMode; } set { ComboBox.AutoCompleteMode = value; } }
        public AutoCompleteSource AutoCompleteSource { get { return ComboBox.AutoCompleteSource; } set { ComboBox.AutoCompleteSource = value; } }
        public bool IntegralHeight { get { return ComboBox.IntegralHeight; } set { ComboBox.IntegralHeight = value; } }
        public event EventHandler SelectedIndexChanged;
        public event EventHandler DropDown;
        public event EventHandler DropDownClosed;
        public event EventHandler TextUpdate;
        public event EventHandler SelectionChangeCommitted;
        public event EventHandler DropDownStyleChanged;
        protected virtual void OnSelectedIndexChanged(EventArgs e) { SelectedIndexChanged?.Invoke(this, e); }
        public void BeginUpdate() { ComboBox.BeginUpdate(); }
        public void EndUpdate() { ComboBox.EndUpdate(); }
        public int FindString(string s) { return ComboBox.FindString(s); }
        public int FindStringExact(string s) { return ComboBox.FindStringExact(s); }
        public void SelectAll() { ComboBox.SelectAll(); }
        public void Select(int start, int length) { ComboBox.Select(start, length); }
        protected override Padding DefaultMargin { get { return new Padding(1, 0, 1, 0); } }
    }

    public class ToolStripTextBox : ToolStripControlHost
    {
        public ToolStripTextBox() : base(new TextBox { Size = new Size(100, 23) })
        {
            HookEvents();
            TextBox.TextChanged += (s, e) => TextChanged_?.Invoke(this, e);
        }
        public ToolStripTextBox(string name) : this() { Name = name; }
        public TextBox TextBox { get { return (TextBox)Control; } }
        public HorizontalAlignment TextBoxTextAlign { get { return TextBox.TextAlign; } set { TextBox.TextAlign = value; } }
        public bool ReadOnly { get { return TextBox.ReadOnly; } set { TextBox.ReadOnly = value; } }
        public int MaxLength { get { return TextBox.MaxLength; } set { TextBox.MaxLength = value; } }
        public bool Multiline { get { return TextBox.Multiline; } set { TextBox.Multiline = value; } }
        public BorderStyle BorderStyle { get { return TextBox.BorderStyle; } set { TextBox.BorderStyle = value; } }
        public CharacterCasing CharacterCasing { get { return TextBox.CharacterCasing; } set { TextBox.CharacterCasing = value; } }
        public bool AcceptsReturn { get { return TextBox.AcceptsReturn; } set { TextBox.AcceptsReturn = value; } }
        public bool AcceptsTab { get { return TextBox.AcceptsTab; } set { TextBox.AcceptsTab = value; } }
        public bool HideSelection { get { return TextBox.HideSelection; } set { TextBox.HideSelection = value; } }
        public bool ShortcutsEnabled { get { return TextBox.ShortcutsEnabled; } set { TextBox.ShortcutsEnabled = value; } }
        public string[] Lines { get { return TextBox.Lines; } set { TextBox.Lines = value; } }
        public bool Modified { get { return TextBox.Modified; } set { TextBox.Modified = value; } }
        public int SelectionStart { get { return TextBox.SelectionStart; } set { TextBox.SelectionStart = value; } }
        public int SelectionLength { get { return TextBox.SelectionLength; } set { TextBox.SelectionLength = value; } }
        public string SelectedText { get { return TextBox.SelectedText; } set { TextBox.SelectedText = value; } }
        public int TextLength { get { return TextBox.TextLength; } }
        public AutoCompleteMode AutoCompleteMode { get; set; }
        public AutoCompleteSource AutoCompleteSource { get; set; }
        public AutoCompleteStringCollection AutoCompleteCustomSource { get; set; } = new AutoCompleteStringCollection();
        public event EventHandler TextChanged_;
        public new event EventHandler TextChanged { add { TextChanged_ += value; } remove { TextChanged_ -= value; } }
        public void AppendText(string text) { TextBox.AppendText(text); }
        public void Clear() { TextBox.Clear(); }
        public void SelectAll() { TextBox.SelectAll(); }
        public void Select(int start, int length) { TextBox.Select(start, length); }
        public void Copy() { TextBox.Copy(); }
        public void Cut() { TextBox.Cut(); }
        public void Paste() { TextBox.Paste(); }
        public void Undo() { TextBox.Undo(); }
        public void ScrollToCaret() { TextBox.ScrollToCaret(); }
        protected override Padding DefaultMargin { get { return new Padding(1, 0, 1, 0); } }
    }

    public class ToolStripProgressBar : ToolStripControlHost
    {
        public ToolStripProgressBar() : base(new ProgressBar { Size = new Size(100, 15) }) { }
        public ToolStripProgressBar(string name) : this() { Name = name; }
        public ProgressBar ProgressBar { get { return (ProgressBar)Control; } }
        public int Value { get { return ProgressBar.Value; } set { ProgressBar.Value = value; } }
        public int Minimum { get { return ProgressBar.Minimum; } set { ProgressBar.Minimum = value; } }
        public int Maximum { get { return ProgressBar.Maximum; } set { ProgressBar.Maximum = value; } }
        public int Step { get { return ProgressBar.Step; } set { ProgressBar.Step = value; } }
        public ProgressBarStyle Style { get { return ProgressBar.Style; } set { ProgressBar.Style = value; } }
        public int MarqueeAnimationSpeed { get { return ProgressBar.MarqueeAnimationSpeed; } set { ProgressBar.MarqueeAnimationSpeed = value; } }
        public void PerformStep() { ProgressBar.PerformStep(); }
        public void Increment(int value) { ProgressBar.Increment(value); }
        protected override Padding DefaultMargin { get { return new Padding(1, 2, 1, 1); } }
    }

    public class ToolStripItemCollection : ArrangedElementCollection, IList
    {
        private readonly ToolStrip _owner;
        private readonly List<ToolStripItem> _list = new List<ToolStripItem>();
        private readonly bool _readOnly;
        public ToolStripItemCollection(ToolStrip owner, ToolStripItem[] value)
        {
            _owner = owner;
            if (value != null) AddRange(value);
        }
        internal ToolStripItemCollection(ToolStrip owner, bool readOnly) { _owner = owner; _readOnly = readOnly; }
        public override int Count { get { return _list.Count; } }
        public bool IsReadOnly { get { return _readOnly; } }
        public virtual ToolStripItem this[int index] { get { return _list[index]; } }
        public virtual ToolStripItem this[string key] { get { int i = IndexOfKey(key); return i < 0 ? null : _list[i]; } }
        public ToolStripItem Add(string text) { ToolStripItem item = _owner.CreateDefaultItemInternal(text, null, null); Add(item); return item; }
        public ToolStripItem Add(Image image) { ToolStripItem item = _owner.CreateDefaultItemInternal(null, image, null); Add(item); return item; }
        public ToolStripItem Add(string text, Image image) { ToolStripItem item = _owner.CreateDefaultItemInternal(text, image, null); Add(item); return item; }
        public ToolStripItem Add(string text, Image image, EventHandler onClick) { ToolStripItem item = _owner.CreateDefaultItemInternal(text, image, onClick); Add(item); return item; }
        public int Add(ToolStripItem value)
        {
            Insert(_list.Count, value);
            return _list.Count - 1;
        }
        public void AddRange(ToolStripItem[] toolStripItems)
        {
            _owner.SuspendItemLayout();
            foreach (ToolStripItem item in toolStripItems) Add(item);
            _owner.ResumeItemLayout();
        }
        public void AddRange(ToolStripItemCollection toolStripItems) { AddRange(toolStripItems.Cast<ToolStripItem>().ToArray()); }
        public void Insert(int index, ToolStripItem value)
        {
            if (value == null) return;
            if (!_readOnly)
            {
                value.OwnerInternal?.Items.Remove(value);
                value.OwnerInternal = _owner;
            }
            _list.Insert(Math.Min(index, _list.Count), value);
            if (!_readOnly)
            {
                value.OnOwnerSet();
                value.RaiseOwnerChanged();
                _owner.OnItemAddedInternal(value);
            }
        }
        public void Remove(ToolStripItem value)
        {
            if (value == null || !_list.Remove(value)) return;
            if (!_readOnly)
            {
                if (value is ToolStripControlHost host && host.Control.Parent == _owner) _owner.Controls.Remove(host.Control);
                value.OwnerInternal = null;
                value.RaiseOwnerChanged();
                _owner.OnItemRemovedInternal(value);
            }
        }
        public void RemoveAt(int index) { Remove(_list[index]); }
        public virtual void RemoveByKey(string key) { int i = IndexOfKey(key); if (i >= 0) RemoveAt(i); }
        public virtual void Clear()
        {
            _owner.SuspendItemLayout();
            foreach (ToolStripItem item in _list.ToArray()) Remove(item);
            _owner.ResumeItemLayout();
        }
        public bool Contains(ToolStripItem value) { return _list.Contains(value); }
        public virtual bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
        public int IndexOf(ToolStripItem value) { return _list.IndexOf(value); }
        public virtual int IndexOfKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return -1;
            for (int i = 0; i < _list.Count; i++) if (string.Equals(_list[i].Name, key, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
        public ToolStripItem[] Find(string key, bool searchAllChildren)
        {
            List<ToolStripItem> found = new List<ToolStripItem>();
            foreach (ToolStripItem item in _list)
            {
                if (string.Equals(item.Name, key, StringComparison.OrdinalIgnoreCase)) found.Add(item);
                if (searchAllChildren && item is ToolStripDropDownItem d && d.HasDropDownItems) found.AddRange(d.DropDownItems.Find(key, true));
            }
            return found.ToArray();
        }
        public void CopyTo(ToolStripItem[] array, int index) { _list.CopyTo(array, index); }
        public override IEnumerator GetEnumerator() { return _list.ToList().GetEnumerator(); }
        internal List<ToolStripItem> List { get { return _list; } }
        int IList.Add(object value) { return Add((ToolStripItem)value); }
        bool IList.Contains(object value) { return value is ToolStripItem i && Contains(i); }
        int IList.IndexOf(object value) { return value is ToolStripItem i ? IndexOf(i) : -1; }
        void IList.Insert(int index, object value) { Insert(index, (ToolStripItem)value); }
        void IList.Remove(object value) { Remove(value as ToolStripItem); }
        void IList.Clear() { Clear(); }
        void IList.RemoveAt(int index) { RemoveAt(index); }
        bool IList.IsFixedSize { get { return false; } }
        bool IList.IsReadOnly { get { return _readOnly; } }
        object IList.this[int index] { get { return _list[index]; } set { throw new NotSupportedException(); } }
    }

    // Strips
    public class ToolStrip : ScrollableControl
    {
        private readonly ToolStripItemCollection _items;
        private ToolStripRenderer _renderer;
        private ToolStripRenderMode _renderMode = ToolStripRenderMode.ManagerRenderMode;
        private ToolStripItem _hotItem;
        private ToolStripItem _pressedItem;
        private ToolStripLayoutStyle _layoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
        private ToolStripGripStyle _gripStyle = ToolStripGripStyle.Visible;
        private int _itemLayoutSuspend;
        private ImageList _imageList;
        private Size _imageScalingSize = new Size(16, 16);
        internal ToolStripDropDown OpenDropDown;

        public ToolStrip()
        {
            _items = new ToolStripItemCollection(this, (ToolStripItem[])null);
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
            Dock = DockStyle.Top;
            Font = SystemFonts.MenuFont;
        }
        public ToolStrip(params ToolStripItem[] items) : this() { _items.AddRange(items); }
        protected override Size DefaultSize { get { return new Size(100, 25); } }
        protected override Padding DefaultPadding { get { return new Padding(0, 0, 1, 0); } }
        protected override Padding DefaultMargin { get { return Padding.Empty; } }
        protected virtual Padding DefaultGripMargin { get { return new Padding(2); } }
        public virtual ToolStripItemCollection Items { get { return _items; } }
        public ToolStripItemCollection DisplayedItems { get { return _items; } }
        public ToolStripRenderer Renderer
        {
            get { return _renderer ?? ToolStripManager.Renderer; }
            set
            {
                _renderer = value;
                _renderMode = value == null ? ToolStripRenderMode.ManagerRenderMode : ToolStripRenderMode.Custom;
                Invalidate();
                RendererChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public ToolStripRenderMode RenderMode
        {
            get { return _renderMode; }
            set
            {
                _renderMode = value;
                if (value == ToolStripRenderMode.System) _renderer = new ToolStripSystemRenderer();
                else if (value == ToolStripRenderMode.Professional) _renderer = new ToolStripProfessionalRenderer();
                else if (value == ToolStripRenderMode.ManagerRenderMode) _renderer = null;
                Invalidate();
            }
        }
        public ToolStripLayoutStyle LayoutStyle { get { return _layoutStyle; } set { _layoutStyle = value; LayoutItems(); } }
        public ToolStripGripStyle GripStyle { get { return _gripStyle; } set { _gripStyle = value; LayoutItems(); } }
        public Padding GripMargin { get; set; } = new Padding(2);
        public Rectangle GripRectangle
        {
            get { return _gripStyle == ToolStripGripStyle.Visible && IsHorizontal ? new Rectangle(2, 0, 7, Height) : Rectangle.Empty; }
        }
        public ToolStripGripDisplayStyle GripDisplayStyle { get { return IsHorizontal ? ToolStripGripDisplayStyle.Vertical : ToolStripGripDisplayStyle.Horizontal; } }
        public bool CanOverflow { get; set; } = true;
        public bool Stretch { get; set; }
        public bool ShowItemToolTips { get; set; } = true;
        public bool AllowItemReorder { get; set; }
        public bool AllowMerge { get; set; } = true;
        public ImageList ImageList { get { return _imageList; } set { _imageList = value; } }
        public Size ImageScalingSize { get { return _imageScalingSize; } set { _imageScalingSize = value; LayoutItems(); } }
        public ToolStripTextDirection TextDirection { get; set; } = ToolStripTextDirection.Horizontal;
        public Orientation Orientation { get { return IsHorizontal ? Orientation.Horizontal : Orientation.Vertical; } }
        public ToolStripOverflowButton OverflowButton { get; } = new ToolStripOverflowButton();
        public bool IsCurrentlyDragging { get { return false; } }
        public bool IsDropDown { get { return this is ToolStripDropDown; } }
        public override DockStyle Dock { get { return base.Dock; } set { base.Dock = value; LayoutItems(); } }
        internal virtual bool IsHorizontal
        {
            get { return _layoutStyle != ToolStripLayoutStyle.VerticalStackWithOverflow && Dock != DockStyle.Left && Dock != DockStyle.Right; }
        }
        internal ToolStripItem HotItem
        {
            get { return _hotItem; }
            set
            {
                if (_hotItem == value) return;
                ToolStripItem old = _hotItem;
                _hotItem = value;
                if (old != null) Toolkit.Dispatch(() => old.RaiseMouseLeave());
                if (value != null) Toolkit.Dispatch(() => value.RaiseMouseEnter());
                Invalidate();
                UpdateToolTip();
            }
        }
        internal ToolStripItem PressedItem { get { return _pressedItem; } set { _pressedItem = value; Invalidate(); } }

        public event ToolStripItemClickedEventHandler ItemClicked;
        public event ToolStripItemEventHandler ItemAdded;
        public event ToolStripItemEventHandler ItemRemoved;
        public event EventHandler RendererChanged;
        public event EventHandler LayoutCompleted;
        public event EventHandler BeginDrag;
        public event EventHandler EndDrag;
        public event PaintEventHandler PaintGrip;
        protected virtual void OnItemClicked(ToolStripItemClickedEventArgs e) { ItemClicked?.Invoke(this, e); }
        protected virtual void OnItemAdded(ToolStripItemEventArgs e) { ItemAdded?.Invoke(this, e); }
        protected virtual void OnItemRemoved(ToolStripItemEventArgs e) { ItemRemoved?.Invoke(this, e); }
        protected virtual void OnLayoutCompleted(EventArgs e) { LayoutCompleted?.Invoke(this, e); }
        protected virtual ToolStripItem CreateDefaultItem(string text, Image image, EventHandler onClick)
        {
            if (text == "-") return new ToolStripSeparator();
            return new ToolStripButton(text, image, onClick);
        }
        internal ToolStripItem CreateDefaultItemInternal(string text, Image image, EventHandler onClick) { return CreateDefaultItem(text, image, onClick); }
        internal void OnItemAddedInternal(ToolStripItem item)
        {
            LayoutItems();
            Invalidate();
            OnItemAdded(new ToolStripItemEventArgs(item));
        }
        internal void OnItemRemovedInternal(ToolStripItem item)
        {
            if (_hotItem == item) _hotItem = null;
            if (_pressedItem == item) _pressedItem = null;
            LayoutItems();
            Invalidate();
            OnItemRemoved(new ToolStripItemEventArgs(item));
        }
        internal void SuspendItemLayout() { _itemLayoutSuspend++; }
        internal void ResumeItemLayout()
        {
            if (_itemLayoutSuspend > 0) _itemLayoutSuspend--;
            if (_itemLayoutSuspend == 0) LayoutItems();
        }
        public ToolStripItem GetItemAt(Point point)
        {
            foreach (ToolStripItem item in _items.List)
                if (item.VisibleSelf && item.BoundsInternal.Contains(point)) return item;
            return null;
        }
        public ToolStripItem GetItemAt(int x, int y) { return GetItemAt(new Point(x, y)); }
        public virtual ToolStripItem GetNextItem(ToolStripItem start, ArrowDirection direction)
        {
            List<ToolStripItem> items = _items.List.Where(i => i.VisibleSelf && i.CanSelect && i.Enabled).ToList();
            if (items.Count == 0) return null;
            int index = start == null ? -1 : items.IndexOf(start);
            bool forward = direction == ArrowDirection.Down || direction == ArrowDirection.Right;
            index = forward ? (index + 1) % items.Count : (index <= 0 ? items.Count - 1 : index - 1);
            return items[index];
        }
        public void ResetMinimumSize() { MinimumSize = Size.Empty; }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size content = ToolStripLayout.PreferredStripSize(this);
            return content;
        }
        internal override Size GetDockedSize(Size proposed)
        {
            if (!AutoSizeStrip) return proposed;
            Size preferred = GetPreferredSize(Size.Empty);
            return IsHorizontal ? new Size(proposed.Width, preferred.Height) : new Size(preferred.Width, proposed.Height);
        }
        internal virtual bool AutoSizeStrip { get { return true; } }
        public override bool AutoSize { get { return true; } set { } }
        // Places the items in the strip
        internal virtual void LayoutItems()
        {
            if (_itemLayoutSuspend > 0) return;
            ToolStripLayout.Layout(this);
            OnLayoutCompleted(EventArgs.Empty);
            Invalidate();
            // The panel arranges the strips in rows by their preferred sizes: a strip that grew or shrank moves the others
            if (Parent is ToolStripPanel panel && GetPreferredSize(Size.Empty) != Size) panel.PerformLayout();
        }
        internal override void LayoutChildren()
        {
            // The strip places the hosted controls; the anchors of the base layout would move them back to their first places
            base.LayoutChildren();
            ToolStripLayout.Layout(this);
        }
        protected override void OnFontChanged(EventArgs e) { base.OnFontChanged(e); LayoutItems(); }

        // Painting
        internal override bool PaintsItself { get { return true; } }
        internal override Color PaintedBackColor { get { return Color.Empty; } }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Renderer.DrawToolStripBackground(new ToolStripRenderEventArgs(e.Graphics, this));
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ToolStripRenderer renderer = Renderer;
            if (this is ToolStripDropDownMenu menu && menu.ShowImageMargin)
                renderer.DrawImageMargin(new ToolStripRenderEventArgs(g, this, new Rectangle(0, 0, menu.ImageMarginWidth, Height), BackColor));
            if (_gripStyle == ToolStripGripStyle.Visible && IsHorizontal && !(this is MenuStrip) && !(this is StatusStrip) && !(this is ToolStripDropDown))
                renderer.DrawGrip(new ToolStripGripRenderEventArgs(g, this));
            foreach (ToolStripItem item in _items.List)
            {
                if (!item.VisibleSelf || item is ToolStripControlHost) continue;
                GraphicsState state = g.Save();
                Rectangle b = item.BoundsInternal;
                g.TranslateTransform(b.X, b.Y);
                ToolStripLayout.PaintItem(renderer, g, item, this);
                if (item.HasPaintHandlers) item.RaisePaint(new PaintEventArgs(g, new Rectangle(Point.Empty, b.Size)));
                g.Restore(state);
            }
            renderer.DrawToolStripBorder(new ToolStripRenderEventArgs(g, this));
            base.OnPaint(e);
        }

        // Mouse
        internal override bool CanSelectOnClick { get { return false; } }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            ToolStripItem item = GetItemAt(e.Location);
            if (item != null && !item.CanSelect && !(item is ToolStripLabel)) item = null;
            HotItem = item;
            if (item != null) item.RaiseMouseMove(new MouseEventArgs(e.Button, e.Clicks, e.X - item.Bounds.X, e.Y - item.Bounds.Y, e.Delta));
            // Moving over the menu bar while a menu is open opens the menu of the item
            if (item is ToolStripDropDownItem dd && OpenDropDown != null && OpenDropDown.OwnerItem != item && item.Enabled && this is MenuStrip)
            {
                OpenDropDown.Close(ToolStripDropDownCloseReason.AppClicked);
                dd.ShowDropDown();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            if (!(this is ToolStripDropDown) || OpenDropDown == null) HotItem = OpenDropDown?.OwnerItem;
            base.OnMouseLeave(e);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            ToolStripItem item = GetItemAt(e.Location);
            if (item != null && item.Enabled)
            {
                PressedItem = item;
                item.RaiseMouseDown(new MouseEventArgs(e.Button, e.Clicks, e.X - item.Bounds.X, e.Y - item.Bounds.Y, e.Delta));
                if (e.Clicks == 2 && item.DoubleClickEnabled) item.RaiseDoubleClick();
                // Menu items of the menu bar and drop-down buttons open on mouse down
                if (e.Button == MouseButtons.Left && item is ToolStripDropDownItem dd && !(item is ToolStripSplitButton) && !(this is ToolStripDropDown))
                {
                    if (dd.DropDownIsOpen) dd.HideDropDown();
                    else dd.ShowDropDown();
                    PressedItem = null;
                }
            }
            base.OnMouseDown(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            ToolStripItem item = GetItemAt(e.Location);
            ToolStripItem pressed = _pressedItem;
            PressedItem = null;
            if (item != null && item.Enabled)
            {
                item.RaiseMouseUp(new MouseEventArgs(e.Button, e.Clicks, e.X - item.Bounds.X, e.Y - item.Bounds.Y, e.Delta));
                if (item == pressed && e.Button == MouseButtons.Left && item.IsClickable) ClickItem(item, e.Location);
            }
            base.OnMouseUp(e);
        }
        internal void ClickItem(ToolStripItem item, Point location)
        {
            if (item is ToolStripSplitButton split)
            {
                split.ClickAt(location);
                OnItemClicked(new ToolStripItemClickedEventArgs(item));
                return;
            }
            if (item is ToolStripDropDownItem dd && dd.HasDropDownItems)
            {
                if (this is ToolStripDropDown) dd.ShowDropDown();
                return;
            }
            if (item is ToolStripDropDownItem && !(this is ToolStripDropDown)) return;
            // A click on a menu item closes all menus before the item runs its action
            ToolStripDropDown.CloseAll(ToolStripDropDownCloseReason.ItemClicked);
            OnItemClicked(new ToolStripItemClickedEventArgs(item));
            if (item.OwnerItem is ToolStripDropDownItem parentItem) parentItem.OnDropDownItemClicked(new ToolStripItemClickedEventArgs(item));
            item.RaiseClick();
        }
        private void UpdateToolTip()
        {
            AC.Control native = NativeControl;
            ToolStripItem item = _hotItem;
            string tip = null;
            if (ShowItemToolTips && item != null && !(this is ToolStripDropDown))
            {
                tip = item.ToolTipText;
                if (string.IsNullOrEmpty(tip) && item.AutoToolTip && !item.ShowsText) tip = item.DisplayText;
            }
            AC.ToolTip.SetTip(native, string.IsNullOrEmpty(tip) ? null : tip);
            if (string.IsNullOrEmpty(tip)) AC.ToolTip.SetIsOpen(native, false);
        }

        // Shortcuts
        internal virtual bool ProcessShortcut(Keys keys)
        {
            if (!Enabled) return false;
            return ProcessShortcut(_items, keys);
        }
        private static bool ProcessShortcut(ToolStripItemCollection items, Keys keys)
        {
            foreach (ToolStripItem item in items.List)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    if (menuItem.ShortcutKeys == keys && menuItem.EnabledSelf && keys != Keys.None)
                    {
                        Toolkit.Dispatch(() =>
                        {
                            menuItem.OwnerInternal?.ProcessShortcutOpening(menuItem);
                            menuItem.PerformClick();
                        });
                        return true;
                    }
                    if (menuItem.HasDropDownItems && ProcessShortcut(menuItem.DropDownItems, keys)) return true;
                }
            }
            return false;
        }
        // The drop-down owners update the item states (Opening events) before a shortcut runs
        internal void ProcessShortcutOpening(ToolStripMenuItem item)
        {
            if (item.OwnerItem is ToolStripDropDownItem owner) owner.OnDropDownOpening(EventArgs.Empty);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) foreach (ToolStripItem item in _items.List.ToArray()) item.Dispose();
            base.Dispose(disposing);
        }
    }

    public enum ArrowDirection { Left = 0, Up = 1, Right = 16, Down = 17 }

    public enum ToolStripGripDisplayStyle { Horizontal = 0, Vertical = 1 }

    public class ToolStripOverflowButton : ToolStripDropDownButton
    {
        public bool HasDropDownItems_ { get { return false; } }
    }

    public class MenuStrip : ToolStrip
    {
        public MenuStrip()
        {
            GripStyle = ToolStripGripStyle.Hidden;
            Padding = new Padding(6, 2, 0, 2);
            CanOverflow = false;
        }
        protected override Size DefaultSize { get { return new Size(200, 24); } }
        public ToolStripMenuItem MdiWindowListItem { get; set; }
        public event EventHandler MenuActivate;
        public event EventHandler MenuDeactivate;
        protected override ToolStripItem CreateDefaultItem(string text, Image image, EventHandler onClick)
        {
            if (text == "-") return new ToolStripSeparator();
            return new ToolStripMenuItem(text, image, onClick);
        }
    }

    public class StatusStrip : ToolStrip
    {
        public StatusStrip()
        {
            Dock = DockStyle.Bottom;
            GripStyle = ToolStripGripStyle.Hidden;
            LayoutStyle = ToolStripLayoutStyle.Table;
            Padding = new Padding(1, 0, 14, 0);
            CanOverflow = false;
        }
        protected override Size DefaultSize { get { return new Size(200, 22); } }
        public bool SizingGrip { get; set; } = true;
        public Rectangle SizeGripBounds { get { return SizingGrip ? new Rectangle(Width - 12, Height - 12, 12, 12) : Rectangle.Empty; } }
        protected override ToolStripItem CreateDefaultItem(string text, Image image, EventHandler onClick)
        {
            if (text == "-") return new ToolStripSeparator();
            return new ToolStripStatusLabel(text, image);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (SizingGrip) Renderer.DrawStatusStripSizingGrip(new ToolStripRenderEventArgs(e.Graphics, this));
        }
    }

    public class ToolStripDropDown : ToolStrip
    {
        private static readonly List<ToolStripDropDown> _openDropDowns = new List<ToolStripDropDown>();
        private Popup _popup;
        private ToolStrip _sourceStrip;
        private Control _sourceControl;
        private bool _closing;

        public ToolStripDropDown()
        {
            Dock = DockStyle.None;
            GripStyle = ToolStripGripStyle.Hidden;
            LayoutStyle = ToolStripLayoutStyle.VerticalStackWithOverflow;
            Padding = new Padding(1, 2, 1, 2);
            base.SetVisibleCore(false);
        }
        public ToolStripItem OwnerItem { get; set; }
        public bool AutoClose { get; set; } = true;
        // An auto complete list keeps the focus in its editor
        public bool TakesFocus { get; set; } = true;
        public bool DropShadowEnabled { get; set; } = true;
        public double Opacity { get; set; } = 1;
        public bool TopLevel { get; set; } = true;
        public new bool Visible { get { return _popup != null && _popup.IsOpen; } set { if (!value) Close(ToolStripDropDownCloseReason.CloseCalled); } }
        public event CancelEventHandler Opening;
        public event EventHandler Opened;
        public event ToolStripDropDownClosingEventHandler Closing;
        public event ToolStripDropDownClosedEventHandler Closed;
        protected virtual void OnOpening(CancelEventArgs e) { Opening?.Invoke(this, e); }
        protected virtual void OnOpened(EventArgs e) { Opened?.Invoke(this, e); }
        protected virtual void OnClosing(ToolStripDropDownClosingEventArgs e) { Closing?.Invoke(this, e); }
        protected virtual void OnClosed(ToolStripDropDownClosedEventArgs e) { Closed?.Invoke(this, e); }
        internal Control SourceControlInternal { get { return _sourceControl; } }
        internal override bool IsHorizontal { get { return false; } }
        internal override bool AutoSizeStrip { get { return true; } }

        // Opening
        internal void ShowFrom(ToolStrip strip, Point local, ToolStripDropDownItem item)
        {
            _sourceStrip = strip;
            _sourceControl = strip is ToolStripDropDown d ? d._sourceControl : strip;
            strip.OpenDropDown?.Close(ToolStripDropDownCloseReason.AppClicked);
            OpenAt(strip, local);
        }
        public void Show() { Show(Input.MousePosition); }
        public void Show(Point screenLocation)
        {
            Form form = Form.ActiveForm ?? (Application.OpenForms.Count > 0 ? Application.OpenForms[0] : null);
            if (form == null) return;
            Show(form, form.PointToClient(screenLocation));
        }
        public void Show(int x, int y) { Show(new Point(x, y)); }
        public void Show(Control control, Point position)
        {
            if (control == null) return;
            _sourceStrip = null;
            _sourceControl = control;
            CloseAll(ToolStripDropDownCloseReason.AppClicked);
            OpenAt(control, position);
        }
        public void Show(Control control, int x, int y) { Show(control, new Point(x, y)); }
        public void Show(Control control, Point position, ToolStripDropDownDirection direction) { Show(control, position); }
        public void Show(Point position, ToolStripDropDownDirection direction) { Show(position); }
        private void OpenAt(Control anchor, Point local)
        {
            CancelEventArgs opening = new CancelEventArgs();
            Toolkit.Dispatch(() => OnOpening(opening));
            if (OwnerItem is ToolStripDropDownItem owner && opening.Cancel == false) { }
            if (opening.Cancel || Items.Count == 0) return;
            LayoutItems();
            Size size = GetPreferredSize(Size.Empty);
            SetBoundsCoreFromLayout(0, 0, size.Width, size.Height);
            LayoutItems();
            AC.Control anchorNative = anchor.NativeControl;
            Avalonia.Point origin = new Avalonia.Point(local.X + anchor.NonClientInsets.Left, local.Y + anchor.NonClientInsets.Top);
            if (_popup == null)
            {
                _popup = new Popup
                {
                    IsLightDismissEnabled = true,
                    OverlayDismissEventPassThrough = false,
                    Placement = AC.PlacementMode.AnchorAndGravity,
                    PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft,
                    PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight,
                    PlacementConstraintAdjustment = Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.FlipY |
                                                    Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.FlipX |
                                                    Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideX |
                                                    Avalonia.Controls.Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY,
                };
                _popup.Closed += (s, e) => OnPopupClosed();
                _popup.AddHandler(AC.Control.KeyDownEvent, OnPopupKeyDown, RoutingStrategies.Tunnel);
            }
            if (_popup.Parent is AC.Panel oldPanel) oldPanel.Children.Remove(_popup);
            if (anchorNative is AC.Panel panel) panel.Children.Add(_popup);
            _popup.PlacementTarget = anchorNative;
            _popup.PlacementRect = new Avalonia.Rect(origin, new Avalonia.Size(1, 1));
            // The items of the drop-down still hang in the frame of its last opening, which lets them go first
            if (Host.Parent is AC.Decorator oldFrame) oldFrame.Child = null;
            _popup.Child = new AC.Border
            {
                Child = Host,
                Width = size.Width,
                Height = size.Height,
                BoxShadow = new Avalonia.Media.BoxShadows(new Avalonia.Media.BoxShadow { OffsetX = 2, OffsetY = 2, Blur = 4, Color = Avalonia.Media.Color.FromArgb(70, 0, 0, 0) }),
            };
            base.SetVisibleCore(true);
            CreateControl();
            _popup.IsOpen = true;
            if (!_openDropDowns.Contains(this)) _openDropDowns.Add(this);
            if (_sourceStrip != null) _sourceStrip.OpenDropDown = this;
            HotItem = null;
            if (TakesFocus)
            {
                Host.Focusable = true;
                Dispatcher.UIThread.Post(() => Host.Focus(), DispatcherPriority.Input);
            }
            _popup.IsLightDismissEnabled = AutoClose;
            if (OwnerItem is ToolStripDropDownItem dd) Toolkit.Dispatch(() => dd.RaiseDropDownOpened());
            Toolkit.Dispatch(() => OnOpened(EventArgs.Empty));
        }
        public void Close() { Close(ToolStripDropDownCloseReason.CloseCalled); }
        public void Close(ToolStripDropDownCloseReason reason)
        {
            if (_popup == null || !_popup.IsOpen || _closing) return;
            ToolStripDropDownClosingEventArgs closing = new ToolStripDropDownClosingEventArgs(reason);
            Toolkit.Dispatch(() => OnClosing(closing));
            if (closing.Cancel) return;
            _closeReason = reason;
            _closing = true;
            // Submenus close first
            if (OpenDropDown != null) OpenDropDown.Close(reason);
            _popup.IsOpen = false;
            _closing = false;
        }
        private ToolStripDropDownCloseReason _closeReason = ToolStripDropDownCloseReason.AppClicked;
        private void OnPopupClosed()
        {
            _openDropDowns.Remove(this);
            OpenDropDown?.Close(_closeReason);
            base.SetVisibleCore(false);
            if (_sourceStrip != null)
            {
                if (_sourceStrip.OpenDropDown == this) _sourceStrip.OpenDropDown = null;
                _sourceStrip.HotItem = null;
                _sourceStrip.Invalidate();
            }
            if (_popup != null)
            {
                _popup.Child = null;
                if (_popup.Parent is AC.Panel panel) panel.Children.Remove(_popup);
            }
            ToolStripDropDownCloseReason reason = _closeReason;
            _closeReason = ToolStripDropDownCloseReason.AppClicked;
            if (OwnerItem is ToolStripDropDownItem dd) Toolkit.Dispatch(() => dd.RaiseDropDownClosed());
            Toolkit.Dispatch(() => OnClosed(new ToolStripDropDownClosedEventArgs(reason)));
            // A menu closed by a click outside closes its parent menus too
            if (reason == ToolStripDropDownCloseReason.AppClicked && _sourceStrip is ToolStripDropDown parent && !IsPointerOver(parent)) { }
        }
        private static bool IsPointerOver(ToolStripDropDown dropDown) { return dropDown.MouseIsInside; }
        public static void CloseAll(ToolStripDropDownCloseReason reason)
        {
            foreach (ToolStripDropDown d in _openDropDowns.ToArray()) d.Close(reason);
        }
        private void OnPopupKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close(ToolStripDropDownCloseReason.Keyboard);
                    e.Handled = true;
                    break;
                case Key.Down:
                    HotItem = GetNextItem(HotItem, ArrowDirection.Down);
                    e.Handled = true;
                    break;
                case Key.Up:
                    HotItem = GetNextItem(HotItem, ArrowDirection.Up);
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (HotItem is ToolStripDropDownItem dd && dd.HasDropDownItems) { dd.ShowDropDown(); e.Handled = true; }
                    break;
                case Key.Left:
                    if (_sourceStrip is ToolStripDropDown) { Close(ToolStripDropDownCloseReason.Keyboard); e.Handled = true; }
                    break;
                case Key.Enter:
                case Key.Space:
                    if (HotItem != null && HotItem.Enabled) { ToolStripItem item = HotItem; Toolkit.Dispatch(() => ClickItem(item, item.Bounds.Location)); }
                    e.Handled = true;
                    break;
            }
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            // Hovering a menu item with a submenu opens the submenu
            ToolStripItem item = HotItem;
            if (item is ToolStripDropDownItem dd && dd.HasDropDownItems && item.Enabled)
            {
                if (OpenDropDown == null || OpenDropDown.OwnerItem != item)
                {
                    OpenDropDown?.Close(ToolStripDropDownCloseReason.AppFocusChange);
                    StartSubmenuTimer(dd);
                }
            }
            else if (item != null && OpenDropDown != null)
            {
                StartSubmenuTimer(null);
            }
        }
        private DispatcherTimer _submenuTimer;
        private ToolStripDropDownItem _submenuItem;
        private void StartSubmenuTimer(ToolStripDropDownItem item)
        {
            _submenuItem = item;
            if (_submenuTimer == null)
            {
                _submenuTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _submenuTimer.Tick += (s, e) =>
                {
                    _submenuTimer.Stop();
                    if (HotItem != _submenuItem && _submenuItem != null) return;
                    if (_submenuItem == null) OpenDropDown?.Close(ToolStripDropDownCloseReason.AppFocusChange);
                    else if (Visible) Toolkit.Dispatch(() => _submenuItem.ShowDropDown());
                };
            }
            _submenuTimer.Stop();
            _submenuTimer.Start();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && _popup != null) _popup.IsOpen = false;
            base.Dispose(disposing);
        }
    }

    public class ToolStripDropDownMenu : ToolStripDropDown
    {
        public ToolStripDropDownMenu() { }
        public bool ShowImageMargin { get; set; } = true;
        public bool ShowCheckMargin { get; set; }
        internal int ImageMarginWidth { get { return ShowImageMargin || ShowCheckMargin ? Math.Max(24, ImageScalingSize.Width + 8) : 0; } }
        protected override Padding DefaultPadding { get { return new Padding(1, 2, 1, 2); } }
        protected override ToolStripItem CreateDefaultItem(string text, Image image, EventHandler onClick)
        {
            if (text == "-") return new ToolStripSeparator();
            return new ToolStripMenuItem(text, image, onClick);
        }
        // Size of a menu item: image margin, text, shortcut and arrow columns of the menu
        internal Size MenuItemSize(ToolStripMenuItem item)
        {
            int textWidth = ContentLayout.MeasureText(item.DisplayText, item.Font).Width;
            int shortcutWidth = 0;
            foreach (ToolStripItem other in Items.List)
            {
                if (other is ToolStripMenuItem mi && mi.ShortcutText != null)
                    shortcutWidth = Math.Max(shortcutWidth, ContentLayout.MeasureText(mi.ShortcutText, mi.Font).Width);
            }
            int width = ImageMarginWidth + 8 + textWidth + (shortcutWidth > 0 ? 30 + shortcutWidth : 0) + 25;
            return new Size(width, 22);
        }
    }

    public class ContextMenuStrip : ToolStripDropDownMenu
    {
        public ContextMenuStrip() { }
        public ContextMenuStrip(IContainer container) : this() { container?.Add(this); }
        public Control SourceControl { get { return SourceControlInternal; } }
    }

    // Layout of the items of a strip
    internal static class ToolStripLayout
    {
        public static Size ContentSize(ToolStripItem item)
        {
            Size text = item.ShowsText ? ContentLayout.MeasureText(item.DisplayText, item.Font) : Size.Empty;
            Size image = item.ShowsImage ? (item.ImageScaling == ToolStripItemImageScaling.SizeToFit && item.OwnerInternal != null ?
                                            item.OwnerInternal.ImageScalingSize : item.Image.Size) : Size.Empty;
            if (item is ToolStripSeparator) return new Size(6, 6);
            int w, h;
            if (item.ShowsImage && item.ShowsText)
            {
                if (item.TextImageRelation == TextImageRelation.ImageAboveText || item.TextImageRelation == TextImageRelation.TextAboveImage)
                {
                    w = Math.Max(text.Width, image.Width) + 6;
                    h = text.Height + image.Height + 6;
                }
                else
                {
                    w = text.Width + image.Width + 9;
                    h = Math.Max(text.Height, image.Height) + 6;
                }
            }
            else if (item.ShowsImage)
            {
                w = image.Width + 7;
                h = image.Height + 6;
            }
            else
            {
                w = text.Width + 6;
                h = Math.Max(text.Height + 4, 19);
            }
            if (item is ToolStripLabel && !item.ShowsText && !item.ShowsImage) w = Math.Max(w, 0);
            return new Size(w, Math.Max(h, 22));
        }
        public static Size PreferredStripSize(ToolStrip strip)
        {
            Padding p = strip.Padding;
            bool horizontal = strip.IsHorizontal;
            int w = 0, h = 0;
            int grip = GripWidth(strip);
            foreach (ToolStripItem item in strip.Items.List)
            {
                if (!item.VisibleSelf) continue;
                Size s = item.GetPreferredSize(Size.Empty);
                Padding m = item.Margin;
                if (horizontal)
                {
                    w += s.Width + m.Horizontal;
                    h = Math.Max(h, s.Height + m.Vertical);
                }
                else
                {
                    if (item is ToolStripSeparator) s = new Size(0, 6);
                    w = Math.Max(w, s.Width + m.Horizontal);
                    h += s.Height + m.Vertical;
                }
            }
            if (horizontal) return new Size(w + p.Horizontal + grip, Math.Max(h + p.Vertical, strip is StatusStrip ? 22 : strip is MenuStrip ? 24 : 25));
            return new Size(Math.Max(w + p.Horizontal, 40), h + p.Vertical);
        }
        private static int GripWidth(ToolStrip strip)
        {
            return strip.GripStyle == ToolStripGripStyle.Visible && strip.IsHorizontal && !(strip is MenuStrip) && !(strip is StatusStrip) &&
                   !(strip is ToolStripDropDown) ? 11 : 0;
        }
        public static void Layout(ToolStrip strip)
        {
            Padding p = strip.Padding;
            Size client = strip.ClientSize;
            if (strip.IsHorizontal)
            {
                int left = p.Left + GripWidth(strip);
                int right = client.Width - p.Right;
                int top = p.Top, height = client.Height - p.Vertical;
                List<ToolStripItem> springs = new List<ToolStripItem>();
                int used = 0;
                foreach (ToolStripItem item in strip.Items.List)
                {
                    if (!item.VisibleSelf) continue;
                    Size s = item.GetPreferredSize(Size.Empty);
                    if (item is ToolStripStatusLabel sl && sl.Spring) springs.Add(item);
                    else used += s.Width + item.Margin.Horizontal;
                }
                int springWidth = springs.Count > 0 ? Math.Max(0, (right - left - used) / springs.Count) : 0;
                foreach (ToolStripItem item in strip.Items.List)
                {
                    if (!item.VisibleSelf)
                    {
                        item.BoundsInternal = Rectangle.Empty;
                        HideHost(item);
                        continue;
                    }
                    Size s = item.GetPreferredSize(Size.Empty);
                    Padding m = item.Margin;
                    int w = springs.Contains(item) ? Math.Max(0, springWidth - m.Horizontal) : s.Width;
                    int h = item is ToolStripSeparator ? height - m.Vertical : Math.Min(s.Height, height - m.Vertical);
                    if (item.Alignment == ToolStripItemAlignment.Right)
                    {
                        right -= w + m.Right;
                        item.BoundsInternal = new Rectangle(right, top + m.Top + (height - m.Vertical - h) / 2, w, h);
                        right -= m.Left;
                    }
                    else
                    {
                        left += m.Left;
                        item.BoundsInternal = new Rectangle(left, top + m.Top + (height - m.Vertical - h) / 2, w, h);
                        left += w + m.Right;
                    }
                    PlaceHost(item);
                }
            }
            else
            {
                int top = p.Top;
                int width = client.Width - p.Horizontal;
                foreach (ToolStripItem item in strip.Items.List)
                {
                    if (!item.VisibleSelf)
                    {
                        item.BoundsInternal = Rectangle.Empty;
                        HideHost(item);
                        continue;
                    }
                    Size s = item is ToolStripSeparator ? new Size(width, 6) : item.GetPreferredSize(Size.Empty);
                    Padding m = item.Margin;
                    item.BoundsInternal = new Rectangle(p.Left + m.Left, top + m.Top, width - m.Horizontal, s.Height);
                    top += s.Height + m.Vertical;
                    PlaceHost(item);
                }
            }
        }
        private static void HideHost(ToolStripItem item)
        {
            if (item is ToolStripControlHost host && host.Control.Parent == item.OwnerInternal) host.Control.Visible = false;
        }
        private static void PlaceHost(ToolStripItem item)
        {
            if (item is ToolStripControlHost host && host.Control.Parent == item.OwnerInternal)
            {
                Rectangle b = item.BoundsInternal;
                Control c = host.Control;
                int h = Math.Min(c.Height, b.Height);
                c.SetBoundsCoreFromLayout(b.X, b.Y + (b.Height - h) / 2, b.Width, c.Height);
                c.Visible = item.VisibleSelf;
            }
        }
        public static void PaintItem(ToolStripRenderer renderer, Graphics g, ToolStripItem item, ToolStrip strip)
        {
            if (item is ToolStripSeparator)
            {
                renderer.DrawSeparator(new ToolStripSeparatorRenderEventArgs(g, (ToolStripSeparator)item, !strip.IsHorizontal));
                return;
            }
            Rectangle bounds = new Rectangle(Point.Empty, item.Bounds.Size);
            ToolStripItemRenderEventArgs args = new ToolStripItemRenderEventArgs(g, item);
            if (item is ToolStripMenuItem) renderer.DrawMenuItemBackground(args);
            else if (item is ToolStripSplitButton) renderer.DrawSplitButton(args);
            else if (item is ToolStripDropDownButton) renderer.DrawDropDownButtonBackground(args);
            else if (item is ToolStripButton) renderer.DrawButtonBackground(args);
            else if (item is ToolStripStatusLabel) renderer.DrawItemBackground(args);
            else if (item is ToolStripLabel) renderer.DrawLabelBackground(args);
            else renderer.DrawItemBackground(args);
            bool onMenu = strip is ToolStripDropDownMenu;
            int imageMargin = onMenu ? ((ToolStripDropDownMenu)strip).ImageMarginWidth : 0;
            Rectangle imageRect = Rectangle.Empty;
            Size imageSize = item.ShowsImage ? (item.ImageScaling == ToolStripItemImageScaling.SizeToFit ? strip.ImageScalingSize : item.Image.Size) : Size.Empty;
            Rectangle textRect;
            if (onMenu)
            {
                imageRect = new Rectangle((imageMargin - imageSize.Width) / 2, (bounds.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
                textRect = new Rectangle(imageMargin + 8, 0, bounds.Width - imageMargin - 8, bounds.Height);
            }
            else if (item.ShowsImage && item.ShowsText)
            {
                imageRect = new Rectangle(3, (bounds.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
                textRect = new Rectangle(imageRect.Right + 3, 0, bounds.Width - imageRect.Right - 3, bounds.Height);
            }
            else if (item.ShowsImage)
            {
                imageRect = new Rectangle((bounds.Width - imageSize.Width) / 2, (bounds.Height - imageSize.Height) / 2, imageSize.Width, imageSize.Height);
                textRect = Rectangle.Empty;
            }
            else textRect = new Rectangle(0, 0, bounds.Width - (item is ToolStripDropDownButton ? 10 : 0), bounds.Height);
            if (item is ToolStripMenuItem mi && mi.Checked && onMenu)
                renderer.DrawItemCheck(new ToolStripItemImageRenderEventArgs(g, item, mi.Image, new Rectangle(2, (bounds.Height - 20) / 2, imageMargin - 4, 20)));
            if (item.ShowsImage)
                renderer.DrawItemImage(new ToolStripItemImageRenderEventArgs(g, item, item.Image, imageRect));
            if (item.ShowsText || onMenu)
            {
                ContentAlignment align = onMenu || (item.ShowsImage && item.ShowsText) ? ContentAlignment.MiddleLeft : item.TextAlign;
                renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(g, item, item.DisplayText, textRect, item.Enabled ? item.ForeColor : SystemColors.GrayText,
                                                                           item.Font, align));
                if (item is ToolStripMenuItem menuItem && onMenu && menuItem.ShortcutText != null)
                {
                    Rectangle shortcutRect = new Rectangle(0, 0, bounds.Width - 22, bounds.Height);
                    renderer.DrawItemText(new ToolStripItemTextRenderEventArgs(g, item, menuItem.ShortcutText, shortcutRect,
                                                                               item.Enabled ? item.ForeColor : SystemColors.GrayText, item.Font,
                                                                               ContentAlignment.MiddleRight));
                }
            }
            if (item is ToolStripDropDownItem dd)
            {
                if (onMenu && dd.HasDropDownItems)
                    renderer.DrawArrow(new ToolStripArrowRenderEventArgs(g, item, new Rectangle(bounds.Width - 17, 0, 10, bounds.Height),
                                                                         item.Enabled ? SystemColors.ControlText : SystemColors.GrayText, ArrowDirection.Right));
                else if (item is ToolStripDropDownButton b && b.ShowDropDownArrow)
                    renderer.DrawArrow(new ToolStripArrowRenderEventArgs(g, item, new Rectangle(bounds.Width - 11, 0, 9, bounds.Height),
                                                                         item.Enabled ? SystemColors.ControlText : SystemColors.GrayText, ArrowDirection.Down));
                else if (item is ToolStripSplitButton s)
                    renderer.DrawArrow(new ToolStripArrowRenderEventArgs(g, item, new Rectangle(bounds.Width - s.DropDownButtonWidth, 0, s.DropDownButtonWidth, bounds.Height),
                                                                         item.Enabled ? SystemColors.ControlText : SystemColors.GrayText, ArrowDirection.Down));
            }
        }
        public static string ShortcutToText(Keys keys)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if ((keys & Keys.Control) != 0) sb.Append("Ctrl+");
            if ((keys & Keys.Shift) != 0) sb.Append("Shift+");
            if ((keys & Keys.Alt) != 0) sb.Append("Alt+");
            Keys key = keys & Keys.KeyCode;
            string name;
            switch (key)
            {
                case Keys.Delete: name = "Del"; break;
                case Keys.Insert: name = "Ins"; break;
                case Keys.Next: name = "PgDn"; break;
                case Keys.Prior: name = "PgUp"; break;
                case Keys.Return: name = "Enter"; break;
                case Keys.Escape: name = "Esc"; break;
                case Keys.Oemplus: name = "+"; break;
                case Keys.OemMinus: name = "-"; break;
                default:
                    name = key >= Keys.D0 && key <= Keys.D9 ? ((int)(key - Keys.D0)).ToString() : key.ToString();
                    break;
            }
            sb.Append(name);
            return sb.ToString();
        }
    }

    // Event arguments
    public class ToolStripItemClickedEventArgs : EventArgs
    {
        public ToolStripItem ClickedItem { get; }
        public ToolStripItemClickedEventArgs(ToolStripItem clickedItem) { ClickedItem = clickedItem; }
    }

    public delegate void ToolStripItemClickedEventHandler(object sender, ToolStripItemClickedEventArgs e);

    public class ToolStripItemEventArgs : EventArgs
    {
        public ToolStripItem Item { get; }
        public ToolStripItemEventArgs(ToolStripItem item) { Item = item; }
    }

    public delegate void ToolStripItemEventHandler(object sender, ToolStripItemEventArgs e);

    public class ToolStripDropDownClosedEventArgs : EventArgs
    {
        public ToolStripDropDownCloseReason CloseReason { get; }
        public ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason reason) { CloseReason = reason; }
    }

    public delegate void ToolStripDropDownClosedEventHandler(object sender, ToolStripDropDownClosedEventArgs e);

    public class ToolStripDropDownClosingEventArgs : CancelEventArgs
    {
        public ToolStripDropDownCloseReason CloseReason { get; }
        public ToolStripDropDownClosingEventArgs(ToolStripDropDownCloseReason reason) { CloseReason = reason; }
    }

    public delegate void ToolStripDropDownClosingEventHandler(object sender, ToolStripDropDownClosingEventArgs e);

    public class ToolStripRenderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ToolStrip ToolStrip { get; }
        public Rectangle AffectedBounds { get; }
        public Color BackColor { get; }
        public Rectangle ConnectedArea { get { return Rectangle.Empty; } }
        public ToolStripRenderEventArgs(Graphics g, ToolStrip toolStrip) : this(g, toolStrip, new Rectangle(Point.Empty, toolStrip.Size), toolStrip.BackColor) { }
        public ToolStripRenderEventArgs(Graphics g, ToolStrip toolStrip, Rectangle affectedBounds, Color backColor)
        {
            Graphics = g;
            ToolStrip = toolStrip;
            AffectedBounds = affectedBounds;
            BackColor = backColor;
        }
    }

    public delegate void ToolStripRenderEventHandler(object sender, ToolStripRenderEventArgs e);

    public class ToolStripGripRenderEventArgs : ToolStripRenderEventArgs
    {
        public ToolStripGripRenderEventArgs(Graphics g, ToolStrip toolStrip) : base(g, toolStrip) { }
        public Rectangle GripBounds { get { return ToolStrip.GripRectangle; } }
        public ToolStripGripDisplayStyle GripDisplayStyle { get { return ToolStrip.GripDisplayStyle; } }
        public ToolStripGripStyle GripStyle { get { return ToolStrip.GripStyle; } }
    }

    public delegate void ToolStripGripRenderEventHandler(object sender, ToolStripGripRenderEventArgs e);

    public class ToolStripItemRenderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ToolStripItem Item { get; }
        public ToolStrip ToolStrip { get { return Item.OwnerInternal; } }
        public ToolStripItemRenderEventArgs(Graphics g, ToolStripItem item)
        {
            Graphics = g;
            Item = item;
        }
    }

    public delegate void ToolStripItemRenderEventHandler(object sender, ToolStripItemRenderEventArgs e);

    public class ToolStripItemTextRenderEventArgs : ToolStripItemRenderEventArgs
    {
        public string Text { get; set; }
        public Color TextColor { get; set; }
        public Font TextFont { get; set; }
        public Rectangle TextRectangle { get; set; }
        public TextFormatFlags TextFormat { get; set; }
        public ToolStripTextDirection TextDirection { get; set; }
        internal ContentAlignment Align;
        public ToolStripItemTextRenderEventArgs(Graphics g, ToolStripItem item, string text, Rectangle textRectangle, Color textColor,
                                                Font textFont, ContentAlignment textAlign) : base(g, item)
        {
            Text = text;
            TextRectangle = textRectangle;
            TextColor = textColor;
            TextFont = textFont;
            Align = textAlign;
        }
        public ToolStripItemTextRenderEventArgs(Graphics g, ToolStripItem item, string text, Rectangle textRectangle, Color textColor,
                                                Font textFont, TextFormatFlags format) : base(g, item)
        {
            Text = text;
            TextRectangle = textRectangle;
            TextColor = textColor;
            TextFont = textFont;
            TextFormat = format;
            Align = ContentAlignment.MiddleLeft;
        }
    }

    public delegate void ToolStripItemTextRenderEventHandler(object sender, ToolStripItemTextRenderEventArgs e);

    public class ToolStripItemImageRenderEventArgs : ToolStripItemRenderEventArgs
    {
        public Image Image { get; }
        public Rectangle ImageRectangle { get; }
        public ToolStripItemImageRenderEventArgs(Graphics g, ToolStripItem item, Rectangle imageRectangle) : this(g, item, item.Image, imageRectangle) { }
        public ToolStripItemImageRenderEventArgs(Graphics g, ToolStripItem item, Image image, Rectangle imageRectangle) : base(g, item)
        {
            Image = image;
            ImageRectangle = imageRectangle;
        }
    }

    public delegate void ToolStripItemImageRenderEventHandler(object sender, ToolStripItemImageRenderEventArgs e);

    public class ToolStripSeparatorRenderEventArgs : ToolStripItemRenderEventArgs
    {
        public bool Vertical { get; }
        public ToolStripSeparatorRenderEventArgs(Graphics g, ToolStripSeparator separator, bool vertical) : base(g, separator) { Vertical = !vertical; }
    }

    public delegate void ToolStripSeparatorRenderEventHandler(object sender, ToolStripSeparatorRenderEventArgs e);

    public class ToolStripArrowRenderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ToolStripItem Item { get; }
        public Rectangle ArrowRectangle { get; set; }
        public Color ArrowColor { get; set; }
        public ArrowDirection Direction { get; set; }
        public ToolStripArrowRenderEventArgs(Graphics g, ToolStripItem toolStripItem, Rectangle arrowRectangle, Color arrowColor, ArrowDirection arrowDirection)
        {
            Graphics = g;
            Item = toolStripItem;
            ArrowRectangle = arrowRectangle;
            ArrowColor = arrowColor;
            Direction = arrowDirection;
        }
    }

    public delegate void ToolStripArrowRenderEventHandler(object sender, ToolStripArrowRenderEventArgs e);

    // Renderers
    public abstract class ToolStripRenderer
    {
        public event ToolStripRenderEventHandler RenderToolStripBackground;
        public event ToolStripRenderEventHandler RenderToolStripBorder;
        public event ToolStripItemRenderEventHandler RenderButtonBackground;
        public event ToolStripItemRenderEventHandler RenderMenuItemBackground;
        public event ToolStripItemTextRenderEventHandler RenderItemText;
        public event ToolStripItemImageRenderEventHandler RenderItemImage;
        public event ToolStripSeparatorRenderEventHandler RenderSeparator;

        public void DrawToolStripBackground(ToolStripRenderEventArgs e) { OnRenderToolStripBackground(e); RenderToolStripBackground?.Invoke(this, e); }
        public void DrawToolStripBorder(ToolStripRenderEventArgs e) { OnRenderToolStripBorder(e); RenderToolStripBorder?.Invoke(this, e); }
        public void DrawImageMargin(ToolStripRenderEventArgs e) { OnRenderImageMargin(e); }
        public void DrawGrip(ToolStripGripRenderEventArgs e) { OnRenderGrip(e); }
        public void DrawButtonBackground(ToolStripItemRenderEventArgs e) { OnRenderButtonBackground(e); RenderButtonBackground?.Invoke(this, e); }
        public void DrawMenuItemBackground(ToolStripItemRenderEventArgs e) { OnRenderMenuItemBackground(e); RenderMenuItemBackground?.Invoke(this, e); }
        public void DrawDropDownButtonBackground(ToolStripItemRenderEventArgs e) { OnRenderDropDownButtonBackground(e); }
        public void DrawSplitButton(ToolStripItemRenderEventArgs e) { OnRenderSplitButtonBackground(e); }
        public void DrawLabelBackground(ToolStripItemRenderEventArgs e) { OnRenderLabelBackground(e); }
        public void DrawItemBackground(ToolStripItemRenderEventArgs e) { OnRenderItemBackground(e); }
        public void DrawItemText(ToolStripItemTextRenderEventArgs e) { OnRenderItemText(e); RenderItemText?.Invoke(this, e); }
        public void DrawItemImage(ToolStripItemImageRenderEventArgs e) { OnRenderItemImage(e); RenderItemImage?.Invoke(this, e); }
        public void DrawItemCheck(ToolStripItemImageRenderEventArgs e) { OnRenderItemCheck(e); }
        public void DrawSeparator(ToolStripSeparatorRenderEventArgs e) { OnRenderSeparator(e); RenderSeparator?.Invoke(this, e); }
        public void DrawArrow(ToolStripArrowRenderEventArgs e) { OnRenderArrow(e); }
        public void DrawStatusStripSizingGrip(ToolStripRenderEventArgs e) { OnRenderStatusStripSizingGrip(e); }
        public void DrawToolStripContentPanelBackground(ToolStripContentPanelRenderEventArgs e) { OnRenderToolStripContentPanelBackground(e); }
        public void DrawToolStripPanelBackground(ToolStripPanelRenderEventArgs e) { OnRenderToolStripPanelBackground(e); }

        protected internal virtual void Initialize(ToolStrip toolStrip) { }
        protected virtual void OnRenderToolStripBackground(ToolStripRenderEventArgs e) { }
        protected virtual void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
        protected virtual void OnRenderImageMargin(ToolStripRenderEventArgs e) { }
        protected virtual void OnRenderGrip(ToolStripGripRenderEventArgs e) { }
        protected virtual void OnRenderButtonBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderLabelBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderItemBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderOverflowButtonBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderToolStripStatusLabelBackground(ToolStripItemRenderEventArgs e) { }
        protected virtual void OnRenderToolStripContentPanelBackground(ToolStripContentPanelRenderEventArgs e) { }
        protected virtual void OnRenderToolStripPanelBackground(ToolStripPanelRenderEventArgs e) { }
        protected virtual void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Text)) return;
            Rectangle r = e.TextRectangle;
            StringFormat format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.None };
            Avalonia.Layout.HorizontalAlignment h = ContentLayout.H(e.Align);
            format.Alignment = h == Avalonia.Layout.HorizontalAlignment.Left ? StringAlignment.Near : h == Avalonia.Layout.HorizontalAlignment.Right ? StringAlignment.Far : StringAlignment.Center;
            format.LineAlignment = StringAlignment.Center;
            using (SolidBrush brush = new SolidBrush(e.TextColor))
                e.Graphics.DrawString(e.Text, e.TextFont, brush, new RectangleF(r.X, r.Y, r.Width, r.Height), format);
        }
        protected virtual void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image == null) return;
            if (e.Item.Enabled) e.Graphics.DrawImage(e.Image, e.ImageRectangle);
            else
            {
                using (Image disabled = CreateDisabledImage(e.Image)) e.Graphics.DrawImage(disabled, e.ImageRectangle);
            }
        }
        protected virtual void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle r = e.ImageRectangle;
            using (SolidBrush back = new SolidBrush(Color.FromArgb(0x90, 0xC8, 0xF6)))
            using (Pen border = new Pen(Color.FromArgb(0x33, 0x99, 0xFF)))
            {
                Rectangle box = new Rectangle(r.X + (r.Width - 20) / 2, r.Y, 20, r.Height);
                e.Graphics.FillRectangle(back, box);
                e.Graphics.DrawRectangle(border, box.X, box.Y, box.Width - 1, box.Height - 1);
                if (e.Image == null)
                {
                    using (Pen check = new Pen(Color.Black, 1.6f))
                    {
                        SmoothingMode mode = e.Graphics.SmoothingMode;
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        int cx = box.X + box.Width / 2, cy = box.Y + box.Height / 2;
                        e.Graphics.DrawLines(check, new[] { new PointF(cx - 4, cy), new PointF(cx - 1, cy + 3), new PointF(cx + 4, cy - 3) });
                        e.Graphics.SmoothingMode = mode;
                    }
                }
            }
        }
        protected virtual void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Rectangle b = new Rectangle(Point.Empty, e.Item.Bounds.Size);
            using (Pen dark = new Pen(Color.FromArgb(0xBD, 0xBD, 0xBD)))
            using (Pen light = new Pen(Color.White))
            {
                if (e.Vertical)
                {
                    int x = b.Width / 2;
                    e.Graphics.DrawLine(dark, x, 4, x, b.Height - 4);
                    e.Graphics.DrawLine(light, x + 1, 4, x + 1, b.Height - 4);
                }
                else
                {
                    int y = b.Height / 2;
                    int left = e.Item.OwnerInternal is ToolStripDropDownMenu menu ? menu.ImageMarginWidth + 2 : 2;
                    e.Graphics.DrawLine(dark, left, y, b.Width - 2, y);
                    e.Graphics.DrawLine(light, left, y + 1, b.Width - 2, y + 1);
                }
            }
        }
        protected virtual void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            Rectangle r = e.ArrowRectangle;
            int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2;
            Point[] points;
            switch (e.Direction)
            {
                case ArrowDirection.Right: points = new[] { new Point(cx - 2, cy - 4), new Point(cx + 2, cy), new Point(cx - 2, cy + 4) }; break;
                case ArrowDirection.Left: points = new[] { new Point(cx + 2, cy - 4), new Point(cx - 2, cy), new Point(cx + 2, cy + 4) }; break;
                case ArrowDirection.Up: points = new[] { new Point(cx - 3, cy + 2), new Point(cx, cy - 1), new Point(cx + 3, cy + 2) }; break;
                default: points = new[] { new Point(cx - 3, cy - 1), new Point(cx, cy + 2), new Point(cx + 3, cy - 1) }; break;
            }
            using (SolidBrush brush = new SolidBrush(e.ArrowColor)) e.Graphics.FillPolygon(brush, points);
        }
        protected virtual void OnRenderStatusStripSizingGrip(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(0xA0, 0xA0, 0xA0)))
            {
                int w = e.ToolStrip.Width, h = e.ToolStrip.Height;
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j <= i; j++)
                        e.Graphics.FillRectangle(brush, w - 4 - 4 * j, h - 4 - 4 * (i - j), 2, 2);
            }
        }
        public static Image CreateDisabledImage(Image normalImage)
        {
            Bitmap result = new Bitmap(normalImage.Width, normalImage.Height);
            Bitmap source = normalImage as Bitmap ?? new Bitmap(normalImage);
            for (int y = 0; y < normalImage.Height; y++)
                for (int x = 0; x < normalImage.Width; x++)
                {
                    Color c = source.GetPixel(x, y);
                    int gray = (int)(c.R * 0.3 + c.G * 0.59 + c.B * 0.11);
                    gray = 120 + gray / 2;
                    result.SetPixel(x, y, Color.FromArgb(c.A / 2, Math.Min(255, gray), Math.Min(255, gray), Math.Min(255, gray)));
                }
            return result;
        }
    }

    public class ToolStripSystemRenderer : ToolStripProfessionalRenderer { }

    public class ProfessionalColorTable
    {
        public bool UseSystemColors { get; set; }
        public virtual Color ToolStripGradientBegin { get { return Color.FromArgb(0xFA, 0xFA, 0xFA); } }
        public virtual Color ToolStripGradientMiddle { get { return Color.FromArgb(0xF5, 0xF5, 0xF5); } }
        public virtual Color ToolStripGradientEnd { get { return Color.FromArgb(0xF0, 0xF0, 0xF0); } }
        public virtual Color ToolStripBorder { get { return Color.FromArgb(0xE0, 0xE0, 0xE0); } }
        public virtual Color MenuStripGradientBegin { get { return SystemColors.MenuBar; } }
        public virtual Color MenuStripGradientEnd { get { return SystemColors.MenuBar; } }
        public virtual Color StatusStripGradientBegin { get { return SystemColors.Control; } }
        public virtual Color StatusStripGradientEnd { get { return SystemColors.Control; } }
        public virtual Color ToolStripDropDownBackground { get { return Color.FromArgb(0xF2, 0xF2, 0xF2); } }
        public virtual Color ImageMarginGradientBegin { get { return Color.FromArgb(0xF2, 0xF2, 0xF2); } }
        public virtual Color ImageMarginGradientMiddle { get { return Color.FromArgb(0xF2, 0xF2, 0xF2); } }
        public virtual Color ImageMarginGradientEnd { get { return Color.FromArgb(0xF2, 0xF2, 0xF2); } }
        public virtual Color MenuBorder { get { return Color.FromArgb(0xCC, 0xCC, 0xCC); } }
        public virtual Color MenuItemBorder { get { return Color.FromArgb(0x99, 0xCC, 0xFF); } }
        public virtual Color MenuItemSelected { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color MenuItemPressedGradientBegin { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color MenuItemPressedGradientMiddle { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color MenuItemPressedGradientEnd { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonSelectedHighlight { get { return Color.FromArgb(0xE5, 0xF3, 0xFF); } }
        public virtual Color ButtonSelectedHighlightBorder { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonSelectedBorder { get { return Color.FromArgb(0x99, 0xD1, 0xFF); } }
        public virtual Color ButtonSelectedGradientBegin { get { return Color.FromArgb(0xE5, 0xF3, 0xFF); } }
        public virtual Color ButtonSelectedGradientMiddle { get { return Color.FromArgb(0xE5, 0xF3, 0xFF); } }
        public virtual Color ButtonSelectedGradientEnd { get { return Color.FromArgb(0xE5, 0xF3, 0xFF); } }
        public virtual Color ButtonPressedHighlight { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonPressedHighlightBorder { get { return Color.FromArgb(0x99, 0xD1, 0xFF); } }
        public virtual Color ButtonPressedBorder { get { return Color.FromArgb(0x99, 0xD1, 0xFF); } }
        public virtual Color ButtonPressedGradientBegin { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonPressedGradientMiddle { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonPressedGradientEnd { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonCheckedHighlight { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonCheckedHighlightBorder { get { return Color.FromArgb(0x99, 0xD1, 0xFF); } }
        public virtual Color ButtonCheckedGradientBegin { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonCheckedGradientMiddle { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color ButtonCheckedGradientEnd { get { return Color.FromArgb(0xCC, 0xE8, 0xFF); } }
        public virtual Color CheckBackground { get { return Color.FromArgb(0x90, 0xC8, 0xF6); } }
        public virtual Color CheckSelectedBackground { get { return Color.FromArgb(0x90, 0xC8, 0xF6); } }
        public virtual Color CheckPressedBackground { get { return Color.FromArgb(0x90, 0xC8, 0xF6); } }
        public virtual Color GripDark { get { return Color.FromArgb(0xA0, 0xA0, 0xA0); } }
        public virtual Color GripLight { get { return Color.White; } }
        public virtual Color SeparatorDark { get { return Color.FromArgb(0xBD, 0xBD, 0xBD); } }
        public virtual Color SeparatorLight { get { return Color.White; } }
        public virtual Color ToolStripContentPanelGradientBegin { get { return SystemColors.Control; } }
        public virtual Color ToolStripContentPanelGradientEnd { get { return SystemColors.Control; } }
        public virtual Color ToolStripPanelGradientBegin { get { return SystemColors.Control; } }
        public virtual Color ToolStripPanelGradientEnd { get { return SystemColors.Control; } }
        public virtual Color OverflowButtonGradientBegin { get { return SystemColors.Control; } }
        public virtual Color OverflowButtonGradientMiddle { get { return SystemColors.Control; } }
        public virtual Color OverflowButtonGradientEnd { get { return SystemColors.Control; } }
        public virtual Color RaftingContainerGradientBegin { get { return SystemColors.Control; } }
        public virtual Color RaftingContainerGradientEnd { get { return SystemColors.Control; } }
    }

    public class ToolStripProfessionalRenderer : ToolStripRenderer
    {
        private readonly ProfessionalColorTable _colors;
        public ToolStripProfessionalRenderer() : this(new ProfessionalColorTable()) { }
        public ToolStripProfessionalRenderer(ProfessionalColorTable professionalColorTable) { _colors = professionalColorTable; }
        public ProfessionalColorTable ColorTable { get { return _colors; } }
        public bool RoundedEdges { get; set; } = true;
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            ToolStrip strip = e.ToolStrip;
            Rectangle r = new Rectangle(Point.Empty, strip.Size);
            Color back;
            if (strip is ToolStripDropDown) back = _colors.ToolStripDropDownBackground;
            else if (strip is MenuStrip) back = strip.BackColorSet ? strip.BackColor : _colors.MenuStripGradientBegin;
            else if (strip is StatusStrip) back = strip.BackColorSet ? strip.BackColor : _colors.StatusStripGradientBegin;
            else if (strip.BackColorSet) back = strip.BackColor;
            else
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(r, _colors.ToolStripGradientBegin, _colors.ToolStripGradientEnd, LinearGradientMode.Vertical))
                    e.Graphics.FillRectangle(brush, r);
                return;
            }
            using (SolidBrush brush = new SolidBrush(back)) e.Graphics.FillRectangle(brush, r);
        }
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            ToolStrip strip = e.ToolStrip;
            if (strip is ToolStripDropDown)
            {
                using (Pen pen = new Pen(_colors.MenuBorder)) e.Graphics.DrawRectangle(pen, 0, 0, strip.Width - 1, strip.Height - 1);
            }
            else if (!(strip is MenuStrip) && !(strip is StatusStrip))
            {
                using (Pen pen = new Pen(_colors.ToolStripBorder)) e.Graphics.DrawLine(pen, 0, strip.Height - 1, strip.Width, strip.Height - 1);
            }
        }
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(_colors.ImageMarginGradientBegin)) e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }
        protected override void OnRenderGrip(ToolStripGripRenderEventArgs e)
        {
            using (SolidBrush dark = new SolidBrush(_colors.GripDark))
            using (SolidBrush light = new SolidBrush(_colors.GripLight))
            {
                int h = e.ToolStrip.Height;
                for (int y = 5; y < h - 5; y += 4)
                {
                    e.Graphics.FillRectangle(light, 4, y + 1, 2, 2);
                    e.Graphics.FillRectangle(dark, 3, y, 2, 2);
                }
            }
        }
        private void FillItem(Graphics g, Rectangle r, Color fill, Color border)
        {
            using (SolidBrush brush = new SolidBrush(fill)) g.FillRectangle(brush, r);
            using (Pen pen = new Pen(border)) g.DrawRectangle(pen, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            Rectangle r = new Rectangle(Point.Empty, item.Bounds.Size);
            bool isChecked = item is ToolStripButton b && b.Checked;
            if (!item.Enabled)
            {
                if (isChecked) FillItem(e.Graphics, r, Color.FromArgb(0xE6, 0xE6, 0xE6), Color.FromArgb(0xC0, 0xC0, 0xC0));
                return;
            }
            if (item.Pressed) FillItem(e.Graphics, r, _colors.ButtonPressedGradientBegin, _colors.ButtonPressedBorder);
            else if (item.Selected && isChecked) FillItem(e.Graphics, r, _colors.ButtonPressedGradientBegin, _colors.ButtonPressedBorder);
            else if (item.Selected) FillItem(e.Graphics, r, _colors.ButtonSelectedGradientBegin, _colors.ButtonSelectedBorder);
            else if (isChecked) FillItem(e.Graphics, r, _colors.ButtonCheckedGradientBegin, _colors.ButtonSelectedBorder);
        }
        protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            Rectangle r = new Rectangle(Point.Empty, item.Bounds.Size);
            if (!item.Enabled) return;
            if (item.Pressed || (item is ToolStripDropDownItem d && d.DropDownIsOpen)) FillItem(e.Graphics, r, _colors.ButtonPressedGradientBegin, _colors.ButtonPressedBorder);
            else if (item.Selected) FillItem(e.Graphics, r, _colors.ButtonSelectedGradientBegin, _colors.ButtonSelectedBorder);
        }
        protected override void OnRenderSplitButtonBackground(ToolStripItemRenderEventArgs e) { OnRenderDropDownButtonBackground(e); }
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            Rectangle r = new Rectangle(Point.Empty, item.Bounds.Size);
            bool open = item is ToolStripDropDownItem d && d.DropDownIsOpen;
            if (item.IsOnDropDown)
            {
                if ((item.Selected || open) && item.Enabled)
                    FillItem(e.Graphics, new Rectangle(r.X + 2, r.Y, r.Width - 3, r.Height), _colors.MenuItemSelected, _colors.MenuItemBorder);
                else if (item.Selected)
                    FillItem(e.Graphics, new Rectangle(r.X + 2, r.Y, r.Width - 3, r.Height), Color.FromArgb(0xE6, 0xE6, 0xE6), Color.FromArgb(0xCC, 0xCC, 0xCC));
            }
            else
            {
                if (open) FillItem(e.Graphics, r, _colors.MenuItemPressedGradientBegin, _colors.MenuItemBorder);
                else if (item.Selected && item.Enabled) FillItem(e.Graphics, r, _colors.MenuItemSelected, _colors.MenuItemBorder);
            }
        }
        protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
        {
            ToolStripItem item = e.Item;
            if (item.BackColorSet)
            {
                using (SolidBrush brush = new SolidBrush(item.BackColor)) e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, item.Bounds.Size));
            }
            if (item is ToolStripStatusLabel label && label.BorderSides != ToolStripStatusLabelBorderSides.None)
            {
                Rectangle r = new Rectangle(Point.Empty, item.Bounds.Size);
                using (Pen pen = new Pen(Color.FromArgb(0xA0, 0xA0, 0xA0)))
                {
                    if ((label.BorderSides & ToolStripStatusLabelBorderSides.Left) != 0) e.Graphics.DrawLine(pen, 0, 0, 0, r.Height - 1);
                    if ((label.BorderSides & ToolStripStatusLabelBorderSides.Top) != 0) e.Graphics.DrawLine(pen, 0, 0, r.Width - 1, 0);
                    if ((label.BorderSides & ToolStripStatusLabelBorderSides.Right) != 0) e.Graphics.DrawLine(pen, r.Width - 1, 0, r.Width - 1, r.Height - 1);
                    if ((label.BorderSides & ToolStripStatusLabelBorderSides.Bottom) != 0) e.Graphics.DrawLine(pen, 0, r.Height - 1, r.Width - 1, r.Height - 1);
                }
            }
        }
        protected override void OnRenderLabelBackground(ToolStripItemRenderEventArgs e) { OnRenderItemBackground(e); }
    }

    public static class ToolStripManager
    {
        private static ToolStripRenderer _renderer;
        public static ToolStripRenderer Renderer
        {
            get { return _renderer ?? (_renderer = new ToolStripProfessionalRenderer()); }
            set { _renderer = value; }
        }
        public static ToolStripManagerRenderMode RenderMode { get; set; } = ToolStripManagerRenderMode.Professional;
        public static bool VisualStylesEnabled { get; set; } = true;
        public static bool IsValidShortcut(Keys shortcut) { return (shortcut & Keys.KeyCode) != Keys.None; }
        public static bool Merge(ToolStrip sourceToolStrip, ToolStrip targetToolStrip) { return false; }
        public static bool RevertMerge(ToolStrip targetToolStrip) { return false; }
    }

    public enum ToolStripManagerRenderMode { Custom = 0, System = 1, Professional = 2 }

    // Tool strip container
    public class ToolStripContentPanel : Panel
    {
        public ToolStripRenderer Renderer { get; set; }
        public ToolStripRenderMode RenderMode { get; set; }
        public event EventHandler Load;
    }

    public class ToolStripContentPanelRenderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ToolStripContentPanel ToolStripContentPanel { get; }
        public bool Handled { get; set; }
        public ToolStripContentPanelRenderEventArgs(Graphics g, ToolStripContentPanel contentPanel) { Graphics = g; ToolStripContentPanel = contentPanel; }
    }

    public class ToolStripPanelRenderEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public ToolStripPanel ToolStripPanel { get; }
        public bool Handled { get; set; }
        public ToolStripPanelRenderEventArgs(Graphics g, ToolStripPanel toolStripPanel) { Graphics = g; ToolStripPanel = toolStripPanel; }
    }

    // A panel of tool strips: the strips keep their designer location; rows are as high as their strips
    public class ToolStripPanel : ContainerControl
    {
        public ToolStripPanel()
        {
            SetStyle(ControlStyles.Selectable, false);
        }
        public Orientation Orientation { get; set; }
        public Padding RowMargin { get; set; } = new Padding(3, 0, 0, 0);
        public ToolStripRenderer Renderer { get; set; }
        public ToolStripRenderMode RenderMode { get; set; }
        public bool Locked { get; set; }
        public override bool AutoSize { get { return true; } set { } }
        public void Join(ToolStrip toolStripToDrag) { Controls.Add(toolStripToDrag); }
        public void Join(ToolStrip toolStripToDrag, int row) { Controls.Add(toolStripToDrag); }
        public void Join(ToolStrip toolStripToDrag, int x, int y) { Controls.Add(toolStripToDrag); toolStripToDrag.Location = new Point(x, y); }
        public void Join(ToolStrip toolStripToDrag, Point location) { Join(toolStripToDrag, location.X, location.Y); }
        private bool _arranging;
        internal override void LayoutChildren()
        {
            if (!HasChildren || _arranging) return;
            _arranging = true;
            try { ArrangeRows(); }
            finally { _arranging = false; }
        }
        private void ArrangeRows()
        {
            bool horizontal = Orientation == Orientation.Horizontal;
            // Rows by the y location of the strips; in a row the strips keep their x location without overlapping
            List<Control> strips = Controls.Items.Where(c => c.VisibleSelf).OrderBy(c => horizontal ? c.Top : c.Left).ThenBy(c => horizontal ? c.Left : c.Top).ToList();
            int rowStart = 0;
            int rowPosition = 0;
            int rowSize = 0;
            int lastKey = int.MinValue;
            int next = 0;
            foreach (Control c in strips)
            {
                Size preferred = c is ToolStrip ts ? ts.GetPreferredSize(Size.Empty) : c.Size;
                int key = horizontal ? c.Top : c.Left;
                if (lastKey != int.MinValue && key >= rowPosition + rowSize)
                {
                    rowStart = rowPosition + rowSize;
                    rowSize = 0;
                    next = 0;
                }
                rowPosition = rowStart;
                lastKey = key;
                if (horizontal)
                {
                    int x = Math.Max(c.Left, next);
                    int w = c is ToolStrip ? preferred.Width : c.Width;
                    c.SetBoundsCoreFromLayout(x, rowPosition, w, preferred.Height);
                    next = x + w;
                    rowSize = Math.Max(rowSize, preferred.Height);
                }
                else
                {
                    int y = Math.Max(c.Top, next);
                    int h = c is ToolStrip ? preferred.Height : c.Height;
                    c.SetBoundsCoreFromLayout(rowPosition, y, preferred.Width, h);
                    next = y + h;
                    rowSize = Math.Max(rowSize, preferred.Width);
                }
            }
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            if (!HasChildren) return Orientation == Orientation.Horizontal ? new Size(Width, 0) : new Size(0, Height);
            // The rows are packed before the size is taken
            LayoutChildren();
            int max = 0;
            foreach (Control c in Controls.Items)
            {
                if (!c.VisibleSelf) continue;
                max = Math.Max(max, Orientation == Orientation.Horizontal ? c.Bottom : c.Right);
            }
            return Orientation == Orientation.Horizontal ? new Size(Width, max) : new Size(max, Height);
        }
        internal override Size GetDockedSize(Size proposed)
        {
            Size preferred = GetPreferredSize(Size.Empty);
            return Orientation == Orientation.Horizontal ? new Size(proposed.Width, preferred.Height) : new Size(preferred.Width, proposed.Height);
        }
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control.SizeChanged += (s, a) => Parent?.PerformLayout();
            e.Control.LocationChanged += (s, a) => { if (!_arranging) Parent?.PerformLayout(); };
            e.Control.VisibleChanged += (s, a) => Parent?.PerformLayout();
            Parent?.PerformLayout();
        }
    }

    public class ToolStripContainer : ContainerControl
    {
        private readonly ToolStripPanel _top, _bottom, _left, _right;
        private readonly ToolStripContentPanel _content;
        public ToolStripContainer()
        {
            _content = new ToolStripContentPanel { Dock = DockStyle.Fill };
            _top = new ToolStripPanel { Dock = DockStyle.Top, Orientation = Orientation.Horizontal };
            _bottom = new ToolStripPanel { Dock = DockStyle.Bottom, Orientation = Orientation.Horizontal };
            _left = new ToolStripPanel { Dock = DockStyle.Left, Orientation = Orientation.Vertical };
            _right = new ToolStripPanel { Dock = DockStyle.Right, Orientation = Orientation.Vertical };
            Controls.Add(_content);
            Controls.Add(_left);
            Controls.Add(_right);
            Controls.Add(_top);
            Controls.Add(_bottom);
            SetStyle(ControlStyles.Selectable, false);
        }
        protected override Size DefaultSize { get { return new Size(150, 175); } }
        public ToolStripContentPanel ContentPanel { get { return _content; } }
        public ToolStripPanel TopToolStripPanel { get { return _top; } }
        public ToolStripPanel BottomToolStripPanel { get { return _bottom; } }
        public ToolStripPanel LeftToolStripPanel { get { return _left; } }
        public ToolStripPanel RightToolStripPanel { get { return _right; } }
        public bool TopToolStripPanelVisible { get { return _top.Visible; } set { _top.Visible = value; } }
        public bool BottomToolStripPanelVisible { get { return _bottom.Visible; } set { _bottom.Visible = value; } }
        public bool LeftToolStripPanelVisible { get { return _left.Visible; } set { _left.Visible = value; } }
        public bool RightToolStripPanelVisible { get { return _right.Visible; } set { _right.Visible = value; } }
        public ToolStripRenderer Renderer { get; set; }
        public ToolStripRenderMode RenderMode { get; set; }
    }
}
