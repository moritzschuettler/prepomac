// PrePoMax for macOS - start of Avalonia and the services the WinForms controls share (fonts, focus, hover, input state).

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;
using Avalonia.Platform.Storage;
using AC = Avalonia.Controls;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using Color = System.Drawing.Color;
using FontFamily = Avalonia.Media.FontFamily;

namespace System.Windows.Forms
{
    public partial class Control
    {
        // Rectangle of the Avalonia content in the host (the client area)
        internal virtual Rectangle ContentBounds
        {
            get
            {
                Padding insets = NonClientInsets;
                Size client = ClientSize;
                return new Rectangle(insets.Left, insets.Top, client.Width, client.Height);
            }
        }
        // Position of the client coordinate origin of the children in the host
        internal virtual Point ChildOrigin
        {
            get
            {
                Padding insets = NonClientInsets;
                return new Point(insets.Left, insets.Top);
            }
        }
        internal virtual void OnHostArranged(Avalonia.Size size) { }
        // The background the host paints; empty lets the parent show through (the ambient back color of WinForms)
        internal virtual Color PaintedBackColor
        {
            get
            {
                if (BackColorSet || UsesCustomPaint) return BackColor;
                return Color.Empty;
            }
        }
        internal virtual bool PaintsItself { get { return UsesCustomPaint || BackgroundImage != null || HasNonClientPaint; } }
        internal virtual bool HasNonClientPaint { get { return false; } }
        internal virtual void PaintNonClient(SKCanvas canvas, float width, float height) { }
        internal virtual bool CapturesMouseOverContent { get { return false; } }
        internal virtual bool CanSelectOnClick { get { return GetStyle(ControlStyles.Selectable) && UsesCustomPaint; } }
        internal virtual bool HandlesContextMenu { get { return false; } }
    }

    /// <summary>The compatibility layer: start of Avalonia and shared services of the controls.</summary>
    public static class Toolkit
    {
        private static bool _initialized;
        private static readonly Dictionary<Type, bool> _overridesPaint = new Dictionary<Type, bool>();
        private static DispatcherTimer _hoverTimer;
        private static Control _hoverControl;
        private static Control _focusedControl;

        // The application installs the Avalonia setup (platform and theme); tests use the headless platform
        public static Action InitializeAvalonia { get; set; }
        public static string UiFontFamily { get; set; } = "$Default";
        public static double FontScale { get; set; } = 1.0;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;
            System.Drawing.SystemColorsPatch.Apply();
            Win32Bridge.Register();
            // Diagnostics: PPM_TRACE=1 writes every exception with its stack to the error output
            if (Environment.GetEnvironmentVariable("PPM_TRACE") == "1")
            {
                AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
                {
                    try { Console.Error.WriteLine("[exception] " + e.Exception.GetType().Name + ": " + e.Exception.Message +
                                                  Environment.NewLine + Environment.StackTrace); }
                    catch { }
                };
            }
            if (Avalonia.Application.Current == null)
            {
                if (InitializeAvalonia != null) InitializeAvalonia();
                else CompatApplication.SetupDesktop(Application.ProductName);
            }
            Dispatcher.UIThread.UnhandledException += (s, e) =>
            {
                Application.OnThreadException(e.Exception);
                e.Handled = true;
            };
        }
        public static bool IsInitialized { get { return _initialized; } }

        // Runs an event handler of the PrePoMax code; exceptions go to Application.ThreadException like in WinForms
        public static void Dispatch(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Application.OnThreadException(ex);
            }
        }

        // Fonts and colors
        public static double FontSizeOf(System.Drawing.Font font) { return font.SizeInPixels * FontScale; }
        public static FontFamily FontFamilyOf(System.Drawing.Font font)
        {
            string family = System.Drawing.Font.MapFamily(font.Name);
            return family == null ? (UiFontFamily == "$Default" ? FontFamily.Default : new FontFamily(UiFontFamily)) : new FontFamily(family);
        }
        public static void ApplyFont(AC.Control element, System.Drawing.Font font)
        {
            element.SetValue(TextElement.FontSizeProperty, FontSizeOf(font));
            element.SetValue(TextElement.FontFamilyProperty, FontFamilyOf(font));
            element.SetValue(TextElement.FontWeightProperty, font.Bold ? FontWeight.Bold : FontWeight.Normal);
            element.SetValue(TextElement.FontStyleProperty, font.Italic ? Avalonia.Media.FontStyle.Italic : Avalonia.Media.FontStyle.Normal);
        }
        public static void ApplyForeground(AC.Control element, System.Drawing.Color color)
        {
            element.SetValue(TextElement.ForegroundProperty, ToBrush(color));
        }
        public static IBrush ToBrush(System.Drawing.Color color)
        {
            if (color.IsEmpty) return null;
            return new SolidColorBrush(ToAvalonia(color));
        }
        public static SKColor ToSK(System.Drawing.Color color) { return new SKColor(color.R, color.G, color.B, color.A); }
        public static void DrawBackgroundImagePublic(Graphics g, Image image, Rectangle bounds, ImageLayout layout)
        {
            DrawBackgroundImage(g, image, bounds, layout);
        }
        public static Avalonia.Media.Color ToAvalonia(System.Drawing.Color color)
        {
            return Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public static System.Drawing.Color FromAvalonia(Avalonia.Media.Color color)
        {
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
        public static Avalonia.Media.Imaging.Bitmap ToAvaloniaBitmap(Image image)
        {
            if (image?.SKBitmap == null) return null;
            if (image.NativeCache is Avalonia.Media.Imaging.Bitmap cached) return cached;
            using (SKImage skImage = SKImage.FromBitmap(image.SKBitmap))
            using (SKData data = skImage.Encode(SKEncodedImageFormat.Png, 100))
            using (System.IO.Stream stream = data.AsStream())
            {
                var bitmap = new Avalonia.Media.Imaging.Bitmap(stream);
                image.NativeCache = bitmap;
                return bitmap;
            }
        }
        public static AC.Image CreateImageElement(Image image, double width = double.NaN, double height = double.NaN)
        {
            AC.Image element = new AC.Image { Source = ToAvaloniaBitmap(image), Stretch = Stretch.None };
            if (!double.IsNaN(width)) element.Width = width;
            if (!double.IsNaN(height)) element.Height = height;
            RenderOptions.SetBitmapInterpolationMode(element, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality);
            return element;
        }

        // Painting
        public static bool OverridesPaint(Type type)
        {
            lock (_overridesPaint)
            {
                if (_overridesPaint.TryGetValue(type, out bool value)) return value;
                MethodInfo paint = type.GetMethod("OnPaint", BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                  new[] { typeof(PaintEventArgs) }, null);
                MethodInfo background = type.GetMethod("OnPaintBackground", BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                       new[] { typeof(PaintEventArgs) }, null);
                value = (paint != null && paint.DeclaringType.Assembly != typeof(Control).Assembly) ||
                        (background != null && background.DeclaringType.Assembly != typeof(Control).Assembly);
                _overridesPaint[type] = value;
                return value;
            }
        }
        private static readonly Dictionary<Type, bool> _overridesWndProc = new Dictionary<Type, bool>();
        public static bool OverridesWndProc(Type type)
        {
            lock (_overridesWndProc)
            {
                if (_overridesWndProc.TryGetValue(type, out bool value)) return value;
                MethodInfo method = type.GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic, null,
                                                   new[] { typeof(Message).MakeByRefType() }, null);
                value = method != null && method.DeclaringType.Assembly != typeof(Control).Assembly;
                _overridesWndProc[type] = value;
                return value;
            }
        }
        internal static void DrawBackgroundImage(Graphics g, Image image, Rectangle bounds, ImageLayout layout)
        {
            switch (layout)
            {
                case ImageLayout.None: g.DrawImage(image, bounds.X, bounds.Y); break;
                case ImageLayout.Center:
                    g.DrawImage(image, bounds.X + (bounds.Width - image.Width) / 2, bounds.Y + (bounds.Height - image.Height) / 2); break;
                case ImageLayout.Stretch: g.DrawImage(image, bounds); break;
                case ImageLayout.Zoom:
                    {
                        float scale = Math.Min((float)bounds.Width / image.Width, (float)bounds.Height / image.Height);
                        float w = image.Width * scale, h = image.Height * scale;
                        g.DrawImage(image, new RectangleF(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h));
                        break;
                    }
                default:
                    for (int y = bounds.Y; y < bounds.Bottom; y += Math.Max(1, image.Height))
                        for (int x = bounds.X; x < bounds.Right; x += Math.Max(1, image.Width)) g.DrawImage(image, x, y);
                    break;
            }
        }
        // Renders the Avalonia element of the control into the bitmap
        internal static void RenderToBitmap(Control control, Bitmap bitmap, Rectangle targetBounds)
        {
            AC.Control host = control.NativeControl;
            int w = Math.Max(1, control.Width), h = Math.Max(1, control.Height);
            var target = new Avalonia.Media.Imaging.RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
            target.Render(host);
            using (var stream = new System.IO.MemoryStream())
            {
                target.Save(stream);
                stream.Position = 0;
                using (Bitmap rendered = new Bitmap(stream))
                using (Graphics g = Graphics.FromImage(bitmap))
                    g.DrawImage(rendered, targetBounds.X, targetBounds.Y);
            }
        }

        // Coordinates
        // Screen coordinates are device independent pixels of the main screen
        internal static System.Drawing.Point ClientOriginOnScreen(Control control)
        {
            AC.Control host = control.NativeControl;
            Padding insets = control.NonClientInsets;
            var topLevel = AC.TopLevel.GetTopLevel(host);
            if (topLevel == null)
            {
                // Not shown: the bounds relative to the top level control
                int x = insets.Left, y = insets.Top;
                for (Control c = control; c != null; c = c.Parent)
                {
                    x += c.Left;
                    y += c.Top;
                }
                return new System.Drawing.Point(x, y);
            }
            PixelPoint p = host.PointToScreen(new Avalonia.Point(insets.Left, insets.Top));
            double scaling = DesktopScaling(topLevel);
            return new System.Drawing.Point((int)Math.Round(p.X / scaling), (int)Math.Round(p.Y / scaling));
        }
        internal static double DesktopScaling(AC.TopLevel topLevel)
        {
            return topLevel is AC.WindowBase window ? window.DesktopScaling : topLevel?.RenderScaling ?? 1.0;
        }
        // Screen coordinates of Avalonia are desktop pixels: points on macOS (desktop scaling 1), device pixels elsewhere
        public static PixelPoint ToPixelPoint(System.Drawing.Point screen, AC.TopLevel topLevel)
        {
            double scaling = DesktopScaling(topLevel);
            return new PixelPoint((int)Math.Round(screen.X * scaling), (int)Math.Round(screen.Y * scaling));
        }

        // Hover
        internal static void StartHover(Control control)
        {
            _hoverControl = control;
            if (_hoverTimer == null)
            {
                _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(SystemInformation.MouseHoverTime) };
                _hoverTimer.Tick += (s, e) =>
                {
                    _hoverTimer.Stop();
                    Control c = _hoverControl;
                    if (c != null && c.MouseIsInside) Dispatch(() => c.HandleMouseHover());
                };
            }
            _hoverTimer.Stop();
            _hoverTimer.Start();
        }
        internal static void StopHover(Control control)
        {
            if (_hoverControl == control)
            {
                _hoverControl = null;
                _hoverTimer?.Stop();
            }
        }

        // Focus
        public static Control FocusedControl { get { return _focusedControl; } }
        // WinForms raises Leave and LostFocus on the old control (and the containers it leaves), Enter and GotFocus on the new
        internal static void FocusChanged(Control newControl)
        {
            Control old = _focusedControl;
            if (old == newControl) return;
            _focusedControl = newControl;
            List<Control> oldChain = Chain(old);
            List<Control> newChain = Chain(newControl);
            Dispatch(() =>
            {
                if (old != null && !old.IsDisposed)
                {
                    old.RaiseLostFocus();
                    foreach (Control c in oldChain) if (!newChain.Contains(c) && !(c is Form)) c.RaiseLeave();
                }
                for (int i = newChain.Count - 1; i >= 0; i--)
                {
                    Control c = newChain[i];
                    if (!oldChain.Contains(c) && !(c is Form)) c.RaiseEnter();
                }
                newControl.RaiseGotFocus();
                Form form = newControl.FindForm();
                if (form != null) form.ActiveControlInternal = newControl;
            });
        }
        private static List<Control> Chain(Control control)
        {
            List<Control> chain = new List<Control>();
            for (Control c = control; c != null; c = c.Parent) chain.Add(c);
            return chain;
        }

        // Drag and drop
        internal static DragDropEffects ToEffects(Avalonia.Input.DragDropEffects effects)
        {
            DragDropEffects result = DragDropEffects.None;
            if ((effects & Avalonia.Input.DragDropEffects.Copy) != 0) result |= DragDropEffects.Copy;
            if ((effects & Avalonia.Input.DragDropEffects.Move) != 0) result |= DragDropEffects.Move;
            if ((effects & Avalonia.Input.DragDropEffects.Link) != 0) result |= DragDropEffects.Link;
            return result;
        }
        internal static Avalonia.Input.DragDropEffects FromEffects(DragDropEffects effects)
        {
            Avalonia.Input.DragDropEffects result = Avalonia.Input.DragDropEffects.None;
            if ((effects & DragDropEffects.Copy) != 0) result |= Avalonia.Input.DragDropEffects.Copy;
            if ((effects & DragDropEffects.Move) != 0) result |= Avalonia.Input.DragDropEffects.Move;
            if ((effects & DragDropEffects.Link) != 0) result |= Avalonia.Input.DragDropEffects.Link;
            return result;
        }
        internal static DragDropEffects DoDragDrop(Control control, object data, DragDropEffects allowedEffects)
        {
            // Drag and drop between the controls of PrePoMax is done by the controls themselves (tree, grids)
            return DragDropEffects.None;
        }
    }

    /// <summary>State of the keyboard and the mouse.</summary>
    public static class Input
    {
        private static KeyModifiers _modifiers;
        private static MouseButtons _buttons;
        private static System.Drawing.Point _mousePosition;

        public static Keys ModifierKeys { get { return ToKeys(_modifiers); } }
        public static MouseButtons MouseButtons { get { return _buttons; } }
        public static System.Drawing.Point MousePosition { get { return _mousePosition; } }

        public static Keys ToKeys(KeyModifiers modifiers)
        {
            Keys keys = Keys.None;
            if ((modifiers & KeyModifiers.Shift) != 0) keys |= Keys.Shift;
            // The command key of macOS is the control key of the PrePoMax shortcuts
            if ((modifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0) keys |= Keys.Control;
            if ((modifiers & KeyModifiers.Alt) != 0) keys |= Keys.Alt;
            return keys;
        }
        public static void Update(PointerEventArgs e)
        {
            _modifiers = e.KeyModifiers;
            if (e.Source is Visual visual)
            {
                var topLevel = AC.TopLevel.GetTopLevel(visual);
                if (topLevel != null)
                {
                    PixelPoint p = topLevel.PointToScreen(e.GetPosition(topLevel));
                    double scaling = Toolkit.DesktopScaling(topLevel);
                    _mousePosition = new System.Drawing.Point((int)Math.Round(p.X / scaling), (int)Math.Round(p.Y / scaling));
                }
                PointerPointProperties properties = e.GetCurrentPoint(visual).Properties;
                MouseButtons buttons = MouseButtons.None;
                if (properties.IsLeftButtonPressed) buttons |= MouseButtons.Left;
                if (properties.IsRightButtonPressed) buttons |= MouseButtons.Right;
                if (properties.IsMiddleButtonPressed) buttons |= MouseButtons.Middle;
                _buttons = buttons;
            }
        }
        public static void Update(KeyModifiers modifiers) { _modifiers = modifiers; }
        public static MouseButtons ButtonOf(PointerUpdateKind kind)
        {
            switch (kind)
            {
                case PointerUpdateKind.LeftButtonPressed:
                case PointerUpdateKind.LeftButtonReleased: return MouseButtons.Left;
                case PointerUpdateKind.RightButtonPressed:
                case PointerUpdateKind.RightButtonReleased: return MouseButtons.Right;
                case PointerUpdateKind.MiddleButtonPressed:
                case PointerUpdateKind.MiddleButtonReleased: return MouseButtons.Middle;
                case PointerUpdateKind.XButton1Pressed:
                case PointerUpdateKind.XButton1Released: return MouseButtons.XButton1;
                case PointerUpdateKind.XButton2Pressed:
                case PointerUpdateKind.XButton2Released: return MouseButtons.XButton2;
                default: return MouseButtons.None;
            }
        }
        public static MouseButtons ButtonOf(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left: return MouseButtons.Left;
                case MouseButton.Right: return MouseButtons.Right;
                case MouseButton.Middle: return MouseButtons.Middle;
                case MouseButton.XButton1: return MouseButtons.XButton1;
                case MouseButton.XButton2: return MouseButtons.XButton2;
                default: return MouseButtons.None;
            }
        }
        // One notch of a mouse wheel is 120 in WinForms; the precise deltas of a trackpad are summed to whole steps
        private static double _wheelRemainder;
        public static int WheelDelta(double delta)
        {
            double value = delta * 120 + _wheelRemainder;
            int steps = (int)(value / 40) * 40;
            _wheelRemainder = value - steps;
            if (Math.Abs(_wheelRemainder) > 240) _wheelRemainder = 0;
            return steps;
        }
        public static Keys ToKeys(Key key)
        {
            switch (key)
            {
                case Key.Back: return Keys.Back;
                case Key.Tab: return Keys.Tab;
                case Key.Enter: return Keys.Enter;
                case Key.Pause: return Keys.Pause;
                case Key.CapsLock: return Keys.CapsLock;
                case Key.Escape: return Keys.Escape;
                case Key.Space: return Keys.Space;
                case Key.PageUp: return Keys.PageUp;
                case Key.PageDown: return Keys.PageDown;
                case Key.End: return Keys.End;
                case Key.Home: return Keys.Home;
                case Key.Left: return Keys.Left;
                case Key.Up: return Keys.Up;
                case Key.Right: return Keys.Right;
                case Key.Down: return Keys.Down;
                case Key.Insert: return Keys.Insert;
                case Key.Delete: return Keys.Delete;
                case Key.LWin: return Keys.LWin;
                case Key.RWin: return Keys.RWin;
                case Key.Apps: return Keys.Apps;
                case Key.Multiply: return Keys.Multiply;
                case Key.Add: return Keys.Add;
                case Key.Separator: return Keys.Separator;
                case Key.Subtract: return Keys.Subtract;
                case Key.Decimal: return Keys.Decimal;
                case Key.Divide: return Keys.Divide;
                case Key.NumLock: return Keys.NumLock;
                case Key.Scroll: return Keys.Scroll;
                case Key.LeftShift: return Keys.LShiftKey;
                case Key.RightShift: return Keys.RShiftKey;
                case Key.LeftCtrl: return Keys.LControlKey;
                case Key.RightCtrl: return Keys.RControlKey;
                case Key.LeftAlt: return Keys.LMenu;
                case Key.RightAlt: return Keys.RMenu;
                case Key.OemSemicolon: return Keys.OemSemicolon;
                case Key.OemPlus: return Keys.Oemplus;
                case Key.OemComma: return Keys.Oemcomma;
                case Key.OemMinus: return Keys.OemMinus;
                case Key.OemPeriod: return Keys.OemPeriod;
                case Key.OemQuestion: return Keys.OemQuestion;
                case Key.OemTilde: return Keys.Oemtilde;
                case Key.OemOpenBrackets: return Keys.OemOpenBrackets;
                case Key.OemPipe: return Keys.OemPipe;
                case Key.OemCloseBrackets: return Keys.OemCloseBrackets;
                case Key.OemQuotes: return Keys.OemQuotes;
                case Key.OemBackslash: return Keys.OemBackslash;
            }
            if (key >= Key.A && key <= Key.Z) return Keys.A + (key - Key.A);
            if (key >= Key.D0 && key <= Key.D9) return Keys.D0 + (key - Key.D0);
            if (key >= Key.NumPad0 && key <= Key.NumPad9) return Keys.NumPad0 + (key - Key.NumPad0);
            if (key >= Key.F1 && key <= Key.F24) return Keys.F1 + (key - Key.F1);
            return Keys.None;
        }
        public static Keys ToKeyData(Avalonia.Input.KeyEventArgs e)
        {
            return ToKeys(e.Key) | ToKeys(e.KeyModifiers);
        }
    }

    /// <summary>Data of a drag and drop operation from Avalonia.</summary>
    internal class AvaloniaDataObject : IDataObject
    {
        private readonly Avalonia.Input.IDataObject _data;
        public AvaloniaDataObject(Avalonia.Input.IDataObject data) { _data = data; }
        public object GetData(string format)
        {
            if (format == DataFormats.FileDrop)
            {
                var files = _data.GetFiles();
                return files?.Select(f => f.TryGetLocalPath()).Where(p => p != null).ToArray();
            }
            if (format == DataFormats.Text || format == DataFormats.UnicodeText || format == DataFormats.StringFormat)
                return _data.GetText();
            return _data.Get(format);
        }
        public object GetData(string format, bool autoConvert) { return GetData(format); }
        public object GetData(Type format) { return GetData(format.FullName); }
        public bool GetDataPresent(string format)
        {
            if (format == DataFormats.FileDrop) return _data.Contains(Avalonia.Input.DataFormats.Files);
            if (format == DataFormats.Text || format == DataFormats.UnicodeText || format == DataFormats.StringFormat)
                return _data.Contains(Avalonia.Input.DataFormats.Text);
            return _data.Contains(format);
        }
        public bool GetDataPresent(string format, bool autoConvert) { return GetDataPresent(format); }
        public bool GetDataPresent(Type format) { return GetDataPresent(format.FullName); }
        public string[] GetFormats()
        {
            List<string> formats = new List<string>(_data.GetDataFormats());
            if (formats.Contains(Avalonia.Input.DataFormats.Files)) formats.Add(DataFormats.FileDrop);
            return formats.ToArray();
        }
        public string[] GetFormats(bool autoConvert) { return GetFormats(); }
        public void SetData(object data) { }
        public void SetData(string format, object data) { }
        public void SetData(string format, bool autoConvert, object data) { }
        public void SetData(Type format, object data) { }
    }
}
