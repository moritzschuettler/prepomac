// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://github.com/PrePoMax/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;

namespace UserControls
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PARAFORMAT
    {
        public int cbSize;
        public uint dwMask;
        public short wNumbering;
        public short wReserved;
        public int dxStartIndent;
        public int dxRightIndent;
        public int dxOffset;
        public short wAlignment;
        public short cTabCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public int[] rgxTabs;
        // PARAFORMAT2 from here onwards
        public int dySpaceBefore;
        public int dySpaceAfter;
        public int dyLineSpacing;
        public short sStyle;
        public byte bLineSpacingRule;
        public byte bOutlineLevel;
        public short wShadingWeight;
        public short wShadingStyle;
        public short wNumberingStart;
        public short wNumberingStyle;
        public short wNumberingTab;
        public short wBorderSpace;
        public short wBorderWidth;
        public short wBorders;
    }
    public class ViewportEditorRichTextBox : RichTextBox
    {
        // DllImports                                                                                                                
        const int PFM_SPACEBEFORE = 0x00000040;
        const int PFM_SPACEAFTER = 0x00000080;
        const int PFM_LINESPACING = 0x00000100;
        const int PFM_STARTINDENT = 0x00000001;
        const int SCF_SELECTION = 1;
        const int EM_SETPARAFORMAT = 1095;
        const int EM_GETRECT = 0x00B2;
        //
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        //
        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, ref PARAFORMAT lParam);
        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, ref RECT lParam);
        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(HandleRef hWnd, int msg, int wParam, IntPtr lParam);
        const int WM_SETREDRAW = 0x000B;
        //
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct TEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public char tmFirstChar;
            public char tmLastChar;
            public char tmDefaultChar;
            public char tmBreakChar;
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetTextMetrics(IntPtr hdc, out TEXTMETRIC lptm);
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);
        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
        // Scroll
        private const UInt32 SB_TOP = 0x6;
        private const UInt32 WM_VSCROLL = 0x115;
        //
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);


        // Variables                                                                                                                
        private bool _beep;
        private Size _minSize;
        private Size _maxSize;
        private byte _lineSpacingRule;
        private int _lineSpacing;
        private double _linePitchInPixels;
        private bool _textOriginSet;
        private Point _textOrigin;
        private int _firstLineHeight;
        private int _firstLineSpacing;
        private int _startIndent;
        private bool _applyingLineFormats;
        private int _formattedLineCount;


        // Properties                                                                                                               
        public bool Beep { get { return _beep; } set { _beep = value; } }
        public Size MinSize
        {
            get { return _minSize; }
            set
            {
                _minSize = value;
                if (Width < _minSize.Width) Size = new Size(_minSize.Width, Height);
                if (Height < _minSize.Height) Size = new Size(Width, _minSize.Height);
            }
        }
        public Size MaxSize
        {
            get { return _maxSize; }
            set
            {
                _maxSize = value;
                if (Width > _maxSize.Width) Size = new Size(_maxSize.Width, Height);
                if (Height > _maxSize.Height) Size = new Size(Width, _maxSize.Height);
            }
        }
        public new string Text
        {
            get { return base.Text; }
            set
            {
                base.Text = value;
                ApplyLineFormats();
            }
        }


        // Constructors                                                                                                             
        public ViewportEditorRichTextBox()
        {
            _beep = false;
            _lineSpacingRule = 5;
            _lineSpacing = 23;
        }


        // Methods                                                                                                                  
        public void SetTextFont(string familyName, float sizeInPixels, double linePitchInPixels)
        {
            Font = new Font(familyName, sizeInPixels, FontStyle.Regular, GraphicsUnit.Pixel);
            _linePitchInPixels = linePitchInPixels;
            _textOriginSet = false;
            _startIndent = 0;
            using (Graphics graphics = CreateGraphics())
            {
                _lineSpacingRule = 4;
                _lineSpacing = (int)Math.Round(linePitchInPixels * 1440 / graphics.DpiY);
            }
            ApplyLineFormats();
        }
        // Moves the text so that the left edge of its first line and the row the bottom of its capitals is drawn on
        // fall on the point, in pixels from the top left corner of the text box. An exact line spacing puts the space a
        // line has over its font above it, so the first line gets the exact height that ends one descent below the
        // point, and every line after it the line pitch
        // The leftmost column text can start at, which an origin further left cannot move it past
        public int GetMinimumTextLeft()
        {
            RECT rectangle = new RECT();
            SendMessage(new HandleRef(this, Handle), EM_GETRECT, 0, ref rectangle);
            return rectangle.Left;
        }
        public void SetTextOrigin(Point textOrigin)
        {
            RECT rectangle = new RECT();
            SendMessage(new HandleRef(this, Handle), EM_GETRECT, 0, ref rectangle);
            _textOrigin = textOrigin;
            _firstLineHeight = textOrigin.Y - rectangle.Top + 1 + GetFontDescent();
            using (Graphics graphics = CreateGraphics())
            {
                _firstLineSpacing = (int)Math.Round(_firstLineHeight * 1440 / graphics.DpiY);
                _startIndent = (int)Math.Round(Math.Max(0, textOrigin.X - rectangle.Left) * 1440 / graphics.DpiX);
            }
            _textOriginSet = true;
            ApplyLineFormats();
        }
        public void ResetTextOrigin()
        {
            _textOriginSet = false;
            _startIndent = 0;
            ApplyLineFormats();
        }
        // The descent GDI draws the font with, which the rich edit control lays its lines out by
        private int GetFontDescent()
        {
            using (Graphics graphics = CreateGraphics())
            {
                IntPtr deviceContext = graphics.GetHdc();
                IntPtr font = Font.ToHfont();
                try
                {
                    IntPtr previousFont = SelectObject(deviceContext, font);
                    TEXTMETRIC textMetric;
                    GetTextMetrics(deviceContext, out textMetric);
                    SelectObject(deviceContext, previousFont);
                    return textMetric.tmDescent;
                }
                finally
                {
                    DeleteObject(font);
                    graphics.ReleaseHdc(deviceContext);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rule">
        /// 0 - Single spacing.The dyLineSpacing member is ignored.
        /// 1 - One - and - a - half spacing.The dyLineSpacing member is ignored.
        /// 2 - Double spacing.The dyLineSpacing member is ignored.
        /// 3 - The dyLineSpacing member specifies the spacing from one line to the next, in twips.However, if dyLineSpacing
        /// specifies a value that is less than single spacing, the control displays single - spaced text.
        /// 4 - The dyLineSpacing member specifies the spacing from one line to the next, in twips.The control uses the exact
        /// spacing specified, even if dyLineSpacing specifies a value that is less than single spacing.
        /// 5 - The value of dyLineSpacing / 20 is the spacing, in lines, from one line to the next. Thus, setting
        /// dyLineSpacing to 20 produces single-spaced text, 40 is double spaced, 60 is triple spaced, and so on.
        /// </param>
        /// <param name="space">Line spacing</param>
        private void SetLineFormat(int start, int length, byte rule, int space)
        {
            PARAFORMAT fmt = new PARAFORMAT();
            fmt.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(fmt);
            fmt.dwMask = PFM_LINESPACING | PFM_STARTINDENT;
            fmt.dyLineSpacing = space;
            fmt.bLineSpacingRule = rule;
            fmt.dxStartIndent = _startIndent;
            Select(start, length);
            SendMessage(new HandleRef(this, this.Handle), EM_SETPARAFORMAT, SCF_SELECTION, ref fmt);
        }
        // Every line gets the line spacing, and with a text origin the first line its own height. Typed text takes the
        // format of its line, so only a change of the lines needs the formats again. The formats are set through the
        // selection, which is not drawn meanwhile
        private void ApplyLineFormats()
        {
            ApplyLineFormats(true);
        }
        private void ApplyLineFormats(bool force)
        {
            if (_applyingLineFormats) return;
            string text = base.Text;
            int lineCount = 1;
            foreach (char character in text) if (character == '\n') lineCount++;
            if (!force && lineCount == _formattedLineCount) return;
            _formattedLineCount = lineCount;
            //
            _applyingLineFormats = true;
            HandleRef handle = new HandleRef(this, Handle);
            SendMessage(handle, WM_SETREDRAW, 0, IntPtr.Zero);
            try
            {
                int selectionStart = SelectionStart;
                int selectionLength = SelectionLength;
                SetLineFormat(0, TextLength, _lineSpacingRule, _lineSpacing);
                if (_textOriginSet)
                {
                    int firstLineEnd = text.IndexOf('\n');
                    SetLineFormat(0, firstLineEnd < 0 ? TextLength : firstLineEnd, 4, _firstLineSpacing);
                }
                Select(selectionStart, selectionLength);
            }
            finally
            {
                SendMessage(handle, WM_SETREDRAW, 1, IntPtr.Zero);
                Invalidate();
                _applyingLineFormats = false;
            }
        }
        // The text over a text origin: its widest line as the control lays it out, from the origin and with room for
        // the caret, and the first line and the line pitch of the others down to the bottom of the last line. A line
        // wider than the control is scrolled to the caret, which moves both of its ends
        private Size GetTextOriginSize()
        {
            string text = base.Text;
            int lineCount = 1;
            int width = 0;
            int lineStart = 0;
            while (true)
            {
                int lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd < 0) lineEnd = text.Length;
                width = Math.Max(width, GetPositionFromCharIndex(lineEnd).X - GetPositionFromCharIndex(lineStart).X);
                if (lineEnd == text.Length) break;
                lineStart = lineEnd + 1;
                lineCount++;
            }
            int height = _firstLineHeight + (int)Math.Round((lineCount - 1) * _linePitchInPixels);
            return new Size(_textOrigin.X + width + 2, height);
        }
        //
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                Paste(DataFormats.GetFormat("Text"));
                e.Handled = true;
            }
            //
            if (!_beep)
            {
                if (GetLineFromCharIndex(SelectionStart) == 0 && (e.KeyData == Keys.Up || e.KeyData == Keys.PageUp))
                    e.Handled = true;
                else if (GetLineFromCharIndex(SelectionStart) == GetLineFromCharIndex(TextLength) &&
                    (e.KeyData == Keys.Down || e.KeyData == Keys.PageDown))
                    e.Handled = true;
                else if (SelectionStart == TextLength && (e.KeyData == Keys.Right || e.KeyData == Keys.End))
                    e.Handled = true;
                else if (SelectionStart == 0 && (e.KeyData == Keys.Left || e.KeyData == Keys.Home))
                    e.Handled = true;
            }
            //
            base.OnKeyDown(e);
        }
        protected override void OnVScroll(EventArgs e)
        {
            // Prevents autoscroll
            PostMessage(Handle, WM_VSCROLL, (IntPtr)SB_TOP, IntPtr.Zero);
            //
            base.OnVScroll(e);
        }
        protected override void OnTextChanged(EventArgs e)
        {
            Size newSize;
            if (_textOriginSet)
            {
                ApplyLineFormats(false);
                newSize = GetTextOriginSize();
            }
            else
            {
                Size size = GetPreferredSize(Size.Empty);
                newSize = new Size((int)(size.Width * 1.0), (int)(size.Height * 1.15));
            }
            //
            if (newSize.Width < _minSize.Width) newSize.Width = _minSize.Width;
            if (newSize.Height < _minSize.Height) newSize.Height = _minSize.Height;
            if (newSize.Width > _maxSize.Width) newSize.Width = _maxSize.Width;
            if (newSize.Height > _maxSize.Height) newSize.Height = _maxSize.Height;
            //
            Size = newSize;
            //
            base.OnTextChanged(e);
        }
        
    }
}
