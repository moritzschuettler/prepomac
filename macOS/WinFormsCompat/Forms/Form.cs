// PrePoMax for macOS - Application, Form (an Avalonia window), containers, screens and timers of System.Windows.Forms.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using SkiaSharp;
using AC = Avalonia.Controls;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;

namespace System.Windows.Forms
{
    public interface IMessageFilter
    {
        bool PreFilterMessage(ref Message m);
    }

    public enum UnhandledExceptionMode { Automatic = 0, ThrowException = 1, CatchException = 2 }

    public enum HighDpiMode { DpiUnaware = 0, SystemAware = 1, PerMonitor = 2, PerMonitorV2 = 3, DpiUnawareGdiScaled = 4 }

    public class ApplicationContext : IDisposable
    {
        public Form MainForm { get; set; }
        public object Tag { get; set; }
        public event EventHandler ThreadExit;
        public ApplicationContext() { }
        public ApplicationContext(Form mainForm) { MainForm = mainForm; }
        public void ExitThread() { ThreadExit?.Invoke(this, EventArgs.Empty); Application.Exit(); }
        public void Dispose() { }
    }

    public class FormCollection : ReadOnlyCollectionBase
    {
        internal void Add(Form form) { if (!InnerList.Contains(form)) InnerList.Add(form); }
        internal void Remove(Form form) { InnerList.Remove(form); }
        public virtual Form this[int index] { get { return (Form)InnerList[index]; } }
        public virtual Form this[string name]
        {
            get
            {
                foreach (Form f in InnerList) if (string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)) return f;
                return null;
            }
        }
    }

    public sealed class Application
    {
        private static readonly FormCollection _openForms = new FormCollection();
        private static CancellationTokenSource _mainLoop;
        private static int _modalLevel;
        private static readonly List<IMessageFilter> _filters = new List<IMessageFilter>();

        public static FormCollection OpenForms { get { return _openForms; } }
        public static string StartupPath { get { return AppContext.BaseDirectory.TrimEnd('/', '\\'); } }
        public static string ExecutablePath { get { return Environment.ProcessPath; } }
        public static string ProductName { get; set; } = "PrePoMax";
        public static string CompanyName { get; set; } = "PrePoMax";
        public static string ProductVersion
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly();
                var info = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
                if (!string.IsNullOrEmpty(info))
                {
                    int plus = info.IndexOf('+');
                    return plus > 0 ? info.Substring(0, plus) : info;
                }
                return (assembly?.GetName().Version ?? new Version(3, 0, 0, 0)).ToString();
            }
        }
        public static string UserAppDataPath
        {
            get { return IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), CompanyName, ProductName, ProductVersion); }
        }
        public static string LocalUserAppDataPath { get { return UserAppDataPath; } }
        public static string CommonAppDataPath { get { return UserAppDataPath; } }
        public static Globalization.CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
            set { Thread.CurrentThread.CurrentCulture = value; }
        }
        public static bool MessageLoop { get { return _mainLoop != null; } }
        public static bool UseWaitCursor
        {
            get { return _useWaitCursor; }
            set
            {
                _useWaitCursor = value;
                foreach (Form f in _openForms) f.UseWaitCursor = value;
            }
        }
        private static bool _useWaitCursor;
        public static bool RenderWithVisualStyles { get { return true; } }
        public static bool AllowQuit { get { return true; } }

        public static event System.Threading.ThreadExceptionEventHandler ThreadException;
        public static event EventHandler Idle;
        public static event EventHandler ApplicationExit;
        public static event EventHandler ThreadExit;
        public static event EventHandler EnterThreadModal;
        public static event EventHandler LeaveThreadModal;

        public static void EnableVisualStyles() { Toolkit.EnsureInitialized(); }
        public static void SetCompatibleTextRenderingDefault(bool defaultValue) { }
        public static bool SetHighDpiMode(HighDpiMode highDpiMode) { return true; }
        public static void SetUnhandledExceptionMode(UnhandledExceptionMode mode) { }
        public static void SetUnhandledExceptionMode(UnhandledExceptionMode mode, bool threadScope) { }
        public static void AddMessageFilter(IMessageFilter value) { _filters.Add(value); }
        public static void RemoveMessageFilter(IMessageFilter value) { _filters.Remove(value); }
        public static void DoEvents()
        {
            if (!Toolkit.IsInitialized || !Dispatcher.UIThread.CheckAccess()) return;
            Dispatcher.UIThread.RunJobs();
        }
        public static void Run() { RunLoop(null); }
        public static void Run(Form mainForm) { RunLoop(mainForm); }
        public static void Run(ApplicationContext context) { RunLoop(context.MainForm); }
        private static void RunLoop(Form mainForm)
        {
            Toolkit.EnsureInitialized();
            _mainLoop = new CancellationTokenSource();
            // The automation runs from the start: the main form can ask questions while it loads
            Automation.Start();
            if (mainForm != null)
            {
                mainForm.FormClosed += (s, e) => _mainLoop?.Cancel();
                mainForm.Show();
            }
            DispatcherTimer idle = new DispatcherTimer(TimeSpan.FromMilliseconds(100), DispatcherPriority.ApplicationIdle,
                                                       (s, e) => RaiseIdle());
            idle.Start();
            Dispatcher.UIThread.MainLoop(_mainLoop.Token);
            idle.Stop();
            _mainLoop = null;
            ApplicationExit?.Invoke(null, EventArgs.Empty);
            ThreadExit?.Invoke(null, EventArgs.Empty);
        }
        private static void RaiseIdle()
        {
            try
            {
                Idle?.Invoke(null, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                OnThreadException(ex);
            }
        }
        public static void Exit()
        {
            foreach (Form form in _openForms.Cast<Form>().ToArray())
            {
                try
                {
                    form.CloseInternal(CloseReason.ApplicationExitCall);
                }
                catch { }
            }
            _mainLoop?.Cancel();
        }
        public static void Exit(CancelEventArgs e) { Exit(); }
        public static void ExitThread() { Exit(); }
        public static void Restart() { Exit(); }
        public static void RaiseIdle(EventArgs e) { Idle?.Invoke(null, e); }
        // Runs the dispatcher until the condition is met (modal windows and message boxes)
        internal static void RunModal(Func<bool> done)
        {
            _modalLevel++;
            EnterThreadModal?.Invoke(null, EventArgs.Empty);
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    DispatcherTimer check = new DispatcherTimer(TimeSpan.FromMilliseconds(30), DispatcherPriority.Background, (s, e) =>
                    {
                        if (done()) cts.Cancel();
                    });
                    check.Start();
                    if (!done()) Dispatcher.UIThread.MainLoop(cts.Token);
                    check.Stop();
                }
            }
            finally
            {
                _modalLevel--;
                LeaveThreadModal?.Invoke(null, EventArgs.Empty);
            }
        }
        internal static bool IsModal { get { return _modalLevel > 0; } }
        public static void OnThreadException(Exception t)
        {
            if (t is TargetInvocationException tie && tie.InnerException != null) t = tie.InnerException;
            if (ThreadException != null)
            {
                try
                {
                    ThreadException(null, new System.Threading.ThreadExceptionEventArgs(t));
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }
            }
            else
            {
                Console.Error.WriteLine(t);
                if (Toolkit.IsInitialized && Dispatcher.UIThread.CheckAccess())
                    MessageBox.Show(t.Message + Environment.NewLine + Environment.NewLine + t.StackTrace, "Unhandled exception",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        internal static void FormOpened(Form form) { _openForms.Add(form); }
        internal static void FormClosed(Form form) { _openForms.Remove(form); }
    }

    public class ScrollableControl : Control
    {
        private bool _autoScroll;
        private Size _autoScrollMinSize;
        private Size _autoScrollMargin;
        private Point _scrollPosition;      // positive offsets
        private AC.Primitives.ScrollBar _vScroll;
        private AC.Primitives.ScrollBar _hScroll;
        private bool _updatingScrollBars;
        public HScrollProperties HorizontalScroll { get; }
        public VScrollProperties VerticalScroll { get; }
        public DockPaddingEdges DockPadding { get; }

        public ScrollableControl()
        {
            HorizontalScroll = new HScrollProperties(this);
            VerticalScroll = new VScrollProperties(this);
            DockPadding = new DockPaddingEdges(this);
            SetStyle(ControlStyles.Selectable, false);
        }

        public virtual bool AutoScroll
        {
            get { return _autoScroll; }
            set
            {
                if (_autoScroll == value) return;
                _autoScroll = value;
                if (!value) _scrollPosition = Point.Empty;
                PerformLayout();
                UpdateScrollBars();
            }
        }
        public Size AutoScrollMinSize { get { return _autoScrollMinSize; } set { _autoScrollMinSize = value; UpdateScrollBars(); } }
        public Size AutoScrollMargin { get { return _autoScrollMargin; } set { _autoScrollMargin = value; UpdateScrollBars(); } }
        public Point AutoScrollPosition
        {
            get { return new Point(-_scrollPosition.X, -_scrollPosition.Y); }
            set { SetScrollPosition(Math.Abs(value.X), Math.Abs(value.Y)); }
        }
        protected bool HScroll { get { return _hScroll != null && _hScroll.IsVisible; } set { } }
        protected bool VScroll { get { return _vScroll != null && _vScroll.IsVisible; } set { } }
        public event ScrollEventHandler Scroll;
        protected virtual void OnScroll(ScrollEventArgs se) { Scroll?.Invoke(this, se); }

        public override Rectangle DisplayRectangle
        {
            get
            {
                Rectangle client = ClientRectangle;
                if (_vScroll != null && _vScroll.IsVisible) client.Width -= SystemInformation.VerticalScrollBarWidth;
                if (_hScroll != null && _hScroll.IsVisible) client.Height -= SystemInformation.HorizontalScrollBarHeight;
                Padding p = Padding;
                Rectangle display = new Rectangle(client.X + p.Left, client.Y + p.Top,
                                                  Math.Max(0, client.Width - p.Horizontal), Math.Max(0, client.Height - p.Vertical));
                if (_autoScroll)
                {
                    Size extent = ScrollExtent();
                    display.Width = Math.Max(display.Width, extent.Width - p.Horizontal);
                    display.Height = Math.Max(display.Height, extent.Height - p.Vertical);
                }
                return display;
            }
        }
        internal override Point ChildOrigin
        {
            get
            {
                Point origin = base.ChildOrigin;
                return new Point(origin.X - _scrollPosition.X, origin.Y - _scrollPosition.Y);
            }
        }
        // Size of the area covered by the children
        private Size ScrollExtent()
        {
            int w = _autoScrollMinSize.Width, h = _autoScrollMinSize.Height;
            if (HasChildren)
            {
                foreach (Control c in Controls.Items)
                {
                    if (!c.VisibleSelf || c.Dock == DockStyle.Fill) continue;
                    if (c.Dock != DockStyle.Top && c.Dock != DockStyle.Bottom)
                        w = Math.Max(w, c.Right + _scrollPosition.X + _autoScrollMargin.Width);
                    if (c.Dock != DockStyle.Left && c.Dock != DockStyle.Right)
                        h = Math.Max(h, c.Bottom + _scrollPosition.Y + _autoScrollMargin.Height);
                }
            }
            return new Size(w, h);
        }
        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            UpdateScrollBars();
        }
        internal void UpdateScrollBars()
        {
            if (_updatingScrollBars) return;
            _updatingScrollBars = true;
            try
            {
                if (!_autoScroll)
                {
                    if (_vScroll != null) _vScroll.IsVisible = false;
                    if (_hScroll != null) _hScroll.IsVisible = false;
                    return;
                }
                Size client = ClientSize;
                Size extent = ScrollExtent();
                bool needV = extent.Height > client.Height;
                bool needH = extent.Width > client.Width - (needV ? SystemInformation.VerticalScrollBarWidth : 0);
                if (needH && !needV) needV = extent.Height > client.Height - SystemInformation.HorizontalScrollBarHeight;
                EnsureScrollBars();
                int viewW = client.Width - (needV ? SystemInformation.VerticalScrollBarWidth : 0);
                int viewH = client.Height - (needH ? SystemInformation.HorizontalScrollBarHeight : 0);
                _vScroll.IsVisible = needV;
                _hScroll.IsVisible = needH;
                _vScroll.Maximum = Math.Max(0, extent.Height - viewH);
                _vScroll.ViewportSize = viewH;
                _vScroll.LargeChange = Math.Max(1, viewH);
                _vScroll.SmallChange = 20;
                _hScroll.Maximum = Math.Max(0, extent.Width - viewW);
                _hScroll.ViewportSize = viewW;
                _hScroll.LargeChange = Math.Max(1, viewW);
                _hScroll.SmallChange = 20;
                int x = needH ? Math.Min(_scrollPosition.X, (int)_hScroll.Maximum) : 0;
                int y = needV ? Math.Min(_scrollPosition.Y, (int)_vScroll.Maximum) : 0;
                if (x != _scrollPosition.X || y != _scrollPosition.Y) SetScrollPosition(x, y);
                _vScroll.Value = _scrollPosition.Y;
                _hScroll.Value = _scrollPosition.X;
                Host.InvalidateArrange();
            }
            finally
            {
                _updatingScrollBars = false;
            }
        }
        private void EnsureScrollBars()
        {
            if (_vScroll != null) return;
            _vScroll = new AC.Primitives.ScrollBar { Orientation = Avalonia.Layout.Orientation.Vertical, AllowAutoHide = false };
            _hScroll = new AC.Primitives.ScrollBar { Orientation = Avalonia.Layout.Orientation.Horizontal, AllowAutoHide = false };
            _vScroll.ValueChanged += (s, e) => { if (!_updatingScrollBars) SetScrollPosition(_scrollPosition.X, (int)e.NewValue); };
            _hScroll.ValueChanged += (s, e) => { if (!_updatingScrollBars) SetScrollPosition((int)e.NewValue, _scrollPosition.Y); };
            Host.AddOverlay(_vScroll, () => new Rect(Width - NonClientInsets.Right - SystemInformation.VerticalScrollBarWidth, NonClientInsets.Top,
                                                     SystemInformation.VerticalScrollBarWidth,
                                                     Math.Max(0, ClientSize.Height - (HScroll ? SystemInformation.HorizontalScrollBarHeight : 0))));
            Host.AddOverlay(_hScroll, () => new Rect(NonClientInsets.Left, Height - NonClientInsets.Bottom - SystemInformation.HorizontalScrollBarHeight,
                                                     Math.Max(0, ClientSize.Width - (VScroll ? SystemInformation.VerticalScrollBarWidth : 0)),
                                                     SystemInformation.HorizontalScrollBarHeight));
        }
        private void SetScrollPosition(int x, int y)
        {
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            if (_hScroll != null && _hScroll.IsVisible) x = Math.Min(x, (int)_hScroll.Maximum); else x = 0;
            if (_vScroll != null && _vScroll.IsVisible) y = Math.Min(y, (int)_vScroll.Maximum); else y = 0;
            if (x == _scrollPosition.X && y == _scrollPosition.Y) return;
            Point old = _scrollPosition;
            _scrollPosition = new Point(x, y);
            if (_vScroll != null && !_updatingScrollBars)
            {
                _updatingScrollBars = true;
                _vScroll.Value = y;
                _hScroll.Value = x;
                _updatingScrollBars = false;
            }
            Host.InvalidateArrange();
            if (old.Y != y) OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, old.Y, y, ScrollOrientation.VerticalScroll));
            if (old.X != x) OnScroll(new ScrollEventArgs(ScrollEventType.ThumbPosition, old.X, x, ScrollOrientation.HorizontalScroll));
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (_autoScroll && VScroll)
            {
                SetScrollPosition(_scrollPosition.X, _scrollPosition.Y - e.Delta * SystemInformation.MouseWheelScrollLines * 16 / 120);
                if (e is HandledMouseEventArgs h) h.Handled = true;
            }
            base.OnMouseWheel(e);
        }
        public void ScrollControlIntoView(Control activeControl)
        {
            if (!_autoScroll || activeControl == null) return;
            Rectangle client = ClientRectangle;
            Rectangle b = activeControl.Bounds;
            int y = _scrollPosition.Y, x = _scrollPosition.X;
            if (b.Bottom > client.Bottom) y += b.Bottom - client.Bottom;
            if (b.Top < 0) y += b.Top;
            if (b.Right > client.Right) x += b.Right - client.Right;
            if (b.Left < 0) x += b.Left;
            SetScrollPosition(x, y);
        }
        public void SetAutoScrollMargin(int x, int y) { AutoScrollMargin = new Size(x, y); }
        protected void SetDisplayRectLocation(int x, int y) { SetScrollPosition(-x, -y); }
        protected virtual Point ScrollToControl(Control activeControl) { return AutoScrollPosition; }
        protected void AdjustFormScrollbars(bool displayScrollbars) { UpdateScrollBars(); }

        public class DockPaddingEdges
        {
            private readonly ScrollableControl _owner;
            internal DockPaddingEdges(ScrollableControl owner) { _owner = owner; }
            public int All { get { return _owner.Padding.All; } set { _owner.Padding = new Padding(value); } }
            public int Left { get { return _owner.Padding.Left; } set { Padding p = _owner.Padding; p.Left = value; _owner.Padding = p; } }
            public int Top { get { return _owner.Padding.Top; } set { Padding p = _owner.Padding; p.Top = value; _owner.Padding = p; } }
            public int Right { get { return _owner.Padding.Right; } set { Padding p = _owner.Padding; p.Right = value; _owner.Padding = p; } }
            public int Bottom { get { return _owner.Padding.Bottom; } set { Padding p = _owner.Padding; p.Bottom = value; _owner.Padding = p; } }
        }
    }

    public abstract class ScrollProperties
    {
        protected readonly ScrollableControl Owner;
        protected ScrollProperties(ScrollableControl owner) { Owner = owner; }
        public bool Enabled { get; set; } = true;
        public int LargeChange { get; set; } = 10;
        public int SmallChange { get; set; } = 1;
        public int Maximum { get; set; } = 100;
        public int Minimum { get; set; }
        public int Value
        {
            get { return IsVertical ? -Owner.AutoScrollPosition.Y : -Owner.AutoScrollPosition.X; }
            set
            {
                Point p = Owner.AutoScrollPosition;
                Owner.AutoScrollPosition = IsVertical ? new Point(-p.X, value) : new Point(value, -p.Y);
            }
        }
        public bool Visible { get; set; }
        protected abstract bool IsVertical { get; }
    }

    public class HScrollProperties : ScrollProperties
    {
        public HScrollProperties(ScrollableControl container) : base(container) { }
        protected override bool IsVertical { get { return false; } }
    }

    public class VScrollProperties : ScrollProperties
    {
        public VScrollProperties(ScrollableControl container) : base(container) { }
        protected override bool IsVertical { get { return true; } }
    }

    public class ContainerControl : ScrollableControl, IContainerControl
    {
        private Control _activeControl;
        public SizeF AutoScaleDimensions { get; set; }
        public SizeF CurrentAutoScaleDimensions { get { return AutoScaleDimensions; } }
        public AutoScaleMode AutoScaleMode { get; set; }
        public AutoValidate AutoValidate { get; set; } = AutoValidate.EnablePreventFocusChange;
        public SizeF AutoScaleFactor { get { return new SizeF(1, 1); } }
        public Control ActiveControl
        {
            get { return _activeControl; }
            set
            {
                _activeControl = value;
                if (value != null && value.Visible) value.Focus();
            }
        }
        internal Control ActiveControlInternal
        {
            get { return _activeControl; }
            set { _activeControl = value; }
        }
        public Form ParentForm
        {
            get
            {
                Control c = Parent;
                while (c != null && !(c is Form)) c = c.Parent;
                return c as Form;
            }
        }
        public event EventHandler AutoValidateChanged;
        public ContainerControl()
        {
            SetStyle(ControlStyles.ContainerControl, true);
        }
        public bool ActivateControl(Control active)
        {
            ActiveControl = active;
            return true;
        }
        public bool Validate() { return Validate(false); }
        public bool Validate(bool checkAutoValidate)
        {
            return _activeControl == null || _activeControl.RaiseValidating();
        }
        public virtual bool ValidateChildren() { return ValidateChildren(ValidationConstraints.Selectable); }
        public virtual bool ValidateChildren(ValidationConstraints validationConstraints)
        {
            bool valid = true;
            foreach (Control c in Controls) valid &= c.RaiseValidating();
            return valid;
        }
        public void PerformAutoScale() { }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            return base.ProcessDialogKey(keyData);
        }
        protected virtual void UpdateDefaultButton() { }
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
        }
    }

    public class UserControl : ContainerControl
    {
        private BorderStyle _borderStyle;
        public event EventHandler Load;
        private bool _loaded;
        public UserControl()
        {
            SetStyle(ControlStyles.Selectable, true);
        }
        protected override Size DefaultSize { get { return new Size(150, 150); } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                _borderStyle = value;
                SetClientSizeOffset(BorderSizes.Of(value));
                PerformLayout();
                Host.InvalidateVisual();
            }
        }
        public override bool AutoSize { get { return base.AutoSize; } set { base.AutoSize = value; } }
        public AutoSizeMode AutoSizeMode { get; set; }
        public override string Text { get { return base.Text; } set { base.Text = value; } }
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height)
        {
            BorderSizes.Paint(canvas, _borderStyle, width, height);
        }
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            if (!_loaded)
            {
                _loaded = true;
                OnLoad(EventArgs.Empty);
            }
        }
        protected virtual void OnLoad(EventArgs e) { Load?.Invoke(this, e); }
    }

    // Borders of WinForms controls: FixedSingle is one gray line, Fixed3D a sunken edge of two pixels
    internal static class BorderSizes
    {
        public static Size Of(BorderStyle style)
        {
            int b = style == BorderStyle.FixedSingle ? 2 : style == BorderStyle.Fixed3D ? 4 : 0;
            return new Size(b, b);
        }
        public static Padding Insets(BorderStyle style)
        {
            return style == BorderStyle.FixedSingle ? new Padding(1) : style == BorderStyle.Fixed3D ? new Padding(2) : Padding.Empty;
        }
        public static void Paint(SKCanvas canvas, BorderStyle style, float width, float height)
        {
            if (style == BorderStyle.None) return;
            using (SKPaint paint = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = 1, IsAntialias = false })
            {
                if (style == BorderStyle.FixedSingle)
                {
                    paint.Color = new SKColor(0x64, 0x64, 0x64);
                    canvas.DrawRect(new SKRect(0.5f, 0.5f, width - 0.5f, height - 0.5f), paint);
                }
                else
                {
                    paint.Color = new SKColor(0xA0, 0xA0, 0xA0);
                    canvas.DrawLine(0.5f, 0.5f, width - 0.5f, 0.5f, paint);
                    canvas.DrawLine(0.5f, 0.5f, 0.5f, height - 0.5f, paint);
                    paint.Color = SKColors.White;
                    canvas.DrawLine(0.5f, height - 0.5f, width - 0.5f, height - 0.5f, paint);
                    canvas.DrawLine(width - 0.5f, 0.5f, width - 0.5f, height - 0.5f, paint);
                    paint.Color = new SKColor(0x69, 0x69, 0x69);
                    canvas.DrawLine(1.5f, 1.5f, width - 1.5f, 1.5f, paint);
                    canvas.DrawLine(1.5f, 1.5f, 1.5f, height - 1.5f, paint);
                    paint.Color = new SKColor(0xE3, 0xE3, 0xE3);
                    canvas.DrawLine(1.5f, height - 1.5f, width - 1.5f, height - 1.5f, paint);
                    canvas.DrawLine(width - 1.5f, 1.5f, width - 1.5f, height - 1.5f, paint);
                }
            }
        }
    }

    public class Form : ContainerControl
    {
        // Size of the window frame of a sizable WinForms window on Windows 10 (Size - ClientSize)
        private static readonly Size SizableFrame = new Size(16, 39);
        private static readonly Size FixedFrame = new Size(16, 39);
        private FormWindow _window;
        private FormBorderStyle _formBorderStyle = FormBorderStyle.Sizable;
        private FormStartPosition _startPosition = FormStartPosition.WindowsDefaultLocation;
        private FormWindowState _windowState = FormWindowState.Normal;
        private DialogResult _dialogResult;
        private bool _modal;
        private bool _closing;
        private bool _shown;
        private bool _loaded;
        private bool _topMost;
        private bool _showInTaskbar = true;
        private Form _owner;
        private readonly List<Form> _ownedForms = new List<Form>();
        private Icon _icon;
        private double _opacity = 1;
        private bool _keyPreview;
        private bool _locationSet;
        private Point _location;
        private bool _suppressKeyPress;
        private static Form _activeForm;

        public Form()
        {
            InitializeHidden();
            SetClientSizeOffset(SizableFrame);
            SetStyle(ControlStyles.Selectable, false);
            Size = new Size(300, 300);
        }
        private void InitializeHidden() { InitializeVisible(false); }

        // Properties
        internal AC.Window Window { get { return _window; } }
        // The Avalonia window of the form (null while the form is not shown)
        public AC.Window NativeWindow { get { return _window; } }
        public static Form ActiveForm { get { return _activeForm; } }
        public IButtonControl AcceptButton { get; set; }
        public IButtonControl CancelButton { get; set; }
        public bool KeyPreview { get { return _keyPreview; } set { _keyPreview = value; } }
        public bool ControlBox { get; set; } = true;
        public bool MaximizeBox { get; set; } = true;
        public bool MinimizeBox { get; set; } = true;
        public bool HelpButton { get; set; }
        public bool ShowIcon { get; set; } = true;
        public bool IsMdiContainer { get; set; }
        public bool IsMdiChild { get { return false; } }
        public Form MdiParent { get; set; }
        public Form[] MdiChildren { get { return new Form[0]; } }
        public MenuStrip MainMenuStrip { get; set; }
        public SizeGripStyle SizeGripStyle { get; set; }
        public Color TransparencyKey { get; set; }
        public bool AllowTransparency { get; set; }
        public bool AutoScroll_ { get { return AutoScroll; } }
        public bool RightToLeftLayout { get; set; }
        public bool Modal { get { return _modal; } }
        public bool IsRestrictedWindow { get { return false; } }
        public bool TopLevel { get; set; } = true;
        public FormBorderStyle FormBorderStyle
        {
            get { return _formBorderStyle; }
            set
            {
                Size client = ClientSize;
                _formBorderStyle = value;
                SetClientSizeOffset(value == FormBorderStyle.None ? Size.Empty : value == FormBorderStyle.Sizable ||
                                    value == FormBorderStyle.SizableToolWindow ? SizableFrame : FixedFrame);
                ClientSize = client;
                UpdateWindowProperties();
            }
        }
        public FormStartPosition StartPosition { get { return _startPosition; } set { _startPosition = value; } }
        public FormWindowState WindowState
        {
            get
            {
                if (_window != null)
                {
                    switch (_window.WindowState)
                    {
                        case AC.WindowState.Maximized:
                        case AC.WindowState.FullScreen: return FormWindowState.Maximized;
                        case AC.WindowState.Minimized: return FormWindowState.Minimized;
                        default: return FormWindowState.Normal;
                    }
                }
                return _windowState;
            }
            set
            {
                _windowState = value;
                if (_window != null) _window.WindowState = ToAvalonia(value);
            }
        }
        private static AC.WindowState ToAvalonia(FormWindowState state)
        {
            return state == FormWindowState.Maximized ? AC.WindowState.Maximized :
                   state == FormWindowState.Minimized ? AC.WindowState.Minimized : AC.WindowState.Normal;
        }
        public bool TopMost
        {
            get { return _topMost; }
            set
            {
                _topMost = value;
                if (_window != null) _window.Topmost = value;
            }
        }
        protected virtual bool ShowWithoutActivation { get { return false; } }
        public bool ShowInTaskbar
        {
            get { return _showInTaskbar; }
            set
            {
                _showInTaskbar = value;
                if (_window != null) _window.ShowInTaskbar = value;
            }
        }
        public double Opacity
        {
            get { return _opacity; }
            set
            {
                _opacity = value;
                if (_window != null) _window.Opacity = value;
            }
        }
        public Icon Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                UpdateWindowIcon();
            }
        }
        public Form Owner
        {
            get { return _owner; }
            set
            {
                _owner?._ownedForms.Remove(this);
                _owner = value;
                value?._ownedForms.Add(this);
            }
        }
        public Form[] OwnedForms { get { return _ownedForms.ToArray(); } }
        public void AddOwnedForm(Form ownedForm) { if (ownedForm != null) ownedForm.Owner = this; }
        public void RemoveOwnedForm(Form ownedForm) { if (ownedForm != null && ownedForm.Owner == this) ownedForm.Owner = null; }
        public DialogResult DialogResult
        {
            get { return _dialogResult; }
            set
            {
                _dialogResult = value;
                // Setting a result closes a modal form
                if (_modal && value != DialogResult.None && !_closing) Dispatcher.UIThread.Post(() => CloseInternal(CloseReason.None));
            }
        }
        public new Point Location
        {
            get
            {
                // Off the UI thread the window cannot be asked, and the position last set is the answer
                if (_window != null && Dispatcher.UIThread.CheckAccess() && _window.IsVisible && !IsPlacedOffScreen)
                {
                    double scaling = _window.DesktopScaling;
                    return new Point((int)Math.Round(_window.Position.X / scaling), (int)Math.Round(_window.Position.Y / scaling));
                }
                return _location;
            }
            set
            {
                _location = value;
                _locationSet = true;
                if (_window != null) _window.Position = Toolkit.ToPixelPoint(value, _window);
                base.Location = value;
            }
        }
        public new int Left { get { return Location.X; } set { Location = new Point(value, Location.Y); } }
        public new int Top { get { return Location.Y; } set { Location = new Point(Location.X, value); } }
        public new int Right { get { return Location.X + Width; } }
        public new int Bottom { get { return Location.Y + Height; } }
        public new Rectangle Bounds
        {
            get { return new Rectangle(Location, Size); }
            set
            {
                Location = value.Location;
                Size = value.Size;
            }
        }
        public Rectangle DesktopBounds { get { return Bounds; } set { Bounds = value; } }
        public Point DesktopLocation { get { return Location; } set { Location = value; } }
        public Rectangle RestoreBounds { get { return Bounds; } }
        public override Size MaximumSize
        {
            get { return base.MaximumSize; }
            set
            {
                base.MaximumSize = value;
                UpdateWindowProperties();
            }
        }
        public override Size MinimumSize
        {
            get { return base.MinimumSize; }
            set
            {
                base.MinimumSize = value;
                UpdateWindowProperties();
            }
        }
        protected override Size DefaultSize { get { return new Size(300, 300); } }
        internal override bool IsTopLevelShown { get { return VisibleSelf; } }
        internal override Color PaintedBackColor { get { return BackColor; } }
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                if (_window != null) _window.Title = value;
            }
        }

        // Events
        public event EventHandler Load;
        public event EventHandler Shown;
        public event EventHandler Activated;
        public event EventHandler Deactivate;
        public event FormClosingEventHandler FormClosing;
        public event FormClosedEventHandler FormClosed;
        public event CancelEventHandler Closing;
        public event EventHandler Closed;
        public event EventHandler ResizeBegin;
        public event EventHandler ResizeEnd;
        public event EventHandler MaximizedBoundsChanged;
        public event EventHandler MinimumSizeChanged;
        public event EventHandler MaximumSizeChanged;
        public event CancelEventHandler HelpButtonClicked;
        public event InputLanguageChangedEventHandler_ InputLanguageChanged;
        public delegate void InputLanguageChangedEventHandler_(object sender, EventArgs e);

        protected virtual void OnLoad(EventArgs e) { Load?.Invoke(this, e); }
        protected virtual void OnShown(EventArgs e) { Shown?.Invoke(this, e); }
        protected virtual void OnActivated(EventArgs e) { Activated?.Invoke(this, e); }
        protected virtual void OnDeactivate(EventArgs e) { Deactivate?.Invoke(this, e); }
        protected virtual void OnFormClosing(FormClosingEventArgs e) { FormClosing?.Invoke(this, e); }
        protected virtual void OnFormClosed(FormClosedEventArgs e) { FormClosed?.Invoke(this, e); }
        protected virtual void OnClosing(CancelEventArgs e) { Closing?.Invoke(this, e); }
        protected virtual void OnClosed(EventArgs e) { Closed?.Invoke(this, e); }
        protected virtual void OnResizeBegin(EventArgs e) { ResizeBegin?.Invoke(this, e); }
        protected virtual void OnResizeEnd(EventArgs e) { ResizeEnd?.Invoke(this, e); }

        // Window
        private void EnsureWindow()
        {
            if (_window != null) return;
            _window = new FormWindow(this);
            _window.Title = Text;
            _window.Content = Host;
            _window.Topmost = _topMost;
            _window.ShowInTaskbar = _showInTaskbar;
            _window.ShowActivated = !ShowWithoutActivation;
            _window.Opacity = _opacity;
            UpdateWindowProperties();
            UpdateWindowIcon();
            Size client = ClientSize;
            _window.Width = Math.Max(1, client.Width);
            _window.Height = Math.Max(1, client.Height);
            _window.Resized += (s, e) => OnWindowResized();
            _window.PositionChanged += (s, e) => OnMove(EventArgs.Empty);
            _window.Activated += (s, e) =>
            {
                _activeForm = this;
                Toolkit.Dispatch(() => OnActivated(EventArgs.Empty));
            };
            _window.Deactivated += (s, e) =>
            {
                if (_activeForm == this) _activeForm = null;
                Toolkit.Dispatch(() => OnDeactivate(EventArgs.Empty));
            };
            _window.Closing += OnWindowClosing;
            _window.Closed += OnWindowClosed;
        }
        private void UpdateWindowProperties()
        {
            if (_window == null) return;
            bool sizable = _formBorderStyle == FormBorderStyle.Sizable || _formBorderStyle == FormBorderStyle.SizableToolWindow;
            _window.CanResize = sizable;
            _window.SystemDecorations = _formBorderStyle == FormBorderStyle.None ? AC.SystemDecorations.None : AC.SystemDecorations.Full;
            Size frame = ClientSizeOffset;
            if (MinimumSize.Width > 0) _window.MinWidth = Math.Max(0, MinimumSize.Width - frame.Width);
            if (MinimumSize.Height > 0) _window.MinHeight = Math.Max(0, MinimumSize.Height - frame.Height);
            _window.MaxWidth = MaximumSize.Width > 0 ? MaximumSize.Width - frame.Width : double.PositiveInfinity;
            _window.MaxHeight = MaximumSize.Height > 0 ? MaximumSize.Height - frame.Height : double.PositiveInfinity;
        }
        private Size ClientSizeOffset { get { return new Size(Width - ClientSize.Width, Height - ClientSize.Height); } }
        private void UpdateWindowIcon()
        {
            if (_window == null || _icon == null) return;
            try
            {
                using (Bitmap bitmap = _icon.ToBitmap())
                using (IO.MemoryStream stream = new IO.MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    _window.Icon = new AC.WindowIcon(stream);
                }
            }
            catch { }
        }
        private bool _syncingSize;
        private bool IsPlacedOffScreen { get { return _locationSet && (_location.X <= -10000 || _location.Y <= -10000); } }
        private void OnWindowResized()
        {
            if (_window == null || _syncingSize) return;
            // A window placed off screen, as a session without a window is, is fitted onto the screen by macOS; it keeps
            // the size it was given, as it does on Windows, so the viewport and what is picked in it stay the same
            if (IsPlacedOffScreen) return;
            Size client = new Size((int)Math.Round(_window.ClientSize.Width), (int)Math.Round(_window.ClientSize.Height));
            if (client == ClientSize) return;
            _syncingSize = true;
            try
            {
                Toolkit.Dispatch(() => ClientSize = client);
            }
            finally
            {
                _syncingSize = false;
            }
        }
        internal override void OnBoundsUpdated(Rectangle oldBounds)
        {
            if (_window == null || _syncingSize) return;
            Size client = ClientSize;
            _syncingSize = true;
            try
            {
                if (Math.Abs(_window.Width - client.Width) > 0.5) _window.Width = Math.Max(1, client.Width);
                if (Math.Abs(_window.Height - client.Height) > 0.5) _window.Height = Math.Max(1, client.Height);
            }
            finally
            {
                _syncingSize = false;
            }
        }
        // Forms place their client area in the window: the host has no offset
        internal override Rectangle ContentBounds { get { return new Rectangle(Point.Empty, ClientSize); } }
        protected override void SetVisibleCore(bool value)
        {
            if (value == VisibleSelf && (_window != null || !value)) return;
            if (value) ShowWindow(null);
            else HideWindow();
        }
        private void ShowWindow(AC.Window owner)
        {
            EnsureWindow();
            CreateControl();
            if (!_loaded)
            {
                _loaded = true;
                Toolkit.Dispatch(() => OnLoad(EventArgs.Empty));
                if (IsDisposed) return;
            }
            base.SetVisibleCore(true);
            Application.FormOpened(this);
            PlaceWindow();
            AC.Window ownerWindow = owner ?? _owner?._window;
            if (ownerWindow != null && ownerWindow.IsVisible && !_modal) _window.Show(ownerWindow);
            else if (!_modal) _window.Show();
            if (_windowState != FormWindowState.Normal) _window.WindowState = ToAvalonia(_windowState);
            if (!_shown)
            {
                _shown = true;
                Dispatcher.UIThread.Post(() => Toolkit.Dispatch(() => OnShown(EventArgs.Empty)), DispatcherPriority.Background);
            }
        }
        private void PlaceWindow()
        {
            Size size = Size;
            switch (_startPosition)
            {
                case FormStartPosition.CenterScreen:
                    {
                        Rectangle area = Screen.PrimaryScreen.WorkingArea;
                        _window.Position = Toolkit.ToPixelPoint(new Point(area.X + (area.Width - size.Width) / 2,
                                                                          area.Y + (area.Height - size.Height) / 2), _window);
                        break;
                    }
                case FormStartPosition.CenterParent:
                    {
                        Form parent = _owner ?? _activeForm;
                        if (parent != null && parent != this && parent._window != null)
                        {
                            Rectangle b = parent.Bounds;
                            _window.Position = Toolkit.ToPixelPoint(new Point(b.X + (b.Width - size.Width) / 2,
                                                                              b.Y + (b.Height - size.Height) / 2), _window);
                        }
                        else goto case FormStartPosition.CenterScreen;
                        break;
                    }
                case FormStartPosition.Manual:
                    _window.Position = Toolkit.ToPixelPoint(_location, _window);
                    break;
                default:
                    if (_locationSet) _window.Position = Toolkit.ToPixelPoint(_location, _window);
                    else goto case FormStartPosition.CenterScreen;
                    break;
            }
        }
        private void HideWindow()
        {
            if (_window != null && _window.IsVisible) _window.Hide();
            base.SetVisibleCore(false);
        }
        public new void Show() { Visible = true; }
        public void Show(IWin32Window owner)
        {
            if (owner is Form f && f != this) Owner = f;
            else if (owner is Control c && c.FindForm() is Form cf && cf != this) Owner = cf;
            ShowWindow(_owner?._window);
        }
        public DialogResult ShowDialog() { return ShowDialog(null); }
        public DialogResult ShowDialog(IWin32Window owner)
        {
            // A dialog shown from another thread (a splash screen) is shown by the UI thread; the caller waits for it
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                using (System.Threading.ManualResetEventSlim closed = new System.Threading.ManualResetEventSlim(false))
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        FormClosed += (s, e) => closed.Set();
                        try { Show(); }
                        catch { closed.Set(); }
                    });
                    closed.Wait();
                }
                return _dialogResult == DialogResult.None ? DialogResult.Cancel : _dialogResult;
            }
            Form ownerForm = owner as Form ?? (owner as Control)?.FindForm() ?? _activeForm;
            if (ownerForm == this) ownerForm = null;
            if (ownerForm != null && _startPosition == FormStartPosition.WindowsDefaultLocation) _startPosition = FormStartPosition.CenterParent;
            _dialogResult = DialogResult.None;
            _modal = true;
            _closing = false;
            bool finished = false;
            try
            {
                ShowWindow(ownerForm?._window);
                AC.Window ownerWindow = ownerForm?._window;
                if (ownerWindow != null && ownerWindow.IsVisible) _ = _window.ShowDialog(ownerWindow);
                else _window.Show();
                FormClosed += OnModalClosed;
                void OnModalClosed(object s, FormClosedEventArgs e) { finished = true; }
                _window.Closed += (s, e) => finished = true;
                Application.RunModal(() => finished || !VisibleSelf);
                FormClosed -= OnModalClosed;
            }
            finally
            {
                _modal = false;
            }
            if (_dialogResult == DialogResult.None) _dialogResult = DialogResult.Cancel;
            ownerForm?.Activate();
            return _dialogResult;
        }
        public void Close() { CloseInternal(CloseReason.UserClosing); }
        internal void CloseInternal(CloseReason reason)
        {
            if (_closing || IsDisposed) return;
            if (_window == null)
            {
                Dispose();
                return;
            }
            _closeReason = reason;
            _window.Close();
        }
        private CloseReason _closeReason = CloseReason.UserClosing;
        private void OnWindowClosing(object sender, AC.WindowClosingEventArgs e)
        {
            if (_closing) return;
            FormClosingEventArgs args = new FormClosingEventArgs(_closeReason, false);
            Toolkit.Dispatch(() =>
            {
                OnClosing(args);
                OnFormClosing(args);
            });
            _closeReason = CloseReason.UserClosing;
            if (args.Cancel)
            {
                e.Cancel = true;
                // A modal form that cancels the closing stays open; the result is reset
                if (_modal) _dialogResult = DialogResult.None;
                return;
            }
            _closing = true;
        }
        private void OnWindowClosed(object sender, EventArgs e)
        {
            Application.FormClosed(this);
            base.SetVisibleCore(false);
            FormWindow window = _window;
            _window = null;
            if (window != null) window.Content = null;
            Toolkit.Dispatch(() =>
            {
                OnClosed(EventArgs.Empty);
                OnFormClosed(new FormClosedEventArgs(CloseReason.UserClosing));
            });
            _closing = false;
            _shown = false;
            foreach (Form owned in _ownedForms.ToArray()) if (owned.VisibleSelf) owned.CloseInternal(CloseReason.FormOwnerClosing);
            if (!_modal) Dispose();
        }
        public void Activate()
        {
            if (_window != null && _window.IsVisible) _window.Activate();
        }
        internal override void BringTopLevelToFront() { Activate(); }
        public void CenterToScreen()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.X + (area.Width - Width) / 2, area.Y + (area.Height - Height) / 2);
        }
        protected void CenterToParent()
        {
            if (_owner != null) Location = new Point(_owner.Left + (_owner.Width - Width) / 2, _owner.Top + (_owner.Height - Height) / 2);
            else CenterToScreen();
        }
        public void SetDesktopLocation(int x, int y) { Location = new Point(x, y); }
        public void SetDesktopBounds(int x, int y, int width, int height) { Bounds = new Rectangle(x, y, width, height); }
        public void LayoutMdi(MdiLayout value) { }
        protected override void Dispose(bool disposing)
        {
            if (_window != null)
            {
                FormWindow window = _window;
                _window = null;
                window.Closing -= OnWindowClosing;
                window.Closed -= OnWindowClosed;
                try
                {
                    window.Content = null;
                    window.Close();
                }
                catch { }
                Application.FormClosed(this);
            }
            Owner = null;
            base.Dispose(disposing);
        }

        // Keyboard
        // Menu shortcuts of the tool strips of the form
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            foreach (ToolStrip strip in FindToolStrips(this))
            {
                if (strip.ProcessShortcut(keyData)) return true;
            }
            return false;
        }
        private static IEnumerable<ToolStrip> FindToolStrips(Control parent)
        {
            if (!parent.HasChildren) yield break;
            foreach (Control c in parent.Controls.Items)
            {
                if (c is ToolStrip strip) yield return strip;
                foreach (ToolStrip s in FindToolStrips(c)) yield return s;
            }
        }
        protected override bool ProcessDialogKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if ((keyData & (Keys.Alt | Keys.Control)) == 0)
            {
                if (key == Keys.Enter && AcceptButton is IButtonControl accept)
                {
                    accept.PerformClick();
                    return true;
                }
                if (key == Keys.Escape && CancelButton is IButtonControl cancel)
                {
                    cancel.PerformClick();
                    return true;
                }
            }
            return false;
        }
        internal void OnWindowKeyDown(Avalonia.Input.KeyEventArgs e)
        {
            Input.Update(e.KeyModifiers);
            Keys keyData = Input.ToKeyData(e);
            if ((keyData & Keys.KeyCode) == Keys.None) return;
            Control focused = FocusedControlOfForm();
            _suppressKeyPress = false;
            bool handled = false;
            Toolkit.Dispatch(() =>
            {
                PreviewKeyDownEventArgs preview = new PreviewKeyDownEventArgs(keyData);
                focused.RaisePreviewKeyDown(preview);
                if (focused.ProcessCmdKeyInternal(keyData))
                {
                    handled = true;
                    return;
                }
                KeyEventArgs args = new KeyEventArgs(keyData);
                if (_keyPreview && focused != this)
                {
                    RaiseKeyDown(args);
                    if (args.Handled)
                    {
                        handled = true;
                        _suppressKeyPress = args.SuppressKeyPress;
                        return;
                    }
                }
                focused.RaiseKeyDown(args);
                if (args.Handled)
                {
                    handled = true;
                    _suppressKeyPress = args.SuppressKeyPress;
                    return;
                }
                Keys key = keyData & Keys.KeyCode;
                bool isInput = preview.IsInputKey || focused.IsInputKeyInternal(keyData);
                if (!isInput && (key == Keys.Enter || key == Keys.Escape) && focused.ProcessDialogKeyInternal(keyData))
                {
                    handled = true;
                    return;
                }
                // WinForms raises KeyPress for the keys that produce no text input in Avalonia
                char c = key == Keys.Enter ? '\r' : key == Keys.Escape ? (char)27 : key == Keys.Back ? '\b' : '\0';
                if (c != '\0' && (keyData & (Keys.Control | Keys.Alt)) == 0)
                {
                    KeyPressEventArgs press = new KeyPressEventArgs(c);
                    if (_keyPreview && focused != this) RaiseKeyPress(press);
                    if (!press.Handled) focused.RaiseKeyPress(press);
                    if (press.Handled) handled = true;
                }
            });
            if (handled) e.Handled = true;
        }
        internal void OnWindowKeyUp(Avalonia.Input.KeyEventArgs e)
        {
            Input.Update(e.KeyModifiers);
            Keys keyData = Input.ToKeyData(e);
            if ((keyData & Keys.KeyCode) == Keys.None) return;
            Control focused = FocusedControlOfForm();
            bool handled = false;
            Toolkit.Dispatch(() =>
            {
                KeyEventArgs args = new KeyEventArgs(keyData);
                if (_keyPreview && focused != this) RaiseKeyUp(args);
                if (!args.Handled) focused.RaiseKeyUp(args);
                handled = args.Handled;
            });
            if (handled) e.Handled = true;
        }
        internal void OnWindowTextInput(TextInputEventArgs e)
        {
            if (_suppressKeyPress)
            {
                _suppressKeyPress = false;
                e.Handled = true;
                return;
            }
            if (string.IsNullOrEmpty(e.Text)) return;
            Control focused = FocusedControlOfForm();
            bool handled = false;
            Toolkit.Dispatch(() =>
            {
                foreach (char c in e.Text)
                {
                    KeyPressEventArgs press = new KeyPressEventArgs(c);
                    if (_keyPreview && focused != this) RaiseKeyPress(press);
                    if (!press.Handled) focused.RaiseKeyPress(press);
                    if (press.Handled) handled = true;
                }
            });
            if (handled) e.Handled = true;
        }
        private Control FocusedControlOfForm()
        {
            Control focused = Toolkit.FocusedControl;
            if (focused == null || focused.IsDisposed || focused.FindForm() != this) return ActiveControlInternal ?? this;
            return focused;
        }

        private sealed class FormWindow : AC.Window
        {
            private readonly Form _form;
            protected override Type StyleKeyOverride { get { return typeof(AC.Window); } }
            public FormWindow(Form form)
            {
                _form = form;
                SizeToContent = AC.SizeToContent.Manual;
                AddHandler(KeyDownEvent, (s, e) => _form.OnWindowKeyDown(e), RoutingStrategies.Tunnel);
                AddHandler(KeyUpEvent, (s, e) => _form.OnWindowKeyUp(e), RoutingStrategies.Tunnel);
                AddHandler(TextInputEvent, (s, e) => _form.OnWindowTextInput(e), RoutingStrategies.Tunnel);
            }
        }
    }

    public enum MdiLayout { Cascade = 0, TileHorizontal = 1, TileVertical = 2, ArrangeIcons = 3 }


    public class Screen
    {
        public Rectangle Bounds { get; }
        public Rectangle WorkingArea { get; }
        public bool Primary { get; }
        public string DeviceName { get; }
        public int BitsPerPixel { get { return 32; } }
        private Screen(Rectangle bounds, Rectangle workingArea, bool primary, string name)
        {
            Bounds = bounds;
            WorkingArea = workingArea;
            Primary = primary;
            DeviceName = name;
        }
        public static Screen[] AllScreens
        {
            get
            {
                AC.Window window = null;
                foreach (Form form in Application.OpenForms)
                {
                    if (form.Window?.Screens != null) { window = form.Window; break; }
                }
                var screens = window?.Screens;
                if (screens == null || screens.All.Count == 0)
                {
                    Rectangle fallback = new Rectangle(0, 0, 1920, 1080);
                    return new[] { new Screen(fallback, new Rectangle(0, 25, 1920, 1055), true, "Display1") };
                }
                List<Screen> result = new List<Screen>();
                int i = 1;
                foreach (var s in screens.All)
                {
                    double scale = s.Scaling;
                    Rectangle b = new Rectangle((int)(s.Bounds.X / scale), (int)(s.Bounds.Y / scale), (int)(s.Bounds.Width / scale), (int)(s.Bounds.Height / scale));
                    Rectangle w = new Rectangle((int)(s.WorkingArea.X / scale), (int)(s.WorkingArea.Y / scale),
                                                (int)(s.WorkingArea.Width / scale), (int)(s.WorkingArea.Height / scale));
                    result.Add(new Screen(b, w, s.IsPrimary, "Display" + i++));
                }
                return result.ToArray();
            }
        }
        public static Screen PrimaryScreen
        {
            get
            {
                Screen[] all = AllScreens;
                return all.FirstOrDefault(s => s.Primary) ?? all[0];
            }
        }
        public static Screen FromControl(Control control) { return FromPoint(control.PointToScreen(Point.Empty)); }
        public static Screen FromHandle(IntPtr hwnd) { return PrimaryScreen; }
        public static Screen FromPoint(Point point)
        {
            foreach (Screen s in AllScreens) if (s.Bounds.Contains(point)) return s;
            return PrimaryScreen;
        }
        public static Screen FromRectangle(Rectangle rect) { return FromPoint(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2)); }
        public static Rectangle GetWorkingArea(Control ctl) { return FromControl(ctl).WorkingArea; }
        public static Rectangle GetWorkingArea(Point pt) { return FromPoint(pt).WorkingArea; }
        public static Rectangle GetBounds(Control ctl) { return FromControl(ctl).Bounds; }
        public static Rectangle GetBounds(Point pt) { return FromPoint(pt).Bounds; }
    }

    public static class SystemInformation
    {
        public static bool TerminalServerSession { get { return false; } }
        public static int DoubleClickTime { get { return 500; } }
        public static Size DoubleClickSize { get { return new Size(4, 4); } }
        public static int MouseHoverTime { get { return 400; } }
        public static Size MouseHoverSize { get { return new Size(4, 4); } }
        public static int MouseWheelScrollLines { get { return 3; } }
        public static int MouseWheelScrollDelta { get { return 120; } }
        public static bool MouseWheelPresent { get { return true; } }
        public static int VerticalScrollBarWidth { get { return 17; } }
        public static int HorizontalScrollBarHeight { get { return 17; } }
        public static int VerticalScrollBarArrowHeight { get { return 17; } }
        public static int HorizontalScrollBarArrowWidth { get { return 17; } }
        public static int CaptionHeight { get { return 23; } }
        public static int ToolWindowCaptionHeight { get { return 23; } }
        public static int MenuHeight { get { return 20; } }
        public static Size BorderSize { get { return new Size(1, 1); } }
        public static Size Border3DSize { get { return new Size(2, 2); } }
        public static Size FrameBorderSize { get { return new Size(8, 8); } }
        public static Size FixedFrameBorderSize { get { return new Size(3, 3); } }
        public static Size SmallIconSize { get { return new Size(16, 16); } }
        public static Size IconSize { get { return new Size(32, 32); } }
        public static Size CursorSize { get { return new Size(32, 32); } }
        public static Size DragSize { get { return new Size(4, 4); } }
        public static Size MenuCheckSize { get { return new Size(13, 13); } }
        public static Size PrimaryMonitorSize { get { return Screen.PrimaryScreen.Bounds.Size; } }
        public static Rectangle WorkingArea { get { return Screen.PrimaryScreen.WorkingArea; } }
        public static Rectangle VirtualScreen { get { return Screen.PrimaryScreen.Bounds; } }
        public static int MonitorCount { get { return Screen.AllScreens.Length; } }
        public static int MouseSpeed { get { return 10; } }
        public static int KeyboardDelay { get { return 1; } }
        public static int KeyboardSpeed { get { return 31; } }
        public static int CaretBlinkTime { get { return 530; } }
        public static int CaretWidth { get { return 1; } }
        public static bool HighContrast { get { return false; } }
        public static bool IsDropShadowEnabled { get { return true; } }
        public static string ComputerName { get { return Environment.MachineName; } }
        public static string UserName { get { return Environment.UserName; } }
        public static PowerStatus PowerStatus { get { return new PowerStatus(); } }
    }

    public class PowerStatus
    {
        public float BatteryLifePercent { get { return 1; } }
    }

    [DefaultEvent("Tick")]
    public class Timer : Component
    {
        private readonly DispatcherTimer _timer;
        private int _interval = 100;
        public event EventHandler Tick;
        public object Tag { get; set; }
        public Timer()
        {
            Toolkit.EnsureInitialized();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_interval) };
            _timer.Tick += (s, e) => Toolkit.Dispatch(() => OnTick(EventArgs.Empty));
        }
        public Timer(IContainer container) : this() { container?.Add(this); }
        public virtual bool Enabled
        {
            get { return _timer.IsEnabled; }
            set
            {
                if (value) _timer.Start();
                else _timer.Stop();
            }
        }
        public int Interval
        {
            get { return _interval; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(value));
                _interval = value;
                _timer.Interval = TimeSpan.FromMilliseconds(value);
            }
        }
        public void Start() { Enabled = true; }
        public void Stop() { Enabled = false; }
        protected virtual void OnTick(EventArgs e) { Tick?.Invoke(this, e); }
        protected override void Dispose(bool disposing)
        {
            _timer.Stop();
            base.Dispose(disposing);
        }
    }
}
