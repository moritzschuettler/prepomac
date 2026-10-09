// PrePoMax - Copyright (C) 2016-2026 Matej Borovinšek
//
// Licensed under the terms defined in the LICENSE file located in the root directory of this source code.
//
// Source code: https://gitlab.com/MatejB/PrePoMax
//
// Author: Matej Borovinšek
// Contributors:

using PrePoMax.Python;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace PrePoMax.Forms
{
    // The stop button of Python run from the Python panel. That Python runs on the thread that owns the main
    // window, so the window does not answer while it runs; this window runs a message loop on a thread of its
    // own and therefore does. It is on top, because the main window it stands for may be drawn over it by the
    // system while that window is not answering, and it never takes the activation: neither when it is shown nor
    // when its button is clicked, so the keyboard stays where the user had it. TopMost is set through the window
    // style rather than the property, because the property makes WinForms ignore ShowWithoutActivation
    public partial class FrmPythonStop : Form
    {
        // Variables                                                                                                                
        private const int WS_EX_TOPMOST = 0x00000008;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WM_MOUSEACTIVATE = 0x0021;
        private const int MA_NOACTIVATE = 3;
        private readonly PythonStopWatchdog _watchdog;
        private readonly Rectangle _ownerBounds;


        // Properties                                                                                                               
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams createParams = base.CreateParams;
                createParams.ExStyle |= WS_EX_TOPMOST | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return createParams;
            }
        }


        // Constructor                                                                                                              
        public FrmPythonStop(PythonStopWatchdog watchdog, Rectangle ownerBounds)
        {
            InitializeComponent();
            //
            _watchdog = watchdog;
            _ownerBounds = ownerBounds;
        }


        // Event handlers                                                                                                           
        private void FrmPythonStop_Load(object sender, EventArgs e)
        {
            Rectangle bounds = _ownerBounds;
            if (!SystemInformation.VirtualScreen.IntersectsWith(bounds)) bounds = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(bounds.Left + (bounds.Width - Width) / 2, bounds.Top + (bounds.Height - Height) / 2);
        }
        // Each click delivers an interrupt of its own, because code that catches one - a bare except - goes on
        // running, so the button is offered again after a moment while the Python still runs
        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            bool isDelivered;
            if (_watchdog.RequestStop(out isDelivered))
            {
                if (isDelivered)
                {
                    labMessage.Text = "Stopping. Python stops at its next statement; a wait such as time.sleep " +
                                      "returns first. Press Stop again if it keeps running.";
                }
                else
                {
                    labMessage.Text = "Stopping. Python is inside an operation of PrePoMax, such as meshing, and " +
                                      "stops when that operation finishes.";
                }
            }
            else if (_watchdog.IsRunning()) labMessage.Text = "Python is not running at the moment.";
            else labMessage.Text = "Python has already finished.";
            //
            timerEnableStop.Stop();
            timerEnableStop.Start();
        }
        private void timerEnableStop_Tick(object sender, EventArgs e)
        {
            timerEnableStop.Stop();
            if (_watchdog.IsRunning()) btnStop.Enabled = true;
        }


        // Methods                                                                                                                  
        // A click must not activate the window either; the button still receives it
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }
            base.WndProc(ref m);
        }
        // Called from the thread that ran the Python, once it is done
        public void CloseFromOtherThread()
        {
            try
            {
                if (!IsDisposed && IsHandleCreated) BeginInvoke(new MethodInvoker(Close));
            }
            catch (InvalidOperationException)
            {
                // The user closed it in the meantime
            }
        }
    }
}
