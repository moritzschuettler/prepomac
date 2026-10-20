// PrePoMax for macOS - System.Windows.Forms.Control on Avalonia.
//
// Every control owns an Avalonia host panel (ControlHost). The host draws the background and the OnPaint output of the
// control, shows the Avalonia visual of the control (button, text box, ...) and places the hosts of the child controls at
// their WinForms bounds. The WinForms layout (Dock, Anchor, AutoSize) is computed here, like DefaultLayout of WinForms.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Avalonia.Threading;
using AC = Avalonia.Controls;
using AI = Avalonia.Input;

namespace System.Windows.Forms
{
    public interface IWin32Window
    {
        IntPtr Handle { get; }
    }

    public interface IContainerControl
    {
        Control ActiveControl { get; set; }
        bool ActivateControl(Control active);
    }

    public interface IButtonControl
    {
        DialogResult DialogResult { get; set; }
        void NotifyDefault(bool value);
        void PerformClick();
    }

    [DesignerCategory("Code")]
    public partial class Control : Component, IWin32Window, ISynchronizeInvoke
    {
        // Handles
        private static long _nextHandle = 0x1000;
        private static readonly Dictionary<IntPtr, WeakReference<Control>> _handles = new Dictionary<IntPtr, WeakReference<Control>>();
        private readonly IntPtr _handle;

        // State
        private ControlHost _host;
        private Control _parent;
        private ControlCollection _controls;
        private Rectangle _bounds;
        private Size _clientSizeOffset;     // Size - ClientSize
        private DockStyle _dock;
        private AnchorStyles _anchor = AnchorStyles.Top | AnchorStyles.Left;
        private Padding _padding;
        private Padding _margin;
        private bool _visible = true;
        private bool _enabled = true;
        private string _text = string.Empty;
        private string _name = string.Empty;
        private Font _font;
        private Color _foreColor = Color.Empty;
        private Color _backColor = Color.Empty;
        private Image _backgroundImage;
        private ImageLayout _backgroundImageLayout = ImageLayout.Tile;
        private Cursor _cursor;
        private ContextMenuStrip _contextMenuStrip;
        private int _tabIndex = -1;
        private bool _tabStop = true;
        private bool _autoSize;
        private Size _minimumSize;
        private Size _maximumSize;
        private int _layoutSuspendCount;
        private bool _layoutPending;
        private bool _inLayout;
        private bool _created;
        private bool _disposed;
        private bool _disposing;
        private ControlStyles _styles = ControlStyles.UserPaint | ControlStyles.StandardClick | ControlStyles.Selectable |
                                        ControlStyles.StandardDoubleClick | ControlStyles.AllPaintingInWmPaint | ControlStyles.UseTextForAccessibility;
        private bool _allowDrop;
        private bool _causesValidation = true;
        private RightToLeft _rightToLeft = RightToLeft.Inherit;
        private ImeMode _imeMode = ImeMode.Inherit;
        private bool _useWaitCursor;
        private Region _region;
        // Anchor layout: distances to the edges of the display rectangle of the parent
        internal int AnchorLeft, AnchorTop, AnchorRight, AnchorBottom;
        internal bool AnchorInfoValid;
        // Mouse
        private bool _mouseDownInside;
        private MouseButtons _mouseDownButton;
        private bool _mouseInside;

        // Properties
        public IntPtr Handle { get { return _handle; } }
        public bool IsHandleCreated { get { return !_disposed; } }
        public bool Created { get { return _created; } }
        public bool IsDisposed { get { return _disposed; } }
        public bool Disposing { get { return _disposing; } }
        public bool RecreatingHandle { get { return false; } }
        public bool InvokeRequired { get { return !Dispatcher.UIThread.CheckAccess(); } }
        // The Avalonia element of this control
        public AC.Control NativeControl { get { return Host; } }
        internal ControlHost Host
        {
            get
            {
                if (_host == null)
                {
                    _host = new ControlHost(this);
                    AC.Control content = CreateContent();
                    if (content != null) _host.SetContent(content);
                    ApplyNativeProperties();
                }
                return _host;
            }
        }
        // A native visual shown over the client area under the child controls; the mouse goes to the control
        public void SetNativeVisual(AC.Control visual) { Host.SetNativeVisual(visual); }
        public AC.Control GetNativeVisual() { return Host.NativeVisual; }
        // The Avalonia visual of the control itself (a button, a text box, ...) shown over the client area
        protected internal AC.Control Content { get { return Host.Content; } }
        public string Name { get { return _name; } set { _name = value ?? string.Empty; } }
        public object Tag { get; set; }
        public virtual string Text
        {
            get { return _text; }
            set
            {
                value = value ?? string.Empty;
                if (_text == value) return;
                if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) { string text = value; Toolkit.OnUiThread(() => Text = text); return; }
                _text = value;
                OnTextChanged(EventArgs.Empty);
            }
        }
        public Control Parent
        {
            get { return _parent; }
            set
            {
                if (_parent == value) return;
                if (_parent != null) _parent.Controls.Remove(this);
                if (value != null) value.Controls.Add(this);
            }
        }
        public ControlCollection Controls { get { return _controls ?? (_controls = CreateControlsInstance()); } }
        public bool HasChildren { get { return _controls != null && _controls.Count > 0; } }
        protected virtual ControlCollection CreateControlsInstance() { return new ControlCollection(this); }

        // Bounds
        public Rectangle Bounds
        {
            get { return _bounds; }
            set { SetBounds(value.X, value.Y, value.Width, value.Height, BoundsSpecified.All); }
        }
        public Point Location
        {
            get { return _bounds.Location; }
            set { SetBounds(value.X, value.Y, _bounds.Width, _bounds.Height, BoundsSpecified.Location); }
        }
        public Size Size
        {
            get { return _bounds.Size; }
            set { SetBounds(_bounds.X, _bounds.Y, value.Width, value.Height, BoundsSpecified.Size); }
        }
        public int Left { get { return _bounds.X; } set { SetBounds(value, _bounds.Y, _bounds.Width, _bounds.Height, BoundsSpecified.X); } }
        public int Top { get { return _bounds.Y; } set { SetBounds(_bounds.X, value, _bounds.Width, _bounds.Height, BoundsSpecified.Y); } }
        public int Width { get { return _bounds.Width; } set { SetBounds(_bounds.X, _bounds.Y, value, _bounds.Height, BoundsSpecified.Width); } }
        public int Height { get { return _bounds.Height; } set { SetBounds(_bounds.X, _bounds.Y, _bounds.Width, value, BoundsSpecified.Height); } }
        public int Right { get { return _bounds.Right; } }
        public int Bottom { get { return _bounds.Bottom; } }
        public Size ClientSize
        {
            get { return new Size(Math.Max(0, _bounds.Width - _clientSizeOffset.Width), Math.Max(0, _bounds.Height - _clientSizeOffset.Height)); }
            set { SetClientSizeCore(value.Width, value.Height); }
        }
        public Rectangle ClientRectangle { get { return new Rectangle(Point.Empty, ClientSize); } }
        public virtual Rectangle DisplayRectangle { get { return ClientRectangle; } }
        // Offset of the client area in the bounds (borders)
        protected internal virtual Padding NonClientInsets { get { return Padding.Empty; } }
        public Size PreferredSize { get { return GetPreferredSize(Size.Empty); } }
        public virtual Size MinimumSize
        {
            get { return _minimumSize; }
            set
            {
                _minimumSize = value;
                if (_bounds.Width < value.Width || _bounds.Height < value.Height)
                    Size = new Size(Math.Max(_bounds.Width, value.Width), Math.Max(_bounds.Height, value.Height));
            }
        }
        public virtual Size MaximumSize { get { return _maximumSize; } set { _maximumSize = value; } }
        protected virtual Size DefaultSize { get { return Size.Empty; } }
        protected virtual Size DefaultMinimumSize { get { return Size.Empty; } }
        protected virtual Size DefaultMaximumSize { get { return Size.Empty; } }
        protected virtual Padding DefaultPadding { get { return Padding.Empty; } }
        protected virtual Padding DefaultMargin { get { return new Padding(3); } }
        public virtual DockStyle Dock
        {
            get { return _dock; }
            set
            {
                if (_dock == value) return;
                _dock = value;
                _parent?.PerformLayout(this, "Dock");
                OnDockChanged(EventArgs.Empty);
            }
        }
        public virtual AnchorStyles Anchor
        {
            get { return _anchor; }
            set
            {
                if (_anchor == value) return;
                _anchor = value;
                if (_dock != DockStyle.None) _dock = DockStyle.None;
                UpdateAnchorInfo();
            }
        }
        public Padding Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value) return;
                _padding = value;
                OnPaddingChanged(EventArgs.Empty);
                PerformLayout(this, "Padding");
            }
        }
        public Padding Margin
        {
            get { return _margin; }
            set
            {
                if (_margin == value) return;
                _margin = value;
                OnMarginChanged(EventArgs.Empty);
            }
        }
        public virtual bool AutoSize
        {
            get { return _autoSize; }
            set
            {
                if (_autoSize == value) return;
                _autoSize = value;
                if (value) AdjustAutoSize();
                OnAutoSizeChanged(EventArgs.Empty);
            }
        }
        public virtual Point AutoScrollOffset { get; set; }

        // Appearance
        public virtual Font Font
        {
            get { return _font ?? _parent?.Font ?? DefaultFont; }
            set
            {
                if (Equals(_font, value)) return;
                _font = value;
                OnFontChanged(EventArgs.Empty);
            }
        }
        internal bool FontSet { get { return _font != null; } }
        protected int FontHeight { get { return Font.Height; } set { } }
        public virtual Color ForeColor
        {
            get { return !_foreColor.IsEmpty ? _foreColor : _parent?.ForeColor ?? DefaultForeColor; }
            set
            {
                if (_foreColor == value) return;
                _foreColor = value;
                OnForeColorChanged(EventArgs.Empty);
            }
        }
        public virtual Color BackColor
        {
            get
            {
                if (!_backColor.IsEmpty) return _backColor;
                if (_parent != null && _parent.BackColor != Color.Transparent) return _parent.BackColor;
                return DefaultBackColor;
            }
            set
            {
                if (_backColor == value) return;
                _backColor = value;
                OnBackColorChanged(EventArgs.Empty);
            }
        }
        internal bool BackColorSet { get { return !_backColor.IsEmpty; } }
        internal bool ForeColorSet { get { return !_foreColor.IsEmpty; } }
        public virtual Image BackgroundImage
        {
            get { return _backgroundImage; }
            set { _backgroundImage = value; Invalidate(); OnBackgroundImageChanged(EventArgs.Empty); }
        }
        public virtual ImageLayout BackgroundImageLayout
        {
            get { return _backgroundImageLayout; }
            set { _backgroundImageLayout = value; Invalidate(); }
        }
        public static Font DefaultFont { get { return SystemFonts.DefaultFont; } }
        public static Color DefaultForeColor { get { return SystemColors.ControlText; } }
        public static Color DefaultBackColor { get { return SystemColors.Control; } }
        public virtual Cursor Cursor
        {
            get { return _cursor ?? _parent?.Cursor ?? Cursors.Default; }
            set
            {
                _cursor = value;
                if (_host != null) _host.Cursor = value?.Native;
                OnCursorChanged(EventArgs.Empty);
            }
        }
        public bool UseWaitCursor
        {
            get { return _useWaitCursor; }
            set
            {
                _useWaitCursor = value;
                if (_host != null) _host.Cursor = value ? Cursors.WaitCursor.Native : _cursor?.Native;
                if (_controls != null) foreach (Control c in _controls) c.UseWaitCursor = value;
            }
        }
        public virtual RightToLeft RightToLeft { get { return _rightToLeft == RightToLeft.Inherit ? RightToLeft.No : _rightToLeft; } set { _rightToLeft = value; } }
        public ImeMode ImeMode { get { return _imeMode; } set { _imeMode = value; } }
        public Region Region { get { return _region; } set { _region = value; } }
        public virtual ContextMenuStrip ContextMenuStrip
        {
            get { return _contextMenuStrip; }
            set
            {
                _contextMenuStrip = value;
                OnContextMenuStripChanged(EventArgs.Empty);
            }
        }
        public string AccessibleName { get; set; }
        public string AccessibleDescription { get; set; }
        public string AccessibleDefaultActionDescription { get; set; }
        public AccessibleRole AccessibleRole { get; set; }
        public string CompanyName { get { return "PrePoMax"; } }
        public string ProductName { get { return Application.ProductName; } }
        public string ProductVersion { get { return Application.ProductVersion; } }
        public ControlBindingsCollection DataBindings { get; } = new ControlBindingsCollection();
        public BindingContext BindingContext { get; set; } = new BindingContext();
        public bool IsMirrored { get { return false; } }
        public LayoutEngine LayoutEngine { get { return LayoutEngine.Default; } }
        public virtual ISite Site_ { get { return Site; } }

        // State
        public bool Visible
        {
            get { return _visible && (_parent == null ? IsTopLevelShown : _parent.Visible); }
            set
            {
                if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) SetVisibleCore(value);
                else Toolkit.OnUiThread(() => SetVisibleCore(value));
            }
        }
        // The value of Visible set on this control
        internal bool VisibleSelf { get { return _visible; } }
        internal virtual bool IsTopLevelShown { get { return _visible; } }
        public bool Enabled
        {
            get { return _enabled && (_parent == null || _parent.Enabled); }
            set
            {
                if (_enabled == value) return;
                if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) { bool enabled = value; Toolkit.OnUiThread(() => Enabled = enabled); return; }
                bool old = Enabled;
                _enabled = value;
                if (_host != null) _host.IsEnabled = value;
                if (old != Enabled) OnEnabledChanged(EventArgs.Empty);
            }
        }
        public int TabIndex
        {
            get { return _tabIndex < 0 ? 0 : _tabIndex; }
            set
            {
                if (_tabIndex == value) return;
                _tabIndex = value;
                OnTabIndexChanged(EventArgs.Empty);
            }
        }
        public bool TabStop
        {
            get { return _tabStop; }
            set
            {
                if (_tabStop == value) return;
                _tabStop = value;
                OnTabStopChanged(EventArgs.Empty);
            }
        }
        public bool CausesValidation { get { return _causesValidation; } set { _causesValidation = value; } }
        public virtual bool AllowDrop
        {
            get { return _allowDrop; }
            set
            {
                _allowDrop = value;
                if (_host != null) AI.DragDrop.SetAllowDrop(_host, value);
            }
        }
        public virtual bool Focused { get { return _host != null && (_host.IsFocused || (Content != null && Content.IsFocused)); } }
        public bool ContainsFocus { get { return _host != null && _host.IsKeyboardFocusWithin; } }
        public bool CanFocus { get { return Visible && Enabled; } }
        public bool CanSelect { get { return GetStyle(ControlStyles.Selectable) && Visible && Enabled; } }
        public bool Capture
        {
            get { return ControlHost.CapturedControl == this; }
            set
            {
                if (value) ControlHost.CapturedControl = this;
                else if (ControlHost.CapturedControl == this) ControlHost.CapturedControl = null;
            }
        }
        protected bool DoubleBuffered { get { return GetStyle(ControlStyles.OptimizedDoubleBuffer); } set { SetStyle(ControlStyles.OptimizedDoubleBuffer, value); } }
        protected virtual bool ResizeRedraw { get { return GetStyle(ControlStyles.ResizeRedraw); } set { SetStyle(ControlStyles.ResizeRedraw, value); } }
        protected virtual bool ShowFocusCues { get { return true; } }
        protected virtual bool ShowKeyboardCues { get { return false; } }
        protected virtual bool CanRaiseEvents { get { return true; } }
        protected virtual CreateParams CreateParams { get { return new CreateParams { ClassName = GetType().Name, Caption = Text }; } }
        protected virtual Cursor DefaultCursor { get { return Cursors.Default; } }
        protected virtual ImeMode DefaultImeMode { get { return ImeMode.Inherit; } }
        protected internal virtual bool ScaleChildren { get { return true; } }
        public Control TopLevelControl
        {
            get
            {
                Control c = this;
                while (c._parent != null) c = c._parent;
                return c;
            }
        }
        public static Keys ModifierKeys { get { return Input.ModifierKeys; } }
        public static MouseButtons MouseButtons { get { return Input.MouseButtons; } }
        public static Point MousePosition { get { return Input.MousePosition; } }
        public static bool CheckForIllegalCrossThreadCalls { get; set; }
        public WindowTarget WindowTarget { get; set; }

        // Events
        public event EventHandler AutoSizeChanged;
        public event EventHandler BackColorChanged;
        public event EventHandler BackgroundImageChanged;
        public event EventHandler BackgroundImageLayoutChanged;
        public event EventHandler BindingContextChanged;
        public event EventHandler CausesValidationChanged;
        public event EventHandler Click;
        public event EventHandler ClientSizeChanged;
        public event EventHandler ContextMenuStripChanged;
        public event ControlEventHandler ControlAdded;
        public event ControlEventHandler ControlRemoved;
        public event EventHandler CursorChanged;
        public event EventHandler DockChanged;
        public event EventHandler DoubleClick;
        public event DragEventHandler DragDrop;
        public event DragEventHandler DragEnter;
        public event EventHandler DragLeave;
        public event DragEventHandler DragOver;
        public event EventHandler EnabledChanged;
        public event EventHandler Enter;
        public event EventHandler FontChanged;
        public event EventHandler ForeColorChanged;
        public event GiveFeedbackEventHandler GiveFeedback;
        public event EventHandler GotFocus;
        public event EventHandler HandleCreated;
        public event EventHandler HandleDestroyed;
        public event HelpEventHandler HelpRequested;
        public event InvalidateEventHandler Invalidated;
        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;
        public event LayoutEventHandler Layout;
        public event EventHandler Leave;
        public event EventHandler LocationChanged;
        public event EventHandler LostFocus;
        public event EventHandler MarginChanged;
        public event EventHandler MouseCaptureChanged;
        public event MouseEventHandler MouseClick;
        public event MouseEventHandler MouseDoubleClick;
        public event MouseEventHandler MouseDown;
        public event EventHandler MouseEnter;
        public event EventHandler MouseHover;
        public event EventHandler MouseLeave;
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseUp;
        public event MouseEventHandler MouseWheel;
        public event EventHandler Move;
        public event EventHandler PaddingChanged;
        public event PaintEventHandler Paint;
        public event EventHandler ParentChanged;
        public event PreviewKeyDownEventHandler PreviewKeyDown;
        public event QueryContinueDragEventHandler QueryContinueDrag;
        public event EventHandler RegionChanged;
        public event EventHandler Resize;
        public event EventHandler RightToLeftChanged;
        public event EventHandler SizeChanged;
        public event EventHandler StyleChanged;
        public event EventHandler SystemColorsChanged;
        public event EventHandler TabIndexChanged;
        public event EventHandler TabStopChanged;
        public event EventHandler TextChanged;
        public event EventHandler Validated;
        public event CancelEventHandler Validating;
        public event EventHandler VisibleChanged;
        public event UICuesEventHandler ChangeUICues;
        public event EventHandler ImeModeChanged;

        internal bool HasPaintHandlers { get { return Paint != null; } }


        // Constructors
        public Control()
        {
            Toolkit.EnsureInitialized();
            _handle = new IntPtr(System.Threading.Interlocked.Increment(ref _nextHandle));
            lock (_handles) _handles[_handle] = new WeakReference<Control>(this);
            _padding = DefaultPadding;
            _margin = DefaultMargin;
            _minimumSize = DefaultMinimumSize;
            _maximumSize = DefaultMaximumSize;
            Size size = DefaultSize;
            _bounds = new Rectangle(0, 0, size.Width, size.Height);
        }
        public Control(string text) : this() { Text = text; }
        public Control(Control parent, string text) : this() { Text = text; Parent = parent; }
        public Control(string text, int left, int top, int width, int height) : this()
        {
            Text = text;
            Bounds = new Rectangle(left, top, width, height);
        }
        public Control(Control parent, string text, int left, int top, int width, int height)
            : this(text, left, top, width, height) { Parent = parent; }


        // Native element
        // Creates the Avalonia visual of the control; null for controls that only paint and host children
        protected internal virtual AC.Control CreateContent() { return null; }
        // Applies the WinForms properties to the Avalonia elements after they are created
        private void ApplyNativeProperties()
        {
            _host.IsVisible = _visible;
            _host.IsEnabled = _enabled;
            if (_cursor != null) _host.Cursor = _cursor.Native;
            if (_allowDrop) AI.DragDrop.SetAllowDrop(_host, true);
            UpdateNativeFont();
            UpdateNativeColors();
            OnContentCreated();
        }
        // Called after the content is created so that derived controls apply their state to it
        protected internal virtual void OnContentCreated() { }
        internal void UpdateNativeFont()
        {
            if (_host == null) return;
            Font font = Font;
            Toolkit.ApplyFont(_host, font);
        }
        internal void UpdateNativeColors()
        {
            if (_host == null) return;
            _host.UpdateBackground();
            Toolkit.ApplyForeground(_host, ForeColor);
        }
        public static Control FromHandle(IntPtr handle)
        {
            lock (_handles)
            {
                if (_handles.TryGetValue(handle, out var reference) && reference.TryGetTarget(out Control control)) return control;
            }
            return null;
        }
        public static Control FromChildHandle(IntPtr handle) { return FromHandle(handle); }
        public void CreateControl()
        {
            if (_created) return;
            _created = true;
            _ = Host;
            // As in WinForms the handle exists before OnCreateControl raises Load
            OnHandleCreated(EventArgs.Empty);
            OnCreateControl();
            if (_controls != null) foreach (Control child in _controls.ToArray()) if (child._visible) child.CreateControl();
        }
        protected virtual void OnCreateControl() { }
        protected virtual void CreateHandle() { CreateControl(); }
        protected virtual void DestroyHandle() { }
        protected void RecreateHandle() { }
        protected void UpdateStyles() { }
        protected void UpdateBounds() { }
        protected void UpdateBounds(int x, int y, int width, int height) { SetBounds(x, y, width, height); }
        protected virtual void WndProc(ref Message m) { DefWndProc(ref m); }
        protected virtual void DefWndProc(ref Message m) { _defWndProcReached = true; }
        private bool _defWndProcReached;
        // Passes a message through WndProc: true if it reached the default processing (it was not swallowed)
        internal bool FilterMessage(int msg, IntPtr wParam, IntPtr lParam)
        {
            bool reached = _defWndProcReached;
            _defWndProcReached = false;
            try
            {
                Message m = Message.Create(_handle, msg, wParam, lParam);
                WndProc(ref m);
                return _defWndProcReached;
            }
            finally
            {
                _defWndProcReached = reached;
            }
        }
        // WM_MOUSEMOVE, WM_LBUTTONDOWN, ... with the position in lParam
        internal static int MouseMessage(MouseButtons button, int kind)
        {
            int base_ = button == MouseButtons.Right ? 0x204 : button == MouseButtons.Middle ? 0x207 : 0x201;
            return base_ + kind;
        }
        internal static IntPtr PointParam(int x, int y) { return new IntPtr(((y & 0xFFFF) << 16) | (x & 0xFFFF)); }
        // Controls that filter the mouse in WndProc get the messages before the events
        internal virtual bool FiltersMouseMessages { get { return Toolkit.OverridesWndProc(GetType()); } }
        // Messages that are sent to the control (SendMessage of user32); see Win32Messages
        internal IntPtr SendMessageInternal(int msg, IntPtr wParam, IntPtr lParam)
        {
            Message m = Message.Create(_handle, msg, wParam, lParam);
            if (!Win32Messages.Handle(this, ref m)) WndProc(ref m);
            return m.Result;
        }
        protected virtual void OnNotifyMessage(Message m) { }
        protected virtual bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (_contextMenuStrip != null && _contextMenuStrip.ProcessShortcut(keyData)) return true;
            return _parent?.ProcessCmdKey(ref msg, keyData) ?? false;
        }
        internal bool ProcessCmdKeyInternal(Keys keyData)
        {
            Message msg = Message.Create(_handle, 0x0100, new IntPtr((int)(keyData & Keys.KeyCode)), IntPtr.Zero);
            return ProcessCmdKey(ref msg, keyData);
        }
        protected virtual bool ProcessDialogKey(Keys keyData) { return _parent?.ProcessDialogKey(keyData) ?? false; }
        internal bool ProcessDialogKeyInternal(Keys keyData) { return ProcessDialogKey(keyData); }
        protected virtual bool ProcessDialogChar(char charCode) { return _parent?.ProcessDialogChar(charCode) ?? false; }
        protected virtual bool ProcessKeyPreview(ref Message m) { return _parent?.ProcessKeyPreview(ref m) ?? false; }
        protected virtual bool ProcessKeyMessage(ref Message m) { return false; }
        protected virtual bool ProcessKeyEventArgs(ref Message m) { return false; }
        protected virtual bool ProcessMnemonic(char charCode) { return false; }
        public virtual bool PreProcessMessage(ref Message msg) { return false; }
        protected virtual bool IsInputKey(Keys keyData) { return false; }
        internal bool IsInputKeyInternal(Keys keyData) { return IsInputKey(keyData); }
        protected virtual bool IsInputChar(char charCode) { return true; }
        public static bool IsMnemonic(char charCode, string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            int index = text.IndexOf('&');
            return index >= 0 && index + 1 < text.Length && char.ToUpperInvariant(text[index + 1]) == char.ToUpperInvariant(charCode);
        }

        // Styles
        protected bool GetStyle(ControlStyles flag) { return (_styles & flag) == flag; }
        protected void SetStyle(ControlStyles flag, bool value)
        {
            if (value) _styles |= flag;
            else _styles &= ~flag;
        }
        // True if OnPaint is overridden or Paint handlers exist: the host then paints the control
        internal bool UsesCustomPaint
        {
            get
            {
                if (Paint != null) return true;
                return Toolkit.OverridesPaint(GetType());
            }
        }


        // Bounds and layout
        public void SetBounds(int x, int y, int width, int height) { SetBounds(x, y, width, height, BoundsSpecified.All); }
        public void SetBounds(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if ((specified & BoundsSpecified.X) == 0) x = _bounds.X;
            if ((specified & BoundsSpecified.Y) == 0) y = _bounds.Y;
            if ((specified & BoundsSpecified.Width) == 0) width = _bounds.Width;
            if ((specified & BoundsSpecified.Height) == 0) height = _bounds.Height;
            if (_bounds.X != x || _bounds.Y != y || _bounds.Width != width || _bounds.Height != height)
                SetBoundsCore(x, y, width, height, specified);
            else if (_parent != null && !_parent._inLayout) UpdateAnchorInfo();
        }
        protected virtual void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Toolkit.OnUiThread(() => SetBoundsCore(x, y, width, height, specified));
                return;
            }
            if (_minimumSize.Width > 0) width = Math.Max(width, _minimumSize.Width);
            if (_minimumSize.Height > 0) height = Math.Max(height, _minimumSize.Height);
            if (_maximumSize.Width > 0) width = Math.Min(width, _maximumSize.Width);
            if (_maximumSize.Height > 0) height = Math.Min(height, _maximumSize.Height);
            width = Math.Max(0, width);
            height = Math.Max(0, height);
            UpdateBounds(x, y, width, height, width - (_bounds.Width - _clientSizeOffset.Width), height - (_bounds.Height - _clientSizeOffset.Height));
        }
        protected virtual void SetClientSizeCore(int x, int y)
        {
            Size = new Size(x + _clientSizeOffset.Width, y + _clientSizeOffset.Height);
        }
        protected virtual Size SizeFromClientSize(Size clientSize) { return clientSize + _clientSizeOffset; }
        internal void SetClientSizeOffset(Size offset)
        {
            _clientSizeOffset = offset;
        }
        private void UpdateBounds(int x, int y, int width, int height, int clientWidth, int clientHeight)
        {
            Rectangle old = _bounds;
            _bounds = new Rectangle(x, y, width, height);
            bool moved = old.X != x || old.Y != y;
            bool resized = old.Width != width || old.Height != height;
            if (_parent != null)
            {
                if (!_parent._inLayout) UpdateAnchorInfo();
                _parent.Host.InvalidateArrange();
            }
            OnBoundsUpdated(old);
            if (moved)
            {
                OnLocationChanged(EventArgs.Empty);
            }
            if (resized)
            {
                if (_host != null)
                {
                    _host.InvalidateMeasure();
                    _host.InvalidateArrange();
                    if (UsesCustomPaint) _host.InvalidateVisual();
                }
                OnSizeChanged(EventArgs.Empty);
                OnClientSizeChanged(EventArgs.Empty);
                PerformLayout(this, "Bounds");
                if (_parent != null && (_dock != DockStyle.None || _parent.AutoSize)) _parent.PerformLayout(this, "Bounds");
            }
        }
        // Called by top level windows when the bounds change
        internal virtual void OnBoundsUpdated(Rectangle oldBounds) { }
        internal void SetBoundsFromNative(Rectangle bounds)
        {
            Rectangle old = _bounds;
            if (old == bounds) return;
            UpdateBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0, 0);
        }
        internal void UpdateAnchorInfo()
        {
            if (_parent == null) return;
            Rectangle display = _parent.DisplayRectangle;
            AnchorLeft = _bounds.Left - display.Left;
            AnchorTop = _bounds.Top - display.Top;
            AnchorRight = display.Right - _bounds.Right;
            AnchorBottom = display.Bottom - _bounds.Bottom;
            AnchorInfoValid = true;
        }
        public void SuspendLayout() { _layoutSuspendCount++; }
        public void ResumeLayout() { ResumeLayout(true); }
        public void ResumeLayout(bool performLayout)
        {
            if (_layoutSuspendCount > 0) _layoutSuspendCount--;
            if (_layoutSuspendCount != 0) return;
            if (performLayout) PerformLayout();
            else
            {
                // As LayoutEngine.InitLayout of WinForms: the anchors of the children are taken from where they are now,
                // relative to the size the control has now (designer code sets the client size after the children)
                if (_controls != null) foreach (Control c in _controls.ToArray()) if (c._dock == DockStyle.None) c.UpdateAnchorInfo();
                _layoutPending = false;
            }
        }
        internal bool IsLayoutSuspended { get { return _layoutSuspendCount > 0; } }
        public void PerformLayout() { PerformLayout(null, null); }
        public void PerformLayout(Control affectedControl, string affectedProperty)
        {
            if (_disposed) return;
            if (_layoutSuspendCount > 0)
            {
                _layoutPending = true;
                return;
            }
            if (_inLayout) return;
            _layoutPending = false;
            _inLayout = true;
            try
            {
                OnLayout(new LayoutEventArgs(affectedControl, affectedProperty));
            }
            finally
            {
                _inLayout = false;
            }
            if (_autoSize && affectedControl != this) AdjustAutoSize();
        }
        protected virtual void OnLayout(LayoutEventArgs levent)
        {
            Layout?.Invoke(this, levent);
            LayoutChildren();
        }
        // The DefaultLayout of WinForms: docked children in reverse z-order, then the anchored ones
        internal virtual void LayoutChildren()
        {
            if (_controls == null || _controls.Count == 0) return;
            Rectangle remaining = DisplayRectangle;
            for (int i = _controls.Count - 1; i >= 0; i--)
            {
                Control c = _controls[i];
                if (!c._visible || c._dock == DockStyle.None) continue;
                switch (c._dock)
                {
                    case DockStyle.Top:
                        {
                            Size s = c.GetDockedSize(new Size(remaining.Width, c.Height));
                            c.SetBoundsCoreFromLayout(remaining.Left, remaining.Top, remaining.Width, s.Height);
                            remaining.Y += c.Height;
                            remaining.Height -= c.Height;
                            break;
                        }
                    case DockStyle.Bottom:
                        {
                            Size s = c.GetDockedSize(new Size(remaining.Width, c.Height));
                            c.SetBoundsCoreFromLayout(remaining.Left, remaining.Bottom - s.Height, remaining.Width, s.Height);
                            remaining.Height -= c.Height;
                            break;
                        }
                    case DockStyle.Left:
                        {
                            Size s = c.GetDockedSize(new Size(c.Width, remaining.Height));
                            c.SetBoundsCoreFromLayout(remaining.Left, remaining.Top, s.Width, remaining.Height);
                            remaining.X += c.Width;
                            remaining.Width -= c.Width;
                            break;
                        }
                    case DockStyle.Right:
                        {
                            Size s = c.GetDockedSize(new Size(c.Width, remaining.Height));
                            c.SetBoundsCoreFromLayout(remaining.Right - s.Width, remaining.Top, s.Width, remaining.Height);
                            remaining.Width -= c.Width;
                            break;
                        }
                    case DockStyle.Fill:
                        c.SetBoundsCoreFromLayout(remaining.Left, remaining.Top, Math.Max(0, remaining.Width), Math.Max(0, remaining.Height));
                        break;
                }
            }
            Rectangle display = DisplayRectangle;
            foreach (Control c in _controls.ToArray())
            {
                if (c._dock != DockStyle.None) continue;
                if (!c.AnchorInfoValid) c.UpdateAnchorInfo();
                AnchorStyles a = c._anchor;
                int x = c._bounds.X, y = c._bounds.Y, w = c._bounds.Width, h = c._bounds.Height;
                bool left = (a & AnchorStyles.Left) != 0, right = (a & AnchorStyles.Right) != 0;
                bool top = (a & AnchorStyles.Top) != 0, bottom = (a & AnchorStyles.Bottom) != 0;
                if (left && right)
                {
                    x = display.Left + c.AnchorLeft;
                    w = display.Right - c.AnchorRight - x;
                }
                else if (right) x = display.Right - c.AnchorRight - w;
                else if (left) x = display.Left + c.AnchorLeft;
                else
                {
                    // Neither left nor right: the center keeps its relative position
                    int total = c.AnchorLeft + w + c.AnchorRight;
                    x = display.Left + (int)Math.Round((display.Width - w) * (total - w == 0 ? 0.5 : (double)c.AnchorLeft / (total - w)));
                }
                if (top && bottom)
                {
                    y = display.Top + c.AnchorTop;
                    h = display.Bottom - c.AnchorBottom - y;
                }
                else if (bottom) y = display.Bottom - c.AnchorBottom - h;
                else if (top) y = display.Top + c.AnchorTop;
                else
                {
                    int total = c.AnchorTop + h + c.AnchorBottom;
                    y = display.Top + (int)Math.Round((display.Height - h) * (total - h == 0 ? 0.5 : (double)c.AnchorTop / (total - h)));
                }
                if (x != c._bounds.X || y != c._bounds.Y || w != c._bounds.Width || h != c._bounds.Height)
                    c.SetBoundsCoreFromLayout(x, y, Math.Max(0, w), Math.Max(0, h));
            }
        }
        // Size a docked control keeps (AutoSize controls keep their preferred size)
        internal virtual Size GetDockedSize(Size proposed) { return proposed; }
        internal void SetBoundsCoreFromLayout(int x, int y, int width, int height)
        {
            bool inLayout = _parent != null && _parent._inLayout;
            if (_parent != null) _parent._inLayout = true;
            try
            {
                SetBoundsCore(x, y, width, height, BoundsSpecified.All);
            }
            finally
            {
                if (_parent != null) _parent._inLayout = inLayout;
            }
        }
        public virtual Size GetPreferredSize(Size proposedSize)
        {
            if (_controls == null || _controls.Count == 0) return _bounds.Size;
            int w = 0, h = 0;
            foreach (Control c in _controls)
            {
                if (!c._visible) continue;
                w = Math.Max(w, c.Right + c.Margin.Right);
                h = Math.Max(h, c.Bottom + c.Margin.Bottom);
            }
            Padding insets = NonClientInsets;
            return new Size(w + _padding.Right + insets.Horizontal, h + _padding.Bottom + insets.Vertical);
        }
        internal virtual void AdjustAutoSize()
        {
            if (!_autoSize || _dock == DockStyle.Fill) return;
            Size preferred = GetPreferredSize(Size.Empty);
            if (preferred != _bounds.Size)
            {
                int w = _dock == DockStyle.Top || _dock == DockStyle.Bottom ? _bounds.Width : preferred.Width;
                int h = _dock == DockStyle.Left || _dock == DockStyle.Right ? _bounds.Height : preferred.Height;
                Size = new Size(w, h);
            }
        }
        public void Scale(SizeF factor) { }
        public void Scale(float ratio) { }
        protected virtual void ScaleControl(SizeF factor, BoundsSpecified specified) { }
        protected virtual Rectangle GetScaledBounds(Rectangle bounds, SizeF factor, BoundsSpecified specified) { return bounds; }

        // Z-order and visibility
        public void BringToFront()
        {
            if (_parent != null) _parent.Controls.SetChildIndex(this, 0);
            else BringTopLevelToFront();
        }
        internal virtual void BringTopLevelToFront() { }
        public void SendToBack()
        {
            if (_parent != null) _parent.Controls.SetChildIndex(this, _parent.Controls.Count - 1);
        }
        public void Show() { Visible = true; }
        public void Hide() { Visible = false; }
        // Top level forms start hidden; WinForms raises no VisibleChanged while the derived form is constructed
        internal void InitializeVisible(bool value)
        {
            _visible = value;
            if (_host != null) _host.IsVisible = value;
        }
        protected virtual void SetVisibleCore(bool value)
        {
            if (_visible == value) return;
            bool oldVisible = Visible;
            _visible = value;
            if (_host != null) _host.IsVisible = value;
            if (value && !_created && (_parent == null || _parent._created)) CreateControl();
            if (_parent != null && (_dock != DockStyle.None || _parent.AutoSize)) _parent.PerformLayout(this, "Visible");
            if (oldVisible != Visible) OnVisibleChanged(EventArgs.Empty);
        }
        public bool Contains(Control ctl)
        {
            while (ctl != null)
            {
                ctl = ctl._parent;
                if (ctl == this) return true;
            }
            return false;
        }
        public Form FindForm()
        {
            Control c = this;
            while (c != null && !(c is Form)) c = c._parent;
            return c as Form;
        }
        public IContainerControl GetContainerControl()
        {
            Control c = _parent;
            while (c != null && !(c is IContainerControl)) c = c._parent;
            return c as IContainerControl;
        }
        public Control GetChildAtPoint(Point pt) { return GetChildAtPoint(pt, GetChildAtPointSkip.None); }
        public Control GetChildAtPoint(Point pt, GetChildAtPointSkip skipValue)
        {
            if (_controls == null) return null;
            foreach (Control c in _controls)
            {
                if ((skipValue & GetChildAtPointSkip.Invisible) != 0 && !c.Visible) continue;
                if ((skipValue & GetChildAtPointSkip.Disabled) != 0 && !c.Enabled) continue;
                if (c._visible && c._bounds.Contains(pt)) return c;
            }
            return null;
        }
        public Control GetNextControl(Control ctl, bool forward)
        {
            List<Control> ordered = new List<Control>();
            CollectTabOrder(this, ordered);
            int index = ctl == null ? -1 : ordered.IndexOf(ctl);
            if (forward) return index + 1 < ordered.Count ? ordered[index + 1] : null;
            if (index < 0) index = ordered.Count;
            return index - 1 >= 0 ? ordered[index - 1] : null;
        }
        private static void CollectTabOrder(Control parent, List<Control> list)
        {
            if (parent._controls == null) return;
            foreach (Control c in parent._controls.Cast<Control>().OrderBy(c => c.TabIndex))
            {
                list.Add(c);
                CollectTabOrder(c, list);
            }
        }
        public bool SelectNextControl(Control ctl, bool forward, bool tabStopOnly, bool nested, bool wrap)
        {
            Control next = ctl;
            for (int i = 0; i < 1000; i++)
            {
                next = GetNextControl(next, forward);
                if (next == null)
                {
                    if (!wrap) return false;
                    wrap = false;
                    continue;
                }
                if (next.CanSelect && (!tabStopOnly || next.TabStop) && next.Content != null)
                {
                    next.Select();
                    return true;
                }
            }
            return false;
        }

        // Focus
        public bool Focus()
        {
            if (!CanFocus) return false;
            AC.Control target = Content ?? Host;
            if (Content == null) Host.Focusable = true;
            target.Focus();
            return true;
        }
        public void Select() { Select(false, false); }
        protected virtual void Select(bool directed, bool forward)
        {
            Focus();
            Form form = FindForm();
            if (form != null) form.ActiveControlInternal = this;
        }

        // Painting
        public void Invalidate()
        {
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess()) { Toolkit.OnUiThread(Invalidate); return; }
            _host?.ClearOverlayPictures();
            _host?.InvalidateVisual();
            _host?.InvalidateContent();
            OnInvalidated(new InvalidateEventArgs(ClientRectangle));
        }
        public void Invalidate(bool invalidateChildren)
        {
            Invalidate();
            if (invalidateChildren && _controls != null) foreach (Control c in _controls) c.Invalidate(true);
        }
        public void Invalidate(Rectangle rc) { Invalidate(); }
        public void Invalidate(Rectangle rc, bool invalidateChildren) { Invalidate(invalidateChildren); }
        public void Invalidate(Region region) { Invalidate(); }
        public void Invalidate(Region region, bool invalidateChildren) { Invalidate(invalidateChildren); }
        public void Update() { }
        public virtual void Refresh() { Invalidate(true); }
        // Drawing outside of OnPaint stays on the control until it is invalidated
        public Graphics CreateGraphics()
        {
            Size client = ClientSize;
            SkiaSharp.SKPictureRecorder recorder = new SkiaSharp.SKPictureRecorder();
            SkiaSharp.SKCanvas canvas = recorder.BeginRecording(new SkiaSharp.SKRect(0, 0, Math.Max(1, client.Width), Math.Max(1, client.Height)));
            return Graphics.FromCanvas(canvas, new SizeF(client.Width, client.Height), () =>
            {
                SkiaSharp.SKPicture picture = recorder.EndRecording();
                recorder.Dispose();
                Host.AddOverlayPicture(picture);
            });
        }
        public void DrawToBitmap(Bitmap bitmap, Rectangle targetBounds)
        {
            Toolkit.RenderToBitmap(this, bitmap, targetBounds);
        }
        protected virtual void OnPaint(PaintEventArgs e) { Paint?.Invoke(this, e); }
        protected virtual void OnPaintBackground(PaintEventArgs pevent)
        {
            if (_backgroundImage != null) Toolkit.DrawBackgroundImage(pevent.Graphics, _backgroundImage, ClientRectangle, _backgroundImageLayout);
        }
        internal void PaintInternal(Graphics g, Rectangle clip)
        {
            PaintEventArgs e = new PaintEventArgs(g, clip);
            try
            {
                OnPaintBackground(e);
                OnPaint(e);
            }
            catch (Exception ex)
            {
                Application.OnThreadException(ex);
            }
        }
        protected void InvokePaint(Control c, PaintEventArgs e) { c.OnPaint(e); }
        protected void InvokePaintBackground(Control c, PaintEventArgs e) { c.OnPaintBackground(e); }

        // Coordinates
        public Point PointToScreen(Point p)
        {
            Point origin = Toolkit.ClientOriginOnScreen(this);
            return new Point(origin.X + p.X, origin.Y + p.Y);
        }
        public Point PointToClient(Point p)
        {
            Point origin = Toolkit.ClientOriginOnScreen(this);
            return new Point(p.X - origin.X, p.Y - origin.Y);
        }
        public Rectangle RectangleToScreen(Rectangle r) { return new Rectangle(PointToScreen(r.Location), r.Size); }
        public Rectangle RectangleToClient(Rectangle r) { return new Rectangle(PointToClient(r.Location), r.Size); }

        // Invoke
        public object Invoke(Delegate method) { return Invoke(method, null); }
        public object Invoke(Delegate method, params object[] args)
        {
            if (Dispatcher.UIThread.CheckAccess()) return method.DynamicInvoke(args);
            return Dispatcher.UIThread.Invoke(() => method.DynamicInvoke(args));
        }
        public T Invoke<T>(Func<T> method)
        {
            if (Dispatcher.UIThread.CheckAccess()) return method();
            return Dispatcher.UIThread.Invoke(method);
        }
        public void Invoke(Action method)
        {
            if (Dispatcher.UIThread.CheckAccess()) method();
            else Dispatcher.UIThread.Invoke(method);
        }
        public IAsyncResult BeginInvoke(Delegate method) { return BeginInvoke(method, null); }
        public IAsyncResult BeginInvoke(Delegate method, params object[] args)
        {
            InvokeAsyncResult result = new InvokeAsyncResult();
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    result.Result = method.DynamicInvoke(args);
                }
                catch (TargetInvocationException ex)
                {
                    Application.OnThreadException(ex.InnerException ?? ex);
                }
                catch (Exception ex)
                {
                    Application.OnThreadException(ex);
                }
                finally
                {
                    result.Complete();
                }
            });
            return result;
        }
        public IAsyncResult BeginInvoke(Action method) { return BeginInvoke((Delegate)method, null); }
        public object EndInvoke(IAsyncResult asyncResult)
        {
            InvokeAsyncResult result = (InvokeAsyncResult)asyncResult;
            if (!result.IsCompleted && Dispatcher.UIThread.CheckAccess())
            {
                while (!result.IsCompleted) Dispatcher.UIThread.RunJobs();
            }
            result.AsyncWaitHandle.WaitOne();
            return result.Result;
        }

        // Drag and drop
        public DragDropEffects DoDragDrop(object data, DragDropEffects allowedEffects)
        {
            return Toolkit.DoDragDrop(this, data, allowedEffects);
        }

        // Context menu
        internal void ShowContextMenu(Point clientLocation)
        {
            if (_contextMenuStrip != null) _contextMenuStrip.Show(this, clientLocation);
        }


        // Event raising
        protected virtual void OnAutoSizeChanged(EventArgs e) { AutoSizeChanged?.Invoke(this, e); }
        protected virtual void OnBackColorChanged(EventArgs e)
        {
            UpdateNativeColors();
            Invalidate();
            BackColorChanged?.Invoke(this, e);
            if (_controls != null) foreach (Control c in _controls) c.OnParentBackColorChanged(e);
        }
        protected virtual void OnBackgroundImageChanged(EventArgs e) { BackgroundImageChanged?.Invoke(this, e); }
        protected virtual void OnBackgroundImageLayoutChanged(EventArgs e) { BackgroundImageLayoutChanged?.Invoke(this, e); }
        protected virtual void OnBindingContextChanged(EventArgs e) { BindingContextChanged?.Invoke(this, e); }
        protected virtual void OnCausesValidationChanged(EventArgs e) { CausesValidationChanged?.Invoke(this, e); }
        protected virtual void OnClick(EventArgs e) { Click?.Invoke(this, e); }
        internal void RaiseClick(EventArgs e) { OnClick(e); }
        protected virtual void OnClientSizeChanged(EventArgs e) { ClientSizeChanged?.Invoke(this, e); }
        protected virtual void OnContextMenuStripChanged(EventArgs e) { ContextMenuStripChanged?.Invoke(this, e); }
        protected virtual void OnControlAdded(ControlEventArgs e) { ControlAdded?.Invoke(this, e); }
        protected virtual void OnControlRemoved(ControlEventArgs e) { ControlRemoved?.Invoke(this, e); }
        protected virtual void OnCursorChanged(EventArgs e) { CursorChanged?.Invoke(this, e); }
        protected virtual void OnDockChanged(EventArgs e) { DockChanged?.Invoke(this, e); }
        protected virtual void OnDoubleClick(EventArgs e) { DoubleClick?.Invoke(this, e); }
        protected virtual void OnDragDrop(DragEventArgs drgevent) { DragDrop?.Invoke(this, drgevent); }
        protected virtual void OnDragEnter(DragEventArgs drgevent) { DragEnter?.Invoke(this, drgevent); }
        protected virtual void OnDragLeave(EventArgs e) { DragLeave?.Invoke(this, e); }
        protected virtual void OnDragOver(DragEventArgs drgevent) { DragOver?.Invoke(this, drgevent); }
        internal void RaiseDragDrop(DragEventArgs e) { OnDragDrop(e); }
        internal void RaiseDragEnter(DragEventArgs e) { OnDragEnter(e); }
        internal void RaiseDragOver(DragEventArgs e) { OnDragOver(e); }
        internal void RaiseDragLeave(EventArgs e) { OnDragLeave(e); }
        protected virtual void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            EnabledChanged?.Invoke(this, e);
            if (_controls != null) foreach (Control c in _controls) c.OnParentEnabledChanged(e);
        }
        protected virtual void OnEnter(EventArgs e) { Enter?.Invoke(this, e); }
        protected virtual void OnFontChanged(EventArgs e)
        {
            UpdateNativeFont();
            if (_autoSize) AdjustAutoSize();
            Invalidate();
            FontChanged?.Invoke(this, e);
            if (_controls != null) foreach (Control c in _controls) c.OnParentFontChanged(e);
        }
        protected virtual void OnForeColorChanged(EventArgs e)
        {
            UpdateNativeColors();
            Invalidate();
            ForeColorChanged?.Invoke(this, e);
            if (_controls != null) foreach (Control c in _controls) c.OnParentForeColorChanged(e);
        }
        protected virtual void OnGiveFeedback(GiveFeedbackEventArgs gfbevent) { GiveFeedback?.Invoke(this, gfbevent); }
        protected virtual void OnGotFocus(EventArgs e) { GotFocus?.Invoke(this, e); }
        internal void RaiseGotFocus() { OnGotFocus(EventArgs.Empty); }
        protected virtual void OnHandleCreated(EventArgs e) { HandleCreated?.Invoke(this, e); }
        protected virtual void OnHandleDestroyed(EventArgs e) { HandleDestroyed?.Invoke(this, e); }
        protected virtual void OnHelpRequested(HelpEventArgs hevent) { HelpRequested?.Invoke(this, hevent); }
        protected virtual void OnInvalidated(InvalidateEventArgs e) { Invalidated?.Invoke(this, e); }
        protected virtual void OnKeyDown(KeyEventArgs e) { KeyDown?.Invoke(this, e); }
        protected virtual void OnKeyPress(KeyPressEventArgs e) { KeyPress?.Invoke(this, e); }
        protected virtual void OnKeyUp(KeyEventArgs e) { KeyUp?.Invoke(this, e); }
        internal void RaiseKeyDown(KeyEventArgs e)
        {
            OnKeyDown(e);
            OnKeyDownCompleted(e);
        }
        // Called after OnKeyDown: controls that do their key processing in OnKeyDown (FastColoredTextBox) handle the keys
        // a derived class kept from them
        protected internal virtual void OnKeyDownCompleted(KeyEventArgs e) { }
        internal void RaiseKeyPress(KeyPressEventArgs e) { OnKeyPress(e); }
        internal void RaiseKeyUp(KeyEventArgs e) { OnKeyUp(e); }
        protected virtual void OnLeave(EventArgs e) { Leave?.Invoke(this, e); }
        internal void RaiseEnter() { OnEnter(EventArgs.Empty); }
        internal void RaiseLeave() { OnLeave(EventArgs.Empty); }
        protected virtual void OnLocationChanged(EventArgs e)
        {
            OnMove(EventArgs.Empty);
            LocationChanged?.Invoke(this, e);
        }
        protected virtual void OnLostFocus(EventArgs e) { LostFocus?.Invoke(this, e); }
        internal void RaiseLostFocus() { OnLostFocus(EventArgs.Empty); }
        protected virtual void OnMarginChanged(EventArgs e) { MarginChanged?.Invoke(this, e); }
        protected virtual void OnMouseCaptureChanged(EventArgs e) { MouseCaptureChanged?.Invoke(this, e); }
        protected virtual void OnMouseClick(MouseEventArgs e) { MouseClick?.Invoke(this, e); }
        protected virtual void OnMouseDoubleClick(MouseEventArgs e) { MouseDoubleClick?.Invoke(this, e); }
        protected virtual void OnMouseDown(MouseEventArgs e) { MouseDown?.Invoke(this, e); }
        protected virtual void OnMouseEnter(EventArgs e) { MouseEnter?.Invoke(this, e); }
        protected virtual void OnMouseHover(EventArgs e) { MouseHover?.Invoke(this, e); }
        protected virtual void OnMouseLeave(EventArgs e) { MouseLeave?.Invoke(this, e); }
        protected virtual void OnMouseMove(MouseEventArgs e) { MouseMove?.Invoke(this, e); }
        protected virtual void OnMouseUp(MouseEventArgs e) { MouseUp?.Invoke(this, e); }
        protected virtual void OnMouseWheel(MouseEventArgs e) { MouseWheel?.Invoke(this, e); }
        protected virtual void OnMove(EventArgs e) { Move?.Invoke(this, e); }
        protected virtual void OnPaddingChanged(EventArgs e) { PaddingChanged?.Invoke(this, e); }
        protected virtual void OnParentBackColorChanged(EventArgs e) { if (_backColor.IsEmpty) OnBackColorChanged(e); }
        protected virtual void OnParentBindingContextChanged(EventArgs e) { }
        protected virtual void OnParentChanged(EventArgs e)
        {
            ParentChanged?.Invoke(this, e);
        }
        protected virtual void OnParentEnabledChanged(EventArgs e) { if (_enabled) OnEnabledChanged(e); }
        protected virtual void OnParentFontChanged(EventArgs e) { if (_font == null) OnFontChanged(e); }
        protected virtual void OnParentForeColorChanged(EventArgs e) { if (_foreColor.IsEmpty) OnForeColorChanged(e); }
        protected virtual void OnParentVisibleChanged(EventArgs e) { if (_visible) OnVisibleChanged(e); }
        protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e) { PreviewKeyDown?.Invoke(this, e); }
        internal void RaisePreviewKeyDown(PreviewKeyDownEventArgs e) { OnPreviewKeyDown(e); }
        protected virtual void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent) { QueryContinueDrag?.Invoke(this, qcdevent); }
        protected virtual void OnRegionChanged(EventArgs e) { RegionChanged?.Invoke(this, e); }
        protected virtual void OnResize(EventArgs e)
        {
            if (ResizeRedraw) Invalidate();
            Resize?.Invoke(this, e);
        }
        protected virtual void OnRightToLeftChanged(EventArgs e) { RightToLeftChanged?.Invoke(this, e); }
        protected virtual void OnSizeChanged(EventArgs e)
        {
            OnResize(EventArgs.Empty);
            SizeChanged?.Invoke(this, e);
        }
        protected virtual void OnStyleChanged(EventArgs e) { StyleChanged?.Invoke(this, e); }
        protected virtual void OnSystemColorsChanged(EventArgs e) { SystemColorsChanged?.Invoke(this, e); }
        protected virtual void OnTabIndexChanged(EventArgs e) { TabIndexChanged?.Invoke(this, e); }
        protected virtual void OnTabStopChanged(EventArgs e) { TabStopChanged?.Invoke(this, e); }
        protected virtual void OnTextChanged(EventArgs e) { TextChanged?.Invoke(this, e); }
        protected virtual void OnValidated(EventArgs e) { Validated?.Invoke(this, e); }
        protected virtual void OnValidating(CancelEventArgs e) { Validating?.Invoke(this, e); }
        internal bool RaiseValidating()
        {
            CancelEventArgs e = new CancelEventArgs();
            OnValidating(e);
            if (!e.Cancel) OnValidated(EventArgs.Empty);
            return !e.Cancel;
        }
        protected virtual void OnVisibleChanged(EventArgs e)
        {
            VisibleChanged?.Invoke(this, e);
            if (_controls != null) foreach (Control c in _controls.ToArray()) c.OnParentVisibleChanged(e);
        }
        protected virtual void OnChangeUICues(UICuesEventArgs e) { ChangeUICues?.Invoke(this, e); }
        protected virtual void OnImeModeChanged(EventArgs e) { ImeModeChanged?.Invoke(this, e); }
        protected virtual void OnTabControlSelectionChanged() { }
        protected void RaiseKeyEvent(object key, KeyEventArgs e) { }
        protected void RaiseMouseEvent(object key, MouseEventArgs e) { }
        protected void RaisePaintEvent(object key, PaintEventArgs e) { }
        protected void InvokeOnClick(Control toInvoke, EventArgs e) { toInvoke?.OnClick(e); }
        protected void InvokeGotFocus(Control toInvoke, EventArgs e) { toInvoke?.OnGotFocus(e); }
        protected void InvokeLostFocus(Control toInvoke, EventArgs e) { toInvoke?.OnLostFocus(e); }
        public void ResetBackColor() { BackColor = Color.Empty; }
        public void ResetForeColor() { ForeColor = Color.Empty; }
        public void ResetFont() { Font = null; }
        public void ResetCursor() { Cursor = null; }
        public void ResetText() { Text = string.Empty; }
        public virtual void ResetRightToLeft() { }
        public void ResetBindings() { }
        public void ResetImeMode() { }

        // Mouse input from the host
        internal void HandleMouseDown(MouseEventArgs e)
        {
            if (FiltersMouseMessages && !FilterMessage(MouseMessage(e.Button, e.Clicks >= 2 ? 2 : 0), IntPtr.Zero, PointParam(e.X, e.Y))) return;
            _mouseDownInside = true;
            _mouseDownButton = e.Button;
            if (e.Clicks >= 2 && GetStyle(ControlStyles.StandardDoubleClick))
            {
                OnMouseDown(e);
                OnDoubleClick(EventArgs.Empty);
                OnMouseDoubleClick(e);
            }
            else OnMouseDown(e);
        }
        internal void HandleMouseUp(MouseEventArgs e, bool inside)
        {
            if (FiltersMouseMessages && !FilterMessage(MouseMessage(e.Button, 1), IntPtr.Zero, PointParam(e.X, e.Y)))
            {
                _mouseDownInside = false;
                return;
            }
            OnMouseUp(e);
            if (_mouseDownInside && inside && GetStyle(ControlStyles.StandardClick) && !ContentHandlesClick)
            {
                OnClick(e);
                OnMouseClick(e);
            }
            if (e.Button == MouseButtons.Right && inside && _contextMenuStrip != null) ShowContextMenu(e.Location);
            _mouseDownInside = false;
        }
        // Controls whose Avalonia visual raises Click (buttons) do not take the click from the mouse up
        protected internal virtual bool ContentHandlesClick { get { return false; } }
        internal void HandleMouseMove(MouseEventArgs e)
        {
            if (FiltersMouseMessages && !FilterMessage(0x200, IntPtr.Zero, PointParam(e.X, e.Y))) return;
            OnMouseMove(e);
        }
        internal void HandleMouseWheel(MouseEventArgs e)
        {
            if (FiltersMouseMessages && !FilterMessage(0x20A, new IntPtr(e.Delta << 16), PointParam(e.X, e.Y))) return;
            OnMouseWheel(e);
        }
        internal void HandleMouseEnter()
        {
            if (_mouseInside) return;
            _mouseInside = true;
            OnMouseEnter(EventArgs.Empty);
            Toolkit.StartHover(this);
        }
        internal void HandleMouseLeave()
        {
            if (!_mouseInside) return;
            _mouseInside = false;
            Toolkit.StopHover(this);
            OnMouseLeave(EventArgs.Empty);
        }
        internal void HandleMouseHover() { OnMouseHover(EventArgs.Empty); }
        internal bool MouseIsInside { get { return _mouseInside; } }

        // Children
        internal void SetParentInternal(Control parent)
        {
            _parent = parent;
            if (parent != null)
            {
                UpdateAnchorInfo();
                if (parent._created && _visible) CreateControl();
            }
            OnParentChanged(EventArgs.Empty);
            if (_font == null) UpdateNativeFont();
            if (_foreColor.IsEmpty || _backColor.IsEmpty) UpdateNativeColors();
        }
        internal void RaiseControlAdded(Control c) { OnControlAdded(new ControlEventArgs(c)); }
        internal void RaiseControlRemoved(Control c) { OnControlRemoved(new ControlEventArgs(c)); }

        // Dispose
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposing = true;
            if (disposing)
            {
                if (_controls != null)
                {
                    foreach (Control c in _controls.ToArray()) c.Dispose();
                }
                _parent?.Controls.Remove(this);
                OnHandleDestroyed(EventArgs.Empty);
            }
            _disposed = true;
            lock (_handles) _handles.Remove(_handle);
            _disposing = false;
            base.Dispose(disposing);
        }

        public override string ToString()
        {
            string text = Text;
            return GetType().FullName + (string.IsNullOrEmpty(text) ? "" : ", Text: " + text);
        }

        // ControlCollection
        [ListBindable(false)]
        public class ControlCollection : ArrangedElementCollection, IList, ICloneable
        {
            private readonly Control _owner;
            private readonly List<Control> _list = new List<Control>();

            public Control Owner { get { return _owner; } }
            public ControlCollection(Control owner) { _owner = owner; }

            public override int Count { get { return _list.Count; } }
            public virtual Control this[int index] { get { return _list[index]; } }
            public virtual Control this[string key]
            {
                get
                {
                    int index = IndexOfKey(key);
                    return index < 0 ? null : _list[index];
                }
            }
            public virtual void Add(Control value)
            {
                if (value == null) return;
                if (value._parent == _owner)
                {
                    // WinForms moves the control to the end
                    SetChildIndex(value, _list.Count - 1);
                    return;
                }
                value._parent?.Controls.Remove(value);
                _list.Add(value);
                _owner.Host.AddChild(value.Host);
                value.SetParentInternal(_owner);
                _owner.RaiseControlAdded(value);
                _owner.PerformLayout(value, "Parent");
            }
            public virtual void AddRange(Control[] controls)
            {
                _owner.SuspendLayout();
                try
                {
                    foreach (Control c in controls) Add(c);
                }
                finally
                {
                    _owner.ResumeLayout(true);
                }
            }
            public virtual void Remove(Control value)
            {
                if (value == null || !_list.Remove(value)) return;
                _owner.Host.RemoveChild(value.Host);
                value._parent = null;
                value.AnchorInfoValid = false;
                value.OnParentChanged(EventArgs.Empty);
                _owner.RaiseControlRemoved(value);
                _owner.PerformLayout(value, "Parent");
            }
            public void RemoveAt(int index) { Remove(_list[index]); }
            public virtual void RemoveByKey(string key)
            {
                int index = IndexOfKey(key);
                if (index >= 0) RemoveAt(index);
            }
            public virtual void Clear()
            {
                _owner.SuspendLayout();
                try
                {
                    foreach (Control c in _list.ToArray()) Remove(c);
                }
                finally
                {
                    _owner.ResumeLayout(true);
                }
            }
            public bool Contains(Control control) { return _list.Contains(control); }
            public virtual bool ContainsKey(string key) { return IndexOfKey(key) >= 0; }
            public int IndexOf(Control control) { return _list.IndexOf(control); }
            public virtual int IndexOfKey(string key)
            {
                if (string.IsNullOrEmpty(key)) return -1;
                for (int i = 0; i < _list.Count; i++)
                    if (string.Equals(_list[i].Name, key, StringComparison.OrdinalIgnoreCase)) return i;
                return -1;
            }
            public Control[] Find(string key, bool searchAllChildren)
            {
                List<Control> found = new List<Control>();
                foreach (Control c in _list)
                {
                    if (string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase)) found.Add(c);
                    if (searchAllChildren && c.HasChildren) found.AddRange(c.Controls.Find(key, true));
                }
                return found.ToArray();
            }
            public int GetChildIndex(Control child) { return GetChildIndex(child, true); }
            public virtual int GetChildIndex(Control child, bool throwException)
            {
                int index = _list.IndexOf(child);
                if (index < 0 && throwException) throw new ArgumentException("The control is not a child control.");
                return index;
            }
            public virtual void SetChildIndex(Control child, int newIndex)
            {
                int index = _list.IndexOf(child);
                if (index < 0) return;
                if (newIndex < 0 || newIndex >= _list.Count) newIndex = _list.Count - 1;
                if (index == newIndex) return;
                _list.RemoveAt(index);
                _list.Insert(newIndex, child);
                _owner.Host.UpdateZOrder();
                _owner.PerformLayout(child, "ChildIndex");
            }
            public Control[] ToArray() { return _list.ToArray(); }
            public override IEnumerator GetEnumerator() { return _list.GetEnumerator(); }
            public object Clone() { return new ArrayList(_list); }
            // IList
            int IList.Add(object value) { Add((Control)value); return _list.Count - 1; }
            bool IList.Contains(object value) { return value is Control c && Contains(c); }
            int IList.IndexOf(object value) { return value is Control c ? IndexOf(c) : -1; }
            void IList.Insert(int index, object value) { throw new NotSupportedException(); }
            void IList.Remove(object value) { Remove(value as Control); }
            bool IList.IsFixedSize { get { return false; } }
            bool IList.IsReadOnly { get { return false; } }
            object IList.this[int index] { get { return _list[index]; } set { throw new NotSupportedException(); } }
            internal IEnumerable<Control> Items { get { return _list; } }
        }
    }

    public class ArrangedElementCollection : IList
    {
        public virtual int Count { get { return 0; } }
        public virtual IEnumerator GetEnumerator() { return new object[0].GetEnumerator(); }
        public bool IsSynchronized { get { return false; } }
        public object SyncRoot { get { return this; } }
        public void CopyTo(Array array, int index)
        {
            int i = index;
            foreach (object o in this) array.SetValue(o, i++);
        }
        int IList.Add(object value) { throw new NotSupportedException(); }
        void IList.Clear() { throw new NotSupportedException(); }
        bool IList.Contains(object value) { foreach (object o in this) if (Equals(o, value)) return true; return false; }
        int IList.IndexOf(object value) { int i = 0; foreach (object o in this) { if (Equals(o, value)) return i; i++; } return -1; }
        void IList.Insert(int index, object value) { throw new NotSupportedException(); }
        void IList.Remove(object value) { throw new NotSupportedException(); }
        void IList.RemoveAt(int index) { throw new NotSupportedException(); }
        bool IList.IsFixedSize { get { return true; } }
        bool IList.IsReadOnly { get { return true; } }
        object IList.this[int index]
        {
            get { int i = 0; foreach (object o in this) { if (i++ == index) return o; } throw new ArgumentOutOfRangeException(); }
            set { throw new NotSupportedException(); }
        }
    }

    [Flags]
    public enum GetChildAtPointSkip { None = 0, Invisible = 1, Disabled = 2, Transparent = 4 }

    public enum AccessibleRole { Default = -1, None = 0 }

    public class WindowTarget { }

    public abstract class LayoutEngine
    {
        public static readonly LayoutEngine Default = new DefaultLayoutEngine();
        public virtual bool Layout(object container, LayoutEventArgs layoutEventArgs) { return false; }
        public virtual void InitLayout(object child, BoundsSpecified specified) { }
        private class DefaultLayoutEngine : LayoutEngine { }
    }

    public class ControlBindingsCollection : BindingsCollection
    {
        public Binding Add(string propertyName, object dataSource, string dataMember) { return new Binding(propertyName, dataSource, dataMember); }
        public Binding Add(string propertyName, object dataSource, string dataMember, bool formattingEnabled)
        {
            return new Binding(propertyName, dataSource, dataMember);
        }
        public void Clear() { }
    }

    public class BindingsCollection : IEnumerable
    {
        public int Count { get { return 0; } }
        public IEnumerator GetEnumerator() { return new object[0].GetEnumerator(); }
    }

    public class Binding
    {
        public string PropertyName { get; }
        public object DataSource { get; }
        public Binding(string propertyName, object dataSource, string dataMember)
        {
            PropertyName = propertyName;
            DataSource = dataSource;
        }
    }

    public class BindingContext : ICollection
    {
        private readonly Dictionary<object, BindingManagerBase> _managers = new Dictionary<object, BindingManagerBase>();
        public BindingManagerBase this[object dataSource] { get { return this[dataSource, string.Empty]; } }
        public BindingManagerBase this[object dataSource, string dataMember]
        {
            get
            {
                if (dataSource == null) return null;
                if (!_managers.TryGetValue(dataSource, out BindingManagerBase manager))
                {
                    IList list = dataSource as IList ?? (dataSource as IListSource)?.GetList();
                    manager = list != null ? (BindingManagerBase)new CurrencyManager(list) : new PropertyManager(dataSource);
                    _managers[dataSource] = manager;
                }
                return manager;
            }
        }
        public bool Contains(object dataSource) { return _managers.ContainsKey(dataSource); }
        public bool Contains(object dataSource, string dataMember) { return Contains(dataSource); }
        public bool IsReadOnly { get { return false; } }
        int ICollection.Count { get { return _managers.Count; } }
        bool ICollection.IsSynchronized { get { return false; } }
        object ICollection.SyncRoot { get { return this; } }
        void ICollection.CopyTo(Array array, int index) { }
        IEnumerator IEnumerable.GetEnumerator() { return _managers.Values.GetEnumerator(); }
    }

    internal sealed class InvokeAsyncResult : IAsyncResult
    {
        private readonly System.Threading.ManualResetEvent _event = new System.Threading.ManualResetEvent(false);
        public object Result;
        public object AsyncState { get { return null; } }
        public System.Threading.WaitHandle AsyncWaitHandle { get { return _event; } }
        public bool CompletedSynchronously { get { return false; } }
        public bool IsCompleted { get; private set; }
        public void Complete()
        {
            IsCompleted = true;
            _event.Set();
        }
    }
}
