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
using System.Drawing;
using System.Windows.Forms;

namespace UserControls
{
    /// <summary>
    /// The output window: read only text that keeps the scroll position and the selection when it is replaced,
    /// wraps its lines itself and draws the names a link finder recognizes as links, which can be clicked.
    /// </summary>
    public class OutputTextBox : FastColoredTextBox
    {
        // Variables
        private readonly TextStyle _linkStyle = new TextStyle(Brushes.Blue, null, FontStyle.Underline);


        // Properties
        /// <summary>
        /// The links of a line, given its index and its text, each one as its start and its length, or null when the
        /// line has none.
        /// </summary>
        public Func<int, string, int[][]> LinkFinder { get; set; }


        // Events
        /// <summary>
        /// A link was clicked: the whole line it is on, however it is wrapped, and the position of the click in it.
        /// </summary>
        public event Action<string, int> LinkClicked;


        // Constructors
        public OutputTextBox()
        {
            ReadOnly = true;
            ShowLineNumbers = false;
            WordWrap = true;
            WordWrapMode = WordWrapMode.WordWrapControlWidth;
            AutoIndent = false;
            WordWrapAutoIndent = false;
            CurrentLineColor = Color.Transparent;
            ServiceLinesColor = Color.Transparent;
            ShowFoldingLines = false;
            AllowDrop = false;
            SelectionHighlightingForLineBreaksEnabled = false;
            HotkeysMapping = GetTextBoxHotkeys(HotkeysMapping);
        }


        // Methods
        /// <summary>
        /// Only the keys of a plain read only text box: moving the caret, selecting and copying.
        /// </summary>
        private static HotkeysMapping GetTextBoxHotkeys(HotkeysMapping defaultHotkeys)
        {
            HashSet<FCTBAction> textBoxActions = new HashSet<FCTBAction>
            {
                FCTBAction.GoLeft, FCTBAction.GoLeftWithSelection, FCTBAction.GoRight, FCTBAction.GoRightWithSelection,
                FCTBAction.GoUp, FCTBAction.GoUpWithSelection, FCTBAction.GoDown, FCTBAction.GoDownWithSelection,
                FCTBAction.GoHome, FCTBAction.GoHomeWithSelection, FCTBAction.GoEnd, FCTBAction.GoEndWithSelection,
                FCTBAction.GoPageUp, FCTBAction.GoPageUpWithSelection,
                FCTBAction.GoPageDown, FCTBAction.GoPageDownWithSelection,
                FCTBAction.GoFirstLine, FCTBAction.GoFirstLineWithSelection,
                FCTBAction.GoLastLine, FCTBAction.GoLastLineWithSelection,
                FCTBAction.GoWordLeft, FCTBAction.GoWordLeftWithSelection,
                FCTBAction.GoWordRight, FCTBAction.GoWordRightWithSelection,
                FCTBAction.SelectAll, FCTBAction.Copy
            };
            HotkeysMapping hotkeys = new HotkeysMapping();
            hotkeys.Clear();
            foreach (var entry in defaultHotkeys)
            {
                if (textBoxActions.Contains(entry.Value)) hotkeys[entry.Key] = entry.Value;
            }
            return hotkeys;
        }
        /// <summary>
        /// Replaces the text, appending only what is new when the text grew at its end. The view stays where the
        /// user left it - at the end when the end was in view - and so does the selection.
        /// </summary>
        public void SetOutputText(string text)
        {
            // The text box gives its lines back joined as the platform joins them
            text = text == null ? "" : text.Replace("\r\n", "\n");
            string currentText = Text.Replace("\r\n", "\n");
            if (text.Equals(currentText, StringComparison.Ordinal)) return;
            //
            bool atEnd = IsLastLineVisible();
            int verticalScroll = VerticalScroll.Value;
            int selectionStart = PlaceToPosition(Selection.Start);
            int selectionEnd = PlaceToPosition(Selection.End);
            BeginUpdate();
            try
            {
                int firstChangedLine;
                if (currentText.Length > 0 && text.Length > currentText.Length &&
                    text.StartsWith(currentText, StringComparison.Ordinal))
                {
                    firstChangedLine = Math.Max(0, LinesCount - 1);
                    AppendText(text.Substring(currentText.Length));
                }
                else
                {
                    firstChangedLine = 0;
                    Text = text;
                }
                ClearUndo();
                SetLinkStyles(firstChangedLine);
                //
                int length = TextLength;
                Selection = GetRange(PositionToPlace(Math.Min(selectionStart, length)),
                                     PositionToPlace(Math.Min(selectionEnd, length)));
            }
            finally
            {
                EndUpdate();
            }
            //
            if (atEnd) DoRangeVisible(GetRange(Range.End, Range.End));
            else
            {
                VerticalScroll.Value = Math.Max(VerticalScroll.Minimum, Math.Min(VerticalScroll.Maximum, verticalScroll));
                UpdateScrollbars();
            }
            Invalidate();
        }
        private bool IsLastLineVisible()
        {
            return LinesCount == 0 || VisibleRange.End.iLine >= LinesCount - 1;
        }
        private void SetLinkStyles(int firstLine)
        {
            if (LinkFinder == null) return;
            //
            for (int i = firstLine; i < LinesCount; i++)
            {
                int[][] links = LinkFinder(i, GetLineText(i));
                if (links == null) continue;
                //
                foreach (var link in links)
                    GetRange(new Place(link[0], i), new Place(link[0] + link[1], i)).SetStyle(_linkStyle);
            }
        }
        /// <summary>
        /// The character under a point, which is not the place nearest to it: that one is the boundary after a
        /// character when the point is over the right half of it, and the end of the line past its end.
        /// </summary>
        private bool GetCharUnderPoint(Point point, out Place place)
        {
            place = PointToPlace(point);
            if (place.iLine < 0 || place.iLine >= LinesCount) return false;
            //
            int lineLength = GetLineText(place.iLine).Length;
            for (int iChar = place.iChar; iChar >= Math.Max(0, place.iChar - 1); iChar--)
            {
                if (iChar >= lineLength) continue;
                Place candidate = new Place(iChar, place.iLine);
                Point location = PlaceToPoint(candidate);
                if (point.X >= location.X && point.X < location.X + CharWidth &&
                    point.Y >= location.Y && point.Y < location.Y + CharHeight)
                {
                    place = candidate;
                    return true;
                }
            }
            return false;
        }
        private bool IsLinkUnderPoint(Point point, out Place place)
        {
            return GetCharUnderPoint(point, out place) && GetStylesOfChar(place).Contains(_linkStyle);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            //
            Place place;
            Cursor cursor = e.Button == MouseButtons.None && IsLinkUnderPoint(e.Location, out place) ? Cursors.Hand
                                                                                                       : Cursors.IBeam;
            if (Cursor != cursor) Cursor = cursor;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            // A drag selects text to copy and is not a click on a link
            Place place;
            if (e.Button == MouseButtons.Left && Selection.IsEmpty && IsLinkUnderPoint(e.Location, out place))
                LinkClicked?.Invoke(GetLineText(place.iLine), place.iChar);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            // No middle click scrolling mode
            if (e.Button == MouseButtons.Middle) return;
            base.OnMouseDown(e);
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            // No zooming
            if ((ModifierKeys & Keys.Control) != 0)
            {
                if (e is HandledMouseEventArgs handledArgs) handledArgs.Handled = true;
                return;
            }
            base.OnMouseWheel(e);
        }
        protected override void CheckAndChangeSelectionType()
        {
            Selection.ColumnSelectionMode = false;
        }
        public override void Copy()
        {
            // Without a selection the base copies the whole line
            if (Selection.IsEmpty) return;
            base.Copy();
        }
        protected override void OnCreateClipboardData(DataObject data)
        {
            // Text only, without the html and rtf copies that carry the font and the colors
            data.SetData(DataFormats.UnicodeText, true, Selection.Text);
        }
    }
}
