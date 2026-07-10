// PrePoMax - Copyright (C) 2016-2026 Matej Borovinsek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinsek
// Contributors:

using CaeGlobals;
using FastColoredTextBoxNS;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace UserControls
{
    public class CalculixInpTextBox : UserControl, ICalculixInpTextBox
    {
        // Variables                                                                                                                
        private FastColoredTextBox _textBox;
        private readonly Style[] _allStyles;
        private readonly Style _greenStyle = new TextStyle(Brushes.Green,
                                                           new SolidBrush(Color.FromArgb(230, 255, 230)),
                                                           FontStyle.Regular);
        private readonly Style _blueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Regular);
        private readonly Style _grayStyle = new TextStyle(Brushes.Gray, null, FontStyle.Regular);


        // Events                                                                                                                   
        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new event EventHandler TextChanged;


        // Properties                                                                                                               
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Control Control { get { return this; } }
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int LineCount { get { return _textBox.Lines.Count; } }
        public override string Text
        {
            get { return _textBox.Text; }
            set { _textBox.Text = value; }
        }
        public bool ReadOnly
        {
            get { return _textBox.ReadOnly; }
            set { _textBox.ReadOnly = value; }
        }
        public new BorderStyle BorderStyle
        {
            get { return _textBox.BorderStyle; }
            set { _textBox.BorderStyle = value; }
        }


        // Constructors                                                                                                             
        public CalculixInpTextBox()
        {
            _allStyles = new Style[] { _greenStyle, _blueStyle, _grayStyle };
            //
            InitializeComponent();
            InitializeCalculixTextBox();
        }


        // Methods                                                                                                                  
        private void InitializeComponent()
        {
            SuspendLayout();
            //
            _textBox = new FastColoredTextBox();    // must be here for the designer to work
            _textBox.Dock = DockStyle.Fill;
            _textBox.Name = "fctb";
            _textBox.TabIndex = 0;
            //
            Controls.Add(_textBox);
            //
            Name = "CalculixKeywordTextBox";
            Size = new Size(300, 150);
            //
            ResumeLayout(false);
        }
        private void InitializeCalculixTextBox()
        {
            _textBox.AutoCompleteBracketsList = new char[] { '(', ')', '{', '}', '[', ']', '"', '"', '\'', '\'' };
            _textBox.BackBrush = null;
            _textBox.BorderStyle = BorderStyle.FixedSingle;
            _textBox.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            _textBox.Font = new Font("Consolas", 9.0f);
            _textBox.Paddings = new Padding(0);
            _textBox.SelectionColor = Color.FromArgb(60, 0, 0, 255);
            _textBox.Zoom = 100;

            _textBox.TextChanged += CalculixInpTextBox_TextChanged;
        }
        public void Clear()
        {
            _textBox.Clear();
        }
        public string GetActivatedText()
        {
            return string.Join(Environment.NewLine,
                               _textBox.Lines.Select(line => line.Length >= 2 ? line.Substring(2) : ""));
        }
        public string GetDeactivatedText()
        {
            return string.Join(Environment.NewLine,
                               _textBox.Lines.Select(line => "**" + line));
        }
        public string GetLine(int index)
        {
            return _textBox.Lines[index];
        }
        public string GetTextRows()
        {
            return _textBox.Lines.ToArray().ToRows(_textBox.Lines.Count);
        }
        public void ApplyCalculixStyles()
        {
            ApplyCalculixStyles(_textBox.Range);
        }
        private void ApplyCalculixStyles(FastColoredTextBoxNS.Range range)
        {
            range.ClearStyle(_allStyles);
            range.SetStyle(_greenStyle, @"\*\*.*$", RegexOptions.Multiline);
            range.SetStyle(_blueStyle, @"\*.*$", RegexOptions.Multiline);
            range.SetStyle(_grayStyle, @"\.\.\..*$", RegexOptions.Multiline);
        }
        public string ReplaceLines(int firstLine, int numOfLines, string newText, bool adding)
        {
            StringBuilder sb = new StringBuilder();

            if (firstLine >= 0)
            {
                if (adding) numOfLines = 0;

                int lastLine = firstLine + numOfLines;

                _textBox.Selection.Start = new Place(0, firstLine);
                _textBox.Selection.End = new Place(0, lastLine);

                for (int i = firstLine; i < firstLine + numOfLines; i++)
                {
                    if (i < firstLine + numOfLines - 1) sb.AppendLine(_textBox.Selection.tb[i].Text);
                    else sb.Append(_textBox.Selection.tb[i].Text);
                }

                _textBox.ClearSelected();

                string text = newText;
                if (text != null) text += Environment.NewLine;

                _textBox.InsertText(text);
            }

            return sb.ToString();
        }
        public void SelectLinesAndScrollToSelection(int firstLine, int numOfLines)
        {
            _textBox.Navigate(firstLine);
            _textBox.Selection = new FastColoredTextBoxNS.Range(_textBox,
                                                                0,
                                                                firstLine,
                                                                0,
                                                                firstLine + numOfLines);
        }
        private void CalculixInpTextBox_TextChanged(object sender, FastColoredTextBoxNS.TextChangedEventArgs e)
        {
            ApplyCalculixStyles(e.ChangedRange);
            TextChanged?.Invoke(this, EventArgs.Empty);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _greenStyle?.Dispose();
                _blueStyle?.Dispose();
                _grayStyle?.Dispose();
                _textBox?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}