// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace UserControls
{
    public class PythonConsoleTextBox : UserControl
    {
        // Variables                                                                                                                
        private int _inputStart;
        private bool _needMoreInput;
        private bool _internalTextChange;
        private bool _consoleInitialized;
        private bool _mouseSelecting;
        //
        private readonly List<string> _commandHistory = new List<string>();
        private int _historyIndex = -1;
        private const string MainPrompt = ">>> ";
        private const string MorePrompt = "... ";
        //
        private readonly TextStyle _promptStyle = new TextStyle(Brushes.DarkBlue, null, FontStyle.Bold);
        private readonly TextStyle _outputStyle = new TextStyle(Brushes.Black, null, FontStyle.Regular);
        private readonly TextStyle _errorStyle = new TextStyle(Brushes.DarkRed, null, FontStyle.Regular);
        private readonly TextStyle _introStyle = new TextStyle(Brushes.DarkGreen, null, FontStyle.Regular);
        private readonly TextStyle _keywordStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly TextStyle _stringStyle = new TextStyle(Brushes.Brown, null, FontStyle.Regular);
        private readonly TextStyle _commentStyle = new TextStyle(Brushes.Green, null, FontStyle.Italic);
        private readonly TextStyle _numberStyle = new TextStyle(Brushes.DarkCyan, null, FontStyle.Regular);
        private PythonConsoleFastTextBox _textBox;
        // Autocomplete
        private string _lastCompletionKey;
        private string _lastCompletionExpression;
        private string _lastCompletionOriginalPrefix;
        private int _lastCompletionReplaceStartLocal = -1;
        private List<string> _lastCompletions = new List<string>();
        private int _lastCompletionIndex = -1;
        // Autocomplete drop-down
        private ListBox _autocompleteListBox;
        private string _autocompleteExpression;
        private string _autocompletePrefix;
        private int _autocompleteReplaceStartLocal;


        // Events                                                                                                                   
        public event EventHandler<PythonConsoleCommandEventArgs> ExecuteCommand;
        public event EventHandler<PythonConsoleAutocompleteEventArgs> AutocompleteRequested;


        // Properties                                                                                                               
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentInput
        {
            get
            {
                if (_textBox.TextLength < _inputStart) return "";
                return _textBox.Text.Substring(_inputStart);
            }
        }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TextLength
        {
            get { return _textBox.TextLength; }
        }
        public override string Text
        {
            get { return _textBox.Text; }
            set { _textBox.Text = value; }
        }
        public override Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                if (_textBox != null) _textBox.Font = value;
            }
        }
        [DefaultValue(BorderStyle.FixedSingle)]
        public new BorderStyle BorderStyle
        {
            get { return _textBox.BorderStyle; }
            set { _textBox.BorderStyle = value; }
        }
        [DefaultValue(true)]
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
        public PythonConsoleTextBox()
        {
            InitializeComponent();
            InitializeConsoleControl();
        }


        // Methods                                                                                                                  
        private void InitializeComponent()
        {
            SuspendLayout();
            //
            _textBox = new PythonConsoleFastTextBox(this);
            _textBox.Dock = DockStyle.Fill;
            _textBox.Name = "fctbConsole";
            _textBox.TabIndex = 0;
            //
            _autocompleteListBox = new ListBox();
            _autocompleteListBox.Visible = false;
            _autocompleteListBox.IntegralHeight = true;
            _autocompleteListBox.Height = 140;
            _autocompleteListBox.Width = 260;
            _autocompleteListBox.TabStop = false;
            _autocompleteListBox.BorderStyle = BorderStyle.FixedSingle;
            _autocompleteListBox.DoubleClick += AutocompleteListBox_DoubleClick;
            _autocompleteListBox.KeyDown += AutocompleteListBox_KeyDown;
            //
            Name = "PythonConsoleTextBox";
            Size = new Size(400, 250);
            Controls.Add(_textBox);
            Controls.Add(_autocompleteListBox);
            //
            _autocompleteListBox.BringToFront();
            //
            ResumeLayout(false);
        }
        private void InitializeConsoleControl()
        {
            _textBox.Language = Language.Custom;
            _textBox.ShowLineNumbers = false;
            _textBox.WordWrap = true;
            _textBox.AcceptsTab = true;
            _textBox.AutoIndent = false;
            _textBox.Font = new Font("Consolas", 10.0f);
            _textBox.AllowDrop = false;
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            // Empty menu prevents default cut/paste actions on protected history.
            _textBox.ContextMenuStrip = new ContextMenuStrip();
            //
            _textBox.TextChanging += PythonConsoleTextBox_TextChanging;
            _textBox.TextChangedDelayed += PythonConsoleTextBox_TextChangedDelayed;
        }
        public void InitializeConsole()
        {
            if (_consoleInitialized) return;

            _consoleInitialized = true;
            ClearConsole();
            Focus();
        }
        public void ClearConsole()
        {
            _internalTextChange = true;
            try
            {
                Clear();

                AppendConsoleText("PrePoMax Python Console" + Environment.NewLine, _introStyle);
                AppendConsoleText("The current results object is available as 'results'." +
                                  Environment.NewLine + Environment.NewLine, _introStyle);

                _needMoreInput = false;
                AppendPrompt(MainPrompt);
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        public void WriteOutput(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _internalTextChange = true;
            try
            {
                AppendConsoleText(text, _outputStyle);
                MoveCaretToEnd();
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        public void WriteError(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            _internalTextChange = true;
            try
            {
                AppendConsoleText(text, _errorStyle);
                MoveCaretToEnd();
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        public void WriteLine(string text)
        {
            WriteOutput(text + Environment.NewLine);
        }
        public void Clear()
        {
            _textBox.Clear();
        }
        public new bool Focus()
        {
            return _textBox.Focus();
        }
        internal bool ProcessKeyDown(KeyEventArgs e)
        {
            // Auto-complete list navigation
            if (_autocompleteListBox.Visible)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    HideAutocompleteList();
                    e.SuppressKeyPress = true;
                    return true;
                }
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    AcceptAutocompleteListSelection();
                    e.SuppressKeyPress = true;
                    return true;
                }
                if (e.KeyCode == Keys.Up)
                {
                    MoveAutocompleteSelection(-1);
                    e.SuppressKeyPress = true;
                    return true;
                }
                if (e.KeyCode == Keys.Down)
                {
                    MoveAutocompleteSelection(1);
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            // Tab key: Auto-complete the current input
            if (e.KeyCode == Keys.Tab)
            {
                CompleteCurrentInput();
                e.SuppressKeyPress = true;
                return true;
            }
            // Ctrl+Space: Show the autocomplete list
            if (e.Control && e.KeyCode == Keys.Space)
            {
                ShowAutocompleteList();
                e.SuppressKeyPress = true;
                return true;
            }
            // Ctrl+C: Copy selected text, even if it is in history
            if (e.Control && e.KeyCode == Keys.C)
            {
                if (_textBox.SelectionLength > 0) _textBox.Copy();
                e.SuppressKeyPress = true;
                return true;
            }
            // Ctrl+A: Select all text in the current input, not the entire console
            if (e.Control && e.KeyCode == Keys.A)
            {
                SelectCurrentInput();
                e.SuppressKeyPress = true;
                return true;
            }
            // Ctrl+X: Cut only if the selection is fully in the current input
            if (e.Control && e.KeyCode == Keys.X)
            {
                if (!IsSelectionFullyInInput())
                {
                    ResetAutocomplete();
                    e.SuppressKeyPress = true;
                    return true;
                }
                return false;
            }
            // Ctrl+V or Shift+Insert: Paste into the current input only
            if (e.Control && e.KeyCode == Keys.V)
            {
                ResetAutocomplete();
                PasteIntoInputOnly();
                e.SuppressKeyPress = true;
                return true;
            }
            // Shift+Insert: Paste into the current input only
            if (e.Shift && e.KeyCode == Keys.Insert)
            {
                ResetAutocomplete();
                PasteIntoInputOnly();
                e.SuppressKeyPress = true;
                return true;
            }
            // Enter key: Execute the current input
            if (e.KeyCode == Keys.Enter)
            {
                ResetAutocomplete();
                ExecuteCurrentInput();
                e.SuppressKeyPress = true;
                return true;
            }
            // Backspace key: Prevent deleting text in history
            if (e.KeyCode == Keys.Back)
            {
                if (!CanBackspace())
                {
                    ResetAutocomplete();
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            // Delete key: Prevent deleting text in history
            if (e.KeyCode == Keys.Delete)
            {
                if (!CanDelete())
                {
                    ResetAutocomplete();
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            // Up and Down keys: Navigate through command history
            if (e.KeyCode == Keys.Up)
            {
                ResetAutocomplete();
                ShowPreviousHistoryItem();
                e.SuppressKeyPress = true;
                return true;
            }
            // Down key: Navigate through command history
            if (e.KeyCode == Keys.Down)
            {
                ResetAutocomplete();
                ShowNextHistoryItem();
                e.SuppressKeyPress = true;
                return true;
            }
            // Left key: Prevent moving the caret into history
            if (e.KeyCode == Keys.Left)
            {
                if (_textBox.SelectionLength == 0 && _textBox.SelectionStart <= _inputStart)
                {
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            // Home  key: Move the caret to the start of the current input
            if (e.KeyCode == Keys.Home || e.Control && e.KeyCode == Keys.Home)
            {
                MoveCaretToInputStart();
                e.SuppressKeyPress = true;
                return true;
            }
            // PageUp key: Prevent moving the caret into history
            if (e.KeyCode == Keys.PageUp)
            {
                // Prevent moving the editing caret into old history
                e.SuppressKeyPress = true;
                return true;
            }
            //
            HideAutocompleteList();
            ResetAutocomplete();
            //
            return false;
        }
        internal void ProcessKeyPress(KeyPressEventArgs e)
        {
            if (!_internalTextChange && IsCaretOrSelectionInHistory())
                MoveCaretToEnd();
        }
        internal void ProcessMouseDown(MouseEventArgs e)
        {
            _mouseSelecting = true;
        }
        internal void ProcessMouseUp(MouseEventArgs e)
        {
            _mouseSelecting = false;
            BeginInvoke(new Action(CorrectSelectionAfterMouse));
        }
        internal void ProcessSelectionChanged()
        {
            if (_internalTextChange || _mouseSelecting) return;

            // Allow selecting old text for copy, but do not leave simple caret in history.
            if (_textBox.SelectionLength == 0 && _textBox.SelectionStart < _inputStart)
                MoveCaretToEnd();
        }
        private void PythonConsoleTextBox_TextChanging(object sender, TextChangingEventArgs e)
        {
            if (_internalTextChange) return;

            // Last line of defense: no edit operation may start in history.
            if (!IsSelectionFullyInInput())
            {
                e.Cancel = true;
                MoveCaretToEnd();
            }
        }
        private void PythonConsoleTextBox_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            if (_internalTextChange) return;

            HighlightPythonSyntax(e.ChangedRange);
        }
        private void HighlightPythonSyntax(Range range)
        {
            range.ClearStyle(_keywordStyle, _stringStyle, _commentStyle, _numberStyle);

            // Comments
            range.SetStyle(_commentStyle, @"#.*$", RegexOptions.Multiline);

            // Strings
            range.SetStyle(_stringStyle, "\"\"\".*?\"\"\"", RegexOptions.Singleline);
            range.SetStyle(_stringStyle, "'''.*?'''", RegexOptions.Singleline);
            range.SetStyle(_stringStyle, "\"([^\"\\\\]|\\\\.)*\"");
            range.SetStyle(_stringStyle, "'([^'\\\\]|\\\\.)*'");

            // Numbers
            range.SetStyle(_numberStyle, @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b");

            // Python keywords
            range.SetStyle(_keywordStyle,
                @"\b(" +
                "False|None|True|" +
                "and|as|assert|async|await|break|class|continue|def|del|elif|else|" +
                "except|finally|for|from|global|if|import|in|is|lambda|nonlocal|" +
                "not|or|pass|raise|return|try|while|with|yield" +
                @")\b");
        }
        private void MoveCaretToEndOfInsertedText(int caretPosition)
        {
            _textBox.SelectionStart = caretPosition;
            _textBox.SelectionLength = 0;
            _textBox.DoCaretVisible();
        }
        private void ExecuteCurrentInput()
        {
            if (!IsSelectionFullyInInput())
                MoveCaretToEnd();

            string command = CurrentInput;
            AddCommandToHistory(command);

            _internalTextChange = true;
            try
            {
                AppendConsoleText(Environment.NewLine, _outputStyle);
            }
            finally
            {
                _internalTextChange = false;
            }

            PythonConsoleCommandEventArgs args = new PythonConsoleCommandEventArgs(command);

            try
            {
                EventHandler<PythonConsoleCommandEventArgs> handler = ExecuteCommand;
                if (handler != null)
                    handler(this, args);
            }
            catch (Exception ex)
            {
                args.Output = "";
                args.Error = ex.ToString() + Environment.NewLine;
                args.NeedMoreInput = false;
            }

            _internalTextChange = true;
            try
            {
                if (!string.IsNullOrEmpty(args.Output))
                    AppendConsoleText(args.Output, _outputStyle);

                if (!string.IsNullOrEmpty(args.Error))
                    AppendConsoleText(args.Error, _errorStyle);

                _needMoreInput = args.NeedMoreInput;

                AppendPrompt(_needMoreInput ? MorePrompt : MainPrompt);
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        private void AppendPrompt(string prompt)
        {
            AppendConsoleText(prompt, _promptStyle);
            _inputStart = _textBox.TextLength;
            MoveCaretToEnd();
        }
        private void AppendConsoleText(string text, Style style)
        {
            if (string.IsNullOrEmpty(text)) return;

            int start = _textBox.TextLength;

            _textBox.AppendText(text);

            int end = _textBox.TextLength;

            _textBox.SelectionStart = start;
            _textBox.SelectionLength = end - start;
            _textBox.Selection.SetStyle(style);

            _textBox.SelectionStart = end;
            _textBox.SelectionLength = 0;
        }
        private void PasteIntoInputOnly()
        {
            if (!Clipboard.ContainsText()) return;

            if (!IsSelectionFullyInInput())
                MoveCaretToEnd();

            string text = Clipboard.GetText();
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");

            if (!text.Contains("\n"))
            {
                _textBox.InsertText(text);
                return;
            }

            bool hasTrailingLineBreak = text.EndsWith("\n");
            string[] lines = text.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                _textBox.InsertText(lines[i]);

                if (i < lines.Length - 1)
                {
                    ExecuteCurrentInput();
                }
                else if (hasTrailingLineBreak && _needMoreInput)
                {
                    ExecuteCurrentInput();
                }
            }
        }
        private bool CanBackspace()
        {
            if (!IsSelectionFullyInInput()) return false;

            if (_textBox.SelectionLength > 0)
                return _textBox.SelectionStart >= _inputStart;

            return _textBox.SelectionStart > _inputStart;
        }
        private bool CanDelete()
        {
            if (!IsSelectionFullyInInput()) return false;

            if (_textBox.SelectionLength > 0)
                return _textBox.SelectionStart >= _inputStart;

            return _textBox.SelectionStart >= _inputStart;
        }
        private bool IsCaretOrSelectionInHistory()
        {
            if (_textBox.SelectionStart < _inputStart) return true;

            if (_textBox.SelectionLength == 0) return false;

            int selectionEnd = _textBox.SelectionStart + _textBox.SelectionLength;
            return selectionEnd <= _inputStart;
        }
        private bool IsSelectionFullyInInput()
        {
            return _textBox.SelectionStart >= _inputStart;
        }
        private void CorrectSelectionAfterMouse()
        {
            if (_textBox.SelectionLength == 0 && _textBox.SelectionStart < _inputStart)
                MoveCaretToEnd();
        }
        private void AddCommandToHistory(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
            {
                _historyIndex = _commandHistory.Count;
                return;
            }

            if (_commandHistory.Count == 0 || _commandHistory[_commandHistory.Count - 1] != command)
                _commandHistory.Add(command);

            _historyIndex = _commandHistory.Count;
        }
        private void ShowPreviousHistoryItem()
        {
            if (_commandHistory.Count == 0) return;

            if (_historyIndex < 0 || _historyIndex > _commandHistory.Count)
                _historyIndex = _commandHistory.Count;

            if (_historyIndex > 0)
                _historyIndex--;

            ReplaceCurrentInput(_commandHistory[_historyIndex]);
        }
        private void ShowNextHistoryItem()
        {
            if (_commandHistory.Count == 0) return;

            if (_historyIndex < 0 || _historyIndex >= _commandHistory.Count)
            {
                _historyIndex = _commandHistory.Count;
                ReplaceCurrentInput("");
                return;
            }

            _historyIndex++;

            ReplaceCurrentInput(_historyIndex < _commandHistory.Count ? _commandHistory[_historyIndex] : "");
        }
        private void ReplaceCurrentInput(string text)
        {
            _internalTextChange = true;
            try
            {
                _textBox.SelectionStart = _inputStart;
                _textBox.SelectionLength = _textBox.TextLength - _inputStart;
                _textBox.InsertText(text);
                MoveCaretToEnd();
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        private void SelectCurrentInput()
        {
            _textBox.SelectionStart = _inputStart;
            _textBox.SelectionLength = _textBox.TextLength - _inputStart;
        }
        private void MoveCaretToInputStart()
        {
            _textBox.SelectionStart = _inputStart;
            _textBox.SelectionLength = 0;
            _textBox.DoCaretVisible();
            _textBox.Invalidate();
        }
        private void MoveCaretToEnd()
        {
            _textBox.SelectionStart = _textBox.TextLength;
            _textBox.SelectionLength = 0;
            _textBox.DoCaretVisible();
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _textBox.TextChanging -= PythonConsoleTextBox_TextChanging;
                _textBox.TextChangedDelayed -= PythonConsoleTextBox_TextChangedDelayed;

                _promptStyle.Dispose();
                _outputStyle.Dispose();
                _errorStyle.Dispose();
                _introStyle.Dispose();
                _keywordStyle.Dispose();
                _stringStyle.Dispose();
                _commentStyle.Dispose();
                _numberStyle.Dispose();

                _textBox.Dispose();
            }

            base.Dispose(disposing);
        }
        // Auto-completion methods
        private void CompleteCurrentInput()
        {
            string currentInput = CurrentInput;
            int localCaret = _textBox.SelectionStart - _inputStart;

            if (localCaret < 0) localCaret = 0;
            if (localCaret > currentInput.Length) localCaret = currentInput.Length;

            string beforeCaret = currentInput.Substring(0, localCaret);

            string expression;
            string prefix;
            int replaceStartLocal;
            GetCompletionExpression(beforeCaret, out expression, out prefix, out replaceStartLocal);

            if (string.IsNullOrEmpty(prefix) && string.IsNullOrEmpty(expression)) return;

            bool continueCycling = CanContinueCompletionCycle(currentInput,
                                                               localCaret,
                                                               expression,
                                                               replaceStartLocal);

            if (!continueCycling)
            {
                List<string> completions = GetCompletions(expression, prefix);
                if (completions == null || completions.Count == 0) return;

                completions.Sort(StringComparer.OrdinalIgnoreCase);

                _lastCompletionExpression = expression;
                _lastCompletionOriginalPrefix = prefix;
                _lastCompletionReplaceStartLocal = replaceStartLocal;
                _lastCompletions = completions;
                _lastCompletionIndex = 0;
                _lastCompletionKey = expression + "|" + prefix;
            }
            else
            {
                _lastCompletionIndex++;

                if (_lastCompletionIndex >= _lastCompletions.Count)
                    _lastCompletionIndex = 0;
            }

            string selectedCompletion = _lastCompletions[_lastCompletionIndex];

            int replaceLength = localCaret - _lastCompletionReplaceStartLocal;

            ReplaceInputRange(_lastCompletionReplaceStartLocal,
                              replaceLength,
                              selectedCompletion);
        }
        private bool CanContinueCompletionCycle(string currentInput, int localCaret, string expression, int replaceStartLocal)
        {
            if (_lastCompletions == null || _lastCompletions.Count == 0) return false;
            if (_lastCompletionReplaceStartLocal < 0) return false;
            if (_lastCompletionIndex < 0) return false;

            if (_lastCompletionExpression != expression) return false;

            // The caret must still be after the same completed token.
            if (replaceStartLocal != _lastCompletionReplaceStartLocal) return false;
            if (localCaret < _lastCompletionReplaceStartLocal) return false;

            string currentCompletedText = currentInput.Substring(_lastCompletionReplaceStartLocal,
                                                                 localCaret - _lastCompletionReplaceStartLocal);

            // Continue cycling only if the current text is one of the previously inserted completions.
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
        private void GetCompletionExpression(string beforeCaret, out string expression, out string prefix,
                                             out int replaceStartLocal)
        {
            expression = "";
            prefix = "";
            replaceStartLocal = beforeCaret.Length;

            int i = beforeCaret.Length - 1;

            while (i >= 0 && IsPythonIdentifierCharacter(beforeCaret[i]))
                i--;

            prefix = beforeCaret.Substring(i + 1);
            replaceStartLocal = i + 1;

            if (i >= 0 && beforeCaret[i] == '.')
            {
                int dotIndex = i;
                i--;

                while (i >= 0 && IsPythonExpressionCharacter(beforeCaret[i]))
                    i--;

                expression = beforeCaret.Substring(i + 1, dotIndex - i - 1);
            }
        }
        private bool IsPythonIdentifierCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
        private bool IsPythonExpressionCharacter(char c)
        {
            return char.IsLetterOrDigit(c) ||
                   c == '_' ||
                   c == '.' ||
                   c == ')' ||
                   c == ']' ||
                   c == '"' ||
                   c == '\'';
        }
        private List<string> GetCompletions(string expression, string prefix)
        {
            PythonConsoleAutocompleteEventArgs args =
                new PythonConsoleAutocompleteEventArgs(expression, prefix);

            EventHandler<PythonConsoleAutocompleteEventArgs> handler = AutocompleteRequested;

            if (handler != null)
                handler(this, args);

            return args.Completions;
        }
        private void ReplaceInputRange(int startLocal, int length, string newText)
        {
            _internalTextChange = true;
            try
            {
                int absoluteStart = _inputStart + startLocal;

                _textBox.SelectionStart = absoluteStart;
                _textBox.SelectionLength = length;
                _textBox.InsertText(newText);

                MoveCaretToEndOfInsertedText(absoluteStart + newText.Length);
            }
            finally
            {
                _internalTextChange = false;
            }
        }
        private void ResetAutocomplete()
        {
            _lastCompletionKey = null;
            _lastCompletionExpression = null;
            _lastCompletionOriginalPrefix = null;
            _lastCompletionReplaceStartLocal = -1;
            _lastCompletions.Clear();
            _lastCompletionIndex = -1;
        }
        // Intelisense methods
        private void ShowAutocompleteList()
        {
            string currentInput = CurrentInput;
            int localCaret = _textBox.SelectionStart - _inputStart;

            if (localCaret < 0) localCaret = 0;
            if (localCaret > currentInput.Length) localCaret = currentInput.Length;

            string beforeCaret = currentInput.Substring(0, localCaret);

            string expression;
            string prefix;
            int replaceStartLocal;
            GetCompletionExpression(beforeCaret, out expression, out prefix, out replaceStartLocal);

            List<string> completions = GetCompletions(expression, prefix);
            if (completions == null || completions.Count == 0)
            {
                HideAutocompleteList();
                return;
            }

            completions.Sort(StringComparer.OrdinalIgnoreCase);

            _autocompleteExpression = expression;
            _autocompletePrefix = prefix;
            _autocompleteReplaceStartLocal = replaceStartLocal;

            _autocompleteListBox.BeginUpdate();
            try
            {
                _autocompleteListBox.Items.Clear();

                foreach (string completion in completions)
                    _autocompleteListBox.Items.Add(completion);

                _autocompleteListBox.SelectedIndex = 0;
            }
            finally
            {
                _autocompleteListBox.EndUpdate();
            }

            Point popupLocation = GetAutocompletePopupLocation();
            _autocompleteListBox.Location = popupLocation;
            _autocompleteListBox.Width = Math.Max(220, GetAutocompleteWidth(completions));
            _autocompleteListBox.Height = Math.Min(160, Math.Max(40, completions.Count * _autocompleteListBox.ItemHeight + 4));

            _autocompleteListBox.Visible = true;
            _autocompleteListBox.BringToFront();

            _textBox.Focus();
        }
        private Point GetAutocompletePopupLocation()
        {
            Point p = _textBox.PlaceToPoint(_textBox.Selection.Start);
            p.Y += _textBox.CharHeight + 2;

            if (p.X + _autocompleteListBox.Width > Width)
                p.X = Math.Max(0, Width - _autocompleteListBox.Width);

            if (p.Y + _autocompleteListBox.Height > Height)
                p.Y = Math.Max(0, p.Y - _autocompleteListBox.Height - _textBox.CharHeight - 4);

            return p;
        }
        private void AcceptAutocompleteListSelection()
        {
            if (!_autocompleteListBox.Visible) return;
            if (_autocompleteListBox.SelectedItem == null) return;

            string selectedCompletion = _autocompleteListBox.SelectedItem.ToString();

            string currentInput = CurrentInput;
            int localCaret = _textBox.SelectionStart - _inputStart;

            if (localCaret < 0) localCaret = 0;
            if (localCaret > currentInput.Length) localCaret = currentInput.Length;

            int replaceLength = localCaret - _autocompleteReplaceStartLocal;
            if (replaceLength < 0) replaceLength = 0;

            ReplaceInputRange(_autocompleteReplaceStartLocal,
                              replaceLength,
                              selectedCompletion);

            HideAutocompleteList();
        }
        private void HideAutocompleteList()
        {
            _autocompleteListBox.Visible = false;
            _autocompleteListBox.Items.Clear();
            _autocompleteExpression = null;
            _autocompletePrefix = null;
            _autocompleteReplaceStartLocal = -1;
        }
        private void MoveAutocompleteSelection(int delta)
        {
            if (!_autocompleteListBox.Visible) return;
            if (_autocompleteListBox.Items.Count == 0) return;

            int index = _autocompleteListBox.SelectedIndex;
            if (index < 0) index = 0;

            index += delta;

            if (index < 0) index = _autocompleteListBox.Items.Count - 1;
            if (index >= _autocompleteListBox.Items.Count) index = 0;

            _autocompleteListBox.SelectedIndex = index;
        }
        private void AutocompleteListBox_DoubleClick(object sender, EventArgs e)
        {
            AcceptAutocompleteListSelection();
        }
        private void AutocompleteListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                AcceptAutocompleteListSelection();
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                HideAutocompleteList();
                e.SuppressKeyPress = true;
            }
        }
        private int GetAutocompleteWidth(List<string> completions)
        {
            int width = 220;

            using (Graphics g = CreateGraphics())
            {
                foreach (string item in completions)
                {
                    SizeF size = g.MeasureString(item, _autocompleteListBox.Font);
                    width = Math.Max(width, (int)size.Width + 30);
                }
            }

            return Math.Min(width, 500);
        }
    }
}