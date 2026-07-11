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


        // Properties                                                                                                               
        public override bool Focused { get { return petbPythonEditor.Focused || pctbPythonConsole.Focused; } }


        // Events                                                                                                                   
        public event Action<object, EventArgs> CloseButtonPressedEvent;
        public event EventHandler<PythonTextBoxCommandEventArgs> ExecuteCommand;
        public event EventHandler<PythonTextBoxCommandEventArgs> ExecuteScript;
        public event EventHandler<PythonTextBoxAutocompleteEventArgs> AutocompleteRequested;


        // Constructor                                                                                                              
        public PythonPanel()
        {
            DoubleBuffered = true;
            //
            InitializeComponent();
            //
            pctbPythonConsole.ExecuteCommand += PythonConsole_ExecuteCommand;
            pctbPythonConsole.AutocompleteRequested += PythonConsole_AutocompleteRequested;
            petbPythonEditor.AutocompleteRequested += PythonEditor_AutocompleteRequested;
            petbPythonEditor.RunScriptRequested += PythonEditor_RunScriptRequested;
            petbPythonEditor.RunSelectionRequested += PythonEditor_RunSelectionRequested;
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
                this.btnClose.Image = global::UserControls.Properties.Resources.Close_White;
            }
            catch
            { }
        }
        private void btnClose_MouseLeave(object sender, EventArgs e)
        {
            try
            {
                this.btnClose.Image = global::UserControls.Properties.Resources.Close;
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


        // Methods                                                                                                                  
        public void ShowFind()
        {
            petbPythonEditor.ShowFind();
        }
        private void ExecuteEditorScript(string script, int errorLineOffset)
        {
            if (string.IsNullOrWhiteSpace(script)) return;

            petbPythonEditor.ClearErrorLine();

            PythonTextBoxCommandEventArgs args = new PythonTextBoxCommandEventArgs(script);

            try
            {
                ExecuteScript?.Invoke(petbPythonEditor, args);
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

            MatchCollection matches = Regex.Matches(error, @"File ""<string>"", line (?<line>\d+)");
            if (matches.Count == 0) return false;

            Match match = matches[matches.Count - 1];
            return int.TryParse(match.Groups["line"].Value, out lineNumber);
        }
    }
}
