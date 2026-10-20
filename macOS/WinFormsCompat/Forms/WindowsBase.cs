// PrePoMax for macOS - the classes of WindowsBase (WPF) that the WinForms code of PrePoMax uses.
using System;
using AT = Avalonia.Threading;

namespace System.Windows.Threading
{
    public enum DispatcherPriority
    {
        Invalid = -1, Inactive = 0, SystemIdle = 1, ApplicationIdle = 2, ContextIdle = 3, Background = 4, Input = 5,
        Loaded = 6, Render = 7, DataBind = 8, Normal = 9, Send = 10
    }

    // A timer of the UI thread, like the WPF DispatcherTimer
    public class DispatcherTimer
    {
        private readonly AT.DispatcherTimer _timer;

        public DispatcherTimer()
        {
            System.Windows.Forms.Toolkit.EnsureInitialized();
            _timer = new AT.DispatcherTimer();
            _timer.Tick += (s, e) => Tick?.Invoke(this, EventArgs.Empty);
        }
        public DispatcherTimer(DispatcherPriority priority) : this() { }

        public event EventHandler Tick;
        public TimeSpan Interval { get { return _timer.Interval; } set { _timer.Interval = value; } }
        public bool IsEnabled
        {
            get { return _timer.IsEnabled; }
            set { if (value) Start(); else Stop(); }
        }
        public object Tag { get; set; }
        public void Start() { _timer.Start(); }
        public void Stop() { _timer.Stop(); }
    }
}
