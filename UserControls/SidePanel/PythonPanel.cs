// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace UserControls
{
    public partial class PythonPanel : SidePanel
    {
        // Variables
        private const string DefaultTitle = "Python Script Editor";
        private const string PythonFileFilter = "Python files (*.py)|*.py|All files (*.*)|*.*";
        private string _lastScriptDirectory;


        // Properties
        public override bool Focused { get { return ContainsFocus; } }


        // Events
        public event Action<object, EventArgs> CloseButtonPressedEvent;
        public event EventHandler<PythonTextBoxCommandEventArgs> ExecuteCommand;
        public event EventHandler<PythonTextBoxCommandEventArgs> ExecuteScript;
        public event EventHandler<PythonTextBoxCommandEventArgs> RecordAndExecuteScript;
        public event EventHandler<PythonTextBoxAutocompleteEventArgs> AutocompleteRequested;


        // Constructor
        public PythonPanel()
        {
            DoubleBuffered = true;
            //
            InitializeComponent();
            btnRecordAndRun.Click += btnRecordAndRun_Click;
            //
            pctbPythonConsole.ExecuteCommand += PythonConsole_ExecuteCommand;
            pctbPythonConsole.AutocompleteRequested += PythonConsole_AutocompleteRequested;
            petbPythonEditor.AutocompleteRequested += PythonEditor_AutocompleteRequested;
            petbPythonEditor.RunScriptRequested += PythonEditor_RunScriptRequested;
            petbPythonEditor.RunSelectionRequested += PythonEditor_RunSelectionRequested;
            petbPythonEditor.ModifiedChanged += PythonEditor_ModifiedChanged;
            petbPythonEditor.ScriptTextChanged += PythonEditor_ScriptTextChanged;
            //
            InitializeToolTips();
            UpdateTitle();
            UpdateButtonState();
            NewScript();
            //
            pctbPythonConsole.InitializeConsole();
        }


        // Event handlers

        private void PythonPanel_Resize(object sender, EventArgs e)
        {
        }
        //
        private void btnClose_MouseEnter(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = Properties.Resources.Close_White;
            }
            catch
            { }
        }
        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = Properties.Resources.Close;
            }
            catch
            { }
        }
        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseButtonPressedEvent?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        //
        private void btnNew_Click(object sender, EventArgs e)
        {
            try
            {
                NewScript();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnOpen_Click(object sender, EventArgs e)
        {
            try
            {
                OpenScript();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                SaveScript();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                SaveScriptAs();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnUndo_Click(object sender, EventArgs e)
        {
            try
            {
                petbPythonEditor.Undo();
                UpdateButtonState();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnRedo_Click(object sender, EventArgs e)
        {
            try
            {
                petbPythonEditor.Redo();
                UpdateButtonState();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                petbPythonEditor.RunScript();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnRunSelection_Click(object sender, EventArgs e)
        {
            try
            {
                petbPythonEditor.RunSelection();
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        private void btnRecordAndRun_Click(object sender, EventArgs e)
        {
            try
            {
                ExecuteEditorScript(petbPythonEditor.ScriptText, 0, true);
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
            }
        }
        //
        private void PythonConsole_AutocompleteRequested(object sender, PythonTextBoxAutocompleteEventArgs e)
        {
            AutocompleteRequested(sender, e);
        }
        private void PythonConsole_ExecuteCommand(object sender, PythonTextBoxCommandEventArgs e)
        {
            ExecuteCommand(sender, e);
        }
        //
        private void PythonEditor_AutocompleteRequested(object sender, PythonTextBoxAutocompleteEventArgs e)
        {
            AutocompleteRequested(sender, e);
        }
        private void PythonEditor_RunScriptRequested(object sender, EventArgs e)
        {
            ExecuteEditorScript(petbPythonEditor.ScriptText, 0);
        }
        private void PythonEditor_RunSelectionRequested(object sender, EventArgs e)
        {
            if (!petbPythonEditor.HasSelection) return;

            ExecuteEditorScript(petbPythonEditor.SelectedScriptText, petbPythonEditor.SelectionStartLineNumber - 1);
        }
        private void PythonEditor_ModifiedChanged(object sender, EventArgs e)
        {
            UpdateTitle();
            UpdateButtonState();
        }
        private void PythonEditor_ScriptTextChanged(object sender, EventArgs e)
        {
            UpdateButtonState();
        }


        // Methods
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ContainsFocus && ProcessShortcutKey(keyData)) return true;

            return base.ProcessCmdKey(ref msg, keyData);
        }
        public bool ProcessShortcutKey(Keys keyData)
        {
            Keys keyCode = keyData & Keys.KeyCode;
            Keys modifiers = keyData & (Keys.Control | Keys.Shift | Keys.Alt);

            try
            {
                if (modifiers == Keys.Control)
                {
                    if (keyCode == Keys.S)
                    {
                        SaveScript();
                        return true;
                    }
                    else if (keyCode == Keys.O)
                    {
                        OpenScript();
                        return true;
                    }
                    else if (keyCode == Keys.N)
                    {
                        NewScript();
                        return true;
                    }
                    else if (keyCode == Keys.F)
                    {
                        ShowEditorFind();
                        return true;
                    }
                    else if (keyCode == Keys.H)
                    {
                        ShowEditorReplace();
                        return true;
                    }
                    else if (keyCode == Keys.G)
                    {
                        ShowEditorGoToLine();
                        return true;
                    }
                }
                else if (modifiers == (Keys.Control | Keys.Shift) && keyCode == Keys.S)
                {
                    SaveScriptAs();
                    return true;
                }
                else if (modifiers == Keys.None)
                {
                    if (keyCode == Keys.F5)
                    {
                        petbPythonEditor.RunScript();
                        return true;
                    }
                    else if (keyCode == Keys.F9)
                    {
                        petbPythonEditor.RunSelection();
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                CaeGlobals.ExceptionTools.Show(this, ex);
                return true;
            }

            return false;
        }
        public void ShowFind()
        {
            ShowEditorFind();
        }
        private void ShowEditorFind()
        {
            petbPythonEditor.Focus();
            petbPythonEditor.ShowFind();
        }
        private void ShowEditorReplace()
        {
            petbPythonEditor.Focus();
            petbPythonEditor.ShowReplace();
        }
        private void ShowEditorGoToLine()
        {
            petbPythonEditor.Focus();
            petbPythonEditor.ShowGoToLine();
        }
        private void InitializeToolTips()
        {
            ttText.SetToolTip(btnNew, "New script");
            ttText.SetToolTip(btnOpen, "Open script");
            ttText.SetToolTip(btnSave, "Save script");
            ttText.SetToolTip(btnSaveAs, "Save script as");
            ttText.SetToolTip(btnUndo, "Undo");
            ttText.SetToolTip(btnRedo, "Redo");
            ttText.SetToolTip(btnRun, "Run script");
            ttText.SetToolTip(btnRunSelection, "Run selection");
            ttText.SetToolTip(btnRecordAndRun, "Record script into command history and run");
            ttText.SetToolTip(btnClose, "Close Python panel");
        }
        private void NewScript()
        {
            if (!SaveModifiedScriptIfNeeded()) return;

            petbPythonEditor.ScriptText = GetDefaultScriptText();
            petbPythonEditor.FileName = "";
            petbPythonEditor.ClearModifiedFlag();
            petbPythonEditor.ClearErrorLine();
            UpdateTitle();
            UpdateButtonState();
            petbPythonEditor.Focus();
        }
        private void OpenScript()
        {
            if (!SaveModifiedScriptIfNeeded()) return;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = PythonFileFilter;
                openFileDialog.FileName = "";
                openFileDialog.InitialDirectory = GetDialogInitialDirectory();

                if (openFileDialog.ShowDialog() != DialogResult.OK) return;

                LoadScript(openFileDialog.FileName);
            }
        }
        private void LoadScript(string fileName)
        {
            petbPythonEditor.ScriptText = File.ReadAllText(fileName, Encoding.UTF8);
            petbPythonEditor.FileName = fileName;
            petbPythonEditor.ClearModifiedFlag();
            petbPythonEditor.ClearErrorLine();
            UpdateLastScriptDirectory(fileName);
            UpdateTitle();
            UpdateButtonState();
            petbPythonEditor.Focus();
        }
        private bool SaveScript()
        {
            string fileName = petbPythonEditor.FileName;

            if (string.IsNullOrEmpty(fileName))
                return SaveScriptAs();

            SaveScript(fileName);
            return true;
        }
        private bool SaveScriptAs()
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = PythonFileFilter;
                saveFileDialog.DefaultExt = "py";
                saveFileDialog.AddExtension = true;
                saveFileDialog.FileName = GetSaveFileName();
                saveFileDialog.InitialDirectory = GetDialogInitialDirectory();

                if (saveFileDialog.ShowDialog() != DialogResult.OK) return false;

                SaveScript(saveFileDialog.FileName);
                return true;
            }
        }
        private void SaveScript(string fileName)
        {
            File.WriteAllText(fileName, petbPythonEditor.ScriptText, Encoding.UTF8);
            petbPythonEditor.FileName = fileName;
            petbPythonEditor.ClearModifiedFlag();
            UpdateLastScriptDirectory(fileName);
            UpdateTitle();
            UpdateButtonState();
        }
        private bool SaveModifiedScriptIfNeeded()
        {
            if (!petbPythonEditor.IsModified) return true;

            DialogResult result = CaeGlobals.MessageBoxes.ShowWarningQuestionYesNoCancel(
                "Save changes to the current Python script?");

            if (result == DialogResult.Cancel) return false;
            if (result == DialogResult.Yes) return SaveScript();

            return true;
        }
        private string GetSaveFileName()
        {
            if (!string.IsNullOrEmpty(petbPythonEditor.FileName))
                return Path.GetFileName(petbPythonEditor.FileName);

            return "Script.py";
        }
        private string GetDialogInitialDirectory()
        {
            if (!string.IsNullOrEmpty(petbPythonEditor.FileName))
            {
                string directory = Path.GetDirectoryName(petbPythonEditor.FileName);
                if (Directory.Exists(directory)) return directory;
            }

            if (Directory.Exists(_lastScriptDirectory)) return _lastScriptDirectory;

            return "";
        }
        private void UpdateLastScriptDirectory(string fileName)
        {
            string directory = Path.GetDirectoryName(fileName);
            if (Directory.Exists(directory))
                _lastScriptDirectory = directory;
        }
        private void UpdateTitle()
        {
            string fileName = petbPythonEditor.FileName;

            if (string.IsNullOrEmpty(fileName))
            {
                labTitle.Text = DefaultTitle;
                ttText.SetToolTip(labTitle, "Unsaved Python script");
            }
            else
            {
                string title = Path.GetFileName(fileName);
                if (petbPythonEditor.IsModified) title += "*";

                labTitle.Text = title;
                ttText.SetToolTip(labTitle, fileName);
            }
        }
        private void UpdateButtonState()
        {
            btnUndo.Enabled = petbPythonEditor.CanUndo;
            btnRedo.Enabled = petbPythonEditor.CanRedo;
            btnRun.Enabled = !string.IsNullOrWhiteSpace(petbPythonEditor.ScriptText);
            btnRecordAndRun.Enabled = btnRun.Enabled;
        }
        private string GetDefaultScriptText()
        {
            return "#PrePoMax Python Script" + Environment.NewLine +
                   "#The PrePoMax object is available as 'pmx'" + Environment.NewLine;
        }
        private void ExecuteEditorScript(string script, int errorLineOffset, bool record = false)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            petbPythonEditor.ClearErrorLine();

            PythonTextBoxCommandEventArgs args = new PythonTextBoxCommandEventArgs(script);

            try
            {
                if (record) RecordAndExecuteScript?.Invoke(petbPythonEditor, args);
                else ExecuteScript?.Invoke(petbPythonEditor, args);
            }
            catch (Exception ex)
            {
                args.Output = "";
                args.Error = ex.ToString() + Environment.NewLine;
                args.NeedMoreInput = false;
            }

            if (!string.IsNullOrEmpty(args.Error))
            {
                int lineNumber;
                if (TryGetPythonErrorLine(args.Error, out lineNumber))
                    petbPythonEditor.SetErrorLine(lineNumber + errorLineOffset, args.Error);
            }

            pctbPythonConsole.WriteExecutionResult(args.Output, args.Error);
        }
        private bool TryGetPythonErrorLine(string error, out int lineNumber)
        {
            lineNumber = -1;

            if (TryGetLastPythonErrorLine(error, @"File ""<string>"", line (?<line>\d+)", out lineNumber))
                return true;

            return TryGetLastPythonErrorLine(error, @"(?m)^<string>:(?<line>\d+):", out lineNumber);
        }
        private bool TryGetLastPythonErrorLine(string error, string pattern, out int lineNumber)
        {
            lineNumber = -1;

            MatchCollection matches = Regex.Matches(error, pattern);
            if (matches.Count == 0) return false;

            Match match = matches[matches.Count - 1];
            return int.TryParse(match.Groups["line"].Value, out lineNumber);
        }


    }
}
