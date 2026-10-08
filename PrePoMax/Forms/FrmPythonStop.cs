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
    // own and therefore does. It is shown without taking the focus, and on top, because the main window it
    // stands for may be drawn over it by the system while that window is not answering
    public partial class FrmPythonStop : Form
    {
        // Variables                                                                                                                
        private readonly int _managedThreadId;
        private readonly Rectangle _ownerBounds;


        // Properties                                                                                                               
        protected override bool ShowWithoutActivation { get { return true; } }


        // Constructor                                                                                                              
        public FrmPythonStop(int managedThreadId, Rectangle ownerBounds)
        {
            InitializeComponent();
            //
            _managedThreadId = managedThreadId;
            _ownerBounds = ownerBounds;
        }


        // Event handlers                                                                                                           
        private void FrmPythonStop_Load(object sender, EventArgs e)
        {
            Rectangle bounds = _ownerBounds;
            if (!SystemInformation.VirtualScreen.IntersectsWith(bounds)) bounds = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(bounds.Left + (bounds.Width - Width) / 2, bounds.Top + (bounds.Height - Height) / 2);
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            btnStop.Enabled = false;
            if (PythonEngineManager.RequestInterrupt(_managedThreadId))
            {
                labMessage.Text = "Stopping. Python stops at its next statement; an operation of PrePoMax that " +
                                  "is already running, such as meshing, finishes first.";
            }
            else labMessage.Text = "Python has already finished.";
        }


        // Methods                                                                                                                  
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
