// PrePoMax for macOS - clipboard, data objects, cursors and the Win32 messages the PrePoMax code sends to controls.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using Avalonia.Input;
using Avalonia.Threading;
using AC = Avalonia.Controls;

namespace System.Windows.Forms
{
    public interface IDataObject
    {
        object GetData(string format, bool autoConvert);
        object GetData(string format);
        object GetData(Type format);
        void SetData(string format, bool autoConvert, object data);
        void SetData(string format, object data);
        void SetData(Type format, object data);
        void SetData(object data);
        bool GetDataPresent(string format, bool autoConvert);
        bool GetDataPresent(string format);
        bool GetDataPresent(Type format);
        string[] GetFormats(bool autoConvert);
        string[] GetFormats();
    }

    public static class DataFormats
    {
        public static readonly string Text = "Text";
        public static readonly string UnicodeText = "UnicodeText";
        public static readonly string StringFormat = typeof(string).FullName;
        public static readonly string FileDrop = "FileDrop";
        public static readonly string Bitmap = "Bitmap";
        public static readonly string Html = "HTML Format";
        public static readonly string Rtf = "Rich Text Format";
        public static readonly string CommaSeparatedValue = "Csv";
        public static readonly string Serializable = "WindowsForms10PersistentObject";
        public class Format
        {
            public string Name { get; }
            public int Id { get; }
            public Format(string name, int id) { Name = name; Id = id; }
        }
        public static Format GetFormat(string format) { return new Format(format, format.GetHashCode()); }
    }

    public enum TextDataFormat { Text = 0, UnicodeText = 1, Rtf = 2, Html = 3, CommaSeparatedValue = 4 }

    public class DataObject : IDataObject
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        public DataObject() { }
        public DataObject(object data) { SetData(data); }
        public DataObject(string format, object data) { SetData(format, data); }
        public object GetData(string format, bool autoConvert) { return GetData(format); }
        public object GetData(string format)
        {
            if (_data.TryGetValue(format, out object value)) return value;
            if ((format == DataFormats.Text || format == DataFormats.UnicodeText || format == DataFormats.StringFormat))
            {
                if (_data.TryGetValue(DataFormats.UnicodeText, out value)) return value;
                if (_data.TryGetValue(DataFormats.Text, out value)) return value;
                if (_data.TryGetValue(DataFormats.StringFormat, out value)) return value;
            }
            return null;
        }
        public object GetData(Type format) { return GetData(format.FullName); }
        public void SetData(string format, bool autoConvert, object data) { SetData(format, data); }
        public void SetData(string format, object data) { _data[format] = data; }
        public void SetData(Type format, object data) { SetData(format.FullName, data); }
        public void SetData(object data)
        {
            if (data is string s) SetData(DataFormats.UnicodeText, s);
            else if (data != null) SetData(data.GetType().FullName, data);
        }
        public bool GetDataPresent(string format, bool autoConvert) { return GetData(format) != null; }
        public bool GetDataPresent(string format) { return GetData(format) != null; }
        public bool GetDataPresent(Type format) { return GetData(format) != null; }
        public string[] GetFormats(bool autoConvert) { return GetFormats(); }
        public string[] GetFormats() { return new List<string>(_data.Keys).ToArray(); }
        public bool ContainsImage() { return GetData(DataFormats.Bitmap) != null; }
        public Image GetImage() { return GetData(DataFormats.Bitmap) as Image; }
        public void SetImage(Image image) { SetData(DataFormats.Bitmap, image); }
        public bool ContainsText() { return GetData(DataFormats.UnicodeText) != null; }
        public string GetText() { return GetData(DataFormats.UnicodeText) as string; }
        public void SetText(string textData) { SetData(DataFormats.UnicodeText, textData); }
        public void SetText(string textData, TextDataFormat format) { SetData(DataFormats.UnicodeText, textData); }
        public bool ContainsFileDropList() { return GetData(DataFormats.FileDrop) != null; }
        public StringCollection GetFileDropList()
        {
            StringCollection files = new StringCollection();
            if (GetData(DataFormats.FileDrop) is string[] list) files.AddRange(list);
            return files;
        }
    }

    /// <summary>The clipboard of macOS. Objects other than text are kept in the process.</summary>
    public static class Clipboard
    {
        private static IDataObject _localData;
        private static string _localText;

        private static Avalonia.Input.Platform.IClipboard Native
        {
            get
            {
                Toolkit.EnsureInitialized();
                var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
                foreach (Form form in Application.OpenForms)
                {
                    var topLevel = AC.TopLevel.GetTopLevel(form.NativeControl);
                    if (topLevel?.Clipboard != null) return topLevel.Clipboard;
                }
                return null;
            }
        }
        private static T Run<T>(Func<System.Threading.Tasks.Task<T>> task)
        {
            var t = task();
            if (Dispatcher.UIThread.CheckAccess())
            {
                while (!t.IsCompleted) Dispatcher.UIThread.RunJobs();
            }
            return t.GetAwaiter().GetResult();
        }
        private static void Run(Func<System.Threading.Tasks.Task> task)
        {
            var t = task();
            if (Dispatcher.UIThread.CheckAccess())
            {
                while (!t.IsCompleted) Dispatcher.UIThread.RunJobs();
            }
            t.GetAwaiter().GetResult();
        }
        public static void SetText(string text)
        {
            _localText = text;
            _localData = null;
            var clipboard = Native;
            if (clipboard != null) Run(() => clipboard.SetTextAsync(text));
        }
        public static void SetText(string text, TextDataFormat format) { SetText(text); }
        public static string GetText()
        {
            var clipboard = Native;
            if (clipboard != null)
            {
                string text = Run(() => clipboard.GetTextAsync());
                if (text != null) return text;
            }
            return _localText ?? string.Empty;
        }
        public static string GetText(TextDataFormat format) { return GetText(); }
        public static bool ContainsText() { return !string.IsNullOrEmpty(GetText()); }
        public static bool ContainsText(TextDataFormat format) { return ContainsText(); }
        public static void SetDataObject(object data) { SetDataObject(data, false); }
        public static void SetDataObject(object data, bool copy)
        {
            IDataObject dataObject = data as IDataObject ?? new DataObject(data);
            _localData = dataObject;
            object text = dataObject.GetData(DataFormats.UnicodeText) ?? dataObject.GetData(DataFormats.Text);
            if (text is string s)
            {
                _localText = s;
                var clipboard = Native;
                if (clipboard != null) Run(() => clipboard.SetTextAsync(s));
            }
        }
        public static void SetDataObject(object data, bool copy, int retryTimes, int retryDelay) { SetDataObject(data, copy); }
        public static IDataObject GetDataObject()
        {
            if (_localData != null) return _localData;
            return new DataObject(DataFormats.UnicodeText, GetText());
        }
        public static void SetData(string format, object data)
        {
            DataObject dataObject = new DataObject(format, data);
            _localData = dataObject;
            if (data is string s) SetText(s);
        }
        public static object GetData(string format)
        {
            if (format == DataFormats.Text || format == DataFormats.UnicodeText) return GetText();
            return _localData?.GetData(format);
        }
        public static bool ContainsData(string format) { return GetData(format) != null; }
        public static void Clear()
        {
            _localData = null;
            _localText = null;
            var clipboard = Native;
            if (clipboard != null) Run(() => clipboard.ClearAsync());
        }
        public static bool ContainsFileDropList() { return false; }
        public static StringCollection GetFileDropList() { return new StringCollection(); }
        public static bool ContainsImage() { return false; }
        public static Image GetImage() { return null; }
        public static void SetImage(Image image) { _localData = new DataObject(DataFormats.Bitmap, image); }
    }

    public sealed class Cursor : IDisposable
    {
        internal Avalonia.Input.Cursor Native { get; }
        private readonly string _name;
        internal Cursor(StandardCursorType type, string name)
        {
            _name = name;
            try
            {
                Native = new Avalonia.Input.Cursor(type);
            }
            catch
            {
                Native = null;
            }
        }
        public Cursor(IntPtr handle) : this(StandardCursorType.Arrow, "Handle") { }
        public Cursor(string fileName) : this(StandardCursorType.Arrow, fileName) { }
        public Cursor(System.IO.Stream stream) : this(StandardCursorType.Arrow, "Stream") { }
        public Cursor(Type type, string resource) : this(StandardCursorType.Arrow, resource) { }
        public IntPtr Handle { get { return IntPtr.Zero; } }
        public Size Size { get { return new Size(32, 32); } }
        public Point HotSpot { get { return Point.Empty; } }
        public static Point Position
        {
            get { return Input.MousePosition; }
            set { }
        }
        public static Cursor Current { get; set; }
        public static Rectangle Clip { get; set; }
        public static void Show() { }
        public static void Hide() { }
        public void Dispose() { }
        public override string ToString() { return "[Cursor: " + _name + "]"; }
    }

    public static class Cursors
    {
        private static Cursor _default, _arrow, _wait, _hand, _ibeam, _cross, _sizeAll, _sizeNS, _sizeWE, _sizeNWSE, _sizeNESW, _no, _help, _appStarting, _hSplit, _vSplit, _upArrow;
        public static Cursor Default { get { return _default ?? (_default = new Cursor(StandardCursorType.Arrow, "Default")); } }
        public static Cursor Arrow { get { return _arrow ?? (_arrow = new Cursor(StandardCursorType.Arrow, "Arrow")); } }
        public static Cursor WaitCursor { get { return _wait ?? (_wait = new Cursor(StandardCursorType.Wait, "WaitCursor")); } }
        public static Cursor AppStarting { get { return _appStarting ?? (_appStarting = new Cursor(StandardCursorType.AppStarting, "AppStarting")); } }
        public static Cursor Hand { get { return _hand ?? (_hand = new Cursor(StandardCursorType.Hand, "Hand")); } }
        public static Cursor IBeam { get { return _ibeam ?? (_ibeam = new Cursor(StandardCursorType.Ibeam, "IBeam")); } }
        public static Cursor Cross { get { return _cross ?? (_cross = new Cursor(StandardCursorType.Cross, "Cross")); } }
        public static Cursor SizeAll { get { return _sizeAll ?? (_sizeAll = new Cursor(StandardCursorType.SizeAll, "SizeAll")); } }
        public static Cursor SizeNS { get { return _sizeNS ?? (_sizeNS = new Cursor(StandardCursorType.SizeNorthSouth, "SizeNS")); } }
        public static Cursor SizeWE { get { return _sizeWE ?? (_sizeWE = new Cursor(StandardCursorType.SizeWestEast, "SizeWE")); } }
        public static Cursor SizeNWSE { get { return _sizeNWSE ?? (_sizeNWSE = new Cursor(StandardCursorType.TopLeftCorner, "SizeNWSE")); } }
        public static Cursor SizeNESW { get { return _sizeNESW ?? (_sizeNESW = new Cursor(StandardCursorType.TopRightCorner, "SizeNESW")); } }
        public static Cursor No { get { return _no ?? (_no = new Cursor(StandardCursorType.No, "No")); } }
        public static Cursor Help { get { return _help ?? (_help = new Cursor(StandardCursorType.Help, "Help")); } }
        public static Cursor HSplit { get { return _hSplit ?? (_hSplit = new Cursor(StandardCursorType.SizeNorthSouth, "HSplit")); } }
        public static Cursor VSplit { get { return _vSplit ?? (_vSplit = new Cursor(StandardCursorType.SizeWestEast, "VSplit")); } }
        public static Cursor UpArrow { get { return _upArrow ?? (_upArrow = new Cursor(StandardCursorType.UpArrow, "UpArrow")); } }
        public static Cursor PanEast { get { return SizeAll; } }
        public static Cursor PanNorth { get { return SizeAll; } }
        public static Cursor PanSouth { get { return SizeAll; } }
        public static Cursor PanWest { get { return SizeAll; } }
        public static Cursor NoMove2D { get { return SizeAll; } }
    }

    /// <summary>The Win32 messages the PrePoMax code sends to its controls through SendMessage.</summary>
    public static class Win32Messages
    {
        public const int WM_SETREDRAW = 0x000B;
        public const int WM_VSCROLL = 0x0115;
        public const int WM_HSCROLL = 0x0114;
        public const int EM_LINESCROLL = 0x00B6;
        public const int EM_SCROLLCARET = 0x00B7;
        public const int EM_GETFIRSTVISIBLELINE = 0x00CE;
        public const int EM_SETTABSTOPS = 0x00CB;
        public const int SB_BOTTOM = 7;
        public const int SB_TOP = 6;

        // Handlers of controls for messages (scroll and redraw); returns true if handled
        internal static bool Handle(Control control, ref Message m)
        {
            if (control is IWin32MessageTarget target) return target.HandleMessage(ref m);
            switch (m.Msg)
            {
                case WM_SETREDRAW:
                    if (((long)m.WParam & 0xFFFFFFFF) != 0) control.Invalidate(true);
                    return true;
            }
            return false;
        }
    }

    internal interface IWin32MessageTarget
    {
        bool HandleMessage(ref Message m);
    }
}
