// PrePoMax for macOS - TextBoxBase, TextBox and RichTextBox on the Avalonia TextBox.

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using SkiaSharp;
using AC = Avalonia.Controls;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;
using HorizontalAlignment = System.Windows.Forms.HorizontalAlignment;

namespace System.Windows.Forms
{
    public enum RichTextBoxScrollBars { None = 0, Horizontal = 1, Vertical = 2, Both = 3, ForcedHorizontal = 17, ForcedVertical = 18, ForcedBoth = 19 }

    [Flags]
    public enum RichTextBoxFinds { None = 0, WholeWord = 2, MatchCase = 4, NoHighlight = 8, Reverse = 16 }

    public enum RichTextBoxStreamType { RichText = 0, PlainText = 1, RichNoOleObjs = 2, TextTextOleObjs = 3, UnicodePlainText = 4 }

    public abstract class TextBoxBase : Control, IWin32MessageTarget
    {
        protected internal AC.TextBox NativeTextBox;
        private AC.ScrollViewer _scrollViewer;
        private bool _multiline;
        private bool _readOnly;
        private bool _wordWrap = true;
        private int _maxLength = 32767;
        private BorderStyle _borderStyle = BorderStyle.Fixed3D;
        private bool _hideSelection = true;
        private bool _acceptsTab;
        private bool _modified;
        private bool _settingText;
        private int _selectionStart;
        private int _selectionLength;
        private bool _redraw = true;
        private bool _shortcutsEnabled = true;

        protected TextBoxBase()
        {
            SetStyle(ControlStyles.Selectable, true);
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, false);
            Cursor = Cursors.IBeam;
        }
        protected override Size DefaultSize { get { return new Size(100, 20); } }
        public override Color BackColor
        {
            get { return BackColorSet ? base.BackColor : (_readOnly ? SystemColors.Control : SystemColors.Window); }
            set { base.BackColor = value; UpdateNative(); }
        }
        public override Color ForeColor
        {
            get { return ForeColorSet ? base.ForeColor : SystemColors.WindowText; }
            set { base.ForeColor = value; UpdateNative(); }
        }
        public virtual bool Multiline
        {
            get { return _multiline; }
            set { _multiline = value; UpdateNative(); }
        }
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                if (_readOnly == value) return;
                _readOnly = value;
                UpdateNative();
                OnReadOnlyChanged(EventArgs.Empty);
            }
        }
        public bool WordWrap { get { return _wordWrap; } set { _wordWrap = value; UpdateNative(); } }
        public virtual int MaxLength { get { return _maxLength; } set { _maxLength = value; UpdateNative(); } }
        public BorderStyle BorderStyle { get { return _borderStyle; } set { _borderStyle = value; UpdateNative(); } }
        public bool HideSelection { get { return _hideSelection; } set { _hideSelection = value; } }
        public bool AcceptsTab { get { return _acceptsTab; } set { _acceptsTab = value; UpdateNative(); } }
        public bool Modified { get { return _modified; } set { _modified = value; } }
        public bool ShortcutsEnabled { get { return _shortcutsEnabled; } set { _shortcutsEnabled = value; } }
        public bool CanUndo { get { return NativeTextBox?.CanUndo ?? false; } }
        public int TextLength { get { return Text.Length; } }
        public int PreferredHeight { get { return Font.Height + (_borderStyle == BorderStyle.None ? 0 : 7); } }
        public string[] Lines
        {
            get { return Text.Replace("\r\n", "\n").Split('\n'); }
            set { Text = value == null ? string.Empty : string.Join(Environment.NewLine, value); }
        }
        public override string Text
        {
            get { return base.Text; }
            set
            {
                value = value ?? string.Empty;
                if (_maxLength > 0 && value.Length > _maxLength && !_multiline) value = value.Substring(0, _maxLength);
                if (base.Text == value) return;
                _settingText = true;
                try
                {
                    if (NativeTextBox != null && NativeTextBox.Text != value) NativeTextBox.Text = value;
                    _selectionStart = 0;
                    _selectionLength = 0;
                    base.Text = value;
                }
                finally
                {
                    _settingText = false;
                }
            }
        }
        public virtual int SelectionStart
        {
            get
            {
                if (NativeTextBox != null) return Math.Min(NativeTextBox.SelectionStart, NativeTextBox.SelectionEnd);
                return _selectionStart;
            }
            set
            {
                _selectionStart = Math.Max(0, Math.Min(value, TextLength));
                ApplySelection();
            }
        }
        public virtual int SelectionLength
        {
            get
            {
                if (NativeTextBox != null) return Math.Abs(NativeTextBox.SelectionEnd - NativeTextBox.SelectionStart);
                return _selectionLength;
            }
            set
            {
                _selectionStart = SelectionStart;
                _selectionLength = Math.Max(0, Math.Min(value, TextLength - _selectionStart));
                ApplySelection();
            }
        }
        public virtual string SelectedText
        {
            get
            {
                int start = SelectionStart, length = SelectionLength;
                string text = Text;
                if (start >= text.Length || length == 0) return string.Empty;
                return text.Substring(start, Math.Min(length, text.Length - start));
            }
            set
            {
                int start = SelectionStart, length = SelectionLength;
                string text = Text;
                start = Math.Min(start, text.Length);
                length = Math.Min(length, text.Length - start);
                string newText = text.Substring(0, start) + (value ?? "") + text.Substring(start + length);
                Text = newText;
                _selectionStart = start + (value ?? "").Length;
                _selectionLength = 0;
                ApplySelection();
            }
        }
        private void ApplySelection()
        {
            if (NativeTextBox == null) return;
            int start = Math.Min(_selectionStart, TextLength);
            int end = Math.Min(start + _selectionLength, TextLength);
            NativeTextBox.SelectionStart = start;
            NativeTextBox.SelectionEnd = end;
            NativeTextBox.CaretIndex = end;
        }
        public event EventHandler ReadOnlyChanged;
        public event EventHandler MultilineChanged;
        public event EventHandler BorderStyleChanged;
        public event EventHandler HideSelectionChanged;
        public event EventHandler ModifiedChanged;
        protected virtual void OnReadOnlyChanged(EventArgs e) { ReadOnlyChanged?.Invoke(this, e); }

        // Methods
        public void AppendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Text = Text + text;
            _selectionStart = TextLength;
            _selectionLength = 0;
            ApplySelection();
            ScrollToCaret();
        }
        public void Clear() { Text = string.Empty; }
        public void ClearUndo() { NativeTextBox?.ClearValue(AC.TextBox.UndoLimitProperty); }
        public void Copy() { NativeTextBox?.Copy(); }
        public void Cut() { NativeTextBox?.Cut(); }
        public void Paste() { NativeTextBox?.Paste(); }
        public void Paste(string text) { SelectedText = text; }
        public void Undo() { NativeTextBox?.Undo(); }
        public void Select(int start, int length)
        {
            _selectionStart = Math.Max(0, Math.Min(start, TextLength));
            _selectionLength = Math.Max(0, Math.Min(length, TextLength - _selectionStart));
            ApplySelection();
        }
        public void SelectAll() { Select(0, TextLength); }
        public void DeselectAll() { Select(SelectionStart, 0); }
        public void ScrollToCaret()
        {
            if (NativeTextBox == null) return;
            Dispatcher.UIThread.Post(() =>
            {
                int caret = Math.Min(NativeTextBox.CaretIndex, TextLength);
                if (caret >= TextLength && _scrollViewer != null) _scrollViewer.ScrollToEnd();
                else if (NativeTextBox.SelectionStart == NativeTextBox.SelectionEnd)
                {
                    // Setting the caret brings it into view
                    NativeTextBox.CaretIndex = Math.Max(0, caret - 1);
                    NativeTextBox.CaretIndex = caret;
                }
            }, DispatcherPriority.Background);
        }
        public int GetLineFromCharIndex(int index)
        {
            string text = Text;
            int line = 0;
            for (int i = 0; i < index && i < text.Length; i++) if (text[i] == '\n') line++;
            return line;
        }
        public int GetFirstCharIndexFromLine(int lineNumber)
        {
            if (lineNumber < 0) return -1;
            string text = Text;
            int line = 0;
            if (lineNumber == 0) return 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    line++;
                    if (line == lineNumber) return i + 1;
                }
            }
            return -1;
        }
        public int GetFirstCharIndexOfCurrentLine() { return GetFirstCharIndexFromLine(GetLineFromCharIndex(SelectionStart)); }
        public virtual char GetCharFromPosition(Point pt)
        {
            int index = GetCharIndexFromPosition(pt);
            return index >= 0 && index < TextLength ? Text[index] : '\0';
        }
        public virtual int GetCharIndexFromPosition(Point pt) { return SelectionStart; }
        public virtual Point GetPositionFromCharIndex(int index) { return Point.Empty; }
        protected virtual void OnVScroll(EventArgs e) { VScroll?.Invoke(this, e); }
        protected virtual void OnHScroll(EventArgs e) { HScroll?.Invoke(this, e); }
        public event EventHandler VScroll;
        public event EventHandler HScroll;

        // The scroll messages the PrePoMax text boxes send
        public bool HandleMessage(ref Message m)
        {
            switch (m.Msg)
            {
                case Win32Messages.WM_SETREDRAW:
                    _redraw = ((long)m.WParam & 0xFFFFFFFF) != 0;
                    return true;
                case Win32Messages.WM_VSCROLL:
                    {
                        int code = (int)((long)m.WParam & 0xFFFF);
                        if (code == Win32Messages.SB_BOTTOM) ScrollViewerAction(sv => sv.ScrollToEnd());
                        else if (code == Win32Messages.SB_TOP) ScrollViewerAction(sv => sv.ScrollToHome());
                        else if (code == 0) ScrollViewerAction(sv => sv.LineUp());
                        else if (code == 1) ScrollViewerAction(sv => sv.LineDown());
                        else if (code == 2) ScrollViewerAction(sv => sv.PageUp());
                        else if (code == 3) ScrollViewerAction(sv => sv.PageDown());
                        return true;
                    }
                case Win32Messages.WM_HSCROLL:
                    {
                        int code = (int)((long)m.WParam & 0xFFFF);
                        if (code == Win32Messages.SB_TOP) ScrollViewerAction(sv => sv.Offset = new Vector(0, sv.Offset.Y));
                        return true;
                    }
                case Win32Messages.EM_GETFIRSTVISIBLELINE:
                    m.Result = new IntPtr(FirstVisibleLine());
                    return true;
                case Win32Messages.EM_LINESCROLL:
                    {
                        int lines = (int)((long)m.LParam & 0xFFFFFFFF);
                        double lineHeight = Math.Max(1, Font.GetHeight());
                        ScrollViewerAction(sv => sv.Offset = new Vector(sv.Offset.X, Math.Max(0, sv.Offset.Y + lines * lineHeight)));
                        return true;
                    }
                case Win32Messages.EM_SCROLLCARET:
                    ScrollToCaret();
                    return true;
                case 1095:      // EM_SETPARAFORMAT
                case Win32Messages.EM_SETTABSTOPS:
                    return true;
            }
            return false;
        }
        private int FirstVisibleLine()
        {
            if (_scrollViewer == null) return 0;
            return (int)Math.Floor(_scrollViewer.Offset.Y / Math.Max(1, Font.GetHeight()));
        }
        private void ScrollViewerAction(Action<AC.ScrollViewer> action)
        {
            if (_scrollViewer != null) action(_scrollViewer);
            else Dispatcher.UIThread.Post(() => { if (_scrollViewer != null) action(_scrollViewer); }, DispatcherPriority.Background);
        }

        // Native
        protected internal override AC.Control CreateContent()
        {
            NativeTextBox = new AC.TextBox { MinHeight = 0, MinWidth = 0, VerticalContentAlignment = VerticalAlignment.Top };
            NativeTextBox.Classes.Add("winforms");
            NativeTextBox.Text = Text;
            NativeTextBox.TemplateApplied += (s, e) =>
            {
                _scrollViewer = e.NameScope.Find("PART_ScrollViewer") as AC.ScrollViewer;
                if (_scrollViewer != null) _scrollViewer.ScrollChanged += (s2, e2) =>
                {
                    if (e2.OffsetDelta.Y != 0) Toolkit.Dispatch(() => OnVScroll(EventArgs.Empty));
                    if (e2.OffsetDelta.X != 0) Toolkit.Dispatch(() => OnHScroll(EventArgs.Empty));
                };
            };
            NativeTextBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == AC.TextBox.TextProperty && !_settingText)
                {
                    string text = NativeTextBox.Text ?? string.Empty;
                    if (text != base.Text)
                    {
                        _modified = true;
                        Toolkit.Dispatch(() =>
                        {
                            _settingText = true;
                            try { base.Text = text; }
                            finally { _settingText = false; }
                        });
                    }
                }
            };
            NativeTextBox.AddHandler(AC.TextBox.KeyDownEvent, (s, e) =>
            {
                // Tab moves the focus unless the text box accepts tabs
                if (e.Key == Key.Tab && !_acceptsTab) e.Handled = false;
            }, RoutingStrategies.Tunnel);
            NativeTextBox.ContextRequested += (s, e) =>
            {
                if (ContextMenuStrip != null) e.Handled = true;
            };
            return NativeTextBox;
        }
        protected internal override void OnContentCreated()
        {
            UpdateNative();
            ApplySelection();
        }
        internal virtual void UpdateNative()
        {
            if (NativeTextBox == null) return;
            NativeTextBox.AcceptsReturn = _multiline;
            NativeTextBox.AcceptsTab = _acceptsTab;
            NativeTextBox.TextWrapping = _multiline && _wordWrap ? TextWrapping.Wrap : TextWrapping.NoWrap;
            NativeTextBox.IsReadOnly = _readOnly;
            NativeTextBox.MaxLength = _maxLength > 0 && _maxLength < 32767 ? _maxLength : 0;
            // Multiline: the scroll viewer of the template fills the box (top alignment would size it to the text)
            NativeTextBox.VerticalContentAlignment = _multiline ? VerticalAlignment.Stretch : VerticalAlignment.Center;
            if (_readOnly) NativeTextBox.Classes.Add("readonly");
            else NativeTextBox.Classes.Remove("readonly");
            if (_borderStyle == BorderStyle.None) NativeTextBox.Classes.Add("noborder");
            else NativeTextBox.Classes.Remove("noborder");
            NativeTextBox.Background = Toolkit.ToBrush(BackColor);
            NativeTextBox.Foreground = Toolkit.ToBrush(ForeColor);
            AC.ScrollViewer.SetVerticalScrollBarVisibility(NativeTextBox, VerticalScrollBars ? ScrollBarVisibility.Visible :
                                                           _multiline ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled);
            AC.ScrollViewer.SetHorizontalScrollBarVisibility(NativeTextBox, HorizontalScrollBars ? ScrollBarVisibility.Visible :
                                                             ScrollBarVisibility.Hidden);
        }
        internal virtual bool VerticalScrollBars { get { return false; } }
        internal virtual bool HorizontalScrollBars { get { return false; } }
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Enter && _multiline) return true;
            if (key == Keys.Tab && _acceptsTab) return true;
            return base.IsInputKey(keyData);
        }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = ContentLayout.MeasureText(Text, Font);
            return new Size(Math.Max(text.Width + 8, Width), PreferredHeight);
        }
    }

    public class TextBox : TextBoxBase
    {
        private ScrollBars _scrollBars;
        private HorizontalAlignment _textAlign;
        private char _passwordChar;
        private bool _useSystemPasswordChar;
        private CharacterCasing _characterCasing;

        public TextBox() { }
        public ScrollBars ScrollBars { get { return _scrollBars; } set { _scrollBars = value; UpdateNative(); } }
        public HorizontalAlignment TextAlign
        {
            get { return _textAlign; }
            set
            {
                _textAlign = value;
                UpdateNative();
                TextAlignChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public char PasswordChar { get { return _passwordChar; } set { _passwordChar = value; UpdateNative(); } }
        public bool UseSystemPasswordChar { get { return _useSystemPasswordChar; } set { _useSystemPasswordChar = value; UpdateNative(); } }
        public CharacterCasing CharacterCasing { get { return _characterCasing; } set { _characterCasing = value; } }
        public bool AcceptsReturn { get; set; }
        public AutoCompleteMode AutoCompleteMode { get; set; }
        public AutoCompleteSource AutoCompleteSource { get; set; }
        public AutoCompleteStringCollection AutoCompleteCustomSource { get; set; } = new AutoCompleteStringCollection();
        public string PlaceholderText { get; set; }
        public event EventHandler TextAlignChanged;
        internal override bool VerticalScrollBars { get { return Multiline && (_scrollBars & ScrollBars.Vertical) != 0; } }
        internal override bool HorizontalScrollBars { get { return Multiline && (_scrollBars & ScrollBars.Horizontal) != 0; } }
        internal override void UpdateNative()
        {
            base.UpdateNative();
            if (NativeTextBox == null) return;
            NativeTextBox.TextAlignment = _textAlign == HorizontalAlignment.Right ? TextAlignment.Right :
                                          _textAlign == HorizontalAlignment.Center ? TextAlignment.Center : TextAlignment.Left;
            NativeTextBox.PasswordChar = _useSystemPasswordChar ? '●' : _passwordChar;
            if (AcceptsReturn) NativeTextBox.AcceptsReturn = true;
            NativeTextBox.Watermark = PlaceholderText;
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (!e.Handled && _characterCasing != CharacterCasing.Normal && char.IsLetter(e.KeyChar))
            {
                e.Handled = true;
                SelectedText = (_characterCasing == CharacterCasing.Upper ? char.ToUpperInvariant(e.KeyChar) : char.ToLowerInvariant(e.KeyChar)).ToString();
            }
        }
    }

    public enum AutoCompleteMode { None = 0, Suggest = 1, Append = 2, SuggestAppend = 3 }

    public enum AutoCompleteSource { FileSystem = 1, HistoryList = 2, RecentlyUsedList = 4, AllUrl = 6, AllSystemSources = 7, FileSystemDirectories = 32, CustomSource = 64, None = 128, ListItems = 256 }

    public class AutoCompleteStringCollection : List<string>
    {
        public void AddRange(string[] value) { base.AddRange(value); }
    }

    /// <summary>A rich text box shows plain text; the formatting of the selection is kept as text colors where possible.</summary>
    public class RichTextBox : TextBoxBase
    {
        private RichTextBoxScrollBars _scrollBars = RichTextBoxScrollBars.Both;
        public RichTextBox()
        {
            Multiline = true;
        }
        public RichTextBoxScrollBars ScrollBars { get { return _scrollBars; } set { _scrollBars = value; UpdateNative(); } }
        public bool DetectUrls { get; set; } = true;
        public float ZoomFactor { get; set; } = 1;
        public bool AutoWordSelection { get; set; }
        public bool EnableAutoDragDrop { get; set; }
        public int BulletIndent { get; set; }
        public int RightMargin { get; set; }
        public Color SelectionColor { get; set; }
        public Color SelectionBackColor { get; set; }
        public Font SelectionFont { get; set; }
        public HorizontalAlignment SelectionAlignment { get; set; }
        public int SelectionIndent { get; set; }
        public int SelectionHangingIndent { get; set; }
        public int SelectionRightIndent { get; set; }
        public bool SelectionBullet { get; set; }
        public int SelectionCharOffset { get; set; }
        public bool SelectionProtected { get; set; }
        public string SelectedRtf { get { return SelectedText; } set { SelectedText = value; } }
        public string Rtf { get { return Text; } set { Text = value; } }
        public bool CanRedo { get { return NativeTextBox?.CanRedo ?? false; } }
        public event LinkClickedEventHandler LinkClicked;
        public event EventHandler SelectionChanged;
        public event ContentsResizedEventHandler ContentsResized;
        internal override bool VerticalScrollBars { get { return (_scrollBars & RichTextBoxScrollBars.Vertical) != 0; } }
        internal override bool HorizontalScrollBars { get { return (_scrollBars & RichTextBoxScrollBars.Horizontal) != 0 && !WordWrap; } }
        public void Redo() { NativeTextBox?.Redo(); }
        public void Paste(DataFormats.Format clipFormat) { Paste(); }
        public bool CanPaste(DataFormats.Format clipFormat) { return true; }
        public int Find(string str) { return Find(str, 0, RichTextBoxFinds.None); }
        public int Find(string str, RichTextBoxFinds options) { return Find(str, 0, options); }
        public int Find(string str, int start, RichTextBoxFinds options)
        {
            StringComparison comparison = (options & RichTextBoxFinds.MatchCase) != 0 ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            int index = Text.IndexOf(str, Math.Min(start, TextLength), comparison);
            if (index >= 0 && (options & RichTextBoxFinds.NoHighlight) == 0) Select(index, str.Length);
            return index;
        }
        public int Find(string str, int start, int end, RichTextBoxFinds options) { return Find(str, start, options); }
        public void LoadFile(string path) { Text = IO.File.ReadAllText(path); }
        public void LoadFile(string path, RichTextBoxStreamType fileType) { LoadFile(path); }
        public void SaveFile(string path) { IO.File.WriteAllText(path, Text); }
        public void SaveFile(string path, RichTextBoxStreamType fileType) { SaveFile(path); }
        public override Size GetPreferredSize(Size proposedSize)
        {
            Size text = ContentLayout.MeasureText(Text, Font);
            return new Size(text.Width + 10, text.Height + 6);
        }
    }

    public class LinkClickedEventArgs : EventArgs
    {
        public string LinkText { get; }
        public LinkClickedEventArgs(string linkText) { LinkText = linkText; }
    }

    public delegate void LinkClickedEventHandler(object sender, LinkClickedEventArgs e);

    public class ContentsResizedEventArgs : EventArgs
    {
        public Rectangle NewRectangle { get; }
        public ContentsResizedEventArgs(Rectangle newRectangle) { NewRectangle = newRectangle; }
    }

    public delegate void ContentsResizedEventHandler(object sender, ContentsResizedEventArgs e);
}
