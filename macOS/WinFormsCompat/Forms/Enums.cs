// PrePoMax for macOS - enums of System.Windows.Forms with the values of .NET Framework.

namespace System.Windows.Forms
{
    public enum DockStyle { None = 0, Top = 1, Bottom = 2, Left = 3, Right = 4, Fill = 5 }

    [Flags]
    public enum AnchorStyles { None = 0, Top = 1, Bottom = 2, Left = 4, Right = 8 }

    public enum BorderStyle { None = 0, FixedSingle = 1, Fixed3D = 2 }

    public enum FormBorderStyle { None = 0, FixedSingle = 1, Fixed3D = 2, FixedDialog = 3, Sizable = 4, FixedToolWindow = 5, SizableToolWindow = 6 }

    public enum FormStartPosition { Manual = 0, CenterScreen = 1, WindowsDefaultLocation = 2, WindowsDefaultBounds = 3, CenterParent = 4 }

    public enum CloseReason { None = 0, WindowsShutDown = 1, MdiFormClosing = 2, UserClosing = 3, TaskManagerClosing = 4, FormOwnerClosing = 5, ApplicationExitCall = 6 }

    public enum AutoScaleMode { None = 0, Font = 1, Dpi = 2, Inherit = 3 }

    public enum AutoSizeMode { GrowAndShrink = 0, GrowOnly = 1 }

    public enum AutoValidate { Inherit = -1, Disable = 0, EnablePreventFocusChange = 1, EnableAllowFocusChange = 2 }

    public enum ImeMode { Inherit = -1, NoControl = 0, On = 1, Off = 2, Disable = 3, Hiragana = 4, Katakana = 5, KatakanaHalf = 6, AlphaFull = 7, Alpha = 8, HangulFull = 9, Hangul = 10, Close = 11, OnHalf = 12 }

    public enum RightToLeft { No = 0, Yes = 1, Inherit = 2 }

    public enum HorizontalAlignment { Left = 0, Right = 1, Center = 2 }

    public enum LeftRightAlignment { Left = 0, Right = 1 }

    public enum FlatStyle { Flat = 0, Popup = 1, Standard = 2, System = 3 }

    public enum Appearance { Normal = 0, Button = 1 }

    public enum ScrollBars { None = 0, Horizontal = 1, Vertical = 2, Both = 3 }

    public enum ScrollEventType { SmallDecrement = 0, SmallIncrement = 1, LargeDecrement = 2, LargeIncrement = 3, ThumbPosition = 4, ThumbTrack = 5, First = 6, Last = 7, EndScroll = 8 }

    public enum ScrollOrientation { HorizontalScroll = 0, VerticalScroll = 1 }

    public enum Orientation { Horizontal = 0, Vertical = 1 }

    public enum FixedPanel { None = 0, Panel1 = 1, Panel2 = 2 }

    public enum SelectionMode { None = 0, One = 1, MultiSimple = 2, MultiExtended = 3 }

    public enum DrawMode { Normal = 0, OwnerDrawFixed = 1, OwnerDrawVariable = 2 }

    [Flags]
    public enum DrawItemState { None = 0, Selected = 1, Grayed = 2, Disabled = 4, Checked = 8, Focus = 16, Default = 32, HotLight = 64, Inactive = 128, NoAccelerator = 256, NoFocusRect = 512, ComboBoxEdit = 4096 }

    public enum ComboBoxStyle { Simple = 0, DropDown = 1, DropDownList = 2 }

    public enum CharacterCasing { Normal = 0, Upper = 1, Lower = 2 }

    public enum TabAlignment { Top = 0, Bottom = 1, Left = 2, Right = 3 }

    public enum TabAppearance { Normal = 0, Buttons = 1, FlatButtons = 2 }

    public enum TabSizeMode { Normal = 0, FillToRight = 1, Fixed = 2 }

    public enum TabDrawMode { Normal = 0, OwnerDrawFixed = 1 }

    public enum View { LargeIcon = 0, Details = 1, SmallIcon = 2, List = 3, Tile = 4 }

    public enum SortOrder { None = 0, Ascending = 1, Descending = 2 }

    public enum ColumnHeaderStyle { None = 0, Nonclickable = 1, Clickable = 2 }

    public enum ItemActivation { Standard = 0, OneClick = 1, TwoClick = 2 }

    public enum ListViewAlignment { Default = 0, Left = 1, Top = 2, SnapToGrid = 5 }

    public enum ColumnHeaderAutoResizeStyle { None = 0, HeaderSize = 1, ColumnContent = 2 }

    public enum ImageLayout { None = 0, Tile = 1, Center = 2, Stretch = 3, Zoom = 4 }

    public enum PictureBoxSizeMode { Normal = 0, StretchImage = 1, AutoSize = 2, CenterImage = 3, Zoom = 4 }

    public enum ProgressBarStyle { Blocks = 0, Continuous = 1, Marquee = 2 }

    public enum TickStyle { None = 0, TopLeft = 1, BottomRight = 2, Both = 3 }

    public enum TextImageRelation { Overlay = 0, ImageAboveText = 1, TextAboveImage = 2, ImageBeforeText = 4, TextBeforeImage = 8 }

    public enum LinkBehavior { SystemDefault = 0, AlwaysUnderline = 1, HoverUnderline = 2, NeverUnderline = 3 }


    public enum TreeViewAction { Unknown = 0, ByKeyboard = 1, ByMouse = 2, Collapse = 3, Expand = 4 }

    public enum TreeViewDrawMode { Normal = 0, OwnerDrawText = 1, OwnerDrawAll = 2 }

    [Flags]
    public enum TreeNodeStates { Selected = 1, Grayed = 2, Checked = 8, Focused = 16, Default = 32, Hot = 64, Marked = 128, Indeterminate = 256, ShowKeyboardCues = 512 }

    public enum TreeViewHitTestLocations { None = 1, Image = 2, Label = 4, Indent = 8, AboveClientArea = 256, BelowClientArea = 512, LeftOfClientArea = 2048, RightOfClientArea = 1024, RightOfLabel = 32, StateImage = 64, PlusMinus = 16 }

    public enum ArrangeDirection { Left = 0, Right = 0, Down = 4, Up = 4 }

    public enum ToolStripItemDisplayStyle { None = 0, Text = 1, Image = 2, ImageAndText = 3 }

    public enum ToolStripLayoutStyle { StackWithOverflow = 0, HorizontalStackWithOverflow = 1, VerticalStackWithOverflow = 2, Flow = 3, Table = 4 }

    public enum ToolStripRenderMode { Custom = 0, System = 1, Professional = 2, ManagerRenderMode = 3 }

    public enum ToolStripGripStyle { Hidden = 0, Visible = 1 }

    public enum ToolStripItemAlignment { Left = 0, Right = 1 }

    public enum ToolStripItemOverflow { Never = 0, Always = 1, AsNeeded = 2 }

    public enum ToolStripItemImageScaling { None = 0, SizeToFit = 1 }

    public enum ToolStripTextDirection { Inherit = 0, Horizontal = 1, Vertical90 = 2, Vertical270 = 3 }

    public enum ToolStripDropDownDirection { AboveLeft = 0, AboveRight = 1, BelowLeft = 2, BelowRight = 3, Left = 4, Right = 5, Default = 7 }

    public enum ToolStripDropDownCloseReason { AppFocusChange = 0, AppClicked = 1, ItemClicked = 2, Keyboard = 3, CloseCalled = 4 }

    [Flags]
    public enum ToolStripStatusLabelBorderSides { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8, All = 15 }

    public enum MergeAction { Append = 0, Insert = 1, Replace = 2, Remove = 3, MatchOnly = 4 }


    public enum MessageBoxOptions { DefaultDesktopOnly = 131072, RightAlign = 524288, RtlReading = 1048576, ServiceNotification = 2097152 }

    public enum ValidationConstraints { None = 0, Selectable = 1, Enabled = 2, Visible = 4, TabStop = 8, ImmediateChildren = 16 }

    public enum UICues { None = 0, ShowFocus = 1, ShowKeyboard = 2, Shown = 3, ChangeFocus = 4, ChangeKeyboard = 8, Changed = 12 }

    [Flags]
    public enum ControlStyles
    {
        ContainerControl = 1, UserPaint = 2, Opaque = 4, ResizeRedraw = 16, FixedWidth = 32, FixedHeight = 64, StandardClick = 256,
        Selectable = 512, UserMouse = 1024, SupportsTransparentBackColor = 2048, StandardDoubleClick = 4096, AllPaintingInWmPaint = 8192,
        CacheText = 16384, EnableNotifyMessage = 32768, DoubleBuffer = 65536, OptimizedDoubleBuffer = 131072, UseTextForAccessibility = 262144
    }

    [Flags]
    public enum BoundsSpecified { None = 0, X = 1, Y = 2, Location = 3, Width = 4, Height = 8, Size = 12, All = 15 }

    public enum DragAction { Continue = 0, Drop = 1, Cancel = 2 }

    [Flags]
    public enum DragDropEffects { Scroll = -2147483648, All = -2147483645, None = 0, Copy = 1, Move = 2, Link = 4 }

    [Flags]
    public enum MouseButtons { None = 0, Left = 1048576, Right = 2097152, Middle = 4194304, XButton1 = 8388608, XButton2 = 16777216 }


    public enum SizeGripStyle { Auto = 0, Show = 1, Hide = 2 }


    public enum TableLayoutPanelCellBorderStyle { None = 0, Single = 1, Inset = 2, InsetDouble = 3, Outset = 4, OutsetDouble = 5, OutsetPartial = 6 }

    public enum TableLayoutPanelGrowStyle { FixedSize = 0, AddRows = 1, AddColumns = 2 }

    public enum SizeType { AutoSize = 0, Absolute = 1, Percent = 2 }

    public enum FlowDirection { LeftToRight = 0, TopDown = 1, RightToLeft = 2, BottomUp = 3 }


    [Flags]
    public enum TextFormatFlags
    {
        Default = 0, Left = 0, Top = 0, GlyphOverhangPadding = 0, HorizontalCenter = 1, Right = 2, VerticalCenter = 4, Bottom = 8,
        WordBreak = 16, SingleLine = 32, ExpandTabs = 64, NoClipping = 256, ExternalLeading = 512, NoPrefix = 2048, Internal = 4096,
        TextBoxControl = 8192, PathEllipsis = 16384, EndEllipsis = 32768, ModifyString = 65536, RightToLeft = 131072,
        WordEllipsis = 262144, NoFullWidthCharacterBreak = 524288, HidePrefix = 1048576, PrefixOnly = 2097152,
        PreserveGraphicsClipping = 16777216, PreserveGraphicsTranslateTransform = 33554432, NoPadding = 268435456,
        LeftAndRightPadding = 536870912
    }

    [Flags]
    public enum Keys
    {
        KeyCode = 65535, Modifiers = -65536, None = 0, LButton = 1, RButton = 2, Cancel = 3, MButton = 4, XButton1 = 5, XButton2 = 6,
        Back = 8, Tab = 9, LineFeed = 10, Clear = 12, Return = 13, Enter = 13, ShiftKey = 16, ControlKey = 17, Menu = 18,
        Pause = 19, Capital = 20, CapsLock = 20, KanaMode = 21, HanguelMode = 21, HangulMode = 21, JunjaMode = 23, FinalMode = 24,
        HanjaMode = 25, KanjiMode = 25, Escape = 27, IMEConvert = 28, IMENonconvert = 29, IMEAccept = 30, IMEAceept = 30,
        IMEModeChange = 31, Space = 32, Prior = 33, PageUp = 33, Next = 34, PageDown = 34, End = 35, Home = 36, Left = 37, Up = 38,
        Right = 39, Down = 40, Select = 41, Print = 42, Execute = 43, Snapshot = 44, PrintScreen = 44, Insert = 45, Delete = 46,
        Help = 47, D0 = 48, D1 = 49, D2 = 50, D3 = 51, D4 = 52, D5 = 53, D6 = 54, D7 = 55, D8 = 56, D9 = 57, A = 65, B = 66, C = 67,
        D = 68, E = 69, F = 70, G = 71, H = 72, I = 73, J = 74, K = 75, L = 76, M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82,
        S = 83, T = 84, U = 85, V = 86, W = 87, X = 88, Y = 89, Z = 90, LWin = 91, RWin = 92, Apps = 93, Sleep = 95, NumPad0 = 96,
        NumPad1 = 97, NumPad2 = 98, NumPad3 = 99, NumPad4 = 100, NumPad5 = 101, NumPad6 = 102, NumPad7 = 103, NumPad8 = 104,
        NumPad9 = 105, Multiply = 106, Add = 107, Separator = 108, Subtract = 109, Decimal = 110, Divide = 111, F1 = 112, F2 = 113,
        F3 = 114, F4 = 115, F5 = 116, F6 = 117, F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123, F13 = 124, F14 = 125,
        F15 = 126, F16 = 127, F17 = 128, F18 = 129, F19 = 130, F20 = 131, F21 = 132, F22 = 133, F23 = 134, F24 = 135,
        NumLock = 144, Scroll = 145, LShiftKey = 160, RShiftKey = 161, LControlKey = 162, RControlKey = 163, LMenu = 164,
        RMenu = 165, BrowserBack = 166, BrowserForward = 167, BrowserRefresh = 168, BrowserStop = 169, BrowserSearch = 170,
        BrowserFavorites = 171, BrowserHome = 172, VolumeMute = 173, VolumeDown = 174, VolumeUp = 175, MediaNextTrack = 176,
        MediaPreviousTrack = 177, MediaStop = 178, MediaPlayPause = 179, LaunchMail = 180, SelectMedia = 181,
        LaunchApplication1 = 182, LaunchApplication2 = 183, OemSemicolon = 186, Oem1 = 186, Oemplus = 187, Oemcomma = 188,
        OemMinus = 189, OemPeriod = 190, OemQuestion = 191, Oem2 = 191, Oemtilde = 192, Oem3 = 192, OemOpenBrackets = 219,
        Oem4 = 219, OemPipe = 220, Oem5 = 220, OemCloseBrackets = 221, Oem6 = 221, OemQuotes = 222, Oem7 = 222, Oem8 = 223,
        OemBackslash = 226, Oem102 = 226, ProcessKey = 229, Packet = 231, Attn = 246, Crsel = 247, Exsel = 248, EraseEof = 249,
        Play = 250, Zoom = 251, NoName = 252, Pa1 = 253, OemClear = 254, Shift = 65536, Control = 131072, Alt = 262144
    }
}
