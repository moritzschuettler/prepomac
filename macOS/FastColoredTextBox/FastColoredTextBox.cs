// PrePoMax for macOS - FastColoredTextBox: the code editor API that PrePoMax uses for the Python editor, the Python console
// and the CalculiX keyword editor, implemented on the AvaloniaEdit text editor.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Search;
using AC = Avalonia.Controls;
using Color = System.Drawing.Color;
using Brush = System.Drawing.Brush;
using SolidBrush = System.Drawing.SolidBrush;
using FontStyle = System.Drawing.FontStyle;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;
using FontFamily = Avalonia.Media.FontFamily;
using DataObject = System.Windows.Forms.DataObject;
using DataFormats = System.Windows.Forms.DataFormats;
using Avalonia.VisualTree;
using Avalonia;

namespace FastColoredTextBoxNS
{
    public enum Language { Custom, CSharp, VB, HTML, XML, SQL, PHP, JS, Lua, JSON }
    public enum WordWrapMode { WordWrapControlWidth, WordWrapPreferredWidth, CharWrapControlWidth, CharWrapPreferredWidth, Custom }
    public enum FCTBAction
    {
        None, AutocompleteMenu, AutoIndentChars, BookmarkLine, ClearWordLeft, ClearWordRight, CommentSelected, Copy, Cut, DeleteCharRight,
        FindChar, FindDialog, FindNext, GoDown, GoDownWithSelection, GoDown_ColumnSelectionMode, GoEnd, GoEndWithSelection, GoFirstLine,
        GoFirstLineWithSelection, GoHome, GoHomeWithSelection, GoLastLine, GoLastLineWithSelection, GoLeft, GoLeftWithSelection,
        GoLeft_ColumnSelectionMode, GoPageDown, GoPageDownWithSelection, GoPageDown_ColumnSelectionMode, GoPageUp, GoPageUpWithSelection,
        GoPageUp_ColumnSelectionMode, GoRight, GoRightWithSelection, GoRight_ColumnSelectionMode, GoToDialog, GoNextBookmark, GoPrevBookmark,
        GoUp, GoUpWithSelection, GoUp_ColumnSelectionMode, GoWordLeft, GoWordLeftWithSelection, GoWordRight, GoWordRightWithSelection,
        IndentIncrease, IndentDecrease, LowerCase, MacroExecute, MacroRecord, MoveSelectedLinesDown, MoveSelectedLinesUp, NavigateBackward,
        NavigateForward, Paste, Redo, ReplaceDialog, ReplaceMode, ScrollDown, ScrollUp, SelectAll, UnbookmarkLine, Undo, UpperCase, ZoomIn,
        ZoomNormal, ZoomOut, CustomAction1, CustomAction2, CustomAction3, CustomAction4, CustomAction5, CustomAction6, CustomAction7,
        CustomAction8, CustomAction9, CustomAction10, CustomAction11, CustomAction12, CustomAction13, CustomAction14, CustomAction15,
        CustomAction16, CustomAction17, CustomAction18, CustomAction19, CustomAction20, ClearHints_
    }

    [Serializable]
    public struct Place : IEquatable<Place>
    {
        public int iChar;
        public int iLine;
        public Place(int iChar, int iLine) { this.iChar = iChar; this.iLine = iLine; }
        public static Place Empty { get { return new Place(); } }
        public void Offset(int dx, int dy) { iChar += dx; iLine += dy; }
        public bool Equals(Place other) { return iChar == other.iChar && iLine == other.iLine; }
        public override bool Equals(object obj) { return obj is Place p && Equals(p); }
        public override int GetHashCode() { return iChar.GetHashCode() ^ iLine.GetHashCode(); }
        public static bool operator ==(Place p1, Place p2) { return p1.Equals(p2); }
        public static bool operator !=(Place p1, Place p2) { return !p1.Equals(p2); }
        public static bool operator <(Place p1, Place p2) { return p1.iLine < p2.iLine || (p1.iLine == p2.iLine && p1.iChar < p2.iChar); }
        public static bool operator <=(Place p1, Place p2) { return p1 == p2 || p1 < p2; }
        public static bool operator >(Place p1, Place p2) { return p2 < p1; }
        public static bool operator >=(Place p1, Place p2) { return p1 == p2 || p1 > p2; }
        public static Place operator +(Place p1, Place p2) { return new Place(p1.iChar + p2.iChar, p1.iLine + p2.iLine); }
        public override string ToString() { return "(" + iChar + "," + iLine + ")"; }
    }

    // Styles
    public abstract class Style : IDisposable
    {
        public bool IsExportable { get; set; } = true;
        public virtual void Dispose() { }
        internal abstract void Apply(VisualLineElement element);
    }

    public class TextStyle : Style
    {
        public Brush ForeBrush { get; set; }
        public Brush BackgroundBrush { get; set; }
        public FontStyle FontStyle { get; set; }
        public System.Drawing.StringFormat stringFormat;
        public TextStyle(Brush foreBrush, Brush backgroundBrush, FontStyle fontStyle)
        {
            ForeBrush = foreBrush;
            BackgroundBrush = backgroundBrush;
            FontStyle = fontStyle;
        }
        internal override void Apply(VisualLineElement element)
        {
            if (ForeBrush is SolidBrush fore) element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Convert(fore.Color)));
            if (BackgroundBrush is SolidBrush back) element.BackgroundBrush = new SolidColorBrush(Convert(back.Color));
            if ((FontStyle & (FontStyle.Bold | FontStyle.Italic)) != 0)
            {
                Typeface t = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetTypeface(new Typeface(t.FontFamily,
                    (FontStyle & FontStyle.Italic) != 0 ? Avalonia.Media.FontStyle.Italic : t.Style,
                    (FontStyle & FontStyle.Bold) != 0 ? FontWeight.Bold : t.Weight));
            }
            if ((FontStyle & FontStyle.Underline) != 0) element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
        }
        internal static Avalonia.Media.Color Convert(Color c) { return Avalonia.Media.Color.FromArgb(c.A, c.R, c.G, c.B); }
    }

    public class MarkerStyle : Style
    {
        public Brush BackgroundBrush { get; set; }
        public MarkerStyle(Brush backgroundBrush) { BackgroundBrush = backgroundBrush; }
        internal override void Apply(VisualLineElement element)
        {
            if (BackgroundBrush is SolidBrush back) element.BackgroundBrush = new SolidColorBrush(TextStyle.Convert(back.Color));
        }
    }

    public class SelectionStyle : Style
    {
        public Brush BackgroundBrush { get; set; }
        public SelectionStyle(Brush backgroundBrush) { BackgroundBrush = backgroundBrush; }
        internal override void Apply(VisualLineElement element) { }
    }

    public class ReadOnlyStyle : Style { internal override void Apply(VisualLineElement element) { } }

    // A span of a style in the document; the offsets move with the edits
    internal sealed class StyleSpan
    {
        public Style Style;
        public TextAnchor Start;
        public TextAnchor End;
    }

    // Ranges
    public class Range : IEnumerable<Place>
    {
        private readonly FastColoredTextBox _tb;
        private Place _start, _end;
        public Range(FastColoredTextBox tb) { _tb = tb; }
        public Range(FastColoredTextBox tb, int iStartChar, int iStartLine, int iEndChar, int iEndLine) : this(tb)
        {
            _start = new Place(iStartChar, iStartLine);
            _end = new Place(iEndChar, iEndLine);
        }
        public Range(FastColoredTextBox tb, Place start, Place end) : this(tb) { _start = start; _end = end; }
        public Range(FastColoredTextBox tb, int iLine) : this(tb, 0, iLine, tb.GetLineLength(iLine), iLine) { }
        public FastColoredTextBox tb { get { return _tb; } }
        public Place Start
        {
            get { return _start; }
            set { _start = value; if (_tb != null && ReferenceEquals(_tb.SelectionRange, this)) _tb.ApplySelection(this); }
        }
        public Place End
        {
            get { return _end; }
            set { _end = value; if (_tb != null && ReferenceEquals(_tb.SelectionRange, this)) _tb.ApplySelection(this); }
        }
        public int FromLine { get { return Math.Min(_start.iLine, _end.iLine); } }
        public int ToLine { get { return Math.Max(_start.iLine, _end.iLine); } }
        public int FromX { get { return _start < _end ? _start.iChar : _end.iChar; } }
        public int ToX { get { return _start < _end ? _end.iChar : _start.iChar; } }
        public bool IsEmpty { get { return _start == _end; } }
        public bool ColumnSelectionMode { get; set; }
        public bool ReadOnly { get; set; }
        public int Length { get { return Text.Length; } }
        public string Text
        {
            get
            {
                int a = _tb.PlaceToPosition(_start), b = _tb.PlaceToPosition(_end);
                if (a > b) (a, b) = (b, a);
                return _tb.Document.GetText(a, b - a);
            }
        }
        public char CharBeforeStart
        {
            get
            {
                int p = _tb.PlaceToPosition(_start);
                return p > 0 ? _tb.Document.GetCharAt(p - 1) : '\n';
            }
        }
        public char CharAfterStart
        {
            get
            {
                int p = _tb.PlaceToPosition(_start);
                return p < _tb.Document.TextLength ? _tb.Document.GetCharAt(p) : '\n';
            }
        }
        public void Normalize()
        {
            if (_end < _start) (_start, _end) = (_end, _start);
        }
        public Range Clone() { return new Range(_tb, _start, _end); }
        public void SelectAll()
        {
            _start = new Place(0, 0);
            int last = Math.Max(0, _tb.LinesCount - 1);
            _end = new Place(_tb.GetLineLength(last), last);
        }
        public bool Contains(Place place)
        {
            Place a = _start < _end ? _start : _end, b = _start < _end ? _end : _start;
            return place >= a && place <= b;
        }
        public Range GetIntersectionWith(Range range)
        {
            Range r1 = Clone(), r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            Place start = r1.Start > r2.Start ? r1.Start : r2.Start;
            Place end = r1.End < r2.End ? r1.End : r2.End;
            return end < start ? new Range(_tb, start, start) : new Range(_tb, start, end);
        }
        public Range GetUnionWith(Range range)
        {
            Range r1 = Clone(), r2 = range.Clone();
            r1.Normalize();
            r2.Normalize();
            return new Range(_tb, r1.Start < r2.Start ? r1.Start : r2.Start, r1.End > r2.End ? r1.End : r2.End);
        }
        public void Expand()
        {
            Normalize();
            _start = new Place(0, _start.iLine);
            _end = new Place(_tb.GetLineLength(_end.iLine), _end.iLine);
        }
        public bool GoRight() { int p = _tb.PlaceToPosition(_start); if (p >= _tb.Document.TextLength) return false; _start = _end = _tb.PositionToPlace(p + 1); return true; }
        public bool GoLeft() { int p = _tb.PlaceToPosition(_start); if (p <= 0) return false; _start = _end = _tb.PositionToPlace(p - 1); return true; }
        public bool GoRightThroughFolded() { return GoRight(); }
        public void GoLineEnd() { _start = _end = new Place(_tb.GetLineLength(_start.iLine), _start.iLine); }
        public void GoHome() { _start = _end = new Place(0, _start.iLine); }
        public void SetStyle(Style style) { _tb.AddStyle(style, this); }
        public void SetStyle(Style style, string regexPattern) { SetStyle(style, regexPattern, RegexOptions.None); }
        public void SetStyle(Style style, string regexPattern, RegexOptions options) { SetStyle(style, new Regex(regexPattern, options)); }
        public void SetStyle(Style style, Regex regex)
        {
            foreach (Range r in GetRanges(regex)) _tb.AddStyle(style, r);
            _tb.Redraw();
        }
        public void SetStyle(StyleIndex styleIndex) { }
        public void ClearStyle(params Style[] styles) { _tb.ClearStyles(this, styles); }
        public void ClearStyle(StyleIndex styleIndex) { _tb.ClearStyles(this, null); }
        public void ClearFoldingMarkers() { }
        public void SetFoldingMarkers(string startFoldingPattern, string finishFoldingPattern) { }
        public IEnumerable<Range> GetRanges(string regexPattern) { return GetRanges(new Regex(regexPattern)); }
        public IEnumerable<Range> GetRanges(string regexPattern, RegexOptions options) { return GetRanges(new Regex(regexPattern, options)); }
        public IEnumerable<Range> GetRanges(Regex regex)
        {
            int a = _tb.PlaceToPosition(_start), b = _tb.PlaceToPosition(_end);
            if (a > b) (a, b) = (b, a);
            string text = _tb.Document.GetText(a, b - a);
            foreach (Match m in regex.Matches(text))
            {
                Group g = m.Groups["range"].Success ? m.Groups["range"] : m.Groups[0];
                yield return new Range(_tb, _tb.PositionToPlace(a + g.Index), _tb.PositionToPlace(a + g.Index + g.Length));
            }
        }
        public IEnumerable<Range> GetRangesByLines(string regexPattern, RegexOptions options) { return GetRanges(regexPattern, options); }
        public IEnumerator<Place> GetEnumerator()
        {
            int a = _tb.PlaceToPosition(_start), b = _tb.PlaceToPosition(_end);
            if (a > b) (a, b) = (b, a);
            for (int p = a; p < b; p++) yield return _tb.PositionToPlace(p);
        }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public override string ToString() { return "Start: " + _start + " End: " + _end; }
    }

    [Flags]
    public enum StyleIndex : ushort { None = 0, Style0 = 1, Style1 = 2, Style2 = 4, Style3 = 8, Style4 = 16, Style5 = 32, Style6 = 64, Style7 = 128, All = 0xFFFF }

    // Event arguments
    public class TextChangedEventArgs : EventArgs
    {
        public Range ChangedRange { get; set; }
        public TextChangedEventArgs(Range changedRange) { ChangedRange = changedRange; }
    }

    public class TextChangingEventArgs : EventArgs
    {
        public string InsertingText { get; set; }
        public bool Cancel { get; set; }
    }

    public class KeyPressingEventArgs : KeyPressEventArgs
    {
        public KeyPressingEventArgs(char keyChar) : base(keyChar) { }
    }

    public class AutoIndentEventArgs : EventArgs
    {
        public int iLine { get; }
        public string LineText { get; }
        public string PrevLineText { get; }
        public int TabLength { get; }
        public int Shift { get; set; }
        public int ShiftNextLines { get; set; }
        public int AbsoluteIndentation { get; set; }
        public AutoIndentEventArgs(int iLine, string lineText, string prevLineText, int tabLength, int currentIndentation)
        {
            this.iLine = iLine; LineText = lineText; PrevLineText = prevLineText; TabLength = tabLength; AbsoluteIndentation = currentIndentation;
        }
    }

    // The keys of the editor actions; a key of an action that is not in the mapping is not processed by the editor
    public class HotkeysMapping : SortedDictionary<Keys, FCTBAction>
    {
        public HotkeysMapping() { }
        public virtual void InitDefault()
        {
            Clear();
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.G] = FCTBAction.GoToDialog;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.F] = FCTBAction.FindDialog;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F] = FCTBAction.FindChar;
            this[System.Windows.Forms.Keys.F3] = FCTBAction.FindNext;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.H] = FCTBAction.ReplaceDialog;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C] = FCTBAction.Copy;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.C] = FCTBAction.CommentSelected;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X] = FCTBAction.Cut;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V] = FCTBAction.Paste;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.A] = FCTBAction.SelectAll;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Z] = FCTBAction.Undo;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R] = FCTBAction.Redo;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U] = FCTBAction.UpperCase;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.U] = FCTBAction.LowerCase;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.OemMinus] = FCTBAction.NavigateBackward;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.OemMinus] = FCTBAction.NavigateForward;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.B] = FCTBAction.BookmarkLine;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.B] = FCTBAction.UnbookmarkLine;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N] = FCTBAction.GoNextBookmark;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.N] = FCTBAction.GoPrevBookmark;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Back] = FCTBAction.Undo;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Back] = FCTBAction.ClearWordLeft;
            this[System.Windows.Forms.Keys.Insert] = FCTBAction.ReplaceMode;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Insert] = FCTBAction.Copy;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Insert] = FCTBAction.Paste;
            this[System.Windows.Forms.Keys.Delete] = FCTBAction.DeleteCharRight;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete] = FCTBAction.ClearWordRight;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Delete] = FCTBAction.Cut;
            this[System.Windows.Forms.Keys.Left] = FCTBAction.GoLeft;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Left] = FCTBAction.GoLeftWithSelection;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Left] = FCTBAction.GoWordLeft;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Left] = FCTBAction.GoWordLeftWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Left] = FCTBAction.GoLeft_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.Right] = FCTBAction.GoRight;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Right] = FCTBAction.GoRightWithSelection;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Right] = FCTBAction.GoWordRight;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Right] = FCTBAction.GoWordRightWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Right] = FCTBAction.GoRight_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.Up] = FCTBAction.GoUp;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Up] = FCTBAction.GoUpWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Up] = FCTBAction.GoUp_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Up] = FCTBAction.MoveSelectedLinesUp;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Up] = FCTBAction.ScrollUp;
            this[System.Windows.Forms.Keys.Down] = FCTBAction.GoDown;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Down] = FCTBAction.GoDownWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Down] = FCTBAction.GoDown_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Down] = FCTBAction.MoveSelectedLinesDown;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Down] = FCTBAction.ScrollDown;
            this[System.Windows.Forms.Keys.PageUp] = FCTBAction.GoPageUp;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.PageUp] = FCTBAction.GoPageUpWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.PageUp] = FCTBAction.GoPageUp_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.PageDown] = FCTBAction.GoPageDown;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.PageDown] = FCTBAction.GoPageDownWithSelection;
            this[System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.PageDown] = FCTBAction.GoPageDown_ColumnSelectionMode;
            this[System.Windows.Forms.Keys.Home] = FCTBAction.GoHome;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Home] = FCTBAction.GoHomeWithSelection;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Home] = FCTBAction.GoFirstLine;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Home] = FCTBAction.GoFirstLineWithSelection;
            this[System.Windows.Forms.Keys.End] = FCTBAction.GoEnd;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.End] = FCTBAction.GoEndWithSelection;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.End] = FCTBAction.GoLastLine;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.End] = FCTBAction.GoLastLineWithSelection;
            this[System.Windows.Forms.Keys.Escape] = FCTBAction.ClearHints_;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M] = FCTBAction.MacroRecord;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.E] = FCTBAction.MacroExecute;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Space] = FCTBAction.AutocompleteMenu;
            this[System.Windows.Forms.Keys.Tab] = FCTBAction.IndentIncrease;
            this[System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Tab] = FCTBAction.IndentDecrease;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Subtract] = FCTBAction.ZoomOut;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Add] = FCTBAction.ZoomIn;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.D0] = FCTBAction.ZoomNormal;
            this[System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.I] = FCTBAction.AutoIndentChars;
        }
        public static HotkeysMapping CreateDefault() { HotkeysMapping m = new HotkeysMapping(); m.InitDefault(); return m; }
    }

    // The scroll position of the editor like the scroll properties of FastColoredTextBox
    public class EditorScroll
    {
        private readonly FastColoredTextBox _tb;
        private readonly bool _vertical;
        internal EditorScroll(FastColoredTextBox tb, bool vertical) { _tb = tb; _vertical = vertical; }
        public int Minimum { get { return 0; } set { } }
        public int Maximum { get { return (int)Math.Ceiling(_vertical ? _tb.ScrollExtentY : _tb.ScrollExtentX); } set { } }
        public int Value
        {
            get { return (int)Math.Round(_vertical ? _tb.ScrollOffsetY : _tb.ScrollOffsetX); }
            set { if (_vertical) _tb.ScrollOffsetY = value; else _tb.ScrollOffsetX = value; }
        }
        public int SmallChange { get; set; } = 16;
        public int LargeChange { get; set; } = 100;
        public bool Visible { get { return Maximum > 0; } set { } }
        public bool Enabled { get; set; } = true;
    }

    public class Line : List<char>
    {
        public Line(string text) { if (text != null) AddRange(text); }
        public string Text { get { return new string(ToArray()); } }
        public int StartSpacesCount { get { int i = 0; while (i < Count && this[i] == ' ') i++; return i; } }
        public bool IsChanged { get; set; }
    }

    public class Bookmark
    {
        public int LineIndex { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; } = Color.PowderBlue;
        public Bookmark(FastColoredTextBox tb, string name, int lineIndex) { Name = name; LineIndex = lineIndex; }
    }

    public class Bookmarks : List<Bookmark>
    {
        private readonly FastColoredTextBox _tb;
        public Bookmarks(FastColoredTextBox tb) { _tb = tb; }
        public void Add(int lineIndex) { if (!Contains(lineIndex)) Add(new Bookmark(_tb, "Bookmark " + Count, lineIndex)); }
        public bool Contains(int lineIndex) { return this.Any(b => b.LineIndex == lineIndex); }
        public bool Remove(int lineIndex) { return RemoveAll(b => b.LineIndex == lineIndex) > 0; }
        public Bookmark GetBookmark(int i) { return this[i]; }
    }

    // The editor
    public class FastColoredTextBox : UserControl, System.ComponentModel.ISupportInitialize
    {
        public void BeginInit() { }
        public void EndInit() { }
        private TextEditor _editor;
        private readonly List<StyleSpan> _spans = new List<StyleSpan>();
        private Range _selection;
        private bool _keyDownProcessed;
        private static readonly HotkeysMapping DefaultMapping = HotkeysMapping.CreateDefault();
        private bool _readOnly;
        private bool _wordWrap;
        private bool _showLineNumbers = true;
        private int _updating;
        private int _autoUndo;
        private DispatcherTimer _delayTimer;
        private Range _delayedRange;
        private bool _settingText;
        private bool _changed;
        private Font _font = new Font("Courier New", 9.75f);
        private float _zoom = 100;
        private Color _backColor = Color.White;
        private Color _lineNumberColor = Color.Teal;
        private Color _currentLineColor = Color.Transparent;
        private Color _selectionColor = Color.Blue;

        public FastColoredTextBox()
        {
            Bookmarks = new Bookmarks(this);
            _selection = new Range(this);
            HotkeysMapping = HotkeysMapping.CreateDefault();
            BorderStyle = BorderStyle.None;
        }
        protected override Size DefaultSize { get { return new Size(150, 150); } }
        internal TextDocument Document { get { EnsureEditor(); return _editor.Document; } }
        public TextEditor NativeEditor { get { EnsureEditor(); return _editor; } }
        private void EnsureEditor() { _ = NativeControl; }
        protected override AC.Control CreateContent()
        {
            _editor = new TextEditor
            {
                FontFamily = new FontFamily("Menlo"),
                ShowLineNumbers = true,
                Background = Avalonia.Media.Brushes.White,
                HorizontalScrollBarVisibility = AC.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = AC.Primitives.ScrollBarVisibility.Auto,
                Padding = new Avalonia.Thickness(2, 0, 0, 0),
            };
            _editor.Options.EnableHyperlinks = false;
            _editor.Options.EnableEmailHyperlinks = false;
            _editor.Options.ConvertTabsToSpaces = true;
            _editor.Options.IndentationSize = 4;
            _editor.Options.HighlightCurrentLine = false;
            _editor.TextArea.TextView.LineTransformers.Add(new StyleColorizer(this));
            _editor.Document.Changing += Document_Changing;
            _editor.Document.Changed += Document_Changed;
            _editor.TextArea.Caret.PositionChanged += (s, e) => SelectionChangedNative();
            _editor.TextArea.SelectionChanged += (s, e) => SelectionChangedNative();
            SearchPanel.Install(_editor);
            ApplyOptions();
            return _editor;
        }
        private void ApplyOptions()
        {
            if (_editor == null) return;
            _editor.IsReadOnly = _readOnly;
            _editor.WordWrap = _wordWrap;
            _editor.ShowLineNumbers = _showLineNumbers;
            _editor.FontFamily = new FontFamily(Font.MapFamily(_font.Name) ?? "Menlo");
            _editor.FontSize = _font.SizeInPixels * _zoom / 100f;
            _editor.Background = new SolidColorBrush(TextStyle.Convert(_backColor));
            _editor.LineNumbersForeground = new SolidColorBrush(TextStyle.Convert(_lineNumberColor));
            _editor.Options.HighlightCurrentLine = _currentLineColor.A > 0;
            if (_currentLineColor.A > 0) _editor.TextArea.TextView.CurrentLineBackground = new SolidColorBrush(TextStyle.Convert(_currentLineColor));
            _editor.TextArea.SelectionBrush = new SolidColorBrush(Avalonia.Media.Color.FromArgb(60, _selectionColor.R, _selectionColor.G, _selectionColor.B));
            _editor.Options.IndentationSize = TabLength;
        }

        // Properties
        public override string Text
        {
            get { return _editor != null ? _editor.Document.Text : base.Text; }
            set
            {
                EnsureEditor();
                _settingText = true;
                try
                {
                    _editor.Document.Text = value ?? string.Empty;
                    ClearUndoStack();
                }
                finally
                {
                    _settingText = false;
                }
                base.Text = value ?? string.Empty;
                RaiseChanged(Range);
            }
        }
        public override Font Font { get { return _font; } set { _font = value ?? _font; ApplyOptions(); } }
        public override Color BackColor { get { return _backColor; } set { _backColor = value; ApplyOptions(); } }
        public Brush BackBrush { get; set; }
        public bool ReadOnly { get { return _readOnly; } set { _readOnly = value; ApplyOptions(); } }
        public bool WordWrap { get { return _wordWrap; } set { _wordWrap = value; ApplyOptions(); } }
        public WordWrapMode WordWrapMode { get; set; }
        public bool ShowLineNumbers { get { return _showLineNumbers; } set { _showLineNumbers = value; ApplyOptions(); } }
        public int ReservedCountOfLineNumberChars { get; set; } = 1;
        public Color LineNumberColor { get { return _lineNumberColor; } set { _lineNumberColor = value; ApplyOptions(); } }
        public Color CurrentLineColor { get { return _currentLineColor; } set { _currentLineColor = value; ApplyOptions(); } }
        public Color SelectionColor { get { return _selectionColor; } set { _selectionColor = value; ApplyOptions(); } }
        public Color DisabledColor { get; set; } = Color.FromArgb(100, 180, 180, 180);
        public Color IndentBackColor { get; set; } = Color.WhiteSmoke;
        public Color ServiceLinesColor { get; set; } = Color.Silver;
        public Color FoldingIndicatorColor { get; set; } = Color.Green;
        public Color BookmarkColor { get; set; } = Color.PowderBlue;
        public Color CaretColor { get; set; } = Color.Black;
        public Color ChangedLineColor { get; set; } = Color.Transparent;
        public Color PaddingBackColor { get; set; } = Color.Transparent;
        public Color TextAreaBorderColor { get; set; } = Color.Black;
        public Padding Paddings { get; set; }
        public Language Language { get; set; }
        public string CommentPrefix { get; set; } = "//";
        public bool AutoIndent { get; set; } = true;
        public bool AutoIndentChars { get; set; } = true;
        public bool AutoCompleteBrackets { get; set; }
        public char[] AutoCompleteBracketsList { get; set; } = { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };
        public bool AcceptsTab { get; set; } = true;
        public bool AcceptsReturn { get; set; } = true;
        public int TabLength { get; set; } = 4;
        public int Zoom { get { return (int)_zoom; } set { _zoom = Math.Max(10, value); ApplyOptions(); } }
        public bool UndoEnabled { get { return _editor?.CanUndo ?? false; } }
        public bool RedoEnabled { get { return _editor?.CanRedo ?? false; } }
        public bool IsChanged { get { return _changed; } set { _changed = value; } }
        public bool Multiline { get; set; } = true;
        public bool HighlightFoldingIndicator { get; set; }
        public bool ShowFoldingLines { get; set; }
        public bool ShowScrollBars { get; set; } = true;
        public bool CaretBlinking { get; set; } = true;
        public bool VirtualSpace { get; set; }
        public int LeftPadding { get; set; }
        public int PreferredLineWidth { get; set; }
        public string DescriptionFile { get; set; }
        public HotkeysMapping HotkeysMapping { get; set; }
        public bool WordWrapAutoIndent { get; set; } = true;
        public int WordWrapIndent { get; set; }
        public bool SelectionHighlightingForLineBreaksEnabled { get; set; } = true;
        public bool ShowCaretWhenInactive { get; set; }
        public new EditorScroll VerticalScroll { get { return new EditorScroll(this, true); } }
        public new EditorScroll HorizontalScroll { get { return new EditorScroll(this, false); } }
        // The lines that are visible in the editor
        public Range VisibleRange
        {
            get
            {
                EnsureEditor();
                AvaloniaEdit.Rendering.TextView view = _editor.TextArea.TextView;
                int first = 0, last = Math.Max(0, LinesCount - 1);
                if (view.VisualLinesValid && view.VisualLines.Count > 0)
                {
                    first = view.VisualLines[0].FirstDocumentLine.LineNumber - 1;
                    last = view.VisualLines[view.VisualLines.Count - 1].LastDocumentLine.LineNumber - 1;
                }
                return new Range(this, 0, first, GetLineLength(last), last);
            }
        }
        public void UpdateScrollbars() { }
        public void OnVisibleRangeChanged() { VisibleRangeChanged?.Invoke(this, EventArgs.Empty); }
        public List<Style> GetStylesOfChar(Place place)
        {
            int position = PlaceToPosition(place);
            return _spans.Where(sp => !sp.Start.IsDeleted && !sp.End.IsDeleted && sp.Start.Offset <= position && sp.End.Offset > position)
                         .Select(sp => sp.Style).Distinct().ToList();
        }
        internal double ScrollOffsetY
        {
            get { EnsureEditor(); return _editor.TextArea.TextView.ScrollOffset.Y; }
            set { EnsureEditor(); _editor.ScrollToVerticalOffset(Math.Max(0, value)); }
        }
        internal double ScrollOffsetX
        {
            get { EnsureEditor(); return _editor.TextArea.TextView.ScrollOffset.X; }
            set { EnsureEditor(); _editor.ScrollToHorizontalOffset(Math.Max(0, value)); }
        }
        internal double ScrollExtentY { get { EnsureEditor(); return Math.Max(0, _editor.ExtentHeight - _editor.ViewportHeight); } }
        internal double ScrollExtentX { get { EnsureEditor(); return Math.Max(0, _editor.ExtentWidth - _editor.ViewportWidth); } }
        public Bookmarks Bookmarks { get; }
        public int LinesCount { get { return Document.LineCount; } }
        // The line of the editor (FastColoredTextBox has a list of characters with the text)
        public Line this[int iLine] { get { return new Line(GetLineText(iLine)); } }
        public void BookmarkLine(int iLine) { Bookmarks.Add(iLine); }
        public void UnbookmarkLine(int iLine) { Bookmarks.Remove(iLine); }
        public int TextLength { get { return Document.TextLength; } }
        public int CharHeight { get { return (int)Math.Ceiling(_font.GetHeight() * _zoom / 100); } }
        public int CharWidth { get { return (int)Math.Ceiling(_font.SizeInPixels * 0.6f * _zoom / 100); } }
        public IList<string> Lines
        {
            get
            {
                TextDocument d = Document;
                List<string> lines = new List<string>(d.LineCount);
                foreach (DocumentLine line in d.Lines) lines.Add(d.GetText(line.Offset, line.Length));
                return lines.AsReadOnly();
            }
        }
        public Range Range
        {
            get
            {
                int last = Math.Max(0, LinesCount - 1);
                return new Range(this, 0, 0, GetLineLength(last), last);
            }
        }
        internal Range SelectionRange { get { return _selection; } }
        public Range Selection
        {
            get
            {
                EnsureEditor();
                int start = _editor.SelectionStart, length = _editor.SelectionLength;
                int caret = _editor.CaretOffset;
                // The end of the selection is the caret like in FastColoredTextBox
                Place a = PositionToPlace(caret == start ? start + length : start);
                Place b = PositionToPlace(caret);
                if (length == 0) a = b;
                _selection = new Range(this, a, b);
                return _selection;
            }
            set
            {
                EnsureEditor();
                _selection = value;
                ApplySelection(value);
            }
        }
        public int SelectionStart
        {
            get { EnsureEditor(); return _editor.SelectionStart; }
            set { EnsureEditor(); _editor.Select(Math.Max(0, Math.Min(value, TextLength)), 0); }
        }
        public int SelectionLength
        {
            get { EnsureEditor(); return _editor.SelectionLength; }
            set { EnsureEditor(); _editor.Select(_editor.SelectionStart, Math.Max(0, Math.Min(value, TextLength - _editor.SelectionStart))); }
        }
        public string SelectedText
        {
            get { EnsureEditor(); return _editor.SelectedText; }
            set { EnsureEditor(); _editor.SelectedText = value ?? string.Empty; }
        }

        // Events
        public new event EventHandler<TextChangedEventArgs> TextChanged;
        public event EventHandler<TextChangedEventArgs> TextChangedDelayed;
        public event EventHandler<TextChangingEventArgs> TextChanging;
        public event EventHandler SelectionChanged;
        public event EventHandler SelectionChangedDelayed;
        public event EventHandler<KeyPressEventArgs> KeyPressing;
        public event EventHandler<KeyPressEventArgs> KeyPressed;
        public event EventHandler<AutoIndentEventArgs> AutoIndentNeeded;
        public event EventHandler VisibleRangeChanged;
        public event EventHandler ZoomChanged;
        public int DelayedEventsInterval { get; set; } = 100;
        public int DelayedTextChangedInterval { get; set; } = 100;

        public virtual void OnSelectionChanged() { SelectionChanged?.Invoke(this, EventArgs.Empty); }
        public virtual void OnTextChanged(Range r) { TextChanged?.Invoke(this, new TextChangedEventArgs(r)); }
        public virtual void OnTextChanging(ref string text)
        {
            TextChangingEventArgs e = new TextChangingEventArgs { InsertingText = text };
            TextChanging?.Invoke(this, e);
            if (e.Cancel) text = null;
            else text = e.InsertingText;
        }
        private bool _selectionPending;
        private void SelectionChangedNative()
        {
            if (_selectionPending) return;
            _selectionPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _selectionPending = false;
                Toolkit.Dispatch(OnSelectionChanged);
            }, DispatcherPriority.Background);
        }
        private int _changeStartLine = -1, _changeEndLine = -1;
        private void Document_Changing(object sender, DocumentChangeEventArgs e)
        {
            if (_settingText) return;
            if (TextChanging != null && _updating == 0)
            {
                string text = e.InsertedText?.Text ?? string.Empty;
                TextChangingEventArgs args = new TextChangingEventArgs { InsertingText = text };
                TextChanging(this, args);
                // A canceled change is undone after it is applied
                if (args.Cancel) _cancelChange = true;
            }
        }
        private bool _cancelChange;
        private void Document_Changed(object sender, DocumentChangeEventArgs e)
        {
            if (_cancelChange)
            {
                _cancelChange = false;
                Dispatcher.UIThread.Post(() => { if (_editor.CanUndo) _editor.Undo(); });
                return;
            }
            _changed = true;
            if (_settingText) return;
            TextDocument d = _editor.Document;
            int startLine = d.GetLineByOffset(Math.Min(e.Offset, d.TextLength)).LineNumber - 1;
            int endLine = d.GetLineByOffset(Math.Min(e.Offset + e.InsertionLength, d.TextLength)).LineNumber - 1;
            // Styles of the changed lines are removed like in FastColoredTextBox
            int lineStart = d.GetLineByNumber(startLine + 1).Offset;
            DocumentLine last = d.GetLineByNumber(endLine + 1);
            int lineEnd = last.Offset + last.Length;
            _spans.RemoveAll(s => s.Start.IsDeleted || s.End.IsDeleted || (s.Start.Offset < lineEnd && s.End.Offset > lineStart) || s.End.Offset <= s.Start.Offset);
            if (_autoUndo > 0 || _updating > 0)
            {
                _changeStartLine = _changeStartLine < 0 ? startLine : Math.Min(_changeStartLine, startLine);
                _changeEndLine = Math.Max(_changeEndLine, endLine);
                return;
            }
            RaiseChanged(new Range(this, 0, startLine, GetLineLength(endLine), endLine));
        }
        private void RaiseChanged(Range range)
        {
            Toolkit.Dispatch(() => OnTextChanged(range));
            _delayedRange = _delayedRange == null ? range : _delayedRange.GetUnionWith(range);
            if (_delayTimer == null)
            {
                _delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DelayedTextChangedInterval) };
                _delayTimer.Tick += (s, e) =>
                {
                    _delayTimer.Stop();
                    Range r = _delayedRange ?? Range;
                    _delayedRange = null;
                    Toolkit.Dispatch(() => TextChangedDelayed?.Invoke(this, new TextChangedEventArgs(r)));
                };
            }
            _delayTimer.Stop();
            _delayTimer.Start();
        }

        // Positions
        public int GetLineLength(int iLine)
        {
            TextDocument d = Document;
            if (iLine < 0 || iLine >= d.LineCount) return 0;
            return d.GetLineByNumber(iLine + 1).Length;
        }
        public int PlaceToPosition(Place point)
        {
            TextDocument d = Document;
            int line = Math.Max(0, Math.Min(point.iLine, d.LineCount - 1));
            DocumentLine l = d.GetLineByNumber(line + 1);
            return l.Offset + Math.Max(0, Math.Min(point.iChar, l.Length));
        }
        public Place PositionToPlace(int pos)
        {
            TextDocument d = Document;
            pos = Math.Max(0, Math.Min(pos, d.TextLength));
            DocumentLine l = d.GetLineByOffset(pos);
            return new Place(pos - l.Offset, l.LineNumber - 1);
        }
        public Point PlaceToPoint(Place place)
        {
            EnsureEditor();
            try
            {
                Avalonia.Point p = _editor.TextArea.TextView.GetVisualPosition(new TextViewPosition(place.iLine + 1, place.iChar + 1), VisualYPosition.LineBottom);
                Avalonia.Point view = _editor.TextArea.TextView.TranslatePoint(p - _editor.TextArea.TextView.ScrollOffset, _editor) ?? p;
                return new Point((int)view.X, (int)view.Y);
            }
            catch
            {
                return Point.Empty;
            }
        }
        public Point PositionToPoint(int pos) { return PlaceToPoint(PositionToPlace(pos)); }
        public Point GetPositionFromCharIndex(int pos) { return PositionToPoint(pos); }
        public Place PointToPlace(Point point)
        {
            EnsureEditor();
            // The text view has no positions before it is laid out
            TextViewPosition? p;
            try { p = _editor.GetPositionFromPoint(new Avalonia.Point(point.X, point.Y)); }
            catch (InvalidOperationException) { p = null; }
            return p == null ? Place.Empty : new Place(p.Value.Column - 1, p.Value.Line - 1);
        }
        public int PointToPosition(Point point) { return PlaceToPosition(PointToPlace(point)); }
        public Range GetRange(int fromPos, int toPos) { return new Range(this, PositionToPlace(fromPos), PositionToPlace(toPos)); }
        public Range GetRange(Place fromPlace, Place toPlace) { return new Range(this, fromPlace, toPlace); }
        public Range GetLine(int iLine) { return new Range(this, 0, iLine, GetLineLength(iLine), iLine); }
        public string GetLineText(int iLine) { return iLine >= 0 && iLine < LinesCount ? Lines[iLine] : string.Empty; }
        internal void ApplySelection(Range range)
        {
            int a = PlaceToPosition(range.Start), b = PlaceToPosition(range.End);
            int start = Math.Min(a, b), end = Math.Max(a, b);
            _editor.Select(start, end - start);
            _editor.CaretOffset = b;
        }

        // Styles
        internal void AddStyle(Style style, Range range)
        {
            int a = PlaceToPosition(range.Start), b = PlaceToPosition(range.End);
            if (a > b) (a, b) = (b, a);
            if (a == b) return;
            TextDocument d = Document;
            TextAnchor start = d.CreateAnchor(a);
            start.MovementType = AnchorMovementType.AfterInsertion;
            TextAnchor end = d.CreateAnchor(b);
            end.MovementType = AnchorMovementType.BeforeInsertion;
            _spans.Add(new StyleSpan { Style = style, Start = start, End = end });
            Redraw();
        }
        internal void ClearStyles(Range range, Style[] styles)
        {
            int a = PlaceToPosition(range.Start), b = PlaceToPosition(range.End);
            if (a > b) (a, b) = (b, a);
            _spans.RemoveAll(s => (styles == null || styles.Length == 0 || styles.Contains(s.Style)) && !s.Start.IsDeleted && !s.End.IsDeleted &&
                                  s.Start.Offset < Math.Max(b, a + 1) && s.End.Offset > a - (a == b ? 1 : 0));
            Redraw();
        }
        public void ClearStylesBuffer() { _spans.Clear(); Redraw(); }
        public void ClearStyle(StyleIndex styleIndex) { _spans.Clear(); Redraw(); }
        public int GetStyleIndex(Style style) { return 0; }
        public StyleIndex GetStyleIndexMask(Style[] styles) { return StyleIndex.All; }
        public int AddStyle(Style style) { return 0; }
        private bool _redrawPending;
        internal void Redraw()
        {
            if (_editor == null || _redrawPending) return;
            _redrawPending = true;
            Dispatcher.UIThread.Post(() =>
            {
                _redrawPending = false;
                _editor.TextArea.TextView.Redraw();
            }, DispatcherPriority.Render);
        }
        internal IEnumerable<StyleSpan> Spans { get { return _spans; } }

        private sealed class StyleColorizer : DocumentColorizingTransformer
        {
            private readonly FastColoredTextBox _owner;
            public StyleColorizer(FastColoredTextBox owner) { _owner = owner; }
            protected override void ColorizeLine(DocumentLine line)
            {
                int lineStart = line.Offset, lineEnd = line.EndOffset;
                foreach (StyleSpan span in _owner.Spans.ToArray())
                {
                    if (span.Start.IsDeleted || span.End.IsDeleted) continue;
                    int s = Math.Max(lineStart, span.Start.Offset), e = Math.Min(lineEnd, span.End.Offset);
                    if (e <= s) continue;
                    Style style = span.Style;
                    ChangeLinePart(s, e, element => style.Apply(element));
                }
            }
        }

        // Editing
        public virtual void InsertText(string text) { InsertText(text, true); }
        public virtual Range InsertText(string text, bool jumpToCaret)
        {
            EnsureEditor();
            text = text ?? string.Empty;
            int start = _editor.SelectionStart;
            _editor.Document.Replace(start, _editor.SelectionLength, text);
            _editor.Select(start + text.Length, 0);
            _editor.CaretOffset = start + text.Length;
            if (jumpToCaret) DoCaretVisible();
            return new Range(this, PositionToPlace(start), PositionToPlace(start + text.Length));
        }
        public virtual Range InsertText(string text, Style style) { Range r = InsertText(text, true); if (style != null) AddStyle(style, r); return r; }
        public virtual Range InsertText(string text, Style style, bool jumpToCaret) { Range r = InsertText(text, jumpToCaret); if (style != null) AddStyle(style, r); return r; }
        public virtual Range InsertTextAndRestoreSelection(Range replaceRange, string text, Style style)
        {
            Range old = Selection.Clone();
            Selection = replaceRange;
            Range r = InsertText(text, style, false);
            Selection = old;
            return r;
        }
        public void AppendText(string text) { AppendText(text, null); }
        public void AppendText(string text, Style style)
        {
            EnsureEditor();
            int start = _editor.Document.TextLength;
            _editor.Document.Insert(start, text ?? string.Empty);
            if (style != null) AddStyle(style, new Range(this, PositionToPlace(start), PositionToPlace(start + (text ?? "").Length)));
        }
        public void Clear() { Text = string.Empty; }
        public void ClearSelected()
        {
            EnsureEditor();
            if (_editor.SelectionLength > 0) _editor.SelectedText = string.Empty;
        }
        public void ClearCurrentLine()
        {
            Place caret = Selection.Start;
            Selection = new Range(this, 0, caret.iLine, GetLineLength(caret.iLine), caret.iLine);
            ClearSelected();
        }
        public void SelectAll() { EnsureEditor(); _editor.SelectAll(); }
        public virtual void Copy()
        {
            EnsureEditor();
            if (_editor.SelectionLength == 0) return;
            DataObject data = new DataObject();
            OnCreateClipboardData(data);
            Clipboard.SetDataObject(data, true);
        }
        protected virtual void OnCreateClipboardData(DataObject data)
        {
            data.SetData(DataFormats.UnicodeText, true, SelectedText);
        }
        protected virtual void CheckAndChangeSelectionType() { }
        public void Cut() { EnsureEditor(); _editor.Cut(); }
        public void Paste() { EnsureEditor(); _editor.Paste(); }
        public void Undo() { EnsureEditor(); _editor.Undo(); }
        public void Redo() { EnsureEditor(); _editor.Redo(); }
        public void ClearUndo()
        {
            EnsureEditor();
            ClearUndoStack();
        }
        private void ClearUndoStack()
        {
            // AvaloniaEdit cannot clear the stack while an undo group is open; it is cleared when the last group closes
            if (_updating > 0 || _autoUndo > 0) _clearUndoPending = true;
            else _editor.Document.UndoStack.ClearAll();
        }
        private bool _clearUndoPending;
        private void ClearPendingUndo()
        {
            if (!_clearUndoPending || _updating > 0 || _autoUndo > 0) return;
            _clearUndoPending = false;
            _editor.Document.UndoStack.ClearAll();
        }
        public void BeginUpdate()
        {
            EnsureEditor();
            if (_updating++ == 0) _editor.Document.BeginUpdate();
        }
        public void EndUpdate()
        {
            if (_updating == 0) return;
            if (--_updating == 0)
            {
                _editor.Document.EndUpdate();
                ClearPendingUndo();
                FlushGroupedChange();
            }
        }
        public void BeginAutoUndo()
        {
            EnsureEditor();
            if (_autoUndo++ == 0) _editor.Document.UndoStack.StartUndoGroup();
        }
        public void EndAutoUndo()
        {
            if (_autoUndo == 0) return;
            if (--_autoUndo == 0)
            {
                _editor.Document.UndoStack.EndUndoGroup();
                ClearPendingUndo();
                FlushGroupedChange();
            }
        }
        private void FlushGroupedChange()
        {
            if (_changeStartLine < 0) return;
            int s = Math.Min(_changeStartLine, LinesCount - 1), e = Math.Min(_changeEndLine, LinesCount - 1);
            _changeStartLine = _changeEndLine = -1;
            RaiseChanged(new Range(this, 0, Math.Max(0, s), GetLineLength(Math.Max(0, e)), Math.Max(0, e)));
        }
        public void DoCaretVisible() { if (_editor != null) _editor.TextArea.Caret.BringCaretToView(); }
        public void DoSelectionVisible() { DoCaretVisible(); }
        public void DoRangeVisible(Range range) { DoRangeVisible(range, false); }
        public void DoRangeVisible(Range range, bool tryToCentre)
        {
            EnsureEditor();
            _editor.ScrollTo(range.Start.iLine + 1, range.Start.iChar + 1);
        }
        public void Navigate(int iLine)
        {
            EnsureEditor();
            iLine = Math.Max(0, Math.Min(iLine, LinesCount - 1));
            Selection = new Range(this, 0, iLine, 0, iLine);
            _editor.ScrollToLine(iLine + 1);
        }
        public void ShowFindDialog() { ShowFindDialog(null); }
        public void ShowFindDialog(string findText)
        {
            EnsureEditor();
            SearchPanel panel = _editor.SearchPanel;
            if (panel == null) return;
            panel.IsReplaceMode = false;
            if (findText != null) panel.SearchPattern = findText;
            panel.Open();
        }
        public void ShowReplaceDialog() { ShowReplaceDialog(null); }
        public void ShowReplaceDialog(string findText)
        {
            EnsureEditor();
            SearchPanel panel = _editor.SearchPanel;
            if (panel == null) return;
            panel.IsReplaceMode = true;
            if (findText != null) panel.SearchPattern = findText;
            panel.Open();
        }
        public void ShowGoToDialog() { }
        public void IncreaseIndent()
        {
            BeginAutoUndo();
            Range sel = Selection;
            for (int i = sel.FromLine; i <= sel.ToLine; i++)
            {
                Selection = new Range(this, 0, i, 0, i);
                InsertText(new string(' ', TabLength), false);
            }
            EndAutoUndo();
        }
        public void DecreaseIndent()
        {
            BeginAutoUndo();
            Range sel = Selection;
            for (int i = sel.FromLine; i <= sel.ToLine; i++)
            {
                string line = GetLineText(i);
                int remove = 0;
                while (remove < TabLength && remove < line.Length && line[remove] == ' ') remove++;
                if (remove > 0)
                {
                    Selection = new Range(this, 0, i, remove, i);
                    ClearSelected();
                }
            }
            EndAutoUndo();
        }
        public void CommentSelected() { CommentSelected(CommentPrefix); }
        public void CommentSelected(string commentPrefix)
        {
            BeginAutoUndo();
            Range sel = Selection;
            for (int i = sel.FromLine; i <= sel.ToLine; i++)
            {
                Selection = new Range(this, 0, i, 0, i);
                InsertText(commentPrefix, false);
            }
            EndAutoUndo();
        }
        public void ExpandAllFoldingBlocks() { }
        public void CollapseAllFoldingBlocks() { }
        public void OnSyntaxHighlight(TextChangedEventArgs args) { }
        public void OnKeyPressing(KeyPressEventArgs args) { KeyPressing?.Invoke(this, args); }

        // Keyboard
        protected override bool IsInputKey(Keys keyData)
        {
            Keys key = keyData & Keys.KeyCode;
            if (key == Keys.Tab && AcceptsTab) return true;
            if (key == Keys.Enter && AcceptsReturn) return true;
            return true;
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            _keyDownProcessed = true;
            base.OnKeyDown(e);
            if (e.Handled) return;
            // A key of an editor action that is not in the mapping (or is mapped to no action) is not processed by the editor
            if (HotkeysMapping != null)
            {
                bool mapped = HotkeysMapping.TryGetValue(e.KeyData, out FCTBAction action);
                if ((mapped && action == FCTBAction.None) || (!mapped && DefaultMapping.ContainsKey(e.KeyData))) e.Handled = true;
                else if (mapped && action == FCTBAction.Copy) { Copy(); e.Handled = true; }
            }
        }
        // A derived class that does not call base.OnKeyDown keeps the key from the editor
        protected override void OnKeyDownCompleted(KeyEventArgs e)
        {
            if (!_keyDownProcessed) e.Handled = true;
            _keyDownProcessed = false;
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            KeyPressing?.Invoke(this, e);
            base.OnKeyPress(e);
            if (!e.Handled) KeyPressed?.Invoke(this, e);
        }
        public new bool Focus()
        {
            EnsureEditor();
            _editor.TextArea.Focus();
            return true;
        }
        public override bool Focused { get { return _editor != null && _editor.TextArea.IsKeyboardFocusWithin; } }
        protected override void Dispose(bool disposing)
        {
            _delayTimer?.Stop();
            base.Dispose(disposing);
        }
    }
}
