// PrePoMax for macOS - event arguments, delegates and small value types of System.Windows.Forms.

using System.ComponentModel;
using System.Drawing;

namespace System.Windows.Forms
{
    public delegate void MethodInvoker();
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);
    public delegate void KeyEventHandler(object sender, KeyEventArgs e);
    public delegate void KeyPressEventHandler(object sender, KeyPressEventArgs e);
    public delegate void PaintEventHandler(object sender, PaintEventArgs e);
    public delegate void InvalidateEventHandler(object sender, InvalidateEventArgs e);
    public delegate void ControlEventHandler(object sender, ControlEventArgs e);
    public delegate void LayoutEventHandler(object sender, LayoutEventArgs e);
    public delegate void PreviewKeyDownEventHandler(object sender, PreviewKeyDownEventArgs e);
    public delegate void FormClosingEventHandler(object sender, FormClosingEventArgs e);
    public delegate void FormClosedEventHandler(object sender, FormClosedEventArgs e);
    public delegate void DragEventHandler(object sender, DragEventArgs e);
    public delegate void GiveFeedbackEventHandler(object sender, GiveFeedbackEventArgs e);
    public delegate void QueryContinueDragEventHandler(object sender, QueryContinueDragEventArgs e);
    public delegate void HelpEventHandler(object sender, HelpEventArgs hlpevent);
    public delegate void ScrollEventHandler(object sender, ScrollEventArgs e);
    public delegate void DrawItemEventHandler(object sender, DrawItemEventArgs e);
    public delegate void MeasureItemEventHandler(object sender, MeasureItemEventArgs e);
    public delegate void ItemCheckEventHandler(object sender, ItemCheckEventArgs e);
    public delegate void SplitterEventHandler(object sender, SplitterEventArgs e);
    public delegate void SplitterCancelEventHandler(object sender, SplitterCancelEventArgs e);
    public delegate void TabControlEventHandler(object sender, TabControlEventArgs e);
    public delegate void TabControlCancelEventHandler(object sender, TabControlCancelEventArgs e);

    public delegate void UICuesEventHandler(object sender, UICuesEventArgs e);

    public class MouseEventArgs : EventArgs
    {
        public MouseButtons Button { get; }
        public int Clicks { get; }
        public int X { get; }
        public int Y { get; }
        public int Delta { get; }
        public Point Location { get { return new Point(X, Y); } }
        public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
        {
            Button = button;
            Clicks = clicks;
            X = x;
            Y = y;
            Delta = delta;
        }
    }

    public class HandledMouseEventArgs : MouseEventArgs
    {
        public bool Handled { get; set; }
        public HandledMouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta) : base(button, clicks, x, y, delta) { }
        public HandledMouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta, bool defaultHandledValue)
            : base(button, clicks, x, y, delta) { Handled = defaultHandledValue; }
    }

    public class KeyEventArgs : EventArgs
    {
        private bool _suppressKeyPress;
        public Keys KeyData { get; }
        public Keys KeyCode { get { return KeyData & Keys.KeyCode; } }
        public Keys Modifiers { get { return KeyData & Keys.Modifiers; } }
        public int KeyValue { get { return (int)(KeyData & Keys.KeyCode); } }
        public bool Alt { get { return (KeyData & Keys.Alt) == Keys.Alt; } }
        public bool Control { get { return (KeyData & Keys.Control) == Keys.Control; } }
        public bool Shift { get { return (KeyData & Keys.Shift) == Keys.Shift; } }
        public bool Handled { get; set; }
        public bool SuppressKeyPress
        {
            get { return _suppressKeyPress; }
            set { _suppressKeyPress = value; Handled = value; }
        }
        public KeyEventArgs(Keys keyData) { KeyData = keyData; }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public char KeyChar { get; set; }
        public bool Handled { get; set; }
        public KeyPressEventArgs(char keyChar) { KeyChar = keyChar; }
    }

    public class PreviewKeyDownEventArgs : EventArgs
    {
        public Keys KeyData { get; }
        public Keys KeyCode { get { return KeyData & Keys.KeyCode; } }
        public Keys Modifiers { get { return KeyData & Keys.Modifiers; } }
        public int KeyValue { get { return (int)(KeyData & Keys.KeyCode); } }
        public bool Alt { get { return (KeyData & Keys.Alt) == Keys.Alt; } }
        public bool Control { get { return (KeyData & Keys.Control) == Keys.Control; } }
        public bool Shift { get { return (KeyData & Keys.Shift) == Keys.Shift; } }
        public bool IsInputKey { get; set; }
        public PreviewKeyDownEventArgs(Keys keyData) { KeyData = keyData; }
    }

    public class PaintEventArgs : EventArgs, IDisposable
    {
        public Graphics Graphics { get; }
        public Rectangle ClipRectangle { get; }
        public PaintEventArgs(Graphics graphics, Rectangle clipRect)
        {
            Graphics = graphics;
            ClipRectangle = clipRect;
        }
        public void Dispose() { }
    }

    public class InvalidateEventArgs : EventArgs
    {
        public Rectangle InvalidRect { get; }
        public InvalidateEventArgs(Rectangle invalidRect) { InvalidRect = invalidRect; }
    }

    public class ControlEventArgs : EventArgs
    {
        public Control Control { get; }
        public ControlEventArgs(Control control) { Control = control; }
    }

    public sealed class LayoutEventArgs : EventArgs
    {
        public IComponent AffectedComponent { get; }
        public Control AffectedControl { get { return AffectedComponent as Control; } }
        public string AffectedProperty { get; }
        public LayoutEventArgs(IComponent affectedComponent, string affectedProperty)
        {
            AffectedComponent = affectedComponent;
            AffectedProperty = affectedProperty;
        }
        public LayoutEventArgs(Control affectedControl, string affectedProperty)
            : this((IComponent)affectedControl, affectedProperty) { }
    }

    public class FormClosingEventArgs : CancelEventArgs
    {
        public CloseReason CloseReason { get; }
        public FormClosingEventArgs(CloseReason closeReason, bool cancel) : base(cancel) { CloseReason = closeReason; }
    }

    public class FormClosedEventArgs : EventArgs
    {
        public CloseReason CloseReason { get; }
        public FormClosedEventArgs(CloseReason closeReason) { CloseReason = closeReason; }
    }

    public class DragEventArgs : EventArgs
    {
        public IDataObject Data { get; }
        public int KeyState { get; }
        public int X { get; }
        public int Y { get; }
        public DragDropEffects AllowedEffect { get; }
        public DragDropEffects Effect { get; set; }
        public DragEventArgs(IDataObject data, int keyState, int x, int y, DragDropEffects allowedEffect, DragDropEffects effect)
        {
            Data = data;
            KeyState = keyState;
            X = x;
            Y = y;
            AllowedEffect = allowedEffect;
            Effect = effect;
        }
    }

    public class GiveFeedbackEventArgs : EventArgs
    {
        public DragDropEffects Effect { get; }
        public bool UseDefaultCursors { get; set; }
        public GiveFeedbackEventArgs(DragDropEffects effect, bool useDefaultCursors)
        {
            Effect = effect;
            UseDefaultCursors = useDefaultCursors;
        }
    }

    public class QueryContinueDragEventArgs : EventArgs
    {
        public int KeyState { get; }
        public bool EscapePressed { get; }
        public DragAction Action { get; set; }
        public QueryContinueDragEventArgs(int keyState, bool escapePressed, DragAction action)
        {
            KeyState = keyState;
            EscapePressed = escapePressed;
            Action = action;
        }
    }

    public class HelpEventArgs : EventArgs
    {
        public Point MousePos { get; }
        public bool Handled { get; set; }
        public HelpEventArgs(Point mousePos) { MousePos = mousePos; }
    }

    public class ScrollEventArgs : EventArgs
    {
        public ScrollEventType Type { get; }
        public int NewValue { get; set; }
        public int OldValue { get; }
        public ScrollOrientation ScrollOrientation { get; }
        public ScrollEventArgs(ScrollEventType type, int newValue) : this(type, 0, newValue, ScrollOrientation.VerticalScroll) { }
        public ScrollEventArgs(ScrollEventType type, int newValue, ScrollOrientation scroll) : this(type, 0, newValue, scroll) { }
        public ScrollEventArgs(ScrollEventType type, int oldValue, int newValue) : this(type, oldValue, newValue, ScrollOrientation.VerticalScroll) { }
        public ScrollEventArgs(ScrollEventType type, int oldValue, int newValue, ScrollOrientation scroll)
        {
            Type = type;
            OldValue = oldValue;
            NewValue = newValue;
            ScrollOrientation = scroll;
        }
    }

    public class DrawItemEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public Font Font { get; }
        public Rectangle Bounds { get; }
        public int Index { get; }
        public DrawItemState State { get; }
        public Color ForeColor { get; }
        public Color BackColor { get; }
        public DrawItemEventArgs(Graphics graphics, Font font, Rectangle rect, int index, DrawItemState state)
            : this(graphics, font, rect, index, state, SystemColors.WindowText, SystemColors.Window) { }
        public DrawItemEventArgs(Graphics graphics, Font font, Rectangle rect, int index, DrawItemState state, Color foreColor,
                                 Color backColor)
        {
            Graphics = graphics;
            Font = font;
            Bounds = rect;
            Index = index;
            State = state;
            ForeColor = foreColor;
            BackColor = backColor;
        }
        public virtual void DrawBackground()
        {
            using (SolidBrush brush = new SolidBrush((State & DrawItemState.Selected) != 0 ? SystemColors.Highlight : BackColor))
                Graphics.FillRectangle(brush, Bounds);
        }
        public virtual void DrawFocusRectangle() { }
    }

    public class MeasureItemEventArgs : EventArgs
    {
        public Graphics Graphics { get; }
        public int Index { get; }
        public int ItemHeight { get; set; }
        public int ItemWidth { get; set; }
        public MeasureItemEventArgs(Graphics graphics, int index, int itemHeight)
        {
            Graphics = graphics;
            Index = index;
            ItemHeight = itemHeight;
        }
    }

    public enum CheckState { Unchecked = 0, Checked = 1, Indeterminate = 2 }

    public class ItemCheckEventArgs : EventArgs
    {
        public int Index { get; }
        public CheckState NewValue { get; set; }
        public CheckState CurrentValue { get; }
        public ItemCheckEventArgs(int index, CheckState newCheckValue, CheckState currentValue)
        {
            Index = index;
            NewValue = newCheckValue;
            CurrentValue = currentValue;
        }
    }

    public class SplitterEventArgs : EventArgs
    {
        public int X { get; }
        public int Y { get; }
        public int SplitX { get; set; }
        public int SplitY { get; set; }
        public SplitterEventArgs(int x, int y, int splitX, int splitY)
        {
            X = x;
            Y = y;
            SplitX = splitX;
            SplitY = splitY;
        }
    }

    public class SplitterCancelEventArgs : CancelEventArgs
    {
        public int MouseCursorX { get; }
        public int MouseCursorY { get; }
        public int SplitX { get; set; }
        public int SplitY { get; set; }
        public SplitterCancelEventArgs(int mouseCursorX, int mouseCursorY, int splitX, int splitY)
        {
            MouseCursorX = mouseCursorX;
            MouseCursorY = mouseCursorY;
            SplitX = splitX;
            SplitY = splitY;
        }
    }

    public enum TabControlAction { Selecting = 0, Selected = 1, Deselecting = 2, Deselected = 3 }

    public class TabControlEventArgs : EventArgs
    {
        public TabPage TabPage { get; }
        public int TabPageIndex { get; }
        public TabControlAction Action { get; }
        public TabControlEventArgs(TabPage tabPage, int tabPageIndex, TabControlAction action)
        {
            TabPage = tabPage;
            TabPageIndex = tabPageIndex;
            Action = action;
        }
    }

    public class TabControlCancelEventArgs : CancelEventArgs
    {
        public TabPage TabPage { get; }
        public int TabPageIndex { get; }
        public TabControlAction Action { get; }
        public TabControlCancelEventArgs(TabPage tabPage, int tabPageIndex, bool cancel, TabControlAction action) : base(cancel)
        {
            TabPage = tabPage;
            TabPageIndex = tabPageIndex;
            Action = action;
        }
    }

    public class UICuesEventArgs : EventArgs
    {
        public UICues Changed { get; }
        public UICuesEventArgs(UICues uicues) { Changed = uicues; }
    }

    public struct Message
    {
        public IntPtr HWnd { get; set; }
        public int Msg { get; set; }
        public IntPtr WParam { get; set; }
        public IntPtr LParam { get; set; }
        public IntPtr Result { get; set; }
        public static Message Create(IntPtr hWnd, int msg, IntPtr wparam, IntPtr lparam)
        {
            return new Message { HWnd = hWnd, Msg = msg, WParam = wparam, LParam = lparam };
        }
        public object GetLParam(Type cls) { return null; }
    }

    public class CreateParams
    {
        public string ClassName { get; set; }
        public string Caption { get; set; }
        public int Style { get; set; }
        public int ExStyle { get; set; }
        public int ClassStyle { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public IntPtr Parent { get; set; }
        public object Param { get; set; }
    }

    [TypeConverter(typeof(PaddingConverter))]
    [Serializable]
    public struct Padding
    {
        private int _left, _top, _right, _bottom;
        private bool _all;

        public Padding(int all)
        {
            _left = _top = _right = _bottom = all;
            _all = true;
        }
        public Padding(int left, int top, int right, int bottom)
        {
            _left = left;
            _top = top;
            _right = right;
            _bottom = bottom;
            _all = left == top && top == right && right == bottom;
        }
        public int All
        {
            get { return _all ? _left : -1; }
            set { _left = _top = _right = _bottom = value; _all = true; }
        }
        public int Left { get { return _left; } set { _left = value; _all = Check(); } }
        public int Top { get { return _top; } set { _top = value; _all = Check(); } }
        public int Right { get { return _right; } set { _right = value; _all = Check(); } }
        public int Bottom { get { return _bottom; } set { _bottom = value; _all = Check(); } }
        public int Horizontal { get { return _left + _right; } }
        public int Vertical { get { return _top + _bottom; } }
        public Size Size { get { return new Size(Horizontal, Vertical); } }
        private bool Check() { return _left == _top && _top == _right && _right == _bottom; }
        public static readonly Padding Empty = new Padding(0);
        public static Padding operator +(Padding p1, Padding p2)
        {
            return new Padding(p1.Left + p2.Left, p1.Top + p2.Top, p1.Right + p2.Right, p1.Bottom + p2.Bottom);
        }
        public static Padding operator -(Padding p1, Padding p2)
        {
            return new Padding(p1.Left - p2.Left, p1.Top - p2.Top, p1.Right - p2.Right, p1.Bottom - p2.Bottom);
        }
        public static bool operator ==(Padding p1, Padding p2)
        {
            return p1.Left == p2.Left && p1.Top == p2.Top && p1.Right == p2.Right && p1.Bottom == p2.Bottom;
        }
        public static bool operator !=(Padding p1, Padding p2) { return !(p1 == p2); }
        public static Padding Add(Padding p1, Padding p2) { return p1 + p2; }
        public static Padding Subtract(Padding p1, Padding p2) { return p1 - p2; }
        public override bool Equals(object other) { return other is Padding p && p == this; }
        public override int GetHashCode() { return Left ^ (Top << 8) ^ (Right << 16) ^ (Bottom << 24); }
        public override string ToString() { return "{Left=" + Left + ",Top=" + Top + ",Right=" + Right + ",Bottom=" + Bottom + "}"; }
    }

    public class PaddingConverter : TypeConverter { }

    [Serializable]
    public struct LinkArea
    {
        public int Start { get; set; }
        public int Length { get; set; }
        public bool IsEmpty { get { return Length == 0 && Start == 0; } }
        public LinkArea(int start, int length) { Start = start; Length = length; }
    }
}
