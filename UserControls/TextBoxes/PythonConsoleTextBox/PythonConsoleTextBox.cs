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
using System.Windows.Forms;

namespace UserControls
{
    public class PythonConsoleTextBox : UserControl
    {
        // Variables                                                                                                                
        private int _inputStart;
        private int _promptStart;
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
        private readonly TextStyle _introStyle = new TextStyle(Brushes.DarkGreen, null, FontStyle.Italic);
        private readonly PythonTextBoxSyntaxHighlighter _syntaxHighlighter = new PythonTextBoxSyntaxHighlighter();
        private PythonConsoleFastTextBox _textBox;
        private AutocompleteMenu _autocompleteMenu;
        // Context menu
        private ToolStripMenuItem _cutMenuItem;
        private ToolStripMenuItem _copyMenuItem;
        private ToolStripMenuItem _pasteMenuItem;
        private ToolStripMenuItem _selectAllMenuItem;
        private ToolStripMenuItem _clearMenuItem;
        // Autocomplete
        private string _lastCompletionKey;
        private string _lastCompletionExpression;
        private string _lastCompletionOriginalPrefix;
        private int _lastCompletionReplaceStartLocal = -1;
        private List<string> _lastCompletions = new List<string>();
        private int _lastCompletionIndex = -1;


        // Events                                                                                                                   
        public event EventHandler<PythonTextBoxCommandEventArgs> ExecuteCommand;
        public event EventHandler<PythonTextBoxAutocompleteEventArgs> AutocompleteRequested;


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
                if (_autocompleteMenu != null) _autocompleteMenu.Font = value;
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
            Name = "PythonConsoleTextBox";
            Size = new Size(400, 250);
            Controls.Add(_textBox);
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
            _textBox.ContextMenuStrip = CreateContextMenu();
            //
            _autocompleteMenu = PythonTextBoxAutocompleteMenuHelper.Create(_textBox);
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
                _textBox.Clear();

                AppendConsoleText("#Python Interactive Console" + Environment.NewLine, _introStyle);

                _needMoreInput = false;
                AppendPrompt(MainPrompt);
            }
            finally
            {
                _internalTextChange = false;
            }

            _historyIndex = _commandHistory.Count;

            HideAutocompleteList();
            ResetAutocomplete();
        }
        private ContextMenuStrip CreateContextMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            _cutMenuItem = new ToolStripMenuItem("Cut");
            _copyMenuItem = new ToolStripMenuItem("Copy");
            _pasteMenuItem = new ToolStripMenuItem("Paste");
            _selectAllMenuItem = new ToolStripMenuItem("Select All");
            _clearMenuItem = new ToolStripMenuItem("Clear Console");

            _cutMenuItem.ShortcutKeys = Keys.Control | Keys.X;
            _copyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
            _pasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
            _selectAllMenuItem.ShortcutKeys = Keys.Control | Keys.A;

            _cutMenuItem.Click += ContextMenuCut_Click;
            _copyMenuItem.Click += ContextMenuCopy_Click;
            _pasteMenuItem.Click += ContextMenuPaste_Click;
            _selectAllMenuItem.Click += ContextMenuSelectAll_Click;
            _clearMenuItem.Click += ContextMenuClear_Click;

            menu.Items.Add(_cutMenuItem);
            menu.Items.Add(_copyMenuItem);
            menu.Items.Add(_pasteMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_selectAllMenuItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_clearMenuItem);

            menu.Opening += ContextMenu_Opening;

            return menu;
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
        public void WriteExecutionResult(string output, string error)
        {
            if (string.IsNullOrEmpty(output) && string.IsNullOrEmpty(error)) return;

            _internalTextChange = true;
            try
            {
                AppendConsoleText(Environment.NewLine, _outputStyle);

                if (!string.IsNullOrEmpty(output))
                    AppendConsoleText(output, _outputStyle);

                if (!string.IsNullOrEmpty(output) && !output.EndsWith(Environment.NewLine) &&
                    !string.IsNullOrEmpty(error) && !error.StartsWith(Environment.NewLine))
                {
                    AppendConsoleText(Environment.NewLine, _outputStyle);
                }

                if (!string.IsNullOrEmpty(error))
                    AppendConsoleText(error, _errorStyle);

                _needMoreInput = false;
                AppendPrompt(MainPrompt);
            }
            finally
            {
                _internalTextChange = false;
            }

            HideAutocompleteList();
            ResetAutocomplete();
        }
        public new bool Focus()
        {
            return _textBox.Focus();
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
                ShowAutocompleteList(true);
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
                ResetAutocomplete();
                if (!IsSelectionFullyInInput())
                {
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
            if (e.KeyCode == Keys.Up && IsSelectionFullyInInput())
            {
                ResetAutocomplete();
                ShowPreviousHistoryItem();
                e.SuppressKeyPress = true;
                return true;
            }
            // Down key: Navigate through command history
            if (e.KeyCode == Keys.Down && IsSelectionFullyInInput())
            {
                ResetAutocomplete();
                ShowNextHistoryItem();
                e.SuppressKeyPress = true;
                return true;
            }
            // Home: On the active input line, move only after the prompt. In history, allow the normal Home behavior.
            if (e.KeyCode == Keys.Home && !e.Control)
            {
                if (_textBox.SelectionStart >= _inputStart)
                {
                    if (e.Shift)
                    {
                        int selectionEnd = _textBox.SelectionStart + _textBox.SelectionLength;
                        _textBox.SelectionStart = _inputStart;
                        _textBox.SelectionLength = Math.Max(0, selectionEnd - _inputStart);
                        _textBox.DoSelectionVisible();
                        _textBox.Invalidate();
                    }
                    else
                    {
                        MoveCaretToInputStart();
                    }
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            //
            if (!IsSelectionFullyInInput())
            {
                bool navigationKey =
                    e.KeyCode == Keys.Left ||
                    e.KeyCode == Keys.Right ||
                    e.KeyCode == Keys.Up ||
                    e.KeyCode == Keys.Down ||
                    e.KeyCode == Keys.Home ||
                    e.KeyCode == Keys.End ||
                    e.KeyCode == Keys.PageUp ||
                    e.KeyCode == Keys.PageDown;

                bool copyKey = e.Control && e.KeyCode == Keys.C;

                if (!navigationKey && !copyKey &&
                    e.KeyCode != Keys.ShiftKey &&
                    e.KeyCode != Keys.ControlKey)
                {
                    e.SuppressKeyPress = true;
                    return true;
                }
            }
            //
            HideAutocompleteList();
            ResetAutocomplete();
            //
            return false;
        }
        internal void ProcessKeyPress(KeyPressEventArgs e)
        {
            if (_internalTextChange) return;

            // Allow the caret to remain in history, but prevent typing there.
            if (!IsSelectionFullyInInput())
            {
                e.Handled = true;
                return;
            }

            if (IsAutocompleteMenuVisible() && IsAutocompleteCommitAndContinueCharacter(e.KeyChar))
                _autocompleteMenu.OnSelecting();

            if (IsAutocompleteTriggerCharacter(e.KeyChar) && IsHandleCreated)
                BeginInvoke(new MethodInvoker(ShowAutocompleteListAfterTextInput));
            else if (!char.IsControl(e.KeyChar) && IsHandleCreated)
                BeginInvoke(new MethodInvoker(HideAutocompleteList));
        }
        internal void ProcessMouseDown(MouseEventArgs e)
        {
            _mouseSelecting = true;
        }
        internal void ProcessMouseUp(MouseEventArgs e)
        {
            _mouseSelecting = false;
        }
        internal void ProcessSelectionChanged()
        {
            if (_internalTextChange || _mouseSelecting) return;
        }
        private void PythonConsoleTextBox_TextChanging(object sender, TextChangingEventArgs e)
        {
            if (_internalTextChange) return;

            // No modification may start in the protected console history.
            if (!IsSelectionFullyInInput())
                e.Cancel = true;
        }
        private void PythonConsoleTextBox_TextChangedDelayed(object sender, TextChangedEventArgs e)
        {
            if (_internalTextChange) return;

            HighlightPythonSyntax(e.ChangedRange);

            if (IsAutocompleteMenuVisible())
                ShowAutocompleteList(false);
        }
        private void ContextMenu_Opening(object sender, CancelEventArgs e)
        {
            bool hasSelection = _textBox.SelectionLength > 0;

            _cutMenuItem.Enabled =
                hasSelection &&
                IsSelectionFullyInInput();

            _copyMenuItem.Enabled = hasSelection;

            _pasteMenuItem.Enabled =
                Clipboard.ContainsText() &&
                CanPasteIntoCurrentInputSelection();

            _selectAllMenuItem.Enabled =
                _textBox.TextLength > 0;

            _clearMenuItem.Enabled =
                _textBox.TextLength > 0;
        }
        private void ContextMenuCut_Click(object sender, EventArgs e)
        {
            if (!IsSelectionFullyInInput()) return;
            if (_textBox.SelectionLength == 0) return;

            _textBox.Cut();
        }
        private void ContextMenuCopy_Click(object sender, EventArgs e)
        {
            if (_textBox.SelectionLength == 0) return;

            _textBox.Copy();
        }
        private void ContextMenuPaste_Click(object sender, EventArgs e)
        {
            PasteIntoInputOnly();
        }
        private void ContextMenuSelectAll_Click(object sender, EventArgs e)
        {
            _textBox.SelectAll();
        }
        private void ContextMenuClear_Click(object sender, EventArgs e)
        {
            ClearConsole();
        }
        private void HighlightPythonSyntax(Range range)
        {
            _syntaxHighlighter.Highlight(range);
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
                return;

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

            PythonTextBoxCommandEventArgs args = new PythonTextBoxCommandEventArgs(command);

            try
            {
                EventHandler<PythonTextBoxCommandEventArgs> handler = ExecuteCommand;
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
            _promptStart = _textBox.TextLength;
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
            if (!PrepareSelectionForInputPaste()) return;

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
                return true;

            return _textBox.SelectionStart > _inputStart;
        }
        private bool CanDelete()
        {
            if (!IsSelectionFullyInInput()) return false;

            if (_textBox.SelectionLength > 0)
                return true;

            return _textBox.SelectionStart >= _inputStart && _textBox.SelectionStart < _textBox.TextLength;
        }
        private bool IsSelectionFullyInInput()
        {
            int selectionStart = _textBox.SelectionStart;
            int selectionEnd = selectionStart + _textBox.SelectionLength;

            return selectionStart >= _inputStart &&
                   selectionEnd >= _inputStart;
        }
        private bool CanPasteIntoCurrentInputSelection()
        {
            if (IsSelectionFullyInInput()) return true;

            int selectionStart = _textBox.SelectionStart;
            int selectionEnd = selectionStart + _textBox.SelectionLength;

            return selectionStart >= _promptStart &&
                   selectionStart < _inputStart &&
                   selectionEnd >= _promptStart;
        }
        private bool PrepareSelectionForInputPaste()
        {
            if (IsSelectionFullyInInput()) return true;
            if (!CanPasteIntoCurrentInputSelection()) return false;

            int selectionEnd = _textBox.SelectionStart + _textBox.SelectionLength;
            _textBox.SelectionStart = _inputStart;
            _textBox.SelectionLength = Math.Max(0, selectionEnd - _inputStart);
                        _textBox.DoSelectionVisible();
                        _textBox.Invalidate();
            return true;
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
                _syntaxHighlighter.Dispose();

                _autocompleteMenu?.Dispose();
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
            PythonTextBoxCompletionHelper.GetCompletionExpression(beforeCaret, out expression, out prefix,
                                                                  out replaceStartLocal);

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
        private List<string> GetCompletions(string expression, string prefix)
        {
            PythonTextBoxAutocompleteEventArgs args =
                new PythonTextBoxAutocompleteEventArgs(expression, prefix);

            EventHandler<PythonTextBoxAutocompleteEventArgs> handler = AutocompleteRequested;

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
        private void ShowAutocompleteListAfterTextInput()
        {
            if (IsDisposed || _textBox == null || !_textBox.Focused) return;

            ShowAutocompleteList(false);
        }
        private void ShowAutocompleteList(bool forced)
        {
            if (!IsSelectionFullyInInput())
            {
                HideAutocompleteList();
                return;
            }

            string currentInput = CurrentInput;
            int localCaret = _textBox.SelectionStart - _inputStart;

            if (localCaret < 0) localCaret = 0;
            if (localCaret > currentInput.Length) localCaret = currentInput.Length;

            string beforeCaret = currentInput.Substring(0, localCaret);

            string expression;
            string prefix;
            int replaceStartLocal;
            PythonTextBoxCompletionHelper.GetCompletionExpression(beforeCaret, out expression, out prefix,
                                                                  out replaceStartLocal);

            if (!forced && string.IsNullOrEmpty(expression) && string.IsNullOrEmpty(prefix))
            {
                HideAutocompleteList();
                return;
            }

            List<string> completions = GetCompletions(expression, prefix);
            if (completions == null || completions.Count == 0)
            {
                HideAutocompleteList();
                return;
            }

            completions.Sort(StringComparer.OrdinalIgnoreCase);

            PythonTextBoxAutocompleteMenuHelper.ShowPrepared(_autocompleteMenu, completions);
            _textBox.Focus();
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
    }
}
