// PrePoMax for macOS - buttons, check boxes, labels, link labels, group boxes, panels and simple controls.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SkiaSharp;
using AC = Avalonia.Controls;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;
using Orientation = System.Windows.Forms.Orientation;

namespace System.Windows.Forms
{
    internal static class ContentLayout
    {
        public static Avalonia.Layout.HorizontalAlignment H(ContentAlignment a)
        {
            if ((a & (ContentAlignment.TopLeft | ContentAlignment.MiddleLeft | ContentAlignment.BottomLeft)) != 0) return Avalonia.Layout.HorizontalAlignment.Left;
            if ((a & (ContentAlignment.TopRight | ContentAlignment.MiddleRight | ContentAlignment.BottomRight)) != 0) return Avalonia.Layout.HorizontalAlignment.Right;
            return Avalonia.Layout.HorizontalAlignment.Center;
        }
        public static VerticalAlignment V(ContentAlignment a)
        {
            if ((a & (ContentAlignment.TopLeft | ContentAlignment.TopCenter | ContentAlignment.TopRight)) != 0) return VerticalAlignment.Top;
            if ((a & (ContentAlignment.BottomLeft | ContentAlignment.BottomCenter | ContentAlignment.BottomRight)) != 0) return VerticalAlignment.Bottom;
            return VerticalAlignment.Center;
        }
        public static TextAlignment T(ContentAlignment a)
        {
            Avalonia.Layout.HorizontalAlignment h = H(a);
            return h == Avalonia.Layout.HorizontalAlignment.Left ? TextAlignment.Left : h == Avalonia.Layout.HorizontalAlignment.Right ? TextAlignment.Right : TextAlignment.Center;
        }
        // WinForms shows "&&" as "&" and hides a single "&" of a mnemonic
        public static string StripMnemonic(string text, bool useMnemonic)
        {
            if (!useMnemonic || string.IsNullOrEmpty(text) || text.IndexOf('&') < 0) return text ?? string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '&')
                {
                    if (i + 1 < text.Length && text[i + 1] == '&') { sb.Append('&'); i++; }
                    continue;
                }
                sb.Append(text[i]);
            }
            return sb.ToString();
        }
        // Text and image of a button or label arranged like WinForms
        public static AC.Control Build(string text, Image image, ContentAlignment textAlign, ContentAlignment imageAlign,
                                       TextImageRelation relation, bool wrap, out AC.TextBlock textBlock)
        {
            textBlock = new AC.TextBlock
            {
                Text = text,
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = TextTrimming.None,
                HorizontalAlignment = H(textAlign),
                VerticalAlignment = V(textAlign),
                TextAlignment = T(textAlign),
            };
            if (image == null) return textBlock;
            AC.Image img = Toolkit.CreateImageElement(image, image.Width, image.Height);
            img.HorizontalAlignment = H(imageAlign);
            img.VerticalAlignment = V(imageAlign);
            if (relation == TextImageRelation.Overlay || string.IsNullOrEmpty(text))
            {
                AC.Grid grid = new AC.Grid();
                grid.Children.Add(img);
                if (!string.IsNullOrEmpty(text)) grid.Children.Add(textBlock);
                return grid;
            }
            AC.StackPanel panel = new AC.StackPanel
            {
                Spacing = 3,
                HorizontalAlignment = H(textAlign),
                VerticalAlignment = V(textAlign),
                Orientation = relation == TextImageRelation.ImageAboveText || relation == TextImageRelation.TextAboveImage ?
                              Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal,
            };
            textBlock.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            img.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            img.VerticalAlignment = VerticalAlignment.Center;
            if (relation == TextImageRelation.ImageBeforeText || relation == TextImageRelation.ImageAboveText)
            {
                panel.Children.Add(img);
                panel.Children.Add(textBlock);
            }
            else
            {
                panel.Children.Add(textBlock);
                panel.Children.Add(img);
            }
            return panel;
        }
        public static Size MeasureText(string text, Font font, int maxWidth = 0)
        {
            // Controls can only be made on the UI thread; WinForms measures on any thread
            if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                Avalonia.Media.TextFormatting.TextLayout layout = new Avalonia.Media.TextFormatting.TextLayout(
                    string.IsNullOrEmpty(text) ? " " : text,
                    new Typeface(Toolkit.FontFamilyOf(font), Avalonia.Media.FontStyle.Normal, font.Bold ? FontWeight.Bold : FontWeight.Normal),
                    Toolkit.FontSizeOf(font), null, TextAlignment.Left,
                    maxWidth > 0 ? TextWrapping.Wrap : TextWrapping.NoWrap, maxWidth: maxWidth > 0 ? maxWidth : double.PositiveInfinity);
                return new Size((int)Math.Ceiling(layout.WidthIncludingTrailingWhitespace), (int)Math.Ceiling(layout.Height));
            }
            AC.TextBlock block = new AC.TextBlock
            {
                Text = string.IsNullOrEmpty(text) ? " " : text,
                FontSize = Toolkit.FontSizeOf(font),
                FontFamily = Toolkit.FontFamilyOf(font),
                FontWeight = font.Bold ? FontWeight.Bold : FontWeight.Normal,
                TextWrapping = maxWidth > 0 ? TextWrapping.Wrap : TextWrapping.NoWrap,
            };
            block.Measure(new Avalonia.Size(maxWidth > 0 ? maxWidth : double.PositiveInfinity, double.PositiveInfinity));
            Avalonia.Size s = block.DesiredSize;
            return new Size((int)Math.Ceiling(s.Width), (int)Math.Ceiling(s.Height));
        }
    }

    public abstract class ButtonBase : Control
    {
        private Image _image;
        private ContentAlignment _imageAlign = ContentAlignment.MiddleCenter;
        private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;
        private TextImageRelation _textImageRelation = TextImageRelation.Overlay;
        private FlatStyle _flatStyle = FlatStyle.Standard;
        private int _imageIndex = -1;
        private string _imageKey = string.Empty;
        private ImageList _imageList;
        protected AC.TextBlock TextElement;

        protected ButtonBase()
        {
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
        }

        public Image Image
        {
            get
            {
                if (_image != null) return _image;
                if (_imageList != null)
                {
                    if (_imageIndex >= 0 && _imageIndex < _imageList.Images.Count) return _imageList.Images[_imageIndex];
                    if (!string.IsNullOrEmpty(_imageKey)) return _imageList.Images[_imageKey];
                }
                return null;
            }
            set { _image = value; UpdateContent(); }
        }
        public ContentAlignment ImageAlign { get { return _imageAlign; } set { _imageAlign = value; UpdateContent(); } }
        public int ImageIndex { get { return _imageIndex; } set { _imageIndex = value; UpdateContent(); } }
        public string ImageKey { get { return _imageKey; } set { _imageKey = value; UpdateContent(); } }
        public ImageList ImageList { get { return _imageList; } set { _imageList = value; UpdateContent(); } }
        public virtual ContentAlignment TextAlign { get { return _textAlign; } set { _textAlign = value; UpdateContent(); } }
        public TextImageRelation TextImageRelation { get { return _textImageRelation; } set { _textImageRelation = value; UpdateContent(); } }
        public FlatStyle FlatStyle { get { return _flatStyle; } set { _flatStyle = value; UpdateContent(); } }
        public FlatButtonAppearance FlatAppearance { get; } = new FlatButtonAppearance();
        public bool UseMnemonic { get; set; } = true;
        public bool UseVisualStyleBackColor { get; set; } = true;
        public bool UseCompatibleTextRendering { get; set; }
        public bool AutoEllipsis { get; set; }
        public new ImeMode ImeMode { get; set; }
        protected internal bool IsDefault { get; set; }
        public override string Text { get { return base.Text; } set { base.Text = value; UpdateContent(); } }
        protected override Padding DefaultPadding { get { return new Padding(0); } }
        internal virtual void UpdateContent() { }
        internal string DisplayText { get { return ContentLayout.StripMnemonic(Text, UseMnemonic); } }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = ContentLayout.MeasureText(DisplayText, Font);
            Image image = Image;
            int w = text.Width + 16, h = Math.Max(text.Height + 10, 23);
            if (image != null)
            {
                if (_textImageRelation == TextImageRelation.ImageBeforeText || _textImageRelation == TextImageRelation.TextBeforeImage) w += image.Width + 3;
                else h = Math.Max(h, image.Height + 8);
            }
            return new Size(Math.Max(w, 75), h);
        }
    }

    public class FlatButtonAppearance
    {
        public Color BorderColor { get; set; } = Color.Empty;
        public int BorderSize { get; set; } = 1;
        public Color CheckedBackColor { get; set; } = Color.Empty;
        public Color MouseDownBackColor { get; set; } = Color.Empty;
        public Color MouseOverBackColor { get; set; } = Color.Empty;
    }

    public class Button : ButtonBase, IButtonControl
    {
        private AC.Button _button;
        private DialogResult _dialogResult;

        public Button()
        {
            SetStyle(ControlStyles.Selectable, true);
        }
        protected override Size DefaultSize { get { return new Size(75, 23); } }
        public virtual DialogResult DialogResult { get { return _dialogResult; } set { _dialogResult = value; } }
        public AutoSizeMode AutoSizeMode { get; set; }
        protected internal override bool ContentHandlesClick { get { return true; } }
        protected internal override AC.Control CreateContent()
        {
            _button = new AC.Button { Focusable = true };
            _button.Classes.Add("winforms");
            _button.Click += (s, e) => Toolkit.Dispatch(() => OnClick(EventArgs.Empty));
            return _button;
        }
        protected internal override void OnContentCreated() { UpdateContent(); }
        internal override void UpdateContent()
        {
            if (_button == null) return;
            _button.Content = ContentLayout.Build(DisplayText, Image, TextAlign, ImageAlign, TextImageRelation, false, out TextElement);
            _button.Padding = new Thickness(Padding.Left + 1, Padding.Top + 1, Padding.Right + 1, Padding.Bottom + 1);
            if (FlatStyle == FlatStyle.Flat)
            {
                _button.BorderThickness = new Thickness(FlatAppearance.BorderSize);
                if (!FlatAppearance.BorderColor.IsEmpty) _button.BorderBrush = Toolkit.ToBrush(FlatAppearance.BorderColor);
            }
            if (BackColorSet && !UseVisualStyleBackColor || (BackColorSet && FlatStyle == FlatStyle.Flat)) _button.Background = Toolkit.ToBrush(BackColor);
            else if (BackColorSet && BackColor != SystemColors.Control) _button.Background = Toolkit.ToBrush(BackColor);
            else _button.ClearValue(AC.Button.BackgroundProperty);
            if (ForeColorSet) _button.Foreground = Toolkit.ToBrush(ForeColor);
        }
        protected override void OnBackColorChanged(EventArgs e) { base.OnBackColorChanged(e); UpdateContent(); }
        protected override void OnForeColorChanged(EventArgs e) { base.OnForeColorChanged(e); UpdateContent(); }
        protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); UpdateContent(); }
        protected override void OnClick(EventArgs e)
        {
            Form form = FindForm();
            if (form != null && _dialogResult != DialogResult.None) form.DialogResult = _dialogResult;
            base.OnClick(e);
        }
        public void NotifyDefault(bool value)
        {
            IsDefault = value;
            if (_button != null)
            {
                if (value) _button.Classes.Add("default");
                else _button.Classes.Remove("default");
            }
        }
        public void PerformClick()
        {
            if (CanSelect || Enabled) OnClick(EventArgs.Empty);
        }
    }

    public class CheckBox : ButtonBase
    {
        private AC.CheckBox _checkBox;
        private CheckState _checkState;
        private bool _updating;
        private ContentAlignment _checkAlign = ContentAlignment.MiddleLeft;

        public CheckBox()
        {
            SetStyle(ControlStyles.Selectable, true);
            TextAlign = ContentAlignment.MiddleLeft;
            AutoSize = false;
        }
        protected override Size DefaultSize { get { return new Size(104, 24); } }
        public Appearance Appearance { get; set; }
        public bool AutoCheck { get; set; } = true;
        public bool ThreeState { get; set; }
        public ContentAlignment CheckAlign { get { return _checkAlign; } set { _checkAlign = value; } }
        public bool Checked
        {
            get { return _checkState != CheckState.Unchecked; }
            set { CheckState = value ? CheckState.Checked : CheckState.Unchecked; }
        }
        public CheckState CheckState
        {
            get { return _checkState; }
            set
            {
                if (_checkState == value) return;
                bool oldChecked = Checked;
                _checkState = value;
                UpdateNative();
                if (oldChecked != Checked) OnCheckedChanged(EventArgs.Empty);
                OnCheckStateChanged(EventArgs.Empty);
            }
        }
        public event EventHandler CheckedChanged;
        public event EventHandler CheckStateChanged;
        public event EventHandler AppearanceChanged;
        protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }
        protected virtual void OnCheckStateChanged(EventArgs e) { CheckStateChanged?.Invoke(this, e); }
        protected internal override bool ContentHandlesClick { get { return true; } }
        protected internal override AC.Control CreateContent()
        {
            _checkBox = new AC.CheckBox { Focusable = true, VerticalAlignment = VerticalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Center, MinHeight = 0, Padding = new Thickness(3, 0, 0, 0) };
            _checkBox.Classes.Add("winforms");
            _checkBox.IsCheckedChanged += (s, e) =>
            {
                if (_updating) return;
                Toolkit.Dispatch(() =>
                {
                    if (!AutoCheck)
                    {
                        UpdateNative();
                        OnClick(EventArgs.Empty);
                        return;
                    }
                    CheckState = _checkBox.IsChecked == null ? CheckState.Indeterminate : _checkBox.IsChecked.Value ? CheckState.Checked : CheckState.Unchecked;
                    OnClick(EventArgs.Empty);
                });
            };
            return _checkBox;
        }
        protected internal override void OnContentCreated()
        {
            UpdateContent();
            UpdateNative();
        }
        private void UpdateNative()
        {
            if (_checkBox == null) return;
            _updating = true;
            _checkBox.IsThreeState = ThreeState;
            _checkBox.IsChecked = _checkState == CheckState.Indeterminate ? (bool?)null : _checkState == CheckState.Checked;
            _updating = false;
        }
        internal override void UpdateContent()
        {
            if (_checkBox == null) return;
            _checkBox.Content = ContentLayout.Build(DisplayText, Image, TextAlign, ImageAlign, TextImageRelation, false, out TextElement);
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = ContentLayout.MeasureText(DisplayText, Font);
            return new Size(text.Width + 20, Math.Max(17, text.Height + 2));
        }
        public override bool AutoSize { get { return base.AutoSize; } set { base.AutoSize = value; } }
    }

    public class RadioButton : ButtonBase
    {
        private AC.RadioButton _radio;
        private bool _checked;
        private bool _updating;

        public RadioButton()
        {
            SetStyle(ControlStyles.Selectable, true);
            TextAlign = ContentAlignment.MiddleLeft;
        }
        protected override Size DefaultSize { get { return new Size(104, 24); } }
        public bool AutoCheck { get; set; } = true;
        public Appearance Appearance { get; set; }
        public ContentAlignment CheckAlign { get; set; } = ContentAlignment.MiddleLeft;
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked == value) return;
                _checked = value;
                if (value && AutoCheck && Parent != null)
                {
                    foreach (Control c in Parent.Controls)
                        if (c is RadioButton r && r != this && r.AutoCheck && r._checked) r.Checked = false;
                }
                UpdateNative();
                OnCheckedChanged(EventArgs.Empty);
            }
        }
        public event EventHandler CheckedChanged;
        protected virtual void OnCheckedChanged(EventArgs e) { CheckedChanged?.Invoke(this, e); }
        protected internal override bool ContentHandlesClick { get { return true; } }
        protected internal override AC.Control CreateContent()
        {
            _radio = new AC.RadioButton { Focusable = true, VerticalContentAlignment = VerticalAlignment.Center, MinHeight = 0, GroupName = "rb" + Handle.ToInt64(), Padding = new Thickness(3, 0, 0, 0) };
            _radio.Classes.Add("winforms");
            _radio.IsCheckedChanged += (s, e) =>
            {
                if (_updating) return;
                bool value = _radio.IsChecked == true;
                Toolkit.Dispatch(() =>
                {
                    if (value)
                    {
                        if (AutoCheck) Checked = true;
                        else UpdateNative();
                        OnClick(EventArgs.Empty);
                    }
                    else UpdateNative();
                });
            };
            return _radio;
        }
        protected internal override void OnContentCreated()
        {
            UpdateContent();
            UpdateNative();
        }
        private void UpdateNative()
        {
            if (_radio == null) return;
            _updating = true;
            _radio.IsChecked = _checked;
            _updating = false;
        }
        internal override void UpdateContent()
        {
            if (_radio == null) return;
            _radio.Content = ContentLayout.Build(DisplayText, Image, TextAlign, ImageAlign, TextImageRelation, false, out TextElement);
        }
        public void PerformClick() { if (AutoCheck) Checked = true; OnClick(EventArgs.Empty); }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = ContentLayout.MeasureText(DisplayText, Font);
            return new Size(text.Width + 20, Math.Max(17, text.Height + 2));
        }
    }

    public class Label : Control
    {
        private AC.TextBlock _text;
        private AC.Border _border;
        private ContentAlignment _textAlign = ContentAlignment.TopLeft;
        private BorderStyle _borderStyle;
        private Image _image;
        private ContentAlignment _imageAlign = ContentAlignment.MiddleCenter;
        private bool _useMnemonic = true;
        private bool _autoEllipsis;

        public Label()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
        protected override Size DefaultSize { get { return new Size(100, 23); } }
        protected override Padding DefaultMargin { get { return new Padding(3, 0, 3, 0); } }
        public virtual ContentAlignment TextAlign { get { return _textAlign; } set { _textAlign = value; UpdateText(); } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                _borderStyle = value;
                SetClientSizeOffset(BorderSizes.Of(value));
                Host.InvalidateVisual();
                Host.InvalidateArrange();
            }
        }
        public FlatStyle FlatStyle { get; set; }
        public Image Image { get { return _image; } set { _image = value; UpdateText(); } }
        public ContentAlignment ImageAlign { get { return _imageAlign; } set { _imageAlign = value; UpdateText(); } }
        public int ImageIndex { get; set; } = -1;
        public string ImageKey { get; set; }
        public ImageList ImageList { get; set; }
        public bool UseMnemonic { get { return _useMnemonic; } set { _useMnemonic = value; UpdateText(); } }
        public bool AutoEllipsis { get { return _autoEllipsis; } set { _autoEllipsis = value; UpdateText(); } }
        public bool UseCompatibleTextRendering { get; set; }
        public int PreferredWidth { get { return GetPreferredSize(Size.Empty).Width; } }
        public int PreferredHeight { get { return GetPreferredSize(Size.Empty).Height; } }
        public override string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                UpdateText();
                if (AutoSize) AdjustAutoSize();
            }
        }
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height)
        {
            BorderSizes.Paint(canvas, _borderStyle, width, height);
        }
        protected internal override AC.Control CreateContent()
        {
            _text = new AC.TextBlock();
            _border = new AC.Border { Child = _text, ClipToBounds = true };
            return _border;
        }
        protected internal override void OnContentCreated() { UpdateText(); }
        internal virtual void UpdateText()
        {
            if (_text == null) return;
            _text.Text = ContentLayout.StripMnemonic(Text, _useMnemonic);
            // An auto sized label wraps only within its maximum width
            _text.TextWrapping = AutoSize && MaximumSize.Width <= 0 ? TextWrapping.NoWrap : TextWrapping.Wrap;
            _text.TextTrimming = _autoEllipsis ? TextTrimming.CharacterEllipsis : TextTrimming.None;
            _text.HorizontalAlignment = ContentLayout.H(_textAlign);
            _text.VerticalAlignment = ContentLayout.V(_textAlign);
            _text.TextAlignment = ContentLayout.T(_textAlign);
            _border.Padding = new Thickness(Padding.Left, Padding.Top, Padding.Right, Padding.Bottom);
            if (_border.Child is AC.Grid oldGrid) oldGrid.Children.Remove(_text);
            if (_image != null)
            {
                AC.Grid grid = new AC.Grid();
                AC.Image img = Toolkit.CreateImageElement(_image, _image.Width, _image.Height);
                img.HorizontalAlignment = ContentLayout.H(_imageAlign);
                img.VerticalAlignment = ContentLayout.V(_imageAlign);
                grid.Children.Add(img);
                _border.Child = null;
                grid.Children.Add(_text);
                _border.Child = grid;
            }
            else if (_border.Child != _text)
            {
                _border.Child = _text;
            }
        }
        protected override void OnPaddingChanged(EventArgs e) { base.OnPaddingChanged(e); UpdateText(); }
        // A maximum width makes an auto sized label wrap its text
        public override Size MaximumSize
        {
            get { return base.MaximumSize; }
            set
            {
                base.MaximumSize = value;
                UpdateText();
                if (AutoSize) AdjustAutoSize();
            }
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            int maxWidth = MaximumSize.Width > 0 ? MaximumSize.Width - Padding.Horizontal : 0;
            Size text = ContentLayout.MeasureText(ContentLayout.StripMnemonic(Text, _useMnemonic), Font, maxWidth);
            Size border = BorderSizes.Of(_borderStyle);
            return new Size(text.Width + Padding.Horizontal + border.Width + 3, text.Height + Padding.Vertical + border.Height + 2);
        }
        internal override Size GetDockedSize(Size proposed)
        {
            return AutoSize ? new Size(Dock == DockStyle.Top || Dock == DockStyle.Bottom ? proposed.Width : PreferredWidth,
                                       Dock == DockStyle.Left || Dock == DockStyle.Right ? proposed.Height : PreferredHeight) : proposed;
        }
        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (AutoSize) AdjustAutoSize();
        }
        protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); }
    }

    public class LinkLabel : Label, IButtonControl
    {
        public DialogResult DialogResult { get; set; }
        public void NotifyDefault(bool value) { }
        public void PerformClick() { OnClick(EventArgs.Empty); }
        private readonly LinkCollection _links;
        private AC.TextBlock _textBlock;
        private LinkArea _linkArea;
        private bool _hover;
        private Color _linkColor = Color.FromArgb(0, 0, 255);
        private LinkBehavior _linkBehavior = LinkBehavior.SystemDefault;

        public LinkLabel()
        {
            _links = new LinkCollection(this);
            _links.Add(new Link(0, 0));
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            Cursor = Cursors.Hand;
        }
        public LinkCollection Links { get { return _links; } }
        public LinkArea LinkArea
        {
            get { return _linkArea; }
            set
            {
                _linkArea = value;
                _links.Clear();
                _links.Add(new Link(value.Start, value.Length));
                UpdateText();
            }
        }
        public Color LinkColor { get { return _linkColor; } set { _linkColor = value; UpdateText(); } }
        public Color ActiveLinkColor { get; set; } = Color.Red;
        public Color VisitedLinkColor { get; set; } = Color.FromArgb(128, 0, 128);
        public Color DisabledLinkColor { get; set; } = Color.FromArgb(133, 133, 133);
        public bool LinkVisited { get; set; }
        public LinkBehavior LinkBehavior { get { return _linkBehavior; } set { _linkBehavior = value; UpdateText(); } }
        public new Cursor OverrideCursor { get; set; }
        public event LinkLabelLinkClickedEventHandler LinkClicked;
        protected virtual void OnLinkClicked(LinkLabelLinkClickedEventArgs e) { LinkClicked?.Invoke(this, e); }
        protected internal override AC.Control CreateContent()
        {
            AC.Control content = base.CreateContent();
            _textBlock = (AC.TextBlock)((AC.Border)content).Child;
            return content;
        }
        internal override void UpdateText()
        {
            base.UpdateText();
            if (_textBlock == null) return;
            string text = _textBlock.Text ?? string.Empty;
            Link link = _links.Count > 0 ? _links[0] : null;
            int start = link == null ? 0 : Math.Min(link.Start, text.Length);
            int length = link == null || link.Length == 0 ? text.Length - start : Math.Min(link.Length, text.Length - start);
            if (length < 0) length = 0;
            // A text block draws its text and its inlines one after the other, so the text goes into the inlines alone
            _textBlock.Text = null;
            _textBlock.Inlines = new InlineCollection();
            if (start > 0) _textBlock.Inlines.Add(new Run(text.Substring(0, start)));
            Run linkRun = new Run(text.Substring(start, length))
            {
                Foreground = Toolkit.ToBrush(Enabled ? (LinkVisited ? VisitedLinkColor : _linkColor) : DisabledLinkColor),
            };
            bool underline = _linkBehavior == LinkBehavior.AlwaysUnderline || _linkBehavior == LinkBehavior.SystemDefault ||
                             (_linkBehavior == LinkBehavior.HoverUnderline && _hover);
            if (underline) linkRun.TextDecorations = TextDecorations.Underline;
            _textBlock.Inlines.Add(linkRun);
            if (start + length < text.Length) _textBlock.Inlines.Add(new Run(text.Substring(start + length)));
        }
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            if (_linkBehavior == LinkBehavior.HoverUnderline) UpdateText();
            base.OnMouseEnter(e);
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            if (_linkBehavior == LinkBehavior.HoverUnderline) UpdateText();
            base.OnMouseLeave(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && Enabled && ClientRectangle.Contains(e.Location) && _links.Count > 0)
                OnLinkClicked(new LinkLabelLinkClickedEventArgs(_links[0], e.Button));
        }
        public class Link
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public object LinkData { get; set; }
            public string Name { get; set; }
            public object Tag { get; set; }
            public bool Enabled { get; set; } = true;
            public bool Visited { get; set; }
            public string Description { get; set; }
            public Link() { }
            public Link(int start, int length) { Start = start; Length = length; }
            public Link(int start, int length, object linkData) : this(start, length) { LinkData = linkData; }
        }
        public class LinkCollection : System.Collections.ObjectModel.Collection<Link>
        {
            private readonly LinkLabel _owner;
            public LinkCollection(LinkLabel owner) { _owner = owner; }
            public Link Add(int start, int length) { Link l = new Link(start, length); Add(l); return l; }
            public Link Add(int start, int length, object linkData) { Link l = new Link(start, length, linkData); Add(l); return l; }
            public bool LinksAdded { get { return Count > 0; } }
        }
    }

    public class LinkLabelLinkClickedEventArgs : EventArgs
    {
        public LinkLabel.Link Link { get; }
        public MouseButtons Button { get; }
        public LinkLabelLinkClickedEventArgs(LinkLabel.Link link) : this(link, MouseButtons.Left) { }
        public LinkLabelLinkClickedEventArgs(LinkLabel.Link link, MouseButtons button)
        {
            Link = link;
            Button = button;
        }
    }

    public delegate void LinkLabelLinkClickedEventHandler(object sender, LinkLabelLinkClickedEventArgs e);

    public class GroupBox : Control
    {
        public GroupBox()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
        protected override Size DefaultSize { get { return new Size(200, 100); } }
        protected override Padding DefaultPadding { get { return new Padding(3); } }
        public FlatStyle FlatStyle { get; set; }
        public bool UseCompatibleTextRendering { get; set; }
        public AutoSizeMode AutoSizeMode { get; set; }
        public override string Text { get { return base.Text; } set { base.Text = value; Host.InvalidateVisual(); } }
        public override Rectangle DisplayRectangle
        {
            get
            {
                Size size = ClientSize;
                int fontHeight = Font.Height;
                Padding p = Padding;
                return new Rectangle(p.Left, fontHeight + p.Top, Math.Max(0, size.Width - p.Horizontal),
                                     Math.Max(0, size.Height - fontHeight - p.Vertical));
            }
        }
        internal override bool HasNonClientPaint { get { return true; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height)
        {
            string text = ContentLayout.StripMnemonic(Text, true);
            Font font = Font;
            using (SKFont skFont = font.ToSKFont())
            using (SKPaint textPaint = new SKPaint { Color = Toolkit.ToSK(Enabled ? ForeColor : SystemColors.GrayText), IsAntialias = true, Typeface = skFont.Typeface, TextSize = skFont.Size })
            using (SKPaint line = new SKPaint { Color = new SKColor(0xDC, 0xDC, 0xDC), Style = SKPaintStyle.Stroke, StrokeWidth = 1 })
            {
                SKFontMetrics m = skFont.Metrics;
                float textHeight = -m.Ascent + m.Descent;
                float y = (float)Math.Floor(textHeight / 2) + 0.5f;
                float textWidth = string.IsNullOrEmpty(text) ? 0 : textPaint.MeasureText(text);
                float gapStart = 6, gapEnd = string.IsNullOrEmpty(text) ? 6 : 8 + textWidth + 2;
                using (SKPath path = new SKPath())
                {
                    path.MoveTo(gapStart, y);
                    path.LineTo(0.5f, y);
                    path.LineTo(0.5f, height - 0.5f);
                    path.LineTo(width - 0.5f, height - 0.5f);
                    path.LineTo(width - 0.5f, y);
                    path.LineTo(gapEnd, y);
                    canvas.DrawPath(path, line);
                }
                if (!string.IsNullOrEmpty(text)) canvas.DrawText(text, 8, -m.Ascent, textPaint);
            }
        }
    }

    public class Panel : ScrollableControl
    {
        private BorderStyle _borderStyle;
        public Panel()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
        protected override Size DefaultSize { get { return new Size(200, 100); } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set
            {
                _borderStyle = value;
                SetClientSizeOffset(BorderSizes.Of(value));
                PerformLayout();
                Host.InvalidateVisual();
                Host.InvalidateArrange();
            }
        }
        public AutoSizeMode AutoSizeMode { get; set; }
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool HasNonClientPaint { get { return _borderStyle != BorderStyle.None; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height)
        {
            BorderSizes.Paint(canvas, _borderStyle, width, height);
        }
    }

    public class SplitterPanel : Panel
    {
        private readonly SplitContainer _owner;
        public SplitterPanel(SplitContainer owner) { _owner = owner; }
        public new int Width { get { return base.Width; } set { } }
        public new int Height { get { return base.Height; } set { } }
        public new DockStyle Dock { get { return DockStyle.None; } set { } }
    }

    public class PictureBox : Control
    {
        private Image _image;
        private PictureBoxSizeMode _sizeMode;
        private BorderStyle _borderStyle;
        public PictureBox()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
        protected override Size DefaultSize { get { return new Size(100, 50); } }
        public Image Image { get { return _image; } set { _image = value; Invalidate(); } }
        public Image InitialImage { get; set; }
        public Image ErrorImage { get; set; }
        public PictureBoxSizeMode SizeMode { get { return _sizeMode; } set { _sizeMode = value; Invalidate(); } }
        public BorderStyle BorderStyle
        {
            get { return _borderStyle; }
            set { _borderStyle = value; SetClientSizeOffset(BorderSizes.Of(value)); Invalidate(); }
        }
        public void Load(string url) { Image = Image.FromFile(url); }
        protected internal override Padding NonClientInsets { get { return BorderSizes.Insets(_borderStyle); } }
        internal override bool PaintsItself { get { return true; } }
        internal override void PaintNonClient(SKCanvas canvas, float width, float height) { BorderSizes.Paint(canvas, _borderStyle, width, height); }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_image != null)
            {
                Rectangle r = ClientRectangle;
                switch (_sizeMode)
                {
                    case PictureBoxSizeMode.StretchImage: e.Graphics.DrawImage(_image, r); break;
                    case PictureBoxSizeMode.CenterImage: e.Graphics.DrawImage(_image, (r.Width - _image.Width) / 2, (r.Height - _image.Height) / 2); break;
                    case PictureBoxSizeMode.Zoom: Toolkit.DrawBackgroundImagePublic(e.Graphics, _image, r, ImageLayout.Zoom); break;
                    default: e.Graphics.DrawImage(_image, 0, 0); break;
                }
            }
            base.OnPaint(e);
        }
    }

    public class ProgressBar : Control
    {
        private AC.ProgressBar _bar;
        private int _minimum, _maximum = 100, _value, _step = 10;
        private ProgressBarStyle _style = ProgressBarStyle.Blocks;
        public ProgressBar()
        {
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;
        }
        protected override Size DefaultSize { get { return new Size(100, 23); } }
        public int Minimum { get { return _minimum; } set { _minimum = value; Update(); } }
        public int Maximum { get { return _maximum; } set { _maximum = value; Update(); } }
        public int Value
        {
            get { return _value; }
            set
            {
                if (value < _minimum || value > _maximum) throw new ArgumentOutOfRangeException(nameof(value));
                _value = value;
                Update();
            }
        }
        public int Step { get { return _step; } set { _step = value; } }
        public ProgressBarStyle Style { get { return _style; } set { _style = value; Update(); } }
        public int MarqueeAnimationSpeed { get; set; } = 100;
        public bool RightToLeftLayout { get; set; }
        public void PerformStep() { Increment(_step); }
        public void Increment(int value) { _value = Math.Max(_minimum, Math.Min(_maximum, _value + value)); Update(); }
        private new void Update()
        {
            if (_bar == null) return;
            _bar.Minimum = _minimum;
            _bar.Maximum = _maximum;
            _bar.Value = _value;
            _bar.IsIndeterminate = _style == ProgressBarStyle.Marquee;
        }
        protected internal override AC.Control CreateContent()
        {
            _bar = new AC.ProgressBar
            {
                MinHeight = 0,
                MinWidth = 0,
                Foreground = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0x06, 0xB0, 0x25)),
                Background = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0xE6, 0xE6, 0xE6)),
                BorderBrush = new SolidColorBrush(Avalonia.Media.Color.FromRgb(0xBC, 0xBC, 0xBC)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0),
            };
            return _bar;
        }
        protected internal override void OnContentCreated() { Update(); }
    }

    public class TrackBar : Control, ISupportInitialize
    {
        private AC.Slider _slider;
        private int _minimum, _maximum = 10, _value;
        private bool _updating;
        public TrackBar() { SetStyle(ControlStyles.Selectable, true); }
        protected override Size DefaultSize { get { return new Size(104, 45); } }
        public int Minimum { get { return _minimum; } set { _minimum = value; UpdateNative(); } }
        public int Maximum { get { return _maximum; } set { _maximum = value; UpdateNative(); } }
        public int Value
        {
            get { return _value; }
            set
            {
                value = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value == value) return;
                _value = value;
                UpdateNative();
                OnValueChanged(EventArgs.Empty);
            }
        }
        public int TickFrequency { get; set; } = 1;
        public TickStyle TickStyle { get; set; } = TickStyle.BottomRight;
        public int SmallChange { get; set; } = 1;
        public int LargeChange { get; set; } = 5;
        public Orientation Orientation { get; set; }
        public event EventHandler ValueChanged;
        public event EventHandler Scroll;
        protected virtual void OnValueChanged(EventArgs e) { ValueChanged?.Invoke(this, e); }
        protected virtual void OnScroll(EventArgs e) { Scroll?.Invoke(this, e); }
        public void SetRange(int minValue, int maxValue) { _minimum = minValue; _maximum = maxValue; UpdateNative(); }
        public void BeginInit() { }
        public void EndInit() { }
        private void UpdateNative()
        {
            if (_slider == null) return;
            _updating = true;
            _slider.Minimum = _minimum;
            _slider.Maximum = _maximum;
            _slider.Value = _value;
            _slider.Orientation = Orientation == Orientation.Vertical ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal;
            _slider.TickFrequency = TickFrequency;
            _slider.IsSnapToTickEnabled = true;
            _slider.TickPlacement = TickStyle == TickStyle.None ? AC.TickPlacement.None : AC.TickPlacement.BottomRight;
            _updating = false;
        }
        protected internal override AC.Control CreateContent()
        {
            _slider = new AC.Slider { MinHeight = 0, MinWidth = 0 };
            _slider.ValueChanged += (s, e) =>
            {
                if (_updating) return;
                int v = (int)Math.Round(e.NewValue);
                Toolkit.Dispatch(() =>
                {
                    if (v != _value)
                    {
                        _value = v;
                        OnScroll(EventArgs.Empty);
                        OnValueChanged(EventArgs.Empty);
                    }
                });
            };
            return _slider;
        }
        protected internal override void OnContentCreated() { UpdateNative(); }
    }

    public abstract class ScrollBar : Control
    {
        private AC.Primitives.ScrollBar _bar;
        private int _minimum, _maximum = 100, _value, _smallChange = 1, _largeChange = 10;
        private bool _updating;
        protected abstract bool IsVertical { get; }
        protected ScrollBar() { SetStyle(ControlStyles.Selectable, false); TabStop = false; }
        public int Minimum { get { return _minimum; } set { _minimum = value; UpdateNative(); } }
        public int Maximum { get { return _maximum; } set { _maximum = value; UpdateNative(); } }
        public int SmallChange { get { return _smallChange; } set { _smallChange = value; UpdateNative(); } }
        public int LargeChange { get { return _largeChange; } set { _largeChange = value; UpdateNative(); } }
        public int Value
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
        public event ScrollEventHandler Scroll;
        public event EventHandler ValueChanged;
        protected virtual void OnScroll(ScrollEventArgs se) { Scroll?.Invoke(this, se); }
        protected virtual void OnValueChanged(EventArgs e) { ValueChanged?.Invoke(this, e); }
        private void UpdateNative()
        {
            if (_bar == null) return;
            _updating = true;
            _bar.Minimum = _minimum;
            // WinForms can reach Maximum - LargeChange + 1 with the thumb
            _bar.Maximum = Math.Max(_minimum, _maximum - _largeChange + 1);
            _bar.ViewportSize = _largeChange;
            _bar.LargeChange = _largeChange;
            _bar.SmallChange = _smallChange;
            _bar.Value = _value;
            _updating = false;
        }
        protected internal override AC.Control CreateContent()
        {
            _bar = new AC.Primitives.ScrollBar
            {
                Orientation = IsVertical ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal,
                AllowAutoHide = false,
            };
            _bar.ValueChanged += (s, e) =>
            {
                if (_updating) return;
                int old = _value;
                int v = (int)Math.Round(e.NewValue);
                Toolkit.Dispatch(() =>
                {
                    _value = v;
                    OnScroll(new ScrollEventArgs(ScrollEventType.ThumbTrack, old, v, IsVertical ? ScrollOrientation.VerticalScroll : ScrollOrientation.HorizontalScroll));
                    if (old != v) OnValueChanged(EventArgs.Empty);
                });
            };
            return _bar;
        }
        protected internal override void OnContentCreated() { UpdateNative(); }
    }

    public class HScrollBar : ScrollBar
    {
        protected override bool IsVertical { get { return false; } }
        protected override Size DefaultSize { get { return new Size(80, SystemInformation.HorizontalScrollBarHeight); } }
    }

    public class VScrollBar : ScrollBar
    {
        protected override bool IsVertical { get { return true; } }
        protected override Size DefaultSize { get { return new Size(SystemInformation.VerticalScrollBarWidth, 80); } }
    }
}
