// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace UserControls
{
    public class PythonEditorTextBox : UserControl
    {
        // Variables                                                                                                                
        private const int IndentSize = 4;
        private const string IndentText = "    ";
        //
        private readonly PythonTextBoxSyntaxHighlighter _syntaxHighlighter;
        private readonly TextStyle _errorLineStyle;
        private readonly MarkerStyle _matchingBracketStyle;
        private PythonEditorFastTextBox _textBox;
        private AutocompleteMenu _autocompleteMenu;
        private bool _internalTextChange;
        private bool _lastKnownModified;
        private string _fileName = "";
        private int _errorLine = -1;
        private string _errorMessage;
        private bool _hasErrorDisplay;
        private EventHandler<PythonTextBoxAutocompleteEventArgs> _autocompleteProvider;
        // Autocomplete
        private string _lastCompletionExpression;
        private int _lastCompletionReplaceStartLine = -1;
        private int _lastCompletionReplaceStartColumn = -1;
        private List<string> _lastCompletions = new List<string>();
        private int _lastCompletionIndex = -1;
        // Context menu
        private ToolStripMenuItem _undoMenuItem;
        private ToolStripMenuItem _redoMenuItem;
        private ToolStripMenuItem _cutMenuItem;
        private ToolStripMenuItem _copyMenuItem;
        private ToolStripMenuItem _pasteMenuItem;
        private ToolStripMenuItem _deleteMenuItem;
        private ToolStripMenuItem _selectAllMenuItem;
        private ToolStripMenuItem _runMenuItem;
        private ToolStripMenuItem _runSelectionMenuItem;


        // Events                                                                                                                   
        public event EventHandler ModifiedChanged;
        public event EventHandler RunScriptRequested;
        public event EventHandler RunSelectionRequested;
        public event EventHandler<PythonTextBoxAutocompleteEventArgs> AutocompleteRequested;


        // Properties                                                                                                               
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ScriptText
        {
            get { return _textBox.Text; }
            set
            {
                _internalTextChange = true;
                try
                {
                    ClearErrorLine();
                    _textBox.Text = value ?? "";
                    _syntaxHighlighter.Highlight(_textBox.Range);
                    UpdateLineNumberMargin();
                    _textBox.ClearUndo();
                    _textBox.IsChanged = false;
                }
                finally
                {
                    _internalTextChange = false;
                }

                UpdateModifiedState();
            }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsModified
        {
            get { return _textBox.IsChanged; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CanUndo
        {
            get { return _textBox != null && _textBox.UndoEnabled; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool CanRedo
        {
            get { return _textBox != null && _textBox.RedoEnabled; }
        }
        [DefaultValue("")]
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value ?? ""; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedScriptText
        {
            get { return _textBox.SelectedText; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool HasSelection
        {
            get { return _textBox.SelectionLength > 0; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int SelectionStartLineNumber
        {
            get
            {
                int line = Math.Min(_textBox.Selection.Start.iLine, _textBox.Selection.End.iLine);
                return line + 1;
            }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ErrorMessage
        {
            get { return _errorMessage; }
        }
        public override string Text
        {
            get { return ScriptText; }
            set { ScriptText = value; }
        }
        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                if (_textBox != null) _textBox.Font = value;
                if (_autocompleteMenu != null) _autocompleteMenu.Font = value;
            }
        }
        [DefaultValue(BorderStyle.FixedSingle)]
        public new BorderStyle BorderStyle
        {
            get { return _textBox.BorderStyle; }
            set { _textBox.BorderStyle = value; }
        }
        [DefaultValue(false)]
        public bool WordWrap
        {
            get { return _textBox.WordWrap; }
            set { _textBox.WordWrap = value; }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ReadOnly
        {
            get { return _textBox.ReadOnly; }
            set { _textBox.ReadOnly = value; }
        }
        public new bool Focused
        {
            get { return _textBox.Focused; }
        }


        // Constructors                                                                                                             
        public PythonEditorTextBox()
        {
            _syntaxHighlighter = new PythonTextBoxSyntaxHighlighter();
            _errorLineStyle = new TextStyle(null, new SolidBrush(Color.FromArgb(255, 235, 235)), FontStyle.Regular);
            _matchingBracketStyle = new MarkerStyle(new SolidBrush(Color.FromArgb(80, Color.SteelBlue)));
            //
            InitializeComponent();
            InitializeEditorControl();
            ClearModifiedFlag();
        }


        // Methods                                                                                                                  
        private void InitializeComponent()
        {
            SuspendLayout();
            //
            _textBox = new PythonEditorFastTextBox(this);
            _textBox.Dock = DockStyle.Fill;
            _textBox.Name = "fctbPythonEditor";
            _textBox.TabIndex = 0;
            _textBox.Text = "#PrePoMax Python Script" + Environment.NewLine +
                            "#The PrePoMax object is available as 'pmx'" + Environment.NewLine;
            //
            Name = "PythonEditorTextBox";
            Size = new Size(400, 300);
            Controls.Add(_textBox);
            //
            ResumeLayout(false);
        }
        private void InitializeEditorControl()
        {
            _textBox.Language = Language.Custom;
            _textBox.ShowLineNumbers = true;
            _textBox.ReservedCountOfLineNumberChars = 3;
            _textBox.LineNumberColor = Color.Gray;
            _textBox.BookmarkColor = Color.DarkRed;
            _textBox.WordWrap = false;
            _textBox.AcceptsTab = true;
            _textBox.TabLength = IndentSize;
            _textBox.AutoIndent = false;
            _textBox.AutoCompleteBrackets = true;
            _textBox.AutoCompleteBracketsList = new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };
            _textBox.Font = new Font("Consolas", 10.0f);
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            _textBox.CurrentLineColor = Color.FromArgb(35, Color.LightSteelBlue);
            _textBox.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            _textBox.CommentPrefix = "#";
            _textBox.ContextMenuStrip = CreateContextMenu();
            //
            _autocompleteMenu = PythonTextBoxAutocompleteMenuHelper.Create(_textBox);
            //
            _textBox.TextChanged += PythonEditorTextBox_TextChanged;
            _textBox.TextChangedDelayed += PythonEditorTextBox_TextChangedDelayed;
            _textBox.Leave += PythonEditorTextBox_Leave;
        }
        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            _undoMenuItem = new ToolStripMenuItem("Undo");
            _redoMenuItem = new ToolStripMenuItem("Redo");
            _cutMenuItem = new ToolStripMenuItem("Cut");
            _copyMenuItem = new ToolStripMenuItem("Copy");
            _pasteMenuItem = new ToolStripMenuItem("Paste");
            _deleteMenuItem = new ToolStripMenuItem("Delete");
            _selectAllMenuItem = new ToolStripMenuItem("Select All");
            _runMenuItem = new ToolStripMenuItem("Run");
            _runSelectionMenuItem = new ToolStripMenuItem("Run Selection");

            _undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
            _redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
            _cutMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            _copyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            _pasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            _selectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;
            _runMenuItem.ShortcutKeys = Keys.F5;
            _runSelectionMenuItem.ShortcutKeys = Keys.F9;

            _undoMenuItem.Click += ContextMenuUndo_Click;
            _redoMenuItem.Click += ContextMenuRedo_Click;
            _cutMenuItem.Click += ContextMenuCut_Click;
            _copyMenuItem.Click += ContextMenuCopy_Click;
            _pasteMenuItem.Click += ContextMenuPaste_Click;
            _deleteMenuItem.Click += ContextMenuDelete_Click;
            _selectAllMenuItem.Click += ContextMenuSelectAll_Click;
            _runMenuItem.Click += ContextMenuRun_Click;
            _runSelectionMenuItem.Click += ContextMenuRunSelection_Click;

            _undoMenuItem.Image = Properties.Resources.Undo;
            _redoMenuItem.Image = Properties.Resources.Redo;
            _runMenuItem.Image = Properties.Resources.Run;
            _runSelectionMenuItem.Image = Properties.Resources.RunSelected;

            menu.Items.Add(_undoMenuItem);
            menu.Items.Add(_redoMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_cutMenuItem);
            menu.Items.Add(_copyMenuItem);
            menu.Items.Add(_pasteMenuItem);
            menu.Items.Add(_deleteMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_selectAllMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_runMenuItem);
            menu.Items.Add(_runSelectionMenuItem);

            menu.Opening += ContextMenu_Opening;

            return menu;
        }
        public new bool Focus()
        {
            return _textBox.Focus();
        }
        public void SetPythonContext(EventHandler<PythonTextBoxAutocompleteEventArgs> autocompleteProvider)
        {
            if (_autocompleteProvider != null)
                AutocompleteRequested -= _autocompleteProvider;

            _autocompleteProvider = autocompleteProvider;

            if (_autocompleteProvider != null)
                AutocompleteRequested += _autocompleteProvider;
        }
        public void ShowFind()
        {
            _textBox.ShowFindDialog();
        }
        public void ShowReplace()
        {
            _textBox.ShowReplaceDialog();
        }
        public void Undo()
        {
            if (CanUndo) _textBox.Undo();
        }
        public void Redo()
        {
            if (CanRedo) _textBox.Redo();
        }
        public void RunScript()
        {
            RunScriptRequested?.Invoke(this, EventArgs.Empty);
        }
        public void RunSelection()
        {
            RunSelectionRequested?.Invoke(this, EventArgs.Empty);
        }
        public void GoToLine(int lineNumber)
        {
            if (lineNumber < 1) lineNumber = 1;
            if (lineNumber > _textBox.LinesCount) lineNumber = _textBox.LinesCount;

            int lineIndex = lineNumber - 1;
            _textBox.Navigate(lineIndex);
            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, 0, lineIndex, 0, lineIndex);
            _textBox.DoCaretVisible();
        }
        public void SelectLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > _textBox.LinesCount) return;

            int lineIndex = lineNumber - 1;
            int lineLength = _textBox.Lines[lineIndex].Length;

            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, 0, lineIndex, lineLength, lineIndex);
            _textBox.DoSelectionVisible();
        }
        public void ClearModifiedFlag()
        {
            if (_textBox == null) return;

            _textBox.IsChanged = false;
            UpdateModifiedState();
        }
        public void SetErrorLine(int lineNumber, string message)
        {
            ClearErrorLine();

            if (lineNumber < 1 || lineNumber > _textBox.LinesCount) return;

            _errorLine = lineNumber - 1;
            _errorMessage = message ?? "";
            _textBox.BookmarkLine(_errorLine);
            ApplyErrorLineStyle();
            _hasErrorDisplay = true;
            _textBox.Invalidate();
        }
        public void ClearErrorLine()
        {
            if (!_hasErrorDisplay && _errorLine < 0)
            {
                _errorMessage = null;
                return;
            }

            if (_textBox.Bookmarks != null)
                _textBox.Bookmarks.Clear();

            ClearErrorLineStyles();

            _errorLine = -1;
            _errorMessage = null;
            _hasErrorDisplay = false;
            _textBox.Invalidate();
        }
        private void ClearErrorLineStyles()
        {
            for (int i = 0; i < _textBox.LinesCount; i++)
            {
                int lineLength = _textBox.Lines[i].Length;
                if (lineLength == 0) continue;

                FastColoredTextBoxNS.Range range =
                    new FastColoredTextBoxNS.Range(_textBox, 0, i, lineLength, i);
                range.ClearStyle(_errorLineStyle);
            }
        }
        internal bool ProcessKeyDown(KeyEventArgs e)
        {
            if (e.Alt) return false;

            if (IsAutocompleteMenuVisible() && IsAutocompleteMenuKey(e.KeyCode))
                return false;

            if (IsAutocompleteMenuVisible() && TryAutocompleteCommitAndInsert(e))
                return true;

            if (IsAutocompleteMenuVisible() && IsAutocompletePassThroughKey(e))
                return false;

            if (e.KeyCode == Keys.F5)
            {
                RunScript();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.KeyCode == Keys.F9)
            {
                RunSelection();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.Space)
            {
                ShowAutocompleteList(true);
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowFind();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.H)
            {
                ShowReplace();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.G)
            {
                ShowGoToLineDialog();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && (e.KeyCode == Keys.OemQuestion || e.KeyCode == Keys.Divide))
            {
                ToggleCommentSelectedLines();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Control && e.KeyCode == Keys.V)
            {
                PasteFromClipboard();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.Shift && e.KeyCode == Keys.Insert)
            {
                PasteFromClipboard();
                e.SuppressKeyPress = true;
                return true;
            }

            if (e.KeyCode == Keys.Tab)
            {
                if (e.Shift)
                    UnindentSelectedLines();
                else if (HasSelection)
                    IndentSelectedLines();
                else if (!CompleteCurrentInput())
                    _textBox.InsertText(IndentText);

                e.SuppressKeyPress = true;
                return true;
            }

            if (e.KeyCode == Keys.Enter)
            {
                InsertNewLineWithIndent();
                e.SuppressKeyPress = true;
                return true;
            }

            return false;
        }
        internal bool ProcessCommandKey(Keys keyData)
        {
            if ((keyData & Keys.Alt) == Keys.Alt)
                return false;

            if ((keyData & Keys.KeyCode) == Keys.F &&
                (keyData & (Keys.Control | Keys.Alt | Keys.Shift)) == Keys.Control)
            {
                ShowFind();
                return true;
            }

            return false;
        }
        internal void ProcessKeyPress(KeyPressEventArgs e)
        {
            if (_internalTextChange || _textBox.ReadOnly) return;

            if (IsAutocompleteMenuVisible() && IsAutocompleteCommitAndContinueCharacter(e.KeyChar))
                _autocompleteMenu.OnSelecting();

            if (IsAutocompleteTriggerCharacter(e.KeyChar) && IsHandleCreated)
                BeginInvoke(new MethodInvoker(ShowAutocompleteListAfterTextInput));
            else if (!char.IsControl(e.KeyChar) && IsHandleCreated)
                BeginInvoke(new MethodInvoker(HideAutocompleteList));
        }
        internal void ProcessSelectionChanged()
        {
            HighlightMatchingBrackets();
        }
        private void PythonEditorTextBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            if (!_internalTextChange)
                ClearErrorLine();

            UpdateModifiedState();
        }
        private void PythonEditorTextBox_TextChangedDelayed(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            _syntaxHighlighter.Highlight(e.ChangedRange);
            UpdateLineNumberMargin();

            if (IsAutocompleteMenuVisible())
                ShowAutocompleteList(false);
        }
        private void PythonEditorTextBox_Leave(object sender, EventArgs e)
        {
            if (!IsAutocompleteMenuVisible())
                HideAutocompleteList();
        }
        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            bool hasSelection = HasSelection;

            _undoMenuItem.Enabled = _textBox.UndoEnabled;
            _redoMenuItem.Enabled = _textBox.RedoEnabled;
            _cutMenuItem.Enabled = hasSelection && !_textBox.ReadOnly;
            _copyMenuItem.Enabled = hasSelection;
            _pasteMenuItem.Enabled = Clipboard.ContainsText() && !_textBox.ReadOnly;
            _deleteMenuItem.Enabled = hasSelection && !_textBox.ReadOnly;
            _selectAllMenuItem.Enabled = _textBox.TextLength > 0;
            _runMenuItem.Enabled = !string.IsNullOrWhiteSpace(ScriptText);
            _runSelectionMenuItem.Enabled = hasSelection;
        }
        private void ContextMenuUndo_Click(object sender, EventArgs e)
        {
            Undo();
        }
        private void ContextMenuRedo_Click(object sender, EventArgs e)
        {
            Redo();
        }
        private void ContextMenuCut_Click(object sender, EventArgs e)
        {
            if (HasSelection) _textBox.Cut();
        }
        private void ContextMenuCopy_Click(object sender, EventArgs e)
        {
            if (HasSelection) _textBox.Copy();
        }
        private void ContextMenuPaste_Click(object sender, EventArgs e)
        {
            PasteFromClipboard();
        }
        private void ContextMenuDelete_Click(object sender, EventArgs e)
        {
            if (HasSelection) _textBox.ClearSelected();
        }
        private void ContextMenuSelectAll_Click(object sender, EventArgs e)
        {
            _textBox.SelectAll();
        }
        private void ContextMenuRun_Click(object sender, EventArgs e)
        {
            RunScript();
        }
        private void ContextMenuRunSelection_Click(object sender, EventArgs e)
        {
            RunSelection();
        }
        private void UpdateModifiedState()
        {
            bool isModified = IsModified;
            if (_lastKnownModified == isModified) return;

            _lastKnownModified = isModified;
            ModifiedChanged?.Invoke(this, EventArgs.Empty);
        }
        private void UpdateLineNumberMargin()
        {
            int digits = Math.Max(3, _textBox.LinesCount.ToString().Length);
            if (_textBox.ReservedCountOfLineNumberChars != digits)
                _textBox.ReservedCountOfLineNumberChars = digits;
        }
        private void PasteFromClipboard()
        {
            if (_textBox.ReadOnly) return;
            if (!Clipboard.ContainsText()) return;

            InsertNormalizedText(Clipboard.GetText());
        }
        private void InsertNormalizedText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _textBox.InsertText(text.Replace("\t", IndentText));
        }
        private void InsertNewLineWithIndent()
        {
            Place caret = _textBox.Selection.Start;
            if (caret.iLine < 0 || caret.iLine >= _textBox.LinesCount)
            {
                _textBox.InsertText(Environment.NewLine);
                return;
            }

            string line = _textBox.Lines[caret.iLine];
            int column = Math.Min(caret.iChar, line.Length);
            string beforeCaret = line.Substring(0, column);
            string indent = GetLeadingWhitespace(line);

            if (beforeCaret.TrimEnd().EndsWith(":"))
                indent += IndentText;

            _textBox.InsertText(Environment.NewLine + indent);
        }
        private string GetLeadingWhitespace(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";

            int count = 0;
            while (count < line.Length && (line[count] == ' ' || line[count] == '\t'))
                count++;

            return line.Substring(0, count).Replace("\t", IndentText);
        }
        private void GetSelectedLineRange(out int firstLine, out int lastLine)
        {
            Place start = _textBox.Selection.Start;
            Place end = _textBox.Selection.End;

            firstLine = Math.Min(start.iLine, end.iLine);
            lastLine = Math.Max(start.iLine, end.iLine);

            if (HasSelection && end.iChar == 0 && lastLine > firstLine)
                lastLine--;
        }
        private void IndentSelectedLines()
        {
            int firstLine;
            int lastLine;
            GetSelectedLineRange(out firstLine, out lastLine);

            Place originalStart = _textBox.Selection.Start;
            Place originalEnd = _textBox.Selection.End;

            _textBox.BeginAutoUndo();
            try
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, 0, i, 0, i);
                    _textBox.InsertText(IndentText);
                }
            }
            finally
            {
                _textBox.EndAutoUndo();
            }

            RestoreSelectionAfterLinePrefixChange(originalStart, originalEnd, firstLine, lastLine, IndentSize);
        }
        private void UnindentSelectedLines()
        {
            int firstLine;
            int lastLine;
            GetSelectedLineRange(out firstLine, out lastLine);

            Place originalStart = _textBox.Selection.Start;
            Place originalEnd = _textBox.Selection.End;
            Dictionary<int, int> removedByLine = new Dictionary<int, int>();

            _textBox.BeginAutoUndo();
            try
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    int removeCount = GetUnindentCount(_textBox.Lines[i]);
                    removedByLine[i] = removeCount;

                    if (removeCount == 0) continue;

                    _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, 0, i, removeCount, i);
                    _textBox.ClearSelected();
                }
            }
            finally
            {
                _textBox.EndAutoUndo();
            }

            RestoreSelectionAfterLineUnindent(originalStart, originalEnd, firstLine, lastLine, removedByLine);
        }
        private int GetUnindentCount(string line)
        {
            if (string.IsNullOrEmpty(line)) return 0;
            if (line[0] == '\t') return 1;

            int count = 0;
            while (count < line.Length && count < IndentSize && line[count] == ' ')
                count++;

            return count;
        }
        private void RestoreSelectionAfterLinePrefixChange(Place originalStart, Place originalEnd,
                                                           int firstLine, int lastLine, int delta)
        {
            Place start = AdjustPlaceByDelta(originalStart, firstLine, lastLine, delta);
            Place end = AdjustPlaceByDelta(originalEnd, firstLine, lastLine, delta);
            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, start, end);
        }
        private void RestoreSelectionAfterLineUnindent(Place originalStart, Place originalEnd,
                                                       int firstLine, int lastLine, Dictionary<int, int> removedByLine)
        {
            Place start = AdjustPlaceByRemovedPrefix(originalStart, firstLine, lastLine, removedByLine);
            Place end = AdjustPlaceByRemovedPrefix(originalEnd, firstLine, lastLine, removedByLine);
            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, start, end);
        }
        private Place AdjustPlaceByDelta(Place place, int firstLine, int lastLine, int delta)
        {
            if (place.iLine < firstLine || place.iLine > lastLine)
                return place;

            return new Place(place.iChar + delta, place.iLine);
        }
        private Place AdjustPlaceByRemovedPrefix(Place place, int firstLine, int lastLine,
                                                 Dictionary<int, int> removedByLine)
        {
            if (place.iLine < firstLine || place.iLine > lastLine)
                return place;

            int removed;
            if (!removedByLine.TryGetValue(place.iLine, out removed) || removed == 0)
                return place;

            return new Place(Math.Max(0, place.iChar - removed), place.iLine);
        }
        private void ToggleCommentSelectedLines()
        {
            int firstLine;
            int lastLine;
            GetSelectedLineRange(out firstLine, out lastLine);

            bool uncomment = AreSelectedLinesCommented(firstLine, lastLine);

            if (uncomment)
                UncommentLines(firstLine, lastLine);
            else
                CommentLines(firstLine, lastLine);
        }
        private bool AreSelectedLinesCommented(int firstLine, int lastLine)
        {
            bool foundCodeLine = false;

            for (int i = firstLine; i <= lastLine; i++)
            {
                string line = _textBox.Lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                foundCodeLine = true;
                int index = GetFirstNonWhitespaceIndex(line);
                if (index >= line.Length || line[index] != '#')
                    return false;
            }

            return foundCodeLine;
        }
        private void CommentLines(int firstLine, int lastLine)
        {
            Place originalStart = _textBox.Selection.Start;
            Place originalEnd = _textBox.Selection.End;

            _textBox.BeginAutoUndo();
            try
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    string line = _textBox.Lines[i];
                    int insertColumn = string.IsNullOrWhiteSpace(line) ? 0 : GetFirstNonWhitespaceIndex(line);
                    _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, insertColumn, i, insertColumn, i);
                    _textBox.InsertText("# ");
                }
            }
            finally
            {
                _textBox.EndAutoUndo();
            }

            RestoreSelectionAfterLinePrefixChange(originalStart, originalEnd, firstLine, lastLine, 2);
        }
        private void UncommentLines(int firstLine, int lastLine)
        {
            Place originalStart = _textBox.Selection.Start;
            Place originalEnd = _textBox.Selection.End;
            Dictionary<int, int> removedByLine = new Dictionary<int, int>();

            _textBox.BeginAutoUndo();
            try
            {
                for (int i = firstLine; i <= lastLine; i++)
                {
                    string line = _textBox.Lines[i];
                    int index = GetFirstNonWhitespaceIndex(line);
                    int removeCount = 0;

                    if (index < line.Length && line[index] == '#')
                    {
                        removeCount = 1;
                        if (index + 1 < line.Length && line[index + 1] == ' ')
                            removeCount = 2;

                        _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox, index, i, index + removeCount, i);
                        _textBox.ClearSelected();
                    }

                    removedByLine[i] = removeCount;
                }
            }
            finally
            {
                _textBox.EndAutoUndo();
            }

            RestoreSelectionAfterLineUnindent(originalStart, originalEnd, firstLine, lastLine, removedByLine);
        }
        private int GetFirstNonWhitespaceIndex(string line)
        {
            int index = 0;
            while (index < line.Length && (line[index] == ' ' || line[index] == '\t'))
                index++;

            return index;
        }
        private void ShowGoToLineDialog()
        {
            using (Form form = new Form())
            using (Label label = new Label())
            using (NumericUpDown numeric = new NumericUpDown())
            using (Button okButton = new Button())
            using (Button cancelButton = new Button())
            {
                form.Text = "Go To Line";
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ClientSize = new Size(220, 88);

                label.Text = "Line:";
                label.AutoSize = true;
                label.Location = new Point(12, 16);

                numeric.Minimum = 1;
                numeric.Maximum = Math.Max(1, _textBox.LinesCount);
                numeric.Value = Math.Min(numeric.Maximum, Math.Max(1, _textBox.Selection.Start.iLine + 1));
                numeric.Location = new Point(58, 12);
                numeric.Width = 145;

                okButton.Text = "OK";
                okButton.DialogResult = DialogResult.OK;
                okButton.Location = new Point(47, 52);
                okButton.Width = 75;

                cancelButton.Text = "Cancel";
                cancelButton.DialogResult = DialogResult.Cancel;
                cancelButton.Location = new Point(128, 52);
                cancelButton.Width = 75;

                form.Controls.Add(label);
                form.Controls.Add(numeric);
                form.Controls.Add(okButton);
                form.Controls.Add(cancelButton);
                form.AcceptButton = okButton;
                form.CancelButton = cancelButton;

                if (form.ShowDialog(this) == DialogResult.OK)
                    GoToLine((int)numeric.Value);
            }
        }
        private void ApplyErrorLineStyle()
        {
            if (_errorLine < 0 || _errorLine >= _textBox.LinesCount) return;

            int lineLength = _textBox.Lines[_errorLine].Length;
            if (lineLength == 0) return;

            FastColoredTextBoxNS.Range range =
                new FastColoredTextBoxNS.Range(_textBox, 0, _errorLine, lineLength, _errorLine);
            range.SetStyle(_errorLineStyle);
        }
        private void HighlightMatchingBrackets()
        {
            if (_textBox == null) return;

            _textBox.Range.ClearStyle(_matchingBracketStyle);

            Place bracketPlace;
            char bracket;
            if (!TryGetBracketNearCaret(out bracketPlace, out bracket)) return;

            char openBracket;
            char closeBracket;
            bool forward;
            if (!GetBracketPair(bracket, out openBracket, out closeBracket, out forward)) return;

            Place matchingPlace;
            if (!FindMatchingBracket(bracketPlace, openBracket, closeBracket, forward, out matchingPlace)) return;

            HighlightSingleCharacter(bracketPlace);
            HighlightSingleCharacter(matchingPlace);
        }
        private bool TryGetBracketNearCaret(out Place place, out char bracket)
        {
            Place caret = _textBox.Selection.Start;

            if (TryGetCharacterAt(caret, out bracket) && IsBracketCharacter(bracket))
            {
                place = caret;
                return true;
            }

            if (caret.iChar > 0)
            {
                Place previous = new Place(caret.iChar - 1, caret.iLine);
                if (TryGetCharacterAt(previous, out bracket) && IsBracketCharacter(bracket))
                {
                    place = previous;
                    return true;
                }
            }

            place = Place.Empty;
            bracket = '\0';
            return false;
        }
        private bool TryGetCharacterAt(Place place, out char character)
        {
            character = '\0';

            if (place.iLine < 0 || place.iLine >= _textBox.LinesCount) return false;

            string line = _textBox.Lines[place.iLine];
            if (place.iChar < 0 || place.iChar >= line.Length) return false;

            character = line[place.iChar];
            return true;
        }
        private bool IsBracketCharacter(char c)
        {
            return c == '(' || c == ')' || c == '[' || c == ']' || c == '{' || c == '}';
        }
        private bool GetBracketPair(char bracket, out char openBracket, out char closeBracket, out bool forward)
        {
            forward = true;

            if (bracket == '(' || bracket == ')')
            {
                openBracket = '(';
                closeBracket = ')';
                forward = bracket == '(';
                return true;
            }

            if (bracket == '[' || bracket == ']')
            {
                openBracket = '[';
                closeBracket = ']';
                forward = bracket == '[';
                return true;
            }

            if (bracket == '{' || bracket == '}')
            {
                openBracket = '{';
                closeBracket = '}';
                forward = bracket == '{';
                return true;
            }

            openBracket = '\0';
            closeBracket = '\0';
            return false;
        }
        private bool FindMatchingBracket(Place bracketPlace, char openBracket, char closeBracket,
                                         bool forward, out Place matchingPlace)
        {
            int depth = 0;

            if (forward)
            {
                for (int lineIndex = bracketPlace.iLine; lineIndex < _textBox.LinesCount; lineIndex++)
                {
                    string line = _textBox.Lines[lineIndex];
                    int column = lineIndex == bracketPlace.iLine ? bracketPlace.iChar : 0;

                    for (int i = column; i < line.Length; i++)
                    {
                        char c = line[i];
                        if (c == openBracket) depth++;
                        else if (c == closeBracket)
                        {
                            depth--;
                            if (depth == 0)
                            {
                                matchingPlace = new Place(i, lineIndex);
                                return true;
                            }
                        }
                    }
                }
            }
            else
            {
                for (int lineIndex = bracketPlace.iLine; lineIndex >= 0; lineIndex--)
                {
                    string line = _textBox.Lines[lineIndex];
                    int column = lineIndex == bracketPlace.iLine ? bracketPlace.iChar : line.Length - 1;

                    for (int i = column; i >= 0; i--)
                    {
                        char c = line[i];
                        if (c == closeBracket) depth++;
                        else if (c == openBracket)
                        {
                            depth--;
                            if (depth == 0)
                            {
                                matchingPlace = new Place(i, lineIndex);
                                return true;
                            }
                        }
                    }
                }
            }

            matchingPlace = Place.Empty;
            return false;
        }
        private void HighlightSingleCharacter(Place place)
        {
            FastColoredTextBoxNS.Range range =
                new FastColoredTextBoxNS.Range(_textBox, place.iChar, place.iLine, place.iChar + 1, place.iLine);
            range.SetStyle(_matchingBracketStyle);
        }
        private bool CompleteCurrentInput()
        {
            string expression;
            string prefix;
            int replaceStartColumn;
            int lineIndex;
            List<string> localCompletions;

            if (!GetCompletionContext(out expression, out prefix, out replaceStartColumn, out lineIndex,
                                      out localCompletions))
                return false;

            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(expression)) return false;

            Place caret = _textBox.Selection.Start;
            if (caret.iLine < 0 || caret.iLine >= _textBox.LinesCount) return false;

            string line = _textBox.Lines[caret.iLine];
            int column = Math.Min(caret.iChar, line.Length);

            bool continueCycling = CanContinueCompletionCycle(line,
                                                               lineIndex,
                                                               column,
                                                               expression,
                                                               replaceStartColumn);

            if (!continueCycling)
            {
                List<string> completions = GetCompletions(expression, prefix);
                if (string.IsNullOrEmpty(expression))
                    AddCompletions(completions, localCompletions);

                if (completions == null || completions.Count == 0)
                {
                    ResetAutocomplete();
                    return false;
                }

                completions.Sort(StringComparer.OrdinalIgnoreCase);

                _lastCompletionExpression = expression;
                _lastCompletionReplaceStartLine = lineIndex;
                _lastCompletionReplaceStartColumn = replaceStartColumn;
                _lastCompletions = completions;
                _lastCompletionIndex = 0;
            }
            else
            {
                _lastCompletionIndex++;

                if (_lastCompletionIndex >= _lastCompletions.Count)
                    _lastCompletionIndex = 0;
            }

            string selectedCompletion = _lastCompletions[_lastCompletionIndex];
            int replaceLength = column - _lastCompletionReplaceStartColumn;

            ReplaceLineRange(_lastCompletionReplaceStartLine,
                             _lastCompletionReplaceStartColumn,
                             replaceLength,
                             selectedCompletion);

            return true;
        }
        private bool CanContinueCompletionCycle(string line, int lineIndex, int column,
                                                string expression, int replaceStartColumn)
        {
            if (_lastCompletions == null || _lastCompletions.Count == 0) return false;
            if (_lastCompletionReplaceStartLine < 0 || _lastCompletionReplaceStartColumn < 0) return false;
            if (_lastCompletionIndex < 0) return false;

            if (_lastCompletionExpression != expression) return false;
            if (_lastCompletionReplaceStartLine != lineIndex) return false;
            if (_lastCompletionReplaceStartColumn != replaceStartColumn) return false;
            if (column < _lastCompletionReplaceStartColumn) return false;

            string currentCompletedText = line.Substring(_lastCompletionReplaceStartColumn,
                                                         column - _lastCompletionReplaceStartColumn);

            for (int i = 0; i < _lastCompletions.Count; i++)
            {
                if (string.Equals(currentCompletedText,
                                  _lastCompletions[i],
                                  StringComparison.Ordinal))
                {
                    _lastCompletionIndex = i;
                    return true;
                }
            }

            return false;
        }
        private void ReplaceLineRange(int lineIndex, int startColumn, int length, string newText)
        {
            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox,
                                                                startColumn,
                                                                lineIndex,
                                                                startColumn + length,
                                                                lineIndex);
            _textBox.InsertText(newText);
        }
        private void ResetAutocomplete()
        {
            _lastCompletionExpression = null;
            _lastCompletionReplaceStartLine = -1;
            _lastCompletionReplaceStartColumn = -1;
            _lastCompletions.Clear();
            _lastCompletionIndex = -1;
        }
        private void ShowAutocompleteListAfterTextInput()
        {
            if (IsDisposed || _textBox == null || !_textBox.Focused) return;

            ShowAutocompleteList(false);
        }
        private void ShowAutocompleteList(bool forced)
        {
            string expression;
            string prefix;
            int replaceStartColumn;
            int lineIndex;
            List<string> localCompletions;

            if (!GetCompletionContext(out expression, out prefix, out replaceStartColumn, out lineIndex,
                                      out localCompletions))
            {
                HideAutocompleteList();
                return;
            }

            if (!forced && string.IsNullOrEmpty(expression) && string.IsNullOrEmpty(prefix))
            {
                HideAutocompleteList();
                return;
            }

            List<string> completions = GetCompletions(expression, prefix);
            if (string.IsNullOrEmpty(expression))
                AddCompletions(completions, localCompletions);

            if (completions == null || completions.Count == 0)
            {
                HideAutocompleteList();
                return;
            }

            completions.Sort(StringComparer.OrdinalIgnoreCase);

            PythonTextBoxAutocompleteMenuHelper.ShowPrepared(_autocompleteMenu, completions);
            _textBox.Focus();
        }
        private bool GetCompletionContext(out string expression, out string prefix,
                                          out int replaceStartColumn, out int lineIndex,
                                          out List<string> localCompletions)
        {
            expression = "";
            prefix = "";
            replaceStartColumn = 0;
            lineIndex = -1;
            localCompletions = new List<string>();

            Place caret = _textBox.Selection.Start;
            if (caret.iLine < 0 || caret.iLine >= _textBox.LinesCount) return false;

            string line = _textBox.Lines[caret.iLine];
            int column = Math.Min(caret.iChar, line.Length);
            string beforeCaret = line.Substring(0, column);

            PythonTextBoxCompletionHelper.GetCompletionExpression(beforeCaret,
                                                                  out expression,
                                                                  out prefix,
                                                                  out replaceStartColumn);

            PythonTextBoxScriptAnalysis analysis =
                PythonTextBoxScriptAnalyzer.Analyze(GetTextBeforeCaret(caret, column));

            expression = analysis.ResolveExpression(expression);
            localCompletions = analysis.GetMatchingNames(prefix);

            lineIndex = caret.iLine;
            return true;
        }
        private string GetTextBeforeCaret(Place caret, int column)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < caret.iLine; i++)
                sb.AppendLine(_textBox.Lines[i]);

            if (caret.iLine >= 0 && caret.iLine < _textBox.LinesCount)
            {
                string line = _textBox.Lines[caret.iLine];
                column = Math.Min(column, line.Length);
                sb.Append(line.Substring(0, column));
            }

            return sb.ToString();
        }
        private List<string> GetCompletions(string expression, string prefix)
        {
            PythonTextBoxAutocompleteEventArgs args =
                new PythonTextBoxAutocompleteEventArgs(expression, prefix);

            EventHandler<PythonTextBoxAutocompleteEventArgs> handler = AutocompleteRequested;

            if (handler != null)
                handler(this, args);

            return args.Completions;
        }
        private void AddCompletions(List<string> completions, List<string> additionalCompletions)
        {
            if (completions == null || additionalCompletions == null) return;

            HashSet<string> existing = new HashSet<string>(completions, StringComparer.OrdinalIgnoreCase);

            foreach (string completion in additionalCompletions)
            {
                if (existing.Add(completion))
                    completions.Add(completion);
            }
        }
        private void HideAutocompleteList()
        {
            if (_autocompleteMenu == null) return;

            _autocompleteMenu.Close();
            PythonTextBoxAutocompleteMenuHelper.ClearAutocompleteItems(_autocompleteMenu);
        }
        private bool IsAutocompleteMenuVisible()
        {
            return _autocompleteMenu != null && _autocompleteMenu.Visible;
        }
        private bool IsAutocompleteMenuKey(Keys keyCode)
        {
            return keyCode == Keys.Enter ||
                   keyCode == Keys.Tab ||
                   keyCode == Keys.Escape ||
                   keyCode == Keys.Up ||
                   keyCode == Keys.Down ||
                   keyCode == Keys.PageUp ||
                   keyCode == Keys.PageDown;
        }
        private bool IsAutocompletePassThroughKey(KeyEventArgs e)
        {
            return IsModifierKey(e.KeyCode) || IsShiftPrintableKey(e);
        }
        private bool IsModifierKey(Keys keyCode)
        {
            return keyCode == Keys.Shift ||
                   keyCode == Keys.ShiftKey ||
                   keyCode == Keys.LShiftKey ||
                   keyCode == Keys.RShiftKey ||
                   keyCode == Keys.Control ||
                   keyCode == Keys.ControlKey ||
                   keyCode == Keys.LControlKey ||
                   keyCode == Keys.RControlKey ||
                   keyCode == Keys.Alt ||
                   keyCode == Keys.Menu ||
                   keyCode == Keys.LMenu ||
                   keyCode == Keys.RMenu;
        }
        private bool IsShiftPrintableKey(KeyEventArgs e)
        {
            if (!e.Shift || e.Control || e.Alt) return false;

            Keys keyCode = e.KeyCode;

            return (keyCode >= Keys.A && keyCode <= Keys.Z) ||
                   (keyCode >= Keys.D0 && keyCode <= Keys.D9) ||
                   keyCode == Keys.OemMinus ||
                   keyCode == Keys.Oemplus ||
                   keyCode == Keys.OemOpenBrackets ||
                   keyCode == Keys.OemCloseBrackets ||
                   keyCode == Keys.OemPipe ||
                   keyCode == Keys.OemSemicolon ||
                   keyCode == Keys.OemQuotes ||
                   keyCode == Keys.Oemcomma ||
                   keyCode == Keys.OemPeriod ||
                   keyCode == Keys.OemQuestion ||
                   keyCode == Keys.Oemtilde;
        }
        private bool TryAutocompleteCommitAndInsert(KeyEventArgs e)
        {
            string text;
            if (!TryGetAutocompleteCommitAndInsertText(e, out text)) return false;

            _autocompleteMenu.OnSelecting();
            _textBox.InsertText(text);

            if (text == "." && IsHandleCreated)
                BeginInvoke(new MethodInvoker(ShowAutocompleteListAfterTextInput));

            e.SuppressKeyPress = true;
            return true;
        }
        private bool TryGetAutocompleteCommitAndInsertText(KeyEventArgs e, out string text)
        {
            text = null;
            if (e.Control || e.Alt) return false;

            if (!e.Shift && (e.KeyCode == Keys.OemPeriod || e.KeyCode == Keys.Decimal))
            {
                text = ".";
                return true;
            }

            char keyChar;
            if (PythonTextBoxAutocompleteMenuHelper.TryGetKeyChar(e, out keyChar) && keyChar == '(')
            {
                text = "(";
                return true;
            }

            return false;
        }
        private bool IsAutocompleteCommitAndContinueCharacter(char c)
        {
            return c == '(';
        }
        private bool IsAutocompleteTriggerCharacter(char c)
        {
            return c == '.' || c == '_' || char.IsLetterOrDigit(c);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_textBox != null)
                {
                    _textBox.TextChanged -= PythonEditorTextBox_TextChanged;
                    _textBox.TextChangedDelayed -= PythonEditorTextBox_TextChangedDelayed;
                    _textBox.Leave -= PythonEditorTextBox_Leave;
                }

                SetPythonContext(null);

                _syntaxHighlighter.Dispose();
                _errorLineStyle.Dispose();
                _matchingBracketStyle.Dispose();
                _autocompleteMenu?.Dispose();
                _textBox?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
