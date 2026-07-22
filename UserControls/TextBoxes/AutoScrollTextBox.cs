// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace UserControls
{
    public partial class AutoScrollTextBox : TextBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        private static extern bool GetScrollInfo(IntPtr hWnd, int nBar, ref ScrollInfo lpScrollInfo);
        [DllImport("user32.dll")]
        private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct ScrollInfo
        {
            public uint cbSize;
            public uint fMask;
            public int nMin;
            public int nMax;
            public uint nPage;
            public int nPos;
            public int nTrackPos;
        }


        // Variables                                                                                                                
        private const int SB_HORZ = 0x0;
        private const int SB_VERT = 0x1;
        private const int SIF_ALL = 0x17;
        private const int WM_SETREDRAW = 0x000B;
        private const int WM_HSCROLL = 0x114;
        private const int WM_VSCROLL = 0x115;
        private const int EM_GETFIRSTVISIBLELINE = 0x00CE;
        private const int EM_LINESCROLL = 0x00B6;
        private const int SB_THUMBPOSITION = 4;
        private const int SB_BOTTOM = 7;
        //
        private double _textCharWidth;


        // Properties                                                                                                               
        public double TextCharWidth
        {
            get
            {
                if (_textCharWidth < 0)
                {
                    SizeF sizeA = TextRenderer.MeasureText("A", Font);
                    SizeF sizeAA = TextRenderer.MeasureText("AA", Font);
                    //
                    _textCharWidth = sizeAA.Width - sizeA.Width;
                }
                return _textCharWidth;
            }
        }

        // Constructors                                                                                                             
        public AutoScrollTextBox()
        {
            InitializeComponent();
            //
            _textCharWidth = -1;
        }


        // Methods                                                                                                                  
        public void AutoScrollAppendText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            UpdateText(text, true);
        }

        public void AutoScrollSetText(string text)
        {
            if (text == null) text = "";

            string currentText = Text;
            if (text.Equals(currentText, StringComparison.Ordinal)) return;

            // CalculiX output files normally only grow. Appending their new suffix avoids a full
            // repaint and also leaves the existing selection positions unchanged.
            if (text.Length > currentText.Length && text.StartsWith(currentText, StringComparison.Ordinal))
                UpdateText(text.Substring(currentText.Length), true);
            else
                UpdateText(text, false);
        }

        public void ScrollToEnd()
        {
            if (IsHandleCreated)
                SendMessage(Handle, WM_VSCROLL, new IntPtr(SB_BOTTOM), IntPtr.Zero);
            else
            {
                Select(TextLength, 0);
                ScrollToCaret();
            }
        }

        private void UpdateText(string text, bool append)
        {
            int selectionStart = SelectionStart;
            int selectionLength = SelectionLength;
            bool restoreView = IsHandleCreated;
            bool scrollToEnd = restoreView && IsLastLineVisible();
            int firstVisibleLine = restoreView ? GetFirstVisibleLine() : 0;
            int horizontalScrollPosition = restoreView ? GetScrollPos(Handle, SB_HORZ) : 0;

            if (restoreView)
                SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

            try
            {
                if (append)
                    base.AppendText(text);
                else
                    base.Text = text;

                int restoredSelectionStart = Math.Min(selectionStart, TextLength);
                int restoredSelectionLength = Math.Min(selectionLength, TextLength - restoredSelectionStart);
                Select(restoredSelectionStart, restoredSelectionLength);

                if (restoreView)
                {
                    RestoreHorizontalScrollPosition(horizontalScrollPosition);

                    if (scrollToEnd)
                        ScrollToEnd();
                    else
                        RestoreFirstVisibleLine(firstVisibleLine);
                }
            }
            finally
            {
                if (restoreView)
                {
                    SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    Invalidate();
                }
            }
        }

        private bool IsLastLineVisible()
        {
            ScrollInfo scrollInfo = new ScrollInfo();
            scrollInfo.cbSize = (uint)Marshal.SizeOf(typeof(ScrollInfo));
            scrollInfo.fMask = SIF_ALL;

            if (GetScrollInfo(Handle, SB_VERT, ref scrollInfo))
            {
                int lastPosition = scrollInfo.nMax - Math.Max((int)scrollInfo.nPage - 1, 0);
                return scrollInfo.nPos >= lastPosition;
            }

            int visibleLineCount = Math.Max(1, (int)Math.Ceiling(ClientSize.Height / (double)Font.Height));
            int lastLine = GetLineFromCharIndex(TextLength);
            return GetFirstVisibleLine() + visibleLineCount > lastLine;
        }

        private int GetFirstVisibleLine()
        {
            return SendMessage(Handle, EM_GETFIRSTVISIBLELINE, IntPtr.Zero, IntPtr.Zero).ToInt32();
        }

        private void RestoreFirstVisibleLine(int firstVisibleLine)
        {
            int currentFirstVisibleLine = GetFirstVisibleLine();
            int lineOffset = firstVisibleLine - currentFirstVisibleLine;

            if (lineOffset != 0)
                SendMessage(Handle, EM_LINESCROLL, IntPtr.Zero, new IntPtr(lineOffset));
        }

        private void RestoreHorizontalScrollPosition(int scrollPosition)
        {
            scrollPosition = Math.Max(0, Math.Min(scrollPosition, ushort.MaxValue));
            SetScrollPos(Handle, SB_HORZ, scrollPosition, true);

            int wParam = SB_THUMBPOSITION + (scrollPosition << 16);
            SendMessage(Handle, WM_HSCROLL, new IntPtr(wParam), IntPtr.Zero);
        }
    }
}
